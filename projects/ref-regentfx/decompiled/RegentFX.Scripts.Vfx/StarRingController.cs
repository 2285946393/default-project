using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace RegentFX.Scripts.Vfx;

[ScriptPath("res://Scripts/Vfx/StarRingController.cs")]
public class StarRingController : Node2D
{
	public class StarData
	{
		public Star Star { get; set; }

		public float CurrentAngle { get; set; }

		public float TargetAngle { get; set; }

		public bool IsSpawning { get; set; }

		public float SpawnProgress { get; set; }

		public bool IsRemoving { get; set; }

		public float RemoveProgress { get; set; }

		public float RadiusMultiplier { get; set; } = 1f;


		public Tween? RadiusTween { get; set; }
	}

	public class MethodName : MethodName
	{
		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName ResetStarCount = StringName.op_Implicit("ResetStarCount");

		public static readonly StringName SetStarCount = StringName.op_Implicit("SetStarCount");

		public static readonly StringName SpawnStars = StringName.op_Implicit("SpawnStars");

		public static readonly StringName RemoveStars = StringName.op_Implicit("RemoveStars");

		public static readonly StringName RecalculateTargetAngles = StringName.op_Implicit("RecalculateTargetAngles");

		public static readonly StringName AngleDistance = StringName.op_Implicit("AngleDistance");

		public static readonly StringName UpdateStars = StringName.op_Implicit("UpdateStars");

		public static readonly StringName BackEaseOut = StringName.op_Implicit("BackEaseOut");

		public static readonly StringName NormalizeAngle = StringName.op_Implicit("NormalizeAngle");

		public static readonly StringName FindNearestAngle = StringName.op_Implicit("FindNearestAngle");

		public static readonly StringName CalculateOrbitPosition = StringName.op_Implicit("CalculateOrbitPosition");

		public static readonly StringName ClearAllStars = StringName.op_Implicit("ClearAllStars");

		public static readonly StringName SetActive = StringName.op_Implicit("SetActive");

		public static readonly StringName TakeStarForProjectile = StringName.op_Implicit("TakeStarForProjectile");

		public static readonly StringName GetCurrentStarCount = StringName.op_Implicit("GetCurrentStarCount");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName OrbitRadius = StringName.op_Implicit("OrbitRadius");

		public static readonly StringName OrbitSpeed = StringName.op_Implicit("OrbitSpeed");

		public static readonly StringName StarScaleMin = StringName.op_Implicit("StarScaleMin");

		public static readonly StringName StarScaleMax = StringName.op_Implicit("StarScaleMax");

		public static readonly StringName MaxStarCount = StringName.op_Implicit("MaxStarCount");

		public static readonly StringName SpawnAnimationDuration = StringName.op_Implicit("SpawnAnimationDuration");

		public static readonly StringName VerticalOffset = StringName.op_Implicit("VerticalOffset");

		public static readonly StringName AngleLerpSpeed = StringName.op_Implicit("AngleLerpSpeed");

		public static readonly StringName StarEffectController = StringName.op_Implicit("StarEffectController");

		public static readonly StringName _baseOrbitRadius = StringName.op_Implicit("_baseOrbitRadius");

		public static readonly StringName _baseVerticalOffset = StringName.op_Implicit("_baseVerticalOffset");

		public static readonly StringName _playerNode = StringName.op_Implicit("_playerNode");

		public static readonly StringName _orbitAngle = StringName.op_Implicit("_orbitAngle");

		public static readonly StringName _isActive = StringName.op_Implicit("_isActive");

		public static readonly StringName childOfTheStarsMode = StringName.op_Implicit("childOfTheStarsMode");
	}

	public class SignalName : SignalName
	{
	}

	private float _baseOrbitRadius;

	private float _baseVerticalOffset;

	public const int STAR_FRONT_ZINDEX = 0;

	public const int STAR_BACK_ZINDEX = -5;

	private readonly List<StarData> _orbitStars = new List<StarData>();

	private NCreature? _playerNode;

	private float _orbitAngle;

	private bool _isActive;

	private Player _player;

	private bool childOfTheStarsMode;

	public float OrbitRadius { get; set; } = 120f;


