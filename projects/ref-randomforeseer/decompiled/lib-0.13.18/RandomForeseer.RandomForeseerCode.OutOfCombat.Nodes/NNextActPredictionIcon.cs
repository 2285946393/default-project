using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Rooms;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Nodes;

[ScriptPath("res://RandomForeseerCode/OutOfCombat/Nodes/NNextActPredictionIcon.cs")]
[ScriptPath("res://RandomForeseerCode/OutOfCombat/Nodes/NNextActPredictionIcon.cs")]
internal sealed class NNextActPredictionIcon : NClickableControl
{
	public class MethodName : MethodName
	{
		public static readonly StringName Create = StringName.op_Implicit("Create");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName OnFocus = StringName.op_Implicit("OnFocus");

		public static readonly StringName OnUnfocus = StringName.op_Implicit("OnUnfocus");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _kind = StringName.op_Implicit("_kind");

		public static readonly StringName _icon = StringName.op_Implicit("_icon");

		public static readonly StringName _outline = StringName.op_Implicit("_outline");

		public static readonly StringName _secondBossIcon = StringName.op_Implicit("_secondBossIcon");

		public static readonly StringName _secondBossIconOutline = StringName.op_Implicit("_secondBossIconOutline");
	}

	public class SignalName : SignalName
	{
	}

	private const string ScenePath = "res://RandomForeseer/scenes/next_act_prediction_icon.tscn";

	private const string SecondBossIconScenePath = "res://scenes/ui/top_bar/second_boss_icon.tscn";

	private static readonly StringName TintColor = new StringName("tint_color");

	private NextActPredictionIconKind _kind;

	private HoverTip? _hoverTip;

	private TextureRect _icon;

	private TextureRect _outline;

	private TextureRect? _secondBossIcon;

	private TextureRect? _secondBossIconOutline;

	public static NNextActPredictionIcon Create(NextActPredictionIconKind kind)
	{
		NNextActPredictionIcon nNextActPredictionIcon = GD.Load<PackedScene>("res://RandomForeseer/scenes/next_act_prediction_icon.tscn").Instantiate<NNextActPredictionIcon>((GenEditState)0);
		nNextActPredictionIcon._kind = kind;
		return nNextActPredictionIcon;
	}

	public override void _Ready()
	{
		_icon = ((Node)this).GetNode<TextureRect>(NodePath.op_Implicit("Icon"));
		_outline = ((Node)this).GetNode<TextureRect>(NodePath.op_Implicit("Icon/Outline"));
		((NClickableControl)this).ConnectSignals();
	}

