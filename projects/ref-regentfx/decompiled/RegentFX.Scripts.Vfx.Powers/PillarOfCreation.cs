using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace RegentFX.Scripts.Vfx.Powers;

[PowerFx(typeof(PillarOfCreationPower))]
[HarmonyPatch]
public class PillarOfCreation : PowerFX
{
	public override string? VfxScenePath => "res://RegentFX/scenes/vfx/pillar.tscn";

	public override void BeforeBeforeApplied(Creature target, decimal amount)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(target) : null);
		if (val != null)
		{
			int num = (VFXUtil.IsCharacterFacingRight(target) ? 1 : (-1));
			Vector2 position = ((Control)val).GlobalPosition + new Vector2(-130f * (float)num, -50f);
			Pillar pillar = Pillar.Spawn(target, position);
			if (pillar != null)
			{
				SetSize(pillar, amount);
			}
		}
	}

	private static void SetSize(Pillar PillarOfCreationNode, decimal amount)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Min(1.3f, ((float)amount - 3f) * 0.04f + 1f);
		((Node2D)PillarOfCreationNode).Scale = Vector2.One * num;
	}

	public override void AfterAfterRemoved(Creature target)
	{
		if (Pillar.Pillars.TryGetValue(target, out Pillar value))
		{
			GodotTreeExtensions.QueueFreeSafely((Node)(object)value);
			Pillar.Pillars.Remove(target);
		}
	}

	public override void AfterSetAmount(decimal amount)
	{
		PowerModel? obj = power;
		Creature val = ((obj != null) ? obj.Owner : null);
		if (val != null && Pillar.Pillars.TryGetValue(val, out Pillar value))
		{
			SetSize(value, amount);
		}
	}
}
