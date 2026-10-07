using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MeleeAttack;

public static class Toutou
{
	private class FloatWrapper
	{
		public float Value;

		public FloatWrapper(float value)
		{
			Value = value;
		}
	}

	public const float DEFAULT_FADE_DURATION = 0.7f;

	public const float DEFAULT_RETURN_MOVE_DURATION = 0.2f;

	public const float DEFAULT_FADE_START_ALPHA = 0.7f;

	public const float DASH_OUT_DISTANCE = 2000f;

	public const float BASE_DASH_OUT_DURATION = 0.2f;

	public const float OPPOSITE_SCREEN_MULTIPLIER = 1.2f;

	private static readonly ConditionalWeakTable<NCreature, FloatWrapper> _originalAlphas = new ConditionalWeakTable<NCreature, FloatWrapper>();

	private static readonly ConditionalWeakTable<NCreature, Tween> _fadeTweens = new ConditionalWeakTable<NCreature, Tween>();

	internal static readonly ConditionalWeakTable<NCreature, SimpleTeleportPatch.Vector2Wrapper> _stretchOriginalScales = new ConditionalWeakTable<NCreature, SimpleTeleportPatch.Vector2Wrapper>();

	private static readonly ConditionalWeakTable<NCreature, Tween> _stretchTweens = new ConditionalWeakTable<NCreature, Tween>();

	public static void StartFadeIn(NCreature node, float duration = 0.7f, float startAlpha = 0f)
	{
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		NCreature obj = node;
		Node2D val = ((obj != null) ? obj.Body : null);
		if (val == null || !GodotObject.IsInstanceValid((GodotObject)(object)val))
		{
			return;
		}
		if (!_originalAlphas.TryGetValue(node, out var _))
		{
			float a = ((CanvasItem)val).Modulate.A;
			_originalAlphas.Add(node, new FloatWrapper(a));
		}
		CancelFade(node, instantRestore: false);
		((CanvasItem)val).Modulate = new Color(((CanvasItem)val).Modulate, startAlpha);
		Tween tween = ((Node)val).CreateTween();
		tween.SetTrans((TransitionType)0);
		tween.SetEase((EaseType)2);
		tween.TweenProperty((GodotObject)(object)val, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(_originalAlphas.TryGetValue(node, out var value2) ? value2.Value : 1f), (double)duration);
		_fadeTweens.AddOrUpdate(node, tween);
		tween.Finished += delegate
		{
			if (_fadeTweens.TryGetValue(node, out var value3) && value3 == tween)
			{
				_fadeTweens.Remove(node);
			}
		};
	}

