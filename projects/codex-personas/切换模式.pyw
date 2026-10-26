# -*- coding: utf-8 -*-
"""
Codex 人格切换器 v2 —— 妈妈模式 / 鲸鱼娘女仆 / 正常

⚠️ v1 的教训（2026-10-13）：
   v1 把人设写进 ~/.codex/config.toml 的 `developer_instructions`。
   **那个键没用** —— Codex 每次发请求时，会用它自己的「协作模式」文本覆盖这个字段
   （实测会话里该字段的值是 "# Collaboration Mode: Default ..."），
   我们的人设一个字都没到模型面前。
   ✅ 真正有效的是 `~/.codex/AGENTS.md`：实测该文件内容会整份进上下文
   （会话里 AGENTS.md 的原文命中 17 处，是硬凭证）。
   所以 v2 改成往 AGENTS.md 里插一段带标记的人设块。

用法：双击桌面「Codex 人格切换」图标。切完请自己把 Codex 关掉再打开。
"""
import os
import re
import shutil
import datetime
import subprocess
import tkinter as tk
from tkinter import font as tkfont

HOME = os.path.expanduser("~")
CODEX_HOME = os.path.join(HOME, ".codex")
AGENTS = os.path.join(CODEX_HOME, "AGENTS.md")       # ← 真正生效的落点
CONFIG = os.path.join(CODEX_HOME, "config.toml")     # ← 只用来清理 v1 的残留
BASE = os.path.dirname(os.path.abspath(__file__))
PERSONA_DIR = os.path.join(BASE, "personas")
BACKUP_DIR = os.path.join(BASE, "备份")
LOG = os.path.join(BACKUP_DIR, "切换日志.txt")

BEGIN = "<!-- ===== CODEX-PERSONA-BEGIN（本块由「Codex 人格切换」管理，手改会被覆盖）===== -->"
END = "<!-- ===== CODEX-PERSONA-END ===== -->"
BLOCK_RE = re.compile(re.escape(BEGIN) + r".*?" + re.escape(END) + r"[ \t]*\r?\n?", re.S)

# v1 留在 config.toml 里的没用字段，顺手清掉
DEV_RE = re.compile(r'^developer_instructions[ \t]*=[ \t]*"""(?:.|\n)*?"""[ \t]*\r?\n?', re.M)

MODES = [
    ("妈妈", "妈妈模式", "溺爱型妈妈 · 软到发腻，一碰「失去你」就安静下来"),
    ("女仆", "女仆模式", "鲸鱼娘大肥鱼 · 软糯认真，干活时小声嘀咕"),
    (None, "恢复正常", "关掉人设，Codex 变回平时的样子"),
]

CREATE_NO_WINDOW = 0x08000000
BG = "#FFF6FA"
INK = "#4A235F"
SUB = "#8A6E9B"
COLORS = {"妈妈": "#FFD9E8", "女仆": "#D9ECFF", None: "#EDEDF2"}
BORDER = {"妈妈": "#F5A9C8", "女仆": "#8EC5F5", None: "#C6C6D2"}
TITLES = {"妈妈": "妈妈模式", "女仆": "女仆模式", None: "正常 Codex"}


def log(msg):
    try:
        os.makedirs(BACKUP_DIR, exist_ok=True)
        stamp = datetime.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        with open(LOG, "a", encoding="utf-8") as f:
            f.write(f"[{stamp}] {msg}\n")
    except Exception:
        pass


def load_persona(key):
    with open(os.path.join(PERSONA_DIR, key + ".md"), encoding="utf-8") as f:
        return f.read().strip()


def read_agents():
    with open(AGENTS, encoding="utf-8") as f:
        return f.read()


def current_mode():
    """返回 (key, 字数)。没人设时 key 为 None；认不出来返回 'unknown'。"""
    try:
        text = read_agents()
    except Exception:
        return "unknown", 0
    m = BLOCK_RE.search(text)
    if not m:
        return None, 0
    block = m.group(0)
    for key, _label, _desc in MODES:
        if key is None:
            continue
        try:
            body = load_persona(key)
        except Exception:
            continue
        if body and body in block:
            return key, len(body)
    return "unknown", 0


