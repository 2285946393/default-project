#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
从 Godot .pck 里提取单个文件（依赖 list_pck_scenes.read_pck_index）。

用法：
    python extract_pck_file.py <pck> <资源路径> [输出文件]

例：
    python extract_pck_file.py SlayTheSpire2.pck scenes/creature_visuals/guardbot.tscn /tmp/g.tscn
"""
import sys
import os

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from list_pck_scenes import read_pck_index   # noqa: E402


def extract(pck, res_path, out_path=None):
    for name, off, size in read_pck_index(pck, verbose=False):
        if name != res_path:
            continue
        with open(pck, "rb") as f:
            f.seek(off)
            data = f.read(size)
        if out_path:
            with open(out_path, "wb") as g:
                g.write(data)
            print(f"# 已写出 {len(data)} 字节 → {out_path}", file=sys.stderr)
        return data
    raise SystemExit(f"pck 里没有 '{res_path}'")


def main():
    if len(sys.argv) < 3:
        raise SystemExit(__doc__)
    data = extract(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else None)
    if len(sys.argv) <= 3:
        sys.stdout.buffer.write(data)


if __name__ == "__main__":
    main()
