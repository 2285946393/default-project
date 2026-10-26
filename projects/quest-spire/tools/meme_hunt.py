# 摸梗：拉 B站 视频信息 + 热评（评论区通常有人解释梗的含义）
# 用法： python meme_hunt.py BV1xxx BV1yyy ...
# 报告写到 %TEMP%\meme_hunt.txt（UTF-8）
import json, os, re, sys, time, urllib.parse, urllib.request

PROXY = "http://127.0.0.1:7892"
UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/131.0 Safari/537.36")
BVIDS = sys.argv[1:] or ["BV1NmvwzQEvr", "BV1Yhjt6CEjs", "BV14QuC6fEX6", "BV1hkYUz9ERr"]
WAIT = 6


def get_json(url, referer="https://www.bilibili.com/"):
    h = {"User-Agent": UA, "Accept-Language": "zh-CN,zh;q=0.9", "Referer": referer}
    last = None
    for i in range(4):
        try:
            op = urllib.request.build_opener(
                urllib.request.ProxyHandler({"http": PROXY, "https": PROXY}))
            op.addheaders = list(h.items())
            with op.open(url, timeout=45) as r:
                return json.loads(r.read().decode("utf-8", "replace"))
        except Exception as e:
            last = e
            time.sleep(WAIT * (i + 1))
    raise last


out = []

for bv in BVIDS:
    out.append("")
    out.append("=" * 72)
    try:
        v = get_json("https://api.bilibili.com/x/web-interface/view?bvid=%s" % bv)
        if v.get("code") != 0:
            out.append("%s -> code=%s %s" % (bv, v.get("code"), v.get("message")))
            time.sleep(WAIT)
            continue
        d = v["data"]
        out.append("【%s】%s" % (bv, d.get("title")))
        out.append("  UP=%s  播放=%s  时长=%ss  发布=%s" % (
            d.get("owner", {}).get("name"), d.get("stat", {}).get("view"),
            d.get("duration"), time.strftime("%Y-%m-%d", time.localtime(d.get("pubdate", 0)))))
        desc = re.sub(r"<[^>]+>", "", d.get("desc") or "")
        out.append("  简介: %s" % (desc.strip()[:500] if desc.strip() else "(空)"))
        tags = [t.get("tag_name") for t in (d.get("tags") or [])]
        if tags:
            out.append("  标签: %s" % " ".join(tags))
        aid = d.get("aid")
        time.sleep(WAIT)

        r = get_json("https://api.bilibili.com/x/v2/reply?type=1&ps=20&sort=2&oid=%s" % aid)
        reps = (r.get("data") or {}).get("replies") or []
        out.append("  —— 热评 %d 条 ——" % len(reps))
        for c in reps[:10]:
            msg = re.sub(r"<[^>]+>", "", (c.get("content") or {}).get("message", ""))
            out.append("    · [%s赞] %s" % (c.get("like"), msg.replace("\n", " ")[:220]))
        time.sleep(WAIT)
    except Exception as e:
        out.append("%s -> 失败 %s" % (bv, e))
        time.sleep(WAIT)

path = os.path.join(os.environ.get("TEMP", "."), "meme_hunt.txt")
open(path, "w", encoding="utf-8").write("\n".join(out))
print("report ->", path)
