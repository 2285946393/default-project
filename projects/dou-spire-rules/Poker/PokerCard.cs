namespace DouSpireRules;

/// <summary>
/// 德州扑克的花色。**和斗地主不一样：德州看花色**（"同花"是一个正经牌型）。
/// </summary>
public enum PokerSuit
{
    Spade = 0,
    Heart = 1,
    Club = 2,
    Diamond = 3,
}

/// <summary>
/// 一张德州扑克的牌。
///
/// ⚠️ **点数口径和斗地主不一样，千万别混**（这是最容易出错的地方）：
/// <list type="bullet">
///   <item>斗地主（<see cref="Card"/>）：3=3 … K=13, A=14, <b>2=15</b>（2 最大），王=16/17。</item>
///   <item>德州（本类型）：<b>2=2 … K=13, A=14</b>（<b>A 最大</b>），**没有王**。</item>
/// </list>
/// 所以德州用**独立的类型**，不复用 <see cref="Card"/> —— 两套点数混在一起必出错。
/// </summary>
public readonly record struct PokerCard(int Rank, PokerSuit Suit)
{
    public const int MinRank = 2;
    public const int MaxRank = 14;   // A

    public bool IsAce => Rank == MaxRank;

    public static readonly PokerSuit[] AllSuits =
    {
        PokerSuit.Spade, PokerSuit.Heart, PokerSuit.Club, PokerSuit.Diamond,
    };

    /// <summary>点数显示名：2..10 / J / Q / K / A。</summary>
    public static string RankName(int rank) => rank switch
    {
        11 => "J",
        12 => "Q",
        13 => "K",
        14 => "A",
        _ => rank.ToString(),
    };

    /// <summary>花色符号（用 Unicode，免得还要找贴图）。</summary>
    public static string SuitSymbol(PokerSuit s) => s switch
    {
        PokerSuit.Spade => "♠",
        PokerSuit.Heart => "♥",
        PokerSuit.Club => "♣",
        _ => "♦",
    };

    /// <summary>花色中文名。</summary>
    public static string SuitName(PokerSuit s) => s switch
    {
        PokerSuit.Spade => "黑桃",
        PokerSuit.Heart => "红桃",
        PokerSuit.Club => "梅花",
        _ => "方块",
    };

    /// <summary>红色花色（红桃/方块）—— 画牌时用来上色。</summary>
    public bool IsRed => Suit is PokerSuit.Heart or PokerSuit.Diamond;

    public override string ToString() => RankName(Rank) + SuitSymbol(Suit);

    /// <summary>
    /// 短写解析，方便写测试：点数 + 一个花色字母。
    /// 例：<c>"As"</c> 黑桃A / <c>"Th"</c> 红桃10 / <c>"2c"</c> 梅花2 / <c>"10d"</c> 方块10。
    /// 花色字母：s=黑桃 h=红桃 c=梅花 d=方块（大小写都认）。
    /// </summary>
    public static PokerCard Parse(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) throw new ArgumentException("空牌");
        s = s.Trim();

        var suitChar = char.ToLowerInvariant(s[^1]);
        var suit = suitChar switch
        {
            's' => PokerSuit.Spade,
            'h' => PokerSuit.Heart,
            'c' => PokerSuit.Club,
            'd' => PokerSuit.Diamond,
            _ => throw new ArgumentException($"看不懂的花色：'{s[^1]}'（只能是 s/h/c/d）"),
        };

        var rankText = s[..^1].Trim().ToUpperInvariant();
        var rank = rankText switch
        {
            "2" => 2,
            "3" => 3,
            "4" => 4,
            "5" => 5,
            "6" => 6,
            "7" => 7,
            "8" => 8,
            "9" => 9,
            "10" or "T" => 10,
            "J" => 11,
            "Q" => 12,
            "K" => 13,
            "A" => 14,
            _ => throw new ArgumentException($"看不懂的点数：'{rankText}'"),
        };
        return new PokerCard(rank, suit);
    }
}

/// <summary>一副 52 张的德州牌堆（无王）。</summary>
public static class PokerDeck
{
    /// <summary>造一副 52 张。</summary>
    public static List<PokerCard> Build()
    {
        var deck = new List<PokerCard>(52);
        for (var rank = PokerCard.MinRank; rank <= PokerCard.MaxRank; rank++)
            foreach (var suit in PokerCard.AllSuits)
                deck.Add(new PokerCard(rank, suit));
        return deck;
    }

    /// <summary>用固定种子洗牌（Fisher-Yates）—— 同种子必得同一副，联机各端靠这个对齐。</summary>
    public static List<PokerCard> Shuffled(int seed)
    {
        var deck = Build();
        var rng = new Random(seed);
        for (var i = deck.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
        return deck;
    }
}
