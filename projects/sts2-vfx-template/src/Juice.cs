// ============================================================================
//  Juice —— 「打击感」工具箱
//  来源：反编译自「动作与特效」(MeleeAttack) v1.0.31
//        HitStop.cs / SimpleTeleportPatch.cs / TargetCenter.cs / VfxFacingFix.cs
//
//  这些东西跟美术无关，纯粹是"手感"。特效再漂亮，没有这几样也不爽。
//
//  内容：
//    1. EasedProgress / StretchFactor  —— 冲刺缓动 + 拉伸形变（squash & stretch）
//    2. DashWithStretch / ArcMove      —— 位移（直线 / 弧线）
//    3. HitStop                        —— 打击停顿（命中瞬间冻帧）
//    4. TargetCenterOf                 —— 用 Hitbox 算目标中心（比 GlobalPosition 准）
//    5. ApplyFacingToVisual            —— 朝向翻转 + 粒子修正
//    6. WaitTweenOrTimeout             —— tween 超时保护
// ============================================================================

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace YourMod.Scripts.Vfx;

public static class Juice
{
    // =================================================================
    //  1. 两个纯数学函数（★ 最值钱，跟引擎无关，直接抄）
    // =================================================================

    /// <summary>
    /// 分段缓动：前 62.5% 的时间只走 20% 路程（慢慢蓄力起步），
    /// 后 37.5% 的时间冲完剩下 80%。冲刺/出拳"快"的感觉主要来自这里。
    /// </summary>
    public static float EasedProgress(float t, float splitTime = 0.625f, float splitProgress = 0.2f)
    {
        t = Mathf.Clamp(t, 0f, 1f);
        if (t <= splitTime) return t / splitTime * splitProgress;
        return splitProgress + (t - splitTime) / (1f - splitTime) * (1f - splitProgress);
    }

    /// <summary>
    /// 拉伸倍率：0 → 峰值（默认 4.5 倍，发生在进度 70% 处）→ 回落。
    /// squash & stretch —— 角色像橡皮筋被"拽长"再弹回来。
    /// 只用在 X 轴（运动方向），Y 轴不变。
    /// </summary>
    public static float StretchFactor(float progress, float peakStrength = 4.5f,
                                      float exponent = 4f, float peakProgress = 0.7f)
    {
        progress = Mathf.Clamp(progress, 0f, 1f);
        if (progress <= peakProgress)
        {
            float n = progress / peakProgress;
            return 1f + (peakStrength - 0.8f) * Mathf.Pow(n, exponent);   // 加速拉起
        }
        float m = 1f - (progress - peakProgress) / (1f - peakProgress);
        return 1f + (peakStrength - 1f) * m;                              // 线性回落
    }

    // =================================================================
    //  2. 位移
    // =================================================================

    /// <summary>
    /// 冲刺 + 拉伸形变。
    /// 调参参考（原作者调过的）：
    ///   故障机器人 Defect  : duration 0.25, peakStrength 3.0, exponent 6, peakProgress 0.75
    ///   铁甲战士 Ironclad  : duration 0.20, peakStrength 2.0, exponent 6, peakProgress 0.35
    /// </summary>
    public static async Task DashWithStretch(NCreature node, Vector2 targetPos,
        float duration = 0.2f, float peakStrength = 2f, float exponent = 6f,
        float peakProgress = 0.7f, bool applyFacing = true, Vector2? facingReferencePoint = null)
    {
        if (!IsValid(node)) return;

        Node2D visual = node.Body;
        if (!IsValid(visual)) { node.GlobalPosition = targetPos; return; }

        Vector2 dir = ResolveDirection(node, targetPos, facingReferencePoint);

        Vector2 origScale = visual.Scale;
        var absScale = new Vector2(Mathf.Abs(origScale.X), Mathf.Abs(origScale.Y));
        float sign = applyFacing ? Mathf.Sign(dir.X) : Mathf.Sign(origScale.X);
        if (Mathf.Abs(dir.X) < 0.001f) sign = 1f;
        if (applyFacing) visual.Scale = new Vector2(absScale.X * sign, absScale.Y);

        Vector2 startPos = node.GlobalPosition;

        var tween = node.CreateTween();
        tween.TweenMethod(Callable.From<float>(t =>
        {
            if (!IsValid(node) || !IsValid(visual)) return;
            float p = t / duration;
            node.GlobalPosition = startPos.Lerp(targetPos, EasedProgress(p));
            float s = StretchFactor(p, peakStrength, exponent, peakProgress);
            visual.Scale = new Vector2(absScale.X * s * sign, absScale.Y);   // ★ 只拉 X
        }), 0f, duration, duration);

        bool completed = await WaitTweenOrTimeout(tween, node, duration + 0.5f);

        // ★ 收尾：还原缩放 + 吸死到目标点（防浮点误差留偏移）
        if (completed && IsValid(node))
        {
            if (IsValid(visual)) visual.Scale = new Vector2(absScale.X * sign, absScale.Y);
            node.GlobalPosition = targetPos;
        }
    }

