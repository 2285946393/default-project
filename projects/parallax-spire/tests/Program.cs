using Godot;

namespace ParallaxSpire.Tests;

// 不加载游戏的纯逻辑检查：验证视差系数计算与插值行为。
internal static class Program
{
    private static void Main()
    {
        int checks = 0;
        void Check(bool condition, string message)
        {
            checks++;
            if (!condition) throw new Exception(message);
        }

        // 远景到近景：系数必须递增，且都为正。
        float previous = 0f;
        for (int i = 0; i < 6; i++)
        {
            float factor = ParallaxMath.LayerFactor(i, 6);
            Check(factor > previous, "layer factors must increase");
            Check(factor >= 3f && factor <= 14f, "layer factor must stay in safe range");
            previous = factor;
        }
        // 前景必须为负：离玩家越近，视差方向相反。
        Check(ParallaxMath.ForegroundFactor < 0f, "foreground factor must be negative");
        Check(ParallaxMath.ForegroundFactor >= -24f, "foreground must not move too far");

        // 归一化鼠标：中点是 0，边缘接近 ±1。
        Check(ParallaxMath.Normalize(new Vector2(960, 540), new Vector2(1920, 1080))
            .DistanceTo(Vector2.Zero) < 0.001f, "center must normalize to zero");
        var edge = ParallaxMath.Normalize(new Vector2(1920, 0), new Vector2(1920, 1080));
        Check(Mathf.Abs(edge.X - 1f) < 0.001f && Mathf.Abs(edge.Y + 1f) < 0.001f,
            "edge must normalize to near one");

        // 插值必须在多帧后收敛到目标，不能越界。
        var current = new Vector2(0, 0);
        for (int i = 0; i < 120; i++)
            current = ParallaxMath.Smooth(current, new Vector2(100, 100), 1f / 60f);
        Check(current.DistanceTo(new Vector2(100, 100)) < 1f, "mouse smoothing must converge");

        Console.WriteLine($"通过 {checks} 项视差逻辑检查。");
    }
}

