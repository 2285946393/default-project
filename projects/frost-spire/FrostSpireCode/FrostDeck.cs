using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace FrostSpire;

// ============================================================================
// 凛冬起始卡组（M3.8）
//
// ★ 用户原话：
//   「那本来的卡是不是没什么用？要不要直接把自己卡组全部删掉换成我们凛冬前线的卡组？
//     但是难度也得和凛冬前线对齐。」
//
// ★ 先回答"原版卡是不是没用"——**不是没用，是作用不同**：
//   · 短期内（前几波）原版 Strike 是**唯一能当场打死一只怪**的牌；
//     我们的牌是"投资"——箭塔 4 伤/回合、篝火 2 回血/回合，要 2~3 回合才回本。
//   · 长期（16 波连打）我们的牌是**复利**：建筑每回合自动输出，原版 Strike 每轮就 6 点。
//   ⇒ 所以「换成纯凛冬卡组」在**补上攻击类之前是不可能的**——
//     那时候全池 14 张只有 1 张攻击牌（暴风雪），纯凛冬卡组第 1 波都过不去。
//     M3.8 先补了 `FrostStrikeCard` / `FrostDefendCard`（对标本体 Strike/Defend 同费同数值），
//     现在这套卡组才成立。
//
// ★ 难度对齐的做法：**起始卡组的输出/防御量与本体起始卡组对齐**，波次表因此不用重调。
//   · 本体 Ironclad 起始 10 张：5×Strike(1费6伤) + 4×Defend(1费5格挡) + 1×Bash
//     ⇒ **攻击牌密度 50%**（5/10），这是"每回合至少能摸到一张打点"的保证。
//   · 凛冬起始 13 张：5×凛冬打击 + 3×凛冬壁垒 + 5 张功能牌
//     ⇒ **攻击牌密度 38%**（5/13），略低于本体，但用"建筑持续输出"补回来。
//   ⚠️ 第一版只放了 4 张打击（4/12 = 33%），开局会明显比本体弱 —— 已加到 5 张。
//   多出来的功能牌短期不产生伤害（是投资），所以**开局不会更强**、中期才开始滚雪球。
// ============================================================================

/// <summary>凛冬起始卡组 + 换装逻辑。</summary>
public static class FrostDeck
{
    /// <summary>
    /// 凛冬起始卡组（13 张）。顺序无关紧要（进牌库后会洗）。
    ///
    /// 构成（对标《凛冬前线》五大类）：
    ///   · 攻击 ×5：凛冬打击（1 费 6 伤，密度对齐本体 Strike）
    ///   · 防御 ×3：凛冬壁垒（1 费 5 格挡）
    ///   · 攻击/技能 ×1：暴风雪（2 费 AOE 8 + 虚弱）
    ///   · 防御/建筑 ×1：冰垒（前排 6 血替伤）
    ///   · 建筑 ×2：箭塔（4 AOE/回合）、篝火（0 费，2 回血/回合）
    ///   · 技能 ×1：冰封术（打断）
    ///
    /// 剩下 7 张（民兵 / 后排冰垒 / 符文壁 / 瞭望台 / 法师 / 霜爆塔 / 王座）
    /// 走**间隙补给三选一**进牌库 —— 那是构筑成长的部分（开了凛冬卡组后补给是 2 凛冬 + 1 角色牌）。
    /// </summary>
    public static async Task<bool> InstallAsync(RunState state, Player p)
    {
        try
        {
            // ① 清空原本的起始卡组
            var deck = p.Deck;
            var old = new List<CardModel>(deck.Cards);
            if (old.Count > 0)
                await CardPileCmd.RemoveFromDeck(old, showPreview: false);

            // ② 铺凛冬卡组
            var fresh = new List<CardModel>
            {
                state.CreateCard<Cards.FrostStrikeCard>(p),
                state.CreateCard<Cards.FrostStrikeCard>(p),
                state.CreateCard<Cards.FrostStrikeCard>(p),
                state.CreateCard<Cards.FrostStrikeCard>(p),
                state.CreateCard<Cards.FrostStrikeCard>(p),

                state.CreateCard<Cards.FrostDefendCard>(p),
                state.CreateCard<Cards.FrostDefendCard>(p),
                state.CreateCard<Cards.FrostDefendCard>(p),

                state.CreateCard<Cards.BlizzardCard>(p),
                state.CreateCard<Cards.IceBulwarkCard>(p),
                state.CreateCard<Cards.ArrowTowerCard>(p),
                state.CreateCard<Cards.CampfireCard>(p),
                state.CreateCard<Cards.FreezeCard>(p),
            };

            await CardPileCmd.Add(fresh, PileType.Deck, CardPilePosition.Bottom, null, skipVisuals: true);

            Entry.Logger.Info($"凛冬尖塔：起始卡组已换成凛冬卡组（清掉 {old.Count} 张，铺入 {fresh.Count} 张）。");
            return true;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"换凛冬起始卡组失败（保留原卡组，不影响本局）: {e}");
            return false;
        }
    }
}
