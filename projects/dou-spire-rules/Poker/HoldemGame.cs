namespace DouSpireRules;

/// <summary>打到哪条街了。</summary>
public enum Street
{
    /// <summary>翻牌前：每人 2 张底牌，还没公共牌。</summary>
    Preflop = 0,

    /// <summary>翻牌圈：3 张公共牌。</summary>
    Flop = 1,

    /// <summary>转牌圈：4 张公共牌。</summary>
    Turn = 2,

    /// <summary>河牌圈：5 张公共牌。</summary>
    River = 3,

    /// <summary>摊牌中。</summary>
    Showdown = 4,

    /// <summary>这手结束了（弃牌赢 or 摊牌完）。</summary>
    Done = 5,
}

/// <summary>玩家能做的动作。</summary>
public enum PokerAction
{
    /// <summary>弃牌：不玩了，已下的钱不退。</summary>
    Fold = 0,

    /// <summary>过牌：没人下注时，把决定权交给下家。</summary>
    Check = 1,

    /// <summary>跟注：补到和当前注一样多。</summary>
    Call = 2,

    /// <summary>下注：本轮还没人下注时，主动掏钱。</summary>
    Bet = 3,

    /// <summary>加注：在别人的注上加更多。</summary>
    Raise = 4,

    /// <summary>全下：把身上的筹码全推出去。</summary>
    AllIn = 5,
}

/// <summary>桌上一席。</summary>
public sealed class HoldemPlayer
{
    public required string Name { get; init; }
    public required int Seat { get; init; }

    /// <summary>开局的筹码（用来算"这手赢了多少/输了多少"）。</summary>
    public int StartStack { get; init; }

    /// <summary>面前还剩多少筹码。</summary>
    public int Stack { get; set; }

    /// <summary>两张底牌（**只有本人能看**）。</summary>
    public PokerCard[] Hole { get; set; } = new PokerCard[2];

    /// <summary>**本轮**已经投进多少（每换一条街清零）。</summary>
    public int CommittedStreet { get; set; }

    /// <summary>**整手牌**已经投进多少（不清零，用来分锅）。</summary>
    public int CommittedTotal { get; set; }

    /// <summary>弃牌了没。</summary>
    public bool Folded { get; set; }

    /// <summary>全下了没（全下的人不能再行动，但还在牌局里）。</summary>
    public bool AllIn { get; set; }

    /// <summary>**本轮**轮到他时表态过没有。有人加注会把其他人这个标记清掉（要重新表态）。</summary>
    public bool ActedThisStreet { get; set; }

    /// <summary>还在牌局里（没弃牌）。</summary>
    public bool InHand => !Folded;

    /// <summary>这手牌净赢多少（正 = 赢，负 = 输）。终局后才有意义。</summary>
    public int Delta => Stack - StartStack;
}

/// <summary>
/// 一手德州扑克的状态机。**只管规则，不管画面**。
///
/// 流程（照标准德州）：
/// <code>
///   定庄家 → 下盲注 → 每人 2 张底牌
///     → 翻牌前下注（从大盲下家开始）
///     → 翻 3 张公共牌 → 翻牌圈下注（从庄家下家开始）
///     → 翻第 4 张    → 转牌圈下注（同上）
///     → 翻第 5 张    → 河牌圈下注（同上）
///     → 摊牌：每人 7 张挑最好 5 张，最大者赢锅
/// </code>
///
/// <para><b>★ 钱必须平</b>：任何时刻 <c>所有人 Stack 之和 + 所有人已投注之和 = 开局总筹码</c>。
/// 这手打完还要再验一次 —— 这是整个内核最容易出 bug 的地方（边锅、平分、零头）。</para>
/// </summary>
public sealed class HoldemGame
{
    public List<HoldemPlayer> Players { get; }
    public List<PokerCard> Board { get; } = new();

    public Street Street { get; private set; } = Street.Preflop;

