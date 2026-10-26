using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using QuestSpire.Relics;
using STS2RitsuLib.Patching.Models;

namespace QuestSpire.Patches;

/// <summary>
/// **本体没有"卡牌加入卡组"的正向钩子**（`AbstractModel` 只有"阻止/修改加入"这类），
/// 所以这里用补丁补一刀 —— 用途是我们自己的：给「往卡组加牌」这类任务提供事件源。
///
/// 只补 `CardPileCmd.Add(CardModel, PileType, ...)` 这一个重载：
/// 把牌加进 `PileType.Deck`（整局卡组）时，通知**那张牌主人的**任务册。
///
/// ⚠️ 补丁签名是照反编译**逐字核对**的，别凭感觉改：
/// `Add(CardModel card, PileType newPileType, CardPilePosition position, AbstractModel? clonedBy, bool skipVisuals)`
///
/// ── 为什么必须有这一刀（2026-10-08 复核过）──────────────────────────────
/// 查过游戏的官方 API 文档（`sts2.xml`）：`Hook` 类一共 147 个成员，
/// 名字里带 `Deck` 的只有两个 —— `ShouldAddToDeck` 和 `ModifyCardBeingAddedToDeck`，
/// 都是**"加之前的闸门/改写"**，没有"加完之后通知我一下"。
/// 而 `Hook.AfterCardChangedPiles`（`AfterCardChangedPiles` 那个钩子）签名里带 `CombatState`,
/// 是**战斗牌堆之间**的搬运，盖不到"整局卡组"。
/// RitsuLib 的 `CardMovedBetweenPilesEvent` 正是挂在这个钩子上，同样盖不到；
/// 两个框架也都没有包装 `ShouldAddToDeck` / `ModifyCardBeingAddedToDeck`。
/// → **这一刀删不掉**，只能换个更靠谱的"架子"（见下）。
///
/// ── 架子 ────────────────────────────────────────────────────────────
/// 用 RitsuLib 的 `IPatchMethod`（不再自己 `new Harmony`）。原来手写"逐个补丁分开打"，
/// 理由是"`PatchAll` 一个失败整批失败" —— 读了 RitsuLib 源码发现**这个理由不成立**：
/// `ModPatcher.PatchAll()` 本身就是逐补丁记录结果（`ModPatchResult[]`），
/// 只有 `IsCritical == true` 的失败才 `UnpatchAll()` 回滚。
/// 所以标 `IsCritical => false` 就得到同样的隔离，外加拿到了补丁诊断和带 `PatchId` 的日志。
/// </summary>
internal sealed class CardAddedToDeckPatch : IPatchMethod
{
    /// <summary>补丁的唯一 id（会进 RitsuLib 的补丁诊断）。</summary>
    public static string PatchId => "questspire_card_added_to_deck";

    /// <summary>给人看的说明（日志/诊断里显示）。</summary>
    public static string Description => "牌被加进整局卡组 → 通知任务册（往卡组加牌类任务的事件源）";

    /// <summary>
    /// **失败不算严重**：它只喂「加 3 攻 / 3 技 / 3 能」那批任务。
    /// 打不上（游戏更新改了签名）时应该只影响那几个任务，**绝不能连累另一个补丁**。
    /// </summary>
    public static bool IsCritical => false;

    /// <summary>要补的目标：`CardPileCmd.Add` 的那个 5 参重载。</summary>
    public static ModPatchTarget[] GetTargets() =>
    [
        new(typeof(CardPileCmd), nameof(CardPileCmd.Add),
            [typeof(CardModel), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)]),
    ];

    private static void Postfix(CardModel card, PileType newPileType)
    {
        if (newPileType != PileType.Deck || card?.Owner == null) return;
        try
        {
            card.Owner.GetRelic<QuestBookRelic>()?.NotifyCardAddedToDeck(card);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"卡牌加入卡组的通知失败: {e.Message}");
        }
    }
}
