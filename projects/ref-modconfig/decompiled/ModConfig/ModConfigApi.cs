using System.Collections.Generic;

namespace ModConfig;

public static class ModConfigApi
{
	public static void Register(string modId, string displayName, ConfigEntry[] entries)
	{
		ModConfigManager.Register(new ModRegistration
		{
			ModId = modId,
			DisplayName = displayName,
			Entries = entries
		});
	}

	public static void Register(string modId, string displayName, Dictionary<string, string> displayNames, ConfigEntry[] entries)
	{
		ModConfigManager.Register(new ModRegistration
		{
			ModId = modId,
			DisplayName = displayName,
			DisplayNames = displayNames,
			Entries = entries
		});
	}

	public static T GetValue<T>(string modId, string key)
	{
		return ModConfigManager.GetValue<T>(modId, key);
	}

	public static void SetValue(string modId, string key, object value)
	{
		ModConfigManager.SetValue(modId, key, value);
	}
}
