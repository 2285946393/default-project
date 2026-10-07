using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace RegentFX.Scripts.Vfx;

public static class StarManager
{
	private const string StarScenePath = "res://RegentFX/scenes/Star.tscn";

	private static PackedScene? _cachedScene;

	private static PackedScene GetStarScene()
	{
		if (_cachedScene == null)
		{
			_cachedScene = GD.Load<PackedScene>("res://RegentFX/scenes/Star.tscn");
			if (_cachedScene == null)
			{
				GD.PushError("[StarManager] Failed to load star scene: res://RegentFX/scenes/Star.tscn");
			}
		}
		return _cachedScene;
	}

	public static Star? SpawnStar(Vector2 position, Node? parent = null)
	{
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		PackedScene starScene = GetStarScene();
		if (starScene == null)
		{
			return null;
		}
		Star star = starScene.Instantiate<Star>((GenEditState)0);
		if (star == null)
		{
			GD.PushError("[StarManager] Failed to instantiate star");
			return null;
		}
		if (parent == null)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			parent = (Node?)(object)((instance != null) ? instance.CombatVfxContainer : null);
		}
		if (parent == null)
		{
			GD.PushWarning("[StarManager] No parent available for star");
			((Node)star).QueueFree();
			return null;
		}
		GodotTreeExtensions.AddChildSafely(parent, (Node)(object)star);
		((Node2D)star).GlobalPosition = position;
		return star;
	}

	public static List<Star> SpawnStarsInArea(Vector2 center, Vector2 size, int count, Node? parent = null)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		List<Star> list = new List<Star>();
		RandomNumberGenerator val = new RandomNumberGenerator();
		val.Randomize();
		for (int i = 0; i < count; i++)
		{
			float num = val.RandfRange(center.X - size.X / 2f, center.X + size.X / 2f);
			float num2 = val.RandfRange(center.Y - size.Y / 2f, center.Y + size.Y / 2f);
			Star star = SpawnStar(new Vector2(num, num2), parent);
			if (star != null)
			{
				list.Add(star);
			}
		}
		return list;
	}

	public static List<Star> SpawnStarsInCircle(Vector2 center, float radius, int count, Node? parent = null)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		List<Star> list = new List<Star>();
		RandomNumberGenerator val = new RandomNumberGenerator();
		val.Randomize();
		for (int i = 0; i < count; i++)
		{
			float num = val.RandfRange(0f, (float)Math.PI * 2f);
			float num2 = val.RandfRange(0f, radius);
			float num3 = center.X + Mathf.Cos(num) * num2;
			float num4 = center.Y + Mathf.Sin(num) * num2;
			Star star = SpawnStar(new Vector2(num3, num4), parent);
			if (star != null)
			{
				list.Add(star);
			}
		}
		return list;
	}

	public static List<Star> SpawnStarRing(Vector2 center, float radius, int count, float startAngle = 0f, Node? parent = null)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		List<Star> list = new List<Star>();
		float num = (float)Math.PI * 2f / (float)count;
		for (int i = 0; i < count; i++)
		{
			float num2 = startAngle + num * (float)i;
			float num3 = center.X + Mathf.Cos(num2) * radius;
			float num4 = center.Y + Mathf.Sin(num2) * radius;
			Star star = SpawnStar(new Vector2(num3, num4), parent);
			if (star != null)
			{
				((Node2D)star).Rotation = num2 + (float)Math.PI / 2f;
				list.Add(star);
			}
		}
		return list;
	}

	public static async void SpawnStarSequence(Vector2[] positions, float delayBetweenStars, Node? parent = null)
	{
		for (int i = 0; i < positions.Length; i++)
		{
			SpawnStar(positions[i], parent);
			await Task.Delay((int)(delayBetweenStars * 1000f));
		}
	}

	public static void ClearAllStars(float fadeDuration = 0.5f)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		Control val = ((instance != null) ? instance.CombatVfxContainer : null);
		if (val == null)
		{
			return;
		}
		foreach (Node child in ((Node)val).GetChildren(false))
		{
			if (child is Star star)
			{
				star.FadeOutAndDestroy(fadeDuration);
			}
		}
	}

	public static List<Star> GetAllStars()
	{
		List<Star> list = new List<Star>();
		NCombatRoom instance = NCombatRoom.Instance;
		Control val = ((instance != null) ? instance.CombatVfxContainer : null);
		if (val == null)
		{
			return list;
		}
		foreach (Node child in ((Node)val).GetChildren(false))
		{
			if (child is Star item)
			{
				list.Add(item);
			}
		}
		return list;
	}
}
