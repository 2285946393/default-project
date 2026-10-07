using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

[CardFx(typeof(LunarBlast))]
public class LunarBlast : CardFX
{
	private static List<string> LunarScenePaths = new List<string> { "res://RegentFX/scenes/vfx/lunar_blast_1.tscn", "res://RegentFX/scenes/vfx/lunar_blast_2.tscn" };

	public override HoldingModes HoldingMode => HoldingModes.Custom;

	public override Vector2 TargetOffset => new Vector2(100f, -450f);

	public override List<string> AssetPaths => new List<string> { VfxScenePath }.Concat(LunarScenePaths).ToList();

	public override string VfxScenePath => "res://RegentFX/scenes/vfx/laser_1.tscn";

	public static string LunarScenePath => LunarScenePaths[GD.RandRange(0, LunarScenePaths.Count - 1)];

	public override bool HasExposureEffect => false;

	public override string HitSfxPath => "event:/RegentFx/sfx/lunarTest1";

	public string HitSfxPath2 => "event:/RegentFx/sfx/lunarTest1";

	public override bool UseV2Patch => true;

	public override bool HasOnBeforeDamage => true;

	private Vector2 GeneratePosAt()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		return TargetOffset + VFXUtil.RandVec2(100f);
	}

	public override void HoldingCustom()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		CardModel? obj = card;
		LunarBlast val = (LunarBlast)(object)((obj is LunarBlast) ? obj : null);
		if (val == null)
		{
			return;
		}
		int num = (int)((CalculatedVar)((CardModel)val).DynamicVars["CalculatedHits"]).Calculate((Creature)null);
		Creature creature = card.Owner.Creature;
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val2 = ((instance != null) ? instance.GetCreatureNode(creature) : null);
		if (Entry.StarEffectController != null && val2 != null && num > 0)
		{
			num = Mathf.Min(20, num);
			for (int i = 0; i < num; i++)
			{
				Vector2 position = GeneratePosAt();
				Entry.StarEffectController.GenerateStarAt(position, (Color?)new Color(8.4f, 8.9f, 9f, 1f));
			}
			Entry.StarEffectController.StartShaking();
			TryPlayHoldingSfx();
		}
	}

	public override async Task OnBeforeDamage(AttackCommand command)
	{
		CardModel? obj = card;
		Creature val = ((obj != null) ? obj.Owner.Creature : null);
		Creature singleTarget = command._singleTarget;
		if (val == null || singleTarget == null || command._singleTarget == null)
		{
			return;
		}
		Vector2? val2 = Entry.StarEffectController?.PopStar(this);
		if (!val2.HasValue)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature obj2 = ((instance != null) ? instance.GetCreatureNode(val) : null);
			Vector2 val3 = ((obj2 != null) ? obj2.VfxSpawnPosition : Vector2.Zero);
			val2 = val3 + GeneratePosAt();
		}
		if (!TestMode.IsOn)
		{
			NCombatRoom instance2 = NCombatRoom.Instance;
			NCreature val4 = ((instance2 != null) ? instance2.GetCreatureNode(singleTarget) : null);
			if (val4 != null)
			{
				Vector2 vfxSpawnPosition = val4.VfxSpawnPosition;
				WorldEnvironmentUtil.FullExposure(1.5f, 0f, 0.05f, 0.05f);
				await Task.WhenAll(PlayLunarVfx(vfxSpawnPosition, val2.Value), PlayLaserVfx(vfxSpawnPosition, val2.Value));
			}
		}
	}

	private async Task PlayLaserVfx(Vector2 targetPos, Vector2 starPos)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		string vfxScenePath = VfxScenePath;
		try
		{
			Node2D val = VFXUtil.GenVFXNode(vfxScenePath);
			Node obj = ((Node)val).FindChild("StartPos", true, true);
			Node2D val2 = (Node2D)(object)((obj is Node2D) ? obj : null);
			if (val2 == null)
			{
				Entry.Logger.Error("[Laser] No StartPos found in VFX scene", 1);
				return;
			}
			val.FitVFX(val2.GlobalPosition, Vector2.Zero, starPos, targetPos);
			val.GlobalPosition = targetPos;
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)val);
			}
			FmodLite.Play(HitSfxPath);
			FmodLite.Play(HitSfxPath2);
			TaskHelper.RunSafely(CardVfxUtil.ClearAfter(val, VfxClearDelay));
			await VFXUtil.Wait(0.15f);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn("[Laser] Error playing VFX: " + ex.Message, 1);
		}
	}

	private async Task PlayLunarVfx(Vector2 targetPos, Vector2 starPos)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Node2D val = VFXUtil.GenVFXNode(LunarScenePath);
			if (val != null)
			{
				Vector2 val2 = targetPos - starPos;
				float rotationDegrees = Mathf.RadToDeg(Mathf.Atan2(val2.Y, val2.X));
				val.RotationDegrees = rotationDegrees;
				((Vector2)(ref val2)).Normalized();
				val.GlobalPosition = starPos;
				NCombatRoom instance = NCombatRoom.Instance;
				if (instance != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)val);
				}
				TaskHelper.RunSafely(CardVfxUtil.ClearAfter(val, 2f));
				await VFXUtil.Wait(0.15f);
			}
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn("[CrescentSpear] Error playing VFX: " + ex.Message, 1);
		}
	}
}
