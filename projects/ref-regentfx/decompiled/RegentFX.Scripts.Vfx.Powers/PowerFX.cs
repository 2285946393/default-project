using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace RegentFX.Scripts.Vfx.Powers;

public abstract class PowerFX : FX
{
	protected static bool _registryInitialized;

	public static readonly Dictionary<Type, Type> Registry = new Dictionary<Type, Type>();

	public PowerModel? power;

	public bool Enabled => Setting.ToggleEnabled(GetToggleKey(GetType()));

	public static IEnumerable<Type> GetTypes => from t in typeof(PowerFX).Assembly.GetTypes()
		where t.IsSubclassOf(typeof(PowerFX)) && !t.IsAbstract
		select t;

	public static string GetToggleKey(Type type)
	{
		return "power_" + type.Name;
	}

	public static bool IsTypeEnabled<T>() where T : PowerFX
	{
		return Setting.ToggleEnabled(GetToggleKey(typeof(T)));
	}

	public static void EnsureRegistry()
	{
		if (_registryInitialized)
		{
			return;
		}
		_registryInitialized = true;
		foreach (Type getType in GetTypes)
		{
			PowerFxAttribute powerFxAttribute = getType.GetCustomAttributes(typeof(PowerFxAttribute), inherit: false).Cast<PowerFxAttribute>().FirstOrDefault();
			if (powerFxAttribute != null)
			{
				Registry[powerFxAttribute.PowerType] = getType;
			}
		}
	}

	public static PowerFX? FromPower(PowerModel power)
	{
		EnsureRegistry();
		if (power == null)
		{
			return null;
		}
		Type type = ((object)power).GetType();
		if (Registry.TryGetValue(type, out Type value))
		{
			PowerFX powerFX = (PowerFX)Activator.CreateInstance(value);
			if (powerFX != null && powerFX.Enabled)
			{
				powerFX.power = power;
				return powerFX;
			}
		}
		return null;
	}

	public virtual void BeforeBeforeApplied(Creature target, decimal amount)
	{
	}

	public virtual void AfterAfterRemoved(Creature target)
	{
	}

	public virtual void AfterSetAmount(decimal amount)
	{
	}
}
