# -*- coding: utf-8 -*-
"""解析 RitsuLib 的 XML API 文档，抽出公开 API 表面。
用法: python parse_xml_api.py <xml文件...> 
输出: 每个 assembly 的类型清单 / 命名空间分布 / 成员统计。
"""
import sys, os, re, collections
import xml.etree.ElementTree as ET

def strip_ns(tag):
    return tag.split('}', 1)[-1]

def text_of(elem):
    """把 <summary> 里的双语段落合并成一行中文优先的说明。"""
    parts = []
    for child in elem.iter():
        if strip_ns(child.tag) == 'para':
            lang = child.get('{http://www.w3.org/XML/1998/namespace}lang', '')
            t = ''.join(child.itertext()).strip()
            t = re.sub(r'\s+', ' ', t)
            if not t:
                continue
            parts.append(('zh' if lang.startswith('zh') else 'en', t))
    if not parts:
        t = ''.join(elem.itertext()).strip()
        return re.sub(r'\s+', ' ', t)
    zh = [t for k, t in parts if k == 'zh']
    en = [t for k, t in parts if k == 'en']
    return (zh[0] if zh else (en[0] if en else ''))

def parse(path):
    tree = ET.parse(path)
    root = tree.getroot()
    asm = ''
    members = []
    for m in root.iter():
        if strip_ns(m.tag) == 'assembly':
            for n in m.iter():
                if strip_ns(n.tag) == 'name':
                    asm = (n.text or '').strip()
        if strip_ns(m.tag) == 'member':
            name = m.get('name', '')
            summary = ''
            for c in m:
                if strip_ns(c.tag) == 'summary':
                    summary = text_of(c)
            members.append((name, summary))
    return asm, members

def kind(name):
    return name[0] if name else '?'

def type_full_name(name):
    return name[2:]

def ns_of(full):
    if '.' in full:
        return full.rsplit('.', 1)[0]
    return '(root)'

def short(full):
    return full.rsplit('.', 1)[-1]

def main(paths):
    for p in paths:
        if not os.path.exists(p):
            print('!! 缺失: %s' % p); continue
        asm, members = parse(p)
        types = [(n[2:], s) for n, s in members if n.startswith('T:')]
        meth = [(n[2:], s) for n, s in members if n.startswith('M:')]
        prop = [(n[2:], s) for n, s in members if n.startswith('P:')]
        fld  = [(n[2:], s) for n, s in members if n.startswith('F:')]
        evt  = [(n[2:], s) for n, s in members if n.startswith('E:')]
        print('=' * 78)
        print('ASSEMBLY  %s   (%s)' % (asm, os.path.basename(p)))
        print('  类型 %d | 方法 %d | 属性 %d | 字段 %d | 事件 %d | 合计 %d'
              % (len(types), len(meth), len(prop), len(fld), len(evt), len(members)))
        groups = collections.defaultdict(list)
        for full, s in types:
            groups[ns_of(full)].append((short(full), s))
        print('  命名空间 %d 个：' % len(groups))
        for ns in sorted(groups, key=lambda k: (-len(groups[k]), k)):
            print('    %-58s %3d' % (ns, len(groups[ns])))
        print()

if __name__ == '__main__':
    main(sys.argv[1:])
