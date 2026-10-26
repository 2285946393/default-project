namespace DouSpireRules;

/// <summary>
/// 斗地主牌型判定 + 大小比较。这是整个玩法的规则内核：
/// 之后搬进游戏时，这段代码可以原样复用（同是 net9.0，无第三方依赖）。
/// </summary>
public sealed class ComboJudge
{
    /// <summary>用几副牌。1 = 54 张（三人斗地主）；2 = 108 张（四人斗地主）。</summary>
    public int DeckCopies { get; }

    /// <summary>
    /// 王炸最少要几张王。默认 0 = 老口径（= 副数 × 2：一副 2 张、两副 4 张）。
    /// 欢乐 2v2 组队口径传 3：三张王就是王炸，四张更大。
    /// </summary>
    public int RocketJokers { get; }

    public ComboJudge(int deckCopies = 1, int rocketJokers = 0)
    {
        if (deckCopies is < 1 or > 2) throw new ArgumentOutOfRangeException(nameof(deckCopies));
        DeckCopies = deckCopies;
        RocketJokers = rocketJokers > 0 ? rocketJokers : 2 * deckCopies;
    }

    /// <summary>把一组牌判成牌型。不成牌型返回 null。</summary>
    public Combo? TryParse(IEnumerable<Card> cards)
    {
        var list = cards.ToList();
        var n = list.Count;
        if (n == 0) return null;

        var byRank = new Dictionary<int, int>();
        foreach (var c in list)
            byRank[c.Rank] = byRank.TryGetValue(c.Rank, out var v) ? v + 1 : 1;

        var ranks = byRank.Keys.OrderBy(r => r).ToList();
        var distinct = ranks.Count;
        var maxRank = ranks[^1];
        var maxCount = byRank.Values.Max();

        var consecutive = IsConsecutive(ranks);
        var chainable = ranks.All(r => r <= Ranks.Ace);       // 顺子/连对/飞机 不含 2 和王
        var allSameCount = (int c) => byRank.Values.All(v => v == c);

        Combo Make(ComboType t, int main) => new(t, main, list);

        // —— 王炸：全部是王，且张数达到门槛（老口径 = 副数 × 2；2v2 口径 ≥ 3 张）——
        if (list.All(c => c.IsJoker) && n >= RocketJokers)
            return Make(ComboType.Rocket, maxRank);

        // —— 炸弹：清一色同点数且 ≥4 张（两副牌下 5~8 张也算，且张数多的大）——
        if (distinct == 1 && n >= 4)
            return Make(ComboType.Bomb, maxRank);

        // —— 基础牌型 ——
        if (n == 1) return Make(ComboType.Single, maxRank);
        if (n == 2 && distinct == 1) return Make(ComboType.Pair, maxRank);
        if (n == 3 && distinct == 1) return Make(ComboType.Triple, maxRank);
        if (n == 4 && distinct == 2 && maxCount == 3) return Make(ComboType.TripleWithSingle, TripleRank(byRank));
        if (n == 5 && distinct == 2 && maxCount == 3 && byRank.Values.Min() == 2)
            return Make(ComboType.TripleWithPair, TripleRank(byRank));

        // —— 顺子：≥5 张、张张不同、连续、3~A ——
        if (n >= 5 && distinct == n && chainable && consecutive)
            return Make(ComboType.Straight, maxRank);

        // —— 连对：≥3 对、每对 2 张、连续、3~A ——
        if (n >= 6 && n % 2 == 0 && allSameCount(2) && n / 2 >= 3 && chainable && consecutive)
            return Make(ComboType.ConsecutivePairs, maxRank);

        // —— 飞机不带：≥2 组、每组 3 张、连续、3~A ——
        if (n >= 6 && n % 3 == 0 && allSameCount(3) && n / 3 >= 2 && chainable && consecutive)
            return Make(ComboType.Plane, maxRank);

        // —— 四带二 / 四带两对（四张的点数决定大小）——
        // rlcard 规矩：带的那两张不能是"双王" —— 那是王炸，不许拆着当带牌。
        // （这条不是我拍脑袋写的，是双向对车从 rlcard 真实判牌器里逮出来的。）
        if (n == 6 && maxCount == 4)
        {
            var four = byRank.First(kv => kv.Value == 4).Key;
            var kickersAreBothJokers = byRank.ContainsKey(Ranks.SmallJoker) && byRank.ContainsKey(Ranks.BigJoker);
            if (!kickersAreBothJokers)
                return Make(ComboType.FourWithTwoSingles, four);
        }
        if (n == 8 && maxCount == 4)
        {
            var four = byRank.First(kv => kv.Value == 4).Key;
            var rest = byRank.Where(kv => kv.Key != four).ToList();
            if (rest.Count > 0 && rest.All(kv => kv.Value == 2))
                return Make(ComboType.FourWithTwoPairs, four);
        }

        // —— 飞机带单（n = 4k）/ 飞机带对（n = 5k）——
        // 两条硬规矩（同样是双向对车逮出来的，我原来漏了）：
        //   · 飞机的每个点数必须**正好 3 张**：多出来的那张不能拿去当"带牌"，
        //     也就是**不许拆炸弹去凑飞机**（QQQQKKK 不是飞机带单）。
        //   · 带牌有上限：跟飞机贴边的点数最多拿 2 张（第 3 张会被吸进飞机、产生歧义），
        //     任何点数最多拿 3 张（第 4 张是炸弹本体）。'2' 例外，不受贴边限制。
        if (n % 4 == 0 && n / 4 >= 2 && TryPlaneWithSingles(byRank, n / 4, out var topSingles))
            return Make(ComboType.PlaneWithSingles, topSingles);

        if (n % 5 == 0 && n / 5 >= 2 && TryPlaneWithPairs(byRank, n / 5, out var topPairs))
            return Make(ComboType.PlaneWithPairs, topPairs);

        return null;
    }

