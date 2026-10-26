using Godot;

namespace DouSpire.Table;

/// <summary>
/// 界面基础设施：字体、屏幕缩放、贴图 NinePatch、摆放小工具、配色。
///
/// 三件事值得单独说：
///
/// **1. 字体。** 我们的界面是用 C# 代码新建的 Control，挂在场景树根部，拿不到游戏的 theme。
/// Godot 默认字体**不含中文**，不借就是满屏豆腐块。优先用游戏自带的中文衬线字体
/// （<c>res://fonts/zhs/SourceHanSerifSC-Bold.otf</c>），拿不到再退到从场景里"借"一个 Label 的字体。
///
/// **2. 尺寸按屏幕宽缩放。** 玩家可能 1920 也可能 2560（用户本机是 2560×1494），
/// 全用死像素在大屏上会小得不像话。<see cref="UseScreen"/> 算一个 1920 → 实际宽度的系数。
///
/// **3. 所有取图/取字体的入口都吃「候选链」**（参见 <see cref="DouSpireAssets"/>），
/// 取不到就返回 null，调用方各自退回纯色块。
/// </summary>
public static class DouSpireUi
{
    /// <summary>打在自己那棵子树上的标记：找字体时跳过，免得借到自己头上。</summary>
    public const string MetaTag = "douspire_ui";

    public static Node? Root => (Engine.GetMainLoop() as SceneTree)?.Root;

    /// <summary>屏幕缩放系数（1920 基准）。开桌时由 <see cref="UseScreen"/> 设定。</summary>
    public static float Screen { get; private set; } = 1f;

    public static void UseScreen(Node any)
    {
        try
        {
            var w = any.GetViewport()?.GetVisibleRect().Size.X ?? 1920f;
            Screen = Mathf.Clamp(w / 1920f, 0.6f, 2.2f);
        }
        catch (System.Exception) { Screen = 1f; }

        // 每次都按设置里的选择重取皮肤：改完设置不用重启，下次开桌就是新皮。
        DouSpireTheme.Refresh();
    }

    public static float Px(float baseSize) => baseSize * Screen * DouSpireConfig.UiScale;

    public static int Scaled(int baseSize) => Mathf.Max(8, Mathf.RoundToInt(Px(baseSize)));

    /// <summary>
    /// 容器间距，**允许负数**（让牌叠起来）。
    /// 不能用 <see cref="Scaled"/> —— 它里面有 <c>Mathf.Max(8, …)</c> 的下限，
    /// 传 -8 会被抬成 +8，想叠牌就永远叠不上（这是 2026-10-06 手牌"铺满一整行"的真凶）。
    /// </summary>
    public static int Gap(float baseSize) => Mathf.RoundToInt(Px(baseSize));

    public static float ScaledF(float baseSize) => Mathf.Max(6f, Px(baseSize));

    // ══════════════════════════ 摆放 ══════════════════════════

    /// <summary>
    /// 把一个控件**居中**放到「相对坐标 (ax,ay) + 像素偏移 (ox,oy)」处，尺寸固定为 w×h。
    /// 位置跟比例走（换分辨率不跑位），图标大小跟 <see cref="Screen"/> 走。
    /// </summary>
    public static T Place<T>(T c, float ax, float ay, float w, float h, float ox = 0f, float oy = 0f)
        where T : Control
    {
        c.AnchorLeft = c.AnchorRight = ax;
        c.AnchorTop = c.AnchorBottom = ay;
        c.OffsetLeft = ox - w / 2f;
        c.OffsetTop = oy - h / 2f;
        c.OffsetRight = ox + w / 2f;
        c.OffsetBottom = oy + h / 2f;
        return c;
    }

    /// <summary>铺满父级（当背景/遮罩用）。</summary>
    public static T Fill<T>(T c) where T : Control
    {
        // anchors + offsets 一起设，比只设 anchors 更不容易留下旧的 offset。
        c.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return c;
    }

