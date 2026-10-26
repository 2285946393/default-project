"""
字段名定位法：在大文件里直接搜字段名字符串，读它后面对齐处的 double。
比整体解析 SerializedFile 快得多 —— 426MB 文件也能秒出。

用法: python find_field_values.py <assets文件路径> <字段名1> [字段名2] ...
"""
import sys
import os
import re
import struct
import mmap


def pretty(v):
    if v != v or v == 0:
        return False
    if abs(v) < 1e-9 or abs(v) > 1e15:
        return False
    x = abs(v) * 1e6
    return abs(x - round(x)) < 1e-3


def search(path, fields):
    size = os.path.getsize(path)
    out = {f: [] for f in fields}
    with open(path, 'rb') as fh:
        mm = mmap.mmap(fh.fileno(), 0, access=mmap.ACCESS_READ)
        for f in fields:
            fb = f.encode('ascii')
            start = 0
            while True:
                pos = mm.find(fb, start)
                if pos < 0:
                    break
                start = pos + 1
                # 校验：pos-4 处应等于字段名的长度
                if pos >= 4:
                    L = struct.unpack_from('<i', mm, pos - 4)[0]
                    if L != len(fb):
                        continue
                end = pos + len(fb)
                for q in ((end + 3) // 4 * 4, (end + 7) // 8 * 8):
                    if q + 8 > size:
                        continue
                    v = struct.unpack_from('<d', mm, q)[0]
                    if pretty(v):
                        out[f].append((pos, round(v, 8)))
                        break
        mm.close()
    return out


if __name__ == '__main__':
    path = sys.argv[1]
    fields = sys.argv[2:]
    print(f'# {os.path.basename(path)} ({os.path.getsize(path)/1048576:.0f}MB)')
    r = search(path, fields)
    for f, hits in r.items():
        if hits:
            vals = [v for _, v in hits]
            print(f'  {f:38s} x{len(hits)}  {vals}')
        else:
            print(f'  {f:38s} --')
