# -*- coding: utf-8 -*-
"""
生成「梗化尖塔 · 对照图鉴」页面（单文件 HTML，可搜可筛，零依赖）。

用法（在任意目录）：
    python projects/meme-spire/tools/gen_meme_gallery.py

做三件事：
  1. 读 MemeSpire/localization/zhs/*.json（我们写的梗）
  2. 读 reference/zhs__*.json（原版中文，抽出来的）
  3. 只挑**真的改过**的条目，生成 原版 → 梗 的对照页 → pages/梗化尖塔图鉴.html

⚠️ 改完梗表之后重跑一次即可刷新页面。
⚠️ 新增了表（localization 里多一个 json）会自动出现，不用改这个脚本；
   但表的中文名要去 TABLE_NAMES 里补一条，否则显示成英文表名。
"""
import html
import io
import json
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))          # projects/meme-spire/tools
PROJ = os.path.dirname(HERE)                                # projects/meme-spire
ROOT = os.path.dirname(os.path.dirname(PROJ))               # .../Default Project

MEME_DIR = os.path.join(PROJ, "MemeSpire", "localization", "zhs")
REF_DIR = os.path.join(PROJ, "reference")
OUT_HTML = os.path.join(ROOT, "pages", "梗化尖塔图鉴.html")

# 表名 → 中文分类名（显示用）。新表没在这里就显示英文表名。
TABLE_NAMES = {
    "cards": "卡牌", "relics": "遗物", "monsters": "怪物", "powers": "状态",
    "events": "事件", "ancients": "先古对白", "encounters": "遭遇", "potions": "药水",
    "enchantments": "附魔", "achievements": "成就", "badges": "徽章", "characters": "角色",
    "epochs": "纪年", "timeline": "时间线", "acts": "幕名", "game_modes": "模式",
    "orbs": "充能球", "intents": "意图", "bestiary": "图鉴", "map": "地图",
    "merchant_room": "商人", "rest_site_ui": "篝火", "game_over_screen": "死亡结算",
    "ascension": "进阶", "afflictions": "苦痛", "modifiers": "修正",
    "card_selection": "选牌界面", "card_reward_ui": "卡牌奖励", "potion_lab": "药水实验室",
    "relic_collection": "遗物收藏", "inspect_relic_screen": "遗物检视",
    "static_hover_tips": "悬浮提示", "combat_messages": "战斗提示",
}

# 这些 key 的后缀是"说明类"文字，我们不改（改了就没人看得懂牌了）——图鉴里不列
SKIP_SUFFIX = (".description",)

# 渲染前把游戏用的 BBCode 标签去掉，只看文字
TAG_RE = re.compile(r"\[/?(?:b|i|u|s|sine|red|gold|blue|green|purple|orange|aqua|thinky_dots|font_size[^\]]*|lb|rb)\]")


def plain(s: str) -> str:
    return TAG_RE.sub("", s or "").replace("\\n", " ").strip()


def main() -> None:
    tables = []
    total = 0

    for fn in sorted(os.listdir(MEME_DIR)):
        if not fn.endswith(".json"):
            continue
        table = fn[:-5]
        meme = json.load(io.open(os.path.join(MEME_DIR, fn), encoding="utf-8"))

        ref_path = os.path.join(REF_DIR, "zhs__%s.json" % table)
        vanilla = {}
        if os.path.exists(ref_path):
            try:
                vanilla = json.load(io.open(ref_path, encoding="utf-8"))
            except Exception as e:
                print("  !! 原版表读不了 %s: %s" % (table, e))

        rows = []
        for key, new in meme.items():
            if key.endswith(SKIP_SUFFIX):
                continue
            old = vanilla.get(key, "")
            new_p, old_p = plain(new), plain(old)
            if not new_p or new_p == old_p:
                continue                      # 没改过的就不列
            rows.append({"old": old_p, "new": new_p, "key": key})

        if rows:
            tables.append({"table": table, "name": TABLE_NAMES.get(table, table), "rows": rows})
            total += len(rows)
            print("  %-18s %4d 条" % (TABLE_NAMES.get(table, table), len(rows)))

    tables.sort(key=lambda t: -len(t["rows"]))
    payload = json.dumps(tables, ensure_ascii=False)

    doc = HTML_TEMPLATE.replace("__DATA__", payload) \
                        .replace("__TOTAL__", str(total)) \
                        .replace("__TABLES__", str(len(tables)))
    io.open(OUT_HTML, "w", encoding="utf-8", newline="\n").write(doc)
    print("\n共 %d 张表、%d 条对照" % (len(tables), total))
    print("已生成：%s" % OUT_HTML)


