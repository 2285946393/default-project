using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal static class SpecialAttackCardMirrors
{
	public static void EchoingSlashOnPlay(EchoingSlash card, CardOnPlayMirrorContext context)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		using SimAttackContext simAttackContext = context.Simulator.CreateAttackContext(context.CardPlay);
		int num;
		for (num = 1; num > 0; num--)
		{
			IReadOnlyList<DamageResult> readOnlyList = context.Simulator.Damage(context.State.HittableEnemies, ((DynamicVar)((CardModel)card).DynamicVars.Damage).BaseValue, ((CardModel)card).DynamicVars.Damage.Props, ((CardModel)card).Owner.Creature, context.Card, context.CardPlay);
			simAttackContext.AddHit(readOnlyList);
			num += readOnlyList.Count((DamageResult result) => result.WasTargetKilled);
		}
	}

	public static void OmnisliceOnPlay(Omnislice card, CardOnPlayMirrorContext context)
	{
		CardOnPlayMirrorContext context2 = context;
		using SimAttackContext simAttackContext = context2.Simulator.CreateAttackContext(context2.CardPlay);
		IReadOnlyList<DamageResult> readOnlyList = context2.Simulator.Damage(new _003C_003Ez__ReadOnlySingleElementList<Creature>(context2.Target), ((DynamicVar)((CardModel)card).DynamicVars.Damage).BaseValue, (ValueProp)8, ((CardModel)card).Owner.Creature, context2.Card, context2.CardPlay);
		simAttackContext.AddHit(readOnlyList);
		if (readOnlyList != null && readOnlyList.Count >= 1)
		{
			DamageResult firstResult = readOnlyList[0];
			Creature[] array = (from creature in context2.State.GetTeammatesOf(firstResult.Receiver)
				where creature != firstResult.Receiver && context2.State.GetCreature(creature).IsHittable
				select creature).ToArray();
			if (array.Length != 0)
			{
				simAttackContext.AddHit(context2.Simulator.Damage(array, firstResult.TotalDamage + firstResult.OverkillDamage, (ValueProp)12, ((CardModel)card).Owner.Creature, context2.Card, context2.CardPlay));
			}
		}
	}
}
