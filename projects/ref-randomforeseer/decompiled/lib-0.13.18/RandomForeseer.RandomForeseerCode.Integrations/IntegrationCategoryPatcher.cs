using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.Integrations;

internal sealed class IntegrationCategoryPatcher
{
	private sealed class Registration
	{
		[CompilerGenerated]
		private Harmony _003Charmony_003EP;

		[CompilerGenerated]
		private Assembly _003Cassembly_003EP;

		[CompilerGenerated]
		private string _003CmodId_003EP;

		[CompilerGenerated]
		private string _003Ccategory_003EP;

		private bool _patched;

		public Registration(Harmony harmony, Assembly assembly, string modId, string category)
		{
			_003Charmony_003EP = harmony;
			_003Cassembly_003EP = assembly;
			_003CmodId_003EP = modId;
			_003Ccategory_003EP = category;
			base._002Ector();
		}

		public bool Matches(Mod mod)
		{
			return mod.manifest?.id == _003CmodId_003EP;
		}

		public bool TryPatchIfLoaded()
		{
			if (_patched)
			{
				return true;
			}
			if (!ModManager.GetLoadedMods().Any((Mod mod) => mod.manifest?.id == _003CmodId_003EP))
			{
				return false;
			}
			try
			{
				_003Charmony_003EP.PatchCategory(_003Cassembly_003EP, _003Ccategory_003EP);
				Entry.Logger.Info($"Patched integration category {_003Ccategory_003EP} for {_003CmodId_003EP}.", 1);
			}
			catch (Exception ex)
			{
				Entry.Logger.Warn($"Could not patch integration category {_003Ccategory_003EP} for {_003CmodId_003EP}: {ex}", 1);
				ModTelemetry.CaptureException(ex, "integration_category_patcher", "patch_category", new
				{
					ModId = _003CmodId_003EP,
					PatchCategory = _003Ccategory_003EP
				});
			}
			_patched = true;
			return true;
		}
	}

	[CompilerGenerated]
	private Harmony _003Charmony_003EP;

	[CompilerGenerated]
	private Assembly _003Cassembly_003EP;

	private readonly List<Registration> _registrations;

	private bool _subscribed;

	public IntegrationCategoryPatcher(Harmony harmony, Assembly assembly)
	{
		_003Charmony_003EP = harmony;
		_003Cassembly_003EP = assembly;
		_registrations = new List<Registration>();
		base._002Ector();
	}

	public void Register(string modId, string category)
	{
		Registration registration = new Registration(_003Charmony_003EP, _003Cassembly_003EP, modId, category);
		if (!registration.TryPatchIfLoaded())
		{
			_registrations.Add(registration);
			if (!_subscribed)
			{
				ModManager.OnModDetected += OnModDetected;
				_subscribed = true;
			}
		}
	}

	private void OnModDetected(Mod mod)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)mod.state != 1)
		{
			return;
		}
		for (int num = _registrations.Count - 1; num >= 0; num--)
		{
			Registration registration = _registrations[num];
			if (registration.Matches(mod) && registration.TryPatchIfLoaded())
			{
				_registrations.RemoveAt(num);
			}
		}
		if (_registrations.Count == 0 && _subscribed)
		{
			ModManager.OnModDetected -= OnModDetected;
			_subscribed = false;
		}
	}
}
