using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using MegaCrit.Sts2.addons.mega_text;
using ModPresetManager.Scripts.Localization;
using ModPresetManager.Scripts.Ui;

namespace ModPresetManager.Scripts.Patches;

[HarmonyPatch(typeof(NModdingScreen), "_Ready")]
internal static class ModPresetScreenPatch
{
	private const string ButtonName = "ModPresetButton";

	private const int ButtonDuplicateFlags = 14;

	[HarmonyPostfix]
	private static void AddPresetButton(NModdingScreen __instance)
	{
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		NModdingScreen __instance2 = __instance;
		NModInfoContainer node = ((Node)__instance2).GetNode<NModInfoContainer>(NodePath.op_Implicit("%ModInfoContainer"));
		if (((Node)node).HasNode(NodePath.op_Implicit("ModPresetButton")))
		{
			return;
		}
		Node obj = ((Node)((Node)__instance2).GetNode<NButton>(NodePath.op_Implicit("%GetModsButton"))).Duplicate(14);
		NButton val = (NButton)(object)((obj is NButton) ? obj : null);
		if (val != null)
		{
			TextureRect node2 = ((Node)val).GetNode<TextureRect>(NodePath.op_Implicit("Visuals/Image"));
			Material material = ((CanvasItem)node2).Material;
			ShaderMaterial val2 = (ShaderMaterial)(object)((material is ShaderMaterial) ? material : null);
			if (val2 != null)
			{
				((CanvasItem)node2).Material = (Material)(ShaderMaterial)((Resource)val2).Duplicate(false);
			}
			((Node)val).Name = StringName.op_Implicit("ModPresetButton");
			((CanvasItem)val).Visible = true;
			((Control)val).FocusMode = (FocusModeEnum)2;
			((Control)val).SetAnchorsPreset((LayoutPreset)7, false);
			((Control)val).OffsetLeft = -145f;
			((Control)val).OffsetTop = -84f;
			((Control)val).OffsetRight = 145f;
			((Control)val).OffsetBottom = -14f;
			((Node)node).AddChild((Node)(object)val, false, (InternalMode)0);
			((Node)val).GetNode<MegaLabel>(NodePath.op_Implicit("Visuals/Label")).SetTextAutoSize(ModLocalization.Text("preset.entry_button"));
			((GodotObject)val).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)delegate
			{
				ModPresetModal.Show(__instance2);
			}), 0u);
			Control node3 = ((Node)node).GetNode<Control>(NodePath.op_Implicit("ModDescription"));
			node3.OffsetBottom = Mathf.Min(node3.OffsetBottom, 806f);
		}
	}
}
