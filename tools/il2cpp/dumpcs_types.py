"""
从 Il2CppDumper 的 dump.cs 提取「类 -> 字段列表」类型表。

作用：IL2CPP 游戏的资产里没有类型树（typetree），只有裸二进制。
但 dump.cs 给了每个类的字段名 + 类型名，据此可以按 Unity 序列化规则
自己算出每个字段在资产里的偏移，从而读出真实数值。

用法:
    python dumpcs_types.py <dump.cs> <输出.json> [--stats]

输出 json: {"类名(含命名空间)": [["字段名","类型"], ...], ...}
"""
import os
import re
import sys
import json
from collections import Counter

# 类声明：// Namespace: X \n [modifiers] class Name : Base // TypeDefIndex: N
CLASS_RE = re.compile(
    r'^\s*(?:public|internal|private|protected)?\s*'
    r'(?:(?:sealed|abstract|static|readonly)\s+)*'
    r'(?:class|struct|enum|interface)\s+(\w+)'
)

# 字段行: "\tpublic GameObject SourceGameobject; // 0x10"
FIELD_RE = re.compile(
    r'^\t+(?:(public|private|internal|protected)\s+)?'
    r'([\w\[\]<>,.`\s]+?)\s+(\w+)\s*;\s*//\s*0x([0-9A-Fa-f]+)\s*$'
)

# 明显不是 Unity 能序列化的字段
SKIP_MODIFIERS = ('static', 'const', 'readonly', 'readonly static')


def parse_dumpcs(path):
    types = {}
    cur_ns = ''
    cur_cls = None
    in_fields = False
    fields = []
    ns_map = {}

    with open(path, encoding='utf-8', errors='ignore') as f:
        for line in f:
            s = line.rstrip('\n')

            if s.startswith('// Namespace:'):
                cur_ns = s.split(':', 1)[1].strip()
                continue

            if s.startswith('// Image') or s.startswith('// RVA'):
                continue

            # 新类开始
            if not s.startswith('\t') and ('class ' in s or 'struct ' in s or 'enum ' in s):
                m = CLASS_RE.match(s)
                if m:
                    # 保存上一个
                    if cur_cls and fields:
                        types.setdefault(cur_cls, []).extend(fields)
                    cur_cls = (cur_ns + '.' + m.group(1)) if cur_ns else m.group(1)
                    ns_map[cur_cls] = cur_ns
                    fields = []
                    in_fields = False
                    continue

            if s.strip() == '// Fields':
                in_fields = True
                continue
            if s.strip() in ('// Properties', '// Methods', '// Nested types'):
                in_fields = False
                continue

            if in_fields and cur_cls:
                m = FIELD_RE.match(s)
                if not m:
                    continue
                mod, ftype, fname, _off = (
                    m.group(1) or '', m.group(2).strip(), m.group(3), m.group(4)
                )
                if any(k in ftype.lower().split() for k in SKIP_MODIFIERS):
                    continue
                # 只保留 public 字段
                # （[SerializeField] 私有字段无法从 dump.cs 判断，会漏，见笔记）
                if mod != 'public':
                    continue
                fields.append([fname, ftype])

    if cur_cls and fields:
        types.setdefault(cur_cls, []).extend(fields)
    return types, ns_map


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(1)
    src, out_path = sys.argv[1], sys.argv[2]
    stats = '--stats' in sys.argv

    types, ns_map = parse_dumpcs(src)
    print(f'解析到 {len(types)} 个类 / {sum(len(v) for v in types.values())} 个 public 字段')

    if stats:
        # 字段类型频次 top40
        c = Counter()
        for fs in types.values():
            for _, t in fs:
                c[t] += 1
        print('\n字段类型 TOP40:')
        for t, n in c.most_common(40):
            print(f'  {n:>6}  {t}')

        # 命名空间频次 top25
        cn = Counter(ns_map.values())
        print('\n命名空间 TOP25:')
        for ns, n in cn.most_common(25):
            print(f'  {n:>5}  {ns or "(全局)"}')

    json.dump(types, open(out_path, 'w', encoding='utf-8'), ensure_ascii=False)
    print(f'\n-> {out_path}')


if __name__ == '__main__':
    main()