    /// <summary>庄家位（按钮位）—— 每手顺时针轮换。</summary>
    public int Button { get; }

    public int SmallBlind { get; }
    public int BigBlind { get; }

    /// <summary>轮到谁行动。</summary>
    public int CurrentSeat { get; private set; }

    /// <summary>本轮当前的注额（要跟就得补到这个数）。</summary>
    public int CurrentBet { get; private set; }

    /// <summary>加注**至少要加到**多少（本轮总额）。</summary>
    public int MinRaiseTo { get; private set; }

    /// <summary>最后一个下注/加注的人（显示用，-1 = 还没人）。</summary>
    public int LastAggressor { get; private set; } = -1;

    /// <summary>底池 = 所有人已投注之和。</summary>
    public int Pot => Players.Sum(p => p.CommittedTotal);

    /// <summary>这手结束了没。</summary>
    public bool HandOver { get; private set; }

    /// <summary>赢家座位（可能多个 —— 平分锅）。</summary>
    public List<int> Winners { get; } = new();

    /// <summary>每席的结算（赢多少/输多少）—— 终局后才有意义。</summary>
    public int[] ResultDelta => Players.Select(p => p.Delta).ToArray();

    public List<string> Log { get; } = new();

    /// <summary>这一手是谁赢的（文字版，直接可以显示）。</summary>
    public string ResultText { get; private set; } = "";

    /// <summary>赢家是怎么赢的（弃牌赢 / 摊牌赢 + 牌型）。</summary>
    public string WinKind { get; private set; } = "";

    /// <summary>这手最终底池多大（结算后仍有值，给 UI 显示用）。</summary>
    public int FinalPot { get; private set; }

    /// <summary>这手最终的分锅结构（主锅 + 边锅）—— 结算后仍有值。</summary>
    public List<(int Amount, List<int> Eligible)> FinalPots { get; private set; } = new();

    private readonly List<PokerCard> _deck;
    private int _deckIndex;
    private readonly int _totalChips;

    private HoldemGame(List<HoldemPlayer> players, int button, int sb, int bb, List<PokerCard> deck, int totalChips)
    {
        Players = players;
        Button = button;
        SmallBlind = sb;
        BigBlind = bb;
        _deck = deck;
        _totalChips = totalChips;
    }

    public HoldemPlayer Me => Players[CurrentSeat];

    /// <summary>还没弃牌的人。</summary>
    public IEnumerable<HoldemPlayer> Alive => Players.Where(p => !p.Folded);

    // ══════════════════════════ 开局 ══════════════════════════

