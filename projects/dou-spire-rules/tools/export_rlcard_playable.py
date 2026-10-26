# -*- coding: utf-8 -*-
"""
从 rlcard **真实的判牌器**里导出「给定手牌 → 所有合法出牌」的对应关系。

为什么不用 card_type.json？
  那张表是 rlcard 穷举出的"牌型清单"，只回答"这手牌是什么型"，
  不回答"这手牌现在允许出哪些"。而 `playable_cards_from_hand` 才是
  真正决定"玩家能点哪些牌"的函数 —— 我们就是要跟**它**对齐。

产出：obj/rlcard_playable.tsv
  每行  <手牌> \t <一手合法出牌>
  同一个 <手牌> 会出现多行（它的每一个合法出牌各一行）。

用法（需要 numpy，用系统 Python）：
  D:/software/Python/python.exe tools/export_rlcard_playable.py
"""

import os
import sys
import random

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(HERE)
PROBE = r"D:\work\AI产出\doudizhu-refs\_probe"
sys.path.insert(0, PROBE)

from rlcard.games.doudizhu.judger import DoudizhuJudger          # noqa: E402
from rlcard.games.doudizhu.utils import CARD_RANK_STR, CARD_RANK_STR_INDEX  # noqa: E402

RANKS = [c for c in CARD_RANK_STR if c not in ("B", "R")]
DECK = []
for _ch in CARD_RANK_STR:
    DECK.extend([_ch] * (1 if _ch in ("B", "R") else 4))
assert len(DECK) == 54, len(DECK)


def canon(cards):
    """按 rlcard 的点数序排序并拼成一个串（这就是 rlcard 内部用的牌串格式）。"""
    return "".join(sorted(cards, key=lambda c: CARD_RANK_STR_INDEX[c]))


def deal(rng, pool):
    """从 pool 里抽 17 张（每个点数最多 4 张）。"""
    shuffled = list(pool)
    rng.shuffle(shuffled)
    out, cnt = [], {}
    for c in shuffled:
        if len(out) == 17:
            break
        if cnt.get(c, 0) >= 4:
            continue
        out.append(c)
        cnt[c] = cnt.get(c, 0) + 1
    return canon(out)


def craft(rng):
    """造"有货"的手牌：故意塞进炸弹、双王、多个三张。"""
    pool = list(DECK)
    heavy = rng.sample(RANKS, 3)
    for r in heavy:
        pool.extend([r] * 4)          # 让这几个点数明显扎堆
    return deal(rng, pool)


def build_hands():
    rng = random.Random(20261006)
    hands = []

    # ① 纯随机
    for _ in range(40):
        hands.append(deal(rng, DECK))

    # ② 带炸弹 / 双王的
    for _ in range(20):
        hands.append(craft(rng))

    # ③ 点名要的手牌：专门去踩那几条有争议的边界
    def filler(used, n):
        """补 n 张杂牌，避开 used 里的点数，免得污染实验。"""
        avail = [c for c in DECK if c not in used]
        rng.shuffle(avail)
        picked, cnt = [], {}
        for c in avail:
            if len(picked) == n:
                break
            if cnt.get(c, 0) >= 4:
                continue
            picked.append(c)
            cnt[c] = cnt.get(c, 0) + 1
        return picked

    # 四张 4 + 双王 → 四带二能不能带"双王"？
    hands.append(canon(["4"] * 4 + ["B", "R"] + filler({"4", "B", "R"}, 11)))
    # 四张 4 + 一对 5 → 四带二带一对
    hands.append(canon(["4"] * 4 + ["5"] * 2 + filler({"4", "5"}, 11)))
    # 333444 + 55 → 飞机带单，带牌跟三张"贴边"
    hands.append(canon(["3"] * 3 + ["4"] * 3 + ["5"] * 2 + filler({"3", "4", "5"}, 9)))
    # 333444 + 66 → 飞机带单，带牌不贴边（对照）
    hands.append(canon(["3"] * 3 + ["4"] * 3 + ["6"] * 2 + filler({"3", "4", "6"}, 9)))
    # 333 + 4 → 三带一，带牌跟三张连成顺
    hands.append(canon(["3"] * 3 + ["4"] + filler({"3", "4"}, 13)))
    # 333 + 44 → 三带二
    hands.append(canon(["3"] * 3 + ["4"] * 2 + filler({"3", "4"}, 12)))
    # 222 + 3 → 三带一，三条是 2（例外规则）
    hands.append(canon(["2"] * 3 + ["3"] + filler({"2", "3"}, 13)))
    # 四张 4 + 单 5 + 单 6（干净的四带二）
    hands.append(canon(["4"] * 4 + ["5", "6"] + filler({"4", "5", "6"}, 11)))

    # ——— 下面几副专门踩"炸弹能不能拆进飞机"这条线 ———
    # QQQQ + KKK + 单 3 → 拆炸弹凑飞机带单（应被拒）
    hands.append(canon(["Q"] * 4 + ["K"] * 3 + ["3"] + filler({"Q", "K", "3"}, 9)))
    # QQQQ + KKKK → 两个炸弹冒充飞机（应被拒）
    hands.append(canon(["Q"] * 4 + ["K"] * 4 + filler({"Q", "K"}, 9)))
    # 333444555666 → 4 连飞机；它的 12 张整手同时踩"贴边带 3 张"
    hands.append(canon(["3"] * 3 + ["4"] * 3 + ["5"] * 3 + ["6"] * 3 + filler({"3", "4", "5", "6"}, 5)))
    # 333444555 + 777 → 飞机带单，带牌不贴边（对照，应允许）
    hands.append(canon(["3"] * 3 + ["4"] * 3 + ["5"] * 3 + ["7"] * 3 + filler({"3", "4", "5", "7"}, 5)))
    # 飞机带单的带牌里同时有双王（应被拒）
    hands.append(canon(["3"] * 3 + ["4"] * 3 + ["B", "R"] + filler({"3", "4", "B", "R"}, 9)))
    # 四张 A + 双王 → 四带二带双王（应被拒）
    hands.append(canon(["A"] * 4 + ["B", "R"] + filler({"A", "B", "R"}, 11)))
    # 四个王不存在，跳过

    # 去重
    seen, uniq = set(), []
    for h in hands:
        if h not in seen:
            seen.add(h)
            uniq.append(h)
    return uniq


def main():
    hands = build_hands()
    out_path = os.path.join(PROJECT, "obj", "rlcard_playable.tsv")
    os.makedirs(os.path.dirname(out_path), exist_ok=True)

    total = 0
    lines = []
    for h in hands:
        playable = DoudizhuJudger.playable_cards_from_hand(h)
        for combo in sorted(playable, key=lambda s: (len(s), s)):
            lines.append(f"{h}\t{combo}")
            total += 1

    with open(out_path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines) + "\n")

    print(f"手牌 {len(hands)} 副 → 合法出牌 {total} 条")
    print(f"写出 {out_path}")

    # 顺手把"点名踩边界"的那几副手牌单独打出来，方便人眼核对
    print("\n—— 点名手牌（前 9 副）的合法出牌里有没有那个争议组合 ——")
    probes = [
        ("4444BR", "4444BR"),
        ("33344455", "33344455"),
        ("33344466", "33344466"),
        ("3334", "3334"),
        ("33344", "33344"),
    ]
    for h in hands[-9:]:
        ps = DoudizhuJudger.playable_cards_from_hand(h)
        hits = [f"{name}={('有' if combo in ps else '无')}" for name, combo in probes if all(ch in h for ch in set(combo))]
        if hits:
            print(f"  {h}\n    " + "  ".join(hits))


if __name__ == "__main__":
    main()
