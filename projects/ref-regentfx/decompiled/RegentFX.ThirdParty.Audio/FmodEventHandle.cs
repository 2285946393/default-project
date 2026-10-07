using System;
using Godot;

namespace RegentFX.ThirdParty.Audio;

public sealed class FmodEventHandle : IDisposable
{
	private readonly GodotObject _instance;

	private bool _released;

	public GodotObject RawInstance => _instance;

	public bool IsReleased => _released;

	internal FmodEventHandle(GodotObject instance)
	{
		_instance = instance;
	}

	public bool Start()
	{
		return TryCall("start");
	}

	public bool Stop(bool allowFadeOut = true)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		return TryCall("stop", Variant.op_Implicit((!allowFadeOut) ? 1 : 0));
	}

	public bool SetParameter(string name, float value)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		return TryCall("set_parameter_by_name", Variant.op_Implicit(name), Variant.op_Implicit(value));
	}

	public bool SetVolume(float volume)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		return TryCall("set_volume", Variant.op_Implicit(volume));
	}

	public bool SetPitch(float pitch)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		return TryCall("set_pitch", Variant.op_Implicit(pitch));
	}

	public bool SetPaused(bool paused)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		return TryCall("set_paused", Variant.op_Implicit(paused));
	}

	public bool Release()
	{
		if (_released)
		{
			return true;
		}
		return _released = TryCall("release");
	}

	public void Dispose()
	{
		Stop();
		Release();
	}

	private bool TryCall(string method, params Variant[] args)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		if (_released)
		{
			return false;
		}
		try
		{
			_instance.Call(StringName.op_Implicit(method), args);
			return true;
		}
		catch
		{
			return false;
		}
	}
}
