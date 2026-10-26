using System.Text;
using DouSpireRules;

// 斗地主规则判定器 —— 自测。
// 跑法：dotnet run
// 目的：把「牌型判得对不对」「谁压得住谁」这两件事一次验干净。
// 这里每一条都是斗地主的硬规则，写错了后面整个游戏都是错的。

Console.OutputEncoding = Encoding.UTF8;

// ——————————————————— 命令行入口：试玩 / 批量对战 ———————————————————
//   dotnet run -- --play [种子]    跟两个电脑打一局
//   dotnet run -- --sim [局数]     电脑自战 N 局，看会不会卡死
//   不带参数 = 跑规则自测

if (args.Length > 0 && args[0] is "--play" or "play")
{
    var playSeed = args.Length > 1 && int.TryParse(args[1], out var ps) ? ps : Environment.TickCount;
    TextTable.Play(playSeed);
    return 0;
}

if (args.Length > 0 && args[0] is "--demo" or "demo")
{
    var demoSeed = args.Length > 1 && int.TryParse(args[1], out var ds) ? ds : 20261006;
    TextTable.Demo(demoSeed);
    return 0;
}

if (args.Length > 0 && args[0] is "--sim" or "sim")
{
    var games = args.Length > 1 && int.TryParse(args[1], out var gc) ? gc : 500;
    var st = Simulation.RunBatch(games, 20261006, Console.WriteLine);
    Console.WriteLine();
    Console.WriteLine($"  跑了 {st.Games} 局：地主胜 {st.LandlordWins} ／ 农民胜 {st.FarmerWins}" +
                      $"（地主胜率 {100.0 * st.LandlordWins / Math.Max(1, st.LandlordWins + st.FarmerWins):F1}%）");
    Console.WriteLine($"  平均 {st.AvgTurns:F1} 回合，最长 {st.MaxTurns} 回合，卡死 {st.Stalls} 局");
    return st.Stalls == 0 ? 0 : 1;
}

//   dotnet run -- --sim4 [局数]   四人两副牌电脑自战
if (args.Length > 0 && args[0] is "--sim4" or "sim4")
{
    var games4 = args.Length > 1 && int.TryParse(args[1], out var gc4) ? gc4 : 300;
    var st4 = Simulation.RunBatch(games4, 20261008, Console.WriteLine, deckCopies: 2);
    Console.WriteLine();
    Console.WriteLine($"  四人（两副牌）跑了 {st4.Games} 局：地主胜 {st4.LandlordWins} ／ 农民胜 {st4.FarmerWins}" +
                      $"（地主胜率 {100.0 * st4.LandlordWins / Math.Max(1, st4.LandlordWins + st4.FarmerWins):F1}%）");
    Console.WriteLine($"  平均 {st4.AvgTurns:F1} 回合，最长 {st4.MaxTurns} 回合，卡死 {st4.Stalls} 局");
    return st4.Stalls == 0 ? 0 : 1;
}

//   dotnet run -- --poker        德州扑克内核自检（牌型 / 下注顺序 / 边锅 / 钱要平）
if (args.Length > 0 && args[0] is "--poker" or "poker")
{
    Console.WriteLine("德州扑克内核自检 —— 牌型 / 下注顺序 / 边锅 / 钱要平");
    Console.WriteLine(new string('═', 56));
    var (pp, pf) = PokerCrossCheck.Run(Console.WriteLine);
    Console.WriteLine();
    Console.WriteLine(new string('─', 56));
    Console.WriteLine($"通过 {pp} 条，失败 {pf} 条。");
    Console.WriteLine(new string('─', 56));
    return pf == 0 ? 0 : 1;
}

