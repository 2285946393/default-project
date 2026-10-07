using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class YiShanEffect
{
	private const string VFX_PATH = "res://scenes/一闪.tscn";

	private const float AUTO_FREE_SECONDS = 2f;

	private const int VFX_Z_INDEX = 100;

	private static PackedScene _scene;

	public static void PlayAt(NCreature attackerNode, Vector2 globalPosition)
	{
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Expected O, but got Unknown
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null || attackerNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)attackerNode))
		{
			return;
		}
		if (_scene == null)
		{
			_scene = GD.Load<PackedScene>("res://scenes/一闪.tscn");
			if (_scene == null)
			{
				GD.PrintErr("[YiShanEffect] 无法加载特效场景: res://scenes/一闪.tscn");
				return;
			}
		}
		Node2D node = _scene.Instantiate<Node2D>((GenEditState)0);
		if (node == null)
		{
			return;
		}
		float num = 1f;
		Node2D body = attackerNode.Body;
		if (body != null && GodotObject.IsInstanceValid((GodotObject)(object)body))
		{
			num = ((body.Scale.X < 0f) ? (-1f) : 1f);
		}
		float num2 = Mathf.Abs(node.Scale.X);
		float y = node.Scale.Y;
		node.Scale = new Vector2(num2 * num, y);
		node.GlobalPosition = globalPosition;
		((CanvasItem)node).ZIndex = 100;
		((CanvasItem)node).ZAsRelative = false;
		Node val = (Node)(((object)instance.CombatVfxContainer) ?? ((object)instance));
		val.AddChild((Node)(object)node, false, (InternalMode)0);
		Timer val2 = new Timer();
		val2.WaitTime = 2.0;
		val2.OneShot = true;
		val2.Timeout += delegate
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)node))
			{
				((Node)node).QueueFree();
			}
		};
		((Node)node).AddChild((Node)(object)val2, false, (InternalMode)0);
		val2.Start(-1.0);
	}
}
