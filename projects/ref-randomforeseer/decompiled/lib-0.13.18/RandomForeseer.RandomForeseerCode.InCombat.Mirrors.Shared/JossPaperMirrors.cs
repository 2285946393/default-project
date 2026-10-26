using System.Linq;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.TurnEnd;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Shared;

internal static class JossPaperMirrors
{
	private sealed class State
	{
		public int CardsExhausted { get; set; } = relic.CardsExhausted;


		public int EtherealCount { get; set; } = relic.EtherealCount;


		public State(JossPaper relic)
		{
		}
	}

	public static void AfterCardExhausted(JossPaper relic, AfterCardExhaustedMirrorContext context)
	{
		if (context.PreviewCard.Owner == ((RelicModel)relic).Owner)
		{
			State state = GetState(relic, context);
			if (context.CausedByEthereal)
			{
				state.EtherealCount++;
				return;
			}
			state.CardsExhausted++;
			DrawIfThresholdMet(relic, state, context);
		}
	}

	public static void AfterSideTurnEnd(JossPaper relic, SideTurnEndMirrorContext context)
	{
		if (context.Participants.Contains(((RelicModel)relic).Owner.Creature))
		{
			State state = GetState(relic, context);
			state.CardsExhausted += state.EtherealCount;
			state.EtherealCount = 0;
			DrawIfThresholdMet(relic, state, context);
		}
	}

	private static State GetState(JossPaper relic, CombatMirrorContext<AbstractModel> context)
	{
		JossPaper relic2 = relic;
		return context.StateStore.Get((AbstractModel)(object)relic2, () => new State(relic2));
	}

	private static void DrawIfThresholdMet(JossPaper relic, State state, CombatMirrorContext<AbstractModel> context)
	{
		if (!((decimal)state.CardsExhausted < ((RelicModel)relic).DynamicVars["ExhaustAmount"].BaseValue))
		{
			int drawCount = (int)((decimal)state.CardsExhausted / ((RelicModel)relic).DynamicVars["ExhaustAmount"].BaseValue);
			context.Simulator.Draw(((RelicModel)relic).Owner, drawCount);
			state.CardsExhausted %= ((RelicModel)relic).DynamicVars["ExhaustAmount"].IntValue;
		}
	}
}
