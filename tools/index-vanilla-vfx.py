"""给 sts2-vanilla-vfx/ 里抽出来的场景建一份可检索目录（markdown）。

用法:
    python index-vanilla-vfx.py <sts2-vanilla-vfx 目录> <输出 md>

解析每个 .tscn，统计：根节点类型 / 节点构成 / 纹理数 / 用到的 shader / 是否一次性粒子。
场景 → 材质(.tres) → shader(.gdshader) 这条链会一并解开（材质是中间层，别漏）。
目标是"想找什么效果，先在这张表里搜"。
"""
import io
import os
import re
import sys
from collections import Counter, defaultdict

NODE_RE = re.compile(r'^\[node\s+name="([^"]*)"\s+type="([^"]*)"', re.M)
EXT_RE = re.compile(r'\[ext_resource\s+([^\]]*)\]')
SHADER_REF_RE = re.compile(r'^shader\s*=\s*ExtResource\("([^"]*)"\)', re.M)
ONESHOT_RE = re.compile(r'^one_shot\s*=\s*true', re.M)
AMOUNT_RE = re.compile(r'^amount\s*=\s*(\d+)', re.M)


def parse_ext(txt):
    """→ {id: (type, path)}

    ⚠️ 锚点必须写 `(?:^|\\s)`：不然 `id="([^"]*)"` 会先匹配到 `uid="uid://..."` 里的
    `id="uid://..."`，把 id 全认成 uid（踩过一次，害得材质→shader 的链只解开 17/153）。
    """
    out = {}
    for m in EXT_RE.finditer(txt):
        attrs = m.group(1)
        pid = re.search(r'(?:^|\s)id="([^"]*)"', attrs)
        ppath = re.search(r'(?:^|\s)path="(res://[^"]*)"', attrs)
        ptype = re.search(r'(?:^|\s)type="([^"]*)"', attrs)
        if pid and ppath:
            out[pid.group(1)] = (ptype.group(1) if ptype else '?', ppath.group(1))
    return out


def build_material_map(root):
    """res://materials/xxx.tres → shader 文件名（解开中间层）"""
    mmap = {}
    for dirpath, _, files in os.walk(os.path.join(root, 'materials')):
        for fn in files:
            if not fn.endswith('.tres'):
                continue
            p = os.path.join(dirpath, fn)
            rel = os.path.relpath(p, root).replace('\\', '/')
            txt = io.open(p, encoding='utf-8', errors='replace').read()
            exts = parse_ext(txt)
            shaders = []
            for ref in SHADER_REF_RE.findall(txt):
                if ref in exts:
                    bn = os.path.basename(exts[ref][1])
                    # 少数材质不直接挂 shader，而是再套一层材质（如 vfx_molten_fist.tres）
                    shaders.append(bn if bn.endswith('.gdshader') else '(再套一层材质) ' + bn)
            if shaders:
                mmap['res://' + rel] = shaders
    return mmap


def analyze(path, mmap):
    txt = io.open(path, encoding='utf-8', errors='replace').read()
    nodes = NODE_RE.findall(txt)
    exts = parse_ext(txt)

    kinds = Counter(t for _, t in nodes)
    etypes = Counter(t for t, _ in exts.values())

    shaders, materials, sub_scenes = set(), [], []
    for t, p in exts.values():
        if t == 'Material':
            materials.append(os.path.basename(p))
            for s in mmap.get(p, []):
                shaders.add(s)
        elif t == 'PackedScene':
            sub_scenes.append(os.path.basename(p))

    # 场景内联的 ShaderMaterial
    for ref in SHADER_REF_RE.findall(txt):
        if ref in exts and exts[ref][0] == 'Shader':
            shaders.add(os.path.basename(exts[ref][1]))

    return dict(kinds=kinds, etypes=etypes, root=nodes[0][1] if nodes else '?',
                shaders=sorted(shaders), materials=sorted(set(materials)),
                sub_scenes=sorted(set(sub_scenes)),
                one_shot=bool(ONESHOT_RE.search(txt)),
                amount=AMOUNT_RE.findall(txt), node_count=len(nodes),
                script_driven=bool(etypes.get('Script')) and len(nodes) <= 1,
                size=len(txt))


