using Godot;

namespace DouSpire.Table;

/// <summary>
/// 桌面 UI 的整体风格。设置页里是一个下拉。
///
/// ⚠️ **成员名故意写成中文**：BaseLib 的下拉标签是
/// `LocString.GetIfExists("settings_ui", "&lt;前缀&gt;ui_theme.&lt;成员名&gt;") ?? 成员名`。
/// 我们这种纯 dll 的 mod 没有本地化表，会直接退回**成员名本身**当显示文字 ——
/// 所以成员名写什么，玩家就在下拉里看到什么。C# 标识符允许中文，这是最省事的做法。
/// </summary>
public enum DouSpireUiTheme
{
    /// <summary>原样 —— 深绿毡 + 金边那一套，不留光晕。想退回改造前就选它。</summary>
    原样 = 0,

    /// <summary>崩坏：星穹铁道风 —— 深蓝黑玻璃 + 冷白蓝细边 + 金。</summary>
    星穹铁道 = 1,

    /// <summary>原神风 —— 米白亮面 + 金褐细边 + 深褐色文字。</summary>
    原神 = 2,

    /// <summary>明日方舟风 —— 白灰极简 + 纯黑细边 + 荧光青点缀。</summary>
    明日方舟 = 3,

    /// <summary>蔚蓝档案风 —— 浅蓝白 + 高饱和天蓝边 + 大圆角。</summary>
    蔚蓝档案 = 4,
}

/// <summary>
/// 一套皮肤的配色与形状参数。<see cref="DouSpireTheme"/> 里按 <see cref="DouSpireUiTheme"/> 取值。
/// </summary>
public sealed class DouSpireSkin
{
    public required string Label;

    /// <summary>面板底色（默认面板、头像牌位底）。</summary>
    public required Color Panel;

    /// <summary>细亮边的颜色。**这是二游感最关键的一项** —— 原来的金粗边换成它。</summary>
    public required Color Border;

    /// <summary>主色／强调色。原来代码里的 <c>DouSpireUi.Gold</c> 会指向它。</summary>
    public required Color Accent;

    /// <summary>次要文字。</summary>
    public required Color Dim;

    /// <summary>桌布（纯色兜底时用）。</summary>
    public required Color Felt;

    /// <summary>整屏底色（背景图缺失时用）。</summary>
    public required Color Deep;

    /// <summary>光晕颜色。</summary>
    public required Color Glow;

    /// <summary>渐变按钮的上沿色。</summary>
    public required Color BtnTop;

    /// <summary>渐变按钮的下沿色。</summary>
    public required Color BtnBottom;

    /// <summary>圆角放大系数。二游普遍比传统 UI 更圆。</summary>
    public float RadiusK = 1f;

    /// <summary>边框粗细系数。二游是**细**边，所以通常 &lt; 1。</summary>
    public float BorderK = 1f;

    /// <summary>光晕强度系数。<c>0</c> = 完全不发光。</summary>
    public float GlowK = 0f;

    /// <summary>是否亮色主题（决定文字用深色还是浅色）。</summary>
    public bool Light = false;
}

/// <summary>
/// 二游风格皮肤层 —— 一行代码就能把整桌换皮。
///
/// 思路是**一个汇聚点**：界面里所有纯色底都走 <see cref="DouSpireUi.Box"/>，
/// 所以只要把它改成转发到这里，全桌的边、圆角、光晕一起变，不用逐个控件改。
///
/// 两条实现路径：
/// <list type="bullet">
///   <item><b>面板</b> → <see cref="StyleBoxFlat"/>：圆角 + 细亮边，大面板再加
///         <c>ShadowColor/ShadowSize</c> 当外发光（偏移设 0 就是匀光晕）。</item>
///   <item><b>按钮</b> → <see cref="StyleBoxTexture"/>：<see cref="MakeButtonTex"/>
///         现算一张带**竖直渐变 + 圆角 + 细边 + 外发光**的九宫格图。
///         StyleBoxFlat 只有纯色，做不出二游那种上亮下暗的按钮，所以这里必须现画。</item>
/// </list>
///
/// 选 <see cref="DouSpireUiTheme.Classic"/> 时 <see cref="On"/> 为 false，
/// <see cref="Panel"/> 原样返回，等价于改造前 —— 随时可退。
/// </summary>
public static partial class DouSpireTheme
{
    // ════════════════════════════════════════════════════
    //  预设
    // ════════════════════════════════════════════════════

