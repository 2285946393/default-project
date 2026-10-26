namespace QuestSpire.Quests;

using MegaCrit.Sts2.Core.Models.Characters;

// 所有可接任务的目录。想加新任务，往 All 里加一行即可。
// 接取时是随机抽取其中几个（见 PickOffers），不会全列出。
public static class QuestCatalog
{
    public static readonly QuestDef[] All =
    [
        // —— 累计计数类 ——
        new(QuestKind.DamageDealt,      "见血封喉", "本局累计造成 1200 点伤害",       1200, 120, "奖励：一张「恶魔形态」") { Reward = RewardKind.CardNamed, RewardCard = "DEMON_FORM" },
        new(QuestKind.BlockGained,      "铁壁防御", "本局累计获得 1000 点格挡",       1000, 110, "奖励：一张「壁垒」") { Reward = RewardKind.CardNamed, RewardCard = "BARRICADE" },
        new(QuestKind.AttacksPlayed,    "刀剑连鸣", "本局打出 90 张攻击牌",           90,  90) { Reward = RewardKind.CardSkill },
        new(QuestKind.CardsPlayed,      "行云流水", "本局累计打出 200 张牌",           200,  100, "奖励：一张「机器学习」") { Reward = RewardKind.CardNamed, RewardCard = "MACHINE_LEARNING" },
        new(QuestKind.CardsExhausted,   "破釜沉舟", "本局消耗 40 张牌",               40,  80, "奖励：一张「腐化」") { Reward = RewardKind.CardNamed, RewardCard = "CORRUPTION" },
        new(QuestKind.TurnsSurvived,    "稳如磐石", "本局存活 60 个回合",             60,  90) { Reward = RewardKind.Relic },
        new(QuestKind.PotionsUsed,      "药剂大师", "本局使用 6 瓶药水",               6,  60) { Reward = RewardKind.CardAny },
        new(QuestKind.CombatsWon,       "凯旋之师", "本局赢得 18 场战斗",               18,  120) { Reward = RewardKind.Relic },
        new(QuestKind.EliteWins,        "精英猎手", "赢下 3 场精英战",                   3,  120) { Reward = RewardKind.Relic },
        new(QuestKind.BossWins,         "屠龙者",   "赢下 2 场首领战",                   2,  180) { Reward = RewardKind.Relic },
        new(QuestKind.EmptyHands,       "空手出招", "累计 15 次把整只手牌全部打出",     15,  60) { Reward = RewardKind.CardSkill },
        new(QuestKind.IdleTurns,        "蓄势待发", "累计 12 个回合不出任何牌",         12,  60) { Reward = RewardKind.CardPower },
        new(QuestKind.SelfDamage,       "硬抗到底", "累计承受 800 点伤害", 800, 100, "奖励：一张「主宰」") { Reward = RewardKind.CardNamed, RewardCard = "JUGGERNAUT" },
        new(QuestKind.FlawlessCombats,  "毫发无伤", "5 场战斗全程不掉血取胜",           5,  110) { Reward = RewardKind.CardRare },
        new(QuestKind.CloseCalls,       "命悬一线", "5 次以低于 30% 血量赢下战斗",      5,  100) { Reward = RewardKind.CardPower },
        new(QuestKind.CampfireSmiths,   "淬火修行", "在篝火升级 4 张牌",               4,  60) { Reward = RewardKind.CardSkill },
        new(QuestKind.RestSites,        "篝火夜话", "在篝火休息 4 次",                 4,  50) { Reward = RewardKind.CardAny },
        new(QuestKind.ShopPurchases,    "挥金如土", "在商店购买 10 件商品",             10,  90) { Reward = RewardKind.CardOtherClass },
        new(QuestKind.FreeCardsPlayed,  "白刃相见", "打出 25 张 0 费牌",                25,  70) { Reward = RewardKind.CardRare },
        new(QuestKind.HeavyCardsPlayed, "重拳出击", "打出 25 张费用≥2 的牌",            25,  70) { Reward = RewardKind.CardSkill },
        new(QuestKind.RareCardsPlayed,  "奇珍异宝", "打出 10 张稀有牌",                 10,  80, "奖励：一张「未掘宝石」") { Reward = RewardKind.CardNamed, RewardCard = "HIDDEN_GEM" },
        new(QuestKind.PowersPlayed,     "十八般武艺", "打出 12 张能力牌",              12, 80, "奖励：一张「创造AI」") { Reward = RewardKind.CardNamed, RewardCard = "CREATIVE_AI" },
        // —— 单回合内计数（每回合清零）——
        new(QuestKind.AttacksInOneTurn, "狂暴连击", "单回合内打出 6 张攻击牌",         6,  50) { ScaleWithPlayers = true, Reward = RewardKind.CardPower },
        // —— 快照类 ——
        new(QuestKind.GoldHoard,        "守财之路", "持有金币达到 400",                400, 80),
        new(QuestKind.RoomTypesSeen,    "采风向导", "进入 6 种不同类型的房间",         6,  45) { Reward = RewardKind.CardOtherClass },
        // —— 2026-09-22 新增（一代池子的「能做没做」）——
        new(QuestKind.ComboCosts,            "三费齐鸣", "一回合内打出 1 费、2 费、3 费各一张",        1,  40) { Reward = RewardKind.Enchant },
        new(QuestKind.CommonsOnlyWins,       "朴实无华", "不打出罕见或稀有牌，赢下精英或首领战",      1,  80),
        new(QuestKind.OrbsInOneTurn,         "球球交响", "单回合激发 4 种不同的充能球",              1,  60,
            null, false, typeof(Defect)),   // 只有故障机器人有充能球
        new(QuestKind.DeckOrganized,         "整整齐齐", "卡组恰好 10 攻击 / 10 技能 / 5 能力",       1,  90) { Reward = RewardKind.Enchant },
        new(QuestKind.EliteFirstTurnIdle,    "礼让一手", "精英战第一回合一张牌都不出",              1,  40),
        new(QuestKind.ExactKills,            "精准收割", "用恰到好处的伤害击杀 3 个敌人",            3,  70) { Reward = RewardKind.Enchant },
        new(QuestKind.EventRooms,            "问号猎手", "访问 5 个 ? 房间",                        5,  55) { Reward = RewardKind.Enchant },
        new(QuestKind.DeckPeasant,           "贫下中农", "卡组里普通牌比罕见+稀有牌多 7 张以上",      1,  70),
        new(QuestKind.WindowShopping,        "只看不买", "3 次进商店一分钱不花就离开",              3,  55),
        new(QuestKind.DeckEvenOnly,          "偶数为王", "卡组有 7 张以上偶数/0 费牌且没有奇数费牌",  1,  80),
        new(QuestKind.FastCombats,           "闪电战",   "4 回合内结束 5 场战斗",                    5,  90),
        new(QuestKind.ClawPlays,             "爪爪爪",   "打出 30 张 Claw",                         30,  60,
            null, false, typeof(Defect)),   // Claw 是故障机器人的牌
        new(QuestKind.FullHpAtBoss,          "满血见王", "满血状态下走进首领房",                    1,  50),
        new(QuestKind.UnchangedMaxHpBossWin, "原装出厂", "没改过生命上限，赢下二幕首领",            1,  80),
        // —— 2026-09-22 夜：梗任务三连（奖励不是金币）——
        // 最后那个 true = EarlyBird（前置型），开局选任务时会优先抽到它们。
        new(QuestKind.CaptainsLegacy,    "船长的执念", "本局集齐锚、角夹、船长之轮三件船用遗物",   3,   0,
            "奖励：把三件零件拼成一艘帆船", true),
        new(QuestKind.CardsExhaustedBig, "枯枝还魂",   "单场战斗里消耗 10 张以上牌",              10,  0,
            "奖励：遗物「枯木树枝」", true),
        new(QuestKind.ShopXDeal, "展望未来", "带着 250 金币进商店", 1, 0,
            "奖励：遗物化学X + 一张任意职业的 X 费牌", true),
        // —— 2026-09-22 深夜二批：一代遗物任务 ——
        // ⚠️ 设计原则：这几件都是「拿到之后才生效」的前置型遗物，条件必须够早、
        //    而且必须开局就摆出来，否则到手时局已经快结束，等于白给。
        new(QuestKind.BlockGainedBig, "小伤不理",     "本局累计获得 350 点格挡",                 350, 0,
            "奖励：遗物「鸟居」", true),
        new(QuestKind.SmithNoRest,    "今晚不睡",     "在篝火升级 4 张牌，而且一次都没休息过",   1,   0,
            "奖励：遗物「咖啡滤杯」", true),
        new(QuestKind.PotionsUsedBig, "药罐子",       "本局使用 4 瓶药水",                       4,   0,
            "奖励：遗物「玩具扑翼飞机」", true),
        new(QuestKind.GoldHoardBig,   "见钱眼开",     "本局攒到 250 金币",                       250, 0,
            // 数值依据（反编译 EncounterModel / CharacterModel）：开局 99 金；普通怪 10~20、
            // 精英 35~45、首领 100；A3「贫困」起全部 ×0.75。
            // 250 差不多等于「第一幕一场不落地打，而且什么都不买」。
            "奖励：遗物「金偶像」", true),
        new(QuestKind.TreasureRooms,  "一个都不放过", "在第一幕打开宝箱房，并赢下一场精英战",      1,   0,
            // 原来只要求「开第一个宝箱房」= 白送（每幕都有宝箱房）。
            // 套娃是前置型（管后面 2 个宝箱），条件必须在第一幕内完成，
            // 所以改成「第一幕的宝箱房 + 第一幕的精英」——都做到才算，且仍然够早。
            "奖励：遗物「套娃」", true),
        new(QuestKind.CurseFreeCombats, "一身清白",   "牌组里最多 1 张诅咒，赢下 5 场战斗",       5,   0,
            // ⚠️ 不能写「一张诅咒都没有」：A5 起会白送一张「进阶之灾」，它带 Eternal 关键词，
            //    从根本上无法移除（反编译 AscendersBane 确认）→ 高进阶下永远做不完。
            "奖励：遗物「御守」", true),
        // —— 2026-09-22 深夜五批：鸡煲三部曲（故障机器人专属）——
        new(QuestKind.SlowStartWin,   "还在启动",     "前 3 个回合一张牌都不出，还把这场战斗打赢", 1,   0,
            "奖励：遗物「机械臂」", true, typeof(Defect)),
        new(QuestKind.EchoFormTurn,   "回响形态",     "单回合内把同一张牌打出两次",               1,   0,
            "奖励：遗物「齿轮工艺品」", true, typeof(Defect)),
        new(QuestKind.ActOneEliteWin, "严父在上",     "在第一幕赢下一场精英战",                   1,   0,
            "奖励：遗物「突变酵素」", false, typeof(Defect)),
        // —— 2026-09-22 深夜六批：借《海克斯符文》的两个手法 ——
        new(QuestKind.CardsPlayedBig, "熟能生巧", "本局打出 150 张牌",                         150,  0,
            "奖励：随机升级牌组里的 2 张牌"),
        new(QuestKind.CloseCallsBig,  "玩命",     "3 次以低于 30% 的血量赢下战斗",            3,   0,
            "奖励：莽夫之道", true),
        // —— 2026-09-22 深夜七批：每个奖励对着一个不同的游戏旋钮 ——
        new(QuestKind.CardsDrawn,       "手不释卷", "本局抽 150 张牌",                       150, 0,
            "奖励：每回合多抽 1 张牌", true),
        new(QuestKind.GoldSpent,        "砍价高手", "本局在商店累计花掉 600 金币",            600, 0,
            "奖励：商店价格 -30%", true),
        new(QuestKind.RestSitesBig,     "养生",     "本局在篝火休息 5 次",                     5,   0,
            "奖励：篝火休息的回复量 +50%"),
        new(QuestKind.FlawlessCombatsBig, "稳如老狗", "6 场战斗全程不掉血取胜",               6,   0,
            "奖励：能量上限 +1"),
        new(QuestKind.PowersPlayedBig,  "厚积薄发", "本局打出 15 张能力牌",                   15,  0,
            "奖励：你施加的增益与减益都 +1 层", true),
        new(QuestKind.SelfDamageBig,    "铁壁",     "本局累计承受 1500 点伤害",               1500, 0,
            "奖励：你单次受到的伤害不超过 15 点"),
        new(QuestKind.AttacksPlayedBig, "连环拳",   "本局打出 100 张攻击牌",                   100,  0,
            "奖励：你的攻击牌多打一段"),
        // —— 2026-09-22 深夜八批：B 站梗（摄政王专属）——
        new(QuestKind.CosmicIndifferencePlayed, "宇！宙！冷！漠！", "牌组里有 3 张或以上的「宇宙冷漠」",      3,   0,
            "奖励：每当你获得格挡，额外获得 3 点格挡", true, typeof(Regent)),
        // —— 2026-09-25：玩家点名的（战士专属）——
        new(QuestKind.StrikeDeckHellraiser, "地狱狂徒", "牌组里有 9 张或以上带「打击」标签的牌",  9,   0,
            "奖励：获得一张「地狱狂徒」", true, typeof(Ironclad)),
        // —— 2026-09-24：跨界联动（题目是别的游戏，条件必须和奖励相关）——
        new(QuestKind.ParryFlawless, "只狼：弹反", "一场战斗里不掉血、格挡也没被打穿，还把这场打赢", 1, 0,
            "奖励：选一张牌升级", true),
        new(QuestKind.MinecraftDig, "我的世界：挖矿", "一局里打开 1 个宝箱房", 1, 0,
            "奖励：随机一件遗物", true),
        new(QuestKind.PokemonEvolve, "宝可梦：进化", "本局打出 30 张「打击」或「防御」", 30, 0,
            "奖励：把一张牌变成另一张", true),
        new(QuestKind.WoWEnchant, "魔兽世界：附魔", "一场战斗里消耗 6 张牌", 6, 0,
            "奖励：给一张牌挂「保留」词条", true),
        new(QuestKind.DeckNoDuplicates, "我的世界：纯净卡组", "牌组 15 张以上，且「打击 / 防御」合计不超过 2 张", 1, 0,
            "奖励：把一张牌变成另一张", true),
        new(QuestKind.OnlyOneCostWin, "文明6：精打细算", "一场战斗里只打出 1 费牌，还打赢", 1, 0,
            "奖励：金币 +250", true),
        new(QuestKind.TerrariaShieldWin, "泰拉瑞亚：以盾取胜", "单场战斗获得 100 点以上格挡、自己一滴血不掉，还打赢", 1, 0,
            "奖励：给一张牌附上「保留」", true),
        new(QuestKind.DontStarveEndure, "饥荒：硬扛过冬", "一场战斗里自己挨 40 点伤害，还打赢", 1, 0,
            "奖励：金币 +200", true),
        new(QuestKind.DiabloFarming, "暗黑破坏神：刷装备", "一场战斗里打出 3 张稀有牌，还打赢", 1, 0,
            "奖励：随机一件遗物", true),
        new(QuestKind.GenshinWish, "原神：三分祈愿", "一场战斗里打出普通 4 张、罕见 2 张、稀有 1 张，还打赢", 1, 0,
            "奖励：金币 +150", true),
        new(QuestKind.ArknightsDeploy, "明日方舟：部署", "一场战斗里打出 4 张能力牌，还打赢", 1, 0,
            "奖励：给一张牌附上「固有」", true),
        new(QuestKind.PokemonAllEvolve, "宝可梦：全队进化", "牌组里有 10 张以上升级过的牌", 1, 0,
            "奖励：再选 2 张牌升级", true),
        // —— 2026-09-23 批：《尖塔梗百科》里的梗（B 站 UP：TowerHeart22，合集 178 万播放）——
        // 标题基本照搬那几期的说法，机制尽量贴原意。每条的钩子都在交接文档里记了。
        new(QuestKind.CardsDrawnInOneTurn, "我说过牌有没有懂的", "一回合内抽 15 张牌",          15,  70) { ScaleWithPlayers = true },
        new(QuestKind.NoSkillCombatWin,    "我说别带",           "一场战斗里一张技能牌都不打，还打赢", 1,   80),
        new(QuestKind.BigPurchase,         "152 金币",          "在商店一口气花掉 150 金币以上",     1,   70),
        new(QuestKind.OrbsChanneled,       "通电",              "本局生成 30 个充能球",             30,   50,
            null, false, typeof(Defect)),   // 「别人在过牌输出叠甲，鸡煲在通电」
        new(QuestKind.DebuffLayersGiven,   "蛇花圣经",          "给敌人累计叠 20 层减益",          20,  0,
            "奖励：你给敌人施加的减益 ×1.5", true),
        new(QuestKind.FlameBarrierDamage,  "火焰屏障输出特别高",  "靠火焰屏障的反弹打出 40 点伤害",    40,  70),
        new(QuestKind.FocusLost,           "掉集中",            "本局累计失去 10 点集中",          10,  60,
            null, false, typeof(Defect)),   // 「偏差认知」每回合掉集中，玩家说话越来越短
        // —— 2026-09-23 批之二：把剩下几个能做的梗也做掉 ——
        new(QuestKind.SnakebitePlays,         "蛇咬",       "打出 3 张「蛇咬」",                  3,  70),
        new(QuestKind.CardRemovals,           "删内切",     "在商店花钱删掉 2 张牌",              2,  60),
        new(QuestKind.CardsGeneratedInCombat, "复制防御",   "一场战斗内生成 5 张牌",              5,  70),
        new(QuestKind.BloodiedWin,            "战个未来",   "一场战斗里先被打到半血以下，最后还赢", 1,  80),
        new(QuestKind.FakeRelicsOwned,        "假货我也要", "同时拥有 2 件假货",                  2,  90),
        new(QuestKind.RealAndFakeAnchor,      "真的假的",   "同时拥有真锚和假锚「锚???」",         1, 100),
        // —— 2026-09-23 批之三 ——
        new(QuestKind.FakeMerchantWin,   "打赢假商人", "击败假商人",                       1,   0,
            "奖励：一件随机遗物", false),
        new(QuestKind.DamageInOneTurn,   "数值轮椅",   "单回合造成 60 点伤害",              60,  80) { ScaleWithPlayers = true },
        new(QuestKind.DefectEliteWins,   "你玩的鸡煲啊", "本局用故障机器人赢下 2 场精英战",    2,  90,
            null, false, typeof(Defect)),
        new(QuestKind.SilentCardsInTurn, "劣人手速",   "单回合打出 12 张牌",               12,  90,
            null, false, typeof(Silent)) { ScaleWithPlayers = true },   // 「劣人」= 社区给静默猎手起的外号
        // —— 2026-09-23 批之四 ——
        new(QuestKind.TankedCombatWin,      "战士鸽",   "一场战斗里自己挨 20 点伤害，还赢",    20,  90,
            null, false, typeof(Ironclad)),   // 「战士鸽」= 社区给铁甲战士起的外号
        new(QuestKind.CardsInOneTurnBig,    "无限",     "单回合打出 15 张牌",                15, 100) { ScaleWithPlayers = true },
        new(QuestKind.EnergySpentInOneTurn, "我说有费有没有懂的", "单回合花掉 6 点能量",      6,  70) { ScaleWithPlayers = true },
        // —— 2026-09-23 批之五：梗含义是从 B 站那几期的最高赞评论里读出来的 ——
        new(QuestKind.GreedyShopVisit, "基米精神", "带着 200 金币进商店，一分钱不花就走",      1, 120),
        new(QuestKind.UpgradedDeck,    "东尼意思", "卡组里有 8 张升级过的牌",                 8,   0,
            "奖励：卡牌奖励更容易出现升级版", true),
        // —— 2026-09-23 批之六：纯乐子（B 站现搜的玩笑 / 名场面）——
        new(QuestKind.HellCrawlWin,  "我从地狱蠕动出来了", "以 1 点生命赢下一场战斗",           1, 150),
        new(QuestKind.MarriageBoss,  "谁最适合结婚",     "满血赢下一场首领战",                 1, 120),
        new(QuestKind.GuguGaga,      "咕咕嘎嘎",         "一场战斗里一张牌都不打，还赢了",      1, 100),
        new(QuestKind.OnePunch,      "一拳超人",         "单次造成 100 点以上伤害",           100, 100,
            null, false, null) { ScaleWithPlayers = true },
        new(QuestKind.IronShirt,     "铁布衫",           "单次获得 50 点以上格挡",             50,  80,
            null, false, null) { ScaleWithPlayers = true },
        new(QuestKind.TripleKill,    "三杀",             "单回合击杀 3 个敌人",                1, 100,
            null, false, null) { ScaleWithPlayers = true },
        new(QuestKind.Repeater,      "复读机",           "单回合把同一张牌打出 3 次",          1,  80),
        new(QuestKind.SelfDestruct,  "自暴自弃",         "卡组里攒到 5 张以上诅咒",            5, 120),
        new(QuestKind.GuardBroken,   "破防",             "单场战斗里格挡被打穿 5 次",          5,  80),
        new(QuestKind.FullHand,      "满手牌",           "单回合手牌达到 10 张",              10,  70),
        new(QuestKind.ThunderDefect, "雷霆大机煲",       "单场战斗激发 6 个充能球",            6,  80,
            null, false, typeof(Defect)),
        new(QuestKind.GaRenMen,      "噶人焖",           "累计获得 30 点辉星",                30,  90,
            null, false, typeof(Regent)),   // 「噶人焖」= 农神的口头禅
        // —— 2026-09-23 批之七：行话 / 主播梗 / 角色外号（第二轮搜索）——
        new(QuestKind.BashPlays,      "肘击",     "打出 10 张「痛击」",                10,  90,
            null, false, typeof(Ironclad)),
        new(QuestKind.DefectBossWins, "小蓝人",   "用故障机器人赢下 2 场首领战",         2, 130,
            null, false, typeof(Defect)),   // 社区提到机器人就说「提一嘴小蓝人」
        new(QuestKind.OstySummons,    "骨小妹",   "累计召唤 5 次",                     5, 100,
            null, false, typeof(Necrobinder)),   // Osty 就是亡灵绑定者那只小骷髅
        new(QuestKind.CardsDiscarded, "我说弃牌有没有懂的", "累计弃掉 30 张牌",         30,  90,
            null, false, typeof(Silent)),
        new(QuestKind.LongCombatWin,  "太牢了",   "一场战斗打到 10 回合还赢",            1, 100),
        // —— 2026-09-23 批之八：杂耍（反编译查实的机制）——
        new(QuestKind.JugglingTricks, "杂耍",     "带着「杂耍」一回合打满 3 张攻击牌，累计 3 次", 3, 100),
        // —— 2026-09-23 批之九：把「尖塔梗百科」剩下的期数补齐 ——
        new(QuestKind.JackpotStreak,   "农种",     "连续 3 场战斗不掉血",                1, 100),
        new(QuestKind.NeverSurrender,  "绝不认输", "以低于 10% 生命赢下 2 场战斗",        2, 120),
        new(QuestKind.WatcherBurst,    "观者？！", "单回合造成 300 点伤害",            300, 120) { ScaleWithPlayers = true },
        new(QuestKind.TwoCardWin,      "你知道是谁", "只用 2 种牌赢下一场战斗",           1, 150),
        // —— 2026-09-23 批之十：流派线（buff 类型都反编译核实过）——
        new(QuestKind.StrengthGained,  "健身教练",   "累计给自己叠 15 层力量",            15, 100),
        new(QuestKind.PoisonGiven,     "我说叠毒有没有懂的", "给敌人累计叠 25 层中毒",    25, 110, "奖励：一张「剧毒烟雾」") { Reward = RewardKind.CardNamed, RewardCard = "NOXIOUS_FUMES" },
        new(QuestKind.DexterityGained, "身法",       "累计给自己叠 8 层敏捷",             8,  90),
        new(QuestKind.VulnerableGiven, "破甲",       "给敌人累计叠 15 层易伤",            15,  90),
        new(QuestKind.TurtleBlock,     "龟壳",       "单场战斗里获得 200 点格挡",        200, 110),
        // —— 2026-10-01 批：跨游戏「收集 / 成长」维度（都是没被用过的数据）——
        new(QuestKind.RelicsHeld,  "以撒的结合：道具大亨", "同时持有 12 件遗物",          12, 120),
        new(QuestKind.PotionsHeld, "星露谷物语：背包",     "把药水栏装满（2 瓶）",          2,  90),
        new(QuestKind.DeckSizeBig, "俄罗斯方块：堆高",     "卡组攒到 40 张牌",            40, 100),
        new(QuestKind.MaxHpGained, "黑神话：悟空·修炼",   "生命上限比开局多 25 点",      25, 110),
        // —— 风险换回报：接下契约后敌人变强，撑过去给强力遗物 ——
        new(QuestKind.ContractChallenge, "血之契约", "接下后，每场战斗敌人开场随机获得一个增益；打赢 3 场", 3, 0,
            "奖励：一件强力随机遗物 + 150 金币", true),
        // —— 盛碗虫 + 单回合 / 条件类（2026-10-01 批之二）——
        new(QuestKind.BowlbugSwarm,     "盛碗虫泛滥",          "接下后，每场战斗会多出现一只盛碗虫；打赢 3 场", 3, 0,
            "奖励：随身物「盛碗虫常数」（获得格挡时额外 +1 —— 正好把 15 补成 16）", true),
        new(QuestKind.BlockInOneTurn,   "我说叠甲有没有懂的",   "单回合获得 40 点格挡",          1,  90),
        new(QuestKind.SkillsInOneTurn,  "花拳绣腿",            "单回合打出 8 张技能牌",          8,  80),
        new(QuestKind.GoldZeroWin,      "两袖清风",            "身上 0 金币时赢下一场战斗",      1,  80),
        new(QuestKind.DoomKills,        "骨头人：灾厄",          "用「灾厄」击杀 3 个敌人",         3, 100,
            null, false, typeof(Necrobinder)),
        // 难度刻意拉高（奖励是稀有卡「未掘宝石」）：单回合把同一张牌打 8 次
        new(QuestKind.SameCardManyTimes, "堆叠宝石", "单回合把同一张牌打出 8 次", 8, 0,
            "奖励：一张「未掘宝石」（给一张牌叠上「重放」）"),
        // —— 往战斗里加怪 / 加精英（奖励是遗物，不是金币）——
        new(QuestKind.MonsterRush, "怪潮", "接下后，每场战斗会多出 2 只怪；打赢 3 场", 3, 0,
            "奖励：随机一件遗物", true),
        new(QuestKind.EliteAmbush, "精英加餐", "接下后，每场战斗会多出一只精英；打赢 2 场", 2, 0,
            "奖励：随机一件遗物 + 150 金币", true),
        // —— 接取时改牌组（2026-10-01 批之四）——
        new(QuestKind.CursedBargain, "诅咒交易", "接取时随机塞 5 张诅咒；打赢 3 场后清除全部诅咒", 3, 0,
            "奖励：500 金币 + 清除牌组里所有诅咒"),
        new(QuestKind.DeckOverhaul, "大改造", "接取时：打击→飞溅、防御→发现（都是升级版）；每场战斗多一只精英；打赢 3 场", 3, 0,
            "奖励：随机一件遗物", true),
        // —— 用没碰过的钩子做的（2026-10-01 批之五）——
        new(QuestKind.FlushCount,      "轮回",                 "本局弃牌堆洗回抽牌堆 8 次",     8,  0) { Reward = RewardKind.CardPower },
        new(QuestKind.PotionDiscarded, "我说丢药有没有懂的",    "累计丢弃 6 瓶药水",             6,  0) { Reward = RewardKind.CardAny },
        new(QuestKind.EnergyTotal,     "能量黑洞",             "本局累计花掉 120 点能量",       120, 0) { Reward = RewardKind.CardRare },
        new(QuestKind.OstyRevive,      "骨小妹·不死",          "Osty 复活 3 次",                3,  0,
            "奖励：随机一件遗物", false, typeof(Necrobinder)),
        // —— 接着挖没用过的钩子（2026-10-01 批之六）——
        new(QuestKind.PileMoved,      "牌堆搬运工",         "累计让牌在牌堆之间移动 300 次", 300, 0) { Reward = RewardKind.CardAny },
        new(QuestKind.PotionProcured, "我说给药有没有懂的", "累计获得 8 瓶药水",              8, 0) { Reward = RewardKind.CardAny },
        new(QuestKind.CardReplay,     "我说重放有没有懂的", "让牌重放 5 次",                  5, 0) { Reward = RewardKind.CardRare },
        // —— 批之七 ——
        new(QuestKind.ActThree,            "一步之遥",           "走到第三幕",                   1,  0,
            "奖励：随机一件遗物", true),
        new(QuestKind.BlockClearPrevented, "永固",               "阻止格挡被清空 3 次",          3,  0) { Reward = RewardKind.CardPower },
        new(QuestKind.DamageTakenCount,    "皮糙肉厚",           "累计受到 30 次伤害",           30, 0) { Reward = RewardKind.CardAny },
        new(QuestKind.DeckAddPrevented,    "我说别塞有没有懂的", "阻止 3 张牌进入牌组",          3,  0) { Reward = RewardKind.CardAny },
        // —— 批之八：靠 Harmony 补丁才有的「卡牌加入卡组」维度（一代 #7）——
        new(QuestKind.CardsAddedAttack, "我全都要·攻", "往卡组加入 4 张攻击牌", 4, 0) { Reward = RewardKind.CardRare },
        new(QuestKind.CardsAddedSkill,  "我全都要·技", "往卡组加入 4 张技能牌", 4, 0) { Reward = RewardKind.CardPower },
        new(QuestKind.CardsAddedPower,  "我全都要·能", "往卡组加入 4 张能力牌", 4, 0) { Reward = RewardKind.CardAny },
        // —— 批之九：把「梗化」那批梗做成的任务 ——
        // ① 计数型
        new(QuestKind.WorkerCards,     "打工人",   "本局打出 60 张「打击 / 防御」这类起始牌", 60, 100),
        new(QuestKind.MokugyoSkills,   "电子木鱼", "本局打出 50 张技能牌",                   50, 100) { Reward = RewardKind.CardAny },
        // ② 挑战型：接下后**改变接下来几场战斗的打法**；本场违规就不算这一场，赢下 N 场完成
        new(QuestKind.RetreatBlock,    "退退退",   "接下后，接下来 2 场战斗不能打出攻击牌；赢下 2 场",      2, 100) { Reward = RewardKind.CardRare },
        new(QuestKind.SlackOff,        "摸鱼",     "接下后，接下来 2 场战斗的前 2 回合不能出牌；赢下 2 场", 2, 100) { Reward = RewardKind.CardPower },
        new(QuestKind.NakedRun,        "裸装",     "接下后，接下来 3 场战斗不能打出能力牌；赢下 3 场",      3, 100) { Reward = RewardKind.Relic },
        // ③ 叙事型：不是一路数下去，而是要凑齐一组条件（达成那一刻才有故事感）
        new(QuestKind.SanheGod,        "三和大神", "一局里在篝火休息 3 次，且一次铁都没打",                 1, 100) { Reward = RewardKind.Relic },
        new(QuestKind.FamilyBucket,    "全家桶",   "牌组里同时有 6 张攻击、6 张技能、6 张能力",             1, 100) { Reward = RewardKind.CardPower },

        // —— 批之十：速度型 / 牌组极致型（奖励里第一次出现「删牌」）——
        new(QuestKind.FirstTurnKill,   "一发入魂",   "第一个回合就把这场战斗打完",                   1, 100) { Reward = RewardKind.CardRare },
        new(QuestKind.SpeedContract,   "限时特惠",   "接下后，接下来 3 场都得在 3 回合内打赢",        3, 100) { Reward = RewardKind.Relic },
        new(QuestKind.EliteSpeedKill,  "落地成盒",   "3 回合内打赢一场精英战",                      1, 250),
        new(QuestKind.MinimalDeck,     "极简主义",   "牌组不超过 10 张的时候赢下一场战斗",            1, 100) { Reward = RewardKind.RemoveCard },
        new(QuestKind.ZeroCostDeck,    "斗地主附体", "牌组里有 8 张 0 费牌",                         8, 100) { Reward = RewardKind.CardRare },
        new(QuestKind.DiscardInCombat, "羊关通关",   "单场战斗里弃掉 25 张牌",                      25, 100) { Reward = RewardKind.CardAny },
        new(QuestKind.RainbowDeck,     "国际纵队",   "牌组里有 4 张别的职业的牌",                     4, 100) { Reward = RewardKind.CardSkill },
        new(QuestKind.PurgeMaster,     "清仓大甩卖", "本局一共删掉 6 张牌",                          6, 100) { Reward = RewardKind.RemoveCard },
    ];

