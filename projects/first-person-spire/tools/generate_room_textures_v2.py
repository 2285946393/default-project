# -*- coding: utf-8 -*-
"""第一人称 mod 房间材质生成 v2。

用户反馈 v1 不行：丛林画成了米黄石板地，跟原版不匹配。
v2 基准来自本体解包图 images/rooms/rest_sites/*/*_rest_site_*.png。

先探代理、再用魔搭创空间 lf3000y/image-studio 出图。
"""
import base64
import json
import os
import sys
import time
import urllib.request
import urllib.error

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
BASE = "https://studio-lf3000y-image-studio.api-inference.modelscope.net"
TOKEN_FILE = r"D:/software/dsh-home/.modelscope-token"
PROMPTS = os.path.join(ROOT, "assets", "杀戮尖塔测试", "房间纹理", "v2", "prompts-v2.json")
OUTDIR = os.path.join(ROOT, "assets", "杀戮尖塔测试", "房间纹理", "v2")
LIB = os.path.join(ROOT, "projects", "sts2-asset-library", "images", "rooms", "rest_sites")

# ⚠️ 实测：走 edits 传参考图时输出尺寸**跟着参考图比例走**（768x360 进 → 1024x480 出），
# 材质必须是正方形四方连续，所以**一律走纯文生图**（/v1/images/generations，size 1024x1024）。
# 原版配色已经量化写进 prompts-v2.json 的 RGB 里，不靠参考图带风格。
REFS = {}


def token():
    with open(TOKEN_FILE, "r", encoding="utf-8") as f:
        return f.read().strip()


def load_prompts():
    with open(PROMPTS, "r", encoding="utf-8") as f:
        raw = json.load(f)
    return {k: v for k, v in raw.items() if not k.startswith("_")}


def ref_b64(spec):
    """把参考图缩到 768 宽再编码，省流量。"""
    path = os.path.join(LIB, spec)
    if not os.path.exists(path):
        return None
    try:
        from PIL import Image
        import io
        im = Image.open(path).convert("RGB")
        im.thumbnail((768, 768))
        buf = io.BytesIO()
        im.save(buf, "JPEG", quality=82)
        return base64.b64encode(buf.getvalue()).decode()
    except Exception as exc:  # 没装 PIL 就原图直传
        print(f"  参考图缩放出错({exc})，改用原图", flush=True)
        with open(path, "rb") as f:
            return base64.b64encode(f.read()).decode()


def generate(name, prompt, refs):
    """纯文生图，锁定 1024x1024 正方形，保证材质四方连续。
    参考图走 edits 时输出尺寸会跟随参考图比例（实测 768x360 → 1024x480），因此不用参考图。"""
    payload = {
        "model": "qwen-image-2.1",
        "prompt": prompt,
        "size": "1024x1024",
        "n": 1,
    }
    url = BASE + "/v1/images/generations"

    body = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(url, data=body, method="POST", headers={
        "Authorization": "Bearer " + token(),
        "Content-Type": "application/json",
    })
    try:
        with urllib.request.urlopen(req, timeout=300) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        detail = exc.read().decode("utf-8", "replace")[:300]
        print(f"  HTTP {exc.code}: {detail}", flush=True)
        return None


def download(url, dest):
    req = urllib.request.Request(url, headers={"User-Agent": "curl/8"})
    with urllib.request.urlopen(req, timeout=180) as resp, open(dest, "wb") as out:
        out.write(resp.read())
    return os.path.getsize(dest)


def main():
    os.makedirs(OUTDIR, exist_ok=True)
    prompts = load_prompts()
    only = sys.argv[1:] or list(prompts.keys())
    print(f"输出目录：{OUTDIR}")
    print(f"待生成 {len(only)} 张：{', '.join(only)}\n")

    for name in only:
        if name not in prompts:
            print(f"[跳过] 提示词里没有 {name}")
            continue
        dest = os.path.join(OUTDIR, f"{name}-v2.png")
        if os.path.exists(dest) and "--force" not in sys.argv:
            print(f"[已有] {name}-v2.png，跳过（加 --force 重画）")
            continue
        print(f"[生成] {name} ...", flush=True)
        t0 = time.time()
        result = generate(name, prompts[name], REFS.get(name, []))
        if not result or not result.get("data"):
            print(f"  失败：{result}\n")
            continue
        item = result["data"][0]
        n = download(item["url"], dest)
        print(f"  OK {n/1024/1024:.2f} MB  {time.time()-t0:.0f}s  cache_hit={result.get('cache_hit')}\n")
    print("全部完成。")


if __name__ == "__main__":
    main()
