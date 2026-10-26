"""
字节级扫描：在任意二进制里找 [int32长度][字段名][对齐][double] 模式。
不依赖 UnityPy / typetree —— 直接在文件字节流上找。

用法: python scan_raw_fields.py <文件或目录> [最小命中数]
"""
import os
import re
import sys
import struct
import mmap
import collections

# 只认"像游戏数值字段名"的：小驼峰 / 下划线小驼峰，长度 4-32
FIELD = re.compile(rb'_?[a-z][A-Za-z0-9]{3,31}')


def pretty(v):
    if v != v or v == 0:
        return False
    if abs(v) < 1e-9 or abs(v) > 1e15:
        return False
    x = abs(v) * 1e6
    return abs(x - round(x)) < 1e-3


def scan(path, min_hits=1):
    size = os.path.getsize(path)
    hits = collections.OrderedDict()
    try:
        with open(path, 'rb') as fh:
            mm = mmap.mmap(fh.fileno(), 0, access=mmap.ACCESS_READ)
            for m in FIELD.finditer(mm):
                s, e = m.span()
                if s < 4:
                    continue
                L = struct.unpack_from('<i', mm, s - 4)[0]
                if L != e - s:
                    continue
                for q in ((e + 3) // 4 * 4, (e + 7) // 8 * 8):
                    if q + 8 > size:
                        continue
                    v = struct.unpack_from('<d', mm, q)[0]
                    if pretty(v):
                        name = m.group().decode('ascii')
                        hits.setdefault(name, []).append(round(v, 8))
                        break
            mm.close()
    except Exception as ex:
        return None, str(ex)
    return {k: v for k, v in hits.items() if len(v) >= min_hits}, None


def files_of(arg):
    if os.path.isfile(arg):
        return [arg]
    out = []
    for f in os.listdir(arg):
        if f.endswith('.resS') or f.endswith('.dat') or f.endswith('.resource'):
            continue
        fp = os.path.join(arg, f)
        if os.path.isfile(fp):
            out.append(fp)
    return out


if __name__ == '__main__':
    target = sys.argv[1]
    min_hits = int(sys.argv[2]) if len(sys.argv) > 2 else 1
    fl = files_of(target)
    if os.path.isdir(target):
        fl.sort(key=lambda p: -os.path.getsize(p))
    total = 0
    for fp in fl:
        h, err = scan(fp, min_hits)
        if err:
            print(f'  [skip] {os.path.basename(fp)}: {err}')
            continue
        if not h:
            continue
        n = sum(len(v) for v in h.values())
        total += n
        print(f'\n### {os.path.basename(fp)} ({os.path.getsize(fp)/1048576:.1f}MB) — {len(h)} 个字段 / {n} 个值')
        for k, v in list(h.items())[:60]:
            u = sorted(set(v))
            print(f'   {k:32s} {u[:6]}')
    print(f'\n总计 {total} 个值')
