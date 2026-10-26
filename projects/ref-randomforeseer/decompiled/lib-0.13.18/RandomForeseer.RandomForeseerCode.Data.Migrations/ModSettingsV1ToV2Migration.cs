using System.Text.Json.Nodes;
using STS2RitsuLib.Utils.Persistence.Migration;

namespace RandomForeseer.RandomForeseerCode.Data.Migrations;

internal sealed class ModSettingsV1ToV2Migration : IMigration
{
	public int FromVersion => 1;

	public int ToVersion => 2;

	public bool Migrate(JsonObject data)
	{
		if (data.TryGetPropertyValue("RelicPickupPredictionEnabled", out JsonNode jsonNode))
		{
			data.SetIfMissing("AncientRelicPickupPredictionEnabled", jsonNode);
		}
		return true;
	}
}
