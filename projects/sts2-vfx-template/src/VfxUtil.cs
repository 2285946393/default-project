// ============================================================================
//  VfxUtil —— 特效工具箱
//  来源：反编译自万象辉星(RegentFX) v0.5.1 Scripts/VFXUtil.cs，做了语义化改名 + 中文注释
//  用法：整个文件丢进你的 mod，改 namespace 即可
// ============================================================================

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using HarmonyLib;

namespace YourMod.Scripts;   // ← 改这里

public static class VfxUtil
{
    // ------------------------------------------------------------------
    // ★ 核心：mod 私有的场景缓存
    //
    // 为什么不能用本体的 PreloadManager.Cache？
    //   它在换房间时会调 UnloadAssets()，把 mod 的资源一起清掉。
    //   表现就是：第一次进战斗特效正常，第二次"特效没了"。
    //
    // 由 ModEntry.Init() 在启动时填满（见 ModEntry.cs）。
    // ------------------------------------------------------------------
    public static readonly ConcurrentDictionary<string, PackedScene> SceneCache = new();

    // ------------------------------------------------------------------
    // 生成特效节点：先查自己的缓存，再退到本体缓存
    // ------------------------------------------------------------------
    public static Node2D GenVfx(string scenePath)
    {
        if (SceneCache.TryGetValue(scenePath, out var cached))
            return cached.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
        return PreloadManager.Cache.GetScene(scenePath)
                                  .Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
    }

    public static T GenVfx<T>(string scenePath) where T : Node2D
    {
        if (SceneCache.TryGetValue(scenePath, out var cached))
            return cached.Instantiate<T>(PackedScene.GenEditState.Disabled);
        return PreloadManager.Cache.GetScene(scenePath)
                                  .Instantiate<T>(PackedScene.GenEditState.Disabled);
    }

    // ------------------------------------------------------------------
    // ★★ 最值钱的一个方法：把场景「拉伸 + 旋转」对准到世界的两个点
    //
    // 场景约定（重要）：
    //   特效场景按「目标在原点 (0,0)、起点在名为 StartPos 的空 Node2D」来画。
    //   调用时传：
    //     sceneStart —— 场景里 StartPos 节点的局部坐标（例：(-362,-1)）
    //     Vector2.Zero —— 场景原点 = 目标
    //     worldStart  —— 世界上出招者的位置
    //     worldEnd    —— 世界上目标的位置
    //
    // 原理：
    //   场景向量 a = sceneStart - Zero，世界向量 b = worldStart - worldEnd
    //   缩放 = |b| / |a|，旋转 = b.Angle() - a.Angle()
    //   → 变换后 StartPos 恰好落在 worldStart，原点恰好落在 worldEnd。
    //   （已验算，首尾吻合）
    //
    // 注意：反编译出来的形参名是错的（把世界起点叫成 sceneStartPos），这里已改对。
    // ------------------------------------------------------------------
    public static void FitVfx(this Node2D node,
        Vector2 sceneStart, Vector2 sceneEnd,
        Vector2 worldStart, Vector2 worldEnd)
    {
        Vector2 a = sceneStart - sceneEnd;
        Vector2 b = worldStart - worldEnd;
        if (a.Length() < 0.001f || b.Length() < 0.001f) return;   // 防除零

        node.Rotation = b.Angle() - a.Angle();
        node.Scale    = Vector2.One * (b.Length() / a.Length());
    }

    // ------------------------------------------------------------------
    // 最简播放：挂到战斗特效层 + 指定时间后自动销毁（不做对准）
    // 适合定点特效（爆点、光环、星光）
    // ------------------------------------------------------------------
    public static Node2D? PlaySimple(string scenePath, Vector2 position, float lifetime = 2f)
        => PlaySimpleInternal(scenePath, position, lifetime, front: true);

    /// <summary>垫在角色后面播（比如地面烟雾、背景光晕）</summary>
    public static Node2D? PlaySimpleBack(string scenePath, Vector2 position, float lifetime = 2f)
        => PlaySimpleInternal(scenePath, position, lifetime, front: false);

    private static Node2D? PlaySimpleInternal(string scenePath, Vector2 position, float lifetime, bool front)
    {
        if (TestMode.IsOn || NCombatRoom.Instance == null) return null;

        var node = GenVfx(scenePath);
        var container = front ? NCombatRoom.Instance.CombatVfxContainer
                              : NCombatRoom.Instance.BackCombatVfxContainer;
        container.AddChildSafely(node);
        node.GlobalPosition = position;

        ClearAfter(node, lifetime);
        return node;
    }

