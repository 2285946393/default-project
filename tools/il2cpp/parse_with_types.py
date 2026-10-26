"""
用 dump.cs 的类型信息解析 IL2CPP 资产里的 MonoBehaviour / ScriptableObject 数值。

背景：IL2CPP 游戏的资产里**没有类型树**，只有按类型树排布的裸二进制。
Il2CppDumper 出的 dump.cs 给了「类 -> 字段名 + 类型名」，据此可以自己
重算 Unity 的序列化布局，把数值读出来。

★ 自校验机制：解析结束时消耗的字节数必须等于资产实际长度（差 <= 3 的对齐余量）。
 只有 exact 命中的结果才可信 —— 这样即使某些字段判断错了也只会丢弃，不会给出错值。

用法:
    python parse_with_types.py <dump.cs> <资产文件或目录> <输出.json> [--only 类名] [--maxfile MB]
"""
import os
import re
import sys
import json
import struct
from collections import Counter

import UnityPy

# ---------------------------------------------------------------- 类型系统

PRIM = {  # 名 -> (size, align, struct格式)
    'bool': (1, 1, 'b'), 'sbyte': (1, 1, 'b'), 'byte': (1, 1, 'B'),
    'short': (2, 2, 'h'), 'ushort': (2, 2, 'H'), 'char': (2, 2, 'H'),
    'int': (4, 4, 'i'), 'uint': (4, 4, 'I'), 'float': (4, 4, 'f'),
    'long': (8, 8, 'q'), 'ulong': (8, 8, 'Q'), 'double': (8, 8, 'd'),
}

# Unity 内置值类型：(字段列表[(名, 类型)], align)
BUILTIN = {
    'Vector2': ([('x', 'float'), ('y', 'float')], 4),
    'Vector2Int': ([('x', 'int'), ('y', 'int')], 4),
    'Vector3': ([('x', 'float'), ('y', 'float'), ('z', 'float')], 4),
    'Vector3Int': ([('x', 'int'), ('y', 'int'), ('z', 'int')], 4),
    'Vector4': ([('x', 'float'), ('y', 'float'), ('z', 'float'), ('w', 'float')], 4),
    'Quaternion': ([('x', 'float'), ('y', 'float'), ('z', 'float'), ('w', 'float')], 4),
    'Color': ([('r', 'float'), ('g', 'float'), ('b', 'float'), ('a', 'float')], 4),
    'Color32': ([('r', 'byte'), ('g', 'byte'), ('b', 'byte'), ('a', 'byte')], 1),
    'Rect': ([('x', 'float'), ('y', 'float'), ('w', 'float'), ('h', 'float')], 4),
    'RectInt': ([('x', 'int'), ('y', 'int'), ('w', 'int'), ('h', 'int')], 4),
    'Matrix4x4': ([('e%02d' % i, 'float') for i in range(16)], 4),
    'Bounds': ([('c', 'Vector3'), ('e', 'Vector3')], 4),
    'LayerMask': ([('m_Mask', 'int')], 4),
    'Hash128': ([('u64_0', 'ulong'), ('u64_1', 'ulong')], 8),
}

# 不参与序列化的类型（Unity 序列化器会跳过它们）
NON_SERIALIZED = {
    'IntPtr', 'UIntPtr', 'Guid', 'Type', 'Delegate', 'MulticastDelegate',
    'XmlQualifiedName', 'CultureInfo', 'Encoding', 'Stream', 'Version',
    'EResult', 'Action', 'Func',
}
NON_SERIALIZED_EXACT = {
    'IntPtr', 'UIntPtr', 'Guid', 'XmlQualifiedName', 'EResult',
    'Action', 'Func', 'Delegate', 'MulticastDelegate', 'Type',
}

# 已知继承自 UnityEngine.Object（引用 = PPtr）的类名
UNITY_OBJ_HINTS = {
    'GameObject', 'Transform', 'Texture', 'Texture2D', 'Texture3D', 'Sprite',
    'Material', 'Mesh', 'Shader', 'MonoScript', 'MonoBehaviour', 'ScriptableObject',
    'AudioClip', 'AnimationClip', 'AnimatorController', 'Font', 'TMP_FontAsset',
    'PlayMakerFSM', 'Renderer', 'MeshRenderer', 'SkinnedMeshRenderer', 'Camera',
    'Light', 'Rigidbody', 'Collider', 'BoxCollider', 'NavMesh', 'Cubemap',
    'SpriteAtlas', 'ComputeShader', 'RenderTexture', 'VideoClip', 'PhysicsMaterial',
    'AnimationCurve', 'Gradient',  # 这两个是可序列化的，单独处理
}

