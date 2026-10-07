using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace MeleeAttack;

public static class SnapshotManager
{
	[HarmonyPatch(typeof(CardModel), "OnPlayWrapper")]
	private static class FranticEscapeClearPatch
	{
		private static void Prefix(CardModel __instance)
		{
			object obj;
			if (__instance == null)
			{
				obj = null;
			}
			else
			{
				ModelId id = ((AbstractModel)__instance).Id;
				obj = ((id != null) ? id.Entry : null);
			}
			string text = (string)obj;
			if (!string.IsNullOrEmpty(text) && text.Equals("FRANTIC_ESCAPE", StringComparison.OrdinalIgnoreCase))
			{
				SimpleTeleportPatch.ClearAllSnapshots();
			}
		}
	}

	private static bool _subscribed;

	public static void Initialize()
	{
		if (!_subscribed)
		{
			AttackTimeState.OnEnd += OnTurnEndHandler;
			_subscribed = true;
		}
	}

	private static void OnTurnEndHandler()
	{
		SimpleTeleportPatch.ClearAllSnapshots();
	}
}