	public static void CancelFade(NCreature node, bool instantRestore = true)
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		if (_fadeTweens.TryGetValue(node, out var value))
		{
			if (value != null && GodotObject.IsInstanceValid((GodotObject)(object)value))
			{
				value.Kill();
			}
			_fadeTweens.Remove(node);
		}
		if (instantRestore)
		{
			Node2D body = node.Body;
			if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
			{
				FloatWrapper value2;
				float num = (_originalAlphas.TryGetValue(node, out value2) ? value2.Value : 1f);
				((CanvasItem)body).Modulate = new Color(((CanvasItem)body).Modulate, num);
			}
		}
	}

	public static void CancelStretch(NCreature node)
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		if (_stretchTweens.TryGetValue(node, out var value))
		{
			if (value != null && GodotObject.IsInstanceValid((GodotObject)(object)value))
			{
				value.Kill();
			}
			_stretchTweens.Remove(node);
		}
		if (_stretchOriginalScales.TryGetValue(node, out var value2))
		{
			Node2D body = node.Body;
			if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
			{
				body.Scale = value2.Value;
			}
			_stretchOriginalScales.Remove(node);
		}
	}

	public static void OnAttackStart(NCreature node)
	{
		CancelFade(node);
		CancelStretch(node);
	}

	private static void SnapGlobalViaLocal(NCreature node, Vector2 targetPos)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			((Control)node).Position = ((Control)node).Position + (targetPos - ((Control)node).GlobalPosition);
		}
	}

	public static async Task StartReturnMove(NCreature node, Vector2 targetPos, float duration = 0.2f, bool skipStretch = false)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		Node2D visual = node.Body;
		if (visual == null || !GodotObject.IsInstanceValid((GodotObject)(object)visual))
		{
			SnapGlobalViaLocal(node, targetPos);
			return;
		}
		if (skipStretch)
		{
			Vector2 startPos = ((Control)node).GlobalPosition;
			if (((Vector2)(ref startPos)).DistanceSquaredTo(targetPos) < 0.0001f)
			{
				SnapGlobalViaLocal(node, targetPos);
				return;
			}
			Tween moveTween = ((Node)node).CreateTween();
			moveTween.SetTrans((TransitionType)0);
			moveTween.SetEase((EaseType)2);
			TaskCompletionSource<bool> moveTcs = new TaskCompletionSource<bool>();
			moveTween.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
			{
				//IL_0037: Unknown result type (might be due to invalid IL or missing references)
				//IL_003d: Unknown result type (might be due to invalid IL or missing references)
				float t2 = t / duration;
				float easedProgress2 = SimpleTeleportPatch.GetEasedProgress(t2);
				((Control)node).GlobalPosition = ((Vector2)(ref startPos)).Lerp(targetPos, easedProgress2);
			}), Variant.op_Implicit(0f), Variant.op_Implicit(duration), (double)duration);
			moveTween.Finished += delegate
			{
				moveTcs.SetResult(result: true);
			};
			await moveTcs.Task;
			SnapGlobalViaLocal(node, targetPos);
			return;
		}
		Vector2 origScale = visual.Scale;
		Vector2 absScale = new Vector2(Math.Abs(origScale.X), Math.Abs(origScale.Y));
		float sign = Math.Sign(origScale.X);
		if (Math.Abs(origScale.X) < 0.001f)
		{
			sign = 1f;
		}
		visual.Scale = new Vector2(absScale.X * sign, absScale.Y);
		Vector2 startPosStretch = ((Control)node).GlobalPosition;
		_stretchOriginalScales.AddOrUpdate(node, new SimpleTeleportPatch.Vector2Wrapper(absScale));
		Tween tween = ((Node)node).CreateTween();
		tween.SetParallel(false);
		tween.SetTrans((TransitionType)0);
		tween.SetEase((EaseType)2);
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		tween.TweenMethod(Callable.From<float>((Action<float>)delegate(float t)
		{
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			float num = t / duration;
			float easedProgress = SimpleTeleportPatch.GetEasedProgress(num);
			((Control)node).GlobalPosition = ((Vector2)(ref startPosStretch)).Lerp(targetPos, easedProgress);
			float stretchFactor = SimpleTeleportPatch.GetStretchFactor(num, 2f, 6f);
			float num2 = absScale.X * stretchFactor;
			float y = absScale.Y;
			visual.Scale = new Vector2(num2 * sign, y);
		}), Variant.op_Implicit(0f), Variant.op_Implicit(duration), (double)duration);
		tween.Finished += delegate
		{
			tcs.SetResult(result: true);
		};
		await tcs.Task;
		visual.Scale = origScale;
		_stretchOriginalScales.Remove(node);
		SnapGlobalViaLocal(node, targetPos);
	}

	public static async Task DashOutOfScreen(NCreature node, Vector2 dashDir, CancellationToken token)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node) || token.IsCancellationRequested)
		{
			return;
		}
		if (((Vector2)(ref dashDir)).LengthSquared() < 0.001f)
		{
			dashDir = Vector2.Right;
		}
		dashDir = ((Vector2)(ref dashDir)).Normalized();
		Node2D visual = node.Body;
		if (visual != null && GodotObject.IsInstanceValid((GodotObject)(object)visual))
		{
			float sign = Math.Sign(dashDir.X);
			if (Math.Abs(dashDir.X) < 0.001f)
			{
				sign = 1f;
			}
			float absX = Math.Abs(visual.Scale.X);
			visual.Scale = new Vector2(absX * sign, visual.Scale.Y);
		}
		Vector2 outPos = ((Control)node).GlobalPosition + dashDir * 2000f;
		Tween tween = ((Node)node).CreateTween();
		tween.TweenProperty((GodotObject)(object)node, NodePath.op_Implicit("global_position"), Variant.op_Implicit(outPos), 0.20000000298023224).SetTrans((TransitionType)4).SetEase((EaseType)1);
		TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
		tween.Finished += delegate
		{
			tcs.TrySetResult(result: true);
		};
		using (token.Register(delegate
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)tween))
			{
				tween.Kill();
			}
			tcs.TrySetResult(result: false);
		}))
		{
			await tcs.Task;
			if (!token.IsCancellationRequested)
			{
			}
		}
	}

	public static void FlashToLeftSide(NCreature node, Vector2 homePos)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			Viewport viewport = ((Node)node).GetViewport();
			float num;
			if (viewport == null)
			{
				num = 1920f;
			}
			else
			{
				Rect2 visibleRect = viewport.GetVisibleRect();
				num = ((Rect2)(ref visibleRect)).Size.X;
			}
			float num2 = num;
			Vector2 globalPosition = default(Vector2);
			((Vector2)(ref globalPosition))._002Ector(homePos.X - num2 * 1.2f, homePos.Y);
			((Control)node).GlobalPosition = globalPosition;
		}
	}

	private static void AddOrUpdate<TKey, TValue>(this ConditionalWeakTable<TKey, TValue> table, TKey key, TValue value) where TKey : class where TValue : class
	{
		if (table.TryGetValue(key, out var _))
		{
			table.Remove(key);
		}
		table.Add(key, value);
	}
}
