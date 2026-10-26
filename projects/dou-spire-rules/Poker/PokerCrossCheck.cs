namespace DouSpireRules;

/// <summary>
/// 德州内核的自检。控制台跑：<c>dotnet run -- --poker</c>。
///
/// ⚠️ **不链接进 mod** —— 和 <c>RlcardCrossCheck</c> 一样，是纯测试代码。
///
/// 验的东西按重要性排：
/// <list type="number">
///   <item><b>钱必须平</b>（最容易出 bug：边锅、平分、零头）</item>
///   <item>牌型全序 + 几个经典陷阱（轮子顺子 / A 两头）</item>
///   <item>下注顺序（翻牌前 UTG 先动、翻牌后小盲先动）</item>
///   <item>边锅 / 平分锅 / 全下自动跑牌</item>
///   <item>电脑自战几百手不卡死、钱一直平</item>
/// </list>
/// </summary>
public static class PokerCrossCheck
{
    private static List<PokerCard> P(params string[] s) => s.Select(PokerCard.Parse).ToList();

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

        // ══════════════════ 一、牌堆 ══════════════════

        Section("一、牌堆：52 张、无王、不重不漏");

        {
            var deck = PokerDeck.Build();
            Check("一副牌 52 张", deck.Count == 52, $"实际 {deck.Count}");
            Check("没有王", deck.All(c => !c.IsJokerLike()));
            Check("不重不漏", deck.Distinct().Count() == 52, $"去重后 {deck.Distinct().Count()}");
            Check("点数范围 2..14", deck.All(c => c.Rank is >= 2 and <= 14));
            Check("四种花色各 13 张",
                deck.GroupBy(c => c.Suit).Count() == 4 && deck.GroupBy(c => c.Suit).All(g => g.Count() == 13));

            var a = PokerDeck.Shuffled(20261009);
            var b = PokerDeck.Shuffled(20261009);
            Check("同种子洗出来同一副（联机各端靠这个对齐）", a.SequenceEqual(b));
            var c2 = PokerDeck.Shuffled(20261010);
            Check("不同种子洗出来不一样", !a.SequenceEqual(c2));
        }

        // ══════════════════ 二、牌型全序 ══════════════════

        Section("二、牌型全序：从皇家同花顺一路排到高牌");

        {
            var ladder = new (string Name, string[] Cards)[]
            {
                ("皇家同花顺", new[] { "As", "Ks", "Qs", "Js", "Ts" }),
                ("同花顺",     new[] { "9h", "8h", "7h", "6h", "5h" }),
                ("四条",       new[] { "Kd", "Ks", "Kc", "Kh", "7d" }),
                ("葫芦",       new[] { "Qs", "Qd", "Qc", "8h", "8d" }),
                ("同花",       new[] { "Ad", "Jd", "8d", "6d", "2d" }),
                ("顺子",       new[] { "Ts", "9d", "8c", "7h", "6s" }),
                ("三条",       new[] { "7s", "7d", "7c", "Kh", "3d" }),
                ("两对",       new[] { "Js", "Jd", "4c", "4h", "9s" }),
                ("一对",       new[] { "Ts", "Td", "Ac", "6h", "3s" }),
                ("高牌",       new[] { "As", "Kd", "9c", "6h", "3s" }),
            };

            var vals = ladder.Select(x => HandEval.Best(P(x.Cards), out _)).ToList();

            for (var i = 0; i < ladder.Length - 1; i++)
                Check($"{ladder[i].Name} > {ladder[i + 1].Name}", vals[i] > vals[i + 1],
                    $"实际 {HandEval.Name(vals[i])} vs {HandEval.Name(vals[i + 1])}");

            Check("皇家同花顺的显示名对",
                HandEval.Name(vals[0]) == "皇家同花顺", HandEval.Name(vals[0]));
        }

        Section("二·B、最容易搞错的几条（回归测试，不许退化）");

