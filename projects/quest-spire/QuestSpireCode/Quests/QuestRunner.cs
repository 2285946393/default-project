using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Players;
using QuestSpire.Relics;

namespace QuestSpire.Quests;

/// <summary>
/// 任务系统的**对外门面**：面板、「接取任务」界面、测试按钮都读这里。
///
/// ⚠️ 真正的进度**不在这个静态类里**，而在每本 <c>QuestBookRelic</c> 自己持有的
/// <see cref="QuestRunState"/> 里（多人局每个客户端都会为所有玩家推进，见那个类的说明）。
/// 这里只是指向**本地玩家那一本**，供 UI 读取。
/// </summary>
public static class QuestRunner
{
    public const int MaxActive = QuestRunState.MaxActive;

    /// <summary>本局队伍人数（单人 1）。开局时由 Entry 写入。</summary>
    public static int PlayerCount { get; set; } = 1;

    private static QuestRunState? _state;
    private static object? _boundRun;   // 这本账属于哪一局（RunState 引用）

    /// <summary>本地玩家那本任务册（发册/读档时绑定）。接取后的"改动牌组"要用它。</summary>
    public static QuestBookRelic? LocalBook { get; private set; }

    /// <summary>
    /// 刚接取完一批任务后立刻跑一次「接取时改动」（诅咒交易塞诅咒 / 大改造换牌）。
    /// ⚠️ 别只靠进房间那次：玩家接下之后**当场**看不到牌组变化会以为没生效。
    /// </summary>
    public static async Task ApplyAcceptSetups()
    {
        try { if (LocalBook != null) await LocalBook.EnsureQuestSetups(); }
        catch (Exception e) { Entry.Logger.Warn($"接取改动执行失败: {e.Message}"); }
    }

    /// <summary>把门面指向本地玩家这一本。开局发册 / 读档时调用。</summary>
    public static void BindLocal(QuestBookRelic book)
    {
        LocalBook = book;
        _state = book.State;
        // 以"绑定时的那一局"为准（发册/读档都发生在当前局里）；
        // 拿不到就退回账本自己记的那一局。
        _boundRun = Entry.CurrentRunRef ?? book.Owner?.RunState;
        Entry.Logger.Info($"QuestRunner: 已绑定本地任务册（本局任务 {book.State.Active.Count} 条）。");
    }

    /// <summary>断开指向。局结束 / 新局开头调用。</summary>
    public static void ClearLocal()
    {
        LocalBook = null;
        _state = null;
        _boundRun = null;
    }

    /// <summary>
    /// 本地玩家那本任务册的进度。
    ///
    /// ⚠️ **只认当前这一局**：绑定时记下了它属于哪个 RunState，读的时候如果发现
    /// 现在这一局不是那个，就一律当空——放弃一局重开时，静态引用只要晚一步更新，
    /// 面板就会显示上一局的任务、接取也会写进上一局（玩家实测的 bug，见交接文档 §27）。
    /// </summary>
    public static QuestRunState? LocalState
    {
        get
        {
            if (_state == null) return null;
            var cur = CurrentRun();
            if (cur != null && _boundRun != null && !ReferenceEquals(cur, _boundRun)) return null;
            return _state;
        }
    }

    private static object? CurrentRun() => Entry.CurrentRunRef;

    /// <summary>
    /// 联机时**队友那几本任务册**（名字 + 进行中的任务），供面板按人分开显示。
    /// 每台客户端手里都有所有人的册子，而且进度算出来一致，所以直接读就行、不用联网。
    /// </summary>
    public static List<(string Name, IReadOnlyList<QuestState> Quests)> OtherBooks()
    {
        var list = new List<(string, IReadOnlyList<QuestState>)>();
        try
        {
            if (PlayerCount <= 1) return list;
            if (Entry.CurrentRunRef is not RunState run) return list;
            // ⚠️ 用**本机 NetId** 判断"谁是我"，不依赖可能兜底成第一位玩家的 LocalPlayer。
            //    优先 `LocalContext.NetId`（永不抛异常）；`NetService.NetId` 断线时会抛，兜一下。
            var myId = MegaCrit.Sts2.Core.Context.LocalContext.NetId;
            if (!myId.HasValue)
            {
                try { myId = RunManager.Instance.NetService?.NetId; }
                catch { /* 拿不到就走下面的 LocalPlayer 兜底 */ }
            }
            var local = Entry.LocalPlayer(run);
            foreach (var p in run.Players)
            {
                bool isMe = myId.HasValue ? p.NetId == myId.Value
                                          : (local != null && p.NetId == local.NetId);
                Entry.Logger.Info($"面板分人：玩家 {p.NetId} → {(isMe ? "我" : "队友")}");
                if (isMe) continue;
                var book = p.GetRelic<QuestBookRelic>();
                if (book == null) continue;
                list.Add((PlayerLabel(p), book.State.Active));
            }
        }
        catch (Exception e) { Entry.Logger.Warn($"取队友任务册失败: {e.Message}"); }
        return list;
    }

    /// <summary>队友叫什么：用他的角色名（拿不到就写"队友"）。</summary>
    private static string PlayerLabel(Player p)
    {
        var n = p.Character?.GetType().Name ?? "队友";
        return n switch
        {
            "Ironclad" => "铁甲战士",
            "Silent" => "静默猎手",
            "Defect" => "故障机器人",
            "Regent" => "摄政王",
            "Necrobinder" => "亡灵绑定者",
            "Watcher" => "观者",
            _ => n,
        };
    }

    public static IReadOnlyList<QuestState> Active
        => LocalState?.Active ?? (IReadOnlyList<QuestState>)Array.Empty<QuestState>();

    public static bool RestoredFromSave => LocalState?.RestoredFromSave ?? false;
    public static int SlotsLeft => LocalState?.SlotsLeft ?? MaxActive;
    public static bool IsFull => LocalState?.IsFull ?? false;

    public static bool TryAdd(QuestDef def) => LocalState?.TryAdd(def) ?? false;

    public static IReadOnlyList<QuestDef> Offers(int n, bool preferEarly = false, Type? character = null)
        => LocalState?.Offers(n, preferEarly, character) ?? (IReadOnlyList<QuestDef>)Array.Empty<QuestDef>();

    public static IReadOnlyList<QuestDef> OffersSeeded(int n, int seed, Type? character = null)
        => LocalState?.OffersSeeded(n, seed, character) ?? (IReadOnlyList<QuestDef>)Array.Empty<QuestDef>();

    public static void ResetProgress(QuestKind kind) => LocalState?.ResetProgress(kind);

    /// <summary>发奖出口：任务册拿到时把自己注册进来（UI 上的测试按钮也走同一套）。</summary>
    public static Func<QuestState, Task>? RewardSink { get; set; }

    /// <summary>测试用：把本地这本所有进行中的任务直接判完成并发奖。返回完成数量。</summary>
    public static async Task<int> ForceCompleteAll()
    {
        var st = LocalState;
        if (st == null) return 0;
        int count = 0;
        foreach (var q in st.Active.ToList())
        {
            if (!q.ForceComplete()) continue;
            count++;
            if (RewardSink == null) continue;
            // 单个任务的奖励出问题，不能让后面所有任务都发不出来（玩家反馈：点测试按钮"直接完成都不行"）。
            try { await RewardSink(q); }
            catch (System.Exception e) { Entry.Logger.Error($"测试按钮：任务「{q.Def.Title}」发奖失败：{e}"); }
        }
        return count;
    }
}
