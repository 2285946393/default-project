"""扫描 Hades II 的 .pkg_manifest，提取封包内的资源清单。

pkg 是 Supergiant 自家的封包格式（不是 Unity 的 bundle）。
manifest 是索引文件，内部路径以明文存在，可以直接读出资源树。

用法：
    python scan_packages.py
"""

import glob
import json
import os
import re
import sys
from collections import Counter

sys.stdout.reconfigure(encoding="utf-8")

PKG_ROOT = r"D:/software/steam/steamapps/common/Hades II/Content/Packages"
OUT = r"D:/work/AI产出/Hades反编译"

# 路径特征：反斜杠分隔、字母开头、长度足够
PATH_RE = re.compile(rb"[A-Za-z][A-Za-z0-9_\-\\/]{5,120}")


def extract_paths(manifest_path):
    """从 manifest 里抽资源路径。"""
    with open(manifest_path, "rb") as f:
        data = f.read()

    raw = PATH_RE.findall(data)
    out = []
    seen = set()
    for b in raw:
        try:
            s = b.decode("ascii")
        except UnicodeDecodeError:
            continue
        # 必须像路径：有反斜杠，或者首字母大写的资源名
        if "\\" not in s and not re.match(r"^[A-Z][A-Za-z0-9_]+$", s):
            continue
        if s in seen:
            continue
        seen.add(s)
        out.append(s)
    return out


def classify(path):
    """按顶层目录归类。"""
    p = path.replace("/", "\\")
    if "\\" in p:
        return p.split("\\")[0]
    return "(资源名)"


def main():
    os.makedirs(OUT, exist_ok=True)
    manifests = sorted(glob.glob(os.path.join(PKG_ROOT, "*", "*.pkg_manifest")))
    if not manifests:
        print("没找到 pkg_manifest")
        return

    all_rows = []
    per_pkg = {}

    for mf in manifests:
        pkg_name = os.path.basename(mf).replace(".pkg_manifest", "")
        paths = extract_paths(mf)
        per_pkg[pkg_name] = len(paths)
        for p in paths:
            all_rows.append({"封包": pkg_name, "分类": classify(p), "路径": p})

    print(f"扫描 {len(manifests)} 个封包，共 {len(all_rows)} 条资源记录\n")

    # 按封包
    print("按封包：")
    for k, v in sorted(per_pkg.items(), key=lambda t: -t[1])[:15]:
        print(f"  {k:28} {v:5}")

    # 按分类
    cats = Counter(r["分类"] for r in all_rows)
    print("\n按资源类型：")
    for k, v in cats.most_common(20):
        print(f"  {k:28} {v:5}")

    # 写 CSV
    import csv

    csv_path = os.path.join(OUT, "Hades2-封包资源清单.csv")
    with open(csv_path, "w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=["封包", "分类", "路径"])
        w.writeheader()
        w.writerows(all_rows)
    print(f"\n清单 -> {csv_path}")

    # 汇总
    summary = {
        "封包数": len(manifests),
        "资源记录数": len(all_rows),
        "按分类": dict(cats),
        "按封包": per_pkg,
    }
    jpath = os.path.join(OUT, "Hades2-封包资源统计.json")
    with open(jpath, "w", encoding="utf-8") as f:
        json.dump(summary, f, ensure_ascii=False, indent=1)
    print(f"统计 -> {jpath}")


if __name__ == "__main__":
    main()