    /// <summary>等一会儿再销毁。★ 用 QueueFreeSafely，别用原生 QueueFree</summary>
    public static async void ClearAfter(Node2D? node, float delay)
    {
        await Wait(delay);
        if (node != null && GodotObject.IsInstanceValid(node))
            node.QueueFreeSafely();
    }

    // ------------------------------------------------------------------
    // 等待（自带"快进模式 / 战斗已结束"保护）
    // ------------------------------------------------------------------
    public static Task Wait(float seconds, bool ignoreCombatEnd = false)
        => Wait(seconds, default, ignoreCombatEnd);

    public static async Task Wait(float seconds, CancellationToken token, bool ignoreCombatEnd = false)
    {
        if (NonInteractiveMode.IsActive) return;
        if (seconds <= 0f) return;
        if (NGame.Instance == null) return;
        if (SaveManager.Instance.PrefsSave.FastMode == 3) return;                 // 3 = 瞬间模式
        if (!ignoreCombatEnd && CombatManager.Instance.IsEnding) return;          // 战斗结束了就别等了

        var timer = (SceneTreeTimer)Engine.GetMainLoop().CreateTimer(seconds, true, false, false);
        var tcs = new TaskCompletionSource();
        timer.Timeout += () => tcs.TrySetResult();
        if (token.CanBeCanceled) token.Register(() => tcs.TrySetCanceled(token));
        await tcs.Task;
    }

    // ------------------------------------------------------------------
    // 小工具
    // ------------------------------------------------------------------
    public static bool RandChance(float percentage) => GD.Randf() < percentage;

    public static Vector2 RandVec2(float radius)
        => new((float)GD.RandRange(-1.0, 1.0) * radius, (float)GD.RandRange(-1.0, 1.0) * radius);

    /// <summary>角色是朝右吗（用 Body.Scale.X 判方向）</summary>
    public static bool IsFacingRight(Creature creature)
    {
        var node = NCombatRoom.Instance?.GetCreatureNode(creature);
        var body = node?.Body as Node2D;
        return body == null || body.Scale.X > 0f;
    }

    // ------------------------------------------------------------------
    // 拿"敌方半场的中心" —— 群体卡特效往那儿打
    //
    // 本体 VfxCmd.GetSideCenter(CombatSide, CombatState) 未必是 public，
    // 所以万象辉星用 Harmony 的 Traverse 反射调。
    // 如果你的游戏版本里它是 public，删掉反射直接调更省事：
    //     return VfxCmd.GetSideCenter(CombatSide.Enemy, card.CombatState);
    // ------------------------------------------------------------------
    public static Vector2? GetCombatSidePos(CardModel card)
    {
        var state = Traverse.Create(card).Property("CombatState").GetValue();
        if (state == null) return null;

        var v = Traverse.Create(typeof(VfxCmd))
                        .Method("GetSideCenter", new object[] { CombatSide.Enemy, state })
                        .GetValue();
        return v is Vector2 vec ? vec : null;
    }

    /// <summary>整棵子树里的所有 GPUParticles2D 重新播放一遍</summary>
    public static void ReplayAllParticles(Node2D? node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node)) return;
        if (node is GpuParticles2D p) p.Restart();
        foreach (var child in node.GetChildren(false))
            if (child is Node2D n2) ReplayAllParticles(n2);
    }

    /// <summary>整棵子树等比缩放（注意会把 GpuParticles2D 设为局部坐标）</summary>
    public static void ScaleAllParticles(Node2D? node, float scale)
    {
        if (node == null || !GodotObject.IsInstanceValid(node)) return;
        if (node is GpuParticles2D p) { p.LocalCoords = true; p.Scale *= scale; }
        foreach (var child in node.GetChildren(false))
            if (child is Node2D n2) ScaleAllParticles(n2, scale);
    }

    /// <summary>屏幕抖动</summary>
    public static async void ShakeAfter(float delay, ShakeStrength strength, ShakeDuration duration, float degAngle = -1f)
    {
        await Wait(delay);
        NGame.Instance?.ScreenShake(strength, duration, degAngle);
    }
}
