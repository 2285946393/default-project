using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Potions.OnUse;

internal static class PotionOnUseMirrors
{
	private static readonly MirrorMethodSpec OnUse = new MirrorMethodSpec(typeof(PotionModel), "OnUse", BindingFlags.Instance | BindingFlags.NonPublic, new Type[2]
	{
		typeof(PlayerChoiceContext),
		typeof(Creature)
	});

	private static readonly MethodMirrorRegistry<PotionModel, PotionOnUseMirrorContext> Registry = CreateRegistry();

	public static bool CanMirror(PotionModel potion)
	{
		return Registry.HasRegisteredHandler(potion);
	}

	public static bool IsOnUseInvocation(PredictionInvocation invocation)
	{
		return (object)invocation.Method == OnUse.BaseMethod;
	}

	public static MirrorDispatchResult Invoke(CombatPredictionSimulator simulator, PotionModel potion, Creature? target)
	{
		return Registry.Invoke(potion, new PotionOnUseMirrorContext
		{
			Simulator = simulator,
			Potion = potion,
			Target = target
		});
	}

	private static MethodMirrorRegistry<PotionModel, PotionOnUseMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<PotionModel, PotionOnUseMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<PotionModel, PotionOnUseMirrorContext>(OnUse);
		methodMirrorRegistry.Register<AttackPotion>((Action<AttackPotion, PotionOnUseMirrorContext>)CardGenerationPotionMirrors.OnUse);
		methodMirrorRegistry.Register<SkillPotion>((Action<SkillPotion, PotionOnUseMirrorContext>)CardGenerationPotionMirrors.OnUse);
		methodMirrorRegistry.Register<PowerPotion>((Action<PowerPotion, PotionOnUseMirrorContext>)CardGenerationPotionMirrors.OnUse);
		methodMirrorRegistry.Register<ColorlessPotion>((Action<ColorlessPotion, PotionOnUseMirrorContext>)CardGenerationPotionMirrors.OnUse);
		methodMirrorRegistry.Register<CosmicConcoction>((Action<CosmicConcoction, PotionOnUseMirrorContext>)CardGenerationPotionMirrors.OnUse);
		methodMirrorRegistry.Register<OrobicAcid>((Action<OrobicAcid, PotionOnUseMirrorContext>)CardGenerationPotionMirrors.OnUse);
		methodMirrorRegistry.Register<EntropicBrew>((Action<EntropicBrew, PotionOnUseMirrorContext>)EntropicBrewMirrors.OnUse);
		methodMirrorRegistry.Register<BottledPotential>((Action<BottledPotential, PotionOnUseMirrorContext>)DrawPotionMirrors.BottledPotentialOnUse);
		methodMirrorRegistry.Register<Clarity>((Action<Clarity, PotionOnUseMirrorContext>)DrawPotionMirrors.ClarityOnUse);
		methodMirrorRegistry.Register<CureAll>((Action<CureAll, PotionOnUseMirrorContext>)DrawPotionMirrors.CureAllOnUse);
		methodMirrorRegistry.Register<GlowwaterPotion>((Action<GlowwaterPotion, PotionOnUseMirrorContext>)DrawPotionMirrors.GlowwaterPotionOnUse);
		methodMirrorRegistry.Register<SneckoOil>((Action<SneckoOil, PotionOnUseMirrorContext>)DrawPotionMirrors.SneckoOilOnUse);
		methodMirrorRegistry.Register<SwiftPotion>((Action<SwiftPotion, PotionOnUseMirrorContext>)DrawPotionMirrors.SwiftPotionOnUse);
		methodMirrorRegistry.Register<DistilledChaos>((Action<DistilledChaos, PotionOnUseMirrorContext>)AutoPlayPotionMirrors.DistilledChaosOnUse);
		methodMirrorRegistry.Register<EssenceOfDarkness>((Action<EssenceOfDarkness, PotionOnUseMirrorContext>)OrbPotionMirrors.EssenceOfDarknessOnUse);
		return methodMirrorRegistry;
	}
}
