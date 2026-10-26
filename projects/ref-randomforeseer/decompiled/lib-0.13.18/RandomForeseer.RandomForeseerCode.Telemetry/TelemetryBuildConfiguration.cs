using System;
using STS2RitsuLib.Telemetry;

namespace RandomForeseer.RandomForeseerCode.Telemetry;

internal static class TelemetryBuildConfiguration
{
	private const string PostHogHostMetadataName = "RandomForeseerPostHogHost";

	private const string PostHogProjectApiKeyMetadataName = "RandomForeseerPostHogProjectApiKey";

	public static ITelemetryAdapter CreateAdapter()
	{
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected O, but got Unknown
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		Entry.AssemblyMetadata.TryGetValue("RandomForeseerPostHogHost", out string value);
		Entry.AssemblyMetadata.TryGetValue("RandomForeseerPostHogProjectApiKey", out string value2);
		if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(value2))
		{
			return (ITelemetryAdapter)new DisabledTelemetryAdapter("PostHog host or project API key was not configured at build time.");
		}
		try
		{
			return (ITelemetryAdapter)new PostHogTelemetryAdapter(value, value2);
		}
		catch (Exception value3)
		{
			Entry.Logger.Warn($"PostHog telemetry adapter initialization failed: {value3}", 1);
			return (ITelemetryAdapter)new DisabledTelemetryAdapter("PostHog telemetry adapter initialization failed.");
		}
	}
}
