# -*- coding: utf-8 -*-
"""扫 pages/*.html 里的 <figure> 图集，汇总成一个总览墙页面。
零联网，纯本地解析。输出 pages/图集总览.html
"""
import re, os, json, glob, html

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
PAGES = os.path.join(ROOT, 'pages')

def strip_tags(s):
    s = re.sub(r'<br\s*/?>', ' ', s)
    s = re.sub(r'<[^>]+>', '', s)
    return html.unescape(re.sub(r'\s+', ' ', s)).strip()

def parse_page(path):
    src = open(path, encoding='utf-8', errors='ignore').read()
    title = re.search(r'<title>(.*?)</title>', src, re.S)
    title = strip_tags(title.group(1)) if title else os.path.basename(path)
    sub = re.search(r'class="sub">(.*?)</p>', src, re.S)
    sub = strip_tags(sub.group(1)) if sub else ''
    items = []
    for m in re.finditer(r'<figure>(.*?)</figure>', src, re.S):
        block = m.group(1)
        img = re.search(r'<img[^>]+src="([^"]+)"', block)
        if not img:
            continue
        rel = img.group(1)
        cap = re.search(r'<figcaption>(.*?)</figcaption>', block, re.S)
        cap_html = cap.group(1) if cap else ''
        name = re.search(r'<b>(.*?)</b>', cap_html, re.S)
        name = strip_tags(name.group(1)) if name else ''
        tags = re.findall(r'<span class="(act|ok)">(.*?)</span>', cap_html, re.S)
        desc = cap_html
        if name:
            desc = desc.replace(re.search(r'<b>.*?</b>', desc, re.S).group(0), '', 1)
        desc = re.sub(r'<span class="(act|ok)">.*?</span>', '', desc, flags=re.S)
        items.append({
            'img': rel,
            'name': name,
            'tags': [{'k': k, 'v': strip_tags(v)} for k, v in tags],
            'desc': strip_tags(desc),
            'page': os.path.basename(path),
            'pageTitle': title,
        })
    return {'file': os.path.basename(path), 'title': title, 'sub': sub, 'items': items}

def main():
    albums = []
    for p in sorted(glob.glob(os.path.join(PAGES, '*.html'))):
        if os.path.basename(p) == '图集总览.html':
            continue
        a = parse_page(p)
        if a['items']:
            albums.append(a)
    def order(a):
        m = re.search(r'第(\d+)轮', a['file'])
        if m:
            return (0, -int(m.group(1)))
        z = re.search(r'Z系列', a['file'])
        if z:
            return (1, a['file'])
        return (2, a['file'])
    albums.sort(key=order)
    data = json.dumps(albums, ensure_ascii=False)
    total = sum(len(a['items']) for a in albums)
    tpl = open(os.path.join(os.path.dirname(__file__), 'template.html'), encoding='utf-8').read()
    out = tpl.replace('/*__DATA__*/', data).replace('__ALBUMS__', str(len(albums))).replace('__IMAGES__', str(total))
    dst = os.path.join(PAGES, '图集总览.html')
    open(dst, 'w', encoding='utf-8').write(out)
    print('albums=%d images=%d -> %s' % (len(albums), total, dst))

if __name__ == '__main__':
    main()
