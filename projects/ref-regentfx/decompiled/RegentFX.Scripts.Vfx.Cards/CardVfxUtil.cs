using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using RegentFX.ThirdParty.Audio;

namespace RegentFX.Scripts.Vfx.Cards;

public static class CardVfxUtil
{
	public static async Task PlayTargetedVfx(CardFX config, Creature owner, Creature target, string logTag)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(owner) : null);
		NCombatRoom instance2 = NCombatRoom.Instance;
		NCreature val2 = ((instance2 != null) ? instance2.GetCreatureNode(target) : null);
		if (val == null || val2 == null)
		{
			Entry.Logger.Warn("[" + logTag + "] Could not get creature nodes for VFX", 1);
			return;
		}
		Vector2 startPos = ((Control)val).GlobalPosition + config.TargetOffset;
		Vector2 vfxSpawnPosition = val2.VfxSpawnPosition;
		await PlayVfxInternal(config, startPos, vfxSpawnPosition, logTag);
	}

	public static async Task PlayAoeVfx(CardFX config, Creature owner, CardModel card, string logTag)
	{
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(owner) : null);
		if (val == null)
		{
			Entry.Logger.Warn("[" + logTag + "] Could not get owner creature node for VFX", 1);
			return;
		}
		Vector2 startPos = ((Control)val).GlobalPosition + config.TargetOffset;
		Vector2? combatSidePos = VFXUtil.GetCombatSidePos(card);
		if (combatSidePos.HasValue)
		{
			await PlayVfxInternal(config, startPos, combatSidePos.Value, logTag);
		}
	}

	private static async Task PlayVfxInternal(CardFX config, Vector2 startPos, Vector2 targetPos, string logTag)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (TestMode.IsOn)
		{
			return;
		}
		string vfxScenePath = config.VfxScenePath;
		if (string.IsNullOrEmpty(vfxScenePath))
		{
			Entry.Logger.Warn("[" + logTag + "] No VfxScenePath configured", 1);
			return;
		}
		try
		{
			Node2D val = VFXUtil.GenVFXNode(vfxScenePath);
			Node obj = ((Node)val).FindChild("StartPos", true, true);
			Node2D val2 = (Node2D)(object)((obj is Node2D) ? obj : null);
			if (val2 == null)
			{
				Entry.Logger.Error("[" + logTag + "] No StartPos found in VFX scene", 1);
				return;
			}
			val.FitVFX(val2.GlobalPosition, Vector2.Zero, startPos, targetPos);
			val.GlobalPosition = targetPos;
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)val);
			}
			if (!string.IsNullOrEmpty(config.HitSfxPath))
			{
				try
				{
					FmodLite.Play(config.HitSfxPath);
				}
				catch (Exception ex)
				{
					Entry.Logger.Warn("[CardVfxUtil] FMOD 播放失败: " + config.HitSfxPath + ". 错误: " + ex.Message, 1);
				}
			}
			TaskHelper.RunSafely(ClearAfter(val, config.VfxClearDelay));
			if (!config.HasExposureEffect)
			{
				await VFXUtil.Wait(0.15f);
			}
			else
			{
				try
				{
					WorldEnvironmentUtil.TweenExposure(config.ExposurePeak, config.ExposureInDuration, (EaseType)2, (TransitionType)7);
					await VFXUtil.Wait(config.ExposureInDuration + 0.05f);
				}
				finally
				{
					WorldEnvironmentUtil.TweenExposure(1f, config.ExposureOutDuration, (EaseType)2, (TransitionType)7);
				}
			}
			if (string.IsNullOrEmpty(config.SecondarySfxPath))
			{
				return;
			}
			try
			{
				FmodLite.Play(config.SecondarySfxPath);
			}
			catch (Exception ex2)
			{
				Entry.Logger.Warn("[CardVfxUtil] FMOD 播放失败: " + config.SecondarySfxPath + ". 错误: " + ex2.Message, 1);
			}
		}
		catch (Exception ex3)
		{
			Entry.Logger.Warn("[" + logTag + "] Error playing VFX: " + ex3.Message, 1);
		}
	}

	public static async Task ClearAfter(Node2D? node, float delay)
	{
		await VFXUtil.Wait(delay);
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			GodotTreeExtensions.QueueFreeSafely((Node)(object)node);
		}
	}
}
