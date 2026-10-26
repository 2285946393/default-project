using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace RestSiteExpansion;

internal static class RestSiteChoicesLayout
{
	private const int FittingOptionsAtVanilla = 5;

	private const int Columns = 5;

	private const int MaxVisibleRows = 2;

	private const float ButtonWidth = 251f;

	private const float ButtonHeight = 164f;

	private const float FallbackTextBottom = 205f;

	private const float OutlineAllowance = 6f;

	private const float GapX = 40f;

	private const float GapY = 14f;

	private const float SidePadding = 16f;

	private const float TopPadding = 10f;

	private const float BottomPadding = 10f;

	private const float HeaderShift = 0f;

	private const float DescriptionShift = 223f;

	private const float HintTopOffset = 164f;

	private const float BandTopOffset = -300f;

	private const float BandBottomOffset = 190f;

	internal const string RigMeta = "rse_choices_layout";

	private const string LogMeta = "rse_choices_log_sig";

	private const string ScrollHintLocKey = "REST_SITE_EXPANSION_RESTSITE_SCROLL_HINT";

	private const string HintNodeName = "RestSiteScrollHint";

	private const string LocTable = "relics";

	public static void TryInstall(NRestSiteRoom room)
	{
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Expected O, but got Unknown
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0200: Expected O, but got Unknown
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0307: Unknown result type (might be due to invalid IL or missing references)
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_031d: Unknown result type (might be due to invalid IL or missing references)
		//IL_032b: Expected O, but got Unknown
		//IL_044a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0467: Unknown result type (might be due to invalid IL or missing references)
		//IL_0499: Unknown result type (might be due to invalid IL or missing references)
		//IL_049f: Unknown result type (might be due to invalid IL or missing references)
		//IL_04be: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c4: Unknown result type (might be due to invalid IL or missing references)
		if (room == null || !GodotObject.IsInstanceValid((GodotObject)(object)room))
		{
			return;
		}
		IEnumerable<string> optionIds;
		if (room.Options != null)
		{
			optionIds = room.Options.Select((RestSiteOption o) => o.OptionId);
		}
		else
		{
			IEnumerable<string> enumerable = Array.Empty<string>();
			optionIds = enumerable;
		}
		RestSubmenu restSubmenu = RestMenuState.ActiveMask(optionIds);
		if (restSubmenu == RestSubmenu.None && !ModConfigPage.ReadConfig().EnableChoicesLayout)
		{
			return;
		}
		int num = room.Options?.Count ?? 0;
		if (restSubmenu == RestSubmenu.None && num <= 5)
		{
			return;
		}
		try
		{
			Control nodeOrNull = ((Node)room).GetNodeOrNull<Control>(NodePath.op_Implicit("%ChoicesContainer"));
			Node obj = ((nodeOrNull != null) ? ((Node)nodeOrNull).GetParent() : null);
			Control val = (Control)(object)((obj is Control) ? obj : null);
			if (nodeOrNull == null || val == null)
			{
				MainFile.Logger.Warn("[Layout] 未找到 %ChoicesContainer，跳过选项布局适配（保持原版）。", 1);
			}
			else
			{
				if (((GodotObject)nodeOrNull).HasMeta(StringName.op_Implicit("rse_choices_layout")))
				{
					return;
				}
				ShiftControl(room, "%Header", 0f);
				ShiftControl(room, "%Description", -223f);
				float visibleHeight = VisibleHeight(ProvisionalRows(num), 205f);
				int rowCount = Math.Min(num, 5);
				ScrollContainer val2 = new ScrollContainer
				{
					Name = StringName.op_Implicit("RestSiteChoicesScroll"),
					HorizontalScrollMode = (ScrollMode)0,
					VerticalScrollMode = (ScrollMode)3,
					MouseFilter = (MouseFilterEnum)1,
					FollowFocus = true
				};
				CopyAnchors(nodeOrNull, (Control)(object)val2);
				ApplyBlockRect(val2, rowCount, visibleHeight);
				Label val3 = new Label
				{
					Name = StringName.op_Implicit("RestSiteScrollHint"),
					MouseFilter = (MouseFilterEnum)2,
					HorizontalAlignment = (HorizontalAlignment)1,
					VerticalAlignment = (VerticalAlignment)1,
					Visible = false,
					Text = new LocString("relics", "REST_SITE_EXPANSION_RESTSITE_SCROLL_HINT").GetFormattedText()
				};
				CopyAnchors(nodeOrNull, (Control)(object)val3);
				((Control)val3).OffsetLeft = -300f;
				((Control)val3).OffsetRight = 300f;
				((Control)val3).OffsetTop = 164f;
				((Control)val3).OffsetBottom = 190f;
				Control nodeOrNull2 = ((Node)room).GetNodeOrNull<Control>(NodePath.op_Implicit("%Description"));
				Font val4 = ((nodeOrNull2 != null) ? nodeOrNull2.GetThemeFont(StringName.op_Implicit("normal_font"), (StringName)null) : null);
				if (val4 != null)
				{
					((Control)val3).AddThemeFontOverride(StringName.op_Implicit("font"), val4);
				}
				((Control)val3).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 18);
				((Control)val3).AddThemeColorOverride(StringName.op_Implicit("font_color"), new Color(1f, 0.96f, 0.89f, 0.65f));
				((Control)val3).AddThemeColorOverride(StringName.op_Implicit("font_outline_color"), new Color(0f, 0f, 0f, 0.6f));
				((Control)val3).AddThemeConstantOverride(StringName.op_Implicit("outline_size"), 8);
				Control holder = new Control
				{
					Name = StringName.op_Implicit("ChoicesContainer"),
					MouseFilter = (MouseFilterEnum)2
				};
				((Node)nodeOrNull).UniqueNameInOwner = false;
				((Node)nodeOrNull).Name = StringName.op_Implicit("ChoicesContainerVanilla");
				((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
				((Node)val2).Owner = (Node)(object)room;
				((Node)val2).AddChild((Node)(object)holder, false, (InternalMode)0);
				((Node)holder).Owner = (Node)(object)room;
				((Node)holder).UniqueNameInOwner = true;
				((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
				((Node)val3).Owner = (Node)(object)room;
				if (((Node)room).GetNodeOrNull<Control>(NodePath.op_Implicit("%ChoicesContainer")) != holder)
				{
					((Node)holder).UniqueNameInOwner = false;
					((Node)val).RemoveChild((Node)(object)val2);
					((Node)val2).QueueFree();
					if (GodotObject.IsInstanceValid((GodotObject)(object)val3))
					{
						((Node)val).RemoveChild((Node)(object)val3);
						((Node)val3).QueueFree();
					}
					((Node)nodeOrNull).Name = StringName.op_Implicit("ChoicesContainer");
					((Node)nodeOrNull).UniqueNameInOwner = true;
					MainFile.Logger.Warn("[Layout] 唯一名校验未通过，已回滚为原版布局。", 1);
					return;
				}
				GodotTreeExtensions.QueueFreeSafely((Node)(object)nodeOrNull);
				((GodotObject)val2).SetMeta(StringName.op_Implicit("rse_choices_layout"), Variant.op_Implicit(true));
				((GodotObject)holder).SetMeta(StringName.op_Implicit("rse_choices_layout"), Variant.op_Implicit(true));
				RestSiteSubmenuFilter.MarkActiveSubmenus(holder, restSubmenu);
				((GodotObject)holder).Connect(SignalName.ChildEnteredTree, Callable.From<Node>((Action<Node>)delegate(Node node)
				{
					OnChildEntered(holder, node);
				}), 0u);
				((GodotObject)holder).Connect(SignalName.ChildExitingTree, Callable.From<Node>((Action<Node>)delegate
				{
					RelayoutDeferred(holder);
				}), 0u);
				RelayoutDeferred(holder);
				MainFile.Logger.Info($"[Layout] 休息处选项适配已启用：选项={num} 每行={5} 可见行数≤{2} 子菜单={RestSiteSubmenuFilter.DescribeMask(restSubmenu)} 可用高度={490f:0}（标题上移 {0f} / 描述下移 {223f}）" + "，按钮保持原尺寸不缩放", 1);
			}
		}
		catch (Exception value)
		{
			MainFile.Logger.Warn($"[Layout] 选项布局适配安装异常，保持原版布局：{value}", 1);
		}
	}

	internal static List<NRestSiteButton> LiveButtons(Control holder)
	{
		return (from b in ((IEnumerable)((Node)holder).GetChildren(false)).OfType<NRestSiteButton>()
			where !((GodotObject)b).IsQueuedForDeletion() && ((Node)b).IsInsideTree()
			select b).ToList();
	}

	private static void OnChildEntered(Control holder, Node node)
	{
		RestSiteSubmenuFilter.OnChildEntered(holder, node);
		RelayoutDeferred(holder);
	}

	private static void CopyAnchors(Control from, Control to)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		to.AnchorLeft = from.AnchorLeft;
		to.AnchorTop = from.AnchorTop;
		to.AnchorRight = from.AnchorRight;
		to.AnchorBottom = from.AnchorBottom;
		to.GrowHorizontal = from.GrowHorizontal;
		to.GrowVertical = from.GrowVertical;
	}

	private static void ShiftControl(NRestSiteRoom room, string uniqueName, float deltaY)
	{
		if (!Mathf.IsZeroApprox(deltaY))
		{
			Control nodeOrNull = ((Node)room).GetNodeOrNull<Control>(NodePath.op_Implicit(uniqueName));
			if (nodeOrNull != null)
			{
				nodeOrNull.OffsetTop -= deltaY;
				nodeOrNull.OffsetBottom -= deltaY;
			}
		}
	}

	private static float ContentWidth(int rowCount)
	{
		return (float)rowCount * 251f + (float)(rowCount - 1) * 40f;
	}

	private static int ProvisionalRows(int count)
	{
		return Math.Max(1, (count + 5 - 1) / 5);
	}

	private static float VisibleHeight(int rows, float textBottom)
	{
		int num = Math.Min(rows, 2);
		float num2 = 10f + (float)(num - 1) * (textBottom + 14f) + textBottom + 10f;
		float num3 = 490f;
		if (num2 > num3)
		{
			MainFile.Logger.Warn($"[Layout] 选项块高度({num2:0})超出可用区间({num3:0})，已按区间裁切（单行占位实测={textBottom:0}；如观感异常请反馈该数值）", 1);
			num2 = num3;
		}
		return num2;
	}

	private static void ApplyBlockRect(ScrollContainer scroll, int rowCount, float visibleHeight)
	{
		float num = ContentWidth(rowCount) + 32f;
		((Control)scroll).OffsetLeft = (0f - num) / 2f;
		((Control)scroll).OffsetRight = num / 2f;
		((Control)scroll).OffsetTop = -300f;
		((Control)scroll).OffsetBottom = -300f + visibleHeight;
	}

	internal static void RelayoutDeferred(Control holder)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		Control holder2 = holder;
		if (GodotObject.IsInstanceValid((GodotObject)(object)holder2))
		{
			Callable val = Callable.From((Action)delegate
			{
				Relayout(holder2);
			});
			((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		}
	}

	private static float MeasureTextBottom(List<NRestSiteButton> buttons)
	{
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		float num = 0f;
		foreach (NRestSiteButton button in buttons)
		{
			Control nodeOrNull = ((Node)button).GetNodeOrNull<Control>(NodePath.op_Implicit("%Label"));
			if (nodeOrNull == null)
			{
				continue;
			}
			Font themeFont = nodeOrNull.GetThemeFont(StringName.op_Implicit("font"), (StringName)null);
			if (themeFont != null)
			{
				int themeFontSize = nodeOrNull.GetThemeFontSize(StringName.op_Implicit("font_size"), (StringName)null);
				if (themeFontSize > 0)
				{
					num = Mathf.Max(num, nodeOrNull.Position.Y + themeFont.GetHeight(themeFontSize) + 6f);
				}
			}
		}
		return (num > 164f) ? num : 205f;
	}

	private static void Relayout(Control holder)
	{
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_036e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0373: Unknown result type (might be due to invalid IL or missing references)
		//IL_0377: Unknown result type (might be due to invalid IL or missing references)
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_040a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0430: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_05f4: Unknown result type (might be due to invalid IL or missing references)
		if (!GodotObject.IsInstanceValid((GodotObject)(object)holder) || !((Node)holder).IsInsideTree())
		{
			return;
		}
		RestSubmenu open;
		List<NRestSiteButton> list = RestSiteSubmenuFilter.Apply(holder, LiveButtons(holder), out open);
		if (list.Count == 0)
		{
			RestSiteSubmenuFilter.UpdateHint(holder, 0, open);
			return;
		}
		int num = (list.Count + 5 - 1) / 5;
		int rowCount = Math.Min(list.Count, 5);
		float num2 = MeasureTextBottom(list);
		float num3 = num2 + 14f;
		float num4 = VisibleHeight(num, num2);
		float num5 = 10f + (float)(num - 1) * num3 + num2 + 10f;
		Node parent = ((Node)holder).GetParent();
		ScrollContainer val = (ScrollContainer)(object)((parent is ScrollContainer) ? parent : null);
		if (val != null)
		{
			ApplyBlockRect(val, rowCount, num4);
		}
		float num6 = ContentWidth(rowCount);
		float num7 = num6 + 32f;
		int[] array = new int[num];
		for (int i = 0; i < list.Count; i++)
		{
			array[i / 5]++;
		}
		float[] array2 = new float[num];
		for (int j = 0; j < num; j++)
		{
			float num8 = (float)array[j] * 251f + (float)(array[j] - 1) * 40f;
			array2[j] = 16f + (num6 - num8) / 2f;
		}
		for (int k = 0; k < list.Count; k++)
		{
			int num9 = k / 5;
			int num10 = k % 5;
			NRestSiteButton val2 = list[k];
			((Control)val2).Position = new Vector2(array2[num9] + (float)num10 * 291f, 10f + (float)num9 * num3);
		}
		RestSiteSubmenuFilter.UpdateHint(holder, num, open);
		if (list.Any((NRestSiteButton b) => !((Node)b).IsInsideTree()))
		{
			return;
		}
		for (int l = 0; l < list.Count; l++)
		{
			NRestSiteButton val3 = list[l];
			int num11 = l / 5;
			int num12 = l - 5;
			int num13 = l + 5;
			int index = l;
			if (num13 < list.Count)
			{
				index = num13;
			}
			else if (num11 + 1 < num)
			{
				index = Math.Min((num11 + 2) * 5, list.Count) - 1;
			}
			int index2 = ((l == 0) ? (list.Count - 1) : (l - 1));
			int index3 = ((l != list.Count - 1) ? (l + 1) : 0);
			NodePath val4 = ((num12 >= 0) ? ((Node)list[num12]).GetPath() : ((Node)val3).GetPath());
			NodePath path = ((Node)list[index]).GetPath();
			NodePath path2 = ((Node)list[index2]).GetPath();
			NodePath path3 = ((Node)list[index3]).GetPath();
			if (((Control)val3).FocusNeighborTop != val4)
			{
				((Control)val3).FocusNeighborTop = val4;
			}
			if (((Control)val3).FocusNeighborBottom != path)
			{
				((Control)val3).FocusNeighborBottom = path;
			}
			if (((Control)val3).FocusNeighborLeft != path2)
			{
				((Control)val3).FocusNeighborLeft = path2;
			}
			if (((Control)val3).FocusNeighborRight != path3)
			{
				((Control)val3).FocusNeighborRight = path3;
			}
		}
		Vector2 val5 = default(Vector2);
		((Vector2)(ref val5))._002Ector(num7, num5);
		Vector2 customMinimumSize = holder.CustomMinimumSize;
		if (!((Vector2)(ref customMinimumSize)).IsEqualApprox(val5))
		{
			holder.CustomMinimumSize = val5;
		}
		string value = string.Empty;
		Control nodeOrNull = ((Node)list[0]).GetNodeOrNull<Control>(NodePath.op_Implicit("%Label"));
		if (nodeOrNull != null)
		{
			Font themeFont = nodeOrNull.GetThemeFont(StringName.op_Implicit("font"), (StringName)null);
			if (themeFont != null)
			{
				int themeFontSize = nodeOrNull.GetThemeFontSize(StringName.op_Implicit("font_size"), (StringName)null);
				value = $" 样例标签(y={nodeOrNull.Position.Y:0} h={nodeOrNull.Size.Y:0} 字号={themeFontSize} 行高={themeFont.GetHeight(themeFontSize):0})";
			}
		}
		string text = $"sub={RestMenuState.Describe(open)} n={list.Count} rows={num} textBottom={num2:0} stride={num3:0} visible={num4:0} content={num5:0} blockW={num7:0}{value}";
		string text2;
		if (!((GodotObject)holder).HasMeta(StringName.op_Implicit("rse_choices_log_sig")))
		{
			text2 = string.Empty;
		}
		else
		{
			StringName obj = StringName.op_Implicit("rse_choices_log_sig");
			Variant val6 = default(Variant);
			val6 = ((GodotObject)holder).GetMeta(obj, val6);
			text2 = ((Variant)(ref val6)).AsString();
		}
		string text3 = text2;
		if (text != text3)
		{
			((GodotObject)holder).SetMeta(StringName.op_Implicit("rse_choices_log_sig"), Variant.op_Implicit(text));
			MainFile.Logger.Info("[Layout] 选项摆位：" + text, 1);
		}
	}
}
