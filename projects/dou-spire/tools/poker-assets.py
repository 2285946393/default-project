#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
poker-assets.py —— 德州扑克素材入库：抠图 / 裁边 / 切分 / 缩放 / 体检

为什么单独写一个（而不是用 sts2-asset-library/tools/keyout.py）：
  1. keyout.py 是**全局色键**：整张图凡是接近底色的像素一律透明。
     这对洋红底没问题，但**白底会误伤**（筹码的奶油色段、木牌匾本身就很白，
     全局键会把主体打出洞）。所以这里改成**从四边泛洪**，只干掉和边界连通的那片。
  2. 这一批图里有 8 张生成时**直接带 alpha**（主体 253~255，边缘无白边），
     不用再抠 —— 硬抠反而会把软边搞脏。所以要有 `alpha` 模式：直接用原 alpha。
  3. 有三张是**一张图里并排好几个东西**（5 枚筹码 / 5 个动作图标 / 10 种牌型图标），
     要按列投影切开，keyout.py 的 `--split N` 只支持 N×N 方阵，切不了 1×5。

用法：
    python tools/poker-assets.py                 # 默认跑德州那套（源目录 assets/德州扑克）
    python tools/poker-assets.py --set gilded    # 跑烫金牌面那套
    python tools/poker-assets.py --set all
    python tools/poker-assets.py --dry           # 只报告，不写文件
    python tools/poker-assets.py --only chip
    python tools/poker-assets.py --list          # 只列出源目录里有什么（认图用）
    python tools/poker-assets.py --src <目录>    # 换源目录

⚠️ 生成器给的文件名是**哈希**，认不出谁是谁。拿到新图先 `--list` 看一眼尺寸，
   或者干脆自己把文件改成 `pip_2.png` / `card_face.png` 这种名字 ——
   脚本的 `src` 字段就是按**文件名前缀**匹配的。

