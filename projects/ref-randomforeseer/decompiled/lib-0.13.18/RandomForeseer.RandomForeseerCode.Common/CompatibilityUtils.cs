using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Data;

namespace RandomForeseer.RandomForeseerCode.Common;

internal static class CompatibilityUtils
{
	public static bool IsBaseGameType(Type type)
	{
		ArgumentNullException.ThrowIfNull(type, "type");
		bool result = default(bool);
		AssemblyInfo.ModForType(type, ref result);
		return result;
	}

	public static bool IsPredictionAllowedForType(Type type)
	{
		if (ModData.Settings.PredictBaseGameCardsOnly)
		{
			return IsBaseGameType(type);
		}
		return true;
	}

	public static IEnumerable<AbstractModel> FilterHookListeners(IEnumerable<AbstractModel> listeners)
	{
		if (!ModData.Settings.InvokeBaseGameHookListenersOnly)
		{
			return listeners;
		}
		return listeners.Where((AbstractModel listener) => IsBaseGameType(((object)listener).GetType()));
	}
}
