using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class DebugVisualizer
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class DebugDrawPatch
	{
		private static void Postfix(object __instance, object choiceContext, object runState, object combatState, Creature target, DamageResult result, object props, Creature dealer, CardModel cardSource)
		{
		}
	}

	private const float MARKER_LIFETIME = 0.8f;

	private const float CROSS_SIZE = 20f;

	private const float DOT_RADIUS = 10f;

	private const int CANVAS_LAYER = 100;

	public static void DrawMonsterCenter(Vector2 position)
	{
	}

	public static void DrawPlayerRoot(Vector2 position)
	{
	}

	private static Vector2[] GetCirclePoints(float radius, int segments = 64)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		Vector2[] array = (Vector2[])(object)new Vector2[segments + 1];
		float num = (float)Math.PI * 2f / (float)segments;
		for (int i = 0; i < segments; i++)
		{
			float num2 = (float)i * num;
			array[i] = new Vector2(Mathf.Cos(num2) * radius, Mathf.Sin(num2) * radius);
		}
		array[segments] = array[0];
		return array;
	}

	private static CanvasLayer GetCanvasLayer()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return null;
		}
		CanvasLayer val = ((Node)instance).GetNodeOrNull<CanvasLayer>(NodePath.op_Implicit("DebugCanvas"));
		if (val == null)
		{
			val = new CanvasLayer();
			((Node)val).Name = StringName.op_Implicit("DebugCanvas");
			val.Layer = 100;
			((Node)instance).AddChild((Node)(object)val, false, (InternalMode)0);
		}
		return val;
	}

	private static void ScheduleCleanup(CanvasLayer canvas, Node[] nodes)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		if (canvas == null)
		{
			return;
		}
		Timer val = new Timer();
		val.WaitTime = 0.800000011920929;
		val.OneShot = true;
		val.Timeout += delegate
		{
			Node[] array = nodes;
			foreach (Node val2 in array)
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)val2))
				{
					val2.QueueFree();
				}
			}
			if (((Node)canvas).GetChildCount(false) == 0 && GodotObject.IsInstanceValid((GodotObject)(object)canvas))
			{
				((Node)canvas).QueueFree();
			}
		};
		((Node)canvas).AddChild((Node)(object)val, false, (InternalMode)0);
		val.Start(-1.0);
	}
}
