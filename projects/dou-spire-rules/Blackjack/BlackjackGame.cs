namespace DouSpireRules;

/// <summary>
/// 21 点（Blackjack）的规则内核 —— **纯逻辑，不碰 Godot、不碰 UI**。
///
/// 和斗地主（<see cref="GameState"/>）、德州（<see cref="HoldemGame"/>）并列，是 DouSpire 的第三种玩法。
///
/// ★ 设计前提（用户 2026-10-11 拍板）：
/// <list type="bullet">
///   <item>**庄家是电脑荷官** —— 玩家各自和庄家比牌，**互不干扰**（用户原话「我自己都找不到一起玩的人」，
///     所以单机是主场景；联机只是"大家坐一桌各打各的"，同步极简单）。</item>
///   <item>**四种动作**：要牌 / 停牌 / 加倍 / 分牌（**不做**保险和投降 —— 用户选了不要）。</item>
///   <item>赌注有两套：**金币** / **血量**（设置里切），本内核只管**数字**，不管单位。</item>
/// </list>
/// </summary>
public sealed class BlackjackGame
{
    /// <summary>21 点能做的动作。</summary>
    public enum BjMove
    {
        /// <summary>要牌（再来一张）。</summary>
        Hit,

        /// <summary>停牌（不再要了，交给下一位）。</summary>
        Stand,

        /// <summary>加倍：注码翻倍，**只再发一张**就强制停牌。</summary>
        Double,

        /// <summary>分牌：两张同点拆成两手，各自补一张、各自独立结算。</summary>
        Split,
    }

    /// <summary>一手的最终结局。</summary>
    public enum BjOutcome
    {
        /// <summary>黑杰克（起手两张凑 21），赔 3:2。</summary>
        Blackjack,

        /// <summary>赢了，赔 1:1。</summary>
        Win,

        /// <summary>平局，退注。</summary>
        Push,

        /// <summary>输了（含爆牌），赔掉注码。</summary>
        Lose,
    }

    /// <summary>一局的阶段。</summary>
    public enum BjPhase
    {
        /// <summary>下注中（还没发牌）。</summary>
        Betting,

        /// <summary>已经发完牌，玩家在动作。</summary>
        PlayerTurn,

        /// <summary>所有玩家都定牌了，庄家在补牌。</summary>
        DealerTurn,

        /// <summary>结算完毕。</summary>
        Settled,
    }

    // ────────────────────────────── 常量 ──────────────────────────────

    /// <summary>庄家**多少点必须停牌**。标准是 17（软 17 也停 = S17 规则，对玩家有利，也最好解释）。</summary>
    public const int DealerStandOn = 17;

    /// <summary>黑杰克的赔率：赢的时候拿回 1.5 倍注码（净赚 1.5 倍）。</summary>
    public const decimal BlackjackPayout = 1.5m;

    /// <summary>一局最多能分成几手（分牌次数 = 这个数 - 1）。赌场常见 4 手。</summary>
    public const int MaxHandsPerSeat = 4;

    /// <summary>牌靴用几副牌。6 副是赌场标准；太多副会稀释算牌，太少副要一直洗。</summary>
    public const int DeckCount = 6;

    /// <summary>牌靴剩不到这个比例就重洗（切牌位置）。</summary>
    public const float ReshuffleAt = 0.25f;

    // ────────────────────────────── 一手牌 ──────────────────────────────

    /// <summary>
    /// **一手牌**。一个座位可能同时有好几手（分牌拆出来的），每手**独立结算**。
    /// </summary>
    public sealed class BjHand
    {
        /// <summary>这手的牌。</summary>
        public List<PokerCard> Cards { get; } = new();

        /// <summary>这手押了多少。</summary>
        public int Bet { get; set; }

        /// <summary>是不是加倍过的（加倍后只能再发一张）。</summary>
        public bool Doubled { get; set; }

        /// <summary>已经停牌（定牌）了。</summary>
        public bool Stood { get; set; }

        /// <summary>是不是从分牌拆出来的（拆出来的**不算黑杰克**，只算普通 21）。</summary>
        public bool FromSplit { get; set; }

