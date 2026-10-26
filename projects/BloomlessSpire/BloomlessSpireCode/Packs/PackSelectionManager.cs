using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using BloomlessSpire.Packs;

namespace BloomlessSpire.Packs;

// 卡包选择管理器：跟踪当前可用卡包、已选卡包、当前激活卡包。
public static class PackSelectionManager
{
    // 本局所有可选卡包（完整池）
    public static CardPack[] AllPacks { get; private set; } = [];

    // 本局已选的卡包（玩家选过的）
    public static List<CardPack> SelectedPacks { get; } = [];

    // 当前激活的卡包（下次 PackRip 从这里开）
    public static CardPack? ActivePack { get; private set; }

    // 初始选择是否已展示
    public static bool InitialSelectionShown { get; set; }

    // 卡包选择回调
    public static event Action<CardPack>? OnPackSelected;

    public static void Initialize(CardPack[] allPacks)
    {
        AllPacks = allPacks;
        SelectedPacks.Clear();
        ActivePack = null;
        InitialSelectionShown = false;
    }

    public static void SelectPack(CardPack pack)
    {
        ActivePack = pack;
        if (!SelectedPacks.Contains(pack))
            SelectedPacks.Add(pack);
        OnPackSelected?.Invoke(pack);
    }

    // 获取 N 个随机候选卡包（排除已选的）
    public static CardPack[] GetCandidatePacks(int count = 3)
    {
        return AllPacks
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Min(count, AllPacks.Length))
            .ToArray();
    }

    // PackRip 调用：从当前激活卡包开一张卡
    public static CardModel? RipFromActivePack(Player owner)
    {
        return ActivePack?.CreateRandomCard(owner);
    }
}
