#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
DouSpire 工坊上传助手（Windows）

用法：
    python tools/upload-workshop.py               # 体检 + 上传
    python tools/upload-workshop.py --check-only  # 只体检，不上传

⚠️ 最重要的一条（2026-10-10 实测得出）
------------------------------------------------------------------
Steamworks 的 IPC 要求 **上传器和 Steam 客户端处在同一权限级别**。
从 **提权的（管理员）终端** 跑 ModUploader.exe 一定失败：

    卡在 "Initializing Steam" 不动
    或报 k_ESteamAPIInitResult_FailedGeneric / ConnectToGlobalUser failed

而 Steam 自己是普通权限启动的。**所以这个脚本要在普通（非管理员）终端里跑**，
或者直接在资源管理器里双击 ModUploader.exe 相关脚本。

脚本启动时会自动检测自己是不是被提权了，是就直接退出并提示，不做无谓重试。
它也**不会**去重启 Steam —— 实测重启 Steam 解决不了这个问题，只会白折腾。
------------------------------------------------------------------

它会做的事：
    1. 检查自己是不是提权运行（是 → 退出）
    2. 读注册表确认 Steam 装在哪、当前登录的是不是「有花无实」
    3. 看 connection_log.txt 最近有没有 [Logged On]
    4. 跑 ModUploader.exe upload -w <包目录>（超时 240 秒）
    5. 输出里出现 "Successfully uploaded" → 成功；否则原样打印上传器输出
"""

import ctypes
import json
import os
import re
import subprocess
import sys
import time
import winreg

UPLOADER_DIR = r"D:\software\sts2-mod-uploader"
PACKAGE_DIR = os.path.join(UPLOADER_DIR, "DouSpire")
OWNER_ACCOUNT_ID = "1572415305"      # 有花无实（SteamID64 76561199532681033）
UPLOAD_TIMEOUT = 240


def log(msg):
    print(f"[{time.strftime('%H:%M:%S')}] {msg}", flush=True)


def is_elevated():
    """当前进程是否提权（High integrity）。"""
    try:
        return bool(ctypes.windll.shell32.IsUserAnAdmin())
    except Exception:
        return False


def steam_home():
    with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r"Software\Valve\Steam") as k:
        return winreg.QueryValueEx(k, "SteamPath")[0]


def active_user():
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER,
                            r"Software\Valve\Steam\ActiveProcess") as k:
            return str(winreg.QueryValueEx(k, "ActiveUser")[0])
    except OSError:
        return None


def steam_running():
    r = subprocess.run(["tasklist", "/FI", "IMAGENAME eq steam.exe"],
                       capture_output=True, text=True, encoding="gbk", errors="replace")
    return "steam.exe" in r.stdout


def last_logon_age(home):
    """返回 Steam 最近一次 [Logged On] 距现在多少秒；找不到返回 None。"""
    p = os.path.join(home, "logs", "connection_log.txt")
    if not os.path.exists(p):
        return None
    txt = open(p, "rb").read().decode("utf-8", errors="replace")[-200000:]
    for line in reversed(txt.splitlines()):
        if "Logged On" in line:
            m = re.match(r"\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\]", line)
            if m:
                return time.time() - time.mktime(
                    time.strptime(m.group(1), "%Y-%m-%d %H:%M:%S"))
    return None


def package_version():
    try:
        j = json.load(open(os.path.join(PACKAGE_DIR, "content", "DouSpire.json"),
                           encoding="utf-8"))
        return j.get("version", "?")
    except Exception as e:
        return f"?（读不到：{e}）"


def do_upload():
    out_path = os.path.join(UPLOADER_DIR, "upload-latest.out")
    try:
        with open(out_path, "wb") as f:
            p = subprocess.Popen(
                [os.path.join(UPLOADER_DIR, "ModUploader.exe"),
                 "upload", "-w", PACKAGE_DIR],
                cwd=UPLOADER_DIR, stdout=f, stderr=subprocess.STDOUT)
        try:
            p.wait(timeout=UPLOAD_TIMEOUT)
        except subprocess.TimeoutExpired:
            p.kill()
            subprocess.run(["taskkill", "/F", "/IM", "ModUploader.exe"],
                           capture_output=True)
    except OSError as e:
        return False, f"启动失败: {e}"
    return ("Successfully uploaded" in
            open(out_path, "rb").read().decode("utf-8", errors="replace")), \
        open(out_path, "rb").read().decode("utf-8", errors="replace")


def main():
    check_only = "--check-only" in sys.argv[1:]

    log(f"待上传版本：{package_version()}")
    log(f"包目录：{PACKAGE_DIR}")

    if is_elevated():
        log("❌ 当前终端是【管理员/提权】状态。")
        log("   Steamworks 的 IPC 和提权进程不通（实测：卡在 Initializing Steam，")
        log("   或报 ConnectToGlobalUser failed）。Steam 自己是普通权限。")
        log("   → 请在【普通】终端里重跑本脚本，或直接用资源管理器双击运行。")
        return 2

    home = steam_home()
    log(f"Steam 目录：{home}")
    log(f"Steam 进程：{'在跑' if steam_running() else '没跑'}")
    user = active_user()
    ok_user = (user == OWNER_ACCOUNT_ID)
    log(f"当前登录账号：{user}（{'✔ 有花无实' if ok_user else '⚠ 不是本人账号'}）")
    age = last_logon_age(home)
    log(f"最近一次 [Logged On]：{'未知' if age is None else f'{int(age)} 秒前'}")

    if check_only:
        log("体检模式，不上传。")
        return 0

    ok, txt = do_upload()
    print("---- 上传器输出 ----")
    print("\n".join(txt.strip().splitlines()[-15:]))
    print("--------------------")
    if ok:
        log("✅ 上传成功")
        return 0
    if "Caching Steam ID" not in txt:
        log("❌ 连 Steam 握手都没完成。先确认：普通终端 + Steam 已登录 + 游戏没在跑。")
    else:
        log("❌ 握手成功但上传失败，看上面的输出找原因。")
    return 1


if __name__ == "__main__":
    sys.exit(main())