    /// <summary>按相对坐标摆一张贴图。**候选链全空就返回 null**，调用方必须判空。</summary>
    public static TextureRect? Sprite(string[] chain, float ax, float ay, float w, float h,
                                      float ox = 0f, float oy = 0f, bool cover = false)
    {
        var tex = DouSpireAssets.First(chain);
        if (tex == null) return null;

        var tr = new TextureRect
        {
            Texture = tex,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = cover ? TextureRect.StretchModeEnum.KeepAspectCovered
                                : TextureRect.StretchModeEnum.KeepAspectCentered,
        };
        Place(tr, ax, ay, w, h, ox, oy);
        return tr;
    }

    /// <summary>铺满全屏的贴图（背景/暗角）。取不到返回 null。</summary>
    public static TextureRect? Backdrop(string[] chain)
    {
        var tex = DouSpireAssets.First(chain);
        if (tex == null) return null;
        var tr = new TextureRect
        {
            Texture = tex,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,   // 硬拉伸铺满
        };
        return Fill(tr);
    }

    // ══════════════════════════ 字体 ══════════════════════════

    /// <summary>从场景里已有的 Label 上"借"它正在用的字体（保底方案）。</summary>
    public static Font? GameFont()
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

    /// <summary>界面默认字体：游戏自带中文衬线 → 借场景 Label → 系统字体。</summary>
    public static Font? UiFont() =>
        DouSpireAssets.FirstFont(DouSpireAssets.FontTitle) ?? GameFont() ?? FallbackCjkFont();

    /// <summary>牌面点数用的字体。</summary>
    public static Font? PipFont() =>
        DouSpireAssets.FirstFont(DouSpireAssets.FontPip) ?? UiFont();

    /// <summary>给整棵子树的文本控件套用字体与字号缩放。</summary>
    public static void StyleFont(Node node)
    {
        if (node is Control c) c.SetMeta(MetaTag, true);
        Apply(node, UiFont());
    }

    /// <summary>只给**这一个**控件套字体（并递归它的子树）。挂到游戏界面上的单个按钮用。</summary>
    public static void StyleControl(Control c, int fontSize = 18)
    {
        c.SetMeta(MetaTag, true);
        var font = UiFont();
        if (font != null) c.AddThemeFontOverride("font", font);
        c.AddThemeFontSizeOverride("font_size", Scaled(fontSize));
        Apply(c, font);
    }

    /// <summary>把 <paramref name="template"/> 的外观整套抄给 <paramref name="target"/>。</summary>
    public static void BorrowLook(Button target, Control template)
    {
        try
        {
            var font = template.GetThemeFont("font") ?? GameFont();
            if (font != null) target.AddThemeFontOverride("font", font);

            var size = template.GetThemeFontSize("font_size");
            target.AddThemeFontSizeOverride("font_size", size > 0 ? size : Scaled(18));

            foreach (var state in new[] { "normal", "hover", "pressed", "focus", "disabled" })
            {
                var sb = template.GetThemeStylebox(state);
                if (sb != null) target.AddThemeStyleboxOverride(state, sb);
            }
            foreach (var col in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
            {
                var c = template.GetThemeColor(col);
                if (c.A > 0f) target.AddThemeColorOverride(col, c);
            }
            target.SetMeta(MetaTag, true);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"借用菜单按钮外观失败（只影响好看程度）: {e.Message}");
        }
    }

