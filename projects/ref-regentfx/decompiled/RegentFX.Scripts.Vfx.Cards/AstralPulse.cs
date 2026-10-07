using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(AstralPulse))]
public class AstralPulse : CardFX
{
	private readonly List<Vector2> starPos;

	public override int StarCount => 3;

	public override string? VfxScenePath => "res://RegentFX/scenes/vfx/astral_pulse.tscn";

	public override Vector2 TargetOffset => new Vector2(0f, -180f);

	public override float VfxClearDelay => 3f;

	public override bool HasExposureEffect => true;

	public override float ExposurePeak => 1.3f;

	public override float ExposureInDuration => 0.2f;

	public override float ExposureOutDuration => 0.3f;

	public override bool UseV2Patch => true;

	public override bool HasOnBeforeExecute => true;

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
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		star.PulseMinScale *= 2f;
		star.PulseMaxScale *= 1.8f;
		star.PulseSpeed *= 3f;
		star.ChangeColorTo(new Color(14.551f, 0.683f, 9.982f, 1f));
	}

	public override async Task OnBeforeExecute()
	{
		if (card == null)
		{
			return;
		}
		Creature creature = card.Owner.Creature;
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature ownerNode = ((instance != null) ? instance.GetCreatureNode(creature) : null);
		if (ownerNode == null)
		{
			Entry.Logger.Info("Could not get creature nodes for VFX", 1);
		}
		else
		{
			FmodLite.Play("event:/RegentFx/sfx/common_magic_1");
			VFXUtil.ShakeAfter(0.03f, (ShakeStrength)4, (ShakeDuration)2);
			Node2D val = VFXUtil.PlaySimple(VfxScenePath, ownerNode.VfxSpawnPosition);
			if (val != null)
			{
				val.Scale *= 1.3f;
			}
			WorldEnvironmentUtil.TweenExposure(2.8f, 0.05f, (EaseType)2, (TransitionType)7);
			await VFXUtil.Wait(0.15f);
			VFXUtil.ReplayAllParticles(VFXUtil.PlaySimple("res://RegentFX/scenes/vfx/distortions/vfx_outward_screen_distortion_ellipse.tscn", ownerNode.VfxSpawnPosition));
			WorldEnvironmentUtil.TweenExposure(1f, 0.44f, (EaseType)2, (TransitionType)7);
		}
		Entry.StarEffectController?.OnPlayCard();
	}

	public AstralPulse()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		int num = 3;
		List<Vector2> list = new List<Vector2>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<Vector2> span = CollectionsMarshal.AsSpan(list);
		span[0] = new Vector2(0f, 0f);
		span[1] = new Vector2(3f, -5f);
		span[2] = new Vector2(-4f, -2f);
		starPos = list;
		base._002Ector();
	}
}
