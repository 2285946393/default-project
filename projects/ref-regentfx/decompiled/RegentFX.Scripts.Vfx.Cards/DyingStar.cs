using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(DyingStar))]
public class DyingStar : CardFX
{
	private List<Vector2> starPos = new List<Vector2>
	{
		new Vector2(0f, 0f),
		new Vector2(13f, -24f),
		new Vector2(30f, -5f)
	};

	public override int StarCount => 3;

	public override Vector2 TargetOffset => new Vector2(-200f, -450f);

	public override string VfxScenePath => "res://RegentFX/scenes/vfx/dying_star.tscn";

	public override string HitSfxPath => "event:/RegentFx/sfx/common_hold_4";

	public override string SecondarySfxPath => "event:/RegentFx/sfx/dying_star";

	public override float VfxClearDelay => 3f;

	public override bool HasExposureEffect => true;

	public override float ExposurePeak => 1.7f;

	public override float ExposureInDuration => 0.6f;

	public override float ExposureOutDuration => 0.2f;

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
		return basePosition + TargetOffset + starPos[index] * 2.5f;
	}

	public override void OnStartHolding(Star star, int index)
	{
		star.PulseMinScale *= 2f;
		star.PulseMaxScale *= 1.8f;
	}

	public override async Task OnBeforeDamage(AttackCommand command)
	{
		CardModel? obj = card;
		Creature val = ((obj != null) ? obj.Owner.Creature : null);
		if (val != null)
		{
			VFXUtil.ShakeAfter(0.35f, (ShakeStrength)4, (ShakeDuration)2);
			Entry.StarEffectController?.OnPlayCard();
			await CardVfxUtil.PlayAoeVfx(this, val, card, "DyingStar");
			await VFXUtil.Wait(0.3f);
		}
	}
}
