# -*- coding: utf-8 -*-
"""
把 FrostSpire/images/card_art/ 下的卡面拼成一张总览图，方便一次看全。

用法：
    python tools/preview_card_art.py
输出：
    tools/card_art_preview.png

为什么放 tools/ 下：export_presets.cfg 的 exclude_filter 排除了 tools/*，
所以预览图**不会被打进 pck**（放项目根就会，因为 export_filter="all_files"）。

需要 PIL。managed Python 没有 PIL，用系统 Python：
    "D:/software/Python/python.exe" tools/preview_card_art.py
"""
import os

from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.normpath(os.path.join(HERE, ".."))
ART_DIR = os.path.join(PROJ, "FrostSpire", "images", "card_art")
OUT = os.path.join(HERE, "card_art_preview.png")

COLS = 4
GAP, PAD, TOP = 14, 16, 52
BG = (18, 22, 28)
FG = (230, 234, 239)
FONT = "C:/Windows/Fonts/arialbd.ttf"


def main():
    names = sorted(
        f[:-4] for f in os.listdir(ART_DIR)
        if f.endswith(".png") and not f.startswith("_")
    )
    if not names:
        print("没找到卡面图：", ART_DIR)
        return 1

    first = Image.open(os.path.join(ART_DIR, names[0] + ".png"))
    cw, ch = first.size
    rows = (len(names) + COLS - 1) // COLS

    w = PAD * 2 + COLS * cw + (COLS - 1) * GAP
    h = TOP + PAD + rows * ch + (rows - 1) * GAP
    canvas = Image.new("RGB", (w, h), BG)
    d = ImageDraw.Draw(canvas)
    d.text((PAD, 16), "FrostSpire - card art (%d)" % len(names),
           font=ImageFont.truetype(FONT, 22), fill=FG)

    for i, n in enumerate(names):
        im = Image.open(os.path.join(ART_DIR, n + ".png")).convert("RGB")
        r, c = divmod(i, COLS)
        canvas.paste(im, (PAD + c * (cw + GAP), TOP + PAD + r * (ch + GAP)))

    canvas.save(OUT)
    print("已保存 %s  (%dx%d, %d 张)" % (OUT, w, h, len(names)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
