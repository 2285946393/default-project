# -*- coding: utf-8 -*-
"""把 DouSpire 打包成官方上传器要的工作区（照 QuestSpire / MemeSpire 那套来）。

用法：
    python tools/prepare_workshop.py
然后：
    cd /d D:\\software\\sts2-mod-uploader
    ModUploader.exe upload -w D:\\software\\sts2-mod-uploader\\DouSpire

跟梗化尖塔那套的区别：斗地主是 **has_pck: false** —— 素材全在 `assets/` 目录里按文件系统读，
所以 content/ 里要连 `assets/` 整个拷过去（**除了 voice-log.txt，那是本地调试日志，不能传**）。
"""
import json
import os
import shutil
import sys
import time

# Windows 控制台默认 GBK，标题里那个 emoji 一 print 就崩 —— 先把输出切成 UTF-8
try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass

PROJ = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
WS_SRC = os.path.join(PROJ, "workshop")
GAME_MODS = r"D:\software\steam\steamapps\common\Slay the Spire 2\mods\DouSpire"
UPLOADER = r"D:\software\sts2-mod-uploader"
WORKSPACE = os.path.join(UPLOADER, "DouSpire")
COVER = os.path.join(WS_SRC, "cover.png")

# 包里要带的东西（**不含 voice-log.txt**）
# 2026-10-09 加：DouZero AI 的 ONNX 模型（onnx/）+ ONNX Runtime 运行库（~50MB，跑 AI 出牌用的）。
FILES = ("DouSpire.json", "DouSpire.dll",
         "Microsoft.ML.OnnxRuntime.dll", "onnxruntime.dll", "onnxruntime_providers_shared.dll")
DIRS = ("assets", "onnx")
SKIP = {"voice-log.txt", "voice-log.txt.bak"}
# 这些子目录是**制作素材**，不是运行时资源，绝不能进包：
#   _orig/  —— 未经压缩的原图（55MB！）。每个文件在正式目录里都有一个几倍小的处理版，
#              代码里没有任何一处引用 _orig（已 grep 验证）。传上去纯属白送 55MB 流量。
SKIP_DIRS = {"_orig"}
# 这些扩展名不进包：
#   .wav —— 10-05 留下的 Windows/QQ/微信 系统音占位素材，早被 .ogg 取代，代码零引用。
#           防一道：万一有人又往 assets\voice\ 丢 .wav，也不会白传。
#   .md  —— 只是给人看的清单。
SKIP_EXTS = {".wav", ".md"}


