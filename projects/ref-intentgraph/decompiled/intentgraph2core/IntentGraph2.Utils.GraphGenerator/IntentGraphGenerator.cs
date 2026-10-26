using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using IntentGraph2.Models;
using IntentGraph2.Utils.Expression;
using IntentGraph2.Utils.Variable;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace IntentGraph2.Utils.GraphGenerator;

public class IntentGraphGenerator
{
	public const float IconPaddingInMove = -0.33f;

	public const float IconGroupPadding = 0.1f;

	public const float IconGroupLabelHeight = 0.25f;

	public static bool ShowMonsterMoveNames => IntentGraphMod.Config.ShowMonsterMoveNames;

	public static float IconGroupSingleMovePadding
	{
		get
		{
			if (!ShowMonsterMoveNames)
			{
				return -0.15f;
			}
			return 0f;
		}
	}

	public static Graph? GenerateAndCacheGraphForCreature(Creature creature)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			MonsterModel monster = creature.Monster;
			IgLogger.Info("Generating intent graph for monster: " + creature.Name + ".");
			Graph graph = GenerateGraph(monster);
			if (monster != null && graph != null)
			{
				IntentGraphMod.GeneratedGraphs.AddOrUpdate(monster, graph);
			}
			return graph;
		}
		catch (Exception ex)
		{
			Log.Warn(ex.ToString(), 2);
			return null;
		}
		finally
		{
			stopwatch.Stop();
			IgLogger.Info($"Finished generating intent graph for monster: {creature.Name} in {stopwatch.ElapsedMilliseconds} ms.");
		}
	}

	public static Graph? GenerateGraphForBestiary(MonsterModel monsterModel, EncounterModel encounter, string? slot)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			monsterModel.Rng = Rng.Chaotic;
			monsterModel.SetUpForCombat();
			new Creature(monsterModel, (CombatSide)2, (string)null)
			{
				CombatState = (ICombatState)new NullCombatState(),
				SlotName = slot
			};
			MonsterModel val = MonsterSpecificInitialize(monsterModel);
			if (val == null)
			{
				return null;
			}
			return GenerateGraph(val, null, null, inBestiary: true);
		}
		catch (Exception value)
		{
			IgLogger.Error($"Failed to generate intent graph for {monsterModel.Title.GetFormattedText()} in encounter {encounter.Title.GetFormattedText()} with slot {slot}: {value}");
		}
		return null;
	}

	public static Graph? GenerateGraph(MonsterModel? monster, IntentDefinition? overwriteIntentDefinition = null, IReadOnlyDictionary<string, string>? overwriteIntentStrings = null, bool inBestiary = false)
	{
		if (((monster != null) ? monster.MoveStateMachine : null) == null)
		{
			return null;
		}
		MonsterMoveStateMachine moveStateMachine = monster.MoveStateMachine;
		moveStateMachine.GetInitialState();
		IntentDefinition intentDefinition = overwriteIntentDefinition;
		VariableContext variableContext = new VariableContext(monster)
		{
			InBestiary = inBestiary
		};
		if (intentDefinition == null)
		{
			intentDefinition = GetIntentDefinition(monster, variableContext);
		}
		IntentGraphLocalizer intentGraphLocalizer = new IntentGraphLocalizer(overwriteIntentStrings, variableContext, intentDefinition);
		string warning = null;
		if (intentDefinition != null && intentDefinition.UpToDateCondition != null)
		{
			try
			{
				IExpression? expression = ExpressionParserHelper.Parse(intentDefinition.UpToDateCondition, variableContext);
				if (expression != null && !expression.GetBool())
				{
					warning = intentGraphLocalizer.GetOrElse("ui.Outdated", "Outdated");
				}
			}
			catch (Exception ex)
			{
				IgLogger.Warn($"Failed to evaluate up to date condition '{intentDefinition.UpToDateCondition}' for monster '{((AbstractModel)monster).Id}', error message: {ex.Message}");
			}
		}
		Font font = ResourceLoader.Load<Font>("res://themes/kreon_bold_glyph_space_one.tres", (string)null, (CacheMode)1);
		IntentGraphLayouter intentGraphLayouter = new IntentGraphLayouter(monster, intentGraphLocalizer, variableContext);
		Graph graph;
		if (intentDefinition?.Graph != null)
		{
			graph = intentGraphLayouter.MakeGraphFromIntentDefinition(moveStateMachine, intentDefinition.Graph, intentDefinition, font);
			graph.Warning = warning;
			graph.IntentDefinition = intentDefinition;
			return graph;
		}
		List<MonsterStateNode> stateNodes = new MonsterStateNodeConverter(intentGraphLocalizer, intentDefinition, (string text, int fontSize) => font.GetStringSize(text, (HorizontalAlignment)0, -1f, fontSize, (JustificationFlag)3, (Direction)0, (Orientation)0)).ToMonsterStateNodes(monster, moveStateMachine, ref warning);
		graph = intentGraphLayouter.StateNodesToGraph(stateNodes, intentDefinition);
		graph.Warning = warning;
		graph.IntentDefinition = intentDefinition;
		if (intentDefinition?.GraphPatch != null)
		{
			Graph graph2 = intentGraphLayouter.MakeGraphFromIntentDefinition(moveStateMachine, intentDefinition.GraphPatch, intentDefinition, font, stateNodes);
			graph.Width = Math.Max(graph.Width, graph2.Width);
			graph.Height = Math.Max(graph.Height, graph2.Height);
			graph.Icons.AddRange(graph2.Icons);
			graph.Moves.AddRange(graph2.Moves);
			graph.IconGroups.AddRange(graph2.IconGroups);
			graph.Labels.AddRange(graph2.Labels);
			graph.Arrows.AddRange(graph2.Arrows);
		}
		if (graph.Moves.Sum(delegate(Move m)
		{
			Icon[]? icons = m.Icons;
			return (icons != null) ? icons.Length : 0;
		}) == 0 && graph.IconGroups.Count == 0 && graph.Labels.Count == 0 && graph.Icons.Count == 0)
		{
			return null;
		}
		return graph;
	}

	internal static IntentDefinition? GetIntentDefinition(MonsterModel monster, VariableContext variableContext)
	{
		return IntentGraphMod.IntentDefinitions.GetValueOrDefault(((object)monster).GetType().FullName ?? string.Empty)?.FindFirstMatchIntentDefinition(variableContext);
	}

	internal static float GetMoveWidth(int iconCount)
	{
		if (iconCount != 0)
		{
			return (float)iconCount + (float)(iconCount - 1) * -0.33f;
		}
		return 0f;
	}

	private static MonsterModel? MonsterSpecificInitialize(MonsterModel monsterModel)
	{
		return monsterModel;
	}
}