# 注意：dump.cs 里有嵌套类（如 DOTweenSettings.SafeModeOptions），
# 类名必须带点一起抓，否则会覆盖本体字段
CLASS_RE = re.compile(
    r'^\s*(?:public|internal|private|protected)?\s*'
    r'(?:(?:sealed|abstract|static|readonly)\s+)*'
    r'(class|struct|enum|interface)\s+([\w\.]+)(?:\s*:\s*([\w\.]+))?'
)
FIELD_RE = re.compile(
    r'^\t+(?:(public|private|internal|protected)\s+)?'
    r'([\w\[\]<>,.`\s]+?)\s+(\w+)\s*;\s*//\s*0x([0-9A-Fa-f]+)\s*$'
)


def parse_dumpcs(path):
    """-> {cls: {'base': str|None, 'fields': [(name, type)]}}"""
    types = {}
    cur_ns, cur_cls, cur_base, cur_kind = '', None, None, 'class'
    in_fields = False
    fields = []
    attrs = []  # ★ dump.cs 里 [SerializeField] 是独立一行，紧跟在它下面的字段才被序列化
    with open(path, encoding='utf-8', errors='ignore') as f:
        for line in f:
            s = line.rstrip('\n')
            st = s.strip()
            # 特性行（可能是 [SerializeField]，也可能 [TextArea(0, 10)] 之类）
            if in_fields and st.startswith('[') and st.endswith(']'):
                attrs.append(st)
                continue
            if s.startswith('// Namespace:'):
                cur_ns = s.split(':', 1)[1].strip()
                continue
            if s.startswith('// Image') or s.startswith('// RVA'):
                continue
            if not s.startswith('\t') and re.search(r'\b(class|struct|enum|interface)\s+\w', s):
                m = CLASS_RE.match(s)
                if m:
                    if cur_cls and fields:
                        types.setdefault(cur_cls, {'base': cur_base, 'fields': [],
                                                   'kind': cur_kind})['fields'] = fields
                    nm2 = m.group(2)
                    cur_cls = (cur_ns + '.' + nm2) if cur_ns else nm2
                    cur_base = m.group(3)
                    cur_kind = m.group(1)
                    fields, in_fields = [], False
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
                mod, ftype, fname = m.group(1) or '', m.group(2).strip(), m.group(3)
                if ftype.lower().split() and ftype.lower().split()[0] in ('static', 'const', 'readonly'):
                    attrs = []
                    continue
                # ★ Unity 序列化规则：public 字段 + 带 [SerializeField] 的字段
                #   （再排除 [NonSerialized] / const / static / readonly）
                has_sf = any('SerializeField' in a for a in attrs)
                non_ser = any('NonSerialized' in a for a in attrs)
                ser = (mod == 'public' or has_sf) and not non_ser
                attrs = []
                fields.append((fname, ftype, mod, ser))
    if cur_cls and fields:
        types.setdefault(cur_cls, {'base': cur_base, 'fields': [],
                                   'kind': cur_kind})['fields'] = fields
    return types


def is_enum(types, t):
    """该类型是不是 enum（Unity 按底层整型序列化，默认 int32）"""
    info = types.get(t)
    if info is None:
        cand = sorted([k for k in types if short(k) == short(t)], key=len)
        if not cand:
            return False
        info = types[cand[0]]
    return info.get('kind') == 'enum'


# Unity 不会序列化的字段类型/形态（即使它是 public）
def is_nonserialized_field(name, t):
    st = short(t)
    if st in NON_SERIALIZED_EXACT:
        return True
    if st.startswith('Dictionary') or st.startswith('HashSet'):
        return True
    if st.endswith('Delegate') or st.endswith('Callback') or st.endswith('Event'):
        return True
    if t.startswith('Action') or t.startswith('Func'):
        return True
    return False


# 这些基类的字段已经在资产头部处理过（或本身无字段），不再递归
STOP_BASES = {'MonoBehaviour', 'ScriptableObject', 'Object', 'UnityEngine.Object',
              'Component', 'Behaviour', 'MonoScript'}


