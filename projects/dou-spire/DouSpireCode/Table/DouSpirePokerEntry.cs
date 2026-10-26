using Godot;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace DouSpire.Table;

/// <summary>
/// 点主菜单「德州扑克尖塔」之后弹出来的小菜单 —— **单人 / 玩法说明**。
///
/// 和 <see cref="DouSpireEntry"/> 是**平行的两页**（玩法二自己的一页），
/// 不共用：斗地主那页讲的是"单人 / 多人怎么进"，德州这边用户**从来没玩过**，
/// 所以最该给的是**一页规则**（用户 2026-10-09：「实话说我从来没玩过德州扑克，
/// 你自己看着办吧。主要是观众要加的。」—— 他没玩过，观众大概率也没玩过）。
///
/// 多人那条路走的是**篝火开桌**（主菜单是 run 外，没有 lobby）——
/// 联机层见 <see cref="DouSpirePokerNet"/>，P4 已接上。
/// </summary>
public sealed partial class DouSpirePokerEntry : Control
{
    private static DouSpirePokerEntry? _instance;

    public static bool IsOpen => _instance != null && GodotObject.IsInstanceValid(_instance);

    public static void Toggle(Logger logger)
    {
        if (IsOpen) Close(logger);
        else Open(logger);
    }

    public static void Open(Logger logger)
    {
        if (IsOpen) return;
        var root = DouSpireUi.Root;
        if (root == null)
        {
            logger.Warn("德州：场景树还没准备好，入口菜单开不了（稍后再试）。");
            return;
        }
        _instance = new DouSpirePokerEntry();
        root.AddChild(_instance);
        logger.Info("德州：入口菜单已打开（单人 / 玩法说明）。");
    }

    public static void Close(Logger logger)
    {
        if (!IsOpen) return;
        _instance!.QueueFree();
        _instance = null;
    }

    private VBoxContainer _mainPage = null!;
    private VBoxContainer _rulePage = null!;
    private Label _hint = null!;

