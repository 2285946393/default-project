using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Modifiers;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.CustomRun;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]
[assembly: AssemblyCompany("SpirePvp")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("0.2.5.0")]
[assembly: AssemblyInformationalVersion("0.2.5")]
[assembly: AssemblyProduct("SpirePvp")]
[assembly: AssemblyTitle("SpirePvp")]
[assembly: AssemblyVersion("0.2.5.0")]
[module: RefSafetyRules(11)]
namespace SpirePvp;

public sealed record DuelHealthSnapshot(Player Player, decimal Hp, decimal MaxHp)
{
	public static DuelHealthSnapshot Capture(Player p)
	{
		return new DuelHealthSnapshot(p, p.Creature.CurrentHp, p.Creature.MaxHp);
	}

	public async Task Restore()
	{
		await CreatureCmd.SetMaxHp(Player.Creature, MaxHp);
		await CreatureCmd.SetCurrentHp(Player.Creature, Hp);
	}
}
internal static class BossDuel
{
	internal static bool Active;

	internal static bool Fighting;

	internal static bool Bypass;

	internal static DuelMultipliers Multipliers = DuelMultipliers.Default;

	private static CancellationTokenSource? _cancellation;

	private static TaskCompletionSource? _combatDone;

	private static Control? _overlay;

	internal static bool Eligible(IRunState? state)
	{
		return false;
	}

	internal static Task Begin(RunManager manager)
	{
		return Task.CompletedTask;
	}

	private static async Task Transition(RunManager manager)
	{
		RunState state = PvpSession.GetState(manager);
		_cancellation = new CancellationTokenSource();
		manager.NetService.Disconnected += OnDisconnected;
		CancellationToken token = _cancellation.Token;
		DuelHealthSnapshot[] health = null;
		bool participated = false;
		try
		{
			Player me = LocalContext.GetMe((IPlayerCollection)(object)state);
			bool isHost = (int)manager.NetService.Type != 3;
			if ((await PvpVotes.Exchange(manager.PlayerChoiceSynchronizer, state.Players, me, () => Task.FromResult(isHost && PvpSettings.Current.BossDuels), token)).Any((bool enabled) => enabled))
			{
				Multipliers = await PvpVotes.HostMultipliers(manager.PlayerChoiceSynchronizer, state.Players, me, isHost, isHost ? PvpSettings.Current.Multipliers : DuelMultipliers.Default, token);
				NOverlayStack instance = NOverlayStack.Instance;
				if (instance != null)
				{
					instance.Clear();
				}
				bool[] source = await PvpVotes.Exchange(manager.PlayerChoiceSynchronizer, state.Players, me, () => Prompt("幕间 PvP · 可选决斗", $"生命 ×{Multipliers.Health:0.#} · 格挡 ×{Multipliers.Block:0.#}，双方同意才开打。\n胜者获得一个遗物、一瓶药水和一次精英卡牌奖励。\n结束后恢复入场前生命与生命上限，继续下一幕。药水照常消耗。", "参加决斗", "跳过，继续爬塔", token), token);
				ClosePrompt();
				if (source.All((bool yes) => yes))
				{
					health = state.Players.Select(DuelHealthSnapshot.Capture).ToArray();
					Active = true;
					Fighting = true;
					participated = true;
					PvpSession.Result = DuelOutcome.InProgress;
					PvpSession.Winner = null;
					PvpSession.PreparingRound = true;
					_combatDone = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
					await RoundSynchronizer.WaitForBoth(manager.PlayerChoiceSynchronizer, state.Players, me, 0, token);
					await manager.FadeOut();
					await manager.EnterRoom((AbstractRoom)new MapRoom());
					NMapScreen instance2 = NMapScreen.Instance;
					if (instance2 != null)
					{
						instance2.Close(false);
					}
					CombatRoom val = new CombatRoom(((EncounterModel)ModelDb.Encounter<PvpArena>()).ToMutable(), (IRunState)(object)state);
					val.set_ShouldResumeParentEventAfterCombat(false);
					await manager.EnterRoomWithoutExitingCurrentRoom((AbstractRoom)val, true);
					await _combatDone.Task.WaitAsync(token);
					DuelHealthSnapshot[] array = health;
					for (int i = 0; i < array.Length; i++)
					{
						await array[i].Restore();
					}
					health = null;
					await PvpVotes.Exchange(manager.PlayerChoiceSynchronizer, state.Players, me, () => Prompt((!PvpSession.Winner.HasValue) ? "决斗平局" : ((PvpSession.Winner == me.NetId) ? "决斗胜利" : "决斗失败"), (!PvpSession.Winner.HasValue) ? "双方生命已恢复。平局不发放奖励，继续爬塔。" : "双方生命已恢复。胜者领取奖励后，双方继续下一幕。", "继续", null, token), token);
					ClosePrompt();
					ulong? winner = PvpSession.Winner;
					if (winner.HasValue)
					{
						ulong winnerId = winner.GetValueOrDefault();
						Player val2 = state.Players.Single((Player p) => p.NetId == winnerId);
						if (val2 != me)
						{
							ShowWaiting("等待胜者领取决斗奖励…");
						}
						await new RewardsSet(val2, (RewardsSetSynchronizer)null).WithCustomRewards(CreateRewards(val2)).Offer().WaitAsync(token);
						ClosePrompt();
					}
					await RoundSynchronizer.WaitForBoth(manager.PlayerChoiceSynchronizer, state.Players, me, 0, token);
					Active = false;
				}
			}
			token.ThrowIfCancellationRequested();
			Bypass = true;
			Task task;
			try
			{
				task = manager.EnterNextAct();
			}
			finally
			{
				Bypass = false;
			}
			await task;
			if (participated && state == PvpSession.GetState(manager))
			{
				await SaveManager.Instance.SaveRun((AbstractRoom)null, true);
			}
		}
		finally
		{
			try
			{
				ClosePrompt();
				if (health != null && PvpSession.GetState(manager) == state)
				{
					DuelHealthSnapshot[] array = health;
					for (int i = 0; i < array.Length; i++)
					{
						await array[i].Restore();
					}
				}
			}
			finally
			{
				Active = false;
				Fighting = false;
				PvpSession.PreparingRound = false;
				_combatDone = null;
				manager.NetService.Disconnected -= OnDisconnected;
				_cancellation?.Dispose();
				_cancellation = null;
			}
		}
	}

	internal static List<Reward> CreateRewards(Player winner)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Expected O, but got Unknown
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Expected O, but got Unknown
		int num = 3;
		List<Reward> list = new List<Reward>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<Reward> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = (Reward)new RelicReward(winner);
		num2++;
		span[num2] = (Reward)new PotionReward(winner);
		num2++;
		span[num2] = (Reward)new CardReward(CardCreationOptions.ForRoom(winner, (RoomType)2).WithFlags((CardCreationFlags)256), 3, winner, (PlayerChoiceSynchronizer)null);
		return list;
	}

	internal static void CombatFinished()
	{
		Fighting = false;
		_combatDone?.TrySetResult();
	}

	internal static void CombatFailed(Exception error)
	{
		_combatDone?.TrySetException(error);
	}

	private static void OnDisconnected(NetErrorInfo error)
	{
		_cancellation?.Cancel();
	}

	internal static void Reset()
	{
		Multipliers = DuelMultipliers.Default;
		_cancellation?.Cancel();
		_combatDone?.TrySetCanceled();
		ClosePrompt();
		Active = false;
		Fighting = false;
		Bypass = false;
	}

	private static Task<bool> Prompt(string title, string text, string accept, string? decline, CancellationToken token)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		TaskCompletionSource<bool> decision = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		VBoxContainer box = ShowPanel(title, text);
		HBoxContainer val = new HBoxContainer
		{
			Alignment = (AlignmentMode)1
		};
		((Node)box).AddChild((Node)(object)val, false, (InternalMode)0);
		Button yes = new Button
		{
			Text = accept,
			CustomMinimumSize = new Vector2(300f, 64f)
		};
		Button no = ((decline == null) ? ((Button)null) : new Button
		{
			Text = decline,
			CustomMinimumSize = new Vector2(300f, 64f)
		});
		((BaseButton)yes).Pressed += delegate
		{
			Choose(value: true);
		};
		((Node)val).AddChild((Node)(object)yes, false, (InternalMode)0);
		if (no != null)
		{
			((BaseButton)no).Pressed += delegate
			{
				Choose(value: false);
			};
			((Node)val).AddChild((Node)(object)no, false, (InternalMode)0);
		}
		((Control)yes).GrabFocus();
		return decision.Task.WaitAsync(token);
		void Choose(bool value)
		{
			((BaseButton)yes).Disabled = true;
			if (no != null)
			{
				((BaseButton)no).Disabled = true;
			}
			((Node)box).GetNode<Label>(NodePath.op_Implicit("Description")).Text = "等待另一名玩家确认…";
			decision.TrySetResult(value);
		}
	}

	private static void ShowWaiting(string text)
	{
		ShowPanel("幕间 PvP", text);
	}

	private static VBoxContainer ShowPanel(string title, string text)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected O, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Expected O, but got Unknown
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Expected O, but got Unknown
		ClosePrompt();
		if (NRun.Instance == null)
		{
			throw new InvalidOperationException("找不到当前游戏画面。");
		}
		_overlay = (Control?)new PanelContainer
		{
			Name = StringName.op_Implicit("SpirePvpInterlude"),
			ZIndex = 300,
			MouseFilter = (MouseFilterEnum)0
		};
		((Node)NRun.Instance).AddChild((Node)(object)_overlay, false, (InternalMode)0);
		_overlay.SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		CenterContainer val = new CenterContainer();
		((Node)_overlay).AddChild((Node)(object)val, false, (InternalMode)0);
		VBoxContainer val2 = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(850f, 200f)
		};
		((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("separation"), 32);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		Label val3 = new Label
		{
			Text = title,
			HorizontalAlignment = (HorizontalAlignment)1
		};
		((Control)val3).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 40);
		((Node)val2).AddChild((Node)(object)val3, false, (InternalMode)0);
		((Node)val2).AddChild((Node)new Label
		{
			Name = StringName.op_Implicit("Description"),
			Text = text,
			HorizontalAlignment = (HorizontalAlignment)1,
			AutowrapMode = (AutowrapMode)3
		}, false, (InternalMode)0);
		return val2;
	}

	private static void ClosePrompt()
	{
		if (_overlay != null && GodotObject.IsInstanceValid((GodotObject)(object)_overlay))
		{
			((CanvasItem)_overlay).Visible = false;
			((Node)_overlay).QueueFree();
		}
		_overlay = null;
	}
}
[HarmonyPatch(typeof(Hook), "ShouldStopCombatFromEnding")]
internal static class ArenaVictoryPatch
{
	private static bool Prefix(ICombatState combatState, ref bool __result)
	{
		if (!PvpSession.IsArena(combatState))
		{
			return true;
		}
		__result = combatState.Players.Count == 2 && combatState.Players.All((Player p) => p.Creature.IsAlive);
		return false;
	}
}
[HarmonyPatch(typeof(Hook), "BeforeCombatStart")]
internal static class ArenaFortitudePatch
{
	private static void Postfix(ICombatState? combatState, ref Task __result)
	{
		if (PvpSession.IsArena(combatState))
		{
			__result = Apply(__result, combatState);
		}
	}

	private static async Task Apply(Task original, ICombatState state)
	{
		await original;
		foreach (Player player in state.Players)
		{
			await PowerCmd.Apply<PvpFortitudePower>((PlayerChoiceContext)new BlockingPlayerChoiceContext(), player.Creature, 1m, (Creature)null, (CardModel)null, false);
		}
	}
}
[HarmonyPatch(typeof(NCombatUi), "OnCombatWon")]
internal static class ArenaProceedPatch
{
	private static bool Prefix(CombatRoom room)
	{
		return !PvpSession.IsArena((ICombatState?)(object)room.CombatState);
	}
}
[HarmonyPatch(typeof(NMapScreen), "Open")]
internal static class InterludeMapOpenPatch
{
	private static bool Prefix()
	{
		return !BossDuel.Active;
	}
}
[HarmonyPatch(typeof(NMapScreen), "SetTravelEnabled")]
internal static class InterludeMapTravelPatch
{
	private static void Prefix(ref bool enabled)
	{
		if (BossDuel.Active)
		{
			enabled = false;
		}
	}
}
[HarmonyPatch]
internal static class InterludeSavePatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(SaveManager), "SaveRun", new Type[2]
		{
			typeof(AbstractRoom),
			typeof(bool)
		}, (Type[])null);
	}

	private static bool Prefix(ref Task __result)
	{
		if (!BossDuel.Active)
		{
			return true;
		}
		__result = Task.CompletedTask;
		return false;
	}
}
[HarmonyPatch(typeof(Hook), "AfterCombatVictory")]
internal static class InterludeVictoryRewardsPatch
{
	private static bool Prefix(ICombatState? combatState, ref Task __result)
	{
		if (!BossDuel.Active || !PvpSession.IsArena(combatState))
		{
			return true;
		}
		__result = Task.CompletedTask;
		return false;
	}
}
[HarmonyPatch(typeof(CombatManager), "SetUpCombat")]
internal static class SetupPatch
{
	private static void Postfix(CombatState state)
	{
		if (!PvpSession.IsArena((ICombatState?)(object)state))
		{
			return;
		}
		foreach (Creature creature in state.Creatures)
		{
			Player val = creature.Player ?? creature.PetOwner;
			if (val != null)
			{
				creature.CombatState = (ICombatState)(object)new PvpCombatView(state, val);
			}
		}
		PvpUi.SetupCombat(state);
	}
}
[HarmonyPatch(typeof(CombatState), "AddCreature")]
internal static class PetViewPatch
{
	private static void Prefix(CombatState __instance, Creature creature)
	{
		if (creature.CombatState is PvpCombatView pvpCombatView && pvpCombatView.State == __instance)
		{
			creature.CombatState = (ICombatState)(object)__instance;
		}
	}

