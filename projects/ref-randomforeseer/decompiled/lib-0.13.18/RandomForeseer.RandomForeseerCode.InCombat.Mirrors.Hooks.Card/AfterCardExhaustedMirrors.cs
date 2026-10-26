using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Achievements;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Shared;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal static class AfterCardExhaustedMirrors
{
	private static readonly MirrorMethodSpec AfterCardExhausted = MirrorMethodSpec.Hook("AfterCardExhausted", new Type[3]
	{
		typeof(PlayerChoiceContext),
		typeof(CardModel),
		typeof(bool)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterCardExhaustedMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, AfterCardExhaustedMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterCardExhaustedMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterCardExhaustedMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterCardExhaustedMirrorContext>(AfterCardExhausted);
		methodMirrorRegistry.Register<BurningSticks>((Action<BurningSticks, AfterCardExhaustedMirrorContext>)HandleBurningSticks);
		methodMirrorRegistry.Register<CharonsAshes>((Action<CharonsAshes, AfterCardExhaustedMirrorContext>)HandleCharonsAshes);
		methodMirrorRegistry.Register<DarkEmbracePower>((Action<DarkEmbracePower, AfterCardExhaustedMirrorContext>)HandleDarkEmbracePower);
		methodMirrorRegistry.Register<DrumOfBattle>((Action<DrumOfBattle, AfterCardExhaustedMirrorContext>)HandleDrumOfBattle);
		methodMirrorRegistry.Register<FeelNoPainPower>((Action<FeelNoPainPower, AfterCardExhaustedMirrorContext>)HandleFeelNoPainPower);
		methodMirrorRegistry.Register<ForgottenSoul>((Action<ForgottenSoul, AfterCardExhaustedMirrorContext>)HandleForgottenSoul);
		methodMirrorRegistry.Register<JossPaper>((Action<JossPaper, AfterCardExhaustedMirrorContext>)JossPaperMirrors.AfterCardExhausted);
		methodMirrorRegistry.Register<Midnight>((Action<Midnight, AfterCardExhaustedMirrorContext>)HandleMidnight);
		methodMirrorRegistry.RegisterIgnored<SkillIronclad1Achievement>();
		return methodMirrorRegistry;
	}

	private static void HandleBurningSticks(BurningSticks relic, AfterCardExhaustedMirrorContext context)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		BurningSticks relic2 = relic;
		if (context.PreviewCard.Owner == ((RelicModel)relic2).Owner && (int)context.PreviewCard.Type == 2)
		{
			BurningSticksPredictionState burningSticksPredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new BurningSticksPredictionState(relic2));
			if (!burningSticksPredictionState.WasUsedThisCombat)
			{
				context.Simulator.AddGeneratedCardToCombat(context.Card.CreateClone(), (PileType)2, ((RelicModel)relic2).Owner, (CardPilePosition)1, CardGenerationResultKind.Contextual);
				burningSticksPredictionState.WasUsedThisCombat = true;
			}
		}
	}

	private static void HandleDarkEmbracePower(DarkEmbracePower power, AfterCardExhaustedMirrorContext context)
	{
		DarkEmbracePower power2 = power;
		if (context.PreviewCard.Owner.Creature != ((PowerModel)power2).Owner)
		{
			return;
		}
		Player player = ((PowerModel)power2).Owner.Player;
		if (player == null)
		{
			return;
		}
		if (context.CausedByEthereal)
		{
			context.StateStore.Get((AbstractModel)(object)power2, () => new DarkEmbracePredictionState(power2)).EtherealCount++;
		}
		else
		{
			context.Simulator.Draw(player, ((PowerModel)power2).Amount);
		}
	}

	private static void HandleDrumOfBattle(DrumOfBattle card, AfterCardExhaustedMirrorContext context)
	{
		if (context.Card.References(card))
		{
			int num = context.Card.GeneratePlayCount(context.Simulator, null);
			for (int i = 0; i < num; i++)
			{
				context.Simulator.GainEnergy(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Energy).BaseValue);
			}
		}
	}

	private static void HandleCharonsAshes(CharonsAshes relic, AfterCardExhaustedMirrorContext context)
	{
		if (((RelicModel)relic).Owner == context.PreviewCard.Owner)
		{
			context.Simulator.Damage(context.State.HittableEnemies, ((RelicModel)relic).DynamicVars.Damage, ((RelicModel)relic).Owner.Creature);
		}
	}

	private static void HandleForgottenSoul(ForgottenSoul relic, AfterCardExhaustedMirrorContext context)
	{
		if (((RelicModel)relic).Owner == context.PreviewCard.Owner)
		{
			Creature val = context.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)context.State.HittableEnemies);
			if (val != null)
			{
				context.Simulator.Damage(val, ((RelicModel)relic).DynamicVars.Damage, ((RelicModel)relic).Owner.Creature);
			}
		}
	}

	private static void HandleFeelNoPainPower(FeelNoPainPower power, AfterCardExhaustedMirrorContext context)
	{
		if (((PowerModel)power).Owner == context.PreviewCard.Owner.Creature)
		{
			context.Simulator.GainBlock(((PowerModel)power).Owner, ((PowerModel)power).Amount, (ValueProp)4);
		}
	}

	private static void HandleMidnight(Midnight card, AfterCardExhaustedMirrorContext context)
	{
		PredictedCard? predictedCard = context.State.FindCard((CardModel)(object)card);
		if (predictedCard != null)
		{
			predictedCard.MutablePreview.EnergyCost.AddThisCombat(-1, false);
		}
	}
}
