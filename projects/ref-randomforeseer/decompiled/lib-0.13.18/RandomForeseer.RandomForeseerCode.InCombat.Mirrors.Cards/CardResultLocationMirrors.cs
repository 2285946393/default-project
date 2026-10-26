using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards;

internal static class CardResultLocationMirrors
{
	private static readonly MirrorMethodSpec GetResultLocationForCardPlay = new MirrorMethodSpec(typeof(CardModel), "GetResultLocationForCardPlay", BindingFlags.Instance | BindingFlags.NonPublic, Array.Empty<Type>());

	private static readonly MethodMirrorRegistry<CardModel, CardResultLocationMirrorContext, CardLocation> Registry = CreateRegistry();

	public static CardLocation GetResultLocation(CombatPredictionSimulator simulator, PredictedCard card)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		CardModel mutablePreview = card.MutablePreview;
		CardLocation baseResultLocation = GetBaseResultLocation(simulator, card);
		return Registry.Invoke(mutablePreview, new CardResultLocationMirrorContext
		{
			Simulator = simulator,
			Card = card,
			BaseResult = baseResultLocation
		}, baseResultLocation).Value;
	}

	private static MethodMirrorRegistry<CardModel, CardResultLocationMirrorContext, CardLocation> CreateRegistry()
	{
		MethodMirrorRegistry<CardModel, CardResultLocationMirrorContext, CardLocation> methodMirrorRegistry = new MethodMirrorRegistry<CardModel, CardResultLocationMirrorContext, CardLocation>(GetResultLocationForCardPlay);
		methodMirrorRegistry.Register<ParticleWall>((Func<ParticleWall, CardResultLocationMirrorContext, CardLocation>)HandleParticleWall);
		methodMirrorRegistry.Register<ShiningStrike>((Func<ShiningStrike, CardResultLocationMirrorContext, CardLocation>)HandleShiningStrike);
		methodMirrorRegistry.Register<TheBall>((Func<TheBall, CardResultLocationMirrorContext, CardLocation>)HandleTheBall);
		return methodMirrorRegistry;
	}

	private static CardLocation GetBaseResultLocation(CombatPredictionSimulator simulator, PredictedCard card)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		CardModel mutablePreview = card.MutablePreview;
		if (mutablePreview.IsDupe || (int)mutablePreview.Type == 3)
		{
			return new CardLocation(mutablePreview.Owner, (PileType)0, (CardPilePosition)1);
		}
		if (mutablePreview.ExhaustOnNextPlay || card.GetKeywords(simulator).Contains((CardKeyword)1))
		{
			mutablePreview.ExhaustOnNextPlay = false;
			return new CardLocation(mutablePreview.Owner, (PileType)4, (CardPilePosition)1);
		}
		return new CardLocation(mutablePreview.Owner, (PileType)3, (CardPilePosition)1);
	}

	private static CardLocation HandleParticleWall(ParticleWall _, CardResultLocationMirrorContext context)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		CardLocation baseResult = context.BaseResult;
		if ((int)baseResult.pileType == 3)
		{
			baseResult.pileType = (PileType)2;
		}
		return baseResult;
	}

	private static CardLocation HandleShiningStrike(ShiningStrike _, CardResultLocationMirrorContext context)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		CardLocation baseResult = context.BaseResult;
		if ((int)baseResult.pileType == 3)
		{
			baseResult.pileType = (PileType)1;
			baseResult.position = (CardPilePosition)2;
		}
		return baseResult;
	}

	private static CardLocation HandleTheBall(TheBall _, CardResultLocationMirrorContext context)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Invalid comparison between Unknown and I4
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		CardResultLocationMirrorContext context2 = context;
		CardLocation baseResult = context2.BaseResult;
		Player owner = context2.PreviewCard.Owner;
		List<Creature> list = (from creature in context2.State.GetTeammatesOf(owner.Creature)
			where context2.State.GetCreature(creature).IsAlive && creature.IsPlayer && creature.Player != owner
			select creature).ToList();
		if (list.Count == 0)
		{
			return baseResult;
		}
		baseResult.player = context2.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)list).Player;
		if ((int)baseResult.pileType == 3)
		{
			baseResult.pileType = (PileType)1;
			baseResult.position = (CardPilePosition)3;
		}
		return baseResult;
	}
}
