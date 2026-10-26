using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;

namespace FrostSpire.UI;

/// <summary>
/// 界面基础设施（quest-spire UiKit 的 FrostSpire 版）：
///
/// **1. 字体。** 我们的控件挂在场景树根部，拿不到游戏 UI 的主题。优先用游戏自带的中文衬线字体
/// （<c>res://fonts/zhs/SourceHanSerifSC-Bold.otf</c>，走 <see cref="FrostAssets"/> 的候选链），
/// 拿不到再退到从场景里已存在的一个 Label 上「借」字体。
///
/// **2. 尺寸按屏幕宽缩放。** 玩家可能 1920 也可能 2560，全用死像素在大屏上会小得不像话。
/// <see cref="UseScreen"/> 算一个 1920 → 实际宽度的系数。
///
/// **3. 取图/取字体都吃「候选链」**（见 <see cref="FrostAssets"/>），取不到返回 null，
/// 调用方各自退回纯色 <see cref="Box"/>。
/// </summary>
public static class FrostUi
{
    public const string MetaTag = "frostspire_ui";

    public static Node? Root => (Engine.GetMainLoop() as SceneTree)?.Root;

    /// <summary>屏幕缩放系数（1920 基准）。开面板时由 <see cref="UseScreen"/> 设定。</summary>
    public static float Screen { get; private set; } = 1f;

    public static void UseScreen(Node any)
    {
        try
        {
            var w = any.GetViewport()?.GetVisibleRect().Size.X ?? 1920f;
            Screen = Mathf.Clamp(w / 1920f, 0.6f, 2.2f);
        }
        catch (System.Exception) { Screen = 1f; }
    }

    public static float Px(float baseSize) => baseSize * Screen;