	public float OrbitSpeed { get; set; } = 30f;


	public float StarScaleMin { get; set; } = 0.6f;


	public float StarScaleMax { get; set; } = 1f;


	public int MaxStarCount { get; set; } = 20;


	public float SpawnAnimationDuration { get; set; } = 0.3f;


	public float VerticalOffset { get; set; } = -180f;


	public float AngleLerpSpeed { get; set; } = 8f;


	public List<StarData> OrbitStars => _orbitStars;

	private StarEffectController? StarEffectController => Entry.StarEffectController;

	public override void _Process(double delta)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		if (_isActive && _playerNode != null)
		{
			float x = ((Node2D)_playerNode.Visuals).Scale.X;
			OrbitRadius = _baseOrbitRadius * x;
			VerticalOffset = _baseVerticalOffset * x;
			((Node2D)this).GlobalPosition = ((Control)_playerNode).GlobalPosition;
			_orbitAngle += OrbitSpeed * (float)delta;
			UpdateStars((float)delta);
		}
	}

	public void Initialize(NCreature playerNode, Player player)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		_playerNode = playerNode;
		_isActive = true;
		((Node2D)this).GlobalPosition = ((Control)playerNode).GlobalPosition;
		((CanvasItem)this).ZAsRelative = true;
		_player = player;
		_baseOrbitRadius = OrbitRadius;
		_baseVerticalOffset = VerticalOffset;
	}

	public void ResetStarCount()
	{
		if (_player.PlayerCombatState != null)
		{
			SetStarCount(_player.PlayerCombatState.Stars);
		}
		else
		{
			Entry.Logger.Warn("ResetStarCount: 无法获取playerCombatState", 1);
		}
	}

	public void SetStarCount(int count)
	{
		int num = Mathf.Min(count, MaxStarCount);
		int num2 = _orbitStars.Count((StarData s) => !s.IsRemoving);
		if (num > num2)
		{
			SpawnStars(num - num2);
		}
		else if (num < num2)
		{
			RemoveStars(num2 - num);
		}
		RecalculateTargetAngles();
	}

	private void SpawnStars(int count)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		if (_playerNode != null && count > 0)
		{
			float num = (float)Math.PI;
			for (int i = 0; i < count; i++)
			{
				Star star = Star.Create();
				((Node)this).AddChild((Node)(object)star, false, (InternalMode)0);
				star.EnablePulse = true;
				star.PulseSpeed = 2f + GD.Randf() * 1f;
				star.RotationSpeed = 45f + GD.Randf() * 45f;
				((CanvasItem)star).ZAsRelative = true;
				((Node2D)star).Scale = Vector2.Zero;
				((CanvasItem)star).ZIndex = -5;
				star.EnableTrail = false;
				((Node2D)star).Position = CalculateOrbitPosition(num, 1f);
				StarData item = new StarData
				{
					Star = star,
					CurrentAngle = num,
					TargetAngle = num,
					IsSpawning = true,
					SpawnProgress = 0f,
					RadiusMultiplier = 1f
				};
				_orbitStars.Add(item);
			}
		}
	}

	private void RemoveStars(int count)
	{
		List<StarData> starsToRemove = new List<StarData>();
		foreach (StarData orbitStar in _orbitStars)
		{
			if (starsToRemove.Count >= count)
			{
				break;
			}
			if (orbitStar.IsSpawning && !orbitStar.IsRemoving)
			{
				starsToRemove.Add(orbitStar);
			}
		}
		if (starsToRemove.Count < count)
		{
			List<StarData> collection = (from s in _orbitStars
				where !s.IsRemoving && !starsToRemove.Contains(s)
				orderby Mathf.Abs(NormalizeAngle(s.CurrentAngle - (float)Math.PI))
				select s).Take(count - starsToRemove.Count).ToList();
			starsToRemove.AddRange(collection);
		}
		foreach (StarData item in starsToRemove)
		{
			item.IsRemoving = true;
			item.RemoveProgress = 0f;
		}
	}

	private void RecalculateTargetAngles()
	{
		List<StarData> list = _orbitStars.Where((StarData s) => !s.IsRemoving).ToList();
		int count = list.Count;
		switch (count)
		{
		case 0:
			return;
		case 1:
			list[0].TargetAngle = list[0].CurrentAngle;
			return;
		}
		float angleStep = (float)Math.PI * 2f / (float)count;
		float orbitAngleRad = Mathf.DegToRad(_orbitAngle);
		List<float> list2 = list.Select((StarData s) => NormalizeAngle(s.CurrentAngle + orbitAngleRad)).ToList();
		float baseAngle = 0f;
		float num = float.MaxValue;
		for (int i = 0; i < count; i++)
		{
			float num2 = list2[i];
			float num3 = CalculateTotalDistance(list2, num2, angleStep);
			if (num3 < num)
			{
				num = num3;
				baseAngle = num2;
			}
		}
		AssignTargetAngles(list, baseAngle, angleStep, orbitAngleRad);
	}

	private float CalculateTotalDistance(List<float> currentAngles, float baseAngle, float angleStep)
	{
		float num = 0f;
		int count = currentAngles.Count;
		List<float> list = new List<float>();
		for (int i = 0; i < count; i++)
		{
			list.Add(NormalizeAngle(baseAngle + angleStep * (float)i));
		}
		bool[] array = new bool[count];
		foreach (float currentAngle in currentAngles)
		{
			float num2 = float.MaxValue;
			int num3 = -1;
			for (int j = 0; j < count; j++)
			{
				if (!array[j])
				{
					float num4 = AngleDistance(currentAngle, list[j]);
					if (num4 < num2)
					{
						num2 = num4;
						num3 = j;
					}
				}
			}
			if (num3 >= 0)
			{
				array[num3] = true;
				num += num2;
			}
		}
		return num;
	}

	private void AssignTargetAngles(List<StarData> activeStars, float baseAngle, float angleStep, float orbitAngleRad)
	{
		int count = activeStars.Count;
		List<(float, bool)> list = new List<(float, bool)>();
		for (int i = 0; i < count; i++)
		{
			list.Add((NormalizeAngle(baseAngle + angleStep * (float)i), false));
		}
		List<(StarData, int, float)> list2 = new List<(StarData, int, float)>();
		for (int j = 0; j < count; j++)
		{
			StarData starData = activeStars[j];
			float angle = NormalizeAngle(starData.CurrentAngle + orbitAngleRad);
			for (int k = 0; k < count; k++)
			{
				float item = AngleDistance(angle, list[k].Item1);
				list2.Add((starData, k, item));
			}
		}
		list2 = list2.OrderBy<(StarData, int, float), float>(((StarData star, int targetIndex, float distance) x) => x.distance).ToList();
		HashSet<StarData> hashSet = new HashSet<StarData>();
		HashSet<int> hashSet2 = new HashSet<int>();
		foreach (var (starData2, num, _) in list2)
		{
			if (!hashSet.Contains(starData2) && !hashSet2.Contains(num))
			{
				float targetAngle = list[num].Item1 - orbitAngleRad;
				starData2.TargetAngle = FindNearestAngle(starData2.CurrentAngle, targetAngle);
				hashSet.Add(starData2);
				hashSet2.Add(num);
			}
		}
	}

	private float AngleDistance(float angle1, float angle2)
	{
		float num;
		for (num = Mathf.Abs(angle2 - angle1); num > (float)Math.PI; num -= (float)Math.PI * 2f)
		{
		}
		return Mathf.Abs(num);
	}

	private void UpdateStars(float delta)
	{
		for (int num = _orbitStars.Count - 1; num >= 0; num--)
		{
			StarData starData = _orbitStars[num];
			if (starData.IsRemoving && starData.RemoveProgress >= 1f)
			{
				((Node)starData.Star).QueueFree();
				_orbitStars.RemoveAt(num);
			}
		}
		if (_orbitStars.Count == 0)
		{
			return;
		}
		foreach (StarData orbitStar in _orbitStars)
		{
			UpdateStar(orbitStar, delta);
		}
	}

	private void UpdateStar(StarData starData, float delta)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		Star star = starData.Star;
		if (star == null)
		{
			return;
		}
		if (starData.IsSpawning)
		{
			starData.SpawnProgress += delta / SpawnAnimationDuration;
			if (starData.SpawnProgress >= 1f)
			{
				starData.SpawnProgress = 1f;
				starData.IsSpawning = false;
			}
			float spawnProgress = starData.SpawnProgress;
			float num = BackEaseOut(spawnProgress);
			((Node2D)star).Scale = new Vector2(num, num);
		}
		if (starData.IsRemoving)
		{
			starData.RemoveProgress += delta / SpawnAnimationDuration;
			if (starData.RemoveProgress > 1f)
			{
				starData.RemoveProgress = 1f;
			}
			float num2 = 1f - starData.RemoveProgress;
			((Node2D)star).Scale = new Vector2(num2, num2);
			return;
		}
		float num3 = starData.TargetAngle - starData.CurrentAngle;
		if (num3 > (float)Math.PI)
		{
			num3 -= (float)Math.PI * 2f;
		}
		if (num3 < -(float)Math.PI)
		{
			num3 += (float)Math.PI * 2f;
		}
		float num4 = Mathf.Min(AngleLerpSpeed * delta, 1f);
		starData.CurrentAngle += num3 * num4;
		float num5 = starData.CurrentAngle + Mathf.DegToRad(_orbitAngle);
		float num6 = Mathf.Sin(num5);
		float num7 = (num6 + 1f) / 2f;
		float num8 = Mathf.Lerp(StarScaleMin, StarScaleMax, num7);
		Vector2 scale = ((Node2D)star).Scale;
		if (!starData.IsSpawning && !starData.IsRemoving)
		{
			((Node2D)star).Scale = new Vector2(num8, num8);
		}
		else
		{
			((Node2D)star).Scale = new Vector2(scale.X * num8, scale.Y * num8);
		}
		((Node2D)star).Position = CalculateOrbitPosition(num5, starData.RadiusMultiplier);
		((CanvasItem)star).ZIndex = ((!(num6 > 0f)) ? (-5) : 0);
		((CanvasItem)star).ZAsRelative = true;
	}

	private float BackEaseOut(float t)
	{
		return 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
	}

	private float NormalizeAngle(float angle)
	{
		while (angle < 0f)
		{
			angle += (float)Math.PI * 2f;
		}
		while (angle >= (float)Math.PI * 2f)
		{
			angle -= (float)Math.PI * 2f;
		}
		return angle;
	}

	private float FindNearestAngle(float currentAngle, float targetAngle)
	{
		float num;
		for (num = targetAngle - currentAngle; num > (float)Math.PI; num -= (float)Math.PI * 2f)
		{
		}
		for (; num < -(float)Math.PI; num += (float)Math.PI * 2f)
		{
		}
		return currentAngle + num;
	}

	private Vector2 CalculateOrbitPosition(float angle, float radiusMultiplier)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Cos(angle) * OrbitRadius * radiusMultiplier;
		float num2 = Mathf.Sin(angle) * OrbitRadius * 0.4f * radiusMultiplier + VerticalOffset;
		return new Vector2(num, num2);
	}

	public void ClearAllStars(bool animate = false)
	{
		if (animate)
		{
			foreach (StarData orbitStar in _orbitStars)
			{
				orbitStar.Star?.FadeOutAndDestroy(0.3f);
			}
		}
		else
		{
			foreach (StarData orbitStar2 in _orbitStars)
			{
				Star star = orbitStar2.Star;
				if (star != null)
				{
					((Node)star).QueueFree();
				}
			}
		}
		_orbitStars.Clear();
	}

	public void SetActive(bool active)
	{
		_isActive = active;
		((CanvasItem)this).Visible = active;
	}

	public Star? TakeStarForProjectile()
	{
		StarData starData = _orbitStars.FirstOrDefault((StarData s) => !s.IsRemoving);
		if (starData == null)
		{
			return null;
		}
		Star star = starData.Star;
		_orbitStars.Remove(starData);
		RecalculateTargetAngles();
		return star;
	}

	public int GetCurrentStarCount()
	{
		return _orbitStars.Count((StarData s) => !s.IsRemoving);
	}

	public override void _ExitTree()
	{
		ClearAllStars();
		((Node)this)._ExitTree();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0365: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_0391: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_03da: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0406: Unknown result type (might be due to invalid IL or missing references)
		//IL_042c: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e4: Expected O, but got Unknown
		//IL_04df: Unknown result type (might be due to invalid IL or missing references)
		//IL_04e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_050e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0517: Unknown result type (might be due to invalid IL or missing references)
		//IL_053d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0546: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(17)
		{
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ResetStarCount, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetStarCount, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("count"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SpawnStars, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("count"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.RemoveStars, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("count"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.RecalculateTargetAngles, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.AngleDistance, new PropertyInfo((Type)3, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("angle1"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)3, StringName.op_Implicit("angle2"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateStars, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.BackEaseOut, new PropertyInfo((Type)3, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("t"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.NormalizeAngle, new PropertyInfo((Type)3, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("angle"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.FindNearestAngle, new PropertyInfo((Type)3, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("currentAngle"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)3, StringName.op_Implicit("targetAngle"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.CalculateOrbitPosition, new PropertyInfo((Type)5, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("angle"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)3, StringName.op_Implicit("radiusMultiplier"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ClearAllStars, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("animate"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetActive, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("active"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.TakeStarForProjectile, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetCurrentStarCount, new PropertyInfo((Type)2, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Unknown result type (might be due to invalid IL or missing references)
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02be: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_031a: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_034c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ResetStarCount && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ResetStarCount();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetStarCount && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetStarCount(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SpawnStars && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SpawnStars(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RemoveStars && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			RemoveStars(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.RecalculateTargetAngles && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			RecalculateTargetAngles();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.AngleDistance && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			float num = AngleDistance(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<float>(ref num);
			return true;
		}
		if ((ref method) == MethodName.UpdateStars && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateStars(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.BackEaseOut && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			float num2 = BackEaseOut(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<float>(ref num2);
			return true;
		}
		if ((ref method) == MethodName.NormalizeAngle && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			float num3 = NormalizeAngle(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<float>(ref num3);
			return true;
		}
		if ((ref method) == MethodName.FindNearestAngle && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			float num4 = FindNearestAngle(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<float>(ref num4);
			return true;
		}
		if ((ref method) == MethodName.CalculateOrbitPosition && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			Vector2 val = CalculateOrbitPosition(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<Vector2>(ref val);
			return true;
		}
		if ((ref method) == MethodName.ClearAllStars && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ClearAllStars(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetActive && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetActive(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.TakeStarForProjectile && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Star star = TakeStarForProjectile();
			ret = VariantUtils.CreateFrom<Star>(ref star);
			return true;
		}
		if ((ref method) == MethodName.GetCurrentStarCount && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			int currentStarCount = GetCurrentStarCount();
			ret = VariantUtils.CreateFrom<int>(ref currentStarCount);
			return true;
		}
		if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._ExitTree();
			ret = default(godot_variant);
			return true;
		}
		return ((Node2D)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.ResetStarCount)
		{
			return true;
		}
		if ((ref method) == MethodName.SetStarCount)
		{
			return true;
		}
		if ((ref method) == MethodName.SpawnStars)
		{
			return true;
		}
		if ((ref method) == MethodName.RemoveStars)
		{
			return true;
		}
		if ((ref method) == MethodName.RecalculateTargetAngles)
		{
			return true;
		}
		if ((ref method) == MethodName.AngleDistance)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateStars)
		{
			return true;
		}
		if ((ref method) == MethodName.BackEaseOut)
		{
			return true;
		}
		if ((ref method) == MethodName.NormalizeAngle)
		{
			return true;
		}
		if ((ref method) == MethodName.FindNearestAngle)
		{
			return true;
		}
		if ((ref method) == MethodName.CalculateOrbitPosition)
		{
			return true;
		}
		if ((ref method) == MethodName.ClearAllStars)
		{
			return true;
		}
		if ((ref method) == MethodName.SetActive)
		{
			return true;
		}
		if ((ref method) == MethodName.TakeStarForProjectile)
		{
			return true;
		}
		if ((ref method) == MethodName.GetCurrentStarCount)
		{
			return true;
		}
		if ((ref method) == MethodName._ExitTree)
		{
			return true;
		}
		return ((Node2D)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.OrbitRadius)
		{
			OrbitRadius = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.OrbitSpeed)
		{
			OrbitSpeed = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.StarScaleMin)
		{
			StarScaleMin = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.StarScaleMax)
		{
			StarScaleMax = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.MaxStarCount)
		{
			MaxStarCount = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.SpawnAnimationDuration)
		{
			SpawnAnimationDuration = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.VerticalOffset)
		{
			VerticalOffset = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.AngleLerpSpeed)
		{
			AngleLerpSpeed = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._baseOrbitRadius)
		{
			_baseOrbitRadius = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._baseVerticalOffset)
		{
			_baseVerticalOffset = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._playerNode)
		{
			_playerNode = VariantUtils.ConvertTo<NCreature>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._orbitAngle)
		{
			_orbitAngle = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isActive)
		{
			_isActive = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.childOfTheStarsMode)
		{
			childOfTheStarsMode = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.OrbitRadius)
		{
			float orbitRadius = OrbitRadius;
			value = VariantUtils.CreateFrom<float>(ref orbitRadius);
			return true;
		}
		if ((ref name) == PropertyName.OrbitSpeed)
		{
			float orbitRadius = OrbitSpeed;
			value = VariantUtils.CreateFrom<float>(ref orbitRadius);
			return true;
		}
		if ((ref name) == PropertyName.StarScaleMin)
		{
			float orbitRadius = StarScaleMin;
			value = VariantUtils.CreateFrom<float>(ref orbitRadius);
			return true;
		}
		if ((ref name) == PropertyName.StarScaleMax)
		{
			float orbitRadius = StarScaleMax;
			value = VariantUtils.CreateFrom<float>(ref orbitRadius);
			return true;
		}
		if ((ref name) == PropertyName.MaxStarCount)
		{
			int maxStarCount = MaxStarCount;
			value = VariantUtils.CreateFrom<int>(ref maxStarCount);
			return true;
		}
		if ((ref name) == PropertyName.SpawnAnimationDuration)
		{
			float orbitRadius = SpawnAnimationDuration;
			value = VariantUtils.CreateFrom<float>(ref orbitRadius);
			return true;
		}
		if ((ref name) == PropertyName.VerticalOffset)
		{
			float orbitRadius = VerticalOffset;
			value = VariantUtils.CreateFrom<float>(ref orbitRadius);
			return true;
		}
		if ((ref name) == PropertyName.AngleLerpSpeed)
		{
			float orbitRadius = AngleLerpSpeed;
			value = VariantUtils.CreateFrom<float>(ref orbitRadius);
			return true;
		}
		if ((ref name) == PropertyName.StarEffectController)
		{
			StarEffectController starEffectController = StarEffectController;
			value = VariantUtils.CreateFrom<StarEffectController>(ref starEffectController);
			return true;
		}
		if ((ref name) == PropertyName._baseOrbitRadius)
		{
			value = VariantUtils.CreateFrom<float>(ref _baseOrbitRadius);
			return true;
		}
		if ((ref name) == PropertyName._baseVerticalOffset)
		{
			value = VariantUtils.CreateFrom<float>(ref _baseVerticalOffset);
			return true;
		}
		if ((ref name) == PropertyName._playerNode)
		{
			value = VariantUtils.CreateFrom<NCreature>(ref _playerNode);
			return true;
		}
		if ((ref name) == PropertyName._orbitAngle)
		{
			value = VariantUtils.CreateFrom<float>(ref _orbitAngle);
			return true;
		}
		if ((ref name) == PropertyName._isActive)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isActive);
			return true;
		}
		if ((ref name) == PropertyName.childOfTheStarsMode)
		{
			value = VariantUtils.CreateFrom<bool>(ref childOfTheStarsMode);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, PropertyName.OrbitRadius, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.OrbitSpeed, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.StarScaleMin, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.StarScaleMax, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName.MaxStarCount, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.SpawnAnimationDuration, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.VerticalOffset, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.AngleLerpSpeed, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._baseOrbitRadius, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._baseVerticalOffset, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._playerNode, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._orbitAngle, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isActive, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.StarEffectController, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.childOfTheStarsMode, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName orbitRadius = PropertyName.OrbitRadius;
		float orbitRadius2 = OrbitRadius;
		info.AddProperty(orbitRadius, Variant.From<float>(ref orbitRadius2));
		StringName orbitSpeed = PropertyName.OrbitSpeed;
		orbitRadius2 = OrbitSpeed;
		info.AddProperty(orbitSpeed, Variant.From<float>(ref orbitRadius2));
		StringName starScaleMin = PropertyName.StarScaleMin;
		orbitRadius2 = StarScaleMin;
		info.AddProperty(starScaleMin, Variant.From<float>(ref orbitRadius2));
		StringName starScaleMax = PropertyName.StarScaleMax;
		orbitRadius2 = StarScaleMax;
		info.AddProperty(starScaleMax, Variant.From<float>(ref orbitRadius2));
		StringName maxStarCount = PropertyName.MaxStarCount;
		int maxStarCount2 = MaxStarCount;
		info.AddProperty(maxStarCount, Variant.From<int>(ref maxStarCount2));
		StringName spawnAnimationDuration = PropertyName.SpawnAnimationDuration;
		orbitRadius2 = SpawnAnimationDuration;
		info.AddProperty(spawnAnimationDuration, Variant.From<float>(ref orbitRadius2));
		StringName verticalOffset = PropertyName.VerticalOffset;
		orbitRadius2 = VerticalOffset;
		info.AddProperty(verticalOffset, Variant.From<float>(ref orbitRadius2));
		StringName angleLerpSpeed = PropertyName.AngleLerpSpeed;
		orbitRadius2 = AngleLerpSpeed;
		info.AddProperty(angleLerpSpeed, Variant.From<float>(ref orbitRadius2));
		info.AddProperty(PropertyName._baseOrbitRadius, Variant.From<float>(ref _baseOrbitRadius));
		info.AddProperty(PropertyName._baseVerticalOffset, Variant.From<float>(ref _baseVerticalOffset));
		info.AddProperty(PropertyName._playerNode, Variant.From<NCreature>(ref _playerNode));
		info.AddProperty(PropertyName._orbitAngle, Variant.From<float>(ref _orbitAngle));
		info.AddProperty(PropertyName._isActive, Variant.From<bool>(ref _isActive));
		info.AddProperty(PropertyName.childOfTheStarsMode, Variant.From<bool>(ref childOfTheStarsMode));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.OrbitRadius, ref val))
		{
			OrbitRadius = ((Variant)(ref val)).As<float>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.OrbitSpeed, ref val2))
		{
			OrbitSpeed = ((Variant)(ref val2)).As<float>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.StarScaleMin, ref val3))
		{
			StarScaleMin = ((Variant)(ref val3)).As<float>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName.StarScaleMax, ref val4))
		{
			StarScaleMax = ((Variant)(ref val4)).As<float>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName.MaxStarCount, ref val5))
		{
			MaxStarCount = ((Variant)(ref val5)).As<int>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName.SpawnAnimationDuration, ref val6))
		{
			SpawnAnimationDuration = ((Variant)(ref val6)).As<float>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName.VerticalOffset, ref val7))
		{
			VerticalOffset = ((Variant)(ref val7)).As<float>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName.AngleLerpSpeed, ref val8))
		{
			AngleLerpSpeed = ((Variant)(ref val8)).As<float>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._baseOrbitRadius, ref val9))
		{
			_baseOrbitRadius = ((Variant)(ref val9)).As<float>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._baseVerticalOffset, ref val10))
		{
			_baseVerticalOffset = ((Variant)(ref val10)).As<float>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName._playerNode, ref val11))
		{
			_playerNode = ((Variant)(ref val11)).As<NCreature>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName._orbitAngle, ref val12))
		{
			_orbitAngle = ((Variant)(ref val12)).As<float>();
		}
		Variant val13 = default(Variant);
		if (info.TryGetProperty(PropertyName._isActive, ref val13))
		{
			_isActive = ((Variant)(ref val13)).As<bool>();
		}
		Variant val14 = default(Variant);
		if (info.TryGetProperty(PropertyName.childOfTheStarsMode, ref val14))
		{
			childOfTheStarsMode = ((Variant)(ref val14)).As<bool>();
		}
	}
}
