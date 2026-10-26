namespace DouSpireRules;

/// <summary>斗地主的全部牌型。</summary>
public enum ComboType
{
    /// <summary>不成牌型，不能出。</summary>
    Invalid = 0,

    /// <summary>单张。</summary>
    Single,

    /// <summary>对子：两张同点数。</summary>
    Pair,

    /// <summary>三张：三张同点数。</summary>
    Triple,

    /// <summary>三带一：三张 + 一张单。</summary>
    TripleWithSingle,

    /// <summary>三带二：三张 + 一对。</summary>
    TripleWithPair,

    /// <summary>顺子：5 张以上连续单牌（3~A，不含 2 和王）。</summary>
    Straight,

    /// <summary>连对：3 对以上连续对子（3~A）。</summary>
    ConsecutivePairs,

    /// <summary>飞机不带：2 组以上连续三张（3~A）。</summary>
    Plane,

    /// <summary>飞机带单：k 组连续三张 + k 张单。</summary>
    PlaneWithSingles,

    /// <summary>飞机带对：k 组连续三张 + k 对。</summary>
    PlaneWithPairs,

    /// <summary>四带二（两张单）。</summary>
    FourWithTwoSingles,

    /// <summary>四带两对。</summary>
    FourWithTwoPairs,

    /// <summary>炸弹：四张及以上同点数。</summary>
    Bomb,

    /// <summary>王炸：全部王一起出（一副牌 2 张 / 两副牌 4 张）。</summary>
    Rocket,
}

/// <summary>
/// 一个合法的出牌（一组牌 + 判出来的牌型）。
/// <para><b>MainRank</b>：决定大小的那个点数。
/// 单张/对子/三张 = 该点数；三带 = 三张的点数；顺子/连对/飞机 = 最大的那个点数；
/// 四带二 = 四张的点数；炸弹 = 该点数。</para>
/// <para><b>CardCount</b>：张数。比较时要求张数一致（顺子长度、飞机组数都由它决定）。</para>
/// </summary>
public sealed record Combo(ComboType Type, int MainRank, IReadOnlyList<Card> Cards)
{
    public int CardCount => Cards.Count;

    /// <summary>顺子/连对/飞机 的"连续段数"；其他牌型为 1。</summary>
    public int ChainLength => Type switch
    {
        ComboType.Straight => CardCount,
        ComboType.ConsecutivePairs => CardCount / 2,
        ComboType.Plane => CardCount / 3,
        ComboType.PlaneWithSingles => CardCount / 4,
        ComboType.PlaneWithPairs => CardCount / 5,
        _ => 1,
    };

    public string TypeName => Type switch
    {
        ComboType.Single => "单张",
        ComboType.Pair => "对子",
        ComboType.Triple => "三张",
        ComboType.TripleWithSingle => "三带一",
        ComboType.TripleWithPair => "三带二",
        ComboType.Straight => "顺子",
        ComboType.ConsecutivePairs => "连对",
        ComboType.Plane => "飞机",
        ComboType.PlaneWithSingles => "飞机带单",
        ComboType.PlaneWithPairs => "飞机带对",
        ComboType.FourWithTwoSingles => "四带二",
        ComboType.FourWithTwoPairs => "四带两对",
        ComboType.Bomb => "炸弹",
        ComboType.Rocket => "王炸",
        _ => "不成牌型",
    };

    public string Describe()
    {
        var rankName = Type == ComboType.Rocket ? "" : Ranks.Name(MainRank);
        var chain = ChainLength > 1 ? $"{ChainLength}连" : "";
        var cards = string.Join(" ", Cards.Select(c => c.ToString()));
        return $"{TypeName}({rankName}{chain}) [{cards}]";
    }
}
