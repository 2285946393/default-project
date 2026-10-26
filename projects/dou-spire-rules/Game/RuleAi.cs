namespace DouSpireRules;

/// <summary>
/// 电脑出牌。策略照抄 rlcard 的规则型 AI（<c>doudizhu_rule_models.py</c>），但修了两处它的毛病：
///
/// <list type="number">
///   <item><b>抢队友的牌</b>：rlcard 只要能用同牌型压住就压，不管上家是不是队友。
///         这里改成"两个农民之间不互相压"（能一把走完除外）。</item>
///   <item><b>拿王炸压炸弹</b>：rlcard 的 AI 只比对同牌型族，所以它永远不会用王炸去压炸弹。
///         这里允许。</item>
/// </list>
///
/// 策略本身很朴素，但打起来"像个人"：领出时甩最小的那组，跟牌时能压就压最小的，不浪费大牌。
/// 想更聪明以后可以换 DouZero。
/// </summary>
public static class RuleAi
{
    /// <summary>决定这一步出什么。<c>null</c> = 要不起。</summary>
    public static List<Card>? Choose(GameState g)
    {
        var hand = g.Me.Hand;
        if (hand.Count == 0) return new List<Card>();

        // —— 该我领出：甩包含"手里最小那张牌"的那一组 ——
        if (g.MustLead) return ChooseLead(hand, g.Judge);

        // —— 跟牌 ——
        var target = g.LastPlay!;
        var moves = MoveGenerator.Beating(hand, target, g.Judge);

        // 能一把走完就走完（赢牌优先，管他是不是队友）
        var finish = moves.FirstOrDefault(m => m.Cards.Count == hand.Count);
        if (finish is not null) return finish.Cards;

        // 队友的牌不抢
        var holderIsTeammate = g.TrickHolder >= 0
                               && g.Players[g.TrickHolder].SameSideAs(g.Me)
                               && !g.Me.IsLandlord;
        if (holderIsTeammate) return null;

        // 能压就压最省的那手
        var sameType = moves.Where(m => m.Combo.Type == target.Type).ToList();
        if (sameType.Count > 0)
            return sameType.OrderBy(m => m.Combo.MainRank).First().Cards;

        // 压不住：只有对手快走完了才动炸弹
        var holder = g.Players[g.TrickHolder];
        if (!holder.SameSideAs(g.Me) && holder.Hand.Count <= 2)
        {
            var bomb = moves.FirstOrDefault(m => m.Combo.Type is ComboType.Bomb or ComboType.Rocket);
            if (bomb is not null) return bomb.Cards;
        }
        return null;
    }

    /// <summary>
    /// 2v2 开局扣牌：挑**最不亏**的一张 —— 优先扔"最小点数的孤张"（这个点数就这一张，拆了不心疼），
    /// 没有孤张就扔最小点数的随便一张。
    /// </summary>
    public static Card ChooseDiscard(IReadOnlyList<Card> hand)
    {
        var groups = hand.GroupBy(c => c.Rank).ToList();
        var lonely = groups.Where(g => g.Count() == 1).OrderBy(g => g.Key).ToList();
        if (lonely.Count > 0) return lonely[0].First();
        return hand.OrderBy(c => c.Rank).First();
    }

    /// <summary>
    /// 领出：**优先甩"大牌型"**（飞机 / 顺子 / 连对 / 三张这种一次走几张的），
    /// 没有大牌型才退回去出"含最小牌"的那一组。
    /// 用户 2026-10-07："不需要接牌应该出最大的。飞机顺子这种"。
    /// 注意**炸弹 / 王炸不算**在这里 —— 那是留着救命的，不主动甩。
    /// </summary>
    public static List<Card> ChooseLead(IReadOnlyList<Card> hand, ComboJudge judge)
    {
        var groups = Decompose(hand, judge);

        // ① 先找大牌型：张数越多的越优先（飞机 > 顺子 > 连对 > 三张）
        List<Card>? big = null;
        var bigCount = 0;
        foreach (var g in groups)
        {
            var p = judge.TryParse(g);
            if (p is null) continue;
            var isBig = p.Type is ComboType.Plane or ComboType.PlaneWithSingles or ComboType.PlaneWithPairs
                or ComboType.Straight or ComboType.ConsecutivePairs
                or ComboType.Triple or ComboType.TripleWithSingle or ComboType.TripleWithPair;
            if (!isBig) continue;
            if (g.Count > bigCount) { big = g; bigCount = g.Count; }
        }
        if (big is not null) return AttachKickers(big, hand, judge);

        // ② 没有大牌型 → 老规矩：甩含最小牌的那一组
        var minRank = hand.Min(c => c.Rank);
        List<Card>? grp = null;
        foreach (var g in groups)
            if (g.Any(c => c.Rank == minRank)) { grp = g; break; }
        grp ??= new List<Card> { hand.OrderBy(c => c.Rank).First() };

        return AttachKickers(grp, hand, judge);
    }