def build_block(body):
    return BEGIN + "\n" + body.strip("\n") + "\n" + END + "\n"


def clean_config_toml():
    """把 v1 塞进 config.toml 的 developer_instructions 清掉（那玩意被 Codex 自己占用，无效）。"""
    if not os.path.isfile(CONFIG):
        return False
    try:
        old = open(CONFIG, encoding="utf-8").read()
        if "developer_instructions" not in old:
            return False
        new = DEV_RE.sub("", old)
        if new == old:
            return False
        os.makedirs(BACKUP_DIR, exist_ok=True)
        stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
        shutil.copy2(CONFIG, os.path.join(BACKUP_DIR, f"config.toml.bak-清理v1残留-{stamp}"))
        import tomllib
        tomllib.loads(new)          # 解析不过就不写
        with open(CONFIG, "w", encoding="utf-8", newline="\n") as f:
            f.write(new)
        log("已清掉 config.toml 里 v1 留下的 developer_instructions（那个键被 Codex 自己占用，无效）")
        return True
    except Exception as e:
        log(f"清理 config.toml 残留失败：{e}")
        return False


def apply_mode(key):
    """把人设写进 AGENTS.md。返回 (ok, 消息, 附注)。"""
    if not os.path.isfile(AGENTS):
        return False, f"找不到 Codex 全局指令文件：\n{AGENTS}", ""

    old = read_agents()

    new = BLOCK_RE.sub("", old) if BLOCK_RE.search(old) else old
    if key is not None:
        new = build_block(load_persona(key)) + "\n" + new.lstrip("\n")
    else:
        new = new.lstrip("\n")

    if new == old:
        note = "已顺手清理 config.toml 残留。" if clean_config_toml() else ""
        return True, "已经就是这个模式了，没改动。", note

    os.makedirs(BACKUP_DIR, exist_ok=True)
    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    bak = os.path.join(BACKUP_DIR, f"AGENTS.md.bak-{stamp}")
    n = 1
    while os.path.exists(bak):
        n += 1
        bak = os.path.join(BACKUP_DIR, f"AGENTS.md.bak-{stamp}-{n}")
    shutil.copy2(AGENTS, bak)

    try:
        with open(AGENTS, "w", encoding="utf-8", newline="\n") as f:
            f.write(new)
    except Exception as e:
        return False, f"写不进去：{e}", ""

    # 回读确认：人设真在里面 + 原来的内容没丢
    try:
        back = read_agents()
        want = "" if key is None else load_persona(key)
        if want and want.strip() not in back:
            shutil.copy2(bak, AGENTS)
            return False, "写进去了但回读对不上，已经自动还原。", ""
        if "全局指令" not in back:
            shutil.copy2(bak, AGENTS)
            return False, "原来的内容好像丢了，已经自动还原。", ""
    except Exception as e:
        shutil.copy2(bak, AGENTS)
        return False, f"回读校验失败：{e}", ""

    log(f"切到 {key or '正常'}；备份 {os.path.basename(bak)}；AGENTS.md 现在 {len(new)} 字符")
    note = "已顺手清理 config.toml 残留。" if clean_config_toml() else ""
    return True, "", note


def codex_running():
    try:
        out = subprocess.run(
            ["tasklist", "/FI", "IMAGENAME eq codex.exe", "/NH"],
            capture_output=True, text=True, timeout=10,
            creationflags=CREATE_NO_WINDOW,
        ).stdout
        return "codex.exe" in out.lower()
    except Exception:
        return False


