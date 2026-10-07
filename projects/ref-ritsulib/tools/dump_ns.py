# -*- coding: utf-8 -*-
"""按命名空间前缀导出 RitsuLib 的成员清单（带中文文档摘要）。
用法:
  python dump_ns.py <xml> <ns前缀>            # 列出该 ns 下所有类型 + 其成员
  python dump_ns.py <xml> <ns前缀> --type Foo # 只看某个类型
  python dump_ns.py <xml> --list              # 列出所有类型名（裸清单）
"""
import sys, os, re
import xml.etree.ElementTree as ET

XMLNS = '{http://www.w3.org/XML/1998/namespace}lang'

def strip_ns(tag):
    return tag.split('}', 1)[-1]

def text_of(elem):
    parts = []
    for child in elem.iter():
        if strip_ns(child.tag) == 'para':
            lang = child.get(XMLNS, '')
            t = re.sub(r'\s+', ' ', ''.join(child.itertext())).strip()
            if t:
                parts.append(('zh' if lang.startswith('zh') else 'en', t))
    if not parts:
        return re.sub(r'\s+', ' ', ''.join(elem.itertext())).strip()
    zh = [t for k, t in parts if k == 'zh']
    en = [t for k, t in parts if k == 'en']
    return zh[0] if zh else (en[0] if en else '')

def load(path):
    out = []
    for m in ET.parse(path).getroot().iter():
        if strip_ns(m.tag) != 'member':
            continue
        name = m.get('name', '')
        summary = params = returns = ''
        for c in m:
            t = strip_ns(c.tag)
            if t == 'summary':
                summary = text_of(c)
            elif t == 'param':
                params += ' [%s] %s' % (c.get('name', ''), text_of(c))
            elif t == 'returns':
                returns = text_of(c)
        out.append((name, summary, params, returns))
    return out

def pretty(name):
    """M:Ns.Type.Method(System.String,X) -> Type.Method(String,X)"""
    k, body = name[:1], name[2:]
    paren = ''
    if '(' in body:
        body, rest = body.split('(', 1)
        args = rest.rstrip(')')
        args = re.sub(r'System\.', '', args)
        args = re.sub(r'STS2RitsuLib(\.\w+)*\.', '', args)
        args = re.sub(r'\b([A-Za-z_][\w.]*)\.([\w`]+)', r'\2', args)
        paren = '(' + args + ')'
    if '.' in body:
        ns, short = body.rsplit('.', 1)
        return '%s%s%s' % (short, '(' if paren else '', paren.lstrip('(') if paren else '')
    return body

def main():
    path, rest = sys.argv[1], sys.argv[2:]
    if '--list' in rest:
        members = load(path)
        for n, s, _, _ in members:
            if n.startswith('T:'):
                print(n[2:])
        return
    prefix = rest[0] if rest and not rest[0].startswith('--') else ''
    only = None
    if '--type' in rest:
        only = rest[rest.index('--type') + 1]
    members = load(path)
    types = {}
    for n, s, p, r in members:
        if n.startswith('T:'):
            types[n[2:]] = s
    order = [t for t in types if t.startswith(prefix)]
    if only:
        order = [t for t in order if t.split('.')[-1] == only or t == only]
    for t in sorted(order):
        print('─' * 76)
        print('▌ %s' % t)
        if types[t]:
            print('    %s' % types[t])
        kids = [m for m in members if not m[0].startswith('T:') and m[0][2:].startswith(t + '.')
                and m[0][2:].rsplit('.', 1)[0] == t]
        for n, s, p, r in kids:
            tag = {'M': '方法', 'P': '属性', 'F': '字段', 'E': '事件'}.get(n[0], n[0])
            print('  · %s %s' % (tag, n[2:][len(t) + 1:]))
            if s:
                print('      %s' % s[:220])
            if p:
                print('    参数%s' % p[:200])
        if kids:
            print()

if __name__ == '__main__':
    main()
