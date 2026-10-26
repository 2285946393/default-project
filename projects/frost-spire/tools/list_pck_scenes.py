#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
列出 Godot .pck 里的资源路径（只读文件索引，不读资源数据，1.9GB 也是秒级）。

用途：核实「本体的 creature_visuals 到底有哪些场景」——
借外观时路径必须指向真实存在的场景，否则会 fallback 到 error 场景
（日志：Asset not cached + InvalidCastException: Unable to cast 'Godot.Node2D' to 'NCreatureVisuals'）。

★ 实测格式（SlayTheSpire2.pck，Godot 4.5.1，pck 格式 v3）——索引区在**文件尾部**：

    头部 (112 字节)：
      +0   uint32 magic = 0x43504447 ("GDPC")
      +4   uint32 pack_version = 3
      +8   uint32 ver_major / ver_minor / ver_patch   (4.5.1)
      +20  uint32 pack_flags  (bit1 = PACK_REL_FILEBASE → offset 相对 file_base)
      +24  uint64 file_base_offset = 0x70 (112)        ← 数据区起点
      +32  uint64 directory_offset                     ← 索引区起点
      +40  64 字节 reserved
    （v1/v2 的 file_count 在头部；v3 挪到索引区开头）

    索引区 @directory_offset：
      uint32 file_count
      × file_count:
        uint32 path_len
        char[path_len] path          (4 字节对齐)
        uint64 offset                (相对 file_base)
        uint64 size
        byte[16] md5
        uint32 flags

用法：
    python list_pck_scenes.py <pck 路径> [路径关键字]

例：
    python list_pck_scenes.py "D:/.../SlayTheSpire2.pck" creature_visuals/
"""
import struct
import sys

GDPC = 0x43504447


def read_pck_index(path, verbose=True):
    with open(path, "rb") as f:
        head = f.read(40)
        magic, pack_version, vmaj, vmin, vpat, pack_flags = struct.unpack_from("<IIIIII", head, 0)
        if magic != GDPC:
            raise SystemExit(f"不是 Godot pck（magic=0x{magic:08X}）")
        file_base, = struct.unpack_from("<Q", head, 24)
        dir_off, = struct.unpack_from("<Q", head, 32)
        if verbose:
            print(f"# pck 格式 v{pack_version}，Godot {vmaj}.{vmin}.{vpat}，"
                  f"flags={pack_flags}，file_base={file_base}，索引区@{dir_off}", file=sys.stderr)

        f.seek(dir_off)
        (file_count,) = struct.unpack("<I", f.read(4))
        if verbose:
            print(f"# 共 {file_count} 个文件", file=sys.stderr)

        entries = []
        for _ in range(file_count):
            raw = f.read(4)
            if len(raw) < 4:
                break
            (plen,) = struct.unpack("<I", raw)
            if plen > 4096:      # 明显越界 = 格式猜错了，别静默跑飞
                raise SystemExit(f"path_len 异常（{plen}），索引格式可能不对")
            p = f.read(plen)
            pad = (4 - plen % 4) % 4
            if pad:
                f.read(pad)
            foff, fsize = struct.unpack("<QQ", f.read(16))
            f.read(16)           # md5
            f.read(4)            # flags
            # ★ path_len 是**对齐后的**长度（含 \x00 填充），所以路径尾部可能带 0x00，
            #   必须 rstrip 掉，否则按路径精确查找会「明明在列表里却查不到」。
            entries.append((p.decode("utf-8", "replace").rstrip("\x00"),
                            (file_base + foff) if (pack_flags & 2) else foff,
                            fsize))
        return entries


def main():
    if len(sys.argv) < 2:
        raise SystemExit(__doc__)
    pck = sys.argv[1]
    keyword = sys.argv[2] if len(sys.argv) > 2 else ""
    hits = 0
    for name, _off, size in read_pck_index(pck):
        if keyword and keyword not in name:
            continue
        print(f"{size:>10}  {name}")
        hits += 1
    if keyword:
        print(f"# 命中 {hits} 条", file=sys.stderr)


if __name__ == "__main__":
    main()
