using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.Scripts.Vfx;
using RegentFX.Scripts.Vfx.Cards;

namespace RegentFX.Scripts.Patches;

[HarmonyPatch]
public static class SupermassivePatch
{
	[HarmonyPatch(typeof(NCombatRoom), "CreateAllyNodes")]
	[HarmonyPostfix]
	public static void CreateAllyNodesPostfix()
	{
		EnsureController();
	}

	[HarmonyPatch(typeof(Hook), "AfterCardGeneratedForCombat")]
	[HarmonyPostfix]
	public static void CardGeneratedPostfix(ICombatState combatState, CardModel card, Player? creator, ref Task __result)
	{
		if (creator != null && LocalContext.IsMe(creator))
		{
			__result = NotifyAfterGenerated(__result, combatState, creator);
		}
	}

	private static async Task NotifyAfterGenerated(Task original, ICombatState combatState, Player creator)
	{
		await original;
		if (creator.Creature.CombatState == combatState && !creator.Creature.IsDead)
		{
			EnsureController(creator)?.OnCardGenerated(creator);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(NCombatRoom), "OnProceedButtonPressed")]
	public static void CombatEndPrefix()
	{
		ClearController();
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(NCombatRoom), "_ExitTree")]
	public static void CombatRoomExitPrefix()
	{
		ClearController();
	}

	internal static SupermassiveController? EnsureController(Player? preferredPlayer = null)
	{
		if (!CardFX.IsTypeEnabled<Supermassive>())
		{
			return null;
		}
		if (Entry.SupermassiveController != null)
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)Entry.SupermassiveController))
			{
				return Entry.SupermassiveController;
			}
			Entry.SupermassiveController = null;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		Player val = preferredPlayer ?? GetLocalPlayer();
		if (instance == null || ((val != null) ? val.PlayerCombatState : null) == null || val.Creature.IsDead)
		{
			return null;
		}
		if (!LocalContext.IsMe(val))
		{
			return null;
		}
		NCreature creatureNode = instance.GetCreatureNode(val.Creature);
		if (creatureNode == null || instance.CombatVfxContainer == null)
		{
			return null;
		}
		SupermassiveController supermassiveController = new SupermassiveController();
		GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)supermassiveController);
		supermassiveController.Initialize(creatureNode, val);
		Entry.SupermassiveController = supermassiveController;
		return supermassiveController;
	}

	private static Player? GetLocalPlayer()
	{
		CombatState obj = CombatManager.Instance.DebugOnlyGetState();
		return ((obj != null) ? obj.Players : null)?.FirstOrDefault((Func<Player, bool>)LocalContext.IsMe);
	}

	private static void ClearController()
	{
		SupermassiveController supermassiveController = Entry.SupermassiveController;
		Entry.SupermassiveController = null;
		if (supermassiveController != null && GodotObject.IsInstanceValid((GodotObject)(object)supermassiveController))
		{
			supermassiveController.ClearImmediate();
			GodotTreeExtensions.QueueFreeSafely((Node)(object)supermassiveController);
		}
	}
}
