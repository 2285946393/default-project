using System;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Attack;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

internal static class ModifyDamageMirrors
{
	private static readonly MirrorMethodSpec ModifyDamageAdditive = MirrorMethodSpec.Hook("ModifyDamageAdditive", new Type[6]
	{
		typeof(Creature),
		typeof(decimal),
		typeof(ValueProp),
		typeof(Creature),
		typeof(CardModel),
		typeof(CardPlay)
	});

	private static readonly MirrorMethodSpec ModifyDamageMultiplicative = MirrorMethodSpec.Hook("ModifyDamageMultiplicative", new Type[6]
	{
		typeof(Creature),
		typeof(decimal),
		typeof(ValueProp),
		typeof(Creature),
		typeof(CardModel),
		typeof(CardPlay)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyDamageMirrorContext, decimal> AdditiveRegistry = CreateAdditiveRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyDamageMirrorContext, decimal> MultiplicativeRegistry = CreateMultiplicativeRegistry();

	public static decimal InvokeAdditive(AbstractModel listener, ModifyDamageMirrorContext context)
	{
		if (!AdditiveRegistry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return InvokeOriginalAdditive(listener, context);
		}
		return dispatchResult.Value;
	}

	public static decimal InvokeMultiplicative(AbstractModel listener, ModifyDamageMirrorContext context)
	{
		if (!MultiplicativeRegistry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return InvokeOriginalMultiplicative(listener, context);
		}
		return dispatchResult.Value;
	}

	private static decimal InvokeOriginalAdditive(AbstractModel listener, ModifyDamageMirrorContext context)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		return listener.ModifyDamageAdditive(context.Target, context.Amount, context.Props, context.Dealer, context.CardSource?.Preview, context.CardPlay);
	}

	private static decimal InvokeOriginalMultiplicative(AbstractModel listener, ModifyDamageMirrorContext context)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		return listener.ModifyDamageMultiplicative(context.Target, context.Amount, context.Props, context.Dealer, context.CardSource?.Preview, context.CardPlay);
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyDamageMirrorContext, decimal> CreateAdditiveRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyDamageMirrorContext, decimal> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyDamageMirrorContext, decimal>(ModifyDamageAdditive);
		methodMirrorRegistry.Register<OneForAllPower>((Func<OneForAllPower, ModifyDamageMirrorContext, decimal>)HandleOneForAllPower);
		methodMirrorRegistry.Register<PhantomBladesPower>((Func<PhantomBladesPower, ModifyDamageMirrorContext, decimal>)HandlePhantomBladesPower);
		methodMirrorRegistry.Register<VigorPower>((Func<VigorPower, ModifyDamageMirrorContext, decimal>)VigorPowerMirrors.ModifyDamageAdditive);
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyDamageMirrorContext, decimal> CreateMultiplicativeRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyDamageMirrorContext, decimal> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyDamageMirrorContext, decimal>(ModifyDamageMultiplicative);
		methodMirrorRegistry.Register<FlutterPower>((Func<FlutterPower, ModifyDamageMirrorContext, decimal>)HandleFlutterPower);
		methodMirrorRegistry.Register<GigantificationPower>((Func<GigantificationPower, ModifyDamageMirrorContext, decimal>)GigantificationPowerMirrors.ModifyDamageMultiplicative);
		methodMirrorRegistry.Register<LethalityPower>((Func<LethalityPower, ModifyDamageMirrorContext, decimal>)HandleLethalityPower);
		methodMirrorRegistry.Register<SlowPower>((Func<SlowPower, ModifyDamageMirrorContext, decimal>)HandleSlowPower);
		methodMirrorRegistry.Register<SurroundedPower>((Func<SurroundedPower, ModifyDamageMirrorContext, decimal>)HandleSurroundedPower);
		methodMirrorRegistry.Register<PenNib>((Func<PenNib, ModifyDamageMirrorContext, decimal>)HandlePenNib);
		return methodMirrorRegistry;
	}

