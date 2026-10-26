using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using Godot;
using RandomForeseer.RandomForeseerCode.Common.Nodes;
using RandomForeseer.RandomForeseerCode.InCombat.Nodes;
using RandomForeseer.RandomForeseerCode.OutOfCombat.Nodes;

[assembly: AssemblyMetadata("RandomForeseerModVersion", "0.13.18")]
[assembly: AssemblyMetadata("RandomForeseerPostHogHost", "https://random-foreseer-telemetry.hotwords.top")]
[assembly: AssemblyMetadata("RandomForeseerPostHogProjectApiKey", "rf_ingress_135535c31aba72712ebb2becd88d62e6a8a2811a5802f4c16289b03403922a1b")]
[assembly: IgnoresAccessChecksTo("sts2")]
[assembly: AssemblyCompany("RandomForeseer")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0+d17ff4e7de6036dee9db89d47d10c5f2d2334fd9")]
[assembly: AssemblyProduct("RandomForeseer")]
[assembly: AssemblyTitle("RandomForeseer")]
[assembly: InternalsVisibleTo("RandomForeseer.Tests")]
[assembly: AssemblyHasScripts(new Type[]
{
	typeof(NMegaLabel),
	typeof(NCombatPredictionDamageIndicator),
	typeof(NCombatPredictionSourceIcon),
	typeof(NNextActPredictionIcon)
})]
[assembly: AssemblyVersion("1.0.0.0")]
[module: RefSafetyRules(11)]
