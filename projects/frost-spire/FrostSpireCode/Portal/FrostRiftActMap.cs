using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Random;

namespace FrostSpire.Portal;

/// <summary>
/// 凛冬裂隙的**独立小地图**——「传送门后面的那张地图」。
///
/// ★ 复刻自集成战略事件（工坊 3747637213）的 `IntegratedStrategyTreeHoleActMap : ActMap`，
///   这是**已验证可行**的写法：自己继承 `ActMap`，自己填 `Grid` + 三个 abstract 成员。
///
/// 形状（3 列 × 6 层，一条直上直下的主路，每层 3 个并排节点里选 1 个）：
///
///         col 1     col 3     col 5
///   row 0         [ 起点 ]               ← Ancient（会被隐藏图标）
///   row 1   ① ──── ② ──── ③             ← 大战斗（16 波）
///   row 2   ① ──── ② ──── ③             ← 幕：商店 / 火堆 / 事件 / 宝箱
///   row 3   ① ──── ② ──── ③             ← 大战斗（16 波）
///   row 4   ① ──── ② ──── ③             ← 幕
///   row 5   ① ──── ② ──── ③             ← 幕
///   row 6   ① ──── ② ──── ③             ← 大战斗（16 波）
///   row 7         [ 终点 ]               ← Treasure（走完 = 出裂隙）
///   row 8         [ BOSS ]               ← Boss（图标会被隐藏；不走）
///
/// ★ 层构成是**用户指定的**（原话）：
///   「每个一次大战斗（16波）。然后下一幕然后大战斗然后下一幕然后下一幕然后大战斗然后结束」
///   ⇒ 大战斗 → 幕 → 大战斗 → 幕 → 幕 → 大战斗 → 结束
///   即 3 场大战斗（row 1 / 3 / 6）+ 3 个非战斗「幕」（row 2 / 4 / 5）。
///   「幕」= 补给与选择的层，节点类型照本体地图来（商店 / 火堆 / 事件 / 宝箱），
///   用户原话「有商店回血火堆啥的」。
///
/// 设计取舍（相对 Natsuki 的 7×6 网格）：
///   - 它做的是"一整幕"的体量，我们只要"一个小副本"，3×6 够了。
///   - 它每条路都要接下一层三个点（3×3=9 条连线），我们**每层只连自己那一列**，
///     走起来是一条道走到黑，逻辑简单、图上干净。
///   - 它靠 `AddChildPoint` 连路；我们同样。**注意方向：从起点往终点连**。
/// </summary>
public sealed class FrostRiftActMap : ActMap
{
    /// <summary>每层几个选项。</summary>
    private const int LanesPerRow = 3;

    /// <summary>
    /// 地图总层数（不含起点 / 终点 / Boss）。
    /// 见类头：6 层 = 3 场大战斗 + 3 个幕。
    /// </summary>
    public const int EncounterRows = 6;

    /// <summary>
    /// 「大战斗层」的 row 号（1-based，相对起点）。
    /// 踩到这些层 = 打那场 16 波（波次由 `FrostPactRelic` + `WaveTable` 驱动，
    /// 只要玩家带着凛冬契约遗物，**任何战斗**都跑满 16 波，所以这里给 `Monster` 就够）。
    /// </summary>
    private static readonly int[] BattleRows = { 1, 3, 6 };

    /// <summary>三层可选列（1 / 3 / 5），中间那列是 3。</summary>
    private static readonly int[] LaneColumns = { 1, 3, 5 };

    /// <summary>起点/终点的列（居中）。</summary>
    private const int CenterColumn = 3;

    /// <summary>
    /// 「幕」层（非战斗补给层）的候选节点池。
    ///
    /// ★ 为什么是 4 选 3：每层洗牌后取前 3 个 → 每层必定**只少一种**，
    ///   于是「商店」和「火堆」**至少有一个在场**，玩家每层都拿得到补给/回血的选择权。
    ///   刻意**不放 `Elite`** —— 精英是战斗，会跟大战斗一样跑满 16 波，混进"幕"里名不副实。
    ///
    /// `MapPointType` 枚举（反编译核实）：
    ///   `Unassigned=0, Unknown=1(问号/事件), Shop=2, Treasure=3, RestSite=4(火堆),
    ///    Monster=5, Elite=6, Boss=7, Ancient=8(起点)`
    /// </summary>
    private static readonly MapPointType[] InterludePool =
    {
        MapPointType.Shop,      // 商店
        MapPointType.RestSite,  // 火堆（回血 / 升级）
        MapPointType.Unknown,   // 问号（事件）
        MapPointType.Treasure,  // 宝箱
    };

    private readonly MapCoord _terminalCoord;

    protected override MapPoint?[,] Grid { get; }

    public override MapPoint BossMapPoint { get; }

    public override MapPoint StartingMapPoint { get; }

    /// <summary>终点坐标。踩到它 = 裂隙走完。</summary>
    public MapCoord TerminalCoord => _terminalCoord;

    /// <summary>裂隙里所有"遭遇层"的点，按层从上到下、每层从左到右。</summary>
    public IReadOnlyList<MapPoint> EncounterPoints { get; }

    /// <summary>大战斗层数（= `BattleRows.Length`，当前 3）。文案/统计用。</summary>
    public static int BattleCount => BattleRows.Length;

    /// <summary>幕（非战斗补给层）数（= 总层数 - 大战斗层数，当前 3）。</summary>
    public static int InterludeCount => EncounterRows - BattleRows.Length;

    public static FrostRiftActMap Create(Rng rng) => new(rng);

