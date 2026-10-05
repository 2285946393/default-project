"""生成 pages/我的游戏统计.html。

数据来自 tools/_steam_playtime.json（由 tools/steam_playtime.py 生成）。
改完 Steam 库重跑：
    python tools/steam_playtime.py --json tools/_steam_playtime.json
    python tools/gen_game_stats.py
"""
import io
import json
import os
import sys
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import steam_shortcuts as ss  # noqa: E402

SRC = os.path.join(ROOT, "tools", "_steam_playtime.json")
OUT = os.path.join(ROOT, "pages", "我的游戏统计.html")


def load_steam_games():
    with io.open(SRC, encoding="utf-8") as f:
        rows = json.load(f)
    games = []
    for r in rows:
        name = r["name"]
        if name.startswith("APP "):
            name = "未知游戏（已下架）"
        games.append({
            "name": name,
            "hours": r["hours"],
            "minutes": r["minutes"],
            "last": r["last"],
            "appid": r["appid"],
            "installed": r.get("installed", False),
        })
    games.sort(key=lambda g: -g["minutes"])
    return games


def load_non_steam():
    try:
        _k, entries, _t = ss.load_vdf(ss.VDF)
    except Exception:
        return []
    out = []
    for e in entries:
        exe = ss.exe_of(e)
        if not exe:
            continue
        if "BaiduNetdiskDownload" in exe or "\\game\\" in exe or "\\gal\\" in exe:
            out.append({"name": e.get("AppName", "?"), "exe": os.path.basename(exe)})
    return out


def human_days(hours):
    return hours / 24.0


