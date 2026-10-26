namespace DouSpireRules;

/// <summary>花色。斗地主不看花色，但保留它以便将来做牌面显示。</summary>
public enum Suit
{
    Spade,
    Heart,
    Club,
    Diamond,
    Joker,
}

/// <summary>
/// 一张牌。点数用整数表示，越大越强：
/// 3=3 … 10=10、J=11、Q=12、K=13、A=14、2=15、小王=16、大王=17。
/// （注意：A 比 K 大但比 2 小；2 比 A 大；王最大。）
/// </summary>
public readonly record struct Card(int Rank, Suit Suit)
{
    public bool IsJoker => Suit == Suit.Joker;

    public static Card SmallJoker => new(Ranks.SmallJoker, Suit.Joker);
    public static Card BigJoker => new(Ranks.BigJoker, Suit.Joker);

    /// <summary>能参与顺子 / 连对 / 飞机的点数上限：A（14）。2 和王不参与。</summary>
    public bool IsChainable => Rank <= Ranks.Ace;

    /// <summary>
    /// 解析 rlcard（开源斗地主参考实现）风格的牌串，如 "3456789TJQKA2BR"。
    /// T=10、B=小王、R=大王。用来跟权威动作表对答案。
    /// </summary>
    public static List<Card> ParseHand(string s)
    {
        var list = new List<Card>();
        foreach (var ch in s)
        {
            list.Add(ch switch
            {
                'T' => new Card(10, Suit.Spade),
                'B' => Card.SmallJoker,
                'R' => Card.BigJoker,
                _ => Card.Parse(ch.ToString()),
            });
        }
        return list;
    }

    public override string ToString() => Ranks.Name(Rank);

    /// <summary>从短写解析一张牌，方便写测试：3/10/J/Q/K/A/2/w(小王)/W(大王)。花色忽略。</summary>
    public static Card Parse(string s) => s.Trim() switch
    {
        "3" => new Card(3, Suit.Spade),
        "4" => new Card(4, Suit.Spade),
        "5" => new Card(5, Suit.Spade),
        "6" => new Card(6, Suit.Spade),
        "7" => new Card(7, Suit.Spade),
        "8" => new Card(8, Suit.Spade),
        "9" => new Card(9, Suit.Spade),
        "10" or "T" => new Card(10, Suit.Spade),
        "J" or "j" => new Card(11, Suit.Spade),
        "Q" or "q" => new Card(12, Suit.Spade),
        "K" or "k" => new Card(13, Suit.Spade),
        "A" or "a" => new Card(14, Suit.Spade),
        "2" => new Card(15, Suit.Spade),
        "w" => Card.SmallJoker,
        "W" => Card.BigJoker,
        _ => throw new ArgumentException($"看不懂的牌：'{s}'"),
    };
}

/// <summary>点数常量与显示名。</summary>
public static class Ranks
{
    public const int Jack = 11;
    public const int Queen = 12;
    public const int King = 13;
    public const int Ace = 14;
    public const int Two = 15;
    public const int SmallJoker = 16;
    public const int BigJoker = 17;

    public static string Name(int rank) => rank switch
    {
        11 => "J",
        12 => "Q",
        13 => "K",
        14 => "A",
        15 => "2",
        16 => "小王",
        17 => "大王",
        _ => rank.ToString(),
    };
}

/// <summary>一副 / 多副牌。</summary>
public static class Deck
{
    /// <summary>
    /// 造牌。<paramref name="copies"/> = 1 → 54 张（三人斗地主）；2 → 108 张（四人斗地主·1v3 老口径）。
    /// <paramref name="strip345"/> = 两副牌去掉 3、4、5（欢乐斗地主 2v2 组队标准：84 张，最小从 6 起）。
    /// </summary>
    public static List<Card> Build(int copies = 1, bool strip345 = false)
    {
        var deck = new List<Card>();
        var suits = new[] { Suit.Spade, Suit.Heart, Suit.Club, Suit.Diamond };
        var minRank = strip345 ? 6 : 3;
        for (var c = 0; c < copies; c++)
        {
            for (var rank = minRank; rank <= Ranks.Two; rank++)
                foreach (var suit in suits)
                    deck.Add(new Card(rank, suit));
            deck.Add(Card.SmallJoker);
            deck.Add(Card.BigJoker);
        }
        return deck;
    }
}
