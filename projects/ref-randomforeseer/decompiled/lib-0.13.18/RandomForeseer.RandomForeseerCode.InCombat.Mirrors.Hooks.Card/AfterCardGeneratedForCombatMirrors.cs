using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal static class AfterCardGeneratedForCombatMirrors
{
	private static readonly MirrorMethodSpec AfterCardGeneratedForCombat = MirrorMethodSpec.Hook("AfterCardGeneratedForCombat", new Type[2]
	{
		typeof(CardModel),
		typeof(Player)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterCardGeneratedForCombatMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, AfterCardGeneratedForCombatMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterCardGeneratedForCombatMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterCardGeneratedForCombatMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterCardGeneratedForCombatMirrorContext>(AfterCardGeneratedForCombat);
		methodMirrorRegistry.Register<Aeonglass>((Action<Aeonglass, AfterCardGeneratedForCombatMirrorContext>)HandleAeonglass);
		methodMirrorRegistry.Register<ArsenalPower>((Action<ArsenalPower, AfterCardGeneratedForCombatMirrorContext>)HandleArsenalPower);
		methodMirrorRegistry.Register<Regalite>((Action<Regalite, AfterCardGeneratedForCombatMirrorContext>)HandleRegalite);
		methodMirrorRegistry.Register<SoulboundPower>((Action<SoulboundPower, AfterCardGeneratedForCombatMirrorContext>)HandleSoulboundPower);
		methodMirrorRegistry.Register<PillarOfCreationPower>((Action<PillarOfCreationPower, AfterCardGeneratedForCombatMirrorContext>)HandlePillarOfCreationPower);
		methodMirrorRegistry.Register<SmokestackPower>((Action<SmokestackPower, AfterCardGeneratedForCombatMirrorContext>)HandleSmokestackPower);
		methodMirrorRegistry.Register<TrashToTreasurePower>((Action<TrashToTreasurePower, AfterCardGeneratedForCombatMirrorContext>)HandleTrashToTreasurePower);
		methodMirrorRegistry.Register<RocketPunch>((Action<RocketPunch, AfterCardGeneratedForCombatMirrorContext>)HandleRocketPunch);
		return methodMirrorRegistry;
	}

	private static void HandleAeonglass(Aeonglass monster, AfterCardGeneratedForCombatMirrorContext context)
	{
		CardModel mutablePreviewCard = context.MutablePreviewCard;
		Wither val = (Wither)(object)((mutablePreviewCard is Wither) ? mutablePreviewCard : null);
		if (val != null)
		{
			for (int i = 0; i < monster.WitherUpgradeCount; i++)
			{
				val.FakeUpgrade();
			}
		}
	}

	private static void HandleArsenalPower(ArsenalPower power, AfterCardGeneratedForCombatMirrorContext context)
	{
		Player? creator = context.Creator;
		if (((creator != null) ? creator.Creature : null) == ((PowerModel)power).Owner)
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private static void HandleRegalite(Regalite relic, AfterCardGeneratedForCombatMirrorContext context)
	{
		Regalite relic2 = relic;
		if (context.Creator == ((RelicModel)relic2).Owner)
		{
			RegalitePredictionState regalitePredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new RegalitePredictionState(relic2));
			if (!regalitePredictionState.UsedThisTurn)
			{
				regalitePredictionState.UsedThisTurn = true;
				context.Simulator.GainBlock(((RelicModel)relic2).Owner.Creature, ((RelicModel)relic2).DynamicVars.Block);
			}
		}
	}

	private static void HandleSoulboundPower(SoulboundPower power, AfterCardGeneratedForCombatMirrorContext context)
	{
		SoulboundPower power2 = power;
		Player? creator = context.Creator;
		if (((creator != null) ? creator.Creature : null) != ((PowerModel)power2).Applier || !(context.PreviewCard is Soul))
		{
			return;
		}
		Player player = ((PowerModel)power2).Owner.Player;
		if (player == null)
		{
			return;
		}
		SoulboundPredictionState soulboundPredictionState = context.StateStore.Get((AbstractModel)(object)power2, () => new SoulboundPredictionState(power2));
		if (soulboundPredictionState.IsAddingSoul)
		{
			return;
		}
		soulboundPredictionState.IsAddingSoul = true;
		try
		{
			context.Simulator.CreateAndAddGeneratedCardsToCombat<Soul>(player, (PileType)1, ((PowerModel)power2).Amount, player, (CardPilePosition)3);
		}
		finally
		{
			soulboundPredictionState.IsAddingSoul = false;
		}
	}

	private static void HandlePillarOfCreationPower(PillarOfCreationPower power, AfterCardGeneratedForCombatMirrorContext context)
	{
		Player? creator = context.Creator;
		if (((creator != null) ? creator.Creature : null) == ((PowerModel)power).Owner)
		{
			context.Simulator.GainBlock(((PowerModel)power).Owner, ((PowerModel)power).Amount, (ValueProp)4);
		}
	}

	private static void HandleSmokestackPower(SmokestackPower power, AfterCardGeneratedForCombatMirrorContext context)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)context.PreviewCard.Type == 4)
		{
			Player? creator = context.Creator;
			if (((creator != null) ? creator.Creature : null) == ((PowerModel)power).Owner)
			{
				context.Simulator.Damage(context.State.HittableEnemies, ((PowerModel)power).Amount, (ValueProp)4, ((PowerModel)power).Owner);
			}
		}
	}

	private static void HandleTrashToTreasurePower(TrashToTreasurePower power, AfterCardGeneratedForCombatMirrorContext context)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)context.PreviewCard.Type != 4)
		{
			return;
		}
		Player? creator = context.Creator;
		if (((creator != null) ? creator.Creature : null) != ((PowerModel)power).Owner)
		{
			return;
		}
		Player player = ((PowerModel)power).Owner.Player;
		if (player != null)
		{
			for (int i = 0; i < ((PowerModel)power).Amount; i++)
			{
				OrbModel orb = OrbModel.GetRandomOrb(context.Rng.CombatOrbGeneration).ToMutable(0);
				context.Simulator.OrbChannel(player, orb);
			}
		}
	}

	private static void HandleRocketPunch(RocketPunch card, AfterCardGeneratedForCombatMirrorContext context)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Invalid comparison between Unknown and I4
		if (context.Creator == ((CardModel)card).Owner && context.PreviewCard.Owner == ((CardModel)card).Owner && (int)context.PreviewCard.Type == 4)
		{
			PredictedCard? predictedCard = context.State.FindCard((CardModel)(object)card);
			if (predictedCard != null)
			{
				predictedCard.MutablePreview.EnergyCost.AddUntilPlayed(-1, false);
			}
		}
	}
}
