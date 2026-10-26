using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class SimAttackContext : IDisposable
{
	private readonly CombatPredictionSimulator _simulator;

	private readonly AttackCommand _attackCommand;

	private bool _disposed;

	private SimAttackContext(CombatPredictionSimulator simulator, CardPlay cardPlay)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		_simulator = simulator;
		_attackCommand = new AttackCommand(0m).FromCard(cardPlay.Card, cardPlay).TargetingAllOpponents(simulator.State.CombatState);
	}

	public static SimAttackContext Create(CombatPredictionSimulator simulator, CardPlay cardPlay)
	{
		SimAttackContext simAttackContext = new SimAttackContext(simulator, cardPlay);
		HookMirrors.BeforeAttack(simAttackContext._simulator, simAttackContext._attackCommand);
		return simAttackContext;
	}

	public void AddHit(IEnumerable<DamageResult> results)
	{
		_attackCommand.IncrementHitsInternal();
		_attackCommand.AddResultsInternal(results);
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			HookMirrors.AfterAttack(_simulator, _attackCommand);
		}
	}
}
