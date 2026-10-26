using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.Telemetry;
using STS2RitsuLib.Utils.HarmonyIl;

namespace RandomForeseer.RandomForeseerCode.InCombat.Patches;

[HarmonyPatch(typeof(NCombatCardPile), "OnRelease")]
internal static class FrozenEyeEmptyDrawPileOpenPatch
{
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		List<CodeInstruction> list = instructions.ToList();
		try
		{
			HarmonyIlRewriter obj = HarmonyIlRewriter.From((IEnumerable<CodeInstruction>)list, original);
			MethodInfo methodInfo = AccessTools.PropertyGetter(typeof(CardPile), "IsEmpty");
			MethodInfo toMethod = AccessTools.Method(typeof(FrozenEyeEmptyDrawPileOpenPatch), "ShouldTreatPileAsEmpty", (Type[])null, (Type[])null);
			HarmonyIlRewriteReport val = obj.ReplaceCall("allow empty draw pile view when shuffle prediction can be shown", methodInfo, toMethod, (Func<IReadOnlyList<CodeInstruction>, bool>)((IReadOnlyList<CodeInstruction> code) => code.Any(HarmonyIl.IsCall(toMethod))));
			((HarmonyIlRewriteReport)(ref val)).RequireExactSitesOrAlreadySatisfied(1);
			return obj.InstructionsChecked("Frozen Eye empty draw pile open");
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Frozen Eye empty draw pile transpiler failed for {GeneralExtensions.FullDescription(original)}: {ex}", 1);
			ModTelemetry.CaptureException(ex, "frozen_eye_patch", "rewrite_empty_draw_pile_open", TelemetryContext.ForMethod(original));
			return list;
		}
	}

	private static bool ShouldTreatPileAsEmpty(CardPile pile)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		if (!pile.IsEmpty)
		{
			return false;
		}
		ModSettings settings = ModData.Settings;
		if (ModSettingsExtensions.get_IsPredictionEnabled(settings) && settings.FrozenEyeEnabled && settings.ShufflePredictionEnabled && CardPileUtils.TryGetDrawPileOwner(pile, out Player player))
		{
			ICombatState combatState = player.Creature.CombatState;
			if (((combatState != null) ? new CombatSide?(combatState.CurrentSide) : null) == (CombatSide?)player.Creature.Side)
			{
				PlayerCombatState playerCombatState = player.PlayerCombatState;
				if (playerCombatState == null)
				{
					return true;
				}
				return playerCombatState.DiscardPile.IsEmpty;
			}
		}
		return true;
	}
}
