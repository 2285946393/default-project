"""扫描 Hades 1 / 2 的 sjson 资源与对话文本，输出统计与导出。

用法：
    python export_sjson.py            # 全量
    python export_sjson.py --dialogue # 只导对话
"""

import csv
import glob
import os
import sys
from collections import Counter

sys.stdout.reconfigure(encoding="utf-8")

from sjson import parse_dialogue, load_sjson

H1 = r"D:/software/steam/steamapps/common/Hades/Content/Game"
H2 = r"D:/software/steam/steamapps/common/Hades II/Content/Game"
OUT = r"D:/work/AI产出/Hades反编译"


# ---------- 对话导出 ----------
def export_dialogue(base, gen):
    """只导出真正的对话（NPCData / DeathLoopData 这类），
    界面帮助、音频提示、角色台词表分开存。"""
    lang_dir = os.path.join(base, "Text", "zh-CN")
    if not os.path.isdir(lang_dir):
        return 0, 0

    DIALOGUE_PREFIX = ("_NPCData", "_DeathLoopData", "TextLineSets")
    OTHER_PREFIX = ("HelpText", "TraitText", "ScreenText", "ShellText",
                    "CodexText", "MiscText", "PatchNotes", "SongLyrics")

    dialogue, other = [], []
    files = sorted(glob.glob(os.path.join(lang_dir, "*.sjson")))
    en_dir = os.path.join(base, "Text", "en")
    for f in files:
        base_name = os.path.basename(f)
        en_file = os.path.join(en_dir, base_name.replace(".zh-CN.", ".en."))
        try:
            rows = parse_dialogue(f, en_file if os.path.exists(en_file) else None)
        except Exception as e:
            print(f"  [跳过] {os.path.basename(f)}: {e}")
            continue
        if not rows:
            continue
        src = os.path.basename(f).replace(".zh-CN.sjson", "")
        bucket = dialogue if src.startswith(DIALOGUE_PREFIX) else other
        for r in rows:
            r["来源"] = src
            bucket.append(r)

    def dump(rows, suffix):
        if not rows:
            return 0
        path = os.path.join(OUT, f"{gen}-{suffix}.csv")
        with open(path, "w", encoding="utf-8-sig", newline="") as fh:
            w = csv.DictWriter(
                fh,
                fieldnames=["来源", "Id", "Speaker", "Event", "Text", "English"],
                extrasaction="ignore",
            )
            w.writeheader()
            for r in rows:
                w.writerow(r)
        return len(rows)

    n1 = dump(dialogue, "对话中英对照")
    n2 = dump(other, "界面文本中英对照")
    return n1, len(files), n2


# ---------- 资源统计 ----------
def scan_resources(base, gen):
    """统计各类 sjson 资源的条目数。"""
    result = []
    for f in sorted(glob.glob(os.path.join(base, "**", "*.sjson"), recursive=True)):
        rel = os.path.relpath(f, base)
        if rel.startswith("Text"):
            continue
        try:
            data = load_sjson(f)
        except Exception:
            continue
        if not isinstance(data, dict):
            continue
        for k, v in data.items():
            if isinstance(v, list) and v:
                result.append((rel, k, len(v)))
    return result


def main():
    only_dialogue = "--dialogue" in sys.argv
    os.makedirs(OUT, exist_ok=True)

    for gen, base in [("Hades1", H1), ("Hades2", H2)]:
        n, nf, n2 = export_dialogue(base, gen)
        print(f"{gen}：对话 {n} 条 / 界面文本 {n2} 条（扫描 {nf} 个文件）")

    if only_dialogue:
        return

    print("\n资源扫描：")
    rows = []
    for gen, base in [("Hades1", H1), ("Hades2", H2)]:
        res = scan_resources(base, gen)
        print(f"\n  === {gen} ===")
        by_file = Counter()
        for rel, key, cnt in res:
            by_file[rel] += cnt
        for rel, cnt in by_file.most_common(18):
            print(f"    {rel:44} {cnt:5}")
            rows.append((gen, rel, cnt))

    idx = os.path.join(OUT, "资源清单.csv")
    with open(idx, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f)
        w.writerow(["游戏", "文件", "条目数"])
        w.writerows(rows)
    print(f"\n资源清单 -> {idx}")


if __name__ == "__main__":
    main()
