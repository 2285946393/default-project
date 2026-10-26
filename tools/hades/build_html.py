"""把导出的 CSV 打包成一个可搜索的 HTML 总览页。

用法：python build_html.py
输出：D:/work/AI产出/Hades反编译/数值总览.html
"""

import csv
import glob
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")

OUT = r"D:/work/AI产出/Hades反编译"


def load(csv_path):
    if not os.path.exists(csv_path):
        return []
    with open(csv_path, encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


# 优先展示的字段关键词（命中越靠前越重要）
PRIORITY = [
    "God", "Slot", "Icon", "IsDuoBoon", "DisplayName", "Name",
    "MaxHealth", "Health", "Damage", "BaseDamage", "Speed",
    "Cost", "Price", "Multiplier", "RarityLevels.Common.Multiplier",
    "Cooldown", "Duration", "Range", "ProjectileSpeed",
]

# 噪声字段：镜头抖动、音效、动画、台词这类，不进表
NOISE = [
    "HitSimSlow", "CameraMotion", "Screenshake", "Rumble", "Sound", "Cue",
    "Animation", "Anim", "Color[", "Offset", "Frame", "VFX", "FX",
    "Presentation", "Fade", "Zoom", "Glow", "Trail", "Particle",
    "TextLine", "VoiceLine", "Speech", "PriorityPickup", "Requirement",
    "Requirements", "Condition", "Trigger", "Event", "Callback",
    "Function", "Script", "Loop", "Weight", "Chance", "Probability",
]


def is_noise(col):
    return any(n.lower() in col.lower() for n in NOISE)


def depth_of(col):
    """字段层级：a.b.c -> 2，a[1].b -> 2。"""
    return col.count(".") + col.count("[")


def score(col, counts):
    """列重要度：优先关键词 > 层级浅 > 填充率高。"""
    s = 0.0
    for i, key in enumerate(PRIORITY):
        if col.lower() == key.lower():
            s += 2000 - i * 2
            break
    else:
        for i, key in enumerate(PRIORITY):
            if key.lower() in col.lower():
                s += 800 - i
                break
    # 层级越浅越重要
    s -= depth_of(col) * 60
    # 填充率
    s += counts.get(col, 0) * 0.5
    return s


def slim(rows, keep=14):
    """只保留有用的列，砍掉噪声、深层嵌套和稀疏列。"""
    if not rows:
        return [], []
    counts = {}
    for r in rows:
        for k, v in r.items():
            if v not in ("", None):
                counts[k] = counts.get(k, 0) + 1

    cols = ["条目"]
    rest = [
        k for k in counts
        if k != "条目"
        and not is_noise(k)
        and depth_of(k) <= 2
        and counts[k] >= max(1, len(rows) // 10)
    ]
    rest.sort(key=lambda k: -score(k, counts))
    cols += rest[:keep]
    out = [{c: r.get(c, "") for c in cols} for r in rows]
    return cols, out


def main():
    datasets = {}
    for path in sorted(glob.glob(os.path.join(OUT, "*.csv"))):
        name = os.path.basename(path)[:-4]
        if "对话" in name or "界面文本" in name:
            continue   # 单独成页
        if "封包资源清单" in name:
            continue   # 33 万行，单独成页
        rows = load(path)
        if not rows:
            continue
        if "对话" in name or "界面文本" in name:
            # 对话表不裁列，全保留
            cols = list(rows[0].keys())
            slimmed = rows
        else:
            cols, slimmed = slim(rows)
        datasets[name] = {"cols": cols, "rows": slimmed}
        print(f"  {name:26} {len(slimmed):4} 行 / {len(cols):2} 列")

    payload = json.dumps(datasets, ensure_ascii=False)

    html = """<!doctype html>
<html lang="zh">
<head>
<meta charset="utf-8">
<title>Hades 1 / 2 数值总览</title>
<style>
*{box-sizing:border-box}
body{margin:0;background:#12100f;color:#e8ddd4;font-family:"Microsoft YaHei",system-ui,sans-serif;
     display:flex;flex-direction:column;height:100vh;overflow:hidden}
header{padding:12px 18px;background:#1c1815;border-bottom:1px solid #332a24;flex:none;
       display:flex;align-items:center;gap:14px;flex-wrap:wrap}
h1{font-size:15px;margin:0;font-weight:600;color:#e8a33d;letter-spacing:.5px}
.tabs{display:flex;gap:6px;flex-wrap:wrap}
.tab{background:#241f1b;border:1px solid #3a2f27;color:#b8a496;padding:5px 12px;
     border-radius:6px;font-size:12.5px;cursor:pointer;font-family:inherit}
.tab:hover{background:#2e2721;color:#e8ddd4}
.tab.on{background:#5c3a1a;border-color:#8a5a24;color:#ffd79a}
#q{flex:1;min-width:200px;background:#241f1b;border:1px solid #3a2f27;color:#e8ddd4;
   padding:7px 12px;border-radius:7px;font-size:13px;font-family:inherit;outline:none}
#q:focus{border-color:#8a5a24}
#q::placeholder{color:#6d5f55}
#count{font-size:12px;color:#8a7a6d;flex:none}
main{flex:1;overflow:auto}
table{border-collapse:collapse;width:100%;font-size:12.5px}
th{position:sticky;top:0;background:#241f1b;color:#e8a33d;text-align:left;
   padding:8px 12px;border-bottom:1px solid #3a2f27;white-space:nowrap;
   font-weight:600;cursor:pointer;user-select:none}
th:hover{background:#2e2721}
th.sorted::after{content:" ▾";color:#8a5a24}
td{padding:6px 12px;border-bottom:1px solid #241f1b;vertical-align:top}
tr:hover td{background:#1c1815}
td:first-child{color:#e8c9a0;font-family:ui-monospace,Consolas,monospace;white-space:nowrap}
td.num{color:#8fd4a8;font-family:ui-monospace,Consolas,monospace;text-align:right}
.empty{padding:60px;text-align:center;color:#5a4f47}
mark{background:#5c3a1a;color:#ffd79a;padding:0 2px;border-radius:2px}
footer{padding:8px 18px;background:#1c1815;border-top:1px solid #332a24;
       font-size:11.5px;color:#6d5f55;flex:none}
</style>
</head>
<body>
<header>
  <h1>Hades 数值总览</h1>
  <div class="tabs" id="tabs"></div>
  <input id="q" type="search" placeholder="搜索条目 / 字段值…">
  <span id="count"></span>
</header>
<main id="main"></main>
<footer>数据来自游戏自带的明文 Lua 脚本 · 点击表头排序</footer>
<script>
const DATA = __PAYLOAD__;
let cur = Object.keys(DATA)[0];
let q = "";
let sortCol = null, sortDir = 1;

const tabsEl = document.getElementById("tabs");
const mainEl = document.getElementById("main");
const qEl = document.getElementById("q");
const countEl = document.getElementById("count");

function isNum(v){
  return v !== "" && v != null && !isNaN(v) && String(v).trim() !== "";
}

function buildTabs(){
  tabsEl.innerHTML = "";
  for(const name of Object.keys(DATA)){
    const b = document.createElement("button");
    b.className = "tab" + (name === cur ? " on" : "");
    b.textContent = name.replace(/^Hades/, "H").replace(/-/g, " ");
    b.onclick = () => { cur = name; sortCol = null; buildTabs(); render(); };
    tabsEl.appendChild(b);
  }
}

function esc(s){
  return String(s).replace(/[&<>"]/g, c => ({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;"}[c]));
}

function hl(s){
  const t = esc(s);
  if(!q) return t;
  const re = new RegExp("(" + q.replace(/[.*+?^${}()|[\\]\\\\]/g, "\\\\$&") + ")", "gi");
  return t.replace(re, "<mark>$1</mark>");
}

function render(){
  const ds = DATA[cur];
  if(!ds){ mainEl.innerHTML = '<div class="empty">没有数据</div>'; return; }
  const {cols, rows} = ds;

  let list = rows;
  if(q){
    const ql = q.toLowerCase();
    list = rows.filter(r => cols.some(c => String(r[c] || "").toLowerCase().includes(ql)));
  }
  if(sortCol){
    list = list.slice().sort((a,b) => {
      const av = a[sortCol], bv = b[sortCol];
      if(isNum(av) && isNum(bv)) return (parseFloat(av) - parseFloat(bv)) * sortDir;
      return String(av).localeCompare(String(bv)) * sortDir;
    });
  }

  countEl.textContent = list.length + " / " + rows.length + " 条";

  let h = "<table><thead><tr>";
  for(const c of cols){
    const cls = sortCol === c ? "sorted" : "";
    h += `<th class="${cls}" data-col="${esc(c)}">${esc(c)}</th>`;
  }
  h += "</tr></thead><tbody>";
  for(const r of list){
    h += "<tr>";
    for(const c of cols){
      const v = r[c] ?? "";
      const cls = isNum(v) ? "num" : "";
      h += `<td class="${cls}">${hl(v)}</td>`;
    }
    h += "</tr>";
  }
  h += "</tbody></table>";
  mainEl.innerHTML = h;

  mainEl.querySelectorAll("th").forEach(th => {
    th.onclick = () => {
      const c = th.dataset.col;
      if(sortCol === c) sortDir = -sortDir;
      else { sortCol = c; sortDir = 1; }
      render();
    };
  });
}

qEl.oninput = () => { q = qEl.value.trim(); render(); };
buildTabs();
render();
</script>
</body>
</html>
"""
    html = html.replace("__PAYLOAD__", payload)
    out = os.path.join(OUT, "数值总览.html")
    with open(out, "w", encoding="utf-8") as f:
        f.write(html)
    print(f"\n生成 {out}")
    print(f"体积 {os.path.getsize(out)/1024:.0f} KB")


if __name__ == "__main__":
    main()