    private static Font? FallbackCjkFont()
    {
        try
        {
            return new SystemFont
            {
                FontNames = new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "sans-serif" },
                AllowSystemFallback = true,
            };
        }
        catch (System.Exception) { return null; }
    }

    private static void Apply(Node node, Font? font)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Control c)
            {
                if (font != null) c.AddThemeFontOverride("font", font);
                switch (c)
                {
                    case Label l:
                        if (!l.HasMeta("douspire_big"))
                            l.AddThemeFontSizeOverride("font_size", Scaled(18));
                        break;
                    case CheckBox cb: cb.AddThemeFontSizeOverride("font_size", Scaled(18)); break;
                    case Button b:
                        if (!b.HasMeta("douspire_big"))
                            b.AddThemeFontSizeOverride("font_size", Scaled(18));
                        break;
                    case RichTextLabel rt: rt.AddThemeFontSizeOverride("normal_font_size", Scaled(16)); break;
                }
            }
            Apply(child, font);
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

    // ══════════════════════════ StyleBox ══════════════════════════

    /// <summary>用一张贴图做 NinePatch 风格底（给按钮/面板当底图）。</summary>
    public static StyleBoxTexture TextureBox(Texture2D tex, int margin = 0, float padX = 0f, float padY = 0f)
    {
        var m = margin > 0 ? Scaled(margin) : 0;
        var sb = new StyleBoxTexture
        {
            Texture = tex,
            TextureMarginLeft = m,
            TextureMarginTop = m,
            TextureMarginRight = m,
            TextureMarginBottom = m,
            ContentMarginLeft = padX,
            ContentMarginRight = padX,
            ContentMarginTop = padY,
            ContentMarginBottom = padY,
        };
        return sb;
    }

    public static StyleBoxTexture? TextureBox(string[] chain, int margin = 0, float padX = 0f, float padY = 0f)
    {
        var tex = DouSpireAssets.First(chain);
        return tex == null ? null : TextureBox(tex, margin, padX, padY);
    }

    // ══════════════════════════ 横牌匾（自己切九宫格） ══════════════════════════

    /// <summary>
    /// **横牌匾**：一张"两端有装饰、中间是平纹理"的图，铺成 <paramref name="w"/>×<paramref name="h"/> 像素，
    /// 返回一个已经装好三块贴图的 <see cref="Control"/>（调用方自己 AddChild）。
    ///
    /// <para>★★ 2026-10-11 为什么**不能**用 <c>StyleBoxTexture</c>（九宫格）★★</para>
    ///
    /// <para>Godot 的九宫格**不缩放边角**。看引擎源码 <c>servers/rendering/renderer_rd/shaders/canvas.glsl</c>
    /// 里的 <c>map_ninepatch_axis()</c>，第一段就是：</para>
    /// <code>if (pixel &lt; margin_begin) return pixel * tex_pixel_size;</code>
    /// <para>目标像素和贴图像素是 **1:1** 走的 —— 也就是说 <c>texture_margin_*</c> 写 59，
    /// 那 59 个**贴图像素**就原样画在 59 个**屏幕像素**上，**跟控件大小无关**。</para>
    ///
    /// <para>我们这里的控件普遍比贴图小 3~6 倍（贴图 1024×193 画进 900×58 的框），于是：
    /// 两端装饰帽保持**原始像素大小**显得硕大，中间那段被挤成一条线 ——
    /// 用户 2026-10-11 截图里"提示牌型的那块板子"（金边粗得离谱、红底只剩一条缝）
    /// 和右下角那块木牌（两端卷草大得离谱、中间木纹竖着拉）都是这个原因。</para>
    ///
    /// <para>所以这里**自己切三块**：左右装饰帽按**高度**等比缩放（形状和原图一模一样），
    /// 中间那块单独横向拉伸 —— 那才是九宫格的本意。</para>
    /// </summary>
    /// <param name="tex">牌匾原图。</param>
    /// <param name="capL">左端装饰帽宽度（**原图像素**，从轮廓平直段量出来）。</param>
    /// <param name="capR">右端装饰帽宽度（**原图像素**）。</param>
    /// <param name="w">目标宽（屏幕像素）。</param>
    /// <param name="h">目标高（屏幕像素）。</param>
    public static Control Plate(Texture2D tex, int capL, int capR, float w, float h)
    {
        var box = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        box.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        box.Position = Vector2.Zero;
        box.Size = new Vector2(w, h);

        var texW = tex.GetWidth();
        var texH = tex.GetHeight();
        if (texW <= 0 || texH <= 0 || w <= 0f || h <= 0f) return box;

        capL = Mathf.Clamp(capL, 0, texW);
        capR = Mathf.Clamp(capR, 0, texW - capL);

        // 装饰帽按**高度**等比缩放：帽子在原图里占多高，就缩到多高 —— 形状不变。
        var s = h / texH;
        var dl = capL * s;
        var dr = capR * s;

        // 框窄到放不下两个帽子时，退化成"整体等比缩小"（只缩不拉），别让它们叠在一起。
        var mid = w - dl - dr;
        if (mid < 1f)
        {
            var k = w / Mathf.Max(1f, capL + capR);
            dl = capL * k;
            dr = capR * k;
            mid = 0f;
        }

        AddPiece(box, tex, 0, capL, 0f, dl, h);                                    // 左装饰帽
        if (mid > 1f) AddPiece(box, tex, capL, texW - capL - capR, dl, mid, h);    // 中段（横向拉伸）
        if (capR > 0) AddPiece(box, tex, texW - capR, capR, w - dr, dr, h);        // 右装饰帽
        return box;
    }

    /// <summary>牌匾里的一块：把原图 <c>[srcX, srcX+srcW)</c> 那条**整高**竖条画到 <c>(dstX,0)</c> 起、宽 <paramref name="dstW"/>、高 <paramref name="h"/>。</summary>
    private static void AddPiece(Control parent, Texture2D tex, int srcX, int srcW,
                                 float dstX, float dstW, float h)
    {
        if (srcW <= 0 || dstW <= 0f) return;

        var tr = new TextureRect
        {
            // Godot 4 的 TextureRect 去掉了 region 属性，要画原图的一块就用 AtlasTexture。
            Texture = new AtlasTexture { Atlas = tex, Region = new Rect2(srcX, 0, srcW, tex.GetHeight()) },
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
        };
        // ⚠️ 先挂进树、再写 Position/Size：Control 入树时会按 anchors 重算一次 rect，
        //    先写的话有可能被那一次重算吃掉（anchors 全 0 = TopLeft，所以写的就是最终值）。
        parent.AddChild(tr);
        tr.Position = new Vector2(dstX, 0f);
        tr.Size = new Vector2(dstW, h);
    }

    /// <summary>
    /// 给按钮套贴图外观。候选链里优先找 `<c>&lt;name&gt;_hover</c>` / `_pressed` / `_disabled`
    /// （只有我们自己的素材才有分状态图）；游戏自带的图只有一个状态，就用明暗区分。
    /// 一张都没有返回 false（调用方退回纯色 <see cref="Box"/>）。
    /// </summary>
    public static bool ImageButton(Button btn, string[] chain, int margin = 12, int padX = 24, int padY = 10)
    {
        var normal = DouSpireAssets.First(chain);
        if (normal == null) return false;

        var ownBase = chain.FirstOrDefault(p => !p.StartsWith("res://", StringComparison.Ordinal));
        var hover = ownBase != null ? DouSpireAssets.One(ownBase.Replace(".png", "_hover.png")) : null;
        var pressed = ownBase != null ? DouSpireAssets.One(ownBase.Replace(".png", "_pressed.png")) : null;
        var disabled = ownBase != null ? DouSpireAssets.One(ownBase.Replace(".png", "_disabled.png")) : null;

        var px = Scaled(padX);
        var py = Scaled(padY);

        btn.AddThemeStyleboxOverride("normal", TextureBox(normal, margin, px, py));

        var hoverSb = TextureBox(hover ?? normal, margin, px, py);
        if (hover == null) hoverSb.ModulateColor = new Color(1.18f, 1.18f, 1.18f);
        btn.AddThemeStyleboxOverride("hover", hoverSb);

        var pressSb = TextureBox(pressed ?? normal, margin, px, py);
        if (pressed == null) pressSb.ModulateColor = new Color(0.82f, 0.82f, 0.82f);
        btn.AddThemeStyleboxOverride("pressed", pressSb);

        var disSb = TextureBox(disabled ?? normal, margin, px, py);
        if (disabled == null) disSb.ModulateColor = new Color(0.5f, 0.5f, 0.52f, 0.75f);
        btn.AddThemeStyleboxOverride("disabled", disSb);

        btn.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        return true;
    }

    // ══════════════════════════ 配色 ══════════════════════════
    //
    //  这几项现在**跟着皮肤走**（见 DouSpireTheme）。选「原样」时它们的值
    //  就是第一批硬编码的那套，所以老外观一字不差地保留着。
    //  改成属性而不是 static readonly，是为了换皮时不用重新赋值整个界面。

    /// <summary>桌布色。</summary>
    public static Color Felt => DouSpireTheme.Skin.Felt;

    /// <summary>面板底色。</summary>
    public static Color Panel => DouSpireTheme.Skin.Panel;

    /// <summary>主色／强调色（边框、标题文字都用它）。</summary>
    public static Color Gold => DouSpireTheme.Skin.Accent;

    /// <summary>次要文字。</summary>
    public static Color Dim => DouSpireTheme.Skin.Dim;

    /// <summary>整屏底色（背景图缺失时的兜底）。</summary>
    public static Color Deep => DouSpireTheme.Skin.Deep;

    // 下面这几个是**语义色**，不跟皮肤变 —— 牌面就该是米白、红牌就该是红的。
    public static readonly Color CardFace = new(0.96f, 0.95f, 0.90f);
    public static readonly Color CardFaceOn = new(1.00f, 0.84f, 0.30f);
    public static readonly Color Red = new(0.72f, 0.11f, 0.11f);
    public static readonly Color Black = new(0.11f, 0.12f, 0.14f);

    /// <summary>
    /// 纯色圆角底。**全界面所有纯色底都走这里**，所以它转发给
    /// <see cref="DouSpireTheme.Panel"/> 就等于整桌一起换皮。
    /// </summary>
    public static StyleBoxFlat Box(Color bg, int radius = 10, Color? border = null, int borderWidth = 2)
        => DouSpireTheme.Panel(bg, radius, border, borderWidth, glow: false);

    /// <summary>同上，但带外发光。给大面板用（选人面板、桌子兜底、头像牌位）。</summary>
    public static StyleBoxFlat BoxGlow(Color bg, int radius = 10, Color? border = null, int borderWidth = 2)
        => DouSpireTheme.Panel(bg, radius, border, borderWidth, glow: true);

    /// <summary>
    /// 渐变按钮底。皮肤是「原样」时返回 null → 调用方退回 <see cref="Box"/>。
    /// </summary>
    public static StyleBoxTexture? GradButton(int radius, bool bright = false)
        => DouSpireTheme.Button(radius, bright);

    /// <summary>
    /// 给按钮套上整套渐变皮肤（normal/hover/pressed/disabled）。
    /// 返回 false = 当前皮肤不做渐变，调用方自己走纯色。
    /// </summary>
    public static bool ApplyGradButton(Button btn, int radius = 10, Color? disabledBg = null)
    {
        var normal = GradButton(radius);
        if (normal == null) return false;

        btn.AddThemeStyleboxOverride("normal", normal);
        btn.AddThemeStyleboxOverride("hover", GradButton(radius, bright: true) ?? normal);
        btn.AddThemeStyleboxOverride("pressed", GradButton(radius, bright: true) ?? normal);
        btn.AddThemeStyleboxOverride("disabled",
            Box(disabledBg ?? new Color(0.10f, 0.10f, 0.11f, 0.7f), radius, new Color(0.3f, 0.3f, 0.3f), 1));
        btn.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        return true;
    }
}
