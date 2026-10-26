using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.addons.mega_text;
using RandomForeseer.RandomForeseerCode.Data;

namespace RandomForeseer.RandomForeseerCode.InCombat.Nodes;

[ScriptPath("res://RandomForeseerCode/InCombat/Nodes/NCombatPredictionDamageIndicator.cs")]
[ScriptPath("res://RandomForeseerCode/InCombat/Nodes/NCombatPredictionDamageIndicator.cs")]
internal sealed class NCombatPredictionDamageIndicator : MarginContainer
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName ShowPrediction = StringName.op_Implicit("ShowPrediction");

		public static readonly StringName ClearSourceIcons = StringName.op_Implicit("ClearSourceIcons");

		public static readonly StringName ShowHoverTips = StringName.op_Implicit("ShowHoverTips");

		public static readonly StringName OnMouseEntered = StringName.op_Implicit("OnMouseEntered");

		public static readonly StringName OnMouseExited = StringName.op_Implicit("OnMouseExited");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _sourceIcons = StringName.op_Implicit("_sourceIcons");

		public static readonly StringName _damageLabel = StringName.op_Implicit("_damageLabel");

		public static readonly StringName _initialized = StringName.op_Implicit("_initialized");

		public static readonly StringName _hasRisk = StringName.op_Implicit("_hasRisk");

		public static readonly StringName _isHovering = StringName.op_Implicit("_isHovering");
	}

	public class SignalName : SignalName
	{
	}

	private const string ScenePath = "res://RandomForeseer/scenes/combat_prediction_damage_indicator.tscn";

	private static readonly Color DefaultOutlineColor = StsColors.halfTransparentBlack;

	private static readonly Color BlockedOutlineColor = new Color("2B5A85");

	private static readonly Color LethalOutlineColor = new Color("900000");

	private Creature _target;

	private HBoxContainer _sourceIcons;

	private Label _damageLabel;

	private bool _initialized;

	private DamagePredictionTarget? _prediction;

	private bool _hasRisk;

	private IEnumerable<IHoverTip> _hoverTips = Array.Empty<IHoverTip>();

	private bool _isHovering;

	public static NCombatPredictionDamageIndicator Create(Creature target)
	{
		NCombatPredictionDamageIndicator nCombatPredictionDamageIndicator = GD.Load<PackedScene>("res://RandomForeseer/scenes/combat_prediction_damage_indicator.tscn").Instantiate<NCombatPredictionDamageIndicator>((GenEditState)0);
		nCombatPredictionDamageIndicator._target = target;
		return nCombatPredictionDamageIndicator;
	}

	public override void _Ready()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		_sourceIcons = ((Node)this).GetNode<HBoxContainer>(NodePath.op_Implicit("Content/SourceIcons"));
		_damageLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("Content/DamageLabel"));
		if (_sourceIcons == null || _damageLabel == null)
		{
			Entry.Logger.Error("Failed to initialize NCombatPredictionDamageIndicator: missing child nodes.", 1);
			((CanvasItem)this).Visible = false;
			return;
		}
		((GodotObject)this).Connect(SignalName.MouseEntered, Callable.From((Action)OnMouseEntered), 0u);
		((GodotObject)this).Connect(SignalName.MouseExited, Callable.From((Action)OnMouseExited), 0u);
		_initialized = true;
		ShowPrediction();
	}

	public void SetPrediction(DamagePredictionTarget prediction, bool hasRisk)
	{
		_prediction = prediction;
		_hasRisk = hasRisk;
		if (_initialized)
		{
			ShowPrediction();
		}
	}

	public void SetHoverTips(IEnumerable<IHoverTip> hoverTips)
	{
		_hoverTips = hoverTips;
		if (_isHovering)
		{
			ShowHoverTips();
		}
	}

	private void ShowPrediction()
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		ClearSourceIcons();
		DamagePredictionTarget prediction = _prediction;
		if ((object)prediction != null)
		{
			IReadOnlyList<DamagePredictionLine> damageLines = prediction.DamageLines;
			if (damageLines != null && damageLines.Count > 0)
			{
				foreach (var item3 in from line in prediction.DamageLines
					group line by line.Source.Id into @group
					select (Source: @group.First().Source, @group.Count()))
				{
					AbstractModel item = item3.Source;
					int item2 = item3.Item2;
					NCombatPredictionSourceIcon nCombatPredictionSourceIcon = NCombatPredictionSourceIcon.Create(GetIcon(item), item2);
					GodotTreeExtensions.AddChildSafely((Node)(object)_sourceIcons, (Node)(object)nCombatPredictionSourceIcon);
				}
				string amountText = GetAmountText(prediction, _hasRisk);
				Color outlineColor = GetOutlineColor(prediction);
				_damageLabel.Text = amountText;
				((Control)_damageLabel).AddThemeColorOverride(Label.FontOutlineColor, outlineColor);
				((CanvasItem)this).Visible = true;
				((Control)this).CustomMinimumSize = Vector2.Zero;
				((Control)this).Size = ((Control)this).GetCombinedMinimumSize();
				return;
			}
		}
		((CanvasItem)this).Visible = false;
		((Control)this).Size = Vector2.Zero;
		((Control)this).CustomMinimumSize = Vector2.Zero;
	}

	private void ClearSourceIcons()
	{
		foreach (Node item in ((IEnumerable<Node>)((Node)_sourceIcons).GetChildren(false)).ToList())
		{
			GodotTreeExtensions.RemoveChildSafely((Node)(object)_sourceIcons, item);
			GodotTreeExtensions.QueueFreeSafely(item);
		}
	}

	private void ShowHoverTips()
	{
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			NCreature creatureNode = instance.GetCreatureNode(_target);
			if (creatureNode != null)
			{
				creatureNode.ShowHoverTips(_hoverTips);
			}
		}
	}

	private void OnMouseEntered()
	{
		_isHovering = true;
		ShowHoverTips();
	}

	private void OnMouseExited()
	{
		_isHovering = false;
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			NCreature creatureNode = instance.GetCreatureNode(_target);
			if (creatureNode != null)
			{
				creatureNode.HideHoverTips();
			}
		}
	}

	private static Color GetOutlineColor(DamagePredictionTarget prediction)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if ((object)prediction != null)
		{
			if (prediction.WasTargetKilled)
			{
				return LethalOutlineColor;
			}
			if (prediction.TotalUnblockedDamage == 0m && prediction.TotalDamage > 0m)
			{
				return BlockedOutlineColor;
			}
		}
		return DefaultOutlineColor;
	}

	private static string GetAmountText(DamagePredictionTarget prediction, bool hasRisk)
	{
		int num = (int)prediction.TotalDamage;
		int num2 = (int)prediction.TotalUnblockedDamage;
		string text = ((num2 < num) ? $"{num}({num2})" : num.ToString());
		if (!hasRisk || !ModData.Settings.ShowDriftWarnings)
		{
			return text;
		}
		return text + "*";
	}

	private static Texture2D GetIcon(AbstractModel source)
	{
		CardModel val = (CardModel)(object)((source is CardModel) ? source : null);
		Texture2D val7;
		if (val == null)
		{
			PotionModel val2 = (PotionModel)(object)((source is PotionModel) ? source : null);
			if (val2 == null)
			{
				EnchantmentModel val3 = (EnchantmentModel)(object)((source is EnchantmentModel) ? source : null);
				if (val3 == null)
				{
					OrbModel val4 = (OrbModel)(object)((source is OrbModel) ? source : null);
					if (val4 == null)
					{
						PowerModel val5 = (PowerModel)(object)((source is PowerModel) ? source : null);
						if (val5 == null)
						{
							RelicModel val6 = (RelicModel)(object)((source is RelicModel) ? source : null);
							val7 = ((val6 == null) ? null : val6.Icon);
						}
						else
						{
							val7 = val5.Icon;
						}
					}
					else
					{
						val7 = (Texture2D)(object)val4.Icon;
					}
				}
				else
				{
					val7 = (Texture2D)(object)val3.Icon;
				}
			}
			else
			{
				val7 = val2.Image;
			}
		}
		else
		{
			val7 = GetCardIcon(val);
		}
		return val7 ?? ((PowerModel)ModelDb.Power<StrengthPower>()).Icon;
	}

	private static Texture2D? GetCardIcon(CardModel card)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Expected O, but got Unknown
		if (ResourceLoader.Exists(card.PortraitPath, ""))
		{
			Texture2D portrait = card.Portrait;
			if (portrait != null)
			{
				Vector2 size = portrait.GetSize();
				if ((int)card.Rarity != 5 || size.Y <= size.X)
				{
					return portrait;
				}
				AtlasTexture val = (AtlasTexture)(object)((portrait is AtlasTexture) ? portrait : null);
				var (atlas, val2) = ((val == null) ? (portrait, new Rect2(Vector2.Zero, size)) : (val.Atlas, val.Region));
				Vector2 val3 = default(Vector2);
				((Vector2)(ref val3))._002Ector(((Rect2)(ref val2)).Size.X * 28f / 299f, ((Rect2)(ref val2)).Size.Y * 47f / 421f);
				Vector2 val4 = default(Vector2);
				((Vector2)(ref val4))._002Ector(((Rect2)(ref val2)).Size.X * 250f / 299f, ((Rect2)(ref val2)).Size.Y * 190f / 421f);
				return (Texture2D?)new AtlasTexture
				{
					Atlas = atlas,
					Region = new Rect2(((Rect2)(ref val2)).Position + val3, val4),
					FilterClip = true
				};
			}
		}
		return null;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(6)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ShowPrediction, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ClearSourceIcons, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ShowHoverTips, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnMouseEntered, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnMouseExited, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ShowPrediction && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ShowPrediction();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ClearSourceIcons && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ClearSourceIcons();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ShowHoverTips && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ShowHoverTips();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnMouseEntered && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnMouseEntered();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnMouseExited && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnMouseExited();
			ret = default(godot_variant);
			return true;
		}
		return ((MarginContainer)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.ShowPrediction)
		{
			return true;
		}
		if ((ref method) == MethodName.ClearSourceIcons)
		{
			return true;
		}
		if ((ref method) == MethodName.ShowHoverTips)
		{
			return true;
		}
		if ((ref method) == MethodName.OnMouseEntered)
		{
			return true;
		}
		if ((ref method) == MethodName.OnMouseExited)
		{
			return true;
		}
		return ((MarginContainer)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._sourceIcons)
		{
			_sourceIcons = VariantUtils.ConvertTo<HBoxContainer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._damageLabel)
		{
			_damageLabel = VariantUtils.ConvertTo<Label>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._initialized)
		{
			_initialized = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hasRisk)
		{
			_hasRisk = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isHovering)
		{
			_isHovering = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
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
		if ((ref name) == PropertyName._sourceIcons)
		{
			value = VariantUtils.CreateFrom<HBoxContainer>(ref _sourceIcons);
			return true;
		}
		if ((ref name) == PropertyName._damageLabel)
		{
			value = VariantUtils.CreateFrom<Label>(ref _damageLabel);
			return true;
		}
		if ((ref name) == PropertyName._initialized)
		{
			value = VariantUtils.CreateFrom<bool>(ref _initialized);
			return true;
		}
		if ((ref name) == PropertyName._hasRisk)
		{
			value = VariantUtils.CreateFrom<bool>(ref _hasRisk);
			return true;
		}
		if ((ref name) == PropertyName._isHovering)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isHovering);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._sourceIcons, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._damageLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._initialized, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._hasRisk, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isHovering, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
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
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._sourceIcons, Variant.From<HBoxContainer>(ref _sourceIcons));
		info.AddProperty(PropertyName._damageLabel, Variant.From<Label>(ref _damageLabel));
		info.AddProperty(PropertyName._initialized, Variant.From<bool>(ref _initialized));
		info.AddProperty(PropertyName._hasRisk, Variant.From<bool>(ref _hasRisk));
		info.AddProperty(PropertyName._isHovering, Variant.From<bool>(ref _isHovering));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._sourceIcons, ref val))
		{
			_sourceIcons = ((Variant)(ref val)).As<HBoxContainer>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._damageLabel, ref val2))
		{
			_damageLabel = ((Variant)(ref val2)).As<Label>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._initialized, ref val3))
		{
			_initialized = ((Variant)(ref val3)).As<bool>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._hasRisk, ref val4))
		{
			_hasRisk = ((Variant)(ref val4)).As<bool>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._isHovering, ref val5))
		{
			_isHovering = ((Variant)(ref val5)).As<bool>();
		}
	}
}
