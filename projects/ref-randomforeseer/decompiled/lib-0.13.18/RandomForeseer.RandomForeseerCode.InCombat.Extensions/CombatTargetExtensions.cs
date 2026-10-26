using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Extensions;

internal static class CombatTargetExtensions
{
	[SpecialName]
	public sealed class _003CG_003E_0024AF28B12ECBA0B248B043FAEFCC7ABDF0
	{
		[SpecialName]
		public static class _003CM_003E_00242C3E41D0CA83EE4A8436A2802BF0F40C
		{
		}

		[ExtensionMarker("<M>$2C3E41D0CA83EE4A8436A2802BF0F40C")]
		public IReadOnlyList<Creature> GetValidTargets()
		{
			throw new NotSupportedException();
		}
	}

	public static IReadOnlyList<Creature> GetValidTargets(this CardModel card)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected I4, but got Unknown
		CardModel card2 = card;
		ICombatState combatState = card2.Owner.Creature.CombatState;
		if (combatState == null)
		{
			return Array.Empty<Creature>();
		}
		TargetType targetType = card2.TargetType;
		switch (targetType - 2)
		{
		case 0:
		{
			List<Creature> list3 = new List<Creature>();
			list3.AddRange(combatState.Enemies.Where((Creature creature) => creature.IsAlive));
			return new _003C_003Ez__ReadOnlyList<Creature>(list3);
		}
		case 3:
		{
			List<Creature> list2 = new List<Creature>();
			list2.AddRange(combatState.PlayerCreatures.Where((Creature creature) => creature.IsAlive));
			return new _003C_003Ez__ReadOnlyList<Creature>(list2);
		}
		case 4:
		{
			List<Creature> list = new List<Creature>();
			list.AddRange(combatState.PlayerCreatures.Where((Creature creature) => creature != card2.Owner.Creature && creature.IsAlive));
			return new _003C_003Ez__ReadOnlyList<Creature>(list);
		}
		default:
			return Array.Empty<Creature>();
		}
	}
}
