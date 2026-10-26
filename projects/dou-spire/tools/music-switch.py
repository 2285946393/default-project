#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""斗地主尖塔 · 两套歌单切换器（自己玩版 / 上架版）

为什么需要它
------------
音乐系统本来就是从磁盘读文件的，所以"想听什么歌"和"工坊上架什么"完全可以分开：

  * 自己玩版 —— 想放什么放什么（别人的商业歌也行），只在自己机器上，不上传。
  * 上架版   —— 只留自己生成 / 免费授权的歌，或者干脆空着。

但这两套是同一份目录，手动来回倒很容易忘。**忘了清干净就上传 = 把别人的歌打包上架。**
这个脚本就是干这个的：一条命令切过去，另一条切回来。

用法
----
  # 看现在是哪一版（各槽位几首、游戏目录里是什么）
  python tools/music-switch.py status

  # 自检：报一下 mod 里带了几首歌（**不拦上架**，免费商用素材可随包分发）
  python tools/music-switch.py check

  # 把个人歌库里的歌全部导入 → 切到"自己玩版"
  python tools/music-switch.py private

  # 把 mod 里的歌全部撤走 → 切到"不带歌版"（撤下的文件会留备份，不删）
  python tools/music-switch.py public

个人歌库放哪
------------
  projects/dou-spire/music-private/
      table/    tense/    climax/    win/    lose/

