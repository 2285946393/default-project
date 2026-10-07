// ============================================================================
//  CardVfxPlayer —— 通用播放流程
//  来源：反编译自万象辉星(RegentFX) v0.5.1 Scripts/Vfx/Cards/CardVfxUtil.cs
//
//  一次完整的「打一张牌 → 从出牌者射向目标」：
//    取节点 → 找 StartPos → 拉伸对准 → 挪到目标 → 挂特效层 → 音效 → 定时销毁
// ============================================================================

using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;          // ★ 必须：AddChildSafely / QueueFreeSafely 在这里
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace YourMod.Scripts.Vfx;

public static class CardVfxPlayer
{
    /// <summary>单体指向：从出牌者射向某个目标</summary>
    public static async Task PlayTargeted(CardFX config, Creature owner, Creature target, string logTag = "Vfx")
    {
        var ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (ownerNode == null || targetNode == null)
        {
            ModEntry.Log($"[{logTag}] 拿不到出牌者/目标的节点，特效跳过");
            return;
        }

        Vector2 startPos = ownerNode.GlobalPosition + config.TargetOffset;
        Vector2 endPos = targetNode.VfxSpawnPosition;
        await Play(config, startPos, endPos, logTag);
    }

    /// <summary>群体：从出牌者射向"敌方半场的中心"</summary>
    public static async Task PlayAoe(CardFX config, Creature owner, CardModel card, string logTag = "Vfx")
    {
        var ownerNode = NCombatRoom.Instance?.GetCreatureNode(owner);
        if (ownerNode == null) return;

        Vector2 startPos = ownerNode.GlobalPosition + config.TargetOffset;
        var sidePos = VfxUtil.GetCombatSidePos(card);       // 见文末：用反射拿本体私有 API
        if (sidePos.HasValue) await Play(config, startPos, sidePos.Value, logTag);
    }

    // =================================================================
    //  ★ 核心流程
    // =================================================================
    private static async Task Play(CardFX config, Vector2 startPos, Vector2 targetPos, string logTag)
    {
        if (TestMode.IsOn) return;

        if (string.IsNullOrEmpty(config.VfxScenePath))
        {
            ModEntry.Log($"[{logTag}] 没有配 VfxScenePath");
            return;
        }

        try
        {
            // 1) 取节点（走 mod 私有缓存）
            var node = VfxUtil.GenVfx(config.VfxScenePath!);

            // 2) 找场景里的起点标记
            //    ★ 约定：指向性特效的场景必须放一个名为 StartPos 的空 Node2D
            var startMarker = node.FindChild("StartPos", recursive: true, owned: true) as Node2D;
            if (startMarker == null)
            {
                ModEntry.Log($"[{logTag}] 场景里没有 StartPos 节点，无法对准");
                return;
            }

            // 3) 拉伸 + 旋转，让 StartPos 对齐出招者、原点对齐目标
            node.FitVfx(startMarker.GlobalPosition, Vector2.Zero, startPos, targetPos);

            // 4) 整体挪到目标
            node.GlobalPosition = targetPos;

            // 5) 挂到战斗特效层（ZIndex 0，见 ZIndexPatch）
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(node);

            // 6) 命中音效
            if (!string.IsNullOrEmpty(config.HitSfxPath)) Sfx.Play(config.HitSfxPath!);

            // 7) 定时销毁（★ 不做的话特效会堆在场上）
            VfxUtil.ClearAfter(node, config.VfxClearDelay);

            // 8) 可选：闪屏
            if (config.HasExposureEffect)
            {
                try
                {
                    WorldEnvironmentUtil.TweenExposure(config.ExposurePeak, config.ExposureInDuration);
                    await VfxUtil.Wait(config.ExposureInDuration + 0.05f);
                }
                finally
                {
                    // ★ 用 finally 兜底，保证一定会弹回默认，不然画面会一直被过曝
                    WorldEnvironmentUtil.TweenExposure(1f, config.ExposureOutDuration);
                }
            }
            else
            {
                await VfxUtil.Wait(0.15f);
            }

            // 9) 可选：第二段音效
            if (!string.IsNullOrEmpty(config.SecondarySfxPath)) Sfx.Play(config.SecondarySfxPath!);
        }
        catch (Exception ex)
        {
            ModEntry.Log($"[{logTag}] 播特效出错：{ex.Message}");
        }
    }
}

// =====================================================================
//  补：GetCombatSidePos —— 拿"敌方半场的中心"
//
//  本体 VfxCmd 里有 GetSideCenter(CombatSide, CombatState) 但未必是 public，
//  万象辉星用的是 Harmony 的 Traverse 反射。如果你的游戏版本里它是 public，
//  直接调更省事：
//
//      public static Vector2? GetCombatSidePos(CardModel card)
//          => VfxCmd.GetSideCenter(CombatSide.Enemy, card.CombatState);
//
//  不是 public 的话照抄反射版：
//
//      var state = Traverse.Create(card).Property("CombatState").GetValue();
//      if (state == null) return null;
//      var v = Traverse.Create(typeof(VfxCmd)).Method("GetSideCenter",
//                  new object[] { CombatSide.Enemy, state }).GetValue();
//      return v is Vector2 vec ? vec : null;
// =====================================================================