    // 按类型取定义（读档恢复用）。目录里没有这个类型时返回 null。
    public static QuestDef? Find(QuestKind kind)
    {
        foreach (var d in All)
            if (d.Kind == kind) return d;
        return null;
    }

    /// <summary>
    /// **确定性**候选：给定种子，每个客户端抽出来的顺序完全一样。
    /// 多人局必须用这个（`Random.Shared` 各抽各的 → 各客户端任务不一样 → 不同步）。
    /// 先抽前置型（EarlyBird），不够再补别的。
    /// </summary>
    public static IReadOnlyList<QuestDef> PickOffersSeeded(
        int n, int seed, IEnumerable<QuestKind> exclude, Type? character = null)
    {
        var ex = new HashSet<QuestKind>(exclude);
        var rng = new System.Random(seed);
        var usable = All
            .Where(d => !ex.Contains(d.Kind))
            .Where(d => d.OnlyCharacter == null
                        || character == null
                        || d.OnlyCharacter.IsAssignableFrom(character))
            .SelectMany(d => Enumerable.Repeat(d, Math.Max(1, d.Weight)))   // 权重（权重高的更容易被抽到）
            .Distinct()
            .ToList();

        var early = usable.Where(d => d.EarlyBird).ToList();
        var rest = usable.Where(d => !d.EarlyBird).ToList();
        Shuffle(early, rng);
        Shuffle(rest, rng);

        var result = new List<QuestDef>();
        result.AddRange(early);
        result.AddRange(rest);
        return result.Take(n).ToList();
    }

