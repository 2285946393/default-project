using System;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;

namespace RandomForeseer.RandomForeseerCode.InCombat.Extensions;

internal static class CombatManagerExtensions
{
	[SpecialName]
	public sealed class _003CG_003E_002494CFA2F0BFBE606B46ADF0DD4804ABD7
	{
		[SpecialName]
		public static class _003CM_003E_0024197039CC1B20652AC4F02E3F13450A17
		{
		}

		[ExtensionMarker("<M>$197039CC1B20652AC4F02E3F13450A17")]
		public CombatTurnState? LiveTurnState
		{
			[ExtensionMarker("<M>$197039CC1B20652AC4F02E3F13450A17")]
			get
			{
				throw new NotSupportedException();
			}
		}

		[ExtensionMarker("<M>$197039CC1B20652AC4F02E3F13450A17")]
		public CombatState? LiveCombatState
		{
			[ExtensionMarker("<M>$197039CC1B20652AC4F02E3F13450A17")]
			get
			{
				throw new NotSupportedException();
			}
		}
	}

	public static CombatTurnState? get_LiveTurnState(CombatManager combatManager)
	{
		CombatTurnState turnState = combatManager._turnState;
		if (turnState == null || !turnState.IsInProgress)
		{
			return null;
		}
		return turnState;
	}

	public static CombatState? get_LiveCombatState(CombatManager combatManager)
	{
		CombatTurnState? obj = get_LiveTurnState(combatManager);
		if (obj == null)
		{
			return null;
		}
		return obj.State;
	}
}
