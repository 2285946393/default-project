using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal static class ModifyEnergyCostInCombatMirrors
{
	private static readonly MirrorMethodSpec ModifyEnergyCostInCombat = MirrorMethodSpec.Hook("TryModifyEnergyCostInCombat", new Type[3]
	{
		typeof(CardModel),
		typeof(decimal),
		typeof(decimal).MakeByRefType()
	});

	private static readonly MirrorMethodSpec ModifyEnergyCostInCombatLate = MirrorMethodSpec.Hook("TryModifyEnergyCostInCombatLate", new Type[3]
	{
		typeof(CardModel),
		typeof(decimal),
		typeof(decimal).MakeByRefType()
	});

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyEnergyCostInCombatMirrorContext, decimal> Registry = new MethodMirrorRegistry<AbstractModel, ModifyEnergyCostInCombatMirrorContext, decimal>(ModifyEnergyCostInCombat);

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyEnergyCostInCombatMirrorContext, decimal> LateRegistry = CreateLateRegistry();

	public static decimal Invoke(AbstractModel listener, ModifyEnergyCostInCombatMirrorContext context)
	{
		if (Registry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return dispatchResult.Value;
		}
		decimal result = default(decimal);
		listener.TryModifyEnergyCostInCombat(context.Card.Preview, context.Cost, ref result);
		return result;
	}

	public static decimal InvokeLate(AbstractModel listener, ModifyEnergyCostInCombatMirrorContext context)
	{
		if (LateRegistry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return dispatchResult.Value;
		}
		decimal result = default(decimal);
		listener.TryModifyEnergyCostInCombatLate(context.Card.Preview, context.Cost, ref result);
		return result;
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyEnergyCostInCombatMirrorContext, decimal> CreateLateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyEnergyCostInCombatMirrorContext, decimal> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyEnergyCostInCombatMirrorContext, decimal>(ModifyEnergyCostInCombatLate);
		methodMirrorRegistry.Register<FreeAttackPower>((Func<FreeAttackPower, ModifyEnergyCostInCombatMirrorContext, decimal>)HandleFreeAttackPower);
		methodMirrorRegistry.Register<FreePowerPower>((Func<FreePowerPower, ModifyEnergyCostInCombatMirrorContext, decimal>)HandleFreePowerPower);
		methodMirrorRegistry.Register<FreeSkillPower>((Func<FreeSkillPower, ModifyEnergyCostInCombatMirrorContext, decimal>)HandleFreeSkillPower);
		methodMirrorRegistry.Register<VeilpiercerPower>((Func<VeilpiercerPower, ModifyEnergyCostInCombatMirrorContext, decimal>)HandleVeilpiercerPower);
		methodMirrorRegistry.Register<VoidFormPower>((Func<VoidFormPower, ModifyEnergyCostInCombatMirrorContext, decimal>)HandleVoidFormPower);
		methodMirrorRegistry.Register<BrilliantScarf>((Func<BrilliantScarf, ModifyEnergyCostInCombatMirrorContext, decimal>)HandleBrilliantScarf);
		return methodMirrorRegistry;
	}

	private static decimal HandleFreeAttackPower(FreeAttackPower power, ModifyEnergyCostInCombatMirrorContext context)
	{
		return HandleFreeCardPower((PowerModel)(object)power, (CardType)1, context);
	}

	private static decimal HandleFreePowerPower(FreePowerPower power, ModifyEnergyCostInCombatMirrorContext context)
	{
		return HandleFreeCardPower((PowerModel)(object)power, (CardType)3, context);
	}

	private static decimal HandleFreeSkillPower(FreeSkillPower power, ModifyEnergyCostInCombatMirrorContext context)
	{
		return HandleFreeCardPower((PowerModel)(object)power, (CardType)2, context);
	}

	private static decimal HandleVeilpiercerPower(VeilpiercerPower power, ModifyEnergyCostInCombatMirrorContext context)
	{
		if (context.Card.Preview.Owner.Creature != ((PowerModel)power).Owner || !context.Card.GetKeywords(context.Simulator).Contains((CardKeyword)2) || !IsInPlayablePile(context) || !context.StateStore.GetPowerAmount((PowerModel)(object)power).IsActive)
		{
			return context.Cost;
		}
		return 0m;
	}

	private static decimal HandleVoidFormPower(VoidFormPower power, ModifyEnergyCostInCombatMirrorContext context)
	{
		VoidFormPower power2 = power;
		if (context.Card.Preview.Owner.Creature != ((PowerModel)power2).Owner || !IsInPlayablePile(context) || context.StateStore.Get((AbstractModel)(object)power2, () => new VoidFormPredictionState(power2)).CardsPlayedThisTurn >= ((PowerModel)power2).Amount)
		{
			return context.Cost;
		}
		return 0m;
	}

	private static decimal HandleBrilliantScarf(BrilliantScarf relic, ModifyEnergyCostInCombatMirrorContext context)
	{
		BrilliantScarf relic2 = relic;
		if (context.Card.Preview.Owner != ((RelicModel)relic2).Owner || !IsInPlayablePile(context) || context.StateStore.Get((AbstractModel)(object)relic2, () => new CounterPredictionState(relic2._cardsPlayedThisTurn)).Value != ((DynamicVar)((RelicModel)relic2).DynamicVars.Cards).IntValue - 1)
		{
			return context.Cost;
		}
		return 0m;
	}

	private static decimal HandleFreeCardPower(PowerModel power, CardType type, ModifyEnergyCostInCombatMirrorContext context)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (context.Card.Preview.Owner.Creature != power.Owner || context.Card.Preview.Type != type || !IsInPlayablePile(context) || !context.StateStore.GetPowerAmount(power).IsActive)
		{
			return context.Cost;
		}
		return 0m;
	}

	private static bool IsInPlayablePile(ModifyEnergyCostInCombatMirrorContext context)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Invalid comparison between Unknown and I4
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Invalid comparison between Unknown and I4
		PileType? val = context.Card.GetPile(context.State)?.Type;
		if (val.HasValue)
		{
			PileType valueOrDefault = val.GetValueOrDefault();
			if ((int)valueOrDefault == 2 || (int)valueOrDefault == 5)
			{
				return true;
			}
		}
		return false;
	}
}
