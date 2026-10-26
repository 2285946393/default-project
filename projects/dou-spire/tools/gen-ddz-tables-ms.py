# -*- coding: utf-8 -*-
"""
斗地主桌图 —— 走朋友三千的魔搭 Image Studio（最高优先级，见 dsh-home/AGENTS.md 六点九）。

用法:
  python tools/gen-ddz-tables-ms.py            # 出全部 7 款
  python tools/gen-ddz-tables-ms.py 0          # 只出第 0 款（验证用）
  python tools/gen-ddz-tables-ms.py 0 3        # 出第 0~3 款

提示词从 assets/斗地主桌面-魔搭提示词.md 里抽（``` 代码块，按顺序对号）。
★ 不挂参考图 —— 上一批就是被德州桌参考图带歪成"德州换配色"的。
"""
import io, json, re, sys, time, urllib.request, pathlib, os

ROOT = pathlib.Path(__file__).resolve().parent.parent
MD = ROOT / "assets/斗地主桌面-魔搭提示词.md"
OUT = pathlib.Path(r"D:/work/AI产出/斗地主桌")
TOKEN_FILE = pathlib.Path(r"D:/software/dsh-home/.modelscope-token")

BASE = "https://studio-lf3000y-image-studio.api-inference.modelscope.net"
SIZE = "1536x864"           # 16:9，锁死
MODEL = "qwen-image-2.1"

# 文件名（顺序必须和 md 里的代码块一一对应）
NAMES = [
    "table_ddz_green",      # 1 经典绿绒
    "table_ddz_brass",      # 2 暗黑黄铜 · 酒红皮革
    "table_ddz_obsidian",   # 3 黑曜石 · 熔金纹
    "table_ddz_frost",      # 4 冰蓝水晶 · 月白
    "table_ddz_anime",      # 5 二次元明亮
    "table_ddz_lacquer",    # 6 东方漆器 · 朱红
    "table_ddz_steam",      # 7 蒸汽朋克 · 黄铜齿轮
]


def token() -> str:
    return TOKEN_FILE.read_text(encoding="utf-8").strip()


def blocks():
    s = io.open(MD, encoding="utf-8").read()
    out = [b.strip() for b in re.findall(r"```\n(.*?)```", s, re.S)]
    # 只留"桌图"那种长块（正文里的调用示例很短）
    return [b for b in out if len(b) > 1500]


def gen(prompt: str, dest: pathlib.Path) -> bool:
    body = json.dumps({"model": MODEL, "prompt": prompt,
                       "n": 1, "size": SIZE}).encode()
    req = urllib.request.Request(
        BASE + "/v1/images/generations", data=body, method="POST",
        headers={"Authorization": "Bearer " + token(),
                 "Content-Type": "application/json"})
    t0 = time.time()
    with urllib.request.urlopen(req, timeout=900) as r:
        resp = json.load(r)
    d0 = resp["data"][0]
    meta = d0.get("meta", {})
    url = d0["url"]
    with urllib.request.urlopen(url, timeout=180) as r2:
        data = r2.read()
    dest.write_bytes(data)
    print(f"    ✅ {dest.name}  {len(data)//1024} KB  "
          f"{meta.get('width')}x{meta.get('height')}  seed={meta.get('seed')}  "
          f"用时 {time.time()-t0:.0f}s", flush=True)
    return True


def main():
    bs = blocks()
    print(f"抽出 {len(bs)} 段提示词（文件：{MD.name}）")
    if len(bs) != len(NAMES):
        print(f"⚠️ 块数 {len(bs)} != 文件名数 {len(NAMES)}，只处理前 {min(len(bs), len(NAMES))} 个")

    lo = int(sys.argv[1]) if len(sys.argv) > 1 else 0
    hi = int(sys.argv[2]) + 1 if len(sys.argv) > 2 else len(NAMES)

    OUT.mkdir(parents=True, exist_ok=True)
    for i in range(lo, min(hi, len(bs), len(NAMES))):
        prompt, name = bs[i], NAMES[i]
        dest = OUT / f"{name}.png"
        if dest.exists() and dest.stat().st_size > 10_000:
            print(f"[{i}] {name}.png 已存在，跳过（要重出先删）")
            continue
        print(f"[{i}] 生成 {name} …（提示词 {len(prompt)} 字符）", flush=True)
        try:
            gen(prompt, dest)
        except Exception as e:
            print(f"[{i}] ❌ {name}: {e}", flush=True)
    print("完毕 →", OUT)


if __name__ == "__main__":
    main()
