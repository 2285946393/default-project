# -*- coding: utf-8 -*-
"""
斗地主桌图收尾：魔搭 4 倍超分 → 抠图（兼容"品红底"和"已带 alpha"两种）→ 缩到 1920x1080
→ 落进 assets/table/table_ddz_*.png。

用法（用带 cv2 的那个 python）：
  D:/software/Python/python.exe tools/finish-ddz-tables-ms.py            # 全部
  D:/software/Python/python.exe tools/finish-ddz-tables-ms.py green      # 只跑名字含 green 的

为什么这么干：魔搭出图固定在 1024x576（显存上限），直接铺 1920x1080 会发虚。
它家的 real-cugan-x4 是**4 倍**超分（实测 1024x576 → 4096x2304，4 秒），先用它放大再缩。
"""
import io, json, os, sys, glob, time, urllib.request, uuid, pathlib

import numpy as np
import cv2

ROOT = pathlib.Path(__file__).resolve().parent.parent
SRC = pathlib.Path(r"D:/work/AI产出/斗地主桌")
UP = SRC / "upscaled"
ASSETS = ROOT / "assets/table"
TOKEN_FILE = pathlib.Path(r"D:/software/dsh-home/.modelscope-token")
BASE = "https://studio-lf3000y-image-studio.api-inference.modelscope.net"

TARGET = (1920, 1080)      # 整幅 1920x1080，椭圆外透明（DouSpireUi.Fill 铺满整屏）
BOX = (1720, 957)          # ★ 代码写死的椭圆尺寸（DouSpireTable.TableAx/TableAy + Px(1720)/Px(957)）
ASPECT_TOL = 0.15          # 生成椭圆和 1720:957 差多少以内允许"拉正"（差太多就只 contain，不拉）

NAMES = ["table_ddz_green", "table_ddz_brass", "table_ddz_obsidian",
         "table_ddz_frost", "table_ddz_anime", "table_ddz_lacquer",
         "table_ddz_steam"]


def token() -> str:
    return TOKEN_FILE.read_text(encoding="utf-8").strip()


# ── 1. 魔搭 4 倍超分 ──────────────────────────────────────────────

def upscale(src: pathlib.Path, dst: pathlib.Path) -> None:
    boundary = uuid.uuid4().hex
    parts = []
    for k, v in (("model", "real-cugan-x4"), ("scale", "4")):
        parts.append(f"--{boundary}\r\nContent-Disposition: form-data; name=\"{k}\"\r\n\r\n{v}\r\n".encode())
    parts.append(
        f"--{boundary}\r\nContent-Disposition: form-data; name=\"image\"; "
        f"filename=\"{src.name}\"\r\nContent-Type: image/png\r\n\r\n".encode())
    parts.append(src.read_bytes())
    parts.append(f"\r\n--{boundary}--\r\n".encode())
    body = b"".join(parts)

    req = urllib.request.Request(
        BASE + "/v1/images/edits", data=body, method="POST",
        headers={"Authorization": "Bearer " + token(),
                 "Content-Type": f"multipart/form-data; boundary={boundary}"})
    t0 = time.time()
    with urllib.request.urlopen(req, timeout=600) as r:
        resp = json.load(r)
    d0 = resp["data"][0]
    url = d0["url"]
    with urllib.request.urlopen(url, timeout=300) as r2:
        data = r2.read()
    dst.parent.mkdir(parents=True, exist_ok=True)
    dst.write_bytes(data)
    m = d0.get("meta", {})
    print(f"    ⤴ 超分 {m.get('width')}x{m.get('height')}  {len(data)//1024} KB  "
          f"{time.time()-t0:.0f}s", flush=True)


# ── 2. 抠图 + 缩放 ────────────────────────────────────────────────

FLOOD_TOL = 60          # 洪水填充：和种子色差多少以内算"同一片底"