HTML_TEMPLATE = r"""<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>梗化尖塔 · 对照图鉴</title>
<style>
  :root{
    --bg:#14101a; --bg2:#1d1726; --card:#241d31; --line:#3a2f4d;
    --ink:#f3ecff; --dim:#a294bd; --hi:#ffb7d5; --hi2:#ffd98a; --acc:#8ce6c8;
  }
  *{box-sizing:border-box}
  body{margin:0;background:radial-gradient(1200px 600px at 50% -10%,#2a2038,transparent),var(--bg);
       color:var(--ink);font:15px/1.6 "Segoe UI","PingFang SC","Microsoft YaHei",sans-serif;}
  header{padding:38px 20px 18px;text-align:center}
  h1{margin:0;font-size:30px;letter-spacing:2px;
     background:linear-gradient(90deg,var(--hi),var(--hi2));-webkit-background-clip:text;background-clip:text;color:transparent}
  .sub{color:var(--dim);margin-top:8px;font-size:14px}
  .sub b{color:var(--acc)}
  .bar{position:sticky;top:0;z-index:5;backdrop-filter:blur(10px);
       background:rgba(20,16,26,.82);border-bottom:1px solid var(--line);padding:12px 16px}
  .search{display:flex;gap:10px;max-width:1100px;margin:0 auto}
  input[type=search]{flex:1;padding:11px 14px;border-radius:12px;border:1px solid var(--line);
       background:var(--bg2);color:var(--ink);font-size:15px;outline:none}
  input[type=search]:focus{border-color:var(--hi)}
  .tabs{display:flex;flex-wrap:wrap;gap:8px;max-width:1100px;margin:10px auto 0;padding:0 0 2px}
  .tab{cursor:pointer;border:1px solid var(--line);background:var(--bg2);color:var(--dim);
       padding:6px 12px;border-radius:999px;font-size:13px;white-space:nowrap;transition:.15s}
  .tab:hover{color:var(--ink);border-color:var(--hi)}
  .tab.on{background:linear-gradient(90deg,#ff9fc4,#ffcf8a);color:#2a1c2e;border-color:transparent;font-weight:700}
  .tab i{font-style:normal;opacity:.7;margin-left:5px;font-size:12px}
  main{max-width:1100px;margin:0 auto;padding:18px 16px 60px}
  .grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(268px,1fr));gap:12px}
  .item{background:var(--card);border:1px solid var(--line);border-radius:14px;padding:13px 15px;
        transition:.15s}
  .item:hover{border-color:var(--hi);transform:translateY(-2px)}
  .old{color:var(--dim);font-size:12.5px;text-decoration:line-through;opacity:.75}
  .new{font-size:17px;font-weight:700;color:var(--hi2);margin-top:3px;word-break:break-word}
  .key{color:#6f6288;font-size:11px;margin-top:6px;font-family:ui-monospace,Consolas,monospace}
  .empty{color:var(--dim);text-align:center;padding:60px 0}
  .count{max-width:1100px;margin:0 auto;padding:10px 16px 0;color:var(--dim);font-size:13px}
  mark{background:rgba(255,183,213,.28);color:var(--hi);border-radius:3px;padding:0 2px}
  @media(max-width:520px){h1{font-size:23px}.old{font-size:12px}.new{font-size:16px}}
</style>
</head>
<body>
<header>
  <h1>梗化尖塔 · 对照图鉴</h1>
  <div class="sub">把游戏里<b>几乎所有文字</b>换成中文大众梗 —— 这里是「原版叫什么 → 现在叫什么」的对照。
    共 <b>__TABLES__</b> 张表 / <b>__TOTAL__</b> 条</div>
</header>
<div class="bar">
  <div class="search"><input id="q" type="search" placeholder="搜「原版名」或「梗名」，比如：痛击 / 一个大逼斗"></div>
  <div class="tabs" id="tabs"></div>
</div>
<div class="count" id="count"></div>
<main><div class="grid" id="grid"></div></main>
<script>
const DATA = __DATA__;
let cur = "__ALL__", kw = "";

const tabs = document.getElementById("tabs");
function buildTabs(){
  const all = [{table:"__ALL__",name:"全部",rows:DATA.flatMap(t=>t.rows)}].concat(DATA);
  tabs.innerHTML = "";
  all.forEach(t=>{
    const d = document.createElement("div");
    d.className = "tab" + (t.table===cur ? " on" : "");
    d.innerHTML = t.name + "<i>" + t.rows.length + "</i>";
    d.onclick = ()=>{ cur = t.table; buildTabs(); render(); };
    tabs.appendChild(d);
  });
}

function esc(s){ return s.replace(/[&<>"]/g, c=>({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;"}[c])); }
function hl(s){
  const e = esc(s);
  if(!kw) return e;
  return e.replace(new RegExp(kw.replace(/[.*+?^${}()|[\]\\]/g,"\\$&"), "gi"), m=>"<mark>"+m+"</mark>");
}

function render(){
  const src = cur==="__ALL__" ? DATA.flatMap(t=>t.rows) : (DATA.find(t=>t.table===cur)||{rows:[]}).rows;
  const k = kw.trim().toLowerCase();
  const rows = k ? src.filter(r=>r.old.toLowerCase().includes(k) || r.new.toLowerCase().includes(k)) : src;
  document.getElementById("count").textContent = "显示 " + rows.length + " 条" + (k ? "（搜索：" + kw + "）" : "");
  const grid = document.getElementById("grid");
  if(!rows.length){ grid.innerHTML = '<div class="empty">没有匹配的条目 🐟</div>'; return; }
  grid.innerHTML = rows.slice(0, 3000).map(r=>
    '<div class="item"><div class="old">' + hl(r.old || "（原版没有）") + '</div>' +
    '<div class="new">' + hl(r.new) + '</div>' +
    '<div class="key">' + esc(r.key) + '</div></div>').join("");
}

document.getElementById("q").addEventListener("input", e=>{ kw = e.target.value; render(); });
buildTabs(); render();
</script>
</body>
</html>
"""


if __name__ == "__main__":
    main()
