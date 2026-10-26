using MegaCrit.Sts2.Core.Map;

namespace FrostSpire.Portal;

/// <summary>
/// 凛冬裂隙（传送门）——需要跨存档保存的状态。
///
/// ★ 这是整个传送门方案里唯一"难"的地方，但现在不难了：
///   RitsuLib 提供 `RunSavedData&lt;T&gt;`（`STS2RitsuLib.RunData` 命名空间），
///   专门用来挂"跟着某一局存档走的自定义数据"——**不用自己碰 `SerializableRun`**。
///   用法：`RunSavedDataStore.For(ModId).Register(key, factory)` 拿一个槽，
///   然后 `Get/TryGet/Set/Remove/Modify`。序列化、读档、联机贡献全由 RitsuLib 管。
///
/// 反编译参考：集成战略事件（工坊 3747637213）的 `TreeHolePersistedState`（18 个字段），
/// 我们只需要它的一半——因为我们的临时地图是**固定形状**的，不需要存整张 map。
/// </summary>
public sealed class FrostRiftSavedState
{
    /// <summary>数据版本。改结构时 +1，配 RunSavedDataOptions.Migrations 做迁移。</summary>
    public int Version { get; set; } = 1;

    /// <summary>

    /// 裂隙是否处于"激活中"（我们正站在临时地图上）。
    /// false = 没有裂隙会话，一切照常。
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// 已打完的节点数（0 ~ NodeCount）。
    /// ★ 这个字段就是"进度"，也是唯一需要每步落盘的东西。
    /// </summary>
    public int ClearedNodes { get; set; }

    /// <summary>造临时地图用的种子（目前固定形状用不到，留给以后随机化，先存着不亏）。</summary>
    public uint RiftMapSeed { get; set; }

    // ——— 以下三项是"返回原地图"所需的快照 ———

    /// <summary>进入裂隙时的幕索引。</summary>
    public int OriginalActIndex { get; set; }

    /// <summary>进入裂隙时在幕里的楼层。</summary>
    public int OriginalActFloor { get; set; }

    /// <summary>
    /// 进入裂隙时玩家所在的地图坐标（返回时落回这里）。
    /// ★ 存成 (Col, Row) 两个 int，比直接存 `MapCoord?` 更稳——`MapCoord` 是不是可 JSON 序列化没验证过。
    /// </summary>
    public int EntryCol { get; set; } = -1;
    public int EntryRow { get; set; } = -1;

    /// <summary>EntryCol/Row 是否有效。</summary>
    public bool HasEntryCoord => EntryCol >= 0 && EntryRow >= 0;

    /// <summary>取回入口坐标（无效时返回 null）。</summary>
    public MapCoord? GetEntryCoord() => HasEntryCoord ? new MapCoord(EntryCol, EntryRow) : null;

    // ——— 裂隙入口点（原版地图上那个点）的坐标 ———

    /// <summary>
    /// 本局在原版地图上注入的「凛冬裂隙入口点」坐标。
    /// ★ 存进 run 槽（而不是静态字段），这样跨局不会残留：
    ///   上一局在 (6,0) 放的裂隙点，不会把新一局 (6,0) 上的真地图点误拦成"开门"。
    ///   同时也让读档后仍能认出哪个点是我们的。
    /// </summary>
    public int RiftNodeCol { get; set; } = -1;
    public int RiftNodeRow { get; set; } = -1;

    /// <summary>写入裂隙入口点坐标。</summary>
    public void SetRiftNodeCoord(MapCoord coord)
    {
        RiftNodeCol = coord.col;
        RiftNodeRow = coord.row;
    }

    /// <summary>取回裂隙入口点坐标（没注入过 → null）。</summary>
    public MapCoord? GetRiftNodeCoord()
        => (RiftNodeCol >= 0 && RiftNodeRow >= 0) ? new MapCoord(RiftNodeCol, RiftNodeRow) : null;

    /// <summary>写入入口坐标。</summary>
    public void SetEntryCoord(MapCoord? coord)
    {
        if (coord.HasValue)
        {
            // ★ 字段名小写（反编译核实：public int col; public int row;）
            EntryCol = coord.Value.col;
            EntryRow = coord.Value.row;
        }
        else
        {
            EntryCol = -1;
            EntryRow = -1;
        }
    }

    /// <summary>重置成"没有裂隙会话"。</summary>
    public void Reset()
    {
        Active = false;
        ClearedNodes = 0;
        OriginalActIndex = 0;
        OriginalActFloor = 0;
        EntryCol = -1;
        EntryRow = -1;
        RiftNodeCol = -1;
        RiftNodeRow = -1;
    }
}
