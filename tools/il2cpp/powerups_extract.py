#!/usr/bin/env python
# 从 dump.cs 抽出指定命名空间下所有类的字段清单（含继承链），供数值设计参考
# 注意：Il2CppDumper 的命名空间是独立的 "// Namespace: X" 注释行，不写在类名里
import re, sys, json, collections

dump = sys.argv[1]
out = sys.argv[2]
ns_filter = sys.argv[3] if len(sys.argv) > 3 else 'NEON.Game.PowerUps'

CLASS_RE = re.compile(r'^\s*(?:public|internal|private|protected)?\s*'
                      r'(?:(?:sealed|abstract|static|readonly)\s+)*'
                      r'(class|struct|enum|interface)\s+([\w\.]+)(?:\s*:\s*([\w\.]+))?')
FIELD_RE = re.compile(r'^\t+(?:(public|private|internal|protected)\s+)?'
                      r'(?:\[[^\]]*\]\s+)*'
                      r'(?:readonly\s+|static\s+|const\s+)*'
                      r'([\w\[\]<>,.`\s]+?)\s+(\w+)\s*;\s*//\s*0x[0-9A-Fa-f]+\s*$')
NS_RE = re.compile(r'^// Namespace:\s*(.*)$')

types = {}          # 限定名 -> {base, fields}
short2full = {}     # 短名 -> 限定名（解析基类用）
cur = None
cur_ns = ''
attrs = []
in_fields = False

for line in open(dump, encoding='utf-8', errors='ignore'):
    m = NS_RE.match(line)
    if m:
        cur_ns = m.group(1).strip()
        continue
    st = line.strip()
    m = CLASS_RE.match(line)
    if m and not st.startswith('//'):
        short = m.group(2)
        full = (cur_ns + '.' + short) if cur_ns else short
        cur = full
        types[cur] = {'base': m.group(3), 'fields': []}
        short2full.setdefault(short, full)
        in_fields = False
        attrs = []
        continue
    if cur is None:
        continue
    if st == '// Fields':
        in_fields = True
        continue
    if st.startswith('// Properties') or st.startswith('// Methods'):
        in_fields = False
        continue
    if st.startswith('[') and st.endswith(']'):
        attrs.append(st)
        continue
    if not in_fields:
        continue
    fm = FIELD_RE.match(line)
    if fm:
        mod, ftype, fname = fm.group(1), fm.group(2).strip(), fm.group(3)
        has_sf = any('SerializeField' in a for a in attrs)
        non_ser = any('NonSerialized' in a for a in attrs)
        ser = (mod == 'public' or has_sf) and not non_ser
        types[cur]['fields'].append((fname, ftype, mod or '', ser))
        attrs = []

def fullname(b):
    b = b.split(',')[0].strip()
    if b in types:
        return b
    return short2full.get(b, b)

def chain(cls, _seen=None):
    if _seen is None:
        _seen = set()
    if cls in _seen or cls not in types:
        return []
    _seen.add(cls)
    out = []
    b = types[cls].get('base')
    if b:
        out += chain(fullname(b), _seen)
    out += [(cls, n, t) for n, t, m, s in types[cls]['fields'] if s]
    return out

targets = [c for c in types if c.startswith(ns_filter + '.')]
print(f'{ns_filter} 下 {len(targets)} 个类')

res = {}
for c in targets:
    res[c] = chain(c)

json.dump(res, open(out, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

nfield = sum(len(v) for v in res.values())
print(f'字段总数 {nfield}')
fc = collections.Counter()
for c, fl in res.items():
    for _, n, t in fl:
        fc[n] += 1
print('\n最常见字段名 TOP30:')
for n, k in fc.most_common(30):
    print(f'  {k:>5}  {n}')

# 按「本类自己声明的字段」分组，看道具各自的差异化参数
print('\n=== 自带字段最多的类 TOP15 ===')
own = sorted(((c, [f for f in res[c] if f[0] == c]) for c in targets),
             key=lambda x: -len(x[1]))[:15]
for c, fl in own:
    print(f'  {len(fl):>3}  {c.split(".")[-1]}')
    for oc, n, t in fl:
        print(f'          {n}: {t}')