//   dotnet run -- --bj           21 点内核自检（算点 / 加倍 / 分牌 / 偷看 / 2 万局返还率）
if (args.Length > 0 && args[0] is "--bj" or "bj" or "--blackjack")
{
    Console.WriteLine("21 点内核自检 —— 算点 / 黑杰克 / 加倍 / 分牌 / 庄家偷看 / 返还率");
    Console.WriteLine(new string('═', 56));
    var (bp, bf) = BlackjackSelfTest.Run(Console.WriteLine);
    Console.WriteLine();
    Console.WriteLine(new string('─', 56));
    Console.WriteLine($"通过 {bp} 条，失败 {bf} 条。");
    Console.WriteLine(new string('─', 56));
    return bf == 0 ? 0 : 1;
}

var judge = new ComboJudge(deckCopies: 1);
var judge2 = new ComboJudge(deckCopies: 2);   // 四人斗地主：两副牌

var pass = 0;
var fail = 0;

// ——————————————————— 工具 ———————————————————

static List<Card> C(params string[] cards) => cards.Select(Card.Parse).ToList();

void Check(string name, bool ok, string? detail = null)
{
    if (ok) { pass++; Console.WriteLine($"  ✅ {name}"); }
    else { fail++; Console.WriteLine($"  ❌ {name}{(detail is null ? "" : "   ← " + detail)}"); }
}

void Section(string title)
{
    Console.WriteLine();
    Console.WriteLine($"【{title}】");
}

// ——————————————————— 一、牌型识别 ———————————————————

Section("一、牌型识别（该认出来的要认出来）");

void ShouldBe(string name, ComboType expect, List<Card> cards, ComboJudge? j = null)
{
    var c = (j ?? judge).TryParse(cards);
    Check($"{name} → {expect}", c?.Type == expect,
        c is null ? "判成了「不成牌型」" : $"实际判成 {c.TypeName}");
}

ShouldBe("3", ComboType.Single, C("3"));
ShouldBe("5 5", ComboType.Pair, C("5", "5"));
ShouldBe("7 7 7", ComboType.Triple, C("7", "7", "7"));
ShouldBe("7 7 7 3", ComboType.TripleWithSingle, C("7", "7", "7", "3"));
ShouldBe("7 7 7 3 3", ComboType.TripleWithPair, C("7", "7", "7", "3", "3"));
ShouldBe("3 4 5 6 7（顺子）", ComboType.Straight, C("3", "4", "5", "6", "7"));
ShouldBe("3 4 5 6 7 8（6 张顺子）", ComboType.Straight, C("3", "4", "5", "6", "7", "8"));
ShouldBe("10 J Q K A（顺子）", ComboType.Straight, C("10", "J", "Q", "K", "A"));
ShouldBe("3 3 4 4 5 5（连对）", ComboType.ConsecutivePairs, C("3", "3", "4", "4", "5", "5"));
ShouldBe("3 3 3 4 4 4（飞机）", ComboType.Plane, C("3", "3", "3", "4", "4", "4"));
ShouldBe("3 3 3 4 4 4 6 9（飞机带单）", ComboType.PlaneWithSingles,
    C("3", "3", "3", "4", "4", "4", "6", "9"));
ShouldBe("3 3 3 4 4 4 6 6 9 9（飞机带对）", ComboType.PlaneWithPairs,
    C("3", "3", "3", "4", "4", "4", "6", "6", "9", "9"));
ShouldBe("5 5 5 5 3 9（四带二）", ComboType.FourWithTwoSingles, C("5", "5", "5", "5", "3", "9"));
ShouldBe("5 5 5 5 3 3 9 9（四带两对）", ComboType.FourWithTwoPairs,
    C("5", "5", "5", "5", "3", "3", "9", "9"));
ShouldBe("8 8 8 8（炸弹）", ComboType.Bomb, C("8", "8", "8", "8"));
ShouldBe("小王 大王（王炸）", ComboType.Rocket, C("w", "W"));

Section("二、不该认的必须拒掉（不然玩家能乱出）");

void ShouldReject(string name, List<Card> cards, ComboJudge? j = null)
{
    var c = (j ?? judge).TryParse(cards);
    Check($"{name} → 不成牌型", c is null, $"却判成了 {c?.TypeName}");
}