        /// <summary>这手是不是"分出来的 A" —— 赌场规矩：A 分牌后每手只补一张就锁死。</summary>
        public bool SplitAcesLocked { get; set; }

        /// <summary>A 全按 1 算的点数（硬点数）。</summary>
        public int Hard => Cards.Sum(c => c.Rank >= 14 ? 1 : Math.Min(c.Rank, 10));

        /// <summary>手上有没有 A。</summary>
        public bool HasAce => Cards.Any(c => c.Rank >= 14);

        /// <summary>软牌：把某个 A 算成 11 也不超过 21（这种点数"还能再要一张而不爆"）。</summary>
        public bool IsSoft => HasAce && Hard + 10 <= 21;

        /// <summary>这手最好的点数（软牌就把 A 算 11）。</summary>
        public int Best => IsSoft ? Hard + 10 : Hard;

        /// <summary>爆了（超过 21）。</summary>
        public bool Busted => Hard > 21;

        /// <summary>
        /// **黑杰克**：起手正好两张、凑 21、**且不是分出来的**。
        /// 赔 3:2，比普通的 21 值钱就值钱在这儿。
        /// </summary>
        public bool IsBlackjack => Cards.Count == 2 && Best == 21 && !FromSplit;

        /// <summary>是不是"21 点但不是黑杰克"（三张凑的、或分出来的）—— 赔 1:1，和庄家比大小。</summary>
        public bool IsTwentyOne => Best == 21 && !IsBlackjack;

        /// <summary>这手还能不能再动作（爆了 / 停了 / 21 点了都算完结）。</summary>
        public bool IsDone => Busted || Stood || Best >= 21 || SplitAcesLocked;

        public override string ToString()
            => $"{string.Join(" ", Cards.Select(c => c.ToString()))} = {Best}{(IsSoft ? "（软）" : "")}"
               + (Busted ? " 爆" : "") + (IsBlackjack ? " ★黑杰克" : "");
    }

    // ────────────────────────────── 座位 ──────────────────────────────

    /// <summary>
    /// 一个座位（一个玩家）。**每人可能有好几手**（分牌），`ActiveHand` 指当前在打第几手。
    /// </summary>
    public sealed class BjSeat
    {
        /// <summary>座位的注（下注阶段填的），发牌时落到第 1 手上。</summary>
        public int Bet { get; set; }

        /// <summary>这个座位的所有手。分牌后长度 &gt; 1。</summary>
        public List<BjHand> Hands { get; } = new();

        /// <summary>当前在打第几手（0 起）。</summary>
        public int ActiveHand { get; set; }

        /// <summary>这个座位是不是电脑。</summary>
        public bool IsAi { get; set; }

        /// <summary>这局的净收益（正 = 赢了多少；负 = 输了多少）。结算后才有意义。</summary>
        public int Net { get; set; }

        /// <summary>当前这一手。</summary>
        public BjHand Current => Hands[ActiveHand];

        /// <summary>这个座位所有手都打完了吗。</summary>
        public bool AllDone => Hands.All(h => h.IsDone);
    }

    // ────────────────────────────── 状态 ──────────────────────────────

    /// <summary>牌靴（洗好的一叠牌）。发一张从头上取一张。</summary>
    private List<PokerCard> _shoe = new();

    /// <summary>牌靴里取到哪了。</summary>
    private int _shoePos;

    /// <summary>当前阶段。</summary>
    public BjPhase Phase { get; private set; } = BjPhase.Betting;

    /// <summary>所有座位（含电脑座位）。下标 = 座位号。</summary>
    public List<BjSeat> Seats { get; } = new();

    /// <summary>庄家的手。</summary>
    public BjHand Dealer { get; private set; } = new();

    /// <summary>当前该谁动作（座位号）；-1 = 没人了（该庄家了）。</summary>
    public int TurnSeat { get; private set; } = -1;

    /// <summary>
    /// 庄家是不是已经"偷看过"底牌（明牌是 A 或 10 时赌场会立刻查是不是黑杰克）。
    /// 是黑杰克的话**马上结算**，玩家不用白动作一轮。
    /// </summary>
    public bool DealerPeeked { get; private set; }

    /// <summary>庄家是不是黑杰克（偷看完才知道）。</summary>
    public bool DealerBlackjack { get; private set; }

