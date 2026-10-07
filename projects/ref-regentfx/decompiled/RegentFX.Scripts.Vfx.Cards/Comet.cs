using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(Comet))]
public class Comet : CardFX
{
	public override int StarCount => 5;

	public override Vector2 TargetOffset => new Vector2(-250f, -520f);

	public override string VfxScenePath => "res://RegentFX/scenes/vfx/comet.tscn";

	public override string HitSfxPath => "event:/RegentFx/sfx/Comet";

	public override bool HasExposureEffect => false;

	public override bool RemoveHitFx => true;

	public override bool UseV2Patch => true;

	public override bool HasOnBeforeDamage => true;

	public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return basePosition + TargetOffset + VFXUtil.RandVec2(14f);
	}

	public override async Task OnBeforeDamage(AttackCommand command)
	{
		CardModel? obj = card;
		Creature obj2 = ((obj != null) ? obj.Owner.Creature : null);
		Creature singleTarget = command._singleTarget;
		if (obj2 != null && singleTarget != null && command._singleTarget != null)
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
		NCreature obj = ((instance != null) ? instance.GetCreatureNode(owner) : null);
		NCombatRoom instance2 = NCombatRoom.Instance;
		NCreature val = ((instance2 != null) ? instance2.GetCreatureNode(target) : null);
		if (obj == null || val == null)
		{
			return;
		}
		try
		{
			Node2D val2 = VFXUtil.GenVFXNode(VfxScenePath);
			val2.Scale *= 1.26f;
			if (!VFXUtil.IsCharacterFacingRight(owner))
			{
				val2.Scale *= new Vector2(-1f, 1f);
			}
			Vector2 globalPosition = (val.VfxSpawnPosition + ((Control)val).GlobalPosition * 2f) / 3f;
			val2.GlobalPosition = globalPosition;
			NCombatRoom instance3 = NCombatRoom.Instance;
			if (instance3 != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance3.CombatVfxContainer, (Node)(object)val2);
			}
			Entry.StarEffectController?.OnPlayCard();
			FmodLite.Play(HitSfxPath);
			WorldEnvironmentUtil.FullExposure(1.3f, 0.3f, 0.3f, 0.1f);
			TaskHelper.RunSafely(CardVfxUtil.ClearAfter(val2, 3f));
			await VFXUtil.Wait(0.6f);
			NGame instance4 = NGame.Instance;
			if (instance4 != null)
			{
				instance4.ScreenShake((ShakeStrength)3, (ShakeDuration)2, -1f);
			}
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn("[Comet] Error playing VFX: " + ex.Message, 1);
		}
	}
}
