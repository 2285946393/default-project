using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace BloomlessSpire.Capabilities;

// 「牌技」效果：下一张打出的牌回到抽牌堆（一代的 Rebound）。
// 打出牌技时把手牌都挂上这个组件，其中第一张被打出的牌会改落到抽牌堆。
[RegisterModelCapability]
public sealed class ReboundCapability : CardCapability, ICardPlayResultContributor
{
    private static bool _armed;
    private static bool _used;

    public static void Arm()
    {
        _armed = true;
        _used = false;
    }

    public PileType? GetResultPileTypeForCardPlay(CardModel card)
    {
        if (!_armed || _used)
            return null;

        _used = true;
        _armed = false;
        return PileType.Draw;
    }
}
