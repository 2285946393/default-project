# -*- coding: utf-8 -*-
"""v2 切换器回归测试：拿 AGENTS.md 的**副本**跑，不碰真文件。"""
import os
import sys
import shutil
import tempfile
import importlib.machinery

sys.stdout.reconfigure(encoding="utf-8")

SRC = os.path.expanduser("~/.codex/AGENTS.md")
TMP = tempfile.mkdtemp(prefix="agents_test_")
FAKE_HOME = os.path.join(TMP, ".codex")
os.makedirs(FAKE_HOME)
FAKE_AGENTS = os.path.join(FAKE_HOME, "AGENTS.md")
shutil.copy2(SRC, FAKE_AGENTS)

DEPLOY = r"D:\work\AI产出\Codex人格切换"
backup_dir = os.path.join(TMP, "备份")
shutil.copytree(os.path.join(DEPLOY, "personas"), os.path.join(TMP, "personas"))

# 加载模块（它用 __file__ 定位 personas/备份，所以先搬到 TMP）
mod_path = os.path.join(TMP, "sw2.py")
shutil.copy2(os.path.join(os.path.dirname(os.path.abspath(__file__)), "切换模式.pyw"), mod_path)
m = importlib.machinery.SourceFileLoader("sw2", mod_path).load_module()
# 把落点指到假 home
m.AGENTS = FAKE_AGENTS
m.CONFIG = os.path.join(FAKE_HOME, "config.toml")   # 不存在 → 清理逻辑直接跳过

orig = open(FAKE_AGENTS, encoding="utf-8").read()
print(f"原始 AGENTS.md: {len(orig)} 字符")
print()

fails = []


def check(label, cond, extra=""):
    print(f"  {'✅' if cond else '❌'} {label}{' ' + extra if extra else ''}")
    if not cond:
        fails.append(label)


for key in ["妈妈", "女仆", "妈妈", None, "女仆", None]:
    label = key or "正常"
    ok, msg, note = m.apply_mode(key)
    text = open(FAKE_AGENTS, encoding="utf-8").read()
    got = m.current_mode()
    body = "" if key is None else m.load_persona(key)

    print(f"--- 切到 {label} ---")
    check("写入成功", ok, msg)
    check("模式识别正确", got[0] == key, f"识别为 {got[0]}({got[1]}字)")
    if key:
        check("人设正文在文件里", body in text)
        check("有标记块", m.BEGIN in text and m.END in text)
    else:
        check("标记块已清掉", m.BEGIN not in text)
    check("原有内容没丢（含『全局指令』）", "全局指令" in text)
    check(
        "原有内容逐字保留",
        orig.strip() in text or text.strip() == orig.strip(),
        f"当前 {len(text)} 字符",
    )
    print()

# 最终状态 = 正常，应与原始完全一致
final = open(FAKE_AGENTS, encoding="utf-8").read()
print("=== 最终一致性 ===")
check("回到正常后与原始文件逐字节相同", final == orig,
      f"{len(final)} vs {len(orig)}")

baks = os.listdir(backup_dir)
print(f"\n备份文件 {len(baks)} 份：{sorted(baks)}")

print("\n" + ("=" * 40))
print("全部通过 ✅" if not fails else f"失败 {len(fails)} 项：{fails}")
shutil.rmtree(TMP, ignore_errors=True)
sys.exit(1 if fails else 0)