ShouldReject("3 4 5 6（顺子不足 5 张）", C("3", "4", "5", "6"));
ShouldReject("3 3 4 4（连对不足 3 对）", C("3", "3", "4", "4"));
ShouldReject("J Q K A 2（顺子里不能有 2）", C("J", "Q", "K", "A", "2"));
ShouldReject("A 2 小王 大王（王不能进顺子）", C("A", "2", "w", "W"));
ShouldReject("3 3 3 4 4（三带二里那两张必须是一对）", C("3", "3", "3", "4", "5"));
ShouldReject("3 5 8（凑不成）", C("3", "5", "8"));
ShouldReject("3 3 3 4 4 4 5（飞机少一张，凑不齐）", C("3", "3", "3", "4", "4", "4", "5"));
ShouldReject("3 4 5（既不是顺子也不是三张）", C("3", "4", "5"));

Section("二·B 双向对车逮出来的边界规则（回归测试，不许退化）");

// 这几条是拿 rlcard 真实判牌器穷举 970 万种子集比出来的。原来我判定器是错的。
ShouldReject("Q Q Q Q K K K（不许拆炸弹凑飞机带单）", C("Q", "Q", "Q", "Q", "K", "K", "K"));
ShouldReject("Q Q Q Q K K K K（两个炸弹不是飞机）", C("Q", "Q", "Q", "Q", "K", "K", "K", "K"));
ShouldReject("A A A A 小王 大王（四带二不许带双王）", C("A", "A", "A", "A", "w", "W"));
ShouldReject("3 3 3 4 4 4 小王 大王（飞机带单不许带双王）", C("3", "3", "3", "4", "4", "4", "w", "W"));
ShouldReject("3 3 3 4 4 4 4（四条腿的飞机）", C("3", "3", "3", "4", "4", "4", "4"));

// 对照：这些"看着像但确实合法"的必须认
ShouldBe("Q Q Q K K K（手上有四张 Q 也不影响这手是飞机）", ComboType.Plane, C("Q", "Q", "Q", "K", "K", "K"));
ShouldBe("3 3 3 4 4 4 5 5（飞机带单，带牌是贴边的一对）", ComboType.PlaneWithSingles,
    C("3", "3", "3", "4", "4", "4", "5", "5"));
ShouldBe("3 3 3 4 4 4 5 5 5 7 7 7（飞机带单，带牌是不贴边的三张）", ComboType.PlaneWithSingles,
    C("3", "3", "3", "4", "4", "4", "5", "5", "5", "7", "7", "7"));
ShouldBe("3 3 3 4 4 4 5 5 5（三连飞机）", ComboType.Plane, C("3", "3", "3", "4", "4", "4", "5", "5", "5"));
ShouldBe("5 5 5 5 3 3（四带二带一对）", ComboType.FourWithTwoSingles, C("5", "5", "5", "5", "3", "3"));
ShouldBe("A A A A 2 小王（四带二带一张 2 和一张王）", ComboType.FourWithTwoSingles,
    C("A", "A", "A", "A", "2", "w"));

// ——————————————————— 三、大小比较 ———————————————————

Section("三、谁能压谁（斗地主的核心手感）");

void ShouldBeat(string name, string prev, string cand, bool expect, ComboJudge? j = null)
{
    var jj = j ?? judge;
    var p = jj.TryParse(C(prev.Split(' ', StringSplitOptions.RemoveEmptyEntries)));
    var c = jj.TryParse(C(cand.Split(' ', StringSplitOptions.RemoveEmptyEntries)));
    var actual = ComboJudge.Beats(c, p);
    Check($"{name}：{cand} {(expect ? "应该" : "不该")}压得住 {prev}", actual == expect);
}

