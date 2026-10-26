"""校验任务文案是否齐全（枚举 / 目录 / 英文表 / C# 英文兜底 四方对齐）。

用法：在工程目录执行  python tools/check_i18n_coverage.py
加任务之后跑一次，能立刻看出漏了哪条文案。
"""
import json
import re
import sys

ENG = "QuestSpire/localization/eng/relics.json"
MODEL = "QuestSpireCode/Quests/QuestModel.cs"
CATALOG = "QuestSpireCode/Quests/QuestCatalog.cs"
TEXTEN = "QuestSpireCode/Quests/QuestTextEn.cs"


def main() -> int:
    eng = json.load(open(ENG, encoding="utf-8"))
    model = open(MODEL, encoding="utf-8").read()
    catalog = open(CATALOG, encoding="utf-8").read()
    texten = open(TEXTEN, encoding="utf-8").read()

    # 只取 QuestKind 枚举块（QuestModel 里还有别的枚举，不能整文件扫）
    block = re.search(r"public enum QuestKind\s*\{(.*?)\n\}", model, re.S)
    if not block:
        print("!! 找不到 QuestKind 枚举")
        return 1
    kinds = re.findall(r"^\s+([A-Za-z][A-Za-z0-9]*),\s*//", block.group(1), re.M)

    chunks = catalog.split("new(QuestKind.")[1:]
    catalog_kinds = []
    has_reward = {}
    for ch in chunks:
        m = re.match(r"([A-Za-z0-9]+)", ch)
        if not m:
            continue
        catalog_kinds.append(m.group(1))
        has_reward[m.group(1)] = "奖励：" in ch

    problems = []
    missing_title = [k for k in kinds if f"QUEST_SPIRE_QUEST_{k}.title" not in eng]
    missing_desc = [k for k in kinds if f"QUEST_SPIRE_QUEST_{k}.description" not in eng]
    missing_reward = [
        k for k in kinds
        if has_reward.get(k) and f"QUEST_SPIRE_QUEST_{k}.reward" not in eng
    ]
    not_in_catalog = [k for k in kinds if k not in catalog_kinds]
    not_in_enum = [k for k in catalog_kinds if k not in kinds]
    not_in_texten = [k for k in kinds if f"QuestKind.{k}," not in texten]

    print(f"QuestKind: {len(kinds)}  目录: {len(catalog_kinds)}  英文条目: {len(eng)}")
    for label, bad in (
        ("英文缺标题", missing_title),
        ("英文缺描述", missing_desc),
        ("带奖励标签但英文缺 reward", missing_reward),
        ("枚举里有、目录里没有", not_in_catalog),
        ("目录里有、枚举里没有", not_in_enum),
        ("QuestTextEn 里没有（跑 gen_questtexten.py）", not_in_texten),
    ):
        if bad:
            problems.append(f"{label}: {bad}")
            print(f"!! {label}: {bad}")

    if problems:
        print(f"\n共 {len(problems)} 类问题")
        return 1
    print("OK：文案齐全")
    return 0


if __name__ == "__main__":
    sys.exit(main())