    private FrostRiftActMap(Rng rng)
    {
        // 3 列 × 9 行（row 0..8）
        Grid = new MapPoint[6, EncounterRows + 3];

        // —— 起点 & BOSS（固定坐标，CanBeModified=false 表示"这俩别被随机改"）——
        StartingMapPoint = CreateSpecialPoint(CenterColumn, 0, MapPointType.Ancient);
        BossMapPoint = CreateSpecialPoint(CenterColumn, EncounterRows + 2, MapPointType.Boss);

        // —— 遭遇层（大战斗 / 幕 交替，见 BattleRows）——
        var rows = new List<MapPoint[]>(EncounterRows);
        for (int r = 0; r < EncounterRows; r++)
            rows.Add(CreateEncounterRow(rng, row: r + 1));

        // —— 终点（在最后一层遭遇之后）——
        // ★★ 必须用 **CreateGridPoint**（会写进 Grid），不能用 CreateSpecialPoint！反编译 ActMap 核实：
        //     · `ActMap.GetAllMapPoints()` 是**双层循环遍历 Grid** —— 不写进 Grid 的点，地图屏根本不画；
        //     · `ActMap.GetPoint(col, row)` 只对 `StartingMapPoint` / `BossMapPoint` 做特殊返回，
        //       其余一律 `return Grid[col, row]` —— 终点两者都不是，不写 Grid 就会拿到 null。
        //   ⇒ 起点/Boss 不写 Grid 是**对的**（基类对这两个坐标特殊处理，原版 StandardActMap 也这么干），
        //     但终点是我们自己加的普通点，**必须写 Grid**，否则玩家走完所有层根本看不到出口。
        var terminal = CreateGridPoint(CenterColumn, EncounterRows + 1, MapPointType.Treasure);
        _terminalCoord = terminal.coord;

        // —— 连线：起点 → 第1层；层间；最后一层 → 终点；终点 → BOSS ——
        // ★ 方向必须是从"前"往"后"连（AddChildPoint），Natsuki 也是这么连的。
        foreach (var p in rows[0]) StartingMapPoint.AddChildPoint(p);

        for (int r = 0; r + 1 < EncounterRows; r++)
            for (int lane = 0; lane < LanesPerRow; lane++)
                rows[r][lane].AddChildPoint(rows[r + 1][lane]);   // ★ 同列直连，保证走得通

        foreach (var p in rows[^1]) p.AddChildPoint(terminal);
        terminal.AddChildPoint(BossMapPoint);

        EncounterPoints = rows.SelectMany(x => x).ToList();
    }

    /// <summary>
    /// 造一整层（3 个并排节点）。
    ///
    /// 层构成**照用户指定的结构来**（见类头）：
    ///   · **大战斗层**（row 1 / 3 / 6）：三条岔路全是 `Monster`。
    ///     进战斗即 16 波（`FrostPactRelic` 的波次账目，`WaveTable` 出表），
    ///     打完最后一波才放行 —— 这就是用户说的「每个一次大战斗（16波）」。
    ///   · **幕层**（row 2 / 4 / 5）：从 `InterludePool`（商店 / 火堆 / 事件 / 宝箱）
    ///     洗牌取 3 个、**三个岔路互不重复** —— 玩家挑一个走，挑什么就得到什么。
    ///     洗牌用本局 `Rng`（各端一致、读档不变）。
    /// </summary>
    private MapPoint[] CreateEncounterRow(Rng rng, int row)
    {
        var arr = new MapPoint[LanesPerRow];

        // —— 大战斗层：固定战斗（16 波）——
        if (BattleRows.Contains(row))
        {
            for (int lane = 0; lane < LanesPerRow; lane++)
                arr[lane] = CreateGridPoint(LaneColumns[lane], row, MapPointType.Monster);
            return arr;
        }

        // —— 幕层：商店 / 火堆 / 事件 / 宝箱，洗牌后取 3 个 ——
        var pool = new List<MapPointType>(InterludePool);
        // Fisher–Yates 洗牌，用本局 Rng（各端一致、可复现）
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = rng.NextInt(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        for (int lane = 0; lane < LanesPerRow; lane++)
            arr[lane] = CreateGridPoint(LaneColumns[lane], row, pool[lane % pool.Count]);
        return arr;
    }

    /// <summary>造一个「要显示在地图上」的点（**会写进 Grid**）。除起点/Boss 外一律用这个。</summary>
    private MapPoint CreateGridPoint(int col, int row, MapPointType type)
    {
        var p = CreateSpecialPoint(col, row, type);
        Grid[col, row] = p;
        return p;
    }

    /// <summary>
    /// 造一个**不占 Grid** 的点 —— 只给起点/Boss 用。
    /// `ActMap.GetPoint` 对这两个坐标有特殊返回，所以它们不写 Grid 也能被找到；
    /// 其它点这么造就会「隐身」（地图屏拿不到、也画不出）。
    /// </summary>
    private static MapPoint CreateSpecialPoint(int col, int row, MapPointType type)
        => new(col, row) { PointType = type, CanBeModified = false };

    /// <summary>这个坐标是不是裂隙终点（走完 = 该出去了）。</summary>
    public bool IsTerminalCoord(MapCoord? coord)
        => coord.HasValue && coord.Value.Equals(_terminalCoord);

    /// <summary>走过的坐标里有没有终点。</summary>
    public bool HasVisitedTerminal(IEnumerable<MapCoord> visitedCoords)
        => visitedCoords.Any(c => c.Equals(_terminalCoord));
}