def cutout(bgra: np.ndarray) -> np.ndarray:
    """返回 RGBA。两种输入都兼容：① 不透明品红底 ② 已经带 alpha 的透明底。

    ★ **为什么不能靠"离纯品红多远"判**：魔搭这几张画出来的"品红"其实是
      **(118, 0, 255)**（偏蓝的洋红）—— 离纯品红 (255,0,255) 有 137 的距离，
      用固定阈值 130 会把它判成"实体"，整片底就抠不掉（第一版就是这么翻的车）。
      改成**从四边洪水填充**：只要和边界连通、颜色又接近边角那种底，就是背景。
      这样不管底是哪种洋红、还是模型自作主张换的颜色，都认得出来。
    """
    if bgra.ndim == 2:
        bgra = cv2.cvtColor(bgra, cv2.COLOR_GRAY2BGRA)
    if bgra.shape[2] == 3:
        bgra = cv2.cvtColor(bgra, cv2.COLOR_BGR2BGRA)

    b, g, r, a0 = (bgra[:, :, i].astype(np.float32) for i in range(4))
    h, w = a0.shape
    bgr8 = np.stack([b, g, r], axis=2).astype(np.uint8)

    corner_a = max(int(a0[2, 2]), int(a0[2, w - 3]), int(a0[h - 3, 2]), int(a0[h - 3, w - 3]))

    # ⚠️ 阈值必须是**很低**的 30，不能是 128 —— 实测 lacquer 那批的底是**半透明紫**
    #    （alpha≈119、颜色 RGB(120,0,66)），按"半透明就当透明"处理的话，
    #    那层紫底会原样留在图里（预览里就是桌子外面一个紫方框）。
    if corner_a < 30:
        # ① 底已经是全透明 —— 直接用，只清残余洋红
        a = a0.copy()
    else:
        # ② 不透明底 —— 从四边洪水填充（FLOODFILL_MASK_ONLY，不动原图）
        ff = np.zeros((h + 2, w + 2), np.uint8)
        tol = (FLOOD_TOL,) * 3
        # ★ 种子**撒满整条边界**，不能只撒四个角：模型画的底常带渐变，
        #   只从角上起步会有一片远端的底没被填到（实测 lacquer 就是四角抠掉了、
        #   中间一圈底还在，预览里看得见一个方框）。
        seeds = []
        for x in range(2, w - 2, 24):
            seeds += [(x, 2), (x, h - 3)]
        for y in range(2, h - 2, 24):
            seeds += [(2, y), (w - 3, y)]
        for sx, sy in seeds:
            if ff[sy + 1, sx + 1]:
                continue
            # ⚠️ 必须带 FIXED_RANGE：不带的话 floodFill 是拿**邻居**比，
            #    会顺着抗锯齿渐变一路蔓延进桌面（实测 obsidian 被吃掉 99%）。
            #    带上是拿**种子色**比，只在"和边角底同色"的连通区域里走。
            cv2.floodFill(bgr8, ff, (sx, sy), 0,
                          loDiff=tol, upDiff=tol,
                          flags=8 | cv2.FLOODFILL_MASK_ONLY
                          | cv2.FLOODFILL_FIXED_RANGE | (255 << 8))
        bg = ff[1:-1, 1:-1] > 0
        a = np.where(bg, 0.0, a0)

        # 兜底保护：抠掉的面积离谱（<3% 或 >97%）说明这把洪水判歪了，宁可不抠
        frac = float(bg.mean())
        if not (0.03 < frac < 0.97):
            print(f"    ⚠️ 洪水填充占比 {frac:.1%} 不合理，这张不抠底（保留原 alpha）")
            a = a0.copy()

    # ③ 去洋红溢色：洋红是 R/B 高、G 低 —— 把 G 抬到 (R+B)/2。
    #    斗地主这几张台面（绿绒/酒红/近黑/月白/薄荷/朱红/油布）里没有一个是这种色，
    #    所以这条只在抠剩下的洋红边缘上生效，不会误伤主体。
    spill = (b > g + 60) & (r > g + 40) & (a > 0)
    g = np.where(spill, np.minimum((r + b) * 0.5, 255.0), g)

    out = np.stack([b, g, r, a], axis=2)
    return out.astype(np.uint8)


