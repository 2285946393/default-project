using System;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks;

internal static class PredictionStateStorePowerAmountExtensions
{
	[SpecialName]
	public sealed class _003CG_003E_0024528957721750EC65C62FF45CF9F7CB9D
	{
		[SpecialName]
		public static class _003CM_003E_0024FDD4E7A69A8250276D50ABD000234538
		{
		}

		[ExtensionMarker("<M>$FDD4E7A69A8250276D50ABD000234538")]
		public PowerAmountPredictionState GetPowerAmount(PowerModel power)
		{
			throw new NotSupportedException();
		}
	}

	public static PowerAmountPredictionState GetPowerAmount(this PredictionStateStore store, PowerModel power)
	{
		PowerModel power2 = power;
		return store.Get((AbstractModel)(object)power2, () => new PowerAmountPredictionState(power2.Amount));
	}
}
