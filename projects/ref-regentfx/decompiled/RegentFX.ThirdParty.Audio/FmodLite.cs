using System;
using System.Collections.Generic;
using Godot;
using RegentFX.Scripts;

namespace RegentFX.ThirdParty.Audio;

public static class FmodLite
{
	private sealed record LoopSlot(FmodEventHandle Event, bool UsesLoopParam);

	private static readonly object Gate = new object();

	private static readonly StringName FmodServerName = new StringName("FmodServer");

	private static readonly StringName LoadBankMethod = new StringName("load_bank");

	private static readonly StringName UnloadBankMethod = new StringName("unload_bank");

	private static readonly StringName WaitForAllLoadsMethod = new StringName("wait_for_all_loads");

	private static readonly StringName BanksStillLoadingMethod = new StringName("banks_still_loading");

	private static readonly StringName CheckEventPathMethod = new StringName("check_event_path");

	private static readonly StringName CheckEventGuidMethod = new StringName("check_event_guid");

	private static readonly StringName CreateEventInstanceMethod = new StringName("create_event_instance");

	private static readonly StringName CreateEventInstanceWithGuidMethod = new StringName("create_event_instance_with_guid");

	private static readonly StringName StartMethod = new StringName("start");

	private static readonly StringName StopMethod = new StringName("stop");

	private static readonly StringName ReleaseMethod = new StringName("release");

	private static readonly StringName SetVolumeMethod = new StringName("set_volume");

	private static readonly StringName SetPitchMethod = new StringName("set_pitch");

	private static readonly StringName SetPausedMethod = new StringName("set_paused");

	private static readonly StringName SetParameterByNameMethod = new StringName("set_parameter_by_name");

	private static readonly StringName LoopParameterName = new StringName("loop");

	private static readonly StringName[] GuidMappingInjectCandidates = (StringName[])(object)new StringName[4]
	{
		new StringName("register_guid_path_mappings_from_file"),
		new StringName("inject_guid_mappings_from_file"),
		new StringName("register_strings_from_guid_file"),
		new StringName("load_guid_mapping_file")
	};

	private static readonly Dictionary<string, GodotObject> LoadedBankPins = new Dictionary<string, GodotObject>(StringComparer.Ordinal);

	private static Dictionary<string, string> EventPathToGuid = new Dictionary<string, string>(StringComparer.Ordinal);

	private static readonly Dictionary<string, List<LoopSlot>> LoopQueues = new Dictionary<string, List<LoopSlot>>(StringComparer.Ordinal);

	public static int EventMappingCount
	{
		get
		{
			lock (Gate)
			{
				return EventPathToGuid.Count;
			}
		}
	}

	private static IReadOnlyDictionary<string, float> EmptyParameters { get; } = new Dictionary<string, float>(0);


