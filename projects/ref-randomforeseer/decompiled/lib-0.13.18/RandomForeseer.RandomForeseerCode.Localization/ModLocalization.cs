using System.Collections.Generic;
using System.Reflection;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib;
using STS2RitsuLib.Utils;

namespace RandomForeseer.RandomForeseerCode.Localization;

internal static class ModLocalization
{
	private static readonly string UiLocTableId = RitsuLibFramework.GetI18NLocTableId("RandomForeseer", "DEFAULT");

	public static I18N UiLocalization { get; } = CreateI18N("ui");


	public static I18N SettingsLocalization { get; } = CreateI18N("settings");


	public static void Register()
	{
		RitsuLibFramework.RegisterI18NLocTableBridge("RandomForeseer", UiLocalization, "DEFAULT", false);
	}

	public static LocString Text(string key)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		return new LocString(UiLocTableId, key);
	}

	private static I18N CreateI18N(string tableId)
	{
		return RitsuLibFramework.CreateModLocalization("RandomForeseer", tableId, (IEnumerable<string>)null, (IEnumerable<string>)null, (IEnumerable<string>)new _003C_003Ez__ReadOnlySingleElementList<string>("res://RandomForeseer/localization/" + tableId), Assembly.GetExecutingAssembly());
	}
}
