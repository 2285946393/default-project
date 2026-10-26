using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Mirrors.Hooks.CardCreation;

internal static class TryModifyCardRewardOptionsMirrors
{
	private static readonly MirrorMethodSpec TryModifyCardRewardOptions = MirrorMethodSpec.Hook("TryModifyCardRewardOptions", new Type[3]
	{
		typeof(Player),
		typeof(List<CardCreationResult>),
		typeof(CardCreationOptions)
	});

	private static readonly MirrorMethodSpec TryModifyCardRewardOptionsLate = MirrorMethodSpec.Hook("TryModifyCardRewardOptionsLate", new Type[3]
	{
		typeof(Player),
		typeof(List<CardCreationResult>),
		typeof(CardCreationOptions)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, TryModifyCardRewardOptionsMirrorContext, bool> Registry = CreateRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, TryModifyCardRewardOptionsMirrorContext, bool> LateRegistry = CreateLateRegistry();

	public static bool Invoke(AbstractModel listener, TryModifyCardRewardOptionsMirrorContext context)
	{
		return Registry.Invoke(listener, context, defaultResult: false).Value;
	}

	public static bool InvokeLate(AbstractModel listener, TryModifyCardRewardOptionsMirrorContext context)
	{
		return LateRegistry.Invoke(listener, context, defaultResult: false).Value;
	}

	private static MethodMirrorRegistry<AbstractModel, TryModifyCardRewardOptionsMirrorContext, bool> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, TryModifyCardRewardOptionsMirrorContext, bool> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, TryModifyCardRewardOptionsMirrorContext, bool>(TryModifyCardRewardOptions);
		methodMirrorRegistry.Register<LastingCandy>((Func<LastingCandy, TryModifyCardRewardOptionsMirrorContext, bool>)HandleLastingCandy);
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<AbstractModel, TryModifyCardRewardOptionsMirrorContext, bool> CreateLateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, TryModifyCardRewardOptionsMirrorContext, bool> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, TryModifyCardRewardOptionsMirrorContext, bool>(TryModifyCardRewardOptionsLate);
		methodMirrorRegistry.Register<FrozenEgg>((Func<FrozenEgg, TryModifyCardRewardOptionsMirrorContext, bool>)((FrozenEgg relic, TryModifyCardRewardOptionsMirrorContext context) => HandleEggRelic((RelicModel)(object)relic, context, (CardType)3)));
		methodMirrorRegistry.Register<MoltenEgg>((Func<MoltenEgg, TryModifyCardRewardOptionsMirrorContext, bool>)((MoltenEgg relic, TryModifyCardRewardOptionsMirrorContext context) => HandleEggRelic((RelicModel)(object)relic, context, (CardType)1)));
		methodMirrorRegistry.Register<ToxicEgg>((Func<ToxicEgg, TryModifyCardRewardOptionsMirrorContext, bool>)((ToxicEgg relic, TryModifyCardRewardOptionsMirrorContext context) => HandleEggRelic((RelicModel)(object)relic, context, (CardType)2)));
		methodMirrorRegistry.Register<SilverCrucible>((Func<SilverCrucible, TryModifyCardRewardOptionsMirrorContext, bool>)HandleSilverCrucible);
		methodMirrorRegistry.Register<LavaLamp>((Func<LavaLamp, TryModifyCardRewardOptionsMirrorContext, bool>)HandleLavaLamp);
		methodMirrorRegistry.Register<Glitter>((Func<Glitter, TryModifyCardRewardOptionsMirrorContext, bool>)HandleGlitter);
		methodMirrorRegistry.Register<FresnelLens>((Func<FresnelLens, TryModifyCardRewardOptionsMirrorContext, bool>)HandleFresnelLens);
		methodMirrorRegistry.Register<SilkenTress>((Func<SilkenTress, TryModifyCardRewardOptionsMirrorContext, bool>)HandleSilkenTress);
		methodMirrorRegistry.Register<WingCharm>((Func<WingCharm, TryModifyCardRewardOptionsMirrorContext, bool>)HandleWingCharm);
		return methodMirrorRegistry;
	}

	private static bool HandleLastingCandy(LastingCandy relic, TryModifyCardRewardOptionsMirrorContext context)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Expected O, but got Unknown
		TryModifyCardRewardOptionsMirrorContext context2 = context;
		if (((RelicModel)relic).Owner != context2.Player || (int)context2.Options.Source != 1 || !relic.IsInTriggeringCombat || !((Enum)context2.Options.Flags).HasFlag((Enum)(object)(CardCreationFlags)128) || !((Enum)context2.Options.Flags).HasFlag((Enum)(object)(CardCreationFlags)256))
		{
			return false;
		}
		List<CardModel> source = context2.Options.GetPossibleCards(context2.Player).ToList();
		bool allowDupes = false;
		if (!source.Any(IsLastingCandyCandidate))
		{
			allowDupes = true;
			if (!source.Any(IsLastingCandyCandidate))
			{
				return false;
			}
		}
		Func<CardModel, bool> parentFilter = context2.Options.CardPoolFilter;
		CardCreationOptions options = new CardCreationOptions((IEnumerable<CardPoolModel>)context2.Options.CardPools, (CardCreationSource)3, context2.Options.RarityOdds, (Func<CardModel, bool>)((CardModel card) => (parentFilter == null || parentFilter(card)) && IsLastingCandyCandidate(card))).WithFlags((CardCreationFlags)24);
		CardCreationResult? obj = CardRewardPrediction.CreateBaseRewards(context2.Player, 1, options, context2.Rng.Rewards, context2.RarityOdds).FirstOrDefault();
		CardModel val = ((obj != null) ? obj.Card : null);
		if (val == null)
		{
			return false;
		}
		CardCreationResult val2 = new CardCreationResult(val);
		val2.ModifyCard(val, (RelicModel)(object)relic);
		context2.Results.Add(val2);
		return true;
		bool IsLastingCandyCandidate(CardModel card)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Invalid comparison between Unknown and I4
			CardModel card2 = card;
			if ((int)card2.Type == 3)
			{
				if (!allowDupes)
				{
					return context2.Results.All((CardCreationResult result) => ((AbstractModel)result.originalCard).Id != ((AbstractModel)card2).Id);
				}
				return true;
			}
			return false;
		}
	}

	private static bool HandleSilverCrucible(SilverCrucible relic, TryModifyCardRewardOptionsMirrorContext context)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (((RelicModel)relic).Owner != context.Player || relic.TimesUsed >= ((DynamicVar)((RelicModel)relic).DynamicVars.Cards).IntValue || !((Enum)context.Options.Flags).HasFlag((Enum)(object)(CardCreationFlags)128))
		{
			return false;
		}
		CardCreationResultUtils.UpgradeValidCards(context.Results, (RelicModel)(object)relic);
		return true;
	}

	private static bool HandleLavaLamp(LavaLamp relic, TryModifyCardRewardOptionsMirrorContext context)
	{
		if (((RelicModel)relic).Owner != context.Player || !(context.Player.RunState.CurrentRoom is CombatRoom) || relic.TookDamageThisCombat)
		{
			return false;
		}
		CardCreationResultUtils.UpgradeValidCards(context.Results, (RelicModel)(object)relic);
		return true;
	}

	private static bool HandleGlitter(Glitter relic, TryModifyCardRewardOptionsMirrorContext context)
	{
		if (((RelicModel)relic).Owner != context.Player)
		{
			return false;
		}
		CardCreationResultUtils.EnchantValidCards<Glam>((IEnumerable<CardCreationResult>)context.Results, (RelicModel)(object)relic, 1m);
		return true;
	}

	private static bool HandleFresnelLens(FresnelLens relic, TryModifyCardRewardOptionsMirrorContext context)
	{
		if (((RelicModel)relic).Owner != context.Player)
		{
			return false;
		}
		decimal baseValue = ((RelicModel)relic).DynamicVars["NimbleAmount"].BaseValue;
		CardCreationResultUtils.EnchantValidCards<Nimble>((IEnumerable<CardCreationResult>)context.Results, (RelicModel)(object)relic, baseValue);
		return true;
	}

	private static bool HandleSilkenTress(SilkenTress relic, TryModifyCardRewardOptionsMirrorContext context)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if (((RelicModel)relic).Owner != context.Player || ((RelicModel)relic).IsUsedUp || !((Enum)context.Options.Flags).HasFlag((Enum)(object)(CardCreationFlags)128))
		{
			return false;
		}
		CardCreationResultUtils.EnchantValidCards<Glam>((IEnumerable<CardCreationResult>)context.Results, (RelicModel)(object)relic, 1m);
		return true;
	}

	private static bool HandleWingCharm(WingCharm relic, TryModifyCardRewardOptionsMirrorContext context)
	{
		if (((RelicModel)relic).Owner != context.Player)
		{
			return false;
		}
		Swift swift = ModelDb.Enchantment<Swift>();
		List<CardCreationResult> list = context.Results.Where((CardCreationResult result) => ((EnchantmentModel)swift).CanEnchant(result.Card)).ToList();
		CardCreationResult val = context.SharedRng.Niche.NextItem<CardCreationResult>((IEnumerable<CardCreationResult>)list);
		if (val == null)
		{
			return false;
		}
		decimal baseValue = ((RelicModel)relic).DynamicVars["SwiftAmount"].BaseValue;
		val.ModifyCard(PredictionUtils.CreateEnchantedCard(((EnchantmentModel)swift).ToMutable(), val.Card, baseValue), (RelicModel)(object)relic);
		return true;
	}

	private static bool HandleEggRelic(RelicModel relic, TryModifyCardRewardOptionsMirrorContext context, CardType type)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (relic.Owner != context.Player || ((Enum)context.Options.Flags).HasFlag((Enum)(object)(CardCreationFlags)4))
		{
			return false;
		}
		CardCreationResultUtils.UpgradeCardsOfType(context.Results, relic, type);
		return true;
	}
}