ShouldBeat("大对子压小对子", "3 3", "5 5", true);
ShouldBeat("小对子压不住大对子", "5 5", "3 3", false);
ShouldBeat("单张压不了对子（牌型不同）", "3 3", "A", false);
ShouldBeat("大单张压小单张", "3", "K", true);
ShouldBeat("2 比 A 大", "A", "2", true);
ShouldBeat("大王比 2 大", "2", "W", true);
ShouldBeat("三带一比三张里的点数", "3 3 3 9", "7 7 7 4", true);
ShouldBeat("三带一 vs 三带二（牌型不同）", "7 7 7 3 3", "8 8 8 4", false);
ShouldBeat("长顺子压不住短顺子（张数不同不能比）", "3 4 5 6 7", "4 5 6 7 8 9", false);
ShouldBeat("同长顺子比最大牌", "3 4 5 6 7", "4 5 6 7 8", true);
ShouldBeat("炸弹压对子", "A A", "6 6 6 6", true);
ShouldBeat("炸弹压顺子", "3 4 5 6 7", "6 6 6 6", true);
ShouldBeat("大炸弹压小炸弹", "6 6 6 6", "9 9 9 9", true);
ShouldBeat("王炸压一切炸弹", "9 9 9 9", "w W", true);
ShouldBeat("炸弹压不住王炸", "w W", "9 9 9 9", false);
ShouldBeat("非炸弹压不住炸弹", "6 6 6 6", "A A A", false);

Section("四、四人斗地主（两副牌 108 张）");

var four = judge2;
Check("四张王 = 王炸", four.TryParse(C("w", "w", "W", "W"))?.Type == ComboType.Rocket);
Check("四张同点数 = 炸弹", four.TryParse(C("8", "8", "8", "8"))?.Type == ComboType.Bomb);
Check("五张同点数 = 炸弹（两副牌下更大）", four.TryParse(C("8", "8", "8", "8", "8"))?.Type == ComboType.Bomb);
Check("两张王在两副牌里不算王炸",
    four.TryParse(C("w", "w"))?.Type != ComboType.Rocket);

{
    var b4 = four.TryParse(C("9", "9", "9", "9"));
    var b5 = four.TryParse(C("5", "5", "5", "5", "5"));
    Check("五张炸弹压四张炸弹（先比张数）", ComboJudge.Beats(b5, b4));
}

Section("五、牌堆张数");

Check("一副牌 = 54 张", Deck.Build(1).Count == 54, $"实际 {Deck.Build(1).Count}");
Check("两副牌 = 108 张", Deck.Build(2).Count == 108, $"实际 {Deck.Build(2).Count}");

Section("六、三个人的发牌（17/17/17 + 3 底牌）");

{
    var deck = Deck.Build(1);
    var rng = new Random(20261006);            // 固定种子 → 每次跑结果一样，方便对答案
    var shuffled = deck.OrderBy(_ => rng.Next()).ToList();
    var hands = new List<List<Card>> { shuffled.Take(17).ToList(), shuffled.Skip(17).Take(17).ToList(), shuffled.Skip(34).Take(17).ToList() };
    var bottom = shuffled.Skip(51).Take(3).ToList();

    Check("三家各 17 张", hands.All(h => h.Count == 17));
    Check("底牌 3 张", bottom.Count == 3);
    Check("发完不重不漏（17×3+3 = 54）", hands.Sum(h => h.Count) + bottom.Count == 54);
}

// ——————————————————— 七、跟权威实现对车 ———————————————————

Section("七、跟开源斗地主 rlcard 的全量合法出牌表逐条对答案");

{
    // 从输出目录往上找 obj/rlcard_actions.tsv（由 README 里的导出命令生成）
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    string? tsv = null;
    for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
    {
        var candidate = Path.Combine(dir.FullName, "obj", "rlcard_actions.tsv");
        if (File.Exists(candidate)) { tsv = candidate; break; }
    }

    if (tsv is null)
    {
        Console.WriteLine("  ⚠️ 没找到 obj/rlcard_actions.tsv，跳过这一组（导出命令见 README）");
    }
    else
    {
        var (p, f) = RlcardCrossCheck.Run(tsv, Console.WriteLine);
        pass += p;
        fail += f;
    }
}

// ——————————————————— 八、双向对车（穷举子集） ———————————————————

