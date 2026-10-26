namespace DouSpireRules;

/// <summary>
/// 牌型档位，**从小到大**（数值越大越强）。
///
/// ⚠️ 注意：**"皇家同花顺"不单列一档** —— 它本质上就是"A 高的同花顺"，
/// 比法完全一样（同花顺比最大那张）。所以只在**显示名字**的时候特判成"皇家同花顺"
/// （见 <see cref="HandEval.Name"/>），比大小的时候它就是 StraightFlush。
/// </summary>
public enum HandCategory
{
    /// <summary>高牌：啥都没成，比最大那张。</summary>
    HighCard = 0,

    /// <summary>一对。</summary>
    Pair = 1,

    /// <summary>两对。</summary>
    TwoPair = 2,

    /// <summary>三条。</summary>
    Trips = 3,

    /// <summary>顺子（5 张连续点数，不分花色）。</summary>
    Straight = 4,

    /// <summary>同花（5 张同花色，不连续）。</summary>
    Flush = 5,

    /// <summary>葫芦（三条 + 一对）。</summary>
    FullHouse = 6,

    /// <summary>四条（四张同点数）。</summary>
    Quads = 7,

    /// <summary>同花顺（5 张同花色且连续）。A 高时就是"皇家同花顺"。</summary>
    StraightFlush = 8,
}

/// <summary>
/// 一手 5 张牌的牌力，**可以直接比大小**（<see cref="CompareTo"/>）。
///
/// <para><b>Category</b>：牌型档位。</para>
/// <para><b>K1..K5</b>：比大小时依次比较的"关键点数"，已按优先级排好。</para>
/// 例：两对 A 和 K，带一张 7 → K1=A, K2=K, K3=7，其余 0。
/// </summary>
public readonly record struct HandValue(HandCategory Category, int K1, int K2, int K3, int K4, int K5)
    : IComparable<HandValue>
{
    public int CompareTo(HandValue o)
    {
        var c = ((int)Category).CompareTo((int)o.Category);
        if (c != 0) return c;
        c = K1.CompareTo(o.K1); if (c != 0) return c;
        c = K2.CompareTo(o.K2); if (c != 0) return c;
        c = K3.CompareTo(o.K3); if (c != 0) return c;
        c = K4.CompareTo(o.K4); if (c != 0) return c;
        return K5.CompareTo(o.K5);
    }

    public static bool operator >(HandValue a, HandValue b) => a.CompareTo(b) > 0;
    public static bool operator <(HandValue a, HandValue b) => a.CompareTo(b) < 0;
    public static bool operator >=(HandValue a, HandValue b) => a.CompareTo(b) >= 0;
    public static bool operator <=(HandValue a, HandValue b) => a.CompareTo(b) <= 0;
}

