using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace MeleeAttack;

[HarmonyPatch(typeof(NCharacterSelectButton))]
public static class CharacterSelectHoverFadePatch
{
	private const float IDLE_ICON_ALPHA = 0f;

	private const float IDLE_SHADOW_ALPHA = 0f;

	private const float IDLE_DELAY_SECONDS = 1f;

	private const float IDLE_FADE_DURATION = 2f;

	private const float HOVER_RESTORE_DURATION = 0.5f;

	private const float SHADOW_FADE_DURATION = 1f;

	private const string ICON_NODE = "%Icon";

	private const string SHADOW_NODE = "%Shadow";

	private static readonly string[] OUTLINE_NODES = new string[3] { "%OutlineLocal", "%OutlineRemote", "%OutlineMixed" };

	private static NCharacterSelectButton _hovered = null;

	private static bool _idleEnabled = false;

	private static float _idleProgress = 0f;

	private static Tween _idleTween = null;

	private static NCharacterSelectButton _tweenHost = null;

	private static readonly List<NCharacterSelectButton> _buttons = new List<NCharacterSelectButton>();

	private static readonly Dictionary<NCharacterSelectButton, float> _shadowBaseAlpha = new Dictionary<NCharacterSelectButton, float>();

	private static readonly Dictionary<NCharacterSelectButton, Tween> _shadowTweens = new Dictionary<NCharacterSelectButton, Tween>();

	[HarmonyPostfix]
	[HarmonyPatch("_Ready")]
	private static void ReadyPostfix(NCharacterSelectButton __instance)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		if (__instance != null && GodotObject.IsInstanceValid((GodotObject)(object)__instance))
		{
			if (!_buttons.Contains(__instance))
			{
				_buttons.Add(__instance);
			}
			Control nodeOrNull = ((Node)__instance).GetNodeOrNull<Control>(NodePath.op_Implicit("%Shadow"));
			if (nodeOrNull != null && GodotObject.IsInstanceValid((GodotObject)(object)nodeOrNull))
			{
				_shadowBaseAlpha[__instance] = ((CanvasItem)nodeOrNull).Modulate.A;
			}
			if (_tweenHost == null || !GodotObject.IsInstanceValid((GodotObject)(object)_tweenHost))
			{
				_tweenHost = __instance;
			}
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch("OnFocus")]
	private static void OnFocusPostfix(NCharacterSelectButton __instance)
	{
		_hovered = __instance;
		_idleEnabled = true;
		ApplyState(hovered: true);
	}

