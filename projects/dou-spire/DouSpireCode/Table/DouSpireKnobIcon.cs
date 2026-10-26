using Godot;

namespace DouSpire.Table;

/// <summary>
/// 德州桌面上那两个小圆钮里的**矢量图标**（2026-10-09，用户：
/// 「**做两个小圆圈型的就行了，还可以镶嵌在桌子上**」）。
///
/// 为什么自己画、不吃贴图：
/// <list type="bullet">
///   <item>**马上就能看效果** —— 现在 <c>assets/buttons/</c> 里一张图都没有，
///         等着出图就得多等一轮。矢量画出来当场能验收。</item>
///   <item>**跟 7 档牌桌都配** —— 图标是**单色**的（圆钮那边给香槟金），
///         压在绿绒 / 黄铜 / 黑曜石 / 冰蓝水晶 / 二次元 / 漆器 / 蒸汽朋克
///         任何一张台面上都读得出来，不会跟台面的花纹打架。</item>
///   <item>**跟着圆钮自动缩放** —— 尺寸全按 <see cref="Size"/> 的百分比算，
///         换分辨率 / 改 UiScale 都不用重新切图。</item>
/// </list>
///
/// 两个图形：
/// <list type="bullet">
///   <item><see cref="Kind.Room"/> = 一间屋子（三角屋顶 + 方墙 + 一扇门）
///         → 切**室内**（牌室场景）。</item>
///   <item><see cref="Kind.Desk"/> = 从上往下看的一张椭圆牌桌（台面 + 桌心三张摊开的牌）
///         → 切**桌面**（牌桌台面）。</item>
/// </list>
///
/// ⚠️ <see cref="MouseFilter"/> 必须是 <c>Ignore</c>（见 <see cref="_Ready"/>）——
/// 不然图标把鼠标吃掉了，圆钮点不动、划过也不出 hover。
/// </summary>
public sealed partial class DouSpireKnobIcon : Control
{
    public enum Kind
    {
        /// <summary>屋子 —— 切室内 / 牌室场景。</summary>
        Room = 0,

        /// <summary>牌桌台面 —— 切桌面 / 牌桌台面。</summary>
        Desk = 1,

        /// <summary>叠着的三张牌 —— 切**牌面**（经典 / 尖塔 / 烫金）。★ 2026-10-10 加。</summary>
        Card = 2,

        /// <summary>一个叉（×）—— **收桌**。★ 2026-10-10 加。</summary>
        Close = 3,
    }

    /// <summary>画哪种图。</summary>
    public Kind Icon = Kind.Room;

    /// <summary>线条颜色（圆钮那边给的是香槟金）。</summary>
    public Color Col = new(0.96f, 0.90f, 0.76f);

