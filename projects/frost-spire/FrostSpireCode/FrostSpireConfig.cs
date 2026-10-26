using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.addons.mega_text;

namespace FrostSpire;

/// <summary>
/// mod 自己的设置页（照 quest-spire 的 QuestSpireConfig 写法：BaseLib 反射 public static 属性成设置项）。
/// bool → 开关；enum → 下拉。
/// </summary>
public sealed class FrostSpireConfig : SimpleModConfig
{
    /// <summary>敌潮难度：乘在波次怪量与属性上。</summary>
    public enum WaveDifficulty { Relaxed, Normal, Hardcore }

    /// <summary>
    /// 间隙回血档位（M3.7 新增）。
    ///
    /// ★ 为什么必须有这个（用户实测「第三波就死了」的主因之一）：
    ///   16 波是**一场连续战斗**，中间没有火堆、没有商店、没有回血 —— 只有每波发一张牌。
    ///   纯消耗战，玩家的血量池根本撑不到后期。这个旋钮就是给「间隙」补上恢复。
    ///   事件波（第 5/10 波）按 2 倍档位回血，因为它们本来就是"喘息波"。
    ///
    /// 用 enum 而不是 int：BaseLib 的设置页对 bool / enum 有成熟支持，int 没验证过，不冒险。
    /// </summary>
    public enum GapHealing { Off, Low, Normal, High, VeryHigh }

    // —— 玩法 ——
    /// <summary>
    /// 凛冬模式总开关（默认开）：开启后**新开局**会发「凛冬契约」遗物，本局所有战斗变成敌潮战。
    /// 关闭时完全不影响游戏。对**已经在跑的局**不生效（要新开一局）。
    /// </summary>
    public static bool WinterMode { get; set; } = true;

    /// <summary>敌潮难度：休闲 0.6× / 正常 1× / 硬核 1.5×。</summary>
    public static WaveDifficulty Difficulty { get; set; } = WaveDifficulty.Normal;

    /// <summary>
    /// 单房间锁（M1.5）：开启后凛冬局里玩家出不了战斗房间（ShouldProceedToNextMapPoint=false）。
    /// ⚠️ M1.5 验证版是无条件锁（局打不完），应急把它关掉就能恢复原版流动，运行时生效。
    /// </summary>
    public static bool LockRoom { get; set; } = true;

    /// <summary>
    /// 部署容量限制开关。
    ///
    /// ★ 默认 **false = 不限量** —— 2026-10-06 用户原话：
    ///   「现在不用设置召唤和容量上限。首先我得测难度。多少召唤上限合适。」
    ///   所以这一版**先把限制放开**，想放几个建筑就放几个，把难度手感先测出来，再回头定数值。
    ///
    /// 打开后按 <see cref="Deploy.FrostDeployment.BaseCapacity"/> 限制（当前 6 点）。
    /// 不限量模式下 `CanDeploy` 恒 true、剩余容量显示 99（牌不会再变灰卡手）。
    /// </summary>
    public static bool LimitCapacity { get; set; } = false;

    /// <summary>
    /// ★ 凛冬起始卡组（M3.8，**新开局生效**）。
    ///
    /// 开启后：开局把角色的**原本起始卡组整副删掉**，换成 12 张凛冬卡组
    /// （4 凛冬打击 + 3 凛冬壁垒 + 暴风雪 / 冰垒 / 箭塔 / 篝火 / 冰封术）。
    /// 用户原话：「要不要直接把自己卡组全部删掉换成我们凛冬前线的卡组？」
    ///
    /// 前 7 张与本体 Strike/Defend **同费同数值**，所以换装后**开局难度不变**
    /// （波次表不用跟着重调）；多出来的 6 张功能牌短期不产生伤害（是投资），
    /// 中期靠建筑滚雪球。详见 `FrostDeck.cs` 的类注释。
    ///
    /// 关闭时保留角色原卡组，凛冬牌只从**间隙补给**进来（M3.7 及以前的行为）。
    /// </summary>
    public static bool FrostDeck { get; set; } = true;

    /// <summary>波次数值倍率（由难度换算）。</summary>
    public static double WaveMultiplier => Difficulty switch
    {
        WaveDifficulty.Relaxed => 0.6,
        WaveDifficulty.Hardcore => 1.5,
        _ => 1.0,
    };

    /// <summary>
    /// 间隙回血：每清空一波、进入间隙时回复「最大生命的百分之几」。
    /// 事件波按 **2 倍**档位回（它们是"喘息波"，不打怪）。
    /// </summary>
    public static GapHealing GapHeal { get; set; } = GapHealing.Normal;

    /// <summary>间隙回血百分比（% 最大生命）。0 = 关掉。</summary>
    public static int GapHealPercent => GapHeal switch
    {
        GapHealing.Off => 0,
        GapHealing.Low => 8,
        GapHealing.High => 25,
        GapHealing.VeryHigh => 40,
        _ => 15,
    };

    /// <summary>事件波的回血百分比 = 间隙档位 ×2（喘息波）。</summary>
    public static int EventHealPercent => GapHealPercent * 2;

    /// <summary>当前游戏语言是不是英文（游戏的语言代码是 "eng"）。</summary>
    public static bool GameIsEnglish()
    {
        try { return LocManager.Instance?.Language == "eng"; }
        catch (System.Exception) { return false; }
    }

    public override void SetupConfigUI(Control optionContainer)
    {
        base.SetupConfigUI(optionContainer);
        bool en = GameIsEnglish();
        SetLabel(optionContainer, nameof(WinterMode),
            en ? "Winter mode (new runs)" : "凛冬模式（新开局生效）");
        SetLabel(optionContainer, nameof(Difficulty),
            en ? "Wave difficulty" : "敌潮难度");
        SetLabel(optionContainer, nameof(LockRoom),
            en ? "Single-room lock (M1.5)" : "单房间锁（应急开关）");
        SetLabel(optionContainer, nameof(LimitCapacity),
            en ? "Limit deploy capacity" : "部署容量限制（关 = 不限量）");
        SetLabel(optionContainer, nameof(GapHeal),
            en ? "Heal between waves" : "间隙回血（16 波连打的唯一恢复）");
        SetLabel(optionContainer, nameof(FrostDeck),
            en ? "Replace starting deck with Frost deck" : "凛冬起始卡组（换掉角色原卡组）");
    }

    // BaseLib 生成的每一行结构是 <属性名>/Label；直接改文字比去凑它的本地化表省事。
    private static void SetLabel(Control optionContainer, string propertyName, string text)
    {
        try
        {
            optionContainer.GetNode<MegaRichTextLabel>(new NodePath(propertyName + "/Label")).Text = text;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"设置项「{propertyName}」的文字没设上: {e.Message}");
        }
    }
}
