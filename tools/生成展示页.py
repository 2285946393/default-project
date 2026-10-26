#!/usr/bin/env python3
"""出图展示页生成器。

把一整个目录的 NovelAI 出图，自动生成一个「第N轮」风格的展示页：
- 从 PNG 里读参数（prompt / seed / 画师 / 尺寸）
- 按文件名前缀分组
- 输出和你现有页面同款样式的单文件 HTML

用法：
    python 生成展示页.py <图片目录> [--title "第40轮 · xxx"] [--out xxx.html]

不传 --title 时用目录名当标题。
"""

from __future__ import annotations

import argparse
import html
import json
import os
import re
import sys
from pathlib import Path

from PIL import Image

IMAGE_EXTS = {".png", ".jpg", ".jpeg", ".webp"}

# 和现有页面一致的配色
STYLE = """
  body{margin:0;padding:28px;background:#141216;color:#f3e9ef;
       font-family:"Microsoft YaHei",system-ui,sans-serif}
  h1{font-size:21px;font-weight:600;margin:0 0 6px}
  p.sub{color:#a9949f;margin:0 0 18px;font-size:13px;line-height:1.75}
  h2{font-size:15px;color:#f0b8cc;margin:32px 0 12px;font-weight:600;
     padding-bottom:6px;border-bottom:1px solid #3a2f36}
  .box{background:#1d1a20;border-left:3px solid #c78;border-radius:8px;
       padding:13px 17px;margin:0 0 22px;font-size:12.5px;line-height:1.95;color:#cbb8c2}
  .box b{color:#f0b8cc}
  code{background:#2a2429;padding:1px 5px;border-radius:4px;
       font-size:11.5px;color:#e8b8cc}
  .grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(300px,1fr));gap:16px}
  figure{margin:0;background:#1d1a20;border-radius:13px;overflow:hidden;
         box-shadow:0 8px 26px #0007}
  img{display:block;width:100%;height:auto}
  figcaption{padding:10px 13px;font-size:12.5px;line-height:1.7}
  figcaption b{display:block;font-size:14px;margin-bottom:4px;color:#f5e3ea}
  figcaption span{color:#9c8f96}
  .meta{font-size:11px;color:#7a6c74;padding:0 13px 10px;line-height:1.6}
  .meta code{font-size:10.5px}
  .seed{color:#8fb8a0}
"""


def strip_weights(tag: str) -> str:
    t = tag.strip()
    m = re.match(r"^-?\d+(?:\.\d+)?::(.*?)::$", t)
    if m:
        t = m.group(1)
    prev = None
    while prev != t:
        prev = t
        t = t.strip("{}[]").strip()
    return t.strip()


def read_meta(path: Path) -> dict:
    """读 NovelAI 参数，读不到就返回最小信息。"""
    out = {"prompt": "", "seed": "", "model": "", "w": 0, "h": 0, "artists": []}
    try:
        with Image.open(path) as im:
            out["w"], out["h"] = im.size
            raw = im.info.get("Comment")
            if raw:
                d = json.loads(raw)
                out["prompt"] = d.get("prompt") or ""
                out["seed"] = str(d.get("seed") or "")
                out["model"] = d.get("model_name") or ""
    except Exception:
        pass
    if out["prompt"]:
        tags = [strip_weights(t) for t in out["prompt"].split(",")]
        out["artists"] = [
            m.group(1).strip()
            for t in tags
            if (m := re.match(r"^artist:\s*(.+)$", t, re.I))
        ]
    return out


def group_key(path: Path) -> str:
    """按文件名第一个下划线前的部分分组，比如 B1_xxx → B1。"""
    stem = path.stem
    m = re.match(r"^([A-Za-z]+\d+)_", stem)
    if m:
        return m.group(1)
    m = re.match(r"^([A-Za-z]+)_", stem)
    if m:
        return m.group(1)
    return "其他"


