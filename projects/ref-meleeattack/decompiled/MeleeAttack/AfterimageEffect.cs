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

public static class AfterimageEffect
{
	private class CounterWrapper
	{
		public int Value;
	}

	private class TimeWrapper
	{
		public long Value;
	}

	[HarmonyPatch(typeof(CardModel), "OnEnqueuePlayVfx")]
	private static class CardOnPlayPatch
	{
		private static void Prefix(CardModel __instance)
		{
			if (!SettingsUI.IsAfterimageEnabled())
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
			if (obj == null || !((AbstractModel)__instance).Id.Entry.Contains("AfterImage", StringComparison.OrdinalIgnoreCase) || !ShouldTrigger(__instance))
			{
				return;
			}
			Player owner = __instance.Owner;
			Creature val = ((owner != null) ? owner.Creature : null);
			if (val == null)
			{
				return;
			}
			long ticksMsec = (long)Time.GetTicksMsec();
			if (!_activeCts.TryGetValue(val, out var _))
			{
				CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
				_activeCts.Add(val, cancellationTokenSource);
				_ghostCounter.Add(val, new CounterWrapper
				{
					Value = 0
				});
				_activationTime.Add(val, new TimeWrapper
				{
					Value = ticksMsec
				});
				_lastPlayTime.Add(val, new TimeWrapper
				{
					Value = ticksMsec
				});
				ContinuousGhostTrail(val, cancellationTokenSource.Token);
				return;
			}
			long num = 0L;
			if (_lastPlayTime.TryGetValue(val, out var value2))
			{
				num = value2.Value;
			}
			bool flag = ticksMsec - num >= 1000;
			_lastPlayTime.Remove(val);
			_lastPlayTime.Add(val, new TimeWrapper
			{
				Value = ticksMsec
			});
			if (flag)
			{
				_activationTime.Remove(val);
				_activationTime.Add(val, new TimeWrapper
				{
					Value = ticksMsec
				});
				_ghostCounter.Remove(val);
				_ghostCounter.Add(val, new CounterWrapper
				{
					Value = 0
				});
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
			_ghostCounter.Clear();
			_activationTime.Clear();
			_lastPlayTime.Clear();
			_recentlyTriggered.Clear();
		}
	}

	private const float GHOST_INTERVAL_OUTSIDE = 0.2f;

	private const float GHOST_INTERVAL_INSIDE = 0.1f;

	private const float GHOST_ALPHA = 0.5f;

	private const float ACTIVE_WINDOW = 1f;

	private const float MOVE_DISTANCE = 100f;

	private const float MOVE_DURATION = 0.5f;

	private const float FADE_DURATION_OUT = 0.3f;

	private const long PERFORMANCE_COOLDOWN_MS = 1000L;

	private static readonly ConditionalWeakTable<Creature, CounterWrapper> _ghostCounter = new ConditionalWeakTable<Creature, CounterWrapper>();

	private static readonly ConditionalWeakTable<Creature, TimeWrapper> _activationTime = new ConditionalWeakTable<Creature, TimeWrapper>();

	private static readonly ConditionalWeakTable<Creature, CancellationTokenSource> _activeCts = new ConditionalWeakTable<Creature, CancellationTokenSource>();

	private static readonly ConditionalWeakTable<Creature, TimeWrapper> _lastPlayTime = new ConditionalWeakTable<Creature, TimeWrapper>();

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
			while (!token.IsCancellationRequested)
			{
				float interval = 0.2f;
				bool inWindow = false;
				if (_activationTime.TryGetValue(creature, out var timeWrapper))
				{
					long startTicks = timeWrapper.Value;
					long now = (long)Time.GetTicksMsec();
					long elapsed = now - startTicks;
					if ((float)elapsed < 1000f)
					{
						interval = 0.1f;
						inWindow = true;
					}
				}
				NCreature node = creature.GetCreatureNode();
				if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
				{
					if (inWindow)
					{
						if (!GhostSpawner.ShouldSkipSpawn(node))
						{
							int seq = 0;
							if (_ghostCounter.TryGetValue(creature, out var counterWrapper))
							{
								seq = ++counterWrapper.Value;
							}
							int moveDir = ((seq % 2 == 1) ? 1 : (-1));
							float distance = (float)moveDir * 100f;
							GhostSpawner.SpawnGhostWithReturn(node, 0.5f, 1f, distance, 0.5f, 0.5f, null, isPassive: true);
							counterWrapper = null;
						}
					}
					else if (!GhostSpawner.IsPassiveBlocked() && !GhostSpawner.ShouldSkipSpawn(node))
					{
						GhostSpawner.SpawnGhostFadeOnly(node, 0.5f, 0.3f, null, isPassive: true);
					}
				}
				await Task.Delay((int)(interval * 1000f), token);
				timeWrapper = null;
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex3)
		{
			Exception ex = ex3;
			Log.Warn("[AfterimageEffect] ContinuousGhostTrail error: " + ex.Message, 2);
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
			_ghostCounter.Remove(creature);
			_activationTime.Remove(creature);
			_lastPlayTime.Remove(creature);
		}
	}
}
