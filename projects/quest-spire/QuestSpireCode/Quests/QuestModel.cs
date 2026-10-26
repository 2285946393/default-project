namespace QuestSpire.Quests;

// 任务类型：决定哪个游戏事件给它累加进度。每种类型同时最多接一个。
public enum QuestKind
{
    // —— 累计计数类 ——
    DamageDealt,       // 累计造成伤害
    BlockGained,       // 累计获得格挡
    AttacksPlayed,     // 打出攻击牌数
    CardsPlayed,       // 累计出牌数
    CardsExhausted,    // 消耗牌数
    TurnsSurvived,     // 存活玩家回合数
    PotionsUsed,       // 使用药水数
    CombatsWon,        // 赢得战斗数
    EliteWins,         // 精英战胜利
    BossWins,          // 首领战胜利
    EmptyHands,        // 打空手牌次数（Hellbent）
    IdleTurns,         // 整回合不出牌的回合数（Wait For It）
    SelfDamage,        // 自身累计受到伤害（Unbreakable）
    FlawlessCombats,   // 无伤赢得的战斗数（Untouchable）
    CloseCalls,        // 残血(<30%)赢下的战斗数（Autotransfusion）
    CampfireSmiths,    // 篝火升级卡次数（Humble Smithy）
    RestSites,         // 篝火休息次数（That's S'Mores）
    ShopPurchases,     // 商店购买次数（Favourite Customer）
    FreeCardsPlayed,   // 打出 0 费牌数（Who needs energy）
    HeavyCardsPlayed,  // 打出费用≥2 的牌数（Heavy Hitter）
    RareCardsPlayed,   // 打出稀有牌数（Rares Are Good）
    PowersPlayed,      // 打出能力牌数（Jack of All Trades）
    // —— 单回合内计数（每回合清零）——
    AttacksInOneTurn,  // 单回合打出攻击数（Rager）
    // —— 快照类（取当前值，只增不减）——
    GoldHoard,         // 持有金币（Amass a Hoard）
    RoomTypesSeen,     // 进入过的不同房间类型数（Biome Sampler）

    // ===================================================================
    // 2026-09-22 新增（从一代池子的「🔵 能做没做」里挑的）。
    // ⚠️ 新成员一律**追加在末尾**：存档里存的是 (int)Kind，插在中间会让老存档串位。
    // ===================================================================
    ComboCosts,            // #2  一回合打出 1/2/3 费各一张
    CommonsOnlyWins,       // #4  不用罕见/稀有牌赢下精英或首领
    OrbsInOneTurn,         // #6  单回合激发 4 种不同充能球
    DeckOrganized,         // #16 卡组恰好 10 攻击 / 10 技能 / 5 能力
    EliteFirstTurnIdle,    // #39 精英战第一回合不出牌
    ExactKills,            // #41 精确伤害击杀
    EventRooms,            // #47 访问 ? 房间
    DeckPeasant,           // #52 卡组里普通牌 −（罕见+稀有）≥ 7
    WindowShopping,        // #67 空手离开商店
    DeckEvenOnly,          // #70 卡组 ≥7 张偶数/0 费且没有奇数费牌
    FastCombats,           // #74 4 回合内结束战斗
    ClawPlays,             // #75 打出 Claw
    FullHpAtBoss,          // #76 进首领房时满血
    UnchangedMaxHpBossWin, // #77 没改过生命上限，赢下二幕首领

    // ===================================================================
    // 2026-09-22 夜：梗任务三连（奖励不再是金币，而是遗物 / 卡牌）。
    // 同样必须追加在末尾。
    // ===================================================================
    CaptainsLegacy,        // 船长的执念：集齐锚 / 角夹 / 船长之轮 → 送「锚???」
    CardsExhaustedBig,     // 枯枝还魂：**单场战斗**消耗 10 张以上牌 → 给一根自研枯枝（2026-09-25 改）
    ShopXDeal,             // 展望未来：带着 250 金币进商店（自动扣 250）→ 化学X + 一张任意职业的 X 费牌
                           // ⚠️ 这个位置原来叫 XCardsPlayed（条件是「打出 X 费牌」，玩家反馈抓不到牌就完不成）。
                           //    只改名、不动位置：存档存的是 (int)Kind，位置一动老存档就串位。

