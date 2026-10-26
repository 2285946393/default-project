using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Data;
using STS2RitsuLib.RunData;
using STS2RitsuLib.Utils.Persistence;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace RestSiteExpansion;

public static class BuriedCardRuntime
{
	private const string SaveKey = "buried_card_save";

	internal const string SlotKey = "buried_card";

	private static PlayerRunSavedData<BuriedCardState>? _slot;

	internal static PlayerRunSavedData<BuriedCardState>? Slot => _slot;

	public static void Register()
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected O, but got Unknown
		ModDataStore.For("RestSiteExpansion").Register<BuriedCardSave>("buried_card_save", "buried_card.json", (SaveScope)0, (Func<BuriedCardSave>)(() => new BuriedCardSave()), true, (ModDataMigrationConfig)null, (IEnumerable<IMigration>)null);
		RunSavedDataStore obj = RunSavedDataStore.For("RestSiteExpansion");
		Func<BuriedCardState> obj2 = () => new BuriedCardState();
		RunSavedDataOptions val = new RunSavedDataOptions();
		val.set_WritePolicy((RunSavedDataWritePolicy)0);
		val.set_SyncLobbyOnChange(true);
		_slot = obj.RegisterPerPlayer<BuriedCardState>("buried_card", obj2, val);
	}

	public static SerializableCard? ReadPersistentCard()
	{
		return ModDataStore.For("RestSiteExpansion").Get<BuriedCardSave>("buried_card_save").Card;
	}

	internal static SerializableCard? EnsureDefaultCard()
	{
		ModDataStore val = ModDataStore.For("RestSiteExpansion");
		BuriedCardSave buriedCardSave = val.Get<BuriedCardSave>("buried_card_save");
		if (buriedCardSave.Card != null || buriedCardSave.DefaultGranted)
		{
			return buriedCardSave.Card;
		}
		SerializableCard card = CreateDefaultCard();
		if (card == null)
		{
			return null;
		}
		val.Modify<BuriedCardSave>("buried_card_save", (Action<BuriedCardSave>)delegate(BuriedCardSave s)
		{
			s.Card = card;
			s.DefaultGranted = true;
		});
		val.Save("buried_card_save");
		MainFile.Logger.Info("[BuriedCard] 已发放默认埋藏卡「" + CardDisplayName(card) + "」（首次，取走后不再补发）", 1);
		return card;
	}

	private static SerializableCard? CreateDefaultCard()
	{
		try
		{
			return ((CardModel)ModelDb.Card<IronWave>()).ToMutable().ToSerializable();
		}
		catch (Exception ex)
		{
			MainFile.Logger.Warn("[BuriedCard] 默认卡创建失败，埋藏位留空：" + ex.Message, 1);
			return null;
		}
	}

	internal static void WritePersistentCard(SerializableCard? card)
	{
		SerializableCard card2 = card;
		ModDataStore val = ModDataStore.For("RestSiteExpansion");
		val.Modify<BuriedCardSave>("buried_card_save", (Action<BuriedCardSave>)delegate(BuriedCardSave s)
		{
			s.Card = card2;
		});
		val.Save("buried_card_save");
	}

	public static SerializableCard? GetCard(Player player)
	{
		if (_slot != null)
		{
			IRunState runState = player.RunState;
			RunState val = (RunState)(object)((runState is RunState) ? runState : null);
			BuriedCardState buriedCardState = default(BuriedCardState);
			if (val != null && _slot.TryGet(val, player.NetId, ref buriedCardState))
			{
				return buriedCardState.Card;
			}
		}
		return ReadPersistentCard();
	}

	public static bool HasCard(Player player)
	{
		return GetCard(player) != null;
	}

	public static string CardDisplayName(SerializableCard? card)
	{
		if (card == null)
		{
			return "—";
		}
		try
		{
			return CardModel.FromSerializable(card).Title;
		}
		catch (Exception ex)
		{
			MainFile.Logger.Warn("[BuriedCard] 卡牌名解析失败，回落到条目 id：" + ex.Message, 1);
			ModelId id = card.Id;
			return ((id != null) ? id.Entry : null) ?? "—";
		}
	}

	public static void SetCard(Player player, SerializableCard? card)
	{
		if (_slot != null)
		{
			IRunState runState = player.RunState;
			RunState val = (RunState)(object)((runState is RunState) ? runState : null);
			if (val != null)
			{
				_slot.Set(val, player.NetId, new BuriedCardState
				{
					Card = card
				});
			}
		}
		if (LocalContext.IsMe(player))
		{
			WritePersistentCard(card);
		}
	}
}
