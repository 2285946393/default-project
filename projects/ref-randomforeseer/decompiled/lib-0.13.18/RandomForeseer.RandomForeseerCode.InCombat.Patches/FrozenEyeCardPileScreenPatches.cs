using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NCardPileScreen))]
internal static class FrozenEyeCardPileScreenPatches
{
	private sealed class ScreenRefreshCallback
	{
		[CompilerGenerated]
		private NCardPileScreen _003Cscreen_003EP;

		public ScreenRefreshCallback(NCardPileScreen screen)
		{
			_003Cscreen_003EP = screen;
			base._002Ector();
		}

		public void Refresh()
		{
			if (ModData.Settings.ShufflePredictionEnabled)
			{
				FrozenEyeDrawPileView.TryRefresh(_003Cscreen_003EP);
			}
		}
	}

	private static readonly ConditionalWeakTable<NCardPileScreen, ScreenRefreshCallback> RefreshCallbacks = new ConditionalWeakTable<NCardPileScreen, ScreenRefreshCallback>();

	[HarmonyPatch("OnPileContentsChanged")]
	[HarmonyPrefix]
	private static bool RefreshDrawPileView(NCardPileScreen __instance)
	{
		try
		{
			return !FrozenEyeDrawPileView.TryRefresh(__instance);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Frozen Eye draw pile view refresh failed: {ex}", 1);
			ModTelemetry.CaptureException(ex, "frozen_eye_patch", "refresh_draw_pile_view");
			return true;
		}
	}

	[HarmonyPatch("_EnterTree")]
	[HarmonyPostfix]
	private static void SubscribeDiscardPileChanges(NCardPileScreen __instance)
	{
		if (!CardPileUtils.TryGetDrawPileOwner(__instance.Pile, out Player player))
		{
			return;
		}
		PlayerCombatState playerCombatState = player.PlayerCombatState;
		if (playerCombatState != null)
		{
			ScreenRefreshCallback value = RefreshCallbacks.GetValue(__instance, (NCardPileScreen screen) => new ScreenRefreshCallback(screen));
			playerCombatState.DiscardPile.ContentsChanged -= value.Refresh;
			playerCombatState.DiscardPile.ContentsChanged += value.Refresh;
		}
	}

	[HarmonyPatch("_ExitTree")]
	[HarmonyPostfix]
	private static void UnsubscribeDiscardPileChanges(NCardPileScreen __instance)
	{
		if (CardPileUtils.TryGetDrawPileOwner(__instance.Pile, out Player player))
		{
			PlayerCombatState playerCombatState = player.PlayerCombatState;
			if (playerCombatState != null && RefreshCallbacks.TryGetValue(__instance, out ScreenRefreshCallback value))
			{
				playerCombatState.DiscardPile.ContentsChanged -= value.Refresh;
			}
		}
	}

	[HarmonyPatch("AfterCapstoneOpened")]
	[HarmonyPostfix]
	private static void RefreshAfterOpened(NCardPileScreen __instance)
	{
		__instance.OnPileContentsChanged();
	}
}
