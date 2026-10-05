#!/usr/bin/env python3
"""全息典藏卡册 —— 一次做一整套「徽记卡」，并打包成单文件卡册页面。

渲染器外壳（Three.js + 镭射材质）来自（MIT）:
    https://github.com/HRuiCcc/RuiC-card-skill
本脚本不跑 Blender：卡牌几何用 Python 直接写成 glTF。

用法:
    python tools/holo-card/make_cards.py --vendor <含 app.bundle.js 的目录> \
        --out pages/全息典藏卡册.html
"""

import argparse
import base64
import io
import json
import math
import os
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageOps

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from make_card import (  # noqa: E402
    H, SS, W, font, sakura, build_glb,
)

# ---------------------------------------------------------------- 卡组设定

CARDS = [
    {
        "id": "tail",
        "no": "001", "total": "005",
        "title": "鲸尾", "subtitle": "樱花海",
        "tagline": "把整片海，收进一条尾巴里",
        "technique": "TAIL OF THE TIDE",
        "rarity": "SSR",
        "description": "第一张。鲸尾划过樱花落下的海面，镭射会跟着视角流动。",
        "emblem": "tail",
        "foil": 0.62, "finish": "pearl",
        "pal": {
            "sky": (255, 226, 240), "sky2": (255, 203, 226), "haze": (252, 236, 242),
            "sea1": (168, 226, 238), "sea2": (58, 146, 174), "deep": (10, 48, 70),
            "accent": (255, 158, 196), "accent2": (126, 240, 255),
            "ring": (232, 198, 128), "ink": (255, 252, 248), "glyph1": (255, 236, 246),
            "glyph2": (255, 150, 190), "badge1": (58, 132, 162), "badge2": (12, 54, 78),
        },
    },
    {
        "id": "jelly",
        "no": "002", "total": "005",
        "title": "水母", "subtitle": "夜光潮",
        "tagline": "深海里那盏不用电的灯",
        "technique": "GLOW OF THE DEEP",
        "rarity": "SR",
        "description": "第二张。水母的裙边与触手，在镭射下会一层层亮起来。",
        "emblem": "jelly",
        "foil": 0.72, "finish": "pearl",
        "pal": {
            "sky": (216, 214, 255), "sky2": (176, 178, 250), "haze": (226, 222, 252),
            "sea1": (120, 176, 232), "sea2": (48, 84, 168), "deep": (16, 22, 66),
            "accent": (176, 150, 255), "accent2": (140, 255, 236),
            "ring": (198, 178, 255), "ink": (248, 246, 255), "glyph1": (226, 255, 250),
            "glyph2": (140, 210, 255), "badge1": (72, 86, 168), "badge2": (14, 20, 60),
        },
    },
    {
        "id": "wave",
        "no": "003", "total": "005",
        "title": "潮汐", "subtitle": "月光浪",
        "tagline": "月亮拉着海，走了一整夜",
        "technique": "MOONLIT TIDE",
        "rarity": "SR",
        "description": "第三张。三道浪纹叠在月轮上，银箔质感最像它。",
        "emblem": "wave",
        "foil": 0.58, "finish": "silver",
        "pal": {
            "sky": (226, 244, 252), "sky2": (186, 226, 244), "haze": (238, 248, 252),
            "sea1": (108, 190, 216), "sea2": (32, 106, 148), "deep": (8, 40, 62),
            "accent": (168, 226, 246), "accent2": (255, 255, 255),
            "ring": (226, 236, 244), "ink": (250, 253, 255), "glyph1": (255, 255, 255),
            "glyph2": (150, 208, 236), "badge1": (46, 116, 154), "badge2": (8, 38, 60),
        },
    },
    {
        "id": "star",
        "no": "004", "total": "005",
        "title": "星屑", "subtitle": "泡泡星轨",
        "tagline": "海底也能看见星星，只要抬头够久",
        "technique": "STARDUST ORBIT",
        "rarity": "SSR",
        "description": "第四张。星星和泡泡绕着轨道走，烫金材质最出效果。",
        "emblem": "star",
        "foil": 0.78, "finish": "gold",
        "pal": {
            "sky": (255, 238, 214), "sky2": (252, 216, 178), "haze": (255, 244, 226),
            "sea1": (150, 176, 200), "sea2": (44, 72, 118), "deep": (10, 20, 46),
            "accent": (255, 214, 140), "accent2": (255, 246, 210),
            "ring": (236, 200, 128), "ink": (255, 250, 238), "glyph1": (255, 246, 214),
            "glyph2": (255, 190, 96), "badge1": (68, 86, 138), "badge2": (12, 20, 48),
        },
    },
    {
        "id": "sakura",
        "no": "005", "total": "005",
        "title": "樱信", "subtitle": "风的花信",
        "tagline": "花瓣落进海里，就是回信",
        "technique": "LETTER IN BLOOM",
        "rarity": "R",
        "description": "第五张。一整朵樱花当徽记，粉色卡面配珠光最温柔。",
        "emblem": "sakura",
        "foil": 0.5, "finish": "pearl",
        "pal": {
            "sky": (255, 236, 244), "sky2": (255, 210, 232), "haze": (255, 242, 246),
            "sea1": (238, 176, 200), "sea2": (150, 74, 120), "deep": (52, 18, 52),
            "accent": (255, 186, 214), "accent2": (255, 236, 250),
            "ring": (240, 200, 216), "ink": (255, 250, 252), "glyph1": (255, 240, 248),
            "glyph2": (255, 160, 200), "badge1": (168, 84, 128), "badge2": (44, 16, 46),
        },
    },
]


