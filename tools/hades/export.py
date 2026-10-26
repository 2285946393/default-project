"""把 Hades 1 / 2 的 Lua 数据表导成 CSV + HTML 总览。

用法：
    python export.py              # 全量导出
    python export.py --only trait # 只导祝福
"""

import csv
import glob
import os
import sys
import html
import json

sys.stdout.reconfigure(encoding="utf-8")

from lua_table import load_file, flatten

H1 = r"D:/software/steam/steamapps/common/Hades/Content/Scripts"
H2 = r"D:/software/steam/steamapps/common/Hades II/Content/Scripts"
OUT = r"D:/work/AI产出/Hades反编译"

# 要导的表：显示名 -> (文件名模式, 顶层表名)
TABLES = {
    "祝福 TraitData": ("TraitData*.lua", "TraitData"),
    "武器 WeaponData": ("WeaponData*.lua", "WeaponData"),
    "敌人 EnemyData": ("EnemyData*.lua", "EnemyData"),
    "掉落 LootData": ("LootData*.lua", "LootData"),
    "主角 HeroData": ("HeroData*.lua", "HeroData"),
    "房间 RoomData": ("RoomData*.lua", "RoomData"),
    "升级 UpgradeData": ("UpgradeData*.lua", "UpgradeData"),
    "文本 TextData": ("TextData*.lua", "TextData"),
}

# 各表里值得单独成列的核心字段
KEY_COLS = {
    "TraitData": ["God", "Slot", "Icon", "IsDuoBoon", "InheritFrom", "RarityLevels"],
    "WeaponData": ["Icon", "Slot", "WeaponName", "BaseDamage", "Cooldown"],
    "EnemyData": ["MaxHealth", "Damage", "Icon", "DisplayName", "Speed"],
    "LootData": ["Icon", "DisplayName", "Cost", "Rarity"],
    "HeroData": ["MaxHealth", "MaxHealthMultiplier", "DefaultWeapon"],
}


def collect(base, pattern, table_name):
    """合并同名前缀的所有文件，返回 {条目名: 条目dict}。"""
    merged = {}
    files = sorted(glob.glob(os.path.join(base, pattern)))
    for f in files:
        try:
            data = load_file(f)
        except Exception as e:
            print(f"  [跳过] {os.path.basename(f)}: {e}")
            continue
        tbl = data.get(table_name)
        if not isinstance(tbl, dict):
            continue
        for k, v in tbl.items():
            if k == "__array__":
                continue
            if k in merged and isinstance(merged[k], dict) and isinstance(v, dict):
                merged[k].update(v)
            else:
                merged[k] = v
    return merged, len(files)


def to_rows(entries, table_name):
    """把条目转成扁平行。"""
    keycols = KEY_COLS.get(table_name, [])
    rows = []
    for name, body in entries.items():
        if not isinstance(body, dict):
            continue
        flat = flatten(body)
        row = {"条目": name}
        for c in keycols:
            v = flat.get(c, "")
            row[c] = v
        # 数值型字段：只保留最上层
        for k, v in flat.items():
            if k in keycols:
                continue
            depth = k.count(".")
            if depth <= 2 and isinstance(v, (int, float, bool)) and not isinstance(v, bool):
                row[k] = v
        rows.append(row)
    return rows


def write_csv(rows, path):
    if not rows:
        return 0
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


def main():
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1]

    os.makedirs(OUT, exist_ok=True)
    summary = []

    for label, (pattern, table_name) in TABLES.items():
        if only and only not in label.lower() and only != table_name.lower():
            continue
        for gen, base in [("Hades1", H1), ("Hades2", H2)]:
            entries, nfiles = collect(base, pattern, table_name)
            if not entries:
                continue
            rows = to_rows(entries, table_name)
            if not rows:
                continue
            csv_path = os.path.join(OUT, f"{gen}-{table_name}.csv")
            n = write_csv(rows, csv_path)
            summary.append((gen, label, n, nfiles))
            print(f"  {gen} {label:20} {n:5} 条  ({nfiles} 文件)")

    # 写索引
    idx = os.path.join(OUT, "_索引.md")
    with open(idx, "w", encoding="utf-8") as f:
        f.write("# Hades 1 / 2 数值表导出\n\n")
        f.write(f"源目录：\n- H1 `{H1}`\n- H2 `{H2}`\n\n")
        f.write("| 游戏 | 表 | 条数 | 文件数 |\n|---|---|---|---|\n")
        for gen, label, n, nf in summary:
            f.write(f"| {gen} | {label} | {n} | {nf} |\n")
        f.write("\n> 这些数据来自游戏自带的明文 Lua 脚本，不是反编译产物。\n")
    print(f"\n输出目录：{OUT}")
    print(f"索引：{idx}")


if __name__ == "__main__":
    main()
