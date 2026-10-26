using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Godot;
using HarmonyLib;

namespace LoadOrderManager;

internal static class I18n
{
	private static readonly Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

	private static readonly object LockObj = new object();

	private static bool _loaded;

	public static string T(string key)
	{
		EnsureLoaded();
		string key2 = ResolveLanguageTag(GetClientLanguageCode() ?? "en");
		if (Tables.TryGetValue(key2, out Dictionary<string, string> value) && value.TryGetValue(key, out var value2))
		{
			return value2;
		}
		if (Tables.TryGetValue("en", out Dictionary<string, string> value3) && value3.TryGetValue(key, out var value4))
		{
			return value4;
		}
		return key;
	}

	public static string Tf(string key, params object[] args)
	{
		return string.Format(CultureInfo.InvariantCulture, T(key), args);
	}

	private static void EnsureLoaded()
	{
		if (_loaded)
		{
			return;
		}
		lock (LockObj)
		{
			if (_loaded)
			{
				return;
			}
			string i18nDirectory = GetI18nDirectory();
			if (!Directory.Exists(i18nDirectory))
			{
				DebugLog.Error("i18n directory not found: " + i18nDirectory);
				_loaded = true;
				return;
			}
			string[] files = Directory.GetFiles(i18nDirectory, "*.lang", SearchOption.TopDirectoryOnly);
			foreach (string path in files)
			{
				string text = NormalizeTag(Path.GetFileNameWithoutExtension(path));
				if (!string.IsNullOrWhiteSpace(text))
				{
					Dictionary<string, string> dictionary = ParseLangFile(path);
					if (dictionary.Count != 0)
					{
						Tables[text] = dictionary;
					}
				}
			}
			if (!Tables.ContainsKey("en"))
			{
				DebugLog.Warn("Missing en.lang; fallback texts may be unavailable.");
			}
			DebugLog.Info("i18n loaded from: " + i18nDirectory + ", languages: " + string.Join(", ", Tables.Keys));
			_loaded = true;
		}
	}

	private static string GetI18nDirectory()
	{
		List<string> list = new List<string>();
		try
		{
			string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			if (!string.IsNullOrWhiteSpace(directoryName))
			{
				list.Add(Path.Combine(directoryName, "i18n"));
				string text = directoryName;
				for (int i = 0; i < 2; i++)
				{
					string directoryName2 = Path.GetDirectoryName(text);
					if (!string.IsNullOrWhiteSpace(directoryName2))
					{
						text = directoryName2;
						list.Add(Path.Combine(text, "i18n"));
						continue;
					}
					break;
				}
			}
		}
		catch
		{
		}
		try
		{
			string directoryName3 = Path.GetDirectoryName(OS.GetExecutablePath());
			if (!string.IsNullOrWhiteSpace(directoryName3))
			{
				list.Add(Path.Combine(directoryName3, "mods", "LoadOrderManager", "i18n"));
			}
		}
		catch
		{
		}
		try
		{
			string userDataDir = OS.GetUserDataDir();
			if (!string.IsNullOrWhiteSpace(userDataDir))
			{
				list.Add(Path.Combine(userDataDir, "mods", "LoadOrderManager", "i18n"));
			}
		}
		catch
		{
		}
		foreach (string item in list)
		{
			if (Directory.Exists(item))
			{
				return item;
			}
		}
		if (list.Count <= 0)
		{
			return "i18n";
		}
		return list[0];
	}

	private static Dictionary<string, string> ParseLangFile(string path)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (string item in File.ReadLines(path, Encoding.UTF8))
		{
			string text = item.Trim();
			if (text.Length == 0 || text.StartsWith("#", StringComparison.Ordinal) || text.StartsWith(";", StringComparison.Ordinal))
			{
				continue;
			}
			int num = text.IndexOf('=');
			if (num > 0)
			{
				string text2 = text.Substring(0, num).Trim();
				string text3 = text;
				int num2 = num + 1;
				string text4 = text3.Substring(num2, text3.Length - num2).Trim();
				if (text2.Length != 0)
				{
					dictionary[text2] = text4.Replace("\\n", "\n", StringComparison.Ordinal);
				}
			}
		}
		return dictionary;
	}

	private static string ResolveLanguageTag(string code)
	{
		string text = NormalizeTag(code);
		if (!text.StartsWith("zhs", StringComparison.Ordinal) && !text.Contains("hans", StringComparison.Ordinal))
		{
			switch (text)
			{
			case "zh-cn":
			case "zh-sg":
			case "zh":
				break;
			default:
				if (!text.StartsWith("zht", StringComparison.Ordinal) && !text.Contains("hant", StringComparison.Ordinal))
				{
					switch (text)
					{
					case "zh-tw":
					case "zh-hk":
					case "zh-mo":
						break;
					default:
						if (text.StartsWith("en", StringComparison.Ordinal))
						{
							return "en";
						}
						if (text.StartsWith("ko", StringComparison.Ordinal))
						{
							return "ko";
						}
						if (text.StartsWith("de", StringComparison.Ordinal))
						{
							return "de";
						}
						if (text.StartsWith("ja", StringComparison.Ordinal))
						{
							return "ja";
						}
						if (text.StartsWith("fr", StringComparison.Ordinal))
						{
							return "fr";
						}
						if (text.StartsWith("ru", StringComparison.Ordinal))
						{
							return "ru";
						}
						if (text.StartsWith("pl", StringComparison.Ordinal))
						{
							return "pl";
						}
						if (text.StartsWith("tr", StringComparison.Ordinal))
						{
							return "tr";
						}
						if (text.StartsWith("it", StringComparison.Ordinal))
						{
							return "it";
						}
						if (text.StartsWith("pt", StringComparison.Ordinal))
						{
							return "pt-br";
						}
						if (text == "es-419" || text.Contains("latam", StringComparison.Ordinal))
						{
							return "es-419";
						}
						if (text.StartsWith("es-", StringComparison.Ordinal))
						{
							string text2 = text;
							if (IsLatamRegion(text2.Substring(3, text2.Length - 3)))
							{
								return "es-419";
							}
							return "es-es";
						}
						if (text == "es")
						{
							return "es-es";
						}
						return "en";
					}
				}
				return "zht";
			}
		}
		return "zhs";
	}

	private static bool IsLatamRegion(string region)
	{
		switch (region)
		{
		case "ar":
		case "bo":
		case "cl":
		case "co":
		case "cr":
		case "cu":
		case "do":
		case "ec":
		case "sv":
		case "gt":
		case "hn":
		case "mx":
		case "ni":
		case "pa":
		case "py":
		case "pe":
		case "pr":
		case "uy":
		case "ve":
			return true;
		default:
			return false;
		}
	}

	private static string NormalizeTag(string tag)
	{
		return tag.Trim().Replace('_', '-').ToLowerInvariant();
	}

	private static string? GetClientLanguageCode()
	{
		try
		{
			if (GetMemberValue(GetMemberValue(AccessTools.Property(AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.SaveManager"), "Instance")?.GetValue(null), "SettingsSave"), "Language") is string text && !string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
		}
		catch
		{
		}
		try
		{
			return TranslationServer.GetLocale();
		}
		catch
		{
			return null;
		}
	}

	private static object? GetMemberValue(object? obj, string name)
	{
		if (obj == null)
		{
			return null;
		}
		Type type = obj.GetType();
		PropertyInfo propertyInfo = AccessTools.Property(type, name);
		if (propertyInfo != null)
		{
			return propertyInfo.GetValue(obj);
		}
		return AccessTools.Field(type, name)?.GetValue(obj);
	}
}