class App:
    def __init__(self, root):
        self.root = root
        root.title("Codex 人格切换")
        root.configure(bg=BG)
        root.resizable(False, False)

        ui = "Microsoft YaHei UI"
        f_title = tkfont.Font(family=ui, size=15, weight="bold")
        f_mode = tkfont.Font(family=ui, size=13, weight="bold")
        f_btn = tkfont.Font(family=ui, size=12, weight="bold")
        f_sub = tkfont.Font(family=ui, size=9)
        f_note = tkfont.Font(family=ui, size=9)

        pad = tk.Frame(root, bg=BG)
        pad.pack(fill="both", expand=True, padx=18, pady=14)

        tk.Label(pad, text="Codex 人格切换", font=f_title, bg=BG, fg=INK).pack(anchor="w")
        tk.Label(pad, text="写进 Codex 的全局指令文件（这个才是真的会送到它面前的）",
                 font=f_sub, bg=BG, fg=SUB).pack(anchor="w", pady=(2, 10))

        box = tk.Frame(pad, bg="#FFFFFF", highlightbackground="#F0DCE8", highlightthickness=1)
        box.pack(fill="x", pady=(0, 12))
        self.lbl_now = tk.Label(box, text="", font=f_mode, bg="#FFFFFF", fg=INK)
        self.lbl_now.pack(anchor="w", padx=12, pady=(9, 1))
        self.lbl_now_sub = tk.Label(box, text="", font=f_sub, bg="#FFFFFF", fg=SUB)
        self.lbl_now_sub.pack(anchor="w", padx=12, pady=(0, 9))

        self.buttons = {}
        for key, title, desc in MODES:
            row = tk.Frame(pad, bg=BG)
            row.pack(fill="x", pady=3)
            b = tk.Button(
                row, text=title, font=f_btn, bg=COLORS[key], fg=INK,
                activebackground=COLORS[key], activeforeground=INK,
                relief="flat", bd=0, cursor="hand2", width=12,
                highlightbackground=BORDER[key], highlightthickness=2,
                command=lambda k=key: self.do_switch(k),
            )
            b.pack(side="left", ipady=6)
            tk.Label(row, text=desc, font=f_sub, bg=BG, fg=SUB,
                     justify="left", wraplength=250).pack(side="left", padx=(10, 0))
            self.buttons[key] = b

        self.lbl_msg = tk.Label(pad, text="", font=f_note, bg=BG, fg="#B23A6B",
                                justify="left", wraplength=400)
        self.lbl_msg.pack(anchor="w", pady=(10, 0))

        self.lbl_hint = tk.Label(pad, text="", font=f_note, bg=BG, fg=SUB,
                                 justify="left", wraplength=400)
        self.lbl_hint.pack(anchor="w", pady=(6, 0))

        self.refresh()
        root.update_idletasks()
        w, h = root.winfo_width(), root.winfo_height()
        sw, sh = root.winfo_screenwidth(), root.winfo_screenheight()
        root.geometry(f"+{(sw - w) // 2}+{(sh - h) // 3}")

    def refresh(self):
        key, size = current_mode()
        if key == "unknown":
            self.lbl_now.config(text="当前：认不出来的人设")
            self.lbl_now_sub.config(text="AGENTS.md 里的人设块不是我认识的模式")
        elif key is None:
            self.lbl_now.config(text="当前：正常 Codex")
            self.lbl_now_sub.config(text="AGENTS.md 里没有人设块")
        else:
            self.lbl_now.config(text=f"当前：{TITLES[key]}")
            self.lbl_now_sub.config(text=f"人设 {size} 字，写在 AGENTS.md 开头")

        for k, b in self.buttons.items():
            active = (k == key)
            b.config(relief="sunken" if active else "flat",
                     highlightthickness=3 if active else 2)

        if codex_running():
            self.lbl_hint.config(
                text="⚠ Codex 现在开着。切换要生效，请自己把 Codex 关掉再打开\n"
                     "（妈妈不替你关窗口，怕碰到你正在做的事）",
                fg="#B23A6B")
        else:
            self.lbl_hint.config(text="Codex 没在跑，切完直接打开就是新的。", fg=SUB)

    def do_switch(self, key):
        ok, msg, note = apply_mode(key)
        if ok:
            self.lbl_msg.config(text=f"已切到「{TITLES[key]}」　{note}", fg="#1F7A4D")
            log(f"switch ok -> {key}")
        else:
            self.lbl_msg.config(text="失败：" + msg, fg="#B23A6B")
            log(f"switch FAILED -> {key}: {msg}")
        self.refresh()


def main():
    try:
        import ctypes
        ctypes.windll.shcore.SetProcessDpiAwareness(1)
    except Exception:
        pass
    root = tk.Tk()
    App(root)
    root.mainloop()


if __name__ == "__main__":
    main()
