using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal static class ModifyStarCostMirrors
{
	private static readonly MirrorMethodSpec ModifyStarCost = MirrorMethodSpec.Hook("TryModifyStarCost", new Type[3]
	{
		typeof(CardModel),
		typeof(decimal),
		typeof(decimal).MakeByRefType()
	});

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyStarCostMirrorContext, decimal> Registry = CreateRegistry();

	public static decimal Invoke(AbstractModel listener, ModifyStarCostMirrorContext context)
	{
		if (Registry.TryInvokeRegistered(listener, context, out var dispatchResult))
		{
			return dispatchResult.Value;
		}
		decimal result = default(decimal);
		listener.TryModifyStarCost(context.Card.Preview, context.Cost, ref result);
		return result;
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyStarCostMirrorContext, decimal> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyStarCostMirrorContext, decimal> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyStarCostMirrorContext, decimal>(ModifyStarCost);
		methodMirrorRegistry.Register<VoidFormPower>((Func<VoidFormPower, ModifyStarCostMirrorContext, decimal>)HandleVoidFormPower);
		methodMirrorRegistry.Register<BrilliantScarf>((Func<BrilliantScarf, ModifyStarCostMirrorContext, decimal>)HandleBrilliantScarf);
		return methodMirrorRegistry;
	}

	private static decimal HandleVoidFormPower(VoidFormPower power, ModifyStarCostMirrorContext context)
	{
		VoidFormPower power2 = power;
		if (context.Card.Preview.Owner.Creature != ((PowerModel)power2).Owner || !IsInPlayablePile(context) || context.StateStore.Get((AbstractModel)(object)power2, () => new VoidFormPredictionState(power2)).CardsPlayedThisTurn >= ((PowerModel)power2).Amount)
		{
			return context.Cost;
		}
		return 0m;
	}

	private static decimal HandleBrilliantScarf(BrilliantScarf relic, ModifyStarCostMirrorContext context)
	{
		BrilliantScarf relic2 = relic;
		if (context.Card.Preview.Owner != ((RelicModel)relic2).Owner || !IsInPlayablePile(context) || context.StateStore.Get((AbstractModel)(object)relic2, () => new CounterPredictionState(relic2._cardsPlayedThisTurn)).Value != ((DynamicVar)((RelicModel)relic2).DynamicVars.Cards).IntValue - 1)
		{
			return context.Cost;
		}
		return 0m;
	}

	private static bool IsInPlayablePile(ModifyStarCostMirrorContext context)
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
