"""从 localization/eng/relics.json 生成 QuestSpireCode/Quests/QuestTextEn.cs

改英文文案后跑一次：  python tools/gen_questtexten.py
"""
import json, os, re, io

PROJ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
eng = json.load(open(os.path.join(PROJ, "QuestSpire", "localization", "eng", "relics.json"), encoding="utf-8"))
model = open(os.path.join(PROJ, "QuestSpireCode", "Quests", "QuestModel.cs"), encoding="utf-8").read()
kinds = re.findall(r"^\s+([A-Za-z][A-Za-z0-9]*),\s*//", model, re.M)

BS = chr(92)
DQ = chr(34)


def lit(s):
    return DQ + s.replace(BS, BS + BS).replace(DQ, BS + DQ) + DQ


titles, descs, rewards = [], [], []
for k in kinds:
    for key, arr in ((f"QUEST_SPIRE_QUEST_{k}.title", titles),
                     (f"QUEST_SPIRE_QUEST_{k}.description", descs),
                     (f"QUEST_SPIRE_QUEST_{k}.reward", rewards)):
        if key in eng:
            arr.append("        { QuestKind.%s, %s }," % (k, lit(eng[key])))

head = (
    "using System.Collections.Generic;\n\n"
    "namespace QuestSpire.Quests;\n\n"
    "/// <summary>\n"
    "/// 任务文案的**英文表**（由 tools/gen_questtexten.py 从 localization/eng/relics.json 生成）。\n"
    "/// 为什么塞进 C#：玩家可以**强制英文**——即使游戏是中文也要能显示英文，\n"
    "/// 而 `LocString` 只按当前游戏语言取表、取不到别的语言。所以自带一份，任意组合都能用。\n"
    "/// ⚠️ 改英文文案：先改 eng/relics.json，再跑 tools/gen_questtexten.py。\n"
    "/// </summary>\n"
    "internal static class QuestTextEn\n{\n"
    '    public const string RewardPrefix = "Reward: ";\n'
    '    public const string GoldFormat = "Gold +{0}";\n'
    '    public const string Completed = "Completed";\n\n'
)

buf = io.StringIO()
buf.write(head)
for name, arr in (("Titles", titles), ("Descriptions", descs), ("Rewards", rewards)):
    buf.write("    private static readonly Dictionary<QuestKind, string> %s = new()\n    {\n" % name)
    buf.write("\n".join(arr))
    buf.write("\n    };\n\n")
buf.write("    public static string? Title(QuestKind k) => Titles.GetValueOrDefault(k);\n")
buf.write("    public static string? Description(QuestKind k) => Descriptions.GetValueOrDefault(k);\n")
buf.write("    public static string? Reward(QuestKind k) => Rewards.GetValueOrDefault(k);\n}\n")

open(os.path.join(PROJ, "QuestSpireCode", "Quests", "QuestTextEn.cs"), "w", encoding="utf-8").write(buf.getvalue())
print("written: titles=%d descs=%d rewards=%d" % (len(titles), len(descs), len(rewards)))
