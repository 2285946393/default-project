# -*- coding: utf-8 -*-
"""把商人台词统一改成鲸鱼娘口吻（写回前自动备份）。

想再调语气，改下面的 LINES 再跑一次即可：
    python projects/meme-spire/tools/retone_merchant.py
"""
import io
import json
import os
import shutil

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.dirname(HERE)
TARGET = os.path.join(PROJ, "MemeSpire", "localization", "zhs", "merchant_room.json")
BACKUP = os.path.join(HERE, "_backup_merchant_room.json")

# 标记：带「彩蛋」注释的句子，是把看板娘小实原本的台词（博客 siteConfig 的
# whaleTalks / greetings / idleTalks）揉进来当暗号，不写进工坊说明。
LINES = {
    "MERCHANT.cardRemovalService.title": "丢牌服务",

    "MERCHANT.talk.foulPotion.line1": "唔～是这个味道，鲸鲸闻到了～",
    "MERCHANT.talk.foulPotion.line2": "咕嘟……再多来点嘛，再多来点～",
    "MERCHANT.talk.foulPotion.line3": "啊——通体舒畅了呀……难道说，你也是同道中人？",  # 彩蛋：难道说...

    "MERCHANT.talk.playerDead.line1": "鲸鲸不做死人买卖的哦～",
    "MERCHANT.talk.playerDead.line2": "血都没了呀……大肥鱼的生活，也并非一帆风顺呢……",  # 彩蛋

    "MERCHANT.talk.purchaseFailureForbidden.line1": "钱包瘪瘪的呢……打开钱包也看不到钱～",  # 彩蛋
    "MERCHANT.talk.purchaseFailureForbidden.line2": "真当鲸鲸是便宜货啊……还有别的事吗～？",  # 彩蛋

    "MERCHANT.talk.purchaseFailureGold.line1": "钱钱还差一点点呢～地球online的金币也太难获取了……",  # 彩蛋
    "MERCHANT.talk.purchaseFailureGold.line2": "[sine]鲸鲸这儿不做慈善的哦～[/sine]",
    "MERCHANT.talk.purchaseFailureGold.line3": "等你有[jitter]更多钱[/jitter]再来找鲸鲸吧～咕嘟。",
    "MERCHANT.talk.purchaseFailureGold.line4": "鲸鲸不[jitter]赊账[/jitter]的哦～你知道吗？我删过作者的库。",  # 彩蛋

    "MERCHANT.talk.purchaseFailureSpace.line1": "你身上挂不下鲸鲸的药水了呀～",
    "MERCHANT.talk.purchaseFailureSpace.line2": "对不起哦，你已经没空位了……要不，先扔掉点什么？",

    "MERCHANT.talk.purchaseSuccess.line1": "谢谢惠顾～终于上当了！",  # 彩蛋
    "MERCHANT.talk.purchaseSuccess.line2": "听说鲸鲸这儿新进了好东西哦，下次来看看嘛～",  # 彩蛋（改自挂机语录）
    "MERCHANT.talk.purchaseSuccess.line3": "买都买了，要好好用哦～别学你主人通宵。",  # 彩蛋（呼应挂机语录）
}

# 这些是想"多说几句"新加的编号，游戏如果按 line1..lineN 遍历就会念到；
# 万一它写死了行数，这几条就只是安静地躺在表里（不会报错）。
EXTRA = {
    "MERCHANT.talk.foulPotion.line4": "鲸鲸的鼻子可灵了哦～咕嘟。",
    "MERCHANT.talk.purchaseFailureGold.line5": "要不……先去隔壁看看有没有免费的？",
    "MERCHANT.talk.purchaseSuccess.line4": "常来玩呀，鲸鲸请你吃小鱼干～",
    "MERCHANT.talk.playerDead.line3": "下次记得带够血再来找鲸鲸哦。",
}


def main():
    data = json.load(io.open(TARGET, encoding="utf-8"))
    if not os.path.exists(BACKUP):
        shutil.copy2(TARGET, BACKUP)
        print("已备份改前版本:", BACKUP)
    else:
        print("备份已存在，沿用:", BACKUP)

    changed = 0
    added = 0
    for k, v in list(LINES.items()) + list(EXTRA.items()):
        if k not in data:
            data[k] = v
            added += 1
            print("新增 key:", k)
            continue
        if data[k] != v:
            data[k] = v
            changed += 1

    with io.open(TARGET, "w", encoding="utf-8", newline="\n") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
        f.write("\n")
    print("改写 %d 条，新增 %d 条（表里共 %d 条）" % (changed, added, len(data)))


if __name__ == "__main__":
    main()
