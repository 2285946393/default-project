using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using RandomForeseer.RandomForeseerCode.Data;
using STS2RitsuLib.Telemetry;

namespace RandomForeseer.RandomForeseerCode.Telemetry;

internal sealed class SettingsTelemetryContribution : ITelemetryContributionProvider
{
	public const string Id = "settings_snapshot";

	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters = { (JsonConverter)new JsonStringEnumConverter() }
	};

	public string ContributorModId => "RandomForeseer";

	public string ContributionId => "settings_snapshot";

	public TelemetryDataCategory Category => (TelemetryDataCategory)1;

	public TelemetryContributionVisibility Visibility => (TelemetryContributionVisibility)0;

	public JsonNode Build(TelemetryContributionContext context)
	{
		return JsonSerializer.SerializeToNode(ModData.Settings, JsonOptions);
	}
}