    // ===================================================================
    // 2026-09-22 深夜二批：一代遗物任务（奖励优先发「更多遗物」mod 里的真货，
    // 没装那个 mod 就发我们自己的仿制品）。同样追加在末尾。
    // ===================================================================
    BlockGainedBig,        // 小伤不理：累计 200 格挡 → 鸟居（鸟居本身随时有用，条件早期一点更稳）
    SmithNoRest,           // 今晚不睡：篝火升级 2 张且从未休息 → 咖啡滤杯
    PotionsUsedBig,        // 药罐子：使用 4 瓶药水 → 玩具扑翼飞机
    GoldHoardBig,          // 见钱眼开：攒到 150 金币 → 金偶像（金偶像是「之后金币+25%」，必须早给）
    TreasureRooms,         // 一个都不放过：打开第 1 个宝箱房 → 套娃（套娃只管后面 2 个宝箱，所以必须早给）
    CurseFreeCombats,      // 一身清白：牌组 0 诅咒赢下 5 场 → 御守（御守是「抵消之后的诅咒」，反着来才合理）

    // ===================================================================
    // 2026-09-22 深夜五批：「鸡煲三部曲」——故障机器人专属（OnlyCharacter = Defect）。
    // 梗来源：故障机器人社区谐音「鸡煲」（机宝 → 鸡煲，「鸡」= 菜鸡），
    // 公认最弱是因为「启动太慢」：别人在过牌输出叠甲，它在通电。
    // ===================================================================
    SlowStartWin,          // 还在启动：前 3 个回合不出牌，还把这场打赢 → 机械臂
    EchoFormTurn,          // 回响形态：单回合内把同一张牌打出两次 → 齿轮工艺品
    ActOneEliteWin,        // 严父在上：第一幕赢下精英战（小红是「鸡煲严父」）→ 突变酵素

    // ===================================================================
    // 2026-09-22 深夜六批：借《海克斯符文》的两个手法。
    // ① 「升级一张牌」型奖励（不是发遗物，是直接强化你的牌）
    // ② 「代价换强度」型奖励（强力，但有明确的代价）
    // ===================================================================
    CardsPlayedBig,        // 熟能生巧：打出 60 张牌 → 随机升级牌组里 2 张
    CloseCallsBig,         // 玩命：4 次残血赢下战斗 → 莽夫之道（伤害 +50%，但不能获得格挡）

    // ===================================================================
    // 2026-09-22 深夜七批：顺着「钩子菜单」自己设计的一批（不抄别人 mod 的效果）。
    // 每个奖励对着一个不同的游戏旋钮：
    //   CardsDrawn        → ModifyHandDraw（抽牌数）
    //   GoldSpent         → ModifyMerchantPrice（商店价格）
    //   RestSitesBig      → ModifyRestSiteHealAmount（篝火回复）
    //   FlawlessCombatsBig→ ModifyMaxEnergy（能量上限）
    // ===================================================================
    CardsDrawn,            // 手不释卷：本局抽 120 张牌 → 每回合多抽 1 张
    GoldSpent,             // 砍价高手：本局在商店花掉 300 金币 → 商店价格 -30%
    RestSitesBig,          // 养生：本局在篝火休息 4 次 → 篝火回复量 +50%
    FlawlessCombatsBig,    // 稳如老狗：5 场无伤取胜 → 能量上限 +1

    // ===================================================================
    // 2026-09-22 深夜七批之二：继续对着没被用过的旋钮设计。
    //   PowersPlayedBig → ModifyPowerAmountGivenAdditive（你施加的增益/减益层数）
    //   SelfDamageBig   → ModifyDamageCap（你单次受到的伤害上限）
    //   AttacksPlayedBig→ ModifyAttackHitCount（攻击牌多打一段）
    // ===================================================================
    PowersPlayedBig,       // 厚积薄发：打出 15 张能力牌 → 你施加的增益/减益 +1 层
    SelfDamageBig,         // 铁壁：本局承受 300 点伤害 → 你单次受到的伤害不超过 15
    AttacksPlayedBig,      // 连环拳：本局打出 40 张攻击牌 → 你的攻击牌多打一段

    // ===================================================================
    // 2026-09-22 深夜八批：B 站梗。摄政王专属。
    // 「宇宙冷漠」= 游戏里真有的牌 CosmicIndifference（1 费技能：6 格挡 + 从弃牌堆捞一张
    // 放回抽牌堆顶）。社区把它当歌唱：「宇！宙！冷！漠！」大合唱、KTV 救赎感。
    // ===================================================================
    CosmicIndifferencePlayed, // 宇！宙！冷！漠！：打出 3 张宇宙冷漠 → 获得格挡时额外 +3

