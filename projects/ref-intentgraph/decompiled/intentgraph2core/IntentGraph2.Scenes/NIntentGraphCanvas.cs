using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using IntentGraph2.Models;
using IntentGraph2.Patches;
using IntentGraph2.Utils.GraphGenerator;
using IntentGraph2.Utils.Variable;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

namespace IntentGraph2.Scenes;

[ScriptPath("res://intentgraph2/src/Scenes/NIntentGraphCanvas.cs")]
public class NIntentGraphCanvas : Control
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName _Draw = StringName.op_Implicit("_Draw");

		public static readonly StringName GetFormattedAttackText = StringName.op_Implicit("GetFormattedAttackText");

		public static readonly StringName DrawTextureCover = StringName.op_Implicit("DrawTextureCover");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName AnimatedIcons = StringName.op_Implicit("AnimatedIcons");

		public static readonly StringName ShowCurrentMove = StringName.op_Implicit("ShowCurrentMove");

		public static readonly StringName AttackDamageFormat = StringName.op_Implicit("AttackDamageFormat");

		public static readonly StringName arrowTexture = StringName.op_Implicit("arrowTexture");

		public static readonly StringName groupBorderTexture = StringName.op_Implicit("groupBorderTexture");

		public static readonly StringName glowTexture = StringName.op_Implicit("glowTexture");

		public static readonly StringName font = StringName.op_Implicit("font");

		public static readonly StringName labelFont = StringName.op_Implicit("labelFont");

		public static readonly StringName glowColor = StringName.op_Implicit("glowColor");

		public static readonly StringName hasAnimatedIcons = StringName.op_Implicit("hasAnimatedIcons");

		public static readonly StringName previousStateLogLength = StringName.op_Implicit("previousStateLogLength");

		public static readonly StringName previousStateId = StringName.op_Implicit("previousStateId");

		public static readonly StringName lastDrawTime = StringName.op_Implicit("lastDrawTime");
	}

	public class SignalName : SignalName
	{
	}

	private const string BlankImageResourcePath = "res://intentgraph2/images/blank.png";

	private static readonly Regex PlaceholderFinder = new Regex("{{|{([^}]+)}", RegexOptions.Compiled);

	private const int ArrowWidth = 10;

	private const int ArrowEndLength = 15;

	private const int AnimatedIconFrameDurationMs = 80;

	private readonly Move InitMove = new Move("intentgraph2_special_init_move_", null, 0f, 0f, null, Array.Empty<int?>());

	private static readonly Dictionary<IntentType, string> IntentImageResourcePath = new Dictionary<IntentType, string>
	{
		{
			(IntentType)0,
			"res://images/packed/intents/attack/intent_attack_1.png"
		},
		{
			(IntentType)1,
			"res://images/packed/intents/intent_buff.png"
		},
		{
			(IntentType)2,
			"res://images/packed/intents/debuff/intent_megadebuff_01.png"
		},
		{
			(IntentType)3,
			"res://images/packed/intents/debuff/intent_megadebuff_01.png"
		},
		{
			(IntentType)4,
			"res://images/packed/intents/intent_defend.png"
		},
		{
			(IntentType)5,
			"res://images/packed/intents/intent_escape.png"
		},
		{
			(IntentType)6,
			"res://images/packed/intents/intent_heal.png"
		},
		{
			(IntentType)7,
			"res://images/packed/intents/intent_hidden.png"
		},
		{
			(IntentType)8,
			"res://images/packed/intents/intent_summon.png"
		},
		{
			(IntentType)9,
			"res://images/packed/intents/intent_sleep.png"
		},
		{
			(IntentType)10,
			"res://images/packed/intents/intent_stun.png"
		},
		{
			(IntentType)11,
			"res://images/packed/intents/intent_status_card.png"
		},
		{
			(IntentType)12,
			"res://images/packed/intents/intent_card_debuff.png"
		},
		{
			(IntentType)13,
			"res://images/packed/intents/intent_death_blow.png"
		},
		{
			(IntentType)14,
			"res://images/packed/intents/intent_unknown.png"
		}
	};

	private static readonly Dictionary<IntentType, string> IntentImageAnimationResourcePath = new Dictionary<IntentType, string>
	{
		{
			(IntentType)1,
			"res://images/packed/intents/buff/intent_buff_{0:00}.png"
		},
		{
			(IntentType)2,
			"res://images/packed/intents/debuff/intent_megadebuff_{0:00}.png"
		},
		{
			(IntentType)3,
			"res://images/packed/intents/debuff/intent_megadebuff_{0:00}.png"
		},
		{
			(IntentType)4,
			"res://images/packed/intents/defend/intent_defend_{0:00}.png"
		},
		{
			(IntentType)5,
			"res://images/packed/intents/escape/intent_escape_{0:00}.png"
		},
		{
			(IntentType)6,
			"res://images/packed/intents/heal/intent_heal_{0:00}.png"
		},
		{
			(IntentType)8,
			"res://images/packed/intents/summon/intent_summon_{0:00}.png"
		},
		{
			(IntentType)9,
			"res://images/packed/intents/sleep/intent_sleep_{0:00}.png"
		},
		{
			(IntentType)10,
			"res://images/packed/intents/stun/intent_stunned_{0:00}.png"
		},
		{
			(IntentType)11,
			"res://images/packed/intents/status/intent_statuscard_{0:00}.png"
		},
		{
			(IntentType)12,
			"res://images/packed/intents/card_debuff/intent_carddebuff_{0:00}.png"
		},
		{
			(IntentType)14,
			"res://images/packed/intents/unknown/intent_unknown_{0:00}.png"
		}
	};

	private static readonly Dictionary<IntentType, int> IntentImageAnimationFrameCounts = new Dictionary<IntentType, int>
	{
		{
			(IntentType)1,
			30
		},
		{
			(IntentType)2,
			11
		},
		{
			(IntentType)3,
			11
		},
		{
			(IntentType)4,
			45
		},
		{
			(IntentType)5,
			40
		},
		{
			(IntentType)6,
			45
		},
		{
			(IntentType)8,
			25
		},
		{
			(IntentType)9,
			16
		},
		{
			(IntentType)10,
			16
		},
		{
			(IntentType)11,
			19
		},
		{
			(IntentType)12,
			15
		},
		{
			(IntentType)14,
			30
		}
	};

	private static readonly Rect2 IconGroupLT = new Rect2(0f, 0f, 3f, 3f);

	private static readonly Rect2 IconGroupTop = new Rect2(3f, 0f, 26f, 3f);

	private static readonly Rect2 IconGroupTR = new Rect2(29f, 0f, 3f, 3f);

	private static readonly Rect2 IconGroupLeft = new Rect2(0f, 3f, 3f, 26f);

	private static readonly Rect2 IconGroupRight = new Rect2(29f, 3f, 3f, 26f);

	private static readonly Rect2 IconGroupBL = new Rect2(0f, 29f, 3f, 3f);

	private static readonly Rect2 IconGroupBottom = new Rect2(3f, 29f, 26f, 3f);

	private static readonly Rect2 IconGroupBR = new Rect2(29f, 29f, 3f, 3f);

	private static readonly Rect2 ArrowHorizontal = new Rect2(1f, 0f, 62f, 10f);

	private static readonly Rect2 ArrowVertical = new Rect2(65f, 1f, 10f, 62f);

	private static readonly Rect2 ArrowDR = new Rect2(0f, 11f, 10f, 10f);

	private static readonly Rect2 ArrowDL = new Rect2(11f, 11f, 10f, 10f);

	private static readonly Rect2 ArrowUR = new Rect2(0f, 22f, 10f, 10f);

	private static readonly Rect2 ArrowUL = new Rect2(11f, 22f, 10f, 10f);

	private static readonly Rect2 ArrowU = new Rect2(91f, 0f, 20f, 15f);

	private static readonly Rect2 ArrowD = new Rect2(91f, 35f, 20f, 15f);

	private static readonly Rect2 ArrowR = new Rect2(111f, 15f, 15f, 20f);

	private static readonly Rect2 ArrowL = new Rect2(76f, 15f, 15f, 20f);

	private Texture2D? arrowTexture;

	private Texture2D? groupBorderTexture;

	private Texture2D? glowTexture;

	private Dictionary<string, Texture2D> intentTextures = new Dictionary<string, Texture2D>();

	private Font? font;

	private Font? labelFont;

	private Color glowColor;

	private Graph? graph;

	private bool hasAnimatedIcons;

	private int previousStateLogLength;

	private string? previousStateId;

	private List<Move> glowingMoves = new List<Move>();

	private IntentGraphLocalizer? intentGraphLocalizer;

	private MonsterModel? monster;

	private ulong lastDrawTime;

	public Graph? Graph
	{
		get
		{
			return graph;
		}
		set
		{
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			graph = value;
			Graph? obj = graph;
			hasAnimatedIcons = obj != null && (obj.Moves?.Any((Move m) => m.Icons?.Any((Icon icon) => IntentImageAnimationFrameCounts.ContainsKey(icon.IntentType)) ?? false)).GetValueOrDefault();
			((Control)this).CustomMinimumSize = new Vector2((80f * graph?.Width).GetValueOrDefault(80f), (80f * graph?.Height).GetValueOrDefault(80f));
			previousStateId = null;
			((CanvasItem)this).QueueRedraw();
		}
	}

	public MonsterModel? Monster
	{
		get
		{
			return monster;
		}
		set
		{
			monster = value;
			if (value != null)
			{
				intentGraphLocalizer = new IntentGraphLocalizer(null, new VariableContext(value), null);
			}
			else
			{
				intentGraphLocalizer = null;
			}
		}
	}

	public bool AnimatedIcons { get; set; }

	public bool ShowCurrentMove { get; set; }

	private string AttackDamageFormat => IntentGraphMod.Config.AttackDamageFormat;

	public override void _Ready()
	{
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Expected O, but got Unknown
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Expected O, but got Unknown
		labelFont = (font = ResourceLoader.Load<Font>("res://themes/kreon_bold_glyph_space_one.tres", (string)null, (CacheMode)1));
		if (LocManager.Instance.Language == "zhs" || LocManager.Instance.Language == "zht")
		{
			labelFont = ResourceLoader.Load<Font>("res://intentgraph2/themes/kreon_bold_glyph_space_one_zhs.tres", (string)null, (CacheMode)1);
		}
		else if (!Engine.IsEditorHint() && !TestMode.IsOn && FontManager.NeedsFontSubstitution(LocManager.Instance.Language))
		{
			FontVariation val = new FontVariation();
			Font substituteFont = FontManager.GetSubstituteFont(LocManager.Instance.Language, (FontType)1);
			if (substituteFont != null)
			{
				val.BaseFont = substituteFont;
				((Font)val).Fallbacks.Add(ResourceLoader.Load<Font>("res://intentgraph2/images/ui/icon.png", (string)null, (CacheMode)1));
				labelFont = (Font?)(object)val;
			}
		}
		else
		{
			FontVariation val2 = new FontVariation();
			val2.BaseFont = labelFont;
			((Font)val2).Fallbacks.Add(ResourceLoader.Load<Font>("res://intentgraph2/images/ui/icon.png", (string)null, (CacheMode)1));
			labelFont = (Font?)(object)val2;
		}
	}

	public override void _Process(double delta)
	{
		if ((hasAnimatedIcons || ShowCurrentMove) && graph != null && ((CanvasItem)this).Visible && AnimatedIcons)
		{
			((CanvasItem)this).QueueRedraw();
		}
		else if (((CanvasItem)this).Visible && Time.GetTicksMsec() - lastDrawTime > 1000)
		{
			((CanvasItem)this).QueueRedraw();
		}
	}

	public override void _Draw()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		lastDrawTime = Time.GetTicksMsec();
		if (graph == null)
		{
			return;
		}
		if (ShowCurrentMove)
		{
			float num = (AnimatedIcons ? (0.3f + 0.4f * Mathf.Sin((float)lastDrawTime / 1000f * (float)Math.PI)) : 0.5f);
			glowColor = new Color(1f, 1f, 1f, num);
			DrawGlow(graph.Moves);
		}
		IEnumerable<Icon> icons = graph.Icons;
		foreach (Icon item in icons ?? Enumerable.Empty<Icon>())
		{
			DrawIcon(item);
		}
		IEnumerable<Move> moves = graph.Moves;
		foreach (Move item2 in moves ?? Enumerable.Empty<Move>())
		{
			DrawMove(item2);
		}
		IEnumerable<IconGroup> iconGroups = graph.IconGroups;
		foreach (IconGroup item3 in iconGroups ?? Enumerable.Empty<IconGroup>())
		{
			DrawIconGroup(item3);
		}
		IEnumerable<Arrow> arrows = graph.Arrows;
		foreach (Arrow item4 in arrows ?? Enumerable.Empty<Arrow>())
		{
			DrawArrow(item4);
		}
		IEnumerable<Label> labels = graph.Labels;
		foreach (Label item5 in labels ?? Enumerable.Empty<Label>())
		{
			DrawLabel(item5);
		}
	}

	private void DrawGlow(List<Move> moves)
	{
		List<Move> moves2 = moves;
		if (moves2 == null)
		{
			return;
		}
		MonsterModel? obj = Monster;
		if (((obj != null) ? obj.MoveStateMachine : null) == null)
		{
			return;
		}
		MonsterMoveStateMachine moveStateMachine = Monster.MoveStateMachine;
		if (!StateLogPatches.FullStateLog.TryGetValue(moveStateMachine, out List<MonsterState> value))
		{
			return;
		}
		if (previousStateLogLength == value.Count)
		{
			if (value.Count != 0)
			{
				string? text = previousStateId;
				List<MonsterState> list = value;
				if (!(text == list[list.Count - 1].Id))
				{
					goto IL_00bd;
				}
			}
			{
				foreach (Move glowingMove in glowingMoves)
				{
					DrawMoveGlow(glowingMove);
				}
				return;
			}
		}
		goto IL_00bd;
		IL_00bd:
		previousStateLogLength = value.Count;
		List<MonsterState> list2 = value;
		previousStateId = list2[list2.Count - 1].Id;
		glowingMoves.Clear();
		int j = 1;
		List<(Move, Move)> source = (from m in moves2
			where m.CurrentMoveCondition?.GetBool() ?? true
			select (curr: m, final: m)).ToList();
		for (; value.Count >= j; j++)
		{
			List<MonsterState> list3 = value;
			int num = j;
			MonsterState state = list3[list3.Count - num];
			List<(Move, Move)> list4 = source.Where<(Move, Move)>(((Move curr, Move final) m) => m.curr == null || (m.curr.Ids?.Contains(state.Id) ?? false)).ToList();
			if (list4.Count == 1)
			{
				glowingMoves.Add(list4[0].Item2);
				DrawMoveGlow(list4[0].Item2);
				return;
			}
			if (list4.Count == 0)
			{
				break;
			}
			source = list4.SelectMany<(Move, Move), (Move, Move)>(((Move curr, Move final) m) => (!(m.curr == null)) ? ((m.curr.PossiblePreviousMoveNodeIndices != null) ? m.curr.PossiblePreviousMoveNodeIndices.Select((int? i) => i.HasValue ? (curr: moves2[i.Value], final: m.final) : (curr: InitMove, final: m.final)) : new _003C_003Ez__ReadOnlySingleElementList<(Move, Move)>((null, m.final))) : new _003C_003Ez__ReadOnlySingleElementList<(Move, Move)>(m)).ToList();
		}
		foreach (Move item in (from m in source
			where m.curr == null || m.curr == InitMove
			select m.final).Distinct())
		{
			glowingMoves.Add(item);
			DrawMoveGlow(item);
		}
	}

	private void DrawMoveGlow(Move move)
	{
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if (glowTexture == null)
		{
			glowTexture = ResourceLoader.Load<Texture2D>("res://intentgraph2/images/ui/glow.png", (string)null, (CacheMode)1);
		}
		IEnumerable<Icon> icons = move.Icons;
		foreach (Icon item in icons ?? Enumerable.Empty<Icon>())
		{
			float num = 20f;
			((CanvasItem)this).DrawTextureRect(glowTexture, new Rect2(item.X * 80f - num, item.Y * 80f - num, 80f + 2f * num, 80f + 2f * num), false, (Color?)glowColor, false);
		}
	}

	private void DrawMove(Move move)
	{
		IEnumerable<Icon> icons = move.Icons;
		foreach (Icon item in icons ?? Enumerable.Empty<Icon>())
		{
			DrawIcon(item);
		}
	}

	private void DrawIcon(Icon icon)
	{
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		if (icon.MoveDetailType == MoveDetailIconType.None || !TryDrawDetailedIconIntent(icon))
		{
			DrawNormalIconIntent(icon);
		}
		string text = ((!string.IsNullOrEmpty(icon.ValueText)) ? icon.ValueText : (icon.Value?.ToString() ?? string.Empty));
		string text2 = ((string.IsNullOrEmpty(text) || (icon.Times <= 1 && string.IsNullOrEmpty(icon.TimesText))) ? string.Empty : ((!string.IsNullOrEmpty(icon.TimesText)) ? icon.TimesText : icon.Times.ToString()));
		string text3 = ((!string.IsNullOrEmpty(text2)) ? (text + "x" + text2) : text);
		if ((int)icon.IntentType == 0)
		{
			text3 = GetFormattedAttackText(text, text2, text3, icon.VolatileValueText);
		}
		if (!string.IsNullOrEmpty(text3))
		{
			Vector2 val = default(Vector2);
			((Vector2)(ref val))._002Ector(icon.X * 80f + 12f, icon.Y * 80f + 71f);
			((CanvasItem)this).DrawStringOutline(font, val, text3, (HorizontalAlignment)0, -1f, 22, 16, (Color?)new Color(0f, 0f, 0f, 0.5f), (JustificationFlag)3, (Direction)0, (Orientation)0, 0f);
			((CanvasItem)this).DrawString(font, val, text3, (HorizontalAlignment)0, -1f, 22, (Color?)null, (JustificationFlag)3, (Direction)0, (Orientation)0, 0f);
		}
	}

	private string GetFormattedAttackText(string valueText, string timesText, string text, string volatileValueText)
	{
		string valueText2 = valueText;
		string text2 = text;
		string timesText2 = timesText;
		if (string.IsNullOrEmpty(AttackDamageFormat) || AttackDamageFormat == "{base}" || Monster == null)
		{
			return text2;
		}
		string s = valueText2;
		if (!string.IsNullOrEmpty(volatileValueText) && intentGraphLocalizer != null)
		{
			s = intentGraphLocalizer.FormatWithVariables(volatileValueText, (string v, string? t) => (!(v == "originalValue")) ? null : valueText2);
		}
		int result;
		int? valueForModified = (int.TryParse(s, out result) ? new int?(result) : null);
		int result2;
		int? value = (int.TryParse(valueText2, out result2) ? new int?(result2) : null);
		int result3;
		int? times = (int.TryParse(timesText2, out result3) ? new int?(result3) : null);
		return PlaceholderFinder.Replace(AttackDamageFormat, delegate(Match match)
		{
			if (match.Value == "{{")
			{
				return "{";
			}
			switch (match.Groups[1].Value)
			{
			case "base":
				return text2;
			case "base:total":
				if (!value.HasValue || !times.HasValue)
				{
					return text2;
				}
				return (value.Value * times.Value).ToString();
			case "modified":
			{
				int? num2 = (valueForModified.HasValue ? new int?(ModifyDamage(valueForModified.Value)) : null);
				if (!num2.HasValue)
				{
					return text2;
				}
				if (!string.IsNullOrEmpty(timesText2))
				{
					return $"{num2.Value}x{timesText2}";
				}
				return num2.Value.ToString();
			}
			case "modified:total":
			{
				int? num = (valueForModified.HasValue ? new int?(ModifyDamage(valueForModified.Value)) : null);
				if (!num.HasValue)
				{
					return text2;
				}
				if (times.HasValue)
				{
					return (num.Value * times.Value).ToString();
				}
				if (!string.IsNullOrEmpty(timesText2))
				{
					return $"{num.Value}x{timesText2}";
				}
				return num.Value.ToString();
			}
			default:
				return string.Empty;
			}
		});
		int ModifyDamage(int damage)
		{
			Player me = LocalContext.GetMe(Monster.CombatState);
			if (me != null)
			{
				IEnumerable<AbstractModel> enumerable = default(IEnumerable<AbstractModel>);
				damage = (int)Hook.ModifyDamage(me.RunState, me.Creature.CombatState, me.Creature, Monster.Creature, (decimal)damage, (ValueProp)8, (CardModel)null, (CardPlay)null, (ModifyDamageHookType)14, (CardPreviewMode)0, ref enumerable);
			}
			return Math.Max(0, damage);
		}
	}

	private void DrawNormalIconIntent(Icon icon)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if ((int)icon.IntentType == 0)
		{
			Texture2D iconTexture = GetIconTexture(icon);
			((CanvasItem)this).DrawTextureRect(iconTexture, new Rect2(icon.X * 80f + 4f, icon.Y * 80f + 4f, 72f, 72f), false, (Color?)null, false);
		}
		else
		{
			Texture2D iconTexture2 = GetIconTexture(icon);
			((CanvasItem)this).DrawTextureRect(iconTexture2, new Rect2(icon.X * 80f + 4f, icon.Y * 80f, 72f, 72f), false, (Color?)null, false);
		}
	}

	private Texture2D GetIconTexture(Icon icon)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		IntentType val;
		if ((int)icon.IntentType == 0)
		{
			int num = icon.Value.GetValueOrDefault() * icon.Times;
			int num2 = ((num < 20) ? ((num < 5) ? 1 : ((num >= 10) ? 3 : 2)) : ((num >= 40) ? 5 : 4));
			int value = num2;
			val = (IntentType)0;
			string key = ((object)(IntentType)(ref val)).ToString() + value;
			if (!intentTextures.TryGetValue(key, out Texture2D value2))
			{
				return intentTextures[key] = ResourceLoader.Load<Texture2D>($"res://images/packed/intents/attack/intent_attack_{value}.png", (string)null, (CacheMode)1);
			}
			return value2;
		}
		if (!TryGetAnimatedIntentTexture(icon.IntentType, out Texture2D texture))
		{
			Dictionary<string, Texture2D> dictionary = intentTextures;
			val = icon.IntentType;
			if (!dictionary.TryGetValue(((object)(IntentType)(ref val)).ToString(), out texture))
			{
				string text = IntentImageResourcePath.GetValueOrDefault(icon.IntentType) ?? "res://intentgraph2/images/blank.png";
				Dictionary<string, Texture2D> dictionary2 = intentTextures;
				val = icon.IntentType;
				return dictionary2[((object)(IntentType)(ref val)).ToString()] = ResourceLoader.Load<Texture2D>(text, (string)null, (CacheMode)1);
			}
		}
		return texture;
	}

	private void DrawOriginalIntentBadge(Icon icon)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		float num = 36f;
		Texture2D iconTexture = GetIconTexture(icon);
		Rect2 val = default(Rect2);
		((Rect2)(ref val))._002Ector(icon.X * 80f + 80f - num - 8f, icon.Y * 80f + 80f - num - 8f, num, num);
		((CanvasItem)this).DrawTextureRect(iconTexture, val, false, (Color?)null, false);
	}

	private bool TryDrawDetailedIconIntent(Icon icon)
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(icon.ImageResourcePath))
		{
			return false;
		}
		float num = 44.199997f;
		float num2 = (80f - num) / 2f;
		Rect2 val = default(Rect2);
		((Rect2)(ref val))._002Ector(icon.X * 80f + num2, icon.Y * 80f + num2, num, num);
		string key = "resource:" + icon.ImageResourcePath;
		if (!intentTextures.TryGetValue(key, out Texture2D value))
		{
			Texture2D val3 = (intentTextures[key] = ResourceLoader.Load<Texture2D>(icon.ImageResourcePath, (string)null, (CacheMode)1));
			value = val3;
		}
		if (icon.MoveDetailType == MoveDetailIconType.Card)
		{
			DrawTextureCover(value, val);
		}
		else
		{
			((CanvasItem)this).DrawTextureRect(value, val, false, (Color?)null, false);
		}
		DrawOriginalIntentBadge(icon);
		return true;
	}

	private void DrawTextureCover(Texture2D texture, Rect2 destination)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		Vector2 size = texture.GetSize();
		if (size.X <= 0f || size.Y <= 0f)
		{
			((CanvasItem)this).DrawTextureRect(texture, destination, false, (Color?)null, false);
			return;
		}
		float num = Math.Min(size.X, size.Y);
		Rect2 val = default(Rect2);
		((Rect2)(ref val))._002Ector((size.X - num) / 2f, (size.Y - num) / 2f, num, num);
		((CanvasItem)this).DrawTextureRectRegion(texture, destination, val, (Color?)null, false, true);
	}

	private bool TryGetAnimatedIntentTexture(IntentType intentType, [NotNullWhen(true)] out Texture2D? texture)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		texture = null;
		if (!AnimatedIcons || !IntentImageAnimationFrameCounts.TryGetValue(intentType, out var value) || value <= 0 || !IntentImageAnimationResourcePath.TryGetValue(intentType, out string value2))
		{
			return false;
		}
		int num = (int)(lastDrawTime / 80 % (ulong)value);
		string key = $"{intentType}_{num}";
		if (!intentTextures.TryGetValue(key, out texture))
		{
			Texture2D val2 = (intentTextures[key] = ResourceLoader.Load<Texture2D>(string.Format(value2, num), (string)null, (CacheMode)1));
			texture = val2;
		}
		return texture != null;
	}

	private void DrawArrow(Arrow arrow)
	{
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0354: Unknown result type (might be due to invalid IL or missing references)
		//IL_0363: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_039f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0416: Unknown result type (might be due to invalid IL or missing references)
		//IL_0425: Unknown result type (might be due to invalid IL or missing references)
		//IL_042f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0434: Unknown result type (might be due to invalid IL or missing references)
		//IL_045b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0471: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_048a: Unknown result type (might be due to invalid IL or missing references)
		//IL_048f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		if (arrowTexture == null)
		{
			arrowTexture = ResourceLoader.Load<Texture2D>("res://intentgraph2/images/ui/arrow.png", (string)null, (CacheMode)1);
		}
		float[] path = arrow.Path;
		if (path.Length <= 3)
		{
			return;
		}
		bool flag = path[0] == 0f;
		float num = path[1];
		float num2 = path[2];
		int num3 = -1;
		float num6;
		float num7;
		for (int i = 3; i < path.Length; i++)
		{
			bool flag2 = i == 3;
			bool flag3 = i == path.Length - 1;
			float num4 = (flag ? path[i] : num);
			float num5 = (flag ? num2 : path[i]);
			num6 = num2 * 80f - 5f;
			num7 = num * 80f - 5f;
			int num8;
			if (flag)
			{
				bool flag4 = num4 > num;
				num8 = (flag4 ? 1 : 3);
				int num9 = ((!flag2) ? (flag4 ? 1 : (-1)) : 0) * 10 / 2;
				int num10 = (flag3 ? 15 : 5) * ((!flag4) ? 1 : (-1));
				float num11 = num * 80f + (float)num9;
				float num12 = num4 * 80f + (float)num10;
				float num13 = Math.Abs(num11 - num12);
				((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(Math.Min(num11, num12), num6, num13, 10f), ArrowHorizontal, (Color?)null, false, true);
			}
			else
			{
				bool flag5 = num5 > num2;
				num8 = (flag5 ? 2 : 0);
				int num14 = ((!flag2) ? (flag5 ? 1 : (-1)) : 0) * 10 / 2;
				int num15 = (flag3 ? 15 : 5) * ((!flag5) ? 1 : (-1));
				float num16 = num2 * 80f + (float)num14;
				float num17 = num5 * 80f + (float)num15;
				float num18 = Math.Abs(num16 - num17);
				((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7, Math.Min(num16, num17), 10f, num18), ArrowVertical, (Color?)null, false, true);
			}
			if (!flag2)
			{
				if ((num3 == 2 && num8 == 1) || (num3 == 3 && num8 == 0))
				{
					((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7, num6, 10f, 10f), ArrowUR, (Color?)null, false, true);
				}
				else if ((num3 == 2 && num8 == 3) || (num3 == 1 && num8 == 0))
				{
					((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7, num6, 10f, 10f), ArrowUL, (Color?)null, false, true);
				}
				else if ((num3 == 0 && num8 == 3) || (num3 == 1 && num8 == 2))
				{
					((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7, num6, 10f, 10f), ArrowDL, (Color?)null, false, true);
				}
				else if ((num3 == 0 && num8 == 1) || (num3 == 3 && num8 == 2))
				{
					((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7, num6, 10f, 10f), ArrowDR, (Color?)null, false, true);
				}
			}
			flag = !flag;
			num = num4;
			num2 = num5;
			num3 = num8;
		}
		num6 = num2 * 80f;
		num7 = num * 80f;
		switch (num3)
		{
		case 0:
			((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7 - ((Rect2)(ref ArrowU)).Size.X / 2f, num6, ((Rect2)(ref ArrowU)).Size.X, ((Rect2)(ref ArrowU)).Size.Y), ArrowU, (Color?)null, false, true);
			break;
		case 1:
			((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7 - 15f, num6 - ((Rect2)(ref ArrowR)).Size.Y / 2f, ((Rect2)(ref ArrowR)).Size.X, ((Rect2)(ref ArrowR)).Size.Y), ArrowR, (Color?)null, false, true);
			break;
		case 2:
			((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7 - ((Rect2)(ref ArrowD)).Size.X / 2f, num6 - 15f, ((Rect2)(ref ArrowD)).Size.X, ((Rect2)(ref ArrowD)).Size.Y), ArrowD, (Color?)null, false, true);
			break;
		case 3:
			((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7, num6 - ((Rect2)(ref ArrowL)).Size.Y / 2f, ((Rect2)(ref ArrowL)).Size.X, ((Rect2)(ref ArrowL)).Size.Y), ArrowL, (Color?)null, false, true);
			break;
		}
	}

	private void DrawLabel(Label label)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		string text = label.Text;
		int fontSize = label.FontSize;
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		Vector2 val = default(Vector2);
		((Vector2)(ref val))._002Ector(label.X * 80f, label.Y * 80f);
		string[] array = text.Split('\n');
		foreach (string text2 in array)
		{
			Vector2 val2 = val;
			if (label.Align == "right")
			{
				Vector2 stringSize = labelFont.GetStringSize(text, (HorizontalAlignment)0, -1f, fontSize, (JustificationFlag)3, (Direction)0, (Orientation)0);
				val2.X -= stringSize.X;
			}
			else if (label.Align != "left")
			{
				Vector2 stringSize2 = labelFont.GetStringSize(text, (HorizontalAlignment)0, -1f, fontSize, (JustificationFlag)3, (Direction)0, (Orientation)0);
				val2.X -= stringSize2.X / 2f;
			}
			((CanvasItem)this).DrawStringOutline(labelFont, val2, text2, (HorizontalAlignment)0, -1f, fontSize, 12, (Color?)new Color(0f, 0f, 0f, 0.5f), (JustificationFlag)3, (Direction)0, (Orientation)0, 0f);
			((CanvasItem)this).DrawString(labelFont, val2, text2, (HorizontalAlignment)0, -1f, fontSize, (Color?)null, (JustificationFlag)3, (Direction)0, (Orientation)0, 0f);
			val.Y += (float)fontSize + 2f;
		}
	}

	private void DrawIconGroup(IconGroup iconGroup)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_025c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_0270: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_027a: Unknown result type (might be due to invalid IL or missing references)
		if (groupBorderTexture == null)
		{
			groupBorderTexture = ResourceLoader.Load<Texture2D>("res://intentgraph2/images/ui/groupborder.png", (string)null, (CacheMode)1);
		}
		float num = iconGroup.X * 80f;
		float num2 = iconGroup.Y * 80f;
		float num3 = iconGroup.Width * 80f;
		float num4 = iconGroup.Height * 80f;
		Vector2 val = default(Vector2);
		((Vector2)(ref val))._002Ector(num, num2);
		((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val, new Vector2(3f, 3f)), IconGroupLT, (Color?)null, false, true);
		((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val + new Vector2(3f, 0f), new Vector2(num3 - 6f, 3f)), IconGroupTop, (Color?)null, false, true);
		((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val + new Vector2(num3 - 3f, 0f), new Vector2(3f, 3f)), IconGroupTR, (Color?)null, false, true);
		((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val + new Vector2(0f, 3f), new Vector2(3f, num4 - 6f)), IconGroupLeft, (Color?)null, false, true);
		((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val + new Vector2(num3 - 3f, 3f), new Vector2(3f, num4 - 6f)), IconGroupRight, (Color?)null, false, true);
		((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val + new Vector2(0f, num4 - 3f), new Vector2(3f, 3f)), IconGroupBL, (Color?)null, false, true);
		((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val + new Vector2(3f, num4 - 3f), new Vector2(num3 - 6f, 3f)), IconGroupBottom, (Color?)null, false, true);
		((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val + new Vector2(num3 - 3f, num4 - 3f), new Vector2(3f, 3f)), IconGroupBR, (Color?)null, false, true);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Expected O, but got Unknown
		//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(5)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Draw, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetFormattedAttackText, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("valueText"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)4, StringName.op_Implicit("timesText"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)4, StringName.op_Implicit("text"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)4, StringName.op_Implicit("volatileValueText"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.DrawTextureCover, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("texture"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Texture2D"), false),
				new PropertyInfo((Type)7, StringName.op_Implicit("destination"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Draw && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((CanvasItem)this)._Draw();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.GetFormattedAttackText && ((NativeVariantPtrArgs)(ref args)).Count == 4)
		{
			string formattedAttackText = GetFormattedAttackText(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[2]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[3]));
			ret = VariantUtils.CreateFrom<string>(ref formattedAttackText);
			return true;
		}
		if ((ref method) == MethodName.DrawTextureCover && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			DrawTextureCover(VariantUtils.ConvertTo<Texture2D>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Rect2>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName._Draw)
		{
			return true;
		}
		if ((ref method) == MethodName.GetFormattedAttackText)
		{
			return true;
		}
		if ((ref method) == MethodName.DrawTextureCover)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.AnimatedIcons)
		{
			AnimatedIcons = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ShowCurrentMove)
		{
			ShowCurrentMove = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.arrowTexture)
		{
			arrowTexture = VariantUtils.ConvertTo<Texture2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.groupBorderTexture)
		{
			groupBorderTexture = VariantUtils.ConvertTo<Texture2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.glowTexture)
		{
			glowTexture = VariantUtils.ConvertTo<Texture2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.font)
		{
			font = VariantUtils.ConvertTo<Font>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.labelFont)
		{
			labelFont = VariantUtils.ConvertTo<Font>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.glowColor)
		{
			glowColor = VariantUtils.ConvertTo<Color>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.hasAnimatedIcons)
		{
			hasAnimatedIcons = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.previousStateLogLength)
		{
			previousStateLogLength = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.previousStateId)
		{
			previousStateId = VariantUtils.ConvertTo<string>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.lastDrawTime)
		{
			lastDrawTime = VariantUtils.ConvertTo<ulong>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.AnimatedIcons)
		{
			bool animatedIcons = AnimatedIcons;
			value = VariantUtils.CreateFrom<bool>(ref animatedIcons);
			return true;
		}
		if ((ref name) == PropertyName.ShowCurrentMove)
		{
			bool animatedIcons = ShowCurrentMove;
			value = VariantUtils.CreateFrom<bool>(ref animatedIcons);
			return true;
		}
		if ((ref name) == PropertyName.AttackDamageFormat)
		{
			string attackDamageFormat = AttackDamageFormat;
			value = VariantUtils.CreateFrom<string>(ref attackDamageFormat);
			return true;
		}
		if ((ref name) == PropertyName.arrowTexture)
		{
			value = VariantUtils.CreateFrom<Texture2D>(ref arrowTexture);
			return true;
		}
		if ((ref name) == PropertyName.groupBorderTexture)
		{
			value = VariantUtils.CreateFrom<Texture2D>(ref groupBorderTexture);
			return true;
		}
		if ((ref name) == PropertyName.glowTexture)
		{
			value = VariantUtils.CreateFrom<Texture2D>(ref glowTexture);
			return true;
		}
		if ((ref name) == PropertyName.font)
		{
			value = VariantUtils.CreateFrom<Font>(ref font);
			return true;
		}
		if ((ref name) == PropertyName.labelFont)
		{
			value = VariantUtils.CreateFrom<Font>(ref labelFont);
			return true;
		}
		if ((ref name) == PropertyName.glowColor)
		{
			value = VariantUtils.CreateFrom<Color>(ref glowColor);
			return true;
		}
		if ((ref name) == PropertyName.hasAnimatedIcons)
		{
			value = VariantUtils.CreateFrom<bool>(ref hasAnimatedIcons);
			return true;
		}
		if ((ref name) == PropertyName.previousStateLogLength)
		{
			value = VariantUtils.CreateFrom<int>(ref previousStateLogLength);
			return true;
		}
		if ((ref name) == PropertyName.previousStateId)
		{
			value = VariantUtils.CreateFrom<string>(ref previousStateId);
			return true;
		}
		if ((ref name) == PropertyName.lastDrawTime)
		{
			value = VariantUtils.CreateFrom<ulong>(ref lastDrawTime);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName.arrowTexture, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.groupBorderTexture, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.glowTexture, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.font, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.labelFont, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)20, PropertyName.glowColor, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.hasAnimatedIcons, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName.previousStateLogLength, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName.previousStateId, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName.lastDrawTime, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.AnimatedIcons, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.ShowCurrentMove, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)4, PropertyName.AttackDamageFormat, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName animatedIcons = PropertyName.AnimatedIcons;
		bool animatedIcons2 = AnimatedIcons;
		info.AddProperty(animatedIcons, Variant.From<bool>(ref animatedIcons2));
		StringName showCurrentMove = PropertyName.ShowCurrentMove;
		animatedIcons2 = ShowCurrentMove;
		info.AddProperty(showCurrentMove, Variant.From<bool>(ref animatedIcons2));
		info.AddProperty(PropertyName.arrowTexture, Variant.From<Texture2D>(ref arrowTexture));
		info.AddProperty(PropertyName.groupBorderTexture, Variant.From<Texture2D>(ref groupBorderTexture));
		info.AddProperty(PropertyName.glowTexture, Variant.From<Texture2D>(ref glowTexture));
		info.AddProperty(PropertyName.font, Variant.From<Font>(ref font));
		info.AddProperty(PropertyName.labelFont, Variant.From<Font>(ref labelFont));
		info.AddProperty(PropertyName.glowColor, Variant.From<Color>(ref glowColor));
		info.AddProperty(PropertyName.hasAnimatedIcons, Variant.From<bool>(ref hasAnimatedIcons));
		info.AddProperty(PropertyName.previousStateLogLength, Variant.From<int>(ref previousStateLogLength));
		info.AddProperty(PropertyName.previousStateId, Variant.From<string>(ref previousStateId));
		info.AddProperty(PropertyName.lastDrawTime, Variant.From<ulong>(ref lastDrawTime));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.AnimatedIcons, ref val))
		{
			AnimatedIcons = ((Variant)(ref val)).As<bool>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.ShowCurrentMove, ref val2))
		{
			ShowCurrentMove = ((Variant)(ref val2)).As<bool>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.arrowTexture, ref val3))
		{
			arrowTexture = ((Variant)(ref val3)).As<Texture2D>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName.groupBorderTexture, ref val4))
		{
			groupBorderTexture = ((Variant)(ref val4)).As<Texture2D>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName.glowTexture, ref val5))
		{
			glowTexture = ((Variant)(ref val5)).As<Texture2D>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName.font, ref val6))
		{
			font = ((Variant)(ref val6)).As<Font>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName.labelFont, ref val7))
		{
			labelFont = ((Variant)(ref val7)).As<Font>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName.glowColor, ref val8))
		{
			glowColor = ((Variant)(ref val8)).As<Color>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName.hasAnimatedIcons, ref val9))
		{
			hasAnimatedIcons = ((Variant)(ref val9)).As<bool>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName.previousStateLogLength, ref val10))
		{
			previousStateLogLength = ((Variant)(ref val10)).As<int>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName.previousStateId, ref val11))
		{
			previousStateId = ((Variant)(ref val11)).As<string>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName.lastDrawTime, ref val12))
		{
			lastDrawTime = ((Variant)(ref val12)).As<ulong>();
		}
	}
}