# ---------------------------------------------------------------- 小工具

def bezier(p0, p1, p2, p3, n=26):
    out = []
    for i in range(n + 1):
        t = i / n
        u = 1 - t
        x = u ** 3 * p0[0] + 3 * u * u * t * p1[0] + 3 * u * t * t * p2[0] + t ** 3 * p3[0]
        y = u ** 3 * p0[1] + 3 * u * u * t * p1[1] + 3 * u * t * t * p2[1] + t ** 3 * p3[1]
        out.append((x, y))
    return out


def vgrad(size, c0, c1):
    w, h = size
    t = np.linspace(0, 1, h)[:, None, None]
    a = np.array(c0, dtype=float)[None, None, :]
    b = np.array(c1, dtype=float)[None, None, :]
    arr = (a * (1 - t) + b * t).astype(np.uint8)
    return Image.fromarray(np.repeat(arr, w, axis=1), "RGB")


def rgrad(size, c_in, c_out, power=1.0):
    w, h = size
    yy, xx = np.mgrid[0:h, 0:w]
    cx, cy = (w - 1) / 2, (h - 1) / 2
    r = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2) / (min(w, h) / 2)
    r = np.clip(r, 0, 1) ** power
    a = np.array(c_in, dtype=float)[None, None, :]
    b = np.array(c_out, dtype=float)[None, None, :]
    arr = (a * (1 - r[..., None]) + b * r[..., None]).astype(np.uint8)
    return Image.fromarray(arr, "RGB")


def fill_gradient(mask, grad):
    layer = grad.convert("RGBA")
    layer.putalpha(mask)
    return layer


def blur_circle(img, cx, cy, r, color, blur):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse([(cx - r) * SS, (cy - r) * SS, (cx + r) * SS, (cy + r) * SS], fill=color)
    img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(blur * SS)))


# ---------------------------------------------------------------- 背景层

