using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips.Patches;

[HarmonyPatch(typeof(NHoverTipSet), "Init")]
internal static class PredictionHoverTipInitPatch
{
	private static readonly ConditionalWeakTable<NHoverTipSet, List<bool>> PredictionTextTipMasks = new ConditionalWeakTable<NHoverTipSet, List<bool>>();

	private static readonly Lazy<ShaderMaterial> PredictionBackgroundMaterial = new Lazy<ShaderMaterial>((Func<ShaderMaterial>)CreatePredictionBackgroundMaterial);

	private static void Prefix(NHoverTipSet __instance, ref IEnumerable<IHoverTip> hoverTips)
	{
		List<bool> list = (from tip in IHoverTip.RemoveDupes(hoverTips = PrepareForPresentation(hoverTips))
			where tip is HoverTip
			select tip.IsPredictionTextHoverTip()).ToList();
		if (list.Contains(item: true))
		{
			PredictionTextTipMasks.AddOrUpdate(__instance, list);
		}
	}

	private static void Postfix(NHoverTipSet __instance)
	{
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		if (!PredictionTextTipMasks.TryGetValue(__instance, out List<bool> value))
		{
			return;
		}
		PredictionTextTipMasks.Remove(__instance);
		foreach (Control item in from pair in ((IEnumerable)((Node)__instance._textHoverTipContainer).GetChildren(false)).OfType<Control>().Zip(value)
			where pair.Second
			select pair.First)
		{
			CanvasItem node = ((Node)item).GetNode<CanvasItem>(NodePath.op_Implicit("%Bg"));
			node.Material = (Material)(object)PredictionBackgroundMaterial.Value;
			node.SelfModulate = Colors.White;
		}
	}

	private static ShaderMaterial CreatePredictionBackgroundMaterial()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Expected O, but got Unknown
		ShaderMaterial val = new ShaderMaterial
		{
			Shader = ResourceLoader.Load<Shader>("res://shaders/hsv.gdshader", (string)null, (CacheMode)1)
		};
		val.SetShaderParameter(StringName.op_Implicit("h"), Variant.op_Implicit(0.52f));
		val.SetShaderParameter(StringName.op_Implicit("s"), Variant.op_Implicit(1.75f));
		val.SetShaderParameter(StringName.op_Implicit("v"), Variant.op_Implicit(1.15f));
		return val;
	}

	private static List<IHoverTip> PrepareForPresentation(IEnumerable<IHoverTip> hoverTips)
	{
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		List<IHoverTip> list = hoverTips.ToList();
		List<PredictionCardBundleHoverTip> list2 = list.OfType<PredictionCardBundleHoverTip>().ToList();
		if (list2 != null && list2.Count == 1)
		{
			PredictionCardBundleHoverTip predictionCardBundleHoverTip = list2[0];
			if (list.OfType<CardHoverTip>().All((CardHoverTip tip) => tip is PredictionCardBundleHoverTip))
			{
				int index = list.IndexOf((IHoverTip)(object)predictionCardBundleHoverTip);
				list.RemoveAt(index);
				list.InsertRange(index, predictionCardBundleHoverTip.Cards.ToPredictionHoverTips());
				list2.Clear();
			}
		}
		PredictionCardBundleHoverTip predictionCardBundleHoverTip2 = list2.Find((PredictionCardBundleHoverTip tip) => tip.Kind == PredictionCardBundleKind.Transform);
		if (predictionCardBundleHoverTip2 != null)
		{
			int index2 = list.IndexOf((IHoverTip)(object)predictionCardBundleHoverTip2);
			list.Insert(index2, (IHoverTip)(object)PredictionHoverTipFactory.Text("transform_bundle_explanation"));
		}
		return list;
	}
}
