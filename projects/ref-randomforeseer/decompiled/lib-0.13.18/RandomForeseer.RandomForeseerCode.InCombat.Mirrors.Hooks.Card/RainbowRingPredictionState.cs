using MegaCrit.Sts2.Core.Models.Relics;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class RainbowRingPredictionState
{
	public int AttacksPlayedThisTurn { get; set; } = relic._attacksPlayedThisTurn;


	public int SkillsPlayedThisTurn { get; set; } = relic._skillsPlayedThisTurn;


	public int PowersPlayedThisTurn { get; set; } = relic._powersPlayedThisTurn;


	public int ActivationCountThisTurn { get; set; } = relic._activationCountThisTurn;


	public RainbowRingPredictionState(RainbowRing relic)
	{
	}
}
