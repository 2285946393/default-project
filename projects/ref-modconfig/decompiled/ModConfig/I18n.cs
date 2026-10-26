using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace ModConfig;

internal static class I18n
{
	[CompilerGenerated]
	private static class _003C_003EO
	{
		public static LocaleChangeCallback _003C0_003E__OnLocaleChanged;
	}

	[CompilerGenerated]
	private sealed class _003CGetLanguageCandidates_003Ed__33 : IEnumerable<string>, IEnumerable, IEnumerator<string>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private string _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private string language;

		public string _003C_003E3__language;

		string IEnumerator<string>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CGetLanguageCandidates_003Ed__33(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				_003C_003E2__current = language;
				_003C_003E1__state = 1;
				return true;
			case 1:
			{
				_003C_003E1__state = -1;
				int num = language.IndexOf('_');
				if (num < 0)
				{
					num = language.IndexOf('-');
				}
				if (num > 0)
				{
					_003C_003E2__current = language.Substring(0, num);
					_003C_003E1__state = 2;
					return true;
				}
				goto IL_0089;
			}
			case 2:
				_003C_003E1__state = -1;
				goto IL_0089;
			case 3:
				{
					_003C_003E1__state = -1;
					break;
				}
				IL_0089:
				if (language != "en")
				{
					_003C_003E2__current = "en";
					_003C_003E1__state = 3;
					return true;
				}
				break;
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			_003CGetLanguageCandidates_003Ed__33 _003CGetLanguageCandidates_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CGetLanguageCandidates_003Ed__ = this;
			}
			else
			{
				_003CGetLanguageCandidates_003Ed__ = new _003CGetLanguageCandidates_003Ed__33(0);
			}
			_003CGetLanguageCandidates_003Ed__.language = _003C_003E3__language;
			return _003CGetLanguageCandidates_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<string>)this).GetEnumerator();
		}
	}

	private static Dictionary<string, string> _translations = new Dictionary<string, string>();

	private static string? _loadedLanguage;

	private static bool _subscribed;

	internal static string CurrentLang => _loadedLanguage ?? ResolveLanguage();

	internal static string TabMods => Get("tab_mods", "Mods");

	internal static string NoConfigs => Get("no_configs", "No mods have registered configurations.");

	internal static string ResetDefaults => Get("reset_defaults", "Reset");

	internal static string PressAnyKey => Get("press_any_key", "Press any key...");

	internal static string KeyUnbound => Get("key_unbound", "Unbound");

	internal static string KeyBindTooltip => Get("keybind_tooltip", "Click to rebind. Press Esc or click anywhere to clear.");

	internal static string ColorPickerTooltip => Get("color_picker_tooltip", "Click to pick a color.");

	internal static event Action? Changed;

	internal static void Initialize()
	{
		ForceReload();
		TrySubscribe();
	}

	internal static string Get(string key, string fallback)
	{
		EnsureLoaded();
		return _translations.GetValueOrDefault(key) ?? fallback;
	}

	private static void TrySubscribe()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		if (_subscribed)
		{
			return;
		}
		try
		{
			LocManager instance = LocManager.Instance;
			if (instance != null)
			{
				object obj = _003C_003EO._003C0_003E__OnLocaleChanged;
				if (obj == null)
				{
					LocaleChangeCallback val = OnLocaleChanged;
					_003C_003EO._003C0_003E__OnLocaleChanged = val;
					obj = (object)val;
				}
				instance.SubscribeToLocaleChange((LocaleChangeCallback)obj);
				_subscribed = true;
			}
			else
			{
				MainFile.Log.Info("LocManager.Instance is null at init, will use lazy detection", 1);
			}
		}
		catch (Exception value)
		{
			MainFile.Log.Error($"Failed to subscribe to locale change: {value}", 1);
		}
	}

	private static void OnLocaleChanged()
	{
		_loadedLanguage = null;
		ForceReload();
		I18n.Changed?.Invoke();
	}

	private static void EnsureLoaded()
	{
		if (!_subscribed)
		{
			TrySubscribe();
		}
		string text = ResolveLanguage();
		if (!string.Equals(_loadedLanguage, text, StringComparison.OrdinalIgnoreCase))
		{
			_translations = LoadTranslations(text);
			_loadedLanguage = text;
		}
	}

	private static void ForceReload()
	{
		string text = ResolveLanguage();
		_translations = LoadTranslations(text);
		_loadedLanguage = text;
	}

	private static string ResolveLanguage()
	{
		string text = null;
		try
		{
			LocManager instance = LocManager.Instance;
			text = ((instance != null) ? instance.Language : null);
		}
		catch
		{
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			try
			{
				text = TranslationServer.GetLocale();
			}
			catch
			{
			}
		}
		return NormalizeLanguageCode(text);
	}

	private static Dictionary<string, string> LoadTranslations(string language)
	{
		foreach (string languageCandidate in GetLanguageCandidates(language))
		{
			Dictionary<string, string> dictionary = TryLoadEmbedded(languageCandidate);
			if (dictionary != null && dictionary.Count > 0)
			{
				return dictionary;
			}
			Dictionary<string, string> dictionary2 = TryLoadFromPck("res://ModConfig/localization/" + languageCandidate + ".json");
			if (dictionary2 != null && dictionary2.Count > 0)
			{
				return dictionary2;
			}
		}
		return new Dictionary<string, string>();
	}

	private static Dictionary<string, string>? TryLoadEmbedded(string lang)
	{
		try
		{
			Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ModConfig.localization." + lang + ".json");
			if (manifestResourceStream == null)
			{
				return null;
			}
			using StreamReader streamReader = new StreamReader(manifestResourceStream);
			return JsonSerializer.Deserialize<Dictionary<string, string>>(streamReader.ReadToEnd());
		}
		catch
		{
			return null;
		}
	}

	private static Dictionary<string, string>? TryLoadFromPck(string path)
	{
		try
		{
			if (!FileAccess.FileExists(path))
			{
				return null;
			}
			FileAccess val = FileAccess.Open(path, (ModeFlags)1);
			try
			{
				return JsonSerializer.Deserialize<Dictionary<string, string>>(val.GetAsText(false));
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}
		catch
		{
			return null;
		}
	}

	private static string NormalizeLanguageCode(string? code)
	{
		if (string.IsNullOrWhiteSpace(code))
		{
			return "en";
		}
		code = code.ToLowerInvariant().Trim();
		if (code != null)
		{
			int length = code.Length;
			if (length != 2)
			{
				if (length == 3)
				{
					switch (code[0])
					{
					case 'z':
						break;
					case 'j':
						goto IL_00b8;
					case 'k':
						goto IL_00cd;
					case 'd':
						goto IL_00e2;
					case 'f':
						goto IL_00f7;
					case 'e':
						goto IL_0106;
					case 'i':
						goto IL_0115;
					case 'p':
						goto IL_0124;
					case 'r':
						goto IL_0140;
					case 't':
						goto IL_014f;
					default:
						goto IL_017c;
					}
					if (code == "zhs" || code == "zht")
					{
						goto IL_0178;
					}
				}
			}
			else if (code == "en")
			{
				goto IL_0178;
			}
		}
		goto IL_017c;
		IL_0140:
		if (code == "rus")
		{
			goto IL_0178;
		}
		goto IL_017c;
		IL_014f:
		if (code == "tha" || code == "tur")
		{
			goto IL_0178;
		}
		goto IL_017c;
		IL_0178:
		bool flag = true;
		goto IL_017e;
		IL_017e:
		if (flag)
		{
			return code;
		}
		if (code.StartsWith("zh"))
		{
			return "zhs";
		}
		if (code.StartsWith("en"))
		{
			return "en";
		}
		if (code.StartsWith("ja"))
		{
			return "jpn";
		}
		if (code.StartsWith("ko"))
		{
			return "kor";
		}
		return "en";
		IL_017c:
		flag = false;
		goto IL_017e;
		IL_00e2:
		if (code == "deu")
		{
			goto IL_0178;
		}
		goto IL_017c;
		IL_0106:
		if (code == "esp")
		{
			goto IL_0178;
		}
		goto IL_017c;
		IL_00f7:
		if (code == "fra")
		{
			goto IL_0178;
		}
		goto IL_017c;
		IL_0115:
		if (code == "ita")
		{
			goto IL_0178;
		}
		goto IL_017c;
		IL_00b8:
		if (code == "jpn")
		{
			goto IL_0178;
		}
		goto IL_017c;
		IL_00cd:
		if (code == "kor")
		{
			goto IL_0178;
		}
		goto IL_017c;
		IL_0124:
		if (code == "pol" || code == "ptb")
		{
			goto IL_0178;
		}
		goto IL_017c;
	}

	[IteratorStateMachine(typeof(_003CGetLanguageCandidates_003Ed__33))]
	private static IEnumerable<string> GetLanguageCandidates(string language)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetLanguageCandidates_003Ed__33(-2)
		{
			_003C_003E3__language = language
		};
	}
}