把原始文件（mp3 / flac / m4a / wav …）直接丢进对应文件夹就行，不用先转格式 ——
脚本会调 import-music.py 那套流程转成 .ogg 并做响度归一化。
这个目录**不会**被打包、不会被同步到游戏目录，是纯粹的本机素材区。
"""

import argparse
import datetime
import importlib.util
import os
import shutil
import sys

# ── 路径 ──────────────────────────────────────────────────────────
HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.dirname(HERE)                        # projects/dou-spire
ASSETS_MUSIC = os.path.join(PROJ, "assets", "music")   # 工程素材（编译时会同步进游戏）
PRIVATE = os.path.join(PROJ, "music-private")          # 个人歌库（本机，不上传）
BACKUP = os.path.join(PRIVATE, "_撤下备份")             # public 时把歌挪到这里，不删

STS2 = r"D:\software\steam\steamapps\common\Slay the Spire 2"
MOD_MUSIC = os.path.join(STS2, "mods", "DouSpire", "assets", "music")   # 游戏目录（直接生效）

# 复用 import-music.py（文件名带连字符，不能直接 import）
_spec = importlib.util.spec_from_file_location(
    "import_music", os.path.join(HERE, "import-music.py"))
IM = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(IM)

CUES = IM.CUES
AUDIO_EXT = IM.AUDIO_EXT

# 游戏目录里可能残留的、我们不该动的文件（说明文本）
KEEP_NAMES = {"放这里.txt", "说明.txt", "readme.txt"}


def is_audio(name):
    return os.path.splitext(name)[1].lower() in AUDIO_EXT and name.lower() not in KEEP_NAMES


def roots():
    """要管理的两个根目录（工程 + 游戏），游戏没装就只返回工程。"""
    out = [("工程素材", ASSETS_MUSIC)]
    if os.path.isdir(os.path.dirname(MOD_MUSIC)) or os.path.isdir(MOD_MUSIC):
        out.append(("游戏目录", MOD_MUSIC))
    return out


def scan(root):
    """扫一个根目录，返回 {槽位: [文件名, ...]}。含单文件形式 music/table.ogg。"""
    found = {c: [] for c in CUES}
    if not os.path.isdir(root):
        return found
    for cue in CUES:
        d = os.path.join(root, cue)
        if os.path.isdir(d):
            found[cue] = sorted(f for f in os.listdir(d)
                                if is_audio(f) and os.path.isfile(os.path.join(d, f)))
        for ext in (".ogg", ".mp3", ".wav"):
            if os.path.isfile(os.path.join(root, cue + ext)):
                found[cue].append(cue + ext + "  (单文件)")
    return found


def total(found):
    return sum(len(v) for v in found.values())


def show(root, label):
    found = scan(root)
    n = total(found)
    head = f"{label}  {root}"
    print(head)
    if n == 0:
        print("    —— 空（干净版：平时/紧张回退游戏本体曲子）")
        return 0
    for cue in CUES:
        for f in found[cue]:
            print(f"    {cue:<7} {f}")
    return n


# ── status / check ────────────────────────────────────────────────
def do_status(verbose=True):
    dirty = 0
    for label, root in roots():
        dirty += show(root, label)
        if verbose:
            print()
    print("=" * 62)
    if dirty:
        print(f"现在 = 带歌版（两个目录合计 {dirty} 个音乐文件，可随包上架）。")
        print("想做个不带歌的版本：python tools/music-switch.py public   （歌会挪进 _撤下备份，不删）")
    else:
        print("现在 = 不带歌版（干净）。")
    print("=" * 62)
    print(f"个人歌库：{PRIVATE}")
    if os.path.isdir(PRIVATE):
        lib = 0
        for cue in CUES:
            d = os.path.join(PRIVATE, cue)
            if os.path.isdir(d):
                lib += len([f for f in os.listdir(d) if is_audio(f)])
        print(f"          里面存了 {lib} 首原曲（切 private 时会全部导入）")
    else:
        print("          （还没有这个目录，切 private 时会自动建好 5 个槽位文件夹）")
    return dirty


def do_check():
    """上架前自检。

    ⚠️ 2026-10-10 起：**带歌上架不再是错误**。
    用户确认 mod 里那 4 首是**免费商用素材站**下的 ⇒ 可以随包分发（线上 0.2.39 就是带歌的）。
    所以这里改成"提示式"：报个数、**返回 0**，不再拦上架。
    （`public` / `private` 两版歌单的切换仍然保留 —— 想做个不带歌的版本还是有用的。）
    """
    dirty = 0
    for label, root in roots():
        dirty += total(scan(root))
    if dirty:
        print(f"ℹ️ mod 里带了 {dirty} 个音乐文件（免费商用素材，可随包上架）。")
        print("   换歌请自行确认授权。想做个不带歌的版本：python tools/music-switch.py public")
        return 0
    print("mod 里没有任何音乐文件（不带歌版）。")
    return 0


# ── public：撤走所有歌 ─────────────────────────────────────────────
def do_public():
    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    moved = 0
    for label, root in roots():
        if not os.path.isdir(root):
            continue
        for cue in CUES:
            d = os.path.join(root, cue)
            if not os.path.isdir(d):
                continue
            dst_dir = os.path.join(BACKUP, stamp, os.path.basename(root), cue)
            for f in sorted(os.listdir(d)):
                if not is_audio(f) or not os.path.isfile(os.path.join(d, f)):
                    continue
                os.makedirs(dst_dir, exist_ok=True)
                shutil.move(os.path.join(d, f), os.path.join(dst_dir, f))
                moved += 1
                print(f"  撤下  {label}/{cue}/{f}")
            # 单文件形式
        for cue in CUES:
            for ext in (".ogg", ".mp3", ".wav"):
                p = os.path.join(root, cue + ext)
                if os.path.isfile(p):
                    dst_dir = os.path.join(BACKUP, stamp, os.path.basename(root), cue)
                    os.makedirs(dst_dir, exist_ok=True)
                    shutil.move(p, os.path.join(dst_dir, cue + ext))
                    moved += 1
                    print(f"  撤下  {label}/{cue}{ext}  (单文件)")

    print()
    if moved:
        print(f"已撤下 {moved} 首 → {os.path.join(BACKUP, stamp)}")
        print("（是挪走不是删掉，想找回就从这个目录里拿。）")
    else:
        print("本来就没有音乐文件，已经是干净版。")
    print()
    print("现在可以上传了。changeNote 里也别写「包含 XX 首歌」之类的话。")
    return 0


# ── private：把个人歌库导进来 ─────────────────────────────────────
def ensure_library():
    """建好个人歌库的 5 个槽位文件夹，并写一份说明。"""
    made = []
    for cue in CUES:
        d = os.path.join(PRIVATE, cue)
        if not os.path.isdir(d):
            os.makedirs(d, exist_ok=True)
            made.append(cue)
    readme = os.path.join(PRIVATE, "说明.txt")
    if not os.path.isfile(readme):
        with open(readme, "w", encoding="utf-8") as fh:
            fh.write(
                "个人歌库（本机自用，不会被打包、不会上传）\n"
                "========================================\n\n"
                "把歌丢进对应的槽位文件夹，然后跑：\n"
                "    python tools\\music-switch.py private\n\n"
                "  table\\    平时打牌\n"
                "  tense\\    有人只剩 1~2 张牌\n"
                "  climax\\   炸弹 / 王炸 / 加倍 / 抢地主（插播 11 秒）\n"
                "  win\\      这局我赢了\n"
                "  lose\\     这局我输了\n\n"
                "格式随便（mp3 / flac / m4a / wav 都行），脚本会转成 .ogg 并统一响度。\n"
                "放歌之后记得进游戏 F9 收桌再开桌。\n\n"
                "想做个不带歌的版本（少见）：\n"
                "    python tools\\music-switch.py public\n"
                "把歌挪进备份、包里就不带歌了。\n"
                "（本 mod 现有的 4 首是免费商用素材，正常带歌上架即可。）\n")
    return made


def do_private(no_convert=False):
    made = ensure_library()
    if made:
        print("已建好个人歌库槽位：" + " / ".join(made))
        print(f"    {PRIVATE}")
        print()

    jobs = []
    for cue in CUES:
        d = os.path.join(PRIVATE, cue)
        if not os.path.isdir(d):
            continue
        for f in sorted(os.listdir(d)):
            if is_audio(f) and os.path.isfile(os.path.join(d, f)):
                jobs.append((cue, os.path.join(d, f)))

    if not jobs:
        print(f"个人歌库里一首歌都没有：{PRIVATE}")
        print("把歌丢进 table\\ tense\\ climax\\ win\\ lose\\ 再跑一次。")
        return 1

    ffmpeg = IM.find_ffmpeg()
    if not ffmpeg and not no_convert:
        print("!! 找不到 ffmpeg，无法转码。用 --no-convert 只拷贝 .ogg，或装一个 ffmpeg。")
        return 1

    print(f"个人歌库 : {PRIVATE}")
    print(f"待导入   : {len(jobs)} 首" + ("（直接拷贝，不转码）" if no_convert else f"   ffmpeg: {ffmpeg}"))
    print("-" * 62)

    ok = 0
    for i, (cue, src) in enumerate(jobs, 1):
        stem = IM.safe_stem(os.path.basename(src))
        out_dir = os.path.join(ASSETS_MUSIC, cue)
        os.makedirs(out_dir, exist_ok=True)
        dst = os.path.join(out_dir, stem + ".ogg")
        n = 2
        while os.path.exists(dst):
            dst = os.path.join(out_dir, f"{stem}_{n}.ogg")
            n += 1
        print(f"[{i}/{len(jobs)}] {cue:<7} {os.path.basename(src)}")

        if no_convert:
            try:
                shutil.copy2(src, dst)
                ok += 1
                print(f"         OK → {os.path.basename(dst)}")
            except Exception as e:
                print(f"         失败 {e}")
            continue

        good, info = IM.convert(ffmpeg, src, dst)
        if good:
            ok += 1
            print(f"         OK → {os.path.basename(dst)}   {info}")
        else:
            print(f"         失败 {info}")

    print("-" * 62)
    print(f"导入 {ok}/{len(jobs)}。")

    # 同步进游戏目录（免编译）
    copied = 0
    if os.path.isdir(os.path.dirname(MOD_MUSIC)):
        for cue in CUES:
            src_dir = os.path.join(ASSETS_MUSIC, cue)
            if not os.path.isdir(src_dir):
                continue
            dst_dir = os.path.join(MOD_MUSIC, cue)
            os.makedirs(dst_dir, exist_ok=True)
            for f in os.listdir(src_dir):
                if f.lower().endswith(".ogg"):
                    shutil.copy2(os.path.join(src_dir, f), os.path.join(dst_dir, f))
                    copied += 1
        print(f"已拷进游戏目录 {copied} 首 → {MOD_MUSIC}")
    else:
        print("!! 找不到游戏 mod 目录，跳过同步（编译一次也会自动同步）。")

    print()
    print("现在 = 自己玩版。进游戏 F9 收桌、再 F9 开桌即可生效。")
    return 0 if ok else 1


def main():
    ap = argparse.ArgumentParser(
        description="斗地主尖塔 · 自己玩版 / 上架版 歌单切换",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    ap.add_argument("mode", nargs="?", default="status",
                    choices=["status", "check", "private", "public"],
                    help="status(默认) / check / private / public")
    ap.add_argument("--no-convert", action="store_true",
                    help="private 时只拷贝、不转码（只对已经是 .ogg 的源有意义）")
    args = ap.parse_args()

    if args.mode == "status":
        do_status()
        return 0
    if args.mode == "check":
        return do_check()
    if args.mode == "public":
        return do_public()
    return do_private(no_convert=args.no_convert)


if __name__ == "__main__":
    sys.exit(main())
