# -*- coding: utf-8 -*-
"""把游戏本体的全部卡牌（中文名 + 效果描述）导出来，用来查"新卡是不是撞车了"。

用法： python tools/dump_base_cards.py
输出： tools/base_cards_zh.txt   （每行：中文名 <TAB> 内部ID <TAB> 效果描述）

原理：从游戏 pck 里抓中文卡表（靠 "BODY_SLAM.title"="全身撞击" 定位中文区），
      再跟反编译出来的 CardModel 类名对一遍，只留真正的卡。
"""
import io
import os
import re
import subprocess
import sys

GAME = r"D:\software\steam\steamapps\common\Slay the Spire 2"
PCK = os.path.join(GAME, "SlayTheSpire2.pck")
STS2 = os.path.join(GAME, "data_sts2_windows_x86_64", "sts2.dll")
PROJ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(PROJ, "tools", "base_cards_zh.txt")

CJK = re.compile(r"[\u4e00-\u9fff]")


def main():
    if not os.path.exists(PCK):
        sys.exit("找不到游戏 pck: %s" % PCK)
    data = open(PCK, "rb").read()
    anchor = '"BODY_SLAM.title": "全身撞击"'.encode("utf-8")
    pos = data.find(anchor)
    if pos < 0:
        sys.exit("定位不到中文卡表（游戏更新后锚点可能变了，换个常见卡名再试）")
    seg = data[max(0, pos - 3_000_000): pos + 3_000_000].decode("utf-8", "replace")

    def grab(kind):
        d = {}
        for m in re.finditer(r'"([A-Z][A-Z0-9_]{2,40})\.' + kind + r'":\s*"((?:[^"\\]|\\.)*)"', seg):
            if CJK.search(m.group(2)):
                d.setdefault(m.group(1), m.group(2))
        return d

    titles, descs = grab("title"), grab("description")

    out = subprocess.run(["ilspycmd", "-l", "c", STS2],
                         capture_output=True, text=True, encoding="utf-8", errors="replace").stdout
    names = {m.group(1) for m in
             (re.match(r"Class MegaCrit\.Sts2\.Core\.Models\.Cards\.([A-Za-z0-9_]+)$", l.strip())
              for l in out.splitlines()) if m}

    def snake(n):
        return re.sub(r"(?<!^)(?=[A-Z])", "_", n).upper()

    rows = sorted((titles[snake(n)], snake(n), descs.get(snake(n), "").replace("\\n", " "))
                  for n in names if snake(n) in titles)
    with io.open(OUT, "w", encoding="utf-8") as f:
        for zh, key, d in rows:
            f.write(f"{zh}\t{key}\t{d}\n")
    print(f"导出 {len(rows)} 张卡 -> {OUT}")
    print("查撞车：搜这个文件的效果列，例如  python -c \"import io;print(''.join(l for l in io.open('tools/base_cards_zh.txt',encoding='utf-8') if '格挡' in l.split(chr(9))[2]))\"")


if __name__ == "__main__":
    main()