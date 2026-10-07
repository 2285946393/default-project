using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(FallingStar))]
public class FallingStar : CardFX
{
	private List<Vector2> starPos = new List<Vector2>
	{
		new Vector2(0f, 0f),
		new Vector2(30f, -50f)
	};

	public override int StarCount => 2;

	public override Vector2 TargetOffset => new Vector2(-200f, -450f);

	public override string VfxScenePath => "res://RegentFX/scenes/vfx/falling_star.tscn";

	public override string HitSfxPath => "event:/RegentFx/sfx/falling_star";

	public override bool HasExposureEffect => true;

	public override float ExposureInDuration => 0.1f;

	public override float ExposurePeak => 1.3f;

	public override float ExposureOutDuration => 0.5f;

	public override bool UseV2Patch => true;

	public override bool HasOnBeforeDamage => true;

	public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		return basePosition + TargetOffset + starPos[index] * 1.2f;
	}

	public override void OnStartHolding(Star star, int index)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		if (index == 0)
		{
			star.ChangeColorTo(new Color(14.551f, 14.551f, 0f, 1f));
		}
		if (index == 1)
		{
			star.ChangeColorTo(new Color(14.551f, 0.683f, 9.982f, 1f));
		}
	}

	public override async Task OnBeforeDamage(AttackCommand command)
	{
		CardModel? obj = card;
		Creature obj2 = ((obj != null) ? obj.Owner.Creature : null);
		Creature singleTarget = command._singleTarget;
		if (obj2 != null && singleTarget != null && command._singleTarget != null)
		{
			Entry.StarEffectController?.OnPlayCard();
			await CardVfxUtil.PlayTargetedVfx(this, card.Owner.Creature, singleTarget, "FallingStar");
		}
	}
}
