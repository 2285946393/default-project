using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.Scripts.Vfx;

namespace RegentFX.Scripts.Patches;

[HarmonyPatch]
public static class RegentStarRingPatch
{
	[HarmonyPatch(typeof(NCombatRoom), "CreateAllyNodes")]
	public static class CreateAllyNodesPatch
	{
		public static void Postfix()
		{
			if (NCombatRoom.Instance == null)
			{
				return;
			}
			CombatState obj = CombatManager.Instance.DebugOnlyGetState();
			IReadOnlyList<Player> readOnlyList = ((obj != null) ? obj.Players : null);
			if (readOnlyList == null)
			{
				return;
			}
			foreach (Player item in readOnlyList)
			{
				EnsureControllers(item);
			}
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	public static class StarsChangedPatch
	{
		public static void Postfix(PlayerCombatState __instance, int value)
		{
			Player localRegentPlayer = GetLocalRegentPlayer();
			if (((localRegentPlayer != null) ? localRegentPlayer.PlayerCombatState : null) == __instance)
			{
				EnsureControllers(localRegentPlayer);
				Entry.StarRingController?.SetStarCount(value);
			}
		}

		private static Player? GetLocalRegentPlayer()
		{
			CombatState obj = CombatManager.Instance.DebugOnlyGetState();
			IReadOnlyList<Player> readOnlyList = ((obj != null) ? obj.Players : null);
			if (readOnlyList == null)
			{
				return null;
			}
			foreach (Player item in readOnlyList)
			{
				if (item.Character is Regent && LocalContext.IsMe(item))
				{
					return item;
				}
			}
			return null;
		}
	}

	private static void EnsureControllers(Player? player)
	{
		if ((Entry.StarRingController == null || !GodotObject.IsInstanceValid((GodotObject)(object)Entry.StarRingController)) && player != null && player.Character is Regent && LocalContext.IsMe(player))
		{
			SetupStarRingForLocalPlayer(player);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(NCombatRoom), "OnProceedButtonPressed")]
	public static void CombatEndPatch()
	{
		ClearStarRing();
	}

	[HarmonyPatch(typeof(NCombatRoom), "_ExitTree")]
	[HarmonyPrefix]
	public static void Prefix_ExitTree()
	{
		ClearStarRing();
	}

	private static void SetupStarRingForLocalPlayer(Player player)
	{
		if (NCombatRoom.Instance != null)
		{
			NCreature creatureNode = NCombatRoom.Instance.GetCreatureNode(player.Creature);
			if (creatureNode == null)
			{
				Entry.Logger.Warn("[RegentStarRing] Could not find player node", 1);
				return;
			}
			ClearStarRing();
			StarRingController starRingController = new StarRingController();
			((Node)NCombatRoom.Instance.CombatVfxContainer).AddChild((Node)(object)starRingController, false, (InternalMode)0);
			starRingController.Initialize(creatureNode, player);
			PlayerCombatState playerCombatState = player.PlayerCombatState;
			int starCount = ((playerCombatState != null) ? playerCombatState.Stars : 0);
			starRingController.SetStarCount(starCount);
			Entry.StarRingController = starRingController;
			StarEffectController starEffectController = new StarEffectController();
			((Node)NCombatRoom.Instance.CombatVfxContainer).AddChild((Node)(object)starEffectController, false, (InternalMode)0);
			starEffectController.Initialize(creatureNode, starRingController);
			Entry.StarEffectController = starEffectController;
		}
	}

	private static void ClearStarRing()
	{
		if (Entry.StarRingController != null)
		{
			Entry.StarRingController.ClearAllStars(animate: true);
			((Node)Entry.StarRingController).QueueFree();
			Entry.StarRingController = null;
		}
		if (Entry.StarEffectController != null)
		{
			Entry.StarEffectController.ClearAllEffects();
			((Node)Entry.StarEffectController).QueueFree();
			Entry.StarEffectController = null;
		}
		Blackhole.Blackholes.Clear();
		Pillar.Pillars.Clear();
	}
}
