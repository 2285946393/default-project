using System;
using MegaCrit.Sts2.Core.Combat;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal sealed class CombatPredictionSession : IDisposable
{
	private CombatPredictionSimulator? _simulator;

	public CombatPredictionSimulator Simulator
	{
		get
		{
			ObjectDisposedException.ThrowIf(_simulator == null, this);
			return _simulator;
		}
	}

	public CombatPredictionSession(ICombatState combatState)
	{
		ArgumentNullException.ThrowIfNull(combatState, "combatState");
		_simulator = new CombatPredictionSimulator(combatState);
	}

	public void Dispose()
	{
		_simulator = null;
	}
}
