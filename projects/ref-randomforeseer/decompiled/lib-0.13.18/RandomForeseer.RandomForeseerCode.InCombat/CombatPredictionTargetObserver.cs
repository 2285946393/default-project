using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal sealed class CombatPredictionTargetObserver : IDisposable
{
	private static readonly HashSet<CombatPredictionTargetObserver> _activeObservers = new HashSet<CombatPredictionTargetObserver>();

	private readonly NTargetManager _targetManager;

	private Node? _hoveredNode;

	private bool _disposed;

	public event Action<Creature?>? TargetChanged;

	public event Action? TargetingFinishing;

	public CombatPredictionTargetObserver(NTargetManager targetManager)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		_targetManager = targetManager;
		targetManager.CreatureHovered += new CreatureHoveredEventHandler(OnCreatureHovered);
		targetManager.CreatureUnhovered += new CreatureUnhoveredEventHandler(OnCreatureUnhovered);
		targetManager.NodeHovered += new NodeHoveredEventHandler(OnNodeHovered);
		targetManager.NodeUnhovered += new NodeUnhoveredEventHandler(OnNodeUnhovered);
		_activeObservers.Add(this);
	}

	public void Dispose()
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		if (!_disposed)
		{
			_disposed = true;
			_targetManager.CreatureHovered -= new CreatureHoveredEventHandler(OnCreatureHovered);
			_targetManager.CreatureUnhovered -= new CreatureUnhoveredEventHandler(OnCreatureUnhovered);
			_targetManager.NodeHovered -= new NodeHoveredEventHandler(OnNodeHovered);
			_targetManager.NodeUnhovered -= new NodeUnhoveredEventHandler(OnNodeUnhovered);
			_activeObservers.Remove(this);
		}
	}

	private void OnCreatureHovered(NCreature creature)
	{
		SetHoveredTarget((Node)(object)creature);
	}

	private void OnCreatureUnhovered(NCreature creature)
	{
		ClearHoveredTarget((Node)(object)creature);
	}

	private void OnNodeHovered(Node node)
	{
		SetHoveredTarget(node);
	}

	private void OnNodeUnhovered(Node node)
	{
		ClearHoveredTarget(node);
	}

	private void SetHoveredTarget(Node node)
	{
		if (_hoveredNode != node && CombatPredictionTargetResolver.TryResolveCreature(node, out Creature creature))
		{
			_hoveredNode = node;
			this.TargetChanged?.Invoke(creature);
		}
	}

	private void ClearHoveredTarget(Node node)
	{
		if (_hoveredNode == node)
		{
			_hoveredNode = null;
			this.TargetChanged?.Invoke(null);
		}
	}

	private void OnTargetingFinishing()
	{
		_hoveredNode = null;
		Dispose();
		this.TargetingFinishing?.Invoke();
	}

	internal static void OnTargetingFinishing(NTargetManager targetManager)
	{
		CombatPredictionTargetObserver[] array = _activeObservers.ToArray();
		foreach (CombatPredictionTargetObserver combatPredictionTargetObserver in array)
		{
			if (combatPredictionTargetObserver._targetManager == targetManager)
			{
				combatPredictionTargetObserver.OnTargetingFinishing();
			}
		}
	}
}
