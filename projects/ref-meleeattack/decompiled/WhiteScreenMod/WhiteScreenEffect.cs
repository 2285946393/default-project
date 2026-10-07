using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace WhiteScreenMod;

public static class WhiteScreenEffect
{
	public static void TriggerWhiteFlash(float fadeIn = 0.1f, float hold = 0.4f, float fadeOut = 0.2f)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Expected O, but got Unknown
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		Node val = (Node)(((object)instance.BackCombatVfxContainer) ?? ((object)instance));
		CanvasItem val2 = (CanvasItem)(((object)((val is CanvasItem) ? val : null)) ?? ((object)instance));
		Rect2 visibleRect = ((Node)instance).GetViewport().GetVisibleRect();
		Transform2D globalTransform = val2.GetGlobalTransform();
		Transform2D val3 = ((Transform2D)(ref globalTransform)).AffineInverse();
		Vector2 val4 = val3 * ((Rect2)(ref visibleRect)).Position;
		Vector2 val5 = val3 * (((Rect2)(ref visibleRect)).Position + ((Rect2)(ref visibleRect)).Size);
		ColorRect whiteRect = new ColorRect
		{
			Position = val4,
			Size = val5 - val4,
			Color = new Color(1f, 1f, 1f, 0f),
			MouseFilter = (MouseFilterEnum)2
		};
		val.AddChild((Node)(object)whiteRect, false, (InternalMode)0);
		Tween val6 = ((Node)whiteRect).CreateTween();
		val6.SetParallel(false);
		val6.TweenProperty((GodotObject)(object)whiteRect, NodePath.op_Implicit("color:a"), Variant.op_Implicit(1f), (double)fadeIn);
		val6.TweenInterval((double)hold);
		val6.TweenProperty((GodotObject)(object)whiteRect, NodePath.op_Implicit("color:a"), Variant.op_Implicit(0f), (double)fadeOut);
		val6.TweenCallback(Callable.From((Action)delegate
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)whiteRect))
			{
				((Node)whiteRect).QueueFree();
			}
		}));
	}
}
