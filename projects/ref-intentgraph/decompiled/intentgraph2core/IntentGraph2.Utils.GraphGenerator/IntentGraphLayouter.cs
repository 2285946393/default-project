using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;
using IntentGraph2.Models;
using IntentGraph2.Utils.Expression;
using IntentGraph2.Utils.Variable;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace IntentGraph2.Utils.GraphGenerator;

internal class IntentGraphLayouter
{
	private class GraphGenerationContext
	{
		public int NextXIndex { get; set; }

		public int YIndex { get; set; }

		public float NextX { get; set; }

		public float Y { get; set; }

		public Dictionary<float, MonsterStateNode> HLineTargetNode { get; set; } = new Dictionary<float, MonsterStateNode>();


		public Dictionary<float, MonsterStateNode> VLineTargetNode { get; set; } = new Dictionary<float, MonsterStateNode>();


		public Dictionary<(int x, int y), MonsterStateNode> IndexToNode { get; set; } = new Dictionary<(int, int), MonsterStateNode>();


		public IntentDefinition? IntentDefinition { get; init; }

		public Dictionary<Arrow, MonsterStateNode> ArrowTarget { get; set; } = new Dictionary<Arrow, MonsterStateNode>(ReferenceEqualityComparer.Instance);


		public Dictionary<MonsterStateNode, HashSet<MonsterStateNode>> PreviousStateNodes { get; set; } = new Dictionary<MonsterStateNode, HashSet<MonsterStateNode>>();


		public Dictionary<MonsterStateNode, Move> StateNodeToMove { get; set; } = new Dictionary<MonsterStateNode, Move>(ReferenceEqualityComparer.Instance);


		public Dictionary<Move, MonsterStateNode> MoveToStateNode { get; set; } = new Dictionary<Move, MonsterStateNode>(ReferenceEqualityComparer.Instance);


		public Dictionary<int, SubGraph> SubGraphs { get; set; } = new Dictionary<int, SubGraph>();


		internal void NewLine(float y)
		{
			NextXIndex = 0;
			NextX = 0f;
			YIndex++;
			VLineTargetNode.Clear();
			Y = y;
		}
	}

	private class SubGraph
	{
		public float Y { get; set; }

		public List<int> MoveIndices { get; set; } = new List<int>();


		public List<int> LabelIndices { get; set; } = new List<int>();


		public List<int> IconGroupIndices { get; set; } = new List<int>();


		public List<(Arrow arrow, int startIndex, int length)> Arrows { get; set; } = new List<(Arrow, int, int)>();


		public List<MonsterStateNode> Nodes { get; set; } = new List<MonsterStateNode>();


		internal void MoveY(Graph graph, float yOffset)
		{
			Y += yOffset;
			foreach (int moveIndex in MoveIndices)
			{
				Move move = graph.Moves[moveIndex];
				graph.Moves[moveIndex] = move with
				{
					Y = move.Y + yOffset,
					Icons = move.Icons?.Select((Icon icon) => icon with
					{
						Y = icon.Y + yOffset
					}).ToArray()
				};
			}
			foreach (int labelIndex in LabelIndices)
			{
				Label label = graph.Labels[labelIndex];
				graph.Labels[labelIndex] = label with
				{
					Y = label.Y + yOffset
				};
			}
			foreach (int iconGroupIndex in IconGroupIndices)
			{
				IconGroup iconGroup = graph.IconGroups[iconGroupIndex];
				graph.IconGroups[iconGroupIndex] = iconGroup with
				{
					Y = iconGroup.Y + yOffset
				};
			}
			foreach (MonsterStateNode node in Nodes)
			{
				node.Y += yOffset;
			}
			foreach (var (arrow, num, num2) in Arrows)
			{
				if (2 >= num && 2 < num + num2)
				{
					arrow.Path[2] += yOffset;
				}
				for (int i = Math.Max(3, num); i < num + num2; i++)
				{
					if ((float)(i % 2) == arrow.Path[0])
					{
						arrow.Path[i] += yOffset;
					}
				}
			}
		}
	}

	[CompilerGenerated]
	private sealed class _003CArrowSegments_003Ed__28 : IEnumerable<(bool horizontal, Vector2 start, Vector2 end, int pathIndex)>, IEnumerable, IEnumerator<(bool horizontal, Vector2 start, Vector2 end, int pathIndex)>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private (bool horizontal, Vector2 start, Vector2 end, int pathIndex) _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private Arrow arrow;

		public Arrow _003C_003E3__arrow;

		private bool _003Chorizontal_003E5__2;

		private float _003Cx_003E5__3;

		private float _003Cy_003E5__4;

		private int _003CxIndex_003E5__5;

		private int _003CyIndex_003E5__6;

		private int _003Ci_003E5__7;

