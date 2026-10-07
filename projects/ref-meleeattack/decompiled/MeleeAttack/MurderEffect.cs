using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace MeleeAttack;

[HarmonyPatch]
public static class MurderEffect
{
	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim", new Type[]
	{
		typeof(Creature),
		typeof(string),
		typeof(float)
	})]
	[HarmonyPriority(600)]
	private static class TriggerAnimPatch
	{
		public static bool Prefix(Creature creature, string triggerName, float waitTime, ref Task __result)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Invalid comparison between Unknown and I4
			if (SimpleTeleportPatch._isProxyRunning.Value)
			{
				return true;
			}
			if (creature == null || (int)creature.Side != 1)
			{
				return true;
			}
			if (string.IsNullOrEmpty(triggerName))
			{
				return true;
			}
			if (!triggerName.StartsWith("Attack", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			string creatureId = SimpleTeleportPatch.GetCreatureId(creature);
			if (!string.Equals(creatureId, "Silent", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			AttackCommand value = SimpleTeleportPatch._currentAttack.Value;
			if (value == null)
			{
				return true;
			}
			AbstractModel modelSource = value.ModelSource;
			CardModel val = (CardModel)(object)((modelSource is CardModel) ? modelSource : null);
			if (val == null)
			{
				return true;
			}
			if (!((AbstractModel)val).Id.Entry.Equals("MURDER", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (!SettingsUI.IsMurderEnabled())
			{
				return true;
			}
			if (!SimpleTeleportPatch.TryGetCreatureNode(creature, out var _, out var node) || node == null)
			{
				return true;
			}
			if (SimpleTeleportPatch.HasAnyAttackState(node))
			{
				int value2 = SimpleTeleportPatch._currentHitIndex.Value;
				__result = SimpleTeleportPatch.RunTeleportAttack(creature, triggerName, waitTime, SimpleTeleportPatch.GetAliveTargets(value), value2);
				return false;
			}
			__result = RunMurderSequence(creature, triggerName, waitTime, value, node);
			return false;
		}

		private static async Task RunMurderSequence(Creature creature, string triggerName, float waitTime, AttackCommand cmd, NCreature attackerNode)
		{
			NCombatRoom room = NCombatRoom.Instance;
			if (room == null)
			{
				return;
			}
			AttackCommand savedAttack = SimpleTeleportPatch._currentAttack.Value;
			int savedHitIndex = SimpleTeleportPatch._currentHitIndex.Value;
			SimpleTeleportPatch._currentAttack.Value = cmd;
			SimpleTeleportPatch._currentHitIndex.Value = 0;
			try
			{
				if (SettingsUI.IsFastModeEnabled())
				{
					await SimpleTeleportPatch.RunTeleportAttack(creature, triggerName, waitTime, SimpleTeleportPatch.GetAliveTargets(cmd), 0);
					return;
				}
				TaskCompletionSource<bool> lockTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
				_unstoppableLocks[attackerNode] = lockTcs;
				List<Creature> allTargets = new List<Creature>();
				try
				{
					AttackBlackoutEffect.TriggerBlackout(0.2f, 1000f, 0.2f);
					Task.Delay(1000).ContinueWith((Task _) => AttackBlackoutEffect.EndBlackout(0.2f));
					CreatureCmd.TriggerAnim(creature, "Shiv", 0.1f);
					GhostSpawner.ClearAllGhostsFor(attackerNode);
					StartGhostTrail(creature);
					NCreature targetNode = null;
					try
					{
						MethodInfo method = typeof(AttackCommand).GetMethod("GetPossibleTargets", BindingFlags.Instance | BindingFlags.NonPublic);
						if (method != null && method.Invoke(cmd, null) is IReadOnlyList<Creature> result)
						{
							allTargets.AddRange(result);
							if (result.Count > 0)
							{
								Creature firstTarget = result[0];
								if (firstTarget != null && firstTarget.IsAlive)
								{
									targetNode = room.GetCreatureNode(firstTarget);
								}
							}
						}
					}
					catch
					{
					}
					if (targetNode != null)
					{
						Task.Run(async delegate
						{
							await Task.Delay(300);
							SpawnRedShivAndEffects(attackerNode, targetNode, room);
						});
					}
					await Task.Delay(700);
					StopGhostTrail(creature);
				}
				finally
				{
					if (_unstoppableLocks.TryRemove(attackerNode, out var removed))
					{
						removed.TrySetResult(result: true);
					}
				}
				SimpleTeleportPatch._currentAttack.Value = cmd;
				SimpleTeleportPatch._currentHitIndex.Value = 0;
				await SimpleTeleportPatch.RunTeleportAttack(creature, triggerName, waitTime, allTargets, 0);
			}
			finally
			{
				SimpleTeleportPatch._currentAttack.Value = savedAttack;
				SimpleTeleportPatch._currentHitIndex.Value = savedHitIndex;
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class CombatResetPatch
	{
		public static void Postfix()
		{
			foreach (KeyValuePair<Creature, CancellationTokenSource> item in (IEnumerable<KeyValuePair<Creature, CancellationTokenSource>>)_ghostCts)
			{
				try
				{
					item.Value?.Cancel();
				}
				catch
				{
				}
			}
			_ghostCts.Clear();
			Engine.TimeScale = 1.0;
			foreach (KeyValuePair<NCreature, TaskCompletionSource<bool>> unstoppableLock in _unstoppableLocks)
			{
				try
				{
					unstoppableLock.Value?.TrySetResult(result: true);
				}
				catch
				{
				}
			}
			_unstoppableLocks.Clear();
		}
	}

	private const float WAIT_BEFORE_FADE = 0.7f;

	private const float SHIV_ANIM_DURATION = 0.1f;

	private const float GHOST_INTERVAL = 0.05f;

	private const float GHOST_FADE_DURATION = 0.3f;

	private const float GHOST_DURATION = 1f;

	private const float SHAKE_AMPLITUDE = 6f;

	private const float SHAKE_STEP = 0.03f;

	private const int SHAKE_LOOPS = 6;

	private const int SLOW_DOWN_DELAY_MS = 100;

	private static PackedScene _murder01VfxScene;

	private static readonly ConditionalWeakTable<Creature, CancellationTokenSource> _ghostCts = new ConditionalWeakTable<Creature, CancellationTokenSource>();

	private static double _originalTimeScaleForMurder = 1.0;

	private static readonly ConcurrentDictionary<NCreature, TaskCompletionSource<bool>> _unstoppableLocks = new ConcurrentDictionary<NCreature, TaskCompletionSource<bool>>();

	public static async Task WaitIfUnstoppableAsync(NCreature node)
	{
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		long startTicks = (long)Time.GetTicksMsec();
		TaskCompletionSource<bool> tcs;
		while (_unstoppableLocks.TryGetValue(node, out tcs) && (long)Time.GetTicksMsec() - startTicks <= 15000 && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			try
			{
				await tcs.Task;
			}
			catch
			{
			}
		}
	}

	private static CancellationTokenSource StartGhostTrail(Creature creature)
	{
		if (_ghostCts.TryGetValue(creature, out var value))
		{
			try
			{
				value?.Cancel();
			}
			catch
			{
			}
			_ghostCts.Remove(creature);
		}
		CancellationTokenSource cts = new CancellationTokenSource();
		_ghostCts.Add(creature, cts);
		Task.Run(async delegate
		{
			try
			{
				for (float elapsed = 0f; elapsed < 1f; elapsed += 0.05f)
				{
					if (cts.Token.IsCancellationRequested)
					{
						break;
					}
					NCreature node = creature.GetCreatureNode();
					if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
					{
						await Task.Delay(50, cts.Token);
					}
					else
					{
						await ((GodotObject)node).ToSignal((GodotObject)(object)((Node)node).GetTree(), StringName.op_Implicit("process_frame"));
						GhostSpawner.SpawnGhostFadeOnlyUnlimited(node, 0.5f, 0.3f, (Color?)new Color(0.75f, 0.85f, 1f, 0.5f));
					}
					await Task.Delay(50, cts.Token);
				}
			}
			catch (OperationCanceledException)
			{
			}
			catch (ObjectDisposedException)
			{
			}
			catch (Exception ex4)
			{
				Exception ex = ex4;
				GD.PrintErr("[MurderEffect] 残影生成异常: " + ex.Message);
			}
			finally
			{
				try
				{
					cts.Dispose();
				}
				catch
				{
				}
			}
		}, cts.Token);
		return cts;
	}

	private static void StopGhostTrail(Creature creature)
	{
		if (_ghostCts.TryGetValue(creature, out var value))
		{
			try
			{
				value?.Cancel();
			}
			catch
			{
			}
			_ghostCts.Remove(creature);
		}
	}

	private static void SpawnRedShivAndEffects(NCreature attackerNode, NCreature targetNode, NCombatRoom room)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		if (attackerNode == null || targetNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)targetNode) || room == null)
		{
			return;
		}
		Vector2 vfxSpawnPosition = attackerNode.VfxSpawnPosition;
		Vector2 vfxSpawnPosition2 = targetNode.VfxSpawnPosition;
		NShivThrowVfx val = NShivThrowVfx.Create(vfxSpawnPosition, vfxSpawnPosition2, Colors.Red);
		if (val != null)
		{
			((CanvasItem)val).ZIndex = 1;
			((CanvasItem)val).ZAsRelative = false;
			Node val2 = (Node)(((object)room.BackCombatVfxContainer) ?? ((object)room));
			((GodotObject)val2).CallDeferred(StringName.op_Implicit("add_child"), (Variant[])(object)new Variant[1] { Variant.op_Implicit((GodotObject)(object)val) });
		}
		else
		{
			GD.PrintErr("[MurderEffect] NShivThrowVfx.Create 返回 null");
		}
		_originalTimeScaleForMurder = Engine.TimeScale;
		Task.Run(async delegate
		{
			await Task.Delay(100);
			Engine.TimeScale = 0.5;
		});
		Task.Run(async delegate
		{
			await Task.Delay(200);
			try
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)targetNode))
				{
					await ((GodotObject)targetNode).ToSignal((GodotObject)(object)((Node)targetNode).GetTree(), StringName.op_Implicit("process_frame"));
					if (GodotObject.IsInstanceValid((GodotObject)(object)targetNode))
					{
						染色.Apply(targetNode, Colors.Red);
						if (_murder01VfxScene == null)
						{
							_murder01VfxScene = GD.Load<PackedScene>("res://scenes/谋杀01.tscn");
							if (_murder01VfxScene == null)
							{
								GD.PrintErr("[MurderEffect] 无法加载谋杀01特效场景");
								return;
							}
						}
						Node2D vfx = _murder01VfxScene.Instantiate<Node2D>((GenEditState)0);
						if (vfx != null)
						{
							((Node)targetNode).AddChild((Node)(object)vfx, false, (InternalMode)0);
							Vector2 localPos = targetNode.VfxSpawnPosition - ((Control)targetNode).GlobalPosition;
							vfx.Position = localPos;
							((CanvasItem)vfx).ZIndex = 10;
							((CanvasItem)vfx).ZAsRelative = true;
							Vector2 basePos = vfx.Position;
							Tween shakeTween = ((Node)vfx).CreateTween();
							shakeTween.SetLoops(6);
							shakeTween.SetTrans((TransitionType)0);
							shakeTween.TweenProperty((GodotObject)(object)vfx, NodePath.op_Implicit("position"), Variant.op_Implicit(basePos + new Vector2(6f, 0f)), 0.029999999329447746);
							shakeTween.TweenProperty((GodotObject)(object)vfx, NodePath.op_Implicit("position"), Variant.op_Implicit(basePos + new Vector2(-6f, 0f)), 0.029999999329447746);
							shakeTween.TweenProperty((GodotObject)(object)vfx, NodePath.op_Implicit("position"), Variant.op_Implicit(basePos), 0.029999999329447746);
							shakeTween.TweenCallback(Callable.From((Action)delegate
							{
								//IL_0021: Unknown result type (might be due to invalid IL or missing references)
								if (GodotObject.IsInstanceValid((GodotObject)(object)vfx))
								{
									vfx.Position = basePos;
								}
							}));
							Timer timer = new Timer();
							timer.WaitTime = 0.5;
							timer.OneShot = true;
							timer.Timeout += delegate
							{
								if (GodotObject.IsInstanceValid((GodotObject)(object)vfx))
								{
									((Node)vfx).QueueFree();
								}
							};
							((Node)vfx).AddChild((Node)(object)timer, false, (InternalMode)0);
							timer.Start(-1.0);
							Engine.TimeScale = _originalTimeScaleForMurder;
						}
						else
						{
							GD.PrintErr("[MurderEffect] 实例化谋杀01特效失败");
							Engine.TimeScale = _originalTimeScaleForMurder;
						}
					}
					else
					{
						Engine.TimeScale = _originalTimeScaleForMurder;
					}
				}
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				GD.PrintErr("[MurderEffect] 延迟染色/特效异常: " + ex.Message);
				Engine.TimeScale = _originalTimeScaleForMurder;
			}
		});
	}
}