    // ===================================================================
    // 2026-09-23 批：B 站《尖塔梗百科》里挑出来的梗（那合集每期标题就是一个梗）。
    // 每一条都先反编译确认过游戏里真有对应的钩子 / 牌 / 能力，不是猜的（见交接文档 §10）。
    // 依然**追加在末尾**。
    // ===================================================================
    CardsDrawnInOneTurn,   // 我说过牌有没有懂的：单回合抽 8 张（AfterCardDrawn，每回合清零）
    NoSkillCombatWin,      // 我说别带：一场战斗里一张技能牌都不打还赢（AfterCardPlayed + AfterCombatVictory）
    BigPurchase,           // 152 金币：在商店一次花掉 150 金币以上（AfterItemPurchased 自带 goldSpent）
    OrbsChanneled,         // 通电：本局生成 8 个充能球（AfterOrbChanneled）——「别人在输出，鸡煲在通电」
    DebuffLayersGiven,     // 蛇花圣经：给敌人累计叠 20 层减益（BeforePowerAmountChanged + PowerType.Debuff）
    FlameBarrierDamage,    // 火焰屏障输出特别高：靠火焰屏障的反弹打出伤害（伤害来自 FlameBarrierPower，不是牌）
    FocusLost,             // 掉集中：累计失去 10 点集中（偏差认知每回合掉集中 → 玩家话越来越短）

    // ===================================================================
    // 2026-09-23 批之二：剩下的几个梗，同样先反编译核实过才写。
    // ===================================================================
    SnakebitePlays,         // 蛇咬：打出 3 张「蛇咬」（社区吵「是不是全游戏最烂的卡」）
    CardRemovals,           // 删内切：在商店花钱删掉 2 张牌（「152 金币删内切」的本体）
    CardsGeneratedInCombat, // 复制防御：一场战斗内生成 5 张牌（DualWield 那类复制走 CreateClone）
    BloodiedWin,            // 战个未来：先被打到半血以下，最后还赢（「先亏后赚」的轻量版）
    FakeRelicsOwned,        // 假货我也要：同时拥有 2 件假货（假商人卖的 Fake* 遗物）
    RealAndFakeAnchor,      // 真的假的：同时拥有真锚和假锚「锚???」

    // ===================================================================
    // 2026-09-23 批之三：认房间 / 认角色（CombatRoom.ModelId 能认出打的是谁）。
    // ===================================================================
    FakeMerchantWin,        // 打赢假商人：击败假商人（Encounter Id 带 FAKE_MERCHANT）
    DamageInOneTurn,        // 数值轮椅：单回合造成 30 点伤害（每回合清零）
    DefectEliteWins,        // 你玩的鸡煲啊：故障机器人本局赢下 2 场精英战
    SilentCardsInTurn,      // 劣人手速：静默猎手单回合打出 7 张牌（「劣人」= 社区给猎手起的外号）

    // ===================================================================
    // 2026-09-23 批之四：把角色外号补全（战士鸽 / 劣人 / 鸡煲），顺手用上「花费能量」这个钩子。
    // ===================================================================
    TankedCombatWin,        // 战士鸽：一场战斗里自己挨 20 点伤害还赢（战士鸽就是挨打变强的）
    CardsInOneTurnBig,      // 无限：单回合打出 10 张牌（无限连的雏形）
    EnergySpentInOneTurn,   // 我说有费有没有懂的：单回合花掉 6 点能量

    // ===================================================================
    // 2026-09-23 批之五：把《尖塔梗百科》里最后两个能搞懂的梗做了。
    // 含义是从那几期视频的**最高赞评论**里读出来的（原话记在交接文档 §10.8）：
    //   「基米精神」= 没有开辉眼的强行战未来 —— 宁愿赌极小概率胡局，也不肯打过渡牌
    //   「东尼意思」= 制作人安东尼（Anthony）的平衡哲学：「xxx 很强 → 增强别的让 xxx 变得合理」
    //   「onp」查实是主播圈刷屏梗（源头是评论区互撕），含义不适合做成任务 → 不做。
    // ===================================================================
    GreedyShopVisit,        // 基米精神：带着 200 金币进商店、一分钱不花就走（死赌未来）
    UpgradedDeck,           // 东尼意思：卡组里有 8 张升级过的牌（打不过就把别的都加强）

