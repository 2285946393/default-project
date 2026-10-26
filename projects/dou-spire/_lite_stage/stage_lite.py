# -*- coding: utf-8 -*-
"""组装 DouSpireLite 的工坊上传工作区（一次性）。
来源：projects/dou-spire/_lite_stage/{DouSpireLite.json, DouSpireLite.dll, description.md}
     + 完整版 assets（剔除客串角色与制作素材）+ workshop/cover.png 做封面。
产出：D:/software/sts2-mod-uploader/DouSpireLite/{content/, workshop.json, image.png}
"""
import json
import os
import shutil
import sys

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

PROJ = r"C:\Users\有花无实\Documents\Default Project\projects\dou-spire"
STAGE = os.path.join(PROJ, "_lite_stage")
GAME_MODS = r"D:\software\steam\steamapps\common\Slay the Spire 2\mods\DouSpire"
UPLOADER = r"D:\software\sts2-mod-uploader"
WORKSPACE = os.path.join(UPLOADER, "DouSpireLite")
COVER = os.path.join(PROJ, "workshop", "cover.png")

SKIP_DIRS = {"_orig",                    # 制作原图（55MB）
             "whale", "chun", "yuki", "sherry"}   # 客串角色素材（在 assets/ 顶层）
SKIP_FILES = {"voice-log.txt", "voice-log.txt.bak", "DouSpire.json", "DouSpire.dll"}
SKIP_EXTS = {".wav", ".md"}
GUEST_VOICE = {"whale", "chun", "yuki", "sherry"}


def copy_content():
    dst = os.path.join(WORKSPACE, "content")
    if os.path.isdir(dst):
        shutil.rmtree(dst)
    os.makedirs(dst, exist_ok=True)

    # manifest 与 dll（来自 _lite_stage）
    shutil.copy2(os.path.join(STAGE, "DouSpireLite.json"), dst)
    shutil.copy2(os.path.join(STAGE, "DouSpireLite.dll"), dst)
    print("manifest + dll：DouSpireLite.json / DouSpireLite.dll")

    # assets（从**已部署的完整版**拷，剔除客串）
    src = os.path.join(GAME_MODS, "assets")
    n, size = 0, 0
    for root, dirs, files in os.walk(src):
        rel = os.path.relpath(root, src)
        top = rel.split(os.sep)[0] if rel != "." else ""
        # 剪掉客串角色目录（assets 顶层的 whale/chun/yuki/sherry）
        if top in SKIP_DIRS:
            dirs[:] = []
            continue
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS]
        for f in files:
            if f in SKIP_FILES or os.path.splitext(f)[1].lower() in SKIP_EXTS:
                continue
            # voice/ 下的客串语音目录
            if top == "voice" and rel != "voice":
                sub = os.path.relpath(root, src).split(os.sep)[1]
                if sub in GUEST_VOICE:
                    continue
            s = os.path.join(root, f)
            t = os.path.join(dst, "assets", os.path.relpath(s, src))
            os.makedirs(os.path.dirname(t), exist_ok=True)
            shutil.copy2(s, t)
            n += 1
            size += os.path.getsize(s)
    print("assets：%d 个文件，%.0fMB" % (n, size / 1048576))


def write_workshop_json():
    desc = open(os.path.join(STAGE, "description.md"), encoding="utf-8").read().strip()
    out = {
        "title": "🀄 斗地主尖塔·简洁版｜纯玩法无二次元角色 Slay the Spire 2",
        "description": desc,
        "visibility": "public",
        "changeNote": "1.1.0：同步完整版 0.2.33 全部更新 —— 四人 2v2 组队斗地主正式上线（对家是队友、两副牌、猜拳定先手、后手队扣底牌、按队结算）；炸弹/王炸/定地主/蓄力全套本体特效可见化；AI 明牌改看手牌（牌好才明，不再无脑全明）；修超时托管死循环刷屏、终局按钮不出现、打完的局重开卡死；铁甲战士与故障机器人语音包扩充（含 2v2 队友报单专属语音）。",
        "tags": ["Cards", "gameplay", "schinese", "english", "Doudizhu", "Poker", "Minigame", "UI"],
        "dependencies": [3747602295, 3737335127],
        "contentDescriptors": [],
    }
    with open(os.path.join(WORKSPACE, "workshop.json"), "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=2)
    nb = len(desc.encode("utf-8"))
    tb = len(out["title"].encode("utf-8"))
    print("workshop.json：title=%d 字节（上限128）/ 说明 %d 字节（上限8000）" % (tb, nb))
    assert tb <= 128, "标题超 128 字节！"
    assert nb <= 8000, "说明超 8000 字节！"


def make_image():
    from PIL import Image
    dst = os.path.join(WORKSPACE, "image.png")
    im = Image.open(COVER).convert("RGB")
    w, h = im.size
    for tw in (2560, 2200, 1920, 1600, 1440, 1280, 1024):
        if tw > w:
            continue
        out = im.resize((tw, max(1, round(tw * h / w))), Image.LANCZOS)
        out.save(dst, "JPEG", quality=92, optimize=True, progressive=True)
        if os.path.getsize(dst) < 950 * 1024:
            print("image.png：%dx%d，%.0f KB" % (out.size[0], out.size[1], os.path.getsize(dst) / 1024))
            return
    print("[!] 封面压不到 1MB")


def main():
    os.makedirs(WORKSPACE, exist_ok=True)
    copy_content()
    write_workshop_json()
    make_image()
    print("\n工作区: %s" % WORKSPACE)
    print('上传： cd "%s" && ./ModUploader.exe upload -w "%s"' % (UPLOADER, WORKSPACE))


if __name__ == "__main__":
    main()
