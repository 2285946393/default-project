using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal static class ModifyCardPlayResultLocationMirrors
{
	private static readonly MirrorMethodSpec ModifyCardPlayResultLocation = MirrorMethodSpec.Hook("ModifyCardPlayResultLocation", new Type[4]
	{
		typeof(CardModel),
		typeof(bool),
		typeof(ResourceInfo),
		typeof(CardLocation)
	});

	private static readonly MirrorMethodSpec AfterModifyingCardPlayResultLocation = MirrorMethodSpec.Hook("AfterModifyingCardPlayResultLocation", new Type[2]
	{
		typeof(CardModel),
		typeof(CardLocation)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyCardPlayResultLocationMirrorContext, CardLocation> Registry = CreateRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, AfterModifyingCardPlayResultLocationMirrorContext> AfterRegistry = CreateAfterRegistry();

	public static CardLocation Invoke(AbstractModel listener, ModifyCardPlayResultLocationMirrorContext context)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if (!Registry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return InvokeOriginal(listener, context);
		}
		return dispatchResult.Value;
	}

	public static void InvokeAfter(AbstractModel modifier, AfterModifyingCardPlayResultLocationMirrorContext context)
	{
		AfterRegistry.Invoke(modifier, context);
	}

	private static CardLocation InvokeOriginal(AbstractModel listener, ModifyCardPlayResultLocationMirrorContext context)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		return listener.ModifyCardPlayResultLocation(context.Card.Preview, context.IsAutoPlay, context.Resources, context.Location);
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyCardPlayResultLocationMirrorContext, CardLocation> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyCardPlayResultLocationMirrorContext, CardLocation> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyCardPlayResultLocationMirrorContext, CardLocation>(ModifyCardPlayResultLocation);
		methodMirrorRegistry.Register<FeralPower>((Func<FeralPower, ModifyCardPlayResultLocationMirrorContext, CardLocation>)HandleFeralPower);
		methodMirrorRegistry.Register<NostalgiaPower>((Func<NostalgiaPower, ModifyCardPlayResultLocationMirrorContext, CardLocation>)HandleNostalgiaPower);
		methodMirrorRegistry.Register<ReboundPower>((Func<ReboundPower, ModifyCardPlayResultLocationMirrorContext, CardLocation>)HandleReboundPower);
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<AbstractModel, AfterModifyingCardPlayResultLocationMirrorContext> CreateAfterRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterModifyingCardPlayResultLocationMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterModifyingCardPlayResultLocationMirrorContext>(AfterModifyingCardPlayResultLocation);
		methodMirrorRegistry.RegisterIgnored<CorruptionPower>();
		methodMirrorRegistry.Register<FeralPower>((Action<FeralPower, AfterModifyingCardPlayResultLocationMirrorContext>)HandleFeralPowerAfter);
		methodMirrorRegistry.RegisterIgnored<NostalgiaPower>();
		methodMirrorRegistry.Register<ReboundPower>((Action<ReboundPower, AfterModifyingCardPlayResultLocationMirrorContext>)HandleReboundPowerAfter);
		return methodMirrorRegistry;
	}

	private static CardLocation HandleFeralPower(FeralPower power, ModifyCardPlayResultLocationMirrorContext context)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Invalid comparison between Unknown and I4
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		FeralPower power2 = power;
		CardModel preview = context.Card.Preview;
		FeralPredictionState feralPredictionState = context.StateStore.Get((AbstractModel)(object)power2, () => new FeralPredictionState(power2));
		if (preview.Owner.Creature == ((PowerModel)power2).Owner && (int)preview.Type == 1)
		{
			ResourceInfo resources = context.Resources;
			if (((ResourceInfo)(ref resources)).EnergyValue <= 0 && !preview.IsDupe && feralPredictionState.ZeroCostAttacksPlayed < ((PowerModel)power2).Amount)
			{
				CardLocation location = context.Location;
				location.pileType = (PileType)2;
				location.position = (CardPilePosition)2;
				location.player = preview.Owner;
				return location;
			}
		}
		return context.Location;
	}

	private static CardLocation HandleNostalgiaPower(NostalgiaPower power, ModifyCardPlayResultLocationMirrorContext context)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Invalid comparison between Unknown and I4
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Invalid comparison between Unknown and I4
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		NostalgiaPower power2 = power;
		CardModel preview = context.Card.Preview;
		bool flag = preview.Owner.Creature != ((PowerModel)power2).Owner;
		if (!flag)
		{
			CardType type = preview.Type;
			bool flag2 = type - 1 <= 1;
			flag = !flag2;
		}
		if (flag || (int)context.Location.pileType != 3)
		{
			return context.Location;
		}
		if (CombatManager.Instance.History.CardPlaysStarted.Count(delegate(CardPlayStartedEntry entry)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Invalid comparison between Unknown and I4
			bool flag3 = ((CombatHistoryEntry)entry).HappenedThisTurn(((PowerModel)power2).CombatState);
			if (flag3)
			{
				CardType type3 = entry.CardPlay.Card.Type;
				bool flag4 = type3 - 1 <= 1;
				flag3 = flag4;
			}
			return flag3 && entry.CardPlay.Player == ((PowerModel)power2).Owner.Player;
		}) + context.History.OfType<CombatPredictionCardPlayStartedEntry>().Count(delegate(CombatPredictionCardPlayStartedEntry entry)
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Invalid comparison between Unknown and I4
			CardType type2 = entry.CardPlay.Card.Type;
			return type2 - 1 <= 1 && entry.CardPlay.Player == ((PowerModel)power2).Owner.Player;
		}) >= ((PowerModel)power2).Amount)
		{
			return context.Location;
		}
		CardLocation location = context.Location;
		location.pileType = (PileType)1;
		location.position = (CardPilePosition)2;
		return location;
	}

	private static CardLocation HandleReboundPower(ReboundPower power, ModifyCardPlayResultLocationMirrorContext context)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (!context.StateStore.GetPowerAmount((PowerModel)(object)power).IsActive)
		{
			return context.Location;
		}
		return InvokeOriginal((AbstractModel)(object)power, context);
	}

	private static void HandleFeralPowerAfter(FeralPower power, AfterModifyingCardPlayResultLocationMirrorContext context)
	{
		FeralPower power2 = power;
		context.StateStore.Get((AbstractModel)(object)power2, () => new FeralPredictionState(power2)).ZeroCostAttacksPlayed++;
	}

	private static void HandleReboundPowerAfter(ReboundPower power, AfterModifyingCardPlayResultLocationMirrorContext context)
	{
		context.StateStore.GetPowerAmount((PowerModel)(object)power).Decrement();
	}
}
