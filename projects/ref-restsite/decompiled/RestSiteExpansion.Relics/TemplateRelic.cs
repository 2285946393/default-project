using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion.Relics;

public abstract class TemplateRelic : ModRelicTemplate
{
	public abstract override RelicRarity Rarity { get; }

	public override RelicAssetProfile AssetProfile => new RelicAssetProfile("res://art/images/relics/" + ((object)this).GetType().Name + ".png", "res://art/images/relics/" + ((object)this).GetType().Name + ".png", "res://art/images/relics/" + ((object)this).GetType().Name + ".png");
}
