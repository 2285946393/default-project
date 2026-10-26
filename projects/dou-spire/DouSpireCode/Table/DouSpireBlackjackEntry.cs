using Godot;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace DouSpire.Table;

/// <summary>
/// 点主菜单「21 点尖塔」之后弹出来的小菜单 —— **单人 / 玩法说明**。
///
/// 和 <see cref="DouSpireEntry"/>（斗地主）、<see cref="DouSpirePokerEntry"/>（德州）是**平行的三页**。
///
/// 21 点**必须给一页规则**：它的规矩（要牌停牌加倍分牌、庄家 17 点停、黑杰克赔 3:2）
/// 比斗地主和德州都短，但"什么时候该停"这件事**不看说明是真的不会**。
/// 用户自己说过没玩过德州，观众大概率也没玩过 21 点 —— 所以这里照抄德州那页的做法：一页大白话。
///
/// 篝火那条路（金币 / 血量赌注）走 <c>DouSpireBlackjackRestSiteOption</c>，
/// 这一版先只接主菜单单人筹码局。
/// </summary>
public sealed partial class DouSpireBlackjackEntry : Control
{
    private static DouSpireBlackjackEntry? _instance;

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
            logger.Warn("21 点：场景树还没准备好，入口菜单开不了（稍后再试）。");
            return;
        }
        _instance = new DouSpireBlackjackEntry();
        root.AddChild(_instance);
        logger.Info("21 点：入口菜单已打开（单人 / 玩法说明）。");
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
        ZIndex = 260;
        MouseFilter = MouseFilterEnum.Stop;

        FitToViewport();
        DouSpireUi.UseScreen(this);

        var dim = new ColorRect { Color = new Color(0f, 0f, 0f, 0.66f), MouseFilter = MouseFilterEnum.Stop };
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

        var title = new Label { Text = "21 点尖塔", HorizontalAlignment = HorizontalAlignment.Center };
        DouSpireUi.StyleFont(title);
        title.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        title.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(40));
        col.AddChild(title);

        // ── 第 1 页：单人 / 说明 ──
        _mainPage = Page();
        _mainPage.AddChild(Btn("单人（你 + 两个电脑）", () =>
        {
            Close(Entry.Logger);
            DouSpireBlackjackTable.Open(Entry.Logger);
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
        _rulePage.AddChild(Btn("← 明白了，开桌", () =>
        {
            Close(Entry.Logger);
            DouSpireBlackjackTable.Open(Entry.Logger);
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
    /// 规则说明 —— **大白话，一页看完**。刻意不提"保险 / 投降 / 算牌"这些术语。
    /// </summary>
    private static IEnumerable<string> RuleLines()
    {
        yield return "【目标】手上的牌加起来**越接近 21 越好**，但**超过 21 就直接输**。";
        yield return "　　　　你不是和旁边的人比 —— 是**和庄家比**。所以旁边的人怎么打，跟你没关系。";
        yield return "";
        yield return "【点数怎么算】";
        yield return "  2~10 = 牌面上的数；J / Q / K = 10 点；**A 可以是 1 也可以是 11**（怎么划算怎么算）。";
        yield return "  例：A + K = 21（**这就是黑杰克**，赔 1.5 倍）；A + 5 + 10 = 16（A 只能算 1，不然就爆了）。";
        yield return "";
        yield return "【一局怎么走】";
        yield return "  ① 先押注 → ② 发牌：你两张（明的），庄家两张（**一张明一张暗**）；";
        yield return "  ③ 轮到你，四个按钮随便按；④ 你定牌后电脑打，最后庄家翻开暗牌补牌；";
        yield return "  ⑤ 庄家**不到 17 点必须一直要牌**，到 17 就停 —— 它爆了你就赢。";
        yield return "";
        yield return "【四个按钮】";
        yield return "  要牌 = 再来一张（超过 21 就爆，直接输）；";
        yield return "  停牌 = 不要了，交给庄家；";
        yield return "  加倍 = 注码翻倍，但**只再发一张**就必须停（11 点的时候最好用）；";
        yield return "  分牌 = 两张点数一样时拆成两手，各补一张、**各自独立算钱**（A 和 8 一定要分）。";
        yield return "";
        yield return "【怎么算钱】";
        yield return "  黑杰克 = 赢 1.5 倍；比庄家大 = 赢 1 倍；一样大 = 平局退注；爆了或比庄家小 = 输掉。";
        yield return "";
        yield return "【实在不知道怎么办】点「提示」，它会按胜率告诉你现在该按哪个。";
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
        btn.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(22));
        return btn;
    }

    private void ShowPage(int page)
    {
        _mainPage.Visible = page == 0;
        _rulePage.Visible = page == 1;
        _hint.Text = page == 0
            ? "单人：你 + 两个电脑，各打各的，都和庄家比。\n每人 1000 筹码，输光了自动补 —— 随便玩。"
            : "";
    }
}
