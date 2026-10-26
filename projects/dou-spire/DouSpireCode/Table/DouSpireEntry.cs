using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace DouSpire.Table;

/// <summary>
/// 点主菜单「斗地主尖塔」之后先弹出来的那个小菜单 —— **单人 / 多人**（用户原话：
/// 「你点那个斗地主应该是先弹出多人和单人选项。选多人然后弹三人和四人房」）。
///
/// ⚠️ 2026-10-08 改：**多人不再从这里走**。
/// 用户原话：「斗地主是选多人再走联机可能不太好。**而篝火多人代表玩家你已经在联机了**。
///   你只需要如何让主机客机坐在一起打牌」。
/// 所以多人的正路是：**先组队进 run → 到篝火 → 点「斗地主」**（见 <see cref="DouSpireNet"/>）。
/// 这一页现在只负责两件事：① 帮你把官方的组队界面打开（不然组不了队）；
/// ② 把"怎么在篝火开桌"讲清楚。
/// </summary>
public sealed partial class DouSpireEntry : Control
{
    private static DouSpireEntry? _instance;

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
            logger.Warn("DouSpire: 场景树还没准备好，入口菜单开不了（稍后再试）。");
            return;
        }
        _instance = new DouSpireEntry();
        root.AddChild(_instance);
        logger.Info("斗地主：入口菜单已打开（单人 / 多人）。");
    }

    public static void Close(Logger logger)
    {
        if (!IsOpen) return;
        _instance!.QueueFree();
        _instance = null;
    }

    private VBoxContainer _singlePage = null!;
    private VBoxContainer _multiPage = null!;
    private Label _hint = null!;

    public override void _Ready()
    {
        SetMeta(DouSpireUi.MetaTag, true);
        ProcessMode = ProcessModeEnum.Always;
        ZIndex = 260;                       // 比牌桌(200)还高 —— 入口永远在最上面
        MouseFilter = MouseFilterEnum.Stop;

        FitToViewport();
        DouSpireUi.UseScreen(this);

        // 压暗主菜单，让弹出的面板醒目
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
            DouSpireUi.BoxGlow(new Color(0.11f, 0.09f, 0.07f, 0.97f), 18, DouSpireUi.Gold, 3));
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
            Text = "斗地主尖塔",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        DouSpireUi.StyleFont(title);
        title.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        title.SetMeta("douspire_big", true);
        title.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(40));
        col.AddChild(title);

        // ── 第 1 页：单人 / 多人 ──
        _singlePage = Page();
        _singlePage.AddChild(Btn("单人（打两个电脑）", () =>
        {
            Close(Entry.Logger);
            DouSpireTable.Toggle(Entry.Logger);
        }));
        _singlePage.AddChild(Btn("多人（在篝火开桌）", () => ShowPage(1)));

        // ── 第 2 页：怎么多人 ──
        _multiPage = Page();
        _multiPage.AddChild(Btn("打开官方多人界面（去组队）", OpenOfficialMultiplayer));
        _multiPage.AddChild(Btn("← 返回", () => ShowPage(0)));

        col.AddChild(_singlePage);
        col.AddChild(_multiPage);

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

    /// <summary>整层贴住可见视口 —— 同 <c>DouSpireTable.FitToViewport</c>（根 Control 的锚点不可信）。</summary>
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
        v.AddThemeConstantOverride("separation", DouSpireUi.Gap(12));
        return v;
    }

    private static Button Btn(string text, System.Action onPressed)
    {
        var btn = new Button
        {
            Text = text,
            FocusMode = FocusModeEnum.None,
            CustomMinimumSize = new Vector2(DouSpireUi.Px(620), DouSpireUi.Px(66)),
        };
        // ★★ 2026-10-10 用户：「按钮没有声音，加个本体点按钮的声音」—— 本 mod 自造的按钮统一补上。
        btn.Pressed += () => { DouSpireSfx.Select(); onPressed(); };
        btn.MouseEntered += DouSpireSfx.Hover;
        // 跟牌桌按钮同一套外观：优先用我们自己的按钮图，其次 SDF 渐变，最后纯色兜底
        if (!DouSpireUi.ImageButton(btn, DouSpireAssets.BtnPrimary, 14, 30, 12)
            && !DouSpireUi.ApplyGradButton(btn, 12))
        {
            btn.AddThemeStyleboxOverride("normal", DouSpireUi.Box(DouSpireUi.Panel, 12, DouSpireUi.Gold, 2));
            btn.AddThemeStyleboxOverride("hover", DouSpireUi.Box(new Color(0.16f, 0.14f, 0.10f, 0.95f), 12, DouSpireUi.Gold, 2));
            btn.AddThemeStyleboxOverride("pressed", DouSpireUi.Box(DouSpireUi.Panel, 12, DouSpireUi.Gold, 3));
        }
        btn.AddThemeColorOverride("font_color", DouSpireUi.Gold);
        btn.SetMeta("douspire_big", true);
        btn.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(24));
        return btn;
    }

    private void ShowPage(int page)
    {
        _singlePage.Visible = page == 0;
        _multiPage.Visible = page == 1;
        _hint.Text = page == 0
            ? "单人：你 + 两个电脑，按标准斗地主规则打完一局。"
            : "多人怎么玩：\n"
              + "① 先组队 —— 点上面那个按钮进官方的多人界面，开个房或加入好友；\n"
              + "② 一起进 run，走到**任意一处篝火（休息处）**；\n"
              + "③ 随便谁在篝火里点「斗地主」—— 全队自动坐到同一张桌上，\n"
              + "　　各自用自己的金币下注，谁点谁开桌（主机负责发牌和判定）。\n"
              + "人不够会自动补电脑（2 个人 = 2 真人 + 1 电脑）。";
    }

    /// <summary>
    /// 打开**游戏自己的多人界面**（Host / 加入好友，都是 Steam 大厅）。
    ///
    /// 反编译 <c>sts2.dll</c> 得到的事实：主菜单 <see cref="NMainMenu"/> 有个公开属性
    /// <c>SubmenuStack</c>，游戏自己的「多人」按钮做的事就是
    /// <c>SubmenuStack.PushSubmenuType&lt;NMultiplayerSubmenu&gt;()</c> —— 我们照抄同一句。
    /// 官方开房上限写死 4 人（<c>NetHostGameService.StartSteamHost(4)</c>）。
    /// </summary>
    private void OpenOfficialMultiplayer()
    {
        try
        {
            var root = DouSpireUi.Root;
            if (root == null)
            {
                Entry.Logger.Warn("斗地主：场景树还没准备好，进不了官方联机界面。");
                return;
            }

            var menu = DouSpireUi.Descendants(root).OfType<NMainMenu>().FirstOrDefault();
            if (menu == null)
            {
                Entry.Toast("当前不在主菜单 —— 回主菜单再点这个按钮。", "斗地主");
                Entry.Logger.Warn("斗地主：当前不在主菜单，进不了官方联机界面。");
                return;
            }

            var stack = menu.SubmenuStack;
            if (stack == null)
            {
                Entry.Toast("主菜单还没就绪，稍后再点一次。", "斗地主");
                return;
            }

            // 跟游戏自己的「多人」按钮走同一句 —— 这才是官方渠道
            Close(Entry.Logger);
            stack.PushSubmenuType<NMultiplayerSubmenu>();
            Entry.Logger.Info("斗地主：已打开官方联机界面（Steam 大厅 / Host / 加入好友）。");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：打开官方联机界面失败（不影响单人和 F9）: {e}");
            Entry.Toast("官方联机界面没打开，先玩单人吧。", "DouSpire");
        }
    }
}
