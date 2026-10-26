using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace CombatFlight;

internal static class FlightController
{
	private const ulong DoubleTapWindowMsec = 350uL;

	private const float MovementSpeed = 520f;

	private const float RotationSpeedRadians = 2.2f;

	private const double MovingSendIntervalSeconds = 0.05;

	private const double IdleSendIntervalSeconds = 0.75;

	private const float MaxNetworkOffset = 10000f;

	private const string SongResourceName = "CombatFlight.CombatFlightSong.mp3";

	private static readonly MessageHandlerDelegate<Sts2CombatFlightPositionMessage> MessageHandler = HandlePositionMessage;

	private static readonly Dictionary<ulong, Vector2> Origins = new Dictionary<ulong, Vector2>();

	private static readonly Dictionary<ulong, float> OriginRotations = new Dictionary<ulong, float>();

	private static readonly Dictionary<ulong, uint> LastRemoteSequences = new Dictionary<ulong, uint>();

	private static readonly HashSet<ulong> FlyingPlayers = new HashSet<ulong>();

	private static INetGameService? _netService;

	private static AudioStreamPlayer? _songPlayer;

	private static CanvasLayer? _notificationLayer;

	private static Label? _notificationLabel;

	private static Tween? _notificationTween;

	private static ulong? _roomInstanceId;

	private static RunLocation? _runLocation;

	private static ulong _lastStartPressMsec;

	private static ulong _lastStopPressMsec;

	private static uint _localSequence;

	private static double _sendAccumulator;

	private static bool _isFlying;

	private static bool _moveUpPressed;

	private static bool _moveDownPressed;

	private static bool _moveLeftPressed;

	private static bool _moveRightPressed;

	private static bool _rotateLeftPressed;

	private static bool _rotateRightPressed;

	private static bool _positionDirty;

	private static bool _combatInactiveHandled;

	private static bool _songLoadAttempted;

	private static bool _flightSongActive;

