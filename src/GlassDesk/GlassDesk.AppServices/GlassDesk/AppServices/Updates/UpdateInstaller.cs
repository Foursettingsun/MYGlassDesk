using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using GlassDesk.Platform.Installation;

namespace GlassDesk.AppServices.Updates;

public static class UpdateInstaller
{
	public static async Task<ProcessStartInfo> PrepareAsync(UpdateRelease release, string file, InstallationInfo installation, CancellationToken token)
	{
		if (installation.State != InstallationState.VerifiedInstalled || string.IsNullOrWhiteSpace(installation.InstallDirectory))
		{
			throw new InvalidOperationException("当前程序不是已核验的安装版，请先手动安装正式安装包。");
		}
		UpdateClient.ValidateInstaller(release);
		string directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(installation.InstallDirectory));
		string installer = Path.GetFullPath(file);
		if (installer.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("更新安装包必须位于安装目录之外。");
		}
		if (!Directory.Exists(directory) || Path.GetFileName(installer) != release.FileName)
		{
			throw new InvalidDataException("安装目录或更新安装包路径无效。");
		}
		await using (FileStream stream = new FileStream(installer, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync: true))
		{
			bool flag = stream.Length != release.Size;
			if (!flag)
			{
				flag = !Convert.ToHexString(await SHA256.HashDataAsync(stream, token).ConfigureAwait(continueOnCapturedContext: false)).Equals(release.Sha256, StringComparison.OrdinalIgnoreCase);
			}
			if (flag)
			{
				throw new InvalidDataException("下载后的安装包发生变化，请重新下载。");
			}
		}
		return new ProcessStartInfo(installer)
		{
			UseShellExecute = true,
			WorkingDirectory = Path.GetDirectoryName(installer),
			ArgumentList = 
			{
				"/SP-",
				"/SILENT",
				"/NORESTART",
				"/GLASSDESKUPDATE=1",
				"/DIR=" + directory,
				"/LOG=" + Path.Combine(Path.GetDirectoryName(installer), "install.log")
			}
		};
	}
}
