using System.Text;

namespace DouSpireRules;

/// <summary>
/// 跟车校验：拿开源斗地主 rlcard 的**全量合法出牌表**来验我们自己的判定器。
///
/// 表从 `D:\software\doudizhu-refs\rlcard\rlcard\games\doudizhu\jsondata\card_type.json` 导出，
/// 共 27471 条"合法出牌 → 牌型 → 强度权重"。这些条目是 rlcard 用穷举生成的，
/// 可以当作斗地主规则的权威答案。
///
/// 两步验证：
///   ①【认得出】27471 条每一条，我们的判定器都要判成同一个牌型族。
///   ②【比得对】同一个牌型族内，权重大的一方必须且只能压住权重小的一方。
/// </summary>
public static class RlcardCrossCheck
{
    /// <summary>rlcard 的牌型族名 → 我们的牌型。</summary>
    private static ComboType? MapFamily(string family) => family switch
    {
        "solo" => ComboType.Single,
        "pair" => ComboType.Pair,
        "trio" => ComboType.Triple,
        "trio_solo" => ComboType.TripleWithSingle,
        "trio_pair" => ComboType.TripleWithPair,
        "solo_chain" => ComboType.Straight,
        "pair_chain" => ComboType.ConsecutivePairs,
        "trio_chain" => ComboType.Plane,
        "trio_solo_chain" => ComboType.PlaneWithSingles,
        "trio_pair_chain" => ComboType.PlaneWithPairs,
        "four_two_solo" => ComboType.FourWithTwoSingles,
        "four_two_pair" => ComboType.FourWithTwoPairs,
        "bomb" => ComboType.Bomb,
        "rocket" => ComboType.Rocket,
        _ => null,
    };

    /// <summary>把 rlcard 形如 "solo_chain_5" / "trio_solo" 压成族名。</summary>
    private static string FamilyOf(string rlcardType)
    {
        var idx = rlcardType.LastIndexOf('_');
        if (idx > 0 && rlcardType[(idx + 1)..].All(char.IsDigit))
            return rlcardType[..idx];       // solo_chain_5 → solo_chain
        return rlcardType;
    }

    private sealed record Entry(string Cards, string RlcardType, int Weight);

    /// <summary>跑完整套对车验证。返回 (通过数, 失败数)。</summary>
    public static (int Pass, int Fail) Run(string tsvPath, Action<string> report)
    {
        if (!File.Exists(tsvPath))
        {
            report($"⚠️ 找不到对车数据 {tsvPath} —— 跳过（先按 README 导出）");
            return (0, 0);
        }

        var entries = new List<Entry>();
        foreach (var line in File.ReadLines(tsvPath, Encoding.UTF8))
        {
            if (line.Length == 0) continue;
            var parts = line.Split('\t');
            if (parts.Length < 3) continue;
            entries.Add(new Entry(parts[0], parts[1], int.Parse(parts[2])));
        }

        var judge = new ComboJudge(deckCopies: 1);
        var pass = 0;
        var fail = 0;

        // ——————— ① 认得出：每一条合法出牌都要判成同一个牌型族 ———————
        report($"  （共 {entries.Count} 条权威合法出牌，逐条核对）");
        var mismatches = new List<string>();
        foreach (var e in entries)
        {
            var family = FamilyOf(e.RlcardType);
            var expect = MapFamily(family);
            if (expect is null)
            {
                fail++;
                mismatches.Add($"{e.Cards}: 没见过的牌型族 '{e.RlcardType}'");
                continue;
            }
            var got = judge.TryParse(Card.ParseHand(e.Cards));
            if (got is null)
            {
                fail++;
                if (mismatches.Count < 12) mismatches.Add($"{e.Cards} ({e.RlcardType}) → 我们判成「不成牌型」");
            }
            else if (got.Type != expect)
            {
                fail++;
                if (mismatches.Count < 12) mismatches.Add($"{e.Cards} ({e.RlcardType}) → 我们判成 {got.TypeName}");
            }
            else
            {
                pass++;
            }
        }
        foreach (var m in mismatches) report($"    ❌ {m}");

        // ——————— ② 比得对：同族内权重大者必须压得住权重小者 ———————
        var byType = entries.GroupBy(e => e.RlcardType);
        var beatChecks = 0;
        var beatMismatches = new List<string>();
        foreach (var group in byType)
        {
            // 每个权重取一条代表（同权重强度相同，不必全配对）
            var reps = group.GroupBy(e => e.Weight)
                            .Select(g => g.First())
                            .OrderBy(e => e.Weight)
                            .ToList();
            for (var i = 0; i < reps.Count; i++)
            {
                for (var j = 0; j < reps.Count; j++)
                {
                    var a = judge.TryParse(Card.ParseHand(reps[i].Cards));   // 大
                    var b = judge.TryParse(Card.ParseHand(reps[j].Cards));   // 小
                    if (a is null || b is null) continue;

                    var expectBeat = reps[i].Weight > reps[j].Weight;
                    var actualBeat = ComboJudge.Beats(a, b);
                    beatChecks++;
                    if (expectBeat == actualBeat) pass++;
                    else
                    {
                        fail++;
                        if (beatMismatches.Count < 15)
                            beatMismatches.Add($"[比大小] {reps[i].Cards}({reps[i].RlcardType},w{reps[i].Weight}) vs " +
                                              $"{reps[j].Cards}(w{reps[j].Weight}) → 我们判 {actualBeat}，应为 {expectBeat}");
                    }
                }
            }
        }
        foreach (var m in beatMismatches) report($"    ❌ {m}");
        report($"  （比大小另核对 {beatChecks} 对）");

        return (pass, fail);
    }
}