	[HarmonyPostfix]
	[HarmonyPatch("OnUnfocus")]
	private static void OnUnfocusPostfix(NCharacterSelectButton __instance)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		if (_hovered == __instance)
		{
			_hovered = null;
		}
		Callable val = Callable.From((Action)CheckAndMaybeFadeOut);
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
	}

	private static void CheckAndMaybeFadeOut()
	{
		if ((_hovered == null || !GodotObject.IsInstanceValid((GodotObject)(object)_hovered)) && _idleEnabled)
		{
			ApplyState(hovered: false);
		}
	}

	private static void ApplyState(bool hovered)
	{
		//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		for (int num = _buttons.Count - 1; num >= 0; num--)
		{
			NCharacterSelectButton val = _buttons[num];
			if (val == null || !GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				_buttons.RemoveAt(num);
				if (val != null)
				{
					_shadowBaseAlpha.Remove(val);
					_shadowTweens.Remove(val);
				}
			}
		}
		foreach (NCharacterSelectButton button in _buttons)
		{
			ApplyShadow(button, hovered);
		}
		if (_idleTween != null && _idleTween.IsValid())
		{
			_idleTween.Kill();
		}
		if (_tweenHost == null || !GodotObject.IsInstanceValid((GodotObject)(object)_tweenHost))
		{
			_idleProgress = (hovered ? 0f : 1f);
			RefreshAllVisuals();
			return;
		}
		float num2 = (hovered ? 0f : 1f);
		float num3 = (hovered ? 0.5f : 2f);
		float num4 = (hovered ? 0f : 1f);
		if (Mathf.IsEqualApprox(_idleProgress, num2) && num4 <= 0f)
		{
			_idleProgress = num2;
			RefreshAllVisuals();
			return;
		}
		_idleTween = ((Node)_tweenHost).CreateTween();
		_idleTween.TweenMethod(Callable.From<float>((Action<float>)delegate(float v)
		{
			_idleProgress = v;
			RefreshAllVisuals();
		}), Variant.op_Implicit(_idleProgress), Variant.op_Implicit(num2), (double)num3).SetDelay((double)num4).SetEase((EaseType)1)
			.SetTrans((TransitionType)1);
	}

	[HarmonyPostfix]
	[HarmonyPatch("_Process")]
	private static void ProcessPostfix(NCharacterSelectButton __instance, double delta)
	{
		if (__instance != null && GodotObject.IsInstanceValid((GodotObject)(object)__instance) && _idleEnabled && !(_idleProgress <= 0f))
		{
			ApplyOutlineFade(__instance, _idleProgress);
			ApplyIconAlpha(__instance, _idleProgress);
		}
	}

	private static void RefreshAllVisuals()
	{
		foreach (NCharacterSelectButton button in _buttons)
		{
			if (button != null && GodotObject.IsInstanceValid((GodotObject)(object)button))
			{
				ApplyIconAlpha(button, _idleProgress);
				ApplyOutlineFade(button, _idleProgress);
			}
		}
	}

	private static void ApplyIconAlpha(NCharacterSelectButton b, float p)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		TextureRect nodeOrNull = ((Node)b).GetNodeOrNull<TextureRect>(NodePath.op_Implicit("%Icon"));
		if (nodeOrNull != null && GodotObject.IsInstanceValid((GodotObject)(object)nodeOrNull))
		{
			float num = Mathf.Lerp(1f, 0f, p);
			Color selfModulate = ((CanvasItem)nodeOrNull).SelfModulate;
			if (!Mathf.IsEqualApprox(selfModulate.A, num))
			{
				selfModulate.A = num;
				((CanvasItem)nodeOrNull).SelfModulate = selfModulate;
			}
		}
	}

	private static void ApplyOutlineFade(NCharacterSelectButton b, float p)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		if (p <= 0f)
		{
			return;
		}
		float num = 1f - p;
		string[] oUTLINE_NODES = OUTLINE_NODES;
		foreach (string text in oUTLINE_NODES)
		{
			Control nodeOrNull = ((Node)b).GetNodeOrNull<Control>(NodePath.op_Implicit(text));
			if (nodeOrNull != null && GodotObject.IsInstanceValid((GodotObject)(object)nodeOrNull) && ((CanvasItem)nodeOrNull).Visible)
			{
				Color modulate = ((CanvasItem)nodeOrNull).Modulate;
				modulate.A *= num;
				((CanvasItem)nodeOrNull).Modulate = modulate;
			}
		}
	}

	private static void ApplyShadow(NCharacterSelectButton b, bool hovered)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		Control nodeOrNull = ((Node)b).GetNodeOrNull<Control>(NodePath.op_Implicit("%Shadow"));
		if (nodeOrNull != null && GodotObject.IsInstanceValid((GodotObject)(object)nodeOrNull))
		{
			if (_shadowTweens.TryGetValue(b, out var value) && value != null && value.IsValid())
			{
				value.Kill();
			}
			float value2;
			float num = ((!hovered) ? 0f : (_shadowBaseAlpha.TryGetValue(b, out value2) ? value2 : 1f));
			Tween val = ((Node)b).CreateTween();
			val.TweenProperty((GodotObject)(object)nodeOrNull, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(num), 1.0).SetEase((EaseType)1).SetTrans((TransitionType)1);
			_shadowTweens[b] = val;
		}
	}

	internal static void ResetGlobalState()
	{
		_idleEnabled = false;
		_idleProgress = 0f;
		_hovered = null;
		if (_idleTween != null && _idleTween.IsValid())
		{
			_idleTween.Kill();
		}
		_idleTween = null;
		if (_tweenHost != null && !GodotObject.IsInstanceValid((GodotObject)(object)_tweenHost))
		{
			_tweenHost = null;
		}
		for (int num = _buttons.Count - 1; num >= 0; num--)
		{
			NCharacterSelectButton val = _buttons[num];
			if (val == null || !GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				_buttons.RemoveAt(num);
				if (val != null)
				{
					_shadowBaseAlpha.Remove(val);
					_shadowTweens.Remove(val);
				}
			}
		}
	}
}
