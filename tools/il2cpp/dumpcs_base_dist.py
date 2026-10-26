import re, collections, sys

dump = sys.argv[1]
nsprefix = sys.argv[2]
CR = re.compile(r'^\s*(?:public|internal|private|protected)?\s*'
                r'(?:(?:sealed|abstract|static|readonly)\s+)*'
                r'(class|struct|enum|interface)\s+([\w\.]+)(?:\s*:\s*([\w\.]+))?')
NR = re.compile(r'^// Namespace:\s*(.*)$')
ns = ''
base = {}
for line in open(dump, encoding='utf-8', errors='ignore'):
    m = NR.match(line)
    if m:
        ns = m.group(1).strip()
        continue
    m = CR.match(line)
    if m and not line.strip().startswith('//'):
        full = (ns + '.' + m.group(2)) if ns else m.group(2)
        if full.startswith(nsprefix + '.'):
            base[full] = (m.group(3) or '').split(',')[0].strip()
c = collections.Counter(base.values())
print('总类数', len(base))
print('直接基类分布 TOP25:')
for k, v in c.most_common(25):
    print('  %5d  %s' % (v, k or '(无)'))
