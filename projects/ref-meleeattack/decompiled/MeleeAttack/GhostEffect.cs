using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class GhostEffect
{
	private class TimeWrapper
	{
		public long Value;
	}

	[HarmonyPatch(typeof(CardModel), "OnEnqueuePlayVfx")]
	private static class CardOnPlayPatch
	{
		private static void Prefix(CardModel __instance)
		{
			if (!SettingsUI.IsBlurEnabled())
			{
				return;
			}
			object obj;
			if (__instance == null)
			{
				obj = null;
			}
			else
			{
				ModelId id = ((AbstractModel)__instance).Id;
				obj = ((id != null) ? id.Entry : null);
			}
			if (obj == null)
			{
				return;
			}
			string entry = ((AbstractModel)__instance).Id.Entry;
			if (!entry.Equals("BLUR", StringComparison.OrdinalIgnoreCase) || !ShouldTrigger(__instance))
			{
				return;
			}
			Player owner = __instance.Owner;
			Creature val = ((owner != null) ? owner.Creature : null);
			if (val != null)
			{
				if (_activeCts.TryGetValue(val, out var value))
				{
					value?.Cancel();
					value?.Dispose();
					_activeCts.Remove(val);
				}
				_startTime.Remove(val);
				CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
				_activeCts.Add(val, cancellationTokenSource);
				_startTime.Add(val, new TimeWrapper
				{
					Value = (long)Time.GetTicksMsec()
				});
				ContinuousGhostTrail(val, cancellationTokenSource.Token);
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class NCombatRoomReadyPatch
	{
		private static void Postfix()
		{
			foreach (KeyValuePair<Creature, CancellationTokenSource> item in (IEnumerable<KeyValuePair<Creature, CancellationTokenSource>>)_activeCts)
			{
				item.Value?.Cancel();
				item.Value?.Dispose();
			}
			_activeCts.Clear();
			_startTime.Clear();
			_recentlyTriggered.Clear();
		}
	}

	private const float GENERATION_DURATION = 0.5f;

	private const float GHOST_INTERVAL = 0.1f;

	private const float MOVE_DISTANCE = 200f;

	private const float MIN_MOVE_DURATION = 0.5f;

	private const float MAX_MOVE_DURATION = 1f;

	private const float FADE_DURATION = 0.5f;

	private const float GHOST_ALPHA = 0.5f;

	private static readonly int TOTAL_GHOSTS = 5;

	private static readonly ConditionalWeakTable<Creature, TimeWrapper> _startTime = new ConditionalWeakTable<Creature, TimeWrapper>();

	private static readonly ConditionalWeakTable<Creature, CancellationTokenSource> _activeCts = new ConditionalWeakTable<Creature, CancellationTokenSource>();

	private static readonly Dictionary<CardModel, float> _recentlyTriggered = new Dictionary<CardModel, float>();

	private static readonly object _dedupLock = new object();

	private static bool ShouldTrigger(CardModel card)
	{
		if (card == null)
		{
			return false;
		}
		float num = (float)((double)Time.GetTicksMsec() / 1000.0);
		lock (_dedupLock)
		{
			if (_recentlyTriggered.TryGetValue(card, out var value) && num - value < 0.2f)
			{
				return false;
			}
			_recentlyTriggered[card] = num;
			if (_recentlyTriggered.Count > 32)
			{
				List<CardModel> list = new List<CardModel>();
				foreach (KeyValuePair<CardModel, float> item in _recentlyTriggered)
				{
					if (num - item.Value > 1f)
					{
						list.Add(item.Key);
					}
				}
				foreach (CardModel item2 in list)
				{
					_recentlyTriggered.Remove(item2);
				}
			}
			return true;
		}
	}

	private static async Task ContinuousGhostTrail(Creature creature, CancellationToken token)
	{
		try
		{
			if (!_startTime.TryGetValue(creature, out var tw))
			{
				return;
			}
			long startTime = tw.Value;
			int spawnIndex = 0;
			while (!token.IsCancellationRequested)
			{
				long now = (long)Time.GetTicksMsec();
				if ((float)(now - startTime) >= 500f)
				{
					break;
				}
				NCreature node = creature.GetCreatureNode();
				if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
				{
					float progress = ((TOTAL_GHOSTS > 1) ? ((float)spawnIndex / (float)(TOTAL_GHOSTS - 1)) : 0f);
					float moveDuration = Mathf.Lerp(0.5f, 1f, progress);
					float distance = 200f;
					GhostSpawner.SpawnGhostSimple(node, 0.5f, 0.5f, distance, moveDuration);
					spawnIndex++;
				}
				await Task.Delay(100, token);
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex3)
		{
			Exception ex = ex3;
			Log.Warn("[GhostEffect] ContinuousGhostTrail error: " + ex.Message, 2);
		}
		finally
		{
			if (_activeCts.TryGetValue(creature, out var cts))
			{
				_activeCts.Remove(creature);
				try
				{
					cts.Dispose();
				}
				catch
				{
				}
			}
			_startTime.Remove(creature);
		}
	}
}
