"""从 Godot 4 的 .pck 里**抽取文件内容**（不只是列目录）。

用法:
    python extract-pck-files.py <pck> <输出目录> [路径过滤关键词]

例:
    # 抽全部着色器（源码）
    python extract-pck-files.py "...\\SlayTheSpire2.pck" out "gdshader"
    # 抽全部 vfx 场景
    python extract-pck-files.py "...\\SlayTheSpire2.pck" out "scenes/vfx"

格式要点（Godot 4 GDPC v3，实测 2026-10-08）:
    头部 32 字节处是 u64 目录偏移；**偏移 24 处 u32 是 file_base**（本机 = 112）。
    目录条目 = plen(u32) + path(plen) + offset(u64) + size(u64) + md5(16) + flags(u32)。
    ⚠️ 条目里的 offset 是**相对 file_base** 的 —— 真实位置 = file_base + offset。
       这一条踩过坑：直接用 offset 去读，读到的是上一个文件的尾巴。
"""
import io
import os
import struct
import sys

FILE_BASE_HDR_OFF = 24   # u32：file_base
DIR_OFF_HDR_OFF = 32     # u64：目录偏移


def read_dir(f):
    head = f.read(64)
    if head[:4] != b"GDPC":
        raise SystemExit("不是 Godot pck（头部无 GDPC magic）")
    ver = struct.unpack_from("<i", head, 4)[0]
    file_base = struct.unpack_from("<i", head, FILE_BASE_HDR_OFF)[0]
    dir_off = struct.unpack_from("<Q", head, DIR_OFF_HDR_OFF)[0]

    f.seek(dir_off)
    count = struct.unpack("<i", f.read(4))[0]
    out = []
    for _ in range(count):
        plen = struct.unpack("<i", f.read(4))[0]
        path = f.read(plen).rstrip(b"\x00").decode("utf-8", "replace")
        ofs, size = struct.unpack("<QQ", f.read(16))
        f.read(16)                        # md5
        if ver >= 2:
            f.read(4)                     # flags
        out.append((path, file_base + ofs, size))
    return ver, file_base, count, out


def main():
    if len(sys.argv) < 3:
        raise SystemExit(__doc__)
    pck, outdir = sys.argv[1], sys.argv[2]
    flt = sys.argv[3] if len(sys.argv) > 3 else None

    with io.open(pck, "rb") as f:
        ver, file_base, count, entries = read_dir(f)
        print(f"GDPC v{ver} file_base={file_base} 共 {count} 条")

        hits = [e for e in entries if flt is None or flt.lower() in e[0].lower()]
        print(f"命中 {len(hits)} 个")
        total = 0
        for path, ofs, size in hits:
            dest = os.path.join(outdir, path)
            os.makedirs(os.path.dirname(dest), exist_ok=True)
            f.seek(ofs)
            data = f.read(size)
            with open(dest, "wb") as g:
                g.write(data)
            total += size
        print(f"写出 {len(hits)} 个文件 / {total/1024/1024:.1f} MB → {outdir}")


if __name__ == "__main__":
    main()
