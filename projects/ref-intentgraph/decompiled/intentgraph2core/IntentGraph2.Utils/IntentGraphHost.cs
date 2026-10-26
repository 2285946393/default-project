using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IntentGraph2.Models;
using IntentGraph2.Scenes;
using IntentGraph2.Utils.GraphGenerator;
using IntentGraph2.Utils.Variable;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace IntentGraph2.Utils;

public static class IntentGraphHost
{
	private class IntentGraphItem
	{
		public required NIntentGraphPanel IntentGraphPanel { get; set; }

		public required Action RemoveIntentGraphPanel { get; set; }
	}

	private const float IntentGraphPanelTop = 90f;

	private const float IntentGraphPanelTopPinned = 160f;

	private static Dictionary<NCreature, IntentGraphItem> availableIntentGraphs = new Dictionary<NCreature, IntentGraphItem>();

	private static bool intentGraphVisible = true;

	private static CombatState? currentCombatState;

	public static void Initialize()
	{
		CombatManager.Instance.CombatSetUp += delegate(CombatState s)
		{
			currentCombatState = s;
		};
		CombatManager.Instance.CombatEnded += delegate
		{
			currentCombatState = null;
		};
	}

	public static void ToggleIntentGraphVisibility()
	{
		intentGraphVisible = !intentGraphVisible;
		if (intentGraphVisible)
		{
			foreach (IntentGraphItem value in availableIntentGraphs.Values)
			{
				((CanvasItem)value.IntentGraphPanel).Show();
			}
			return;
		}
		foreach (IntentGraphItem value2 in availableIntentGraphs.Values)
		{
			((CanvasItem)value2.IntentGraphPanel).Hide();
		}
	}

