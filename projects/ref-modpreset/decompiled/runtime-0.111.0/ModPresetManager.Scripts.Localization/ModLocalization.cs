using MegaCrit.Sts2.Core.Localization;

namespace ModPresetManager.Scripts.Localization;

internal static class ModLocalization
{
	private const string Table = "main_menu_ui";

	private const string Prefix = "MOD_PRESET_MANAGER.";

	public static string Text(string key, params (string Name, string Value)[] variables)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		LocString val = new LocString("main_menu_ui", "MOD_PRESET_MANAGER." + key);
		for (int i = 0; i < variables.Length; i++)
		{
			var (text, text2) = variables[i];
			val.Add(text, text2);
		}
		return val.GetFormattedText();
	}
}
