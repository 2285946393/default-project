using MegaCrit.Sts2.Core.Localization;

namespace QuestSpire.Quests;

/// <summary>
/// 任务文案的取词口。优先级：**设置页里的语言** →
///   强制中文 = `QuestCatalog` 里硬编码的中文；
///   强制英文 = <see cref="QuestTextEn"/>（生成进 C# 的英文表，不依赖 pck → 中文游戏也能显示英文）；
///   跟随游戏 = mod 自带本地化表（`res://QuestSpire/localization/&lt;语言&gt;/relics.json`），查不到再用中文兜底。
/// </summary>
public static class QuestText
{
    private const string Table = "relics";

    public static string Title(QuestDef d)
        => Choose(d.Title, QuestTextEn.Title(d.Kind) ?? d.Title, $"QUEST_SPIRE_QUEST_{d.Kind}.title");

    public static string Description(QuestDef d)
        => Choose(d.Description, QuestTextEn.Description(d.Kind) ?? d.Description, $"QUEST_SPIRE_QUEST_{d.Kind}.description");

    public static string Reward(QuestDef d)
    {
        if (!string.IsNullOrWhiteSpace(d.RewardLabel))
            // ⚠️ QuestCatalog 里的 RewardLabel 自带「奖励：」前缀，面板还会再加一次 → 这里只返回**内容**。
            return StripPrefix(Choose(d.RewardLabel, QuestTextEn.Reward(d.Kind) ?? d.RewardLabel,
                $"QUEST_SPIRE_QUEST_{d.Kind}.reward"));

        if (d.Reward != RewardKind.Gold) return KindLabel(d.Reward);
        return Gold(d.RewardGold);
    }

    /// <summary>非金币奖励类型的中文/英文说明。</summary>
    private static string KindLabel(RewardKind kind) => QuestSpireConfig.Language == QuestSpireConfig.TextLanguage.English
        ? kind switch
        {
            RewardKind.Relic => "a random relic",
            RewardKind.CardAny => "a card (pick 1 of 3)",
            RewardKind.CardSkill => "a Skill card (pick 1 of 3)",
            RewardKind.CardPower => "a Power card (pick 1 of 3)",
            RewardKind.CardRare => "a Rare card (pick 1 of 3)",
            RewardKind.CardOtherClass => "a card from another character (pick 1 of 3)",
            RewardKind.Upgrade => "upgrade 2 random cards",
            RewardKind.Transform => "transform a card",
            RewardKind.CardNamed => "a key card",
            RewardKind.Enchant => "enchant a card",
            RewardKind.RemoveCard => "remove a card from your deck",
            _ => "Gold",
        }
        : kind switch
        {
            RewardKind.Relic => "一件随机遗物",
            RewardKind.CardAny => "一张牌（三选一）",
            RewardKind.CardSkill => "一张技能牌（三选一）",
            RewardKind.CardPower => "一张能力牌（三选一）",
            RewardKind.CardRare => "一张稀有牌（三选一）",
            RewardKind.CardOtherClass => "一张其他职业的牌（三选一）",
            RewardKind.Upgrade => "随机升级 2 张牌",
            RewardKind.Transform => "把一张牌变成另一张",
            RewardKind.CardNamed => "一张核心牌",
            RewardKind.Enchant => "给一张牌附魔",
            RewardKind.RemoveCard => "从牌组里删掉一张牌",
            _ => "金币",
        };

    /// <summary>面板里「奖励：」这个前缀。</summary>
    public static string RewardPrefix
        => Choose("奖励：", QuestTextEn.RewardPrefix, "QUEST_SPIRE_QUEST_REWARD_PREFIX");

    /// <summary>面板里「已完成」这个状态。</summary>
    public static string Completed
        => Choose("已完成", QuestTextEn.Completed, "QUEST_SPIRE_QUEST_STATUS_COMPLETED");

    private static string Choose(string cn, string en, string locKey)
    {
        switch (QuestSpireConfig.Language)
        {
            case QuestSpireConfig.TextLanguage.Chinese: return cn;
            case QuestSpireConfig.TextLanguage.English: return en;
            // 跟随游戏：**按游戏语言直接选**，不去查本地化表。
            //
            // 2026-10-04 玩家反馈"中文游戏里任务界面是英文"，根因就在这：
            // 我们只给 zhs/relics.json 写了遗物那 21 条，任务文案的 key 一个都没写；
            // 而游戏的本地化系统在当前语言查不到 key 时会**回退到英文表**，
            // 于是 LocString.Exists 返回 true，拿回来的是 eng 表里那份英文。
            // 中文文案本来就在 QuestCatalog 里硬编码着（cn 参数），直接用就行。
            default: return QuestSpireConfig.GameIsEnglish() ? en : cn;
        }
    }

    private static string Gold(int amount)
    {
        switch (QuestSpireConfig.Language)
        {
            case QuestSpireConfig.TextLanguage.Chinese:
                return $"金币 +{amount}";
            case QuestSpireConfig.TextLanguage.English:
                return string.Format(QuestTextEn.GoldFormat, amount);
            default:
                // 同 Choose：跟随时按游戏语言选，别查表（会回退到英文）
                return QuestSpireConfig.GameIsEnglish()
                    ? string.Format(QuestTextEn.GoldFormat, amount)
                    : $"金币 +{amount}";
        }
    }

    private static string Resolve(string key, string fallback)
    {
        try
        {
            if (LocString.Exists(Table, key))
                return new LocString(Table, key).GetFormattedText();
        }
        catch (System.Exception) { }
        return fallback;
    }

    private static string StripPrefix(string s)
    {
        foreach (var p in new[] { "奖励：", "奖励:" })
            if (s.StartsWith(p, System.StringComparison.Ordinal))
                return s.Substring(p.Length).TrimStart();
        return s;
    }
}
