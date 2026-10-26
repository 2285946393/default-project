using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Models;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace QuestSpire;

// ============================================================================
// 让「纯 dll 的 mod」也能给自己造的遗物起中文名。
//
// 背景：游戏读 mod 本地化表的路径是写死的 ——
//     ModManager:  $"res://{manifest.id}/localization/{language}/{file}"
// 而 res://<ModId>/ 只有**打了 pck** 才存在。本 mod 是纯 dll，所以正常的本地化走不通，
// 自己造的遗物在界面上只会显示一串内部 key（QUEST_SPIRE_RELIC_XXX.title）。
//
// 官方留的另一条路（LocManager.LoadTablesFromPath / TryLoadOverrideFile）：
//     user://localization_override/<语言>/<表名>.json      →  %APPDATA%\SlayTheSpire2\localization_override\...
// 游戏启动时会把这里的 JSON **合并**进对应语言的本地化表，而且只校验 smart-format 格式串、
// **不要求 key 已经存在**。而 mod 初始化发生在本地化加载之前，所以这里写完**当次启动就生效**。
//
// 注意：
//  - 只写我们自己的 key，并且**先读再合并**，绝不覆盖玩家/译者自己放的覆盖文件。
//  - 已存在的文件如果解析不了，就整个跳过（宁可没名字，也不能把别人的东西弄坏）。
//  - 文本里不要出现裸的 { }，会被 LocValidator 当成 smart-format 占位符而报错。
// ============================================================================
public static class LocInjector
{
    private static readonly string[] Languages = ["zhs", "eng"];

    public static void Inject(Logger logger)
    {
        try
        {
            // 现在 mod 带 pck 了，pck 里就有正规的本地化表（res://QuestSpire/localization/<语言>/relics.json）。
            // 那条路才是正路 —— 检测到它就不再往 AppData 写东西。
            // 下面的注入是给「没有 pck 的构建」留的退路（比如临时把 has_pck 关掉调试时）。
            if (HasPackedLocTable())
            {
                logger.Info("LocInjector: 检测到 pck 里的本地化表，跳过 AppData 注入。");
                return;
            }

            Write("zhs", ChineseTexts(), logger);
            Write("eng", EnglishTexts(), logger);
        }
        catch (System.Exception e)
        {
            // 这里绝不能把游戏搞崩：拿不到名字顶多是显示内部 key
            logger.Warn($"LocInjector failed (不影响游戏，只是少了文本): {e}");
        }
    }

