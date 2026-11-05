using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using System.Threading;
using System.Threading.Tasks;

namespace GlassDesk.AppServices.Updates;

public sealed class UpdateClient
{
	public const long MaximumInstallerSize = 536870912L;

	private const int MaximumMetadataSize = 524288;

	private readonly HttpClient _http;

	private readonly Uri _source;

	public UpdateClient(HttpClient http, Uri source)
	{
		ArgumentNullException.ThrowIfNull(http, "http");
		Repository(source);
		_http = http;
		_source = source;
	}

	public async Task<UpdateNotice?> CheckAsync(Version current, CancellationToken token)
	{
		return ParseNotice(await ReadMetadataAsync(token).ConfigureAwait(continueOnCapturedContext: false), current);
	}

	public async Task<UpdateRelease?> CheckInstallerAsync(Version current, CancellationToken token)
	{
		return ParseRelease(await ReadMetadataAsync(token).ConfigureAwait(continueOnCapturedContext: false), current, _source);
	}

	private async Task<string> ReadMetadataAsync(CancellationToken token)
	{
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
		timeout.CancelAfter(TimeSpan.FromSeconds(20L));
		using HttpRequestMessage request = Request(_source, metadata: true);
		using HttpResponseMessage response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
		response.EnsureSuccessStatusCode();
		if (response.Content.Headers.ContentLength > 524288)
		{
			throw new InvalidDataException("更新信息过大。");
		}
		string result;
		await using (Stream input = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(continueOnCapturedContext: false))
		{
			using MemoryStream output = new MemoryStream();
			byte[] buffer = new byte[16384];
			int num;
			while ((num = await input.ReadAsync(buffer, timeout.Token).ConfigureAwait(continueOnCapturedContext: false)) != 0)
			{
				if (output.Length + num > 524288)
				{
					throw new InvalidDataException("更新信息过大。");
				}
				output.Write(buffer, 0, num);
			}
			result = Encoding.UTF8.GetString(output.ToArray());
		}
		return result;
	}

