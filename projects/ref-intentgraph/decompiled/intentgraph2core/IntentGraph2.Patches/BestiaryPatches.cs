using System;
using Godot;
using HarmonyLib;
using IntentGraph2.Scenes;
using IntentGraph2.Utils;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;

namespace IntentGraph2.Patches;

public class BestiaryPatches
{
	[HarmonyPatch(typeof(NBestiary), "_Ready")]
	public static class BestiaryReadyPatch
	{
		public static void Postfix(NBestiary __instance)
		{
			try
			{
				if (intentGraphBestiary != null)
				{
					GodotTreeExtensions.QueueFreeSafely((Node)(object)intentGraphBestiary);
					intentGraphBestiary = null;
				}
				intentGraphBestiary = PreloadManager.Cache.GetScene("res://intentgraph2/scenes/intent_graph_bestiary.tscn").Instantiate<NIntentGraphBestiary>((GenEditState)0);
				((Node)__instance).AddChild((Node)(object)intentGraphBestiary, false, (InternalMode)0);
			}
			catch (Exception value)
			{
				IgLogger.Error($"Failed to load IntentGraph Bestiary scene: {value}");
			}
		}
	}

	[HarmonyPatch(typeof(NBestiary), "SelectMonster")]
	public static class BestiarySelectMonsterPatch
	{
		public static void Postfix(NBestiary __instance, NBestiaryEntry entry)
		{
			if (intentGraphBestiary == null)
			{
				return;
			}
			MonsterModel monsterModel = entry.Entry.monsterModel;
			if (monsterModel != null)
			{
				intentGraphBestiary.Monster = monsterModel.CanonicalInstance;
				return;
			}
			EncounterModel encounterModel = entry.Entry.encounterModel;
			if (encounterModel != null)
			{
				intentGraphBestiary.Encounter = encounterModel.CanonicalInstance;
				return;
			}
			intentGraphBestiary.Monster = null;
			intentGraphBestiary.Encounter = null;
		}
	}

	private static NIntentGraphBestiary? intentGraphBestiary;

	public static void ReloadIntents()
	{
		if (!GodotObject.IsInstanceValid((GodotObject)(object)intentGraphBestiary))
		{
			intentGraphBestiary = null;
		}
		if (intentGraphBestiary != null)
		{
			intentGraphBestiary.GenerateGraphs();
		}
	}
}
