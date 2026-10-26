namespace DouSpireRules;

/// <summary>
/// 21 点内核自检 —— 和 <see cref="PokerCrossCheck"/> 同一套路子：**用数据证明规则没写错**。
///
/// 跑法：<c>dotnet run -- --bj</c>
///
/// 最硬的一条是最后那个**返还率（RTP）**：按基本策略打 2 万局，
/// 标准规则（S17 + 黑杰克赔 3:2 + 6 副牌）下玩家期望返还率 ≈ **99.4%~99.7%**。
/// 只要算点、加倍、分牌、结算里**任何一处写错**，这个数就会明显跑偏（比如黑杰克赔成 1:1 会掉到 97% 以下）。
/// 比一条条看断言更能兜住系统性错误。
/// </summary>
public static class BlackjackSelfTest
{
    public static (int Pass, int Fail) Run(Action<string> log)
    {
        var pass = 0;
        var fail = 0;

        void Check(string name, bool ok, string? detail = null)
        {
            if (ok) { pass++; log($"  ✅ {name}"); }
            else { fail++; log($"  ❌ {name}{(detail is null ? "" : "   ← " + detail)}"); }
        }

        void Section(string t)
        {
            log("");
            log($"【{t}】");
        }

        // ─────────────────── 一、算点 ───────────────────

        Section("一、点数：A 到底算 1 还是 11");

        static BlackjackGame.BjHand H(params string[] cards)
        {
            var h = new BlackjackGame.BjHand();
            foreach (var c in cards) h.Cards.Add(PokerCard.Parse(c));
            return h;
        }

        Check("A + K = 21（软）", H("As", "Kd").Best == 21 && H("As", "Kd").IsSoft);
        Check("A + K 是黑杰克", H("As", "Kd").IsBlackjack);
        Check("A + 9 = 20（软）", H("As", "9d").Best == 20 && H("As", "9d").IsSoft);
        Check("A + A + 9 = 21（一个 A 算 11，另一个只能算 1）", H("As", "Ah", "9d").Best == 21);
        Check("A + A = 12（软，不是 22）", H("As", "Ah").Best == 12 && H("As", "Ah").IsSoft);
        Check("A + A + A = 13（软）", H("As", "Ah", "Ad").Best == 13 && H("As", "Ah", "Ad").IsSoft);
        Check("A + 5 + 10 = 16（A 只能算 1，硬牌）",
            H("As", "5h", "10d").Best == 16 && !H("As", "5h", "10d").IsSoft);
        Check("K + Q + 2 = 22 爆了", H("Ks", "Qd", "2c").Busted);
        Check("10 + 9 = 19 硬牌", H("10s", "9d").Best == 19 && !H("10s", "9d").IsSoft);
        Check("三张凑 21 不算黑杰克（只有起手两张算）", !H("7s", "7h", "7d").IsBlackjack);
        Check("三张凑 21 = 普通 21", H("7s", "7h", "7d").IsTwentyOne);

        // ─────────────────── 二、黑杰克的认定 ───────────────────

        Section("二、黑杰克（赔 3:2 的那手）");

        Check("A + 10 = 黑杰克", H("As", "10d").IsBlackjack);
        Check("A + J = 黑杰克", H("As", "Jd").IsBlackjack);
        Check("10 + A（顺序反过来）也算", H("10s", "Ad").IsBlackjack);
        {
            var split21 = H("As", "Kd");
            split21.FromSplit = true;               // 分出来的
            Check("分出来的 21 **不算**黑杰克（只赔 1:1）", !split21.IsBlackjack && split21.IsTwentyOne);
        }
        Check("三张的 21 不算黑杰克", !H("As", "5h", "5d").IsBlackjack);

        // ─────────────────── 三、庄家补牌（S17） ───────────────────

        Section("三、庄家：不到 17 一直要，到 17 就停");

        {
            // 造一个"庄家 16，玩家 20 停牌"的局面，看庄家会不会补到 17+
            var g = new BlackjackGame(1, 20261011);
            g.PlaceBet(0, 10);
            g.Deal();
            // 直接改牌来构造场景（白盒测试，改内部状态最快）
            SetDealer(g, "10s", "6d");              // 16 点，必须补
            SetPlayer(g, 0, "Ks", "Qd");            // 20 点
            g.DealerPlay();
            Check("庄家 16 会补牌（补完 ≥17 或爆）",
                g.Dealer.Best >= 17 || g.Dealer.Busted, $"补完 {g.Dealer.Best}");
            Check("庄家不会在 <17 时停牌", g.Dealer.Busted || g.Dealer.Best >= 17);
        }

        {
            var g = new BlackjackGame(1, 20261012);
            g.PlaceBet(0, 10);
            g.Deal();
            SetDealer(g, "10s", "7d");              // 硬 17，必须停
            SetPlayer(g, 0, "Ks", "Qd");
            g.DealerPlay();
            Check("庄家硬 17 停牌（不再补）", g.Dealer.Cards.Count == 2 && g.Dealer.Best == 17,
                $"补了 {g.Dealer.Cards.Count} 张，{g.Dealer.Best} 点");
        }

        {
            var g = new BlackjackGame(1, 20261013);
            g.PlaceBet(0, 10);
            g.Deal();
            SetDealer(g, "As", "6d");               // 软 17 —— S17 规则下**停**
            SetPlayer(g, 0, "Ks", "Qd");
            g.DealerPlay();
            Check("庄家软 17 **停牌**（S17 规则，对玩家有利）",
                g.Dealer.Cards.Count == 2 && g.Dealer.Best == 17,
                $"补了 {g.Dealer.Cards.Count} 张，{g.Dealer.Best} 点");
        }

        // ─────────────────── 四、结算 ───────────────────

        Section("四、算钱（黑杰克 3:2 / 赢 1:1 / 平退注 / 爆全输）");

        Check("玩家黑杰克净赚 1.5 倍（押 100 赢 150）",
            BlackjackGame.OutcomeOf(H("As", "Kd"), H("9s", "8d")) == BlackjackGame.BjOutcome.Blackjack);
        Check("玩家爆了 = 输（哪怕庄家也爆）",
            BlackjackGame.OutcomeOf(H("Ks", "Qd", "5c"), H("9s", "8d", "9h")) == BlackjackGame.BjOutcome.Lose);
        Check("庄家爆了、玩家没爆 = 赢",
            BlackjackGame.OutcomeOf(H("Ks", "7d"), H("9s", "8d", "9h")) == BlackjackGame.BjOutcome.Win);
        Check("20 vs 19 = 赢",
            BlackjackGame.OutcomeOf(H("Ks", "Qd"), H("9s", "10d")) == BlackjackGame.BjOutcome.Win);
        Check("18 vs 19 = 输",
            BlackjackGame.OutcomeOf(H("Ks", "8d"), H("9s", "10d")) == BlackjackGame.BjOutcome.Lose);
        Check("19 vs 19 = 平（退注）",
            BlackjackGame.OutcomeOf(H("Ks", "9d"), H("9s", "10d")) == BlackjackGame.BjOutcome.Push);
        Check("双方都黑杰克 = 平（不赔 3:2）",
            BlackjackGame.OutcomeOf(H("As", "Kd"), H("Ah", "Qd")) == BlackjackGame.BjOutcome.Push);
        Check("玩家黑杰克 vs 庄家 20 = 赢 3:2",
            BlackjackGame.OutcomeOf(H("As", "Kd"), H("Ks", "Qd")) == BlackjackGame.BjOutcome.Blackjack);
        Check("玩家 21（三张）vs 庄家黑杰克 = 输",
            BlackjackGame.OutcomeOf(H("7s", "7h", "7d"), H("As", "Kd")) == BlackjackGame.BjOutcome.Lose);

        {
            // 3:2 真的给到钱了吗（净收益 = +150）
            var g = new BlackjackGame(1, 20261014);
            g.PlaceBet(0, 100);
            g.Deal();
            SetPlayer(g, 0, "As", "Kd");            // 黑杰克
            SetDealer(g, "9s", "8d");               // 17
            g.DealerPlay();
            Check("押 100 的黑杰克，净收益 = +150", g.Seats[0].Net == 150, $"实际 {g.Seats[0].Net}");
        }

        {
            var g = new BlackjackGame(1, 20261015);
            g.PlaceBet(0, 100);
            g.Deal();
            SetPlayer(g, 0, "Ks", "9d");            // 19
            SetDealer(g, "9s", "10d");              // 19
            g.DealerPlay();
            Check("19 vs 19 平局，净收益 = 0", g.Seats[0].Net == 0, $"实际 {g.Seats[0].Net}");
        }

        {
            var g = new BlackjackGame(1, 20261016);
            g.PlaceBet(0, 100);
            g.Deal();
            SetPlayer(g, 0, "Ks", "Qd", "5c");      // 爆
            SetDealer(g, "9s", "8d");
            g.DealerPlay();
            Check("爆牌净收益 = -100", g.Seats[0].Net == -100, $"实际 {g.Seats[0].Net}");
        }

        // ─────────────────── 五、加倍 ───────────────────

        Section("五、加倍：注翻倍、只再发一张、然后强制停");

        {
            var g = new BlackjackGame(1, 20261017);
            g.PlaceBet(0, 100);
            g.Deal();
            SetPlayer(g, 0, "6s", "5d");            // 11 点 —— 教科书级的加倍手
            SetDealer(g, "9s", "7d");
            Check("11 点能加倍", g.CanDouble(1000));
            Check("身上钱不够就不能加倍（押 100 只有 50）", !g.CanDouble(50));
            g.Apply(BlackjackGame.BjMove.Double, 1000);
            Check("加倍后注码翻倍（100 → 200）", g.Seats[0].Current.Bet == 200);
            Check("加倍后只多了一张牌（共 3 张）", g.Seats[0].Current.Cards.Count == 3);
            Check("加倍后强制停牌", g.Seats[0].Current.Stood);
            Check("加倍打完这局就结束了", g.IsOver);
        }

        {
            var g = new BlackjackGame(1, 20261018);
            g.PlaceBet(0, 100);
            g.Deal();
            SetPlayer(g, 0, "Ks", "Qd", "5c");      // 三张
            Check("三张了就不能加倍（只能前两张加倍）", !g.CanDouble(1000));
        }

        // ─────────────────── 六、分牌 ───────────────────

        Section("六、分牌：拆成两手，各自独立结算");

        {
            var g = new BlackjackGame(1, 20261019);
            g.PlaceBet(0, 100);
            g.Deal();
            SetPlayer(g, 0, "8s", "8d");            // 8/8 —— 必分
            SetDealer(g, "9s", "7d");
            Check("8/8 能分牌", g.CanSplit(1000));
            Check("钱不够不能分（押 100 只有 50）", !g.CanSplit(50));
            g.Apply(BlackjackGame.BjMove.Split, 1000);
            Check("分成两手了", g.Seats[0].Hands.Count == 2);
            Check("每手各 2 张（原来那张 + 补的一张）",
                g.Seats[0].Hands.All(h => h.Cards.Count == 2));
            Check("两手都押 100（分牌要再掏一份）", g.Seats[0].Hands.All(h => h.Bet == 100));
            Check("两手都标成 FromSplit", g.Seats[0].Hands.All(h => h.FromSplit));
            Check("还在打第 1 手", g.Seats[0].ActiveHand == 0);
        }

        {
            var g = new BlackjackGame(1, 20261020);
            g.PlaceBet(0, 100);
            g.Deal();
            SetPlayer(g, 0, "Ks", "Qd");            // K/Q —— 点数都是 10，能分
            Check("K/Q 点数相同（都算 10）→ 能分",
                BlackjackGame.HandValue(PokerCard.Parse("Ks"))
                == BlackjackGame.HandValue(PokerCard.Parse("Qd")));
            SetDealer(g, "9s", "7d");
            Check("K/Q 能分牌（赌场按点数分，不按是不是同一张）", g.CanSplit(1000));
        }

        {
            var g = new BlackjackGame(1, 20261021);
            g.PlaceBet(0, 100);
            g.Deal();
            SetPlayer(g, 0, "7s", "9d");            // 点数不同
            SetDealer(g, "9s", "7d");
            Check("点数不同不能分", !g.CanSplit(1000));
        }

        {
            var g = new BlackjackGame(1, 20261022);
            g.PlaceBet(0, 100);
            g.Deal();
            SetPlayer(g, 0, "As", "Ad");            // A/A —— 必分
            SetDealer(g, "9s", "7d");
            g.Apply(BlackjackGame.BjMove.Split, 1000);
            Check("分 A 之后两手都锁死（赌场规矩：A 分牌后只补一张）",
                g.Seats[0].Hands.All(h => h.SplitAcesLocked));
            Check("分 A 之后这局直接进结算（两手都锁死了）", g.IsOver);
        }

        {
            // 分牌后两手独立结算：一手赢一手输，净收益应该相抵
            var g = new BlackjackGame(1, 20261023);
            g.PlaceBet(0, 100);
            g.Deal();
            SetPlayer(g, 0, "8s", "8d");
            SetDealer(g, "10s", "7d");              // 庄家 17
            g.Apply(BlackjackGame.BjMove.Split, 1000);
            // 手工把两手摆成：一手 20（赢）、一手 15（输）
            g.Seats[0].Hands[0].Cards.Clear();
            g.Seats[0].Hands[0].Cards.Add(PokerCard.Parse("Ks"));
            g.Seats[0].Hands[0].Cards.Add(PokerCard.Parse("Qd"));
            g.Seats[0].Hands[0].Stood = true;
            g.Seats[0].Hands[1].Cards.Clear();
            g.Seats[0].Hands[1].Cards.Add(PokerCard.Parse("10s"));
            g.Seats[0].Hands[1].Cards.Add(PokerCard.Parse("5d"));
            g.Seats[0].Hands[1].Stood = true;
            g.DealerPlay();
            Check("分牌两手独立结算（20 赢 +100，15 输 -100，净 0）",
                g.Seats[0].Net == 0, $"实际 {g.Seats[0].Net}");
        }

        // ─────────────────── 七、庄家偷看（明牌 A 或 10） ───────────────────

        Section("七、庄家偷看：明牌是 A / 10 先查底牌，是黑杰克当场结算");

        {
            var g = new BlackjackGame(1, 20261024);
            g.PlaceBet(0, 100);
            g.Deal();
            // 构造：庄家 A + K = 黑杰克，玩家 20
            SetDealer(g, "As", "Kd");
            SetPlayer(g, 0, "Ks", "Qd");
            g.DealerPlay();
            Check("庄家黑杰克，玩家 20 也照样输（不用白动作一轮）",
                g.Seats[0].Net == -100, $"实际 {g.Seats[0].Net}");
        }

        {
            var g = new BlackjackGame(1, 20261025);
            g.PlaceBet(0, 100);
            g.Deal();
            SetDealer(g, "As", "Kd");               // 庄家黑杰克
            SetPlayer(g, 0, "Ah", "Qd");            // 玩家也黑杰克
            g.DealerPlay();
            Check("双方黑杰克 = 平，净 0", g.Seats[0].Net == 0, $"实际 {g.Seats[0].Net}");
        }

        // ─────────────────── 八、电脑自战：不能卡死 ───────────────────

        Section("八、电脑自战 2 万局：不能卡死，且返还率要落在理论区间");

        {
            const int games = 20000;
            long wagered = 0;
            long net = 0;
            var stalls = 0;
            var maxActions = 0;
            var rngSeed = 20261011;

            for (var i = 0; i < games; i++)
            {
                var g = new BlackjackGame(3, rngSeed + i);     // 3 个座位（1 真人位 + 2 电脑位）
                foreach (var s in g.Seats) { s.IsAi = true; s.Bet = 100; }
                g.Deal();

                var actions = 0;
                while (g.Phase == BlackjackGame.BjPhase.PlayerTurn && g.TurnSeat >= 0)
                {
                    if (++actions > 200) { stalls++; break; }   // 兜底：真卡了就记一笔
                    var seat = g.TurnSeat;
                    var hand = g.Seats[seat].Current;
                    var move = BlackjackAi.Decide(hand, g.DealerUpCard, g.CanSplit(9999), g.CanDouble(9999));
                    if (!g.Apply(move, 9999)) g.Apply(BlackjackGame.BjMove.Stand, 9999);
                }
                maxActions = Math.Max(maxActions, actions);

                foreach (var s in g.Seats)
                {
                    wagered += s.Hands.Sum(h => h.Bet);
                    net += s.Net;
                }
            }

            Check($"{games} 局全部打完，没有卡死", stalls == 0, $"{stalls} 局异常");
            Check("单局动作数正常（<60）", maxActions < 60, $"最长 {maxActions} 个动作");

            var rtp = 1.0 + (double)net / wagered;
            log($"     总下注 {wagered:N0}，净收益 {net:N0}，返还率 RTP = {rtp * 100:F2}%");
            // 标准 S17 + BJ 3:2 + 6 副牌，基本策略下理论 RTP ≈ 99.4%~99.7%
            // 我们的 AI 是简化版（不算牌、分牌策略不完美），略低一点点也算正常
            Check("返还率落在 98.5%~100.5%（证明算点/加倍/分牌/结算没系统性写错）",
                rtp is > 0.985 and < 1.005, $"实际 {rtp * 100:F2}%");
        }

        return (pass, fail);
    }

    // ─────────────────── 白盒辅助：直接摆牌 ───────────────────

    /// <summary>把庄家那手换成指定牌（测试用，绕开洗牌）。</summary>
    private static void SetDealer(BlackjackGame g, params string[] cards)
    {
        g.Dealer.Cards.Clear();
        foreach (var c in cards) g.Dealer.Cards.Add(PokerCard.Parse(c));
    }

    /// <summary>把某个座位第 1 手换成指定牌（测试用）。</summary>
    private static void SetPlayer(BlackjackGame g, int seat, params string[] cards)
    {
        var h = g.Seats[seat].Hands[0];
        h.Cards.Clear();
        foreach (var c in cards) h.Cards.Add(PokerCard.Parse(c));
    }
}
