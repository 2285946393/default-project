using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using HarmonyLib;
using LoadOrderManager;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]
[assembly: AssemblyCompany("LoadOrderManager.Impl")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("0.4.0")]
[assembly: AssemblyInformationalVersion("0.4.0+72f0c4e9ab2d773cdde71b1aaa5695554578c18a")]
[assembly: AssemblyProduct("LoadOrderManager.Impl")]
[assembly: AssemblyTitle("LoadOrderManager.Impl")]
[assembly: AssemblyHasScripts(new Type[] { typeof(LoadOrderPanel) })]
[assembly: AssemblyVersion("0.4.0.0")]
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
namespace LoadOrderManager
{
	internal static class DebugLog
	{
		private static readonly object Sync = new object();

		private static string? _logPath;

		public static string LogPath
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(_logPath))
				{
					return _logPath;
				}
				try
				{
					string text = Path.Combine(OS.GetUserDataDir(), "LoadOrderManager");
					Directory.CreateDirectory(text);
					_logPath = Path.Combine(text, "load_order_manager.log");
				}
				catch
				{
					_logPath = "load_order_manager.log";
				}
				return _logPath;
			}
		}

		public static void Info(string message)
		{
			Write("INFO", message, null);
		}

		public static void Warn(string message)
		{
			Write("WARN", message, null);
		}

		public static void Error(string message, Exception? ex = null)
		{
			Write("ERROR", message, ex);
		}

		private static void Write(string level, string message, Exception? ex)
		{
			string value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
			string text = $"[LoadOrderManager] [{value}] [{level}] {message}";
			string text2 = ((ex == null) ? text : $"{text}{Environment.NewLine}{ex}");
			try
			{
				if (level == "ERROR")
				{
					GD.PrintErr(text);
				}
				else
				{
					GD.Print(text);
				}
			}
			catch
			{
			}
			try
			{
				lock (Sync)
				{
					File.AppendAllText(LogPath, text2 + Environment.NewLine);
				}
			}
			catch
			{
			}
		}
	}
	internal static class I18n
	{
		private static readonly Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

		private static readonly object LockObj = new object();

		private static bool _loaded;

		public static string T(string key)
		{
			EnsureLoaded();
			string key2 = ResolveLanguageTag(GetClientLanguageCode() ?? "en");
			if (Tables.TryGetValue(key2, out Dictionary<string, string> value) && value.TryGetValue(key, out var value2))
			{
				return value2;
			}
			if (Tables.TryGetValue("en", out Dictionary<string, string> value3) && value3.TryGetValue(key, out var value4))
			{
				return value4;
			}
			return key;
		}

		public static string Tf(string key, params object[] args)
		{
			return string.Format(CultureInfo.InvariantCulture, T(key), args);
		}

		private static void EnsureLoaded()
		{
			if (_loaded)
			{
				return;
			}
			lock (LockObj)
			{
				if (_loaded)
				{
					return;
				}
				string i18nDirectory = GetI18nDirectory();
				if (!Directory.Exists(i18nDirectory))
				{
					DebugLog.Error("i18n directory not found: " + i18nDirectory);
					_loaded = true;
					return;
				}
				string[] files = Directory.GetFiles(i18nDirectory, "*.lang", SearchOption.TopDirectoryOnly);
				foreach (string path in files)
				{
					string text = NormalizeTag(Path.GetFileNameWithoutExtension(path));
					if (!string.IsNullOrWhiteSpace(text))
					{
						Dictionary<string, string> dictionary = ParseLangFile(path);
						if (dictionary.Count != 0)
						{
							Tables[text] = dictionary;
						}
					}
				}
				if (!Tables.ContainsKey("en"))
				{
					DebugLog.Warn("Missing en.lang; fallback texts may be unavailable.");
				}
				DebugLog.Info("i18n loaded from: " + i18nDirectory + ", languages: " + string.Join(", ", Tables.Keys));
				_loaded = true;
			}
		}

		private static string GetI18nDirectory()
		{
			List<string> list = new List<string>();
			try
			{
				string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				if (!string.IsNullOrWhiteSpace(directoryName))
				{
					list.Add(Path.Combine(directoryName, "i18n"));
					string text = directoryName;
					for (int i = 0; i < 2; i++)
					{
						string directoryName2 = Path.GetDirectoryName(text);
						if (!string.IsNullOrWhiteSpace(directoryName2))
						{
							text = directoryName2;
							list.Add(Path.Combine(text, "i18n"));
							continue;
						}
						break;
					}
				}
			}
			catch
			{
			}
			try
			{
				string directoryName3 = Path.GetDirectoryName(OS.GetExecutablePath());
				if (!string.IsNullOrWhiteSpace(directoryName3))
				{
					list.Add(Path.Combine(directoryName3, "mods", "LoadOrderManager", "i18n"));
				}
			}
			catch
			{
			}
			try
			{
				string userDataDir = OS.GetUserDataDir();
				if (!string.IsNullOrWhiteSpace(userDataDir))
				{
					list.Add(Path.Combine(userDataDir, "mods", "LoadOrderManager", "i18n"));
				}
			}
			catch
			{
			}
			foreach (string item in list)
			{
				if (Directory.Exists(item))
				{
					return item;
				}
			}
			if (list.Count <= 0)
			{
				return "i18n";
			}
			return list[0];
		}

		private static Dictionary<string, string> ParseLangFile(string path)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (string item in File.ReadLines(path, Encoding.UTF8))
			{
				string text = item.Trim();
				if (text.Length == 0 || text.StartsWith("#", StringComparison.Ordinal) || text.StartsWith(";", StringComparison.Ordinal))
				{
					continue;
				}
				int num = text.IndexOf('=');
				if (num > 0)
				{
					string text2 = text.Substring(0, num).Trim();
					string text3 = text;
					int num2 = num + 1;
					string text4 = text3.Substring(num2, text3.Length - num2).Trim();
					if (text2.Length != 0)
					{
						dictionary[text2] = text4.Replace("\\n", "\n", StringComparison.Ordinal);
					}
				}
			}
			return dictionary;
		}

		private static string ResolveLanguageTag(string code)
		{
			string text = NormalizeTag(code);
			if (!text.StartsWith("zhs", StringComparison.Ordinal) && !text.Contains("hans", StringComparison.Ordinal))
			{
				switch (text)
				{
				case "zh-cn":
				case "zh-sg":
				case "zh":
					break;
				default:
					if (!text.StartsWith("zht", StringComparison.Ordinal) && !text.Contains("hant", StringComparison.Ordinal))
					{
						switch (text)
						{
						case "zh-tw":
						case "zh-hk":
						case "zh-mo":
							break;
						default:
							if (text.StartsWith("en", StringComparison.Ordinal))
							{
								return "en";
							}
							if (text.StartsWith("ko", StringComparison.Ordinal))
							{
								return "ko";
							}
							if (text.StartsWith("de", StringComparison.Ordinal))
							{
								return "de";
							}
							if (text.StartsWith("ja", StringComparison.Ordinal))
							{
								return "ja";
							}
							if (text.StartsWith("fr", StringComparison.Ordinal))
							{
								return "fr";
							}
							if (text.StartsWith("ru", StringComparison.Ordinal))
							{
								return "ru";
							}
							if (text.StartsWith("pl", StringComparison.Ordinal))
							{
								return "pl";
							}
							if (text.StartsWith("tr", StringComparison.Ordinal))
							{
								return "tr";
							}
							if (text.StartsWith("it", StringComparison.Ordinal))
							{
								return "it";
							}
							if (text.StartsWith("pt", StringComparison.Ordinal))
							{
								return "pt-br";
							}
							if (text == "es-419" || text.Contains("latam", StringComparison.Ordinal))
							{
								return "es-419";
							}
							if (text.StartsWith("es-", StringComparison.Ordinal))
							{
								string text2 = text;
								if (IsLatamRegion(text2.Substring(3, text2.Length - 3)))
								{
									return "es-419";
								}
								return "es-es";
							}
							if (text == "es")
							{
								return "es-es";
							}
							return "en";
						}
					}
					return "zht";
				}
			}
			return "zhs";
		}

		private static bool IsLatamRegion(string region)
		{
			switch (region)
			{
			case "ar":
			case "bo":
			case "cl":
			case "co":
			case "cr":
			case "cu":
			case "do":
			case "ec":
			case "sv":
			case "gt":
			case "hn":
			case "mx":
			case "ni":
			case "pa":
			case "py":
			case "pe":
			case "pr":
			case "uy":
			case "ve":
				return true;
			default:
				return false;
			}
		}

		private static string NormalizeTag(string tag)
		{
			return tag.Trim().Replace('_', '-').ToLowerInvariant();
		}

		private static string? GetClientLanguageCode()
		{
			try
			{
				if (GetMemberValue(GetMemberValue(AccessTools.Property(AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.SaveManager"), "Instance")?.GetValue(null), "SettingsSave"), "Language") is string text && !string.IsNullOrWhiteSpace(text))
				{
					return text;
				}
			}
			catch
			{
			}
			try
			{
				return TranslationServer.GetLocale();
			}
			catch
			{
				return null;
			}
		}

		private static object? GetMemberValue(object? obj, string name)
		{
			if (obj == null)
			{
				return null;
			}
			Type type = obj.GetType();
			PropertyInfo propertyInfo = AccessTools.Property(type, name);
			if (propertyInfo != null)
			{
				return propertyInfo.GetValue(obj);
			}
			return AccessTools.Field(type, name)?.GetValue(obj);
		}
	}
	public static class LoadOrderManagerMod
	{
		public const string ModId = "LoadOrderManager";

		private const string SelectedVersionEnvVar = "AMS_LOADER_SELECTED_VERSION";

		private static bool _initialized;

		private static bool _harmonyPatched;

		public static void Initialize()
		{
			if (!_initialized)
			{
				_initialized = true;
				DebugLog.Info("Initialize called.");
				DebugLog.Info("Version bundle: " + ResolveVersionBundle() + ".");
				DebugLog.Info("Log file: " + DebugLog.LogPath);
				ApplyHarmonyPatches();
				DebugLog.Info("Loaded.");
				SelfTest.Schedule();
			}
		}

		private static string ResolveVersionBundle()
		{
			try
			{
				string environmentVariable = Environment.GetEnvironmentVariable("AMS_LOADER_SELECTED_VERSION");
				return string.IsNullOrWhiteSpace(environmentVariable) ? "unknown (no launcher)" : environmentVariable;
			}
			catch
			{
				return "unknown";
			}
		}

		internal static void ApplyHarmonyPatches()
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			if (_harmonyPatched)
			{
				return;
			}
			_harmonyPatched = true;
			try
			{
				new Harmony("LoadOrderManager").PatchAll();
				DebugLog.Info("Harmony patches applied.");
			}
			catch (Exception ex)
			{
				DebugLog.Error("Harmony patch failed.", ex);
			}
		}
	}
	[ScriptPath("res://LoadOrderPanel.cs")]
	public class LoadOrderPanel : Control
	{
		private sealed class ClipboardPayload
		{
			public string format { get; set; } = "load_order_manager_v2";


			public string exported_at_utc { get; set; } = string.Empty;


			public List<ClipboardEntry> entries { get; set; } = new List<ClipboardEntry>();


			public PresetExportData? presets { get; set; }
		}

		private sealed class PresetExportData
		{
			public string current_preset { get; set; } = string.Empty;


			public List<PresetExportEntry> profiles { get; set; } = new List<PresetExportEntry>();

		}

		private sealed class PresetExportEntry
		{
			public string name { get; set; } = string.Empty;


			public List<string> disabled_mods { get; set; } = new List<string>();


			public bool auto_enable_new_mods { get; set; }

			public List<string> known_mods { get; set; } = new List<string>();

		}

		private sealed class ClipboardEntry
		{
			public string key { get; set; } = string.Empty;


			public string id { get; set; } = string.Empty;


			public int? source { get; set; }

			public bool? is_enabled { get; set; }

			[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
			public bool? enabled { get; set; }

			public bool? ResolveEnabled()
			{
				if (is_enabled.HasValue)
				{
					return is_enabled.Value;
				}
				if (enabled.HasValue)
				{
					return enabled.Value;
				}
				return null;
			}
		}

		public class MethodName : MethodName
		{
			public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

			public static readonly StringName _Input = StringName.op_Implicit("_Input");

			public static readonly StringName OpenPanel = StringName.op_Implicit("OpenPanel");

			public static readonly StringName BuildUiIfNeeded = StringName.op_Implicit("BuildUiIfNeeded");

			public static readonly StringName LayoutForViewport = StringName.op_Implicit("LayoutForViewport");

			public static readonly StringName ScheduleLayoutVerification = StringName.op_Implicit("ScheduleLayoutVerification");

			public static readonly StringName OnProcessFrame = StringName.op_Implicit("OnProcessFrame");

			public static readonly StringName CorrectLayoutOverflow = StringName.op_Implicit("CorrectLayoutOverflow");

			public static readonly StringName FixedChromeMinHeight = StringName.op_Implicit("FixedChromeMinHeight");

			public static readonly StringName BoundedMinHeight = StringName.op_Implicit("BoundedMinHeight");

			public static readonly StringName LogLayoutCheck = StringName.op_Implicit("LogLayoutCheck");

			public static readonly StringName ReadBoolEnv = StringName.op_Implicit("ReadBoolEnv");

			public static readonly StringName RefreshFromRuntime = StringName.op_Implicit("RefreshFromRuntime");

			public static readonly StringName RefreshOverwriteWarning = StringName.op_Implicit("RefreshOverwriteWarning");

			public static readonly StringName RefreshListOnly = StringName.op_Implicit("RefreshListOnly");

			public static readonly StringName GetSelectedIndex = StringName.op_Implicit("GetSelectedIndex");

			public static readonly StringName MoveSelected = StringName.op_Implicit("MoveSelected");

			public static readonly StringName MoveToTop = StringName.op_Implicit("MoveToTop");

			public static readonly StringName MoveToBottom = StringName.op_Implicit("MoveToBottom");

			public static readonly StringName SortByAlphabet = StringName.op_Implicit("SortByAlphabet");

			public static readonly StringName IsLibraryMod = StringName.op_Implicit("IsLibraryMod");

			public static readonly StringName SmartSort = StringName.op_Implicit("SmartSort");

			public static readonly StringName ToggleSelectedEnabled = StringName.op_Implicit("ToggleSelectedEnabled");

			public static readonly StringName RefreshPresetDropdown = StringName.op_Implicit("RefreshPresetDropdown");

			public static readonly StringName RefreshPolicyCheckbox = StringName.op_Implicit("RefreshPolicyCheckbox");

			public static readonly StringName OnNewModPolicyToggled = StringName.op_Implicit("OnNewModPolicyToggled");

			public static readonly StringName OnPresetSelected = StringName.op_Implicit("OnPresetSelected");

			public static readonly StringName CreatePresetFromCurrent = StringName.op_Implicit("CreatePresetFromCurrent");

			public static readonly StringName RenameCurrentPreset = StringName.op_Implicit("RenameCurrentPreset");

			public static readonly StringName DeleteCurrentPreset = StringName.op_Implicit("DeleteCurrentPreset");

			public static readonly StringName ExportToClipboard = StringName.op_Implicit("ExportToClipboard");

			public static readonly StringName ImportFromClipboard = StringName.op_Implicit("ImportFromClipboard");

			public static readonly StringName ApplyOrder = StringName.op_Implicit("ApplyOrder");

			public static readonly StringName SetStatus = StringName.op_Implicit("SetStatus");

			public static readonly StringName DebugLogLayoutCheck = StringName.op_Implicit("DebugLogLayoutCheck");

			public static readonly StringName DebugSelectPreset = StringName.op_Implicit("DebugSelectPreset");

			public static readonly StringName DebugCreatePreset = StringName.op_Implicit("DebugCreatePreset");

			public static readonly StringName DebugApplyOrder = StringName.op_Implicit("DebugApplyOrder");
		}

		public class PropertyName : PropertyName
		{
			public static readonly StringName _uiBuilt = StringName.op_Implicit("_uiBuilt");

			public static readonly StringName _list = StringName.op_Implicit("_list");

			public static readonly StringName _dialog = StringName.op_Implicit("_dialog");

			public static readonly StringName _margin = StringName.op_Implicit("_margin");

			public static readonly StringName _root = StringName.op_Implicit("_root");

			public static readonly StringName _title = StringName.op_Implicit("_title");

			public static readonly StringName _subtitle = StringName.op_Implicit("_subtitle");

			public static readonly StringName _statusLabel = StringName.op_Implicit("_statusLabel");

			public static readonly StringName _warningLabel = StringName.op_Implicit("_warningLabel");

			public static readonly StringName _presetBar = StringName.op_Implicit("_presetBar");

			public static readonly StringName _footer = StringName.op_Implicit("_footer");

			public static readonly StringName _sideScroll = StringName.op_Implicit("_sideScroll");

			public static readonly StringName _side = StringName.op_Implicit("_side");

			public static readonly StringName _presetDropdown = StringName.op_Implicit("_presetDropdown");

			public static readonly StringName _newModPolicyCheck = StringName.op_Implicit("_newModPolicyCheck");

			public static readonly StringName _suppressPresetEvent = StringName.op_Implicit("_suppressPresetEvent");

			public static readonly StringName _layoutViewport = StringName.op_Implicit("_layoutViewport");

			public static readonly StringName _layoutCheckHooked = StringName.op_Implicit("_layoutCheckHooked");

			public static readonly StringName _layoutCheckFramesPending = StringName.op_Implicit("_layoutCheckFramesPending");

			public static readonly StringName _layoutStepsRemaining = StringName.op_Implicit("_layoutStepsRemaining");
		}

		public class SignalName : SignalName
		{
		}

		private const string ClipboardFormatV1 = "load_order_manager_v1";

		private const string ClipboardFormatV2 = "load_order_manager_v2";

		private static readonly Regex TokenSplitRegex = new Regex("[\\r\\n,;|\\t]+", RegexOptions.Compiled);

		private const float DesignMaxWidth = 940f;

		private const float DesignMaxListHeight = 410f;

		private const float MinListHeight = 120f;

		private const float RootSeparation = 8f;

		private static readonly bool ForceCompact = ReadBoolEnv("LOADORDER_UI_COMPACT");

		private static readonly Vector2? SimulatedViewport = ReadSimulatedViewport();

		private readonly List<LoadOrderEntry> _entries = new List<LoadOrderEntry>();

		private bool _uiBuilt;

		private ItemList _list;

		private PanelContainer _dialog;

		private MarginContainer _margin;

		private VBoxContainer _root;

		private Label _title;

		private Label _subtitle;

		private Label _statusLabel;

		private Label _warningLabel;

		private HFlowContainer _presetBar;

		private HBoxContainer _footer;

		private ScrollContainer _sideScroll;

		private VBoxContainer _side;

		private OptionButton _presetDropdown;

		private CheckBox _newModPolicyCheck;

		private bool _suppressPresetEvent;

		private Vector2 _layoutViewport = new Vector2(1920f, 1080f);

		private bool _layoutCheckHooked;

		private int _layoutCheckFramesPending;

		private int _layoutStepsRemaining;

		private static readonly string[] LibKeywords = new string[8] { "lib", "base", "core", "framework", "common", "api", "helper", "toolkit" };

		internal IReadOnlyList<LoadOrderEntry> DebugEntries => _entries;

		public override void _Ready()
		{
			BuildUiIfNeeded();
			((CanvasItem)this).Visible = false;
			Viewport viewport = ((Node)this).GetViewport();
			if (viewport != null)
			{
				viewport.SizeChanged += LayoutForViewport;
			}
		}

		public override void _Input(InputEvent @event)
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Invalid comparison between Unknown and I8
			if (((CanvasItem)this).Visible)
			{
				InputEventKey val = (InputEventKey)(object)((@event is InputEventKey) ? @event : null);
				if (val != null && val.Pressed && !val.Echo && (long)val.Keycode == 4194305)
				{
					((CanvasItem)this).Visible = false;
					((Node)this).GetViewport().SetInputAsHandled();
				}
			}
		}

		public void OpenPanel()
		{
			BuildUiIfNeeded();
			((CanvasItem)this).Visible = true;
			DebugLog.Info("OpenPanel called.");
			LoadOrderRuntime.LogDiagnosticsOnce();
			LayoutForViewport();
			RefreshFromRuntime();
		}

		private void BuildUiIfNeeded()
		{
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Expected O, but got Unknown
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ee: Expected O, but got Unknown
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Expected O, but got Unknown
			//IL_0179: Unknown result type (might be due to invalid IL or missing references)
			//IL_0183: Expected O, but got Unknown
			//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d0: Expected O, but got Unknown
			//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0201: Unknown result type (might be due to invalid IL or missing references)
			//IL_0211: Unknown result type (might be due to invalid IL or missing references)
			//IL_0219: Unknown result type (might be due to invalid IL or missing references)
			//IL_0226: Expected O, but got Unknown
			//IL_023b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0240: Unknown result type (might be due to invalid IL or missing references)
			//IL_0247: Unknown result type (might be due to invalid IL or missing references)
			//IL_0252: Unknown result type (might be due to invalid IL or missing references)
			//IL_0267: Unknown result type (might be due to invalid IL or missing references)
			//IL_0271: Unknown result type (might be due to invalid IL or missing references)
			//IL_0279: Unknown result type (might be due to invalid IL or missing references)
			//IL_0281: Unknown result type (might be due to invalid IL or missing references)
			//IL_0289: Unknown result type (might be due to invalid IL or missing references)
			//IL_0290: Unknown result type (might be due to invalid IL or missing references)
			//IL_029d: Expected O, but got Unknown
			//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bc: Expected O, but got Unknown
			//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0301: Unknown result type (might be due to invalid IL or missing references)
			//IL_0311: Unknown result type (might be due to invalid IL or missing references)
			//IL_031a: Expected O, but got Unknown
			//IL_032a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0334: Expected O, but got Unknown
			//IL_0341: Unknown result type (might be due to invalid IL or missing references)
			//IL_034b: Expected O, but got Unknown
			//IL_035b: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_040d: Unknown result type (might be due to invalid IL or missing references)
			//IL_041d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0429: Expected O, but got Unknown
			//IL_0436: Unknown result type (might be due to invalid IL or missing references)
			//IL_0440: Expected O, but got Unknown
			//IL_0454: Unknown result type (might be due to invalid IL or missing references)
			//IL_045a: Expected O, but got Unknown
			//IL_0484: Unknown result type (might be due to invalid IL or missing references)
			//IL_0489: Unknown result type (might be due to invalid IL or missing references)
			//IL_0491: Unknown result type (might be due to invalid IL or missing references)
			//IL_049d: Expected O, but got Unknown
			//IL_04c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_04d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_04e9: Expected O, but got Unknown
			//IL_04f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0503: Expected O, but got Unknown
			//IL_06df: Unknown result type (might be due to invalid IL or missing references)
			//IL_06e9: Expected O, but got Unknown
			//IL_0768: Unknown result type (might be due to invalid IL or missing references)
			//IL_076d: Unknown result type (might be due to invalid IL or missing references)
			//IL_077d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0785: Unknown result type (might be due to invalid IL or missing references)
			//IL_0792: Expected O, but got Unknown
			if (!_uiBuilt)
			{
				_uiBuilt = true;
				((Control)this).AnchorLeft = 0f;
				((Control)this).AnchorTop = 0f;
				((Control)this).AnchorRight = 1f;
				((Control)this).AnchorBottom = 1f;
				((Control)this).OffsetLeft = 0f;
				((Control)this).OffsetTop = 0f;
				((Control)this).OffsetRight = 0f;
				((Control)this).OffsetBottom = 0f;
				((Control)this).MouseFilter = (MouseFilterEnum)0;
				((CanvasItem)this).ZIndex = 100;
				ColorRect val = new ColorRect
				{
					Color = new Color(0f, 0f, 0f, 0.62f),
					MouseFilter = (MouseFilterEnum)0
				};
				((Control)val).AnchorLeft = 0f;
				((Control)val).AnchorTop = 0f;
				((Control)val).AnchorRight = 1f;
				((Control)val).AnchorBottom = 1f;
				((Node)this).AddChild((Node)(object)val, false, (InternalMode)0);
				_dialog = new PanelContainer
				{
					MouseFilter = (MouseFilterEnum)0
				};
				((Node)val).AddChild((Node)(object)_dialog, false, (InternalMode)0);
				_margin = new MarginContainer();
				((Control)_margin).AddThemeConstantOverride(StringName.op_Implicit("margin_left"), 14);
				((Control)_margin).AddThemeConstantOverride(StringName.op_Implicit("margin_top"), 12);
				((Control)_margin).AddThemeConstantOverride(StringName.op_Implicit("margin_right"), 14);
				((Control)_margin).AddThemeConstantOverride(StringName.op_Implicit("margin_bottom"), 12);
				((Node)_dialog).AddChild((Node)(object)_margin, false, (InternalMode)0);
				_root = new VBoxContainer();
				((Control)_root).AddThemeConstantOverride(StringName.op_Implicit("separation"), 8);
				((Node)_margin).AddChild((Node)(object)_root, false, (InternalMode)0);
				_title = new Label
				{
					Text = I18n.T("title"),
					HorizontalAlignment = (HorizontalAlignment)1
				};
				((Control)_title).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 22);
				((Node)_root).AddChild((Node)(object)_title, false, (InternalMode)0);
				_subtitle = new Label
				{
					Text = I18n.T("subtitle"),
					HorizontalAlignment = (HorizontalAlignment)1,
					AutowrapMode = (AutowrapMode)3
				};
				((Node)_root).AddChild((Node)(object)_subtitle, false, (InternalMode)0);
				_warningLabel = new Label
				{
					Visible = false,
					Text = string.Empty,
					Modulate = new Color(1f, 0.77f, 0.3f, 1f),
					AutowrapMode = (AutowrapMode)3,
					HorizontalAlignment = (HorizontalAlignment)1,
					SizeFlagsHorizontal = (SizeFlags)3,
					MaxLinesVisible = 2,
					TextOverrunBehavior = (OverrunBehavior)3
				};
				((Node)_root).AddChild((Node)(object)_warningLabel, false, (InternalMode)0);
				_presetBar = new HFlowContainer();
				((Control)_presetBar).AddThemeConstantOverride(StringName.op_Implicit("h_separation"), 6);
				((Control)_presetBar).AddThemeConstantOverride(StringName.op_Implicit("v_separation"), 4);
				((Node)_root).AddChild((Node)(object)_presetBar, false, (InternalMode)0);
				Label val2 = new Label
				{
					Text = I18n.T("preset_label"),
					VerticalAlignment = (VerticalAlignment)1
				};
				((Node)_presetBar).AddChild((Node)(object)val2, false, (InternalMode)0);
				_presetDropdown = new OptionButton();
				_presetDropdown.ItemSelected += new ItemSelectedEventHandler(OnPresetSelected);
				((Control)_presetDropdown).CustomMinimumSize = new Vector2(200f, 0f);
				((Node)_presetBar).AddChild((Node)(object)_presetDropdown, false, (InternalMode)0);
				((Node)_presetBar).AddChild((Node)(object)MakeButton(I18n.T("btn_preset_new"), CreatePresetFromCurrent), false, (InternalMode)0);
				((Node)_presetBar).AddChild((Node)(object)MakeButton(I18n.T("btn_preset_rename"), RenameCurrentPreset), false, (InternalMode)0);
				((Node)_presetBar).AddChild((Node)(object)MakeButton(I18n.T("btn_preset_delete"), DeleteCurrentPreset), false, (InternalMode)0);
				_newModPolicyCheck = new CheckBox
				{
					Text = I18n.T("policy_auto_enable"),
					TooltipText = I18n.T("policy_auto_enable_tooltip"),
					ButtonPressed = true
				};
				((BaseButton)_newModPolicyCheck).Toggled += new ToggledEventHandler(OnNewModPolicyToggled);
				((Node)_presetBar).AddChild((Node)(object)_newModPolicyCheck, false, (InternalMode)0);
				HBoxContainer val3 = new HBoxContainer();
				((Control)val3).SizeFlagsVertical = (SizeFlags)3;
				((Control)val3).AddThemeConstantOverride(StringName.op_Implicit("separation"), 10);
				((Node)_root).AddChild((Node)(object)val3, false, (InternalMode)0);
				_list = new ItemList
				{
					SelectMode = (SelectModeEnum)0,
					AllowReselect = true
				};
				((Control)_list).SizeFlagsHorizontal = (SizeFlags)3;
				((Control)_list).SizeFlagsVertical = (SizeFlags)3;
				((Node)val3).AddChild((Node)(object)_list, false, (InternalMode)0);
				_sideScroll = new ScrollContainer
				{
					HorizontalScrollMode = (ScrollMode)0,
					VerticalScrollMode = (ScrollMode)1,
					SizeFlagsVertical = (SizeFlags)3
				};
				((Node)val3).AddChild((Node)(object)_sideScroll, false, (InternalMode)0);
				_side = new VBoxContainer();
				((Control)_side).AddThemeConstantOverride(StringName.op_Implicit("separation"), 6);
				((Control)_side).SizeFlagsHorizontal = (SizeFlags)3;
				((Node)_sideScroll).AddChild((Node)(object)_side, false, (InternalMode)0);
				((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_move_up"), delegate
				{
					MoveSelected(-1);
				}), false, (InternalMode)0);
				((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_move_down"), delegate
				{
					MoveSelected(1);
				}), false, (InternalMode)0);
				((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_move_top"), MoveToTop), false, (InternalMode)0);
				((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_move_bottom"), MoveToBottom), false, (InternalMode)0);
				((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_sort_alpha"), SortByAlphabet), false, (InternalMode)0);
				((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_sort_smart"), SmartSort), false, (InternalMode)0);
				((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_toggle_enable"), ToggleSelectedEnabled), false, (InternalMode)0);
				((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_reload"), RefreshFromRuntime), false, (InternalMode)0);
				((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_export_clipboard"), ExportToClipboard), false, (InternalMode)0);
				((Node)_side).AddChild((Node)(object)MakeButton(I18n.T("btn_import_clipboard"), ImportFromClipboard), false, (InternalMode)0);
				_footer = new HBoxContainer();
				((Control)_footer).AddThemeConstantOverride(StringName.op_Implicit("separation"), 8);
				((Node)_root).AddChild((Node)(object)_footer, false, (InternalMode)0);
				((Node)_footer).AddChild((Node)(object)MakeButton(I18n.T("btn_apply"), ApplyOrder), false, (InternalMode)0);
				((Node)_footer).AddChild((Node)(object)MakeButton(I18n.T("btn_close"), delegate
				{
					((CanvasItem)this).Visible = false;
				}), false, (InternalMode)0);
				_statusLabel = new Label
				{
					Text = I18n.T("status_ready"),
					SizeFlagsHorizontal = (SizeFlags)3,
					AutowrapMode = (AutowrapMode)3
				};
				_statusLabel.HorizontalAlignment = (HorizontalAlignment)2;
				((Node)_footer).AddChild((Node)(object)_statusLabel, false, (InternalMode)0);
				LayoutForViewport();
			}
		}

		private Button MakeButton(string text, Action onPressed)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Expected O, but got Unknown
			Button val = new Button
			{
				Text = text,
				FocusMode = (FocusModeEnum)2
			};
			((BaseButton)val).Pressed += onPressed;
			return val;
		}

		private void LayoutForViewport()
		{
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0204: Unknown result type (might be due to invalid IL or missing references)
			if (_uiBuilt && _dialog != null)
			{
				Vector2? simulatedViewport = SimulatedViewport;
				Vector2 layoutViewport;
				if (!simulatedViewport.HasValue)
				{
					Rect2 viewportRect = ((CanvasItem)this).GetViewportRect();
					layoutViewport = ((Rect2)(ref viewportRect)).Size;
				}
				else
				{
					layoutViewport = simulatedViewport.GetValueOrDefault();
				}
				Vector2 val = (_layoutViewport = layoutViewport);
				bool flag = ForceCompact || val.Y < 720f || val.X < 1100f;
				float num = (flag ? 12f : 24f);
				float num2 = (flag ? 12f : 48f);
				float num3 = (flag ? 12f : 48f);
				float num4 = Mathf.Clamp(val.X - num * 2f, 640f, 940f);
				float num5 = Mathf.Max(val.Y - num2 - num3, 260f);
				((Control)_dialog).AnchorLeft = 0.5f;
				((Control)_dialog).AnchorRight = 0.5f;
				((Control)_dialog).AnchorTop = 0f;
				((Control)_dialog).AnchorBottom = 0f;
				((Control)_dialog).OffsetLeft = (0f - num4) / 2f;
				((Control)_dialog).OffsetRight = num4 / 2f;
				((Control)_dialog).OffsetTop = num2;
				((Control)_dialog).OffsetBottom = num2 + num5;
				((CanvasItem)_subtitle).Visible = !flag;
				float num6 = (flag ? 150f : 180f);
				float num7 = Mathf.Clamp(num5 - FixedChromeMinHeight(), 120f, 410f);
				((Control)_list).CustomMinimumSize = new Vector2(Mathf.Max(num4 - num6 - 40f, 320f), num7);
				((Control)_sideScroll).CustomMinimumSize = new Vector2(num6, Mathf.Min(300f, num7));
				DebugLog.Info($"UI layout: viewport={val.X:0}x{val.Y:0}, compact={flag}, dialog={num4:0}x{num5:0}, top={num2:0}, listMinHeight={num7:0}");
				ScheduleLayoutVerification();
			}
		}

		private void ScheduleLayoutVerification()
		{
			_layoutStepsRemaining = 2;
			_layoutCheckFramesPending = 2;
			SceneTree tree = ((Node)this).GetTree();
			if (tree != null && !_layoutCheckHooked)
			{
				_layoutCheckHooked = true;
				tree.ProcessFrame += OnProcessFrame;
			}
		}

		private void OnProcessFrame()
		{
			if (_layoutStepsRemaining > 0)
			{
				if (_layoutCheckFramesPending > 0)
				{
					_layoutCheckFramesPending--;
				}
				else if (_layoutStepsRemaining == 2)
				{
					_layoutStepsRemaining = 1;
					CorrectLayoutOverflow();
					_layoutCheckFramesPending = 2;
				}
				else
				{
					_layoutStepsRemaining = 0;
					LogLayoutCheck();
				}
			}
		}

		private void CorrectLayoutOverflow()
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			if (_dialog == null || _list == null)
			{
				return;
			}
			float num = ((Control)_dialog).OffsetBottom - ((Control)_dialog).OffsetTop;
			float num2 = ((Control)_dialog).Size.Y - num;
			if (!(num2 <= 1f))
			{
				float y = ((Control)_list).CustomMinimumSize.Y;
				float num3 = Mathf.Max(120f, y - num2);
				if (!(Mathf.Abs(num3 - y) < 0.5f))
				{
					((Control)_list).CustomMinimumSize = new Vector2(((Control)_list).CustomMinimumSize.X, num3);
					DebugLog.Info($"UI layout correction: panel exceeded its box by {num2:0}px, list minimum {y:0} -> {num3:0}");
				}
			}
		}

		private float FixedChromeMinHeight()
		{
			float sum = 0f;
			int visible = 0;
			Add((Control)(object)_title, 44f);
			Add((Control)(object)_subtitle, 70f);
			Add((Control)(object)_warningLabel, 56f);
			Add((Control)(object)_presetBar, 220f);
			Add((Control)(object)_footer, 80f);
			visible++;
			if (visible > 1)
			{
				sum += 8f * (float)(visible - 1);
			}
			sum += 24f;
			return sum + 8f;
			void Add(Control control, float cap)
			{
				if (((CanvasItem)control).Visible)
				{
					sum += BoundedMinHeight(control, cap);
					visible++;
				}
			}
		}

		private static float BoundedMinHeight(Control control, float cap)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			float y = control.GetCombinedMinimumSize().Y;
			if (float.IsNaN(y) || y <= 0f)
			{
				return cap;
			}
			return Mathf.Min(y, cap);
		}

		private void LogLayoutCheck()
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0111: Unknown result type (might be due to invalid IL or missing references)
			//IL_0139: Unknown result type (might be due to invalid IL or missing references)
			//IL_0180: Unknown result type (might be due to invalid IL or missing references)
			//IL_019e: Unknown result type (might be due to invalid IL or missing references)
			if (((Node)this).IsInsideTree() && _dialog != null && _footer != null)
			{
				Vector2 layoutViewport = _layoutViewport;
				float num = ((Control)_footer).GlobalPosition.Y + ((Control)_footer).Size.Y;
				float num2 = ((Control)_dialog).Position.Y + ((Control)_dialog).Size.Y;
				bool flag = num <= layoutViewport.Y + 0.5f && ((Control)_footer).GlobalPosition.Y >= -0.5f;
				bool flag2 = num2 <= layoutViewport.Y + 0.5f;
				DebugLog.Info($"UI layout check: dialogPos=({((Control)_dialog).Position.X:0},{((Control)_dialog).Position.Y:0}) dialogSize={((Control)_dialog).Size.X:0}x{((Control)_dialog).Size.Y:0}, footerEndY={num:0}, viewport={layoutViewport.X:0}x{layoutViewport.Y:0}, footerInside={flag}, dialogInside={flag2}" + ((flag && flag2) ? string.Empty : "  <-- LAYOUT OVERFLOW"));
			}
		}

		private static bool ReadBoolEnv(string name)
		{
			try
			{
				string environmentVariable = Environment.GetEnvironmentVariable(name);
				if (string.IsNullOrWhiteSpace(environmentVariable))
				{
					return false;
				}
				return environmentVariable != "0" && !environmentVariable.Equals("false", StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}

		private static Vector2? ReadSimulatedViewport()
		{
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				string environmentVariable = Environment.GetEnvironmentVariable("LOADORDER_UI_SIMULATE_H");
				if (string.IsNullOrWhiteSpace(environmentVariable))
				{
					return null;
				}
				if (!float.TryParse(environmentVariable, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
				{
					return null;
				}
				if (result <= 0f)
				{
					return null;
				}
				float num = 1163f;
				string environmentVariable2 = Environment.GetEnvironmentVariable("LOADORDER_UI_SIMULATE_W");
				if (!string.IsNullOrWhiteSpace(environmentVariable2) && float.TryParse(environmentVariable2, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2) && result2 > 0f)
				{
					num = result2;
				}
				return new Vector2(num, result);
			}
			catch
			{
				return null;
			}
		}

		private void RefreshFromRuntime()
		{
			if (!LoadOrderRuntime.TryGetOrderedEntries(out List<LoadOrderEntry> entries, out string error))
			{
				_entries.Clear();
				RefreshListOnly();
				SetStatus(I18n.Tf("status_load_failed", error));
				DebugLog.Error("Refresh failed: " + error);
				RefreshOverwriteWarning();
				return;
			}
			_entries.Clear();
			_entries.AddRange(entries);
			ModPresetStore.MigrateIfNeeded(_entries);
			int num = ModPresetStore.ApplyNewModPolicy(ModPresetStore.CurrentProfile, _entries);
			RefreshListOnly();
			RefreshPresetDropdown();
			RefreshPolicyCheckbox();
			if (_entries.Count > 0)
			{
				_list.Select(0, true);
			}
			SetStatus((num > 0) ? I18n.Tf("status_new_mods_seen", num) : I18n.Tf("status_loaded", _entries.Count));
			DebugLog.Info($"Loaded {_entries.Count} mods into panel. newMods={num}");
			RefreshOverwriteWarning();
		}

		private void RefreshOverwriteWarning()
		{
			if (LoadOrderRuntime.TryBuildOverwriteWarning(out string warningText) && !string.IsNullOrWhiteSpace(warningText))
			{
				_warningLabel.Text = warningText;
				((Control)_warningLabel).TooltipText = warningText;
				((CanvasItem)_warningLabel).Visible = true;
			}
			else
			{
				_warningLabel.Text = string.Empty;
				((Control)_warningLabel).TooltipText = string.Empty;
				((CanvasItem)_warningLabel).Visible = false;
			}
			LayoutForViewport();
		}

		private void RefreshListOnly()
		{
			//IL_0113: Unknown result type (might be due to invalid IL or missing references)
			_list.Clear();
			for (int i = 0; i < _entries.Count; i++)
			{
				LoadOrderEntry loadOrderEntry = _entries[i];
				string value = (loadOrderEntry.IsEnabled ? "  ✓" : "  ✗");
				string value2 = (loadOrderEntry.IsNew ? ("  · " + I18n.T("tag_new")) : string.Empty);
				string text = $"{i + 1:00}. [{loadOrderEntry.SourceText}] {loadOrderEntry.Name} ({loadOrderEntry.Id}){value}{value2}";
				_list.AddItem(text, (Texture2D)null, true);
				if (!loadOrderEntry.IsEnabled)
				{
					_list.SetItemCustomFgColor(i, new Color(0.55f, 0.55f, 0.55f, 1f));
				}
			}
		}

		private int GetSelectedIndex()
		{
			int[] selectedItems = _list.GetSelectedItems();
			if (selectedItems.Length != 0)
			{
				return selectedItems[0];
			}
			return -1;
		}

		private void MoveSelected(int delta)
		{
			int selectedIndex = GetSelectedIndex();
			if (selectedIndex >= 0)
			{
				int num = selectedIndex + delta;
				if (num >= 0 && num < _entries.Count)
				{
					List<LoadOrderEntry> entries = _entries;
					int index = selectedIndex;
					List<LoadOrderEntry> entries2 = _entries;
					int index2 = num;
					LoadOrderEntry value = _entries[num];
					LoadOrderEntry value2 = _entries[selectedIndex];
					entries[index] = value;
					entries2[index2] = value2;
					RefreshListOnly();
					_list.Select(num, true);
					SetStatus(I18n.Tf("status_moved_pos", num + 1));
				}
			}
		}

		private void MoveToTop()
		{
			int selectedIndex = GetSelectedIndex();
			if (selectedIndex > 0)
			{
				LoadOrderEntry item = _entries[selectedIndex];
				_entries.RemoveAt(selectedIndex);
				_entries.Insert(0, item);
				RefreshListOnly();
				_list.Select(0, true);
				SetStatus(I18n.T("status_moved_top"));
			}
		}

		private void MoveToBottom()
		{
			int selectedIndex = GetSelectedIndex();
			if (selectedIndex >= 0 && selectedIndex < _entries.Count - 1)
			{
				LoadOrderEntry item = _entries[selectedIndex];
				_entries.RemoveAt(selectedIndex);
				_entries.Add(item);
				RefreshListOnly();
				_list.Select(_entries.Count - 1, true);
				SetStatus(I18n.T("status_moved_bottom"));
			}
		}

		private void SortByAlphabet()
		{
			if (_entries.Count < 2)
			{
				SetStatus(I18n.T("status_nothing_to_sort"));
				return;
			}
			int selectedIndex = GetSelectedIndex();
			string selectedKey = ((selectedIndex >= 0 && selectedIndex < _entries.Count) ? _entries[selectedIndex].Key : null);
			_entries.Sort(delegate(LoadOrderEntry a, LoadOrderEntry b)
			{
				int num2 = string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
				if (num2 != 0)
				{
					return num2;
				}
				int num3 = string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase);
				return (num3 != 0) ? num3 : a.Source.CompareTo(b.Source);
			});
			RefreshListOnly();
			if (!string.IsNullOrEmpty(selectedKey))
			{
				int num = _entries.FindIndex((LoadOrderEntry e) => string.Equals(e.Key, selectedKey, StringComparison.Ordinal));
				if (num >= 0)
				{
					_list.Select(num, true);
				}
			}
			SetStatus(I18n.T("status_sorted_alpha"));
			DebugLog.Info($"Manual alphabetical sort completed. entries={_entries.Count}");
		}

		private static string SortKeyFor(LoadOrderEntry e)
		{
			return ((IsLibraryMod(e.Id) || IsLibraryMod(e.Name)) ? "0_" : "1_") + e.Name;
		}

		private static bool IsLibraryMod(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			string text2 = text.ToLowerInvariant();
			string[] libKeywords = LibKeywords;
			foreach (string text3 in libKeywords)
			{
				if (text2 == text3)
				{
					return true;
				}
				if (text2.StartsWith(text3))
				{
					return true;
				}
				for (int j = 1; j < text2.Length; j++)
				{
					if (char.IsUpper(text[j]) && text2.Substring(0, j) == text3)
					{
						return true;
					}
				}
				if (text2.EndsWith(text3, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		private void SmartSort()
		{
			if (_entries.Count < 2)
			{
				SetStatus(I18n.T("status_nothing_to_sort"));
				return;
			}
			int selectedIndex = GetSelectedIndex();
			string selectedKey = ((selectedIndex >= 0 && selectedIndex < _entries.Count) ? _entries[selectedIndex].Key : null);
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < _entries.Count; i++)
			{
				if (!string.IsNullOrWhiteSpace(_entries[i].Id))
				{
					dictionary.TryAdd(_entries[i].Id, i);
				}
			}
			int[] array = new int[_entries.Count];
			List<List<int>> list = new List<List<int>>();
			for (int j = 0; j < _entries.Count; j++)
			{
				list.Add(new List<int>());
			}
			for (int k = 0; k < _entries.Count; k++)
			{
				foreach (string dependency in _entries[k].Dependencies)
				{
					if (dictionary.TryGetValue(dependency, out var value))
					{
						array[k]++;
						list[value].Add(k);
					}
				}
			}
			for (int l = 0; l < _entries.Count; l++)
			{
				if (array[l] > 0)
				{
					continue;
				}
				string text = _entries[l].Id ?? string.Empty;
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				for (int m = 0; m < _entries.Count; m++)
				{
					if (l == m)
					{
						continue;
					}
					foreach (string dependency2 in _entries[m].Dependencies)
					{
						if (string.Equals(dependency2, text, StringComparison.OrdinalIgnoreCase))
						{
							array[m]++;
							list[l].Add(m);
							break;
						}
					}
				}
			}
			PriorityQueue<int, string> priorityQueue = new PriorityQueue<int, string>(StringComparer.CurrentCultureIgnoreCase);
			for (int n = 0; n < _entries.Count; n++)
			{
				if (array[n] == 0)
				{
					priorityQueue.Enqueue(n, SortKeyFor(_entries[n]));
				}
			}
			List<LoadOrderEntry> list2 = new List<LoadOrderEntry>();
			HashSet<int> hashSet = new HashSet<int>();
			while (priorityQueue.Count > 0)
			{
				int num = priorityQueue.Dequeue();
				hashSet.Add(num);
				list2.Add(_entries[num]);
				foreach (int item in list[num])
				{
					array[item]--;
					if (array[item] == 0)
					{
						priorityQueue.Enqueue(item, SortKeyFor(_entries[item]));
					}
				}
			}
			List<LoadOrderEntry> list3 = new List<LoadOrderEntry>();
			for (int num2 = 0; num2 < _entries.Count; num2++)
			{
				if (!hashSet.Contains(num2))
				{
					list3.Add(_entries[num2]);
				}
			}
			list3.Sort((LoadOrderEntry a, LoadOrderEntry b) => string.Compare(SortKeyFor(a), SortKeyFor(b), StringComparison.CurrentCultureIgnoreCase));
			list2.AddRange(list3);
			_entries.Clear();
			_entries.AddRange(list2);
			RefreshListOnly();
			if (!string.IsNullOrEmpty(selectedKey))
			{
				int num3 = _entries.FindIndex((LoadOrderEntry e) => string.Equals(e.Key, selectedKey, StringComparison.Ordinal));
				if (num3 >= 0)
				{
					_list.Select(num3, true);
				}
			}
			int count = list3.Count;
			SetStatus((count > 0) ? I18n.Tf("status_sorted_smart_circular", count) : I18n.T("status_sorted_smart"));
			DebugLog.Info($"Smart sort completed. entries={_entries.Count}, circular={count}");
		}

		private void ToggleSelectedEnabled()
		{
			int selectedIndex = GetSelectedIndex();
			if (selectedIndex >= 0)
			{
				LoadOrderEntry loadOrderEntry = _entries[selectedIndex];
				loadOrderEntry.IsEnabled = !loadOrderEntry.IsEnabled;
				loadOrderEntry.IsNew = false;
				RefreshListOnly();
				_list.Select(selectedIndex, true);
				string text = (loadOrderEntry.IsEnabled ? "ON" : "OFF");
				SetStatus(I18n.Tf("status_toggled", loadOrderEntry.Name, text));
				DebugLog.Info($"Toggled {loadOrderEntry.Id} to {loadOrderEntry.IsEnabled}.");
			}
		}

		private void RefreshPresetDropdown()
		{
			if (_presetDropdown != null)
			{
				_suppressPresetEvent = true;
				PresetSaveData presetSaveData = ModPresetStore.Load();
				_presetDropdown.Clear();
				for (int i = 0; i < presetSaveData.Profiles.Count; i++)
				{
					_presetDropdown.AddItem(presetSaveData.Profiles[i].Name, i);
				}
				_presetDropdown.Select(presetSaveData.CurrentProfileIndex);
				_suppressPresetEvent = false;
			}
		}

		private void RefreshPolicyCheckbox()
		{
			if (_newModPolicyCheck != null)
			{
				ModPreset currentProfile = ModPresetStore.CurrentProfile;
				((BaseButton)_newModPolicyCheck).SetPressedNoSignal(currentProfile.AutoEnableNewMods);
				((Control)_newModPolicyCheck).TooltipText = I18n.T("policy_auto_enable_tooltip");
			}
		}

		private void OnNewModPolicyToggled(bool pressed)
		{
			ModPreset currentProfile = ModPresetStore.CurrentProfile;
			if (!ModPresetStore.SetAutoEnableNewMods(pressed))
			{
				SetStatus(I18n.T("status_save_failed"));
				return;
			}
			int value = ModPresetStore.ApplyNewModPolicy(ModPresetStore.CurrentProfile, _entries);
			RefreshListOnly();
			SetStatus(I18n.Tf("status_policy_changed", currentProfile.Name, I18n.T(pressed ? "policy_on" : "policy_off")));
			DebugLog.Info($"Preset '{currentProfile.Name}' AutoEnableNewMods={pressed}; newMods={value}");
		}

		private void OnPresetSelected(long index)
		{
			if (!_suppressPresetEvent && index >= 0 && index < ModPresetStore.Load().Profiles.Count)
			{
				ModPresetStore.SelectProfile((int)index, _entries);
				ModPreset currentProfile = ModPresetStore.CurrentProfile;
				int num = ModPresetStore.ApplyPreset(currentProfile, _entries);
				RefreshListOnly();
				RefreshPolicyCheckbox();
				string text = I18n.Tf("status_preset_applied", currentProfile.Name);
				if (num > 0)
				{
					text = text + " " + I18n.Tf("status_new_mods_seen", num);
				}
				SetStatus(text);
				DebugLog.Info($"Preset switched to '{currentProfile.Name}'. autoEnableNewMods={currentProfile.AutoEnableNewMods}, newMods={num}");
			}
		}

		private void CreatePresetFromCurrent()
		{
			PresetSaveData presetSaveData = ModPresetStore.Load();
			string name = "Profile " + (presetSaveData.Profiles.Count + 1);
			CreatePresetFromCurrent(name);
		}

		private void CreatePresetFromCurrent(string name)
		{
			ModPresetStore.CreatePreset(name, _entries);
			RefreshPresetDropdown();
			RefreshPolicyCheckbox();
			SetStatus(I18n.Tf("status_preset_created", name));
			DebugLog.Info("Created preset '" + name + "'.");
		}

		private void RenameCurrentPreset()
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Expected O, but got Unknown
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Expected O, but got Unknown
			ConfirmationDialog val = new ConfirmationDialog
			{
				Title = I18n.T("preset_rename_title"),
				DialogText = I18n.T("preset_rename_prompt")
			};
			LineEdit lineEdit = new LineEdit
			{
				Text = ModPresetStore.CurrentProfile.Name
			};
			((Node)val).AddChild((Node)(object)lineEdit, false, (InternalMode)0);
			((AcceptDialog)val).RegisterTextEnter(lineEdit);
			((AcceptDialog)val).Confirmed += delegate
			{
				string text = lineEdit.Text;
				if (ModPresetStore.RenameCurrentPreset(text))
				{
					RefreshPresetDropdown();
					RefreshPolicyCheckbox();
					SetStatus(I18n.Tf("status_preset_renamed", text.Trim()));
					DebugLog.Info("Preset renamed to '" + text.Trim() + "'.");
				}
			};
			((Node)this).AddChild((Node)(object)val, false, (InternalMode)0);
			((Window)val).PopupCentered((Vector2I?)null);
		}

		private void DeleteCurrentPreset()
		{
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Expected O, but got Unknown
			if (ModPresetStore.Load().Profiles.Count <= 1)
			{
				SetStatus(I18n.T("status_preset_cannot_delete_last"));
				return;
			}
			string presetName = ModPresetStore.CurrentProfile.Name;
			ConfirmationDialog val = new ConfirmationDialog();
			((Window)val).Title = I18n.T("preset_delete_title");
			((AcceptDialog)val).DialogText = I18n.Tf("preset_delete_prompt", presetName);
			ConfirmationDialog val2 = val;
			((AcceptDialog)val2).Confirmed += delegate
			{
				if (ModPresetStore.DeleteCurrentPreset())
				{
					RefreshPresetDropdown();
					RefreshPolicyCheckbox();
					SetStatus(I18n.Tf("status_preset_deleted", presetName));
					DebugLog.Info("Preset '" + presetName + "' deleted.");
				}
			};
			((Node)this).AddChild((Node)(object)val2, false, (InternalMode)0);
			((Window)val2).PopupCentered((Vector2I?)null);
		}

		private void ExportToClipboard()
		{
			if (_entries.Count == 0)
			{
				SetStatus(I18n.T("status_nothing_to_export"));
				return;
			}
			try
			{
				PresetSaveData presetSaveData = ModPresetStore.Load();
				PresetExportData presets = new PresetExportData
				{
					current_preset = presetSaveData.Profiles[presetSaveData.CurrentProfileIndex].Name,
					profiles = presetSaveData.Profiles.Select((ModPreset p) => new PresetExportEntry
					{
						name = p.Name,
						disabled_mods = p.DisabledMods.ToList(),
						auto_enable_new_mods = p.AutoEnableNewMods,
						known_mods = p.KnownMods.ToList()
					}).ToList()
				};
				ClipboardPayload clipboardPayload = new ClipboardPayload
				{
					exported_at_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
					entries = _entries.Select((LoadOrderEntry e) => new ClipboardEntry
					{
						key = e.Key,
						id = e.Id,
						source = e.Source,
						is_enabled = e.IsEnabled
					}).ToList(),
					presets = presets
				};
				DisplayServer.ClipboardSet(JsonSerializer.Serialize(clipboardPayload, new JsonSerializerOptions
				{
					WriteIndented = true
				}));
				SetStatus(I18n.Tf("status_exported_clipboard", clipboardPayload.entries.Count));
				DebugLog.Info($"Exported load-order config to clipboard. entries={clipboardPayload.entries.Count}");
			}
			catch (Exception ex)
			{
				SetStatus(I18n.Tf("status_export_failed", ex.Message));
				DebugLog.Error("ExportToClipboard failed.", ex);
			}
		}

		private void ImportFromClipboard()
		{
			try
			{
				string text = DisplayServer.ClipboardGet();
				if (string.IsNullOrWhiteSpace(text))
				{
					SetStatus(I18n.T("status_clipboard_empty"));
					return;
				}
				if (!TryParseClipboard(text, out List<ClipboardEntry> entries, out string error))
				{
					SetStatus(I18n.Tf("status_import_parse_failed", error));
					return;
				}
				if (entries.Count == 0)
				{
					SetStatus(I18n.T("status_import_no_valid"));
					return;
				}
				List<LoadOrderEntry> list = new List<LoadOrderEntry>();
				HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
				int num = 0;
				int num2 = 0;
				foreach (ClipboardEntry item in entries)
				{
					if (!TryResolveImportedEntry(item, out LoadOrderEntry entry))
					{
						num++;
						continue;
					}
					if (!seen.Add(entry.Key))
					{
						num2++;
						continue;
					}
					bool? flag = item.ResolveEnabled();
					if (flag.HasValue)
					{
						entry.IsEnabled = flag.Value;
						entry.IsNew = false;
					}
					list.Add(entry);
				}
				if (list.Count == 0)
				{
					SetStatus(I18n.Tf("status_import_no_match", num));
					DebugLog.Warn($"Clipboard import found no matching mods. parsed={entries.Count}, missing={num}");
					return;
				}
				List<LoadOrderEntry> collection = _entries.Where((LoadOrderEntry e) => !seen.Contains(e.Key)).ToList();
				_entries.Clear();
				_entries.AddRange(list);
				_entries.AddRange(collection);
				RefreshListOnly();
				if (_entries.Count > 0)
				{
					_list.Select(0, true);
				}
				SetStatus(I18n.Tf("status_imported_clipboard", list.Count, num, num2));
				DebugLog.Info($"Imported load-order config from clipboard. parsed={entries.Count}, matched={list.Count}, missing={num}, duplicates={num2}");
			}
			catch (Exception ex)
			{
				SetStatus(I18n.Tf("status_import_failed", ex.Message));
				DebugLog.Error("ImportFromClipboard failed.", ex);
			}
		}

		private bool TryParseClipboard(string raw, out List<ClipboardEntry> entries, out string error)
		{
			entries = new List<ClipboardEntry>();
			error = string.Empty;
			if (TryParseClipboardJson(raw, out entries, out error))
			{
				return true;
			}
			entries = ParsePlainTextClipboard(raw);
			if (entries.Count > 0)
			{
				return true;
			}
			if (string.IsNullOrWhiteSpace(error))
			{
				error = I18n.T("status_import_invalid_format");
			}
			return false;
		}

		private bool TryParseClipboardJson(string raw, out List<ClipboardEntry> entries, out string error)
		{
			entries = new List<ClipboardEntry>();
			error = string.Empty;
			try
			{
				using JsonDocument jsonDocument = JsonDocument.Parse(raw);
				JsonElement rootElement = jsonDocument.RootElement;
				if (rootElement.ValueKind == JsonValueKind.Object)
				{
					if (rootElement.TryGetProperty("entries", out var value))
					{
						entries = ParseClipboardJsonArray(value);
						return true;
					}
					if (rootElement.TryGetProperty("mod_list", out var value2))
					{
						entries = ParseClipboardJsonArray(value2);
						return true;
					}
					return false;
				}
				if (rootElement.ValueKind == JsonValueKind.Array)
				{
					entries = ParseClipboardJsonArray(rootElement);
					return true;
				}
				return false;
			}
			catch (JsonException ex)
			{
				error = ex.Message;
				return false;
			}
		}

		private List<ClipboardEntry> ParseClipboardJsonArray(JsonElement element)
		{
			List<ClipboardEntry> list = new List<ClipboardEntry>();
			if (element.ValueKind != JsonValueKind.Array)
			{
				return list;
			}
			foreach (JsonElement item in element.EnumerateArray())
			{
				switch (item.ValueKind)
				{
				case JsonValueKind.String:
				{
					string text = item.GetString() ?? string.Empty;
					if (!string.IsNullOrWhiteSpace(text))
					{
						list.Add(new ClipboardEntry
						{
							key = text.Trim(),
							id = text.Trim()
						});
					}
					break;
				}
				case JsonValueKind.Object:
				{
					ClipboardEntry clipboardEntry = new ClipboardEntry
					{
						key = GetJsonString(item, "key"),
						id = GetJsonString(item, "id"),
						source = GetJsonInt(item, "source"),
						is_enabled = (GetJsonBool(item, "is_enabled") ?? GetJsonBool(item, "isEnabled")),
						enabled = GetJsonBool(item, "enabled")
					};
					if (!string.IsNullOrWhiteSpace(clipboardEntry.key) || !string.IsNullOrWhiteSpace(clipboardEntry.id) || clipboardEntry.source.HasValue)
					{
						list.Add(clipboardEntry);
					}
					break;
				}
				}
			}
			return list;
		}

		private static string GetJsonString(JsonElement obj, string key)
		{
			if (obj.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
			{
				return value.GetString() ?? string.Empty;
			}
			return string.Empty;
		}

		private static int? GetJsonInt(JsonElement obj, string key)
		{
			if (obj.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var value2))
			{
				return value2;
			}
			return null;
		}

		private static bool? GetJsonBool(JsonElement obj, string key)
		{
			if (obj.TryGetProperty(key, out var value))
			{
				if (value.ValueKind == JsonValueKind.True)
				{
					return true;
				}
				if (value.ValueKind == JsonValueKind.False)
				{
					return false;
				}
			}
			return null;
		}

		private List<ClipboardEntry> ParsePlainTextClipboard(string raw)
		{
			List<ClipboardEntry> list = new List<ClipboardEntry>();
			string[] array = TokenSplitRegex.Split(raw);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim().Trim('"', '\'');
				if (!string.IsNullOrWhiteSpace(text))
				{
					list.Add(new ClipboardEntry
					{
						key = text,
						id = text
					});
				}
			}
			return list;
		}

		private bool TryResolveImportedEntry(ClipboardEntry imported, out LoadOrderEntry entry)
		{
			entry = null;
			string text = imported.key?.Trim() ?? string.Empty;
			string importedId = imported.id?.Trim() ?? string.Empty;
			int? importedSource = imported.source;
			if (!string.IsNullOrWhiteSpace(text) && TryFindByKey(text, out entry))
			{
				return true;
			}
			if (!string.IsNullOrWhiteSpace(text) && TryParseEntryKey(text, out string id, out int source))
			{
				importedId = id;
				int valueOrDefault = importedSource.GetValueOrDefault();
				if (!importedSource.HasValue)
				{
					valueOrDefault = source;
					importedSource = valueOrDefault;
				}
			}
			if (string.IsNullOrWhiteSpace(importedId) && !string.IsNullOrWhiteSpace(text))
			{
				importedId = text;
			}
			if (string.IsNullOrWhiteSpace(importedId))
			{
				return false;
			}
			if (importedSource.HasValue)
			{
				string key = LoadOrderEntry.BuildKey(importedId, importedSource.Value);
				if (TryFindByKey(key, out entry))
				{
					return true;
				}
			}
			List<LoadOrderEntry> list = _entries.Where((LoadOrderEntry e) => string.Equals(e.Id, importedId, StringComparison.OrdinalIgnoreCase)).ToList();
			if (list.Count == 0)
			{
				return false;
			}
			if (list.Count == 1)
			{
				entry = list[0];
				return true;
			}
			if (importedSource.HasValue)
			{
				LoadOrderEntry loadOrderEntry = list.FirstOrDefault((LoadOrderEntry e) => e.Source == importedSource.Value);
				if (loadOrderEntry != null)
				{
					entry = loadOrderEntry;
					return true;
				}
			}
			entry = list.FirstOrDefault((LoadOrderEntry e) => e.Source == 1) ?? list[0];
			return true;
		}

		private bool TryFindByKey(string key, out LoadOrderEntry entry)
		{
			string key2 = key;
			entry = _entries.FirstOrDefault((LoadOrderEntry e) => string.Equals(e.Key, key2, StringComparison.OrdinalIgnoreCase));
			return entry != null;
		}

		private static bool TryParseEntryKey(string key, out string id, out int source)
		{
			id = string.Empty;
			source = 0;
			if (string.IsNullOrWhiteSpace(key))
			{
				return false;
			}
			int num = key.LastIndexOf("::", StringComparison.Ordinal);
			if (num <= 0 || num >= key.Length - 2)
			{
				return false;
			}
			string text = key.Substring(0, num).Trim();
			int num2 = num + 2;
			string s = key.Substring(num2, key.Length - num2).Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				return false;
			}
			id = text;
			source = result;
			return true;
		}

		private void ApplyOrder()
		{
			string error;
			if (_entries.Count == 0)
			{
				SetStatus(I18n.T("status_nothing_to_save"));
			}
			else if (!LoadOrderRuntime.TrySaveOrderedEntries(_entries, out error))
			{
				SetStatus(I18n.Tf("status_save_failed", error));
				DebugLog.Error("Apply failed: " + error);
				RefreshOverwriteWarning();
			}
			else
			{
				ModPresetStore.CommitCurrentProfile(_entries);
				SetStatus(I18n.T("status_saved"));
				DebugLog.Info("Apply succeeded. Preset '" + ModPresetStore.CurrentProfile.Name + "' committed. Restart required for effect.");
				RefreshOverwriteWarning();
			}
		}

		private void SetStatus(string text)
		{
			_statusLabel.Text = text;
		}

		internal void DebugLogLayoutCheck()
		{
			LogLayoutCheck();
		}

		internal void DebugSelectPreset(int index)
		{
			OnPresetSelected(index);
		}

		internal void DebugCreatePreset(string name)
		{
			CreatePresetFromCurrent(name);
		}

		internal void DebugApplyOrder()
		{
			ApplyOrder();
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<MethodInfo> GetGodotMethodList()
		{
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Expected O, but got Unknown
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
			//IL_013e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
			//IL_016d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0176: Unknown result type (might be due to invalid IL or missing references)
			//IL_019c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0223: Unknown result type (might be due to invalid IL or missing references)
			//IL_022e: Expected O, but got Unknown
			//IL_0229: Unknown result type (might be due to invalid IL or missing references)
			//IL_024a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0255: Unknown result type (might be due to invalid IL or missing references)
			//IL_027b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0284: Unknown result type (might be due to invalid IL or missing references)
			//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0308: Unknown result type (might be due to invalid IL or missing references)
			//IL_032e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0337: Unknown result type (might be due to invalid IL or missing references)
			//IL_035d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0366: Unknown result type (might be due to invalid IL or missing references)
			//IL_038c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0395: Unknown result type (might be due to invalid IL or missing references)
			//IL_03bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_03de: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_040f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0418: Unknown result type (might be due to invalid IL or missing references)
			//IL_043e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0447: Unknown result type (might be due to invalid IL or missing references)
			//IL_046d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0476: Unknown result type (might be due to invalid IL or missing references)
			//IL_049c: Unknown result type (might be due to invalid IL or missing references)
			//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_04cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_04f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_04fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0520: Unknown result type (might be due to invalid IL or missing references)
			//IL_0529: Unknown result type (might be due to invalid IL or missing references)
			//IL_054f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0558: Unknown result type (might be due to invalid IL or missing references)
			//IL_057e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0587: Unknown result type (might be due to invalid IL or missing references)
			//IL_05ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_05d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_05db: Unknown result type (might be due to invalid IL or missing references)
			//IL_0601: Unknown result type (might be due to invalid IL or missing references)
			//IL_0624: Unknown result type (might be due to invalid IL or missing references)
			//IL_062f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0655: Unknown result type (might be due to invalid IL or missing references)
			//IL_065e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0684: Unknown result type (might be due to invalid IL or missing references)
			//IL_06a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_06b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_06d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_06e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0707: Unknown result type (might be due to invalid IL or missing references)
			//IL_0710: Unknown result type (might be due to invalid IL or missing references)
			//IL_0736: Unknown result type (might be due to invalid IL or missing references)
			//IL_073f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0765: Unknown result type (might be due to invalid IL or missing references)
			//IL_076e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0794: Unknown result type (might be due to invalid IL or missing references)
			//IL_079d: Unknown result type (might be due to invalid IL or missing references)
			//IL_07c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_07e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_07f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0817: Unknown result type (might be due to invalid IL or missing references)
			//IL_0820: Unknown result type (might be due to invalid IL or missing references)
			//IL_0846: Unknown result type (might be due to invalid IL or missing references)
			//IL_0869: Unknown result type (might be due to invalid IL or missing references)
			//IL_0874: Unknown result type (might be due to invalid IL or missing references)
			//IL_089a: Unknown result type (might be due to invalid IL or missing references)
			//IL_08bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_08c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_08ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_08f7: Unknown result type (might be due to invalid IL or missing references)
			return new List<MethodInfo>(39)
			{
				new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName._Input, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)24, StringName.op_Implicit("event"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.OpenPanel, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.BuildUiIfNeeded, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.LayoutForViewport, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.ScheduleLayoutVerification, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.OnProcessFrame, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.CorrectLayoutOverflow, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.FixedChromeMinHeight, new PropertyInfo((Type)3, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.BoundedMinHeight, new PropertyInfo((Type)3, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
				{
					new PropertyInfo((Type)24, StringName.op_Implicit("control"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false),
					new PropertyInfo((Type)3, StringName.op_Implicit("cap"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.LogLayoutCheck, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.ReadBoolEnv, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("name"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.RefreshFromRuntime, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.RefreshOverwriteWarning, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.RefreshListOnly, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.GetSelectedIndex, new PropertyInfo((Type)2, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.MoveSelected, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)2, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.MoveToTop, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.MoveToBottom, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.SortByAlphabet, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.IsLibraryMod, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("text"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.SmartSort, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.ToggleSelectedEnabled, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.RefreshPresetDropdown, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.RefreshPolicyCheckbox, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.OnNewModPolicyToggled, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)1, StringName.op_Implicit("pressed"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.OnPresetSelected, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)2, StringName.op_Implicit("index"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.CreatePresetFromCurrent, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.CreatePresetFromCurrent, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("name"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.RenameCurrentPreset, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.DeleteCurrentPreset, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.ExportToClipboard, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.ImportFromClipboard, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.ApplyOrder, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.SetStatus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("text"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.DebugLogLayoutCheck, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.DebugSelectPreset, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)2, StringName.op_Implicit("index"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.DebugCreatePreset, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("name"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.DebugApplyOrder, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			//IL_012e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0156: Unknown result type (might be due to invalid IL or missing references)
			//IL_015b: Unknown result type (might be due to invalid IL or missing references)
			//IL_019c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0223: Unknown result type (might be due to invalid IL or missing references)
			//IL_0248: Unknown result type (might be due to invalid IL or missing references)
			//IL_026d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0295: Unknown result type (might be due to invalid IL or missing references)
			//IL_029a: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0316: Unknown result type (might be due to invalid IL or missing references)
			//IL_033b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0371: Unknown result type (might be due to invalid IL or missing references)
			//IL_0376: Unknown result type (might be due to invalid IL or missing references)
			//IL_039a: Unknown result type (might be due to invalid IL or missing references)
			//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0409: Unknown result type (might be due to invalid IL or missing references)
			//IL_043c: Unknown result type (might be due to invalid IL or missing references)
			//IL_046f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0494: Unknown result type (might be due to invalid IL or missing references)
			//IL_04c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_04ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_0511: Unknown result type (might be due to invalid IL or missing references)
			//IL_0536: Unknown result type (might be due to invalid IL or missing references)
			//IL_055b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0580: Unknown result type (might be due to invalid IL or missing references)
			//IL_05b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_05d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_060b: Unknown result type (might be due to invalid IL or missing references)
			//IL_066d: Unknown result type (might be due to invalid IL or missing references)
			//IL_063e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0663: Unknown result type (might be due to invalid IL or missing references)
			if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				((Node)this)._Ready();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName._Input && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				((Node)this)._Input(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OpenPanel && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OpenPanel();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.BuildUiIfNeeded && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				BuildUiIfNeeded();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.LayoutForViewport && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				LayoutForViewport();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.ScheduleLayoutVerification && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				ScheduleLayoutVerification();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnProcessFrame && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnProcessFrame();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.CorrectLayoutOverflow && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				CorrectLayoutOverflow();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.FixedChromeMinHeight && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				float num = FixedChromeMinHeight();
				ret = VariantUtils.CreateFrom<float>(ref num);
				return true;
			}
			if ((ref method) == MethodName.BoundedMinHeight && ((NativeVariantPtrArgs)(ref args)).Count == 2)
			{
				float num2 = BoundedMinHeight(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]));
				ret = VariantUtils.CreateFrom<float>(ref num2);
				return true;
			}
			if ((ref method) == MethodName.LogLayoutCheck && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				LogLayoutCheck();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.ReadBoolEnv && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				bool flag = ReadBoolEnv(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<bool>(ref flag);
				return true;
			}
			if ((ref method) == MethodName.RefreshFromRuntime && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				RefreshFromRuntime();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.RefreshOverwriteWarning && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				RefreshOverwriteWarning();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.RefreshListOnly && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				RefreshListOnly();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.GetSelectedIndex && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				int selectedIndex = GetSelectedIndex();
				ret = VariantUtils.CreateFrom<int>(ref selectedIndex);
				return true;
			}
			if ((ref method) == MethodName.MoveSelected && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				MoveSelected(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.MoveToTop && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				MoveToTop();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.MoveToBottom && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				MoveToBottom();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.SortByAlphabet && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				SortByAlphabet();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.IsLibraryMod && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				bool flag2 = IsLibraryMod(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<bool>(ref flag2);
				return true;
			}
			if ((ref method) == MethodName.SmartSort && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				SmartSort();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.ToggleSelectedEnabled && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				ToggleSelectedEnabled();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.RefreshPresetDropdown && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				RefreshPresetDropdown();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.RefreshPolicyCheckbox && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				RefreshPolicyCheckbox();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnNewModPolicyToggled && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				OnNewModPolicyToggled(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnPresetSelected && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				OnPresetSelected(VariantUtils.ConvertTo<long>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.CreatePresetFromCurrent && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				CreatePresetFromCurrent();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.CreatePresetFromCurrent && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				CreatePresetFromCurrent(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.RenameCurrentPreset && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				RenameCurrentPreset();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.DeleteCurrentPreset && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				DeleteCurrentPreset();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.ExportToClipboard && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				ExportToClipboard();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.ImportFromClipboard && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				ImportFromClipboard();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.ApplyOrder && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				ApplyOrder();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.SetStatus && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				SetStatus(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.DebugLogLayoutCheck && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				DebugLogLayoutCheck();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.DebugSelectPreset && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				DebugSelectPreset(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.DebugCreatePreset && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				DebugCreatePreset(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.DebugApplyOrder && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				DebugApplyOrder();
				ret = default(godot_variant);
				return true;
			}
			return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			if ((ref method) == MethodName.BoundedMinHeight && ((NativeVariantPtrArgs)(ref args)).Count == 2)
			{
				float num = BoundedMinHeight(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]));
				ret = VariantUtils.CreateFrom<float>(ref num);
				return true;
			}
			if ((ref method) == MethodName.ReadBoolEnv && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				bool flag = ReadBoolEnv(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<bool>(ref flag);
				return true;
			}
			if ((ref method) == MethodName.IsLibraryMod && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				bool flag2 = IsLibraryMod(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<bool>(ref flag2);
				return true;
			}
			ret = default(godot_variant);
			return false;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool HasGodotClassMethod(in godot_string_name method)
		{
			if ((ref method) == MethodName._Ready)
			{
				return true;
			}
			if ((ref method) == MethodName._Input)
			{
				return true;
			}
			if ((ref method) == MethodName.OpenPanel)
			{
				return true;
			}
			if ((ref method) == MethodName.BuildUiIfNeeded)
			{
				return true;
			}
			if ((ref method) == MethodName.LayoutForViewport)
			{
				return true;
			}
			if ((ref method) == MethodName.ScheduleLayoutVerification)
			{
				return true;
			}
			if ((ref method) == MethodName.OnProcessFrame)
			{
				return true;
			}
			if ((ref method) == MethodName.CorrectLayoutOverflow)
			{
				return true;
			}
			if ((ref method) == MethodName.FixedChromeMinHeight)
			{
				return true;
			}
			if ((ref method) == MethodName.BoundedMinHeight)
			{
				return true;
			}
			if ((ref method) == MethodName.LogLayoutCheck)
			{
				return true;
			}
			if ((ref method) == MethodName.ReadBoolEnv)
			{
				return true;
			}
			if ((ref method) == MethodName.RefreshFromRuntime)
			{
				return true;
			}
			if ((ref method) == MethodName.RefreshOverwriteWarning)
			{
				return true;
			}
			if ((ref method) == MethodName.RefreshListOnly)
			{
				return true;
			}
			if ((ref method) == MethodName.GetSelectedIndex)
			{
				return true;
			}
			if ((ref method) == MethodName.MoveSelected)
			{
				return true;
			}
			if ((ref method) == MethodName.MoveToTop)
			{
				return true;
			}
			if ((ref method) == MethodName.MoveToBottom)
			{
				return true;
			}
			if ((ref method) == MethodName.SortByAlphabet)
			{
				return true;
			}
			if ((ref method) == MethodName.IsLibraryMod)
			{
				return true;
			}
			if ((ref method) == MethodName.SmartSort)
			{
				return true;
			}
			if ((ref method) == MethodName.ToggleSelectedEnabled)
			{
				return true;
			}
			if ((ref method) == MethodName.RefreshPresetDropdown)
			{
				return true;
			}
			if ((ref method) == MethodName.RefreshPolicyCheckbox)
			{
				return true;
			}
			if ((ref method) == MethodName.OnNewModPolicyToggled)
			{
				return true;
			}
			if ((ref method) == MethodName.OnPresetSelected)
			{
				return true;
			}
			if ((ref method) == MethodName.CreatePresetFromCurrent)
			{
				return true;
			}
			if ((ref method) == MethodName.RenameCurrentPreset)
			{
				return true;
			}
			if ((ref method) == MethodName.DeleteCurrentPreset)
			{
				return true;
			}
			if ((ref method) == MethodName.ExportToClipboard)
			{
				return true;
			}
			if ((ref method) == MethodName.ImportFromClipboard)
			{
				return true;
			}
			if ((ref method) == MethodName.ApplyOrder)
			{
				return true;
			}
			if ((ref method) == MethodName.SetStatus)
			{
				return true;
			}
			if ((ref method) == MethodName.DebugLogLayoutCheck)
			{
				return true;
			}
			if ((ref method) == MethodName.DebugSelectPreset)
			{
				return true;
			}
			if ((ref method) == MethodName.DebugCreatePreset)
			{
				return true;
			}
			if ((ref method) == MethodName.DebugApplyOrder)
			{
				return true;
			}
			return ((Control)this).HasGodotClassMethod(ref method);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
		{
			//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
			if ((ref name) == PropertyName._uiBuilt)
			{
				_uiBuilt = VariantUtils.ConvertTo<bool>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._list)
			{
				_list = VariantUtils.ConvertTo<ItemList>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._dialog)
			{
				_dialog = VariantUtils.ConvertTo<PanelContainer>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._margin)
			{
				_margin = VariantUtils.ConvertTo<MarginContainer>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._root)
			{
				_root = VariantUtils.ConvertTo<VBoxContainer>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._title)
			{
				_title = VariantUtils.ConvertTo<Label>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._subtitle)
			{
				_subtitle = VariantUtils.ConvertTo<Label>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._statusLabel)
			{
				_statusLabel = VariantUtils.ConvertTo<Label>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._warningLabel)
			{
				_warningLabel = VariantUtils.ConvertTo<Label>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._presetBar)
			{
				_presetBar = VariantUtils.ConvertTo<HFlowContainer>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._footer)
			{
				_footer = VariantUtils.ConvertTo<HBoxContainer>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._sideScroll)
			{
				_sideScroll = VariantUtils.ConvertTo<ScrollContainer>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._side)
			{
				_side = VariantUtils.ConvertTo<VBoxContainer>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._presetDropdown)
			{
				_presetDropdown = VariantUtils.ConvertTo<OptionButton>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._newModPolicyCheck)
			{
				_newModPolicyCheck = VariantUtils.ConvertTo<CheckBox>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._suppressPresetEvent)
			{
				_suppressPresetEvent = VariantUtils.ConvertTo<bool>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._layoutViewport)
			{
				_layoutViewport = VariantUtils.ConvertTo<Vector2>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._layoutCheckHooked)
			{
				_layoutCheckHooked = VariantUtils.ConvertTo<bool>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._layoutCheckFramesPending)
			{
				_layoutCheckFramesPending = VariantUtils.ConvertTo<int>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._layoutStepsRemaining)
			{
				_layoutStepsRemaining = VariantUtils.ConvertTo<int>(ref value);
				return true;
			}
			return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			//IL_0119: Unknown result type (might be due to invalid IL or missing references)
			//IL_0134: Unknown result type (might be due to invalid IL or missing references)
			//IL_0139: Unknown result type (might be due to invalid IL or missing references)
			//IL_0154: Unknown result type (might be due to invalid IL or missing references)
			//IL_0159: Unknown result type (might be due to invalid IL or missing references)
			//IL_0174: Unknown result type (might be due to invalid IL or missing references)
			//IL_0179: Unknown result type (might be due to invalid IL or missing references)
			//IL_0194: Unknown result type (might be due to invalid IL or missing references)
			//IL_0199: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0214: Unknown result type (might be due to invalid IL or missing references)
			//IL_0219: Unknown result type (might be due to invalid IL or missing references)
			//IL_0234: Unknown result type (might be due to invalid IL or missing references)
			//IL_0239: Unknown result type (might be due to invalid IL or missing references)
			//IL_0254: Unknown result type (might be due to invalid IL or missing references)
			//IL_0259: Unknown result type (might be due to invalid IL or missing references)
			//IL_0274: Unknown result type (might be due to invalid IL or missing references)
			//IL_0279: Unknown result type (might be due to invalid IL or missing references)
			if ((ref name) == PropertyName._uiBuilt)
			{
				value = VariantUtils.CreateFrom<bool>(ref _uiBuilt);
				return true;
			}
			if ((ref name) == PropertyName._list)
			{
				value = VariantUtils.CreateFrom<ItemList>(ref _list);
				return true;
			}
			if ((ref name) == PropertyName._dialog)
			{
				value = VariantUtils.CreateFrom<PanelContainer>(ref _dialog);
				return true;
			}
			if ((ref name) == PropertyName._margin)
			{
				value = VariantUtils.CreateFrom<MarginContainer>(ref _margin);
				return true;
			}
			if ((ref name) == PropertyName._root)
			{
				value = VariantUtils.CreateFrom<VBoxContainer>(ref _root);
				return true;
			}
			if ((ref name) == PropertyName._title)
			{
				value = VariantUtils.CreateFrom<Label>(ref _title);
				return true;
			}
			if ((ref name) == PropertyName._subtitle)
			{
				value = VariantUtils.CreateFrom<Label>(ref _subtitle);
				return true;
			}
			if ((ref name) == PropertyName._statusLabel)
			{
				value = VariantUtils.CreateFrom<Label>(ref _statusLabel);
				return true;
			}
			if ((ref name) == PropertyName._warningLabel)
			{
				value = VariantUtils.CreateFrom<Label>(ref _warningLabel);
				return true;
			}
			if ((ref name) == PropertyName._presetBar)
			{
				value = VariantUtils.CreateFrom<HFlowContainer>(ref _presetBar);
				return true;
			}
			if ((ref name) == PropertyName._footer)
			{
				value = VariantUtils.CreateFrom<HBoxContainer>(ref _footer);
				return true;
			}
			if ((ref name) == PropertyName._sideScroll)
			{
				value = VariantUtils.CreateFrom<ScrollContainer>(ref _sideScroll);
				return true;
			}
			if ((ref name) == PropertyName._side)
			{
				value = VariantUtils.CreateFrom<VBoxContainer>(ref _side);
				return true;
			}
			if ((ref name) == PropertyName._presetDropdown)
			{
				value = VariantUtils.CreateFrom<OptionButton>(ref _presetDropdown);
				return true;
			}
			if ((ref name) == PropertyName._newModPolicyCheck)
			{
				value = VariantUtils.CreateFrom<CheckBox>(ref _newModPolicyCheck);
				return true;
			}
			if ((ref name) == PropertyName._suppressPresetEvent)
			{
				value = VariantUtils.CreateFrom<bool>(ref _suppressPresetEvent);
				return true;
			}
			if ((ref name) == PropertyName._layoutViewport)
			{
				value = VariantUtils.CreateFrom<Vector2>(ref _layoutViewport);
				return true;
			}
			if ((ref name) == PropertyName._layoutCheckHooked)
			{
				value = VariantUtils.CreateFrom<bool>(ref _layoutCheckHooked);
				return true;
			}
			if ((ref name) == PropertyName._layoutCheckFramesPending)
			{
				value = VariantUtils.CreateFrom<int>(ref _layoutCheckFramesPending);
				return true;
			}
			if ((ref name) == PropertyName._layoutStepsRemaining)
			{
				value = VariantUtils.CreateFrom<int>(ref _layoutStepsRemaining);
				return true;
			}
			return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<PropertyInfo> GetGodotPropertyList()
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0102: Unknown result type (might be due to invalid IL or missing references)
			//IL_0123: Unknown result type (might be due to invalid IL or missing references)
			//IL_0144: Unknown result type (might be due to invalid IL or missing references)
			//IL_0165: Unknown result type (might be due to invalid IL or missing references)
			//IL_0186: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0209: Unknown result type (might be due to invalid IL or missing references)
			//IL_0229: Unknown result type (might be due to invalid IL or missing references)
			//IL_0249: Unknown result type (might be due to invalid IL or missing references)
			//IL_0269: Unknown result type (might be due to invalid IL or missing references)
			//IL_0289: Unknown result type (might be due to invalid IL or missing references)
			return new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, PropertyName._uiBuilt, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName._list, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName._dialog, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName._margin, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName._root, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName._title, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName._subtitle, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName._statusLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName._warningLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName._presetBar, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName._footer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName._sideScroll, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName._side, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName._presetDropdown, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName._newModPolicyCheck, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)1, PropertyName._suppressPresetEvent, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)5, PropertyName._layoutViewport, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)1, PropertyName._layoutCheckHooked, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)2, PropertyName._layoutCheckFramesPending, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)2, PropertyName._layoutStepsRemaining, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void SaveGodotObjectData(GodotSerializationInfo info)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_0105: Unknown result type (might be due to invalid IL or missing references)
			//IL_011b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0131: Unknown result type (might be due to invalid IL or missing references)
			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
			//IL_015d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			//IL_0189: Unknown result type (might be due to invalid IL or missing references)
			//IL_019f: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
			((GodotObject)this).SaveGodotObjectData(info);
			info.AddProperty(PropertyName._uiBuilt, Variant.From<bool>(ref _uiBuilt));
			info.AddProperty(PropertyName._list, Variant.From<ItemList>(ref _list));
			info.AddProperty(PropertyName._dialog, Variant.From<PanelContainer>(ref _dialog));
			info.AddProperty(PropertyName._margin, Variant.From<MarginContainer>(ref _margin));
			info.AddProperty(PropertyName._root, Variant.From<VBoxContainer>(ref _root));
			info.AddProperty(PropertyName._title, Variant.From<Label>(ref _title));
			info.AddProperty(PropertyName._subtitle, Variant.From<Label>(ref _subtitle));
			info.AddProperty(PropertyName._statusLabel, Variant.From<Label>(ref _statusLabel));
			info.AddProperty(PropertyName._warningLabel, Variant.From<Label>(ref _warningLabel));
			info.AddProperty(PropertyName._presetBar, Variant.From<HFlowContainer>(ref _presetBar));
			info.AddProperty(PropertyName._footer, Variant.From<HBoxContainer>(ref _footer));
			info.AddProperty(PropertyName._sideScroll, Variant.From<ScrollContainer>(ref _sideScroll));
			info.AddProperty(PropertyName._side, Variant.From<VBoxContainer>(ref _side));
			info.AddProperty(PropertyName._presetDropdown, Variant.From<OptionButton>(ref _presetDropdown));
			info.AddProperty(PropertyName._newModPolicyCheck, Variant.From<CheckBox>(ref _newModPolicyCheck));
			info.AddProperty(PropertyName._suppressPresetEvent, Variant.From<bool>(ref _suppressPresetEvent));
			info.AddProperty(PropertyName._layoutViewport, Variant.From<Vector2>(ref _layoutViewport));
			info.AddProperty(PropertyName._layoutCheckHooked, Variant.From<bool>(ref _layoutCheckHooked));
			info.AddProperty(PropertyName._layoutCheckFramesPending, Variant.From<int>(ref _layoutCheckFramesPending));
			info.AddProperty(PropertyName._layoutStepsRemaining, Variant.From<int>(ref _layoutStepsRemaining));
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void RestoreGodotObjectData(GodotSerializationInfo info)
		{
			//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01de: Unknown result type (might be due to invalid IL or missing references)
			((GodotObject)this).RestoreGodotObjectData(info);
			Variant val = default(Variant);
			if (info.TryGetProperty(PropertyName._uiBuilt, ref val))
			{
				_uiBuilt = ((Variant)(ref val)).As<bool>();
			}
			Variant val2 = default(Variant);
			if (info.TryGetProperty(PropertyName._list, ref val2))
			{
				_list = ((Variant)(ref val2)).As<ItemList>();
			}
			Variant val3 = default(Variant);
			if (info.TryGetProperty(PropertyName._dialog, ref val3))
			{
				_dialog = ((Variant)(ref val3)).As<PanelContainer>();
			}
			Variant val4 = default(Variant);
			if (info.TryGetProperty(PropertyName._margin, ref val4))
			{
				_margin = ((Variant)(ref val4)).As<MarginContainer>();
			}
			Variant val5 = default(Variant);
			if (info.TryGetProperty(PropertyName._root, ref val5))
			{
				_root = ((Variant)(ref val5)).As<VBoxContainer>();
			}
			Variant val6 = default(Variant);
			if (info.TryGetProperty(PropertyName._title, ref val6))
			{
				_title = ((Variant)(ref val6)).As<Label>();
			}
			Variant val7 = default(Variant);
			if (info.TryGetProperty(PropertyName._subtitle, ref val7))
			{
				_subtitle = ((Variant)(ref val7)).As<Label>();
			}
			Variant val8 = default(Variant);
			if (info.TryGetProperty(PropertyName._statusLabel, ref val8))
			{
				_statusLabel = ((Variant)(ref val8)).As<Label>();
			}
			Variant val9 = default(Variant);
			if (info.TryGetProperty(PropertyName._warningLabel, ref val9))
			{
				_warningLabel = ((Variant)(ref val9)).As<Label>();
			}
			Variant val10 = default(Variant);
			if (info.TryGetProperty(PropertyName._presetBar, ref val10))
			{
				_presetBar = ((Variant)(ref val10)).As<HFlowContainer>();
			}
			Variant val11 = default(Variant);
			if (info.TryGetProperty(PropertyName._footer, ref val11))
			{
				_footer = ((Variant)(ref val11)).As<HBoxContainer>();
			}
			Variant val12 = default(Variant);
			if (info.TryGetProperty(PropertyName._sideScroll, ref val12))
			{
				_sideScroll = ((Variant)(ref val12)).As<ScrollContainer>();
			}
			Variant val13 = default(Variant);
			if (info.TryGetProperty(PropertyName._side, ref val13))
			{
				_side = ((Variant)(ref val13)).As<VBoxContainer>();
			}
			Variant val14 = default(Variant);
			if (info.TryGetProperty(PropertyName._presetDropdown, ref val14))
			{
				_presetDropdown = ((Variant)(ref val14)).As<OptionButton>();
			}
			Variant val15 = default(Variant);
			if (info.TryGetProperty(PropertyName._newModPolicyCheck, ref val15))
			{
				_newModPolicyCheck = ((Variant)(ref val15)).As<CheckBox>();
			}
			Variant val16 = default(Variant);
			if (info.TryGetProperty(PropertyName._suppressPresetEvent, ref val16))
			{
				_suppressPresetEvent = ((Variant)(ref val16)).As<bool>();
			}
			Variant val17 = default(Variant);
			if (info.TryGetProperty(PropertyName._layoutViewport, ref val17))
			{
				_layoutViewport = ((Variant)(ref val17)).As<Vector2>();
			}
			Variant val18 = default(Variant);
			if (info.TryGetProperty(PropertyName._layoutCheckHooked, ref val18))
			{
				_layoutCheckHooked = ((Variant)(ref val18)).As<bool>();
			}
			Variant val19 = default(Variant);
			if (info.TryGetProperty(PropertyName._layoutCheckFramesPending, ref val19))
			{
				_layoutCheckFramesPending = ((Variant)(ref val19)).As<int>();
			}
			Variant val20 = default(Variant);
			if (info.TryGetProperty(PropertyName._layoutStepsRemaining, ref val20))
			{
				_layoutStepsRemaining = ((Variant)(ref val20)).As<int>();
			}
		}
	}
	internal sealed class LoadOrderEntry
	{
		public string Id { get; }

		public string Name { get; }

		public int Source { get; }

		public bool IsEnabled { get; set; }

		public bool IsNew { get; set; }

		public List<string> Dependencies { get; } = new List<string>();


		public string SourceText => Source switch
		{
			1 => I18n.T("source_mods"), 
			2 => I18n.T("source_workshop"), 
			_ => I18n.T("source_unknown"), 
		};

		public string Key => BuildKey(Id, Source);

		public LoadOrderEntry(string id, string name, int source, bool isEnabled)
		{
			Id = id;
			Name = (string.IsNullOrWhiteSpace(name) ? id : name);
			Source = source;
			IsEnabled = isEnabled;
		}

		public static string BuildKey(string id, int source)
		{
			return $"{id}::{source}";
		}
	}
	internal static class LoadOrderRuntime
	{
		private sealed class SaveContext
		{
			public object SaveManager { get; }

			public string SettingsPath { get; }

			public ulong? RuntimeUserId { get; }

			public ulong? PathUserId { get; }

			public SaveContext(object saveManager, string settingsPath, ulong? runtimeUserId, ulong? pathUserId)
			{
				SaveManager = saveManager;
				SettingsPath = settingsPath;
				RuntimeUserId = runtimeUserId;
				PathUserId = pathUserId;
			}
		}

		private sealed class ApplySnapshot
		{
			public string SavedAtUtc { get; set; } = string.Empty;


			public string RuntimeUserId { get; set; } = string.Empty;


			public string PathUserId { get; set; } = string.Empty;


			public string SettingsPath { get; set; } = string.Empty;


			public List<SnapshotEntry> Entries { get; set; } = new List<SnapshotEntry>();

		}

		private sealed class SnapshotEntry
		{
			public string Key { get; set; } = string.Empty;


			public bool IsEnabled { get; set; } = true;

		}

		private const string SettingsSaveFileName = "settings.save";

		private static readonly JsonSerializerOptions SnapshotJsonOptions = new JsonSerializerOptions
		{
			WriteIndented = true
		};

		private static readonly Regex UserIdPathRegex = new Regex("[\\/](?:steam|gog|epic|xbox|none)[\\/](?<id>\\d{1,20})(?:[\\/]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Regex InvalidSnapshotKeyCharsRegex = new Regex("[^0-9A-Za-z_-]", RegexOptions.Compiled);

		private static bool _diagnosticsLogged;

		public static void LogDiagnosticsOnce()
		{
			if (_diagnosticsLogged)
			{
				return;
			}
			_diagnosticsLogged = true;
			try
			{
				if (!TryResolveSaveContext(out SaveContext context, out string error))
				{
					DebugLog.Warn("Startup diagnostics skipped: " + error);
					return;
				}
				ReadSettings(out List<string> orderedKeys, out Dictionary<string, bool> enabledByKey);
				DebugLog.Info($"Startup diagnostics context: runtimeUserId={FormatUserId(context.RuntimeUserId)}, pathUserId={FormatUserId(context.PathUserId)}, settingsPath={context.SettingsPath}, currentTop={FormatTop(orderedKeys)}");
				if (!TryLoadApplySnapshot(context, out ApplySnapshot snapshot, out string error2))
				{
					DebugLog.Warn("Startup diagnostics failed to load snapshot: " + error2);
					return;
				}
				if (snapshot == null || snapshot.Entries.Count == 0)
				{
					DebugLog.Info("Startup diagnostics: no previous apply snapshot for current user.");
					return;
				}
				List<string> keys = (from e in snapshot.Entries
					where !string.IsNullOrWhiteSpace(e.Key)
					select e.Key).ToList();
				DebugLog.Info($"Startup diagnostics snapshot: savedAtUtc={snapshot.SavedAtUtc}, entries={snapshot.Entries.Count}, top={FormatTop(keys)}");
				if (IsSnapshotConsistent(snapshot, orderedKeys, enabledByKey, out string reason))
				{
					DebugLog.Info("Startup diagnostics: current settings match the last apply snapshot.");
					return;
				}
				DebugLog.Warn($"Startup diagnostics: detected drift from last apply snapshot. reason={reason}, snapshotTop={FormatTop(keys)}, currentTop={FormatTop(orderedKeys)}");
			}
			catch (Exception ex)
			{
				DebugLog.Error("LogDiagnosticsOnce failed.", ex);
			}
		}

		public static bool TryBuildOverwriteWarning(out string warningText)
		{
			warningText = string.Empty;
			try
			{
				if (!TryResolveSaveContext(out SaveContext context, out string error))
				{
					DebugLog.Warn("Overwrite check skipped: " + error);
					return false;
				}
				if (!TryLoadApplySnapshot(context, out ApplySnapshot snapshot, out string error2))
				{
					DebugLog.Warn("Overwrite check skipped: failed to load snapshot. " + error2);
					return false;
				}
				if (snapshot == null || snapshot.Entries.Count == 0)
				{
					return false;
				}
				ReadSettings(out List<string> orderedKeys, out Dictionary<string, bool> enabledByKey);
				if (IsSnapshotConsistent(snapshot, orderedKeys, enabledByKey, out string reason))
				{
					return false;
				}
				string text = FormatSnapshotTime(snapshot.SavedAtUtc);
				warningText = I18n.Tf("warning_overwritten", text);
				List<string> keys = (from e in snapshot.Entries
					where !string.IsNullOrWhiteSpace(e.Key)
					select e.Key).ToList();
				DebugLog.Warn($"Overwrite check detected drift. reason={reason}, snapshotTop={FormatTop(keys)}, currentTop={FormatTop(orderedKeys)}");
				return true;
			}
			catch (Exception ex)
			{
				DebugLog.Error("TryBuildOverwriteWarning failed.", ex);
				return false;
			}
		}

		public static bool TryGetOrderedEntries(out List<LoadOrderEntry> entries, out string error)
		{
			entries = new List<LoadOrderEntry>();
			error = string.Empty;
			try
			{
				Dictionary<string, LoadOrderEntry> dictionary = ReadLoadedMods();
				if (dictionary.Count == 0)
				{
					error = "No mods found in ModManager.Mods.";
					DebugLog.Warn(error);
					return false;
				}
				ReadSettings(out List<string> orderedKeys, out Dictionary<string, bool> enabledByKey);
				HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
				foreach (string item in orderedKeys)
				{
					if (dictionary.TryGetValue(item, out var value))
					{
						value.IsEnabled = enabledByKey.GetValueOrDefault(item, defaultValue: true);
						entries.Add(value);
						hashSet.Add(item);
					}
				}
				foreach (KeyValuePair<string, LoadOrderEntry> item2 in dictionary)
				{
					if (!hashSet.Contains(item2.Key))
					{
						item2.Value.IsEnabled = enabledByKey.GetValueOrDefault(item2.Key, defaultValue: true);
						entries.Add(item2.Value);
					}
				}
				DebugLog.Info($"Read order success. allMods={dictionary.Count}, settingsOrder={orderedKeys.Count}, merged={entries.Count}");
				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				DebugLog.Error("TryGetOrderedEntries failed.", ex);
				return false;
			}
		}

		public static bool TrySaveOrderedEntries(IReadOnlyList<LoadOrderEntry> orderedEntries, out string error)
		{
			error = string.Empty;
			try
			{
				if (!TryResolveSaveContext(out SaveContext context, out string error2))
				{
					error = error2;
					DebugLog.Error(error);
					return false;
				}
				if (!ValidateCurrentUserScope(context, out string error3))
				{
					error = error3;
					DebugLog.Error(error);
					return false;
				}
				object saveManager = context.SaveManager;
				object memberValue = GetMemberValue(saveManager, "SettingsSave");
				if (memberValue == null)
				{
					error = "SaveManager.SettingsSave is null.";
					DebugLog.Error(error);
					return false;
				}
				Type type = AccessTools.TypeByName("MegaCrit.Sts2.Core.Modding.ModSettings");
				Type type2 = AccessTools.TypeByName("MegaCrit.Sts2.Core.Modding.SettingsSaveMod");
				Type type3 = AccessTools.TypeByName("MegaCrit.Sts2.Core.Modding.ModSource");
				if (type == null || type2 == null || type3 == null)
				{
					error = "Mod settings runtime types not found.";
					DebugLog.Error(error);
					return false;
				}
				object obj = GetMemberValue(memberValue, "ModSettings");
				if (obj == null)
				{
					obj = Activator.CreateInstance(type);
					SetMemberValue(memberValue, "ModSettings", obj);
				}
				if (obj == null)
				{
					error = "Failed to create ModSettings object.";
					DebugLog.Error(error);
					return false;
				}
				IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(type2));
				if (list == null)
				{
					error = "Failed to create mod list.";
					DebugLog.Error(error);
					return false;
				}
				DebugLog.Info($"Saving manual order with {orderedEntries.Count} entries. runtimeUserId={FormatUserId(context.RuntimeUserId)}, pathUserId={FormatUserId(context.PathUserId)}, settingsPath={context.SettingsPath}, top={FormatTop(orderedEntries)}");
				foreach (LoadOrderEntry orderedEntry in orderedEntries)
				{
					object obj2 = Activator.CreateInstance(type2);
					if (obj2 != null)
					{
						SetMemberValue(obj2, "Id", orderedEntry.Id);
						SetMemberValue(obj2, "Source", Enum.ToObject(type3, orderedEntry.Source));
						SetMemberValue(obj2, "IsEnabled", orderedEntry.IsEnabled);
						list.Add(obj2);
					}
				}
				SetMemberValue(obj, "ModList", list);
				MethodInfo methodInfo = AccessTools.Method(ResolveCoreType("MegaCrit.Sts2.Core.Saves.SaveManager"), "SaveSettings", Type.EmptyTypes, (Type[])null);
				if (methodInfo == null)
				{
					error = "SaveManager.SaveSettings() not found.";
					DebugLog.Error(error);
					return false;
				}
				methodInfo.Invoke(saveManager, null);
				List<string> list2 = orderedEntries.Select((LoadOrderEntry e) => e.Key).ToList();
				ReadSettings(out List<string> orderedKeys, out Dictionary<string, bool> _);
				if (!IsOrderMonotonic(list2, orderedKeys))
				{
					error = "Order verification failed after save. Check log for details.";
					DebugLog.Warn("Order verification failed. expectedTop=" + FormatTop(list2) + ", persistedTop=" + FormatTop(orderedKeys));
					return false;
				}
				if (!TryWriteApplySnapshot(context, orderedEntries, out string error4))
				{
					DebugLog.Warn("SaveSettings succeeded but snapshot write failed: " + error4);
				}
				DebugLog.Info("SaveSettings succeeded. Persisted top=" + FormatTop(orderedKeys));
				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				DebugLog.Error("TrySaveOrderedEntries failed.", ex);
				return false;
			}
		}

		private static Dictionary<string, LoadOrderEntry> ReadLoadedMods()
		{
			Dictionary<string, LoadOrderEntry> dictionary = new Dictionary<string, LoadOrderEntry>(StringComparer.Ordinal);
			Type type = ResolveCoreType("MegaCrit.Sts2.Core.Modding.ModManager");
			if (type == null)
			{
				DebugLog.Warn("ReadLoadedMods: ModManager type not found.");
				return dictionary;
			}
			if (TryCollectModsFromMember(type, "Mods", dictionary))
			{
				DebugLog.Info($"ReadLoadedMods: collected {dictionary.Count} entries from ModManager.Mods.");
				return dictionary;
			}
			if (TryCollectModsFromMember(type, "_mods", dictionary))
			{
				DebugLog.Warn($"ReadLoadedMods: Mods empty, fallback to ModManager._mods ({dictionary.Count}).");
				return dictionary;
			}
			if (TryCollectModsFromSettings(dictionary))
			{
				DebugLog.Warn($"ReadLoadedMods: ModManager lists empty, fallback to settings.mod_list ({dictionary.Count}).");
			}
			return dictionary;
		}

		private static void ReadSettings(out List<string> orderedKeys, out Dictionary<string, bool> enabledByKey)
		{
			orderedKeys = new List<string>();
			enabledByKey = new Dictionary<string, bool>(StringComparer.Ordinal);
			Type type = ResolveCoreType("MegaCrit.Sts2.Core.Saves.SaveManager");
			if (type == null || !(GetMemberValue(GetMemberValue(GetMemberValue(AccessTools.Property(type, "Instance")?.GetValue(null), "SettingsSave"), "ModSettings"), "ModList") is IEnumerable enumerable))
			{
				return;
			}
			foreach (object item in enumerable)
			{
				string text = AsString(GetMemberValue(item, "Id"));
				if (!string.IsNullOrWhiteSpace(text))
				{
					int source = ToInt(GetMemberValue(item, "Source"));
					bool value = ToBool(GetMemberValue(item, "IsEnabled"), fallback: true);
					string text2 = LoadOrderEntry.BuildKey(text, source);
					orderedKeys.Add(text2);
					enabledByKey[text2] = value;
				}
			}
		}

		private static bool TryResolveSaveContext(out SaveContext context, out string error)
		{
			context = null;
			error = string.Empty;
			Type type = ResolveCoreType("MegaCrit.Sts2.Core.Saves.SaveManager");
			if (type == null)
			{
				error = "SaveManager type not found.";
				return false;
			}
			object obj = AccessTools.Property(type, "Instance")?.GetValue(null);
			if (obj == null)
			{
				error = "SaveManager.Instance is null.";
				return false;
			}
			object memberValue = GetMemberValue(obj, "_saveStore");
			if (memberValue == null)
			{
				error = "SaveManager._saveStore is null.";
				return false;
			}
			MethodInfo methodInfo = AccessTools.Method(memberValue.GetType(), "GetFullPath", new Type[1] { typeof(string) }, (Type[])null);
			if (methodInfo == null)
			{
				error = "GetFullPath(string) not found on save store " + memberValue.GetType().FullName + ".";
				return false;
			}
			string text = AsString(methodInfo.Invoke(memberValue, new object[1] { "settings.save" }));
			if (string.IsNullOrWhiteSpace(text))
			{
				error = "Could not resolve full settings.save path from save store.";
				return false;
			}
			ulong? runtimeUserId = TryResolveRuntimeUserId();
			ulong? pathUserId = TryExtractPathUserId(text);
			context = new SaveContext(obj, text, runtimeUserId, pathUserId);
			return true;
		}

		private static bool ValidateCurrentUserScope(SaveContext context, out string error)
		{
			error = string.Empty;
			if (!context.RuntimeUserId.HasValue)
			{
				error = "Current runtime user id is unavailable. Refusing save to avoid cross-account overwrite. settingsPath=" + context.SettingsPath;
				return false;
			}
			if (!context.PathUserId.HasValue)
			{
				if (ContainsUserIdSegment(context.SettingsPath, context.RuntimeUserId.Value))
				{
					return true;
				}
				error = $"Could not verify settings path belongs to runtime user {context.RuntimeUserId}. path={context.SettingsPath}";
				return false;
			}
			if (context.RuntimeUserId.Value != context.PathUserId.Value)
			{
				error = $"Refusing save: runtime user id {context.RuntimeUserId} does not match settings path user id {context.PathUserId}. path={context.SettingsPath}";
				return false;
			}
			return true;
		}

		private static ulong? TryResolveRuntimeUserId()
		{
			try
			{
				Type type = AccessTools.TypeByName("MegaCrit.Sts2.Core.Platform.PlatformUtil");
				if (type == null)
				{
					return null;
				}
				object obj = AccessTools.Property(type, "PrimaryPlatform")?.GetValue(null);
				if (obj == null)
				{
					return null;
				}
				MethodInfo methodInfo = AccessTools.Method(type, "GetLocalPlayerId", (Type[])null, (Type[])null);
				if (methodInfo == null)
				{
					return null;
				}
				return ToUlong(methodInfo.Invoke(null, new object[1] { obj }));
			}
			catch
			{
				return null;
			}
		}

		private static Type? ResolveCoreType(string fullTypeName)
		{
			Type type = ResolveCoreAssembly()?.GetType(fullTypeName, throwOnError: false, ignoreCase: false);
			if (type != null)
			{
				return type;
			}
			Type type2 = AccessTools.TypeByName(fullTypeName);
			if (type2 != null)
			{
				DebugLog.Warn("ResolveCoreType fallback used for " + fullTypeName + ": " + type2.Assembly.FullName);
			}
			return type2;
		}

		private static Assembly? ResolveCoreAssembly()
		{
			return AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.SaveManager")?.Assembly;
		}

		private static bool TryCollectModsFromMember(Type modManagerType, string memberName, Dictionary<string, LoadOrderEntry> dict)
		{
			object obj = null;
			PropertyInfo propertyInfo = AccessTools.Property(modManagerType, memberName);
			if (propertyInfo != null)
			{
				obj = propertyInfo.GetValue(null);
			}
			else
			{
				FieldInfo fieldInfo = AccessTools.Field(modManagerType, memberName);
				if (fieldInfo != null)
				{
					obj = fieldInfo.GetValue(null);
				}
			}
			if (!(obj is IEnumerable modList))
			{
				return false;
			}
			return TryCollectMods(modList, dict);
		}

		private static bool TryCollectMods(IEnumerable modList, Dictionary<string, LoadOrderEntry> dict)
		{
			int num = 0;
			foreach (object mod in modList)
			{
				object memberValue = GetMemberValue(mod, "manifest");
				string text = AsString(GetMemberValue(memberValue, "id"));
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				string text2 = AsString(GetMemberValue(memberValue, "name"));
				int source = ToInt(GetMemberValue(mod, "modSource"));
				string key = LoadOrderEntry.BuildKey(text, source);
				bool flag = !dict.ContainsKey(key);
				LoadOrderEntry loadOrderEntry = new LoadOrderEntry(text, text2 ?? text, source, isEnabled: true);
				if (GetMemberValue(memberValue, "dependencies") is IEnumerable enumerable)
				{
					foreach (object item in enumerable)
					{
						string text3 = AsString(GetMemberValue(item, "id"));
						if (!string.IsNullOrWhiteSpace(text3))
						{
							loadOrderEntry.Dependencies.Add(text3);
						}
					}
				}
				dict[key] = loadOrderEntry;
				if (flag)
				{
					num++;
				}
			}
			return num > 0;
		}

		private static bool TryCollectModsFromSettings(Dictionary<string, LoadOrderEntry> dict)
		{
			ReadSettings(out List<string> orderedKeys, out Dictionary<string, bool> enabledByKey);
			if (orderedKeys.Count == 0)
			{
				return false;
			}
			int num = 0;
			foreach (string item in orderedKeys)
			{
				if (TryParseEntryKey(item, out string id, out int source) && !dict.ContainsKey(item))
				{
					dict[item] = new LoadOrderEntry(id, id, source, enabledByKey.GetValueOrDefault(item, defaultValue: true));
					num++;
				}
			}
			return num > 0;
		}

		private static bool TryParseEntryKey(string key, out string id, out int source)
		{
			id = string.Empty;
			source = 0;
			if (string.IsNullOrWhiteSpace(key))
			{
				return false;
			}
			int num = key.LastIndexOf("::", StringComparison.Ordinal);
			if (num <= 0 || num >= key.Length - 2)
			{
				return false;
			}
			string text = key.Substring(0, num);
			int num2 = num + 2;
			string s = key.Substring(num2, key.Length - num2);
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				return false;
			}
			id = text;
			source = result;
			return true;
		}

		private static ulong? TryExtractPathUserId(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return null;
			}
			Match match = UserIdPathRegex.Match(path);
			if (!match.Success)
			{
				return null;
			}
			if (!ulong.TryParse(match.Groups["id"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
			{
				return null;
			}
			return result;
		}

		private static bool ContainsUserIdSegment(string path, ulong userId)
		{
			string value = $"/{userId}/";
			string value2 = $"\\{userId}\\";
			if (!path.Contains(value, StringComparison.OrdinalIgnoreCase))
			{
				return path.Contains(value2, StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}

		private static string BuildSnapshotPath(SaveContext context)
		{
			string directoryName = Path.GetDirectoryName(DebugLog.LogPath);
			string path = Path.Combine(string.IsNullOrWhiteSpace(directoryName) ? "." : directoryName, "state");
			string input = context.RuntimeUserId?.ToString(CultureInfo.InvariantCulture) ?? context.PathUserId?.ToString(CultureInfo.InvariantCulture) ?? "unknown";
			string text = InvalidSnapshotKeyCharsRegex.Replace(input, "_");
			return Path.Combine(path, "last_apply_" + text + ".json");
		}

		private static bool TryWriteApplySnapshot(SaveContext context, IReadOnlyList<LoadOrderEntry> orderedEntries, out string error)
		{
			error = string.Empty;
			try
			{
				ApplySnapshot applySnapshot = new ApplySnapshot
				{
					SavedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
					RuntimeUserId = FormatUserId(context.RuntimeUserId),
					PathUserId = FormatUserId(context.PathUserId),
					SettingsPath = context.SettingsPath,
					Entries = orderedEntries.Select((LoadOrderEntry e) => new SnapshotEntry
					{
						Key = e.Key,
						IsEnabled = e.IsEnabled
					}).ToList()
				};
				string text = BuildSnapshotPath(context);
				string directoryName = Path.GetDirectoryName(text);
				if (!string.IsNullOrWhiteSpace(directoryName))
				{
					Directory.CreateDirectory(directoryName);
				}
				string contents = JsonSerializer.Serialize(applySnapshot, SnapshotJsonOptions);
				File.WriteAllText(text, contents);
				List<string> keys = (from e in applySnapshot.Entries
					where !string.IsNullOrWhiteSpace(e.Key)
					select e.Key).ToList();
				DebugLog.Info($"Apply snapshot saved. path={text}, entries={applySnapshot.Entries.Count}, top={FormatTop(keys)}");
				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				DebugLog.Error("TryWriteApplySnapshot failed.", ex);
				return false;
			}
		}

		private static bool TryLoadApplySnapshot(SaveContext context, out ApplySnapshot? snapshot, out string error)
		{
			snapshot = null;
			error = string.Empty;
			try
			{
				string text = BuildSnapshotPath(context);
				if (!File.Exists(text))
				{
					return true;
				}
				ApplySnapshot applySnapshot = JsonSerializer.Deserialize<ApplySnapshot>(File.ReadAllText(text));
				if (applySnapshot == null)
				{
					error = "Snapshot json parse returned null. path=" + text;
					return false;
				}
				ApplySnapshot applySnapshot2 = applySnapshot;
				if (applySnapshot2.Entries == null)
				{
					List<SnapshotEntry> list2 = (applySnapshot2.Entries = new List<SnapshotEntry>());
				}
				snapshot = applySnapshot;
				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				DebugLog.Error("TryLoadApplySnapshot failed.", ex);
				return false;
			}
		}

		private static bool IsSnapshotConsistent(ApplySnapshot snapshot, IReadOnlyList<string> currentOrder, IReadOnlyDictionary<string, bool> currentEnabledByKey, out string reason)
		{
			List<SnapshotEntry> list = snapshot.Entries.Where((SnapshotEntry e) => !string.IsNullOrWhiteSpace(e.Key)).ToList();
			List<string> list2 = list.Select((SnapshotEntry e) => e.Key).ToList();
			if (list2.Count == 0)
			{
				reason = "snapshot_empty";
				return true;
			}
			if (!IsOrderMonotonic(list2, currentOrder))
			{
				reason = "order_changed";
				return false;
			}
			foreach (SnapshotEntry item in list)
			{
				if (!currentEnabledByKey.TryGetValue(item.Key, out var value))
				{
					reason = "missing:" + item.Key;
					return false;
				}
				if (value != item.IsEnabled)
				{
					reason = "enabled_changed:" + item.Key;
					return false;
				}
			}
			reason = "matched";
			return true;
		}

		private static string FormatSnapshotTime(string savedAtUtc)
		{
			if (DateTime.TryParse(savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
			{
				return result.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
			}
			return savedAtUtc;
		}

		private static string FormatUserId(ulong? userId)
		{
			return userId?.ToString(CultureInfo.InvariantCulture) ?? "unknown";
		}

		private static object? GetMemberValue(object? obj, string name)
		{
			if (obj == null)
			{
				return null;
			}
			Type type = obj.GetType();
			PropertyInfo propertyInfo = AccessTools.Property(type, name);
			if (propertyInfo != null)
			{
				return propertyInfo.GetValue(obj);
			}
			return AccessTools.Field(type, name)?.GetValue(obj);
		}

		private static void SetMemberValue(object? obj, string name, object? value)
		{
			if (obj != null)
			{
				Type type = obj.GetType();
				PropertyInfo propertyInfo = AccessTools.Property(type, name);
				if (propertyInfo != null)
				{
					propertyInfo.SetValue(obj, value);
				}
				else
				{
					AccessTools.Field(type, name)?.SetValue(obj, value);
				}
			}
		}

		private static string? AsString(object? obj)
		{
			if (obj != null)
			{
				if (obj is string result)
				{
					return result;
				}
				return obj.ToString();
			}
			return null;
		}

		private static ulong? ToUlong(object? obj)
		{
			if (obj != null)
			{
				if (!(obj is ulong value))
				{
					if (!(obj is long num))
					{
						if (!(obj is int num2))
						{
							if (obj is Enum value2)
							{
								return (ulong)Convert.ToInt64(value2, CultureInfo.InvariantCulture);
							}
						}
						else
						{
							int num3 = num2;
							if (num3 >= 0)
							{
								return (ulong)num3;
							}
						}
					}
					else
					{
						long num4 = num;
						if (num4 >= 0)
						{
							return (ulong)num4;
						}
					}
					if (ulong.TryParse(obj.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var result))
					{
						return result;
					}
					return null;
				}
				return value;
			}
			return null;
		}

		private static int ToInt(object? obj)
		{
			if (obj != null)
			{
				if (!(obj is int result))
				{
					if (!(obj is long num))
					{
						if (obj is Enum value)
						{
							return Convert.ToInt32(value, CultureInfo.InvariantCulture);
						}
						if (int.TryParse(obj.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2))
						{
							return result2;
						}
						return 0;
					}
					return (int)num;
				}
				return result;
			}
			return 0;
		}

		private static bool ToBool(object? obj, bool fallback)
		{
			if (obj != null)
			{
				if (obj is bool result)
				{
					return result;
				}
				if (bool.TryParse(obj.ToString(), out var result2))
				{
					return result2;
				}
				return fallback;
			}
			return fallback;
		}

		private static bool IsOrderMonotonic(IReadOnlyList<string> expectedOrder, IReadOnlyList<string> persistedOrder)
		{
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
			for (int i = 0; i < persistedOrder.Count; i++)
			{
				dictionary[persistedOrder[i]] = i;
			}
			int num = -1;
			foreach (string item in expectedOrder)
			{
				if (dictionary.TryGetValue(item, out var value))
				{
					if (value < num)
					{
						return false;
					}
					num = value;
				}
			}
			return true;
		}

		private static string FormatTop(IReadOnlyList<LoadOrderEntry> entries)
		{
			int num = Math.Min(entries.Count, 8);
			if (num == 0)
			{
				return "<empty>";
			}
			IEnumerable<string> values = from e in entries.Take(num)
				select $"{e.Id}({e.Source})";
			return string.Join(" > ", values);
		}

		private static string FormatTop(IReadOnlyList<string> keys)
		{
			int num = Math.Min(keys.Count, 8);
			if (num == 0)
			{
				return "<empty>";
			}
			return string.Join(" > ", keys.Take(num));
		}
	}
	[HarmonyPatch]
	internal static class ModdingScreenReadyPatch
	{
		private const string ScreenTypeName = "MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen.NModdingScreen";

		private static MethodBase? TargetMethod()
		{
			return (AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen.NModdingScreen") ?? AccessTools.TypeByName("NModdingScreen"))?.GetMethod("_Ready", BindingFlags.Instance | BindingFlags.Public);
		}

		private static void Postfix(object __instance)
		{
			try
			{
				LoadOrderUiInjector.TryInject(__instance);
			}
			catch (Exception ex)
			{
				DebugLog.Error("UI inject failed.", ex);
			}
		}
	}
	internal static class LoadOrderUiInjector
	{
		private const string OpenButtonName = "LoadOrderManager_OpenButton";

		private const string PanelName = "LoadOrderManager_Panel";

		public static void TryInject(object screenObject)
		{
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Expected O, but got Unknown
			Control screen = (Control)((screenObject is Control) ? screenObject : null);
			if (screen != null && GodotObject.IsInstanceValid((GodotObject)(object)screen) && ((Node)screen).FindChild("LoadOrderManager_OpenButton", true, false) == null)
			{
				Button val = new Button
				{
					Name = StringName.op_Implicit("LoadOrderManager_OpenButton"),
					Text = I18n.T("open_button"),
					TooltipText = I18n.T("open_button_tooltip"),
					FocusMode = (FocusModeEnum)2,
					MouseFilter = (MouseFilterEnum)0
				};
				((Control)val).AnchorLeft = 1f;
				((Control)val).AnchorRight = 1f;
				((Control)val).AnchorTop = 0f;
				((Control)val).AnchorBottom = 0f;
				((Control)val).OffsetLeft = -220f;
				((Control)val).OffsetRight = -20f;
				((Control)val).OffsetTop = 60f;
				((Control)val).OffsetBottom = 98f;
				((CanvasItem)val).ZIndex = 40;
				((BaseButton)val).Pressed += delegate
				{
					DebugLog.Info("Load-order button clicked.");
					EnsurePanel(screen).OpenPanel();
				};
				((Node)screen).AddChild((Node)(object)val, false, (InternalMode)0);
				DebugLog.Info("Injected load-order button into " + ((object)screen).GetType().FullName + ".");
			}
		}

		private static LoadOrderPanel EnsurePanel(Control screen)
		{
			if (((Node)screen).FindChild("LoadOrderManager_Panel", true, false) is LoadOrderPanel result)
			{
				return result;
			}
			LoadOrderPanel loadOrderPanel = new LoadOrderPanel();
			((Node)loadOrderPanel).Name = StringName.op_Implicit("LoadOrderManager_Panel");
			LoadOrderPanel loadOrderPanel2 = loadOrderPanel;
			((Node)screen).AddChild((Node)(object)loadOrderPanel2, false, (InternalMode)0);
			return loadOrderPanel2;
		}
	}
	internal sealed class ModPreset
	{
		public string Name { get; set; } = "Default";


		public HashSet<string> DisabledMods { get; set; } = new HashSet<string>();


		public HashSet<string> KnownMods { get; set; } = new HashSet<string>();


		public bool AutoEnableNewMods { get; set; } = true;

	}
	internal sealed class PresetSaveData
	{
		public int SchemaVersion { get; set; } = 2;


		public List<ModPreset> Profiles { get; set; } = new List<ModPreset>();


		public int CurrentProfileIndex { get; set; }
	}
	internal static class ModPresetStore
	{
		public const int CurrentSchemaVersion = 2;

		private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
		{
			WriteIndented = true
		};

		private static PresetSaveData? _cache;

		private static bool _freshStore;

		private static string StorePath
		{
			get
			{
				string text = Path.Combine(OS.GetUserDataDir(), "LoadOrderManager");
				Directory.CreateDirectory(text);
				return Path.Combine(text, "presets.json");
			}
		}

		public static ModPreset CurrentProfile
		{
			get
			{
				PresetSaveData presetSaveData = Load();
				NormalizeIndex(presetSaveData);
				return presetSaveData.Profiles[presetSaveData.CurrentProfileIndex];
			}
		}

		public static PresetSaveData Load()
		{
			if (_cache != null)
			{
				return _cache;
			}
			try
			{
				string storePath = StorePath;
				if (File.Exists(storePath))
				{
					_cache = JsonSerializer.Deserialize<PresetSaveData>(File.ReadAllText(storePath)) ?? CreateDefault();
				}
				else
				{
					_cache = CreateDefault();
					_freshStore = true;
				}
			}
			catch (Exception ex)
			{
				DebugLog.Error("ModPresetStore.Load failed.", ex);
				_cache = CreateDefault();
				_freshStore = true;
			}
			NormalizeIndex(_cache);
			return _cache;
		}

		public static bool Save()
		{
			if (_cache == null)
			{
				return false;
			}
			try
			{
				string storePath = StorePath;
				string contents = JsonSerializer.Serialize(_cache, JsonOpts);
				File.WriteAllText(storePath, contents);
				return true;
			}
			catch (Exception ex)
			{
				DebugLog.Error("ModPresetStore.Save failed.", ex);
				return false;
			}
		}

		public static void MigrateIfNeeded(IReadOnlyList<LoadOrderEntry> entries)
		{
			PresetSaveData presetSaveData = Load();
			bool flag = presetSaveData.SchemaVersion < 2;
			if (!flag && !_freshStore)
			{
				return;
			}
			List<string> list = (from e in entries
				where !string.IsNullOrWhiteSpace(e.Id)
				select e.Id).ToList();
			for (int i = 0; i < presetSaveData.Profiles.Count; i++)
			{
				ModPreset modPreset = presetSaveData.Profiles[i];
				foreach (string item in list)
				{
					modPreset.KnownMods.Add(item);
				}
				if (flag)
				{
					modPreset.AutoEnableNewMods = i == 0;
				}
			}
			if (_freshStore)
			{
				ModPreset modPreset2 = presetSaveData.Profiles[presetSaveData.CurrentProfileIndex];
				foreach (LoadOrderEntry entry in entries)
				{
					if (!string.IsNullOrWhiteSpace(entry.Id) && !entry.IsEnabled)
					{
						modPreset2.DisabledMods.Add(entry.Id);
					}
				}
			}
			presetSaveData.SchemaVersion = 2;
			_freshStore = false;
			Save();
			DebugLog.Info($"Presets initialised (legacy={flag}, schema={2}). profiles={presetSaveData.Profiles.Count}, installed={list.Count}");
		}

		public static void CommitCurrentProfile(IReadOnlyList<LoadOrderEntry> entries)
		{
			ModPreset currentProfile = CurrentProfile;
			currentProfile.DisabledMods.Clear();
			currentProfile.KnownMods.Clear();
			foreach (LoadOrderEntry entry in entries)
			{
				if (!string.IsNullOrWhiteSpace(entry.Id))
				{
					currentProfile.KnownMods.Add(entry.Id);
					if (!entry.IsEnabled)
					{
						currentProfile.DisabledMods.Add(entry.Id);
					}
				}
			}
			Save();
		}

		public static int ApplyPreset(ModPreset preset, List<LoadOrderEntry> entries)
		{
			int num = 0;
			foreach (LoadOrderEntry entry in entries)
			{
				if (string.IsNullOrWhiteSpace(entry.Id) || !preset.KnownMods.Contains(entry.Id))
				{
					entry.IsEnabled = preset.AutoEnableNewMods;
					entry.IsNew = true;
					num++;
				}
				else
				{
					entry.IsEnabled = !preset.DisabledMods.Contains(entry.Id);
					entry.IsNew = false;
				}
			}
			return num;
		}

		public static int ApplyNewModPolicy(ModPreset preset, List<LoadOrderEntry> entries)
		{
			int num = 0;
			foreach (LoadOrderEntry entry in entries)
			{
				if (string.IsNullOrWhiteSpace(entry.Id) || preset.KnownMods.Contains(entry.Id))
				{
					entry.IsNew = false;
					continue;
				}
				entry.IsEnabled = preset.AutoEnableNewMods;
				entry.IsNew = true;
				num++;
			}
			return num;
		}

		public static bool DecideEnabled(bool isKnown, bool isDisabled, bool autoEnableNewMods)
		{
			if (!isKnown)
			{
				return autoEnableNewMods;
			}
			return !isDisabled;
		}

		public static ModPreset CreatePreset(string name, IReadOnlyList<LoadOrderEntry> entries)
		{
			PresetSaveData presetSaveData = Load();
			ModPreset modPreset = new ModPreset
			{
				Name = name,
				KnownMods = (from e in entries
					where !string.IsNullOrWhiteSpace(e.Id)
					select e.Id).ToHashSet(),
				DisabledMods = (from e in entries
					where !e.IsEnabled && !string.IsNullOrWhiteSpace(e.Id)
					select e.Id).ToHashSet(),
				AutoEnableNewMods = false
			};
			presetSaveData.Profiles.Add(modPreset);
			presetSaveData.CurrentProfileIndex = presetSaveData.Profiles.Count - 1;
			Save();
			return modPreset;
		}

		public static bool SetAutoEnableNewMods(bool value)
		{
			PresetSaveData presetSaveData = Load();
			presetSaveData.Profiles[presetSaveData.CurrentProfileIndex].AutoEnableNewMods = value;
			return Save();
		}

		public static bool DeleteCurrentPreset()
		{
			PresetSaveData presetSaveData = Load();
			if (presetSaveData.Profiles.Count <= 1)
			{
				return false;
			}
			presetSaveData.Profiles.RemoveAt(presetSaveData.CurrentProfileIndex);
			if (presetSaveData.CurrentProfileIndex >= presetSaveData.Profiles.Count)
			{
				presetSaveData.CurrentProfileIndex = presetSaveData.Profiles.Count - 1;
			}
			Save();
			return true;
		}

		public static bool RenameCurrentPreset(string newName)
		{
			string text = newName.Trim();
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			PresetSaveData presetSaveData = Load();
			presetSaveData.Profiles[presetSaveData.CurrentProfileIndex].Name = text;
			return Save();
		}

		public static void SelectProfile(int index, IReadOnlyList<LoadOrderEntry> entries)
		{
			PresetSaveData presetSaveData = Load();
			CommitCurrentProfile(entries);
			presetSaveData.CurrentProfileIndex = index;
			NormalizeIndex(presetSaveData);
			Save();
		}

		private static PresetSaveData CreateDefault()
		{
			return new PresetSaveData
			{
				SchemaVersion = 2,
				Profiles = new List<ModPreset>
				{
					new ModPreset
					{
						Name = "Default",
						AutoEnableNewMods = true
					}
				},
				CurrentProfileIndex = 0
			};
		}

		private static void NormalizeIndex(PresetSaveData? data)
		{
			if (data != null)
			{
				if (data.Profiles.Count == 0)
				{
					data.Profiles.Add(new ModPreset
					{
						Name = "Default",
						AutoEnableNewMods = true
					});
				}
				if (data.CurrentProfileIndex < 0)
				{
					data.CurrentProfileIndex = 0;
				}
				if (data.CurrentProfileIndex >= data.Profiles.Count)
				{
					data.CurrentProfileIndex = data.Profiles.Count - 1;
				}
			}
		}
	}
	internal static class SelfTest
	{
		private sealed record SavedEntry(string Id, string Source, bool IsEnabled);

		private const string ModeEnv = "LOADORDER_SELFTEST";

		private const string WriteEnv = "LOADORDER_SELFTEST_WRITE";

		private const string ApplyEnv = "LOADORDER_SELFTEST_APPLY";

		private const string ProbeEnv = "LOADORDER_SELFTEST_PROBE";

		private const string AutoPresetName = "SelfTest";

		private const double TickSeconds = 0.25;

		private static string _mode = string.Empty;

		private static bool _write;

		private static string _applyPreset = string.Empty;

		private static string _probeId = "LomProbe";

		private static bool _scheduled;

		private static bool _ticking;

		private static bool _finished;

		private static LoadOrderPanel? _panel;

		private static int _step;

		private static int _waitTicks;

		private static int _readyTicks;

		private static int _ticks;

		public static void Schedule()
		{
			if (_scheduled)
			{
				return;
			}
			string text = ReadEnv("LOADORDER_SELFTEST");
			if (!string.IsNullOrWhiteSpace(text))
			{
				_mode = text.Trim().ToLowerInvariant();
				_write = ReadEnv("LOADORDER_SELFTEST_WRITE") == "1";
				_applyPreset = ReadEnv("LOADORDER_SELFTEST_APPLY")?.Trim() ?? string.Empty;
				string text2 = ReadEnv("LOADORDER_SELFTEST_PROBE");
				if (!string.IsNullOrWhiteSpace(text2))
				{
					_probeId = text2.Trim();
				}
				_scheduled = true;
				MainLoop mainLoop = Engine.GetMainLoop();
				SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
				if (val == null)
				{
					Log("no SceneTree; aborted");
					return;
				}
				Log($"scheduled: mode={_mode}, write={_write}, apply='{_applyPreset}', probe={_probeId}");
				StartTicking(val);
			}
		}

		private static void StartTicking(SceneTree tree)
		{
			if (!_ticking && !_finished)
			{
				_ticking = true;
				ChainNextTick(tree);
			}
		}

		private static void ChainNextTick(SceneTree tree)
		{
			SceneTree tree2 = tree;
			if (!_finished)
			{
				tree2.CreateTimer(0.25, true, false, false).Timeout += delegate
				{
					_ticking = false;
					Tick(tree2);
				};
			}
		}

		private static void Tick(SceneTree tree)
		{
			try
			{
				_ticks++;
				if (!SaveManagerReady())
				{
					if (++_readyTicks % 40 == 0)
					{
						Log($"waiting for SaveManager.SettingsSave.ModSettings... ({_readyTicks} ticks)");
					}
					if (_readyTicks > 400)
					{
						Log("timed out waiting for the save system; aborted");
						Finish(tree);
					}
					else
					{
						StartTicking(tree);
					}
				}
				else if (_waitTicks > 0)
				{
					_waitTicks--;
					StartTicking(tree);
				}
				else
				{
					Step(tree);
				}
			}
			catch (Exception value)
			{
				Log($"FAILED: {value}");
				Finish(tree);
			}
		}

		private static void Finish(SceneTree tree)
		{
			if (!_finished)
			{
				_finished = true;
				Log("=== self-test end ===");
				Log("quitting");
				tree.Quit(0);
			}
		}

		private static void Step(SceneTree tree)
		{
			switch (_step)
			{
			case 0:
			{
				Log($"=== self-test begin === (sceneTreePaused={tree.Paused})");
				LoadOrderPanel loadOrderPanel = new LoadOrderPanel();
				((Node)loadOrderPanel).Name = StringName.op_Implicit("LoadOrderManager_SelfTestPanel");
				_panel = loadOrderPanel;
				((Node)tree.Root).AddChild((Node)(object)_panel, false, (InternalMode)0);
				_panel.OpenPanel();
				Log("panel opened");
				_waitTicks = 2;
				_step = 1;
				StartTicking(tree);
				break;
			}
			case 1:
				_panel.DebugLogLayoutCheck();
				ReportEntries("initial");
				if (_mode == "panel")
				{
					ProbeModdingScreenInjection();
					Finish(tree);
					break;
				}
				EnsureAutoPreset();
				ReportProfiles("after-ensure");
				SelectPresetByName("Default");
				ReportEntries("Default");
				SelectPresetByName("SelfTest");
				ReportEntries("SelfTest");
				ProbeModdingScreenInjection();
				_waitTicks = 2;
				_step = 2;
				StartTicking(tree);
				break;
			case 2:
				if (_write && !string.IsNullOrEmpty(_applyPreset))
				{
					SelectPresetByName(_applyPreset);
					ReportEntries("before-apply(" + _applyPreset + ")");
					_panel.DebugApplyOrder();
					Log("applied preset '" + _applyPreset + "'");
					_waitTicks = 4;
					_step = 3;
					StartTicking(tree);
				}
				else
				{
					Log("no Apply requested (set LOADORDER_SELFTEST_WRITE=1 and LOADORDER_SELFTEST_APPLY=<preset>)");
					Finish(tree);
				}
				break;
			case 3:
				_panel.DebugLogLayoutCheck();
				ReportSavedState();
				ReportEntries("after-apply(" + _applyPreset + ")");
				Finish(tree);
				break;
			default:
				Finish(tree);
				break;
			}
		}

		private static void ProbeModdingScreenInjection()
		{
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				Type type = AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen.NModdingScreen");
				if (type == null)
				{
					Log("injection: NModdingScreen type NOT FOUND (patch cannot bind)");
					return;
				}
				MethodInfo methodInfo = AccessTools.Method(type, "Create", (Type[])null, (Type[])null);
				if (methodInfo == null)
				{
					Log("injection: NModdingScreen.Create() NOT FOUND");
					return;
				}
				object? obj = methodInfo.Invoke(null, null);
				Node val = (Node)((obj is Node) ? obj : null);
				if (val == null)
				{
					Log("injection: Create() returned null (scene load failed - expected in some headless runs)");
					return;
				}
				((Node)((SceneTree)Engine.GetMainLoop()).Root).AddChild(val, false, (InternalMode)0);
				Node val2 = val.FindChild("LoadOrderManager_OpenButton", true, false);
				Node val3 = val.FindChild("LoadOrderManager_Panel", true, false);
				Log($"injection: modding screen ready, open button present = {val2 != null}, panel present = {val3 != null}");
				val.QueueFree();
			}
			catch (Exception ex)
			{
				Log("injection: FAILED " + ex.GetType().Name + ": " + ex.Message);
			}
		}

		private static void ReportEntries(string stage)
		{
			IReadOnlyList<LoadOrderEntry> readOnlyList = _panel?.DebugEntries;
			if (readOnlyList == null)
			{
				Log(stage + ": no entries");
				return;
			}
			ModPreset currentProfile = ModPresetStore.CurrentProfile;
			Log($"{stage}: preset='{currentProfile.Name}' autoEnableNewMods={currentProfile.AutoEnableNewMods} known={currentProfile.KnownMods.Count} disabled={currentProfile.DisabledMods.Count} entries={readOnlyList.Count}");
			LoadOrderEntry loadOrderEntry = readOnlyList.FirstOrDefault((LoadOrderEntry e) => string.Equals(e.Id, _probeId, StringComparison.OrdinalIgnoreCase));
			if (loadOrderEntry == null)
			{
				Log(stage + ": " + _probeId + " NOT in list");
				return;
			}
			Log($"{stage}: {_probeId} known={currentProfile.KnownMods.Contains(loadOrderEntry.Id)} enabled={loadOrderEntry.IsEnabled} isNew={loadOrderEntry.IsNew}");
		}

		private static void ReportProfiles(string stage)
		{
			PresetSaveData presetSaveData = ModPresetStore.Load();
			for (int i = 0; i < presetSaveData.Profiles.Count; i++)
			{
				ModPreset modPreset = presetSaveData.Profiles[i];
				Log($"{stage}: profile[{i}] '{modPreset.Name}' autoEnableNewMods={modPreset.AutoEnableNewMods} known={modPreset.KnownMods.Count} disabled={modPreset.DisabledMods.Count}" + ((i == presetSaveData.CurrentProfileIndex) ? "  <- current" : string.Empty));
			}
		}

		private static void ReportSavedState()
		{
			try
			{
				var (source, text) = ReadSavedModList();
				if (text != null)
				{
					Log("saved: unable to read (" + text + ")");
					return;
				}
				foreach (SavedEntry item in source.Where((SavedEntry e) => e.Id == "LoadOrderManager" || string.Equals(e.Id, _probeId, StringComparison.OrdinalIgnoreCase)))
				{
					Log($"saved: {item.Id} source={item.Source} is_enabled={item.IsEnabled}");
				}
			}
			catch (Exception ex)
			{
				Log("saved: read failed: " + ex.Message);
			}
		}

		private static (List<SavedEntry> Entries, string? Error) ReadSavedModList()
		{
			List<SavedEntry> list = new List<SavedEntry>();
			if (!(GetMember(GetMember(GetMember(AccessTools.Property(AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.SaveManager"), "Instance")?.GetValue(null), "SettingsSave"), "ModSettings"), "ModList") is IEnumerable enumerable))
			{
				return (Entries: list, Error: "ModList unavailable");
			}
			bool flag = default(bool);
			foreach (object item in enumerable)
			{
				string text = GetMember(item, "Id")?.ToString();
				if (!string.IsNullOrWhiteSpace(text))
				{
					string? source = GetMember(item, "Source")?.ToString() ?? "?";
					object member = GetMember(item, "IsEnabled");
					int num;
					if (member is bool)
					{
						flag = (bool)member;
						num = 1;
					}
					else
					{
						num = 0;
					}
					list.Add(new SavedEntry(text, source, (byte)((uint)num & (flag ? 1u : 0u)) != 0));
				}
			}
			return (Entries: list, Error: null);
		}

		private static void EnsureAutoPreset()
		{
			if (ModPresetStore.Load().Profiles.Any((ModPreset p) => string.Equals(p.Name, "SelfTest", StringComparison.Ordinal)))
			{
				Log("preset 'SelfTest' already exists");
				return;
			}
			_panel.DebugCreatePreset("SelfTest");
			Log("created preset 'SelfTest' (autoEnableNewMods=false expected)");
		}

		private static void SelectPresetByName(string name)
		{
			string name2 = name;
			int num = ModPresetStore.Load().Profiles.FindIndex((ModPreset p) => string.Equals(p.Name, name2, StringComparison.Ordinal));
			if (num < 0)
			{
				Log("preset '" + name2 + "' not found; skipping switch");
			}
			else
			{
				_panel.DebugSelectPreset(num);
			}
		}

		private static bool SaveManagerReady()
		{
			object obj = AccessTools.Property(AccessTools.TypeByName("MegaCrit.Sts2.Core.Saves.SaveManager"), "Instance")?.GetValue(null);
			if (obj == null)
			{
				return false;
			}
			object member = GetMember(obj, "SettingsSave");
			if (member == null)
			{
				return false;
			}
			return GetMember(member, "ModSettings") != null;
		}

		private static object? GetMember(object? obj, string name)
		{
			if (obj == null)
			{
				return null;
			}
			Type type = obj.GetType();
			PropertyInfo propertyInfo = AccessTools.Property(type, name);
			if (propertyInfo != null)
			{
				return propertyInfo.GetValue(obj);
			}
			return AccessTools.Field(type, name)?.GetValue(obj);
		}

		private static string? ReadEnv(string name)
		{
			try
			{
				return Environment.GetEnvironmentVariable(name);
			}
			catch
			{
				return null;
			}
		}

		private static void Log(string message)
		{
			DebugLog.Info("[SelfTest] " + message);
		}
	}
}
