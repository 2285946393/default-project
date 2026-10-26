using MegaCrit.Sts2.Core.Debug;

namespace JmcModLib.Dispatch.Bootstrap;

internal readonly record struct GameVersionInfo(string? RawVersion, SemanticVersion? SemVer);
