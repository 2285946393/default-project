using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(StrikeRegent))]
public class StrikeRegent : CardFX
{
	public override HoldingModes HoldingMode => HoldingModes.None;

	public override bool UseV2Patch => true;

	public override bool HasOnBeforeDamage => true;

	public override string? ChangeHitFx => "vfx/vfx_starry_impact";

	public override bool ShouldDisableRegentWeaponSFX => false;

	public override async Task OnBeforeDamage(AttackCommand command)
	{
		Creature singleTarget = command._singleTarget;
		if (singleTarget != null && command._singleTarget != null)
		{
			await PlayVfx(singleTarget);
		}
	}

	private static async Task PlayVfx(Creature target)
	{
		if (!TestMode.IsOn)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature val = ((instance != null) ? instance.GetCreatureNode(target) : null);
			if (val != null)
			{
				Blade.PlayBlade(val.VfxSpawnPosition);
				await VFXUtil.Wait(0.05f);
			}
		}
	}
}