    // ===================================================================
    // 2026-09-23 批之六：纯「乐子」——从 B 站社区现搜的玩笑/名场面里挑的。
    // 名字来源：视频标题或社区连载梗（找梗工具见 tools/meme_hunt.py）。
    // ===================================================================
    HellCrawlWin,           // 我从地狱蠕动出来了（6.9 万播放那条）：以 1 点生命赢下一场战斗
    MarriageBoss,           // boss 谁最适合结婚（社区连载梗）：满血赢下一场首领战
    GuguGaga,               // 咕咕嘎嘎：一场战斗里一张牌都不打，还赢了
    OnePunch,               // 一拳超人：单次造成 100 点以上伤害
    IronShirt,              // 铁布衫：单次获得 50 点以上格挡
    TripleKill,             // 三杀：单回合击杀 3 个敌人
    Repeater,               // 复读机：单回合把同一张牌打出 3 次
    SelfDestruct,           // 自暴自弃：卡组里攒到 5 张以上诅咒（和「一身清白」正好相反）
    GuardBroken,            // 破防：单场战斗里格挡被打穿 5 次
    FullHand,               // 满手牌：单回合手牌达到 10 张
    ThunderDefect,          // 雷霆大机煲（B 站标题）：故障机器人单场战斗激发 6 个充能球
    GaRenMen,               // 噶人焖（农神菜农来辣的口头禅）：摄政王累计获得 30 点辉星

    // ===================================================================
    // 2026-09-23 批之七：第二轮搜出来的（行话 / 主播梗 / 角色外号）。
    // 顺带解开了「小蓝人」——社区提到机器人时会说「提一嘴小蓝人」，所以它 = 故障机器人的外号。
    // ===================================================================
    BashPlays,              // 肘击：铁甲战士打出 10 张「痛击」（Bash；「肘击」是牢大系谐音梗）
    DefectBossWins,         // 小蓝人：用故障机器人赢下 2 场首领战
    OstySummons,            // 骨小妹：亡灵绑定者累计召唤 5 次（Osty 就是那只小骷髅）
    CardsDiscarded,         // 我说弃牌有没有懂的：静默猎手累计弃掉 30 张牌
    LongCombatWin,          // 太牢了：一场战斗打到 10 回合还赢（「牢」= 卡在那儿起不来）

    // ===================================================================
    // 2026-09-23 批之八：把「杂耍」这个梗也做了（反编译查实的）。
    // 「杂耍」= 牌 `Cards.Juggling`（1 费能力 / 罕见）：挂着 JugglingPower 时，
    // **每回合第 3 张攻击牌会被复制一张到手牌**。社区管这套叫「杂耍」，
    // B 站有「这个世界上有两种杂耍」（10.9 万播放）「论杂耍与粒子墙的共通之处」等。
    // ===================================================================
    JugglingTricks,         // 杂耍：带着「杂耍」一回合打满 3 张攻击牌（触发复制），累计 3 次

    // ===================================================================
    // 2026-09-23 批之九：把「尖塔梗百科」剩下的期数补齐（一共 18 期，我们原先只记了 14 期）。
    // 含义都是从那几期的**热评**里读出来的（见交接文档 §15）。
    // ===================================================================
    JackpotStreak,          // 农种：连续 3 场战斗不掉血（「农种」= 开刷的爽种子）
    NeverSurrender,         // 绝不认输：以低于 10% 生命赢下 2 场战斗（不 SL，死磕到底）
    WatcherBurst,           // 观者？！：单回合造成 300 点伤害（观者级爆发）
    TwoCardWin,             // 你知道是谁：只用 2 种牌赢下一场战斗（「一层两牌无限」）

    // ===================================================================
    // 2026-09-23 批之十：按「流派」做（玩家讨论里最常出现的几条构筑路线）。
    // buff 类型都是反编译核实的：StrengthPower / DexterityPower / PoisonPower /
    // VulnerablePower 都在 Models.Powers 里（共 268 个能力，别乱猜名字）。
    // ===================================================================
    StrengthGained,         // 健身教练：累计给自己叠 30 层力量
    PoisonGiven,            // 我说叠毒有没有懂的：给敌人累计叠 50 层中毒
    DexterityGained,        // 身法：累计给自己叠 20 层敏捷
    VulnerableGiven,        // 破甲：给敌人累计叠 30 层易伤
    TurtleBlock,            // 龟壳：单场战斗里获得 200 点格挡

