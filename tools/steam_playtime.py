"""统计 Steam 上的游戏时长。

数据源：
    userdata/<id>/config/localconfig.vdf   —— 每个 appid 的 Playtime(分钟) / LastPlayed
    steamapps/appmanifest_*.acf            —— 已安装游戏的名字
    appcache/appinfo.vdf                   —— 全部 app 的名字（二进制，用来补没装的游戏）

用法:
    python tools/steam_playtime.py            # 打印排行
    python tools/steam_playtime.py --json out.json
"""
import argparse
import io
import json
import os
import struct
import time

STEAM = r"D:\software\steam"
USER_ID = "1572415305"
FIX_JSON = os.path.join(os.path.dirname(os.path.abspath(__file__)), "_steam_name_fix.json")


# ---------- Valve KeyValues (文本) ----------

_ESC = {"n": "\n", "t": "\t", "\\": "\\", '"': '"'}


def _tokens(text):
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if c in " \t\r\n":
            i += 1
        elif c == '"':
            j = i + 1
            buf = []
            while j < n:
                if text[j] == "\\" and j + 1 < n:
                    buf.append(_ESC.get(text[j + 1], text[j + 1]))
                    j += 2
                elif text[j] == '"':
                    break
                else:
                    buf.append(text[j])
                    j += 1
            yield ("v", "".join(buf))
            i = j + 1
        elif c == "{":
            yield ("{", None)
            i += 1
        elif c == "}":
            yield ("}", None)
            i += 1
        elif c == "/" and i + 1 < n and text[i + 1] == "/":
            j = text.find("\n", i)
            i = n if j < 0 else j + 1
        else:
            i += 1


def parse_kv(text):
    toks = list(_tokens(text))

    def block(pos):
        d = {}
        while pos < len(toks):
            t, v = toks[pos]
            if t == "}":
                return d, pos + 1
            if t != "v":
                raise ValueError("unexpected token %r" % (t,))
            pos += 1
            t2, v2 = toks[pos]
            if t2 == "{":
                sub, pos = block(pos + 1)
                d[v] = sub
            else:
                d[v] = v2
                pos += 1
        return d, pos

    return block(0)[0]


def read_text(path):
    with io.open(path, encoding="utf-8", errors="replace") as f:
        return f.read()


# ---------- appinfo.vdf (二进制) ----------

def parse_appinfo(path, want=None):
    """返回 {appid: name}。

    v29 布局：magic(4) universe(4) string_pool_offset(8)，然后是若干条
    `appid(4) size(4) kv(size) sha1(20)`。kv 里 key 是字符串表下标，
    顶层第一个字段固定是 name（下标 4），所以直接扫这个模式最快。
    """
    with open(path, "rb") as f:
        data = f.read()
    if len(data) < 16:
        return {}
    magic, _universe, sp_off = struct.unpack_from("<IIQ", data, 0)
    if magic != 0x07564429:
        return {}

    pat = b"\x00\x01\x04\x00\x00\x00"  # 根 -> name(下标4)
    names = {}
    limit = sp_off - 16
    pos = 16
    while True:
        i = data.find(pat, pos, limit)
        if i < 0:
            break
        rec = i - 8
        if rec >= 16:
            appid, size = struct.unpack_from("<II", data, rec)
            if 0 < appid < 50000000 and 0 < size < 5000000:
                end = min(len(data), i + 6 + size)
                j = data.find(b"\x00", i + 6, end)
                if j > 0:
                    names[appid] = data[i + 6:j].decode("utf-8", "replace")
        pos = i + 1

    # 第二遍：有些 app 的 name 不在第一个字段（比如 CS2），单独补
    for appid in (want or ()):
        if appid in names:
            continue
        nb = struct.pack("<I", appid)
        p = 16
        while True:
            h = data.find(nb, p, limit)
            if h < 0:
                break
            p = h + 1
            if data[h + 8] != 0x00:
                continue
            size = struct.unpack_from("<I", data, h + 4)[0]
            if not (0 < size < 5000000):
                continue
            end = min(len(data), h + 8 + size)
            k = data.find(b"\x01\x04\x00\x00\x00", h + 9, end)
            if k < 0:
                continue
            j = data.find(b"\x00", k + 5, end)
            if j < 0:
                continue
            names[appid] = data[k + 5:j].decode("utf-8", "replace")
            break
    return names


# ---------- 主流程 ----------

def load_library_folders():
    p = os.path.join(STEAM, "steamapps", "libraryfolders.vdf")
    if not os.path.exists(p):
        return []
    data = parse_kv(read_text(p)).get("libraryfolders", {})
    out = []
    for key, val in data.items():
        if isinstance(val, dict) and val.get("path"):
            out.append(val["path"])
    return out


def load_manifests():
    names = {}
    for lib in load_library_folders():
        d = os.path.join(lib, "steamapps")
        if not os.path.isdir(d):
            continue
        for fn in os.listdir(d):
            if fn.startswith("appmanifest_") and fn.endswith(".acf"):
                try:
                    info = parse_kv(read_text(os.path.join(d, fn))).get("AppState", {})
                except Exception:
                    continue
                if info.get("name"):
                    names[int(info["appid"])] = info["name"]
    return names


def load_playtime():
    p = os.path.join(STEAM, "userdata", USER_ID, "config", "localconfig.vdf")
    root = parse_kv(read_text(p))
    apps = root.get("UserLocalConfigStore", {}).get("Software", {}).get("Valve", {}).get("Steam", {}).get("apps", {})
    out = {}
    for k, v in apps.items():
        if not k.isdigit() or not isinstance(v, dict):
            continue
        if int(k) <= 0:  # appid 0 是 Steam 自己的汇总项
            continue
        try:
            mins = int(v.get("Playtime", 0))
        except (TypeError, ValueError):
            continue
        if mins <= 0:
            continue
        out[int(k)] = {
            "appid": int(k),
            "minutes": mins,
            "last": int(v.get("LastPlayed", 0) or 0),
        }
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--json")
    a = ap.parse_args()

    play = load_playtime()
    manifest_names = load_manifests()
    names = dict(manifest_names)
    need = [x for x in play if x not in names]
    if need:
        extra = parse_appinfo(os.path.join(STEAM, "appcache", "appinfo.vdf"), want=need)
        for appid in need:
            if appid in extra:
                names[appid] = extra[appid]

    rows = []
    fix = {}
    if os.path.exists(FIX_JSON):
        with io.open(FIX_JSON, encoding="utf-8") as f:
            fix = json.load(f)
    for appid, info in play.items():
        rows.append({
            "appid": appid,
            "name": fix.get(str(appid)) or names.get(appid, "APP %d" % appid),
            "minutes": info["minutes"],
            "hours": round(info["minutes"] / 60.0, 1),
            "last": info["last"],
            "installed": appid in manifest_names,
        })
    rows.sort(key=lambda r: -r["minutes"])

    total = sum(r["minutes"] for r in rows)
    print("游戏数: %d    总时长: %.1f 小时" % (len(rows), total / 60.0))
    print("-" * 62)
    for r in rows:
        when = time.strftime("%Y-%m-%d", time.localtime(r["last"])) if r["last"] else "-"
        print("%-42s %7.1f h   最后 %s   (id %d)" % (r["name"][:40], r["hours"], when, r["appid"]))

    if a.json:
        with io.open(a.json, "w", encoding="utf-8") as f:
            json.dump(rows, f, ensure_ascii=False, indent=2)
        print("\n已写: %s" % a.json)


if __name__ == "__main__":
    main()