def fields_chain(types, cls, mode='public', _seen=None):
    """按 Unity 序列化顺序收集字段：先基类，再本类，类内按声明顺序。
    mode='public' 只取 public；'all' 连 private 也取（近似 [SerializeField] 私有字段，
    会多算，靠长度自校验淘汰）。
    """
    out = []
    seen = set()

    def rec(c, depth=0):
        if c in seen or depth > 8:
            return
        seen.add(c)
        info = types.get(c)
        if info is None:
            cand = sorted([k for k in types if short(k) == short(c)], key=len)
            if not cand:
                return
            info = types[cand[0]]
        b = info.get('base')
        if b and short(b) not in STOP_BASES:
            full = next((k for k in types if short(k) == short(b)), b)
            rec(full, depth + 1)
        for f in info['fields']:
            n, t, m = f[0], f[1], f[2]
            ser = f[3] if len(f) > 3 else (m == 'public')
            if mode == 'ser' and not ser:
                continue
            if mode == 'public' and m != 'public':
                continue
            if is_nonserialized_field(n, t):
                continue
            out.append((n, t))

    rec(cls)
    return out


def short(t):
    return t.split('.')[-1]


def flds2(info):
    """把 (名, 类型[, 修饰符]) 统一成 (名, 类型)"""
    return [(x[0], x[1]) for x in info['fields']]


def is_pptr(tname, types, seen=None):
    """判断该类型的字段在序列化流里是 PPtr（12 字节引用）还是内嵌值"""
    st = short(tname)
    if st in ('AnimationCurve', 'Gradient'):
        return False  # 可序列化值类型（特殊，暂当 skip 处理）
    if st in UNITY_OBJ_HINTS and st not in ('AnimationCurve', 'Gradient'):
        return True
    if st in BUILTIN or st in PRIM:
        return False
    seen = seen or set()
    if tname in seen:
        return True
    seen.add(tname)
    info = types.get(tname)
    if info is None:
        # 命名空间不同的同名类，退而求其次按短名找
        cand = sorted([k for k in types if short(k) == st], key=len)
        if not cand:
            return False
        info = types[cand[0]]
        tname = cand[0]
    b = info.get('base')
    if b:
        bs = short(b)
        if bs in UNITY_OBJ_HINTS:
            return True
        if bs in ('Object', 'UnityEngine.Object'):
            return True
        full = next((k for k in types if short(k) == bs), b)
        return is_pptr(full, types, seen)
    return False


def align_up(o, a):
    return (o + a - 1) // a * a


# 防爆：单个对象的字段读取预算（超限即判定布局有误并丢弃）
# 起因：寄居蟹出现过「数组的数组」，逐元素重解析导致成本相乘，
#      一个对象跑了 30+ 分钟。这里给总量和单数组元素数都设上限。
FIELD_BUDGET = 20000      # 一个对象最多读多少字段后用尽（超限 raise）
MAX_ARRAY_ELEMS = 64      # 超过这个元素数的数组：只取前若干个值，其余按元素大小跳过


