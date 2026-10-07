using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Models.Cards;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(ParticleWall))]
public class ParticleWall : CardFX
{
	private List<Vector2> starPos = new List<Vector2>
	{
		new Vector2(240f, -170f),
		new Vector2(140f, -60f)
	};

	public override string? VfxScenePath => "res://RegentFX/scenes/vfx/particle_wall.tscn";

	public override int StarCount => 2;

	public override Vector2 CalculateTargetPosition(Vector2 basePosition, int index, int totalCount)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = starPos[index];
		if (!base.IsCharacterFacingRight)
		{
			val.X *= -1f;
		}
		return basePosition + val;
	}
}
