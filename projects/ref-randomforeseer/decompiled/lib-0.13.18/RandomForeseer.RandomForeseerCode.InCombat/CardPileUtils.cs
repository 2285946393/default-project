using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using RandomForeseer.RandomForeseerCode.InCombat.Extensions;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class CardPileUtils
{
	public static bool TryGetDrawPileOwner(CardPile pile, [NotNullWhen(true)] out Player? player)
	{
		CardPile pile2 = pile;
		CombatState? obj = CombatManagerExtensions.get_LiveCombatState(CombatManager.Instance);
		player = ((obj != null) ? ((IEnumerable<Player>)obj.Players).FirstOrDefault((Func<Player, bool>)delegate(Player candidate)
		{
			PlayerCombatState playerCombatState = candidate.PlayerCombatState;
			return ((playerCombatState != null) ? playerCombatState.DrawPile : null) == pile2;
		}) : null);
		return player != null;
	}
}