def copy_content():
    dst = os.path.join(WORKSPACE, "content")
    os.makedirs(dst, exist_ok=True)

    if not os.path.isdir(GAME_MODS):
        raise SystemExit("找不到已部署的 mod：%s（先编译并拷进游戏 mods）" % GAME_MODS)

    picked = []
    keep = set()          # 这次应该有的文件（相对 dst 的路径），用来清掉上次的残留
    for f in FILES:
        src = os.path.join(GAME_MODS, f)
        if os.path.exists(src):
            keep.add(f)
            shutil.copy2(src, os.path.join(dst, f))
            picked.append("%s(%.0fKB)" % (f, os.path.getsize(src) / 1024))

    for d in DIRS:
        src = os.path.join(GAME_MODS, d)
        if not os.path.isdir(src):
            continue
        n = 0
        size = 0
        for root, _dirs, files in os.walk(src):
            # 剪掉制作素材目录（原地改 _dirs 才能阻止 os.walk 往下走）
            _dirs[:] = [d for d in _dirs if d not in SKIP_DIRS]
            for f in files:
                if f in SKIP:
                    continue
                if os.path.splitext(f)[1].lower() in SKIP_EXTS:
                    continue
                s = os.path.join(root, f)
                rel = os.path.relpath(s, GAME_MODS)
                t = os.path.join(dst, rel)
                os.makedirs(os.path.dirname(t), exist_ok=True)
                keep.add(rel.replace("\\", "/"))
                shutil.copy2(s, t)
                n += 1
                size += os.path.getsize(s)
        picked.append("%s/(%d 个文件, %.0fMB)" % (d, n, size / 1048576))

    # ★ 2026-10-09：**不用 `shutil.rmtree(dst)`** —— 一次删掉 1000+ 个文件会撞上
    #   WorkBuddy 沙箱的"批量删除"安全闸（SAFE_DELETE_BULK_CONFIRM_REQUIRED），
    #   打包会在半路失败（这次就这么挂的）。改成**增量同步**：覆盖 + 只删"这次不再产出"
    #   的那几个文件（通常是 0 个），再顺手清掉空目录。顺带也快得多。
    stale = 0
    for root, _dirs, files in os.walk(dst):
        for f in files:
            p = os.path.join(root, f)
            rel = os.path.relpath(p, dst).replace("\\", "/")
            if rel in keep:
                continue
            try:
                os.remove(p)
                stale += 1
            except OSError as e:
                print("  清不掉旧文件 %s：%s" % (rel, e))
    for root, dirs, files in os.walk(dst, topdown=False):
        if root != dst and not dirs and not files:
            try:
                os.rmdir(root)
            except OSError:
                pass

    print("content/：" + "、".join(picked) + ("；清掉上次残留 %d 个" % stale if stale else ""))


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
    with open(os.path.join(WORKSPACE, "workshop.json"), "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=2)
    nb = len(desc.encode("utf-8"))
    print("workshop.json：title=%s" % out["title"])
    print("  visibility=%s / tags=%s / deps=%s" % (out["visibility"], out["tags"], out["dependencies"]))
    # ★ 2026-10-11 实测钉死的结论：**Steam 工坊说明文的上限是 8000「字节」**（不是字符）。
    #   证据链：
    #     ① 0.2.41 上传成功，线上 description = 3725 字 / **7999 字节**（差 1 字节到 8000，明显是掐着上限砍的）；
    #     ② 0.2.42 第一次上传，本地 description 涨到 4433 字 / 9569 字节 →
    #        上传器在 `k_EItemUpdateStatusCommittingChanges` 报 **k_EResultInvalidParam**（错误码 8）。
    #   我一度以为按字节判是"误报"（因为字符数只有 4433 < 8000），把它改成按字符判 —— 那是错的，已改回来。
    #   ⇒ 这里**硬拦**，不再只是警告：超过 8000 字节直接拒绝打包，
    #     否则用户要白等一次 235MB 的上传才看到错误码。
    print("  说明文 %d 字 / %d 字节（Steam 上限 8000 **字节**，余 %d）" % (len(desc), nb, 8000 - nb))
    if nb > 8000:
        raise SystemExit(
            "[说明文超限] description.md 是 %d 字节，超过 Steam 的 8000 字节上限。\n"
            "  超了会在提交那一步报 k_EResultInvalidParam（错误码 8），白传一次整包。\n"
            "  请先删/压内容，砍到 7950 字节以下（留点余量）再打包。\n"
            "  当前：%d 字节，需要再砍 %d 字节。" % (nb, nb, nb - 7950))


def make_image():
    """封面输出成 image.png。**文件名必须是 image.png**，字节用 JPEG 编 —— 上传器认内容。
    画作类 PNG 直接传会很大（Steam 工坊也压不到 1MB 以下就传不上去）。"""
    from PIL import Image

    if not os.path.exists(COVER):
        raise SystemExit("没有封面：%s" % COVER)
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


def check_identity():
    """★ 2026-10-09 加：上架前的身份闸。

    背景：本地开发版从这天起改名成 `DouSpireDev`（部署在 `mods\\DouSpireDev`），
    工坊版仍然是 `DouSpire`。默认的 `build-mod.sh dou-spire` **编的是开发版** ——
    所以直接跑打包脚本会拿到 `id=DouSpireDev` 的 dll，传上去就把工坊条目改成开发版身份了。

    这里挡两道：
      ① `mods\\DouSpire\\DouSpire.json` 必须存在，且 id 必须是 `DouSpire`；
      ② 源码里任何一个 .cs 比这个 dll 新 → 说明工坊身份那份**没重编**，拒绝打包。
    """
    manifest = os.path.join(GAME_MODS, "DouSpire.json")
    rebuild = ("  先按工坊身份编一次：\n"
               "      cd \"C:/Users/有花无实/Documents/Default Project/projects\" && "
               "bash dou-spire-rules/tools/build-mod.sh dou-spire -p:ModName=DouSpire")
    if not os.path.exists(manifest):
        raise SystemExit("[身份闸] 找不到 %s\n%s" % (manifest, rebuild))
    try:
        mid = json.load(open(manifest, encoding="utf-8")).get("id")
    except Exception as e:
        raise SystemExit("[身份闸] 读不了 %s：%s" % (manifest, e))
    if mid != "DouSpire":
        raise SystemExit("[身份闸] %s 里的 id 是 %r，不是 'DouSpire' —— 拒绝打包。\n%s"
                         % (manifest, mid, rebuild))

    dll = os.path.join(GAME_MODS, "DouSpire.dll")
    if not os.path.exists(dll):
        raise SystemExit("[身份闸] 找不到 %s\n%s" % (dll, rebuild))
    dll_mtime = os.path.getmtime(dll)
    code_root = os.path.join(PROJ, "DouSpireCode")
    newest, newest_f = 0.0, ""
    for root, _dirs, files in os.walk(code_root):
        for f in files:
            if not f.endswith(".cs"):
                continue
            p = os.path.join(root, f)
            m = os.path.getmtime(p)
            if m > newest:
                newest, newest_f = m, p
    if newest > dll_mtime:
        raise SystemExit("[身份闸] 工坊身份那份 dll 比源码旧 —— 拒绝打包（会传上旧代码）。\n"
                         "  源码最新改动：%s\n  dll 时间：%s\n%s"
                         % (newest_f, time.strftime("%Y-%m-%d %H:%M:%S", time.localtime(dll_mtime)),
                            rebuild))
    print("身份闸：通过（id=DouSpire，dll 比所有 .cs 新）")


def main():
    os.makedirs(WORKSPACE, exist_ok=True)
    check_identity()
    copy_content()
    write_workshop_json()
    make_image()
    print("\n工作区: %s" % WORKSPACE)
    print('上传： cd "%s" && ./ModUploader.exe upload -w "%s"' % (UPLOADER, WORKSPACE))


if __name__ == "__main__":
    main()
