#!/usr/bin/env python3
"""全息镭射卡生成器 —— 画四层图 + 造 card.glb + 打包成单文件网页。

借鉴（MIT）: https://github.com/HRuiCcc/RuiC-card-skill
用它的 web-template（style.css + app.bundle.js）当渲染器；这里不跑 Blender，
卡牌几何直接用 Python 写成 glTF，省掉 380MB 的 Blender 下载。

用法:
    python tools/holo-card/make_card.py --vendor <含 style.css/app.bundle.js 的目录> \
        --out pages/全息镭射卡.html
"""

import argparse
import base64
import io
import json
import math
import os
import struct
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

# ---------------------------------------------------------------- 基本参数

W, H = 1024, 1536          # 卡面贴图尺寸（2:3）
CARD_W, CARD_H = 6.3, 9.45  # 与上游 Blender 模板一致的卡牌尺寸（单位）
CARD_T = 0.045              # 卡厚
CORNER = 0.20               # 圆角半径（单位）
SS = 2                      # 超采样倍数，画完缩小当抗锯齿

INK = (30, 62, 74)          # 线稿颜色

SKIN = (255, 233, 222)
SKIN_SH = (244, 205, 196)
HAIR = (255, 176, 206)
HAIR_D = (238, 137, 180)
HAIR_L = (255, 214, 232)
TEAL = (92, 208, 219)
TEAL_D = (42, 136, 158)
WHITE = (252, 252, 255)
NAVY = (46, 78, 104)
BLUSH = (255, 152, 182)
EYE = (32, 76, 94)
GOLD = (216, 178, 104)


def lerp(a, b, t):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(len(a)))


def load_font(candidates, size):
    for path in candidates:
        if os.path.exists(path):
            try:
                return ImageFont.truetype(path, size)
            except OSError:
                continue
    return ImageFont.load_default()


F_SANS = "C:/Windows/Fonts/MiSans-Regular.otf"
F_YAHEI = "C:/Windows/Fonts/msyh.ttc"
F_YAHEI_B = "C:/Windows/Fonts/msyhbd.ttc"
F_KAI = "C:/Windows/Fonts/simkai.ttf"


def font(size, bold=False, kai=False):
    if kai:
        return load_font([F_KAI, F_YAHEI], size)
    if bold:
        return load_font([F_YAHEI_B, F_YAHEI], size)
    return load_font([F_SANS, F_YAHEI], size)


# ---------------------------------------------------------------- 画笔

class Pen:
    """同一套画法，color 模式填充、line 模式只描边 —— 保证线稿和主图严丝合缝。"""

    def __init__(self, img, mode):
        self.img = img
        self.d = ImageDraw.Draw(img)
        self.mode = mode

    def _box(self, box):
        return [v * SS for v in box]

    def ellipse(self, box, fill=None, outline=None, width=0, line_w=None):
        box = self._box(box)
        if self.mode == "color":
            self.d.ellipse(box, fill=fill, outline=outline, width=int(width * SS))
        else:
            self.d.ellipse(box, outline=INK, width=int((line_w or 3.2) * SS))

    def circle(self, cx, cy, r, **kw):
        self.ellipse([cx - r, cy - r, cx + r, cy + r], **kw)

    def poly(self, pts, fill=None, outline=None, width=0, line_w=None):
        pts = [(x * SS, y * SS) for x, y in pts]
        if self.mode == "color":
            self.d.polygon(pts, fill=fill, outline=outline)
            if width:
                self.d.line(list(pts) + [pts[0]], fill=outline or INK, width=int(width * SS), joint="curve")
        else:
            self.d.polygon(pts, outline=INK)
            self.d.line(list(pts) + [pts[0]], fill=INK, width=int((line_w or 3.2) * SS), joint="curve")

    def capsule(self, p0, p1, r, fill):
        if self.mode == "color":
            self.d.line([p0[0] * SS, p0[1] * SS, p1[0] * SS, p1[1] * SS], fill=fill, width=int(r * 2 * SS))
            self.circle(p0[0], p0[1], r, fill=fill)
            self.circle(p1[0], p1[1], r, fill=fill)
        else:
            self.d.line([p0[0] * SS, p0[1] * SS, p1[0] * SS, p1[1] * SS], fill=INK, width=int(3.2 * SS))
            self.circle(p0[0], p0[1], r, line_w=3.2)
            self.circle(p1[0], p1[1], r, line_w=3.2)

    def arc(self, box, start, end, fill, width, line_w=None):
        box = self._box(box)
        self.d.arc(box, start, end, fill=INK if self.mode == "line" else fill,
                   width=int((line_w or width) * SS))

    def chord(self, box, start, end, fill=None, line_w=None):
        box = self._box(box)
        if self.mode == "color":
            self.d.chord(box, start, end, fill=fill)
        else:
            self.d.arc(box, start, end, fill=INK, width=int((line_w or 3.2) * SS))

    def text(self, xy, s, f, fill, anchor="mm"):
        x, y = xy[0] * SS, xy[1] * SS
        self.d.text((x, y), s, font=f, fill=fill, anchor=anchor)


