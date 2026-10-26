using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace RestSiteExpansion;

internal static class RestSiteSubmenuFilter
{
	private const string SubmenusMeta = "rse_choices_submenus";

	private const string ScrollHintLocKey = "REST_SITE_EXPANSION_RESTSITE_SCROLL_HINT";

	private const string HintNodeName = "RestSiteScrollHint";

	private const string LocTable = "relics";

	private const int MaxVisibleRows = 2;

	internal static void MarkActiveSubmenus(Control holder, RestSubmenu mask)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)holder).SetMeta(StringName.op_Implicit("rse_choices_submenus"), Variant.op_Implicit((int)mask));
	}

	internal static RestSubmenu SubmenusOf(Control holder)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		int result;
		if (!((GodotObject)holder).HasMeta(StringName.op_Implicit("rse_choices_submenus")))
		{
			result = 0;
		}
		else
		{
			StringName obj = StringName.op_Implicit("rse_choices_submenus");
			Variant val = default(Variant);
			val = ((GodotObject)holder).GetMeta(obj, val);
			result = ((Variant)(ref val)).AsInt32();
		}
		return (RestSubmenu)result;
	}

	internal static string DescribeMask(RestSubmenu mask)
	{
		List<string> list = RestMenuState.MenusOf(mask).Select(RestMenuState.Describe).ToList();
		return (list.Count == 0) ? "无" : string.Join("+", list);
	}

	internal static void RefreshForSubmenu(NRestSiteRoom room)
	{
		try
		{
			Control val = ((room != null) ? ((Node)room).GetNodeOrNull<Control>(NodePath.op_Implicit("%ChoicesContainer")) : null);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val) && ((GodotObject)val).HasMeta(StringName.op_Implicit("rse_choices_layout")))
			{
				Apply(val, RestSiteChoicesLayout.LiveButtons(val), out var _);
				RestSiteChoicesLayout.RelayoutDeferred(val);
			}
		}
		catch (Exception value)
		{
			MainFile.Logger.Warn($"[RestMenu] 层级显示刷新异常（忽略，保持原显示）：{value}", 1);
		}
	}

	internal static void OnChildEntered(Control holder, Node node)
	{
		NRestSiteButton val = (NRestSiteButton)(object)((node is NRestSiteButton) ? node : null);
		if (val == null)
		{
			return;
		}
		try
		{
			RestSubmenu restSubmenu = SubmenusOf(holder);
			if (restSubmenu != 0 && TryResolveLocalPlayer(val, out Player owner))
			{
				RestSubmenu openSubmenu = RestMenuState.GetOpenSubmenu(NRestSiteRoom.Instance, owner.NetId);
				string optionId = OptionIdOf(val);
				bool flag = RestMenuState.IsVisible(optionId, openSubmenu, restSubmenu) && !TentBalanceGate.IsBlockedFor(owner, optionId);
				if (((CanvasItem)val).Visible != flag)
				{
					((CanvasItem)val).Visible = flag;
				}
			}
		}
		catch (Exception ex)
		{
			MainFile.Logger.Warn("[RestMenu] 新按钮可见性判定异常（忽略，保持可见）：" + ex.Message, 1);
		}
	}

	internal static List<NRestSiteButton> Apply(Control holder, List<NRestSiteButton> buttons, out RestSubmenu open)
	{
		RestSubmenu restSubmenu = SubmenusOf(holder);
		open = RestSubmenu.None;
		if (restSubmenu == RestSubmenu.None || buttons.Count == 0)
		{
			return buttons;
		}
		if (!TryResolveLocalPlayer(buttons[0], out Player owner))
		{
			return buttons;
		}
		open = ResolveOpenSubmenu(owner, buttons, restSubmenu);
		foreach (NRestSiteButton button in buttons)
		{
			string optionId = OptionIdOf(button);
			bool flag = RestMenuState.IsVisible(optionId, open, restSubmenu) && !TentBalanceGate.IsBlockedFor(owner, optionId);
			if (flag)
			{
				RestSubmenu restSubmenu2 = RestMenuState.MenuOfEntryOption(optionId);
				if (restSubmenu2 != 0 && !GroupHasMembers(owner, buttons, restSubmenu2))
				{
					flag = false;
				}
			}
			if (((CanvasItem)button).Visible != flag)
			{
				((CanvasItem)button).Visible = flag;
			}
		}
		List<NRestSiteButton> list = buttons.Where((NRestSiteButton button) => ((CanvasItem)button).Visible).ToList();
		MoveVisibleToFront(holder, list);
		List<NRestSiteButton> list2 = new List<NRestSiteButton>(list.Count);
		list2.AddRange(list.Where(IsBack));
		list2.AddRange(from button in list
			where !IsBack(button)
			orderby RestSiteExpansionSingleton.RankOf(OptionIdOf(button))
			select button);
		return list2;
	}

	internal static void UpdateHint(Control holder, int rows, RestSubmenu open)
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		Node parent = ((Node)holder).GetParent();
		object obj;
		if (parent == null)
		{
			obj = null;
		}
		else
		{
			Node parent2 = parent.GetParent();
			obj = ((parent2 != null) ? parent2.GetNodeOrNull<Label>(NodePath.op_Implicit("RestSiteScrollHint")) : null);
		}
		Label val = (Label)obj;
		if (val == null)
		{
			return;
		}
		bool flag = open != RestSubmenu.None;
		string text = (flag ? RestMenuState.HintLocKeyOf(open) : "REST_SITE_EXPANSION_RESTSITE_SCROLL_HINT");
		if (string.IsNullOrEmpty(text))
		{
			text = "REST_SITE_EXPANSION_RESTSITE_SCROLL_HINT";
		}
		bool flag2 = flag || rows > 2;
		if (flag2)
		{
			string formattedText = new LocString("relics", text).GetFormattedText();
			if (val.Text != formattedText)
			{
				val.Text = formattedText;
			}
		}
		if (((CanvasItem)val).Visible != flag2)
		{
			((CanvasItem)val).Visible = flag2;
		}
	}

	private static RestSubmenu ResolveOpenSubmenu(Player owner, List<NRestSiteButton> buttons, RestSubmenu submenus)
	{
		RestSubmenu openSubmenu = RestMenuState.GetOpenSubmenu(NRestSiteRoom.Instance, owner.NetId);
		if (openSubmenu == RestSubmenu.None)
		{
			return RestSubmenu.None;
		}
		if ((submenus & openSubmenu) == 0)
		{
			return RestSubmenu.None;
		}
		if (!GroupHasMembers(owner, buttons, openSubmenu))
		{
			return RestSubmenu.None;
		}
		return openSubmenu;
	}

	private static bool GroupHasMembers(Player owner, List<NRestSiteButton> buttons, RestSubmenu menu)
	{
		Player owner2 = owner;
		return buttons.Any(delegate(NRestSiteButton button)
		{
			string optionId = OptionIdOf(button);
			return RestMenuState.IsMemberOf(optionId, menu) && !TentBalanceGate.IsBlockedFor(owner2, optionId);
		});
	}

	private static void MoveVisibleToFront(Control holder, List<NRestSiteButton> visible)
	{
		int num = 0;
		foreach (NRestSiteButton item in visible.Where((NRestSiteButton button) => !IsBack(button)))
		{
			if (((Node)item).GetIndex(false) != num)
			{
				((Node)holder).MoveChild((Node)(object)item, num);
			}
			num++;
		}
		foreach (NRestSiteButton item2 in visible.Where(IsBack))
		{
			if (((Node)item2).GetIndex(false) != num)
			{
				((Node)holder).MoveChild((Node)(object)item2, num);
			}
			num++;
		}
	}

	private static bool IsBack(NRestSiteButton button)
	{
		return OptionIdOf(button) == "REST_MENU_BACK";
	}

	private static string OptionIdOf(NRestSiteButton button)
	{
		RestSiteOption option = button.Option;
		return ((option != null) ? option.OptionId : null) ?? string.Empty;
	}

	private static bool TryResolveLocalPlayer(NRestSiteButton button, out Player owner)
	{
		Player val = RestSiteOptionAccess.GetOwner(button.Option);
		if (val == null)
		{
			Node parent = ((Node)button).GetParent();
			if (parent != null)
			{
				val = (from sibling in ((IEnumerable)parent.GetChildren(false)).OfType<NRestSiteButton>()
					select RestSiteOptionAccess.GetOwner(sibling.Option)).FirstOrDefault<Player>((Func<Player, bool>)((Player candidate) => candidate != null));
			}
		}
		owner = val;
		return val != null;
	}
}