	private static void Postfix(CombatState __instance, Creature creature)
	{
		Player val = creature.Player ?? creature.PetOwner;
		if (PvpSession.IsArena((ICombatState?)(object)__instance) && val != null)
		{
			creature.CombatState = (ICombatState)(object)new PvpCombatView(__instance, val);
		}
	}
}
[HarmonyPatch(typeof(CombatState), "RemoveCreature")]
internal static class RemoveViewPatch
{
	private static void Prefix(CombatState __instance, Creature creature, out PvpCombatView? __state)
	{
		__state = creature.CombatState as PvpCombatView;
		if (__state?.State == __instance)
		{
			creature.CombatState = (ICombatState)(object)__instance;
		}
	}

	private static Exception? Finalizer(Creature creature, PvpCombatView? __state, Exception? __exception)
	{
		if (__state != null && creature.CombatState == __state.State)
		{
			creature.CombatState = (ICombatState)(object)__state;
		}
		return __exception;
	}
}
[HarmonyPatch]
internal static class AddCardViewPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(CombatState), "AddCard", new Type[1] { typeof(CardModel) }, (Type[])null);
	}

	private static void Prefix(CombatState __instance, CardModel card, out PvpCombatView? __state)
	{
		Player owner = card.Owner;
		__state = ((owner != null) ? owner.Creature.CombatState : null) as PvpCombatView;
		if (__state?.State == __instance && card.Owner != null)
		{
			card.Owner.Creature.CombatState = (ICombatState)(object)__instance;
		}
	}

	private static Exception? Finalizer(CardModel card, PvpCombatView? __state, Exception? __exception)
	{
		if (__state != null)
		{
			Player owner = card.Owner;
			if (((owner != null) ? owner.Creature.CombatState : null) == __state.State)
			{
				card.Owner.Creature.CombatState = (ICombatState)(object)__state;
			}
		}
		return __exception;
	}
}
[HarmonyPatch(typeof(CombatManager), "LoseCombat")]
internal static class PvpLossPatch
{
	private static bool Prefix()
	{
		if (!PvpSession.IsPvp((IRunState?)(object)PvpSession.GetState(RunManager.Instance)))
		{
			return !BossDuel.Active;
		}
		return false;
	}
}
[HarmonyPatch(typeof(AttackCommand), "GetPossibleTargets")]
internal static class PetAttackPatch
{
	private static bool Prefix(AttackCommand __instance, ref IReadOnlyList<Creature> __result)
	{
		Creature attacker = __instance.Attacker;
		if (((attacker != null) ? attacker.PetOwner : null) == null || !PvpSession.IsArena(__instance.Attacker.CombatState))
		{
			return true;
		}
		object? value = AccessTools.Field(typeof(AttackCommand), "_combatState").GetValue(__instance);
		ICombatState val = (ICombatState)((value is ICombatState) ? value : null);
		if (val == null)
		{
			return true;
		}
		__result = val.GetOpponentsOf(__instance.Attacker);
		return false;
	}
}
[HarmonyPatch(typeof(SaveManager), "UpdateProgressAfterCombatWon")]
internal static class PvpProgressPatch
{
	private static bool Prefix(CombatRoom combatRoom)
	{
		return !PvpSession.IsArena((ICombatState?)(object)combatRoom.CombatState);
	}
}
[HarmonyPatch(typeof(CardModel), "IsValidTarget")]
internal static class CardTargetPatch
{
	private static bool Prefix(CardModel __instance, Creature? target, ref bool __result)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Invalid comparison between Unknown and I4
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Invalid comparison between Unknown and I4
		Player owner = __instance.Owner;
		if (!PvpSession.IsArena((owner != null) ? owner.Creature.CombatState : null))
		{
			return true;
		}
		if ((int)__instance.TargetType == 2)
		{
			Player val = ((target != null) ? target.Player : null) ?? ((target != null) ? target.PetOwner : null);
			__result = target != null && target.IsAlive && val != null && val != __instance.Owner && PvpTargets.IsEnemyTarget(target);
			return false;
		}
		if ((int)__instance.TargetType == 6)
		{
			__result = target != null && target.IsAlive && (target.Player ?? target.PetOwner) == __instance.Owner;
			return false;
		}
		return true;
	}
}
[HarmonyPatch(typeof(NTargetManager), "AllowedToTargetCreature")]
internal static class UiTargetPatch
{
	private static bool Prefix(NTargetManager __instance, Creature creature, ref bool __result)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Invalid comparison between Unknown and I4
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Invalid comparison between Unknown and I4
		if (!PvpSession.IsArena(creature.CombatState))
		{
			return true;
		}
		TargetType val = (TargetType)AccessTools.Field(typeof(NTargetManager), "_validTargetsType").GetValue(__instance);
		if (((int)val != 2 && (int)val != 6) || 1 == 0)
		{
			return true;
		}
		Player me = LocalContext.GetMe(creature.CombatState);
		Player val2 = creature.Player ?? creature.PetOwner;
		__result = creature.IsAlive && val2 != null && (((int)val != 2) ? (val2 == me) : (val2 != me && PvpTargets.IsEnemyTarget(creature)));
		return false;
	}
}
[HarmonyPatch(typeof(CombatManager), "StartTurn")]
internal static class RoundStartPatch
{
	private static bool Prefix(CombatManager __instance, object turnState, ref Task __result)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Invalid comparison between Unknown and I4
		CombatState val = PvpSession.TurnState(turnState);
		if (!PvpSession.IsArena((ICombatState?)(object)val))
		{
			return true;
		}
		if ((int)val.CurrentSide == 2)
		{
			__result = PvpSession.CallTask(typeof(CombatManager), "EndEnemyTurn", __instance, turnState);
			return false;
		}
		PvpSession.PreparingRound = true;
		PvpUi.SetStatus("双方正在恢复能量、抽牌…");
		return true;
	}

	private static void Postfix(object turnState, ref Task __result)
	{
		if (PvpSession.IsArena((ICombatState?)(object)PvpSession.TurnState(turnState)) && PvpSession.PreparingRound)
		{
			__result = AfterSetup(__result, turnState);
		}
	}

	private static async Task AfterSetup(Task original, object turn)
	{
		await original;
		if ((int)PvpSession.TurnState(turn).CurrentSide == 1 && PvpSession.TurnLive(turn))
		{
			await PvpSession.RoundBarrier(turn);
		}
	}
}
[HarmonyPatch(typeof(CombatManager), "RunAutoPrePlayPhase")]
internal static class HoldPlayPhasePatch
{
	private static void Postfix(Player player, ref Task __result)
	{
		if (PvpSession.IsArena(player.Creature.CombatState))
		{
			__result = Hold(__result, player);
		}
	}

	private static async Task Hold(Task original, Player player)
	{
		await original;
		if (PvpSession.PreparingRound && player.PlayerCombatState != null)
		{
			player.PlayerCombatState.Phase = (PlayerTurnPhase)1;
		}
	}
}
[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class InputGatePatch
{
	private static void Postfix(ref bool __result)
	{
		if ((PvpSession.IsPvp((IRunState?)(object)PvpSession.GetState(RunManager.Instance)) || BossDuel.Active) && PvpSession.PreparingRound)
		{
			__result = true;
		}
	}
}
[HarmonyPatch(typeof(CombatManager), "SwitchSides")]
internal static class SharedExtraTurnPatch
{
	private static void Prefix(object turnState)
	{
		CombatState val = PvpSession.TurnState(turnState);
		if (PvpSession.IsArena((ICombatState?)(object)val))
		{
			List<Player> list = (List<Player>)AccessTools.Property(turnState.GetType(), "PlayersTakingExtraTurn").GetValue(turnState);
			if (list.Count != 0)
			{
				list.Clear();
				list.AddRange(val.Players);
			}
		}
	}
}
[HarmonyPatch]
internal static class CombatEndPatch
{
	private static MethodBase TargetMethod()
	{
		return typeof(CombatManager).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).Single((MethodInfo m) => m.Name == "EndCombatInternal" && m.GetParameters().Length == 1);
	}

	private static void Prefix(object turnState)
	{
		CombatState state = PvpSession.TurnState(turnState);
		if (PvpSession.IsArena((ICombatState?)(object)state))
		{
			PvpSession.CaptureResult(state);
		}
	}

	private static void Postfix(object turnState, ref Task __result)
	{
		if (PvpSession.IsArena((ICombatState?)(object)PvpSession.TurnState(turnState)))
		{
			__result = Show(__result);
		}
	}

	private static async Task Show(Task original)
	{
		try
		{
			await original;
		}
		catch (Exception error)
		{
			if (BossDuel.Active)
			{
				BossDuel.CombatFailed(error);
			}
			throw;
		}
		PvpSession.PreparingRound = false;
		if (BossDuel.Active)
		{
			BossDuel.CombatFinished();
		}
		else
		{
			PvpUi.ShowResult();
		}
	}
}
internal static class DraftBrowsing
{
	internal static bool OpeningDeck;

	internal static bool Preserve(IOverlayScreen? screen)
	{
		if (PvpSession.SelectingDecks)
		{
			if (!OpeningDeck)
			{
				NCapstoneContainer instance = NCapstoneContainer.Instance;
				if (!(((instance != null) ? instance.CurrentCapstoneScreen : null) is NDeckViewScreen))
				{
					goto IL_0026;
				}
			}
			return Pending(screen);
		}
		goto IL_0026;
		IL_0026:
		return false;
	}

	internal static bool Pending(IOverlayScreen? screen)
	{
		if ((!(screen is NCardRewardSelectionScreen) && !(screen is NCardGridSelectionScreen)) || 1 == 0)
		{
			return false;
		}
		object value = AccessTools.Field(((object)screen).GetType(), "_completionSource").GetValue(screen);
		if (value?.GetType().GetProperty("Task")?.GetValue(value) is Task task)
		{
			return !task.IsCompleted;
		}
		return true;
	}
}
[HarmonyPatch(typeof(NCapstoneContainer), "Close")]
internal static class ResumeDraftAfterBrowsingPatch
{
	private static void Postfix()
	{
		if (!PvpSession.SelectingDecks)
		{
			return;
		}
		NOverlayStack instance = NOverlayStack.Instance;
		if (DraftBrowsing.Pending((instance != null) ? instance.Peek() : null))
		{
			NMapScreen instance2 = NMapScreen.Instance;
			if (instance2 != null)
			{
				instance2.Close(false);
			}
			NOverlayStack instance3 = NOverlayStack.Instance;
			if (instance3 != null)
			{
				instance3.ShowOverlays();
			}
		}
	}
}
[HarmonyPatch(typeof(NDeckViewScreen), "ShowScreen")]
internal static class BrowseDraftDeckPatch
{
	[HarmonyPriority(800)]
	private static void Prefix(out bool __state)
	{
		__state = DraftBrowsing.OpeningDeck;
		if (PvpSession.SelectingDecks)
		{
			DraftBrowsing.OpeningDeck = true;
		}
	}

	private static void Finalizer(bool __state)
	{
		DraftBrowsing.OpeningDeck = __state;
	}
}
[HarmonyPatch(typeof(NOverlayStack), "Clear")]
internal static class PreserveDraftStackPatch
{
	private static bool Prefix(NOverlayStack __instance)
	{
		return !DraftBrowsing.Preserve(__instance.Peek());
	}
}
[HarmonyPatch(typeof(NOverlayStack), "Remove")]
internal static class PreserveDraftSelectionPatch
{
	private static bool Prefix(IOverlayScreen screen)
	{
		return !DraftBrowsing.Preserve(screen);
	}
}
public static class DraftCompletion
{
	public static async Task WaitForBoth(IReadOnlyList<Task> selections, int localIndex, Action localReady)
	{
		Task all = Task.WhenAll(selections);
		await selections[localIndex];
		localReady();
		await all;
	}
}
public readonly record struct DuelPlacement(float X, float Y, bool Mirror);
public static class DuelLayout
{
	public static DuelPlacement Place(ulong viewer, ulong owner, int petIndex = -1)
	{
		bool flag = viewer == owner;
		float num = (flag ? 0.25f : 0.75f);
		if (petIndex < 0)
		{
			return new DuelPlacement(num, 0.55f, !flag);
		}
		float num2 = (flag ? 1f : (-1f));
		int num3 = petIndex % 2;
		int num4 = petIndex / 2;
		return new DuelPlacement(num + num2 * (0.085f + (float)num3 * 0.065f), 0.57f - (float)num4 * 0.065f, !flag);
	}
}
public sealed class DuelMap : ActMap
{
	private readonly MapPoint?[,] _grid = new MapPoint[7, 2];

	protected override MapPoint?[,] Grid => _grid;

	public override MapPoint BossMapPoint { get; } = new MapPoint(3, 0)
	{
		PointType = (MapPointType)7,
		CanBeModified = false
	};


	public override MapPoint StartingMapPoint { get; } = new MapPoint(3, -1)
	{
		PointType = (MapPointType)8,
		CanBeModified = false
	};


	public DuelMap()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Expected O, but got Unknown
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		base.startMapPoints.Add(((ActMap)this).BossMapPoint);
	}
}
public sealed record DuelMultipliers(decimal Health, decimal Block, decimal Summon = 1.5m, decimal Doom = 0.5m, decimal Poison = 1m, bool Barricade = true)
{
	public static DuelMultipliers Default { get; } = new DuelMultipliers(2m, 1.5m);


	public int HealthSteps => (int)(Health * 2m);

	public int BlockSteps => (int)(Block * 2m);

	public int SummonSteps => (int)(Summon * 2m);

	public int DoomSteps => (int)(Doom * 2m);

	public int PoisonSteps => (int)(Poison * 2m);

	public int Encode()
	{
		return ((((HealthSteps * 21 + BlockSteps) * 21 + SummonSteps) * 21 + DoomSteps) * 21 + PoisonSteps) * 2 + (Barricade ? 1 : 0);
	}

