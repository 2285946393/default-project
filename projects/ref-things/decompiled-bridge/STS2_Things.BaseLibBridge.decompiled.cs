using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using BaseLib;
using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]
[assembly: AssemblyCompany("STS2_Things.BaseLibBridge")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.10.0.0")]
[assembly: AssemblyInformationalVersion("1.10.0+4938c43d3b0d2387923a3da5897c8b110ec7cdfb")]
[assembly: AssemblyProduct("STS2_Things.BaseLibBridge")]
[assembly: AssemblyTitle("STS2_Things.BaseLibBridge")]
[assembly: AssemblyVersion("1.10.0.0")]
[module: RefSafetyRules(11)]
namespace GodotPlugins.Game
{
	internal static class Main
	{
		[UnmanagedCallersOnly(EntryPoint = "godotsharp_game_main_init")]
		private static godot_bool InitializeFromGameProject(nint godotDllHandle, nint outManagedCallbacks, nint unmanagedCallbacks, int unmanagedCallbacksSize)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected O, but got Unknown
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				DllImportResolver resolver = new GodotDllImportResolver((IntPtr)godotDllHandle).OnResolveDllImport;
				NativeLibrary.SetDllImportResolver(typeof(GodotObject).Assembly, resolver);
				NativeFuncs.Initialize((IntPtr)unmanagedCallbacks, unmanagedCallbacksSize);
				ManagedCallbacks.Create((IntPtr)outManagedCallbacks);
				ScriptManagerBridge.LookupScriptsInAssembly(typeof(Main).Assembly);
				return (godot_bool)1;
			}
			catch (Exception value)
			{
				Console.Error.WriteLine(value);
				return GodotBoolExtensions.ToGodotBool(false);
			}
		}
	}
}
namespace STS2_Things.BaseLibBridge
{
	public static class BridgeEntry
	{
		private const string ModId = "STS2_Things";

		private static bool _synchronizing;

		private static Type? _mainConfig;

		private static ThingsBaseLibConfig? _config;

		private static readonly PropertyInfo[] Settings = (from property in typeof(ThingsBaseLibConfig).GetProperties(BindingFlags.Static | BindingFlags.Public)
			where property.CanRead && property.CanWrite && (property.PropertyType == typeof(bool) || property.PropertyType == typeof(int))
			select property).ToArray();

