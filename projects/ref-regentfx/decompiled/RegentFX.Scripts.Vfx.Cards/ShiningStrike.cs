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

[CardFx(typeof(ShiningStrike))]
public class ShiningStrike : CardFX
{
	public override HoldingModes HoldingMode => HoldingModes.None;

	public override bool UseV2Patch => true;

	public override bool HasOnBeforeDamage => true;

	public override string? VfxScenePath => "res://RegentFX/scenes/vfx/shining_strike.tscn";

	public override string? HitSfxPath => "event:/RegentFx/sfx/shining_strike";

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
		if (val == null || val2 == null)
		{
			return;
		}
		try
		{
			Node2D val3 = VFXUtil.GenVFXNode(VfxScenePath);
			val3.Scale *= 1.8f;
			if (VFXUtil.RandRD(0.4f))
			{
				val3.Scale *= new Vector2(1f, -1f);
			}
			Vector2 val4 = ((Control)val).GlobalPosition + new Vector2(0f, -460f) + VFXUtil.RandVec2(80f);
			Vector2 vfxSpawnPosition = val2.VfxSpawnPosition;
			Vector2 val5 = vfxSpawnPosition - val4;
			float rotationDegrees = Mathf.RadToDeg(Mathf.Atan2(val5.Y, val5.X));
			val3.RotationDegrees = rotationDegrees;
			((Vector2)(ref val5)).Normalized();
			val3.GlobalPosition = vfxSpawnPosition;
			NCombatRoom instance3 = NCombatRoom.Instance;
			if (instance3 != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance3.CombatVfxContainer, (Node)(object)val3);
			}
			Entry.StarEffectController?.OnPlayCard();
			FmodLite.Play(HitSfxPath);
			WorldEnvironmentUtil.FullExposure(1.5f, 0.1f, 0.2f, 0.1f);
			TaskHelper.RunSafely(CardVfxUtil.ClearAfter(val3, 3f));
			await VFXUtil.Wait(0.15f);
			NGame instance4 = NGame.Instance;
			if (instance4 != null)
			{
				instance4.ScreenShake((ShakeStrength)2, (ShakeDuration)1, -1f);
			}
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn("[ShiningStrike] Error playing VFX: " + ex.Message, 1);
		}
	}
}
