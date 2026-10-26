# -*- coding: utf-8 -*-
# 从 提示词 md 里抽出 4 段提示词，调本地 M365 Gateway 出图。
# 用法: D:/software/Python/python.exe tools/gen-ddz-tables.py
import io, json, re, sys, time, urllib.request, pathlib

MD = pathlib.Path(__file__).resolve().parent.parent / "assets/斗地主桌面-二次元提示词.md"
OUT = pathlib.Path(r"C:/Users/有花无实/Documents/Default Project/assets/斗地主桌")
API = "http://127.0.0.1:4141/v1/images/generations"
KEY = "m365_c0cba696eb9203f68387e719925041a7103e401eecc5084f5e39ff74062afdc0"

s = io.open(MD, encoding="utf-8").read()
blocks = re.findall(r"```\n(.*?)```", s, re.S)
blocks = [b.strip() for b in blocks if "poker-table" in b or "card table" in b]
print(f"抽出 {len(blocks)} 段提示词")

names = ["table_ddz_a_luminous", "table_ddz_b_flatposter",
         "table_ddz_c_airylight", "table_ddz_d_galgame"]

for i, prompt in enumerate(blocks):
    name = names[i] if i < len(names) else f"table_ddz_extra_{i}"
    # 没有参考图可挂，把"对照参考图"那句改掉
    prompt = prompt.replace(
        "SIZE IS RIGID -- match the attached reference table's silhouette exactly: the oval's",
        "SIZE IS RIGID: the oval's")
    dest = OUT / f"{name}.png"
    if dest.exists():
        print(f"[{i}] {name}.png 已存在，跳过")
        continue
    body = json.dumps({"model": "gpt-image-2", "prompt": prompt,
                       "n": 1, "size": "1536x1024"}).encode()
    req = urllib.request.Request(API, data=body, method="POST",
        headers={"Authorization": f"Bearer {KEY}", "Content-Type": "application/json"})
    print(f"[{i}] 生成 {name} …（提示词 {len(prompt)} 字符）", flush=True)
    t0 = time.time()
    try:
        with urllib.request.urlopen(req, timeout=600) as r:
            resp = json.load(r)
        url = resp["data"][0]["url"]
        if url.startswith("http"):
            src = url
        else:
            src = url
        # 本地地址直接下载
        if src.startswith("file://"):
            data = pathlib.Path(src[7:]).read_bytes()
        else:
            with urllib.request.urlopen(src, timeout=120) as r2:
                data = r2.read()
        dest.write_bytes(data)
        print(f"[{i}] ✅ {dest}  {len(data)//1024} KB  用时 {time.time()-t0:.0f}s")
    except Exception as e:
        print(f"[{i}] ❌ {e}", flush=True)
print("完毕")