    // ===================================================================
    // 2026-09-25 批：玩家点名的（战士专属）。同样**追加在末尾**（存档存的是 (int)Kind）。
    // 「地狱狂徒」= Hellraiser（2 费稀有**能力**牌）：它的能力是「抽到带『打击』标签的牌时
    // 自动打出」（反编译 HellraiserPower.AfterCardDrawnEarly 核实），所以条件做成"厚打击牌组"。
    // ===================================================================
    StrikeDeckHellraiser,   // 地狱狂徒：牌组里有 9 张以上带「打击」标签的牌 → 发一张「地狱狂徒」

    // ===================================================================
    // 2026-09-24 批：跨界联动（题目是别的游戏）。铁律：**条件必须和奖励相关**。
    // 奖励用 BaseLib 提供的「选一张牌升级 / 变牌」零件（和海克斯同一套）。
    // ===================================================================
    ParryFlawless,          // 只狼：一场战斗不掉血、格挡也没被打穿还赢 → 选一张牌升级
    // —— 跨界联动 第二批 ——
    MinecraftDig,           // 我的世界：挖矿 —— 一局打开 3 个宝箱房 → 随机一件遗物（挖到矿）
    PokemonEvolve,          // 宝可梦：进化 —— 牌组里 8 张以上升级过的牌 → 选一张牌升级（继续进化）
    WoWEnchant,             // 魔兽世界：附魔 —— 一场战斗消耗 10 张牌 → 给牌组里一张牌挂「消耗」词条（附魔）

    // —— 跨界联动 第三批（维度：牌组唯一性 / 单场费用纪律，之前都没用过）——
    DeckNoDuplicates,       // 我的世界：纯净卡组 —— 牌组 ≥15 张且没有一张重样 → 随机遗物
    OnlyOneCostWin,         // 文明6：精打细算 —— 一场战斗只打出 1 费牌还赢 → 金币 +250

    // —— 跨界联动 第四批（维度：单场阈值组合 / 稀有度组合 / 牌型 / 全升级）——
    TerrariaShieldWin,     // 泰拉瑞亚：以盾取胜
    DontStarveEndure,      // 饥荒：硬扛过冬
    DiabloFarming,         // 暗黑破坏神：刷装备
    GenshinWish,           // 原神：三分祈愿
    ArknightsDeploy,       // 明日方舟：部署
    PokemonAllEvolve,      // 宝可梦：全队进化

    // ===================================================================
    // 2026-10-01 批：跨游戏「收集 / 成长」维度 —— 用的都是之前没碰过的
    // 游戏数据（反编译 Player 确认可读）：遗物列表 / 药水列表 / 卡组张数 / 生命上限成长。
    // 老规矩：**只能追加在末尾**（存档存的是 (int)Kind）。
    // ===================================================================
    RelicsHeld,            // 以撒的结合 · 道具大亨：同时持有 N 件遗物（维度：遗物**数量**）
    PotionsHeld,           // 星露谷物语 · 背包：同时持有 N 瓶药水（维度：药水**存量**）
    DeckSizeBig,           // 俄罗斯方块 · 堆高：卡组攒到 N 张（维度：卡组**张数**）
    MaxHpGained,           // 黑神话：悟空 · 修炼：生命上限比开局多 N（维度：**成长**）
    ContractChallenge,     // 血之契约：接下后每场战斗敌人开场随机带一个增益，打赢 N 场（维度：风险换强力遗物）

    // ===================================================================
    // 2026-10-01 批之二：盛碗虫 + 单回合 / 条件类（玩家要求「多点任务」）。
    // 依旧**只能追加在末尾**。
    // ===================================================================
    BowlbugSwarm,          // 盛碗虫泛滥：接下后每场战斗多出现一只盛碗虫 → 随身物「盛碗虫常数」（社区梗：15 挡不住 16）
    BlockInOneTurn,        // 我说叠甲有没有懂的：单回合获得 40 点格挡（维度：**单回合**格挡，之前只有单场总量）
    SkillsInOneTurn,       // 花拳绣腿：单回合打出 8 张技能牌（维度：单回合技能牌）
    GoldZeroWin,           // 两袖清风：身上 0 金币时赢下一场战斗（维度：**金币为 0**）
    DoomKills,             // 骨头人（亡灵绑定者）：用「灾厄」击杀敌人（灾厄是直接 Kill，不走伤害）
                           // ⚠️ 原来这条叫 NoDamageDealtWin（零输出赢战斗），但它**站不住脚**：
                           //    灾厄本来就是直接 Kill、不算你造成的伤害，联机里队友帮你杀光也是零输出 →
                           //    「零输出」只等于"你没亲自打伤害"，白送还依赖伤害归属。改成直接用灾厄机制。
    SameCardManyTimes,     // 堆叠宝石：单回合把同一张牌打出 8 次 → 给一张「未掘宝石」(HIDDEN_GEM，
                           //                    本体里唯一带「宝石」的卡：给一张牌叠「重放」)

