using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using RegentFX.Scripts.Vfx;
using RegentFX.Scripts.Vfx.Cards;
using RegentFX.Scripts.Vfx.Powers;
using RegentFX.ThirdParty;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts;

[ModInitializer("Init")]
public class Entry
{
	public const string ModId = "RegentFX";

	public static readonly ConcurrentDictionary<string, PackedScene> ModSceneCache = new ConcurrentDictionary<string, PackedScene>();

	public const string VERSION = "0.5.1";

	public static bool FmodLoaded = false;

	public static Logger Logger { get; } = new Logger("RegentFX", (LogType)0);


	public static StarRingController? StarRingController { get; set; }

	public static StarEffectController? StarEffectController { get; set; }

	public static SupermassiveController? SupermassiveController { get; set; }

	public static void Init()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			FmodLoaded = FmodLite.TryLoadBankAndGuidMappings("res://RegentFX/banks/RegentFx.bank", "res://RegentFX/banks/GUIDs.txt");
			if (!FmodLoaded)
			{
				Logger.Warn("Fmod加载失败，可能是移动端，回退到默认音效", 1);
			}
			new Harmony("sts2.vitech.regentFx").PatchAll();
			ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
			RitsuLibModConfig.SetDefaults();
			if (Setting.PreloadEffects)
			{
				LoadScenes();
			}
			else
			{
				Log.Warn("[RegentFX]Skipping effect scene preloading, may cause lagging.", 2);
			}
			Log.Info("RegentFX Omnistar 0.5.1 Load Complete![万象辉星]加载成功!", 2);
		}
		catch (Exception ex)
		{
			Logger.Error("RegentFX initialized failed! 错误详情: " + ex.Message, 1);
		}
	}

	private static void LoadScenes()
	{
		try
		{
			List<string> list = CollectAssetPathsSafely();
			if (list.Count <= 0)
			{
				return;
			}
			Logger.Info($"Preloading {list.Count} RegentFX assets synchronously", 1);
			int num = 0;
			int num2 = 0;
			foreach (string item in list)
			{
				try
				{
					if (!ModSceneCache.ContainsKey(item))
					{
						PackedScene val = ResourceLoader.Load<PackedScene>(item, (string)null, (CacheMode)1);
						if (val != null)
						{
							ModSceneCache[item] = val;
							num++;
						}
						else
						{
							num2++;
							Logger.Warn("Failed to preload: " + item, 1);
						}
					}
				}
				catch (Exception ex)
				{
					num2++;
					Logger.Warn("Error preloading " + item + ": " + ex.Message, 1);
				}
			}
			Logger.Info($"Preloading complete: {num} succeeded, {num2} failed", 1);
		}
		catch (Exception ex2)
		{
			Logger.Warn("Failed to preload RegentFX assets: " + ex2.Message, 1);
		}
	}

	private static List<string> CollectAssetPathsSafely()
	{
		HashSet<string> hashSet = new HashSet<string>
		{
			"res://RegentFX/scenes/vfx/distortions/vfx_outward_screen_distortion_ellipse.tscn",
			Blade.Blade1Path,
			Blade.Blade2Path,
			Blackhole.VfxScenePath,
			"res://RegentFX/scenes/vfx/pillar.tscn",
			"res://RegentFX/scenes/vfx/p_burst.tscn",
			Star.VfxScenePath,
			"res://RegentFX/scenes/Stardust.tscn"
		};
		Assembly assembly = typeof(Entry).Assembly;
		foreach (Type item in from t in assembly.GetTypes()
			where t.IsSubclassOf(typeof(CardFX)) && !t.IsAbstract && !t.ContainsGenericParameters
			select t)
		{
			try
			{
				if (!(Activator.CreateInstance(item) is CardFX cardFX))
				{
					continue;
				}
				foreach (string assetPath in cardFX.AssetPaths)
				{
					if (!string.IsNullOrEmpty(assetPath))
					{
						hashSet.Add(assetPath);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Debug("Skip preloading for " + item.Name + ": " + ex.Message, 1);
			}
		}
		foreach (Type item2 in from t in assembly.GetTypes()
			where t.IsSubclassOf(typeof(PowerFX)) && !t.IsAbstract && !t.ContainsGenericParameters
			select t)
		{
			try
			{
				if (!(Activator.CreateInstance(item2) is PowerFX powerFX))
				{
					continue;
				}
				foreach (string assetPath2 in powerFX.AssetPaths)
				{
					if (!string.IsNullOrEmpty(assetPath2))
					{
						hashSet.Add(assetPath2);
					}
				}
			}
			catch (Exception ex2)
			{
				Logger.Debug("Skip preloading for " + item2.Name + ": " + ex2.Message, 1);
			}
		}
		return hashSet.ToList();
	}
}
