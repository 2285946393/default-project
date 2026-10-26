using System.Diagnostics.CodeAnalysis;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class CombatPredictionTargetResolver
{
	public static bool TryResolveCreature(Node node, [NotNullWhen(true)] out Creature? creature)
	{
		NCreature val = (NCreature)(object)((node is NCreature) ? node : null);
		Creature val3;
		if (val == null)
		{
			NMultiplayerPlayerState val2 = (NMultiplayerPlayerState)(object)((node is NMultiplayerPlayerState) ? node : null);
			val3 = ((val2 == null) ? null : val2.Player.Creature);
		}
		else
		{
			val3 = val.Entity;
		}
		creature = val3;
		return creature != null;
	}
}
