using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace RegentFX.Scripts.Vfx;

public static class WorldEnvironmentUtil
{
	private static WorldEnvironment? _cachedEnv;

	public const bool ENABLE_EXPOSURE = true;

	public static WorldEnvironment? GetOrActivateEnvironment()
	{
		if (_cachedEnv != null && GodotObject.IsInstanceValid((GodotObject)(object)_cachedEnv))
		{
			return _cachedEnv;
		}
		if (NGame.Instance == null)
		{
			Entry.Logger.Warn("NGame.Instance is null, cannot activate WorldEnvironment.", 1);
			return null;
		}
		_cachedEnv = NGame.Instance.ActivateWorldEnvironment();
		return _cachedEnv;
	}

	public static void DeactivateEnvironment()
	{
		if (NGame.Instance == null)
		{
			Entry.Logger.Warn("NGame.Instance is null, cannot deactivate WorldEnvironment.", 1);
			return;
		}
		NGame.Instance.DeactivateWorldEnvironment();
		_cachedEnv = null;
	}

	public static void SetGlowIntensity(float intensity)
	{
		WorldEnvironment orActivateEnvironment = GetOrActivateEnvironment();
		if (orActivateEnvironment != null)
		{
			orActivateEnvironment.Environment.GlowIntensity = intensity;
		}
	}

