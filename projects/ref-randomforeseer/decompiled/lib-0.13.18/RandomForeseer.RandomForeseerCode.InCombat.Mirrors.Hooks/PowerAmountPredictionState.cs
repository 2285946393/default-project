using System;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks;

internal sealed class PowerAmountPredictionState
{
	public int Amount { get; set; }

	public bool IsActive => Amount > 0;

	public PowerAmountPredictionState(int amount)
	{
		Amount = amount;
		base._002Ector();
	}

	public void Decrement()
	{
		Decrease(1);
	}

	public void Decrease(int amount)
	{
		Amount = Math.Max(0, Amount - amount);
	}

	public void Consume()
	{
		Amount = 0;
	}
}