/// <summary>
/// 评牌器：从 5~7 张牌里挑出**最好的 5 张**，给一个可比较的 <see cref="HandValue"/>。
///
/// 做法很朴素但绝对正确：**把 5 张的所有组合都评一遍，取最大的**。
/// 7 张只有 21 种组合，快得看不出来 —— 比手写 7 张专用求值器（容易写错）稳得多。
/// </summary>
public static class HandEval
{
    /// <summary>
    /// 从 <paramref name="cards"/>（5~7 张）里挑出最好的 5 张。
    /// <paramref name="bestFive"/> 输出**具体是哪 5 张**（UI 要高亮它们）。
    /// </summary>
    public static HandValue Best(IReadOnlyList<PokerCard> cards, out PokerCard[] bestFive)
    {
        var n = cards.Count;
        if (n < 5) throw new ArgumentException($"至少 5 张牌才能评，给了 {n} 张");

        if (n == 5)
        {
            bestFive = new[] { cards[0], cards[1], cards[2], cards[3], cards[4] };
            return Evaluate5(cards[0], cards[1], cards[2], cards[3], cards[4]);
        }

        // 6 张 = 去掉 1 张（6 种）；7 张 = 去掉 2 张（21 种）。都不重不漏。
        var buf = new PokerCard[5];
        var best = default(HandValue);
        var keep = new PokerCard[5];
        var first = true;

        if (n == 6)
        {
            for (var skip = 0; skip < 6; skip++)
            {
                var w = 0;
                for (var i = 0; i < 6; i++)
                    if (i != skip) buf[w++] = cards[i];
                var v = Evaluate5(buf[0], buf[1], buf[2], buf[3], buf[4]);
                if (first || v.CompareTo(best) > 0)
                {
                    best = v;
                    Array.Copy(buf, keep, 5);
                    first = false;
                }
            }
        }
        else if (n == 7)
        {
            for (var a = 0; a < 6; a++)
                for (var b = a + 1; b < 7; b++)
                {
                    var w = 0;
                    for (var i = 0; i < 7; i++)
                        if (i != a && i != b) buf[w++] = cards[i];
                    var v = Evaluate5(buf[0], buf[1], buf[2], buf[3], buf[4]);
                    if (first || v.CompareTo(best) > 0)
                    {
                        best = v;
                        Array.Copy(buf, keep, 5);
                        first = false;
                    }
                }
        }
        else
        {
            // 8 张以上（理论上用不到）—— 通用组合兜底
            foreach (var combo in Combinations(n, 5))
            {
                var v = Evaluate5(cards[combo[0]], cards[combo[1]], cards[combo[2]], cards[combo[3]], cards[combo[4]]);
                if (first || v.CompareTo(best) > 0)
                {
                    best = v;
                    for (var i = 0; i < 5; i++) keep[i] = cards[combo[i]];
                    first = false;
                }
            }
        }

        bestFive = keep;
        return best;
    }

    private static IEnumerable<int[]> Combinations(int n, int k)
    {
        var idx = new int[k];
        for (var i = 0; i < k; i++) idx[i] = i;
        while (true)
        {
            yield return (int[])idx.Clone();
            var p = k - 1;
            while (p >= 0 && idx[p] == n - k + p) p--;
            if (p < 0) yield break;
            idx[p]++;
            for (var i = p + 1; i < k; i++) idx[i] = idx[i - 1] + 1;
        }
    }

    /// <summary>评**恰好 5 张**。</summary>
    public static HandValue Evaluate5(PokerCard a, PokerCard b, PokerCard c, PokerCard d, PokerCard e)
    {
        // —— 点数降序排好 ——
        Span<int> r = stackalloc int[5] { a.Rank, b.Rank, c.Rank, d.Rank, e.Rank };
        for (var i = 1; i < 5; i++)
        {
            var v = r[i];
            var j = i - 1;
            while (j >= 0 && r[j] < v) { r[j + 1] = r[j]; j--; }
            r[j + 1] = v;
        }

        var flush = a.Suit == b.Suit && b.Suit == c.Suit && c.Suit == d.Suit && d.Suit == e.Suit;

        // —— 顺子（必须 5 张点数全不同）——
        var straightHigh = 0;
        if (r[0] != r[1] && r[1] != r[2] && r[2] != r[3] && r[3] != r[4])
        {
            if (r[0] - r[4] == 4) straightHigh = r[0];
            // ★ 轮子：A-5-4-3-2 是**最小**的顺子（5 高），不是"K-A-2-3-4"那种绕圈
            else if (r[0] == 14 && r[1] == 5) straightHigh = 5;
        }

        if (flush && straightHigh > 0) return new HandValue(HandCategory.StraightFlush, straightHigh, 0, 0, 0, 0);
        if (flush) return new HandValue(HandCategory.Flush, r[0], r[1], r[2], r[3], r[4]);
        if (straightHigh > 0) return new HandValue(HandCategory.Straight, straightHigh, 0, 0, 0, 0);

        // —— 分组：按（出现次数降序，同次数按点数降序）——
        Span<int> cnt = stackalloc int[15];
        cnt[a.Rank]++; cnt[b.Rank]++; cnt[c.Rank]++; cnt[d.Rank]++; cnt[e.Rank]++;

        Span<int> gr = stackalloc int[5];
        Span<int> gc = stackalloc int[5];
        var ng = 0;
        for (var rank = 14; rank >= 2; rank--)
            if (cnt[rank] > 0) { gr[ng] = rank; gc[ng] = cnt[rank]; ng++; }

        // 稳定插入排序：次数大的往前；同次数保持上面的"点数降序"
        for (var i = 1; i < ng; i++)
        {
            int vr = gr[i], vc = gc[i];
            var j = i - 1;
            while (j >= 0 && gc[j] < vc) { gr[j + 1] = gr[j]; gc[j + 1] = gc[j]; j--; }
            gr[j + 1] = vr; gc[j + 1] = vc;
        }

        if (gc[0] == 4) return new HandValue(HandCategory.Quads, gr[0], gr[1], 0, 0, 0);
        if (gc[0] == 3 && ng >= 2 && gc[1] == 2) return new HandValue(HandCategory.FullHouse, gr[0], gr[1], 0, 0, 0);
        if (gc[0] == 3) return new HandValue(HandCategory.Trips, gr[0], gr[1], gr[2], 0, 0);
        if (gc[0] == 2 && ng >= 2 && gc[1] == 2) return new HandValue(HandCategory.TwoPair, gr[0], gr[1], gr[2], 0, 0);
        if (gc[0] == 2) return new HandValue(HandCategory.Pair, gr[0], gr[1], gr[2], gr[3], 0);
        return new HandValue(HandCategory.HighCard, r[0], r[1], r[2], r[3], r[4]);
    }

