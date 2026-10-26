using System.Collections.Generic;
using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace QuestSpire;

/// <summary>
/// mod 自己的设置页。
///
/// 走 **BaseLib 的配置框架**（`ModConfigRegistry`）—— 它会把这里的 public static 属性反射成设置项、
/// 自动存/读 `user://mod_configs/QuestSpire.cfg`，并把页面挂到游戏的「设置 → 模组配置」里。
/// `bool` → 开关；`enum` → 下拉。
/// </summary>
public sealed class QuestSpireConfig : SimpleModConfig
{
    /// <summary>文案语言。（枚举会自动变成下拉框）</summary>
    public enum TextLanguage { FollowGame, Chinese, English }

    /// <summary>任务难度：乘在目标数值上。</summary>
    public enum QuestDifficulty { Relaxed, Normal, Hardcore }

    // —— 界面 ——
    /// <summary>要不要显示右上角的任务侧栏（默认开）。</summary>
    public static bool ShowQuestPanel { get; set; } = true;

    /// <summary>任务行下面要不要显示「奖励：…」（默认开）。</summary>
    public static bool ShowRewardLines { get; set; } = true;

    /// <summary>
    /// 任务面板 / 选任务浮层的**界面缩放**（默认 0.85）。范围 **0.1（极小）~ 1.4（大）**。
    /// 改一下面板就按新尺度重建（`QuestPanel` 里监听）。
    ///
    /// ⚠️ 这个值只是**玩家自己的微调**。真正的"整体变小"由 `UiKit.BaseShrink`（0.8）承担 ——
    ///    因为 BaseLib 首次运行就把默认值写进 `user://mod_configs/QuestSpire.cfg`，
    ///    之后以文件为准，**改这个默认值对老玩家毫无作用**（详见 `UiKit.BaseShrink` 的注释）。
    ///    实际生效的缩放 = 本值 × `UiKit.BaseShrink`。
    ///
    /// ⚠️ 下限放到 0.1（2026-10-11，玩家要求）时要注意：字号保底已同步降到 6px
    ///    （`UiKit.Scaled`），否则低于 ~0.52x 之后所有字都锁死在 8px、拖了没反应。
    ///    面板/浮层另有最小宽度兜底（见 `QuestPanel.Width` / `QuestSelection`），
    ///    不然 0.1 时面板会缩成 26px 宽的细条。
    /// </summary>
    [ConfigSlider(0.1, 1.4, 0.05, Format = "{0:0.00}x")]
    public static float UiScale { get; set; } = 0.85f;

    // —— 玩法 ——
    /// <summary>文案语言：跟随游戏 / 强制中文 / 强制英文（默认跟随游戏）。</summary>
    public static TextLanguage Language { get; set; } = TextLanguage.FollowGame;

    /// <summary>任务难度：休闲 0.6× / 正常 1× / 硬核 1.5×（乘在目标数值上）。</summary>
    public static QuestDifficulty Difficulty { get; set; } = QuestDifficulty.Normal;

    /// <summary>无限刷新候选（默认关；开了就没有"每批只准刷一次"的限制）。</summary>
    public static bool UnlimitedRerolls { get; set; }

    /// <summary>
    /// 应急开关：多人局里**关闭「随身物」效果**（默认关 = 多人局也照常生效）。
    ///
    /// 现在随身物会在**所有客户端**统一点亮（见 `QuestBookRelic.ApplySharedBoon`），
    /// 各端战斗数值一致，所以默认开着没问题。万一哪天联机又出现不同步，可以把这个打开当保险。
    /// </summary>
    public static bool SafeMultiplayerBoons { get; set; } = false;

    /// <summary>随身物效果此刻是否真的生效（单人永远生效；多人看玩家设置）。</summary>
    public static bool BoonsActive
        => !(SafeMultiplayerBoons && Quests.QuestRunner.PlayerCount > 1);

    /// <summary>任务目标数值的倍率（由难度换算）。</summary>
    public static double TargetMultiplier => Difficulty switch
    {
        QuestDifficulty.Relaxed => 0.6,
        QuestDifficulty.Hardcore => 1.5,
        _ => 1.0,
    };

    /// <summary>当前游戏语言是不是英文（游戏的语言代码是 "eng"）。</summary>
    public static bool GameIsEnglish()
    {
        try { return LocManager.Instance?.Language == "eng"; }
        catch (System.Exception) { return false; }
    }

