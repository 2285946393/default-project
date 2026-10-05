# -*- coding: utf-8 -*-
"""
多路免费额度调度器 —— 给 agent 批量干活用（想梗、抄长文、分类、写正则…）。

为什么有这个东西：单靠一个免费站会被限速（gcmod 是「1 分钟最多 5 次」），
把手上几家串起来按顺序试，一家不通/限流就自动换下一家，吞吐就上来了。

用法：
    python tools/ask.py "你的问题"
    python tools/ask.py --prompt-file 提示词.txt          # 长提示词走文件
    python tools/ask.py --only gcmod "只走这家"
    python tools/ask.py --list                            # 看配置了哪几家
    python tools/ask.py --test                            # 每家真发一条「1+1=?」看谁活着

密钥从 `D:\\software\\dsh-home\\.credentials.yaml` 的 refs 里读，不写死在这里。

⚠️ 两个坑（2026-10-04 记）：
   1. **梯子会拦国内站**。gcmod / fiime / AMD 都是国内的，走代理反而连不上 →
      这几家**一律直连**；unsee 是国外的，直连不通时再走代理。
   2. unsee 那个 key 主人标注过「禁止蒸馏」，**别拿它大批量跑数据**，这里把它排在最后。
"""
import argparse
import io
import json
import os
import re
import sys
import time
import urllib.request

CRED = r"D:\software\dsh-home\.credentials.yaml"
PROXY = "http://127.0.0.1:7892"

# 顺序 = 尝试顺序。via="direct" 直连；via="proxy" 走代理；via="both" 先直连再代理
PROVIDERS = [
    {"name": "gcmod", "url": "https://zc.gcmod.cn/v1/chat/completions",
     "key_ref": "GCMOD_API_KEY", "model": "deepseek-v4.1-flash", "via": "direct",
     "note": "公益站，最好用；限速 1 分钟 5 次"},
    {"name": "fiime", "url": "https://opc.fiime.cn/api/model-service/v1/chat/completions",
     "key_ref": "FIIME_API_KEY", "model": "deepseek-v4-flash-0731", "via": "direct",
     "note": "公益站，高峰会排队（等十几秒重发）"},
    {"name": "amd", "url": "https://developer.amd.com.cn/radeon/api/v1/chat/completions",
     "key_ref": "AMD_API_KEY", "model": "Qwen3.8-Flash-Next", "via": "direct",
     "note": "AMD 开发者站"},
    {"name": "xtvacnl", "url": "http://ai.xtvacnl.cn/v1/chat/completions",
     "key_ref": "XTVACNL_API_KEY", "model": "deepseek-v4-flash", "via": "direct",
     "note": "2026-10-05 实测直连可用；还有 glm-5.3-flash / space-bunny"},
    {"name": "deepseek", "url": "https://api.deepseek.com/v1/chat/completions",
     "key_ref": "DEEPSEEK_API_KEY", "model": "deepseek-chat", "via": "both",
     "note": "官方 key（要花钱，放后面）"},
    {"name": "unsee", "url": "https://sub.unsee.you/v1/chat/completions",
     "key_ref": "UNSEE_LUNA_API_KEY", "model": "gpt-5.6-luna", "via": "both",
     "note": "禁止大批量跑（key 主人标注）"},
]


def load_keys():
    """从 DSH 的 credentials.yaml 里读 refs（简单的 key: value 扫描，不依赖 yaml 库）。"""
    keys = {}
    if not os.path.exists(CRED):
        return keys
    in_refs = False
    for line in io.open(CRED, encoding="utf-8"):
        if re.match(r"^refs:", line):
            in_refs = True
            continue
        if in_refs:
            if line.strip() and not line.startswith(" "):
                break
            m = re.match(r"\s+([A-Z0-9_]+):\s*(\S+)", line)
            if m:
                keys[m.group(1)] = m.group(2)
    return keys


def call(p, key, prompt, timeout=150):
    body = json.dumps({"model": p["model"], "messages": [{"role": "user", "content": prompt}],
                       "stream": False}).encode("utf-8")
    handlers = []
    if p["via"] in ("direct", "both"):
        handlers.append(urllib.request.ProxyHandler({}))
    if p["via"] in ("proxy", "both"):
        handlers.append(urllib.request.ProxyHandler({"http": PROXY, "https": PROXY}))
    last = None
    for h in handlers:
        req = urllib.request.Request(p["url"], data=body, method="POST")
        req.add_header("Content-Type", "application/json")
        req.add_header("Authorization", "Bearer " + key)
        try:
            op = urllib.request.build_opener(h)
            op.addheaders = [("User-Agent", "ask.py/1.0")]
            with op.open(req, timeout=timeout) as r:
                d = json.loads(r.read().decode("utf-8", "replace"))
            txt = (d.get("choices") or [{}])[0].get("message", {}).get("content", "")
            if txt and txt.strip():
                return txt
            last = "返回空内容"
        except Exception as e:
            last = str(e)[:160]
    raise RuntimeError(last or "失败")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("prompt", nargs="*")
    ap.add_argument("--prompt-file")
    ap.add_argument("--only")
    ap.add_argument("--list", action="store_true")
    ap.add_argument("--test", action="store_true")
    a = ap.parse_args()

    if a.list:
        keys = load_keys()
        for p in PROVIDERS:
            print(f'  {p["name"]:<9} {p["model"]:<22} {"有 key" if keys.get(p["key_ref"]) else "缺 key":<7} {p["note"]}')
        return

    if a.test:
        prompt = "1+1 等于几？只回答数字。"
    else:
        prompt = (io.open(a.prompt_file, encoding="utf-8").read() if a.prompt_file else " ".join(a.prompt))
    if not prompt.strip():
        sys.exit("没给问题（用参数或 --prompt-file）")

    keys = load_keys()
    todo = [p for p in PROVIDERS if (not a.only or p["name"] == a.only)]
    errs = []
    for p in todo:
        key = keys.get(p["key_ref"])
        if not key:
            errs.append(f'{p["name"]}: 没 key')
            continue
        t0 = time.time()
        try:
            out = call(p, key, prompt)
            print(f'通过 {p["name"]}（{p["model"]}，{time.time()-t0:.1f}s）\n', file=sys.stderr)
            print(out)
            return
        except Exception as e:
            errs.append(f'{p["name"]}: {e}')
            print(f'  x {p["name"]} 不通：{e}', file=sys.stderr)
    sys.exit("全部失败：\n  " + "\n  ".join(errs))


if __name__ == "__main__":
    main()