        {
            // ★ 轮子：A-2-3-4-5 是最小的顺子（5 高）
            var wheel = HandEval.Best(P("As", "2d", "3c", "4h", "5s"), out _);
            Check("A-2-3-4-5 是顺子", wheel.Category == HandCategory.Straight,
                HandEval.Name(wheel));
            Check("轮子算 5 高（不是 A 高）", wheel.K1 == 5, $"K1={wheel.K1}");
            var six = HandEval.Best(P("2s", "3d", "4c", "5h", "6s"), out _);
            Check("6 高的顺子压得住轮子", six > wheel);

            // ★ K-A-2-3-4 不是顺子（不能绕圈）
            var wrap = HandEval.Best(P("Ks", "Ad", "2c", "3h", "4s"), out _);
            Check("K-A-2-3-4 不是顺子", wrap.Category != HandCategory.Straight,
                HandEval.Name(wrap));

            // ★ 同花比大小：先比最大，再比第二…
            var f1 = HandEval.Best(P("Ad", "Jd", "8d", "6d", "2d"), out _);
            var f2 = HandEval.Best(P("Kd", "Qd", "9d", "7d", "3d"), out _);
            Check("同花 A 高压 K 高", f1 > f2);
            var f3 = HandEval.Best(P("Ad", "Td", "9d", "7d", "3d"), out _);
            Check("同花同样 A 高时比第二张（J > T）", f1 > f3);

            // ★ 葫芦比大小：先比三条，再比对子
            var b1 = HandEval.Best(P("Qs", "Qd", "Qc", "8h", "8d"), out _);
            var b2 = HandEval.Best(P("Js", "Jd", "Jc", "Ah", "Ad"), out _);
            Check("葫芦 QQQ88 压 JJJAA（先比三条）", b1 > b2);
            var b3 = HandEval.Best(P("Qs", "Qd", "Qc", "Jh", "Jd"), out _);
            Check("葫芦同样 QQQ 时比对子（8 < J）", b3 > b1);

            // ★ 两对：先比大对，再比小对，最后比散牌
            var t1 = HandEval.Best(P("As", "Ad", "5c", "5h", "Ks"), out _);
            var t2 = HandEval.Best(P("As", "Ad", "5c", "5h", "Qs"), out _);
            Check("两对一样时比散牌（K > Q）", t1 > t2);

            // ★ 四条 > 葫芦（哪怕葫芦的三条更大）
            var quads = HandEval.Best(P("2s", "2d", "2c", "2h", "3d"), out _);
            Check("四条 2 压得住葫芦 A", quads > HandEval.Best(P("As", "Ad", "Ac", "Kh", "Kd"), out _));
        }

        // ══════════════════ 三、7 张挑最好 5 张 ══════════════════

        Section("三、7 张挑最好 5 张");

        {
            var v = HandEval.Best(P("As", "Ks", "Qs", "Js", "Ts", "2h", "3d"), out var five);
            Check("7 张里有皇家同花顺就认出来", v.Category == HandCategory.StraightFlush && v.K1 == 14,
                HandEval.Name(v));
            Check("挑出来的正是那 5 张同花",
                five.All(c => c.Suit == PokerSuit.Spade) && five.Select(c => c.Rank).OrderBy(x => x).SequenceEqual(new[] { 10, 11, 12, 13, 14 }));

            var v2 = HandEval.Best(P("Ah", "Ad", "Ac", "Kh", "Kd", "2s", "3s"), out _);
            Check("7 张里有葫芦就认葫芦（不是两对）", v2.Category == HandCategory.FullHouse,
                HandEval.Name(v2));

            var v3 = HandEval.Best(P("2h", "7d", "9c", "Js", "Kd", "3s", "5s"), out _);
            Check("啥都不成就是高牌", v3.Category == HandCategory.HighCard, HandEval.Name(v3));

            // 6 张也要能挑
            var v4 = HandEval.Best(P("Ah", "Kh", "Qh", "Jh", "Th", "2s"), out _);
            Check("6 张里也能挑出皇家同花顺", v4.Category == HandCategory.StraightFlush && v4.K1 == 14);
        }

        // ══════════════════ 四、开局与下注顺序 ══════════════════

        Section("四、开局：盲注扣钱 + 谁先动");