def resize_rgba(bgra: np.ndarray, size) -> np.ndarray:
    """预乘再缩 —— 不然边缘会挂一圈底色。"""
    f = bgra.astype(np.float32)
    a = f[:, :, 3:4] / 255.0
    pm = np.concatenate([f[:, :, :3] * a, f[:, :, 3:4]], axis=2)
    pm = cv2.resize(pm, size, interpolation=cv2.INTER_AREA)
    a2 = np.clip(pm[:, :, 3:4] / 255.0, 0.0, 1.0)
    rgb = np.where(a2 > 1e-4, pm[:, :, :3] / np.maximum(a2, 1e-4), 0.0)
    return np.concatenate([np.clip(rgb, 0, 255), pm[:, :, 3:4]], axis=2).astype(np.uint8)


def finish(up: pathlib.Path, dst: pathlib.Path) -> None:
    raw = cv2.imdecode(np.fromfile(str(up), dtype=np.uint8), cv2.IMREAD_UNCHANGED)
    if raw is None:
        raise RuntimeError(f"读不了 {up}")
    rgba = cutout(raw)
    # 先把椭圆外多余的透明边裁掉，再等比放进 1920x1080 的画布（保持居中、不拉伸）
    bbox = cv2.boundingRect((rgba[:, :, 3] > 8).astype(np.uint8))
    x, y, w, h = bbox
    rgba = rgba[y:y + h, x:x + w]

    canvas = np.zeros((TARGET[1], TARGET[0], 4), np.uint8)

    # ★ 必须把椭圆**拉到代码写死的那 1720x957** —— 牌位/底牌/出牌区都是按**整屏比例**
    #   定位的，桌子尺寸差一点，牌就不落在刻线里。生成椭圆和目标的宽高比一般差 3~5%，
    #   这点拉伸肉眼看不出；差太多（>15%）就说明模型画歪了，只 contain、不硬拉。
    if abs((w / h) / (BOX[0] / BOX[1]) - 1.0) <= ASPECT_TOL:
        nw, nh = BOX
        rgba = resize_rgba(rgba, (nw, nh))
        fit = "拉正"
    else:
        scale = min(BOX[0] / w, BOX[1] / h)
        nw, nh = max(1, int(round(w * scale))), max(1, int(round(h * scale)))
        rgba = resize_rgba(rgba, (nw, nh))
        fit = "contain（比例差太多，没拉）"

    ox, oy = (TARGET[0] - nw) // 2, (TARGET[1] - nh) // 2
    canvas[oy:oy + nh, ox:ox + nw] = rgba

    dst.parent.mkdir(parents=True, exist_ok=True)
    ok, buf = cv2.imencode(".png", canvas)
    if not ok:
        raise RuntimeError("png 编码失败")
    buf.tofile(str(dst))
    cov = float((canvas[:, :, 3] > 8).mean()) * 100
    flag = "  ⚠️ 不透明区太小，八成抠穿了" if cov < 40 else ""
    print(f"    ✂ {dst.name}  原比例 {w/h:.3f} → {nw}x{nh}（{fit}）"
          f" 画布 {TARGET[0]}x{TARGET[1]}  实心 {cov:.0f}%  "
          f"{os.path.getsize(dst)//1024} KB{flag}")


def main():
    want = sys.argv[1] if len(sys.argv) > 1 else ""
    for name in NAMES:
        if want and want not in name:
            continue
        src = SRC / f"{name}.png"
        if not src.exists():
            print(f"[{name}] 源图不在，跳过")
            continue
        up = UP / f"{name}.png"
        if not up.exists() or up.stat().st_size < 10_000:
            print(f"[{name}] 超分…", flush=True)
            try:
                upscale(src, up)
            except Exception as e:                    # noqa: BLE001
                print(f"[{name}] ❌ 超分失败：{e}")
                continue
        else:
            print(f"[{name}] 超分图已存在")
        try:
            finish(up, ASSETS / f"{name}.png")
        except Exception as e:                        # noqa: BLE001
            print(f"[{name}] ❌ 抠图失败：{e}")
    print("完毕 →", ASSETS)


if __name__ == "__main__":
    main()