    private readonly Random _rng;

    /// <param name="seatCount">几个座位（含电脑）。</param>
    /// <param name="seed">洗牌种子。传同一个种子 = 同一副牌（联机各端靠这个对齐）。</param>
    public BlackjackGame(int seatCount, int seed)
    {
        for (var i = 0; i < Math.Max(1, seatCount); i++) Seats.Add(new BjSeat());
        _rng = new Random(seed);
        Reshuffle();
    }

    // ────────────────────────────── 牌靴 ──────────────────────────────

    /// <summary>重洗一副新的牌靴。</summary>
    public void Reshuffle()
    {
        var shoe = new List<PokerCard>(52 * DeckCount);
        for (var d = 0; d < DeckCount; d++) shoe.AddRange(PokerDeck.Build());
        for (var i = shoe.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (shoe[i], shoe[j]) = (shoe[j], shoe[i]);
        }
        _shoe = shoe;
        _shoePos = 0;
    }

    /// <summary>牌靴还剩多少（0~1）。</summary>
    public float ShoeLeft => _shoe.Count == 0 ? 0f : (float)(_shoe.Count - _shoePos) / _shoe.Count;

    private PokerCard Draw()
    {
        if (_shoePos >= _shoe.Count || ShoeLeft < ReshuffleAt) Reshuffle();
        return _shoe[_shoePos++];
    }

    // ────────────────────────────── 一局的流程 ──────────────────────────────

    /// <summary>下注（发牌之前）。返回 false = 这个数不能押（得 &gt; 0）。</summary>
    public bool PlaceBet(int seat, int amount)
    {
        if (Phase != BjPhase.Betting) return false;
        if (seat < 0 || seat >= Seats.Count) return false;
        if (amount <= 0) return false;
        Seats[seat].Bet = amount;
        return true;
    }

    /// <summary>
    /// **发牌**（下注完调这个）。顺序是赌场规矩：每人一张 → 庄家一张 → 每人第二张 → 庄家第二张（暗牌）。
    ///
    /// 发完立刻做**庄家偷看**：明牌是 A 或 10 时查底牌，是黑杰克就**当场结算**。
    /// </summary>
    public void Deal()
    {
        if (Phase != BjPhase.Betting) return;

        Dealer = new BjHand();
        foreach (var s in Seats)
        {
            s.Hands.Clear();
            s.ActiveHand = 0;
            s.Net = 0;
            var h = new BjHand { Bet = s.Bet > 0 ? s.Bet : 1 };
            s.Hands.Add(h);
        }

        // 一圈明牌
        foreach (var s in Seats) s.Current.Cards.Add(Draw());
        Dealer.Cards.Add(Draw());
        // 第二圈
        foreach (var s in Seats) s.Current.Cards.Add(Draw());
        Dealer.Cards.Add(Draw());

        DealerPeeked = false;
        DealerBlackjack = Dealer.IsBlackjack;

        // 偷看：明牌是 A 或 10 才查（赌场规矩，省得每次都翻）
        var up = Dealer.Cards[0];
        var upValue = up.Rank >= 14 ? 11 : Math.Min(up.Rank, 10);
        if (upValue is 11 or 10)
        {
            DealerPeeked = true;
            if (DealerBlackjack)
            {
                // 庄家黑杰克 —— 立刻结算，不用玩家白动作
                Phase = BjPhase.Settled;
                Settle();
                return;
            }
        }

        Phase = BjPhase.PlayerTurn;
        TurnSeat = FirstSeatNeedingAction();
        if (TurnSeat < 0) DealerPlay();
    }

    /// <summary>第一个还需要动作的座位；都定完了返回 -1。</summary>
    private int FirstSeatNeedingAction()
    {
        for (var s = 0; s < Seats.Count; s++)
            if (!Seats[s].AllDone) return s;
        return -1;
    }

    /// <summary>推进到"下一个该动作的座位"（当前座位打完一手时调）。</summary>
    private void AdvanceSeat()
    {
        var seat = Seats[TurnSeat];
        while (seat.ActiveHand + 1 < seat.Hands.Count)   // 还有分出来的手没打
        {
            seat.ActiveHand++;
            if (!seat.Current.IsDone) return;
        }
        // 这个座位彻底打完了 → 找下一个
        for (var s = TurnSeat + 1; s < Seats.Count; s++)
        {
            if (!Seats[s].AllDone) { TurnSeat = s; return; }
        }
        TurnSeat = -1;
        DealerPlay();
    }

