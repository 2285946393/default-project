using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MeleeAttack;

public static class 染色
{
	private class ActiveTint
	{
		public Color Color;

		public long Generation;

		public Action OnExpire;
	}

	private const float ENTER_DURATION = 0.1f;

	private const float EXIT_DURATION = 0.2f;

	private static readonly Color WHITE_BLOWOUT = new Color(100f, 100f, 100f, 1f);

	private static readonly Dictionary<NCreature, ActiveTint> _activeTints = new Dictionary<NCreature, ActiveTint>();

	private static readonly Dictionary<NCreature, Tween> _activeTweens = new Dictionary<NCreature, Tween>();

	private static readonly Dictionary<NCreature, CancellationTokenSource> _expireCts = new Dictionary<NCreature, CancellationTokenSource>();

	private static readonly object _lock = new object();

	private static long _nextGeneration = 0L;

	public static void Apply(NCreature node, Color color, float duration = 1f, Action onExpire = null)
	{
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		Node2D body = node.Body;
		if (body == null || !GodotObject.IsInstanceValid((GodotObject)(object)body))
		{
			return;
		}
		Color val = default(Color);
		((Color)(ref val))._002Ector(color.R, color.G, color.B, ((CanvasItem)body).Modulate.A);
		long generation;
		CancellationToken token;
		lock (_lock)
		{
			generation = ++_nextGeneration;
			_activeTints[node] = new ActiveTint
			{
				Color = val,
				Generation = generation,
				OnExpire = onExpire
			};
			if (_expireCts.TryGetValue(node, out var value))
			{
				try
				{
					value.Cancel();
				}
				catch
				{
				}
				try
				{
					value.Dispose();
				}
				catch
				{
				}
			}
			CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
			_expireCts[node] = cancellationTokenSource;
			token = cancellationTokenSource.Token;
		}
		KillActiveTween(node);
		Tween val2 = ((Node)node).CreateTween();
		val2.SetTrans((TransitionType)0);
		val2.SetEase((EaseType)2);
		val2.TweenProperty((GodotObject)(object)body, NodePath.op_Implicit("modulate"), Variant.op_Implicit(val), 0.10000000149011612);
		_activeTweens[node] = val2;
		ExpireTintAsync(node, generation, duration, token);
	}

	public static void Whiten(NCreature node, float duration = 1f, Action onExpire = null)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		Apply(node, WHITE_BLOWOUT, duration, onExpire);
	}

	private static async Task WaitWithSceneTimerAsync(float seconds, CancellationToken token)
	{
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree tree = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		if (tree == null)
		{
			try
			{
				await Task.Delay((int)(seconds * 1000f), token);
				return;
			}
			catch (OperationCanceledException)
			{
				return;
			}
		}
		float step;
		for (float elapsed = 0f; elapsed < seconds; elapsed += step)
		{
			if (token.IsCancellationRequested)
			{
				break;
			}
			step = Mathf.Min(0.05f, seconds - elapsed);
			SceneTreeTimer timer = tree.CreateTimer((double)step, true, false, false);
			await ((GodotObject)tree).ToSignal((GodotObject)(object)timer, SignalName.Timeout);
		}
	}

	private static async Task ExpireTintAsync(NCreature node, long generation, float duration, CancellationToken token)
	{
		await WaitWithSceneTimerAsync(0.1f + duration, token);
		if (token.IsCancellationRequested)
		{
			return;
		}
		Action onExpire = null;
		lock (_lock)
		{
			if (!_activeTints.TryGetValue(node, out var active) || active.Generation != generation)
			{
				return;
			}
			onExpire = active.OnExpire;
			_activeTints.Remove(node);
			if (_expireCts.TryGetValue(node, out var cts))
			{
				_expireCts.Remove(node);
				try
				{
					cts.Dispose();
				}
				catch
				{
				}
			}
		}
		try
		{
			onExpire?.Invoke();
		}
		catch (Exception ex)
		{
			GD.PrintErr("[染色] onExpire 异常: " + ex.Message);
		}
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			Node2D body = node.Body;
			if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
			{
				KillActiveTween(node);
				Color fadeTarget = new Color(1f, 1f, 1f, ((CanvasItem)body).Modulate.A);
				Tween exitTween = ((Node)node).CreateTween();
				exitTween.SetTrans((TransitionType)0);
				exitTween.SetEase((EaseType)2);
				exitTween.TweenProperty((GodotObject)(object)body, NodePath.op_Implicit("modulate"), Variant.op_Implicit(fadeTarget), 0.20000000298023224);
				_activeTweens[node] = exitTween;
			}
		}
	}

	private static void KillActiveTween(NCreature node)
	{
		if (_activeTweens.TryGetValue(node, out var value))
		{
			if (value != null && GodotObject.IsInstanceValid((GodotObject)(object)value))
			{
				value.Kill();
			}
			_activeTweens.Remove(node);
		}
	}

	public static void Clear(NCreature node)
	{
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		if (node == null)
		{
			return;
		}
		lock (_lock)
		{
			_activeTints.Remove(node);
			if (_expireCts.TryGetValue(node, out var value))
			{
				_expireCts.Remove(node);
				try
				{
					value.Cancel();
				}
				catch
				{
				}
				try
				{
					value.Dispose();
				}
				catch
				{
				}
			}
		}
		KillActiveTween(node);
		if (GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			Node2D body = node.Body;
			if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
			{
				((CanvasItem)body).Modulate = new Color(1f, 1f, 1f, ((CanvasItem)body).Modulate.A);
			}
		}
	}

	public static void ClearAll()
	{
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		List<NCreature> list;
		lock (_lock)
		{
			list = new List<NCreature>(_activeTints.Keys);
			_activeTints.Clear();
			foreach (CancellationTokenSource value in _expireCts.Values)
			{
				try
				{
					value.Cancel();
				}
				catch
				{
				}
				try
				{
					value.Dispose();
				}
				catch
				{
				}
			}
			_expireCts.Clear();
		}
		List<NCreature> list2 = new List<NCreature>(_activeTweens.Keys);
		foreach (NCreature item in list2)
		{
			KillActiveTween(item);
		}
		foreach (NCreature item2 in list)
		{
			if (item2 != null && GodotObject.IsInstanceValid((GodotObject)(object)item2))
			{
				Node2D body = item2.Body;
				if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
				{
					((CanvasItem)body).Modulate = new Color(1f, 1f, 1f, ((CanvasItem)body).Modulate.A);
				}
			}
		}
	}

	public static bool HasActiveTint(NCreature node)
	{
		if (node == null)
		{
			return false;
		}
		lock (_lock)
		{
			return _activeTints.ContainsKey(node);
		}
	}
}