	public static void Bind(INetGameService netService)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		Unbind();
		_netService = netService;
		_netService.RegisterMessageHandler<Sts2CombatFlightPositionMessage>(MessageHandler);
		ResetRoomState();
		Log.Info($"CombatFlight bound to {netService.Type} network service.", 2);
	}

	public static void Unbind()
	{
		if (_netService != null)
		{
			try
			{
				_netService.UnregisterMessageHandler<Sts2CombatFlightPositionMessage>(MessageHandler);
			}
			catch (Exception ex)
			{
				Log.Warn("CombatFlight could not unregister its network handler: " + ex.Message, 2);
			}
		}
		_netService = null;
		ResetRoomState();
		DisposeSongPlayer();
		DisposeNotification();
	}

	public static bool HandleInput(InputEvent inputEvent)
	{
		InputEventKey val = (InputEventKey)(object)((inputEvent is InputEventKey) ? inputEvent : null);
		if (val == null || val.Echo)
		{
			return false;
		}
		UpdateHeldControlState(val);
		EnsureRoomState();
		if (MatchesBinding(val, FlightSettings.ResetFlightKey) && val.Pressed && CanResetLocalPlayer())
		{
			ResetLocalPlayer(showNotification: true);
			return true;
		}
		bool flag = MatchesBinding(val, FlightSettings.StartFlightKey);
		bool flag2 = MatchesBinding(val, FlightSettings.StopFlightKey);
		if ((flag || flag2) && CanControlLocalPlayer())
		{
			if (val.Pressed)
			{
				if (!_isFlying && flag && CompleteGesture(ref _lastStartPressMsec, FlightSettings.DoubleTapStart))
				{
					SetLocalFlightState(isFlying: true);
				}
				else if (_isFlying && flag2 && CompleteGesture(ref _lastStopPressMsec, FlightSettings.DoubleTapStop))
				{
					SetLocalFlightState(isFlying: false);
				}
			}
			return true;
		}
		if (_isFlying)
		{
			return IsConfiguredFlightControlKey(val);
		}
		return false;
	}

	public static void Update(double delta)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		EnsureRoomState();
		ulong? roomInstanceId = _roomInstanceId;
		if (!roomInstanceId.HasValue)
		{
			return;
		}
		PruneDisconnectedFlyingPlayers();
		if (!CombatManager.Instance.IsInProgress)
		{
			if (!_combatInactiveHandled)
			{
				_combatInactiveHandled = true;
				if (CanResetLocalPlayer())
				{
					ResetLocalPlayer(showNotification: false);
				}
				FlyingPlayers.Clear();
				UpdateFlightSongState();
			}
			return;
		}
		_combatInactiveHandled = false;
		INetGameService? netService = _netService;
		NCreature creatureNode = GetCreatureNode((netService != null) ? new ulong?(netService.NetId) : LocalContext.NetId);
		if (creatureNode == null)
		{
			return;
		}
		bool flag = false;
		if (_isFlying)
		{
			Vector2 movementDirection = GetMovementDirection();
			if (!((Vector2)(ref movementDirection)).IsZeroApprox())
			{
				((Control)creatureNode).Position = ((Control)creatureNode).Position + ((Vector2)(ref movementDirection)).Normalized() * 520f * (float)delta;
				flag = true;
			}
			float rotationDirection = GetRotationDirection();
			if (!Mathf.IsZeroApprox(rotationDirection))
			{
				((Node2D)creatureNode.Visuals).Rotation = Mathf.Wrap(((Node2D)creatureNode.Visuals).Rotation + rotationDirection * 2.2f * (float)delta, -(float)Math.PI, (float)Math.PI);
				flag = true;
			}
			if (flag)
			{
				_positionDirty = true;
			}
		}
		_sendAccumulator += delta;
		double num = ((_isFlying || flag || _positionDirty) ? 0.05 : 0.75);
		if (_sendAccumulator >= num)
		{
			SendLocalPosition();
		}
	}

	public static void SuppressBgmWhileFlightSongIsPlaying(ref float volume)
	{
		if (_flightSongActive)
		{
			volume = 0f;
		}
	}

	public static void OnAudioSettingsChanged()
	{
		SetSongVolume(FlightSettings.FlightBgmVolume);
		UpdateFlightSongState();
	}

	public static void OnControlSettingsChanged()
	{
		ClearGestureState();
		ClearHeldControlState();
	}

	private static void EnsureRoomState()
	{
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance == null || !GodotObject.IsInstanceValid((GodotObject)(object)instance))
		{
			ulong? roomInstanceId = _roomInstanceId;
			if (roomInstanceId.HasValue)
			{
				ResetRoomState();
			}
			return;
		}
		ulong instanceId = ((GodotObject)instance).GetInstanceId();
		if (_roomInstanceId == instanceId)
		{
			RunLocation? runLocation = _runLocation;
			if (!runLocation.HasValue)
			{
				_runLocation = TryGetCurrentRunLocation();
			}
			CaptureMissingOrigins(instance);
		}
		else
		{
			ResetRoomState();
			_roomInstanceId = instanceId;
			_runLocation = TryGetCurrentRunLocation();
			CaptureMissingOrigins(instance);
		}
	}

	private static void CaptureMissingOrigins(NCombatRoom room)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		foreach (NCreature creatureNode in room.CreatureNodes)
		{
			Player player = creatureNode.Entity.Player;
			ulong? num = ((player != null) ? new ulong?(player.NetId) : null);
			if (num.HasValue && !Origins.ContainsKey(num.Value))
			{
				Origins[num.Value] = ((Control)creatureNode).Position;
				OriginRotations[num.Value] = ((Node2D)creatureNode.Visuals).Rotation;
			}
		}
	}

	private static void ResetRoomState()
	{
		FlyingPlayers.Clear();
		UpdateFlightSongState();
		Origins.Clear();
		OriginRotations.Clear();
		LastRemoteSequences.Clear();
		_roomInstanceId = null;
		_runLocation = null;
		ClearGestureState();
		ClearHeldControlState();
		_localSequence = 0u;
		_sendAccumulator = 0.0;
		_isFlying = false;
		_positionDirty = false;
		_combatInactiveHandled = false;
	}

	private static void SetLocalFlightState(bool isFlying)
	{
		_isFlying = isFlying;
		ClearGestureState();
		_positionDirty = true;
		INetGameService? netService = _netService;
		ulong? num = ((netService != null) ? new ulong?(netService.NetId) : LocalContext.NetId);
		if (num.HasValue)
		{
			SetPlayerFlyingState(num.Value, _isFlying);
		}
		SendLocalPosition();
		ShowNotification(_isFlying ? FlightText.Choose("飞行模式：开启", "Flight mode: ON") : FlightText.Choose("飞行模式：关闭", "Flight mode: OFF"));
	}

	private static void ResetLocalPlayer(bool showNotification)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		INetGameService? netService = _netService;
		ulong? playerId = ((netService != null) ? new ulong?(netService.NetId) : LocalContext.NetId);
		NCreature creatureNode = GetCreatureNode(playerId);
		if (playerId.HasValue && creatureNode != null)
		{
			if (Origins.TryGetValue(playerId.Value, out var value))
			{
				((Control)creatureNode).Position = value;
			}
			if (OriginRotations.TryGetValue(playerId.Value, out var value2))
			{
				((Node2D)creatureNode.Visuals).Rotation = value2;
			}
			SetPlayerFlyingState(playerId.Value, isFlying: false);
		}
		_isFlying = false;
		ClearGestureState();
		ClearHeldControlState();
		_positionDirty = true;
		SendLocalPosition();
		if (showNotification)
		{
			ShowNotification(FlightText.Choose("位置和朝向已复位", "Position and facing reset"));
		}
	}

	private static bool CanControlLocalPlayer()
	{
		if (CombatManager.Instance.IsInProgress)
		{
			INetGameService? netService = _netService;
			return GetCreatureNode((netService != null) ? new ulong?(netService.NetId) : LocalContext.NetId) != null;
		}
		return false;
	}

	private static bool CanResetLocalPlayer()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		INetGameService? netService = _netService;
		ulong? playerId = ((netService != null) ? new ulong?(netService.NetId) : LocalContext.NetId);
		if (!playerId.HasValue || !Origins.TryGetValue(playerId.Value, out var value) || !OriginRotations.TryGetValue(playerId.Value, out var value2))
		{
			return false;
		}
		NCreature creatureNode = GetCreatureNode(playerId);
		if (creatureNode != null)
		{
			if (!_isFlying)
			{
				Vector2 position = ((Control)creatureNode).Position;
				if (((Vector2)(ref position)).IsEqualApprox(value))
				{
					return !Mathf.IsEqualApprox(((Node2D)creatureNode.Visuals).Rotation, value2);
				}
			}
			return true;
		}
		return false;
	}

	private static NCreature? GetCreatureNode(ulong? playerId)
	{
		if (playerId.HasValue)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				return instance.CreatureNodes.FirstOrDefault((Func<NCreature, bool>)delegate(NCreature creature)
				{
					Player player = creature.Entity.Player;
					return ((player != null) ? new ulong?(player.NetId) : null) == playerId.Value;
				});
			}
		}
		return null;
	}

	private static Vector2 GetMovementDirection()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		Vector2 zero = Vector2.Zero;
		if (_moveUpPressed)
		{
			zero.Y -= 1f;
		}
		if (_moveDownPressed)
		{
			zero.Y += 1f;
		}
		if (_moveLeftPressed)
		{
			zero.X -= 1f;
		}
		if (_moveRightPressed)
		{
			zero.X += 1f;
		}
		return zero;
	}

	private static float GetRotationDirection()
	{
		float num = 0f;
		if (_rotateLeftPressed)
		{
			num -= 1f;
		}
		if (_rotateRightPressed)
		{
			num += 1f;
		}
		return num;
	}

	private static void SendLocalPosition()
	{
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		_sendAccumulator = 0.0;
		if (_netService != null && _netService.IsConnected)
		{
			RunLocation? runLocation = _runLocation;
			if (runLocation.HasValue)
			{
				ulong netId = _netService.NetId;
				NCreature creatureNode = GetCreatureNode(netId);
				if (creatureNode != null && Origins.TryGetValue(netId, out var value) && OriginRotations.TryGetValue(netId, out var value2))
				{
					Vector2 offset = ClampNetworkOffset(((Control)creatureNode).Position - value);
					float rotationOffset = NormalizeRotation(((Node2D)creatureNode.Visuals).Rotation - value2);
					_netService.SendMessage<Sts2CombatFlightPositionMessage>(new Sts2CombatFlightPositionMessage
					{
						Location = _runLocation.Value,
						Offset = offset,
						RotationOffset = rotationOffset,
						Sequence = ++_localSequence,
						IsFlying = _isFlying
					});
					_positionDirty = false;
				}
				return;
			}
		}
		_positionDirty = false;
	}

	private static void HandlePositionMessage(Sts2CombatFlightPositionMessage message, ulong senderId)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		if (_netService == null || senderId == _netService.NetId || !CombatManager.Instance.IsInProgress)
		{
			return;
		}
		RunLocation? runLocation = _runLocation;
		if (runLocation.HasValue && !(message.Location != _runLocation.Value) && IsFinite(message.Offset) && float.IsFinite(message.RotationOffset) && (!LastRemoteSequences.TryGetValue(senderId, out var value) || IsNewerSequence(message.Sequence, value)))
		{
			LastRemoteSequences[senderId] = message.Sequence;
			SetPlayerFlyingState(senderId, message.IsFlying);
			NCreature creatureNode = GetCreatureNode(senderId);
			if (creatureNode != null && Origins.TryGetValue(senderId, out var value2) && OriginRotations.TryGetValue(senderId, out var value3))
			{
				((Control)creatureNode).Position = value2 + ClampNetworkOffset(message.Offset);
				((Node2D)creatureNode.Visuals).Rotation = NormalizeRotation(value3 + message.RotationOffset);
			}
		}
	}

	private static void SetPlayerFlyingState(ulong playerId, bool isFlying)
	{
		if (isFlying ? FlyingPlayers.Add(playerId) : FlyingPlayers.Remove(playerId))
		{
			UpdateFlightSongState();
		}
	}

	private static void PruneDisconnectedFlyingPlayers()
	{
		if (FlyingPlayers.RemoveWhere((ulong playerId) => GetCreatureNode(playerId) == null) > 0)
		{
			UpdateFlightSongState();
		}
	}

	private static void UpdateFlightSongState()
	{
		if (FlyingPlayers.Count > 0 && FlightSettings.PlayFlightBgm)
		{
			StartFlightSong();
		}
		else
		{
			StopFlightSong();
		}
	}

	private static void StartFlightSong()
	{
		if (_flightSongActive)
		{
			return;
		}
		AudioStreamPlayer val = EnsureSongPlayer();
		if (val != null)
		{
			SetSongVolume(FlightSettings.FlightBgmVolume);
			val.Autoplay = true;
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.SetBgmVol(0f);
			}
			_flightSongActive = true;
			if (((Node)val).IsInsideTree())
			{
				val.Play(0f);
			}
		}
	}

	private static void StopFlightSong()
	{
		if (!_flightSongActive)
		{
			return;
		}
		_flightSongActive = false;
		if (_songPlayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_songPlayer))
		{
			_songPlayer.Autoplay = false;
			_songPlayer.Stop();
		}
		try
		{
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.SetBgmVol(SaveManager.Instance.SettingsSave.VolumeBgm);
			}
		}
		catch (Exception ex)
		{
			Log.Warn("CombatFlight could not restore background music volume: " + ex.Message, 2);
		}
	}

	private static AudioStreamPlayer? EnsureSongPlayer()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Expected O, but got Unknown
		if (_songPlayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_songPlayer))
		{
			return _songPlayer;
		}
		if (!_songLoadAttempted)
		{
			NGame instance = NGame.Instance;
			if (instance != null)
			{
				_songLoadAttempted = true;
				try
				{
					using Stream stream = typeof(FlightController).Assembly.GetManifestResourceStream("CombatFlight.CombatFlightSong.mp3");
					if (stream == null)
					{
						throw new InvalidOperationException("embedded resource CombatFlight.CombatFlightSong.mp3 was not found");
					}
					using MemoryStream memoryStream = new MemoryStream();
					stream.CopyTo(memoryStream);
					AudioStreamMP3 stream2 = new AudioStreamMP3
					{
						Data = memoryStream.ToArray(),
						Loop = true
					};
					_songPlayer = new AudioStreamPlayer
					{
						Name = StringName.op_Implicit("CombatFlightSong"),
						Stream = (AudioStream)(object)stream2,
						ProcessMode = (ProcessModeEnum)3
					};
					GodotTreeExtensions.AddChildSafely((Node)(object)instance, (Node)(object)_songPlayer);
					return _songPlayer;
				}
				catch (Exception value)
				{
					Log.Error($"CombatFlight could not load its embedded flight song: {value}", 2);
					_songPlayer = null;
					return null;
				}
			}
		}
		return null;
	}

	private static void DisposeSongPlayer()
	{
		StopFlightSong();
		if (_songPlayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_songPlayer))
		{
			GodotTreeExtensions.QueueFreeSafely((Node)(object)_songPlayer);
		}
		_songPlayer = null;
		_songLoadAttempted = false;
	}

	private static void SetSongVolume(float volume)
	{
		if (_songPlayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_songPlayer))
		{
			float num = Math.Clamp(volume, 0f, 1f);
			_songPlayer.VolumeDb = ((num <= 0f) ? (-80f) : Mathf.LinearToDb(num));
		}
	}

	private static RunLocation? TryGetCurrentRunLocation()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			RunState obj = RunManager.Instance.DebugOnlyGetState();
			return (obj != null) ? new RunLocation?(obj.RunLocation) : null;
		}
		catch (Exception ex)
		{
			Log.Warn("CombatFlight could not determine the current run location: " + ex.Message, 2);
			return null;
		}
	}

	private static bool IsNewerSequence(uint sequence, uint previous)
	{
		if (sequence != previous)
		{
			return sequence - previous < 2147483648u;
		}
		return false;
	}

	private static bool IsFinite(Vector2 value)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		if (float.IsFinite(value.X))
		{
			return float.IsFinite(value.Y);
		}
		return false;
	}

	private static Vector2 ClampNetworkOffset(Vector2 value)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		return new Vector2(Math.Clamp(value.X, -10000f, 10000f), Math.Clamp(value.Y, -10000f, 10000f));
	}

	private static float NormalizeRotation(float value)
	{
		return Mathf.Wrap(value, -(float)Math.PI, (float)Math.PI);
	}

	private static bool CompleteGesture(ref ulong lastPressMsec, bool requiresDoubleTap)
	{
		if (!requiresDoubleTap)
		{
			lastPressMsec = 0uL;
			return true;
		}
		ulong ticksMsec = Time.GetTicksMsec();
		if (lastPressMsec != 0L && ticksMsec - lastPressMsec <= 350)
		{
			lastPressMsec = 0uL;
			return true;
		}
		lastPressMsec = ticksMsec;
		return false;
	}

	private static void ClearGestureState()
	{
		_lastStartPressMsec = 0uL;
		_lastStopPressMsec = 0uL;
	}

	private static bool MatchesBinding(InputEventKey keyEvent, long binding)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I8
		if (binding != 0L)
		{
			return (long)keyEvent.GetKeycodeWithModifiers() == binding;
		}
		return false;
	}

	private static bool IsConfiguredFlightControlKey(InputEventKey keyEvent)
	{
		if (!MatchesBinding(keyEvent, FlightSettings.MoveUp) && !MatchesBinding(keyEvent, FlightSettings.MoveDown) && !MatchesBinding(keyEvent, FlightSettings.MoveLeft) && !MatchesBinding(keyEvent, FlightSettings.MoveRight) && !MatchesBinding(keyEvent, FlightSettings.RotateLeft))
		{
			return MatchesBinding(keyEvent, FlightSettings.RotateRight);
		}
		return true;
	}

	private static void UpdateHeldControlState(InputEventKey keyEvent)
	{
		bool pressed = keyEvent.Pressed;
		if (MatchesBinding(keyEvent, FlightSettings.MoveUp))
		{
			_moveUpPressed = pressed;
		}
		if (MatchesBinding(keyEvent, FlightSettings.MoveDown))
		{
			_moveDownPressed = pressed;
		}
		if (MatchesBinding(keyEvent, FlightSettings.MoveLeft))
		{
			_moveLeftPressed = pressed;
		}
		if (MatchesBinding(keyEvent, FlightSettings.MoveRight))
		{
			_moveRightPressed = pressed;
		}
		if (MatchesBinding(keyEvent, FlightSettings.RotateLeft))
		{
			_rotateLeftPressed = pressed;
		}
		if (MatchesBinding(keyEvent, FlightSettings.RotateRight))
		{
			_rotateRightPressed = pressed;
		}
	}

	private static void ClearHeldControlState()
	{
		_moveUpPressed = false;
		_moveDownPressed = false;
		_moveLeftPressed = false;
		_moveRightPressed = false;
		_rotateLeftPressed = false;
		_rotateRightPressed = false;
	}

	private static void ShowNotification(string text)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		if (!FlightSettings.ShowFlightStatusText)
		{
			return;
		}
		NGame instance = NGame.Instance;
		if (instance == null)
		{
			return;
		}
		EnsureNotification(instance);
		if (_notificationLabel == null || !GodotObject.IsInstanceValid((GodotObject)(object)_notificationLabel))
		{
			return;
		}
		Tween? notificationTween = _notificationTween;
		if (notificationTween != null)
		{
			notificationTween.Kill();
		}
		_notificationLabel.Text = text;
		((CanvasItem)_notificationLabel).Modulate = Colors.White;
		((CanvasItem)_notificationLabel).Visible = true;
		_notificationTween = ((Node)_notificationLabel).CreateTween();
		_notificationTween.TweenInterval(1.1);
		_notificationTween.TweenProperty((GodotObject)(object)_notificationLabel, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), 0.25);
		_notificationTween.TweenCallback(Callable.From((Action)delegate
		{
			if (_notificationLabel != null && GodotObject.IsInstanceValid((GodotObject)(object)_notificationLabel))
			{
				((CanvasItem)_notificationLabel).Visible = false;
			}
		}));
	}

	private static void EnsureNotification(NGame game)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		if (_notificationLayer == null || !GodotObject.IsInstanceValid((GodotObject)(object)_notificationLayer) || _notificationLabel == null || !GodotObject.IsInstanceValid((GodotObject)(object)_notificationLabel))
		{
			_notificationLayer = new CanvasLayer
			{
				Name = StringName.op_Implicit("CombatFlightNotificationLayer"),
				Layer = 100
			};
			_notificationLabel = new Label
			{
				Name = StringName.op_Implicit("CombatFlightNotification"),
				HorizontalAlignment = (HorizontalAlignment)1,
				VerticalAlignment = (VerticalAlignment)1,
				MouseFilter = (MouseFilterEnum)2,
				ProcessMode = (ProcessModeEnum)3
			};
			((Control)_notificationLabel).SetAnchorsPreset((LayoutPreset)5, false);
			((Control)_notificationLabel).OffsetLeft = -500f;
			((Control)_notificationLabel).OffsetTop = 82f;
			((Control)_notificationLabel).OffsetRight = 500f;
			((Control)_notificationLabel).OffsetBottom = 126f;
			((Control)_notificationLabel).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 22);
			((Control)_notificationLabel).AddThemeConstantOverride(StringName.op_Implicit("outline_size"), 5);
			((Control)_notificationLabel).AddThemeColorOverride(StringName.op_Implicit("font_outline_color"), new Color(0f, 0f, 0f, 0.85f));
			GodotTreeExtensions.AddChildSafely((Node)(object)game, (Node)(object)_notificationLayer);
			GodotTreeExtensions.AddChildSafely((Node)(object)_notificationLayer, (Node)(object)_notificationLabel);
		}
	}

	private static void DisposeNotification()
	{
		Tween? notificationTween = _notificationTween;
		if (notificationTween != null)
		{
			notificationTween.Kill();
		}
		_notificationTween = null;
		if (_notificationLayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_notificationLayer))
		{
			GodotTreeExtensions.QueueFreeSafely((Node)(object)_notificationLayer);
		}
		_notificationLayer = null;
		_notificationLabel = null;
	}
}
