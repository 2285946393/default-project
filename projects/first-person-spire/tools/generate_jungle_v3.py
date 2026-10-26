# -*- coding: utf-8 -*-
"""丛林材质 v3：用原版场景图当「风格参考」出图。

前两轮失败原因（记录，别重犯）：
  v2 第一版：绿得对，但糊（边缘量 5.33 / 原版 0.52）、闷（明暗标准差 4.7 / 原版 13.1）。
  强行提对比那版：绿太鲜太亮、藤蔓竖列重复；地面变成一个个绿圆球。
  → 结论：纯文字调参调不出原版手绘感。

本轮做法：把原版解包场景图作为参考喂给模型，让它照着原版的色彩和笔触画材质。
用 multipart 的 images/edits 口（实测这个口吃 multipart，字段名 image）。
⚠️ 走 edits 时输出尺寸跟随参考图比例，所以生成后要裁成正方形再存。
"""
import base64
import json
import os
import subprocess
import sys
import time

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
BASE = "https://studio-lf3000y-image-studio.api-inference.modelscope.net"
TOKEN_FILE = r"D:/software/dsh-home/.modelscope-token"
OUTDIR = os.path.join(ROOT, "assets", "杀戮尖塔测试", "房间纹理", "v2")
LIB = os.path.join(ROOT, "projects", "sts2-asset-library", "images", "rooms", "rest_sites")
PROMPTS = os.path.join(OUTDIR, "prompts-v3.json")


def token():
    with open(TOKEN_FILE, "r", encoding="utf-8") as f:
        return f.read().strip()


def main():
    with open(PROMPTS, "r", encoding="utf-8") as f:
        prompts = json.load(f)
    only = sys.argv[1:] or [k for k in prompts if not k.startswith("_")]

    for name in only:
        spec = prompts[name]
        prompt, ref = spec["prompt"], spec["ref"]
        ref_path = os.path.join(LIB, ref)
        if not os.path.exists(ref_path):
            print(f"[跳过] 参考图不存在：{ref}")
            continue
        dest = os.path.join(OUTDIR, f"{name}-v3.png")
        if os.path.exists(dest):
            print(f"[已有] {name}-v3.png")
            continue
        print(f"[生成] {name}  (参考 {ref}) ...", flush=True)
        t0 = time.time()
        cmd = [
            "curl", "-s", "-m", "300", "-X", "POST", BASE + "/v1/images/edits",
            "-H", "Authorization: Bearer " + token(),
            "-F", "model=qwen-image-2.1",
            "-F", "prompt=" + prompt,
            "-F", "image=@" + ref_path,
        ]
        out = subprocess.run(cmd, capture_output=True)
        try:
            data = json.loads(out.stdout.decode("utf-8"))
        except Exception:
            print(f"  返回无法解析：{out.stdout[:200]}"); continue
        if not data.get("data"):
            print(f"  失败：{str(data)[:200]}"); continue
        url = data["data"][0]["url"]
        raw = os.path.join(OUTDIR, f"{name}-raw.png")
        subprocess.run(["curl", "-s", "-L", "-o", raw, url], check=True)

        # edits 输出跟随参考图比例，裁成正方形（居中裁）
        subprocess.run([sys.executable, "-c", f"""
from PIL import Image
im = Image.open(r'{raw}').convert('RGB')
w, h = im.size
s = min(w, h)
im.crop(((w-s)//2, (h-s)//2, (w-s)//2+s, (h-s)//2+s)).resize((1024,1024)).save(r'{dest}')
"""], check=True)
        os.remove(raw)
        print(f"  OK {os.path.getsize(dest)/1024/1024:.2f} MB  {time.time()-t0:.0f}s\n")
    print("完成。")


if __name__ == "__main__":
    main()
