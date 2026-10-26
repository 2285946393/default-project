"""把 MemeSpire 打包成官方上传器要的工作区（照 QuestSpire 的来）。

用法：
    python tools/prepare_workshop.py
然后：
    cd /d D:\software\sts2-mod-uploader
    ModUploader.exe upload -w D:\software\sts2-mod-uploader\MemeSpire
"""
import json
import os
import shutil

from PIL import Image, ImageDraw, ImageFont

PROJ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WS_SRC = os.path.join(PROJ, "workshop")
GAME_MODS = r"D:\software\steam\steamapps\common\Slay the Spire 2\mods\MemeSpire"
UPLOADER = r"D:\software\sts2-mod-uploader"
WORKSPACE = os.path.join(UPLOADER, "MemeSpire")
COVER = os.path.join(WS_SRC, "cover.png")

FONTS = [
    (r"C:\Windows\Fonts\msyhbd.ttc", r"C:\Windows\Fonts\msyh.ttc"),
    (r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\msyh.ttc"),
    (r"C:\Windows\Fonts\simhei.ttf", r"C:\Windows\Fonts\simhei.ttf"),
]


def pick_font(size, bold=True):
    for pair in FONTS:
        p = pair[0] if bold else pair[1]
        if os.path.exists(p):
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def make_cover():
    """没现成封面就画一张：深紫渐变 + 标题 + 示例梗。"""
    W, H = 1600, 900
    im = Image.new("RGB", (W, H))
    d = ImageDraw.Draw(im)
    for y in range(H):
        t = y / H
        r = int(24 + 40 * (1 - t)); g = int(12 + 20 * (1 - t)); b = int(48 + 70 * (1 - t))
        d.line([(0, y), (W, y)], fill=(r, g, b))

    d.text((90, 120), "梗化尖塔", font=pick_font(180), fill=(255, 220, 90))
    d.text((96, 330), "Meme Spire", font=pick_font(84), fill=(240, 240, 255))
    d.text((96, 440), "全网梗 · 全文本改名 · 3000+ 条", font=pick_font(52, False), fill=(200, 200, 220))

    lines = [
        "痛击 → 一个大逼斗     愤怒 → 急了急了",
        "预借时间 → 花呗       双重释放 → 双卡双待",
        "超能光束 → 动感光波   电击 → 原神，启动！",
        "机器学习 → 张雪峰狂喜 谋杀 → 法外狂徒",
        "铁甲战士 → 红裤衩     故障机器人 → 机宝",
    ]
    f = pick_font(46, False)
    y = 560
    for ln in lines:
        d.text((96, y), ln, font=f, fill=(255, 255, 255))
        y += 62

    for (tw, th) in [(1600, 900)]:
        im.resize((tw, th), Image.LANCZOS).save(COVER, "PNG", optimize=True)
    print("封面生成：%dx%d，%.0f KB" % (W, H, os.path.getsize(COVER) / 1024))


def copy_content():
    dst = os.path.join(WORKSPACE, "content")
    os.makedirs(dst, exist_ok=True)
    if not os.path.isdir(GAME_MODS):
        raise SystemExit("找不到已部署的 mod：%s（先跑 tools/build_pck.ps1 并拷进游戏 mods）" % GAME_MODS)
    picked = []
    for f in ("MemeSpire.json", "MemeSpire.pck", "README.md"):
        src = os.path.join(GAME_MODS, f)
        if os.path.exists(src):
            shutil.copy2(src, os.path.join(dst, f))
            picked.append("%s(%.0fKB)" % (f, os.path.getsize(src) / 1024))
    print("content/：" + "、".join(picked))


def write_workshop_json():
    meta = json.load(open(os.path.join(WS_SRC, "meta.json"), encoding="utf-8"))
    desc = open(os.path.join(WS_SRC, "description.md"), encoding="utf-8").read().strip()
    out = {
        "title": meta["title"],
        "description": desc,
        "visibility": meta.get("visibility", "private"),
        "changeNote": meta.get("changeNote", ""),
        "tags": meta.get("tags", []),
        "dependencies": meta.get("dependencies", []),
        "contentDescriptors": meta.get("contentDescriptors", []),
    }
    p = os.path.join(WORKSPACE, "workshop.json")
    with open(p, "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=2)
    nb = len(desc.encode("utf-8"))
    print("workshop.json：title=%s / visibility=%s / tags=%s" % (out["title"], out["visibility"], out["tags"]))
    print("  说明文 %d 字 / %d 字节（上限 8000）" % (len(desc), nb))
    if nb > 8000:
        print("  [!] 超了 8000 字节，可能被拒！")


def make_image():
    """封面输出为 image.png（内容用 JPEG 编码——画作类图 PNG 会很大且减色会糊/带色带）。
    文件名必须是 image.png（上传器要求），但字节是 JPEG，Steam 认内容不认扩展名。"""
    if not os.path.exists(COVER):
        make_cover()
    dst = os.path.join(WORKSPACE, "image.png")
    im = Image.open(COVER).convert("RGB")
    w, h = im.size
    for tw in (2560, 2200, 1920, 1600, 1440, 1280, 1024):
        if tw > w:
            continue
        out = im.resize((tw, max(1, round(tw * h / w))), Image.LANCZOS)
        out.save(dst, "JPEG", quality=92, optimize=True, progressive=True)
        kb = os.path.getsize(dst) / 1024
        if kb < 950:
            print("image.png（JPEG 编码）：%dx%d，%.0f KB" % (out.size[0], out.size[1], kb))
            return
    print("[!] 压不到 1MB，请自己换图")


def main():
    os.makedirs(WORKSPACE, exist_ok=True)
    copy_content()
    write_workshop_json()
    make_image()
    print("\n工作区: %s" % WORKSPACE)
    print('上传： cd "%s" && ./ModUploader.exe upload -w "%s"' % (UPLOADER, WORKSPACE))


if __name__ == "__main__":
    main()
