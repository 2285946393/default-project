using System;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class SimCreatureState
{
	[CompilerGenerated]
	private CombatPredictionSimulator _003Csimulator_003EP;

	public Creature Creature { get; }

	public int CurrentHp { get; private set; }

	public int MaxHp { get; }

	public int Block { get; private set; }

	public HpDisplay HpDisplay { get; set; }

	public bool IsAlive => CurrentHp > 0;

	public bool IsDead => !IsAlive;

	public bool IsHittable
	{
		get
		{
			if (IsAlive)
			{
				return HookMirrors.ShouldAllowHitting(_003Csimulator_003EP, Creature);
			}
			return false;
		}
	}

	public bool CanReceivePowers
	{
		get
		{
			if (_003Csimulator_003EP.State.Creatures.Contains(Creature))
			{
				return HookMirrors.ShouldAllowHitting(_003Csimulator_003EP, Creature);
			}
			return false;
		}
	}

	public SimCreatureState(CombatPredictionSimulator simulator, Creature creature)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		_003Csimulator_003EP = simulator;
		Creature = creature;
		CurrentHp = creature.CurrentHp;
		MaxHp = creature.MaxHp;
		Block = creature.Block;
		HpDisplay = creature.HpDisplay;
		base._002Ector();
	}

	public decimal DamageBlock(decimal amount, ValueProp props)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		decimal num = (((Enum)props).HasFlag((Enum)(object)(ValueProp)2) ? 0m : Math.Min(Block, amount));
		Block -= (int)num;
		return num;
	}

	public DamageResult LoseHp(decimal amount, ValueProp props)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected O, but got Unknown
		bool flag = CurrentHp > 0 && amount >= (decimal)CurrentHp;
		int currentHp = CurrentHp;
		int num = (int)Math.Min(amount, 999999999m);
		CurrentHp = Math.Max(CurrentHp - num, 0);
		DamageResult val = new DamageResult(Creature, props);
		val.set_UnblockedDamage(currentHp - CurrentHp);
		val.set_WasTargetKilled(flag);
		val.set_OverkillDamage(flag ? Math.Max(num - currentHp, 0) : 0);
		return val;
	}

	public void LoseBlock(decimal amount)
	{
		if (amount < 0m)
		{
			throw new ArgumentException("amount must be positive. Use GainBlock for block gain.", "amount");
		}
		Block = (int)Math.Max((decimal)Block - amount, 0m);
	}

	public void GainBlock(decimal amount)
	{
		if (amount < 0m)
		{
			throw new ArgumentException("amount must be positive. Use LoseBlock for block loss.", "amount");
		}
		Block = (int)Math.Min((decimal)Block + amount, 999999999m);
	}

	public void Heal(decimal amount)
	{
		if (amount < 0m)
		{
			throw new ArgumentException("amount must be positive.", "amount");
		}
		CurrentHp = (int)Math.Min((decimal)CurrentHp + amount, MaxHp);
	}
}
