# 调研：杀戮尖塔2创意工坊热门模组的说明文都怎么写
# 结果写到 %TEMP%\workshop_survey.txt（UTF-8）
import collections, json, os, re, urllib.parse, urllib.request

APPID = "2868840"
PROXY = "http://127.0.0.1:7892"
BROWSE = ("https://steamcommunity.com/workshop/browse/?appid=%s"
          "&browsesort=trend&section=readytouseitems&days=365" % APPID)
UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/131.0 Safari/537.36")


def fetch(url, proxy=None, data=None):
    handlers = [urllib.request.ProxyHandler(
        {"http": proxy, "https": proxy} if proxy else {})]
    op = urllib.request.build_opener(*handlers)
    op.addheaders = [("User-Agent", UA), ("Accept-Language", "en-US,en;q=0.9")]
    with op.open(url, data=data or None, timeout=45) as r:
        return r.read().decode("utf-8", "replace")


# ---------- 1. 抓热门列表 ----------
html, used = None, None
for p in (None, PROXY):
    try:
        html = fetch(BROWSE, p)
        used = p
        print("browse ok  proxy=%s  %d bytes" % (p, len(html)))
        break
    except Exception as e:
        print("browse FAIL proxy=%s  %s" % (p, e))

ids = []
if html:
    ids = list(dict.fromkeys(re.findall(
        r"sharedfiles/filedetails/\?id=(\d+)", html)))[:10]
print("found ids:", ids)

# ---------- 2. 查说明文 ----------
API = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/"


def details(item_ids):
    data = {"itemcount": len(item_ids)}
    for i, x in enumerate(item_ids):
        data["publishedfileids[%d]" % i] = str(x)
    body = urllib.parse.urlencode(data).encode()
    req = urllib.request.Request(API, data=body, headers={
        "Content-Type": "application/x-www-form-urlencoded", "User-Agent": UA})
    op = urllib.request.build_opener(urllib.request.ProxyHandler({}))
    with op.open(req, timeout=45) as r:
        return json.load(r)


out = []
markers = collections.Counter()
files = []
if ids:
    for f in details(ids)["response"].get("publishedfiledetails", []):
        if f.get("result") != 1:
            continue
        d = f.get("description", "") or ""
        title = f.get("title", "")
        out.append("=" * 70)
        out.append("%s   (id %s, %d 字, 标签: %s)" % (
            title, f.get("publishedfileid"), len(d),
            ", ".join(t.get("tag", "") for t in f.get("tags", []))))
        out.append("-" * 70)
        out.append(d)
        files.append((title, len(d), d))
        for m in re.findall(r"\[(h1|h2|h3|b|i|list|hr|url|table|img|code|noparse|quote)\]", d):
            markers[m] += 1
        zh = len(re.findall(r"[\u4e00-\u9fff]", d))
        out.append("")

report = [
    "热门模组说明文调研（按趋势排序前 %d 个）" % len(files),
    "BBCode 标记使用次数：%s" % dict(markers.most_common()),
    "中文占比：%s" % ", ".join("%s=%d%%" % (t, round(100 * len(re.findall(r'[\u4e00-\u9fff]', d)) / max(1, len(d))))
                              for t, _, d in files),
    "", "各篇字数：" + ", ".join("%s=%d" % (t, n) for t, n, _ in files),
    "", "=" * 70, ""]
open(os.path.join(os.environ.get("TEMP", "."), "workshop_survey.txt"),
     "w", encoding="utf-8").write("\n".join(report + out))
print("markers:", dict(markers.most_common()))
print("sizes:", [(t[:20].encode('ascii', 'replace').decode(), n) for t, n, _ in files])
