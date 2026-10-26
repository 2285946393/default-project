#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
把 TTS 生成好的台词音频装进语音包。

用法：
    python tools/import-voice.py D:/BaiduNetdiskDownload

它会：
  1. 扫描目录里的 mp3/wav/ogg，从**文件名里的台词原文**反查属于哪个槽位；
  2. 转成 ogg（游戏只认 ogg / wav，ogg 体积小得多）；
  3. 按 silent/9_bomb_1.ogg 这种名字写进 assets/voice/<角色>/；
  4. 最后报一份账：装了哪些、哪些槽位还空着、哪些文件没认出来。

★ 为什么能靠台词反查：
  用户用 FishAudio 生成时文件名是「角色-日期-台词原文.mp3」，
  台词原文在 notes/ 的台词文档里都有，**归一化后一字对一字**就能定位槽位。
  比让用户手工改 64 个文件名靠谱得多。

★ 槽位命名规则（见 DouSpireVoice.cs）：
  {编号}_{事件}[_{第几句}].ogg —— 同前缀多个文件**自动随机轮播**，
  所以某槽位只有一句时叫 _1 还是 _2 都无所谓（游戏传的是不带 _N 的词干）。
  这里统一按「该槽位第几个文件」从 _1 开始排，整洁。
"""

import os
import re
import subprocess
import sys
import shutil

# ─────────────────────────── 槽位表（和 DouSpireVoice.cs 一一对应） ───────────────────────────

SLOTS = [
    ("1_deal", "发牌"),
    ("2_landlord", "当上地主"),
    ("3_myturn", "轮到我"),
    ("4_hint", "点提示"),
    ("5_small", "出小牌（单张/对子）"),
    ("6_triple", "出三带"),
    ("7_straight", "出顺子/连对"),
    ("8_big", "出飞机/四带"),
    ("9_bomb", "炸弹/王炸"),
    ("10_pass", "要不起"),
    ("11_beat", "压住别人"),
    ("12_lastone", "报单（我剩1张）"),
    ("13_win", "赢"),
    ("14_lose", "输"),
    ("15_bid", "抢地主"),
    ("16_nobid", "我不抢"),
]

# 台词原文 → 槽位。每个槽位两句（_1 / _2）。
# 来源：notes/斗地主尖塔-沉默猎手中文台词.md、notes/斗地主尖塔-亡灵契约师中文台词.md
LINES = {
    "silent": {
        "发牌吧，猎物已经上桌了。": "1_deal",
        "这副牌的破绽，我会找出来的。": "1_deal",
        "地主？正合我意，一个打两个而已。": "2_landlord",
        "被围猎的，可不一定是我。": "2_landlord",
        "轮到我了。": "3_myturn",
        "看好了，一刀的事。": "3_myturn",
        "提示？我自己就是提示。": "4_hint",
        "牌路，早就看清了。": "4_hint",
        "先试试你的深浅。": "5_small",
        "小鱼小虾，放出去。": "5_small",
        "三连击，轻巧。": "6_triple",
        "这一手，够你受的。": "6_triple",
        "刀刀连着，别眨眼。": "7_straight",
        "顺下去了，接得住吗？": "7_straight",
        "大的，压场。": "8_big",
        "这把，梭了。": "8_big",
        "毒发之前，你不会察觉。": "9_bomb",
        "双王，见血封喉。": "9_bomb",
        "要不起，退一步。": "10_pass",
        "这轮，让给你。": "10_pass",
        "抱歉，压你一头。": "11_beat",
        "你的破绽，在这。": "11_beat",
        "只剩一张了。": "12_lastone",
        "最后一刀，留给你。": "12_lastone",
        "狩猎结束。": "13_win",
        "从坐下的那一刻，你就输了。": "13_win",
        "……失手了。下局补上。": "14_lose",
        "有点意思，你赢了。": "14_lose",
        "抢。": "15_bid",
        "地主，我来当。": "15_bid",
        "不抢，先看着。": "16_nobid",
        "让你们先跑两步。": "16_nobid",
    },
    "necrobinder": {
        "牌，如命运……一张张发下来。": "1_deal",
        "新的灵魂，上桌了。": "1_deal",
        "地主之位，与我契约。": "2_landlord",
        "一挑二？正好……凑一对。": "2_landlord",
        "轮到我了……别急。": "3_myturn",
        "让我看看，你们的命数。": "3_myturn",
        "亡者低语，在指引我。": "4_hint",
        "牌灵在说话……听到了吗？": "4_hint",
        "小卒，先行一步。": "5_small",
        "送一张出去……探探阴气。": "5_small",
        "三魂七魄……带走。": "6_triple",
        "三个，一起收了。": "6_triple",
        "魂魄列队……缓缓而行。": "7_straight",
        "一列亡魂，你拦不住的。": "7_straight",
        "大阵……起。": "8_big",
        "仪式，正式开始。": "8_big",
        "轰然……湮灭吧。": "9_bomb",
        "双王现世，万魂齐鸣。": "9_bomb",
        "要不起……时机未到。": "10_pass",
        "阴气不足，暂避锋芒。": "10_pass",
        "你，被我压在下面了。": "11_beat",
        "安分些……凡人。": "11_beat",
        "只剩一缕残魂。": "12_lastone",
        "最后一张……送你们上路。": "12_lastone",
        "胜负已定，魂归我。": "13_win",
        "这一桌的命……都归我了。": "13_win",
        "契约……失效了么。": "14_lose",
        "今日运势，不利。": "14_lose",
        "抢。这是……命运的安排。": "15_bid",
        "地主之位，非我莫属。": "15_bid",
        "不抢，先送你们一程。": "16_nobid",
        "静观……其变。": "16_nobid",
    },
}

# 文件名里的角色前缀 → 语音包目录（用户这次是拿国产二游角色当音色参考配的）
ROLE_PREFIX = {
    "黄泉": "silent",        # 沉默猎手 ← 黄泉（崩铁）
    "黑天鹅": "necrobinder",  # 亡灵契约师 ← 黑天鹅（崩铁）
    "沉默猎手": "silent",
    "猎豹": "silent",
    "亡灵契约师": "necrobinder",
    "骨妹": "necrobinder",
    "骨头人": "necrobinder",
    "silent": "silent",
    "necrobinder": "necrobinder",
}

# ─────────────────────────── 工具 ───────────────────────────

# 归一化：只留汉字/字母/数字，去掉所有标点和空白（「抢。」和「抢」要能配上）
NOISE = re.compile(r"[^0-9A-Za-z\u4e00-\u9fff]+")


def norm(s: str) -> str:
    return NOISE.sub("", s)


def find_ffmpeg() -> str | None:
    for c in ("ffmpeg", "ffmpeg.exe"):
        p = shutil.which(c)
        if p:
            return p
    home = os.path.expanduser("~")
    for c in (r"tools\ffmpeg\bin\ffmpeg.exe", "tools/ffmpeg/bin/ffmpeg.exe"):
        p = os.path.join(home, c)
        if os.path.exists(p):
            return p
    return None


def to_ogg(ffmpeg: str, src: str, dst: str) -> bool:
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    cmd = [ffmpeg, "-y", "-i", src, "-c:a", "libvorbis", "-q:a", "4",
           "-ar", "44100", "-ac", "1", "-vn", dst]
    r = subprocess.run(cmd, capture_output=True)
    if r.returncode != 0 or not os.path.exists(dst) or os.path.getsize(dst) == 0:
        print(f"    ❌ 转码失败：{os.path.basename(src)}")
        if r.stderr:
            print("       " + r.stderr.decode("utf-8", "replace").strip().splitlines()[-1])
        return False
    return True


def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__)
        return 1
    src_dir = sys.argv[1]
    if not os.path.isdir(src_dir):
        print(f"❌ 目录不存在：{src_dir}")
        return 1

    here = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    out_root = os.path.join(here, "assets", "voice")

    ffmpeg = find_ffmpeg()
    if ffmpeg is None:
        print("❌ 找不到 ffmpeg（要它把 mp3 转成 ogg）")
        return 1
    print(f"转码工具：{ffmpeg}\n")

    # 归一化台词 → 槽位，方便按文件名里的台词查
    table = {}
    for role, lines in LINES.items():
        for text, slot in lines.items():
            table.setdefault(role, {})[norm(text)] = slot

    ok, unknown, failed = 0, [], 0
    used = {r: {} for r in LINES}      # role → slot → 已用了几个（决定 _1 / _2）

    files = sorted(f for f in os.listdir(src_dir)
                   if os.path.splitext(f)[1].lower() in (".mp3", ".wav", ".ogg", ".m4a", ".flac"))

    for name in files:
        stem = os.path.splitext(name)[0]

        # ① 认角色（文件名开头的中文角色名）
        role = None
        for p, r in ROLE_PREFIX.items():
            if stem.startswith(p):
                role = r
                break
        if role is None:
            unknown.append((name, "文件名开头认不出角色（要写「黄泉-…」或「黑天鹅-…」）"))
            continue

        # ② 认台词：拿掉角色名和日期时间戳，剩下的就是台词原文
        tail = stem
        for p in ROLE_PREFIX:
            if tail.startswith(p):
                tail = tail[len(p):]
                break
        tail = tail.lstrip("-–— ")
        # 去掉 2026-10-11-13-41 这种时间戳
        tail = re.sub(r"\d{4}-\d{2}-\d{2}-\d{2}-\d{2}(-\d+)?", "", tail).lstrip("-–— ")

        key = norm(tail)
        slot = None
        for cand, s in table[role].items():
            if cand == key or cand in key or (len(key) >= 3 and key in cand):
                slot = s
                break
        if slot is None:
            unknown.append((name, f"台词对不上号：「{tail}」"))
            continue

        # ③ 定文件名：该槽位第几个
        n = used[role].get(slot, 0) + 1
        used[role][slot] = n
        dst = os.path.join(out_root, role, f"{slot}_{n}.ogg")

        src = os.path.join(src_dir, name)
        size_before = os.path.getsize(src)
        if to_ogg(ffmpeg, src, dst):
            got = os.path.getsize(dst)
            ok += 1
            print(f"  ✅ {role}/{slot}_{n}.ogg"
                  f"   ← {name[:46]}  ({size_before//1024}KB → {got//1024}KB)")
        else:
            failed += 1

    # ─────────── 报账 ───────────
    print()
    print("═" * 62)
    print(f"装好了 {ok} 条，失败 {failed} 条，没认出来 {len(unknown)} 条。")
    print("═" * 62)

    if unknown:
        print("\n没认出来的（要人工对一下）：")
        for name, why in unknown:
            print(f"  ⚠️ {name}")
            print(f"      {why}")

    print("\n还空着的槽位（游戏里这些时候他会不出声）：")
    empty_any = False
    for role in ("silent", "necrobinder"):
        miss = [f"{s}（{des}）" for s, des in SLOTS if used[role].get(s, 0) == 0]
        label = {"silent": "沉默猎手 silent", "necrobinder": "亡灵契约师 necrobinder"}[role]
        if miss:
            empty_any = True
            print(f"\n  【{label}】缺 {len(miss)} / 16：")
            for m in miss:
                print(f"      · {m}")
        else:
            print(f"\n  【{label}】16 / 16 全齐 ✅")
    if not empty_any:
        print("\n两个角色都齐了。")

    print("\n下一步：把这些 ogg 复制到游戏 mods 目录（开发版 + 工坊版各一份），脚本不自动做。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
