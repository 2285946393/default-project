using System;

namespace RegentFX.Scripts.Vfx.Powers;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class PowerFxAttribute : Attribute
{
	public Type PowerType { get; }

	public PowerFxAttribute(Type powerType)
	{
		PowerType = powerType;
		base._002Ector();
	}
}
