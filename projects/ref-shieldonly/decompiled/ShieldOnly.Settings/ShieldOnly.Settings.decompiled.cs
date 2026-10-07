using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using JmcModLib.Config;
using JmcModLib.Config.Storage;
using JmcModLib.Config.UI;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]
[assembly: AssemblyCompany("ShieldOnly.Settings")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.9.48.0")]
[assembly: AssemblyInformationalVersion("1.9.48-tempo.4")]
[assembly: AssemblyProduct("ShieldOnly.Settings")]
[assembly: AssemblyTitle("ShieldOnly.Settings")]
[assembly: AssemblyVersion("1.9.48.0")]
[module: RefSafetyRules(11)]
namespace ShieldOnly;

public static class ModSettingsMenu
{
	private sealed class SettingsStorage : IConfigStorage
	{
		[CompilerGenerated]
		private AnimationSettingsStore <store>P;

		public SettingsStorage(AnimationSettingsStore store)
		{
			<store>P = store;
			base..ctor();
		}

		public string GetFileName(Assembly? assembly = null)
		{
			return Path.GetFileName(<store>P.FilePath);
		}

		public string GetFilePath(Assembly? assembly = null)
		{
			return <store>P.FilePath;
		}

		public bool Exists(Assembly? assembly = null)
		{
			return File.Exists(<store>P.FilePath);
		}

		public void Save(string key, string group, object? value, Assembly? assembly = null)
		{
			SettingsState.Save(key, value);
		}

		public bool TryLoad(string key, string group, Type valueType, out object? value, Assembly? assembly = null)
		{
			value = <store>P.Get(key);
			return value.GetType() == valueType;
		}

		public void Flush(Assembly? assembly = null)
		{
		}
	}

	public static void Install()
	{
		AnimationSettings current = AnimationSettings.Current;
		try
		{
			RegisterSettings();
		}
		catch
		{
			try
			{
				ConfigManager.Unregister(typeof(ModEntry).Assembly);
			}
			catch
			{
			}
			AnimationSettings.Current = current;
			throw;
		}
	}

	private static void RegisterSettings()
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Expected O, but got Unknown
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Expected O, but got Unknown
		Assembly assembly = typeof(ModEntry).Assembly;
		if (ConfigManager.GetEntries(assembly).Count != 0)
		{
			return;
		}
		AnimationSettingsStore store = SettingsState.Store;
		SettingsState.Activate();
		ConfigManager.SetStorage((IConfigStorage)(object)new SettingsStorage(store), assembly);
		for (int i = 0; i < SettingsState.Options.Length; i++)
		{
			(string, string, object) tuple = SettingsState.Options[i];
			if (tuple.Item3 is bool defaultValue2)
			{
				Register<bool>(tuple.Item2, tuple.Item1, defaultValue2, (UIConfigAttribute)new UIToggleAttribute(), i);
			}
			else
			{
				Register<int>(tuple.Item2, tuple.Item1, (int)tuple.Item3, (UIConfigAttribute)new UISliderAttribute(0.0, (double)((tuple.Item1 == "BodySlamBlockThreshold") ? 50 : 40), 1.0), i);
			}
		}
		void Register<T>(string title, string key, T defaultValue, UIConfigAttribute ui, int order) where T : notnull
		{
			string key2 = key;
			bool registering = true;
			Func<T> obj = () => (!registering) ? ((T)store.Get(key2)) : defaultValue;
			Action<T> obj2 = delegate(T value)
			{
				SettingsState.Save(key2, value);
			};
			string text = key2;
			string text2 = SettingsLocalization.Description(key2, english: true);
			ConfigManager.RegisterConfig<T>(title, obj, obj2, "DefaultGroup", (Action<T>)null, ui, text, "settings_ui", SettingsLocalization.TitleKey(key2), (string)null, text2, SettingsLocalization.DescriptionKey(key2), order, false, assembly);
			registering = false;
		}
	}
}
