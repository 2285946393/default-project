using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(CrescentSpear))]
public class CrescentSpear : CardFX
{
	private const float SpearLength = 900f;

	private const float ScaleFactor = 1.2f;

	private const string HitSFX = "event:/RegentFx/sfx/crescent_spear";

	public override int StarCount => 1;

	public override string? VfxScenePath => "res://RegentFX/scenes/vfx/crescent_spear.tscn";

	public override Vector2 TargetOffset => new Vector2(-200f, -350f);

	public override bool UseV2Patch => true;

	public override bool HasOnBeforeDamage => true;

	public override void OnStartHolding(Star star, int index)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		star.ChangeColorTo(new Color(14.551f, 0.683f, 9.982f, 1f));
	}

	public override async Task OnBeforeDamage(AttackCommand command)
	{
		CardModel? obj = card;
		Creature val = ((obj != null) ? obj.Owner.Creature : null);
		Creature singleTarget = command._singleTarget;
		if (val != null && singleTarget != null)
		{
			await PlayCrescentSpearVfx(val, singleTarget);
		}
	}

	private async Task PlayCrescentSpearVfx(Creature owner, Creature target)
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
			Entry.Logger.Warn("[CrescentSpear] Could not get creature nodes for VFX", 1);
			return;
		}
		try
		{
			Node2D val3 = VFXUtil.GenVFXNode(VfxScenePath);
			val3.Scale = Vector2.One * 1.2f;
			Vector2 globalPosition = ((Control)val).GlobalPosition;
			Vector2 vfxSpawnPosition = val2.VfxSpawnPosition;
			Vector2 val4 = globalPosition + new CrescentSpear().TargetOffset;
			Vector2 val5 = vfxSpawnPosition - val4;
			float rotationDegrees = Mathf.RadToDeg(Mathf.Atan2(val5.Y, val5.X));
			val3.RotationDegrees = rotationDegrees;
			Vector2 val6 = ((Vector2)(ref val5)).Normalized();
			val3.GlobalPosition = vfxSpawnPosition - val6 * 900f;
			NCombatRoom instance3 = NCombatRoom.Instance;
			if (instance3 != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance3.CombatVfxContainer, (Node)(object)val3);
			}
			Entry.StarEffectController?.OnPlayCard();
			FmodLite.Play("event:/RegentFx/sfx/crescent_spear");
			TaskHelper.RunSafely(ClearAfter(val3));
			await VFXUtil.Wait(0.15f);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn("[CrescentSpear] Error playing VFX: " + ex.Message, 1);
		}
	}

	public static async Task ClearAfter(Node2D? node)
	{
		await VFXUtil.Wait(1f);
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			GodotTreeExtensions.QueueFreeSafely((Node)(object)node);
		}
	}
}