Section("八、双向对车：穷举同一手牌的每一个子集，跟 rlcard 真实判牌器比");

{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    string? tsv = null;
    for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
    {
        var candidate = Path.Combine(dir.FullName, "obj", "rlcard_playable.tsv");
        if (File.Exists(candidate)) { tsv = candidate; break; }
    }

    if (tsv is null)
    {
        Console.WriteLine("  ⚠️ 没找到 obj/rlcard_playable.tsv，跳过（先跑 tools/export_rlcard_playable.py）");
    }
    else
    {
        var r = BidirectionalCrossCheck.Run(tsv, maxHands: 200, Console.WriteLine);
        if (r is null)
        {
            fail++;
            Console.WriteLine("  ❌ 对车数据读不出来");
        }
        else
        {
            Console.WriteLine($"  手牌 {r.Hands} 副，穷举子集 {r.SubsetsChecked:N0} 种");
            Console.WriteLine($"  我判为合法 {r.MineAccepted} 手 ／ rlcard 判为合法 {r.RlcardAccepted} 手");

            if (r.OnlyRlcard.Count > 0)
            {
                fail += r.OnlyRlcard.Count;
                Console.WriteLine($"  ❌ 有 {r.OnlyRlcard.Count} 手 rlcard 认而我漏认（真 bug）：");
                foreach (var d in r.OnlyRlcard.Take(10))
                    Console.WriteLine($"      手牌 {d.Hand} → {d.Combo}");
            }
            else
            {
                pass++;
                Console.WriteLine("  ✅ 反向干净：rlcard 能出的牌，我一分不漏");
            }

            if (r.OnlyMine.Count == 0)
            {
                pass++;
                Console.WriteLine("  ✅ 正向干净：我认得跟 rlcard 一模一样（规则完全对齐）");
            }
            else
            {
                Console.WriteLine($"  ⚠️ 有 {r.OnlyMine.Count} 手我认、rlcard 不认（规则分歧，需要拍板）：");
                foreach (var g in r.OnlyMine.GroupBy(d => d.Signature).OrderByDescending(g => g.Count()))
                {
                    var ex = g.First();
                    Console.WriteLine($"      [{g.Count()} 条] {g.Key}");
                    Console.WriteLine($"          例：手牌 {ex.Hand} → 我判成 {ex.TypeName} 的 {ex.Combo}");
                }
            }
        }
    }
}

// ——————————————————— 九、整局流程（电脑自战） ———————————————————

Section("九、电脑自战：证明这套流程不会卡死");

{
    var st = Simulation.RunBatch(games: 200, seed: 20261006, Console.WriteLine);
    Console.WriteLine($"  跑了 {st.Games} 局：地主胜 {st.LandlordWins} ／ 农民胜 {st.FarmerWins}");
    Console.WriteLine($"  平均 {st.AvgTurns:F1} 回合，最长 {st.MaxTurns} 回合");
    Check("200 局全部打完，没有卡死", st.Stalls == 0, $"{st.Stalls} 局异常");
    Check("地主胜率落在合理区间（30%~80%）",
        st.LandlordWins * 1.0 / Math.Max(1, st.LandlordWins + st.FarmerWins) is > 0.30 and < 0.80);
    Check("回合数不像无限循环（<200）", st.MaxTurns < 200, $"最长 {st.MaxTurns}");
}

// ——————————————————— 九·B、四人 2v2 组队（内核冒烟） ———————————————————

Section("九·B、四人 2v2（两副牌去 345，84 张）：发牌守恒 + 扣牌 + 自战不卡死");

