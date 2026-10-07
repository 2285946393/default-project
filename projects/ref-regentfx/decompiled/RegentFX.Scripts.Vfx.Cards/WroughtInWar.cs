using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(WroughtInWar))]
public class WroughtInWar : CardFX
{
	public override HoldingModes HoldingMode => HoldingModes.None;

	public override string? VfxScenePath => "res://RegentFX/scenes/vfx/wrought_in_war.tscn";

	public override bool UseV2Patch => true;

	public override bool HasOnBeforeDamage => true;

	public override async Task OnBeforeDamage(AttackCommand command)
	{
		Creature singleTarget = command._singleTarget;
		if (singleTarget != null)
		{
			await PlayVfx(singleTarget);
		}
	}

	private async Task PlayVfx(Creature target)
	{
		if (TestMode.IsOn)
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(target) : null);
		if (val != null)
		{
			FmodLite.Play("event:/RegentFx/sfx/wiw1");
			VFXUtil.PlaySimple(VfxScenePath, val.VfxSpawnPosition);
			await VFXUtil.Wait(0.1f);
			NGame instance2 = NGame.Instance;
			if (instance2 != null)
			{
				instance2.ScreenShake((ShakeStrength)4, (ShakeDuration)2, -1f);
			}
			await VFXUtil.Wait(0.15f);
		}
	}
}
