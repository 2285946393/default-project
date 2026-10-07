"""扫游戏二进制里的可读字符串（ASCII + UTF-16LE），按关键词过滤。
用法: python scan-game-strings.py <文件> <关键词1> <关键词2> ...
"""
import re
import sys

def extract(path):
    with open(path, "rb") as f:
        data = f.read()
    # ASCII: 连续可见字符
    ascii_re = re.compile(rb"[\x20-\x7e]{6,}")
    # UTF-16LE: 可见字符 + \x00 交替
    u16_re = re.compile(rb"(?:[\x20-\x7e]\x00){6,}")
    out = set()
    for m in ascii_re.finditer(data):
        out.add(m.group().decode("ascii", "ignore"))
    for m in u16_re.finditer(data):
        out.add(m.group().decode("utf-16-le", "ignore"))
    return out

if __name__ == "__main__":
    path, kws = sys.argv[1], [k.lower() for k in sys.argv[2:]]
    ss = extract(path)
    for s in sorted(ss):
        low = s.lower()
        if any(k in low for k in kws):
            print(s)
