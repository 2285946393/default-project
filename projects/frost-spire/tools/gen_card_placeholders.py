# -*- coding: utf-8 -*-
"""
生成凛冬尖塔 8 张卡面的**占位图**（真美术出来前，用来验证 pck 链路 + 卡面文件名推导）。

风格跟 assets/生成提示词.md 的冰雪色板一致：
    冷钢 #4A5A6B / 霜蓝 #2B4A6B / 冰川青 #14494E / 暗铁 #5A5148
    骨白 #E6EAEF / 霜金 #D9B96A / 墨线 #15161A

用法：
    python tools/gen_card_placeholders.py
输出：
    FrostSpire/images/card_art/*.png   （256x190，与游戏卡面比例接近）
之后必须：
    1) 跑 tools/build_pck.ps1  重新打 pck
    2) dotnet build            部署到 mods/
"""
import math
import os

from PIL import Image, ImageDraw, ImageFont

W, H = 256, 190
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "FrostSpire", "images", "card_art")
OUT = os.path.normpath(OUT)

FROST = (43, 74, 107)      # 霜蓝
FROST_DARK = (22, 40, 60)  # 更深的蓝（渐变底部）
STEEL = (74, 90, 107)      # 冷钢
IRON = (90, 81, 72)        # 暗铁
ICE = (20, 73, 78)         # 冰川青
BONE = (230, 234, 239)     # 骨白
GOLD = (217, 185, 106)     # 霜金
INK = (21, 22, 26)         # 墨线

FONT_BOLD = "C:/Windows/Fonts/arialbd.ttf"
FONT_REG = "C:/Windows/Fonts/arial.ttf"


def base_canvas():
    """霜蓝渐变底 + 45° 斜纹 + 双层边框。"""
    img = Image.new("RGB", (W, H), FROST_DARK)
    d = ImageDraw.Draw(img)

    # 垂直渐变：顶霜蓝 → 底深蓝
    for y in range(H):
        t = y / (H - 1)
        c = tuple(int(FROST[i] * (1 - t) + FROST_DARK[i] * t) for i in range(3))
        d.line([(0, y), (W, y)], fill=c)

    # 45° 斜纹（每 14px 一条，比底色亮一点点）
    stripe = tuple(min(255, c + 12) for c in FROST_DARK)
    for x in range(-H, W, 14):
        d.line([(x, H), (x + H, 0)], fill=stripe, width=2)

    # 外框：骨白 3px；内框：墨线 1px
    d.rectangle([0, 0, W - 1, H - 1], outline=BONE, width=3)
    d.rectangle([5, 5, W - 6, H - 6], outline=INK, width=1)
    return img, d


def draw_label(d, text, sub=None, gold=False):
    """底部卡名（英文）+ 可选副标题。"""
    col = GOLD if gold else BONE
    f = ImageFont.truetype(FONT_BOLD, 17)
    tw = d.textlength(text, font=f)
    d.text(((W - tw) / 2, H - 34), text, font=f, fill=col)
    if sub:
        fs = ImageFont.truetype(FONT_REG, 12)
        sw = d.textlength(sub, font=fs)
        d.text(((W - sw) / 2, H - 16), sub, font=fs, fill=(180, 190, 200))


def icon_shield(d, cx, cy, fill=IRON, edge=BONE, gold=False, plus=False):
    """盾牌。plus=True 时加金色十字（表示升级版）。"""
    pts = [(cx - 46, cy - 52), (cx + 46, cy - 52), (cx + 46, cy + 6),
           (cx, cy + 56), (cx - 46, cy + 6)]
    d.polygon(pts, fill=fill, outline=edge)
    d.line(pts + [pts[0]], fill=edge, width=3)
    if gold:
        d.line([(cx - 46, cy - 52), (cx + 46, cy - 52)], fill=GOLD, width=3)
    if plus:
        d.line([(cx, cy - 26), (cx, cy + 22)], fill=GOLD, width=7)
        d.line([(cx - 22, cy - 2), (cx + 22, cy - 2)], fill=GOLD, width=7)


def icon_tower(d, cx, cy, crenels=True, window=True, banner=False):
    """箭塔/瞭望塔：梯形塔身 + 顶部垛口。"""
    body = [(cx - 30, cy + 50), (cx + 30, cy + 50), (cx + 20, cy - 30), (cx - 20, cy - 30)]
    d.polygon(body, fill=STEEL, outline=INK)
    d.line(body + [body[0]], fill=BONE, width=3)
    # 垛口
    if crenels:
        top = cy - 52
        for i in range(-2, 3):
            x0 = cx + i * 14 - 5
            d.rectangle([x0, top, x0 + 9, cy - 30], fill=STEEL, outline=BONE, width=2)
    if window:
        d.rectangle([cx - 6, cy - 8, cx + 6, cy + 12], fill=INK)
    if banner:
        d.polygon([(cx, cy - 66), (cx + 16, cy - 58), (cx, cy - 50)], fill=GOLD, outline=INK)


