using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using DouSpireRules;
using Godot;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace DouSpire.Table;

/// <summary>
/// DouZero 神经网络 AI（快手开源，github.com/kwai/DouZero，Apache-2.0；
/// Botzone 344 个斗地主 bot 排第一）。模型：HuggingFace palemoky/douzero-baselines
/// 的 ONNX 转换（官方 baselines 的 landlord / landlord_up / landlord_down 三位置）。
///
/// 工作方式：
///   ① 把牌局状态编码成 DouZero 的观测（手牌 54 维 one-hot、出牌历史 5×162、动作 54 维…），
///      编码**逐条对齐** douzero/env/env.py 的 get_obs —— 差一位整个 AI 就是废的；
///   ② 用我们规则内核的判定器生成**全部合法候选**（领出全枚举 / 跟牌同型更大 + 炸弹），
///      每个候选拼一行 x，一次 batch 前向拿到每个候选的胜率，取最高；
///   ③ 合法性判定永远归内核 —— 神经网络只在"合法动作里挑一个"。
///
/// 兜底铁律（用户 2026-10-09 拍板接 DouZero 时定下）：
///   模型缺失、原生库加载失败、推理抛异常 —— 一律返回 null，调用方退回规则 AI。
///   mod 可以没有 DouZero，但不能没有 AI。
///
/// ⚠️ DouZero 只有**三人斗地主**的模型。四人模式（两副牌）没有训练数据，
///    调用方在四人局里别用它（Choose 开头按人数挡掉）。
/// </summary>
public static class DouZeroAi
{
    // ══════════════ DouZero 编码常量（对齐 env.py）══════════════

    /// <summary>一副牌向量：4×13 列主序（3..A 各一列，2 一列）+ 小王 + 大王。</summary>
    private const int CardsVec = 54;

    /// <summary>历史里每手牌占 54 维。</summary>
    private const int HistStep = CardsVec;

    /// <summary>编码最近 15 手（不够前面补空）。</summary>
    private const int HistSteps = 15;

    /// <summary>z 的行数：15 手 ÷ 一轮 3 手 = 5 行，每行 3×54=162。</summary>
    private const int ZRows = 5;
    private const int ZDim = 162;

    /// <summary>x（不含动作）：地主 319 / 农民 430；拼上 54 维动作 = 373 / 484。</summary>
    private const int XLandlord = 319;
    private const int XFarmer = 430;

    private const int BombOneHotLen = 15;

    // ══════════════ 会话缓存 ══════════════

    private enum Pos { Landlord, LandlordUp, LandlordDown }

    // ⚠️ 值类型故意写 object 而不是 InferenceSession：BaseLib 启动时会反射**所有 mod 类型**
    // 的每个静态字段读 FieldType（PostModInitPatch.CheckSpecialSpireField，先读类型再判断、
    // 没有 try/catch）—— 字段签名里出现 ONNX 类型就会在启动期强制加载 ONNX 程序集，
    // 默认 ALC 不探 mod 目录，加载不到 = 游戏启动直接炸。
    // 见 notes/斗地主尖塔-启动崩溃诊断-ONNX依赖-20261009.md（方案二）。
    private static readonly Dictionary<string, object> Sessions = new();
    private static bool _nativeLoaded;
    private static bool _warned;

    /// <summary>
    /// 让 AI 出一手。返回 null = 用不了（难度设成规则 AI / 模型缺失 / 出错），
    /// 调用方退回 <see cref="RuleAi.Choose"/>。<see cref="GameState.MustLead"/> 时也可能返回
    /// 空列表以外的合法领出；要不起返回**空列表**。
    /// </summary>
    public static List<Card>? Choose(GameState g, int seat)
    {
        // 四人局（两副牌）没有 DouZero 模型 —— 明确不用，别硬编码出垃圾
        if (g.Players.Count != 3) return null;
        if (DouSpireConfig.AiLevel == DouSpireAiLevel.规则AI) return null;

        try
        {
            return ChooseCore(g, seat, DouSpireConfig.AiLevel == DouSpireAiLevel.DouZero狠);
        }
        catch (System.Exception e)
        {
            if (!_warned)
            {
                _warned = true;
                Entry.Logger.Warn($"斗地主：DouZero 推理失败，本局退回规则 AI（只报这一次）: {e.Message}");
            }
            return null;
        }
    }