    // ────────────────────────────── 能做什么 ──────────────────────────────

    /// <summary>当前这一手能不能**要牌**。</summary>
    public bool CanHit()
    {
        if (Phase != BjPhase.PlayerTurn || TurnSeat < 0) return false;
        var h = Seats[TurnSeat].Current;
        return !h.IsDone;
    }

    /// <summary>能不能**加倍**：只有刚发的两张、没爆、注码够再押一份。</summary>
    public bool CanDouble(int purse)
    {
        if (Phase != BjPhase.PlayerTurn || TurnSeat < 0) return false;
        var h = Seats[TurnSeat].Current;
        // 分出来的 A 锁死那手不能再动；加倍只在两张时允许（分牌后有的赌场允许，这里不允许，规则更好讲）
        if (h.Cards.Count != 2 || h.IsDone || h.SplitAcesLocked) return false;
        return purse >= h.Bet;
    }

    /// <summary>能不能**分牌**：两张点数相同、还没分到上限。</summary>
    public bool CanSplit(int purse)
    {
        if (Phase != BjPhase.PlayerTurn || TurnSeat < 0) return false;
        var seat = Seats[TurnSeat];
        var h = seat.Current;
        if (h.Cards.Count != 2 || h.IsDone) return false;
        if (seat.Hands.Count >= MaxHandsPerSeat) return false;
        if (HandValue(h.Cards[0]) != HandValue(h.Cards[1])) return false;
        return purse >= h.Bet;
    }

    /// <summary>21 点里一张牌值多少（A=11、花牌=10）。**分牌按这个值比**，所以 K 和 Q 能互分。</summary>
    public static int HandValue(PokerCard c) => c.Rank >= 14 ? 11 : Math.Min(c.Rank, 10);

    // ────────────────────────────── 动作 ──────────────────────────────

    /// <summary>
    /// 当前座位做一个动作。
    /// </summary>
    /// <param name="move">要做什么。</param>
    /// <param name="purse">这个人身上还有多少钱（加倍/分牌要再掏一份，不够就不让做）。</param>
    /// <returns>做成了没有（做不成什么都不变）。</returns>
    public bool Apply(BjMove move, int purse)
    {
        if (Phase != BjPhase.PlayerTurn || TurnSeat < 0) return false;
        var seat = Seats[TurnSeat];
        var h = seat.Current;

        switch (move)
        {
            case BjMove.Hit:
                if (!CanHit()) return false;
                h.Cards.Add(Draw());
                if (h.IsDone) AdvanceSeat();
                return true;

            case BjMove.Stand:
                if (!CanHit()) return false;
                h.Stood = true;
                AdvanceSeat();
                return true;

            case BjMove.Double:
                if (!CanDouble(purse)) return false;
                h.Bet *= 2;
                h.Doubled = true;
                h.Cards.Add(Draw());
                h.Stood = true;                 // 加倍后强制停牌（不管爆没爆）
                AdvanceSeat();
                return true;

            case BjMove.Split:
                if (!CanSplit(purse)) return false;
                var second = h.Cards[1];
                h.Cards.RemoveAt(1);
                h.FromSplit = true;

                var fresh = new BjHand { Bet = h.Bet, FromSplit = true };
                fresh.Cards.Add(second);
                seat.Hands.Add(fresh);

                // 各补一张
                h.Cards.Add(Draw());
                fresh.Cards.Add(Draw());

                // 分出来的 A：赌场规矩 —— 每手只补一张就锁死，不能再要
                if (h.Cards[0].Rank >= 14) { h.SplitAcesLocked = true; h.Stood = true; }
                if (fresh.Cards[0].Rank >= 14) { fresh.SplitAcesLocked = true; fresh.Stood = true; }

                // 注意：不调 AdvanceSeat —— 还在打第 1 手（除非它已经锁死了）
                if (h.IsDone) AdvanceSeat();
                return true;

            default:
                return false;
        }
    }