    /// <summary>牌型的**全名**（带点数）—— 例："葫芦 Q 带 8"、"同花 A 高"、"皇家同花顺"。</summary>
    public static string Name(HandValue v)
    {
        static string R(int x) => PokerCard.RankName(x);
        return v.Category switch
        {
            HandCategory.HighCard => $"高牌 {R(v.K1)}",
            HandCategory.Pair => $"一对 {R(v.K1)}",
            HandCategory.TwoPair => $"两对 {R(v.K1)} 和 {R(v.K2)}",
            HandCategory.Trips => $"三条 {R(v.K1)}",
            HandCategory.Straight => $"顺子 {R(v.K1)} 高",
            HandCategory.Flush => $"同花 {R(v.K1)} 高",
            HandCategory.FullHouse => $"葫芦 {R(v.K1)} 带 {R(v.K2)}",
            HandCategory.Quads => $"四条 {R(v.K1)}",
            HandCategory.StraightFlush => v.K1 == 14 ? "皇家同花顺" : $"同花顺 {R(v.K1)} 高",
            _ => "—",
        };
    }

    /// <summary>牌型的**短名**（不带点数）—— 用在牌桌角落的小标签上。</summary>
    public static string ShortName(HandValue v) => v.Category switch
    {
        HandCategory.HighCard => "高牌",
        HandCategory.Pair => "一对",
        HandCategory.TwoPair => "两对",
        HandCategory.Trips => "三条",
        HandCategory.Straight => "顺子",
        HandCategory.Flush => "同花",
        HandCategory.FullHouse => "葫芦",
        HandCategory.Quads => "四条",
        HandCategory.StraightFlush => v.K1 == 14 ? "皇家同花顺" : "同花顺",
        _ => "—",
    };

    /// <summary>牌型档位的中文名（给"牌型说明"面板用）。</summary>
    public static string CategoryName(HandCategory c) => c switch
    {
        HandCategory.HighCard => "高牌",
        HandCategory.Pair => "一对",
        HandCategory.TwoPair => "两对",
        HandCategory.Trips => "三条",
        HandCategory.Straight => "顺子",
        HandCategory.Flush => "同花",
        HandCategory.FullHouse => "葫芦",
        HandCategory.Quads => "四条",
        HandCategory.StraightFlush => "同花顺",
        _ => "—",
    };
}