	public static Tween? TweenGlowIntensity(float intensity, float duration, EaseType ease = 2L, TransitionType trans = 7L)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		WorldEnvironment orActivateEnvironment = GetOrActivateEnvironment();
		if (orActivateEnvironment == null)
		{
			return null;
		}
		Tween obj = ((Node)orActivateEnvironment).CreateTween();
		if (obj != null)
		{
			obj.TweenProperty((GodotObject)(object)orActivateEnvironment, NodePath.op_Implicit("environment:glow_intensity"), Variant.op_Implicit(intensity), (double)duration).SetEase(ease).SetTrans(trans);
			return obj;
		}
		return obj;
	}

	public static void SetExposure(float exposure)
	{
		WorldEnvironment orActivateEnvironment = GetOrActivateEnvironment();
		if (orActivateEnvironment != null)
		{
			orActivateEnvironment.Environment.TonemapExposure = exposure;
		}
	}

	public static async Task FullExposure(float exposure, float waitTime, float inTime, float outTime)
	{
		await VFXUtil.Wait(waitTime);
		TweenExposure(exposure, inTime, (EaseType)2, (TransitionType)7);
		await VFXUtil.Wait(inTime);
		TweenExposure(1f, outTime, (EaseType)2, (TransitionType)7);
	}

	public static Tween? TweenExposure(float exposure, float duration, EaseType ease = 2L, TransitionType trans = 7L)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		float exposureThreshold = Setting.ExposureThreshold;
		if (exposureThreshold <= 0.001f)
		{
			return null;
		}
		float num = (exposure - 1f) * exposureThreshold + exposure;
		WorldEnvironment orActivateEnvironment = GetOrActivateEnvironment();
		if (orActivateEnvironment == null)
		{
			return null;
		}
		Tween obj = ((Node)orActivateEnvironment).CreateTween();
		if (obj != null)
		{
			obj.TweenProperty((GodotObject)(object)orActivateEnvironment, NodePath.op_Implicit("environment:tonemap_exposure"), Variant.op_Implicit(num), (double)duration).SetEase(ease).SetTrans(trans);
			return obj;
		}
		return obj;
	}

	public static void SetBrightness(float brightness)
	{
		WorldEnvironment orActivateEnvironment = GetOrActivateEnvironment();
		if (orActivateEnvironment != null)
		{
			orActivateEnvironment.Environment.AdjustmentBrightness = brightness;
		}
	}

	public static Tween? TweenBrightness(float brightness, float duration, EaseType ease = 2L, TransitionType trans = 7L)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		WorldEnvironment orActivateEnvironment = GetOrActivateEnvironment();
		if (orActivateEnvironment == null)
		{
			return null;
		}
		Tween obj = ((Node)orActivateEnvironment).CreateTween();
		if (obj != null)
		{
			obj.TweenProperty((GodotObject)(object)orActivateEnvironment, NodePath.op_Implicit("environment:adjustment_brightness"), Variant.op_Implicit(brightness), (double)duration).SetEase(ease).SetTrans(trans);
			return obj;
		}
		return obj;
	}

	public static void SetContrast(float contrast)
	{
		WorldEnvironment orActivateEnvironment = GetOrActivateEnvironment();
		if (orActivateEnvironment != null)
		{
			orActivateEnvironment.Environment.AdjustmentContrast = contrast;
		}
	}

	public static Tween? TweenContrast(float contrast, float duration, EaseType ease = 2L, TransitionType trans = 7L)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		WorldEnvironment orActivateEnvironment = GetOrActivateEnvironment();
		if (orActivateEnvironment == null)
		{
			return null;
		}
		Tween obj = ((Node)orActivateEnvironment).CreateTween();
		if (obj != null)
		{
			obj.TweenProperty((GodotObject)(object)orActivateEnvironment, NodePath.op_Implicit("environment:adjustment_contrast"), Variant.op_Implicit(contrast), (double)duration).SetEase(ease).SetTrans(trans);
			return obj;
		}
		return obj;
	}

	public static void SetSaturation(float saturation)
	{
		WorldEnvironment orActivateEnvironment = GetOrActivateEnvironment();
		if (orActivateEnvironment != null)
		{
			orActivateEnvironment.Environment.AdjustmentSaturation = saturation;
		}
	}

	public static Tween? TweenSaturation(float saturation, float duration, EaseType ease = 2L, TransitionType trans = 7L)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		WorldEnvironment orActivateEnvironment = GetOrActivateEnvironment();
		if (orActivateEnvironment == null)
		{
			return null;
		}
		Tween obj = ((Node)orActivateEnvironment).CreateTween();
		if (obj != null)
		{
			obj.TweenProperty((GodotObject)(object)orActivateEnvironment, NodePath.op_Implicit("environment:adjustment_saturation"), Variant.op_Implicit(saturation), (double)duration).SetEase(ease).SetTrans(trans);
			return obj;
		}
		return obj;
	}

	public static void ResetToDefaults()
	{
		WorldEnvironment orActivateEnvironment = GetOrActivateEnvironment();
		if (orActivateEnvironment != null)
		{
			orActivateEnvironment.Environment.TonemapExposure = 1f;
			orActivateEnvironment.Environment.AdjustmentBrightness = 1f;
			orActivateEnvironment.Environment.AdjustmentContrast = 1f;
			orActivateEnvironment.Environment.AdjustmentSaturation = 1f;
			orActivateEnvironment.Environment.GlowIntensity = 0.8f;
		}
	}

	public static Tween? TweenResetToDefaults(float duration)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		WorldEnvironment orActivateEnvironment = GetOrActivateEnvironment();
		if (orActivateEnvironment == null)
		{
			return null;
		}
		Tween obj = ((Node)orActivateEnvironment).CreateTween().SetParallel(true);
		obj.TweenProperty((GodotObject)(object)orActivateEnvironment, NodePath.op_Implicit("environment:tonemap_exposure"), Variant.op_Implicit(1f), (double)duration);
		obj.TweenProperty((GodotObject)(object)orActivateEnvironment, NodePath.op_Implicit("environment:adjustment_brightness"), Variant.op_Implicit(1f), (double)duration);
		obj.TweenProperty((GodotObject)(object)orActivateEnvironment, NodePath.op_Implicit("environment:adjustment_contrast"), Variant.op_Implicit(1f), (double)duration);
		obj.TweenProperty((GodotObject)(object)orActivateEnvironment, NodePath.op_Implicit("environment:adjustment_saturation"), Variant.op_Implicit(1f), (double)duration);
		obj.TweenProperty((GodotObject)(object)orActivateEnvironment, NodePath.op_Implicit("environment:glow_intensity"), Variant.op_Implicit(0.8f), (double)duration);
		return obj;
	}
}
