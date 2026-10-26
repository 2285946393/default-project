import re, os, sys, collections, io, json

GAMES = {
    "Blue Prince":   r"D:\BaiduNetdiskDownload\game\Blue Prince\BLUE PRINCE_Data\il2cpp_data\Metadata\global-metadata.dat",
    "BALLxPIT":      r"D:\BaiduNetdiskDownload\game\BALLxPIT\Balls_Data\il2cpp_data\Metadata\global-metadata.dat",
    "AstroProspector": r"D:\BaiduNetdiskDownload\game\咖啡星球工匠\Astro Prospector_Data\il2cpp_data\Metadata\global-metadata.dat",
    "RACCOIN":       r"D:\BaiduNetdiskDownload\game\推币机\RACCOIN_Data\il2cpp_data\Metadata\global-metadata.dat",
    "EverythingIsCrab": r"D:\BaiduNetdiskDownload\game\寄居蟹\Everything is Crab_Data\il2cpp_data\Metadata\global-metadata.dat",
    "NeonAbyss2":    r"D:\BaiduNetdiskDownload\game\霓虹深渊2\NeonAbyss2_Data\il2cpp_data\Metadata\global-metadata.dat",
    "ToM":           r"D:\BaiduNetdiskDownload\game\金属之泪\ToM_Data\il2cpp_data\Metadata\global-metadata.dat",
}

# 第三方库噪声前缀
NOISE = ('UnityEngine', 'Unity.', 'System', 'Mono.', 'Newtonsoft', 'I2.', 'TMPro', 'Steam',
         'FMOD', 'Rewired', 'Cinemachine', 'Epic', 'PlayFab', 'AOT', 'mscorlib', 'netstandard',
         'LitJson', 'Naninovel', 'Ink', 'DG.Tweening', 'Cysharp', 'UniTask', 'Microsoft',
         'RenderPipeline', 'UnityEditor', 'Unity.Collections', 'Unity.Burst', 'Unity.Mathematics',
         'Unity.Jobs', 'Unity.IL2CPP', 'Sirenix', 'Odin', 'NodeCanvas', 'ParadoxNotion',
         'Pathfinding', 'Amplify', 'AwesomeTechnologies', 'Volumetric', 'Digital', 'Houdini',
         'Photonic', 'Smart', 'Tayx', 'Tobii', 'Wizards', 'ZString', 'MessagePack', 'protobuf')

KW = ['Upgrade', 'Cost', 'Price', 'Damage', 'Health', 'Reward', 'Currency', 'Stat', 'Config',
      'Balance', 'Mult', 'Rate', 'Value', 'Amount', 'Spawn', 'Wave', 'Difficulty', 'Progress',
      'Score', 'Speed', 'Duration', 'Chance', 'Rarity', 'Tier', 'Level', 'Economy', 'Income',
      'Resource', 'Modifier', 'Inventory', 'Deck', 'Card', 'Room', 'Draft', 'Reroll', 'Prestige']


def namespaces(strs):
    ns = collections.Counter()
    for s in strs:
        m = re.match(r'^([A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*){1,3})\|', s)
        if not m:
            continue
        n = m.group(1)
        if n.startswith(NOISE):
            continue
        ns[n] += 1
    return ns


def scan(name, path):
    if not os.path.exists(path):
        return {'name': name, 'error': 'metadata 不存在'}
    d = open(path, 'rb').read()
    strs = [m.group().decode('ascii') for m in re.finditer(rb'[ -~]{4,}', d)]
    S = set(strs)
    ns = namespaces(S)
    hits = {k: sum(1 for s in S if k in s and len(s) < 70) for k in KW}
    # 资源路径 / ScriptableObject 路径
    paths = sorted(s for s in S if ('/Data' in s or re.search(r'/[A-Z][a-z].*/', s)) and len(s) < 60)[:40]
    return {'name': name, 'size': len(d), 'unique': len(S), 'ns': ns.most_common(25),
            'hits': hits, 'paths': paths}


out = []
for n, p in GAMES.items():
    try:
        r = scan(n, p)
        out.append(r)
        print(f"[ok] {n}: {r.get('unique',0)} 串")
    except Exception as e:
        out.append({'name': n, 'error': str(e)})
        print(f"[err] {n}: {e}")

json.dump(out, open(r'C:\Temp\il2cpp_scan.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print('done')
