using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace ModVersionLoader;

internal static class GameVersion
{
	private static readonly Lazy<Version?> Lazy = new Lazy<Version>(DetectCore);

	internal static Version? Detect()
	{
		return Lazy.Value;
	}

	private static Version? DetectCore()
	{
		Version version = FromReleaseInfo();
		if (version != null)
		{
			return version;
		}
		Version version2 = FromGameApi();
		if (version2 != null)
		{
			return version2;
		}
		VersionLoader.Log("[Loader] could not determine game version; falling back to newest implementation.");
		return null;
	}

	private static Version? FromReleaseInfo()
	{
		try
		{
			string directoryName = Path.GetDirectoryName(Environment.ProcessPath);
			if (string.IsNullOrEmpty(directoryName))
			{
				return null;
			}
			string path = Path.Combine(directoryName, "release_info.json");
			if (!File.Exists(path))
			{
				return null;
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(path));
			if (!jsonDocument.RootElement.TryGetProperty("version", out var value))
			{
				return null;
			}
			return Parse(value.GetString());
		}
		catch (Exception ex)
		{
			VersionLoader.LogError("[Loader] reading release_info.json failed: " + ex.Message);
			return null;
		}
	}

	private static Version? FromGameApi()
	{
		try
		{
			if ((Type.GetType("MegaCrit.Sts2.Core.Nodes.NGame, sts2", throwOnError: false)?.GetMethod("GetGameVersion", BindingFlags.Static | BindingFlags.Public))?.Invoke(null, null) is string text)
			{
				return Parse(text);
			}
		}
		catch
		{
		}
		return null;
	}

	internal static Version? Parse(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		string text2 = text.Trim();
		int num = text2.IndexOf('-');
		if (num >= 0)
		{
			text2 = text2.Substring(0, num);
		}
		int num2 = text2.IndexOf('+');
		if (num2 >= 0)
		{
			text2 = text2.Substring(0, num2);
		}
		text2 = text2.Trim();
		if (text2.Length >= 2 && (text2[0] == 'v' || text2[0] == 'V') && char.IsDigit(text2[1]))
		{
			text2 = text2.Substring(1);
		}
		if (!Version.TryParse(text2, out Version result))
		{
			return null;
		}
		return new Version(result.Major, result.Minor, (result.Build >= 0) ? result.Build : 0, (result.Revision >= 0) ? result.Revision : 0);
	}
}
