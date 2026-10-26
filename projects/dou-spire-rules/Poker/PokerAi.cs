namespace DouSpireRules;

/// <summary>
/// 电脑怎么打德州。
///
/// ── 为什么不用"查表"那一套 ──────────────────────────────
/// 斗地主的 AI 是"牌力 → 出哪手"的映射，查表就行。
/// 德州不一样：**同样的牌，该不该跟注取决于"要花多少钱"** —— 这是**赔率**问题。
/// 所以这里用**蒙特卡洛**：随机模拟对手的底牌 + 剩下的公共牌几百次，
/// 算出"我这手牌大概有多少概率赢"，再拿它跟底池赔率比。
///
/// <para>★ **绝不偷看别人的底牌** —— 模拟时对手的牌是**随机抽**的，
/// 和真人的信息量完全一样。这条对联机公平性很重要。</para>
///
/// 风格（<see cref="Style"/>）只影响三件事：加注门槛、跟注的松紧、诈唬频率。
/// </summary>
public static class PokerAi
{
    /// <summary>电脑的性格。成员名故意写中文 —— 设置页下拉没本地化表时直接显示成员名。</summary>
    public enum Style
    {
        /// <summary>保守：牌不好就弃，很少诈唬。适合新手看。</summary>
        保守 = 0,

        /// <summary>正常：按赔率打，偶尔诈唬。</summary>
        正常 = 1,

        /// <summary>激进：敢加注、诈唬多。牌桌热闹。</summary>
        激进 = 2,
    }

    /// <summary>默认模拟次数。越大越准、越慢；160 次已经足够"像个人"。</summary>
    public const int DefaultRollouts = 160;

    private readonly record struct Tuning(double RaiseEquity, double CallMargin, double BluffChance, double BetFraction);

    private static Tuning Tune(Style style) => style switch
    {
        Style.保守 => new Tuning(RaiseEquity: 0.70, CallMargin: +0.06, BluffChance: 0.05, BetFraction: 0.55),
        Style.激进 => new Tuning(RaiseEquity: 0.50, CallMargin: -0.04, BluffChance: 0.24, BetFraction: 0.78),
        _ => new Tuning(RaiseEquity: 0.60, CallMargin: +0.01, BluffChance: 0.12, BetFraction: 0.65),
    };

    /// <summary>
    /// 决定这一步怎么走。
    /// 返回 <c>(动作, 金额)</c>；金额只在 <see cref="PokerAction.Bet"/> / <see cref="PokerAction.Raise"/>
    /// 时有意义，含义是"**加注到**多少"。
    /// </summary>
    public static (PokerAction Action, int Amount) Choose(HoldemGame g, Style style, Random rng,
        int rollouts = DefaultRollouts)
    {
        var seat = g.CurrentSeat;
        var me = g.Players[seat];
        var legal = g.LegalActions();
        if (legal.Count == 0) return (PokerAction.Check, 0);

        var t = Tune(style);
        var equity = Equity(g, seat, rollouts, rng);
        var toCall = g.CallCost(seat);
        var potOdds = toCall <= 0 ? 0.0 : (double)toCall / (g.Pot + toCall);

        var canBet = legal.Contains(PokerAction.Bet);
        var canRaise = legal.Contains(PokerAction.Raise);
        var canCheck = legal.Contains(PokerAction.Check);

        // ── 不用补钱：可以过牌，也可以主动下注/加注 ──
        // （大盲已经投了大盲注时也走这条：他不用补钱，但那 100 就是本轮的注，所以他是"加注"）
        if (toCall <= 0)
        {
            var strong = equity >= t.RaiseEquity;
            var bluff = !strong && rng.NextDouble() < t.BluffChance;

            if ((strong || bluff) && (canBet || canRaise))
            {
                var target = g.PotSizedRaiseTo(seat, strong ? t.BetFraction : t.BetFraction * 0.8);
                if (target > 0)
                    return (canBet ? PokerAction.Bet : PokerAction.Raise, target);
            }
            return canCheck ? (PokerAction.Check, 0) : (PokerAction.Fold, 0);
        }

        // ── 面对下注 ──

        // ① 牌够硬 → 加注（偶尔慢打，别每次都加，不然太容易被读）
        if (equity >= t.RaiseEquity && canRaise && rng.NextDouble() < 0.75)
        {
            var target = g.PotSizedRaiseTo(seat, t.BetFraction);
            if (target >= g.MaxRaiseTo(seat)) return (PokerAction.AllIn, 0);
            if (target > g.CurrentBet) return (PokerAction.Raise, target);
        }

        // ② 该不该跟：胜率要盖过底池赔率（外加风格带来的松紧）
        var need = potOdds + t.CallMargin;
        if (equity >= need)
        {
            // 跟注就要把身家全压上 → 只有真够硬才跟
            if (toCall >= me.Stack)
                return equity >= t.RaiseEquity ? (PokerAction.AllIn, 0) : (PokerAction.Fold, 0);
            return (PokerAction.Call, 0);
        }

        // ③ 不够 → 弃
        return (PokerAction.Fold, 0);
    }

    /// <summary>
    /// 蒙特卡洛算"我这手牌赢的概率"。
    ///
    /// 做法：把**已知的牌**（我的 2 张底牌 + 公共牌）从牌堆里去掉，
    /// 然后反复随机发对手的底牌 + 补齐公共牌，数我赢了几次。
    /// 打平时按人头平分（3 家平局算 1/3）。
    /// </summary>
    public static double Equity(HoldemGame g, int seat, int rollouts, Random rng)
    {
        var me = g.Players[seat];
        var known = new List<PokerCard>(me.Hole);
        known.AddRange(g.Board);

        var pool = PokerDeck.Build();
        pool.RemoveAll(c => known.Contains(c));

        var opponents = g.Players.Count(p => !p.Folded && p.Seat != seat);
        if (opponents == 0) return 1.0;

        var need = 5 - g.Board.Count;
        if (pool.Count < opponents * 2 + need) return 0.5;   // 理论上不会发生

        var board = new List<PokerCard>(7);
        var seven = new List<PokerCard>(7);
        double score = 0;

        for (var r = 0; r < rollouts; r++)
        {
            for (var i = pool.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            var idx = 0;
            board.Clear();
            board.AddRange(g.Board);
            for (var i = 0; i < need; i++) board.Add(pool[idx++]);

            seven.Clear();
            seven.AddRange(me.Hole);
            seven.AddRange(board);
            var myVal = HandEval.Best(seven, out _);

            var beaten = false;
            var ties = 0;
            for (var o = 0; o < opponents; o++)
            {
                seven.Clear();
                seven.Add(pool[idx++]);
                seven.Add(pool[idx++]);
                seven.AddRange(board);
                var cmp = myVal.CompareTo(HandEval.Best(seven, out _));
                if (cmp < 0) { beaten = true; break; }
                if (cmp == 0) ties++;
            }

            if (beaten) continue;
            score += ties == 0 ? 1.0 : 1.0 / (ties + 1);
        }

        return score / rollouts;
    }
}
