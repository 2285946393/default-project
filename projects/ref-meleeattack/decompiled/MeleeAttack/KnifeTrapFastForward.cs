using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace MeleeAttack;

public static class KnifeTrapFastForward
{
	[HarmonyPatch(typeof(CardModel), "OnPlayWrapper")]
	private static class KnifeTrapPlayPatch
	{
		private static void Prefix(CardModel __instance)
		{
			if (!SettingsUI.IsKnifeTrapEnabled())
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
			string text = (string)obj;
			if (!string.IsNullOrEmpty(text) && text.Equals("KNIFE_TRAP", StringComparison.OrdinalIgnoreCase))
			{
				Player owner = __instance.Owner;
				Creature val = ((owner != null) ? owner.Creature : null);
				if (val != null)
				{
					StartFastForward(val);
				}
			}
		}
	}

	[HarmonyPatch(typeof(CardModel), "OnPlayWrapper")]
	private static class ShivPlayPatch
	{
		private static void Prefix(CardModel __instance)
		{
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
			string text = (string)obj;
			if (!string.IsNullOrEmpty(text) && text.Equals("SHIV", StringComparison.OrdinalIgnoreCase))
			{
				OnShivPlayed();
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	private static class ResetPatch
	{
		private static void Postfix()
		{
			if (IsKnifeTrapOrigin)
			{
				IsKnifeTrapOrigin = false;
				RestoreTimeScale();
			}
			RemainingShivs = 0;
			lock (_lockObj)
			{
				if (_timeoutCts != null)
				{
					_timeoutCts.Cancel();
					_timeoutCts.Dispose();
					_timeoutCts = null;
				}
			}
		}
	}

	private const double FAST_SPEED = 2.0;

	private const double TIMEOUT_SECONDS = 0.4;

	internal static bool IsKnifeTrapOrigin = false;

	internal static int RemainingShivs = 0;

	private static double _originalTimeScale = 1.0;

	private static CancellationTokenSource _timeoutCts = null;

	private static readonly object _lockObj = new object();

	private static void RestoreTimeScale()
	{
		lock (_lockObj)
		{
			if (_timeoutCts != null)
			{
				_timeoutCts.Cancel();
				_timeoutCts.Dispose();
				_timeoutCts = null;
			}
			Engine.TimeScale = _originalTimeScale;
		}
	}

	private static void StartTimeoutTimer()
	{
		lock (_lockObj)
		{
			if (_timeoutCts != null)
			{
				_timeoutCts.Cancel();
				_timeoutCts.Dispose();
				_timeoutCts = null;
			}
			_timeoutCts = new CancellationTokenSource();
			CancellationToken token = _timeoutCts.Token;
			Task.Run(async delegate
			{
				try
				{
					await Task.Delay(400, token);
					if (IsKnifeTrapOrigin && RemainingShivs > 0)
					{
						IsKnifeTrapOrigin = false;
						RemainingShivs = 0;
						RestoreTimeScale();
					}
				}
				catch (OperationCanceledException)
				{
				}
			});
		}
	}

	internal static async void OnAllShivsProcessed()
	{
		NCombatRoom room = NCombatRoom.Instance;
		if (room == null || !GodotObject.IsInstanceValid((GodotObject)(object)room))
		{
			await Task.Delay(50);
		}
		else
		{
			await ((GodotObject)room).ToSignal((GodotObject)(object)((Node)room).GetTree(), StringName.op_Implicit("process_frame"));
		}
		IsKnifeTrapOrigin = false;
		RestoreTimeScale();
	}

	private static int CountShivsInConsumePile(Creature creature)
	{
		try
		{
			Player player = creature.Player;
			if (player == null)
			{
				return 0;
			}
			CardPile pile = PileTypeExtensions.GetPile((PileType)4, player);
			if (pile == null)
			{
				return 0;
			}
			return pile.Cards.Count(delegate(CardModel card)
			{
				ModelId id = ((AbstractModel)card).Id;
				return ((id != null) ? id.Entry : null) == "SHIV";
			});
		}
		catch (Exception ex)
		{
			GD.PrintErr("[KnifeTrap] 获取消耗牌堆失败: " + ex.Message + "，使用默认值 3");
			return 3;
		}
	}

	private static void StartFastForward(Creature player)
	{
		if (player != null)
		{
			if (IsKnifeTrapOrigin)
			{
				IsKnifeTrapOrigin = false;
				RestoreTimeScale();
			}
			int num = CountShivsInConsumePile(player);
			if (num != 0)
			{
				_originalTimeScale = Engine.TimeScale;
				Engine.TimeScale = 2.0;
				IsKnifeTrapOrigin = true;
				RemainingShivs = num;
				StartTimeoutTimer();
			}
		}
	}

	private static void OnShivPlayed()
	{
		if (IsKnifeTrapOrigin && RemainingShivs > 0)
		{
			RemainingShivs--;
			StartTimeoutTimer();
			if (RemainingShivs == 0)
			{
				OnAllShivsProcessed();
			}
		}
	}
}