    public override void _Ready()
    {
        SetMeta(DouSpireUi.MetaTag, true);
        ProcessMode = ProcessModeEnum.Always;
        ZIndex = 260;                       // 比牌桌(200)还高 —— 入口永远在最上面
        MouseFilter = MouseFilterEnum.Stop;

        FitToViewport();
        DouSpireUi.UseScreen(this);

        var dim = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.66f),
            MouseFilter = MouseFilterEnum.Stop,
        };
        AddChild(DouSpireUi.Fill(dim));

        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(DouSpireUi.Fill(center));

        var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        panel.AddThemeStyleboxOverride("panel",
            DouSpireUi.BoxGlow(new Color(0.09f, 0.07f, 0.05f, 0.97f), 18, DouSpireUi.Gold, 3));
        center.AddChild(panel);

        var pad = new MarginContainer();
        foreach (var side in new[] { "margin_left", "margin_right", "margin_top", "margin_bottom" })
            pad.AddThemeConstantOverride(side, DouSpireUi.Scaled(30));
        panel.AddChild(pad);

        var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        col.AddThemeConstantOverride("separation", DouSpireUi.Gap(16));
        pad.AddChild(col);

        var title = new Label
        {
            Text = "德州扑克尖塔",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        DouSpireUi.StyleFont(title);
        title.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        title.SetMeta("douspire_big", true);
        title.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(40));
        col.AddChild(title);

        // ── 第 1 页：单人 / 说明 ──
        _mainPage = Page();
        _mainPage.AddChild(Btn("单人（打两个电脑）", () =>
        {
            Close(Entry.Logger);
            DouSpirePokerTable.Toggle(Entry.Logger);
        }));
        _mainPage.AddChild(Btn("我没玩过 —— 先看怎么玩", () => ShowPage(1)));

        // ── 第 2 页：规则说明 ──
        _rulePage = Page();
        foreach (var line in RuleLines())
        {
            var l = new Label
            {
                Text = line,
                HorizontalAlignment = HorizontalAlignment.Left,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(DouSpireUi.Px(760), DouSpireUi.Px(0)),
            };
            DouSpireUi.StyleFont(l);
            l.AddThemeColorOverride("font_color", new Color(0.93f, 0.92f, 0.88f));
            l.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(17));
            _rulePage.AddChild(l);
        }
        _rulePage.AddChild(Btn("← 明白了，回去开桌", () =>
        {
            Close(Entry.Logger);
            DouSpirePokerTable.Toggle(Entry.Logger);
        }));
        _rulePage.AddChild(Btn("← 返回", () => ShowPage(0)));

        col.AddChild(_mainPage);
        col.AddChild(_rulePage);

        _hint = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(DouSpireUi.Px(620), DouSpireUi.Px(0)),
        };
        DouSpireUi.StyleFont(_hint);
        _hint.AddThemeColorOverride("font_color", new Color(0.72f, 0.74f, 0.78f, 0.9f));
        _hint.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(15));
        col.AddChild(_hint);

        ShowPage(0);
    }

    /// <summary>
    /// 规则说明 —— **大白话，一页看完**。
    /// 用户没玩过德州，所以这里刻意不提"位置/位置名/3-bet"这些术语，
    /// 只讲"怎么赢、什么时候该掏钱、什么时候该跑"。
    /// </summary>
    private static IEnumerable<string> RuleLines()
    {
        yield return "【目标】用你手上的 2 张底牌 + 桌上 5 张公共牌，凑出最大的 5 张牌，把别人下的钱全拿走。";
        yield return "";
        yield return "【一轮怎么走】";
        yield return "  ① 翻牌前：先发你 2 张只有你能看的底牌 —— 这时就可以开始下注；";
        yield return "  ② 翻牌：桌上翻出 3 张公共牌，再下一轮注；";
        yield return "  ③ 转牌：翻第 4 张，再下一轮注；";
        yield return "  ④ 河牌：翻第 5 张，最后一轮注；";
        yield return "  ⑤ 摊牌：还没弃牌的人把底牌翻开，谁的 5 张最大谁赢。";
        yield return "";
        yield return "【按钮就三个意思】";
        yield return "  弃牌 = 这手不玩了（已经投进去的钱不退）；";
        yield return "  过牌 / 跟注 = 不出钱或补到和别人一样多，继续玩；";
        yield return "  加注 = 往上抬价。全下 = 把面前所有筹码一次性推出去。";
        yield return "";
        yield return "【只要记住三件事】";
        yield return "  ① 牌好就加钱，牌差就弃 —— 别舍不得已经投进去的那点；";
        yield return "  ② 桌上会一直写「你现在是：一对 A」，看不懂牌型就看那一行；";
        yield return "  ③ 会写「建议跟 / 建议弃」，那是按胜率算的，新手照它走就不会吃大亏。";
        yield return "";
        yield return "【牌型大小】右上角有个「牌型速查」按钮，随时能开出来对照。";
    }

    private void FitToViewport()
    {
        var r = GetViewport()?.GetVisibleRect() ?? new Rect2(0, 0, 1920, 1080);
        if (r.Size.X < 8f || r.Size.Y < 8f) return;
        AnchorLeft = AnchorTop = AnchorRight = AnchorBottom = 0f;
        Position = r.Position;
        Size = r.Size;
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == (int)NotificationResized || what == (int)NotificationWMSizeChanged) FitToViewport();
    }

    private static VBoxContainer Page()
    {
        var v = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        v.AddThemeConstantOverride("separation", DouSpireUi.Gap(10));
        return v;
    }

    private static Button Btn(string text, System.Action onPressed)
    {
        var btn = new Button
        {
            Text = text,
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(DouSpireUi.Px(620), DouSpireUi.Px(62)),
        };
        // ★★ 2026-10-10 用户：「按钮没有声音，加个本体点按钮的声音」—— 本 mod 自造的按钮统一补上。
        btn.Pressed += () => { DouSpireSfx.Select(); onPressed(); };
        btn.MouseEntered += DouSpireSfx.Hover;
        if (!DouSpireUi.ImageButton(btn, DouSpireAssets.BtnPrimary, 14, 30, 12)
            && !DouSpireUi.ApplyGradButton(btn, 12))
        {
            btn.AddThemeStyleboxOverride("normal", DouSpireUi.Box(DouSpireUi.Panel, 12, DouSpireUi.Gold, 2));
            btn.AddThemeStyleboxOverride("hover", DouSpireUi.Box(new Color(0.16f, 0.14f, 0.10f, 0.95f), 12, DouSpireUi.Gold, 2));
            btn.AddThemeStyleboxOverride("pressed", DouSpireUi.Box(DouSpireUi.Panel, 12, DouSpireUi.Gold, 3));
        }
        btn.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        btn.SetMeta("douspire_big", true);
        btn.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(22));
        return btn;
    }

    private void ShowPage(int page)
    {
        _mainPage.Visible = page == 0;
        _rulePage.Visible = page == 1;
        _hint.Text = page == 0
            ? "单人：你 + 两个电脑。每人先发 1000 筹码，输光为止。\n"
              + "（在篝火里开桌那一版还没接，先用单人玩。）"
            : "";
    }
}
