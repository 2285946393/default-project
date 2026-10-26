# -*- coding: utf-8 -*-
"""丛林材质 v4：干净平涂 + 数值调色。

## 前几轮为什么失败（别再重犯）

| 版本 | 结果 | 量化证据 |
|---|---|---|
| v2 第一版 | 绿对但糊、闷 | 边缘量 5.33（原版 0.40，多 13 倍）、明暗标准差 4.7（原版 12.2） |
| 强行提对比 | 绿太鲜、藤蔓竖列、地面变绿球 | — |
| 强行加石头 | 变成石砌遗迹（用户最早否掉的方向） | — |
| 原版图当参考 | 模型原样还图，没画材质 | — |

## 关键发现

把本体解包图 `overgrowth_rest_site_bg.png` 的中央战斗区**放大 3 倍**后看清：
**原版草地极其简单** —— 几乎均匀的暗绿平涂，细节只集中在边缘（几簇草、几块小石头），
中间刻意留白好让怪物看得清。**原版不要"细节和质感"，妈妈之前一直在拼命加，方向反了。**

并且原版的绿是「脏绿」：饱和度 68%，掺了蓝和灰；模型默认画出来的绿饱和度 94%，太干净太艳。

## 做法（两步）

1. **生成**：提示词强调 calm / clean / mostly flat colour / NO speckle / NO brush strokes /
   中心留空 / 细节只在边缘。
2. **调色**：按目标均值逐通道平移，再把饱和度往灰度拉 18%。
   目标取值来自本体解包图实测：地面 RGB(32,42,13)、墙 RGB(42,54,31)、顶 RGB(46,58,38)。

调色这一步比继续改提示词可靠得多 —— 色彩是数值问题，算就行，不用猜。
"""
import json
import os
import subprocess
import sys

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
BASE = "https://studio-lf3000y-image-studio.api-inference.modelscope.net"
TOKEN_FILE = r"D:/software/dsh-home/.modelscope-token"
OUTDIR = os.path.join(ROOT, "assets", "杀戮尖塔测试", "房间纹理", "v2", "v4")

# 目标平均色取自本体解包图实测（projects/sts2-asset-library/images/rooms/rest_sites/overgrowth/）
TARGETS = {
    "floor": (32, 42, 13),
    "wall": (42, 54, 31),
    "ceiling": (46, 58, 38),
}
# 饱和度压缩系数：模型默认太艳，往灰度拉。1.0 = 不动。
SATURATION = 0.82


def token():
    with open(TOKEN_FILE, "r", encoding="utf-8") as f:
        return f.read().strip()


def generate(name):
    with open(os.path.join(OUTDIR, "prompts-v4.json"), "r", encoding="utf-8") as f:
        prompt = json.load(f)[name]
    body = json.dumps({"model": "qwen-image-2.1", "prompt": prompt,
                       "size": "1024x1024", "n": 1}).encode("utf-8")
    result = subprocess.run(
        ["curl", "-s", "-m", "300", "-X", "POST", BASE + "/v1/images/generations",
         "-H", "Authorization: Bearer " + token(),
         "-H", "Content-Type: application/json", "--data-binary", "@-"],
        input=body, capture_output=True)
    data = json.loads(result.stdout.decode("utf-8"))
    if not data.get("data"):
        raise RuntimeError(f"生成失败：{str(data)[:200]}")
    raw = os.path.join(OUTDIR, f"{name}-raw.png")
    subprocess.run(["curl", "-s", "-L", "-o", raw, data["data"][0]["url"]], check=True)
    return raw


def tone(raw, dest, target):
    """把平均色拉到目标值，并压低饱和度。"""
    image = np.asarray(Image.open(raw).convert("RGB"), dtype=float)
    mean = image.reshape(-1, 3).mean(0)
    out = image.copy()
    for channel in range(3):
        out[:, :, channel] = np.clip(image[:, :, channel] - mean[channel] + target[channel], 0, 255)
    gray = out.mean(2, keepdims=True)
    out = np.clip(gray + (out - gray) * SATURATION, 0, 255)
    Image.fromarray(out.astype("uint8")).save(dest)
    final = out.reshape(-1, 3).mean(0)
    return final


def main():
    os.makedirs(OUTDIR, exist_ok=True)
    targets = sys.argv[1:] or list(TARGETS)
    for name in targets:
        dest = os.path.join(OUTDIR, f"{name}.png")
        try:
            raw = generate(name)
        except Exception as exc:
            print(f"[失败] {name}: {exc}")
            continue
        final = tone(raw, dest, TARGETS[name])
        os.remove(raw)
        print(f"[完成] {name}.png  平均色 {[round(v) for v in final]}  目标 {TARGETS[name]}")


if __name__ == "__main__":
    main()
