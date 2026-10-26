"""
扫 *_Data 目录，收集所有 MonoScript，建立 pathID -> (类名/命名空间/程序集) 映射。

IL2CPP 游戏里 MonoScript 通常集中在一个文件（如 globalgamemanagers.assets），
而业务资产（level2 / resources.assets）里的 MonoBehaviour 通过 PPtr 跨文件引用它。
不先建这张表，就没法知道一个裸二进制对象对应哪个 C# 类。

用法:
    python build_script_map.py <游戏_Data目录> <输出.json>
"""
import os
import sys
import json
import struct
from collections import Counter

import UnityPy


def rdstr(b, p):
    if p + 4 > len(b):
        return None, p + 4
    ln = struct.unpack_from('<i', b, p)[0]
    if ln < 0 or ln > 500 or p + 4 + ln > len(b):
        return None, p + 4
    try:
        s = b[p + 4:p + 4 + ln].decode('utf-8')
    except Exception:
        return None, p + 4
    return s, (p + 4 + ln + 3) // 4 * 4


def parse_monoscript(raw):
    """MonoScript raw: m_Name / m_ClassName / m_Namespace / m_AssemblyName"""
    p = 0
    name, p = rdstr(raw, p)
    # 部分版本 m_Name 后有 m_ExecutionOrder + m_PropertiesHash(16B)
    cls, p2 = rdstr(raw, p)
    if cls is None:
        # 试着跳过 4(int) + 16(hash) = 20 字节
        p2 = p + 20
        cls, p2 = rdstr(raw, p2)
    ns, p3 = rdstr(raw, p2)
    asm, _ = rdstr(raw, p3)
    return cls, ns, asm, name


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(1)
    data_dir, out_path = sys.argv[1], sys.argv[2]

    m = {}
    dup = Counter()
    per_file = []
    for f in sorted(os.listdir(data_dir)):
        fp = os.path.join(data_dir, f)
        if not os.path.isfile(fp):
            continue
        if f.endswith(('.resS', '.resource', '.dat', '.dll', '.exe', '.json', '.txt', '.bin')):
            continue
        if f.startswith('sharedassets') and f.endswith(('.assets',)):
            pass  # 也扫，可能含脚本
        if os.path.getsize(fp) > 600 * 1048576:
            print(f'  [跳过] {f} 过大 {os.path.getsize(fp)/1048576:.0f}MB')
            continue
        try:
            env = UnityPy.load(fp)
        except Exception:
            continue
        n = 0
        for o in env.objects:
            if o.type.name != 'MonoScript':
                continue
            cls = None
            try:
                d = o.read_typetree()
                cls = d.get('m_ClassName')
                ns = d.get('m_Namespace') or ''
                asm = d.get('m_AssemblyName') or ''
            except Exception:
                d = None
            if not cls:
                try:
                    cls, ns, asm, _nm = parse_monoscript(o.get_raw_data())
                except Exception:
                    cls = None
            if not cls:
                continue
            if o.path_id in m and m[o.path_id][0] != cls:
                dup[o.path_id] += 1
            m[o.path_id] = [cls, ns or '', asm or '']
            n += 1
        if n:
            per_file.append((f, n))
            print(f'  {f}: {n} 个 MonoScript')

    print(f'\n合计 {len(m)} 个脚本，冲突 {len(dup)} 个 pathID')
    json.dump({str(k): v for k, v in m.items()},
              open(out_path, 'w', encoding='utf-8'), ensure_ascii=False)
    print(f'-> {out_path}')

    # 命名空间分布
    c = Counter(v[1] for v in m.values())
    print('\n脚本命名空间 TOP15:')
    for ns, n in c.most_common(15):
        print(f'  {n:>5}  {ns or "(全局)"}')


if __name__ == '__main__':
    main()
