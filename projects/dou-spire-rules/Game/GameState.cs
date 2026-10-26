namespace DouSpireRules;

/// <summary>
/// 一条出牌流水：谁、出了什么。空列表 = 要不起（pass）。
/// 顺序 = 实际出牌顺序。AI（DouZero 编码）和"每家出过什么"的展示都吃它。
/// </summary>
public record struct PlayRecord(int Seat, IReadOnlyList<Card> Cards);

/// <summary>一个座位（一局里就是一个玩家）。</summary>
public sealed class PlayerState
{
    public required string Name { get; init; }
    public required int Seat { get; init; }
    public List<Card> Hand { get; set; } = new();
    public bool IsLandlord { get; set; }

    /// <summary>
    /// 队伍号（0/1）。三人：地主 0、农民 1；四人 2v2：对家同队（0+2 一队、1+3 一队）。
    /// </summary>
    public int Team { get; set; }

    /// <summary>是不是我这边的（一队的）。用来判断"队友"。</summary>
    public bool SameSideAs(PlayerState other) => Team == other.Team;
}

/// <summary>
/// 一局斗地主的状态机。只管规则，不管画面。
///
/// 流程就照 rlcard 的 <c>round.py</c>：
///   洗牌 → 发 17/17/17 + 3 张底牌 → 定地主（地主拿走底牌变 20 张）
///   → 地主先领出 → 轮流"出牌/要不起" → 谁手牌空了谁赢
///
/// "两家都要不起 → 我自由出" 这条靠 <see cref="TrickHolder"/> 实现：
/// 转回来时把 <see cref="LastPlay"/> 清空，<see cref="MustLead"/> 就变 true。
/// </summary>
public sealed class GameState
{
    public ComboJudge Judge { get; }
    public List<PlayerState> Players { get; }
    public List<Card> Bottom { get; }

    /// <summary>轮到谁（座位号）。</summary>
    public int Current { get; private set; }

    /// <summary>场上当前最大的一手。null = 现在该领出，随便出。</summary>
    public Combo? LastPlay { get; private set; }

    /// <summary>场上当前最大的那手具体是哪几张牌。</summary>
    public List<Card>? LastCards { get; private set; }

    /// <summary>这手牌是谁打的（-1 = 无）。两家要不起、轮到他自己时，他就重新获得自由出牌权。</summary>
    public int TrickHolder { get; private set; } = -1;

    /// <summary>赢家座位（null = 还没结束）。</summary>
    public int? Winner { get; private set; }

    /// <summary>已进行的回合数（用来兜底防死循环）。</summary>
    public int TurnNo { get; private set; }

    public List<string> Log { get; } = new();

    /// <summary>完整出牌流水（含要不起）。DouZero 的 z 编码（最近 15 手）和"谁出过什么"都从这推。</summary>
    public List<PlayRecord> History { get; } = new();

    /// <summary>2v2：开局还要扣牌的座位（后手队，每人 1 张）。逐家 <see cref="TryDiscard"/> 扣完才开打。</summary>
    public List<int> PendingDiscards { get; } = new();

    /// <summary>2v2：已经扣掉的牌（后手队开局各扣 1 张，摊开给大家看）。</summary>
    public List<Card> DiscardPile { get; } = new();

    /// <summary>赢家的队伍（Winner 非空才有意义）。2v2 一人走完，全队赢。</summary>
    public int? WinningTeam => Winner is null ? null : Players[Winner.Value].Team;

    /// <summary>现在该领出（可以随便出）？</summary>
    public bool MustLead => LastPlay is null;

    public PlayerState Me => Players[Current];

    /// <summary>输家（除赢家以外的所有人）—— 对应"我一走完，另外两家直接判负"。</summary>
    public IEnumerable<int> Losers =>
        Winner is null ? Enumerable.Empty<int>() : Enumerable.Range(0, Players.Count).Where(i => i != Winner);

    private GameState(ComboJudge judge, List<PlayerState> players, List<Card> bottom)
    {
        Judge = judge;
        Players = players;
        Bottom = bottom;
    }

