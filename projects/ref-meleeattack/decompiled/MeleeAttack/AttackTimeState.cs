using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class AttackTimeState
{
	private static bool _isActive;

	private static readonly HashSet<NCreature> _frozen = new HashSet<NCreature>();

	private static bool _stopRequested;

	private static Creature _playerCreature;

	public static bool IsActive => _isActive;

	public static event Action OnEnd;

	public static void Trigger(Creature playerCreature)
	{
		if (!_isActive)
		{
			_playerCreature = playerCreature;
			_isActive = true;
			_stopRequested = false;
			RunController();
		}
	}

	private static async Task RunController()
	{
		NCombatRoom room = NCombatRoom.Instance;
		if (room == null || !GodotObject.IsInstanceValid((GodotObject)(object)room))
		{
			_isActive = false;
			AttackTimeState.OnEnd?.Invoke();
			return;
		}
		while (_isActive && !_stopRequested)
		{
			if (IsPlayerTurnEnded())
			{
				AttackTimeState.OnEnd?.Invoke();
				_stopRequested = true;
				break;
			}
			FreezeEnemies(room);
			await Cmd.Wait(0.016f, true);
		}
		RestoreEnemies();
		_isActive = false;
	}

	private static bool IsPlayerTurnEnded()
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Invalid comparison between Unknown and I4
		if (_playerCreature == null)
		{
			return false;
		}
		try
		{
			Player player = _playerCreature.Player;
			if (player == null)
			{
				return false;
			}
			PlayerCombatState playerCombatState = player.PlayerCombatState;
			if (playerCombatState == null)
			{
				return false;
			}
			return (int)playerCombatState.Phase != 3;
		}
		catch
		{
			try
			{
				CombatManager instance = CombatManager.Instance;
				if (instance == null)
				{
					return false;
				}
				return instance.IsOverOrEnding;
			}
			catch
			{
				return false;
			}
		}
	}

	private static void FreezeEnemies(NCombatRoom room)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Invalid comparison between Unknown and I4
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		foreach (NCreature creatureNode in room.CreatureNodes)
		{
			if (creatureNode != null)
			{
				Creature entity = creatureNode.Entity;
				if (entity != null && (int)entity.Side == 2 && entity.IsAlive && creatureNode.HasSpineAnimation && _frozen.Add(creatureNode))
				{
					SpineAnimationAccess spineAnimation = creatureNode.SpineAnimation;
					((SpineAnimationAccess)(ref spineAnimation)).SetTimeScale(0.04f);
				}
			}
		}
	}

	private static void RestoreEnemies()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		foreach (NCreature item in _frozen)
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)item) && item.HasSpineAnimation)
			{
				SpineAnimationAccess spineAnimation = item.SpineAnimation;
				((SpineAnimationAccess)(ref spineAnimation)).SetTimeScale(1f);
			}
		}
		_frozen.Clear();
	}

	public static void Stop()
	{
		if (_isActive)
		{
			_stopRequested = true;
		}
	}

	public static void HardCleanup()
	{
		_isActive = false;
		_stopRequested = false;
		RestoreEnemies();
	}

	public static void SetPlayerAnimationSpeed(NCreature playerNode, float speed)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if (playerNode != null)
		{
			if (playerNode.HasSpineAnimation)
			{
				SpineAnimationAccess spineAnimation = playerNode.SpineAnimation;
				((SpineAnimationAccess)(ref spineAnimation)).SetTimeScale(speed);
			}
			else
			{
				GD.PrintErr("[AttackTimeState] 玩家没有 SpineAnimation，无法设置速度");
			}
		}
	}
}
