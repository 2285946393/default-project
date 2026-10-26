namespace DouSpireRules;

/// <summary>
/// 电脑自战模拟。用途：
///   ① 证明这套流程**不会卡死**（两个人要不起、无限循环之类）；
///   ② 看地主胜率和平均回合数，判断规则/发牌/AI 有没有明显不对。
/// </summary>
public static class Simulation
{
    public sealed record Stats(int Games, int LandlordWins, int FarmerWins, int Stalls, int MaxTurns, double AvgTurns, int MaxStepGuard);

    public static Stats RunBatch(int games, int seed, Action<string> report, int deckCopies = 1, int playerCount = 0)
    {
        var rng = new Random(seed);
        var landlordWins = 0;
        var farmerWins = 0;
        var stalls = 0;
        var maxTurns = 0;
        var totalTurns = 0L;
        var maxGuard = 0;
        var guardLimit = deckCopies == 1 ? 2000 : 6000;   // 4 人 25 张手牌，一局天然更长，护栏放宽

        for (var i = 0; i < games; i++)
        {
            var g = GameState.NewGame(rng.Next(), deckCopies: deckCopies, playerCount: playerCount);
            var guard = 0;
            var broken = false;

            // 2v2：后手队先扣牌（AI 自动挑一张）
            foreach (var s in g.PendingDiscards.ToList())
            {
                var card = RuleAi.ChooseDiscard(g.Players[s].Hand);
                if (!g.TryDiscard(s, card, out var de))
                { report($"  ❌ 第 {i} 局：扣牌失败 —— {de}"); stalls++; broken = true; break; }
            }
            if (broken) { continue; }

            while (g.Winner is null && guard++ < guardLimit)
            {
                var move = RuleAi.Choose(g);
                if (move is null)
                {
                    if (!g.TryPass(out var e)) { report($"  ❌ 第 {i} 局：要不起失败 —— {e}"); stalls++; broken = true; break; }
                }
                else if (!g.TryPlay(move, out var e))
                {
                    report($"  ❌ 第 {i} 局：AI 出了非法牌 —— {e}");
                    stalls++;
                    broken = true;
                    break;
                }
            }

            maxGuard = Math.Max(maxGuard, guard);
            if (broken) continue;
            if (g.Winner is null) { report($"  ❌ 第 {i} 局：{guard} 步还没打完（疑似卡死）"); stalls++; continue; }

            maxTurns = Math.Max(maxTurns, g.TurnNo);
            totalTurns += g.TurnNo;
            if (g.Players[g.Winner.Value].IsLandlord) landlordWins++;
            else farmerWins++;
        }

        var done = landlordWins + farmerWins;
        var avg = done == 0 ? 0 : (double)totalTurns / done;
        return new Stats(games, landlordWins, farmerWins, stalls, maxTurns, avg, maxGuard);
    }
}