    /// <summary>
    /// <paramref name="candidate"/> 能不能压过 <paramref name="previous"/>。
    /// <paramref name="previous"/> 为 null 表示这一轮由你自由出牌，任何合法牌型都行。
    /// </summary>
    public static bool Beats(Combo? candidate, Combo? previous)
    {
        if (candidate is null || candidate.Type == ComboType.Invalid) return false;
        if (previous is null || previous.Type == ComboType.Invalid) return true;

        // 王炸最大。同为王炸时比张数（2v2 口径下 4 张王压 3 张王；
        // 老口径两边张数一样大 → 压不了，保持原样）
        if (candidate.Type == ComboType.Rocket)
            return previous.Type != ComboType.Rocket || candidate.CardCount > previous.CardCount;
        if (previous.Type == ComboType.Rocket) return false;

        // 炸弹：压一切非炸弹；炸弹之间先比张数再比点数
        if (candidate.Type == ComboType.Bomb)
        {
            if (previous.Type != ComboType.Bomb) return true;
            if (candidate.CardCount != previous.CardCount) return candidate.CardCount > previous.CardCount;
            return candidate.MainRank > previous.MainRank;
        }
        if (previous.Type == ComboType.Bomb) return false;

        // 其余：必须同牌型、同张数，再比点数
        if (candidate.Type != previous.Type) return false;
        if (candidate.CardCount != previous.CardCount) return false;
        return candidate.MainRank > previous.MainRank;
    }

    /// <summary>一步到位：这组牌现在能不能出。</summary>
    public bool CanPlay(IEnumerable<Card> cards, Combo? previous, out Combo? parsed)
    {
        parsed = TryParse(cards);
        return Beats(parsed, previous);
    }

    // —————————————————————————— 内部工具 ——————————————————————————

    private static int TripleRank(Dictionary<int, int> byRank)
        => byRank.First(kv => kv.Value == 3).Key;

    private static bool IsConsecutive(IReadOnlyList<int> sortedRanks)
    {
        for (var i = 1; i < sortedRanks.Count; i++)
            if (sortedRanks[i] != sortedRanks[i - 1] + 1)
                return false;
        return true;
    }

    /// <summary>飞机的链：这 k 个连续点数是不是每个都"正好 3 张"。</summary>
    private static bool ChainIsExactlyThrees(Dictionary<int, int> byRank, int start, int k)
    {
        for (var r = start; r < start + k; r++)
            if (!byRank.TryGetValue(r, out var c) || c != 3)
                return false;
        return true;
    }

    /// <summary>刨掉飞机的链之后，剩下的牌（点数 → 张数）。剩下的就是"带牌"。</summary>
    private static Dictionary<int, int> LeftoverOf(Dictionary<int, int> byRank, int start, int k)
    {
        var leftover = new Dictionary<int, int>(byRank);
        for (var r = start; r < start + k; r++)
            leftover.Remove(r);
        return leftover;
    }

    /// <summary>
    /// 某个点数最多能拿出几张当"带牌"。对应 rlcard 里 solo_attachments 的两条过滤：
    /// <list type="bullet">
    ///   <item>第 4 张永远拿不出来 → 带牌不能是炸弹本体，上限 3。</item>
    ///   <item>跟飞机贴边的点数，第 3 张会被跳过 → 上限 2（'2' 例外）。</item>
    /// </list>
    /// </summary>
    private static int MaxKickerCards(int rank, int start, int k)
    {
        if (rank == Ranks.Two) return 3;
        var adjacent = rank == start - 1 || rank == start + k;
        return adjacent ? 2 : 3;
    }

    /// <summary>飞机带单：k 组连续三张 + k 张单。</summary>
    private static bool TryPlaneWithSingles(Dictionary<int, int> byRank, int k, out int topRank)
    {
        topRank = 0;
        for (var start = 3; start + k - 1 <= Ranks.Ace; start++)
        {
            if (!ChainIsExactlyThrees(byRank, start, k)) continue;
            var leftover = LeftoverOf(byRank, start, k);
            if (leftover.Values.Sum() != k) continue;
            if (leftover.Any(kv => kv.Value > MaxKickerCards(kv.Key, start, k))) continue;
            if (leftover.ContainsKey(Ranks.SmallJoker) && leftover.ContainsKey(Ranks.BigJoker)) continue;   // 带牌不能是双王
            topRank = start + k - 1;
            return true;
        }
        return false;
    }

    /// <summary>飞机带对：k 组连续三张 + k 对。</summary>
    private static bool TryPlaneWithPairs(Dictionary<int, int> byRank, int k, out int topRank)
    {
        topRank = 0;
        for (var start = 3; start + k - 1 <= Ranks.Ace; start++)
        {
            if (!ChainIsExactlyThrees(byRank, start, k)) continue;
            var leftover = LeftoverOf(byRank, start, k);
            if (leftover.Count != k || leftover.Values.Any(v => v != 2)) continue;
            if (leftover.Keys.Any(r => r >= Ranks.SmallJoker)) continue;                                      // 王配不成对
            topRank = start + k - 1;
            return true;
        }
        return false;
    }
}
