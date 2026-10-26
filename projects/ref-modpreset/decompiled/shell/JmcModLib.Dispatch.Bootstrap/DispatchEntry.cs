using System;
using System.Collections.Generic;
using System.IO;
using MegaCrit.Sts2.Core.Debug;

namespace JmcModLib.Dispatch.Bootstrap;

internal sealed class DispatchEntry
{
	private SemanticVersion? minVersion;

	private SemanticVersion? maxVersionExclusive;

	public string Id { get; set; } = string.Empty;


	public string? MinGameVersion { get; set; }

	public string? MaxGameVersionExclusive { get; set; }

	public string RuntimeAssembly { get; set; } = string.Empty;


	public List<string> ProbeDirectories { get; set; } = new List<string>();


	public List<string> Dependencies { get; set; } = new List<string>();


	public bool? ProbeAllDlls { get; set; }

	public bool EffectiveProbeAllDlls => ProbeAllDlls.GetValueOrDefault();

	public void Normalize()
	{
		Id = (string.IsNullOrWhiteSpace(Id) ? RuntimeAssembly : Id.Trim());
		RuntimeAssembly = NormalizeText(RuntimeAssembly, string.Empty);
		if (string.IsNullOrWhiteSpace(RuntimeAssembly))
		{
			throw new InvalidOperationException("分派项 " + Id + " 缺少 runtimeAssembly。");
		}
		MinGameVersion = NormalizeOptionalText(MinGameVersion);
		MaxGameVersionExclusive = NormalizeOptionalText(MaxGameVersionExclusive);
		ProbeDirectories = DispatchDescriptor.NormalizeList(ProbeDirectories);
		Dependencies = DispatchDescriptor.NormalizeList(Dependencies);
		if (ProbeDirectories.Count == 0)
		{
			string directoryName = Path.GetDirectoryName(RuntimeAssembly);
			ProbeDirectories.Add(string.IsNullOrWhiteSpace(directoryName) ? "." : directoryName);
		}
		minVersion = ParseOptionalVersion(MinGameVersion, "MinGameVersion");
		maxVersionExclusive = ParseOptionalVersion(MaxGameVersionExclusive, "MaxGameVersionExclusive");
	}

	public bool IsMatch(GameVersionInfo gameVersion)
	{
		if (minVersion == null && maxVersionExclusive == null)
		{
			return true;
		}
		if (gameVersion.SemVer == null)
		{
			return false;
		}
		if (minVersion != null && gameVersion.SemVer.CompareTo(minVersion) < 0)
		{
			return false;
		}
		return maxVersionExclusive == null || gameVersion.SemVer.CompareTo(maxVersionExclusive) < 0;
	}

	private static SemanticVersion? ParseOptionalVersion(string? version, string memberName)
	{
		if (string.IsNullOrWhiteSpace(version))
		{
			return null;
		}
		SemanticVersion val = default(SemanticVersion);
		if (SemanticVersion.TryFromString(version, ref val) && val != null)
		{
			return val;
		}
		throw new InvalidOperationException(memberName + " 不是有效的语义化版本：" + version);
	}

	private static string NormalizeText(string? text, string fallback)
	{
		return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
	}

	private static string? NormalizeOptionalText(string? text)
	{
		return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
	}
}
