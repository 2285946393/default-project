using MegaCrit.Sts2.Core.Models.Monsters;

namespace FrostSpire;

/// <summary>
/// 凛冬波次表（M3.7 重做 —— 难度大修）。
///
/// ★ 为什么重做（2026-10-06 用户实测原话）：
///   「你看我那波正常玩。我第三波就死了。你觉得这是正常难度？」
///
///   查完老表的数值，结论是：**用户是对的，老表在数学上就不可能活**。老表三个致命问题：
///
///   ① **第 4 波（第一场精英）就上 `GlobeHead`（148 血、每回合 16~18 伤害）** ——
///      它是三幕 BOSS 级怪。第 4 波「Wurm 56 + GlobeHead 148 + 幕池补位 1~2 只」≈ 230~250 血、
///      每回合 30+ 伤害，**用起始卡组（≈70 血 / 每回合 12~18 输出）必死，打都打不动**。
///   ② **前 3 波每波 2~3 只，池子里混着 LeafSlime（33 血）** —— 一波 ≈ 59 血 / 15 伤害，
///      而玩家在此之前还要先打完**房间本体遭遇（第 0 波）**。等于开局连打 4 场硬仗。
///   ③ **全程零回复** —— 16 波连打，中间没有火堆、没有商店、没有回血，
///      只有每波发一张牌。纯消耗战，起始卡组的血量池根本撑不过第 3 波。
///      （⇒ M3.7 在 `FrostPactRelic` 的间隙里加了回血，见 `FrostSpireConfig.GapHeal`。）
///
/// ★ 重做原则：**前期必须明显能过，后期才是硬仗**（前松后紧，不是均匀分布）。
///   玩家在这局里是会**成长**的（每波发一张牌 + 建筑越铺越多），所以曲线要跟着玩家的成长走。
///
/// 数值全部来自**反编译实测**（非估算，非进阶难度下的第一档数值）：
///   Inklet        11-17 血 / 3、2×3、10 伤   → 均值 ≈14 血 / 6 伤
///   BowlbugEgg    21-22 血 / 7 伤（另带 7 格挡）→ 均值 ≈21 血 / 4 伤
///   CorpseSlug    25-27 血 / 3×2、8 伤       → 均值 ≈26 血 / 7 伤
///   BowlbugNectar 35-38 血 / 3 伤            → 均值 ≈36 血 / 3 伤
///   LeafSlimeM    32-35 血 / 8 伤            → 均值 ≈33 血 / 8 伤
///   BowlbugSilk   40-43 血 / 4×2 伤          → 均值 ≈41 血 / 8 伤
///   BowlbugRock   45-48 血 / 冲撞            → 均值 ≈46 血 / 9 伤
///   Flyconid      47-49 血 / 11、8 伤        → 均值 ≈48 血 / 9 伤
///   FuzzyWurmCrawler 55-57 血 / 4 伤         → 均值 ≈56 血 / 4 伤（血厚但打得轻）
///   GlobeHead     148 血 / 6×3、13、16 伤    → 均值 148 血 / 16 伤（★ BOSS，只许出现在最后一波）
///
/// 骨架**固定**（每局一致，保证节奏）；波内怪物**按种子确定性随机**（联机各端一致）。
/// </summary>
public static class WaveTable
{
    public enum WaveKind { Normal, Elite, Boss, Event }

    /// <summary>
    /// 一场波 = 标题 + 种类 + 怪编号序列（事件波为空）+ 每只怪挂几层力量 + 血量缩放。
    /// <para><c>HpScale</c>：由难度设置换算（见 <c>FrostSpireConfig.WaveMultiplier</c>），
    /// 刷怪时用 <c>CreatureCmd.SetMaxAndCurrentHp</c> 直接缩放，默认 1.0 = 原值。</para>
    /// </summary>
    public sealed record Wave(string Title, WaveKind Kind, int[] Monsters, int Strength = 0, double HpScale = 1.0);

    public const int
        LeafSlime = 0, Inklet = 1, CorpseSlug = 2, GlobeHead = 3,
        FuzzyWurmCrawler = 4, Flyconid = 5, BowlbugEgg = 6, BowlbugNectar = 7,
        BowlbugSilk = 8, BowlbugRock = 9;

    // ========================================================================
    //  四档怪池（按「实测血量 × 实测伤害」分档，越靠后越硬）
    // ========================================================================
    private static readonly int[][] TierPools =
    {
        // T0 软：≈18 血 / 5 伤。开局两波用这档，让玩家有时间铺第一座建筑。
        new[] { Inklet, BowlbugEgg },

        // T1 中：≈31 血 / 5 伤。第 4 波精英之后的主力。
        new[] { CorpseSlug, BowlbugNectar },

        // T2 硬：≈37 血 / 8 伤。二幕后期 ~ 三幕前期。
        new[] { LeafSlime, BowlbugSilk },

        // T3 凶：≈50 血 / 7 伤。三幕主力。
        new[] { BowlbugRock, Flyconid, FuzzyWurmCrawler },
    };