	public static void Create(NCreature nCreature)
	{
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		NCreature nCreature2 = nCreature;
		NGame instance = NGame.Instance;
		if (((instance != null) ? instance.HoverTipsContainer : null) == null)
		{
			return;
		}
		NCombatRoom instance2 = NCombatRoom.Instance;
		if (instance2 == null || instance2.Ui.Hand.InCardPlay)
		{
			return;
		}
		Creature entity = nCreature2.Entity;
		if (entity == null || !entity.IsMonster)
		{
			return;
		}
		Creature entity2 = nCreature2.Entity;
		if (entity2.Monster == null)
		{
			return;
		}
		IntentGraphMod.GeneratedGraphs.TryGetValue(entity2.Monster, out Graph value);
		if (value == null || IntentGraphGenerator.GetIntentDefinition(entity2.Monster, new VariableContext(entity2.Monster)) != value.IntentDefinition)
		{
			value = IntentGraphGenerator.GenerateAndCacheGraphForCreature(entity2);
		}
		if (availableIntentGraphs.TryGetValue(nCreature2, out IntentGraphItem value2))
		{
			GodotTreeExtensions.MoveToFrontSafely((CanvasItem)(object)value2.IntentGraphPanel);
			NIntentGraph node = ((Node)value2.IntentGraphPanel).GetNode<NIntentGraph>(NodePath.op_Implicit("%IntentGraph"));
			if (node.Graph != value)
			{
				if (value == null && !value2.IntentGraphPanel.Pinned)
				{
					value2.RemoveIntentGraphPanel();
					availableIntentGraphs.Remove(nCreature2);
				}
				else
				{
					UpdateGraph(value2.IntentGraphPanel, node, value);
					((Control)value2.IntentGraphPanel).ResetSize();
				}
			}
		}
		else
		{
			if (value == null)
			{
				return;
			}
			foreach (var (key, intentGraphItem2) in availableIntentGraphs.ToList())
			{
				if (!intentGraphItem2.IntentGraphPanel.Pinned)
				{
					intentGraphItem2.RemoveIntentGraphPanel();
					availableIntentGraphs.Remove(key);
				}
			}
			PackedScene scene = PreloadManager.Cache.GetScene("res://intentgraph2/scenes/intent_graph_panel.tscn");
			NIntentGraphPanel intentGraphPanel = scene.Instantiate<NIntentGraphPanel>((GenEditState)0);
			intentGraphPanel.NCreature = nCreature2;
			Label node2 = ((Node)intentGraphPanel).GetNode<Label>(NodePath.op_Implicit("%MonsterName"));
			node2.Text = entity2.Name;
			FontControlUtils.ApplyLocaleFontSubstitution((Control)(object)node2, (FontType)1, StringName.op_Implicit("font"));
			NIntentGraph node3 = ((Node)intentGraphPanel).GetNode<NIntentGraph>(NodePath.op_Implicit("%IntentGraph"));
			node3.GraphScale = new Vector2(IntentGraphMod.Config.IntentGraphScale, IntentGraphMod.Config.IntentGraphScale);
			node3.Monster = entity2.Monster;
			node3.AnimatedIcons = IntentGraphMod.Config.UseAnimatedIntentIcon;
			node3.ShowCurrentMove = IntentGraphMod.Config.ShowCurrentMove;
			UpdateGraph(intentGraphPanel, node3, value);
			bool pinableIntentGraph = IntentGraphMod.Config.PinableIntentGraph;
			Action handleResized = OnIntentGraphPanelResized(nCreature2, intentGraphPanel, pinableIntentGraph);
			if (!pinableIntentGraph)
			{
				((Control)nCreature2).Resized += handleResized;
			}
			((Control)intentGraphPanel).Resized += handleResized;
			((Control)intentGraphPanel).ResetSize();
			if (pinableIntentGraph)
			{
				NCombatRoom instance3 = NCombatRoom.Instance;
				if (instance3 != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)instance3.Ui, (Node)(object)intentGraphPanel);
				}
			}
			else
			{
				GodotTreeExtensions.AddChildSafely(NGame.Instance.HoverTipsContainer, (Node)(object)intentGraphPanel);
			}
			if (!intentGraphVisible)
			{
				((CanvasItem)intentGraphPanel).Hide();
			}
			availableIntentGraphs[nCreature2] = new IntentGraphItem
			{
				IntentGraphPanel = intentGraphPanel,
				RemoveIntentGraphPanel = delegate
				{
					try
					{
						((Control)intentGraphPanel).Resized -= handleResized;
						if (!pinableIntentGraph)
						{
							((Control)nCreature2).Resized -= handleResized;
						}
					}
					catch (Exception ex)
					{
						IgLogger.Error("Error unregistering resized event handlers: " + ex);
					}
					GodotTreeExtensions.QueueFreeSafely((Node)(object)intentGraphPanel);
				}
			};
		}
	}

	private static void UpdateGraph(NIntentGraphPanel intentGraphPanel, NIntentGraph intentGraph, Graph? graph)
	{
		intentGraph.Graph = graph;
		MarginContainer node = ((Node)intentGraphPanel).GetNode<MarginContainer>(NodePath.op_Implicit("%OutdatedMarkContainer"));
		if (graph != null && graph.Warning != null)
		{
			Label node2 = ((Node)node).GetNode<Label>(NodePath.op_Implicit("OutdatedMark"));
			((CanvasItem)node).Show();
			node2.Text = "⚠\ufe0f" + graph.Warning;
			FontControlUtils.ApplyLocaleFontSubstitution((Control)(object)node2, (FontType)1, StringName.op_Implicit("font"));
		}
		else
		{
			((CanvasItem)node).Hide();
		}
	}

	public static void Remove(NCreature creature)
	{
		if (availableIntentGraphs.TryGetValue(creature, out IntentGraphItem value))
		{
			value.RemoveIntentGraphPanel();
			availableIntentGraphs.Remove(creature);
		}
	}

	private static Action OnIntentGraphPanelResized(NCreature __instance, NIntentGraphPanel intentGraphPanel, bool pinableIntentGraph)
	{
		NIntentGraphPanel intentGraphPanel2 = intentGraphPanel;
		NCreature __instance2 = __instance;
		return delegate
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_012e: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Unknown result type (might be due to invalid IL or missing references)
			//IL_01af: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0178: Unknown result type (might be due to invalid IL or missing references)
			//IL_021c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0221: Unknown result type (might be due to invalid IL or missing references)
			//IL_019d: Unknown result type (might be due to invalid IL or missing references)
			//IL_022d: Expected O, but got Unknown
			//IL_0238: Unknown result type (might be due to invalid IL or missing references)
			//IL_023d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0241: Unknown result type (might be due to invalid IL or missing references)
			//IL_0246: Unknown result type (might be due to invalid IL or missing references)
			//IL_0248: Unknown result type (might be due to invalid IL or missing references)
			//IL_025a: Unknown result type (might be due to invalid IL or missing references)
			//IL_026a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0271: Unknown result type (might be due to invalid IL or missing references)
			//IL_0349: Unknown result type (might be due to invalid IL or missing references)
			//IL_0280: Unknown result type (might be due to invalid IL or missing references)
			//IL_028f: Unknown result type (might be due to invalid IL or missing references)
			//IL_029f: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0301: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0313: Unknown result type (might be due to invalid IL or missing references)
			//IL_0320: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e8: Unknown result type (might be due to invalid IL or missing references)
			if (!intentGraphPanel2.Pinned)
			{
				Rect2 viewportRect = ((CanvasItem)NGame.Instance).GetViewportRect();
				float x = ((Rect2)(ref viewportRect)).Size.X;
				float num = x / 2f;
				try
				{
					Player me = LocalContext.GetMe((ICombatState)(object)currentCombatState);
					NCreature val = ((me != null) ? me.Creature.GetCreatureNode() : null);
					if (val != null)
					{
						num = ((Control)val).GlobalPosition.X + ((Control)val).Size.X / 2f;
					}
				}
				catch (Exception ex)
				{
					IgLogger.Error("Error getting player from current combat state: " + ex);
				}
				float num2 = ((Control)__instance2).GlobalPosition.X + ((Control)__instance2).Size.X / 2f;
				if (IntentGraphMod.Config.IntentGraphPosition == IntentGraphPosition.TopLeft || (IntentGraphMod.Config.IntentGraphPosition == IntentGraphPosition.TopLeftOrRight && num2 < num))
				{
					((Control)intentGraphPanel2).Position = new Vector2(8f, pinableIntentGraph ? 160f : 90f);
				}
				else if (IntentGraphMod.Config.IntentGraphPosition == IntentGraphPosition.TopRight || (IntentGraphMod.Config.IntentGraphPosition == IntentGraphPosition.TopLeftOrRight && num2 >= num))
				{
					((Control)intentGraphPanel2).Position = new Vector2(x - ((Control)intentGraphPanel2).Size.X, pinableIntentGraph ? 160f : 90f);
				}
				else if (IntentGraphMod.Config.IntentGraphPosition == IntentGraphPosition.TopCenter)
				{
					((Control)intentGraphPanel2).Position = new Vector2(x / 2f - ((Control)intentGraphPanel2).Size.X / 2f, pinableIntentGraph ? 160f : 90f);
				}
				else
				{
					float num3 = x - ((Control)intentGraphPanel2).Size.X;
					float num4 = Math.Clamp(num2 - ((Control)intentGraphPanel2).Size.X / 2f, 0f, num3);
					Node parent = ((Node)intentGraphPanel2).GetParent();
					NHoverTipSet val2 = (NHoverTipSet)((parent != null) ? ((IEnumerable<Node>)parent.GetChildren(false)).LastOrDefault((Func<Node, bool>)((Node c) => c is NHoverTipSet)) : null);
					Control val3 = (((int)val2 != 0) ? HoverTipSetExtensions.GetTextHoverTipContainer(val2) : null);
					if (val3 != null)
					{
						Vector2 globalPosition = val3.GlobalPosition;
						Vector2 size = val3.Size;
						if (globalPosition.Y < 90f + ((Control)intentGraphPanel2).Size.Y && globalPosition.X + size.X > num4 && globalPosition.X < num4 + ((Control)intentGraphPanel2).Size.X)
						{
							if (globalPosition.X + size.X / 2f < num4 + ((Control)intentGraphPanel2).Size.X / 2f && globalPosition.X + size.X <= num3)
							{
								num4 = globalPosition.X + size.X;
							}
							else if (globalPosition.X - ((Control)intentGraphPanel2).Size.X >= 0f)
							{
								num4 = globalPosition.X - ((Control)intentGraphPanel2).Size.X;
							}
						}
					}
					((Control)intentGraphPanel2).Position = new Vector2(num4, pinableIntentGraph ? 160f : 90f);
				}
			}
		};
	}
}