class Reader:
    """按 Unity 序列化布局读字段"""

    def __init__(self, buf, types, budget=None):
        self.b = buf
        self.types = types
        self.vals = []
        self.end = -1
        self.budget = FIELD_BUDGET if budget is None else budget

    def u(self, off, fmt, size):
        if off + size > len(self.b):
            raise EOFError
        return struct.unpack_from(fmt, self.b, off)[0]

    def layout(self, fields, off, prefix=''):
        """返回字段结束后的 offset；把读到的值放进 self.vals"""
        for name, t in fields:
            off = self.read_field(name, t, off, prefix)
        self.end = align_up(off, 4)
        return self.end

    def read_field(self, name, t, off, prefix=''):
        self.budget -= 1
        if self.budget < 0:
            raise EOFError
        st = short(t)
        inner = None
        m = re.match(r'^([\w\.]+)\s*\[\s*\]$', t) or re.match(r'^List<(.+)>$', t)
        if m:
            inner = m.group(1)

        # 1) 数组 / List
        if inner:
            ist = short(inner)
            if ist in NON_SERIALIZED_EXACT:
                return off
            n = self.u(align_up(off, 4), '<i', 4)
            p = prefix + name
            if n < 0 or n > 100000:
                raise EOFError
            off = align_up(off, 4) + 4
            if ist in PRIM:
                sz, al, fmt = PRIM[ist]
                off = align_up(off, al)
                if ist == 'bool':
                    self.vals.append((p, [bool(self.b[off + i]) for i in range(n)], '[]bool'))
                else:
                    self.vals.append((p, list(struct.unpack_from('<%d%s' % (n, fmt), self.b, off)), '[]' + ist))
                return off + sz * n
            if is_enum(self.types, inner):
                off = align_up(off, 4)
                self.vals.append((p, list(struct.unpack_from('<%di' % n, self.b, off)), '[]enum:' + ist))
                return off + 4 * n
            if ist == 'string':
                out = []
                for i in range(n):
                    ln = self.u(align_up(off, 4), '<i', 4)
                    if ln < 0 or ln > 10000:
                        raise EOFError
                    s = align_up(off, 4) + 4
                    out.append(self.b[s:s + ln].decode('utf-8', 'ignore'))
                    off = align_up(s + ln, 4)
                self.vals.append((p, out, '[]string'))
                return off
            # 元素类型：内嵌 struct 或 PPtr
            if is_pptr(inner, self.types):
                off = align_up(off, 4)
                self.vals.append((p + '.count', n, 'int'))
                return off + 12 * n
            einfo = None
            if inner in self.types:
                einfo = self.types[inner]
            else:
                cand = sorted([k for k in self.types if short(k) == ist], key=len)
                if cand:
                    einfo = self.types[cand[0]]
            if einfo is None or not einfo['fields']:
                raise EOFError
            # 先量一个元素的大小
            probe = Reader(self.b, self.types, self.budget)
            start = align_up(off, 4)
            probe.layout(flds2(einfo), start)
            esz = probe.end - start
            self.budget = probe.budget          # 探测消耗的预算要记账
            # 大数组：只取前 MAX_ARRAY_ELEMS 个元素的值，其余按 esz 跳过。
            # 跳过的字节数仍然正确，外层的长度自校验照常生效。
            take = n if n <= MAX_ARRAY_ELEMS else MAX_ARRAY_ELEMS
            for i in range(take):
                probe2 = Reader(self.b, self.types, self.budget)
                probe2.layout(flds2(einfo), start + i * esz, prefix=p + '[%d].' % i)
                self.budget = probe2.budget
                self.vals.extend(probe2.vals)
            if take < n:
                self.vals.append((p + '.count', n, 'int'))
            return start + esz * n

        # 2) 基础类型
        if st in PRIM:
            sz, al, fmt = PRIM[st]
            off = align_up(off, al)
            v = struct.unpack_from('<' + fmt, self.b, off)[0]
            if st == 'bool':
                v = bool(v)
            self.vals.append((prefix + name, v, st))
            return off + sz

        # 2.5) enum -> 按底层整型（默认 int32）序列化
        if is_enum(self.types, t):
            off = align_up(off, 4)
            v = self.u(off, '<i', 4)
            self.vals.append((prefix + name, v, 'enum:' + st))
            return off + 4

        # 3) string
        if st == 'string':
            ln = self.u(align_up(off, 4), '<i', 4)
            if ln < 0 or ln > 100000:
                raise EOFError
            s = align_up(off, 4) + 4
            v = self.b[s:s + ln].decode('utf-8', 'ignore')
            self.vals.append((prefix + name, v, 'string'))
            return align_up(s + ln, 4)

        # 4) 不可序列化
        if st in NON_SERIALIZED or st in NON_SERIALIZED_EXACT:
            return off
        if st.endswith('Delegate') or st.endswith('Event') or t.endswith('Callback'):
            return off
        if st.startswith('Dictionary') or st.startswith('HashSet'):
            return off

        # 5) Unity 内置值类型
        if st in BUILTIN:
            fl, al = BUILTIN[st]
            off = align_up(off, al)
            o2 = off
            got = []
            for fn, ft in fl:
                r = Reader(self.b, self.types, self.budget)
                r.layout([(fn, ft)], o2)
                self.budget = r.budget
                o2 = r.end
                got.extend([(prefix + name + '.' + k, v, t2) for k, v, t2 in r.vals])
            self.vals.extend(got)
            return o2

        # 6) PPtr 引用
        if is_pptr(t, self.types):
            off = align_up(off, 4)
            fid, pid = struct.unpack_from('<iq', self.b, off)
            self.vals.append((prefix + name, [fid, pid], 'PPtr<' + st + '>'))
            return off + 12

        # 7) 内嵌可序列化 struct/class
        info = self.types.get(t)
        if info is None:
            cand = sorted([k for k in self.types if short(k) == st], key=len)
            if cand:
                info = self.types[cand[0]]
        if info is None or not info['fields']:
            raise EOFError
        sub = Reader(self.b, self.types, self.budget)
        sub.layout(flds2(info), align_up(off, 4), prefix=prefix + name + '.')
        self.budget = sub.budget
        self.vals.extend(sub.vals)
        return sub.end

    def run(self, fields, off=0):
        try:
            return self.layout(fields, off)
        except (EOFError, struct.error, IndexError, ValueError, AttributeError, RecursionError):
            self.end = -1
            return -1


