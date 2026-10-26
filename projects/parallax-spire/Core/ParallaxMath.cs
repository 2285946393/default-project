using Godot;

namespace ParallaxSpire;

// 视差的纯数学部分，离线可测，不依赖游戏节点。
public static class ParallaxMath
{
    public const float ForegroundFactor = -18f;

    public static float LayerFactor(int index, int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
        float depth = (index + 1f) / count;
        return 3f + depth * 11f;
    }

    public static Vector2 Normalize(Vector2 mouse, Vector2 size)
    {
        if (size.X <= 1f || size.Y <= 1f) throw new ArgumentOutOfRangeException(nameof(size));
        return (mouse / size - Vector2.One * 0.5f) * 2f;
    }

    public static Vector2 Smooth(Vector2 current, Vector2 target, float delta)
    {
        if (delta <= 0f) return current;
        return current.Lerp(target, Mathf.Min(1f, delta * 8f));
    }
}

