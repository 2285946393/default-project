using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Achievements;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Extensions;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal static class AfterCardPlayedMirrors
{
	private static readonly MirrorMethodSpec AfterCardPlayed = MirrorMethodSpec.Hook("AfterCardPlayed", new Type[2]
	{
		typeof(PlayerChoiceContext),
		typeof(CardPlay)
	});

	private static readonly MirrorMethodSpec AfterCardPlayedLate = MirrorMethodSpec.Hook("AfterCardPlayedLate", new Type[2]
	{
		typeof(PlayerChoiceContext),
		typeof(CardPlay)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterCardPlayedMirrorContext> Registry = CreateRegistry();

	private static readonly MethodMirrorRegistry<AbstractModel, AfterCardPlayedMirrorContext> LateRegistry = CreateLateRegistry();

	public static void Invoke(AbstractModel listener, AfterCardPlayedMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	public static void InvokeLate(AbstractModel listener, AfterCardPlayedMirrorContext context)
	{
		LateRegistry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, AfterCardPlayedMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterCardPlayedMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterCardPlayedMirrorContext>(AfterCardPlayed);
		methodMirrorRegistry.RegisterIgnored<Play20CardsSingleTurnAchievement>();
		methodMirrorRegistry.RegisterIgnored<SkillSilent1Achievement>();
		methodMirrorRegistry.RegisterIgnored<CccComboModel>();
		methodMirrorRegistry.Register<BansheesCry>((Action<BansheesCry, AfterCardPlayedMirrorContext>)HandleBansheesCry);
		methodMirrorRegistry.Register<Pinpoint>((Action<Pinpoint, AfterCardPlayedMirrorContext>)HandlePinpoint);
		methodMirrorRegistry.Register<Glam>((Action<Glam, AfterCardPlayedMirrorContext>)HandleGlam);
		methodMirrorRegistry.Register<Goopy>((Action<Goopy, AfterCardPlayedMirrorContext>)HandleGoopy);
		methodMirrorRegistry.Register<Vigorous>((Action<Vigorous, AfterCardPlayedMirrorContext>)HandleVigorous);
		methodMirrorRegistry.Register<AfterimagePower>((Action<AfterimagePower, AfterCardPlayedMirrorContext>)HandleAfterimagePower);
		methodMirrorRegistry.Register<BlackHolePower>((Action<BlackHolePower, AfterCardPlayedMirrorContext>)HandleBlackHolePower);
		methodMirrorRegistry.Register<CalamityPower>((Action<CalamityPower, AfterCardPlayedMirrorContext>)HandleCalamityPower);
		methodMirrorRegistry.Register<CurlUpPower>((Action<CurlUpPower, AfterCardPlayedMirrorContext>)HandleCurlUpPower);
		methodMirrorRegistry.Register<DevourLifePower>((Action<DevourLifePower, AfterCardPlayedMirrorContext>)HandleDevourLifePower);
		methodMirrorRegistry.RegisterIgnored<EchoFormPower>();
		methodMirrorRegistry.RegisterIgnored<EnragePower>();
		methodMirrorRegistry.Register<GalvanicPower>((Action<GalvanicPower, AfterCardPlayedMirrorContext>)HandleGalvanicPower);
		methodMirrorRegistry.Register<GravityPower>((Action<GravityPower, AfterCardPlayedMirrorContext>)HandleGravityPower);
		methodMirrorRegistry.Register<HauntPower>((Action<HauntPower, AfterCardPlayedMirrorContext>)HandleHauntPower);
		methodMirrorRegistry.Register<ImitationLearningPower>((Action<ImitationLearningPower, AfterCardPlayedMirrorContext>)HandleImitationLearningPower);
		methodMirrorRegistry.Register<MasterPlannerPower>((Action<MasterPlannerPower, AfterCardPlayedMirrorContext>)HandleMasterPlannerPower);
		methodMirrorRegistry.Register<MonologuePower>((Action<MonologuePower, AfterCardPlayedMirrorContext>)HandleMonologuePower);
		methodMirrorRegistry.Register<OblivionPower>((Action<OblivionPower, AfterCardPlayedMirrorContext>)HandleOblivionPower);
		methodMirrorRegistry.RegisterIgnored<PaleBlueDotPower>();
		methodMirrorRegistry.Register<PanachePower>((Action<PanachePower, AfterCardPlayedMirrorContext>)HandlePanachePower);
		methodMirrorRegistry.Register<RagePower>((Action<RagePower, AfterCardPlayedMirrorContext>)HandleRagePower);
		methodMirrorRegistry.Register<RupturePower>((Action<RupturePower, AfterCardPlayedMirrorContext>)HandleRupturePower);
		methodMirrorRegistry.Register<SerpentFormPower>((Action<SerpentFormPower, AfterCardPlayedMirrorContext>)HandleSerpentFormPower);
		methodMirrorRegistry.Register<SlowPower>((Action<SlowPower, AfterCardPlayedMirrorContext>)HandleSlowPower);
		methodMirrorRegistry.Register<SmoggyPower>((Action<SmoggyPower, AfterCardPlayedMirrorContext>)HandleSmoggyPower);
		methodMirrorRegistry.Register<SneakyPower>((Action<SneakyPower, AfterCardPlayedMirrorContext>)HandleSneakyPower);
		methodMirrorRegistry.Register<StormPower>((Action<StormPower, AfterCardPlayedMirrorContext>)HandleStormPower);
		methodMirrorRegistry.Register<StranglePower>((Action<StranglePower, AfterCardPlayedMirrorContext>)HandleStranglePower);
		methodMirrorRegistry.Register<SubroutinePower>((Action<SubroutinePower, AfterCardPlayedMirrorContext>)HandleSubroutinePower);
		methodMirrorRegistry.Register<TenderPower>((Action<TenderPower, AfterCardPlayedMirrorContext>)HandleTenderPower);
		methodMirrorRegistry.Register<VitalSparkPower>((Action<VitalSparkPower, AfterCardPlayedMirrorContext>)HandleVitalSparkPower);
		methodMirrorRegistry.Register<VoidFormPower>((Action<VoidFormPower, AfterCardPlayedMirrorContext>)HandleVoidFormPower);
		methodMirrorRegistry.Register<WitheringPresencePower>((Action<WitheringPresencePower, AfterCardPlayedMirrorContext>)HandleWitheringPresencePower);
		methodMirrorRegistry.RegisterIgnored<ArtOfWar>();
		methodMirrorRegistry.Register<BrilliantScarf>((Action<BrilliantScarf, AfterCardPlayedMirrorContext>)HandleBrilliantScarf);
		methodMirrorRegistry.Register<DaughterOfTheWind>((Action<DaughterOfTheWind, AfterCardPlayedMirrorContext>)HandleDaughterOfTheWind);
		methodMirrorRegistry.Register<GamePiece>((Action<GamePiece, AfterCardPlayedMirrorContext>)HandleGamePiece);
		methodMirrorRegistry.Register<HelicalDart>((Action<HelicalDart, AfterCardPlayedMirrorContext>)HandleHelicalDart);
		methodMirrorRegistry.Register<IronClub>((Action<IronClub, AfterCardPlayedMirrorContext>)HandleIronClub);
		methodMirrorRegistry.Register<IvoryTile>((Action<IvoryTile, AfterCardPlayedMirrorContext>)HandleIvoryTile);
		methodMirrorRegistry.Register<Kunai>((Action<Kunai, AfterCardPlayedMirrorContext>)HandleKunai);
		methodMirrorRegistry.Register<Kusarigama>((Action<Kusarigama, AfterCardPlayedMirrorContext>)HandleKusarigama);
		methodMirrorRegistry.Register<LetterOpener>((Action<LetterOpener, AfterCardPlayedMirrorContext>)HandleLetterOpener);
		methodMirrorRegistry.Register<LostWisp>((Action<LostWisp, AfterCardPlayedMirrorContext>)HandleLostWisp);
		methodMirrorRegistry.Register<MummifiedHand>((Action<MummifiedHand, AfterCardPlayedMirrorContext>)HandleMummifiedHand);
		methodMirrorRegistry.Register<MusicBox>((Action<MusicBox, AfterCardPlayedMirrorContext>)HandleMusicBox);
		methodMirrorRegistry.Register<Nunchaku>((Action<Nunchaku, AfterCardPlayedMirrorContext>)HandleNunchaku);
		methodMirrorRegistry.Register<OrnamentalFan>((Action<OrnamentalFan, AfterCardPlayedMirrorContext>)HandleOrnamentalFan);
		methodMirrorRegistry.Register<PaelsLegion>((Action<PaelsLegion, AfterCardPlayedMirrorContext>)HandlePaelsLegion);
		methodMirrorRegistry.Register<PenNib>((Action<PenNib, AfterCardPlayedMirrorContext>)HandlePenNib);
		methodMirrorRegistry.Register<Permafrost>((Action<Permafrost, AfterCardPlayedMirrorContext>)HandlePermafrost);
		methodMirrorRegistry.RegisterIgnored<Pocketwatch>();
		methodMirrorRegistry.Register<RainbowRing>((Action<RainbowRing, AfterCardPlayedMirrorContext>)HandleRainbowRing);
		methodMirrorRegistry.Register<RazorTooth>((Action<RazorTooth, AfterCardPlayedMirrorContext>)HandleRazorTooth);
		methodMirrorRegistry.RegisterIgnored<RippleBasin>();
		methodMirrorRegistry.Register<Shuriken>((Action<Shuriken, AfterCardPlayedMirrorContext>)HandleShuriken);
		methodMirrorRegistry.Register<TuningFork>((Action<TuningFork, AfterCardPlayedMirrorContext>)HandleTuningFork);
		methodMirrorRegistry.Register<UnsettlingLamp>((Action<UnsettlingLamp, AfterCardPlayedMirrorContext>)HandleUnsettlingLamp);
		methodMirrorRegistry.Register<Vambrace>((Action<Vambrace, AfterCardPlayedMirrorContext>)HandleVambrace);
		methodMirrorRegistry.Register<VelvetChoker>((Action<VelvetChoker, AfterCardPlayedMirrorContext>)HandleVelvetChoker);
		return methodMirrorRegistry;
	}

	private static MethodMirrorRegistry<AbstractModel, AfterCardPlayedMirrorContext> CreateLateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, AfterCardPlayedMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, AfterCardPlayedMirrorContext>(AfterCardPlayedLate);
		methodMirrorRegistry.Register<MakeItSo>((Action<MakeItSo, AfterCardPlayedMirrorContext>)HandleMakeItSo);
		methodMirrorRegistry.Register<RightHandHand>((Action<RightHandHand, AfterCardPlayedMirrorContext>)HandleRightHandHand);
		return methodMirrorRegistry;
	}

	private static void HandleDaughterOfTheWind(DaughterOfTheWind relic, AfterCardPlayedMirrorContext context)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		if (context.PreviewCard.Owner == ((RelicModel)relic).Owner && (int)context.PreviewCard.Type == 1)
		{
			context.Simulator.GainBlock(((RelicModel)relic).Owner.Creature, ((RelicModel)relic).DynamicVars.Block);
		}
	}

	private static void HandleBrilliantScarf(BrilliantScarf relic, AfterCardPlayedMirrorContext context)
	{
		BrilliantScarf relic2 = relic;
		if (!context.CardPlay.IsAutoPlay && context.PreviewCard.Owner == ((RelicModel)relic2).Owner)
		{
			context.StateStore.Get((AbstractModel)(object)relic2, () => new CounterPredictionState(relic2._cardsPlayedThisTurn)).Value++;
		}
	}

	private static void HandleGamePiece(GamePiece relic, AfterCardPlayedMirrorContext context)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		if (context.PreviewCard.Owner == ((RelicModel)relic).Owner && (int)context.PreviewCard.Type == 3)
		{
			context.Simulator.Draw(((RelicModel)relic).Owner, ((DynamicVar)((RelicModel)relic).DynamicVars.Cards).BaseValue);
		}
	}

	private static void HandleHelicalDart(HelicalDart relic, AfterCardPlayedMirrorContext context)
	{
		if (context.PreviewCard.Owner == ((RelicModel)relic).Owner && context.PreviewCard.Tags.Contains((CardTag)5))
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private static void HandleIronClub(IronClub relic, AfterCardPlayedMirrorContext context)
	{
		IronClub relic2 = relic;
		if (context.PreviewCard.Owner == ((RelicModel)relic2).Owner && ++context.StateStore.Get((AbstractModel)(object)relic2, () => new CounterPredictionState(relic2.CardsPlayed)).Value % ((DynamicVar)((RelicModel)relic2).DynamicVars.Cards).IntValue == 0)
		{
			context.Simulator.Draw(((RelicModel)relic2).Owner, 1);
		}
	}

	private static void HandleIvoryTile(IvoryTile relic, AfterCardPlayedMirrorContext context)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		if (context.PreviewCard.Owner == ((RelicModel)relic).Owner)
		{
			ResourceInfo resources = context.CardPlay.Resources;
			if (((ResourceInfo)(ref resources)).EnergyValue >= ((RelicModel)relic).DynamicVars["EnergyThreshold"].IntValue)
			{
				context.Simulator.GainEnergy(((RelicModel)relic).Owner, ((DynamicVar)((RelicModel)relic).DynamicVars.Energy).BaseValue);
			}
		}
	}

	private static void HandleKusarigama(Kusarigama relic, AfterCardPlayedMirrorContext context)
	{
		if (IncrementCounter((RelicModel)(object)relic, relic._attacksPlayedThisTurn, (CardType)1, context))
		{
			Creature val = context.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)context.State.HittableEnemies);
			if (val != null)
			{
				context.Simulator.Damage(val, ((RelicModel)relic).DynamicVars.Damage, ((RelicModel)relic).Owner.Creature);
			}
		}
	}

	private static void HandleLetterOpener(LetterOpener relic, AfterCardPlayedMirrorContext context)
	{
		if (IncrementCounter((RelicModel)(object)relic, relic._skillsPlayedThisTurn, (CardType)2, context))
		{
			context.Simulator.Damage(context.State.HittableEnemies, ((RelicModel)relic).DynamicVars.Damage, ((RelicModel)relic).Owner.Creature);
		}
	}

	private static void HandleKunai(Kunai relic, AfterCardPlayedMirrorContext context)
	{
		if (IncrementCounter((RelicModel)(object)relic, relic._attacksPlayedThisTurn, (CardType)1, context))
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private static void HandleLostWisp(LostWisp relic, AfterCardPlayedMirrorContext context)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		if (context.PreviewCard.Owner == ((RelicModel)relic).Owner && (int)context.PreviewCard.Type == 3)
		{
			context.Simulator.Damage(context.State.HittableEnemies, ((RelicModel)relic).DynamicVars.Damage, ((RelicModel)relic).Owner.Creature);
		}
	}

	private static void HandleMummifiedHand(MummifiedHand relic, AfterCardPlayedMirrorContext context)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		AfterCardPlayedMirrorContext context2 = context;
		if (context2.PreviewCard.Owner == ((RelicModel)relic).Owner && (int)context2.PreviewCard.Type == 3)
		{
			IReadOnlyList<PredictedCard> cards = context2.State.GetPlayerCombatState(((RelicModel)relic).Owner).Hand.Cards;
			List<PredictedCard> list = cards.Where((PredictedCard card) => card.Preview.EnergyCost._base > 0 || card.Preview.BaseStarCost > 0).ToList();
			Rng combatCardSelection = context2.Rng.CombatCardSelection;
			PredictedCard predictedCard = combatCardSelection.NextItem<PredictedCard>(list.Where((PredictedCard card) => card.CostsEnergyOrStars(context2.Simulator))) ?? combatCardSelection.NextItem<PredictedCard>(cards.Where((PredictedCard card) => card.CostsEnergyOrStars(context2.Simulator))) ?? combatCardSelection.NextItem<PredictedCard>((IEnumerable<PredictedCard>)list) ?? combatCardSelection.NextItem<PredictedCard>((IEnumerable<PredictedCard>)cards);
			if (predictedCard != null)
			{
				predictedCard.SetToFreeThisTurn();
				context2.Simulator.History.CardsSelected(new _003C_003Ez__ReadOnlySingleElementList<PredictedCard>(predictedCard));
			}
		}
	}

	private static void HandleMusicBox(MusicBox relic, AfterCardPlayedMirrorContext context)
	{
		MusicBox relic2 = relic;
		MusicBoxPredictionState musicBoxPredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new MusicBoxPredictionState(relic2));
		if (musicBoxPredictionState.CardBeingPlayed == context.Card.Original)
		{
			PredictedCard predictedCard = context.Card.CreateClone();
			predictedCard.MutablePreview.LocalKeywords.Add((CardKeyword)2);
			context.Simulator.AddGeneratedCardToCombat(predictedCard, (PileType)2, ((RelicModel)relic2).Owner, (CardPilePosition)1, CardGenerationResultKind.Contextual);
			musicBoxPredictionState.WasUsedThisTurn = true;
			musicBoxPredictionState.CardBeingPlayed = null;
		}
	}

	private static void HandleNunchaku(Nunchaku relic, AfterCardPlayedMirrorContext context)
	{
		if (IncrementCounter((RelicModel)(object)relic, relic.AttacksPlayed, (CardType)1, context))
		{
			context.Simulator.GainEnergy(((RelicModel)relic).Owner, ((DynamicVar)((RelicModel)relic).DynamicVars.Energy).BaseValue);
		}
	}

	private static void HandleOrnamentalFan(OrnamentalFan relic, AfterCardPlayedMirrorContext context)
	{
		if (IncrementCounter((RelicModel)(object)relic, relic._attacksPlayedThisTurn, (CardType)1, context))
		{
			context.Simulator.GainBlock(((RelicModel)relic).Owner.Creature, ((RelicModel)relic).DynamicVars.Block);
		}
	}

	private static void HandlePermafrost(Permafrost relic, AfterCardPlayedMirrorContext context)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		Permafrost relic2 = relic;
		if (context.PreviewCard.Owner == ((RelicModel)relic2).Owner && (int)context.PreviewCard.Type == 3)
		{
			FlagPredictionState flagPredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new FlagPredictionState(relic2._activatedThisCombat));
			if (!flagPredictionState.Value)
			{
				context.Simulator.GainBlock(((RelicModel)relic2).Owner.Creature, ((RelicModel)relic2).DynamicVars.Block);
				flagPredictionState.Value = true;
			}
		}
	}

	private static void HandlePaelsLegion(PaelsLegion relic, AfterCardPlayedMirrorContext context)
	{
		PaelsLegion relic2 = relic;
		PaelsLegionPredictionState paelsLegionPredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new PaelsLegionPredictionState(relic2));
		if (paelsLegionPredictionState.AffectedCardPlay == context.CardPlay)
		{
			paelsLegionPredictionState.AffectedCardPlay = null;
			paelsLegionPredictionState.Cooldown = ((RelicModel)relic2).DynamicVars["Turns"].IntValue;
			paelsLegionPredictionState.TriggeredBlockLastTurn = true;
		}
	}

	private static void HandlePenNib(PenNib relic, AfterCardPlayedMirrorContext context)
	{
		PenNib relic2 = relic;
		PenNibPredictionState penNibPredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new PenNibPredictionState(relic2));
		if (penNibPredictionState.AttackToDouble == context.Card.Original)
		{
			penNibPredictionState.AttackToDouble = null;
		}
	}

	private static void HandleRazorTooth(RazorTooth relic, AfterCardPlayedMirrorContext context)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Invalid comparison between Unknown and I4
		bool flag = context.PreviewCard.Owner == ((RelicModel)relic).Owner;
		if (flag)
		{
			CardType type = context.PreviewCard.Type;
			bool flag2 = type - 1 <= 1;
			flag = flag2;
		}
		if (flag && context.PreviewCard.IsUpgradable)
		{
			context.Simulator.Upgrade(context.Card);
		}
	}

	private static void HandleRainbowRing(RainbowRing relic, AfterCardPlayedMirrorContext context)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Invalid comparison between Unknown and I4
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Invalid comparison between Unknown and I4
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Invalid comparison between Unknown and I4
		RainbowRing relic2 = relic;
		if (context.PreviewCard.Owner != ((RelicModel)relic2).Owner)
		{
			return;
		}
		RainbowRingPredictionState rainbowRingPredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new RainbowRingPredictionState(relic2));
		if (rainbowRingPredictionState.ActivationCountThisTurn < 1)
		{
			rainbowRingPredictionState.AttacksPlayedThisTurn += (((int)context.PreviewCard.Type == 1) ? 1 : 0);
			rainbowRingPredictionState.SkillsPlayedThisTurn += (((int)context.PreviewCard.Type == 2) ? 1 : 0);
			rainbowRingPredictionState.PowersPlayedThisTurn += (((int)context.PreviewCard.Type == 3) ? 1 : 0);
			if (rainbowRingPredictionState.AttacksPlayedThisTurn > 0 && rainbowRingPredictionState.SkillsPlayedThisTurn > 0 && rainbowRingPredictionState.PowersPlayedThisTurn > 0)
			{
				rainbowRingPredictionState.ActivationCountThisTurn++;
				context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
			}
		}
	}

	private static void HandleTuningFork(TuningFork relic, AfterCardPlayedMirrorContext context)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		TuningFork relic2 = relic;
		if (context.PreviewCard.Owner == ((RelicModel)relic2).Owner && (int)context.PreviewCard.Type == 2)
		{
			CounterPredictionState counterPredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new CounterPredictionState(relic2.SkillsPlayed));
			if (++counterPredictionState.Value >= ((DynamicVar)((RelicModel)relic2).DynamicVars.Cards).IntValue)
			{
				context.Simulator.GainBlock(((RelicModel)relic2).Owner.Creature, ((RelicModel)relic2).DynamicVars.Block);
				counterPredictionState.Value -= ((DynamicVar)((RelicModel)relic2).DynamicVars.Cards).IntValue;
			}
		}
	}

	private static void HandleVambrace(Vambrace relic, AfterCardPlayedMirrorContext context)
	{
		Vambrace relic2 = relic;
		VambracePredictionState vambracePredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new VambracePredictionState(relic2));
		if (context.PreviewCard.Owner == ((RelicModel)relic2).Owner && context.Card.Original == vambracePredictionState.TriggeringCard && !vambracePredictionState.BlockGainedThisCombat)
		{
			vambracePredictionState.BlockGainedThisCombat = true;
		}
	}

	private static void HandleUnsettlingLamp(UnsettlingLamp relic, AfterCardPlayedMirrorContext context)
	{
	}

	private static void HandleVelvetChoker(VelvetChoker relic, AfterCardPlayedMirrorContext context)
	{
		VelvetChoker relic2 = relic;
		if (context.PreviewCard.Owner == ((RelicModel)relic2).Owner)
		{
			context.StateStore.Get((AbstractModel)(object)relic2, () => new CounterPredictionState(relic2._cardsPlayedThisTurn)).Value++;
		}
	}

	private static void HandleShuriken(Shuriken relic, AfterCardPlayedMirrorContext context)
	{
		if (IncrementCounter((RelicModel)(object)relic, relic._attacksPlayedThisTurn, (CardType)1, context))
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private static void HandleAfterimagePower(AfterimagePower power, AfterCardPlayedMirrorContext context)
	{
		int? num = TakePairAmount((AbstractModel)(object)power, context);
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			if (valueOrDefault > 0)
			{
				context.Simulator.GainBlock(((PowerModel)power).Owner, valueOrDefault, (ValueProp)4);
			}
		}
	}

	private static void HandleBlackHolePower(BlackHolePower power, AfterCardPlayedMirrorContext context)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		ResourceInfo resources = context.CardPlay.Resources;
		if (((ResourceInfo)(ref resources)).StarsSpent > 0 && context.PreviewCard.Owner == ((PowerModel)power).Owner.Player && context.CardPlay.IsLastInSeries)
		{
			context.Simulator.Damage(context.State.HittableEnemies, ((PowerModel)power).Amount, (ValueProp)4, ((PowerModel)power).Owner);
		}
	}

	private static void HandleCalamityPower(CalamityPower power, AfterCardPlayedMirrorContext context)
	{
		if (!TakePairAmount((AbstractModel)(object)power, context).HasValue)
		{
			return;
		}
		Player player = ((PowerModel)power).Owner.Player;
		if (player != null)
		{
			List<PredictedCard> cards = (from card in player.GetUnlockedCharacterCards()
				where (int)card.Type == 1
				select card).GetForCombat(player, ((PowerModel)power).Amount, context.Rng.CombatCardGeneration).ToList();
			context.Simulator.AddGeneratedCardsToCombat(cards, (PileType)2, player, (CardPilePosition)1);
		}
	}

	private static void HandleCurlUpPower(CurlUpPower power, AfterCardPlayedMirrorContext context)
	{
		CurlUpPredictionState curlUpPredictionState = context.StateStore.Get<CurlUpPredictionState>((AbstractModel)(object)power);
		if (!curlUpPredictionState.Consumed && curlUpPredictionState.PlayedCard == context.Card.Original)
		{
			curlUpPredictionState.PlayedCard = null;
			context.Simulator.GainBlock(((PowerModel)power).Owner, ((PowerModel)power).Amount, (ValueProp)4);
			curlUpPredictionState.Consumed = true;
		}
	}

	private static void HandleDevourLifePower(DevourLifePower power, AfterCardPlayedMirrorContext context)
	{
		if (context.PreviewCard is Soul && context.PreviewCard.Owner.Creature == ((PowerModel)power).Owner)
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private static void HandleGalvanicPower(GalvanicPower power, AfterCardPlayedMirrorContext context)
	{
		if (context.PreviewCard.Affliction is Galvanized)
		{
			context.Simulator.Damage(context.PreviewCard.Owner.Creature, ((PowerModel)power).Amount, (ValueProp)12, null);
		}
	}

	private static void HandleGravityPower(GravityPower power, AfterCardPlayedMirrorContext context)
	{
		int? num = TakePairAmount((AbstractModel)(object)power, context);
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			if (valueOrDefault > 0)
			{
				context.Simulator.Damage(context.State.HittableEnemies, valueOrDefault, (ValueProp)4, ((PowerModel)power).Owner);
			}
		}
	}

	private static void HandleHauntPower(HauntPower power, AfterCardPlayedMirrorContext context)
	{
		if (context.PreviewCard is Soul && context.PreviewCard.Owner.Creature == ((PowerModel)power).Owner)
		{
			Creature val = context.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)context.State.HittableEnemies);
			if (val != null)
			{
				context.Simulator.Damage(val, ((PowerModel)power).Amount, (ValueProp)6, null);
			}
		}
	}

	private static void HandleMasterPlannerPower(MasterPlannerPower power, AfterCardPlayedMirrorContext context)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		if (context.PreviewCard.Owner == ((PowerModel)power).Owner.Player && (int)context.PreviewCard.Type == 2)
		{
			context.MutablePreviewCard.LocalKeywords.Add((CardKeyword)6);
		}
	}

	private static void HandleImitationLearningPower(ImitationLearningPower power, AfterCardPlayedMirrorContext context)
	{
		ImitationLearningPower power2 = power;
		AfterCardPlayedMirrorContext context2 = context;
		ImitationLearningPredictionState imitationLearningPredictionState = context2.StateStore.Get((AbstractModel)(object)power2, () => new ImitationLearningPredictionState(power2));
		if (imitationLearningPredictionState.Amount > 0)
		{
			int num = imitationLearningPredictionState.CardAndClones.FindIndex(((PredictedCard Card, PredictedCard Clone) pair) => pair.Card == context2.Card);
			if (num >= 0)
			{
				PredictedCard item = imitationLearningPredictionState.CardAndClones[num].Clone;
				imitationLearningPredictionState.CardAndClones.RemoveAt(num);
				imitationLearningPredictionState.Amount--;
				context2.Simulator.AutoPlay(item, null, (AutoPlayType)1);
			}
		}
	}

	private static void HandleMonologuePower(MonologuePower power, AfterCardPlayedMirrorContext context)
	{
		RecordRiskIfPaired((AbstractModel)(object)power, context);
	}

	private static void HandleOblivionPower(OblivionPower power, AfterCardPlayedMirrorContext context)
	{
		RecordRiskIfPaired((AbstractModel)(object)power, context);
	}

	private static void HandlePanachePower(PanachePower power, AfterCardPlayedMirrorContext context)
	{
		PanachePower power2 = power;
		if (context.PreviewCard.Owner == ((PowerModel)power2).Owner.Player)
		{
			PanachePredictionState panachePredictionState = context.StateStore.Get((AbstractModel)(object)power2, () => new PanachePredictionState(power2));
			if (panachePredictionState.AlreadyApplied && --panachePredictionState.CardsLeft <= 0)
			{
				context.Simulator.Damage(context.State.HittableEnemies, ((PowerModel)power2).Amount, (ValueProp)4, ((PowerModel)power2).Owner);
				panachePredictionState.CardsLeft = 5;
			}
			panachePredictionState.AlreadyApplied = true;
		}
	}

	private static void HandleRagePower(RagePower power, AfterCardPlayedMirrorContext context)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		if (context.PreviewCard.Owner == ((PowerModel)power).Owner.Player && (int)context.PreviewCard.Type == 1)
		{
			context.Simulator.GainBlock(((PowerModel)power).Owner, ((PowerModel)power).Amount, (ValueProp)4);
		}
	}

	private static void HandleSerpentFormPower(SerpentFormPower power, AfterCardPlayedMirrorContext context)
	{
		int? num = TakePairAmount((AbstractModel)(object)power, context);
		if (!num.HasValue)
		{
			return;
		}
		int valueOrDefault = num.GetValueOrDefault();
		if (valueOrDefault > 0)
		{
			Creature val = context.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)context.State.HittableEnemies);
			if (val != null)
			{
				context.Simulator.Damage(val, valueOrDefault, (ValueProp)4, ((PowerModel)power).Owner);
			}
		}
	}

	private static void HandleRupturePower(RupturePower power, AfterCardPlayedMirrorContext context)
	{
	}

	private static void HandleSmoggyPower(SmoggyPower power, AfterCardPlayedMirrorContext context)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Invalid comparison between Unknown and I4
		if (context.PreviewCard.Owner.Creature != ((PowerModel)power).Owner || (int)context.PreviewCard.Type != 2)
		{
			return;
		}
		foreach (PredictedCard item in context.State.GetPlayerCombatState(context.PreviewCard.Owner).AllCards.ToList())
		{
			if ((int)item.Preview.Type == 2 && item.Preview.Affliction == null)
			{
				context.Simulator.Afflict<Smog>(item, 1m);
			}
		}
	}

	private static void HandleSlowPower(SlowPower power, AfterCardPlayedMirrorContext context)
	{
		SlowPower power2 = power;
		context.StateStore.Get((AbstractModel)(object)power2, () => new CounterPredictionState(((PowerModel)power2).DynamicVars["SlowAmount"].IntValue)).Value++;
	}

	private static void HandleSneakyPower(SneakyPower power, AfterCardPlayedMirrorContext context)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Invalid comparison between Unknown and I4
		if (context.PreviewCard.Owner.Creature != ((PowerModel)power).Owner && (int)context.PreviewCard.Type == 1)
		{
			context.Simulator.GainBlock(((PowerModel)power).Owner, ((PowerModel)power).Amount, (ValueProp)4);
		}
	}

	private static void HandleStormPower(StormPower power, AfterCardPlayedMirrorContext context)
	{
		int? num = TakePairAmount((AbstractModel)(object)power, context);
		if (!num.HasValue)
		{
			return;
		}
		int valueOrDefault = num.GetValueOrDefault();
		if (valueOrDefault > 0)
		{
			Player player = ((PowerModel)power).Owner.Player;
			if (player != null)
			{
				context.Simulator.OrbChannel<LightningOrb>(player, valueOrDefault);
			}
		}
	}

	private static void HandleStranglePower(StranglePower power, AfterCardPlayedMirrorContext context)
	{
		int? num = TakePairAmount((AbstractModel)(object)power, context);
		if (num.HasValue)
		{
			int valueOrDefault = num.GetValueOrDefault();
			context.Simulator.Damage(((PowerModel)power).Owner, valueOrDefault, (ValueProp)6, null);
		}
	}

	private static void HandleSubroutinePower(SubroutinePower power, AfterCardPlayedMirrorContext context)
	{
		int? num = TakePairAmount((AbstractModel)(object)power, context);
		if (!num.HasValue)
		{
			return;
		}
		int valueOrDefault = num.GetValueOrDefault();
		if (valueOrDefault > 0)
		{
			Player player = ((PowerModel)power).Owner.Player;
			if (player != null)
			{
				context.Simulator.GainEnergy(player, valueOrDefault);
			}
		}
	}

	private static void HandleVoidFormPower(VoidFormPower power, AfterCardPlayedMirrorContext context)
	{
		VoidFormPower power2 = power;
		if (context.PreviewCard.Owner.Creature != ((PowerModel)power2).Owner)
		{
			return;
		}
		CardPlay cardPlay = context.CardPlay;
		if (cardPlay != null && !cardPlay.IsAutoPlay && cardPlay.IsLastInSeries)
		{
			context.StateStore.Get((AbstractModel)(object)power2, () => new VoidFormPredictionState(power2)).CardsPlayedThisTurn++;
		}
	}

	private static void HandleTenderPower(TenderPower power, AfterCardPlayedMirrorContext context)
	{
		if (context.PreviewCard.Owner == ((PowerModel)power).Owner.Player)
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private static void HandleVitalSparkPower(VitalSparkPower power, AfterCardPlayedMirrorContext context)
	{
	}

	private static void HandleWitheringPresencePower(WitheringPresencePower power, AfterCardPlayedMirrorContext context)
	{
		WitheringPresencePower power2 = power;
		Player owner = context.PreviewCard.Owner;
		Creature target = ((PowerModel)power2).Target;
		if (owner == ((target != null) ? target.Player : null))
		{
			CounterPredictionState counterPredictionState = context.StateStore.Get((AbstractModel)(object)power2, () => new CounterPredictionState(((PowerModel)power2).DynamicVars["CardsLeft"].IntValue));
			if (--counterPredictionState.Value <= 0)
			{
				context.Simulator.CreateAndAddGeneratedCardsToCombat<Wither>(context.PreviewCard.Owner, (PileType)2, 1, (Player?)null, (CardPilePosition)1);
				counterPredictionState.Value = 6;
			}
		}
	}

	private static void HandleBansheesCry(BansheesCry card, AfterCardPlayedMirrorContext context)
	{
		if (context.PreviewCard.Owner == ((CardModel)card).Owner && context.Card.GetKeywords(context.Simulator).Contains((CardKeyword)2))
		{
			PredictedCard predictedCard = context.State.FindCard((CardModel)(object)card);
			if (predictedCard != null)
			{
				predictedCard.MutablePreview.EnergyCost.AddThisCombat(-((DynamicVar)((CardModel)card).DynamicVars.Energy).IntValue, false);
			}
		}
	}

	private static void HandlePinpoint(Pinpoint card, AfterCardPlayedMirrorContext context)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		if (context.PreviewCard.Owner == ((CardModel)card).Owner && (int)context.PreviewCard.Type == 2)
		{
			PredictedCard predictedCard = context.State.FindCard((CardModel)(object)card);
			if (predictedCard != null)
			{
				predictedCard.MutablePreview.EnergyCost.AddThisTurn(-1, false);
			}
		}
	}

	private static void HandleGoopy(Goopy enchantment, AfterCardPlayedMirrorContext context)
	{
		if (context.Card.References(((EnchantmentModel)enchantment).Card))
		{
			EnchantmentModel enchantment2 = context.MutablePreviewCard.Enchantment;
			Goopy val = (Goopy)(object)((enchantment2 is Goopy) ? enchantment2 : null);
			if (val != null)
			{
				((EnchantmentModel)val)._amount = ((EnchantmentModel)val)._amount + 1;
			}
		}
	}

	private static void HandleGlam(Glam enchantment, AfterCardPlayedMirrorContext context)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (context.Card.References(((EnchantmentModel)enchantment).Card))
		{
			EnchantmentModel enchantment2 = context.MutablePreviewCard.Enchantment;
			Glam val = (Glam)(object)((enchantment2 is Glam) ? enchantment2 : null);
			if (val != null)
			{
				val._usedThisCombat = true;
				((EnchantmentModel)val)._status = (EnchantmentStatus)1;
			}
		}
	}

	private static void HandleVigorous(Vigorous enchantment, AfterCardPlayedMirrorContext context)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		if (context.Card.References(((EnchantmentModel)enchantment).Card))
		{
			EnchantmentModel enchantment2 = context.MutablePreviewCard.Enchantment;
			Vigorous val = (Vigorous)(object)((enchantment2 is Vigorous) ? enchantment2 : null);
			if (val != null)
			{
				((EnchantmentModel)val)._status = (EnchantmentStatus)1;
			}
		}
	}

	private static void HandleMakeItSo(MakeItSo card, AfterCardPlayedMirrorContext context)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Invalid comparison between Unknown and I4
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Invalid comparison between Unknown and I4
		AfterCardPlayedMirrorContext context2 = context;
		MakeItSo card2 = card;
		if (context2.PreviewCard.Owner != ((CardModel)card2).Owner || (int)context2.PreviewCard.Type != 2)
		{
			return;
		}
		PredictedCard predictedCard = context2.State.FindCard((CardModel)(object)card2);
		if (predictedCard != null)
		{
			SimCardPile? pile = predictedCard.GetPile(context2.State);
			if ((pile == null || (int)pile.Type != 2) && (CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry entry) => ((CombatHistoryEntry)entry).HappenedThisTurn(context2.CombatState) && (int)entry.CardPlay.Card.Type == 2 && entry.CardPlay.Player == ((CardModel)card2).Owner) + context2.History.OfType<CombatPredictionCardPlayFinishedEntry>().Count((CombatPredictionCardPlayFinishedEntry entry) => (int)entry.CardPlay.Card.Type == 2 && entry.CardPlay.Player == ((CardModel)card2).Owner)) % ((DynamicVar)((CardModel)card2).DynamicVars.Cards).IntValue == 0)
			{
				context2.Simulator.AddToPile(predictedCard, (PileType)2, (CardPilePosition)1);
			}
		}
	}

	private static void HandleRightHandHand(RightHandHand card, AfterCardPlayedMirrorContext context)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Invalid comparison between Unknown and I4
		if (context.PreviewCard.Owner != ((CardModel)card).Owner)
		{
			return;
		}
		ResourceInfo resources = context.CardPlay.Resources;
		if (((ResourceInfo)(ref resources)).EnergyValue < ((DynamicVar)((CardModel)card).DynamicVars.Energy).IntValue)
		{
			return;
		}
		PredictedCard predictedCard = context.State.FindCard((CardModel)(object)card);
		if (predictedCard != null)
		{
			SimCardPile? pile = predictedCard.GetPile(context.State);
			if (pile != null && (int)pile.Type == 3)
			{
				context.Simulator.AddToPile(predictedCard, (PileType)2, (CardPilePosition)1);
			}
		}
	}

	private static bool IncrementCounter(RelicModel relic, int initialValue, CardType cardType, AfterCardPlayedMirrorContext context)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (context.PreviewCard.Owner != relic.Owner || context.PreviewCard.Type != cardType)
		{
			return false;
		}
		return ++context.StateStore.Get((AbstractModel)(object)relic, () => new CounterPredictionState(initialValue)).Value % ((DynamicVar)relic.DynamicVars.Cards).IntValue == 0;
	}

	private static int? TakePairAmount(AbstractModel model, AfterCardPlayedMirrorContext context)
	{
		if (!context.StateStore.Get<CardPlayPairPredictionState>(model).Amounts.Remove(context.CardPlay, out var value))
		{
			return null;
		}
		return value;
	}

	private static void RecordRiskIfPaired(AbstractModel model, AfterCardPlayedMirrorContext context)
	{
		if (TakePairAmount(model, context).HasValue)
		{
			context.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}
}
