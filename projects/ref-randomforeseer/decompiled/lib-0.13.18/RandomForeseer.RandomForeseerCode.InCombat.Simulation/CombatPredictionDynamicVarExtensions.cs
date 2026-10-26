using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Telemetry;
using STS2RitsuLib.Cards.DynamicVars;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal static class CombatPredictionDynamicVarExtensions
{
	private delegate DynamicVar GetDynamicVarDelegate(CalculatedVar calculatedVar);

	private static readonly GetDynamicVarDelegate GetBaseVar = AccessTools.Method(typeof(CalculatedVar), "GetBaseVar", (Type[])null, (Type[])null).CreateDelegate<GetDynamicVarDelegate>();

	private static readonly GetDynamicVarDelegate GetExtraVar = AccessTools.Method(typeof(CalculatedVar), "GetExtraVar", (Type[])null, (Type[])null).CreateDelegate<GetDynamicVarDelegate>();

	public static decimal InvokeCalculate(this DynamicVar dynamicVar, CombatPredictionSimulator simulator, PredictedCard card, Creature? target)
	{
		return ((CalculatedVar)(object)((dynamicVar is CalculatedVar) ? dynamicVar : null))?.InvokeCalculate(simulator, card, target) ?? ((IComputedDynamicVar)(object)((dynamicVar is IComputedDynamicVar) ? dynamicVar : null))?.InvokeCalculate(simulator, card, target) ?? dynamicVar.BaseValue;
	}

	public static decimal InvokeCalculate(this CalculatedVar calculatedVar, CombatPredictionSimulator simulator, PredictedCard card, Creature? target)
	{
		Func<CardModel, Creature, decimal> func = calculatedVar._multiplierCalc ?? throw new InvalidOperationException("CalculatedVar simulation requires a multiplier calculation function.");
		using (simulator.PushActionSource((AbstractModel)(object)card.Original, PredictionActionKind.DynamicVariableCalculation))
		{
			simulator.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
			try
			{
				decimal num = func(card.Preview, target);
				DynamicVar obj = GetBaseVar(calculatedVar);
				DynamicVar val = GetExtraVar(calculatedVar);
				return obj.BaseValue + val.BaseValue * num;
			}
			catch (Exception ex)
			{
				Entry.Logger.Warn($"CalculatedVar simulation failed: {ex}", 1);
				ModTelemetry.CaptureException(ex, "combat_dynamic_var_prediction", "calculate", GetTelemetryContext(card, (DynamicVar)(object)calculatedVar, target));
				return 0m;
			}
		}
	}

	public static decimal InvokeCalculate(this IComputedDynamicVar computedDynamicVar, CombatPredictionSimulator simulator, PredictedCard card, Creature? target)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		using (simulator.PushActionSource((AbstractModel)(object)card.Original, PredictionActionKind.DynamicVariableCalculation))
		{
			simulator.History.RecordRisk(PredictionRiskReason.MethodMirrorIncomplete);
			try
			{
				return computedDynamicVar.Calculate(target);
			}
			catch (Exception ex)
			{
				Entry.Logger.Warn($"IComputedDynamicVar simulation failed: {ex}", 1);
				ModTelemetry.CaptureException(ex, "combat_dynamic_var_prediction", "calculate", GetTelemetryContext(card, (DynamicVar)computedDynamicVar, target));
				return 0m;
			}
		}
	}

	private static object GetTelemetryContext(PredictedCard card, DynamicVar dynamicVar, Creature? target)
	{
		return new
		{
			Card = TelemetryContext.ForModel((AbstractModel)(object)card.Original),
			DynamicVar = dynamicVar.Name,
			DynamicVarType = TelemetryContext.ForType(((object)dynamicVar).GetType()),
			Target = ((target == null) ? null : TelemetryContext.ForCreature(target))
		};
	}
}
