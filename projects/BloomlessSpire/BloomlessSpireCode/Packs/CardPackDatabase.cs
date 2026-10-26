using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using BloomlessSpire.Cards;

namespace BloomlessSpire.Packs;

// 一个「卡包」= 一组主题卡的集合。这是从一代 ThePackmaster 借鉴的核心概念。
public sealed class CardPack
{
    public required string Id { get; init; }
    public required Func<Player, CardModel>[] Cards { get; init; }

    public CardModel CreateRandomCard(Player owner)
    {
        var factory = Cards[Random.Shared.Next(Cards.Length)];
        return factory(owner);
    }
}

public static class CardPackDatabase
{
    // 战斗内生成的卡必须走 CombatState.CreateCard，
    // 否则打出时报 "must be added to a CombatState before playing it"。
    private static CardModel New<T>(Player owner) where T : CardModel
        => owner.Creature.CombatState!.CreateCard(ModelDb.Card<T>(), owner);

    public static readonly CardPack[] Packs =
    [
        // 侵略包：力量/姿态/多段伤害（一代 aggressionpack）
        new CardPack
        {
            Id = "aggression",
            Cards =
            [
                owner => New<Vindicate>(owner),
                owner => New<DarkLance>(owner),
                owner => New<Decapitate>(owner),
                owner => New<Epiphany>(owner),
                owner => New<FuriousAssault>(owner),
                owner => New<LunaticRage>(owner),
                owner => New<Slam>(owner),
                owner => New<TitansStrength>(owner),
                owner => New<Animosity>(owner)
            ]
        },
        // 打击包：围绕「打击」标签的联动（一代 strikepack）
        new CardPack
        {
            Id = "strike",
            Cards =
            [
                owner => New<Backstrike>(owner),
                owner => New<DifferentStrikes>(owner),
                owner => New<LightningStrike>(owner),
                owner => New<PenaltyStrike>(owner),
                owner => New<StrikeABargain>(owner),
                owner => New<StrikeDummyJr>(owner),
                owner => New<StrikeOfGenius>(owner),
                owner => New<StrikeOfLuck>(owner),
                owner => New<StrikeOfMidnight>(owner),
                owner => New<WorkersStrike>(owner)
            ]
        },
        // 实用包：辅助/诅咒/抽牌（一代 utilitypack）
        new CardPack
        {
            Id = "utility",
            Cards =
            [
                owner => New<Enrage>(owner),
                owner => New<Excitement>(owner),
                owner => New<GreaterHex>(owner),
                owner => New<LayeredDefenses>(owner),
                owner => New<LesserHex>(owner),
                owner => New<PreemptiveStrike>(owner),
                owner => New<RareStrike>(owner),
                owner => New<Replenish>(owner),
                owner => New<Whispers>(owner)
            ]
        },
        // 毒药包（早期测试用）
        new CardPack
        {
            Id = "poison",
            Cards = [owner => New<PoisonStab>(owner)]
        },
        // 爪击包：爪击牌联动 + 永久成长（一代 clawpack）
        new CardPack
        {
            Id = "claw",
            Cards =
            [
                owner => New<PMClaw>(owner),
                owner => New<GhostClaw>(owner),
                owner => New<CloakAndClaw>(owner),
                owner => New<TwinClaw>(owner),
                owner => New<SearingClaw>(owner),
                owner => New<ClawForOne>(owner),
                owner => New<Conclawd>(owner),
                owner => New<InfiniteClaws>(owner),
                owner => New<TalkToTheClaw>(owner),
                owner => New<MutateClaw>(owner)
            ]
        }
    ];

    // 本局实际可用的卡包（开局随机抽若干个）。撕包只从这里面开。
    private static CardPack[] _runPacks = [];

    // 一局抽几个包。一代是 7 个包位（核心 + 3 随机 + 3 选择），这里先简化成随机 3 个。
    private const int PacksPerRun = 3;

    public static void StartRunPacks()
    {
        _runPacks = Packs
            .OrderBy(_ => Random.Shared.Next())
            .Take(PacksPerRun)
            .ToArray();
    }

    public static IEnumerable<string> RunPackIds => _runPacks.Select(p => p.Id);

    public static CardPack RandomPack()
    {
        var pool = _runPacks.Length > 0 ? _runPacks : Packs;
        return pool[Random.Shared.Next(pool.Length)];
    }
}
