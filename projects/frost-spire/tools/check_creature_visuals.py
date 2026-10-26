#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
核查本体 `creature_visuals/*.tscn` 里哪些**真能当生物视觉用**。

★ 为什么需要这个：借外观时「场景存在」≠「能当生物视觉」——
  `MonsterModel.CreateVisuals()` 是
      PreloadManager.Cache.GetScene(VisualsPath).Instantiate<NCreatureVisuals>()
  所以场景根节点**必须挂 `NCreatureVisuals` 脚本**，否则抛
      InvalidCastException: Unable to cast 'Godot.Node2D' to 'NCreatureVisuals'
  然后回退 error 场景（场上一个错误占位）。典型反例：`battleworn_dummy.tscn`
  —— 文件确实在 pck 里（3194 字节），但根节点没挂那个脚本。

  判定方法：抠出 .tscn 文本，看 ext_resource 里有没有
      path="res://src/Core/Nodes/Combat/NCreatureVisuals.cs"

用法：
    python check_creature_visuals.py <pck 路径> [只看可用的关键字]
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from list_pck_scenes import read_pck_index   # noqa: E402

NEEDLE = b"res://src/Core/Nodes/Combat/NCreatureVisuals.cs"


def main():
    if len(sys.argv) < 2:
        raise SystemExit(__doc__)
    pck = sys.argv[1]
    keyword = sys.argv[2] if len(sys.argv) > 2 else ""

    entries = read_pck_index(pck, verbose=False)
    scenes = [(n, o, s) for n, o, s in entries
              if n.startswith("scenes/creature_visuals/") and n.endswith(".tscn")]

    good, bad = [], []
    with open(pck, "rb") as f:
        for name, off, size in scenes:
            f.seek(off)
            head = f.read(min(size, 4096))     # 脚本声明一定在文件开头
            (good if NEEDLE in head else bad).append(name.rsplit("/", 1)[-1])

    print(f"# creature_visuals 场景共 {len(scenes)} 个：可用 {len(good)} / 不可用 {len(bad)}")
    print("\n== 可用（根节点 = NCreatureVisuals）==")
    for n in sorted(good):
        if not keyword or keyword in n:
            print(f"  {n}")
    print("\n== 不可用（根节点不是 NCreatureVisuals，别拿来做生物视觉）==")
    for n in sorted(bad):
        print(f"  {n}")


if __name__ == "__main__":
    main()
