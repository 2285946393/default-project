using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(SolarStrike))]
public class SolarStrike : CardFX
{
	public override HoldingModes HoldingMode => HoldingModes.None;

	public override bool UseV2Patch => true;

	public override bool HasOnBeforeDamage => true;

	public override string? VfxScenePath => "res://RegentFX/scenes/vfx/solar_strike.tscn";

	public override string? HitSfxPath => "event:/RegentFx/sfx/Solar_Strike";

	public override bool HasExposureEffect => false;

	public override async Task OnBeforeDamage(AttackCommand command)
	{
		Creature singleTarget = command._singleTarget;
		if (singleTarget != null && command._singleTarget != null && card != null)
		{
			await PlayVfx(card.Owner.Creature, singleTarget);
		}
	}

	private async Task PlayVfx(Creature owner, Creature target)
	{
		if (TestMode.IsOn)
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(owner) : null);
		NCombatRoom instance2 = NCombatRoom.Instance;
		NCreature val2 = ((instance2 != null) ? instance2.GetCreatureNode(target) : null);
		NCombatRoom instance3 = NCombatRoom.Instance;
		Control val3 = ((instance3 != null) ? instance3.CombatVfxContainer : null);
		if (val == null || val2 == null || val3 == null)
		{
			return;
		}
		try
		{
			Node2D val4 = VFXUtil.GenVFXNode(VfxScenePath);
			Node obj = ((Node)val4).FindChild("StartPos", true, true);
			Node2D val5 = (Node2D)(object)((obj is Node2D) ? obj : null);
			if (val5 == null)
			{
				Entry.Logger.Error("[SolarStrike] No StartPos found in VFX scene", 1);
				return;
			}
			int num = (VFXUtil.IsCharacterFacingRight(owner) ? 1 : (-1));
			Vector2 val6 = ((Control)val).GlobalPosition + new Vector2(-140f * (float)num, -620f) + VFXUtil.RandVec2(70f);
			Vector2 targetPos = val2.VfxSpawnPosition;
			val4.FitVFX(val5.GlobalPosition, Vector2.Zero, val6, targetPos);
			val4.GlobalPosition = targetPos;
			Vector2 val7 = val6 + new Vector2(0f, 20f);
			if (VFXUtil.RandRD(0.4f))
			{
				val4.Scale *= new Vector2(1f, -1f);
				val7 += new Vector2(0f, 50f);
			}
			VFXUtil.ReplayAllParticles(VFXUtil.PlaySimple("res://scenes/vfx/energy/regent/regent_energy_vfx_back.tscn", val7, 3f));
			GodotTreeExtensions.AddChildSafely((Node)(object)val3, (Node)(object)val4);
			Entry.StarEffectController?.OnPlayCard();
			FmodLite.Play(HitSfxPath);
			WorldEnvironmentUtil.FullExposure(1.3f, 0.1f, 0.2f, 0.1f);
			TaskHelper.RunSafely(CardVfxUtil.ClearAfter(val4, 3f));
			await VFXUtil.Wait(0.27f);
			VFXUtil.ReplayAllParticles(VFXUtil.PlaySimple("res://scenes/vfx/energy/regent/regent_energy_vfx_front.tscn", targetPos, 3f));
			NGame instance4 = NGame.Instance;
			if (instance4 != null)
			{
				instance4.ScreenShake((ShakeStrength)2, (ShakeDuration)1, -1f);
			}
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn("[SolarStrike] Error playing VFX: " + ex.Message, 1);
		}
	}
}
