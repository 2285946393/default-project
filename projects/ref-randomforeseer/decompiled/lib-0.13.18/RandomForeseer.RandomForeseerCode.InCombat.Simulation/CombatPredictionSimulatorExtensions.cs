using System.Linq;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal static class CombatPredictionSimulatorExtensions
{
	public static void Simulate(this AttackCommand attackCommand, CombatPredictionSimulator simulator)
	{
		simulator.ExecuteAttack(attackCommand);
	}

	public static void ExhaustHand(this CombatPredictionSimulator simulator, Player player)
	{
		PredictedCard[] array = simulator.State.GetPlayerCombatState(player).Hand.Cards.ToArray();
		foreach (PredictedCard card in array)
		{
			simulator.Exhaust(card);
		}
	}

	public static void MoveHandToDrawPile(this CombatPredictionSimulator simulator, Player player)
	{
		PredictedCard[] cards = simulator.State.GetPlayerCombatState(player).Hand.Cards.ToArray();
		simulator.AddToPile(cards, (PileType)1, (CardPilePosition)1);
	}
}