	public static decimal Normalize(decimal value, decimal fallback)
	{
		if (!(value >= 0.5m) || !(value <= 10m))
		{
			return fallback;
		}
		return Math.Round(value * 2m, 0, MidpointRounding.AwayFromZero) / 2m;
	}

	public static decimal NormalizeEffect(decimal value, decimal fallback)
	{
		if (!(value >= 0m) || !(value <= 10m))
		{
			return fallback;
		}
		return Math.Round(value * 2m, 0, MidpointRounding.AwayFromZero) / 2m;
	}

	public static DuelMultipliers FromSteps(int health, int block, int summon = 3, int doom = 1, int poison = 2, bool barricade = true)
	{
		bool flag = ((health < 1 || health > 20) ? true : false);
		bool flag2 = flag;
		if (!flag2)
		{
			bool flag3 = ((block < 1 || block > 20) ? true : false);
			flag2 = flag3;
		}
		bool flag4 = flag2;
		if (!flag4)
		{
			bool flag3 = ((summon < 0 || summon > 20) ? true : false);
			flag4 = flag3;
		}
		bool flag5 = flag4;
		if (!flag5)
		{
			bool flag3 = ((doom < 0 || doom > 20) ? true : false);
			flag5 = flag3;
		}
		bool flag6 = flag5;
		if (!flag6)
		{
			bool flag3 = ((poison < 0 || poison > 20) ? true : false);
			flag6 = flag3;
		}
		if (flag6)
		{
			throw new InvalidOperationException("PvP 倍率数据超出允许范围。");
		}
		return new DuelMultipliers((decimal)health / 2m, (decimal)block / 2m, (decimal)summon / 2m, (decimal)doom / 2m, (decimal)poison / 2m, barricade);
	}

	public static DuelMultipliers Decode(int encoded)
	{
		if (encoded < 0)
		{
			throw new InvalidOperationException("PvP 倍率数据无效。");
		}
		bool barricade = encoded % 2 != 0;
		int num = encoded / 2;
		int poison = num % 21;
		num /= 21;
		int doom = num % 21;
		num /= 21;
		int summon = num % 21;
		num /= 21;
		return FromSteps(num / 21, num % 21, summon, doom, poison, barricade);
	}

	public static DuelMultipliers FromModifier(PvpDuel duel)
	{
		return FromSteps(duel.HealthSteps, duel.BlockSteps, duel.SummonSteps, duel.DoomSteps, duel.PoisonSteps, duel.Barricade);
	}

	public static DuelMultipliers ForRun(IRunState? run)
	{
		if (((run != null) ? ((IEnumerable<ModifierModel>)run.Modifiers).FirstOrDefault((Func<ModifierModel, bool>)((ModifierModel m) => m is PvpDuel)) : null) is PvpDuel duel)
		{
			return FromModifier(duel);
		}
		if (!BossDuel.Active)
		{
			return Default;
		}
		return BossDuel.Multipliers;
	}

	[CompilerGenerated]
	private DuelMultipliers(DuelMultipliers original)
	{
		Health = original.Health;
		Block = original.Block;
		Summon = original.Summon;
		Doom = original.Doom;
		Poison = original.Poison;
		Barricade = original.Barricade;
	}
}
public enum DuelOutcome
{
	InProgress,
	FirstWins,
	SecondWins,
	Draw
}
public static class DuelRules
{
	public static DuelOutcome Outcome(bool firstAlive, bool secondAlive)
	{
		if (firstAlive)
		{
			if (secondAlive)
			{
				return DuelOutcome.InProgress;
			}
			return DuelOutcome.FirstWins;
		}
		if (secondAlive)
		{
			return DuelOutcome.SecondWins;
		}
		return DuelOutcome.Draw;
	}

	public static bool Opponents(ulong owner, ulong target)
	{
		return owner != target;
	}
}
[HarmonyPatch(typeof(RunManager), "EnterAct")]
internal static class SkipTowerPatch
{
	private static bool Prefix(RunManager __instance, ref Task __result)
	{
		if (!PvpSession.IsPvp((IRunState?)(object)PvpSession.GetState(__instance)))
		{
			return true;
		}
		__result = PvpSession.Prepare(__instance);
		return false;
	}
}
[HarmonyPatch(typeof(NGame), "StartNewMultiplayerRun")]
internal static class NoPvpSavePatch
{
	private static void Prefix(IReadOnlyList<ModifierModel> modifiers, ref bool shouldSave)
	{
		if (modifiers.Any((ModifierModel m) => m is PvpDuel))
		{
			shouldSave = false;
		}
	}
}
[HarmonyPatch(typeof(RunManager), "CleanUp")]
internal static class CleanupPatch
{
	private static void Postfix()
	{
		BossDuel.Reset();
		PvpSession.HostingPvp = false;
		PvpSession.PreparingRound = false;
		PvpSession.SelectingDecks = false;
		PvpSession.LocalDeckReady = false;
		DraftBrowsing.OpeningDeck = false;
		PvpSession.Winner = null;
		PvpSession.Result = DuelOutcome.InProgress;
	}
}
[HarmonyPatch]
internal static class FortitudeIconPatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		return new MethodInfo[2]
		{
			AccessTools.PropertyGetter(typeof(PowerModel), "PackedIconPath"),
			AccessTools.PropertyGetter(typeof(PowerModel), "ResolvedBigIconPath")
		};
	}

	private static bool Prefix(PowerModel __instance, MethodBase __originalMethod, ref string __result)
	{
		if (!(__instance is PvpFortitudePower))
		{
			return true;
		}
		UnmovablePower val = ModelDb.Power<UnmovablePower>();
		__result = ((__originalMethod.Name == "get_PackedIconPath") ? ((PowerModel)val).PackedIconPath : ((PowerModel)val).ResolvedBigIconPath);
		return false;
	}
}
[HarmonyPatch]
internal static class CombatLayoutPatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		return new MethodInfo[2]
		{
			AccessTools.Method(typeof(NCombatRoom), "AdjustCreatureScaleForAspectRatio", (Type[])null, (Type[])null),
			AccessTools.Method(typeof(NCombatRoom), "AddCreature", (Type[])null, (Type[])null)
		};
	}

	private static void Postfix(NCombatRoom __instance)
	{
		object? value = AccessTools.Field(typeof(NCombatRoom), "_visuals").GetValue(__instance);
		CombatRoom val = (CombatRoom)((value is CombatRoom) ? value : null);
		if (val != null && PvpSession.IsArena((ICombatState?)(object)val.CombatState))
		{
			PvpUi.PositionPlayers(__instance);
		}
	}
}
[HarmonyPatch(typeof(NCreature), "_Ready")]
internal static class CreatureStatePatch
{
	private static void Postfix(NCreature __instance)
	{
		if (PvpSession.IsArena(__instance.Entity.CombatState))
		{
			PvpUi.ShowCreatureState(__instance);
		}
	}
}
[HarmonyPatch(typeof(NCreature), "UpdatePhobiaMode")]
internal static class BodyVariantPatch
{
	private static void Postfix(NCreature __instance)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (!PvpSession.IsArena(__instance.Entity.CombatState) || NCombatRoom.Instance == null)
		{
			return;
		}
		NCombatRoom room = NCombatRoom.Instance;
		Callable val = Callable.From((Action)delegate
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)room) && ((Node)room).IsInsideTree())
			{
				PvpUi.PositionPlayers(room);
			}
		});
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
	}
}
[HarmonyPatch(typeof(NCreature), "OstyScaleToSize")]
internal static class PetScaleLayoutPatch
{
	private static void Postfix(NCreature __instance)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		if (!PvpSession.IsArena(__instance.Entity.CombatState))
		{
			return;
		}
		object? value = AccessTools.Field(typeof(NCreature), "_scaleTween").GetValue(__instance);
		Tween val = (Tween)((value is Tween) ? value : null);
		if (val == null)
		{
			return;
		}
		val.TweenCallback(Callable.From((Action)delegate
		{
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null && GodotObject.IsInstanceValid((GodotObject)(object)instance) && ((Node)instance).IsInsideTree())
			{
				PvpUi.PositionPlayers(instance);
			}
		}));
	}
}
[HarmonyPatch]
internal static class LocalizationPatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		return new string[2] { "GetRawText", "GetFormattedText" }.Select((string name) => AccessTools.Method(typeof(LocString), name, (Type[])null, (Type[])null));
	}

	private static bool Prefix(LocString __instance, ref string __result)
	{
		string locTable = __instance.LocTable;
		if ((!(locTable == "modifiers") && !(locTable == "powers")) || 1 == 0)
		{
			return true;
		}
		__result = __instance.LocEntryKey switch
		{
			"SPIRE_PVP.title" => "PvP · 构筑决斗", 
			"SPIRE_PVP.description" => "双方同步回合、抽牌和恢复能量，自由抢速出牌。", 
			"SPIRE_PVP_DRAFT15.title" => "PvP 选牌：15 张", 
			"SPIRE_PVP_DRAFT15.description" => "15 次三选一，清空初始牌组。", 
			"SPIRE_PVP_DRAFT20.title" => "PvP 选牌：20 张", 
			"SPIRE_PVP_DRAFT20.description" => "20 次三选一，清空初始牌组。", 
			"SPIRE_PVP_FORTITUDE.title" => "决斗坚韧", 
			"SPIRE_PVP_FORTITUDE.description" => FortitudeDescription(), 
			_ => "", 
		};
		return __result.Length == 0;
	}

	private static string FortitudeDescription()
	{
		DuelMultipliers duelMultipliers = DuelMultipliers.ForRun((IRunState?)(object)PvpSession.GetState(RunManager.Instance));
		return $"开战时最大生命与当前生命变为 {duelMultipliers.Health:0.#} 倍。获得的格挡变为 {duelMultipliers.Block:0.#} 倍，召唤量变为 {duelMultipliers.Summon:0.#} 倍，可与其他能力牌的效果叠加。" + (duelMultipliers.Barricade ? "回合交替时格挡不会消失。" : "本状态不保留回合间格挡。") + $"受到的新增灾厄层数变为 {duelMultipliers.Doom:0.#} 倍，新增中毒层数变为 {duelMultipliers.Poison:0.#} 倍；减少或清除层数的效果不变。小数沿用原版向下取整；存活玩家缩放后的生命至少为 1。持续整场决斗，不叠加。";
	}
}
[HarmonyPatch(typeof(RunManager), "GenerateMap")]
internal static class DuelMapGenerationPatch
{
	private static bool Prefix(RunManager __instance, ref Task __result)
	{
		RunState state = PvpSession.GetState(__instance);
		if (!PvpSession.IsPvp((IRunState?)(object)state))
		{
			return true;
		}
		__instance.MapSelectionSynchronizer.BeforeMapGenerated();
		state.Map = (ActMap)(object)new DuelMap();
		state.RemoveStaleVisitedMapCoords(state.Map);
		NMapScreen instance = NMapScreen.Instance;
		if (instance != null)
		{
			instance.SetMap(state.Map, state.Rng.Seed, true);
		}
		__result = Task.CompletedTask;
		return false;
	}
}
[HarmonyPatch(typeof(RunManager), "CreateRoom")]
internal static class DuelRoomPatch
{
	private static bool Prefix(RunManager __instance, RoomType roomType, ref AbstractRoom __result)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		RunState state = PvpSession.GetState(__instance);
		if (!PvpSession.IsPvp((IRunState?)(object)state) || (int)roomType != 3)
		{
			return true;
		}
		__result = (AbstractRoom)new CombatRoom(((EncounterModel)ModelDb.Encounter<PvpArena>()).ToMutable(), (IRunState)(object)state);
		return false;
	}
}
[HarmonyPatch(typeof(NMapScreen), "SetMap")]
internal static class DuelMapAppearancePatch
{
	private static void Postfix(NMapScreen __instance, ActMap map)
	{
		if (map is DuelMap)
		{
			object? value = AccessTools.Field(typeof(NMapScreen), "_startingPointNode").GetValue(__instance);
			Control val = (Control)((value is Control) ? value : null);
			if (val != null)
			{
				((CanvasItem)val).Visible = false;
			}
			AccessTools.Field(typeof(NMapScreen), "_hasPlayedAnimation").SetValue(__instance, true);
			DuelMapView.Position(__instance);
		}
	}
}
internal static class DuelMapView
{
	internal static bool IsDuel(NMapScreen screen)
	{
		return AccessTools.Field(typeof(NMapScreen), "_map").GetValue(screen) is DuelMap;
	}