    private static readonly DouSpireSkin ClassicSkin = new()
    {
        Label = "原样",
        Panel = new Color(0.07f, 0.10f, 0.12f, 0.85f),
        Border = new Color(0.98f, 0.86f, 0.52f),
        Accent = new Color(0.98f, 0.86f, 0.52f),
        Dim = new Color(0.72f, 0.76f, 0.82f),
        Felt = new Color(0.05f, 0.24f, 0.16f, 0.97f),
        Deep = new Color(0.045f, 0.055f, 0.075f, 1f),
        Glow = new Color(0.98f, 0.86f, 0.52f, 0f),
        BtnTop = new Color(0.16f, 0.14f, 0.10f),
        BtnBottom = new Color(0.07f, 0.10f, 0.12f),
        RadiusK = 1f, BorderK = 1f, GlowK = 0f, Light = false,
    };

    /// <summary>崩铁：深蓝黑玻璃底，冷白蓝细边勾轮廓，金做强调。</summary>
    private static readonly DouSpireSkin StarRailSkin = new()
    {
        Label = "崩坏：星穹铁道",
        Panel = new Color(0.043f, 0.070f, 0.110f, 0.86f),
        Border = new Color(0.643f, 0.800f, 0.914f, 0.94f),   // #A4CCE9 冷白蓝
        Accent = new Color(0.937f, 0.812f, 0.494f),          // #EFCF7E 金
        Dim = new Color(0.596f, 0.667f, 0.753f),
        Felt = new Color(0.043f, 0.086f, 0.129f, 0.97f),
        Deep = new Color(0.024f, 0.035f, 0.055f, 1f),
        Glow = new Color(0.435f, 0.694f, 0.937f, 0.42f),     // #6FB1EF 冷蓝光
        BtnTop = new Color(0.157f, 0.251f, 0.376f, 0.97f),
        BtnBottom = new Color(0.055f, 0.098f, 0.161f, 0.99f),
        RadiusK = 1.15f, BorderK = 0.7f, GlowK = 1.0f, Light = false,
    };

    /// <summary>原神：米白亮面 + 金褐细边 + 深褐文字（亮色主题）。</summary>
    private static readonly DouSpireSkin GenshinSkin = new()
    {
        Label = "原神",
        Panel = new Color(0.949f, 0.937f, 0.902f, 0.92f),    // #F2EFE6
        Border = new Color(0.784f, 0.663f, 0.416f, 0.95f),   // #C8A96A 金褐
        Accent = new Color(0.545f, 0.416f, 0.204f),          // #8B6A34 深金褐（当文字用要有对比）
        Dim = new Color(0.420f, 0.392f, 0.333f),
        Felt = new Color(0.925f, 0.914f, 0.878f, 0.95f),
        Deep = new Color(0.910f, 0.898f, 0.867f, 1f),
        Glow = new Color(0.902f, 0.788f, 0.510f, 0.38f),
        BtnTop = new Color(0.988f, 0.976f, 0.949f, 0.99f),
        BtnBottom = new Color(0.882f, 0.851f, 0.788f, 0.99f),
        RadiusK = 1.1f, BorderK = 0.75f, GlowK = 0.7f, Light = true,
    };

    /// <summary>方舟：白灰极简底，纯黑细边，荧光青只用在强调处。</summary>
    private static readonly DouSpireSkin ArknightsSkin = new()
    {
        Label = "明日方舟",
        Panel = new Color(0.941f, 0.949f, 0.945f, 0.93f),    // #F0F2F1
        Border = new Color(0.129f, 0.145f, 0.161f, 0.95f),   // #212527 近黑
        Accent = new Color(0.086f, 0.565f, 0.522f),          // #16908 5 荧光青（压深一档当文字）
        Dim = new Color(0.376f, 0.400f, 0.412f),
        Felt = new Color(0.898f, 0.910f, 0.902f, 0.96f),
        Deep = new Color(0.878f, 0.890f, 0.886f, 1f),
        Glow = new Color(0.212f, 0.808f, 0.749f, 0.34f),
        BtnTop = new Color(0.976f, 0.984f, 0.980f, 0.99f),
        BtnBottom = new Color(0.847f, 0.867f, 0.859f, 0.99f),
        RadiusK = 0.8f, BorderK = 0.7f, GlowK = 0.55f, Light = true,
    };