        {
            var g = HoldemGame.NewGame(12345, new[] { "我", "电脑A", "电脑B" }, 1000, 50, 100, button: 0);
            Check("庄家 0 → 小盲 1、大盲 2", true);
            Check("小盲扣了 50", g.Players[1].CommittedTotal == 50, $"{g.Players[1].CommittedTotal}");
            Check("大盲扣了 100", g.Players[2].CommittedTotal == 100, $"{g.Players[2].CommittedTotal}");
            Check("底池 = 150", g.Pot == 150, $"{g.Pot}");
            Check("★ 翻牌前从大盲下家开始（枪口位 = 座位 0）", g.CurrentSeat == 0, $"实际 {g.CurrentSeat}");
            Check("当前注 = 大盲 100", g.CurrentBet == 100, $"{g.CurrentBet}");
            Check("最小加注到 200", g.MinRaiseTo == 200, $"{g.MinRaiseTo}");
            Check("每人 2 张底牌", g.Players.All(p => p.Hole.Length == 2));
            Check("开局钱是平的", g.MoneyBalanced());
        }

        {
            // 三人全部跟注 → 翻牌圈该轮到小盲（庄家下家）
            var g = HoldemGame.NewGame(12345, new[] { "我", "电脑A", "电脑B" }, 1000, 50, 100, button: 0);
            Check("座位 0 跟注", g.Apply(PokerAction.Call, 0, out _));
            Check("座位 1（小盲）跟注", g.Apply(PokerAction.Call, 0, out _));
            Check("座位 2（大盲）过牌（大盲有权）", g.Apply(PokerAction.Check, 0, out _));
            Check("进翻牌圈", g.Street == Street.Flop, $"{g.Street}");
            Check("翻了 3 张公共牌", g.Board.Count == 3, $"{g.Board.Count}");
            Check("★ 翻牌后从小盲位开始（座位 1）", g.CurrentSeat == 1, $"实际 {g.CurrentSeat}");
            Check("翻牌圈当前注清零", g.CurrentBet == 0, $"{g.CurrentBet}");
            Check("底池还是 300", g.Pot == 300, $"{g.Pot}");
            Check("钱还是平的", g.MoneyBalanced());
        }

        Section("四·B、大盲的最后发言权（大家都跟了，大盲还能加注）");

        {
            var g = HoldemGame.NewGame(999, new[] { "我", "电脑A", "电脑B" }, 1000, 50, 100, button: 0);
            g.Apply(PokerAction.Call, 0, out _);
            g.Apply(PokerAction.Call, 0, out _);
            Check("轮到大盲时还没进翻牌（他有发言权）", g.Street == Street.Preflop && g.CurrentSeat == 2);
            Check("大盲加注到 400", g.Apply(PokerAction.Raise, 400, out var e1), e1);
            Check("加注后别人要重新表态（座位 0 又轮到了）",
                !g.Players[0].ActedThisStreet && !g.Players[1].ActedThisStreet);
            Check("当前注变成 400", g.CurrentBet == 400, $"{g.CurrentBet}");
            Check("钱是平的", g.MoneyBalanced());
        }

        Section("四·C、非法动作必须被拒（不然玩家能乱来）");

        {
            var g = HoldemGame.NewGame(7, new[] { "我", "电脑A", "电脑B" }, 1000, 50, 100, button: 0);
            Check("有人下注时不能过牌", !g.Apply(PokerAction.Check, 0, out _));
            Check("加注低于最小额被拒", !g.Apply(PokerAction.Raise, 150, out _));
            Check("加注到 200（正好最小）可以", g.Apply(PokerAction.Raise, 200, out _));
            Check("加注超过身家被拒", !g.Apply(PokerAction.Raise, 99999, out _));
        }

        // ══════════════════ 五、全下 + 边锅 ══════════════════

        Section("五、全下：全下后自动把牌发完，直接摊牌");

        {
            var g = HoldemGame.NewGame(2026, new[] { "我", "电脑A", "电脑B" }, 1000, 50, 100, button: 0);
            Check("座位 0 全下", g.Apply(PokerAction.AllIn, 0, out _));
            Check("座位 1 全下", g.Apply(PokerAction.AllIn, 0, out _));
            Check("座位 2 全下", g.Apply(PokerAction.AllIn, 0, out _));
            Check("三家全下后这手就结束了", g.HandOver);
            Check("公共牌自动发满 5 张", g.Board.Count == 5, $"{g.Board.Count}");
            Check("有赢家", g.Winners.Count > 0);
            Check("★ 钱是平的", g.MoneyBalanced());
            Check("★ 输赢加起来是 0", g.ZeroSum(), string.Join(",", g.ResultDelta));
        }

        Section("五·B、边锅：筹码少的全下，只能赢他够得着的那一份");