    /// <summary>弧形位移（抛物线），比直线更有"跳过去"的重量感</summary>
    public static async Task ArcMove(NCreature node, Vector2 targetPos, float duration = 0.2f,
                                     Vector2? facingReferencePoint = null)
    {
        if (!IsValid(node)) return;

        Vector2 startPos = node.GlobalPosition;
        float distance = startPos.DistanceTo(targetPos);
        Vector2 dir = ResolveDirection(node, targetPos, facingReferencePoint);

        if (distance < 1f) { node.GlobalPosition = targetPos; return; }

        float maxHeight = distance * 0.3f;                       // ★ 弧高按距离比例算

        Node2D visual = node.Body;
        if (IsValid(visual)) ApplyFacingToVisual(visual, dir);

        var tween = node.CreateTween();
        tween.TweenMethod(Callable.From<float>(t =>
        {
            if (!IsValid(node)) return;
            float num = Mathf.Clamp(t / duration, 0f, 1f);
            Vector2 flat = startPos.Lerp(targetPos, EasedProgress(num));
            float arch = maxHeight * (1f - Mathf.Pow((num - 0.5f) / 0.5f, 2f));   // 开口向下抛物线
            arch = Mathf.Max(arch, 0f);
            node.GlobalPosition = new Vector2(flat.X, flat.Y - arch);            // ★ Y 向上减
        }), 0f, duration, duration);

        await WaitTweenOrTimeout(tween, node, duration + 0.5f);
        if (IsValid(node)) node.GlobalPosition = targetPos;
    }

    /// <summary>静默冲刺（带淡出，适合"闪"的一下）</summary>
    public static async Task SilentDash(NCreature node, Vector2 targetPos, float duration = 0.12f)
    {
        if (!IsValid(node)) return;
        var tween = node.CreateTween();
        tween.TweenProperty(node, "global_position", targetPos, duration);
        if (await WaitTweenOrTimeout(tween, node, duration + 0.5f) && IsValid(node))
            node.GlobalPosition = targetPos;
    }

    /// <summary>静默撤退（边退边淡出，再瞬间归位并恢复透明度）</summary>
    public static async Task SilentRetreat(NCreature node, Vector2 targetPos, Vector2 homePos,
                                           float distance = 120f, float duration = 0.1f, float fadeEnd = 0.05f)
    {
        if (!IsValid(node)) return;
        Node2D visual = node.Body;
        if (!IsValid(visual)) { node.GlobalPosition = targetPos; return; }

        float originalAlpha = visual.Modulate.A;
        Vector2 dir = (homePos - targetPos).Normalized();
        if (dir.LengthSquared() < 0.001f) dir = Vector2.Right;

        var tween = node.CreateTween().SetParallel(true);
        tween.TweenProperty(node, "global_position", homePos + dir * distance, duration);
        tween.TweenProperty(visual, "modulate:a", fadeEnd, duration);

        if (await WaitTweenOrTimeout(tween, node, duration + 0.5f) && IsValid(node) && IsValid(visual))
        {
            node.GlobalPosition = targetPos;
            visual.Modulate = new Color(visual.Modulate, originalAlpha);      // ★ 恢复透明度
        }
    }

    // =================================================================
    //  3. 打击停顿（命中瞬间把整个游戏冻住 0.1~0.2 秒）
    // =================================================================

