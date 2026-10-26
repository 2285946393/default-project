using Godot;

namespace DouSpire.Table;

/// <summary>
/// 桌面上那种**小圆钮**（「室内」/「桌面」/「收桌」/「牌面」）的共用工厂。
///
/// 为什么抽出来：这三个角上的小圆钮，**斗地主桌和德州桌要用同一套长相**。
/// 以前它只在 <c>DouSpirePokerTable</c> 里（私有 static），斗地主想用就只能复制粘贴 ——
/// 复制粘贴的下场就是两边慢慢漂移（改了这边忘了那边）。现在两边共用这一份。
///
/// 三件事跟大按钮（动作按钮）不一样：
/// <list type="number">
///   <item>**正圆、无字**。钮上什么都不写，图标自己 <see cref="DouSpireKnobIcon"/> 画
///         （屋子 / 牌桌），名字放在钮**下面**一行小字里（<c>caption</c>）。</item>
///   <item>**不吃贴图**。不走 <c>DouSpireAssets.BtnSecondary</c>（那是所有大按钮共用的那张）。</item>
///   <item>**带 <c>douspire_big</c> 标记**。不然 <see cref="DouSpireUi"/> 的字号自动适配
///         会把写进去的小字撑成 18 号（见 <c>DouSpireUi.Apply</c>）。</item>
/// </list>
/// </summary>
public static class DouSpireKnob
{
    /// <summary>圆钮直径（1920 基准像素）。★ 2026-10-10 从 86 缩到 62 ——
    /// 用户要把这排钮收进左上角、还要"适当缩小"。</summary>
    public const float Size = 62f;

    /// <summary>圆钮底：暗金半透。</summary>
    public static readonly Color Fill = new(0.10f, 0.08f, 0.06f, 0.78f);

    /// <summary>描边圈：香槟金。</summary>
    public static readonly Color Ring = new(0.83f, 0.71f, 0.44f);

    /// <summary>图标线条色。</summary>
    public static readonly Color Icon = new(0.96f, 0.90f, 0.76f);

    /// <summary>
    /// 造一颗圆钮。
    /// </summary>
    /// <param name="kind">钮里画什么图标。</param>
    /// <param name="onPressed">点下去干什么（音效已包好，不用自己加）。</param>
    /// <param name="tooltip">鼠标悬停提示（钮上不写字，这个更有用）。</param>
    /// <param name="caption">出参：钮**下面**那行小字，已挂在钮上、内容空白等你自己填。
    /// **传 <c>false</c> 就不建**（比如「收桌」钮不需要文字）。</param>
    public static Button Make(DouSpireKnobIcon.Kind kind, System.Action onPressed,
                              out Label? caption, string tooltip = "", bool withCaption = true)
    {
        var d = DouSpireUi.Px(Size);
        var btn = new Button
        {
            Text = "",
            // ★ 这里必须写全名 `Control.FocusModeEnum` —— 本类是 static class、不是 Control 的子类，
            //   裸写 `FocusModeEnum` 编译器找不到（Godot 4 把这两个枚举嵌在 Control 里）。
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(d, d),
            TooltipText = tooltip,
        };
        btn.Pressed += () => { DouSpireSfx.Select(); onPressed(); };
        btn.MouseEntered += DouSpireSfx.Hover;   // 划过一声很轻的官方 hover 音

        btn.AddThemeStyleboxOverride("normal", Box(Fill, Ring, 0.92f, 3));
        btn.AddThemeStyleboxOverride("hover", Box(new Color(0.22f, 0.17f, 0.10f, 0.90f),
            Ring.Lightened(0.28f), 1f, 3));
        btn.AddThemeStyleboxOverride("pressed", Box(new Color(0.05f, 0.04f, 0.03f, 0.94f),
            Ring, 1f, 4));
        btn.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

        // 图标：铺满整颗钮，自己按 Size 的比例画（见 DouSpireKnobIcon）。
        var icon = new DouSpireKnobIcon { Icon = kind, Col = Icon };
        btn.AddChild(DouSpireUi.Fill(icon));

        caption = null;
        if (withCaption)
        {
            caption = MakeCaption();
            btn.AddChild(caption);
        }
        return btn;
    }

    /// <summary>钮下面那行小字。挂在钮的**底边**上往下溢 ——
    /// 按钮不是 Container，不会被子控件撑大，所以尽管让它露出去。</summary>
    public static Label MakeCaption()
    {
        var caption = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,   // ★ 同上，本类不是 Control 子类，得写全名
        };
        var lw = DouSpireUi.Px(120);
        caption.AnchorLeft = caption.AnchorRight = 0.5f;
        caption.AnchorTop = caption.AnchorBottom = 1.0f;
        caption.OffsetLeft = -lw / 2f;
        caption.OffsetRight = lw / 2f;
        caption.OffsetTop = DouSpireUi.Px(2);
        caption.OffsetBottom = DouSpireUi.Px(2 + 20);
        DouSpireUi.StyleControl(caption, 13);            // ★ 把中文字体装上（否则满屏豆腐块）
        caption.SetMeta("douspire_big", true);           // ★ 免得被自适应改成 18 号
        caption.AddThemeColorOverride("font_color", Icon);
        return caption;
    }

    /// <summary>
    /// 圆钮底：**圆角半径 = 一半边长**，正方形按钮 ⇒ 正圆。
    /// 边框给两个状态（常态略透、悬停/按下全亮），靠 <paramref name="ringAlpha"/> 区分。
    /// </summary>
    private static StyleBoxFlat Box(Color bg, Color ring, float ringAlpha, int borderWidth)
    {
        var r = Mathf.RoundToInt(DouSpireUi.Px(Size) * 0.5f);
        var bw = Mathf.Max(2, Mathf.RoundToInt(DouSpireUi.Px(borderWidth)));
        return new StyleBoxFlat
        {
            BgColor = bg,
            CornerRadiusTopLeft = r,
            CornerRadiusTopRight = r,
            CornerRadiusBottomLeft = r,
            CornerRadiusBottomRight = r,
            BorderColor = new Color(ring.R, ring.G, ring.B, ringAlpha),
            BorderWidthTop = bw,
            BorderWidthBottom = bw,
            BorderWidthLeft = bw,
            BorderWidthRight = bw,
        };
    }
}
