using System;
using System.Collections.Generic;
using Godot;
using ModConfig;

namespace CombatFlight;

internal static class FlightSettings
{
	private const string ModId = "CombatFlight";

	private const string StartKey = "start_flight_key";

	private const string StopKey = "stop_flight_key";

	private const string ResetKey = "reset_flight_key";

	private const string MoveUpKey = "move_up_key";

	private const string MoveDownKey = "move_down_key";

	private const string MoveLeftKey = "move_left_key";

	private const string MoveRightKey = "move_right_key";

	private const string RotateLeftKey = "rotate_left_key";

	private const string RotateRightKey = "rotate_right_key";

	private const string DoubleTapStartKey = "double_tap_start";

	private const string DoubleTapStopKey = "double_tap_stop";

	private const string ShowStatusTextKey = "show_flight_status_text";

	private const string PlayBgmKey = "play_flight_bgm";

	private const string BgmVolumeKey = "flight_bgm_volume";

	public static long StartFlightKey => GetKey("start_flight_key");

	public static long StopFlightKey => GetKey("stop_flight_key");

	public static long ResetFlightKey => GetKey("reset_flight_key");

	public static long MoveUp => GetKey("move_up_key");

	public static long MoveDown => GetKey("move_down_key");

	public static long MoveLeft => GetKey("move_left_key");

	public static long MoveRight => GetKey("move_right_key");

	public static long RotateLeft => GetKey("rotate_left_key");

	public static long RotateRight => GetKey("rotate_right_key");

	public static bool DoubleTapStart => ModConfigApi.GetValue<bool>("CombatFlight", "double_tap_start");

	public static bool DoubleTapStop => ModConfigApi.GetValue<bool>("CombatFlight", "double_tap_stop");

	public static bool ShowFlightStatusText => ModConfigApi.GetValue<bool>("CombatFlight", "show_flight_status_text");

	public static bool PlayFlightBgm => ModConfigApi.GetValue<bool>("CombatFlight", "play_flight_bgm");

	public static float FlightBgmVolume => Math.Clamp(ModConfigApi.GetValue<float>("CombatFlight", "flight_bgm_volume") / 100f, 0f, 1f);

