# -*- coding: utf-8 -*-
"""干跑测试：拿真实 config.toml 的副本，验证切换逻辑不破坏配置。"""
import os, sys, shutil, tempfile, importlib.machinery, tomllib
sys.stdout.reconfigure(encoding='utf-8')

SRC = 'D:/work/AI产出/Codex人格切换/切换模式.pyw'
PERSONAS = 'D:/work/AI产出/Codex人格切换/personas'
REAL = os.path.expanduser('~/.codex/config.toml')

m = importlib.machinery.SourceFileLoader('sw', SRC).load_module()

tmp = tempfile.mkdtemp()
cfg = os.path.join(tmp, 'config.toml')
shutil.copy2(REAL, cfg)
m.CONFIG = cfg
m.PERSONA_DIR = PERSONAS
m.BACKUP_DIR = os.path.join(tmp, 'bak')

before = tomllib.loads(open(cfg, encoding='utf-8').read())
before_keys = sorted(before.keys())
print('原始顶层键数:', len(before_keys))

for mode in ['妈妈', '女仆', '妈妈', None, '女仆', None]:
    ok, msg = m.apply_mode(mode)
    txt = open(cfg, encoding='utf-8').read()
    try:
        d = tomllib.loads(txt); parsed = True
    except Exception as e:
        parsed = False; d = {}
        print('  TOML 解析失败!', e)
    dev = d.get('developer_instructions', '')
    now_keys = sorted(d.keys())
    print(f'切到 {str(mode):6} ok={ok} TOML={parsed} dev={len(dev):5}字 顶层键={len(now_keys)} {msg[:30]}')
    if parsed:
        lost = [k for k in before_keys if k not in now_keys]
        if lost: print('  !! 丢键:', lost)

after = tomllib.loads(open(cfg, encoding='utf-8').read())
diff = [k for k in set(before) | set(after) if before.get(k) != after.get(k)]
print()
print('回到正常后，与原始配置有差异的键:', diff if diff else '无 ✅ 完全一致')
print('模型:', after.get('model'), '| provider:', after.get('model_provider'))
print('插件:', len(after.get('plugins', {})), '条 | provider:', len(after.get('model_providers', {})), '条')
print('备份文件数:', len(os.listdir(m.BACKUP_DIR)), '(6 次调用，2 次无改动不备份 -> 期望 4)')
shutil.rmtree(tmp, ignore_errors=True)