    /// <summary>
    /// 精英波的「定海神针」—— 必出，保证精英成色稳定。
    /// ★ 只有 FuzzyWurmCrawler（56 血 / 4 伤）：血厚、打得轻，是**合格的精英靶子**。
    ///   **GlobeHead 绝不进这里** —— 它是 BOSS 级（148 血 / 16 伤），
    ///   放进第 4 波就是老表「第三波就死」的直接原因。
    /// </summary>
    private static readonly int[] EliteCore = { FuzzyWurmCrawler };

    /// <summary>BOSS 波的固定骨架（两巨头 + 幕池补位）。</summary>
    private static readonly int[] BossCore = { GlobeHead, FuzzyWurmCrawler };

    /// <summary>
    /// 骨架（16 波）：（怪池档位, 种类, 怪数量, 每只怪力量）。
    ///
    /// 期望难度曲线（均值，未计难度倍率）：
    ///   波 1-3   1/1/2 只软怪  ≈18/18/36 血   ≈5/5/10 伤   ← 开局，必须能过
    ///   波 4     精英 2 只     ≈87 血          ≈9 伤        ← 第一道坎（原为 230+ 血，已砍）
    ///   波 5     事件（不打架 + 大回血）
    ///   波 6-8   2 只中怪 ×3   ≈62 血          ≈10 伤
    ///   波 9     精英 3 只     ≈130 血         ≈20 伤
    ///   波 10    事件
    ///   波 11-12 2 只硬怪 ×2   ≈74 血          ≈16 伤
    ///   波 13    精英 3 只     ≈156 血         ≈22 伤
    ///   波 14    3 只凶怪      ≈150 血         ≈21 伤
    ///   波 15    精英 3 只     ≈156 血         ≈22 伤
    ///   波 16    BOSS 4 只     ≈310 血         ≈33 伤      ← 终局
    /// </summary>
    private static readonly (int tier, WaveKind kind, int count, int strength)[] Skeleton =
    {
        (0, WaveKind.Normal, 1, 0),   //  1
        (0, WaveKind.Normal, 1, 0),   //  2
        (0, WaveKind.Normal, 2, 0),   //  3  ← 用户原来死在这一波
        (1, WaveKind.Elite,  2, 0),   //  4  第一场精英
        (0, WaveKind.Event,  0, 0),   //  5  遭遇：拾荒者
        (1, WaveKind.Normal, 2, 0),   //  6
        (1, WaveKind.Normal, 2, 0),   //  7
        (1, WaveKind.Normal, 2, 0),   //  8
        (2, WaveKind.Elite,  3, 1),   //  9  第二场精英
        (0, WaveKind.Event,  0, 0),   // 10  遭遇：暴风雪
        (2, WaveKind.Normal, 2, 0),   // 11
        (2, WaveKind.Normal, 2, 0),   // 12
        (3, WaveKind.Elite,  3, 2),   // 13  第三场精英
        (3, WaveKind.Normal, 3, 0),   // 14
        (3, WaveKind.Elite,  3, 2),   // 15  第四场精英
        (3, WaveKind.Boss,   4, 2),   // 16  BOSS：凛冬之矛
    };

    /// <summary>幕段前缀：波 1-5 / 6-10 / 11-16。</summary>
    private static readonly string[] ActPrefix = { "初雪", "寒潮", "凛冬" };

    // ========================================================================
    //  构建与缓存
    // ========================================================================

    private static Wave[]? _built;
    private static ulong _builtSeed = ulong.MaxValue;
    private static int _builtCountBonus = int.MinValue;
    private static double _builtHpScale = double.NaN;

