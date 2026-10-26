using Godot;

namespace DouSpire.Table;

/// <summary>
/// 「该谁出牌」的倒计时环 —— 套在当前出牌那一家的头像外圈。
///
/// ── 为什么**自己画**，不用贴图 ────────────────────────────────
/// <list type="bullet">
///   <item>环要**跟着秒数实时收短**，贴图做不到（除非做几十张序列帧，还得对不上秒）；</item>
///   <item>配色要跟着主题走（<see cref="DouSpireUi.Gold"/>）—— 换 UI 风格不用重画素材；</item>
///   <item>任何分辨率下都不糊（矢量画）。</item>
/// </list>
/// 所以这里**一张图都不需要**，也就不必去魔搭出图。
///
/// ── 三种状态 ──────────────────────────────────────────────
/// <list type="bullet">
///   <item><b>计时中</b>（真人那一家）：金环从正上方顺时针收短，右下挂一枚圆形徽章写剩余秒数；</item>
///   <item><b>告急</b>（≤5 秒）：环转红 + 外面套一圈呼吸红光；</item>
///   <item><b>思考中</b>（电脑那一家）：暗环 + 一小段慢慢转的弧，不读秒
///         （电脑 1.5 秒就出牌，读 20 秒是骗人的）。</item>
/// </list>
/// </summary>
public sealed partial class DouSpireTurnTimer : Control
{
    /// <summary>环半径占控件短边的比例 —— 留出外发光和徽章的余量。</summary>
    private const float RingFrac = 0.415f;

    /// <summary>剩多少秒开始"告急"（转红 + 呼吸）。</summary>
    private const double UrgentSeconds = 5.0;

    /// <summary>正上方（Godot 的 y 轴朝下，所以 -90° 是 12 点方向）。</summary>
    private const float Top = -Mathf.Pi / 2f;

    private bool _armed;          // 真人那一家：读秒
    private bool _thinking;       // 电脑那一家：只转圈
    private double _total;        // 这一手的总秒数（0 = 不限时，显示 ∞）
    private double _endAt;        // 到点时刻（秒，来自 Time.GetTicksMsec）
    private double _now;          // 本帧的时刻（_Draw 里要反复用，缓存一下）
    private double _spin;         // 思考态那截弧的相位

    /// <summary>正在读秒吗（外部只在测试/调试里看）。</summary>
    public bool Armed => _armed;

    private static double Clock => Time.GetTicksMsec() / 1000.0;

    /// <summary>
    /// 开始读秒。<paramref name="seconds"/> ≤ 0 = 不限时（画满环 + 显示 ∞）。
    /// </summary>
    public void Arm(double seconds)
    {
        _armed = true;
        _thinking = false;
        _total = seconds > 0 ? seconds : 0.0;
        _endAt = _total > 0 ? Clock + _total : 0.0;
        _now = Clock;
        Visible = true;
        SetProcess(true);
        QueueRedraw();
    }

    /// <summary>电脑那一家：暗环 + 慢慢转的一小段弧（不读秒）。</summary>
    public void Think()
    {
        _armed = false;
        _thinking = true;
        _spin = 0.0;
        Visible = true;
        SetProcess(true);
        QueueRedraw();
    }

    /// <summary>收起来（不该显示倒计时的时候）。</summary>
    public void Clear()
    {
        _armed = false;
        _thinking = false;
        Visible = false;
        SetProcess(false);
        QueueRedraw();
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (_thinking)
        {
            _spin += delta * 1.7;
            QueueRedraw();
            return;
        }
        if (!_armed) return;
        _now = Clock;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var c = Size * 0.5f;
        var r = Mathf.Min(Size.X, Size.Y) * RingFrac;
        var w = DouSpireUi.ScaledF(5.2f);

        // ── 底盘：一圈很淡的暗环，让金弧有"槽"可嵌（纯金弧浮在头像上会显得廉价）──
        DrawArc(c, r, 0f, Mathf.Tau, 96,
                new Color(0.05f, 0.04f, 0.035f, 0.70f), w + DouSpireUi.ScaledF(2.8f), true);

        if (_thinking)
        {
            // 思考态：一截短弧慢慢绕圈
            DrawArc(c, r, (float)_spin, (float)_spin + 1.25f, 28,
                    new Color(0.74f, 0.74f, 0.80f, 0.55f), w, true);
            return;
        }

        var frac = 1f;
        if (_total > 0.0)
            frac = Mathf.Clamp((float)(Mathf.Max(0.0, _endAt - _now) / _total), 0f, 1f);

        var urgent = _total > 0.0 && (1.0 - frac) * _total <= UrgentSeconds;

        var col = urgent
            ? new Color(1f, 0.30f, 0.24f)
            : frac > 0.45f
                ? DouSpireUi.Gold
                : new Color(1f, 0.62f, 0.18f);

        // ── 告急：外面套一圈呼吸红光 ──
        if (urgent)
        {
            var breath = 0.5f + 0.5f * Mathf.Sin((float)_now * 7f);
            DrawArc(c, r + DouSpireUi.ScaledF(3.4f), 0f, Mathf.Tau, 96,
                    new Color(1f, 0.26f, 0.20f, 0.14f + 0.24f * breath),
                    DouSpireUi.ScaledF(6.4f), true);
        }

        // ── 进度弧：正上方起，顺时针收短 ──
        if (frac > 0.0005f)
        {
            var end = Top + frac * Mathf.Tau;

            // 先画一道更宽更淡的"光晕弧"，再压一道细亮弧 —— 这两层叠出来才有质感
            DrawArc(c, r, Top, end, 128, new Color(col.R, col.G, col.B, 0.22f),
                    w + DouSpireUi.ScaledF(4.4f), true);
            DrawArc(c, r, Top, end, 128, col, w, true);

            // 弧头一颗小亮点
            var head = c + new Vector2(Mathf.Cos(end), Mathf.Sin(end)) * r;
            DrawCircle(head, w * 1.9f, new Color(col.R, col.G, col.B, 0.30f));
            DrawCircle(head, w * 0.82f, new Color(1f, 1f, 1f, 0.94f));
        }

        // ── 秒数徽章：挂在环的右下 45° ──
        var badge = c + new Vector2(0.7071f, 0.7071f) * (r + DouSpireUi.ScaledF(1.6f));
        var br = DouSpireUi.ScaledF(15f);

        DrawCircle(badge + new Vector2(0f, DouSpireUi.ScaledF(1.4f)), br, new Color(0f, 0f, 0f, 0.40f));
        DrawCircle(badge, br, new Color(0.07f, 0.06f, 0.05f, 0.95f));
        DrawArc(badge, br - DouSpireUi.ScaledF(1.3f), 0f, Mathf.Tau, 48, col, DouSpireUi.ScaledF(2.2f), true);

        var font = DouSpireUi.UiFont();
        if (font is null) return;

        var fs = DouSpireUi.Scaled(17);
        var txt = _total <= 0.0
            ? "∞"
            : Mathf.Max(0, Mathf.CeilToInt((float)(_endAt - _now))).ToString();

        var sz = font.GetStringSize(txt, HorizontalAlignment.Left, -1, fs);
        var at = badge + new Vector2(
            -sz.X * 0.5f,
            (font.GetAscent(fs) - font.GetDescent(fs)) * 0.5f);

        DrawString(font, at, txt, HorizontalAlignment.Left, -1, fs, col);
    }
}