	private static decimal HandleOneForAllPower(OneForAllPower power, ModifyDamageMirrorContext context)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		if (ValuePropExtensions.IsPoweredAttack(context.Props))
		{
			PredictedCard cardSource = context.CardSource;
			if (cardSource != null && cardSource.Preview.Owner.Creature == ((PowerModel)power).Owner)
			{
				CardPlay? cardPlay = context.CardPlay;
				if (!((cardPlay != null) ? cardPlay.Card.EnergyCost.CostsX : cardSource.Preview.EnergyCost.CostsX))
				{
					CardPlay cardPlay2 = context.CardPlay;
					int num;
					if (cardPlay2 == null)
					{
						num = cardSource.GetEnergyCostWithModifiers(context.Simulator);
					}
					else
					{
						ResourceInfo resources = cardPlay2.Resources;
						num = ((ResourceInfo)(ref resources)).EnergySpent;
					}
					return (num == 0) ? ((PowerModel)power).Amount : 0;
				}
			}
		}
		return 0m;
	}

	private static decimal HandlePhantomBladesPower(PhantomBladesPower power, ModifyDamageMirrorContext context)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		ModifyDamageMirrorContext context2 = context;
		PhantomBladesPower power2 = power;
		if (ValuePropExtensions.IsPoweredAttack(context2.Props))
		{
			PredictedCard? cardSource = context2.CardSource;
			if (cardSource != null && cardSource.Preview.Tags.Contains((CardTag)5) && context2.Dealer == ((PowerModel)power2).Owner)
			{
				return (!CombatManager.Instance.History.CardPlaysFinished.Any((CardPlayFinishedEntry entry) => ((CombatHistoryEntry)entry).HappenedThisTurn(context2.CombatState) && entry.CardPlay.Player == ((PowerModel)power2).Owner.Player && entry.CardPlay.Card.Tags.Contains((CardTag)5)) && !context2.History.OfType<CombatPredictionCardPlayFinishedEntry>().Any((CombatPredictionCardPlayFinishedEntry entry) => entry.CardPlay.Player == ((PowerModel)power2).Owner.Player && entry.Card.Preview.Tags.Contains((CardTag)5))) ? ((PowerModel)power2).Amount : 0;
			}
		}
		return 0m;
	}

	private static decimal HandleLethalityPower(LethalityPower power, ModifyDamageMirrorContext context)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Invalid comparison between Unknown and I4
		ModifyDamageMirrorContext context2 = context;
		LethalityPower power2 = power;
		PredictedCard cardSource = context2.CardSource;
		if (!ValuePropExtensions.IsPoweredAttack(context2.Props) || cardSource == null || cardSource.Preview.Owner.Creature != ((PowerModel)power2).Owner)
		{
			return 1m;
		}
		SimCardPile? pile = cardSource.GetPile(context2.State);
		bool flag = pile != null && (int)pile.Type == 5;
		if (flag && cardSource.Preview.CurrentPlayIndex > 0)
		{
			return 1m;
		}
		if (CombatManager.Instance.History.CardPlaysStarted.Count((CardPlayStartedEntry entry) => ((CombatHistoryEntry)entry).HappenedThisTurn(context2.CombatState) && entry.CardPlay.Player == ((PowerModel)power2).Owner.Player && (int)entry.CardPlay.Card.Type == 1) + context2.History.OfType<CombatPredictionCardPlayStartedEntry>().Count((CombatPredictionCardPlayStartedEntry entry) => entry.CardPlay.Player == ((PowerModel)power2).Owner.Player && (int)entry.Card.Preview.Type == 1) <= (flag ? 1 : 0))
		{
			return 1m + (decimal)((PowerModel)power2).Amount / 100m;
		}
		return 1m;
	}

	private static decimal HandleFlutterPower(FlutterPower power, ModifyDamageMirrorContext context)
	{
		if (!context.StateStore.GetPowerAmount((PowerModel)(object)power).IsActive)
		{
			return 1m;
		}
		return InvokeOriginalMultiplicative((AbstractModel)(object)power, context);
	}

	private static decimal HandleSlowPower(SlowPower power, ModifyDamageMirrorContext context)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		SlowPower power2 = power;
		if (context.Target != ((PowerModel)power2).Owner || !ValuePropExtensions.IsPoweredAttack(context.Props))
		{
			return 1m;
		}
		int value = context.StateStore.Get((AbstractModel)(object)power2, () => new CounterPredictionState(((PowerModel)power2).DynamicVars["SlowAmount"].IntValue)).Value;
		return 1m + 0.1m * (decimal)value;
	}

	private static decimal HandleSurroundedPower(SurroundedPower power, ModifyDamageMirrorContext context)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Invalid comparison between Unknown and I4
		SurroundedPower power2 = power;
		if (context.Dealer == null || context.Target != ((PowerModel)power2).Owner)
		{
			return 1m;
		}
		Direction facing = context.StateStore.Get((AbstractModel)(object)power2, () => new SurroundedPredictionState(power2)).Facing;
		if ((int)facing != 0)
		{
			if ((int)facing == 1 && context.Dealer.HasPower<BackAttackRightPower>())
			{
				return 1.5m;
			}
		}
		else if (context.Dealer.HasPower<BackAttackLeftPower>())
		{
			return 1.5m;
		}
		return 1m;
	}

	private static decimal HandlePenNib(PenNib relic, ModifyDamageMirrorContext context)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Invalid comparison between Unknown and I4
		PenNib relic2 = relic;
		if (!ValuePropExtensions.IsPoweredAttack(context.Props) || context.CardSource == null || (context.Dealer != ((RelicModel)relic2).Owner.Creature && context.Dealer != ((RelicModel)relic2).Owner.Osty))
		{
			return 1m;
		}
		PenNibPredictionState penNibPredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new PenNibPredictionState(relic2));
		if (penNibPredictionState.AttackToDouble != null)
		{
			return (penNibPredictionState.AttackToDouble != context.CardSource.Original) ? 1 : 2;
		}
		int num;
		if (context.CardPlay == null)
		{
			PileType? val = context.CardSource.GetPile(context.State)?.Type;
			if ((!val.HasValue || (int)val.GetValueOrDefault() != 5) && penNibPredictionState.AttacksPlayed == 9)
			{
				num = 2;
				goto IL_00ef;
			}
		}
		num = 1;
		goto IL_00ef;
		IL_00ef:
		return num;
	}
}
