"""生成对话检索页（单独一个 HTML，按需加载数据）。

对话数据 12 MB，内嵌会让浏览器卡死，所以：
- 生成一个 HTML，数据外置成 JSON
- 用 fetch 加载，打开秒开
"""

import csv
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")

OUT = r"D:/work/AI产出/Hades反编译"

SOURCES = [
    ("Hades1", "Hades1-对话中英对照.csv", "Hades1-界面文本中英对照.csv"),
    ("Hades2", "Hades2-对话中英对照.csv", "Hades2-界面文本中英对照.csv"),
]


def load_csv(path):
    if not os.path.exists(path):
        return []
    with open(path, encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


def main():
    payload = {}
    for gen, dlg, ui in SOURCES:
        rows = load_csv(os.path.join(OUT, dlg))
        if rows:
            payload[f"{gen}-对话"] = [
                {
                    "id": r.get("Id", ""),
                    "sp": r.get("Speaker", ""),
                    "ev": r.get("Event", ""),
                    "zh": r.get("Text", ""),
                    "en": r.get("English", ""),
                    "src": r.get("来源", ""),
                }
                for r in rows
            ]
        urows = load_csv(os.path.join(OUT, ui))
        if urows:
            payload[f"{gen}-界面文本"] = [
                {
                    "id": r.get("Id", ""),
                    "sp": r.get("Speaker", ""),
                    "ev": r.get("Event", ""),
                    "zh": r.get("Text", ""),
                    "en": r.get("English", ""),
                    "src": r.get("来源", ""),
                }
                for r in urows
            ]

    data_path = os.path.join(OUT, "_对话数据.json")
    with open(data_path, "w", encoding="utf-8") as f:
        json.dump(payload, f, ensure_ascii=False)
    size_mb = os.path.getsize(data_path) / 1048576
    print(f"数据文件 {size_mb:.1f} MB")

    html = """<!doctype html>
<html lang="zh">
<head>
<meta charset="utf-8">
<title>Hades 对话检索</title>
<style>
*{box-sizing:border-box}
body{margin:0;background:#12100f;color:#e8ddd4;font-family:"Microsoft YaHei",system-ui,sans-serif;
     display:flex;flex-direction:column;height:100vh;overflow:hidden}
header{padding:12px 18px;background:#1c1815;border-bottom:1px solid #332a24;flex:none;
       display:flex;align-items:center;gap:12px;flex-wrap:wrap}
h1{font-size:15px;margin:0;font-weight:600;color:#e8a33d}
.tabs{display:flex;gap:6px;flex-wrap:wrap}
.tab{background:#241f1b;border:1px solid #3a2f27;color:#b8a496;padding:5px 12px;
     border-radius:6px;font-size:12.5px;cursor:pointer;font-family:inherit}
.tab:hover{background:#2e2721;color:#e8ddd4}
.tab.on{background:#5c3a1a;border-color:#8a5a24;color:#ffd79a}
#q{flex:1;min-width:220px;background:#241f1b;border:1px solid #3a2f27;color:#e8ddd4;
   padding:7px 12px;border-radius:7px;font-size:13px;font-family:inherit;outline:none}
#q:focus{border-color:#8a5a24}
#q::placeholder{color:#6d5f55}
#count{font-size:12px;color:#8a7a6d;flex:none}
main{flex:1;overflow:auto;padding:14px 18px}
.item{background:#1a1614;border:1px solid #2a221d;border-radius:8px;padding:11px 14px;
      margin-bottom:10px}
.item:hover{border-color:#4a3a2c}
.meta{font-size:11.5px;color:#7d6b5e;margin-bottom:6px;display:flex;gap:10px;flex-wrap:wrap}
.sp{color:#e8a33d;font-weight:600}
.zh{font-size:13.5px;line-height:1.65;color:#f0e6dc}
.en{font-size:12.5px;line-height:1.6;color:#8a7a6d;margin-top:6px;
    border-left:2px solid #3a2f27;padding-left:10px;white-space:pre-wrap}
.empty{padding:60px;text-align:center;color:#5a4f47}
mark{background:#5c3a1a;color:#ffd79a;padding:0 2px;border-radius:2px}
footer{padding:8px 18px;background:#1c1815;border-top:1px solid #332a24;
       font-size:11.5px;color:#6d5f55;flex:none}
#more{display:block;margin:14px auto;background:#3a2b33;color:#f0b8cc;border:1px solid #4a3740;
      border-radius:7px;padding:9px 22px;font-size:13px;cursor:pointer;font-family:inherit}
#more:hover{background:#4a3740}
</style>
</head>
<body>
<header>
  <h1>对话检索</h1>
  <div class="tabs" id="tabs"></div>
  <input id="q" type="search" placeholder="搜中文 / 英文 / 说话人 / 事件…">
  <span id="count"></span>
</header>
<main id="main"><div class="empty">加载中…</div></main>
<footer>Hades 1 / 2 全部对白 · 中英对照 · 支持关键词搜索</footer>
<script>
let DATA = {};
let cur = "";
let q = "";
let shown = 0;
const PAGE = 120;

const tabsEl = document.getElementById("tabs");
const mainEl = document.getElementById("main");
const qEl = document.getElementById("q");
const countEl = document.getElementById("count");

function esc(s){
  return String(s).replace(/[&<>"]/g, c => ({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;"}[c]));
}
function hl(s){
  const t = esc(s);
  if(!q) return t;
  try{
    const re = new RegExp("(" + q.replace(/[.*+?^${}()|[\\]\\\\]/g, "\\\\$&") + ")", "gi");
    return t.replace(re, "<mark>$1</mark>");
  }catch(e){ return t; }
}

function buildTabs(){
  tabsEl.innerHTML = "";
  for(const name of Object.keys(DATA)){
    const b = document.createElement("button");
    b.className = "tab" + (name === cur ? " on" : "");
    b.textContent = name.replace(/-/g, " ") + " (" + DATA[name].length + ")";
    b.onclick = () => { cur = name; shown = 0; buildTabs(); render(); };
    tabsEl.appendChild(b);
  }
}

function filtered(){
  const all = DATA[cur] || [];
  if(!q) return all;
  const ql = q.toLowerCase();
  return all.filter(r =>
    (r.zh || "").toLowerCase().includes(ql) ||
    (r.en || "").toLowerCase().includes(ql) ||
    (r.sp || "").toLowerCase().includes(ql) ||
    (r.ev || "").toLowerCase().includes(ql) ||
    (r.id || "").toLowerCase().includes(ql)
  );
}

function render(){
  const list = filtered();
  countEl.textContent = list.length + " 条";
  shown = Math.min(PAGE, list.length);
  paint(list);
}

function paint(list){
  const slice = list.slice(0, shown);
  let h = "";
  for(const r of slice){
    h += '<div class="item"><div class="meta">';
    if(r.sp) h += '<span class="sp">' + esc(r.sp) + '</span>';
    if(r.id) h += "<span>" + esc(r.id) + "</span>";
    if(r.ev) h += "<span>" + esc(r.ev) + "</span>";
    if(r.src) h += "<span>" + esc(r.src) + "</span>";
    h += '</div><div class="zh">' + hl(r.zh) + "</div>";
    if(r.en) h += '<div class="en">' + hl(r.en) + "</div>";
    h += "</div>";
  }
  if(!list.length) h = '<div class="empty">没找到</div>';
  mainEl.innerHTML = h;
  if(shown < list.length){
    const b = document.createElement("button");
    b.id = "more";
    b.textContent = "再加载 " + Math.min(PAGE, list.length - shown) + " 条（还有 " + (list.length - shown) + "）";
    b.onclick = () => { shown += PAGE; paint(list); };
    mainEl.appendChild(b);
  }
}

qEl.oninput = () => { q = qEl.value.trim(); render(); };

fetch("_对话数据.json").then(r => r.json()).then(d => {
  DATA = d;
  cur = Object.keys(d)[0];
  buildTabs();
  render();
}).catch(e => {
  mainEl.innerHTML = '<div class="empty">数据加载失败：' + e.message + "<br>请确认 _对话数据.json 和本页在同一目录</div>";
});
</script>
</body>
</html>
"""
    out = os.path.join(OUT, "对话检索.html")
    with open(out, "w", encoding="utf-8") as f:
        f.write(html)
    print(f"页面 {out}")
    for k, v in payload.items():
        print(f"  {k}: {len(v)} 条")


if __name__ == "__main__":
    main()
