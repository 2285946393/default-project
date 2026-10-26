using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using IntentGraph2.Models;
using IntentGraph2.Patches;
using IntentGraph2.Scenes;
using IntentGraph2.Utils;
using IntentGraph2.Utils.GraphGenerator;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace IntentGraph2.DevConsole;

public class ExportIntentsConsoleCmd : AbstractConsoleCmd
{
	public override string CmdName => "exportintents";

	public override string Args => string.Empty;

	public override string Description => "Export intent graphs as image.";

	public override bool IsNetworked => false;

	public override CmdResult Process(Player? issuingPlayer, string[] args)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		List<Task> tasks = new List<Task>();
		try
		{
			HasAscensionLevelPatches.OverwriteAsensionLevel = (AscensionLevel)0;
			GenerateGraphForAllEncounters(tasks);
			HasAscensionLevelPatches.OverwriteAsensionLevel = (AscensionLevel)9;
			GenerateGraphForAllEncounters(tasks, "a9_");
		}
		finally
		{
			HasAscensionLevelPatches.OverwriteAsensionLevel = null;
		}
		return new CmdResult(Task.WhenAll(tasks).ContinueWith(delegate
		{
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			OS.ShellShowInFileManager(Path.Combine(OS.GetUserDataDir(), "intentgraphs"), true);
		}), true, "Intent graph images exported");
	}

	private static void GenerateGraphForAllEncounters(List<Task> tasks, string prefix = "")
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		HashSet<Graph> hashSet = new HashSet<Graph>();
		Dictionary<Graph, (MonsterModel monster, string suffix)> graphMetadata = new Dictionary<Graph, (MonsterModel, string)>();
		foreach (EncounterModel allEncounter in ModelDb.AllEncounters)
		{
			EncounterModel val = allEncounter.ToMutable();
			new Traverse((object)val).Field("_rng").SetValue((object)Rng.Chaotic);
			val.GenerateMonstersWithSlots((IRunState)(object)NullRunState.Instance);
			int num = 0;
			foreach (var monstersWithSlot in val.MonstersWithSlots)
			{
				MonsterModel item = monstersWithSlot.Item1;
				string item2 = monstersWithSlot.Item2;
				num++;
				GenerateGraph(item, val, hashSet, graphMetadata, num, item2);
			}
			foreach (MonsterModel allPossibleMonster in allEncounter.AllPossibleMonsters)
			{
				IReadOnlyList<string> readOnlyList = allEncounter.Slots;
				if (readOnlyList.Count == 0)
				{
					readOnlyList = new List<string> { "" };
				}
				num = 0;
				foreach (string item3 in readOnlyList)
				{
					num++;
					GenerateGraph(allPossibleMonster.ToMutable(), val, hashSet, graphMetadata, num, item3);
				}
			}
		}
		HashSet<string> hashSet2 = (from g in (from g in hashSet
				select graphMetadata[g] into x
				group x by x.monster.Title.GetFormattedText() into g
				where g.Count() == 1
				select g).SelectMany((IGrouping<string, (MonsterModel monster, string suffix)> g) => g)
			select g.monster.Title.GetFormattedText()).ToHashSet();
		foreach (Graph item4 in hashSet)
		{
			var (val2, suffix) = graphMetadata[item4];
			if (hashSet2.Contains(val2.Title.GetFormattedText()))
			{
				suffix = "";
			}
			tasks.Add(ExportGraphAsImageAsync(item4, val2, prefix, suffix));
		}
	}

	private static void GenerateGraph(MonsterModel monsterModel, EncounterModel encounter, HashSet<Graph> addedGraphs, Dictionary<Graph, (MonsterModel monster, string suffix)> graphMetadata, int index, string? slot)
	{
		Graph graph = IntentGraphGenerator.GenerateGraphForBestiary(monsterModel, encounter, slot);
		if (graph != null && !addedGraphs.Contains(graph))
		{
			addedGraphs.Add(graph);
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(2, 2);
			defaultInterpolatedStringHandler.AppendLiteral("_");
			defaultInterpolatedStringHandler.AppendFormatted(encounter.Title.GetFormattedText());
			defaultInterpolatedStringHandler.AppendLiteral("_");
			defaultInterpolatedStringHandler.AppendFormatted(string.IsNullOrEmpty(slot) ? ((object)index) : slot);
			graphMetadata[graph] = (monsterModel, defaultInterpolatedStringHandler.ToStringAndClear());
		}
	}

	private static async Task ExportGraphAsImageAsync(Graph graph, MonsterModel monster, string prefix, string suffix)
	{
		if (NGame.Instance == null)
		{
			return;
		}
		PackedScene scene = PreloadManager.Cache.GetScene("res://intentgraph2/scenes/intent_graph.tscn");
		NIntentGraph intentGraph = scene.Instantiate<NIntentGraph>((GenEditState)0);
		SubViewport subViewport = new SubViewport();
		try
		{
			intentGraph.Graph = graph;
			intentGraph.Monster = monster;
			((Control)intentGraph).Position = new Vector2(8f, 8f);
			subViewport.Size = new Vector2I((int)Math.Ceiling(((Control)intentGraph).CustomMinimumSize.X + 16f), (int)Math.Ceiling(((Control)intentGraph).CustomMinimumSize.Y + 16f));
			subViewport.RenderTargetUpdateMode = (UpdateMode)4;
			((Viewport)subViewport).TransparentBg = true;
			GodotTreeExtensions.AddChildSafely((Node)(object)subViewport, (Node)(object)intentGraph);
			GodotTreeExtensions.AddChildSafely((Node)(object)NGame.Instance, (Node)(object)subViewport);
			await ((GodotObject)intentGraph).ToSignal((GodotObject)(object)RenderingServer.Singleton, SignalName.FramePostDraw);
			SaveAsPng(intentGraph, $"user://intentgraphs/{prefix}{monster.Title.GetFormattedText()}{suffix}.png");
		}
		catch (Exception value)
		{
			IgLogger.Error($"Failed to export intent graph for {monster.Title}: {value}");
		}
		finally
		{
			GodotTreeExtensions.QueueFreeSafely((Node)(object)subViewport);
		}
	}

	public static void SaveAsPng(NIntentGraph intentGraph, string filePath)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		string baseDir = StringExtensions.GetBaseDir(filePath);
		Error val;
		if (!DirAccess.DirExistsAbsolute(baseDir))
		{
			val = DirAccess.MakeDirAbsolute(baseDir);
			if ((int)val != 0)
			{
				throw new Exception($"Failed to create directory {baseDir}: {val}.");
			}
		}
		val = ((Texture2D)((Node)intentGraph).GetViewport().GetTexture()).GetImage().SavePng(filePath);
		if ((int)val != 0)
		{
			throw new Exception($"Failed to save intent graph to {filePath}: {val}.");
		}
	}
}