    // ===================================================================
    // 2026-10-01 批之三：往战斗里加怪 / 加精英（玩家点名「多来点这种」，
    // 参考一代 Spire Quests 的 #55 怪潮 / #56 强化精英 / #71 随机怪混入）。
    // 奖励**不给金币**，发遗物。
    // ===================================================================
    MonsterRush,           // 怪潮：接下后每场战斗额外多 2 只普通怪 → 打赢 3 场 → 随机遗物
    EliteAmbush,           // 精英加餐：接下后每场战斗额外多 1 只精英 → 打赢 2 场 → 随机遗物 + 金币

    // ===================================================================
    // 2026-10-01 批之四：接取时改牌组的两个任务（玩家点名）。
    // 接取时的改动**各端都要做**（牌组是复制的 → 各端算出来一样才不同步），见 EnsureQuestSetups。
    // ===================================================================
    CursedBargain,         // 诅咒交易：接取时随机塞 5 张诅咒；打赢 3 场 → 500 金币 + 清除所有诅咒
    DeckOverhaul,          // 大改造：接取时把「打击→飞溅、防御→发现」(升级版)；每场战斗多一只精英；打赢 3 场 → 遗物

    // ===================================================================
    // 2026-10-01 批之五：用**没碰过的钩子**做的一批（玩家：「自己找点乐子」）。
    //   AfterFlush / AfterPotionDiscarded / AfterOstyRevived / AfterEnergySpent
    // 依旧只能追加在末尾。
    // ===================================================================
    FlushCount,            // 轮回：本局弃牌堆洗回抽牌堆 N 次（AfterFlush）
    PotionDiscarded,       // 我说丢药有没有懂的：累计丢弃 N 瓶药水（AfterPotionDiscarded）
    OstyRevive,            // 骨小妹·不死：Osty 复活 N 次（AfterOstyRevived，亡灵绑定者）
    EnergyTotal,           // 能量黑洞：本局累计花掉 N 点能量（AfterEnergySpent；之前只有"单回合"的）

    // 2026-10-01 批之六：接着挖没用过的钩子。
    PileMoved,             // 牌堆搬运工：牌在牌堆之间移动 N 次（AfterCardChangedPiles）
    PotionProcured,        // 我说给药有没有懂的：累计获得 N 瓶药水（AfterPotionProcured；之前只算"用"和"丢"）
    CardReplay,            // 我说重放有没有懂的：牌的重放次数被改 N 次（AfterModifyingCardPlayCount）

    // 2026-10-01 批之七：继续挖。
    ActThree,              // 一步之遥：走到第三幕（AfterActEntered）
    BlockClearPrevented,   // 永固：阻止格挡被清空 N 次（AfterPreventingBlockClear，壁垒/卡尺那类）
    DamageTakenCount,      // 皮糙肉厚：累计**挨打次数** N 次（BeforeDamageReceived；之前只算"挨了多少血"）
    DeckAddPrevented,      // 我说别塞有没有懂的：阻止 N 张牌进入牌组（AfterAddToDeckPrevented，比如御守挡诅咒）

    // 2026-10-02 批之八：**Harmony 补丁**提供的钩子（本体没有"卡牌加入卡组"的正向钩子）——
    // 见 `Patches/CardAddedToDeckPatch`。一代 #7「加 3 攻 / 3 技 / 3 能」那一批。
    CardsAddedAttack,      // 我全都要·攻：往卡组加入 N 张攻击牌
    CardsAddedSkill,       // 我全都要·技：往卡组加入 N 张技能牌
    CardsAddedPower,       // 我全都要·能：往卡组加入 N 张能力牌

