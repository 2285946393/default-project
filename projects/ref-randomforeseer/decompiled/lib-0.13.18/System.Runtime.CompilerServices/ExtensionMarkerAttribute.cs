using Microsoft.CodeAnalysis;

namespace System.Runtime.CompilerServices;

[CompilerGenerated]
[Embedded]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
internal sealed class ExtensionMarkerAttribute : Attribute
{
	private readonly string _003CName_003Ek__BackingField;

	public string Name => _003CName_003Ek__BackingField;

	public ExtensionMarkerAttribute(string name)
	{
		_003CName_003Ek__BackingField = name;
	}
}
