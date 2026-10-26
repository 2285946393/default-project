using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using QuestSpire.Relics;
using STS2RitsuLib.Patching.Models;

namespace QuestSpire.Patches;

/// <summary>
/// 「一张牌被**从整局卡组里删掉**」这件事，本体也没有正向钩子 —— 和加牌一样只能补一刀。
/// 给「清仓大甩卖」（本局删牌总数）提供事件源；不管牌是商店花钱删的、事件删的、
/// 还是卡牌效果删的，都算。
///
/// ⚠️ `CardPileCmd.RemoveFromDeck` 有两个重载（单张 / 一堆），参数类型数组**必须**写清楚，
/// 否则 Harmony 找不到方法。签名照反编译逐字核对：
/// `RemoveFromDeck(CardModel card, bool showPreview)`。
///
/// 架子同 <see cref="CardAddedToDeckPatch"/>：走 RitsuLib 的 `IPatchMethod`，
/// `IsCritical => false` —— 打不上只影响"清仓大甩卖"这一个任务，不连累别人。
/// </summary>
internal sealed class CardRemovedFromDeckPatch : IPatchMethod
{
    /// <summary>补丁的唯一 id（会进 RitsuLib 的补丁诊断）。</summary>
    public static string PatchId => "questspire_card_removed_from_deck";

    /// <summary>给人看的说明（日志/诊断里显示）。</summary>
    public static string Description => "牌被从整局卡组删掉 → 通知任务册（「清仓大甩卖」的事件源）";

    /// <summary>**失败不算严重**：只喂一个任务，绝不该连累另一个补丁。</summary>
    public static bool IsCritical => false;

    /// <summary>要补的目标：`CardPileCmd.RemoveFromDeck(CardModel, bool)` 这个重载。</summary>
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardPileCmd), nameof(CardPileCmd.RemoveFromDeck),
            [typeof(CardModel), typeof(bool)]),
    ];

    private static void Postfix(CardModel card)
    {
        if (card?.Owner == null) return;
        try
        {
            card.Owner.GetRelic<QuestBookRelic>()?.NotifyCardRemovedFromDeck(card);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"卡牌被删掉的通知失败: {e.Message}");
        }
    }
}
