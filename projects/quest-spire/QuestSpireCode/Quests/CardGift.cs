using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace QuestSpire.Quests;

// 「凭空生成一张牌」这类效果的公共实现。
// 枯木树枝（我们自己的遗物）和「随身物」版的仿制枯枝都走这里，避免两份代码写歪。
public static class CardGift
{
    /// <summary>
    /// 奖励内容用的**确定性**种子（各端算出来必须一样）。
    ///
    /// ⚠️ 为什么不能用 `Random.Shared`（2026-10-08 修）：
    ///   联机里游戏的奖励是**按「奖励集编号 + 下标」在各端复现**的 ——
    ///   `RewardSelectedMessage` 只带 setId 和 rewardIndex，接收端拿 `set.Rewards[index]` 去应用。
    ///   所以「同一次奖励」在各端必须是**同一个列表**。`Random.Shared` 各端各抽各的 →
    ///   本人那台点了第 3 项，别人那台发的是自己的第 3 项（甚至下标越界抛异常）。
    ///   种子由「谁的任务 + 第几层 + 本册第几次抽」推出，各端完全一致。
    /// </summary>
    public static int SeedFor(Player owner, int seq)
    {
        int floor = 0;
        try { floor = owner.RunState?.TotalFloor ?? 0; } catch { /* 拿不到就算了，还有 NetId 兜着 */ }
        return unchecked((int)(owner.NetId ^ (uint)floor) ^ 0x5F3759DF ^ (seq * unchecked((int)0x9E3779B1)));
    }

    /// <summary>
    /// 确定性洗牌（Fisher–Yates + 固定种子）。各端结果完全一样。
    /// 升级型奖励（<c>UpgradeRandomCards</c>）也用它 —— 见那里的说明。
    /// </summary>
    public static List<CardModel> ShuffleDeterministic(IEnumerable<CardModel> src, int seed)
    {
        var list = src.ToList();
        var rng = new System.Random(seed);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    // 只从**本职业**已解锁的牌池里挑。跨职业的牌塞进手里会出问题，玩家也看不懂。
    private static List<CardModel> PoolOfCharacter(Player owner, bool onlyXCost)
    {
        return owner.Character.CardPool
            .GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Type is CardType.Attack or CardType.Skill or CardType.Power
                        && (!onlyXCost || c.EnergyCost?.CostsX == true))
            .ToList();
    }

