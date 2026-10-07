using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeleeAttack;

public static class AttackZoomEffect
{
	private class AttackZoomConfig
	{
		public bool Enabled { get; set; } = true;


		public float DamageThreshold { get; set; } = 30f;


		public float SlideInDuration { get; set; } = 0.3f;


		public float HoldDuration { get; set; } = 0f;


		public float SlideOutDuration { get; set; } = 0.3f;


		public float SlowMoScale { get; set; } = 0.5f;


		public float SlowDuration { get; set; } = 0.5f;


		public string[] ExcludedCards { get; set; } = new string[0];


		public float BlackoutFadeIn { get; set; } = 0.2f;


		public float BlackoutHold { get; set; } = 0.5f;


		public float BlackoutFadeOut { get; set; } = 0.3f;

	}

	private class PendingKill
	{
		public Creature Target;

		public float Damage;

		public CardModel CardSource;
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class BeforeDamagePatch
	{
		private static void Postfix(IRunState runState, ICombatState combatState, Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource)
		{
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Invalid comparison between Unknown and I4
			if (!IsEffectEnabled() || dealer == null || (int)dealer.Side != 1 || target == null || target.IsPlayer || cardSource == null || IsExcludedCard(cardSource))
			{
				return;
			}
			float num = (float)amount;
			if (num < _config.DamageThreshold || num < (float)target.CurrentHp)
			{
				return;
			}
			string cardId = ((AbstractModel)cardSource).Id.Entry;
			if (_bypassBlackoutCards.Contains(cardId))
			{
				return;
			}
			lock (_triggerLock)
			{
				if (_triggeredCardIds.Contains(cardId))
				{
					return;
				}
				_triggeredCardIds.Add(cardId);
			}
			MarkBlackoutPending(cardId);
			if (BlackBarEffect.IsEnabled)
			{
				_pendingHealthBarRestore = true;
			}
			BlackBarEffect.PerformFullEffect(null, _config.SlideInDuration, _config.HoldDuration, _config.SlideOutDuration);
			AttackBlackoutEffect.TriggerBlackout(_config.BlackoutFadeIn, _config.BlackoutHold, _config.BlackoutFadeOut);
			float effectTotalDuration = GetEffectTotalDuration();
			Task.Delay((int)(effectTotalDuration * 1000f)).ContinueWith(delegate
			{
				lock (_triggerLock)
				{
					_triggeredCardIds.Remove(cardId);
				}
			});
			TrySetPendingKill(target, num, cardSource);
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class DeathPatch
	{
		private static void Postfix(IRunState runState, ICombatState combatState, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
		{
			if (!IsEffectEnabled() || wasRemovalPrevented || creature == null || !creature.IsEnemy)
			{
				return;
			}
			try
			{
				NCombatRoom instance = NCombatRoom.Instance;
				if (instance != null)
				{
					NCreature creatureNode = instance.GetCreatureNode(creature);
					if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
					{
						Xue.FadeInHealthBar(creatureNode);
					}
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr("[AttackZoom] 死亡兜底恢复怪物血条失败: " + ex.Message);
			}
			PendingKill pendingKill = null;
			lock (_pendingLock)
			{
				if (_pendingKill != null && _pendingKill.Target == creature)
				{
					pendingKill = _pendingKill;
					_pendingKill = null;
				}
			}
			ConsumeBlackoutPending();
			BlackBarEffect.EndBlackBar(_config.SlideOutDuration);
			AttackBlackoutEffect.EndBlackout(_config.BlackoutFadeOut);
			if (pendingKill == null)
			{
				return;
			}
			string entry = ((AbstractModel)pendingKill.CardSource).Id.Entry;
			lock (_triggerLock)
			{
				if (!_triggeredCardIds.Contains(entry))
				{
					return;
				}
			}
			HitStopController.TriggerHighDamageHitStop();
			TriggerSlowMo();
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class ResetPatch
	{
		private static void Postfix()
		{
			lock (_slowLock)
			{
				_slowCts?.Cancel();
				_slowCts = null;
			}
			Engine.TimeScale = 1.0;
			ClearPendingKill();
			ClearPendingBlackout();
			_pendingHealthBarRestore = false;
			lock (_triggerLock)
			{
				_triggeredCardIds.Clear();
			}
			lock (_dedupLock)
			{
				_recentlyTriggered.Clear();
			}
			HitStopController.ResetHighDamageFlag();
		}
	}

	[HarmonyPatch(typeof(CardModel), "OnEnqueuePlayVfx")]
	private static class GrandFinaleCardEnqueueVfxPatch
	{
		private static void Prefix(CardModel __instance)
		{
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Invalid comparison between Unknown and I4
			if (!IsEffectEnabled())
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
			if (!_bypassBlackoutCards.Contains(entry))
			{
				return;
			}
			Player owner = __instance.Owner;
			Creature val = ((owner != null) ? owner.Creature : null);
			if (val != null && (int)val.Side == 1 && ShouldTriggerOnce(__instance))
			{
				MarkBlackoutPending(entry);
				if (BlackBarEffect.IsEnabled)
				{
					_pendingHealthBarRestore = true;
				}
				BlackBarEffect.PerformFullEffect(null, _config.SlideInDuration, _config.HoldDuration, _config.SlideOutDuration);
				AttackBlackoutEffect.TriggerBlackout(_config.BlackoutFadeIn, _config.BlackoutHold, _config.BlackoutFadeOut);
			}
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class AfterDamageReceivedPatch
	{
		private static void Postfix(object __instance, object choiceContext, object runState, object combatState, Creature target, DamageResult result, object props, Creature dealer, CardModel cardSource)
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Invalid comparison between Unknown and I4
			if (IsEffectEnabled() && dealer != null && (int)dealer.Side == 1)
			{
				ConsumeBlackoutPending();
				BlackBarEffect.EndBlackBar(_config.SlideOutDuration);
				AttackBlackoutEffect.EndBlackout(_config.BlackoutFadeOut);
			}
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class AfterCardPlayedPatch
	{
		private static void Postfix(object[] __args)
		{
			if (!IsEffectEnabled() || __args == null || __args.Length == 0)
			{
				return;
			}
			object obj = null;
			foreach (object obj2 in __args)
			{
				if (obj2 != null && obj2.GetType().Name == "CardPlay")
				{
					obj = obj2;
					break;
				}
			}
			if (obj != null)
			{
				string text = TryExtractCardId(obj);
				if (!string.IsNullOrEmpty(text) && IsBlackoutPendingForCard(text))
				{
					ConsumeBlackoutPending();
					_pendingHealthBarRestore = false;
					ForceEndBlackout("卡牌 " + text + " 结束但未收到伤害结算");
				}
			}
		}

		private static string TryExtractCardId(object cardPlay)
		{
			try
			{
				PropertyInfo property = cardPlay.GetType().GetProperty("Card");
				if (property == null)
				{
					return null;
				}
				object value = property.GetValue(cardPlay);
				if (value == null)
				{
					return null;
				}
				PropertyInfo property2 = value.GetType().GetProperty("Id");
				if (property2 == null)
				{
					return null;
				}
				object value2 = property2.GetValue(value);
				if (value2 == null)
				{
					return null;
				}
				PropertyInfo property3 = value2.GetType().GetProperty("Entry");
				if (property3 == null)
				{
					return null;
				}
				return property3.GetValue(value2) as string;
			}
			catch
			{
				return null;
			}
		}
	}

	private static readonly AttackZoomConfig _config;

	private static readonly HashSet<string> _bypassBlackoutCards;

	private static volatile bool _pendingHealthBarRestore;

	private static string _pendingBlackoutCardId;

	private static readonly object _pendingBlackoutLock;

	private static CancellationTokenSource _slowCts;

	private static readonly object _slowLock;

	private static PendingKill _pendingKill;

	private static readonly object _pendingLock;

	private static readonly HashSet<string> _triggeredCardIds;

	private static readonly object _triggerLock;

	private static readonly Dictionary<CardModel, float> _recentlyTriggered;

	private static readonly object _dedupLock;

	static AttackZoomEffect()
	{
		_bypassBlackoutCards = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "GRAND_FINALE" };
		_pendingHealthBarRestore = false;
		_pendingBlackoutCardId = null;
		_pendingBlackoutLock = new object();
		_slowCts = null;
		_slowLock = new object();
		_pendingKill = null;
		_pendingLock = new object();
		_triggeredCardIds = new HashSet<string>();
		_triggerLock = new object();
		_recentlyTriggered = new Dictionary<CardModel, float>();
		_dedupLock = new object();
		string location = typeof(AttackZoomEffect).Assembly.Location;
		string path = Path.GetDirectoryName(location) ?? ".";
		string path2 = Path.Combine(path, "MeleeAttack.json");
		try
		{
			if (File.Exists(path2))
			{
				string json = File.ReadAllText(path2);
				using JsonDocument jsonDocument = JsonDocument.Parse(json);
				if (jsonDocument.RootElement.TryGetProperty("AttackZoom", out var value))
				{
					_config = JsonSerializer.Deserialize<AttackZoomConfig>(value.GetRawText()) ?? new AttackZoomConfig();
				}
				else
				{
					_config = new AttackZoomConfig();
				}
			}
			else
			{
				_config = new AttackZoomConfig();
			}
		}
		catch
		{
			_config = new AttackZoomConfig();
		}
		BlackBarEffect.FadeOutCompleted += OnBlackBarFadeOutCompleted;
	}

	private static bool IsEffectEnabled()
	{
		if (!_config.Enabled)
		{
			return false;
		}
		return SettingsUI.IsZoomEffectEnabled();
	}

	private static void OnBlackBarFadeOutCompleted()
	{
		if (_pendingHealthBarRestore)
		{
			_pendingHealthBarRestore = false;
			RestorePlayerHealthBars();
		}
	}

	private static void RestorePlayerHealthBars()
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Invalid comparison between Unknown and I4
		try
		{
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance == null)
			{
				return;
			}
			foreach (NCreature creatureNode in instance.CreatureNodes)
			{
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) && creatureNode.Entity != null && (int)creatureNode.Entity.Side == 1 && creatureNode.Entity.IsAlive && !SimpleTeleportPatch.HasActiveAttackState(creatureNode))
				{
					Xue.FadeInHealthBar(creatureNode);
				}
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr("[AttackZoom][Fade] 异常: " + ex.Message);
		}
	}

	private static void MarkBlackoutPending(string cardId)
	{
		if (string.IsNullOrEmpty(cardId))
		{
			return;
		}
		lock (_pendingBlackoutLock)
		{
			_pendingBlackoutCardId = cardId;
		}
	}

	private static void ConsumeBlackoutPending()
	{
		lock (_pendingBlackoutLock)
		{
			_pendingBlackoutCardId = null;
		}
	}

	private static bool IsBlackoutPendingForCard(string cardId)
	{
		if (string.IsNullOrEmpty(cardId))
		{
			return false;
		}
		lock (_pendingBlackoutLock)
		{
			return !string.IsNullOrEmpty(_pendingBlackoutCardId) && _pendingBlackoutCardId.Equals(cardId, StringComparison.OrdinalIgnoreCase);
		}
	}

	private static void ClearPendingBlackout()
	{
		lock (_pendingBlackoutLock)
		{
			_pendingBlackoutCardId = null;
		}
	}

	private static async Task RunSlowMotion(CancellationToken token)
	{
		try
		{
			Engine.TimeScale = _config.SlowMoScale;
			await Task.Delay((int)(_config.SlowDuration * 1000f), token);
			Engine.TimeScale = 1.0;
		}
		catch (OperationCanceledException)
		{
			Engine.TimeScale = 1.0;
		}
		catch
		{
			Engine.TimeScale = 1.0;
		}
		finally
		{
			lock (_slowLock)
			{
				_slowCts = null;
			}
		}
	}

	private static void TriggerSlowMo()
	{
		lock (_slowLock)
		{
			_slowCts?.Cancel();
			_slowCts = new CancellationTokenSource();
			RunSlowMotion(_slowCts.Token);
		}
	}

	private static bool IsExcludedCard(CardModel card)
	{
		if (card != null)
		{
			ModelId id = ((AbstractModel)card).Id;
			if (((id != null) ? id.Entry : null) != null)
			{
				string entry = ((AbstractModel)card).Id.Entry;
				string[] excludedCards = _config.ExcludedCards;
				foreach (string value in excludedCards)
				{
					if (entry.Equals(value, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
				return false;
			}
		}
		return false;
	}

	private static void ClearPendingKill()
	{
		lock (_pendingLock)
		{
			_pendingKill = null;
		}
	}

	private static void TrySetPendingKill(Creature target, float damage, CardModel cardSource)
	{
		lock (_pendingLock)
		{
			_pendingKill = new PendingKill
			{
				Target = target,
				Damage = damage,
				CardSource = cardSource
			};
		}
		Task.Delay(1000).ContinueWith(delegate
		{
			lock (_pendingLock)
			{
				if (_pendingKill?.Target == target && _pendingKill?.CardSource == cardSource)
				{
					_pendingKill = null;
				}
			}
		});
	}

	private static bool ShouldTriggerOnce(CardModel card)
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

	private static float GetEffectTotalDuration()
	{
		float val = _config.SlideInDuration + _config.HoldDuration + _config.SlideOutDuration;
		float val2 = _config.BlackoutFadeIn + _config.BlackoutHold + _config.BlackoutFadeOut;
		return Math.Max(val, val2) + 0.3f;
	}

	private static void ForceRestorePlayerHealthBar(string logTag)
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Invalid comparison between Unknown and I4
		try
		{
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance == null)
			{
				return;
			}
			foreach (NCreature creatureNode in instance.CreatureNodes)
			{
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) && creatureNode.Entity != null && (int)creatureNode.Entity.Side == 1 && creatureNode.Entity.IsAlive && !SimpleTeleportPatch.HasActiveAttackState(creatureNode))
				{
					Xue.FadeInHealthBar(creatureNode);
				}
			}
		}
		catch (Exception ex)
		{
			GD.PrintErr("[AttackZoom][Fallback] 应急恢复异常: " + ex.Message);
		}
	}

	private static void ForceEndBlackout(string logTag)
	{
		try
		{
			BlackBarEffect.EndBlackBar(_config.SlideOutDuration);
			AttackBlackoutEffect.EndBlackout(_config.BlackoutFadeOut);
			ForceRestorePlayerHealthBar(logTag);
		}
		catch (Exception ex)
		{
			GD.PrintErr("[AttackZoom][Fallback] 应急异常: " + ex.Message);
		}
	}
}
