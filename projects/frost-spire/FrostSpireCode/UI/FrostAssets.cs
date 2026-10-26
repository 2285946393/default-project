using System.Reflection;
using Godot;

namespace FrostSpire.UI;

/// <summary>
/// 素材加载层（FrostSpire 版）—— **每个槽位是一条候选链**，依次尝试：
///
/// <list type="number">
///   <item>美术放在 <c>mods\FrostSpire\assets\</c> 里的图（随时替换，不用重编 dll）</item>
///   <item>**游戏本体自带的贴图**（<c>res://…</c>）—— 面板/按钮/图标全都现成</item>
///   <item>都没有 → 返回 null，界面退回纯色块（<see cref="FrostUi.Box"/>）</item>
/// </list>
///
/// 骨架直接沿用斗地主（DouSpire）那套：候选链 + 缓存 + 开桌自检 + ClearCache 热更新。
/// 区别只在槽位表 —— 凛冬要的是「冰雪工事 / 波次面板 / 事件对话」，不是牌桌。
///
/// 两条加载路径，**不能混**：
/// <list type="bullet">
///   <item><c>res://…</c>（游戏自带）走 <see cref="ResourceLoader"/>（先 Exists 再 Load，免得刷错误日志）</item>
///   <item>其它走 <see cref="Image.LoadFromFile"/>（本 mod <c>has_pck: false</c>，
///         读不到 <c>res://FrostSpire/</c>，只能按文件系统绝对路径读）</item>
/// </list>
///
/// 所有 <c>res://</c> 路径都取自共用素材库
/// <c>projects/sts2-asset-library/</c>（1846 张，从游戏 pck 导出并逐条校验过）。
/// 写错一条只会静默退到下一候选，所以别凭记忆改路径。
/// </summary>
public static class FrostAssets
{
    // ══════════════════════════════════════════════════════════════════
    //  槽位表 —— 候选链，靠前的优先
    //  以 "res://" 开头 = 游戏自带；否则 = 我们自己 assets/ 下的相对路径
    // ══════════════════════════════════════════════════════════════════

    /// <summary>波次面板底（左上角敌潮进度面板）。游戏自带的标准九宫格弹窗面板。</summary>
    public static readonly string[] PanelBg =
    {
        "ui/panel_bg.png",
        "res://images/atlases/ui_atlas.sprites/popup_vertical.tres",
        "res://images/packed/common_ui/submenu_panel.png",
        "res://images/ui/fuzzy_nine_patch_char_select.png",
        "res://images/ui/dialogue_nine_patch.png",
    };

    /// <summary>提示条底（屏幕角落的 Toast）。</summary>
    public static readonly string[] ToastBg =
    {
        "ui/toast_bg.png",
        "res://images/ui/tiny_nine_patch.png",
        "res://images/ui/dialogue_nine_patch.png",
    };

    /// <summary>主按钮（事件选项的「主选」，金色大按钮）。</summary>
    public static readonly string[] BtnPrimary =
    {
        "buttons/btn_primary.png",
        "res://images/packed/combat_ui/end_turn_button.png",
        "res://images/atlases/ui_atlas.sprites/confirm_button.tres",
        "res://images/packed/common_ui/event_button.png",
    };

    /// <summary>次按钮（事件选项的「次选」）。</summary>
    public static readonly string[] BtnSecondary =
    {
        "buttons/btn_secondary.png",
        "res://images/packed/common_ui/event_button.png",
        "res://images/atlases/ui_atlas.sprites/proceed_button.tres",
        "res://images/atlases/ui_atlas.sprites/confirm_button.tres",
    };

    /// <summary>圆角小按钮（关闭 / 返回）。</summary>
    public static readonly string[] BtnRound =
    {
        "buttons/btn_round.png",
        "res://images/atlases/ui_atlas.sprites/back_button.tres",
        "res://images/atlases/ui_atlas.sprites/top_bar/top_bar_settings.tres",
    };

    /// <summary>标题条（波次面板顶上的横幅）。</summary>
    public static readonly string[] Title =
    {
        "ui/title_banner.png",
        "res://images/packed/run_history/banner.png",
        "res://images/ui/reward_screen/reward_banner.png",
        "res://images/ui/combat/combat_nameplate_background.png",
    };

    /// <summary>遗物「凛冬契约」的图标槽位（未画时借游戏的本体图）。</summary>
    public static readonly string[] RelicIcon =
    {
        "relics/frost_pact.png",
        "res://images/atlases/ui_atlas.sprites/top_bar/top_bar_heart.tres",
    };

