using System.Reflection;
using BaseLib.Config;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;
using QuestSpire.Quests;
using QuestSpire.Relics;
using QuestSpire.UI;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Core;
using STS2RitsuLib.Ui.Toast;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace QuestSpire;

[ModInitializer(nameof(Initialize))]
public partial class Entry
{
    public const string ModId = "QuestSpire";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    /// <summary>
    /// **当前这一局**的 RunState 引用（开局时记下、局结束时清空）。
    /// 用来判断某个东西是不是"这一局的"——比如本地任务册是不是上一局留下的。
    /// （`RunManager.State` 是私有的，只能自己记。）
    /// </summary>
    public static object? CurrentRunRef { get; private set; }

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        // 打补丁（我们用它补上本体没暴露的钩子，见 `Patches/`）。
        // 失败只记警告，绝不让它挡住 mod 初始化。
        //
        // ⚠️ 2026-10-08 换了架子：原来是自己 `new HarmonyLib.Harmony("local.questspire")`
        //    再逐个 `CreateClassProcessor(...).Patch()`，理由写的是
        //    「`PatchAll` 是一个失败整批失败 —— 分开打免得互相连累」。
        //    读了 RitsuLib 源码（`Patching/Core/ModPatcher.cs: PatchAll`）之后发现
        //    **那个理由不成立**：`ModPatcher.PatchAll()` 本身就是**逐补丁记录结果**的
        //    （内部 `ModPatchResult[]`，挨个 ApplyPatch），只有 `IsCritical == true`
        //    的补丁失败才 `UnpatchAll()` 回滚。
        //    所以两个补丁都标 `IsCritical => false`，就得到和"分开打"完全一样的隔离，
        //    外加拿到了两样东西：
        //      · 补丁进 RitsuLib 的**补丁诊断**（统一报告里能看出它到底生效没有）
        //      · 日志自动带 `PatchId`，将来游戏改签名时一眼看出是哪一个坏的
        try
        {
            var patcher = RitsuLibFramework.CreatePatcher(ModId, "quest-spire", "QuestSpire 补丁");
            patcher.RegisterPatch<Patches.CardAddedToDeckPatch>();
            patcher.RegisterPatch<Patches.CardRemovedFromDeckPatch>();
            if (patcher.PatchAll())
                Logger.Info("补丁已生效：加/删整局卡组 → 任务册（共 2 条，均为非严重）。");
            else
                Logger.Warn("有严重补丁没打上，详见上面的补丁日志（只影响对应功能）。");
        }
        catch (System.Exception e)
        {
            Logger.Warn($"打补丁失败（只影响「往卡组加牌 / 删牌」那批任务）: {e.Message}");
        }