# ---------------------------------------------------------------- 资产解析

def build_script_map(env):
    """path_id -> (类名, 命名空间, 程序集)"""
    m = {}
    for o in env.objects:
        if o.type.name != 'MonoScript':
            continue
        try:
            d = o.read_typetree()
            cls = d.get('m_ClassName')
            if cls:
                m[o.path_id] = cls
                continue
        except Exception:
            pass
        raw = o.get_raw_data()
        try:
            def rdstr(b, p):
                ln = struct.unpack_from('<i', b, p)[0]
                if ln < 0 or ln > 500 or p + 4 + ln > len(b):
                    return None, p + 4
                return b[p + 4:p + 4 + ln].decode('utf-8', 'ignore'), align_up(p + 4 + ln, 4)
            n, p = rdstr(raw, 0)
            c, p = rdstr(raw, p)
            if c:
                m[o.path_id] = c
        except Exception:
            pass
    return m


def header_variants(raw):
    """返回 [(布局名, body起始偏移, 对象名, script_path_id)] 候选"""
    out = []

    def try_str(p):
        if p + 4 > len(raw):
            return None
        ln = struct.unpack_from('<i', raw, p)[0]
        if ln < 0 or ln > 200 or p + 4 + ln > len(raw):
            return None
        try:
            s = raw[p + 4:p + 4 + ln].decode('utf-8')
        except Exception:
            return None
        return s, align_up(p + 4 + ln, 4)

    # PPtr 布局：int32 fileID @base, int64 pathID @base+4
    def pptr_pathid(base):
        if base + 12 > len(raw):
            return -1
        return struct.unpack_from('<q', raw, base + 4)[0]

    # 变体 A：MonoBehaviour（挂在 GameObject 上）
    # 0x00 PPtr<GameObject>(12) | 0x0C m_Enabled(4) | 0x10 PPtr<MonoScript>(12) | 0x1C m_Name
    # -> m_Script 在 m_Name **之前**，名字读完就是字段区
    nm = try_str(0x1C)
    if nm:
        out.append(('MonoBehaviour', nm[1], nm[0], pptr_pathid(0x10)))

    # 变体 C：ScriptableObject（Unity 2019.3+ 加进来的头）
    # 0x00 int m_ObjectHideFlags | 0x04 PPtr m_CorrespondingSourceObject(12)
    # 0x10 PPtr m_PrefabInstance(12) | 0x1C m_Name | 之后 PPtr<MonoScript>(12) 才是字段区
    # ★ 和 MonoBehaviour 的 m_Name 一样在 0x1C，但 m_Script 在 m_Name **之后**
    if nm:
        after = nm[1]
        if after + 12 <= len(raw):
            out.append(('ScriptableObject', after + 12, nm[0], pptr_pathid(after)))

    # 变体 B：老版 ScriptableObject（m_Name 在 0x10）
    nm0 = try_str(0x10)
    if nm0:
        out.append(('ScriptableObjectOld', nm0[1], nm0[0], pptr_pathid(0x04)))
    return out


