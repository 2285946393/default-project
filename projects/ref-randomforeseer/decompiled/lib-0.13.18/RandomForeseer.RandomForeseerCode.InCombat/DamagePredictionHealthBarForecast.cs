using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RandomForeseer.RandomForeseerCode.Data;
using STS2RitsuLib.Combat.HealthBars;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class DamagePredictionHealthBarForecast
{
	private static readonly Dictionary<Creature, int> DamageByTarget = new Dictionary<Creature, int>();

	public static IEnumerable<HealthBarForecastSegment> GetSegments(HealthBarForecastContext context)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		if (!DamageByTarget.TryGetValue(((HealthBarForecastContext)(ref context)).Creature, out var value) || value <= 0)
		{
			return Array.Empty<HealthBarForecastSegment>();
		}
		return new _003C_003Ez__ReadOnlySingleElementList<HealthBarForecastSegment>(new HealthBarForecastSegment(value, ModSettingsExtensions.get_DamagePredictionHealthBarColorValue(ModData.Settings), (HealthBarForecastGrowthDirection)0, HealthBarForecastOrder.ForSideTurnEnd(((HealthBarForecastContext)(ref context)).Creature, (CombatSide)1)));
	}

	public static void Set(DamagePrediction prediction)
	{
		Creature[] first = DamageByTarget.Keys.ToArray();
		DamageByTarget.Clear();
		foreach (DamagePredictionTarget target in prediction.Targets)
		{
			int num = (int)target.TotalUnblockedDamage;
			if (num > 0)
			{
				DamageByTarget[target.Target] = num;
			}
		}
		RefreshHealthBars(first.Concat(DamageByTarget.Keys));
	}

	public static void Clear()
	{
		if (DamageByTarget.Count != 0)
		{
			List<Creature> targets = DamageByTarget.Keys.ToList();
			DamageByTarget.Clear();
			RefreshHealthBars(targets);
		}
	}

	public static void RefreshActiveForecasts()
	{
		RefreshHealthBars(DamageByTarget.Keys);
	}

	private static void RefreshHealthBars(IEnumerable<Creature> targets)
	{
		HashSet<Creature> hashSet = new HashSet<Creature>();
		foreach (Creature target in targets)
		{
			hashSet.Add(target);
		}
		HashSet<Creature> hashSet2 = hashSet;
		NCombatRoom instance = NCombatRoom.Instance;
		foreach (NCreature item in ((instance != null) ? instance.CreatureNodes : null) ?? Array.Empty<NCreature>())
		{
			if (!hashSet2.Contains(item.Entity))
			{
				continue;
			}
			NCreatureStateDisplay nodeOrNull = ((Node)item).GetNodeOrNull<NCreatureStateDisplay>(NodePath.op_Implicit("%HealthBar"));
			if (nodeOrNull != null)
			{
				NHealthBar nodeOrNull2 = ((Node)nodeOrNull).GetNodeOrNull<NHealthBar>(NodePath.op_Implicit("%HealthBar"));
				if (nodeOrNull2 != null)
				{
					nodeOrNull2.RefreshValues();
				}
			}
		}
		NRun instance2 = NRun.Instance;
		object obj;
		if (instance2 == null)
		{
			obj = null;
		}
		else
		{
			NGlobalUi globalUi = instance2.GlobalUi;
			if (globalUi == null)
			{
				obj = null;
			}
			else
			{
				NMultiplayerPlayerStateContainer multiplayerPlayerContainer = globalUi.MultiplayerPlayerContainer;
				obj = ((multiplayerPlayerContainer != null) ? ((Node)multiplayerPlayerContainer).GetChildren(false) : null);
			}
		}
		if (obj == null)
		{
			obj = new Array<Node>();
		}
		foreach (Node item2 in (Array<Node>)obj)
		{
			NMultiplayerPlayerState val = (NMultiplayerPlayerState)(object)((item2 is NMultiplayerPlayerState) ? item2 : null);
			if (val != null && hashSet2.Contains(val.Player.Creature))
			{
				NHealthBar nodeOrNull3 = ((Node)val).GetNodeOrNull<NHealthBar>(NodePath.op_Implicit("%HealthBar"));
				if (nodeOrNull3 != null)
				{
					nodeOrNull3.RefreshValues();
				}
			}
		}
	}
}
