using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(Alignment))]
public class Alignment : CardFX
{
	private List<Vector2> starPos = new List<Vector2>
	{
		new Vector2(-90f, -320f),
		new Vector2(0f, -320f),
		new Vector2(80f, -320f)
	};

	public override int StarCount => ((CardModel)ModelDb.Card<Alignment>()).CanonicalStarCost;

	public override string? VfxScenePath => "res://RegentFX/scenes/vfx/alignment.tscn";

	public override void OnStartHolding(Star star, int index)
	{
		Star star2 = star;
		if (Entry.StarEffectController != null)
		{
			Star star3 = Entry.StarEffectController.Stars.FindLast((Star star1) => star2 != star1);
			if (star3 != null)
			{
				star2.ConnectTo(star3);
			}
			switch (index)
			{
			case 0:
				star2.PulseMaxScale = 2.3f;
				star2.PulseMinScale = 2.1f;
				break;
			case 1:
				star2.PulseMaxScale = 1.6f;
				star2.PulseMinScale = 1.4f;
				break;
			case 2:
				star2.PulseMaxScale = 0.8f;
				star2.PulseMinScale = 0.6f;
				break;
			}
		}
	}

	public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		return basePosition + starPos[index] * 1.2f;
	}
}
