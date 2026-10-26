# -*- coding: utf-8 -*-
"""生成「Codex 人格切换」的桌面图标：浅蓝圆角底 + 鲸鱼娘 + 小爱心。"""
import os
from PIL import Image, ImageDraw

OUT_DIR = "D:/work/AI产出/Codex人格切换"
S = 512
img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# 圆角底：浅蓝 -> 淡粉 的竖渐变
pad, radius = 16, 116
grad = Image.new("RGBA", (S, S), (0, 0, 0, 0))
gd = ImageDraw.Draw(grad)
top, bot = (217, 236, 255), (255, 226, 241)
for y in range(S):
    t = y / (S - 1)
    c = tuple(int(top[i] + (bot[i] - top[i]) * t) for i in range(3)) + (255,)
    gd.line([(0, y), (S, y)], fill=c)
mask = Image.new("L", (S, S), 0)
ImageDraw.Draw(mask).rounded_rectangle([pad, pad, S - pad, S - pad], radius=radius, fill=255)
img.paste(grad, (0, 0), mask)

# 外框
d.rounded_rectangle([pad, pad, S - pad, S - pad], radius=radius,
                    outline=(255, 163, 205, 255), width=18)

BLUE = (74, 158, 224, 255)
BLUE_D = (52, 128, 194, 255)
WHITE = (255, 255, 255, 255)
INK = (40, 52, 78, 255)
PINK = (255, 150, 190, 255)

# 尾巴
d.polygon([(432, 292), (472, 236), (466, 348)], fill=BLUE_D)
# 身体
d.ellipse([96, 234, 448, 424], fill=BLUE)
# 肚子
d.ellipse([140, 330, 420, 420], fill=WHITE)
# 鲸鳍
d.polygon([(206, 330), (244, 386), (170, 372)], fill=BLUE_D)
# 眼睛
d.ellipse([168, 272, 204, 312], fill=INK)
d.ellipse([178, 280, 192, 296], fill=WHITE)
# 腮红
d.ellipse([132, 330, 176, 358], fill=(255, 178, 206, 255))
# 喷水
d.ellipse([224, 156, 254, 190], fill=(140, 200, 246, 255))
d.ellipse([262, 122, 296, 162], fill=(140, 200, 246, 255))
d.ellipse([306, 100, 338, 138], fill=(140, 200, 246, 255))

# 右上角小爱心
def heart(cx, cy, r, fill):
    d.ellipse([cx - r, cy - r, cx, cy], fill=fill)
    d.ellipse([cx, cy - r, cx + r, cy], fill=fill)
    d.polygon([(cx - r, cy - r * 0.28), (cx + r, cy - r * 0.28), (cx, cy + r)], fill=fill)

heart(384, 148, 46, PINK)
heart(384, 148, 30, (255, 255, 255, 190))

os.makedirs(OUT_DIR, exist_ok=True)
png = os.path.join(OUT_DIR, "图标预览.png")
img.save(png)
ico = os.path.join(OUT_DIR, "图标.ico")
img.save(ico, sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
print("PNG:", png, os.path.getsize(png), "字节")
print("ICO:", ico, os.path.getsize(ico), "字节")