		public static void Register()
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Expected O, but got Unknown
			_mainConfig = FindMainConfig();
			new Harmony("Adnermo.STS2_Things.BaseLibConfigSave").Patch((MethodBase)AccessTools.Method(typeof(ModConfig), "SaveInternal", (Type[])null, (Type[])null), new HarmonyMethod(typeof(BridgeEntry), "SaveLocalPreferences", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			ThingsBaseLibConfig thingsBaseLibConfig = (_config = new ThingsBaseLibConfig());
			((ModConfig)thingsBaseLibConfig).ConfigChanged += delegate
			{
				SynchronizeToMain();
			};
			((ModConfig)thingsBaseLibConfig).OnConfigReloaded += SynchronizeToMain;
			ModConfigRegistry.Register("STS2_Things", (ModConfig)(object)thingsBaseLibConfig);
			_mainConfig?.GetEvent("Changed", BindingFlags.Static | BindingFlags.Public)?.AddEventHandler(null, new Action(SynchronizeFromMain));
			SynchronizeToMain();
		}

		public static bool CanEdit(string key)
		{
			return ((bool?)_mainConfig?.GetMethod("CanEdit")?.Invoke(null, new object[1] { key })).GetValueOrDefault(true);
		}

		private static bool SaveLocalPreferences(ModConfig __instance)
		{
			if (__instance is ThingsBaseLibConfig)
			{
				object obj = _mainConfig?.GetProperty("IsUsingSession")?.GetValue(null);
				if (obj is bool && (bool)obj)
				{
					_mainConfig.GetMethod("Save").Invoke(null, null);
					return false;
				}
			}
			return true;
		}

		private static Type? FindMainConfig()
		{
			return (from assembly in AppDomain.CurrentDomain.GetAssemblies()
				where assembly.GetName().Name == "STS2_Things"
				select assembly.GetType("STS2_Things.Config.ThingsModConfig", throwOnError: false)).FirstOrDefault((Type type) => (object)type != null);
		}

		private static void SynchronizeToMain()
		{
			if (_synchronizing)
			{
				return;
			}
			try
			{
				if ((object)_mainConfig == null)
				{
					_mainConfig = FindMainConfig();
				}
				if ((object)_mainConfig != null)
				{
					_synchronizing = true;
					Dictionary<string, object> dictionary = Settings.ToDictionary((PropertyInfo property) => property.Name, (PropertyInfo property) => property.GetValue(null));
					_mainConfig.GetMethod("SetValues", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[1] { dictionary });
					CopyNormalizedValues();
					_config?.RefreshSessionAccess();
					_mainConfig.GetMethod("Save", BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
				}
			}
			catch (Exception ex)
			{
				BaseLibMain.Logger.Warn("STS2_Things.BaseLibBridge could not synchronize settings: " + ex.Message, 1);
			}
			finally
			{
				_synchronizing = false;
			}
		}

		private static void SynchronizeFromMain()
		{
			if (_synchronizing)
			{
				return;
			}
			try
			{
				_synchronizing = true;
				CopyNormalizedValues();
				ThingsBaseLibConfig? config = _config;
				if (config != null)
				{
					((ModConfig)config).ConfigReloaded();
				}
				_config?.RefreshSessionAccess();
			}
			finally
			{
				_synchronizing = false;
			}
		}

		private static void CopyNormalizedValues()
		{
			MethodInfo methodInfo = _mainConfig?.GetMethod("GetValue", BindingFlags.Static | BindingFlags.Public);
			if ((object)methodInfo == null)
			{
				return;
			}
			PropertyInfo[] settings = Settings;
			foreach (PropertyInfo propertyInfo in settings)
			{
				object obj = methodInfo.Invoke(null, new object[1] { propertyInfo.Name });
				if (obj != null)
				{
					propertyInfo.SetValue(null, obj);
				}
			}
		}
	}
	public sealed class ThingsBaseLibConfig : SimpleModConfig
	{
		private readonly List<(Control Control, string Key, MouseFilterEnum Mouse, FocusModeEnum Focus)> _access = new List<(Control, string, MouseFilterEnum, FocusModeEnum)>();

		[ConfigSection("Audio")]
		[ConfigHoverTip(true)]
		public static bool FeatureCustomBgmEnabled { get; set; } = true;


		[ConfigSection("Bosses")]
		[ConfigHoverTip(true)]
		public static bool BossOnlyModBosses { get; set; }

		[ConfigSection("Bosses")]
		public static bool BossOriginFogmogEnabled { get; set; } = true;


		[ConfigSection("Bosses")]
		[ConfigVisibleIf("CanForceOriginFogmog", new object[] { })]
		public static bool BossOriginFogmogForced { get; set; }

		[ConfigSection("Bosses")]
		[ConfigHoverTip(true)]
		[ConfigSlider(0.0, 1000.0, 10.0, Format = "{0}%")]
		public static int BossOriginFogmogWeightPercent { get; set; } = 100;


		[ConfigSection("Bosses")]
		public static bool BossScaleBeetleEnabled { get; set; } = true;


		[ConfigSection("Bosses")]
		[ConfigVisibleIf("CanForceScaleBeetle", new object[] { })]
		public static bool BossScaleBeetleForced { get; set; }

		[ConfigSection("Bosses")]
		[ConfigHoverTip(true)]
		[ConfigSlider(0.0, 1000.0, 10.0, Format = "{0}%")]
		public static int BossScaleBeetleWeightPercent { get; set; } = 100;


		[ConfigSection("Bosses")]
		public static bool BossGravetideSlugEnabled { get; set; } = true;


		[ConfigSection("Bosses")]
		[ConfigVisibleIf("CanForceGravetideSlug", new object[] { })]
		public static bool BossGravetideSlugForced { get; set; }

		[ConfigSection("Bosses")]
		[ConfigHoverTip(true)]
		[ConfigSlider(0.0, 1000.0, 10.0, Format = "{0}%")]
		public static int BossGravetideSlugWeightPercent { get; set; } = 100;


		[ConfigSection("Bosses")]
		public static bool BossTheLegacyEnabled { get; set; } = true;


		[ConfigSection("Bosses")]
		[ConfigVisibleIf("CanForceTheLegacy", new object[] { })]
		public static bool BossTheLegacyForced { get; set; }

		[ConfigSection("Bosses")]
		[ConfigHoverTip(true)]
		[ConfigSlider(0.0, 1000.0, 10.0, Format = "{0}%")]
		public static int BossTheLegacyWeightPercent { get; set; } = 100;


		[ConfigSection("Bosses")]
		public static bool BossBowlbugProgenitorEnabled { get; set; } = true;


		[ConfigSection("Bosses")]
		[ConfigVisibleIf("CanForceBowlbugProgenitor", new object[] { })]
		public static bool BossBowlbugProgenitorForced { get; set; }

		[ConfigSection("Bosses")]
		[ConfigHoverTip(true)]
		[ConfigSlider(0.0, 1000.0, 10.0, Format = "{0}%")]
		public static int BossBowlbugProgenitorWeightPercent { get; set; } = 100;


		[ConfigSection("Bosses")]
		public static bool BossCaveGodEnabled { get; set; } = true;


		[ConfigSection("Bosses")]
		[ConfigVisibleIf("CanForceCaveGod", new object[] { })]
		public static bool BossCaveGodForced { get; set; }

		[ConfigSection("Bosses")]
		[ConfigHoverTip(true)]
		[ConfigSlider(0.0, 1000.0, 10.0, Format = "{0}%")]
		public static int BossCaveGodWeightPercent { get; set; } = 100;


		[ConfigSection("Other Encounters")]
		public static bool EncounterSoulRoesEnabled { get; set; } = true;


		[ConfigSection("Other Encounters")]
		[ConfigHoverTip(true)]
		[ConfigSlider(0.0, 1000.0, 10.0, Format = "{0}%")]
		public static int EncounterSoulRoesWeightPercent { get; set; } = 100;


		[ConfigSection("Other Encounters")]
		public static bool EncounterQuirkyHopperEnabled { get; set; } = true;


		[ConfigSection("Events")]
		public static bool EventRobberyFakeMerchantEnabled { get; set; } = true;


		[ConfigSection("Events")]
		public static bool EventBackroomsEnabled { get; set; } = true;


		[ConfigSection("Events")]
		public static bool EventMedusaEnabled { get; set; } = true;


		[ConfigSection("Events")]
		public static bool EventCuttingItCloseEnabled { get; set; } = true;


		[ConfigSection("Events")]
		public static bool EventRealityAlignedHousesEnabled { get; set; } = true;


		[ConfigSection("Events")]
		public static bool EventShadowCloakroomEnabled { get; set; } = true;


		[ConfigSection("Events")]
		public static bool EventEchoingWellEnabled { get; set; } = true;


		[ConfigSection("Events")]
		public static bool EventPoliteMawEnabled { get; set; } = true;


		[ConfigSection("Events")]
		public static bool EventMycelialBankEnabled { get; set; } = true;


		[ConfigSection("Events")]
		public static bool EventUnlitFireEnabled { get; set; } = true;


		[ConfigSection("Events")]
		public static bool EventRelicWorkshopEnabled { get; set; } = true;


		[ConfigSection("Events")]
		public static bool EventPotionTastingEnabled { get; set; } = true;


		[ConfigSection("Events")]
		public static bool EventNarrowGateEnabled { get; set; } = true;


		[ConfigSection("Merchant Bargain")]
		public static bool FeatureMerchantBargainEnabled { get; set; } = true;


		[ConfigSection("Neow Starting Relics")]
		public static bool NeowRelicCurseRemoverEnabled { get; set; } = true;


		[ConfigSection("Neow Starting Relics")]
		public static bool NeowRelicWhiteFlagEnabled { get; set; } = true;


		[ConfigSection("Neow Starting Relics")]
		public static bool NeowRelicMagicGloveEnabled { get; set; } = true;


		public override void SetupConfigUI(Control optionContainer)
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Expected O, but got Unknown
			_access.Clear();
			((Node)optionContainer).AddChild((Node)new Label
			{
				Text = new LocString("settings_ui", "STS2_THINGS-MULTIPLAYER_POLICY").GetFormattedText(),
				AutowrapMode = (AutowrapMode)3
			}, false, (InternalMode)0);
			((SimpleModConfig)this).SetupConfigUI(optionContainer);
			Visit((Node)(object)optionContainer);
			RefreshSessionAccess();
			void Remember(Node node, string key)
			{
				//IL_0013: Unknown result type (might be due to invalid IL or missing references)
				//IL_0019: Unknown result type (might be due to invalid IL or missing references)
				Control val = (Control)(object)((node is Control) ? node : null);
				if (val != null)
				{
					_access.Add((val, key, val.MouseFilter, val.FocusMode));
				}
				foreach (Node child in node.GetChildren(false))
				{
					Remember(child, key);
				}
			}
			void Visit(Node node)
			{
				NConfigOptionRow val2 = (NConfigOptionRow)(object)((node is NConfigOptionRow) ? node : null);
				if (val2 != null)
				{
					Remember((Node)(object)val2.SettingControl, ((object)((Node)val2).Name).ToString());
				}
				else
				{
					if (!(node.Name == StringName.op_Implicit("ResetDefaultsButton")))
					{
						foreach (Node child2 in node.GetChildren(false))
						{
							Visit(child2);
						}
						return;
					}
					Remember(node, "BossOnlyModBosses");
				}
			}
		}

		public void RefreshSessionAccess()
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			foreach (var (val, key, val2, val3) in _access)
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)val))
				{
					bool flag = BridgeEntry.CanEdit(key);
					val.MouseFilter = (MouseFilterEnum)((!flag) ? 2 : ((long)val2));
					val.FocusMode = (FocusModeEnum)((!flag) ? 0 : ((long)val3));
					((Node)val).ProcessMode = (ProcessModeEnum)(flag ? 0 : 4);
					((CanvasItem)val).SelfModulate = (Color)(flag ? Colors.White : new Color(1f, 1f, 1f, 0.45f));
				}
			}
		}

		public static bool CanForceOriginFogmog()
		{
			return !BossScaleBeetleForced;
		}

		public static bool CanForceScaleBeetle()
		{
			return !BossOriginFogmogForced;
		}

		public static bool CanForceGravetideSlug()
		{
			return !BossTheLegacyForced;
		}

		public static bool CanForceTheLegacy()
		{
			return !BossGravetideSlugForced;
		}

		public static bool CanForceBowlbugProgenitor()
		{
			return !BossCaveGodForced;
		}

		public static bool CanForceCaveGod()
		{
			return !BossBowlbugProgenitorForced;
		}
	}
}
