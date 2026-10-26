using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]
[assembly: AssemblyCompany("STS2_Things.Bootstrap")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.25.10.0")]
[assembly: AssemblyInformationalVersion("1.25.10+e813212c46f1418e541a10e447deb3e37e65e38d")]
[assembly: AssemblyProduct("STS2_Things.Bootstrap")]
[assembly: AssemblyTitle("STS2_Things.Bootstrap")]
[assembly: AssemblyVersion("1.25.10.0")]
[module: RefSafetyRules(11)]
[CompilerGenerated]
internal sealed class <>z__ReadOnlySingleElementList<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	private sealed class Enumerator : IDisposable, IEnumerator, IEnumerator<T>
	{
		object IEnumerator.Current => _item;

		T IEnumerator<T>.Current => _item;

		public Enumerator(T item)
		{
			_item = item;
		}

		bool IEnumerator.MoveNext()
		{
			if (!_moveNextCalled)
			{
				return _moveNextCalled = true;
			}
			return false;
		}

		void IEnumerator.Reset()
		{
			_moveNextCalled = false;
		}

		void IDisposable.Dispose()
		{
		}
	}

	int ICollection.Count => 1;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	object? IList.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	int IReadOnlyCollection<T>.Count => 1;

	T IReadOnlyList<T>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
	}

	int ICollection<T>.Count => 1;

	bool ICollection<T>.IsReadOnly => true;

	T IList<T>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public <>z__ReadOnlySingleElementList(T item)
	{
		_item = item;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(_item);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		array.SetValue(_item, index);
	}

	int IList.Add(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	bool IList.Contains(object? value)
	{
		return EqualityComparer<T>.Default.Equals(_item, (T)value);
	}

	int IList.IndexOf(object? value)
	{
		if (!EqualityComparer<T>.Default.Equals(_item, (T)value))
		{
			return -1;
		}
		return 0;
	}

	void IList.Insert(int index, object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Remove(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return new Enumerator(_item);
	}

	void ICollection<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.Clear()
	{
		throw new NotSupportedException();
	}

	bool ICollection<T>.Contains(T item)
	{
		return EqualityComparer<T>.Default.Equals(_item, item);
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		array[arrayIndex] = _item;
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	int IList<T>.IndexOf(T item)
	{
		if (!EqualityComparer<T>.Default.Equals(_item, item))
		{
			return -1;
		}
		return 0;
	}

	void IList<T>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	void IList<T>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}
}
namespace STS2_Things.Bootstrap;

[ModInitializer("Initialize")]
public static class UnifiedBootstrap
{
	private const string ModId = "STS2_Things";

	private const string V1071Target = "v107.1";

	private const string V111Target = "v111";

	private const string V1071Resource = "STS2_Things.Implementations.v107.1.dll";

	private const string V111Resource = "STS2_Things.Implementations.v111.dll";

	private const string ImplementationAssemblyName = "STS2_Things";

	private const string LegacyHarmonyId = "Adnermo.STS2_Things.UnifiedBootstrap.V1071";

	private static readonly object InitLock = new object();

	private static Assembly? _gameAssembly;

	private static Assembly? _implementationAssembly;

	private static Harmony? _legacyHarmony;

	private static bool _initialized;

	public static string? SelectedTarget { get; private set; }

	public static void Initialize()
	{
		lock (InitLock)
		{
			if (_initialized)
			{
				Console.WriteLine("[STS2_Things] Unified bootstrap duplicate initialization ignored.");
				return;
			}
			_gameAssembly = typeof(ModInitializerAttribute).Assembly;
			SelectedTarget = DetectTarget(_gameAssembly);
			_implementationAssembly = LoadImplementation(SelectedTarget);
			if (SelectedTarget == "v111")
			{
				AssociateV111Implementation(_gameAssembly, _implementationAssembly);
			}
			else
			{
				InstallV1071RegistrationBridge(_gameAssembly);
			}
			InvokeImplementationInitializer(_implementationAssembly);
			_initialized = true;
			Console.WriteLine($"[STS2_Things] Unified package selected {SelectedTarget} implementation ({_implementationAssembly.GetName().Name}).");
		}
	}

	public static string SelectImplementationResource(Assembly gameAssembly)
	{
		string text = DetectTarget(gameAssembly);
		if (!(text == "v107.1"))
		{
			if (text == "v111")
			{
				return "STS2_Things.Implementations.v111.dll";
			}
			throw new InvalidOperationException("Unsupported STS2 target.");
		}
		return "STS2_Things.Implementations.v107.1.dll";
	}

	private static string DetectTarget(Assembly gameAssembly)
	{
		int[] array = (from method in gameAssembly.GetType("MegaCrit.Sts2.Core.Models.AbstractModel", throwOnError: true).GetMethods(BindingFlags.Instance | BindingFlags.Public)
			where method.Name == "ModifyDamageMultiplicative"
			select method.GetParameters().Length).Distinct().Order().ToArray();
		if (array.SequenceEqual(new <>z__ReadOnlySingleElementList<int>(5)))
		{
			return "v107.1";
		}
		if (array.SequenceEqual(new <>z__ReadOnlySingleElementList<int>(6)) && (object)gameAssembly.GetType("MegaCrit.Sts2.Core.Combat.CombatId") != null)
		{
			if ((object)gameAssembly.GetType("MegaCrit.Sts2.Core.Multiplayer.Connection.HandshakeManager") != null)
			{
				return "v111";
			}
			throw new NotSupportedException("STS2_Things 1.25.10 supports STS2 v0.107.1 and v0.111.x; detected a v0.110.x assembly. Update the game to v0.111.x or install the previous mod release that supports v0.110.x.");
		}
		throw new NotSupportedException("STS2_Things 1.25.10 supports STS2 v0.107.1 and v0.111.x. Detected ModifyDamageMultiplicative parameter counts: " + string.Join(", ", array) + ".");
	}

	private static Assembly LoadImplementation(string target)
	{
		string text = ((target == "v107.1") ? "STS2_Things.Implementations.v107.1.dll" : "STS2_Things.Implementations.v111.dll");
		Assembly assembly = typeof(UnifiedBootstrap).Assembly;
		using Stream assembly2 = assembly.GetManifestResourceStream(text) ?? throw new MissingManifestResourceException("Embedded implementation resource is missing: " + text);
		Assembly assembly3 = (AssemblyLoadContext.GetLoadContext(assembly) ?? AssemblyLoadContext.Default).LoadFromStream(assembly2);
		if (!string.Equals(assembly3.GetName().Name, "STS2_Things", StringComparison.Ordinal))
		{
			throw new InvalidOperationException($"Embedded {target} implementation has assembly name '{assembly3.GetName().Name}', expected '{"STS2_Things"}'.");
		}
		return assembly3;
	}

	private static void AssociateV111Implementation(Assembly gameAssembly, Assembly implementationAssembly)
	{
		Type type = gameAssembly.GetType("MegaCrit.Sts2.Core.Modding.ModManager", throwOnError: true);
		InvokeAndUnwrap(type.GetMethod("AssociateAssemblyWithMod", BindingFlags.Static | BindingFlags.Public, null, new Type[2]
		{
			typeof(string),
			typeof(Assembly)
		}, null) ?? throw new MissingMethodException(type.FullName, "AssociateAssemblyWithMod(string, Assembly)"), null, new object[2] { "STS2_Things", implementationAssembly });
	}

	private static void InstallV1071RegistrationBridge(Assembly gameAssembly)
	{
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Expected O, but got Unknown
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Expected O, but got Unknown
		Type type = gameAssembly.GetType("MegaCrit.Sts2.Core.Helpers.ReflectionHelper", throwOnError: true);
		MethodInfo methodInfo = type.GetProperty("ModTypes", BindingFlags.Static | BindingFlags.Public)?.GetMethod ?? throw new MissingMethodException(type.FullName, "get_ModTypes");
		Type type2 = gameAssembly.GetType("MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache", throwOnError: true);
		MethodInfo methodInfo2 = type2.GetMethod("Init", BindingFlags.Static | BindingFlags.Public) ?? throw new MissingMethodException(type2.FullName, "Init");
		_legacyHarmony = new Harmony("Adnermo.STS2_Things.UnifiedBootstrap.V1071");
		_legacyHarmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(UnifiedBootstrap).GetMethod("AppendV1071ImplementationTypes", BindingFlags.Static | BindingFlags.NonPublic)), (HarmonyMethod)null, (HarmonyMethod)null);
		_legacyHarmony.Patch((MethodBase)methodInfo2, new HarmonyMethod(typeof(UnifiedBootstrap).GetMethod("PromoteV1071ImplementationAssembly", BindingFlags.Static | BindingFlags.NonPublic)), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
	}

	private static void AppendV1071ImplementationTypes(ref Type[] __result)
	{
		Assembly assembly = _implementationAssembly ?? throw new InvalidOperationException("V107.1 implementation is not loaded.");
		Type[] collection = __result ?? Array.Empty<Type>();
		HashSet<Type> hashSet = new HashSet<Type>(collection);
		List<Type> list = new List<Type>();
		list.AddRange(collection);
		list.AddRange(assembly.GetTypes().Where(hashSet.Add));
		__result = list.ToArray();
	}

	private static void PromoteV1071ImplementationAssembly()
	{
		Assembly? obj = _gameAssembly ?? throw new InvalidOperationException("STS2 assembly is not initialized.");
		Assembly value = _implementationAssembly ?? throw new InvalidOperationException("V107.1 implementation is not loaded.");
		foreach (object item in (obj.GetType("MegaCrit.Sts2.Core.Modding.ModManager", throwOnError: true).GetProperty("Mods", BindingFlags.Static | BindingFlags.Public)?.GetValue(null) as IEnumerable) ?? throw new InvalidOperationException("ModManager.Mods is unavailable."))
		{
			if (item != null && IsLoadedTargetMod(item))
			{
				(item.GetType().GetField("assembly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new MissingFieldException(item.GetType().FullName, "assembly")).SetValue(item, value);
				return;
			}
		}
		throw new InvalidOperationException("Could not promote the V107.1 implementation to the loaded STS2_Things Mod.");
	}

	private static bool IsLoadedTargetMod(object mod)
	{
		Type type = mod.GetType();
		object obj = type.GetField("manifest", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(mod);
		if (obj == null)
		{
			return false;
		}
		string a = obj.GetType().GetField("id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(obj) as string;
		string a2 = type.GetField("state", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(mod)?.ToString();
		if (string.Equals(a, "STS2_Things", StringComparison.Ordinal))
		{
			return string.Equals(a2, "Loaded", StringComparison.Ordinal);
		}
		return false;
	}

	private static void InvokeImplementationInitializer(Assembly implementationAssembly)
	{
		Type type = implementationAssembly.GetType("STS2_ThingsInit", throwOnError: true);
		InvokeAndUnwrap(type.GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public) ?? throw new MissingMethodException(type.FullName, "Initialize"), null, null);
	}

	private static object? InvokeAndUnwrap(MethodInfo method, object? instance, object?[]? arguments)
	{
		try
		{
			return method.Invoke(instance, arguments);
		}
		catch (TargetInvocationException ex) when (ex.InnerException != null)
		{
			ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
			throw;
		}
	}
}