	internal static void Position(NMapScreen screen)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		object? value = AccessTools.Field(typeof(NMapScreen), "_bossPointNode").GetValue(screen);
		Control val = (Control)((value is Control) ? value : null);
		if (val != null)
		{
			Vector2 val2 = ((CanvasItem)screen).GetGlobalTransform() * (((Control)screen).Size * new Vector2(0.46f, 0.47f));
			Transform2D globalTransform = ((CanvasItem)val).GetGlobalTransform();
			val.GlobalPosition = val2 - ((Transform2D)(ref globalTransform)).BasisXform(val.Size * 0.5f);
		}
	}

	internal static void UpdateStatus(NMapScreen screen)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Expected O, but got Unknown
		Label val = ((Node)screen).GetNodeOrNull<Label>(NodePath.op_Implicit("SpirePvpMapStatus"));
		if (val == null)
		{
			val = new Label
			{
				Name = StringName.op_Implicit("SpirePvpMapStatus"),
				MouseFilter = (MouseFilterEnum)2,
				HorizontalAlignment = (HorizontalAlignment)1,
				AutowrapMode = (AutowrapMode)3
			};
			((Control)val).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 26);
			((Node)screen).AddChild((Node)(object)val, false, (InternalMode)0);
		}
		((Control)val).Position = new Vector2(((Control)screen).Size.X * 0.16f, ((Control)screen).Size.Y * 0.72f);
		((Control)val).Size = new Vector2(((Control)screen).Size.X * 0.6f, 100f);
		val.Text = (PvpSession.SelectingDecks ? "构筑已完成，等待对手完成选牌与同步…" : "双方构筑已完成，请各自点击决斗图标进入战斗");
		((CanvasItem)val).Visible = !PvpSession.SelectingDecks || PvpSession.LocalDeckReady;
	}
}
[HarmonyPatch(typeof(NMapScreen), "Open")]
internal static class CenterDuelMapOnOpenPatch
{
	private static void Postfix(NMapScreen __instance)
	{
		if (DuelMapView.IsDuel(__instance))
		{
			DuelMapView.Position(__instance);
			DuelMapView.UpdateStatus(__instance);
		}
	}
}
[HarmonyPatch(typeof(NMapScreen), "_Process")]
internal static class FixedDuelMapViewportPatch
{
	private static bool Prefix(NMapScreen __instance)
	{
		if (!DuelMapView.IsDuel(__instance))
		{
			return true;
		}
		if (((CanvasItem)__instance).IsVisibleInTree())
		{
			DuelMapView.Position(__instance);
		}
		return false;
	}
}
[HarmonyPatch(typeof(NMapScreen), "RecalculateTravelability")]
internal static class DuelMapTravelPatch
{
	private static void Postfix(NMapScreen __instance)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		if (AccessTools.Field(typeof(NMapScreen), "_map").GetValue(__instance) is DuelMap)
		{
			object? value = AccessTools.Field(typeof(NMapScreen), "_startingPointNode").GetValue(__instance);
			NMapPoint val = (NMapPoint)((value is NMapPoint) ? value : null);
			if (val != null)
			{
				val.State = (MapPointState)3;
			}
			object? value2 = AccessTools.Field(typeof(NMapScreen), "_bossPointNode").GetValue(__instance);
			NMapPoint val2 = (NMapPoint)((value2 is NMapPoint) ? value2 : null);
			if (val2 != null)
			{
				RunState state = PvpSession.GetState(RunManager.Instance);
				val2.State = (MapPointState)(state.VisitedMapCoords.Contains(val2.Point.coord) ? 2 : ((!PvpSession.SelectingDecks) ? 1 : 3));
			}
		}
	}
}
[HarmonyPatch(typeof(NMapScreen), "SetTravelEnabled")]
internal static class LockDraftMapPatch
{
	private static void Prefix(NMapScreen __instance, ref bool enabled)
	{
		if (PvpSession.SelectingDecks && PvpSession.IsPvp((IRunState?)(object)PvpSession.GetState(RunManager.Instance)))
		{
			enabled = false;
		}
	}

	private static void Postfix(NMapScreen __instance)
	{
		if (DuelMapView.IsDuel(__instance))
		{
			AccessTools.Method(typeof(NMapScreen), "RecalculateTravelability", (Type[])null, (Type[])null).Invoke(__instance, null);
			__instance.RefreshAllPointVisuals();
			DuelMapView.UpdateStatus(__instance);
		}
	}
}
[HarmonyPatch(typeof(NMultiplayerHostSubmenu), "_Ready")]
internal static class HostMenuPatch
{
	private static void Postfix(NMultiplayerHostSubmenu __instance)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected O, but got Unknown
		NMultiplayerHostSubmenu __instance2 = __instance;
		if (!((Node)__instance2).HasNode(NodePath.op_Implicit("SpirePvpButton")))
		{
			Control node = ((Node)__instance2).GetNode<Control>(NodePath.op_Implicit("CustomRunButton"));
			Button button = new Button
			{
				Name = StringName.op_Implicit("SpirePvpButton"),
				Text = "PvP · 构筑决斗",
				Position = node.Position + new Vector2(0f, node.Size.Y + 12f),
				Size = new Vector2(node.Size.X, 64f),
				FocusMode = (FocusModeEnum)2
			};
			((Control)button).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 28);
			((Node)__instance2).AddChild((Node)(object)button, false, (InternalMode)0);
			((BaseButton)button).Pressed += delegate
			{
				TaskHelper.RunSafely(StartHost(__instance2, button));
			};
			node.FocusNeighborBottom = ((Node)button).GetPath();
			((Control)button).FocusNeighborTop = ((Node)node).GetPath();
		}
	}

	private static async Task StartHost(NMultiplayerHostSubmenu menu, Button button)
	{
		if (PvpSession.HostingPvp)
		{
			return;
		}
		((BaseButton)button).Disabled = true;
		PvpSession.HostingPvp = true;
		Control overlay = (Control)AccessTools.Field(typeof(NMultiplayerHostSubmenu), "_loadingOverlay").GetValue(menu);
		NSubmenuStack stack = (NSubmenuStack)AccessTools.Field(typeof(NSubmenu), "_stack").GetValue(menu);
		((CanvasItem)overlay).Visible = true;
		NetHostGameService service = null;
		bool handedOff = false;
		try
		{
			service = new NetHostGameService(PeerVersionInfo.LocalDefault());
			if (SteamInitializer.Initialized && !CommandLineHelper.HasArg("fastmp"))
			{
				NetErrorInfo? val = await service.StartSteamHost(2);
				if (val.HasValue)
				{
					NErrorPopup val2 = NErrorPopup.Create(val.Value);
					if (val2 != null)
					{
						NModalContainer instance = NModalContainer.Instance;
						if (instance != null)
						{
							instance.Add((Node)(object)val2, true);
						}
					}
					return;
				}
			}
			else
			{
				service.StartENetHost((ushort)33771, 2);
			}
			NCustomRunScreen submenuType = stack.GetSubmenuType<NCustomRunScreen>();
			submenuType.InitializeMultiplayerAsHost((INetGameService)(object)service, 2);
			PvpSettings current = PvpSettings.Current;
			submenuType.Lobby.SetModifiers(PvpLobbyModifiers.Create(current.LastConstructionMode, current.Multipliers));
			submenuType.Lobby.SyncAscensionChange(0);
			PvpUi.ConfigureLobby(submenuType);
			stack.Push((NSubmenu)(object)submenuType);
			handedOff = true;
		}
		catch
		{
			NErrorPopup val3 = NErrorPopup.Create(new NetErrorInfo((NetError)202, false));
			if (val3 != null)
			{
				NModalContainer instance2 = NModalContainer.Instance;
				if (instance2 != null)
				{
					instance2.Add((Node)(object)val3, true);
				}
			}
			throw;
		}
		finally
		{
			PvpSession.HostingPvp = false;
			((CanvasItem)overlay).Visible = false;
			((BaseButton)button).Disabled = false;
			if (!handedOff && service != null)
			{
				service.Disconnect((NetError)1, true);
			}
		}
	}
}
[HarmonyPatch(typeof(NCustomRunScreen), "AfterInitialized")]
internal static class LobbyInitPatch
{
	private static void Postfix(NCustomRunScreen __instance)
	{
		StartRunLobby lobby = __instance.Lobby;
		if (lobby != null && lobby.Modifiers.Any((ModifierModel m) => m is PvpDuel))
		{
			PvpUi.ConfigureLobby(__instance);
		}
		else
		{
			PvpUi.RestoreLobby(__instance);
		}
	}
}
[HarmonyPatch(typeof(NCustomRunScreen), "ModifiersChanged")]
internal static class LobbySyncPatch
{
	private static bool Prefix(NCustomRunScreen __instance)
	{
		StartRunLobby lobby = __instance.Lobby;
		if (lobby == null || !lobby.Modifiers.Any((ModifierModel m) => m is PvpDuel))
		{
			return true;
		}
		PvpUi.ConfigureLobby(__instance);
		return false;
	}
}
[HarmonyPatch(typeof(NCustomRunScreen), "OnModifiersListChanged")]
internal static class LockModifierListPatch
{
	private static bool Prefix(NCustomRunScreen __instance)
	{
		StartRunLobby lobby = __instance.Lobby;
		if (lobby == null)
		{
			return true;
		}
		return !lobby.Modifiers.Any((ModifierModel m) => m is PvpDuel);
	}
}
[HarmonyPatch(typeof(StartRunLobby), "IsAboutToBeginGame")]
internal static class TwoPlayersPatch
{
	private static void Postfix(StartRunLobby __instance, ref bool __result)
	{
		if (__instance.Modifiers.Any((ModifierModel m) => m is PvpDuel))
		{
			__result &= __instance.Players.Count == 2;
		}
	}
}
public sealed class PvpDuel : ModifierModel
{
	[SavedProperty]
	public int HealthSteps { get; set; } = 4;


	[SavedProperty]
	public int BlockSteps { get; set; } = 3;


	[SavedProperty]
	public int SummonSteps { get; set; } = 3;


	[SavedProperty]
	public int DoomSteps { get; set; } = 1;


	[SavedProperty]
	public int PoisonSteps { get; set; } = 2;


	[SavedProperty]
	public bool Barricade { get; set; } = true;


	public override LocString Title => new LocString("modifiers", "SPIRE_PVP.title");

	public override LocString Description => new LocString("modifiers", "SPIRE_PVP.description");

	protected override string IconPath => "res://images/packed/modifiers/draft.png";

	public override bool ShouldStopCombatFromEnding()
	{
		if (((ModifierModel)this).RunState.Players.Count == 2)
		{
			return ((ModifierModel)this).RunState.Players.All((Player p) => p.Creature.IsAlive);
		}
		return false;
	}
}
public sealed class PvpArena : EncounterModel
{
	public override RoomType RoomType => (RoomType)1;

	public override bool ShouldGiveRewards => false;

	public override IEnumerable<MonsterModel> AllPossibleMonsters => Array.Empty<MonsterModel>();

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return Array.Empty<(MonsterModel, string)>();
	}

	public override float CalculateGoldProportion(CombatState combatState)
	{
		return 0f;
	}

	public override Vector2 GetCameraOffset()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return Vector2.Zero;
	}
}
[ModInitializer("Initialize")]
public static class ModEntry
{
	public static void Initialize()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		new Harmony("spirepvp.duel").PatchAll(typeof(ModEntry).Assembly);
		Log.Info("[SpirePvp] 已加载双人构筑决斗。双方必须安装相同版本。", 2);
	}
}
[HarmonyPatch]
internal static class MonsterAiEffectPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(CreatureCmd), "Stun", new Type[3]
		{
			typeof(Creature),
			typeof(Func<IReadOnlyList<Creature>, Task>),
			typeof(string)
		}, (Type[])null);
	}

	private static bool Prefix(Creature creature, ref Task __result)
	{
		if (!creature.IsPlayer || !PvpSession.IsArena(creature.CombatState))
		{
			return true;
		}
		__result = Task.CompletedTask;
		return false;
	}
}
public static class NativeEffectSides
{
	public static Player? Owner(AbstractModel effect)
	{
		PowerModel val = (PowerModel)(object)((effect is PowerModel) ? effect : null);
		if (val == null)
		{
			CardModel val2 = (CardModel)(object)((effect is CardModel) ? effect : null);
			if (val2 == null)
			{
				RelicModel val3 = (RelicModel)(object)((effect is RelicModel) ? effect : null);
				if (val3 == null)
				{
					PotionModel val4 = (PotionModel)(object)((effect is PotionModel) ? effect : null);
					if (val4 != null)
					{
						return val4.Owner;
					}
					return null;
				}
				return val3.Owner;
			}
			return val2.Owner;
		}
		return (val.Owner == null) ? null : PvpTargets.Team(val.Owner);
	}

	public static CombatSide Side(Creature target, AbstractModel effect)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		Player val = Owner(effect);
		Player val2 = PvpTargets.Team(target);
		if (!PvpSession.IsArena(target.CombatState) || val == null || val2 == null)
		{
			return target.Side;
		}
		if (val == val2)
		{
			return (CombatSide)1;
		}
		return (CombatSide)2;
	}

	public static bool IsEnemy(Creature target, AbstractModel effect)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Invalid comparison between Unknown and I4
		return (int)Side(target, effect) == 2;
	}

	public static bool IntendsToAttack(Creature target)
	{
		MonsterModel monster = target.Monster;
		if (monster == null)
		{
			return false;
		}
		return monster.IntendsToAttack;
	}
}
[HarmonyPatch]
internal static class NativeEffectRelationPatch
{
	[CompilerGenerated]
	private sealed class <>c__DisplayClass4_0
	{
		public HashSet<string> namespaces;

		public Func<Type, bool> <>9__0;

		internal bool <TargetMethods>b__0(Type t)
		{
			if (!t.IsAbstract && (typeof(AbstractModel).IsAssignableFrom(t) || t.IsNested))
			{
				return namespaces.Contains(t.Namespace ?? "");
			}
			return false;
		}
	}

	[CompilerGenerated]
	private sealed class <TargetMethods>d__4 : IEnumerable<MethodBase>, IEnumerable, IEnumerator<MethodBase>, IEnumerator, IDisposable
	{
		private int <>1__state;

		private MethodBase <>2__current;

		private int <>l__initialThreadId;

		private HashSet<MethodBase> <seen>5__2;

		private IEnumerator<Type> <>7__wrap2;

		private MethodInfo[] <>7__wrap3;

		private int <>7__wrap4;

		MethodBase IEnumerator<MethodBase>.Current
		{
			[DebuggerHidden]
			get
			{
				return <>2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return <>2__current;
			}
		}

