using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Achievements;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Damage;

internal static class AfterDamageGivenMirrors
{
	private static readonly MirrorMethodSpec AfterDamageGiven = MirrorMethodSpec.Hook("AfterDamageGiven", new Type[6]
	{
		typeof(PlayerChoiceContext),
		typeof(Creature),
		typeof(DamageResult),
		typeof(ValueProp),
		typeof(Creature),
		typeof(CardModel)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterDamageGivenMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, AfterDamageGivenMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterDamageGivenMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterDamageGivenMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterDamageGivenMirrorContext>(AfterDamageGiven);
		methodMirrorRegistry.RegisterIgnored<SkillIronclad2Achievement>();
		methodMirrorRegistry.Register<ConcoctPower>((Action<ConcoctPower, AfterDamageGivenMirrorContext>)HandleConcoctPower);
		methodMirrorRegistry.Register<EnvenomPower>((Action<EnvenomPower, AfterDamageGivenMirrorContext>)HandleEnvenomPower);
		methodMirrorRegistry.RegisterIgnored<ImbalancedPower>();
		methodMirrorRegistry.Register<MonarchsGazePower>((Action<MonarchsGazePower, AfterDamageGivenMirrorContext>)HandleMonarchsGazePower);
		methodMirrorRegistry.RegisterIgnored<PaperCutsPower>();
		methodMirrorRegistry.Register<ReaperFormPower>((Action<ReaperFormPower, AfterDamageGivenMirrorContext>)HandleReaperFormPower);
		methodMirrorRegistry.Register<SicEmPower>((Action<SicEmPower, AfterDamageGivenMirrorContext>)HandleSicEmPower);
		methodMirrorRegistry.Register<UnderworldPower>((Action<UnderworldPower, AfterDamageGivenMirrorContext>)HandleUnderworldPower);
		return methodMirrorRegistry;
	}

	private static void HandleConcoctPower(ConcoctPower power, AfterDamageGivenMirrorContext context)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (context.Dealer == ((PowerModel)power).Owner && ValuePropExtensions.IsPoweredAttack(context.Props) && context.Result.UnblockedDamage > 0)
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private static void HandleEnvenomPower(EnvenomPower power, AfterDamageGivenMirrorContext context)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (context.Dealer == ((PowerModel)power).Owner && ValuePropExtensions.IsPoweredAttack(context.Props) && context.Result.UnblockedDamage > 0)
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private static void HandleMonarchsGazePower(MonarchsGazePower power, AfterDamageGivenMirrorContext context)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (context.Dealer == ((PowerModel)power).Owner && ValuePropExtensions.IsPoweredAttack(context.Props))
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private static void HandleReaperFormPower(ReaperFormPower power, AfterDamageGivenMirrorContext context)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if (context.Dealer == null)
		{
			return;
		}
		if (context.Dealer != ((PowerModel)power).Owner)
		{
			Player petOwner = context.Dealer.PetOwner;
			if (((petOwner != null) ? petOwner.Creature : null) != ((PowerModel)power).Owner)
			{
				return;
			}
		}
		if (ValuePropExtensions.IsPoweredAttack(context.Props) && context.Result.TotalDamage > 0)
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private static void HandleSicEmPower(SicEmPower power, AfterDamageGivenMirrorContext context)
	{
		Creature? dealer = context.Dealer;
		MonsterModel obj = ((dealer != null) ? dealer.Monster : null);
		Osty val = (Osty)(object)((obj is Osty) ? obj : null);
		if (val != null && ((PowerModel)power).Applier != null)
		{
			Player petOwner = ((MonsterModel)val).Creature.PetOwner;
			if (((petOwner != null) ? petOwner.Creature : null) == ((PowerModel)power).Applier && context.Target == ((PowerModel)power).Owner)
			{
				context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
			}
		}
	}

	private static void HandleUnderworldPower(UnderworldPower power, AfterDamageGivenMirrorContext context)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		if (context.Dealer != null && context.Dealer.Side == ((PowerModel)power).Owner.Side && context.Dealer != ((PowerModel)power).Owner && context.Dealer.PetOwner != ((PowerModel)power).Owner.Player && ValuePropExtensions.IsPoweredAttack(context.Props) && context.Result.TotalDamage > 0)
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}
}
