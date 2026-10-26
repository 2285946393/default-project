using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.TurnEnd;

internal static class DoomPowerMirrors
{
	public static void BeforeSideTurnEnd(DoomPower power, SideTurnEndMirrorContext context)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if ((int)context.Side != 1 && ShouldDoomTrigger(power, context))
		{
			KillDoomedCreatures(context.Simulator, context.Side);
		}
	}

	public static void AfterSideTurnEnd(DoomPower power, SideTurnEndMirrorContext context)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if ((int)context.Side != 2 && ShouldDoomTrigger(power, context))
		{
			KillDoomedCreatures(context.Simulator, context.Side);
		}
	}

	private static bool ShouldDoomTrigger(DoomPower power, SideTurnEndMirrorContext context)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		if (!context.Simulator.IsOverOrEnding && context.Participants.Contains(((PowerModel)power).Owner) && context.State.GetCreature(((PowerModel)power).Owner).IsAlive && IsOwnerDoomed(context.State, power))
		{
			return GetDoomedCreatures(context.State, context.Side).First() == ((PowerModel)power).Owner;
		}
		return false;
	}

	private static void KillDoomedCreatures(CombatPredictionSimulator simulator, CombatSide side)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		Creature[] array = GetDoomedCreatures(simulator.State, side).ToArray();
		if (array.Length != 0)
		{
			simulator.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
			Creature[] array2 = array;
			foreach (Creature creature in array2)
			{
				simulator.Kill(creature);
			}
		}
	}

	private static IEnumerable<Creature> GetDoomedCreatures(CombatPredictionState state, CombatSide side)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		CombatPredictionState state2 = state;
		return state2.GetCreaturesOnSide(side).Where(delegate(Creature creature)
		{
			DoomPower power = creature.GetPower<DoomPower>();
			return power != null && IsOwnerDoomed(state2, power);
		});
	}

	private static bool IsOwnerDoomed(CombatPredictionState state, DoomPower power)
	{
		return state.GetCreature(((PowerModel)power).Owner).CurrentHp <= ((PowerModel)power).Amount;
	}
}
