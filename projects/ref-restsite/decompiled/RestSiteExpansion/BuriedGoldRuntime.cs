using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Data;
using STS2RitsuLib.RunData;
using STS2RitsuLib.Utils.Persistence;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace RestSiteExpansion;

public static class BuriedGoldRuntime
{
	private const string SaveKey = "buried_gold_save";

	internal const string SlotKey = "buried_gold";

	private static PlayerRunSavedData<BuriedGoldState>? _slot;

	internal static PlayerRunSavedData<BuriedGoldState>? Slot => _slot;

	public static void Register()
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected O, but got Unknown
		ModDataStore.For("RestSiteExpansion").Register<BuriedGoldSave>("buried_gold_save", "buried_gold.json", (SaveScope)0, (Func<BuriedGoldSave>)(() => new BuriedGoldSave()), true, (ModDataMigrationConfig)null, (IEnumerable<IMigration>)null);
		RunSavedDataStore obj = RunSavedDataStore.For("RestSiteExpansion");
		Func<BuriedGoldState> obj2 = () => new BuriedGoldState();
		RunSavedDataOptions val = new RunSavedDataOptions();
		val.set_WritePolicy((RunSavedDataWritePolicy)1);
		val.set_SyncLobbyOnChange(true);
		_slot = obj.RegisterPerPlayer<BuriedGoldState>("buried_gold", obj2, val);
	}

	public static int ReadPersistentAmount()
	{
		return ModDataStore.For("RestSiteExpansion").Get<BuriedGoldSave>("buried_gold_save").Amount;
	}

	private static void WritePersistentAmount(int amount)
	{
		ModDataStore val = ModDataStore.For("RestSiteExpansion");
		val.Modify<BuriedGoldSave>("buried_gold_save", (Action<BuriedGoldSave>)delegate(BuriedGoldSave s)
		{
			s.Amount = Math.Max(0, amount);
		});
		val.Save("buried_gold_save");
	}

	public static int GetAmount(Player player)
	{
		if (_slot != null)
		{
			IRunState runState = player.RunState;
			RunState val = (RunState)(object)((runState is RunState) ? runState : null);
			BuriedGoldState buriedGoldState = default(BuriedGoldState);
			if (val != null && _slot.TryGet(val, player.NetId, ref buriedGoldState))
			{
				return buriedGoldState.Amount;
			}
		}
		return ReadPersistentAmount();
	}

	public static void SetAmount(Player player, int amount)
	{
		amount = Math.Max(0, amount);
		if (_slot != null)
		{
			IRunState runState = player.RunState;
			RunState val = (RunState)(object)((runState is RunState) ? runState : null);
			if (val != null)
			{
				_slot.Set(val, player.NetId, new BuriedGoldState
				{
					Amount = amount
				});
			}
		}
		if (LocalContext.IsMe(player))
		{
			WritePersistentAmount(amount);
		}
	}
}