    /// <summary>蔚蓝档案：浅蓝白 + 高饱和天蓝边 + 大圆角。</summary>
    private static readonly DouSpireSkin BlueArchiveSkin = new()
    {
        Label = "蔚蓝档案",
        Panel = new Color(0.937f, 0.965f, 0.992f, 0.93f),    // #EFF6FD
        Border = new Color(0.278f, 0.671f, 0.949f, 0.98f),   // #47ABF2 天蓝
        Accent = new Color(0.086f, 0.482f, 0.792f),          // #167CC9 深天蓝（文字用）
        Dim = new Color(0.373f, 0.451f, 0.529f),
        Felt = new Color(0.901f, 0.941f, 0.980f, 0.96f),
        Deep = new Color(0.878f, 0.929f, 0.976f, 1f),
        Glow = new Color(0.365f, 0.733f, 0.980f, 0.40f),
        BtnTop = new Color(0.984f, 0.996f, 1.000f, 1f),
        BtnBottom = new Color(0.827f, 0.906f, 0.976f, 1f),
        RadiusK = 1.5f, BorderK = 0.95f, GlowK = 0.85f, Light = true,
    };

    private static readonly Dictionary<DouSpireUiTheme, DouSpireSkin> Presets = new()
    {
        [DouSpireUiTheme.原样] = ClassicSkin,
        [DouSpireUiTheme.星穹铁道] = StarRailSkin,
        [DouSpireUiTheme.原神] = GenshinSkin,
        [DouSpireUiTheme.明日方舟] = ArknightsSkin,
        [DouSpireUiTheme.蔚蓝档案] = BlueArchiveSkin,
    };

    // ════════════════════════════════════════════════════
    //  当前皮肤
    // ════════════════════════════════════════════════════

    private static DouSpireSkin? _skin;

    public static DouSpireSkin Skin
    {
        get { Ensure(); return _skin!; }
    }

    /// <summary>皮肤是否生效（选了「原样」就是 false，此时一切走老逻辑）。</summary>
    public static bool On => !ReferenceEquals(Skin, ClassicSkin);

    /// <summary>按设置里的选择重新取皮肤。开桌时调一次，改设置不用重启。</summary>
    public static void Refresh()
    {
        DouSpireUiTheme want;
        try { want = DouSpireConfig.UiTheme; }
        catch (System.Exception) { want = DouSpireUiTheme.星穹铁道; }

        if (Presets.TryGetValue(want, out var skin))
            _skin = skin;
        else
            _skin = StarRailSkin;

        ButtonCache.Clear();      // 比例/光晕可能变了，重画按钮图
    }

    private static void Ensure()
    {
        if (_skin == null) Refresh();
    }

    // ════════════════════════════════════════════════════
    //  配置读入（都带兜底，读不到就用 1）
    // ════════════════════════════════════════════════════

    private static float Cfg(System.Func<float> get, float fallback)
    {
        try { return get(); }
        catch (System.Exception) { return fallback; }
    }

    /// <summary>圆角放大（设置页滑条）。</summary>
    public static float RadiusMul => Mathf.Clamp(Cfg(() => DouSpireConfig.UiRadius, 1f), 0.3f, 2.5f);

    /// <summary>光晕强度（设置页滑条）。</summary>
    public static float GlowMul => Mathf.Clamp(Cfg(() => DouSpireConfig.UiGlow, 1f), 0f, 3f);

    /// <summary>面板不透明度（设置页滑条）。</summary>
    public static float AlphaMul => Mathf.Clamp(Cfg(() => DouSpireConfig.UiPanelAlpha, 1f), 0.2f, 1f);

    // ════════════════════════════════════════════════════
    //  面板 / 按钮
    // ════════════════════════════════════════════════════