    /// <summary>
    /// 取本局的 16 波。同一「种子 + 难度设置」只构建一次（缓存）；
    /// 任一变了（新局 / 玩家改了难度）自动重建 —— 改难度**不用重启游戏**。
    /// ⚠️ 调用方（FrostPactRelic）持有局种子——面板/引擎都从这里取，绝不自己 new Random。
    /// </summary>
    public static Wave[] For(ulong runSeed)
    {
        int countBonus = CountBonus;
        double hpScale = FrostSpireConfig.WaveMultiplier;

        if (_built != null && _builtSeed == runSeed
            && _builtCountBonus == countBonus && System.Math.Abs(_builtHpScale - hpScale) < 0.0001)
            return _built;

        var rng = new MegaCrit.Sts2.Core.Random.Rng(runSeed, "frostspire_waves");
        var waves = new Wave[Skeleton.Length];

        for (int i = 0; i < Skeleton.Length; i++)
        {
            var (tier, kind, count, strength) = Skeleton[i];
            string act = ActPrefix[System.Math.Min(i / 6, 2)];
            string title;

            switch (kind)
            {
                case WaveKind.Event:
                    waves[i] = new Wave(
                        i == 4 ? "第5波 · 遭遇：拾荒者" : "第10波 · 遭遇：暴风雪",
                        WaveKind.Event, System.Array.Empty<int>(), 0, hpScale);
                    continue;

                case WaveKind.Elite:
                {
                    var monsters = new List<int>(EliteCore);
                    var pool = TierPools[tier];
                    int extra = System.Math.Max(count - monsters.Count, 0) + countBonus;
                    for (int e = 0; e < extra; e++)
                        monsters.Add(pool[rng.NextInt(pool.Length)]);
                    title = $"第{i + 1}波 · 精英：{act}哨卫";
                    waves[i] = new Wave(title, WaveKind.Elite, monsters.ToArray(), strength, hpScale);
                    continue;
                }

                case WaveKind.Boss:
                {
                    var monsters = new List<int>(BossCore);
                    var pool = TierPools[tier];
                    int extra = System.Math.Max(count - monsters.Count, 0) + countBonus;
                    for (int e = 0; e < extra; e++)
                        monsters.Add(pool[rng.NextInt(pool.Length)]);
                    waves[i] = new Wave("第16波 · BOSS：凛冬之矛", WaveKind.Boss, monsters.ToArray(), strength, hpScale);
                    continue;
                }

                default:
                {
                    var pool = TierPools[tier];
                    int n = System.Math.Max(count + countBonus, 1);
                    var monsters = new List<int>(n);
                    for (int m = 0; m < n; m++)
                        monsters.Add(pool[rng.NextInt(pool.Length)]);

                    // 段名按"这一幕的第几波普通怪"编号，读起来有推进感。
                    int seq = 1;
                    for (int k = 0; k < i; k++)
                        if (Skeleton[k].kind == WaveKind.Normal && k / 6 == i / 6) seq++;
                    string seg = seq switch { 1 => "初至", 2 => "跟进", _ => "推进" };

                    waves[i] = new Wave($"第{i + 1}波 · {act}{seg}", WaveKind.Normal, monsters.ToArray(), strength, hpScale);
                    continue;
                }
            }
        }

        _built = waves;
        _builtSeed = runSeed;
        _builtCountBonus = countBonus;
        _builtHpScale = hpScale;
        return waves;
    }

    /// <summary>
    /// 难度换算出来的「每波怪数量调整」（由 `Difficulty` 决定）：
    ///   休闲 0.6× → -1 只；正常 1.0× → 0；硬核 1.5× → +1 只。
    /// </summary>
    private static int CountBonus
    {
        get
        {
            double m = FrostSpireConfig.WaveMultiplier;
            return m < 0.9 ? -1 : (m > 1.1 ? 1 : 0);
        }
    }

    // ========================================================================
    //  ★★ M3.10 修：「每局波次不同」其实从未生效（真 bug）
    //
    //  现象：`For(ulong runSeed)` 老早就写好了种子参数，但**所有实际调用点
    //  （`FrostPactRelic` 生成波次、`FrostUi` 面板预览）用的都是 `All`**，
    //  而 `All` 写死 `For(0)` ⇒ **每一局的 16 波完全相同**，种子参数是个死旋钮。
    //  （和 M3.7 修的 `Difficulty` / `WaveMultiplier` 是同一类毛病：写了没人用。）
    //
    //  修法：加一个「当前局种子」的静态绑定，遗物拿到 RunState 时绑一次，
    //  之后 `All` 返回 `For(_runSeed)` —— **调用点一个都不用改**，
    //  而且面板预览和实战自动一致（如果只改遗物、不改 UI，预览就会显示另一套波次）。
    // ========================================================================

    private static ulong _runSeed;
    private static bool _seedBound;

    /// <summary>
    /// 绑定本局种子。种子来源：`RunState.Rng.Seed`（反编译确认 `RunRngSet` 有 `public ulong Seed`）。
    /// ★ 必须在**生成第一波之前**调（`FrostPactRelic` 开局时调一次）。
    /// </summary>
    public static void BindRunSeed(ulong seed)
    {
        _runSeed = seed;
        _seedBound = true;
        Entry.Logger.Info($"凛冬尖塔：波次表已绑定本局种子 {seed}（16 波按此种子生成，每局不同）。");
    }

    /// <summary>当前局种子（未绑定时是 0，仅作兜底）。</summary>
    public static ulong RunSeed => _runSeed;

    /// <summary>种子是否已绑定（UI 可用它决定要不要显示"预览中"）。</summary>
    public static bool IsSeedBound => _seedBound;

    /// <summary>
    /// 全项目统一入口：返回**本局**的 16 波。
    /// ★ 种子未绑定时退回 0（UI 早于遗物挂载的场景），绑定后自动重建。
    /// </summary>
    public static Wave[] All => For(_runSeed);
}
