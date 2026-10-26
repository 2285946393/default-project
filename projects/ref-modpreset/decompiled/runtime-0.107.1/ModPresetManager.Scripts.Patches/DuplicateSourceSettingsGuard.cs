using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves;

namespace ModPresetManager.Scripts.Patches;

internal static class DuplicateSourceSettingsGuard
{
	private readonly record struct ModSourceKey(string Id, ModSource Source)
	{
		[CompilerGenerated]
		private bool PrintMembers(StringBuilder builder)
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			builder.Append("Id = ");
			builder.Append((object?)Id);
			builder.Append(", Source = ");
			ModSource source = Source;
			builder.Append(((object)(ModSource)(ref source)).ToString());
			return true;
		}

		[CompilerGenerated]
		public void Deconstruct(out string Id, out ModSource Source)
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Expected I4, but got Unknown
			Id = this.Id;
			Source = (ModSource)(int)this.Source;
		}
	}

	private const int MaxDeferredAttempts = 120;

	private static Dictionary<ModSourceKey, bool>? _enabledBySource;

	private static int _remainingAttempts;

	private static bool _restoreScheduled;

	public static void CaptureAndScheduleRestore()
	{
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		if (_restoreScheduled)
		{
			return;
		}
		ModSettings modSettings = SaveManager.Instance.SettingsSave.ModSettings;
		if (modSettings == null)
		{
			return;
		}
		HashSet<string> duplicateIds = (from @group in ModManager.Mods.Where((Mod mod) => !string.IsNullOrWhiteSpace(mod.manifest?.id)).GroupBy<Mod, string>((Mod mod) => mod.manifest.id, StringComparer.Ordinal)
			where @group.Select((Mod mod) => mod.modSource).Distinct().Count() > 1
			select @group.Key).ToHashSet<string>(StringComparer.Ordinal);
		if (duplicateIds.Count != 0)
		{
			_enabledBySource = (from mod in ModManager.Mods.Where(delegate(Mod mod)
				{
					string text = mod.manifest?.id;
					return text != null && duplicateIds.Contains(text);
				})
				select new ModSourceKey(mod.manifest.id, mod.modSource)).Distinct().ToDictionary((ModSourceKey key) => key, (ModSourceKey key) => !modSettings.IsModDisabled(key.Id, key.Source));
			if (_enabledBySource.Count != 0)
			{
				_remainingAttempts = 120;
				_restoreScheduled = true;
				Callable val = Callable.From((Action)RestoreAfterInitialization);
				((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
			}
		}
	}

	private static void RestoreAfterInitialization()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		if ((int)ModManager.State != 1)
		{
			if (--_remainingAttempts > 0)
			{
				Callable val = Callable.From((Action)RestoreAfterInitialization);
				((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
			}
			else
			{
				Log.Warn("ModPresetManager could not restore duplicate-source mod settings because mod initialization did not finish.", 2);
				Clear();
			}
			return;
		}
		ModSettings modSettings = SaveManager.Instance.SettingsSave.ModSettings;
		List<SettingsSaveMod> list = ((modSettings != null) ? modSettings.ModList : null);
		if (list == null || _enabledBySource == null)
		{
			Clear();
			return;
		}
		int num = 0;
		foreach (SettingsSaveMod item in list)
		{
			ModSourceKey key = new ModSourceKey(item.Id, item.Source);
			if (_enabledBySource.TryGetValue(key, out var value) && item.IsEnabled != value)
			{
				item.IsEnabled = value;
				num++;
			}
		}
		if (num > 0)
		{
			Log.Info($"ModPresetManager restored source-specific enabled states for {num} duplicate mod entries after startup normalization.", 2);
		}
		Clear();
	}

	private static void Clear()
	{
		_enabledBySource = null;
		_remainingAttempts = 0;
		_restoreScheduled = false;
	}
}
