using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(SevenStars))]
public class SevenStars : CardFX
{
	private readonly List<Vector2> starPos = new List<Vector2>
	{
		new Vector2(5f, 0f),
		new Vector2(-47f, -5f),
		new Vector2(-55f, 54f),
		new Vector2(0f, 50f),
		new Vector2(50f, 60f),
		new Vector2(100f, 70f),
		new Vector2(150f, 85f)
	};

	public override int StarCount => 7;

	public override string HoldSfxPath => "event:/RegentFx/sfx/seven_stars_hold";

	public override Vector2 TargetOffset => new Vector2(-100f, -450f);

	public override bool ShouldDisableRegentWeaponSFX => false;

	public override bool UseV2Patch => true;

	public override bool HasOnBeforeDamage => true;

	public override void OnStartHolding(Star star, int index)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		Star star2 = star;
		if (Entry.StarEffectController != null)
		{
			Star star3 = Entry.StarEffectController.Stars.FindLast((Star star1) => star2 != star1);
			if (star3 != null)
			{
				star2.ConnectTo(star3);
				return;
			}
			star2.ChangeColorTo(new Color(14.551f, 14.551f, 0f, 1f));
			star2.PulseSpeed = 2.4f;
			star2.PulseMaxScale *= 1.3f;
			star2.PulseMinScale *= 1.4f;
		}
	}

	public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		return basePosition + TargetOffset + starPos[index] * 1.2f;
	}

	private async Task PlayVfx(IReadOnlyList<Creature> enemies)
	{
		if (TestMode.IsOn)
		{
			return;
		}
		Entry.StarEffectController?.PopStar(this);
		foreach (Creature enemy in enemies)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature val = ((instance != null) ? instance.GetCreatureNode(enemy) : null);
			if (val != null)
			{
				Blade.PlayBlade(val.VfxSpawnPosition);
			}
		}
		await VFXUtil.Wait(0.05f);
	}

	public override async Task OnBeforeDamage(AttackCommand command)
	{
		IReadOnlyList<Creature> hittableEnemiesFromCard = VFXUtil.GetHittableEnemiesFromCard(card);
		if (hittableEnemiesFromCard != null)
		{
			await PlayVfx(hittableEnemiesFromCard);
		}
	}
}
