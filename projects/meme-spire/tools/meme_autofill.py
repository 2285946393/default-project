# -*- coding: utf-8 -*-
"""
「梗化尖塔」自动补梗 —— 一条龙：导出待办 → 批量起草 → 自动筛 → 写回 json → 刷图鉴。

用法：
    python tools/meme_autofill.py                 # 把所有还剩的表都推一轮
    python tools/meme_autofill.py --tables powers,events
    python tools/meme_autofill.py --max-batches 6 # 这轮最多调几次模型（默认 8）
    python tools/meme_autofill.py --dry           # 只看会改什么，不写文件

安全措施（都很重要）：
  1. **写之前自动备份**到 tools/_backup_<表>.json（只留一份，覆盖式）。
  2. **自动筛**：新名 == 原名、或新名里含原名（"飞镖→夺命飞镖"这种换近义词的）一律丢掉。
  3. 同一个新名给了不同原名 → 只留第一个。
  4. 只写**原版存在的 key**，且只写 .title/.name/.flavor/招式名/台词 这类（不碰 .description）。
  5. 任何一步出错都只跳过那一条，不中断整轮。

后端：优先 WorkBuddy 国际版（免费、更聪明），失败退回 DSH 免费车道。
"""
import argparse
import io
import json
import os
import re
import shutil
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.dirname(HERE)                                    # projects/meme-spire
ROOT = os.path.dirname(os.path.dirname(PROJ))                    # 仓库根
LIMIT = re.compile(r"\.(title|name|flavor)$|\.moves\.[A-Z0-9_]+\.(title|speakLine\d*|banter)$"
                   r"|\.pages\.[A-Z0-9_]+\.title$|\.eventDescription$")

# ⚠️ 这些是**已授权改写**的字段（包含 .eventDescription —— 2026-10-04 踩过坑，别再当"误改"还原）
MEME_DIR = os.path.join(PROJ, "MemeSpire", "localization", "zhs")
REF_DIR = os.path.join(PROJ, "reference")
WB = os.path.join(ROOT, "tools", "wb.mjs")
FREE = r"D:\software\dsh-home\lib\free-model.mjs"
GALLERY = os.path.join(HERE, "gen_meme_gallery.py")

PROMPT = """你在给《杀戮尖塔2》的中文"梗化"mod起名，把游戏里的名字换成中文大众梗。

【铁律】
1. 新名字必须是**梗**（影视台词/广告词/动漫/游戏/网络热词/名人语录/成语典故/小品相声），
   **不能是原名的近义词或加长版**。反例：飞镖→夺命飞镖（不合格）、灯笼→大红灯笼（不合格）。
   正例：硫磺→火药桶、化学物X→绝命毒师、芒果→芒果TV、节拍器→节奏大师。
2. 梗要全国大众都懂，别用只玩尖塔的人才知道的圈内梗。
3. 字数和原名接近（原名 2 字就给 2~4 字）。
4. 原名里的大括号占位符（花括号包英文）和方括号标签必须原样保留。
5. 想不出梗就改成一个**更接地气、有画面感的叫法**也行（例：骨茶→骨头汤、尘封魔典→吃灰的书）；
   但**不要原样照抄**，实在不行才照抄。

【输出格式】一行一条：编号 + 制表符 + 新名字。不要解释、不要开场白。

【要起名的条目】
{items}
"""


def load(p):
    return json.load(io.open(p, encoding="utf-8"))


def todo_for(table):
    """这个表里"还能改但还没改"的条目（按 LIMIT 过滤字段）。"""
    mp = os.path.join(MEME_DIR, table + ".json")
    rp = os.path.join(REF_DIR, "zhs__%s.json" % table)
    if not (os.path.exists(mp) and os.path.exists(rp)):
        return []
    meme, van = load(mp), load(rp)
    out = []
    for k, v in van.items():
        if not LIMIT.search(k):
            continue
        if k in meme and meme[k] != v:
            continue
        out.append({"key": k, "vanilla": v})
    return out