    public override void SetupConfigUI(Control optionContainer)
    {
        // ★ 必须在 base 之前：base 边生成每一行边去 settings_ui 查文字，
        //   表里没键就直接显示属性名（比如 "ShowQuestPanel"）。
        InjectLabels();
        base.SetupConfigUI(optionContainer);
    }

    /// <summary>
    /// 把本 mod 这批设置项的文案注入游戏的 <c>settings_ui</c> 本地化表。
    ///
    /// 原来是在 <c>base.SetupConfigUI()</c> 之后按 <c>"&lt;属性名&gt;/Label"</c> 这个**内部节点路径**
    /// 逐行改文字 —— BaseLib 一改布局就全部静默失灵（退回属性名，还不报错）。
    /// 正门是 BaseLib 自己在 <c>Config/ModConfig.cs: GetLabelText()</c> 里查的那张表：
    /// <code>
    /// LocString.GetIfExists("settings_ui", $"{ModPrefix}{Slugify(属性名)}.title")
    /// </code>
    /// <c>ModPrefix</c> = **根命名空间转大写 + "-"**（BaseLib <c>Extensions/TypePrefix.cs</c>），
    /// 所以我们是 <c>"QUESTSPIRE-"</c>。真样例（BaseLib 自带 zhs/settings_ui.json）：
    /// <c>BASELIB-RESTORE_DEFAULTS_BUTTON.title</c> = "重置为默认"。
    ///
    /// 枚举下拉的**每一项**的键是 <c>"{前缀}{大写下划线属性名}.{枚举成员名}"</c>
    /// （BaseLib <c>Config/UI/NConfigDropdown.cs</c>）；没配的话下拉里就直接显示成员名。
    ///
    /// 不缓存：开一次设置页注入一次，这样中途换语言也能立刻跟上。
    /// 出错只影响文字，绝不能让"打开设置页"崩掉。
    /// </summary>
    private static void InjectLabels()
    {
        try
        {
            var table = LocManager.Instance?.GetTable("settings_ui");
            if (table is null) return;
            bool en = GameIsEnglish();

            table.MergeWith(new Dictionary<string, string>
            {
                ["QUESTSPIRE-SHOW_QUEST_PANEL.title"]        = en ? "Show quest panel" : "显示任务面板",
                ["QUESTSPIRE-SHOW_REWARD_LINES.title"]       = en ? "Show reward text" : "任务栏显示奖励",
                ["QUESTSPIRE-UI_SCALE.title"]                = en ? "UI scale (panel & picker)" : "界面缩放（面板与选任务浮层）",
                ["QUESTSPIRE-LANGUAGE.title"]                = en ? "Text language" : "任务文案语言",
                ["QUESTSPIRE-DIFFICULTY.title"]              = en ? "Quest difficulty" : "任务难度（数值倍率）",
                ["QUESTSPIRE-UNLIMITED_REROLLS.title"]       = en ? "Unlimited rerolls" : "无限刷新候选",
                ["QUESTSPIRE-SAFE_MULTIPLAYER_BOONS.title"]  =
                    en ? "Disable boons in multiplayer (anti-desync)" : "多人局禁用随身物效果（防不同步）",

                // ── 枚举下拉的每一项 ──
                ["QUESTSPIRE-LANGUAGE.FollowGame"] = en ? "Follow game" : "跟随游戏",
                ["QUESTSPIRE-LANGUAGE.Chinese"]    = en ? "Chinese" : "强制中文",
                ["QUESTSPIRE-LANGUAGE.English"]    = en ? "English" : "强制英文",
                ["QUESTSPIRE-DIFFICULTY.Relaxed"]  = en ? "Relaxed (0.6x)" : "休闲（0.6×）",
                ["QUESTSPIRE-DIFFICULTY.Normal"]   = en ? "Normal (1x)" : "正常（1×）",
                ["QUESTSPIRE-DIFFICULTY.Hardcore"] = en ? "Hardcore (1.5x)" : "硬核（1.5×）",
            });

            Entry.Logger.Info("QuestSpire：设置项文案已注入 settings_ui。");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"QuestSpire：注入设置项文案失败（设置页会显示成属性名）: {e.Message}");
        }
    }
}
