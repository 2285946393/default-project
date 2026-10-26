using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using RandomForeseer.RandomForeseerCode.Data;

namespace RandomForeseer.RandomForeseerCode.Debug.Patches;

[HarmonyPatch(typeof(NEventLayout), "AddOptions")]
internal static class EventDebugRerollPatch
{
	private const string ButtonName = "RandomForeseer_EventDebugReroll";

	private static readonly MethodInfo GenerateInitialOptionsMethod = AccessTools.Method(typeof(EventModel), "GenerateInitialOptionsWrapper", (Type[])null, (Type[])null);

	private static readonly MethodInfo SetEventStateMethod = AccessTools.Method(typeof(EventModel), "SetEventState", new Type[2]
	{
		typeof(LocString),
		typeof(IEnumerable<EventOption>)
	}, (Type[])null);

	private static void Postfix(NEventLayout __instance)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Expected O, but got Unknown
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		ModSettings settings = ModData.Settings;
		if (!settings.DebugSettingsEnabled || !settings.EventDebugRerollEnabled)
		{
			return;
		}
		EventModel eventModel = __instance._event;
		if (eventModel != null && !eventModel.IsFinished && ((Node)__instance).GetNodeOrNull<Button>(NodePath.op_Implicit("RandomForeseer_EventDebugReroll")) == null)
		{
			Button val = new Button
			{
				Name = StringName.op_Implicit("RandomForeseer_EventDebugReroll"),
				Text = "Reroll",
				CustomMinimumSize = new Vector2(180f, 44f),
				FocusMode = (FocusModeEnum)0
			};
			((GodotObject)val).Connect(SignalName.Pressed, Callable.From((Action)delegate
			{
				Reroll(eventModel);
			}), 0u);
			GodotTreeExtensions.AddChildSafely((Node)(object)((Node)__instance).GetNode<VBoxContainer>(NodePath.op_Implicit("%OptionsContainer")), (Node)(object)val);
		}
	}

	private static void Reroll(EventModel eventModel)
	{
		IReadOnlyList<EventOption> readOnlyList = (IReadOnlyList<EventOption>)GenerateInitialOptionsMethod.Invoke(eventModel, null);
		SetEventStateMethod.Invoke(eventModel, new object[2] { eventModel.InitialDescription, readOnlyList });
	}
}