	public static void Register()
	{
		//IL_02fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0303: Unknown result type (might be due to invalid IL or missing references)
		//IL_030e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0319: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Unknown result type (might be due to invalid IL or missing references)
		//IL_033e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_036f: Unknown result type (might be due to invalid IL or missing references)
		//IL_037a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0385: Unknown result type (might be due to invalid IL or missing references)
		//IL_0390: Unknown result type (might be due to invalid IL or missing references)
		//IL_039b: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c1: Expected O, but got Unknown
		Dictionary<string, string> dictionary = new Dictionary<string, string>
		{
			["en"] = "Chinese People Can Fly",
			["zhs"] = "中国人能飞",
			["zht"] = "中國人能飛"
		};
		ModConfigApi.Register("CombatFlight", "Chinese People Can Fly", dictionary, (ConfigEntry[])(object)new ConfigEntry[17]
		{
			Header("Controls", "按键绑定", "按鍵綁定"),
			KeyBind("start_flight_key", "Start flight", "启动飞行", "啟動飛行", (Key)32, "Starts flight. This may use the same key as Stop flight.", "启动飞行；可以与“中止飞行”绑定为同一个按键。", "啟動飛行；可以與「中止飛行」綁定為同一個按鍵。"),
			KeyBind("stop_flight_key", "Stop flight", "中止飞行", "中止飛行", (Key)32, "Stops flight without resetting position. This may use the same key as Start flight.", "中止飞行但保留当前位置；可以与“启动飞行”绑定为同一个按键。", "中止飛行但保留目前位置；可以與「啟動飛行」綁定為同一個按鍵。"),
			KeyBind("reset_flight_key", "Reset", "复位", "復位", (Key)4194305, "Resets position and facing, then stops flight.", "恢复本场战斗的初始位置和朝向，并中止飞行。", "恢復本場戰鬥的初始位置和朝向，並中止飛行。"),
			KeyBind("move_up_key", "Move up", "向上移动", "向上移動", (Key)87),
			KeyBind("move_down_key", "Move down", "向下移动", "向下移動", (Key)83),
			KeyBind("move_left_key", "Move left", "向左移动", "向左移動", (Key)65),
			KeyBind("move_right_key", "Move right", "向右移动", "向右移動", (Key)68),
			KeyBind("rotate_left_key", "Rotate left", "向左旋转", "向左旋轉", (Key)81),
			KeyBind("rotate_right_key", "Rotate right", "向右旋转", "向右旋轉", (Key)69),
			Header("Flight behavior", "飞行行为", "飛行行為"),
			Toggle("double_tap_start", "Double-tap to start", "双击启动", "雙擊啟動", defaultValue: true, "When enabled, press the Start flight key twice within 350 ms.", "启用后，需要在 350 毫秒内连续按两次“启动飞行”按键。", "啟用後，需要在 350 毫秒內連續按兩次「啟動飛行」按鍵。", delegate
			{
				FlightController.OnControlSettingsChanged();
			}),
			Toggle("double_tap_stop", "Double-tap to stop", "双击中止", "雙擊中止", defaultValue: true, "When enabled, press the Stop flight key twice within 350 ms.", "启用后，需要在 350 毫秒内连续按两次“中止飞行”按键。", "啟用後，需要在 350 毫秒內連續按兩次「中止飛行」按鍵。", delegate
			{
				FlightController.OnControlSettingsChanged();
			}),
			Toggle("show_flight_status_text", "Show flight status text", "显示飞行状态提示", "顯示飛行狀態提示", defaultValue: true, "Shows a small flight status message near the upper middle of the combat screen.", "在战斗画面中上方显示小型飞行状态提示。", "在戰鬥畫面中上方顯示小型飛行狀態提示。"),
			Header("Audio", "音频", "音訊"),
			Toggle("play_flight_bgm", "Play flight BGM", "播放飞行 BGM", "播放飛行 BGM", defaultValue: true, "Play the flight song and pause the normal background music while any player is flying. When disabled, normal BGM is left untouched.", "任意玩家飞行时播放飞行歌曲并暂停普通背景音乐。关闭后不会影响原背景音乐。", "任意玩家飛行時播放飛行歌曲並暫停普通背景音樂。關閉後不會影響原背景音樂。", delegate
			{
				FlightController.OnAudioSettingsChanged();
			}),
			new ConfigEntry
			{
				Key = "flight_bgm_volume",
				Label = "Flight BGM volume",
				Labels = Localized("飞行 BGM 音量", "Flight BGM volume", "飛行 BGM 音量"),
				Description = "Adjusts only the flight song volume. The game's master volume still applies.",
				Descriptions = Localized("只调整飞行歌曲音量；游戏主音量仍然生效。", "Adjusts only the flight song volume. The game's master volume still applies.", "只調整飛行歌曲音量；遊戲主音量仍然生效。"),
				Type = (ConfigType)1,
				DefaultValue = 50f,
				Min = 0f,
				Max = 100f,
				Step = 1f,
				Format = "0'%'",
				OnChanged = delegate
				{
					FlightController.OnAudioSettingsChanged();
				}
			}
		});
	}

	private static long GetKey(string key)
	{
		return ModConfigApi.GetValue<long>("CombatFlight", key);
	}

	private static ConfigEntry Header(string english, string simplifiedChinese, string traditionalChinese)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		return new ConfigEntry
		{
			Label = english,
			Labels = Localized(simplifiedChinese, english, traditionalChinese),
			Type = (ConfigType)5
		};
	}

	private static ConfigEntry KeyBind(string key, string english, string simplifiedChinese, string traditionalChinese, Key defaultKey, string? englishDescription = null, string? simplifiedChineseDescription = null, string? traditionalChineseDescription = null)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected I8, but got Unknown
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Expected O, but got Unknown
		return new ConfigEntry
		{
			Key = key,
			Label = english,
			Labels = Localized(simplifiedChinese, english, traditionalChinese),
			Description = (englishDescription ?? string.Empty),
			Descriptions = ((englishDescription == null) ? null : Localized(simplifiedChineseDescription ?? string.Empty, englishDescription, traditionalChineseDescription ?? string.Empty)),
			Type = (ConfigType)3,
			DefaultValue = (long)defaultKey,
			OnChanged = delegate
			{
				FlightController.OnControlSettingsChanged();
			}
		};
	}

	private static ConfigEntry Toggle(string key, string english, string simplifiedChinese, string traditionalChinese, bool defaultValue, string englishDescription, string simplifiedChineseDescription, string traditionalChineseDescription, Action<object>? onChanged = null)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		return new ConfigEntry
		{
			Key = key,
			Label = english,
			Labels = Localized(simplifiedChinese, english, traditionalChinese),
			Description = englishDescription,
			Descriptions = Localized(simplifiedChineseDescription, englishDescription, traditionalChineseDescription),
			Type = (ConfigType)0,
			DefaultValue = defaultValue,
			OnChanged = onChanged
		};
	}

	private static Dictionary<string, string> Localized(string simplifiedChinese, string english, string traditionalChinese)
	{
		return new Dictionary<string, string>
		{
			["en"] = english,
			["zhs"] = simplifiedChinese,
			["zht"] = traditionalChinese
		};
	}
}