    private static void Shuffle(List<QuestDef> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // 从目录里随机挑 n 个候选，排除已在进行 / 已完成的任务类型。
    //
    // preferEarly = true 时（开局选任务），**先从前置型任务里抽**，不够再拿别的补。
    // 不这么做的话，48 个任务里随机抽 6 个，前置型遗物任务经常到第三幕才露脸，
    // 拿到手已经没用了。
    //
    // character = 当前角色类型。带 OnlyCharacter 的任务只发给对应角色，
    // 挡掉「永远做不完」的候选（例如充能球类的任务对铁甲战士毫无意义）。
    public static IReadOnlyList<QuestDef> PickOffers(
        int n, IEnumerable<QuestKind> exclude, bool preferEarly = false, Type? character = null)
    {
        var ex = new HashSet<QuestKind>(exclude);
        var pool = All
            .Where(d => !ex.Contains(d.Kind))
            .Where(d => d.OnlyCharacter == null
                        || character == null
                        || d.OnlyCharacter.IsAssignableFrom(character))
            .SelectMany(d => Enumerable.Repeat(d, Math.Max(1, d.Weight)))
            .OrderBy(_ => Random.Shared.Next())
            .Distinct()
            .ToList();

        if (!preferEarly) return pool.Take(n).ToList();

        var early = pool.Where(d => d.EarlyBird).ToList();
        if (early.Count >= n) return early.Take(n).ToList();
        early.AddRange(pool.Where(d => !d.EarlyBird));
        return early.Take(n).ToList();
    }
}
