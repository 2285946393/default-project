using System.Text;

namespace DouSpireRules;

/// <summary>
/// 终端版牌桌。**这一版的界面就是"数字版"**：
/// 手牌只显示点数（3 4 5 … 2 小王 大王），选牌靠序号。
/// 之后搬进杀戮尖塔时，这里的每一块信息都对应 UI 上的一块位置：
///   顶栏底牌 / 两侧头像+剩牌数 / 中央出牌区 / 底部手牌。
/// </summary>
public static class TextTable
{
    public static void Play(int seed)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║      斗地主尖塔 · 规则内核试玩（数字版 1234）         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝");
        Console.WriteLine("  怎么玩：输入手牌前面的【序号】选牌，多个用空格隔开");
        Console.WriteLine("          比如打出 1 2 5 = 选第 1、2、5 张");
        Console.WriteLine("  p = 要不起    h = 提示我能出什么    log = 看过程    q = 退出");
        Console.WriteLine();

        var s = seed;
        while (true)
        {
            var g = GameState.NewGame(s++, new[] { "我", "电脑A", "电脑B" }, landlordSeat: -1);
            if (RunGame(g)) { Console.WriteLine("  走了，下次再打。"); return; }

            Console.Write("  再来一局？  y = 再来 / 其它键 = 退出 ：");
            var ans = Console.ReadLine();
            if (ans is null || ans.Trim().ToLowerInvariant() is not ("y" or "yes")) return;
            Console.WriteLine();
        }
    }

    /// <summary>跑一整局。返回 true = 玩家要求退出。</summary>
    private static bool RunGame(GameState g)
    {
        while (g.Winner is null)
        {
            Render(g);

            if (g.Current != 0)
            {
                Thread.Sleep(350);      // 给点节奏，能看清电脑在干什么
                var move = RuleAi.Choose(g);
                if (move is null) g.TryPass(out _);
                else g.TryPlay(move, out _);
                continue;
            }

            Console.Write(g.MustLead
                ? "  ▸ 该你领出（输入序号，p = 要不起，h = 提示）： "
                : $"  ▸ 该你，要压过 {GameState.Describe(g.LastPlay!)}（p = 要不起，h = 提示）： ");

            var line = Console.ReadLine();
            if (line is null) return true;
            line = line.Trim();
            if (line.Length == 0) continue;

            switch (line.ToLowerInvariant())
            {
                case "q" or "quit" or "exit":
                    return true;
                case "log":
                    foreach (var l in g.Log) Console.WriteLine("    " + l);
                    continue;
                case "h" or "hint":
                    ShowHints(g);
                    continue;
                case "p" or "pass" or "要不起":
                    if (!g.TryPass(out var pe)) Console.WriteLine($"  ⚠️ {pe}");
                    continue;
            }

            var (cards, err) = ParseSelection(g, line);
            if (err is not null) { Console.WriteLine($"  ⚠️ {err}"); continue; }
            if (!g.TryPlay(cards, out var e)) Console.WriteLine($"  ⚠️ {e}");
        }

        Render(g);
        Console.WriteLine();
        Console.WriteLine("  ══ " + g.ResultText());
        Console.WriteLine("  （对应游戏里：出完牌 → 另外两家一次性结算失败，赢家显示胜利）");
        Console.WriteLine();
        return false;
    }

    private static (List<Card> Cards, string? Error) ParseSelection(GameState g, string line)
    {
        var hand = g.Players[0].Hand;      // 已按点数排序
        var picked = new List<Card>();
        foreach (var tok in line.Split(new[] { ' ', ',', '，', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!int.TryParse(tok, out var idx)) return (picked, $"看不懂「{tok}」—— 请输入手牌序号（1 开始），要不起请打 p");
            if (idx < 1 || idx > hand.Count) return (picked, $"序号 {idx} 超出范围（1~{hand.Count}）");
            picked.Add(hand[idx - 1]);
        }
        return (picked, null);
    }

    private static void ShowHints(GameState g)
    {
        if (g.MustLead)
        {
            Console.WriteLine("    该你领出，任何合法牌型都行。几个例子：");
            foreach (var grp in RuleAi.Decompose(g.Me.Hand, g.Judge).Take(8))
                Console.WriteLine($"      {GameState.Describe(g.Judge.TryParse(grp)!)}");
            return;
        }
        var moves = MoveGenerator.Beating(g.Me.Hand, g.LastPlay!, g.Judge);
        if (moves.Count == 0) { Console.WriteLine("    压不住，只能要不起（p）。"); return; }
        Console.WriteLine("    能压住的：");
        foreach (var m in moves.Take(12))
            Console.WriteLine($"      {GameState.Describe(m.Combo)}");
        if (moves.Count > 12) Console.WriteLine($"      …还有 {moves.Count - 12} 种");
    }

    /// <summary>三家全部交给电脑，把一整局的流程打出来 —— 用来肉眼检查流程对不对。</summary>
    public static void Demo(int seed, int maxTurns = 200)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var g = GameState.NewGame(seed, new[] { "我", "电脑A", "电脑B" });

        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════╗");
        Console.WriteLine("║        斗地主尖塔 · 整局流程演示（三家都交给电脑）      ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════╝");

        var step = 0;
        while (g.Winner is null && step++ < maxTurns)
        {
            Render(g, "0 号位手牌");
            var move = RuleAi.Choose(g);
            if (move is null) g.TryPass(out _);
            else g.TryPlay(move, out _);
        }

        if (g.Winner is null) { Console.WriteLine($"  ⚠️ {maxTurns} 步还没打完，可能卡死了"); return; }
        Render(g, "0 号位手牌");
        Console.WriteLine();
        Console.WriteLine("  ── " + g.ResultText());
        Console.WriteLine($"  ── 一共 {g.TurnNo} 个动作");
        Console.WriteLine();
        Console.WriteLine("  对应到游戏里：赢家手牌空了 → 另外两家一次性判负，赢家显示胜利。");
    }

    private static void Render(GameState g, string handLabel = "我的手牌")
    {
        var mine = g.Players[0];          // 永远显示"我"（0 号位）的手牌，电脑只露张数

        Console.WriteLine();
        Console.WriteLine("  " + new string('─', 56));
        Console.WriteLine($"    底牌： {string.Join(" ", g.Bottom.Select(GameState.CardName))}");
        Console.WriteLine("  " + new string('─', 56));
        for (var s = 0; s < g.Players.Count; s++)
        {
            var p = g.Players[s];
            var cursor = g.Current == s ? "▶" : " ";
            var role = p.IsLandlord ? "地主" : "农民";
            var holder = s == g.TrickHolder ? "   ← 场上最大" : "";
            Console.WriteLine($"  {cursor} {s}. {p.Name,-4}{role}   剩 {p.Hand.Count,2} 张{holder}");
        }
        Console.WriteLine("  " + new string('─', 56));
        Console.WriteLine(g.LastPlay is null
            ? "    场上：（没人出牌，该领出）"
            : $"    场上：{g.Players[g.TrickHolder].Name} 出 {GameState.Describe(g.LastPlay)}");
        Console.WriteLine($"    {handLabel}（{mine.Hand.Count} 张）");
        Console.WriteLine("  " + new string('─', 56));
        Console.Write(WrapHand(mine.Hand));
        Console.WriteLine("  " + new string('─', 56));
    }

    private static string WrapHand(List<Card> hand)
    {
        var sb = new StringBuilder();
        var col = 0;
        for (var i = 0; i < hand.Count; i++)
        {
            var token = $"{i + 1,3}:{GameState.CardName(hand[i]),-2} ";
            if (col + token.Length > 52) { sb.AppendLine(); col = 0; }
            if (col == 0) sb.Append("    ");
            sb.Append(token);
            col += token.Length;
        }
        sb.AppendLine();
        return sb.ToString();
    }
}
