namespace DouSpireRules;

/// <summary>一手出牌：具体是哪几张牌 + 判出来的牌型。</summary>
public sealed record Move(Combo Combo, List<Card> Cards);

/// <summary>
/// 从手牌里找出"能压住场上这手牌"的候选。
///
/// 判定器（<see cref="ComboJudge"/>）回答的是"这手牌合不合法"，
/// 但 AI 和"出牌提示"需要的是"我手上有什么能压" —— 这是两件事。
///
/// 这里生成的候选**一律过一遍判定器**再返回，所以永远不会给出非法出牌。
/// 生成器可以写得很粗糙，判定器会兜底 —— 判定器已经被 rlcard 拷问过 970 万次了。
/// </summary>
public static class MoveGenerator
{
    private static Dictionary<int, int> Counts(IReadOnlyList<Card> hand)
    {
        var d = new Dictionary<int, int>();
        foreach (var c in hand)
            d[c.Rank] = d.TryGetValue(c.Rank, out var v) ? v + 1 : 1;
        return d;
    }

    private static List<Card> Take(IReadOnlyList<Card> hand, int rank, int n)
    {
        var res = new List<Card>(n);
        foreach (var c in hand)
            if (c.Rank == rank && res.Count < n) res.Add(c);
        return res;
    }

    /// <summary>挑 n 张最不值钱的散牌当"带牌"。绝不拆炸弹（4 张的那个点数直接跳过）。</summary>
    private static List<Card>? PickSingles(IReadOnlyList<Card> hand, Dictionary<int, int> counts, HashSet<int> exclude, int n)
    {
        var picked = new List<Card>();
        var order = counts.Where(kv => !exclude.Contains(kv.Key) && kv.Value is > 0 and < 4)
                          .OrderBy(kv => kv.Value)          // 先拆孤张，再拆对子，最后拆三张
                          .ThenBy(kv => kv.Key);
        foreach (var (rank, cnt) in order)
        {
            if (picked.Count >= n) break;
            picked.AddRange(Take(hand, rank, Math.Min(cnt, n - picked.Count)));
        }
        return picked.Count == n ? picked : null;
    }

    /// <summary>挑 n 个对子当"带牌"（优先用本来就是对子的点数）。</summary>
    private static List<Card>? PickPairs(IReadOnlyList<Card> hand, Dictionary<int, int> counts, HashSet<int> exclude, int n)
    {
        var picked = new List<Card>();
        var order = counts.Where(kv => !exclude.Contains(kv.Key) && kv.Value >= 2)
                          .OrderBy(kv => kv.Value)          // 2 张的先动
                          .ThenBy(kv => kv.Key);
        foreach (var (rank, _) in order)
        {
            if (picked.Count >= 2 * n) break;
            picked.AddRange(Take(hand, rank, 2));
        }
        return picked.Count == 2 * n ? picked : null;
    }

    /// <summary>连续段（输入已升序，输出每一段）。</summary>
    private static IEnumerable<List<int>> Runs(IReadOnlyList<int> sorted)
    {
        var cur = new List<int>();
        foreach (var r in sorted)
        {
            if (cur.Count > 0 && r != cur[^1] + 1) { yield return cur; cur = new(); }
            cur.Add(r);
        }
        if (cur.Count > 0) yield return cur;
    }

    private static int Priority(ComboType t) => t switch
    {
        ComboType.Bomb => 1,
        ComboType.Rocket => 2,
        _ => 0,
    };

