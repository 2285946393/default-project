# -*- coding: utf-8 -*-
"""
校验「卡牌类名 → 卡面文件名 → pck 路径」这条链是否对得上。

为什么需要这个：
    FrostCardArt 的路径推导是**静默失败**的 —— 名字拼错了，游戏不会报错，
    只是那张卡悄悄用回默认卡面。人眼很难发现。这个脚本把三处对齐检查出来：
        1) 源码里的卡牌类名（FrostSpireCode/Cards/*.cs）
        2) 磁盘上的 png（FrostSpire/images/card_art/）
        3) pck 里的资源路径（FrostSpire.pck）
    三者必须完全一致。

用法：
    python tools/verify_card_art.py
退出码 0 = 全对；1 = 有对不上的。
"""
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.normpath(os.path.join(HERE, ".."))
CARDS_DIR = os.path.join(PROJ, "FrostSpireCode", "Cards")
ART_DIR = os.path.join(PROJ, "FrostSpire", "images", "card_art")
PCK = os.path.join(PROJ, "FrostSpire.pck")

# 不产卡面的基类（抽象类，本身不是可打出的卡）
SKIP = {"FrostDeployCardBase", "FrostBuildingCard"}


def to_snake(name: str) -> str:
    """复刻 FrostCardArt.ToSnake：PascalCase → snake_case。"""
    out = []
    for i, c in enumerate(name):
        if c.isupper() and i > 0 and name[i - 1] != "_":
            out.append("_")
        out.append(c.lower())
    return "".join(out)


def entry_of(cls_name: str) -> str:
    """复刻 FrostCardArt.EntryOf：去尾部 Card → snake_case。"""
    n = cls_name
    if n.endswith("Card"):
        n = n[: -len("Card")]
    return to_snake(n)


def scan_card_classes():
    """扫描 Cards/ 下的卡牌类名。"""
    found = []
    for fn in sorted(os.listdir(CARDS_DIR)):
        if not fn.endswith(".cs"):
            continue
        text = open(os.path.join(CARDS_DIR, fn), encoding="utf-8").read()
        for m in re.finditer(r"class\s+(\w+Card)\b", text):
            cls = m.group(1)
            if cls in SKIP:
                continue
            # 抽象类跳过（除非它就是卡本身——这里只排除基类）
            seg = text[max(0, m.start() - 60):m.start()]
            if "abstract" in seg:
                continue
            found.append((cls, fn))
    return found


def pck_art_entries():
    """从 pck 里提取 card_art 下的条目名。"""
    if not os.path.exists(PCK):
        return None
    data = open(PCK, "rb").read()
    pat = rb"res://FrostSpire/images/card_art/([a-z0-9_]+)\.png"
    return sorted({m.decode() for m in re.findall(pat, data)})


def main():
    ok = True
    classes = scan_card_classes()
    print("源码里的卡牌类：%d 个" % len(classes))

    disk = sorted(
        f[:-4] for f in os.listdir(ART_DIR)
        if f.endswith(".png") and not f.startswith("_")
    ) if os.path.isdir(ART_DIR) else []
    pck = pck_art_entries()

    print("\n%-28s %-22s %-6s %-6s" % ("类名", "推导文件名", "磁盘", "pck"))
    print("-" * 68)
    expected = []
    for cls, fn in classes:
        e = entry_of(cls)
        expected.append(e)
        on_disk = "有" if e in disk else "缺"
        in_pck = "-" if pck is None else ("有" if e in pck else "缺")
        flag = ""
        if on_disk == "缺" or in_pck == "缺":
            flag = "  <== 对不上"
            ok = False
        print("%-28s %-22s %-6s %-6s%s" % (cls, e + ".png", on_disk, in_pck, flag))

    # 反向：磁盘/pck 里多出来的
    extra_disk = [d for d in disk if d not in expected]
    if extra_disk:
        print("\n磁盘上多余（没有对应卡牌类）：", ", ".join(extra_disk))
        ok = False
    if pck is not None:
        extra_pck = [p for p in pck if p not in expected]
        if extra_pck:
            print("pck 里多余（没有对应卡牌类）：", ", ".join(extra_pck))
            ok = False
    else:
        print("\n[注意] 没找到 FrostSpire.pck —— 还没打包，只校验了源码↔磁盘。")

    print("\n结果：", "全部对得上 ✓" if ok else "有对不上的，见上面 <== 标记 ✗")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