    /// <summary>
    /// 随机一张本职业的牌加入手牌（战斗中）。返回是否真的塞进去了。
    ///
    /// ⚠️ 必须走 CardFactory.GetDistinctForCombat + CardPileCmd.AddGeneratedCardsToCombat，
    /// 让游戏把这牌登记成「战斗内生成的牌」。之前用 RunState.CreateCard + CardPileCmd.Add
    /// 造出来的牌游戏不认，**打出去会直接卡住**。
    /// </summary>
    public static async Task<bool> GiveRandomCardToHand(Player owner)
    {
        var hand = CardPile.Get(PileType.Hand, owner);
        if (hand == null || hand.Cards.Count >= CardPile.MaxCardsInHand) return false;

        var pool = PoolOfCharacter(owner, onlyXCost: false);
        if (pool.Count == 0) return false;

        var cards = CardFactory
            .GetDistinctForCombat(owner, pool, 1, owner.RunState.Rng.CombatCardGeneration)
            .ToList();
        if (cards.Count == 0) return false;

        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, owner, CardPilePosition.Bottom);
        return true;
    }

    /// <summary>
    /// **造**一张 X 费牌的卡牌奖励（三选一、不限职业），交给调用方塞进"这批任务奖励"里，
    /// 最后和金币 / 遗物一起**同一个界面**弹出来（《海克斯符文》的红包、蛋糕就是这么做的）。
    /// 找不到 X 费牌就返回 null（调用方自己决定折现多少）。
    /// </summary>
    public static Reward? BuildXCardReward(Player owner, int seed = 0)
    {
        var picks = ShuffleDeterministic(AllXCostCards(owner), seed)
            .Take(3)
            .ToList();
        if (picks.Count == 0) return null;

        var options = CardCreationOptions.ForNonCombatWithDefaultOdds(
            AllCardPools(), c => c.EnergyCost?.CostsX == true);

        // ⚠️ 必须转成**这名玩家名下的卡**，公共卡模型塞进奖励界面会点不中（0.2.4 玩家反馈）。
        var owned = picks.Select(m => owner.RunState.CreateCard(m, owner)).ToList();
        return new CardReward(owned, CardCreationSource.Other, owner, options);
    }

    /// <summary>
    /// 发一张 X 费牌的**卡牌奖励**（三选一，**不限职业**，玩家自己挑）。
    ///
    /// 为什么改成这样：玩家反馈「X 费牌我希望是以卡牌奖励出现，且可以无视职业限制」。
    /// 之前是直接 CreateCard 塞进卡组（玩家没得选），现在走游戏自己的奖励界面：
    /// `RewardsCmd.OfferCustom` + `CardReward`，玩家能看、能选、能跳过。
    ///
    /// ⚠️ CardCreationOptions 必须用 ForNonCombatWithDefaultOdds（来源 = CardCreationSource.Other），
    /// 否则会被当成战斗奖励、混进战斗结算那套流程里。
    /// </summary>
    public static async Task<string> OfferXCardReward(Player owner, int seed = 0)
    {
        var picks = ShuffleDeterministic(AllXCostCards(owner), seed)
            .Take(3)
            .ToList();

        if (picks.Count == 0)
        {
            await PlayerCmd.GainGold(150, owner);
            return "找不到 X 费牌，折现 150 金币";
        }

        var options = CardCreationOptions.ForNonCombatWithDefaultOdds(
            AllCardPools(), c => c.EnergyCost?.CostsX == true);

        try
        {
            // ⚠️ 必须先把「公共卡模型」变成**这名玩家名下的卡**再塞进奖励界面。
            // 直接塞公共模型，界面能弹出来但牌选不中（2026-09-25 玩家反馈「无法选择牌铺」）。
            // 《海克斯符文》也是先 player.RunState.CreateCard(...) 再放进 CardReward。
            var owned = picks.Select(m => owner.RunState.CreateCard(m, owner)).ToList();
            var reward = new CardReward(owned, CardCreationSource.Other, owner, options);
            await RewardsCmd.OfferCustom(owner, new List<Reward> { reward });
            return "发了一张 X 费牌奖励（三选一，不限职业）";
        }
        catch (System.Exception e)
        {
            // ⚠️ 任务完成是在**战斗中途**触发的，而奖励界面一般出现在战斗结束后。
            // 万一游戏这时候不接受弹奖励，绝不能静默失败——退回「直接塞一张进卡组」。
            Entry.Logger.Error($"OfferXCardReward 失败，退回直接发牌: {e}");
            var card = owner.RunState.CreateCard(picks[0], owner);
            var result = await CardPileCmd.Add(card, PileType.Deck);
            CardCmd.PreviewCardPileAdd(result, 1.6f);
            // 多人局：加牌也要通知其他客户端（游戏自己的水晶球诅咒就是这么做）
            try { RunManager.Instance.RewardSynchronizer?.SyncLocalObtainedCard(card); }
            catch (System.Exception ex) { Entry.Logger.Warn($"SyncLocalObtainedCard 失败: {ex.Message}"); }
            return "一张 X 费牌已加入卡组（奖励界面没弹出来，直接给你了）";
        }
    }

    /// <summary>
    /// 造一张**筛过的**卡牌奖励（默认三选一、**不限职业**），交给调用方塞进"这批任务奖励"里，
    /// 最后和金币/遗物一起同一个界面弹出来。找不到就返回 null（调用方折现）。
    ///
    /// ⚠️ 和 <see cref="BuildXCardReward"/> 同一套写法：必须把公共卡模型转成**这名玩家名下的卡**
    /// （`RunState.CreateCard`），否则奖励界面能弹出来但牌**点不中**（0.2.4/0.2.25 玩家反馈）。
    /// </summary>
    public static Reward? BuildCardReward(Player owner, Func<CardModel, bool> filter, int count = 3, int seed = 0)
    {
        var picks = ShuffleDeterministic(AllPlayableCards(owner).Where(filter), seed)
            .Take(count)
            .ToList();
        if (picks.Count == 0) return null;

        var options = CardCreationOptions.ForNonCombatWithDefaultOdds(AllCardPools(), filter);
        var owned = picks.Select(m => owner.RunState.CreateCard(m, owner)).ToList();
        return new CardReward(owned, CardCreationSource.Other, owner, options);
    }

    // 所有职业池里的**可打出的**牌（限攻击 / 技能 / 能力，挡掉诅咒和衍生物）。
    // 「不限职业」是玩家明确要的：遍历**所有**卡池，不是只看自己职业那个。
    public static List<CardModel> AllPlayableCards(Player owner)
    {
        var result = new List<CardModel>();
        var seen = new HashSet<string>();

        foreach (var model in ModelDb.All)
        {
            if (model is not CardPoolModel pool) continue;

            IEnumerable<CardModel> cards;
            try
            {
                cards = pool.GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint);
            }
            catch
            {
                continue; // 某些池子取不到（未解锁 / 多人限制），跳过就好
            }

            foreach (var card in cards)
            {
                if (card.Type is not (CardType.Attack or CardType.Skill or CardType.Power)) continue;
                if (seen.Add(card.Id.Entry)) result.Add(card);
            }
        }

        return result;
    }

    // 所有职业池里的 X 费牌（攻击 / 技能 / 能力）。
    private static List<CardModel> AllXCostCards(Player owner)
        => AllPlayableCards(owner).Where(c => c.EnergyCost?.CostsX == true).ToList();

    private static List<CardPoolModel> AllCardPools()
    {
        var list = new List<CardPoolModel>();
        foreach (var model in ModelDb.All)
            if (model is CardPoolModel p) list.Add(p);
        return list;
    }
}