	public static bool TryLoadBank(string resourcePath, int loadBankMode = 0)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Invalid comparison between Unknown and I8
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrWhiteSpace(resourcePath) || !FileAccess.FileExists(resourcePath))
		{
			return false;
		}
		if (!TryCallServer(out var result, LoadBankMethod, Variant.op_Implicit(resourcePath), Variant.op_Implicit(loadBankMode)))
		{
			return false;
		}
		if ((long)((Variant)(ref result)).VariantType == 1)
		{
			return ((Variant)(ref result)).AsBool();
		}
		if ((int)((Variant)(ref result)).VariantType == 0)
		{
			return false;
		}
		GodotObject val = ((Variant)(ref result)).AsGodotObject();
		if (val == null || !GodotObject.IsInstanceValid(val))
		{
			return false;
		}
		lock (Gate)
		{
			LoadedBankPins[resourcePath] = val;
		}
		return true;
	}

	public static bool TryUnloadBank(string resourcePath)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		lock (Gate)
		{
			LoadedBankPins.Remove(resourcePath);
		}
		return TryCallServer(UnloadBankMethod, Variant.op_Implicit(resourcePath));
	}

	public static void TryWaitForAllLoads()
	{
		TryCallServer(WaitForAllLoadsMethod);
	}

	public static bool? TryBanksStillLoading()
	{
		if (!TryCallServer(out var result, BanksStillLoadingMethod))
		{
			return null;
		}
		return ((Variant)(ref result)).AsBool();
	}

	public static bool TryLoadGuidMappings(string resourcePath)
	{
		if (!TryParseGuidMappingsFromFile(resourcePath))
		{
			return false;
		}
		if (!TryCallNativeGuidInject(resourcePath))
		{
			return EventMappingCount > 0;
		}
		return true;
	}

	public static bool TryLoadBankAndGuidMappings(string bankResourcePath, string guidMapResourcePath, int loadBankMode = 0, bool waitForLoads = true)
	{
		bool num = TryLoadBank(bankResourcePath, loadBankMode);
		if (waitForLoads)
		{
			TryWaitForAllLoads();
		}
		bool flag = TryLoadGuidMappings(guidMapResourcePath);
		return num && flag;
	}

	public static bool IsMappedPath(string eventPath)
	{
		string guid;
		return TryGetMappedGuid(eventPath, out guid);
	}

	public static bool? TryCheckEventPath(string eventPath)
	{
		if (TryGetMappedGuid(eventPath, out string _))
		{
			return true;
		}
		return TryCheckEventPathByServerOnly(eventPath);
	}

	public static bool? TryCheckEventGuid(string eventGuid)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (!TryNormalizeGuidForAddon(eventGuid, out string bracedLowercase) || !TryCallServer(out var result, CheckEventGuidMethod, Variant.op_Implicit(bracedLowercase)))
		{
			return null;
		}
		return ((Variant)(ref result)).AsBool();
	}

	public static void RegisterGuidMapping(string eventPath, string eventGuid)
	{
		if (string.IsNullOrWhiteSpace(eventPath) || !TryNormalizeGuidForAddon(eventGuid, out string bracedLowercase))
		{
			return;
		}
		lock (Gate)
		{
			EventPathToGuid[eventPath] = bracedLowercase;
		}
	}

	public static void ClearGuidMappings()
	{
		lock (Gate)
		{
			EventPathToGuid = new Dictionary<string, string>(StringComparer.Ordinal);
		}
	}

	public static bool Play(string eventPath, float volume = 1f)
	{
		if (Setting.DisableModSounds)
		{
			return false;
		}
		return Play(eventPath, EmptyParameters, volume);
	}

	public static bool Play(string eventPath, string parameterName, float parameterValue, float volume = 1f)
	{
		if (Setting.DisableModSounds)
		{
			return false;
		}
		return Play(eventPath, new Dictionary<string, float> { [parameterName] = parameterValue }, volume);
	}

	public static bool Play(string eventPath, IReadOnlyDictionary<string, float> parameters, float volume = 1f)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		if (Setting.DisableModSounds)
		{
			return false;
		}
		GodotObject val = TryCreateRaw(eventPath);
		if (val == null)
		{
			return false;
		}
		try
		{
			if (volume != 1f)
			{
				val.Call(SetVolumeMethod, (Variant[])(object)new Variant[1] { Variant.op_Implicit(volume) });
			}
			foreach (KeyValuePair<string, float> parameter in parameters)
			{
				val.Call(SetParameterByNameMethod, (Variant[])(object)new Variant[2]
				{
					Variant.op_Implicit(parameter.Key),
					Variant.op_Implicit(parameter.Value)
				});
			}
			val.Call(StartMethod, Array.Empty<Variant>());
			val.Call(ReleaseMethod, Array.Empty<Variant>());
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool PlayByGuid(string eventGuid, float volume = 1f)
	{
		return PlayByGuid(eventGuid, EmptyParameters, volume);
	}

	public static bool PlayByGuid(string eventGuid, IReadOnlyDictionary<string, float> parameters, float volume = 1f)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		GodotObject val = TryCreateRawFromGuid(eventGuid);
		if (val == null)
		{
			return false;
		}
		try
		{
			if (volume != 1f)
			{
				val.Call(SetVolumeMethod, (Variant[])(object)new Variant[1] { Variant.op_Implicit(volume) });
			}
			foreach (KeyValuePair<string, float> parameter in parameters)
			{
				val.Call(SetParameterByNameMethod, (Variant[])(object)new Variant[2]
				{
					Variant.op_Implicit(parameter.Key),
					Variant.op_Implicit(parameter.Value)
				});
			}
			val.Call(StartMethod, Array.Empty<Variant>());
			val.Call(ReleaseMethod, Array.Empty<Variant>());
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool PlayLoop(string eventPath, bool usesLoopParam = true, IReadOnlyDictionary<string, float>? parameters = null, float volume = 1f)
	{
		FmodEventHandle eventObject;
		return PlayLoop(eventPath, out eventObject, usesLoopParam, parameters, volume);
	}

	public static bool PlayLoop(string eventPath, out FmodEventHandle? eventObject, bool usesLoopParam = true, IReadOnlyDictionary<string, float>? parameters = null, float volume = 1f)
	{
		GodotObject val = TryCreateRaw(eventPath);
		eventObject = null;
		if (val == null)
		{
			return false;
		}
		FmodEventHandle fmodEventHandle = new FmodEventHandle(val);
		try
		{
			if (volume != 1f)
			{
				fmodEventHandle.SetVolume(volume);
			}
			if (parameters != null)
			{
				foreach (KeyValuePair<string, float> parameter in parameters)
				{
					fmodEventHandle.SetParameter(parameter.Key, parameter.Value);
				}
			}
			if (!fmodEventHandle.Start())
			{
				fmodEventHandle.Release();
				return false;
			}
		}
		catch
		{
			fmodEventHandle.Release();
			return false;
		}
		lock (Gate)
		{
			if (!LoopQueues.TryGetValue(eventPath, out List<LoopSlot> value))
			{
				value = new List<LoopSlot>();
				LoopQueues[eventPath] = value;
			}
			value.Add(new LoopSlot(fmodEventHandle, usesLoopParam));
		}
		eventObject = fmodEventHandle;
		return true;
	}

	public static bool StopLoop(string eventPath)
	{
		LoopSlot slot;
		lock (Gate)
		{
			if (!LoopQueues.TryGetValue(eventPath, out List<LoopSlot> value) || value.Count == 0)
			{
				return false;
			}
			slot = value[0];
			value.RemoveAt(0);
			if (value.Count == 0)
			{
				LoopQueues.Remove(eventPath);
			}
		}
		return StopLoopSlot(slot);
	}

	public static void StopAllLoops()
	{
		List<LoopSlot> list = new List<LoopSlot>();
		lock (Gate)
		{
			foreach (List<LoopSlot> value in LoopQueues.Values)
			{
				list.AddRange(value);
			}
			LoopQueues.Clear();
		}
		foreach (LoopSlot item in list)
		{
			StopLoopSlot(item);
		}
	}

	public static bool SetParam(string eventPath, string parameterName, float value)
	{
		lock (Gate)
		{
			if (!LoopQueues.TryGetValue(eventPath, out List<LoopSlot> value2) || value2.Count == 0)
			{
				return false;
			}
			try
			{
				return value2[0].Event.SetParameter(parameterName, value);
			}
			catch
			{
				return false;
			}
		}
	}

	public static FmodEventHandle? CreateEvent(string eventPath, bool start = true, IReadOnlyDictionary<string, float>? parameters = null, float volume = 1f)
	{
		if (!TryCreateEvent(eventPath, out FmodEventHandle eventObject, start, parameters, volume))
		{
			return null;
		}
		return eventObject;
	}

	public static bool TryCreateEvent(string eventPath, out FmodEventHandle? eventObject, bool start = true, IReadOnlyDictionary<string, float>? parameters = null, float volume = 1f)
	{
		GodotObject val = TryCreateRaw(eventPath);
		eventObject = null;
		if (val == null)
		{
			return false;
		}
		FmodEventHandle fmodEventHandle = new FmodEventHandle(val);
		if (volume != 1f)
		{
			fmodEventHandle.SetVolume(volume);
		}
		if (parameters != null)
		{
			foreach (KeyValuePair<string, float> parameter in parameters)
			{
				fmodEventHandle.SetParameter(parameter.Key, parameter.Value);
			}
		}
		if (start && !fmodEventHandle.Start())
		{
			fmodEventHandle.Release();
			return false;
		}
		eventObject = fmodEventHandle;
		return true;
	}

	public static FmodEventHandle? CreateEventByGuid(string eventGuid, bool start = true, IReadOnlyDictionary<string, float>? parameters = null, float volume = 1f)
	{
		if (!TryCreateEventByGuid(eventGuid, out FmodEventHandle eventObject, start, parameters, volume))
		{
			return null;
		}
		return eventObject;
	}

	public static bool TryCreateEventByGuid(string eventGuid, out FmodEventHandle? eventObject, bool start = true, IReadOnlyDictionary<string, float>? parameters = null, float volume = 1f)
	{
		GodotObject val = TryCreateRawFromGuid(eventGuid);
		eventObject = null;
		if (val == null)
		{
			return false;
		}
		FmodEventHandle fmodEventHandle = new FmodEventHandle(val);
		if (volume != 1f)
		{
			fmodEventHandle.SetVolume(volume);
		}
		if (parameters != null)
		{
			foreach (KeyValuePair<string, float> parameter in parameters)
			{
				fmodEventHandle.SetParameter(parameter.Key, parameter.Value);
			}
		}
		if (start && !fmodEventHandle.Start())
		{
			fmodEventHandle.Release();
			return false;
		}
		eventObject = fmodEventHandle;
		return true;
	}

	private static GodotObject? TryCreateRaw(string eventPath)
	{
		if (string.IsNullOrWhiteSpace(eventPath))
		{
			return null;
		}
		if (!TryGetMappedGuid(eventPath, out string guid))
		{
			return TryCreateRawByPathOnly(eventPath);
		}
		GodotObject val = TryCreateRawFromGuid(guid);
		if (val != null)
		{
			return val;
		}
		if (!TryCheckEventPathByServerOnly(eventPath).GetValueOrDefault())
		{
			return null;
		}
		return TryCreateRawByPathOnly(eventPath);
	}

	private static GodotObject? TryCreateRawByPathOnly(string eventPath)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (!TryCallServer(out var result, CreateEventInstanceMethod, Variant.op_Implicit(eventPath)))
		{
			return null;
		}
		return ((Variant)(ref result)).AsGodotObject();
	}

	private static GodotObject? TryCreateRawFromGuid(string eventGuid)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (!TryNormalizeGuidForAddon(eventGuid, out string bracedLowercase))
		{
			return null;
		}
		if (!TryCallServer(out var result, CreateEventInstanceWithGuidMethod, Variant.op_Implicit(bracedLowercase)))
		{
			return null;
		}
		return ((Variant)(ref result)).AsGodotObject();
	}

	private static bool StopLoopSlot(LoopSlot slot)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (slot.UsesLoopParam)
			{
				slot.Event.RawInstance.Call(SetParameterByNameMethod, (Variant[])(object)new Variant[2]
				{
					Variant.op_Implicit(LoopParameterName),
					Variant.op_Implicit(1f)
				});
			}
			else
			{
				slot.Event.Stop(allowFadeOut: false);
			}
			slot.Event.Release();
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryParseGuidMappingsFromFile(string resourcePath)
	{
		if (string.IsNullOrWhiteSpace(resourcePath) || !FileAccess.FileExists(resourcePath))
		{
			return false;
		}
		FileAccess val = FileAccess.Open(resourcePath, (ModeFlags)1);
		try
		{
			if (val == null)
			{
				return false;
			}
			ParseGuidMappings(val.GetAsText(false));
			return true;
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	private static void ParseGuidMappings(string text)
	{
		string[] array = text.Replace("\r\n", "\n").Split('\n');
		Dictionary<string, string> dictionary;
		lock (Gate)
		{
			dictionary = new Dictionary<string, string>(EventPathToGuid, StringComparer.Ordinal);
		}
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string text2 = array2[i].Trim();
			if (text2.Length == 0 || text2[0] == '#')
			{
				continue;
			}
			int num = text2.IndexOf('}', StringComparison.Ordinal);
			if (num > 1 && text2[0] == '{' && Guid.TryParse(text2.Substring(1, num - 1).Trim(), out var result))
			{
				string text3 = ((num + 1 < text2.Length) ? text2.Substring(num + 1).TrimStart() : string.Empty);
				if (text3.StartsWith("event:", StringComparison.Ordinal))
				{
					dictionary[text3] = result.ToString("B");
				}
			}
		}
		lock (Gate)
		{
			EventPathToGuid = dictionary;
		}
	}

	private static bool TryCallNativeGuidInject(string resourcePath)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Invalid comparison between Unknown and I8
		GodotObject val = TryGetServer();
		if (val == null)
		{
			return false;
		}
		StringName[] guidMappingInjectCandidates = GuidMappingInjectCandidates;
		foreach (StringName val2 in guidMappingInjectCandidates)
		{
			if (!val.HasMethod(val2))
			{
				continue;
			}
			try
			{
				Variant val3 = val.Call(val2, (Variant[])(object)new Variant[1] { Variant.op_Implicit(resourcePath) });
				if ((long)((Variant)(ref val3)).VariantType == 1 && !((Variant)(ref val3)).AsBool())
				{
					continue;
				}
				return true;
			}
			catch
			{
			}
		}
		return false;
	}

	private static bool TryGetMappedGuid(string eventPath, out string guid)
	{
		guid = string.Empty;
		lock (Gate)
		{
			return EventPathToGuid.TryGetValue(eventPath, out guid) && !string.IsNullOrEmpty(guid);
		}
	}

	private static bool? TryCheckEventPathByServerOnly(string eventPath)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (!TryCallServer(out var result, CheckEventPathMethod, Variant.op_Implicit(eventPath)))
		{
			return null;
		}
		return ((Variant)(ref result)).AsBool();
	}

	private static bool TryNormalizeGuidForAddon(string raw, out string bracedLowercase)
	{
		bracedLowercase = string.Empty;
		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}
		string text = raw.Trim();
		if (text.Length >= 2 && text[0] == '{')
		{
			string text2 = text;
			if (text2[text2.Length - 1] == '}')
			{
				string text3 = text;
				text = text3.Substring(1, text3.Length - 1 - 1).Trim();
			}
		}
		if (!Guid.TryParse(text, out var result))
		{
			return false;
		}
		bracedLowercase = result.ToString("B");
		return true;
	}

	private static GodotObject? TryGetServer()
	{
		try
		{
			return Engine.HasSingleton(FmodServerName) ? Engine.GetSingleton(FmodServerName) : null;
		}
		catch
		{
			return null;
		}
	}

	private static bool TryCallServer(StringName method, params Variant[] args)
	{
		Variant result;
		return TryCallServer(out result, method, args);
	}

	private static bool TryCallServer(out Variant result, StringName method, params Variant[] args)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		result = default(Variant);
		GodotObject val = TryGetServer();
		if (val == null)
		{
			return false;
		}
		try
		{
			result = ((args.Length == 0) ? val.Call(method, Array.Empty<Variant>()) : val.Call(method, args));
			return true;
		}
		catch
		{
			return false;
		}
	}
}