依赖：opencv-python + scipy + numpy（本机 D:/software/Python 里有）。
"""
import argparse
import glob
import os
import sys

import cv2
import numpy as np
from scipy import ndimage

# ─────────────────────────── 路径 ───────────────────────────

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.dirname(HERE)                       # projects/dou-spire
ASSETS = os.path.join(PROJ, "assets")
ROOT = os.path.abspath(os.path.join(PROJ, "..", ".."))   # Default Project
SRC_POKER = os.path.join(ROOT, "assets", "德州扑克")
SRC_GILDED = os.path.join(ROOT, "assets", "烫金牌面")
PREVIEW_DIR = os.path.join(ROOT, "assets", "_入库预览")

# ─────────────────────────── 任务表 ───────────────────────────
# src  : 源文件名的**前缀**（生成器给的是哈希名，用前缀匹配）
# dst  : 相对 assets/ 的输出路径
# mode : key    = 洋红底，泛洪抠
#        alpha  = 原图已带 alpha，直接用（只清噪点 + 裁边）
#        flat   = 满幅图，不抠，只按目标尺寸居中裁切 + 缩放
#        split  = 原图已带 alpha，按列切成多个（一张图并排几个东西）
# max  : 单边上限，超过就等比缩小（None = 不缩）
# size : 精确输出尺寸（会先按目标宽高比居中裁切）

POKER_JOBS = [
    # ── 第一梯队 ───────────────────────────────────────────────
    dict(src="6e084887", dst="table/table_poker.png", mode="key",
         note="德州专用经典绿绒圆桌（满幅，不缩）"),
    # ── 德州牌桌新风格（原图命名为 table_poker_<style>.png 放入 assets/德州扑克） ──
    # 都是品红底俯视桌面：走四边泛洪抠图；缺图时游戏自动回落到经典 table_poker.png。
    dict(src="table_poker_brass", dst="table/table_poker_brass.png", mode="key",
         note="牌桌风格：暗黑黄铜·酒红皮革"),
    dict(src="table_poker_obsidian", dst="table/table_poker_obsidian.png", mode="key",
         note="牌桌风格：黑曜石·熔金纹"),
    dict(src="table_poker_frost", dst="table/table_poker_frost.png", mode="key",
         note="牌桌风格：冰蓝水晶·月白"),
    dict(src="table_poker_anime", dst="table/table_poker_anime.png", mode="key",
         note="牌桌风格：二次元明亮"),
    dict(src="table_poker_lacquer", dst="table/table_poker_lacquer.png", mode="key",
         note="牌桌风格：东方漆器·朱金"),
    dict(src="table_poker_steam", dst="table/table_poker_steam.png", mode="key",
         note="牌桌风格：蒸汽朋克·黄铜齿轮"),

    # ── 斗地主专用桌图四款（2026-10-10 晚，原图放 assets/斗地主桌/ 与 德州扑克/ 各一份）──
    # ★ 白底俯视桌面（不是品红）：estimate_key 从四边泛洪，白底一样能抠。
    #   功能刻线 = 斗地主版：顶部**四连**底牌框（4 人 2v2）、双牌堆、中央圆纹章、下方出牌大槽。
    #   游戏侧由 DouSpireAssets.DouSpireTableOf() 优先取这几张，缺图回落德州 table_poker*。
    dict(src="table_ddz_a_luminous", dst="table/table_ddz_luminous.png", mode="key",
         note="斗地主桌：A 午夜靛金·发光赛璐珞（米山舞技法）"),
    dict(src="table_ddz_b_flatposter", dst="table/table_ddz_flatposter.png", mode="key",
         note="斗地主桌：B 薄荷灰蓝·大色块平面（望月けい技法）"),
    dict(src="table_ddz_c_airylight", dst="table/table_ddz_airylight.png", mode="key",
         note="斗地主桌：C 月白木色·玻璃箱庭（loundraw 技法）"),
    dict(src="table_ddz_d_galgame", dst="table/table_ddz_galgame.png", mode="key",
         note="斗地主桌：D 翡翠绒·galgame 明亮（ピロ水技法）"),

    # ── 德州牌室场景（原图命名为 poker_<场景>.png 放入 assets/德州扑克） ──
    # ★ 和上面那批牌桌**不是一回事**：这四张是 16:9 **满幅背景**，
    #   **没有品红底、没有透明区、不抠图** —— 只居中裁到 16:9 再缩到 1920×1080（mode=flat）。
    #   缺图时游戏垫一层暖色渐变（PokerRoomFallback），**绝不掉回斗地主那间酒馆**。
    #   文件名主干见 DouSpireAssets.SceneKey()，**改文件名要两处一起改**。
    dict(src="poker_club", dst="bg/poker_club.png", mode="flat", size=(1920, 1080),
         note="牌室场景：暖木俱乐部（默认档）"),
    dict(src="poker_night", dst="bg/poker_night.png", mode="flat", size=(1920, 1080),
         note="牌室场景：月夜私人牌室"),
    dict(src="poker_anime", dst="bg/poker_anime.png", mode="flat", size=(1920, 1080),
         note="牌室场景：二次元画风"),
    dict(src="poker_den", dst="bg/poker_den.png", mode="flat", size=(1920, 1080),
         note="牌室场景：地下赌窟"),

    # ── 德州桌面上那两个小圆钮（原图命名为 knob_room.png / knob_desk.png 放入 assets/德州扑克）──
    # ★ 2026-10-09 加。规格：1:1 正方 + 品红底 + 中性香槟金环（提示词见 assets/德州小按钮-生成提示词-wb.txt）。
    #   走 key 模式抠品红 + 裁边 ⇒ 出来的图**圆的边界就是图的边界**，
    #   代码里整图拉伸到 86×86（DouSpirePokerTable.KnobSize），圆就是 86，跟矢量版同尺寸。
    #   缩到 128（86 的 1.5 倍，够 2K 屏用）。
    #   ⚠️ 图缺失时代码**自动退回矢量自绘**（DouSpireKnobIcon），不会白块。
    dict(src="knob_room", dst="ui/knob_room.png", mode="key", max=(128, 128),
         note="桌面圆钮：室内（屋子图标）"),
    dict(src="knob_desk", dst="ui/knob_desk.png", mode="key", max=(128, 128),
         note="桌面圆钮：桌面（牌桌图标）"),

    dict(src="f53914b1", dst="poker/chip_stack.png", mode="alpha", max=(512, 512),
         note="筹码堆"),
    dict(src="7b483763", dst="poker/chip_single.png", mode="key", max=(256, 256),
         note="单枚筹码"),
    dict(src="17915412", dst="poker/chips_row.png", mode="split", max=(192, 192),
         names=["chip_1", "chip_2", "chip_3", "chip_4", "chip_5"],
         note="筹码五色 → 切成 5 个 chip_N.png（N=1 最小面额）"),
    dict(src="a33b24ca", dst="poker/dealer_button.png", mode="key", max=(256, 256),
         note="庄家按钮（空白黄铜，字由代码画）"),
    dict(src="2d89c452", dst="poker/blind_marker.png", mode="alpha", max=(256, 256),
         note="盲注标记"),
    dict(src="b6b6131e", dst="poker/pot_plate.png", mode="key", max=(1024, 256),
         note="底池牌匾（九宫格）"),
    dict(src="60253cc5", dst="table/rest_option_dou_spire_poker.png", mode="flat",
         size=(512, 338), note="篝火选项图标 2x —— 要进 DouSpire.icon.pck"),
    dict(src="60253cc5", dst="table/rest_option_dou_spire_poker_1x.png", mode="flat",
         size=(256, 169), note="篝火选项图标 1x"),

    # ── 第二梯队 ───────────────────────────────────────────────
    dict(src="4a67c93d", dst="poker/board_plate.png", mode="key", max=(1280, 512),
         note="公共牌区底板（5 个牌位）"),
    dict(src="b93af766", dst="poker/handrank_plate.png", mode="alpha", max=(1024, 256),
         note="「你现在的牌型」标签底板"),
    dict(src="a57d28b6", dst="poker/act_icons.png", mode="split", max=(128, 128),
         names=["act_fold", "act_check", "act_call", "act_raise", "act_allin"],
         note="五个动作图标 → act_*.png（顺序：弃牌/过牌/跟注/加注/全下）"),
    dict(src="0b7941ae", dst="poker/rank_icons.png", mode="split", max=(128, 128),
         split_thresh=80,          # 同花顺/皇家同花顺外面那圈橙色辉光会把三张粘一起
         names=["rank_1", "rank_2", "rank_3", "rank_4", "rank_5",
                "rank_6", "rank_7", "rank_8", "rank_9", "rank_10"],
         note="十种牌型图标 → rank_N.png（N=1 高牌 … 10 皇家同花顺）"),

    # ── 第三梯队 ───────────────────────────────────────────────
    dict(src="ffb97890", dst="fx/winner_halo.png", mode="alpha", max=(512, 512),
         note="赢家光环"),

    # ── 备用（多出来的一张宽木牌匾，先存着） ────────────────────
    dict(src="a1937e98", dst="poker/plate_wood.png", mode="alpha", max=(1024, 256),
         note="木纹宽牌匾（备用：结算标题 / 座位名牌）"),
]

# ─────────────────── 烫金牌面（第三档牌风） ───────────────────
# 见 assets/烫金牌面-生成提示词.md。
# 全部落到 assets/cards/gilded/，**一个旧文件都不覆盖**。
# ⚠️ src 是按**文件名前缀**匹配的：生成器给哈希名时，先 `--list` 认一遍再改这里。

_GILDED_PIP_NAMES = ["2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A"]

GILDED_JOBS = [
    # 四个花色是一张横图，按列切成 4 份
    dict(src="suit_sheet", dst="cards/gilded/suit_sheet.png", mode="split",
         names=["suit_spade", "suit_heart", "suit_club", "suit_diamond"],
         split_thresh=80, max=(256, 256), kill_bg_color=True,
         note="花色合图 → suit_*.png（切分阈值调高，免得金色描边把四个粘一起）"),
] + [
    dict(src=f"pip_{n}", dst=f"cards/gilded/pip_{n}.png", mode="key", max=(256, 256),
         kill_bg_color=True, note=f"点数 {n}")
    for n in _GILDED_PIP_NAMES
] + [
    dict(src="card_face", dst="cards/gilded/card_face.png", mode="key", max=(512, 716),
         kill_bg_color=True, note="牌面底（普通）"),
    dict(src="card_face_sel", dst="cards/gilded/card_face_sel.png", mode="key", max=(512, 716),
         kill_bg_color=True, note="牌面底（高亮，摊牌挑赢牌用）"),
    dict(src="card_back", dst="cards/gilded/card_back.png", mode="key", max=(512, 716),
         kill_bg_color=True, note="牌背（德州最显眼的一张，对手底牌一直盖着）"),
]

SETS = {
    "poker": (POKER_JOBS, SRC_POKER),
    "gilded": (GILDED_JOBS, SRC_GILDED),
}

# 洋红抠图参数（沿用 keyout.py 的默认手感）
TOL_LO = 55.0      # 到这个距离以内 → 全透明
TOL_HI = 110.0     # 超过这个距离 → 全不透明；中间按比例过渡


# ─────────────────────────── 基础工具 ───────────────────────────

def _imread_unicode(path):
    """
    ⚠️ Windows 上 `cv2.imread` 用的是 ANSI 的 fopen —— **中文路径一律读不到**
    （只在控制台打一行 `can't open/read file`，不抛异常）。所以自己读字节再解码。
    """
    try:
        buf = np.fromfile(path, dtype=np.uint8)
    except OSError as e:
        raise RuntimeError(f"读不了：{path}（{e}）") from e
    if buf.size == 0:
        raise RuntimeError(f"文件是空的：{path}")
    return cv2.imdecode(buf, cv2.IMREAD_UNCHANGED)


def _imwrite_unicode(path, bgr):
    """同理，写也不能用 cv2.imwrite。"""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    ok, buf = cv2.imencode(os.path.splitext(path)[1], bgr)
    if not ok:
        raise RuntimeError(f"编码失败：{path}")
    buf.tofile(path)


def read_img(path):
    """读成 (RGB uint8 HxWx3, alpha uint8 HxW 或 None)。"""
    im = _imread_unicode(path)
    if im is None:
        raise RuntimeError(f"读不了：{path}")
    if im.ndim == 2:
        im = cv2.cvtColor(im, cv2.COLOR_GRAY2BGRA)
    if im.shape[2] == 3:
        return cv2.cvtColor(im, cv2.COLOR_BGR2RGB), None
    return cv2.cvtColor(im, cv2.COLOR_BGRA2RGB), im[:, :, 3].copy()


def save_rgba(path, rgb, a):
    bgra = np.dstack([rgb[:, :, 2], rgb[:, :, 1], rgb[:, :, 0], a])
    _imwrite_unicode(path, bgra)


def save_rgb(path, rgb):
    _imwrite_unicode(path, cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR))


def border_ring(rgb, inset=3):
    """取最外一圈像素，用来估底色。"""
    h, w, _ = rgb.shape
    return np.concatenate([
        rgb[:inset].reshape(-1, 3), rgb[-inset:].reshape(-1, 3),
        rgb[:, :inset].reshape(-1, 3), rgb[:, -inset:].reshape(-1, 3)])


def estimate_key(rgb):
    return np.median(border_ring(rgb).astype(np.float32), axis=0)


def flood_background(rgb, key, tol_hi):
    """
    **只**把和四边连通的"像底色"的像素判为背景。

    这是和白底图打交道的关键：主体内部的奶油色/白色段落虽然颜色接近白底，
    但它和边界不连通，所以不会被误杀。
    """
    d = np.linalg.norm(rgb.astype(np.float32) - key[None, None, :], axis=2)
    cand = d <= tol_hi
    lab, n = ndimage.label(cand)
    if n == 0:
        return np.zeros(d.shape, bool), d
    border = set(np.unique(np.concatenate(
        [lab[0, :], lab[-1, :], lab[:, 0], lab[:, -1]])))
    border.discard(0)
    bg = np.isin(lab, list(border))
    return bg, d


def alpha_from_bg(bg, d, tol_lo=TOL_LO, tol_hi=TOL_HI):
    a = np.full(d.shape, 255, np.uint8)
    ramp = np.clip((d - tol_lo) / max(1.0, tol_hi - tol_lo), 0.0, 1.0) * 255.0
    a[bg] = ramp[bg].astype(np.uint8)
    return a


def despill(rgb, a, key):
    """
    半透明边像素是被底色"染"过的：P = α·C + (1-α)·K。
    反解 C = (P - (1-α)·K) / α，否则边上留一圈洋红描边。
    """
    m = (a > 0) & (a < 255)
    if not m.any():
        return rgb
    af = (a[m].astype(np.float32) / 255.0)[:, None]
    c = (rgb[m].astype(np.float32) - (1.0 - af) * key[None, :]) / af
    out = rgb.copy()
    out[m] = np.clip(c, 0, 255).astype(np.uint8)
    return out


def bleed_edges(rgb, a):
    """
    把**完全透明**像素的 RGB 换成"最近的那个不透明像素"的颜色（标准做法，叫 alpha bleed）。

    为什么必须做：抠完之后，透明区的 RGB **还留着洋红**（我们只改了 alpha）。
    平时看不出来，但 `cv2.resize(INTER_AREA)` 是**四个通道各算各的平均**——
    于是透明区的洋红会被平均进边缘像素的 RGB 里，而那个像素的 alpha 又不为 0，
    结果就是整圈**淡洋红描边**。先外扩颜色再缩放，就没有这个问题。
    """
    solid = a > 0
    if solid.all() or not solid.any():
        return rgb
    idx = ndimage.distance_transform_edt(~solid, return_distances=False, return_indices=True)
    return rgb[idx[0], idx[1]]


def clean_mask(a, min_area=48, hole_max=400, floor=6):
    """
    去掉零星噪点、填掉主体里的小洞。
    floor 以下的 alpha 一律归零（生成图边缘常有 1~5 的毛刺）。
    """
    solid = a > floor
    lab, n = ndimage.label(solid)
    if n:
        sizes = ndimage.sum(solid, lab, index=np.arange(1, n + 1))
        keep = np.zeros(n + 1, bool)
        keep[1:] = sizes >= min_area
        solid = keep[lab]

    hole = ~solid
    lab2, n2 = ndimage.label(hole)
    if n2:
        sizes2 = ndimage.sum(hole, lab2, index=np.arange(1, n2 + 1))
        border2 = set(np.unique(np.concatenate(
            [lab2[0, :], lab2[-1, :], lab2[:, 0], lab2[:, -1]])))
        border2.discard(0)
        fill = np.zeros(n2 + 1, bool)
        for i in range(1, n2 + 1):
            if i not in border2 and sizes2[i - 1] <= hole_max:
                fill[i] = True
        solid = solid | fill[lab2]

    out = a.copy()
    out[~solid] = 0
    return out


def kill_bg_color(rgb, a, key, tol=TOL_LO):
    """
    把「颜色就是底色」的像素一律抠掉 —— 专治**字母的闭合孔洞**：
    8 的两个圈、4 的三角、6/9/10 的圈、J/Q/K 的圈…… 泛洪只从四边进，
    进不去这些**被笔画围住的**洋红块，于是它们会被当成"主体"留下来。

    ⚠️ **只在「底色绝不会出现在主体里」时才开**（洋红 #FF00FF 就是这种）。
    白底素材千万别开 —— 主体里本来就有大片奶油白，一开就打洞
    （这正是当初不用全局色键、改用四边泛洪的原因，见 flood_background 的注释）。
    """
    dist = np.linalg.norm(rgb.astype(np.float32) - np.asarray(key, np.float32)[None, None, :], axis=2)
    return np.where(dist < tol, 0, a).astype(np.uint8)


def trim(rgb, a, thresh=4):
    m = a > thresh
    ys, xs = np.nonzero(m)
    if len(ys) == 0:
        return rgb, a
    y0, y1 = ys.min(), ys.max() + 1
    x0, x1 = xs.min(), xs.max() + 1
    return rgb[y0:y1, x0:x1], a[y0:y1, x0:x1]


def resize(rgb, a, size):
    tw, th = size
    interp = cv2.INTER_AREA if (tw * th < rgb.shape[0] * rgb.shape[1]) else cv2.INTER_CUBIC
    r = cv2.resize(rgb, (tw, th), interpolation=interp)
    al = cv2.resize(a, (tw, th), interpolation=interp) if a is not None else None
    return r, al


def cap(rgb, a, mx):
    """只在超过上限时等比缩小。"""
    if not mx:
        return rgb, a
    h, w = rgb.shape[:2]
    s = min(mx[0] / w, mx[1] / h, 1.0)
    if s >= 1.0:
        return rgb, a
    return resize(rgb, a, (max(1, int(round(w * s))), max(1, int(round(h * s)))))


def center_crop_to_aspect(rgb, a, tw, th):
    h, w = rgb.shape[:2]
    tgt = tw / th
    cur = w / h
    if cur > tgt:                       # 太宽 → 裁左右
        nw = max(1, int(round(h * tgt)))
        x0 = (w - nw) // 2
        return rgb[:, x0:x0 + nw], (a[:, x0:x0 + nw] if a is not None else None)
    nh = max(1, int(round(w / tgt)))    # 太高 → 裁上下
    y0 = (h - nh) // 2
    return rgb[y0:y0 + nh], (a[y0:y0 + nh] if a is not None else None)


def split_columns(a, thresh=4, min_gap=8, min_w=12):
    """
    按列投影把并排的几个东西切开。返回 [(x0,x1), ...]。

    ⚠️ `thresh` 很关键：牌型图标那张里，**最后两个（同花顺/皇家同花顺）外面有一圈
    橙色辉光**，辉光是半透明的（alpha 几十），按 alpha>4 投影时会把三个图标连成一坨。
    实测 alpha>80 才干净分开 —— 所以辉光类素材要把阈值调高，只按"实体"投影。
    """
    col = (a > thresh).any(axis=0)
    segs, s = [], None
    for i, v in enumerate(col):
        if v and s is None:
            s = i
        elif not v and s is not None:
            segs.append([s, i])
            s = None
    if s is not None:
        segs.append([s, len(col)])

    merged = []
    for seg in segs:
        if merged and seg[0] - merged[-1][1] < min_gap:
            merged[-1][1] = seg[1]
        else:
            merged.append(list(seg))
    return [tuple(s) for s in merged if s[1] - s[0] >= min_w]


# ─────────────────────────── 各模式 ───────────────────────────

def run_key(path, job, dry):
    rgb, _ = read_img(path)
    key = estimate_key(rgb)
    bg, d = flood_background(rgb, key, TOL_HI)
    a = alpha_from_bg(bg, d)
    rgb = despill(rgb, a, key)
    a = clean_mask(a)
    # ★ 放在 clean_mask **之后**：clean_mask 会把 ≤400px 的洞填回去（它对小瑕疵是好事），
    #   这里再按颜色杀一遍，把填回来的洋红（以及字母闭合孔洞）清干净。
    if job.get("kill_bg_color"):
        a = kill_bg_color(rgb, a, key)
    rgb = bleed_edges(rgb, a)
    rgb, a = trim(rgb, a)
    rgb, a = cap(rgb, a, job.get("max"))
    info = f"底色 RGB{tuple(int(v) for v in key)} → 抠后 {rgb.shape[1]}×{rgb.shape[0]}"
    return [(job["dst"], rgb, a, info)]


def run_alpha(path, job, dry):
    rgb, a = read_img(path)
    if a is None:
        raise RuntimeError(f"{job['dst']}：以为带 alpha，实际没有 —— 改用 mode=key")
    a = clean_mask(a, min_area=64, hole_max=2000)
    rgb = bleed_edges(rgb, a)
    rgb, a = trim(rgb, a)
    rgb, a = cap(rgb, a, job.get("max"))
    info = f"直接用原 alpha → 裁边后 {rgb.shape[1]}×{rgb.shape[0]}"
    return [(job["dst"], rgb, a, info)]


def run_flat(path, job, dry):
    rgb, a = read_img(path)
    if a is not None:
        # 满幅图：把透明处按主体平均色压掉（这几张本来就是不透明的，兜个底）
        m = a > 128
        if m.any():
            rgb = rgb.copy()
            rgb[~m] = rgb[m].mean(0).astype(np.uint8)
    tw, th = job["size"]
    rgb, _ = center_crop_to_aspect(rgb, None, tw, th)
    rgb, _ = resize(rgb, None, (tw, th))
    info = f"居中裁到 {tw/th:.3f} 宽高比 → {tw}×{th}"
    return [(job["dst"], rgb, None, info)]


def run_split(path, job, dry):
    rgb, a = read_img(path)
    if a is None:
        # 洋红底的合图（烫金牌面那套就是）：**先按色键抠出 alpha，再切列**。
        # 带 alpha 的合图（经典那套）走下面的原路径，行为不变。
        key = estimate_key(rgb)
        bg, d = flood_background(rgb, key, TOL_HI)
        a = alpha_from_bg(bg, d)
        rgb = despill(rgb, a, key)
    a = clean_mask(a, min_area=64, hole_max=2000)
    if job.get("kill_bg_color"):
        a = kill_bg_color(rgb, a, estimate_key(rgb))
    rgb = bleed_edges(rgb, a)
    segs = split_columns(a, thresh=job.get("split_thresh", 4),
                         min_gap=job.get("gap", 8), min_w=job.get("minw", 12))
    names = job["names"]
    if len(segs) != len(names):
        raise RuntimeError(
            f"{job['src']}：按列切出 {len(segs)} 块，但任务表里写了 {len(names)} 个名字。"
            f"切分点 = {segs}（调 split_thresh / gap 再试）")
    # 输出的"基底"是 dst 的**目录**，不是文件名 ——
    # 因为 dst（如 poker/chips_row.png）只是个占位名，真正产出的是 poker/chip_1.png…
    base = os.path.dirname(job["dst"])
    ext = os.path.splitext(job["dst"])[1]
    outs = []
    for (x0, x1), nm in zip(segs, names):
        sr, sa = trim(rgb[:, x0:x1], a[:, x0:x1])
        sr, sa = cap(sr, sa, job.get("max"))
        rel = f"{base}/{nm}{ext}" if base else f"{nm}{ext}"
        outs.append((rel, sr, sa, f"x{x0}..{x1} → {sr.shape[1]}×{sr.shape[0]}"))
    return outs


MODES = {"key": run_key, "alpha": run_alpha, "flat": run_flat, "split": run_split}


# ─────────────────────────── 体检预览 ───────────────────────────

def make_preview(results, out_name="入库预览.png"):
    """
    把所有产出拼成一张对照图。**底色刻意做成左深右浅**：
      左半边深绿台面 → 能看出黑边 / 暗描边
      右半边浅灰     → 能看出白边 / 洋红边
    抠得干净的话，同一个东西压在两种底色上都不该有"描边"。
    """
    TILE = 200
    tiles = []
    for rel, rgb, a, _ in results:
        if a is None:
            a = np.full(rgb.shape[:2], 255, np.uint8)
        h, w = a.shape
        s = min((TILE - 20) / max(h, w), 1.0)
        if s < 1.0:
            rgb, a = resize(rgb, a, (max(1, int(w * s)), max(1, int(h * s))))
        h, w = a.shape

        yy, xx = np.mgrid[0:h, 0:w]
        bg = np.zeros((h, w, 3), np.uint8)
        light = xx >= (w // 2)
        bg[~light] = (26, 58, 42)        # RGB 深绿台面
        bg[light] = (205, 205, 205)      # RGB 浅灰

        alpha = (a.astype(np.float32) / 255.0)[:, :, None]
        over = (rgb.astype(np.float32) * alpha + bg.astype(np.float32) * (1 - alpha))
        over = np.clip(over, 0, 255).astype(np.uint8)

        tile = np.full((TILE, TILE, 3), 30, np.uint8)
        y0, x0 = (TILE - h) // 2, (TILE - w) // 2
        tile[y0:y0 + h, x0:x0 + w] = over
        cv2.putText(tile, os.path.basename(rel)[:26], (4, TILE - 6),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.36, (255, 255, 255), 1, cv2.LINE_AA)
        tiles.append(tile)

    if not tiles:
        return None
    cols = 6
    rows = (len(tiles) + cols - 1) // cols
    sheet = np.full((rows * TILE, cols * TILE, 3), 20, np.uint8)
    for i, t in enumerate(tiles):
        r, c = divmod(i, cols)
        sheet[r * TILE:(r + 1) * TILE, c * TILE:(c + 1) * TILE] = t
    os.makedirs(PREVIEW_DIR, exist_ok=True)
    out = os.path.join(PREVIEW_DIR, out_name)
    _imwrite_unicode(out, cv2.cvtColor(sheet, cv2.COLOR_RGB2BGR))   # 拼图是 RGB，写盘要 BGR
    return out


# ─────────────────────────── 主流程 ───────────────────────────

def find_src(src_dir, prefix):
    hits = sorted(glob.glob(os.path.join(src_dir, prefix + "*.png")))
    if not hits:
        raise RuntimeError(f"在 {src_dir} 里找不到以 {prefix} 开头的 png")
    return hits[0]


def list_sources(src_dir):
    """认图用：把源目录里的 png 连尺寸一起打出来。"""
    files = sorted(glob.glob(os.path.join(src_dir, "*.png")))
    if not files:
        print(f"（{src_dir} 里没有 png）")
        return
    print(f"源目录 {src_dir} —— {len(files)} 个 png\n")
    for p in files:
        try:
            im = _imread_unicode(p)
            h, w = im.shape[:2]
            ch = im.shape[2] if im.ndim == 3 else 1
            note = "带alpha" if ch == 4 else "无alpha(要抠)"
        except Exception as e:                      # noqa: BLE001
            w = h = 0
            note = f"读不了 {e}"
        print(f"  {os.path.basename(p):<52} {w}x{h:<7} {note}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry", action="store_true", help="只报告不写文件")
    ap.add_argument("--only", default=None, help="只跑 dst 里含这个子串的任务")
    ap.add_argument("--set", default="poker", choices=["poker", "gilded", "all"],
                    help="跑哪一套（默认 poker）")
    ap.add_argument("--src", default=None, help="换源目录")
    ap.add_argument("--list", action="store_true", help="只列出源目录里的文件")
    args = ap.parse_args()

    if args.list:
        for name in (["poker", "gilded"] if args.set == "all" else [args.set]):
            list_sources(args.src or SETS[name][1])
            print()
        return 0

    if args.set == "all":
        jobs = POKER_JOBS + GILDED_JOBS
        src_dir = args.src or SRC_POKER
    else:
        jobs, src_dir = SETS[args.set]
        src_dir = args.src or src_dir
    if args.only:
        jobs = [j for j in jobs if args.only in j["dst"]]

    results, problems = [], []
    print(f"源目录 {src_dir}")
    print(f"目标   {ASSETS}")
    print(f"任务   {len(jobs)} 条\n")

    for job in jobs:
        try:
            src = find_src(src_dir, job["src"])
        except RuntimeError as e:
            problems.append(f"[{job['dst']}] {e}")
            continue
        print(f"── {os.path.basename(src)[:34]} → {job['dst']}")
        try:
            outs = MODES[job["mode"]](src, job, args.dry)
        except Exception as e:                      # noqa: BLE001
            problems.append(f"[{job['dst']}] {type(e).__name__}: {e}")
            print(f"   ✗ {type(e).__name__}: {e}")
            continue
        for rel, rgb, a, info in outs:
            print(f"   → {rel:<46} {info}")
            if not args.dry:
                if a is None:
                    save_rgb(os.path.join(ASSETS, rel), rgb)
                else:
                    save_rgba(os.path.join(ASSETS, rel), rgb, a)
            results.append((rel, rgb, a, info))

    if not args.dry and results:
        pv = make_preview(results, f"入库预览-{args.set}.png")
        print(f"\n体检预览：{pv}")

    print(f"\n完成 {len(results)} 个产出。")
    if problems:
        print(f"\n⚠️ 有 {len(problems)} 条没跑成：")
        for p in problems[:12]:
            print("  -", p)
        if len(problems) > 12:
            print(f"  …还有 {len(problems) - 12} 条")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