	public void SetPrediction(ActModel nextAct)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		_hoverTip = CreateHoverTip(nextAct, _kind);
		var (pointType, roomType, modelId) = _kind switch
		{
			NextActPredictionIconKind.Ancient => ((MapPointType)8, (RoomType)6, ((AbstractModel)nextAct.Ancient).Id), 
			NextActPredictionIconKind.Boss => ((MapPointType)7, (RoomType)3, ((AbstractModel)nextAct.BossEncounter).Id), 
			_ => throw new InvalidOperationException($"Unknown NextActPredictionIconKind: {_kind}"), 
		};
		SetRoomIconAndOutline(pointType, roomType, modelId, _icon, _outline);
		RefreshSecondBossIcon(nextAct);
	}

	protected override void OnFocus()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		if (_hoverTip.HasValue)
		{
			NHoverTipSet obj = NHoverTipSet.CreateAndShow((Control)(object)this, (IHoverTip)(object)_hoverTip, (HoverTipAlignment)0);
			if (obj != null)
			{
				((Control)obj).SetGlobalPosition(((Control)_icon).GlobalPosition + new Vector2(0f, ((Control)this).Size.Y + 20f), false);
			}
		}
	}

	protected override void OnUnfocus()
	{
		NHoverTipSet.Remove((Control)(object)this);
	}

	private void RefreshSecondBossIcon(ActModel nextAct)
	{
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		if (_kind != NextActPredictionIconKind.Boss || nextAct.SecondBossEncounter == null)
		{
			TextureRect? secondBossIcon = _secondBossIcon;
			if (secondBossIcon != null)
			{
				((CanvasItem)secondBossIcon).Visible = false;
			}
			TextureRect? secondBossIconOutline = _secondBossIconOutline;
			if (secondBossIconOutline != null)
			{
				((CanvasItem)secondBossIconOutline).Visible = false;
			}
			return;
		}
		if (_secondBossIcon == null)
		{
			_secondBossIcon = GD.Load<PackedScene>("res://scenes/ui/top_bar/second_boss_icon.tscn").Instantiate<TextureRect>((GenEditState)0);
			_secondBossIconOutline = ((Node)_secondBossIcon).GetNode<TextureRect>(NodePath.op_Implicit("%Outline"));
			((Control)_secondBossIcon).MouseFilter = (MouseFilterEnum)1;
			((Control)_secondBossIconOutline).MouseFilter = (MouseFilterEnum)1;
			GodotTreeExtensions.AddChildSafely((Node)(object)_icon, (Node)(object)_secondBossIcon);
			((Control)_secondBossIcon).Position = new Vector2(30f, 22f);
		}
		SetRoomIconAndOutline((MapPointType)7, (RoomType)3, ((AbstractModel)nextAct.SecondBossEncounter).Id, _secondBossIcon, _secondBossIconOutline);
		RefreshSecondBossIconColor(nextAct);
		((CanvasItem)_secondBossIcon).Visible = true;
		((CanvasItem)_secondBossIconOutline).Visible = true;
	}

	private void RefreshSecondBossIconColor(ActModel nextAct)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		TextureRect? secondBossIcon = _secondBossIcon;
		Material obj = ((secondBossIcon != null) ? ((CanvasItem)secondBossIcon).Material : null);
		ShaderMaterial val = (ShaderMaterial)(object)((obj is ShaderMaterial) ? obj : null);
		if (val != null)
		{
			TextureRect? secondBossIconOutline = _secondBossIconOutline;
			Material obj2 = ((secondBossIconOutline != null) ? ((CanvasItem)secondBossIconOutline).Material : null);
			ShaderMaterial val2 = (ShaderMaterial)(object)((obj2 is ShaderMaterial) ? obj2 : null);
			if (val2 != null)
			{
				Color mapUntraveledColor = nextAct.MapUntraveledColor;
				Vector3 val3 = default(Vector3);
				((Vector3)(ref val3))._002Ector(mapUntraveledColor.R, mapUntraveledColor.G, mapUntraveledColor.B);
				val.SetShaderParameter(TintColor, Variant.op_Implicit(val3));
				val2.SetShaderParameter(TintColor, Variant.op_Implicit(val3));
			}
		}
	}

	private static void SetRoomIconAndOutline(MapPointType pointType, RoomType roomType, ModelId? modelId, TextureRect icon, TextureRect outline)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		string roomIconPath = ImageHelper.GetRoomIconPath(pointType, roomType, modelId);
		if (roomIconPath != null)
		{
			icon.Texture = PreloadManager.Cache.GetTexture2D(roomIconPath);
		}
		string roomIconOutlinePath = ImageHelper.GetRoomIconOutlinePath(pointType, roomType, modelId);
		if (roomIconOutlinePath != null)
		{
			outline.Texture = PreloadManager.Cache.GetTexture2D(roomIconOutlinePath);
		}
	}

	private static HoverTip CreateHoverTip(ActModel nextAct, NextActPredictionIconKind kind)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		return (HoverTip)(kind switch
		{
			NextActPredictionIconKind.Ancient => CreateAncientHoverTip(nextAct), 
			NextActPredictionIconKind.Boss => CreateBossHoverTip(nextAct), 
			_ => throw new InvalidOperationException($"Unknown NextActPredictionIconKind: {kind}"), 
		});
	}

	private static HoverTip CreateAncientHoverTip(ActModel nextAct)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		ActModel nextAct2 = nextAct;
		return PredictionHoverTipFactory.Text("next_act_ancient_prediction", delegate(LocString description)
		{
			description.Add("Ancient", ((EventModel)nextAct2.Ancient).Title);
		});
	}

	private static HoverTip CreateBossHoverTip(ActModel nextAct)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		int num = 1;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		CollectionsMarshal.AsSpan(list)[0] = nextAct.BossEncounter.Title.GetFormattedText();
		List<string> bossNames = list;
		if (nextAct.SecondBossEncounter != null)
		{
			bossNames.Add(nextAct.SecondBossEncounter.Title.GetFormattedText());
		}
		return PredictionHoverTipFactory.Text("next_act_boss_prediction", delegate(LocString description)
		{
			description.Add("BossNames", (IList<string>)bossNames);
		});
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(4)
		{
			new MethodInfo(MethodName.Create, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("kind"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnFocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnUnfocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			NNextActPredictionIcon nNextActPredictionIcon = Create(VariantUtils.ConvertTo<NextActPredictionIconKind>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<NNextActPredictionIcon>(ref nNextActPredictionIcon);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnFocus && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NClickableControl)this).OnFocus();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnUnfocus && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NClickableControl)this).OnUnfocus();
			ret = default(godot_variant);
			return true;
		}
		return ((NClickableControl)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			NNextActPredictionIcon nNextActPredictionIcon = Create(VariantUtils.ConvertTo<NextActPredictionIconKind>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<NNextActPredictionIcon>(ref nNextActPredictionIcon);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.Create)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.OnFocus)
		{
			return true;
		}
		if ((ref method) == MethodName.OnUnfocus)
		{
			return true;
		}
		return ((NClickableControl)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._kind)
		{
			_kind = VariantUtils.ConvertTo<NextActPredictionIconKind>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._icon)
		{
			_icon = VariantUtils.ConvertTo<TextureRect>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._outline)
		{
			_outline = VariantUtils.ConvertTo<TextureRect>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._secondBossIcon)
		{
			_secondBossIcon = VariantUtils.ConvertTo<TextureRect>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._secondBossIconOutline)
		{
			_secondBossIconOutline = VariantUtils.ConvertTo<TextureRect>(ref value);
			return true;
		}
		return ((NClickableControl)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._kind)
		{
			value = VariantUtils.CreateFrom<NextActPredictionIconKind>(ref _kind);
			return true;
		}
		if ((ref name) == PropertyName._icon)
		{
			value = VariantUtils.CreateFrom<TextureRect>(ref _icon);
			return true;
		}
		if ((ref name) == PropertyName._outline)
		{
			value = VariantUtils.CreateFrom<TextureRect>(ref _outline);
			return true;
		}
		if ((ref name) == PropertyName._secondBossIcon)
		{
			value = VariantUtils.CreateFrom<TextureRect>(ref _secondBossIcon);
			return true;
		}
		if ((ref name) == PropertyName._secondBossIconOutline)
		{
			value = VariantUtils.CreateFrom<TextureRect>(ref _secondBossIconOutline);
			return true;
		}
		return ((NClickableControl)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)2, PropertyName._kind, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._icon, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._outline, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._secondBossIcon, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._secondBossIconOutline, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		((NClickableControl)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._kind, Variant.From<NextActPredictionIconKind>(ref _kind));
		info.AddProperty(PropertyName._icon, Variant.From<TextureRect>(ref _icon));
		info.AddProperty(PropertyName._outline, Variant.From<TextureRect>(ref _outline));
		info.AddProperty(PropertyName._secondBossIcon, Variant.From<TextureRect>(ref _secondBossIcon));
		info.AddProperty(PropertyName._secondBossIconOutline, Variant.From<TextureRect>(ref _secondBossIconOutline));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NClickableControl)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._kind, ref val))
		{
			_kind = ((Variant)(ref val)).As<NextActPredictionIconKind>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._icon, ref val2))
		{
			_icon = ((Variant)(ref val2)).As<TextureRect>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._outline, ref val3))
		{
			_outline = ((Variant)(ref val3)).As<TextureRect>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._secondBossIcon, ref val4))
		{
			_secondBossIcon = ((Variant)(ref val4)).As<TextureRect>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._secondBossIconOutline, ref val5))
		{
			_secondBossIconOutline = ((Variant)(ref val5)).As<TextureRect>();
		}
	}
}
