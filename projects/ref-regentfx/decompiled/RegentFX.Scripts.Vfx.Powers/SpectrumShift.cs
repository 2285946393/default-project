using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Powers;

[PowerFx(typeof(SpectrumShiftPower))]
[HarmonyPatch]
public class SpectrumShift : PowerFX
{
	public override string? VfxScenePath => "res://RegentFX/scenes/vfx/spectrum_shift.tscn";

	public override void BeforeBeforeApplied(Creature target, decimal amount)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(target) : null);
		if (val != null)
		{
			TaskHelper.RunSafely(PlayAnim(val.VfxSpawnPosition));
		}
	}

	private async Task PlayAnim(Vector2 pos)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		VFXUtil.PlaySimple(VfxScenePath, pos, 3f);
		FmodLite.Play("event:/RegentFx/sfx/spectrumshift");
	}
}