        RunManager.Instance.RunStarted += OnRunStarted;
        // 兜底：只要有房间进入事件，就确认侧栏挂着（读档进局不一定触发 RunStarted）。
        RunManager.Instance.RoomEntered += EnsureUi;
        RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ =>
        {
            QuestPanel.HidePanel(Logger);
            QuestSelection.CloseOpen();
            // 局结束就断开静态引用：否则下一局开头（发册还没完成时）面板/选单读的是这一局的旧账本。
            QuestRunner.ClearLocal();
            QuestRunner.RewardSink = null;
            CurrentRunRef = null;
            // ⚠️ 联机相关的一起清（2026-10-08）：不清的话下一局开局前，
            //    `QuestPickSync` 还留着上一局的编号/本地玩家，`PlayerCount` 也还是上一局的人数
            //    —— 面板的「队友分册」、选任务的候选个数（单人 5 个 / 联机 10 个）、
            //    随身物闸（`SafeMultiplayerBoons`）都会按上一局的人数来算。
            QuestPickSync.Reset();
            QuestRunner.PlayerCount = 1;
        });

        // 把自己造的遗物的中文名写进游戏的本地化覆盖目录。
        // 必须在这里做：mod 初始化早于 LocManager 加载本地化，所以当次启动就生效。
        LocInjector.Inject(Logger);

        // 注册设置页（挂到游戏的「设置 → 模组配置」里，见 QuestSpireConfig）。
        try
        {
            ModConfigRegistry.Register(ModId, new QuestSpireConfig());
            Logger.Info("QuestSpire config registered.");
        }
        catch (System.Exception e)
        {
            Logger.Warn($"注册设置页失败（不影响玩法）: {e.Message}");
        }

        Logger.Info("QuestSpire initialized.");
    }

    /// <summary>侧栏没挂就挂上；已挂着则什么都不做。</summary>
    public static void EnsureUi() => QuestPanel.ShowPanel(Logger);

    /// <summary>屏幕角落的轻提示。失败只记日志，绝不让它把游戏搞崩。</summary>
    public static void Toast(string body, string? title = null)
    {
        try
        {
            RitsuToastService.ShowInfo(body, title);
        }
        catch (System.Exception e)
        {
            Logger.Warn($"Toast failed: {e.Message}");
        }
    }

    private static void OnRunStarted(RunState state)
    {
        CurrentRunRef = state;   // 先记下"这一局"是谁，后面所有"是不是本局的"判断都靠它
        // ⚠️ 多人局：任务册必须**每个客户端给所有玩家都发一遍**（确定性操作），
        // 绝不能「每个客户端只给自己那位发」——那样各客户端的遗物列表不一致，
        // 一进房间就会「多人数据不同步」（2026-09-23 玩家实测报的 bug，就是这里造成的）。
        // 参考游戏自己的开局逻辑（`RunManager.ApplyAscensionEffects(player)` 也是对所有玩家跑的）。
        var player = LocalPlayer(state);

        // 记下队伍人数：用来放大那些「联机里会被队友加码」的目标（见 QuestDef.ScaleWithPlayers）。
        QuestRunner.PlayerCount = state.Players.Count > 0 ? state.Players.Count : 1;

        var existing = player?.GetRelic<QuestBookRelic>();

        if (existing == null)
        {
            // 新的一局：给**所有人**发任务册（新遗物的状态本来就是空的，不用手动清），
            // 并给一次开局接任务的机会。
            QuestBoons.Reset();
            // ⚠️ 必须先把上一局的静态引用清掉：放弃一局再开新局时，面板和「接取任务」还在读上一局的账本，
            // 结果就是「新局里旧任务还在、接取接进了旧账本」（2026-09-25 玩家实测，日志已确认）。
            QuestRunner.ClearLocal();
            QuestRunner.RewardSink = null;
            _ = GrantThenArmAsync(state, player);
            Logger.Info($"Run started: quest book granted to {state.Players.Count} player(s).");
            return;
        }

        // 读档继续：进度和随身物已经随存档填进遗物（QuestData / HasBoat / HasDeadBranch），
        // 这里不重发第二个任务册、不重置进度，也不白送一次“开局选任务”（否则读档就能反复刷任务）。
        existing.MirrorBoons(); // 读档：把随身物镜像到面板用的静态表
        // ⚠️ 读档 / 快速 SL 后必须丢掉内存里的奖励对象：它们攥着**旧的 Player 实例**，
        //    一点"领取"就会被 SelectLocalReward 判成"非本地玩家" → 点了没反应、只能跳过。
        existing.ClearInMemoryRewardObjects();
        QuestRunner.BindLocal(existing);   // UI 看本地这一本（并记住它属于哪一局）
        QuestRunner.RewardSink = existing.Reward;
        EnsureUi();
        QuestSelection.CloseOpen();
        Logger.Info($"Run loaded: quest book restored, {QuestRunner.Active.Count} active quest(s), players={QuestRunner.PlayerCount}.");
    }

    /// <summary>
    /// 找出**本地玩家**：NetId 跟本机网络服务一致的那一位。
    /// 找不到（单人 / 测试环境）就退回第一位玩家。
    /// ⚠️ 以前这里是硬写 `state.Players[0]` —— 多人局里其他玩家就永远拿不到任务册。
    /// </summary>
    public static MegaCrit.Sts2.Core.Entities.Players.Player? LocalPlayer(RunState? state)
    {
        if (state == null || state.Players.Count == 0) return null;
        try
        {
            // ⚠️ 优先用 `LocalContext.NetId` —— 那是游戏自己的口径（`RunManager.Launch()` 里就是写的它），
            //    是个普通属性、**永不抛异常**。`NetService.NetId` 在「还没连上 / 已断线」时会**抛异常**
            //    （`NetClientGameService.NetId => (NetClient ?? throw ...).NetId`），只能当第二手。
            ulong? localId = MegaCrit.Sts2.Core.Context.LocalContext.NetId;
            if (!localId.HasValue)
            {
                try { localId = RunManager.Instance.NetService?.NetId; }
                catch (System.Exception e) { Logger.Warn($"拿 NetService.NetId 失败（改用 LocalContext）: {e.Message}"); }
            }

            if (localId.HasValue)
            {
                foreach (var p in state.Players)
                    if (p.NetId == localId.Value)
                    {
                        Logger.Info($"本地玩家识别：NetId={localId.Value} → 匹配到玩家 {p.NetId}（共 {state.Players.Count} 人）");
                        return p;
                    }
                Logger.Warn($"本地玩家识别失败：NetId={localId.Value} 不在对局玩家列表里"
                            + $"（{string.Join(",", state.Players.Select(x => x.NetId))}）→ 退回第一位玩家");
            }
            else
            {
                Logger.Warn("本地玩家识别失败：拿不到 NetId → 退回第一位玩家");
            }
        }
        catch (System.Exception e)
        {
            Logger.Warn($"找不到本地玩家，退回第一位玩家: {e.Message}");
        }
        return state.Players[0];
    }

    /// <summary>
    /// 多人局专用：**自动**给每位玩家分配一批任务。
    ///
    /// 为什么不能沿用「UI 里自己挑」：那是纯本地操作，各客户端挑出来的不一样，主机那份存档
    /// 也就没有别人的进度 → SL 回来任务就变（玩家反馈）。改用**同一个种子**（由玩家 NetId 推出）
    /// 抽取，每个客户端算出来完全一样 → 天然同步、存档也是全的。
    /// 单人局不这么做，保留手动挑选的乐趣。
    /// </summary>
    private static Task AutoAssignQuestsForMultiplayer(RunState state)
    {
        if (state.Players.Count <= 1) return Task.CompletedTask;

        foreach (var p in state.Players)
        {
            try
            {
                var book = p.GetRelic<QuestBookRelic>();
                if (book == null) continue;
                int seed = unchecked((int)(p.NetId ^ 0x5F3759DFu) ^ state.Players.Count);
                foreach (var def in book.State.OffersSeeded(QuestRunState.MaxActive, seed, p.Character?.GetType()))
                    book.State.TryAdd(def);
                Logger.Info($"多人局：给玩家 {p.NetId} 自动分配了 {book.State.Active.Count} 条任务。");
            }
            catch (System.Exception e)
            {
                Logger.Error($"多人局自动分配任务失败（{p.NetId}）: {e}");
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 给**所有玩家**发任务册（每个客户端都执行同一套操作 → 各客户端状态一致，不需要网络同步）。
    /// 只有本地玩家那本会驱动本机的进度与面板（守卫在 QuestBookRelic.IsLocalOwner）。
    /// </summary>
    private static async Task GiveQuestBookToEveryone(RunState state, MegaCrit.Sts2.Core.Entities.Players.Player? local)
    {
        foreach (var p in state.Players)
        {
            try
            {
                if (p.GetRelic<QuestBookRelic>() != null) continue; // 保险：别发第二本
                var book = await RelicCmd.Obtain<QuestBookRelic>(p);
                // ⚠️ 新的一局必须从**空册**开始。玩家实测 + 日志确认：新开局拿到的那本册子里
                //    居然带着上一局的任务（遗物/存档属性不区分"哪一局"），
                //    不清就是「放弃一局重开，旧任务还在」。
                if (book.State.Active.Count > 0 || book.State.RestoredFromSave)
                {
                    Logger.Warn($"新局的任务册带着 {book.State.Active.Count} 条旧任务，已清空。");
                    book.State.ClearAll();
                    book.ClearPendingRewards();   // 上一局没发出去的奖励队列也一起丢掉
                }
                if (p == local)
                {
                    // UI（面板 / 接取界面 / 测试按钮）都看本地这一本
                    QuestRunner.BindLocal(book);
                    QuestRunner.RewardSink = book.Reward;
                }
            }
            catch (System.Exception e)
            {
                Logger.Error($"给玩家 {p.NetId} 发任务册失败: {e}");
            }
        }
    }

    /// <summary>
    /// 发完任务册**再**点亮「接取任务」。
    ///
    /// ⚠️ 以前是「fire-and-forget 发册 + 立刻点亮」——发册是异步的，点亮时新账本还没挂上，
    /// 选单抽出来的候选、接取写进去的地方，全是**上一局**那本账。玩家看到的就是
    /// 「放弃一局重开，旧任务还在」，而且接的任务也没真的进新局（日志已确认）。
    /// </summary>
    private static async Task GrantThenArmAsync(RunState state, MegaCrit.Sts2.Core.Entities.Players.Player? player)
    {
        try
        {
            await GiveQuestBookToEveryone(state, player);
            // 多人局：由 QuestPickSync 接管"谁挑了什么"的同步（本机挑完广播给其他人）。
            // 只有拿不到游戏的选择通道时才退回"同种子自动分配"（宁可没得挑，也不能不同步）。
            if (!QuestPickSync.StartMultiplayer(state))
                await AutoAssignQuestsForMultiplayer(state);
        }
        catch (System.Exception e)
        {
            Logger.Error($"开局发任务册失败: {e}");
        }
        EnsureUi();
        QuestSelection.CloseOpen();
        QuestSelection.ArmRunStart(player?.Character?.GetType());
    }
}
