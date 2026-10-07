using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(Stardust))]
public class Stardust : CardFX
{
	public override HoldingModes HoldingMode => HoldingModes.BorrowAll;

	public override string? VfxScenePath => "res://RegentFX/scenes/vfx/star_strike.tscn";

	public override string? HitSfxPath => "event:/RegentFx/sfx/stardust";

	public override bool ShouldDisableRegentWeaponSFX => true;

	public override bool RemoveHitFx => true;

	public override bool UseV2Patch => true;

	public override bool HasOnBeforeDamageTargeted => true;

	public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return basePosition + TargetOffset + VFXUtil.RandVec2(40f);
	}

	public override void OnStartHolding(Star star, int index)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		star.ChangeColorTo(new Color(14.551f, 14.551f, 0f, 1f));
	}

	public override async Task OnBeforeDamage(AttackCommand command, IReadOnlyList<Creature> targets)
	{
		CardModel? obj = card;
		Creature val = ((obj != null) ? obj.Owner.Creature : null);
		if (targets.Count != 1)
		{
			return;
		}
		Creature val2 = targets.First();
		if (val == null || val2 == null)
		{
			return;
		}
		Vector2? val3 = Entry.StarEffectController?.PopStar(this);
		Vector2 val4;
		if (!val3.HasValue)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature obj2 = ((instance != null) ? instance.GetCreatureNode(val) : null);
			val4 = ((obj2 != null) ? obj2.VfxSpawnPosition : Vector2.Zero) + new Vector2(0f, -400f);
		}
		else
		{
			val4 = val3.Value + new Vector2(0f, -200f);
		}
		val4 += VFXUtil.RandVec2(200f);
		if (!TestMode.IsOn)
		{
			FmodLite.Play("event:/RegentFx/sfx/stardust");
			NCombatRoom instance2 = NCombatRoom.Instance;
			NCreature val5 = ((instance2 != null) ? instance2.GetCreatureNode(val2) : null);
			if (val5 != null)
			{
				Vector2 vfxSpawnPosition = val5.VfxSpawnPosition;
				StardustVfx.PlayStardust(val4, vfxSpawnPosition);
				TaskHelper.RunSafely(OnHit(vfxSpawnPosition, val2));
				await VFXUtil.Wait(0.1f);
			}
		}
	}

	private async Task OnHit(Vector2 targetPos, Creature target)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		await VFXUtil.Wait(0.35f);
		VFXUtil.ReplayAllParticles(VFXUtil.PlaySimple(VfxScenePath, targetPos));
		VfxCmd.PlayOnCreatureCenter(target, "vfx/vfx_starry_impact");
	}
}
