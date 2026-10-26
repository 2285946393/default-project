# -*- coding: utf-8 -*-
"""
用 DSH 免费额度给「梗化尖塔」批量起草新梗名。

用法：
    python tools/meme_fill.py <表名> [起始] [条数]
    python tools/meme_fill.py monsters 0 25

流程：读 tools/_todo_memes.json（由 dump 脚本生成）→ 挑一批 → 写提示词 →
      调 `node D:\\software\\dsh-home\\lib\\free-model.mjs`（不花钱）→ 存草稿到
      tools/_draft_<表>_<起始>.json

⚠️ 免费模型只是**起草**：它会给"换了近义词但不是梗"的答案，必须人工筛。
"""
import io
import json
import os
import re
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
TODO = os.path.join(HERE, "_todo_memes.json")
FREE = r"D:\software\dsh-home\lib\free-model.mjs"
# 更强的免费后端：WorkBuddy 国际版的 deepseek-v4.1-flash（用户说这个免费）。
# 路径：仓库根 /tools/wb.mjs（HERE 往上三层是仓库根）
WB = os.path.normpath(os.path.join(HERE, "..", "..", "..", "tools", "wb.mjs"))

PROMPT = """你在给《杀戮尖塔2》的中文"梗化"mod 起名。这个 mod 把游戏里的名字全换成中文大众梗。

【铁律】
1. 每个新名字必须是一个梗（影视台词 / 广告词 / 动漫 / 游戏 / 网络热词 / 名人语录 / 成语典故 / 小品相声），
   不能只是换个近义词。错误示例：飞镖 改成 夺命飞镖。
2. 梗要全国大众都懂，不要用只玩尖塔的人才知道的圈内梗。
3. 尽量和原名字数接近（原名 2 字就给 2-4 字），要像名字，不要写成一句话。
4. 原版里如果有大括号占位符（花括号包着英文），必须原样保留；方括号标签也要原样保留。
5. 只有原名**本身就是梗、或者已经足够好玩**时才照抄；**不要因为一时想不出就照抄**——
   这时可以退一步，改成一个「更接地气、更有画面感的叫法」也算合格
   （已采用的例子：骨茶→骨头汤、音乐盒→八音盒、尘封魔典→吃灰的书、小血瓶→续命血瓶、宾邦→冰棒）。

【输出格式】一行一条，只输出 编号 + 制表符 + 新名字，不要解释、不要开场白。

【要起名的条目】
{items}
"""


def main() -> None:
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    table = sys.argv[1]
    start = int(sys.argv[2]) if len(sys.argv) > 2 else 0
    count = int(sys.argv[3]) if len(sys.argv) > 3 else 25

    todo = json.load(io.open(TODO, encoding="utf-8"))
    rows = [r for r in todo if r["table"] == table][start:start + count]
    if not rows:
        sys.exit("这个表没有待办条目（或起始越界）")

    items = "\n".join("%d. %s = %s" % (i + 1, r["key"], r["vanilla"]) for i, r in enumerate(rows))
    prompt = PROMPT.replace("{items}", items)

    # 首选 WorkBuddy 国际版（deepseek-v4.1-flash，免费且更聪明）；不行再退回 DSH 免费车道。
    if os.path.exists(WB):
        p = subprocess.run(["node", WB, "--stdin", "--region", "global",
                            "--model", "deepseek-v4.1-flash"],
                           input=prompt, capture_output=True, text=True,
                           encoding="utf-8", errors="replace", timeout=300)
    else:
        p = subprocess.run(["node", FREE, "--stdin", "--model", "space-bunny-free", "--timeout", "240"],
                           input=prompt, capture_output=True, text=True,
                           encoding="utf-8", errors="replace")
    out = (p.stdout or "") + (p.stderr or "")

    got = {}
    for line in out.splitlines():
        m = re.match(r"\s*(\d+)\s*[\t ]\s*(.+?)\s*$", line)
        if m:
            got[int(m.group(1))] = m.group(2)

    draft = [{"table": r["table"], "key": r["key"], "vanilla": r["vanilla"],
              "new": got.get(i + 1, "")} for i, r in enumerate(rows)]
    dest = os.path.join(HERE, "_draft_%s_%d.json" % (table, start))
    io.open(dest, "w", encoding="utf-8").write(json.dumps(draft, ensure_ascii=False, indent=1))

    ok = sum(1 for d in draft if d["new"] and d["new"] != d["vanilla"])
    print("%s[%d:%d] 共 %d 条，免费模型给了 %d 条 -> %s" %
          (table, start, start + count, len(draft), ok, os.path.basename(dest)))


if __name__ == "__main__":
    main()
