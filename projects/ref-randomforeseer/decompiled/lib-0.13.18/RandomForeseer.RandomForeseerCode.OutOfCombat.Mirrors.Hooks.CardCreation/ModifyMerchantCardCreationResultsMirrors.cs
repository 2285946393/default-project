using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Mirrors.Hooks.CardCreation;

internal static class ModifyMerchantCardCreationResultsMirrors
{
	private static readonly MirrorMethodSpec ModifyMerchantCardCreationResults = MirrorMethodSpec.Hook("ModifyMerchantCardCreationResults", new Type[2]
	{
		typeof(Player),
		typeof(List<CardCreationResult>)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, ModifyMerchantCardCreationResultsMirrorContext> Registry = CreateRegistry();

	public static MirrorDispatchResult Invoke(AbstractModel listener, ModifyMerchantCardCreationResultsMirrorContext context)
	{
		return Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, ModifyMerchantCardCreationResultsMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, ModifyMerchantCardCreationResultsMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, ModifyMerchantCardCreationResultsMirrorContext>(ModifyMerchantCardCreationResults);
		methodMirrorRegistry.Register<FrozenEgg>((Action<FrozenEgg, ModifyMerchantCardCreationResultsMirrorContext>)delegate(FrozenEgg relic, ModifyMerchantCardCreationResultsMirrorContext context)
		{
			HandleEggRelic((RelicModel)(object)relic, context, (CardType)3);
		});
		methodMirrorRegistry.Register<MoltenEgg>((Action<MoltenEgg, ModifyMerchantCardCreationResultsMirrorContext>)delegate(MoltenEgg relic, ModifyMerchantCardCreationResultsMirrorContext context)
		{
			HandleEggRelic((RelicModel)(object)relic, context, (CardType)1);
		});
		methodMirrorRegistry.Register<ToxicEgg>((Action<ToxicEgg, ModifyMerchantCardCreationResultsMirrorContext>)delegate(ToxicEgg relic, ModifyMerchantCardCreationResultsMirrorContext context)
		{
			HandleEggRelic((RelicModel)(object)relic, context, (CardType)2);
		});
		methodMirrorRegistry.Register<FresnelLens>((Action<FresnelLens, ModifyMerchantCardCreationResultsMirrorContext>)HandleFresnelLens);
		return methodMirrorRegistry;
	}

	private static void HandleEggRelic(RelicModel relic, ModifyMerchantCardCreationResultsMirrorContext context, CardType cardType)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (relic.Owner == context.Player)
		{
			CardCreationResultUtils.UpgradeCardsOfType(context.Results, relic, cardType);
		}
	}

	private static void HandleFresnelLens(FresnelLens relic, ModifyMerchantCardCreationResultsMirrorContext context)
	{
		if (((RelicModel)relic).Owner == context.Player)
		{
			decimal baseValue = ((RelicModel)relic).DynamicVars["NimbleAmount"].BaseValue;
			CardCreationResultUtils.EnchantValidCards<Nimble>((IEnumerable<CardCreationResult>)context.Results, (RelicModel)(object)relic, baseValue);
		}
	}
}
