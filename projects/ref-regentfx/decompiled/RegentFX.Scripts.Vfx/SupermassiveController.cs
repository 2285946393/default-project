using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.Scripts.Vfx.Cards;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx;

[ScriptPath("res://Scripts/Vfx/SupermassiveController.cs")]
public class SupermassiveController : Node2D
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName ClearImmediate = StringName.op_Implicit("ClearImmediate");

		public static readonly StringName CalculateScale = StringName.op_Implicit("CalculateScale");

		public static readonly StringName SubscribeToPiles = StringName.op_Implicit("SubscribeToPiles");

		public static readonly StringName UnsubscribeFromPiles = StringName.op_Implicit("UnsubscribeFromPiles");

		public static readonly StringName OnPileContentsChanged = StringName.op_Implicit("OnPileContentsChanged");

		public static readonly StringName SyncPossessionDeferred = StringName.op_Implicit("SyncPossessionDeferred");

		public static readonly StringName HasActiveSupermassive = StringName.op_Implicit("HasActiveSupermassive");

		public static readonly StringName GetGeneratedCardCount = StringName.op_Implicit("GetGeneratedCardCount");

		public static readonly StringName CreateOrb = StringName.op_Implicit("CreateOrb");

		public static readonly StringName TweenOrbToLatestScale = StringName.op_Implicit("TweenOrbToLatestScale");

		public static readonly StringName PlayOneShotAnimation = StringName.op_Implicit("PlayOneShotAnimation");

		public static readonly StringName OnFlightFinished = StringName.op_Implicit("OnFlightFinished");

		public static readonly StringName FinishAttack = StringName.op_Implicit("FinishAttack");

		public static readonly StringName IsCurrentAttack = StringName.op_Implicit("IsCurrentAttack");

		public static readonly StringName DismissAllVisuals = StringName.op_Implicit("DismissAllVisuals");

		public static readonly StringName FadeAndFree = StringName.op_Implicit("FadeAndFree");

		public static readonly StringName UpdateWander = StringName.op_Implicit("UpdateWander");

		public static readonly StringName PickNewWanderTarget = StringName.op_Implicit("PickNewWanderTarget");

		public static readonly StringName GetAnchorPosition = StringName.op_Implicit("GetAnchorPosition");

		public static readonly StringName GetPlayerVisualScale = StringName.op_Implicit("GetPlayerVisualScale");

		public static readonly StringName IsValid = StringName.op_Implicit("IsValid");

		public static readonly StringName QueueFreeIfValid = StringName.op_Implicit("QueueFreeIfValid");

		public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName HasReadyOrb = StringName.op_Implicit("HasReadyOrb");

		public static readonly StringName _rng = StringName.op_Implicit("_rng");

		public static readonly StringName _playerNode = StringName.op_Implicit("_playerNode");

		public static readonly StringName _orb = StringName.op_Implicit("_orb");

		public static readonly StringName _attackOrb = StringName.op_Implicit("_attackOrb");

		public static readonly StringName _scaleTween = StringName.op_Implicit("_scaleTween");

		public static readonly StringName _flightTween = StringName.op_Implicit("_flightTween");

		public static readonly StringName _wanderOffset = StringName.op_Implicit("_wanderOffset");

		public static readonly StringName _wanderTarget = StringName.op_Implicit("_wanderTarget");

		public static readonly StringName _wanderRetargetTimer = StringName.op_Implicit("_wanderRetargetTimer");

		public static readonly StringName _latestScale = StringName.op_Implicit("_latestScale");

		public static readonly StringName _lifecycleVersion = StringName.op_Implicit("_lifecycleVersion");

		public static readonly StringName _isAttacking = StringName.op_Implicit("_isAttacking");

		public static readonly StringName _possessionSyncQueued = StringName.op_Implicit("_possessionSyncQueued");

		public static readonly StringName _isClearing = StringName.op_Implicit("_isClearing");
	}

	public class SignalName : SignalName
	{
	}

	public const string ScenePath = "res://RegentFX/scenes/vfx/super_massive.tscn";

	private const float BaseScale = 0.2f;

	private const float MaxScaleMultiplier = 4f;

	private const float SpawnDuration = 0.25f;

	private const float GrowScaleDuration = 0.2f;

	private const float DismissDuration = 0.2f;

	private const float FlightDuration = 0.15f;

	private const float WanderRadius = 55f;

	private const float WanderLerpSpeed = 0.85f;

	private const float WanderRetargetMin = 2f;

	private const float WanderRetargetMax = 3.5f;

	private static readonly Vector2 BaseAnchorOffset = new Vector2(120f, -250f);

	private readonly List<CardPile> _subscribedPiles = new List<CardPile>();

	private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();

	private NCreature? _playerNode;

	private Player? _player;

	private Node2D? _orb;

	private Node2D? _attackOrb;

	private Tween? _scaleTween;

	private Tween? _flightTween;

	private Vector2 _wanderOffset;

	private Vector2 _wanderTarget;

	private float _wanderRetargetTimer;

	private float _latestScale = 0.2f;

	private ulong _lifecycleVersion;

	private bool _isAttacking;

	private bool _possessionSyncQueued;

	private bool _isClearing;

	public bool HasReadyOrb
	{
		get
		{
			if (!_isClearing && !_isAttacking)
			{
				return IsValid((GodotObject?)(object)_orb);
			}
			return false;
		}
	}

	public void Initialize(NCreature playerNode, Player player)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		_playerNode = playerNode;
		_player = player;
		_rng.Randomize();
		((Node2D)this).GlobalPosition = ((Control)playerNode).GlobalPosition;
		((CanvasItem)this).ZAsRelative = true;
		SubscribeToPiles();
		PickNewWanderTarget();
	}

	public override void _Process(double delta)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		if (!_isClearing && _playerNode != null && _player != null && GodotObject.IsInstanceValid((GodotObject)(object)_playerNode) && NCombatRoom.Instance != null)
		{
			if (_player.Creature.IsDead)
			{
				ClearImmediate();
				return;
			}
			((Node2D)this).GlobalPosition = ((Control)_playerNode).GlobalPosition;
			UpdateWander((float)delta);
		}
	}

	public void OnCardGenerated(Player? creator)
	{
		if (_isClearing || _player == null || creator != _player || !CardFX.IsTypeEnabled<Supermassive>() || NCombatRoom.Instance == null || _player.Creature.IsDead)
		{
			return;
		}
		_latestScale = CalculateScale(GetGeneratedCardCount());
		if (HasActiveSupermassive() && !_isAttacking)
		{
			if (!IsValid((GodotObject?)(object)_orb))
			{
				CreateOrb(playGrow: true);
				return;
			}
			TweenOrbToLatestScale(0.2f, ensureVisible: true);
			PlayOneShotAnimation(_orb, StringName.op_Implicit("grow"), 1);
		}
	}

	public bool TryLaunch(Creature target)
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		if (_isClearing || _isAttacking || !IsValid((GodotObject?)(object)_orb))
		{
			return false;
		}
		if (_player == null || _player.Creature.IsDead || target.IsDead)
		{
			return false;
		}
		if (!CardFX.IsTypeEnabled<Supermassive>() || !HasActiveSupermassive())
		{
			return false;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(target) : null);
		Node val2 = (Node)(object)((instance != null) ? instance.CombatVfxContainer : null);
		if (instance == null || val == null || val2 == null)
		{
			return false;
		}
		Vector2 vfxSpawnPosition = val.VfxSpawnPosition;
		Node2D attackOrb = _orb;
		_orb = null;
		_attackOrb = attackOrb;
		_isAttacking = true;
		_lifecycleVersion++;
		ulong version = _lifecycleVersion;
		Tween? scaleTween = _scaleTween;
		if (scaleTween != null)
		{
			scaleTween.Kill();
		}
		_scaleTween = null;
		Transform2D globalTransform = attackOrb.GlobalTransform;
		Node parent = ((Node)attackOrb).GetParent();
		if (parent != null)
		{
			GodotTreeExtensions.RemoveChildSafely(parent, (Node)(object)attackOrb);
		}
		GodotTreeExtensions.AddChildSafely(val2, (Node)(object)attackOrb);
		attackOrb.GlobalTransform = globalTransform;
		FmodLite.Play("event:/RegentFx/sfx/supermassive_launch", "sizelaunch", _latestScale);
		Tween? flightTween = _flightTween;
		if (flightTween != null)
		{
			flightTween.Kill();
		}
		_flightTween = ((Node)attackOrb).CreateTween();
		_flightTween.SetTrans((TransitionType)4);
		_flightTween.SetEase((EaseType)0);
		_flightTween.TweenProperty((GodotObject)(object)attackOrb, NodePath.op_Implicit("global_position"), Variant.op_Implicit(vfxSpawnPosition), 0.15000000596046448);
		_flightTween.Finished += delegate
		{
			OnFlightFinished(attackOrb, version);
		};
		return true;
	}

	public void ClearImmediate()
	{
		if (!_isClearing)
		{
			_isClearing = true;
			_lifecycleVersion++;
			_possessionSyncQueued = false;
			UnsubscribeFromPiles();
			Tween? scaleTween = _scaleTween;
			if (scaleTween != null)
			{
				scaleTween.Kill();
			}
			Tween? flightTween = _flightTween;
			if (flightTween != null)
			{
				flightTween.Kill();
			}
			_scaleTween = null;
			_flightTween = null;
			QueueFreeIfValid((Node?)(object)_orb);
			if (_attackOrb != _orb)
			{
				QueueFreeIfValid((Node?)(object)_attackOrb);
			}
			_orb = null;
			_attackOrb = null;
			_isAttacking = false;
		}
	}

	internal static float CalculateScale(int generatedCardCount)
	{
		float num = Mathf.Min(4f, 1f + Mathf.Sqrt((float)Mathf.Max(0, generatedCardCount) / 3f));
		return 0.2f * num;
	}

	private void SubscribeToPiles()
	{
		Player? player = _player;
		PlayerCombatState val = ((player != null) ? player.PlayerCombatState : null);
		if (val == null)
		{
			Entry.Logger.Warn("[Supermassive] 玩家战斗牌堆尚未初始化", 1);
			return;
		}
		foreach (CardPile allPile in val.AllPiles)
		{
			allPile.ContentsChanged += OnPileContentsChanged;
			_subscribedPiles.Add(allPile);
		}
	}

	private void UnsubscribeFromPiles()
	{
		foreach (CardPile subscribedPile in _subscribedPiles)
		{
			subscribedPile.ContentsChanged -= OnPileContentsChanged;
		}
		_subscribedPiles.Clear();
	}

	private void OnPileContentsChanged()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		if (!_isClearing && !_possessionSyncQueued)
		{
			_possessionSyncQueued = true;
			Callable val = Callable.From((Action)SyncPossessionDeferred);
			((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		}
	}

	private void SyncPossessionDeferred()
	{
		_possessionSyncQueued = false;
		if (!_isClearing && (!CardFX.IsTypeEnabled<Supermassive>() || !HasActiveSupermassive()))
		{
			DismissAllVisuals();
		}
	}

	private bool HasActiveSupermassive()
	{
		Player? player = _player;
		PlayerCombatState val = ((player != null) ? player.PlayerCombatState : null);
		if (val == null)
		{
			return false;
		}
		if (!val.Hand.Cards.Any(IsSupermassive) && !val.DrawPile.Cards.Any(IsSupermassive) && !val.DiscardPile.Cards.Any(IsSupermassive))
		{
			return val.PlayPile.Cards.Any(IsSupermassive);
		}
		return true;
	}

	private static bool IsSupermassive(CardModel card)
	{
		return card is Supermassive;
	}

	private int GetGeneratedCardCount()
	{
		if (_player == null)
		{
			return 0;
		}
		return CombatManager.Instance.History.Entries.OfType<CardGeneratedEntry>().Count((CardGeneratedEntry entry) => entry.Creator == _player);
	}

	private void CreateOrb(bool playGrow)
	{
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		if (_isClearing || _isAttacking || IsValid((GodotObject?)(object)_orb) || NCombatRoom.Instance == null)
		{
			return;
		}
		Node2D node = null;
		try
		{
			Node2D val = VFXUtil.GenVFXNode("res://RegentFX/scenes/vfx/super_massive.tscn");
			node = val;
			AnimatedSprite2D nodeOrNull = ((Node)val).GetNodeOrNull<AnimatedSprite2D>(NodePath.op_Implicit("Ball"));
			if (nodeOrNull == null || nodeOrNull.SpriteFrames == null)
			{
				Entry.Logger.Error("[Supermassive] super_massive.tscn 缺少 Ball/SpriteFrames", 1);
				GodotTreeExtensions.QueueFreeSafely((Node)(object)val);
				return;
			}
			FmodLite.Play("event:/RegentFx/sfx/supermassive_create", "sizecreate", _latestScale);
			_lifecycleVersion++;
			_orb = val;
			val.Position = GetAnchorPosition();
			val.Scale = Vector2.Zero;
			((CanvasItem)val).Modulate = new Color(1f, 1f, 1f, 0f);
			GodotTreeExtensions.AddChildSafely((Node)(object)this, (Node)(object)val);
			nodeOrNull.Play(StringName.op_Implicit("ball"), 1f, false);
			TweenOrbToLatestScale(0.25f, ensureVisible: true);
			if (playGrow)
			{
				PlayOneShotAnimation(val, StringName.op_Implicit("grow"), 1);
			}
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn("[Supermassive] 创建黑洞失败: " + ex.Message, 1);
			QueueFreeIfValid((Node?)(object)node);
			_orb = null;
		}
	}

	private void TweenOrbToLatestScale(float duration, bool ensureVisible)
	{
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		if (IsValid((GodotObject?)(object)_orb))
		{
			Tween? scaleTween = _scaleTween;
			if (scaleTween != null)
			{
				scaleTween.Kill();
			}
			_scaleTween = ((Node)_orb).CreateTween().SetParallel(true);
			_scaleTween.SetTrans((TransitionType)4);
			_scaleTween.SetEase((EaseType)1);
			_scaleTween.TweenProperty((GodotObject)(object)_orb, NodePath.op_Implicit("scale"), Variant.op_Implicit(Vector2.One * _latestScale), (double)duration);
			if (ensureVisible)
			{
				_scaleTween.TweenProperty((GodotObject)(object)_orb, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(1f), (double)duration);
			}
		}
	}

	private void PlayOneShotAnimation(Node2D parent, StringName animation, int zIndex)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		if (!IsValid((GodotObject?)(object)parent))
		{
			return;
		}
		AnimatedSprite2D nodeOrNull = ((Node)parent).GetNodeOrNull<AnimatedSprite2D>(NodePath.op_Implicit("Ball"));
		SpriteFrames val = ((nodeOrNull != null) ? nodeOrNull.SpriteFrames : null);
		if (val != null && val.HasAnimation(animation))
		{
			AnimatedSprite2D sprite = new AnimatedSprite2D
			{
				SpriteFrames = val,
				Animation = animation,
				ZIndex = zIndex,
				ZAsRelative = true
			};
			GodotTreeExtensions.AddChildSafely((Node)(object)parent, (Node)(object)sprite);
			sprite.AnimationFinished += delegate
			{
				QueueFreeIfValid((Node?)(object)sprite);
			};
			FmodLite.Play("event:/RegentFx/sfx/supermassive_grow", "sizegrow", _latestScale);
			sprite.Play(animation, 1f, false);
		}
	}

	private void OnFlightFinished(Node2D attackOrb, ulong version)
	{
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected O, but got Unknown
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		Node2D attackOrb2 = attackOrb;
		_flightTween = null;
		if (!IsCurrentAttack(attackOrb2, version))
		{
			return;
		}
		AnimatedSprite2D nodeOrNull = ((Node)attackOrb2).GetNodeOrNull<AnimatedSprite2D>(NodePath.op_Implicit("Ball"));
		SpriteFrames val = ((nodeOrNull != null) ? nodeOrNull.SpriteFrames : null);
		if (nodeOrNull != null)
		{
			nodeOrNull.Stop();
			((CanvasItem)nodeOrNull).Visible = false;
		}
		if (val == null || !val.HasAnimation(StringName.op_Implicit("explode")))
		{
			FinishAttack(attackOrb2, version);
			return;
		}
		AnimatedSprite2D val2 = new AnimatedSprite2D
		{
			SpriteFrames = val,
			Animation = StringName.op_Implicit("explode"),
			ZIndex = 2,
			ZAsRelative = true
		};
		((Node2D)val2).Scale = ((Node2D)val2).Scale * 3f;
		GodotTreeExtensions.AddChildSafely((Node)(object)attackOrb2, (Node)(object)val2);
		val2.AnimationFinished += delegate
		{
			FinishAttack(attackOrb2, version);
		};
		val2.Play(StringName.op_Implicit("explode"), 1f, false);
	}

	private void FinishAttack(Node2D attackOrb, ulong version)
	{
		if (IsCurrentAttack(attackOrb, version))
		{
			QueueFreeIfValid((Node?)(object)attackOrb);
			_attackOrb = null;
			_isAttacking = false;
			if (!_isClearing && _player != null && !_player.Creature.IsDead && NCombatRoom.Instance != null && CardFX.IsTypeEnabled<Supermassive>() && HasActiveSupermassive())
			{
				_latestScale = CalculateScale(GetGeneratedCardCount());
				CreateOrb(playGrow: false);
			}
		}
	}

	private bool IsCurrentAttack(Node2D attackOrb, ulong version)
	{
		if (!_isClearing && _isAttacking && version == _lifecycleVersion && _attackOrb == attackOrb)
		{
			return IsValid((GodotObject?)(object)attackOrb);
		}
		return false;
	}

	private void DismissAllVisuals()
	{
		if (!IsValid((GodotObject?)(object)_orb) && !IsValid((GodotObject?)(object)_attackOrb))
		{
			_orb = null;
			_attackOrb = null;
			_isAttacking = false;
			return;
		}
		_lifecycleVersion++;
		_isAttacking = false;
		Tween? scaleTween = _scaleTween;
		if (scaleTween != null)
		{
			scaleTween.Kill();
		}
		Tween? flightTween = _flightTween;
		if (flightTween != null)
		{
			flightTween.Kill();
		}
		_scaleTween = null;
		_flightTween = null;
		Node2D orb = _orb;
		Node2D attackOrb = _attackOrb;
		_orb = null;
		_attackOrb = null;
		FadeAndFree(orb);
		if (attackOrb != orb)
		{
			FadeAndFree(attackOrb);
		}
	}

	private static void FadeAndFree(Node2D? node)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		Node2D node2 = node;
		if (IsValid((GodotObject?)(object)node2))
		{
			Tween obj = ((Node)node2).CreateTween().SetParallel(true);
			obj.SetTrans((TransitionType)4);
			obj.SetEase((EaseType)0);
			obj.TweenProperty((GodotObject)(object)node2, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), 0.20000000298023224);
			obj.TweenProperty((GodotObject)(object)node2, NodePath.op_Implicit("scale"), Variant.op_Implicit(Vector2.Zero), 0.20000000298023224);
			obj.Finished += delegate
			{
				QueueFreeIfValid((Node?)(object)node2);
			};
		}
	}

	private void UpdateWander(float delta)
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		if (IsValid((GodotObject?)(object)_orb) && !_isAttacking)
		{
			_wanderRetargetTimer -= delta;
			if (_wanderRetargetTimer <= 0f)
			{
				PickNewWanderTarget();
			}
			float num = 1f - Mathf.Exp(-0.85f * delta);
			_wanderOffset = ((Vector2)(ref _wanderOffset)).Lerp(_wanderTarget, num);
			if (((Vector2)(ref _wanderOffset)).Length() > 55f)
			{
				_wanderOffset = ((Vector2)(ref _wanderOffset)).Normalized() * 55f;
			}
			_orb.Position = GetAnchorPosition() + _wanderOffset * GetPlayerVisualScale();
		}
	}

	private void PickNewWanderTarget()
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		float num = _rng.RandfRange(0f, (float)Math.PI * 2f);
		float num2 = Mathf.Sqrt(_rng.Randf()) * 55f;
		_wanderTarget = Vector2.FromAngle(num) * num2;
		_wanderRetargetTimer = _rng.RandfRange(2f, 3.5f);
	}

	private Vector2 GetAnchorPosition()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if (_playerNode == null)
		{
			return Vector2.Zero;
		}
		return new Vector2(VFXUtil.IsCharacterFacingRight(_playerNode.Entity) ? (0f - BaseAnchorOffset.X) : BaseAnchorOffset.X, BaseAnchorOffset.Y) * GetPlayerVisualScale();
	}

	private float GetPlayerVisualScale()
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		NCreature? playerNode = _playerNode;
		if (((playerNode != null) ? playerNode.Visuals : null) == null)
		{
			return 1f;
		}
		return Mathf.Max(0.01f, Mathf.Abs(((Node2D)_playerNode.Visuals).Scale.X));
	}

	private static bool IsValid(GodotObject? value)
	{
		if (value != null)
		{
			return GodotObject.IsInstanceValid(value);
		}
		return false;
	}

	private static void QueueFreeIfValid(Node? node)
	{
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			GodotTreeExtensions.QueueFreeSafely(node);
		}
	}

	public override void _ExitTree()
	{
		ClearImmediate();
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
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0188: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0269: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0306: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Expected O, but got Unknown
		//IL_030c: Unknown result type (might be due to invalid IL or missing references)
		//IL_032e: Unknown result type (might be due to invalid IL or missing references)
		//IL_034f: Unknown result type (might be due to invalid IL or missing references)
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Expected O, but got Unknown
		//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0400: Unknown result type (might be due to invalid IL or missing references)
		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
		//IL_0433: Expected O, but got Unknown
		//IL_042e: Unknown result type (might be due to invalid IL or missing references)
		//IL_044f: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b3: Expected O, but got Unknown
		//IL_04ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_04da: Unknown result type (might be due to invalid IL or missing references)
		//IL_0500: Unknown result type (might be due to invalid IL or missing references)
		//IL_0509: Unknown result type (might be due to invalid IL or missing references)
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0558: Unknown result type (might be due to invalid IL or missing references)
		//IL_0563: Expected O, but got Unknown
		//IL_055e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0569: Unknown result type (might be due to invalid IL or missing references)
		//IL_058f: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0612: Unknown result type (might be due to invalid IL or missing references)
		//IL_061b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0641: Unknown result type (might be due to invalid IL or missing references)
		//IL_064a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0670: Unknown result type (might be due to invalid IL or missing references)
		//IL_0699: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a4: Expected O, but got Unknown
		//IL_069f: Unknown result type (might be due to invalid IL or missing references)
		//IL_06aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0704: Expected O, but got Unknown
		//IL_06ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_070a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0730: Unknown result type (might be due to invalid IL or missing references)
		//IL_0739: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(24)
		{
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ClearImmediate, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CalculateScale, new PropertyInfo((Type)3, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("generatedCardCount"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SubscribeToPiles, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UnsubscribeFromPiles, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnPileContentsChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SyncPossessionDeferred, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.HasActiveSupermassive, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetGeneratedCardCount, new PropertyInfo((Type)2, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.CreateOrb, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("playGrow"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.TweenOrbToLatestScale, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("duration"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)1, StringName.op_Implicit("ensureVisible"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.PlayOneShotAnimation, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("parent"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false),
				new PropertyInfo((Type)21, StringName.op_Implicit("animation"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)2, StringName.op_Implicit("zIndex"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.OnFlightFinished, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("attackOrb"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false),
				new PropertyInfo((Type)2, StringName.op_Implicit("version"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.FinishAttack, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("attackOrb"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false),
				new PropertyInfo((Type)2, StringName.op_Implicit("version"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.IsCurrentAttack, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("attackOrb"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false),
				new PropertyInfo((Type)2, StringName.op_Implicit("version"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.DismissAllVisuals, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.FadeAndFree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("node"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateWander, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.PickNewWanderTarget, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetAnchorPosition, new PropertyInfo((Type)5, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.GetPlayerVisualScale, new PropertyInfo((Type)3, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.IsValid, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("value"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Object"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.QueueFreeIfValid, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("node"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Unknown result type (might be due to invalid IL or missing references)
		//IL_0275: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0321: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_0386: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0401: Unknown result type (might be due to invalid IL or missing references)
		//IL_0406: Unknown result type (might be due to invalid IL or missing references)
		//IL_043b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0440: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0471: Unknown result type (might be due to invalid IL or missing references)
		//IL_0496: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ClearImmediate && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			ClearImmediate();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.CalculateScale && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			float num = CalculateScale(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<float>(ref num);
			return true;
		}
		if ((ref method) == MethodName.SubscribeToPiles && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			SubscribeToPiles();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UnsubscribeFromPiles && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			UnsubscribeFromPiles();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnPileContentsChanged && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnPileContentsChanged();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SyncPossessionDeferred && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			SyncPossessionDeferred();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.HasActiveSupermassive && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			bool flag = HasActiveSupermassive();
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.GetGeneratedCardCount && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			int generatedCardCount = GetGeneratedCardCount();
			ret = VariantUtils.CreateFrom<int>(ref generatedCardCount);
			return true;
		}
		if ((ref method) == MethodName.CreateOrb && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			CreateOrb(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.TweenOrbToLatestScale && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			TweenOrbToLatestScale(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.PlayOneShotAnimation && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			PlayOneShotAnimation(VariantUtils.ConvertTo<Node2D>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<StringName>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnFlightFinished && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			OnFlightFinished(VariantUtils.ConvertTo<Node2D>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<ulong>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.FinishAttack && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			FinishAttack(VariantUtils.ConvertTo<Node2D>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<ulong>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.IsCurrentAttack && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			bool flag2 = IsCurrentAttack(VariantUtils.ConvertTo<Node2D>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<ulong>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = VariantUtils.CreateFrom<bool>(ref flag2);
			return true;
		}
		if ((ref method) == MethodName.DismissAllVisuals && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			DismissAllVisuals();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.FadeAndFree && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			FadeAndFree(VariantUtils.ConvertTo<Node2D>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateWander && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateWander(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.PickNewWanderTarget && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			PickNewWanderTarget();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.GetAnchorPosition && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Vector2 anchorPosition = GetAnchorPosition();
			ret = VariantUtils.CreateFrom<Vector2>(ref anchorPosition);
			return true;
		}
		if ((ref method) == MethodName.GetPlayerVisualScale && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			float playerVisualScale = GetPlayerVisualScale();
			ret = VariantUtils.CreateFrom<float>(ref playerVisualScale);
			return true;
		}
		if ((ref method) == MethodName.IsValid && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag3 = IsValid(VariantUtils.ConvertTo<GodotObject>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag3);
			return true;
		}
		if ((ref method) == MethodName.QueueFreeIfValid && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			QueueFreeIfValid(VariantUtils.ConvertTo<Node>(ref ((NativeVariantPtrArgs)(ref args))[0]));
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
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.CalculateScale && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			float num = CalculateScale(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<float>(ref num);
			return true;
		}
		if ((ref method) == MethodName.FadeAndFree && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			FadeAndFree(VariantUtils.ConvertTo<Node2D>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.IsValid && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			bool flag = IsValid(VariantUtils.ConvertTo<GodotObject>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<bool>(ref flag);
			return true;
		}
		if ((ref method) == MethodName.QueueFreeIfValid && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			QueueFreeIfValid(VariantUtils.ConvertTo<Node>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.ClearImmediate)
		{
			return true;
		}
		if ((ref method) == MethodName.CalculateScale)
		{
			return true;
		}
		if ((ref method) == MethodName.SubscribeToPiles)
		{
			return true;
		}
		if ((ref method) == MethodName.UnsubscribeFromPiles)
		{
			return true;
		}
		if ((ref method) == MethodName.OnPileContentsChanged)
		{
			return true;
		}
		if ((ref method) == MethodName.SyncPossessionDeferred)
		{
			return true;
		}
		if ((ref method) == MethodName.HasActiveSupermassive)
		{
			return true;
		}
		if ((ref method) == MethodName.GetGeneratedCardCount)
		{
			return true;
		}
		if ((ref method) == MethodName.CreateOrb)
		{
			return true;
		}
		if ((ref method) == MethodName.TweenOrbToLatestScale)
		{
			return true;
		}
		if ((ref method) == MethodName.PlayOneShotAnimation)
		{
			return true;
		}
		if ((ref method) == MethodName.OnFlightFinished)
		{
			return true;
		}
		if ((ref method) == MethodName.FinishAttack)
		{
			return true;
		}
		if ((ref method) == MethodName.IsCurrentAttack)
		{
			return true;
		}
		if ((ref method) == MethodName.DismissAllVisuals)
		{
			return true;
		}
		if ((ref method) == MethodName.FadeAndFree)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateWander)
		{
			return true;
		}
		if ((ref method) == MethodName.PickNewWanderTarget)
		{
			return true;
		}
		if ((ref method) == MethodName.GetAnchorPosition)
		{
			return true;
		}
		if ((ref method) == MethodName.GetPlayerVisualScale)
		{
			return true;
		}
		if ((ref method) == MethodName.IsValid)
		{
			return true;
		}
		if ((ref method) == MethodName.QueueFreeIfValid)
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
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._playerNode)
		{
			_playerNode = VariantUtils.ConvertTo<NCreature>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._orb)
		{
			_orb = VariantUtils.ConvertTo<Node2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._attackOrb)
		{
			_attackOrb = VariantUtils.ConvertTo<Node2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._scaleTween)
		{
			_scaleTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._flightTween)
		{
			_flightTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._wanderOffset)
		{
			_wanderOffset = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._wanderTarget)
		{
			_wanderTarget = VariantUtils.ConvertTo<Vector2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._wanderRetargetTimer)
		{
			_wanderRetargetTimer = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._latestScale)
		{
			_latestScale = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._lifecycleVersion)
		{
			_lifecycleVersion = VariantUtils.ConvertTo<ulong>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isAttacking)
		{
			_isAttacking = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._possessionSyncQueued)
		{
			_possessionSyncQueued = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isClearing)
		{
			_isClearing = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.HasReadyOrb)
		{
			bool hasReadyOrb = HasReadyOrb;
			value = VariantUtils.CreateFrom<bool>(ref hasReadyOrb);
			return true;
		}
		if ((ref name) == PropertyName._rng)
		{
			value = VariantUtils.CreateFrom<RandomNumberGenerator>(ref _rng);
			return true;
		}
		if ((ref name) == PropertyName._playerNode)
		{
			value = VariantUtils.CreateFrom<NCreature>(ref _playerNode);
			return true;
		}
		if ((ref name) == PropertyName._orb)
		{
			value = VariantUtils.CreateFrom<Node2D>(ref _orb);
			return true;
		}
		if ((ref name) == PropertyName._attackOrb)
		{
			value = VariantUtils.CreateFrom<Node2D>(ref _attackOrb);
			return true;
		}
		if ((ref name) == PropertyName._scaleTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _scaleTween);
			return true;
		}
		if ((ref name) == PropertyName._flightTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _flightTween);
			return true;
		}
		if ((ref name) == PropertyName._wanderOffset)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref _wanderOffset);
			return true;
		}
		if ((ref name) == PropertyName._wanderTarget)
		{
			value = VariantUtils.CreateFrom<Vector2>(ref _wanderTarget);
			return true;
		}
		if ((ref name) == PropertyName._wanderRetargetTimer)
		{
			value = VariantUtils.CreateFrom<float>(ref _wanderRetargetTimer);
			return true;
		}
		if ((ref name) == PropertyName._latestScale)
		{
			value = VariantUtils.CreateFrom<float>(ref _latestScale);
			return true;
		}
		if ((ref name) == PropertyName._lifecycleVersion)
		{
			value = VariantUtils.CreateFrom<ulong>(ref _lifecycleVersion);
			return true;
		}
		if ((ref name) == PropertyName._isAttacking)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isAttacking);
			return true;
		}
		if ((ref name) == PropertyName._possessionSyncQueued)
		{
			value = VariantUtils.CreateFrom<bool>(ref _possessionSyncQueued);
			return true;
		}
		if ((ref name) == PropertyName._isClearing)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isClearing);
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
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._rng, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._playerNode, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._orb, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._attackOrb, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._scaleTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._flightTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName._wanderOffset, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName._wanderTarget, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._wanderRetargetTimer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._latestScale, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._lifecycleVersion, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isAttacking, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._possessionSyncQueued, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isClearing, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.HasReadyOrb, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._playerNode, Variant.From<NCreature>(ref _playerNode));
		info.AddProperty(PropertyName._orb, Variant.From<Node2D>(ref _orb));
		info.AddProperty(PropertyName._attackOrb, Variant.From<Node2D>(ref _attackOrb));
		info.AddProperty(PropertyName._scaleTween, Variant.From<Tween>(ref _scaleTween));
		info.AddProperty(PropertyName._flightTween, Variant.From<Tween>(ref _flightTween));
		info.AddProperty(PropertyName._wanderOffset, Variant.From<Vector2>(ref _wanderOffset));
		info.AddProperty(PropertyName._wanderTarget, Variant.From<Vector2>(ref _wanderTarget));
		info.AddProperty(PropertyName._wanderRetargetTimer, Variant.From<float>(ref _wanderRetargetTimer));
		info.AddProperty(PropertyName._latestScale, Variant.From<float>(ref _latestScale));
		info.AddProperty(PropertyName._lifecycleVersion, Variant.From<ulong>(ref _lifecycleVersion));
		info.AddProperty(PropertyName._isAttacking, Variant.From<bool>(ref _isAttacking));
		info.AddProperty(PropertyName._possessionSyncQueued, Variant.From<bool>(ref _possessionSyncQueued));
		info.AddProperty(PropertyName._isClearing, Variant.From<bool>(ref _isClearing));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._playerNode, ref val))
		{
			_playerNode = ((Variant)(ref val)).As<NCreature>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._orb, ref val2))
		{
			_orb = ((Variant)(ref val2)).As<Node2D>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._attackOrb, ref val3))
		{
			_attackOrb = ((Variant)(ref val3)).As<Node2D>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._scaleTween, ref val4))
		{
			_scaleTween = ((Variant)(ref val4)).As<Tween>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._flightTween, ref val5))
		{
			_flightTween = ((Variant)(ref val5)).As<Tween>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._wanderOffset, ref val6))
		{
			_wanderOffset = ((Variant)(ref val6)).As<Vector2>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._wanderTarget, ref val7))
		{
			_wanderTarget = ((Variant)(ref val7)).As<Vector2>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._wanderRetargetTimer, ref val8))
		{
			_wanderRetargetTimer = ((Variant)(ref val8)).As<float>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._latestScale, ref val9))
		{
			_latestScale = ((Variant)(ref val9)).As<float>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._lifecycleVersion, ref val10))
		{
			_lifecycleVersion = ((Variant)(ref val10)).As<ulong>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName._isAttacking, ref val11))
		{
			_isAttacking = ((Variant)(ref val11)).As<bool>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName._possessionSyncQueued, ref val12))
		{
			_possessionSyncQueued = ((Variant)(ref val12)).As<bool>();
		}
		Variant val13 = default(Variant);
		if (info.TryGetProperty(PropertyName._isClearing, ref val13))
		{
			_isClearing = ((Variant)(ref val13)).As<bool>();
		}
	}
}