    /// <summary>裂隙入口的地图节点图标（自画优先，否则借游戏的地图标记）。</summary>
    public static readonly string[] RiftNodeIcon =
    {
        "map/rift_node.png",
        "res://images/atlases/ui_atlas.sprites/map/icons/map_chest.tres",
        "res://images/atlases/ui_atlas.sprites/map/icons/map_monster.tres",
    };

    /// <summary>标题字体（游戏自带中文衬线粗体，冰雪主题很配）。</summary>
    public static readonly string[] FontTitle =
    {
        "fonts/title.otf",
        "res://fonts/zhs/SourceHanSerifSC-Bold.otf",
        "res://fonts/zhs/SourceHanSerifSC-Medium.otf",
    };

    /// <summary>
    /// 工事牌卡面：`cards/<entry>.png`（entry 用卡牌的内部 id）。
    ///
    /// ⚠️ **目前没有调用方，卡面图丢进来也看不到。** 原因：
    /// 游戏取卡图走 <c>CardModel.Portrait</c> = <c>ResourceLoader.Load("res://…card_atlas…/<entry>.tres")</c>，
    /// **从 pck 里按 entry 读**；而本 mod 是 <c>has_pck: false</c>，
    /// 且 <c>Portrait</c> **不是 virtual**，改不了它的读取路径。
    /// 要生效得走「开 pck」或「Harmony 改 Portrait 返回值」——见
    /// <c>assets/生成提示词.md</c> §5「卡面怎么接进游戏」。
    ///
    /// 这个槽位先留着当占位：M4 接 pck 时把它接上即可。
    /// </summary>
    public static string[] CardArtOf(string entry) => new[] { $"cards/{entry}.png" };

    /// <summary>开桌/开局盘点用的槽位清单（名字 → 候选链）。</summary>
    private static IEnumerable<(string Name, string[] Chain)> AllSlots()
    {
        yield return ("波次面板底", PanelBg);
        yield return ("提示条底", ToastBg);
        yield return ("主按钮", BtnPrimary);
        yield return ("次按钮", BtnSecondary);
        yield return ("圆按钮", BtnRound);
        yield return ("标题条", Title);
        yield return ("遗物图标", RelicIcon);
        yield return ("裂隙节点图标", RiftNodeIcon);
        yield return ("标题字体", FontTitle);
    }

    // ══════════════════════════════════════════════════════════════════

    /// <summary>我们自己 assets 目录的绝对路径（正斜杠）。</summary>
    public static string RootDir { get; } = ResolveRoot();

    private static readonly Dictionary<string, Texture2D?> Cache = new();
    private static readonly Dictionary<string, Font?> FontCache = new();
    private static readonly Dictionary<string, string> Chosen = new();   // 槽位 → 最终命中路径（诊断用）
    private static bool _reported;

    /// <summary>依次尝试一条候选链，返回第一张拿到的图。</summary>
    public static Texture2D? First(params string[] chain)
    {
        foreach (var path in chain)
        {
            var tex = One(path);
            if (tex != null) return tex;
        }
        return null;
    }

    /// <summary>同 <see cref="First"/>，但返回命中的那个路径（写日志用）。</summary>
    public static string? FirstHit(params string[] chain)
    {
        foreach (var path in chain)
            if (One(path) != null) return path;
        return null;
    }

    /// <summary>加载一个具体路径：`res://…` 走游戏 pck，其它走我们的 assets 目录。</summary>
    public static Texture2D? One(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (Cache.TryGetValue(path, out var cached)) return cached;

        Texture2D? tex = path.StartsWith("res://", StringComparison.Ordinal)
            ? LoadGame(path)
            : LoadOwn(path);
        Cache[path] = tex;
        return tex;
    }

    public static bool Has(string path) => One(path) != null;

