using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace GlassDesk.Platform.Shell;

public static class LaunchTargetIdentity
{
	public static string? FromShellTarget(string? target)
	{
		if (string.IsNullOrWhiteSpace(target))
		{
			return null;
		}
		string text = target.Trim();
		string[] array = new string[2] { "shell:AppsFolder\\", "::{4234d49b-0245-4df3-b780-3893943456e1}\\" };
		foreach (string text2 in array)
		{
			if (text.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring(text2.Length);
			}
		}
		string[] array2 = text.Split('!');
		if (array2.Length != 2 || !array2[0].Contains('_') || !array2.All((string part) => part.Length > 0) || !text.All((char c) =>
		{
			bool flag = char.IsAsciiLetterOrDigit(c);
			if (!flag)
			{
				bool flag2;
				switch (c)
				{
				case '!':
				case '-':
				case '.':
				case '_':
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			return flag;
		}))
		{
			return null;
		}
		return text;
	}

	public static string? ReadManagedShellLauncher(string path)
	{
		try
		{
			if (!path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || new FileInfo(path).Length > 1048576)
			{
				return null;
			}
			using FileStream peStream = File.OpenRead(path);
			using PEReader pEReader = new PEReader((Stream)peStream);
			if (!pEReader.HasMetadata)
			{
				return null;
			}
			MetadataReader metadataReader = pEReader.GetMetadataReader();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			bool flag = false;
			UserStringHandle handle = MetadataTokens.UserStringHandle(0);
			while (true)
			{
				UserStringHandle userStringHandle = (handle = metadataReader.GetNextHandle(handle));
				if (userStringHandle.IsNil)
				{
					break;
				}
				string userString = metadataReader.GetUserString(handle);
				flag |= userString.Equals("explorer.exe", StringComparison.OrdinalIgnoreCase);
				if (userString.StartsWith("shell:AppsFolder\\", StringComparison.OrdinalIgnoreCase))
				{
					string text = FromShellTarget(userString);
					if (text != null)
					{
						hashSet.Add(text);
					}
				}
			}
			return (flag && hashSet.Count == 1) ? hashSet.Single() : null;
		}
		catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException || ex is BadImageFormatException || ex is ArgumentException || ex is InvalidOperationException) ? true : false)
		{
			return null;
		}
	}
}