        {
            // 我 100、A 1000、B 1000。我全下 100，A 加到 300，B 跟 300。
            // （A/B 留够筹码 → 这手不会当场结束，方便在中途检查"谁投了多少"）
            var g = HoldemGame.NewGame(555, new[] { "我", "电脑A", "电脑B" }, 1000, 50, 100,
                button: 0, stacks: new[] { 100, 1000, 1000 });

            Check("座位 0（我）全下 100", g.Apply(PokerAction.AllIn, 0, out var e0), e0);
            Check("座位 1 加注到 300", g.Apply(PokerAction.Raise, 300, out var e1), e1);
            Check("座位 2 跟注到 300", g.Apply(PokerAction.Call, 0, out var e2), e2);

            Check("三人投入 = 100/300/300",
                g.Players[0].CommittedTotal == 100 && g.Players[1].CommittedTotal == 300 && g.Players[2].CommittedTotal == 300,
                string.Join("/", g.Players.Select(p => p.CommittedTotal)));

            var pots = g.ComputePots();
            Check("分出两个锅（主锅 + 边锅）", pots.Count == 2, $"实际 {pots.Count} 个");
            if (pots.Count == 2)
            {
                Check("主锅 300，三家都能分", pots[0].Amount == 300 && pots[0].Eligible.Count == 3,
                    $"{pots[0].Amount} / {pots[0].Eligible.Count} 家");
                Check("边锅 400，只有 A 和 B 能分", pots[1].Amount == 400 && pots[1].Eligible.Count == 2,
                    $"{pots[1].Amount} / {pots[1].Eligible.Count} 家");
                Check("两个锅加起来 = 总底池 700", pots.Sum(p => p.Amount) == 700 && g.Pot == 700,
                    $"锅合计 {pots.Sum(p => p.Amount)}，底池 {g.Pot}");
            }

            // 一路过牌到摊牌（我已经全下，不再行动）
            for (var i = 0; i < 8 && !g.HandOver; i++) g.Apply(PokerAction.Check, 0, out _);

            Check("打到摊牌了", g.HandOver && g.Street == Street.Showdown, $"{g.Street}");
            Check("最终底池 700", g.FinalPot == 700, $"{g.FinalPot}");
            Check("★ 短筹码（我）最多只能拿回 300",
                g.Players[0].Stack <= 300, $"实际 {g.Players[0].Stack}");
            Check("★ 钱是平的", g.MoneyBalanced());
            Check("★ 输赢加起来是 0", g.ZeroSum(), string.Join(",", g.ResultDelta));
        }

        // ══════════════════ 六、平分锅 ══════════════════

        Section("六、平分锅：公共牌就是最大的牌 → 三家平分");

        {
            // 公共牌 A K Q J T（顺子），三家的底牌都接不上、也凑不出同花 → 三家都打公共牌
            var deck = P(
                // 底牌：发牌顺序是 座位1、座位2、座位0（各一张），转两圈
                "2c", "4d", "6h",   // 第一圈：座位1、座位2、座位0
                "3c", "5d", "7h",   // 第二圈：座位1、座位2、座位0
                // 公共牌
                "As", "Kd", "Qh", "Jc", "Ts"
            );
            var g = HoldemGame.NewGameWithDeck(new[] { "我", "电脑A", "电脑B" }, new[] { 1000, 1000, 1000 },
                50, 100, button: 0, deck);

            Check("座位 0 拿到 6♥ 7♥（发牌顺序：从庄家下家开始，一人一张转两圈）",
                g.Players[0].Hole[0].ToString() == "6♥" && g.Players[0].Hole[1].ToString() == "7♥",
                string.Join(" ", g.Players[0].Hole));

            // 三家全跟，然后一路过牌到摊牌
            g.Apply(PokerAction.Call, 0, out _);
            g.Apply(PokerAction.Call, 0, out _);
            g.Apply(PokerAction.Check, 0, out _);
            for (var street = 0; street < 3; street++)
            {
                if (g.HandOver) break;
                g.Apply(PokerAction.Check, 0, out _);
                g.Apply(PokerAction.Check, 0, out _);
                g.Apply(PokerAction.Check, 0, out _);
            }

            Check("打到摊牌了", g.HandOver && g.Street == Street.Showdown, $"{g.Street}");
            Check("公共牌 5 张", g.Board.Count == 5);
            Check("★ 三家都赢（平分）", g.Winners.Count == 3, $"实际 {g.Winners.Count} 家");
            Check("★ 每家拿回自己那份（输赢都是 0）",
                g.ResultDelta.All(d => d == 0), string.Join(",", g.ResultDelta));
            Check("★ 钱是平的", g.MoneyBalanced());
        }