    // ────────────────────────────── 庄家 ──────────────────────────────

    /// <summary>庄家补牌：不到 17 一直要，到 17 停（S17）。会一路补到爆或停。</summary>
    public void DealerPlay()
    {
        Phase = BjPhase.DealerTurn;
        TurnSeat = -1;

        // 所有人全爆了的话庄家不用补（省得看着他一张张摸）
        if (Seats.All(s => s.Hands.All(h => h.Busted)))
        {
            Settle();
            return;
        }

        while (Dealer.Best < DealerStandOn && !Dealer.Busted)
            Dealer.Cards.Add(Draw());

        Settle();
    }

    // ────────────────────────────── 结算 ──────────────────────────────

    /// <summary>算钱。每个座位每手独立和庄家比。</summary>
    private void Settle()
    {
        Phase = BjPhase.Settled;
        DealerBlackjack = Dealer.IsBlackjack;

        foreach (var seat in Seats)
        {
            seat.Net = 0;
            foreach (var h in seat.Hands)
                seat.Net += SettleHand(h);
        }
    }

    /// <summary>单手结算，返回**净收益**（正赢负输）。UI 显示"这手赢了/输了多少"时也用它。</summary>
    public int SettleHand(BjHand h)
    {
        // 爆了：直接输（哪怕庄家也爆 —— 爆在先，庄家都不用补）
        if (h.Busted) return -h.Bet;

        // 庄家黑杰克
        if (Dealer.IsBlackjack)
            return h.IsBlackjack ? 0 : -h.Bet;         // 双方都黑杰克 = 平

        // 玩家黑杰克：赔 3:2
        if (h.IsBlackjack) return (int)Math.Round(h.Bet * BlackjackPayout);

        // 庄家爆了（玩家没爆）= 赢
        if (Dealer.Busted) return h.Bet;

        // 比大小
        if (h.Best > Dealer.Best) return h.Bet;
        if (h.Best < Dealer.Best) return -h.Bet;
        return 0;                                       // 平局退注
    }

    /// <summary>这手是什么结局（给 UI 显示"黑杰克！/ 你赢了 / 平 / 你输了"）。</summary>
    public static BjOutcome OutcomeOf(BjHand h, BjHand dealer)
    {
        if (h.Busted) return BjOutcome.Lose;
        if (dealer.IsBlackjack) return h.IsBlackjack ? BjOutcome.Push : BjOutcome.Lose;
        if (h.IsBlackjack) return BjOutcome.Blackjack;
        if (dealer.Busted) return BjOutcome.Win;
        if (h.Best > dealer.Best) return BjOutcome.Win;
        if (h.Best < dealer.Best) return BjOutcome.Lose;
        return BjOutcome.Push;
    }

    /// <summary>这手的结局（用本局的庄家比）。</summary>
    public BjOutcome OutcomeOf(BjHand h) => OutcomeOf(h, Dealer);

    // ────────────────────────────── 给 UI 的快照 ──────────────────────────────

    /// <summary>这一局是不是已经结束了。</summary>
    public bool IsOver => Phase == BjPhase.Settled;

    /// <summary>庄家的**明牌**（第一张）。暗牌在玩家回合不该露出来。</summary>
    public PokerCard DealerUpCard => Dealer.Cards.Count > 0 ? Dealer.Cards[0] : default;

    /// <summary>庄家这手在玩家回合显示的点数（**只算明牌** —— 底牌翻着呢）。</summary>
    public int DealerShownTotal()
    {
        if (Phase is BjPhase.DealerTurn or BjPhase.Settled) return Dealer.Best;
        if (Dealer.Cards.Count == 0) return 0;
        var up = DealerUpCard;
        return up.Rank >= 14 ? 11 : Math.Min(up.Rank, 10);
    }
}

/// <summary>
/// 21 点的电脑策略 —— **基本策略（Basic Strategy）**。
///
/// 不是瞎猜：这是算出来的最优打法（"什么点数 vs 庄家什么明牌，期望损失最小"）。
/// 电脑同桌按它打，看起来才像个会玩的人；用户点「提示」时也是给它看这个建议。
/// </summary>
public static class BlackjackAi
{
    /// <summary>庄家明牌的点数（A 记 11，花牌记 10）。</summary>
    private static int Up(PokerCard c) => c.Rank >= 14 ? 11 : Math.Min(c.Rank, 10);

