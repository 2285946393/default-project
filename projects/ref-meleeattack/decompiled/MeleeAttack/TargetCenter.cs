using Godot;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace MeleeAttack;

public static class TargetCenter
{
	public static Vector2 GetCenter(NCreature targetNode, float offsetY = 0f)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		if (targetNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)targetNode))
		{
			return Vector2.Zero;
		}
		if (targetNode.Hitbox != null)
		{
			Vector2 val = targetNode.Hitbox.GlobalPosition + targetNode.Hitbox.Size * 0.5f;
			return val + new Vector2(0f, offsetY);
		}
		return ((Control)targetNode).GlobalPosition + new Vector2(0f, offsetY);
	}
}
