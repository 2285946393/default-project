using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(Supermassive))]
public sealed class Supermassive : CardFX
{
	public override HoldingModes HoldingMode => HoldingModes.None;

	public override string VfxScenePath => "res://RegentFX/scenes/vfx/super_massive.tscn";

	public override bool UseV2Patch => true;

	public override bool HasOnBeforeDamageTargeted => true;

	public override bool RemoveHitFx => Entry.SupermassiveController?.HasReadyOrb ?? false;

	public override bool ShouldDisableRegentWeaponAttack => Entry.SupermassiveController?.HasReadyOrb ?? false;

	public override bool ShouldDisableRegentWeaponSFX => true;

	public override Task OnBeforeDamage(AttackCommand command, IReadOnlyList<Creature> targets)
	{
		Creature val = targets.FirstOrDefault();
		if (val != null && !val.IsDead)
		{
			CardModel? obj = card;
			if (obj != null)
			{
				Player owner = obj.Owner;
				if (((owner != null) ? new bool?(owner.Creature.IsDead) : null) == false)
				{
					Entry.SupermassiveController?.TryLaunch(val);
					return Task.CompletedTask;
				}
			}
		}
		return Task.CompletedTask;
	}
}
