namespace QuestSpire.Quests;

// 任务给玩家的「随身物」。
//
// 为什么不做成遗物：游戏的 mod 本地化表是从 res://<ModId>/localization/<语言>/<表名>.json 读的
// （见 ModManager 里拼路径那段），那要求 mod 打 pck。本 mod 是纯 dll、没有 pck，
// 所以自己新建的遗物拿不到中文名，界面上只会显示一串内部 key。
// 于是「本体没有对应遗物」的奖励改成挂在任务册上的永久效果，由任务面板里的「随身物」一栏展示。
//
// 一代遗物类的奖励优先发真货：只要装了「更多遗物」(moreRelics) 之类的 mod，
// 就按 ID 把那件遗物直接发给玩家（见 QuestBookRelic.GrantLegacyRelic）。
// 只有找不到那件遗物时，才退回来开下面这些仿制效果。
//
// ============================================================================
// 下面这批随身物是**自己设计的**，不是从别的 mod 抄的。
// 手法来自拆解《海克斯符文》得到的「钩子菜单」：游戏在 AbstractModel 上留了
// 25 个 Should*（否决一件事）和 29 个 Modify*（改写一个数值）钩子，
// 顺着这些旋钮去设计，效果自然就多样了。每一个随身物都对应一个不同的旋钮：
//
//   HasReckless  伤害倍率 + 格挡倍率     （代价换强度：不防守，只进攻）
//   HasScholar   抽牌数 ModifyHandDraw
//   HasHaggler   商店价格 ModifyMerchantPrice
//   HasZen       篝火回复量 ModifyRestSiteHealAmount
//   HasSteady    能量上限 ModifyMaxEnergy
// ============================================================================
public static class QuestBoons
{
    // ⚠️ 这个静态表现在**只给任务面板显示用**（本地玩家那一本的镜像，见 QuestBookRelic.MirrorBoons）。
    // 真正生效的随身物是 QuestBookRelic 的**实例字段**——那样联机时每个客户端都会为每位玩家
    // 各算一遍，主客机结果一致，既不会不同步、也不用禁用。

    /// 修好的帆船：集齐锚 + 角夹 + 船长之轮 拼出来的。战斗开始 +15 格挡，第一次格挡被清空再 +20。
    public static bool HasBoat { get; set; }

    /// 仿制枯木树枝：每消耗一张牌，随机一张牌加入手牌。
    public static bool HasDeadBranch { get; set; }

    /// 仿制鸟居：战斗开始时获得 8 点格挡。
    public static bool HasTorii { get; set; }

    /// 仿制咖啡滤杯：每场战斗的第一个回合 +1 能量。
    public static bool HasDripper { get; set; }

    /// 仿制玩具扑翼飞机：每场战斗开始时回复 4 点生命。
    public static bool HasOrnithopter { get; set; }

    /// 「莽夫之道」（代价换强度）：你打出的攻击牌伤害 +50%，但你的技能牌不再提供格挡。
    /// 注意代价只针对技能牌 —— 遗物/药水给的格挡照常（第一版写成「一切格挡归零」太粗暴，已改）。
    public static bool HasReckless { get; set; }

    /// 「手不释卷」：每回合多抽 1 张牌。
    public static bool HasScholar { get; set; }

    /// 「砍价高手」：商店价格 -30%。
    public static bool HasHaggler { get; set; }

    /// 「养生」：篝火休息的回复量 +50%。
    public static bool HasZen { get; set; }

    /// 「稳如老狗」：能量上限 +1。
    public static bool HasSteady { get; set; }

    /// 「厚积薄发」：你施加的增益与减益都 +1 层。
    public static bool HasSurge { get; set; }

    /// 「铁壁」：你单次受到的伤害不超过 15 点。
    public static bool HasBulwark { get; set; }

    /// 「连环拳」：你的攻击牌多打一段。
    public static bool HasCombo { get; set; }

    /// 「冷漠」：每当你获得格挡，额外获得 3 点格挡。
    /// 梗来源：摄政王的「宇宙冷漠」，被 B 站社区当成歌来唱。
    public static bool HasIndifference { get; set; }

    /// 「盛碗虫常数」：每当你获得格挡，额外 +1 —— 正好把社区梗里那个「15 挡不住 16」补上。
    /// 来源：二幕巢穴的盛碗虫（石），头槌 15 / 9+难度 16，防住就眩晕；玩家吐槽一堆牌恰好 15。
    public static bool HasBowlbugConstant { get; set; }

    /// 「蛇花圣经」：你给敌人施加的减益层数 ×1.5。
    /// 梗来源：《尖塔梗百科》「蛇花圣经（萎靡减 8 力量）」。
    /// 用的是 ModifyPowerAmountGivenMultiplicative —— 和「厚积薄发」的加法版是**两个不同的旋钮**，
    /// 所以同时拿到两件也不会打架（先加 1，再乘 1.5）。
    public static bool HasScripture { get; set; }

    /// 「东尼意思」：卡牌奖励更容易出现升级版（ModifyCardRewardUpgradeOdds ×3）。
    /// 梗来源：《尖塔梗百科》「东尼意思」——东尼 = 制作人安东尼，
    /// 那期热评原话：「有些游戏：xxx很强→削成区；有些游戏：xxx很强→增强别的让 xxx 变得合理」。
    public static bool HasAnthony { get; set; }

    public static bool Any =>
        HasBoat || HasDeadBranch || HasTorii || HasDripper || HasOrnithopter
        || HasReckless || HasScholar || HasHaggler || HasZen || HasSteady
        || HasSurge || HasBulwark || HasCombo || HasIndifference || HasScripture
        || HasAnthony || HasBowlbugConstant;

    public static void Reset()
    {
        HasBoat = false;
        HasDeadBranch = false;
        HasTorii = false;
        HasDripper = false;
        HasOrnithopter = false;
        HasReckless = false;
        HasScholar = false;
        HasHaggler = false;
        HasZen = false;
        HasSteady = false;
        HasSurge = false;
        HasBulwark = false;
        HasCombo = false;
        HasIndifference = false;
        HasScripture = false;
        HasAnthony = false;
        HasBowlbugConstant = false;
    }
}
