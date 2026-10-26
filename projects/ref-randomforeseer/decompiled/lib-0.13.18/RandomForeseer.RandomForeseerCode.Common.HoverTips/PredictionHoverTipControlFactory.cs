using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips;

internal static class PredictionHoverTipControlFactory
{
	private const string CardHoverTipScenePath = "res://scenes/ui/card_hover_tip.tscn";

	private const float BundleCardScale = 1f;

	private const float BundleCardSeparation = 45f;

	public static Control CreateAndAddStack(NHoverTipCardContainer parent, PredictionCardBundleHoverTip bundleTip)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Expected O, but got Unknown
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_0263: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		Control val = new Control
		{
			MouseFilter = (MouseFilterEnum)2
		};
		GodotTreeExtensions.AddChildSafely((Node)(object)parent, (Node)(object)val);
		Vector2 val2 = Vector2.Zero;
		IReadOnlyList<CardModel> readOnlyList2;
		if (bundleTip.Kind != PredictionCardBundleKind.ScrollBoxes)
		{
			List<CardModel> list = new List<CardModel>();
			list.AddRange(bundleTip.Cards.Reverse());
			IReadOnlyList<CardModel> readOnlyList = new _003C_003Ez__ReadOnlyList<CardModel>(list);
			readOnlyList2 = readOnlyList;
		}
		else
		{
			readOnlyList2 = bundleTip.Cards;
		}
		IReadOnlyList<CardModel> readOnlyList3 = readOnlyList2;
		for (int i = 0; i < readOnlyList3.Count; i++)
		{
			Control val3 = CreateAndAddCard(val, readOnlyList3[i]);
			float num = (float)(readOnlyList3.Count - 1) / 2f;
			Vector2 val4 = new Vector2(-1f, 1f) * 45f * ((float)i - num) * 1f;
			val3.Scale = Vector2.One * 1f;
			val3.Position = val4;
			float num2 = ((readOnlyList3.Count <= 1) ? 1f : (0.5f + (float)i / (float)(readOnlyList3.Count - 1) * 0.5f));
			((CanvasItem)val3).Modulate = new Color(num2, num2, num2, 1f);
			((Vector2)(ref val2))._002Ector(Mathf.Max(val2.X, val4.X + val3.Size.X * 1f), Mathf.Max(val2.Y, val4.Y + val3.Size.Y * 1f));
		}
		List<Control> list2 = ((IEnumerable)((Node)val).GetChildren(false)).OfType<Control>().ToList();
		float num3 = list2.Select((Control child) => child.Position.X).DefaultIfEmpty(0f).Min();
		float num4 = list2.Select((Control child) => child.Position.Y).DefaultIfEmpty(0f).Min();
		if (num3 < 0f || num4 < 0f)
		{
			Vector2 val5 = default(Vector2);
			((Vector2)(ref val5))._002Ector((num3 < 0f) ? (0f - num3) : 0f, (num4 < 0f) ? (0f - num4) : 0f);
			foreach (Control item in list2)
			{
				item.Position += val5;
			}
			val2 += val5;
		}
		val.Size = val2;
		return val;
	}

	private static Control CreateAndAddCard(Control parent, CardModel card)
	{
		Control val = PreloadManager.Cache.GetScene("res://scenes/ui/card_hover_tip.tscn").Instantiate<Control>((GenEditState)0);
		GodotTreeExtensions.AddChildSafely((Node)(object)parent, (Node)(object)val);
		NCard node = ((Node)val).GetNode<NCard>(NodePath.op_Implicit("%Card"));
		node.Model = card;
		node.UpdateVisuals((PileType)6, (CardPreviewMode)1);
		return val;
	}
}
