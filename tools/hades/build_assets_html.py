"""生成 Hades II 封包资源浏览器。

33 万条资源记录不能内嵌，做成 JSON + 搜索页。
"""

import csv
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")

OUT = r"D:/work/AI产出/Hades反编译"


def main():
    src = os.path.join(OUT, "Hades2-封包资源清单.csv")
    if not os.path.exists(src):
        print("先跑 scan_packages.py")
        return

    with open(src, encoding="utf-8-sig") as f:
        rows = list(csv.DictReader(f))

    # 压缩成紧凑结构：{封包: [[分类, 路径], ...]}
    by_pkg = {}
    for r in rows:
        pkg = r["封包"]
        by_pkg.setdefault(pkg, []).append([r["分类"], r["路径"]])

    data_path = os.path.join(OUT, "_封包资源.json")
    with open(data_path, "w", encoding="utf-8") as f:
        json.dump(by_pkg, f, ensure_ascii=False, separators=(",", ":"))
    mb = os.path.getsize(data_path) / 1048576
    print(f"数据 {mb:.1f} MB，{len(rows)} 条，{len(by_pkg)} 个封包")

    html = """<!doctype html>
<html lang="zh">
<head>
<meta charset="utf-8">
<title>Hades II 资源浏览器</title>
<style>
*{box-sizing:border-box}
body{margin:0;background:#0f1418;color:#d8e4ec;font-family:"Microsoft YaHei",system-ui,sans-serif;
     display:flex;flex-direction:column;height:100vh;overflow:hidden}
header{padding:12px 18px;background:#161d23;border-bottom:1px solid #26343d;flex:none;
       display:flex;align-items:center;gap:12px;flex-wrap:wrap}
h1{font-size:15px;margin:0;font-weight:600;color:#4fc3d9}
#q{flex:1;min-width:240px;background:#1c252c;border:1px solid #2d3d47;color:#d8e4ec;
   padding:7px 12px;border-radius:7px;font-size:13px;font-family:inherit;outline:none}
#q:focus{border-color:#4fc3d9}
#q::placeholder{color:#5a6f7d}
#count{font-size:12px;color:#6b8291;flex:none}
.bar{padding:8px 18px;background:#131a1f;border-bottom:1px solid #26343d;flex:none;
     display:flex;gap:6px;flex-wrap:wrap;align-items:center;font-size:12px}
.chip{background:#1c252c;border:1px solid #2d3d47;color:#8fa5b3;padding:3px 10px;
      border-radius:5px;font-size:11.5px;cursor:pointer;font-family:inherit}
.chip:hover{background:#25313a;color:#d8e4ec}
.chip.on{background:#1e4a55;border-color:#4fc3d9;color:#8ee4f2}
main{flex:1;overflow:auto;font-size:12.5px}
table{border-collapse:collapse;width:100%}
th{position:sticky;top:0;background:#161d23;color:#4fc3d9;text-align:left;padding:8px 12px;
   border-bottom:1px solid #2d3d47;font-weight:600;white-space:nowrap}
td{padding:5px 12px;border-bottom:1px solid #161d23;vertical-align:top}
tr:hover td{background:#131a1f}
td:first-child{color:#6b8291;white-space:nowrap;width:110px}
td:last-child{font-family:ui-monospace,Consolas,monospace;color:#a8bcc8;word-break:break-all}
.empty{padding:60px;text-align:center;color:#4a5c68}
mark{background:#1e4a55;color:#8ee4f2;padding:0 2px;border-radius:2px}
#more{display:block;margin:14px auto;background:#1c252c;color:#8ee4f2;border:1px solid #2d3d47;
      border-radius:7px;padding:9px 22px;font-size:13px;cursor:pointer;font-family:inherit}
#more:hover{background:#25313a}
footer{padding:8px 18px;background:#161d23;border-top:1px solid #26343d;
       font-size:11.5px;color:#5a6f7d;flex:none}
</style>
</head>
<body>
<header>
  <h1>Hades II 资源浏览器</h1>
  <input id="q" type="search" placeholder="搜资源路径…（如 Portraits、BoonGet、Chronos）">
  <span id="count"></span>
</header>
<div class="bar" id="cats"></div>
<main id="main"><div class="empty">加载中…</div></main>
<footer>数据来自 Content/Packages 的 manifest 索引 · 33 万条资源记录</footer>
<script>
let DATA = {};
let flat = [];
let q = "";
let cat = "";
let shown = 0;
const PAGE = 300;

const mainEl = document.getElementById("main");
const qEl = document.getElementById("q");
const countEl = document.getElementById("count");
const catsEl = document.getElementById("cats");

function esc(s){return String(s).replace(/[&<>"]/g,c=>({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;"}[c]));}
function hl(s){
  const t = esc(s);
  if(!q) return t;
  try{
    const re = new RegExp("("+q.replace(/[.*+?^${}()|[\\]\\\\]/g,"\\\\$&")+")","gi");
    return t.replace(re,"<mark>$1</mark>");
  }catch(e){return t;}
}

function filtered(){
  let list = flat;
  if(cat) list = list.filter(r => r[0] === cat);
  if(q){
    const ql = q.toLowerCase();
    list = list.filter(r => r[1].toLowerCase().includes(ql) || r[0].toLowerCase().includes(ql));
  }
  return list;
}

function render(){
  const list = filtered();
  countEl.textContent = list.length.toLocaleString() + " 条";
  shown = Math.min(PAGE, list.length);
  paint(list);
}

function paint(list){
  let h = '<table><thead><tr><th>类型</th><th>路径</th></tr></thead><tbody>';
  const slice = list.slice(0, shown);
  for(const r of slice){
    h += "<tr><td>" + esc(r[0]) + "</td><td>" + hl(r[1]) + "</td></tr>";
  }
  h += "</tbody></table>";
  if(!list.length) h = '<div class="empty">没找到</div>';
  mainEl.innerHTML = h;
  if(shown < list.length){
    const b = document.createElement("button");
    b.id = "more";
    b.textContent = "再加载 " + PAGE + " 条（还有 " + (list.length - shown).toLocaleString() + "）";
    b.onclick = () => { shown += PAGE; paint(list); };
    mainEl.appendChild(b);
  }
}

function buildCats(){
  const counts = {};
  for(const r of flat) counts[r[0]] = (counts[r[0]] || 0) + 1;
  const sorted = Object.entries(counts).sort((a,b) => b[1] - a[1]).slice(0, 14);
  catsEl.innerHTML = "";
  const all = document.createElement("button");
  all.className = "chip" + (cat === "" ? " on" : "");
  all.textContent = "全部 " + flat.length.toLocaleString();
  all.onclick = () => { cat = ""; buildCats(); render(); };
  catsEl.appendChild(all);
  for(const [name, n] of sorted){
    const b = document.createElement("button");
    b.className = "chip" + (cat === name ? " on" : "");
    b.textContent = name + " " + n.toLocaleString();
    b.onclick = () => { cat = name; buildCats(); render(); };
    catsEl.appendChild(b);
  }
}

qEl.oninput = () => { q = qEl.value.trim(); render(); };

fetch("_封包资源.json").then(r => r.json()).then(d => {
  DATA = d;
  for(const [pkg, arr] of Object.entries(d)){
    for(const item of arr) flat.push(item);
  }
  buildCats();
  render();
}).catch(e => {
  mainEl.innerHTML = '<div class="empty">数据加载失败：' + e.message + "</div>";
});
</script>
</body>
</html>
"""
    out = os.path.join(OUT, "资源浏览器.html")
    with open(out, "w", encoding="utf-8") as f:
        f.write(html)
    print(f"页面 -> {out}")


if __name__ == "__main__":
    main()
