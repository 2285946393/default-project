using Godot;
using STS2RitsuLib.Ui.Shell.Theme;

namespace QuestSpire.UI;

// 复用游戏自带字体/主题的小工具。
//
// ⚠️ 历史：原来是"从场景里已存在的第一个 Label 上借它正在用的字体"。
//    那要**递归遍历整棵场景树**，而且拿到的是谁，取决于场景里谁先被建出来 ——
//    同一个面板在首页进和读档进，可能借到不同的字体。
//    2026-10-08 改用 RitsuLib 的 Shell 主题令牌（`RitsuShellTheme.Current.Font.Body`）：
//    一次属性读取，拿到的就是**当前主题的正文字体**（含 CJK），跟玩家换主题联动。
//    老的走法保留在 `BorrowFontFromScene()` 里当兜底（主题还没起来时仍能出字）。
public static class UiKit
{
    public const string MetaTag = "questspire_ui";

    /// <summary>
    /// 全局基准缩放 —— 「让所有人一起变小」的那个系数。
    ///
    /// ⚠️ 为什么不能只改设置页的默认值（2026-10-11）：
    ///   BaseLib 在**第一次运行**时就把当时的默认值写进
    ///   `user://mod_configs/QuestSpire.cfg`（`ModConfig.Init()`：文件不存在就 `Save()`），
    ///   之后**永远以文件里的值为准**。
    ///   所以老玩家（包括本机那份 cfg）里躺着的 `"UiScale": "0.85"` 会**盖住**新默认值 ——
    ///   只改默认值，玩家反馈「还是太大」的问题一点都不会变。
    ///   要让**已经有存档设置的老玩家**也变小，只能动这个基准。
    ///
    /// 0.8 = 整体再小 20%。玩家自己的滑块仍然叠加在它上面（见 <see cref="Scale"/>）。
    /// </summary>
    public const float BaseShrink = 0.8f;

    /// <summary>面板 / 浮层实际用的缩放 = 玩家设置 × 全局基准。</summary>
    public static float Scale => QuestSpireConfig.UiScale * BaseShrink;

    /// <summary>
    /// 按设置页的「界面缩放」换算字号。
    ///
    /// ⚠️ 保底 6px（2026-10-11 从 8 降下来）：滑块下限放到 0.1 之后，
    ///    8px 的保底会在缩放 ~0.52 就吃满 —— 再往下拖**所有字都锁死在 8px**，
    ///    滑块后半段变成「拖了没反应」的空档。降到 6px 才让 0.1~1.4 整段都有效。
    ///    6px 已经是"还看得出是字"的下限了，再低没意义。
    /// </summary>
    public static int Scaled(int baseSize)
        => Mathf.Max(6, Mathf.RoundToInt(baseSize * Scale));

    /// <summary>按缩放换算一个像素长度（内边距 / 间距 / 控件高度用；下限 1 免得被算成 0）。</summary>
    public static int Px(float basePx) => Mathf.Max(1, Mathf.RoundToInt(basePx * Scale));

    public static Node? Root => (Engine.GetMainLoop() as SceneTree)?.Root;

    /// <summary>
    /// 游戏当前的主题字体（含 CJK）。
    ///
    /// 首选 RitsuLib 的 Shell 主题令牌 —— 这是"游戏现在用什么字体"的**准口径**，
    /// 而且是一次属性读取（老写法要遍历整棵场景树）。
    /// 主题还没初始化 / 拿不到就退回老的"借 Label 字体"。
    /// </summary>
    public static Font? GameFont()
    {
        try
        {
            var themed = RitsuShellTheme.Current.Font.Body;
            if (themed != null) return themed;
        }
        catch (System.Exception)
        {
            // 主题运行时还没起来（很早期的调用）→ 走下面的兜底，不记日志免得刷屏
        }
        return BorrowFontFromScene();
    }

    /// <summary>
    /// 兜底：从场景里已存在的第一个 Label 上"借"它正在用的字体（含 CJK、游戏风格）。
    /// 找不到返回 null。
    /// </summary>
    private static Font? BorrowFontFromScene()
    {
        var root = Root;
        if (root == null) return null;
        foreach (var node in Descendants(root))
        {
            if (node is not Label label) continue;
            if (label.HasMeta(MetaTag)) continue;
            var f = label.GetThemeFont("font");
            if (f != null) return f;
        }
        return null;
    }

    // 给整棵子树的文本控件套用字体与字号。
    public static void StyleFont(Node node, Control ourRoot)
    {
        ourRoot.SetMeta(MetaTag, true);
        var font = GameFont();
        ApplyFont(node, font);
    }

    private static void ApplyFont(Node node, Font? font)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Control c)
            {
                if (font != null) c.AddThemeFontOverride("font", font);
                // ⚠️ 别覆盖**已经显式设过字号**的控件：`QuestSelection` 的标题就是先在建的时候
                //    设成 `Scaled(26)`，然后才走到这里的 —— 无条件改会把标题也压成 18。
                if (!c.HasThemeFontSizeOverride("font_size"))
                {
                    switch (c)
                    {
                        case Label l: l.AddThemeFontSizeOverride("font_size", Scaled(18)); break;
                        case CheckBox cb: cb.AddThemeFontSizeOverride("font_size", Scaled(18)); break;
                        case Button b: b.AddThemeFontSizeOverride("font_size", Scaled(18)); break;
                    }
                }
            }
            ApplyFont(child, font);
        }
    }

    public static IEnumerable<Node> Descendants(Node n)
    {
        foreach (var ch in n.GetChildren())
        {
            yield return ch;
            foreach (var d in Descendants(ch))
                yield return d;
        }
    }
}