        Section("六·B、弃牌：只剩一个人没弃，他直接赢（不用摊牌）");

        {
            var g = HoldemGame.NewGame(31, new[] { "我", "电脑A", "电脑B" }, 1000, 50, 100, button: 0);
            g.Apply(PokerAction.Fold, 0, out _);
            g.Apply(PokerAction.Fold, 0, out _);
            Check("两个人都弃了 → 这手结束", g.HandOver);
            Check("赢家是大盲（座位 2）", g.Winners.Count == 1 && g.Winners[0] == 2, string.Join(",", g.Winners));
            Check("赢的是底池 150", g.Players[2].Delta == 50, $"{g.Players[2].Delta}");
            Check("没摊牌", g.WinKind == "fold", g.WinKind);
            Check("★ 钱是平的", g.MoneyBalanced());
            Check("★ 输赢加起来是 0", g.ZeroSum());
        }

        // ══════════════════ 七、电脑自战 ══════════════════

        Section("七、电脑自战：几百手不卡死，钱一直平");

        foreach (var seats in new[] { 3, 4 })
        {
            var r = SelfPlay(hands: 240, seed: 20261009 + seats, players: seats);
            Check($"{seats} 人：240 手全部打完（没有卡死）", r.Stalls == 0, $"{r.Stalls} 手异常");
            Check($"{seats} 人：★ 每手结束钱都平", r.BadMoney == 0, $"{r.BadMoney} 手不平");
            Check($"{seats} 人：★ 每手输赢加起来都是 0", r.BadZeroSum == 0, $"{r.BadZeroSum} 手不为 0");
            Check($"{seats} 人：动作数不像死循环", r.MaxActions < 400, $"最长 {r.MaxActions} 步");
            log($"     （{seats} 人桌：平均每手 {r.AvgActions:F1} 步，最长 {r.MaxActions} 步，" +
                $"平均底池 {r.AvgPot:F0}）");
        }

        // ══════════════════ 八、AI 的胜率估算像不像话 ══════════════════

        Section("八、AI 的胜率估算（蒙特卡洛）合不合理");

        {
            var rng = new Random(20261009);

            // ⚠️ 指定牌堆时的发牌顺序（庄家=0、2 人）：座位 1、座位 0、座位 1、座位 0，
            //    接着才是公共牌。所以 **座位 0 拿的是第 2 张和第 4 张**。
            //    翻牌前 AA 对一个随机对手：胜率应该在 85% 上下。
            var g = HoldemGame.NewGameWithDeck(new[] { "我", "电脑A" }, new[] { 1000, 1000 }, 50, 100, button: 0,
                P("2c", "As", "7h", "Ad",
                  "3s", "4s", "5d", "8c", "9h"));
            Check("座位 0 拿到了 AA",
                g.Players[0].Hole[0].ToString() == "A♠" && g.Players[0].Hole[1].ToString() == "A♦",
                string.Join(" ", g.Players[0].Hole));
            var aa = PokerAi.Equity(g, 0, rollouts: 4000, rng);
            Check("翻牌前 AA 对随机牌胜率 ≈ 80~93%", aa is > 0.80 and < 0.93, $"实际 {aa:P1}");

            // 已经成了皇家同花顺 → 必赢
            var g2 = HoldemGame.NewGameWithDeck(new[] { "我", "电脑A" }, new[] { 1000, 1000 }, 50, 100, button: 0,
                P("2c", "As", "7h", "Ks",
                  "Qs", "Js", "Ts", "3d", "4d"));
            Check("座位 0 拿到了 A♠ K♠",
                g2.Players[0].Hole[0].ToString() == "A♠" && g2.Players[0].Hole[1].ToString() == "K♠",
                string.Join(" ", g2.Players[0].Hole));
            g2.Apply(PokerAction.AllIn, 0, out _);
            g2.Apply(PokerAction.AllIn, 0, out _);
            Check("★ 手上是皇家同花顺 → 这手必赢",
                g2.Winners.Count == 1 && g2.Winners[0] == 0, string.Join(",", g2.Winners));
            Check("赢的牌型显示成皇家同花顺",
                HandEval.Name(g2.HandOf(0)) == "皇家同花顺", HandEval.Name(g2.HandOf(0)));

            // 7-2 这种最烂的起手牌，对一个随机对手胜率应该明显低于 50%
            var g3 = HoldemGame.NewGameWithDeck(new[] { "我", "电脑A" }, new[] { 1000, 1000 }, 50, 100, button: 0,
                P("3c", "7h", "4d", "2c",
                  "5s", "6s", "8d", "9c", "Th"));
            Check("座位 0 拿到了 7♥ 2♣",
                g3.Players[0].Hole[0].ToString() == "7♥" && g3.Players[0].Hole[1].ToString() == "2♣",
                string.Join(" ", g3.Players[0].Hole));
            var junk = PokerAi.Equity(g3, 0, rollouts: 3000, rng);
            Check("翻牌前 7-2 垃圾牌胜率明显低于 50%", junk < 0.45, $"实际 {junk:P1}");
        }