def brief(info):
    k, e = info['kinds'], info['etypes']
    parts = []
    for name, label in (('GPUParticles2D', '粒子'), ('AnimatedSprite2D', '帧动画'),
                        ('Sprite2D', '精灵'), ('SpineSprite', 'Spine'),
                        ('Node2D', 'Node2D'), ('Control', 'Control'),
                        ('CPUParticles2D', 'CPU粒子'), ('AnimationPlayer', '动画播放器'),
                        ('PointLight2D', '点光'), ('CanvasGroup', '组'), ('Line2D', 'Line2D')):
        if k.get(name):
            parts.append('%s×%d' % (label, k[name]))
    if e.get('Texture2D'):
        parts.append('纹理×%d' % e['Texture2D'])
    if info['one_shot']:
        parts.append('一次性')
    if info['script_driven']:
        parts.append('**脚本驱动**')
    if info['shaders']:
        parts.append('shader:`%s`' % '`,`'.join(info['shaders']))
    if info['sub_scenes']:
        parts.append('引用子场景')
    return ' · '.join(parts) if parts else '（无子节点）'


def main():
    root, outmd = sys.argv[1], sys.argv[2]
    mmap = build_material_map(root)
    scenes = []
    for dirpath, _, files in os.walk(os.path.join(root, 'scenes', 'vfx')):
        for fn in sorted(files):
            if fn.endswith('.tscn'):
                p = os.path.join(dirpath, fn)
                rel = os.path.relpath(p, root).replace('\\', '/')
                scenes.append((rel, analyze(p, mmap)))
    scenes.sort()

    groups = defaultdict(list)
    for rel, info in scenes:
        d = os.path.dirname(rel)[len('scenes/vfx'):].strip('/') or '（根 · 主力/通用特效）'
        groups[d].append((rel, info))

    L = []
    w = L.append
    w('# 杀戮尖塔 2 · 本体特效场景目录（可检索）')
    w('')
    w('> 自动生成：`tools/index-vanilla-vfx.py`。源文件由 `tools/extract-pck-files.py` 从本机')
    w('> `SlayTheSpire2.pck` 抽出，落在 `projects/sts2-vanilla-vfx/`。')
    w('> 游戏升级后**重新抽 + 重新生成**，别当永久事实。')
    w('')
    w('共 **%d 个特效场景**、%d 个着色器、%d 个材质。' % (
        len(scenes),
        sum(1 for _, _, fs in os.walk(os.path.join(root, 'shaders')) for f in fs if f.endswith('.gdshader')),
        sum(1 for d, _, fs in os.walk(os.path.join(root, 'materials')) for f in fs if f.endswith('.tres'))))
    w('')
    w('## 怎么用这张表')
    w('')
    w('- 想做什么效果 → 搜关键词（`slash` / `fire` / `smoke` / `distortion` / `ring` / `spine` …）')
    w('- 找到中意的 → 打开对应 `.tscn`，抄节点树 / 参数；**运行时也可以直接引用本体路径**（纹理不用抽，pck 里有）')
    w('- 三层链：**场景 .tscn → 材质 .tres → 着色器 .gdshader**，看效果时三层都要翻')
    w('')
    w('## 一览（按目录分组）')
    w('')

    for d in sorted(groups, key=lambda x: (x != '（根 · 主力/通用特效）', x)):
        w('### %s' % d)
        w('')
        w('| 场景 | 构成 |')
        w('|---|---|')
        for rel, info in sorted(groups[d]):
            w('| `%s` | %s |' % (os.path.basename(rel), brief(info)))
        w('')

    w('## 附录 A · 挂了着色的特效场景（场景 → 材质 → shader）')
    w('')
    for rel, info in scenes:
        if info['shaders']:
            mats = '、'.join('`%s`' % m for m in info['materials']) or '（内联）'
            w('- `%s` → 材质 %s → shader %s' % (
                os.path.basename(rel), mats, '、'.join('`%s`' % s for s in info['shaders'])))
    w('')
    w('## 附录 B · 一次性（`one_shot`）粒子场景')
    w('')
    for rel, info in scenes:
        if info['one_shot']:
            w('- `%s`（粒子数 ×%s）' % (os.path.basename(rel), '、'.join(info['amount']) or '?'))
    w('')
    w('## 附录 C · 脚本驱动场景（子节点少，逻辑在 C# 里）')
    w('')
    w('这类场景本体文件很空，行为写在同名的 `N*.cs` 里。')
    w('反编译源码在 `projects/sts2-moddev-workspace/02-游戏反编译资料/版本-0.111.0/反编译源码/MegaCrit.Sts2.Core.Nodes.Vfx/`。')
    w('')
    for rel, info in scenes:
        if info['script_driven']:
            w('- `%s`' % os.path.basename(rel))
    w('')

    io.open(outmd, 'w', encoding='utf-8').write('\n'.join(L))
    print('写入 %s（%d 场景 / %d 材质带 shader）' % (outmd, len(scenes), len(mmap)))


if __name__ == '__main__':
    main()