    private static List<Card>? ChooseCore(GameState g, int seat, bool adp)
    {
        EnsureRuntime();
        var hand = g.Players[seat].Hand;
        if (hand.Count == 0) return new List<Card>();

        var legal = LegalMoves(g, seat);
        if (legal.Count == 0) return new List<Card>();            // 只剩"要不起"
        if (legal.Count == 1) return legal[0];                    // 只有一个候选，不用算（deep_agent 同款）

        var pos = PositionOf(g, seat);
        var (xNoAction, z) = Encode(g, seat, pos);

        var n = legal.Count;
        var xLen = xNoAction.Length;
        // ⚠️ 每行 = 状态（xLen）+ 动作 one-hot（54）拼在一起 —— 模型的 x 维度就是
        //    xLen+54（landlord 319+54=373 / 农民 430+54=484，用 _douzero-probe 探针实测）。
        //    2026-10-09 修的真 bug：原来缓冲按 n*xLen 开、tensor 也按 {n, xLen} 建，
        //    拼动作时必然越界 → "Destination array was not long enough" → DouZero 从没跑成过。
        var rowLen = xLen + CardsVec;
        var xBatch = new float[n * rowLen];
        var zBatch = new float[n * ZRows * ZDim];

        for (var i = 0; i < n; i++)
        {
            Array.Copy(xNoAction, 0, xBatch, i * rowLen, xLen);
            var act = Cards2Array(legal[i]);
            Array.Copy(act, 0, xBatch, i * rowLen + xLen, CardsVec);
            Array.Copy(z, 0, zBatch, i * ZRows * ZDim, ZRows * ZDim);
        }

        var zTensor = new DenseTensor<float>(zBatch, new[] { n, ZRows, ZDim });
        var xTensor = new DenseTensor<float>(xBatch, new[] { n, rowLen });
        var session = (InferenceSession)SessionFor(pos, adp);
        using var results = session.Run(new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("z", zTensor),
            NamedOnnxValue.CreateFromTensor("x", xTensor),
        });

        var values = results.First().AsTensor<float>().ToArray();
        var best = 0;
        for (var i = 1; i < values.Length; i++)
            if (values[i] > values[best]) best = i;

