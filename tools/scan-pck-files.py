"""解析 Godot 4 pck 目录，列出全部内嵌文件路径。
用法: python scan-pck-files.py <pck> [过滤关键词]
"""
import struct
import sys

path = sys.argv[1]
flt = sys.argv[2].lower() if len(sys.argv) > 2 else None

with open(path, "rb") as f:
    f.seek(0)
    if f.read(4) != b"GDPC":
        print("头部无 GDPC magic")
        sys.exit(1)
    ver = struct.unpack("<i", f.read(4))[0]     # pack format version
    vmaj, vmin, vpat = struct.unpack("<iii", f.read(12))
    flags = 0
    if ver >= 2:
        flags = struct.unpack("<i", f.read(4))[0]
    f.read(8)  # file base offset (int64)
    dir_off = struct.unpack("<i", f.read(4))[0]   # v3: 目录偏移就在头部
    f.seek(dir_off)
    reserved = f.read(0)  # noqa
    count = struct.unpack("<i", f.read(4))[0]
    print(f"GDPC v{ver} godot {vmaj}.{vmin}.{vpat} flags={flags:#x} 文件数={count}", file=sys.stderr)
    n = 0
    shown = 0
    for _ in range(count):
        plen = struct.unpack("<i", f.read(4))[0]
        p = f.read(plen).rstrip(b"\x00").decode("utf-8", "replace")
        f.read(8)   # offset
        f.read(8)   # size
        f.read(16)  # md5
        if ver >= 2:
            f.read(4)  # flags
        n += 1
        if flt is None or flt in p.lower():
            print(p)
            shown += 1
    print(f"—— 共 {n} 个文件，命中 {shown}", file=sys.stderr)