def ask(rows):
    items = "\n".join("%d. %s = %s" % (i + 1, r["key"], r["vanilla"]) for i, r in enumerate(rows))
    prompt = PROMPT.replace("{items}", items)
    cmds = []
    if os.path.exists(WB):
        cmds.append(["node", WB, "--stdin", "--region", "global", "--model", "deepseek-v4.1-flash"])
    cmds.append(["node", FREE, "--stdin", "--model", "space-bunny-free", "--timeout", "240"])
    for c in cmds:
        try:
            p = subprocess.run(c, input=prompt, capture_output=True, text=True,
                               encoding="utf-8", errors="replace", timeout=300)
            got = {}
            for line in ((p.stdout or "") + (p.stderr or "")).splitlines():
                m = re.match(r"\s*(\d+)\s*[\t ]\s*(.+?)\s*$", line)
                if m:
                    got[int(m.group(1))] = m.group(2)
            if got:
                return got
        except Exception as e:
            print("   ! %s 失败: %s" % (c[1] if len(c) > 1 else "?", e))
    return {}


def looks_synonym(old, new):
    o, n = old.strip(), new.strip()
    if not n or n == o:
        return True
    # 新名里含原名（或反过来）→ 基本就是"加长版近义词"，丢掉
    if len(o) >= 2 and (o in n or n in o):
        return True
    return False


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--tables")
    ap.add_argument("--batch", type=int, default=25)
    ap.add_argument("--max-batches", type=int, default=8)
    ap.add_argument("--dry", action="store_true")
    a = ap.parse_args()

    tables = a.tables.split(",") if a.tables else [
        t[:-5] for t in sorted(os.listdir(MEME_DIR)) if t.endswith(".json")]
    # 先做"剩得最多"的表，免得额度被小表吃掉、大表永远轮不上
    if not a.tables:
        tables = sorted(tables, key=lambda t: -len(todo_for(t)))

    total_added = 0
    batches = 0
    for table in tables:
        rows = todo_for(table)
        if not rows:
            continue
        print("\n=== %s：还剩 %d 条" % (table, len(rows)))
        mp = os.path.join(MEME_DIR, table + ".json")
        meme = load(mp)
        van = load(os.path.join(REF_DIR, "zhs__%s.json" % table))

        # 已有新名的集合（防重名）
        used = {}
        for k, x in meme.items():
            used.setdefault(x, []).append(van.get(k, ""))

        added, rejects = 0, []
        while rows and batches < a.max_batches:
            chunk, rows = rows[:a.batch], rows[a.batch:]
            batches += 1
            print("   批次 %d：%d 条…" % (batches, len(chunk)), end=" ", flush=True)
            got = ask(chunk)
            print("拿到 %d" % len(got))
            for i, r in enumerate(chunk, 1):
                new = (got.get(i) or "").strip()
                old, key = r["vanilla"].strip(), r["key"]
                if looks_synonym(old, new):
                    if new: rejects.append((old, new))
                    continue
                if new in used and old not in used[new]:
                    rejects.append((old, new + "（重名）"))
                    continue
                if not a.dry:
                    meme[key] = new
                used.setdefault(new, []).append(old)
                added += 1

        if added and not a.dry:
            shutil.copy2(mp, os.path.join(HERE, "_backup_%s.json" % table))
            io.open(mp, "w", encoding="utf-8", newline="\n").write(
                json.dumps(meme, ensure_ascii=False, indent=2) + "\n")
        total_added += added
        print("   → 写入 %d 条（剔掉近义词/重名 %d 条）" % (added, len(rejects)))

    print("\n本轮共写入 %d 条，调模型 %d 次" % (total_added, batches))
    if total_added and not a.dry:
        subprocess.run([sys.executable, GALLERY], check=False)
    if batches >= a.max_batches:
        print("（达到 --max-batches 上限，剩下的下一轮继续）")
    elif total_added == 0:
        print("（没有可做的了，或模型没给结果）")


if __name__ == "__main__":
    main()