    /// <summary>
    /// 找出所有能压过 <paramref name="target"/> 的出牌。
    /// 返回结果按"最省"排序：先用同牌型里最小的，炸弹/王炸垫底。
    /// </summary>
    public static List<Move> Beating(IReadOnlyList<Card> hand, Combo target, ComboJudge judge)
    {
        var counts = Counts(hand);
        var moves = new List<Move>();

        void Add(List<Card>? cards)
        {
            if (cards is null || cards.Count == 0) return;
            var c = judge.TryParse(cards);
            if (c is null) return;
            if (!ComboJudge.Beats(c, target)) return;
            moves.Add(new Move(c, cards));
        }

        var ranksAsc = counts.Keys.OrderBy(r => r).ToList();

        switch (target.Type)
        {
            case ComboType.Single:
                foreach (var r in ranksAsc.Where(r => r > target.MainRank)) Add(Take(hand, r, 1));
                break;

            case ComboType.Pair:
                foreach (var r in ranksAsc.Where(r => r > target.MainRank && counts[r] >= 2)) Add(Take(hand, r, 2));
                break;

            case ComboType.Triple:
                foreach (var r in ranksAsc.Where(r => r > target.MainRank && counts[r] >= 3)) Add(Take(hand, r, 3));
                break;

            case ComboType.TripleWithSingle:
                foreach (var r in ranksAsc.Where(r => r > target.MainRank && counts[r] >= 3))
                {
                    var head = Take(hand, r, 3);
                    foreach (var k in PickSingles(hand, counts, new HashSet<int> { r }, 1) ?? new List<Card>())
                        Add(new List<Card>(head) { k });
                }
                break;

            case ComboType.TripleWithPair:
                foreach (var r in ranksAsc.Where(r => r > target.MainRank && counts[r] >= 3))
                {
                    var head = Take(hand, r, 3);
                    var wing = PickPairs(hand, counts, new HashSet<int> { r }, 1);
                    if (wing is not null) Add(head.Concat(wing).ToList());
                }
                break;

            case ComboType.Straight:
            {
                var len = target.CardCount;
                for (var s = 3; s + len - 1 <= Ranks.Ace; s++)
                {
                    if (s + len - 1 <= target.MainRank) continue;
                    var cards = new List<Card>();
                    for (var r = s; r < s + len; r++)
                    {
                        if (!counts.ContainsKey(r)) { cards.Clear(); break; }
                        cards.AddRange(Take(hand, r, 1));
                    }
                    Add(cards);
                }
                break;
            }

            case ComboType.ConsecutivePairs:
            {
                var pairs = target.CardCount / 2;
                for (var s = 3; s + pairs - 1 <= Ranks.Ace; s++)
                {
                    if (s + pairs - 1 <= target.MainRank) continue;
                    var cards = new List<Card>();
                    for (var r = s; r < s + pairs; r++)
                    {
                        if (!counts.TryGetValue(r, out var n) || n < 2) { cards.Clear(); break; }
                        cards.AddRange(Take(hand, r, 2));
                    }
                    Add(cards);
                }
                break;
            }

            case ComboType.Plane:
            {
                var k = target.CardCount / 3;
                for (var s = 3; s + k - 1 <= Ranks.Ace; s++)
                {
                    if (s + k - 1 <= target.MainRank) continue;
                    var cards = new List<Card>();
                    for (var r = s; r < s + k; r++)
                    {
                        if (!counts.TryGetValue(r, out var n) || n < 3) { cards.Clear(); break; }
                        cards.AddRange(Take(hand, r, 3));
                    }
                    Add(cards);
                }
                break;
            }

            case ComboType.PlaneWithSingles:
            case ComboType.PlaneWithPairs:
            {
                var isPairs = target.Type == ComboType.PlaneWithPairs;
                var k = target.CardCount / (isPairs ? 5 : 4);
                for (var s = 3; s + k - 1 <= Ranks.Ace; s++)
                {
                    if (s + k - 1 <= target.MainRank) continue;
                    var chain = new List<Card>();
                    var chainRanks = new HashSet<int>();
                    var ok = true;
                    for (var r = s; r < s + k; r++)
                    {
                        if (!counts.TryGetValue(r, out var n) || n < 3) { ok = false; break; }
                        chain.AddRange(Take(hand, r, 3));
                        chainRanks.Add(r);
                    }
                    if (!ok) continue;
                    var wing = isPairs ? PickPairs(hand, counts, chainRanks, k) : PickSingles(hand, counts, chainRanks, k);
                    if (wing is not null) Add(chain.Concat(wing).ToList());
                }
                break;
            }

            case ComboType.FourWithTwoSingles:
            case ComboType.FourWithTwoPairs:
            {
                var isPairs = target.Type == ComboType.FourWithTwoPairs;
                foreach (var r in ranksAsc.Where(r => r > target.MainRank && counts[r] >= 4))
                {
                    var four = Take(hand, r, 4);
                    var wing = isPairs ? PickPairs(hand, counts, new HashSet<int> { r }, 2)
                                       : PickSingles(hand, counts, new HashSet<int> { r }, 2);
                    if (wing is not null) Add(four.Concat(wing).ToList());
                }
                break;
            }

            case ComboType.Rocket:
            {
                // 2v2 口径下王炸能被更大的王炸压（3 张王 < 4 张王）
                var jokers = hand.Where(c => c.IsJoker).ToList();
                if (jokers.Count > target.CardCount) Add(jokers);
                break;
            }
        }

        // 炸弹 / 王炸随时能救场
        if (target.Type != ComboType.Rocket)
        {
            foreach (var r in ranksAsc.Where(r => counts[r] >= 4))
                if (target.Type != ComboType.Bomb || r > target.MainRank)
                    Add(Take(hand, r, 4));

            if (counts.ContainsKey(Ranks.SmallJoker) && counts.ContainsKey(Ranks.BigJoker))
            {
                var jokers = hand.Where(c => c.IsJoker).ToList();
                // 2v2 口径（RocketJokers ≥ 3）：手里的王凑够门槛就一起走
                if (judge.RocketJokers >= 3 && jokers.Count >= judge.RocketJokers)
                    Add(jokers);
                // 老口径王炸（一副牌两张王）；两副牌下这两张也能当一对出
                Add(new List<Card> { Card.SmallJoker, Card.BigJoker });
            }
        }

        return moves.OrderBy(m => Priority(m.Combo.Type))
                    .ThenBy(m => m.Combo.MainRank)
                    .ThenBy(m => m.Combo.CardCount)
                    .ToList();
    }
}
