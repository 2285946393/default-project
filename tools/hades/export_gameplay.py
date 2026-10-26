"""导出 Hades 1 / 2 的玩法机制数据：房间、遭遇战、敌人、成长曲线。

用法：
    python export_gameplay.py
"""

import csv
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")

from lua_table import load_file, flatten

H1 = r"D:/software/steam/steamapps/common/Hades/Content/Scripts"
H2 = r"D:/software/steam/steamapps/common/Hades II/Content/Scripts"
OUT = r"D:/work/AI产出/Hades反编译"


def dump_json(obj, name):
    path = os.path.join(OUT, name)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(obj, f, ensure_ascii=False, indent=1)
    return path


def write_csv(rows, path, cols=None):
    if not rows:
        return 0
    if cols is None:
        cols = ["条目"]
        seen = {"条目"}
        for r in rows:
            for k in r:
                if k not in seen:
                    seen.add(k)
                    cols.append(k)
    with open(path, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=cols, extrasaction="ignore")
        w.writeheader()
        for r in rows:
            w.writerow(r)
    return len(rows)


def export_encounters(base, gen):
    """遭遇战配置 + 每层的房间序列。"""
    out = {}

    enc = load_file(os.path.join(base, "EncounterData.lua"))
    ed = enc.get("EncounterData", {})
    if isinstance(ed, dict):
        out["EncounterData"] = ed

    # 难度相关全局参数
    for k in ("MinimumDifficulty", "WaveDifficultyPatterns", "BaseWaveOverrideValues",
              "ElysiumWaveOverrideValues", "IntroWaveOverrideValues"):
        if k in enc:
            out[k] = enc[k]

    sets = load_file(os.path.join(base, "EncounterSets.lua"))
    es = sets.get("EncounterSets", {})
    if isinstance(es, dict):
        out["EncounterSets"] = es

    if not out:
        return 0

    dump_json(out, f"{gen}-遭遇战与难度.json")

    # 房间序列导出成表
    rows = []
    for biome, seq in (es or {}).items():
        if not isinstance(seq, list):
            continue
        for idx, room in enumerate(seq, 1):
            rows.append({"层": biome, "序号": idx, "房间类型": room})
    if rows:
        write_csv(rows, os.path.join(OUT, f"{gen}-房间序列.csv"), ["层", "序号", "房间类型"])
    return len(ed) if isinstance(ed, dict) else 0


def export_enemies(base, gen):
    """敌人配置。"""
    data = load_file(os.path.join(base, "EnemyData.lua"))
    # 敌人在 Enemies 键下；EnemyData 只是全局参数
    ed = data.get("Enemies") or data.get("EnemyData") or {}
    if not isinstance(ed, dict) or not ed:
        return 0

    dump_json(ed, f"{gen}-敌人全量.json")

    rows = []
    for name, body in ed.items():
        if not isinstance(body, dict):
            continue
        flat = flatten(body)
        row = {"敌人": name}
        for key in ("MaxHealth", "Health", "Damage", "Speed", "DisplayName",
                    "IsBoss", "IsMiniboss", "IsElite", "ThreatLevel"):
            if key in flat:
                row[key] = flat[key]
        # 生命值可能藏在嵌套里
        for k, v in flat.items():
            if "Health" in k and k not in row and isinstance(v, (int, float)):
                row[k] = v
                break
        rows.append(row)

    return write_csv(rows, os.path.join(OUT, f"{gen}-敌人数值.csv"))


def export_meta(base, gen):
    """永久成长（镜子/天赋）—— 难度曲线的另一半。"""
    data = load_file(os.path.join(base, "MetaUpgradeData.lua"))
    md = data.get("MetaUpgradeData", {})
    if not isinstance(md, dict) or not md:
        return 0

    dump_json(md, f"{gen}-永久成长.json")

    rows = []
    for name, body in md.items():
        if not isinstance(body, dict):
            continue
        flat = flatten(body)
        row = {"升级": name}
        for k, v in flat.items():
            if isinstance(v, (int, float)) and flat.get(k) is not None:
                row[k] = v
        rows.append(row)

    return write_csv(rows, os.path.join(OUT, f"{gen}-永久成长.csv"))


def export_rooms(base, gen):
    """房间定义。"""
    total = 0
    merged = {}
    for fn in ("RoomData.lua", "RoomDataTartarus.lua", "RoomDataAsphodel.lua",
               "RoomDataElysium.lua", "RoomDataStyx.lua", "RoomDataSurface.lua",
               "RoomDataSecrets.lua"):
        p = os.path.join(base, fn)
        if not os.path.exists(p):
            continue
        try:
            d = load_file(p)
        except Exception:
            continue
        for key in ("RoomData", "RoomSetData", "Base"):
            v = d.get(key)
            if isinstance(v, dict):
                merged.update(v)
        total += 1

    if not merged:
        return 0

    dump_json(merged, f"{gen}-房间定义.json")
    return len(merged)


def main():
    os.makedirs(OUT, exist_ok=True)

    for gen, base in [("Hades1", H1), ("Hades2", H2)]:
        print(f"\n=== {gen} ===")
        if not os.path.isdir(base):
            print("  目录不存在，跳过")
            continue

        n = export_encounters(base, gen)
        print(f"  遭遇战配置   {n} 种")

        n = export_enemies(base, gen)
        print(f"  敌人         {n} 个")

        n = export_meta(base, gen)
        print(f"  永久成长     {n} 项")

        n = export_rooms(base, gen)
        print(f"  房间定义     {n} 个")

    print(f"\n输出目录：{OUT}")


if __name__ == "__main__":
    main()
