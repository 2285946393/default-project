using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events.Custom.CrystalSphere;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.Settings;
using STS2RitsuLib.Settings;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Patches;

[HarmonyPatch(typeof(NCrystalSphereMask), "_Ready")]
internal static class CrystalSphereClairvoyancePatch
{
	private const float MaskAlpha = 0.4f;

	private static void Postfix(NCrystalSphereMask __instance)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		NCrystalSphereMask __instance2 = __instance;
		Color original = ((CanvasItem)__instance2).SelfModulate;
		Refresh(__instance2, original);
		ModSettingsBindingWriteEvents.SubscribeValueWrittenWhileNodeAlive((Node)(object)__instance2, (Action<IModSettingsBinding>)delegate(IModSettingsBinding binding)
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			if (IsRefreshBinding(binding))
			{
				Refresh(__instance2, original);
			}
		});
	}

	private static bool IsRefreshBinding(IModSettingsBinding binding)
	{
		if (binding != SettingsUiBindings.SingleplayerPredictionEnabled && binding != SettingsUiBindings.MultiplayerPredictionEnabled)
		{
			return binding == SettingsUiBindings.CrystalSphereClairvoyanceEnabled;
		}
		return true;
	}

	private static void Refresh(NCrystalSphereMask mask, Color original)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		ModSettings settings = ModData.Settings;
		Color selfModulate;
		if (!ModSettingsExtensions.get_IsPredictionEnabled(settings) || !settings.CrystalSphereClairvoyanceEnabled)
		{
			selfModulate = original;
		}
		else
		{
			Color val = original;
			val.A = 0.4f;
			selfModulate = val;
		}
		((CanvasItem)mask).SelfModulate = selfModulate;
	}
}