    // —— 2026-10-02 批之九：把「梗化」那批梗拿来做任务 ——
    // 两类：① 计数型（打工人 / 电子木鱼）；② **挑战型**（接下后改变接下来几场战斗的打法，
    // 退退退 / 摸鱼 / 裸装）—— 挑战型的写法照「血之契约」那批：本场打错了就作废，
    // 赢下 N 场才算完成（判定在 AfterCombatVictory）。
    WorkerCards,           // 打工人：本局打出 N 张「打击 / 防御」这类起始牌
    MokugyoSkills,         // 电子木鱼：本局打出 N 张技能牌
    RetreatBlock,          // 退退退：接下后，接下来 N 场战斗不能打出攻击牌；赢 N 场
    SlackOff,              // 摸鱼：接下后，接下来 N 场战斗的前 2 回合不能出牌；赢 N 场
    NakedRun,              // 裸装：接下后，接下来 N 场战斗不能打出能力牌；赢 N 场

    // —— 批之九·叙事型：有"故事"、要满足一组条件才达成（不像计数型一路数下去）——
    SanheGod,              // 三和大神：一局里在篝火休息 3 次，且一次铁都没打（做一天玩三天）
    FamilyBucket,          // 全家桶：牌组里同时有 6 张攻击 / 6 张技能 / 6 张能力

    // ===================================================================
    // 2026-10-03 批之十：速度型 / 牌组极致型 / 新奖励维度（删牌）。**只能追加在末尾**。
    // ===================================================================
    FirstTurnKill,         // 一发入魂：第一个回合就把这场战斗解决掉（维度：结束得**快不快**）
    SpeedContract,         // 限时特惠：接下后 3 场战斗必须 3 回合内赢（挑战型 + 时间压力）
    EliteSpeedKill,        // 落地成盒：3 回合内打赢一场精英战（维度：精英 + 速度）
    MinimalDeck,           // 极简主义：牌组 ≤ 10 张时赢下一场（维度：牌组**小**到极致）
    ZeroCostDeck,          // 斗地主附体：牌组里有 8 张 0 费牌（维度：卡组里的**费用分布**）
    DiscardInCombat,       // 羊关通关：单场战斗弃掉 25 张牌（维度：**单场**弃牌量）
    RainbowDeck,           // 国际纵队：牌组里有 4 张**别的职业**的牌（维度：跨职业收藏）
    PurgeMaster,           // 清仓大甩卖：本局一共删掉 6 张牌（维度：**删牌总数**，奖励也是删牌）
}

// 任务静态定义（模板）。
// RewardLabel 只有「奖励不是金币」的任务才填；填了就用它当奖励说明。
//
// EarlyBird = 「前置型」任务：奖励是拿到之后才持续生效的东西（遗物 / 随身物）。
// 这种任务必须在局早期就摆到玩家面前，否则到手时局已经快结束 = 白给。
// 开局选任务的候选会**优先**从 EarlyBird 里抽（见 QuestCatalog.PickOffers）。
// 反例（历史踩坑）：金偶像「之后金币 +25%」配了「攒到 500 金币」；御守「抵消之后的诅咒」
// 配了「牌组里已经有 3 张诅咒」；套娃「后面 2 个宝箱双倍」配了「开满 3 个宝箱房」。
//
// OnlyCharacter = 只有这个角色才抽得到（null = 谁都能抽）。
// 用来挡掉「永远做不完」的任务：充能球和 Claw 都只有故障机器人有，
// 抽给铁甲战士就是占着候选位的废牌。
/// <summary>任务奖励类型。`Gold` = 用 `RewardGold` 发金币；其余走各自的发放逻辑。</summary>
public enum RewardKind
{
    Gold,
    Relic,          // 一件随机遗物（我们自己的遗物池）
    CardAny,        // 一张牌三选一（任意职业）
    CardSkill,      // 一张技能牌三选一
    CardPower,      // 一张能力牌三选一
    CardRare,       // 一张稀有牌三选一
    CardOtherClass, // 一张**其他职业**的牌三选一
    Upgrade,        // 随机升级牌组里 2 张牌
    Transform,      // 把一张牌变成另一张
    CardNamed,      // 奖励一张**指定的**核心牌（看 `QuestDef.RewardCard`）
    Enchant,        // 给牌组里一张牌附魔（用本体自带的附魔系统，22 种）
    RemoveCard,     // 从牌组里**删掉**一张牌（本体自带的删牌奖励，`CardRemovalReward`）
}

