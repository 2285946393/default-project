using System;
using System.Collections.Generic;
using RandomForeseer.RandomForeseerCode.Data.Migrations;
using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace RandomForeseer.RandomForeseerCode.Data;

internal static class ModData
{
	public const string SettingsKey = "settings";

	public const string SettingsFileName = "settings.json";

	private static readonly ModDataStore Store = ModDataStore.For("RandomForeseer");

	private static bool _isRegistered;

	public static ModSettings Settings => Store.Get<ModSettings>("settings");

	public static void Register()
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		if (_isRegistered)
		{
			return;
		}
		using (RitsuLibFramework.BeginModDataRegistration("RandomForeseer", true))
		{
			ModDataStore store = Store;
			Func<ModSettings> obj = () => new ModSettings();
			ModDataMigrationConfig val = new ModDataMigrationConfig();
			val.set_CurrentDataVersion(3);
			val.set_MinimumSupportedDataVersion(0);
			store.Register<ModSettings>("settings", "settings.json", (SaveScope)0, obj, true, val, (IEnumerable<IMigration>)new _003C_003Ez__ReadOnlyArray<IMigration>((IMigration[])(object)new IMigration[3]
			{
				new ModSettingsV0ToV1Migration(),
				new ModSettingsV1ToV2Migration(),
				new ModSettingsV2ToV3Migration()
			}));
		}
		_isRegistered = true;
	}
}