    /// <summary>
    /// 纯色面板。<paramref name="bg"/> 由调用方给（它带着语义，比如牌面是米白），
    /// 这里只改**边**、**圆角**、**光晕**这三样 + 轻微压透明度。
    /// </summary>
    public static StyleBoxFlat Panel(Color bg, int radius, Color? border, int borderWidth, bool glow)
    {
        var s = Skin;
        if (!On) return Plain(bg, radius, border, borderWidth);

        var r = Mathf.RoundToInt(radius * s.RadiusK * RadiusMul);
        var bw = border.HasValue
            ? Mathf.Max(1, Mathf.RoundToInt(borderWidth * s.BorderK))
            : 0;
        var bc = border.HasValue ? border.Value.Lerp(s.Border, 0.88f) : s.Border;

        var b = AlphaMul < 1f
            ? new Color(bg.R, bg.G, bg.B, bg.A * AlphaMul)
            : bg;

        var sb = Plain(b, r, bc, bw);
        sb.AntiAliasing = true;
        sb.CornerDetail = 8;

        // 「外发光」= 偏移 0 的阴影。Godot 的 StyleBoxFlat 只有这一个发光手段。
        var g = s.GlowK * GlowMul;
        if (glow && g > 0.01f)
        {
            sb.ShadowColor = new Color(s.Glow.R, s.Glow.G, s.Glow.B, s.Glow.A * Mathf.Min(1f, g));
            sb.ShadowSize = Mathf.Max(1, Mathf.RoundToInt(DouSpireUi.Px(12f * g)));
            sb.ShadowOffset = Vector2.Zero;      // 偏移 0 → 四周匀光，不是投影
        }
        return sb;
    }

    /// <summary>改造前的样子（不加光、不改色）。</summary>
    public static StyleBoxFlat Plain(Color bg, int radius, Color? border, int borderWidth)
    {
        var sb = new StyleBoxFlat
        {
            BgColor = bg,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 6,
            ContentMarginBottom = 6,
        };
        if (border.HasValue)
        {
            sb.BorderColor = border.Value;
            sb.BorderWidthLeft = borderWidth;
            sb.BorderWidthRight = borderWidth;
            sb.BorderWidthTop = borderWidth;
            sb.BorderWidthBottom = borderWidth;
        }
        return sb;
    }

    /// <summary>
    /// 渐变按钮。二游的按钮基本都长得一样：上亮下暗的竖渐变 + 细亮边 + 圆角 + 外发光。
    /// StyleBoxFlat 出不了渐变，所以现画一张九宫格图。
    /// </summary>
    public static StyleBoxTexture? Button(int radius, bool bright)
    {
        var s = Skin;
        if (!On) return null;

        var key = $"{(int)radius}|{(bright ? 1 : 0)}|{(int)(s.RadiusK * 100 * RadiusMul)}|{(int)(GlowMul * 100)}";
        if (ButtonCache.TryGetValue(key, out var hit)) return hit;

        var r = Mathf.Max(4, Mathf.RoundToInt(radius * s.RadiusK * RadiusMul));
        var glow = Mathf.Max(1, Mathf.RoundToInt(DouSpireUi.Px(9f * s.GlowK * GlowMul)));
        var border = Mathf.Max(1, Mathf.RoundToInt(DouSpireUi.Px(2f * s.BorderK)));

        // 悬停/按下 = 整体提亮 / 压暗，另外边色更亮
        var top = bright ? s.BtnTop.Lightened(0.14f) : s.BtnTop;
        var bot = bright ? s.BtnBottom.Lightened(0.10f) : s.BtnBottom;
        var bcl = bright ? s.Border.Lightened(0.25f) : s.Border;

        var tex = MakeButtonTex(top, bot, bcl, s.Glow, r, border, glow, s.Light);
        if (tex == null) return null;

        var sb = new StyleBoxTexture
        {
            Texture = tex,
            TextureMarginLeft = r + glow,
            TextureMarginTop = r + glow,
            TextureMarginRight = r + glow,
            TextureMarginBottom = r + glow,
            AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Stretch,
            AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Stretch,
            ContentMarginLeft = DouSpireUi.ScaledF(30),
            ContentMarginRight = DouSpireUi.ScaledF(30),
            ContentMarginTop = DouSpireUi.ScaledF(12),
            ContentMarginBottom = DouSpireUi.ScaledF(12),
        };
        ButtonCache[key] = sb;
        return sb;
    }

    private static readonly Dictionary<string, StyleBoxTexture> ButtonCache = new();