public sealed record QuestDef(
    QuestKind Kind, string Title, string Description, int Target, int RewardGold,
    string? RewardLabel = null, bool EarlyBird = false, Type? OnlyCharacter = null)
{
    /// <summary>抽候选时的权重（默认 1，现在**所有任务都是 1** = 概率均等）。
    /// 2026-10-01 玩家要求：把之前跨界任务测试用的 8 倍权重全部拉平。</summary>
    public int Weight { get; init; } = 1;

    /// <summary>
    /// 奖励类型（默认金币）。玩家反馈"奖励清一色金币没意思" →
    /// 一批凑数型任务改成**遗物 / 卡牌三选一 / 升级 / 变牌**。
    /// 填了 `RewardLabel` 的任务不受影响（那类走自定义 case）。
    /// </summary>
    public RewardKind Reward { get; init; } = RewardKind.Gold;

    /// <summary>`RewardKind.CardNamed` 用：要发的那张牌的**模型 Id**（例如 "DEMON_FORM"、"BARRICADE"）。</summary>
    public string? RewardCard { get; init; }

    public string RewardText => RewardLabel ?? $"金币 +{RewardGold}";

    /// <summary>
    /// 多人局里要不要按**队伍人数**放大目标。
    ///
    /// 依据（反编译 `MultiplayerScalingModel` / `EncounterModel` / `PlayerCmd` 确认）：
    /// - 敌人的**血量与格挡**按人数放大（一幕 ×1.1 / 二幕 ×1.2 / 三幕 ×1.2、首领 ×1.3，再乘人数）；
    /// - 队友能直接给你加码：喂能量、上增益、挂易伤——所以**单回合爆发类**和**伤害/格挡累计类**
    ///   在联机里会明显变容易；
    /// - 金币是**按人各算**（`player.Gold += amount`），房间数、胜负场次、卡组构成也跟人数无关 → 这些**不放大**。
    /// </summary>
    public bool ScaleWithPlayers { get; init; }
}

// 任务运行时实例（进度）。
public sealed class QuestState
{
    public QuestDef Def { get; }
    public int Current { get; private set; }
    public bool Completed { get; private set; }

    /// <summary>
    /// **实际**目标：多人局里带 <see cref="QuestDef.ScaleWithPlayers"/> 的任务按队伍人数放大。
    /// 进度判定、读档夹取、面板显示都走这个，别直接用 `Def.Target`。
    /// 人数由 <see cref="QuestRunner.PlayerCount"/> 在开局时记下。
    /// </summary>
    public int Target
    {
        get
        {
            int baseTarget = Def.ScaleWithPlayers
                ? Def.Target * Math.Max(1, QuestRunner.PlayerCount)
                : Def.Target;
            // 设置页的任务难度：直接乘在目标数值上（休闲 0.6× / 正常 1× / 硬核 1.5×）。
            double mult = QuestSpireConfig.TargetMultiplier;
            return mult == 1.0 ? baseTarget : Math.Max(1, (int)Math.Round(baseTarget * mult));
        }
    }

    public QuestState(QuestDef def) => Def = def;

    // 累加。返回 true 表示本次累加让任务首次完成。
    public bool Add(int amount)
    {
        if (Completed || amount <= 0) return false;
        Current += amount;
        return CheckDone();
    }

    // 设定一个绝对值（快照类）：只在变大时推进，且永不回退。
    public bool SetAbsolute(int value)
    {
        if (Completed || value <= Current) return false;
        Current = Math.Min(value, Target);
        return CheckDone();
    }

    // 清零（单回合计数类，每回合开始调用）。
    public void Reset()
    {
        if (!Completed) Current = 0;
    }

    // 读档恢复：把存档里的进度灌回来（Current / Completed 是私有 set，只能从这里写）。
    public void Restore(int current, bool completed)
    {
        Current = Math.Clamp(current, 0, Target);
        Completed = completed || Current >= Target;
        if (Completed) Current = Target;
    }

    // 测试用：不管条件直接判完成。返回 true 表示这次真的刚完成（之前没完成过）。
    public bool ForceComplete()
    {
        if (Completed) return false;
        Current = Target;
        Completed = true;
        return true;
    }

    private bool CheckDone()
    {
        if (Current >= Target)
        {
            Current = Target;
            Completed = true;
            return true;
        }
        return false;
    }
}
