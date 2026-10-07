using System;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using RegentFX.Scripts.Vfx;
using RegentFX.Scripts.Vfx.Cards;

namespace RegentFX.Scripts.Patches;

[HarmonyPatch]
public static class CardAnimPatch
{
	private static bool _isProcessing;

	[HarmonyPatch(typeof(AttackCommand), "Execute")]
	[HarmonyPrefix]
	public static bool ExecutePatch(AttackCommand __instance, PlayerChoiceContext? choiceContext, ref Task<AttackCommand> __result)
	{
		if (_isProcessing)
		{
			return true;
		}
		AttackVfxContext.ShouldDisableRegentWeaponAttack = false;
		AttackVfxContext.ShouldDisableRegentWeaponSFX = false;
		AttackVfxContext.CurrentAttackCommand.Value = __instance;
		if (__instance.ModelSource == null)
		{
			return true;
		}
		try
		{
			CardModel val2 = (AttackVfxContext.CurrentModelSource = (CardModel?)/*isinst with value type is only supported in some contexts*/);
			CardFX cardFX = CardFX.FromCard(val2);
			if (cardFX == null)
			{
				return true;
			}
			if (!cardFX.UseV2Patch)
			{
				return true;
			}
			if (!LocalContext.IsMe(val2.Owner))
			{
				return true;
			}
			if (cardFX.ShouldDisableRegentWeaponAttack)
			{
				AttackVfxContext.ShouldDisableRegentWeaponAttack = true;
			}
			if (cardFX.ShouldDisableRegentWeaponSFX)
			{
				AttackVfxContext.ShouldDisableRegentWeaponSFX = true;
			}
			if (cardFX.ChangeHitFx != null)
			{
				__instance.WithHitFx(cardFX.ChangeHitFx, (string)null, (string)null);
			}
			if (cardFX.RemoveHitFx)
			{
				__instance.WithHitFx((string)null, (string)null, (string)null);
			}
			if (cardFX.HasOnBeforeExecute)
			{
				__result = RunCustomFlow(cardFX, val2, __instance, choiceContext);
				return false;
			}
		}
		catch (InvalidCastException)
		{
		}
		return true;
	}

	private static async Task<AttackCommand> RunCustomFlow(CardFX cardFX, CardModel card, AttackCommand instance, PlayerChoiceContext? choiceContext)
	{
		_isProcessing = true;
		try
		{
			await BeforeExecute(cardFX, card, instance);
			return await instance.Execute(choiceContext);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private static async Task BeforeExecute(CardFX cardFX, CardModel card, AttackCommand command)
	{
		await cardFX.OnBeforeExecute();
	}

	[HarmonyPatch(typeof(CardModel), "OnPlayWrapper")]
	[HarmonyPostfix]
	public static void PostOnPlayPatch(CardModel __instance, ref Task __result)
	{
		__result = AsyncPostOnPlayPatch(__result, __instance);
	}

	private static async Task AsyncPostOnPlayPatch(Task originalTask, CardModel card)
	{
		try
		{
			await originalTask;
		}
		finally
		{
			Entry.StarEffectController?.OnCardPlayed(card);
			AttackVfxContext.ShouldDisableRegentWeaponAttack = false;
			AttackVfxContext.ShouldDisableRegentWeaponSFX = false;
			AttackVfxContext.CurrentModelSource = null;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(NRegentVfx), "Attack")]
	private static bool PreventRegentAnimPatch()
	{
		if (AttackVfxContext.ShouldDisableRegentWeaponAttack)
		{
			return false;
		}
		return true;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(SfxCmd), "Play", new Type[]
	{
		typeof(string),
		typeof(float)
	})]
	private static bool PreventRegentSfx(string sfx, float volume)
	{
		if (AttackVfxContext.ShouldDisableRegentWeaponSFX && !Setting.DisableModSounds && sfx == "event:/sfx/characters/regent/regent_attack")
		{
			return false;
		}
		return true;
	}
}