	private static UpdateNotice? ParseNotice(string json, Version current)
	{
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.GetProperty("draft").GetBoolean() || rootElement.GetProperty("prerelease").GetBoolean())
			{
				return null;
			}
			string text = rootElement.GetProperty("tag_name").GetString() ?? string.Empty;
			string input = (text.StartsWith('v') ? text.Substring(1) : text);
			if (!StableVersionPattern().IsMatch(input) || !Version.TryParse(input, out Version result))
			{
				throw new InvalidDataException("更新版本号无效。仅支持正式版本。");
			}
			Version version = new Version(current.Major, current.Minor, Math.Max(0, current.Build));
			if (result <= version)
			{
				return null;
			}
			string text2 = (rootElement.TryGetProperty("body", out var value) ? (value.GetString() ?? string.Empty) : string.Empty);
			return new UpdateNotice(result, (text2.Length > 12000) ? text2.Substring(0, 12000) : text2);
		}
		catch (Exception ex) when ((ex is JsonException || ex is KeyNotFoundException || ex is InvalidOperationException || ex is FormatException || ex is OverflowException) ? true : false)
		{
			throw new InvalidDataException("更新信息格式无效。", ex);
		}
	}

	public static UpdateRelease? ParseRelease(string json, Version current, Uri source)
	{
		string text = Repository(source);
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(json);
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.GetProperty("draft").GetBoolean() || rootElement.GetProperty("prerelease").GetBoolean())
			{
				return null;
			}
			string text2 = rootElement.GetProperty("tag_name").GetString() ?? string.Empty;
			string input = (text2.StartsWith('v') ? text2.Substring(1) : text2);
			if (!StableVersionPattern().IsMatch(input) || !Version.TryParse(input, out Version result))
			{
				throw new InvalidDataException("更新版本号无效。仅支持正式版本。");
			}
			Version version = new Version(current.Major, current.Minor, Math.Max(0, current.Build));
			if (result <= version)
			{
				return null;
			}
			string fileName = $"GlassDesk-{result}-win-x64-Setup.exe";
			JsonElement[] array = (from asset in rootElement.GetProperty("assets").EnumerateArray()
				where asset.GetProperty("name").GetString() == fileName
				select asset).ToArray();
			if (array.Length != 1)
			{
				throw new InvalidDataException("此版本没有唯一的 Windows x64 安装包。");
			}
			JsonElement jsonElement = array[0];
			long @int = jsonElement.GetProperty("size").GetInt64();
			if (@int <= 0 || @int > 536870912 || jsonElement.GetProperty("state").GetString() != "uploaded")
			{
				throw new InvalidDataException("更新安装包尚未就绪或大小无效。");
			}
			string text3 = jsonElement.GetProperty("digest").GetString() ?? string.Empty;
			if (!DigestPattern().IsMatch(text3))
			{
				throw new InvalidDataException("更新安装包缺少 SHA-256 校验信息。");
			}
			string text4 = jsonElement.GetProperty("browser_download_url").GetString();
			string text5 = $"https://github.com/{text}/releases/download/{text2}/{fileName}";
			string text6 = rootElement.GetProperty("html_url").GetString();
			if (text4 != text5 || text6 != "https://github.com/" + text + "/releases/tag/" + text2)
			{
				throw new InvalidDataException("更新地址不属于配置的发布仓库。");
			}
			string text7 = (rootElement.TryGetProperty("body", out var value) ? (value.GetString() ?? string.Empty) : string.Empty);
			return new UpdateRelease(result, fileName, new Uri(text4), text3.Substring(7).ToLowerInvariant(), @int, (text7.Length > 12000) ? text7.Substring(0, 12000) : text7, new Uri(text6));
		}
		catch (Exception ex) when ((ex is JsonException || ex is KeyNotFoundException || ex is InvalidOperationException || ex is FormatException || ex is OverflowException) ? true : false)
		{
			throw new InvalidDataException("更新信息格式无效。", ex);
		}
	}

	public async Task<string> DownloadAsync(UpdateRelease release, string cacheRoot, IProgress<int>? progress, CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(release, "release");
		string value = Repository(_source);
		if (release.DownloadUrl.AbsoluteUri != $"https://github.com/{value}/releases/download/v{release.Version}/{release.FileName}" && release.DownloadUrl.AbsoluteUri != $"https://github.com/{value}/releases/download/{release.Version}/{release.FileName}")
		{
			throw new InvalidDataException("更新下载地址无效。");
		}
		ValidateInstaller(release);
		string directory = Path.Combine(Path.GetFullPath(cacheRoot), Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		string partial = Path.Combine(directory, "installer.partial");
		string final = Path.Combine(directory, release.FileName);
		try
		{
			using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
			timeout.CancelAfter(TimeSpan.FromMinutes(15L));
			using HttpRequestMessage request = Request(release.DownloadUrl, metadata: false);
			using HttpResponseMessage response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
			response.EnsureSuccessStatusCode();
			Uri uri = response.RequestMessage?.RequestUri;
			if ((object)uri != null && !(uri.Scheme == "https"))
			{
				throw new InvalidDataException("更新下载被重定向到不安全的地址。");
			}
			long? contentLength = response.Content.Headers.ContentLength;
			if (contentLength.HasValue)
			{
				long valueOrDefault = contentLength.GetValueOrDefault();
				if (valueOrDefault != release.Size)
				{
					throw new InvalidDataException("安装包大小与更新信息不一致。");
				}
			}
			string result;
			await using (Stream input = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(continueOnCapturedContext: false))
			{
				using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
				long received = 0L;
				int lastProgress = -1;
				await using (FileStream output = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, useAsync: true))
				{
					byte[] buffer = new byte[65536];
					progress?.Report(0);
					int num;
					while ((num = await input.ReadAsync(buffer, timeout.Token).ConfigureAwait(continueOnCapturedContext: false)) != 0)
					{
						received += num;
						if (received > release.Size)
						{
							throw new InvalidDataException("安装包超出声明大小。");
						}
						hash.AppendData(buffer, 0, num);
						await output.WriteAsync(buffer.AsMemory(0, num), timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
						int num2 = (int)Math.Min(99L, received * 100 / release.Size);
						if (num2 != lastProgress)
						{
							progress?.Report(num2);
							lastProgress = num2;
						}
					}
					timeout.Token.ThrowIfCancellationRequested();
					if (received != release.Size || !Convert.ToHexString(hash.GetHashAndReset()).Equals(release.Sha256, StringComparison.OrdinalIgnoreCase))
					{
						throw new InvalidDataException("安装包校验失败，请重试下载。");
					}
					await output.FlushAsync(timeout.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
				timeout.Token.ThrowIfCancellationRequested();
				File.Move(partial, final);
				progress?.Report(100);
				result = final;
			}
			return result;
		}
		catch
		{
			try
			{
				Directory.Delete(directory, recursive: true);
			}
			catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) ? true : false)
			{
			}
			throw;
		}
	}

	internal static void ValidateInstaller(UpdateRelease release)
	{
		if (!StableVersionPattern().IsMatch(release.Version.ToString()) || release.FileName != $"GlassDesk-{release.Version}-win-x64-Setup.exe" || !DigestPattern().IsMatch("sha256:" + release.Sha256) || release.Size <= 0 || release.Size > 536870912)
		{
			throw new InvalidDataException("安装包信息无效。");
		}
	}

	private static HttpRequestMessage Request(Uri url, bool metadata)
	{
		HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
		httpRequestMessage.Headers.UserAgent.ParseAdd("GlassDesk-Updater/1.0");
		if (metadata)
		{
			httpRequestMessage.Headers.Accept.ParseAdd("application/vnd.github+json");
		}
		return httpRequestMessage;
	}

	private static string Repository(Uri source)
	{
		if (!source.IsAbsoluteUri || source.Scheme != "https" || source.Host != "api.github.com" || !source.IsDefaultPort || source.UserInfo.Length != 0 || source.Query.Length != 0 || source.Fragment.Length != 0 || !SourcePattern().IsMatch(source.AbsolutePath))
		{
			throw new InvalidDataException("更新源必须是 GitHub 的公开仓库 Releases 接口。");
		}
		return string.Join('/', source.AbsolutePath.Split('/').Skip(2).Take(2));
	}

	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.42308")]
	private static Regex StableVersionPattern()
	{
		return _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__StableVersionPattern_0.Instance;
	}

	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.42308")]
	private static Regex DigestPattern()
	{
		return _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__DigestPattern_1.Instance;
	}

	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.42308")]
	private static Regex SourcePattern()
	{
		return _003CRegexGenerator_g_003EF412E83EB17AB8B8453D5A4FB139FD7212E89E679FCFCBC3B8099D905681DAA13__SourcePattern_2.Instance;
	}
}