    public static int Scaled(int baseSize) => Mathf.Max(8, Mathf.RoundToInt(Px(baseSize)));

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
        c.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        return c;
    }

    /// <summary>按相对坐标摆一张贴图。**候选链全空就返回 null**，调用方必须判空。</summary>
    public static TextureRect? Sprite(string[] chain, float ax, float ay, float w, float h,
                                      float ox = 0f, float oy = 0f, bool cover = false)
    {
        var tex = FrostAssets.First(chain);
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

    // ══════════════════════════ StyleBox ══════════════════════════

    /// <summary>用一张贴图做 NinePatch 风格底（给按钮/面板当底图）。</summary>
    public static StyleBoxTexture TextureBox(Texture2D tex, int margin = 0, float padX = 0f, float padY = 0f)
    {
        var m = margin > 0 ? Scaled(margin) : 0;
        return new StyleBoxTexture
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
    }

    public static StyleBoxTexture? TextureBox(string[] chain, int margin = 0, float padX = 0f, float padY = 0f)
    {
        var tex = FrostAssets.First(chain);
        return tex == null ? null : TextureBox(tex, margin, padX, padY);
    }

    /// <summary>
    /// 给按钮套贴图外观。候选链里优先找 `<c>&lt;name&gt;_hover</c>` / `_pressed` / `_disabled`
    /// （只有我们自己的素材才有分状态图）；游戏自带的图只有一个状态，就用明暗区分。
    /// 一张都没有返回 false（调用方退回纯色 <see cref="Box"/>）。
    /// </summary>
    public static bool ImageButton(Button btn, string[] chain, int margin = 12, int padX = 24, int padY = 10)
    {
        var normal = FrostAssets.First(chain);
        if (normal == null) return false;

        var ownBase = chain.FirstOrDefault(p => !p.StartsWith("res://", StringComparison.Ordinal));
        var hover = ownBase != null ? FrostAssets.One(ownBase.Replace(".png", "_hover.png")) : null;
        var pressed = ownBase != null ? FrostAssets.One(ownBase.Replace(".png", "_pressed.png")) : null;
        var disabled = ownBase != null ? FrostAssets.One(ownBase.Replace(".png", "_disabled.png")) : null;

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

    /// <summary>纯色兜底盒子（贴图缺失时用）。</summary>
    public static StyleBoxFlat Box(Color bg, int radius = 10, Color? border = null, int borderWidth = 2)
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

    // ══════════════════════════ 配色（贴图缺失时的兜底）══════════════════════════

    /// <summary>冰雪主题：面板底（冷蓝黑）。</summary>
    public static readonly Color Panel = new(0.07f, 0.10f, 0.14f, 0.88f);

    /// <summary>霜金（强调色）。</summary>
    public static readonly Color Gold = new(0.98f, 0.86f, 0.52f);

    /// <summary>淡蓝（次要文字）。</summary>
    public static readonly Color Dim = new(0.72f, 0.80f, 0.88f);

    /// <summary>霜白（主要文字）。</summary>
    public static readonly Color Frost = new(0.88f, 0.94f, 1.00f);

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
        FrostAssets.FirstFont(FrostAssets.FontTitle) ?? GameFont() ?? FallbackCjkFont();

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

    public static void StyleFont(Node node, Control ourRoot)
    {
        ourRoot.SetMeta(MetaTag, true);
        var font = UiFont();
        ApplyFont(node, font);
    }

    private static void ApplyFont(Node node, Font? font)
    {
        foreach (var child in node.GetChildren())
        {
            if (child is Control c)
            {
                if (font != null) c.AddThemeFontOverride("font", font);
                switch (c)
                {
                    case Label l: l.AddThemeFontSizeOverride("font_size", Scaled(14)); break;
                    case Button b: b.AddThemeFontSizeOverride("font_size", Scaled(14)); break;
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

/// <summary>
/// 波次预告面板（屏幕左上角）：凛冬敌潮进度 + 下一波阵容。
/// **可拖动**（用户要求：面板会挡别的东西）：按住面板拖到哪算哪，松手停在那。
/// 拖动手艺照抄 quest-spire QuestPanel 的 `_GuiInput` 三件套。
/// Bind 的遗物为 null 时自动隐藏。
/// </summary>
public static partial class FrostPanel
{
    /// <summary>可拖动的面板本体（按住左键拖动；MouseFilter 必须 Stop 才收得到输入）。</summary>
    private sealed partial class FrostDragPanel : PanelContainer
    {
        private bool _dragging;
        private Vector2 _dragOffset;

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed)
                {
                    _dragging = true;
                    _dragOffset = GetGlobalMousePosition() - Position;
                    AcceptEvent();
                }
                else _dragging = false;
            }
            else if (@event is InputEventMouseMotion && _dragging)
            {
                Position = GetGlobalMousePosition() - _dragOffset;
            }
        }
    }

    private static Relics.FrostPactRelic? _relic;
    private static PanelContainer? _panel;
    private static Label? _title;
    private static Label? _body;
    private static Label? _after;
    private static readonly List<Godot.ColorRect> _segments = new();

    public static void Bind(Relics.FrostPactRelic? relic)
    {
        _relic = relic;
        if (relic == null) HidePanel();
        else { ShowPanel(); }
    }

    public static void EnsurePanel()
    {
        if (_relic != null) ShowPanel();
    }

    public static void ShowPanel()
    {
        var root = FrostUi.Root;
        if (root == null) return;
        if (_panel == null) Build();
        var panel = _panel;
        if (panel == null) return;
        if (panel.GetParent() == null) root.AddChild(panel);
        Refresh();
    }

    public static void HidePanel()
    {
        _panel?.QueueFree();
        _panel = null;
        _title = null;
        _body = null;
        _after = null;
        _segments.Clear();
    }

    /// <summary>
    /// 热更新：清素材缓存 → 拆掉旧面板 → 按新素材重建。
    /// 美术改完图按一下热键就能看到效果，**不用重编译、不用重启游戏**。
    /// 面板没在台上时只清缓存（下次 ShowPanel 自然用新图）。
    /// </summary>
    public static void Reload()
    {
        FrostAssets.ClearCache();
        var had = _panel != null;
        HidePanel();
        if (had && _relic != null) ShowPanel();
    }

    public static void Refresh()
    {
        var relic = _relic;
        var title = _title;
        var body = _body;
        var after = _after;
        if (_panel == null || relic == null || title == null || body == null || after == null) return;
        int spawned = relic.SubWavesSpawned, planned = relic.SubWavesPlanned;
        title.Text = $"凛冬敌潮 {spawned}/{planned}";

        // —— 分段追踪条：一格一波，悬停可看波名 ——
        for (int i = 0; i < _segments.Count; i++)
        {
            var seg = _segments[i];
            if (planned <= 0) { seg.Color = FrostUi.Dim with { A = 0.25f }; continue; }
            Godot.Color c;
            string tip;
            if (i < planned)
            {
                var w = WaveTable.All[i];
                if (i < spawned) { c = FrostUi.Frost; tip = $"✓ {w.Title}"; }
                else if (i == spawned) { c = FrostUi.Gold; tip = $"▶ {w.Title}（当前）"; }
                else
                {
                    c = w.Kind switch
                    {
                        WaveTable.WaveKind.Elite => new Godot.Color(1f, 0.45f, 0.40f, 0.55f),
                        WaveTable.WaveKind.Boss => new Godot.Color(1f, 0.80f, 0.30f, 0.75f),
                        WaveTable.WaveKind.Event => new Godot.Color(0.72f, 0.52f, 1f, 0.55f),
                        _ => FrostUi.Dim with { A = 0.45f },
                    };
                    tip = w.Title;
                }
            }
            else { c = FrostUi.Dim with { A = 0.25f }; tip = ""; }
            seg.Color = c;
            seg.TooltipText = tip;
        }

        if (planned <= 0)
        {
            body.Text = "敌潮集结中…";
            after.Text = "";
            return;
        }
        if (spawned >= planned)
        {
            body.Text = "✓ 已肃清";
            after.Text = "";
            return;
        }

        var next = WaveTable.All[spawned];
        body.Text = $"▶ {next.Title}（{next.Monsters.Length} 只）";
        var tail = new System.Text.StringBuilder();
        for (int i = spawned + 1; i < System.Math.Min(spawned + 3, planned); i++)
            tail.Append(i > spawned + 1 ? " → " : "").Append(WaveTable.All[i].Title);
        after.Text = tail.Length > 0 ? $"之后：{tail}" : "";
    }

    private static void Build()
    {
        var panel = new FrostDragPanel();
        panel.Position = new Vector2(12, 40);   // 初始位；拖动后停在用户放的地方
        panel.MouseFilter = Control.MouseFilterEnum.Stop;   // 要收拖动输入，必须 Stop（面板小，可接受）
        panel.ProcessMode = Node.ProcessModeEnum.Always;

        // 面板底：候选链（自己的图 → 游戏九宫格 → 纯色兜底）。
        StyleBox? panelBox = (StyleBox?)FrostUi.TextureBox(FrostAssets.PanelBg, margin: 12, padX: 14, padY: 10)
                             ?? FrostUi.Box(FrostUi.Panel, 10, FrostUi.Gold, 2);
        panel.AddThemeStyleboxOverride("panel", panelBox);

        var vbox = new VBoxContainer();
        vbox.MouseFilter = Control.MouseFilterEnum.Ignore;
        vbox.AddThemeConstantOverride("separation", 4);

        // 标题（金）
        _title = new Label { Text = "凛冬敌潮" };
        _title.AddThemeColorOverride("font_color", FrostUi.Gold);

        // ★ 分段追踪条：一格一波。清掉=冰蓝，当前=亮金， upcoming 按类型着色
        //   （精英红 / BOSS 橙金 / 事件紫 / 普通=灰蓝），悬停显示波名。
        var bar = new HBoxContainer();
        bar.AddThemeConstantOverride("separation", 3);
        _segments.Clear();
        foreach (var _ in WaveTable.All)
        {
            var seg = new Godot.ColorRect
            {
                CustomMinimumSize = new Vector2(13, 15),
                MouseFilter = Control.MouseFilterEnum.Stop,   // 要悬停提示
            };
            bar.AddChild(seg);
            _segments.Add(seg);
        }

        _body = new Label { Text = "" };          // 当前波（白）
        _body.AddThemeColorOverride("font_color", FrostUi.Frost);
        _after = new Label { Text = "" };         // 之后两波（灰蓝）
        _after.AddThemeColorOverride("font_color", FrostUi.Dim);

        vbox.AddChild(_title);
        vbox.AddChild(bar);
        vbox.AddChild(_body);
        vbox.AddChild(_after);
        panel.AddChild(vbox);

        FrostUi.StyleFont(panel, panel);
        FrostUi.UseScreen(panel);
        _panel = panel;
    }
}

/// <summary>
/// 事件波的双选对话框（M2.6）。屏幕居中，两个按钮各带一个动作，点完即关。
/// v1 动作只用已验证的接口：PlayerCmd.GainGold（quest-spire 同款）+ 战斗内发牌（枯枝同款）。
/// ⚠️ 回调在点击时刻执行，用点击当下的 relic.Owner 取玩家实例（防「旧存档旧实例」坑）。
/// </summary>
public static class FrostEvents
{
    public static void ShowEvent(string title, Relics.FrostPactRelic relic)
    {
        var root = FrostUi.Root;
        if (root == null) return;

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.ProcessMode = Node.ProcessModeEnum.Always;

        // 面板底：候选链（自己的图 → 游戏九宫格 → 纯色兜底）。
        StyleBox? eventBox = (StyleBox?)FrostUi.TextureBox(FrostAssets.PanelBg, margin: 12, padX: 20, padY: 16)
                             ?? FrostUi.Box(FrostUi.Panel, 12, FrostUi.Gold, 2);
        panel.AddThemeStyleboxOverride("panel", eventBox);

        var box = new VBoxContainer();
        var t = new Label { Text = title };
        var desc = new Label { Text = DescFor(title) };
        var buttons = new HBoxContainer();

        foreach (var (label, action) in OptionsFor(title, relic))
        {
            var b = new Button { Text = label };
            // 按钮外观：候选链优先，取不到退回纯色 Box。
            if (!FrostUi.ImageButton(b, FrostAssets.BtnPrimary))
                b.AddThemeStyleboxOverride("normal", FrostUi.Box(FrostUi.Gold * 0.6f, 8, FrostUi.Gold, 2));
            b.Pressed += () =>
            {
                try { action(); }
                catch (System.Exception e) { Entry.Logger.Warn($"事件选项「{label}」执行失败: {e.Message}"); }
                panel.QueueFree();
            };
            buttons.AddChild(b);
        }

        box.AddChild(t);
        box.AddChild(desc);
        box.AddChild(buttons);
        panel.AddChild(box);

        FrostUi.StyleFont(panel, panel);
        FrostUi.UseScreen(panel);
        root.AddChild(panel);
    }

    private static string DescFor(string title) => title.Contains("拾荒者")
        ? "你在雪堆边发现了一辆翻倒的商队货车，物资还剩一半。"
        : "风雪封住了前路，队伍必须就地抉择。";

    private static (string, System.Action)[] OptionsFor(string title, Relics.FrostPactRelic relic)
        => title.Contains("拾荒者")
            ? new (string, System.Action)[]
            {
                ("翻找货箱（+80 金币）", () => _ = GrantGold(relic, 80)),
                ("分给弟兄们（2 张补给牌）", () => GrantCards(relic, 2)),
            }
            : new (string, System.Action)[]
            {
                ("掘雪开路（+100 金币）", () => _ = GrantGold(relic, 100)),
                ("搜索物资（1 张补给牌）", () => GrantCards(relic, 1)),
            };

    private static async System.Threading.Tasks.Task GrantGold(Relics.FrostPactRelic relic, int amount)
    {
        var owner = relic.Owner;
        if (owner == null) return;
        await PlayerCmd.GainGold(amount, owner);
        Entry.Logger.Info($"凛冬尖塔：事件发放 {amount} 金币。");
    }

    private static void GrantCards(Relics.FrostPactRelic relic, int count)
    {
        var owner = relic.Owner;
        if (owner?.Character?.CardPool == null) return;
        var pool = owner.Character.CardPool
            .GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint);
        var cards = CardFactory
            .GetDistinctForCombat(owner, pool, count, owner.RunState.Rng.CombatCardGeneration)
            .ToList();
        if (cards.Count == 0) return;
        _ = CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Draw, owner, CardPilePosition.Bottom);
        Entry.Logger.Info($"凛冬尖塔：事件发放 {cards.Count} 张补给牌（进抽牌堆）。");
    }
}
