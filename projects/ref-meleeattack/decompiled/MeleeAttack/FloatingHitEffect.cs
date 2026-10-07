using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class FloatingHitEffect
{
	public enum BodyHitKind
	{
		Float,
		Knockback,
		BodySlam,
		Orbit,
		Uppercut
	}

	private enum BodyHitPhase
	{
		Idle,
		Displacing,
		WaitingDelay,
		Returning
	}

	private enum UppercutSubPhase
	{
		None,
		Lifting,
		Holding
	}

	private class BodyHitState
	{
		public NCreature Node;

		public Node2D Visual;

		public Vector2 OriginalGlobalPos;

		public float OriginalRot;

		public Vector2 OriginalScale;

		public int OriginalZIndex;

		public int OriginalNodeZIndex;

		public BodyHitPhase Phase;

		public Queue<BodyHitKind> PendingKinds = new Queue<BodyHitKind>();

		public BodyHitKind LastKind = BodyHitKind.Float;

		public bool Unstoppable = false;

		public volatile bool Aborted = false;

		public Vector2 KnockDirection;

		public Vector2 SlamDirection;

		public float OrbitStartAngle;

		public float OrbitAngleOffset;

		public CancellationTokenSource OrbitCts;

		public TaskCompletionSource<bool> DisplacementDoneSignal;

		public float BaseDelay = 0.4f;

		public float ExtraDelay = 0f;

		public bool DelayInterrupted;

		public UppercutSubPhase UppercutPhase = UppercutSubPhase.None;

		public volatile bool HoldResetFlag = false;
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class KnockbackPatch
	{
		private static void Postfix(object __instance, object choiceContext, object runState, object combatState, Creature target, DamageResult result, object props, Creature dealer, CardModel cardSource)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Invalid comparison between Unknown and I4
			if (target != null && (int)target.Side == 2 && target.IsAlive && IsShivCard(cardSource) && SimpleTeleportPatch.TryGetCreatureNode(target, out var _, out var node) && node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
			{
				TriggerBodyHit(node, BodyHitKind.Knockback);
			}
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class BodySlamFlyPatch
	{
		private static void Postfix(object __instance, object choiceContext, object runState, object combatState, Creature target, DamageResult result, object props, Creature dealer, CardModel cardSource)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Invalid comparison between Unknown and I4
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Invalid comparison between Unknown and I4
			if (dealer != null && (int)dealer.Side == 1 && target != null && (int)target.Side == 2 && cardSource != null && ((AbstractModel)cardSource).Id.Entry.Equals("BODY_SLAM", StringComparison.OrdinalIgnoreCase) && SettingsUI.IsBodySlamEnabled() && SimpleTeleportPatch.TryGetCreatureNode(target, out var _, out var node) && node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
			{
				TriggerBodyHit(node, BodyHitKind.BodySlam);
			}
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class UppercutFlyPatch
	{
		private static void Postfix(object __instance, object choiceContext, object runState, object combatState, Creature target, DamageResult result, object props, Creature dealer, CardModel cardSource)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Invalid comparison between Unknown and I4
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Invalid comparison between Unknown and I4
			if (dealer != null && (int)dealer.Side == 1 && target != null && (int)target.Side == 2 && cardSource != null && ((AbstractModel)cardSource).Id.Entry.Equals("UPPERCUT", StringComparison.OrdinalIgnoreCase) && SettingsUI.IsUppercutEnabled())
			{
				string creatureId = SimpleTeleportPatch.GetCreatureId(dealer);
				if (SettingsUI.IsTeleportEnabledForCharacter(creatureId) && SimpleTeleportPatch.TryGetCreatureNode(target, out var _, out var node) && node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
				{
					TriggerBodyHit(node, BodyHitKind.Uppercut);
				}
			}
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class UppercutDelayResetPatch
	{
		private static void Postfix(object __instance, object choiceContext, object runState, object combatState, Creature target, DamageResult result, object props, Creature dealer, CardModel cardSource)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Invalid comparison between Unknown and I4
			if (target != null && (int)target.Side == 2 && SimpleTeleportPatch.TryGetCreatureNode(target, out var _, out var node) && node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
			{
				ResetUppercutDelay(node);
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class CombatResetPatch
	{
		private static void Postfix()
		{
			ClearAll();
		}
	}

	private const float KNOCKBACK_DISTANCE = 10f;

	private const float KNOCKBACK_MOVE_DURATION = 0.05f;

	private const float KNOCKBACK_HOLD_DURATION = 0.2f;

	private const float FLY_DISTANCE = 1500f;

	private const float FLY_DURATION = 0.5f;

	private const float FLY_ROTATION_SPEED = 5f;

	private const float FLY_ARC_HEIGHT_RATIO = 0.3f;

	private const float ORBIT_RADIUS_X = 200f;

	private const float ORBIT_RADIUS_Y = 50f;

	private const float ORBIT_PERIOD = 0.5f;

	private const float BASE_RETURN_DELAY = 0.4f;

	private const float HIT_DELAY_INCREMENT = 0.2f;

	private const float MAX_RETURN_DELAY = 3f;

	private const float RETURN_FADE_DURATION = 0.2f;

	private const float DELAY_TICK = 0.05f;

	private const float FLOAT_HOLD_DURATION = 0.55f;

	private static readonly Dictionary<NCreature, BodyHitState> _bodyHitStates = new Dictionary<NCreature, BodyHitState>();

	private static readonly object _bodyHitLock = new object();

	public static void TriggerBodyHit(NCreature node, BodyHitKind kind)
	{
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		bool flag = false;
		BodyHitState value;
		lock (_bodyHitLock)
		{
			if (_bodyHitStates.TryGetValue(node, out value))
			{
				if (!value.Unstoppable && value.Phase != BodyHitPhase.Returning)
				{
					switch (kind)
					{
					case BodyHitKind.Knockback:
						value.KnockDirection = CalculateDirectionFromPlayer(node);
						break;
					case BodyHitKind.BodySlam:
						value.SlamDirection = CalculateDirectionFromPlayer(node);
						value.Unstoppable = true;
						break;
					}
					value.PendingKinds.Enqueue(kind);
					value.ExtraDelay += 0.2f;
					if (value.ExtraDelay > 3f)
					{
						value.ExtraDelay = 3f;
					}
					if (value.Phase == BodyHitPhase.WaitingDelay)
					{
						value.DelayInterrupted = true;
					}
				}
				return;
			}
			Node2D body = node.Body;
			if (body == null || !GodotObject.IsInstanceValid((GodotObject)(object)body))
			{
				return;
			}
			value = new BodyHitState
			{
				Node = node,
				Visual = body,
				OriginalGlobalPos = body.GlobalPosition,
				OriginalRot = body.Rotation,
				OriginalScale = body.Scale,
				OriginalZIndex = ((CanvasItem)body).ZIndex,
				OriginalNodeZIndex = ((CanvasItem)node).ZIndex,
				Phase = BodyHitPhase.Idle,
				LastKind = kind,
				Unstoppable = (kind == BodyHitKind.BodySlam)
			};
			switch (kind)
			{
			case BodyHitKind.Knockback:
				value.KnockDirection = CalculateDirectionFromPlayer(node);
				break;
			case BodyHitKind.BodySlam:
				value.SlamDirection = CalculateDirectionFromPlayer(node);
				break;
			}
			value.PendingKinds.Enqueue(kind);
			_bodyHitStates[node] = value;
			flag = true;
		}
		if (flag)
		{
			RunBodyHitSequence(value);
		}
	}

	public static void ResetUppercutDelay(NCreature node)
	{
		if (node == null)
		{
			return;
		}
		lock (_bodyHitLock)
		{
			if (_bodyHitStates.TryGetValue(node, out var value) && value.LastKind == BodyHitKind.Uppercut && value.UppercutPhase == UppercutSubPhase.Holding)
			{
				value.HoldResetFlag = true;
			}
		}
	}

	private static async Task RunBodyHitSequence(BodyHitState state)
	{
		try
		{
			bool backToDisplace;
			do
			{
				if (state.Aborted)
				{
					return;
				}
				while (true)
				{
					BodyHitKind kind;
					lock (_bodyHitLock)
					{
						if (state.PendingKinds.Count == 0)
						{
							break;
						}
						kind = state.PendingKinds.Dequeue();
						state.Phase = BodyHitPhase.Displacing;
						goto IL_00d6;
					}
					IL_00d6:
					await PerformDisplacement(state, kind);
					if (state.Aborted)
					{
						return;
					}
					lock (_bodyHitLock)
					{
						if (state.PendingKinds.Count == 0)
						{
							break;
						}
					}
				}
				bool skipDelay;
				lock (_bodyHitLock)
				{
					skipDelay = state.LastKind == BodyHitKind.Orbit && state.PendingKinds.Count == 0;
				}
				if (skipDelay)
				{
					break;
				}
				lock (_bodyHitLock)
				{
					state.Phase = BodyHitPhase.WaitingDelay;
					state.DelayInterrupted = false;
				}
				float elapsed = 0f;
				backToDisplace = false;
				while (true)
				{
					if (state.Aborted || state.Node == null || !GodotObject.IsInstanceValid((GodotObject)(object)state.Node))
					{
						return;
					}
					lock (_bodyHitLock)
					{
						if (!state.Unstoppable && (state.DelayInterrupted || state.PendingKinds.Count > 0))
						{
							state.DelayInterrupted = false;
							backToDisplace = true;
							break;
						}
						float total = state.BaseDelay + state.ExtraDelay;
						if (total > 3f)
						{
							total = 3f;
						}
						if (elapsed >= total)
						{
							break;
						}
					}
					await ScaledDelayAsync(0.05f);
					elapsed += 0.05f;
				}
			}
			while (backToDisplace);
			lock (_bodyHitLock)
			{
				state.Phase = BodyHitPhase.Returning;
			}
			await PerformFlashReturn(state);
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			GD.PrintErr("[BodyHit] 流程异常: " + ex.Message + "\n" + ex.StackTrace);
		}
		finally
		{
			lock (_bodyHitLock)
			{
				_bodyHitStates.Remove(state.Node);
			}
			TryRestoreVisualSafety(state);
		}
	}

	private static async Task ScaledDelayAsync(float seconds)
	{
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree tree = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		if (tree == null)
		{
			await Task.Delay((int)(seconds * 1000f));
			return;
		}
		ulong startTicks = Time.GetTicksMsec();
		long targetMs = (long)(seconds * 1000f);
		double elapsedGame;
		do
		{
			SceneTreeTimer timer = tree.CreateTimer(0.05, true, false, false);
			await ((GodotObject)tree).ToSignal((GodotObject)(object)timer, SignalName.Timeout);
			elapsedGame = (double)(long)(Time.GetTicksMsec() - startTicks) * Math.Max(Engine.TimeScale, 0.0);
		}
		while (!(elapsedGame >= (double)targetMs));
	}

	private static async Task PerformDisplacement(BodyHitState state, BodyHitKind kind)
	{
		lock (_bodyHitLock)
		{
			state.LastKind = kind;
		}
		switch (kind)
		{
		case BodyHitKind.Float:
			await PerformFloatDisplacement(state);
			break;
		case BodyHitKind.Knockback:
			await PerformKnockbackDisplacement(state);
			break;
		case BodyHitKind.BodySlam:
			await PerformBodySlamDisplacement(state);
			break;
		case BodyHitKind.Orbit:
			await PerformOrbitDisplacement(state);
			break;
		case BodyHitKind.Uppercut:
			await PerformUppercutDisplacement(state);
			break;
		}
	}

	private static async Task PerformFloatDisplacement(BodyHitState state)
	{
		if (state.Visual != null && GodotObject.IsInstanceValid((GodotObject)(object)state.Visual))
		{
			await ScaledDelayAsync(0.55f);
		}
	}

	private static async Task PerformUppercutDisplacement(BodyHitState state)
	{
		if (state.Visual == null || !GodotObject.IsInstanceValid((GodotObject)(object)state.Visual) || state.Node == null || !GodotObject.IsInstanceValid((GodotObject)(object)state.Node))
		{
			return;
		}
		float startY = state.Visual.Position.Y;
		float peakY = startY - 300f;
		lock (_bodyHitLock)
		{
			state.UppercutPhase = UppercutSubPhase.Lifting;
		}
		Tween upTween = ((Node)state.Node).CreateTween();
		upTween.SetTrans((TransitionType)4);
		upTween.SetEase((EaseType)1);
		upTween.TweenProperty((GodotObject)(object)state.Visual, NodePath.op_Implicit("position:y"), Variant.op_Implicit(peakY), 0.15000000596046448);
		await ((GodotObject)state.Node).ToSignal((GodotObject)(object)upTween, SignalName.Finished);
		if (state.Aborted)
		{
			lock (_bodyHitLock)
			{
				state.UppercutPhase = UppercutSubPhase.None;
			}
			return;
		}
		if (state.Visual == null || !GodotObject.IsInstanceValid((GodotObject)(object)state.Visual))
		{
			lock (_bodyHitLock)
			{
				state.UppercutPhase = UppercutSubPhase.None;
			}
			return;
		}
		if (state.Node == null || !GodotObject.IsInstanceValid((GodotObject)(object)state.Node))
		{
			lock (_bodyHitLock)
			{
				state.UppercutPhase = UppercutSubPhase.None;
			}
			return;
		}
		lock (_bodyHitLock)
		{
			state.UppercutPhase = UppercutSubPhase.Holding;
			state.HoldResetFlag = false;
		}
		long holdStartTicks = (long)Time.GetTicksMsec();
		while (true)
		{
			if (state.Aborted)
			{
				lock (_bodyHitLock)
				{
					state.UppercutPhase = UppercutSubPhase.None;
				}
				return;
			}
			if (state.Visual == null || !GodotObject.IsInstanceValid((GodotObject)(object)state.Visual))
			{
				lock (_bodyHitLock)
				{
					state.UppercutPhase = UppercutSubPhase.None;
				}
				return;
			}
			if (state.Node == null || !GodotObject.IsInstanceValid((GodotObject)(object)state.Node))
			{
				lock (_bodyHitLock)
				{
					state.UppercutPhase = UppercutSubPhase.None;
				}
				return;
			}
			if (state.HoldResetFlag)
			{
				state.HoldResetFlag = false;
				holdStartTicks = (long)Time.GetTicksMsec();
			}
			double elapsedGame = (double)((long)Time.GetTicksMsec() - holdStartTicks) * Math.Max(Engine.TimeScale, 0.0);
			if (elapsedGame >= 400.0000059604645)
			{
				break;
			}
			await ScaledDelayAsync(0.05f);
		}
		lock (_bodyHitLock)
		{
			state.UppercutPhase = UppercutSubPhase.None;
			state.BaseDelay = 0f;
			state.ExtraDelay = 0f;
		}
	}

	private static async Task PerformKnockbackDisplacement(BodyHitState state)
	{
		if (state.Visual != null && GodotObject.IsInstanceValid((GodotObject)(object)state.Visual) && state.Node != null && GodotObject.IsInstanceValid((GodotObject)(object)state.Node))
		{
			float currentX = state.Visual.Position.X;
			float targetX = currentX + state.KnockDirection.X * 10f;
			Tween tween = ((Node)state.Node).CreateTween();
			tween.TweenProperty((GodotObject)(object)state.Visual, NodePath.op_Implicit("position:x"), Variant.op_Implicit(targetX), 0.05000000074505806).SetEase((EaseType)1);
			await ((GodotObject)state.Node).ToSignal((GodotObject)(object)tween, SignalName.Finished);
			await ScaledDelayAsync(0.2f);
		}
	}

	private static async Task PerformBodySlamDisplacement(BodyHitState state)
	{
		if (state.Visual == null || !GodotObject.IsInstanceValid((GodotObject)(object)state.Visual) || state.Node == null || !GodotObject.IsInstanceValid((GodotObject)(object)state.Node))
		{
			return;
		}
		Vector2 dir = state.SlamDirection;
		if (((Vector2)(ref dir)).LengthSquared() < 0.001f)
		{
			dir = Vector2.Right;
		}
		Vector2 startVisualPos = state.Visual.Position;
		Vector2 endVisualPos = startVisualPos + dir * 1500f;
		float peakHeight = 450.00003f;
		CardSmogEffect.HideMirrorEffect(state.Node);
		Xue.MakeHealthBarTransparent(state.Node);
		try
		{
			Tween flyTween = ((Node)state.Node).CreateTween();
			float totalRotation = (float)Math.PI * 5f;
			flyTween.TweenMethod(Callable.From<float>((Action<float>)delegate(float progress)
			{
				//IL_0059: Unknown result type (might be due to invalid IL or missing references)
				//IL_005f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0064: Unknown result type (might be due to invalid IL or missing references)
				//IL_0091: Unknown result type (might be due to invalid IL or missing references)
				//IL_0097: Unknown result type (might be due to invalid IL or missing references)
				//IL_009f: Unknown result type (might be due to invalid IL or missing references)
				if (state.Visual != null && GodotObject.IsInstanceValid((GodotObject)(object)state.Visual))
				{
					float easedProgress = SimpleTeleportPatch.GetEasedProgress(progress);
					Vector2 val = ((Vector2)(ref startVisualPos)).Lerp(endVisualPos, easedProgress);
					float num = -4f * peakHeight * progress * (1f - progress);
					state.Visual.Position = new Vector2(val.X, val.Y + num);
					state.Visual.Rotation = totalRotation * progress;
				}
			}), Variant.op_Implicit(0f), Variant.op_Implicit(1f), 0.5).SetTrans((TransitionType)0);
			await ((GodotObject)state.Node).ToSignal((GodotObject)(object)flyTween, SignalName.Finished);
		}
		finally
		{
			if (state.Node != null && GodotObject.IsInstanceValid((GodotObject)(object)state.Node))
			{
				if (state.Visual != null && GodotObject.IsInstanceValid((GodotObject)(object)state.Visual))
				{
					state.Visual.GlobalPosition = state.OriginalGlobalPos;
					state.Visual.Rotation = state.OriginalRot;
				}
				Xue.FadeInHealthBar(state.Node);
				CardSmogEffect.ShowMirrorEffect(state.Node);
			}
		}
	}

	private static async Task PerformOrbitDisplacement(BodyHitState state)
	{
		if (state.Visual != null && GodotObject.IsInstanceValid((GodotObject)(object)state.Visual) && state.Node != null && GodotObject.IsInstanceValid((GodotObject)(object)state.Node))
		{
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
			lock (_bodyHitLock)
			{
				state.DisplacementDoneSignal = tcs;
			}
			state.OrbitCts = new CancellationTokenSource();
			OrbitLoopAsync(state, state.OrbitCts.Token);
			await tcs.Task;
			lock (_bodyHitLock)
			{
				state.DisplacementDoneSignal = null;
			}
		}
	}

	private static async Task OrbitLoopAsync(BodyHitState state, CancellationToken token)
	{
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree tree = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		if (tree == null)
		{
			return;
		}
		float angularSpeed = (float)Math.PI * 4f;
		float lastTime = (float)((double)Time.GetTicksUsec() / 1000000.0);
		NCreature playerNode = FindLocalPlayerNode();
		while (!token.IsCancellationRequested)
		{
			await ((GodotObject)tree).ToSignal((GodotObject)(object)tree, SignalName.ProcessFrame);
			if (token.IsCancellationRequested || state.Aborted || state.Node == null || !GodotObject.IsInstanceValid((GodotObject)(object)state.Node) || state.Visual == null || !GodotObject.IsInstanceValid((GodotObject)(object)state.Visual))
			{
				break;
			}
			if (playerNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)playerNode))
			{
				playerNode = FindLocalPlayerNode();
				if (playerNode == null)
				{
					break;
				}
			}
			float now = (float)((double)Time.GetTicksUsec() / 1000000.0);
			float dt = now - lastTime;
			lastTime = now;
			if (dt > 0.1f)
			{
				dt = 0.1f;
			}
			float scaledDt = dt * (float)Engine.TimeScale;
			state.OrbitAngleOffset -= angularSpeed * scaledDt;
			if (state.OrbitAngleOffset < (float)Math.PI * -2f)
			{
				state.OrbitAngleOffset += (float)Math.PI * 2f;
			}
			Vector2 center = ((Control)playerNode).GlobalPosition;
			float a = state.OrbitStartAngle + state.OrbitAngleOffset;
			float cosA = Mathf.Cos(a);
			float sinA = Mathf.Sin(a);
			float ox = cosA * 200f;
			float oy = sinA * 50f;
			state.Visual.GlobalPosition = new Vector2(center.X + ox, center.Y + oy);
			bool lowerHalf = sinA > 0f;
			float baseSign = ((state.OriginalScale.X >= 0f) ? 1f : (-1f));
			float absX = Mathf.Abs(state.Visual.Scale.X);
			if (absX < 0.001f)
			{
				absX = 1f;
			}
			float wantSign = (lowerHalf ? baseSign : (0f - baseSign));
			float curSign = ((state.Visual.Scale.X >= 0f) ? 1f : (-1f));
			if (curSign != wantSign)
			{
				state.Visual.Scale = new Vector2(absX * wantSign, state.Visual.Scale.Y);
			}
			int playerZ = ((CanvasItem)playerNode).ZIndex;
			int wantZ = (lowerHalf ? (playerZ + 1) : Math.Max(0, playerZ - 1));
			if (((CanvasItem)state.Node).ZIndex != wantZ)
			{
				((CanvasItem)state.Node).ZIndex = wantZ;
			}
		}
	}

	private static async Task PerformFlashReturn(BodyHitState state)
	{
		Node2D visual = state.Visual;
		if (visual == null || !GodotObject.IsInstanceValid((GodotObject)(object)visual))
		{
			return;
		}
		if (state.OrbitCts != null)
		{
			try
			{
				state.OrbitCts.Cancel();
			}
			catch
			{
			}
			try
			{
				state.OrbitCts.Dispose();
			}
			catch
			{
			}
			state.OrbitCts = null;
		}
		visual.GlobalPosition = state.OriginalGlobalPos;
		visual.Rotation = state.OriginalRot;
		visual.Scale = state.OriginalScale;
		((CanvasItem)visual).ZIndex = state.OriginalZIndex;
		if (state.Node != null && GodotObject.IsInstanceValid((GodotObject)(object)state.Node))
		{
			((CanvasItem)state.Node).ZIndex = state.OriginalNodeZIndex;
		}
		Color c = ((CanvasItem)visual).Modulate;
		((CanvasItem)visual).Modulate = new Color(c.R, c.G, c.B, 0f);
		Tween tween = ((Node)visual).CreateTween();
		tween.TweenProperty((GodotObject)(object)visual, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(c.A), 0.20000000298023224);
		await ((GodotObject)visual).ToSignal((GodotObject)(object)tween, SignalName.Finished);
		if (state.Node != null && GodotObject.IsInstanceValid((GodotObject)(object)state.Node))
		{
			Xue.FadeInHealthBar(state.Node);
			CardSmogEffect.ShowMirrorEffect(state.Node);
		}
	}

	private static void TryRestoreVisualSafety(BodyHitState state)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		Node2D visual = state.Visual;
		if (visual != null && GodotObject.IsInstanceValid((GodotObject)(object)visual))
		{
			visual.GlobalPosition = state.OriginalGlobalPos;
			visual.Rotation = state.OriginalRot;
			visual.Scale = state.OriginalScale;
			((CanvasItem)visual).ZIndex = state.OriginalZIndex;
		}
		if (state.Node != null && GodotObject.IsInstanceValid((GodotObject)(object)state.Node))
		{
			((CanvasItem)state.Node).ZIndex = state.OriginalNodeZIndex;
			Xue.FadeInHealthBar(state.Node);
			CardSmogEffect.ShowMirrorEffect(state.Node);
		}
	}

	public static void StartWhirlwindOrbit(NCreature playerNode)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Invalid comparison between Unknown and I4
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		if (playerNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)playerNode))
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		Vector2 center = ((Control)playerNode).GlobalPosition;
		List<NCreature> list = new List<NCreature>();
		foreach (NCreature creatureNode in instance.CreatureNodes)
		{
			if (creatureNode != null && creatureNode.Entity != null && (int)creatureNode.Entity.Side == 2 && creatureNode.Entity.IsAlive && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				Node2D body = creatureNode.Body;
				if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
				{
					list.Add(creatureNode);
				}
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		list.Sort(delegate(NCreature a, NCreature b)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			Vector2 val3 = a.Body.GlobalPosition - center;
			Vector2 val4 = b.Body.GlobalPosition - center;
			return Mathf.Atan2(val3.Y, val3.X).CompareTo(Mathf.Atan2(val4.Y, val4.X));
		});
		int count = list.Count;
		float num = (float)Math.PI * 2f / (float)count;
		Vector2 val = list[0].Body.GlobalPosition - center;
		float num2 = ((((Vector2)(ref val)).LengthSquared() > 1f) ? Mathf.Atan2(val.Y, val.X) : 0f);
		for (int i = 0; i < count; i++)
		{
			NCreature val2 = list[i];
			float orbitStartAngle = num2 + num * (float)i;
			bool flag = false;
			lock (_bodyHitLock)
			{
				if (_bodyHitStates.TryGetValue(val2, out var value) && value.Phase == BodyHitPhase.Displacing && value.DisplacementDoneSignal != null)
				{
					value.OrbitStartAngle = orbitStartAngle;
					flag = true;
				}
			}
			if (flag)
			{
				continue;
			}
			TriggerBodyHit(val2, BodyHitKind.Orbit);
			lock (_bodyHitLock)
			{
				if (_bodyHitStates.TryGetValue(val2, out var value2))
				{
					value2.OrbitStartAngle = orbitStartAngle;
					value2.OrbitAngleOffset = 0f;
				}
			}
		}
	}

	public static void StopWhirlwindOrbit(NCreature playerNode)
	{
		List<BodyHitState> list = new List<BodyHitState>();
		lock (_bodyHitLock)
		{
			foreach (KeyValuePair<NCreature, BodyHitState> bodyHitState in _bodyHitStates)
			{
				BodyHitState value = bodyHitState.Value;
				if (value.Phase == BodyHitPhase.Displacing && value.DisplacementDoneSignal != null)
				{
					list.Add(value);
				}
			}
		}
		foreach (BodyHitState item in list)
		{
			item.DisplacementDoneSignal?.TrySetResult(result: true);
		}
	}

	public static void OnWorldMirrored(float worldCenterX)
	{
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		lock (_bodyHitLock)
		{
			foreach (KeyValuePair<NCreature, BodyHitState> bodyHitState in _bodyHitStates)
			{
				BodyHitState value = bodyHitState.Value;
				NCreature node = value.Node;
				Node2D visual = value.Visual;
				if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node) && visual != null && GodotObject.IsInstanceValid((GodotObject)(object)visual) && (value.Phase == BodyHitPhase.Displacing || value.Phase == BodyHitPhase.WaitingDelay))
				{
					value.OriginalGlobalPos = new Vector2(2f * worldCenterX - value.OriginalGlobalPos.X, value.OriginalGlobalPos.Y);
					value.OriginalScale = new Vector2(0f - value.OriginalScale.X, value.OriginalScale.Y);
					if (value.Phase == BodyHitPhase.Displacing && value.LastKind == BodyHitKind.BodySlam)
					{
						value.SlamDirection = new Vector2(0f - value.SlamDirection.X, value.SlamDirection.Y);
					}
					if (value.Phase == BodyHitPhase.Displacing && value.LastKind == BodyHitKind.Knockback)
					{
						value.KnockDirection = new Vector2(0f - value.KnockDirection.X, value.KnockDirection.Y);
					}
				}
			}
		}
	}

	public static void OnWorldUnmirrored()
	{
	}

	private static Vector2 CalculateDirectionFromPlayer(NCreature node)
	{
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		NCreature val = FindLocalPlayerNode();
		if (val != null)
		{
			Node2D body = val.Body;
			if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
			{
				float num = ((body.Scale.X >= 0f) ? 1f : (-1f));
				return new Vector2(num, 0f);
			}
			Vector2 val2 = ((Control)node).GlobalPosition - ((Control)val).GlobalPosition;
			Vector2 val3 = ((Vector2)(ref val2)).Normalized();
			if (((Vector2)(ref val3)).LengthSquared() < 0.001f)
			{
				val3 = Vector2.Right;
			}
			val3.Y = 0f;
			if (((Vector2)(ref val3)).LengthSquared() < 0.001f)
			{
				val3 = Vector2.Right;
			}
			return ((Vector2)(ref val3)).Normalized();
		}
		return Vector2.Right;
	}

	private static NCreature FindLocalPlayerNode()
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Invalid comparison between Unknown and I4
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return null;
		}
		foreach (NCreature creatureNode in instance.CreatureNodes)
		{
			if (creatureNode != null)
			{
				Creature entity = creatureNode.Entity;
				if ((int)((entity != null) ? new CombatSide?(entity.Side) : null).GetValueOrDefault() == 1 && creatureNode.Entity.IsAlive)
				{
					return creatureNode;
				}
			}
		}
		return null;
	}

	private static bool IsShivCard(CardModel card)
	{
		if (card == null)
		{
			return false;
		}
		int result;
		if (!(card is Shiv))
		{
			ModelId id = ((AbstractModel)card).Id;
			result = (string.Equals((id != null) ? id.Entry : null, "SHIV", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
		}
		else
		{
			result = 1;
		}
		return (byte)result != 0;
	}

	public static void ClearAll()
	{
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		List<BodyHitState> list;
		lock (_bodyHitLock)
		{
			list = new List<BodyHitState>(_bodyHitStates.Values);
			foreach (BodyHitState item in list)
			{
				item.Aborted = true;
			}
			_bodyHitStates.Clear();
		}
		foreach (BodyHitState item2 in list)
		{
			Node2D visual = item2.Visual;
			if (visual != null && GodotObject.IsInstanceValid((GodotObject)(object)visual))
			{
				visual.GlobalPosition = item2.OriginalGlobalPos;
				visual.Rotation = item2.OriginalRot;
				visual.Scale = item2.OriginalScale;
				((CanvasItem)visual).ZIndex = item2.OriginalZIndex;
			}
			if (item2.Node != null && GodotObject.IsInstanceValid((GodotObject)(object)item2.Node))
			{
				((CanvasItem)item2.Node).ZIndex = item2.OriginalNodeZIndex;
			}
			try
			{
				item2.OrbitCts?.Cancel();
			}
			catch
			{
			}
			try
			{
				item2.OrbitCts?.Dispose();
			}
			catch
			{
			}
			try
			{
				item2.DisplacementDoneSignal?.TrySetResult(result: true);
			}
			catch
			{
			}
			if (item2.Node != null && GodotObject.IsInstanceValid((GodotObject)(object)item2.Node))
			{
				Xue.FadeInHealthBar(item2.Node);
				CardSmogEffect.ShowMirrorEffect(item2.Node);
			}
		}
	}
}
