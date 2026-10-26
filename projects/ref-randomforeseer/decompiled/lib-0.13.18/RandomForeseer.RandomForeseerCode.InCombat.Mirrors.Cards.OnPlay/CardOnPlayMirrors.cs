using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal static class CardOnPlayMirrors
{
	private static readonly MirrorMethodSpec OnPlay = new MirrorMethodSpec(typeof(CardModel), "OnPlay", BindingFlags.Instance | BindingFlags.NonPublic, new Type[2]
	{
		typeof(PlayerChoiceContext),
		typeof(CardPlay)
	});

	private static readonly MethodMirrorRegistry<CardModel, CardOnPlayMirrorContext> Registry = CreateRegistry();

	public static bool AllowInference
	{
		get
		{
			return Registry.AllowInference;
		}
		set
		{
			Registry.AllowInference = value;
		}
	}

	public static void InvalidateLookupCache()
	{
		Registry.InvalidateLookupCache();
	}

	public static bool CanMirror(CardModel card)
	{
		return Registry.HasRegisteredHandler(card);
	}

	public static bool IsOnPlayInvocation(PredictionInvocation invocation)
	{
		return (object)invocation.Method == OnPlay.BaseMethod;
	}

	public static MirrorDispatchResult Invoke(CombatPredictionSimulator simulator, PredictedCard card, CardPlay cardPlay)
	{
		return Registry.Invoke(card.MutablePreview, new CardOnPlayMirrorContext
		{
			Simulator = simulator,
			Card = card,
			CardPlay = cardPlay
		});
	}

	private static MethodMirrorRegistry<CardModel, CardOnPlayMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<CardModel, CardOnPlayMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<CardModel, CardOnPlayMirrorContext>(OnPlay);
		methodMirrorRegistry.Register<HowlFromBeyond>((Action<HowlFromBeyond, CardOnPlayMirrorContext>)GeneralCardMirrors.GeneralAttackOnPlay);
		methodMirrorRegistry.Register<IAmInvincible>((Action<IAmInvincible, CardOnPlayMirrorContext>)GeneralCardMirrors.GeneralBlockOnPlay);
		methodMirrorRegistry.Register<Havoc>((Action<Havoc, CardOnPlayMirrorContext>)AutoPlayCardMirrors.HavocOnPlay);
		methodMirrorRegistry.Register<Cascade>((Action<Cascade, CardOnPlayMirrorContext>)AutoPlayCardMirrors.CascadeOnPlay);
		methodMirrorRegistry.Register<Alchemize>((Action<Alchemize, CardOnPlayMirrorContext>)MiscCardMirrors.AlchemizeOnPlay);
		methodMirrorRegistry.Register<MadScience>((Action<MadScience, CardOnPlayMirrorContext>)MiscCardMirrors.MadScienceOnPlay);
		methodMirrorRegistry.Register<SecondWind>((Action<SecondWind, CardOnPlayMirrorContext>)MiscCardMirrors.SecondWindOnPlay);
		methodMirrorRegistry.Register<Dominate>((Action<Dominate, CardOnPlayMirrorContext>)VulnerableCardMirrors.DominateOnPlay);
		methodMirrorRegistry.Register<Expose>((Action<Expose, CardOnPlayMirrorContext>)VulnerableCardMirrors.ExposeOnPlay);
		methodMirrorRegistry.Register<HighFive>((Action<HighFive, CardOnPlayMirrorContext>)VulnerableCardMirrors.HighFiveOnPlay);
		methodMirrorRegistry.Register<MoltenFist>((Action<MoltenFist, CardOnPlayMirrorContext>)VulnerableCardMirrors.MoltenFistOnPlay);
		methodMirrorRegistry.Register<Shockwave>((Action<Shockwave, CardOnPlayMirrorContext>)VulnerableCardMirrors.ShockwaveOnPlay);
		methodMirrorRegistry.Register<CalculatedGamble>((Action<CalculatedGamble, CardOnPlayMirrorContext>)CardDrawCardMirrors.CalculatedGambleOnPlay);
		methodMirrorRegistry.Register<CompileDriver>((Action<CompileDriver, CardOnPlayMirrorContext>)CardDrawCardMirrors.CompileDriverOnPlay);
		methodMirrorRegistry.Register<Constellation>((Action<Constellation, CardOnPlayMirrorContext>)CardDrawCardMirrors.ConstellationOnPlay);
		methodMirrorRegistry.Register<EscapePlan>((Action<EscapePlan, CardOnPlayMirrorContext>)CardDrawCardMirrors.EscapePlanOnPlay);
		methodMirrorRegistry.Register<Expertise>((Action<Expertise, CardOnPlayMirrorContext>)CardDrawCardMirrors.ExpertiseOnPlay);
		methodMirrorRegistry.Register<Fetch>((Action<Fetch, CardOnPlayMirrorContext>)CardDrawCardMirrors.FetchOnPlay);
		methodMirrorRegistry.Register<Ftl>((Action<Ftl, CardOnPlayMirrorContext>)CardDrawCardMirrors.FtlOnPlay);
		methodMirrorRegistry.Register<HuddleUp>((Action<HuddleUp, CardOnPlayMirrorContext>)CardDrawCardMirrors.HuddleUpOnPlay);
		methodMirrorRegistry.Register<Impatience>((Action<Impatience, CardOnPlayMirrorContext>)CardDrawCardMirrors.ImpatienceOnPlay);
		methodMirrorRegistry.Register<Pillage>((Action<Pillage, CardOnPlayMirrorContext>)CardDrawCardMirrors.PillageOnPlay);
		methodMirrorRegistry.Register<Reboot>((Action<Reboot, CardOnPlayMirrorContext>)CardDrawCardMirrors.RebootOnPlay);
		methodMirrorRegistry.Register<Restlessness>((Action<Restlessness, CardOnPlayMirrorContext>)CardDrawCardMirrors.RestlessnessOnPlay);
		methodMirrorRegistry.Register<Scrape>((Action<Scrape, CardOnPlayMirrorContext>)CardDrawCardMirrors.ScrapeOnPlay);
		methodMirrorRegistry.Register<Scrawl>((Action<Scrawl, CardOnPlayMirrorContext>)CardDrawCardMirrors.ScrawlOnPlay);
		methodMirrorRegistry.Register<FlakCannon>((Action<FlakCannon, CardOnPlayMirrorContext>)RandomTargetAttackCardMirrors.FlakCannonOnPlay);
		methodMirrorRegistry.Register<Ricochet>((Action<Ricochet, CardOnPlayMirrorContext>)RandomTargetAttackCardMirrors.RicochetOnPlay);
		methodMirrorRegistry.Register<RipAndTear>((Action<RipAndTear, CardOnPlayMirrorContext>)RandomTargetAttackCardMirrors.RipAndTearOnPlay);
		methodMirrorRegistry.Register<Stardust>((Action<Stardust, CardOnPlayMirrorContext>)RandomTargetAttackCardMirrors.StardustOnPlay);
		methodMirrorRegistry.Register<SweepingGaze>((Action<SweepingGaze, CardOnPlayMirrorContext>)RandomTargetAttackCardMirrors.SweepingGazeOnPlay);
		methodMirrorRegistry.Register<SwordBoomerang>((Action<SwordBoomerang, CardOnPlayMirrorContext>)RandomTargetAttackCardMirrors.SwordBoomerangOnPlay);
		methodMirrorRegistry.Register<Volley>((Action<Volley, CardOnPlayMirrorContext>)RandomTargetAttackCardMirrors.VolleyOnPlay);
		methodMirrorRegistry.Register<AstralPulse>((Action<AstralPulse, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.AstralPulseOnPlay);
		methodMirrorRegistry.Register<Barrage>((Action<Barrage, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.BarrageOnPlay);
		methodMirrorRegistry.Register<DaggerSpray>((Action<DaggerSpray, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.DaggerSprayOnPlay);
		methodMirrorRegistry.Register<Dismantle>((Action<Dismantle, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.DismantleOnPlay);
		methodMirrorRegistry.Register<FiendFire>((Action<FiendFire, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.FiendFireOnPlay);
		methodMirrorRegistry.Register<Finisher>((Action<Finisher, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.FinisherOnPlay);
		methodMirrorRegistry.Register<Flechettes>((Action<Flechettes, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.FlechettesOnPlay);
		methodMirrorRegistry.Register<GunkUp>((Action<GunkUp, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.GunkUpOnPlay);
		methodMirrorRegistry.Register<HeavenlyDrill>((Action<HeavenlyDrill, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.HeavenlyDrillOnPlay);
		methodMirrorRegistry.Register<HelixDrill>((Action<HelixDrill, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.HelixDrillOnPlay);
		methodMirrorRegistry.Register<LunarBlast>((Action<LunarBlast, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.LunarBlastOnPlay);
		methodMirrorRegistry.Register<Maul>((Action<Maul, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.MaulOnPlay);
		methodMirrorRegistry.Register<PullFromBelow>((Action<PullFromBelow, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.PullFromBelowOnPlay);
		methodMirrorRegistry.Register<Radiate>((Action<Radiate, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.RadiateOnPlay);
		methodMirrorRegistry.Register<Rattle>((Action<Rattle, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.RattleOnPlay);
		methodMirrorRegistry.Register<SovereignBlade>((Action<SovereignBlade, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.SovereignBladeOnPlay);
		methodMirrorRegistry.Register<Spite>((Action<Spite, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.SpiteOnPlay);
		methodMirrorRegistry.Register<TearAsunder>((Action<TearAsunder, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.TearAsunderOnPlay);
		methodMirrorRegistry.Register<TwinStrike>((Action<TwinStrike, CardOnPlayMirrorContext>)MultiHitAttackCardMirrors.TwinStrikeOnPlay);
		methodMirrorRegistry.Register<EchoingSlash>((Action<EchoingSlash, CardOnPlayMirrorContext>)SpecialAttackCardMirrors.EchoingSlashOnPlay);
		methodMirrorRegistry.Register<Omnislice>((Action<Omnislice, CardOnPlayMirrorContext>)SpecialAttackCardMirrors.OmnisliceOnPlay);
		methodMirrorRegistry.Register<BallLightning>((Action<BallLightning, CardOnPlayMirrorContext>)OrbCardMirrors.BallLightningOnPlay);
		methodMirrorRegistry.Register<Chaos>((Action<Chaos, CardOnPlayMirrorContext>)OrbCardMirrors.ChaosOnPlay);
		methodMirrorRegistry.Register<Chill>((Action<Chill, CardOnPlayMirrorContext>)OrbCardMirrors.ChillOnPlay);
		methodMirrorRegistry.Register<ColdSnap>((Action<ColdSnap, CardOnPlayMirrorContext>)OrbCardMirrors.ColdSnapOnPlay);
		methodMirrorRegistry.Register<ConsumingShadow>((Action<ConsumingShadow, CardOnPlayMirrorContext>)OrbCardMirrors.ConsumingShadowOnPlay);
		methodMirrorRegistry.Register<Coolheaded>((Action<Coolheaded, CardOnPlayMirrorContext>)OrbCardMirrors.CoolheadedOnPlay);
		methodMirrorRegistry.Register<Darkness>((Action<Darkness, CardOnPlayMirrorContext>)OrbCardMirrors.DarknessOnPlay);
		methodMirrorRegistry.Register<Dualcast>((Action<Dualcast, CardOnPlayMirrorContext>)OrbCardMirrors.DualcastOnPlay);
		methodMirrorRegistry.Register<Fusion>((Action<Fusion, CardOnPlayMirrorContext>)OrbCardMirrors.FusionOnPlay);
		methodMirrorRegistry.Register<Glacier>((Action<Glacier, CardOnPlayMirrorContext>)OrbCardMirrors.GlacierOnPlay);
		methodMirrorRegistry.Register<Glasswork>((Action<Glasswork, CardOnPlayMirrorContext>)OrbCardMirrors.GlassworkOnPlay);
		methodMirrorRegistry.Register<IceLance>((Action<IceLance, CardOnPlayMirrorContext>)OrbCardMirrors.IceLanceOnPlay);
		methodMirrorRegistry.Register<Ignition>((Action<Ignition, CardOnPlayMirrorContext>)OrbCardMirrors.IgnitionOnPlay);
		methodMirrorRegistry.Register<MeteorStrike>((Action<MeteorStrike, CardOnPlayMirrorContext>)OrbCardMirrors.MeteorStrikeOnPlay);
		methodMirrorRegistry.Register<MultiCast>((Action<MultiCast, CardOnPlayMirrorContext>)OrbCardMirrors.MultiCastOnPlay);
		methodMirrorRegistry.Register<Null>((Action<Null, CardOnPlayMirrorContext>)OrbCardMirrors.NullOnPlay);
		methodMirrorRegistry.Register<Quadcast>((Action<Quadcast, CardOnPlayMirrorContext>)OrbCardMirrors.QuadcastOnPlay);
		methodMirrorRegistry.Register<Rainbow>((Action<Rainbow, CardOnPlayMirrorContext>)OrbCardMirrors.RainbowOnPlay);
		methodMirrorRegistry.Register<Refract>((Action<Refract, CardOnPlayMirrorContext>)OrbCardMirrors.RefractOnPlay);
		methodMirrorRegistry.Register<ShadowShield>((Action<ShadowShield, CardOnPlayMirrorContext>)OrbCardMirrors.ShadowShieldOnPlay);
		methodMirrorRegistry.Register<Shatter>((Action<Shatter, CardOnPlayMirrorContext>)OrbCardMirrors.ShatterOnPlay);
		methodMirrorRegistry.Register<Spinner>((Action<Spinner, CardOnPlayMirrorContext>)OrbCardMirrors.SpinnerOnPlay);
		methodMirrorRegistry.Register<Tempest>((Action<Tempest, CardOnPlayMirrorContext>)OrbCardMirrors.TempestOnPlay);
		methodMirrorRegistry.Register<TeslaCoil>((Action<TeslaCoil, CardOnPlayMirrorContext>)OrbCardMirrors.TeslaCoilOnPlay);
		methodMirrorRegistry.Register<Voltaic>((Action<Voltaic, CardOnPlayMirrorContext>)OrbCardMirrors.VoltaicOnPlay);
		methodMirrorRegistry.Register<Zap>((Action<Zap, CardOnPlayMirrorContext>)OrbCardMirrors.ZapOnPlay);
		methodMirrorRegistry.Register<Anointed>((Action<Anointed, CardOnPlayMirrorContext>)CardSelectionCardMirrors.AnointedOnPlay);
		methodMirrorRegistry.Register<BeatDown>((Action<BeatDown, CardOnPlayMirrorContext>)CardSelectionCardMirrors.BeatDownOnPlay);
		methodMirrorRegistry.Register<Catastrophe>((Action<Catastrophe, CardOnPlayMirrorContext>)CardSelectionCardMirrors.CatastropheOnPlay);
		methodMirrorRegistry.Register<Cinder>((Action<Cinder, CardOnPlayMirrorContext>)CardSelectionCardMirrors.CinderOnPlay);
		methodMirrorRegistry.Register<DrainPower>((Action<DrainPower, CardOnPlayMirrorContext>)CardSelectionCardMirrors.DrainPowerOnPlay);
		methodMirrorRegistry.Register<HiddenGem>((Action<HiddenGem, CardOnPlayMirrorContext>)CardSelectionCardMirrors.HiddenGemOnPlay);
		methodMirrorRegistry.Register<SeekerStrike>((Action<SeekerStrike, CardOnPlayMirrorContext>)CardSelectionCardMirrors.SeekerStrikeOnPlay);
		methodMirrorRegistry.Register<Thrash>((Action<Thrash, CardOnPlayMirrorContext>)CardSelectionCardMirrors.ThrashOnPlay);
		methodMirrorRegistry.Register<TrueGrit>((Action<TrueGrit, CardOnPlayMirrorContext>)CardSelectionCardMirrors.TrueGritOnPlay);
		methodMirrorRegistry.Register<Uproar>((Action<Uproar, CardOnPlayMirrorContext>)CardSelectionCardMirrors.UproarOnPlay);
		methodMirrorRegistry.Register<Abundance>((Action<Abundance, CardOnPlayMirrorContext>)CardGenerationCardMirrors.AbundanceOnPlay);
		methodMirrorRegistry.Register<BundleOfJoy>((Action<BundleOfJoy, CardOnPlayMirrorContext>)CardGenerationCardMirrors.BundleOfJoyOnPlay);
		methodMirrorRegistry.Register<Distraction>((Action<Distraction, CardOnPlayMirrorContext>)CardGenerationCardMirrors.DistractionOnPlay);
		methodMirrorRegistry.Register<Discovery>((Action<Discovery, CardOnPlayMirrorContext>)CardGenerationCardMirrors.DiscoveryOnPlay);
		methodMirrorRegistry.Register<InfernalBlade>((Action<InfernalBlade, CardOnPlayMirrorContext>)CardGenerationCardMirrors.InfernalBladeOnPlay);
		methodMirrorRegistry.Register<JackOfAllTrades>((Action<JackOfAllTrades, CardOnPlayMirrorContext>)CardGenerationCardMirrors.JackOfAllTradesOnPlay);
		methodMirrorRegistry.Register<Jackpot>((Action<Jackpot, CardOnPlayMirrorContext>)CardGenerationCardMirrors.JackpotOnPlay);
		methodMirrorRegistry.Register<Largesse>((Action<Largesse, CardOnPlayMirrorContext>)CardGenerationCardMirrors.LargesseOnPlay);
		methodMirrorRegistry.Register<ManifestAuthority>((Action<ManifestAuthority, CardOnPlayMirrorContext>)CardGenerationCardMirrors.ManifestAuthorityOnPlay);
		methodMirrorRegistry.Register<Metamorphosis>((Action<Metamorphosis, CardOnPlayMirrorContext>)CardGenerationCardMirrors.MetamorphosisOnPlay);
		methodMirrorRegistry.Register<Quasar>((Action<Quasar, CardOnPlayMirrorContext>)CardGenerationCardMirrors.QuasarOnPlay);
		methodMirrorRegistry.Register<Splash>((Action<Splash, CardOnPlayMirrorContext>)CardGenerationCardMirrors.SplashOnPlay);
		methodMirrorRegistry.Register<Stoke>((Action<Stoke, CardOnPlayMirrorContext>)CardGenerationCardMirrors.StokeOnPlay);
		methodMirrorRegistry.Register<WhiteNoise>((Action<WhiteNoise, CardOnPlayMirrorContext>)CardGenerationCardMirrors.WhiteNoiseOnPlay);
		methodMirrorRegistry.Register<StrikeIronclad>((Action<StrikeIronclad, CardOnPlayMirrorContext>)GeneralCardMirrors.GeneralAttackOnPlay);
		methodMirrorRegistry.Register<StrikeSilent>((Action<StrikeSilent, CardOnPlayMirrorContext>)GeneralCardMirrors.GeneralAttackOnPlay);
		methodMirrorRegistry.Register<StrikeRegent>((Action<StrikeRegent, CardOnPlayMirrorContext>)GeneralCardMirrors.GeneralAttackOnPlay);
		methodMirrorRegistry.Register<StrikeNecrobinder>((Action<StrikeNecrobinder, CardOnPlayMirrorContext>)GeneralCardMirrors.GeneralAttackOnPlay);
		methodMirrorRegistry.Register<StrikeDefect>((Action<StrikeDefect, CardOnPlayMirrorContext>)GeneralCardMirrors.GeneralAttackOnPlay);
		methodMirrorRegistry.Register<DefendIronclad>((Action<DefendIronclad, CardOnPlayMirrorContext>)GeneralCardMirrors.GeneralBlockOnPlay);
		methodMirrorRegistry.Register<DefendSilent>((Action<DefendSilent, CardOnPlayMirrorContext>)GeneralCardMirrors.GeneralBlockOnPlay);
		methodMirrorRegistry.Register<DefendRegent>((Action<DefendRegent, CardOnPlayMirrorContext>)GeneralCardMirrors.GeneralBlockOnPlay);
		methodMirrorRegistry.Register<DefendNecrobinder>((Action<DefendNecrobinder, CardOnPlayMirrorContext>)GeneralCardMirrors.GeneralBlockOnPlay);
		methodMirrorRegistry.Register<DefendDefect>((Action<DefendDefect, CardOnPlayMirrorContext>)GeneralCardMirrors.GeneralBlockOnPlay);
		methodMirrorRegistry.RegisterInferrer(CardOnPlayInferrer.Infer);
		return methodMirrorRegistry;
	}
}
