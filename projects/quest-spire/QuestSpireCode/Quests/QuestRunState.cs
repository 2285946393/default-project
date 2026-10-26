namespace QuestSpire.Quests;

/// <summary>
/// **一本任务册自己的进度**（每本各存各的）。
///
/// 为什么不再是静态单例：多人局里本机只写「本地玩家那本」，主机那份存档里就没有别人的进度 →
/// 玩家 SL 回来任务会变（2026-09-24 玩家反馈）。改成每本各自持有之后，**每个客户端都会对
/// 所有玩家的任务册照常推进**（钩子里只判 Owner），所以每台机器算出来的东西一样、
/// 主机存下去的也就是对的 —— 这是拆《海克斯符文》学到的做法（见交接文档 §23）。
/// </summary>
public sealed class QuestRunState
{
    /// <summary>
    /// 一本任务册最多同时挂几条任务。
    /// 2026-09-25 玩家要求从 10 提到 20（「可能有四个人一起玩」，每人多接一点）。
    /// ⚠️ 这是**每人**的上限：4 人局同时可能有 80 条在跑，进度钩子是按人头跑的，
    ///    性能开销随之变大（但都是整数加减，实际影响很小）。
    /// </summary>
    public const int MaxActive = 20;

    private readonly List<QuestState> _active = new();
    private readonly HashSet<QuestKind> _completed = new();

    public IReadOnlyList<QuestState> Active => _active;

    /// <summary>
    /// 这一册里**已经判定完成**的任务种类。
    ///
    /// 用途（2026-09-27）：进房间时拿它跟"已经发过奖励的账"对一遍 ——
    /// **已完成但没发过奖励的，一律补发**。这样不管中间哪一步被中断（切房间、读档、异常），
    /// 都不会出现「任务显示完成、玩家却什么都没有」的静默丢失。
    /// </summary>
    public IReadOnlyCollection<QuestKind> CompletedKinds => _completed;

    /// <summary>本册状态是否来自存档（读档时为 true）。</summary>
    public bool RestoredFromSave { get; private set; }

    public int SlotsLeft => MaxActive - _active.Count;
    public bool IsFull => SlotsLeft <= 0;

    // —— 存档桥：扁平三元组 [任务类型, 当前进度, 是否已完成] ——
    public int[] Export()
    {
        var data = new int[_active.Count * 3];
        for (int i = 0; i < _active.Count; i++)
        {
            var q = _active[i];
            data[i * 3] = (int)q.Def.Kind;
            data[i * 3 + 1] = q.Current;
            data[i * 3 + 2] = q.Completed ? 1 : 0;
        }
        return data;
    }

    public void Import(int[]? data)
    {
        _active.Clear();
        _completed.Clear();
        RestoredFromSave = true;
        if (data == null) return;
        for (int i = 0; i + 2 < data.Length; i += 3)
        {
            var def = QuestCatalog.Find((QuestKind)data[i]);
            if (def == null) continue;
            if (_active.Any(a => a.Def.Kind == def.Kind)) continue;
            var state = new QuestState(def);
            state.Restore(data[i + 1], data[i + 2] != 0);
            _active.Add(state);
            if (state.Completed) _completed.Add(def.Kind);
        }
    }

    public bool CanAdd(QuestDef def)
        => !IsFull && !_active.Any(a => a.Def.Kind == def.Kind) && !_completed.Contains(def.Kind);

    public bool TryAdd(QuestDef def)
    {
        if (!CanAdd(def)) return false;
        _active.Add(new QuestState(def));
        return true;
    }

    public List<QuestState> AddProgress(QuestKind kind, int amount)
    {
        var done = new List<QuestState>();
        if (amount <= 0) return done;
        foreach (var q in _active)
            if (q.Def.Kind == kind && q.Add(amount))
            {
                _completed.Add(kind);
                done.Add(q);
            }
        return done;
    }

    public List<QuestState> SetProgress(QuestKind kind, int value)
    {
        var done = new List<QuestState>();
        foreach (var q in _active)
            if (q.Def.Kind == kind && q.SetAbsolute(value))
            {
                _completed.Add(kind);
                done.Add(q);
            }
        return done;
    }

    public void ResetProgress(QuestKind kind)
    {
        foreach (var q in _active)
            if (q.Def.Kind == kind) q.Reset();
    }

    /// <summary>
    /// 清成"全新一局"的样子。
    ///
    /// ⚠️ **新局拿到册子后必须调这个**：玩家实测发现，新开局拿到的那本任务册里居然带着上一局的
    /// 任务（遗物实例/存档属性不区分"哪一局"），不清就会表现为"下一局任务还在"。
    /// </summary>
    public void ClearAll()
    {
        _active.Clear();
        _completed.Clear();
        RestoredFromSave = false;
    }

    /// <summary>候选（排除进行中/已完成）。character 用来挡掉角色专属任务。</summary>
    public IReadOnlyList<QuestDef> Offers(int n, bool preferEarly = false, Type? character = null)
        => QuestCatalog.PickOffers(
            n, _active.Select(a => a.Def.Kind).Concat(_completed), preferEarly, character);

    /// <summary>
    /// **确定性**候选：多人局里每个客户端要用同一个种子抽，才能抽出同一批（否则各客户端任务不一样）。
    /// </summary>
    public IReadOnlyList<QuestDef> OffersSeeded(int n, int seed, Type? character = null)
        => QuestCatalog.PickOffersSeeded(
            n, seed, _active.Select(a => a.Def.Kind).Concat(_completed), character);
}
