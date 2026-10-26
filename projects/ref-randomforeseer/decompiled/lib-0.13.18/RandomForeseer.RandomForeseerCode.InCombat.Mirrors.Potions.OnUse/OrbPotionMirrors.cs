using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Potions;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Potions.OnUse;

internal static class OrbPotionMirrors
{
	public static void EssenceOfDarknessOnUse(EssenceOfDarkness _, PotionOnUseMirrorContext context)
	{
		Player targetPlayer = context.TargetPlayer;
		int capacity = context.State.GetPlayerCombatState(targetPlayer).OrbQueue.Capacity;
		context.Simulator.OrbChannel<DarkOrb>(targetPlayer, capacity);
	}
}
