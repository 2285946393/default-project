"""
IL2CPP 游戏数值提取器（不依赖 Il2CppDumper / typetree）

用法:
    python extract_il2cpp_values.py "<游戏_Data目录>" <输出json>

原理:
  1. UnityPy 读 SerializedFile（IL2CPP 也能读 raw_data，只是 typetree 会失败）
  2. MonoBehaviour 的 m_Name 在 raw_data 偏移 0x1C（int32 长度 + 字符串）
  3. ★ 字段名锚定法：找 `[int32 长度][ASCII字段名]`（如 `\x09\x00\x00\x00_maxUses`），
     值就在 align4(字符串结束) 或 align8 处的 double
     → 不需要 typetree 就能读出全部 ScriptableObject 数值

输出: [{name, file, len, fields:[[字段名, double值], ...]}, ...]
"""
import os
import re
import sys
import json
import struct

import UnityPy


def mono_name(raw):
    """MonoBehaviour 的 m_Name 在 0x1C"""
    if len(raw) < 0x20:
        return None
    ln = struct.unpack_from('<i', raw, 0x1C)[0]
    if ln <= 0 or ln > 300 or 0x20 + ln > len(raw):
        return None
    try:
        return raw[0x20:0x20 + ln].decode('utf-8')
    except Exception:
        return None


def body_of(raw, ln):
    off = (0x20 + ln + 3) // 4 * 4
    return raw[off:]


def pretty(v):
    """只保留"像人填的"数值，滤掉指针误读"""
    if v != v or v == 0:
        return False
    if abs(v) < 1e-6 or abs(v) > 1e15:
        return False
    x = abs(v) * 1e6
    return abs(x - round(x)) < 1e-3


def extract_fields(b):
    """字段名锚定法：长度前缀 + ASCII 字段名 -> 后面的 double"""
    recs = []
    for m in re.finditer(rb'[A-Za-z_][A-Za-z0-9_]{2,40}', b):
        s = m.start()
        if s < 4:
            continue
        L = struct.unpack_from('<i', b, s - 4)[0]
        if L != m.end() - s:
            continue
        end = m.end()
        for q in ((end + 3) // 4 * 4, (end + 7) // 8 * 8):
            if q + 8 > len(b):
                continue
            v = struct.unpack_from('<d', b, q)[0]
            if pretty(v):
                recs.append([b[s:end].decode('ascii'), round(v, 8)])
                break
    return recs


def scan_file(path, out):
    try:
        env = UnityPy.load(path)
    except Exception as e:
        print(f'  [skip] {os.path.basename(path)}: {e}')
        return
    n = 0
    for o in env.objects:
        if o.type.name != 'MonoBehaviour':
            continue
        try:
            raw = o.get_raw_data()
        except Exception:
            continue
        if len(raw) < 0x20:
            continue
        ln = struct.unpack_from('<i', raw, 0x1C)[0]
        if ln <= 0 or ln > 300 or 0x20 + ln > len(raw):
            continue
        nm = mono_name(raw)
        if not nm:
            continue
        try:
            nm.encode('ascii')
        except Exception:
            continue
        b = body_of(raw, ln)
        f = extract_fields(b)
        if f:
            out.append({'name': nm, 'file': os.path.basename(path),
                        'len': len(b), 'fields': f})
            n += 1
    print(f'  {os.path.basename(path)}: {n} 个有数值的对象')


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(1)
    data_dir, out_path = sys.argv[1], sys.argv[2]

    # 挑出可能含 ScriptableObject 的文件（按大小降序，跳过 resS 大资源）
    cands = []
    for f in os.listdir(data_dir):
        fp = os.path.join(data_dir, f)
        if not os.path.isfile(fp):
            continue
        if f.endswith('.resS') or f.endswith('.dat'):
            continue
        if f.startswith('level') or f.endswith('.assets'):
            cands.append((os.path.getsize(fp), fp))
    cands.sort(reverse=True)
    cands = cands[:12]

    out = []
    for sz, fp in cands:
        print(f'[scan] {os.path.basename(fp)} ({sz/1048576:.1f}MB)')
        scan_file(fp, out)

    json.dump(out, open(out_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    total = sum(len(r['fields']) for r in out)
    print(f'\n完成：{len(out)} 个对象 / {total} 个数值 -> {out_path}')


if __name__ == '__main__':
    main()
