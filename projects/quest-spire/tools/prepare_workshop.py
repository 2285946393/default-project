"""把 mod 打包成官方上传器要的工作区。

官方工具是 Mega Crit 自己出的：https://github.com/megacrit/sts2-mod-uploader
它的规矩（见它的 template/README.md）：
    工作区/
      ├─ content/        ← 要上传的 mod 文件（dll + json + pck）
      ├─ workshop.json   ← 标题 / 说明 / 可见性 / 标签 / 依赖
      ├─ image.png       ← 工坊封面图，**必须小于 1MB**
      └─ previews/       ← 可选，额外预览图，也 <1MB

本脚本做三件事：
  1. 从游戏 mods 目录（或本地构建）拷 mod 文件进 content/
  2. 读 workshop/meta.json + workshop/description.md，生成 workshop.json
  3. 从截图生成 image.png（自动压到 1MB 以内）

用法：
    python tools/prepare_workshop.py
然后到上传器目录执行：
    ModUploader.exe upload -w <工作区目录>
"""
import json
import os
import shutil
import sys

from PIL import Image

PROJ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WS_SRC = os.path.join(PROJ, "workshop")
GAME_MODS = r"D:\software\steam\steamapps\common\Slay the Spire 2\mods\QuestSpire"
UPLOADER = r"D:\software\sts2-mod-uploader"
WORKSPACE = os.path.join(UPLOADER, "QuestSpire")

# 工坊封面。现在用的是用户给的那张画（任务册 + 遗物），放在 workshop/cover.png。
# 换图就换这个文件；横图竖图都行，下面的 make_image 会按比例塞进 1MB。
SHOT = os.path.join(WS_SRC, "cover.png")


def copy_content():
    dst = os.path.join(WORKSPACE, "content")
    os.makedirs(dst, exist_ok=True)
    if not os.path.isdir(GAME_MODS):
        sys.exit("找不到已部署的 mod：%s（先 dotnet build）" % GAME_MODS)
    picked = []
    for f in ("QuestSpire.dll", "QuestSpire.json", "QuestSpire.pck"):
        src = os.path.join(GAME_MODS, f)
        if os.path.exists(src):
            shutil.copy2(src, os.path.join(dst, f))
            picked.append("%s(%.0f KB)" % (f, os.path.getsize(src) / 1024))
    # 源文件也放一份，方便别人看
    for extra in ("description.md",):
        p = os.path.join(WS_SRC, extra)
        if os.path.exists(p):
            shutil.copy2(p, os.path.join(dst, extra))
    print("content/ 放好了：" + "、".join(picked))


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
    # ensure_ascii=False：中文直接写进去，上传器读的是 UTF-8
    with open(p, "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=2)
    print("workshop.json 写好了：title=%s / visibility=%s / tags=%s / deps=%s"
          % (out["title"], out["visibility"], out["tags"], out["dependencies"]))
    # ⚠️ 上限是**字节**不是字数：超过 8000 字节上传器只会回一句
    #    k_EResultInvalidParam，看不出是说明文的锅（2026-10-04 踩过：8184 字节被拒）。
    nb = len(desc.encode("utf-8"))
    print("  说明文 %d 字 / %d 字节（上限 8000 字节）" % (len(desc), nb))
    if nb > 8000:
        print("  [!] 超了！精简一下 description.md，否则上传会被拒。")


def make_image():
    if not os.path.exists(SHOT):
        print("[!] 找不到封面 %s，请自己放一张 image.png（<1MB）" % SHOT)
        return
    dst = os.path.join(WORKSPACE, "image.png")

    # 源图本身就已经 < 950KB 的话直接用（自己裁好的版本往往比这里重压的更好）
    if SHOT.lower().endswith(".png") and os.path.getsize(SHOT) < 950 * 1024:
        shutil.copy2(SHOT, dst)
        im0 = Image.open(dst)
        print("image.png 直接用源图：%dx%d，%.0f KB" % (im0.size[0], im0.size[1], os.path.getsize(dst) / 1024))
        return

    im = Image.open(SHOT).convert("RGB")
    w, h = im.size

    # 横图：按老规矩裁成 16:9（工坊页最上面那块是宽幅的）。
    # 竖图：**不裁**，保持原比例缩小（用户给的这张画是竖的，裁了就没了）。
    if w >= h:
        target_h = int(w * 9 / 16)
        if target_h <= h:
            im = im.crop((0, 0, w, target_h))
        sizes = [(1600, int(1600 * 9 / 16)), (1280, int(1280 * 9 / 16)), (1024, int(1024 * 9 / 16))]
    else:
        sizes = [(1024, int(1024 * h / w)), (900, int(900 * h / w)), (800, int(800 * h / w))]

    for (tw, th) in sizes:
        out = im.resize((tw, th), Image.LANCZOS)
        out.save(dst, "PNG", optimize=True)
        kb = os.path.getsize(dst) / 1024
        if kb < 950:
            print("image.png 生成：%dx%d，%.0f KB（工坊上限 1MB）" % (tw, th, kb))
            return

    # 还是太大就减色：**先保住分辨率**，别一路降尺寸（暖色油画 256 色看不出来）
    for (tw, th) in sizes:
        out = im.resize((tw, th), Image.LANCZOS).quantize(colors=256)
        out.save(dst, "PNG", optimize=True)
        kb = os.path.getsize(dst) / 1024
        if kb < 950:
            print("image.png 生成（减色版）：%dx%d，%.0f KB" % (tw, th, kb))
            return
    print("[!] 封面压不到 1MB 以内，请自己换一张")


def main():
    os.makedirs(WORKSPACE, exist_ok=True)
    copy_content()
    write_workshop_json()
    make_image()
    print("\n工作区: %s" % WORKSPACE)
    print("下一步（要 Steam 开着并登录你自己的账号）：")
    print('  cd "%s"' % UPLOADER)
    print('  .\\ModUploader.exe upload -w "%s"' % WORKSPACE)
    print("上传后目录里会有 mod_id.txt（记住它，以后更新要用）和 mod-uploader.log。")


if __name__ == "__main__":
    main()
