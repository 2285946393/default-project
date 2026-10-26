using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Death;

internal static class AfterDeathMirrors
{
	private static readonly MirrorMethodSpec AfterDeath = MirrorMethodSpec.Hook("AfterDeath", new Type[4]
	{
		typeof(PlayerChoiceContext),
		typeof(Creature),
		typeof(bool),
		typeof(float)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterDeathMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, AfterDeathMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterDeathMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterDeathMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterDeathMirrorContext>(AfterDeath);
		methodMirrorRegistry.RegisterIgnored<Aeonglass>();
		methodMirrorRegistry.RegisterIgnored<DecimillipedeSegment>();
		methodMirrorRegistry.RegisterIgnored<KinPriest>();
		methodMirrorRegistry.RegisterIgnored<LagavulinMatriarch>();
		methodMirrorRegistry.RegisterIgnored<Queen>();
		methodMirrorRegistry.RegisterIgnored<SoulFysh>();
		methodMirrorRegistry.RegisterIgnored<TestSubject>();
		methodMirrorRegistry.RegisterIgnored<TheInsatiable>();
		methodMirrorRegistry.RegisterIgnored<Vantom>();
		methodMirrorRegistry.RegisterIgnored<WaterfallGiant>();
		methodMirrorRegistry.Register<CrabRagePower>((Action<CrabRagePower, AfterDeathMirrorContext>)HandleCrabRagePower);
		methodMirrorRegistry.Register<GremlinHorn>((Action<GremlinHorn, AfterDeathMirrorContext>)HandleGremlinHorn);
		methodMirrorRegistry.Register<Melancholy>((Action<Melancholy, AfterDeathMirrorContext>)HandleMelancholy);
		return methodMirrorRegistry;
	}

	private static void HandleCrabRagePower(CrabRagePower power, AfterDeathMirrorContext context)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (context.Creature != ((PowerModel)power).Owner && context.Creature.Side == ((PowerModel)power).Owner.Side)
		{
			PowerAmountPredictionState powerAmount = context.StateStore.GetPowerAmount((PowerModel)(object)power);
			if (powerAmount.IsActive)
			{
				context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
				context.Simulator.GainBlock(((PowerModel)power).Owner, ((PowerModel)power).DynamicVars.Block);
				powerAmount.Consume();
			}
		}
	}

	private static void HandleGremlinHorn(GremlinHorn relic, AfterDeathMirrorContext context)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (context.Creature.Side != ((RelicModel)relic).Owner.Creature.Side)
		{
			context.Simulator.GainEnergy(((RelicModel)relic).Owner, ((DynamicVar)((RelicModel)relic).DynamicVars.Energy).BaseValue);
			context.Simulator.Draw(((RelicModel)relic).Owner, ((DynamicVar)((RelicModel)relic).DynamicVars.Cards).BaseValue);
		}
	}

	private static void HandleMelancholy(Melancholy card, AfterDeathMirrorContext context)
	{
		if (!context.WasRemovalPrevented)
		{
			CardModel val = context.State.FindCard((CardModel)(object)card)?.MutablePreview;
			if (val != null)
			{
				val.EnergyCost.AddThisCombat(-((DynamicVar)val.DynamicVars.Energy).IntValue, false);
			}
		}
	}
}