		[DebuggerHidden]
		public <TargetMethods>d__4(int <>1__state)
		{
			this.<>1__state = <>1__state;
			<>l__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = <>1__state;
			if (num == -3 || num == 1)
			{
				try
				{
				}
				finally
				{
					<>m__Finally1();
				}
			}
			<seen>5__2 = null;
			<>7__wrap2 = null;
			<>7__wrap3 = null;
			<>1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				int num = <>1__state;
				if (num != 0)
				{
					if (num != 1)
					{
						return false;
					}
					<>1__state = -3;
					goto IL_01fd;
				}
				<>1__state = -1;
				<>c__DisplayClass4_0 CS$<>8__locals0 = new <>c__DisplayClass4_0();
				<seen>5__2 = new HashSet<MethodBase>();
				CS$<>8__locals0.namespaces = new HashSet<string> { "MegaCrit.Sts2.Core.Models.Cards", "MegaCrit.Sts2.Core.Models.Powers", "MegaCrit.Sts2.Core.Models.Relics", "MegaCrit.Sts2.Core.Models.Potions" };
				<>7__wrap2 = (from t in typeof(CardModel).Assembly.GetTypes()
					where !t.IsAbstract && (typeof(AbstractModel).IsAssignableFrom(t) || t.IsNested) && CS$<>8__locals0.namespaces.Contains(t.Namespace ?? "")
					select t).GetEnumerator();
				<>1__state = -3;
				goto IL_0225;
				IL_020b:
				if (<>7__wrap4 < <>7__wrap3.Length)
				{
					MethodInfo methodInfo = <>7__wrap3[<>7__wrap4];
					if (!methodInfo.IsAbstract && methodInfo.GetMethodBody() != null)
					{
						Type type = methodInfo.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType ?? methodInfo.GetCustomAttribute<IteratorStateMachineAttribute>()?.StateMachineType;
						MethodInfo methodInfo2 = ((type == null) ? methodInfo : AccessTools.Method(type, "MoveNext", (Type[])null, (Type[])null));
						if (Context(methodInfo2).HasValue && <seen>5__2.Add(methodInfo2))
						{
							List<CodeInstruction> originalInstructions = PatchProcessor.GetOriginalInstructions((MethodBase)methodInfo2, (ILGenerator)null);
							if (originalInstructions.Any((CodeInstruction i) => CodeInstructionExtensions.Calls(i, GetSide) || CodeInstructionExtensions.Calls(i, GetEnemy)) || originalInstructions.Zip(originalInstructions.Skip(1)).Any(((CodeInstruction First, CodeInstruction Second) pair) => CodeInstructionExtensions.Calls(pair.First, GetMonster) && CodeInstructionExtensions.Calls(pair.Second, GetIntent)))
							{
								<>2__current = methodInfo2;
								<>1__state = 1;
								return true;
							}
						}
					}
					goto IL_01fd;
				}
				<>7__wrap3 = null;
				goto IL_0225;
				IL_0225:
				if (<>7__wrap2.MoveNext())
				{
					Type current = <>7__wrap2.Current;
					<>7__wrap3 = current.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					<>7__wrap4 = 0;
					goto IL_020b;
				}
				<>m__Finally1();
				<>7__wrap2 = null;
				return false;
				IL_01fd:
				<>7__wrap4++;
				goto IL_020b;
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void <>m__Finally1()
		{
			<>1__state = -1;
			if (<>7__wrap2 != null)
			{
				<>7__wrap2.Dispose();
			}
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<MethodBase> IEnumerable<MethodBase>.GetEnumerator()
		{
			if (<>1__state == -2 && <>l__initialThreadId == Environment.CurrentManagedThreadId)
			{
				<>1__state = 0;
				return this;
			}
			return new <TargetMethods>d__4(0);
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<MethodBase>)this).GetEnumerator();
		}
	}

	[CompilerGenerated]
	private sealed class <Transpiler>d__6 : IEnumerable<CodeInstruction>, IEnumerable, IEnumerator<CodeInstruction>, IEnumerator, IDisposable
	{
		private int <>1__state;

		private CodeInstruction <>2__current;

		private int <>l__initialThreadId;

		private IEnumerable<CodeInstruction> instructions;

		public IEnumerable<CodeInstruction> <>3__instructions;

		private MethodBase __originalMethod;

		public MethodBase <>3____originalMethod;

		private List<CodeInstruction> <code>5__2;

		private (int Argument, FieldInfo? Field) <context>5__3;

		private int <i>5__4;

		private string <helper>5__5;

		CodeInstruction IEnumerator<CodeInstruction>.Current
		{
			[DebuggerHidden]
			get
			{
				return <>2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return <>2__current;
			}
		}

		[DebuggerHidden]
		public <Transpiler>d__6(int <>1__state)
		{
			this.<>1__state = <>1__state;
			<>l__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			<code>5__2 = null;
			<context>5__3 = default((int, FieldInfo));
			<helper>5__5 = null;
			<>1__state = -2;
		}

		private bool MoveNext()
		{
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			//IL_011f: Expected O, but got Unknown
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00da: Expected O, but got Unknown
			//IL_01b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e1: Expected O, but got Unknown
			switch (<>1__state)
			{
			default:
				return false;
			case 0:
				<>1__state = -1;
				<code>5__2 = instructions.ToList();
				<context>5__3 = Context(__originalMethod).Value;
				<i>5__4 = 0;
				break;
			case 1:
				<>1__state = -1;
				if (<context>5__3.Field != null)
				{
					<>2__current = new CodeInstruction(OpCodes.Ldfld, (object)<context>5__3.Field);
					<>1__state = 2;
					return true;
				}
				goto IL_012f;
			case 2:
				<>1__state = -1;
				goto IL_012f;
			case 3:
				<>1__state = -1;
				<helper>5__5 = null;
				goto IL_026c;
			case 4:
				<>1__state = -1;
				<i>5__4++;
				goto IL_026c;
			case 5:
				{
					<>1__state = -1;
					goto IL_026c;
				}
				IL_026c:
				<i>5__4++;
				break;
				IL_012f:
				<>2__current = CodeInstruction.Call(typeof(NativeEffectSides), <helper>5__5, (Type[])null, (Type[])null);
				<>1__state = 3;
				return true;
			}
			if (<i>5__4 < <code>5__2.Count)
			{
				CodeInstruction val = <code>5__2[<i>5__4];
				if (CodeInstructionExtensions.Calls(val, GetSide) || CodeInstructionExtensions.Calls(val, GetEnemy))
				{
					<helper>5__5 = (CodeInstructionExtensions.Calls(val, GetSide) ? "Side" : "IsEnemy");
					CodeInstruction val2 = new CodeInstruction(val)
					{
						opcode = OpCodes.Ldarg,
						operand = <context>5__3.Argument
					};
					<>2__current = val2;
					<>1__state = 1;
					return true;
				}
				if (CodeInstructionExtensions.Calls(val, GetMonster) && <i>5__4 + 1 < <code>5__2.Count && CodeInstructionExtensions.Calls(<code>5__2[<i>5__4 + 1], GetIntent))
				{
					CodeInstruction val3 = new CodeInstruction(val)
					{
						opcode = OpCodes.Call,
						operand = AccessTools.Method(typeof(NativeEffectSides), "IntendsToAttack", (Type[])null, (Type[])null)
					};
					val3.labels.AddRange(<code>5__2[<i>5__4 + 1].labels);
					val3.blocks.AddRange(<code>5__2[<i>5__4 + 1].blocks);
					<>2__current = val3;
					<>1__state = 4;
					return true;
				}
				<>2__current = val;
				<>1__state = 5;
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
		IEnumerator<CodeInstruction> IEnumerable<CodeInstruction>.GetEnumerator()
		{
			<Transpiler>d__6 <Transpiler>d__;
			if (<>1__state == -2 && <>l__initialThreadId == Environment.CurrentManagedThreadId)
			{
				<>1__state = 0;
				<Transpiler>d__ = this;
			}
			else
			{
				<Transpiler>d__ = new <Transpiler>d__6(0);
			}
			<Transpiler>d__.instructions = <>3__instructions;
			<Transpiler>d__.__originalMethod = <>3____originalMethod;
			return <Transpiler>d__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<CodeInstruction>)this).GetEnumerator();
		}
	}

	private static readonly MethodInfo GetSide = AccessTools.PropertyGetter(typeof(Creature), "Side");

	private static readonly MethodInfo GetEnemy = AccessTools.PropertyGetter(typeof(Creature), "IsEnemy");

	private static readonly MethodInfo GetMonster = AccessTools.PropertyGetter(typeof(Creature), "Monster");

	private static readonly MethodInfo GetIntent = AccessTools.PropertyGetter(typeof(MonsterModel), "IntendsToAttack");

	[IteratorStateMachine(typeof(<TargetMethods>d__4))]
	private static IEnumerable<MethodBase> TargetMethods()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new <TargetMethods>d__4(-2);
	}

	private static (int Argument, FieldInfo? Field)? Context(MethodBase method)
	{
		if (!method.IsStatic && typeof(AbstractModel).IsAssignableFrom(method.DeclaringType))
		{
			return (0, null);
		}
		if (!method.IsStatic)
		{
			FieldInfo fieldInfo = (from f in method.DeclaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				orderby (!(f.Name == "<>4__this")) ? ((f.Name == "card") ? 1 : 2) : 0
				select f).FirstOrDefault((FieldInfo f) => typeof(AbstractModel).IsAssignableFrom(f.FieldType));
			if (fieldInfo != null)
			{
				return (0, fieldInfo);
			}
		}
		ParameterInfo parameterInfo = method.GetParameters().FirstOrDefault((ParameterInfo p) => typeof(AbstractModel).IsAssignableFrom(p.ParameterType));
		if (parameterInfo != null)
		{
			return (parameterInfo.Position + ((!method.IsStatic) ? 1 : 0), null);
		}
		return null;
	}

	[IteratorStateMachine(typeof(<Transpiler>d__6))]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new <Transpiler>d__6(-2)
		{
			<>3__instructions = instructions,
			<>3____originalMethod = __originalMethod
		};
	}
}
public static class PvpCardGeneration
{
	public static IEnumerable<CardModel> Filter(IRunState? state, IEnumerable<CardModel> cards)
	{
		if (!PvpSession.IsPvp(state))
		{
			if (BossDuel.Fighting)
			{
				AbstractRoom obj = ((state != null) ? state.CurrentRoom : null);
				CombatRoom val = (CombatRoom)(object)((obj is CombatRoom) ? obj : null);
				if (val != null && PvpSession.IsArena((ICombatState?)(object)val.CombatState))
				{
					goto IL_0035;
				}
			}
			return cards;
		}
		goto IL_0035;
		IL_0035:
		return cards.Where((CardModel card) => (int)card.MultiplayerConstraint != 1);
	}
}
[HarmonyPatch(typeof(CardCreationOptions), "GetPossibleCards")]
internal static class PvpRewardCardPoolPatch
{
	private static void Postfix(Player player, ref IEnumerable<CardModel> __result)
	{
		__result = PvpCardGeneration.Filter(player.RunState, __result);
	}
}
[HarmonyPatch(typeof(CardFactory), "FilterForPlayerCount")]
internal static class PvpCombatCardPoolPatch
{
	private static void Postfix(IRunState runState, ref IEnumerable<CardModel> __result)
	{
		__result = PvpCardGeneration.Filter(runState, __result);
	}
}
[HarmonyPatch(typeof(CardPoolModel), "GetUnlockedCards")]
internal static class PvpUnlockedCardPoolPatch
{
	private static void Postfix(ref IEnumerable<CardModel> __result)
	{
		__result = PvpCardGeneration.Filter((IRunState?)(object)PvpSession.GetState(RunManager.Instance), __result);
	}
}
public sealed class PvpCombatView : ICombatState, ICardScope
{
	[CompilerGenerated]
	private CombatState <state>P;

	[CompilerGenerated]
	private Player <owner>P;

	public CombatState State => <state>P;

	public Player Owner => <owner>P;

	public IReadOnlyList<Creature> Allies => <state>P.Creatures.Where((Creature c) => Team(c) == <owner>P).ToList();

	public IReadOnlyList<Creature> Enemies => <state>P.Creatures.Where((Creature c) => Team(c) != null && Team(c) != <owner>P && PvpTargets.IsEnemyTarget(c)).ToList();

	public IReadOnlyList<Creature> HittableEnemies => Enemies.Where((Creature c) => c.IsHittable).ToList();

	public IReadOnlyList<Creature> Creatures => <state>P.Creatures;

	public IReadOnlyList<Creature> PlayerCreatures => Allies.Where((Creature c) => c.IsPlayer).ToList();

	public IReadOnlyList<Player> Players => <state>P.Players;

	public IRunState RunState => <state>P.RunState;

	public IReadOnlyList<ModifierModel> Modifiers => <state>P.Modifiers;

	public MultiplayerScalingModel? MultiplayerScalingModel => null;

	public int RoundNumber
	{
		get
		{
			return <state>P.RoundNumber;
		}
		set
		{
			<state>P.RoundNumber = value;
		}
	}

