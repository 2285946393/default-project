"""给 quest-spire 的自研遗物画图标。

走本机 M365 网关的 gpt-image-2（AGENTS 里记的路线）。
成功率很低（实测约 1%），所以每个图最多重试 N 次，超了就跳过。
key 从 opencode 配置里读，绝不打印、不写进任何会被提交的文件。
"""
import base64
import json
import os
import re
import sys
import time
import urllib.request

CONFIG = r"C:\Users\有花无实\.config\opencode\opencode.jsonc"
OUT_DIR = r"C:\Users\有花无实\Documents\Default Project\assets\尖塔任务图标"
ENDPOINT = "http://127.0.0.1:4141/v1/images/generations"
MODEL = "gpt-image-2"
MAX_TRIES_PER_IMAGE = 28

STYLE = ("游戏道具图标，单个物体居中构图，深蓝灰色石质背景，方形，"
         "手绘厚涂质感，暖色高光，干净，无文字，无边框，无人物")

ITEMS = [
    ("quest_book", "一本厚厚的小书，摊开着，封面上有一个小小的尖塔图案，书页微微发光"),
    ("dead_branch", "一根扭曲的枯树枝，木质纹理清晰，枝头有几片新芽"),
    ("torii", "一座小小的日式朱红色鸟居，立在石台上，有一点青苔"),
    ("coffee_dripper", "一个陶瓷手冲咖啡滤杯，冒着热气，旁边有几颗咖啡豆"),
    ("toy_ornithopter", "一只发条玩具扑翼飞机，黄铜齿轮外露，翅膀是布做的"),
]


def read_key():
    raw = open(CONFIG, "r", encoding="utf-8").read()
    # 去掉 jsonc 注释后再解析
    raw = re.sub(r"^\s*//.*$", "", raw, flags=re.M)
    data = json.loads(raw)
    return data["m365"]["options"]["apiKey"]


def gen(key, prompt):
    body = json.dumps({
        "model": MODEL,
        "prompt": prompt,
        "size": "1024x1024",
        "n": 1,
    }).encode("utf-8")
    req = urllib.request.Request(
        ENDPOINT, data=body,
        headers={"Content-Type": "application/json", "Authorization": "Bearer " + key},
    )
    with urllib.request.urlopen(req, timeout=180) as resp:
        payload = json.loads(resp.read().decode("utf-8"))
    item = payload["data"][0]
    if item.get("b64_json"):
        return base64.b64decode(item["b64_json"])
    if item.get("url"):
        with urllib.request.urlopen(item["url"], timeout=120) as r2:
            return r2.read()
    raise RuntimeError("响应里既没有 b64_json 也没有 url")


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    key = read_key()
    print("key 读到了，长度 %d（不打印内容）" % len(key))
    total_ok = 0
    for name, desc in ITEMS:
        path = os.path.join(OUT_DIR, name + ".png")
        if os.path.exists(path) and os.path.getsize(path) > 10000:
            print("[%s] 已经有了，跳过" % name)
            total_ok += 1
            continue
        prompt = desc + "。" + STYLE
        for attempt in range(1, MAX_TRIES_PER_IMAGE + 1):
            try:
                t0 = time.time()
                data = gen(key, prompt)
                open(path, "wb").write(data)
                print("[%s] 成功！第 %d 次尝试，%.0f KB，耗时 %.1fs"
                      % (name, attempt, len(data) / 1024, time.time() - t0))
                total_ok += 1
                break
            except Exception as e:
                msg = str(e)[:120]
                if attempt % 5 == 0 or attempt == 1:
                    print("[%s] 第 %d 次失败: %s" % (name, attempt, msg))
                time.sleep(1.0)
        else:
            print("[%s] 重试 %d 次都没成，跳过" % (name, MAX_TRIES_PER_IMAGE))
    print("画完：%d / %d 张" % (total_ok, len(ITEMS)))


if __name__ == "__main__":
    main()
