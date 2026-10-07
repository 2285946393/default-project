// ============================================================================
//  ModEntry —— 模组入口
//  来源：反编译自万象辉星(RegentFX) v0.5.1 Scripts/Entry.cs
//
//  三件事：
//    1. 装 Harmony 补丁（特效时机）
//    2. ScriptManagerBridge 注册 C# 脚本（不调的话 .tscn 里的脚本引用会变空壳）
//    3. 把特效场景预加载进自己的私有缓存（避免第一次卡顿 / 换房间被清）
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using YourMod.Scripts.Vfx;

namespace YourMod.Scripts;

[ModInitializer("Init")]                 // ★ 框架按这个名字找静态 Init 方法
public class ModEntry
{
    public const string ModId = "YourMod";   // ← 改成你的 mod id（跟 json 里一致）
    public const string Version = "0.1.0";

    // ⚠️ 反编译里是 `new Logger("RegentFX", (LogType)0)` —— 成员名不直观，
    //    所以这里用强转，编译不过时先看 LogType 的实际枚举名。
    public static Logger Logger { get; } = new Logger(ModId, (LogType)0);

    /// <summary>统一日志出口。用 Console 兜底，避免日志本身把 mod 搞崩。</summary>
    public static void Log(string msg)
    {
        try { Logger.Info(msg, 1); } catch { }
    }

    public static void Init()
    {
        try
        {
            // 1) 装补丁（时机相关，见 Patches/）
            new Harmony("sts2.yourname.yourmod").PatchAll();

            // 2) ★ 必须：把程序集里的 C# 脚本注册给 Godot
            //    不调的话，场景里 path="res://Scripts/Xxx.cs" 解析不到你的类，
            //    场景加载出来是个不动的空壳。
            ScriptManagerBridge.LookupScriptsInAssembly(typeof(ModEntry).Assembly);

            // 3) 预加载特效场景
            if (Setting.PreloadEffects) LoadScenes();
            else Log("[Vfx] 跳过场景预加载，可能会卡顿");

            Log($"{ModId} {Version} 加载完成");
        }
        catch (Exception ex)
        {
            Logger.Error("初始化失败：" + ex, 1);
        }
    }

    // =================================================================
    //  预加载：反射扫全程序集，自动收集所有 CardFX / PowerFX 的资源
    //  → 加一张新卡的特效，不用回来改这个列表
    // =================================================================
    private static void LoadScenes()
    {
        var paths = CollectAssetPaths();
        if (paths.Count == 0) return;

        int ok = 0, fail = 0;
        foreach (var path in paths)
        {
            try
            {
                if (VfxUtil.SceneCache.ContainsKey(path)) continue;

                var scene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse);
                if (scene != null) { VfxUtil.SceneCache[path] = scene; ok++; }
                else { fail++; Logger.Warn($"预加载失败：{path}", 1); }
            }
            catch (Exception ex)
            {
                fail++;
                Logger.Warn($"预加载出错 {path}：{ex.Message}", 1);
            }
        }
        Log($"[Vfx] 预加载完成：成功 {ok}，失败 {fail}");
    }

    private static List<string> CollectAssetPaths()
    {
        // 手工补的场景（不走 CardFX 类的那些）写在这
        var set = new HashSet<string>
        {
            // "res://YourMod/scenes/common/burst.tscn",
        };

        var asm = typeof(ModEntry).Assembly;

        foreach (var t in asm.GetTypes().Where(
            t => t.IsSubclassOf(typeof(CardFX)) && !t.IsAbstract && !t.ContainsGenericParameters))
        {
            try
            {
                if (Activator.CreateInstance(t) is not CardFX fx) continue;
                foreach (var p in fx.AssetPaths)
                    if (!string.IsNullOrEmpty(p)) set.Add(p);
            }
            catch (Exception ex)
            {
                Logger.Debug($"跳过 {t.Name} 的预加载：{ex.Message}", 1);
            }
        }

        // 有 PowerFX 同理再加一段

        return set.ToList();
    }
}
