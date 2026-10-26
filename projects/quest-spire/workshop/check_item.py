# 用 Steam 官方接口读创意工坊条目的标题 / 标签 / 依赖 / 说明文
# 用法： python check_item.py 3806270140 3747602295
# 结果写到 %TEMP%\steam_items.txt（UTF-8），控制台只打英文摘要避免乱码
import json, os, sys, urllib.parse, urllib.request

API = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/"


def query(ids):
    data = {"itemcount": len(ids)}
    for i, x in enumerate(ids):
        data["publishedfileids[%d]" % i] = str(x)
    body = urllib.parse.urlencode(data).encode()
    req = urllib.request.Request(
        API, data=body,
        headers={"Content-Type": "application/x-www-form-urlencoded"})
    with urllib.request.urlopen(req, timeout=40) as r:
        return json.load(r)


def tags_of(appid="2868840"):
    """抓创意工坊的可用标签列表（上传时乱填的标签会被 Steam 静默丢掉）"""
    import re
    url = "https://steamcommunity.com/workshop/browse/?appid=%s&browsesort=trend" % appid
    for proxy in (None, "http://127.0.0.1:7892"):
        try:
            op = urllib.request.build_opener(urllib.request.ProxyHandler(
                {"http": proxy, "https": proxy} if proxy else {}))
            op.addheaders = [("User-Agent", "Mozilla/5.0"), ("Accept-Language", "en-US,en;q=0.9")]
            with op.open(url, timeout=45) as r:
                html = r.read().decode("utf-8", "replace")
            break
        except Exception as e:
            print("tag fetch fail proxy=%s: %s" % (proxy, e))
    else:
        return []
    found = re.findall(r"requiredtags\[\]=([^&\"']+)", html)
    found += re.findall(r"tag_(\w+)\"", html)
    seen = []
    for t in found:
        t = urllib.parse.unquote(t)
        if t not in seen:
            seen.append(t)
    return seen


if len(sys.argv) > 1 and sys.argv[1] == "--tags":
    ts = tags_of()
    print("可用标签 %d 个:" % len(ts))
    print(", ".join(ts))
    sys.exit(0)

ids = [int(x) for x in sys.argv[1:]] or [3806270140]
resp = query(ids)["response"]
out = []

for f in resp.get("publishedfiledetails", []):
    out.append("=" * 70)
    out.append("id           : %s" % f.get("publishedfileid"))
    out.append("result       : %s   (1=正常, 9=不存在或不可见)" % f.get("result"))
    out.append("title        : %s" % f.get("title"))
    out.append("visibility   : %s" % f.get("visibility"))
    out.append("tags         : %s" % ", ".join(t.get("tag", "") for t in f.get("tags", [])))
    out.append("dependencies : %s" % f.get("children"))
    out.append("file_size    : %s  time_updated: %s" % (f.get("file_size"), f.get("time_updated")))
    desc = f.get("description", "") or ""
    out.append("desc_len     : %d" % len(desc))
    out.append("--- description ---")
    out.append(desc)

path = os.path.join(os.environ.get("TEMP", "."), "steam_items.txt")
open(path, "w", encoding="utf-8").write("\n".join(out))
print("report ->", path)
for f in resp.get("publishedfiledetails", []):
    print("  id=%s result=%s len=%s title=%s" % (
        f.get("publishedfileid"), f.get("result"),
        len(f.get("description", "") or ""),
        (f.get("title") or "").encode("ascii", "replace").decode()))
