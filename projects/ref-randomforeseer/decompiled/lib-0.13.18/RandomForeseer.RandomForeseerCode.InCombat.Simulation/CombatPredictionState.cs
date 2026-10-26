using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class CombatPredictionState
{
	[CompilerGenerated]
	private CombatPredictionSimulator _003Csimulator_003EP;

	private readonly Dictionary<Creature, SimCreatureState> _creatures;

	private readonly HashSet<Creature> _removedCreatures;

	private readonly Dictionary<Player, SimPlayerCombatState> _playerCombatStates;

	public ICombatState CombatState { get; }

	public IReadOnlyList<Creature> Allies
	{
		get
		{
			List<Creature> list = new List<Creature>();
			list.AddRange(ExcludeRemoved(CombatState.Allies));
			return new _003C_003Ez__ReadOnlyList<Creature>(list);
		}
	}

	public IReadOnlyList<Creature> Enemies
	{
		get
		{
			List<Creature> list = new List<Creature>();
			list.AddRange(ExcludeRemoved(CombatState.Enemies));
			return new _003C_003Ez__ReadOnlyList<Creature>(list);
		}
	}

	public IReadOnlyList<Creature> Creatures
	{
		get
		{
			List<Creature> list = new List<Creature>();
			list.AddRange(ExcludeRemoved(CombatState.Creatures));
			return new _003C_003Ez__ReadOnlyList<Creature>(list);
		}
	}

	public IReadOnlyList<Creature> PlayerCreatures
	{
		get
		{
			List<Creature> list = new List<Creature>();
			list.AddRange(ExcludeRemoved(CombatState.PlayerCreatures));
			return new _003C_003Ez__ReadOnlyList<Creature>(list);
		}
	}

	public IReadOnlyList<Player> Players => CombatState.Players;

	public IReadOnlyList<Creature> HittableEnemies
	{
		get
		{
			List<Creature> list = new List<Creature>();
			list.AddRange(from enemy in ExcludeRemoved(CombatState.Enemies)
				where GetCreature(enemy).IsHittable
				select enemy);
			return new _003C_003Ez__ReadOnlyList<Creature>(list);
		}
	}

	public CombatPredictionState(CombatPredictionSimulator simulator, ICombatState combatState)
	{
		_003Csimulator_003EP = simulator;
		CombatState = combatState;
		_creatures = new Dictionary<Creature, SimCreatureState>();
		_removedCreatures = new HashSet<Creature>();
		_playerCombatStates = new Dictionary<Player, SimPlayerCombatState>();
		base._002Ector();
	}

	public SimCreatureState GetCreature(Creature creature)
	{
		if (!_creatures.TryGetValue(creature, out SimCreatureState value))
		{
			value = new SimCreatureState(_003Csimulator_003EP, creature);
			_creatures.Add(creature, value);
		}
		return value;
	}

	public IReadOnlyList<Creature> GetOpponentsOf(Creature creature)
	{
		List<Creature> list = new List<Creature>();
		list.AddRange(ExcludeRemoved(CombatState.GetOpponentsOf(creature)));
		return new _003C_003Ez__ReadOnlyList<Creature>(list);
	}

	public IReadOnlyList<Creature> GetTeammatesOf(Creature creature)
	{
		List<Creature> list = new List<Creature>();
		list.AddRange(ExcludeRemoved(CombatState.GetTeammatesOf(creature)));
		return new _003C_003Ez__ReadOnlyList<Creature>(list);
	}

	public IReadOnlyList<Creature> GetCreaturesOnSide(CombatSide side)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		List<Creature> list = new List<Creature>();
		list.AddRange(ExcludeRemoved(CombatState.GetCreaturesOnSide(side)));
		return new _003C_003Ez__ReadOnlyList<Creature>(list);
	}

	public IEnumerable<AbstractModel> IterateHookListeners()
	{
		return CombatState.IterateHookListeners();
	}

	public SimPlayerCombatState GetPlayerCombatState(Player player)
	{
		if (!_playerCombatStates.TryGetValue(player, out SimPlayerCombatState value))
		{
			value = new SimPlayerCombatState(player.PlayerCombatState ?? throw new InvalidOperationException("Player " + player.Creature.Name + " has no combat state to simulate."));
			_playerCombatStates.Add(player, value);
		}
		return value;
	}

	public void RemoveCreature(Creature creature)
	{
		if (Creatures.Contains(creature))
		{
			_removedCreatures.Add(creature);
		}
	}

	public PredictedCard? FindCard(CardModel card)
	{
		return GetPlayerCombatState(card.Owner).FindCard(card);
	}

	private IEnumerable<Creature> ExcludeRemoved(IEnumerable<Creature> creatures)
	{
		return creatures.Where((Creature creature) => !_removedCreatures.Contains(creature));
	}
}