# 这些是处理过程中的衍生物，不是成品，展示页里不该出现
_SKIP_SUFFIXES = ("_clean", "_masked", "_打码")


def is_derivative(path: Path) -> bool:
    stem = path.stem.lower()
    return any(stem.endswith(s) for s in _SKIP_SUFFIXES)


def build(folder: Path, title: str, out_path: Path) -> int:
    imgs = sorted(
        p for p in folder.iterdir()
        if p.is_file() and p.suffix.lower() in IMAGE_EXTS
        and not is_derivative(p)
    )
    if not imgs:
        print(f"目录里没有可用图片（已排除 _clean/_masked 衍生物）：{folder}", file=sys.stderr)
        return 1

    groups: dict[str, list[Path]] = {}
    for p in imgs:
        groups.setdefault(group_key(p), []).append(p)

    # 图片相对路径：以输出 HTML 所在目录为基准算
    try:
        rel_base = Path(os.path.relpath(folder, out_path.parent)).as_posix()
    except Exception:
        rel_base = folder.name

    parts = [
        "<!doctype html>",
        '<html lang="zh">',
        '<meta charset="utf-8">',
        f"<title>{html.escape(title)}</title>",
        f"<style>{STYLE}</style>",
        f"<h1>{html.escape(title)}</h1>",
    ]

    # 副标题：总张数 + 尺寸
    sizes = set()
    for p in imgs:
        m = read_meta(p)
        if m["w"] and m["h"]:
            sizes.add(f'{m["w"]}×{m["h"]}')
    sub = f"共 {len(imgs)} 张"
    if sizes:
        sub += " · " + " / ".join(sorted(sizes))
    parts.append(f'<p class="sub">{html.escape(sub)}</p>')

    for gname, items in groups.items():
        if len(groups) > 1:
            parts.append(f"<h2>{html.escape(gname)} ×{len(items)}</h2>")
        parts.append('<div class="grid">')

        for idx, p in enumerate(items, 1):
            meta = read_meta(p)
            src = f"{rel_base}/{p.name}"
            cap_title = html.escape(p.stem)
            parts.append("<figure>")
            parts.append(f'<img src="{html.escape(src)}" alt="{cap_title}" loading="lazy">')
            parts.append("<figcaption>")
            parts.append(f"<b>{idx}. {cap_title}</b>")
            if meta["artists"]:
                artists = " / ".join(html.escape(a) for a in meta["artists"][:4])
                parts.append(f"<span>画师：{artists}</span>")
            parts.append("</figcaption>")

            # 参数行
            bits = []
            if meta["w"]:
                bits.append(f'{meta["w"]}×{meta["h"]}')
            if meta["seed"]:
                bits.append(f'<span class="seed">seed {meta["seed"]}</span>')
            if meta["model"]:
                bits.append(html.escape(meta["model"].replace("NovelAI Diffusion ", "NAI ")))
            if bits:
                parts.append(f'<div class="meta">{" · ".join(bits)}</div>')

            parts.append("</figure>")

        parts.append("</div>")

    parts.append("</html>")

    out_path.write_text("\n".join(parts), encoding="utf-8")
    print(f"已生成：{out_path}")
    print(f"  {len(imgs)} 张，{len(groups)} 组")
    return 0


def main():
    ap = argparse.ArgumentParser(description="从图片目录生成展示页")
    ap.add_argument("folder", help="图片所在目录")
    ap.add_argument("--title", default="", help="页面标题，默认用目录名")
    ap.add_argument("--out", default="", help="输出 HTML 路径")
    args = ap.parse_args()

    folder = Path(args.folder).resolve()
    if not folder.is_dir():
        print(f"不是目录：{folder}", file=sys.stderr)
        return 1

    title = args.title or folder.name
    out_path = Path(args.out) if args.out else folder.parent.parent / "pages" / f"{folder.name}.html"
    out_path.parent.mkdir(parents=True, exist_ok=True)

    return build(folder, title, out_path)


if __name__ == "__main__":
    sys.exit(main())