def icon_arrow(d, cx, cy, ang=-35):
    """一支箭。"""
    rad = math.radians(ang)
    dx, dy = math.cos(rad), math.sin(rad)
    x0, y0 = cx - dx * 44, cy - dy * 44
    x1, y1 = cx + dx * 44, cy + dy * 44
    d.line([(x0, y0), (x1, y1)], fill=BONE, width=4)
    # 箭头（两条短线）
    for off in (150, -150):
        a = math.radians(ang + off)
        d.line([(x1, y1), (x1 + math.cos(a) * 16, y1 + math.sin(a) * 16)], fill=BONE, width=4)
    # 尾羽
    for off in (150, -150):
        a = math.radians(ang + off)
        d.line([(x0, y0), (x0 + math.cos(a) * 13, y0 + math.sin(a) * 13)], fill=GOLD, width=3)


def icon_militia(d, cx, cy):
    """民兵：圆头 + 身体 + 长矛。"""
    d.ellipse([cx - 15, cy - 46, cx + 15, cy - 16], fill=IRON, outline=BONE, width=3)
    body = [(cx - 22, cy + 50), (cx + 22, cy + 50), (cx + 16, cy - 12), (cx - 16, cy - 12)]
    d.polygon(body, fill=STEEL, outline=BONE)
    d.line(body + [body[0]], fill=BONE, width=3)
    # 长矛（偏右，避免跟头部重叠成「天线」）
    d.line([(cx + 42, cy + 56), (cx + 22, cy - 54)], fill=IRON, width=5)
    d.polygon([(cx + 22, cy - 54), (cx + 29, cy - 42), (cx + 15, cy - 42)], fill=BONE, outline=INK)


def icon_wall(d, cx, cy):
    """冰墙：错缝砖块。"""
    bw, bh = 46, 24
    for row in range(3):
        y = cy - 42 + row * (bh + 4)
        offset = -bw // 2 if row % 2 else 0
        for col in range(-1, 3):
            x = cx + offset + col * (bw + 4) - bw
            d.rectangle([x, y, x + bw, y + bh], fill=ICE, outline=BONE, width=3)
            d.line([(x + 4, y + 5), (x + bw - 4, y + 5)], fill=(90, 150, 155), width=1)


def icon_campfire(d, cx, cy):
    """火堆：交叉木柴 + 火焰。"""
    d.line([(cx - 42, cy + 46), (cx + 42, cy + 20)], fill=IRON, width=10)
    d.line([(cx + 42, cy + 46), (cx - 42, cy + 20)], fill=IRON, width=10)
    d.line([(cx - 42, cy + 46), (cx + 42, cy + 20)], fill=BONE, width=2)
    d.line([(cx + 42, cy + 46), (cx - 42, cy + 20)], fill=BONE, width=2)
    flame = [(cx, cy - 56), (cx + 26, cy - 4), (cx + 14, cy + 20), (cx - 14, cy + 20), (cx - 26, cy - 4)]
    d.polygon(flame, fill=(200, 96, 48), outline=BONE)
    inner = [(cx, cy - 30), (cx + 13, cy - 2), (cx - 13, cy - 2)]
    d.polygon(inner, fill=GOLD, outline=None)


# ---- 每张卡面的图标画法 ----

def art_ice_bulwark(d, cx, cy):
    icon_shield(d, cx, cy)


def art_ice_bulwark_back(d, cx, cy):
    icon_shield(d, cx, cy, fill=STEEL, gold=True, plus=True)


def art_arrow_tower(d, cx, cy):
    icon_tower(d, cx, cy, banner=True)
    icon_arrow(d, cx + 34, cy + 30, ang=-42)


def art_summon_militia(d, cx, cy):
    icon_militia(d, cx, cy)


def art_ice_wall(d, cx, cy):
    icon_wall(d, cx, cy)


def art_arrow_tower_building(d, cx, cy):
    icon_tower(d, cx, cy, window=True)


def art_campfire(d, cx, cy):
    icon_campfire(d, cx, cy)


def art_watchtower(d, cx, cy):
    icon_tower(d, cx, cy, window=True, banner=False)


# 卡面清单：文件名 → (画法, 卡名, 副标题, 是否金色)
# ★ 文件名必须与 FrostCardArt.EntryOf(类名) 一致，否则静默回退默认卡面！
CARDS = [
    ("ice_bulwark",          art_ice_bulwark,          "ICE BULWARK",   "Deploy",   False),
    ("ice_bulwark_back",     art_ice_bulwark_back,     "ICE BULWARK+",  "Upgraded", True),
    ("arrow_tower",          art_arrow_tower,          "ARROW TOWER",   "Deploy",   False),
    ("summon_militia",       art_summon_militia,       "SUMMON MILITIA", "Deploy",  False),
    ("ice_wall_building",    art_ice_wall,             "ICE WALL",      "Building", False),
    ("arrow_tower_building", art_arrow_tower_building, "ARROW TOWER",   "Building", False),
    ("campfire_building",    art_campfire,             "CAMPFIRE",      "Building", False),
    ("watchtower_building",  art_watchtower,           "WATCHTOWER",    "Building", False),
]


def main():
    os.makedirs(OUT, exist_ok=True)
    cx, cy = W // 2, 74
    for name, draw_icon, label, sub, gold in CARDS:
        img, d = base_canvas()
        draw_icon(d, cx, cy)
        draw_label(d, label, sub, gold=gold)
        path = os.path.join(OUT, name + ".png")
        img.save(path)
        print("生成", path)
    print("共 %d 张 → %s" % (len(CARDS), OUT))


if __name__ == "__main__":
    main()
