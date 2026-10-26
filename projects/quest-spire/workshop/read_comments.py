# 读创意工坊条目的留言区（看格式 / 别人留言的写法）
# 用法： python read_comments.py <工坊id> [条数]
import os, re, sys, urllib.parse, urllib.request

UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/131.0 Safari/537.36")
PROXY = "http://127.0.0.1:7892"


def fetch(url):
    for proxy in (PROXY, None):
        try:
            op = urllib.request.build_opener(urllib.request.ProxyHandler(
                {"http": proxy, "https": proxy} if proxy else {}))
            op.addheaders = [("User-Agent", UA), ("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8")]
            with op.open(url, timeout=60) as r:
                return r.read().decode("utf-8", "replace")
        except Exception as e:
            last = e
    raise SystemExit("抓取失败: %s" % last)


def clean(h):
    h = re.sub(r"<br\s*/?>", " / ", h)
    h = re.sub(r"<[^>]+>", "", h)
    h = urllib.parse.unquote(h)
    return " ".join(h.split())


def main(item_id, limit):
    if str(item_id).startswith("http"):
        url = item_id
    else:
        url = "https://steamcommunity.com/sharedfiles/filedetails/?id=%s" % item_id
    html = fetch(url)
    out = []

    # 留言框（未登录时是什么样）
    box = re.search(r'<div[^>]*class="[^"]*commentthread_area[^"]*"[^>]*>(.*?)</div>\s*</div>', html, re.S)
    area = re.search(r'commentthread_area(.{0,1200})', html, re.S)
    if area:
        out.append("【留言框区原始片段】")
        out.append(clean(area.group(1))[:400])

    # 已有留言：Steam 的注释区里作者/时间/正文是三组同序的元素，分别抓再按顺序配对
    authors = re.findall(r'commentthread_author_link[^>]*>(.*?)</a>', html, re.S)
    dates = re.findall(r'commentthread_comment_timestamp[^>]*>(.*?)</(?:a|span)>', html, re.S)
    texts = re.findall(r'commentthread_comment_text"[^>]*>(.*?)</div>', html, re.S)
    n = max(len(authors), len(texts))
    out.append("")
    out.append("【已有留言 %d 条，抓取名/时间/正文 = %d/%d/%d】"
               % (n, len(authors), len(dates), len(texts)))
    for i in range(min(n, limit)):
        out.append("--- %d ---" % (i + 1))
        out.append("  昵称: %s" % (clean(authors[i]) if i < len(authors) else "?"))
        out.append("  时间: %s" % (clean(dates[i]) if i < len(dates) else "?"))
        out.append("  内容: %s" % (clean(texts[i])[:400] if i < len(texts) else "?"))
    if n == 0:
        out.append("  （这个条目还没有人留言）")

    # 留言里有几个带链接的（看工坊允不允许放网址）
    with_link = sum(1 for t in texts if "http" in t or "www." in t)
    out.append("")
    out.append("【统计】共 %d 条，其中带网址 %d 条，正文平均 %d 字"
               % (len(texts), with_link,
                  (sum(len(clean(t)) for t in texts) // max(1, len(texts)))))

    tag = re.sub(r"[^0-9A-Za-z]+", "_", str(item_id))[-40:]
    p = os.path.join(os.environ["TEMP"], "steam_comments_%s.txt" % tag)
    open(p, "w", encoding="utf-8").write("\n".join(out))
    print("report ->", p)
    print("comments:", n)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "3806270140",
         int(sys.argv[2]) if len(sys.argv) > 2 else 8)