def petal(d, cx, cy, r, angle, color, scale=1):
    """一片花瓣（两个圆弧拼成的水滴形），够用。"""
    a = math.radians(angle)
    pts = []
    for t in range(0, 181, 15):
        th = math.radians(t)
        rr = r * (0.45 + 0.55 * math.sin(th))
        pts.append((cx + rr * math.cos(th + a - math.pi / 2), cy + rr * math.sin(th + a - math.pi / 2)))
    for t in range(180, -1, -15):
        th = math.radians(t)
        rr = r * (0.45 + 0.55 * math.sin(th))
        pts.append((cx + rr * 0.55 * math.cos(-th + a - math.pi / 2), cy + rr * 0.55 * math.sin(-th + a - math.pi / 2)))
    d.polygon([(x * scale, y * scale) for x, y in pts], fill=color)


def sakura(d, cx, cy, r, color, rot=0, scale=1):
    for i in range(5):
        petal(d, cx, cy, r, rot + i * 72, color, scale)
    d.ellipse([(cx - r * 0.18) * scale, (cy - r * 0.18) * scale,
               (cx + r * 0.18) * scale, (cy + r * 0.18) * scale],
              fill=(255, 236, 168))


# ---------------------------------------------------------------- 背景层

def make_background():
    stops = [
        (0.00, (255, 226, 240)),
        (0.24, (255, 205, 227)),
        (0.42, (253, 232, 240)),
        (0.50, (176, 224, 236)),
        (0.64, (72, 158, 184)),
        (0.82, (22, 92, 118)),
        (1.00, (8, 42, 62)),
    ]
    ys = np.linspace(0, 1, H)
    rows = np.zeros((H, 3))
    for i in range(len(stops) - 1):
        p0, c0 = stops[i]
        p1, c1 = stops[i + 1]
        m = (ys >= p0) & (ys <= p1)
        t = (ys[m] - p0) / (p1 - p0)
        for ch in range(3):
            rows[m, ch] = c0[ch] + (c1[ch] - c0[ch]) * t
    arr = np.repeat(rows[:, None, :], W, axis=1).astype(np.uint8)
    img = Image.fromarray(arr, "RGB").resize((W * SS, H * SS), Image.BICUBIC)

    overlay = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(overlay)

    # 海平线上的光晕 + 角色身后的柔光
    glow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse([140 * SS, 620 * SS, 884 * SS, 812 * SS], fill=(255, 250, 240, 165))
    gd.ellipse([250 * SS, 380 * SS, 774 * SS, 1120 * SS], fill=(255, 255, 255, 96))
    glow = glow.filter(ImageFilter.GaussianBlur(52 * SS))
    overlay = Image.alpha_composite(overlay, glow)
    d = ImageDraw.Draw(overlay)

    # 天上的云（扁一点、别糊成一坨）
    for (cx, cy, rx, ry, a) in [(232, 232, 150, 34, 120), (744, 168, 172, 36, 112),
                                (520, 342, 200, 30, 84), (872, 402, 120, 24, 76),
                                (150, 400, 110, 22, 70)]:
        cloud = Image.new("RGBA", img.size, (0, 0, 0, 0))
        cd = ImageDraw.Draw(cloud)
        cd.ellipse([(cx - rx) * SS, (cy - ry) * SS, (cx + rx) * SS, (cy + ry) * SS], fill=(255, 255, 255, a))
        cd.ellipse([(cx - rx * 0.55) * SS, (cy - ry * 1.5) * SS, (cx + rx * 0.45) * SS, (cy + ry * 0.6) * SS],
                   fill=(255, 255, 255, a))
        cloud = cloud.filter(ImageFilter.GaussianBlur(7 * SS))
        overlay = Image.alpha_composite(overlay, cloud)
    d = ImageDraw.Draw(overlay)

    # 海平线
    line = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ld = ImageDraw.Draw(line)
    ld.rectangle([0, 752 * SS, W * SS, 760 * SS], fill=(255, 252, 244, 70))
    line = line.filter(ImageFilter.GaussianBlur(6 * SS))
    overlay = Image.alpha_composite(overlay, line)
    d = ImageDraw.Draw(overlay)

    # 水下的斜光柱
    for (x0, wdt, a) in [(180, 90, 34), (430, 130, 26), (760, 100, 30)]:
        ray = Image.new("RGBA", img.size, (0, 0, 0, 0))
        rd = ImageDraw.Draw(ray)
        rd.polygon([(x0 * SS, 760 * SS), ((x0 + wdt) * SS, 760 * SS),
                    ((x0 + wdt + 210) * SS, 1536 * SS), ((x0 + 150) * SS, 1536 * SS)],
                   fill=(255, 255, 255, a))
        ray = ray.filter(ImageFilter.GaussianBlur(28 * SS))
        overlay = Image.alpha_composite(overlay, ray)
    d = ImageDraw.Draw(overlay)

    # 泡泡（避开中间主体区）
    for (cx, cy, r, a) in [(196, 980, 26, 90), (150, 1180, 16, 70), (836, 900, 30, 80),
                           (876, 1120, 18, 66), (250, 1340, 20, 60), (760, 1330, 24, 62),
                           (300, 700, 14, 70), (720, 640, 12, 60)]:
        ring = Image.new("RGBA", img.size, (0, 0, 0, 0))
        rr = ImageDraw.Draw(ring)
        rr.ellipse([(cx - r) * SS, (cy - r) * SS, (cx + r) * SS, (cy + r) * SS],
                   fill=(255, 255, 255, max(10, a // 3)), outline=(255, 255, 255, a), width=int(2 * SS))
        overlay = Image.alpha_composite(overlay, ring)
    d = ImageDraw.Draw(overlay)

    # 樱花（只撒在四角/边缘，中间留干净）
    for (cx, cy, r, rot, a) in [(120, 430, 34, 12, 220), (900, 330, 28, 40, 200),
                                (170, 1560, 30, 70, 190), (880, 1500, 34, 20, 210),
                                (420, 150, 24, 55, 180), (640, 90, 20, 15, 170),
                                (960, 700, 22, 33, 160), (70, 860, 20, 60, 150),
                                (300, 1180, 18, 25, 130), (740, 1240, 20, 50, 130)]:
        layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ld = ImageDraw.Draw(layer)
        sakura(ld, cx, cy, r, (255, 214, 232, a), rot, SS)
        overlay = Image.alpha_composite(overlay, layer)
    d = ImageDraw.Draw(overlay)

    img = Image.alpha_composite(img.convert("RGBA"), overlay).convert("RGB")
    return img.resize((W, H), Image.LANCZOS)


# ---------------------------------------------------------------- 主体层

def draw_character(pen):
    """小鲸鱼娘。同一套调用会同时产出彩图和线稿。"""
    cx, hy = 512, 560.0          # 头部中心
    hr = 150.0

    # 尾巴（身体后面，偏右下）
    pen.poly([(618, 1046), (704, 978), (818, 966), (716, 1062), (822, 1136),
              (712, 1176), (622, 1096)], fill=TEAL, outline=TEAL_D, width=3)
    pen.poly([(668, 1032), (734, 992), (792, 986), (722, 1046)], fill=(184, 243, 248))
    pen.poly([(668, 1104), (732, 1140), (790, 1144), (720, 1092)], fill=(184, 243, 248))
    # 后发
    pen.ellipse([cx - 186, hy - 152, cx + 186, hy + 232], fill=HAIR_D)
    # 身体（水手裙）
    pen.poly([(cx - 118, hy + 176), (cx + 118, hy + 176), (cx + 172, hy + 400),
              (cx + 108, hy + 436), (cx - 108, hy + 436), (cx - 172, hy + 400)],
             fill=WHITE, outline=NAVY, width=3)
    pen.poly([(cx - 172, hy + 344), (cx + 172, hy + 344), (cx + 172, hy + 400),
              (cx + 108, hy + 436), (cx - 108, hy + 436), (cx - 172, hy + 400)], fill=NAVY)
    # 领子 + 领结
    pen.poly([(cx - 96, hy + 176), (cx - 40, hy + 264), (cx + 40, hy + 264), (cx + 96, hy + 176)], fill=NAVY)
    pen.poly([(cx, hy + 226), (cx + 36, hy + 264), (cx, hy + 306), (cx - 36, hy + 264)],
             fill=(255, 146, 178), outline=(226, 106, 144), width=2)
    # 手臂：白袖子 + 手
    for sgn in (-1, 1):
        pen.capsule((cx + sgn * 112, hy + 200), (cx + sgn * 178, hy + 292), 31, WHITE)
        pen.capsule((cx + sgn * 178, hy + 292), (cx + sgn * 216, hy + 364), 25, SKIN)
    # 腿 + 脚
    for sgn in (-1, 1):
        pen.capsule((cx + sgn * 56, hy + 416), (cx + sgn * 62, hy + 512), 27, SKIN)
        pen.poly([(cx + sgn * 26, hy + 500), (cx + sgn * 92, hy + 514),
                  (cx + sgn * 98, hy + 548), (cx + sgn * 30, hy + 540)],
                 fill=TEAL, outline=TEAL_D, width=2)
    # 头
    pen.ellipse([cx - hr - 4, hy - hr, cx + hr + 4, hy + hr], fill=SKIN)
    # 侧发（先画，后面刘海盖住接缝）
    pen.ellipse([cx - 196, hy - 74, cx - 106, hy + 232], fill=HAIR)
    pen.ellipse([cx + 106, hy - 74, cx + 196, hy + 232], fill=HAIR)
    # 刘海：一个圆顶 + 三个圆弧发绺，全部压在眼睛上方
    pen.chord([cx - hr - 4, hy - hr - 4, cx + hr + 4, hy + hr - 30], 180, 360, HAIR)
    pen.circle(cx - 104, hy - 84, 58, fill=HAIR)
    pen.circle(cx - 6, hy - 70, 62, fill=HAIR)
    pen.circle(cx + 98, hy - 88, 56, fill=HAIR)
    # 头顶两片鲸鳍
    pen.poly([(cx - 152, hy - 108), (cx - 84, hy - 216), (cx - 42, hy - 92)], fill=TEAL, outline=TEAL_D, width=3)
    pen.poly([(cx + 152, hy - 114), (cx + 88, hy - 220), (cx + 42, hy - 96)], fill=TEAL, outline=TEAL_D, width=3)
    # 眼睛
    for ex in (cx - 64, cx + 64):
        pen.ellipse([ex - 32, hy + 6, ex + 32, hy + 90], fill=EYE)
        if pen.mode == "color":
            pen.d.ellipse([(ex - 17) * SS, (hy + 16) * SS, (ex + 5) * SS, (hy + 46) * SS],
                          fill=(255, 255, 255, 240))
            pen.d.ellipse([(ex + 8) * SS, (hy + 56) * SS, (ex + 21) * SS, (hy + 70) * SS],
                          fill=(255, 242, 248, 210))
    # 腮红
    for bx in (cx - 108, cx + 108):
        if pen.mode == "color":
            pen.d.ellipse([(bx - 36) * SS, (hy + 82) * SS, (bx + 36) * SS, (hy + 116) * SS],
                          fill=(255, 150, 180, 120))
        else:
            pen.ellipse([bx - 36, hy + 82, bx + 36, hy + 116], line_w=2.6)
    # 嘴
    pen.arc([cx - 24, hy + 92, cx + 24, hy + 132], 20, 160, (226, 106, 144), 5, line_w=3)
    # 头上的樱花
    if pen.mode == "color":
        sakura(pen.d, cx - 178, hy - 66, 34, (255, 206, 226, 245), 18, SS)
        pen.d.ellipse([(cx - 192) * SS, (hy - 80) * SS, (cx - 164) * SS, (hy - 52) * SS],
                      fill=(255, 236, 168, 255))
    else:
        for i in range(5):
            a = math.radians(18 + i * 72)
            pen.circle(cx - 178 + 30 * math.cos(a), hy - 66 + 30 * math.sin(a), 22, line_w=2.6)
    # 身边的水花/泡泡
    if pen.mode == "color":
        for (bx, by, r, a) in [(286, 760, 24, 135), (726, 712, 17, 120), (252, 918, 14, 105)]:
            pen.d.ellipse([(bx - r) * SS, (by - r) * SS, (bx + r) * SS, (by + r) * SS],
                          fill=(255, 255, 255, a // 4), outline=(255, 255, 255, a), width=int(2 * SS))
    else:
        for (bx, by, r, _a) in [(286, 760, 24, 0), (726, 712, 17, 0)]:
            pen.circle(bx, by, r, line_w=2.4)


def add_shading(base):
    """在彩图上叠一层柔和明暗，并用主体 alpha 裁一下，别糊到卡外。"""
    cx, hy = 512, 560.0
    shade = Image.new("RGBA", base.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shade)
    # 刘海投在脸上的阴影
    sd.ellipse([(cx - 146) * SS, (hy - 86) * SS, (cx + 146) * SS, (hy + 16) * SS], fill=(198, 136, 156, 62))
    # 后发下缘的暗部
    sd.ellipse([(cx - 190) * SS, (hy + 120) * SS, (cx + 190) * SS, (hy + 250) * SS], fill=(188, 92, 142, 80))
    # 领口 / 裙下的暗部
    sd.ellipse([(cx - 150) * SS, (hy + 250) * SS, (cx + 150) * SS, (hy + 360) * SS], fill=(150, 150, 190, 60))
    sd.ellipse([(cx - 180) * SS, (hy + 380) * SS, (cx + 180) * SS, (hy + 450) * SS], fill=(120, 130, 170, 60))
    shade = shade.filter(ImageFilter.GaussianBlur(24 * SS))

    hi = Image.new("RGBA", base.size, (0, 0, 0, 0))
    hd = ImageDraw.Draw(hi)
    # 头发高光 + 头顶反光
    hd.ellipse([(cx - 190) * SS, (hy - 200) * SS, (cx - 60) * SS, (hy - 100) * SS], fill=(255, 255, 255, 104))
    hd.ellipse([(cx + 76) * SS, (hy - 170) * SS, (cx + 180) * SS, (hy - 106) * SS], fill=(255, 255, 255, 74))
    hd.ellipse([(cx - 120) * SS, (hy + 210) * SS, (cx + 120) * SS, (hy + 300) * SS], fill=(255, 255, 255, 70))
    hi = hi.filter(ImageFilter.GaussianBlur(22 * SS))

    alpha = base.split()[3]
    for layer in (shade, hi):
        r, g, b, a = layer.split()
        layer.putalpha(ImageChops.multiply(a, alpha))
        base = Image.alpha_composite(base, layer)
    return base


def make_subject():
    img = Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0))
    pen = Pen(img, "color")
    draw_character(pen)
    img = add_shading(img)
    return img.resize((W, H), Image.LANCZOS)


def make_lineart():
    img = Image.new("RGBA", (W * SS, H * SS), (255, 255, 255, 255))
    pen = Pen(img, "line")
    draw_character(pen)
    return img.convert("RGB").resize((W, H), Image.LANCZOS)


# ---------------------------------------------------------------- 文字层

def make_text_layer(cfg):
    img = Image.new("RGBA", (W * SS, H * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)

    def rrect(box, radius, outline, width):
        d.rounded_rectangle([v * SS for v in box], radius=radius * SS, outline=outline, width=int(width * SS))

    # 卡框（跟着卡边走，不参与景深）
    rrect([26, 26, W - 26, H - 26], 30, (222, 190, 122, 205), 4)
    rrect([44, 44, W - 44, H - 44], 22, (255, 255, 255, 120), 2)

    f_small = font(30 * SS)
    f_title = font(126 * SS, bold=True)
    f_sub = font(56 * SS)
    f_tag = font(33 * SS)
    f_no = font(27 * SS)

    def put(xy, s, f, fill, anchor="mm", shadow=None, blur=6):
        x, y = xy[0] * SS, xy[1] * SS
        if shadow:
            layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
            ImageDraw.Draw(layer).text((x + 3 * SS, y + 4 * SS), s, font=f, fill=shadow, anchor=anchor)
            layer = layer.filter(ImageFilter.GaussianBlur(blur * SS))
            img.alpha_composite(layer)
        d.text((x, y), s, font=f, fill=fill, anchor=anchor)

    put((W / 2, 108), cfg["collection"], f_small, (28, 80, 100, 240))
    put((W / 2, 150), "—  WHALE ATELIER  —", f_no, (34, 94, 114, 195))
    put((W / 2, 194), cfg["technique"], f_no, (40, 100, 120, 175))

    put((W / 2, 1235), cfg["title"], f_title, (255, 252, 248, 255),
        shadow=(16, 52, 72, 150), blur=7)
    put((W / 2, 1332), cfg["subtitle"], f_sub, (255, 178, 210, 255),
        shadow=(16, 52, 72, 110), blur=5)
    put((W / 2, 1392), cfg["tagline"], f_tag, (230, 243, 250, 240))
    put((W / 2, 1450), cfg["edition"], f_no, (238, 224, 196, 240))

    return img.resize((W, H), Image.LANCZOS)


# ---------------------------------------------------------------- glTF

def perimeter(w, h, r, n=12):
    pts = []
    for cx, cy, start in [(w / 2 - r, h / 2 - r, 0), (-w / 2 + r, h / 2 - r, 90),
                          (-w / 2 + r, -h / 2 + r, 180), (w / 2 - r, -h / 2 + r, 270)]:
        for j in range(n + 1):
            a = math.radians(start + j * 90 / n)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


class GlbBuilder:
    def __init__(self):
        self.bin = bytearray()
        self.views = []
        self.accessors = []
        self.meshes = []
        self.nodes = []
        self.materials = {}

    def _view(self, data, target=None):
        off = len(self.bin)
        self.bin += data
        while len(self.bin) % 4:
            self.bin += b"\x00"
        view = {"buffer": 0, "byteOffset": off, "byteLength": len(data)}
        if target:
            view["target"] = target
        self.views.append(view)
        return len(self.views) - 1

    def accessor_vec3(self, arr, target):
        data = b"".join(struct.pack("<3f", *v) for v in arr)
        mn = [min(v[i] for v in arr) for i in range(3)]
        mx = [max(v[i] for v in arr) for i in range(3)]
        self.accessors.append({"bufferView": self._view(data, target), "componentType": 5126,
                               "count": len(arr), "type": "VEC3", "min": mn, "max": mx})
        return len(self.accessors) - 1

    def accessor_vec2(self, arr, target):
        data = b"".join(struct.pack("<2f", *v) for v in arr)
        self.accessors.append({"bufferView": self._view(data, target), "componentType": 5126,
                               "count": len(arr), "type": "VEC2"})
        return len(self.accessors) - 1

    def accessor_idx(self, arr):
        data = b"".join(struct.pack("<I", i) for i in arr)
        self.accessors.append({"bufferView": self._view(data, 34963), "componentType": 5125,
                               "count": len(arr), "type": "SCALAR"})
        return len(self.accessors) - 1

    def material(self, name):
        self.materials[name] = {"name": name, "doubleSided": False,
                                "pbrMetallicRoughness": {"baseColorFactor": [0.8, 0.8, 0.8, 1],
                                                          "metallicFactor": 0.0, "roughnessFactor": 0.7}}

    def mesh(self, name, poly_pts, uvs, z, indices=None, mat="web_front"):
        n = len(poly_pts)
        pos = [(x, y, z) for x, y in poly_pts]
        nrm = [(0.0, 0.0, 1.0)] * n
        if indices is None:
            indices = []
            for i in range(1, n - 1):
                indices += [0, i, i + 1]
        prim = {"attributes": {"POSITION": self.accessor_vec3(pos, 34962),
                               "NORMAL": self.accessor_vec3(nrm, 34962),
                               "TEXCOORD_0": self.accessor_vec2([(u, 1 - v) for u, v in uvs], 34962)},
                "indices": self.accessor_idx(indices),
                "material": list(self.materials).index(mat)}
        self.meshes.append({"name": name, "primitives": [prim]})
        self.nodes.append({"name": name, "mesh": len(self.meshes) - 1})

    def write(self, path):
        gltf = {
            "asset": {"version": "2.0", "generator": "holo-card/local"},
            "scene": 0,
            "scenes": [{"nodes": list(range(len(self.nodes)))}],
            "nodes": self.nodes,
            "meshes": self.meshes,
            "materials": list(self.materials.values()),
            "buffers": [{"byteLength": len(self.bin)}],
            "bufferViews": self.views,
            "accessors": self.accessors,
        }
        json_bytes = json.dumps(gltf, separators=(",", ":")).encode("utf8")
        while len(json_bytes) % 4:
            json_bytes += b" "
        bin_bytes = bytes(self.bin)
        total = 12 + 8 + len(json_bytes) + 8 + len(bin_bytes)
        with open(path, "wb") as fh:
            fh.write(struct.pack("<III", 0x46546C67, 2, total))
            fh.write(struct.pack("<II", len(json_bytes), 0x4E4F534A))
            fh.write(json_bytes)
            fh.write(struct.pack("<II", len(bin_bytes), 0x004E4942))
            fh.write(bin_bytes)


def build_glb(path):
    gb = GlbBuilder()
    for m in ("web_front", "web_back", "web_edge", "web_gold"):
        gb.material(m)

    outer = perimeter(CARD_W, CARD_H, CORNER)
    uv = [(x / CARD_W + 0.5, y / CARD_H + 0.5) for x, y in outer]
    # 正面（+Z）：绕序逆时针
    gb.mesh("front", outer, uv, 0.0, mat="web_front")
    # 背面（-Z）：反向绕序
    back = list(reversed(outer))
    gb.mesh("back", back, list(reversed(uv)), -CARD_T, mat="web_back")
    # 侧面
    n = len(outer)
    side_pos, side_uv, side_idx = [], [], []
    for i in range(n):
        j = (i + 1) % n
        base = len(side_pos)
        x0, y0 = outer[i]
        x1, y1 = outer[j]
        side_pos += [(x0, y0, 0.0), (x1, y1, 0.0), (x1, y1, -CARD_T), (x0, y0, -CARD_T)]
        side_uv += [(x0 / CARD_W + 0.5, y0 / CARD_H + 0.5), (x1 / CARD_W + 0.5, y1 / CARD_H + 0.5),
                    (x1 / CARD_W + 0.5, y1 / CARD_H + 0.5), (x0 / CARD_W + 0.5, y0 / CARD_H + 0.5)]
        side_idx += [base, base + 1, base + 2, base, base + 2, base + 3]
    prim_idx = gb.accessor_idx(side_idx)
    prim = {
        "attributes": {
            "POSITION": gb.accessor_vec3(side_pos, 34962),
            "NORMAL": gb.accessor_vec3([(0.0, 0.0, 1.0)] * len(side_pos), 34962),
            "TEXCOORD_0": gb.accessor_vec2([(u, 1 - v) for u, v in side_uv], 34962),
        },
        "indices": prim_idx,
        "material": list(gb.materials).index("web_edge"),
    }
    gb.meshes.append({"name": "edge", "primitives": [prim]})
    gb.nodes.append({"name": "edge", "mesh": len(gb.meshes) - 1})

    # 外圈 / 内圈压边
    for name, w, h, band, mat, z in (("ring-outer", CARD_W, CARD_H, 0.060, "web_edge", 0.025),
                                     ("ring-inner", CARD_W - 0.17, CARD_H - 0.17, 0.018, "web_gold", 0.026)):
        o = perimeter(w, h, 0.20)
        inner_r = max(0.01, 0.20 - band)
        i2 = perimeter(w - band * 2, h - band * 2, inner_r)
        pts = o + i2
        uvs = [(x / w + 0.5, y / h + 0.5) for x, y in pts]
        N = len(o)
        idx = []
        for i in range(N):
            j = (i + 1) % N
            idx += [i, j, N + j, i, N + j, N + i]
        prim = {
            "attributes": {
                "POSITION": gb.accessor_vec3([(x, y, z) for x, y in pts], 34962),
                "NORMAL": gb.accessor_vec3([(0.0, 0.0, 1.0)] * len(pts), 34962),
                "TEXCOORD_0": gb.accessor_vec2([(u, 1 - v) for u, v in uvs], 34962),
            },
            "indices": gb.accessor_idx(idx),
            "material": list(gb.materials).index(mat),
        }
        gb.meshes.append({"name": name, "primitives": [prim]})
        gb.nodes.append({"name": name, "mesh": len(gb.meshes) - 1})

    gb.write(path)


# ---------------------------------------------------------------- 组装单文件网页

def data_uri(raw, mime):
    return "data:%s;base64,%s" % (mime, base64.b64encode(raw).decode("ascii"))


def img_bytes(img, fmt, **kw):
    buf = io.BytesIO()
    img.save(buf, fmt, **kw)
    return buf.getvalue()


def build_page(vendor, out_html, assets, cfg):
    css = (vendor / "style.css").read_text(encoding="utf-8")
    bundle = (vendor / "app.bundle.js").read_text(encoding="utf-8")
    html = (vendor / "index.html").read_text(encoding="utf-8")
    bundle = bundle.replace("</script", "<\\/script")

    page_cfg = {
        "title": cfg["title"],
        "subtitle": cfg["subtitle"],
        "technique": cfg["technique"],
        "tagline": cfg["tagline"],
        "edition": cfg["edition"],
        "collection": cfg["collection"],
        "description": cfg["description"],
        "sourceMode": "composite",
        "assets": assets,
        "parameters": {"subjectScale": 1.0, "subjectDepth": 0.32, "backgroundDepth": -0.20, "foil": 0.62},
        "safeArea": {"scale": 1.0, "offset": [0, 0]},
        "appearance": {"background": "#fdf1f5", "finish": "pearl"},
        "artworkFit": [1, 1],
    }

    html = html.replace('<link rel="stylesheet" href="./style.css" />', "<style>\n%s\n</style>" % css)
    shim = (
        "<script>\nwindow.__HOLO_CONFIG__ = %s;\n"
        "(function(){var f=window.fetch;window.fetch=function(u,o){"
        "var s=String((u&&u.url)||u);if(s.indexOf('card-config.json')>=0){"
        "return Promise.resolve(new Response(JSON.stringify(window.__HOLO_CONFIG__),"
        "{status:200,headers:{'Content-Type':'application/json'}}));}"
        "return f.apply(this,arguments);};})();\n</script>\n"
    ) % json.dumps(page_cfg, ensure_ascii=False)
    tag = '<script type="module" src="./app.bundle.js"></script>'
    html = html.replace(tag, shim + '<script type="module">\n%s\n</script>' % bundle, 1)
    html = html.replace(tag, "")
    out_html.parent.mkdir(parents=True, exist_ok=True)
    out_html.write_text(html, encoding="utf-8")


# ---------------------------------------------------------------- main

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--vendor", required=True, help="含 style.css / app.bundle.js / index.html 的目录")
    ap.add_argument("--out", required=True, help="输出的单文件 HTML")
    ap.add_argument("--work", default=None, help="中间产物目录（四层图 + card.glb）")
    args = ap.parse_args()

    vendor = Path(args.vendor)
    work = Path(args.work) if args.work else Path(os.environ.get("TEMP", ".")) / "holo-card"
    work.mkdir(parents=True, exist_ok=True)

    cfg = {
        "title": "小鲸鱼娘",
        "subtitle": "樱花海",
        "technique": "泡泡冲击 · BUBBLE TIDE",
        "tagline": "吐个泡泡，把烦恼都卷走",
        "edition": "NO.001 / 001",
        "collection": "有花无实 · 全息典藏",
        "description": "深海与樱花交界的地方，她浮上来换了口气。",
    }

    print("· 画背景层")
    bg = make_background()
    print("· 画主体层")
    subj = make_subject()
    print("· 画线稿层")
    line = make_lineart()
    print("· 画文字层")
    text = make_text_layer(cfg)

    bg.save(work / "background.jpg", quality=90, optimize=True)
    subj.save(work / "subject.png", optimize=True)
    line.save(work / "lineart.jpg", quality=88, optimize=True)
    text.save(work / "text.png", optimize=True)
    print("· 造 card.glb")
    build_glb(work / "card.glb")

    assets = {
        "model": data_uri((work / "card.glb").read_bytes(), "model/gltf-binary"),
        "subject": data_uri((work / "subject.png").read_bytes(), "image/png"),
        "background": data_uri((work / "background.jpg").read_bytes(), "image/jpeg"),
        "lineart": data_uri((work / "lineart.jpg").read_bytes(), "image/jpeg"),
        "text": data_uri((work / "text.png").read_bytes(), "image/png"),
    }
    print("· 打包单文件网页")
    build_page(vendor, Path(args.out), assets, cfg)
    size = Path(args.out).stat().st_size
    print("完成：%s（%.2f MB）" % (args.out, size / 1024 / 1024))
    print("中间产物在：%s" % work)


if __name__ == "__main__":
    main()