	public CombatSide CurrentSide
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return <state>P.CurrentSide;
		}
		set
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			<state>P.CurrentSide = value;
		}
	}

	public EncounterModel? Encounter => <state>P.Encounter;

	public IReadOnlyList<Creature> EscapedCreatures => <state>P.EscapedCreatures;

	public IReadOnlyList<Creature> CreaturesOnCurrentSide => <state>P.CreaturesOnCurrentSide;

	public event Action<ICombatState>? CreaturesChanged
	{
		add
		{
			<state>P.CreaturesChanged += value;
		}
		remove
		{
			<state>P.CreaturesChanged -= value;
		}
	}

	public PvpCombatView(CombatState state, Player owner)
	{
		<state>P = state;
		<owner>P = owner;
		base..ctor();
	}

	private static Player? Team(Creature c)
	{
		return c.Player ?? c.PetOwner;
	}

	public T CreateCard<T>(Player p) where T : CardModel
	{
		return <state>P.CreateCard<T>(p);
	}

	public CardModel CreateCard(CardModel c, Player p)
	{
		return <state>P.CreateCard(c, p);
	}

	public CardModel CloneCard(CardModel c)
	{
		return <state>P.CloneCard(c);
	}

	public void AddCard(CardModel c, Player p)
	{
		<state>P.AddCard(c, p);
	}

	public void RemoveCard(CardModel c)
	{
		<state>P.RemoveCard(c);
	}

	public bool ContainsCard(CardModel c)
	{
		return <state>P.ContainsCard(c);
	}

	public void AddPlayer(Player p)
	{
		<state>P.AddPlayer(p);
	}

	public Creature CreateCreature(MonsterModel m, CombatSide side, string? slot)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return <state>P.CreateCreature(m, side, slot);
	}

	public void CreatureEscaped(Creature c)
	{
		<state>P.CreatureEscaped(c);
	}

	public void RemoveCreature(Creature c, bool unattach = true)
	{
		<state>P.RemoveCreature(c, unattach);
	}

	public bool ContainsCreature(Creature c)
	{
		return <state>P.ContainsCreature(c);
	}

	public bool ContainsMonster<T>() where T : MonsterModel
	{
		return <state>P.ContainsMonster<T>();
	}

	public Creature? GetCreature(uint? id)
	{
		return <state>P.GetCreature(id);
	}

	public Task<Creature?> GetCreatureAsync(uint? id, double timeout)
	{
		return <state>P.GetCreatureAsync(id, timeout);
	}

	public IReadOnlyList<Creature> GetCreaturesOnSide(CombatSide side)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		if ((int)side != 1)
		{
			return Enemies;
		}
		return Allies;
	}

	public IReadOnlyList<Creature> GetOpponentsOf(Creature c)
	{
		Creature c2 = c;
		return <state>P.Creatures.Where((Creature x) => Team(x) != null && Team(x) != Team(c2) && PvpTargets.IsEnemyTarget(x)).ToList();
	}

	public IReadOnlyList<Creature> GetTeammatesOf(Creature c)
	{
		Creature c2 = c;
		return <state>P.Creatures.Where((Creature x) => Team(x) == Team(c2)).ToList();
	}

	public Player? GetPlayer(ulong id)
	{
		return <state>P.GetPlayer(id);
	}

	public IEnumerable<AbstractModel> IterateHookListeners()
	{
		return <state>P.IterateHookListeners();
	}

	public void SortEnemiesBySlotName()
	{
		<state>P.SortEnemiesBySlotName();
	}

	public void SetEnemyIndex(Creature c, int i)
	{
		<state>P.SetEnemyIndex(c, i);
	}

	public void AddCreature(Creature c)
	{
		<state>P.AddCreature(c);
	}

	public bool IsLiveCombat()
	{
		return <state>P.IsLiveCombat();
	}
}
public sealed class PvpDraft15 : Draft
{
	public override LocString Title => new LocString("modifiers", "SPIRE_PVP_DRAFT15.title");

	public override LocString Description => new LocString("modifiers", "SPIRE_PVP_DRAFT15.description");

	protected override string IconPath => "res://images/packed/modifiers/draft.png";

	public override Func<Task> GenerateNeowOption(EventModel e)
	{
		EventModel e2 = e;
		return () => PvpDraft.Select(e2.Owner, 15);
	}
}
public sealed class PvpDraft20 : Draft
{
	public override LocString Title => new LocString("modifiers", "SPIRE_PVP_DRAFT20.title");

	public override LocString Description => new LocString("modifiers", "SPIRE_PVP_DRAFT20.description");

	protected override string IconPath => "res://images/packed/modifiers/draft.png";

	public override Func<Task> GenerateNeowOption(EventModel e)
	{
		EventModel e2 = e;
		return () => PvpDraft.Select(e2.Owner, 20);
	}
}
internal static class PvpDraft
{
	internal static async Task Select(Player player, int count)
	{
		CardCreationOptions options = new CardCreationOptions((IEnumerable<CardPoolModel>)new <>z__ReadOnlySingleElementList<CardPoolModel>(player.Character.CardPool), (CardCreationSource)3, (CardRarityOddsType)1, (Func<CardModel, bool>)null).WithFlags((CardCreationFlags)2);
		for (int i = 0; i < count; i++)
		{
			CardReward val = new CardReward(options, 3, player, (PlayerChoiceSynchronizer)null);
			val.set_CanSkip(false);
			((Reward)val).Populate();
			await ((Reward)val).SelectUnsynchronized();
		}
		foreach (Player player2 in ((IPlayerCollection)player.RunState).Players)
		{
			player2.RelicGrabBag.Remove<PandorasBox>();
		}
		player.RunState.SharedRelicGrabBag.Remove<PandorasBox>();
	}
}
public sealed class PvpFortitudePower : PowerModel
{
	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)2;

	public override LocString Title => new LocString("powers", "SPIRE_PVP_FORTITUDE.title");

	public override LocString Description => new LocString("powers", "SPIRE_PVP_FORTITUDE.description");

	public override async Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (!(amount <= 0m) && !target.HasPower<PvpFortitudePower>())
		{
			Player player = target.Player;
			DuelMultipliers duelMultipliers = DuelMultipliers.ForRun((player != null) ? player.RunState : null);
			decimal hp = ((target.CurrentHp > 0) ? Math.Max(1m, decimal.Floor((decimal)target.CurrentHp * duelMultipliers.Health)) : 0m);
			await CreatureCmd.SetMaxHp(target, Math.Max(1m, decimal.Floor((decimal)target.MaxHp * duelMultipliers.Health)));
			await CreatureCmd.SetCurrentHp(target, hp);
		}
	}

	public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
	{
		if (target != ((PowerModel)this).Owner)
		{
			return 1m;
		}
		Player player = ((PowerModel)this).Owner.Player;
		return DuelMultipliers.ForRun((player != null) ? player.RunState : null).Block;
	}

	public override decimal ModifySummonAmount(Player summoner, decimal amount, AbstractModel? source)
	{
		if (summoner.Creature != ((PowerModel)this).Owner)
		{
			return amount;
		}
		Player player = ((PowerModel)this).Owner.Player;
		return amount * DuelMultipliers.ForRun((player != null) ? player.RunState : null).Summon;
	}

	public override bool ShouldClearBlock(Creature creature)
	{
		if (creature == ((PowerModel)this).Owner)
		{
			Player player = ((PowerModel)this).Owner.Player;
			return !DuelMultipliers.ForRun((player != null) ? player.RunState : null).Barricade;
		}
		return true;
	}

	public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
	{
		bool flag = target == ((PowerModel)this).Owner;
		if (flag)
		{
			bool flag2 = ((canonicalPower is DoomPower || canonicalPower is PoisonPower) ? true : false);
			flag = flag2;
		}
		bool flag3 = flag && amount > 0m;
		Player player = ((PowerModel)this).Owner.Player;
		DuelMultipliers duelMultipliers = DuelMultipliers.ForRun((player != null) ? player.RunState : null);
		modifiedAmount = (flag3 ? (amount * ((canonicalPower is DoomPower) ? duelMultipliers.Doom : duelMultipliers.Poison)) : amount);
		return flag3;
	}
}
public static class PvpLobbyModifiers
{
	public static IReadOnlyCollection<ModifierModel> Create(bool sealedDeck)
	{
		return Create(sealedDeck ? 1 : 0);
	}

	public static int GetMode(IEnumerable<ModifierModel> modifiers)
	{
		if (!modifiers.Any((ModifierModel m) => m is PvpDraft20))
		{
			if (!modifiers.Any((ModifierModel m) => m is PvpDraft15))
			{
				return modifiers.Any((ModifierModel m) => m is SealedDeck) ? 1 : 0;
			}
			return 2;
		}
		return 3;
	}

	public static IReadOnlyCollection<ModifierModel> Create(int mode, int healthSteps = 4, int blockSteps = 3)
	{
		return Create(mode, DuelMultipliers.FromSteps(healthSteps, blockSteps));
	}

	public static IReadOnlyCollection<ModifierModel> Create(int mode, DuelMultipliers rules)
	{
		DuelMultipliers.FromSteps(rules.HealthSteps, rules.BlockSteps, rules.SummonSteps, rules.DoomSteps, rules.PoisonSteps, rules.Barricade);
		PvpDuel obj = (PvpDuel)(object)((ModifierModel)ModelDb.Modifier<PvpDuel>()).ToMutable();
		obj.HealthSteps = rules.HealthSteps;
		obj.BlockSteps = rules.BlockSteps;
		obj.SummonSteps = rules.SummonSteps;
		obj.DoomSteps = rules.DoomSteps;
		obj.PoisonSteps = rules.PoisonSteps;
		obj.Barricade = rules.Barricade;
		ModifierModel val = (ModifierModel)(object)obj;
		ModifierModel val2 = (ModifierModel)(mode switch
		{
			0 => ((ModifierModel)ModelDb.Modifier<Draft>()).ToMutable(), 
			1 => ((ModifierModel)ModelDb.Modifier<SealedDeck>()).ToMutable(), 
			2 => ((ModifierModel)ModelDb.Modifier<PvpDraft15>()).ToMutable(), 
			3 => ((ModifierModel)ModelDb.Modifier<PvpDraft20>()).ToMutable(), 
			_ => throw new ArgumentOutOfRangeException("mode"), 
		});
		return new <>z__ReadOnlyArray<ModifierModel>((ModifierModel[])(object)new ModifierModel[2] { val, val2 });
	}
}
public sealed class PvpModConfig : SimpleModConfig
{
	[ConfigSlider(0.5, 10.0, 0.5, Format = "{0:0.0} 倍")]
	public static double 生命倍率
	{
		get
		{
			return (double)PvpSettings.Current.HealthMultiplier;
		}
		set
		{
			PvpSettings.Current.HealthMultiplier = ((double.IsFinite(value) && value >= 0.5 && value <= 10.0) ? ((decimal)value) : 2m);
			Persist();
		}
	}

	[ConfigSlider(0.5, 10.0, 0.5, Format = "{0:0.0} 倍")]
	public static double 格挡倍率
	{
		get
		{
			return (double)PvpSettings.Current.BlockMultiplier;
		}
		set
		{
			PvpSettings.Current.BlockMultiplier = ((double.IsFinite(value) && value >= 0.5 && value <= 10.0) ? ((decimal)value) : 1.5m);
			Persist();
		}
	}

	[ConfigSlider(0.0, 10.0, 0.5, Format = "{0:0.0} 倍")]
	public static double 召唤倍率
	{
		get
		{
			return (double)PvpSettings.Current.SummonMultiplier;
		}
		set
		{
			PvpSettings.Current.SummonMultiplier = EffectValue(value, 1.5m);
			Persist();
		}
	}

	[ConfigSlider(0.0, 10.0, 0.5, Format = "{0:0.0} 倍")]
	public static double 受到灾厄倍率
	{
		get
		{
			return (double)PvpSettings.Current.DoomMultiplier;
		}
		set
		{
			PvpSettings.Current.DoomMultiplier = EffectValue(value, 0.5m);
			Persist();
		}
	}

	[ConfigSlider(0.0, 10.0, 0.5, Format = "{0:0.0} 倍")]
	public static double 受到中毒倍率
	{
		get
		{
			return (double)PvpSettings.Current.PoisonMultiplier;
		}
		set
		{
			PvpSettings.Current.PoisonMultiplier = EffectValue(value, 1m);
			Persist();
		}
	}

	public static bool 壁垒
	{
		get
		{
			return PvpSettings.Current.Barricade;
		}
		set
		{
			PvpSettings.Current.Barricade = value;
			Persist();
		}
	}

	private static decimal EffectValue(double value, decimal fallback)
	{
		if (!double.IsFinite(value) || !(value >= 0.0) || !(value <= 10.0))
		{
			return fallback;
		}
		return (decimal)value;
	}

	private static void Persist()
	{
		try
		{
			PvpSettings.Current.Save(PvpSettings.SettingsPath);
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			Log.Error("[SpirePvp] 倍率保存失败，本次会话仍使用新设置：" + ex.Message, 2);
		}
	}
}
[HarmonyPatch(typeof(NMainMenu), "_Ready")]
internal static class PvpModConfigRegistrationPatch
{
	private static void Prefix()
	{
		if (ModConfigRegistry.Get("SpirePvp") == null)
		{
			ModConfigRegistry.Register("SpirePvp", (ModConfig)(object)new PvpModConfig());
		}
	}
}
internal static class PvpSession
{
	public static bool HostingPvp;

	public static bool PreparingRound;

	public static bool SelectingDecks;

	public static bool LocalDeckReady;

	public static DuelOutcome Result;

	public static ulong? Winner;

