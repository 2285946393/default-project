using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.Common.Mirrors;

internal class MirrorMethodSpec
{
	private readonly BindingFlags _lookupFlags;

	private readonly Type[] _parameterTypes;

	public string Name { get; }

	public MethodInfo BaseMethod { get; }

	public MirrorMethodSpec(Type declaringType, string name, BindingFlags bindingFlags, Type[] parameterTypes)
	{
		Name = name;
		_lookupFlags = bindingFlags & ~BindingFlags.DeclaredOnly;
		_parameterTypes = parameterTypes;
		BaseMethod = declaringType.GetMethod(name, _lookupFlags | BindingFlags.DeclaredOnly, null, parameterTypes, null) ?? throw new MissingMethodException(declaringType.FullName, name);
	}

	public static MirrorMethodSpec Hook(string name, Type[] parameterTypes)
	{
		return new MirrorMethodSpec(typeof(AbstractModel), name, BindingFlags.Instance | BindingFlags.Public, parameterTypes);
	}

	public bool TryGetOverride(Type receiverType, [NotNullWhen(true)] out MethodInfo? overrideMethod)
	{
		overrideMethod = receiverType.GetMethod(Name, _lookupFlags, null, _parameterTypes, null);
		if ((object)overrideMethod == null || overrideMethod.DeclaringType == BaseMethod.DeclaringType)
		{
			overrideMethod = null;
			return false;
		}
		return true;
	}
}