    /// <summary>游戏自带的贴图/字体资源。</summary>
    private static Texture2D? LoadGame(string resPath)
    {
        try
        {
            // 先 Exists 再 Load：不存在的路径直接问会往日志里刷错误
            if (!ResourceLoader.Exists(resPath)) return null;
            return ResourceLoader.Load(resPath) as Texture2D;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"游戏资源 {resPath} 读不了（退回下一候选）: {e.Message}");
            return null;
        }
    }

    /// <summary>我们自己 assets 目录里的图。mod 没打 pck，所以只能按文件系统绝对路径读。</summary>
    private static Texture2D? LoadOwn(string rel)
    {
        try
        {
            if (string.IsNullOrEmpty(RootDir)) return null;
            var full = RootDir + "/" + rel.Replace('\\', '/');
            if (!File.Exists(full)) return null;
            var img = Image.LoadFromFile(full);
            if (img == null || img.GetWidth() <= 0) return null;
            return ImageTexture.CreateFromImage(img);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"素材 {rel} 加载失败: {e.Message}");
            return null;
        }
    }

    /// <summary>字体也走同一条候选链。</summary>
    public static Font? FirstFont(params string[] chain)
    {
        foreach (var path in chain)
        {
            if (string.IsNullOrEmpty(path)) continue;
            if (FontCache.TryGetValue(path, out var cached))
            {
                if (cached != null) return cached;
                continue;
            }

            Font? font = null;
            try
            {
                if (path.StartsWith("res://", StringComparison.Ordinal))
                {
                    if (ResourceLoader.Exists(path)) font = ResourceLoader.Load(path) as Font;
                }
                else if (!string.IsNullOrEmpty(RootDir))
                {
                    var full = RootDir + "/" + path;
                    if (File.Exists(full)) font = ResourceLoader.Load(full) as Font;
                }
            }
            catch (System.Exception e)
            {
                Entry.Logger.Warn($"字体 {path} 加载失败: {e.Message}");
            }

            FontCache[path] = font;
            if (font != null) return font;
        }
        return null;
    }

    /// <summary>
    /// 开局时清点一遍，把「哪个槽位最终用了哪张图」写进日志。
    /// 美术想知道还缺什么、或想确认自己的图有没有被用上，看这条就够了。
    /// </summary>
    public static void ReportOnce()
    {
        if (_reported) return;
        _reported = true;

        try
        {
            Chosen.Clear();
            var fromGame = new List<string>();
            var fromOwn = new List<string>();
            var none = new List<string>();

            foreach (var (name, chain) in AllSlots())
            {
                var hit = FirstHit(chain);
                if (hit == null) { none.Add(name); continue; }
                Chosen[name] = hit;
                if (hit.StartsWith("res://", StringComparison.Ordinal)) fromGame.Add($"{name}={hit}");
                else fromOwn.Add($"{name}={hit}");
            }

            Entry.Logger.Info($"凛冬素材：用自己的 {fromOwn.Count} 个，用游戏自带 {fromGame.Count} 个，"
                              + $"没有素材 {none.Count} 个。assets 目录：{RootDir}");

            if (fromOwn.Count > 0)
                Entry.Logger.Info("  自己的：" + string.Join("、", fromOwn));
            if (fromGame.Count > 0)
                Entry.Logger.Info("  借游戏的：" + string.Join("、", fromGame));
            if (none.Count > 0)
                Entry.Logger.Info("  走纯色兜底的：" + string.Join("、", none));
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"凛冬素材盘点失败（不影响玩）: {e.Message}");
        }
    }

    /// <summary>清缓存 —— 美术改完图，重开面板/重进局就能看到效果，不用重启游戏。</summary>
    public static void ClearCache()
    {
        Cache.Clear();
        FontCache.Clear();
        Chosen.Clear();
        _reported = false;
    }

    // ══════════════════════════════════════════════════════════════════

    private static string ResolveRoot()
    {
        var candidates = new List<string>();

        // 首选：用我们这个 dll 自己的位置 → 最稳（不管 exe 在哪）
        try
        {
            var loc = Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(loc))
            {
                var dir = Path.GetDirectoryName(loc);
                if (!string.IsNullOrEmpty(dir)) candidates.Add(Path.Combine(dir, "assets"));
            }
        }
        catch (System.Exception) { }

        // 兜底：游戏目录下的 mods/FrostSpire/assets
        try
        {
            var exe = OS.GetExecutablePath();
            if (!string.IsNullOrEmpty(exe))
            {
                var dir = Path.GetDirectoryName(exe);
                if (!string.IsNullOrEmpty(dir))
                    candidates.Add(Path.Combine(dir, "mods", Entry.ModId, "assets"));
            }
        }
        catch (System.Exception) { }

        foreach (var c in candidates)
        {
            try { if (Directory.Exists(c)) return c.Replace('\\', '/'); } catch (System.Exception) { }
        }
        return (candidates.FirstOrDefault() ?? "").Replace('\\', '/');
    }
}