    /// <summary>
    /// 开一局。
    /// <paramref name="landlordSeat"/> 传 -1 = 按手牌评分自动定地主（仅 3 人）。
    /// <paramref name="playerCount"/> 传 0 = 按 deckCopies 推导（1 副 → 3 人，2 副 → 4 人）。
    /// 4 人 = **欢乐斗地主 2v2 组队口径**：两副牌去 3/4/5 共 84 张，每人 20 张，
    /// 没有地主（剩 4 张作暗底牌），对家组队（0+2、1+3），
    /// <paramref name="firstSeat"/> 所在队先出（-1 = 随机），另一队开局各扣 1 张
    /// （<see cref="PendingDiscards"/>，须逐家 <see cref="TryDiscard"/> 后才能开打）。
    /// </summary>
    public static GameState NewGame(int seed, IReadOnlyList<string>? names = null, int landlordSeat = -1,
        int deckCopies = 1, int playerCount = 0, int firstSeat = -1)
    {
        if (playerCount <= 0) playerCount = deckCopies == 1 ? 3 : 4;
        var teamMode = playerCount == 4;

        if (teamMode)
        {
            // 2v2：84 张 = 4×20 + 4 暗底
            deckCopies = 2;
            var deck0 = Deck.Build(2, strip345: true);
            if (deck0.Count != 84) throw new InvalidOperationException($"2v2 牌堆应为 84 张，实际 {deck0.Count}");
            deckCopies = 2;
        }

        // 发牌必须整除：手牌 × 人数 + 底牌 = 全部牌。不整除就说明这组参数是错的，直接炸出来。
        var total = teamMode ? 84 : 54 * deckCopies;
        var bottomSize = teamMode ? 4 : (deckCopies == 1 ? 3 : 8);
        if ((total - bottomSize) % playerCount != 0)
            throw new ArgumentException(
                $"发不均：{deckCopies} 副牌 {playerCount} 人，({total}−{bottomSize}) 除不尽 {playerCount}");
        var handSize = (total - bottomSize) / playerCount;

        var rng = new Random(seed);
        var deck = teamMode ? Deck.Build(2, strip345: true) : Deck.Build(deckCopies);
        for (var i = deck.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        string[] defaultNames = playerCount == 4
            ? new[] { "我", "电脑A", "电脑B", "电脑C" }
            : new[] { "我", "电脑A", "电脑B" };
        var namesN = names is { Count: var n } && n == playerCount ? names : defaultNames;
        var players = new List<PlayerState>();
        for (var s = 0; s < playerCount; s++)
        {
            players.Add(new PlayerState
            {
                Name = namesN[s],
                Seat = s,
                Hand = deck.Skip(s * handSize).Take(handSize).OrderBy(c => c.Rank).ToList(),
            });
        }
        var bottom = deck.Skip(playerCount * handSize).Take(bottomSize).OrderBy(c => c.Rank).ToList();

        var judge = teamMode ? new ComboJudge(2, rocketJokers: 3) : new ComboJudge(deckCopies);
        var g = new GameState(judge, players, bottom);

        // 队伍：4 人对家组队（0+2 一队、1+3 一队）；3 人地主队 0、农民队 1（定完地主再标）
        if (teamMode)
            for (var s = 0; s < 4; s++) players[s].Team = s % 2;
        else
            for (var s = 0; s < playerCount; s++) players[s].Team = 1;

        if (teamMode)
        {
            // 2v2：没有地主。先出的人所在队先手；另一队每人开局扣 1 张。
            var seat = firstSeat;
            if (seat < 0) seat = rng.Next(4);
            players[seat].IsLandlord = false;
            g.Current = seat;
            for (var s = 0; s < 4; s++)
                if (players[s].Team != players[seat].Team)
                    g.PendingDiscards.Add(s);
            g.Log.Add($"{players[seat].Name} 先出（他这队先手），另一队各扣一张底牌");
        }
        else
        {
            // 定地主（3 人老口径）
            var seat = landlordSeat;
            if (seat < 0)
            {
                var best = -1;
                var bestScore = int.MinValue;
                for (var s = 0; s < playerCount; s++)
                {
                    var sc = LandlordScore(players[s].Hand);
                    if (sc > bestScore) { bestScore = sc; best = s; }
                }
                seat = best;
            }
            players[seat].IsLandlord = true;
            players[seat].Team = 0;
            players[seat].Hand = players[seat].Hand.Concat(bottom).OrderBy(c => c.Rank).ToList();
            g.Current = seat;
            g.Log.Add($"{players[seat].Name} 当地主（{LandlordScore(players[seat].Hand)} 分手牌），拿走底牌 " +
                      string.Join(" ", bottom.Select(CardName)));
        }
        return g;
    }

    /// <summary>
    /// **从 <paramref name="seat"/> 的视角把这一局重排一遍** —— 让他变成 0 号位（本地视角）。
    ///
    /// 为什么需要它：多人同桌时，每家屏幕上"我"都必须坐在最下面（0 号位），
    /// 而 <see cref="NewGame"/> 是**按座位号切牌**的（<c>deck.Skip(s * handSize)</c>），
    /// 所以不能靠"把名字数组转一圈"来换视角 —— 那样同一个座位在不同机器上会拿到不同的牌。
    /// 正确做法是：**全局同一副牌 → 各自旋转**。
    ///
    /// 旋转是**纯置换**：牌、地主、轮到谁、场上那一手、谁拿了这一手，全部跟着搬。
    /// 因为斗地主的回合是 <c>(Current + 1) % 3</c> 的三循环，旋转之后循环还是同一个循环，
    /// 所以规则一个字都不用改 —— 每家拿到的"谁该出牌""谁压得住"完全一致。
    ///
    /// <paramref name="seat"/> 传 0 时直接返回自己（不复制）。
    /// </summary>
    public GameState ViewFrom(int seat)
    {
        var n = Players.Count;
        if (n == 0) return this;
        seat = ((seat % n) + n) % n;
        if (seat == 0) return this;

        // 本地第 i 席 ← 全局第 (seat + i) % n 席
        var players = new List<PlayerState>(n);
        for (var i = 0; i < n; i++)
        {
            var p = Players[(seat + i) % n];
            players.Add(new PlayerState
            {
                Name = p.Name,
                Seat = i,
                Hand = new List<Card>(p.Hand),
                IsLandlord = p.IsLandlord,
                Team = p.Team,
            });
        }

        var g = new GameState(Judge, players, Bottom);
        g.Current = ((Current - seat) % n + n) % n;
        g.TrickHolder = TrickHolder < 0 ? -1 : ((TrickHolder - seat) % n + n) % n;
        g.Winner = Winner is null ? null : ((Winner.Value - seat) % n + n) % n;
        g.LastPlay = LastPlay;
        g.LastCards = LastCards;
        g.TurnNo = TurnNo;
        foreach (var line in Log) g.Log.Add(line);
        foreach (var r in History)
            g.History.Add(new PlayRecord(((r.Seat - seat) % n + n) % n, r.Cards.ToList()));
        foreach (var s in PendingDiscards)
            g.PendingDiscards.Add(((s - seat) % n + n) % n);
        foreach (var c in DiscardPile) g.DiscardPile.Add(c);
        return g;
    }

    /// <summary>rlcard 同款的粗略手牌评分：炸弹 +6、王炸 +8、A/2/王各加分。用来定地主。</summary>
    public static int LandlordScore(IReadOnlyList<Card> hand)
    {
        var counts = new Dictionary<int, int>();
        foreach (var c in hand) counts[c.Rank] = counts.TryGetValue(c.Rank, out var v) ? v + 1 : 1;

        var score = 0;
        var hasRocket = counts.ContainsKey(Ranks.SmallJoker) && counts.ContainsKey(Ranks.BigJoker);
        if (hasRocket) score += 8;

        foreach (var (rank, cnt) in counts)
        {
            if (hasRocket && rank is Ranks.SmallJoker or Ranks.BigJoker) continue;
            if (cnt >= 4) { score += 6; continue; }
            score += rank switch
            {
                Ranks.Ace => 1,
                Ranks.Two => 2,
                Ranks.SmallJoker => 3,
                Ranks.BigJoker => 4,
                _ => 0,
            };
        }
        return score;
    }

    /// <summary>2v2：后手队开局扣一张（摊开）。扣完 <see cref="PendingDiscards"/> 清空才能出牌。</summary>
    public bool TryDiscard(int seat, Card card, out string error)
    {
        error = "";
        if (Winner is not null) { error = "牌局已经结束了"; return false; }
        if (!PendingDiscards.Contains(seat)) { error = "这个座位不用扣牌"; return false; }
        var hand = Players[seat].Hand;
        if (!hand.Remove(card)) { error = $"手里没有 {CardName(card)}"; return false; }
        DiscardPile.Add(card);
        PendingDiscards.Remove(seat);
        Log.Add($"{Players[seat].Name} 扣一张 {CardName(card)}");
        return true;
    }

    /// <summary>出牌。失败时 <paramref name="error"/> 说明原因。</summary>
    public bool TryPlay(IEnumerable<Card> cards, out string error)
    {
        error = "";
        if (Winner is not null) { error = "牌局已经结束了"; return false; }
        if (PendingDiscards.Count > 0) { error = "还有玩家没扣完底牌"; return false; }

        var list = cards.ToList();
        if (list.Count == 0) { error = "没选牌"; return false; }

        var parsed = Judge.TryParse(list);
        if (parsed is null) { error = $"这手牌不成牌型（{string.Join(" ", list.Select(CardName))}）"; return false; }
        if (!ComboJudge.Beats(parsed, LastPlay)) { error = $"压不住场上的 {Describe(LastPlay!)}"; return false; }

        var left = new List<Card>(Me.Hand);
        foreach (var c in list)
            if (!left.Remove(c)) { error = $"手里没有 {CardName(c)}"; return false; }

        Me.Hand = left;
        LastPlay = parsed;
        LastCards = list;
        TrickHolder = Current;
        Log.Add($"{Me.Name} 出 {Describe(parsed)}");
        History.Add(new PlayRecord(Current, list.ToList()));
        TurnNo++;

        if (left.Count == 0)
        {
            Winner = Current;
            Log.Add(Players.Count == 4
                ? $"{Me.Name} 牌走完了 —— {Me.Name} 这队（{Me.Name}、{Players[(Current + 2) % 4].Name}）赢"
                : $"{Me.Name} 牌走完了 —— 这一局 {Me.Name} 赢");
            return true;
        }
        Advance();
        return true;
    }

    /// <summary>要不起。</summary>
    public bool TryPass(out string error)
    {
        error = "";
        if (Winner is not null) { error = "牌局已经结束了"; return false; }
        if (PendingDiscards.Count > 0) { error = "还有玩家没扣完底牌"; return false; }
        if (LastPlay is null) { error = "轮到你领出，不能要不起"; return false; }

        Log.Add($"{Me.Name} 要不起");
        History.Add(new PlayRecord(Current, new List<Card>()));
        TurnNo++;
        Advance();
        return true;
    }

    private void Advance()
    {
        Current = (Current + 1) % Players.Count;
        if (Current == TrickHolder)
        {
            // 转了一圈回到出牌的那位 → 新的一轮，他自由出
            LastPlay = null;
            LastCards = null;
            TrickHolder = -1;
        }
    }

    // —— 显示用的小工具 ——

    public static string CardName(Card c) => Ranks.Name(c.Rank);

    public static string Describe(Combo c) =>
        $"{c.TypeName}（{string.Join(" ", c.Cards.OrderBy(x => x.Rank).Select(CardName))}）";

    /// <summary>一局的结算陈述 —— 直接对应"出完牌就把另外两家判负"。</summary>
    public string ResultText()
    {
        if (Winner is null) return "还没结束";
        var w = Players[Winner.Value];
        var losers = Losers.Select(i => Players[i].Name).ToList();
        return Players.Count == 4
            ? $"{w.Name} 这队赢（队友 {Players[(Winner.Value + 2) % 4].Name}）；{string.Join("、", losers)} 判负。"
            : $"{w.Name}（{(w.IsLandlord ? "地主" : "农民")}）赢了；{string.Join("、", losers)} 判负。";
    }
}
