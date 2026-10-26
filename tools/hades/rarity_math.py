"""计算 Hades 1 祝福稀有度的实际概率。

游戏不是"按权重抽一次"，而是按顺序独立掷骰：
  1. 先掷 Legendary，中了就是 Legendary
  2. 没中再掷 Heroic
  3. 没中再掷 Epic
  4. 没中再掷 Rare
  5. 全没中 = Common

所以 P(最终是 Epic) = (1-PL)(1-PH)·PE，不是 PE 本身。

本脚本用游戏里的真实基础值算一遍，并对比"如果按权重抽"会差多少。
"""

import sys

sys.stdout.reconfigure(encoding="utf-8")

# HeroData.lua 里的基础概率（BoonData 段）
BASE = {
    "Rare": 0.10,
    "Epic": 0.05,
    "Legendary": 0.12,
}
# 注意：基础值里没有 HeroicChance，默认 0
BASE_HEROIC = 0.0


def sequential(rare, epic, heroic, legendary):
    """按游戏的判定顺序算最终概率。

    顺序：Legendary → Heroic → Epic → Rare → Common
    """
    pl = legendary
    ph = (1 - pl) * heroic
    pe = (1 - pl) * (1 - heroic) * epic
    pr = (1 - pl) * (1 - heroic) * (1 - epic) * rare
    pc = (1 - pl) * (1 - heroic) * (1 - epic) * (1 - rare)
    return {
        "Legendary": pl,
        "Heroic": ph,
        "Epic": pe,
        "Rare": pr,
        "Common": pc,
    }


def weighted(rare, epic, heroic, legendary):
    """如果按权重抽一次（很多人以为的算法）。"""
    total = rare + epic + heroic + legendary + 1.0  # Common 权重 1
    return {
        "Legendary": legendary / total,
        "Heroic": heroic / total,
        "Epic": epic / total,
        "Rare": rare / total,
        "Common": 1.0 / total,
    }


def show(title, probs):
    print(f"\n{title}")
    print(f"  {'档位':<12}{'概率':>9}")
    for k in ("Legendary", "Heroic", "Epic", "Rare", "Common"):
        v = probs.get(k, 0)
        bar = "█" * int(v * 60)
        print(f"  {k:<12}{v*100:>8.2f}%  {bar}")


def main():
    print("=" * 62)
    print("Hades 1 祝福稀有度 —— 实际概率")
    print("=" * 62)
    print(f"\n基础值（HeroData.lua）：")
    print(f"  RareChance      = {BASE['Rare']:.2f}")
    print(f"  EpicChance      = {BASE['Epic']:.2f}")
    print(f"  LegendaryChance = {BASE['Legendary']:.2f}")
    print(f"  HeroicChance    = {BASE_HEROIC:.2f}  （基础值里没有，默认 0）")

    seq = sequential(BASE["Rare"], BASE["Epic"], BASE_HEROIC, BASE["Legendary"])
    show("按游戏真实算法（顺序独立掷骰）", seq)

    w = weighted(BASE["Rare"], BASE["Epic"], BASE_HEROIC, BASE["Legendary"])
    show("如果按权重抽一次（常见误解）", w)

    print("\n" + "=" * 62)
    print("差异")
    print("=" * 62)
    for k in ("Legendary", "Epic", "Rare", "Common"):
        d = (seq[k] - w[k]) * 100
        print(f"  {k:<12} 顺序掷骰 {seq[k]*100:>6.2f}%  vs  权重抽 {w[k]*100:>6.2f}%"
              f"   差 {d:+.2f} 个百分点")

    print("\n" + "=" * 62)
    print("加上永久升级后的变化（每级 +0.5 倍基础值）")
    print("=" * 62)
    print(f"  {'镜子等级':<10}{'Rare':>8}{'Epic':>8}{'Heroic':>8}{'Legendary':>10}")
    for lvl in range(0, 6):
        boost = lvl * 0.5
        r = BASE["Rare"] + boost * 0.10
        e = BASE["Epic"] + boost * 0.05
        p = sequential(r, e, BASE_HEROIC, BASE["Legendary"])
        print(f"  {lvl:<10}{p['Rare']*100:>7.2f}%{p['Epic']*100:>7.2f}%"
              f"{p['Heroic']*100:>7.2f}%{p['Legendary']*100:>9.2f}%")

    print("\n" + "=" * 62)
    print("关键约束：不是每条祝福都能升档")
    print("=" * 62)
    print("  rarityTable 只装入「该祝福自身支持的档位」。")
    print("  所以 LegendaryChance=12% 只对带 Legendary 档位的祝福生效。\n")
    print(f"  {'档位':<12}{'支持的祝福数':>14}{'占 487 条':>12}")
    support = {
        "Common": 163,
        "Rare": 163,
        "Epic": 163,
        "Heroic": 135,
        "Legendary": 31,
    }
    for k, v in support.items():
        print(f"  {k:<12}{v:>14}{v/487*100:>11.1f}%")

    print("\n  实际抽到 Legendary 的概率：")
    print(f"    先有 {31/487*100:.1f}% 的祝福能升 Legendary")
    print(f"    再乘 12% 的掷骰命中 → {31/487*seq['Legendary']*100:.2f}%")
    print("    也就是说，三选一里出 Legendary 的期望不到 1%。")

    print("\n" + "=" * 62)
    print("一次三选一的真实期望")
    print("=" * 62)
    p_any_legendary = 1 - (1 - (31 / 487) * seq["Legendary"]) ** 3
    print(f"  三个选项里至少一个 Legendary：{p_any_legendary*100:.2f}%")
    print(f"  三个全 Common：{seq['Common']**3*100:.1f}%")
    print(f"  至少一个非 Common：{(1-seq['Common']**3)*100:.1f}%")


if __name__ == "__main__":
    main()
