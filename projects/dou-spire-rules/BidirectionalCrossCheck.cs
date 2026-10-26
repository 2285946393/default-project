using System.Text;

namespace DouSpireRules;

/// <summary>
/// 双向对车：拿 rlcard **真实的判牌函数**输出的"手牌 → 可出的牌"来双向验证我们的判定器。
///
/// 跟 <see cref="RlcardCrossCheck"/> 的区别（那个是单向的）：
///   · RlcardCrossCheck 只问「这张表里的出法，我认不认」——表里全是合法项，验不出"我多认了什么"。
///   · 本文件把同一手牌的所有子集穷举一遍，既查"我漏认的"，也查"我瞎认的"。
///
/// 数据来源：tools/export_rlcard_playable.py 调用 rlcard 的
/// `DoudizhuJudger.playable_cards_from_hand`（就是真正决定玩家能点哪些牌的那个函数）。
///
/// 于是任何差异都是**真差异**，可以直接当规则分歧清单用。
/// </summary>
public static class BidirectionalCrossCheck
{
    /// <summary>点数 → rlcard 用的字符。</summary>
    private static char RankChar(int rank) => rank switch
    {
        10 => 'T',
        11 => 'J',
        12 => 'Q',
        13 => 'K',
        14 => 'A',
        15 => '2',
        16 => 'B',   // 小王
        17 => 'R',   // 大王
        _ => (char)('0' + rank),
    };

    /// <summary>点数 → 排序权重（跟 rlcard 的 CARD_RANK_STR 顺序一致）。</summary>
    private static int RankOrder(int rank) => rank - 3;

    /// <summary>把一组牌拼成 rlcard 风格的牌串（这个就是比对用的规范形式）。</summary>
    public static string Canon(IEnumerable<Card> cards)
        => string.Concat(cards.Select(c => RankChar(c.Rank))
                              .OrderBy(ch => ch switch
                              {
                                  'T' => 7, 'J' => 8, 'Q' => 9, 'K' => 10,
                                  'A' => 11, '2' => 12, 'B' => 13, 'R' => 14,
                                  _ => ch - '3',
                              }));

    /// <summary>分歧的"形状"签名，用来把成千条差异归成几类。</summary>
    private static string Signature(Combo c)
    {
        var counts = c.Cards.GroupBy(x => x.Rank)
                            .Select(g => g.Count())
                            .OrderByDescending(n => n)
                            .ToList();
        return $"{c.TypeName} | 张数分布 {string.Join("-", counts)}";
    }

    public sealed record Divergence(string Hand, string Combo, string TypeName, string Signature);

    public sealed record Result(
        int Hands,
        int SubsetsChecked,
        int MineAccepted,
        int RlcardAccepted,
        List<Divergence> OnlyMine,       // 我认、rlcard 不认
        List<Divergence> OnlyRlcard)     // rlcard 认、我不认
    {
        public int TotalDiff => OnlyMine.Count + OnlyRlcard.Count;
    }

    public static Result? Run(string tsvPath, int maxHands, Action<string> report)
    {
        if (!File.Exists(tsvPath))
        {
            report($"⚠️ 找不到 {tsvPath} —— 先跑 tools/export_rlcard_playable.py");
            return null;
        }

        // —— 读 tsv：hand \t combo ——
        var byHand = new Dictionary<string, HashSet<string>>();
        foreach (var line in File.ReadLines(tsvPath, Encoding.UTF8))
        {
            if (line.Length == 0) continue;
            var i = line.IndexOf('\t');
            if (i < 0) continue;
            var hand = line[..i];
            var combo = line[(i + 1)..];
            if (!byHand.TryGetValue(hand, out var set)) byHand[hand] = set = new HashSet<string>();
            set.Add(combo);
        }

        var judge = new ComboJudge(deckCopies: 1);
        var onlyMine = new List<Divergence>();
        var onlyRlcard = new List<Divergence>();
        var subsetsChecked = 0L;
        var mineAccepted = 0L;
        var rlcardAccepted = 0L;
        var handsDone = 0;

        foreach (var (hand, rlcardSet) in byHand)
        {
            if (handsDone >= maxHands) break;
            handsDone++;

            var cards = Card.ParseHand(hand);
            var n = cards.Count;
            if (n is < 1 or > 20) continue;

            var mine = new Dictionary<string, Combo>();
            var limit = 1 << n;
            for (var mask = 1; mask < limit; mask++)
            {
                var sel = new List<Card>(n);
                for (var b = 0; b < n; b++)
                    if ((mask & (1 << b)) != 0) sel.Add(cards[b]);

                var c = judge.TryParse(sel);
                if (c is null) continue;
                mine[Canon(sel)] = c;
            }

            subsetsChecked += limit - 1;
            mineAccepted += mine.Count;
            rlcardAccepted += rlcardSet.Count;

            foreach (var (combo, c) in mine)
                if (!rlcardSet.Contains(combo))
                    onlyMine.Add(new Divergence(hand, combo, c.TypeName, Signature(c)));

            foreach (var combo in rlcardSet)
                if (!mine.ContainsKey(combo))
                    onlyRlcard.Add(new Divergence(hand, combo, "?", "?"));
        }

        return new Result(handsDone, (int)subsetsChecked, (int)mineAccepted, (int)rlcardAccepted, onlyMine, onlyRlcard);
    }
}
