using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MeleeAttack;

public static class UppercutEffect
{
	public const string UPPERCUT_CARD_ID = "UPPERCUT";

	public const float LIFT_DISTANCE = 300f;

	public const float UP_DURATION = 0.15f;

	public const float HOLD_DURATION = 0.4f;

	public const float PRE_LIFT_DELAY = 0f;

	public const float PLAYER_LIFT_DISTANCE = 250f;

	public const float PLAYER_LIFT_DURATION = 0.2f;

	public const float FALL_HOLD_MAX = 0.2f;

	public const float FALL_DURATION_MAX = 0.2f;

	private const string VFX_PATH = "res://scenes/上勾拳.tscn";

	private const float VFX_LIFETIME = 0.6f;

	private static PackedScene _vfxScene;

	public static string NormalizeTrigger(string cardId, string triggerName)
	{
		if (string.IsNullOrEmpty(cardId))
		{
			return triggerName;
		}
		if (!cardId.Equals("UPPERCUT", StringComparison.OrdinalIgnoreCase))
		{
			return triggerName;
		}
		if (!SettingsUI.IsUppercutEnabled())
		{
			return triggerName;
		}
		if (!string.Equals(triggerName, "heavyAttack", StringComparison.OrdinalIgnoreCase))
		{
			return triggerName;
		}
		return "Attack";
	}

	public static async Task DoLiftAsync(NCreature attackerNode, Node2D visualNode)
	{
		if (attackerNode != null && GodotObject.IsInstanceValid((GodotObject)(object)attackerNode) && visualNode != null && GodotObject.IsInstanceValid((GodotObject)(object)visualNode))
		{
			PlayUppercutVfx(attackerNode);
			if (GodotObject.IsInstanceValid((GodotObject)(object)attackerNode) && visualNode != null && GodotObject.IsInstanceValid((GodotObject)(object)visualNode))
			{
				float startY = visualNode.Position.Y;
				float peakY = startY - 250f;
				Tween upTween = ((Node)attackerNode).CreateTween();
				upTween.SetTrans((TransitionType)4);
				upTween.SetEase((EaseType)1);
				upTween.TweenProperty((GodotObject)(object)visualNode, NodePath.op_Implicit("position:y"), Variant.op_Implicit(peakY), 0.20000000298023224);
				await ((GodotObject)attackerNode).ToSignal((GodotObject)(object)upTween, SignalName.Finished);
			}
		}
	}

	public static async Task RunFallDuringDelayAsync(NCreature attackerNode, Node2D visualNode, float totalDelay)
	{
		if (attackerNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)attackerNode) || visualNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)visualNode) || totalDelay <= 0f)
		{
			return;
		}
		float holdInAir = Mathf.Min(0.2f, totalDelay);
		float remainingAfterHold = totalDelay - holdInAir;
		float fallDuration = Mathf.Min(0.2f, remainingAfterHold);
		if (holdInAir > 0f)
		{
			await WaitGameTimeAsync(holdInAir);
		}
		if (!(fallDuration <= 0f) && GodotObject.IsInstanceValid((GodotObject)(object)attackerNode) && visualNode != null && GodotObject.IsInstanceValid((GodotObject)(object)visualNode))
		{
			float targetY = visualNode.Position.Y + 250f;
			if (SimpleTeleportPatch._attackStates.TryGetValue(attackerNode, out var st) && st.VisualOriginalPosition.HasValue)
			{
				targetY = st.VisualOriginalPosition.Value.Y;
			}
			Tween downTween = ((Node)attackerNode).CreateTween();
			downTween.SetTrans((TransitionType)4);
			downTween.SetEase((EaseType)0);
			downTween.TweenProperty((GodotObject)(object)visualNode, NodePath.op_Implicit("position:y"), Variant.op_Implicit(targetY), (double)fallDuration);
			await ((GodotObject)attackerNode).ToSignal((GodotObject)(object)downTween, SignalName.Finished);
		}
	}

	private static async Task WaitGameTimeAsync(float seconds)
	{
		if (seconds <= 0f)
		{
			return;
		}
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree tree = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		if (tree == null)
		{
			await Task.Delay((int)(seconds * 1000f));
			return;
		}
		long startTicks = (long)Time.GetTicksMsec();
		long targetMs = (long)(seconds * 1000f);
		while (true)
		{
			double elapsedGame = (double)((long)Time.GetTicksMsec() - startTicks) * Math.Max(Engine.TimeScale, 0.0);
			if (elapsedGame >= (double)targetMs)
			{
				break;
			}
			SceneTreeTimer timer = tree.CreateTimer(0.05, true, false, false);
			await ((GodotObject)tree).ToSignal((GodotObject)(object)timer, SignalName.Timeout);
		}
	}

	private static void PlayUppercutVfx(NCreature creatureNode)
	{
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Expected O, but got Unknown
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
		{
			return;
		}
		if (_vfxScene == null)
		{
			_vfxScene = GD.Load<PackedScene>("res://scenes/上勾拳.tscn");
			if (_vfxScene == null)
			{
				GD.PrintErr("[UppercutEffect] 无法加载特效: res://scenes/上勾拳.tscn");
				return;
			}
		}
		Node2D vfx = _vfxScene.Instantiate<Node2D>((GenEditState)0);
		if (vfx == null)
		{
			return;
		}
		((Node)creatureNode).AddChild((Node)(object)vfx, false, (InternalMode)0);
		Node2D body = creatureNode.Body;
		if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
		{
			vfx.GlobalPosition = body.GlobalPosition;
			float num = ((body.Scale.X < 0f) ? (-1f) : 1f);
			float num2 = Mathf.Abs(vfx.Scale.X);
			float y = vfx.Scale.Y;
			vfx.Scale = new Vector2(num2 * num, y);
		}
		else
		{
			vfx.GlobalPosition = ((Control)creatureNode).GlobalPosition;
		}
		((CanvasItem)vfx).ZIndex = 1;
		Timer val = new Timer();
		val.WaitTime = 0.6000000238418579;
		val.OneShot = true;
		val.Timeout += delegate
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)vfx))
			{
				((Node)vfx).QueueFree();
			}
		};
		((Node)vfx).AddChild((Node)(object)val, false, (InternalMode)0);
		val.Start(-1.0);
	}
}
