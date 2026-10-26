using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Achievements;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal static class BeforeCardPlayedMirrors
{
	private static readonly MirrorMethodSpec BeforeCardPlayed = MirrorMethodSpec.Hook("BeforeCardPlayed", new Type[1] { typeof(CardPlay) });

	private static readonly MethodMirrorRegistry<AbstractModel, BeforeCardPlayedMirrorContext> Registry = CreateRegistry();

	public static void Invoke(AbstractModel listener, BeforeCardPlayedMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}

	private static MethodMirrorRegistry<AbstractModel, BeforeCardPlayedMirrorContext> CreateRegistry()
	{
		MethodMirrorRegistry<AbstractModel, BeforeCardPlayedMirrorContext> methodMirrorRegistry = new MethodMirrorRegistry<AbstractModel, BeforeCardPlayedMirrorContext>(BeforeCardPlayed);
		methodMirrorRegistry.RegisterIgnored<SkillSilent1Achievement>();
		methodMirrorRegistry.Register<Stomp>((Action<Stomp, BeforeCardPlayedMirrorContext>)HandleStomp);
		methodMirrorRegistry.Register<AfterimagePower>((Action<AfterimagePower, BeforeCardPlayedMirrorContext>)HandleAfterimagePower);
		methodMirrorRegistry.Register<CalamityPower>((Action<CalamityPower, BeforeCardPlayedMirrorContext>)HandleCalamityPower);
		methodMirrorRegistry.Register<ChainsOfBindingPower>((Action<ChainsOfBindingPower, BeforeCardPlayedMirrorContext>)HandleChainsOfBindingPower);
		methodMirrorRegistry.Register<DanseMacabrePower>((Action<DanseMacabrePower, BeforeCardPlayedMirrorContext>)HandleDanseMacabrePower);
		methodMirrorRegistry.Register<FreeAttackPower>((Action<FreeAttackPower, BeforeCardPlayedMirrorContext>)HandleFreeAttackPower);
		methodMirrorRegistry.Register<FreePowerPower>((Action<FreePowerPower, BeforeCardPlayedMirrorContext>)HandleFreePowerPower);
		methodMirrorRegistry.Register<FreeSkillPower>((Action<FreeSkillPower, BeforeCardPlayedMirrorContext>)HandleFreeSkillPower);
		methodMirrorRegistry.Register<GravityPower>((Action<GravityPower, BeforeCardPlayedMirrorContext>)HandleGravityPower);
		methodMirrorRegistry.Register<ImitationLearningPower>((Action<ImitationLearningPower, BeforeCardPlayedMirrorContext>)HandleImitationLearningPower);
		methodMirrorRegistry.Register<JugglingPower>((Action<JugglingPower, BeforeCardPlayedMirrorContext>)HandleJugglingPower);
		methodMirrorRegistry.Register<MonologuePower>((Action<MonologuePower, BeforeCardPlayedMirrorContext>)HandleMonologuePower);
		methodMirrorRegistry.Register<OblivionPower>((Action<OblivionPower, BeforeCardPlayedMirrorContext>)HandleOblivionPower);
		methodMirrorRegistry.Register<RupturePower>((Action<RupturePower, BeforeCardPlayedMirrorContext>)HandleRupturePower);
		methodMirrorRegistry.Register<SerpentFormPower>((Action<SerpentFormPower, BeforeCardPlayedMirrorContext>)HandleSerpentFormPower);
		methodMirrorRegistry.Register<SlothPower>((Action<SlothPower, BeforeCardPlayedMirrorContext>)HandleSlothPower);
		methodMirrorRegistry.Register<SpiritOfAshPower>((Action<SpiritOfAshPower, BeforeCardPlayedMirrorContext>)HandleSpiritOfAshPower);
		methodMirrorRegistry.Register<StormPower>((Action<StormPower, BeforeCardPlayedMirrorContext>)HandleStormPower);
		methodMirrorRegistry.Register<StranglePower>((Action<StranglePower, BeforeCardPlayedMirrorContext>)HandleStranglePower);
		methodMirrorRegistry.Register<SubroutinePower>((Action<SubroutinePower, BeforeCardPlayedMirrorContext>)HandleSubroutinePower);
		methodMirrorRegistry.Register<SurroundedPower>((Action<SurroundedPower, BeforeCardPlayedMirrorContext>)HandleSurroundedPower);
		methodMirrorRegistry.Register<TheSealedThronePower>((Action<TheSealedThronePower, BeforeCardPlayedMirrorContext>)HandleTheSealedThronePower);
		methodMirrorRegistry.Register<VeilpiercerPower>((Action<VeilpiercerPower, BeforeCardPlayedMirrorContext>)HandleVeilpiercerPower);
		methodMirrorRegistry.RegisterIgnored<ChemicalX>();
		methodMirrorRegistry.Register<IntimidatingHelmet>((Action<IntimidatingHelmet, BeforeCardPlayedMirrorContext>)HandleIntimidatingHelmet);
		methodMirrorRegistry.Register<MusicBox>((Action<MusicBox, BeforeCardPlayedMirrorContext>)HandleMusicBox);
		methodMirrorRegistry.RegisterIgnored<PaelsEye>();
		methodMirrorRegistry.Register<PenNib>((Action<PenNib, BeforeCardPlayedMirrorContext>)HandlePenNib);
		return methodMirrorRegistry;
	}

	private static void HandleStomp(Stomp stomp, BeforeCardPlayedMirrorContext context)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		if (context.PreviewCard.Owner == ((CardModel)stomp).Owner && (int)context.PreviewCard.Type == 1)
		{
			PredictedCard predictedCard = context.State.FindCard((CardModel)(object)stomp);
			if (predictedCard != null)
			{
				predictedCard.MutablePreview.EnergyCost.AddThisTurn(-1, false);
			}
		}
	}

	private static void HandleAfterimagePower(AfterimagePower power, BeforeCardPlayedMirrorContext context)
	{
		SnapshotOwnerCard((PowerModel)(object)power, context, ((PowerModel)power).Amount);
	}

	private static void HandleCalamityPower(CalamityPower power, BeforeCardPlayedMirrorContext context)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)context.PreviewCard.Type == 1)
		{
			SnapshotOwnerCard((PowerModel)(object)power, context, ((PowerModel)power).Amount);
		}
	}

	private static void HandleDanseMacabrePower(DanseMacabrePower power, BeforeCardPlayedMirrorContext context)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (context.PreviewCard.Owner.Creature == ((PowerModel)power).Owner)
		{
			ResourceInfo resources = context.CardPlay.Resources;
			if (((ResourceInfo)(ref resources)).EnergyValue >= ((DynamicVar)((PowerModel)power).DynamicVars.Energy).IntValue)
			{
				context.Simulator.GainBlock(((PowerModel)power).Owner, ((PowerModel)power).Amount, (ValueProp)4);
			}
		}
	}

	private static void HandleChainsOfBindingPower(ChainsOfBindingPower power, BeforeCardPlayedMirrorContext context)
	{
		ChainsOfBindingPower power2 = power;
		if (!context.PreviewCard.IsDupe && context.PreviewCard.Owner.Creature == ((PowerModel)power2).Owner && context.PreviewCard.Affliction is Bound)
		{
			context.StateStore.Get((AbstractModel)(object)power2, () => new ChainsOfBindingPredictionState(power2)).BoundCardPlayed = true;
		}
	}

	private static void HandleFreeAttackPower(FreeAttackPower power, BeforeCardPlayedMirrorContext context)
	{
		ConsumeFreeCardPower((PowerModel)(object)power, (CardType)1, context);
	}

	private static void HandleFreePowerPower(FreePowerPower power, BeforeCardPlayedMirrorContext context)
	{
		ConsumeFreeCardPower((PowerModel)(object)power, (CardType)3, context);
	}

	private static void HandleFreeSkillPower(FreeSkillPower power, BeforeCardPlayedMirrorContext context)
	{
		ConsumeFreeCardPower((PowerModel)(object)power, (CardType)2, context);
	}

	private static void HandleGravityPower(GravityPower power, BeforeCardPlayedMirrorContext context)
	{
		SnapshotOwnerCard((PowerModel)(object)power, context, ((PowerModel)power).Amount);
	}

	private static void HandleJugglingPower(JugglingPower power, BeforeCardPlayedMirrorContext context)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Invalid comparison between Unknown and I4
		JugglingPower power2 = power;
		if (context.PreviewCard.Owner != ((PowerModel)power2).Owner.Player || (int)context.PreviewCard.Type != 1)
		{
			return;
		}
		JugglingPredictionState jugglingPredictionState = context.StateStore.Get((AbstractModel)(object)power2, () => new JugglingPredictionState(power2));
		jugglingPredictionState.AttacksPlayedThisTurn++;
		if (jugglingPredictionState.AttacksPlayedThisTurn == 3)
		{
			for (int i = 0; i < ((PowerModel)power2).Amount; i++)
			{
				context.Simulator.AddGeneratedCardToCombat(context.Card.CreateClone(), (PileType)2, ((PowerModel)power2).Owner.Player, (CardPilePosition)1, CardGenerationResultKind.Contextual);
			}
		}
	}

	private static void HandleImitationLearningPower(ImitationLearningPower power, BeforeCardPlayedMirrorContext context)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		ImitationLearningPower power2 = power;
		if (context.PreviewCard.Owner != power2._playerTarget || (int)context.PreviewCard.Type != 3 || !context.CardPlay.IsFirstInSeries)
		{
			return;
		}
		Player player = ((PowerModel)power2).Owner.Player;
		if (player != null)
		{
			ImitationLearningPredictionState imitationLearningPredictionState = context.StateStore.Get((AbstractModel)(object)power2, () => new ImitationLearningPredictionState(power2));
			if (imitationLearningPredictionState.Amount > 0)
			{
				imitationLearningPredictionState.CardAndClones.Add((context.Card, context.Card.CreateCloneForPlayer(player)));
			}
		}
	}

	private static void HandleMonologuePower(MonologuePower power, BeforeCardPlayedMirrorContext context)
	{
		SnapshotOwnerCard((PowerModel)(object)power, context, ((DynamicVar)((PowerModel)power).DynamicVars.Strength).IntValue);
	}

	private static void HandleOblivionPower(OblivionPower power, BeforeCardPlayedMirrorContext context)
	{
		Creature applier = ((PowerModel)power).Applier;
		if (((applier != null) ? applier.Player : null) == context.PreviewCard.Owner)
		{
			GetPairState((AbstractModel)(object)power, context).Amounts.Add(context.CardPlay, ((PowerModel)power).Amount);
		}
	}

	private static void HandleRupturePower(RupturePower power, BeforeCardPlayedMirrorContext context)
	{
	}

	private static void HandleSerpentFormPower(SerpentFormPower power, BeforeCardPlayedMirrorContext context)
	{
		SnapshotOwnerCard((PowerModel)(object)power, context, ((PowerModel)power).Amount);
	}

	private static void HandleSlothPower(SlothPower power, BeforeCardPlayedMirrorContext context)
	{
		SlothPower power2 = power;
		if (context.PreviewCard.Owner == ((PowerModel)power2).Owner.Player)
		{
			context.StateStore.Get((AbstractModel)(object)power2, () => new CounterPredictionState(power2._cardsPlayedThisTurn)).Value++;
		}
	}

	private static void HandleSpiritOfAshPower(SpiritOfAshPower power, BeforeCardPlayedMirrorContext context)
	{
		if (context.PreviewCard.Owner == ((PowerModel)power).Owner.Player && context.Card.GetKeywords(context.Simulator).Contains((CardKeyword)2))
		{
			context.Simulator.GainBlock(((PowerModel)power).Owner, ((PowerModel)power).Amount, (ValueProp)4);
		}
	}

	private static void HandleStormPower(StormPower power, BeforeCardPlayedMirrorContext context)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)context.PreviewCard.Type == 3)
		{
			SnapshotOwnerCard((PowerModel)(object)power, context, ((PowerModel)power).Amount);
		}
	}

	private static void HandleStranglePower(StranglePower power, BeforeCardPlayedMirrorContext context)
	{
		Creature applier = ((PowerModel)power).Applier;
		if (((applier != null) ? applier.Player : null) == context.PreviewCard.Owner)
		{
			GetPairState((AbstractModel)(object)power, context).Amounts.Add(context.CardPlay, ((PowerModel)power).Amount);
		}
	}

	private static void HandleSubroutinePower(SubroutinePower power, BeforeCardPlayedMirrorContext context)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		if ((int)context.PreviewCard.Type == 3)
		{
			SnapshotOwnerCard((PowerModel)(object)power, context, ((PowerModel)power).Amount);
		}
	}

	private static void HandleTheSealedThronePower(TheSealedThronePower power, BeforeCardPlayedMirrorContext context)
	{
		if (context.PreviewCard.Owner == ((PowerModel)power).Owner.Player)
		{
			context.Simulator.GainStars(context.PreviewCard.Owner, ((PowerModel)power).Amount);
		}
	}

	private static void HandleSurroundedPower(SurroundedPower power, BeforeCardPlayedMirrorContext context)
	{
		SurroundedPower power2 = power;
		Creature target = context.CardPlay.Target;
		if (target != null && context.PreviewCard.Owner == ((PowerModel)power2).Owner.Player)
		{
			SurroundedPredictionState surroundedPredictionState = context.StateStore.Get((AbstractModel)(object)power2, () => new SurroundedPredictionState(power2));
			if (target.HasPower<BackAttackLeftPower>())
			{
				surroundedPredictionState.Facing = (Direction)1;
			}
			else if (target.HasPower<BackAttackRightPower>())
			{
				surroundedPredictionState.Facing = (Direction)0;
			}
		}
	}

	private static void HandleVeilpiercerPower(VeilpiercerPower power, BeforeCardPlayedMirrorContext context)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Invalid comparison between Unknown and I4
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Invalid comparison between Unknown and I4
		bool flag = context.PreviewCard.Owner.Creature == ((PowerModel)power).Owner && context.Card.GetKeywords(context.Simulator).Contains((CardKeyword)2);
		bool flag2;
		if (flag)
		{
			PileType? val = context.Card.GetPile(context.State)?.Type;
			if (val.HasValue)
			{
				PileType valueOrDefault = val.GetValueOrDefault();
				if ((int)valueOrDefault == 2 || (int)valueOrDefault == 5)
				{
					flag2 = true;
					goto IL_0081;
				}
			}
			flag2 = false;
			goto IL_0081;
		}
		goto IL_0083;
		IL_0083:
		if (flag)
		{
			context.StateStore.GetPowerAmount((PowerModel)(object)power).Decrement();
		}
		return;
		IL_0081:
		flag = flag2;
		goto IL_0083;
	}

	private static void HandleIntimidatingHelmet(IntimidatingHelmet relic, BeforeCardPlayedMirrorContext context)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		if (context.PreviewCard.Owner == ((RelicModel)relic).Owner)
		{
			ResourceInfo resources = context.CardPlay.Resources;
			if (((ResourceInfo)(ref resources)).EnergyValue >= ((DynamicVar)((RelicModel)relic).DynamicVars.Energy).IntValue)
			{
				context.Simulator.GainBlock(((RelicModel)relic).Owner.Creature, ((RelicModel)relic).DynamicVars.Block);
			}
		}
	}

	private static void HandleMusicBox(MusicBox relic, BeforeCardPlayedMirrorContext context)
	{
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Invalid comparison between Unknown and I4
		MusicBox relic2 = relic;
		MusicBoxPredictionState musicBoxPredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new MusicBoxPredictionState(relic2));
		if (musicBoxPredictionState.CardBeingPlayed == null && !musicBoxPredictionState.WasUsedThisTurn && context.PreviewCard.Owner == ((RelicModel)relic2).Owner && (int)context.PreviewCard.Type == 1)
		{
			musicBoxPredictionState.CardBeingPlayed = context.Card.Original;
		}
	}

	private static void HandlePenNib(PenNib relic, BeforeCardPlayedMirrorContext context)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		PenNib relic2 = relic;
		if (context.PreviewCard.Owner == ((RelicModel)relic2).Owner && (int)context.PreviewCard.Type == 1)
		{
			PenNibPredictionState penNibPredictionState = context.StateStore.Get((AbstractModel)(object)relic2, () => new PenNibPredictionState(relic2));
			penNibPredictionState.AttacksPlayed = (penNibPredictionState.AttacksPlayed + 1) % 10;
			if (penNibPredictionState.AttacksPlayed == 0)
			{
				penNibPredictionState.AttackToDouble = context.Card.Original;
			}
		}
	}

	private static void ConsumeFreeCardPower(PowerModel power, CardType type, BeforeCardPlayedMirrorContext context)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Invalid comparison between Unknown and I4
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Invalid comparison between Unknown and I4
		bool flag = context.PreviewCard.Owner.Creature == power.Owner && context.PreviewCard.Type == type;
		bool flag2;
		if (flag)
		{
			PileType? val = context.Card.GetPile(context.State)?.Type;
			if (val.HasValue)
			{
				PileType valueOrDefault = val.GetValueOrDefault();
				if ((int)valueOrDefault == 2 || (int)valueOrDefault == 5)
				{
					flag2 = true;
					goto IL_0078;
				}
			}
			flag2 = false;
			goto IL_0078;
		}
		goto IL_007a;
		IL_007a:
		if (flag)
		{
			context.StateStore.GetPowerAmount(power).Decrement();
		}
		return;
		IL_0078:
		flag = flag2;
		goto IL_007a;
	}

	private static void SnapshotOwnerCard(PowerModel power, BeforeCardPlayedMirrorContext context, int amount)
	{
		if (context.PreviewCard.Owner == power.Owner.Player)
		{
			GetPairState((AbstractModel)(object)power, context).Amounts.Add(context.CardPlay, amount);
		}
	}

	private static CardPlayPairPredictionState GetPairState(AbstractModel model, BeforeCardPlayedMirrorContext context)
	{
		return context.StateStore.Get<CardPlayPairPredictionState>(model);
	}
}
