#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""把歌批量转成 .ogg 并放进斗地主尖塔的音乐槽位。

为什么需要它：Godot 只认 .ogg / .wav / .mp3，而 AI 音乐站和大多数下载源给的是
.mp3 / .flac / .m4a。手动转还要担心几首歌响度不一致（一首吵一首轻），
所以这里顺手做 EBU R128 响度归一化。

用法
----
  # 把一个文件夹里的歌全部转进"高潮"槽位
  python tools/import-music.py D:\\歌\\辉夜姬 --cue climax

  # 只转一首，并且裁掉前面 8 秒
  python tools/import-music.py 主题曲.mp3 --cue table --skip 8

  # 裁掉 AI 水印：只保留前 256 秒（见 notes/AI音乐生成-偏好与操作.md）
  python tools/import-music.py 歌.mp3 --cue win --keep 256

  # 转完顺便直接拷进游戏 mod 目录（不用重新编译）
  python tools/import-music.py D:\\歌 --cue tense --to-mod

  # 看看现在每个槽位里有几首
  python tools/import-music.py --list
"""

import argparse
import json
import os
import re
import shutil
import subprocess
import sys

# ── 路径 ──────────────────────────────────────────────────────────
HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.dirname(HERE)                       # projects/dou-spire
ASSETS_MUSIC = os.path.join(PROJ, "assets", "music")

STS2 = r"D:\software\steam\steamapps\common\Slay the Spire 2"
MOD_MUSIC = os.path.join(STS2, "mods", "DouSpire", "assets", "music")

CUES = ["table", "tense", "climax", "win", "lose"]

FFMPEG_CANDIDATES = [
    os.path.expanduser(r"~\tools\ffmpeg\bin\ffmpeg.exe"),
    r"C:\ffmpeg\bin\ffmpeg.exe",
    "ffmpeg",
]

AUDIO_EXT = {".mp3", ".flac", ".m4a", ".wav", ".aac", ".opus", ".wma", ".ogg", ".aiff", ".ape"}

# 响度归一化：-16 LUFS 是背景音乐常用的档位（比人声/音效低一点，不抢戏）
LOUDNORM = "loudnorm=I=-16:TP=-1.5:LRA=11"


def find_ffmpeg():
    for c in FFMPEG_CANDIDATES:
        if c == "ffmpeg":
            p = shutil.which("ffmpeg")
            if p:
                return p
        elif os.path.isfile(c):
            return c
    return None


def safe_stem(name):
    """文件名安全化：去掉路径分隔符和奇怪字符，保留中文。"""
    s = os.path.splitext(os.path.basename(name))[0]
    s = re.sub(r'[<>:"/\\|?*\x00-\x1f]', "_", s).strip()
    return s or "track"


def convert(ffmpeg, src, dst, skip=None, keep=None, normalize=True, quiet=False):
    """转一个文件。返回 (成功?, 说明)。"""
    af = []
    if normalize:
        af.append(LOUDNORM)
    if skip:
        af.append(f"atrim=start={skip},asetpts=PTS-STARTPTS")

    cmd = [ffmpeg, "-hide_banner", "-loglevel", "error", "-y"]
    if skip:
        cmd += ["-ss", str(skip)]
    cmd += ["-i", src]
    if keep:
        cmd += ["-t", str(keep)]
    cmd += ["-vn", "-map_metadata", "-1"]
    if af:
        cmd += ["-af", ",".join(af)]
    cmd += ["-c:a", "libvorbis", "-q:a", "5", "-ar", "44100", "-ac", "2", dst]

    try:
        r = subprocess.run(cmd, capture_output=True, text=True, timeout=1800)
    except subprocess.TimeoutExpired:
        return False, "超时"
    if r.returncode != 0:
        return False, (r.stderr or "").strip().splitlines()[-1] if r.stderr else "ffmpeg 失败"
    if not os.path.isfile(dst) or os.path.getsize(dst) < 1024:
        return False, "输出文件是空的"
    return True, f"{os.path.getsize(dst) / 1048576:.1f} MB"


def human(n):
    for u in ("B", "KB", "MB", "GB"):
        if n < 1024:
            return f"{n:.0f} {u}"
        n /= 1024
    return f"{n:.1f} TB"


def do_list():
    print("斗地主尖塔 · 音乐槽位现状")
    print("=" * 58)
    total = 0
    for cue in CUES:
        d = os.path.join(ASSETS_MUSIC, cue)
        files = []
        if os.path.isdir(d):
            files = [f for f in sorted(os.listdir(d))
                     if os.path.splitext(f)[1].lower() in AUDIO_EXT]
        # 单文件形式 music/table.ogg
        single = ""
        for ext in (".ogg", ".mp3", ".wav"):
            p = os.path.join(ASSETS_MUSIC, cue + ext)
            if os.path.isfile(p):
                single = cue + ext
                break
        if not files and not single:
            print(f"  {cue:<8} —— 空（用游戏本体曲子）")
            continue
        if single:
            print(f"  {cue:<8} 单文件 {single}")
            total += 1
        for f in files:
            sz = os.path.getsize(os.path.join(d, f))
            print(f"  {cue:<8} {f}   ({human(sz)})")
            total += 1
    print("=" * 58)
    print(f"共 {total} 首。放好歌之后，游戏里 F9 收桌、F9 开桌即可生效。")
    print(f"素材目录：{ASSETS_MUSIC}")
    print(f"游戏目录：{MOD_MUSIC}")


def main():
    ap = argparse.ArgumentParser(
        description="把歌转成 .ogg 并放进斗地主尖塔的音乐槽位",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    ap.add_argument("source", nargs="?", help="音频文件，或装着音频的文件夹")
    ap.add_argument("--cue", choices=CUES, help="放进哪个槽位：" + " / ".join(CUES))
    ap.add_argument("--skip", type=float, default=None, help="开头裁掉多少秒")
    ap.add_argument("--keep", type=float, default=None, help="只保留前多少秒（裁掉 AI 水印用）")
    ap.add_argument("--name", default=None, help="单文件模式下的输出名（不含扩展名）")
    ap.add_argument("--no-normalize", action="store_true", help="不做响度归一化")
    ap.add_argument("--to-mod", action="store_true", help="顺便直接拷进游戏 mod 目录（免编译）")
    ap.add_argument("--list", action="store_true", help="只列出各槽位现状")
    ap.add_argument("--ffmpeg", default=None, help="指定 ffmpeg 路径")
    args = ap.parse_args()

    if args.list or not args.source:
        do_list()
        return 0 if args.list else 1

    if not args.cue:
        print("❌ 要指定槽位：--cue " + " / ".join(CUES))
        return 1

    ffmpeg = args.ffmpeg or find_ffmpeg()
    if not ffmpeg:
        print("❌ 找不到 ffmpeg。用 --ffmpeg 指定路径，或装一个放到 PATH 里。")
        print("   本机已知位置：C:\\Users\\<你>\\tools\\ffmpeg\\bin\\ffmpeg.exe")
        return 1

    src = args.source
    jobs = []
    if os.path.isdir(src):
        for f in sorted(os.listdir(src)):
            if os.path.splitext(f)[1].lower() in AUDIO_EXT and not f.startswith("."):
                jobs.append((os.path.join(src, f), safe_stem(f)))
        if not jobs:
            print(f"❌ 这个文件夹里没有音频文件：{src}")
            return 1
    elif os.path.isfile(src):
        jobs.append((src, args.name or safe_stem(src)))
    else:
        print(f"❌ 找不到：{src}")
        return 1

    out_dir = os.path.join(ASSETS_MUSIC, args.cue)
    os.makedirs(out_dir, exist_ok=True)

    print(f"ffmpeg : {ffmpeg}")
    print(f"槽位   : {args.cue}  →  {out_dir}")
    print(f"待转   : {len(jobs)} 个")
    print("-" * 58)

    ok = 0
    for i, (path, stem) in enumerate(jobs, 1):
        dst = os.path.join(out_dir, f"{stem}.ogg")
        # 同名就加序号，不覆盖已有的歌
        n = 2
        while os.path.exists(dst):
            dst = os.path.join(out_dir, f"{stem}_{n}.ogg")
            n += 1
        print(f"[{i}/{len(jobs)}] {os.path.basename(path)}")
        good, info = convert(ffmpeg, path, dst,
                             skip=args.skip, keep=args.keep,
                             normalize=not args.no_normalize)
        if good:
            ok += 1
            print(f"        ✅ → {os.path.basename(dst)}   {info}")
        else:
            print(f"        ❌ {info}")

    print("-" * 58)
    print(f"完成 {ok}/{len(jobs)}。")

    if args.to_mod:
        try:
            os.makedirs(os.path.join(MOD_MUSIC, args.cue), exist_ok=True)
            copied = 0
            for f in os.listdir(out_dir):
                if f.lower().endswith(".ogg"):
                    shutil.copy2(os.path.join(out_dir, f),
                                 os.path.join(MOD_MUSIC, args.cue, f))
                    copied += 1
            print(f"已拷进游戏目录 {copied} 个 → {os.path.join(MOD_MUSIC, args.cue)}")
        except Exception as e:
            print(f"⚠️ 拷进游戏目录失败（不影响上面的转换）：{e}")

    print()
    print("进游戏后按 F9 收桌、再按 F9 开桌即可生效（不用重启游戏）。")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
