using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Models.Cards;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(Resonance))]
public class Resonance : CardFX
{
	private static readonly List<Star> _borrowedStars = new List<Star>();

	public override int StarCount => 3;

	public override HoldingModes HoldingMode => HoldingModes.Custom;

	public override string? VfxScenePath => "res://RegentFX/scenes/vfx/resonance.tscn";

	public override void HoldingCustom()
	{
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		_borrowedStars.Clear();
		StarRingController starRingController = Entry.StarRingController;
		if (starRingController == null)
		{
			return;
		}
		List<StarRingController.StarData> list = starRingController.OrbitStars.Take(StarCount).ToList();
		if (list.Count < StarCount)
		{
			return;
		}
		foreach (StarRingController.StarData item in list)
		{
			_borrowedStars.Add(item.Star);
			item.Star.ToggleTrail(trail: true);
			item.Star.ChangeColorImmediate(new Color(14.551f, 14.551f, 0f, 1f));
			TweenRadius(item, 1.5f, 0.15f);
		}
		TryPlayHoldingSfx();
	}

	public static void PlayStarVfx()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		foreach (Star borrowedStar in _borrowedStars)
		{
			if (borrowedStar != null && GodotObject.IsInstanceValid((GodotObject)(object)borrowedStar))
			{
				VFXUtil.PlaySpecialStarAt(((Node2D)borrowedStar).GlobalPosition);
			}
		}
		_borrowedStars.Clear();
		ResetOrbit();
	}

	private static void ResetOrbit()
	{
		StarRingController starRingController = Entry.StarRingController;
		if (starRingController == null)
		{
			return;
		}
		foreach (StarRingController.StarData orbitStar in starRingController.OrbitStars)
		{
			orbitStar.Star.ToggleTrail(trail: false);
			orbitStar.Star.ResetColor();
			if (Math.Abs(orbitStar.RadiusMultiplier - 1f) > 0.01f)
			{
				TweenRadius(orbitStar, 1f, 0.15f);
			}
		}
	}

	public override void OnCancel()
	{
		ResetOrbit();
		_borrowedStars.Clear();
	}

	private static void TweenRadius(StarRingController.StarData starDatum, float targetRadius, float duration)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		StarRingController.StarData starDatum2 = starDatum;
		Tween? radiusTween = starDatum2.RadiusTween;
		if (radiusTween != null)
		{
			radiusTween.Kill();
		}
		Tween val = ((Node)starDatum2.Star).CreateTween();
		starDatum2.RadiusTween = val;
		val.SetTrans((TransitionType)5);
		val.SetEase((EaseType)1);
		val.TweenMethod(Callable.From<float>((Action<float>)delegate(float v)
		{
			starDatum2.RadiusMultiplier = v;
		}), Variant.op_Implicit(starDatum2.RadiusMultiplier), Variant.op_Implicit(targetRadius), (double)duration);
	}
}
