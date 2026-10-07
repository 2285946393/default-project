// ============================================================================
//  Setting —— 特效开关
//
//  万象辉星用了第三方配置库 RitsuLibModConfig（内置在它 dll 里），
//  好处是每个 CardFX 自动在设置界面生成一个开关，玩家可以逐张卡关掉。
//
//  这个模板给一个零依赖的最小版：内存里的字典 + 可选持久化。
//  有配置库（RitsuLib / ModConfig 之类）就换成库的读写。
// ============================================================================

using System.Collections.Generic;
using Godot;

namespace YourMod.Scripts;

public static class Setting
{
    // ⚠️ bool 和 float 分开存 —— 之前用同一个 bool 字典冒充 float 是错的
    private static readonly Dictionary<string, bool> Bools = new();
    private static readonly Dictionary<string, float> Floats = new();

    /// <summary>开关默认全开</summary>
    private const bool DefaultValue = true;

    /// <summary>启动时预加载特效场景？关了省内存，但第一次放特效会卡一下</summary>
    public static bool PreloadEffects => GetBool("PreloadEffects", true);

    /// <summary>闪屏强度阈值 0~1。★ 必须给玩家关掉闪屏的权利（光敏感）</summary>
    public static float ExposureThreshold => GetFloat("ExposureThreshold", 1f);

    public static bool ToggleEnabled(string key) => GetBool(key, DefaultValue);

    // ------------------------------------------------------------------
    public static bool GetBool(string key, bool def)
        => Bools.TryGetValue(key, out var v) ? v : def;

    public static void SetBool(string key, bool value) => Bools[key] = value;

    /// <summary>取值顺序：代码里设过 → 配置文件里存过 → 默认值</summary>
    public static float GetFloat(string key, float def)
        => Floats.TryGetValue(key, out var v) ? v : def;

    public static void SetFloat(string key, float value) => Floats[key] = value;

    // ---- 持久化：用 Godot 自带的 ConfigFile ----
    private const string DefaultPath = "user://yourmod_vfx.cfg";

    public static void Save(string path = DefaultPath)
    {
        var cfg = new ConfigFile();
        foreach (var (k, v) in Bools)  cfg.SetValue("toggles", k, v);
        foreach (var (k, v) in Floats) cfg.SetValue("floats",  k, v);
        cfg.Save(path);
    }

    public static void Load(string path = DefaultPath)
    {
        var cfg = new ConfigFile();
        if (cfg.Load(path) != Error.Ok) return;

        foreach (var k in cfg.GetSectionKeys("toggles"))
            Bools[k] = (bool)cfg.GetValue("toggles", k);

        foreach (var k in cfg.GetSectionKeys("floats"))
            Floats[k] = (float)cfg.GetValue("floats", k);
    }
}
