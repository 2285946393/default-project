"""管理 Steam 的「非 Steam 游戏」列表（userdata/<id>/config/shortcuts.vdf）。

用法:
    python tools/steam_shortcuts.py --list
    python tools/steam_shortcuts.py --import tools/_steam_entries.json

--import 会先备份 shortcuts.vdf，再追加没添加过的条目（按 exe 路径去重）。
写之前请先完全退出 Steam，否则会被 Steam 覆盖回去。
"""
import argparse
import json
import os
import shutil
import struct
import time

VDF = r"D:\software\steam\userdata\1572415305\config\shortcuts.vdf"


def _read(data, i):
    j = data.index(0x00, i)
    return data[i:j].decode("utf-8", "replace"), j + 1


def parse(data):
    if not data or data[0] != 0x00:
        raise ValueError("not a binary vdf")
    key, i = _read(data, 1)
    entries = []
    while data[i] != 0x08:
        if data[i] != 0x00:
            raise ValueError("bad entry marker at %d" % i)
        _idx, i = _read(data, i + 1)
        entry = {}
        while data[i] != 0x08:
            t = data[i]
            k, i = _read(data, i + 1)
            if t == 0x01:
                v, i = _read(data, i)
                entry[k] = v
            elif t == 0x02:
                entry[k] = struct.unpack_from("<i", data, i)[0]
                i += 4
            elif t == 0x00:
                sub = {}
                while data[i] != 0x08:
                    tt = data[i]
                    kk, i = _read(data, i + 1)
                    if tt == 0x01:
                        vv, i = _read(data, i)
                    elif tt == 0x02:
                        vv = struct.unpack_from("<i", data, i)[0]
                        i += 4
                    else:
                        raise ValueError("bad nested type 0x%02x" % tt)
                    sub[kk] = vv
                i += 1
                entry[k] = sub
            else:
                raise ValueError("unknown value type 0x%02x" % t)
        i += 1
        entries.append(entry)
    i += 1
    return key, entries, data[i:]


def _cstr(s):
    return s.encode("utf-8") + b"\x00"


def build(entries):
    out = bytearray(b"\x00" + b"shortcuts\x00")
    for n, entry in enumerate(entries):
        out += b"\x00" + str(n).encode("ascii") + b"\x00"
        for k, v in entry.items():
            if isinstance(v, dict):
                out += b"\x00" + _cstr(k)
                for kk, vv in v.items():
                    out += b"\x01" + _cstr(kk) + _cstr(str(vv))
                out += b"\x08"
            elif isinstance(v, bool):
                out += b"\x02" + _cstr(k) + struct.pack("<i", 1 if v else 0)
            elif isinstance(v, int):
                out += b"\x02" + _cstr(k) + struct.pack("<i", v)
            else:
                out += b"\x01" + _cstr(k) + _cstr(v)
        out += b"\x08"
    out += b"\x08\x08"
    return bytes(out)


def exe_of(entry):
    return entry.get("Exe", "").strip('"').split('" ')[0].strip()


def new_entry(name, exe, startdir=None, icon=None, args=""):
    startdir = startdir or os.path.dirname(exe)
    if startdir and not startdir.endswith("\\"):
        startdir += "\\"
    exe_field = '"%s"' % exe
    if args:
        exe_field += " " + args
    return {
        "appid": 0,
        "AppName": name,
        "Exe": exe_field,
        "StartDir": '"%s"' % startdir,
        "icon": icon or exe,
        "ShortcutPath": "",
        "LaunchOptions": "",
        "IsHidden": 0,
        "AllowDesktopConfig": 1,
        "AllowOverlay": 1,
        "OpenVR": 0,
        "Devkit": 0,
        "DevkitGameID": "",
        "DevkitOverrideAppID": 0,
        "LastPlayTime": 0,
        "FlatpakAppID": "",
        "tags": {},
    }