		(bool horizontal, Vector2 start, Vector2 end, int pathIndex) IEnumerator<(bool, Vector2, Vector2, int)>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CArrowSegments_003Ed__28(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			//IL_0112: Unknown result type (might be due to invalid IL or missing references)
			//IL_012f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			switch (_003C_003E1__state)
			{
			default:
				return false;
			case 0:
				_003C_003E1__state = -1;
				_003Chorizontal_003E5__2 = arrow.Path[0] == 0f;
				_003Cx_003E5__3 = arrow.Path[1];
				_003Cy_003E5__4 = arrow.Path[2];
				_003CxIndex_003E5__5 = 1;
				_003CyIndex_003E5__6 = 2;
				_003Ci_003E5__7 = 3;
				break;
			case 1:
				_003C_003E1__state = -1;
				_003Cx_003E5__3 = arrow.Path[_003Ci_003E5__7];
				_003CxIndex_003E5__5 = _003Ci_003E5__7;
				goto IL_0178;
			case 2:
				{
					_003C_003E1__state = -1;
					_003Cy_003E5__4 = arrow.Path[_003Ci_003E5__7];
					_003CyIndex_003E5__6 = _003Ci_003E5__7;
					goto IL_0178;
				}
				IL_0178:
				_003Chorizontal_003E5__2 = !_003Chorizontal_003E5__2;
				_003Ci_003E5__7++;
				break;
			}
			if (_003Ci_003E5__7 < arrow.Path.Length)
			{
				if (_003Chorizontal_003E5__2)
				{
					_003C_003E2__current = (horizontal: _003Chorizontal_003E5__2, start: new Vector2(_003Cx_003E5__3, _003Cy_003E5__4), end: new Vector2(arrow.Path[_003Ci_003E5__7], _003Cy_003E5__4), pathIndex: _003CyIndex_003E5__6);
					_003C_003E1__state = 1;
					return true;
				}
				_003C_003E2__current = (horizontal: _003Chorizontal_003E5__2, start: new Vector2(_003Cx_003E5__3, _003Cy_003E5__4), end: new Vector2(_003Cx_003E5__3, arrow.Path[_003Ci_003E5__7]), pathIndex: _003CxIndex_003E5__5);
				_003C_003E1__state = 2;
				return true;
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<(bool horizontal, Vector2 start, Vector2 end, int pathIndex)> IEnumerable<(bool, Vector2, Vector2, int)>.GetEnumerator()
		{
			_003CArrowSegments_003Ed__28 _003CArrowSegments_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CArrowSegments_003Ed__ = this;
			}
			else
			{
				_003CArrowSegments_003Ed__ = new _003CArrowSegments_003Ed__28(0);
			}
			_003CArrowSegments_003Ed__.arrow = _003C_003E3__arrow;
			return _003CArrowSegments_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<(bool, Vector2, Vector2, int)>)this).GetEnumerator();
		}
	}

	private readonly MonsterModel monster;

	private readonly IntentGraphLocalizer localizer;

	private readonly VariableContext variableContext;

	public IntentGraphLayouter(MonsterModel monster, IntentGraphLocalizer localizer, VariableContext variableContext)
	{
		this.monster = monster;
		this.localizer = localizer;
		this.variableContext = variableContext;
	}

	public Graph MakeGraphFromIntentDefinition(MonsterMoveStateMachine stateMachine, Graph graph, IntentDefinition intentDefinition, Font font, List<MonsterStateNode>? stateNodes = null)
	{
		Font font2 = font;
		Dictionary<string, MonsterStateNode> allStateNodes = (from n in stateNodes?.GetAllNodes()
			where n.FullId != null
			select n).ToDictionary((MonsterStateNode n) => n.FullId);
		Graph graph2 = new Graph
		{
			Width = graph.Width,
			Height = graph.Height,
			Icons = (from i in graph.Icons
				select (i: i, ResolveRelative(i, allStateNodes)) into t
				select t.i with
				{
					X = t.Item2.x,
					Y = t.Item2.y,
					RelativeTo = null,
					VolatileValueText = (string.IsNullOrEmpty(t.i.VolatileValueText) ? t.i.ValueText : t.i.VolatileValueText)
				}).ToList(),
			IconGroups = (from i in graph.IconGroups
				select (i: i, ResolveRelative(i, allStateNodes)) into t
				select t.i with
				{
					X = t.Item2.x,
					Y = t.Item2.y,
					RelativeTo = null
				}).ToList(),
			Arrows = graph.Arrows.Select((Arrow i) => ResolveRelative(i, allStateNodes)).ToList()
		};
		foreach (Label label in graph.Labels)
		{
			(float x, float y) tuple = ResolveRelative(label, allStateNodes);
			float item = tuple.x;
			float item2 = tuple.y;
			Label resolvedLabel = label with
			{
				X = item,
				Y = item2,
				Text = localizer.FormatWithVariables(localizer.GetOrElse(label.Text, label.Text)),
				RelativeTo = null
			};
			graph2.Labels.Add(resolvedLabel);
			if (graph.Expand)
			{
				string[] array = resolvedLabel.Text.Split('\n');
				float num = array.Max((string l) => font2.GetStringSize(l, (HorizontalAlignment)0, -1f, resolvedLabel.FontSize, (JustificationFlag)3, (Direction)0, (Orientation)0).X) / 80f;
				graph2.Height = Math.Max(graph2.Height, resolvedLabel.Y + ((float)resolvedLabel.FontSize + 2f) * (float)(array.Length - 1) / 80f);
				if (resolvedLabel.Align != "right")
				{
					graph2.Width = ((resolvedLabel.Align == "left") ? Math.Max(graph2.Width, resolvedLabel.X + num) : Math.Max(graph2.Width, resolvedLabel.X + num / 2f));
				}
			}
		}
		foreach (Move move in graph.Moves)
		{
			MonsterState val = ((IEnumerable<MonsterState>)stateMachine.States.Values).FirstOrDefault((Func<MonsterState, bool>)((MonsterState s) => s.Id == move.Id));
			if (val != null)
			{
				MoveState val2 = (MoveState)(object)((val is MoveState) ? val : null);
				if (val2 != null)
				{
					(float x, float y) tuple2 = ResolveRelative(move, allStateNodes);
					float item3 = tuple2.x;
					float item4 = tuple2.y;
					MoveReplacement value = null;
					(intentDefinition?.MoveReplacements)?.TryGetValue(((MonsterState)val2).Id, out value);
					List<ResolvedIntent> resolvedIntents = MoveDetailResolver.ResolveIntentIcons(val2, value?.IntentOverrides);
					string id = move.Id;
					string[] array2 = move.Ids ?? Array.Empty<string>();
					int num2 = 0;
					string[] array3 = new string[1 + array2.Length];
					array3[num2] = id;
					num2++;
					ReadOnlySpan<string> readOnlySpan = new ReadOnlySpan<string>(array2);
					readOnlySpan.CopyTo(new Span<string>(array3).Slice(num2, readOnlySpan.Length));
					num2 += readOnlySpan.Length;
					AddMove(val2, resolvedIntents, new HashSet<string>(new _003C_003Ez__ReadOnlyArray<string>(array3)).ToArray(), graph2, item3, item4, value, move.PossiblePreviousMoveNodeIndices, null);
				}
			}
		}
		return graph2;
	}

	public Graph StateNodesToGraph(List<MonsterStateNode> stateNodes, IntentDefinition? intentDefinition)
	{
		Graph graph = new Graph();
		float num = intentDefinition?.Offset.X ?? 0f;
		GraphGenerationContext graphGenerationContext = new GraphGenerationContext
		{
			IntentDefinition = intentDefinition,
			PreviousStateNodes = MonsterStateNodeSimplifier.GetPrecessorDict(stateNodes.GetAllNodes()),
			Y = (intentDefinition?.Offset.Y ?? 0f)
		};
		foreach (MonsterStateNode stateNode in stateNodes)
		{
			graphGenerationContext.SubGraphs[graphGenerationContext.YIndex] = new SubGraph
			{
				Y = graphGenerationContext.Y
			};
			graphGenerationContext.Y += stateNode.Offset.Y;
			if (stateNodes.Count == 1 && stateNode.NextState == stateNode && stateNode.Children == null)
			{
				stateNode.NextState = null;
				stateNode.NextStateCount = 0;
			}
			AddStateNodeToGraph(stateNode, null, graph, graphGenerationContext, num + stateNode.Offset.X);
			AddPossiblePreviousMoveIds(graph.Moves, graphGenerationContext);
			graphGenerationContext.NewLine(graph.Height + 0.25f);
		}
		TuneArrowPosition(graph.Arrows, graphGenerationContext.ArrowTarget);
		return graph;
	}

	private (float x, float y) ResolveRelative(IRelativeToPosition relativeToPosition, Dictionary<string, MonsterStateNode>? stateNodes)
	{
		if (stateNodes == null || relativeToPosition.RelativeTo == null || !stateNodes.TryGetValue(relativeToPosition.RelativeTo, out MonsterStateNode value))
		{
			return (x: relativeToPosition.X, y: relativeToPosition.Y);
		}
		return (x: value.X + relativeToPosition.X, y: value.Y + relativeToPosition.Y);
	}

	private Arrow ResolveRelative(Arrow relativeToPosition, Dictionary<string, MonsterStateNode>? stateNodes)
	{
		if (stateNodes == null || relativeToPosition.RelativeTo == null || !stateNodes.TryGetValue(relativeToPosition.RelativeTo, out MonsterStateNode value))
		{
			return relativeToPosition;
		}
		float[] array = (float[])relativeToPosition.Path.Clone();
		array[1] += value.X;
		array[2] += value.Y;
		for (int i = 3; i < array.Length; i++)
		{
			array[i] += ((array[0] == (float)(i % 2)) ? value.Y : value.X);
		}
		return new Arrow(array);
	}

	private void AddStateNodeToGraph(MonsterStateNode stateNode, MonsterStateNode? precessorNode, Graph graph, GraphGenerationContext context, float x)
	{
		if (!stateNode.AddedToGraph)
		{
			if (stateNode.SimpleLoopStart && stateNode.SimpleLoopPrecessorCount == 1 && (stateNode.SimpleLoopLength >= 4 || (stateNode.SimpleLoopLength == 3 && (context.NextX >= 3f || graph.Height - context.Y >= 2f))))
			{
				AddSimpleLoopToGraph(stateNode, precessorNode, graph, context, x);
			}
			else
			{
				AddStateNodeToGraphNormal(stateNode, graph, context, x, 0f);
			}
		}
	}

	private void AddStateNodeToGraphNormal(MonsterStateNode stateNode, Graph graph, GraphGenerationContext context, float x, float yOffset)
	{
		if (stateNode.AddedToGraph)
		{
			return;
		}
		stateNode.AddedToGraph = true;
		stateNode.X = x;
		stateNode.Y = context.Y + yOffset;
		float y = stateNode.Y;
		if (stateNode.Parent != null)
		{
			stateNode.XIndex = stateNode.Parent.XIndex;
			stateNode.YIndex = stateNode.Parent.YIndex;
		}
		else
		{
			stateNode.XIndex = context.NextXIndex++;
			stateNode.YIndex = context.YIndex;
			context.IndexToNode[(stateNode.XIndex, stateNode.YIndex)] = stateNode;
		}
		context.SubGraphs[stateNode.YIndex].Nodes.Add(stateNode);
		if (context.NextX < x + stateNode.Width + 0.25f + 0.25f * (float)stateNode.NextStateCount)
		{
			context.NextX = x + stateNode.Width + 0.25f + 0.25f * (float)stateNode.NextStateCount;
		}
		if (x + stateNode.Width > graph.Width)
		{
			graph.Width = x + stateNode.Width;
		}
		if (y + stateNode.Height > graph.Height)
		{
			graph.Height = y + stateNode.Height;
		}
		if (stateNode.Parent == null)
		{
			stateNode.ArrowRight = x + stateNode.Width + 0.25f;
			stateNode.ArrowBottom = y + stateNode.Height + 0.25f;
		}
		else
		{
			stateNode.ArrowRight = stateNode.Parent.ArrowRight;
			stateNode.ArrowBottom = stateNode.Parent.ArrowBottom;
		}
		if (stateNode.Children == null)
		{
			MonsterState? state = stateNode.State;
			MoveState val = (MoveState)(object)((state is MoveState) ? state : null);
			if (val != null)
			{
				AddMove(val, stateNode, graph, x, y, context);
			}
		}
		else
		{
			SubGraph subGraph = context.SubGraphs[stateNode.YIndex];
			float num = yOffset + 0.25f + 0.1f;
			float num2 = x + 0.1f;
			for (int i = 0; i < stateNode.Children.Count; i++)
			{
				MonsterStateNode monsterStateNode = stateNode.Children[i];
				graph.Labels.Add(new Label(num2, context.Y + num - 0.04f, monsterStateNode.Label?.Text ?? string.Empty));
				subGraph.LabelIndices.Add(graph.Labels.Count - 1);
				if (stateNode.HorizontalLayout)
				{
					AddStateNodeToGraphNormal(monsterStateNode, graph, context, num2, num + ((monsterStateNode.Children == null) ? IntentGraphGenerator.IconGroupSingleMovePadding : 0f));
					num2 += monsterStateNode.Width + 0.1f;
					continue;
				}
				if (monsterStateNode.Children == null)
				{
					num += IntentGraphGenerator.IconGroupSingleMovePadding;
				}
				AddStateNodeToGraphNormal(monsterStateNode, graph, context, num2, num);
				num += monsterStateNode.Height + 0.25f;
			}
			graph.IconGroups.Add(new IconGroup(stateNode.X, stateNode.Y, stateNode.Width, stateNode.Height));
			subGraph.IconGroupIndices.Add(graph.IconGroups.Count - 1);
		}
		if (stateNode.NextState != null)
		{
			MonsterStateNode monsterStateNode2 = stateNode;
			while (monsterStateNode2.Parent != null)
			{
				monsterStateNode2 = monsterStateNode2.Parent;
			}
			MonsterStateNode nextState = stateNode.NextState;
			AddStateNodeToGraph(nextState, stateNode, graph, context, context.NextX);
			AddArrow(stateNode, nextState, graph, context);
		}
	}

	private void AddMove(MoveState moveState, MonsterStateNode node, Graph graph, float x, float y, GraphGenerationContext context)
	{
		SubGraph subGraph = context.SubGraphs[node.YIndex];
		MoveReplacement replacement = context.IntentDefinition?.MoveReplacements.GetMoveReplacementOrNull(((MonsterState)moveState).Id, node.FullId);
		List<ResolvedIntent> resolvedIntentIcons = node.ResolvedIntentIcons;
		if (resolvedIntentIcons != null)
		{
			Move move = AddMove(moveState, resolvedIntentIcons, node.MoveStateIds.ToArray(), graph, x, y, replacement, null, subGraph);
			context.StateNodeToMove[node] = move;
			context.MoveToStateNode[move] = node;
		}
	}

	private Move AddMove(MoveState moveState, List<ResolvedIntent> resolvedIntents, string[] ids, Graph graph, float x, float y, MoveReplacement? replacement, int?[]? possiblePreviousMoveIndices, SubGraph? subGraph)
	{
		//IL_0301: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		IntentOverride[] array = replacement?.IntentOverrides;
		Icon[] array2 = new Icon[resolvedIntents.Count];
		for (int i = 0; i < resolvedIntents.Count; i++)
		{
			ResolvedIntent resolvedIntent = resolvedIntents[i];
			AbstractIntent intent = resolvedIntent.Intent;
			IntentOverride intentOverride = ((resolvedIntent.OriginalIntentIndex < array?.Length) ? array[resolvedIntent.OriginalIntentIndex] : null);
			float x2 = (float)i * 0.66999996f + x;
			if (resolvedIntent.ModelId != (ModelId)null)
			{
				string category = resolvedIntent.ModelId.Category;
				string text;
				if (!(category == "POWER"))
				{
					if (category == "CARD")
					{
						CardModel byIdOrNull = ModelDb.GetByIdOrNull<CardModel>(resolvedIntent.ModelId);
						text = ((byIdOrNull != null) ? byIdOrNull.PortraitPath : null) ?? string.Empty;
					}
					else
					{
						text = string.Empty;
					}
				}
				else
				{
					PowerModel byIdOrNull2 = ModelDb.GetByIdOrNull<PowerModel>(resolvedIntent.ModelId);
					text = ((byIdOrNull2 != null) ? byIdOrNull2.IconPath : null) ?? string.Empty;
				}
				string imageResourcePath = text;
				int? value = resolvedIntent.Value;
				int? obj2;
				if (!value.HasValue)
				{
					AbstractIntent obj = ((intent is StatusIntent) ? intent : null);
					obj2 = ((obj != null) ? new int?(((StatusIntent)obj).CardCount) : null);
				}
				else
				{
					obj2 = value;
				}
				int? value2 = obj2;
				array2[i] = new Icon(x2, y, intent.IntentType, value2, 1, GetLocalizedValueText(resolvedIntent.ValueText ?? intentOverride?.ValueText, value2.GetValueOrDefault()), "", "", null, imageResourcePath, Enum.TryParse<MoveDetailIconType>(resolvedIntent.ModelId.Category, ignoreCase: true, out var result) ? result : MoveDetailIconType.None);
				continue;
			}
			AttackIntent val = (AttackIntent)(object)((intent is AttackIntent) ? intent : null);
			if (val != null)
			{
				int? value3 = (int?)val.DamageCalc?.Invoke();
				int repeats = val.Repeats;
				array2[i] = new Icon(x2, y, intent.IntentType, value3, repeats, GetLocalizedValueText(intentOverride?.ValueText, value3.GetValueOrDefault()), GetLocalizedValueText(intentOverride?.TimesText, repeats), intentOverride?.VolatileValueText ?? intentOverride?.ValueText ?? string.Empty);
				continue;
			}
			StatusIntent val2 = (StatusIntent)(object)((intent is StatusIntent) ? intent : null);
			if (val2 != null)
			{
				int cardCount = val2.CardCount;
				array2[i] = new Icon(x2, y, intent.IntentType, cardCount, 1, GetLocalizedValueText(intentOverride?.ValueText, cardCount));
			}
			else
			{
				array2[i] = new Icon(x2, y, intent.IntentType);
			}
		}
		string text2 = replacement?.CurrentMoveCondition;
		IExpression currentMoveCondition = ((text2 != null) ? IExpression.Parse(text2, variableContext) : null);
		Move move = new Move(ids[0], ids, x, y, array2, possiblePreviousMoveIndices, null, currentMoveCondition);
		graph.Moves.Add(move);
		subGraph?.MoveIndices.Add(graph.Moves.Count - 1);
		if (IntentGraphGenerator.ShowMonsterMoveNames && array2.Length != 0)
		{
			AddMoveName(moveState, graph, x, y, IntentGraphGenerator.GetMoveWidth(resolvedIntents.Count), subGraph);
		}
		return move;
	}

	private string GetLocalizedValueText(string? text, int originalValue)
	{
		if (text == null)
		{
			return string.Empty;
		}
		text = localizer.GetOrElse(text, text);
		text = localizer.FormatWithVariables(text, (string v, string? t) => (!(v == "originalValue")) ? null : originalValue.ToString());
		return text;
	}

	private void AddMoveName(MoveState moveState, Graph graph, float x, float y, float moveWidth, SubGraph? subGraph)
	{
		string moveName = localizer.GetMoveName(monster, ((MonsterState)moveState).Id);
		if (moveName != null)
		{
			Label item = new Label(x + moveWidth / 2f, y + 0.2f, moveName, "center", 15);
			graph.Labels.Add(item);
			subGraph?.LabelIndices.Add(graph.Labels.Count - 1);
		}
	}

	private void AddSimpleLoopToGraph(MonsterStateNode loopStart, MonsterStateNode? precessorNode, Graph graph, GraphGenerationContext context, float x)
	{
		if (loopStart.AddedToGraph)
		{
			return;
		}
		float y = context.Y;
		bool flag = precessorNode == null || (precessorNode.XIndex == context.NextXIndex - 1 && precessorNode.Y < y + 0.5f);
		List<MonsterStateNode> list = new List<MonsterStateNode>();
		MonsterStateNode monsterStateNode = loopStart;
		while (monsterStateNode != null && !list.Contains(monsterStateNode))
		{
			list.Add(monsterStateNode);
			monsterStateNode = monsterStateNode.NextState;
		}
		int simpleLoopLength = loopStart.SimpleLoopLength;
		float num = x;
		int num2 = list.IndexOf(loopStart);
		int i;
		for (i = num2 + (simpleLoopLength + 1) / 2; (double)(list.Skip(num2).Take(i - num2).Sum((MonsterStateNode n) => n.Width) + 0.5f * (float)(i - num2 - 1) + (flag ? 0.5f : ((0f - loopStart.Width) / 2f - 0.5f))) < (double)list.Skip(i).Sum((MonsterStateNode n) => n.Width) + 0.5 * (double)(list.Count - i - 1); i++)
		{
		}
		if (list.Count - i <= 0)
		{
			AddStateNodeToGraphNormal(loopStart, graph, context, x, y);
			return;
		}
		if (list.Count - i == 1 && list.Count > 3 && list[i].Width < 1.5f && context.NextX < 2f)
		{
			AddStateNodeToGraphNormal(loopStart, graph, context, x, y);
			return;
		}
		bool flag2 = list.Count - i <= 1 && flag;
		float num3 = y + 1.35f;
		for (int j = 0; j < i; j++)
		{
			MonsterStateNode monsterStateNode2 = list[j];
			monsterStateNode2.X = x;
			monsterStateNode2.Y = y;
			if (j == num2)
			{
				num = x;
			}
			MonsterState? state = monsterStateNode2.State;
			MoveState val = (MoveState)(object)((state is MoveState) ? state : null);
			if (val != null)
			{
				AddMove(val, monsterStateNode2, graph, monsterStateNode2.X, monsterStateNode2.Y, context);
			}
			if (monsterStateNode2.NextState != null)
			{
				if (j == i - 1)
				{
					if (!flag2)
					{
						AddArrow(graph, new Arrow(new float[4]
						{
							1f,
							x + monsterStateNode2.Width - Math.Min(monsterStateNode2.Width, monsterStateNode2.NextState.Width) / 2f,
							monsterStateNode2.Y + monsterStateNode2.Height,
							num3
						}), context, monsterStateNode2.NextState);
					}
				}
				else
				{
					AddArrow(graph, new Arrow(new float[4]
					{
						0f,
						x + monsterStateNode2.Width,
						monsterStateNode2.Y + 0.5f,
						x + monsterStateNode2.Width + 0.5f
					}), context, monsterStateNode2.NextState);
				}
			}
			x += monsterStateNode2.Width + 0.5f;
		}
		x -= 0.5f;
		context.NextX = x + 0.25f;
		graph.Width = x;
		graph.Height = Math.Max(num3 + 1f, graph.Height);
		if (!flag2)
		{
			float num4 = x - num;
			float num5 = ((flag && list.Count - i != 1) ? ((num4 - list.Skip(i).Sum((MonsterStateNode n) => n.Width)) / (float)(list.Count - i - 1)) : 0.5f);
			for (int k = i; k < list.Count; k++)
			{
				MonsterStateNode monsterStateNode3 = list[k];
				monsterStateNode3.X = x - monsterStateNode3.Width;
				monsterStateNode3.Y = num3;
				MonsterState? state2 = monsterStateNode3.State;
				MoveState val2 = (MoveState)(object)((state2 is MoveState) ? state2 : null);
				if (val2 != null)
				{
					AddMove(val2, monsterStateNode3, graph, monsterStateNode3.X, monsterStateNode3.Y, context);
				}
				if (monsterStateNode3.NextState != null)
				{
					if (k == list.Count - 1)
					{
						if (flag)
						{
							AddArrow(graph, new Arrow(new float[4]
							{
								1f,
								monsterStateNode3.X + Math.Min(monsterStateNode3.Width, monsterStateNode3.NextState.Width) / 2f,
								monsterStateNode3.Y,
								loopStart.Y + loopStart.Height
							}), context, monsterStateNode3.NextState);
						}
						else
						{
							AddArrow(graph, new Arrow(new float[5]
							{
								0f,
								monsterStateNode3.X,
								monsterStateNode3.Y + 0.5f,
								num + loopStart.Width / 2f,
								loopStart.Y + loopStart.Height
							}), context, monsterStateNode3.NextState);
						}
					}
					else
					{
						AddArrow(graph, new Arrow(new float[4]
						{
							0f,
							monsterStateNode3.X,
							monsterStateNode3.Y + 0.5f,
							monsterStateNode3.X - num5
						}), context, monsterStateNode3.NextState);
					}
				}
				x -= monsterStateNode3.Width + num5;
			}
		}
		else
		{
			MonsterStateNode monsterStateNode4 = list[i - 1];
			MonsterStateNode monsterStateNode5 = list[i];
			monsterStateNode5.X = num + (x - num) / 2f - monsterStateNode5.Width / 2f;
			monsterStateNode5.Y = num3;
			MonsterState? state3 = monsterStateNode5.State;
			MoveState val3 = (MoveState)(object)((state3 is MoveState) ? state3 : null);
			if (val3 != null)
			{
				AddMove(val3, monsterStateNode5, graph, monsterStateNode5.X, monsterStateNode5.Y, context);
			}
			if (monsterStateNode4.X + monsterStateNode4.Width / 2f < monsterStateNode5.X + monsterStateNode5.Width - 0.25f)
			{
				AddArrow(graph, new Arrow(new float[4]
				{
					1f,
					monsterStateNode4.X + monsterStateNode4.Width / 2f,
					monsterStateNode4.Y + monsterStateNode4.Height,
					monsterStateNode5.Y
				}), context, monsterStateNode5);
			}
			else
			{
				AddArrow(graph, new Arrow(new float[5]
				{
					1f,
					Math.Max(monsterStateNode4.X + monsterStateNode4.Width / 2f, monsterStateNode5.X + monsterStateNode5.Width + 0.25f),
					monsterStateNode4.Y + monsterStateNode4.Height,
					monsterStateNode5.Y + 0.5f,
					monsterStateNode5.X + monsterStateNode5.Width
				}), context, monsterStateNode5);
			}
			if (loopStart.X + loopStart.Width / 2f > monsterStateNode5.X + 0.25f)
			{
				AddArrow(graph, new Arrow(new float[4]
				{
					1f,
					loopStart.X + loopStart.Width / 2f,
					monsterStateNode5.Y,
					loopStart.Y + loopStart.Height
				}), context, loopStart);
			}
			else
			{
				AddArrow(graph, new Arrow(new float[5]
				{
					0f,
					monsterStateNode5.X,
					monsterStateNode5.Y + 0.5f,
					Math.Min(loopStart.X + loopStart.Width / 2f, monsterStateNode5.X - 0.25f),
					loopStart.Y + loopStart.Height
				}), context, loopStart);
			}
		}
		for (int l = 0; l < list.Count; l++)
		{
			MonsterStateNode monsterStateNode6 = list[l];
			monsterStateNode6.AddedToGraph = true;
			monsterStateNode6.AddedArrow = true;
			monsterStateNode6.XIndex = context.NextXIndex++;
			monsterStateNode6.YIndex = context.YIndex;
			context.IndexToNode[(monsterStateNode6.XIndex, monsterStateNode6.YIndex)] = monsterStateNode6;
			context.SubGraphs[monsterStateNode6.YIndex].Nodes.Add(monsterStateNode6);
		}
	}

	private void AddArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
	{
		if (stateNode.AddedArrow)
		{
			return;
		}
		stateNode.AddedArrow = true;
		if (TryAddOverrideArrow(stateNode, nextStateNode, graph, context))
		{
			return;
		}
		if (stateNode.YIndex == nextStateNode.YIndex)
		{
			if ((stateNode.XIndex == nextStateNode.XIndex || !TryAddHorizontalStraightArrow(stateNode, nextStateNode, graph, context)) && (stateNode.XIndex == nextStateNode.XIndex || !TryAddHorizontalThenUpArrow(stateNode, nextStateNode, graph, context)))
			{
				AddDefaultSameYArrow(stateNode, nextStateNode, graph, context);
			}
		}
		else if ((stateNode.YIndex != nextStateNode.YIndex + 1 || !TryAddVerticalStraightArrow(stateNode, nextStateNode, graph, context)) && !TryAddVerticalStartDifferentYArrow(stateNode, nextStateNode, graph, context))
		{
			AddDefaultDifferentYArrow(stateNode, nextStateNode, graph, context);
		}
	}

	private bool TryAddOverrideArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
	{
		if (!TryGetArrowOverride(stateNode, context, out ArrowOverride arrowOverride))
		{
			return false;
		}
		if (arrowOverride.Path.Length <= 3)
		{
			return true;
		}
		float?[] array = (float?[])arrowOverride.Path.Clone();
		int num = 0;
		int num2 = array.Length;
		if (array[0].HasValue)
		{
			if (array[0] == 0f)
			{
				if (!array[1].HasValue && array[3].HasValue)
				{
					float valueOrDefault = array[3].GetValueOrDefault();
					array[1] = ((valueOrDefault > stateNode.X + stateNode.Width / 2f) ? (stateNode.X + stateNode.Width) : stateNode.X);
					num = 1;
				}
				if (!array[2].HasValue)
				{
					array[2] = stateNode.Y + stateNode.Height / 2f;
					num = 2;
				}
			}
			else
			{
				if (!array[1].HasValue)
				{
					array[1] = stateNode.X + stateNode.Width / 2f;
					num = 1;
				}
				if (!array[2].HasValue && array[3].HasValue)
				{
					float valueOrDefault2 = array[3].GetValueOrDefault();
					array[2] = ((valueOrDefault2 > stateNode.Y + stateNode.Height / 2f) ? (stateNode.Y + stateNode.Height) : stateNode.Y);
					num = 2;
				}
			}
			if ((float)(array.Length % 2) == array[0])
			{
				int num3 = ((array.Length <= 5) ? 1 : (array.Length - 3));
				if (!array[^1].HasValue && array[num3].HasValue)
				{
					float valueOrDefault3 = array[num3].GetValueOrDefault();
					array[^1] = ((valueOrDefault3 > nextStateNode.X + nextStateNode.Width / 2f) ? (nextStateNode.X + nextStateNode.Width) : nextStateNode.X);
					num2 = array.Length - 1;
				}
				if (!array[^2].HasValue)
				{
					array[^2] = nextStateNode.Y + nextStateNode.Height / 2f;
					num2 = array.Length - 2;
				}
			}
			else
			{
				int num4 = ((array.Length > 4) ? (array.Length - 3) : 2);
				if (!array[^1].HasValue && array[num4].HasValue)
				{
					float valueOrDefault4 = array[num4].GetValueOrDefault();
					array[^1] = ((valueOrDefault4 > nextStateNode.Y + nextStateNode.Height / 2f) ? (nextStateNode.Y + nextStateNode.Height) : nextStateNode.Y);
					num2 = array.Length - 1;
				}
				if (!array[^2].HasValue && array.Length > 4)
				{
					array[^2] = nextStateNode.X + nextStateNode.Width / 2f;
					num2 = array.Length - 2;
				}
			}
		}
		Arrow arrow = new Arrow(array.Select((float? p) => p.GetValueOrDefault()).ToArray());
		AddArrow(graph, arrow, context, nextStateNode, addSubGraph: false);
		if (num > 0)
		{
			context.SubGraphs[stateNode.YIndex].Arrows.Add((arrow, 0, num + 1));
		}
		if (num2 < array.Length)
		{
			context.SubGraphs[nextStateNode.YIndex].Arrows.Add((arrow, num2, array.Length - num2));
		}
		return true;
	}

	private bool TryAddHorizontalStraightArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
	{
		float num = Math.Max(stateNode.Y + 0.25f, nextStateNode.Y + 0.25f);
		float num2 = Math.Min(stateNode.Y + stateNode.Height - 0.25f, nextStateNode.Y + nextStateNode.Height - 0.25f);
		for (int i = Math.Min(stateNode.XIndex, nextStateNode.XIndex) + 1; i < Math.Max(stateNode.XIndex, nextStateNode.XIndex); i++)
		{
			MonsterStateNode monsterStateNode = context.IndexToNode[(i, stateNode.YIndex)];
			if (monsterStateNode.Y + monsterStateNode.Height + 0.2f > num)
			{
				num = monsterStateNode.Y + monsterStateNode.Height + 0.2f;
			}
		}
		if (num > num2)
		{
			return false;
		}
		float num3 = (num + num2) / 2f;
		ArrowOverride arrowOverride;
		if (stateNode.X < nextStateNode.X)
		{
			if (nextStateNode.NextState == stateNode && !nextStateNode.AddedArrow && !TryGetArrowOverride(nextStateNode, context, out arrowOverride))
			{
				AddArrow(graph, new Arrow(new float[4]
				{
					0f,
					stateNode.X + stateNode.Width,
					num3 - 0.2f,
					nextStateNode.X
				}), context, nextStateNode);
				AddArrow(graph, new Arrow(new float[4]
				{
					0f,
					nextStateNode.X,
					num3 + 0.2f,
					stateNode.X + stateNode.Width
				}), context, stateNode);
				nextStateNode.AddedArrow = true;
				return true;
			}
			Arrow arrow = new Arrow(new float[4]
			{
				0f,
				stateNode.X + stateNode.Width,
				num3,
				nextStateNode.X
			});
			if (stateNode.Parent != null)
			{
				arrow.Path[1] -= 0.1f;
			}
			AddArrow(graph, arrow, context, nextStateNode);
			return true;
		}
		if (nextStateNode.NextState == stateNode && !nextStateNode.AddedArrow && !TryGetArrowOverride(nextStateNode, context, out arrowOverride))
		{
			AddArrow(graph, new Arrow(new float[4]
			{
				0f,
				stateNode.X,
				num3 + 0.2f,
				nextStateNode.X + nextStateNode.Width
			}), context, nextStateNode);
			AddArrow(graph, new Arrow(new float[4]
			{
				0f,
				nextStateNode.X + nextStateNode.Width,
				num3 - 0.2f,
				stateNode.X
			}), context, stateNode);
			nextStateNode.AddedArrow = true;
			return true;
		}
		Arrow arrow2 = new Arrow(new float[4]
		{
			0f,
			stateNode.X,
			num3,
			nextStateNode.X + nextStateNode.Width
		});
		if (stateNode.Parent != null)
		{
			arrow2.Path[1] += 0.1f;
		}
		AddArrow(graph, arrow2, context, nextStateNode);
		return true;
	}

	private bool TryAddHorizontalThenUpArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
	{
		float num = Math.Max(stateNode.Y + 0.25f, nextStateNode.Y + nextStateNode.Height + 0.25f);
		float num2 = stateNode.Y + stateNode.Height - 0.25f;
		for (int i = Math.Min(stateNode.XIndex, nextStateNode.XIndex) + 1; i < Math.Max(stateNode.XIndex, nextStateNode.XIndex); i++)
		{
			MonsterStateNode monsterStateNode = context.IndexToNode[(i, stateNode.YIndex)];
			if (monsterStateNode.Y + monsterStateNode.Height + 0.2f > num)
			{
				num = monsterStateNode.Y + monsterStateNode.Height + 0.2f;
			}
		}
		if (num2 < num)
		{
			return false;
		}
		float num3 = (num + num2) / 2f;
		if (!context.HLineTargetNode.ContainsKey(num3))
		{
			context.HLineTargetNode[num3] = nextStateNode;
			Arrow arrow;
			if (stateNode.X < nextStateNode.X)
			{
				arrow = new Arrow(new float[5]
				{
					0f,
					stateNode.X + stateNode.Width,
					num3,
					nextStateNode.X + nextStateNode.Width / 2f,
					nextStateNode.Y + nextStateNode.Height
				});
				if (stateNode.Parent != null)
				{
					arrow.Path[1] -= 0.1f;
				}
			}
			else
			{
				arrow = new Arrow(new float[5]
				{
					0f,
					stateNode.X,
					num3,
					nextStateNode.X + nextStateNode.Width / 2f,
					nextStateNode.Y + nextStateNode.Height
				});
				if (stateNode.Parent != null)
				{
					arrow.Path[1] += 0.1f;
				}
			}
			AddArrow(graph, arrow, context, nextStateNode);
			return true;
		}
		return false;
	}

	private void AddDefaultSameYArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
	{
		float num;
		MonsterStateNode value;
		for (num = stateNode.ArrowRight; context.VLineTargetNode.TryGetValue(num, out value); num += 0.25f)
		{
			if (value == nextStateNode)
			{
				break;
			}
		}
		float num2 = ((nextStateNode.XIndex <= stateNode.XIndex) ? stateNode.ArrowBottom : 0f);
		for (int i = Math.Min(stateNode.XIndex, nextStateNode.XIndex) + 1; i < Math.Max(stateNode.XIndex, nextStateNode.XIndex); i++)
		{
			MonsterStateNode monsterStateNode = context.IndexToNode[(i, stateNode.YIndex)];
			if (num2 < monsterStateNode.Y + monsterStateNode.Height + 0.25f)
			{
				num2 = monsterStateNode.Y + monsterStateNode.Height + 0.25f;
			}
		}
		if (num2 < nextStateNode.Y + nextStateNode.Height + 0.25f)
		{
			num2 = nextStateNode.Y + nextStateNode.Height + 0.25f;
		}
		MonsterStateNode value2;
		for (; context.HLineTargetNode.TryGetValue(num2, out value2); num2 += 0.25f)
		{
			if (value2 == nextStateNode)
			{
				break;
			}
		}
		context.VLineTargetNode[num] = nextStateNode;
		context.HLineTargetNode[num2] = nextStateNode;
		Arrow arrow = new Arrow(new float[7]
		{
			0f,
			stateNode.X + stateNode.Width,
			stateNode.Y + stateNode.Height / 2f,
			num,
			num2,
			nextStateNode.X + nextStateNode.Width / 2f,
			nextStateNode.Y + nextStateNode.Height
		});
		if (stateNode.Parent != null && stateNode.Children == null)
		{
			arrow.Path[1] -= 0.1f;
		}
		AddArrow(graph, arrow, context, nextStateNode);
		if (num > graph.Width)
		{
			graph.Width = num;
		}
		if (num2 > graph.Height)
		{
			graph.Height = num2;
		}
	}

	private bool TryAddVerticalStraightArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
	{
		if (stateNode.Parent != null && !stateNode.Parent.HorizontalLayout)
		{
			return false;
		}
		float num = Math.Max(stateNode.X + 0.25f, nextStateNode.X + 0.25f);
		float num2 = Math.Min(stateNode.X + stateNode.Width - 0.25f, nextStateNode.X + nextStateNode.Width - 0.25f);
		if (num <= num2)
		{
			float num3 = (num + num2) / 2f;
			Arrow arrow = new Arrow(new float[4]
			{
				1f,
				num3,
				stateNode.Y,
				nextStateNode.Y + nextStateNode.Height
			});
			AddArrow(graph, arrow, context, nextStateNode, addSubGraph: false);
			context.SubGraphs[stateNode.YIndex].Arrows.Add((arrow, 0, 3));
			context.SubGraphs[nextStateNode.YIndex].Arrows.Add((arrow, 3, 1));
			return true;
		}
		return false;
	}

	private bool TryAddVerticalStartDifferentYArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
	{
		MonsterStateNode nextStateNode2 = nextStateNode;
		if (stateNode.Parent != null && !stateNode.Parent.HorizontalLayout)
		{
			return false;
		}
		float num = stateNode.X + stateNode.Width / 2f;
		float num2 = nextStateNode2.X + nextStateNode2.Width / 2f;
		float? midY = ((IEnumerable<float>)(from p in context.HLineTargetNode
			where p.Value == nextStateNode2
			select p.Key into p
			orderby p
			select p)).Select((Func<float, float?>)((float k) => k)).FirstOrDefault();
		SubGraph subGraph = context.SubGraphs[stateNode.YIndex];
		if (!midY.HasValue)
		{
			float num3 = 0.25f;
			midY = subGraph.Y;
			context.Y += num3;
			graph.Height += num3;
			subGraph.MoveY(graph, num3);
			context.HLineTargetNode[midY.Value] = nextStateNode2;
			foreach (var (num5, value) in context.HLineTargetNode.Where<KeyValuePair<float, MonsterStateNode>>((KeyValuePair<float, MonsterStateNode> p) => p.Key > midY.Value).ToList())
			{
				context.HLineTargetNode[num5 + num3] = value;
				context.HLineTargetNode.Remove(num5);
			}
		}
		Arrow arrow = new Arrow(new float[6]
		{
			1f,
			num,
			stateNode.Y,
			midY.Value,
			num2,
			nextStateNode2.Y + nextStateNode2.Height
		});
		AddArrow(graph, arrow, context, nextStateNode2, addSubGraph: false);
		context.SubGraphs[stateNode.YIndex].Arrows.Add((arrow, 0, 3));
		GetSubGraphByY(context.SubGraphs, midY.Value).Arrows.Add((arrow, 3, 2));
		context.SubGraphs[nextStateNode2.YIndex].Arrows.Add((arrow, 5, 1));
		return true;
	}

	private void AddDefaultDifferentYArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
	{
		MonsterStateNode nextStateNode2 = nextStateNode;
		float num;
		MonsterStateNode value;
		for (num = stateNode.ArrowRight; context.VLineTargetNode.TryGetValue(num, out value); num += 0.25f)
		{
			if (value == nextStateNode2)
			{
				break;
			}
		}
		float num2 = nextStateNode2.X + nextStateNode2.Width / 2f;
		float? midY = (from p in context.HLineTargetNode
			where p.Value == nextStateNode2
			select p.Key).Select((Func<float, float?>)((float k) => k)).FirstOrDefault();
		SubGraph subGraph = context.SubGraphs[stateNode.YIndex];
		if (!midY.HasValue)
		{
			float num3 = 0.25f;
			midY = subGraph.Y;
			context.Y += num3;
			graph.Height += num3;
			subGraph.MoveY(graph, num3);
			context.HLineTargetNode[midY.Value] = nextStateNode2;
			foreach (var (num5, value2) in context.HLineTargetNode.Where<KeyValuePair<float, MonsterStateNode>>((KeyValuePair<float, MonsterStateNode> p) => p.Key > midY.Value).ToList())
			{
				context.HLineTargetNode[num5 + num3] = value2;
				context.HLineTargetNode.Remove(num5);
			}
		}
		context.VLineTargetNode[num] = nextStateNode2;
		Arrow arrow = new Arrow(new float[7]
		{
			0f,
			stateNode.X + stateNode.Width,
			stateNode.Y + stateNode.Height / 2f,
			num,
			midY.Value,
			num2,
			nextStateNode2.Y + nextStateNode2.Height
		});
		if (stateNode.Parent != null && stateNode.Children == null)
		{
			arrow.Path[1] -= 0.1f;
		}
		AddArrow(graph, arrow, context, nextStateNode2, addSubGraph: false);
		context.SubGraphs[stateNode.YIndex].Arrows.Add((arrow, 0, 4));
		GetSubGraphByY(context.SubGraphs, midY.Value).Arrows.Add((arrow, 4, 2));
		context.SubGraphs[nextStateNode2.YIndex].Arrows.Add((arrow, 6, 1));
		if (num > graph.Width)
		{
			graph.Width = num;
		}
	}

	private SubGraph GetSubGraphByY(Dictionary<int, SubGraph> subGraphs, float value)
	{
		SubGraph result = subGraphs[0];
		SubGraph value2;
		for (int i = 0; subGraphs.TryGetValue(i, out value2); i++)
		{
			if (value2.Y >= value)
			{
				break;
			}
			result = value2;
		}
		return result;
	}

	private bool TryGetArrowOverride(MonsterStateNode stateNode, GraphGenerationContext context, [NotNullWhen(true)] out ArrowOverride? arrowOverride)
	{
		arrowOverride = null;
		if (stateNode.FullId != null)
		{
			IntentDefinition? intentDefinition = context.IntentDefinition;
			MoveReplacement value = default(MoveReplacement);
			if (intentDefinition != null && (intentDefinition.MoveReplacements?.TryGetValue(stateNode.FullId, out value)).GetValueOrDefault())
			{
				return (arrowOverride = value?.ArrowOverride) != null;
			}
		}
		return false;
	}

	private void AddArrow(Graph graph, Arrow arrow, GraphGenerationContext context, MonsterStateNode target, bool addSubGraph = true)
	{
		graph.Arrows.Add(arrow);
		context.ArrowTarget[arrow] = target;
		if (addSubGraph)
		{
			context.SubGraphs[target.YIndex].Arrows.Add((arrow, 0, arrow.Path.Length));
		}
	}

	private void AddPossiblePreviousMoveIds(List<Move> moves, GraphGenerationContext context)
	{
		List<Move> moves2 = moves;
		GraphGenerationContext context2 = context;
		List<Move> list = new List<Move>(moves2.Count);
		foreach (Move item in moves2)
		{
			MonsterStateNode valueOrDefault = context2.MoveToStateNode.GetValueOrDefault(item);
			if (valueOrDefault == null)
			{
				list.Add(item);
				continue;
			}
			HashSet<MonsterStateNode> valueOrDefault2 = context2.PreviousStateNodes.GetValueOrDefault(valueOrDefault);
			int?[] possiblePreviousMoveNodeIndices;
			if (valueOrDefault2 == null)
			{
				possiblePreviousMoveNodeIndices = ((!valueOrDefault.IsInitialState) ? null : new int?[1]);
			}
			else
			{
				HashSet<int?> hashSet = (from v in (from n in valueOrDefault2.SelectMany(delegate(MonsterStateNode n)
						{
							List<MonsterStateNode> list2 = new List<MonsterStateNode>();
							list2.Add(n);
							list2.AddRange(n.GetAllDescendants());
							return new _003C_003Ez__ReadOnlyList<MonsterStateNode>(list2);
						})
						where n.Children == null
						select n).Select((Func<MonsterStateNode, int?>)((MonsterStateNode n) => moves2.IndexOf(context2.StateNodeToMove.GetValueOrDefault(n))))
					where v.GetValueOrDefault() != -1 && v.HasValue
					select v).ToHashSet();
				if (valueOrDefault.IsInitialState)
				{
					int? num = null;
					HashSet<int?> hashSet2 = hashSet;
					int num2 = 0;
					int?[] array = new int?[1 + hashSet2.Count];
					array[num2] = num;
					num2++;
					foreach (int? item2 in hashSet2)
					{
						array[num2] = item2;
						num2++;
					}
					possiblePreviousMoveNodeIndices = array;
				}
				else
				{
					possiblePreviousMoveNodeIndices = hashSet.ToArray();
				}
			}
			list.Add(item with
			{
				PossiblePreviousMoveNodeIndices = possiblePreviousMoveNodeIndices
			});
		}
		moves2.Clear();
		moves2.AddRange(list);
	}

	private void TuneArrowPosition(List<Arrow> arrows, Dictionary<Arrow, MonsterStateNode> arrowTarget)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0272: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_029a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Unknown result type (might be due to invalid IL or missing references)
		//IL_021e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0239: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_031f: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < arrows.Count; i++)
		{
			Arrow arrow = arrows[i];
			for (int j = i + 1; j < arrows.Count; j++)
			{
				Arrow arrow2 = arrows[j];
				bool flag = arrowTarget[arrow] == arrowTarget[arrow2];
				foreach (var (flag2, val, val2, num) in ArrowSegments(arrow))
				{
					foreach (var (flag3, val3, val4, num2) in ArrowSegments(arrow2))
					{
						if (flag2 != flag3)
						{
							break;
						}
						if (flag2 && Math.Abs(val.Y - val3.Y) < 0.2f)
						{
							if (flag && Math.Abs(val2.X - val4.X) < 0.001f)
							{
								float num3 = (val.Y + val3.Y) / 2f;
								arrow.Path[num] = num3;
								arrow2.Path[num2] = num3;
								continue;
							}
							float val5 = Math.Min(val.X, val2.X);
							float val6 = Math.Min(val3.X, val4.X);
							float val7 = Math.Max(val.X, val2.X);
							float val8 = Math.Max(val3.X, val4.X);
							if (Math.Max(val5, val6) < Math.Min(val7, val8))
							{
								float num4 = (val.Y + val3.Y) / 2f;
								arrow.Path[num] = num4 + ((val.X < val3.X) ? (-0.15f) : 0.15f);
								arrow2.Path[num2] = num4 + ((val.X < val3.X) ? 0.15f : (-0.15f));
							}
						}
						if (flag2 || !((double)Math.Abs(val.X - val3.X) < 0.12))
						{
							continue;
						}
						if (flag && Math.Abs(val2.Y - val4.Y) < 0.001f)
						{
							float num5 = (val.X + val3.X) / 2f;
							arrow.Path[num] = num5;
							arrow2.Path[num2] = num5;
							continue;
						}
						float val9 = Math.Min(val.Y, val2.Y);
						float val10 = Math.Min(val3.Y, val4.Y);
						float val11 = Math.Max(val.Y, val2.Y);
						float val12 = Math.Max(val3.Y, val4.Y);
						if (Math.Max(val9, val10) < Math.Min(val11, val12))
						{
							float num6 = (val.X + val3.X) / 2f;
							arrow.Path[num] = num6 + ((val.Y < val3.Y) ? (-0.15f) : 0.15f);
							arrow2.Path[num2] = num6 + ((val.Y < val3.Y) ? 0.15f : (-0.15f));
						}
					}
				}
			}
		}
	}

	[IteratorStateMachine(typeof(_003CArrowSegments_003Ed__28))]
	private IEnumerable<(bool horizontal, Vector2 start, Vector2 end, int pathIndex)> ArrowSegments(Arrow arrow)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CArrowSegments_003Ed__28(-2)
		{
			_003C_003E3__arrow = arrow
		};
	}
}