        Entry.Logger.Info($"斗地主：DouZero 出牌 {pos} 候选{n} 胜率{values[best]:0.000}");
        return legal[best];
    }

    // ══════════════ 合法动作 ══════════════

    /// <summary>
    /// 当前这一步的**全部**合法出牌（跟 DouZero 的 infoset.legal_actions 同口径）：
    /// 领出 = 手牌能凑出的所有牌型；跟牌 = 同型更大 + 炸弹 + 王炸 + 要不起。
    /// 每个候选都过一遍判定器，不可能给出去非法的。
    /// </summary>
    private static List<List<Card>> LegalMoves(GameState g, int seat)
    {
        var hand = g.Players[seat].Hand;
        var shapes = AllShapes(hand, g.Judge);
        var res = new List<List<Card>>();

        if (g.MustLead)
        {
            res.AddRange(shapes);
        }
        else
        {
            var target = g.LastPlay!;
            foreach (var s in shapes)
                if (g.Judge.TryParse(s) is { } combo && ComboJudge.Beats(combo, target))
                    res.Add(s);
            res.Add(new List<Card>());          // 要不起永远是个选项
        }
        return res;
    }

    /// <summary>
    /// 手牌能凑出的**所有**牌型（每张都过判定器）。覆盖 DouZero move_generator 的全部形状：
    /// 单张/对子/三张/三带一/三带二/顺子/连对/飞机（不带、带单、带对）/四带二单/四带二对/炸弹/王炸。
    /// 花色无关 —— 编码只看点数计数，所以同点数只枚举一次。
    /// </summary>
    private static List<List<Card>> AllShapes(IReadOnlyList<Card> hand, ComboJudge judge)
    {
        var counts = new SortedDictionary<int, int>();
        foreach (var c in hand)
            counts[c.Rank] = counts.TryGetValue(c.Rank, out var v) ? v + 1 : 1;

        var ranks = counts.Keys.OrderBy(r => r).ToList();
        List<Card> Pick(int rank, int n)
        {
            var list = new List<Card>(n);
            foreach (var c in hand)
            {
                if (c.Rank != rank) continue;
                list.Add(c);
                if (list.Count == n) break;
            }
            return list;
        }

        var res = new List<List<Card>>();
        void Add(List<Card>? cs)
        {
            if (cs is not { Count: > 0 }) return;
            if (judge.TryParse(cs) is null) return;      // 兜底：生成器再糙，出去的手牌一定合法
            res.Add(cs);
        }

        // —— 单张 / 对子 / 三张 / 炸弹 ——
        foreach (var r in ranks)
        {
            Add(Pick(r, 1));
            if (counts[r] >= 2) Add(Pick(r, 2));
            if (counts[r] >= 3) Add(Pick(r, 3));
            if (counts[r] >= 4) Add(Pick(r, 4));
        }

        // —— 王炸 ——
        if (counts.ContainsKey(Ranks.SmallJoker) && counts.ContainsKey(Ranks.BigJoker))
            Add(new List<Card> { Card.SmallJoker, Card.BigJoker });

        // —— 顺子：5..12 连张（3..A）——
        for (var len = 5; len <= 12; len++)
            for (var s = 3; s + len - 1 <= Ranks.Ace; s++)
                Add(Window(hand, s, len, 1));

        // —— 连对：3..10 连对 ——
        for (var len = 3; len <= 10; len++)
            for (var s = 3; s + len - 1 <= Ranks.Ace; s++)
                Add(Window(hand, s, len, 2));

        // —— 飞机不带：2..6 组连续三张 ——
        for (var len = 2; len <= 6; len++)
            for (var s = 3; s + len - 1 <= Ranks.Ace; s++)
                Add(Window(hand, s, len, 3));

        // —— 三带一 / 三带二 ——
        foreach (var r in ranks.Where(r => counts[r] >= 3))
        {
            var head = Pick(r, 3);
            foreach (var k in ranks.Where(k => k != r))
                Add(new List<Card>(head) { Pick(k, 1)[0] });
            foreach (var k in ranks.Where(k => k != r && counts[k] >= 2))
                Add(head.Concat(Pick(k, 2)).ToList());
        }

        // —— 飞机带单 / 带对：连续三张窗口 + 从其余点数里选 n 个单张/对子（全组合）——
        foreach (var len in new[] { 2, 3, 4, 5 })
        {
            for (var s = 3; s + len - 1 <= Ranks.Ace; s++)
            {
                var windowRanks = Enumerable.Range(s, len).ToList();
                if (windowRanks.Any(r => counts.GetValueOrDefault(r) < 3)) continue;
                var head = new List<Card>();
                foreach (var r in windowRanks) head.AddRange(Pick(r, 3));

                var kickerRanks = ranks.Where(r => !windowRanks.Contains(r)).ToList();
                foreach (var wings in Combos(kickerRanks, len, 200))
                {
                    var withSingles = new List<Card>(head);
                    foreach (var k in wings) withSingles.Add(Pick(k, 1)[0]);
                    Add(withSingles);

                    if (wings.All(k => counts[k] >= 2))
                    {
                        var withPairs = new List<Card>(head);
                        foreach (var k in wings) withPairs.AddRange(Pick(k, 2));
                        Add(withPairs);
                    }
                }
            }
        }

        // —— 四带二单 / 四带二对 ——
        foreach (var r in ranks.Where(r => counts[r] >= 4))
        {
            var head = Pick(r, 4);
            var kickers = ranks.Where(k => k != r).ToList();
            foreach (var wings in Combos(kickers, 2, 200))
            {
                var withSingles = new List<Card>(head);
                foreach (var k in wings) withSingles.Add(Pick(k, 1)[0]);
                Add(withSingles);

                if (wings.All(k => counts[k] >= 2))
                {
                    var withPairs = new List<Card>(head);
                    foreach (var k in wings) withPairs.AddRange(Pick(k, 2));
                    Add(withPairs);
                }
            }
        }

        return res;
    }

    /// <summary>
    /// 从 [start, start+len) 每个点数取 n 张。任何点数不够就返回 null。
    /// （顺子 n=1、连对 n=2、飞机不带 n=3。）
    /// </summary>
    private static List<Card>? Window(IReadOnlyList<Card> hand, int start, int len, int n)
    {
        var res = new List<Card>(len * n);
        for (var r = start; r < start + len; r++)
        {
            var part = Pick(hand, r, n);
            if (part.Count < n) return null;
            res.AddRange(part);
        }
        return res;
    }

    private static List<Card> Pick(IReadOnlyList<Card> hand, int rank, int n)
    {
        var list = new List<Card>(n);
        foreach (var c in hand)
        {
            if (c.Rank != rank) continue;
            list.Add(c);
            if (list.Count == n) break;
        }
        return list;
    }

    /// <summary>从 items 里选 k 个的全组合（按位次枚举，防重复）。超过 cap 个就停 —— 纯防御，正常到不了。</summary>
    private static List<List<int>> Combos(List<int> items, int k, int cap)
    {
        var res = new List<List<int>>();
        if (k <= 0 || items.Count < k) { res.Add(new List<int>()); return res; }
        void Backtrack(int from, List<int> cur)
        {
            if (res.Count >= cap) return;
            if (cur.Count == k) { res.Add(new List<int>(cur)); return; }
            for (var i = from; i < items.Count; i++)
            {
                cur.Add(items[i]);
                Backtrack(i + 1, cur);
                cur.RemoveAt(cur.Count - 1);
                if (res.Count >= cap) return;
            }
        }
        Backtrack(0, new List<int>());
        return res;
    }

    // ══════════════ 状态编码（逐条对齐 douzero/env/env.py 的 get_obs）══════════════

    /// <summary>座位 → DouZero 位置：地主；地主的下家 = landlord_down；上家 = landlord_up。
    /// 我们的出牌顺序 Current+1 就是下家，跟 DouZero 的 landlord→landlord_down→landlord_up 一致。</summary>
    private static Pos PositionOf(GameState g, int seat)
    {
        var l = g.Players.FindIndex(p => p.IsLandlord);
        var n = g.Players.Count;
        if (seat == l) return Pos.Landlord;
        if (seat == (l + 1) % n) return Pos.LandlordDown;
        return Pos.LandlordUp;
    }

    /// <summary>
    /// 一手牌 → 54 维向量：4×13 矩阵按**列主序**摊平（每列 = 一个点数，行 = 第几张），
    /// 末尾两位 = [有小王, 有大王]（只记"有没有"，单副牌每王最多一张）。
    /// 对齐 env.py 的 _cards2array。
    /// </summary>
    private static float[] Cards2Array(IReadOnlyList<Card> cards)
    {
        var v = new float[CardsVec];
        if (cards.Count == 0) return v;

        var counts = new Dictionary<int, int>();
        foreach (var c in cards)
            counts[c.Rank] = counts.TryGetValue(c.Rank, out var v2) ? v2 + 1 : 1;

        foreach (var (rank, cnt) in counts)
        {
            switch (rank)
            {
                case Ranks.SmallJoker: v[52] = 1; break;
                case Ranks.BigJoker: v[53] = 1; break;
                default:
                    var col = ColumnOf(rank);
                    for (var i = 0; i < cnt && i < 4; i++) v[col * 4 + i] = 1;
                    break;
            }
        }
        return v;

        static int ColumnOf(int rank) => rank switch
        {
            <= Ranks.Ace => rank - 3,      // 3..A → 0..11
            Ranks.Two => 12,               // 2 → 12
            _ => -1,                       // 王不该走到这（上面单独处理）
        };
    }

    /// <summary>剩牌数 one-hot（对齐 _get_one_hot_array：有 1 张 → 第 0 位是 1；0 张全零）。</summary>
    private static float[] OneHotCount(int count, int max)
    {
        var v = new float[max];
        if (count is >= 1) v[Math.Min(count, max) - 1] = 1;
        return v;
    }

    /// <summary>已炸次数 one-hot（对齐 _get_one_hot_bomb；超过 14 就钉在 14，别越界）。</summary>
    private static float[] BombOneHot(int bombs)
    {
        var v = new float[BombOneHotLen];
        v[Math.Min(bombs, BombOneHotLen - 1)] = 1;
        return v;
    }

    /// <summary>
    /// 当前状态 → (x_no_action, z)。
    /// x：按 env.py 三个位置函数的 hstack 顺序拼（一个分量都不能换位置）；
    /// z：最近 15 手每手 54 维，按"每 3 手一行"摊成 5×162（不足 15 手在**前面**补空）。
    /// </summary>
    private static (float[] xNoAction, float[] z) Encode(GameState g, int seat, Pos pos)
    {
        var my = Cards2Array(g.Players[seat].Hand);
        var other = Cards2Array(
            g.Players.Where(p => p.Seat != seat).SelectMany(p => p.Hand).ToList());
        var lastAction = g.LastCards is { } lc ? Cards2Array(lc) : new float[CardsVec];

        // 每家出过的牌（只看不校验）+ 每家最近一次动作（**要不起也覆盖**，对齐 game.py 的 last_move_dict）
        var played = new List<Card>[3];
        var lastBy = new List<Card>[3];
        foreach (var rec in g.History)
        {
            (played[rec.Seat] ??= new List<Card>()).AddRange(rec.Cards);
            lastBy[rec.Seat] = rec.Cards.ToList();
        }
        for (var s = 0; s < 3; s++)
        {
            played[s] ??= new List<Card>();
            lastBy[s] ??= new List<Card>();
        }

        var bombs = g.History.Count(rec => rec.Cards.Count > 0
                                           && g.Judge.TryParse(rec.Cards) is { } cb
                                           && cb.Type is ComboType.Bomb or ComboType.Rocket);
        var bomb = BombOneHot(bombs);

        var l = g.Players.FindIndex(p => p.IsLandlord);
        var n = g.Players.Count;
        var down = (l + 1) % n;      // 地主下家
        var up = (l + 2) % n;        // 地主上家

        float[] xNoAction;
        switch (pos)
        {
            case Pos.Landlord:
            {
                // env.py _get_obs_landlord：我手 / 别人手之和 / 最后一手 / 上家出过的 / 下家出过的 /
                // 上家剩牌 onehot17 / 下家剩牌 onehot17 / 炸弹数 onehot15
                xNoAction = Concat(
                    my, other, lastAction,
                    Cards2Array(played[up]), Cards2Array(played[down]),
                    OneHotCount(g.Players[up].Hand.Count, 17),
                    OneHotCount(g.Players[down].Hand.Count, 17),
                    bomb);
                break;
            }
            case Pos.LandlordUp:
            {
                // 我是地主**上家**，队友 = 地主下家。
                // env.py _get_obs_landlord_up：我手 / 别人手 / 地主出过的 / 队友出过的 / 最后一手 /
                // 地主最近一手 / 队友最近一手 / 地主剩牌 onehot20 / 队友剩牌 onehot17 / 炸弹数
                xNoAction = Concat(
                    my, other,
                    Cards2Array(played[l]), Cards2Array(played[down]),
                    lastAction, Cards2Array(lastBy[l]), Cards2Array(lastBy[down]),
                    OneHotCount(g.Players[l].Hand.Count, 20),
                    OneHotCount(g.Players[down].Hand.Count, 17),
                    bomb);
                break;
            }
            default:
            {
                // 我是地主**下家**，队友 = 地主上家。对齐 _get_obs_landlord_down。
                xNoAction = Concat(
                    my, other,
                    Cards2Array(played[l]), Cards2Array(played[up]),
                    lastAction, Cards2Array(lastBy[l]), Cards2Array(lastBy[up]),
                    OneHotCount(g.Players[l].Hand.Count, 20),
                    OneHotCount(g.Players[up].Hand.Count, 17),
                    bomb);
                break;
            }
        }

        // z：最近 15 手 → 5 行 × 162（每行 = 一轮的 3 手连在一起）
        var seq = new List<float[]>(HistSteps);
        foreach (var rec in g.History) seq.Add(Cards2Array(rec.Cards));
        while (seq.Count < HistSteps) seq.Insert(0, new float[CardsVec]);
        if (seq.Count > HistSteps) seq.RemoveRange(0, seq.Count - HistSteps);

        var z = new float[ZRows * ZDim];
        for (var row = 0; row < ZRows; row++)
        for (var m = 0; m < 3; m++)
        {
            var step = seq[row * 3 + m];
            Array.Copy(step, 0, z, row * ZDim + m * CardsVec, CardsVec);
        }

        return (xNoAction, z);
    }

    private static float[] Concat(params float[][] parts)
    {
        var total = parts.Sum(p => p.Length);
        var res = new float[total];
        var off = 0;
        foreach (var p in parts)
        {
            p.CopyTo(res, off);
            off += p.Length;
        }
        return res;
    }

    // ══════════════ ONNX 运行时 ══════════════

    private static string PosName(Pos pos) => pos switch
    {
        Pos.Landlord => "landlord",
        Pos.LandlordUp => "landlord_up",
        _ => "landlord_down",
    };

    /// <summary>
    /// mod 根目录。<see cref="DouSpireAssets.RootDir"/> 指向的是 …/assets，
    /// 而 OnnxRuntime 三件套和 onnx/ 模型都部署在它**上一级**（mod 根）。
    /// 之前直接拿 RootDir 当根用 → 永远找不到文件 → DouZero 静默退回规则 AI。
    /// </summary>
    private static string ModRoot()
    {
        var assets = DouSpireAssets.RootDir;
        if (string.IsNullOrEmpty(assets)) throw new IOException("DouZero：mod 根目录未知");
        return Path.GetDirectoryName(assets) ?? assets;
    }

    /// <summary>
    /// 显式加载原生 onnxruntime.dll + 托管 Microsoft.ML.OnnxRuntime.dll。
    /// 别指望默认的程序集解析 —— mod 是被游戏的 AssemblyLoadContext 拉起来的，
    /// 原生库和托管依赖都**自己指路**，一次加载终身受用。
    /// ⚠️ 必须**在 mod 初始化阶段就调一次**（Entry.Initialize）：等出牌才加载太晚，
    /// 启动期任何反射碰到 ONNX 类型就炸（2026-10-09 启动崩溃，方案一）。
    /// </summary>
    public static void EnsureRuntime()
    {
        if (_nativeLoaded) return;
        _nativeLoaded = true;

        var root = ModRoot();

        var managed = Path.Combine(root, "Microsoft.ML.OnnxRuntime.dll");
        if (File.Exists(managed) && System.AppDomain.CurrentDomain.GetAssemblies()
                .All(a => a.GetName().Name != "Microsoft.ML.OnnxRuntime"))
            System.Reflection.Assembly.LoadFrom(managed);   // 已加载过就不会重复 Load

        var native = Path.Combine(root, "onnxruntime.dll");
        if (File.Exists(native)) NativeLibrary.TryLoad(native, out _);
    }

    /// <summary>返回 InferenceSession（签名写 object，理由同 <see cref="Sessions"/>）。</summary>
    private static object SessionFor(Pos pos, bool adp)
    {
        var key = $"{(adp ? "ADP" : "WP")}:{pos}";
        lock (Sessions)
        {
            if (Sessions.TryGetValue(key, out var hit)) return hit;

            EnsureRuntime();
            var root = ModRoot();
            var file = Path.Combine(root, "onnx", $"douzero_{(adp ? "ADP" : "WP")}_{PosName(pos)}.onnx");
            if (!File.Exists(file)) throw new FileNotFoundException($"DouZero 模型缺失: {file}");

            Entry.Logger.Info($"斗地主：加载 DouZero 模型 {Path.GetFileName(file)}");
            var session = new InferenceSession(file);
            Sessions[key] = session;
            return session;
        }
    }
}
