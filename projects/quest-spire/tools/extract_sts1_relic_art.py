"""从杀戮尖塔1 的官方素材里抠出一代遗物原画，放进 mod 的 pck 目录。

STS1 的 jar 里每件遗物有三套图：
  images/relics/<名字>.png          小图标（游戏里显示的那个）
  images/relics/outline/<名字>.png  轮廓（选中/发光用）
  images/largeRelics/<名字>.png     大插图（不是每件都有）

这些是 Mega Crit 的官方美术。自己玩没问题；**要公开发布得注意版权**。
"""
import os
import shutil
import zipfile

try:
    from PIL import Image
    HAS_PIL = True
except ImportError:
    HAS_PIL = False

JAR = r"D:\software\steam\steamapps\common\SlayTheSpire\desktop-1.0.jar"
OUT = r"C:\Users\有花无实\Documents\Default Project\projects\quest-spire\QuestSpire\images\relics"

# 我们的文件名 -> STS1 里的名字
MAP = {
    "dead_branch": "deadBranch",
    "torii": "torii",
    "coffee_dripper": "coffeeDripper",
    "ornithopter": "ornithopter",
    # 第二批（2026-09-22 深夜十一批）：官方允许一代素材用在二代，所以把剩下能做的也补上
    "golden_idol": "goldenIdolRelic",
    "omamori": "omamori",
    "clockwork": "clockwork",
    # 注：机械臂(inserter) / 套娃(matryoshka) 的图也有一代原画，
    # 但那两件的效果我们还没找到干净接口，所以先不抠，等接口确认了再加。
}

os.makedirs(OUT, exist_ok=True)
z = zipfile.ZipFile(JAR)
names = set(z.namelist())


def grab(member, dest):
    with z.open(member) as src, open(dest, "wb") as dst:
        shutil.copyfileobj(src, dst)
    return dest


def size_of(path):
    if HAS_PIL:
        with Image.open(path) as im:
            return im.size
    return None


print("Pillow 可用:", HAS_PIL)
for ours, their in MAP.items():
    got = []
    small = "images/relics/%s.png" % their
    outline = "images/relics/outline/%s.png" % their
    large = "images/largeRelics/%s.png" % their

    if small in names:
        p = grab(small, os.path.join(OUT, ours + ".png"))
        got.append(("小图标", size_of(p)))
    if outline in names:
        p = grab(outline, os.path.join(OUT, ours + "_outline.png"))
        got.append(("轮廓", size_of(p)))
    if large in names:
        p = grab(large, os.path.join(OUT, ours + "_big.png"))
        got.append(("大图", size_of(p)))

    print("%-14s %s" % (ours, got))

print("\n放到了:", OUT)
