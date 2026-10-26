using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Attack;

internal static class GigantificationPowerMirrors
{
	private sealed class State
	{
		public AttackCommand? CommandToModify { get; set; }
	}

	public static void BeforeAttack(GigantificationPower power, BeforeAttackMirrorContext context)
	{
		if (context.StateStore.GetPowerAmount((PowerModel)(object)power).IsActive && ShouldTrigger(power, context.Command))
		{
			State state = GetState(power, context);
			if (state.CommandToModify == null)
			{
				AttackCommand val = (state.CommandToModify = context.Command);
			}
		}
	}

	public static void AfterAttack(GigantificationPower power, AfterAttackMirrorContext context)
	{
		State state = GetState(power, context);
		if (context.Command == state.CommandToModify)
		{
			state.CommandToModify = null;
			context.StateStore.GetPowerAmount((PowerModel)(object)power).Decrement();
		}
	}

	public static decimal ModifyDamageMultiplicative(GigantificationPower power, ModifyDamageMirrorContext context)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (!context.StateStore.GetPowerAmount((PowerModel)(object)power).IsActive || context.CardSource == null || context.CardSource.Preview.Owner.Creature != ((PowerModel)power).Owner || !ValuePropExtensions.IsPoweredAttack(context.Props))
		{
			return 1m;
		}
		AttackCommand commandToModify = GetState(power, context).CommandToModify;
		return (commandToModify != null && !context.CardSource.References(commandToModify.ModelSource)) ? 1 : 3;
	}

	private static State GetState(GigantificationPower power, CombatMirrorContext context)
	{
		return context.StateStore.Get<State>((AbstractModel)(object)power);
	}

	private static bool ShouldTrigger(GigantificationPower power, AttackCommand command)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		AbstractModel modelSource = command.ModelSource;
		CardModel val = (CardModel)(object)((modelSource is CardModel) ? modelSource : null);
		if (val != null && val.Owner.Creature == ((PowerModel)power).Owner && (int)val.Type == 1)
		{
			return ValuePropExtensions.IsPoweredAttack(command.DamageProps);
		}
		return false;
	}
}
