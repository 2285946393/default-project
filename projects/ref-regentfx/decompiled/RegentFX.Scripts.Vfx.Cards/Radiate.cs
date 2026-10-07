using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(Radiate))]
public class Radiate : CardFX
{
	public const string ConvergeScenePath = "res://RegentFX/scenes/vfx/radiate_converge.tscn";

	public const string PulseScenePath = "res://RegentFX/scenes/vfx/radiate_outward_pulse.tscn";

	private const float ConvergeDuration = 0.23f;

	private const float PulseLifetime = 0.28f;

	private const float PulsePositionJitter = 12f;

	private const float PulseRotationJitterDegrees = 6f;

	public override HoldingModes HoldingMode => HoldingModes.None;

	public override string VfxScenePath => "res://RegentFX/scenes/vfx/radiate_converge.tscn";

	public override List<string> AssetPaths
	{
		get
		{
			int num = 2;
			List<string> list = new List<string>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<string> span = CollectionsMarshal.AsSpan(list);
			span[0] = "res://RegentFX/scenes/vfx/radiate_converge.tscn";
			span[1] = "res://RegentFX/scenes/vfx/radiate_outward_pulse.tscn";
			return list;
		}
	}

	public override bool UseV2Patch => true;

	public override bool HasOnBeforeExecute => true;

	public override bool HasOnBeforeDamageTargeted => true;

	public override async Task OnBeforeExecute()
	{
		if (TestMode.IsOn || card == null || NCombatRoom.Instance == null || card.Owner.Creature.IsDead || GetCalculatedHits() <= 0)
		{
			return;
		}
		NCreature ownerNode = NCombatRoom.Instance.GetCreatureNode(card.Owner.Creature);
		if (ownerNode == null)
		{
			Entry.Logger.Warn("[Radiate] Could not get owner creature node for converge VFX", 1);
			return;
		}
		FmodLite.Play("event:/RegentFx/sfx/common_hold_3");
		VFXUtil.PlaySimple("res://RegentFX/scenes/vfx/radiate_converge.tscn", ownerNode.VfxSpawnPosition, 0.34f);
		await VFXUtil.Wait(0.23f);
		if (NCombatRoom.Instance != null && GodotObject.IsInstanceValid((GodotObject)(object)ownerNode))
		{
			Node2D val = VFXUtil.PlaySimple("res://scenes/vfx/energy/regent/regent_energy_vfx_back.tscn", ownerNode.VfxSpawnPosition, 3f);
			if (val != null)
			{
				VFXUtil.ActivateScaleAllParticles(val, 3f);
				VFXUtil.ReplayAllParticles(val);
			}
		}
	}

	public override Task OnBeforeDamage(AttackCommand command, IReadOnlyList<Creature> targets)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		if (TestMode.IsOn || card == null || NCombatRoom.Instance == null)
		{
			return Task.CompletedTask;
		}
		Creature creature = card.Owner.Creature;
		if (creature.IsDead)
		{
			return Task.CompletedTask;
		}
		NCreature creatureNode = NCombatRoom.Instance.GetCreatureNode(creature);
		if (creatureNode == null)
		{
			Entry.Logger.Warn("[Radiate] Could not get owner creature node for pulse VFX", 1);
			return Task.CompletedTask;
		}
		Vector2 position = creatureNode.VfxSpawnPosition + VFXUtil.RandVec2(12f);
		FmodLite.Play("event:/RegentFx/sfx/genesis_2");
		Node2D val = VFXUtil.PlaySimple("res://RegentFX/scenes/vfx/radiate_outward_pulse.tscn", position, 0.28f);
		if (val != null)
		{
			val.RotationDegrees = (float)GD.RandRange(-6.0, 6.0);
			RandomizePulseShader(val);
			VFXUtil.ReplayAllParticles(val);
		}
		Node2D? node = VFXUtil.PlaySimple("res://scenes/vfx/energy/regent/regent_energy_vfx_back.tscn", creatureNode.VfxSpawnPosition, 3f);
		VFXUtil.ActivateScaleAllParticles(node, 2f);
		VFXUtil.ReplayAllParticles(node);
		NGame instance = NGame.Instance;
		if (instance != null)
		{
			instance.ScreenShakeTrauma((ShakeStrength)1);
		}
		return Task.CompletedTask;
	}

	private static void RandomizePulseShader(Node2D pulse)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		GpuParticles2D nodeOrNull = ((Node)pulse).GetNodeOrNull<GpuParticles2D>(NodePath.op_Implicit("DistortionRing"));
		Material obj = ((nodeOrNull != null) ? ((CanvasItem)nodeOrNull).Material : null);
		Material obj2 = ((obj is ShaderMaterial) ? obj : null);
		Resource obj3 = ((obj2 != null) ? ((Resource)obj2).Duplicate(false) : null);
		ShaderMaterial val = (ShaderMaterial)(object)((obj3 is ShaderMaterial) ? obj3 : null);
		if (nodeOrNull != null && val != null)
		{
			val.SetShaderParameter(StringName.op_Implicit("noise_seed"), Variant.op_Implicit((float)GD.RandRange(0.0, 1000.0)));
			val.SetShaderParameter(StringName.op_Implicit("distortion_strength"), Variant.op_Implicit((float)GD.RandRange(0.014, 0.021)));
			val.SetShaderParameter(StringName.op_Implicit("swirl_strength"), Variant.op_Implicit((float)GD.RandRange(-0.012, 0.012)));
			val.SetShaderParameter(StringName.op_Implicit("chromatic_aberration"), Variant.op_Implicit((float)GD.RandRange(0.0025, 0.0045)));
			((CanvasItem)nodeOrNull).Material = (Material)(object)val;
		}
	}

	private int GetCalculatedHits()
	{
		CardModel? obj = card;
		DynamicVar obj2 = ((obj != null) ? obj.DynamicVars["CalculatedHits"] : null);
		CalculatedVar val = (CalculatedVar)(object)((obj2 is CalculatedVar) ? obj2 : null);
		if (val == null)
		{
			return 0;
		}
		return (int)val.Calculate((Creature)null);
	}
}
