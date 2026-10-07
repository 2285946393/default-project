using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RegentFX.Scripts.Vfx.Cards;

namespace RegentFX.Scripts.Vfx;

[ScriptPath("res://Scripts/Vfx/StarEffectController.cs")]
public class StarEffectController : Node2D
{
	public class MethodName : MethodName
	{
		public static readonly StringName Initialize = StringName.op_Implicit("Initialize");

		public static readonly StringName BorrowStars = StringName.op_Implicit("BorrowStars");

		public static readonly StringName InitializeShakePhases = StringName.op_Implicit("InitializeShakePhases");

		public static readonly StringName StartShaking = StringName.op_Implicit("StartShaking");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName OnPlayCard = StringName.op_Implicit("OnPlayCard");

		public static readonly StringName OnCancelCard = StringName.op_Implicit("OnCancelCard");

		public static readonly StringName ReturnStar = StringName.op_Implicit("ReturnStar");

		public static readonly StringName ReturnAllStars = StringName.op_Implicit("ReturnAllStars");

		public static readonly StringName ClearAllEffects = StringName.op_Implicit("ClearAllEffects");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName StarRingController = StringName.op_Implicit("StarRingController");

		public static readonly StringName PlayerCenterPos = StringName.op_Implicit("PlayerCenterPos");

		public static readonly StringName _starRingController = StringName.op_Implicit("_starRingController");

		public static readonly StringName _playerNode = StringName.op_Implicit("_playerNode");

		public static readonly StringName _isShaking = StringName.op_Implicit("_isShaking");
	}

	public class SignalName : SignalName
	{
	}

	private StarRingController? _starRingController;

	private NCreature? _playerNode;

	private readonly List<Star> _borrowedStars = new List<Star>();

	private bool _isShaking;

	private readonly Dictionary<Star, float> _starShakePhases = new Dictionary<Star, float>();

	private readonly Dictionary<Star, Vector2> _starTargetPositions = new Dictionary<Star, Vector2>();

	private CardFX? _currentCardFX;

	private StarRingController? StarRingController => Entry.StarRingController;

	private Vector2 PlayerCenterPos => _playerNode.VfxSpawnPosition;

	public List<Star> Stars => _borrowedStars;

