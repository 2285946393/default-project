using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Nodes;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class CombatPredictionOverlay
{
	private const float IntentGap = 6f;

	private const float OrbAvoidanceGap = 8f;

	private static readonly Dictionary<Creature, NCombatPredictionDamageIndicator> Indicators = new Dictionary<Creature, NCombatPredictionDamageIndicator>();

	public static IReadOnlyList<Control> ActiveIndicators => (IReadOnlyList<Control>)Indicators.Values.Where((NCombatPredictionDamageIndicator indicator) => ((CanvasItem)indicator).IsVisibleInTree()).ToList();

	public static void Show(DamagePrediction prediction, PredictionRisk risk, Func<Creature, IEnumerable<IHoverTip>>? getHoverTips = null)
	{
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		HashSet<Creature> hashSet = prediction.Targets.Select((DamagePredictionTarget target) => target.Target).ToHashSet();
		foreach (var (val2, nCombatPredictionDamageIndicator2) in Indicators.ToList())
		{
			if (!hashSet.Contains(val2))
			{
				GodotTreeExtensions.QueueFreeSafely((Node)(object)nCombatPredictionDamageIndicator2);
				Indicators.Remove(val2);
			}
		}
		foreach (DamagePredictionTarget target2 in prediction.Targets)
		{
			if (!Indicators.TryGetValue(target2.Target, out NCombatPredictionDamageIndicator indicator))
			{
				indicator = CreateIndicator(target2.Target);
				if (indicator == null)
				{
					continue;
				}
				if (!((Node)indicator).IsNodeReady())
				{
					((GodotObject)indicator).Connect(SignalName.Ready, Callable.From((Action)delegate
					{
						PositionIndicator(target2.Target, indicator);
					}), 0u);
				}
			}
			indicator.SetPrediction(target2, risk.HasRisk);
			indicator.SetHoverTips(getHoverTips?.Invoke(target2.Target) ?? Array.Empty<IHoverTip>());
		}
		RefreshPositions();
	}

	public static void Clear()
	{
		foreach (NCombatPredictionDamageIndicator value in Indicators.Values)
		{
			GodotTreeExtensions.QueueFreeSafely((Node)(object)value);
		}
		Indicators.Clear();
	}

	public static void RefreshPositions()
	{
		if (NCombatRoom.Instance == null)
		{
			Clear();
			return;
		}
		foreach (var (target, indicator) in Indicators.ToList())
		{
			PositionIndicator(target, indicator);
		}
	}

	private static NCombatPredictionDamageIndicator? CreateIndicator(Creature target)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		Creature target2 = target;
		NCombatRoom instance = NCombatRoom.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			NCreature creatureNode = instance.GetCreatureNode(target2);
			obj = ((creatureNode != null) ? ((Node)creatureNode).GetParent() : null);
		}
		Node val = (Node)obj;
		if (val == null)
		{
			return null;
		}
		NCombatPredictionDamageIndicator indicator = NCombatPredictionDamageIndicator.Create(target2);
		((GodotObject)indicator).Connect(SignalName.TreeExited, Callable.From((Action)delegate
		{
			RemoveIndicator(target2, indicator);
		}), 0u);
		GodotTreeExtensions.AddChildSafely(val, (Node)(object)indicator);
		Indicators[target2] = indicator;
		return indicator;
	}

	private static void RemoveIndicator(Creature target, NCombatPredictionDamageIndicator indicator)
	{
		if (Indicators.TryGetValue(target, out NCombatPredictionDamageIndicator value) && value == indicator)
		{
			Indicators.Remove(target);
		}
	}

	private static void PositionIndicator(Creature target, NCombatPredictionDamageIndicator indicator)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(target) : null);
		if (val == null)
		{
			GodotTreeExtensions.QueueFreeSafely((Node)(object)indicator);
			RemoveIndicator(target, indicator);
		}
		else if (((Node)indicator).IsNodeReady())
		{
			Rect2 globalRect = ((Control)indicator).GetGlobalRect();
			Vector2 size = ((Rect2)(ref globalRect)).Size;
			((Control)indicator).GlobalPosition = GetIndicatorPosition(val, size);
			Color modulate = ((CanvasItem)val.Visuals).Modulate;
			modulate.A = ((CanvasItem)indicator).Modulate.A;
			((CanvasItem)indicator).Modulate = modulate;
		}
	}

	private static Vector2 GetIndicatorPosition(NCreature creatureNode, Vector2 indicatorSize)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		Rect2 globalRect = creatureNode.IntentContainer.GetGlobalRect();
		Vector2 val = default(Vector2);
		((Vector2)(ref val))._002Ector(((Rect2)(ref globalRect)).GetCenter().X - indicatorSize.X / 2f, ((Rect2)(ref globalRect)).Position.Y - indicatorSize.Y - 6f);
		NOrbManager orbManager = creatureNode.OrbManager;
		if (orbManager != null)
		{
			Rect2 val2 = default(Rect2);
			((Rect2)(ref val2))._002Ector(val, indicatorSize);
			foreach (NOrb item in ((IEnumerable)((Node)((Node)orbManager).GetNode<Control>(NodePath.op_Implicit("%Orbs"))).GetChildren(false)).OfType<NOrb>())
			{
				Rect2 globalRect2 = ((Node)item).GetNode<Control>(NodePath.op_Implicit("%SelectionReticle")).GetGlobalRect();
				Rect2 val3 = ((Rect2)(ref globalRect2)).Grow(8f);
				if (((Rect2)(ref val2)).Intersects(val3, false))
				{
					val.Y = Math.Min(val.Y, ((Rect2)(ref val3)).Position.Y - indicatorSize.Y);
				}
			}
		}
		return val;
	}
}
