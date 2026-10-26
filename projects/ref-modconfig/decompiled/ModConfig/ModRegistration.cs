using System;
using System.Collections.Generic;

namespace ModConfig;

internal class ModRegistration
{
	public string ModId { get; set; } = "";


	public string DisplayName { get; set; } = "";


	public Dictionary<string, string>? DisplayNames { get; set; }

	public ConfigEntry[] Entries { get; set; } = Array.Empty<ConfigEntry>();


	internal string GetLocalizedName()
	{
		if (DisplayNames == null || DisplayNames.Count == 0)
		{
			return DisplayName;
		}
		string currentLang = I18n.CurrentLang;
		if (DisplayNames.TryGetValue(currentLang, out string value))
		{
			return value;
		}
		foreach (var (text3, result) in DisplayNames)
		{
			if (currentLang.StartsWith(text3) || text3.StartsWith(currentLang))
			{
				return result;
			}
		}
		return DisplayName;
	}
}
