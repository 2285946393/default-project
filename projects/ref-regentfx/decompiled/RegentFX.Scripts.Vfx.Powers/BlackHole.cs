using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace RegentFX.Scripts.Vfx.Powers;

[PowerFx(typeof(BlackHolePower))]
[HarmonyPatch]
public class BlackHole : PowerFX
{
	public override string? VfxScenePath => Blackhole.VfxScenePath;

	public override void BeforeBeforeApplied(Creature target, decimal amount)
	{
		Blackhole blackhole = Blackhole.Create(target);
		if (blackhole != null)
		{
			SetSize(blackhole, amount);
		}
	}

	private static void SetSize(Blackhole blackHoleNode, decimal amount)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Min(1.6f, ((float)amount - 3f) * 0.05f + 1f);
		((Node2D)blackHoleNode).Scale = Vector2.One * num;
	}

	public override void AfterAfterRemoved(Creature target)
	{
		if (Blackhole.Blackholes.TryGetValue(target, out Blackhole value))
		{
			GodotTreeExtensions.QueueFreeSafely((Node)(object)value);
			Blackhole.Blackholes.Remove(target);
		}
	}

	public override void AfterSetAmount(decimal amount)
	{
		PowerModel? obj = power;
		Creature val = ((obj != null) ? obj.Owner : null);
		if (val != null && Blackhole.Blackholes.TryGetValue(val, out Blackhole value))
		{
			SetSize(value, amount);
		}
	}
}