def load_vdf(path):
    with open(path, "rb") as f:
        return parse(f.read())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--vdf", default=VDF)
    ap.add_argument("--list", action="store_true")
    ap.add_argument("--check", action="store_true", help="parse 后原样重建，对比字节是否一致")
    ap.add_argument("--sync-desktop", action="store_true", help="按 tools/_steam_names.json 把桌面图标加进去")
    ap.add_argument("--import", dest="import_file")
    a = ap.parse_args()

    with open(a.vdf, "rb") as f:
        raw = f.read()
    _key, entries, tail = parse(raw)

    if a.check:
        rebuilt = build(entries)
        print("entries: %d" % len(entries))
        print("raw bytes: %d, rebuilt bytes: %d" % (len(raw), len(rebuilt)))
        print("identical: %s" % (rebuilt == raw))
        print("tail bytes: %r" % tail)
        return

    if a.sync_desktop:
        root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        with open(os.path.join(root, "tools", "_desktop_lnks.json"), encoding="utf-8-sig") as f:
            lnks = json.load(f)
        with open(os.path.join(root, "tools", "_steam_names.json"), encoding="utf-8") as f:
            names = json.load(f)

        have = {}
        for e in entries:
            have.setdefault(exe_of(e).lower(), e)

        added, renamed, skipped = [], [], []
        for x in lnks:
            label = x.get("lnk", "?")
            target = (x.get("target") or "").strip()
            disp = names.get(label)
            if disp is None:
                skipped.append("%s (没配名字/不需要)" % label)
                continue
            if not target:
                skipped.append("%s (找不到程序路径)" % label)
                continue
            want_rename = disp.startswith("@")
            disp = disp.lstrip("@")
            cur = have.get(target.lower())
            if cur is not None:
                if want_rename and cur.get("AppName") != disp:
                    renamed.append("%s -> %s" % (cur.get("AppName"), disp))
                    cur["AppName"] = disp
                else:
                    skipped.append("%s (已经在列表里)" % label)
                continue
            startdir = x.get("workdir") or os.path.dirname(target)
            entry = new_entry(disp, target, startdir, target, x.get("args", ""))
            entries.append(entry)
            have[target.lower()] = entry
            added.append(disp)

        bak = a.vdf + ".bak-" + time.strftime("%Y%m%d-%H%M%S")
        shutil.copy2(a.vdf, bak)
        with open(a.vdf, "wb") as f:
            f.write(build(entries))
        print("backup: %s" % bak)
        print("added %d: %s" % (len(added), " | ".join(added)))
        print("renamed %d: %s" % (len(renamed), " | ".join(renamed)))
        print("skipped %d:" % len(skipped))
        for s in skipped:
            print("   - %s" % s)
        print("total now: %d" % len(entries))
        return

    if a.import_file:
        with open(a.import_file, encoding="utf-8") as f:
            wanted = json.load(f)
        have = set(exe_of(e).lower() for e in entries)
        added = []
        for w in wanted:
            exe = w["exe"]
            if exe.lower() in have:
                continue
            entries.append(
                new_entry(w["name"], exe, w.get("startdir"), w.get("icon"), w.get("args", ""))
            )
            have.add(exe.lower())
            added.append(w["name"])
        bak = a.vdf + ".bak-" + time.strftime("%Y%m%d-%H%M%S")
        shutil.copy2(a.vdf, bak)
        with open(a.vdf, "wb") as f:
            f.write(build(entries))
        print("backup: %s" % bak)
        print("added %d: %s" % (len(added), ", ".join(added)))
        print("total now: %d" % len(entries))
        return

    for n, e in enumerate(entries):
        print("[%d] %s" % (n, e.get("AppName", "?")))
        print("    exe: %s" % exe_of(e))
    print("total: %d" % len(entries))
    if tail:
        print("(note: %d trailing bytes)" % len(tail))


if __name__ == "__main__":
    main()