    /// <summary>
    /// ⚠️ 三个坑：
    ///   1. TimeScale = 0 之后普通 Timer 也会停 → 必须用挂钟 Time.GetTicksMsec() 计时
    ///   2. TimeScale = 0 时 ProcessFrame 信号照常发 → 靠它逐帧轮询
    ///   3. 还原写在 finally，且还原后复查一次（别处可能动过 TimeScale）
    /// </summary>
    public static async Task ApplyHitStop(float duration = 0.2f)
    {
        if (Engine.TimeScale < 0.01f) return;                      // 已经在顿了

        float originalTimeScale = (float)Engine.TimeScale;
        if (Engine.GetMainLoop() is not SceneTree sceneTree || sceneTree.Root == null) return;

        var guardNode = new Node { Name = "HitStopGuard" };
        sceneTree.Root.AddChild(guardNode, false, Node.InternalMode.Disabled);

        bool finished = false;
        long startMs = (long)Time.GetTicksMsec();
        long durationMs = (long)(duration * 1000f);

        void OnFrame()
        {
            if (finished) return;
            if (Math.Abs(Engine.TimeScale) > 0.001f) Engine.TimeScale = 0.0;    // 保持冻住
            if ((long)Time.GetTicksMsec() - startMs >= durationMs) finished = true;
        }
        sceneTree.ProcessFrame += OnFrame;

        try
        {
            Engine.TimeScale = 0.0;

            // 兜底保险：万一 OnFrame 循环没退出，用 Timer 强制结束
            // （Timer 此刻是不走的，等 TimeScale 恢复后才会到点 —— 所以只是保险，不是主机制）
            var timer = new Timer { WaitTime = duration + 0.1f, OneShot = true };
            timer.Timeout += () => { if (!finished) { finished = true; if (IsValid(guardNode)) guardNode.QueueFree(); } };
            guardNode.AddChild(timer, false, Node.InternalMode.Disabled);
            timer.Start();

            while (!finished)
                await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
        }
        finally
        {
            sceneTree.ProcessFrame -= OnFrame;
            if (IsValid(guardNode)) guardNode.QueueFree();
            Engine.TimeScale = originalTimeScale;
            await sceneTree.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);
            if (Math.Abs(Engine.TimeScale - originalTimeScale) > 0.001f)
                Engine.TimeScale = originalTimeScale;                        // 复查
        }
    }

    // =================================================================
    //  4. 目标中心（用 Hitbox，比 GlobalPosition 准）
    // =================================================================

    /// <summary>拿"打在身上"的那个点。GlobalPosition 是脚底，Hitbox 中心才是判定框中心。</summary>
    public static Vector2 TargetCenterOf(NCreature targetNode, float offsetY = 0f)
    {
        if (!IsValid(targetNode)) return Vector2.Zero;

        if (targetNode.Hitbox != null)
            return targetNode.Hitbox.GlobalPosition + targetNode.Hitbox.Size * 0.5f + new Vector2(0f, offsetY);

        return targetNode.GlobalPosition + new Vector2(0f, offsetY);
    }

    // =================================================================
    //  5. 朝向翻转（★ 记得先处理粒子）
    // =================================================================

    /// <summary>按方向把视觉节点翻正/翻反（只动 Scale.X 的符号）</summary>
    public static void ApplyFacingToVisual(Node2D visual, Vector2 direction)
    {
        if (!IsValid(visual)) return;
        float sign = Mathf.Sign(direction.X);
        if (Mathf.Abs(direction.X) < 0.001f) sign = 1f;
        visual.Scale = new Vector2(Mathf.Abs(visual.Scale.X) * sign, visual.Scale.Y);
    }

    /// <summary>
    /// 翻转一个特效节点，**并且修正粒子方向**。
    ///
    /// ⚠️ 真坑：LocalCoords = false 的粒子速度是世界坐标的，
    ///    你把父节点 Scale.X 翻负，粒子照样往原方向飞 → 看起来"反了"。
    ///    必须先切局部坐标 → 翻转 → 重播。
    /// </summary>
    public static void FlipVfxNode(Node2D outerNode)
    {
        if (!IsValid(outerNode)) return;
        PrepareParticlesForFlip(outerNode);
        outerNode.Scale = new Vector2(-outerNode.Scale.X, outerNode.Scale.Y);
        RestartAllParticles(outerNode);
    }

    private static void PrepareParticlesForFlip(Node parent)
    {
        foreach (var child in parent.GetChildren(false))
        {
            if (child is GpuParticles2D g && !g.LocalCoords) g.LocalCoords = true;
            else if (child is CpuParticles2D c && !c.LocalCoords) c.LocalCoords = true;
            PrepareParticlesForFlip(child);
        }
    }

    private static void RestartAllParticles(Node parent)
    {
        foreach (var child in parent.GetChildren(false))
        {
            if (child is GpuParticles2D g) g.Restart();
            else if (child is CpuParticles2D c) c.Restart();
            RestartAllParticles(child);
        }
    }

    // =================================================================
    //  6. tween 超时保护
    // =================================================================

    /// <summary>
    /// 等 tween 结束，但最多等 timeoutSeconds。
    /// 返回 false = 超时 or 节点已失效 —— 这时候就别再对节点做操作了。
    /// </summary>
    public static async Task<bool> WaitTweenOrTimeout(Tween tween, Node node, float timeoutSeconds)
    {
        if (tween == null || node == null || !GodotObject.IsInstanceValid(node)) return false;

        var tcs = new TaskCompletionSource<bool>();
        tween.Finished += () => tcs.TrySetResult(true);

        var tree = node.GetTree();
        if (tree != null && timeoutSeconds > 0f)
            tree.CreateTimer(timeoutSeconds, true, false, false).Timeout += () => tcs.TrySetResult(false);
        else
            _ = Task.Delay((int)(timeoutSeconds * 1000f)).ContinueWith(_ => tcs.TrySetResult(false));

        await tcs.Task;
        return GodotObject.IsInstanceValid(node) && node.IsInsideTree();
    }

    // =================================================================
    //  内部小工具
    // =================================================================

    private static bool IsValid(GodotObject? o) => o != null && GodotObject.IsInstanceValid(o);

    /// <summary>算位移方向。有"朝向参考点"时按它算（不管人在哪），否则按起点→终点算。</summary>
    private static Vector2 ResolveDirection(Node2D node, Vector2 targetPos, Vector2? facingReferencePoint)
    {
        Vector2 dir = facingReferencePoint.HasValue
            ? (facingReferencePoint.Value - targetPos).Normalized()
            : (targetPos - node.GlobalPosition).Normalized();
        return dir.LengthSquared() < 0.001f ? Vector2.Right : dir;
    }
}
