using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Orbs;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.PauseMenu;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Backgrounds;
using MegaCrit.Sts2.Core.Nodes.Vfx.Ui;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]
[assembly: AssemblyCompany("local.action_game")]
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0+1da6aee9dcdc3945d5c187223df0e3a96c52dca8")]
[assembly: AssemblyProduct("local.action_game")]
[assembly: AssemblyTitle("local.action_game")]
[assembly: AssemblyVersion("1.0.0.0")]
[module: RefSafetyRules(11)]
namespace TurnLogMod;

internal static class ActionGameNet
{
	public static INetGameService? Service
	{
		get
		{
			RunManager instance = RunManager.Instance;
			return (instance != null) ? instance.NetService : null;
		}
	}

	public static bool IsOnline
	{
		get
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Invalid comparison between Unknown and I4
			INetGameService? service = Service;
			NetGameType val = (NetGameType)((service != null) ? ((int)service.Type) : 0);
			if (val - 2 <= 1)
			{
				return true;
			}
			return false;
		}
	}

	public static bool IsHost
	{
		get
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Invalid comparison between Unknown and I4
			INetGameService? service = Service;
			return service != null && (int)service.Type == 2;
		}
	}

	public static bool IsClient
	{
		get
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Invalid comparison between Unknown and I4
			INetGameService? service = Service;
			return service != null && (int)service.Type == 3;
		}
	}

	public static bool IsInCombat
	{
		get
		{
			CombatManager instance = CombatManager.Instance;
			return instance != null && instance.IsInProgress && !instance.IsOverOrEnding;
		}
	}

	public static bool Active => IsOnline && IsInCombat;

	public static ulong LocalNetId
	{
		get
		{
			INetGameService? service = Service;
			return (service != null) ? service.NetId : 0;
		}
	}

	public static void Send(ActionGameNetMessage message)
	{
		INetGameService service = Service;
		if (service == null || !IsOnline)
		{
			return;
		}
		try
		{
			service.SendMessage<ActionGameNetMessage>(message);
		}
		catch (Exception ex)
		{
			Log.Warn("[local.action_game] net send failed: " + ex.Message, 2);
		}
	}

	public static bool TryEnsureMessageTypesRegistered()
	{
		try
		{
			MessageTypes.TypeToId<ActionGameNetMessage>();
			return true;
		}
		catch (InvalidOperationException)
		{
		}
		catch (Exception)
		{
		}
		try
		{
			MessageTypes.Initialize();
			MessageTypes.TypeToId<ActionGameNetMessage>();
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
		catch (Exception ex4)
		{
			Log.Warn("[local.action_game] MessageTypes.Initialize failed: " + ex4.Message, 2);
			return false;
		}
	}
}
public sealed class ActionGameNetMessage : INetMessage, IPacketSerializable
{
	public const byte OpcodePose = 1;

	public const byte OpcodeCardAttack = 2;

	public const byte OpcodePlayerStats = 3;

	public const byte OpcodeHostSettings = 4;

	public const byte OpcodeCombatOutcome = 5;

	public const byte OpcodeOrbitHit = 6;

	public const byte OpcodeSpawnMark = 7;

	public const byte OpcodeOrbitPresence = 8;

	public const byte OpcodeCardPlay = 9;

	public byte Opcode;

	public ulong CasterNetId;

	public float X;

	public float Y;

	public uint Tick;

	public float Amount;

	public byte HitCount;

	public byte Flags;

	public int IntA;

	public int IntB;

	public int IntC;

	public int IntD;

	public string StrA = "";

	public bool ShouldBroadcast => true;

	public NetTransferMode Mode => (NetTransferMode)1;

	public LogLevel LogLevel => (LogLevel)0;

	public bool ShouldBuffer => false;

	public void Serialize(PacketWriter writer)
	{
		writer.WriteByte(Opcode, 8);
		writer.WriteULong(CasterNetId, 64);
		writer.WriteFloat(X, (QuantizeParams?)null);
		writer.WriteFloat(Y, (QuantizeParams?)null);
		writer.WriteUInt(Tick, 32);
		writer.WriteFloat(Amount, (QuantizeParams?)null);
		writer.WriteByte(HitCount, 8);
		writer.WriteByte(Flags, 8);
		writer.WriteInt(IntA, 32);
		writer.WriteInt(IntB, 32);
		writer.WriteInt(IntC, 32);
		writer.WriteInt(IntD, 32);
		writer.WriteString(StrA ?? "");
	}

	public void Deserialize(PacketReader reader)
	{
		Opcode = reader.ReadByte(8);
		CasterNetId = reader.ReadULong(64);
		X = reader.ReadFloat((QuantizeParams?)null);
		Y = reader.ReadFloat((QuantizeParams?)null);
		Tick = reader.ReadUInt(32);
		Amount = reader.ReadFloat((QuantizeParams?)null);
		HitCount = reader.ReadByte(8);
		Flags = reader.ReadByte(8);
		IntA = reader.ReadInt(32);
		IntB = reader.ReadInt(32);
		IntC = reader.ReadInt(32);
		IntD = reader.ReadInt(32);
		StrA = reader.ReadString() ?? "";
	}
}
internal static class ActionGameNetSync
{
	private static bool _hooked;

	private static bool _typesReady;

	private static INetGameService? _registeredOn;

	private static MessageHandlerDelegate<ActionGameNetMessage>? _handler;

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			_handler = OnMessage;
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
			}
			_hooked = true;
			Log.Info("[local.action_game] action-game net sync ready (deferred register)", 2);
		}
	}

	private static void OnProcessFrame()
	{
		if (!_typesReady)
		{
			if (!ActionGameNet.TryEnsureMessageTypesRegistered())
			{
				return;
			}
			_typesReady = true;
			try
			{
				int value = MessageTypes.TypeToId<ActionGameNetMessage>();
				Log.Info($"[{"local.action_game"}] MessageTypes ready ActionGameNetMessage id={value}", 2);
			}
			catch (Exception ex)
			{
				Log.Warn("[local.action_game] MessageTypes id lookup failed: " + ex.Message, 2);
			}
		}
		TryRegister();
	}

	private static void TryRegister()
	{
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (!_typesReady)
		{
			return;
		}
		INetGameService service = ActionGameNet.Service;
		if (service != null && _handler != null && _registeredOn != service)
		{
			if (_registeredOn != null)
			{
				_registeredOn.UnregisterMessageHandler<ActionGameNetMessage>(_handler);
			}
			service.RegisterMessageHandler<ActionGameNetMessage>(_handler);
			_registeredOn = service;
			Log.Info($"[{"local.action_game"}] net handler registered type={service.Type}", 2);
		}
	}

	private static void OnMessage(ActionGameNetMessage message, ulong senderId)
	{
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		if (message == null)
		{
			return;
		}
		bool flag = message.CasterNetId != 0L && message.CasterNetId == ActionGameNet.LocalNetId;
		bool flag2 = flag;
		if (flag2)
		{
			byte opcode = message.Opcode;
			bool flag3 = (((uint)(opcode - 1) <= 2u || (uint)(opcode - 6) <= 3u) ? true : false);
			flag2 = flag3;
		}
		if (!flag2)
		{
			switch (message.Opcode)
			{
			case 1:
				PlayerPoseSync.OnRemotePose(message);
				break;
			case 2:
				break;
			case 9:
				CardPlayNetSync.OnRemote(message);
				break;
			case 3:
				PlayerStatsNetSync.OnRemote(message);
				break;
			case 4:
				HostSettingsNetSync.OnRemote(message);
				break;
			case 5:
				CombatOutcomeNetSync.OnRemote(message);
				break;
			case 6:
				OrbitHitNetSync.OnRemote(message);
				break;
			case 8:
				RemoteOrbitFxSync.OnRemote(message);
				break;
			case 7:
				AutoSpawnSystem.ApplyRemoteMark(message.IntA, new Vector2(message.X, message.Y), message.Amount, (message.Flags & 1) != 0);
				break;
			}
		}
	}

	public static void OnCombatStarted()
	{
		OnProcessFrame();
		if (ActionGameNet.IsOnline)
		{
			RunManager instance = RunManager.Instance;
			if (((instance != null) ? instance.ChecksumTracker : null) != null)
			{
				instance.ChecksumTracker.IsEnabled = false;
			}
			HostSettingsNetSync.BroadcastFromHostIfNeeded();
			PlayerPoseSync.OnCombatStarted();
			PlayerStatsNetSync.OnCombatStarted();
			CombatOutcomeNetSync.Reset();
		}
	}

	public static void OnCombatEnded()
	{
		PlayerPoseSync.OnCombatEnded();
		PlayerStatsNetSync.OnCombatEnded();
		CardAttackNetSync.OnCombatEnded();
		CardPlayNetSync.OnCombatEnded();
		OrbitHitNetSync.OnCombatEnded();
		RemoteOrbitFxSync.OnCombatEnded();
		CombatOutcomeNetSync.Reset();
	}
}
internal static class AutoCardSelectSystem
{
	private sealed class ActionGameCardSelector : ICardSelector
	{
		public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect)
		{
			if (!ShouldAutoSelect)
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(63, 3);
				defaultInterpolatedStringHandler.AppendLiteral("[");
				defaultInterpolatedStringHandler.AppendFormatted("local.action_game");
				defaultInterpolatedStringHandler.AppendLiteral("] auto-select BLOCKED outside combat ");
				defaultInterpolatedStringHandler.AppendLiteral("(combatFlag=");
				defaultInterpolatedStringHandler.AppendFormatted(_combatActive);
				defaultInterpolatedStringHandler.AppendLiteral(" inProgress=");
				CombatManager instance = CombatManager.Instance;
				defaultInterpolatedStringHandler.AppendFormatted((instance != null) ? new bool?(instance.IsInProgress) : null);
				defaultInterpolatedStringHandler.AppendLiteral(")");
				Log.Warn(defaultInterpolatedStringHandler.ToStringAndClear(), 2);
				return Task.FromResult((IEnumerable<CardModel>)Array.Empty<CardModel>());
			}
			IList<CardModel> list = (options as IList<CardModel>) ?? options.ToList();
			if (list.Count == 0 || maxSelect <= 0)
			{
				Log.Info($"[{"local.action_game"}] auto-select empty (options={list.Count} max={maxSelect})", 2);
				return Task.FromResult((IEnumerable<CardModel>)Array.Empty<CardModel>());
			}
			int num = Math.Clamp(maxSelect, Math.Max(0, minSelect), list.Count);
			if (num <= 0)
			{
				return Task.FromResult((IEnumerable<CardModel>)Array.Empty<CardModel>());
			}
			List<CardModel> list2 = list.Take(num).ToList();
			Log.Info($"[{"local.action_game"}] auto-select {list2.Count}/{list.Count} cards (min={minSelect} max={maxSelect}): " + string.Join(", ", list2.Select((CardModel c) => ((AbstractModel)c).Id.Entry)), 2);
			return Task.FromResult((IEnumerable<CardModel>)list2);
		}

		public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			if (!ShouldAutoSelect)
			{
				Log.Warn("[local.action_game] auto-select reward BLOCKED outside combat", 2);
				return default(CardRewardSelection);
			}
			if (options != null && options.Count > 0)
			{
				CardRewardSelection result = default(CardRewardSelection);
				result.card = options[0].Card;
				result.alternative = null;
				return result;
			}
			if (alternatives != null && alternatives.Count > 0)
			{
				CardRewardSelection result = default(CardRewardSelection);
				result.card = null;
				result.alternative = alternatives[0];
				return result;
			}
			return default(CardRewardSelection);
		}
	}

	private static readonly ActionGameCardSelector Selector = new ActionGameCardSelector();

	private static IDisposable? _scope;

	private static bool _hooked;

	private static bool _combatActive;

	private static bool WantInstalled => _combatActive && RealtimeCombatSettings.AutoCardSelect;

	public static bool ShouldAutoSelect
	{
		get
		{
			if (!WantInstalled)
			{
				return false;
			}
			CombatManager instance = CombatManager.Instance;
			return instance != null && instance.IsInProgress && !instance.IsOverOrEnding;
		}
	}

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			RealtimeCombatSettings.Changed += Sync;
			_hooked = true;
		}
	}

	public static void OnCombatStarted()
	{
		_combatActive = true;
		Sync();
	}

	public static void OnCombatEnded()
	{
		_combatActive = false;
		Release();
	}

	public static void OnSelectorStackReset()
	{
		_scope = null;
		if (WantInstalled)
		{
			Install();
		}
	}

	private static void Sync()
	{
		if (WantInstalled)
		{
			Install();
		}
		else
		{
			Release();
		}
	}

	private static void Install()
	{
		if (_scope == null)
		{
			_scope = CardSelectCmd.PushSelector((ICardSelector)(object)Selector, false);
			Log.Info("[local.action_game] auto card select installed", 2);
		}
	}

	private static void Release()
	{
		if (_scope != null)
		{
			IDisposable scope = _scope;
			_scope = null;
			Stack<ICardSelector> value = Traverse.Create(typeof(CardSelectCmd)).Field("_selectorStack").GetValue<Stack<ICardSelector>>();
			if (value != null && value.Count > 0 && value.Peek() == Selector)
			{
				scope.Dispose();
			}
			Log.Info("[local.action_game] auto card select released", 2);
		}
	}
}
[HarmonyPatch(typeof(CardSelectCmd), "Reset")]
internal static class AutoCardSelectResetPatch
{
	private static void Postfix()
	{
		AutoCardSelectSystem.OnSelectorStackReset();
	}
}
internal static class AutoSortHandSystem
{
	public static void Tick()
	{
		if (!RealtimeCombatSettings.AutoSortHand)
		{
			return;
		}
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsEnding)
		{
			return;
		}
		CombatState val = instance.DebugOnlyGetState();
		Player me = LocalContext.GetMe((ICombatState)(object)val);
		object obj;
		if (me == null)
		{
			obj = null;
		}
		else
		{
			PlayerCombatState playerCombatState = me.PlayerCombatState;
			obj = ((playerCombatState != null) ? playerCombatState.Hand : null);
		}
		CardPile val2 = (CardPile)obj;
		if (val2 == null || val2.IsEmpty)
		{
			return;
		}
		NPlayerHand instance2 = NPlayerHand.Instance;
		if (instance2 != null)
		{
			Traverse val3 = Traverse.Create((object)instance2);
			if (val3.Property("HasDraggedHolder", (object[])null).GetValue<bool>() || instance2.InCardPlay || instance2.IsInCardSelection)
			{
				return;
			}
		}
		IReadOnlyList<CardModel> cards = val2.Cards;
		int count = cards.Count;
		if (count <= 1 || instance2 == null || !AllHoldersReady(instance2, cards))
		{
			return;
		}
		CardModel[] array = (from x in cards.Select((CardModel card, int index) => (card: card, index: index))
			orderby SortKey(x.card), x.index
			select x.card).ToArray();
		bool flag = true;
		for (int i = 0; i < count; i++)
		{
			if (cards[i] != array[i])
			{
				flag = false;
				break;
			}
		}
		if (!flag)
		{
			for (int j = 0; j < count; j++)
			{
				val2.RemoveInternal(array[j], true);
			}
			for (int k = 0; k < count; k++)
			{
				val2.AddInternal(array[k], k, true);
			}
			val2.InvokeContentsChanged();
		}
		SyncUiOrder(instance2, array);
	}

	private static bool AllHoldersReady(NPlayerHand uiHand, IReadOnlyList<CardModel> cards)
	{
		Node cardHolderContainer = (Node)(object)uiHand.CardHolderContainer;
		if (cardHolderContainer == null)
		{
			return false;
		}
		foreach (CardModel card in cards)
		{
			Node cardHolder = (Node)(object)uiHand.GetCardHolder(card);
			if (cardHolder == null || cardHolder.GetParent() != cardHolderContainer)
			{
				return false;
			}
		}
		return true;
	}

	private static int SortKey(CardModel card)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		if (card.GainsBlock)
		{
			return 0;
		}
		if ((int)card.Type == 1)
		{
			return 1;
		}
		return 2;
	}

	private static void SyncUiOrder(NPlayerHand uiHand, CardModel[] sorted)
	{
		Node cardHolderContainer = (Node)(object)uiHand.CardHolderContainer;
		if (cardHolderContainer == null)
		{
			return;
		}
		Traverse val = Traverse.Create((object)uiHand);
		bool flag = false;
		for (int i = 0; i < sorted.Length; i++)
		{
			Node cardHolder = (Node)(object)uiHand.GetCardHolder(sorted[i]);
			if (cardHolder != null && cardHolder.GetParent() == cardHolderContainer)
			{
				int value = val.Method("GetHandInsertIndex", new object[1] { sorted[i] }).GetValue<int>();
				if (value >= 0 && cardHolder.GetIndex(false) != value)
				{
					cardHolderContainer.MoveChild(cardHolder, value);
					flag = true;
				}
			}
		}
		if (flag)
		{
			val.Method("RefreshLayout", Array.Empty<object>()).GetValue();
		}
	}
}
internal static class AutoSpawnSystem
{
	private sealed class Marker
	{
		public Control Host = null;

		public Vector2 Pos;

		public float Age;

		public SpawnEntry Entry;
	}

	private const float SpawnDelaySec = 3f;

	private const float OpeningWaveIntervalSec = 1f;

	private const int LowFieldAliveThreshold = 3;

	private const float BarLength = 78f;

	private const float BarThickness = 8f;

	private const float ClusterRadius = 90f;

	private static bool _hooked;

	private static float _interval;

	private static int _wavesPlaced;

	private static readonly List<Marker> Markers = new List<Marker>();

	private const int OpeningWaveCount = 9;

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
			}
		}
	}

	public static void ApplyRemoteMark(int catalogId, Vector2 pos, float hostElapsedSec, bool immediate)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		if (!SpawnCatalog.TryGetByNetId(catalogId, out var entry))
		{
			Log.Warn($"[{"local.action_game"}] spawn mark unknown catalogId={catalogId}", 2);
			return;
		}
		if (immediate)
		{
			MonsterSpawnSystem.TrySpawnAt(pos, entry);
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null && GodotObject.IsInstanceValid((GodotObject)(object)instance))
		{
			float num = (float)(EncounterTimerSystem.ElapsedSec - (double)hostElapsedSec);
			if (num < 0f)
			{
				num = 0f;
			}
			PlaceOne((Control)(object)instance, pos, entry, num, broadcast: false);
		}
	}

	private static void OnProcessFrame()
	{
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_024b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsOverOrEnding || !EncounterTimerSystem.SpawningOpen)
		{
			Clear();
		}
		else
		{
			if (PlayerChoicePause.IsPaused)
			{
				return;
			}
			NCombatRoom instance2 = NCombatRoom.Instance;
			float num = ((instance2 != null && GodotObject.IsInstanceValid((GodotObject)(object)instance2)) ? ((float)((Node)instance2).GetProcessDeltaTime()) : (1f / 60f));
			if (num <= 0f)
			{
				num = 1f / 60f;
			}
			if (!ActionGameNet.IsOnline || ActionGameNet.IsHost)
			{
				if (MonsterSpawnSystem.AliveCount() <= 3 && Markers.Count == 0)
				{
					if (TryPlaceMarkers())
					{
						_wavesPlaced++;
					}
					if (TryPlaceMarkers())
					{
						_wavesPlaced++;
					}
					_interval = 0f;
				}
				else
				{
					float num2 = ((_wavesPlaced < 9) ? (1f * SpawnRules.CrowdedIntervalMultiplier(MonsterSpawnSystem.AliveCount())) : SpawnRules.MarkIntervalSec());
					_interval += num;
					if (_interval >= num2)
					{
						_interval = 0f;
						if (TryPlaceMarkers())
						{
							_wavesPlaced++;
						}
					}
				}
			}
			Marker marker = null;
			foreach (Marker marker2 in Markers)
			{
				marker2.Age += num;
				if (marker2.Host != null && GodotObject.IsInstanceValid((GodotObject)(object)marker2.Host))
				{
					((CanvasItem)marker2.Host).TopLevel = true;
					marker2.Host.GlobalPosition = marker2.Pos;
				}
				if (marker == null && marker2.Age >= 3f)
				{
					marker = marker2;
				}
			}
			if (marker != null && !MonsterSpawnSystem.IsBusy)
			{
				Vector2 globalPos = marker.Pos;
				if (marker.Host != null && GodotObject.IsInstanceValid((GodotObject)(object)marker.Host))
				{
					globalPos = marker.Host.GlobalPosition;
				}
				SpawnEntry entry = marker.Entry;
				Markers.Remove(marker);
				if (marker.Host != null && GodotObject.IsInstanceValid((GodotObject)(object)marker.Host))
				{
					((Node)marker.Host).QueueFree();
				}
				MonsterSpawnSystem.TrySpawnAt(globalPos, entry);
			}
		}
	}

	private static bool TryPlaceMarkers()
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		if (MonsterSpawnSystem.AliveCount() + Markers.Count >= 48)
		{
			return false;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null || !GodotObject.IsInstanceValid((GodotObject)(object)instance))
		{
			return false;
		}
		SpawnPick spawnPick = SpawnRules.Pick();
		int count = spawnPick.Count;
		int val = 48 - MonsterSpawnSystem.AliveCount() - Markers.Count;
		count = Math.Min(count, Math.Max(1, val));
		Vector2 val2 = RandomPos((Control)(object)instance);
		for (int i = 0; i < count; i++)
		{
			Vector2 pos = ((count == 1) ? val2 : ClusterOffset(val2, i, count));
			PlaceOne((Control)(object)instance, pos, spawnPick.Entry, 0f, broadcast: true);
		}
		return true;
	}

	private static Vector2 ClusterOffset(Vector2 center, int index, int count)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		float num = (float)Math.PI * 2f * (float)index / (float)count + GD.Randf() * 0.4f;
		float num2 = 28f + GD.Randf() * 90f;
		return center + new Vector2(Mathf.Cos(num), Mathf.Sin(num)) * num2;
	}

	private static void PlaceOne(Control room, Vector2 pos, SpawnEntry entry, float age, bool broadcast)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		Control val = MakeX();
		((Node)room).AddChild((Node)(object)val, false, (InternalMode)0);
		((CanvasItem)val).TopLevel = true;
		((CanvasItem)val).ZAsRelative = false;
		((CanvasItem)val).ZIndex = 4096;
		val.GlobalPosition = pos;
		Markers.Add(new Marker
		{
			Host = val,
			Pos = val.GlobalPosition,
			Age = age,
			Entry = entry
		});
		if (broadcast && ActionGameNet.IsOnline && ActionGameNet.IsHost && SpawnCatalog.TryGetNetId(entry, out var id))
		{
			ActionGameNet.Send(new ActionGameNetMessage
			{
				Opcode = 7,
				CasterNetId = ActionGameNet.LocalNetId,
				X = pos.X,
				Y = pos.Y,
				IntA = id,
				Amount = (float)EncounterTimerSystem.ElapsedSec
			});
		}
	}

	private static Vector2 RandomPos(Control room)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		Rect2 globalRect = room.GetGlobalRect();
		if (((Rect2)(ref globalRect)).Size.X < 200f || ((Rect2)(ref globalRect)).Size.Y < 200f)
		{
			Viewport viewport = ((Node)room).GetViewport();
			? val;
			if (viewport == null)
			{
				val = new Vector2(1600f, 900f);
			}
			else
			{
				Rect2 visibleRect = viewport.GetVisibleRect();
				val = ((Rect2)(ref visibleRect)).Size;
			}
			Vector2 val2 = (Vector2)val;
			((Rect2)(ref globalRect))..ctor(Vector2.Zero, val2);
		}
		float num = Math.Max(40f, ((Rect2)(ref globalRect)).Size.X - 320f);
		float num2 = Math.Max(40f, ((Rect2)(ref globalRect)).Size.Y - 320f);
		return new Vector2(((Rect2)(ref globalRect)).Position.X + 160f + GD.Randf() * num, ((Rect2)(ref globalRect)).Position.Y + 160f + GD.Randf() * num2);
	}

	private static Control MakeX()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		Control val = new Control
		{
			Name = StringName.op_Implicit("ActionGameSpawnX"),
			MouseFilter = (MouseFilterEnum)2
		};
		((Node)val).AddChild((Node)(object)MakeBar((float)Math.PI / 4f), false, (InternalMode)0);
		((Node)val).AddChild((Node)(object)MakeBar(-(float)Math.PI / 4f), false, (InternalMode)0);
		return val;
	}

	private static ColorRect MakeBar(float rotation)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		return new ColorRect
		{
			MouseFilter = (MouseFilterEnum)2,
			Color = new Color(1f, 0.08f, 0.08f, 0.95f),
			Size = new Vector2(78f, 8f),
			Position = new Vector2(-39f, -4f),
			PivotOffset = new Vector2(39f, 4f),
			Rotation = rotation
		};
	}

	private static void Clear()
	{
		ClearAll();
	}

	internal static void ClearAll()
	{
		foreach (Marker marker in Markers)
		{
			if (marker.Host != null && GodotObject.IsInstanceValid((GodotObject)(object)marker.Host))
			{
				((Node)marker.Host).QueueFree();
			}
		}
		Markers.Clear();
		_interval = 0f;
		_wavesPlaced = 0;
	}
}
internal static class BarricadeRewrite
{
	public const string Description = "每打出一张防御牌，获得1点敏捷";
}
[HarmonyPatch(typeof(BarricadePower), "ShouldClearBlock")]
internal static class BarricadeDisableBlockRetainPatch
{
	private static void Postfix(ref bool __result)
	{
		__result = true;
	}
}
[HarmonyPatch(typeof(Hook), "AfterCardPlayed")]
internal static class BarricadeAfterCardPlayedPatch
{
	private static void Postfix(ICombatState combatState, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
	{
		__result = ContinueAfterCardPlayed(__result, choiceContext, cardPlay);
	}

	private static async Task ContinueAfterCardPlayed(Task original, PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (original != null)
		{
			await original;
		}
		CardModel card = ((cardPlay != null) ? cardPlay.Card : null);
		if (card == null || !card.GainsBlock)
		{
			return;
		}
		Player owner = card.Owner;
		Creature creature = ((owner != null) ? owner.Creature : null);
		if (creature != null && creature.IsAlive)
		{
			BarricadePower barricade = creature.GetPower<BarricadePower>();
			if (barricade != null)
			{
				await PowerCmd.Apply<DexterityPower>(choiceContext, creature, 1m, creature, card, false);
			}
		}
	}
}
[HarmonyPatch(typeof(LocString), "GetFormattedText")]
internal static class BarricadeDescriptionPatch
{
	private static void Postfix(LocString __instance, ref string __result)
	{
		bool flag = __instance.LocTable == "cards";
		bool flag2 = flag;
		if (flag2)
		{
			string locEntryKey = __instance.LocEntryKey;
			bool flag3 = ((locEntryKey == "BARRICADE.description" || locEntryKey == "BARRICADE.eventDescription") ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			__result = "每打出一张防御牌，获得1点敏捷";
			return;
		}
		bool flag4 = __instance.LocTable == "powers";
		bool flag5 = flag4;
		if (flag5)
		{
			bool flag3;
			switch (__instance.LocEntryKey)
			{
			case "BARRICADE_POWER.description":
			case "BARRICADE_POWER.smartDescription":
			case "BARRICADE_POWER.eventDescription":
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag5 = flag3;
		}
		if (flag5)
		{
			__result = "每打出一张防御牌，获得1点敏捷";
		}
	}
}
[HarmonyPatch]
public static class BuffPopupVfxScalePatch
{
	private const float ScaleMul = 0.25f;

	private static void Shrink(Node? node)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		Vector2 scale = Vector2.One * 0.25f;
		Node2D val = (Node2D)(object)((node is Node2D) ? node : null);
		if (val != null)
		{
			val.Scale = scale;
			return;
		}
		Control val2 = (Control)(object)((node is Control) ? node : null);
		if (val2 != null)
		{
			val2.Scale = scale;
		}
	}

	[HarmonyPatch(typeof(NPowerAppliedVfx), "Create")]
	[HarmonyPostfix]
	private static void AfterPowerApplied(NPowerAppliedVfx? __result)
	{
		Shrink((Node?)(object)__result);
	}

	[HarmonyPatch(typeof(NPowerAppliedBuffVfx), "Create")]
	[HarmonyPostfix]
	private static void AfterBuffBurst(NPowerAppliedBuffVfx? __result)
	{
		Shrink((Node?)(object)__result);
	}

	[HarmonyPatch(typeof(NPowerAppliedDebuffVfx), "Create")]
	[HarmonyPostfix]
	private static void AfterDebuffBurst(NPowerAppliedDebuffVfx? __result)
	{
		Shrink((Node?)(object)__result);
	}

	[HarmonyPatch(typeof(NPowerFlashVfx), "Create")]
	[HarmonyPostfix]
	private static void AfterPowerFlash(NPowerFlashVfx? __result)
	{
		Shrink((Node?)(object)__result);
	}

	[HarmonyPatch(typeof(NRelicFlashVfx), "Create", new Type[] { typeof(RelicModel) })]
	[HarmonyPostfix]
	private static void AfterRelicFlash(NRelicFlashVfx? __result)
	{
		Shrink((Node?)(object)__result);
	}

	[HarmonyPatch(typeof(NRelicFlashVfx), "_Ready")]
	[HarmonyPostfix]
	private static void AfterRelicFlashReady(NRelicFlashVfx __instance)
	{
		Shrink((Node?)(object)__instance);
	}

	[HarmonyPatch(typeof(NPowerUpVfx), "CreateNormal")]
	[HarmonyPostfix]
	private static void AfterPowerUpNormal(NPowerUpVfx? __result)
	{
		Shrink((Node?)(object)__result);
	}

	[HarmonyPatch(typeof(NPowerUpVfx), "CreateGhostly")]
	[HarmonyPostfix]
	private static void AfterPowerUpGhostly(NPowerUpVfx? __result)
	{
		Shrink((Node?)(object)__result);
	}

	[HarmonyPatch(typeof(NBlockSparkVfx), "Create")]
	[HarmonyPostfix]
	private static void AfterBlockSpark(NBlockSparkVfx? __result)
	{
		Shrink((Node?)(object)__result);
	}

	[HarmonyPatch(typeof(VfxCmd), "PlayVfx")]
	[HarmonyPostfix]
	private static void AfterPlayVfx(string path, Control? vfxContainer)
	{
		if (vfxContainer != null && GodotObject.IsInstanceValid((GodotObject)(object)vfxContainer) && !string.IsNullOrEmpty(path) && path.Contains("vfx_block", StringComparison.OrdinalIgnoreCase) && !path.Contains("broken", StringComparison.OrdinalIgnoreCase) && !path.Contains("blocked", StringComparison.OrdinalIgnoreCase) && ((Node)vfxContainer).GetChildCount(false) > 0)
		{
			Shrink(((Node)vfxContainer).GetChild(((Node)vfxContainer).GetChildCount(false) - 1, false));
		}
	}
}
internal static class CardAttackNetSync
{
	public static bool SuppressBroadcast;

	public static void OnCombatEnded()
	{
		SuppressBroadcast = false;
	}

	public static void BeginLocalCardCapture(CardModel? card = null)
	{
	}

	public static void NoteLocalCardDamage(decimal amount, Creature? target, Creature? dealer, CardModel? card)
	{
	}

	public static void EndLocalCardCaptureAndBroadcast(CardModel? card = null)
	{
	}

	public static void EndLocalCardCaptureAndBroadcast(CardModel? card, Creature? target)
	{
		if (card != null && ActionGameNet.IsOnline && !SuppressBroadcast && !CardPlayNetSync.IsReplayingRemote)
		{
			CardPlayNetSync.BroadcastLocalPlay(card, target);
		}
	}

	public static void OnRemote(ActionGameNetMessage msg)
	{
	}
}
internal static class CardPlayNetSync
{
	public const byte FlagHadTarget = 1;

	public static bool IsReplayingRemote { get; private set; }

	public static void OnCombatEnded()
	{
		IsReplayingRemote = false;
	}

	public static void BroadcastLocalPlay(CardModel card, Creature? target)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected I4, but got Unknown
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		if (!ActionGameNet.IsOnline || IsReplayingRemote || card == null)
		{
			return;
		}
		Vector2 pos = default(Vector2);
		bool flag = false;
		Player owner = card.Owner;
		if (owner != null)
		{
			NCreature playerNode = CombatPeerUtil.GetPlayerNode(owner);
			if (playerNode != null && GodotObject.IsInstanceValid((GodotObject)(object)playerNode))
			{
				pos = ((Control)playerNode).GlobalPosition;
				flag = true;
			}
		}
		if (!flag)
		{
			CombatPeerUtil.TryGetPose(ActionGameNet.LocalNetId, out pos);
		}
		ActionGameNet.Send(new ActionGameNetMessage
		{
			Opcode = 9,
			CasterNetId = ActionGameNet.LocalNetId,
			X = pos.X,
			Y = pos.Y,
			Flags = ((target != null) ? ((byte)1) : ((byte)0)),
			IntA = (int)card.Type,
			StrA = ((object)((AbstractModel)card).Id).ToString()
		});
		Log.Info($"[{"local.action_game"}] card-play SEND id={((AbstractModel)card).Id} type={card.Type} hadTarget={target != null} pose=({pos.X:0.#},{pos.Y:0.#})", 2);
	}

	public static void OnRemote(ActionGameNetMessage msg)
	{
		Log.Info($"[{"local.action_game"}] card-play RECV from={msg.CasterNetId} id={msg.StrA} typeHint={msg.IntA} flags={msg.Flags}", 2);
		if (ActionGameNet.IsOnline && !IsReplayingRemote && msg.CasterNetId != 0L && msg.CasterNetId != ActionGameNet.LocalNetId)
		{
			if (string.IsNullOrEmpty(msg.StrA))
			{
				Log.Warn("[local.action_game] card-play RECV abort: empty card id", 2);
			}
			else
			{
				ReplayAsync(msg);
			}
		}
	}

	private static async Task ReplayAsync(ActionGameNetMessage msg)
	{
		try
		{
			Player player = CombatPeerUtil.FindPlayer(msg.CasterNetId);
			if (((player != null) ? player.PlayerCombatState : null) == null)
			{
				Log.Warn($"[{"local.action_game"}] card-play APPLY FAIL no player {msg.CasterNetId}", 2);
				return;
			}
			CardModel card = FindCard(player, msg.StrA);
			if (card == null)
			{
				Log.Warn($"[{"local.action_game"}] card-play APPLY FAIL card not found id={msg.StrA} on {player.NetId}", 2);
				return;
			}
			Vector2 origin = new Vector2(msg.X, msg.Y);
			if (origin == Vector2.Zero)
			{
				CombatPeerUtil.TryGetPose(msg.CasterNetId, out origin);
			}
			Creature target = null;
			if (NeedsEnemyTarget(card) || ((uint)msg.Flags & (true ? 1u : 0u)) != 0)
			{
				target = CombatPeerUtil.PickNearestEnemy(origin, 800f);
				Log.Info($"[{"local.action_game"}] card-play APPLY target={((target != null) ? target.LogName : "null")} for {((AbstractModel)card).Id}", 2);
			}
			EnsurePlayableEnergy(player, card);
			IsReplayingRemote = true;
			CardAttackNetSync.SuppressBroadcast = true;
			try
			{
				DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(51, 4);
				defaultInterpolatedStringHandler.AppendLiteral("[");
				defaultInterpolatedStringHandler.AppendFormatted("local.action_game");
				defaultInterpolatedStringHandler.AppendLiteral("] card-play APPLY AutoPlay begin id=");
				defaultInterpolatedStringHandler.AppendFormatted<ModelId>(((AbstractModel)card).Id);
				defaultInterpolatedStringHandler.AppendLiteral(" ");
				defaultInterpolatedStringHandler.AppendLiteral("pile=");
				CardPile pile = card.Pile;
				defaultInterpolatedStringHandler.AppendFormatted((pile != null) ? new PileType?(pile.Type) : null);
				defaultInterpolatedStringHandler.AppendLiteral(" energy=");
				defaultInterpolatedStringHandler.AppendFormatted(player.PlayerCombatState.Energy);
				Log.Info(defaultInterpolatedStringHandler.ToStringAndClear(), 2);
				await CardCmd.AutoPlay((PlayerChoiceContext)new BlockingPlayerChoiceContext(), card, target, (AutoPlayType)1, true, false);
				Log.Info("[local.action_game] card-play APPLY AutoPlay done id=" + msg.StrA, 2);
			}
			finally
			{
				CardAttackNetSync.SuppressBroadcast = false;
				IsReplayingRemote = false;
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			IsReplayingRemote = false;
			CardAttackNetSync.SuppressBroadcast = false;
			Log.Error($"[{"local.action_game"}] card-play APPLY EXCEPTION: {ex}", 2);
		}
	}

	private static bool NeedsEnemyTarget(CardModel card)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Invalid comparison between Unknown and I4
		TargetType targetType = card.TargetType;
		if ((int)targetType == 2 || (int)targetType == 4 || (int)targetType == 9)
		{
			return true;
		}
		return false;
	}

	private static void EnsurePlayableEnergy(Player player, CardModel card)
	{
		PlayerCombatState playerCombatState = player.PlayerCombatState;
		if (playerCombatState != null)
		{
			int num = 0;
			try
			{
				CardEnergyCost energyCost = card.EnergyCost;
				num = ((energyCost != null) ? energyCost.GetAmountToSpend() : 0);
			}
			catch
			{
				num = 0;
			}
			if (num < 0)
			{
				num = 0;
			}
			if (playerCombatState.Energy < num)
			{
				Log.Info($"[{"local.action_game"}] card-play APPLY boost energy {playerCombatState.Energy}->{num} for {((AbstractModel)card).Id}", 2);
				playerCombatState.Energy = num;
			}
		}
	}

	private static CardModel? FindCard(Player player, string idStr)
	{
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Invalid comparison between Unknown and I4
		PlayerCombatState playerCombatState = player.PlayerCombatState;
		if (((playerCombatState != null) ? playerCombatState.AllPiles : null) == null)
		{
			return null;
		}
		CardModel val = null;
		foreach (CardPile allPile in playerCombatState.AllPiles)
		{
			if (((allPile != null) ? allPile.Cards : null) == null)
			{
				continue;
			}
			foreach (CardModel card in allPile.Cards)
			{
				if (card != null && !(((object)((AbstractModel)card).Id).ToString() != idStr))
				{
					if ((int)allPile.Type == 2)
					{
						return card;
					}
					if (val == null)
					{
						val = card;
					}
				}
			}
		}
		return val;
	}
}
internal static class CombatCompat
{
	public static object? GetTurnState(CombatManager manager)
	{
		if (manager == null)
		{
			return null;
		}
		return Traverse.Create((object)manager).Field("_turnState").GetValue();
	}

	public static object? GetPendingLoss(CombatManager manager)
	{
		if (manager == null)
		{
			return null;
		}
		object turnState = GetTurnState(manager);
		if (turnState != null)
		{
			object value = Traverse.Create(turnState).Property("PendingLoss", (object[])null).GetValue();
			if (value != null)
			{
				return value;
			}
		}
		return Traverse.Create((object)manager).Field("_pendingLoss").GetValue();
	}

	public static CancellationTokenSource? GetCombatCts(CombatManager manager)
	{
		if (manager == null)
		{
			return null;
		}
		object turnState = GetTurnState(manager);
		if (turnState != null)
		{
			CancellationTokenSource value = Traverse.Create(turnState).Field("_cts").GetValue<CancellationTokenSource>();
			if (value != null)
			{
				return value;
			}
		}
		return Traverse.Create((object)manager).Field("_combatCts").GetValue<CancellationTokenSource>();
	}

	public static void SetInProgress(CombatManager manager, bool value)
	{
		if (manager == null)
		{
			return;
		}
		object turnState = GetTurnState(manager);
		if (turnState != null)
		{
			Traverse val = Traverse.Create(turnState).Property("IsInProgress", (object[])null);
			if (val.PropertyExists())
			{
				val.SetValue((object)value);
				return;
			}
		}
		Traverse val2 = Traverse.Create((object)manager).Property("IsInProgress", (object[])null);
		if (val2.PropertyExists())
		{
			val2.SetValue((object)value);
		}
	}

	public static void CancelTurnState(CombatManager manager)
	{
		if (manager == null)
		{
			return;
		}
		object turnState = GetTurnState(manager);
		if (turnState != null)
		{
			MethodInfo methodInfo = AccessTools.Method(turnState.GetType(), "Cancel", (Type[])null, (Type[])null);
			if (methodInfo != null)
			{
				methodInfo.Invoke(turnState, null);
				Log.Info("[local.action_game] CombatTurnState.Cancel invoked", 2);
				return;
			}
		}
		CancellationTokenSource combatCts = GetCombatCts(manager);
		if (combatCts == null)
		{
			Log.Info("[local.action_game] combat CTS is null", 2);
			return;
		}
		if (combatCts.IsCancellationRequested)
		{
			Log.Info("[local.action_game] combat CTS already cancelled", 2);
			return;
		}
		Log.Info("[local.action_game] cancelling combat CTS", 2);
		combatCts.Cancel();
	}

	public static Task? InvokeDoTurnEnd(CombatManager manager, Player player, PlayerChoiceContext choiceContext)
	{
		List<MethodInfo> source = (from m in AccessTools.GetDeclaredMethods(typeof(CombatManager))
			where m.Name == "DoTurnEnd"
			select m).ToList();
		object turnState = GetTurnState(manager);
		if (turnState != null)
		{
			MethodInfo methodInfo = source.FirstOrDefault(delegate(MethodInfo m)
			{
				ParameterInfo[] parameters2 = m.GetParameters();
				return parameters2.Length == 3 && parameters2[1].ParameterType == typeof(Player) && typeof(PlayerChoiceContext).IsAssignableFrom(parameters2[2].ParameterType);
			});
			if (methodInfo != null)
			{
				return methodInfo.Invoke(manager, new object[3] { turnState, player, choiceContext }) as Task;
			}
		}
		MethodInfo methodInfo2 = source.FirstOrDefault(delegate(MethodInfo m)
		{
			ParameterInfo[] parameters = m.GetParameters();
			return parameters.Length == 2 && parameters[0].ParameterType == typeof(Player) && typeof(PlayerChoiceContext).IsAssignableFrom(parameters[1].ParameterType);
		});
		if (methodInfo2 != null)
		{
			return methodInfo2.Invoke(manager, new object[2] { player, choiceContext }) as Task;
		}
		Log.Warn("[local.action_game] DoTurnEnd overload not found", 2);
		return null;
	}

	public static Task? InvokeEndCombatInternal(CombatManager manager)
	{
		List<MethodInfo> source = (from m in AccessTools.GetDeclaredMethods(typeof(CombatManager))
			where m.Name == "EndCombatInternal"
			select m).ToList();
		MethodInfo methodInfo = source.FirstOrDefault((MethodInfo m) => m.GetParameters().Length == 0);
		if (methodInfo != null)
		{
			return methodInfo.Invoke(manager, Array.Empty<object>()) as Task;
		}
		object turnState = GetTurnState(manager);
		MethodInfo methodInfo2 = source.FirstOrDefault((MethodInfo m) => m.GetParameters().Length == 1);
		if (methodInfo2 != null && turnState != null)
		{
			return methodInfo2.Invoke(manager, new object[1] { turnState }) as Task;
		}
		Log.Warn("[local.action_game] EndCombatInternal overload not found", 2);
		return null;
	}

	public static void PatchAllSafe(Harmony harmony, Assembly assembly)
	{
		Type[] typesFromAssembly = AccessTools.GetTypesFromAssembly(assembly);
		foreach (Type type in typesFromAssembly)
		{
			try
			{
				harmony.CreateClassProcessor(type).Patch();
			}
			catch (Exception ex)
			{
				Log.Warn($"[{"local.action_game"}] skip patch {type.Name}: {ex.GetBaseException().Message}", 2);
			}
		}
	}

	public static Task InvokeBeforeSideTurnEnd(ICombatState state, CombatSide side, IEnumerable creatures)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return InvokeHookTurnEnd("BeforeSideTurnEnd", "BeforeTurnEnd", state, side, creatures);
	}

	public static Task InvokeAfterSideTurnEnd(ICombatState state, CombatSide side, IEnumerable creatures)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return InvokeHookTurnEnd("AfterSideTurnEnd", "AfterTurnEnd", state, side, creatures);
	}

	private static Task InvokeHookTurnEnd(string betaName, string publicName, ICombatState state, CombatSide side, IEnumerable creatures)
	{
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		Type typeFromHandle = typeof(Hook);
		MethodInfo methodInfo = AccessTools.Method(typeFromHandle, betaName, (Type[])null, (Type[])null) ?? AccessTools.Method(typeFromHandle, publicName, (Type[])null, (Type[])null);
		if (methodInfo == null)
		{
			Log.Warn($"[{"local.action_game"}] Hook.{betaName}/{publicName} missing", 2);
			return Task.CompletedTask;
		}
		return (methodInfo.Invoke(null, new object[3] { state, side, creatures }) as Task) ?? Task.CompletedTask;
	}
}
internal static class CombatOutcomeNetSync
{
	public const byte FlagWin = 1;

	private static bool _handlingRemoteWin;

	private static bool _broadcastSent;

	private static int _broadcastBurstLeft;

	private static string? _lastReason;

	public static bool SuppressLocalHostOnlyTimer => ActionGameNet.IsClient;

	public static void BroadcastWin(string reason)
	{
		if (ActionGameNet.IsOnline)
		{
			_lastReason = reason;
			if (!_broadcastSent)
			{
				_broadcastSent = true;
				_broadcastBurstLeft = 3;
			}
			FlushBroadcast();
		}
	}

	public static void TickBurst()
	{
		if (_broadcastBurstLeft > 0)
		{
			FlushBroadcast();
		}
	}

	private static void FlushBroadcast()
	{
		if (_broadcastBurstLeft > 0)
		{
			_broadcastBurstLeft--;
			ActionGameNet.Send(new ActionGameNetMessage
			{
				Opcode = 5,
				CasterNetId = ActionGameNet.LocalNetId,
				Flags = 1,
				IntA = (_lastReason?.GetHashCode() ?? 0)
			});
			Log.Info($"[{"local.action_game"}] combat outcome win broadcast ({_lastReason}) left={_broadcastBurstLeft}", 2);
		}
	}

	public static void OnRemote(ActionGameNetMessage msg)
	{
		if (((uint)msg.Flags & (true ? 1u : 0u)) != 0 && !_handlingRemoteWin)
		{
			_handlingRemoteWin = true;
			_broadcastSent = true;
			_broadcastBurstLeft = 0;
			Log.Info($"[{"local.action_game"}] received peer force-win from {msg.CasterNetId}", 2);
			CombatAccess.TimedWinBypass = true;
			CombatAccess.RequestDeferredForceWin("peer-outcome");
		}
	}

	public static void Reset()
	{
		_handlingRemoteWin = false;
		_broadcastSent = false;
		_broadcastBurstLeft = 0;
		_lastReason = null;
	}
}
public static class CombatPatches
{
	[HarmonyPatch(typeof(CombatManager), "SetUpCombat")]
	private static class SetUpCombatPatch
	{
		private static void Postfix(CombatManager __instance)
		{
			BindManager(__instance);
		}
	}

	[HarmonyPatch(typeof(CombatManager), "SetupPlayerTurn")]
	private static class SetupPlayerTurnPatch
	{
		private static void Postfix(Player player)
		{
			CombatAccess.FreezePlayerPlayPhase();
			RealtimeCombatTicker.Start();
			AutoCardSelectSystem.OnCombatStarted();
			Log.Info($"[{"local.action_game"}] after SetupPlayerTurn (netId={player.NetId}) - frozen in Play, ticker start requested", 2);
		}
	}

	[HarmonyPatch(typeof(PlayerCmd), "EndTurn")]
	private static class EndTurnPatch
	{
		private static bool Prefix(Player player)
		{
			Log.Info($"[{"local.action_game"}] blocked EndTurn (netId={player.NetId})", 2);
			return false;
		}
	}

	[HarmonyPatch(typeof(CombatManager), "DoTurnEnd")]
	private static class DoTurnEndPatch
	{
		private static bool Prefix(ref Task __result)
		{
			if (CombatAccess.AllowDoTurnEnd)
			{
				return true;
			}
			Log.Info("[local.action_game] blocked DoTurnEnd", 2);
			__result = Task.CompletedTask;
			return false;
		}
	}

	[HarmonyPatch(typeof(CombatManager), "FlushPlayerHand")]
	private static class FlushPlayerHandPatch
	{
		private static bool Prefix(ref Task __result)
		{
			Log.Info("[local.action_game] blocked FlushPlayerHand", 2);
			__result = Task.CompletedTask;
			return false;
		}
	}

	[HarmonyPatch(typeof(CombatManager), "SwitchFromPlayerToEnemySide")]
	private static class SwitchToEnemyPatch
	{
		private static bool Prefix(ref Task __result)
		{
			Log.Info("[local.action_game] blocked SwitchFromPlayerToEnemySide", 2);
			__result = Task.CompletedTask;
			return false;
		}
	}

	[HarmonyPatch(typeof(CombatManager), "ExecuteEnemyTurn")]
	private static class ExecuteEnemyTurnPatch
	{
		private static bool Prefix(ref Task __result)
		{
			Log.Info("[local.action_game] blocked ExecuteEnemyTurn (use realtime ticker instead)", 2);
			__result = Task.CompletedTask;
			return false;
		}
	}

	[HarmonyPatch(typeof(CombatManager), "EndEnemyTurn")]
	private static class EndEnemyTurnPatch
	{
		private static bool Prefix(ref Task __result)
		{
			Log.Info("[local.action_game] blocked EndEnemyTurn", 2);
			__result = Task.CompletedTask;
			return false;
		}
	}

	[HarmonyPatch(typeof(NEndTurnButton), "CallReleaseLogic")]
	private static class EndTurnButtonClickPatch
	{
		private static bool Prefix()
		{
			Log.Info("[local.action_game] blocked end-turn button", 2);
			return false;
		}
	}

	[HarmonyPatch(typeof(NEndTurnButton), "Initialize")]
	private static class EndTurnButtonInitPatch
	{
		private static void Postfix(NEndTurnButton __instance)
		{
			((CanvasItem)__instance).Visible = false;
			((Node)__instance).ProcessMode = (ProcessModeEnum)4;
			Log.Info("[local.action_game] end-turn button hidden", 2);
		}
	}

	[HarmonyPatch(typeof(NEndTurnButton), "RefreshEnabled")]
	private static class EndTurnButtonRefreshPatch
	{
		private static bool Prefix(NEndTurnButton __instance)
		{
			((CanvasItem)__instance).Visible = false;
			return false;
		}
	}

	private static bool _eventsHooked;

	public static void SubscribeCombatEvents()
	{
		CombatManager instance = CombatManager.Instance;
		if (instance == null)
		{
			Log.Warn("[local.action_game] CombatManager.Instance null at init; will bind on SetUpCombat", 2);
		}
		else
		{
			BindManager(instance);
		}
	}

	private static void BindManager(CombatManager manager)
	{
		if (!_eventsHooked)
		{
			manager.CombatSetUp += OnCombatSetUp;
			manager.CombatEnded += OnCombatEnded;
			_eventsHooked = true;
			Log.Info("[local.action_game] combat events bound", 2);
		}
	}

	private static void OnCombatSetUp(CombatState state)
	{
		CombatAccess.ResetWinWatchdog();
		CombatOutcomeNetSync.Reset();
		EncounterTimerSystem.OnCombatStarted();
		ActionGameNetSync.OnCombatStarted();
		AutoCardSelectSystem.OnCombatStarted();
		SelfDamageDemonFormSystem.OnCombatStarted();
		Log.Info("[local.action_game] combat setup event", 2);
	}

	private static void OnCombatEnded(CombatRoom room)
	{
		Log.Info("[local.action_game] combat ended", 2);
		RealtimeCombatTicker.Stop();
		EncounterTimerSystem.OnCombatEnded();
		ActionGameNetSync.OnCombatEnded();
		AutoCardSelectSystem.OnCombatEnded();
		SelfDamageDemonFormSystem.OnCombatEnded();
		CombatAccess.ResetWinWatchdog();
		CombatManager instance = CombatManager.Instance;
		if (instance != null)
		{
			CombatCompat.CancelTurnState(instance);
		}
	}
}
internal static class CombatAccess
{
	internal static bool AllowDoTurnEnd;

	private static bool _forceWinning;

	private static bool _deferredForceRequested;

	private static string _deferredReason = "";

	private static TaskCompletionSource<bool>? _deferredTcs;

	private static double _emptyThreatSec;

	private static double _adaptableStuckSec;

	private static double _forceAttemptSec;

	private static int _forceLogCounter;

	public static bool TimedWinBypass;

	public static void SetPlayerActionsDisabled(bool disabled)
	{
		CombatManager instance = CombatManager.Instance;
		if (instance != null)
		{
			Traverse.Create((object)instance).Property("PlayerActionsDisabled", (object[])null).SetValue((object)disabled);
		}
	}

	public static async Task RunDoTurnEnd(Player player, PlayerChoiceContext choiceContext)
	{
		CombatManager manager = CombatManager.Instance;
		if (manager == null || player == null)
		{
			return;
		}
		AllowDoTurnEnd = true;
		try
		{
			Task task = CombatCompat.InvokeDoTurnEnd(manager, player, choiceContext);
			if (task != null)
			{
				await task;
			}
		}
		finally
		{
			AllowDoTurnEnd = false;
		}
	}

	public static async Task<bool> CheckWinAfterDeathEffects()
	{
		CombatManager manager = CombatManager.Instance;
		if (manager == null)
		{
			return true;
		}
		if (!manager.IsInProgress || manager.IsOverOrEnding)
		{
			return true;
		}
		return await manager.CheckWinCondition() || !manager.IsInProgress || manager.IsOverOrEnding;
	}

	internal static bool HasLivingEnemies(CombatState? state)
	{
		if (state == null)
		{
			return false;
		}
		return state.Enemies.Any((Creature e) => e != null && e.IsAlive);
	}

	internal static bool ShouldStopCombatFromEnding(ICombatState? state)
	{
		if (state == null)
		{
			return false;
		}
		return Hook.ShouldStopCombatFromEnding(state);
	}

	internal static bool ShouldEnemyActThisCycle(Creature? enemy, ICombatState state)
	{
		if (enemy == null || !enemy.IsMonster)
		{
			return false;
		}
		if (enemy.IsAlive)
		{
			return true;
		}
		return enemy.GetPower<AdaptablePower>() != null;
	}

	internal static string DescribeEnemies(CombatState? state)
	{
		if (((state != null) ? state.Enemies : null) == null)
		{
			return "enemies=<null>";
		}
		if (state.Enemies.Count == 0)
		{
			return "enemies=[]";
		}
		return "enemies=[" + string.Join(", ", state.Enemies.Select((Creature e) => (e != null) ? $"{e.LogName} hp={e.CurrentHp}/{e.MaxHp} alive={e.IsAlive} primary={e.IsPrimaryEnemy} stunned={e.IsStunned}" : "null")) + "]";
	}

	public static void ResetWinWatchdog()
	{
		_forceWinning = false;
		_deferredForceRequested = false;
		_deferredReason = "";
		_deferredTcs = null;
		_emptyThreatSec = 0.0;
		_adaptableStuckSec = 0.0;
		_forceAttemptSec = 0.0;
		_forceLogCounter = 0;
		TimedWinBypass = false;
	}

	public static Task<bool> RequestDeferredForceWin(string reason)
	{
		if (_deferredTcs != null)
		{
			Log.Info($"[{"local.action_game"}] deferred force win already pending ({_deferredReason}), also from {reason}", 2);
			return _deferredTcs.Task;
		}
		_deferredTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		_deferredForceRequested = true;
		_deferredReason = reason;
		_forceAttemptSec = 0.0;
		Log.Info("[local.action_game] deferred force win scheduled reason=" + reason, 2);
		return _deferredTcs.Task;
	}

	public static void TickWinWatchdog(double delta, CombatState? state)
	{
		CombatManager instance = CombatManager.Instance;
		if (instance == null)
		{
			return;
		}
		if (!instance.IsInProgress)
		{
			CompleteDeferred(value: true);
		}
		else if (_deferredForceRequested || _forceWinning)
		{
			_forceAttemptSec += delta;
			if (_forceLogCounter++ % 30 == 0)
			{
				Log.Info($"[{"local.action_game"}] force-win tick t={_forceAttemptSec:0.00}s inProgress={instance.IsInProgress} ending={instance.IsEnding} overOrEnding={instance.IsOverOrEnding} {DescribeEnemies(state)}", 2);
			}
			if (!_forceWinning)
			{
				Log.Info("[local.action_game] force-win begin (deferred outside CheckWin stack) reason=" + _deferredReason, 2);
				ForceWinNow(instance);
			}
			else if (_forceAttemptSec >= 3.0)
			{
				Log.Warn("[local.action_game] force-win hard timeout 3s -> cancel CTS + IsInProgress=false", 2);
				EmergencyAbortCombat(instance);
				CompleteDeferred(value: true);
			}
			else if (ActionGameNet.IsOnline)
			{
				CombatOutcomeNetSync.TickBurst();
			}
		}
		else if (HasLivingEnemies(state))
		{
			_emptyThreatSec = 0.0;
			_adaptableStuckSec = 0.0;
		}
		else if (EncounterTimerSystem.BlocksEarlyWin)
		{
			if (_emptyThreatSec > 0.0)
			{
				Log.Info($"[{"local.action_game"}] win watchdog hold (elapsed={EncounterTimerSystem.ElapsedSec:0.#}s)", 2);
			}
			_emptyThreatSec = 0.0;
			_adaptableStuckSec = 0.0;
		}
		else if (ShouldStopCombatFromEnding((ICombatState?)(object)state))
		{
			_emptyThreatSec = 0.0;
			_adaptableStuckSec += delta;
			if (_adaptableStuckSec >= 12.0)
			{
				Log.Warn($"[{"local.action_game"}] adaptable revive stuck {_adaptableStuckSec:0.#}s -> force win. " + DescribeEnemies(state), 2);
				_adaptableStuckSec = 0.0;
				RequestDeferredForceWin("adaptable-stuck");
			}
		}
		else
		{
			_adaptableStuckSec = 0.0;
			_emptyThreatSec += delta;
			if (!(_emptyThreatSec < 1.0))
			{
				Log.Info("[local.action_game] win watchdog empty 1s -> schedule force. " + DescribeEnemies(state), 2);
				_emptyThreatSec = 0.0;
				RequestDeferredForceWin("watchdog");
			}
		}
	}

	public static async Task ForceWinNow(CombatManager manager)
	{
		if (_forceWinning)
		{
			Log.Info("[local.action_game] ForceWinNow skipped (already running)", 2);
			return;
		}
		if (manager == null)
		{
			CompleteDeferred(value: true);
			return;
		}
		if (!manager.IsInProgress)
		{
			Log.Info("[local.action_game] ForceWinNow skipped (!IsInProgress)", 2);
			CompleteDeferred(value: true);
			return;
		}
		_forceWinning = true;
		_forceAttemptSec = 0.0;
		SetPlayerActionsDisabled(disabled: false);
		RealtimeCombatTicker.Stop();
		if (ActionGameNet.IsOnline)
		{
			CombatOutcomeNetSync.BroadcastWin(_deferredReason ?? "force-win");
		}
		CombatState state = manager.DebugOnlyGetState();
		Log.Info($"[{"local.action_game"}] ForceWinNow enter ending={manager.IsEnding} overOrEnding={manager.IsOverOrEnding} {DescribeEnemies(state)}", 2);
		try
		{
			Task task = CombatCompat.InvokeEndCombatInternal(manager);
			if (task == null)
			{
				Log.Warn("[local.action_game] EndCombatInternal returned null task", 2);
				EmergencyAbortCombat(manager);
				EnsureActionExecutorAlive("force-win-null-task");
				CompleteDeferred(value: true);
				return;
			}
			Log.Info("[local.action_game] EndCombatInternal started, awaiting with 2s WhenAny", 2);
			if (await Task.WhenAny(task, Task.Delay(2000)) == task)
			{
				await task;
				Log.Info($"[{"local.action_game"}] EndCombatInternal completed normally inProgress={manager.IsInProgress}", 2);
				EnsureActionExecutorAlive("after-force-win");
				CompleteDeferred(value: true);
			}
			else
			{
				Log.Warn($"[{"local.action_game"}] EndCombatInternal WhenAny timeout 2s (task.Status={task.Status})", 2);
				EmergencyAbortCombat(manager);
				EnsureActionExecutorAlive("force-win-timeout");
				CompleteDeferred(value: true);
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Log.Warn($"[{"local.action_game"}] ForceWinNow exception: {ex}", 2);
			EmergencyAbortCombat(manager);
			EnsureActionExecutorAlive("force-win-exception");
			CompleteDeferred(value: true);
		}
		finally
		{
			_forceWinning = false;
			_deferredForceRequested = false;
		}
	}

	public static void EnsureActionExecutorAlive(string reason)
	{
		ClearStuckCombatActions(reason);
	}

	public static void ClearStuckCombatActions(string reason)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Invalid comparison between Unknown and I4
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Invalid comparison between Unknown and I4
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0243: Invalid comparison between Unknown and I4
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02da: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e0: Invalid comparison between Unknown and I4
		RunManager instance = RunManager.Instance;
		ActionQueueSynchronizer val = ((instance != null) ? instance.ActionQueueSynchronizer : null);
		ActionExecutor val2 = ((instance != null) ? instance.ActionExecutor : null);
		if (val == null)
		{
			return;
		}
		if (val2 != null && val2.IsPaused)
		{
			val2.Unpause();
		}
		if ((int)val.CombatState > 0)
		{
			val.SetCombatState((ActionSynchronizerCombatState)0);
		}
		object value = Traverse.Create((object)val).Field("_actionQueueSet").GetValue<object>();
		if (value == null)
		{
			return;
		}
		Traverse.Create(value).Field("_isInCombat").SetValue((object)false);
		Traverse.Create(value).Method("UnpauseAllPlayerQueues", Array.Empty<object>()).GetValue();
		GameAction val3 = ((val2 != null) ? val2.CurrentlyRunningAction : null);
		if (val3 != null && (int)val3.ActionType != 3)
		{
			Log.Info($"[{"local.action_game"}] cancel CurrentlyRunningAction {val3} state={val3.State} ({reason})", 2);
			val3.Cancel();
			Traverse.Create((object)val2).Property("CurrentlyRunningAction", (object[])null).SetValue((object)null);
		}
		int num = 0;
		IList value2 = Traverse.Create(value).Field("_actionQueues").GetValue<IList>();
		if (value2 != null)
		{
			foreach (object item in value2)
			{
				IList value3 = Traverse.Create(item).Field("actions").GetValue<IList>();
				if (value3 == null || value3.Count == 0)
				{
					continue;
				}
				for (int num2 = value3.Count - 1; num2 >= 0; num2--)
				{
					object? obj = value3[num2];
					GameAction val4 = (GameAction)((obj is GameAction) ? obj : null);
					if (val4 != null && (int)val4.ActionType != 3)
					{
						Log.Info($"[{"local.action_game"}] drop combat action {val4} state={val4.State} ({reason})", 2);
						if ((int)val4.State != 6)
						{
							val4.Cancel();
						}
						value3.RemoveAt(num2);
						num++;
					}
				}
				Traverse.Create(item).Field("isPaused").SetValue((object)false);
			}
		}
		if (val2 != null)
		{
			TaskCompletionSource<bool> value4 = Traverse.Create((object)val2).Field("_queueTaskCompletionSource").GetValue<TaskCompletionSource<bool>>();
			if (value4 != null && !value4.Task.IsCompleted)
			{
				value4.TrySetResult(result: true);
			}
			Traverse.Create((object)val2).Field("_queueTaskCompletionSource").SetValue((object)null);
			Traverse.Create((object)val2).Method("ActionQueueChanged", Array.Empty<object>()).GetValue();
		}
		Log.Info($"[{"local.action_game"}] ClearStuckCombatActions({reason}) cleared={num} execRunning={((val2 != null) ? new bool?(val2.IsRunning) : null)}", 2);
	}

	public static void EnsureMapTravelOnly(string reason)
	{
		//IL_023c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Invalid comparison between Unknown and I4
		NMapScreen instance = NMapScreen.Instance;
		RunManager instance2 = RunManager.Instance;
		ActionExecutor val = ((instance2 != null) ? instance2.ActionExecutor : null);
		Log.Info($"[{"local.action_game"}] EnsureMapTravelOnly({reason}) map={instance != null} travel={((instance != null) ? new bool?(instance.IsTravelEnabled) : null)} traveling={((instance != null) ? new bool?(instance.IsTraveling) : null)} open={((instance != null) ? new bool?(instance.IsOpen) : null)} execPaused={((val != null) ? new bool?(val.IsPaused) : null)} execRunning={((val != null) ? new bool?(val.IsRunning) : null)}", 2);
		ClearStuckCombatActions(reason);
		if (instance == null)
		{
			return;
		}
		instance.IsTraveling = false;
		instance.SetTravelEnabled(true);
		if (!instance.IsTravelEnabled)
		{
			Traverse.Create((object)instance).Field("<IsTravelEnabled>k__BackingField").SetValue((object)true);
			Log.Warn("[local.action_game] forced IsTravelEnabled=true", 2);
		}
		Traverse.Create((object)instance).Method("RecalculateTravelability", Array.Empty<object>()).GetValue();
		int num = 0;
		Dictionary<MapCoord, NMapPoint> value = Traverse.Create((object)instance).Field("_mapPointDictionary").GetValue<Dictionary<MapCoord, NMapPoint>>();
		if (value != null)
		{
			foreach (NMapPoint value2 in value.Values)
			{
				if (value2 != null && (int)value2.State == 1)
				{
					num++;
				}
			}
		}
		Log.Info($"[{"local.action_game"}] EnsureMapTravelOnly done travel={instance.IsTravelEnabled} traveling={instance.IsTraveling} travelableNodes={num} execRunning={((val != null) ? new bool?(val.IsRunning) : null)}", 2);
	}

	public static void EnsurePostCombatNavigation(string reason)
	{
		SetPlayerActionsDisabled(disabled: false);
		EnsureMapTravelOnly(reason);
	}

	private static void CancelCombatCts(CombatManager manager)
	{
		CombatCompat.CancelTurnState(manager);
	}

	private static void EmergencyAbortCombat(CombatManager manager)
	{
		CancelCombatCts(manager);
		CombatCompat.SetInProgress(manager, value: false);
		SetPlayerActionsDisabled(disabled: false);
		RealtimeCombatTicker.Stop();
		AutoCardSelectSystem.OnCombatEnded();
		EnsurePostCombatNavigation("emergency-abort");
		Log.Warn("[local.action_game] emergency abort: IsInProgress forced false", 2);
	}

	private static void CompleteDeferred(bool value)
	{
		TaskCompletionSource<bool> deferredTcs = _deferredTcs;
		_deferredTcs = null;
		_deferredForceRequested = false;
		deferredTcs?.TrySetResult(value);
	}

	public static void FreezePlayerPlayPhase()
	{
		CombatManager instance = CombatManager.Instance;
		if (instance == null)
		{
			return;
		}
		SetPlayerActionsDisabled(disabled: false);
		CombatState val = instance.DebugOnlyGetState();
		if (val == null)
		{
			return;
		}
		foreach (Player player in val.Players)
		{
			if (player.PlayerCombatState != null)
			{
				player.PlayerCombatState.Phase = (PlayerTurnPhase)3;
			}
		}
	}
}
internal static class WinCheckIntercept
{
	private static bool _runningRealCheck;

	public static bool TryPrefix(CombatManager manager, ref Task<bool> result, string source)
	{
		if (_runningRealCheck)
		{
			return true;
		}
		CombatState state = manager.DebugOnlyGetState();
		Log.Info($"[{"local.action_game"}] CheckWinCondition[{source}] prefix inProgress={manager.IsInProgress} ending={manager.IsEnding} hold={EncounterTimerSystem.BlocksEarlyWin} elapsed={EncounterTimerSystem.ElapsedSec:0.#}s {CombatAccess.DescribeEnemies(state)}", 2);
		if (!manager.IsInProgress)
		{
			result = Task.FromResult(result: true);
			return false;
		}
		if (CombatAccess.TimedWinBypass)
		{
			Log.Info("[local.action_game] CheckWin timed-room bypass", 2);
			result = Task.FromResult(result: true);
			return false;
		}
		if (CombatAccess.HasLivingEnemies(state))
		{
			object pendingLoss = CombatCompat.GetPendingLoss(manager);
			if (pendingLoss != null)
			{
				Log.Info("[local.action_game] CheckWin: pending loss with living enemies -> vanilla", 2);
				result = RunRealCheck(manager);
				return false;
			}
			Log.Info("[local.action_game] CheckWin blocked (living enemies remain, incl. secondary)", 2);
			result = Task.FromResult(result: false);
			return false;
		}
		if (EncounterTimerSystem.BlocksEarlyWin)
		{
			Log.Info("[local.action_game] CheckWin empty but normal-room timer hold -> not win", 2);
			result = Task.FromResult(result: false);
			return false;
		}
		if (CombatAccess.ShouldStopCombatFromEnding((ICombatState?)(object)state))
		{
			Log.Info("[local.action_game] CheckWin empty but ShouldStopCombatFromEnding -> not win", 2);
			result = Task.FromResult(result: false);
			return false;
		}
		Log.Info("[local.action_game] CheckWin empty -> deferred force (no await on this stack)", 2);
		CombatOutcomeNetSync.BroadcastWin("CheckWinCondition[" + source + "]");
		result = CombatAccess.RequestDeferredForceWin("CheckWinCondition[" + source + "]");
		return false;
	}

	private static async Task<bool> RunRealCheck(CombatManager manager)
	{
		_runningRealCheck = true;
		try
		{
			bool result = await manager.CheckWinCondition();
			Log.Info($"[{"local.action_game"}] vanilla CheckWinCondition -> {result}", 2);
			return result;
		}
		finally
		{
			_runningRealCheck = false;
		}
	}
}
[HarmonyPatch(typeof(CombatManager), "CheckWinCondition", new Type[] { })]
internal static class DelayedWinCheckPatch
{
	private static bool Prefix(CombatManager __instance, ref Task<bool> __result)
	{
		return WinCheckIntercept.TryPrefix(__instance, ref __result, "()");
	}
}
[HarmonyPatch]
internal static class DelayedWinCheckWithTurnStatePatch
{
	private static MethodBase? TargetMethod()
	{
		Type type = AccessTools.TypeByName("MegaCrit.Sts2.Core.Combat.CombatTurnState");
		if (type == null)
		{
			return null;
		}
		return AccessTools.Method(typeof(CombatManager), "CheckWinCondition", new Type[1] { type }, (Type[])null);
	}

	private static bool Prefix(CombatManager __instance, ref Task<bool> __result)
	{
		return WinCheckIntercept.TryPrefix(__instance, ref __result, "(CombatTurnState)");
	}
}
[HarmonyPatch(typeof(CombatManager), "get_IsEnding")]
internal static class IsEndingHoldPatch
{
	private static void Postfix(ref bool __result)
	{
		if (__result && EncounterTimerSystem.BlocksEarlyWin)
		{
			__result = false;
		}
	}
}
[HarmonyPatch(typeof(CombatManager), "get_IsOverOrEnding")]
internal static class IsOverOrEndingHoldPatch
{
	private static void Postfix(ref bool __result)
	{
		if (__result && EncounterTimerSystem.BlocksEarlyWin)
		{
			__result = false;
		}
	}
}
[HarmonyPatch(typeof(RunManager), "ProceedFromTerminalRewardsScreen")]
internal static class ProceedFromRewardsPatch
{
	private static void Prefix()
	{
		Log.Info("[local.action_game] ProceedFromTerminalRewardsScreen begin", 2);
	}

	private static void Postfix(ref Task __result)
	{
		__result = Continue(__result);
	}

	private static async Task Continue(Task original)
	{
		if (original != null)
		{
			await original;
		}
		Log.Info("[local.action_game] ProceedFromTerminalRewardsScreen done", 2);
		CombatAccess.EnsureMapTravelOnly("after-rewards");
	}
}
[HarmonyPatch(typeof(ActChangeSynchronizer), "SetLocalPlayerReady")]
internal static class ActChangeReadyPatch
{
	private static void Prefix()
	{
		CombatAccess.EnsureActionExecutorAlive("act-change-ready");
	}
}
internal static class CombatPeerUtil
{
	public readonly struct PeerTarget
	{
		public readonly Creature Creature;

		public readonly Vector2 Pos;

		public readonly bool IsLocalOwner;

		public readonly ulong NetId;

		public PeerTarget(Creature creature, Vector2 pos, bool isLocalOwner, ulong netId)
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			Creature = creature;
			Pos = pos;
			IsLocalOwner = isLocalOwner;
			NetId = netId;
		}
	}

	public static Player? FindPlayer(ulong netId)
	{
		CombatManager instance = CombatManager.Instance;
		CombatState val = ((instance != null) ? instance.DebugOnlyGetState() : null);
		if (((val != null) ? val.Players : null) == null)
		{
			return null;
		}
		foreach (Player player in val.Players)
		{
			if (player != null && player.NetId == netId)
			{
				return player;
			}
		}
		return null;
	}

	public static NCreature? GetPlayerNode(Player player)
	{
		if (((player != null) ? player.Creature : null) == null)
		{
			return null;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		return (instance != null) ? instance.GetCreatureNode(player.Creature) : null;
	}

	public static bool TryGetPose(ulong netId, out Vector2 pos)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (PlayerPoseSync.TryGetRemotePose(netId, out pos))
		{
			return true;
		}
		Player val = FindPlayer(netId);
		NCreature val2 = ((val != null) ? GetPlayerNode(val) : null);
		if (val2 != null && GodotObject.IsInstanceValid((GodotObject)(object)val2))
		{
			pos = ((Control)val2).GlobalPosition;
			return true;
		}
		pos = default(Vector2);
		return false;
	}

	public static void CollectPlayerTargets(List<PeerTarget> into)
	{
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		into.Clear();
		NCombatRoom instance = NCombatRoom.Instance;
		CombatManager instance2 = CombatManager.Instance;
		CombatState val = ((instance2 != null) ? instance2.DebugOnlyGetState() : null);
		if (instance == null || ((val != null) ? val.Players : null) == null)
		{
			return;
		}
		foreach (Player player in val.Players)
		{
			if (((player != null) ? player.Creature : null) == null || !player.Creature.IsAlive)
			{
				continue;
			}
			bool flag = LocalContext.IsMe(player);
			Vector2 pos;
			if (flag)
			{
				NCreature creatureNode = instance.GetCreatureNode(player.Creature);
				if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
				{
					continue;
				}
				pos = ((Control)creatureNode).GlobalPosition;
			}
			else if (!PlayerPoseSync.TryGetRemotePose(player.NetId, out pos))
			{
				NCreature creatureNode2 = instance.GetCreatureNode(player.Creature);
				if (creatureNode2 == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode2))
				{
					continue;
				}
				pos = ((Control)creatureNode2).GlobalPosition;
			}
			into.Add(new PeerTarget(player.Creature, pos, flag, player.NetId));
		}
	}

	public static bool PickNearestPlayer(Vector2 from, List<PeerTarget> peers, out PeerTarget best)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		best = default(PeerTarget);
		float num = float.MaxValue;
		bool result = false;
		for (int i = 0; i < peers.Count; i++)
		{
			PeerTarget peerTarget = peers[i];
			float num2 = ((Vector2)(ref from)).DistanceSquaredTo(peerTarget.Pos);
			if (!(num2 >= num))
			{
				num = num2;
				best = peerTarget;
				result = true;
			}
		}
		return result;
	}

	public static Creature? PickNearestEnemy(Vector2 origin, float maxDist = 520f)
	{
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return null;
		}
		Creature result = null;
		float num = float.MaxValue;
		float num2 = maxDist * maxDist;
		foreach (NCreature creatureNode in instance.CreatureNodes)
		{
			if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				continue;
			}
			Creature entity = creatureNode.Entity;
			if (OstyCombatUtil.IsHostileMonster(entity) && entity.IsAlive)
			{
				float num3 = ((Vector2)(ref origin)).DistanceSquaredTo(((Control)creatureNode).GlobalPosition);
				if (!(num3 > num2) && !(num3 >= num))
				{
					num = num3;
					result = entity;
				}
			}
		}
		return result;
	}

	public static void PushEnemyFromPoint(Creature target, Vector2 fromPos)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		if (target != null && target.IsAlive)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature val = ((instance != null) ? instance.GetCreatureNode(target) : null);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				Vector2 val2 = ((Control)val).GlobalPosition - fromPos;
				Vector2 away = ((((Vector2)(ref val2)).LengthSquared() > 4f) ? ((Vector2)(ref val2)).Normalized() : Vector2.Right);
				EnemyChaseSystem.PushFromDirection(target, away);
			}
		}
	}
}
internal static class CombatScreenClamp
{
	private const float Margin = 52f;

	public static void Clamp(NCreature? node)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		Viewport viewport = ((Node)node).GetViewport();
		Rect2 val = (Rect2)((viewport != null) ? viewport.GetVisibleRect() : default(Rect2));
		if (!(((Rect2)(ref val)).Size.X < 8f) && !(((Rect2)(ref val)).Size.Y < 8f))
		{
			Vector2 globalPosition = ((Control)node).GlobalPosition;
			Vector2 val2 = ((Rect2)(ref val)).Position + new Vector2(52f, 52f);
			Vector2 val3 = ((Rect2)(ref val)).End - new Vector2(52f, 52f);
			if (!(val3.X < val2.X) && !(val3.Y < val2.Y))
			{
				globalPosition.X = Mathf.Clamp(globalPosition.X, val2.X, val3.X);
				globalPosition.Y = Mathf.Clamp(globalPosition.Y, val2.Y, val3.Y);
				((Control)node).GlobalPosition = globalPosition;
			}
		}
	}
}
internal static class ControlsHintPopup
{
	private static CanvasLayer? _layer;

	private static CheckBox? _skipCheck;

	private static bool _open;

	public static bool IsOpen => _open;

	public static void TryShow()
	{
		if (_open || RealtimeCombatSettings.SkipControlsHint || ActionGameNet.IsOnline || !IsFirstCombat())
		{
			return;
		}
		EnsureUi();
		if (_layer != null && GodotObject.IsInstanceValid((GodotObject)(object)_layer))
		{
			if (_skipCheck != null && GodotObject.IsInstanceValid((GodotObject)(object)_skipCheck))
			{
				((BaseButton)_skipCheck).ButtonPressed = false;
			}
			_layer.Visible = true;
			_open = true;
			PlayerChoicePause.Begin();
			Log.Info("[local.action_game] controls hint shown", 2);
		}
	}

	public static void Hide()
	{
		if (_open)
		{
			if (_skipCheck != null && GodotObject.IsInstanceValid((GodotObject)(object)_skipCheck) && ((BaseButton)_skipCheck).ButtonPressed)
			{
				RealtimeCombatSettings.SkipControlsHint = true;
			}
			if (_layer != null && GodotObject.IsInstanceValid((GodotObject)(object)_layer))
			{
				_layer.Visible = false;
			}
			_open = false;
			PlayerChoicePause.End();
			Log.Info("[local.action_game] controls hint dismissed", 2);
		}
	}

	private static bool IsFirstCombat()
	{
		RunManager instance = RunManager.Instance;
		RunState val = ((instance != null) ? instance.DebugOnlyGetState() : null);
		if (val == null)
		{
			return false;
		}
		if (val.TotalFloor <= 1)
		{
			return true;
		}
		return CountCombatRooms(val) <= 1;
	}

	private static int CountCombatRooms(RunState run)
	{
		if (run.MapPointHistory == null)
		{
			return 0;
		}
		int num = 0;
		foreach (IReadOnlyList<MapPointHistoryEntry> item in run.MapPointHistory)
		{
			if (item == null)
			{
				continue;
			}
			foreach (MapPointHistoryEntry item2 in item)
			{
				if (item2 != null && (item2.HasRoomOfType((RoomType)1) || item2.HasRoomOfType((RoomType)2) || item2.HasRoomOfType((RoomType)3)))
				{
					num++;
				}
			}
		}
		return num;
	}

	private static void EnsureUi()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Expected O, but got Unknown
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Expected O, but got Unknown
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Expected O, but got Unknown
		//IL_0176: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Expected O, but got Unknown
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Expected O, but got Unknown
		//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Expected O, but got Unknown
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Expected O, but got Unknown
		if (_layer == null || !GodotObject.IsInstanceValid((GodotObject)(object)_layer))
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (((val != null) ? val.Root : null) != null)
			{
				_layer = new CanvasLayer
				{
					Name = StringName.op_Implicit("ActionGameControlsHint"),
					Layer = 80,
					Visible = false
				};
				Control val2 = new Control
				{
					Name = StringName.op_Implicit("ControlsHintRoot"),
					MouseFilter = (MouseFilterEnum)0
				};
				val2.SetAnchorsPreset((LayoutPreset)15, false);
				ColorRect val3 = new ColorRect
				{
					Color = new Color(0f, 0f, 0f, 0.65f),
					MouseFilter = (MouseFilterEnum)0
				};
				((Control)val3).SetAnchorsPreset((LayoutPreset)15, false);
				PanelContainer val4 = new PanelContainer
				{
					MouseFilter = (MouseFilterEnum)0
				};
				((Control)val4).SetAnchorsPreset((LayoutPreset)8, false);
				((Control)val4).OffsetLeft = -320f;
				((Control)val4).OffsetRight = 320f;
				((Control)val4).OffsetTop = -240f;
				((Control)val4).OffsetBottom = 240f;
				MarginContainer val5 = new MarginContainer();
				((Control)val5).AddThemeConstantOverride(StringName.op_Implicit("margin_left"), 28);
				((Control)val5).AddThemeConstantOverride(StringName.op_Implicit("margin_right"), 28);
				((Control)val5).AddThemeConstantOverride(StringName.op_Implicit("margin_top"), 24);
				((Control)val5).AddThemeConstantOverride(StringName.op_Implicit("margin_bottom"), 24);
				VBoxContainer val6 = new VBoxContainer();
				((Control)val6).AddThemeConstantOverride(StringName.op_Implicit("separation"), 14);
				Label val7 = new Label
				{
					Text = "WASD移动角色\nJ加速打出卡牌\n与敌人越近，单体攻击的伤害越高\n目标是撑过一段时间\n\nWASD to move\nJ to quickly play cards\nThe closer you are to an enemy, the more damage single-target attacks deal\nSurvive for a period of time",
					HorizontalAlignment = (HorizontalAlignment)0,
					AutowrapMode = (AutowrapMode)3
				};
				((Control)val7).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 22);
				_skipCheck = new CheckBox
				{
					Text = "下次不再提示/Don't show again",
					FocusMode = (FocusModeEnum)0
				};
				Button val8 = new Button
				{
					Text = "确定 / OK",
					FocusMode = (FocusModeEnum)0,
					CustomMinimumSize = new Vector2(0f, 40f)
				};
				((BaseButton)val8).Pressed += Hide;
				((Node)val6).AddChild((Node)(object)val7, false, (InternalMode)0);
				((Node)val6).AddChild((Node)(object)_skipCheck, false, (InternalMode)0);
				((Node)val6).AddChild((Node)(object)val8, false, (InternalMode)0);
				((Node)val5).AddChild((Node)(object)val6, false, (InternalMode)0);
				((Node)val4).AddChild((Node)(object)val5, false, (InternalMode)0);
				((Node)val2).AddChild((Node)(object)val3, false, (InternalMode)0);
				((Node)val2).AddChild((Node)(object)val4, false, (InternalMode)0);
				((Node)_layer).AddChild((Node)(object)val2, false, (InternalMode)0);
				((Node)val.Root).AddChild((Node)(object)_layer, false, (InternalMode)0);
			}
		}
	}
}
internal static class CreatureFacing
{
	private const float MoveFlipDeadzone = 0.55f;

	private const float DeltaFlipDeadzone = 36f;

	private const ulong FlipCooldownMs = 220uL;

	private static readonly Dictionary<ulong, ulong> LastFlipMs = new Dictionary<ulong, ulong>();

	public static void FaceByMoveX(NCreature node, float moveX)
	{
		if (node != null && !(Math.Abs(moveX) < 0.55f))
		{
			bool flag = moveX > 0f;
			if (node.Entity != null && node.Entity.IsMonster)
			{
				flag = !flag;
			}
			FaceRight(node, flag);
		}
	}

	public static void FaceTowardDeltaX(NCreature node, float deltaX)
	{
		if (node != null && !(Math.Abs(deltaX) < 36f))
		{
			bool flag = deltaX > 0f;
			if (node.Entity != null && node.Entity.IsMonster)
			{
				flag = !flag;
			}
			FaceRight(node, flag);
		}
	}

	public static int GetWorldFaceSign(NCreature node)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (((node != null) ? node.Visuals : null) == null || !GodotObject.IsInstanceValid((GodotObject)(object)node.Visuals))
		{
			return 1;
		}
		float x = ((Node2D)node.Visuals).Scale.X;
		if (Math.Abs(x) < 0.0001f)
		{
			return 1;
		}
		if (node.Entity != null && node.Entity.IsMonster)
		{
			return (x < 0f) ? 1 : (-1);
		}
		return (x > 0f) ? 1 : (-1);
	}

	public static void FaceRight(NCreature node, bool faceRight)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		if (node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		NCreatureVisuals visuals = node.Visuals;
		if (visuals == null || !GodotObject.IsInstanceValid((GodotObject)(object)visuals))
		{
			return;
		}
		Vector2 scale = ((Node2D)visuals).Scale;
		float num = Math.Abs(scale.X);
		if (num < 0.0001f)
		{
			num = Math.Abs(visuals.DefaultScale);
		}
		if (num < 0.0001f)
		{
			num = 1f;
		}
		float num2 = (faceRight ? num : (0f - num));
		if (!(Math.Abs(scale.X - num2) < 0.0001f))
		{
			ulong instanceId = ((GodotObject)node).GetInstanceId();
			ulong ticksMsec = Time.GetTicksMsec();
			if (!LastFlipMs.TryGetValue(instanceId, out var value) || ticksMsec - value >= 220)
			{
				float num3 = ((Math.Abs(scale.Y) < 0.0001f) ? num : scale.Y);
				((Node2D)visuals).Scale = new Vector2(num2, num3);
				LastFlipMs[instanceId] = ticksMsec;
			}
		}
	}
}
internal static class CreatureYSortSystem
{
	private static bool _hooked;

	private static readonly Dictionary<ulong, float> BaseScale = new Dictionary<ulong, float>();

	private const float CreatureScale = 0.5f;

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
			}
		}
	}

	private static void OnProcessFrame()
	{
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsOverOrEnding || CombatAccess.TimedWinBypass)
		{
			if (instance == null || !instance.IsInProgress)
			{
				BaseScale.Clear();
			}
			return;
		}
		NCombatRoom instance2 = NCombatRoom.Instance;
		if (instance2 == null)
		{
			return;
		}
		foreach (NCreature creatureNode in instance2.CreatureNodes)
		{
			if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				((CanvasItem)creatureNode).ZAsRelative = false;
				((CanvasItem)creatureNode).ZIndex = (int)((Control)creatureNode).GlobalPosition.Y;
				Creature entity = creatureNode.Entity;
				MonsterModel val = ((entity != null) ? entity.Monster : null);
				if (!(val is Crusher) && !(val is Rocket))
				{
					ApplyHalfScale(creatureNode);
				}
				HideCreatureUi((Node)(object)creatureNode);
			}
		}
	}

	private static void ApplyHalfScale(NCreature node)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		NCreatureVisuals visuals = node.Visuals;
		if (visuals == null || !GodotObject.IsInstanceValid((GodotObject)(object)visuals))
		{
			return;
		}
		ulong instanceId = ((GodotObject)node).GetInstanceId();
		if (!BaseScale.TryGetValue(instanceId, out var value))
		{
			value = Math.Abs(((Node2D)visuals).Scale.X);
			if (value < 0.05f)
			{
				value = Math.Abs(visuals.DefaultScale);
			}
			if (value < 0.05f)
			{
				return;
			}
			BaseScale[instanceId] = value;
		}
		float num = value * 0.5f;
		if (Math.Abs(visuals.DefaultScale - num) > 0.02f)
		{
			visuals.DefaultScale = num;
		}
		float num2 = Math.Abs(((Node2D)visuals).Scale.X);
		if (!(num2 <= value * 0.75f) || !(Math.Abs(num2 - num) < 0.08f))
		{
			float num3 = ((((Node2D)visuals).Scale.X < 0f) ? (-1f) : 1f);
			((Node2D)visuals).Scale = new Vector2(num3 * num, num);
		}
	}

	private static void HideCreatureUi(Node node)
	{
		NCreature val = (NCreature)(object)((node is NCreature) ? node : null);
		if (val != null)
		{
			Control intentContainer = val.IntentContainer;
			if (intentContainer != null && GodotObject.IsInstanceValid((GodotObject)(object)intentContainer))
			{
				((CanvasItem)intentContainer).Visible = false;
			}
		}
		if ((node is NHealthBar || node is NIntent || node is NPower || node is NPowerContainer) ? true : false)
		{
			CanvasItem val2 = (CanvasItem)(object)((node is CanvasItem) ? node : null);
			if (val2 != null)
			{
				val2.Visible = false;
			}
		}
		int childCount = node.GetChildCount(false);
		for (int i = 0; i < childCount; i++)
		{
			HideCreatureUi(node.GetChild(i, false));
		}
	}
}
[HarmonyPatch(typeof(NCreature), "ShowHoverTips")]
internal static class HideEnemyHoverTipsPatch
{
	private static bool Prefix(NCreature __instance)
	{
		return ((__instance != null) ? __instance.Entity : null) == null || !__instance.Entity.IsMonster;
	}
}
[HarmonyPatch(typeof(NCreature), "ShowCreatureHoverTips")]
internal static class HideEnemyCreatureHoverTipsPatch
{
	private static bool Prefix(NCreature __instance)
	{
		return ((__instance != null) ? __instance.Entity : null) == null || !__instance.Entity.IsMonster;
	}
}
[HarmonyPatch(typeof(ThinkCmd), "Play")]
internal static class HideHandFullThoughtPatch
{
	private static bool Prefix(LocString line)
	{
		return line == null || line.LocEntryKey != "HAND_FULL";
	}
}
internal enum DebugForcedMonster
{
	Off,
	WaterfallGiant,
	Crusher,
	Wriggler,
	TestSubject
}
internal static class DebugCheatSystem
{
	public static bool Enabled;

	public static bool ForceDenseVegetation;

	public static DebugForcedMonster ForcedMonster;

	private static bool _hooked;

	private static bool _f5WasDown;

	private static bool _f6WasDown;

	private static bool _f7WasDown;

	private static bool _f8WasDown;

	private static bool _dexBusy;

	private static bool _buffBusy;

	private static CanvasLayer? _layer;

	private static Label? _label;

	public static void EnsureHook()
	{
		if (Enabled && !_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
				EnsureHud();
				Log.Info("[local.action_game] debug cheats F5=事件 F6=刷怪 F7=-10敏捷 F8=+10力量+1敏捷", 2);
			}
		}
	}

	public static bool TryForcedSpawn(out SpawnPick pick)
	{
		pick = default(SpawnPick);
		if (!Enabled)
		{
			return false;
		}
		switch (ForcedMonster)
		{
		case DebugForcedMonster.WaterfallGiant:
			pick = new SpawnPick(new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<WaterfallGiant>(), IsGroup: false), 1);
			return true;
		case DebugForcedMonster.Wriggler:
			pick = new SpawnPick(new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Wriggler>(), IsGroup: true), 4);
			return true;
		case DebugForcedMonster.TestSubject:
			pick = new SpawnPick(new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<TestSubject>(), IsGroup: false), 1);
			return true;
		default:
			return false;
		}
	}

	internal static EventModel MakeDenseVegetation()
	{
		return (EventModel)(object)ModelDb.Event<DenseVegetation>();
	}

	internal static EncounterModel? ForcedEncounter()
	{
		if (!Enabled)
		{
			return null;
		}
		DebugForcedMonster forcedMonster = ForcedMonster;
		if (1 == 0)
		{
		}
		EncounterModel result = (EncounterModel)(forcedMonster switch
		{
			DebugForcedMonster.WaterfallGiant => ModelDb.Encounter<WaterfallGiantBoss>(), 
			DebugForcedMonster.Crusher => ModelDb.Encounter<KaiserCrabBoss>(), 
			DebugForcedMonster.TestSubject => ModelDb.Encounter<TestSubjectBoss>(), 
			_ => null, 
		});
		if (1 == 0)
		{
		}
		return result;
	}

	private static void OnProcessFrame()
	{
		EnsureHud();
		bool flag = Input.IsPhysicalKeyPressed((Key)4194336) || Input.IsKeyPressed((Key)4194336);
		if (flag && !_f5WasDown)
		{
			ForceDenseVegetation = !ForceDenseVegetation;
			Log.Info($"[{"local.action_game"}] cheat event DenseVegetation={ForceDenseVegetation}", 2);
			RefreshHud();
		}
		_f5WasDown = flag;
		bool flag2 = Input.IsPhysicalKeyPressed((Key)4194337) || Input.IsKeyPressed((Key)4194337);
		if (flag2 && !_f6WasDown)
		{
			DebugForcedMonster forcedMonster = ForcedMonster;
			if (1 == 0)
			{
			}
			DebugForcedMonster forcedMonster2 = forcedMonster switch
			{
				DebugForcedMonster.Off => DebugForcedMonster.WaterfallGiant, 
				DebugForcedMonster.WaterfallGiant => DebugForcedMonster.Crusher, 
				DebugForcedMonster.Crusher => DebugForcedMonster.Wriggler, 
				DebugForcedMonster.Wriggler => DebugForcedMonster.TestSubject, 
				_ => DebugForcedMonster.Off, 
			};
			if (1 == 0)
			{
			}
			ForcedMonster = forcedMonster2;
			Log.Info($"[{"local.action_game"}] cheat monster={ForcedMonster}", 2);
			RefreshHud();
		}
		_f6WasDown = flag2;
		bool flag3 = Input.IsPhysicalKeyPressed((Key)4194338) || Input.IsKeyPressed((Key)4194338);
		if (flag3 && !_f7WasDown)
		{
			ApplyMinusDex();
		}
		_f7WasDown = flag3;
		bool flag4 = Input.IsPhysicalKeyPressed((Key)4194339) || Input.IsKeyPressed((Key)4194339);
		if (flag4 && !_f8WasDown)
		{
			ApplyStrengthAndDex();
		}
		_f8WasDown = flag4;
	}

	private static async Task ApplyMinusDex()
	{
		if (_dexBusy)
		{
			return;
		}
		_dexBusy = true;
		try
		{
			Creature creature = LocalPlayerCreature();
			if (creature == null || !creature.IsAlive)
			{
				Log.Warn("[local.action_game] cheat -10 dex skipped (no alive player)", 2);
				return;
			}
			BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
			await PowerCmd.Apply<DexterityPower>((PlayerChoiceContext)(object)ctx, creature, -10m, creature, (CardModel)null, false);
			Log.Info("[local.action_game] cheat applied Dexterity -10 to " + creature.LogName, 2);
			RefreshHud();
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Log.Warn($"[{"local.action_game"}] cheat -10 dex failed: {ex}", 2);
		}
		finally
		{
			_dexBusy = false;
		}
	}

	private static async Task ApplyStrengthAndDex()
	{
		if (_buffBusy)
		{
			return;
		}
		_buffBusy = true;
		try
		{
			Creature creature = LocalPlayerCreature();
			if (creature == null || !creature.IsAlive)
			{
				Log.Warn("[local.action_game] cheat +10 str / +1 dex skipped (no alive player)", 2);
				return;
			}
			BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
			await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)(object)ctx, creature, 10m, creature, (CardModel)null, false);
			await PowerCmd.Apply<DexterityPower>((PlayerChoiceContext)(object)ctx, creature, 1m, creature, (CardModel)null, false);
			Log.Info("[local.action_game] cheat applied Strength +10 Dexterity +1 to " + creature.LogName, 2);
			RefreshHud();
		}
		finally
		{
			_buffBusy = false;
		}
	}

	private static Creature? LocalPlayerCreature()
	{
		CombatManager instance = CombatManager.Instance;
		CombatState val = ((instance != null) ? instance.DebugOnlyGetState() : null);
		if (val != null)
		{
			Player me = LocalContext.GetMe((ICombatState)(object)val);
			if (((me != null) ? me.Creature : null) != null)
			{
				return me.Creature;
			}
		}
		RunManager instance2 = RunManager.Instance;
		RunState val2 = ((instance2 != null) ? instance2.DebugOnlyGetState() : null);
		if (val2 == null)
		{
			return null;
		}
		Player me2 = LocalContext.GetMe((IPlayerCollection)(object)val2);
		return (me2 != null) ? me2.Creature : null;
	}

	private static void EnsureHud()
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Expected O, but got Unknown
		if (_layer == null || !GodotObject.IsInstanceValid((GodotObject)(object)_layer))
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (((val != null) ? val.Root : null) != null)
			{
				_layer = new CanvasLayer
				{
					Name = StringName.op_Implicit("ActionGameDebugCheatHud"),
					Layer = 90
				};
				_label = new Label
				{
					Name = StringName.op_Implicit("CheatHint"),
					MouseFilter = (MouseFilterEnum)2,
					HorizontalAlignment = (HorizontalAlignment)1
				};
				((Control)_label).SetAnchorsPreset((LayoutPreset)10, false);
				((Control)_label).OffsetTop = 8f;
				((Control)_label).OffsetBottom = 48f;
				((Node)_layer).AddChild((Node)(object)_label, false, (InternalMode)0);
				((Node)val.Root).AddChild((Node)(object)_layer, false, (InternalMode)0);
				RefreshHud();
			}
		}
	}

	private static void RefreshHud()
	{
		if (_label != null && GodotObject.IsInstanceValid((GodotObject)(object)_label))
		{
			string value = (ForceDenseVegetation ? "密林植被" : "关");
			DebugForcedMonster forcedMonster = ForcedMonster;
			if (1 == 0)
			{
			}
			string text = forcedMonster switch
			{
				DebugForcedMonster.WaterfallGiant => "瀑布巨兽(下场战斗)", 
				DebugForcedMonster.Crusher => "帝王蟹(下场战斗)", 
				DebugForcedMonster.Wriggler => "蠕虫x4(刷怪)", 
				DebugForcedMonster.TestSubject => "实验体(下场战斗/刷怪)", 
				_ => "关", 
			};
			if (1 == 0)
			{
			}
			string value2 = text;
			_label.Text = $"测试  F5事件={value}  F6刷怪={value2}  F7给自己-10敏捷  F8给自己+10力量+1敏捷";
		}
	}
}
[HarmonyPatch]
internal static class ForcePullNextEventPatch
{
	private static bool Prepare()
	{
		return DebugCheatSystem.Enabled && TargetMethod() != null;
	}

	private static MethodBase? TargetMethod()
	{
		Type type = AccessTools.TypeByName("MegaCrit.Sts2.Core.Models.ActModel") ?? typeof(EventModel).Assembly.GetTypes().FirstOrDefault((Type t) => t.Name == "ActModel");
		return (type == null) ? null : AccessTools.Method(type, "PullNextEvent", (Type[])null, (Type[])null);
	}

	private static bool Prefix(ref EventModel __result)
	{
		if (!DebugCheatSystem.ForceDenseVegetation)
		{
			return true;
		}
		__result = DebugCheatSystem.MakeDenseVegetation();
		Log.Info("[local.action_game] PullNextEvent -> DenseVegetation", 2);
		return false;
	}
}
[HarmonyPatch]
internal static class ForceEventRoomCtorPatch
{
	private static MethodBase? TargetMethod()
	{
		return AccessTools.Constructor(typeof(EventRoom), new Type[1] { typeof(EventModel) }, false);
	}

	private static bool Prepare()
	{
		return DebugCheatSystem.Enabled && TargetMethod() != null;
	}

	private static void Prefix(ref EventModel __0)
	{
		if (DebugCheatSystem.ForceDenseVegetation && !(__0 is DenseVegetation))
		{
			__0 = DebugCheatSystem.MakeDenseVegetation();
			Log.Info("[local.action_game] EventRoom ctor forced DenseVegetation", 2);
		}
	}
}
[HarmonyPatch]
internal static class ForceCreateRoomEncounterPatch
{
	private static bool Prepare()
	{
		return DebugCheatSystem.Enabled && TargetMethod() != null;
	}

	private static MethodBase? TargetMethod()
	{
		return AccessTools.Method(typeof(RunManager), "CreateRoom", (Type[])null, (Type[])null);
	}

	private static void Prefix(RoomType __0, ref AbstractModel __2)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Invalid comparison between Unknown and I4
		if (__0 - 1 <= 2)
		{
			EncounterModel val = DebugCheatSystem.ForcedEncounter();
			if (val != null)
			{
				__2 = (AbstractModel)(object)val.ToMutable();
				Log.Info("[local.action_game] CreateRoom encounter -> " + ((object)val).GetType().Name, 2);
			}
		}
	}
}
[HarmonyPatch]
internal static class ForcePullNextEncounterPatch
{
	private static bool Prepare()
	{
		return DebugCheatSystem.Enabled && TargetMethod() != null;
	}

	private static MethodBase? TargetMethod()
	{
		Type type = AccessTools.TypeByName("MegaCrit.Sts2.Core.Models.ActModel") ?? typeof(EventModel).Assembly.GetTypes().FirstOrDefault((Type t) => t.Name == "ActModel");
		return (type == null) ? null : AccessTools.Method(type, "PullNextEncounter", (Type[])null, (Type[])null);
	}

	private static bool Prefix(ref EncounterModel __result)
	{
		EncounterModel val = DebugCheatSystem.ForcedEncounter();
		if (val == null)
		{
			return true;
		}
		__result = val;
		Log.Info("[local.action_game] PullNextEncounter -> " + ((object)val).GetType().Name, 2);
		return false;
	}
}
[HarmonyPatch(typeof(NEventOptionButton), "_Ready")]
internal static class EventOptionAlwaysClickablePatch
{
	private static void Postfix(NEventOptionButton __instance)
	{
		EventOption option = __instance.Option;
		if (option == null || !option.IsLocked)
		{
			__instance.EnableButton();
		}
	}
}
internal static class EncounterTimerSystem
{
	private static bool _hooked;

	private static bool _active;

	private static bool _fired;

	private static bool _eliteClearWinStarted;

	private static double _elapsedSec;

	private static CanvasLayer? _layer;

	private static Label? _label;

	public static bool SpawningOpen { get; private set; } = true;


	public static double LimitSec
	{
		get
		{
			string text = SpawnRules.CurrentActName();
			if (1 == 0)
			{
			}
			double result = text switch
			{
				"Hive" => 25.0, 
				"Glory" => 30.0, 
				"Overgrowth" => 20.0, 
				_ => 20.0, 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}

	public static bool BlocksEarlyWin
	{
		get
		{
			if (!_active || _fired)
			{
				return false;
			}
			if (!IsNormalRoom())
			{
				return false;
			}
			return ElapsedSec < LimitSec;
		}
	}

	public static double ElapsedSec => _active ? _elapsedSec : 0.0;

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
			}
		}
	}

	public static void OnCombatStarted()
	{
		EnsureHook();
		_active = true;
		_fired = false;
		_eliteClearWinStarted = false;
		SpawningOpen = true;
		_elapsedSec = 0.0;
		Log.Info($"[{"local.action_game"}] encounter timer start hold={IsNormalRoom()} act={SpawnRules.CurrentActName()} limit={LimitSec:0.#}s", 2);
	}

	public static void OnCombatEnded()
	{
		_active = false;
		_fired = false;
		_eliteClearWinStarted = false;
		SpawningOpen = true;
		_elapsedSec = 0.0;
		Hide();
	}

	private static void OnProcessFrame()
	{
		EnsureHook();
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress)
		{
			if (_active)
			{
				OnCombatEnded();
			}
			return;
		}
		if (!_active)
		{
			OnCombatStarted();
		}
		if (!_fired && !PlayerChoicePause.IsPaused)
		{
			NCombatRoom instance2 = NCombatRoom.Instance;
			double num = ((instance2 != null && GodotObject.IsInstanceValid((GodotObject)(object)instance2)) ? ((Node)instance2).GetProcessDeltaTime() : (1.0 / 60.0));
			if (num <= 0.0)
			{
				num = 1.0 / 60.0;
			}
			_elapsedSec += num;
		}
		double limitSec = LimitSec;
		double num2 = Math.Max(0.0, limitSec - ElapsedSec);
		Show((int)Math.Ceiling(num2));
		if (!IsNormalRoom())
		{
			TryEliteClearWin();
		}
		if (_fired)
		{
			if (IsNormalRoom())
			{
				HideMonsters();
			}
		}
		else if (!(num2 > 0.0))
		{
			_fired = true;
			if (!IsNormalRoom())
			{
				SpawningOpen = false;
				AutoSpawnSystem.ClearAll();
				Log.Info("[local.action_game] encounter timer elapsed - elite/boss stop spawn; clear locally to win all", 2);
				TryEliteClearWin();
			}
			else if (CombatOutcomeNetSync.SuppressLocalHostOnlyTimer)
			{
				Log.Info("[local.action_game] encounter timer elapsed on client - wait host outcome", 2);
				HideMonsters();
			}
			else
			{
				Log.Info("[local.action_game] encounter timer elapsed - normal room force win", 2);
				HideMonsters();
				CombatAccess.TimedWinBypass = true;
				CombatOutcomeNetSync.BroadcastWin("room-timer");
				CombatAccess.RequestDeferredForceWin("room-timer");
			}
		}
	}

	private static void TryEliteClearWin()
	{
		if (_eliteClearWinStarted)
		{
			return;
		}
		CombatManager instance = CombatManager.Instance;
		if (instance != null && instance.IsInProgress && !instance.IsOverOrEnding)
		{
			CombatState state = instance.DebugOnlyGetState();
			if (!CombatAccess.HasLivingEnemies(state) && !CombatAccess.ShouldStopCombatFromEnding((ICombatState?)(object)state))
			{
				_eliteClearWinStarted = true;
				Log.Info("[local.action_game] elite/boss local clear -> force win all peers", 2);
				CombatAccess.TimedWinBypass = true;
				CombatOutcomeNetSync.BroadcastWin("elite-clear");
				CombatAccess.RequestDeferredForceWin("elite-clear");
			}
		}
	}

	private static bool IsNormalRoom()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Invalid comparison between Unknown and I4
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		CombatManager instance = CombatManager.Instance;
		CombatState val = ((instance != null) ? instance.DebugOnlyGetState() : null);
		EncounterModel val2 = ((val != null) ? val.Encounter : null);
		if (val2 != null)
		{
			return (int)val2.RoomType != 2 && (int)val2.RoomType != 3;
		}
		return true;
	}

	private static void HideMonsters()
	{
		AutoSpawnSystem.ClearAll();
		GoldDropSystem.ClearAll();
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return;
		}
		foreach (NCreature creatureNode in instance.CreatureNodes)
		{
			HideMonster(creatureNode);
		}
		foreach (NCreature removingCreatureNode in instance.RemovingCreatureNodes)
		{
			HideMonster(removingCreatureNode);
		}
	}

	private static void HideMonster(NCreature? node)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node) && node.Entity != null && node.Entity.IsMonster)
		{
			((CanvasItem)node).Visible = false;
			((CanvasItem)node).ZAsRelative = false;
			((CanvasItem)node).ZIndex = -4096;
			((CanvasItem)node).Modulate = new Color(1f, 1f, 1f, 0f);
		}
	}

	private static void Show(int seconds)
	{
		EnsureLabel();
		if (_label != null)
		{
			((CanvasItem)_label).Visible = true;
			if (IsNormalRoom())
			{
				_label.Text = $"剩余 {seconds}s";
			}
			else if (_fired)
			{
				_label.Text = (ActionGameNet.IsOnline ? "停刷：任一人清场则全员胜利" : "停刷：杀光剩余敌人");
			}
			else
			{
				_label.Text = $"剩余 {seconds}s\n杀死所有敌人";
			}
		}
	}

	private static void Hide()
	{
		if (_label != null && GodotObject.IsInstanceValid((GodotObject)(object)_label))
		{
			((CanvasItem)_label).Visible = false;
		}
	}

	private static void EnsureLabel()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		if (_layer == null || !GodotObject.IsInstanceValid((GodotObject)(object)_layer) || _label == null || !GodotObject.IsInstanceValid((GodotObject)(object)_label))
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (((val != null) ? val.Root : null) != null)
			{
				_layer = new CanvasLayer
				{
					Name = StringName.op_Implicit("ActionGameEncounterTimer"),
					Layer = 66
				};
				_label = new Label
				{
					Name = StringName.op_Implicit("ActionGameEncounterTimerLabel"),
					MouseFilter = (MouseFilterEnum)2,
					HorizontalAlignment = (HorizontalAlignment)1,
					VerticalAlignment = (VerticalAlignment)0,
					Visible = false
				};
				((Control)_label).SetAnchorsPreset((LayoutPreset)5, false);
				((Control)_label).OffsetLeft = -220f;
				((Control)_label).OffsetRight = 220f;
				((Control)_label).OffsetTop = 92f;
				((Control)_label).OffsetBottom = 180f;
				((Control)_label).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 34);
				((Control)_label).AddThemeColorOverride(StringName.op_Implicit("font_color"), new Color(1f, 0.92f, 0.55f, 1f));
				((Control)_label).AddThemeColorOverride(StringName.op_Implicit("font_outline_color"), new Color(0f, 0f, 0f, 1f));
				((Control)_label).AddThemeConstantOverride(StringName.op_Implicit("outline_size"), 8);
				((Node)_layer).AddChild((Node)(object)_label, false, (InternalMode)0);
				((Node)val.Root).AddChild((Node)(object)_layer, false, (InternalMode)0);
			}
		}
	}
}
internal static class EnemyAttackHud
{
	private static CanvasLayer? _layer;

	private static ProgressBar? _bar;

	private static Label? _label;

	public static void Show()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected O, but got Unknown
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Expected O, but got Unknown
		Hide();
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		if (((val != null) ? val.Root : null) == null)
		{
			Log.Warn("[local.action_game] EnemyAttackHud: no SceneTree root", 2);
			return;
		}
		_layer = new CanvasLayer
		{
			Name = StringName.op_Implicit("RealtimeEnemyAttackHud"),
			Layer = 64
		};
		Control val2 = new Control
		{
			Name = StringName.op_Implicit("EnemyAttackHudRoot"),
			MouseFilter = (MouseFilterEnum)2
		};
		val2.SetAnchorsPreset((LayoutPreset)15, false);
		_bar = new ProgressBar
		{
			Name = StringName.op_Implicit("EnemyAttackBar"),
			MinValue = 0.0,
			MaxValue = 1.0,
			Value = 0.0,
			ShowPercentage = false,
			MouseFilter = (MouseFilterEnum)2
		};
		((Control)_bar).SetAnchorsPreset((LayoutPreset)5, false);
		((Control)_bar).Position = new Vector2(-220f, 168f);
		((Control)_bar).Size = new Vector2(440f, 22f);
		((Control)_bar).OffsetLeft = -220f;
		((Control)_bar).OffsetRight = 220f;
		((Control)_bar).OffsetTop = 168f;
		((Control)_bar).OffsetBottom = 190f;
		((CanvasItem)_bar).Modulate = new Color(1f, 0.45f, 0.35f, 0.95f);
		_label = new Label
		{
			Name = StringName.op_Implicit("EnemyAttackLabel"),
			HorizontalAlignment = (HorizontalAlignment)1,
			VerticalAlignment = (VerticalAlignment)1,
			MouseFilter = (MouseFilterEnum)2,
			Text = "离敌人行动还有 3.0s"
		};
		((Control)_label).SetAnchorsPreset((LayoutPreset)5, false);
		((Control)_label).OffsetLeft = -220f;
		((Control)_label).OffsetRight = 220f;
		((Control)_label).OffsetTop = 192f;
		((Control)_label).OffsetBottom = 216f;
		((Node)val2).AddChild((Node)(object)_bar, false, (InternalMode)0);
		((Node)val2).AddChild((Node)(object)_label, false, (InternalMode)0);
		((Node)_layer).AddChild((Node)(object)val2, false, (InternalMode)0);
		Node val3 = (Node)(object)val.Root;
		NCombatRoom instance = NCombatRoom.Instance;
		if (((instance != null) ? instance.Ui : null) != null)
		{
			val3 = (Node)(object)instance.Ui;
		}
		val3.AddChild((Node)(object)_layer, false, (InternalMode)0);
		Log.Info($"[{"local.action_game"}] EnemyAttackHud shown under {val3.Name}", 2);
	}

	public static void Update(double enemyTimer, double intervalSec, bool enemyActing)
	{
		if (_bar == null || !GodotObject.IsInstanceValid((GodotObject)(object)_bar))
		{
			return;
		}
		double value = ((intervalSec <= 0.0) ? 0.0 : Math.Clamp(enemyTimer / intervalSec, 0.0, 1.0));
		((Range)_bar).Value = value;
		if (_label != null && GodotObject.IsInstanceValid((GodotObject)(object)_label))
		{
			if (enemyActing)
			{
				_label.Text = "敌人行动中";
				return;
			}
			double value2 = Math.Max(0.0, intervalSec - enemyTimer);
			_label.Text = $"离敌人行动还有 {value2:0.0}s";
		}
	}

	public static void Hide()
	{
		if (_layer != null && GodotObject.IsInstanceValid((GodotObject)(object)_layer))
		{
			((Node)_layer).QueueFree();
		}
		_layer = null;
		_bar = null;
		_label = null;
	}
}
internal enum EnemyBehaviorKind
{
	Chase,
	Charge,
	PredictiveCharge,
	Flank,
	Stationary
}
internal static class EnemyBehaviorCatalog
{
	public static EnemyBehaviorKind Resolve(Creature creature)
	{
		MonsterModel monster = creature.Monster;
		if (monster == null)
		{
			return EnemyBehaviorKind.Chase;
		}
		if ((monster is Crusher || monster is Rocket) ? true : false)
		{
			return EnemyBehaviorKind.Stationary;
		}
		if (IsEliteOrBoss(monster))
		{
			return EnemyBehaviorKind.Chase;
		}
		if (1 == 0)
		{
		}
		EnemyBehaviorKind result = ((!(monster is CorpseSlug)) ? ((monster is Seapunk) ? EnemyBehaviorKind.Charge : ((monster is SludgeSpinner) ? EnemyBehaviorKind.PredictiveCharge : ((monster is Toadpole) ? EnemyBehaviorKind.Flank : ((!(monster is Guardbot)) ? ((monster is Noisebot) ? EnemyBehaviorKind.Charge : ((monster is Zapbot) ? EnemyBehaviorKind.PredictiveCharge : ((monster is Stabbot) ? EnemyBehaviorKind.Flank : ((!(monster is FuzzyWurmCrawler)) ? ((!(monster is LeafSlimeM)) ? ((monster is LeafSlimeS) ? EnemyBehaviorKind.Charge : ((!(monster is TwigSlimeM)) ? ((monster is TwigSlimeS) ? EnemyBehaviorKind.Charge : ((monster is Nibbit) ? EnemyBehaviorKind.PredictiveCharge : ((monster is ShrinkerBeetle) ? EnemyBehaviorKind.Flank : ((!(monster is BowlbugRock)) ? ((monster is BowlbugNectar) ? EnemyBehaviorKind.PredictiveCharge : ((monster is BowlbugEgg) ? EnemyBehaviorKind.Flank : ((monster is Tunneler) ? EnemyBehaviorKind.Charge : ((monster is Exoskeleton) ? EnemyBehaviorKind.Flank : ((monster is ThievingHopper) ? EnemyBehaviorKind.Flank : ((monster is LivingShield) ? EnemyBehaviorKind.Flank : ((monster is ScrollOfBiting) ? EnemyBehaviorKind.Charge : ((monster is DevotedSculptor) ? EnemyBehaviorKind.PredictiveCharge : ((monster is TurretOperator) ? EnemyBehaviorKind.PredictiveCharge : ((!(monster is CalcifiedCultist)) ? ((monster is DampCultist) ? EnemyBehaviorKind.Flank : ((monster is FatGremlin) ? EnemyBehaviorKind.Charge : ((monster is FossilStalker) ? EnemyBehaviorKind.PredictiveCharge : ((monster is GasBomb) ? EnemyBehaviorKind.Charge : ((monster is GremlinMerc) ? EnemyBehaviorKind.PredictiveCharge : ((monster is HauntedShip) ? EnemyBehaviorKind.Flank : ((!(monster is LivingFog)) ? ((!(monster is PunchConstruct)) ? ((!(monster is SewerClam)) ? ((monster is SneakyGremlin) ? EnemyBehaviorKind.Flank : ((monster is TwoTailedRat) ? EnemyBehaviorKind.Charge : ((monster is AssassinRubyRaider) ? EnemyBehaviorKind.Flank : ((monster is AxeRubyRaider) ? EnemyBehaviorKind.Charge : ((monster is BruteRubyRaider) ? EnemyBehaviorKind.Charge : ((monster is CrossbowRubyRaider) ? EnemyBehaviorKind.PredictiveCharge : ((monster is TrackerRubyRaider) ? EnemyBehaviorKind.PredictiveCharge : ((!(monster is CubexConstruct)) ? ((monster is EyeWithTeeth) ? EnemyBehaviorKind.Flank : ((monster is Flyconid) ? EnemyBehaviorKind.Charge : ((!(monster is Fogmog)) ? ((monster is Inklet) ? EnemyBehaviorKind.PredictiveCharge : ((monster is Mawler) ? EnemyBehaviorKind.Charge : ((monster is SlitheringStrangler) ? EnemyBehaviorKind.Flank : ((monster is SnappingJaxfruit) ? EnemyBehaviorKind.Charge : ((!(monster is VineShambler)) ? ((!(monster is BowlbugSilk)) ? ((monster is Chomper) ? EnemyBehaviorKind.Charge : ((monster is HunterKiller) ? EnemyBehaviorKind.Charge : ((monster is LouseProgenitor) ? EnemyBehaviorKind.Flank : ((monster is Myte) ? EnemyBehaviorKind.PredictiveCharge : ((monster is Ovicopter) ? EnemyBehaviorKind.PredictiveCharge : ((monster is Parafright) ? EnemyBehaviorKind.Flank : ((!(monster is SlumberingBeetle)) ? ((monster is SpinyToad) ? EnemyBehaviorKind.Charge : ((monster is TheObscura) ? EnemyBehaviorKind.PredictiveCharge : ((!(monster is ToughEgg)) ? ((monster is Axebot) ? EnemyBehaviorKind.Charge : ((!(monster is Fabricator)) ? ((!(monster is FrogKnight)) ? ((!(monster is GlobeHead)) ? ((monster is OwlMagistrate) ? EnemyBehaviorKind.PredictiveCharge : ((monster is SlimedBerserker) ? EnemyBehaviorKind.Charge : ((monster is TheForgotten) ? EnemyBehaviorKind.Flank : ((monster is TheLost) ? EnemyBehaviorKind.Flank : EnemyBehaviorKind.Chase)))) : EnemyBehaviorKind.Chase) : EnemyBehaviorKind.Chase) : EnemyBehaviorKind.Chase)) : EnemyBehaviorKind.Chase))) : EnemyBehaviorKind.Chase))))))) : EnemyBehaviorKind.Chase) : EnemyBehaviorKind.Chase))))) : EnemyBehaviorKind.Chase))) : EnemyBehaviorKind.Chase)))))))) : EnemyBehaviorKind.Chase) : EnemyBehaviorKind.Chase) : EnemyBehaviorKind.Chase))))))) : EnemyBehaviorKind.Chase)))))))))) : EnemyBehaviorKind.Chase)))) : EnemyBehaviorKind.Chase)) : EnemyBehaviorKind.Chase) : EnemyBehaviorKind.Chase)))) : EnemyBehaviorKind.Chase)))) : EnemyBehaviorKind.Chase);
		if (1 == 0)
		{
		}
		return result;
	}

	public static bool IsRegularMinion(Creature? creature)
	{
		MonsterModel val = ((creature != null) ? creature.Monster : null);
		if (val == null)
		{
			return false;
		}
		if ((val is Crusher || val is Rocket) ? true : false)
		{
			return false;
		}
		return !IsEliteOrBoss(val);
	}

	public static bool SkipsMelee(Creature creature)
	{
		if (creature == null)
		{
			return false;
		}
		MonsterModel monster = creature.Monster;
		if ((monster is Crusher || monster is Rocket) ? true : false)
		{
			return true;
		}
		EnemyBehaviorKind enemyBehaviorKind = Resolve(creature);
		return (uint)(enemyBehaviorKind - 1) <= 1u;
	}

	private static bool IsEliteOrBoss(MonsterModel monster)
	{
		if (1 == 0)
		{
		}
		bool result = monster is LagavulinMatriarch || monster is SoulFysh || monster is WaterfallGiant || monster is PhantasmalGardener || monster is SkulkingColony || monster is TerrorEel || monster is CeremonialBeast || monster is KinFollower || monster is KinPriest || monster is Vantom || monster is BygoneEffigy || monster is Byrdonis || monster is PhrogParasite || monster is Wriggler || monster is Crusher || monster is KnowledgeDemon || monster is Rocket || monster is TheInsatiable || monster is DecimillipedeSegmentBack || monster is DecimillipedeSegmentFront || monster is DecimillipedeSegmentMiddle || monster is Entomancer || monster is InfestedPrism || monster is Aeonglass || monster is Queen || monster is TestSubject || monster is TorchHeadAmalgam || ((monster is FlailKnight || monster is MagiKnight || monster is MechaKnight || monster is SoulNexus || monster is SpectralKnight) ? true : false);
		if (1 == 0)
		{
		}
		return result;
	}
}
internal static class EnemyChaseSystem
{
	private enum ChargePhase
	{
		Approach,
		Windup,
		Dash,
		Recover
	}

	private sealed class ChargeState
	{
		public ChargePhase Phase;

		public float Timer;

		public Vector2 Dir;

		public Vector2 LockedAim;

		public bool Hit;

		public bool DamageBusy;

		public ColorRect? Line;
	}

	private const float SeekWeight = 1f;

	private const float SeparateWeight = 1.6f;

	private const float SeparateRadius = 100f;

	private const float ArriveStop = 48f;

	private const float AxisAlignSoft = 40f;

	private const float ChargeStartRange = 300f;

	private const float ChargeWindupSec = 0.65f;

	private const float ChargeDashSec = 0.912f;

	private const float ChargeRecoverSec = 0.55f;

	private const float ChargeSpeedMul = 3.6f;

	private const float ChargeHitRadius = 72f;

	private const float PredictSeconds = 0.4f;

	private const float ChargeLineLenWindup = 128f;

	private const float ChargeLineLenDash = 176f;

	private const float KnockbackDist = 150f;

	private const float KnockbackSec = 0.12f;

	private static bool _hooked;

	private static readonly List<NCreature> _monsters = new List<NCreature>(32);

	private static readonly Dictionary<ulong, ChargeState> _charges = new Dictionary<ulong, ChargeState>();

	private static readonly List<ulong> _deadCharges = new List<ulong>();

	private static readonly Dictionary<ulong, Vector2> _knockLeft = new Dictionary<ulong, Vector2>();

	private static readonly Dictionary<ulong, Vector2> _inertia = new Dictionary<ulong, Vector2>();

	private const float MoveInertiaSec = 0.2f;

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
			}
		}
	}

	private static void OnProcessFrame()
	{
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0250: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsOverOrEnding)
		{
			ClearCharges();
			_knockLeft.Clear();
			_inertia.Clear();
		}
		else
		{
			if (PlayerChoicePause.IsPaused)
			{
				return;
			}
			NCombatRoom instance2 = NCombatRoom.Instance;
			if (instance2 == null)
			{
				return;
			}
			CombatState val = instance.DebugOnlyGetState();
			if (val == null)
			{
				return;
			}
			List<CombatPeerUtil.PeerTarget> list = new List<CombatPeerUtil.PeerTarget>(4);
			CombatPeerUtil.CollectPlayerTargets(list);
			Player me = LocalContext.GetMe((ICombatState)(object)val);
			Creature val2 = ((me != null) ? me.Creature : null);
			if (val2 == null || !val2.IsAlive)
			{
				return;
			}
			NCreature creatureNode = instance2.GetCreatureNode(val2);
			if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				return;
			}
			if (list.Count == 0)
			{
				list.Add(new CombatPeerUtil.PeerTarget(val2, ((Control)creatureNode).GlobalPosition, isLocalOwner: true, me.NetId));
			}
			Creature osty;
			NCreature node;
			bool flag = OstyCombatUtil.TryGetAliveOsty(me, out osty, out node);
			Vector2 ostyPos = (flag ? ((Control)node).GlobalPosition : ((Control)creatureNode).GlobalPosition);
			float num = (float)((Node)creatureNode).GetProcessDeltaTime();
			if (num <= 0f)
			{
				num = (float)((Node)creatureNode).GetPhysicsProcessDeltaTime();
			}
			_monsters.Clear();
			foreach (NCreature creatureNode2 in instance2.CreatureNodes)
			{
				if (creatureNode2 != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode2) && OstyCombatUtil.IsHostileMonster(creatureNode2.Entity))
				{
					_monsters.Add(creatureNode2);
				}
			}
			int count = _monsters.Count;
			if (count != 0)
			{
				HashSet<ulong> hashSet = new HashSet<ulong>();
				for (int i = 0; i < count; i++)
				{
					NCreature val3 = _monsters[i];
					EnemyBehaviorKind enemyBehaviorKind = EnemyBehaviorCatalog.Resolve(val3.Entity);
					hashSet.Add(((GodotObject)val3).GetInstanceId());
					if (enemyBehaviorKind != EnemyBehaviorKind.Stationary)
					{
						PickTargetMulti(((Control)val3).GlobalPosition, list, flag, osty, ostyPos, out Creature target, out Vector2 targetPos, out bool targetIsLocalOwner);
						if ((uint)(enemyBehaviorKind - 1) <= 1u)
						{
							TickCharge(val3, target, targetPos, num, instance2, enemyBehaviorKind == EnemyBehaviorKind.PredictiveCharge, targetIsLocalOwner);
						}
						else
						{
							TickChase(val3, enemyBehaviorKind, targetPos, num, count, i);
						}
						ApplyKnockback(val3, num);
						CombatScreenClamp.Clamp(val3);
					}
				}
				_deadCharges.Clear();
				foreach (ulong key in _charges.Keys)
				{
					if (!hashSet.Contains(key))
					{
						_deadCharges.Add(key);
					}
				}
				foreach (ulong deadCharge in _deadCharges)
				{
					if (_charges.Remove(deadCharge, out ChargeState value))
					{
						FreeLine(value);
					}
				}
				_deadCharges.Clear();
				foreach (ulong key2 in _inertia.Keys)
				{
					if (!hashSet.Contains(key2))
					{
						_deadCharges.Add(key2);
					}
				}
				{
					foreach (ulong deadCharge2 in _deadCharges)
					{
						_inertia.Remove(deadCharge2);
					}
					return;
				}
			}
			ClearCharges();
			_inertia.Clear();
		}
	}

	private static void PickTargetMulti(Vector2 from, List<CombatPeerUtil.PeerTarget> peers, bool hasOsty, Creature osty, Vector2 ostyPos, out Creature target, out Vector2 targetPos, out bool targetIsLocalOwner)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		targetIsLocalOwner = true;
		if (!CombatPeerUtil.PickNearestPlayer(from, peers, out var best))
		{
			target = peers[0].Creature;
			targetPos = peers[0].Pos;
			targetIsLocalOwner = peers[0].IsLocalOwner;
		}
		else
		{
			target = best.Creature;
			targetPos = best.Pos;
			targetIsLocalOwner = best.IsLocalOwner;
		}
		if (hasOsty && osty != null)
		{
			float num = ((Vector2)(ref from)).DistanceSquaredTo(targetPos);
			float num2 = ((Vector2)(ref from)).DistanceSquaredTo(ostyPos);
			if (num2 < num)
			{
				target = osty;
				targetPos = ostyPos;
				targetIsLocalOwner = true;
			}
		}
	}

	private static void PickTarget(Vector2 from, Creature player, Vector2 playerPos, bool hasOsty, Creature osty, Vector2 ostyPos, out Creature target, out Vector2 targetPos)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		if (!hasOsty)
		{
			target = player;
			targetPos = playerPos;
			return;
		}
		float num = ((Vector2)(ref from)).DistanceSquaredTo(playerPos);
		float num2 = ((Vector2)(ref from)).DistanceSquaredTo(ostyPos);
		if (num2 < num)
		{
			target = osty;
			targetPos = ostyPos;
		}
		else
		{
			target = player;
			targetPos = playerPos;
		}
	}

	private static void TickChase(NCreature node, EnemyBehaviorKind kind, Vector2 targetPos, float delta, int count, int index)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		Vector2 globalPosition = ((Control)node).GlobalPosition;
		Vector2 val = targetPos - globalPosition;
		float x = val.X;
		Vector2 val2 = ((kind == EnemyBehaviorKind.Flank) ? FlankSeek(node, val) : ChaseSeek(val));
		Vector2 val3 = Vector2.Zero;
		float num = 10000f;
		for (int i = 0; i < count; i++)
		{
			if (index != i)
			{
				Vector2 val4 = globalPosition - ((Control)_monsters[i]).GlobalPosition;
				float num2 = ((Vector2)(ref val4)).LengthSquared();
				if (!(num2 < 0.01f) && !(num2 > num))
				{
					val3 += ((Vector2)(ref val4)).Normalized() * (100f / Mathf.Sqrt(num2));
				}
			}
		}
		if (val3 != Vector2.Zero)
		{
			val3 = ((Vector2)(ref val3)).Normalized();
		}
		Vector2 val5 = val2 * 1f + val3 * 1.6f;
		if (kind == EnemyBehaviorKind.Chase && Math.Abs(x) < 40f && val2 != Vector2.Zero)
		{
			((Vector2)(ref val5))..ctor(val5.X * 0.35f, val5.Y);
		}
		float num3 = MoveSpeedUtil.ForCreature(node.Entity, RealtimeCombatSettings.EnemyMoveSpeed);
		Vector2 desiredVel = ((((Vector2)(ref val5)).LengthSquared() < 0.0001f) ? Vector2.Zero : (((Vector2)(ref val5)).Normalized() * num3));
		MoveWithInertia(node, desiredVel, delta);
		CreatureFacing.FaceTowardDeltaX(node, x);
	}

	public static void PushFrom(Creature target, Creature dealer)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		if (target == null || !target.IsAlive)
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(target) : null);
		if (val == null || !GodotObject.IsInstanceValid((GodotObject)(object)val))
		{
			return;
		}
		Vector2 away = Vector2.Right;
		NCreature val2 = ((dealer == null) ? null : ((instance != null) ? instance.GetCreatureNode(dealer) : null));
		if (val2 != null && GodotObject.IsInstanceValid((GodotObject)(object)val2))
		{
			Vector2 val3 = ((Control)val).GlobalPosition - ((Control)val2).GlobalPosition;
			if (((Vector2)(ref val3)).LengthSquared() > 4f)
			{
				away = ((Vector2)(ref val3)).Normalized();
			}
		}
		PushFromDirection(target, away);
	}

	public static void PushFromDirection(Creature target, Vector2 away)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		if (target != null && target.IsAlive)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature val = ((instance != null) ? instance.GetCreatureNode(target) : null);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				away = ((!(((Vector2)(ref away)).LengthSquared() < 0.0001f)) ? ((Vector2)(ref away)).Normalized() : Vector2.Right);
				_knockLeft[((GodotObject)val).GetInstanceId()] = away * 150f;
			}
		}
	}

	private static void ApplyKnockback(NCreature node, float delta)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		ulong instanceId = ((GodotObject)node).GetInstanceId();
		if (!_knockLeft.TryGetValue(instanceId, out var value))
		{
			return;
		}
		float num = ((Vector2)(ref value)).Length();
		if (num < 1f || delta <= 0f)
		{
			_knockLeft.Remove(instanceId);
			return;
		}
		float num2 = Math.Min(num, 1250f * delta);
		((Control)node).GlobalPosition = ((Control)node).GlobalPosition + value / num * num2;
		value -= value / num * num2;
		if (((Vector2)(ref value)).LengthSquared() < 1f)
		{
			_knockLeft.Remove(instanceId);
		}
		else
		{
			_knockLeft[instanceId] = value;
		}
	}

	private static void MoveWithInertia(NCreature node, Vector2 desiredVel, float delta)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		ulong instanceId = ((GodotObject)node).GetInstanceId();
		_inertia.TryGetValue(instanceId, out var value);
		float num = ((delta <= 0f) ? 1f : (1f - MathF.Exp((0f - delta) / 0.2f)));
		value = ((Vector2)(ref value)).Lerp(desiredVel, Mathf.Clamp(num, 0f, 1f));
		if (((Vector2)(ref value)).LengthSquared() < 1f && ((Vector2)(ref desiredVel)).LengthSquared() < 1f)
		{
			_inertia.Remove(instanceId);
			return;
		}
		_inertia[instanceId] = value;
		((Control)node).GlobalPosition = ((Control)node).GlobalPosition + value * delta;
	}

	private static Vector2 ChaseSeek(Vector2 toPlayer)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (((Vector2)(ref toPlayer)).LengthSquared() <= 2304f)
		{
			return Vector2.Zero;
		}
		return ((Vector2)(ref toPlayer)).Normalized();
	}

	private static Vector2 FlankSeek(NCreature node, Vector2 toPlayer)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		float num = ((Vector2)(ref toPlayer)).Length();
		if (num < 1f)
		{
			return Vector2.Zero;
		}
		Vector2 val = toPlayer / num;
		Vector2 val2 = default(Vector2);
		((Vector2)(ref val2))..ctor(0f - val.Y, val.X);
		if ((((GodotObject)node).GetInstanceId() & 1) == 0)
		{
			val2 = -val2;
		}
		if (num > 260f)
		{
			Vector2 val3 = val * 0.45f + val2;
			return ((Vector2)(ref val3)).Normalized();
		}
		if (num > 110f)
		{
			return val2;
		}
		return val;
	}

	private static void TickCharge(NCreature node, Creature target, Vector2 targetPos, float delta, NCombatRoom room, bool predictive, bool targetIsLocalOwner)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Unknown result type (might be due to invalid IL or missing references)
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_031c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0209: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0193: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_029c: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		ulong instanceId = ((GodotObject)node).GetInstanceId();
		if (!_charges.TryGetValue(instanceId, out ChargeState value))
		{
			value = new ChargeState();
			_charges[instanceId] = value;
		}
		Vector2 globalPosition = ((Control)node).GlobalPosition;
		bool isPet = target.IsPet;
		switch (value.Phase)
		{
		case ChargePhase.Approach:
		{
			Vector2 val = AimPoint(targetPos, isPet, predictive);
			Vector2 val2 = val - globalPosition;
			if (((Vector2)(ref val2)).LengthSquared() <= 90000f)
			{
				value.Phase = ChargePhase.Windup;
				value.Timer = 0.65f;
				value.Hit = false;
				Vector2 val3 = AimPoint(targetPos, isPet, predictive);
				Vector2 val4 = val3 - globalPosition;
				value.LockedAim = val3;
				value.Dir = ((((Vector2)(ref val4)).LengthSquared() > 1f) ? ((Vector2)(ref val4)).Normalized() : Vector2.Right);
				EnsureLine(value, (Node)(object)room);
				UpdateLine(node, value, locked: false);
			}
			else if (((Vector2)(ref val2)).LengthSquared() > 4f)
			{
				float num = MoveSpeedUtil.ForCreature(node.Entity, RealtimeCombatSettings.EnemyMoveSpeed);
				MoveWithInertia(node, ((Vector2)(ref val2)).Normalized() * num, delta);
				CreatureFacing.FaceTowardDeltaX(node, val2.X);
			}
			break;
		}
		case ChargePhase.Windup:
			value.Timer -= delta;
			if (predictive)
			{
				if (value.Timer > 0.325f)
				{
					Vector2 val5 = AimPoint(targetPos, isPet, predictive: true);
					Vector2 val6 = val5 - ((Control)node).GlobalPosition;
					if (((Vector2)(ref val6)).LengthSquared() > 1f)
					{
						value.LockedAim = val5;
						value.Dir = ((Vector2)(ref val6)).Normalized();
					}
					CreatureFacing.FaceTowardDeltaX(node, value.Dir.X);
					UpdateLine(node, value, locked: false);
				}
				else
				{
					CreatureFacing.FaceTowardDeltaX(node, value.Dir.X);
					UpdateLine(node, value, locked: true);
				}
			}
			else
			{
				Vector2 val7 = targetPos - ((Control)node).GlobalPosition;
				if (((Vector2)(ref val7)).LengthSquared() > 1f)
				{
					value.Dir = ((Vector2)(ref val7)).Normalized();
				}
				CreatureFacing.FaceTowardDeltaX(node, value.Dir.X);
				UpdateLine(node, value, locked: false);
			}
			if (!(value.Timer <= 0f))
			{
				break;
			}
			if (!predictive)
			{
				Vector2 val8 = targetPos - ((Control)node).GlobalPosition;
				if (((Vector2)(ref val8)).LengthSquared() > 1f)
				{
					value.Dir = ((Vector2)(ref val8)).Normalized();
				}
			}
			value.Phase = ChargePhase.Dash;
			value.Timer = 0.912f;
			value.Hit = false;
			_inertia.Remove(instanceId);
			UpdateLine(node, value, locked: true);
			break;
		case ChargePhase.Dash:
		{
			value.Timer -= delta;
			float num2 = MoveSpeedUtil.ForCreature(node.Entity, RealtimeCombatSettings.EnemyMoveSpeed) * 3.6f;
			((Control)node).GlobalPosition = ((Control)node).GlobalPosition + value.Dir * num2 * delta;
			CreatureFacing.FaceTowardDeltaX(node, value.Dir.X);
			UpdateLine(node, value, locked: true);
			if (!value.Hit && !value.DamageBusy)
			{
				Vector2 globalPosition2 = ((Control)node).GlobalPosition;
				if (((Vector2)(ref globalPosition2)).DistanceTo(targetPos) <= 72f)
				{
					if (targetIsLocalOwner)
					{
						Creature entity = node.Entity;
						if (entity != null && EnemyMeleeAttackSystem.TryAcceptHit(target))
						{
							value.Hit = true;
							value.DamageBusy = true;
							ResolveChargeHit(entity, target, value);
						}
					}
					else
					{
						value.Hit = true;
					}
				}
			}
			if (value.Timer <= 0f)
			{
				value.Phase = ChargePhase.Recover;
				value.Timer = 0.55f;
				FreeLine(value);
			}
			break;
		}
		case ChargePhase.Recover:
			value.Timer -= delta;
			if (value.Timer <= 0f)
			{
				value.Phase = ChargePhase.Approach;
			}
			break;
		}
	}

	private static Vector2 AimPoint(Vector2 targetPos, bool targetIsOsty, bool predictive)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (!predictive)
		{
			return targetPos;
		}
		Vector2 val = (targetIsOsty ? OstyMoveSystem.Velocity : PlayerWasdMoveSystem.Velocity);
		return targetPos + val * 0.4f;
	}

	private static async Task ResolveChargeHit(Creature dealer, Creature target, ChargeState st)
	{
		try
		{
			await MonsterContactMove.ResolveHit(dealer, target);
		}
		finally
		{
			st.DamageBusy = false;
		}
	}

	private static void EnsureLine(ChargeState st, Node parent)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		if (st.Line == null || !GodotObject.IsInstanceValid((GodotObject)(object)st.Line))
		{
			st.Line = new ColorRect
			{
				Name = StringName.op_Implicit("ActionGameChargeLine"),
				MouseFilter = (MouseFilterEnum)2,
				ZIndex = 4096,
				Color = new Color(1f, 0.55f, 0.1f, 0.55f)
			};
			((CanvasItem)st.Line).ZAsRelative = false;
			parent.AddChild((Node)(object)st.Line, false, (InternalMode)0);
		}
	}

	private static void UpdateLine(NCreature owner, ChargeState st, bool locked)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		if (st.Line != null && GodotObject.IsInstanceValid((GodotObject)(object)st.Line) && !(((Vector2)(ref st.Dir)).LengthSquared() < 0.01f))
		{
			Vector2 globalPosition = ((Control)owner).GlobalPosition;
			((Control)st.Line).GlobalPosition = globalPosition;
			((Control)st.Line).PivotOffset = Vector2.Zero;
			((Control)st.Line).Size = new Vector2(locked ? 176f : 128f, 8f);
			((Control)st.Line).Rotation = ((Vector2)(ref st.Dir)).Angle();
			((CanvasItem)st.Line).Visible = true;
			st.Line.Color = (locked ? new Color(1f, 0.2f, 0.15f, 0.7f) : new Color(1f, 0.7f, 0.2f, 0.45f));
		}
	}

	private static void FreeLine(ChargeState st)
	{
		if (st.Line != null && GodotObject.IsInstanceValid((GodotObject)(object)st.Line))
		{
			((Node)st.Line).QueueFree();
		}
		st.Line = null;
	}

	private static void ClearCharges()
	{
		foreach (ChargeState value in _charges.Values)
		{
			FreeLine(value);
		}
		_charges.Clear();
	}
}
internal static class EnemyMeleeAttackSystem
{
	private sealed class State
	{
		public ColorRect? Box;
	}

	private const float InvulnSec = 2f;

	private static bool _hooked;

	private static float _playerInvuln;

	private static float _ostyInvuln;

	private static readonly Dictionary<ulong, State> States = new Dictionary<ulong, State>();

	private static readonly List<ulong> _deadKeys = new List<ulong>();

	private static ColorRect? _playerBox;

	private static ColorRect? _ostyBox;

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
			}
		}
	}

	public static bool TryAcceptPlayerHit()
	{
		if (_playerInvuln > 0f)
		{
			return false;
		}
		_playerInvuln = 2f;
		return true;
	}

	public static bool TryAcceptOstyHit()
	{
		if (_ostyInvuln > 0f)
		{
			return false;
		}
		_ostyInvuln = 2f;
		if (OstyCombatUtil.RollDodge())
		{
			return false;
		}
		return true;
	}

	public static bool TryAcceptHit(Creature target)
	{
		if (target == null)
		{
			return false;
		}
		return target.IsPet ? TryAcceptOstyHit() : TryAcceptPlayerHit();
	}

	private static void OnProcessFrame()
	{
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_024c: Unknown result type (might be due to invalid IL or missing references)
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsOverOrEnding)
		{
			ClearAll();
			FreePlayerBox();
			FreeOstyBox();
			_playerInvuln = 0f;
			_ostyInvuln = 0f;
		}
		else
		{
			if (PlayerChoicePause.IsPaused)
			{
				return;
			}
			NCombatRoom instance2 = NCombatRoom.Instance;
			if (instance2 == null)
			{
				return;
			}
			CombatState val = instance.DebugOnlyGetState();
			if (val == null)
			{
				return;
			}
			Player me = LocalContext.GetMe((ICombatState)(object)val);
			Creature val2 = ((me != null) ? me.Creature : null);
			if (val2 == null || !val2.IsAlive)
			{
				return;
			}
			NCreature creatureNode = instance2.GetCreatureNode(val2);
			if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				return;
			}
			float num = (float)((Node)creatureNode).GetProcessDeltaTime();
			if (num <= 0f)
			{
				num = 1f / 60f;
			}
			if (_playerInvuln > 0f)
			{
				_playerInvuln -= num;
			}
			if (_ostyInvuln > 0f)
			{
				_ostyInvuln -= num;
			}
			Rect2 val3 = OstyCombatUtil.BodyRect(creatureNode);
			ShowPlayerBox((Node)(object)instance2, val3);
			Creature osty = null;
			Rect2 val4 = default(Rect2);
			NCreature node;
			bool flag = OstyCombatUtil.TryGetAliveOsty(me, out osty, out node);
			if (flag)
			{
				val4 = OstyCombatUtil.OstyBodyRect(node);
				ShowOstyBox((Node)(object)instance2, val4);
			}
			else
			{
				FreeOstyBox();
			}
			HashSet<ulong> hashSet = new HashSet<ulong>();
			foreach (NCreature creatureNode2 in instance2.CreatureNodes)
			{
				if (creatureNode2 == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode2))
				{
					continue;
				}
				Creature entity = creatureNode2.Entity;
				if (OstyCombatUtil.IsHostileMonster(entity) && !EnemyBehaviorCatalog.SkipsMelee(entity))
				{
					ulong instanceId = ((GodotObject)creatureNode2).GetInstanceId();
					hashSet.Add(instanceId);
					if (!States.TryGetValue(instanceId, out State value))
					{
						value = new State();
						States[instanceId] = value;
					}
					TickOne(creatureNode2, entity, val2, val3, flag, osty, val4, value, instance2);
				}
			}
			_deadKeys.Clear();
			foreach (KeyValuePair<ulong, State> state in States)
			{
				if (!hashSet.Contains(state.Key))
				{
					_deadKeys.Add(state.Key);
				}
			}
			foreach (ulong deadKey in _deadKeys)
			{
				if (States.Remove(deadKey, out State value2))
				{
					FreeBox(value2);
				}
			}
		}
	}

	private static void TickOne(NCreature node, Creature enemy, Creature player, Rect2 playerRect, bool hasOsty, Creature? osty, Rect2 ostyRect, State st, NCombatRoom room)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		EnsureBox(st, (Node)(object)room);
		Rect2 val = OstyCombatUtil.BodyRect(node);
		if (st.Box != null && GodotObject.IsInstanceValid((GodotObject)(object)st.Box))
		{
			((Control)st.Box).GlobalPosition = ((Rect2)(ref val)).Position;
			((Control)st.Box).Size = ((Rect2)(ref val)).Size;
			((CanvasItem)st.Box).Visible = RealtimeCombatSettings.ShowAttackBoxes;
		}
		if (((Rect2)(ref val)).Intersects(playerRect, false) && TryAcceptPlayerHit())
		{
			MonsterContactMove.ResolveHit(enemy, player);
		}
		if (hasOsty && osty != null && ((Rect2)(ref val)).Intersects(ostyRect, false) && TryAcceptOstyHit())
		{
			MonsterContactMove.ResolveHit(enemy, osty);
		}
	}

	private static void EnsureBox(State st, Node parent)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		if (st.Box == null || !GodotObject.IsInstanceValid((GodotObject)(object)st.Box))
		{
			st.Box = new ColorRect
			{
				Name = StringName.op_Implicit("ActionGameMeleeHitbox"),
				MouseFilter = (MouseFilterEnum)2,
				ZIndex = 80,
				Color = new Color(1f, 0.15f, 0.15f, 0.45f)
			};
			((CanvasItem)st.Box).ZAsRelative = false;
			parent.AddChild((Node)(object)st.Box, false, (InternalMode)0);
		}
	}

	private static void FreeBox(State st)
	{
		if (st.Box != null && GodotObject.IsInstanceValid((GodotObject)(object)st.Box))
		{
			((Node)st.Box).QueueFree();
		}
		st.Box = null;
	}

	private static void ShowPlayerBox(Node parent, Rect2 rect)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Expected O, but got Unknown
		if (_playerBox == null || !GodotObject.IsInstanceValid((GodotObject)(object)_playerBox))
		{
			_playerBox = new ColorRect
			{
				Name = StringName.op_Implicit("ActionGamePlayerHitbox"),
				MouseFilter = (MouseFilterEnum)2,
				ZIndex = 4096,
				Color = new Color(0.2f, 0.85f, 1f, 0.35f)
			};
			((CanvasItem)_playerBox).ZAsRelative = false;
			parent.AddChild((Node)(object)_playerBox, false, (InternalMode)0);
		}
		((Control)_playerBox).GlobalPosition = ((Rect2)(ref rect)).Position;
		((Control)_playerBox).Size = ((Rect2)(ref rect)).Size;
		((CanvasItem)_playerBox).Visible = RealtimeCombatSettings.ShowAttackBoxes;
	}

	private static void FreePlayerBox()
	{
		if (_playerBox != null && GodotObject.IsInstanceValid((GodotObject)(object)_playerBox))
		{
			((Node)_playerBox).QueueFree();
		}
		_playerBox = null;
	}

	private static void ShowOstyBox(Node parent, Rect2 rect)
	{
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Expected O, but got Unknown
		if (_ostyBox == null || !GodotObject.IsInstanceValid((GodotObject)(object)_ostyBox))
		{
			_ostyBox = new ColorRect
			{
				Name = StringName.op_Implicit("ActionGameOstyHitbox"),
				MouseFilter = (MouseFilterEnum)2,
				ZIndex = 4096,
				Color = new Color(0.35f, 1f, 0.45f, 0.4f)
			};
			((CanvasItem)_ostyBox).ZAsRelative = false;
			parent.AddChild((Node)(object)_ostyBox, false, (InternalMode)0);
		}
		((Control)_ostyBox).GlobalPosition = ((Rect2)(ref rect)).Position;
		((Control)_ostyBox).Size = ((Rect2)(ref rect)).Size;
		((CanvasItem)_ostyBox).Visible = RealtimeCombatSettings.ShowAttackBoxes;
	}

	private static void FreeOstyBox()
	{
		if (_ostyBox != null && GodotObject.IsInstanceValid((GodotObject)(object)_ostyBox))
		{
			((Node)_ostyBox).QueueFree();
		}
		_ostyBox = null;
	}

	private static void ClearAll()
	{
		foreach (State value in States.Values)
		{
			FreeBox(value);
		}
		States.Clear();
	}
}
internal static class EscPauseHotkey
{
	private static bool _hooked;

	private static bool _escWasDown;

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
			}
		}
	}

	private static void OnProcessFrame()
	{
		bool flag = Input.IsPhysicalKeyPressed((Key)4194305) || Input.IsKeyPressed((Key)4194305);
		if (flag && !_escWasDown)
		{
			TogglePause();
		}
		_escWasDown = flag;
	}

	private static void TogglePause()
	{
		if (IsPauseOpen())
		{
			NCapstoneContainer instance = NCapstoneContainer.Instance;
			if (instance != null)
			{
				instance.Close();
			}
			Log.Info("[local.action_game] ESC close pause", 2);
			return;
		}
		NRun instance2 = NRun.Instance;
		object obj;
		if (instance2 == null)
		{
			obj = null;
		}
		else
		{
			NGlobalUi globalUi = instance2.GlobalUi;
			obj = ((globalUi != null) ? globalUi.SubmenuStack : null);
		}
		NCapstoneSubmenuStack val = (NCapstoneSubmenuStack)obj;
		if (val == null)
		{
			return;
		}
		RunManager instance3 = RunManager.Instance;
		RunState val2 = ((instance3 != null) ? instance3.DebugOnlyGetState() : null);
		if (val2 != null)
		{
			NSubmenu obj2 = val.ShowScreen((CapstoneSubmenuType)4);
			NPauseMenu val3 = (NPauseMenu)(object)((obj2 is NPauseMenu) ? obj2 : null);
			if (val3 != null)
			{
				val3.Initialize((IRunState)(object)val2);
			}
			Log.Info("[local.action_game] ESC open pause", 2);
		}
	}

	private static bool IsPauseOpen()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Invalid comparison between Unknown and I4
		NCapstoneContainer instance = NCapstoneContainer.Instance;
		ICapstoneScreen obj = ((instance != null) ? instance.CurrentCapstoneScreen : null);
		NCapstoneSubmenuStack val = (NCapstoneSubmenuStack)(object)((obj is NCapstoneSubmenuStack) ? obj : null);
		return val != null && (int)val.ScreenType == 10;
	}
}
internal static class FiddleRewrite
{
	public const string Description = "每回合多抽 2 张牌。";
}
[HarmonyPatch(typeof(Fiddle), "ShouldDraw")]
internal static class FiddleAllowDrawPatch
{
	private static void Postfix(ref bool __result)
	{
		__result = true;
	}
}
[HarmonyPatch(typeof(LocString), "GetFormattedText")]
internal static class FiddleDescriptionPatch
{
	private static void Postfix(LocString __instance, ref string __result)
	{
		if (!(__instance.LocTable != "relics") && (__instance.LocEntryKey == "FIDDLE.description" || __instance.LocEntryKey == "FIDDLE.eventDescription"))
		{
			__result = "每回合多抽 2 张牌。";
		}
	}
}
[HarmonyPatch]
public static class FloorBackgroundReplace
{
	private const string OverlayName = "ModFloorOverlay";

	private const float BaseHalfW = 1382.4f;

	private const float BaseHalfH = 648f;

	private const int UnderSceneZIndex = -11;

	private static string? _modDir;

	private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

	private static TextureRect? _activeOverlay;

	private static bool _inputHooked;

	private static bool _settingsHooked;

	private static float _saveDelay = -1f;

	private static bool _pgUpHeld;

	private static bool _pgDownHeld;

	private static string ModDir
	{
		get
		{
			if (_modDir != null)
			{
				return _modDir;
			}
			_modDir = Path.GetDirectoryName(typeof(ModEntry).Assembly.Location) ?? ".";
			return _modDir;
		}
	}

	public static void EnsureInputHook()
	{
		if (_inputHooked)
		{
			return;
		}
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		if (val != null)
		{
			val.ProcessFrame += OnProcessFrame;
			_inputHooked = true;
			if (!_settingsHooked)
			{
				RealtimeCombatSettings.Changed += ApplyActiveLayout;
				_settingsHooked = true;
			}
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "SetUpBackground")]
	[HarmonyPostfix]
	private static void AfterCombatBackground(NCombatRoom __instance)
	{
		EnsureInputHook();
		if (!RealtimeCombatSettings.FloorOverlayEnabled)
		{
			ClearActive();
			return;
		}
		if (ShouldKeepVanillaSceneBackground())
		{
			ClearActive();
			ShowVanillaBackground(__instance);
			Log.Info("[local.action_game] floor overlay skipped (scene boss background)", 2);
			return;
		}
		string text = ResolveBackgroundKey(__instance);
		if (string.IsNullOrEmpty(text))
		{
			Log.Warn("[local.action_game] floor overlay skipped: no bg key", 2);
			return;
		}
		Texture2D val = LoadTexture(text + "_00.png");
		if (val == null)
		{
			Log.Warn($"[{"local.action_game"}] floor overlay skipped: missing backgrounds/{text}_00.png", 2);
			return;
		}
		EnsureOverlay(__instance, val);
		Log.Info($"[{"local.action_game"}] floor overlay ({text}) parent=room scale={RealtimeCombatSettings.FloorOverlayScaleX:0.##}x{RealtimeCombatSettings.FloorOverlayScaleY:0.##} offset=({RealtimeCombatSettings.FloorOverlayOffsetX:0},{RealtimeCombatSettings.FloorOverlayOffsetY:0}) z={RealtimeCombatSettings.FloorOverlayZIndex} | " + "Ctrl+Alt+方向键移动 =/-缩放 [/]高度 PgUp/PgDn层级", 2);
	}

	private static void OnProcessFrame()
	{
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		float num = ((val != null) ? ((float)((Node)val.Root).GetProcessDeltaTime()) : 0.016f);
		if (num <= 0f)
		{
			num = 0.016f;
		}
		if (_saveDelay > 0f)
		{
			_saveDelay -= num;
			if (_saveDelay <= 0f)
			{
				RealtimeCombatSettings.Save();
				Log.Info($"[{"local.action_game"}] floor overlay saved scale={RealtimeCombatSettings.FloorOverlayScaleX:0.##}x{RealtimeCombatSettings.FloorOverlayScaleY:0.##} offset=({RealtimeCombatSettings.FloorOverlayOffsetX:0},{RealtimeCombatSettings.FloorOverlayOffsetY:0}) z={RealtimeCombatSettings.FloorOverlayZIndex}", 2);
			}
		}
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsOverOrEnding)
		{
			_pgUpHeld = false;
			_pgDownHeld = false;
		}
		else
		{
			if (_activeOverlay == null || !GodotObject.IsInstanceValid((GodotObject)(object)_activeOverlay))
			{
				return;
			}
			if (!Input.IsPhysicalKeyPressed((Key)4194326) || !Input.IsPhysicalKeyPressed((Key)4194328))
			{
				_pgUpHeld = false;
				_pgDownHeld = false;
				return;
			}
			float num2 = RealtimeCombatSettings.FloorOverlayScaleX;
			float num3 = RealtimeCombatSettings.FloorOverlayScaleY;
			float num4 = RealtimeCombatSettings.FloorOverlayOffsetX;
			float num5 = RealtimeCombatSettings.FloorOverlayOffsetY;
			int num6 = RealtimeCombatSettings.FloorOverlayZIndex;
			bool flag = false;
			float num7 = 420f * num;
			if (Input.IsPhysicalKeyPressed((Key)4194319))
			{
				num4 -= num7;
				flag = true;
			}
			if (Input.IsPhysicalKeyPressed((Key)4194321))
			{
				num4 += num7;
				flag = true;
			}
			if (Input.IsPhysicalKeyPressed((Key)4194320))
			{
				num5 -= num7;
				flag = true;
			}
			if (Input.IsPhysicalKeyPressed((Key)4194322))
			{
				num5 += num7;
				flag = true;
			}
			float num8 = 0.55f * num;
			if (Input.IsPhysicalKeyPressed((Key)61) || Input.IsPhysicalKeyPressed((Key)4194437))
			{
				num2 += num8;
				num3 += num8;
				flag = true;
			}
			if (Input.IsPhysicalKeyPressed((Key)45) || Input.IsPhysicalKeyPressed((Key)4194435))
			{
				num2 -= num8;
				num3 -= num8;
				flag = true;
			}
			if (Input.IsPhysicalKeyPressed((Key)93))
			{
				num3 += num8;
				flag = true;
			}
			if (Input.IsPhysicalKeyPressed((Key)91))
			{
				num3 -= num8;
				flag = true;
			}
			bool flag2 = Input.IsPhysicalKeyPressed((Key)4194323);
			bool flag3 = Input.IsPhysicalKeyPressed((Key)4194324);
			if (flag2 && !_pgUpHeld)
			{
				num6++;
				flag = true;
			}
			if (flag3 && !_pgDownHeld)
			{
				num6--;
				flag = true;
			}
			_pgUpHeld = flag2;
			_pgDownHeld = flag3;
			if (flag)
			{
				RealtimeCombatSettings.SetFloorOverlayLayout(num2, num3, num4, num5, num6, save: false);
				ApplyActiveLayout();
				_saveDelay = 0.45f;
			}
		}
	}

	private static void EnsureOverlay(NCombatRoom room, Texture2D tex)
	{
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Expected O, but got Unknown
		NCombatBackground background = room.Background;
		if (background != null && GodotObject.IsInstanceValid((GodotObject)(object)background))
		{
			((CanvasItem)background).Visible = false;
			foreach (Node child in ((Node)background).GetChildren(false))
			{
				TextureRect val = (TextureRect)(object)((child is TextureRect) ? child : null);
				if (val != null && ((object)((Node)val).Name).ToString() == "ModFloorOverlay")
				{
					((Node)val).QueueFree();
				}
			}
		}
		TextureRect val2 = null;
		foreach (Node child2 in ((Node)room).GetChildren(false))
		{
			TextureRect val3 = (TextureRect)(object)((child2 is TextureRect) ? child2 : null);
			if (val3 != null && ((object)((Node)val3).Name).ToString() == "ModFloorOverlay")
			{
				val2 = val3;
				break;
			}
		}
		if (val2 == null)
		{
			val2 = new TextureRect
			{
				Name = StringName.op_Implicit("ModFloorOverlay"),
				MouseFilter = (MouseFilterEnum)2,
				ExpandMode = (ExpandModeEnum)1,
				StretchMode = (StretchModeEnum)0,
				ZAsRelative = false,
				ZIndex = -11
			};
			((Node)room).AddChild((Node)(object)val2, false, (InternalMode)0);
			((Node)room).MoveChild((Node)(object)val2, 0);
		}
		val2.Texture = tex;
		((CanvasItem)val2).Visible = RealtimeCombatSettings.FloorOverlayEnabled;
		_activeOverlay = val2;
		ApplyActiveLayout();
	}

	private static void ApplyActiveLayout()
	{
		if (_activeOverlay != null && GodotObject.IsInstanceValid((GodotObject)(object)_activeOverlay))
		{
			((CanvasItem)_activeOverlay).Visible = RealtimeCombatSettings.FloorOverlayEnabled;
			if (((CanvasItem)_activeOverlay).Visible)
			{
				float floorOverlayScaleX = RealtimeCombatSettings.FloorOverlayScaleX;
				float floorOverlayScaleY = RealtimeCombatSettings.FloorOverlayScaleY;
				float floorOverlayOffsetX = RealtimeCombatSettings.FloorOverlayOffsetX;
				float floorOverlayOffsetY = RealtimeCombatSettings.FloorOverlayOffsetY;
				float num = 1382.4f * floorOverlayScaleX;
				float num2 = 648f * floorOverlayScaleY;
				((Control)_activeOverlay).AnchorLeft = 0.5f;
				((Control)_activeOverlay).AnchorTop = 0.5f;
				((Control)_activeOverlay).AnchorRight = 0.5f;
				((Control)_activeOverlay).AnchorBottom = 0.5f;
				((Control)_activeOverlay).OffsetLeft = 0f - num + floorOverlayOffsetX;
				((Control)_activeOverlay).OffsetRight = num + floorOverlayOffsetX;
				((Control)_activeOverlay).OffsetTop = 0f - num2 + floorOverlayOffsetY;
				((Control)_activeOverlay).OffsetBottom = num2 + floorOverlayOffsetY;
				((Control)_activeOverlay).GrowHorizontal = (GrowDirection)2;
				((Control)_activeOverlay).GrowVertical = (GrowDirection)2;
				((CanvasItem)_activeOverlay).ZAsRelative = false;
				((CanvasItem)_activeOverlay).ZIndex = RealtimeCombatSettings.FloorOverlayZIndex;
			}
		}
	}

	private static bool ShouldKeepVanillaSceneBackground()
	{
		CombatManager instance = CombatManager.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			CombatState obj2 = instance.DebugOnlyGetState();
			obj = ((obj2 != null) ? obj2.Encounter : null);
		}
		EncounterModel val = (EncounterModel)obj;
		if (val == null)
		{
			return false;
		}
		if (val is KaiserCrabBoss)
		{
			return true;
		}
		return val.HasScene;
	}

	private static void ShowVanillaBackground(NCombatRoom room)
	{
		NCombatBackground background = room.Background;
		if (background != null && GodotObject.IsInstanceValid((GodotObject)(object)background))
		{
			((CanvasItem)background).Visible = true;
			CanvasItem nodeOrNull = ((Node)background).GetNodeOrNull<CanvasItem>(NodePath.op_Implicit("%KaiserCrab"));
			if (nodeOrNull != null)
			{
				nodeOrNull.Visible = true;
			}
		}
	}

	private static void ClearActive()
	{
		if (_activeOverlay != null && GodotObject.IsInstanceValid((GodotObject)(object)_activeOverlay))
		{
			((CanvasItem)_activeOverlay).Visible = false;
		}
		_activeOverlay = null;
	}

	private static string? ResolveBackgroundKey(NCombatRoom room)
	{
		object value = Traverse.Create((object)room).Field("_visuals").GetValue();
		if (value != null)
		{
			object value2 = Traverse.Create(value).Property("Encounter", (object[])null).GetValue();
			EncounterModel val = (EncounterModel)((value2 is EncounterModel) ? value2 : null);
			if (val != null)
			{
				string text = ((AbstractModel)val).Id.Entry.ToLowerInvariant();
				if (TextureExists(text + "_00.png"))
				{
					return text;
				}
			}
			object value3 = Traverse.Create(value).Property("Act", (object[])null).GetValue();
			ActModel act = (ActModel)((value3 is ActModel) ? value3 : null);
			string text2 = ActId(act);
			if (!string.IsNullOrEmpty(text2) && TextureExists(text2 + "_00.png"))
			{
				return text2;
			}
			if (val != null)
			{
				string text3 = ((AbstractModel)val).Id.Entry.ToLowerInvariant();
				if (!string.IsNullOrEmpty(text3))
				{
					return text3;
				}
			}
			if (!string.IsNullOrEmpty(text2))
			{
				return text2;
			}
		}
		RunManager instance = RunManager.Instance;
		RunState val2 = ((instance != null) ? instance.DebugOnlyGetState() : null);
		return ActId((val2 != null) ? val2.Act : null);
	}

	private static bool TextureExists(string fileName)
	{
		return File.Exists(Path.Combine(ModDir, "backgrounds", fileName));
	}

	private static string? ActId(ActModel? act)
	{
		if (act == null)
		{
			return null;
		}
		return ((AbstractModel)act).Id.Entry.ToLowerInvariant();
	}

	private static Texture2D? LoadTexture(string fileName)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Invalid comparison between Unknown and I8
		if (Cache.TryGetValue(fileName, out Texture2D value) && GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			return value;
		}
		string text = Path.Combine(ModDir, "backgrounds", fileName);
		if (!File.Exists(text))
		{
			return null;
		}
		Image val = new Image();
		if ((long)val.Load(text) > 0L)
		{
			Log.Warn("[local.action_game] floor bg failed to load " + text, 2);
			return null;
		}
		ImageTexture val2 = ImageTexture.CreateFromImage(val);
		Cache[fileName] = (Texture2D)(object)val2;
		return (Texture2D?)(object)val2;
	}
}
internal enum GoldDropTier
{
	Weak,
	Strong,
	Elite,
	Boss
}
internal static class GoldDropSystem
{
	private sealed class Coin
	{
		public Node2D Node = null;

		public Vector2 Pos;

		public Vector2 Velocity;

		public float Age;

		public bool Settled;
	}

	private static readonly string[] IconPaths = new string[3] { "res://images/atlases/ui_atlas.sprites/top_bar/top_bar_gold.tres", "res://images/ui/reward_screen/reward_icon_money.png", "res://images/events/crystal_sphere/crystal_sphere_gold.png" };

	private const float CoinSize = 36f;

	private const float PickupRadius = 90f;

	private const float BurstDuration = 0.42f;

	private const float BurstSpeedMin = 220f;

	private const float BurstSpeedMax = 480f;

	private const float BurstGravity = 920f;

	private const float BurstDrag = 3.2f;

	private static bool _hooked;

	private static bool _texTried;

	private static readonly List<Texture2D> _textures = new List<Texture2D>();

	private static readonly List<Coin> Coins = new List<Coin>();

	private static readonly Dictionary<Creature, Vector2> LastFeet = new Dictionary<Creature, Vector2>();

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
			}
		}
	}

	private static void OnProcessFrame()
	{
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsOverOrEnding)
		{
			ClearAll();
			LastFeet.Clear();
			return;
		}
		NCombatRoom instance2 = NCombatRoom.Instance;
		float num = ((instance2 != null && GodotObject.IsInstanceValid((GodotObject)(object)instance2)) ? ((float)((Node)instance2).GetProcessDeltaTime()) : (1f / 60f));
		if (num <= 0f)
		{
			num = 1f / 60f;
		}
		TickBurst(num);
		WatchDeaths(instance);
		TryPickup(instance);
	}

	private static void TickBurst(float delta)
	{
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		foreach (Coin coin in Coins)
		{
			if (!coin.Settled && coin.Node != null && GodotObject.IsInstanceValid((GodotObject)(object)coin.Node))
			{
				coin.Age += delta;
				coin.Velocity.Y += 920f * delta;
				coin.Velocity *= Math.Max(0f, 1f - 3.2f * delta);
				coin.Pos += coin.Velocity * delta;
				coin.Node.GlobalPosition = coin.Pos;
				if (coin.Age >= 0.42f || ((Vector2)(ref coin.Velocity)).Length() < 28f)
				{
					coin.Velocity = Vector2.Zero;
					coin.Settled = true;
				}
			}
		}
	}

	private static void WatchDeaths(CombatManager manager)
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		CombatState val = manager.DebugOnlyGetState();
		NCombatRoom instance = NCombatRoom.Instance;
		if (val == null || instance == null)
		{
			return;
		}
		HashSet<Creature> hashSet = new HashSet<Creature>();
		foreach (Creature enemy in val.Enemies)
		{
			if (enemy != null && enemy.IsMonster && enemy.IsAlive)
			{
				hashSet.Add(enemy);
				NCreature creatureNode = instance.GetCreatureNode(enemy);
				if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
				{
					LastFeet[enemy] = ((Control)creatureNode).GlobalPosition;
				}
			}
		}
		List<Creature> list = null;
		foreach (KeyValuePair<Creature, Vector2> lastFoot in LastFeet)
		{
			if (!hashSet.Contains(lastFoot.Key))
			{
				if (list == null)
				{
					list = new List<Creature>();
				}
				list.Add(lastFoot.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (Creature item in list)
		{
			Vector2 val2 = LastFeet[item];
			LastFeet.Remove(item);
			GoldDropTier goldDropTier = Classify(item);
			int num = IconCount(goldDropTier);
			Drop(val2, num);
			Log.Info($"[{"local.action_game"}] gold drop tier={goldDropTier} icons={num} at {val2}", 2);
		}
	}

	private static void TryPickup(CombatManager manager)
	{
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		if (Coins.Count == 0)
		{
			return;
		}
		CombatState val = manager.DebugOnlyGetState();
		Player val2 = ((val != null) ? LocalContext.GetMe((ICombatState)(object)val) : null);
		object obj;
		if (((val2 != null) ? val2.Creature : null) == null)
		{
			obj = null;
		}
		else
		{
			NCombatRoom instance = NCombatRoom.Instance;
			obj = ((instance != null) ? instance.GetCreatureNode(val2.Creature) : null);
		}
		NCreature val3 = (NCreature)obj;
		if (val2 == null || val3 == null || !GodotObject.IsInstanceValid((GodotObject)(object)val3))
		{
			return;
		}
		Vector2 feet = ((Control)val3).GlobalPosition;
		List<Coin> list = Coins.Where((Coin c) => c.Settled && c.Node != null && GodotObject.IsInstanceValid((GodotObject)(object)c.Node) && ((Vector2)(ref c.Pos)).DistanceTo(feet) <= 90f).ToList();
		if (list.Count == 0)
		{
			return;
		}
		int num = 0;
		foreach (Coin item in list)
		{
			Coins.Remove(item);
			if (GodotObject.IsInstanceValid((GodotObject)(object)item.Node))
			{
				((Node)item.Node).QueueFree();
			}
			num++;
		}
		PlayerCmd.GainGold((decimal)num, val2, false);
		Log.Info($"[{"local.action_game"}] picked gold +{num}", 2);
	}

	private static void Drop(Vector2 feet, int count)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null && GodotObject.IsInstanceValid((GodotObject)(object)instance) && count > 0)
		{
			Vector2 val = feet + new Vector2(0f, -18f);
			for (int i = 0; i < count; i++)
			{
				float num = GD.Randf() * ((float)Math.PI * 2f);
				float num2 = 220f + GD.Randf() * 260f;
				Vector2 velocity = new Vector2(Mathf.Cos(num), Mathf.Sin(num) * 0.85f - 0.2f) * num2;
				Node2D val2 = MakeCoin();
				((Node)instance).AddChild((Node)(object)val2, false, (InternalMode)0);
				((CanvasItem)val2).ZAsRelative = false;
				((CanvasItem)val2).ZIndex = 2;
				val2.GlobalPosition = val;
				Coins.Add(new Coin
				{
					Node = val2,
					Pos = val,
					Velocity = velocity,
					Age = 0f,
					Settled = false
				});
			}
		}
	}

	internal static void ClearAll()
	{
		foreach (Coin coin in Coins)
		{
			if (coin.Node != null && GodotObject.IsInstanceValid((GodotObject)(object)coin.Node))
			{
				((Node)coin.Node).QueueFree();
			}
		}
		Coins.Clear();
	}

	private static int IconCount(GoldDropTier tier)
	{
		if (1 == 0)
		{
		}
		int result = tier switch
		{
			GoldDropTier.Weak => Random.Shared.Next(4, 7), 
			GoldDropTier.Strong => Random.Shared.Next(8, 13), 
			GoldDropTier.Elite => Random.Shared.Next(15, 21), 
			GoldDropTier.Boss => Random.Shared.Next(20, 31), 
			_ => 4, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private static GoldDropTier Classify(Creature creature)
	{
		MonsterModel monster = creature.Monster;
		if (monster == null)
		{
			return GoldDropTier.Strong;
		}
		if (IsBoss(monster))
		{
			return GoldDropTier.Boss;
		}
		if (IsElite(monster))
		{
			return GoldDropTier.Elite;
		}
		if (SpawnCatalog.IsWeakMonster(monster))
		{
			return GoldDropTier.Weak;
		}
		return GoldDropTier.Strong;
	}

	private static bool IsBoss(MonsterModel monster)
	{
		if (1 == 0)
		{
		}
		bool result = monster is LagavulinMatriarch || monster is SoulFysh || monster is WaterfallGiant || monster is CeremonialBeast || monster is KinFollower || monster is KinPriest || monster is Vantom || monster is Crusher || monster is KnowledgeDemon || monster is Rocket || monster is TheInsatiable || ((monster is Aeonglass || monster is Queen || monster is TestSubject || monster is TorchHeadAmalgam) ? true : false);
		if (1 == 0)
		{
		}
		return result;
	}

	private static bool IsElite(MonsterModel monster)
	{
		if (1 == 0)
		{
		}
		bool result = monster is PhantasmalGardener || monster is SkulkingColony || monster is TerrorEel || monster is BygoneEffigy || monster is Byrdonis || monster is PhrogParasite || monster is Wriggler || monster is DecimillipedeSegmentBack || monster is DecimillipedeSegmentFront || monster is DecimillipedeSegmentMiddle || monster is Entomancer || monster is InfestedPrism || ((monster is FlailKnight || monster is MagiKnight || monster is MechaKnight || monster is SoulNexus || monster is SpectralKnight) ? true : false);
		if (1 == 0)
		{
		}
		return result;
	}

	private static Node2D MakeCoin()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Expected O, but got Unknown
		Node2D val = new Node2D
		{
			Name = StringName.op_Implicit("ActionGameGoldCoin")
		};
		Texture2D val2 = PickCoinTexture();
		if (val2 != null && val2.GetWidth() > 2)
		{
			float num = 36f / (float)Math.Max(val2.GetWidth(), 1);
			((Node)val).AddChild((Node)new Sprite2D
			{
				Texture = val2,
				Centered = true,
				Scale = new Vector2(num, num)
			}, false, (InternalMode)0);
			return val;
		}
		Vector2[] array = (Vector2[])(object)new Vector2[14];
		for (int i = 0; i < array.Length; i++)
		{
			float num2 = (float)i / (float)array.Length * ((float)Math.PI * 2f);
			array[i] = new Vector2(Mathf.Cos(num2), Mathf.Sin(num2)) * 15.12f;
		}
		((Node)val).AddChild((Node)new Polygon2D
		{
			Color = new Color(1f, 0.78f, 0.12f, 1f),
			Polygon = array
		}, false, (InternalMode)0);
		return val;
	}

	private static Texture2D? PickCoinTexture()
	{
		EnsureTextures();
		if (_textures.Count == 0)
		{
			return null;
		}
		return _textures[Random.Shared.Next(_textures.Count)];
	}

	private static void EnsureTextures()
	{
		if (_texTried)
		{
			return;
		}
		_texTried = true;
		_textures.Clear();
		string[] iconPaths = IconPaths;
		foreach (string text in iconPaths)
		{
			Resource val = ResourceLoader.Load(text, "", (CacheMode)1);
			Texture2D val2 = (Texture2D)(object)((val is Texture2D) ? val : null);
			if (val2 == null)
			{
				AtlasTexture val3 = (AtlasTexture)(object)((val is AtlasTexture) ? val : null);
				if (val3 != null)
				{
					val2 = (Texture2D)(object)val3;
				}
			}
			if (val2 != null)
			{
				_textures.Add(val2);
				Log.Info($"[{"local.action_game"}] gold icon ready {text} {val2.GetWidth()}x{val2.GetHeight()}", 2);
			}
			else
			{
				Log.Warn("[local.action_game] gold icon missing " + text, 2);
			}
		}
	}
}
internal static class HostSettingsNetSync
{
	public static bool ClientLocked { get; private set; }

	public static void BroadcastFromHostIfNeeded()
	{
		if (!ActionGameNet.IsHost)
		{
			ClientLocked = ActionGameNet.IsClient;
			return;
		}
		ClientLocked = false;
		ActionGameNet.Send(new ActionGameNetMessage
		{
			Opcode = 4,
			CasterNetId = ActionGameNet.LocalNetId,
			IntA = (RealtimeCombatSettings.AutoPlayCards ? 1 : 0),
			IntB = (int)RealtimeCombatSettings.QuickPlayMode,
			IntD = (RealtimeCombatSettings.AutoSortHand ? 1 : 0),
			Flags = (byte)((RealtimeCombatSettings.ShowAttackBoxes ? 1u : 0u) | (uint)((int)RealtimeCombatSettings.Difficulty << 1) | (RealtimeCombatSettings.SaveEnergyForLeftmost ? 8u : 0u))
		});
		Log.Info("[local.action_game] host settings broadcast", 2);
	}

	public static void OnRemote(ActionGameNetMessage msg)
	{
		if (!ActionGameNet.IsHost)
		{
			ClientLocked = true;
			RealtimeCombatSettings.ApplyHostSnapshot(msg.IntA != 0, (QuickPlayMode)Math.Clamp(msg.IntB, 0, 2), msg.IntD != 0, (msg.Flags & 1) != 0, (GameDifficulty)Math.Clamp((msg.Flags >> 1) & 3, 0, 2), (msg.Flags & 8) != 0);
			TurnLengthHud.RefreshFromHostSync();
			Log.Info("[local.action_game] applied host settings snapshot", 2);
		}
	}

	public static void OnHostLocalSettingsChanged()
	{
		if (ActionGameNet.IsHost && ActionGameNet.IsOnline)
		{
			BroadcastFromHostIfNeeded();
		}
	}
}
internal static class KaiserCrabCombatSystem
{
	private static bool _hooked;

	private static float _fireTimer;

	private static float _saveDelay = -1f;

	private static bool _editRight;

	private static bool _key1Held;

	private static bool _key2Held;

	private static bool _resetHeld;

	private static ColorRect? _leftBox;

	private static ColorRect? _rightBox;

	private static CanvasLayer? _hudLayer;

	private static Label? _hud;

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
			}
		}
	}

	private static void OnProcessFrame()
	{
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Unknown result type (might be due to invalid IL or missing references)
		//IL_041c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0421: Unknown result type (might be due to invalid IL or missing references)
		//IL_0442: Unknown result type (might be due to invalid IL or missing references)
		//IL_0436: Unknown result type (might be due to invalid IL or missing references)
		//IL_043c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0447: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0488: Unknown result type (might be due to invalid IL or missing references)
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		float num = ((val != null) ? ((float)((Node)val.Root).GetProcessDeltaTime()) : 0.016f);
		if (num <= 0f)
		{
			num = 0.016f;
		}
		if (_saveDelay > 0f)
		{
			_saveDelay -= num;
			if (_saveDelay <= 0f)
			{
				RealtimeCombatSettings.Save();
				Log.Info($"[{"local.action_game"}] kaiser claws saved L({RealtimeCombatSettings.KaiserClawLOffsetX:0},{RealtimeCombatSettings.KaiserClawLOffsetY:0}) {RealtimeCombatSettings.KaiserClawLWidth:0}x{RealtimeCombatSettings.KaiserClawLHeight:0} R({RealtimeCombatSettings.KaiserClawROffsetX:0},{RealtimeCombatSettings.KaiserClawROffsetY:0}) {RealtimeCombatSettings.KaiserClawRWidth:0}x{RealtimeCombatSettings.KaiserClawRHeight:0}", 2);
			}
		}
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsOverOrEnding)
		{
			ClearAll();
			return;
		}
		NCombatRoom instance2 = NCombatRoom.Instance;
		NKaiserCrabBossBackground val2 = FindCrab(instance2);
		if (instance2 == null || val2 == null || !GodotObject.IsInstanceValid((GodotObject)(object)val2))
		{
			ClearAll();
			return;
		}
		CombatState val3 = instance.DebugOnlyGetState();
		if (val3 == null)
		{
			return;
		}
		Creature val4 = null;
		Creature val5 = null;
		foreach (Creature enemy in val3.Enemies)
		{
			if (enemy != null && enemy.IsAlive)
			{
				if (enemy.Monster is Crusher)
				{
					val4 = enemy;
				}
				else if (enemy.Monster is Rocket)
				{
					val5 = enemy;
				}
			}
		}
		if (val4 == null && val5 == null)
		{
			ClearAll();
			return;
		}
		Node2D nodeOrNull = ((Node)val2).GetNodeOrNull<Node2D>(NodePath.op_Implicit("%ArmBoneL"));
		Node2D nodeOrNull2 = ((Node)val2).GetNodeOrNull<Node2D>(NodePath.op_Implicit("%ArmBoneR"));
		PlaceClaw(ref _leftBox, nodeOrNull, RealtimeCombatSettings.KaiserClawLOffsetX, RealtimeCombatSettings.KaiserClawLOffsetY, RealtimeCombatSettings.KaiserClawLWidth, RealtimeCombatSettings.KaiserClawLHeight, !_editRight, new Color(1f, 0.2f, 0.15f, 0.55f), "KaiserClawL");
		PlaceClaw(ref _rightBox, nodeOrNull2, RealtimeCombatSettings.KaiserClawROffsetX, RealtimeCombatSettings.KaiserClawROffsetY, RealtimeCombatSettings.KaiserClawRWidth, RealtimeCombatSettings.KaiserClawRHeight, _editRight, new Color(1f, 0.55f, 0.1f, 0.5f), "KaiserRocketClaw");
		if (DebugCheatSystem.Enabled)
		{
			TickTune(num);
			ShowHud();
		}
		if (PlayerChoicePause.IsPaused)
		{
			return;
		}
		Player me = LocalContext.GetMe((ICombatState)(object)val3);
		Creature val6 = ((me != null) ? me.Creature : null);
		if (val6 == null || !val6.IsAlive)
		{
			return;
		}
		NCreature creatureNode = instance2.GetCreatureNode(val6);
		if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
		{
			return;
		}
		Rect2 aabb = OstyCombatUtil.BodyRect(creatureNode);
		Creature osty;
		NCreature node;
		bool flag = OstyCombatUtil.TryGetAliveOsty(me, out osty, out node);
		Rect2 aabb2 = (Rect2)(flag ? OstyCombatUtil.OstyBodyRect(node) : default(Rect2));
		if (val4 != null)
		{
			if (ObbHitsAabb(_leftBox, aabb) && EnemyMeleeAttackSystem.TryAcceptPlayerHit())
			{
				MonsterContactMove.ResolveHit(val4, val6);
			}
			if (flag && osty != null && ObbHitsAabb(_leftBox, aabb2) && EnemyMeleeAttackSystem.TryAcceptOstyHit())
			{
				MonsterContactMove.ResolveHit(val4, osty);
			}
		}
		if (val5 != null)
		{
			_fireTimer += num;
			float num2 = (float)(RealtimeCombatSettings.TurnLengthSec * 2.0);
			if (num2 < 1f)
			{
				num2 = 8f;
			}
			if (_fireTimer >= num2)
			{
				_fireTimer = 0f;
				val2.PlayRightSideHeavy(0.4f);
				SfxCmd.Play("event:/sfx/monster/kaiser_crab_rocket", 1f);
			}
		}
	}

	private static void TickTune(float delta)
	{
		if (!Input.IsPhysicalKeyPressed((Key)4194325) || !Input.IsPhysicalKeyPressed((Key)4194328))
		{
			_key1Held = false;
			_key2Held = false;
			_resetHeld = false;
			return;
		}
		bool flag = Input.IsPhysicalKeyPressed((Key)49);
		bool flag2 = Input.IsPhysicalKeyPressed((Key)50);
		if (flag && !_key1Held)
		{
			_editRight = false;
		}
		if (flag2 && !_key2Held)
		{
			_editRight = true;
		}
		_key1Held = flag;
		_key2Held = flag2;
		float num = (_editRight ? RealtimeCombatSettings.KaiserClawROffsetX : RealtimeCombatSettings.KaiserClawLOffsetX);
		float num2 = (_editRight ? RealtimeCombatSettings.KaiserClawROffsetY : RealtimeCombatSettings.KaiserClawLOffsetY);
		float num3 = (_editRight ? RealtimeCombatSettings.KaiserClawRWidth : RealtimeCombatSettings.KaiserClawLWidth);
		float num4 = (_editRight ? RealtimeCombatSettings.KaiserClawRHeight : RealtimeCombatSettings.KaiserClawLHeight);
		bool flag3 = false;
		bool flag4 = Input.IsPhysicalKeyPressed((Key)82);
		if (flag4 && !_resetHeld)
		{
			if (_editRight)
			{
				num = 1393.3f;
				num2 = 40.3f;
				num3 = 480f;
				num4 = 324f;
			}
			else
			{
				num = -1506.9f;
				num2 = -194.3f;
				num3 = 480f;
				num4 = 333f;
			}
			flag3 = true;
		}
		_resetHeld = flag4;
		float num5 = 220f * delta;
		if (Input.IsPhysicalKeyPressed((Key)4194319))
		{
			num -= num5;
			flag3 = true;
		}
		if (Input.IsPhysicalKeyPressed((Key)4194321))
		{
			num += num5;
			flag3 = true;
		}
		if (Input.IsPhysicalKeyPressed((Key)4194320))
		{
			num2 -= num5;
			flag3 = true;
		}
		if (Input.IsPhysicalKeyPressed((Key)4194322))
		{
			num2 += num5;
			flag3 = true;
		}
		float num6 = 180f * delta;
		if (Input.IsPhysicalKeyPressed((Key)61) || Input.IsPhysicalKeyPressed((Key)4194437))
		{
			num3 += num6;
			flag3 = true;
		}
		if (Input.IsPhysicalKeyPressed((Key)45) || Input.IsPhysicalKeyPressed((Key)4194435))
		{
			num3 -= num6;
			flag3 = true;
		}
		if (Input.IsPhysicalKeyPressed((Key)93))
		{
			num4 += num6;
			flag3 = true;
		}
		if (Input.IsPhysicalKeyPressed((Key)91))
		{
			num4 -= num6;
			flag3 = true;
		}
		if (flag3)
		{
			RealtimeCombatSettings.SetKaiserClaw(_editRight, num, num2, num3, num4, save: false);
			_saveDelay = 0.45f;
		}
	}

	private static void PlaceClaw(ref ColorRect? box, Node2D? bone, float ox, float oy, float w, float h, bool selected, Color color, string name)
	{
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Expected O, but got Unknown
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		if (bone == null || !GodotObject.IsInstanceValid((GodotObject)(object)bone))
		{
			FreeBox(ref box);
			return;
		}
		if (box == null || !GodotObject.IsInstanceValid((GodotObject)(object)box) || ((Node)box).GetParent() != bone)
		{
			FreeBox(ref box);
			box = new ColorRect
			{
				Name = StringName.op_Implicit(name),
				MouseFilter = (MouseFilterEnum)2,
				ZIndex = 85,
				Color = color
			};
			((CanvasItem)box).ZAsRelative = false;
			((Node)bone).AddChild((Node)(object)box, false, (InternalMode)0);
		}
		((Control)box).Size = new Vector2(w, h);
		((Control)box).Position = new Vector2(ox - w * 0.5f, oy - h * 0.5f);
		((CanvasItem)box).Visible = DebugCheatSystem.Enabled || RealtimeCombatSettings.ShowAttackBoxes;
		float num = (selected ? 0.72f : color.A);
		box.Color = new Color(color.R, color.G, color.B, num);
	}

	private static bool ObbHitsAabb(ColorRect? box, Rect2 aabb)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		if (box == null || !GodotObject.IsInstanceValid((GodotObject)(object)box))
		{
			return false;
		}
		Transform2D globalTransform = ((CanvasItem)box).GetGlobalTransform();
		Vector2 size = ((Control)box).Size;
		Vector2[] array = (Vector2[])(object)new Vector2[4]
		{
			globalTransform * Vector2.Zero,
			globalTransform * new Vector2(size.X, 0f),
			globalTransform * size,
			globalTransform * new Vector2(0f, size.Y)
		};
		Vector2[] array2 = (Vector2[])(object)new Vector2[4]
		{
			((Rect2)(ref aabb)).Position,
			new Vector2(((Rect2)(ref aabb)).End.X, ((Rect2)(ref aabb)).Position.Y),
			((Rect2)(ref aabb)).End,
			new Vector2(((Rect2)(ref aabb)).Position.X, ((Rect2)(ref aabb)).End.Y)
		};
		return SeparatingAxis(array, array2) && SeparatingAxis(array2, array);
	}

	private static bool SeparatingAxis(Vector2[] poly, Vector2[] other)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		Vector2 axis = default(Vector2);
		for (int i = 0; i < 4; i++)
		{
			Vector2 val = poly[(i + 1) % 4] - poly[i];
			((Vector2)(ref axis))..ctor(0f - val.Y, val.X);
			if (!(((Vector2)(ref axis)).LengthSquared() < 0.0001f))
			{
				Project(poly, axis, out var min, out var max);
				Project(other, axis, out var min2, out var max2);
				if (max < min2 || max2 < min)
				{
					return false;
				}
			}
		}
		return true;
	}

	private static void Project(Vector2[] pts, Vector2 axis, out float min, out float max)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		min = float.MaxValue;
		max = float.MinValue;
		for (int i = 0; i < pts.Length; i++)
		{
			Vector2 val = pts[i];
			float num = ((Vector2)(ref val)).Dot(axis);
			if (num < min)
			{
				min = num;
			}
			if (num > max)
			{
				max = num;
			}
		}
	}

	private static NKaiserCrabBossBackground? FindCrab(NCombatRoom? room)
	{
		NCombatBackground val = ((room != null) ? room.Background : null);
		if (val == null || !GodotObject.IsInstanceValid((GodotObject)(object)val))
		{
			return null;
		}
		return ((Node)val).GetNodeOrNull<NKaiserCrabBossBackground>(NodePath.op_Implicit("%KaiserCrab")) ?? ((Node)val).GetNodeOrNull<NKaiserCrabBossBackground>(NodePath.op_Implicit("KaiserCrab"));
	}

	private static void ShowHud()
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected O, but got Unknown
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		if (_hudLayer == null || !GodotObject.IsInstanceValid((GodotObject)(object)_hudLayer))
		{
			_hudLayer = new CanvasLayer
			{
				Layer = 96,
				Name = StringName.op_Implicit("KaiserClawTuneHud")
			};
			((Node)((SceneTree)Engine.GetMainLoop()).Root).AddChild((Node)(object)_hudLayer, false, (InternalMode)0);
		}
		if (_hud == null || !GodotObject.IsInstanceValid((GodotObject)(object)_hud))
		{
			_hud = new Label
			{
				Name = StringName.op_Implicit("KaiserClawTuneLabel"),
				HorizontalAlignment = (HorizontalAlignment)0,
				AutowrapMode = (AutowrapMode)0
			};
			((Control)_hud).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 16);
			((Control)_hud).AddThemeColorOverride(StringName.op_Implicit("font_color"), Colors.White);
			((Control)_hud).AddThemeColorOverride(StringName.op_Implicit("font_outline_color"), Colors.Black);
			((Control)_hud).AddThemeConstantOverride(StringName.op_Implicit("outline_size"), 6);
			((Control)_hud).Position = new Vector2(24f, 86f);
			((Node)_hudLayer).AddChild((Node)(object)_hud, false, (InternalMode)0);
		}
		string value = (_editRight ? "右钳火箭" : "左钳");
		float value2 = (_editRight ? RealtimeCombatSettings.KaiserClawROffsetX : RealtimeCombatSettings.KaiserClawLOffsetX);
		float value3 = (_editRight ? RealtimeCombatSettings.KaiserClawROffsetY : RealtimeCombatSettings.KaiserClawLOffsetY);
		float value4 = (_editRight ? RealtimeCombatSettings.KaiserClawRWidth : RealtimeCombatSettings.KaiserClawLWidth);
		float value5 = (_editRight ? RealtimeCombatSettings.KaiserClawRHeight : RealtimeCombatSettings.KaiserClawLHeight);
		_hud.Text = $"帝王蟹碰撞 [{value}] 偏移 {value2:0},{value3:0}  大小 {value4:0}x{value5:0}\n" + "Shift+Alt  1/2切换钳子  方向键移动  =/-宽  [/]高  R重置";
		((CanvasItem)_hud).Visible = true;
	}

	private static void FreeBox(ref ColorRect? box)
	{
		if (box != null && GodotObject.IsInstanceValid((GodotObject)(object)box))
		{
			((Node)box).QueueFree();
		}
		box = null;
	}

	private static void ClearAll()
	{
		_fireTimer = 0f;
		FreeBox(ref _leftBox);
		FreeBox(ref _rightBox);
		if (_hud != null && GodotObject.IsInstanceValid((GodotObject)(object)_hud))
		{
			((CanvasItem)_hud).Visible = false;
		}
	}
}
[HarmonyPatch(typeof(NMultiplayerPlayerStateContainer), "Initialize")]
internal static class LocalPlayerStateHudPatch
{
	private static void Postfix(NMultiplayerPlayerStateContainer __instance, RunState runState)
	{
		if (runState != null && GodotObject.IsInstanceValid((GodotObject)(object)__instance) && ((Node)__instance).GetChildCount(false) <= 0)
		{
			Player me = LocalContext.GetMe((IPlayerCollection)(object)runState);
			if (me != null)
			{
				NMultiplayerPlayerState val = NMultiplayerPlayerState.Create(me);
				GodotTreeExtensions.AddChildSafely((Node)(object)__instance, (Node)(object)val);
				HideNameplate(val);
				Traverse.Create((object)__instance).Field("_nodes").GetValue<List<NMultiplayerPlayerState>>()?.Add(val);
				ConnectReposition(__instance);
				__instance.ShowImmediately();
			}
		}
	}

	private static void HideNameplate(NMultiplayerPlayerState state)
	{
		CanvasItem value = Traverse.Create((object)state).Field("_nameplateLabel").GetValue<CanvasItem>();
		if (value != null && GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			value.Visible = false;
		}
	}

	private static void ConnectReposition(NMultiplayerPlayerStateContainer container)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		NMultiplayerPlayerStateContainer container2 = container;
		NRun instance = NRun.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			NGlobalUi globalUi = instance.GlobalUi;
			obj = ((globalUi != null) ? globalUi.RelicInventory : null);
		}
		NRelicInventory val = (NRelicInventory)obj;
		if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
		{
			((GodotObject)val).Connect(SignalName.RelicsChanged, Callable.From((Action)Reposition), 0u);
		}
		Viewport viewport = ((Node)container2).GetViewport();
		if (viewport != null)
		{
			((GodotObject)viewport).Connect(SignalName.SizeChanged, Callable.From((Action)Reposition), 0u);
		}
		void Reposition()
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)container2))
			{
				Traverse.Create((object)container2).Method("UpdatePositionAfterOneFrame", Array.Empty<object>()).GetValue();
			}
		}
	}
}
[ModInitializer("Initialize")]
public static class ModEntry
{
	public const string ModId = "local.action_game";

	public static void Initialize()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		CombatCompat.PatchAllSafe(new Harmony("local.action_game"), typeof(ModEntry).Assembly);
		RealtimeCombatSettings.Load();
		FloorBackgroundReplace.EnsureInputHook();
		ActionGameNetSync.EnsureHook();
		PlayerPoseSync.EnsureHook();
		PlayerStatsNetSync.EnsureHook();
		CombatPatches.SubscribeCombatEvents();
		VanillaHotkeyDisable.EnsureHooks();
		EscPauseHotkey.EnsureHook();
		QuickPlaySystem.EnsureInputHook();
		PlayerWasdMoveSystem.EnsureInputHook();
		OstyMoveSystem.EnsureHook();
		EnemyChaseSystem.EnsureHook();
		CreatureYSortSystem.EnsureHook();
		MonsterSpawnSystem.EnsureHook();
		AutoSpawnSystem.EnsureHook();
		EnemyMeleeAttackSystem.EnsureHook();
		KaiserCrabCombatSystem.EnsureHook();
		SovereignBladeOrbitSystem.EnsureHook();
		OrbOrbitSystem.EnsureHook();
		RemoteOrbitFxSync.EnsureHook();
		GoldDropSystem.EnsureHook();
		EncounterTimerSystem.EnsureHook();
		AutoCardSelectSystem.EnsureHook();
		TurnLengthHud.EnsureShown();
		DebugCheatSystem.EnsureHook();
		Log.Info($"[{"local.action_game"}] loaded - action game prototype (turn={RealtimeCombatSettings.TurnLengthSec:0.##}s)", 2);
	}
}
internal static class MonsterContactMove
{
	private const decimal FallbackDamage = 5m;

	private static readonly HashSet<IntentType> Allowed = new HashSet<IntentType>
	{
		(IntentType)0,
		(IntentType)1,
		(IntentType)2,
		(IntentType)3,
		(IntentType)4,
		(IntentType)11,
		(IntentType)12
	};

	public static async Task ResolveHit(Creature dealer, Creature target)
	{
		try
		{
			CombatManager manager = CombatManager.Instance;
			if (manager == null || !manager.IsInProgress || manager.IsOverOrEnding || dealer == null || !dealer.IsAlive || target == null || !target.IsAlive)
			{
				return;
			}
			MonsterModel monster = dealer.Monster;
			if (monster == null)
			{
				await FallbackDamageAsync(dealer, target, "no-monster");
				return;
			}
			if (monster.IsPerformingMove)
			{
				await FallbackDamageAsync(dealer, target, "busy");
				return;
			}
			EnsureStateMachine(monster);
			List<MoveState> pool = CollectAllowedMoves(monster);
			if (pool.Count == 0)
			{
				await FallbackDamageAsync(dealer, target, "empty-pool");
				return;
			}
			MoveState picked = pool[Random.Shared.Next(pool.Count)];
			bool isAttack = HasAttack(picked);
			bool hasExtra = HasNonAttackIntent(picked);
			Log.Info($"[{"local.action_game"}] contact move {dealer.LogName} -> {((MonsterState)picked).Id} attack={isAttack} extra={hasExtra} intents={DescribeIntents(picked)}", 2);
			if (isAttack)
			{
				await DealAttackIntentsInstantAsync(dealer, target, picked);
				if (hasExtra)
				{
					StartPerformSuppressed(monster, picked);
				}
				return;
			}
			await DamageMoveAsync(dealer, target, 5m);
			StartPerformSuppressed(monster, picked);
		}
		catch (Exception ex)
		{
			Log.Warn($"[{"local.action_game"}] contact move failed: {ex}", 2);
			await FallbackDamageAsync(dealer, target, "exception");
		}
	}

	private static async Task DealAttackIntentsInstantAsync(Creature dealer, Creature target, MoveState move)
	{
		IReadOnlyList<AbstractIntent> intents = move.Intents;
		if (intents == null)
		{
			return;
		}
		BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
		foreach (AbstractIntent intent in intents)
		{
			AttackIntent attack = (AttackIntent)(object)((intent is AttackIntent) ? intent : null);
			if (attack == null)
			{
				continue;
			}
			int hits = Math.Max(1, attack.Repeats);
			for (int i = 0; i < hits; i++)
			{
				CombatManager manager = CombatManager.Instance;
				if (manager == null || !manager.IsInProgress || manager.IsOverOrEnding || !dealer.IsAlive || !target.IsAlive)
				{
					return;
				}
				decimal amount = ResolveBaseDamage(attack);
				Log.Info($"[{"local.action_game"}] contact hit instant {amount} ({i + 1}/{hits}) from {dealer.LogName}", 2);
				await CreatureCmd.Damage((PlayerChoiceContext)(object)ctx, target, amount, (ValueProp)8, dealer);
			}
		}
	}

	private static decimal ResolveBaseDamage(AttackIntent attack)
	{
		Func<decimal> damageCalc = attack.DamageCalc;
		if (damageCalc != null)
		{
			decimal num = damageCalc();
			if (num > 0m)
			{
				return num;
			}
		}
		return 5m;
	}

	private static void StartPerformSuppressed(MonsterModel monster, MoveState move)
	{
		PerformSuppressedAsync(monster, move);
	}

	private static async Task PerformSuppressedAsync(MonsterModel monster, MoveState move)
	{
		List<(AttackIntent Intent, Func<decimal>? Calc)> saved = SuppressAttackDamage(move);
		try
		{
			if (!monster.IsPerformingMove)
			{
				monster.SetMoveImmediate(move, false);
				await monster.PerformMove();
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Log.Warn($"[{"local.action_game"}] contact PerformMove (fx) failed: {ex}", 2);
		}
		finally
		{
			RestoreAttackDamage(saved);
		}
	}

	private static List<(AttackIntent Intent, Func<decimal>? Calc)> SuppressAttackDamage(MoveState move)
	{
		List<(AttackIntent, Func<decimal>)> list = new List<(AttackIntent, Func<decimal>)>();
		IReadOnlyList<AbstractIntent> intents = move.Intents;
		if (intents == null)
		{
			return list;
		}
		PropertyInfo propertyInfo = AccessTools.Property(typeof(AttackIntent), "DamageCalc");
		foreach (AbstractIntent item in intents)
		{
			AttackIntent val = (AttackIntent)(object)((item is AttackIntent) ? item : null);
			if (val != null)
			{
				list.Add((val, val.DamageCalc));
				propertyInfo?.SetValue(val, (Func<decimal>)(() => 0m));
			}
		}
		return list;
	}

	private static void RestoreAttackDamage(List<(AttackIntent Intent, Func<decimal>? Calc)> saved)
	{
		PropertyInfo propertyInfo = AccessTools.Property(typeof(AttackIntent), "DamageCalc");
		foreach (var (obj, func) in saved)
		{
			if (func != null)
			{
				propertyInfo?.SetValue(obj, func);
			}
		}
	}

	private static void EnsureStateMachine(MonsterModel monster)
	{
		if (monster.MoveStateMachine == null || monster.MoveStateMachine.States.Count <= 0)
		{
			monster.SetUpForCombat();
		}
	}

	private static List<MoveState> CollectAllowedMoves(MonsterModel monster)
	{
		List<MoveState> list = new List<MoveState>();
		MonsterMoveStateMachine moveStateMachine = monster.MoveStateMachine;
		if (((moveStateMachine != null) ? moveStateMachine.States : null) == null)
		{
			return list;
		}
		foreach (MonsterState value in moveStateMachine.States.Values)
		{
			MoveState val = (MoveState)(object)((value is MoveState) ? value : null);
			if (val != null && ((MonsterState)val).IsMove && IsAllowed(val))
			{
				list.Add(val);
			}
		}
		return list;
	}

	private static bool IsAllowed(MoveState move)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		IReadOnlyList<AbstractIntent> intents = move.Intents;
		if (intents == null || intents.Count == 0)
		{
			return false;
		}
		foreach (AbstractIntent item in intents)
		{
			if (item == null || !Allowed.Contains(item.IntentType))
			{
				return false;
			}
		}
		return true;
	}

	private static bool HasAttack(MoveState move)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Invalid comparison between Unknown and I4
		IReadOnlyList<AbstractIntent> intents = move.Intents;
		if (intents == null)
		{
			return false;
		}
		foreach (AbstractIntent item in intents)
		{
			if (item != null && (int)item.IntentType == 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasNonAttackIntent(MoveState move)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Invalid comparison between Unknown and I4
		IReadOnlyList<AbstractIntent> intents = move.Intents;
		if (intents == null)
		{
			return false;
		}
		foreach (AbstractIntent item in intents)
		{
			if (item != null && (int)item.IntentType > 0)
			{
				return true;
			}
		}
		return false;
	}

	private static string DescribeIntents(MoveState move)
	{
		IReadOnlyList<AbstractIntent> intents = move.Intents;
		if (intents == null || intents.Count == 0)
		{
			return "[]";
		}
		return "[" + string.Join(",", intents.Select(delegate(AbstractIntent i)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			object obj;
			if (i == null)
			{
				obj = null;
			}
			else
			{
				IntentType intentType = i.IntentType;
				obj = ((object)(IntentType)(ref intentType)).ToString();
			}
			if (obj == null)
			{
				obj = "null";
			}
			return (string)obj;
		})) + "]";
	}

	private static async Task FallbackDamageAsync(Creature? dealer, Creature? target, string reason)
	{
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(29, 4);
		defaultInterpolatedStringHandler.AppendLiteral("[");
		defaultInterpolatedStringHandler.AppendFormatted("local.action_game");
		defaultInterpolatedStringHandler.AppendLiteral("] contact fallback ");
		defaultInterpolatedStringHandler.AppendFormatted(5m);
		defaultInterpolatedStringHandler.AppendLiteral(" (");
		defaultInterpolatedStringHandler.AppendFormatted(reason);
		defaultInterpolatedStringHandler.AppendLiteral(") from ");
		defaultInterpolatedStringHandler.AppendFormatted((dealer != null) ? dealer.LogName : null);
		Log.Info(defaultInterpolatedStringHandler.ToStringAndClear(), 2);
		if (dealer != null && target != null)
		{
			await DamageMoveAsync(dealer, target, 5m);
		}
	}

	private static async Task DamageMoveAsync(Creature dealer, Creature target, decimal amount)
	{
		CombatManager manager = CombatManager.Instance;
		if (manager != null && manager.IsInProgress && !manager.IsOverOrEnding && dealer != null && dealer.IsAlive && target != null && target.IsAlive)
		{
			BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
			await CreatureCmd.Damage((PlayerChoiceContext)(object)ctx, target, amount, (ValueProp)8, dealer);
		}
	}
}
[HarmonyPatch(typeof(NCreature), "StartDeathAnim")]
internal static class MonsterDeathSpeedPatch
{
	internal const float SpeedMul = 1.35f;

	private static void Prefix(NCreature __instance)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		if (ShouldSpeed(__instance) && __instance.HasSpineAnimation)
		{
			SpineAnimationAccess spineAnimation = __instance.SpineAnimation;
			if (((SpineAnimationAccess)(ref spineAnimation)).IsValid)
			{
				spineAnimation = __instance.SpineAnimation;
				((SpineAnimationAccess)(ref spineAnimation)).SetTimeScale(1.35f);
			}
		}
	}

	private static void Postfix(NCreature __instance, ref float __result)
	{
		if (ShouldSpeed(__instance))
		{
			__result /= 1.35f;
		}
	}

	internal static bool ShouldSpeed(NCreature? node)
	{
		return node != null && GodotObject.IsInstanceValid((GodotObject)(object)node) && EnemyBehaviorCatalog.IsRegularMinion(node.Entity);
	}
}
[HarmonyPatch(typeof(NCreature), "GetCurrentAnimationTimeRemaining")]
internal static class MonsterDeathWaitSpeedPatch
{
	private static void Postfix(NCreature __instance, ref float __result)
	{
		if (MonsterDeathSpeedPatch.ShouldSpeed(__instance))
		{
			Creature entity = __instance.Entity;
			if (entity == null || !entity.IsAlive)
			{
				__result /= 1.35f;
			}
		}
	}
}
internal static class MonsterSpawnSystem
{
	private sealed class SpawnPin
	{
		public Creature Creature = null;

		public Vector2 Pos;

		public int Frames;
	}

	private static bool _hooked;

	private static bool _busy;

	private static int _serial;

	internal const int AutoMaxAlive = 48;

	private static Marker2D? _slotMarker;

	private static readonly List<SpawnPin> _pins = new List<SpawnPin>();

	internal static bool IsBusy => _busy;

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
			}
		}
	}

	private static void OnProcessFrame()
	{
		CombatManager instance = CombatManager.Instance;
		if (instance != null && instance.IsInProgress && !instance.IsOverOrEnding)
		{
			TickPins();
		}
	}

	internal static int AliveCount()
	{
		CombatManager instance = CombatManager.Instance;
		CombatState val = ((instance != null) ? instance.DebugOnlyGetState() : null);
		if (val == null)
		{
			return 0;
		}
		return val.Enemies.Count((Creature e) => e != null && e.IsAlive && e.IsMonster);
	}

	internal static async Task<bool> TrySpawnAt(Vector2 globalPos, SpawnEntry entry)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (_busy)
		{
			return false;
		}
		_busy = true;
		try
		{
			CombatManager manager = CombatManager.Instance;
			if (manager == null || !manager.IsInProgress || manager.IsOverOrEnding)
			{
				return false;
			}
			CombatState state = manager.DebugOnlyGetState();
			if (state == null || AliveCount() >= 48)
			{
				return false;
			}
			return await SpawnOneAsync(entry, globalPos, state);
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Log.Warn($"[{"local.action_game"}] auto spawn failed: {ex}", 2);
			return false;
		}
		finally
		{
			_busy = false;
		}
	}

	private static async Task<bool> SpawnOneAsync(SpawnEntry entry, Vector2 globalPos, CombatState state)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		MonsterModel model = entry.Create().ToMutable();
		string slot = BindSlot(globalPos);
		Creature creature = await CreatureCmd.Add(model, (ICombatState)(object)state, (CombatSide)2, slot);
		if (creature == null)
		{
			Log.Warn("[local.action_game] spawn failed slot=" + slot, 2);
			return false;
		}
		NCombatRoom room = NCombatRoom.Instance;
		NCreature node = ((room != null) ? room.GetCreatureNode(creature) : null);
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			((Control)node).GlobalPosition = globalPos;
		}
		Pin(creature, globalPos);
		FreeSlotMarker();
		Log.Info($"[{"local.action_game"}] auto spawned {creature.LogName} at {globalPos}", 2);
		return true;
	}

	private static string? BindSlot(Vector2 globalPos)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		FreeSlotMarker();
		NCombatRoom instance = NCombatRoom.Instance;
		Control val = (Control)((instance == null) ? null : /*isinst with value type is only supported in some contexts*/);
		if (val == null || !GodotObject.IsInstanceValid((GodotObject)(object)val))
		{
			return null;
		}
		string text = $"ag_spawn_{_serial++}";
		_slotMarker = new Marker2D
		{
			Name = StringName.op_Implicit(text)
		};
		((Node)val).AddChild((Node)(object)_slotMarker, false, (InternalMode)0);
		((Node2D)_slotMarker).GlobalPosition = globalPos;
		return text;
	}

	private static void FreeSlotMarker()
	{
		if (_slotMarker != null && GodotObject.IsInstanceValid((GodotObject)(object)_slotMarker))
		{
			((Node)_slotMarker).QueueFree();
		}
		_slotMarker = null;
	}

	private static void Pin(Creature creature, Vector2 pos)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		_pins.Add(new SpawnPin
		{
			Creature = creature,
			Pos = pos,
			Frames = 20
		});
	}

	private static void TickPins()
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		if (_pins.Count == 0)
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		for (int num = _pins.Count - 1; num >= 0; num--)
		{
			SpawnPin spawnPin = _pins[num];
			NCreature val = ((instance != null) ? instance.GetCreatureNode(spawnPin.Creature) : null);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				((Control)val).GlobalPosition = spawnPin.Pos;
			}
			spawnPin.Frames--;
			if (spawnPin.Frames <= 0)
			{
				_pins.RemoveAt(num);
			}
		}
	}
}
internal static class MoveSpeedUtil
{
	public const float DemonFormSpeedMult = 1.5f;

	public static float ForCreature(Creature? creature, float baseSpeed)
	{
		if (creature == null)
		{
			return baseSpeed;
		}
		int num = creature.GetPowerAmount<DexterityPower>() + creature.GetPowerAmount<TemporaryDexterityPower>();
		float num2 = ((num > 0) ? (1f + (float)num * RealtimeCombatSettings.DexteritySpeedBonusPerStack) : 1f);
		float num3 = ((creature.GetPowerAmount<DemonFormPower>() > 0) ? 1.5f : 1f);
		return baseSpeed * num2 * num3;
	}
}
internal static class OrbitHitNetSync
{
	public const byte FlagMove = 1;

	public const byte FlagUnpowered = 2;

	private static bool _capturing;

	private static bool _applyingRemote;

	private static float _capturedTotal;

	private static byte _capturedFlags;

	public static void OnCombatEnded()
	{
		_capturing = false;
		_applyingRemote = false;
		_capturedTotal = 0f;
		_capturedFlags = 0;
	}

	public static void BroadcastHit(decimal amount, byte flags)
	{
		if (ActionGameNet.IsOnline && !(amount <= 0m))
		{
			ActionGameNet.Send(new ActionGameNetMessage
			{
				Opcode = 6,
				CasterNetId = ActionGameNet.LocalNetId,
				Amount = (float)amount,
				HitCount = 1,
				Flags = flags
			});
			Log.Info($"[{"local.action_game"}] orbit-hit net send dmg={amount:0.#} flags={flags}", 2);
		}
	}

	public static void BeginCapture(byte flags)
	{
		if (ActionGameNet.IsOnline)
		{
			_capturing = true;
			_capturedTotal = 0f;
			_capturedFlags = flags;
		}
	}

	public static void NoteCapturedDamage(decimal amount)
	{
		if (_capturing && !_applyingRemote && ActionGameNet.IsOnline && !(amount <= 0m))
		{
			_capturedTotal += (float)amount;
		}
	}

	public static void EndCaptureAndBroadcast()
	{
		if (_capturing)
		{
			_capturing = false;
			float capturedTotal = _capturedTotal;
			byte capturedFlags = _capturedFlags;
			_capturedTotal = 0f;
			_capturedFlags = 0;
			if (capturedTotal > 0f)
			{
				BroadcastHit((decimal)capturedTotal, capturedFlags);
			}
		}
	}

	public static void OnRemote(ActionGameNetMessage msg)
	{
		if (ActionGameNet.IsOnline)
		{
			Log.Info($"[{"local.action_game"}] orbit-hit net recv from={msg.CasterNetId} dmg={msg.Amount:0.#}", 2);
			ApplyRemoteAsync(msg);
		}
	}

	private static async Task ApplyRemoteAsync(ActionGameNetMessage msg)
	{
		try
		{
			if (!CombatPeerUtil.TryGetPose(msg.CasterNetId, out var origin))
			{
				Log.Warn($"[{"local.action_game"}] orbit-hit APPLY FAIL no pose for {msg.CasterNetId}", 2);
				return;
			}
			CombatManager instance = CombatManager.Instance;
			CombatState state = ((instance != null) ? instance.DebugOnlyGetState() : null);
			Player localMe = ((state != null) ? LocalContext.GetMe((ICombatState)(object)state) : null);
			Creature dealer = ((localMe != null) ? localMe.Creature : null);
			string dealerSrc = "localMe";
			if (dealer == null || !dealer.IsAlive)
			{
				Player? obj = CombatPeerUtil.FindPlayer(msg.CasterNetId);
				dealer = ((obj != null) ? obj.Creature : null);
				dealerSrc = "remoteCaster";
			}
			if (dealer == null || !dealer.IsAlive)
			{
				Log.Warn("[local.action_game] orbit-hit APPLY FAIL no dealer", 2);
				return;
			}
			decimal amount = (decimal)msg.Amount;
			if (amount <= 0m)
			{
				return;
			}
			ValueProp props = (ValueProp)4;
			int hits = Math.Max(1, (int)msg.HitCount);
			BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
			_applyingRemote = true;
			try
			{
				for (int i = 0; i < hits; i++)
				{
					if (!ActionGameNet.IsOnline)
					{
						return;
					}
					Creature target = CombatPeerUtil.PickNearestEnemy(origin);
					if (target == null || !target.IsAlive)
					{
						Log.Warn("[local.action_game] orbit-hit APPLY FAIL no enemy in range", 2);
						return;
					}
					int hpBefore = target.CurrentHp;
					Log.Info($"[{"local.action_game"}] orbit-hit APPLY {target.LogName} hp={hpBefore} deal={amount:0.#} dealerSrc={dealerSrc}", 2);
					await CreatureCmd.Damage((PlayerChoiceContext)(object)ctx, target, amount, props, dealer);
					if (target.IsAlive && target.CurrentHp >= hpBefore)
					{
						DamageResult forced = target.LoseHpInternal(amount, props);
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 3);
						defaultInterpolatedStringHandler.AppendLiteral("[");
						defaultInterpolatedStringHandler.AppendFormatted("local.action_game");
						defaultInterpolatedStringHandler.AppendLiteral("] orbit-hit LoseHpInternal fallback ");
						defaultInterpolatedStringHandler.AppendLiteral("hpNow=");
						defaultInterpolatedStringHandler.AppendFormatted(target.CurrentHp);
						defaultInterpolatedStringHandler.AppendLiteral(" unblocked=");
						defaultInterpolatedStringHandler.AppendFormatted((forced != null) ? new int?(forced.UnblockedDamage) : null);
						Log.Warn(defaultInterpolatedStringHandler.ToStringAndClear(), 2);
					}
					else
					{
						Log.Info($"[{"local.action_game"}] orbit-hit APPLY ok hp {hpBefore}->{target.CurrentHp}", 2);
					}
					CombatPeerUtil.PushEnemyFromPoint(target, origin);
				}
			}
			finally
			{
				_applyingRemote = false;
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Log.Error($"[{"local.action_game"}] orbit-hit APPLY EXCEPTION: {ex}", 2);
		}
	}
}
[HarmonyPatch(typeof(CreatureCmd), "Damage", new Type[]
{
	typeof(PlayerChoiceContext),
	typeof(Creature),
	typeof(decimal),
	typeof(ValueProp),
	typeof(Creature)
})]
internal static class OrbitHitDamageCaptureSinglePatch
{
	private static void Prefix(decimal amount)
	{
		OrbitHitNetSync.NoteCapturedDamage(amount);
	}
}
[HarmonyPatch(typeof(CreatureCmd), "Damage", new Type[]
{
	typeof(PlayerChoiceContext),
	typeof(IEnumerable<Creature>),
	typeof(decimal),
	typeof(ValueProp),
	typeof(Creature)
})]
internal static class OrbitHitDamageCaptureMultiPatch
{
	private static void Prefix(decimal amount)
	{
		OrbitHitNetSync.NoteCapturedDamage(amount);
	}
}
internal static class OrbOrbitSystem
{
	private const float TargetOrbitSpeed = 300f;

	private const float HitCooldownSec = 1f;

	private const float CenterHeightFactor = 0.6f;

	private const float OrbitRadius = 186.66667f;

	private const float HitHalf = 36f;

	private static bool _hooked;

	private static double _sharedPhase;

	private static readonly Dictionary<(ulong Orb, ulong Enemy), float> HitCooldowns = new Dictionary<(ulong, ulong), float>();

	private static readonly HashSet<ulong> BusyOrbs = new HashSet<ulong>();

	private static readonly Dictionary<ulong, Polygon2D> DebugPolys = new Dictionary<ulong, Polygon2D>();

	private static readonly List<(ulong Orb, ulong Enemy)> _deadCooldownKeys = new List<(ulong, ulong)>();

	private static readonly List<ulong> _deadPolyKeys = new List<ulong>();

	private static readonly List<NOrb> _orbScratch = new List<NOrb>();

	private static readonly FieldRef<NOrbManager, List<NOrb>>? OrbsRef = AccessTools.FieldRefAccess<NOrbManager, List<NOrb>>("_orbs");

	private static readonly FieldRef<NOrbManager, Tween>? CurTweenRef = AccessTools.FieldRefAccess<NOrbManager, Tween>("_curTween");

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
			}
		}
	}

	private static void OnProcessFrame()
	{
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsOverOrEnding)
		{
			ClearCombatState();
		}
		else
		{
			if (PlayerChoicePause.IsPaused)
			{
				return;
			}
			NCombatRoom instance2 = NCombatRoom.Instance;
			if (instance2 == null)
			{
				return;
			}
			CombatState val = instance.DebugOnlyGetState();
			if (((val != null) ? val.Players : null) == null)
			{
				return;
			}
			float num = 1f / 60f;
			bool flag = false;
			int num2 = 0;
			TickCooldowns(num);
			_sharedPhase += (double)(300f * num) / 400.0;
			foreach (Player player in val.Players)
			{
				if (((player != null) ? player.Creature : null) == null || !player.Creature.IsAlive)
				{
					continue;
				}
				NCreature creatureNode = instance2.GetCreatureNode(player.Creature);
				if (creatureNode == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
				{
					continue;
				}
				NOrbManager orbManager = creatureNode.OrbManager;
				if (orbManager == null || !GodotObject.IsInstanceValid((GodotObject)(object)orbManager))
				{
					continue;
				}
				if (!flag)
				{
					float num3 = (float)((Node)creatureNode).GetProcessDeltaTime();
					if (num3 > 0f)
					{
						num = num3;
					}
					flag = true;
				}
				SyncOrbit(orbManager, creatureNode);
				if (!LocalContext.IsMe(player))
				{
					continue;
				}
				foreach (NOrb item in _orbScratch)
				{
					OrbModel model = item.Model;
					if ((model is LightningOrb || model is GlassOrb) ? true : false)
					{
						num2++;
					}
				}
				RemoteOrbitFxSync.SetLocalOrbs(num2);
				ResolveHits(player.Creature, instance2);
			}
		}
	}

	private static void SyncOrbit(NOrbManager orbManager, NCreature playerNode)
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_017b: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		if (OrbsRef == null)
		{
			return;
		}
		List<NOrb> list = OrbsRef.Invoke(orbManager);
		if (list == null || list.Count == 0)
		{
			return;
		}
		KillLayoutTween(orbManager);
		_orbScratch.Clear();
		foreach (NOrb item in list)
		{
			if (item != null && GodotObject.IsInstanceValid((GodotObject)(object)item))
			{
				_orbScratch.Add(item);
			}
		}
		int count = _orbScratch.Count;
		if (count == 0)
		{
			return;
		}
		Vector2 val = OrbitCenter(playerNode);
		for (int i = 0; i < count; i++)
		{
			float num = (float)((_sharedPhase + (double)i / (double)count) % 1.0);
			if (num < 0f)
			{
				num += 1f;
			}
			float num2 = num * ((float)Math.PI * 2f);
			Vector2 val2 = new Vector2(Mathf.Cos(num2), Mathf.Sin(num2)) * 186.66667f;
			NOrb val3 = _orbScratch[i];
			Vector2 val4 = ((Control)val3).Size * 0.5f;
			if (val4.X < 1f || val4.Y < 1f)
			{
				((Vector2)(ref val4))..ctor(36f, 36f);
			}
			((Control)val3).GlobalPosition = val + val2 - val4;
		}
	}

	private static void KillLayoutTween(NOrbManager orbManager)
	{
		if (CurTweenRef != null)
		{
			Tween val = CurTweenRef.Invoke(orbManager);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val) && val.IsRunning())
			{
				val.Kill();
			}
		}
	}

	private static Vector2 OrbitCenter(NCreature playerNode)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		float num = ((Control)playerNode).Size.Y;
		if (num < 40f)
		{
			num = 120f;
		}
		return ((Control)playerNode).GlobalPosition + new Vector2(0f, (0f - num) * 0.6f);
	}

	private static void ResolveHits(Creature player, NCombatRoom room)
	{
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		List<NCreature> list = new List<NCreature>(8);
		foreach (NCreature creatureNode in room.CreatureNodes)
		{
			if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				list.Add(creatureNode);
			}
		}
		foreach (NOrb item in _orbScratch)
		{
			OrbModel model = item.Model;
			if (model == null || (!(model is LightningOrb) && !(model is GlassOrb)))
			{
				continue;
			}
			ulong instanceId = ((GodotObject)item).GetInstanceId();
			if (BusyOrbs.Contains(instanceId) || !TryGetOrbRect(item, out var rect))
			{
				continue;
			}
			ShowDebugRect(item, rect, (Node)(object)room);
			foreach (NCreature item2 in list)
			{
				if (!GodotObject.IsInstanceValid((GodotObject)(object)item2))
				{
					continue;
				}
				Creature entity = item2.Entity;
				if (entity == null || !entity.IsAlive || !OstyCombatUtil.IsHostileMonster(entity))
				{
					continue;
				}
				Rect2 val = BodyRect(item2);
				if (((Rect2)(ref rect)).Intersects(val, false))
				{
					(ulong, ulong) key = (instanceId, ((GodotObject)item2).GetInstanceId());
					if (!HitCooldowns.TryGetValue(key, out var value) || !(value > 0f))
					{
						HitCooldowns[key] = 1f;
						BusyOrbs.Add(instanceId);
						HitAsync(instanceId, model, player, entity);
					}
				}
			}
		}
		CleanupDebugPolys();
	}

	private static async Task HitAsync(ulong orbId, OrbModel model, Creature player, Creature enemy)
	{
		try
		{
			CombatManager manager = CombatManager.Instance;
			if (manager == null || !manager.IsInProgress || manager.IsOverOrEnding || player == null || !player.IsAlive || enemy == null || !enemy.IsAlive)
			{
				return;
			}
			BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
			if (model is LightningOrb)
			{
				OrbitHitNetSync.BeginCapture(2);
				try
				{
					await model.TriggerPassive((PlayerChoiceContext)(object)ctx, enemy);
					return;
				}
				finally
				{
					OrbitHitNetSync.EndCaptureAndBroadcast();
				}
			}
			GlassOrb glass = (GlassOrb)(object)((model is GlassOrb) ? model : null);
			if (glass != null)
			{
				decimal amount = ((OrbModel)glass).PassiveVal;
				if (!(amount <= 0m))
				{
					await CreatureCmd.Damage((PlayerChoiceContext)(object)ctx, enemy, amount, (ValueProp)4, player);
					OrbitHitNetSync.BroadcastHit(amount, 2);
				}
			}
		}
		finally
		{
			BusyOrbs.Remove(orbId);
		}
	}

	private static bool TryGetOrbRect(NOrb orb, out Rect2 rect)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		Vector2 size = ((Control)orb).Size;
		if (size.X < 8f || size.Y < 8f)
		{
			((Vector2)(ref size))..ctor(72f, 72f);
		}
		rect = new Rect2(((Control)orb).GlobalPosition, size);
		return true;
	}

	private static Rect2 BodyRect(NCreature node)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		Vector2 size = ((Control)node).Size;
		if (size.X < 8f || size.Y < 8f)
		{
			((Vector2)(ref size))..ctor(80f, 120f);
		}
		Vector2 globalPosition = ((Control)node).GlobalPosition;
		float num = size.X * 0.8f;
		float num2 = size.Y * 0.4f;
		return new Rect2(new Vector2(globalPosition.X - num * 0.5f, globalPosition.Y - num2), new Vector2(num, num2));
	}

	private static void TickCooldowns(float delta)
	{
		if (HitCooldowns.Count == 0)
		{
			return;
		}
		_deadCooldownKeys.Clear();
		foreach (KeyValuePair<(ulong, ulong), float> hitCooldown in HitCooldowns)
		{
			float num = hitCooldown.Value - delta;
			if (num <= 0f)
			{
				_deadCooldownKeys.Add(hitCooldown.Key);
			}
			else
			{
				HitCooldowns[hitCooldown.Key] = num;
			}
		}
		foreach (var deadCooldownKey in _deadCooldownKeys)
		{
			HitCooldowns.Remove(deadCooldownKey);
		}
	}

	private static void ShowDebugRect(NOrb orb, Rect2 rect, Node parent)
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		ulong instanceId = ((GodotObject)orb).GetInstanceId();
		if (!DebugPolys.TryGetValue(instanceId, out Polygon2D value) || value == null || !GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			value = new Polygon2D
			{
				Name = StringName.op_Implicit("ActionGameOrbHitRect"),
				ZIndex = 90,
				Color = new Color(0.3f, 0.85f, 1f, 0.35f)
			};
			((CanvasItem)value).ZAsRelative = false;
			parent.AddChild((Node)(object)value, false, (InternalMode)0);
			DebugPolys[instanceId] = value;
		}
		value.Polygon = (Vector2[])(object)new Vector2[4]
		{
			((Rect2)(ref rect)).Position,
			((Rect2)(ref rect)).Position + new Vector2(((Rect2)(ref rect)).Size.X, 0f),
			((Rect2)(ref rect)).Position + ((Rect2)(ref rect)).Size,
			((Rect2)(ref rect)).Position + new Vector2(0f, ((Rect2)(ref rect)).Size.Y)
		};
		((CanvasItem)value).Visible = RealtimeCombatSettings.ShowAttackBoxes;
	}

	private static void CleanupDebugPolys()
	{
		_deadPolyKeys.Clear();
		HashSet<ulong> hashSet = new HashSet<ulong>();
		foreach (NOrb item in _orbScratch)
		{
			hashSet.Add(((GodotObject)item).GetInstanceId());
		}
		foreach (KeyValuePair<ulong, Polygon2D> debugPoly in DebugPolys)
		{
			if (!hashSet.Contains(debugPoly.Key))
			{
				_deadPolyKeys.Add(debugPoly.Key);
			}
		}
		foreach (ulong deadPolyKey in _deadPolyKeys)
		{
			if (DebugPolys.Remove(deadPolyKey, out Polygon2D value) && value != null && GodotObject.IsInstanceValid((GodotObject)(object)value))
			{
				((Node)value).QueueFree();
			}
		}
	}

	private static void ClearCombatState()
	{
		HitCooldowns.Clear();
		BusyOrbs.Clear();
		_sharedPhase = 0.0;
		foreach (Polygon2D value in DebugPolys.Values)
		{
			if (value != null && GodotObject.IsInstanceValid((GodotObject)(object)value))
			{
				((Node)value).QueueFree();
			}
		}
		DebugPolys.Clear();
		_orbScratch.Clear();
	}
}
[HarmonyPatch(typeof(NOrbManager), "TweenLayout")]
internal static class OrbManagerTweenLayoutPatch
{
	private static bool Prefix()
	{
		return false;
	}
}
internal static class OstyCombatUtil
{
	public const float HitboxScale = 0.55f;

	public const float DodgeChance = 0.8f;

	public static bool IsHostileMonster(Creature? entity)
	{
		return entity != null && entity.IsAlive && entity.IsMonster && !entity.IsPet;
	}

	public static bool TryGetAliveOsty(Player? player, out Creature osty, out NCreature node)
	{
		osty = null;
		node = null;
		if (player == null || !player.IsOstyAlive)
		{
			return false;
		}
		Creature osty2 = player.Osty;
		if (osty2 == null || !osty2.IsAlive)
		{
			return false;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(osty2) : null);
		if (val == null || !GodotObject.IsInstanceValid((GodotObject)(object)val))
		{
			return false;
		}
		osty = osty2;
		node = val;
		return true;
	}

	public static bool TryGetAliveOsty(ICombatState? state, out Creature osty, out NCreature node)
	{
		osty = null;
		node = null;
		if (state == null)
		{
			return false;
		}
		return TryGetAliveOsty(LocalContext.GetMe(state), out osty, out node);
	}

	public static Rect2 BodyRect(NCreature node, float scale = 1f)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		Vector2 size = ((Control)node).Size;
		if (size.X < 8f || size.Y < 8f)
		{
			((Vector2)(ref size))..ctor(80f, 120f);
		}
		Vector2 globalPosition = ((Control)node).GlobalPosition;
		float num = size.X * 0.8f * scale;
		float num2 = size.Y * 0.4f * scale;
		return new Rect2(new Vector2(globalPosition.X - num * 0.5f, globalPosition.Y - num2), new Vector2(num, num2));
	}

	public static Rect2 OstyBodyRect(NCreature node)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return BodyRect(node, 0.55f);
	}

	public static bool RollDodge()
	{
		return Random.Shared.NextDouble() < 0.800000011920929;
	}
}
internal static class OstyMoveSystem
{
	private const float ComfortMin = 160f;

	private const float ComfortMax = 280f;

	private static bool _hooked;

	public static Vector2 Velocity { get; private set; }

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
			}
		}
	}

	private static void OnProcessFrame()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_0255: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsOverOrEnding || PlayerChoicePause.IsPaused)
		{
			Velocity = Vector2.Zero;
			return;
		}
		CombatState val = instance.DebugOnlyGetState();
		if (val == null || !OstyCombatUtil.TryGetAliveOsty((ICombatState?)(object)val, out Creature _, out NCreature node))
		{
			Velocity = Vector2.Zero;
			return;
		}
		Player me = LocalContext.GetMe((ICombatState)(object)val);
		Creature val2 = ((me != null) ? me.Creature : null);
		if (val2 == null || !val2.IsAlive)
		{
			Velocity = Vector2.Zero;
			return;
		}
		NCombatRoom instance2 = NCombatRoom.Instance;
		if (instance2 == null)
		{
			Velocity = Vector2.Zero;
			return;
		}
		Vector2 globalPosition = ((Control)node).GlobalPosition;
		Vector2 val3 = Vector2.Zero;
		float num = float.MaxValue;
		foreach (NCreature creatureNode in instance2.CreatureNodes)
		{
			if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) && OstyCombatUtil.IsHostileMonster(creatureNode.Entity))
			{
				Vector2 val4 = ((Control)creatureNode).GlobalPosition - globalPosition;
				float num2 = ((Vector2)(ref val4)).LengthSquared();
				if (num2 < num && num2 > 0.01f)
				{
					num = num2;
					val3 = val4;
				}
			}
		}
		if (num >= float.MaxValue)
		{
			Velocity = Vector2.Zero;
			return;
		}
		float num3 = Mathf.Sqrt(num);
		Vector2 val5;
		if (num3 < 160f)
		{
			val5 = -((Vector2)(ref val3)).Normalized();
		}
		else
		{
			if (!(num3 > 280f))
			{
				Velocity = Vector2.Zero;
				return;
			}
			val5 = ((Vector2)(ref val3)).Normalized();
		}
		float num4 = (float)((Node)node).GetProcessDeltaTime();
		if (num4 <= 0f)
		{
			num4 = (float)((Node)node).GetPhysicsProcessDeltaTime();
		}
		float num5 = MoveSpeedUtil.ForCreature(val2, RealtimeCombatSettings.PlayerMoveSpeed);
		Velocity = val5 * num5;
		((Control)node).GlobalPosition = globalPosition + Velocity * num4;
		CombatScreenClamp.Clamp(node);
		CreatureFacing.FaceByMoveX(node, val5.X);
	}
}
[HarmonyPatch(typeof(Creature), "LoseHpInternal")]
internal static class OstyOverkillPatch
{
	private static void Postfix(Creature __instance, DamageResult __result)
	{
		if (__instance != null && __instance.IsPet && __result != null && __result.OverkillDamage > 0)
		{
			AccessTools.Property(typeof(DamageResult), "OverkillDamage")?.SetValue(__result, 0);
		}
	}
}
internal static class PlayCardBypassPatch
{
	[HarmonyPatch(typeof(CardModel), "EnqueueManualPlay")]
	private static class EnqueueManualPlayPatch
	{
		private static bool Prefix(CardModel __instance, Creature target)
		{
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Expected O, but got Unknown
			if (!ActionGameNet.IsOnline)
			{
				return true;
			}
			MethodInfo methodInfo = AccessTools.Method(typeof(CardModel), "OnEnqueuePlayVfx", (Type[])null, (Type[])null);
			if (methodInfo != null && methodInfo.Invoke(__instance, new object[1] { target }) is Task task)
			{
				TaskHelper.RunSafely(task);
			}
			RunManager instance = RunManager.Instance;
			ActionQueueSynchronizer val = ((instance != null) ? instance.ActionQueueSynchronizer : null);
			if (val == null)
			{
				return true;
			}
			PlayCardAction val2 = new PlayCardAction(__instance, target);
			object value = Traverse.Create((object)val).Field("_actionQueueSet").GetValue();
			if (value == null)
			{
				return true;
			}
			AccessTools.Method(value.GetType(), "EnqueueWithoutSynchronizing", new Type[1] { typeof(GameAction) }, (Type[])null)?.Invoke(value, new object[1] { val2 });
			BroadcastSoon(__instance, target);
			return false;
		}
	}

	[HarmonyPatch(typeof(CardCmd), "AutoPlay")]
	private static class AutoPlayBroadcastPatch
	{
		private static void Postfix(CardModel card, Creature target, Task __result)
		{
			if (ActionGameNet.IsOnline && !CardPlayNetSync.IsReplayingRemote)
			{
				AwaitThenBroadcast(__result, card, target);
			}
		}

		private static async Task AwaitThenBroadcast(Task task, CardModel card, Creature? target)
		{
			if (task != null)
			{
				await task;
			}
			await Task.Delay(80);
			if (!CardPlayNetSync.IsReplayingRemote && !CardAttackNetSync.SuppressBroadcast)
			{
				CardPlayNetSync.BroadcastLocalPlay(card, target);
			}
		}
	}

	private static async Task BroadcastSoon(CardModel card, Creature? target)
	{
		await Task.Delay(50);
		for (int i = 0; i < 40; i++)
		{
			await Task.Delay(50);
			RunManager instance = RunManager.Instance;
			ActionExecutor exec = ((instance != null) ? instance.ActionExecutor : null);
			if (exec == null || !exec.IsRunning)
			{
				break;
			}
		}
		await Task.Delay(80);
		if (!CardPlayNetSync.IsReplayingRemote && !CardAttackNetSync.SuppressBroadcast)
		{
			CardPlayNetSync.BroadcastLocalPlay(card, target);
		}
	}
}
internal static class PlayerChoicePause
{
	private static int _depth;

	public static bool IsChoosing => _depth > 0;

	public static bool IsPaused
	{
		get
		{
			if (ActionGameNet.IsOnline)
			{
				return false;
			}
			int result;
			if (!IsChoosing)
			{
				RunManager instance = RunManager.Instance;
				result = ((instance != null && instance.IsPaused) ? 1 : 0);
			}
			else
			{
				result = 1;
			}
			return (byte)result != 0;
		}
	}

	public static void Reset()
	{
		_depth = 0;
	}

	public static void Begin()
	{
		if (!ActionGameNet.IsOnline)
		{
			_depth++;
			if (_depth == 1)
			{
				Log.Info("[local.action_game] player choice begun -> pause combat clocks", 2);
			}
		}
	}

	public static void End()
	{
		if (!ActionGameNet.IsOnline && _depth > 0)
		{
			_depth--;
			if (_depth == 0)
			{
				Log.Info("[local.action_game] player choice ended -> resume combat clocks", 2);
			}
		}
	}
}
[HarmonyPatch]
internal static class PlayerChoiceBegunPausePatch
{
	[CompilerGenerated]
	private sealed class <TargetMethods>d__0 : IEnumerable<MethodBase>, IEnumerable, IEnumerator<MethodBase>, IEnumerator, IDisposable
	{
		private int <>1__state;

		private MethodBase <>2__current;

		private int <>l__initialThreadId;

		private Type[] <>s__1;

		private int <>s__2;

		private Type <type>5__3;

		private MethodInfo <m>5__4;

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
		public <TargetMethods>d__0(int <>1__state)
		{
			this.<>1__state = <>1__state;
			<>l__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			<>s__1 = null;
			<type>5__3 = null;
			<m>5__4 = null;
			<>1__state = -2;
		}

		private bool MoveNext()
		{
			int num = <>1__state;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				<>1__state = -1;
				goto IL_00de;
			}
			<>1__state = -1;
			<>s__1 = AccessTools.GetTypesFromAssembly(typeof(PlayerChoiceContext).Assembly);
			<>s__2 = 0;
			goto IL_00fb;
			IL_00ed:
			<>s__2++;
			goto IL_00fb;
			IL_00de:
			<m>5__4 = null;
			<type>5__3 = null;
			goto IL_00ed;
			IL_00fb:
			if (<>s__2 < <>s__1.Length)
			{
				<type>5__3 = <>s__1[<>s__2];
				if (<type>5__3 == null || <type>5__3.IsAbstract || !typeof(PlayerChoiceContext).IsAssignableFrom(<type>5__3))
				{
					goto IL_00ed;
				}
				<m>5__4 = AccessTools.DeclaredMethod(<type>5__3, "SignalPlayerChoiceBegun", (Type[])null, (Type[])null);
				if (<m>5__4 != null)
				{
					<>2__current = <m>5__4;
					<>1__state = 1;
					return true;
				}
				goto IL_00de;
			}
			<>s__1 = null;
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
		IEnumerator<MethodBase> IEnumerable<MethodBase>.GetEnumerator()
		{
			if (<>1__state == -2 && <>l__initialThreadId == Environment.CurrentManagedThreadId)
			{
				<>1__state = 0;
				return this;
			}
			return new <TargetMethods>d__0(0);
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<MethodBase>)this).GetEnumerator();
		}
	}

	[IteratorStateMachine(typeof(<TargetMethods>d__0))]
	private static IEnumerable<MethodBase> TargetMethods()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new <TargetMethods>d__0(-2);
	}

	private static void Prefix()
	{
		if (!RealtimeCombatSettings.AutoCardSelect)
		{
			PlayerChoicePause.Begin();
		}
	}
}
[HarmonyPatch]
internal static class PlayerChoiceEndedPausePatch
{
	[CompilerGenerated]
	private sealed class <TargetMethods>d__0 : IEnumerable<MethodBase>, IEnumerable, IEnumerator<MethodBase>, IEnumerator, IDisposable
	{
		private int <>1__state;

		private MethodBase <>2__current;

		private int <>l__initialThreadId;

		private Type[] <>s__1;

		private int <>s__2;

		private Type <type>5__3;

		private MethodInfo <m>5__4;

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
		public <TargetMethods>d__0(int <>1__state)
		{
			this.<>1__state = <>1__state;
			<>l__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			<>s__1 = null;
			<type>5__3 = null;
			<m>5__4 = null;
			<>1__state = -2;
		}

		private bool MoveNext()
		{
			int num = <>1__state;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				<>1__state = -1;
				goto IL_00de;
			}
			<>1__state = -1;
			<>s__1 = AccessTools.GetTypesFromAssembly(typeof(PlayerChoiceContext).Assembly);
			<>s__2 = 0;
			goto IL_00fb;
			IL_00ed:
			<>s__2++;
			goto IL_00fb;
			IL_00de:
			<m>5__4 = null;
			<type>5__3 = null;
			goto IL_00ed;
			IL_00fb:
			if (<>s__2 < <>s__1.Length)
			{
				<type>5__3 = <>s__1[<>s__2];
				if (<type>5__3 == null || <type>5__3.IsAbstract || !typeof(PlayerChoiceContext).IsAssignableFrom(<type>5__3))
				{
					goto IL_00ed;
				}
				<m>5__4 = AccessTools.DeclaredMethod(<type>5__3, "SignalPlayerChoiceEnded", (Type[])null, (Type[])null);
				if (<m>5__4 != null)
				{
					<>2__current = <m>5__4;
					<>1__state = 1;
					return true;
				}
				goto IL_00de;
			}
			<>s__1 = null;
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
		IEnumerator<MethodBase> IEnumerable<MethodBase>.GetEnumerator()
		{
			if (<>1__state == -2 && <>l__initialThreadId == Environment.CurrentManagedThreadId)
			{
				<>1__state = 0;
				return this;
			}
			return new <TargetMethods>d__0(0);
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<MethodBase>)this).GetEnumerator();
		}
	}

	[IteratorStateMachine(typeof(<TargetMethods>d__0))]
	private static IEnumerable<MethodBase> TargetMethods()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new <TargetMethods>d__0(-2);
	}

	private static void Prefix()
	{
		if (!RealtimeCombatSettings.AutoCardSelect)
		{
			PlayerChoicePause.End();
		}
	}
}
internal static class PlayerPoseSync
{
	private sealed class RemotePose
	{
		public Vector2 Target;

		public Vector2 Display;

		public float Age;

		public bool HasDisplay;
	}

	private const float SendHz = 20f;

	private const float SendInterval = 0.05f;

	private const float InterpSpeed = 18f;

	private const float SnapDist = 220f;

	private static readonly Dictionary<ulong, RemotePose> Remotes = new Dictionary<ulong, RemotePose>();

	private static float _sendAcc;

	private static uint _tick;

	private static bool _frameHooked;

	public static void EnsureHook()
	{
		if (!_frameHooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_frameHooked = true;
			}
		}
	}

	public static void OnCombatStarted()
	{
		Remotes.Clear();
		_sendAcc = 0f;
		_tick = 0u;
		EnsureHook();
	}

	public static void OnCombatEnded()
	{
		Remotes.Clear();
		_sendAcc = 0f;
	}

	public static bool TryGetRemotePose(ulong netId, out Vector2 pos)
	{
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		if (Remotes.TryGetValue(netId, out RemotePose value) && value.HasDisplay)
		{
			pos = value.Display;
			return true;
		}
		pos = default(Vector2);
		return false;
	}

	public static void OnRemotePose(ActionGameNetMessage msg)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		if (!Remotes.TryGetValue(msg.CasterNetId, out RemotePose value))
		{
			value = new RemotePose();
			Remotes[msg.CasterNetId] = value;
		}
		value.Target = new Vector2(msg.X, msg.Y);
		value.Age = 0f;
		if (!value.HasDisplay)
		{
			value.Display = value.Target;
			value.HasDisplay = true;
		}
		ApplyToNode(msg.CasterNetId, value.Display);
	}

	private static void OnProcessFrame()
	{
		if (!ActionGameNet.Active || PlayerChoicePause.IsPaused)
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			float num = (float)((Node)instance).GetProcessDeltaTime();
			if (num <= 0f)
			{
				num = 1f / 60f;
			}
			TickRemotes(num);
			_sendAcc += num;
			if (!(_sendAcc < 0.05f))
			{
				_sendAcc = 0f;
				_tick++;
				SendLocalPose();
			}
		}
	}

	private static void TickRemotes(float delta)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		foreach (KeyValuePair<ulong, RemotePose> remote in Remotes)
		{
			RemotePose value = remote.Value;
			value.Age += delta;
			if (value.HasDisplay)
			{
				Vector2 val = value.Target - value.Display;
				float num = ((Vector2)(ref val)).Length();
				if (num > 220f)
				{
					value.Display = value.Target;
				}
				else if (num > 0.5f)
				{
					value.Display += val * Mathf.Clamp(18f * delta, 0f, 1f);
				}
				else
				{
					value.Display = value.Target;
				}
				ApplyToNode(remote.Key, value.Display);
			}
		}
	}

	private static void SendLocalPose()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		CombatManager instance = CombatManager.Instance;
		CombatState val = ((instance != null) ? instance.DebugOnlyGetState() : null);
		Player val2 = ((val != null) ? LocalContext.GetMe((ICombatState)(object)val) : null);
		NCreature val3 = ((val2 != null) ? CombatPeerUtil.GetPlayerNode(val2) : null);
		if (val3 != null && GodotObject.IsInstanceValid((GodotObject)(object)val3))
		{
			Vector2 globalPosition = ((Control)val3).GlobalPosition;
			ActionGameNet.Send(new ActionGameNetMessage
			{
				Opcode = 1,
				CasterNetId = ActionGameNet.LocalNetId,
				X = globalPosition.X,
				Y = globalPosition.Y,
				Tick = _tick
			});
		}
	}

	private static void ApplyToNode(ulong netId, Vector2 pos)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		Player val = CombatPeerUtil.FindPlayer(netId);
		NCreature val2 = ((val != null) ? CombatPeerUtil.GetPlayerNode(val) : null);
		if (val2 != null && GodotObject.IsInstanceValid((GodotObject)(object)val2) && !LocalContext.IsMe(val))
		{
			((Control)val2).GlobalPosition = pos;
			CombatScreenClamp.Clamp(val2);
		}
	}
}
internal static class PlayerStatsNetSync
{
	private const float SendInterval = 0.25f;

	private static float _acc;

	private static bool _hooked;

	private static readonly Dictionary<ulong, (int hp, int maxHp, int block)> Remote = new Dictionary<ulong, (int, int, int)>();

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
			}
		}
	}

	public static void OnCombatStarted()
	{
		Remote.Clear();
		_acc = 0f;
		EnsureHook();
	}

	public static void OnCombatEnded()
	{
		Remote.Clear();
		_acc = 0f;
	}

	public static bool TryGetRemote(ulong netId, out int hp, out int maxHp, out int block)
	{
		if (!Remote.TryGetValue(netId, out (int, int, int) value))
		{
			hp = (maxHp = (block = 0));
			return false;
		}
		(hp, maxHp, block) = value;
		return true;
	}

	public static void OnRemote(ActionGameNetMessage msg)
	{
		Remote[msg.CasterNetId] = ((int)msg.Amount, msg.IntA, msg.IntB);
		Player val = CombatPeerUtil.FindPlayer(msg.CasterNetId);
		Creature val2 = ((val != null) ? val.Creature : null);
		if (val2 != null && !LocalContext.IsMe(val))
		{
			int num = Math.Clamp((int)msg.Amount, 0, Math.Max(1, msg.IntA));
			if (val2.CurrentHp != num)
			{
				CreatureCmd.SetCurrentHp(val2, (decimal)num);
			}
		}
	}

	private static void OnProcessFrame()
	{
		if (!ActionGameNet.Active)
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		float num = ((instance != null && GodotObject.IsInstanceValid((GodotObject)(object)instance)) ? ((float)((Node)instance).GetProcessDeltaTime()) : (1f / 60f));
		_acc += num;
		if (!(_acc < 0.25f))
		{
			_acc = 0f;
			CombatManager instance2 = CombatManager.Instance;
			CombatState val = ((instance2 != null) ? instance2.DebugOnlyGetState() : null);
			Player val2 = ((val != null) ? LocalContext.GetMe((ICombatState)(object)val) : null);
			Creature val3 = ((val2 != null) ? val2.Creature : null);
			if (val3 != null)
			{
				ActionGameNet.Send(new ActionGameNetMessage
				{
					Opcode = 3,
					CasterNetId = ActionGameNet.LocalNetId,
					Amount = val3.CurrentHp,
					IntA = val3.MaxHp,
					IntB = val3.Block
				});
			}
		}
	}
}
internal static class PlayerWasdMoveSystem
{
	private const float StickDeadzone = 0.22f;

	private static bool _inputHooked;

	public static Vector2 Velocity { get; private set; }

	public static void EnsureInputHook()
	{
		if (!_inputHooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_inputHooked = true;
			}
		}
	}

	private static void OnProcessFrame()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsOverOrEnding || PlayerChoicePause.IsPaused)
		{
			Velocity = Vector2.Zero;
			return;
		}
		Vector2 val = ReadMoveAxis();
		if (val == Vector2.Zero)
		{
			Velocity = Vector2.Zero;
			return;
		}
		NCreature val2 = FindLocalPlayerNode();
		if (val2 != null && GodotObject.IsInstanceValid((GodotObject)(object)val2))
		{
			float num = (float)((Node)val2).GetProcessDeltaTime();
			if (num <= 0f)
			{
				num = (float)((Node)val2).GetPhysicsProcessDeltaTime();
			}
			float num2 = MoveSpeedUtil.ForCreature(val2.Entity, RealtimeCombatSettings.PlayerMoveSpeed);
			Velocity = ((((Vector2)(ref val)).LengthSquared() > 1f) ? (((Vector2)(ref val)).Normalized() * num2) : (val * num2));
			Vector2 val3 = Velocity * num;
			((Control)val2).Position = ((Control)val2).Position + val3;
			CombatScreenClamp.Clamp(val2);
			CreatureFacing.FaceByMoveX(val2, val.X);
		}
	}

	private static NCreature? FindLocalPlayerNode()
	{
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return null;
		}
		CombatManager instance2 = CombatManager.Instance;
		CombatState val = ((instance2 != null) ? instance2.DebugOnlyGetState() : null);
		if (val == null)
		{
			return null;
		}
		Player me = LocalContext.GetMe((ICombatState)(object)val);
		Creature val2 = ((me != null) ? me.Creature : null);
		if (val2 == null)
		{
			return null;
		}
		return instance.GetCreatureNode(val2);
	}

	public static Vector2 ReadMoveAxis()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = Vector2.Zero;
		if (KeyHeld((Key)87))
		{
			val.Y -= 1f;
		}
		if (KeyHeld((Key)83))
		{
			val.Y += 1f;
		}
		if (KeyHeld((Key)65))
		{
			val.X -= 1f;
		}
		if (KeyHeld((Key)68))
		{
			val.X += 1f;
		}
		if (val != Vector2.Zero)
		{
			val = ((Vector2)(ref val)).Normalized();
		}
		val += ReadLeftStick();
		if (((Vector2)(ref val)).LengthSquared() > 1f)
		{
			val = ((Vector2)(ref val)).Normalized();
		}
		return val;
	}

	public static bool IsManualPlayHeld()
	{
		if (KeyHeld((Key)74))
		{
			return true;
		}
		if (!IsLiveCombatPlay())
		{
			return false;
		}
		if (ActionHeld(Controller.faceButtonSouth) || ActionHeld(MegaInput.confirm))
		{
			return true;
		}
		foreach (int connectedJoypad in Input.GetConnectedJoypads())
		{
			if (Input.IsJoyButtonPressed(connectedJoypad, (JoyButton)0))
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsLiveCombatPlay()
	{
		CombatManager instance = CombatManager.Instance;
		return instance != null && instance.IsInProgress && !instance.IsOverOrEnding && !PlayerChoicePause.IsPaused;
	}

	private static bool KeyHeld(Key key)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return Input.IsPhysicalKeyPressed(key) || Input.IsKeyPressed(key);
	}

	private static bool ActionHeld(StringName action)
	{
		return action != (StringName)null && InputMap.HasAction(action) && Input.IsActionPressed(action, false);
	}

	private static Vector2 ReadLeftStick()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		Vector2 a = Stronger(Vector2.Zero, ReadManagerStick());
		a = Stronger(a, StickVector(Controller.lStickLeft, Controller.lStickRight, Controller.lStickUp, Controller.lStickDown));
		a = Stronger(a, StickVector(StringName.op_Implicit("raw_l_stick_left"), StringName.op_Implicit("raw_l_stick_right"), StringName.op_Implicit("raw_l_stick_up"), StringName.op_Implicit("raw_l_stick_down")));
		a = Stronger(a, ReadDigitalStick());
		a = Stronger(a, ReadGodotStick());
		return ScaleStick(a);
	}

	private static Vector2 ReadManagerStick()
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		NControllerManager instance = NControllerManager.Instance;
		if (instance == null)
		{
			return Vector2.Zero;
		}
		return instance.GetLeftAnalogStickDirection();
	}

	private static Vector2 StickVector(StringName left, StringName right, StringName up, StringName down)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		if (left == (StringName)null || right == (StringName)null || up == (StringName)null || down == (StringName)null)
		{
			return Vector2.Zero;
		}
		if (!InputMap.HasAction(left) || !InputMap.HasAction(right) || !InputMap.HasAction(up) || !InputMap.HasAction(down))
		{
			return Vector2.Zero;
		}
		return Input.GetVector(left, right, up, down, -1f);
	}

	private static Vector2 ReadDigitalStick()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		Vector2 zero = Vector2.Zero;
		if (ActionHeld(MegaInput.left) || ActionHeld(Controller.dPadLeft))
		{
			zero.X -= 1f;
		}
		if (ActionHeld(MegaInput.right) || ActionHeld(Controller.dPadRight))
		{
			zero.X += 1f;
		}
		if (ActionHeld(MegaInput.up) || ActionHeld(Controller.dPadUp))
		{
			zero.Y -= 1f;
		}
		if (ActionHeld(MegaInput.down) || ActionHeld(Controller.dPadDown))
		{
			zero.Y += 1f;
		}
		return (zero == Vector2.Zero) ? zero : ((Vector2)(ref zero)).Normalized();
	}

	private static Vector2 ReadGodotStick()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		Vector2 result = Vector2.Zero;
		float num = 0f;
		Vector2 val = default(Vector2);
		foreach (int connectedJoypad in Input.GetConnectedJoypads())
		{
			((Vector2)(ref val))..ctor(Input.GetJoyAxis(connectedJoypad, (JoyAxis)0), Input.GetJoyAxis(connectedJoypad, (JoyAxis)1));
			float num2 = ((Vector2)(ref val)).Length();
			if (!(num2 < 0.22f) && !(num2 <= num))
			{
				num = num2;
				result = val;
			}
		}
		return result;
	}

	private static Vector2 Stronger(Vector2 a, Vector2 b)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		return (((Vector2)(ref b)).LengthSquared() > ((Vector2)(ref a)).LengthSquared()) ? b : a;
	}

	private static Vector2 ScaleStick(Vector2 stick)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		float num = ((Vector2)(ref stick)).Length();
		if (num < 0.22f)
		{
			return Vector2.Zero;
		}
		float num2 = (num - 0.22f) / 0.78f;
		return stick * (Mathf.Clamp(num2, 0f, 1f) / num);
	}
}
[HarmonyPatch(typeof(Hook), "AfterDeath")]
internal static class PoisonDeathSpreadPatch
{
	private static void Postfix(ICombatState combatState, Creature creature, bool wasRemovalPrevented, ref Task __result)
	{
		__result = ContinueAfterDeath(__result, combatState, creature, wasRemovalPrevented);
	}

	private static async Task ContinueAfterDeath(Task original, ICombatState combatState, Creature creature, bool wasRemovalPrevented)
	{
		if (original != null)
		{
			await original;
		}
		if (wasRemovalPrevented || combatState == null || creature == null || !creature.IsMonster || creature.IsPet)
		{
			return;
		}
		int poisonAmount = creature.GetPowerAmount<PoisonPower>();
		if (poisonAmount <= 0)
		{
			return;
		}
		List<Creature> targets = null;
		foreach (Creature enemy in combatState.Enemies)
		{
			if (enemy != null && enemy != creature && enemy.IsAlive && enemy.IsMonster && !enemy.IsPet && enemy.CanReceivePowers)
			{
				if (targets == null)
				{
					targets = new List<Creature>();
				}
				targets.Add(enemy);
			}
		}
		if (targets != null && targets.Count != 0)
		{
			PoisonPower poison = creature.GetPower<PoisonPower>();
			Creature applier = ((poison != null) ? ((PowerModel)poison).Applier : null);
			if (applier == null || !applier.IsAlive)
			{
				applier = creature;
			}
			BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
			await PowerCmd.Apply<PoisonPower>((PlayerChoiceContext)(object)ctx, (IEnumerable<Creature>)targets, (decimal)poisonAmount, applier, (CardModel)null, false);
			Log.Info($"[{"local.action_game"}] poison spread on death: {creature.LogName} x{poisonAmount} -> {targets.Count} enemies", 2);
		}
	}
}
[HarmonyPatch(typeof(Cmd), "CustomScaledWait", new Type[]
{
	typeof(float),
	typeof(float),
	typeof(bool),
	typeof(CancellationToken)
})]
public static class PowerApplyWaitSpeedPatch
{
	private const float PowerApplyFast = 0.1f;

	private const float PowerApplyStandard = 0.25f;

	private const float SpeedMul = 0.3f;

	private static void Prefix(ref float fastSeconds, ref float standardSeconds)
	{
		if (!(Math.Abs(fastSeconds - 0.1f) > 0.001f) && !(Math.Abs(standardSeconds - 0.25f) > 0.001f))
		{
			fastSeconds *= 0.3f;
			standardSeconds *= 0.3f;
		}
	}
}
internal static class ProximityDamage
{
	public static decimal Multiplier(Creature dealer, Creature target)
	{
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null)
		{
			return 1m;
		}
		NCreature creatureNode = instance.GetCreatureNode(dealer);
		NCreature creatureNode2 = instance.GetCreatureNode(target);
		if (creatureNode == null || creatureNode2 == null || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) || !GodotObject.IsInstanceValid((GodotObject)(object)creatureNode2))
		{
			return 1m;
		}
		float proximityCloseDist = RealtimeCombatSettings.ProximityCloseDist;
		float proximityFarDist = RealtimeCombatSettings.ProximityFarDist;
		decimal proximityMaxMultiplier = RealtimeCombatSettings.ProximityMaxMultiplier;
		Vector2 globalPosition = ((Control)creatureNode).GlobalPosition;
		float num = ((Vector2)(ref globalPosition)).DistanceTo(((Control)creatureNode2).GlobalPosition);
		if (num <= proximityCloseDist)
		{
			return proximityMaxMultiplier;
		}
		if (num >= proximityFarDist)
		{
			return 1m;
		}
		float num2 = (num - proximityCloseDist) / (proximityFarDist - proximityCloseDist);
		return proximityMaxMultiplier - (decimal)num2 * (proximityMaxMultiplier - 1m);
	}
}
[HarmonyPatch(typeof(Hook), "ModifyDamage")]
internal static class ProximityDamagePatch
{
	private static void Postfix(Creature target, Creature dealer, CardModel cardSource, ModifyDamageHookType modifyDamageHookType, ref decimal __result)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Invalid comparison between Unknown and I4
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Invalid comparison between Unknown and I4
		if (__result <= 0m || (modifyDamageHookType & 4) == 0 || cardSource == null || (int)cardSource.Type != 1 || (int)cardSource.TargetType != 2 || dealer == null || OstyCombatUtil.IsHostileMonster(dealer) || target == null || !OstyCombatUtil.IsHostileMonster(target))
		{
			return;
		}
		decimal num = ProximityDamage.Multiplier(dealer, target);
		if (!(num <= 1m))
		{
			__result = decimal.Round(__result * num, MidpointRounding.AwayFromZero);
			if (target != null && dealer != null)
			{
				EnemyChaseSystem.PushFrom(target, dealer);
			}
		}
	}
}
internal static class QuickPlaySystem
{
	private static bool _tabWasDown;

	private static bool _jWasDown;

	private static bool _inputHooked;

	private static bool _busy;

	private static float _cooldown;

	private const float PlayGapSec = 0.2f;

	private const float AutoPlayWaitScale = 0.5f;

	internal static float AutoPlayWaitMultiplier => 0.5f;

	public static void EnsureInputHook()
	{
		if (!_inputHooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_inputHooked = true;
			}
		}
	}

	private static void OnProcessFrame()
	{
		bool flag = Input.IsPhysicalKeyPressed((Key)4194306);
		if (flag && !_tabWasDown)
		{
			QuickPlayMode quickPlayMode = RealtimeCombatSettings.QuickPlayMode;
			if (1 == 0)
			{
			}
			QuickPlayMode quickPlayMode2 = quickPlayMode switch
			{
				QuickPlayMode.Off => QuickPlayMode.All, 
				QuickPlayMode.All => QuickPlayMode.BlockOnly, 
				_ => QuickPlayMode.Off, 
			};
			if (1 == 0)
			{
			}
			RealtimeCombatSettings.QuickPlayMode = quickPlayMode2;
			TurnLengthHud.RefreshQuickPlayFromHotkey();
			Log.Info($"[{"local.action_game"}] quick play={RealtimeCombatSettings.QuickPlayMode} (Tab)", 2);
		}
		_tabWasDown = flag;
		bool flag2 = PlayerWasdMoveSystem.IsManualPlayHeld();
		if (flag2 && !_jWasDown)
		{
			TryManualAutoPlay();
		}
		_jWasDown = flag2;
		PinCornerCards();
		TickAutoPlay();
	}

	private static void PinCornerCards()
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		NCombatUi val = ((instance != null) ? instance.Ui : null);
		if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
		{
			NGame instance2 = NGame.Instance;
			Vector2 val2;
			if (instance2 == null)
			{
				val2 = Vector2.Zero;
			}
			else
			{
				Rect2 viewportRect = ((CanvasItem)instance2).GetViewportRect();
				val2 = ((Rect2)(ref viewportRect)).Size;
			}
			Vector2 val3 = val2;
			if (!(val3.X < 100f))
			{
				int index = 0;
				index = PinCards((Node)(object)val.PlayContainer, val3, index);
				index = PinCards((Node)(object)val.CardPreviewContainer, val3, index);
				PinCards((Node)(object)val.MessyCardPreviewContainer, val3, index);
			}
		}
	}

	private static int PinCards(Node parent, Vector2 viewSize, int index)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		if (parent == null || !GodotObject.IsInstanceValid((GodotObject)(object)parent))
		{
			return index;
		}
		foreach (Node child in parent.GetChildren(false))
		{
			NCard val = (NCard)(object)((child is NCard) ? child : null);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val) && ((CanvasItem)val).Visible)
			{
				Vector2 val2 = ((Control)val).Size * new Vector2(Mathf.Abs(((Control)val).Scale.X), Mathf.Abs(((Control)val).Scale.Y));
				if (val2.X < 8f)
				{
					((Vector2)(ref val2))..ctor(200f, 280f);
				}
				float num = (float)index * 36f;
				((Control)val).GlobalPosition = new Vector2(viewSize.X - val2.X - 36f - num, viewSize.Y - val2.Y - 28f);
				index++;
			}
		}
		return index;
	}

	private static void TickAutoPlay()
	{
		if (_busy || !RealtimeCombatSettings.AutoPlayCards || RealtimeCombatSettings.QuickPlayMode == QuickPlayMode.Off || PlayerChoicePause.IsPaused)
		{
			return;
		}
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsOverOrEnding)
		{
			_cooldown = 0f;
			return;
		}
		NCombatRoom instance2 = NCombatRoom.Instance;
		float num = ((instance2 != null && GodotObject.IsInstanceValid((GodotObject)(object)instance2)) ? ((float)((Node)instance2).GetProcessDeltaTime()) : (1f / 60f));
		if (num <= 0f)
		{
			num = 1f / 60f;
		}
		_cooldown -= num;
		if (!(_cooldown > 0f))
		{
			_cooldown = 0.2f;
			PlayOneAsync(RealtimeCombatSettings.QuickPlayMode);
		}
	}

	private static void TryManualAutoPlay()
	{
		if (PlayerChoicePause.IsPaused)
		{
			Log.Info("[local.action_game] J ignored (paused)", 2);
			return;
		}
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsOverOrEnding)
		{
			Log.Info("[local.action_game] J ignored (not in combat)", 2);
			return;
		}
		CombatState val = instance.DebugOnlyGetState();
		Player val2 = ((val != null) ? LocalContext.GetMe((ICombatState)(object)val) : null);
		if (val2 == null)
		{
			Log.Info("[local.action_game] J ignored (no local player)", 2);
			return;
		}
		CardPile pile = PileTypeExtensions.GetPile((PileType)2, val2);
		CardModel val3 = null;
		Creature val4 = null;
		foreach (CardModel card in pile.Cards)
		{
			if (card != null && card.CanPlay())
			{
				Creature val5 = ResolveTarget(card);
				if (card.CanPlayTargeting(val5))
				{
					val3 = card;
					val4 = val5;
					break;
				}
			}
		}
		if (val3 == null)
		{
			Log.Info("[local.action_game] J: no playable card in hand", 2);
			return;
		}
		Log.Info($"[{"local.action_game"}] J play {((AbstractModel)val3).Id}", 2);
		val3.TryManualPlay(val4);
	}

	private static async Task PlayOneAsync(QuickPlayMode mode)
	{
		if (_busy)
		{
			return;
		}
		_busy = true;
		try
		{
			CombatManager manager = CombatManager.Instance;
			if (manager == null || !manager.IsInProgress || manager.IsOverOrEnding)
			{
				return;
			}
			CombatState state = manager.DebugOnlyGetState();
			Player player = ((state != null) ? LocalContext.GetMe((ICombatState)(object)state) : null);
			if (player != null)
			{
				CardPile hand = PileTypeExtensions.GetPile((PileType)2, player);
				if (TryPickAutoPlay(hand, mode, out CardModel card, out Creature target))
				{
					await card.SpendResources();
					await CardCmd.AutoPlay((PlayerChoiceContext)new BlockingPlayerChoiceContext(), card, target, (AutoPlayType)1, true, false);
					Log.Info($"[{"local.action_game"}] auto played {((AbstractModel)card).Id}", 2);
				}
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Log.Warn("[local.action_game] auto play failed: " + ex.GetBaseException().Message, 2);
		}
		finally
		{
			_busy = false;
		}
	}

	private static bool TryPickAutoPlay(CardPile hand, QuickPlayMode mode, out CardModel card, out Creature? target)
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		card = null;
		target = null;
		if (((hand != null) ? hand.Cards : null) == null)
		{
			return false;
		}
		if (RealtimeCombatSettings.SaveEnergyForLeftmost)
		{
			bool flag = false;
			UnplayableReason reason = default(UnplayableReason);
			AbstractModel val = default(AbstractModel);
			foreach (CardModel card2 in hand.Cards)
			{
				if (card2 == null || (mode == QuickPlayMode.BlockOnly && !card2.GainsBlock))
				{
					continue;
				}
				bool flag2 = card2.CanPlay(ref reason, ref val);
				if (!flag2 && IsOnlyWaitingOnEnergy(reason))
				{
					flag = true;
					break;
				}
				if (flag2)
				{
					Creature val2 = ResolveTarget(card2);
					if (card2.CanPlayTargeting(val2))
					{
						card = card2;
						target = val2;
						return true;
					}
					break;
				}
				break;
			}
			if (flag)
			{
				foreach (CardModel card3 in hand.Cards)
				{
					if (card3 != null && EnergyToSpend(card3) <= 0 && (mode != QuickPlayMode.BlockOnly || card3.GainsBlock) && card3.CanPlay())
					{
						Creature val3 = ResolveTarget(card3);
						if (card3.CanPlayTargeting(val3))
						{
							card = card3;
							target = val3;
							return true;
						}
					}
				}
				return false;
			}
		}
		foreach (CardModel card4 in hand.Cards)
		{
			if (card4 != null && card4.CanPlay() && (mode != QuickPlayMode.BlockOnly || card4.GainsBlock))
			{
				Creature val4 = ResolveTarget(card4);
				if (card4.CanPlayTargeting(val4))
				{
					card = card4;
					target = val4;
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsOnlyWaitingOnEnergy(UnplayableReason reason)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		return (int)reason != 0 && (reason & -17) == 0;
	}

	private static int EnergyToSpend(CardModel card)
	{
		CardEnergyCost energyCost = card.EnergyCost;
		if (energyCost == null || energyCost.CostsX)
		{
			return int.MaxValue;
		}
		return Math.Max(0, energyCost.GetAmountToSpend());
	}

	public static bool TryQuickPlay(NHandCardHolder holder)
	{
		QuickPlayMode quickPlayMode = RealtimeCombatSettings.QuickPlayMode;
		if (quickPlayMode == QuickPlayMode.Off)
		{
			return true;
		}
		CardModel cardModel = ((NCardHolder)holder).CardModel;
		if (cardModel == null)
		{
			return true;
		}
		if (quickPlayMode == QuickPlayMode.BlockOnly && !cardModel.GainsBlock)
		{
			return true;
		}
		Creature val = ResolveTarget(cardModel);
		if (!cardModel.CanPlayTargeting(val) || !cardModel.TryManualPlay(val))
		{
			return true;
		}
		return false;
	}

	private static Creature? ResolveTarget(CardModel card)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between Unknown and I4
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Invalid comparison between Unknown and I4
		CardModel card2 = card;
		TargetType targetType = card2.TargetType;
		if (1 == 0)
		{
		}
		Creature result = (((int)targetType == 2) ? PickNearest(AttackOrigin(card2), (Creature c) => c.IsAlive && c.Side != card2.Owner.Creature.Side) : (((int)targetType != 6) ? null : PickNearest(AttackOrigin(card2), (Creature c) => c.IsAlive && c.Side == card2.Owner.Creature.Side)));
		if (1 == 0)
		{
		}
		return result;
	}

	private static bool IsOstyAttack(CardModel card)
	{
		return card.DynamicVars != null && card.DynamicVars.ContainsKey("OstyDamage");
	}

	private static Vector2? AttackOrigin(CardModel card)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		CombatManager instance2 = CombatManager.Instance;
		CombatState val = ((instance2 != null) ? instance2.DebugOnlyGetState() : null);
		if (instance == null || val == null)
		{
			return null;
		}
		Player me = LocalContext.GetMe((ICombatState)(object)val);
		if (IsOstyAttack(card) && OstyCombatUtil.TryGetAliveOsty(me, out Creature _, out NCreature node))
		{
			return ((Control)node).GlobalPosition;
		}
		NCreature val2 = ((((me != null) ? me.Creature : null) != null) ? instance.GetCreatureNode(me.Creature) : null);
		if (val2 != null && GodotObject.IsInstanceValid((GodotObject)(object)val2))
		{
			return ((Control)val2).GlobalPosition;
		}
		return null;
	}

	private static Creature? PickNearest(Vector2? origin, Func<Creature, bool> pred)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		CombatManager instance2 = CombatManager.Instance;
		CombatState val = ((instance2 != null) ? instance2.DebugOnlyGetState() : null);
		if (instance == null || !origin.HasValue)
		{
			return (val != null) ? val.Creatures.Where(pred).FirstOrDefault() : null;
		}
		Creature val2 = null;
		float num = float.MaxValue;
		foreach (NCreature creatureNode in instance.CreatureNodes)
		{
			if (((creatureNode != null) ? creatureNode.Entity : null) != null && pred(creatureNode.Entity))
			{
				Vector2 globalPosition = ((Control)creatureNode).GlobalPosition;
				float num2 = ((Vector2)(ref globalPosition)).DistanceSquaredTo(origin.Value);
				if (num2 < num)
				{
					num = num2;
					val2 = creatureNode.Entity;
				}
			}
		}
		return val2 ?? ((val != null) ? val.Creatures.Where(pred).FirstOrDefault() : null);
	}

	internal static bool ShouldSpeedAutoPlayWaits()
	{
		return _busy || (RealtimeCombatSettings.AutoPlayCards && RealtimeCombatSettings.QuickPlayMode != QuickPlayMode.Off);
	}
}
[HarmonyPatch(typeof(Cmd), "Wait", new Type[]
{
	typeof(float),
	typeof(CancellationToken),
	typeof(bool)
})]
internal static class AutoPlayWaitSpeedPatch
{
	private static void Prefix(ref float seconds)
	{
		if (QuickPlaySystem.ShouldSpeedAutoPlayWaits())
		{
			seconds *= QuickPlaySystem.AutoPlayWaitMultiplier;
		}
	}
}
[HarmonyPatch(typeof(NPlayerHand), "StartCardPlay")]
internal static class QuickPlayStartCardPlayPatch
{
	private static bool Prefix(NHandCardHolder holder)
	{
		return QuickPlaySystem.TryQuickPlay(holder);
	}
}
[HarmonyPatch(typeof(PileTypeExtensions), "GetTargetPosition")]
internal static class PlayedCardCornerPatch
{
	private static void Postfix(PileType pileType, NCard node, ref Vector2 __result)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Invalid comparison between Unknown and I4
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		if (((int)pileType != 5 && (int)pileType != 2) || node == null || !GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			return;
		}
		NGame instance = NGame.Instance;
		Vector2 val;
		if (instance == null)
		{
			val = Vector2.Zero;
		}
		else
		{
			Rect2 viewportRect = ((CanvasItem)instance).GetViewportRect();
			val = ((Rect2)(ref viewportRect)).Size;
		}
		Vector2 val2 = val;
		if (!(val2.X < 100f))
		{
			Vector2 val3 = ((Control)node).Size * (((int)pileType == 5) ? 0.8f : 1f);
			if (val3.X < 8f)
			{
				((Vector2)(ref val3))..ctor(200f, 280f);
			}
			__result = new Vector2(val2.X - val3.X - 36f, val2.Y - val3.Y - 28f);
		}
	}
}
[HarmonyPatch(typeof(NDamageNumVfx), "_Ready")]
internal static class DamageNumHalfScalePatch
{
	private static void Postfix(NDamageNumVfx __instance)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		Control nodeOrNull = ((Node)__instance).GetNodeOrNull<Control>(NodePath.op_Implicit("Label"));
		if (nodeOrNull != null && nodeOrNull.Scale.X > 0.75f)
		{
			nodeOrNull.Scale *= 0.5f;
		}
	}
}
[HarmonyPatch(typeof(NCardPlay), "CenterCard")]
internal static class CenterCardCornerPatch
{
	private static void Postfix(NCardPlay __instance)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		NHandCardHolder holder = __instance.Holder;
		if (holder == null || !GodotObject.IsInstanceValid((GodotObject)(object)holder))
		{
			return;
		}
		Viewport viewport = ((Node)holder).GetViewport();
		Vector2 val;
		if (viewport == null)
		{
			val = Vector2.Zero;
		}
		else
		{
			Rect2 visibleRect = viewport.GetVisibleRect();
			val = ((Rect2)(ref visibleRect)).Size;
		}
		Vector2 val2 = val;
		if (!(val2.X < 100f))
		{
			Vector2 val3 = ((Control)holder).Size * 0.75f;
			if (val3.X < 8f)
			{
				((Vector2)(ref val3))..ctor(180f, 250f);
			}
			holder.SetTargetPosition(new Vector2(val2.X - val3.X * 0.5f - 36f, val2.Y - val3.Y * 0.5f - 28f));
		}
	}
}
internal enum QuickPlayMode
{
	All,
	BlockOnly,
	Off
}
internal enum GameDifficulty
{
	Easy,
	Normal,
	Hard
}
internal static class RealtimeCombatSettings
{
	public const double DefaultTurnLengthSec = 4.0;

	public const double MinTurnLengthSec = 2.5;

	public const double MaxTurnLengthSec = 12.0;

	private const double DrawPerTurn = 0.1075;

	private const double EnergyPerTurn = 0.155;

	private const double EasyPaceIntervalMul = 0.85;

	private const double NormalPaceIntervalMul = 0.9;

	private static double _turnLengthSec = 4.0;

	private static bool _autoSortHand = false;

	private static bool _autoPlayCards = true;

	private static bool _saveEnergyForLeftmost = true;

	private static bool _autoCardSelect = true;

	private static bool _showAttackBoxes = false;

	private static bool _reduceCombatVfx = true;

	private static bool _reduceHitVfx = false;

	private static bool _skipControlsHint = false;

	private static QuickPlayMode _quickPlayMode = QuickPlayMode.All;

	private static GameDifficulty _difficulty = GameDifficulty.Normal;

	private static float _playerMoveSpeed = 520f;

	private static float _enemyMoveSpeed = 250f;

	private static float _dexteritySpeedBonusPerStack = 0.25f;

	private static bool _floorOverlayEnabled = true;

	private static float _floorOverlayScaleX = 0.791f;

	private static float _floorOverlayScaleY = 1.156f;

	private static float _floorOverlayOffsetX = -21f;

	private static float _floorOverlayOffsetY = -120f;

	private static int _floorOverlayZIndex = -11;

	private static float _kaiserClawLOffsetX = -1506.9f;

	private static float _kaiserClawLOffsetY = -194.3f;

	private static float _kaiserClawLWidth = 480f;

	private static float _kaiserClawLHeight = 333f;

	private static float _kaiserClawROffsetX = 1393.3f;

	private static float _kaiserClawROffsetY = 40.3f;

	private static float _kaiserClawRWidth = 480f;

	private static float _kaiserClawRHeight = 324f;

	public const float DefaultPlayerMoveSpeed = 520f;

	public const float DefaultFloorOverlayScaleX = 0.791f;

	public const float DefaultFloorOverlayScaleY = 1.156f;

	public const float DefaultFloorOverlayOffsetX = -21f;

	public const float DefaultFloorOverlayOffsetY = -120f;

	public const int DefaultFloorOverlayZIndex = -11;

	public const float MinFloorOverlayScale = 0.4f;

	public const float MaxFloorOverlayScale = 3.5f;

	public const int MinFloorOverlayZIndex = -100;

	public const int MaxFloorOverlayZIndex = 100;

	public const float DefaultKaiserClawLOffsetX = -1506.9f;

	public const float DefaultKaiserClawLOffsetY = -194.3f;

	public const float DefaultKaiserClawLWidth = 480f;

	public const float DefaultKaiserClawLHeight = 333f;

	public const float DefaultKaiserClawROffsetX = 1393.3f;

	public const float DefaultKaiserClawROffsetY = 40.3f;

	public const float DefaultKaiserClawRWidth = 480f;

	public const float DefaultKaiserClawRHeight = 324f;

	public const float MinKaiserClawSize = 24f;

	public const float MaxKaiserClawSize = 480f;

	public const float MinPlayerMoveSpeed = 80f;

	public const float MaxPlayerMoveSpeed = 600f;

	public const float DefaultEnemyMoveSpeed = 250f;

	public const float MinEnemyMoveSpeed = 40f;

	public const float MaxEnemyMoveSpeed = 480f;

	public const float DefaultDexteritySpeedBonusPerStack = 0.25f;

	public const float MinDexteritySpeedBonusPerStack = 0f;

	public const float MaxDexteritySpeedBonusPerStack = 0.25f;

	public static bool EnemyTimedActionsEnabled = false;

	public static bool TurnHookPulsesEnabled = true;

	public static double TurnLengthSec
	{
		get
		{
			return _turnLengthSec;
		}
		set
		{
			double num = Math.Clamp(value, 2.5, 12.0);
			if (!(Math.Abs(num - _turnLengthSec) < 0.001))
			{
				_turnLengthSec = num;
				RealtimeCombatSettings.Changed?.Invoke();
			}
		}
	}

	public static bool AutoSortHand
	{
		get
		{
			return _autoSortHand;
		}
		set
		{
			if (!HostSettingsNetSync.ClientLocked && _autoSortHand != value)
			{
				_autoSortHand = value;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
				HostSettingsNetSync.OnHostLocalSettingsChanged();
			}
		}
	}

	public static bool AutoPlayCards
	{
		get
		{
			return _autoPlayCards;
		}
		set
		{
			if (!HostSettingsNetSync.ClientLocked && _autoPlayCards != value)
			{
				_autoPlayCards = value;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
				HostSettingsNetSync.OnHostLocalSettingsChanged();
			}
		}
	}

	public static bool SaveEnergyForLeftmost
	{
		get
		{
			return _saveEnergyForLeftmost;
		}
		set
		{
			if (!HostSettingsNetSync.ClientLocked && _saveEnergyForLeftmost != value)
			{
				_saveEnergyForLeftmost = value;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
				HostSettingsNetSync.OnHostLocalSettingsChanged();
			}
		}
	}

	public static bool AutoCardSelect
	{
		get
		{
			return _autoCardSelect;
		}
		set
		{
			if (_autoCardSelect != value)
			{
				_autoCardSelect = value;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
			}
		}
	}

	public static bool ShowAttackBoxes
	{
		get
		{
			return _showAttackBoxes;
		}
		set
		{
			if (!HostSettingsNetSync.ClientLocked && _showAttackBoxes != value)
			{
				_showAttackBoxes = value;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
				HostSettingsNetSync.OnHostLocalSettingsChanged();
			}
		}
	}

	public static bool ReduceCombatVfx
	{
		get
		{
			return _reduceCombatVfx;
		}
		set
		{
			if (_reduceCombatVfx != value)
			{
				_reduceCombatVfx = value;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
			}
		}
	}

	public static bool ReduceHitVfx
	{
		get
		{
			return _reduceHitVfx;
		}
		set
		{
			if (_reduceHitVfx != value)
			{
				_reduceHitVfx = value;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
			}
		}
	}

	public static bool FloorOverlayEnabled
	{
		get
		{
			return _floorOverlayEnabled;
		}
		set
		{
			if (_floorOverlayEnabled != value)
			{
				_floorOverlayEnabled = value;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
			}
		}
	}

	public static float FloorOverlayScaleX
	{
		get
		{
			return _floorOverlayScaleX;
		}
		set
		{
			float num = Math.Clamp(value, 0.4f, 3.5f);
			if (!(Math.Abs(num - _floorOverlayScaleX) < 0.0001f))
			{
				_floorOverlayScaleX = num;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
			}
		}
	}

	public static float FloorOverlayScaleY
	{
		get
		{
			return _floorOverlayScaleY;
		}
		set
		{
			float num = Math.Clamp(value, 0.4f, 3.5f);
			if (!(Math.Abs(num - _floorOverlayScaleY) < 0.0001f))
			{
				_floorOverlayScaleY = num;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
			}
		}
	}

	public static float FloorOverlayOffsetX
	{
		get
		{
			return _floorOverlayOffsetX;
		}
		set
		{
			if (!(Math.Abs(value - _floorOverlayOffsetX) < 0.01f))
			{
				_floorOverlayOffsetX = value;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
			}
		}
	}

	public static float FloorOverlayOffsetY
	{
		get
		{
			return _floorOverlayOffsetY;
		}
		set
		{
			if (!(Math.Abs(value - _floorOverlayOffsetY) < 0.01f))
			{
				_floorOverlayOffsetY = value;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
			}
		}
	}

	public static int FloorOverlayZIndex
	{
		get
		{
			return _floorOverlayZIndex;
		}
		set
		{
			int num = Math.Clamp(value, -100, 100);
			if (num != _floorOverlayZIndex)
			{
				_floorOverlayZIndex = num;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
			}
		}
	}

	public static float KaiserClawLOffsetX => _kaiserClawLOffsetX;

	public static float KaiserClawLOffsetY => _kaiserClawLOffsetY;

	public static float KaiserClawLWidth => _kaiserClawLWidth;

	public static float KaiserClawLHeight => _kaiserClawLHeight;

	public static float KaiserClawROffsetX => _kaiserClawROffsetX;

	public static float KaiserClawROffsetY => _kaiserClawROffsetY;

	public static float KaiserClawRWidth => _kaiserClawRWidth;

	public static float KaiserClawRHeight => _kaiserClawRHeight;

	public static bool SkipControlsHint
	{
		get
		{
			return _skipControlsHint;
		}
		set
		{
			if (_skipControlsHint != value)
			{
				_skipControlsHint = value;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
			}
		}
	}

	public static QuickPlayMode QuickPlayMode
	{
		get
		{
			return _quickPlayMode;
		}
		set
		{
			if (!HostSettingsNetSync.ClientLocked && _quickPlayMode != value)
			{
				_quickPlayMode = value;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
				HostSettingsNetSync.OnHostLocalSettingsChanged();
			}
		}
	}

	public static GameDifficulty Difficulty
	{
		get
		{
			return _difficulty;
		}
		set
		{
			if (!HostSettingsNetSync.ClientLocked && _difficulty != value)
			{
				_difficulty = value;
				Save();
				RealtimeCombatSettings.Changed?.Invoke();
				HostSettingsNetSync.OnHostLocalSettingsChanged();
			}
		}
	}

	public static float ProximityCloseDist
	{
		get
		{
			GameDifficulty difficulty = _difficulty;
			if (1 == 0)
			{
			}
			float result = difficulty switch
			{
				GameDifficulty.Easy => 72f, 
				GameDifficulty.Normal => 48f, 
				_ => 36f, 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}

	public static float ProximityFarDist
	{
		get
		{
			GameDifficulty difficulty = _difficulty;
			if (1 == 0)
			{
			}
			float result = difficulty switch
			{
				GameDifficulty.Easy => 500f, 
				GameDifficulty.Normal => 430f, 
				_ => 380f, 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}

	public static decimal ProximityMaxMultiplier
	{
		get
		{
			GameDifficulty difficulty = _difficulty;
			if (1 == 0)
			{
			}
			decimal result = difficulty switch
			{
				GameDifficulty.Easy => 8m, 
				GameDifficulty.Normal => 7m, 
				_ => 5m, 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}

	private static double PaceIntervalMul
	{
		get
		{
			GameDifficulty difficulty = _difficulty;
			if (1 == 0)
			{
			}
			double result = difficulty switch
			{
				GameDifficulty.Easy => 0.85, 
				GameDifficulty.Normal => 0.9, 
				_ => 1.0, 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}

	public static float PlayerMoveSpeed
	{
		get
		{
			return _playerMoveSpeed;
		}
		set
		{
			float num = Math.Clamp(value, 80f, 600f);
			if (!(Math.Abs(num - _playerMoveSpeed) < 0.1f))
			{
				_playerMoveSpeed = num;
				RealtimeCombatSettings.Changed?.Invoke();
			}
		}
	}

	public static float EnemyMoveSpeed
	{
		get
		{
			return _enemyMoveSpeed;
		}
		set
		{
			float num = Math.Clamp(value, 40f, 480f);
			if (!(Math.Abs(num - _enemyMoveSpeed) < 0.1f))
			{
				_enemyMoveSpeed = num;
				RealtimeCombatSettings.Changed?.Invoke();
			}
		}
	}

	public static float DexteritySpeedBonusPerStack
	{
		get
		{
			return _dexteritySpeedBonusPerStack;
		}
		set
		{
			float num = Math.Clamp(value, 0f, 0.25f);
			if (!(Math.Abs(num - _dexteritySpeedBonusPerStack) < 0.0005f))
			{
				_dexteritySpeedBonusPerStack = num;
				RealtimeCombatSettings.Changed?.Invoke();
			}
		}
	}

	public static double EnemyIntervalSec => TurnLengthSec * RelicIntervalMods.EnemyIntervalMultiplier;

	public static double DrawIntervalSec => TurnLengthSec * 0.1075 * PaceIntervalMul;

	public static double EnergyIntervalSec => TurnLengthSec * 0.155 * PaceIntervalMul;

	public static double HookPulseIntervalSec => TurnLengthSec * 0.5;

	public static event Action? Changed;

	public static void SetFloorOverlayLayout(float scaleX, float scaleY, float offsetX, float offsetY, int zIndex, bool save)
	{
		_floorOverlayScaleX = Math.Clamp(scaleX, 0.4f, 3.5f);
		_floorOverlayScaleY = Math.Clamp(scaleY, 0.4f, 3.5f);
		_floorOverlayOffsetX = offsetX;
		_floorOverlayOffsetY = offsetY;
		_floorOverlayZIndex = Math.Clamp(zIndex, -100, 100);
		if (save)
		{
			Save();
		}
		RealtimeCombatSettings.Changed?.Invoke();
	}

	public static void SetKaiserClaw(bool right, float offsetX, float offsetY, float width, float height, bool save)
	{
		width = Math.Clamp(width, 24f, 480f);
		height = Math.Clamp(height, 24f, 480f);
		if (right)
		{
			_kaiserClawROffsetX = offsetX;
			_kaiserClawROffsetY = offsetY;
			_kaiserClawRWidth = width;
			_kaiserClawRHeight = height;
		}
		else
		{
			_kaiserClawLOffsetX = offsetX;
			_kaiserClawLOffsetY = offsetY;
			_kaiserClawLWidth = width;
			_kaiserClawLHeight = height;
		}
		if (save)
		{
			Save();
		}
		RealtimeCombatSettings.Changed?.Invoke();
	}

	public static void ApplyHostSnapshot(bool autoPlay, QuickPlayMode quickPlay, bool autoSort, bool showBoxes, GameDifficulty difficulty, bool saveEnergyForLeftmost)
	{
		_autoPlayCards = autoPlay;
		_quickPlayMode = quickPlay;
		_autoSortHand = autoSort;
		_showAttackBoxes = showBoxes;
		_difficulty = difficulty;
		_saveEnergyForLeftmost = saveEnergyForLeftmost;
		RealtimeCombatSettings.Changed?.Invoke();
	}

	public static void Load()
	{
		try
		{
			string savePath = GetSavePath();
			if (!File.Exists(savePath))
			{
				return;
			}
			string[] array = File.ReadAllLines(savePath);
			foreach (string text in array)
			{
				string[] array2 = text.Split('=', 2);
				if (array2.Length == 2)
				{
					string text2 = array2[0].Trim();
					string text3 = array2[1].Trim();
					int result;
					int result2;
					float result3;
					float result4;
					float result5;
					float result6;
					int result7;
					float result8;
					float result9;
					float result10;
					float result11;
					float result12;
					float result13;
					float result14;
					float result15;
					if (text2 == "auto_sort_hand" && (text3 == "1" || text3 == "0"))
					{
						_autoSortHand = text3 == "1";
					}
					else if (text2 == "auto_play_cards" && (text3 == "1" || text3 == "0"))
					{
						_autoPlayCards = text3 == "1";
					}
					else if (text2 == "save_energy_leftmost" && (text3 == "1" || text3 == "0"))
					{
						_saveEnergyForLeftmost = text3 == "1";
					}
					else if (text2 == "auto_card_select" && (text3 == "1" || text3 == "0"))
					{
						_autoCardSelect = text3 == "1";
					}
					else if (text2 == "manual_card_select" && (text3 == "1" || text3 == "0"))
					{
						_autoCardSelect = text3 != "1";
					}
					else if (text2 == "quick_play_mode" && int.TryParse(text3, out result))
					{
						_quickPlayMode = (QuickPlayMode)Math.Clamp(result, 0, 2);
					}
					else if (text2 == "quick_play" && (text3 == "1" || text3 == "0"))
					{
						_quickPlayMode = ((!(text3 == "1")) ? QuickPlayMode.Off : QuickPlayMode.All);
					}
					else if (text2 == "show_attack_boxes" && (text3 == "1" || text3 == "0"))
					{
						_showAttackBoxes = text3 == "1";
					}
					else if (text2 == "reduce_combat_vfx" && (text3 == "1" || text3 == "0"))
					{
						_reduceCombatVfx = text3 == "1";
					}
					else if (text2 == "reduce_hit_vfx" && (text3 == "1" || text3 == "0"))
					{
						_reduceHitVfx = text3 == "1";
					}
					else if (text2 == "skip_controls_hint" && (text3 == "1" || text3 == "0"))
					{
						_skipControlsHint = text3 == "1";
					}
					else if (text2 == "difficulty" && int.TryParse(text3, out result2))
					{
						_difficulty = (GameDifficulty)Math.Clamp(result2, 0, 2);
					}
					else if (text2 == "floor_overlay" && (text3 == "1" || text3 == "0"))
					{
						_floorOverlayEnabled = text3 == "1";
					}
					else if (text2 == "floor_scale_x" && float.TryParse(text3, NumberStyles.Float, CultureInfo.InvariantCulture, out result3))
					{
						_floorOverlayScaleX = Math.Clamp(result3, 0.4f, 3.5f);
					}
					else if (text2 == "floor_scale_y" && float.TryParse(text3, NumberStyles.Float, CultureInfo.InvariantCulture, out result4))
					{
						_floorOverlayScaleY = Math.Clamp(result4, 0.4f, 3.5f);
					}
					else if (text2 == "floor_offset_x" && float.TryParse(text3, NumberStyles.Float, CultureInfo.InvariantCulture, out result5))
					{
						_floorOverlayOffsetX = result5;
					}
					else if (text2 == "floor_offset_y" && float.TryParse(text3, NumberStyles.Float, CultureInfo.InvariantCulture, out result6))
					{
						_floorOverlayOffsetY = result6;
					}
					else if (text2 == "floor_z_index" && int.TryParse(text3, out result7))
					{
						_floorOverlayZIndex = Math.Clamp(result7, -100, 100);
					}
					else if (text2 == "kaiser_claw_l_x" && float.TryParse(text3, NumberStyles.Float, CultureInfo.InvariantCulture, out result8))
					{
						_kaiserClawLOffsetX = result8;
					}
					else if (text2 == "kaiser_claw_l_y" && float.TryParse(text3, NumberStyles.Float, CultureInfo.InvariantCulture, out result9))
					{
						_kaiserClawLOffsetY = result9;
					}
					else if (text2 == "kaiser_claw_l_w" && float.TryParse(text3, NumberStyles.Float, CultureInfo.InvariantCulture, out result10))
					{
						_kaiserClawLWidth = Math.Clamp(result10, 24f, 480f);
					}
					else if (text2 == "kaiser_claw_l_h" && float.TryParse(text3, NumberStyles.Float, CultureInfo.InvariantCulture, out result11))
					{
						_kaiserClawLHeight = Math.Clamp(result11, 24f, 480f);
					}
					else if (text2 == "kaiser_claw_r_x" && float.TryParse(text3, NumberStyles.Float, CultureInfo.InvariantCulture, out result12))
					{
						_kaiserClawROffsetX = result12;
					}
					else if (text2 == "kaiser_claw_r_y" && float.TryParse(text3, NumberStyles.Float, CultureInfo.InvariantCulture, out result13))
					{
						_kaiserClawROffsetY = result13;
					}
					else if (text2 == "kaiser_claw_r_w" && float.TryParse(text3, NumberStyles.Float, CultureInfo.InvariantCulture, out result14))
					{
						_kaiserClawRWidth = Math.Clamp(result14, 24f, 480f);
					}
					else if (text2 == "kaiser_claw_r_h" && float.TryParse(text3, NumberStyles.Float, CultureInfo.InvariantCulture, out result15))
					{
						_kaiserClawRHeight = Math.Clamp(result15, 24f, 480f);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Warn("[local.action_game] settings load failed: " + ex.Message, 2);
		}
	}

	public static void Save()
	{
		try
		{
			string savePath = GetSavePath();
			string directoryName = Path.GetDirectoryName(savePath);
			if (!string.IsNullOrEmpty(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.WriteAllText(savePath, $"auto_sort_hand={(_autoSortHand ? "1" : "0")}\nauto_play_cards={(_autoPlayCards ? "1" : "0")}\nsave_energy_leftmost={(_saveEnergyForLeftmost ? "1" : "0")}\nauto_card_select={(_autoCardSelect ? "1" : "0")}\nquick_play_mode={_quickPlayMode}\nshow_attack_boxes={(_showAttackBoxes ? "1" : "0")}\nreduce_combat_vfx={(_reduceCombatVfx ? "1" : "0")}\nreduce_hit_vfx={(_reduceHitVfx ? "1" : "0")}\nskip_controls_hint={(_skipControlsHint ? "1" : "0")}\ndifficulty={_difficulty}\nfloor_overlay={(_floorOverlayEnabled ? "1" : "0")}\nfloor_scale_x={_floorOverlayScaleX.ToString("0.###", CultureInfo.InvariantCulture)}\nfloor_scale_y={_floorOverlayScaleY.ToString("0.###", CultureInfo.InvariantCulture)}\nfloor_offset_x={_floorOverlayOffsetX.ToString("0.#", CultureInfo.InvariantCulture)}\nfloor_offset_y={_floorOverlayOffsetY.ToString("0.#", CultureInfo.InvariantCulture)}\nfloor_z_index={_floorOverlayZIndex}\nkaiser_claw_l_x={_kaiserClawLOffsetX.ToString("0.#", CultureInfo.InvariantCulture)}\nkaiser_claw_l_y={_kaiserClawLOffsetY.ToString("0.#", CultureInfo.InvariantCulture)}\nkaiser_claw_l_w={_kaiserClawLWidth.ToString("0.#", CultureInfo.InvariantCulture)}\nkaiser_claw_l_h={_kaiserClawLHeight.ToString("0.#", CultureInfo.InvariantCulture)}\nkaiser_claw_r_x={_kaiserClawROffsetX.ToString("0.#", CultureInfo.InvariantCulture)}\nkaiser_claw_r_y={_kaiserClawROffsetY.ToString("0.#", CultureInfo.InvariantCulture)}\nkaiser_claw_r_w={_kaiserClawRWidth.ToString("0.#", CultureInfo.InvariantCulture)}\nkaiser_claw_r_h={_kaiserClawRHeight.ToString("0.#", CultureInfo.InvariantCulture)}\n");
		}
		catch (Exception ex)
		{
			Log.Warn("[local.action_game] settings save failed: " + ex.Message, 2);
		}
	}

	private static string GetSavePath()
	{
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		return Path.Combine(folderPath, "SlayTheSpire2", "local.action_game", "settings.txt");
	}
}
internal static class RealtimeCombatTicker
{
	private static double _drawTimer;

	private static double _energyTimer;

	private static double _enemyTimer;

	private static double _hookTimer;

	private static double _reviveTimer;

	private static bool _nextHookIsTurnStart = true;

	private static bool _busy;

	private static bool _enemyActing;

	private static bool _running;

	private static bool _frameHooked;

	private static bool _settingsHooked;

	private static ulong _lastTicks;

	private static int _frameLogCounter;

	private const double PendingReviveIntervalSec = 0.85;

	public static double DrawIntervalSec => RealtimeCombatSettings.DrawIntervalSec;

	public static double EnergyIntervalSec => RealtimeCombatSettings.EnergyIntervalSec;

	public static double EnemyIntervalSec => RealtimeCombatSettings.EnemyIntervalSec;

	public static bool IsRunning => _running;

	public static void Start()
	{
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress)
		{
			Log.Warn("[local.action_game] ticker Start skipped (manager/inProgress missing)", 2);
			return;
		}
		_drawTimer = 0.0;
		_energyTimer = 0.0;
		_enemyTimer = 0.0;
		_hookTimer = 0.0;
		_reviveTimer = 0.0;
		_nextHookIsTurnStart = true;
		_busy = false;
		_enemyActing = false;
		_running = true;
		_lastTicks = Time.GetTicksMsec();
		_frameLogCounter = 0;
		PlayerChoicePause.Reset();
		CombatAccess.ResetWinWatchdog();
		EnsureFrameHook();
		EnsureSettingsHook();
		QuickPlaySystem.EnsureInputHook();
		if (RealtimeCombatSettings.EnemyTimedActionsEnabled)
		{
			EnemyAttackHud.Show();
			EnemyAttackHud.Update(0.0, EnemyIntervalSec, enemyActing: false);
		}
		else
		{
			EnemyAttackHud.Hide();
		}
		TurnLengthHud.EnsureShown();
		ControlsHintPopup.TryShow();
		Log.Info("[local.action_game] ticker started (ProcessFrame)", 2);
	}

	public static void Stop()
	{
		if (_running || _busy || _enemyActing)
		{
			_running = false;
			_busy = false;
			_enemyActing = false;
			ControlsHintPopup.Hide();
			PlayerChoicePause.Reset();
			CombatAccess.SetPlayerActionsDisabled(disabled: false);
			EnemyAttackHud.Hide();
			Log.Info("[local.action_game] ticker stopped", 2);
		}
	}

	public static void StopEnemyActions()
	{
		_enemyActing = false;
		_busy = false;
	}

	private static void EnsureSettingsHook()
	{
		if (!_settingsHooked)
		{
			RealtimeCombatSettings.Changed += OnSettingsChanged;
			_settingsHooked = true;
		}
	}

	private static void OnSettingsChanged()
	{
		_drawTimer = Math.Min(_drawTimer, DrawIntervalSec);
		_energyTimer = Math.Min(_energyTimer, EnergyIntervalSec);
		_enemyTimer = Math.Min(_enemyTimer, EnemyIntervalSec);
		_hookTimer = Math.Min(_hookTimer, RealtimeCombatSettings.HookPulseIntervalSec);
		Log.Info($"[{"local.action_game"}] timing updated cycle={RealtimeCombatSettings.TurnLengthSec:0.##}s hook={RealtimeCombatSettings.HookPulseIntervalSec:0.##}s draw={DrawIntervalSec:0.##}s energy={EnergyIntervalSec:0.##}s", 2);
	}

	private static void EnsureFrameHook()
	{
		if (!_frameHooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val == null)
			{
				Log.Warn("[local.action_game] SceneTree missing, cannot hook ProcessFrame", 2);
				return;
			}
			val.ProcessFrame += OnProcessFrame;
			_frameHooked = true;
			Log.Info("[local.action_game] ProcessFrame hooked", 2);
		}
	}

	private static void OnProcessFrame()
	{
		if (!_running)
		{
			return;
		}
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress)
		{
			if (_running)
			{
				Log.Info("[local.action_game] ticker stop: manager gone or !IsInProgress", 2);
			}
			Stop();
			return;
		}
		if (instance.IsEnding)
		{
			ulong ticksMsec = Time.GetTicksMsec();
			double num = (double)(ticksMsec - _lastTicks) / 1000.0;
			_lastTicks = ticksMsec;
			if (num <= 0.0 || num > 1.0)
			{
				num = 1.0 / 60.0;
			}
			CombatAccess.TickWinWatchdog(num, instance.DebugOnlyGetState());
			return;
		}
		ulong ticksMsec2 = Time.GetTicksMsec();
		double num2 = (double)(ticksMsec2 - _lastTicks) / 1000.0;
		_lastTicks = ticksMsec2;
		if (num2 <= 0.0 || num2 > 1.0)
		{
			num2 = 1.0 / 60.0;
		}
		CombatAccess.FreezePlayerPlayPhase();
		AutoSortHandSystem.Tick();
		if (RealtimeCombatSettings.EnemyTimedActionsEnabled)
		{
			EnemyAttackHud.Update(_enemyTimer, EnemyIntervalSec, _enemyActing);
		}
		CombatAccess.TickWinWatchdog(num2, instance.DebugOnlyGetState());
		if (_busy)
		{
			return;
		}
		if (!PlayerChoicePause.IsPaused)
		{
			_drawTimer += num2;
			_energyTimer += num2;
			_reviveTimer += num2;
			if (RealtimeCombatSettings.EnemyTimedActionsEnabled)
			{
				_enemyTimer += num2;
			}
			if (RealtimeCombatSettings.TurnHookPulsesEnabled)
			{
				_hookTimer += num2;
			}
		}
		if (_frameLogCounter++ % 120 == 0)
		{
			Log.Info($"[{"local.action_game"}] tick alive drawT={_drawTimer:0.00} energyT={_energyTimer:0.00} hookT={_hookTimer:0.00} nextStart={_nextHookIsTurnStart} paused={PlayerChoicePause.IsPaused}", 2);
		}
		if (_drawTimer >= DrawIntervalSec)
		{
			_drawTimer = 0.0;
			FireDraw();
			return;
		}
		if (_energyTimer >= EnergyIntervalSec)
		{
			_energyTimer = 0.0;
			FireEnergy();
			return;
		}
		if (RealtimeCombatSettings.TurnHookPulsesEnabled && _hookTimer >= RealtimeCombatSettings.HookPulseIntervalSec)
		{
			_hookTimer = 0.0;
			FireTurnHookPulse();
			return;
		}
		if (_reviveTimer >= 0.85)
		{
			_reviveTimer = 0.0;
			if (HasSpecialTakeTurnActors(instance.DebugOnlyGetState()))
			{
				FireSpecialTakeTurns();
				return;
			}
		}
		if (RealtimeCombatSettings.EnemyTimedActionsEnabled && _enemyTimer >= EnemyIntervalSec)
		{
			_enemyTimer = 0.0;
			FireEnemyActions();
		}
	}

	private static bool HasSpecialTakeTurnActors(CombatState? state)
	{
		if (((state != null) ? state.Enemies : null) == null)
		{
			return false;
		}
		foreach (Creature enemy in state.Enemies)
		{
			if (NeedsSpecialTakeTurn(enemy, (ICombatState)(object)state))
			{
				return true;
			}
		}
		return false;
	}

	private static bool NeedsSpecialTakeTurn(Creature? enemy, ICombatState state)
	{
		if (enemy == null)
		{
			return false;
		}
		if (enemy.IsDead && CombatAccess.ShouldEnemyActThisCycle(enemy, state))
		{
			return true;
		}
		return IsWaterfallAboutToBlow(enemy);
	}

	private static bool IsWaterfallAboutToBlow(Creature? enemy)
	{
		MonsterModel obj = ((enemy != null) ? enemy.Monster : null);
		WaterfallGiant val = (WaterfallGiant)(object)((obj is WaterfallGiant) ? obj : null);
		if (val == null)
		{
			return false;
		}
		return Traverse.Create((object)val).Property("IsAboutToBlow", (object[])null).GetValue<bool>();
	}

	private static async void FireSpecialTakeTurns()
	{
		if (_busy || !CanTickCombat())
		{
			return;
		}
		_busy = true;
		_enemyActing = true;
		CombatManager manager = CombatManager.Instance;
		try
		{
			if (!CanTickCombat() || manager == null)
			{
				return;
			}
			CombatState state = manager.DebugOnlyGetState();
			if (state == null)
			{
				return;
			}
			List<Creature> pending = state.Enemies.Where((Creature e) => NeedsSpecialTakeTurn(e, (ICombatState)(object)state)).ToList();
			if (pending.Count == 0)
			{
				return;
			}
			CombatAccess.SetPlayerActionsDisabled(disabled: true);
			Log.Info($"[{"local.action_game"}] special TakeTurn x{pending.Count}", 2);
			foreach (Creature enemy in pending)
			{
				if (!manager.IsInProgress || manager.IsOverOrEnding)
				{
					break;
				}
				if (enemy.Monster != null)
				{
					if (enemy.Monster.SpawnedThisTurn)
					{
						enemy.Monster.OnSideSwitch();
					}
					Log.Info($"[{"local.action_game"}] special-acting: {enemy.LogName} alive={enemy.IsAlive}", 2);
					await enemy.TakeTurn();
					if (manager.IsPaused)
					{
						await manager.WaitForUnpause();
					}
					if (await CombatAccess.CheckWinAfterDeathEffects())
					{
						return;
					}
				}
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Log.Warn($"[{"local.action_game"}] special TakeTurn failed: {ex}", 2);
		}
		finally
		{
			_enemyActing = false;
			_busy = false;
			CombatAccess.SetPlayerActionsDisabled(disabled: false);
		}
	}

	private static bool CanTickCombat()
	{
		if (!_running)
		{
			return false;
		}
		CombatManager instance = CombatManager.Instance;
		return instance != null && instance.IsInProgress && !instance.IsEnding && !instance.IsOverOrEnding;
	}

	private static async void FireDraw()
	{
		if (_busy || !CanTickCombat())
		{
			return;
		}
		_busy = true;
		try
		{
			CombatManager manager = CombatManager.Instance;
			CombatState state = ((manager != null) ? manager.DebugOnlyGetState() : null);
			if (!CanTickCombat() || state == null)
			{
				return;
			}
			Player player = LocalContext.GetMe((ICombatState)(object)state);
			if (((player != null) ? player.PlayerCombatState : null) == null)
			{
				Log.Warn("[local.action_game] draw tick: local player missing", 2);
				return;
			}
			BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
			await CardPileCmd.Draw((PlayerChoiceContext)(object)ctx, 1m, player, false);
			if (CanTickCombat())
			{
				Log.Info($"[{"local.action_game"}] +1 card (hand={player.PlayerCombatState.Hand.Cards.Count})", 2);
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Log.Warn($"[{"local.action_game"}] draw tick failed: {ex}", 2);
		}
		finally
		{
			_busy = false;
		}
	}

	private static async void FireEnergy()
	{
		if (_busy || !CanTickCombat())
		{
			return;
		}
		_busy = true;
		try
		{
			CombatManager manager = CombatManager.Instance;
			CombatState state = ((manager != null) ? manager.DebugOnlyGetState() : null);
			if (!CanTickCombat() || state == null)
			{
				return;
			}
			Player player = LocalContext.GetMe((ICombatState)(object)state);
			if (((player != null) ? player.PlayerCombatState : null) == null)
			{
				Log.Warn("[local.action_game] energy tick: local player missing", 2);
				return;
			}
			await PlayerCmd.GainEnergy(1m, player);
			if (CanTickCombat())
			{
				Log.Info($"[{"local.action_game"}] +1 energy (energy={player.PlayerCombatState.Energy})", 2);
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Log.Warn($"[{"local.action_game"}] energy tick failed: {ex}", 2);
		}
		finally
		{
			_busy = false;
		}
	}

	private static async void FireTurnHookPulse()
	{
		if (_busy || !CanTickCombat())
		{
			return;
		}
		_busy = true;
		bool runStart = _nextHookIsTurnStart;
		_nextHookIsTurnStart = !_nextHookIsTurnStart;
		CombatManager manager = CombatManager.Instance;
		try
		{
			if (!CanTickCombat() || manager == null)
			{
				return;
			}
			CombatState state = manager.DebugOnlyGetState();
			if (state != null)
			{
				CombatAccess.SetPlayerActionsDisabled(disabled: true);
				if (runStart)
				{
					Log.Info("[local.action_game] turn-start pulse", 2);
					await TurnCycleBridge.RunTurnStartPulse((ICombatState)(object)state);
				}
				else
				{
					Log.Info("[local.action_game] turn-end pulse", 2);
					await TurnCycleBridge.RunTurnEndPulse((ICombatState)(object)state);
				}
				await CombatAccess.CheckWinAfterDeathEffects();
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Log.Warn($"[{"local.action_game"}] turn hook pulse failed: {ex}", 2);
		}
		finally
		{
			_busy = false;
			CombatAccess.SetPlayerActionsDisabled(disabled: false);
		}
	}

	private static async void FireEnemyActions()
	{
		if (_busy || !CanTickCombat())
		{
			return;
		}
		_busy = true;
		_enemyActing = true;
		CombatManager manager = CombatManager.Instance;
		try
		{
			if (!CanTickCombat())
			{
				return;
			}
			CombatState state = manager.DebugOnlyGetState();
			if (state == null)
			{
				return;
			}
			CombatAccess.SetPlayerActionsDisabled(disabled: true);
			List<Creature> enemies = state.Enemies.Where((Creature e) => CombatAccess.ShouldEnemyActThisCycle(e, (ICombatState)(object)state)).ToList();
			Log.Info($"[{"local.action_game"}] enemy tick - {enemies.Count} actors (alive or pending revive)", 2);
			if (enemies.Count == 0)
			{
				await CombatAccess.CheckWinAfterDeathEffects();
				return;
			}
			await TurnCycleBridge.BeforeEnemyActions((ICombatState)(object)state);
			if (!manager.IsInProgress || manager.IsOverOrEnding)
			{
				return;
			}
			enemies = state.Enemies.Where((Creature e) => CombatAccess.ShouldEnemyActThisCycle(e, (ICombatState)(object)state)).ToList();
			foreach (Creature enemy2 in enemies)
			{
				if (!manager.IsInProgress || manager.IsOverOrEnding || (enemy2.IsDead && !CombatAccess.ShouldEnemyActThisCycle(enemy2, (ICombatState)(object)state)))
				{
					continue;
				}
				if (enemy2.Monster == null)
				{
					Log.Info($"[{"local.action_game"}] skip {enemy2.LogName} (no monster model)", 2);
					continue;
				}
				if (enemy2.Monster.SpawnedThisTurn)
				{
					enemy2.Monster.OnSideSwitch();
				}
				Log.Info($"[{"local.action_game"}] enemy acting: {enemy2.LogName} alive={enemy2.IsAlive}", 2);
				await enemy2.TakeTurn();
				if (manager.IsPaused)
				{
					await manager.WaitForUnpause();
				}
				if (!(await CombatAccess.CheckWinAfterDeathEffects()))
				{
					continue;
				}
				return;
			}
			state = manager.DebugOnlyGetState();
			if (state == null || !manager.IsInProgress || manager.IsOverOrEnding)
			{
				return;
			}
			List<Creature> targets = state.PlayerCreatures.Where((Creature c) => c.IsAlive).ToList();
			foreach (Creature enemy in state.Enemies.Where((Creature e) => e.IsAlive && e.IsMonster).ToList())
			{
				if (!IsWaterfallAboutToBlow(enemy))
				{
					enemy.PrepareForNextTurn((IEnumerable<Creature>)targets, true);
				}
			}
			await TurnCycleBridge.AfterEnemyActions((ICombatState)(object)state);
			if (manager.IsInProgress && !manager.IsOverOrEnding)
			{
				Log.Info("[local.action_game] enemy tick done, intents refreshed, turn hooks fired", 2);
			}
		}
		catch (Exception ex2)
		{
			Exception ex = ex2;
			Log.Warn($"[{"local.action_game"}] enemy tick failed: {ex}", 2);
		}
		finally
		{
			_enemyActing = false;
			_busy = false;
			CombatAccess.SetPlayerActionsDisabled(disabled: false);
		}
	}
}
[HarmonyPatch]
internal static class ReduceCombatVfxPatch
{
	[HarmonyPatch(typeof(VfxCmd), "PlayVfx")]
	[HarmonyPrefix]
	private static bool SkipPlayVfx()
	{
		return !RealtimeCombatSettings.ReduceCombatVfx;
	}

	[HarmonyPatch(typeof(VfxCmd), "PlayFullScreenInCombat")]
	[HarmonyPrefix]
	private static bool SkipFullScreen()
	{
		return !RealtimeCombatSettings.ReduceCombatVfx;
	}

	[HarmonyPatch(typeof(AttackCommand), "Execute")]
	[HarmonyPrefix]
	private static void StripAttackCommandVfx(AttackCommand __instance)
	{
		if (RealtimeCombatSettings.ReduceCombatVfx)
		{
			Traverse.Create((object)__instance).Field("_attackerVfx").SetValue((object)null);
			Traverse.Create((object)__instance).Field("_customAttackerVfxNodes").GetValue<List<Func<Node2D>>>()?.Clear();
		}
		if (RealtimeCombatSettings.ReduceHitVfx)
		{
			Traverse.Create((object)__instance).Field("<HitVfx>k__BackingField").SetValue((object)null);
			Traverse.Create((object)__instance).Field("_customHitVfxNodes").GetValue<List<Func<Creature, Node2D>>>()?.Clear();
		}
	}
}
[HarmonyPatch]
internal static class ReduceHitVfxCreatePatch
{
	[CompilerGenerated]
	private sealed class <TargetMethods>d__0 : IEnumerable<MethodBase>, IEnumerable, IEnumerator<MethodBase>, IEnumerator, IDisposable
	{
		private int <>1__state;

		private MethodBase <>2__current;

		private int <>l__initialThreadId;

		private Assembly <asm>5__1;

		private Type[] <>s__2;

		private int <>s__3;

		private Type <type>5__4;

		private string <name>5__5;

		private bool <isHitFx>5__6;

		private bool <>s__7;

		private bool <>s__8;

		private MethodInfo[] <>s__9;

		private int <>s__10;

		private MethodInfo <method>5__11;

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
		public <TargetMethods>d__0(int <>1__state)
		{
			this.<>1__state = <>1__state;
			<>l__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			<asm>5__1 = null;
			<>s__2 = null;
			<type>5__4 = null;
			<name>5__5 = null;
			<>s__9 = null;
			<method>5__11 = null;
			<>1__state = -2;
		}

		private bool MoveNext()
		{
			int num = <>1__state;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				<>1__state = -1;
				goto IL_01b5;
			}
			<>1__state = -1;
			<asm>5__1 = typeof(NHitSparkVfx).Assembly;
			<>s__2 = AccessTools.GetTypesFromAssembly(<asm>5__1);
			<>s__3 = 0;
			goto IL_01ff;
			IL_01ff:
			if (<>s__3 < <>s__2.Length)
			{
				<type>5__4 = <>s__2[<>s__3];
				if (!(<type>5__4 == null) && <type>5__4.IsClass && !(<type>5__4.Namespace != "MegaCrit.Sts2.Core.Nodes.Vfx"))
				{
					<name>5__5 = <type>5__4.Name;
					<>s__7 = <name>5__5.EndsWith("ImpactVfx", StringComparison.Ordinal);
					<>s__8 = <>s__7;
					if (!<>s__8)
					{
						bool flag;
						switch (<name>5__5)
						{
						case "NHitSparkVfx":
						case "NDamageBlockedVfx":
						case "NDamageNumVfx":
							flag = true;
							break;
						default:
							flag = false;
							break;
						}
						<>s__8 = flag;
					}
					<isHitFx>5__6 = <>s__8;
					if (<isHitFx>5__6)
					{
						<>s__9 = <type>5__4.GetMethods(BindingFlags.Static | BindingFlags.Public);
						<>s__10 = 0;
						goto IL_01cb;
					}
				}
				goto IL_01f1;
			}
			<>s__2 = null;
			return false;
			IL_01cb:
			if (<>s__10 < <>s__9.Length)
			{
				<method>5__11 = <>s__9[<>s__10];
				if (<method>5__11.Name == "Create")
				{
					<>2__current = <method>5__11;
					<>1__state = 1;
					return true;
				}
				goto IL_01b5;
			}
			<>s__9 = null;
			<name>5__5 = null;
			<type>5__4 = null;
			goto IL_01f1;
			IL_01f1:
			<>s__3++;
			goto IL_01ff;
			IL_01b5:
			<method>5__11 = null;
			<>s__10++;
			goto IL_01cb;
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
		IEnumerator<MethodBase> IEnumerable<MethodBase>.GetEnumerator()
		{
			if (<>1__state == -2 && <>l__initialThreadId == Environment.CurrentManagedThreadId)
			{
				<>1__state = 0;
				return this;
			}
			return new <TargetMethods>d__0(0);
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<MethodBase>)this).GetEnumerator();
		}
	}

	[IteratorStateMachine(typeof(<TargetMethods>d__0))]
	private static IEnumerable<MethodBase> TargetMethods()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new <TargetMethods>d__0(-2);
	}

	private static void Postfix(object? __result)
	{
		if (!RealtimeCombatSettings.ReduceHitVfx)
		{
			return;
		}
		Node val = (Node)((__result is Node) ? __result : null);
		if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
		{
			CanvasItem val2 = (CanvasItem)(object)((val is CanvasItem) ? val : null);
			if (val2 != null)
			{
				val2.Visible = false;
			}
			val.QueueFree();
		}
	}
}
[HarmonyPatch(typeof(PlayerHurtVignetteHelper), "Play")]
internal static class ReduceHitVignettePatch
{
	private static bool Prefix()
	{
		return !RealtimeCombatSettings.ReduceHitVfx;
	}
}
internal static class RelicIntervalMods
{
	public const double IceCreamBonus = 1.0 / 3.0;

	public const double PyramidBonus = 0.5;

	public const string IceCreamDescription = "敌人行动间隔增加 33%";

	public const string PyramidDescription = "敌人行动间隔增加 50%";

	public static double EnemyIntervalMultiplier
	{
		get
		{
			double num = 0.0;
			if (RelicIntervalMods.LocalHasRelic<IceCream>())
			{
				num += 1.0 / 3.0;
			}
			if (RelicIntervalMods.LocalHasRelic<RunicPyramid>())
			{
				num += 0.5;
			}
			return 1.0 + num;
		}
	}

	public static bool LocalHasRelic<T>() where T : RelicModel
	{
		Player localPlayer = GetLocalPlayer();
		if (localPlayer == null)
		{
			return false;
		}
		foreach (RelicModel relic in localPlayer.Relics)
		{
			if (relic is T)
			{
				return true;
			}
		}
		return false;
	}

	private static Player? GetLocalPlayer()
	{
		CombatManager instance = CombatManager.Instance;
		CombatState val = ((instance != null) ? instance.DebugOnlyGetState() : null);
		if (val != null)
		{
			Player me = LocalContext.GetMe((ICombatState)(object)val);
			if (me != null)
			{
				return me;
			}
		}
		RunManager instance2 = RunManager.Instance;
		if (instance2 == null)
		{
			return null;
		}
		RunState value = Traverse.Create((object)instance2).Property("State", (object[])null).GetValue<RunState>();
		return (value == null) ? null : LocalContext.GetMe((IPlayerCollection)(object)value);
	}
}
[HarmonyPatch(typeof(IceCream), "ShouldPlayerResetEnergy")]
internal static class IceCreamDisableEnergyRetainPatch
{
	private static void Postfix(ref bool __result)
	{
		__result = true;
	}
}
[HarmonyPatch(typeof(RunicPyramid), "ShouldFlush")]
internal static class PyramidDisableHandRetainPatch
{
	private static void Postfix(ref bool __result)
	{
		__result = true;
	}
}
[HarmonyPatch(typeof(LocString), "GetFormattedText")]
internal static class RelicIntervalDescriptionPatch
{
	private static void Postfix(LocString __instance, ref string __result)
	{
		if (!(__instance.LocTable != "relics"))
		{
			if (__instance.LocEntryKey == "ICE_CREAM.description" || __instance.LocEntryKey == "ICE_CREAM.eventDescription")
			{
				__result = "敌人行动间隔增加 33%";
			}
			else if (__instance.LocEntryKey == "RUNIC_PYRAMID.description" || __instance.LocEntryKey == "RUNIC_PYRAMID.eventDescription")
			{
				__result = "敌人行动间隔增加 50%";
			}
		}
	}
}
internal static class RemoteOrbitFxSync
{
	private const float BroadcastIntervalSec = 0.25f;

	private const float BladeRadius = 300f;

	private const float MarkerSize = 18f;

	private static readonly Dictionary<ulong, int> RemoteBlades = new Dictionary<ulong, int>();

	private static readonly Dictionary<(ulong Peer, int Slot), ColorRect> Markers = new Dictionary<(ulong, int), ColorRect>();

	private static readonly List<(ulong Peer, int Slot)> DeadKeys = new List<(ulong, int)>();

	private static int _localBlades;

	private static float _broadcastAcc;

	private static double _phase;

	private static bool _hooked;

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
			}
		}
	}

	public static void OnCombatEnded()
	{
		RemoteBlades.Clear();
		_localBlades = 0;
		ClearMarkers();
		_broadcastAcc = 0f;
		_phase = 0.0;
	}

	public static void SetLocalBlades(int count)
	{
		_localBlades = Math.Clamp(count, 0, 16);
	}

	public static void SetLocalOrbs(int count)
	{
	}

	public static void OnRemote(ActionGameNetMessage msg)
	{
		if (ActionGameNet.IsOnline && msg.CasterNetId != 0L && msg.CasterNetId != ActionGameNet.LocalNetId)
		{
			RemoteBlades[msg.CasterNetId] = Math.Max(0, msg.IntA);
		}
	}

	private static void OnProcessFrame()
	{
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsOverOrEnding)
		{
			if (Markers.Count > 0 || RemoteBlades.Count > 0)
			{
				OnCombatEnded();
			}
		}
		else
		{
			if (!ActionGameNet.IsOnline)
			{
				return;
			}
			NCombatRoom instance2 = NCombatRoom.Instance;
			if (instance2 == null)
			{
				return;
			}
			float num = (float)((Node)instance2).GetProcessDeltaTime();
			if (num <= 0f)
			{
				num = 1f / 60f;
			}
			_phase += (double)num * 2.2;
			_broadcastAcc += num;
			if (_broadcastAcc >= 0.25f)
			{
				_broadcastAcc = 0f;
				ActionGameNet.Send(new ActionGameNetMessage
				{
					Opcode = 8,
					CasterNetId = ActionGameNet.LocalNetId,
					IntA = _localBlades,
					IntB = 0
				});
			}
			HashSet<(ulong, int)> hashSet = new HashSet<(ulong, int)>();
			foreach (KeyValuePair<ulong, int> remoteBlade in RemoteBlades)
			{
				if (remoteBlade.Value > 0 && CombatPeerUtil.TryGetPose(remoteBlade.Key, out var pos))
				{
					Vector2 center = pos + new Vector2(0f, -72f);
					DrawRing(remoteBlade.Key, remoteBlade.Value, center, 300f, new Color(1f, 0.75f, 0.2f, 0.55f), (Node)(object)instance2, hashSet);
				}
			}
			DeadKeys.Clear();
			foreach (var key in Markers.Keys)
			{
				if (!hashSet.Contains(key))
				{
					DeadKeys.Add(key);
				}
			}
			foreach (var deadKey in DeadKeys)
			{
				if (Markers.Remove(deadKey, out ColorRect value) && value != null && GodotObject.IsInstanceValid((GodotObject)(object)value))
				{
					((Node)value).QueueFree();
				}
			}
		}
	}

	private static void DrawRing(ulong peer, int count, Vector2 center, float radius, Color color, Node room, HashSet<(ulong, int)> live)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected O, but got Unknown
		for (int i = 0; i < count; i++)
		{
			live.Add((peer, i));
			float num = (float)((_phase + (double)i / (double)count) % 1.0);
			if (num < 0f)
			{
				num += 1f;
			}
			float num2 = num * ((float)Math.PI * 2f);
			Vector2 val = center + new Vector2(Mathf.Cos(num2), Mathf.Sin(num2)) * radius;
			if (!Markers.TryGetValue((peer, i), out ColorRect value) || value == null || !GodotObject.IsInstanceValid((GodotObject)(object)value))
			{
				value = new ColorRect
				{
					Name = StringName.op_Implicit("ActionGameRemoteBlade"),
					MouseFilter = (MouseFilterEnum)2,
					Color = color,
					Size = new Vector2(18f, 18f),
					ZIndex = 180
				};
				((CanvasItem)value).ZAsRelative = false;
				room.AddChild((Node)(object)value, false, (InternalMode)0);
				Markers[(peer, i)] = value;
			}
			value.Color = color;
			((Control)value).GlobalPosition = val - ((Control)value).Size * 0.5f;
		}
	}

	private static void ClearMarkers()
	{
		foreach (ColorRect value in Markers.Values)
		{
			if (value != null && GodotObject.IsInstanceValid((GodotObject)(object)value))
			{
				((Node)value).QueueFree();
			}
		}
		Markers.Clear();
	}
}
internal static class RetainRewrite
{
	public const string Description = "打出后回到手牌，并失去保留。";
}
[HarmonyPatch(typeof(CardModel), "GetResultPileTypeForCardPlay")]
internal static class RetainReturnToHandPatch
{
	private static void Postfix(CardModel __instance, ref PileType __result)
	{
		if ((int)__result == 3 && __instance.Keywords.Contains((CardKeyword)5))
		{
			__result = (PileType)2;
			__instance.RemoveKeyword((CardKeyword)5);
		}
	}
}
[HarmonyPatch(typeof(LocString), "GetFormattedText")]
internal static class RetainDescriptionPatch
{
	private static void Postfix(LocString __instance, ref string __result)
	{
		if (__instance.LocTable == "card_keywords" && __instance.LocEntryKey == "RETAIN.description")
		{
			__result = "打出后回到手牌，并失去保留。";
		}
	}
}
[HarmonyPatch(typeof(RunSaveManager), "SaveRun", new Type[] { typeof(AbstractRoom) })]
internal static class SaveRunSanitizePatch
{
	private static void Prefix()
	{
		SanitizeBeforeSave();
	}

	internal static void SanitizeBeforeSave()
	{
		RealtimeCombatTicker.StopEnemyActions();
		Log.Info("[local.action_game] SaveRun sanitize", 2);
	}
}
[HarmonyPatch(typeof(RunSaveManager), "SaveRun", new Type[]
{
	typeof(SerializableRun),
	typeof(bool)
})]
internal static class SaveRunSerializeSanitizePatch
{
	private static void Prefix()
	{
		SaveRunSanitizePatch.SanitizeBeforeSave();
	}
}
[HarmonyPatch(typeof(RunSaveManager), "LoadRunSave")]
internal static class LoadRunSanitizePatch
{
	private static void Postfix(ref ReadSaveResult<SerializableRun> __result)
	{
		if (__result != null && __result.Success && __result.SaveData != null)
		{
			SerializableRoom preFinishedRoom = __result.SaveData.PreFinishedRoom;
			if (preFinishedRoom != null && preFinishedRoom.ShouldResumeParentEvent && preFinishedRoom.ParentEventId == (ModelId)null)
			{
				Log.Warn("[local.action_game] repaired broken pre_finished_room (should_resume_parent_event with null parent)", 2);
				preFinishedRoom.ShouldResumeParentEvent = false;
			}
		}
	}
}
internal static class SelfDamageDemonFormSystem
{
	public const int Threshold = 7;

	private static readonly Dictionary<Creature, int> Counts = new Dictionary<Creature, int>();

	private static readonly HashSet<Creature> Granted = new HashSet<Creature>();

	public static void OnCombatStarted()
	{
		Reset();
	}

	public static void OnCombatEnded()
	{
		Reset();
	}

	public static void Reset()
	{
		Counts.Clear();
		Granted.Clear();
	}

	public static async Task OnAfterDamageReceived(Task original, PlayerChoiceContext choiceContext, Creature target, DamageResult result, CardModel cardSource)
	{
		if (original != null)
		{
			await original;
		}
		if (target == null || !target.IsAlive || !target.IsPlayer || cardSource == null)
		{
			return;
		}
		Player owner = cardSource.Owner;
		if (((owner != null) ? owner.Creature : null) != target || result == null || result.UnblockedDamage <= 0 || Granted.Contains(target))
		{
			return;
		}
		Counts.TryGetValue(target, out var count);
		count++;
		Counts[target] = count;
		Log.Info($"[{"local.action_game"}] card self-damage {count}/{7} ({((AbstractModel)cardSource).Id}) on {target.LogName}", 2);
		if (count >= 7)
		{
			Granted.Add(target);
			PlayerChoiceContext ctx = (PlayerChoiceContext)(((object)choiceContext) ?? ((object)new BlockingPlayerChoiceContext()));
			Player player = target.Player;
			float? obj;
			if (player == null)
			{
				obj = null;
			}
			else
			{
				CharacterModel character = player.Character;
				obj = ((character != null) ? new float?(character.PowerUpAnimDelay) : null);
			}
			float? num = obj;
			float delay = num.GetValueOrDefault();
			await CreatureCmd.TriggerAnim(target, "PowerUp", delay);
			TriggerDemonFormVfx(await PowerCmd.Apply<DemonFormPower>(ctx, target, 1m, target, cardSource, false));
			Log.Info($"[{"local.action_game"}] Demon Form granted after {7} card self-hits", 2);
		}
	}

	private static void TriggerDemonFormVfx(DemonFormPower? power)
	{
		if (power != null)
		{
			object value = Traverse.Create((object)power).Property("Vfx", (object[])null).GetValue();
			if (value != null)
			{
				AccessTools.Method(value.GetType(), "OnEffectTriggered", (Type[])null, (Type[])null)?.Invoke(value, null);
			}
		}
	}
}
[HarmonyPatch(typeof(Hook), "AfterDamageReceived")]
internal static class SelfDamageDemonFormPatch
{
	private static void Postfix(PlayerChoiceContext choiceContext, Creature target, DamageResult result, CardModel cardSource, ref Task __result)
	{
		__result = SelfDamageDemonFormSystem.OnAfterDamageReceived(__result, choiceContext, target, result, cardSource);
	}
}
[HarmonyPatch]
internal static class ShuffleVfxMutePatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(NCardFlyShuffleVfx), "PlayAnim", (Type[])null, (Type[])null);
	}

	private static bool Prefix(NCardFlyShuffleVfx __instance, ref Task __result)
	{
		if (__instance != null && GodotObject.IsInstanceValid((GodotObject)(object)__instance))
		{
			((CanvasItem)__instance).Visible = false;
			((Node)__instance).QueueFree();
		}
		__result = Task.CompletedTask;
		return false;
	}
}
internal static class SovereignBladeOrbitSystem
{
	private const float VanillaOrbitSpeed = 60f;

	private const float TargetOrbitSpeed = 450f;

	private const float HitCooldownSec = 1f;

	private const float FacingOffsetRad = 0f;

	private const float CenterHeightFactor = 0.6f;

	private const float RadiusHeightFactor = 0.95f;

	private const float MinRadius = 320f;

	private const float HitLength = 110f;

	private const float HitWidth = 28f;

	private const float HitInwardScale = 1.3f;

	private const string LayerName = "ActionGameSovereignBladeLayer";

	private static bool _hooked;

	private static Node2D? _layer;

	private static double _sharedPhase;

	private static readonly Dictionary<(ulong Blade, ulong Enemy), float> HitCooldowns = new Dictionary<(ulong, ulong), float>();

	private static readonly Dictionary<ulong, Polygon2D> DebugPolys = new Dictionary<ulong, Polygon2D>();

	private static readonly List<(ulong Blade, ulong Enemy)> _deadCooldownKeys = new List<(ulong, ulong)>();

	private static readonly List<ulong> _deadPolyKeys = new List<ulong>();

	private static readonly List<NSovereignBladeVfx> _bladeScratch = new List<NSovereignBladeVfx>();

	private static readonly FieldRef<NSovereignBladeVfx, Node2D>? SpineRef = AccessTools.FieldRefAccess<NSovereignBladeVfx, Node2D>("_spineNode");

	private static readonly FieldRef<NSovereignBladeVfx, Control>? HitboxRef = AccessTools.FieldRefAccess<NSovereignBladeVfx, Control>("_hitbox");

	private static readonly FieldRef<NSovereignBladeVfx, Path2D>? OrbitPathRef = AccessTools.FieldRefAccess<NSovereignBladeVfx, Path2D>("_orbitPath");

	private static readonly FieldRef<NSovereignBladeVfx, bool>? IsAttackingRef = AccessTools.FieldRefAccess<NSovereignBladeVfx, bool>("_isAttacking");

	public static void EnsureHook()
	{
		if (!_hooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnProcessFrame;
				_hooked = true;
			}
		}
	}

	public static NSovereignBladeVfx? FindDetachedBlade(Player player, CardModel card)
	{
		if (player == null || card == null)
		{
			return null;
		}
		EnsureLayer();
		if (_layer == null || !GodotObject.IsInstanceValid((GodotObject)(object)_layer))
		{
			return null;
		}
		CardModel val = card.DupeOf ?? card;
		foreach (Node child in ((Node)_layer).GetChildren(false))
		{
			NSovereignBladeVfx val2 = (NSovereignBladeVfx)(object)((child is NSovereignBladeVfx) ? child : null);
			if (val2 == null || !GodotObject.IsInstanceValid((GodotObject)(object)val2))
			{
				continue;
			}
			CardModel card2 = val2.Card;
			if (card2 != null)
			{
				if (card2 == val || card2 == card)
				{
					return val2;
				}
				if ((card2.DupeOf ?? card2) == val)
				{
					return val2;
				}
			}
		}
		return null;
	}

	private static void OnProcessFrame()
	{
		CombatManager instance = CombatManager.Instance;
		if (instance == null || !instance.IsInProgress || instance.IsOverOrEnding)
		{
			ClearCombatState();
		}
		else
		{
			if (PlayerChoicePause.IsPaused)
			{
				return;
			}
			NCombatRoom instance2 = NCombatRoom.Instance;
			if (instance2 == null)
			{
				return;
			}
			CombatState val = instance.DebugOnlyGetState();
			if (val == null)
			{
				return;
			}
			Player me = LocalContext.GetMe((ICombatState)(object)val);
			Creature val2 = ((me != null) ? me.Creature : null);
			if (val2 == null || !val2.IsAlive)
			{
				return;
			}
			NCreature creatureNode = instance2.GetCreatureNode(val2);
			if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				float num = (float)((Node)creatureNode).GetProcessDeltaTime();
				if (num <= 0f)
				{
					num = 1f / 60f;
				}
				TickCooldowns(num);
				EnsureLayer(instance2);
				_sharedPhase += (double)(450f * num) / 400.0;
				CollectAndSyncBlades(creatureNode, instance2);
				RemoteOrbitFxSync.SetLocalBlades(_bladeScratch.Count);
				ResolveHits(val2, instance2);
			}
		}
	}

	private static void EnsureLayer(NCombatRoom? room = null)
	{
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		if (room == null)
		{
			room = NCombatRoom.Instance;
		}
		if (room != null && (_layer == null || !GodotObject.IsInstanceValid((GodotObject)(object)_layer) || ((Node)_layer).GetParent() != room))
		{
			if (_layer != null && GodotObject.IsInstanceValid((GodotObject)(object)_layer))
			{
				((Node)_layer).QueueFree();
			}
			_layer = new Node2D
			{
				Name = StringName.op_Implicit("ActionGameSovereignBladeLayer"),
				ZIndex = 50
			};
			((CanvasItem)_layer).ZAsRelative = false;
			((Node)room).AddChild((Node)(object)_layer, false, (InternalMode)0);
		}
	}

	private static void CollectAndSyncBlades(NCreature playerNode, NCombatRoom room)
	{
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0112: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		if (_layer == null || !GodotObject.IsInstanceValid((GodotObject)(object)_layer))
		{
			return;
		}
		_bladeScratch.Clear();
		foreach (Node child in ((Node)playerNode).GetChildren(false))
		{
			NSovereignBladeVfx val = (NSovereignBladeVfx)(object)((child is NSovereignBladeVfx) ? child : null);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				DetachToLayer(val);
			}
		}
		foreach (Node child2 in ((Node)_layer).GetChildren(false))
		{
			NSovereignBladeVfx val2 = (NSovereignBladeVfx)(object)((child2 is NSovereignBladeVfx) ? child2 : null);
			if (val2 != null && GodotObject.IsInstanceValid((GodotObject)(object)val2))
			{
				_bladeScratch.Add(val2);
			}
		}
		_bladeScratch.Sort((NSovereignBladeVfx a, NSovereignBladeVfx b) => ((GodotObject)a).GetInstanceId().CompareTo(((GodotObject)b).GetInstanceId()));
		Vector2 val3 = OrbitCenter(playerNode);
		int count = _bladeScratch.Count;
		for (int i = 0; i < count; i++)
		{
			NSovereignBladeVfx val4 = _bladeScratch[i];
			((Node2D)val4).GlobalPosition = val3;
			EnsureVisibleScale(val4);
			SnapOrbit(val4, val3, i, count);
			SyncHoverHitbox(val4);
		}
	}

	private static void DetachToLayer(NSovereignBladeVfx blade)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if (_layer != null && GodotObject.IsInstanceValid((GodotObject)(object)_layer) && ((Node)blade).GetParent() != _layer)
		{
			Vector2 globalPosition = ((Node2D)blade).GlobalPosition;
			((Node)blade).Reparent((Node)(object)_layer, true);
			((Node2D)blade).GlobalPosition = globalPosition;
			EnsureVisibleScale(blade);
		}
	}

	private static void EnsureVisibleScale(NSovereignBladeVfx blade)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		if (SpineRef == null)
		{
			return;
		}
		Node2D val = SpineRef.Invoke(blade);
		if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val) && (!(val.Scale.X > 0.15f) || !(val.Scale.Y > 0.15f)))
		{
			float num = 10f;
			CardModel card = blade.Card;
			object obj;
			if (card == null)
			{
				obj = null;
			}
			else
			{
				DynamicVarSet dynamicVars = card.DynamicVars;
				obj = ((dynamicVars != null) ? dynamicVars.Damage : null);
			}
			if (obj != null)
			{
				num = (float)((DynamicVar)blade.Card.DynamicVars.Damage).BaseValue;
			}
			float num2 = Mathf.Clamp(Mathf.Lerp(0f, 1f, num / 200f), 0f, 1f);
			float num3 = Mathf.Lerp(0.9f, 2f, num2);
			val.Scale = Vector2.One * num3;
		}
	}

	private static Vector2 OrbitCenter(NCreature playerNode)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		float num = ((Control)playerNode).Size.Y;
		if (num < 40f)
		{
			num = 120f;
		}
		return ((Control)playerNode).GlobalPosition + new Vector2(0f, (0f - num) * 0.6f);
	}

	private static float OrbitRadius(NCreature playerNode)
	{
		return 320f;
	}

	internal static void SnapOrbit(NSovereignBladeVfx blade, Vector2 center, int index, int count)
	{
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		if (SpineRef == null || (IsAttackingRef != null && IsAttackingRef.Invoke(blade)))
		{
			return;
		}
		Node2D val = SpineRef.Invoke(blade);
		if (val == null || !GodotObject.IsInstanceValid((GodotObject)(object)val))
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		CardModel card = blade.Card;
		object obj;
		if (card == null)
		{
			obj = null;
		}
		else
		{
			Player owner = card.Owner;
			obj = ((owner != null) ? owner.Creature : null);
		}
		Creature val2 = (Creature)obj;
		float num = 320f;
		if (val2 != null && instance != null)
		{
			NCreature creatureNode = instance.GetCreatureNode(val2);
			if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				num = OrbitRadius(creatureNode);
			}
		}
		if (count < 1)
		{
			count = 1;
		}
		if (index < 0)
		{
			index = 0;
		}
		float num2 = (float)((_sharedPhase + (double)index / (double)count) % 1.0);
		if (num2 < 0f)
		{
			num2 += 1f;
		}
		float num3 = num2 * ((float)Math.PI * 2f);
		Vector2 val3 = new Vector2(Mathf.Cos(num3), Mathf.Sin(num3)) * num;
		val.GlobalPosition = center + val3;
		val.Rotation = num3 + 0f;
		blade.OrbitProgress = num2;
	}

	private static void SyncHoverHitbox(NSovereignBladeVfx blade)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		if (TryGetBladeObb(blade, out var center, out var angle, out var halfExtents) && HitboxRef != null)
		{
			Control val = HitboxRef.Invoke(blade);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				Vector2 val3 = (val.Size = halfExtents * 2f);
				val.PivotOffset = val3 * 0.5f;
				val.Rotation = angle;
				val.GlobalPosition = center - val3 * 0.5f;
			}
		}
	}

	private static void ResolveHits(Creature player, NCombatRoom room)
	{
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		List<NCreature> list = new List<NCreature>(8);
		foreach (NCreature creatureNode in room.CreatureNodes)
		{
			if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode))
			{
				list.Add(creatureNode);
			}
		}
		foreach (NSovereignBladeVfx item in _bladeScratch)
		{
			if (item.Card == null || (IsAttackingRef != null && IsAttackingRef.Invoke(item)) || !TryGetBladeObb(item, out var center, out var angle, out var halfExtents))
			{
				continue;
			}
			ShowDebugObb(item, center, angle, halfExtents, (Node)(object)room);
			ulong instanceId = ((GodotObject)item).GetInstanceId();
			foreach (NCreature item2 in list)
			{
				if (!GodotObject.IsInstanceValid((GodotObject)(object)item2))
				{
					continue;
				}
				Creature entity = item2.Entity;
				if (entity == null || !entity.IsAlive || !entity.IsMonster)
				{
					continue;
				}
				Rect2 aabb = BodyRect(item2);
				if (!ObbIntersectsAabb(center, angle, halfExtents, aabb))
				{
					continue;
				}
				(ulong, ulong) key = (instanceId, ((GodotObject)item2).GetInstanceId());
				if (!HitCooldowns.TryGetValue(key, out var value) || !(value > 0f))
				{
					HitCooldowns[key] = 1f;
					decimal num = decimal.Round(((DynamicVar)item.Card.DynamicVars.Damage).BaseValue / 5m, MidpointRounding.AwayFromZero);
					if (!(num <= 0m))
					{
						DealDamageAsync(player, entity, num);
					}
				}
			}
		}
		_deadPolyKeys.Clear();
		HashSet<ulong> hashSet = new HashSet<ulong>();
		foreach (NSovereignBladeVfx item3 in _bladeScratch)
		{
			hashSet.Add(((GodotObject)item3).GetInstanceId());
		}
		foreach (KeyValuePair<ulong, Polygon2D> debugPoly in DebugPolys)
		{
			if (!hashSet.Contains(debugPoly.Key))
			{
				_deadPolyKeys.Add(debugPoly.Key);
			}
		}
		foreach (ulong deadPolyKey in _deadPolyKeys)
		{
			if (DebugPolys.Remove(deadPolyKey, out Polygon2D value2) && value2 != null && GodotObject.IsInstanceValid((GodotObject)(object)value2))
			{
				((Node)value2).QueueFree();
			}
		}
	}

	private static bool TryGetBladeObb(NSovereignBladeVfx blade, out Vector2 center, out float angle, out Vector2 halfExtents)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		center = default(Vector2);
		angle = 0f;
		halfExtents = default(Vector2);
		if (SpineRef == null)
		{
			return false;
		}
		Node2D val = SpineRef.Invoke(blade);
		if (val == null || !GodotObject.IsInstanceValid((GodotObject)(object)val))
		{
			return false;
		}
		angle = val.Rotation;
		float num = 143f;
		float num2 = num - 110f;
		Vector2 val2 = default(Vector2);
		((Vector2)(ref val2))..ctor(0f - Mathf.Cos(angle), 0f - Mathf.Sin(angle));
		center = val.GlobalPosition + val2 * (num2 * 0.5f);
		halfExtents = new Vector2(num * 0.5f, 14f);
		return true;
	}

	private static bool ObbIntersectsAabb(Vector2 obbCenter, float angle, Vector2 halfExtents, Rect2 aabb)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		Vector2[] array = ObbCorners(obbCenter, angle, halfExtents);
		for (int i = 0; i < 4; i++)
		{
			if (((Rect2)(ref aabb)).HasPoint(array[i]))
			{
				return true;
			}
		}
		Vector2[] array2 = (Vector2[])(object)new Vector2[4]
		{
			((Rect2)(ref aabb)).Position,
			((Rect2)(ref aabb)).Position + new Vector2(((Rect2)(ref aabb)).Size.X, 0f),
			((Rect2)(ref aabb)).Position + ((Rect2)(ref aabb)).Size,
			((Rect2)(ref aabb)).Position + new Vector2(0f, ((Rect2)(ref aabb)).Size.Y)
		};
		for (int j = 0; j < 4; j++)
		{
			if (PointInObb(array2[j], obbCenter, angle, halfExtents))
			{
				return true;
			}
		}
		for (int k = 0; k < 4; k++)
		{
			Vector2 a = array[k];
			Vector2 b = array[(k + 1) % 4];
			for (int l = 0; l < 4; l++)
			{
				Vector2 c = array2[l];
				Vector2 d = array2[(l + 1) % 4];
				if (SegmentsIntersect(a, b, c, d))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static Vector2[] ObbCorners(Vector2 center, float angle, Vector2 halfExtents)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Cos(angle);
		float num2 = Mathf.Sin(angle);
		Vector2 val = new Vector2(num, num2) * halfExtents.X;
		Vector2 val2 = new Vector2(0f - num2, num) * halfExtents.Y;
		return (Vector2[])(object)new Vector2[4]
		{
			center + val + val2,
			center + val - val2,
			center - val - val2,
			center - val + val2
		};
	}

	private static bool PointInObb(Vector2 point, Vector2 center, float angle, Vector2 halfExtents)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = point - center;
		float num = Mathf.Cos(0f - angle);
		float num2 = Mathf.Sin(0f - angle);
		Vector2 val2 = default(Vector2);
		((Vector2)(ref val2))..ctor(val.X * num - val.Y * num2, val.X * num2 + val.Y * num);
		return Mathf.Abs(val2.X) <= halfExtents.X && Mathf.Abs(val2.Y) <= halfExtents.Y;
	}

	private static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = b - a;
		Vector2 val2 = c - a;
		Vector2 val3 = d - a;
		Vector2 val4 = d - c;
		Vector2 val5 = a - c;
		Vector2 val6 = b - c;
		float num = val.X * val2.Y - val.Y * val2.X;
		float num2 = val.X * val3.Y - val.Y * val3.X;
		float num3 = val4.X * val5.Y - val4.Y * val5.X;
		float num4 = val4.X * val6.Y - val4.Y * val6.X;
		return num * num2 < 0f && num3 * num4 < 0f;
	}

	private static async Task DealDamageAsync(Creature dealer, Creature target, decimal amount)
	{
		CombatManager manager = CombatManager.Instance;
		if (manager != null && manager.IsInProgress && !manager.IsOverOrEnding && dealer != null && dealer.IsAlive && target != null && target.IsAlive)
		{
			BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
			await CreatureCmd.Damage((PlayerChoiceContext)(object)ctx, target, amount, (ValueProp)8, dealer);
			OrbitHitNetSync.BroadcastHit(amount, 1);
		}
	}

	private static void TickCooldowns(float delta)
	{
		if (HitCooldowns.Count == 0)
		{
			return;
		}
		_deadCooldownKeys.Clear();
		foreach (KeyValuePair<(ulong, ulong), float> hitCooldown in HitCooldowns)
		{
			float num = hitCooldown.Value - delta;
			if (num <= 0f)
			{
				_deadCooldownKeys.Add(hitCooldown.Key);
			}
			else
			{
				HitCooldowns[hitCooldown.Key] = num;
			}
		}
		foreach (var deadCooldownKey in _deadCooldownKeys)
		{
			HitCooldowns.Remove(deadCooldownKey);
		}
	}

	private static Rect2 BodyRect(NCreature node)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Unknown result type (might be due to invalid IL or missing references)
		Vector2 size = ((Control)node).Size;
		if (size.X < 8f || size.Y < 8f)
		{
			((Vector2)(ref size))..ctor(80f, 120f);
		}
		Vector2 globalPosition = ((Control)node).GlobalPosition;
		float num = size.X * 0.8f;
		float num2 = size.Y * 0.4f;
		return new Rect2(new Vector2(globalPosition.X - num * 0.5f, globalPosition.Y - num2), new Vector2(num, num2));
	}

	private static void ShowDebugObb(NSovereignBladeVfx blade, Vector2 center, float angle, Vector2 half, Node parent)
	{
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		ulong instanceId = ((GodotObject)blade).GetInstanceId();
		if (!DebugPolys.TryGetValue(instanceId, out Polygon2D value) || value == null || !GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			value = new Polygon2D
			{
				Name = StringName.op_Implicit("ActionGameSovereignHitObb"),
				ZIndex = 90,
				Color = new Color(1f, 0.85f, 0.2f, 0.4f)
			};
			((CanvasItem)value).ZAsRelative = false;
			parent.AddChild((Node)(object)value, false, (InternalMode)0);
			DebugPolys[instanceId] = value;
		}
		value.Polygon = ObbCorners(center, angle, half);
		((CanvasItem)value).Visible = RealtimeCombatSettings.ShowAttackBoxes;
	}

	private static void ClearCombatState()
	{
		HitCooldowns.Clear();
		_sharedPhase = 0.0;
		foreach (Polygon2D value in DebugPolys.Values)
		{
			if (value != null && GodotObject.IsInstanceValid((GodotObject)(object)value))
			{
				((Node)value).QueueFree();
			}
		}
		DebugPolys.Clear();
		if (_layer != null && !GodotObject.IsInstanceValid((GodotObject)(object)_layer))
		{
			_layer = null;
		}
		if (NCombatRoom.Instance == null)
		{
			_layer = null;
		}
		_bladeScratch.Clear();
	}

	internal static void BoostOrbitAndFace(NSovereignBladeVfx blade, double delta)
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		if (blade == null || !GodotObject.IsInstanceValid((GodotObject)(object)blade))
		{
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		CardModel card = blade.Card;
		object obj;
		if (card == null)
		{
			obj = null;
		}
		else
		{
			Player owner = card.Owner;
			obj = ((owner != null) ? owner.Creature : null);
		}
		Creature val = (Creature)obj;
		if (val != null && instance != null)
		{
			NCreature creatureNode = instance.GetCreatureNode(val);
			if (creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)creatureNode) && TryGetBladeIndex(blade, out var index, out var count))
			{
				SnapOrbit(blade, OrbitCenter(creatureNode), index, count);
			}
		}
	}

	private static bool TryGetBladeIndex(NSovereignBladeVfx blade, out int index, out int count)
	{
		index = 0;
		count = 0;
		if (_layer == null || !GodotObject.IsInstanceValid((GodotObject)(object)_layer))
		{
			return false;
		}
		List<NSovereignBladeVfx> list = new List<NSovereignBladeVfx>();
		foreach (Node child in ((Node)_layer).GetChildren(false))
		{
			NSovereignBladeVfx val = (NSovereignBladeVfx)(object)((child is NSovereignBladeVfx) ? child : null);
			if (val != null && GodotObject.IsInstanceValid((GodotObject)(object)val))
			{
				list.Add(val);
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		list.Sort((NSovereignBladeVfx a, NSovereignBladeVfx b) => ((GodotObject)a).GetInstanceId().CompareTo(((GodotObject)b).GetInstanceId()));
		count = list.Count;
		index = list.IndexOf(blade);
		return index >= 0;
	}
}
[HarmonyPatch(typeof(NSovereignBladeVfx), "_Process")]
internal static class SovereignBladeProcessPatch
{
	private static void Postfix(NSovereignBladeVfx __instance, double delta)
	{
		SovereignBladeOrbitSystem.BoostOrbitAndFace(__instance, delta);
	}
}
[HarmonyPatch(typeof(SovereignBlade), "GetVfxNode")]
internal static class SovereignBladeGetVfxNodePatch
{
	private static void Postfix(Player player, CardModel card, ref NSovereignBladeVfx? __result)
	{
		if (__result == null || !GodotObject.IsInstanceValid((GodotObject)(object)__result))
		{
			__result = SovereignBladeOrbitSystem.FindDetachedBlade(player, card);
		}
	}
}
internal enum SpawnTierKind
{
	Weak,
	Strong
}
internal readonly record struct SpawnEntry(Func<MonsterModel> Create, bool IsGroup);
internal static class SpawnCatalog
{
	private sealed class ActPools
	{
		public SpawnEntry[] Weak = Array.Empty<SpawnEntry>();

		public SpawnEntry[] Strong = Array.Empty<SpawnEntry>();
	}

	private static HashSet<Type> _weakTypes = null;

	private static readonly ActPools _fallback = new ActPools
	{
		Weak = new SpawnEntry[3]
		{
			new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<LeafSlimeS>(), IsGroup: true),
			new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<TwigSlimeS>(), IsGroup: true),
			new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<FatGremlin>(), IsGroup: true)
		},
		Strong = new SpawnEntry[3]
		{
			new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Seapunk>(), IsGroup: false),
			new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<FatGremlin>(), IsGroup: true),
			new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Nibbit>(), IsGroup: false)
		}
	};

	private static readonly Dictionary<string, ActPools> _byAct = new Dictionary<string, ActPools>
	{
		["Underdocks"] = new ActPools
		{
			Weak = new SpawnEntry[8]
			{
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<CorpseSlug>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Seapunk>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<SludgeSpinner>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Toadpole>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Guardbot>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Noisebot>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Stabbot>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Zapbot>(), IsGroup: true)
			},
			Strong = new SpawnEntry[14]
			{
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<CalcifiedCultist>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<CorpseSlug>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<DampCultist>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<FatGremlin>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<FossilStalker>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<GasBomb>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<GremlinMerc>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<HauntedShip>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<LivingFog>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<PunchConstruct>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Seapunk>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<SewerClam>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<SneakyGremlin>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<TwoTailedRat>(), IsGroup: false)
			}
		},
		["Overgrowth"] = new ActPools
		{
			Weak = new SpawnEntry[7]
			{
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<FuzzyWurmCrawler>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<LeafSlimeM>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<LeafSlimeS>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Nibbit>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<ShrinkerBeetle>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<TwigSlimeM>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<TwigSlimeS>(), IsGroup: true)
			},
			Strong = new SpawnEntry[21]
			{
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<AssassinRubyRaider>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<AxeRubyRaider>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<BruteRubyRaider>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<CrossbowRubyRaider>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<CubexConstruct>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<EyeWithTeeth>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Flyconid>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Fogmog>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<FuzzyWurmCrawler>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Inklet>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<LeafSlimeM>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<LeafSlimeS>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Mawler>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Nibbit>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<ShrinkerBeetle>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<SlitheringStrangler>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<SnappingJaxfruit>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<TrackerRubyRaider>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<TwigSlimeM>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<TwigSlimeS>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<VineShambler>(), IsGroup: false)
			}
		},
		["Hive"] = new ActPools
		{
			Weak = new SpawnEntry[6]
			{
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<BowlbugEgg>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<BowlbugNectar>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<BowlbugRock>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Exoskeleton>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<ThievingHopper>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Tunneler>(), IsGroup: false)
			},
			Strong = new SpawnEntry[16]
			{
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<BowlbugEgg>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<BowlbugNectar>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<BowlbugRock>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<BowlbugSilk>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Chomper>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Exoskeleton>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<HunterKiller>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<LouseProgenitor>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Myte>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Ovicopter>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Parafright>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<SlumberingBeetle>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<SpinyToad>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<TheObscura>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<ToughEgg>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Tunneler>(), IsGroup: false)
			}
		},
		["Glory"] = new ActPools
		{
			Weak = new SpawnEntry[4]
			{
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<DevotedSculptor>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<LivingShield>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<ScrollOfBiting>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<TurretOperator>(), IsGroup: true)
			},
			Strong = new SpawnEntry[11]
			{
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Axebot>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<CubexConstruct>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<Fabricator>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<FrogKnight>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<GlobeHead>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<OwlMagistrate>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<PunchConstruct>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<ScrollOfBiting>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<SlimedBerserker>(), IsGroup: false),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<TheForgotten>(), IsGroup: true),
				new SpawnEntry(() => (MonsterModel)(object)ModelDb.Monster<TheLost>(), IsGroup: true)
			}
		}
	};

	private static List<SpawnEntry>? _netEntries;

	private static Dictionary<string, int>? _netIds;

	public static IReadOnlyList<SpawnEntry> GetPool(string actName, SpawnTierKind tier)
	{
		if (!_byAct.TryGetValue(actName, out ActPools value))
		{
			value = _fallback;
		}
		return (tier == SpawnTierKind.Weak) ? value.Weak : value.Strong;
	}

	public static bool IsWeakMonster(MonsterModel monster)
	{
		EnsureWeakTypes();
		return _weakTypes.Contains(((object)monster).GetType());
	}

	private static void EnsureWeakTypes()
	{
		if (_weakTypes != null)
		{
			return;
		}
		_weakTypes = new HashSet<Type>();
		Add(_fallback);
		foreach (ActPools value in _byAct.Values)
		{
			Add(value);
		}
		static void Add(ActPools pools)
		{
			SpawnEntry[] weak = pools.Weak;
			foreach (SpawnEntry spawnEntry in weak)
			{
				MonsterModel val = spawnEntry.Create();
				if (val != null)
				{
					_weakTypes.Add(((object)val).GetType());
				}
			}
		}
	}

	public static bool TryGetNetId(SpawnEntry entry, out int id)
	{
		EnsureNetIds();
		id = -1;
		MonsterModel val = entry.Create();
		if (val == null || _netIds == null)
		{
			return false;
		}
		string key = NetKey(((object)val).GetType(), entry.IsGroup);
		return _netIds.TryGetValue(key, out id);
	}

	public static bool TryGetByNetId(int id, out SpawnEntry entry)
	{
		EnsureNetIds();
		entry = default(SpawnEntry);
		if (_netEntries == null || id < 0 || id >= _netEntries.Count)
		{
			return false;
		}
		entry = _netEntries[id];
		return true;
	}

	private static string NetKey(Type type, bool isGroup)
	{
		return type.FullName + (isGroup ? "#g" : "#s");
	}

	private static void EnsureNetIds()
	{
		if (_netEntries != null)
		{
			return;
		}
		_netEntries = new List<SpawnEntry>(64);
		_netIds = new Dictionary<string, int>(64);
		SpawnEntry[] weak = _fallback.Weak;
		foreach (SpawnEntry entry2 in weak)
		{
			Add(entry2);
		}
		SpawnEntry[] strong = _fallback.Strong;
		foreach (SpawnEntry entry3 in strong)
		{
			Add(entry3);
		}
		string[] array = new string[4] { "Underdocks", "Overgrowth", "Hive", "Glory" };
		foreach (string key in array)
		{
			if (_byAct.TryGetValue(key, out ActPools value))
			{
				SpawnEntry[] weak2 = value.Weak;
				foreach (SpawnEntry entry4 in weak2)
				{
					Add(entry4);
				}
				SpawnEntry[] strong2 = value.Strong;
				foreach (SpawnEntry entry5 in strong2)
				{
					Add(entry5);
				}
			}
		}
		static void Add(SpawnEntry entry)
		{
			MonsterModel val = entry.Create();
			if (val != null && _netIds != null && _netEntries != null)
			{
				string key2 = NetKey(((object)val).GetType(), entry.IsGroup);
				if (!_netIds.ContainsKey(key2))
				{
					_netIds[key2] = _netEntries.Count;
					_netEntries.Add(entry);
				}
			}
		}
	}
}
internal enum CombatSpawnBand
{
	Weak,
	Strong,
	EliteOrBoss
}
internal readonly record struct SpawnPick(SpawnEntry Entry, int Count);
internal static class SpawnRules
{
	private const float EarlyIntervalStartSec = 3.4f;

	private const float EarlyIntervalEndSec = 1.9f;

	private const int EarlyCombatRampCount = 5;

	private const float HiveIntervalSec = 1.4f;

	private const float GloryIntervalSec = 1.05f;

	private const int CrowdedAliveNormal = 12;

	private const int CrowdedAliveEliteBoss = 8;

	private const float CrowdedIntervalMul = 1.55f;

	public static float MarkIntervalSec()
	{
		string text = CurrentActName();
		float num;
		if (text == "Hive")
		{
			num = 1.4f;
		}
		else if (text == "Glory")
		{
			num = 1.05f;
		}
		else
		{
			RunManager instance = RunManager.Instance;
			RunState val = ((instance != null) ? instance.DebugOnlyGetState() : null);
			if (val == null)
			{
				num = 1.9f;
			}
			else
			{
				int num2 = CountCombatRooms(val);
				if (num2 <= 0)
				{
					num2 = Math.Max(1, val.TotalFloor);
				}
				int num3 = Math.Clamp(num2, 1, 5);
				num = ((num3 >= 5) ? 1.9f : (3.4f - (float)(num3 - 1) * 0.37500003f));
			}
		}
		return num * CrowdedIntervalMultiplier(MonsterSpawnSystem.AliveCount());
	}

	public static float CrowdedIntervalMultiplier(int aliveCount)
	{
		int num = ((CurrentBand() == CombatSpawnBand.EliteOrBoss) ? 8 : 12);
		return (aliveCount >= num) ? 1.55f : 1f;
	}

	public static string CurrentActName()
	{
		RunManager instance = RunManager.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			RunState obj2 = instance.DebugOnlyGetState();
			obj = ((obj2 != null) ? obj2.Act : null);
		}
		ActModel val = (ActModel)obj;
		if (val == null)
		{
			return "Underdocks";
		}
		string name = ((object)val).GetType().Name;
		if (1 == 0)
		{
		}
		string result;
		switch (name)
		{
		case "Underdocks":
		case "Overgrowth":
		case "Hive":
		case "Glory":
			result = name;
			break;
		default:
			result = "Underdocks";
			break;
		}
		if (1 == 0)
		{
		}
		return result;
	}

	public static bool IsAct3OrLater(string actName)
	{
		if (actName == "Hive" || actName == "Glory")
		{
			return true;
		}
		return false;
	}

	public static CombatSpawnBand CurrentBand()
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Invalid comparison between Unknown and I4
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Invalid comparison between Unknown and I4
		CombatManager instance = CombatManager.Instance;
		object obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			CombatState obj2 = instance.DebugOnlyGetState();
			obj = ((obj2 != null) ? obj2.Encounter : null);
		}
		EncounterModel val = (EncounterModel)obj;
		if (val == null)
		{
			return CombatSpawnBand.Strong;
		}
		RoomType roomType = val.RoomType;
		if ((int)roomType == 2 || (int)roomType == 3)
		{
			return CombatSpawnBand.EliteOrBoss;
		}
		if (val.IsWeak)
		{
			return CombatSpawnBand.Weak;
		}
		return CombatSpawnBand.Strong;
	}

	public static SpawnPick Pick()
	{
		if (DebugCheatSystem.TryForcedSpawn(out var pick))
		{
			return pick;
		}
		string text = CurrentActName();
		CombatSpawnBand combatSpawnBand = CurrentBand();
		if (1 == 0)
		{
		}
		bool flag = combatSpawnBand switch
		{
			CombatSpawnBand.Weak => false, 
			CombatSpawnBand.Strong => GDRand() < 0.15f, 
			_ => GDRand() < 0.3f, 
		};
		if (1 == 0)
		{
		}
		SpawnTierKind spawnTierKind = (flag ? SpawnTierKind.Strong : SpawnTierKind.Weak);
		SpawnEntry entry;
		string text2;
		if (spawnTierKind == SpawnTierKind.Strong)
		{
			IReadOnlyList<SpawnEntry> pool = SpawnCatalog.GetPool(text, SpawnTierKind.Strong);
			if (pool.Count == 0)
			{
				pool = SpawnCatalog.GetPool(text, SpawnTierKind.Weak);
			}
			if (pool.Count == 0)
			{
				pool = SpawnCatalog.GetPool("Underdocks", SpawnTierKind.Weak);
			}
			entry = pool[Random.Shared.Next(pool.Count)];
			text2 = text;
		}
		else
		{
			List<(SpawnEntry, string)> list = new List<(SpawnEntry, string)>();
			AddPool(list, text, SpawnTierKind.Weak, text);
			if (text != "Underdocks")
			{
				AddPool(list, "Underdocks", SpawnTierKind.Weak, "Underdocks");
			}
			if (text != "Overgrowth")
			{
				AddPool(list, "Overgrowth", SpawnTierKind.Weak, "Overgrowth");
			}
			if (list.Count == 0)
			{
				AddPool(list, "Underdocks", SpawnTierKind.Weak, "Underdocks");
			}
			(entry, text2) = list[Random.Shared.Next(list.Count)];
		}
		flag = ((text2 == "Underdocks" || text2 == "Overgrowth") ? true : false);
		bool flag2 = flag;
		int num = ClusterCount(entry.IsGroup);
		if (IsAct3OrLater(text) && flag2)
		{
			num++;
		}
		RunManager instance = RunManager.Instance;
		int value = CountCombatRooms((instance != null) ? instance.DebugOnlyGetState() : null);
		Log.Info($"[{"local.action_game"}] spawn pick act={text} src={text2} band={combatSpawnBand} tier={spawnTierKind} group={entry.IsGroup} count={num} interval={MarkIntervalSec():0.##}s combatIdx={value}", 2);
		return new SpawnPick(entry, num);
	}

	private static void AddPool(List<(SpawnEntry Entry, string Source)> list, string actName, SpawnTierKind tier, string sourceLabel)
	{
		foreach (SpawnEntry item in SpawnCatalog.GetPool(actName, tier))
		{
			list.Add((item, sourceLabel));
		}
	}

	public static int ClusterCount(bool isGroup)
	{
		if (!isGroup)
		{
			return 1;
		}
		return Random.Shared.Next(1, 4);
	}

	private static int CountCombatRooms(RunState? run)
	{
		if (((run != null) ? run.MapPointHistory : null) == null)
		{
			return 0;
		}
		int num = 0;
		foreach (IReadOnlyList<MapPointHistoryEntry> item in run.MapPointHistory)
		{
			if (item == null)
			{
				continue;
			}
			foreach (MapPointHistoryEntry item2 in item)
			{
				if (item2 != null && (item2.HasRoomOfType((RoomType)1) || item2.HasRoomOfType((RoomType)2) || item2.HasRoomOfType((RoomType)3)))
				{
					num++;
				}
			}
		}
		return num;
	}

	private static float GDRand()
	{
		return GD.Randf();
	}
}
[HarmonyPatch(typeof(Hook), "ModifyDamage")]
internal static class StunnedTargetDamagePatch
{
	private static void Postfix(Creature target, Creature dealer, ModifyDamageHookType modifyDamageHookType, ref decimal __result)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		if (!(__result <= 0m) && (modifyDamageHookType & 4) != 0 && target != null && target.IsMonster && target.IsStunned && dealer != null && !dealer.IsMonster)
		{
			__result = decimal.Round(__result * 1.5m, MidpointRounding.AwayFromZero);
		}
	}
}
internal static class TurnCycleBridge
{
	public static Task RunTurnStartPulse(ICombatState state)
	{
		return AfterEnemyActions(state);
	}

	public static Task RunTurnEndPulse(ICombatState state)
	{
		return BeforeEnemyActions(state);
	}

	public static async Task BeforeEnemyActions(ICombatState state)
	{
		CombatManager manager = CombatManager.Instance;
		if (manager == null || !manager.IsInProgress || manager.IsOverOrEnding)
		{
			return;
		}
		List<Creature> players = state.PlayerCreatures.Where((Creature c) => c.IsAlive).ToList();
		state.Enemies.Where((Creature c) => c.IsAlive).ToList();
		Log.Info("[local.action_game] AutoPostPlay phase before enemy act", 2);
		foreach (Player player in state.Players)
		{
			if (CombatOver(manager))
			{
				return;
			}
			if (player.PlayerCombatState != null && player.Creature != null && !player.Creature.IsDead)
			{
				await RunAutoPostPlayPhase(state, player);
				if (await CombatAccess.CheckWinAfterDeathEffects())
				{
					return;
				}
			}
		}
		Log.Info("[local.action_game] turn-end hooks (player) before enemy act", 2);
		await CombatCompat.InvokeBeforeSideTurnEnd(state, (CombatSide)1, players);
		if (CombatOver(manager))
		{
			return;
		}
		foreach (Player player2 in state.Players)
		{
			if (CombatOver(manager))
			{
				return;
			}
			if (player2.PlayerCombatState != null && player2.Creature != null && !player2.Creature.IsDead)
			{
				BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
				Log.Info("[local.action_game] DoTurnEnd (orbs/ethereal/turn-end-in-hand)", 2);
				await CombatAccess.RunDoTurnEnd(player2, (PlayerChoiceContext)(object)ctx);
				await DiscardSlyCardsFromHand(player2, (PlayerChoiceContext)(object)ctx);
				player2.PlayerCombatState.EndOfTurnCleanup();
				if (await CombatAccess.CheckWinAfterDeathEffects())
				{
					return;
				}
			}
		}
		await CombatCompat.InvokeAfterSideTurnEnd(state, (CombatSide)1, players);
		if (CombatOver(manager))
		{
			return;
		}
		List<Creature> enemies2 = state.Enemies.Where((Creature c) => c.IsAlive).ToList();
		if (enemies2.Count == 0)
		{
			await CombatAccess.CheckWinAfterDeathEffects();
			return;
		}
		Log.Info("[local.action_game] turn-start hooks (enemy) before enemy act", 2);
		foreach (Creature enemy2 in enemies2)
		{
			enemy2.BeforeTurnStart((CombatSide)2);
		}
		await Hook.BeforeSideTurnStart(state, (CombatSide)2, (IReadOnlyList<Creature>)enemies2);
		if (CombatOver(manager))
		{
			return;
		}
		foreach (Creature enemy in enemies2.Where((Creature e) => e.IsAlive).ToList())
		{
			if (CombatOver(manager))
			{
				return;
			}
			await enemy.AfterTurnStart((CombatSide)2);
		}
		enemies2 = state.Enemies.Where((Creature c) => c.IsAlive).ToList();
		if (enemies2.Count == 0)
		{
			await CombatAccess.CheckWinAfterDeathEffects();
		}
		else if (!CombatOver(manager))
		{
			await Hook.AfterSideTurnStart(state, (CombatSide)2, (IReadOnlyList<Creature>)enemies2);
		}
	}

	public static async Task AfterEnemyActions(ICombatState state)
	{
		CombatManager manager = CombatManager.Instance;
		if (manager == null || !manager.IsInProgress || manager.IsOverOrEnding)
		{
			return;
		}
		state.PlayerCreatures.Where((Creature c) => c.IsAlive).ToList();
		List<Creature> enemies = state.Enemies.Where((Creature c) => c.IsAlive).ToList();
		Log.Info("[local.action_game] turn-end hooks (enemy) after enemy act", 2);
		if (enemies.Count > 0)
		{
			await CombatCompat.InvokeBeforeSideTurnEnd(state, (CombatSide)2, enemies);
			if (CombatOver(manager))
			{
				return;
			}
			await CombatCompat.InvokeAfterSideTurnEnd(state, (CombatSide)2, enemies);
			if (CombatOver(manager))
			{
				return;
			}
		}
		List<Creature> players2 = state.PlayerCreatures.Where((Creature c) => c.IsAlive).ToList();
		if (players2.Count == 0 || CombatOver(manager))
		{
			return;
		}
		Log.Info("[local.action_game] turn-start hooks (player) after enemy act", 2);
		foreach (Creature creature in players2)
		{
			creature.BeforeTurnStart((CombatSide)1);
		}
		await Hook.BeforeSideTurnStart(state, (CombatSide)1, (IReadOnlyList<Creature>)players2);
		if (CombatOver(manager))
		{
			return;
		}
		foreach (Player player in state.Players)
		{
			if (CombatOver(manager))
			{
				return;
			}
			if (player.PlayerCombatState == null || player.Creature == null || player.Creature.IsDead)
			{
				continue;
			}
			player.PlayerCombatState.IncrementTurnNumber();
			BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
			int energyBonus = player.PlayerCombatState.MaxEnergy - player.MaxEnergy;
			if (energyBonus > 0)
			{
				Log.Info($"[{"local.action_game"}] turn-start energy bonus +{energyBonus} (max={player.PlayerCombatState.MaxEnergy} base={player.MaxEnergy})", 2);
				await PlayerCmd.GainEnergy((decimal)energyBonus, player);
				if (CombatOver(manager))
				{
					return;
				}
			}
			await Hook.AfterEnergyReset(state, player);
			if (CombatOver(manager))
			{
				return;
			}
			await Hook.AfterPlayerTurnStart(state, (PlayerChoiceContext)(object)ctx, player);
			if (CombatOver(manager))
			{
				return;
			}
			if (player.PlayerCombatState.OrbQueue != null)
			{
				Log.Info($"[{"local.action_game"}] OrbQueue.AfterTurnStart ({player.PlayerCombatState.OrbQueue.Orbs.Count} orbs)", 2);
				await player.PlayerCombatState.OrbQueue.AfterTurnStart((PlayerChoiceContext)(object)ctx);
				if (await CombatAccess.CheckWinAfterDeathEffects())
				{
					return;
				}
			}
		}
		players2 = state.PlayerCreatures.Where((Creature c) => c.IsAlive).ToList();
		if (players2.Count != 0 && !CombatOver(manager))
		{
			await Hook.AfterSideTurnStart(state, (CombatSide)1, (IReadOnlyList<Creature>)players2);
		}
	}

	private static async Task RunAutoPostPlayPhase(ICombatState state, Player player)
	{
		if (LocalContext.NetId.HasValue && player.PlayerCombatState != null)
		{
			player.PlayerCombatState.Phase = (PlayerTurnPhase)4;
			HookPlayerChoiceContext ctx = new HookPlayerChoiceContext(player, LocalContext.NetId.Value, (GameActionType)2);
			Task task = Hook.AfterAutoPostPlayPhaseEntered(ctx, state, player);
			await ctx.AssignTaskAndWaitForPauseOrCompletion(task);
			await ctx.WaitForCompletion();
			player.PlayerCombatState.Phase = (PlayerTurnPhase)5;
		}
	}

	private static async Task DiscardSlyCardsFromHand(Player player, PlayerChoiceContext choiceContext)
	{
		PlayerCombatState playerCombatState = player.PlayerCombatState;
		CardPile hand = ((playerCombatState != null) ? playerCombatState.Hand : null);
		if (hand != null)
		{
			List<CardModel> slyCards = hand.Cards.Where((CardModel c) => c.IsSlyThisTurn).ToList();
			if (slyCards.Count != 0)
			{
				Log.Info($"[{"local.action_game"}] discarding {slyCards.Count} Sly card(s) before enemy act", 2);
				await CardCmd.Discard(choiceContext, (IEnumerable<CardModel>)slyCards);
			}
		}
	}

	private static bool CombatOver(CombatManager manager)
	{
		return manager == null || !manager.IsInProgress || manager.IsOverOrEnding;
	}
}
internal static class TurnLengthHud
{
	[CompilerGenerated]
	private static class <>O
	{
		public static Action <0>__OnTabPressed;

		public static ToggledEventHandler <1>__OnAttackBoxToggled;

		public static ToggledEventHandler <2>__OnReduceVfxToggled;

		public static ToggledEventHandler <3>__OnReduceHitVfxToggled;

		public static ToggledEventHandler <4>__OnAutoPlayCardsToggled;

		public static ToggledEventHandler <5>__OnSaveEnergyToggled;

		public static ToggledEventHandler <6>__OnAutoCardSelectToggled;

		public static ToggledEventHandler <7>__OnAutoSortHandToggled;

		public static Action <8>__OnAliveFrame;
	}

	private static CanvasLayer? _layer;

	private static Button? _tab;

	private static PanelContainer? _panel;

	private static CheckButton? _autoSortHandCheck;

	private static CheckButton? _autoPlayCardsCheck;

	private static CheckButton? _saveEnergyCheck;

	private static CheckButton? _autoCardSelectCheck;

	private static CheckButton? _attackBoxCheck;

	private static CheckButton? _reduceVfxCheck;

	private static CheckButton? _reduceHitVfxCheck;

	private static Button? _diffEasyBtn;

	private static Button? _diffNormalBtn;

	private static Button? _diffHardBtn;

	private static Button? _quickAllBtn;

	private static Button? _quickBlockBtn;

	private static Button? _quickOffBtn;

	private static bool _expanded;

	private static bool _aliveHooked;

	public static void EnsureShown()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected O, but got Unknown
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Expected O, but got Unknown
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Expected O, but got Unknown
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Expected O, but got Unknown
		//IL_01b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Expected O, but got Unknown
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Expected O, but got Unknown
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0205: Expected O, but got Unknown
		//IL_0260: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Expected O, but got Unknown
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Expected O, but got Unknown
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d4: Expected O, but got Unknown
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02af: Expected O, but got Unknown
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_031b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0329: Expected O, but got Unknown
		//IL_02f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0304: Expected O, but got Unknown
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0364: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Unknown result type (might be due to invalid IL or missing references)
		//IL_037e: Expected O, but got Unknown
		//IL_034e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_0359: Expected O, but got Unknown
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d3: Expected O, but got Unknown
		//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ae: Expected O, but got Unknown
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_040e: Unknown result type (might be due to invalid IL or missing references)
		//IL_041a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0424: Expected O, but got Unknown
		//IL_0424: Unknown result type (might be due to invalid IL or missing references)
		//IL_042b: Expected O, but got Unknown
		//IL_049e: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04af: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ba: Expected O, but got Unknown
		//IL_04ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c1: Expected O, but got Unknown
		//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Expected O, but got Unknown
		EnsureAliveHook();
		if (_layer != null && GodotObject.IsInstanceValid((GodotObject)(object)_layer))
		{
			return;
		}
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		if (((val != null) ? val.Root : null) != null)
		{
			_layer = new CanvasLayer
			{
				Name = StringName.op_Implicit("RealtimeTurnLengthHud"),
				Layer = 65
			};
			Control val2 = new Control
			{
				Name = StringName.op_Implicit("TurnLengthHudRoot"),
				MouseFilter = (MouseFilterEnum)2
			};
			val2.SetAnchorsPreset((LayoutPreset)15, false);
			_tab = new Button
			{
				Text = "设置",
				ToggleMode = true,
				FocusMode = (FocusModeEnum)0
			};
			((Control)_tab).SetAnchorsPreset((LayoutPreset)6, false);
			((Control)_tab).OffsetLeft = -36f;
			((Control)_tab).OffsetRight = 0f;
			((Control)_tab).OffsetTop = -40f;
			((Control)_tab).OffsetBottom = 40f;
			((BaseButton)_tab).Pressed += OnTabPressed;
			_panel = new PanelContainer
			{
				Visible = false,
				MouseFilter = (MouseFilterEnum)0
			};
			((Control)_panel).SetAnchorsPreset((LayoutPreset)6, false);
			((Control)_panel).OffsetLeft = -280f;
			((Control)_panel).OffsetRight = -40f;
			((Control)_panel).OffsetTop = -320f;
			((Control)_panel).OffsetBottom = 320f;
			VBoxContainer val3 = new VBoxContainer();
			((Control)val3).AddThemeConstantOverride(StringName.op_Implicit("separation"), 8);
			_attackBoxCheck = new CheckButton
			{
				Text = "显示碰撞框",
				FocusMode = (FocusModeEnum)0
			};
			((BaseButton)_attackBoxCheck).SetPressedNoSignal(RealtimeCombatSettings.ShowAttackBoxes);
			CheckButton? attackBoxCheck = _attackBoxCheck;
			object obj = <>O.<1>__OnAttackBoxToggled;
			if (obj == null)
			{
				ToggledEventHandler val4 = OnAttackBoxToggled;
				<>O.<1>__OnAttackBoxToggled = val4;
				obj = (object)val4;
			}
			((BaseButton)attackBoxCheck).Toggled += (ToggledEventHandler)obj;
			_reduceVfxCheck = new CheckButton
			{
				Text = "简化原版攻击特效",
				FocusMode = (FocusModeEnum)0
			};
			((BaseButton)_reduceVfxCheck).SetPressedNoSignal(RealtimeCombatSettings.ReduceCombatVfx);
			CheckButton? reduceVfxCheck = _reduceVfxCheck;
			object obj2 = <>O.<2>__OnReduceVfxToggled;
			if (obj2 == null)
			{
				ToggledEventHandler val5 = OnReduceVfxToggled;
				<>O.<2>__OnReduceVfxToggled = val5;
				obj2 = (object)val5;
			}
			((BaseButton)reduceVfxCheck).Toggled += (ToggledEventHandler)obj2;
			_reduceHitVfxCheck = new CheckButton
			{
				Text = "简化受击特效",
				FocusMode = (FocusModeEnum)0
			};
			((BaseButton)_reduceHitVfxCheck).SetPressedNoSignal(RealtimeCombatSettings.ReduceHitVfx);
			CheckButton? reduceHitVfxCheck = _reduceHitVfxCheck;
			object obj3 = <>O.<3>__OnReduceHitVfxToggled;
			if (obj3 == null)
			{
				ToggledEventHandler val6 = OnReduceHitVfxToggled;
				<>O.<3>__OnReduceHitVfxToggled = val6;
				obj3 = (object)val6;
			}
			((BaseButton)reduceHitVfxCheck).Toggled += (ToggledEventHandler)obj3;
			_autoPlayCardsCheck = new CheckButton
			{
				Text = "自动打牌",
				FocusMode = (FocusModeEnum)0
			};
			((BaseButton)_autoPlayCardsCheck).SetPressedNoSignal(RealtimeCombatSettings.AutoPlayCards);
			CheckButton? autoPlayCardsCheck = _autoPlayCardsCheck;
			object obj4 = <>O.<4>__OnAutoPlayCardsToggled;
			if (obj4 == null)
			{
				ToggledEventHandler val7 = OnAutoPlayCardsToggled;
				<>O.<4>__OnAutoPlayCardsToggled = val7;
				obj4 = (object)val7;
			}
			((BaseButton)autoPlayCardsCheck).Toggled += (ToggledEventHandler)obj4;
			_saveEnergyCheck = new CheckButton
			{
				Text = "攒费打最左侧",
				FocusMode = (FocusModeEnum)0
			};
			((BaseButton)_saveEnergyCheck).SetPressedNoSignal(RealtimeCombatSettings.SaveEnergyForLeftmost);
			CheckButton? saveEnergyCheck = _saveEnergyCheck;
			object obj5 = <>O.<5>__OnSaveEnergyToggled;
			if (obj5 == null)
			{
				ToggledEventHandler val8 = OnSaveEnergyToggled;
				<>O.<5>__OnSaveEnergyToggled = val8;
				obj5 = (object)val8;
			}
			((BaseButton)saveEnergyCheck).Toggled += (ToggledEventHandler)obj5;
			_autoCardSelectCheck = new CheckButton
			{
				Text = "自动选牌",
				FocusMode = (FocusModeEnum)0
			};
			((BaseButton)_autoCardSelectCheck).SetPressedNoSignal(RealtimeCombatSettings.AutoCardSelect);
			CheckButton? autoCardSelectCheck = _autoCardSelectCheck;
			object obj6 = <>O.<6>__OnAutoCardSelectToggled;
			if (obj6 == null)
			{
				ToggledEventHandler val9 = OnAutoCardSelectToggled;
				<>O.<6>__OnAutoCardSelectToggled = val9;
				obj6 = (object)val9;
			}
			((BaseButton)autoCardSelectCheck).Toggled += (ToggledEventHandler)obj6;
			_autoSortHandCheck = new CheckButton
			{
				Text = "自动整理手牌",
				FocusMode = (FocusModeEnum)0
			};
			((BaseButton)_autoSortHandCheck).SetPressedNoSignal(RealtimeCombatSettings.AutoSortHand);
			CheckButton? autoSortHandCheck = _autoSortHandCheck;
			object obj7 = <>O.<7>__OnAutoSortHandToggled;
			if (obj7 == null)
			{
				ToggledEventHandler val10 = OnAutoSortHandToggled;
				<>O.<7>__OnAutoSortHandToggled = val10;
				obj7 = (object)val10;
			}
			((BaseButton)autoSortHandCheck).Toggled += (ToggledEventHandler)obj7;
			Label val11 = new Label
			{
				Text = "难度",
				HorizontalAlignment = (HorizontalAlignment)1
			};
			HBoxContainer val12 = new HBoxContainer();
			((Control)val12).AddThemeConstantOverride(StringName.op_Implicit("separation"), 6);
			_diffEasyBtn = MakeDifficultyButton("简单", GameDifficulty.Easy);
			_diffNormalBtn = MakeDifficultyButton("普通", GameDifficulty.Normal);
			_diffHardBtn = MakeDifficultyButton("困难", GameDifficulty.Hard);
			((Node)val12).AddChild((Node)(object)_diffEasyBtn, false, (InternalMode)0);
			((Node)val12).AddChild((Node)(object)_diffNormalBtn, false, (InternalMode)0);
			((Node)val12).AddChild((Node)(object)_diffHardBtn, false, (InternalMode)0);
			Label val13 = new Label
			{
				Text = "迅捷出牌",
				HorizontalAlignment = (HorizontalAlignment)1
			};
			HBoxContainer val14 = new HBoxContainer();
			((Control)val14).AddThemeConstantOverride(StringName.op_Implicit("separation"), 6);
			_quickAllBtn = MakeQuickPlayButton("全开启", QuickPlayMode.All);
			_quickBlockBtn = MakeQuickPlayButton("仅防牌", QuickPlayMode.BlockOnly);
			_quickOffBtn = MakeQuickPlayButton("关闭", QuickPlayMode.Off);
			((Node)val14).AddChild((Node)(object)_quickAllBtn, false, (InternalMode)0);
			((Node)val14).AddChild((Node)(object)_quickBlockBtn, false, (InternalMode)0);
			((Node)val14).AddChild((Node)(object)_quickOffBtn, false, (InternalMode)0);
			((Node)val3).AddChild((Node)(object)_attackBoxCheck, false, (InternalMode)0);
			((Node)val3).AddChild((Node)(object)_reduceVfxCheck, false, (InternalMode)0);
			((Node)val3).AddChild((Node)(object)_reduceHitVfxCheck, false, (InternalMode)0);
			((Node)val3).AddChild((Node)(object)_autoPlayCardsCheck, false, (InternalMode)0);
			((Node)val3).AddChild((Node)(object)_saveEnergyCheck, false, (InternalMode)0);
			((Node)val3).AddChild((Node)(object)_autoCardSelectCheck, false, (InternalMode)0);
			((Node)val3).AddChild((Node)(object)_autoSortHandCheck, false, (InternalMode)0);
			((Node)val3).AddChild((Node)(object)val11, false, (InternalMode)0);
			((Node)val3).AddChild((Node)(object)val12, false, (InternalMode)0);
			((Node)val3).AddChild((Node)(object)val13, false, (InternalMode)0);
			((Node)val3).AddChild((Node)(object)val14, false, (InternalMode)0);
			((Node)_panel).AddChild((Node)(object)val3, false, (InternalMode)0);
			((Node)val2).AddChild((Node)(object)_panel, false, (InternalMode)0);
			((Node)val2).AddChild((Node)(object)_tab, false, (InternalMode)0);
			((Node)_layer).AddChild((Node)(object)val2, false, (InternalMode)0);
			((Node)val.Root).AddChild((Node)(object)_layer, false, (InternalMode)0);
			RefreshAutoSortHandCheck();
			RefreshAutoPlayCardsCheck();
			RefreshSaveEnergyCheck();
			RefreshAutoCardSelectCheck();
			RefreshAttackBoxCheck();
			RefreshReduceVfxCheck();
			RefreshReduceHitVfxCheck();
			RefreshDifficultyButtons();
			RefreshQuickPlayButtons();
			_expanded = false;
			Log.Info("[local.action_game] TurnLengthHud shown (global)", 2);
		}
	}

	private static void EnsureAliveHook()
	{
		if (!_aliveHooked)
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val != null)
			{
				val.ProcessFrame += OnAliveFrame;
				_aliveHooked = true;
			}
		}
	}

	private static void OnAliveFrame()
	{
		if (_layer == null || !GodotObject.IsInstanceValid((GodotObject)(object)_layer))
		{
			EnsureShown();
		}
	}

	public static void Hide()
	{
		if (_layer != null && GodotObject.IsInstanceValid((GodotObject)(object)_layer))
		{
			((Node)_layer).QueueFree();
		}
		_layer = null;
		_tab = null;
		_panel = null;
		_autoSortHandCheck = null;
		_autoPlayCardsCheck = null;
		_saveEnergyCheck = null;
		_autoCardSelectCheck = null;
		_attackBoxCheck = null;
		_reduceVfxCheck = null;
		_reduceHitVfxCheck = null;
		_diffEasyBtn = null;
		_diffNormalBtn = null;
		_diffHardBtn = null;
		_quickAllBtn = null;
		_quickBlockBtn = null;
		_quickOffBtn = null;
		_expanded = false;
	}

	private static Button MakeDifficultyButton(string text, GameDifficulty difficulty)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		Button val = new Button
		{
			Text = text,
			ToggleMode = true,
			FocusMode = (FocusModeEnum)0,
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((BaseButton)val).Pressed += delegate
		{
			RealtimeCombatSettings.Difficulty = difficulty;
			RefreshDifficultyButtons();
		};
		return val;
	}

	private static Button MakeQuickPlayButton(string text, QuickPlayMode mode)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		Button val = new Button
		{
			Text = text,
			ToggleMode = true,
			FocusMode = (FocusModeEnum)0,
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((BaseButton)val).Pressed += delegate
		{
			RealtimeCombatSettings.QuickPlayMode = mode;
			RefreshQuickPlayButtons();
		};
		return val;
	}

	private static void OnTabPressed()
	{
		_expanded = !_expanded;
		if (_panel != null && GodotObject.IsInstanceValid((GodotObject)(object)_panel))
		{
			((CanvasItem)_panel).Visible = _expanded;
		}
		if (_tab != null && GodotObject.IsInstanceValid((GodotObject)(object)_tab))
		{
			((BaseButton)_tab).ButtonPressed = _expanded;
		}
	}

	private static void RefreshAutoSortHandCheck()
	{
		if (_autoSortHandCheck != null && GodotObject.IsInstanceValid((GodotObject)(object)_autoSortHandCheck))
		{
			((BaseButton)_autoSortHandCheck).SetPressedNoSignal(RealtimeCombatSettings.AutoSortHand);
		}
	}

	private static void OnAutoSortHandToggled(bool on)
	{
		RealtimeCombatSettings.AutoSortHand = on;
	}

	private static void RefreshAutoPlayCardsCheck()
	{
		if (_autoPlayCardsCheck != null && GodotObject.IsInstanceValid((GodotObject)(object)_autoPlayCardsCheck))
		{
			((BaseButton)_autoPlayCardsCheck).SetPressedNoSignal(RealtimeCombatSettings.AutoPlayCards);
		}
	}

	private static void OnAutoPlayCardsToggled(bool on)
	{
		RealtimeCombatSettings.AutoPlayCards = on;
	}

	private static void RefreshSaveEnergyCheck()
	{
		if (_saveEnergyCheck != null && GodotObject.IsInstanceValid((GodotObject)(object)_saveEnergyCheck))
		{
			((BaseButton)_saveEnergyCheck).SetPressedNoSignal(RealtimeCombatSettings.SaveEnergyForLeftmost);
		}
	}

	private static void OnSaveEnergyToggled(bool on)
	{
		RealtimeCombatSettings.SaveEnergyForLeftmost = on;
	}

	private static void RefreshAutoCardSelectCheck()
	{
		if (_autoCardSelectCheck != null && GodotObject.IsInstanceValid((GodotObject)(object)_autoCardSelectCheck))
		{
			((BaseButton)_autoCardSelectCheck).SetPressedNoSignal(RealtimeCombatSettings.AutoCardSelect);
		}
	}

	private static void OnAutoCardSelectToggled(bool on)
	{
		RealtimeCombatSettings.AutoCardSelect = on;
	}

	private static void RefreshAttackBoxCheck()
	{
		if (_attackBoxCheck != null && GodotObject.IsInstanceValid((GodotObject)(object)_attackBoxCheck))
		{
			((BaseButton)_attackBoxCheck).SetPressedNoSignal(RealtimeCombatSettings.ShowAttackBoxes);
		}
	}

	private static void OnAttackBoxToggled(bool on)
	{
		RealtimeCombatSettings.ShowAttackBoxes = on;
	}

	private static void RefreshReduceVfxCheck()
	{
		if (_reduceVfxCheck != null && GodotObject.IsInstanceValid((GodotObject)(object)_reduceVfxCheck))
		{
			((BaseButton)_reduceVfxCheck).SetPressedNoSignal(RealtimeCombatSettings.ReduceCombatVfx);
		}
	}

	private static void OnReduceVfxToggled(bool on)
	{
		RealtimeCombatSettings.ReduceCombatVfx = on;
	}

	private static void RefreshReduceHitVfxCheck()
	{
		if (_reduceHitVfxCheck != null && GodotObject.IsInstanceValid((GodotObject)(object)_reduceHitVfxCheck))
		{
			((BaseButton)_reduceHitVfxCheck).SetPressedNoSignal(RealtimeCombatSettings.ReduceHitVfx);
		}
	}

	private static void OnReduceHitVfxToggled(bool on)
	{
		RealtimeCombatSettings.ReduceHitVfx = on;
	}

	private static void RefreshDifficultyButtons()
	{
		GameDifficulty difficulty = RealtimeCombatSettings.Difficulty;
		if (_diffEasyBtn != null && GodotObject.IsInstanceValid((GodotObject)(object)_diffEasyBtn))
		{
			((BaseButton)_diffEasyBtn).ButtonPressed = difficulty == GameDifficulty.Easy;
		}
		if (_diffNormalBtn != null && GodotObject.IsInstanceValid((GodotObject)(object)_diffNormalBtn))
		{
			((BaseButton)_diffNormalBtn).ButtonPressed = difficulty == GameDifficulty.Normal;
		}
		if (_diffHardBtn != null && GodotObject.IsInstanceValid((GodotObject)(object)_diffHardBtn))
		{
			((BaseButton)_diffHardBtn).ButtonPressed = difficulty == GameDifficulty.Hard;
		}
	}

	private static void RefreshQuickPlayButtons()
	{
		QuickPlayMode quickPlayMode = RealtimeCombatSettings.QuickPlayMode;
		if (_quickAllBtn != null && GodotObject.IsInstanceValid((GodotObject)(object)_quickAllBtn))
		{
			((BaseButton)_quickAllBtn).ButtonPressed = quickPlayMode == QuickPlayMode.All;
		}
		if (_quickBlockBtn != null && GodotObject.IsInstanceValid((GodotObject)(object)_quickBlockBtn))
		{
			((BaseButton)_quickBlockBtn).ButtonPressed = quickPlayMode == QuickPlayMode.BlockOnly;
		}
		if (_quickOffBtn != null && GodotObject.IsInstanceValid((GodotObject)(object)_quickOffBtn))
		{
			((BaseButton)_quickOffBtn).ButtonPressed = quickPlayMode == QuickPlayMode.Off;
		}
	}

	public static void RefreshQuickPlayFromHotkey()
	{
		RefreshQuickPlayButtons();
	}

	public static void RefreshFromHostSync()
	{
		RefreshAutoSortHandCheck();
		RefreshAutoPlayCardsCheck();
		RefreshSaveEnergyCheck();
		RefreshAutoCardSelectCheck();
		RefreshAttackBoxCheck();
		RefreshReduceVfxCheck();
		RefreshReduceHitVfxCheck();
		RefreshDifficultyButtons();
		RefreshQuickPlayButtons();
		ApplyClientLockUi();
	}

	private static void ApplyClientLockUi()
	{
		bool clientLocked = HostSettingsNetSync.ClientLocked;
		if (_autoSortHandCheck != null && GodotObject.IsInstanceValid((GodotObject)(object)_autoSortHandCheck))
		{
			((BaseButton)_autoSortHandCheck).Disabled = clientLocked;
		}
		if (_autoPlayCardsCheck != null && GodotObject.IsInstanceValid((GodotObject)(object)_autoPlayCardsCheck))
		{
			((BaseButton)_autoPlayCardsCheck).Disabled = clientLocked;
		}
		if (_saveEnergyCheck != null && GodotObject.IsInstanceValid((GodotObject)(object)_saveEnergyCheck))
		{
			((BaseButton)_saveEnergyCheck).Disabled = clientLocked;
		}
		if (_diffEasyBtn != null && GodotObject.IsInstanceValid((GodotObject)(object)_diffEasyBtn))
		{
			((BaseButton)_diffEasyBtn).Disabled = clientLocked;
		}
		if (_diffNormalBtn != null && GodotObject.IsInstanceValid((GodotObject)(object)_diffNormalBtn))
		{
			((BaseButton)_diffNormalBtn).Disabled = clientLocked;
		}
		if (_diffHardBtn != null && GodotObject.IsInstanceValid((GodotObject)(object)_diffHardBtn))
		{
			((BaseButton)_diffHardBtn).Disabled = clientLocked;
		}
		if (_quickAllBtn != null && GodotObject.IsInstanceValid((GodotObject)(object)_quickAllBtn))
		{
			((BaseButton)_quickAllBtn).Disabled = clientLocked;
		}
		if (_quickBlockBtn != null && GodotObject.IsInstanceValid((GodotObject)(object)_quickBlockBtn))
		{
			((BaseButton)_quickBlockBtn).Disabled = clientLocked;
		}
		if (_quickOffBtn != null && GodotObject.IsInstanceValid((GodotObject)(object)_quickOffBtn))
		{
			((BaseButton)_quickOffBtn).Disabled = clientLocked;
		}
	}
}
internal static class VanillaHotkeyDisable
{
	[CompilerGenerated]
	private static class <>O
	{
		public static InputReboundEventHandler <0>__StripKeyboardBindings;
	}

	private static bool _reboundHooked;

	public static void EnsureHooks()
	{
		StripKeyboardBindings();
		HookRebound();
	}

	private static void HookRebound()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		if (_reboundHooked)
		{
			return;
		}
		NInputManager instance = NInputManager.Instance;
		if (instance != null)
		{
			object obj = <>O.<0>__StripKeyboardBindings;
			if (obj == null)
			{
				InputReboundEventHandler val = StripKeyboardBindings;
				<>O.<0>__StripKeyboardBindings = val;
				obj = (object)val;
			}
			instance.InputRebound += (InputReboundEventHandler)obj;
			_reboundHooked = true;
		}
	}

	public static void StripKeyboardBindings()
	{
		string[] allInputs = MegaInput.AllInputs;
		foreach (string text in allInputs)
		{
			if (text == null || !InputMap.HasAction(StringName.op_Implicit(text)))
			{
				continue;
			}
			Array<InputEvent> val = InputMap.ActionGetEvents(StringName.op_Implicit(text));
			foreach (InputEvent item in val)
			{
				if (item is InputEventKey)
				{
					InputMap.ActionEraseEvent(StringName.op_Implicit(text), item);
				}
			}
		}
	}
}
[HarmonyPatch(typeof(NHotkeyManager), "_UnhandledInput")]
internal static class HotkeyManagerUnhandledInputPatch
{
	private static bool Prefix(InputEvent @event)
	{
		if (@event is InputEventKey)
		{
			return false;
		}
		if (PlayerWasdMoveSystem.IsLiveCombatPlay() && (@event.IsAction(MegaInput.confirm, false) || @event.IsAction(Controller.faceButtonSouth, false)))
		{
			return false;
		}
		return true;
	}
}
[HarmonyPatch(typeof(NInputManager), "ProcessHotkeyInput")]
internal static class ProcessHotkeyInputPatch
{
	private static bool Prefix()
	{
		return false;
	}
}
[HarmonyPatch(typeof(NInputManager), "ProcessFkbInput")]
internal static class ProcessFkbInputPatch
{
	private static bool Prefix()
	{
		return false;
	}
}
[HarmonyPatch(typeof(NInputManager), "Init")]
internal static class InputManagerInitPatch
{
	private static void Postfix()
	{
		VanillaHotkeyDisable.EnsureHooks();
		Log.Info("[local.action_game] vanilla keyboard hotkeys disabled; controller hotkeys kept", 2);
	}
}
internal static class WellLaidPlansRewrite
{
	public const string Description = "回合结束时，随机一张手牌获得保留。";

	public static async Task ApplyRandomRetain(WellLaidPlansPower power)
	{
		Creature owner = ((PowerModel)power).Owner;
		Player player = ((owner != null) ? owner.Player : null);
		object obj;
		if (player == null)
		{
			obj = null;
		}
		else
		{
			PlayerCombatState playerCombatState = player.PlayerCombatState;
			obj = ((playerCombatState != null) ? playerCombatState.Hand : null);
		}
		CardPile hand = (CardPile)obj;
		if (player == null || hand == null || ((PowerModel)power).Amount <= 0)
		{
			return;
		}
		List<CardModel> candidates = hand.Cards.Where((CardModel c) => !c.Keywords.Contains((CardKeyword)5)).ToList();
		if (candidates.Count == 0)
		{
			return;
		}
		Rng rng = new Rng(player, ((AbstractModel)power).Id, 0uL);
		int j = Math.Min(((PowerModel)power).Amount, candidates.Count);
		for (int i = 0; i < j; i++)
		{
			CardModel pick = rng.NextItem<CardModel>((IEnumerable<CardModel>)candidates);
			if (pick == null)
			{
				break;
			}
			CardCmd.ApplyKeyword(pick, (CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)5 });
			candidates.Remove(pick);
		}
		Traverse.Create((object)power).Method("Flash", Array.Empty<object>()).GetValue();
		await Task.CompletedTask;
	}
}
[HarmonyPatch(typeof(AbstractModel), "BeforeSideTurnEnd")]
internal static class WellLaidPlansBeforeTurnEndPatch
{
	private static void Postfix(AbstractModel __instance, CombatSide side, ref Task __result)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)side == 1)
		{
			WellLaidPlansPower val = (WellLaidPlansPower)(object)((__instance is WellLaidPlansPower) ? __instance : null);
			if (val != null)
			{
				__result = Continue(__result, val);
			}
		}
	}

	private static async Task Continue(Task original, WellLaidPlansPower power)
	{
		if (original != null)
		{
			await original;
		}
		await WellLaidPlansRewrite.ApplyRandomRetain(power);
	}
}
[HarmonyPatch(typeof(WellLaidPlansPower), "BeforeFlushLate")]
internal static class WellLaidPlansDisableFlushPatch
{
	private static bool Prefix(ref Task __result)
	{
		__result = Task.CompletedTask;
		return false;
	}
}
[HarmonyPatch(typeof(LocString), "GetFormattedText")]
internal static class WellLaidPlansDescriptionPatch
{
	private static void Postfix(LocString __instance, ref string __result)
	{
		bool flag = __instance.LocTable == "cards";
		bool flag2 = flag;
		if (flag2)
		{
			string locEntryKey = __instance.LocEntryKey;
			bool flag3 = ((locEntryKey == "WELL_LAID_PLANS.description" || locEntryKey == "WELL_LAID_PLANS.eventDescription") ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			__result = "回合结束时，随机一张手牌获得保留。";
			return;
		}
		bool flag4 = __instance.LocTable == "powers";
		bool flag5 = flag4;
		if (flag5)
		{
			bool flag3;
			switch (__instance.LocEntryKey)
			{
			case "WELL_LAID_PLANS_POWER.description":
			case "WELL_LAID_PLANS_POWER.smartDescription":
			case "WELL_LAID_PLANS_POWER.eventDescription":
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag5 = flag3;
		}
		if (flag5)
		{
			__result = "回合结束时，随机一张手牌获得保留。";
		}
	}
}
