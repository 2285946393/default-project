using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using STS2RitsuLib.Telemetry;

namespace RandomForeseer.RandomForeseerCode.Telemetry;

internal sealed class ModTelemetryAdapter : ITelemetryAdapter
{
	[CompilerGenerated]
	private ITelemetryAdapter _003Cinner_003EP;

	private const string ModVersionMetadataName = "RandomForeseerModVersion";

	private const string UnknownModVersion = "<unknown>";

	private readonly string _modVersion;

	public string AdapterId => _003Cinner_003EP.AdapterId;

	public string EndpointDescription => _003Cinner_003EP.EndpointDescription;

	public ModTelemetryAdapter(ITelemetryAdapter inner)
	{
		_003Cinner_003EP = inner;
		_modVersion = ResolveModVersion();
		base._002Ector();
	}

	public ValueTask<TelemetrySendResult> SendAsync(TelemetryApplicant applicant, IReadOnlyList<TelemetryEnvelope> events, CancellationToken cancellationToken = default(CancellationToken))
	{
		foreach (TelemetryEnvelope @event in events)
		{
			@event.Properties["random_foreseer_version"] = _modVersion;
		}
		return _003Cinner_003EP.SendAsync(applicant, events, cancellationToken);
	}

	private static string ResolveModVersion()
	{
		Entry.AssemblyMetadata.TryGetValue("RandomForeseerModVersion", out string value);
		value = value?.Trim();
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		Entry.Logger.Warn("Could not resolve the Random Foreseer version for telemetry events.", 1);
		return "<unknown>";
	}
}
