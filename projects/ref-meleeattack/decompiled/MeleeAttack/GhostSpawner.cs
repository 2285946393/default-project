using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Godot;
using Godot.Collections;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class GhostSpawner
{
	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class NCombatRoomReadyPatch
	{
		public static void Postfix()
		{
			ClearSharedWrappers();
		}
	}

	private const float FADE_IN_DURATION = 0.08f;

	private const int HARD_LIMIT = 12;

	private const long ACTIVE_SPAWN_WINDOW_MS = 300L;

	private static MethodInfo _setAnim3;

	private static MethodInfo _setAnim2;

	private static readonly ConcurrentDictionary<NCreature, AlphaSyncNode> _sharedWrappers = new ConcurrentDictionary<NCreature, AlphaSyncNode>();

	private static long _lastActiveSpawnMs = 0L;

	public static bool IsPassiveBlocked()
	{
		long num = Interlocked.Read(ref _lastActiveSpawnMs);
		if (num == 0)
		{
			return false;
		}
		long ticksMsec = (long)Time.GetTicksMsec();
		return ticksMsec - num < 300;
	}

	private static void MarkActiveSpawn()
	{
		Interlocked.Exchange(ref _lastActiveSpawnMs, (long)Time.GetTicksMsec());
	}

	public static void ClearAllGhostsFor(NCreature attackerNode)
	{
		if (attackerNode == null || !_sharedWrappers.TryGetValue(attackerNode, out var value) || value == null || !GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			return;
		}
		Array<Node> children = ((Node)value).GetChildren(false);
		foreach (Node item in children)
		{
			Node2D val = (Node2D)(object)((item is Node2D) ? item : null);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				((Node)val).QueueFree();
			}
		}
	}

	public static void SpawnGhostFadeOnlyUnlimited(NCreature attackerNode, float alpha, float fadeDuration, Color? color = null)
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		if (attackerNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)attackerNode))
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		Control val = (Control)(((object)instance.BackCombatVfxContainer) ?? ((object)instance));
		if (val == null)
		{
			return;
		}
		Node2D body = attackerNode.Body;
		if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
		{
			Color val2 = (Color)(((_003F?)color) ?? new Color(0.75f, 0.85f, 1f, alpha));
			Node2D val3 = CreateGhostNode(attackerNode, body, val2);
			if (val3 != null)
			{
				((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
				val3.GlobalTransform = body.GlobalTransform;
				Tween val4 = ((Node)val3).CreateTween();
				val4.TweenProperty((GodotObject)(object)val3, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(val2.A), 0.07999999821186066).SetTrans((TransitionType)0);
				val4.TweenProperty((GodotObject)(object)val3, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), (double)fadeDuration).SetTrans((TransitionType)0);
				val4.TweenCallback(Callable.From((Action)((Node)val3).QueueFree));
			}
		}
	}

	public static int GetActiveGhostCount(NCreature attackerNode)
	{
		if (attackerNode == null)
		{
			return 0;
		}
		if (!_sharedWrappers.TryGetValue(attackerNode, out var value))
		{
			return 0;
		}
		if (value == null || !GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			return 0;
		}
		int num = 0;
		foreach (Node child in ((Node)value).GetChildren(false))
		{
			Node2D val = (Node2D)(object)((child is Node2D) ? child : null);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				num++;
			}
		}
		return num;
	}

	public static bool ShouldSkipSpawn(NCreature attackerNode)
	{
		if (attackerNode == null)
		{
			return false;
		}
		return GetActiveGhostCount(attackerNode) >= 12;
	}

	public static void SpawnGhostSimple(NCreature attackerNode, float alpha, float fadeDuration, float moveDistance, float moveDuration, Color? color = null, bool isPassive = false)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		if (!isPassive)
		{
			MarkActiveSpawn();
		}
		if (ValidateNodes(attackerNode, out var container, out var body))
		{
			Color val = (Color)(((_003F?)color) ?? new Color(0.75f, 0.85f, 1f, alpha));
			Node2D val2 = CreateGhostNode(attackerNode, body, val);
			if (val2 != null)
			{
				MountGhost(attackerNode, val2, container);
				val2.GlobalTransform = body.GlobalTransform;
				Vector2 facingDirection = GetFacingDirection(body);
				Vector2 val3 = default(Vector2);
				((Vector2)(ref val3))._002Ector((0f - facingDirection.X) * moveDistance, 0f);
				Vector2 globalPosition = val2.GlobalPosition;
				Tween val4 = ((Node)val2).CreateTween();
				val4.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("global_position"), Variant.op_Implicit(globalPosition + val3), (double)moveDuration).SetTrans((TransitionType)7).SetEase((EaseType)1);
				Tween val5 = ((Node)val2).CreateTween();
				val5.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(val.A), 0.07999999821186066).SetTrans((TransitionType)0);
				val5.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), (double)fadeDuration).SetTrans((TransitionType)0);
				val5.TweenCallback(Callable.From((Action)((Node)val2).QueueFree));
			}
		}
	}

	public static void SpawnGhostWithReturn(NCreature attackerNode, float alpha, float fadeDuration, float moveDistance, float moveDuration, float returnDuration, Color? color = null, bool isPassive = false)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		if (!isPassive)
		{
			MarkActiveSpawn();
		}
		if (ValidateNodes(attackerNode, out var container, out var body))
		{
			Color val = (Color)(((_003F?)color) ?? new Color(0.75f, 0.85f, 1f, alpha));
			Node2D val2 = CreateGhostNode(attackerNode, body, val);
			if (val2 != null)
			{
				MountGhost(attackerNode, val2, container);
				val2.GlobalTransform = body.GlobalTransform;
				Vector2 facingDirection = GetFacingDirection(body);
				Vector2 val3 = default(Vector2);
				((Vector2)(ref val3))._002Ector((0f - facingDirection.X) * moveDistance, 0f);
				Vector2 globalPosition = val2.GlobalPosition;
				Tween val4 = ((Node)val2).CreateTween();
				val4.SetParallel(false);
				val4.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("global_position"), Variant.op_Implicit(globalPosition + val3), (double)moveDuration).SetTrans((TransitionType)4).SetEase((EaseType)1);
				val4.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("global_position"), Variant.op_Implicit(globalPosition), (double)returnDuration).SetTrans((TransitionType)4).SetEase((EaseType)0);
				Tween val5 = ((Node)val2).CreateTween();
				val5.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(val.A), 0.07999999821186066).SetTrans((TransitionType)0);
				val5.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), (double)fadeDuration).SetTrans((TransitionType)0);
				val5.TweenCallback(Callable.From((Action)((Node)val2).QueueFree));
			}
		}
	}

	public static void SpawnGhostWithScale(NCreature attackerNode, float alpha, float fadeDuration, float moveDistance, float moveDuration, float scaleMultiplier, Color? color = null, bool isPassive = false)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		if (!isPassive)
		{
			MarkActiveSpawn();
		}
		if (ValidateNodes(attackerNode, out var container, out var body))
		{
			Color val = (Color)(((_003F?)color) ?? new Color(0.75f, 0.85f, 1f, alpha));
			Node2D val2 = CreateGhostNode(attackerNode, body, val);
			if (val2 != null)
			{
				MountGhost(attackerNode, val2, container);
				val2.GlobalTransform = body.GlobalTransform;
				Vector2 facingDirection = GetFacingDirection(body);
				Vector2 val3 = default(Vector2);
				((Vector2)(ref val3))._002Ector((0f - facingDirection.X) * moveDistance, 0f);
				Vector2 globalPosition = val2.GlobalPosition;
				Vector2 scale = val2.Scale;
				Vector2 val4 = scale * scaleMultiplier;
				Tween val5 = ((Node)val2).CreateTween();
				val5.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("global_position"), Variant.op_Implicit(globalPosition + val3), (double)moveDuration).SetTrans((TransitionType)7).SetEase((EaseType)1);
				Tween val6 = ((Node)val2).CreateTween();
				val6.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("scale"), Variant.op_Implicit(val4), (double)fadeDuration).SetTrans((TransitionType)7).SetEase((EaseType)1);
				Tween val7 = ((Node)val2).CreateTween();
				val7.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(val.A), 0.07999999821186066).SetTrans((TransitionType)0);
				val7.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), (double)fadeDuration).SetTrans((TransitionType)0);
				val7.TweenCallback(Callable.From((Action)((Node)val2).QueueFree));
			}
		}
	}

	public static void SpawnGhostFadeOnly(NCreature attackerNode, float alpha, float fadeDuration, Color? color = null, bool isPassive = false)
	{
		SpawnGhostSimple(attackerNode, alpha, fadeDuration, 0f, 0f, color, isPassive);
	}

	private static bool ValidateNodes(NCreature attackerNode, out Node container, out Node2D body)
	{
		container = null;
		body = null;
		if (attackerNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)attackerNode))
		{
			return false;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return false;
		}
		container = (Node)(((object)instance.BackCombatVfxContainer) ?? ((object)instance));
		if (container == null)
		{
			return false;
		}
		body = attackerNode.Body;
		if (body == null || !GodotObject.IsInstanceValid((GodotObject)(object)body))
		{
			return false;
		}
		return true;
	}

	private static void MountGhost(NCreature attackerNode, Node2D ghost, Node container)
	{
		AlphaSyncNode orCreateSharedWrapper = GetOrCreateSharedWrapper(attackerNode, container);
		((Node)orCreateSharedWrapper).AddChild((Node)(object)ghost, false, (InternalMode)0);
	}

	private static AlphaSyncNode GetOrCreateSharedWrapper(NCreature attackerNode, Node container)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		if (_sharedWrappers.TryGetValue(attackerNode, out var value))
		{
			if (value != null && GodotObject.IsInstanceValid((GodotObject)(object)value))
			{
				return value;
			}
			_sharedWrappers.TryRemove(attackerNode, out var _);
		}
		AlphaSyncNode alphaSyncNode = new AlphaSyncNode
		{
			Target = (CanvasItem)(object)attackerNode
		};
		((CanvasItem)alphaSyncNode).Modulate = new Color(1f, 1f, 1f, ((CanvasItem)attackerNode).Modulate.A);
		container.AddChild((Node)(object)alphaSyncNode, false, (InternalMode)0);
		_sharedWrappers[attackerNode] = alphaSyncNode;
		return alphaSyncNode;
	}

	public static void ClearSharedWrappers()
	{
		foreach (KeyValuePair<NCreature, AlphaSyncNode> sharedWrapper in _sharedWrappers)
		{
			if (sharedWrapper.Value != null && GodotObject.IsInstanceValid((GodotObject)(object)sharedWrapper.Value))
			{
				((Node)sharedWrapper.Value).QueueFree();
			}
		}
		_sharedWrappers.Clear();
		Interlocked.Exchange(ref _lastActiveSpawnMs, 0L);
	}

	private static Node2D CreateGhostNode(NCreature attackerNode, Node2D body, Color modulate)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		Node val = ((Node)body).Duplicate(15);
		Node2D val2 = (Node2D)(object)((val is Node2D) ? val : null);
		if (val2 == null)
		{
			return null;
		}
		DisableAllProcessing((Node)(object)val2);
		((CanvasItem)val2).Modulate = new Color(modulate.R, modulate.G, modulate.B, 0f);
		SyncSpineAnimation(attackerNode, val2);
		return val2;
	}

	private static void DisableAllProcessing(Node node)
	{
		if (node == null)
		{
			return;
		}
		node.SetProcess(false);
		node.SetPhysicsProcess(false);
		node.SetProcessInput(false);
		node.SetProcessUnhandledInput(false);
		node.SetProcessUnhandledKeyInput(false);
		foreach (Node child in node.GetChildren(false))
		{
			DisableAllProcessing(child);
		}
	}

	private static void SyncSpineAnimation(NCreature source, Node2D ghost)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		SpineAnimationAccess spineAnimation = source.SpineAnimation;
		MegaTrackEntry currentTrack = ((SpineAnimationAccess)(ref spineAnimation)).GetCurrentTrack(0);
		string liveAnim = ((currentTrack != null) ? currentTrack.GetAnimationName() : null);
		float liveTime = ((currentTrack != null) ? currentTrack.GetTrackTime() : 0f);
		if (string.IsNullOrEmpty(liveAnim))
		{
			return;
		}
		SpineNodeExtensions.RunWhenSpineReady((Node)(object)ghost, new MegaSprite(Variant.op_Implicit((GodotObject)(object)ghost)), (Action<MegaAnimationState>)delegate(MegaAnimationState state)
		{
			Type type = ((object)state).GetType();
			if ((object)_setAnim3 == null)
			{
				_setAnim3 = type.GetMethod("SetAnimation", new Type[3]
				{
					typeof(string),
					typeof(bool),
					typeof(int)
				});
			}
			if ((object)_setAnim2 == null)
			{
				_setAnim2 = type.GetMethod("SetAnimation", new Type[2]
				{
					typeof(string),
					typeof(bool)
				});
			}
			if (_setAnim3 != null)
			{
				_setAnim3.Invoke(state, new object[3] { liveAnim, false, 0 });
			}
			else
			{
				_setAnim2?.Invoke(state, new object[2] { liveAnim, false });
			}
			MegaTrackEntry current = state.GetCurrent(0);
			if (current != null)
			{
				current.SetTrackTime(liveTime);
				((MegaSpineBinding)current).Dispose();
			}
			state.SetTimeScale(0f);
		});
	}

	private static Vector2 GetFacingDirection(Node2D body)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		Transform2D globalTransform = body.GlobalTransform;
		Vector2 val = ((Vector2)(ref globalTransform.X)).Normalized();
		float num = Math.Sign(val.X);
		if (num == 0f)
		{
			num = 1f;
		}
		return new Vector2(num, 0f);
	}
}
