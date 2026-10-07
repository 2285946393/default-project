// ============================================================================
//  PresentationFlow —— 「表现层工程学」
//  来源：反编译自「More Ironclad Animations」(ShieldOnly) v1.9.57
//
//  一句话：特效崩了、慢了，都不能影响游戏。
//
//  这是最容易忽略、但后果最严重的一块：
//  我们自己的特效代码经常直接 await 一堆 Godot 调用，
//  一旦中间抛异常，就会把**卡牌结算**卡死 —— 玩家看到的是"游戏卡住了"。
//
//  三条规矩：
//    1. 表现层的异常必须自己吃掉（SafelyAsync）
//    2. 表现层和原逻辑并行跑，不让游戏等特效（AlongsideAsync）
//    3. 放行动作写在 finally，无论成败都执行（release）
// ============================================================================

using System;
using System.Threading;
using System.Threading.Tasks;

namespace YourMod.Scripts.Vfx;

public static class PresentationFlow
{
    /// <summary>
    /// 让「表现层」和「原游戏逻辑」并行跑：游戏不被特效拖住。
    ///
    /// 执行顺序：
    ///   1. 先启动 visual（不 await）
    ///   2. await original() —— 原逻辑走完，游戏照常推进
    ///   3. await visual    —— 再等视觉收尾
    ///   4. finally release() —— 一定会放行
    /// </summary>
    public static async Task AlongsideAsync(
        Func<Task> visual, Func<Task> original,
        Action release, Action<Exception>? failed = null)
    {
        Task presentation = SafelyAsync(visual, failed);
        try
        {
            await original();
            await presentation;
        }
        finally
        {
            release();
            await presentation;
        }
    }

    /// <summary>
    /// 在原逻辑前后各插一段表现（前段不保证一定跑完 —— 但不会抛）。
    /// 适合"出牌前闪一下、出牌后收一下"。
    /// </summary>
    public static async Task AroundAsync(
        Func<Task> before, Func<Task> original, Func<Task> after,
        Action<Exception>? failed = null)
    {
        await SafelyAsync(before, failed);
        await original();
        await SafelyAsync(after, failed);
    }

    /// <summary>
    /// ★ 表现层的异常绝不能往上冒 —— 只记日志。
    /// 你在特效里写的每一段 await 链条，都应该经过这个包装。
    /// </summary>
    public static async Task SafelyAsync(Func<Task> visual, Action<Exception>? failed = null)
    {
        try
        {
            await visual();
        }
        catch (Exception e)
        {
            failed?.Invoke(e);
            ModEntry.Log("[PresentationFlow] 表现层出错（已吞掉，不影响游戏）：" + e.Message);
        }
    }

    /// <summary>同步版（给不返回 Task 的表现用）</summary>
    public static void Safely(Action visual, Action<Exception>? failed = null)
    {
        try
        {
            visual();
        }
        catch (Exception e)
        {
            failed?.Invoke(e);
            ModEntry.Log("[PresentationFlow] 表现层出错（已吞掉）：" + e.Message);
        }
    }
}

// ============================================================================
//  PresentationPreloader —— 按需预热 + 可取消 + 代际号
//
//  为什么要"代际号"（generation）：
//    预热是个异步活（读文件、建纹理、组装 mesh）。
//    如果预热跑到一半，玩家已经换角色/退出战斗了，
//    旧任务醒来后必须**知道自己过期了**，直接退出，不能往新状态里塞东西。
//    比单纯的 CancellationToken 多一层保险。
// ============================================================================
public static class PresentationPreloader
{
    private static CancellationTokenSource? _pending;
    private static int _generation;
    private static bool _ready;

    public static bool Ready => _ready;
    public static Task Completion { get; private set; } = Task.CompletedTask;

    /// <summary>预热入口。条件不满足就什么都不做（不浪费内存）。</summary>
    public static void Prepare(Func<bool> shouldWarmup, Func<CancellationToken, int, Task> warmup)
    {
        if (_ready || _pending != null) return;
        if (!shouldWarmup()) return;

        _pending = new CancellationTokenSource();
        Completion = RunAsync(_pending, _generation, warmup);
    }

    private static async Task RunAsync(CancellationTokenSource cts, int generation, Func<CancellationToken, int, Task> warmup)
    {
        try
        {
            await warmup(cts.Token, generation);
            if (generation == _generation) _ready = true;        // ★ 只有代际没变才算成功
        }
        catch (OperationCanceledException) { /* 正常取消 */ }
        catch (Exception e)
        {
            ModEntry.Log("[Preloader] 预热失败：" + e.Message);
        }
        finally
        {
            if (generation == _generation) _pending = null;
        }
    }

    /// <summary>换角色 / 换场景 / 关 mod 时调 —— 让在跑的预热作废</summary>
    public static void Invalidate()
    {
        _generation++;              // ★ 代际 +1，旧任务醒来会发现对不上，自己退出
        _pending?.Cancel();
        _pending?.Dispose();
        _pending = null;
        _ready = false;
    }
}
