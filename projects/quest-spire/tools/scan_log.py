"""扫描游戏日志里**所有**异常，按"异常类型 + 前几帧"去重打印。

为什么写这个：只 grep 我们自己的名字会漏掉"游戏侧因为我们的东西抛的异常"
（例如 RelicReward.OnSelect 抛异常时，栈帧里根本没有 QuestSpire 字样）。
用法：python tools/scan_log.py [日志文件数，默认 3]
"""
import io, os, glob, re, sys, time

ROOT = os.path.join(os.environ["APPDATA"], "SlayTheSpire2", "logs")
N = int(sys.argv[1]) if len(sys.argv) > 1 else 3
files = sorted(glob.glob(os.path.join(ROOT, "*.log")), key=os.path.getmtime, reverse=True)[:N]

# 这些第三方噪音每次都有，直接忽略
NOISE = re.compile(r"OrchisNecrobinder|MegaSpine|SpineClickInteractor|RID allocations|shaders of type|"
                   r"resources still in use|Asset not cached|VoiceMod|Mesugaki|Cloud write failed|Invalid Task ID")

for f in files:
    lines = io.open(f, encoding="utf-8", errors="ignore").read().splitlines()
    print("=" * 70)
    print("%s  %d 行  (%s)" % (os.path.basename(f), len(lines),
                               time.strftime("%m-%d %H:%M", time.localtime(os.path.getmtime(f)))))
    seen = set()
    i = 0
    while i < len(lines):
        l = lines[i]
        if "ERROR" in l or "Exception" in l:
            block = [l]
            j = i + 1
            while j < len(lines) and j < i + 12 and (lines[j].startswith("   ") or lines[j].startswith("    ") or "at " in lines[j]):
                block.append(lines[j]); j += 1
            text = "\n".join(block)
            if NOISE.search(text):
                i = j; continue
            # 去重：异常首行 + 栈里前 3 个有名字的帧
            frames = [x.strip()[:90] for x in block if "at " in x][:3]
            key = (block[0][:120], tuple(frames))
            if key not in seen:
                seen.add(key)
                print("---- 行 %d ----" % (i + 1))
                for x in block[:10]:
                    print("   " + x.strip()[:180])
            i = j
        else:
            i += 1
    print("（本文件去重后异常块：%d）" % len(seen))
