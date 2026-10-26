# -*- coding: utf-8 -*-
"""
把「弹幕尖塔」社区梗素材，补进梗化尖塔最后剩下的那批**没改过的台词槽**。

背景（2026-10-09 全表体检的结论）：
    梗化尖塔的「可改名面」已经 100% 做完了 —— 卡牌/遗物/怪物/事件/药水/先古…的
    `.title` / `.name` / `.flavor` / `moves.*.title` / `speakLine` / `banter` 全都改过。
    剩下没动的 2600 多条**全是说明类**（`.description` 等），按铁律不能碰。

    真正的空缺只有 **64 条「台词/口吻」槽**（游戏里会"说话"但没被覆盖的地方）。
    这个脚本就是把这 64 条补上。

用法（在 projects/meme-spire 下）：
    python tools/fill_danmaku_memes.py            # 写入
    python tools/fill_danmaku_memes.py --dry      # 只看会改什么，不写

改前自动备份到 tools/_backup_<表>.json。
改完照旧：重打 pck → 部署 → 刷图鉴。
"""

import argparse
import json
import os
import shutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.dirname(HERE)
LOC = os.path.join(PROJ, "MemeSpire", "localization", "zhs")

# ============================================================
# 64 条补丁
# 规矩：只动「说话/名字」类，绝不动 .description 等说明类；
#      {占位符} 与 [] 标签原样保留。
# ============================================================
PATCH = {
    # ---- 纪年（时间线）里漏掉的 8 个"故事名" ----
    # 前 5 个 = 角色，跟 characters 表用同一套社区昵称（红裤衩/猎宝/机宝/骨妹/太子）
    "epochs": {
        "STORY_IRONCLAD": "红裤衩",
        "STORY_SILENT": "猎宝",
        "STORY_DEFECT": "机宝",
        "STORY_NECROBINDER": "骨妹",
        "STORY_REGENT": "太子",
        "STORY_MAGNUM_OPUS": "封神之作",
        "STORY_REOPENING": "重新营业",
        "STORY_TALES_FROM_THE_SPIRE": "尖塔那些事儿",
    },

    # ---- 怪物台词 6 条 ----
    "monsters": {
        # 「不可能！」⇒ 大众梗（常威 / 网络「不可能，绝对不可能」）
        "KIN_PRIEST.followersDeathLine": "不可能！绝对不可能！",
        # 「我说 X」= 社区句式（弹幕尖塔 E1/E3/E4 都在用）
        "KNOWLEDGE_DEMON.moves.CURSE_OF_KNOWLEDGE.startLine": "我说，做出选择。",
        # 大众/网络梗
        "KNOWLEDGE_DEMON.moves.CURSE_OF_KNOWLEDGE.doneLine": "已经结束咧。",
        # 圈内梗：血条 = 「红色格挡条」，「用脸接」= 不防硬吃
        "MECHA_KNIGHT.SHIELDS_DOWN.dialogue": "护盾掉了！用脸接！",
        "MECHA_KNIGHT.SHIELDS_UP.dialogue": "护盾续费成功！",
        "QUEEN.banter": "[jitter]你逃不掉了！[/jitter]",
    },

    # ---- 状态「抑制」的台词 ----
    # 抑制 = 机甲骑士在场时你的牌全被降级，所以原句是"和你的升级说再见吧"。
    # 「敲牌」= 社区对"升级卡牌"的固定叫法（弹幕尖塔 G7 / §3 对照表）
    "powers": {
        "DAMPEN_POWER.banter": "你的牌，白敲了！",
    },

    # ---- 附魔「腐化」的台词 ----
    "enchantments": {
        "CORRUPTED.dialogue": "力量……给我力量……",
    },

    # ---- 操作提示里漏掉的一条（其余 8 条早就改过了）----
    "combat_messages": {
        "BLOCKED_BY_HOOK": "[red]{BlockingHook}[/red]不让。",
    },

    # ---- 事件：水晶球占卜（神棍腔，13 条）----
    "events": {
        "CRYSTAL_SPHERE.banter.START.1": "来都来了，看看你这条命怎么走吧……",
        "CRYSTAL_SPHERE.banter.START.2": "盯着球看，别慌，慌了也算不出来……",
        "CRYSTAL_SPHERE.banter.REVEAL_GOOD.1": "[sine]上上签……[/sine]",
        "CRYSTAL_SPHERE.banter.REVEAL_GOOD.2":
            "你[rainbow freq=0.3 sat=0.8 val=1]这波要起飞啊[/rainbow]！",
        "CRYSTAL_SPHERE.banter.REVEAL_GOOD.3": "祖坟冒青烟了！",
        "CRYSTAL_SPHERE.banter.REVEAL_GOOD.4": "我说对了吧…[jitter]我说对了吧[/jitter]！！",
        "CRYSTAL_SPHERE.banter.REVEAL_GOOD.5": "[sine]好耶~~好耶…好耶~~！！[/sine]",
        "CRYSTAL_SPHERE.banter.REVEAL_BAD.1": "[jitter]完~了[/jitter]!!",
        "CRYSTAL_SPHERE.banter.REVEAL_BAD.2": "塞翁失马，焉知非福……骗你的。",
        "CRYSTAL_SPHERE.banter.REVEAL_BAD.3": "别！[jitter]别~~~[/jitter]！！！",
        "CRYSTAL_SPHERE.banter.END.1": "今日的命算完了，散了吧……",
        "CRYSTAL_SPHERE.banter.END.2": "水晶球也得充会儿电……",
        "CRYSTAL_SPHERE.banter.END.3": "你已经…走在你[jitter]命定[/jitter]的那条道上了！！",

        # ---- 事件：假商人（油滑骗子，34 条）----
        "FAKE_MERCHANT.talk.welcome.line1": "欢迎欢迎！一看就是老玩家了！",
        "FAKE_MERCHANT.talk.welcome.line2": "遗物！遗物！走过路过不要错过！",
        "FAKE_MERCHANT.talk.welcome.line3": "地毯上的统统五折，手慢无！",
        "FAKE_MERCHANT.talk.welcome.line4": "清塔大甩卖！最后一天！明天还来！",

        "FAKE_MERCHANT.talk.openInventory.line1":
            "2697年那届商人好评榜，我可是断层第一，知道不！",
        "FAKE_MERCHANT.talk.openInventory.line2": "我说，我时间紧任务重，孩子，速战速决。",
        "FAKE_MERCHANT.talk.openInventory.line3":
            "你……没在附近见过什么哨卫吧？不不，随口一问，诶嘿……",
        "FAKE_MERCHANT.talk.openInventory.line4": "我说，这一行可不是谁都能干的。",
        "FAKE_MERCHANT.talk.openInventory.line5": "一个人爬塔太危险了！……我就那么一说。",
        "FAKE_MERCHANT.talk.openInventory.line6": "嘿！别把血蹭我货上！",
        "FAKE_MERCHANT.talk.openInventory.line7": "……我的手？有什么问题吗？",
        "FAKE_MERCHANT.talk.openInventory.line8": "无意冒犯，但你这头发该理了，伙计。",

        "FAKE_MERCHANT.talk.playerDead.line1": "你这状态，不太妙啊伙计。",
        "FAKE_MERCHANT.talk.playerDead.line2": "没血，不做生意。",
        "FAKE_MERCHANT.talk.playerDead.line3": "我是做生意的，不是开医院的！",

        "FAKE_MERCHANT.talk.purchaseFailureForbidden.line1": "我很乐意给你，可钱呢……",
        "FAKE_MERCHANT.talk.purchaseFailureForbidden.line2": "别逗我了，伙计。",
        "FAKE_MERCHANT.talk.purchaseFailureForbidden.line3": "有事吗？",

        "FAKE_MERCHANT.talk.purchaseFailureGold.line1": "我这儿不是搞慈善的。",
        "FAKE_MERCHANT.talk.purchaseFailureGold.line2": "嘿，我可是正经生意人。",
        "FAKE_MERCHANT.talk.purchaseFailureGold.line3": "想都别想，伙计。",

        "FAKE_MERCHANT.talk.purchaseFailureSpace.line1": "怎么，还想让我替你扛着？",
        "FAKE_MERCHANT.talk.purchaseFailureSpace.line2": "[b]我自己[/b]都嫌破烂多，你还往这儿塞？",
        "FAKE_MERCHANT.talk.purchaseFailureSpace.line3": "这波是真没戏，伙计。",

        "FAKE_MERCHANT.talk.purchaseSuccess.line1": "有眼光！诶嘿……钱呢？",
        "FAKE_MERCHANT.talk.purchaseSuccess.line2": "一分钱一分货，基本上是。",
        "FAKE_MERCHANT.talk.purchaseSuccess.line3":
            "哎呀，这个我本来不想出手的，但既然是您要嘛，诶嘿……",
        "FAKE_MERCHANT.talk.purchaseSuccess.line4": "成交，合作愉快哈。",
        "FAKE_MERCHANT.talk.purchaseSuccess.line5": "成交！概不退换！",
        "FAKE_MERCHANT.talk.purchaseSuccess.line6": "你很会买，孩子。",

        "FAKE_MERCHANT.talk.foulPotion.line1": "搞什——？！你闻出来了？",
        "FAKE_MERCHANT.talk.foulPotion.line2": "嚯，所以是想这么玩，是吗？！",
        "FAKE_MERCHANT.talk.foulPotion.line3": "惹错人了，小子。",
        "FAKE_MERCHANT.talk.foulPotion.line4": "岂有此理……行，你等着。",
    },
}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry", action="store_true", help="只打印，不写入")
    args = ap.parse_args()

    total = 0
    for table, patch in PATCH.items():
        path = os.path.join(LOC, table + ".json")
        if not os.path.exists(path):
            print(f"!! 找不到 {path}", file=sys.stderr)
            continue
        with open(path, encoding="utf-8") as f:
            data = json.load(f)

        changed = []
        for k, v in patch.items():
            old = data.get(k)
            if old == v:
                continue
            data[k] = v
            changed.append((k, old, v))

        if not changed:
            print(f"{table}: 无需改动")
            continue

        if not args.dry:
            shutil.copy2(path, os.path.join(HERE, f"_backup_{table}.json"))
            with open(path, "w", encoding="utf-8", newline="\n") as f:
                json.dump(dict(sorted(data.items())), f, ensure_ascii=False, indent=2)
                f.write("\n")

        print(f"{table}: {len(changed)} 条 {'(dry-run)' if args.dry else '已写入'}")
        for k, old, new in changed:
            print(f"    {k}")
            print(f"      旧: {old}")
            print(f"      新: {new}")
        total += len(changed)

    print(f"\n合计 {total} 条")


if __name__ == "__main__":
    main()
