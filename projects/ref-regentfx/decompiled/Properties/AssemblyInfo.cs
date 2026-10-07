using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using Godot;
using RegentFX.Scripts;
using RegentFX.Scripts.Vfx;

[assembly: IgnoresAccessChecksTo("sts2")]
[assembly: AssemblyCompany("RegentFX")]
[assembly: AssemblyConfiguration("ExportRelease")]
[assembly: AssemblyFileVersion("0.5.1.0")]
[assembly: AssemblyInformationalVersion("0.5.1+e131eb1525d7703d58a228af387cb6c8ccd47a26")]
[assembly: AssemblyProduct("RegentFX")]
[assembly: AssemblyTitle("RegentFX")]
[assembly: AssemblyMetadata("RitsuLib.ModSettingsInterop.ProviderType", "RegentFX.ThirdParty.RitsuLibModConfig")]
[assembly: AssemblyHasScripts(new Type[]
{
	typeof(MyNLargeMissile),
	typeof(Blackhole),
	typeof(Blade),
	typeof(Pillar),
	typeof(Star),
	typeof(StardustVfx),
	typeof(StarEffectController),
	typeof(StarRingController),
	typeof(StarRingTest),
	typeof(SupermassiveController)
})]
[assembly: AssemblyVersion("0.5.1.0")]
[module: RefSafetyRules(11)]
