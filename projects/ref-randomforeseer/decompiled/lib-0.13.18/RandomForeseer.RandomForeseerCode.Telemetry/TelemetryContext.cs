using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.Telemetry;

internal static class TelemetryContext
{
	public static object ForModel(AbstractModel model)
	{
		return new
		{
			Id = ((object)model.Id).ToString(),
			Type = ForType(((object)model).GetType())
		};
	}

	public static object ForModel(Type modelType)
	{
		return new
		{
			Id = TryGetModelId(modelType),
			Type = ForType(modelType)
		};
	}

	public static object ForCreature(Creature creature)
	{
		if (!creature.IsPlayer)
		{
			return ForModel((AbstractModel)(object)creature.Monster);
		}
		return ForModel((AbstractModel)(object)creature.Player.Character);
	}

	public static object ForMethod(MethodBase method)
	{
		string method2 = GeneralExtensions.FullDescription(method);
		Type declaringType = method.DeclaringType;
		return new
		{
			Method = method2,
			DeclaringType = (((object)declaringType != null) ? ForType(declaringType) : null)
		};
	}

	public static string ForType(Type type)
	{
		return type.FullName ?? type.Name;
	}

	private static string? TryGetModelId(Type modelType)
	{
		try
		{
			return ((object)ModelDb.GetId(modelType)).ToString();
		}
		catch (Exception)
		{
			return null;
		}
	}
}
