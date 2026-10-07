using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace MeleeAttack;

public static class AttackTimeTerminator
{
	private static bool _isEnding;

	private static bool _isMonitoringTurnEnd;

	private static Creature _monitoredPlayerCreature;

	public static event Action OnTurnEnd;

	public static async Task EndAsync()
	{
		if (_isEnding)
		{
			return;
		}
		_isEnding = true;
		try
		{
			AttackTimeState.HardCleanup();
			DistortionFilter.FadeOut();
			await BiasedCognitionEffects.ApplyBlurOnlyAsync();
			SimpleTeleportPatch.ClearAllSnapshots();
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			GD.PrintErr("[AttackTimeTerminator] EndAsync 异常: " + ex.Message);
		}
		finally
		{
			_isEnding = false;
		}
	}

	public static void TriggerAttackTime(Creature playerCreature)
	{
		AttackTimeState.Trigger(playerCreature);
		FilterTrigger.StartFilter(playerCreature);
		AttackTimeState.OnEnd -= OnAttackTimeEnded;
		AttackTimeState.OnEnd += OnAttackTimeEnded;
	}

	private static void OnAttackTimeEnded()
	{
		Task.Run(async delegate
		{
			await Cmd.Wait(0.1f, true);
			await EndAsync();
		});
	}

	public static void StartTurnEndMonitoring(Creature playerCreature)
	{
		if (!_isMonitoringTurnEnd && playerCreature != null)
		{
			_monitoredPlayerCreature = playerCreature;
			_isMonitoringTurnEnd = true;
			MonitorTurnEndAsync();
		}
	}

	private static async Task MonitorTurnEndAsync()
	{
		while (_isMonitoringTurnEnd)
		{
			if (IsPlayerTurnEnded())
			{
				AttackTimeTerminator.OnTurnEnd?.Invoke();
				await BiasedCognitionEffects.ApplyBlurOnlyAsync();
				SimpleTeleportPatch.ClearAllSnapshots();
				_isMonitoringTurnEnd = false;
				break;
			}
			await Cmd.Wait(0.05f, true);
		}
	}

	private static bool IsPlayerTurnEnded()
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Invalid comparison between Unknown and I4
		if (_monitoredPlayerCreature == null)
		{
			return false;
		}
		try
		{
			Player player = _monitoredPlayerCreature.Player;
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
			return false;
		}
	}
}