def scan(path, types, smap, only=None, results=None, stat=None, deadline=None):
    import time as _time
    t0 = _time.time()
    try:
        env = UnityPy.load(path)
    except Exception as e:
        print(f'  [skip] {os.path.basename(path)}: {e}', flush=True)
        return
    # 本文件的 MonoScript 优先（pathID 同文件内唯一），否则用全局表
    local_map = dict(smap)
    local_map.update(build_script_map(env))
    n_ok = n_tot = 0
    for o in env.objects:
        if o.type.name != 'MonoBehaviour':
            continue
        n_tot += 1
        # ★ 超时判断必须每个对象都查：只放在「每 2000 个打一次日志」里的话，
        #   遇到单个极慢的对象（深度嵌套数组）就永远触发不到。
        if deadline:
            el = _time.time() - t0
            if el > deadline:
                print(f'  [超时] {os.path.basename(path)} 超出 {deadline:.0f}s 预算，中断'
                      f'（已扫 {n_tot}，命中 {n_ok}）', flush=True)
                print(f'  {os.path.basename(path)}: {n_ok}/{n_tot} 个对象布局自校验通过'
                      f'（超时中断，用时 {el:.0f}s）', flush=True)
                return
        if n_tot % 2000 == 0:
            print(f'      ... 已扫 {n_tot} 个，命中 {n_ok}，'
                  f'用时 {_time.time()-t0:.0f}s', flush=True)
        try:
            raw = o.get_raw_data()
        except Exception:
            continue
        if len(raw) < 0x20:
            continue
        best = None
        for kind, body_off, nm, pid in header_variants(raw):
            cls = local_map.get(pid)
            if not cls:
                continue
            if only and cls != only and short(cls) != only:
                continue
            key = cls if cls in types else next(
                iter(sorted([k for k in types if short(k) == cls], key=len)), None)
            if key is None:
                continue
            # 三种字段集各试一遍，靠长度自校验挑对的：
            #  ser    = public + [SerializeField] 私有字段（最准）
            #  public = 只有 public
            #  all    = 全部（含普通私有，最容易多算）
            for mode in ('ser', 'all', 'public'):
                flds = fields_chain(types, key, mode)
                if not flds:
                    continue
                body = raw[body_off:]
                r = Reader(body, types)
                end = r.run(flds)
                if end < 0:
                    continue
                slack = len(body) - end
                if slack < 0 or slack > 3:
                    continue  # 不精确 -> 布局判断有误，丢弃
                cand_res = (key, kind, nm, slack, r.vals, len(body), mode)
                if best is None or cand_res[3] < best[3]:
                    best = cand_res
        if best:
            cls, kind, nm, slack, vals, blen, mode = best
            results.append({
                'class': cls, 'kind': kind, 'name': nm, 'mode': mode,
                'file': os.path.basename(path), 'bytes': blen, 'slack': slack,
                'fields': [[k, v, t] for k, v, t in vals],
            })
            n_ok += 1
            if stat is not None:
                stat[cls] += 1
    print(f'  {os.path.basename(path)}: {n_ok}/{n_tot} 个对象布局自校验通过'
          f'（用时 {_time.time()-t0:.0f}s）', flush=True)


def main():
    args = [a for a in sys.argv[1:]]
    if len(args) < 3:
        print(__doc__)
        sys.exit(1)
    dump, target, out_path = args[0], args[1], args[2]
    only = None
    if '--only' in args:
        only = args[args.index('--only') + 1]
    maxmb = 200
    if '--maxfile' in args:
        maxmb = float(args[args.index('--maxfile') + 1])
    deadline = None
    if '--deadline' in args:
        deadline = float(args[args.index('--deadline') + 1])
    topfiles = 0
    if '--topfiles' in args:
        topfiles = int(args[args.index('--topfiles') + 1])
    smap = {}
    if '--scripts' in args:
        raw = json.load(open(args[args.index('--scripts') + 1], encoding='utf-8'))
        smap = {int(k): v[0] for k, v in raw.items()}
        print(f'      载入 {len(smap)} 个 MonoScript 映射')

    print('[1/3] 解析 dump.cs ...')
    types = parse_dumpcs(dump)
    print(f'      {len(types)} 个类')

    files = []
    if os.path.isdir(target):
        for f in sorted(os.listdir(target)):
            fp = os.path.join(target, f)
            if not os.path.isfile(fp):
                continue
            if f.endswith(('.resS', '.resource', '.dat', '.json', '.txt', '.dll', '.exe')):
                continue
            if os.path.getsize(fp) > maxmb * 1048576:
                continue
            files.append((os.path.getsize(fp), fp))
        files.sort(reverse=True)
        if topfiles:
            dropped = len(files) - topfiles
            files = files[:topfiles]
            if dropped > 0:
                print(f'      （只取最大的 {topfiles} 个资产文件，'
                      f'跳过 {dropped} 个小文件）', flush=True)
    else:
        files = [(os.path.getsize(target), target)]

    print(f'[2/3] 扫描 {len(files)} 个资产文件 ...')
    results = []
    stat = Counter()
    for sz, fp in files:
        print(f'  -- {os.path.basename(fp)} ({sz/1048576:.1f}MB)')
        scan(fp, types, smap=smap, only=only, results=results, stat=stat,
             deadline=deadline)

    print('[3/3] 写出 ...')
    json.dump(results, open(out_path, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print(f'\n完成：{len(results)} 个对象 -> {out_path}')
    print('\n命中类 TOP25:')
    for c, n in stat.most_common(25):
        print(f'  {n:>5}  {c}')


if __name__ == '__main__':
    main()