HTML = u"""<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>我的游戏统计</title>
<style>
:root{
  --pink:#ffb3c9; --pink-deep:#f2769a; --purple:#a98cf0; --ink:#3d3550;
  --muted:#8a82a0; --card:#ffffffcc; --line:#f0e6f5;
}
*{box-sizing:border-box}
body{
  margin:0; padding:0 16px 64px; color:var(--ink);
  font-family:"PingFang SC","Microsoft YaHei","Noto Sans SC",system-ui,sans-serif;
  background:
    radial-gradient(circle at 12% 8%,#ffe3ee 0%,transparent 45%),
    radial-gradient(circle at 88% 4%,#e8dcff 0%,transparent 42%),
    radial-gradient(circle at 50% 100%,#e2f2ff 0%,transparent 50%),
    #fdfaff;
  min-height:100vh;
}
.wrap{max-width:900px;margin:0 auto}
header{padding:44px 0 8px;text-align:center}
h1{margin:0;font-size:32px;letter-spacing:2px}
h1 span{background:linear-gradient(90deg,var(--pink-deep),var(--purple));
  -webkit-background-clip:text;background-clip:text;color:transparent}
.sub{margin-top:10px;color:var(--muted);font-size:13px;line-height:1.8}
.cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:12px;margin:26px 0}
.card{
  background:var(--card); border:1px solid var(--line); border-radius:18px;
  padding:16px 18px; box-shadow:0 6px 22px #b48ec91f; backdrop-filter:blur(6px);
}
.card .k{font-size:12px;color:var(--muted);letter-spacing:1px}
.card .v{font-size:26px;font-weight:700;margin-top:6px}
.card .v small{font-size:13px;font-weight:400;color:var(--muted);margin-left:2px}
.panel{background:var(--card);border:1px solid var(--line);border-radius:20px;padding:18px;
  box-shadow:0 6px 22px #b48ec91a;margin-bottom:18px;backdrop-filter:blur(6px)}
.panel h2{font-size:16px;margin:0 0 14px;display:flex;align-items:center;gap:8px}
.panel h2::before{content:"";width:8px;height:8px;border-radius:50%;
  background:linear-gradient(135deg,var(--pink),var(--purple))}
.bar-row{display:grid;grid-template-columns:26px 1fr auto;gap:10px;align-items:center;
  padding:6px 0;font-size:14px}
.bar-row .rank{color:var(--muted);font-size:12px;text-align:right}
.bar-row .nm{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.bar-name{display:flex;flex-direction:column;gap:4px;min-width:0}
.bar-track{height:8px;border-radius:6px;background:#f4eefb;overflow:hidden}
.bar-fill{height:100%;border-radius:6px;
  background:linear-gradient(90deg,var(--pink),var(--purple))}
.bar-row .hh{font-variant-numeric:tabular-nums;color:var(--pink-deep);font-weight:600;font-size:13px}
.tools{display:flex;gap:10px;flex-wrap:wrap;align-items:center;margin-bottom:14px}
input[type=search]{
  flex:1;min-width:180px;padding:10px 14px;border-radius:12px;border:1px solid var(--line);
  background:#fff;font-size:14px;font-family:inherit;color:var(--ink);outline:none;
}
input[type=search]:focus{border-color:var(--pink)}
.seg{display:flex;background:#f7f1fb;border-radius:12px;padding:3px}
.seg button{
  border:0;background:transparent;padding:7px 12px;border-radius:9px;cursor:pointer;
  font-size:13px;color:var(--muted);font-family:inherit
}
.seg button.on{background:#fff;color:var(--pink-deep);font-weight:600;box-shadow:0 2px 6px #b48ec926}
.tier{font-size:11px;padding:1px 7px;border-radius:999px;display:inline-block;
  background:#f6efff;color:#8b6fd0;white-space:nowrap}
.list{display:flex;flex-direction:column}
.item{display:grid;grid-template-columns:1fr auto;gap:6px 12px;padding:11px 2px;
  border-bottom:1px dashed var(--line);align-items:center}
.item:last-child{border-bottom:0}
.item .nm{font-size:14px;display:flex;align-items:center;gap:7px;min-width:0}
.item .nm b{font-weight:600;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
.item .meta{font-size:11.5px;color:var(--muted);margin-top:3px}
.item .hh{font-variant-numeric:tabular-nums;font-weight:700;color:var(--pink-deep);font-size:14px}
.empty{padding:20px;text-align:center;color:var(--muted);font-size:13px}
.ns-item{display:flex;justify-content:space-between;gap:10px;padding:7px 2px;font-size:13px;
  border-bottom:1px dashed var(--line)}
.ns-item:last-child{border-bottom:0}
.ns-item span:last-child{color:var(--muted);font-size:11.5px}
footer{text-align:center;color:var(--muted);font-size:12px;margin-top:24px;line-height:2}
@media (max-width:760px){
  h1{font-size:24px}
  .card .v{font-size:22px}
  header{padding-top:30px}
  .bar-row{grid-template-columns:20px 1fr auto;font-size:13px}
  .sub{font-size:12px;padding:0 6px}
  .tools{flex-direction:column;align-items:stretch;width:100%}
  .seg{width:100%;box-sizing:border-box}
  .seg button{flex:1 1 0;min-width:0;padding:7px 4px;font-size:12px}
  .item .nm{flex-wrap:wrap}
}
</style>
</head>
<body>
<div class="wrap">
  <header>
    <h1>我的<span>游戏统计</span></h1>
    <div class="sub">数据来自本机 Steam 记录 · 生成于 __DATE__</div>
  </header>

  <div class="cards" id="cards"></div>

  <div class="panel">
    <h2>玩得最久的 20 款</h2>
    <div id="top20"></div>
  </div>

  <div class="panel">
    <h2>全部记录 <span id="count" style="font-weight:400;color:var(--muted);font-size:12px"></span></h2>
    <div class="tools">
      <input type="search" id="q" placeholder="搜游戏名…">
      <div class="seg" id="sortseg">
        <button data-sort="time" class="on">按时长</button>
        <button data-sort="recent">按最近</button>
        <button data-sort="name">按名字</button>
      </div>
    </div>
    <div class="list" id="list"></div>
    <div class="empty" id="empty" style="display:none">没有匹配的游戏</div>
  </div>

  <div class="panel">
    <h2>不在 Steam 里的（桌面启动）</h2>
    <div id="nonsteam"></div>
  </div>

  <footer>
    时长由 Steam 本地记录统计，未安装的游戏也保留历史。<br>
    改完 Steam 库后重跑 <code>tools/steam_playtime.py</code> 再生成即可更新。
  </footer>
</div>

<script>
(function () {
const DATA = __DATA__;
const NONSTEAM = __NONSTEAM__;

const pad = n => String(n).padStart(2, "0");
const fmtDate = ts => {
  if (!ts) return "—";
  const d = new Date(ts * 1000);
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
};
const fmtH = h => h >= 100 ? Math.round(h).toLocaleString() : h.toFixed(1);

function tierOf(h) {
  if (h >= 500) return ["无底洞", "#ffe1ec", "#c9457a"];
  if (h >= 100) return ["常客", "#f3e6ff", "#7d5cc4"];
  if (h >= 20)  return ["玩过一阵", "#e6f0ff", "#4a72c9"];
  if (h >= 5)   return ["浅尝", "#e6faf1", "#2f9e77"];
  return ["尝个味", "#f2f2f4", "#7c7c8a"];
}

const totalMin = DATA.reduce((s, g) => s + g.minutes, 0);
const totalH = totalMin / 60;
const over10 = DATA.filter(g => g.hours >= 10).length;
const maxH = DATA.length ? DATA[0].hours : 0;

const summary = [
  ["游戏数量", DATA.length, "款"],
  ["总时长", Math.round(totalH).toLocaleString(), "小时"],
  ["折合", (totalH / 24).toFixed(0), "天"],
  ["10 小时以上的", over10, "款"],
];
document.getElementById("cards").innerHTML = summary.map(([k, v, u]) =>
  `<div class="card"><div class="k">${k}</div><div class="v">${v}<small>${u}</small></div></div>`
).join("");

const top = DATA.slice(0, 20);
document.getElementById("top20").innerHTML = top.map((g, i) => {
  const w = Math.max(2, (g.hours / maxH) * 100);
  return `<div class="bar-row">
    <div class="rank">${i + 1}</div>
    <div class="bar-name">
      <div class="nm" title="${g.name}">${g.name}</div>
      <div class="bar-track"><div class="bar-fill" style="width:${w}%"></div></div>
    </div>
    <div class="hh">${fmtH(g.hours)} h</div>
  </div>`;
}).join("");

let sortKey = "time";
let query = "";

function render() {
  let rows = DATA.slice();
  if (query) {
    const q = query.toLowerCase();
    rows = rows.filter(g => g.name.toLowerCase().includes(q));
  }
  if (sortKey === "time") rows.sort((a, b) => b.minutes - a.minutes);
  else if (sortKey === "recent") rows.sort((a, b) => (b.last || 0) - (a.last || 0));
  else rows.sort((a, b) => a.name.localeCompare(b.name, "zh"));

  document.getElementById("count").textContent =
    query ? `筛出 ${rows.length} / ${DATA.length} 款` : `共 ${DATA.length} 款`;
  document.getElementById("empty").style.display = rows.length ? "none" : "block";

  document.getElementById("list").innerHTML = rows.map(g => {
    const [tier, bg, fg] = tierOf(g.hours);
    return `<div class="item">
      <div>
        <div class="nm"><b title="${g.name}">${g.name}</b>
          <span class="tier" style="background:${bg};color:${fg}">${tier}</span></div>
        <div class="meta">最后游玩 ${fmtDate(g.last)} · appid ${g.appid}${g.installed ? "" : " · 已卸载"}</div>
      </div>
      <div class="hh">${fmtH(g.hours)} h</div>
    </div>`;
  }).join("");
}

document.getElementById("q").addEventListener("input", e => {
  query = e.target.value.trim();
  render();
});
document.getElementById("sortseg").addEventListener("click", e => {
  const b = e.target.closest("button");
  if (!b) return;
  sortKey = b.dataset.sort;
  [...document.querySelectorAll("#sortseg button")].forEach(x => x.classList.toggle("on", x === b));
  render();
});
render();

document.getElementById("nonsteam").innerHTML = NONSTEAM.length
  ? NONSTEAM.map(g => `<div class="ns-item"><span>${g.name}</span><span>${g.exe}</span></div>`).join("")
  : '<div class="empty">没有记录</div>';
})();
</script>
</body>
</html>
"""


def main():
    games = load_steam_games()
    nonsteam = load_non_steam()
    html = HTML.replace("__DATA__", json.dumps(games, ensure_ascii=False))
    html = html.replace("__NONSTEAM__", json.dumps(nonsteam, ensure_ascii=False))
    html = html.replace("__DATE__", time.strftime("%Y-%m-%d %H:%M"))
    with io.open(OUT, "w", encoding="utf-8") as f:
        f.write(html)
    total = sum(g["hours"] for g in games)
    print("游戏 %d 款 / %.0f 小时 / 非 Steam %d 个" % (len(games), total, len(nonsteam)))
    print("已生成: %s (%.0f KB)" % (OUT, os.path.getsize(OUT) / 1024))


if __name__ == "__main__":
    main()
