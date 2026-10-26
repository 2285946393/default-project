"""导出 Hades 1 / 2 的音频系统与地图数据。

用法：
    python export_audio_map.py
"""

import csv
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")

from lua_table import load_file, flatten

H1 = r"D:/software/steam/steamapps/common/Hades/Content"
H2 = r"D:/software/steam/steamapps/common/Hades II/Content"
OUT = r"D:/work/AI产出/Hades反编译"


def dump_json(obj, name):
    path = os.path.join(OUT, name)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(obj, f, ensure_ascii=False, indent=1)
    return path


def export_audio(content_dir, gen):
    """音频：曲目表 + 触发事件 + 语音行。"""
    scripts = os.path.join(content_dir, "Scripts")
    p = os.path.join(scripts, "AudioData.lua")
    if not os.path.exists(p):
        return {}

    data = load_file(p)
    result = {}

    # 曲目
    tracks = data.get("MusicPlayerTrackData") or {}
    if isinstance(tracks, dict) and tracks:
        result["曲目"] = tracks
        rows = []
        for name, body in tracks.items():
            if not isinstance(body, dict):
                continue
            flat = flatten(body)
            row = {"曲目": name}
            for k in ("DisplayName", "MusicTrack", "Icon", "UnlockCost", "Order"):
                if k in flat:
                    row[k] = flat[k]
            rows.append(row)
        if rows:
            with open(os.path.join(OUT, f"{gen}-音乐曲目.csv"), "w",
                      encoding="utf-8-sig", newline="") as f:
                w = csv.DictWriter(f, fieldnames=list(rows[0].keys()), extrasaction="ignore")
                w.writeheader()
                w.writerows(rows)

    # 曲目顺序
    if "MusicPlayerTrackOrderData" in data:
        result["曲目顺序"] = data["MusicPlayerTrackOrderData"]

    # 所有带 VoiceLines / MusicEvents 的触发组
    triggers = {}
    for k, v in data.items():
        if k in ("MusicPlayerTrackData", "MusicPlayerTrackOrderData", "MusicTrackData"):
            continue
        if isinstance(v, (list, dict)) and v:
            triggers[k] = v
    if triggers:
        result["触发事件"] = triggers

    # 战斗音乐映射
    for k in ("MusicTrackData", "RoomStartMusicEvents", "CombatOverMusicEvents", "AmbienceTracks"):
        if k in data:
            result[k] = data[k]

    if result:
        dump_json(result, f"{gen}-音频系统.json")
    return result


def export_maps(content_dir, gen):
    """地图：光源、亮度、图层组。"""
    maps_dir = os.path.join(content_dir, "Maps")
    if not os.path.isdir(maps_dir):
        return 0

    import glob

    rows = []
    for f in sorted(glob.glob(os.path.join(maps_dir, "*.map_text"))):
        name = os.path.basename(f).replace(".map_text", "")
        try:
            with open(f, encoding="utf-8-sig") as fh:
                d = json.load(fh)
        except Exception:
            continue

        groups = d.get("ThingGroups", []) or []

        def count_nodes(gs):
            n = 0
            for g in gs:
                if not isinstance(g, dict):
                    continue
                n += 1
                kids = g.get("ChildGroups") or []
                if isinstance(kids, list):
                    n += count_nodes(kids)
            return n

        amb = d.get("AmbientLightColor", {}) or {}
        back = d.get("BackdropColor", {}) or {}
        rows.append({
            "地图": name,
            "亮度": d.get("Brightness", ""),
            "环境光R": round(amb.get("X", 0), 3),
            "环境光G": round(amb.get("Y", 0), 3),
            "环境光B": round(amb.get("Z", 0), 3),
            "背景R": back.get("R", ""),
            "背景G": back.get("G", ""),
            "背景B": back.get("B", ""),
            "顶层组": len(groups),
            "全部组": count_nodes(groups),
        })

    if rows:
        with open(os.path.join(OUT, f"{gen}-地图光照.csv"), "w",
                  encoding="utf-8-sig", newline="") as f:
            w = csv.DictWriter(f, fieldnames=list(rows[0].keys()), extrasaction="ignore")
            w.writeheader()
            w.writerows(rows)
    return len(rows)


def export_audio_banks(content_dir, gen):
    """音频银行清单（FMOD）。"""
    import glob

    banks = glob.glob(os.path.join(content_dir, "Audio", "**", "*.bank"), recursive=True)
    if not banks:
        return 0

    rows = []
    for b in sorted(banks):
        rows.append({
            "银行": os.path.basename(b),
            "大小MB": round(os.path.getsize(b) / 1048576, 1),
            "路径": os.path.relpath(b, content_dir),
        })

    with open(os.path.join(OUT, f"{gen}-音频银行.csv"), "w",
              encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=["银行", "大小MB", "路径"])
        w.writeheader()
        w.writerows(rows)
    return len(rows)


def main():
    os.makedirs(OUT, exist_ok=True)

    for gen, content in [("Hades1", H1), ("Hades2", H2)]:
        print(f"\n=== {gen} ===")
        if not os.path.isdir(content):
            print("  目录不存在")
            continue

        a = export_audio(content, gen)
        if a:
            print(f"  音频系统     曲目 {len(a.get('曲目', {}))} 首，"
                  f"触发组 {len(a.get('触发事件', {}))} 个")

        n = export_audio_banks(content, gen)
        print(f"  音频银行     {n} 个")

        n = export_maps(content, gen)
        print(f"  地图光照     {n} 张")

    print(f"\n输出目录：{OUT}")


if __name__ == "__main__":
    main()
