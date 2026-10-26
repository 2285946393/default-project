using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Orbs;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Orbs;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class SimOrbQueue
{
	private readonly List<OrbModel> _orbs = ((IEnumerable<OrbModel>)liveOrbQueue.Orbs).Select((Func<OrbModel, OrbModel>)((OrbModel orb) => (OrbModel)((AbstractModel)orb).MutableClone())).ToList();

	public IReadOnlyList<OrbModel> Orbs => _orbs;

	public int Capacity { get; private set; } = liveOrbQueue.Capacity;


	public SimOrbQueue(OrbQueue liveOrbQueue)
	{
	}

	public void Clear()
	{
		_orbs.Clear();
		Capacity = 0;
	}

	public void AddCapacity(int capacity)
	{
		Capacity += capacity;
	}

	public void RemoveCapacity(int capacity)
	{
		Capacity = Math.Max(0, Capacity - capacity);
		while (Orbs.Count > Capacity)
		{
			Remove(_orbs.Last());
		}
	}

	public bool Remove(OrbModel orb)
	{
		return _orbs.Remove(orb);
	}

	public bool TryEnqueue(OrbModel orb)
	{
		if (Capacity == 0)
		{
			return false;
		}
		((AbstractModel)orb).AssertMutable();
		if (Orbs.Count >= Capacity)
		{
			throw new InvalidOperationException("OrbQueue is full");
		}
		_orbs.Add(orb);
		return true;
	}

	public void Insert(int idx, OrbModel orb)
	{
		if (idx >= Capacity)
		{
			throw new InvalidOperationException("idx cannot be greater than capacity");
		}
		_orbs.Insert(idx, orb);
	}

	public void BeforeTurnEnd(CombatPredictionSimulator simulator)
	{
		foreach (OrbModel item in Orbs.ToList())
		{
			OrbMirrors.InvokeBeforeTurnEndOrbTrigger(simulator, item);
		}
	}
}
