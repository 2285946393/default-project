using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeleeAttack;

public static class FastAttackPatches
{
	[HarmonyPatch]
	private static class FastCmdWaitPatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(Cmd), "Wait", new Type[3]
			{
				typeof(float),
				typeof(CancellationToken),
				typeof(bool)
			}, (Type[])null);
		}

		private static bool Prefix(float seconds, CancellationToken cancelToken, bool ignoreCombatEnd, ref Task __result)
		{
			if (!SettingsUI.IsFastModeEnabled())
			{
				bool flag = true;
				bool value = IsDamageRelatedContext();
				bool value2 = false;
				try
				{
					value2 = !ignoreCombatEnd && CombatManager.Instance.IsEnding;
				}
				catch
				{
				}
				GD.Print($"[FastWait] sec={seconds:F3} fast=OFF ctx={value} ignoreEnd={ignoreCombatEnd} isEnding={value2} | {FlagDebugString()} → 原版放行(极速关闭)");
				return true;
			}
			bool value3 = IsDamageRelatedContext();
			bool flag2 = false;
			try
			{
				flag2 = !ignoreCombatEnd && CombatManager.Instance.IsEnding;
			}
			catch
			{
			}
			if (seconds <= 0.02f)
			{
				bool flag3 = true;
				GD.Print($"[FastWait] sec={seconds:F3} fast=ON ctx={value3} ignoreEnd={ignoreCombatEnd} isEnding={flag2} | {FlagDebugString()} → 原版放行(<=floor {0.02f})");
				return true;
			}
			if (flag2)
			{
				bool flag4 = true;
				GD.Print($"[FastWait] sec={seconds:F3} fast=ON ctx={value3} ignoreEnd={ignoreCombatEnd} isEnding=True | {FlagDebugString()} → 原版放行(战斗结束)");
				return true;
			}
			float num = Mathf.Min(seconds * 0.1f, 0.05f);
			if (num < 0.02f)
			{
				num = 0.02f;
			}
			bool flag5 = true;
			GD.Print($"[FastWait] sec={seconds:F3} fast=ON ctx={value3} ignoreEnd={ignoreCombatEnd} isEnding={flag2} | {FlagDebugString()} → 压缩到 {num:F3}");
			__result = FastWaitAsync(seconds, cancelToken);
			return false;
		}

		private static async Task FastWaitAsync(float seconds, CancellationToken cancelToken)
		{
			float compressed = Mathf.Min(seconds * 0.1f, 0.05f);
			if (compressed < 0.02f)
			{
				compressed = 0.02f;
			}
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree tree = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (tree == null)
			{
				try
				{
					await Task.Delay((int)(compressed * 1000f), cancelToken);
					return;
				}
				catch (OperationCanceledException)
				{
					return;
				}
			}
			SceneTreeTimer timer = tree.CreateTimer((double)compressed, true, false, false);
			try
			{
				await SignalAwaiterExtensions.ToTask(((GodotObject)timer).ToSignal((GodotObject)(object)timer, SignalName.Timeout)).WaitAsync(cancelToken);
			}
			catch (OperationCanceledException)
			{
			}
		}
	}

	[HarmonyPatch(typeof(Cmd), "CustomScaledWait")]
	private static class FastCustomScaledWaitPatch
	{
		private static bool Prefix(float fastSeconds, float standardSeconds, bool ignoreCombatEnd, CancellationToken cancellationToken, ref Task __result)
		{
			if (!SettingsUI.IsFastModeEnabled())
			{
				return true;
			}
			if (!ignoreCombatEnd && CombatManager.Instance.IsEnding)
			{
				return true;
			}
			float num = ((standardSeconds > 0f) ? standardSeconds : fastSeconds);
			if (num <= 0.02f)
			{
				return true;
			}
			float num2 = Mathf.Min(num * 0.1f, 0.05f);
			if (num2 < 0.02f)
			{
				num2 = 0.02f;
			}
			bool flag = true;
			GD.Print($"[FastCustomScaledWait] fast={fastSeconds:F3} std={standardSeconds:F3} → 压缩到 {num2:F3}");
			__result = Cmd.Wait(num2, cancellationToken, ignoreCombatEnd);
			return false;
		}
	}

	[HarmonyPatch(typeof(SkittishPower), "AfterAttack")]
	private static class SkittishPowerAfterAttackFastPatch
	{
		private const string RetractSfx = "event:/sfx/enemy/enemy_attacks/phantasmal_gardeners/phantasmal_gardeners_retract";

		private static readonly MethodInfo _setHasGainedBlock = AccessTools.PropertySetter(typeof(SkittishPower), "HasGainedBlockThisTurn");

		public static bool Prefix(SkittishPower __instance, PlayerChoiceContext choiceContext, AttackCommand command, ref Task __result)
		{
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			if (__instance == null || command == null)
			{
				return true;
			}
			if (!(command.ModelSource is CardModel))
			{
				return true;
			}
			if (__instance.HasGainedBlockThisTurn)
			{
				__result = Task.CompletedTask;
				return false;
			}
			if (!((Enum)command.DamageProps).HasFlag((Enum)(object)(ValueProp)8))
			{
				__result = Task.CompletedTask;
				return false;
			}
			DamageResult val = command.Results.SelectMany((List<DamageResult> r) => r).FirstOrDefault((Func<DamageResult, bool>)((DamageResult r) => r.Receiver == ((PowerModel)__instance).Owner));
			if (val == null || val.UnblockedDamage == 0)
			{
				__result = Task.CompletedTask;
				return false;
			}
			Creature owner = ((PowerModel)__instance).Owner;
			int amount = ((PowerModel)__instance).Amount;
			try
			{
				SfxCmd.Play("event:/sfx/enemy/enemy_attacks/phantasmal_gardeners/phantasmal_gardeners_retract", 1f);
				NCombatRoom instance = NCombatRoom.Instance;
				NCreature val2 = ((instance != null) ? instance.GetCreatureNode(owner) : null);
				if (val2 != null)
				{
					val2.SetAnimationTrigger("BlockStart");
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr("[MeleeAttack] Skittish SFX/Anim 异常: " + ex.Message);
			}
			__result = GainBlockAwaited(__instance, owner, amount);
			return false;
		}

		private static async Task GainBlockAwaited(SkittishPower power, Creature owner, int amount)
		{
			try
			{
				await CreatureCmd.GainBlock(owner, (decimal)amount, (ValueProp)4, (CardPlay)null, true);
				_setHasGainedBlock?.Invoke(power, new object[1] { true });
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				GD.PrintErr("[MeleeAttack] Skittish GainBlock 异常: " + ex.Message);
			}
		}
	}

	private const float WaitFloor = 0.02f;

	private const float CompressRatio = 0.1f;

	private const float CompressCap = 0.05f;

	private const bool DebugLog = true;

	private static bool IsDamageRelatedContext()
	{
		try
		{
			if (SimpleTeleportPatch.IsAttackInProgress.Value)
			{
				return true;
			}
			if (SimpleTeleportPatch._currentAttack.Value != null)
			{
				return true;
			}
			if (SimpleTeleportPatch._isProxyRunning.Value)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static string FlagDebugString()
	{
		bool value = false;
		bool value2 = false;
		bool value3 = false;
		try
		{
			value = SimpleTeleportPatch.IsAttackInProgress.Value;
		}
		catch
		{
		}
		try
		{
			value2 = SimpleTeleportPatch._currentAttack.Value != null;
		}
		catch
		{
		}
		try
		{
			value3 = SimpleTeleportPatch._isProxyRunning.Value;
		}
		catch
		{
		}
		return $"inAtk={value}, hasCmd={value2}, proxy={value3}";
	}
}
