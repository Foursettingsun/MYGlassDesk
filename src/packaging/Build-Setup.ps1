param(
    [Parameter(Mandatory = $true)][string]$RuntimeDirectory,
    [Parameter(Mandatory = $true)][string]$CompilerPath,
    [string]$SettingsPath = (Join-Path $PSScriptRoot 'default-settings.json')
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path -LiteralPath $RuntimeDirectory).Path
$buildRoot = $PSScriptRoot
$payloadRoot = Join-Path $buildRoot 'payload'
$compiler = (Resolve-Path -LiteralPath $CompilerPath).Path
$settings = (Resolve-Path -LiteralPath $SettingsPath).Path
$signature = Get-AuthenticodeSignature -LiteralPath $compiler
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Pyrsys B.V.') {
    throw 'The portable compiler does not have the expected valid publisher signature.'
}
New-Item -ItemType Directory -Path $payloadRoot -Force | Out-Null
$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'runtime-manifest.json') -Raw | ConvertFrom-Json
$runtimePaths = @($manifest.files | ForEach-Object { $_.path })
foreach ($entry in $manifest.files) {
    if ([IO.Path]::IsPathRooted($entry.path) -or $entry.path -match '(^|[\\/])\.\.([\\/]|$)') {
        throw 'Unexpected manifest path.'
    }
    $source = Join-Path $projectRoot $entry.path
    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $entry.sha256) {
        throw "Runtime file changed since archive: $($entry.path)"
    }
    $destination = Join-Path $payloadRoot $entry.path
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination -Force
}
New-Item -ItemType Directory -Path (Join-Path $payloadRoot 'Data') -Force | Out-Null
Copy-Item -LiteralPath $settings -Destination (Join-Path $payloadRoot 'Data\settings.json') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'runtime-manifest.json') -Destination (Join-Path $payloadRoot 'runtime-manifest.json') -Force
Copy-Item -LiteralPath (Join-Path $buildRoot 'MODIFICATIONS.txt') -Destination (Join-Path $payloadRoot 'MODIFICATIONS.txt') -Force

$allPayloadPaths = @($runtimePaths) + @('runtime-manifest.json', 'MODIFICATIONS.txt', 'Data/settings.json')
$actualPaths = @(Get-ChildItem -LiteralPath $payloadRoot -Recurse -File | ForEach-Object {
    $_.FullName.Substring($payloadRoot.Length + 1).Replace('\', '/')
})
if (@(Compare-Object ($allPayloadPaths | Sort-Object) ($actualPaths | Sort-Object)).Count -ne 0) {
    throw 'Payload has missing or unexpected files.'
}
$issFiles = foreach ($path in $allPayloadPaths) {
    $relativePath = $path.Replace('/', '\')
    $relativeDirectory = Split-Path -Parent $relativePath
    $destination = if ($relativeDirectory) { '{app}\' + $relativeDirectory } else { '{app}' }
    $flags = if ($path -eq 'Data/settings.json') { 'onlyifdoesntexist uninsneveruninstall' } else { 'ignoreversion' }
    'Source: "payload\' + $relativePath + '"; DestDir: "' + $destination + '"; Flags: ' + $flags
}
[IO.File]::WriteAllLines((Join-Path $buildRoot 'payload-files.iss'), [string[]]$issFiles, [Text.UTF8Encoding]::new($false))
$payloadManifest = foreach ($path in $allPayloadPaths) {
    $file = Get-Item -LiteralPath (Join-Path $payloadRoot $path)
    [ordered]@{ path = $path; bytes = $file.Length; sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash }
}
[IO.File]::WriteAllText((Join-Path $buildRoot 'payload-manifest.json'), (@{ files = @($payloadManifest) } | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
Push-Location $buildRoot
try {
    & $compiler /Qp 'GlassDesk.iss'
    if ($LASTEXITCODE -ne 0) { throw "Compiler failed with exit code $LASTEXITCODE" }
} finally {
    Pop-Location
}
Get-Item -LiteralPath (Join-Path $buildRoot 'output\GlassDesk-1.5.2-Modified-Setup.exe') |
    Select-Object FullName, Length
