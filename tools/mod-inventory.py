#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
盘点本机杀戮尖塔 2 的 mod 库存（工坊 + 本地 mods/）。

用法:
    python tools/mod-inventory.py            # 打印表格
    python tools/mod-inventory.py --json     # 输出 JSON（给别的脚本吃）

坑（都在库存笔记里记过）:
  * manifest 文件名不固定 -> 按内容找（含 "id" 或 "name" 的 dict），不按文件名。
  * 编码必须 utf-8-sig（有 BOM）。
  * 深度要 >=2：框架包可能是嵌套目录（BaseLib 装在 3737335127/BaseLib/BaseLib.json）。
  * 目录名是工坊数字 id，mod 名在 json 里 —— 别拿中文去 grep 目录名。
  * 「目录时间新」不等于「刚下载」：作者更新也会刷新目录 mtime。
    判断新订阅要用 st_ctime（Windows 上是创建时间）。
"""
import os, sys, json, glob, datetime

WORKSHOP = r"D:/software/steam/steamapps/workshop/content/2868840"
LOCAL = r"D:/software/steam/steamapps/common/Slay the Spire 2/mods"
DISABLED = r"D:/software/steam/steamapps/common/Slay the Spire 2/_disabled_mods"


def read_manifest(root, max_depth=3):
    """在 root 下（深度 <= max_depth）找第一个像 manifest 的 json。"""
    pats = []
    for d in range(max_depth + 1):
        pats.append(os.path.join(root, *(["*"] * d), "*.json"))
    for pat in pats:
        for f in sorted(glob.glob(pat)):
            if not os.path.isfile(f):
                continue
            try:
                j = json.load(open(f, encoding="utf-8-sig"))
            except Exception:
                continue
            if isinstance(j, dict) and ("id" in j or "name" in j):
                return j, f
    return None, None


def scan_dir(base, source):
    out = []
    if not os.path.isdir(base):
        return out
    for name in sorted(os.listdir(base)):
        p = os.path.join(base, name)
        if not os.path.isdir(p):
            continue
        m, mf = read_manifest(p)
        dlls = glob.glob(os.path.join(p, "**", "*.dll"), recursive=True)
        pcks = glob.glob(os.path.join(p, "**", "*.pck"), recursive=True)
        try:
            ctime = datetime.datetime.fromtimestamp(os.stat(p).st_ctime).strftime("%Y-%m-%d %H:%M")
        except Exception:
            ctime = "?"
        out.append({
            "dir": name,
            "source": source,
            "path": p,
            "manifest_file": mf,
            "id": (m or {}).get("id"),
            "name": (m or {}).get("name"),
            "version": (m or {}).get("version"),
            "author": (m or {}).get("author"),
            "min_game_version": (m or {}).get("min_game_version"),
            "deps": [(d.get("id") if isinstance(d, dict) else str(d))
                     for d in (m or {}).get("dependencies", []) or []],
            "has_dll": len(dlls) > 0,
            "n_dll": len(dlls),
            "has_pck": len(pcks) > 0,
            "ctime": ctime,
            "no_manifest": m is None,
        })
    return out


def main():
    rows = scan_dir(WORKSHOP, "workshop") + scan_dir(LOCAL, "local") + scan_dir(DISABLED, "disabled")
    if "--json" in sys.argv:
        print(json.dumps(rows, ensure_ascii=False, indent=2))
        return
    print("共 %d 条（工坊 %d / 本地 %d / 已停用 %d）" % (
        len(rows),
        sum(1 for r in rows if r["source"] == "workshop"),
        sum(1 for r in rows if r["source"] == "local"),
        sum(1 for r in rows if r["source"] == "disabled"),
    ))
    print()
    print("%-11s %-6s %-26s %-12s %-5s %-4s %-6s %s" % (
        "工坊id/目录", "来源", "名字", "版本", "dll", "pck", "创建", "依赖"))
    print("-" * 130)
    for r in rows:
        if r["no_manifest"]:
            print("%-11s %-6s %-26s %s" % (r["dir"], r["source"], "(无 manifest)", r["path"]))
            continue
        print("%-11s %-6s %-26s %-12s %-5s %-4s %-6s %s" % (
            r["dir"], r["source"], (r["name"] or "?")[:26], (r["version"] or "?")[:12],
            "Y" if r["has_dll"] else "-", "Y" if r["has_pck"] else "-",
            r["ctime"][5:16], ",".join(r["deps"]) or "-"))


if __name__ == "__main__":
    main()
