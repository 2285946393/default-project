using System;
using System.Collections.Generic;
using Godot;

namespace MeleeAttack;

public static class DrawingUtils
{
	public static Line2D CreateLine(Node2D parent, Vector2[] points, Color color, float width = 2f, bool antialiased = true, int zIndex = 0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		Line2D val = new Line2D
		{
			Points = points,
			DefaultColor = color,
			Width = width,
			Antialiased = antialiased,
			ZIndex = zIndex,
			ZAsRelative = true
		};
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		return val;
	}

	public static Vector2[] GetCirclePoints(float radius, int segments = 64)
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

	public static Line2D DrawCircle(Node2D parent, float radius, Color color, float width = 2f, int segments = 64, int zIndex = 0)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		return CreateLine(parent, GetCirclePoints(radius, segments), color, width, antialiased: true, zIndex);
	}

	public static Line2D DrawLine(Node2D parent, Vector2 from, Vector2 to, Color color, float width = 2f, int zIndex = 0)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		return CreateLine(parent, (Vector2[])(object)new Vector2[2] { from, to }, color, width, antialiased: true, zIndex);
	}

	public static Vector2[] GenerateLightningPoints(Vector2 start, Vector2 end, float jaggedness = 30f, int segments = 10)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = end - start;
		float num = ((Vector2)(ref val)).Length();
		if (num < 0.01f)
		{
			return (Vector2[])(object)new Vector2[2] { start, end };
		}
		Vector2 val2 = new Vector2(0f - val.Y, val.X);
		Vector2 val3 = ((Vector2)(ref val2)).Normalized();
		List<Vector2> list = new List<Vector2> { start };
		float num2 = num / (float)segments;
		for (int i = 1; i < segments; i++)
		{
			float num3 = (float)i / (float)segments;
			Vector2 val4 = start + val * num3;
			float num4 = (GD.Randf() - 0.5f) * 2f * jaggedness * (1f - Mathf.Abs(num3 - 0.5f) * 0.8f);
			Vector2 item = val4 + val3 * num4;
			list.Add(item);
		}
		list.Add(end);
		return list.ToArray();
	}
}
