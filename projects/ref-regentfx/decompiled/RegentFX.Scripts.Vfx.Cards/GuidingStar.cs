using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(GuidingStar))]
public class GuidingStar : CardFX
{
	private List<Vector2> starPos = new List<Vector2>
	{
		new Vector2(2f, 2f),
		new Vector2(0f, -5f),
		new Vector2(-5f, 0f)
	};

	public override int StarCount => 3;

	public override Vector2 TargetOffset => new Vector2(-200f, -450f);

	public override string VfxScenePath => "res://RegentFX/scenes/vfx/guiding_star.tscn";

	public override bool UseV2Patch => true;

	public override bool HasOnBeforeDamage => true;

	public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		return basePosition + TargetOffset + starPos[index];
	}

	public override async Task OnBeforeDamage(AttackCommand command)
	{
		CardModel? obj = card;
		Creature obj2 = ((obj != null) ? obj.Owner.Creature : null);
		Creature singleTarget = command._singleTarget;
		if (obj2 != null && singleTarget != null && command._singleTarget != null)
		{
			Entry.StarEffectController?.OnPlayCard();
			SfxCmd.Play("event:/sfx/characters/regent/regent_guiding_star", 1f);
			await CardVfxUtil.PlayTargetedVfx(this, card.Owner.Creature, singleTarget, "GuidingStar");
		}
	}
}