        // ══════════════════ 九、AI 出招永远合法 ══════════════════

        Section("九、AI 出的每一招都必须合法（不然牌局会卡住）");

        {
            var rng = new Random(4242);
            var bad = 0;
            var total = 0;
            for (var h = 0; h < 120; h++)
            {
                var g = HoldemGame.NewGame(9000 + h, null, 1000, 50, 100);
                var style = (PokerAi.Style)(h % 3);
                var guard = 0;
                while (!g.HandOver && guard++ < 400)
                {
                    var (a, amt) = PokerAi.Choose(g, style, rng, rollouts: 40);
                    total++;
                    if (!g.Apply(a, amt, out _)) { bad++; break; }
                }
            }
            Check($"AI 出了 {total} 招，零非法", bad == 0, $"{bad} 招非法");
        }

        return (pass, fail);
    }

    private readonly record struct SimResult(int Hands, int BadMoney, int BadZeroSum, int Stalls,
        int MaxActions, double AvgActions, double AvgPot);

    /// <summary>电脑自战 N 手，专盯"钱平不平""会不会卡死"。</summary>
    private static SimResult SelfPlay(int hands, int seed, int players)
    {
        var rng = new Random(seed);
        int badMoney = 0, badZeroSum = 0, stalls = 0, maxActions = 0;
        long actionSum = 0;
        long potSum = 0;

        for (var h = 0; h < hands; h++)
        {
            // 筹码故意给得参差不齐 —— 这样才会频繁出现全下和边锅
            var stacks = Enumerable.Range(0, players).Select(_ => 200 + rng.Next(1801)).ToList();
            var g = HoldemGame.NewGame(seed * 7919 + h, null, 1000, 50, 100, -1, stacks);
            var style = (PokerAi.Style)(h % 3);
            var actions = 0;

            while (!g.HandOver && actions < 400)
            {
                if (!g.MoneyBalanced()) { badMoney++; break; }
                var (a, amt) = PokerAi.Choose(g, style, rng, rollouts: 40);
                if (!g.Apply(a, amt, out _))
                {
                    var legal = g.LegalActions();
                    var fallback = legal.Contains(PokerAction.Check) ? PokerAction.Check : PokerAction.Fold;
                    if (!g.Apply(fallback, 0, out _)) { stalls++; break; }
                }
                actions++;
            }

            if (actions >= 400) stalls++;
            maxActions = Math.Max(maxActions, actions);
            actionSum += actions;
            potSum += g.FinalPot;
            if (!g.MoneyBalanced()) badMoney++;
            if (!g.ZeroSum()) badZeroSum++;
        }

        return new SimResult(hands, badMoney, badZeroSum, stalls, maxActions,
            (double)actionSum / hands, (double)potSum / hands);
    }
}

/// <summary>小工具：德州牌堆里不该有"王"这种东西，用这个断言。</summary>
internal static class PokerCardExt
{
    /// <summary>永远 false —— 德州没有王。写成一个方法是为了让测试读起来直白。</summary>
    public static bool IsJokerLike(this PokerCard c) => c.Rank is < 2 or > 14;
}
