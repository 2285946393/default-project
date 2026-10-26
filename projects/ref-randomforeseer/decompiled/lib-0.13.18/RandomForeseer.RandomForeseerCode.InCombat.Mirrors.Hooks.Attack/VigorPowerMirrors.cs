using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Attack;

internal static class VigorPowerMirrors
{
	private sealed class State
	{
		public AttackCommand? CommandToModify { get; set; }

		public int AmountWhenAttackStarted { get; set; }
	}

	public static void BeforeAttack(VigorPower power, BeforeAttackMirrorContext context)
	{
		PowerAmountPredictionState powerAmount = context.StateStore.GetPowerAmount((PowerModel)(object)power);
		if (powerAmount.IsActive && ShouldTrigger(power, context.Command))
		{
			State state = GetState(power, context);
			if (state.CommandToModify == null)
			{
				state.CommandToModify = context.Command;
				state.AmountWhenAttackStarted = powerAmount.Amount;
			}
		}
	}

	public static void AfterAttack(VigorPower power, AfterAttackMirrorContext context)
	{
		State state = GetState(power, context);
		if (context.Command == state.CommandToModify)
		{
			context.StateStore.GetPowerAmount((PowerModel)(object)power).Decrease(state.AmountWhenAttackStarted);
			state.CommandToModify = null;
			state.AmountWhenAttackStarted = 0;
		}
	}

	public static decimal ModifyDamageAdditive(VigorPower power, ModifyDamageMirrorContext context)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		PowerAmountPredictionState powerAmount = context.StateStore.GetPowerAmount((PowerModel)(object)power);
		if (!powerAmount.IsActive || context.Dealer != ((PowerModel)power).Owner || !ValuePropExtensions.IsPoweredAttack(context.Props))
		{
			return 0m;
		}
		AttackCommand commandToModify = GetState(power, context).CommandToModify;
		if (commandToModify != null && context.CardSource != null && !context.CardSource.References(commandToModify.ModelSource))
		{
			return 0m;
		}
		if (commandToModify != null && commandToModify.Attacker != context.Dealer)
		{
			return 0m;
		}
		return powerAmount.Amount;
	}

	private static State GetState(VigorPower power, CombatMirrorContext context)
	{
		return context.StateStore.Get<State>((AbstractModel)(object)power);
	}

	private static bool ShouldTrigger(VigorPower power, AttackCommand command)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		bool flag = command.Attacker == ((PowerModel)power).Owner && ValuePropExtensions.IsPoweredAttack(command.DamageProps);
		if (flag)
		{
			AbstractModel modelSource = command.ModelSource;
			bool flag2 = ((modelSource == null || modelSource is CardModel) ? true : false);
			flag = flag2;
		}
		return flag;
	}
}