{
    // 2v2 牌型：三张王 = 王炸，四张王更大（老口径两副牌要 4 张才认）
    var judge2v2 = new ComboJudge(2, rocketJokers: 3);
    ShouldBe("三张王 = 王炸（2v2 口径）", ComboType.Rocket, C("w", "w", "W"), judge2v2);
    ShouldBe("四张王 = 王炸（2v2 口径）", ComboType.Rocket, C("w", "w", "W", "W"), judge2v2);
    Check("两张王在 2v2 里只是对子", judge2v2.TryParse(C("w", "w"))?.Type == ComboType.Pair);
    {
        var r3 = judge2v2.TryParse(C("w", "w", "W"));
        var r4 = judge2v2.TryParse(C("w", "w", "W", "W"));
        Check("四张王的王炸压三张王的王炸", ComboJudge.Beats(r4, r3));
        Check("三张王的王炸压不住四张王", !ComboJudge.Beats(r3, r4));
    }

    // 发牌守恒：84 张 = 4×20 + 4 暗底；没有地主
    var g4 = GameState.NewGame(20261008, deckCopies: 2);
    var all4 = g4.Players.SelectMany(p => p.Hand).ToList();
    Check("2v2 发牌：4 家手牌合计 = 80 张", all4.Count == 80, $"实际 {all4.Count}");
    Check("2v2 发牌：每人 20 张", g4.Players.All(p => p.Hand.Count == 20),
        string.Join(",", g4.Players.Select(p => p.Hand.Count)));
    Check("2v2 发牌：暗底 4 张", g4.Bottom.Count == 4, $"实际 {g4.Bottom.Count}");
    Check("2v2 发牌：没有 3/4/5",
        all4.Concat(g4.Bottom).All(c => c.IsJoker || c.Rank >= 6));
    Check("2v2 牌堆：每张牌恰好 2 份（两副牌，共 84 张）",
        all4.Concat(g4.Bottom).GroupBy(c => c).All(grp => grp.Count() == 2));
    Check("2v2 发牌：没有地主", g4.Players.Count(p => p.IsLandlord) == 0);
    Check("2v2 组队：0+2 一队、1+3 一队",
        g4.Players[0].Team == g4.Players[2].Team && g4.Players[1].Team == g4.Players[3].Team
        && g4.Players[0].Team != g4.Players[1].Team);
    Check("2v2 开局：恰好两家要扣牌", g4.PendingDiscards.Count == 2, $"实际 {g4.PendingDiscards.Count}");
    Check("2v2 开局：扣牌的是另一队",
        g4.PendingDiscards.All(s => g4.Players[s].Team != g4.Players[g4.Current].Team));

    // 扣完牌才能打；随便扣一张走流程
    {
        var seat = g4.PendingDiscards[0];
        var card = RuleAi.ChooseDiscard(g4.Players[seat].Hand);
        Check("2v2 扣牌：扣完这张手牌少一张且摊开",
            g4.TryDiscard(seat, card, out _) && g4.Players[seat].Hand.Count == 19 && g4.DiscardPile.Count == 1);
        Check("2v2 扣牌：扣过的人不再扣", !g4.PendingDiscards.Contains(seat));
    }

    // 自战：扣牌 + 全流程必须收敛
    var st4 = Simulation.RunBatch(games: 200, seed: 20261008, Console.WriteLine, deckCopies: 2);
    Console.WriteLine($"  跑了 {st4.Games} 局（2v2，两队各胜按地主位计已无意义）");
    Console.WriteLine($"  平均 {st4.AvgTurns:F1} 回合，最长 {st4.MaxTurns} 回合");
    Check("2v2 200 局全部打完，没有卡死", st4.Stalls == 0, $"{st4.Stalls} 局异常");
    Check("2v2 回合数不像无限循环（<600）", st4.MaxTurns < 600, $"最长 {st4.MaxTurns}");

    // 3 人老路必须原样能跑（防止参数化改坏默认行为）
    var st3 = Simulation.RunBatch(games: 100, seed: 20261006, _ => { });
    Check("三人（一副牌）100 局照常打完", st3.Stalls == 0, $"{st3.Stalls} 局异常");
}

// ——————————————————— 结果 ———————————————————

Console.WriteLine();
Console.WriteLine(new string('─', 52));
Console.WriteLine($"通过 {pass} 条，失败 {fail} 条。");
Console.WriteLine(new string('─', 52));

return fail == 0 ? 0 : 1;