	public void Initialize(NCreature playerNode, StarRingController? starRingController = null)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		_playerNode = playerNode;
		_starRingController = starRingController;
		((Node2D)this).GlobalPosition = ((Control)playerNode).GlobalPosition;
	}

	public void OnCardHolding(CardModel card, CardFX cardFX)
	{
		if (_borrowedStars.Count > 0)
		{
			ReturnAllStars();
		}
		_currentCardFX = cardFX;
		switch (cardFX.HoldingMode)
		{
		case CardFX.HoldingModes.BorrowDefault:
		case CardFX.HoldingModes.BorrowAll:
			BorrowStars();
			cardFX.TryPlayHoldingSfx();
			break;
		case CardFX.HoldingModes.Custom:
			cardFX.HoldingCustom();
			break;
		case CardFX.HoldingModes.None:
			break;
		}
	}

	public Vector2? PopStar(CardFX fx)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		if (_currentCardFX == null)
		{
			return null;
		}
		if (_currentCardFX.GetType() == fx.GetType() && _borrowedStars.Count > 0)
		{
			Star star = _borrowedStars.FindLast((Predicate<Star>)GodotObject.IsInstanceValid);
			if (star != null)
			{
				Vector2 globalPosition = ((Node2D)star).GlobalPosition;
				ReturnStar(star);
				_borrowedStars.Remove(star);
				return globalPosition;
			}
		}
		return null;
	}

	public void GenerateStarAt(Vector2 position, Color? color = null)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		Star star = Star.Create();
		((Node)this).AddChild((Node)(object)star, false, (InternalMode)0);
		star.EnablePulse = true;
		star.PulseSpeed = 2f + GD.Randf() * 1f;
		star.RotationSpeed = 45f + GD.Randf() * 45f;
		((CanvasItem)star).ZAsRelative = true;
		((Node2D)star).Scale = Vector2.One;
		star.EnableTrail = false;
		((CanvasItem)star).ZIndex = 0;
		((Node2D)star).Position = position;
		if (color.HasValue)
		{
			star.ChangeColorTo(color.Value);
		}
		_borrowedStars.Add(star);
		VFXUtil.PlaySpecialStarAt(((Node2D)star).GlobalPosition);
		_starTargetPositions[star] = ((Node2D)star).GlobalPosition;
	}

	private void BorrowStars()
	{
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		if (StarRingController == null || _currentCardFX == null)
		{
			return;
		}
		int num = ((_currentCardFX.HoldingMode == CardFX.HoldingModes.BorrowAll) ? StarRingController.GetCurrentStarCount() : _currentCardFX.StarCount);
		if (num <= 0)
		{
			return;
		}
		for (int i = 0; i < num; i++)
		{
			Star star = StarRingController.TakeStarForProjectile();
			if (star == null)
			{
				Entry.Logger.Warn($"[StarEffectController] Failed to borrow star {i + 1}/{num}", 1);
				break;
			}
			star.ToggleTrail(trail: true);
			Node parent = ((Node)star).GetParent();
			if (parent != null)
			{
				parent.RemoveChild((Node)(object)star);
			}
			((Node)this).AddChild((Node)(object)star, false, (InternalMode)0);
			((CanvasItem)star).ZIndex = 0;
			((Node2D)star).Scale = Vector2.One;
			_borrowedStars.Add(star);
			Vector2 val = _currentCardFX.CalculateTargetPosition(((Node2D)this).GlobalPosition, i, num);
			_currentCardFX.OnStartHolding(star, i);
			_starTargetPositions[star] = val;
			bool flag = i == num - 1;
			AnimateStarMove(star, val, flag ? new Action(StartShaking) : null);
		}
	}

	private void AnimateStarMove(Star star, Vector2 targetPosition, Action? onComplete = null)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		if (_currentCardFX != null)
		{
			Tween val = ((Node)this).CreateTween();
			val.SetTrans((TransitionType)5);
			val.SetEase((EaseType)1);
			RandomNumberGenerator val2 = new RandomNumberGenerator();
			(float min, float max) durationRange = _currentCardFX.DurationRange;
			float item = durationRange.min;
			float item2 = durationRange.max;
			float num = _currentCardFX.MoveDuration * (item + val2.Randf() * (item2 - item));
			val.TweenProperty((GodotObject)(object)star, NodePath.op_Implicit("global_position"), Variant.op_Implicit(targetPosition), (double)num);
			if (onComplete != null)
			{
				val.Finished += onComplete;
			}
		}
	}

	private void InitializeShakePhases()
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		_starShakePhases.Clear();
		RandomNumberGenerator val = new RandomNumberGenerator();
		val.Randomize();
		foreach (Star borrowedStar in _borrowedStars)
		{
			_starShakePhases[borrowedStar] = val.RandfRange(0f, (float)Math.PI * 2f);
		}
	}

	public void StartShaking()
	{
		_isShaking = true;
		InitializeShakePhases();
	}

	public override void _Process(double delta)
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		if (!_isShaking || _borrowedStars.Count == 0 || _currentCardFX == null)
		{
			return;
		}
		float num = (float)delta;
		float shakeIntensity = _currentCardFX.ShakeIntensity;
		float shakeSpeed = _currentCardFX.ShakeSpeed;
		foreach (Star borrowedStar in _borrowedStars)
		{
			if (borrowedStar != null && GodotObject.IsInstanceValid((GodotObject)(object)borrowedStar) && _starShakePhases.TryGetValue(borrowedStar, out var value) && _starTargetPositions.TryGetValue(borrowedStar, out var value2))
			{
				value += shakeSpeed * num;
				_starShakePhases[borrowedStar] = value;
				float num2 = Mathf.Sin(value) * shakeIntensity;
				float num3 = Mathf.Cos(value * 1.3f) * shakeIntensity;
				((Node2D)borrowedStar).GlobalPosition = value2 + new Vector2(num2, num3);
			}
		}
	}

	public void OnPlayCard()
	{
		if (_borrowedStars.Count > 0)
		{
			ReturnAllStars();
		}
		StarRingController?.ResetStarCount();
	}

	public void OnCardPlayed(CardModel card)
	{
		if (_currentCardFX?.card == card)
		{
			OnPlayCard();
		}
	}

	public void OnCancelCard()
	{
		if (_currentCardFX != null)
		{
			_currentCardFX.OnCancel();
		}
		if (_borrowedStars.Count > 0)
		{
			ReturnAllStars();
		}
		StarRingController?.ResetStarCount();
	}

	private void ReturnStar(Star star)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		VFXUtil.PlaySpecialStarAt(((Node2D)star).GlobalPosition);
		Tween obj = ((Node)this).CreateTween();
		obj.SetTrans((TransitionType)4);
		obj.SetEase((EaseType)1);
		obj.TweenProperty((GodotObject)(object)star, NodePath.op_Implicit("scale"), Variant.op_Implicit(((Node2D)star).Scale * 1.3f), 0.05000000074505806);
		obj.TweenProperty((GodotObject)(object)star, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), 0.10000000149011612);
		obj.Finished += ((Node)star).QueueFree;
	}

	private void ReturnAllStars()
	{
		_isShaking = false;
		foreach (Star borrowedStar in _borrowedStars)
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)borrowedStar))
			{
				ReturnStar(borrowedStar);
			}
		}
		_borrowedStars.Clear();
		_starShakePhases.Clear();
		_starTargetPositions.Clear();
		_currentCardFX = null;
	}

	public void ClearAllEffects()
	{
		_isShaking = false;
		foreach (Star borrowedStar in _borrowedStars)
		{
			if (borrowedStar != null)
			{
				((Node)borrowedStar).QueueFree();
			}
		}
		_borrowedStars.Clear();
		_starShakePhases.Clear();
		_starTargetPositions.Clear();
		_currentCardFX = null;
	}

	public override void _ExitTree()
	{
		ClearAllEffects();
		((Node)this)._ExitTree();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Expected O, but got Unknown
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Expected O, but got Unknown
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_027b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0284: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(11)
		{
			new MethodInfo(MethodName.Initialize, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("playerNode"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false),
				new PropertyInfo((Type)24, StringName.op_Implicit("starRingController"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.BorrowStars, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.InitializeShakePhases, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.StartShaking, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnPlayCard, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnCancelCard, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ReturnStar, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("star"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ReturnAllStars, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ClearAllEffects, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Initialize && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			Initialize(VariantUtils.ConvertTo<NCreature>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<StarRingController>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.BorrowStars && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			BorrowStars();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.InitializeShakePhases && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			InitializeShakePhases();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.StartShaking && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			StartShaking();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnPlayCard && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnPlayCard();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnCancelCard && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnCancelCard();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ReturnStar && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ReturnStar(VariantUtils.ConvertTo<Star>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ReturnAllStars && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ReturnAllStars();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ClearAllEffects && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ClearAllEffects();
			ret = default(godot_variant);
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
		if ((ref method) == MethodName.Initialize)
		{
			return true;
		}
		if ((ref method) == MethodName.BorrowStars)
		{
			return true;
		}
		if ((ref method) == MethodName.InitializeShakePhases)
		{
			return true;
		}
		if ((ref method) == MethodName.StartShaking)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.OnPlayCard)
		{
			return true;
		}
		if ((ref method) == MethodName.OnCancelCard)
		{
			return true;
		}
		if ((ref method) == MethodName.ReturnStar)
		{
			return true;
		}
		if ((ref method) == MethodName.ReturnAllStars)
		{
			return true;
		}
		if ((ref method) == MethodName.ClearAllEffects)
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
		if ((ref name) == PropertyName._starRingController)
		{
			_starRingController = VariantUtils.ConvertTo<StarRingController>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._playerNode)
		{
			_playerNode = VariantUtils.ConvertTo<NCreature>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isShaking)
		{
			_isShaking = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.StarRingController)
		{
			StarRingController starRingController = StarRingController;
			value = VariantUtils.CreateFrom<StarRingController>(ref starRingController);
			return true;
		}
		if ((ref name) == PropertyName.PlayerCenterPos)
		{
			Vector2 playerCenterPos = PlayerCenterPos;
			value = VariantUtils.CreateFrom<Vector2>(ref playerCenterPos);
			return true;
		}
		if ((ref name) == PropertyName._starRingController)
		{
			value = VariantUtils.CreateFrom<StarRingController>(ref _starRingController);
			return true;
		}
		if ((ref name) == PropertyName._playerNode)
		{
			value = VariantUtils.CreateFrom<NCreature>(ref _playerNode);
			return true;
		}
		if ((ref name) == PropertyName._isShaking)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isShaking);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._starRingController, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._playerNode, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.StarRingController, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isShaking, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName.PlayerCenterPos, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._starRingController, Variant.From<StarRingController>(ref _starRingController));
		info.AddProperty(PropertyName._playerNode, Variant.From<NCreature>(ref _playerNode));
		info.AddProperty(PropertyName._isShaking, Variant.From<bool>(ref _isShaking));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._starRingController, ref val))
		{
			_starRingController = ((Variant)(ref val)).As<StarRingController>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._playerNode, ref val2))
		{
			_playerNode = ((Variant)(ref val2)).As<NCreature>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._isShaking, ref val3))
		{
			_isShaking = ((Variant)(ref val3)).As<bool>();
		}
	}
}
