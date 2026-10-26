using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion.Cards;

public abstract class TemplateCard : ModCardTemplate
{
	public override CardAssetProfile AssetProfile => new CardAssetProfile("res://art/images/card_portraits/" + ((object)this).GetType().Name + ".png", (string)null, (string)null, (string)null, (string)null, (string)null, (string)null, (string)null, (string)null, (Material)null, (Material)null, (string)null, (Material)null, (string)null, (string)null, (string)null, (Material)null, (string)null, (Material)null, (string)null, (Material)null, (string)null, (Material)null, (string)null, (string)null, (Material)null, (CardVisualStyle)0);

	protected TemplateCard(int cost, CardType type, CardRarity rarity, TargetType target, bool showInLibrary = true)
		: base(cost, type, rarity, target, showInLibrary)
	{
	}//IL_0002: Unknown result type (might be due to invalid IL or missing references)
	//IL_0003: Unknown result type (might be due to invalid IL or missing references)
	//IL_0004: Unknown result type (might be due to invalid IL or missing references)

}