	public static RunState? GetState(RunManager manager)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		return (RunState)AccessTools.Property(typeof(RunManager), "State").GetValue(manager);
	}

	public static bool IsPvp(IRunState? state)
	{
		if (state == null)
		{
			return false;
		}
		return state.Modifiers.Any((ModifierModel m) => m is PvpDuel);
	}

	public static bool IsArena(ICombatState? state)
	{
		return ((state != null) ? state.Encounter : null) is PvpArena;
	}

	public static CombatState TurnState(object turn)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		return (CombatState)AccessTools.Property(turn.GetType(), "State").GetValue(turn);
	}

	public static bool TurnLive(object turn)
	{
		return (bool)AccessTools.Property(turn.GetType(), "IsLive").GetValue(turn);
	}

	public static CancellationToken TurnToken(object turn)
	{
		return (CancellationToken)AccessTools.Property(turn.GetType(), "Ct").GetValue(turn);
	}

	public static Task CallTask(Type type, string name, object instance, params object?[] args)
	{
		string name2 = name;
		object?[] args2 = args;
		return (Task)type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Single((MethodInfo m) => m.Name == name2 && m.GetParameters().Length == args2.Length).Invoke(instance, args2);
	}

	public static async Task Prepare(RunManager manager)
	{
		RunState state = GetState(manager);
		if (state.Players.Count != 2)
		{
			throw new InvalidOperationException("PvP 需要且仅支持两名玩家。");
		}
		Result = DuelOutcome.InProgress;
		Winner = null;
		PreparingRound = true;
		SelectingDecks = true;
		LocalDeckReady = false;
		await manager.SetActInternal(0);
		state.AppendToMapPointHistory((MapPointType)8, (RoomType)6, ((AbstractModel)ModelDb.Event<Neow>()).Id);
		await manager.EnterRoom((AbstractRoom)new MapRoom());
		NMapScreen instance = NMapScreen.Instance;
		if (instance != null)
		{
			instance.Close(false);
		}
		await manager.FadeIn(false);
		Player me = LocalContext.GetMe((IPlayerCollection)(object)state);
		await DraftCompletion.WaitForBoth(state.Players.Select(SelectNative).ToArray(), state.Players.ToList().IndexOf(me), delegate
		{
			LocalDeckReady = true;
			NMapScreen instance4 = NMapScreen.Instance;
			if (instance4 != null)
			{
				instance4.Open(true);
			}
		});
		await RoundSynchronizer.WaitForBoth(manager.PlayerChoiceSynchronizer, state.Players, me, 0, CancellationToken.None);
		SelectingDecks = false;
		NMapScreen instance2 = NMapScreen.Instance;
		if (instance2 != null)
		{
			instance2.SetTravelEnabled(true);
		}
		NMapScreen instance3 = NMapScreen.Instance;
		if (instance3 != null)
		{
			instance3.Open(false);
		}
	}

	private static Task SelectNative(Player player)
	{
		EventModel val = ((EventModel)ModelDb.Event<Neow>()).ToMutable();
		AccessTools.PropertySetter(typeof(EventModel), "Owner").Invoke(val, new object[1] { player });
		return player.RunState.Modifiers.First((ModifierModel m) => (m is Draft || m is SealedDeck) ? true : false).GenerateNeowOption(val)();
	}

	public static async Task RoundBarrier(object turn)
	{
		if (!TurnLive(turn))
		{
			return;
		}
		CombatState state = TurnState(turn);
		Player me = LocalContext.GetMe((ICombatState)(object)state);
		int round = me.PlayerCombatState.TurnNumber;
		await RoundSynchronizer.WaitForBoth(RunManager.Instance.PlayerChoiceSynchronizer, state.Players, me, round, TurnToken(turn));
		if (!TurnLive(turn))
		{
			return;
		}
		PreparingRound = false;
		foreach (Player player in state.Players)
		{
			player.PlayerCombatState.Phase = (PlayerTurnPhase)3;
		}
		AccessTools.Field(typeof(CombatManager), "_playerActionsDisabled").SetValue(CombatManager.Instance, true);
		AccessTools.PropertySetter(typeof(CombatManager), "PlayerActionsDisabled").Invoke(CombatManager.Instance, new object[1] { false });
		PvpUi.SetStatus($"第 {round} 回合 · 双方可自由出牌");
	}

	public static void CaptureResult(CombatState state)
	{
		Result = DuelRules.Outcome(state.Players[0].Creature.IsAlive, state.Players[1].Creature.IsAlive);
		Winner = Result switch
		{
			DuelOutcome.FirstWins => state.Players[0].NetId, 
			DuelOutcome.SecondWins => state.Players[1].NetId, 
			_ => null, 
		};
	}
}
public sealed class PvpSettings
{
	private decimal _healthMultiplier = 2m;

	private decimal _blockMultiplier = 1.5m;

	private decimal _summonMultiplier = 1.5m;

	private decimal _doomMultiplier = 0.5m;

	private decimal _poisonMultiplier = 1m;

	private int _lastConstructionMode;

	private static readonly Lazy<PvpSettings> Loaded = new Lazy<PvpSettings>(() => Load(SettingsPath));

	public bool BossDuels { get; set; }

	public decimal HealthMultiplier
	{
		get
		{
			return _healthMultiplier;
		}
		set
		{
			_healthMultiplier = DuelMultipliers.Normalize(value, 2m);
		}
	}

	public decimal BlockMultiplier
	{
		get
		{
			return _blockMultiplier;
		}
		set
		{
			_blockMultiplier = DuelMultipliers.Normalize(value, 1.5m);
		}
	}

	public decimal SummonMultiplier
	{
		get
		{
			return _summonMultiplier;
		}
		set
		{
			_summonMultiplier = DuelMultipliers.NormalizeEffect(value, 1.5m);
		}
	}

	public decimal DoomMultiplier
	{
		get
		{
			return _doomMultiplier;
		}
		set
		{
			_doomMultiplier = DuelMultipliers.NormalizeEffect(value, 0.5m);
		}
	}

	public decimal PoisonMultiplier
	{
		get
		{
			return _poisonMultiplier;
		}
		set
		{
			_poisonMultiplier = DuelMultipliers.NormalizeEffect(value, 1m);
		}
	}

	public bool Barricade { get; set; } = true;


	internal DuelMultipliers Multipliers => new DuelMultipliers(HealthMultiplier, BlockMultiplier, SummonMultiplier, DoomMultiplier, PoisonMultiplier, Barricade);

	public int LastConstructionMode
	{
		get
		{
			return _lastConstructionMode;
		}
		set
		{
			_lastConstructionMode = ((value >= 0 && value <= 3) ? value : 0);
		}
	}

	internal static PvpSettings Current => Loaded.Value;

	internal static string SettingsPath => ProjectSettings.GlobalizePath("user://spire_pvp_settings.json");

	internal static void RememberConstruction(int mode)
	{
		Current.LastConstructionMode = mode;
		try
		{
			Current.Save(SettingsPath);
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			Log.Error("[SpirePvp] 开局模式保存失败：" + ex.Message, 2);
		}
	}

	public static PvpSettings Load(string path)
	{
		try
		{
			return File.Exists(path) ? (JsonSerializer.Deserialize<PvpSettings>(File.ReadAllText(path)) ?? new PvpSettings()) : new PvpSettings();
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException || ex is JsonException) ? 1 : 0) != 0)
		{
			Log.Warn("[SpirePvp] 设置读取失败，幕间决斗默认关闭：" + ex.Message, 2);
			return new PvpSettings();
		}
	}

	public void Save(string path)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
		string text = path + ".tmp";
		File.WriteAllText(text, JsonSerializer.Serialize(this));
		File.Move(text, path, overwrite: true);
	}
}
public static class PvpTargets
{
	public static Player? Team(Creature creature)
	{
		return creature.Player ?? creature.PetOwner;
	}

	public static bool IsEnemyTarget(Creature creature)
	{
		return !(creature.Monster is Osty);
	}

	public static IEnumerable<Creature> NormalizeDamage(IEnumerable<Creature> targets, Creature? dealer, CardModel? source, ValueProp props)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		Player attacker = (Player)((dealer != null) ? ((object)Team(dealer)) : ((object)((source != null) ? source.Owner : null)));
		bool changed = false;
		List<Creature> list = targets.Select((Func<Creature, Creature>)delegate(Creature target)
		{
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			if (PvpSession.IsArena(target.CombatState) && target.Monster is Osty)
			{
				Player petOwner = target.PetOwner;
				if (petOwner != null && attacker != null && attacker != petOwner && ValuePropExtensions.IsPoweredAttack(props))
				{
					changed = true;
					return petOwner.Creature;
				}
			}
			return target;
		}).ToList();
		if (!changed)
		{
			return list;
		}
		return list.Distinct();
	}
}
[HarmonyPatch]
internal static class OstyDamageRoutePatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(CreatureCmd), "Damage", new Type[7]
		{
			typeof(PlayerChoiceContext),
			typeof(IEnumerable<Creature>),
			typeof(decimal),
			typeof(ValueProp),
			typeof(Creature),
			typeof(CardModel),
			typeof(CardPlay)
		}, (Type[])null);
	}

	private static void Prefix(ref IEnumerable<Creature>? targets, Creature? dealer, CardModel? cardSource, ValueProp props)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		if (targets != null)
		{
			targets = PvpTargets.NormalizeDamage(targets, dealer, cardSource, props);
		}
	}
}
internal static class PvpUi
{
	private sealed class BodyPose
	{
		public Vector2 Position { get; } = body.Position;


		public float Sign { get; } = Math.Sign(body.Scale.X);


		public BodyPose(Node2D body)
		{
		}//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)

	}

	private const string PanelName = "SpirePvpSettings";

	private static Label? _status;

	private static readonly ConditionalWeakTable<Node2D, BodyPose> BodyPoses = new ConditionalWeakTable<Node2D, BodyPose>();

	public static void ConfigureLobby(NCustomRunScreen screen)
	{
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Invalid comparison between Unknown and I4
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Expected O, but got Unknown
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected O, but got Unknown
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Expected O, but got Unknown
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Expected O, but got Unknown
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Expected O, but got Unknown
		//IL_0243: Unknown result type (might be due to invalid IL or missing references)
		//IL_0248: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Expected O, but got Unknown
		NCustomRunScreen screen2 = screen;
		Control node = ((Node)screen2).GetNode<Control>(NodePath.op_Implicit("%ModifiersList"));
		((CanvasItem)node).Visible = false;
		Node parent = ((Node)node).GetParent();
		VBoxContainer val = parent.GetNodeOrNull<VBoxContainer>(NodePath.op_Implicit("SpirePvpSettings"));
		if (val == null)
		{
			val = new VBoxContainer
			{
				Name = StringName.op_Implicit("SpirePvpSettings"),
				Position = node.Position,
				Size = node.Size
			};
			((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 20);
			((Node)val).AddChild((Node)new Label
			{
				Text = "PvP · 双人构筑决斗",
				AutowrapMode = (AutowrapMode)3
			}, false, (InternalMode)0);
			OptionButton val2 = new OptionButton
			{
				Name = StringName.op_Implicit("Construction"),
				CustomMinimumSize = new Vector2(330f, 60f)
			};
			val2.AddItem("选牌：10 次三选一", 0);
			val2.AddItem("现开套牌：30 张选 10 张", 1);
			val2.AddItem("选牌：15 次三选一", 2);
			val2.AddItem("选牌：20 次三选一", 3);
			((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
			Button val3 = new Button
			{
				Name = StringName.op_Implicit("RandomCharacter"),
				Text = "随机角色",
				CustomMinimumSize = new Vector2(330f, 54f),
				TooltipText = "从本方已解锁角色中随机选择；只改变自己的角色。"
			};
			((BaseButton)val3).Pressed += delegate
			{
				//IL_0010: Unknown result type (might be due to invalid IL or missing references)
				StartRunLobby lobby2 = screen2.Lobby;
				if (lobby2 != null && !lobby2.LocalPlayer.isReady && lobby2.Modifiers.Any((ModifierModel m) => m is PvpDuel))
				{
					AccessTools.Method(typeof(NCustomRunScreen), "RandomizeLocalCharacter", (Type[])null, (Type[])null).Invoke(screen2, null);
				}
			};
			((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
			((Node)val).AddChild((Node)new Label
			{
				Text = "双方构筑完成后，点击地图上的决斗房间。\n每回合同步恢复能量和抽牌，自由抢速出牌。\n双方都结束回合后，才进入下一回合。\n支持两名玩家；双方须安装相同版本 Mod。",
				AutowrapMode = (AutowrapMode)3
			}, false, (InternalMode)0);
			val2.ItemSelected += (ItemSelectedEventHandler)delegate(long index)
			{
				//IL_0015: Unknown result type (might be due to invalid IL or missing references)
				//IL_001b: Invalid comparison between Unknown and I4
				StartRunLobby lobby = screen2.Lobby;
				if (lobby != null && (int)lobby.NetService.Type != 3 && lobby.Modifiers.Any((ModifierModel m) => m is PvpDuel))
				{
					lobby.SetReady(false);
					PvpDuel duel = lobby.Modifiers.OfType<PvpDuel>().Single();
					lobby.SetModifiers(PvpLobbyModifiers.Create((int)index, DuelMultipliers.FromModifier(duel)));
					PvpSettings.RememberConstruction((int)index);
				}
			};
			parent.AddChild((Node)(object)val, false, (InternalMode)0);
		}
		((CanvasItem)val).Visible = true;
		OptionButton node2 = ((Node)val).GetNode<OptionButton>(NodePath.op_Implicit("Construction"));
		node2.Select(PvpLobbyModifiers.GetMode(screen2.Lobby.Modifiers));
		((BaseButton)node2).Disabled = (int)screen2.Lobby.NetService.Type == 3;
		UpdateRandomCharacter(screen2);
		DuelMultipliers duelMultipliers = DuelMultipliers.FromModifier(screen2.Lobby.Modifiers.OfType<PvpDuel>().Single());
		Label val4 = ((Node)val).GetNodeOrNull<Label>(NodePath.op_Implicit("Multipliers"));
		if (val4 == null)
		{
			val4 = new Label
			{
				Name = StringName.op_Implicit("Multipliers")
			};
			((Node)val).AddChild((Node)(object)val4, false, (InternalMode)0);
		}
		val4.Text = $"房主规则：生命 ×{duelMultipliers.Health:0.#} · 格挡 ×{duelMultipliers.Block:0.#}\n召唤 ×{duelMultipliers.Summon:0.#} · 受到灾厄 ×{duelMultipliers.Doom:0.#} · 受到中毒 ×{duelMultipliers.Poison:0.#}\n壁垒：{(duelMultipliers.Barricade ? "开启" : "关闭")}";
		val4.AutowrapMode = (AutowrapMode)3;
		object? value = AccessTools.Field(typeof(NCustomRunScreen), "_randomizeButton").GetValue(screen2);
		Control val5 = (Control)((value is Control) ? value : null);
		if (val5 != null)
		{
			((CanvasItem)val5).Visible = false;
		}
	}

	public static void UpdateRandomCharacter(NCustomRunScreen screen)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		Control nodeOrNull = ((Node)screen).GetNodeOrNull<Control>(NodePath.op_Implicit("%ModifiersList"));
		object obj;
		if (nodeOrNull == null)
		{
			obj = null;
		}
		else
		{
			VBoxContainer nodeOrNull2 = ((Node)nodeOrNull).GetParent().GetNodeOrNull<VBoxContainer>(NodePath.op_Implicit("SpirePvpSettings"));
			obj = ((nodeOrNull2 != null) ? ((Node)nodeOrNull2).GetNodeOrNull<Button>(NodePath.op_Implicit("RandomCharacter")) : null);
		}
		Button val = (Button)obj;
		if (val != null)
		{
			((BaseButton)val).Disabled = screen.Lobby == null || screen.Lobby.LocalPlayer.isReady;
		}
	}

	public static void RestoreLobby(NCustomRunScreen screen)
	{
		Control node = ((Node)screen).GetNode<Control>(NodePath.op_Implicit("%ModifiersList"));
		((CanvasItem)node).Visible = true;
		Control nodeOrNull = ((Node)node).GetParent().GetNodeOrNull<Control>(NodePath.op_Implicit("SpirePvpSettings"));
		if (nodeOrNull != null)
		{
			((CanvasItem)nodeOrNull).Visible = false;
		}
		object? value = AccessTools.Field(typeof(NCustomRunScreen), "_randomizeButton").GetValue(screen);
		Control val = (Control)((value is Control) ? value : null);
		if (val != null)
		{
			((CanvasItem)val).Visible = true;
		}
	}

	public static void SetupCombat(CombatState state)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			_status = new Label
			{
				Name = StringName.op_Implicit("SpirePvpStatus"),
				Text = "等待双方完成抽牌…",
				Position = new Vector2(600f, 115f),
				Size = new Vector2(720f, 50f),
				HorizontalAlignment = (HorizontalAlignment)1,
				MouseFilter = (MouseFilterEnum)2
			};
			((Control)_status).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 28);
			((Node)instance).AddChild((Node)(object)_status, false, (InternalMode)0);
			PositionPlayers(instance);
		}
	}

	public static void PositionPlayers(NCombatRoom room)
	{
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		if (!LocalContext.NetId.HasValue)
		{
			return;
		}
		foreach (NCreature creatureNode in room.CreatureNodes)
		{
			if (!GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) || !((Node)creatureNode).IsNodeReady())
			{
				continue;
			}
			Player owner = creatureNode.Entity.Player ?? creatureNode.Entity.PetOwner;
			if (owner != null)
			{
				int petIndex = ((creatureNode.Entity.PetOwner == null) ? (-1) : (from n in room.CreatureNodes
					where n.Entity.PetOwner == owner
					orderby n.Entity.CombatId
					select n).ToList().IndexOf(creatureNode));
				DuelPlacement duelPlacement = DuelLayout.Place(LocalContext.NetId.Value, owner.NetId, petIndex);
				((Control)creatureNode).GlobalPosition = ((CanvasItem)room).GetGlobalTransform() * new Vector2(((Control)room).Size.X * duelPlacement.X, ((Control)room).Size.Y * duelPlacement.Y);
				FaceInward(creatureNode, duelPlacement.Mirror);
				creatureNode.ToggleIsInteractable(true);
			}
		}
		if (((Node)room).IsNodeReady())
		{
			AccessTools.Method(typeof(NCombatRoom), "UpdateCreatureNavigation", (Type[])null, (Type[])null).Invoke(room, null);
		}
	}

