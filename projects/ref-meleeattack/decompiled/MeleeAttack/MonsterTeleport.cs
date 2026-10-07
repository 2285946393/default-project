using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class MonsterTeleport
{
	private const float MONSTER_DASH_DURATION = 0.2f;

	private const float MONSTER_STRETCH_PEAK = 2f;

	private const float MONSTER_STRETCH_EXP = 5f;

	private const float MONSTER_STRETCH_PEAK_PROGRESS = 0.5f;

	public static async Task RunMonsterTeleportAttack(Creature attacker, string triggerName, float waitTime, List<Creature> targets, int hitIndex)
	{
		object obj;
		if (attacker == null)
		{
			obj = null;
		}
		else
		{
			MonsterModel monster = attacker.Monster;
			if (monster == null)
			{
				obj = null;
			}
			else
			{
				ModelId id = ((AbstractModel)monster).Id;
				obj = ((id != null) ? id.Entry : null);
			}
		}
		if (obj == null)
		{
			obj = ((attacker != null) ? attacker.Name : null) ?? "unknown";
		}
		string monsterId = (string)obj;
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(90, 6);
		defaultInterpolatedStringHandler.AppendLiteral("[MeleeDebug][MonsterTP] ENTER monsterId='");
		defaultInterpolatedStringHandler.AppendFormatted(monsterId);
		defaultInterpolatedStringHandler.AppendLiteral("' name='");
		defaultInterpolatedStringHandler.AppendFormatted((attacker != null) ? attacker.Name : null);
		defaultInterpolatedStringHandler.AppendLiteral("' trigger='");
		defaultInterpolatedStringHandler.AppendFormatted(triggerName);
		defaultInterpolatedStringHandler.AppendLiteral("' wait=");
		defaultInterpolatedStringHandler.AppendFormatted(waitTime);
		defaultInterpolatedStringHandler.AppendLiteral(" hitIndex=");
		defaultInterpolatedStringHandler.AppendFormatted(hitIndex);
		defaultInterpolatedStringHandler.AppendLiteral(" targetCount=");
		defaultInterpolatedStringHandler.AppendFormatted(targets?.Count ?? 0);
		GD.Print(defaultInterpolatedStringHandler.ToStringAndClear());
		if (!SettingsUI.IsMonsterGlobalEnabled())
		{
			GD.Print("[MeleeDebug][MonsterTP]   -> 全局怪物闪现开关关闭，走原动画");
			await PlayOriginalAnimation(attacker, triggerName, waitTime);
			return;
		}
		GD.Print("[MeleeDebug][MonsterTP]   全局怪物闪现开关=开");
		bool disabled = SimpleTeleportPatch.IsMonsterTeleportDisabled(monsterId, triggerName);
		GD.Print($"[MeleeDebug][MonsterTP]   IsMonsterTeleportDisabled={disabled}");
		if (disabled)
		{
			GD.Print($"[MeleeDebug][MonsterTP]   -> 被禁用列表/白名单拦截 (monsterId='{monsterId}', trigger='{triggerName}')");
			await PlayOriginalAnimation(attacker, triggerName, waitTime);
			return;
		}
		float delay = SimpleTeleportPatch.GetMonsterDelay(monsterId, triggerName);
		float distance = SimpleTeleportPatch.GetMonsterDistance(monsterId, triggerName);
		GD.Print($"[MeleeDebug][MonsterTP]   delay={delay} distance={distance}");
		if (!SimpleTeleportPatch.TryGetCreatureNode(attacker, out var room, out var attackerNode))
		{
			GD.PrintErr("[MeleeDebug][MonsterTP]   -> TryGetCreatureNode 失败，放弃");
			return;
		}
		GD.Print("[MeleeDebug][MonsterTP]   节点获取成功");
		Node2D visualNode = attackerNode.Body;
		if (visualNode == null)
		{
			GD.PrintErr("[MeleeDebug][MonsterTP]   -> attackerNode.Body 为 null，放弃");
			return;
		}
		Vector2 currentPos = ((Control)attackerNode).GlobalPosition;
		SimpleTeleportPatch.AttackSequenceState state = SimpleTeleportPatch.GetOrCreateAttackState(attackerNode, currentPos);
		Vector2 homePos = state.LockedHomePos;
		GD.Print($"[MeleeDebug][MonsterTP]   currentPos={currentPos} homePos={homePos}");
		SimpleTeleportPatch.RaiseLayer(attackerNode);
		Xue.MakeHealthBarTransparent(attackerNode);
		NCreature playerNode = GetLocalPlayerNode(room);
		if (playerNode == null)
		{
			GD.Print("[MeleeDebug][MonsterTP]   -> 未找到玩家节点，走 fallback");
			await FallbackPlayAndReturn(attacker, triggerName, waitTime, attackerNode, homePos, delay, visualNode);
			return;
		}
		GD.Print($"[MeleeDebug][MonsterTP]   playerNode 找到 name='{((Node)playerNode).Name}' pos={((Control)playerNode).GlobalPosition}");
		Vector2 monsterPos = ((Control)attackerNode).GlobalPosition;
		Vector2 playerPos = ((Control)playerNode).GlobalPosition;
		Vector2 val = monsterPos - playerPos;
		Vector2 dir = ((Vector2)(ref val)).Normalized();
		if (((Vector2)(ref dir)).LengthSquared() < 0.001f)
		{
			dir = Vector2.Right;
		}
		Vector2 teleportPos = playerPos + dir * distance;
		float dist = ((Vector2)(ref monsterPos)).DistanceTo(teleportPos);
		GD.Print($"[MeleeDebug][MonsterTP]   monsterPos={monsterPos} playerPos={playerPos} dir={dir} teleportPos={teleportPos} 位移距离={dist}");
		if (dist < 10f)
		{
			GD.Print($"[MeleeDebug][MonsterTP]   -> 目标位置过近({dist}<10)，走 fallback");
			await FallbackPlayAndReturn(attacker, triggerName, waitTime, attackerNode, homePos, delay, visualNode);
			return;
		}
		GD.Print("[MeleeDebug][MonsterTP]   >>> 执行 PerformDashWithStretch");
		await SimpleTeleportPatch.PerformDashWithStretch(attackerNode, teleportPos, 0.2f, 2f, 5f, 0.5f, applyFacing: false);
		GD.Print("[MeleeDebug][MonsterTP]   <<< dash 完成，播放原动画");
		await PlayOriginalAnimation(attacker, triggerName, waitTime);
		GD.Print($"[MeleeDebug][MonsterTP]   <<< 原动画播放完成，启动延时归位(共{delay}s)");
		if (SimpleTeleportPatch._returnCtsTable.TryRemove(attackerNode, out var oldCts2))
		{
			oldCts2?.Cancel();
			oldCts2?.Dispose();
		}
		CancellationTokenSource cts2 = new CancellationTokenSource();
		SimpleTeleportPatch._returnCtsTable[attackerNode] = cts2;
		SimpleTeleportPatch.ReturnAfterDelay(attackerNode, homePos, delay, cts2.Token, visualNode);
		GD.Print("[MeleeDebug][MonsterTP]   归位任务已 fire-and-forget 启动，RunMonsterTeleportAttack 返回");
	}

	private static NCreature GetLocalPlayerNode(NCombatRoom room)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Invalid comparison between Unknown and I4
		if (room == null)
		{
			return null;
		}
		foreach (NCreature creatureNode in room.CreatureNodes)
		{
			if (creatureNode != null)
			{
				Creature entity = creatureNode.Entity;
				if ((int)((entity != null) ? new CombatSide?(entity.Side) : null).GetValueOrDefault() == 1 && creatureNode.Entity.IsAlive)
				{
					return creatureNode;
				}
			}
		}
		return null;
	}

	private static async Task PlayOriginalAnimation(Creature creature, string triggerName, float waitTime)
	{
		SimpleTeleportPatch._isProxyRunning.Value = true;
		try
		{
			if (SimpleTeleportPatch._originalTriggerAnim != null)
			{
				await (Task)SimpleTeleportPatch._originalTriggerAnim.Invoke(null, new object[3] { creature, triggerName, waitTime });
			}
			else
			{
				CreatureCmd.TriggerAnim(creature, triggerName, waitTime);
				await Task.Delay((int)(waitTime * 1000f));
			}
		}
		finally
		{
			SimpleTeleportPatch._isProxyRunning.Value = false;
		}
	}

	private static async Task FallbackPlayAndReturn(Creature attacker, string triggerName, float waitTime, NCreature attackerNode, Vector2 homePos, float delay, Node2D visualNode)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		GD.Print("[MeleeDebug][MonsterTP][Fallback] 走 fallback 路径（不位移，只播动画+归位）");
		await PlayOriginalAnimation(attacker, triggerName, waitTime);
		if (SimpleTeleportPatch._returnCtsTable.TryRemove(attackerNode, out var oldCts))
		{
			oldCts?.Cancel();
			oldCts?.Dispose();
		}
		CancellationTokenSource cts = new CancellationTokenSource();
		SimpleTeleportPatch._returnCtsTable[attackerNode] = cts;
		SimpleTeleportPatch.ReturnAfterDelay(attackerNode, homePos, delay, cts.Token, visualNode);
	}
}