    public override void _Ready()
    {
        // ★ 必须 Ignore：这是纯装饰，鼠标要能穿过去落到下面的圆钮上。
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Notification(int what)
    {
        // 尺寸变了要重画（圆钮的尺寸跟着屏幕缩放走）。
        if (what == NotificationResized) QueueRedraw();
    }

    public override void _Draw()
    {
        var s = Size;
        if (s.X < 8f || s.Y < 8f) return;

        var c = new Vector2(s.X * 0.5f, s.Y * 0.5f);
        var u = Mathf.Min(s.X, s.Y) * 0.30f;          // "半边长" —— 图形大概占中间 60%
        var w = Mathf.Max(1.6f, u * 0.15f);           // 线宽（跟着缩放走）

        if (Icon == Kind.Room) Room(c, u, w);
        else if (Icon == Kind.Card) Card(c, u, w);
        else if (Icon == Kind.Close) Close(c, u, w);
        else Desk(c, u, w);
    }

    /// <summary>叠着的三张牌：两张斜的在后面当影子，最前面一张正的、牌面带一个花。</summary>
    private void Card(Vector2 c, float u, float w)
    {
        // 后面两张（左、右各傾一点）—— 用折线画牌的外框
        foreach (var lean in new[] { -1f, 1f })
        {
            var dx = lean * 0.34f * u;
            var dy = -0.10f * u;
            var rot = lean * 0.26f;                       // 倾斜角
            CardOutline(c + new Vector2(dx, dy), 0.44f * u, 0.62f * u, rot, w * 0.85f);
        }
        // 最前面一张（正中、更亮）
        CardOutline(c + new Vector2(0f, 0.08f * u), 0.46f * u, 0.66f * u, 0f, w);

        // 正面那张牌心画一朵「花」（四个小花瓣）—— 一眼认出是牌面不是空框
        var fx = c.X;
        var fy = c.Y + 0.08f * u;
        var r = 0.085f * u;
        for (var i = 0; i < 4; i++)
        {
            var t = Mathf.Tau * i / 4f + Mathf.Pi / 4f;
            DrawCircle(new Vector2(fx + r * Mathf.Cos(t), fy + r * Mathf.Sin(t)), 0.075f * u, Col);
        }
        DrawCircle(new Vector2(fx, fy), 0.055f * u, Col);
    }

    /// <summary>画一张（带圆角的）扑克牌外框。</summary>
    private void CardOutline(Vector2 c, float halfW, float halfH, float rot, float w)
    {
        var cs = Mathf.Cos(rot);
        var sn = Mathf.Sin(rot);
        // 四个角（本地坐标 → 旋转 → 平移到 c）
        Vector2 P(float x, float y) => new(c.X + x * cs - y * sn, c.Y + x * sn + y * cs);
        var a = P(-halfW, -halfH);
        var b = P(halfW, -halfH);
        var d = P(halfW, halfH);
        var e = P(-halfW, halfH);
        DrawPolyline(new[] { a, b, d, e, a }, Col, w, true);
        // 内侧再描一圈，做出"牌面有边距"的感觉
        var iw = halfW * 0.72f;
        var ih = halfH * 0.86f;
        DrawPolyline(new[] { P(-iw, -ih), P(iw, -ih), P(iw, ih), P(-iw, ih), P(-iw, -ih) },
            new Color(Col.R, Col.G, Col.B, 0.45f), Mathf.Max(1f, w * 0.7f), true);
    }

    /// <summary>一个叉（×）—— 收桌。</summary>
    private void Close(Vector2 c, float u, float w)
    {
        var r = 0.78f * u;
        DrawLine(new Vector2(c.X - r, c.Y - r), new Vector2(c.X + r, c.Y + r), Col, w, true);
        DrawLine(new Vector2(c.X + r, c.Y - r), new Vector2(c.X - r, c.Y + r), Col, w, true);
    }

    /// <summary>屋子：三角屋顶 + 方墙 + 中间一扇门。</summary>
    private void Room(Vector2 c, float u, float w)
    {
        var roofY = c.Y - 0.06f * u;                   // 屋檐线
        var wallBottom = c.Y + 0.74f * u;

        // 屋顶（闭合三角：顶点 → 左檐 → 右檐 → 回顶点）
        var apex = new Vector2(c.X, c.Y - 0.78f * u);
        var eaveL = new Vector2(c.X - 0.95f * u, roofY);
        var eaveR = new Vector2(c.X + 0.95f * u, roofY);
        DrawPolyline(new[] { apex, eaveL, eaveR, apex }, Col, w, true);

        // 墙（左竖 → 底 → 右竖；顶上那条被屋檐线盖住了，不用重复画）
        var wallTopY = roofY;
        DrawPolyline(new[]
        {
            new Vector2(c.X - 0.62f * u, wallTopY),
            new Vector2(c.X - 0.62f * u, wallBottom),
            new Vector2(c.X + 0.62f * u, wallBottom),
            new Vector2(c.X + 0.62f * u, wallTopY),
        }, Col, w, true);

        // 门（不封底 —— 底边就是墙的底边）
        var dw = 0.20f * u;
        var doorTop = wallBottom - 0.42f * u;
        DrawPolyline(new[]
        {
            new Vector2(c.X - dw, wallBottom),
            new Vector2(c.X - dw, doorTop),
            new Vector2(c.X + dw, doorTop),
            new Vector2(c.X + dw, wallBottom),
        }, Col, Mathf.Max(1.2f, w * 0.8f), true);
    }

    /// <summary>牌桌：从上往下看的椭圆台面 + 桌心摊开的三张牌。</summary>
    private void Desk(Vector2 c, float u, float w)
    {
        // 椭圆 —— Godot 只有 DrawArc（正圆），所以自己按椭圆参数方程取点连成折线。
        const int n = 48;
        var rx = 0.95f * u;
        var ry = 0.50f * u;
        var pts = new Vector2[n + 1];
        for (var i = 0; i <= n; i++)
        {
            var t = Mathf.Tau * i / n;
            pts[i] = new Vector2(c.X + rx * Mathf.Cos(t), c.Y + ry * Mathf.Sin(t));
        }
        DrawPolyline(pts, Col, w, true);

        // 桌心三张牌（三个实心点，跟台面轮廓一比就知道是"桌上摆着牌"）
        for (var i = -1; i <= 1; i++)
            DrawCircle(new Vector2(c.X + i * 0.38f * u, c.Y), 0.115f * u, Col);
    }
}