    /// <summary>
    /// 领出时，三张 / 飞机尽量**带上单牌**（三带一 / 飞机带单）—— 能多甩一张是一张。
    /// （用户 2026-10-06："提示打 3 个为什么不是三带一"）
    /// 带的牌挑最小的、而且**不拿 2 / 王**去带（那些是大牌，留着压人）。
    /// 带上之后要是构不成合法牌型，就原样返回。
    /// </summary>
    private static List<Card> AttachKickers(List<Card> grp, IReadOnlyList<Card> hand, ComboJudge judge)
    {
        var parsed = judge.TryParse(grp);
        if (parsed is null) return grp;

        var need = parsed.Type switch
        {
            ComboType.Triple => 1,
            ComboType.Plane => parsed.ChainLength,
            _ => 0,
        };
        if (need == 0) return grp;

        var wantType = parsed.Type == ComboType.Triple ? ComboType.TripleWithSingle : ComboType.PlaneWithSingles;

        var used = new HashSet<Card>(grp);
        var kickers = hand.Where(c => !used.Contains(c) && c.Rank <= Ranks.Ace)
                          .OrderBy(c => c.Rank)
                          .Take(need)
                          .ToList();
        if (kickers.Count < need) return grp;

        var with = grp.Concat(kickers).ToList();
        var check = judge.TryParse(with);
        return check is not null && check.Type == wantType ? with : grp;
    }

    /// <summary>
    /// 贪心拆牌：王炸 → 炸弹 → 飞机/三张 → 顺子 → 连对 → 对子 → 单张。
    /// 拆完每一组都过判定器，判不出牌型的直接拆成单张 —— 保证 AI 提不出非法出牌。
    /// </summary>
    public static List<List<Card>> Decompose(IReadOnlyList<Card> hand, ComboJudge judge)
    {
        var groups = new List<List<Card>>();
        var rest = new Dictionary<int, List<Card>>();
        foreach (var c in hand)
        {
            if (!rest.TryGetValue(c.Rank, out var l)) rest[c.Rank] = l = new List<Card>();
            l.Add(c);
        }

        int Count(int r) => rest.TryGetValue(r, out var l) ? l.Count : 0;
        List<Card> TakeN(int r, int n)
        {
            var l = rest[r];
            var t = l.Take(n).ToList();
            l.RemoveRange(0, t.Count);
            if (l.Count == 0) rest.Remove(r);
            return t;
        }

        // 1) 王炸
        if (Count(Ranks.SmallJoker) > 0 && Count(Ranks.BigJoker) > 0)
            groups.Add(new List<Card> { TakeN(Ranks.SmallJoker, 1)[0], TakeN(Ranks.BigJoker, 1)[0] });

        // 2) 炸弹
        foreach (var r in rest.Keys.Where(r => Count(r) >= 4).OrderBy(r => r).ToList())
            groups.Add(TakeN(r, 4));

        // 3) 飞机 / 三张（连续的三张合成一条飞机）
        var triRanks = rest.Keys.Where(r => r <= Ranks.Ace && Count(r) >= 3).OrderBy(r => r).ToList();
        foreach (var run in ConsecutiveRuns(triRanks))
        {
            var cards = new List<Card>();
            foreach (var r in run) cards.AddRange(TakeN(r, 3));
            groups.Add(cards);
        }

        // 4) 顺子（3~A 连续 ≥5 个点数，各取 1 张）
        var soloRanks = rest.Keys.Where(r => r <= Ranks.Ace).OrderBy(r => r).ToList();
        foreach (var run in ConsecutiveRuns(soloRanks).Where(x => x.Count >= 5))
        {
            var cards = new List<Card>();
            foreach (var r in run) cards.AddRange(TakeN(r, 1));
            groups.Add(cards);
        }

        // 5) 连对（连续 ≥3 个点数，各取 2 张）
        var pairRanks = rest.Keys.Where(r => r <= Ranks.Ace && Count(r) >= 2).OrderBy(r => r).ToList();
        foreach (var run in ConsecutiveRuns(pairRanks).Where(x => x.Count >= 3))
        {
            var cards = new List<Card>();
            foreach (var r in run) cards.AddRange(TakeN(r, 2));
            groups.Add(cards);
        }

        // 6) 剩下的对子
        foreach (var r in rest.Keys.Where(r => Count(r) >= 2).OrderBy(r => r).ToList())
            groups.Add(TakeN(r, 2));

        // 7) 剩下的单张
        foreach (var r in rest.Keys.OrderBy(r => r).ToList())
            while (Count(r) > 0) groups.Add(TakeN(r, 1));

        // 兜底：判不出牌型的组，拆成单张
        var result = new List<List<Card>>();
        foreach (var g in groups)
        {
            if (judge.TryParse(g) is not null) result.Add(g);
            else foreach (var c in g) result.Add(new List<Card> { c });
        }
        return result;
    }

    private static IEnumerable<List<int>> ConsecutiveRuns(IReadOnlyList<int> sorted)
    {
        var cur = new List<int>();
        foreach (var r in sorted)
        {
            if (cur.Count > 0 && r != cur[^1] + 1) { yield return cur; cur = new List<int>(); }
            cur.Add(r);
        }
        if (cur.Count > 0) yield return cur;
    }
}
