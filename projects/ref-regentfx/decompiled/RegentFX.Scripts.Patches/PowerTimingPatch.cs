using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using RegentFX.Scripts.Vfx.Powers;

namespace RegentFX.Scripts.Patches;

[HarmonyPatch]
public static class PowerTimingPatch
{
	private static PowerFX? checkPower(PowerModel powerModel, Creature? target = null)
	{
		PowerFX powerFX = PowerFX.FromPower(powerModel);
		if (powerFX == null)
		{
			return null;
		}
		Creature val = target ?? powerModel.Owner;
		if (val == null)
		{
			return null;
		}
		if (LocalContext.IsMe(val.Player))
		{
			return powerFX;
		}
		return null;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(PowerModel), "BeforeApplied")]
	public static void BeforeBeforeAppliedPatch(PowerModel __instance, Creature target, decimal amount)
	{
		checkPower(__instance, target)?.BeforeBeforeApplied(target, amount);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(PowerModel), "AfterRemoved")]
	public static void AfterAfterRemovedPatch(PowerModel __instance, Creature oldOwner)
	{
		checkPower(__instance, oldOwner)?.AfterAfterRemoved(oldOwner);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(PowerModel), "SetAmount")]
	public static void AfterSetAmountPatch(PowerModel __instance, int amount)
	{
		checkPower(__instance)?.AfterSetAmount(amount);
	}
}