    /// <summary>
    /// 现画一张「圆角竖渐变 + 细边 + 外发光」的九宫格图。
    ///
    /// 用**圆角矩形的有符号距离场**来判断每个像素该是底色、边还是光晕：
    /// <c>d&lt;0</c> 在形状内、<c>d&gt;0</c> 在外面。这样圆角和边缘消锯齿都是解析算的，
    /// 不用超采样，代码也短。
    /// </summary>
    private static Texture2D? MakeButtonTex(Color top, Color bottom, Color border,
                                            Color glowCol, int radius, int borderW,
                                            int glowPad, bool light)
    {
        try
        {
            // 九宫格要求 left+right < width、top+bottom < height，所以留够余量
            var m = radius + glowPad;
            var w = m * 2 + 48;
            var h = m * 2 + 40;

            var hw = (w - 2f * glowPad) * 0.5f;
            var hh = (h - 2f * glowPad) * 0.5f;
            var cx = w * 0.5f;
            var cy = h * 0.5f;
            var r = Mathf.Min(radius, Mathf.Min(hw, hh));

            var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);

            for (var y = 0; y < h; y++)
            {
                for (var x = 0; x < w; x++)
                {
                    var px = x + 0.5f - cx;
                    var py = y + 0.5f - cy;

                    var qx = Mathf.Abs(px) - (hw - r);
                    var qy = Mathf.Abs(py) - (hh - r);
                    var ox = Mathf.Max(qx, 0f);
                    var oy = Mathf.Max(qy, 0f);
                    var d = Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;

                    Color col; float a;

                    if (d <= 0f)
                    {
                        // 形状内：竖直渐变。t: 0=上 1=下
                        var t = Mathf.Clamp((py + hh) / Mathf.Max(1f, hh * 2f), 0f, 1f);
                        col = top.Lerp(bottom, t);
                        // 顶部一条很淡的玻璃高光，二游感主要来自这一条
                        var sheen = Mathf.Clamp(1f - t / 0.42f, 0f, 1f);
                        if (sheen > 0f)
                            col = col.Lerp(new Color(1f, 1f, 1f, col.A), sheen * (light ? 0.28f : 0.14f));
                        a = col.A;
                        if (d > -borderW) { col = border; a = border.A; }
                    }
                    else
                    {
                        var k = Mathf.Clamp(1f - d / Mathf.Max(1f, glowPad), 0f, 1f);
                        col = glowCol;
                        a = glowCol.A * k * k;
                    }

                    // 边缘 1px 消锯齿
                    a *= Mathf.Clamp(0.5f - d, 0f, 1f);
                    img.SetPixel(x, y, new Color(col.R, col.G, col.B, a));
                }
            }

            return ImageTexture.CreateFromImage(img);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"渐变按钮图生成失败（按钮退回纯色）: {e.Message}");
            return null;
        }
    }

    // ════════════════════════════════════════════════════
    //  角饰
    // ════════════════════════════════════════════════════

    /// <summary>
    /// 四角的小直角 + 菱形装饰。二游的面板四角几乎必有这个，加一两行代码就能显著"变贵"。
    /// 自己 <c>_Draw</c>，不依赖任何贴图。
    /// </summary>
    public sealed partial class Corners : Control
    {
        public Color Col = new(0.64f, 0.80f, 0.91f, 0.85f);
        public float Len = 18f;
        public float W = 2f;

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationResized) QueueRedraw();
        }

        public override void _Draw()
        {
            var s = Size;
            var l = Len;
            var t = Mathf.Max(1f, W);

            Bracket(0f, 0f, 1f, 1f);
            Bracket(s.X, 0f, -1f, 1f);
            Bracket(0f, s.Y, 1f, -1f);
            Bracket(s.X, s.Y, -1f, -1f);

            void Bracket(float x, float y, float sx, float sy)
            {
                DrawLine(new Vector2(x, y + sy * l), new Vector2(x, y), Col, t, true);
                DrawLine(new Vector2(x, y), new Vector2(x + sx * l, y), Col, t, true);

                // 角上那颗小菱形
                var d = t * 2.6f;
                var p = new Vector2(x + sx * d, y + sy * d);
                DrawColoredPolygon(new[]
                {
                    p + new Vector2(0f, -d * 0.8f),
                    p + new Vector2(d * 0.8f, 0f),
                    p + new Vector2(0f, d * 0.8f),
                    p + new Vector2(-d * 0.8f, 0f),
                }, Col);
            }
        }
    }

    /// <summary>给一个控件挂上四角装饰。选「原样」主题时返回 null（不挂）。</summary>
    public static Corners? Decorate(Control host, float len = 18f)
    {
        if (!On) return null;
        try
        {
            var c = new Corners
            {
                Col = Skin.Border,
                Len = DouSpireUi.ScaledF(len),
                W = Mathf.Max(1f, DouSpireUi.ScaledF(2f)),
            };
            DouSpireUi.Fill(c);
            host.AddChild(c);
            return c;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"角饰没挂上（只影响好看程度）: {e.Message}");
            return null;
        }
    }
}
