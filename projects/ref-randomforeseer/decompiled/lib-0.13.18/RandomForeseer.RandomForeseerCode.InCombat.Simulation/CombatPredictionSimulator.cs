using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Afflictions.OnPlay;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Enchantments.OnPlay;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Power;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Orbs;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Potions.OnUse;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionSimulator
{
	private const int MaxSimulatedDraws = 100;

	private readonly PredictionTrace _trace = new PredictionTrace();

	private const int MaxSimulatedChanneledOrbs = 1000;

	public CombatPredictionState State { get; }

	public CombatPredictionRngSet Rng { get; }

	public PredictionStateStore StateStore { get; } = new PredictionStateStore();


	public CombatPredictionHistory History { get; }

	public PredictionTraceFrame? CurrentFrame => _trace.Current;

	public bool IsInProgress { get; private set; } = true;


	public bool IsAboutToLose { get; private set; }

	public bool IsEnding => IsCombatEnding();

	public bool IsOverOrEnding
	{
		get
		{
			if (IsInProgress)
			{
				return IsEnding;
			}
			return true;
		}
	}

	public void ExecuteAttack(AttackCommand attackCommand)
	{
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		Creature attacker = attackCommand.Attacker;
		if (attacker == null)
		{
			throw new InvalidOperationException("AttackCommand must have an attacker.");
		}
		if (IsOverOrEnding)
		{
			return;
		}
		AbstractModel modelSource = attackCommand.ModelSource;
		CardModel val = (CardModel)(object)((modelSource is CardModel) ? modelSource : null);
		if (val == null)
		{
			throw new InvalidOperationException("AttackCommand simulation requires a card source.");
		}
		if (!attackCommand.IsSingleTargeted && !attackCommand.IsMultiTargeted)
		{
			throw new InvalidOperationException("AttackCommand must be either single-targeted or multi-targeted.");
		}
		SimCreatureState creature2 = State.GetCreature(attacker);
		if (creature2.IsDead)
		{
			return;
		}
		HookMirrors.BeforeAttack(this, attackCommand);
		int num = HookMirrors.ModifyAttackHitCount(this, attackCommand, attackCommand._hitCount);
		PredictedCard cardSource = State.FindCard(val) ?? new PredictedCard(val);
		for (int i = 0; i < num; i++)
		{
			if (creature2.IsDead)
			{
				break;
			}
			List<Creature> list = (from creature in GetPossibleAttackTargets(attackCommand)
				where State.GetCreature(creature).IsAlive
				select creature).ToList();
			if (list.Count == 0)
			{
				break;
			}
			Creature val2 = SelectSingleAttackTarget(attackCommand, list);
			if (attackCommand.IsRandomlyTargeted && val2 == null)
			{
				break;
			}
			List<Creature> list2;
			if (val2 == null)
			{
				list2 = list;
			}
			else
			{
				int num2 = 1;
				list2 = new List<Creature>(num2);
				CollectionsMarshal.SetCount(list2, num2);
				CollectionsMarshal.AsSpan(list2)[0] = val2;
			}
			IReadOnlyList<DamageResult> readOnlyList = Damage(list2, GetAttackDamageAmount(attackCommand, cardSource, val2), attackCommand.DamageProps, attacker, cardSource, attackCommand.CardPlay);
			attackCommand.AddResultsInternal((IEnumerable<DamageResult>)readOnlyList);
		}
		History.CreatureAttacked(attacker, attackCommand.Results.SelectMany((List<DamageResult> results) => results).ToArray());
		HookMirrors.AfterAttack(this, attackCommand);
	}

	public SimAttackContext CreateAttackContext(CardPlay cardPlay)
	{
		return SimAttackContext.Create(this, cardPlay);
	}

	private IReadOnlyList<Creature> GetPossibleAttackTargets(AttackCommand attackCommand)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		Creature attacker = attackCommand.Attacker;
		if (attacker == null)
		{
			throw new InvalidOperationException("AttackCommand must have an attacker.");
		}
		if (attackCommand.IsSingleTargeted)
		{
			return new _003C_003Ez__ReadOnlySingleElementList<Creature>(attackCommand._singleTarget);
		}
		if (attackCommand.IsMultiTargeted)
		{
			if ((int)attackCommand._sourceType == 2)
			{
				return State.PlayerCreatures;
			}
			return State.GetOpponentsOf(attacker);
		}
		throw new InvalidOperationException("AttackCommand must be either single-targeted or multi-targeted.");
	}

	private Creature? SelectSingleAttackTarget(AttackCommand attackCommand, List<Creature> validTargets)
	{
		if (!attackCommand.IsRandomlyTargeted)
		{
			if (validTargets.Count != 1)
			{
				return null;
			}
			return validTargets[0];
		}
		if (!attackCommand._doesRandomTargetingAllowDuplicates)
		{
			HashSet<Creature> previousReceivers = (from result in attackCommand.Results.SelectMany((List<DamageResult> results) => results)
				select result.Receiver).ToHashSet();
			validTargets = validTargets.Where((Creature creature) => !previousReceivers.Contains(creature)).ToList();
			if (validTargets.Count == 0)
			{
				Entry.Logger.Warn("No valid targets available for randomly-targeted attack.", 1);
				History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
				return null;
			}
		}
		return Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)validTargets);
	}

	private decimal GetAttackDamageAmount(AttackCommand attackCommand, PredictedCard cardSource, Creature? singleTarget)
	{
		return ((CalculatedVar)(object)attackCommand._calculatedDamageVar)?.InvokeCalculate(this, cardSource, singleTarget) ?? attackCommand._damagePerHit;
	}

	public void AutoPlay(PredictedCard card, Creature? target = null, AutoPlayType type = 1, bool skipXCapture = false)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		if (IsOverOrEnding || State.GetCreature(card.Preview.Owner.Creature).IsDead)
		{
			return;
		}
		if (card.GetKeywords(this).Contains((CardKeyword)4) || !HookMirrors.ShouldPlay(this, card, out AbstractModel _, type) || !TryResolveAutoPlayTarget(card, ref target))
		{
			MoveToResultPileWithoutPlaying(card);
			return;
		}
		if (card.GetPile(State) == null)
		{
			AddToPile(card, (PileType)5, (CardPilePosition)1);
		}
		ResourceInfo resources = SpendResources(card, isAutoPlay: true, skipXCapture);
		OnPlayWrapper(card, target, isAutoPlay: true, resources, out PredictionTraceFrame _);
	}

	public void AutoPlayFromDrawPile(Player player, int count, CardPilePosition position, bool forceExhaust = false)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		if (IsOverOrEnding)
		{
			return;
		}
		foreach (PredictedCard item in MoveCardsForAutoPlay(player, count, position))
		{
			if (State.GetCreature(item.Preview.Owner.Creature).IsDead)
			{
				break;
			}
			item.MutablePreview.ExhaustOnNextPlay = forceExhaust;
			AutoPlay(item, null, (AutoPlayType)1);
		}
	}

	private IReadOnlyList<PredictedCard> MoveCardsForAutoPlay(Player player, int count, CardPilePosition position)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected I4, but got Unknown
		List<PredictedCard> list = new List<PredictedCard>(count);
		SimPlayerCombatState playerCombatState = State.GetPlayerCombatState(player);
		SimCardPile drawPile = playerCombatState.DrawPile;
		for (int i = 0; i < count; i++)
		{
			ShuffleIfNecessary(player);
			PredictedCard predictedCard = (position - 1) switch
			{
				1 => drawPile.TopCard, 
				0 => drawPile.BottomCard, 
				2 => Rng.CombatCardSelection.NextItem<PredictedCard>((IEnumerable<PredictedCard>)drawPile.Cards), 
				_ => null, 
			};
			if (predictedCard == null)
			{
				break;
			}
			list.Add(predictedCard);
			AddToPile(predictedCard, playerCombatState.PlayPile, (CardPilePosition)1);
			History.AutoPlayFromDrawPile(predictedCard);
		}
		return list;
	}

	private bool TryResolveAutoPlayTarget(PredictedCard card, ref Creature? target)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Invalid comparison between Unknown and I4
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Invalid comparison between Unknown and I4
		PredictedCard card2 = card;
		TargetType targetType = card2.Preview.TargetType;
		if ((int)targetType != 2)
		{
			if ((int)targetType == 6)
			{
				if (target == null)
				{
					target = Rng.CombatTargets.NextItem<Creature>(State.Allies.Where((Creature ally) => ally.IsPlayer && ally != card2.Preview.Owner.Creature && State.GetCreature(ally).IsAlive));
				}
				return target != null;
			}
			return true;
		}
		if (target == null)
		{
			target = Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)State.HittableEnemies);
		}
		return target != null;
	}

	public void LoseBlock(Creature target, decimal amount, Creature? remover)
	{
		SimCreatureState creature = State.GetCreature(target);
		if (!IsOverOrEnding && !creature.IsDead && !(amount <= 0m))
		{
			int block = creature.Block;
			creature.LoseBlock(amount);
			if (block > 0 && creature.Block <= 0)
			{
				HookMirrors.AfterBlockBroken(this, target, remover);
			}
		}
	}

	public decimal GainBlock(Creature creature, BlockVar blockVar)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		return GainBlock(creature, ((DynamicVar)blockVar).BaseValue, blockVar.Props, null, null);
	}

	public decimal GainBlock(Creature creature, BlockVar blockVar, PredictedCard? cardSource, CardPlay? cardPlay)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		return GainBlock(creature, ((DynamicVar)blockVar).BaseValue, blockVar.Props, cardSource, cardPlay);
	}

	public decimal GainBlock(Creature creature, decimal amount, ValueProp props)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return GainBlock(creature, amount, props, null, null);
	}

	public decimal GainBlock(Creature creature, decimal amount, ValueProp props, PredictedCard? cardSource, CardPlay? cardPlay)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		if (IsOverOrEnding || State.GetCreature(creature).IsDead || amount <= 0m)
		{
			return 0m;
		}
		HookMirrors.BeforeBlockGained(this, creature, amount, props, cardSource);
		List<AbstractModel> modifiers;
		decimal num = HookMirrors.ModifyBlock(this, creature, amount, props, cardSource, cardPlay, out modifiers);
		HookMirrors.AfterModifyingBlockAmount(this, num, cardSource, cardPlay, modifiers);
		if (num <= 0m)
		{
			return 0m;
		}
		State.GetCreature(creature).GainBlock(num);
		HookMirrors.AfterBlockGained(this, creature, num, props, cardSource);
		return num;
	}

	public void AddDuringManualCardPlay(PredictedCard card)
	{
		if (!IsOverOrEnding)
		{
			card.GetPile(State)?.Remove(card);
			State.GetPlayerCombatState(card.Preview.Owner).PlayPile.Add(card);
		}
	}

	public void MoveToResultPileWithoutPlaying(PredictedCard card)
	{
		AddToPile(card, (PileType)5, (CardPilePosition)1);
		if (card.Preview.IsDupe)
		{
			RemoveFromCombat(card);
		}
		else if (card.Preview.ExhaustOnNextPlay || card.GetKeywords(this).Contains((CardKeyword)1))
		{
			Exhaust(card);
		}
		else
		{
			AddToPile(card, (PileType)3, (CardPilePosition)1);
		}
	}

	public void Discard(PredictedCard card)
	{
		DiscardAndDraw(new _003C_003Ez__ReadOnlySingleElementList<PredictedCard>(card), 0);
	}

	public void Discard(IReadOnlyList<PredictedCard> cards)
	{
		DiscardAndDraw(cards, 0);
	}

	public void DiscardAndDraw(IReadOnlyList<PredictedCard> cardsToDiscard, int cardsToDraw)
	{
		if (IsOverOrEnding || (cardsToDiscard.Count == 0 && cardsToDraw == 0))
		{
			return;
		}
		List<PredictedCard> list = new List<PredictedCard>();
		foreach (PredictedCard item in cardsToDiscard)
		{
			if (item.Preview.IsSlyThisTurn)
			{
				list.Add(item);
			}
			AddToPile(item, (PileType)3, (CardPilePosition)1);
			HookMirrors.AfterCardDiscarded(this, item);
		}
		if (cardsToDraw > 0)
		{
			Draw(cardsToDiscard[0].Preview.Owner, cardsToDraw);
		}
		foreach (PredictedCard item2 in list)
		{
			AutoPlay(item2, null, (AutoPlayType)2);
		}
	}

	public void Exhaust(PredictedCard card, bool causedByEthereal = false)
	{
		if (!IsOverOrEnding)
		{
			AddToPile(card, (PileType)4, (CardPilePosition)1);
			HookMirrors.AfterCardExhausted(this, card, causedByEthereal);
		}
	}

	public void ManualPlay(PredictedCard card, Creature? target, out PredictionTraceFrame frame)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		ResourceInfo resources = SpendResources(card, isAutoPlay: false);
		OnPlayWrapper(card, target, isAutoPlay: false, resources, out frame);
	}

	public bool CanPlay(PredictedCard card)
	{
		if (card.GetKeywords(this).Contains((CardKeyword)4))
		{
			return false;
		}
		SimPlayerCombatState playerCombatState = State.GetPlayerCombatState(card.Preview.Owner);
		int num = Math.Max(0, card.GetEnergyCostWithModifiers(this));
		int num2 = Math.Max(0, card.GetStarCostWithModifiers(this));
		if (num > playerCombatState.Energy && HookMirrors.ShouldPayExcessEnergyCostWithStars(this, card.Preview.Owner))
		{
			num2 += 2 * (num - playerCombatState.Energy);
			num = playerCombatState.Energy;
		}
		if (num > playerCombatState.Energy || num2 > playerCombatState.Stars)
		{
			return false;
		}
		if (!HookMirrors.ShouldPlay(this, card, out AbstractModel _, (AutoPlayType)0))
		{
			return false;
		}
		if (!CardIsPlayableMirrors.Invoke(this, card))
		{
			return false;
		}
		return true;
	}

	private ResourceInfo SpendResources(PredictedCard card, bool isAutoPlay, bool skipXCapture = false)
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		SimPlayerCombatState playerCombatState = State.GetPlayerCombatState(card.Preview.Owner);
		int num = card.GetEnergyAmountToSpend(this);
		int num2 = Math.Max(0, card.GetStarCostWithModifiers(this));
		if (!isAutoPlay && num > playerCombatState.Energy && HookMirrors.ShouldPayExcessEnergyCostWithStars(this, card.Preview.Owner))
		{
			num2 += 2 * (num - playerCombatState.Energy);
			num = playerCombatState.Energy;
		}
		if (!skipXCapture)
		{
			if (card.Preview.EnergyCost.CostsX)
			{
				card.MutablePreview.EnergyCost.CapturedXValue = num;
			}
			card.MutablePreview.LastStarsSpent = num2;
		}
		ResourceInfo result;
		if (isAutoPlay)
		{
			result = default(ResourceInfo);
			((ResourceInfo)(ref result)).set_EnergySpent(0);
			((ResourceInfo)(ref result)).set_EnergyValue(num);
			((ResourceInfo)(ref result)).set_StarsSpent(0);
			((ResourceInfo)(ref result)).set_StarValue(num2);
			return result;
		}
		if (num > 0)
		{
			History.EnergySpent(card.Preview.Owner, num);
			playerCombatState.LoseEnergy(num);
		}
		card.MutablePreview.LastStarsSpent = num2;
		if (num2 > 0)
		{
			int stars = playerCombatState.Stars;
			playerCombatState.LoseStars(num2);
			if (playerCombatState.Stars != stars)
			{
				History.StarsModified(card.Preview.Owner, playerCombatState.Stars - stars);
			}
		}
		result = default(ResourceInfo);
		((ResourceInfo)(ref result)).set_EnergySpent(num);
		((ResourceInfo)(ref result)).set_EnergyValue(num);
		((ResourceInfo)(ref result)).set_StarsSpent(num2);
		((ResourceInfo)(ref result)).set_StarValue(num2);
		return result;
	}

	private void OnPlayWrapper(PredictedCard card, Creature? target, bool isAutoPlay, ResourceInfo resources, out PredictionTraceFrame frame)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected O, but got Unknown
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Invalid comparison between Unknown and I4
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Expected I4, but got Unknown
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		using (PushActionSource((AbstractModel)(object)card.Original, PredictionActionKind.CardPlay))
		{
			frame = CurrentFrame ?? throw new UnreachableException("No current frame after pushing action source.");
			CardModel mutablePreview = card.MutablePreview;
			Player owner = mutablePreview.Owner;
			mutablePreview.CurrentTarget = target;
			mutablePreview.CurrentPlayIndex = 0;
			if (isAutoPlay)
			{
				AddToPile(card, (PileType)5, (CardPilePosition)1);
			}
			else
			{
				AddDuringManualCardPlay(card);
			}
			CardLocation resultLocation = CardResultLocationMirrors.GetResultLocation(this, card);
			resultLocation = HookMirrors.ModifyCardPlayResultLocation(this, card, isAutoPlay, resources, resultLocation, out List<AbstractModel> modifiers);
			HookMirrors.AfterModifyingCardPlayResultLocation(this, card, resultLocation, modifiers);
			int num = card.GeneratePlayCount(this, target);
			SimCreatureState creature = State.GetCreature(owner.Creature);
			if (creature.IsDead)
			{
				return;
			}
			for (int i = 0; i < num; i++)
			{
				if (IsOverOrEnding)
				{
					break;
				}
				mutablePreview.CurrentPlayIndex = i;
				CardPlay val = new CardPlay();
				val.set_Card(mutablePreview);
				val.set_Player(owner);
				val.set_Target(target);
				val.set_ResultPile(resultLocation.pileType);
				val.set_Resources(resources);
				val.set_IsAutoPlay(isAutoPlay);
				val.set_PlayIndex(i);
				val.set_PlayCount(num);
				CardPlay cardPlay = val;
				HookMirrors.BeforeCardPlayed(this, card, cardPlay);
				History.CardPlayStarted(card, cardPlay);
				CardOnPlayMirrors.Invoke(this, card, cardPlay);
				if (creature.IsDead)
				{
					return;
				}
				EnchantmentModel enchantment = mutablePreview.Enchantment;
				if (enchantment != null)
				{
					EnchantmentOnPlayMirrors.Invoke(this, card, cardPlay, enchantment);
					if (creature.IsDead)
					{
						return;
					}
				}
				AfflictionModel affliction = mutablePreview.Affliction;
				if (affliction != null)
				{
					AfflictionOnPlayMirrors.Invoke(this, card, target, affliction);
					if (creature.IsDead)
					{
						return;
					}
				}
				History.CardPlayFinished(card, cardPlay, card.GetKeywords(this).Contains((CardKeyword)2));
				HookMirrors.AfterCardPlayed(this, card, cardPlay);
				if (creature.IsDead)
				{
					return;
				}
			}
			if (owner != resultLocation.player && (int)resultLocation.pileType != 0)
			{
				GiveToAnotherPlayer(card, owner, resultLocation.player, resultLocation.pileType, resultLocation.position);
			}
			PileType? val2 = card.GetPile(State)?.Type;
			if (val2.HasValue && (int)val2.GetValueOrDefault() == 5)
			{
				PileType pileType = resultLocation.pileType;
				switch ((int)pileType)
				{
				case 0:
					RemoveFromCombat(card);
					break;
				case 4:
					Exhaust(card);
					break;
				case 1:
				case 2:
				case 3:
					AddToPile(card, resultLocation.pileType, resultLocation.position);
					break;
				default:
					Entry.Logger.Warn($"Unexpected result pile type {resultLocation.pileType} for {((AbstractModel)mutablePreview).Id}.", 1);
					RemoveFromCombat(card);
					break;
				}
			}
			mutablePreview.EnergyCost.AfterCardPlayedCleanup();
			mutablePreview._temporaryStarCosts.RemoveAll((TemporaryCardCost cost) => cost.ClearsWhenCardIsPlayed);
			mutablePreview.CurrentTarget = null;
			mutablePreview.CurrentPlayIndex = 0;
		}
	}

	public T? Afflict<T>(PredictedCard card, decimal amount) where T : AfflictionModel
	{
		AfflictionModel? obj = Afflict(((AfflictionModel)ModelDb.Affliction<T>()).ToMutable(), card, amount);
		return (T)(object)((obj is T) ? obj : null);
	}

	public AfflictionModel? Afflict(AfflictionModel affliction, PredictedCard card, decimal amount)
	{
		if (IsOverOrEnding)
		{
			return null;
		}
		((AbstractModel)affliction).AssertMutable();
		if (!HookMirrors.ShouldAfflict(this, card.Preview, affliction) || !affliction.CanAfflict(card.Preview))
		{
			return null;
		}
		if (card.Preview.Affliction == null)
		{
			card.Afflict(affliction, amount);
			affliction.AfterApplied();
		}
		else
		{
			if (((object)card.Preview.Affliction).GetType() != ((object)affliction).GetType())
			{
				return null;
			}
			AfflictionModel affliction2 = card.MutablePreview.Affliction;
			affliction2._amount += (int)amount;
		}
		History.CardAfflicted(card, affliction);
		return card.Preview.Affliction;
	}

	public bool Upgrade(PredictedCard card)
	{
		if (IsEnding)
		{
			return false;
		}
		card.Upgrade();
		return true;
	}

	public IReadOnlyList<PredictedCard> Draw(Player player, decimal drawCount, bool fromHandDraw = false)
	{
		int drawCount2 = ((drawCount > 0m) ? ((int)Math.Ceiling(drawCount)) : 0);
		return Draw(player, drawCount2, fromHandDraw);
	}

	public IReadOnlyList<PredictedCard> Draw(Player player, int drawCount, bool fromHandDraw = false)
	{
		if (IsOverOrEnding || !HookMirrors.ShouldDraw(this, player, fromHandDraw, out AbstractModel _))
		{
			return Array.Empty<PredictedCard>();
		}
		SimPlayerCombatState playerCombatState = State.GetPlayerCombatState(player);
		List<PredictedCard> list = new List<PredictedCard>();
		for (int i = 0; i < drawCount; i++)
		{
			if (IsOverOrEnding)
			{
				break;
			}
			if (playerCombatState.Hand.Cards.Count >= CardPile.MaxCardsInHand)
			{
				break;
			}
			if (History.Count<CombatPredictionCardDrawnEntry>() >= 100)
			{
				History.RecordRisk(PredictionRiskReason.CardDrawLimitExceeded);
				break;
			}
			ShuffleIfNecessary(player);
			if (playerCombatState.DrawPile.IsEmpty || playerCombatState.Hand.Cards.Count >= CardPile.MaxCardsInHand)
			{
				break;
			}
			PredictedCard predictedCard = playerCombatState.DrawPile.Cards[0];
			list.Add(predictedCard);
			AddToPile(predictedCard, playerCombatState.Hand, (CardPilePosition)1);
			CombatPredictionCardDrawnEntry originalEntry = History.CardDrawn(predictedCard, fromHandDraw);
			HookMirrors.AfterCardDrawn(this, predictedCard, fromHandDraw);
			History.CardDrawResolved(originalEntry, predictedCard);
		}
		return list;
	}

	public void Shuffle(Player player)
	{
		if (!IsOverOrEnding)
		{
			SimPlayerCombatState playerCombatState = State.GetPlayerCombatState(player);
			List<PredictedCard> list = playerCombatState.DiscardPile.Cards.ToList();
			list.AddRange(playerCombatState.DrawPile.Cards);
			ListExtensions.StableShuffle<PredictedCard>(list, Rng.Shuffle);
			HookMirrors.ModifyShuffleOrder(this, player, list, isInitialShuffle: false);
			AddToPile(list, playerCombatState.DrawPile, (CardPilePosition)1);
			if (!IsOverOrEnding)
			{
				HookMirrors.AfterShuffle(this, player);
			}
		}
	}

	private void ShuffleIfNecessary(Player player)
	{
		SimPlayerCombatState playerCombatState = State.GetPlayerCombatState(player);
		if (playerCombatState.DrawPile.IsEmpty && !playerCombatState.DiscardPile.IsEmpty)
		{
			Shuffle(player);
		}
	}

	public void AddToCombat<TCard>(Creature target, PileType pileType, int count, Player? creator, CardPilePosition position = 1) where TCard : CardModel
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		Player val = target.Player ?? target.PetOwner;
		if (val != null && !State.GetCreature(val.Creature).IsDead)
		{
			CreateAndAddGeneratedCardsToCombat<TCard>(val, pileType, count, creator, position);
		}
	}

	public IReadOnlyList<SimCardPileAddResult> CreateAndAddGeneratedCardsToCombat<TCard>(Player player, PileType pileType, int count, Player? creator, CardPilePosition position = 1) where TCard : CardModel
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		List<PredictedCard> list = new List<PredictedCard>();
		for (int i = 0; i < count; i++)
		{
			list.Add(PredictedCard.Create((CardModel)(object)ModelDb.Card<TCard>(), player));
		}
		return AddGeneratedCardsToCombat(list, pileType, creator, position, CardGenerationResultKind.Fixed);
	}

	public SimCardPileAddResult AddGeneratedCardToCombat(PredictedCard card, PileType newPileType, Player? creator, CardPilePosition position = 1, CardGenerationResultKind resultKind = CardGenerationResultKind.Random)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		return AddGeneratedCardsToCombat(new _003C_003Ez__ReadOnlySingleElementList<PredictedCard>(card), newPileType, creator, position, resultKind)[0];
	}

	public IReadOnlyList<SimCardPileAddResult> AddGeneratedCardsToCombat(IReadOnlyList<PredictedCard> cards, PileType newPileType, Player? creator, CardPilePosition position = 1, CardGenerationResultKind resultKind = CardGenerationResultKind.Random)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		if (!IsInProgress || cards.Count == 0)
		{
			return Array.Empty<SimCardPileAddResult>();
		}
		if (!PileTypeExtensions.IsCombatPile(newPileType))
		{
			throw new InvalidOperationException("Generated combat cards can only be added to combat piles.");
		}
		if (cards.Any((PredictedCard card) => card.GetPile(State) != null))
		{
			throw new InvalidOperationException("Generated combat cards cannot already be in a pile.");
		}
		List<SimCardPileAddResult> list = new List<SimCardPileAddResult>();
		foreach (PredictedCard card in cards)
		{
			CombatPredictionCardGeneratedEntry originalEntry = History.CardGenerated(card, resultKind);
			SimCardPileAddResult item = AddToPile(card, newPileType, position);
			list.Add(item);
			HookMirrors.AfterCardGeneratedForCombat(this, card, creator);
			History.CardGenerationResolved(originalEntry, item.Success ? card : null);
		}
		return list;
	}

	public SimCardPileAddResult AddToPile(PredictedCard card, PileType newPileType, CardPilePosition position = 1, bool isChangingOwners = false)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return AddToPile(new _003C_003Ez__ReadOnlySingleElementList<PredictedCard>(card), newPileType, position, isChangingOwners)[0];
	}

	public SimCardPileAddResult AddToPile(PredictedCard card, SimCardPile newPile, CardPilePosition position = 1, bool isChangingOwners = false)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return AddToPile(new _003C_003Ez__ReadOnlySingleElementList<PredictedCard>(card), newPile, position, isChangingOwners)[0];
	}

	public IReadOnlyList<SimCardPileAddResult> AddToPile(IReadOnlyList<PredictedCard> cards, PileType newPileType, CardPilePosition position = 1, bool isChangingOwners = false)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if (cards.Count == 0)
		{
			return Array.Empty<SimCardPileAddResult>();
		}
		SimCardPile newPile = State.GetPlayerCombatState(cards[0].Preview.Owner).GetCardPile(newPileType) ?? throw new InvalidOperationException($"Cannot find combat pile {newPileType} for player {cards[0].Preview.Owner}.");
		return AddToPile(cards, newPile, position, isChangingOwners);
	}

	public IReadOnlyList<SimCardPileAddResult> AddToPile(IReadOnlyList<PredictedCard> cards, SimCardPile newPile, CardPilePosition position = 1, bool isChangingOwners = false)
	{
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Invalid comparison between Unknown and I4
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Expected I4, but got Unknown
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Unknown result type (might be due to invalid IL or missing references)
		if (cards.Count == 0)
		{
			return Array.Empty<SimCardPileAddResult>();
		}
		if (IsEnding)
		{
			List<SimCardPileAddResult> list = new List<SimCardPileAddResult>();
			list.AddRange(cards.Select((PredictedCard card) => new SimCardPileAddResult(success: false, card)));
			return new _003C_003Ez__ReadOnlyList<SimCardPileAddResult>(list);
		}
		Player val = cards[0].Preview.Owner ?? throw new InvalidOperationException("Cannot add cards with no owner to a pile.");
		SimPlayerCombatState playerCombatState = State.GetPlayerCombatState(val);
		List<SimCardPileAddResult> list2 = new List<SimCardPileAddResult>();
		foreach (PredictedCard card in cards)
		{
			if (card.Preview.Owner != val)
			{
				throw new InvalidOperationException("Cannot add cards with different owners to the same pile.");
			}
			SimCardPile? pile = card.GetPile(playerCombatState);
			PileType val2 = (PileType)((pile != null) ? ((int)pile.Type) : 0);
			if (card.Original.HasBeenRemovedFromState || card.Preview.HasBeenRemovedFromState || State.GetCreature(val.Creature).IsDead || ((int)val2 != 0 && !PileTypeExtensions.IsCombatPile(val2)))
			{
				list2.Add(new SimCardPileAddResult(Success: false, card, val2, newPile.Type));
			}
			else
			{
				list2.Add(new SimCardPileAddResult(Success: true, card, val2, newPile.Type));
			}
		}
		foreach (SimCardPileAddResult item in list2)
		{
			if (item.Success)
			{
				PredictedCard cardAdded = item.CardAdded;
				SimCardPile simCardPile = newPile;
				if ((int)simCardPile.Type == 2 && simCardPile.Cards.Count >= CardPile.MaxCardsInHand)
				{
					simCardPile = playerCombatState.DiscardPile;
				}
				cardAdded.GetPile(playerCombatState)?.Remove(cardAdded);
				simCardPile.Insert((position - 1) switch
				{
					0 => simCardPile.Cards.Count, 
					1 => 0, 
					2 => Rng.Shuffle.NextInt(simCardPile.Cards.Count + 1), 
					_ => throw new ArgumentOutOfRangeException("position", position, null), 
				}, cardAdded);
				if ((int)item.OldPileType != 0)
				{
				}
			}
		}
		return list2;
	}

	public void RemoveFromCombat(PredictedCard card)
	{
		RemoveFromCombat(new _003C_003Ez__ReadOnlySingleElementList<PredictedCard>(card));
	}

	public void RemoveFromCombat(IReadOnlyList<PredictedCard> cards)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if (cards.Count == 0)
		{
			return;
		}
		List<(PredictedCard, PileType)> list = new List<(PredictedCard, PileType)>();
		foreach (PredictedCard card in cards)
		{
			SimCardPile simCardPile = card.GetPile(State) ?? throw new InvalidOperationException("Cannot remove card from combat because it is not in a pile.");
			simCardPile.Remove(card);
			list.Add((card, simCardPile.Type));
		}
		foreach (var item in list)
		{
			item.Item1.MutablePreview.HasBeenRemovedFromState = true;
		}
	}

	public void GiveToAnotherPlayer(PredictedCard card, Player originalOwner, Player newOwner, PileType pileType, CardPilePosition position)
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		if (!IsOverOrEnding && !State.GetCreature(newOwner.Creature).IsDead)
		{
			(card.GetPile(State.GetPlayerCombatState(originalOwner)) ?? throw new InvalidOperationException($"Cannot transfer {((AbstractModel)card.Preview).Id} because it is not in {originalOwner}'s combat piles.")).Remove(card);
			card.MutablePreview.GiveToAnotherPlayer(newOwner);
			AddToPile(card, pileType, position, isChangingOwners: true);
		}
	}

	public CombatPredictionSimulator(ICombatState combatState)
	{
		State = new CombatPredictionState(this, combatState);
		Rng = CombatPredictionRngSet.From(combatState.RunState.Rng);
		History = new CombatPredictionHistory(_trace);
	}

	public PredictionRisk Snapshot()
	{
		return History.GetCurrentRisk();
	}

	public void LoseCombat()
	{
		IsAboutToLose = true;
	}

	public bool CheckWinCondition()
	{
		if (!IsAboutToLose && !IsEnding)
		{
			return false;
		}
		IsAboutToLose = false;
		IsInProgress = false;
		return true;
	}

	public IDisposable PushActionSource(AbstractModel model, PredictionActionKind action)
	{
		return _trace.Push(model, PredictionInvocation.ForAction(action));
	}

	public IDisposable PushMethodSource(AbstractModel model, MirrorMethodSpec method)
	{
		return _trace.Push(model, PredictionInvocation.ForMethod(method.BaseMethod));
	}

	private bool IsCombatEnding()
	{
		if (!IsInProgress)
		{
			return false;
		}
		if (IsAboutToLose)
		{
			return true;
		}
		if (!State.Enemies.Any((Creature enemy) => State.GetCreature(enemy).IsAlive && enemy.IsPrimaryEnemy))
		{
			return !HookMirrors.ShouldStopCombatFromEnding(this);
		}
		return false;
	}

	public IReadOnlyList<DamageResult> Damage(Creature target, DamageVar damageVar, Creature? dealer)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		return Damage(new _003C_003Ez__ReadOnlySingleElementList<Creature>(target), ((DynamicVar)damageVar).BaseValue, damageVar.Props, dealer, null, null);
	}

	public IReadOnlyList<DamageResult> Damage(Creature target, decimal amount, ValueProp props, Creature? dealer)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return Damage(new _003C_003Ez__ReadOnlySingleElementList<Creature>(target), amount, props, dealer, null, null);
	}

	public IReadOnlyList<DamageResult> Damage(IReadOnlyList<Creature> targets, DamageVar damageVar, Creature? dealer)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		return Damage(targets, ((DynamicVar)damageVar).BaseValue, damageVar.Props, dealer, null, null);
	}

	public IReadOnlyList<DamageResult> Damage(IReadOnlyList<Creature> targets, decimal amount, ValueProp props, Creature? dealer)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return Damage(targets, amount, props, dealer, null, null);
	}

	public IReadOnlyList<DamageResult> Damage(IReadOnlyList<Creature> targets, decimal amount, ValueProp props, Creature? dealer, PredictedCard? cardSource, CardPlay? cardPlay)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if ((dealer != null && State.GetCreature(dealer).IsDead) || targets.Count == 0)
		{
			return Array.Empty<DamageResult>();
		}
		List<DamageResult> list = new List<DamageResult>();
		foreach (Creature target in targets)
		{
			list.AddRange(DamageTarget(target, amount, props, dealer, cardSource, cardPlay));
		}
		ProcessDamageResults(list, dealer, cardSource);
		return list;
	}

	private IReadOnlyList<DamageResult> DamageTarget(Creature originalTarget, decimal amount, ValueProp props, Creature? dealer, PredictedCard? cardSource, CardPlay? cardPlay)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		SimCreatureState creature = State.GetCreature(originalTarget);
		if (creature.IsDead)
		{
			return Array.Empty<DamageResult>();
		}
		decimal num = HookMirrors.ModifyDamage(this, originalTarget, dealer, amount, props, cardSource, cardPlay);
		HookMirrors.BeforeDamageReceived(this, originalTarget, num, props, dealer, cardSource);
		Player petOwner = originalTarget.PetOwner;
		Creature creature2 = ((petOwner != null) ? petOwner.Creature : null) ?? originalTarget;
		decimal num2 = State.GetCreature(creature2).DamageBlock(num, props);
		decimal amount2 = HookMirrors.ModifyHpLost(this, originalTarget, Math.Max(num - num2, 0m), props, dealer, cardSource, (HpLossHookPhase)1, out List<AbstractModel> _);
		Creature val = HookMirrors.ModifyUnblockedDamageTarget(this, originalTarget, amount2, props, dealer);
		amount2 = HookMirrors.ModifyHpLost(this, val, amount2, props, dealer, cardSource, (HpLossHookPhase)2, out List<AbstractModel> modifiers2);
		HookMirrors.AfterModifyingHpLostAfterOsty(this, modifiers2);
		DamageResult val2 = State.GetCreature(val).LoseHp(amount2, props);
		int num3 = 1;
		List<DamageResult> list = new List<DamageResult>(num3);
		CollectionsMarshal.SetCount(list, num3);
		CollectionsMarshal.AsSpan(list)[0] = val2;
		List<DamageResult> list2 = list;
		bool wasBlockBroken = creature.Block <= 0 && num2 > 0m;
		bool wasFullyBlocked = !((Enum)props).HasFlag((Enum)(object)(ValueProp)2) && (num2 > 0m || creature.Block > 0) && (int)amount2 == 0;
		if (originalTarget == val)
		{
			val2.BlockedDamage = (int)num2;
			val2.WasBlockBroken = wasBlockBroken;
			val2.WasFullyBlocked = wasFullyBlocked;
		}
		else
		{
			List<AbstractModel> modifiers3;
			decimal num4 = HookMirrors.ModifyHpLost(this, originalTarget, val2.OverkillDamage, props, dealer, cardSource, (HpLossHookPhase)2, out modifiers3);
			HookMirrors.AfterModifyingHpLostAfterOsty(this, modifiers3);
			DamageResult val3 = (DamageResult)((num4 > 0m) ? ((object)creature.LoseHp(num4, props)) : ((object)new DamageResult(originalTarget, props)));
			val3.BlockedDamage = (int)num2;
			val3.WasBlockBroken = wasBlockBroken;
			val3.WasFullyBlocked = wasFullyBlocked;
			list2.Add(val3);
		}
		foreach (DamageResult item in list2)
		{
			History.DamageReceived(item.Receiver, dealer, item, cardSource);
		}
		return list2;
	}

	private void ProcessDamageResults(IEnumerable<DamageResult> results, Creature? dealer, PredictedCard? cardSource)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		List<Creature> list = new List<Creature>();
		foreach (DamageResult result in results)
		{
			Creature receiver = result.Receiver;
			if (result.WasBlockBroken)
			{
				HookMirrors.AfterBlockBroken(this, receiver, dealer);
			}
			if (result.UnblockedDamage > 0)
			{
				HookMirrors.AfterCurrentHpChanged(this, receiver, -result.UnblockedDamage);
			}
			HookMirrors.AfterDamageGiven(this, receiver, result, result.Props, dealer, cardSource);
			if (!result.WasTargetKilled || !State.GetCreature(receiver).IsDead)
			{
				HookMirrors.AfterDamageReceived(this, receiver, result, result.Props, dealer, cardSource);
			}
			else
			{
				list.Add(receiver);
			}
		}
		Kill((IReadOnlyList<Creature>)list, force: false);
	}

	public void Kill(Creature creature, bool force = false)
	{
		Kill((IReadOnlyList<Creature>)new _003C_003Ez__ReadOnlySingleElementList<Creature>(creature), force);
	}

	public void Kill(IReadOnlyList<Creature> creatures, bool force = false)
	{
		foreach (Creature creature in creatures)
		{
			KillWithoutCheckingWinCondition(creature, force);
		}
		if (State.Players.All((Player player) => State.GetCreature(player.Creature).IsDead))
		{
			LoseCombat();
		}
	}

	private void KillWithoutCheckingWinCondition(Creature creature, bool force)
	{
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Invalid comparison between Unknown and I4
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Invalid comparison between Unknown and I4
		SimCreatureState creature2 = State.GetCreature(creature);
		int currentHp = creature2.CurrentHp;
		if (currentHp > 0)
		{
			creature2.LoseHp(currentHp, (ValueProp)6);
			HookMirrors.AfterCurrentHpChanged(this, creature, -currentHp);
		}
		HookMirrors.BeforeDeath(this, creature);
		if (force || creature.MaxHp <= 0 || HookMirrors.ShouldDie(this, creature, out AbstractModel preventer))
		{
			bool num = HookMirrors.ShouldCreatureBeRemovedFromCombatAfterDeath(this, creature);
			HookMirrors.AfterDeath(this, creature, wasRemovalPrevented: false);
			Creature[] array = (from c in State.GetTeammatesOf(creature)
				where State.GetCreature(c).IsAlive
				select c).ToArray();
			if (num && (int)creature.Side == 2 && State.Enemies.Contains(creature))
			{
				State.RemoveCreature(creature);
			}
			bool isPrimaryEnemy = creature.IsPrimaryEnemy;
			if ((int)creature.Side == 2)
			{
				if (isPrimaryEnemy && array.Length != 0 && array.All((Creature c) => c.IsSecondaryEnemy))
				{
					Kill((IReadOnlyList<Creature>)array, force: false);
				}
				return;
			}
			Player player = creature.Player;
			if (player != null)
			{
				HandlePlayerDeath(player);
			}
		}
		else
		{
			HookMirrors.AfterDeath(this, creature, wasRemovalPrevented: true);
			HookMirrors.AfterPreventingDeath(this, preventer, creature);
		}
	}

	private void HandlePlayerDeath(Player player)
	{
		SimPlayerCombatState playerCombatState = State.GetPlayerCombatState(player);
		playerCombatState.OrbQueue.Clear();
		Creature osty = player.Osty;
		if (osty != null && State.GetCreature(osty).IsAlive)
		{
			Kill(osty, force: true);
		}
		if (!State.Players.All((Player p) => State.GetCreature(p.Creature).IsDead))
		{
			List<PredictedCard> list = new List<PredictedCard>();
			list.AddRange(playerCombatState.AllCards);
			RemoveFromCombat(new _003C_003Ez__ReadOnlyList<PredictedCard>(list));
			playerCombatState.LoseEnergy(playerCombatState.Energy);
			playerCombatState.LoseStars(playerCombatState.Stars);
		}
	}

	public void SimulateEndPlayerTurn()
	{
		IReadOnlyList<Player> playersTakingExtraTurn = CombatManager.Instance.PlayersTakingExtraTurn;
		IReadOnlyList<Player> readOnlyList = ((playersTakingExtraTurn == null || playersTakingExtraTurn.Count <= 0) ? State.CombatState.Players : playersTakingExtraTurn);
		IReadOnlyList<Player> playersEndingTurn = readOnlyList;
		SimulateEndPlayerTurnPhaseOne(playersEndingTurn);
		if (IsInProgress)
		{
			SimulateEndPlayerTurnPhaseTwo(playersEndingTurn);
		}
	}

	private void SimulateEndPlayerTurnPhaseOne(IReadOnlyList<Player> playersEndingTurn)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		foreach (Player item in playersEndingTurn)
		{
			HookMirrors.AfterAutoPostPlayPhaseEntered(this, item);
		}
		CombatSide currentSide = State.CombatState.CurrentSide;
		List<Creature> list = new List<Creature>();
		list.AddRange(playersEndingTurn.Select((Player player) => player.Creature));
		HookMirrors.BeforeSideTurnEnd(this, currentSide, new _003C_003Ez__ReadOnlyList<Creature>(list));
		if (CheckWinCondition())
		{
			return;
		}
		foreach (Player item2 in playersEndingTurn)
		{
			DoTurnEnd(item2);
		}
		if (CheckWinCondition())
		{
			return;
		}
		foreach (Player item3 in playersEndingTurn)
		{
			HookMirrors.BeforeFlush(this, item3);
		}
		CheckWinCondition();
	}

	private void SimulateEndPlayerTurnPhaseTwo(IReadOnlyList<Player> playersEndingTurn)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		foreach (Player item in playersEndingTurn)
		{
			FlushPlayerHand(item);
		}
		CombatSide currentSide = State.CombatState.CurrentSide;
		List<Creature> list = new List<Creature>();
		list.AddRange(playersEndingTurn.Select((Player player) => player.Creature));
		HookMirrors.AfterSideTurnEnd(this, currentSide, new _003C_003Ez__ReadOnlyList<Creature>(list));
	}

	private void FlushPlayerHand(Player player)
	{
		if (!State.GetCreature(player.Creature).IsAlive || player.PlayerCombatState == null)
		{
			return;
		}
		SimPlayerCombatState playerCombatState = State.GetPlayerCombatState(player);
		bool flag = HookMirrors.ShouldFlush(this, player);
		List<PredictedCard> list = new List<PredictedCard>();
		List<PredictedCard> list2 = new List<PredictedCard>();
		foreach (PredictedCard item in playerCombatState.Hand.Cards.ToList())
		{
			if (!flag || item.GetKeywords(this).Contains((CardKeyword)5) || item.Preview._hasSingleTurnRetain)
			{
				list2.Add(item);
			}
			else
			{
				list.Add(item);
			}
		}
		if (list.Count > 0)
		{
			AddToPile(list, playerCombatState.DiscardPile, (CardPilePosition)1);
		}
		HookMirrors.AfterFlush(this, player, list, list2);
	}

	private void DoTurnEnd(Player player)
	{
		SimPlayerCombatState playerCombatState = State.GetPlayerCombatState(player);
		playerCombatState.OrbQueue.BeforeTurnEnd(this);
		if (IsOverOrEnding)
		{
			return;
		}
		List<PredictedCard> list = new List<PredictedCard>();
		List<PredictedCard> list2 = new List<PredictedCard>();
		foreach (PredictedCard card in playerCombatState.Hand.Cards)
		{
			if (card.Preview.HasTurnEndInHandEffect)
			{
				list.Add(card);
			}
			else if (card.GetKeywords(this).Contains((CardKeyword)2) && HookMirrors.ShouldEtherealTrigger(this, card.Preview))
			{
				list2.Add(card);
			}
		}
		foreach (PredictedCard item in list2)
		{
			Exhaust(item, causedByEthereal: true);
		}
		DoTurnEndCards(list);
	}

	private void DoTurnEndCards(IEnumerable<PredictedCard> cards)
	{
		foreach (PredictedCard card in cards)
		{
			AddToPile(card, (PileType)5, (CardPilePosition)1);
			CardOnTurnEndInHandMirrors.Invoke(this, card);
			if (card.GetKeywords(this).Contains((CardKeyword)2))
			{
				Exhaust(card, causedByEthereal: true);
			}
			else
			{
				AddToPile(card, (PileType)3, (CardPilePosition)1);
			}
		}
	}

	public void GainEnergy(Player player, decimal amount)
	{
		if (!IsEnding && !(amount <= 0m))
		{
			IEnumerable<AbstractModel> modifiers;
			decimal num = HookMirrors.ModifyEnergyGain(this, player, amount, out modifiers);
			if (num > 0m)
			{
				State.GetPlayerCombatState(player).GainEnergy(num);
			}
		}
	}

	public void LoseEnergy(Player player, decimal amount)
	{
		if (!IsEnding && !(amount <= 0m))
		{
			State.GetPlayerCombatState(player).LoseEnergy(amount);
		}
	}

	public void GainStars(Player player, decimal amount)
	{
		if (!IsEnding && HookMirrors.ShouldGainStars(this, amount, player))
		{
			SimPlayerCombatState playerCombatState = State.GetPlayerCombatState(player);
			int stars = playerCombatState.Stars;
			playerCombatState.GainStars(amount);
			if (playerCombatState.Stars != stars)
			{
				History.StarsModified(player, playerCombatState.Stars - stars);
			}
		}
	}

	public void Heal(Creature creature, decimal amount)
	{
		if (!IsEnding || creature.IsPlayer)
		{
			State.GetCreature(creature).Heal(amount);
			if (amount > 0m)
			{
				HookMirrors.AfterCurrentHpChanged(this, creature, amount);
			}
		}
	}

	internal void TriggerOrbPassive(OrbModel orb, Creature? target)
	{
		List<AbstractModel> modifiers;
		int num = HookMirrors.ModifyOrbPassiveTriggerCount(this, orb, 1, out modifiers);
		for (int i = 0; i < num; i++)
		{
			OrbMirrors.InvokePassive(this, orb, target);
		}
	}

	public void OrbChannel<T>(Player player, int count = 1) where T : OrbModel
	{
		for (int i = 0; i < count; i++)
		{
			if (!OrbChannel(player, ((OrbModel)ModelDb.Orb<T>()).ToMutable(0)))
			{
				break;
			}
		}
	}

	public bool OrbChannel(Player player, OrbModel orb)
	{
		if (IsOverOrEnding)
		{
			return false;
		}
		if (History.Count<CombatPredictionOrbChanneledEntry>() >= 1000)
		{
			History.RecordRisk(PredictionRiskReason.OrbChannelLimitExceeded);
			return false;
		}
		SimOrbQueue orbQueue = State.GetPlayerCombatState(player).OrbQueue;
		if (player.Character.BaseOrbSlotCount == 0 && orbQueue.Capacity == 0)
		{
			orbQueue.AddCapacity(1);
		}
		((AbstractModel)orb).AssertMutable();
		orb.Owner = player;
		if (orbQueue.Capacity > 0 && orbQueue.Orbs.Count >= orbQueue.Capacity)
		{
			OrbEvokeNext(player);
			if (orbQueue.Orbs.Count >= orbQueue.Capacity)
			{
				History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
				return false;
			}
		}
		if (!orbQueue.TryEnqueue(orb))
		{
			return false;
		}
		History.OrbChanneled(orb);
		HookMirrors.AfterOrbChanneled(this, player, orb);
		return true;
	}

	public void OrbEvokeNext(Player player, int repeat = 1, bool dequeue = true)
	{
		SimOrbQueue orbQueue = State.GetPlayerCombatState(player).OrbQueue;
		for (int i = 0; i < repeat; i++)
		{
			if (orbQueue.Orbs.Count <= 0)
			{
				break;
			}
			OrbEvoke(player, orbQueue.Orbs[0], dequeue && i == repeat - 1);
		}
	}

	public void OrbEvokeLast(Player player, bool dequeue = true)
	{
		SimOrbQueue orbQueue = State.GetPlayerCombatState(player).OrbQueue;
		if (orbQueue.Orbs.Count > 0)
		{
			IReadOnlyList<OrbModel> orbs = orbQueue.Orbs;
			OrbEvoke(player, orbs[orbs.Count - 1], dequeue);
		}
	}

	public void OrbEvoke(Player player, OrbModel evokedOrb, bool dequeue = true)
	{
		if (IsOverOrEnding)
		{
			return;
		}
		SimOrbQueue orbQueue = State.GetPlayerCombatState(player).OrbQueue;
		if (orbQueue.Orbs.Count > 0)
		{
			if (dequeue)
			{
				orbQueue.Remove(evokedOrb);
			}
			IReadOnlyList<Creature> targets = OrbMirrors.InvokeEvoke(this, evokedOrb);
			HookMirrors.AfterOrbEvoked(this, evokedOrb, targets);
		}
	}

	public void OrbPassive(OrbModel orb, Creature? target = null)
	{
		if (!IsOverOrEnding)
		{
			OrbMirrors.InvokePassive(this, orb, target);
		}
	}

	public bool ManualUse(PotionModel potion, Creature? target, [NotNullWhen(true)] out PredictionTraceFrame? frame)
	{
		Creature creature = potion.Owner.Creature;
		if (target == null && potion.IsValidTarget(creature))
		{
			target = creature;
		}
		if (!potion.IsValidTarget(target))
		{
			frame = null;
			return false;
		}
		OnUseWrapper(potion, target, out frame);
		return true;
	}

	private void OnUseWrapper(PotionModel potion, Creature? target, out PredictionTraceFrame frame)
	{
		using (PushActionSource((AbstractModel)(object)potion, PredictionActionKind.PotionUse))
		{
			frame = CurrentFrame ?? throw new UnreachableException("No current frame after pushing action source.");
			PotionOnUseMirrors.Invoke(this, potion, target);
			_ = State.GetCreature(potion.Owner.Creature).IsAlive;
		}
	}

	public IReadOnlyList<T> ApplyPower<T>(IReadOnlyList<Creature> targets, decimal amount, Creature? applier, PredictedCard? cardSource) where T : PowerModel
	{
		List<T> list = new List<T>();
		foreach (Creature target in targets)
		{
			T val = ApplyPower<T>(target, amount, applier, cardSource);
			if (val != null)
			{
				list.Add(val);
			}
		}
		return list;
	}

	public T? ApplyPower<T>(Creature target, decimal amount, Creature? applier, PredictedCard? cardSource) where T : PowerModel
	{
		if (IsEnding || !State.GetCreature(target).CanReceivePowers)
		{
			return default(T);
		}
		T val = ModelDb.Power<T>();
		PowerModel val2 = PowerCmd.FindExistingInstanceForStacking((PowerModel)(object)val, target, applier);
		if (val2 == null)
		{
			val2 = ((PowerModel)val).ToMutable(0);
			ApplyPower(val2, target, amount, applier, cardSource);
		}
		else if (ModifyPowerAmount(val2, amount, applier, cardSource) == 0)
		{
			val2 = null;
		}
		return (T)(object)((val2 is T) ? val2 : null);
	}

	public void ApplyPower(PowerModel power, Creature target, decimal amount, Creature? applier, PredictedCard? cardSource)
	{
		if (IsEnding || amount == 0m || !State.GetCreature(target).CanReceivePowers)
		{
			return;
		}
		PowerModel val = PowerCmd.FindExistingInstanceForStacking(power, target, applier);
		if (val != null)
		{
			ModifyPowerAmount(val, amount, applier, cardSource);
			return;
		}
		((AbstractModel)power).AssertMutable();
		power.Applier = applier;
		List<AbstractModel> givenModifiers;
		List<AbstractModel> receivedModifiers;
		decimal num = ResolvePowerAmountChange(power, target, amount, applier, cardSource, out givenModifiers, out receivedModifiers);
		if (State.Players.Count > 1 && target.IsEnemy && power.ShouldScaleInMultiplayer)
		{
			num = power.GetScaledAmountForMultiplayer(State.CombatState, applier, num, target, cardSource?.Preview);
		}
		PowerAppliedMirrors.InvokeBefore(this, power, target, num, applier, cardSource);
		if (State.GetCreature(target).CanReceivePowers)
		{
			if (num != 0m)
			{
				StateStore.GetPowerAmount(power).Amount = Math.Clamp((int)num, -999999999, 999999999);
				History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
			}
			AfterModifyingPowerAmount(power, givenModifiers, receivedModifiers);
			if (!(num == 0m))
			{
				PowerAppliedMirrors.InvokeAfter(this, power, target, num, applier, cardSource);
				HookMirrors.AfterPowerAmountChanged(this, power, num, target, applier, cardSource);
			}
		}
	}

	public int ModifyPowerAmount(PowerModel power, decimal offset, Creature? applier, PredictedCard? cardSource)
	{
		if (IsEnding)
		{
			return 0;
		}
		Creature owner = power.Owner;
		if (!State.Creatures.Contains(owner))
		{
			return 0;
		}
		List<AbstractModel> givenModifiers;
		List<AbstractModel> receivedModifiers;
		decimal num = ResolvePowerAmountChange(power, owner, offset, applier, cardSource, out givenModifiers, out receivedModifiers);
		PowerAmountPredictionState powerAmount = StateStore.GetPowerAmount(power);
		int num2 = powerAmount.Amount + (int)num;
		powerAmount.Amount = Math.Clamp(num2, -999999999, 999999999);
		if ((int)num != 0)
		{
			History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
		AfterModifyingPowerAmount(power, givenModifiers, receivedModifiers);
		if ((int)num != 0)
		{
			HookMirrors.AfterPowerAmountChanged(this, power, num, owner, applier, cardSource);
		}
		return num2;
	}

	public void RemovePower<T>(Creature creature) where T : PowerModel
	{
		RemovePower((PowerModel?)(object)creature.GetPower<T>());
	}

	public void RemovePower(PowerModel? power)
	{
		if (power != null)
		{
			StateStore.GetPowerAmount(power).Consume();
			History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
		}
	}

	private decimal ResolvePowerAmountChange(PowerModel power, Creature target, decimal amount, Creature? applier, PredictedCard? cardSource, out List<AbstractModel>? givenModifiers, out List<AbstractModel> receivedModifiers)
	{
		HookMirrors.BeforePowerAmountChanged(this, power, amount, target, applier, cardSource);
		givenModifiers = null;
		if (applier != null && State.Creatures.Contains(applier))
		{
			amount = HookMirrors.ModifyPowerAmountGiven(this, power, applier, amount, target, cardSource, out givenModifiers);
		}
		amount = HookMirrors.ModifyPowerAmountReceived(this, power, target, amount, applier, out receivedModifiers);
		return amount;
	}

	private void AfterModifyingPowerAmount(PowerModel power, List<AbstractModel>? givenModifiers, List<AbstractModel> receivedModifiers)
	{
		if (givenModifiers != null)
		{
			HookMirrors.AfterModifyingPowerAmountGiven(this, givenModifiers, power);
		}
		HookMirrors.AfterModifyingPowerAmountReceived(this, receivedModifiers, power);
	}
}