    /// <summary>
    /// 该做什么动作。
    /// </summary>
    /// <param name="hand">电脑这一手。</param>
    /// <param name="dealerUp">庄家明牌。</param>
    /// <param name="canSplit">还允许分牌吗（手里钱够 + 没分到上限）。</param>
    /// <param name="canDouble">还允许加倍吗。</param>
    public static BlackjackGame.BjMove Decide(
        BlackjackGame.BjHand hand, PokerCard dealerUp, bool canSplit, bool canDouble)
    {
        var up = Up(dealerUp);
        var total = hand.Best;

        // ── 分牌（先看，因为分牌只在两张时可行，而两张时的决策优先级最高）
        if (canSplit && hand.Cards.Count == 2)
        {
            var v = BlackjackGame.HandValue(hand.Cards[0]);
            var shouldSplit = v switch
            {
                11 => true,             // A/A 必分（两个 A 分开能拿两个 21）
                8 => true,              // 8/8 必分（16 是最烂的一手）
                10 => false,            // 10/10 别分（20 已经很好了）
                5 => false,             // 5/5 = 10，不如直接要牌
                4 => false,             // 4/4 只有庄家 5-6 且允许加倍到 9 时才分，太边缘 —— 不分
                9 => up is >= 2 and <= 9 && up != 7,
                7 => up is >= 2 and <= 7,
                6 => up is >= 2 and <= 6,
                3 or 2 => up is >= 2 and <= 7,
                _ => false,
            };
            if (shouldSplit) return BlackjackGame.BjMove.Split;
        }

        // ── 软牌（有 A 且 A 算 11 不爆）
        if (hand.IsSoft)
        {
            // 软 13~17：一路要牌（软牌要牌不会爆，稳赚）
            if (total <= 17) return BlackjackGame.BjMove.Hit;
            // 软 18
            if (total == 18)
            {
                if (canDouble && up is >= 3 and <= 6) return BlackjackGame.BjMove.Double;
                if (up is >= 9 or 10 or 11) return BlackjackGame.BjMove.Hit;   // vs 9/10/A 软18 还得要
                return BlackjackGame.BjMove.Stand;
            }
            // 软 19+ 停（稳）
            return BlackjackGame.BjMove.Stand;
        }

        // ── 硬牌
        if (total <= 8) return BlackjackGame.BjMove.Hit;
        if (total == 9)
        {
            if (canDouble && up is >= 3 and <= 6) return BlackjackGame.BjMove.Double;
            return BlackjackGame.BjMove.Hit;
        }
        if (total == 10)
        {
            if (canDouble && up is >= 2 and <= 9) return BlackjackGame.BjMove.Double;
            return BlackjackGame.BjMove.Hit;
        }
        if (total == 11)
        {
            // 11 点加倍是最爽的一手 —— 庄家不是 A 就加倍
            if (canDouble && up != 11) return BlackjackGame.BjMove.Double;
            return BlackjackGame.BjMove.Hit;
        }
        if (total == 12) return up is >= 4 and <= 6 ? BlackjackGame.BjMove.Stand : BlackjackGame.BjMove.Hit;
        if (total is >= 13 and <= 16) return up is >= 2 and <= 6 ? BlackjackGame.BjMove.Stand : BlackjackGame.BjMove.Hit;
        return BlackjackGame.BjMove.Stand;    // 17+ 停
    }

    /// <summary>
    /// 给玩家看的**建议文案**（点「提示」时用）。返回一句人话，例如「建议加倍」。
    /// </summary>
    public static string AdviceText(BlackjackGame.BjMove m) => m switch
    {
        BlackjackGame.BjMove.Hit => "建议要牌",
        BlackjackGame.BjMove.Stand => "建议停牌 —— 别再要了",
        BlackjackGame.BjMove.Double => "建议加倍 —— 这手值得下重注",
        BlackjackGame.BjMove.Split => "建议分牌 —— 拆开打更划算",
        _ => "随便打",
    };
}
