// ============================================================================
//  WorldEnvironmentUtil —— 全屏后处理（闪白 / 泛光 / 亮度 / 对比 / 饱和）
//  来源：反编译自万象辉星(RegentFX) v0.5.1 Scripts/Vfx/WorldEnvironmentUtil.cs
//
//  原理就一句：拿 NGame.Instance.ActivateWorldEnvironment()，
//            然后 Tween 它的 environment:xxx 属性。
//
//  ★ 两条铁律：
//    1. 要能让玩家关（闪屏对光敏感的人是伤害）→ Setting.ExposureThreshold
//    2. 用完必须弹回默认 → 用 try/finally，否则画面会被永久污染
// ============================================================================

using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace YourMod.Scripts.Vfx;

public static class WorldEnvironmentUtil
{
    private static WorldEnvironment? _cachedEnv;

    public static WorldEnvironment? GetOrActivate()
    {
        if (_cachedEnv != null && GodotObject.IsInstanceValid(_cachedEnv)) return _cachedEnv;
        if (NGame.Instance == null)
        {
            ModEntry.Log("NGame.Instance 为空，拿不到 WorldEnvironment");
            return null;
        }
        return _cachedEnv = NGame.Instance.ActivateWorldEnvironment();
    }

    public static void Deactivate()
    {
        NGame.Instance?.DeactivateWorldEnvironment();
        _cachedEnv = null;
    }

    // ---------------- 曝光（闪白/大招过曝）----------------

    /// <summary>一次性闪一下：waitTime 后拉高，inTime 内到达，然后 outTime 内弹回</summary>
    public static async Task FullExposure(float exposure, float waitTime, float inTime, float outTime)
    {
        await VfxUtil.Wait(waitTime);
        TweenExposure(exposure, inTime);
        await VfxUtil.Wait(inTime);
        TweenExposure(1f, outTime);
    }

    public static Tween? TweenExposure(float exposure, float duration,
        EaseType ease = EaseType.InOut, TransitionType trans = TransitionType.Cubic)
    {
        // 玩家把阈值调到 0 = 关掉闪屏
        float threshold = Setting.ExposureThreshold;
        if (threshold <= 0.001f) return null;

        // 按玩家阈值打折：(target-1)*t + target
        float target = (exposure - 1f) * threshold + exposure;
        return TweenProperty("environment:tonemap_exposure", target, duration, ease, trans);
    }

    public static void SetExposure(float v)
    {
        var env = GetOrActivate();
        if (env != null) env.Environment.TonemapExposure = v;
    }

    // ---------------- 另外四个 ----------------

    public static Tween? TweenGlowIntensity(float v, float dur,
        EaseType ease = EaseType.InOut, TransitionType trans = TransitionType.Cubic)
        => TweenProperty("environment:glow_intensity", v, dur, ease, trans);

    public static Tween? TweenBrightness(float v, float dur,
        EaseType ease = EaseType.InOut, TransitionType trans = TransitionType.Cubic)
        => TweenProperty("environment:adjustment_brightness", v, dur, ease, trans);

    public static Tween? TweenContrast(float v, float dur,
        EaseType ease = EaseType.InOut, TransitionType trans = TransitionType.Cubic)
        => TweenProperty("environment:adjustment_contrast", v, dur, ease, trans);

    public static Tween? TweenSaturation(float v, float dur,
        EaseType ease = EaseType.InOut, TransitionType trans = TransitionType.Cubic)
        => TweenProperty("environment:adjustment_saturation", v, dur, ease, trans);

    // ---------------- 复位 ----------------

    public static void ResetToDefaults()
    {
        var env = GetOrActivate();
        if (env == null) return;
        env.Environment.TonemapExposure = 1f;
        env.Environment.AdjustmentBrightness = 1f;
        env.Environment.AdjustmentContrast = 1f;
        env.Environment.AdjustmentSaturation = 1f;
        env.Environment.GlowIntensity = 0.8f;
    }

    public static Tween? TweenResetToDefaults(float duration)
    {
        var env = GetOrActivate();
        if (env == null) return null;

        var t = env.CreateTween().SetParallel(true);
        t.TweenProperty(env, "environment:tonemap_exposure", 1f, duration);
        t.TweenProperty(env, "environment:adjustment_brightness", 1f, duration);
        t.TweenProperty(env, "environment:adjustment_contrast", 1f, duration);
        t.TweenProperty(env, "environment:adjustment_saturation", 1f, duration);
        t.TweenProperty(env, "environment:glow_intensity", 0.8f, duration);
        return t;
    }

    // ---------------- 内部 ----------------

    private static Tween? TweenProperty(string property, float target, float duration,
        EaseType ease, TransitionType trans)
    {
        var env = GetOrActivate();
        if (env == null) return null;

        var tween = env.CreateTween();
        return tween?.TweenProperty(env, property, target, duration)
                    .SetEase(ease).SetTrans(trans);
    }
}