def make_background(card):
    pal = card["pal"]
    idx = [c["id"] for c in CARDS].index(card["id"])
    stops = [
        (0.00, pal["sky"]), (0.26, pal["sky2"]), (0.44, pal["haze"]),
        (0.50, pal["sea1"]), (0.66, pal["sea2"]), (0.86, pal["deep"]), (1.00, tuple(int(v * 0.72) for v in pal["deep"])),
    ]
    ys = np.linspace(0, 1, H)
    rows = np.zeros((H, 3))
    for i in range(len(stops) - 1):
        p0, c0 = stops[i]
        p1, c1 = stops[i + 1]
        m = (ys >= p0) & (ys <= p1)
        t = (ys[m] - p0) / max(1e-6, (p1 - p0))
        for ch in range(3):
            rows[m, ch] = c0[ch] + (c1[ch] - c0[ch]) * t
    img = Image.fromarray(np.repeat(rows[:, None, :], W, axis=1).astype(np.uint8), "RGB")
    img = img.resize((W * SS, H * SS), Image.BICUBIC).convert("RGBA")

    # 徽记背后的柔光（每张卡换个位置，画面不重复）
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gx = (512, 512, 470, 560, 512)[idx]
    gy = (600, 600, 620, 580, 600)[idx]
    gd.ellipse([(gx - 300) * SS, (gy - 300) * SS, (gx + 300) * SS, (gy + 300) * SS],
               fill=tuple(list(pal["accent2"]) + [70]))
    gd.ellipse([140 * SS, 700 * SS, 884 * SS, 830 * SS], fill=(255, 255, 255, 90))
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(64 * SS)))

    # 云 / 波 / 星屑
    deco = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(deco)
    if idx in (0, 4):
        for (cx, cy, rx, ry, a) in [(236, 236, 150, 32, 130), (752, 172, 168, 34, 120), (520, 348, 196, 28, 92)]:
            d.ellipse([(cx - rx) * SS, (cy - ry) * SS, (cx + rx) * SS, (cy + ry) * SS], fill=(255, 255, 255, a))
    elif idx == 1:
        for i in range(26):
            cx = 90 + (i * 137) % 860
            cy = 120 + (i * 271) % 1290
            r = 3 + (i % 4)
            d.ellipse([(cx - r) * SS, (cy - r) * SS, (cx + r) * SS, (cy + r) * SS],
                      fill=tuple(list(pal["accent2"]) + [150]))
    elif idx == 3:
        for i in range(34):
            cx = 70 + (i * 197) % 890
            cy = 90 + (i * 313) % 1330
            s = 4 + (i % 5)
            d.line([(cx - s) * SS, cy * SS, (cx + s) * SS, cy * SS], fill=(255, 246, 214, 150), width=int(1.6 * SS))
            d.line([cx * SS, (cy - s) * SS, cx * SS, (cy + s) * SS], fill=(255, 246, 214, 150), width=int(1.6 * SS))
    else:
        for i in range(7):
            a = math.radians(i * 51 + 12)
            cx = 512 + math.cos(a) * (300 + (i % 3) * 60)
            cy = 620 + math.sin(a) * (330 + (i % 2) * 70)
            d.line([(512 + math.cos(a) * 250) * SS, (620 + math.sin(a) * 280) * SS,
                    cx * SS, cy * SS], fill=tuple(list(pal["accent"]) + [90]), width=int(2 * SS))
    img.alpha_composite(deco.filter(ImageFilter.GaussianBlur(2 * SS)))

    # 泡泡（都避开徽记正中）
    bubbles = Image.new("RGBA", img.size, (0, 0, 0, 0))
    bd = ImageDraw.Draw(bubbles)
    spots = [(176, 900, 26, 90), (148, 1090, 16, 70), (842, 860, 30, 82), (876, 1060, 18, 66),
             (250, 1300, 20, 62), (770, 1290, 24, 64), (286, 468, 14, 70), (742, 440, 12, 62),
             (330, 700, 10, 54), (700, 690, 11, 56)]
    for (cx, cy, r, a) in spots:
        bd.ellipse([(cx - r) * SS, (cy - r) * SS, (cx + r) * SS, (cy + r) * SS],
                   fill=(255, 255, 255, a // 4), outline=(255, 255, 255, a), width=int(2 * SS))
    img.alpha_composite(bubbles)
    return img.convert("RGB").resize((W, H), Image.LANCZOS)


# ---------------------------------------------------------------- 徽记层

def glyph_mask(kind, size=560, scale=1.0):
    """把徽记图形画成 L 通道的遮罩（超采样）。"""
    m = Image.new("L", (size * SS, size * SS), 0)
    d = ImageDraw.Draw(m)
    c = size * SS / 2

    def sakura_m(cx, cy, r, rot=0, val=255):
        for i in range(5):
            a = math.radians(rot + i * 72)
            pts = []
            for t in range(0, 181, 12):
                th = math.radians(t)
                rr = r * (0.45 + 0.55 * math.sin(th))
                pts.append((cx + rr * math.cos(th + a - math.pi / 2), cy + rr * math.sin(th + a - math.pi / 2)))
            for t in range(180, -1, -12):
                th = math.radians(t)
                rr = r * (0.45 + 0.55 * math.sin(th))
                pts.append((cx + rr * 0.55 * math.cos(-th + a - math.pi / 2),
                            cy + rr * 0.55 * math.sin(-th + a - math.pi / 2)))
            d.polygon(pts, fill=val)
        d.ellipse([cx - r * 0.18, cy - r * 0.18, cx + r * 0.18, cy + r * 0.18], fill=val)

    def poly(pts, fill=255, width=0):
        pts = [(c + x * SS * scale, c + y * SS * scale) for x, y in pts]
        if width:
            d.line(list(pts) + [pts[0]], fill=fill, width=int(width * SS * scale), joint="curve")
        else:
            d.polygon(pts, fill=fill)

    if kind == "tail":
        pts = bezier((-30, 152), (-8, 146), (8, 146), (30, 152))
        pts += bezier((30, 152), (116, 62), (152, -42), (130, -182))
        pts += bezier((130, -182), (86, -134), (32, -48), (0, 32))
        pts += bezier((0, 32), (-32, -48), (-86, -134), (-130, -182))
        pts += bezier((-130, -182), (-152, -42), (-116, 62), (-30, 152))
        poly(pts)
        d.ellipse([c - 13 * SS, c - 56 * SS, c + 13 * SS, c - 18 * SS], fill=0)
    elif kind == "jelly":
        d.chord([c - 132 * SS, c - 150 * SS, c + 132 * SS, c + 92 * SS], 180, 360, fill=255)
        d.polygon([(c - 132 * SS, c - 30 * SS), (c + 132 * SS, c - 30 * SS),
                   (c + 120 * SS, c + 40 * SS), (c - 120 * SS, c + 40 * SS)], fill=255)
        for i, x0 in enumerate([-104, -52, 0, 52, 104]):
            pts = []
            for j in range(41):
                t = j / 40
                x = x0 + math.sin(t * math.pi * 2 + i) * 16
                y = 44 + t * 150
                pts.append((x, y))
            for j, (x, y) in enumerate(pts):
                rr = 7.5 - 5.0 * (j / len(pts))
                d.ellipse([c + (x - rr) * SS, c + (y - rr) * SS, c + (x + rr) * SS, c + (y + rr) * SS], fill=255)
        d.ellipse([c - 118 * SS, c - 118 * SS, c - 74 * SS, c - 74 * SS], fill=0)
        d.ellipse([c + 74 * SS, c - 118 * SS, c + 118 * SS, c - 74 * SS], fill=0)
    elif kind == "wave":
        d.ellipse([c - 120 * SS, c - 176 * SS, c + 120 * SS, c + 64 * SS], fill=255)
        for i, (yy, th) in enumerate([(30, 12), (86, 9), (134, 6)]):
            pts = [(x, yy + math.sin((x + 200) / 78) * 22) for x in range(-210, 211, 4)]
            d.line([(c + x * SS, c + y * SS) for x, y in pts], fill=0, width=int(th * SS), joint="curve")
        d.ellipse([c + 84 * SS, c - 158 * SS, c + 132 * SS, c - 110 * SS], fill=0)
        d.ellipse([c - 150 * SS, c + 120 * SS, c - 126 * SS, c + 144 * SS], fill=255)
        d.ellipse([c + 130 * SS, c + 96 * SS, c + 156 * SS, c + 122 * SS], fill=255)
    elif kind == "star":
        def star(cx, cy, r, inner=0.42, n=5, rot=-math.pi / 2):
            pts = []
            for i in range(n * 2):
                a = rot + i * math.pi / n
                rr = r if i % 2 == 0 else r * inner
                pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
            return pts
        poly(star(0, 0, 170))
        poly(star(96, -104, 62, 0.45, 5, math.pi / 6))
        d.ellipse([c - 246 * SS, c - 116 * SS, c + 246 * SS, c + 116 * SS], outline=255, width=int(4 * SS))
        for (sx, sy, sr) in [(-190, 96, 9), (176, 128, 8), (-236, -60, 7)]:
            d.ellipse([c + (sx - sr) * SS, c + (sy - sr) * SS, c + (sx + sr) * SS, c + (sy + sr) * SS], fill=255)
    elif kind == "sakura":
        sakura_m(c, c - 10 * SS, 128 * SS, 16)
        pts = bezier((-215, 150), (-140, 108), (-70, 96), (10, 0), 24)
        d.line([(c + x * SS, c + y * SS) for x, y in pts], fill=210, width=int(9 * SS), joint="curve")
        for (px, py, pr) in [(-160, 120, 26), (60, 150, 22), (180, -60, 20)]:
            sakura_m(c + px * SS, c + py * SS, pr * SS, 24)
    return m


GLYPH_SIZE = 560


def glyph_box(cx, cy, size=GLYPH_SIZE):
    off = (int(cx * SS - size * SS / 2), int(cy * SS - size * SS / 2))
    return off


def glyph_full(card, canvas, cx, cy, size=GLYPH_SIZE, mode="color"):
    """把方形徽记遮罩贴到整张画布的对应位置，别的什么都不改。"""
    mask = glyph_mask(card["emblem"], size=size)
    out = Image.new("L", canvas, 0)
    out.paste(mask, glyph_box(cx, cy, size))
    if mode == "mask":
        return out
    pal = card["pal"]
    grad = vgrad((size * SS, size * SS), pal["glyph1"], pal["glyph2"])
    square = fill_gradient(mask, grad)
    layer = Image.new("RGBA", canvas, (0, 0, 0, 0))
    layer.paste(square, glyph_box(cx, cy, size), square)
    return layer


def make_emblem(card):
    pal = card["pal"]
    canvas = (W * SS, H * SS)
    img = Image.new("RGBA", canvas, (0, 0, 0, 0))
    cx, cy, R = 512, 600, 252

    # 徽章投影
    blur_circle(img, cx + 6, cy + 16, R + 6, tuple(list((8, 30, 46)) + [110]), 26)

    ring = Image.new("RGBA", img.size, (0, 0, 0, 0))
    rd = ImageDraw.Draw(ring)
    # 外圈 + 内圈 + 刻度
    rd.ellipse([(cx - R) * SS, (cy - R) * SS, (cx + R) * SS, (cy + R) * SS],
               outline=tuple(list(pal["ring"]) + [235]), width=int(3.4 * SS))
    rd.ellipse([(cx - R + 13) * SS, (cy - R + 13) * SS, (cx + R - 13) * SS, (cy + R - 13) * SS],
               outline=tuple(list(pal["ring"]) + [150]), width=int(1.4 * SS))
    for i in range(24):
        a = math.radians(i * 15)
        big = i % 6 == 0
        r0, r1 = (R - 22, R - 4) if big else (R - 14, R - 4)
        rd.line([(cx + math.cos(a) * r0) * SS, (cy + math.sin(a) * r0) * SS,
                 (cx + math.cos(a) * r1) * SS, (cy + math.sin(a) * r1) * SS],
                fill=tuple(list(pal["ring"]) + [190 if big else 110]), width=int((2.2 if big else 1.2) * SS))
    img.alpha_composite(ring)

    # 徽章底盘（径向渐变）
    disc = Image.new("L", (W * SS, H * SS), 0)
    ImageDraw.Draw(disc).ellipse([(cx - R + 26) * SS, (cy - R + 26) * SS,
                                  (cx + R - 26) * SS, (cy + R - 26) * SS], fill=255)
    inner = rgrad((int((R - 25) * 2 * SS), int((R - 25) * 2 * SS)), pal["badge1"], pal["badge2"], 0.9)
    disc_small = Image.new("L", inner.size, 0)
    ImageDraw.Draw(disc_small).ellipse([0, 0, inner.size[0] - 1, inner.size[1] - 1], fill=255)
    base = fill_gradient(disc_small, inner)
    img.alpha_composite(base, (int((cx - R + 25) * SS), int((cy - R + 25) * SS)))

    # 内圈高光
    hi = Image.new("RGBA", img.size, (0, 0, 0, 0))
    hd = ImageDraw.Draw(hi)
    hd.arc([(cx - R + 40) * SS, (cy - R + 40) * SS, (cx + R - 40) * SS, (cy + R - 40) * SS],
           200, 330, fill=(255, 255, 255, 90), width=int(2.4 * SS))
    img.alpha_composite(hi)

    # 徽记本体的柔光
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    gm = glyph_full(card, canvas, cx, cy, mode="mask")
    glow.paste(tuple(list(pal["accent2"]) + [170]), (0, 0), gm)
    img.alpha_composite(glow.filter(ImageFilter.GaussianBlur(20 * SS)))

    # 徽记本体
    img.alpha_composite(glyph_full(card, canvas, cx, cy))

    # 徽记描边
    edge = glyph_mask(card["emblem"]).filter(ImageFilter.FIND_EDGES)
    edge_full = Image.new("L", canvas, 0)
    edge_full.paste(edge, glyph_box(cx, cy))
    line_full = Image.new("RGBA", img.size, (0, 0, 0, 0))
    line_full.paste(tuple(list(pal["ink"]) + [190]), (0, 0), edge_full)
    img.alpha_composite(line_full)

    # 环外的花瓣 / 点缀
    deco = Image.new("RGBA", img.size, (0, 0, 0, 0))
    dd = ImageDraw.Draw(deco)
    for i, (ang, dist, size_) in enumerate([(28, R + 44, 30), (96, R + 30, 22), (168, R + 50, 26),
                                            (216, R + 34, 20), (288, R + 46, 28), (338, R + 28, 18)]):
        a = math.radians(ang)
        px, py = cx + math.cos(a) * dist, cy + math.sin(a) * dist
        sakura(dd, px * SS, py * SS, size_ * SS, tuple(list(pal["accent"]) + [215]), ang, 1)
    img.alpha_composite(deco)
    return img.resize((W, H), Image.LANCZOS)


def make_lineart(emblem):
    alpha = emblem.split()[3]
    lum = emblem.convert("L")
    e1 = alpha.filter(ImageFilter.FIND_EDGES)
    e2 = lum.filter(ImageFilter.FIND_EDGES)
    e = ImageChops.lighter(e1, e2).filter(ImageFilter.GaussianBlur(0.6))
    line = ImageOps.invert(e.convert("L"))
    return line.convert("RGB")


# ---------------------------------------------------------------- 文字层

def make_text_layer(card):
    pal = card["pal"]
    img = Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    def rrect(box, radius, outline, width):
        d.rounded_rectangle([v * SS for v in box], radius=radius * SS, outline=outline, width=int(width * SS))

    ring = tuple(list(pal["ring"]) + [210])
    rrect([26, 26, W - 26, H - 26], 30, ring, 4)
    rrect([44, 44, W - 44, H - 44], 22, (255, 255, 255, 120), 2)

    f_small = font(30 * SS)
    f_title = font(120 * SS, bold=True)
    f_sub = font(52 * SS)
    f_tag = font(32 * SS)
    f_no = font(26 * SS)
    f_rar = font(30 * SS, bold=True)

    def put(xy, s, f, fill, anchor="mm", shadow=None, blur=6):
        x, y = xy[0] * SS, xy[1] * SS
        if shadow:
            layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
            ImageDraw.Draw(layer).text((x + 3 * SS, y + 4 * SS), s, font=f, fill=shadow, anchor=anchor)
            img.alpha_composite(layer.filter(ImageFilter.GaussianBlur(blur * SS)))
        d.text((x, y), s, font=f, fill=fill, anchor=anchor)

    put((W / 2, 106), "有花无实 · 全息典藏", f_small, tuple(list(pal["deep"]) + [235]))
    put((W / 2, 146), card["technique"], f_no, tuple(list(pal["deep"]) + [175]))
    put((W - 96, 202), "NO." + card["no"] + " / " + card["total"], f_no,
        tuple(list(pal["deep"]) + [210]), anchor="rm")
    put((96, 202), card["rarity"], f_rar, tuple(list(pal["ring"]) + [255]), anchor="lm")

    put((W / 2, 1192), card["title"], f_title, tuple(list(pal["ink"]) + [255]),
        shadow=tuple(list(pal["deep"]) + [155]), blur=7)
    put((W / 2, 1290), card["subtitle"], f_sub, tuple(list(pal["accent"]) + [255]),
        shadow=tuple(list(pal["deep"]) + [110]), blur=5)
    put((W / 2, 1348), card["tagline"], f_tag, (238, 246, 250, 240))
    put((W / 2, 1412), "WHALE-CHAN HOLOGRAM COLLECTION", f_no, tuple(list(pal["ring"]) + [200]))
    return img.resize((W, H), Image.LANCZOS)


# ---------------------------------------------------------------- 打包页面

def img_bytes(img, fmt, **kw):
    buf = io.BytesIO()
    img.save(buf, fmt, **kw)
    return buf.getvalue()


def data_uri(raw, mime):
    return "data:%s;base64,%s" % (mime, base64.b64encode(raw).decode("ascii"))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--vendor", required=True, help="含 app.bundle.js 的目录")
    ap.add_argument("--out", required=True)
    ap.add_argument("--work", default=None)
    args = ap.parse_args()

    vendor = Path(args.vendor)
    work = Path(args.work) if args.work else Path(os.environ.get("TEMP", ".")) / "holo-deck"
    work.mkdir(parents=True, exist_ok=True)

    bundle = (vendor / "app.bundle.js").read_text(encoding="utf-8").replace("</script", "<\\/script")
    css = (HERE / "page" / "style.css").read_text(encoding="utf-8")
    html = (HERE / "page" / "index.html").read_text(encoding="utf-8")

    print("· 造卡牌几何 card.glb")
    build_glb(work / "card.glb")
    model_uri = data_uri((work / "card.glb").read_bytes(), "model/gltf-binary")

    payload = []
    for card in CARDS:
        print("· 画卡面 %s %s" % (card["no"], card["title"]))
        bg = make_background(card)
        em = make_emblem(card)
        line = make_lineart(em)
        text = make_text_layer(card)
        cid = card["id"]
        bg.save(work / ("%s-bg.jpg" % cid), quality=88, optimize=True)
        em.save(work / ("%s-emblem.png" % cid), optimize=True)
        line.save(work / ("%s-line.jpg" % cid), quality=86, optimize=True)
        text.save(work / ("%s-text.png" % cid), optimize=True)
        payload.append({
            "id": cid, "no": card["no"], "total": card["total"],
            "title": card["title"], "subtitle": card["subtitle"], "tagline": card["tagline"],
            "technique": card["technique"], "rarity": card["rarity"], "description": card["description"],
            "foil": card["foil"], "finish": card["finish"],
            "assets": {
                "model": model_uri,
                "subject": data_uri((work / ("%s-emblem.png" % cid)).read_bytes(), "image/png"),
                "background": data_uri((work / ("%s-bg.jpg" % cid)).read_bytes(), "image/jpeg"),
                "lineart": data_uri((work / ("%s-line.jpg" % cid)).read_bytes(), "image/jpeg"),
                "text": data_uri((work / ("%s-text.png" % cid)).read_bytes(), "image/png"),
            },
        })

    cards_js = json.dumps(payload, ensure_ascii=False)
    first = {k: v for k, v in payload[0].items() if k != "id"}
    config = {
        "title": first["title"], "subtitle": first["subtitle"], "technique": first["technique"],
        "tagline": first["tagline"], "edition": "NO.%s / %s" % (first["no"], first["total"]),
        "collection": "有花无实 · 全息典藏", "description": first["description"],
        "sourceMode": "composite", "assets": first["assets"],
        "parameters": {"subjectScale": 1.0, "subjectDepth": 0.30, "backgroundDepth": -0.20, "foil": first["foil"]},
        "safeArea": {"scale": 1.0, "offset": [0, 0]},
        "appearance": {"background": "#fdf4f8", "finish": first["finish"]},
        "artworkFit": [1, 1],
    }

    shim = (
        "<script>\nwindow.__HOLO_CARDS__ = %s;\nwindow.__HOLO_CONFIG__ = %s;\n"
        "(function(){var f=window.fetch;window.fetch=function(u,o){"
        "var s=String((u&&u.url)||u);if(s.indexOf('card-config.json')>=0){"
        "return Promise.resolve(new Response(JSON.stringify(window.__HOLO_CONFIG__),"
        "{status:200,headers:{'Content-Type':'application/json'}}));}"
        "return f.apply(this,arguments);};})();\n</script>\n"
    ) % (cards_js, json.dumps(config, ensure_ascii=False))
    picker = "<script>\n%s\n</script>\n" % (HERE / "page" / "picker.js").read_text(encoding="utf-8")

    html = html.replace("/*__STYLE__*/", css)
    html = html.replace("<!--__BOOT__-->", shim + picker + '<script type="module">\n%s\n</script>' % bundle)
    Path(args.out).parent.mkdir(parents=True, exist_ok=True)
    Path(args.out).write_text(html, encoding="utf-8")
    print("完成：%s（%.2f MB）" % (args.out, Path(args.out).stat().st_size / 1024 / 1024))
    print("中间产物：%s" % work)


if __name__ == "__main__":
    main()