    /// <summary>
    /// 开一手牌。
    /// <paramref name="names"/> 的个数 = 人数（支持 2~6 人；我们实际用 3 或 4）。
    /// <paramref name="button"/> 传 -1 = 按种子随机定庄。
    /// <paramref name="stacks"/> 传了就**每人筹码可以不一样**（篝火模式下每人金币不同，边锅就靠这个测）。
    /// </summary>
    public static HoldemGame NewGame(int seed, IReadOnlyList<string>? names = null,
        int startStack = 1000, int smallBlind = 50, int bigBlind = 100, int button = -1,
        IReadOnlyList<int>? stacks = null)
    {
        var n = names?.Count ?? 3;
        if (n is < 2 or > 6) throw new ArgumentException($"德州支持 2~6 人，给了 {n} 人");
        if (smallBlind <= 0 || bigBlind <= smallBlind) throw new ArgumentException("盲注不合法");
        if (startStack <= 0) throw new ArgumentException("开局筹码必须大于 0");

        var defaultNames = n switch
        {
            2 => new[] { "我", "电脑A" },
            3 => new[] { "我", "电脑A", "电脑B" },
            4 => new[] { "我", "电脑A", "电脑B", "电脑C" },
            5 => new[] { "我", "电脑A", "电脑B", "电脑C", "电脑D" },
            _ => new[] { "我", "电脑A", "电脑B", "电脑C", "电脑D", "电脑E" },
        };
        var ns = names is { Count: > 0 } && names.Count == n ? names : defaultNames;

        var rng = new Random(seed);
        var deck = PokerDeck.Build();
        for (var i = deck.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        if (button < 0) button = rng.Next(n);
        button = ((button % n) + n) % n;

        var players = new List<HoldemPlayer>(n);
        for (var s = 0; s < n; s++)
        {
            var st = stacks is { Count: > 0 } && stacks.Count == n ? stacks[s] : startStack;
            if (st <= 0) throw new ArgumentException($"第 {s} 席的筹码必须大于 0（给了 {st}）");
            players.Add(new HoldemPlayer { Name = ns[s], Seat = s, StartStack = st, Stack = st });
        }

        return Begin(players, button, smallBlind, bigBlind, deck);
    }

    /// <summary>
    /// ★ **自检专用**：用一副**指定的牌**开局（不洗牌）。
    /// 牌序：前 2×人数张依次发底牌（从庄家下家开始，一人一张转两圈），
    /// 之后是公共牌。有了它才能写出"确定会平分锅 / 确定出边锅"这种测试。
    /// </summary>
    public static HoldemGame NewGameWithDeck(IReadOnlyList<string> names, IReadOnlyList<int> stacks,
        int smallBlind, int bigBlind, int button, IReadOnlyList<PokerCard> deck)
    {
        var n = names.Count;
        if (n is < 2 or > 6) throw new ArgumentException($"德州支持 2~6 人，给了 {n} 人");
        if (stacks.Count != n) throw new ArgumentException("筹码个数要和人数一致");
        button = ((button % n) + n) % n;

        var players = new List<HoldemPlayer>(n);
        for (var s = 0; s < n; s++)
        {
            if (stacks[s] <= 0) throw new ArgumentException($"第 {s} 席的筹码必须大于 0");
            players.Add(new HoldemPlayer { Name = names[s], Seat = s, StartStack = stacks[s], Stack = stacks[s] });
        }

        return Begin(players, button, smallBlind, bigBlind, deck.ToList());
    }

    /// <summary>发底牌 + 下盲注 + 定先手 —— 两条开局路径共用，免得逻辑分叉。</summary>
    private static HoldemGame Begin(List<HoldemPlayer> players, int button, int sb, int bb, List<PokerCard> deck)
    {
        var n = players.Count;
        var g = new HoldemGame(players, button, sb, bb, deck, players.Sum(p => p.StartStack));

        // —— 发底牌：从庄家下家开始，一人一张转两圈（和真发牌一样）——
        for (var round = 0; round < 2; round++)
            for (var i = 0; i < n; i++)
            {
                var seat = (button + 1 + i) % n;
                players[seat].Hole[round] = g.Draw();
            }

        // —— 下盲注 ——
        // 2 人单挑是特例：庄家 = 小盲，另一个 = 大盲。
        var sbSeat = n == 2 ? button : (button + 1) % n;
        var bbSeat = n == 2 ? (button + 1) % n : (button + 2) % n;
        g.Commit(players[sbSeat], sb);
        g.Commit(players[bbSeat], bb);
        g.Log.Add($"{players[sbSeat].Name} 下小盲 {sb}，{players[bbSeat].Name} 下大盲 {bb}");

        g.CurrentBet = bb;
        g.MinRaiseTo = bb * 2;
        g.LastAggressor = bbSeat;

        // 翻牌前从**大盲的下家**（枪口位 UTG）开始
        g.CurrentSeat = g.NextSeatWhoCanAct(bbSeat);
        g.Log.Add($"—— 翻牌前 —— 轮到 {players[g.CurrentSeat].Name}（大盲下家）");
        return g;
    }

    private PokerCard Draw() => _deck[_deckIndex++];

    // ══════════════════════════ 合法动作 ══════════════════════════

    /// <summary>当前这一席现在能做哪些动作。</summary>
    public List<PokerAction> LegalActions()
    {
        var res = new List<PokerAction>();
        if (HandOver) return res;
        var me = Players[CurrentSeat];
        if (me.Folded || me.AllIn) return res;

        var toCall = CurrentBet - me.CommittedStreet;

        res.Add(PokerAction.Fold);
        if (toCall <= 0) res.Add(PokerAction.Check);
        else res.Add(PokerAction.Call);

        // 能下注/加注吗？—— 筹码得比"当前注"多才谈得上
        // ★ 区分"下注"还是"加注"看的是**本轮有没有人下过注**（CurrentBet），
        //   不是"我要不要补钱"：大盲已经投了 100，他不用补钱（toCall=0），
        //   但他那 100 就是本轮的注 —— 所以他再往上加是"加注"，不是"下注"。
        var maxTo = me.CommittedStreet + me.Stack;
        if (maxTo > CurrentBet && me.Stack > 0)
            res.Add(CurrentBet > 0 ? PokerAction.Raise : PokerAction.Bet);

        if (me.Stack > 0) res.Add(PokerAction.AllIn);
        return res;
    }

    /// <summary>要跟注得掏多少（不够就全下）。</summary>
    public int CallCost(int seat) => Math.Min(Math.Max(0, CurrentBet - Players[seat].CommittedStreet), Players[seat].Stack);

    /// <summary>这一席加注最多能加到多少（= 本轮已投 + 面前筹码）。</summary>
    public int MaxRaiseTo(int seat) => Players[seat].CommittedStreet + Players[seat].Stack;

    /// <summary>这一席加注至少要加到多少（筹码不够最小加注时，退化成"只能全下"）。</summary>
    public int MinRaiseToFor(int seat) => Math.Min(MinRaiseTo, MaxRaiseTo(seat));

    /// <summary>下注/加注的常用尺度：底池的某个比例（UI 上那排"½池 / ¾池 / 满池"就用它）。</summary>
    public int PotSizedRaiseTo(int seat, double fraction)
    {
        var me = Players[seat];
        var toCall = Math.Max(0, CurrentBet - me.CommittedStreet);
        var potAfterCall = Pot + toCall;
        var raw = me.CommittedStreet + toCall + (int)Math.Round(potAfterCall * fraction);
        return Math.Clamp(raw, MinRaiseToFor(seat), MaxRaiseTo(seat));
    }

    // ══════════════════════════ 动作 ══════════════════════════

    /// <summary>
    /// 走一步。
    /// <paramref name="amount"/> 只在 <see cref="PokerAction.Bet"/> / <see cref="PokerAction.Raise"/> 时有用，
    /// 含义是"**加注到**多少"（本轮累计投注额），不是"加多少"。
    /// </summary>
    public bool Apply(PokerAction action, int amount, out string error)
    {
        error = "";
        if (HandOver) { error = "这手已经结束了"; return false; }

        var me = Players[CurrentSeat];
        if (me.Folded || me.AllIn) { error = $"{me.Name} 现在不能行动"; return false; }

        var toCall = CurrentBet - me.CommittedStreet;

        switch (action)
        {
            case PokerAction.Fold:
                me.Folded = true;
                me.ActedThisStreet = true;
                Log.Add($"{me.Name} 弃牌");
                break;

            case PokerAction.Check:
                if (toCall > 0) { error = $"有人下注到 {CurrentBet}，不能过牌（只能跟注/加注/弃牌）"; return false; }
                me.ActedThisStreet = true;
                Log.Add($"{me.Name} 过牌");
                break;

            case PokerAction.Call:
                if (toCall <= 0) { error = "没人下注，不用跟注（可以过牌）"; return false; }
                {
                    var pay = Math.Min(toCall, me.Stack);
                    Commit(me, pay);
                    me.ActedThisStreet = true;
                    Log.Add(me.AllIn ? $"{me.Name} 全下 {pay}（跟注）" : $"{me.Name} 跟注 {pay}");
                }
                break;

            case PokerAction.Bet:
            case PokerAction.Raise:
                var hadBet = CurrentBet > 0;
                if (hadBet && action == PokerAction.Bet)
                { error = $"已经有人下注到 {CurrentBet} 了，只能「加注」（不能「下注」）"; return false; }
                if (!hadBet && action == PokerAction.Raise)
                { error = "本轮还没人下注，只能「下注」（不能「加注」）"; return false; }
                {
                    var maxTo = MaxRaiseTo(me.Seat);
                    var minTo = MinRaiseToFor(me.Seat);
                    if (amount < minTo && amount < maxTo)
                    { error = $"至少要加到 {minTo}（你现在能加到 {maxTo}）"; return false; }
                    if (amount > maxTo) { error = $"最多只能加到 {maxTo}"; return false; }
                    if (amount <= CurrentBet && amount < maxTo)
                    { error = $"要加就得加到 {CurrentBet} 以上"; return false; }

                    var before = CurrentBet;
                    Commit(me, amount - me.CommittedStreet);
                    var raised = me.CommittedStreet - before;

                    if (me.CommittedStreet > CurrentBet)
                    {
                        CurrentBet = me.CommittedStreet;
                        MinRaiseTo = CurrentBet + Math.Max(raised, BigBlind);
                        LastAggressor = me.Seat;
                        // 别人要重新表态
                        foreach (var p in Players)
                            if (p.Seat != me.Seat && !p.Folded && !p.AllIn) p.ActedThisStreet = false;
                    }
                    me.ActedThisStreet = true;
                    Log.Add(me.AllIn
                        ? $"{me.Name} 全下 {me.CommittedStreet}（{(hadBet ? "加注" : "下注")}）"
                        : hadBet
                            ? $"{me.Name} 加注到 {me.CommittedStreet}"
                            : $"{me.Name} 下注 {me.CommittedStreet}");
                }
                break;

            case PokerAction.AllIn:
                if (me.Stack <= 0) { error = "没筹码了"; return false; }
                {
                    var before = CurrentBet;
                    Commit(me, me.Stack);
                    if (me.CommittedStreet > before)
                    {
                        var raised = me.CommittedStreet - before;
                        CurrentBet = me.CommittedStreet;
                        MinRaiseTo = CurrentBet + Math.Max(raised, BigBlind);
                        LastAggressor = me.Seat;
                        foreach (var p in Players)
                            if (p.Seat != me.Seat && !p.Folded && !p.AllIn) p.ActedThisStreet = false;
                    }
                    me.ActedThisStreet = true;
                    Log.Add($"{me.Name} 全下 {me.CommittedStreet}");
                }
                break;

            default:
                error = "看不懂的动作";
                return false;
        }

        AfterAction();
        return true;
    }

    private void Commit(HoldemPlayer p, int amount)
    {
        amount = Math.Min(Math.Max(amount, 0), p.Stack);
        p.Stack -= amount;
        p.CommittedStreet += amount;
        p.CommittedTotal += amount;
        if (p.Stack == 0) p.AllIn = true;
    }

    private void AfterAction()
    {
        // 只剩一个人没弃牌 → 直接赢锅（不用摊牌）
        if (Players.Count(p => !p.Folded) == 1) { WinByFold(); return; }

        if (RoundComplete()) { AdvanceStreet(); return; }
        CurrentSeat = NextSeatWhoCanAct(CurrentSeat);
    }

    /// <summary>本轮下注结束了吗 —— 所有"还能行动"的人都表过态且跟平了。</summary>
    private bool RoundComplete()
    {
        var active = Players.Where(p => !p.Folded && !p.AllIn).ToList();
        if (active.Count == 0) return true;
        return active.All(p => p.ActedThisStreet && p.CommittedStreet == CurrentBet);
    }

    /// <summary>从 <paramref name="from"/> 往后找下一个"还能行动"的人。</summary>
    private int NextSeatWhoCanAct(int from)
    {
        var n = Players.Count;
        for (var i = 1; i <= n; i++)
        {
            var s = ((from + i) % n + n) % n;
            if (!Players[s].Folded && !Players[s].AllIn) return s;
        }
        return from;   // 没人能动（理论上到不了这里）
    }

    private int CanActCount() => Players.Count(p => !p.Folded && !p.AllIn);

    // ══════════════════════════ 推进 ══════════════════════════

    private void AdvanceStreet()
    {
        if (Players.Count(p => !p.Folded) == 1) { WinByFold(); return; }

        while (true)
        {
            Street = Street switch
            {
                Street.Preflop => Street.Flop,
                Street.Flop => Street.Turn,
                Street.Turn => Street.River,
                _ => Street.Showdown,
            };

            if (Street == Street.Flop) DealBoard(3);
            else if (Street is Street.Turn or Street.River) DealBoard(1);

            if (Street == Street.Showdown) { DoShowdown(); return; }

            ResetBettingRound();

            // 只剩一个人还能行动（其他人都全下了）→ 不用再下注了，把牌发完直接摊牌
            if (CanActCount() >= 2)
            {
                CurrentSeat = NextSeatWhoCanAct(Button);
                Log.Add($"—— {StreetName(Street)} —— 轮到 {Players[CurrentSeat].Name}（庄家下家）");
                return;
            }
            Log.Add($"—— {StreetName(Street)} —— 都全下了，直接发牌");
        }
    }

    private void ResetBettingRound()
    {
        foreach (var p in Players)
        {
            p.CommittedStreet = 0;
            p.ActedThisStreet = false;
        }
        CurrentBet = 0;
        MinRaiseTo = BigBlind;
        LastAggressor = -1;
    }

    private void DealBoard(int count)
    {
        for (var i = 0; i < count; i++) Board.Add(Draw());
    }

    // ══════════════════════════ 结算 ══════════════════════════

    private void WinByFold()
    {
        HandOver = true;
        Street = Street.Done;
        var w = Players.First(p => !p.Folded);
        var deltas = new int[Players.Count];
        deltas[w.Seat] = Pot;
        Settle(deltas);
        Winners.Clear();
        Winners.Add(w.Seat);
        WinKind = "fold";
        ResultText = $"{w.Name} 把别人都吓跑了 —— 直接赢 {FinalPot}。";
        Log.Add(ResultText);
    }

    /// <summary>
    /// 结算：先把"分了几个锅"记下来（UI 要显示），再把钱分下去，
    /// 最后**把"已投注"清零** —— 钱已经进了各人的筹码堆，不再属于底池。
    /// 清零是 <see cref="MoneyBalanced"/> 能一直成立的关键。
    /// </summary>
    private void Settle(int[] deltas)
    {
        FinalPots = ComputePots();
        FinalPot = Pot;
        for (var s = 0; s < Players.Count; s++) Players[s].Stack += deltas[s];
        foreach (var p in Players) p.CommittedTotal = 0;
    }

    private void DoShowdown()
    {
        HandOver = true;
        Street = Street.Showdown;

        var best = new HandValue[Players.Count];
        for (var s = 0; s < Players.Count; s++)
        {
            if (Players[s].Folded) continue;
            var seven = new List<PokerCard>(7);
            seven.AddRange(Players[s].Hole);
            seven.AddRange(Board);
            best[s] = HandEval.Best(seven, out _);
        }

        // —— 分锅：按每个人投注额分层，做出主锅 + 边锅 ——
        var deltas = new int[Players.Count];
        var winners = new List<int>();

        foreach (var (amount, eligible) in ComputePots())
        {
            if (amount <= 0 || eligible.Count == 0) continue;

            // 这一锅里谁的牌最大
            var bestSeat = eligible[0];
            foreach (var s in eligible)
                if (best[s].CompareTo(best[bestSeat]) > 0) bestSeat = s;

            var potWinners = eligible.Where(s => best[s].CompareTo(best[bestSeat]) == 0).ToList();

            var share = amount / potWinners.Count;
            var remainder = amount - share * potWinners.Count;
            foreach (var s in potWinners) deltas[s] += share;

            // 零头给"庄家左手第一个赢家"（标准做法）
            if (remainder > 0)
            {
                var first = potWinners.OrderBy(s => (((s - Button - 1) % Players.Count) + Players.Count) % Players.Count).First();
                deltas[first] += remainder;
            }

            foreach (var s in potWinners)
                if (!winners.Contains(s)) winners.Add(s);
        }

        var potWon = Pot;
        Settle(deltas);

        Winners.Clear();
        Winners.AddRange(winners.OrderBy(x => x));
        WinKind = "showdown";

        var parts = Winners.Select(s =>
        {
            var seven = new List<PokerCard>(7);
            seven.AddRange(Players[s].Hole);
            seven.AddRange(Board);
            return $"{Players[s].Name}（{HandEval.Name(HandEval.Best(seven, out _))}）";
        });
        ResultText = "摊牌：" + string.Join("、", parts) + $" 赢 {potWon}。";
        Log.Add(ResultText);
    }

    /// <summary>
    /// 分锅结构：**主锅 + 边锅**。
    ///
    /// 有人全下的筹码比别人少时，多出来的那部分要单独成锅（边锅），
    /// 全下的人**只能赢他够得着的那一份** —— 这是德州最容易算错钱的地方。
    ///
    /// 返回每个锅的（金额，谁有资格分）。UI 上显示"主锅 300 / 边锅 400"也用它。
    /// </summary>
    public List<(int Amount, List<int> Eligible)> ComputePots()
    {
        var pots = new List<(int, List<int>)>();
        var levels = Players.Where(p => p.CommittedTotal > 0)
                            .Select(p => p.CommittedTotal).Distinct().OrderBy(x => x).ToList();
        var prev = 0;

        foreach (var lvl in levels)
        {
            var amount = 0;
            foreach (var p in Players)
                amount += Math.Min(p.CommittedTotal, lvl) - Math.Min(p.CommittedTotal, prev);

            var eligible = Players.Where(p => !p.Folded && p.CommittedTotal >= lvl).Select(p => p.Seat).ToList();
            prev = lvl;
            if (amount > 0) pots.Add((amount, eligible));
        }
        return pots;
    }

    // ══════════════════════════ 给 UI / 自检用的小工具 ══════════════════════════

    public static string StreetName(Street s) => s switch
    {
        Street.Preflop => "翻牌前",
        Street.Flop => "翻牌",
        Street.Turn => "转牌",
        Street.River => "河牌",
        Street.Showdown => "摊牌",
        _ => "结束",
    };

    public static string ActionName(PokerAction a) => a switch
    {
        PokerAction.Fold => "弃牌",
        PokerAction.Check => "过牌",
        PokerAction.Call => "跟注",
        PokerAction.Bet => "下注",
        PokerAction.Raise => "加注",
        PokerAction.AllIn => "全下",
        _ => "?",
    };

    /// <summary>某一席现在的牌力（**只有自己那席该调** —— 别拿这个去看别人的底牌）。</summary>
    public HandValue HandOf(int seat)
    {
        var seven = new List<PokerCard>(7);
        seven.AddRange(Players[seat].Hole);
        seven.AddRange(Board);
        return HandEval.Best(seven, out _);
    }

    /// <summary>★ 自检：钱守恒吗？（任何时刻都该成立）</summary>
    public bool MoneyBalanced()
    {
        var total = Players.Sum(p => p.Stack) + Players.Sum(p => p.CommittedTotal);
        return total == _totalChips;
    }

    /// <summary>★ 自检：这手结束后，所有人的输赢加起来必须是 0。</summary>
    public bool ZeroSum()
    {
        if (!HandOver) return true;
        return Players.Sum(p => p.Delta) == 0;
    }
}