    private static bool HasPackedLocTable()
    {
        try
        {
            return Godot.FileAccess.FileExists($"res://{Entry.ModId}/localization/zhs/relics.json");
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    // 运行期问游戏「这个遗物的 Entry 到底是什么」，别硬编码命名规则（万一规则变了就废）
    private static string EntryOf(System.Type relicType, string fallback)
    {
        try
        {
            return ModelDb.GetId(relicType).Entry;
        }
        catch (System.Exception)
        {
            return fallback;
        }
    }

    // ⚠️ 踩过的坑：在 `Initialize()` 里问 `ModelDb.GetId()`，拿到的是**还没加 mod 前缀**的中间形态
    //    （实测拿到 `QUEST_BOOK_RELIC`，而日志里注册完的最终 ID 是 `QUEST_SPIRE_RELIC_QUEST_BOOK_RELIC`）。
    //    本地化加载发生在内容注册**之后**（所以来得及），但注册前问不到最终 ID。
    //    解法：把两种形态的 key 都写进去 —— 多写几个用不上的 key 完全无害。
    private static IEnumerable<string> CandidateEntries(System.Type relicType)
    {
        var seen = new HashSet<string>();

        string runtime = EntryOf(relicType, "");
        if (!string.IsNullOrEmpty(runtime) && seen.Add(runtime))
            yield return runtime;

        string className = ToScreamingSnake(relicType.Name);
        string prefixed = $"{ToScreamingSnake(Entry.ModId)}_RELIC_{className}";
        if (seen.Add(prefixed)) yield return prefixed;

        if (seen.Add(className)) yield return className;
    }

    // QuestBookRelic → QUEST_BOOK_RELIC；QuestSpire → QUEST_SPIRE
    private static string ToScreamingSnake(string name)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c) && i > 0 && name[i - 1] != '_') sb.Append('_');
            sb.Append(char.ToUpperInvariant(c));
        }
        return sb.ToString();
    }

    private static Dictionary<string, string> ChineseTexts()
    {
        var map = new Dictionary<string, string>();
        AddRelic(map, typeof(Relics.QuestBookRelic),
            "尖塔任务册", "整局跟着你的任务册。右上角的面板显示任务进度；完成任务会发放奖励。",
            "任务什么的，最有趣了。");
        AddRelic(map, typeof(Relics.QuestDeadBranchRelic),
            "枯木树枝", "每当你消耗一张牌，将一张随机牌加入手牌。",
            "来自被遗忘年代的树枝。");
        AddRelic(map, typeof(Relics.QuestToriiRelic),
            "鸟居", "每场战斗开始时，获得 8 点格挡。",
            "小小的门，挡得住风，也挡得住痛。");
        AddRelic(map, typeof(Relics.QuestDripperRelic),
            "咖啡滤杯", "每场战斗的第一个回合，额外获得 1 点能量。",
            "再一杯，继续干活。");
        AddRelic(map, typeof(Relics.QuestOrnithopterRelic),
            "玩具扑翼飞机", "每场战斗开始时，回复 4 点生命。",
            "这只小玩具是独行者的好伙伴。");
        return map;
    }

    private static Dictionary<string, string> EnglishTexts()
    {
        var map = new Dictionary<string, string>();
        AddRelic(map, typeof(Relics.QuestBookRelic),
            "Quest Book", "Follows you for the whole run. The panel shows quest progress; completing quests grants rewards.",
            "Quests are fun.");
        AddRelic(map, typeof(Relics.QuestDeadBranchRelic),
            "Dead Branch", "Whenever you Exhaust a card, add a random card to your hand.",
            "The branch of a tree from a forgotten era.");
        AddRelic(map, typeof(Relics.QuestToriiRelic),
            "Torii", "At the start of each combat, gain 8 Block.",
            "A small gate that keeps the wind out.");
        AddRelic(map, typeof(Relics.QuestDripperRelic),
            "Coffee Dripper", "Gain 1 additional Energy on the first turn of each combat.",
            "One more cup. Back to work.");
        AddRelic(map, typeof(Relics.QuestOrnithopterRelic),
            "Toy Ornithopter", "At the start of each combat, heal 4 HP.",
            "The perfect companion for the lone climber.");
        return map;
    }

    private static void AddRelic(
        Dictionary<string, string> map, System.Type relicType,
        string title, string description, string flavor)
    {
        foreach (string entry in CandidateEntries(relicType))
        {
            map[$"{entry}.title"] = title;
            map[$"{entry}.description"] = description;
            map[$"{entry}.flavor"] = flavor;
        }
    }

    private static void Write(string language, Dictionary<string, string> ours, Logger logger)
    {
        string root = ProjectSettings.GlobalizePath("user://localization_override");
        string dir = System.IO.Path.Combine(root, language);
        string file = System.IO.Path.Combine(dir, "relics.json");

        if (!DirAccess.DirExistsAbsolute(dir))
        {
            Error err = DirAccess.MakeDirRecursiveAbsolute(dir);
            if (err != Error.Ok)
            {
                logger.Warn($"LocInjector: 建不了目录 {dir}（{err}）");
                return;
            }
        }

        var merged = new Dictionary<string, string>();
        if (Godot.FileAccess.FileExists(file))
        {
            try
            {
                using var read = Godot.FileAccess.Open(file, Godot.FileAccess.ModeFlags.Read);
                var existing = read == null
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(read.GetAsText());
                if (existing != null)
                    foreach (var kv in existing)
                        merged[kv.Key] = kv.Value;
            }
            catch (System.Exception e)
            {
                logger.Warn($"LocInjector: {file} 解析不了，整个跳过（不碰玩家自己的文件）：{e.Message}");
                return;
            }
        }

        foreach (var kv in ours) merged[kv.Key] = kv.Value;

        using var write = Godot.FileAccess.Open(file, Godot.FileAccess.ModeFlags.Write);
        if (write == null)
        {
            logger.Warn($"LocInjector: 写不进 {file}");
            return;
        }
        write.StoreString(JsonSerializer.Serialize(merged, new JsonSerializerOptions { WriteIndented = true }));
        logger.Info($"LocInjector: 注入 {merged.Count} 条文本到 {file}");
    }
}