	private static void FaceInward(NCreature node, bool mirror)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		Node2D body = node.Body;
		if (body != null)
		{
			BodyPose value2 = BodyPoses.GetValue(body, (Node2D value) => new BodyPose(value));
			body.Scale = new Vector2(Math.Abs(body.Scale.X) * value2.Sign * (float)((!mirror) ? 1 : (-1)), body.Scale.Y);
			Control bounds = node.Visuals.Bounds;
			float num = bounds.Position.X + bounds.Size.X * 0.5f;
			body.Position = new Vector2(mirror ? (2f * num - value2.Position.X) : value2.Position.X, value2.Position.Y);
		}
	}

	public static void ShowCreatureState(NCreature node)
	{
		if (!node.Entity.IsDead)
		{
			AccessTools.Field(typeof(NCreature), "_isRemotePlayerOrPet").SetValue(node, false);
			object? value = AccessTools.Field(typeof(NCreature), "_stateDisplay").GetValue(node);
			NCreatureStateDisplay val = (NCreatureStateDisplay)((value is NCreatureStateDisplay) ? value : null);
			if (val != null)
			{
				val.AnimateIn((HealthBarAnimMode)3);
			}
			node.ToggleIsInteractable(true);
		}
	}

	public static void SetStatus(string text)
	{
		if (_status != null && GodotObject.IsInstanceValid((GodotObject)(object)_status))
		{
			_status.Text = text;
		}
	}

	public static void ShowResult()
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Expected O, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Expected O, but got Unknown
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Expected O, but got Unknown
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Expected O, but got Unknown
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Expected O, but got Unknown
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null || ((Node)instance).HasNode(NodePath.op_Implicit("SpirePvpResult")))
		{
			return;
		}
		string text = ((PvpSession.Result == DuelOutcome.Draw) ? "平局" : ((PvpSession.Winner == LocalContext.NetId) ? "决斗胜利" : "决斗失败"));
		SetStatus(text);
		PanelContainer val = new PanelContainer
		{
			Name = StringName.op_Implicit("SpirePvpResult"),
			ZIndex = 100,
			MouseFilter = (MouseFilterEnum)0
		};
		((Node)instance).AddChild((Node)(object)val, false, (InternalMode)0);
		((Control)val).SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		CenterContainer val2 = new CenterContainer();
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		VBoxContainer val3 = new VBoxContainer
		{
			CustomMinimumSize = new Vector2(480f, 180f)
		};
		((Control)val3).AddThemeConstantOverride(StringName.op_Implicit("separation"), 36);
		((Node)val2).AddChild((Node)(object)val3, false, (InternalMode)0);
		Label val4 = new Label
		{
			Text = text,
			HorizontalAlignment = (HorizontalAlignment)1
		};
		((Control)val4).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 42);
		((Node)val3).AddChild((Node)(object)val4, false, (InternalMode)0);
		Button back = new Button
		{
			Text = "返回主菜单",
			CustomMinimumSize = new Vector2(360f, 64f)
		};
		((BaseButton)back).Pressed += delegate
		{
			((BaseButton)back).Disabled = true;
			if (NGame.Instance != null)
			{
				TaskHelper.RunSafely(NGame.Instance.ReturnToMainMenuAfterRun());
			}
		};
		((Node)val3).AddChild((Node)(object)back, false, (InternalMode)0);
		((Control)back).GrabFocus();
	}
}
public static class PvpVotes
{
	public static async Task<DuelMultipliers> HostMultipliers(PlayerChoiceSynchronizer sync, IReadOnlyList<Player> players, Player local, bool isHost, DuelMultipliers localSettings, CancellationToken cancellation)
	{
		PlayerChoiceSynchronizer sync2 = sync;
		Player local2 = local;
		if (players.Count != 2 || !players.Contains(local2))
		{
			throw new InvalidOperationException("倍率同步仅支持双人。");
		}
		cancellation.ThrowIfCancellationRequested();
		(Player Player, uint Id)[] source = players.Select((Player p) => (Player: p, Id: sync2.ReserveChoiceId(p))).ToArray();
		(Player, uint) tuple = source.Single(((Player Player, uint Id) s) => s.Player == local2);
		(Player, uint) tuple2 = source.Single(((Player Player, uint Id) s) => s.Player != local2);
		int value = (isHost ? localSettings.Encode() : 0);
		sync2.SyncLocalChoice(local2, tuple.Item2, PlayerChoiceResult.FromIndex((int?)value));
		int num = (await sync2.WaitForRemoteChoice(tuple2.Item1, tuple2.Item2).WaitAsync(cancellation)).AsIndex();
		if (isHost && num != 0)
		{
			throw new InvalidOperationException("客机不能覆盖房主的决斗倍率。");
		}
		return DuelMultipliers.Decode(isHost ? value : num);
	}

	public static async Task<bool[]> Exchange(PlayerChoiceSynchronizer sync, IReadOnlyList<Player> players, Player local, Func<Task<bool>> choose, CancellationToken cancellation)
	{
		PlayerChoiceSynchronizer sync2 = sync;
		Player local2 = local;
		Func<Task<bool>> choose2 = choose;
		if (players.Count != 2 || !players.Contains(local2))
		{
			throw new InvalidOperationException("幕间决斗仅支持双人。");
		}
		cancellation.ThrowIfCancellationRequested();
		return await Task.WhenAll(players.Select((Player p) => (Player: p, Id: sync2.ReserveChoiceId(p))).ToArray().Select(async delegate((Player Player, uint Id) slot)
		{
			int num;
			if (slot.Player == local2)
			{
				num = ((await choose2().WaitAsync(cancellation)) ? 1 : 0);
				sync2.SyncLocalChoice(local2, slot.Id, PlayerChoiceResult.FromIndex((int?)num));
			}
			else
			{
				num = (await sync2.WaitForRemoteChoice(slot.Player, slot.Id).WaitAsync(cancellation)).AsIndex();
			}
			if ((uint)num > 1u)
			{
				throw new InvalidOperationException("幕间决斗投票数据无效。");
			}
			return num == 1;
		})
			.ToArray());
	}
}
[HarmonyPatch]
internal static class RandomCharacterReadinessPatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		return new string[4] { "OnEmbarkPressed", "OnUnreadyPressed", "OnSubmenuOpened", "PlayerChanged" }.Select((string name) => AccessTools.Method(typeof(NCustomRunScreen), name, (Type[])null, (Type[])null));
	}

	private static void Postfix(NCustomRunScreen __instance)
	{
		PvpUi.UpdateRandomCharacter(__instance);
	}
}
public static class RoundSynchronizer
{
	public static async Task WaitForBoth(PlayerChoiceSynchronizer sync, IReadOnlyList<Player> players, Player local, int round, CancellationToken cancellation)
	{
		PlayerChoiceSynchronizer sync2 = sync;
		Player local2 = local;
		if (players.Count != 2 || !players.Contains(local2))
		{
			throw new InvalidOperationException("同步回合需要两名玩家和有效的本地玩家。");
		}
		cancellation.ThrowIfCancellationRequested();
		(Player Player, uint Id)[] source = players.Select((Player p) => (Player: p, Id: sync2.ReserveChoiceId(p))).ToArray();
		(Player, uint) tuple = source.Single(((Player Player, uint Id) e) => e.Player == local2);
		sync2.SyncLocalChoice(local2, tuple.Item2, PlayerChoiceResult.FromIndex((int?)round));
		(Player, uint) tuple2 = source.Single(((Player Player, uint Id) e) => e.Player != local2);
		if ((await sync2.WaitForRemoteChoice(tuple2.Item1, tuple2.Item2).WaitAsync(cancellation)).AsIndex() != round)
		{
			throw new InvalidOperationException("PvP 回合编号不一致，停止对局以防止失步。");
		}
	}
}
[CompilerGenerated]
internal sealed class <>z__ReadOnlyArray<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	int ICollection.Count => _items.Length;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	object? IList.this[int index]
	{
		get
		{
			return _items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	int IReadOnlyCollection<T>.Count => _items.Length;

	T IReadOnlyList<T>.this[int index] => _items[index];

	int ICollection<T>.Count => _items.Length;

	bool ICollection<T>.IsReadOnly => true;

	T IList<T>.this[int index]
	{
		get
		{
			return _items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public <>z__ReadOnlyArray(T[] items)
	{
		_items = items;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)_items).GetEnumerator();
	}

	void ICollection.CopyTo(Array array, int index)
	{
		((ICollection)_items).CopyTo(array, index);
	}

	int IList.Add(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	bool IList.Contains(object? value)
	{
		return ((IList)_items).Contains(value);
	}

	int IList.IndexOf(object? value)
	{
		return ((IList)_items).IndexOf(value);
	}

	void IList.Insert(int index, object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Remove(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return ((IEnumerable<T>)_items).GetEnumerator();
	}

	void ICollection<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.Clear()
	{
		throw new NotSupportedException();
	}

	bool ICollection<T>.Contains(T item)
	{
		return ((ICollection<T>)_items).Contains(item);
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		((ICollection<T>)_items).CopyTo(array, arrayIndex);
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	int IList<T>.IndexOf(T item)
	{
		return ((IList<T>)_items).IndexOf(item);
	}

	void IList<T>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	void IList<T>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}
}
[CompilerGenerated]
internal sealed class <>z__ReadOnlySingleElementList<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	private sealed class Enumerator : IDisposable, IEnumerator, IEnumerator<T>
	{
		object IEnumerator.Current => _item;

		T IEnumerator<T>.Current => _item;

		public Enumerator(T item)
		{
			_item = item;
		}

		bool IEnumerator.MoveNext()
		{
			if (!_moveNextCalled)
			{
				return _moveNextCalled = true;
			}
			return false;
		}

		void IEnumerator.Reset()
		{
			_moveNextCalled = false;
		}

		void IDisposable.Dispose()
		{
		}
	}

	int ICollection.Count => 1;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	object? IList.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	int IReadOnlyCollection<T>.Count => 1;

	T IReadOnlyList<T>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
	}

	int ICollection<T>.Count => 1;

	bool ICollection<T>.IsReadOnly => true;

	T IList<T>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public <>z__ReadOnlySingleElementList(T item)
	{
		_item = item;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(_item);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		array.SetValue(_item, index);
	}

	int IList.Add(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	bool IList.Contains(object? value)
	{
		return EqualityComparer<T>.Default.Equals(_item, (T)value);
	}

	int IList.IndexOf(object? value)
	{
		if (!EqualityComparer<T>.Default.Equals(_item, (T)value))
		{
			return -1;
		}
		return 0;
	}

	void IList.Insert(int index, object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Remove(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return new Enumerator(_item);
	}

	void ICollection<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.Clear()
	{
		throw new NotSupportedException();
	}

	bool ICollection<T>.Contains(T item)
	{
		return EqualityComparer<T>.Default.Equals(_item, item);
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		array[arrayIndex] = _item;
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	int IList<T>.IndexOf(T item)
	{
		if (!EqualityComparer<T>.Default.Equals(_item, item))
		{
			return -1;
		}
		return 0;
	}

	void IList<T>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	void IList<T>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}
}
