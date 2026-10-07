using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Config;
using BaseLib.Extensions;
using BaseLib.Patches.Content;
using BaseLib.Utils;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Orbs;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.addons.mega_text;
using wuwancients.Ancients;
using wuwancients.Cards;
using wuwancients.Config;
using wuwancients.Enchantments;
using wuwancients.Events;
using wuwancients.Keywords;
using wuwancients.Potions;
using wuwancients.Powers;
using wuwancients.Relics;
using wuwancients.Scripts;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]
[assembly: AssemblyCompany("wuwancients")]
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0+89d117f248a0a5c7a2998e4404e4d8d31638fce8")]
[assembly: AssemblyProduct("wuwancients")]
[assembly: AssemblyTitle("wuwancients")]
[assembly: AssemblyHasScripts(new Type[]
{
	typeof(NSuisuiMysteryShop),
	typeof(Shorekeeperaudio)
})]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("1.0.0.0")]
[module: UnverifiableCode]
[module: RefSafetyRules(11)]
[CompilerGenerated]
internal sealed class <>z__ReadOnlyArray<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	int ICollection.Count => _items.Length;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	object? IList.this[int index]
	{
		get
		{
			return _items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	int IReadOnlyCollection<T>.Count => _items.Length;

	T IReadOnlyList<T>.this[int index] => _items[index];

	int ICollection<T>.Count => _items.Length;

	bool ICollection<T>.IsReadOnly => true;

	T IList<T>.this[int index]
	{
		get
		{
			return _items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public <>z__ReadOnlyArray(T[] items)
	{
		_items = items;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)_items).GetEnumerator();
	}

	void ICollection.CopyTo(Array array, int index)
	{
		((ICollection)_items).CopyTo(array, index);
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
		return ((IList)_items).Contains(value);
	}

	int IList.IndexOf(object? value)
	{
		return ((IList)_items).IndexOf(value);
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
		return ((IEnumerable<T>)_items).GetEnumerator();
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
		return ((ICollection<T>)_items).Contains(item);
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		((ICollection<T>)_items).CopyTo(array, arrayIndex);
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	int IList<T>.IndexOf(T item)
	{
		return ((IList<T>)_items).IndexOf(item);
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
			return !_moveNextCalled && (_moveNextCalled = true);
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
		return (!EqualityComparer<T>.Default.Equals(_item, (T)value)) ? (-1) : 0;
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
		return (!EqualityComparer<T>.Default.Equals(_item, item)) ? (-1) : 0;
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
[ScriptPath("res://wuwancients/scenes/Shorekeeperaudio.cs")]
public class Shorekeeperaudio : Control
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName audio1 = StringName.op_Implicit("audio1");

		public static readonly StringName audio2 = StringName.op_Implicit("audio2");

		public static readonly StringName audio3 = StringName.op_Implicit("audio3");
	}

	public class SignalName : SignalName
	{
	}

	private AudioStreamPlayer? audio1;

	private AudioStreamPlayer? audio2;

	private AudioStreamPlayer? audio3;

	public override void _Ready()
	{
		audio1 = ((Node)this).GetNodeOrNull<AudioStreamPlayer>(NodePath.op_Implicit("Audio1"));
		audio2 = ((Node)this).GetNodeOrNull<AudioStreamPlayer>(NodePath.op_Implicit("Audio2"));
		audio3 = ((Node)this).GetNodeOrNull<AudioStreamPlayer>(NodePath.op_Implicit("Audio3"));
		AudioStreamPlayer[] array = ((IEnumerable<AudioStreamPlayer>)(object)new AudioStreamPlayer[3] { audio1, audio2, audio3 }).Where((AudioStreamPlayer p) => p != null).ToArray();
		if (array.Length != 0)
		{
			Random random = new Random();
			int num = random.Next(0, array.Length);
			array[num].Play(0f);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		List<MethodInfo> list = new List<MethodInfo>(1);
		list.Add(new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
		return list;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.audio1)
		{
			audio1 = VariantUtils.ConvertTo<AudioStreamPlayer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.audio2)
		{
			audio2 = VariantUtils.ConvertTo<AudioStreamPlayer>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.audio3)
		{
			audio3 = VariantUtils.ConvertTo<AudioStreamPlayer>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.audio1)
		{
			value = VariantUtils.CreateFrom<AudioStreamPlayer>(ref audio1);
			return true;
		}
		if ((ref name) == PropertyName.audio2)
		{
			value = VariantUtils.CreateFrom<AudioStreamPlayer>(ref audio2);
			return true;
		}
		if ((ref name) == PropertyName.audio3)
		{
			value = VariantUtils.CreateFrom<AudioStreamPlayer>(ref audio3);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		List<PropertyInfo> list = new List<PropertyInfo>();
		list.Add(new PropertyInfo((Type)24, PropertyName.audio1, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)24, PropertyName.audio2, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		list.Add(new PropertyInfo((Type)24, PropertyName.audio3, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
		return list;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName.audio1, Variant.From<AudioStreamPlayer>(ref audio1));
		info.AddProperty(PropertyName.audio2, Variant.From<AudioStreamPlayer>(ref audio2));
		info.AddProperty(PropertyName.audio3, Variant.From<AudioStreamPlayer>(ref audio3));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.audio1, ref val))
		{
			audio1 = ((Variant)(ref val)).As<AudioStreamPlayer>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.audio2, ref val2))
		{
			audio2 = ((Variant)(ref val2)).As<AudioStreamPlayer>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.audio3, ref val3))
		{
			audio3 = ((Variant)(ref val3)).As<AudioStreamPlayer>();
		}
	}
}
namespace GodotPlugins.Game
{
	internal static class Main
	{
		[UnmanagedCallersOnly(EntryPoint = "godotsharp_game_main_init")]
		private static godot_bool InitializeFromGameProject(nint godotDllHandle, nint outManagedCallbacks, nint unmanagedCallbacks, int unmanagedCallbacksSize)
		{
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Expected O, but got Unknown
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				DllImportResolver resolver = new GodotDllImportResolver((IntPtr)godotDllHandle).OnResolveDllImport;
				Assembly assembly = typeof(GodotObject).Assembly;
				NativeLibrary.SetDllImportResolver(assembly, resolver);
				NativeFuncs.Initialize((IntPtr)unmanagedCallbacks, unmanagedCallbacksSize);
				ManagedCallbacks.Create((IntPtr)outManagedCallbacks);
				ScriptManagerBridge.LookupScriptsInAssembly(typeof(Main).Assembly);
				return (godot_bool)1;
			}
			catch (Exception value)
			{
				Console.Error.WriteLine(value);
				return GodotBoolExtensions.ToGodotBool(false);
			}
		}
	}
}
namespace wuwancients.Config
{
	public sealed class WuwancientsConfig : SimpleModConfig
	{
		[ConfigHideInUI]
		public static bool 守岸人是否出现 { get; set; } = true;


		[ConfigHideInUI]
		public static bool 强制守岸人出现 { get; set; } = false;


		[ConfigSection("先古出现设置")]
		public static AncientAppearMode 守岸人出现方式
		{
			get
			{
				return ToMode(守岸人是否出现, 强制守岸人出现);
			}
			set
			{
				FromMode(value, delegate(bool v)
				{
					守岸人是否出现 = v;
				}, delegate(bool v)
				{
					强制守岸人出现 = v;
				});
			}
		}

		[ConfigHideInUI]
		public static bool 菲比是否出现 { get; set; } = true;


		[ConfigHideInUI]
		public static bool 强制菲比出现 { get; set; } = false;


		public static AncientAppearMode 菲比出现方式
		{
			get
			{
				return ToMode(菲比是否出现, 强制菲比出现);
			}
			set
			{
				FromMode(value, delegate(bool v)
				{
					菲比是否出现 = v;
				}, delegate(bool v)
				{
					强制菲比出现 = v;
				});
			}
		}

		[ConfigHideInUI]
		public static bool 尤诺是否出现 { get; set; } = true;


		[ConfigHideInUI]
		public static bool 强制尤诺出现 { get; set; } = false;


		public static AncientAppearMode 尤诺出现方式
		{
			get
			{
				return ToMode(尤诺是否出现, 强制尤诺出现);
			}
			set
			{
				FromMode(value, delegate(bool v)
				{
					尤诺是否出现 = v;
				}, delegate(bool v)
				{
					强制尤诺出现 = v;
				});
			}
		}

		[ConfigHideInUI]
		public static bool 嘉贝莉娜是否出现 { get; set; } = true;


		[ConfigHideInUI]
		public static bool 强制嘉贝莉娜出现 { get; set; } = false;


		public static AncientAppearMode 嘉贝莉娜出现方式
		{
			get
			{
				return ToMode(嘉贝莉娜是否出现, 强制嘉贝莉娜出现);
			}
			set
			{
				FromMode(value, delegate(bool v)
				{
					嘉贝莉娜是否出现 = v;
				}, delegate(bool v)
				{
					强制嘉贝莉娜出现 = v;
				});
			}
		}

		[ConfigHideInUI]
		public static bool 弗洛洛是否出现 { get; set; } = true;


		[ConfigHideInUI]
		public static bool 强制弗洛洛出现 { get; set; } = false;


		public static AncientAppearMode 弗洛洛出现方式
		{
			get
			{
				return ToMode(弗洛洛是否出现, 强制弗洛洛出现);
			}
			set
			{
				FromMode(value, delegate(bool v)
				{
					弗洛洛是否出现 = v;
				}, delegate(bool v)
				{
					强制弗洛洛出现 = v;
				});
			}
		}

		[ConfigHideInUI]
		public static bool 千咲是否出现 { get; set; } = true;


		[ConfigHideInUI]
		public static bool 强制千咲出现 { get; set; } = false;


		public static AncientAppearMode 千咲出现方式
		{
			get
			{
				return ToMode(千咲是否出现, 强制千咲出现);
			}
			set
			{
				FromMode(value, delegate(bool v)
				{
					千咲是否出现 = v;
				}, delegate(bool v)
				{
					强制千咲出现 = v;
				});
			}
		}

		[ConfigHideInUI]
		public static bool 奥古斯塔是否出现 { get; set; } = true;


		[ConfigHideInUI]
		public static bool 强制奥古斯塔出现 { get; set; } = false;


		public static AncientAppearMode 奥古斯塔出现方式
		{
			get
			{
				return ToMode(奥古斯塔是否出现, 强制奥古斯塔出现);
			}
			set
			{
				FromMode(value, delegate(bool v)
				{
					奥古斯塔是否出现 = v;
				}, delegate(bool v)
				{
					强制奥古斯塔出现 = v;
				});
			}
		}

		[ConfigHideInUI]
		public static bool 琳奈是否出现 { get; set; } = true;


		[ConfigHideInUI]
		public static bool 强制琳奈出现 { get; set; } = false;


		public static AncientAppearMode 琳奈出现方式
		{
			get
			{
				return ToMode(琳奈是否出现, 强制琳奈出现);
			}
			set
			{
				FromMode(value, delegate(bool v)
				{
					琳奈是否出现 = v;
				}, delegate(bool v)
				{
					强制琳奈出现 = v;
				});
			}
		}

		[ConfigHideInUI]
		public static bool 赞妮是否出现 { get; set; } = true;


		[ConfigHideInUI]
		public static bool 强制赞妮出现 { get; set; } = false;


		public static AncientAppearMode 赞妮出现方式
		{
			get
			{
				return ToMode(赞妮是否出现, 强制赞妮出现);
			}
			set
			{
				FromMode(value, delegate(bool v)
				{
					赞妮是否出现 = v;
				}, delegate(bool v)
				{
					强制赞妮出现 = v;
				});
			}
		}

		public static bool 禁用原版先古 { get; set; } = false;


		[ConfigSection("卡牌和遗物的平衡性设置")]
		public static bool 血誓可附魔能力牌 { get; set; } = false;


		public static bool 见月花首回合抽牌全免费 { get; set; } = true;


		public static bool 溅染转移牌面数字 { get; set; } = false;


		public static bool 业火每三次伤害叠加一层 { get; set; } = true;


		[ConfigSection("事件配置")]
		public static bool AllEventsEnabled { get; set; } = true;


		public static EventAppearMode 江湖梦出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 莫塔里之忿出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 花女的礼物出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 这里好香出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 生日快乐出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 山洞中的香气出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 数据流出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 好闺蜜出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 律绘之诗出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 呜呜物流出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 漂泊出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 盗观梦出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 狩猎出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 微醺出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 照片中的我们出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 别回头出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 一刻闲暇出现方式 { get; set; } = EventAppearMode.随机;


		public static EventAppearMode 偏移的星图出现方式 { get; set; } = EventAppearMode.随机;


		[ConfigSection("先古场景")]
		public static AncientSceneMode 尤诺场景 { get; set; } = AncientSceneMode.随机出现;


		public static AncientSceneMode 嘉贝莉娜场景 { get; set; } = AncientSceneMode.随机出现;


		public static AncientSceneMode 弗洛洛场景 { get; set; } = AncientSceneMode.随机出现;


		public static AncientSceneMode 赞妮场景 { get; set; } = AncientSceneMode.随机出现;


		public static AncientSingleSceneMode 守岸人场景 { get; set; } = AncientSingleSceneMode.随机出现;


		public static AncientSingleSceneMode 菲比场景 { get; set; } = AncientSingleSceneMode.随机出现;


		public static AncientSingleSceneMode 千咲场景 { get; set; } = AncientSingleSceneMode.随机出现;


		public static AncientSingleSceneMode 奥古斯塔场景 { get; set; } = AncientSingleSceneMode.随机出现;


		public static AncientSingleSceneMode 琳奈场景 { get; set; } = AncientSingleSceneMode.随机出现;


		private static AncientAppearMode ToMode(bool appears, bool forced)
		{
			return appears ? ((!forced) ? AncientAppearMode.开启 : AncientAppearMode.强制出现) : AncientAppearMode.关闭;
		}

		private static void FromMode(AncientAppearMode mode, Action<bool> setAppears, Action<bool> setForced)
		{
			setAppears(mode != AncientAppearMode.关闭);
			setForced(mode == AncientAppearMode.强制出现);
		}
	}
	public enum AncientSceneMode
	{
		场景1,
		场景2,
		随机出现
	}
	public enum AncientSingleSceneMode
	{
		场景1,
		随机出现
	}
	public enum AncientAppearMode
	{
		关闭,
		开启,
		强制出现
	}
	public enum EventAppearMode
	{
		关闭,
		随机
	}
}
namespace wuwancients.Scripts
{
	internal static class BgmDucker
	{
		private static readonly List<object> Holders = new List<object>();

		internal static void Acquire(object holder)
		{
			if (Holders.Contains(holder))
			{
				return;
			}
			Holders.Add(holder);
			if (Holders.Count == 1)
			{
				NAudioManager instance = NAudioManager.Instance;
				if (instance != null)
				{
					instance.SetBgmVol(0f);
				}
			}
		}

		internal static void Release(object holder)
		{
			if (Holders.Remove(holder) && Holders.Count == 0)
			{
				Restore();
			}
		}

		internal static void ReleaseAll()
		{
			if (Holders.Count != 0)
			{
				Holders.Clear();
				Restore();
			}
		}

		private static void Restore()
		{
			SaveManager instance = SaveManager.Instance;
			float bgmVol = ((instance != null) ? instance.SettingsSave.VolumeBgm : 0.5f);
			NAudioManager instance2 = NAudioManager.Instance;
			if (instance2 != null)
			{
				instance2.SetBgmVol(bgmVol);
			}
		}
	}
	public static class OverlimitKillCredit
	{
		private static Player? _owner;

		private static ModelId? _target;

		private static bool _armed;

		public static void Arm(Player? owner, Creature? target)
		{
			_owner = owner;
			_target = ((target != null) ? target.ModelId : null);
			_armed = owner != null && target != null;
		}

		public static bool Consume(Player owner, Creature creature)
		{
			if (!_armed || _target == (ModelId)null)
			{
				return false;
			}
			if (_owner != owner)
			{
				return false;
			}
			if (!_target.Equals(creature.ModelId))
			{
				return false;
			}
			_armed = false;
			_target = null;
			return true;
		}

		public static void Clear()
		{
			_armed = false;
			_target = null;
			_owner = null;
		}
	}
	public static class DemonForceRuntime
	{
		public static void AnnounceUnlock(Player owner, DemonForce force)
		{
			Log.Info($"[wuwancients] demon force unlocked: {force.Name} ({force.Id})", 2);
			DemonForceToast.Show(owner, force);
		}
	}
	public static class DemonForceToast
	{
		private const float HoldSeconds = 2.6f;

		private const float FadeSeconds = 0.8f;

		public static void Show(Player owner, DemonForce force)
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Expected O, but got Unknown
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0152: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			string text = "获得" + force.Name + "之力";
			try
			{
				MainLoop mainLoop = Engine.GetMainLoop();
				SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
				if (val != null && val.Root != null)
				{
					Label val2 = new Label
					{
						Text = text,
						HorizontalAlignment = (HorizontalAlignment)1,
						VerticalAlignment = (VerticalAlignment)1,
						MouseFilter = (MouseFilterEnum)2
					};
					((Control)val2).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 30);
					((Control)val2).AddThemeColorOverride(StringName.op_Implicit("font_color"), new Color(1f, 0.84f, 0.25f, 1f));
					((Control)val2).AddThemeColorOverride(StringName.op_Implicit("font_outline_color"), new Color(0f, 0f, 0f, 1f));
					((Control)val2).AddThemeConstantOverride(StringName.op_Implicit("outline_size"), 6);
					((Node)val.Root).AddChild((Node)(object)val2, false, (InternalMode)0);
					((Control)val2).SetAnchorsPreset((LayoutPreset)10, false);
					((Control)val2).OffsetTop = 150f;
					((Control)val2).OffsetBottom = 190f;
					((Control)val2).OffsetLeft = 0f;
					((Control)val2).OffsetRight = 0f;
					Tween val3 = ((Node)val2).CreateTween();
					val3.TweenInterval(2.5999999046325684);
					val3.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(0f), 0.800000011920929);
					val3.TweenCallback(Callable.From((Action)((Node)val2).QueueFree));
				}
			}
			catch (Exception ex)
			{
				Log.Info("[wuwancients] demon force toast failed: " + ex.Message, 2);
			}
		}
	}
	public static class EnchantUtil
	{
		public static List<EnchantmentModel> BuildPool(CardModel card)
		{
			CardModel card2 = card;
			bool alreadyEnchanted = card2.Enchantment != null;
			return (from e in ModelDb.DebugEnchantments.Where((EnchantmentModel e) => !(e is DeprecatedEnchantment)).Where(delegate(EnchantmentModel e)
				{
					string? @namespace = ((object)e).GetType().Namespace;
					return @namespace == null || !@namespace.Contains("Mocks", StringComparison.Ordinal);
				})
				where alreadyEnchanted ? e.CanEnchantCardType(card2.Type) : e.CanEnchant(card2)
				select e).ToList();
		}

		public static void Apply(CardModel card, EnchantmentModel canonical)
		{
			EnchantmentModel enchantment = card.Enchantment;
			if (enchantment != null)
			{
				if (((object)enchantment).GetType() == ((object)canonical).GetType())
				{
					enchantment.Amount += 1;
					return;
				}
				CardCmd.ClearEnchantment(card);
			}
			EnchantmentModel val = canonical.ToMutable();
			card.EnchantInternal(val, 1m);
			val.ModifyCard();
		}

		public static void ApplyToDeck(CardModel card, EnchantmentModel canonical)
		{
			Apply(card, canonical);
			CardModel deckVersion = card.DeckVersion;
			if (deckVersion != null && deckVersion != card)
			{
				Apply(deckVersion, canonical);
			}
		}

		public static bool ApplyRandom(CardModel card, Rng? rng)
		{
			List<EnchantmentModel> list = BuildPool(card);
			if (list.Count == 0)
			{
				return false;
			}
			int index = SyncedRng.Index(rng, list.Count);
			EnchantmentModel canonical = list[index];
			ApplyToDeck(card, canonical);
			return true;
		}

		public static int NumericTotal(CardModel card)
		{
			int num = 0;
			foreach (DynamicVar value in card.DynamicVars.Values)
			{
				if (IsBumpable(value))
				{
					num += (int)value.BaseValue;
				}
			}
			return num;
		}

		public static void AddNumericVarsToDeck(CardModel card, int amount)
		{
			AddNumericVars(card, amount);
			CardModel deckVersion = card.DeckVersion;
			if (deckVersion != null && deckVersion != card)
			{
				AddNumericVars(deckVersion, amount);
			}
		}

		public static void AddDamage(CardModel card, int amount)
		{
			DynamicVar val = default(DynamicVar);
			if (amount != 0 && card.DynamicVars.TryGetValue("Damage", ref val) && val != null)
			{
				DynamicVar obj = val;
				obj.BaseValue += (decimal)amount;
				card.DynamicVars.RecalculateForUpgradeOrEnchant();
			}
		}

		public static void AddNumericVars(CardModel card, int amount)
		{
			if (amount == 0)
			{
				return;
			}
			foreach (DynamicVar item in card.DynamicVars.Values.ToList())
			{
				if (IsBumpable(item))
				{
					item.BaseValue += (decimal)amount;
				}
			}
			card.DynamicVars.RecalculateForUpgradeOrEnchant();
		}

		private static bool IsBumpable(DynamicVar variable)
		{
			return !(variable is EnergyVar) && !(variable is StringVar) && !(variable is BoolVar) && !(variable is IfUpgradedVar) && !(variable is CalculatedVar);
		}
	}
	[ModInitializer("Init")]
	public class Entry
	{
		private static Harmony? _harmony;

		public static void Init()
		{
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Expected O, but got Unknown
			ModConfigRegistry.Register("wuwancients", (ModConfig)(object)new WuwancientsConfig());
			Console.WriteLine("Entry.Init started");
			_harmony = new Harmony("WUWANCIENTS");
			_harmony.PatchAll();
			FlameLightCostLabelPatch.Apply(_harmony);
			FlameLightDescriptionPatch.Apply(_harmony);
			ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
			ModHelper.AddModelToPool<EventCardPool, PredestinedDeath>();
			ModHelper.AddModelToPool<EventCardPool, IntelligentCreation>();
			ModHelper.AddModelToPool<EventCardPool, FuryUnleashed>();
			ModHelper.AddModelToPool<EventCardPool, Wormhole>();
			ModHelper.AddModelToPool<EventCardPool, ForeseeFuture>();
			ModHelper.AddModelToPool<EventCardPool, Revelation1>();
			ModHelper.AddModelToPool<CurseCardPool, Bloom>();
			ModHelper.AddModelToPool<CurseCardPool, Fuxie>();
			ModHelper.AddModelToPool<EventCardPool, OneDayFlower>();
			ModHelper.AddModelToPool<EventCardPool, JieXian>();
			ModHelper.AddModelToPool<EventCardPool, MoonPhaseFlow>();
			ModHelper.AddModelToPool<EventCardPool, SolsticeCrusade>();
			ModHelper.AddModelToPool<EventCardPool, BlazingSun>();
			ModHelper.AddModelToPool<EventCardPool, ImmortalPurge>();
			ModHelper.AddModelToPool<EventCardPool, StarDomain>();
			ModHelper.AddModelToPool<EventCardPool, UnderTheSea>();
			ModHelper.AddModelToPool<EventCardPool, NewWaveEra>();
			ModHelper.AddModelToPool<EventCardPool, YiZhanZuYi>();
			ModHelper.AddModelToPool<EventCardPool, CurtainsEnd>();
			ModHelper.AddModelToPool<EventCardPool, CurtainsEndIllusion>();
			ModHelper.AddModelToPool<EventCardPool, CollectData>();
			ModHelper.AddModelToPool<EventCardPool, MassEnergyEquivalence>();
			ModHelper.AddModelToPool<EventCardPool, RunePower>();
			ModHelper.AddModelToPool<EventCardPool, CriticalProtocol>();
			ModHelper.AddModelToPool<EventCardPool, TripleBrilliance>();
			ModHelper.AddModelToPool<EventCardPool, Overlimit>();
			ModHelper.AddModelToPool<EventCardPool, ExplosiveSprayPaint>();
			ModHelper.AddModelToPool<EventCardPool, Kuangsha>();
			ModHelper.AddModelToPool<EventCardPool, Resignation>();
			ModHelper.AddModelToPool<EventCardPool, Symbiosis>();
			ModHelper.AddModelToPool<EventCardPool, Endless>();
			ModHelper.AddModelToPool<EventCardPool, RoyalLove>();
			ModHelper.AddModelToPool<EventCardPool, NeuralNetwork>();
			ModHelper.AddModelToPool<EventCardPool, CanRen>();
			ModHelper.AddModelToPool<EventCardPool, MassGrave>();
			ModHelper.AddModelToPool<EventCardPool, Hunt>();
			ModHelper.AddModelToPool<EventCardPool, BiShi>();
			ModHelper.AddModelToPool<EventCardPool, BiAn>();
			ModHelper.AddModelToPool<EventCardPool, Overtime>();
			ModHelper.AddModelToPool<EventCardPool, TimeOff>();
			ModHelper.AddModelToPool<EventCardPool, SuisuiGold>();
			ModHelper.AddModelToPool<TokenCardPool, DeathWarrant>();
			ModHelper.AddModelToPool<SharedRelicPool, PromiseBaton>();
			ModHelper.AddModelToPool<SharedRelicPool, ScatteredLycoris>();
			ModHelper.AddModelToPool<SharedRelicPool, FallingEcho>();
			ModHelper.AddModelToPool<SharedRelicPool, TearStainedBandage>();
			ModHelper.AddModelToPool<SharedRelicPool, HecatesPhantom>();
			ModHelper.AddModelToPool<SharedRelicPool, DeathAndLifeMovement>();
			ModHelper.AddModelToPool<SharedRelicPool, NewWorldCarnival>();
			ModHelper.AddModelToPool<SharedRelicPool, RedCurrantTart>();
			ModHelper.AddModelToPool<SharedRelicPool, UnfinishedSymphony>();
			ModHelper.AddModelToPool<EventRelicPool, UnstableTunnel>();
			ModHelper.AddModelToPool<EventRelicPool, NuoNuoChaoFan>();
			ModHelper.AddModelToPool<EventRelicPool, Taiji>();
			ModHelper.AddModelToPool<EventRelicPool, OldPhotoAlbum>();
			ModHelper.AddModelToPool<EventRelicPool, StarChart>();
			ModHelper.AddModelToPool<EventRelicPool, EglaTributeWine>();
			ModHelper.AddModelToPool<EventRelicPool, WinePot>();
			ModHelper.AddModelToPool<EventRelicPool, SisterLetter>();
			ModHelper.AddModelToPool<EventRelicPool, PeaceAndProsperity>();
			ModHelper.AddModelToPool<EventRelicPool, DeadBranch>();
			ModHelper.AddModelToPool<EventRelicPool, Sundial>();
			ModHelper.AddModelToPool<EventRelicPool, InsectSpecimen>();
			ModHelper.AddModelToPool<EventRelicPool, OuterCalipers>();
			ModHelper.AddModelToPool<EventRelicPool, ToyOrnithopter>();
			ModHelper.AddModelToPool<EventRelicPool, InkBottle>();
			ModHelper.AddModelToPool<EventRelicPool, StrangeSpoon>();
			ModHelper.AddModelToPool<SharedRelicPool, FulouluoDumpling>();
			ModHelper.AddModelToPool<SharedRelicPool, Gospel>();
			ModHelper.AddModelToPool<SharedRelicPool, GoldenGrace>();
			ModHelper.AddModelToPool<SharedRelicPool, MonasticHat>();
			ModHelper.AddModelToPool<SharedRelicPool, PhoebeChobi>();
			ModHelper.AddModelToPool<SharedRelicPool, HiddenSeaRecord>();
			ModHelper.AddModelToPool<SharedRelicPool, PurifyingConch>();
			ModHelper.AddModelToPool<SharedRelicPool, PizzaSlice>();
			ModHelper.AddModelToPool<SharedRelicPool, HeGuangTongChang>();
			ModHelper.AddModelToPool<IroncladRelicPool, BrightBlood>();
			ModHelper.AddModelToPool<DefectRelicPool, FrozenCore>();
			ModHelper.AddModelToPool<NecrobinderRelicPool, LostPhylactery>();
			ModHelper.AddModelToPool<RegentRelicPool, HeavenlyMandate>();
			ModHelper.AddModelToPool<SilentRelicPool, LongSnakeNecklace>();
			ModHelper.AddModelToPool<SharedRelicPool, SnowStew>();
			ModHelper.AddModelToPool<SharedRelicPool, GiftOfQiqiu>();
			ModHelper.AddModelToPool<SharedRelicPool, MottaliBankCard>();
			ModHelper.AddModelToPool<SharedRelicPool, ZannisSalaryCard>();
			ModHelper.AddModelToPool<SharedRelicPool, EchoAbsorptionDevice>();
			ModHelper.AddModelToPool<SharedRelicPool, GoatBaaGreeting>();
			ModHelper.AddModelToPool<SharedRelicPool, ButterflyPrint>();
			ModHelper.AddModelToPool<SharedRelicPool, Texture1>();
			ModHelper.AddModelToPool<SharedRelicPool, YokuuTopology>();
			ModHelper.AddModelToPool<SharedRelicPool, FibiDumpling>();
			ModHelper.AddModelToPool<SharedRelicPool, WhiteRibbon>();
			ModHelper.AddModelToPool<SharedRelicPool, BurySpiritGreeting>();
			ModHelper.AddModelToPool<SharedRelicPool, AutumnWaterGreeting>();
			ModHelper.AddModelToPool<SharedRelicPool, We>();
			ModHelper.AddModelToPool<SharedRelicPool, MissetFallacy>();
			ModHelper.AddModelToPool<SharedRelicPool, SobUnderWail>();
			ModHelper.AddModelToPool<SharedRelicPool, ShorekeeperDango>();
			ModHelper.AddModelToPool<SharedRelicPool, StarSequenceHarmony>();
			ModHelper.AddModelToPool<SharedRelicPool, EndlessLoop>();
			ModHelper.AddModelToPool<SharedRelicPool, TetisFlower>();
			ModHelper.AddModelToPool<SharedRelicPool, FireDevilGreeting>();
			ModHelper.AddModelToPool<SharedRelicPool, CamelliaGreeting>();
			ModHelper.AddModelToPool<SharedRelicPool, FeisaliesGift>();
			ModHelper.AddModelToPool<SharedRelicPool, MottaliGift>();
			ModHelper.AddModelToPool<SharedRelicPool, YingBaiLadosOldRelic>();
			ModHelper.AddModelToPool<SharedRelicPool, HellhoundGreeting>();
			ModHelper.AddModelToPool<SharedRelicPool, Prayer>();
			ModHelper.AddModelToPool<SharedRelicPool, BoZaiDoll>();
			ModHelper.AddModelToPool<SharedRelicPool, OldScissors>();
			ModHelper.AddModelToPool<SharedRelicPool, Knot>();
			ModHelper.AddModelToPool<SharedRelicPool, LongSummerFlower>();
			ModHelper.AddModelToPool<SharedRelicPool, ResonanceSuppressionCollar>();
			ModHelper.AddModelToPool<SharedRelicPool, RedHairband>();
			ModHelper.AddModelToPool<SharedRelicPool, SurveyLog>();
			ModHelper.AddModelToPool<SharedRelicPool, Latte>();
			ModHelper.AddModelToPool<SharedRelicPool, FragrantLemonShabuShabu>();
			ModHelper.AddModelToPool<SharedRelicPool, ChisaDango>();
			ModHelper.AddModelToPool<SharedRelicPool, SweetPickledOlive>();
			ModHelper.AddModelToPool<SharedRelicPool, MoonviewFlower>();
			ModHelper.AddModelToPool<SharedRelicPool, BrokenRebirth>();
			ModHelper.AddModelToPool<SharedRelicPool, IfMeasuredInAnInstant>();
			ModHelper.AddModelToPool<SharedRelicPool, LightlyTossedThoughts>();
			ModHelper.AddModelToPool<SharedRelicPool, YounoDumpling>();
			ModHelper.AddModelToPool<SharedRelicPool, AnnotationsOfEternalPreservation>();
			ModHelper.AddModelToPool<SharedRelicPool, MoonstoneBracelet>();
			ModHelper.AddModelToPool<SharedRelicPool, DirectionalAnchorFragment>();
			ModHelper.AddModelToPool<SharedRelicPool, WaxAndWane>();
			ModHelper.AddModelToPool<SharedRelicPool, SunAndGriffinSeal>();
			ModHelper.AddModelToPool<SharedRelicPool, SundayCrown>();
			ModHelper.AddModelToPool<SharedRelicPool, ResidualFrequencyOfBlackTide>();
			ModHelper.AddModelToPool<SharedRelicPool, AuthorityOfThunderAndCrown>();
			ModHelper.AddModelToPool<SharedRelicPool, SmeltedFragment>();
			ModHelper.AddModelToPool<SharedRelicPool, GlowingMarigold>();
			ModHelper.AddModelToPool<SharedRelicPool, SmallAcorn>();
			ModHelper.AddModelToPool<SharedRelicPool, SweetLeafSplitBread>();
			ModHelper.AddModelToPool<SharedRelicPool, OldHairband>();
			ModHelper.AddModelToPool<SharedRelicPool, AugustaDumpling>();
			ModHelper.AddModelToPool<SharedRelicPool, MemoryStarAnchor>();
			ModHelper.AddModelToPool<SharedRelicPool, JianxinGreeting>();
			ModHelper.AddModelToPool<SharedRelicPool, LinaGreeting>();
			ModHelper.AddModelToPool<SharedRelicPool, SproutGreeting>();
			ModHelper.AddModelToPool<SharedRelicPool, LingYinGreeting>();
			ModHelper.AddModelToPool<SharedRelicPool, PeaceCharm>();
			ModHelper.AddModelToPool<SharedRelicPool, PaperRole>();
			ModHelper.AddModelToPool<SharedRelicPool, Decode>();
			ModHelper.AddModelToPool<SharedRelicPool, OldMemories>();
			ModHelper.AddModelToPool<SharedRelicPool, MiniTone>();
			ModHelper.AddModelToPool<SharedRelicPool, GalbrenasKarmaFire>();
			ModHelper.AddModelToPool<SharedRelicPool, HarpyienSharpFeather>();
			ModHelper.AddModelToPool<SharedRelicPool, DullahansFlesh>();
			ModHelper.AddModelToPool<SharedRelicPool, BalorsEye>();
			ModHelper.AddModelToPool<SharedRelicPool, NamelessShadowHusk>();
			ModHelper.AddModelToPool<SharedRelicPool, SweetDreams>();
			ModHelper.AddModelToPool<SharedRelicPool, ExtraThickMilkshakeShavedIce>();
			ModHelper.AddModelToPool<SharedRelicPool, StoneRose>();
			ModHelper.AddModelToPool<SharedRelicPool, ZheyingWancai>();
			ModHelper.AddModelToPool<SharedRelicPool, FirstBloodOath>();
			ModHelper.AddModelToPool<SharedRelicPool, ChimerasHeart>();
			ModHelper.AddModelToPool<SharedRelicPool, SuisuiGreeting>();
			ModHelper.AddModelToPool<SharedRelicPool, CactusHugPillow>();
			ModHelper.AddModelToPool<SharedRelicPool, ColorfulCupNoodles>();
			ModHelper.AddModelToPool<SharedRelicPool, LinnaDango>();
			ModHelper.AddModelToPool<SharedRelicPool, FrostSignalFlower>();
			ModHelper.AddModelToPool<SharedRelicPool, RadiantGlow>();
			ModHelper.AddModelToPool<SharedRelicPool, ExpeditionKey>();
			ModHelper.AddModelToPool<SharedRelicPool, PaintCan>();
			ModHelper.AddModelToPool<SharedRelicPool, DingDongPendant>();
			ModHelper.AddModelToPool<SharedRelicPool, ViolationPartsSet>();
			ModHelper.AddModelToPool<SharedRelicPool, PersonalMemo>();
			ModHelper.AddModelToPool<SharedRelicPool, TimeManagementMaster>();
			ModHelper.AddModelToPool<SharedRelicPool, ThankYouGift>();
			ModHelper.AddModelToPool<SharedRelicPool, AssortedMeatSauceNoodles>();
			ModHelper.AddModelToPool<SharedRelicPool, SwordCalamus>();
			ModHelper.AddModelToPool<SharedRelicPool, FlameJudgment>();
			ModHelper.AddModelToPool<SharedRelicPool, DemonTax>();
			ModHelper.AddModelToPool<SharedRelicPool, FlameClaw>();
			ModHelper.AddModelToPool<SharedRelicPool, ZaniDango>();
			ModHelper.AddModelToPool<SharedRelicPool, OxHorsePlushie>();
			ModHelper.AddModelToPool<SharedPotionPool, HeroKingsBlood>();
			ModHelper.AddModelToPool<SharedPotionPool, ChongZhouMalaiDouFu>();
			ModHelper.AddModelToPool<SharedPotionPool, DazzlingSweetness>();
			Log.Info("Mod initialized!", 2);
		}
	}
	[ScriptPath("res://Scripts/NSuisuiMysteryShop.cs")]
	public class NSuisuiMysteryShop : Control, ICustomEventNode, IScreenContext
	{
		public class MethodName : MethodName
		{
			public static readonly StringName _EnterTree = StringName.op_Implicit("_EnterTree");

			public static readonly StringName _ExitTree = StringName.op_Implicit("_ExitTree");

			public static readonly StringName OnActiveScreenUpdated = StringName.op_Implicit("OnActiveScreenUpdated");

			public static readonly StringName _Process = StringName.op_Implicit("_Process");

			public static readonly StringName BuildBackground = StringName.op_Implicit("BuildBackground");

			public static readonly StringName BuildMerchant = StringName.op_Implicit("BuildMerchant");

			public static readonly StringName BuildInventory = StringName.op_Implicit("BuildInventory");

			public static readonly StringName InitInventory = StringName.op_Implicit("InitInventory");

			public static readonly StringName BuildProceedButton = StringName.op_Implicit("BuildProceedButton");

			public static readonly StringName BuildFallbackProceedButton = StringName.op_Implicit("BuildFallbackProceedButton");

			public static readonly StringName InitProceedButton = StringName.op_Implicit("InitProceedButton");

			public static readonly StringName BuildCharacters = StringName.op_Implicit("BuildCharacters");

			public static readonly StringName LayoutCharactersWhenReady = StringName.op_Implicit("LayoutCharactersWhenReady");

			public static readonly StringName PlayIdle = StringName.op_Implicit("PlayIdle");

			public static readonly StringName OnMerchantInput = StringName.op_Implicit("OnMerchantInput");

			public static readonly StringName OnInventoryClosed = StringName.op_Implicit("OnInventoryClosed");

			public static readonly StringName OnProceedReleased = StringName.op_Implicit("OnProceedReleased");

			public static readonly StringName FinishEvent = StringName.op_Implicit("FinishEvent");
		}

		public class PropertyName : PropertyName
		{
			public static readonly StringName DefaultFocusedControl = StringName.op_Implicit("DefaultFocusedControl");

			public static readonly StringName _inventory = StringName.op_Implicit("_inventory");

			public static readonly StringName _proceedButton = StringName.op_Implicit("_proceedButton");

			public static readonly StringName _characterContainer = StringName.op_Implicit("_characterContainer");

			public static readonly StringName _charactersPending = StringName.op_Implicit("_charactersPending");

			public static readonly StringName _merchantArea = StringName.op_Implicit("_merchantArea");

			public static readonly StringName _merchantPortrait = StringName.op_Implicit("_merchantPortrait");

			public static readonly StringName _handAnchor = StringName.op_Implicit("_handAnchor");

			public static readonly StringName _hand = StringName.op_Implicit("_hand");

			public static readonly StringName _localPlayerDead = StringName.op_Implicit("_localPlayerDead");

			public static readonly StringName _greetPending = StringName.op_Implicit("_greetPending");

			public static readonly StringName _finished = StringName.op_Implicit("_finished");
		}

		public class SignalName : SignalName
		{
		}

		private const string BackgroundPath = "res://wuwancients/images/events/suisui_shop_bg.png";

		private const string MerchantPath = "res://wuwancients/images/events/suisui_shop_merchant.png";

		private const string HandPath = "res://wuwancients/images/events/suisui_shop_hand.png";

		private const string InventoryScenePath = "res://scenes/events/custom/fake_merchant_inventory.tscn";

		private const string ProceedScenePath = "res://scenes/ui/proceed_button.tscn";

		private static readonly Vector2 MerchantOffsetMin = new Vector2(204f, -179f);

		private static readonly Vector2 MerchantOffsetMax = new Vector2(474f, 258f);

		private static readonly Vector2 MerchantArtPosition = new Vector2(139f, 400f);

		private const float MerchantArtScale = 0.470095f;

		private const double LineSeconds = 4.0;

		private readonly List<Player> _players = new List<Player>();

		private SuisuiMysteryShop? _event;

		private MerchantDialogueSet? _dialogue;

		private NMerchantInventory? _inventory;

		private NProceedButton? _proceedButton;

		private readonly List<NCreatureVisuals> _characterVisuals = new List<NCreatureVisuals>();

		private readonly List<int> _characterRows = new List<int>();

		private Control? _characterContainer;

		private bool _charactersPending;

		private Control? _merchantArea;

		private Sprite2D? _merchantPortrait;

		private Node2D? _handAnchor;

		private Sprite2D? _hand;

		private bool _localPlayerDead;

		private bool _greetPending;

		private bool _finished;

		public IScreenContext CurrentScreenContext
		{
			get
			{
				NMerchantInventory inventory = _inventory;
				IScreenContext result;
				if (inventory == null || !inventory.IsOpen)
				{
					IScreenContext val = (IScreenContext)(object)this;
					result = val;
				}
				else
				{
					IScreenContext val = (IScreenContext)(object)inventory;
					result = val;
				}
				return result;
			}
		}

		public Control? DefaultFocusedControl => null;

		public void Initialize(EventModel eventModel)
		{
			_event = eventModel as SuisuiMysteryShop;
			if (_event?.Inventory != null && ((EventModel)_event).Owner != null)
			{
				_players.AddRange(((IPlayerCollection)((EventModel)_event).Owner.RunState).Players);
				_dialogue = LoadDialogue();
				Player me = LocalContext.GetMe((IEnumerable<Player>)_players);
				bool? obj;
				if (me == null)
				{
					obj = null;
				}
				else
				{
					Creature creature = me.Creature;
					obj = ((creature != null) ? new bool?(creature.IsDead) : null);
				}
				bool? flag = obj;
				_localPlayerDead = flag.GetValueOrDefault();
				((Control)this).SetAnchorsPreset((LayoutPreset)15, false);
				BuildBackground();
				BuildCharacters();
				BuildMerchant();
				BuildProceedButton();
				BuildInventory();
				_greetPending = true;
			}
		}

		public override void _EnterTree()
		{
			ActiveScreenContext.Instance.Updated += OnActiveScreenUpdated;
		}

		public override void _ExitTree()
		{
			ActiveScreenContext.Instance.Updated -= OnActiveScreenUpdated;
			SuisuiShopAudio.SuppressVanillaVoice = false;
		}

		private void OnActiveScreenUpdated()
		{
			if (_proceedButton != null && !_finished)
			{
				NMerchantInventory inventory = _inventory;
				if ((inventory == null || !inventory.IsOpen) && ActiveScreenContext.Instance.IsCurrent((IScreenContext)(object)this))
				{
					((NClickableControl)_proceedButton).Enable();
				}
			}
		}

		public override void _Process(double delta)
		{
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			if (_greetPending)
			{
				_greetPending = false;
				MerchantDialogueSet? dialogue = _dialogue;
				SayLine((dialogue != null) ? dialogue.WelcomeLines : null);
			}
			if (_charactersPending)
			{
				LayoutCharactersWhenReady();
			}
			if (_hand != null && _handAnchor != null && GodotObject.IsInstanceValid((GodotObject)(object)_handAnchor))
			{
				((Node2D)_hand).GlobalPosition = _handAnchor.GlobalPosition;
			}
		}

		private static MerchantDialogueSet LoadDialogue()
		{
			LocTable table = LocManager.Instance.GetTable("events");
			string text = StringHelper.Slugify("SuisuiMysteryShop") + ".talk.";
			return MerchantDialogueSet.CreateFromLocStrings((IEnumerable<LocString>)table.GetLocStringsWithPrefix(text));
		}

		private void BuildBackground()
		{
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Expected O, but got Unknown
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Expected O, but got Unknown
			if (((Node)this).GetNodeOrNull<Node2D>(NodePath.op_Implicit("Background/Art")) == null && ResourceLoader.Exists("res://wuwancients/images/events/suisui_shop_bg.png", ""))
			{
				Control val = new Control
				{
					MouseFilter = (MouseFilterEnum)2
				};
				val.SetAnchorsPreset((LayoutPreset)8, false);
				((Node)this).AddChild((Node)(object)val, false, (InternalMode)0);
				Sprite2D val2 = new Sprite2D
				{
					Texture = PreloadManager.Cache.GetTexture2D("res://wuwancients/images/events/suisui_shop_bg.png"),
					Centered = true,
					Position = Vector2.Zero,
					Scale = Vector2.One
				};
				((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
			}
		}

		private void BuildMerchant()
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Expected O, but got Unknown
			//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_0102: Unknown result type (might be due to invalid IL or missing references)
			//IL_010d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
			//IL_0123: Unknown result type (might be due to invalid IL or missing references)
			//IL_0130: Expected O, but got Unknown
			//IL_015f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0164: Unknown result type (might be due to invalid IL or missing references)
			//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
			_merchantArea = ((Node)this).GetNodeOrNull<Control>(NodePath.op_Implicit("MerchantArea"));
			if (_merchantArea == null)
			{
				_merchantArea = new Control();
				_merchantArea.SetAnchorsPreset((LayoutPreset)8, false);
				_merchantArea.OffsetLeft = MerchantOffsetMin.X;
				_merchantArea.OffsetTop = MerchantOffsetMin.Y;
				_merchantArea.OffsetRight = MerchantOffsetMax.X;
				_merchantArea.OffsetBottom = MerchantOffsetMax.Y;
				((Node)this).AddChild((Node)(object)_merchantArea, false, (InternalMode)0);
			}
			_merchantPortrait = ((Node)_merchantArea).GetNodeOrNull<Sprite2D>(NodePath.op_Implicit("Art"));
			if (_merchantPortrait == null && ResourceLoader.Exists("res://wuwancients/images/events/suisui_shop_merchant.png", ""))
			{
				_merchantPortrait = new Sprite2D
				{
					Texture = PreloadManager.Cache.GetTexture2D("res://wuwancients/images/events/suisui_shop_merchant.png"),
					Position = MerchantArtPosition,
					Scale = new Vector2(0.470095f, 0.470095f),
					Centered = true
				};
				((Node)_merchantArea).AddChild((Node)(object)_merchantPortrait, false, (InternalMode)0);
			}
			Control val = (Control)(((object)((Node)_merchantArea).GetNodeOrNull<Control>(NodePath.op_Implicit("Hit"))) ?? ((object)new Control
			{
				MouseFilter = (MouseFilterEnum)0
			}));
			if (((Node)val).GetParent() == null)
			{
				val.SetAnchorsPreset((LayoutPreset)15, false);
				((Node)_merchantArea).AddChild((Node)(object)val, false, (InternalMode)0);
			}
			((GodotObject)val).Connect(SignalName.GuiInput, Callable.From<InputEvent>((Action<InputEvent>)OnMerchantInput), 0u);
		}

		private void BuildInventory()
		{
			PackedScene obj = GD.Load<PackedScene>("res://scenes/events/custom/fake_merchant_inventory.tscn");
			Node obj2 = ((obj != null) ? obj.Instantiate((GenEditState)0) : null);
			NMerchantInventory val = (NMerchantInventory)(object)((obj2 is NMerchantInventory) ? obj2 : null);
			if (val != null)
			{
				_inventory = val;
				((Control)val).MouseFilter = (MouseFilterEnum)2;
				GodotTreeExtensions.AddChildSafely((Node)(object)this, (Node)(object)val);
				if (((Node)val).IsNodeReady())
				{
					InitInventory();
				}
				else
				{
					((Node)val).Ready += InitInventory;
				}
			}
		}

		private void InitInventory()
		{
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_010a: Expected O, but got Unknown
			if (_inventory != null && _event?.Inventory != null && _dialogue != null)
			{
				_inventory.Initialize(_event.Inventory, _dialogue);
				((GodotObject)_inventory).Connect(SignalName.InventoryClosed, Callable.From((Action)OnInventoryClosed), 0u);
				_handAnchor = ((Node)_inventory).GetNodeOrNull<Node2D>(NodePath.op_Implicit("MerchantHandContainer"));
				if (_handAnchor != null && ResourceLoader.Exists("res://wuwancients/images/events/suisui_shop_hand.png", ""))
				{
					((CanvasItem)_handAnchor).Visible = false;
					_hand = new Sprite2D
					{
						Texture = PreloadManager.Cache.GetTexture2D("res://wuwancients/images/events/suisui_shop_hand.png"),
						Scale = new Vector2(0.4f, 0.4f),
						Centered = true
					};
					GodotTreeExtensions.AddChildSafely((Node)(object)_inventory, (Node)(object)_hand);
				}
			}
		}

		private void BuildProceedButton()
		{
			PackedScene obj = GD.Load<PackedScene>("res://scenes/ui/proceed_button.tscn");
			Node obj2 = ((obj != null) ? obj.Instantiate((GenEditState)0) : null);
			NProceedButton val = (NProceedButton)(object)((obj2 is NProceedButton) ? obj2 : null);
			if (val == null)
			{
				GD.PushError("[wuwancients] 商店的前进键（原版 proceed_button.tscn）没挂上，已改用兜底按钮");
				BuildFallbackProceedButton();
				return;
			}
			_proceedButton = val;
			GodotTreeExtensions.AddChildSafely((Node)(object)this, (Node)(object)val);
			if (((Node)val).IsNodeReady())
			{
				InitProceedButton();
			}
			else
			{
				((Node)val).Ready += InitProceedButton;
			}
		}

		private void BuildFallbackProceedButton()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Expected O, but got Unknown
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			Button val = new Button
			{
				Text = NProceedButton.ProceedLoc.GetFormattedText(),
				CustomMinimumSize = new Vector2(220f, 64f)
			};
			((Control)val).SetAnchorsPreset((LayoutPreset)3, false);
			((Control)val).OffsetLeft = -320f;
			((Control)val).OffsetTop = -150f;
			((Control)val).OffsetRight = -100f;
			((Control)val).OffsetBottom = -86f;
			((GodotObject)val).Connect(SignalName.Pressed, Callable.From((Action)FinishEvent), 0u);
			GodotTreeExtensions.AddChildSafely((Node)(object)this, (Node)(object)val);
		}

		private void InitProceedButton()
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			if (_proceedButton == null)
			{
				return;
			}
			((GodotObject)_proceedButton).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)OnProceedReleased), 0u);
			_proceedButton.UpdateText(NProceedButton.ProceedLoc);
			_proceedButton.SetPulseState(false);
			((NClickableControl)_proceedButton).Enable();
			Callable val = Callable.From((Action)delegate
			{
				NProceedButton? proceedButton = _proceedButton;
				if (proceedButton != null)
				{
					((NClickableControl)proceedButton).Enable();
				}
			});
			((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		}

		private void BuildCharacters()
		{
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Expected O, but got Unknown
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
			if (_players.Count == 0)
			{
				return;
			}
			Player me = LocalContext.GetMe((IEnumerable<Player>)_players);
			if (me != null)
			{
				_players.Remove(me);
				_players.Insert(0, me);
			}
			_characterContainer = new Control
			{
				MouseFilter = (MouseFilterEnum)2
			};
			_characterContainer.SetAnchorsPreset((LayoutPreset)8, false);
			_characterContainer.OffsetLeft = -398f;
			_characterContainer.OffsetTop = 276f;
			_characterContainer.OffsetRight = -398f;
			_characterContainer.OffsetBottom = 276f;
			_characterContainer.Scale = new Vector2(1.75f, 1.75f);
			((Node)this).AddChild((Node)(object)_characterContainer, false, (InternalMode)0);
			((Node)this).MoveChild((Node)(object)_characterContainer, 1);
			int num = Mathf.CeilToInt(Mathf.Sqrt((float)_players.Count));
			for (int i = 0; i < num; i++)
			{
				for (int j = 0; j < num; j++)
				{
					int num2 = i * num + j;
					if (num2 >= _players.Count)
					{
						break;
					}
					Player val = _players[num2];
					if (((val != null) ? val.Character : null) != null)
					{
						NCreatureVisuals val2 = val.Character.CreateVisuals();
						GodotTreeExtensions.AddChildSafely((Node)(object)_characterContainer, (Node)(object)val2);
						GodotTreeExtensions.MoveChildSafely((Node)(object)_characterContainer, (Node)(object)val2, 0);
						if (i > 0)
						{
							((CanvasItem)val2).Modulate = new Color(0.5f, 0.5f, 0.5f, 1f);
						}
						_characterVisuals.Add(val2);
						_characterRows.Add(i);
					}
				}
			}
			_charactersPending = _characterVisuals.Count > 0;
		}

		private void LayoutCharactersWhenReady()
		{
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			foreach (NCreatureVisuals characterVisual in _characterVisuals)
			{
				if (!GodotObject.IsInstanceValid((GodotObject)(object)characterVisual) || !((Node)characterVisual).IsNodeReady())
				{
					return;
				}
			}
			Dictionary<int, float> dictionary = new Dictionary<int, float>();
			for (int i = 0; i < _characterVisuals.Count; i++)
			{
				NCreatureVisuals val = _characterVisuals[i];
				int num = _characterRows[i];
				if (!dictionary.TryGetValue(num, out var value))
				{
					value = -75f * (float)num;
				}
				((Node2D)val).Position = new Vector2(value, -50f * (float)num);
				PlayIdle(val);
				Control bounds = val.Bounds;
				dictionary[num] = value - ((bounds != null) ? bounds.Size.X : 300f) * 0.5f - 25f;
			}
			_charactersPending = false;
			_characterVisuals.Clear();
			_characterRows.Clear();
		}

		private static void PlayIdle(NCreatureVisuals visuals)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			if (visuals.HasSpineAnimation)
			{
				SpineAnimationAccess spineAnimation = visuals.SpineAnimation;
				((SpineAnimationAccess)(ref spineAnimation)).SetAnimation("relaxed_loop", true, 0);
				spineAnimation = visuals.SpineAnimation;
				((SpineAnimationAccess)(ref spineAnimation)).SetTimeScale(Rng.Chaotic.NextFloat(0.9f, 1.1f));
			}
		}

		private void OnMerchantInput(InputEvent @event)
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Invalid comparison between Unknown and I8
			InputEventMouseButton val = (InputEventMouseButton)(object)((@event is InputEventMouseButton) ? @event : null);
			if (val == null || (long)val.ButtonIndex != 1 || !val.Pressed)
			{
				return;
			}
			if (_localPlayerDead)
			{
				MerchantDialogueSet? dialogue = _dialogue;
				SayLine((dialogue != null) ? dialogue.PlayerDeadLines : null);
			}
			else if (_inventory != null && !_inventory.IsOpen)
			{
				NProceedButton? proceedButton = _proceedButton;
				if (proceedButton != null)
				{
					((NClickableControl)proceedButton).Disable();
				}
				SuisuiShopAudio.SuppressVanillaVoice = true;
				SuisuiShopAudio.PlayOpen();
				_inventory.Open();
			}
		}

		private void OnInventoryClosed()
		{
			SuisuiShopAudio.SuppressVanillaVoice = false;
			if (!_finished)
			{
				NProceedButton? proceedButton = _proceedButton;
				if (proceedButton != null)
				{
					((NClickableControl)proceedButton).Enable();
				}
				NProceedButton? proceedButton2 = _proceedButton;
				if (proceedButton2 != null)
				{
					proceedButton2.SetPulseState(true);
				}
			}
		}

		private void OnProceedReleased(NButton _)
		{
			FinishEvent();
		}

		private void FinishEvent()
		{
			if (!_finished)
			{
				_finished = true;
				SuisuiShopAudio.SuppressVanillaVoice = false;
				NProceedButton? proceedButton = _proceedButton;
				if (proceedButton != null)
				{
					((NClickableControl)proceedButton).Disable();
				}
				TaskHelper.RunSafely(NEventRoom.Proceed());
			}
		}

		private void SayLine(IReadOnlyList<LocString>? lines)
		{
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			if (lines == null || lines.Count == 0)
			{
				return;
			}
			LocString val = Rng.Chaotic.NextItem<LocString>((IEnumerable<LocString>)lines);
			if (val != null)
			{
				Vector2 val2 = ((_merchantArea != null) ? (_merchantArea.GlobalPosition + _merchantArea.Size.X * Vector2.Left) : Vector2.Zero);
				NSpeechBubbleVfx val3 = NSpeechBubbleVfx.Create(val.GetFormattedText(), (DialogueSide)2, val2, 4.0, (VfxColor)2);
				if (val3 != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)this, (Node)(object)val3);
				}
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<MethodInfo> GetGodotMethodList()
		{
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0144: Unknown result type (might be due to invalid IL or missing references)
			//IL_016b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0174: Unknown result type (might be due to invalid IL or missing references)
			//IL_019b: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0204: Unknown result type (might be due to invalid IL or missing references)
			//IL_022b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0234: Unknown result type (might be due to invalid IL or missing references)
			//IL_025b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0264: Unknown result type (might be due to invalid IL or missing references)
			//IL_028b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0294: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ef: Expected O, but got Unknown
			//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_031d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0345: Unknown result type (might be due to invalid IL or missing references)
			//IL_0350: Expected O, but got Unknown
			//IL_034b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0357: Unknown result type (might be due to invalid IL or missing references)
			//IL_037e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0387: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e1: Expected O, but got Unknown
			//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_040f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0418: Unknown result type (might be due to invalid IL or missing references)
			List<MethodInfo> list = new List<MethodInfo>(18);
			list.Add(new MethodInfo(MethodName._EnterTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName._ExitTree, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.OnActiveScreenUpdated, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.BuildBackground, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.BuildMerchant, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.BuildInventory, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.InitInventory, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.BuildProceedButton, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.BuildFallbackProceedButton, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.InitProceedButton, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.BuildCharacters, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.LayoutCharactersWhenReady, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.PlayIdle, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("visuals"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false)
			}, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.OnMerchantInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("event"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.OnInventoryClosed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.OnProceedReleased, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("_"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null));
			list.Add(new MethodInfo(MethodName.FinishEvent, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null));
			return list;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0175: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0217: Unknown result type (might be due to invalid IL or missing references)
			//IL_024d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0283: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_02fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_033e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0374: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
			if ((ref method) == MethodName._EnterTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				((Node)this)._EnterTree();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName._ExitTree && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				((Node)this)._ExitTree();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnActiveScreenUpdated && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnActiveScreenUpdated();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.BuildBackground && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				BuildBackground();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.BuildMerchant && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				BuildMerchant();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.BuildInventory && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				BuildInventory();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.InitInventory && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				InitInventory();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.BuildProceedButton && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				BuildProceedButton();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.BuildFallbackProceedButton && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				BuildFallbackProceedButton();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.InitProceedButton && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				InitProceedButton();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.BuildCharacters && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				BuildCharacters();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.LayoutCharactersWhenReady && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				LayoutCharactersWhenReady();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.PlayIdle && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				PlayIdle(VariantUtils.ConvertTo<NCreatureVisuals>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnMerchantInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				OnMerchantInput(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnInventoryClosed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnInventoryClosed();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnProceedReleased && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				OnProceedReleased(VariantUtils.ConvertTo<NButton>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.FinishEvent && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				FinishEvent();
				ret = default(godot_variant);
				return true;
			}
			return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			if ((ref method) == MethodName.PlayIdle && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				PlayIdle(VariantUtils.ConvertTo<NCreatureVisuals>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			ret = default(godot_variant);
			return false;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool HasGodotClassMethod(in godot_string_name method)
		{
			if ((ref method) == MethodName._EnterTree)
			{
				return true;
			}
			if ((ref method) == MethodName._ExitTree)
			{
				return true;
			}
			if ((ref method) == MethodName.OnActiveScreenUpdated)
			{
				return true;
			}
			if ((ref method) == MethodName._Process)
			{
				return true;
			}
			if ((ref method) == MethodName.BuildBackground)
			{
				return true;
			}
			if ((ref method) == MethodName.BuildMerchant)
			{
				return true;
			}
			if ((ref method) == MethodName.BuildInventory)
			{
				return true;
			}
			if ((ref method) == MethodName.InitInventory)
			{
				return true;
			}
			if ((ref method) == MethodName.BuildProceedButton)
			{
				return true;
			}
			if ((ref method) == MethodName.BuildFallbackProceedButton)
			{
				return true;
			}
			if ((ref method) == MethodName.InitProceedButton)
			{
				return true;
			}
			if ((ref method) == MethodName.BuildCharacters)
			{
				return true;
			}
			if ((ref method) == MethodName.LayoutCharactersWhenReady)
			{
				return true;
			}
			if ((ref method) == MethodName.PlayIdle)
			{
				return true;
			}
			if ((ref method) == MethodName.OnMerchantInput)
			{
				return true;
			}
			if ((ref method) == MethodName.OnInventoryClosed)
			{
				return true;
			}
			if ((ref method) == MethodName.OnProceedReleased)
			{
				return true;
			}
			if ((ref method) == MethodName.FinishEvent)
			{
				return true;
			}
			return ((Control)this).HasGodotClassMethod(ref method);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
		{
			if ((ref name) == PropertyName._inventory)
			{
				_inventory = VariantUtils.ConvertTo<NMerchantInventory>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._proceedButton)
			{
				_proceedButton = VariantUtils.ConvertTo<NProceedButton>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._characterContainer)
			{
				_characterContainer = VariantUtils.ConvertTo<Control>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._charactersPending)
			{
				_charactersPending = VariantUtils.ConvertTo<bool>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._merchantArea)
			{
				_merchantArea = VariantUtils.ConvertTo<Control>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._merchantPortrait)
			{
				_merchantPortrait = VariantUtils.ConvertTo<Sprite2D>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._handAnchor)
			{
				_handAnchor = VariantUtils.ConvertTo<Node2D>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._hand)
			{
				_hand = VariantUtils.ConvertTo<Sprite2D>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._localPlayerDead)
			{
				_localPlayerDead = VariantUtils.ConvertTo<bool>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._greetPending)
			{
				_greetPending = VariantUtils.ConvertTo<bool>(ref value);
				return true;
			}
			if ((ref name) == PropertyName._finished)
			{
				_finished = VariantUtils.ConvertTo<bool>(ref value);
				return true;
			}
			return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			//IL_011a: Unknown result type (might be due to invalid IL or missing references)
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0144: Unknown result type (might be due to invalid IL or missing references)
			//IL_0169: Unknown result type (might be due to invalid IL or missing references)
			//IL_016e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0193: Unknown result type (might be due to invalid IL or missing references)
			//IL_0198: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e6: Unknown result type (might be due to invalid IL or missing references)
			if ((ref name) == PropertyName.DefaultFocusedControl)
			{
				Control defaultFocusedControl = DefaultFocusedControl;
				value = VariantUtils.CreateFrom<Control>(ref defaultFocusedControl);
				return true;
			}
			if ((ref name) == PropertyName._inventory)
			{
				value = VariantUtils.CreateFrom<NMerchantInventory>(ref _inventory);
				return true;
			}
			if ((ref name) == PropertyName._proceedButton)
			{
				value = VariantUtils.CreateFrom<NProceedButton>(ref _proceedButton);
				return true;
			}
			if ((ref name) == PropertyName._characterContainer)
			{
				value = VariantUtils.CreateFrom<Control>(ref _characterContainer);
				return true;
			}
			if ((ref name) == PropertyName._charactersPending)
			{
				value = VariantUtils.CreateFrom<bool>(ref _charactersPending);
				return true;
			}
			if ((ref name) == PropertyName._merchantArea)
			{
				value = VariantUtils.CreateFrom<Control>(ref _merchantArea);
				return true;
			}
			if ((ref name) == PropertyName._merchantPortrait)
			{
				value = VariantUtils.CreateFrom<Sprite2D>(ref _merchantPortrait);
				return true;
			}
			if ((ref name) == PropertyName._handAnchor)
			{
				value = VariantUtils.CreateFrom<Node2D>(ref _handAnchor);
				return true;
			}
			if ((ref name) == PropertyName._hand)
			{
				value = VariantUtils.CreateFrom<Sprite2D>(ref _hand);
				return true;
			}
			if ((ref name) == PropertyName._localPlayerDead)
			{
				value = VariantUtils.CreateFrom<bool>(ref _localPlayerDead);
				return true;
			}
			if ((ref name) == PropertyName._greetPending)
			{
				value = VariantUtils.CreateFrom<bool>(ref _greetPending);
				return true;
			}
			if ((ref name) == PropertyName._finished)
			{
				value = VariantUtils.CreateFrom<bool>(ref _finished);
				return true;
			}
			return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<PropertyInfo> GetGodotPropertyList()
		{
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_012c: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Unknown result type (might be due to invalid IL or missing references)
			//IL_016e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0190: Unknown result type (might be due to invalid IL or missing references)
			List<PropertyInfo> list = new List<PropertyInfo>();
			list.Add(new PropertyInfo((Type)24, PropertyName._inventory, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
			list.Add(new PropertyInfo((Type)24, PropertyName._proceedButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
			list.Add(new PropertyInfo((Type)24, PropertyName._characterContainer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
			list.Add(new PropertyInfo((Type)1, PropertyName._charactersPending, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
			list.Add(new PropertyInfo((Type)24, PropertyName._merchantArea, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
			list.Add(new PropertyInfo((Type)24, PropertyName._merchantPortrait, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
			list.Add(new PropertyInfo((Type)24, PropertyName._handAnchor, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
			list.Add(new PropertyInfo((Type)24, PropertyName._hand, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
			list.Add(new PropertyInfo((Type)1, PropertyName._localPlayerDead, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
			list.Add(new PropertyInfo((Type)1, PropertyName._greetPending, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
			list.Add(new PropertyInfo((Type)1, PropertyName._finished, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
			list.Add(new PropertyInfo((Type)24, PropertyName.DefaultFocusedControl, (PropertyHint)0, "", (PropertyUsageFlags)4096, false));
			return list;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void SaveGodotObjectData(GodotSerializationInfo info)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
			((GodotObject)this).SaveGodotObjectData(info);
			info.AddProperty(PropertyName._inventory, Variant.From<NMerchantInventory>(ref _inventory));
			info.AddProperty(PropertyName._proceedButton, Variant.From<NProceedButton>(ref _proceedButton));
			info.AddProperty(PropertyName._characterContainer, Variant.From<Control>(ref _characterContainer));
			info.AddProperty(PropertyName._charactersPending, Variant.From<bool>(ref _charactersPending));
			info.AddProperty(PropertyName._merchantArea, Variant.From<Control>(ref _merchantArea));
			info.AddProperty(PropertyName._merchantPortrait, Variant.From<Sprite2D>(ref _merchantPortrait));
			info.AddProperty(PropertyName._handAnchor, Variant.From<Node2D>(ref _handAnchor));
			info.AddProperty(PropertyName._hand, Variant.From<Sprite2D>(ref _hand));
			info.AddProperty(PropertyName._localPlayerDead, Variant.From<bool>(ref _localPlayerDead));
			info.AddProperty(PropertyName._greetPending, Variant.From<bool>(ref _greetPending));
			info.AddProperty(PropertyName._finished, Variant.From<bool>(ref _finished));
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void RestoreGodotObjectData(GodotSerializationInfo info)
		{
			((GodotObject)this).RestoreGodotObjectData(info);
			Variant val = default(Variant);
			if (info.TryGetProperty(PropertyName._inventory, ref val))
			{
				_inventory = ((Variant)(ref val)).As<NMerchantInventory>();
			}
			Variant val2 = default(Variant);
			if (info.TryGetProperty(PropertyName._proceedButton, ref val2))
			{
				_proceedButton = ((Variant)(ref val2)).As<NProceedButton>();
			}
			Variant val3 = default(Variant);
			if (info.TryGetProperty(PropertyName._characterContainer, ref val3))
			{
				_characterContainer = ((Variant)(ref val3)).As<Control>();
			}
			Variant val4 = default(Variant);
			if (info.TryGetProperty(PropertyName._charactersPending, ref val4))
			{
				_charactersPending = ((Variant)(ref val4)).As<bool>();
			}
			Variant val5 = default(Variant);
			if (info.TryGetProperty(PropertyName._merchantArea, ref val5))
			{
				_merchantArea = ((Variant)(ref val5)).As<Control>();
			}
			Variant val6 = default(Variant);
			if (info.TryGetProperty(PropertyName._merchantPortrait, ref val6))
			{
				_merchantPortrait = ((Variant)(ref val6)).As<Sprite2D>();
			}
			Variant val7 = default(Variant);
			if (info.TryGetProperty(PropertyName._handAnchor, ref val7))
			{
				_handAnchor = ((Variant)(ref val7)).As<Node2D>();
			}
			Variant val8 = default(Variant);
			if (info.TryGetProperty(PropertyName._hand, ref val8))
			{
				_hand = ((Variant)(ref val8)).As<Sprite2D>();
			}
			Variant val9 = default(Variant);
			if (info.TryGetProperty(PropertyName._localPlayerDead, ref val9))
			{
				_localPlayerDead = ((Variant)(ref val9)).As<bool>();
			}
			Variant val10 = default(Variant);
			if (info.TryGetProperty(PropertyName._greetPending, ref val10))
			{
				_greetPending = ((Variant)(ref val10)).As<bool>();
			}
			Variant val11 = default(Variant);
			if (info.TryGetProperty(PropertyName._finished, ref val11))
			{
				_finished = ((Variant)(ref val11)).As<bool>();
			}
		}
	}
	public enum DemonForceOwner
	{
		Card,
		Relic
	}
	public enum DemonForceId
	{
		ThievingHopper,
		Chomper,
		Tunneler,
		Myte,
		SlimedBerserker,
		TheLost,
		TheForgotten,
		ScrollOfBiting,
		Exoskeleton,
		SpinyToad,
		HunterKiller,
		Decimillipede,
		Entomancer,
		InfestedPrism,
		KnowledgeDemon,
		TheInsatiable,
		KaiserCrab,
		GlobeHead,
		TurretOperator,
		Axebot,
		OwlMagistrate,
		DevotedSculptor,
		FrogKnight,
		ConstructMenagerie,
		SoulNexus,
		SpectralKnight,
		FlailKnight,
		MagiKnight,
		Queen,
		TestSubject,
		Aeonglass,
		TheObscura,
		BowlbugRock,
		BowlbugNectar,
		BowlbugEgg,
		BowlbugSilk,
		LouseProgenitor,
		SlumberingBeetle,
		Ovicopter,
		Fabricator
	}
	public sealed class DemonForce
	{
		public DemonForceId Id { get; }

		public DemonForceOwner Owner { get; }

		public string MonsterEntry { get; }

		public string Name { get; }

		public string Description { get; }

		public string Demon { get; }

		public IEnumerable<string> MonsterEntries => MonsterEntry.Split('|', StringSplitOptions.RemoveEmptyEntries);

		public bool IsCardForce => Owner == DemonForceOwner.Card;

		public DemonForce(DemonForceId id, DemonForceOwner owner, string monsterEntry, string name, string description, string demon = "")
		{
			Id = id;
			Owner = owner;
			MonsterEntry = monsterEntry;
			Name = name;
			Description = description;
			Demon = demon;
		}
	}
	public static class OverlimitUnlocks
	{
		[CompilerGenerated]
		private sealed class <>c__DisplayClass7_0
		{
			public HashSet<int> set;

			public Func<string, bool> <>9__0;

			internal bool <UnlockedForces>b__0(string e)
			{
				return set.Contains(HashOf(e));
			}
		}

		[CompilerGenerated]
		private sealed class <UnlockedForces>d__7 : IEnumerable<DemonForce>, IEnumerable, IEnumerator<DemonForce>, IEnumerator, IDisposable
		{
			private int <>1__state;

			private DemonForce <>2__current;

			private int <>l__initialThreadId;

			private IReadOnlyList<int> hashes;

			public IReadOnlyList<int> <>3__hashes;

			private <>c__DisplayClass7_0 <>8__1;

			private DemonForce[] <>s__2;

			private int <>s__3;

			private DemonForce <force>5__4;

			DemonForce IEnumerator<DemonForce>.Current
			{
				[DebuggerHidden]
				get
				{
					return <>2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return <>2__current;
				}
			}

			[DebuggerHidden]
			public <UnlockedForces>d__7(int <>1__state)
			{
				this.<>1__state = <>1__state;
				<>l__initialThreadId = Environment.CurrentManagedThreadId;
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
				<>8__1 = null;
				<>s__2 = null;
				<force>5__4 = null;
				<>1__state = -2;
			}

			private bool MoveNext()
			{
				int num = <>1__state;
				if (num != 0)
				{
					if (num != 1)
					{
						return false;
					}
					<>1__state = -1;
					goto IL_00d0;
				}
				<>1__state = -1;
				<>8__1 = new <>c__DisplayClass7_0();
				<>8__1.set = new HashSet<int>(hashes);
				<>s__2 = All;
				<>s__3 = 0;
				goto IL_00e6;
				IL_00d0:
				<force>5__4 = null;
				<>s__3++;
				goto IL_00e6;
				IL_00e6:
				if (<>s__3 < <>s__2.Length)
				{
					<force>5__4 = <>s__2[<>s__3];
					if (<force>5__4.MonsterEntries.Any((string e) => <>8__1.set.Contains(HashOf(e))))
					{
						<>2__current = <force>5__4;
						<>1__state = 1;
						return true;
					}
					goto IL_00d0;
				}
				<>s__2 = null;
				return false;
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
				throw new NotSupportedException();
			}

			[DebuggerHidden]
			IEnumerator<DemonForce> IEnumerable<DemonForce>.GetEnumerator()
			{
				<UnlockedForces>d__7 <UnlockedForces>d__;
				if (<>1__state == -2 && <>l__initialThreadId == Environment.CurrentManagedThreadId)
				{
					<>1__state = 0;
					<UnlockedForces>d__ = this;
				}
				else
				{
					<UnlockedForces>d__ = new <UnlockedForces>d__7(0);
				}
				<UnlockedForces>d__.hashes = <>3__hashes;
				return <UnlockedForces>d__;
			}

			[DebuggerHidden]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<DemonForce>)this).GetEnumerator();
			}
		}

		public static readonly DemonForce[] All = new DemonForce[40]
		{
			new DemonForce(DemonForceId.ThievingHopper, DemonForceOwner.Card, "THIEVING_HOPPER", "偷窃恶魔", "超阈限斩杀时，额外获得一次卡牌奖励"),
			new DemonForce(DemonForceId.Chomper, DemonForceOwner.Card, "CHOMPER", "啃咬恶魔", "打出超阈限降低所有敌人 8 点力量"),
			new DemonForce(DemonForceId.Tunneler, DemonForceOwner.Card, "TUNNELER", "钻洞恶魔", "超阈限获得32点格挡"),
			new DemonForce(DemonForceId.Myte, DemonForceOwner.Card, "MYTE", "异螨恶魔", "超阈限给予敌人 10 层中毒"),
			new DemonForce(DemonForceId.SlimedBerserker, DemonForceOwner.Card, "SLIMED_BERSERKER", "粘液恶魔", "超阈限给予所有敌人3层虚弱"),
			new DemonForce(DemonForceId.TheLost, DemonForceOwner.Card, "THE_LOST", "力量恶魔", "超阈限使敌人失去2点力量"),
			new DemonForce(DemonForceId.TheForgotten, DemonForceOwner.Card, "THE_FORGOTTEN", "敏捷恶魔", "超阈限使敌人失去2点敏捷"),
			new DemonForce(DemonForceId.ScrollOfBiting, DemonForceOwner.Card, "SCROLL_OF_BITING", "嗜血恶魔", "超阈限斩杀时获得2点最大生命值"),
			new DemonForce(DemonForceId.Exoskeleton, DemonForceOwner.Relic, "EXOSKELETON", "蟑螂恶魔", "受到的所有伤害和生命减少效果不会超过 18 点"),
			new DemonForce(DemonForceId.SpinyToad, DemonForceOwner.Relic, "SPINY_TOAD", "蟾蜍恶魔", "战斗开始时获得 5 点荆棘"),
			new DemonForce(DemonForceId.HunterKiller, DemonForceOwner.Relic, "HUNTER_KILLER", "猎杀恶魔", "你每攻击一次敌人就使其失去 1 点力量"),
			new DemonForce(DemonForceId.Decimillipede, DemonForceOwner.Relic, "DECIMILLIPEDE_SEGMENT_FRONT|DECIMILLIPEDE_SEGMENT_MIDDLE|DECIMILLIPEDE_SEGMENT_BACK", "千足恶魔", "濒死时以 25 生命复活最多三次"),
			new DemonForce(DemonForceId.Entomancer, DemonForceOwner.Relic, "ENTOMANCER", "蜂群恶魔", "力量对所有牌的加成为 2 倍"),
			new DemonForce(DemonForceId.InfestedPrism, DemonForceOwner.Relic, "INFESTED_PRISM", "辐射恶魔", "敌人每攻击一次你，你就获得 6 点活力", "魔物恶魔"),
			new DemonForce(DemonForceId.KnowledgeDemon, DemonForceOwner.Relic, "KNOWLEDGE_DEMON", "知识恶魔", "你的回合结束时一名随机敌人失去 15 点生命值", "知识恶魔"),
			new DemonForce(DemonForceId.TheInsatiable, DemonForceOwner.Relic, "THE_INSATIABLE", "狂沙恶魔", "战斗开始时将 1 张狂沙加入抽牌堆", "沙暴恶魔"),
			new DemonForce(DemonForceId.KaiserCrab, DemonForceOwner.Relic, "KAISER_CRAB", "夹击恶魔", "战斗开始时使一名随机敌人受到的伤害增加百分之 50", "蟹爪恶魔"),
			new DemonForce(DemonForceId.GlobeHead, DemonForceOwner.Relic, "GLOBE_HEAD", "流电恶魔", "敌人强化时失去8点生命值", "机器恶魔"),
			new DemonForce(DemonForceId.TurretOperator, DemonForceOwner.Relic, "TURRET_OPERATOR", "高塔恶魔", "每回合开始时使一名随机队友获得25点格挡", "机器恶魔"),
			new DemonForce(DemonForceId.Axebot, DemonForceOwner.Relic, "AXEBOT", "库存恶魔", "濒死时消耗你弃牌堆的所有牌然后以满血复活", "机器恶魔"),
			new DemonForce(DemonForceId.OwlMagistrate, DemonForceOwner.Relic, "OWL_MAGISTRATE", "翱翔恶魔", "战斗开始的第三个回合获得翱翔，第四个回合失去之", "机器恶魔"),
			new DemonForce(DemonForceId.DevotedSculptor, DemonForceOwner.Relic, "DEVOTED_SCULPTOR", "仪式恶魔", "战斗开始时获得3点仪式", "机器恶魔"),
			new DemonForce(DemonForceId.FrogKnight, DemonForceOwner.Relic, "FROG_KNIGHT", "骑士恶魔", "战斗开始时获得15点覆甲", "机器恶魔"),
			new DemonForce(DemonForceId.ConstructMenagerie, DemonForceOwner.Relic, "PUNCH_CONSTRUCT|CUBEX_CONSTRUCT|MECHA_KNIGHT", "人工恶魔", "获得 2 层人工制品", "机器恶魔"),
			new DemonForce(DemonForceId.SoulNexus, DemonForceOwner.Relic, "SOUL_NEXUS", "灵魂恶魔", "战斗开始时对所有敌人造成31点伤害", "机器恶魔"),
			new DemonForce(DemonForceId.SpectralKnight, DemonForceOwner.Relic, "SPECTRAL_KNIGHT", "恶咒恶魔", "每回合结束时选择至多3张手牌获得虚无", "机器恶魔"),
			new DemonForce(DemonForceId.FlailKnight, DemonForceOwner.Relic, "FLAIL_KNIGHT", "连枷恶魔", "战斗开始时获得2点力量", "机器恶魔"),
			new DemonForce(DemonForceId.MagiKnight, DemonForceOwner.Relic, "MAGI_KNIGHT", "抑制恶魔", "斩杀时随机升级你牌组中的一张牌", "机器恶魔"),
			new DemonForce(DemonForceId.Queen, DemonForceOwner.Relic, "QUEEN", "魂锁恶魔", "每回合结束时选择三张手牌保留", "魂锁恶魔"),
			new DemonForce(DemonForceId.TestSubject, DemonForceOwner.Relic, "TEST_SUBJECT", "融合恶魔", "濒死时回复至满血并获得 1 层无实体最多三次", "融合恶魔"),
			new DemonForce(DemonForceId.Aeonglass, DemonForceOwner.Relic, "AEONGLASS", "时间恶魔", "你每对一名敌人造成 6 次伤害，就使其获得 6 点消亡", "时间恶魔"),
			new DemonForce(DemonForceId.TheObscura, DemonForceOwner.Relic, "THE_OBSCURA", "胧光恶魔", "每打出一张技能牌就获得 1 点格挡"),
			new DemonForce(DemonForceId.BowlbugRock, DemonForceOwner.Card, "BOWLBUG_ROCK", "石碗恶魔", "超阈限获得 16 点格挡"),
			new DemonForce(DemonForceId.BowlbugNectar, DemonForceOwner.Relic, "BOWLBUG_NECTAR", "蜜碗恶魔", "你的第三个回合获得 15 点临时力量"),
			new DemonForce(DemonForceId.BowlbugEgg, DemonForceOwner.Relic, "BOWLBUG_EGG", "卵碗恶魔", "每回合开始时随机造成 5 点伤害获得 5 点格挡"),
			new DemonForce(DemonForceId.BowlbugSilk, DemonForceOwner.Card, "BOWLBUG_SILK", "丝碗恶魔", "超阈限给予 2 层虚弱 额外造成 9 点伤害"),
			new DemonForce(DemonForceId.LouseProgenitor, DemonForceOwner.Relic, "LOUSE_PROGENITOR", "虱虫恶魔", "回合外第一次受到伤害后获得 16 点格挡"),
			new DemonForce(DemonForceId.SlumberingBeetle, DemonForceOwner.Relic, "SLUMBERING_BEETLE", "熟睡恶魔", "若你未打出任何牌就结束回合且受到过伤害，接下来每回合开始时获得 2 点力量"),
			new DemonForce(DemonForceId.Ovicopter, DemonForceOwner.Relic, "OVICOPTER", "产卵恶魔", "队友死亡时以 15 生命值复活并清除所有负面状态 全局游戏限定一次"),
			new DemonForce(DemonForceId.Fabricator, DemonForceOwner.Relic, "FABRICATOR", "组装恶魔", "所有队友每回合开始时获得 1 点能量")
		};

		private static readonly Dictionary<string, DemonForce> ByMonster = BuildIndex();

		private static Dictionary<string, DemonForce> BuildIndex()
		{
			Dictionary<string, DemonForce> dictionary = new Dictionary<string, DemonForce>(StringComparer.OrdinalIgnoreCase);
			DemonForce[] all = All;
			foreach (DemonForce demonForce in all)
			{
				foreach (string monsterEntry in demonForce.MonsterEntries)
				{
					dictionary[monsterEntry] = demonForce;
				}
			}
			return dictionary;
		}

		public static int HashOf(string entry)
		{
			uint num = 2166136261u;
			string text = entry.ToUpperInvariant();
			foreach (char c in text)
			{
				num ^= c;
				num *= 16777619;
			}
			return (int)num;
		}

		public static DemonForce? ForceForMonster(ModelId monsterId)
		{
			return ForceForEntry(monsterId.Entry);
		}

		public static DemonForce? ForceForEntry(string entry)
		{
			DemonForce value;
			return ByMonster.TryGetValue(entry, out value) ? value : null;
		}

		public static bool IsUnlocked(IReadOnlyList<int> unlocked, DemonForce force)
		{
			return Array.IndexOf(unlocked.ToArray(), HashOf(force.MonsterEntries.First())) >= 0;
		}

		[IteratorStateMachine(typeof(<UnlockedForces>d__7))]
		public static IEnumerable<DemonForce> UnlockedForces(IReadOnlyList<int> hashes)
		{
			//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
			return new <UnlockedForces>d__7(-2)
			{
				<>3__hashes = hashes
			};
		}

		public static IEnumerable<DemonForce> UnlockedForces(IReadOnlyList<int> hashes, DemonForceOwner owner)
		{
			return from f in UnlockedForces(hashes)
				where f.Owner == owner
				select f;
		}

		public static int[] WithMonster(int[] unlocked, string entry)
		{
			int num = HashOf(entry);
			if (Array.IndexOf(unlocked, num) >= 0)
			{
				return unlocked;
			}
			int[] array = new int[unlocked.Length + 1];
			Array.Copy(unlocked, array, unlocked.Length);
			array[unlocked.Length] = num;
			return array;
		}

		public static string BuildDescription(IReadOnlyList<int> hashes, string lineFormat, string moreFormat)
		{
			List<DemonForce> list = UnlockedForces(hashes).ToList();
			if (list.Count == 0)
			{
				return string.Empty;
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (DemonForce item in list)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append('\n');
				}
				stringBuilder.Append(string.Format(lineFormat, item.Name, item.Description));
			}
			return stringBuilder.ToString();
		}
	}
	internal static class SuisuiShopAudio
	{
		internal const string OpenSfx = "res://wuwancients/audio/suisui_shop_open.wav";

		internal static bool SuppressVanillaVoice { get; set; }

		internal static void PlayOpen()
		{
			if (ResourceLoader.Exists("res://wuwancients/audio/suisui_shop_open.wav", ""))
			{
				NAudioManager instance = NAudioManager.Instance;
				if (instance != null)
				{
					instance.PlayOneShot("res://wuwancients/audio/suisui_shop_open.wav", 1f);
				}
			}
		}
	}
	[HarmonyPatch(typeof(SfxCmd), "Play", new Type[]
	{
		typeof(string),
		typeof(float)
	})]
	internal static class SuisuiShopVanillaVoicePatch
	{
		private static readonly string[] VanillaMerchantVoices = new string[3] { "event:/sfx/npcs/merchant/merchant_welcome", "event:/sfx/npcs/merchant/merchant_thank_yous", "event:/sfx/npcs/merchant/merchant_dissapointment" };

		private static bool Prefix(string sfx)
		{
			if (!SuisuiShopAudio.SuppressVanillaVoice)
			{
				return true;
			}
			if (string.IsNullOrEmpty(sfx))
			{
				return true;
			}
			string[] vanillaMerchantVoices = VanillaMerchantVoices;
			foreach (string text in vanillaMerchantVoices)
			{
				if (sfx == text)
				{
					return false;
				}
			}
			return true;
		}
	}
	internal static class SyncedRng
	{
		internal static int Index(Rng? rng, int count)
		{
			return (rng != null && count > 0) ? rng.NextInt(count) : 0;
		}

		internal static T? Pick<T>(Rng? rng, IReadOnlyList<T>? pool) where T : class
		{
			return (pool == null || pool.Count == 0) ? null : pool[Index(rng, pool.Count)];
		}

		internal static Rng? Targets(IRunState? runState)
		{
			object result;
			if (runState == null)
			{
				result = null;
			}
			else
			{
				RunRngSet rng = runState.Rng;
				result = ((rng != null) ? rng.CombatTargets : null);
			}
			return (Rng?)result;
		}

		internal static Rng? Cards(IRunState? runState)
		{
			object result;
			if (runState == null)
			{
				result = null;
			}
			else
			{
				RunRngSet rng = runState.Rng;
				result = ((rng != null) ? rng.CombatCardSelection : null);
			}
			return (Rng?)result;
		}

		internal static Rng? Potions(IRunState? runState)
		{
			object result;
			if (runState == null)
			{
				result = null;
			}
			else
			{
				RunRngSet rng = runState.Rng;
				result = ((rng != null) ? rng.CombatPotionGeneration : null);
			}
			return (Rng?)result;
		}

		internal static Rng? Niche(IRunState? runState)
		{
			object result;
			if (runState == null)
			{
				result = null;
			}
			else
			{
				RunRngSet rng = runState.Rng;
				result = ((rng != null) ? rng.Niche : null);
			}
			return (Rng?)result;
		}

		internal static int DeterministicIndex(IRunState? runState, ulong salt, int count)
		{
			if (count <= 0)
			{
				return 0;
			}
			ulong num = ((runState == null) ? 0 : runState.Rng.Seed);
			ulong num2 = num + (ulong)((long)salt * -7046029254386353131L);
			num2 = (num2 ^ (num2 >> 30)) * 13787848793156543929uL;
			num2 = (num2 ^ (num2 >> 27)) * 10723151780598845931uL;
			num2 ^= num2 >> 31;
			return (int)(num2 % (ulong)count);
		}
	}
	public static class WuwuDelivery
	{
		public enum Kind
		{
			CardRewards,
			TwoRelics,
			AncientRelic
		}

		public const int RoomsUntilDelivery = 3;

		private const ulong SaltUpgrade = 4097uL;

		private const ulong SaltPotion = 8194uL;

		private const ulong SaltRelic = 12291uL;

		private static readonly SavedSpireField<RelicModel, string> Order = new SavedSpireField<RelicModel, string>((Func<string>)(() => (string?)null), "wuwancients_wuwu_order");

		private static readonly SavedSpireField<RelicModel, string> PendingCards = new SavedSpireField<RelicModel, string>((Func<string>)(() => (string?)null), "wuwancients_wuwu_pending_cards");

		public static void Schedule(Player player, Kind kind)
		{
			Write(player, Order, $"{kind}|{3}");
			Log.Info($"[wuwancients] wuwu logistics order placed: {kind}", 2);
		}

		private static string? Read(Player player, SavedSpireField<RelicModel, string> field)
		{
			foreach (RelicModel relic in player.Relics)
			{
				string text = ((SpireField<RelicModel, string>)(object)field)[relic];
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
			return null;
		}

		private static void Write(Player player, SavedSpireField<RelicModel, string> field, string? value)
		{
			RelicModel val = null;
			foreach (RelicModel relic in player.Relics)
			{
				if (!string.IsNullOrEmpty(((SpireField<RelicModel, string>)(object)field)[relic]))
				{
					val = relic;
					break;
				}
			}
			if (val == null)
			{
				val = Carrier(player);
			}
			if (val == null)
			{
				Log.Error("[wuwancients] wuwu logistics: player has no relic to carry the order", 2);
			}
			else
			{
				((SpireField<RelicModel, string>)(object)field)[val] = value;
			}
		}

		private static RelicModel? Carrier(Player player)
		{
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Invalid comparison between Unknown and I4
			RelicModel val = null;
			RelicModel val2 = null;
			foreach (RelicModel relic in player.Relics)
			{
				if (relic != null)
				{
					if (val == null && (int)relic.Rarity == 1)
					{
						val = relic;
					}
					if (val2 == null || string.CompareOrdinal(((AbstractModel)relic).Id.Entry, ((AbstractModel)val2).Id.Entry) < 0)
					{
						val2 = relic;
					}
				}
			}
			return val ?? val2;
		}

		public static async Task Tick(Player player)
		{
			if (TryParse(Read(player, PendingCards), out var pendingCount, out var pendingColorless) && player.RunState.CurrentRoom is CombatRoom)
			{
				Write(player, PendingCards, null);
				GrantCardRewards(player, pendingCount, pendingColorless != 0);
			}
			if (TryParse(Read(player, Order), out var kindValue, out var roomsLeft2))
			{
				roomsLeft2--;
				if (roomsLeft2 > 0)
				{
					Write(player, Order, $"{kindValue}|{roomsLeft2}");
					Log.Info($"[wuwancients] wuwu logistics arriving in {roomsLeft2} room(s)", 2);
				}
				else
				{
					Write(player, Order, null);
					await Deliver(player, (Kind)kindValue);
				}
			}
		}

		private static bool TryParse(string? raw, out int a, out int b)
		{
			a = 0;
			b = 0;
			if (string.IsNullOrEmpty(raw))
			{
				return false;
			}
			string[] array = raw.Split('|');
			if (array.Length != 2)
			{
				return false;
			}
			return int.TryParse(array[0], out a) && int.TryParse(array[1], out b);
		}

		private static async Task Deliver(Player player, Kind kind)
		{
			Log.Info($"[wuwancients] wuwu logistics delivered: {kind}", 2);
			switch (kind)
			{
			case Kind.CardRewards:
				GrantCardRewards(player, 2, colorless: false);
				break;
			case Kind.TwoRelics:
			{
				for (int i = 0; i < 2; i++)
				{
					await GrantRandomRelic(player, ancientOnly: false, i);
				}
				break;
			}
			case Kind.AncientRelic:
				await GrantRandomRelic(player, ancientOnly: true);
				break;
			}
		}

		public static void GrantCardRewards(Player player, int count, bool colorless)
		{
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Expected O, but got Unknown
			CardCreationOptions val = (colorless ? CardCreationOptions.ForNonCombatWithDefaultOdds((IEnumerable<CardPoolModel>)(object)new ColorlessCardPool[1] { ModelDb.CardPool<ColorlessCardPool>() }, (Func<CardModel, bool>)null) : null);
			AbstractRoom currentRoom = player.RunState.CurrentRoom;
			CombatRoom val2 = (CombatRoom)(object)((currentRoom is CombatRoom) ? currentRoom : null);
			if (val2 != null)
			{
				for (int i = 0; i < count; i++)
				{
					val2.AddExtraReward(player, (Reward)new CardReward(colorless ? val : CardCreationOptions.ForRoom(player, ((AbstractRoom)val2).RoomType), 3, player, (PlayerChoiceSynchronizer)null));
				}
			}
			else
			{
				Write(player, PendingCards, $"{count}|{(colorless ? 1 : 0)}");
			}
		}

		public static IEnumerable<CardCreationResult> CreateColorlessRewardOptions(Player player, int count)
		{
			CardCreationOptions val = CardCreationOptions.ForNonCombatWithDefaultOdds((IEnumerable<CardPoolModel>)(object)new ColorlessCardPool[1] { ModelDb.CardPool<ColorlessCardPool>() }, (Func<CardModel, bool>)null);
			return CardFactory.CreateForReward(player, count, val);
		}

		public static Task UpgradeRandomDeckCards(Player player, int count)
		{
			List<CardModel> list = player.Deck.Cards.Where((CardModel c) => c != null && !c.IsUpgraded).ToList();
			for (int i = 0; i < count; i++)
			{
				if (list.Count <= 0)
				{
					break;
				}
				CardModel val = list[SyncedRng.DeterministicIndex(player.RunState, 4097uL + (ulong)i, list.Count)];
				if (val == null)
				{
					break;
				}
				list.Remove(val);
				CardCmd.Upgrade(val, (CardPreviewStyle)1);
			}
			return Task.CompletedTask;
		}

		public static async Task GrantRandomPotions(Player player, int count)
		{
			List<PotionModel> pool = ModelDb.AllPotions.ToList();
			for (int i = 0; i < count; i++)
			{
				if (pool.Count <= 0)
				{
					break;
				}
				PotionModel pick = pool[SyncedRng.DeterministicIndex(player.RunState, 8194uL + (ulong)i, pool.Count)].ToMutable();
				if (pick == null)
				{
					break;
				}
				await PotionCmd.TryToProcure(pick, player, -1);
			}
		}

		public static async Task GrantRandomRelic(Player player, bool ancientOnly, int saltIndex = 0)
		{
			Player player2 = player;
			List<RelicModel> pool = (from r in ModelDb.AllRelics.Where(delegate(RelicModel r)
				{
					//IL_0015: Unknown result type (might be due to invalid IL or missing references)
					//IL_001a: Unknown result type (might be due to invalid IL or missing references)
					//IL_001b: Unknown result type (might be due to invalid IL or missing references)
					//IL_001d: Unknown result type (might be due to invalid IL or missing references)
					//IL_001f: Invalid comparison between Unknown and I4
					//IL_0009: Unknown result type (might be due to invalid IL or missing references)
					//IL_000f: Invalid comparison between Unknown and I4
					if (ancientOnly)
					{
						return (int)r.Rarity == 7;
					}
					RelicRarity rarity = r.Rarity;
					return rarity - 2 <= 3;
				})
				where !IsExcludedAncientRelic(r)
				where !player2.Relics.Any((RelicModel owned) => ((AbstractModel)owned).Id.Equals(((AbstractModel)r).Id))
				select r).ToList();
			if (pool.Count == 0 && ancientOnly)
			{
				Log.Warn("[wuwancients] wuwu logistics: no ancient relic left, falling back to any relic", 2);
				pool = ModelDb.AllRelics.Where((RelicModel r) => !player2.Relics.Any((RelicModel owned) => ((AbstractModel)owned).Id.Equals(((AbstractModel)r).Id))).ToList();
			}
			if (pool.Count == 0)
			{
				Log.Error("[wuwancients] wuwu logistics: relic pool is empty, nothing to deliver", 2);
				return;
			}
			RelicModel pick = pool[SyncedRng.DeterministicIndex(player2.RunState, 12291uL + (ulong)saltIndex, pool.Count)].ToMutable();
			if (pick != null)
			{
				await RelicCmd.Obtain(pick, player2, -1);
			}
		}

		private static bool IsExcludedAncientRelic(RelicModel relic)
		{
			string entry = ((AbstractModel)relic).Id.Entry;
			return entry.StartsWith("WUWANCIENTS-SHOREKEEPER", StringComparison.OrdinalIgnoreCase) || entry.StartsWith("NEOW", StringComparison.OrdinalIgnoreCase);
		}
	}
}
namespace wuwancients.Relics
{
	public sealed class AnnotationsOfEternalPreservation : RelicModel
	{
		private const int MeltsEveryCombats = 4;

		private int _combatsSeen;

		private bool _isGrantingWax;

		private bool _subscribed;

		private string _waxRelicKeys = string.Empty;

		private static readonly RelicRarity[] WaxRarities;

		[SavedProperty]
		public int CombatsSeen
		{
			get
			{
				return _combatsSeen;
			}
			private set
			{
				((AbstractModel)this).AssertMutable();
				_combatsSeen = value;
				((RelicModel)this).InvokeDisplayAmountChanged();
			}
		}

		[SavedProperty]
		public string WaxRelicKeys
		{
			get
			{
				return _waxRelicKeys;
			}
			private set
			{
				((AbstractModel)this).AssertMutable();
				_waxRelicKeys = value ?? string.Empty;
			}
		}

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/annotations_of_eternal_preservation.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/annotations_of_eternal_preservation.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/annotations_of_eternal_preservation.png";

		public override bool ShowCounter => true;

		public override int DisplayAmount => CombatsSeen % 4;

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				Subscribe();
				await Task.CompletedTask;
			}
		}

		private void OnRelicObtained(RelicModel relic)
		{
			if (((RelicModel)this).Owner != null && !_isGrantingWax && relic != this && !relic.IsWax)
			{
				TaskHelper.RunSafely(GrantWaxRelicAsync());
			}
		}

		private async Task GrantWaxRelicAsync()
		{
			if (((RelicModel)this).Owner == null || _isGrantingWax)
			{
				return;
			}
			_isGrantingWax = true;
			try
			{
				RelicRarity waxRarity = WaxRarities[SyncedRng.DeterministicIndex(((RelicModel)this).Owner.RunState, 16388uL + (ulong)WaxRelicKeys.Length, WaxRarities.Length)];
				RelicModel waxRelic = RelicFactory.PullNextRelicFromFront(((RelicModel)this).Owner, waxRarity).ToMutable();
				waxRelic.IsWax = true;
				WaxRelicKeys = WaxOriginTracker.Register(WaxRelicKeys, waxRelic);
				await RelicCmd.Obtain(waxRelic, ((RelicModel)this).Owner, -1);
				((RelicModel)this).Flash();
			}
			finally
			{
				_isGrantingWax = false;
			}
		}

		public override async Task AfterCombatEnd(CombatRoom room)
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			CombatsSeen++;
			if (CombatsSeen % 4 == 0)
			{
				RelicModel waxRelic = GetOwnWaxRelic();
				if (waxRelic != null)
				{
					await RelicCmd.Melt(waxRelic);
					WaxRelicKeys = WaxOriginTracker.Unregister(WaxRelicKeys, waxRelic);
				}
			}
		}

		private RelicModel? GetOwnWaxRelic()
		{
			if (((RelicModel)this).Owner == null)
			{
				return null;
			}
			foreach (RelicModel relic in ((RelicModel)this).Owner.Relics)
			{
				if (relic == null || !relic.IsWax || relic.IsMelted || !WaxOriginTracker.Owns(WaxRelicKeys, relic))
				{
					continue;
				}
				return relic;
			}
			return null;
		}

		public override Task BeforeCombatStart()
		{
			Subscribe();
			return Task.CompletedTask;
		}

		public override Task AfterRoomEntered(AbstractRoom room)
		{
			Subscribe();
			return Task.CompletedTask;
		}

		private void Subscribe()
		{
			if (((RelicModel)this).Owner != null && !_subscribed)
			{
				((RelicModel)this).Owner.RelicObtained += OnRelicObtained;
				_subscribed = true;
			}
		}

		public override async Task AfterRemoved()
		{
			if (((RelicModel)this).Owner != null && _subscribed)
			{
				((RelicModel)this).Owner.RelicObtained -= OnRelicObtained;
				_subscribed = false;
			}
			await Task.CompletedTask;
		}

		static AnnotationsOfEternalPreservation()
		{
			RelicRarity[] array = new RelicRarity[4];
			RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
			WaxRarities = (RelicRarity[])(object)array;
		}
	}
	public sealed class AssortedMeatSauceNoodles : RelicModel
	{
		private const decimal MaxHpLossPercent = 0.2m;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override string PackedIconPath => "res://wuwancients/images/relics/assorted_meat_sauce_noodles.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/assorted_meat_sauce_noodles.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/assorted_meat_sauce_noodles.png";

		public override async Task AfterObtained()
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				await CreatureCmd.LoseMaxHp((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RelicModel)this).Owner.Creature, (decimal)((RelicModel)this).Owner.Creature.MaxHp * 0.2m, false);
				((RelicModel)this).Flash();
			}
		}

		public override bool ShouldDisableRemainingRestSiteOptions(Player player)
		{
			return player != ((RelicModel)this).Owner;
		}
	}
	internal static class NoodlesRestSiteUses
	{
		internal const int MaxUsesPerOption = 2;

		private static readonly Dictionary<RestSiteOption, int> Uses = new Dictionary<RestSiteOption, int>();

		internal static void Reset()
		{
			Uses.Clear();
		}

		public static void MaybeRemoveAt(List<RestSiteOption> options, int index)
		{
			if (options == null || index < 0 || index >= options.Count)
			{
				return;
			}
			RestSiteOption val = options[index];
			if (!OwnedByNoodles(val))
			{
				options.RemoveAt(index);
				return;
			}
			Uses.TryGetValue(val, out var value);
			value++;
			if (value >= 2)
			{
				Uses.Remove(val);
				options.RemoveAt(index);
			}
			else
			{
				Uses[val] = value;
			}
		}

		private static bool OwnedByNoodles(RestSiteOption option)
		{
			try
			{
				Player value = Traverse.Create((object)option).Property("Owner", (object[])null).GetValue<Player>();
				return value != null && value.Relics.OfType<AssortedMeatSauceNoodles>().Any();
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
	[HarmonyPatch]
	public static class NoodlesKeepOptionPatch
	{
		private static MethodBase TargetMethod()
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(RestSiteSynchronizer), "ChooseOption", (Type[])null, (Type[])null);
			if (methodInfo == null)
			{
				return null;
			}
			return AccessTools.AsyncMoveNext((MethodBase)methodInfo) ?? methodInfo;
		}

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Expected O, but got Unknown
			MethodInfo methodInfo = AccessTools.Method(typeof(List<RestSiteOption>), "RemoveAt", new Type[1] { typeof(int) }, (Type[])null);
			MethodInfo methodInfo2 = AccessTools.Method(typeof(NoodlesRestSiteUses), "MaybeRemoveAt", (Type[])null, (Type[])null);
			if (methodInfo == null || methodInfo2 == null)
			{
				return instructions;
			}
			List<CodeInstruction> list = new List<CodeInstruction>();
			bool flag = false;
			foreach (CodeInstruction instruction in instructions)
			{
				if (!flag && CodeInstructionExtensions.Calls(instruction, methodInfo))
				{
					list.Add(new CodeInstruction(OpCodes.Call, (object)methodInfo2));
					flag = true;
				}
				else
				{
					list.Add(instruction);
				}
			}
			Log.Info($"[wuwancients] noodles keep-option patch applied={flag} on {typeof(RestSiteSynchronizer).Name}.ChooseOption", 2);
			return list;
		}
	}
	[HarmonyPatch(typeof(RestSiteSynchronizer), "BeginRestSite")]
	public static class NoodlesBeginRestSitePatch
	{
		public static void Prefix()
		{
			NoodlesRestSiteUses.Reset();
		}
	}
	public sealed class AugustaDumpling : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/augusta_dumpling.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/augusta_dumpling.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/augusta_dumpling.png";

		public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
		{
			PowerModel power2 = power;
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null && power2.Owner == ((RelicModel)this).Owner.Creature && !(amount <= 0m) && (int)power2.TypeForCurrentAmount == 1)
			{
				List<PowerModel> negatives = (from p in ((RelicModel)this).Owner.Creature.Powers
					where p != power2 && (decimal)p.Amount > 0m && (int)p.TypeForCurrentAmount == 2
					orderby ((AbstractModel)p).Id
					select p).ToList();
				if (negatives.Count != 0)
				{
					Rng rng = ((RelicModel)this).Owner.PlayerRng.Rewards;
					PowerModel chosen = negatives[rng.NextInt(negatives.Count)];
					await PowerCmd.ModifyAmount(choiceContext, chosen, -1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
					((RelicModel)this).Flash();
				}
			}
		}
	}
	public sealed class AuthorityOfThunderAndCrown : RelicModel
	{
		public const string ForgeAlternativeKey = "FORGE";

		private const string ForgesKey = "Forges";

		private const int RewardOptionCount = 3;

		private bool _isActivating;

		private int _rewardsForged;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool ShowCounter => true;

		public override string PackedIconPath => "res://wuwancients/images/relics/authority_of_thunder_and_crown.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/authority_of_thunder_and_crown.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/authority_of_thunder_and_crown.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("Forges", 2m)
		};

		public override int DisplayAmount => _isActivating ? ((RelicModel)this).DynamicVars["Forges"].IntValue : (_rewardsForged % ((RelicModel)this).DynamicVars["Forges"].IntValue);

		[SavedProperty]
		public int RewardsForged
		{
			get
			{
				return _rewardsForged;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_rewardsForged = value;
				((RelicModel)this).InvokeDisplayAmountChanged();
			}
		}

		private bool IsActivating
		{
			get
			{
				return _isActivating;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_isActivating = value;
				((RelicModel)this).InvokeDisplayAmountChanged();
			}
		}

		public override bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
		{
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Expected O, but got Unknown
			if (((RelicModel)this).Owner != player)
			{
				return false;
			}
			alternatives.Add(new CardRewardAlternative("FORGE", (Func<Task>)OnForgeSelected, (PostAlternateCardRewardAction)2));
			return true;
		}

		private async Task OnForgeSelected()
		{
			RewardsForged++;
			((RelicModel)this).Flash();
			if (((RelicModel)this).Owner != null && RewardsForged % ((RelicModel)this).DynamicVars["Forges"].IntValue == 0)
			{
				TaskHelper.RunSafely(DoActivateVisuals());
				await GrantRareCardReward();
			}
		}

		private async Task GrantRareCardReward()
		{
			CardCreationOptions options = new CardCreationOptions((IEnumerable<CardPoolModel>)(object)new CardPoolModel[1] { ((RelicModel)this).Owner.Character.CardPool }, (CardCreationSource)3, (CardRarityOddsType)5, (Func<CardModel, bool>)((CardModel card) => (int)card.Rarity == 4));
			List<Reward> rewards = new List<Reward> { (Reward)new CardReward(options, 3, ((RelicModel)this).Owner, (PlayerChoiceSynchronizer)null) };
			await RewardsCmd.OfferCustom(((RelicModel)this).Owner, rewards);
		}

		private async Task DoActivateVisuals()
		{
			IsActivating = true;
			await Cmd.Wait(1f, false);
			IsActivating = false;
		}
	}
	public sealed class AutumnWaterGreeting : RelicModel
	{
		private const string _relicsKey = "Relics";

		private const string _combatsKey = "Combats";

		private bool _isActivating;

		private int _combatsSeen;

		private string _waxRelicKeys = string.Empty;

		public static LocString WaxRelicPrefix => new LocString("relics", "AUTUMN_WATER_GREETING.waxRelicPrefix");

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override bool IsUsedUp => CombatsSeen >= ((RelicModel)this).DynamicVars["Combats"].IntValue * ((RelicModel)this).DynamicVars["Relics"].IntValue;

		public override bool ShowCounter => !((RelicModel)this).IsUsedUp;

		public override int DisplayAmount
		{
			get
			{
				if (!IsActivating)
				{
					return CombatsSeen % ((RelicModel)this).DynamicVars["Combats"].IntValue;
				}
				return ((RelicModel)this).DynamicVars["Combats"].IntValue;
			}
		}

		private bool IsActivating
		{
			get
			{
				return _isActivating;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_isActivating = value;
				((RelicModel)this).InvokeDisplayAmountChanged();
			}
		}

		[SavedProperty]
		public int CombatsSeen
		{
			get
			{
				return _combatsSeen;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_combatsSeen = value;
				((RelicModel)this).InvokeDisplayAmountChanged();
				if (((RelicModel)this).IsUsedUp)
				{
					((RelicModel)this).Status = (RelicStatus)2;
				}
			}
		}

		[SavedProperty]
		public string WaxRelicKeys
		{
			get
			{
				return _waxRelicKeys;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_waxRelicKeys = value ?? string.Empty;
			}
		}

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			new DynamicVar("Relics", 3m),
			new DynamicVar("Combats", 2m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/autumn_water_greeting.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/autumn_water_greeting.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/autumn_water_greeting.png";

		public override async Task AfterObtained()
		{
			List<Reward> rewards = new List<Reward>();
			for (int i = 0; i < ((RelicModel)this).DynamicVars["Relics"].IntValue; i++)
			{
				RelicModel relic = RelicFactory.PullNextRelicFromFront(((RelicModel)this).Owner).ToMutable();
				relic.IsWax = true;
				WaxRelicKeys = WaxOriginTracker.Register(WaxRelicKeys, relic);
				rewards.Add((Reward)new RelicReward(relic, ((RelicModel)this).Owner));
			}
			await RewardsCmd.OfferCustom(((RelicModel)this).Owner, rewards);
		}

		public override async Task AfterCombatEnd(CombatRoom _)
		{
			CombatsSeen++;
			if (CombatsSeen % ((RelicModel)this).DynamicVars["Combats"].IntValue == 0)
			{
				await DoActivateVisuals();
				RelicModel waxRelic = GetOwnWaxRelic();
				if (waxRelic != null)
				{
					await RelicCmd.Melt(waxRelic);
					WaxRelicKeys = WaxOriginTracker.Unregister(WaxRelicKeys, waxRelic);
					await Cmd.CustomScaledWait(0.5f, 0.75f, false, default(CancellationToken));
				}
			}
		}

		private RelicModel? GetOwnWaxRelic()
		{
			if (((RelicModel)this).Owner == null)
			{
				return null;
			}
			foreach (RelicModel relic in ((RelicModel)this).Owner.Relics)
			{
				if (relic == null || !relic.IsWax || relic.IsMelted || !WaxOriginTracker.Owns(WaxRelicKeys, relic))
				{
					continue;
				}
				return relic;
			}
			return null;
		}

		private async Task DoActivateVisuals()
		{
			IsActivating = true;
			((RelicModel)this).Flash();
			await Cmd.Wait(1f, false);
			IsActivating = false;
		}

		public override Task BeforeCombatStart()
		{
			return Task.CompletedTask;
		}
	}
	public sealed class BalorsEye : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/balors_eye.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/balors_eye.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/balors_eye.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
		{
			HoverTipFactory.FromPower<StrengthPower>((int?)null),
			HoverTipFactory.FromPower<DexterityPower>((int?)null)
		};

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			int num;
			if (player == ((RelicModel)this).Owner)
			{
				Player owner = ((RelicModel)this).Owner;
				num = ((((owner != null) ? owner.Creature : null) == null) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
			if (num != 0)
			{
				return;
			}
			ICombatState state = ((RelicModel)this).Owner.Creature.CombatState;
			if (state == null)
			{
				return;
			}
			List<Creature> enemies = (from c in state.GetCreaturesOnSide((CombatSide)2)
				where c.IsAlive && c.Monster != null
				select c).ToList();
			if (enemies.Count != 0)
			{
				int attacking = enemies.Count((Creature c) => c.Monster.IntendsToAttack);
				int others = enemies.Count - attacking;
				if (attacking > 0)
				{
					((RelicModel)this).Flash();
					await PowerCmd.Apply<StrengthPower>(choiceContext, ((RelicModel)this).Owner.Creature, (decimal)attacking, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
				if (others > 0)
				{
					((RelicModel)this).Flash();
					await PowerCmd.Apply<DexterityPower>(choiceContext, ((RelicModel)this).Owner.Creature, (decimal)others, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
			}
		}
	}
	public sealed class BoZaiDoll : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool ShowCounter => false;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new CardsVar(3) };

		public override string PackedIconPath => "res://wuwancients/images/relics/bo_zai_doll.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/bo_zai_doll.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/bo_zai_doll.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				List<Reward> rewards = new List<Reward>();
				RelicModel commonRelic = RelicFactory.PullNextRelicFromFront(((RelicModel)this).Owner, (RelicRarity)2).ToMutable();
				rewards.Add((Reward)new RelicReward(commonRelic, ((RelicModel)this).Owner));
				RelicModel uncommonRelic = RelicFactory.PullNextRelicFromFront(((RelicModel)this).Owner, (RelicRarity)3).ToMutable();
				rewards.Add((Reward)new RelicReward(uncommonRelic, ((RelicModel)this).Owner));
				RelicModel rareRelic = RelicFactory.PullNextRelicFromFront(((RelicModel)this).Owner, (RelicRarity)4).ToMutable();
				rewards.Add((Reward)new RelicReward(rareRelic, ((RelicModel)this).Owner));
				await RewardsCmd.OfferCustom(((RelicModel)this).Owner, rewards);
			}
		}
	}
	public sealed class BrightBlood : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)1;

		public override string PackedIconPath => "res://wuwancients/images/relics/bright_blood.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/bright_blood.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/bright_blood.png";

		public override async Task AfterCombatEnd(CombatRoom combatRoom)
		{
			if (((RelicModel)this).Owner != null && ((RelicModel)this).Owner.Creature != null)
			{
				await CreatureCmd.GainMaxHp(((RelicModel)this).Owner.Creature, 6m);
				((RelicModel)this).Flash();
			}
		}
	}
	public sealed class BrokenRebirth : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override string PackedIconPath => "res://wuwancients/images/relics/broken_rebirth.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/broken_rebirth.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/broken_rebirth.png";

		public override async Task AfterObtained()
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				await CreatureCmd.GainMaxHp(((RelicModel)this).Owner.Creature, 18m);
			}
		}

		public override async Task AfterCombatEnd(CombatRoom room)
		{
			if ((int)((AbstractRoom)room).RoomType == 2)
			{
				Player owner = ((RelicModel)this).Owner;
				if (((owner != null) ? owner.Creature : null) != null)
				{
					await CreatureCmd.GainMaxHp(((RelicModel)this).Owner.Creature, 9m);
					int healAmount = (int)((decimal)((RelicModel)this).Owner.Creature.MaxHp * 0.25m);
					await CreatureCmd.Heal(((RelicModel)this).Owner.Creature, (decimal)healAmount, true);
				}
			}
		}
	}
	public sealed class BurySpiritGreeting : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool ShowCounter => false;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("HealAmount", 10m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/bury_spirit_greeting.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/bury_spirit_greeting.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/bury_spirit_greeting.png";

		public override async Task AfterRestSiteSmith(Player player)
		{
			if (player == ((RelicModel)this).Owner && ((RelicModel)this).Owner != null)
			{
				await CreatureCmd.Heal(((RelicModel)this).Owner.Creature, ((RelicModel)this).DynamicVars["HealAmount"].BaseValue, true);
			}
		}

		public override Task BeforeCombatStart()
		{
			return Task.CompletedTask;
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			return Task.CompletedTask;
		}
	}
	public sealed class ButterflyPrint : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new CardsVar(1) };

		public override string PackedIconPath => "res://wuwancients/images/relics/butterfly_print.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/butterfly_print.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/butterfly_print.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			CardCreationOptions options = new CardCreationOptions((IEnumerable<CardPoolModel>)(object)new CardPoolModel[1] { ((RelicModel)this).Owner.Character.CardPool }, (CardCreationSource)3, (CardRarityOddsType)5, (Func<CardModel, bool>)((CardModel c) => (int)c.Rarity == 4));
			IEnumerable<CardCreationResult> rewardResults = CardFactory.CreateForReward(((RelicModel)this).Owner, 3, options);
			List<CardModel> rareCards = rewardResults.Select((CardCreationResult r) => r.Card).ToList();
			if (rareCards.Count == 0)
			{
				return;
			}
			foreach (CardModel card in rareCards)
			{
				if (card != null)
				{
					CardCmd.Upgrade(card, (CardPreviewStyle)1);
				}
			}
			CardModel chosenCard = await CardSelectCmd.FromChooseACardScreen((PlayerChoiceContext)new BlockingPlayerChoiceContext(), (IReadOnlyList<CardModel>)rareCards, ((RelicModel)this).Owner, false);
			if (chosenCard != null)
			{
				CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(chosenCard, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
			}
			foreach (CardModel item in rareCards)
			{
				if (item != chosenCard)
				{
					MapPointHistoryEntry currentMapPointHistoryEntry = ((RelicModel)this).Owner.RunState.CurrentMapPointHistoryEntry;
					if (currentMapPointHistoryEntry != null)
					{
						currentMapPointHistoryEntry.GetEntry(((RelicModel)this).Owner.NetId).CardChoices.Add(new CardChoiceHistoryEntry(item, false));
					}
				}
			}
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			return Task.CompletedTask;
		}

		public override Task BeforeCombatStart()
		{
			return Task.CompletedTask;
		}
	}
	public sealed class CactusHugPillow : RelicModel
	{
		private const int ThornsOnStart = 1;

		private const int DexterityOnStart = 4;

		private const int ThornsPerFullBlock = 1;

		private bool _grantedThisCombat;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/cactus_hug_pillow.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/cactus_hug_pillow.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/cactus_hug_pillow.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
		{
			HoverTipFactory.FromPower<ThornsPower>((int?)null),
			HoverTipFactory.FromPower<DexterityPower>((int?)null)
		};

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null && player == ((RelicModel)this).Owner && !_grantedThisCombat)
			{
				_grantedThisCombat = true;
				((RelicModel)this).Flash();
				await PowerCmd.Apply<ThornsPower>(choiceContext, ((RelicModel)this).Owner.Creature, 1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				await PowerCmd.Apply<DexterityPower>(choiceContext, ((RelicModel)this).Owner.Creature, 4m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
			}
		}

		public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null && target == ((RelicModel)this).Owner.Creature && result.WasFullyBlocked && result.BlockedDamage > 0)
			{
				((RelicModel)this).Flash();
				await PowerCmd.Apply<ThornsPower>(choiceContext, ((RelicModel)this).Owner.Creature, 1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
			}
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			_grantedThisCombat = false;
			return Task.CompletedTask;
		}
	}
	public sealed class CamelliaGreeting : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new CardsVar(2) };

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			get
			{
				List<IHoverTip> list = new List<IHoverTip> { HoverTipFactory.Static((StaticHoverTip)4, Array.Empty<DynamicVar>()) };
				list.AddRange(HoverTipFactory.FromCardWithCardHoverTips<Bloom>(false));
				return list;
			}
		}

		public override string PackedIconPath => "res://wuwancients/images/relics/camellia_greeting.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/camellia_greeting.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/camellia_greeting.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			CardSelectorPrefs prefs = new CardSelectorPrefs(new LocString("relics", "CAMELLIA_GREETING_SELECT"), 2, 2);
			List<CardModel> selectedCards = (await CardSelectCmd.FromDeckForTransformation(((RelicModel)this).Owner, prefs, (Func<CardModel, CardTransformation>)null)).ToList();
			if (selectedCards.Count == 0)
			{
				return;
			}
			foreach (CardModel card in selectedCards)
			{
				await CardCmd.TransformToRandom(card, ((RelicModel)this).Owner.RunState.Rng.Niche, (CardPreviewStyle)1);
			}
			CardModel bloom = (CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<Bloom>(((RelicModel)this).Owner);
			await CardPileCmd.Add(bloom, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}
	public sealed class ChimerasHeart : RelicModel
	{
		private int _playerTurnsThisCombat;

		private bool _soarGranted;

		private Creature? _kaiserTarget;

		private bool _kuangshaSpawned;

		private readonly Dictionary<Creature, int> _hitCounts = new Dictionary<Creature, int>();

		private bool _isMyTurn;

		private bool _playedCardThisTurn;

		private bool _tookDamageThisTurn;

		private bool _louseUsed;

		private bool _slumberActive;

		[SavedProperty]
		private int[] UnlockedHashes { get; set; } = Array.Empty<int>();


		[SavedProperty]
		private int DecimillipedeLives { get; set; } = 3;


		[SavedProperty]
		private int TestSubjectLives { get; set; } = 3;


		[SavedProperty]
		private int AxebotLives { get; set; } = 1;


		[SavedProperty]
		private bool OvicopterUsed { get; set; }

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/chimeras_heart.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/chimeras_heart.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/chimeras_heart.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Overlimit>(false);

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new StringVar("DemonForces", BuildForcesText()) };

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				RefreshDescription();
				CardModel overlimit = (CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<Overlimit>(((RelicModel)this).Owner);
				CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(overlimit, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
			}
		}

		public static ChimerasHeart? Of(Player? player)
		{
			return (player != null) ? player.GetRelic<ChimerasHeart>() : null;
		}

		public bool HasForce(DemonForceId id)
		{
			return OverlimitUnlocks.UnlockedForces(UnlockedHashes).Any((DemonForce f) => f.Id == id);
		}

		public int RemainingLives(DemonForceId id)
		{
			if (1 == 0)
			{
			}
			int result = id switch
			{
				DemonForceId.Decimillipede => DecimillipedeLives, 
				DemonForceId.TestSubject => TestSubjectLives, 
				DemonForceId.Axebot => AxebotLives, 
				_ => 0, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		public void SpendLife(DemonForceId id)
		{
			switch (id)
			{
			case DemonForceId.Decimillipede:
				DecimillipedeLives = Math.Max(0, DecimillipedeLives - 1);
				break;
			case DemonForceId.TestSubject:
				TestSubjectLives = Math.Max(0, TestSubjectLives - 1);
				break;
			case DemonForceId.Axebot:
				AxebotLives = Math.Max(0, AxebotLives - 1);
				break;
			}
		}

		public DemonForce? UnlockFromMonster(string monsterEntry)
		{
			DemonForce demonForce = OverlimitUnlocks.ForceForEntry(monsterEntry);
			if (demonForce == null)
			{
				return null;
			}
			int value = OverlimitUnlocks.HashOf(monsterEntry);
			if (Array.IndexOf(UnlockedHashes, value) >= 0)
			{
				return null;
			}
			bool flag = HasForce(demonForce.Id);
			UnlockedHashes = OverlimitUnlocks.WithMonster(UnlockedHashes, monsterEntry);
			RefreshDescription();
			return flag ? null : demonForce;
		}

		public void RefreshDescription()
		{
			DynamicVar obj = ((RelicModel)this).DynamicVars["DemonForces"];
			StringVar val = (StringVar)(object)((obj is StringVar) ? obj : null);
			if (val != null)
			{
				val.StringValue = BuildForcesText();
			}
		}

		private string BuildForcesText()
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Expected O, but got Unknown
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Expected O, but got Unknown
			LocString val = new LocString("relics", ((AbstractModel)this).Id.Entry + ".demonLine");
			LocString val2 = new LocString("relics", ((AbstractModel)this).Id.Entry + ".demonMore");
			return OverlimitUnlocks.BuildDescription(UnlockedHashes, val.Exists() ? val.GetRawText() : "{0}: {1}", val2.Exists() ? val2.GetRawText() : "...{0}");
		}

		public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
		{
			if (wasRemovalPrevented || ((RelicModel)this).Owner == null)
			{
				return;
			}
			if (creature.IsPlayer && creature.Player != ((RelicModel)this).Owner)
			{
				await TryOvicopterRevive(choiceContext, creature);
			}
			else if (creature.Monster != null && OverlimitKillCredit.Consume(((RelicModel)this).Owner, creature))
			{
				DemonForce unlocked = UnlockFromMonster(creature.ModelId.Entry);
				if (unlocked != null)
				{
					DemonForceRuntime.AnnounceUnlock(((RelicModel)this).Owner, unlocked);
				}
				await TryMagiKnightUpgrade(choiceContext);
			}
		}

		public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			if (HasForce(DemonForceId.TheObscura))
			{
				int num;
				if (((cardPlay != null) ? cardPlay.Card : null) != null)
				{
					Player owner = ((RelicModel)this).Owner;
					num = ((((owner != null) ? owner.Creature : null) == null) ? 1 : 0);
				}
				else
				{
					num = 1;
				}
				if (num == 0 && cardPlay.Card.Owner == ((RelicModel)this).Owner && (int)cardPlay.Card.Type == 2)
				{
					await CreatureCmd.GainBlock(((RelicModel)this).Owner.Creature, 1m, (ValueProp)8, cardPlay, false);
				}
			}
		}

		public override async Task BeforeCombatStart()
		{
			_playerTurnsThisCombat = 0;
			_soarGranted = false;
			_kuangshaSpawned = false;
			Player owner = ((RelicModel)this).Owner;
			Creature me = ((owner != null) ? owner.Creature : null);
			if (me == null)
			{
				return;
			}
			BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
			if (HasForce(DemonForceId.SpinyToad))
			{
				await PowerCmd.Apply<ThornsPower>((PlayerChoiceContext)(object)ctx, me, 5m, me, (CardModel)null, false);
			}
			if (HasForce(DemonForceId.FlailKnight))
			{
				await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)(object)ctx, me, 2m, me, (CardModel)null, false);
			}
			if (HasForce(DemonForceId.DevotedSculptor))
			{
				await PowerCmd.Apply<RitualPower>((PlayerChoiceContext)(object)ctx, me, 3m, me, (CardModel)null, false);
			}
			if (HasForce(DemonForceId.FrogKnight))
			{
				await PowerCmd.Apply<PlatingPower>((PlayerChoiceContext)(object)ctx, me, 15m, me, (CardModel)null, false);
			}
			if (HasForce(DemonForceId.ConstructMenagerie))
			{
				await PowerCmd.Apply<ArtifactPower>((PlayerChoiceContext)(object)ctx, me, 2m, me, (CardModel)null, false);
			}
			if (HasForce(DemonForceId.SoulNexus))
			{
				ICombatState combatState = me.CombatState;
				foreach (Creature enemy in ((combatState != null) ? combatState.HittableEnemies : null) ?? Array.Empty<Creature>())
				{
					await CreatureCmd.Damage((PlayerChoiceContext)(object)ctx, enemy, 31m, (ValueProp)4, (CardModel)null, (CardPlay)null);
				}
			}
			_kaiserTarget = null;
			if (HasForce(DemonForceId.KaiserCrab))
			{
				ICombatState combatState2 = me.CombatState;
				List<Creature> pool = (((combatState2 != null) ? combatState2.HittableEnemies : null) ?? Array.Empty<Creature>()).ToList();
				if (pool.Count > 0)
				{
					ChimerasHeart chimerasHeart = this;
					Player owner2 = ((RelicModel)this).Owner;
					chimerasHeart._kaiserTarget = SyncedRng.Pick(SyncedRng.Targets((owner2 != null) ? owner2.RunState : null), pool);
				}
			}
		}

		public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if ((int)side != 1)
			{
				return;
			}
			_playerTurnsThisCombat++;
			Player owner = ((RelicModel)this).Owner;
			Creature me = ((owner != null) ? owner.Creature : null);
			if (me == null)
			{
				return;
			}
			BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
			if (_playerTurnsThisCombat == 3 && HasForce(DemonForceId.BowlbugNectar))
			{
				await PowerCmd.Apply<BowlbugNectarPower>((PlayerChoiceContext)(object)ctx, me, 15m, me, (CardModel)null, false);
			}
			if (HasForce(DemonForceId.OwlMagistrate))
			{
				if (_playerTurnsThisCombat == 3 && !_soarGranted)
				{
					_soarGranted = true;
					await PowerCmd.Apply<SoarPower>((PlayerChoiceContext)(object)ctx, me, 1m, me, (CardModel)null, false);
				}
				else if (_playerTurnsThisCombat == 4 && _soarGranted)
				{
					_soarGranted = false;
					await PowerCmd.Remove<SoarPower>(me);
				}
			}
		}

		public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			if ((int)side != 2 || !HasForce(DemonForceId.KnowledgeDemon))
			{
				return;
			}
			Player owner = ((RelicModel)this).Owner;
			Creature me = ((owner != null) ? owner.Creature : null);
			if (me == null)
			{
				return;
			}
			ICombatState combatState = me.CombatState;
			List<Creature> enemies = (((combatState != null) ? combatState.HittableEnemies : null) ?? Array.Empty<Creature>()).Where((Creature c) => c.IsAlive).ToList();
			if (enemies.Count != 0)
			{
				Player owner2 = ((RelicModel)this).Owner;
				Creature victim = SyncedRng.Pick(SyncedRng.Targets((owner2 != null) ? owner2.RunState : null), enemies);
				if (victim != null)
				{
					await CreatureCmd.Damage(choiceContext, victim, 15m, (ValueProp)4, (CardModel)null, (CardPlay)null);
				}
			}
		}

		public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			int num;
			if (dealer != null)
			{
				Player owner = ((RelicModel)this).Owner;
				if (((owner != null) ? owner.Creature : null) != null)
				{
					num = ((dealer != ((RelicModel)this).Owner.Creature) ? 1 : 0);
					goto IL_005c;
				}
			}
			num = 1;
			goto IL_005c;
			IL_005c:
			if (num != 0 || target.Monster == null)
			{
				return;
			}
			if (HasForce(DemonForceId.Aeonglass))
			{
				int i;
				int hits = (_hitCounts.TryGetValue(target, out i) ? i : 0) + 1;
				if (hits >= 6)
				{
					_hitCounts[target] = 0;
					await PowerCmd.Apply<DemisePower>(choiceContext, target, 6m, dealer, (CardModel)null, false);
				}
				else
				{
					_hitCounts[target] = hits;
				}
			}
			if (HasForce(DemonForceId.HunterKiller) && cardSource != null && (int)cardSource.Type == 1 && target.IsAlive)
			{
				await PowerCmd.Apply<StrengthPower>(choiceContext, target, -1m, dealer, (CardModel)null, false);
			}
		}

		public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			if (HasForce(DemonForceId.InfestedPrism))
			{
				Player owner = ((RelicModel)this).Owner;
				if (((owner != null) ? owner.Creature : null) != null && target == ((RelicModel)this).Owner.Creature && dealer != null && dealer.Monster != null)
				{
					await PowerCmd.Apply<VigorPower>(choiceContext, target, 6m, dealer, (CardModel)null, false);
				}
			}
		}

		private async Task TryMagiKnightUpgrade(PlayerChoiceContext choiceContext)
		{
			if (!HasForce(DemonForceId.MagiKnight) || ((RelicModel)this).Owner == null)
			{
				return;
			}
			List<CardModel> candidates = ((RelicModel)this).Owner.Deck.Cards.Where((CardModel c) => c != null && !c.IsUpgraded).ToList();
			if (candidates.Count != 0)
			{
				Player owner = ((RelicModel)this).Owner;
				CardModel pick = SyncedRng.Pick(SyncedRng.Cards((owner != null) ? owner.RunState : null), candidates);
				if (pick != null)
				{
					CardCmd.Upgrade(pick, (CardPreviewStyle)1);
				}
			}
		}

		public override decimal ModifyDamageCap(Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
		{
			if (!HasForce(DemonForceId.Exoskeleton))
			{
				return decimal.MaxValue;
			}
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null || target == null)
			{
				return decimal.MaxValue;
			}
			if (target != ((RelicModel)this).Owner.Creature)
			{
				return decimal.MaxValue;
			}
			return 18m;
		}

		public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
		{
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Invalid comparison between Unknown and I4
			if (!HasForce(DemonForceId.Entomancer))
			{
				return 0m;
			}
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null || dealer == null || cardSource == null)
			{
				return 0m;
			}
			if (dealer != ((RelicModel)this).Owner.Creature)
			{
				return 0m;
			}
			if ((int)cardSource.Type != 1)
			{
				return 0m;
			}
			StrengthPower power = ((RelicModel)this).Owner.Creature.GetPower<StrengthPower>();
			return ((decimal?)((power != null) ? new int?(((PowerModel)power).Amount) : null)) ?? 0m;
		}

		public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
		{
			if (!HasForce(DemonForceId.KaiserCrab))
			{
				return 1m;
			}
			if (_kaiserTarget == null || target == null)
			{
				return 1m;
			}
			if (target != _kaiserTarget)
			{
				return 1m;
			}
			return 1.5m;
		}

		public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
		{
			if (HasForce(DemonForceId.GlobeHead) && power != null && !(amount <= 0m) && power != null && (int)power.Type == 1)
			{
				Creature victim = power.Owner;
				if (victim != null && victim.Monster != null && victim.IsAlive)
				{
					await CreatureCmd.Damage(choiceContext, victim, 8m, (ValueProp)4, (CardModel)null, (CardPlay)null);
				}
			}
		}

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			Player player2 = player;
			if (((RelicModel)this).Owner == null || player2 != ((RelicModel)this).Owner)
			{
				return;
			}
			if (HasForce(DemonForceId.TheInsatiable) && !_kuangshaSpawned)
			{
				_kuangshaSpawned = true;
				for (int i = 0; i < 1; i++)
				{
					CardModel kuangsha = (CardModel)(object)((RelicModel)this).Owner.Creature.CombatState.CreateCard<Kuangsha>(((RelicModel)this).Owner);
					await CardPileCmd.AddGeneratedCardToCombat(kuangsha, (PileType)1, ((RelicModel)this).Owner, (CardPilePosition)3);
				}
			}
			if (!HasForce(DemonForceId.TurretOperator))
			{
				return;
			}
			Creature creature = player2.Creature;
			object obj;
			if (creature == null)
			{
				obj = null;
			}
			else
			{
				ICombatState combatState = creature.CombatState;
				obj = ((combatState != null) ? combatState.Players : null);
			}
			if (obj == null)
			{
				obj = new List<Player>();
			}
			List<Player> allies = ((IEnumerable<Player>)obj).Where((Player p) => p != null && p != player2 && p.Creature != null && p.Creature.IsAlive).ToList();
			if (allies.Count != 0)
			{
				Player owner = ((RelicModel)this).Owner;
				Player pick = SyncedRng.Pick(SyncedRng.Targets((owner != null) ? owner.RunState : null), allies);
				if (((pick != null) ? pick.Creature : null) != null)
				{
					await CreatureCmd.GainBlock(pick.Creature, 25m, (ValueProp)8, (CardPlay)null, false);
				}
			}
		}

		public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			if ((int)side != 1)
			{
				return;
			}
			Player player = ((RelicModel)this).Owner;
			if (player == null)
			{
				return;
			}
			bool ghost = HasForce(DemonForceId.SpectralKnight);
			bool queen = HasForce(DemonForceId.Queen);
			if (ghost || queen)
			{
				if (ghost)
				{
					await MarkHandCards(choiceContext, player, 3, (CardKeyword)2, "CHIMERAS_HEART.selectEthereal");
				}
				if (queen)
				{
					await MarkHandCards(choiceContext, player, 3, (CardKeyword)5, "CHIMERAS_HEART.selectRetain");
				}
			}
		}

		private async Task MarkHandCards(PlayerChoiceContext choiceContext, Player player, int count, CardKeyword keyword, string promptKey)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			CardSelectorPrefs val = new CardSelectorPrefs(new LocString("relics", promptKey), 0, count);
			((CardSelectorPrefs)(ref val)).set_Cancelable(true);
			((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
			CardSelectorPrefs prefs = val;
			List<CardModel> hand = PileTypeExtensions.GetPile((PileType)2, player).Cards.ToList();
			if (hand.Count == 0)
			{
				return;
			}
			foreach (CardModel card in (await CardSelectCmd.FromSimpleGrid(choiceContext, (IReadOnlyList<CardModel>)hand, player, prefs)) ?? Enumerable.Empty<CardModel>())
			{
				if (card != null)
				{
					card.AddKeyword(keyword);
				}
			}
		}

		private DemonForceId? ReviveForceFor(Creature creature)
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null || creature != ((RelicModel)this).Owner.Creature)
			{
				return null;
			}
			if (HasForce(DemonForceId.Decimillipede) && DecimillipedeLives > 0)
			{
				return DemonForceId.Decimillipede;
			}
			if (HasForce(DemonForceId.TestSubject) && TestSubjectLives > 0)
			{
				return DemonForceId.TestSubject;
			}
			if (HasForce(DemonForceId.Axebot) && AxebotLives > 0)
			{
				return DemonForceId.Axebot;
			}
			return null;
		}

		public override bool ShouldDie(Creature creature)
		{
			if (ReviveForceFor(creature).HasValue)
			{
				return false;
			}
			if (CanOvicopterRevive(creature))
			{
				return false;
			}
			return true;
		}

		private bool CanOvicopterRevive(Creature creature)
		{
			if (OvicopterUsed || !HasForce(DemonForceId.Ovicopter))
			{
				return false;
			}
			if (creature.IsPlayer)
			{
				Player owner = ((RelicModel)this).Owner;
				if (((owner != null) ? owner.Creature : null) != null)
				{
					if (creature == ((RelicModel)this).Owner.Creature)
					{
						return false;
					}
					ICombatState combatState = ((RelicModel)this).Owner.Creature.CombatState;
					if (((combatState != null) ? combatState.Players : null) == null)
					{
						return false;
					}
					if (((RelicModel)this).Owner.Creature.CombatState.Players.Count <= 1)
					{
						return false;
					}
					return true;
				}
			}
			return false;
		}

		private async Task TryOvicopterRevive(PlayerChoiceContext choiceContext, Creature ally)
		{
			if (!CanOvicopterRevive(ally))
			{
				return;
			}
			OvicopterUsed = true;
			await CreatureCmd.Heal(ally, Math.Max(0m, 15m - (decimal)ally.CurrentHp), true);
			foreach (PowerModel power in ally.Powers.Where((PowerModel p) => p != null && (int)p.Type == 2).ToList())
			{
				await PowerCmd.Remove(power);
			}
		}

		public override async Task AfterPreventingDeath(Creature creature)
		{
			DemonForceId? force = ReviveForceFor(creature);
			if (!force.HasValue)
			{
				return;
			}
			SpendLife(force.Value);
			BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
			switch (force.Value)
			{
			case DemonForceId.Decimillipede:
				await CreatureCmd.Heal(creature, Math.Max(0m, 25m - (decimal)creature.CurrentHp), true);
				break;
			case DemonForceId.TestSubject:
				await CreatureCmd.Heal(creature, Math.Max(0m, creature.MaxHp - creature.CurrentHp), true);
				await PowerCmd.Apply<IntangiblePower>((PlayerChoiceContext)(object)ctx, creature, 1m, creature, (CardModel)null, false);
				break;
			case DemonForceId.Axebot:
			{
				Player owner = ((RelicModel)this).Owner;
				object obj;
				if (owner == null)
				{
					obj = null;
				}
				else
				{
					PlayerCombatState playerCombatState = owner.PlayerCombatState;
					obj = ((playerCombatState != null) ? playerCombatState.DiscardPile : null);
				}
				if (obj != null)
				{
					foreach (CardModel card in ((RelicModel)this).Owner.PlayerCombatState.DiscardPile.Cards.ToList())
					{
						if (card != null)
						{
							await CardCmd.Exhaust((PlayerChoiceContext)(object)ctx, card, false, false);
						}
					}
				}
				await CreatureCmd.Heal(creature, Math.Max(0m, creature.MaxHp - creature.CurrentHp), true);
				break;
			}
			}
		}

		public override async Task AfterAutoPrePlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
		{
			if (((RelicModel)this).Owner == null || player != ((RelicModel)this).Owner)
			{
				return;
			}
			_isMyTurn = true;
			_playedCardThisTurn = false;
			_tookDamageThisTurn = false;
			Creature me = ((RelicModel)this).Owner.Creature;
			if (me == null)
			{
				return;
			}
			if (_slumberActive && HasForce(DemonForceId.SlumberingBeetle))
			{
				await PowerCmd.Apply<StrengthPower>(choiceContext, me, 2m, me, (CardModel)null, false);
			}
			if (HasForce(DemonForceId.BowlbugEgg))
			{
				ICombatState combatState = me.CombatState;
				List<Creature> enemies = (((combatState != null) ? combatState.HittableEnemies : null) ?? Array.Empty<Creature>()).Where((Creature c) => c.IsAlive).ToList();
				if (enemies.Count > 0)
				{
					Player owner = ((RelicModel)this).Owner;
					Creature victim = SyncedRng.Pick(SyncedRng.Targets((owner != null) ? owner.RunState : null), enemies);
					if (victim != null)
					{
						await CreatureCmd.Damage(choiceContext, victim, 5m, (ValueProp)4, (CardModel)null, (CardPlay)null);
					}
				}
				await CreatureCmd.GainBlock(me, 5m, (ValueProp)8, (CardPlay)null, false);
			}
			if (!HasForce(DemonForceId.Fabricator))
			{
				return;
			}
			ICombatState combatState2 = me.CombatState;
			foreach (Player ally in (((combatState2 != null) ? combatState2.Players : null) ?? new List<Player>()).Where((Player p) => p != null && p != ((RelicModel)this).Owner && p.Creature != null && p.Creature.IsAlive))
			{
				PlayerCmd.GainEnergy(1m, ally);
			}
		}

		public override Task AfterSideTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Invalid comparison between Unknown and I4
			if ((int)side != 1)
			{
				return Task.CompletedTask;
			}
			_isMyTurn = false;
			if (!_playedCardThisTurn && _tookDamageThisTurn)
			{
				_slumberActive = true;
			}
			return Task.CompletedTask;
		}

		public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			if (((cardPlay != null) ? cardPlay.Card : null) != null && ((RelicModel)this).Owner != null && cardPlay.Card.Owner == ((RelicModel)this).Owner)
			{
				_playedCardThisTurn = true;
			}
			return Task.CompletedTask;
		}

		public override async Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null && target == ((RelicModel)this).Owner.Creature)
			{
				_tookDamageThisTurn = true;
				if (!_isMyTurn && !_louseUsed && HasForce(DemonForceId.LouseProgenitor) && result.TotalDamage > 0)
				{
					_louseUsed = true;
					await CreatureCmd.GainBlock(target, 16m, (ValueProp)8, (CardPlay)null, false);
				}
			}
		}
	}
	[HarmonyPatch(typeof(RelicModel), "FromSerializable")]
	public static class ChimerasHeartDescriptionRefresh
	{
		[HarmonyPostfix]
		public static void Postfix(RelicModel __result)
		{
			if (__result is ChimerasHeart chimerasHeart)
			{
				chimerasHeart.RefreshDescription();
			}
		}
	}
	public sealed class ChisaDango : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/chisa_dango.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/chisa_dango.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/chisa_dango.png";

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return;
			}
			Creature creature = ((RelicModel)this).Owner.Creature;
			if (((creature != null) ? creature.CombatState : null) == null)
			{
				return;
			}
			ICombatState combatState = ((RelicModel)this).Owner.Creature.CombatState;
			List<Creature> all = combatState.Creatures.Where((Creature c) => !c.IsPet).ToList();
			if (all.Count != 0)
			{
				decimal minHp = all.Min((Creature c) => c.CurrentHp);
				if (!((decimal)((RelicModel)this).Owner.Creature.CurrentHp != minHp))
				{
					PotionModel potion = PotionFactory.CreateRandomPotionInCombat(((RelicModel)this).Owner, ((RelicModel)this).Owner.PlayerRng.Rewards, (IEnumerable<PotionModel>)null).ToMutable();
					await PotionCmd.TryToProcure(potion, ((RelicModel)this).Owner, -1);
				}
			}
		}
	}
	[HarmonyPatch(typeof(NPotionContainer), "GrowPotionHolders")]
	public static class NPotionContainerGrowPatch
	{
		public static void Postfix(NPotionContainer __instance, int newMaxPotionSlots)
		{
			List<NPotionHolder> value = Traverse.Create((object)__instance).Field("_holders").GetValue<List<NPotionHolder>>();
			if (value == null || value.Count <= newMaxPotionSlots)
			{
				return;
			}
			Control value2 = Traverse.Create((object)__instance).Field("_potionHolders").GetValue<Control>();
			while (value.Count > newMaxPotionSlots)
			{
				int index = value.Count - 1;
				NPotionHolder val = value[index];
				value.RemoveAt(index);
				if (value2 != null && val != null)
				{
					((Node)value2).RemoveChild((Node)(object)val);
					((Node)val).QueueFree();
				}
			}
		}
	}
	public sealed class ColorfulCupNoodles : RelicModel
	{
		private const decimal EnergyPerTurn = 1m;

		private const decimal CardsPerTurn = 1m;

		private bool _dexterityLost;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/colorful_cup_noodles.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/colorful_cup_noodles.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/colorful_cup_noodles.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
		{
			HoverTipFactory.ForEnergy((RelicModel)(object)this),
			HoverTipFactory.FromPower<DexterityPower>((int?)null)
		};

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null && player == ((RelicModel)this).Owner)
			{
				if (!_dexterityLost)
				{
					_dexterityLost = true;
					await PowerCmd.Apply<DexterityPower>(choiceContext, ((RelicModel)this).Owner.Creature, -1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
				((RelicModel)this).Flash();
				await PlayerCmd.GainEnergy(1m, ((RelicModel)this).Owner);
				await CardPileCmd.Draw(choiceContext, 1m, ((RelicModel)this).Owner, false);
			}
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			_dexterityLost = false;
			return Task.CompletedTask;
		}
	}
	public sealed class DeadBranch : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)6;

		public override string PackedIconPath => "res://wuwancients/images/relics/dead_branch.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/dead_branch.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/dead_branch.png";

		public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
		{
			if (card.Owner == ((RelicModel)this).Owner)
			{
				((RelicModel)this).Flash();
				Rng rng = ((RelicModel)this).Owner.RunState.Rng.CombatCardGeneration;
				IEnumerable<CardModel> pool = ((RelicModel)this).Owner.Character.CardPool.GetUnlockedCards(((RelicModel)this).Owner.UnlockState, ((RelicModel)this).Owner.RunState.CardMultiplayerConstraint);
				CardModel newCard = CardFactory.GetDistinctForCombat(((RelicModel)this).Owner, pool, 1, rng).First();
				await CardPileCmd.AddGeneratedCardToCombat(newCard, (PileType)2, ((RelicModel)this).Owner, (CardPilePosition)1);
			}
		}
	}
	public sealed class DeathAndLifeMovement : RelicModel
	{
		private class RekindleRestSiteOption : RestSiteOption
		{
			private readonly DeathAndLifeMovement _relic;

			public override string OptionId => "Sustain";

			public override LocString Description
			{
				get
				{
					LocString description = ((RestSiteOption)this).Description;
					description.Add("RelicName", ((RelicModel)_relic).Title);
					return description;
				}
			}

			public RekindleRestSiteOption(Player owner, DeathAndLifeMovement relic)
				: base(owner)
			{
				_relic = relic;
			}

			public override Task<bool> OnSelect()
			{
				_relic.Rekindle();
				return Task.FromResult(result: true);
			}

			public override Task DoLocalPostSelectVfx(CancellationToken ct = default(CancellationToken))
			{
				//IL_0045: Unknown result type (might be due to invalid IL or missing references)
				NRelicFlashVfx val = NRelicFlashVfx.Create((RelicModel)(object)_relic);
				if (val != null)
				{
					NRestSiteRoom instance = NRestSiteRoom.Instance;
					if (instance != null)
					{
						NRestSiteCharacter? obj = ((IEnumerable<NRestSiteCharacter>)instance.Characters).FirstOrDefault((Func<NRestSiteCharacter, bool>)((NRestSiteCharacter c) => c.Player == ((RestSiteOption)this).Owner));
						if (obj != null)
						{
							GodotTreeExtensions.AddChildSafely((Node)(object)obj, (Node)(object)val);
						}
					}
					((Control)val).Position = Vector2.Zero;
				}
				return Task.CompletedTask;
			}

			public override Task DoRemotePostSelectVfx()
			{
				_relic.Rekindle();
				return ((RestSiteOption)this).DoLocalPostSelectVfx(default(CancellationToken));
			}
		}

		private const int _healPercent = 70;

		private bool _wasUsed;

		public override string PackedIconPath => "res://wuwancients/images/relics/death_and_life_movement.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/death_and_life_movement.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/death_and_life_movement.png";

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool IsUsedUp => _wasUsed;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new HealVar(70m) };

		protected override IEnumerable<IHoverTip> ExtraHoverTips => Enumerable.Empty<IHoverTip>();

		[SavedProperty]
		public bool WasUsed
		{
			get
			{
				return _wasUsed;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_wasUsed = value;
				((RelicModel)this).Status = (RelicStatus)(_wasUsed ? 2 : 0);
			}
		}

		public override bool ShouldDieLate(Creature creature)
		{
			if (creature == null)
			{
				return true;
			}
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null)
			{
				return true;
			}
			if (creature != ((RelicModel)this).Owner.Creature)
			{
				return true;
			}
			return WasUsed;
		}

		public override async Task AfterPreventingDeath(Creature creature)
		{
			if (creature != null)
			{
				Player owner = ((RelicModel)this).Owner;
				if (((owner != null) ? owner.Creature : null) != null && creature == ((RelicModel)this).Owner.Creature && !WasUsed)
				{
					((RelicModel)this).Flash();
					WasUsed = true;
					decimal amount = Math.Max(1m, (decimal)creature.MaxHp * 70m / 100m);
					await CreatureCmd.Heal(creature, amount, true);
				}
			}
		}

		public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return false;
			}
			if (!WasUsed)
			{
				return false;
			}
			options.Add((RestSiteOption)(object)new RekindleRestSiteOption(player, this));
			return true;
		}

		public void Rekindle()
		{
			WasUsed = false;
			((RelicModel)this).Flash();
		}
	}
	public sealed class Decode : RelicModel
	{
		private bool _pendingEnergy;

		public override RelicRarity Rarity => (RelicRarity)6;

		public override string PackedIconPath => "res://wuwancients/images/relics/decode.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/decode.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/decode.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new StringVar("BlockTerm", "")
		{
			StringValue = Term("DECODE.termPerfect")
		} };

		private bool HasStarChart
		{
			get
			{
				int result;
				if (((AbstractModel)this).IsMutable)
				{
					Player owner = ((RelicModel)this).Owner;
					result = ((owner != null && (owner.Relics?.Any((RelicModel r) => r is StarChart)).GetValueOrDefault()) ? 1 : 0);
				}
				else
				{
					result = 0;
				}
				return (byte)result != 0;
			}
		}

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			get
			{
				RefreshBlockTerm();
				return Array.Empty<IHoverTip>();
			}
		}

		private static string Term(string key)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return new LocString("relics", key).GetFormattedText();
		}

		private string BlockTerm()
		{
			return Term(HasStarChart ? "DECODE.termFull" : "DECODE.termPerfect");
		}

		private void RefreshBlockTerm()
		{
			DynamicVar val = default(DynamicVar);
			if (((AbstractModel)this).IsMutable && ((RelicModel)this).DynamicVars.TryGetValue("BlockTerm", ref val))
			{
				StringVar val2 = (StringVar)(object)((val is StringVar) ? val : null);
				if (val2 != null)
				{
					val2.StringValue = BlockTerm();
				}
			}
		}

		public override Task AfterObtained()
		{
			RefreshBlockTerm();
			return Task.CompletedTask;
		}

		public override Task BeforeCombatStart()
		{
			RefreshBlockTerm();
			return Task.CompletedTask;
		}

		public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null || (int)command.TargetSide != 1)
			{
				return;
			}
			List<DamageResult> myHits = (from r in command.Results.SelectMany((List<DamageResult> r) => r)
				where r.Receiver == ((RelicModel)this).Owner.Creature
				select r).ToList();
			if (myHits.Count != 0 && myHits.All((DamageResult r) => r.WasFullyBlocked) && (HasStarChart || ((RelicModel)this).Owner.Creature.Block == 0))
			{
				RefreshBlockTerm();
				ICombatState combatState = ((RelicModel)this).Owner.Creature.CombatState;
				IReadOnlyList<Creature> enemies = ((combatState != null) ? combatState.HittableEnemies : null);
				if (enemies != null && enemies.Count > 0)
				{
					await CreatureCmd.Damage(choiceContext, (IEnumerable<Creature>)enemies.ToList(), 9m, (ValueProp)8, ((RelicModel)this).Owner.Creature);
				}
				_pendingEnergy = true;
				((RelicModel)this).Flash();
			}
		}

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner && _pendingEnergy)
			{
				_pendingEnergy = false;
				await PlayerCmd.GainEnergy(2m, ((RelicModel)this).Owner);
			}
		}
	}
	public sealed class DemonTax : RelicModel
	{
		private bool _usedThisTurn;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/demon_tax.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/demon_tax.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/demon_tax.png";

		public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner)
			{
				_usedThisTurn = false;
			}
			return Task.CompletedTask;
		}

		public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null)
			{
				return amount;
			}
			if (target != ((RelicModel)this).Owner.Creature)
			{
				return amount;
			}
			if (amount <= 0m)
			{
				return amount;
			}
			if (_usedThisTurn)
			{
				return amount;
			}
			_usedThisTurn = true;
			((RelicModel)this).Flash();
			TaskHelper.RunSafely(ConvertHandCardToWoundAsync());
			return 0m;
		}

		private async Task ConvertHandCardToWoundAsync()
		{
			Player owner = ((RelicModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				Creature creature = owner.Creature;
				obj = ((creature != null) ? creature.CombatState : null);
			}
			if (obj == null)
			{
				return;
			}
			CardPile hand = PileTypeExtensions.GetPile((PileType)2, ((RelicModel)this).Owner);
			if (hand != null && hand.Cards.Count != 0)
			{
				CardModel card;
				if (hand.Cards.Count == 1)
				{
					card = hand.Cards[0];
				}
				else
				{
					BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
					card = (await CardSelectCmd.FromHand((PlayerChoiceContext)(object)ctx, ((RelicModel)this).Owner, new CardSelectorPrefs(((RelicModel)this).SelectionScreenPrompt, 1), (Func<CardModel, bool>)null, (AbstractModel)(object)this)).FirstOrDefault();
				}
				if (card != null)
				{
					await CardCmd.TransformTo<Wound>(card, (CardPreviewStyle)1);
				}
			}
		}
	}
	public sealed class DingDongPendant : RelicModel
	{
		private const int GoldCost = 5;

		private const decimal BlockGain = 15m;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/ding_dong_pendant.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/ding_dong_pendant.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/ding_dong_pendant.png";

		public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			if ((int)side == 1)
			{
				Player owner = ((RelicModel)this).Owner;
				if (((owner != null) ? owner.Creature : null) != null && ((RelicModel)this).Owner.Gold >= 5)
				{
					await PlayerCmd.LoseGold(5m, ((RelicModel)this).Owner, (GoldLossType)1);
					((RelicModel)this).Flash();
					await CreatureCmd.GainBlock(((RelicModel)this).Owner.Creature, 15m, (ValueProp)4, (CardPlay)null, false);
				}
			}
		}
	}
	public sealed class DirectionalAnchorFragment : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/directional_anchor_fragment.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/directional_anchor_fragment.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/directional_anchor_fragment.png";

		[SavedProperty]
		public ModelId? SavedCardId1 { get; set; }

		[SavedProperty]
		public ModelId? SavedCardId2 { get; set; }

		[SavedProperty]
		public ModelId? SavedCardId3 { get; set; }

		[SavedProperty]
		public int SavedCardUpgrade1 { get; set; }

		[SavedProperty]
		public int SavedCardUpgrade2 { get; set; }

		[SavedProperty]
		public int SavedCardUpgrade3 { get; set; }

		private List<(ModelId Id, int Upgrade)> SavedCards
		{
			get
			{
				List<(ModelId, int)> list = new List<(ModelId, int)>(3);
				if (SavedCardId1 != (ModelId)null)
				{
					list.Add((SavedCardId1, SavedCardUpgrade1));
				}
				if (SavedCardId2 != (ModelId)null)
				{
					list.Add((SavedCardId2, SavedCardUpgrade2));
				}
				if (SavedCardId3 != (ModelId)null)
				{
					list.Add((SavedCardId3, SavedCardUpgrade3));
				}
				return list;
			}
		}

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				Player owner = ((RelicModel)this).Owner;
				CardSelectorPrefs val = new CardSelectorPrefs(new LocString("relics", "DIRECTIONAL_ANCHOR_FRAGMENT_SELECT_PROMPT"), 3, 3);
				((CardSelectorPrefs)(ref val)).set_Cancelable(false);
				((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
				List<CardModel> selected = (await CardSelectCmd.FromDeckForRemoval(owner, val, (Func<CardModel, bool>)null)).ToList();
				if (selected.Count >= 3)
				{
					SavedCardId1 = ((AbstractModel)selected[0]).Id;
					SavedCardUpgrade1 = selected[0].CurrentUpgradeLevel;
					SavedCardId2 = ((AbstractModel)selected[1]).Id;
					SavedCardUpgrade2 = selected[1].CurrentUpgradeLevel;
					SavedCardId3 = ((AbstractModel)selected[2]).Id;
					SavedCardUpgrade3 = selected[2].CurrentUpgradeLevel;
				}
			}
		}

		public override async Task BeforeCombatStart()
		{
			List<(ModelId Id, int Upgrade)> savedCards = SavedCards;
			int num;
			if (savedCards.Count >= 3)
			{
				Player owner = ((RelicModel)this).Owner;
				num = ((((owner != null) ? owner.PlayerCombatState : null) == null) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
			if (num != 0)
			{
				return;
			}
			List<CardModel> allCards = ((RelicModel)this).Owner.PlayerCombatState.AllCards.ToList();
			HashSet<CardModel> taken = new HashSet<CardModel>();
			foreach (var saved in savedCards)
			{
				CardModel card = ((IEnumerable<CardModel>)allCards).FirstOrDefault((Func<CardModel, bool>)delegate(CardModel c)
				{
					//IL_004b: Unknown result type (might be due to invalid IL or missing references)
					//IL_0051: Invalid comparison between Unknown and I4
					int result;
					if (((AbstractModel)c).Id == saved.Id && c.CurrentUpgradeLevel == saved.Upgrade && !taken.Contains(c))
					{
						CardPile pile = c.Pile;
						result = ((pile == null || (int)pile.Type != 2) ? 1 : 0);
					}
					else
					{
						result = 0;
					}
					return (byte)result != 0;
				});
				if (card != null)
				{
					taken.Add(card);
					await CardPileCmd.Add(card, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
				}
			}
		}
	}
	public sealed class DullahansFlesh : RelicModel
	{
		private const decimal HpThreshold = 20m;

		private const int RegenGain = 6;

		private bool _armed = true;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/dullahans_flesh.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/dullahans_flesh.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/dullahans_flesh.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<RegenPower>((int?)null) };

		public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null && creature == ((RelicModel)this).Owner.Creature)
			{
				await CheckThreshold();
			}
		}

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner)
			{
				await CheckThreshold();
			}
		}

		public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null && target == ((RelicModel)this).Owner.Creature)
			{
				RegenPower regen = ((RelicModel)this).Owner.Creature.GetPower<RegenPower>();
				if (regen != null && ((PowerModel)regen).Amount > 0)
				{
					((RelicModel)this).Flash();
					await CreatureCmd.Heal(((RelicModel)this).Owner.Creature, (decimal)((PowerModel)regen).Amount, true);
					await PowerCmd.Decrement((PowerModel)(object)regen);
				}
			}
		}

		private async Task CheckThreshold()
		{
			Player owner2 = ((RelicModel)this).Owner;
			if (((owner2 != null) ? owner2.Creature : null) != null)
			{
				Creature owner = ((RelicModel)this).Owner.Creature;
				if ((decimal)owner.CurrentHp >= 20m)
				{
					_armed = true;
				}
				else if (_armed)
				{
					_armed = false;
					((RelicModel)this).Flash();
					await PowerCmd.Apply<RegenPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), owner, 6m, owner, (CardModel)null, false);
				}
			}
		}
	}
	public sealed class EchoAbsorptionDevice : RelicModel
	{
		private bool _pendingExtraTurn;

		private bool _extraTurnActive;

		[SavedProperty]
		public bool PendingExtraTurn
		{
			get
			{
				return _pendingExtraTurn;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_pendingExtraTurn = value;
			}
		}

		[SavedProperty]
		public bool ExtraTurnActive
		{
			get
			{
				return _extraTurnActive;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_extraTurnActive = value;
			}
		}

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/echo_absorption_device.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/echo_absorption_device.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/echo_absorption_device.png";

		public override bool IsAllowed(IRunState runState)
		{
			return ((IPlayerCollection)runState).Players.Count > 1;
		}

		public override bool IsAllowedAtNeow(Player player)
		{
			return ((IPlayerCollection)player.RunState).Players.Count > 1;
		}

		public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null || creature.Player == null || creature.Player == ((RelicModel)this).Owner || creature.Side != ((RelicModel)this).Owner.Creature.Side)
			{
				return;
			}
			await CreatureCmd.Heal(((RelicModel)this).Owner.Creature, (decimal)((RelicModel)this).Owner.Creature.MaxHp, true);
			ICombatState combatState = ((RelicModel)this).Owner.Creature.CombatState;
			if (combatState != null)
			{
				Player deadPlayer = creature.Player;
				List<CardModel> deckCards = deadPlayer.Deck.Cards.ToList();
				if (deckCards.Count > 0)
				{
					Rng rng = ((RelicModel)this).Owner.PlayerRng.Rewards;
					int stealCount = Math.Min(3, deckCards.Count);
					for (int i = 0; i < stealCount; i++)
					{
						int idx = rng.NextInt(deckCards.Count);
						CardModel canonicalCard = ModelDb.GetById<CardModel>(((AbstractModel)deckCards[idx]).Id);
						CardModel newCard = combatState.CreateCard(canonicalCard, ((RelicModel)this).Owner);
						deckCards.RemoveAt(idx);
						await CardPileCmd.Add(newCard, (PileType)1, (CardPilePosition)1, (AbstractModel)null, false);
					}
				}
			}
			PendingExtraTurn = true;
		}

		public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			if (((RelicModel)this).Owner != null && side == ((RelicModel)this).Owner.Creature.Side && PendingExtraTurn)
			{
				PendingExtraTurn = false;
				ExtraTurnActive = true;
			}
		}

		public override bool ShouldTakeExtraTurn(Player player)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return false;
			}
			if (!ExtraTurnActive)
			{
				return false;
			}
			ExtraTurnActive = false;
			return true;
		}
	}
	public sealed class EglaTributeWine : RelicModel
	{
		private const int Combats = 3;

		private const int CardsPerTurn = 1;

		private int _combatsLeft;

		[SavedProperty]
		public int CombatsLeft
		{
			get
			{
				return _combatsLeft;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_combatsLeft = value;
				((RelicModel)this).InvokeDisplayAmountChanged();
			}
		}

		[SavedProperty]
		public bool SkipCurrentCombat { get; set; }

		public override bool ShowCounter => true;

		public override int DisplayAmount => CombatsLeft;

		public override RelicRarity Rarity => (RelicRarity)6;

		public override string PackedIconPath => "res://wuwancients/images/relics/egla_tribute_wine.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/egla_tribute_wine.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/egla_tribute_wine.png";

		public override Task AfterObtained()
		{
			CombatsLeft = 3;
			Player owner = ((RelicModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				Creature creature = owner.Creature;
				obj = ((creature != null) ? creature.CombatState : null);
			}
			SkipCurrentCombat = obj != null;
			return Task.CompletedTask;
		}

		public override decimal ModifyMaxEnergy(Player player, decimal amount)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return amount;
			}
			if (CombatsLeft <= 0)
			{
				return amount;
			}
			return amount + 1m;
		}

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner && CombatsLeft > 0)
			{
				await CardPileCmd.Draw(choiceContext, 1m, player, false);
			}
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			if (SkipCurrentCombat)
			{
				SkipCurrentCombat = false;
				return Task.CompletedTask;
			}
			if (CombatsLeft > 0)
			{
				CombatsLeft--;
			}
			return Task.CompletedTask;
		}
	}
	public sealed class EndlessLoop : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new CardsVar(1) };

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			get
			{
				List<IHoverTip> list = new List<IHoverTip>();
				list.Add(HoverTipFactory.ForEnergy((RelicModel)(object)this));
				list.AddRange(HoverTipFactory.FromCardWithCardHoverTips<StarDomain>(false));
				return list;
			}
		}

		public override string PackedIconPath => "res://wuwancients/images/relics/endless_loop.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/endless_loop.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/endless_loop.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				CardModel starDomain = (CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<StarDomain>(((RelicModel)this).Owner);
				await CardPileCmd.Add(starDomain, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
			}
		}
	}
	public sealed class ExpeditionKey : RelicModel
	{
		private const int WarpAt = 40;

		private const int SprayAt = 80;

		private const int StrokeAt = 120;

		private const int DecayPerTurn = 40;

		private bool _dealtDamageThisTurn;

		private bool _warpGiven;

		private bool _sprayGiven;

		private bool _strokeGiven;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/expedition_key.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/expedition_key.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/expedition_key.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[4]
		{
			HoverTipFactory.FromPower<SpeedPower>((int?)null),
			HoverTipFactory.FromPower<WarpPower>((int?)null),
			HoverTipFactory.FromPower<SprayPaintPower>((int?)null),
			HoverTipFactory.FromPower<AnotherStrokePower>((int?)null)
		};

		public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null || dealer != ((RelicModel)this).Owner.Creature || target == null || target.Side == ((RelicModel)this).Owner.Creature.Side)
			{
				return;
			}
			int dealt = result.UnblockedDamage;
			if (dealt > 0)
			{
				_dealtDamageThisTurn = true;
				await PowerCmd.Apply<SpeedPower>(choiceContext, ((RelicModel)this).Owner.Creature, (decimal)dealt, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				int speed = ((RelicModel)this).Owner.Creature.GetPowerAmount<SpeedPower>();
				if (!_warpGiven && speed >= 40)
				{
					_warpGiven = true;
					((RelicModel)this).Flash();
					PlayVoice("warp");
					await PowerCmd.Apply<WarpPower>(choiceContext, ((RelicModel)this).Owner.Creature, 1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
				if (!_sprayGiven && speed >= 80)
				{
					_sprayGiven = true;
					((RelicModel)this).Flash();
					PlayVoice("spray");
					await PowerCmd.Apply<SprayPaintPower>(choiceContext, ((RelicModel)this).Owner.Creature, 1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
				if (!_strokeGiven && speed >= 120)
				{
					_strokeGiven = true;
					((RelicModel)this).Flash();
					PlayVoice("stroke");
					await PowerCmd.Apply<AnotherStrokePower>(choiceContext, ((RelicModel)this).Owner.Creature, 1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
			}
		}

		public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			if ((int)side != 1)
			{
				return;
			}
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null)
			{
				return;
			}
			if (!_dealtDamageThisTurn)
			{
				SpeedPower speed = ((RelicModel)this).Owner.Creature.GetPower<SpeedPower>();
				if (speed != null && ((PowerModel)speed).Amount > 0)
				{
					decimal loss = Math.Min(40, ((PowerModel)speed).Amount);
					await PowerCmd.Apply<SpeedPower>(choiceContext, ((RelicModel)this).Owner.Creature, -loss, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
			}
			_dealtDamageThisTurn = false;
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			_dealtDamageThisTurn = false;
			_warpGiven = false;
			_sprayGiven = false;
			_strokeGiven = false;
			return Task.CompletedTask;
		}

		private static void PlayVoice(string name)
		{
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot("res://wuwancients/audio/linna_" + name + ".wav", 1f);
			}
		}
	}
	public sealed class ExtraThickMilkshakeShavedIce : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/extra_thick_milkshake_shaved_ice.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/extra_thick_milkshake_shaved_ice.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/extra_thick_milkshake_shaved_ice.png";

		public override decimal ModifyMaxEnergy(Player player, decimal amount)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return amount;
			}
			return amount + 1m;
		}

		public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
		{
			if (card.Owner != ((RelicModel)this).Owner || !card.Keywords.Contains((CardKeyword)1))
			{
				modifiedCost = originalCost;
				return false;
			}
			modifiedCost = originalCost + 1m;
			return true;
		}
	}
	public sealed class FallingEcho : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<DynamicVar> CanonicalVars => Enumerable.Empty<DynamicVar>();

		protected override IEnumerable<IHoverTip> ExtraHoverTips => Enumerable.Empty<IHoverTip>();

		public override string PackedIconPath => "res://wuwancients/images/relics/falling_echo.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/falling_echo.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/falling_echo.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				List<RelicModel> removable = ((RelicModel)this).Owner.Relics.Where((RelicModel r) => r != this && (int)r.Rarity != 1).ToList();
				if (removable.Count != 0)
				{
					RelicModel toRemove = removable[((RelicModel)this).Owner.PlayerRng.Rewards.NextInt(removable.Count)];
					await RelicCmd.Remove(toRemove);
				}
			}
		}

		public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewards, CardCreationOptions options)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return false;
			}
			foreach (CardCreationResult cardReward in cardRewards)
			{
				CardModel card = cardReward.Card;
				if (card.IsUpgradable)
				{
					CardModel val = ((ICardScope)((RelicModel)this).Owner.RunState).CloneCard(card);
					CardCmd.Upgrade(val, (CardPreviewStyle)1);
					cardReward.ModifyCard(val, (RelicModel)(object)this);
				}
			}
			List<EnchantmentModel> list = ModelDb.DebugEnchantments.ToList();
			if (list.Count == 0)
			{
				return false;
			}
			foreach (CardCreationResult cardReward2 in cardRewards)
			{
				CardModel currentCard = cardReward2.Card;
				List<EnchantmentModel> list2 = list.Where((EnchantmentModel e) => e.CanEnchant(currentCard)).ToList();
				if (list2.Count != 0)
				{
					EnchantmentModel val2 = list2[((RelicModel)this).Owner.PlayerRng.Rewards.NextInt(list2.Count)];
					CardModel val3 = ((ICardScope)((RelicModel)this).Owner.RunState).CloneCard(currentCard);
					CardCmd.Enchant(val2.ToMutable(), val3, 1m);
					cardReward2.ModifyCard(val3, (RelicModel)(object)this);
				}
			}
			return true;
		}
	}
	public sealed class FeisaliesGift : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool ShowCounter => false;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("Potions", 1m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/feisalies_gift.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/feisalies_gift.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/feisalies_gift.png";

		public override async Task AfterCombatEnd(CombatRoom room)
		{
			if (((RelicModel)this).Owner != null && !((RelicModel)this).Owner.Potions.Any())
			{
				((RelicModel)this).Flash();
				PotionModel potion = PotionFactory.CreateRandomPotionOutOfCombat(((RelicModel)this).Owner, ((RelicModel)this).Owner.RunState.Rng.CombatPotionGeneration, (IEnumerable<PotionModel>)null).ToMutable();
				await PotionCmd.TryToProcure(potion, ((RelicModel)this).Owner, -1);
			}
		}

		public override Task BeforeCombatStart()
		{
			return Task.CompletedTask;
		}
	}
	public sealed class FibiDumpling : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => false;

		[SavedProperty]
		public bool HasUsedReplayThisCombat { get; set; }

		public override string PackedIconPath => "res://wuwancients/images/relics/fibi_dumpling.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/fibi_dumpling.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/fibi_dumpling.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				await PlayerCmd.GainGold(400m, ((RelicModel)this).Owner, false);
			}
		}

		public override decimal ModifyHandDraw(Player player, decimal count)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return count;
			}
			if (((RelicModel)this).Owner.Gold > 400)
			{
				return count + 1m;
			}
			return count;
		}

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player != ((RelicModel)this).Owner || ((RelicModel)this).Owner.Gold <= 800 || HasUsedReplayThisCombat)
			{
				return;
			}
			foreach (CardModel card in await CardSelectCmd.FromHand(choiceContext, ((RelicModel)this).Owner, new CardSelectorPrefs(new LocString("relics", "FIBI_DUMPLING_SELECT_PROMPT"), 1), (Func<CardModel, bool>)null, (AbstractModel)(object)this))
			{
				int baseReplayCount = card.BaseReplayCount;
				card.BaseReplayCount = baseReplayCount + 1;
			}
			HasUsedReplayThisCombat = true;
		}

		public override Task BeforeCombatStart()
		{
			HasUsedReplayThisCombat = false;
			return Task.CompletedTask;
		}

		public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			if (((RelicModel)this).Owner.Gold > 1600 && cardPlay.Card.Owner == ((RelicModel)this).Owner)
			{
				await CardPileCmd.Draw(choiceContext, 1m, ((RelicModel)this).Owner, false);
				await PlayerCmd.GainEnergy(1m, ((RelicModel)this).Owner);
			}
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			return Task.CompletedTask;
		}
	}
	public sealed class FireDevilGreeting : RelicModel
	{
		private const string _combatsKey = "Combats";

		private int _combatsLeft;

		private bool _activeThisCombat;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool ShowCounter => true;

		public override int DisplayAmount => CombatsLeft;

		[SavedProperty]
		public int CombatsLeft
		{
			get
			{
				return _combatsLeft;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_combatsLeft = value;
				((RelicModel)this).InvokeDisplayAmountChanged();
				if (CombatsLeft <= 0)
				{
					((RelicModel)this).Status = (RelicStatus)2;
				}
			}
		}

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("Combats", 4m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/fire_devil_greeting.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/fire_devil_greeting.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/fire_devil_greeting.png";

		public override Task AfterObtained()
		{
			CombatsLeft = (int)((RelicModel)this).DynamicVars["Combats"].BaseValue;
			return Task.CompletedTask;
		}

		public override async Task BeforeCombatStart()
		{
			Player owner = ((RelicModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				Creature creature = owner.Creature;
				obj = ((creature != null) ? creature.CombatState : null);
			}
			if (obj == null)
			{
				return;
			}
			_activeThisCombat = CombatsLeft > 0;
			if (!_activeThisCombat)
			{
				return;
			}
			((RelicModel)this).Flash();
			IReadOnlyList<Creature> enemies = ((RelicModel)this).Owner.Creature.CombatState.HittableEnemies;
			VfxCmd.PlayOnCreatureCenters((IEnumerable<Creature>)enemies, "vfx/vfx_bite");
			foreach (Creature enemy in enemies)
			{
				await CreatureCmd.SetCurrentHp(enemy, 1m);
			}
			CombatsLeft--;
		}

		public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			if (!_activeThisCombat)
			{
				return;
			}
			foreach (Creature enemy in combatState.GetCreaturesOnSide((CombatSide)2))
			{
				if (!enemy.IsDead)
				{
					await CreatureCmd.SetCurrentHp(enemy, 1m);
				}
			}
		}

		public override async Task AfterCreatureAddedToCombat(Creature creature)
		{
			if (_activeThisCombat && creature != null && (int)creature.Side == 2 && !creature.IsDead)
			{
				VfxCmd.PlayOnCreatureCenters((IEnumerable<Creature>)(object)new Creature[1] { creature }, "vfx/vfx_bite");
				await CreatureCmd.SetCurrentHp(creature, 1m);
			}
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			_activeThisCombat = false;
			return Task.CompletedTask;
		}
	}
	public sealed class FirstBloodOath : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<BloodOath>(1);

		public override string PackedIconPath => "res://wuwancients/images/relics/first_blood_oath.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/first_blood_oath.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/first_blood_oath.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			Player owner = ((RelicModel)this).Owner;
			BloodOath bloodOath = ModelDb.Enchantment<BloodOath>();
			Func<CardModel, bool> obj = (CardModel? c) => c != null && BloodOath.HasNumericVar(c);
			CardSelectorPrefs val = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 0, 1);
			((CardSelectorPrefs)(ref val)).set_Cancelable(false);
			((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
			foreach (CardModel card in await CardSelectCmd.FromDeckForEnchantment(owner, (EnchantmentModel)(object)bloodOath, 1, obj, val))
			{
				CardCmd.Enchant<BloodOath>(card, 1m);
			}
		}
	}
	public sealed class FlameClaw : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override string PackedIconPath => "res://wuwancients/images/relics/flame_claw.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/flame_claw.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/flame_claw.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<FlameLight>(1);

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			Player owner = ((RelicModel)this).Owner;
			FlameLight flameLight = ModelDb.Enchantment<FlameLight>();
			CardSelectorPrefs val = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 0, 1);
			((CardSelectorPrefs)(ref val)).set_Cancelable(false);
			((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
			foreach (CardModel card in await CardSelectCmd.FromDeckForEnchantment(owner, (EnchantmentModel)(object)flameLight, 1, (Func<CardModel, bool>)null, val))
			{
				CardCmd.Enchant<FlameLight>(card, 1m);
			}
		}
	}
	public sealed class FlameJudgment : RelicModel
	{
		private const int RelicCount = 2;

		private const int CurseCount = 2;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override string PackedIconPath => "res://wuwancients/images/relics/flame_judgment.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/flame_judgment.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/flame_judgment.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				await GrantAncientRelics();
				await AddRandomCurses();
			}
		}

		private async Task GrantAncientRelics()
		{
			List<RelicModel> pool = (from r in ModelDb.AllRelics
				where (int)r.Rarity == 7
				where ((AbstractModel)r).Id != ((AbstractModel)this).Id
				where !((RelicModel)this).Owner.Relics.Any((RelicModel owned) => ((AbstractModel)owned).Id == ((AbstractModel)r).Id)
				select r).ToList();
			Rng rng = SyncedRng.Niche(((RelicModel)this).Owner.RunState);
			for (int i = 0; i < 2; i++)
			{
				if (pool.Count <= 0)
				{
					break;
				}
				RelicModel pick = pool[SyncedRng.Index(rng, pool.Count)];
				pool.Remove(pick);
				await RelicCmd.Obtain(pick.ToMutable(), ((RelicModel)this).Owner, -1);
			}
		}

		private async Task AddRandomCurses()
		{
			List<CardModel> pool = (from c in ((CardPoolModel)ModelDb.CardPool<CurseCardPool>()).GetUnlockedCards(((RelicModel)this).Owner.UnlockState, ((RelicModel)this).Owner.RunState.CardMultiplayerConstraint)
				where c != null && c.CanBeGeneratedByModifiers
				select c).ToList();
			if (pool.Count != 0)
			{
				Rng rng = SyncedRng.Niche(((RelicModel)this).Owner.RunState);
				List<CardPileAddResult> results = new List<CardPileAddResult>();
				for (int i = 0; i < 2; i++)
				{
					CardModel proto = pool[SyncedRng.Index(rng, pool.Count)];
					CardModel card = ((ICardScope)((RelicModel)this).Owner.RunState).CreateCard(proto, ((RelicModel)this).Owner);
					List<CardPileAddResult> list = results;
					list.Add(await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false));
				}
				CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)results, 2f, (CardPreviewStyle)1);
				await Cmd.Wait(0.75f, false);
			}
		}
	}
	public sealed class FragrantLemonShabuShabu : RelicModel
	{
		private bool _attackTriggeredThisTurn;

		private bool _skillTriggeredThisTurn;

		private bool _powerTriggeredThisTurn;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/fragrant_lemon_shabu_shabu.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/fragrant_lemon_shabu_shabu.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/fragrant_lemon_shabu_shabu.png";

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			_attackTriggeredThisTurn = false;
			_skillTriggeredThisTurn = false;
			_powerTriggeredThisTurn = false;
		}

		public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			if ((int)cardPlay.Card.Type == 1 && !_attackTriggeredThisTurn)
			{
				_attackTriggeredThisTurn = true;
				await CardPileCmd.Draw(choiceContext, 1m, ((RelicModel)this).Owner, false);
			}
			else if ((int)cardPlay.Card.Type == 2 && !_skillTriggeredThisTurn)
			{
				_skillTriggeredThisTurn = true;
				await PlayerCmd.GainEnergy(1m, ((RelicModel)this).Owner);
			}
			else if ((int)cardPlay.Card.Type == 3 && !_powerTriggeredThisTurn)
			{
				_powerTriggeredThisTurn = true;
				_attackTriggeredThisTurn = false;
				_skillTriggeredThisTurn = false;
			}
		}
	}
	public sealed class FrostSignalFlower : RelicModel
	{
		private ModelId? _ancientCard;

		private IEnumerable<IHoverTip> _extraHoverTips = Array.Empty<IHoverTip>();

		public override RelicRarity Rarity => (RelicRarity)7;

		[SavedProperty]
		public ModelId? AncientCard
		{
			get
			{
				return _ancientCard;
			}
			set
			{
				//IL_0053: Unknown result type (might be due to invalid IL or missing references)
				((AbstractModel)this).AssertMutable();
				_ancientCard = value;
				if (value != (ModelId)null)
				{
					CardModel val = SaveUtil.CardOrDeprecated(value);
					_extraHoverTips = val.HoverTips.Concat((IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromCard(val, true) });
					((StringVar)((RelicModel)this).DynamicVars["AncientCard"]).StringValue = val.Title;
				}
			}
		}

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new StringVar[1]
		{
			new StringVar("AncientCard", "")
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			get
			{
				if (_ancientCard == (ModelId)null)
				{
					try
					{
						if (((RelicModel)this).Owner != null)
						{
							CardModel targetCardForPlayer = GetTargetCardForPlayer();
							if (targetCardForPlayer != null)
							{
								AncientCard = ((AbstractModel)targetCardForPlayer).Id;
							}
						}
					}
					catch
					{
					}
				}
				return _extraHoverTips;
			}
		}

		public override string PackedIconPath => "res://wuwancients/images/relics/frost_signal_flower.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/frost_signal_flower.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/frost_signal_flower.png";

		private CardModel? GetTargetCardForPlayer()
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Character : null) == null)
			{
				return null;
			}
			Type type = ((object)owner.Character).GetType();
			if (type == typeof(Ironclad))
			{
				return (CardModel?)(object)ModelDb.Card<Resignation>();
			}
			if (type == typeof(Necrobinder))
			{
				return (CardModel?)(object)ModelDb.Card<Symbiosis>();
			}
			if (type == typeof(Silent))
			{
				return (CardModel?)(object)ModelDb.Card<Endless>();
			}
			if (type == typeof(Regent))
			{
				return (CardModel?)(object)ModelDb.Card<RoyalLove>();
			}
			if (type == typeof(Defect))
			{
				return (CardModel?)(object)ModelDb.Card<NeuralNetwork>();
			}
			if (((AbstractModel)owner.Character).Id.Entry == "WATCHER-WATCHER")
			{
				return ModelDb.GetById<CardModel>(ModelId.Deserialize("CARD.WATCHER-ANCIENT_CARD"));
			}
			return null;
		}

		public override async Task AfterObtained()
		{
			Player player = ((RelicModel)this).Owner;
			if (player != null)
			{
				CardModel targetCard = GetTargetCardForPlayer();
				if (targetCard != null)
				{
					AncientCard = ((AbstractModel)targetCard).Id;
					CardModel card = ((ICardScope)player.RunState).CreateCard(targetCard, player);
					CardCmd.Upgrade(card, (CardPreviewStyle)1);
					CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 2f, (CardPreviewStyle)1);
				}
			}
		}
	}
	public sealed class FrozenCore : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)1;

		public override string PackedIconPath => "res://wuwancients/images/relics/frozen_core.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/frozen_core.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/frozen_core.png";

		public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null || side != ((RelicModel)this).Owner.Creature.Side || ((RelicModel)this).Owner.Creature.IsDead)
			{
				return;
			}
			PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
			OrbQueue orbQueue = ((playerCombatState != null) ? playerCombatState.OrbQueue : null);
			if (orbQueue != null)
			{
				if (orbQueue.Orbs.Count < orbQueue.Capacity)
				{
					await OrbCmd.Channel(choiceContext, OrbModel.GetRandomOrb(((RelicModel)this).Owner.PlayerRng.Rewards).ToMutable(0), ((RelicModel)this).Owner);
					return;
				}
				await OrbCmd.AddSlots(((RelicModel)this).Owner, 1);
				((RelicModel)this).Flash();
			}
		}
	}
	public sealed class FulouluoDumpling : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/fulouluo_dumpling.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/fulouluo_dumpling.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/fulouluo_dumpling.png";

		public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			if (side != ((RelicModel)this).Owner.Creature.Side)
			{
				return Task.CompletedTask;
			}
			PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
			CardPile val = ((playerCombatState != null) ? playerCombatState.DrawPile : null);
			if (val == null)
			{
				return Task.CompletedTask;
			}
			IReadOnlyList<CardModel> cards = val.Cards;
			if (cards == null || cards.Count < 2)
			{
				return Task.CompletedTask;
			}
			CardModel val2 = cards.Last();
			if (val2 == null || val2.BaseReplayCount > 0)
			{
				return Task.CompletedTask;
			}
			val2.BaseReplayCount += 2;
			((RelicModel)this).Flash();
			return Task.CompletedTask;
		}
	}
	public sealed class GalbrenasKarmaFire : RelicModel
	{
		private int _damageHits;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/galbrenas_karma_fire.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/galbrenas_karma_fire.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/galbrenas_karma_fire.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<KarmaFirePower>((int?)null) };

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new StringVar("LayerText", "")
		{
			StringValue = LayerText()
		} };

		[SavedProperty]
		public int DamageHits
		{
			get
			{
				return _damageHits;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_damageHits = value;
			}
		}

		private static string LayerText()
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			string text = (WuwancientsConfig.业火每三次伤害叠加一层 ? "GALBRENAS_KARMA_FIRE.layerThree" : "GALBRENAS_KARMA_FIRE.layerEvery");
			return new LocString("relics", text).GetFormattedText();
		}

		public override Task AfterObtained()
		{
			RefreshLayerText();
			return Task.CompletedTask;
		}

		private void RefreshLayerText()
		{
			DynamicVar val = default(DynamicVar);
			if (((RelicModel)this).DynamicVars.TryGetValue("LayerText", ref val))
			{
				StringVar val2 = (StringVar)(object)((val is StringVar) ? val : null);
				if (val2 != null)
				{
					val2.StringValue = LayerText();
				}
			}
		}

		public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null || dealer != ((RelicModel)this).Owner.Creature || target == null || !target.IsAlive || target.Side == ((RelicModel)this).Owner.Creature.Side || result.UnblockedDamage <= 0)
			{
				return;
			}
			DamageHits++;
			if (WuwancientsConfig.业火每三次伤害叠加一层 && DamageHits % 3 != 0)
			{
				return;
			}
			int carried = ((RelicModel)this).Owner.Creature.GetPowerAmount<KarmaFirePower>();
			if (carried > 0)
			{
				KarmaFirePower own = ((RelicModel)this).Owner.Creature.GetPower<KarmaFirePower>();
				if (own != null)
				{
					await PowerCmd.Remove((PowerModel)(object)own);
				}
			}
			await PowerCmd.Apply<KarmaFirePower>(choiceContext, target, (decimal)(carried + 1), ((RelicModel)this).Owner.Creature, (CardModel)null, false);
			((RelicModel)this).Flash();
		}
	}
	public sealed class GiftOfQiqiu : RelicModel
	{
		[CompilerGenerated]
		private sealed class <get_ExtraHoverTips>d__9 : IEnumerable<IHoverTip>, IEnumerable, IEnumerator<IHoverTip>, IEnumerator, IDisposable
		{
			private int <>1__state;

			private IHoverTip <>2__current;

			private int <>l__initialThreadId;

			public GiftOfQiqiu <>4__this;

			IHoverTip IEnumerator<IHoverTip>.Current
			{
				[DebuggerHidden]
				get
				{
					return <>2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return <>2__current;
				}
			}

			[DebuggerHidden]
			public <get_ExtraHoverTips>d__9(int <>1__state)
			{
				this.<>1__state = <>1__state;
				<>l__initialThreadId = Environment.CurrentManagedThreadId;
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
				<>1__state = -2;
			}

			private bool MoveNext()
			{
				switch (<>1__state)
				{
				default:
					return false;
				case 0:
					<>1__state = -1;
					<>2__current = HoverTipFactory.FromPotion<HeroKingsBlood>();
					<>1__state = 1;
					return true;
				case 1:
					<>1__state = -1;
					return false;
				}
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
				throw new NotSupportedException();
			}

			[DebuggerHidden]
			IEnumerator<IHoverTip> IEnumerable<IHoverTip>.GetEnumerator()
			{
				<get_ExtraHoverTips>d__9 result;
				if (<>1__state == -2 && <>l__initialThreadId == Environment.CurrentManagedThreadId)
				{
					<>1__state = 0;
					result = this;
				}
				else
				{
					result = new <get_ExtraHoverTips>d__9(0)
					{
						<>4__this = <>4__this
					};
				}
				return result;
			}

			[DebuggerHidden]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<IHoverTip>)this).GetEnumerator();
			}
		}

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/gift_of_qiqiu.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/gift_of_qiqiu.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/gift_of_qiqiu.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			[IteratorStateMachine(typeof(<get_ExtraHoverTips>d__9))]
			get
			{
				//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
				<get_ExtraHoverTips>d__9 <get_ExtraHoverTips>d__ = new <get_ExtraHoverTips>d__9(-2);
				<get_ExtraHoverTips>d__.<>4__this = this;
				return <get_ExtraHoverTips>d__;
			}
		}

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				HeroKingsBlood potionModel = ModelDb.Potion<HeroKingsBlood>();
				if (potionModel != null)
				{
					PotionModel potion = ((PotionModel)potionModel).ToMutable();
					await PotionCmd.TryToProcure(potion, ((RelicModel)this).Owner, -1);
				}
			}
		}
	}
	public sealed class GlowingMarigold : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/glowing_marigold.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/glowing_marigold.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/glowing_marigold.png";

		public override async Task BeforeCombatStart()
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RelicModel)this).Owner.Creature, 1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				await PowerCmd.Apply<DexterityPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RelicModel)this).Owner.Creature, 1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
			}
		}

		public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
		{
			if (target != ((RelicModel)this).Owner.Creature)
			{
				return 0m;
			}
			if (!(power is StrengthPower) && !(power is DexterityPower))
			{
				return 0m;
			}
			if (amount <= 0m)
			{
				return 0m;
			}
			Log.Info(string.Format("[wuwancients] marigold +1: power={0} amount={1} giver={2} target={3} card={4}", ((object)power).GetType().Name, amount, ((giver != null) ? giver.Name : null) ?? "null", ((target != null) ? target.Name : null) ?? "null", ((cardSource != null) ? ((AbstractModel)cardSource).Id.Entry : null) ?? "null"), 2);
			return 1m;
		}

		public override Task AfterModifyingPowerAmountGiven(PowerModel power)
		{
			if ((power is StrengthPower || power is DexterityPower) ? true : false)
			{
				((RelicModel)this).Flash();
			}
			return Task.CompletedTask;
		}
	}
	public sealed class GoatBaaGreeting : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new CardsVar(2) };

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.Static((StaticHoverTip)4, Array.Empty<DynamicVar>()) };

		public override string PackedIconPath => "res://wuwancients/images/relics/goat_baa_greeting.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/goat_baa_greeting.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/goat_baa_greeting.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			List<CardModel> selectedCards = (await CardSelectCmd.FromDeckForTransformation(((RelicModel)this).Owner, new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 2, 2), (Func<CardModel, CardTransformation>)null)).ToList();
			if (selectedCards.Count < 2)
			{
				return;
			}
			List<CardModel> colorlessPool = (from c in ((CardPoolModel)ModelDb.CardPool<ColorlessCardPool>()).GetUnlockedCards(((RelicModel)this).Owner.UnlockState, ((RelicModel)this).Owner.RunState.CardMultiplayerConstraint)
				where c.CanBeGeneratedByModifiers
				select c).ToList();
			if (colorlessPool.Count == 0)
			{
				return;
			}
			List<CardModel> cursePool = (from c in ((CardPoolModel)ModelDb.CardPool<CurseCardPool>()).GetUnlockedCards(((RelicModel)this).Owner.UnlockState, ((RelicModel)this).Owner.RunState.CardMultiplayerConstraint)
				where c.CanBeGeneratedByModifiers
				select c).ToList();
			if (cursePool.Count != 0)
			{
				CardModel firstCard = selectedCards[0];
				CardModel colorlessTemplate = ((RelicModel)this).Owner.RunState.Rng.Niche.NextItem<CardModel>((IEnumerable<CardModel>)colorlessPool);
				if (colorlessTemplate != null)
				{
					CardModel colorlessCard = ((ICardScope)((RelicModel)this).Owner.RunState).CreateCard(colorlessTemplate, ((RelicModel)this).Owner);
					await CardPileCmd.RemoveFromDeck(firstCard, true);
					await CardPileCmd.Add(colorlessCard, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
				}
				CardModel secondCard = selectedCards[1];
				CardModel curseTemplate = ((RelicModel)this).Owner.RunState.Rng.Niche.NextItem<CardModel>((IEnumerable<CardModel>)cursePool);
				if (curseTemplate != null)
				{
					CardModel curseCard = ((ICardScope)((RelicModel)this).Owner.RunState).CreateCard(curseTemplate, ((RelicModel)this).Owner);
					await CardPileCmd.RemoveFromDeck(secondCard, true);
					await CardPileCmd.Add(curseCard, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
				}
				((RelicModel)this).Flash();
			}
		}
	}
	public sealed class GoldenGrace : RelicModel
	{
		private bool _hasPlayedThisTurn;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/golden_grace.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/golden_grace.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/golden_grace.png";

		public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner)
			{
				_hasPlayedThisTurn = false;
			}
			return Task.CompletedTask;
		}

		public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			if (cardPlay.Card.Owner == ((RelicModel)this).Owner && !_hasPlayedThisTurn)
			{
				_hasPlayedThisTurn = true;
				ResourceInfo resources = cardPlay.Resources;
				int cost = ((ResourceInfo)(ref resources)).EnergyValue;
				if (cost > 0)
				{
					((RelicModel)this).Flash();
					await CardPileCmd.Draw(choiceContext, (decimal)(cost * 2), ((RelicModel)this).Owner, false);
				}
			}
		}

		public override Task BeforeCombatStart()
		{
			_hasPlayedThisTurn = false;
			return Task.CompletedTask;
		}
	}
	public sealed class Gospel : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/gospel.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/gospel.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/gospel.png";

		public override decimal ModifyEnergyGain(Player player, decimal amount)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return amount;
			}
			return amount + 1m;
		}
	}
	[HarmonyPatch(typeof(CardPileCmd), "Draw", new Type[]
	{
		typeof(PlayerChoiceContext),
		typeof(decimal),
		typeof(Player),
		typeof(bool)
	})]
	public static class GospelExtraDrawPatch
	{
		public static void Prefix(Player player, ref decimal count)
		{
			if (player != null && player.Relics.Any((RelicModel r) => r is Gospel))
			{
				count += 1m;
			}
		}
	}
	public sealed class HarpyienSharpFeather : RelicModel
	{
		private const int SkillsNeeded = 2;

		private const decimal SoarDamageMultiplier = 1.5m;

		private int _consecutiveSkills;

		private CardPlay? _lastCountedPlay;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/harpyien_sharp_feather.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/harpyien_sharp_feather.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/harpyien_sharp_feather.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<SoarPower>((int?)null) };

		public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner)
			{
				_consecutiveSkills = 0;
			}
			return Task.CompletedTask;
		}

		public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null)
			{
				return;
			}
			if (((cardPlay != null) ? cardPlay.Card : null) == null || cardPlay.Card.Owner != ((RelicModel)this).Owner || _lastCountedPlay == cardPlay)
			{
				return;
			}
			_lastCountedPlay = cardPlay;
			if ((int)cardPlay.Card.Type == 2)
			{
				_consecutiveSkills++;
				if (_consecutiveSkills >= 2 && ((RelicModel)this).Owner.Creature.GetPower<SoarPower>() == null)
				{
					_consecutiveSkills = 0;
					((RelicModel)this).Flash();
					await PowerCmd.Apply<SoarPower>(choiceContext, ((RelicModel)this).Owner.Creature, 1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
			}
			else
			{
				_consecutiveSkills = 0;
				if ((int)cardPlay.Card.Type == 1)
				{
					await LoseSoar();
				}
			}
		}

		public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
		{
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Invalid comparison between Unknown and I4
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Invalid comparison between Unknown and I4
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null)
			{
				return 1m;
			}
			if (dealer != ((RelicModel)this).Owner.Creature)
			{
				return 1m;
			}
			if (cardSource == null || (int)cardSource.Type != 1)
			{
				return 1m;
			}
			if (((RelicModel)this).Owner.Creature.GetPower<SoarPower>() == null)
			{
				return 1m;
			}
			CardPile pile = cardSource.Pile;
			if (pile != null && (int)pile.Type == 2)
			{
				return 1m;
			}
			return 1.5m;
		}

		private async Task LoseSoar()
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				SoarPower soar = ((RelicModel)this).Owner.Creature.GetPower<SoarPower>();
				if (soar != null)
				{
					await PowerCmd.Remove((PowerModel)(object)soar);
				}
			}
		}
	}
	public sealed class HeavenlyMandate : RelicModel
	{
		private const string _starsKey = "Stars";

		public override RelicRarity Rarity => (RelicRarity)1;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("Stars", 1m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/heavenly_mandate.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/heavenly_mandate.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/heavenly_mandate.png";

		public override async Task BeforeCombatStart()
		{
			if (((RelicModel)this).Owner != null)
			{
				await PlayerCmd.GainStars(2m, ((RelicModel)this).Owner);
			}
		}

		public override async Task AfterEnergyReset(Player player)
		{
			if (player == ((RelicModel)this).Owner)
			{
				((RelicModel)this).Flash();
				await PlayerCmd.GainStars((decimal)(int)((RelicModel)this).DynamicVars["Stars"].BaseValue, ((RelicModel)this).Owner);
			}
		}
	}
	public sealed class HecatesPhantom : RelicModel
	{
		private int _cardsPlayedThisTurn;

		public override string PackedIconPath => "res://wuwancients/images/relics/hecates_phantom.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/hecates_phantom.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/hecates_phantom.png";

		public override RelicRarity Rarity => (RelicRarity)7;

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<PlatingPower>((int?)null) };

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new PowerVar<PlatingPower>(7m) };

		public override async Task BeforeCombatStart()
		{
			_cardsPlayedThisTurn = 0;
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				((RelicModel)this).Flash();
				await PowerCmd.Apply<PlatingPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RelicModel)this).Owner.Creature, ((RelicModel)this).DynamicVars["PlatingPower"].BaseValue, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
			}
		}

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner)
			{
				_cardsPlayedThisTurn = 0;
			}
		}

		public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			if (cardPlay.Card.Owner != ((RelicModel)this).Owner)
			{
				return;
			}
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				_cardsPlayedThisTurn++;
				if (_cardsPlayedThisTurn >= 3)
				{
					_cardsPlayedThisTurn -= 3;
					((RelicModel)this).Flash();
					await PowerCmd.Apply<PlatingPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RelicModel)this).Owner.Creature, 1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
			}
		}
	}
	public sealed class HeGuangTongChang : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/he_guang_tong_chang.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/he_guang_tong_chang.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/he_guang_tong_chang.png";

		public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
		{
			if (card.Owner == ((RelicModel)this).Owner)
			{
				MakePlayable(card);
			}
			return Task.CompletedTask;
		}

		public override Task AfterCardEnteredCombat(CardModel card)
		{
			if (card.Owner == ((RelicModel)this).Owner)
			{
				MakePlayable(card);
			}
			return Task.CompletedTask;
		}

		public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return Task.CompletedTask;
			}
			foreach (CardModel card in ((RelicModel)this).Owner.PlayerCombatState.Hand.Cards)
			{
				MakePlayable(card);
			}
			return Task.CompletedTask;
		}

		public override CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocation cardLocation)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Invalid comparison between Unknown and I4
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			bool flag = card.Owner == ((RelicModel)this).Owner;
			bool flag2 = flag;
			if (flag2)
			{
				CardType type = card.Type;
				bool flag3 = type - 4 <= 1;
				flag2 = flag3;
			}
			if (flag2)
			{
				CardLocation result = cardLocation;
				result.pileType = (PileType)4;
				return result;
			}
			return cardLocation;
		}

		private static void MakePlayable(CardModel card)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Invalid comparison between Unknown and I4
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Invalid comparison between Unknown and I4
			CardType type = card.Type;
			if ((int)type == 5 || (int)type == 4)
			{
				card.RemoveKeyword((CardKeyword)4);
				card.EnergyCost.SetCustomBaseCost(0);
			}
		}
	}
	public sealed class HellhoundGreeting : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/hellhound_greeting.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/hellhound_greeting.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/hellhound_greeting.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
		{
			HoverTipFactory.FromCard<CanRen>(false),
			HoverTipFactory.FromCard<DeathWarrant>(false)
		};

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				CardModel cruelty = (CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<CanRen>(((RelicModel)this).Owner);
				await CardPileCmd.Add(cruelty, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
			}
		}
	}
	public sealed class HiddenSeaRecord : RelicModel
	{
		private static readonly Dictionary<ModelId, ModelId> UpgradeMap = new Dictionary<ModelId, ModelId>
		{
			{
				((AbstractModel)ModelDb.Relic<BurningBlood>()).Id,
				((AbstractModel)ModelDb.Relic<BrightBlood>()).Id
			},
			{
				((AbstractModel)ModelDb.Relic<RingOfTheSnake>()).Id,
				((AbstractModel)ModelDb.Relic<LongSnakeNecklace>()).Id
			},
			{
				((AbstractModel)ModelDb.Relic<DivineRight>()).Id,
				((AbstractModel)ModelDb.Relic<HeavenlyMandate>()).Id
			},
			{
				((AbstractModel)ModelDb.Relic<BoundPhylactery>()).Id,
				((AbstractModel)ModelDb.Relic<LostPhylactery>()).Id
			},
			{
				((AbstractModel)ModelDb.Relic<CrackedCore>()).Id,
				((AbstractModel)ModelDb.Relic<FrozenCore>()).Id
			}
		};

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/hidden_sea_record.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/hidden_sea_record.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/hidden_sea_record.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			RelicModel starter = ((IEnumerable<RelicModel>)((RelicModel)this).Owner.Relics).FirstOrDefault((Func<RelicModel, bool>)((RelicModel r) => (int)r.Rarity == 1));
			if (starter != null && UpgradeMap.TryGetValue(((AbstractModel)starter).Id, out ModelId upgradedId))
			{
				RelicModel byId = ModelDb.GetById<RelicModel>(upgradedId);
				RelicModel replacement = ((byId != null) ? byId.ToMutable() : null);
				if (replacement != null)
				{
					await RelicCmd.Replace(starter, replacement);
				}
			}
		}
	}
	public sealed class IfMeasuredInAnInstant : RelicModel
	{
		private sealed class CardDiscount
		{
			public readonly CardModel Card;

			public int Discount;

			public CardDiscount(CardModel card, int discount = 0)
			{
				Card = card;
				Discount = discount;
			}
		}

		private readonly List<CardDiscount> _discounts = new List<CardDiscount>();

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/if_measured_in_an_instant.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/if_measured_in_an_instant.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/if_measured_in_an_instant.png";

		public override Task BeforeCombatStart()
		{
			_discounts.Clear();
			return Task.CompletedTask;
		}

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return;
			}
			foreach (CardModel card in await CardSelectCmd.FromHand(choiceContext, ((RelicModel)this).Owner, new CardSelectorPrefs(new LocString("relics", "IF_MEASURED_IN_AN_INSTANT_SELECT_PROMPT"), 0, 2), (Func<CardModel, bool>)null, (AbstractModel)(object)this))
			{
				card.AddKeyword((CardKeyword)5);
			}
		}

		public override Task AfterFlush(PlayerChoiceContext choiceContext, Player player, IReadOnlyCollection<CardModel> flushedCards, IReadOnlyCollection<CardModel> retainedCards)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return Task.CompletedTask;
			}
			foreach (CardModel card in retainedCards)
			{
				CardDiscount cardDiscount = _discounts.Find((CardDiscount d) => d.Card == card);
				if (cardDiscount != null)
				{
					cardDiscount.Discount++;
				}
				else
				{
					_discounts.Add(new CardDiscount(card, 1));
				}
			}
			return Task.CompletedTask;
		}

		public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			CardPlay cardPlay2 = cardPlay;
			if (cardPlay2.Card.Owner != ((RelicModel)this).Owner)
			{
				return Task.CompletedTask;
			}
			_discounts.RemoveAll((CardDiscount d) => d.Card == cardPlay2.Card);
			return Task.CompletedTask;
		}

		public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
		{
			CardModel card2 = card;
			modifiedCost = originalCost;
			if (card2.Owner != ((RelicModel)this).Owner)
			{
				return false;
			}
			if (originalCost <= 0m)
			{
				return false;
			}
			CardDiscount cardDiscount = _discounts.Find((CardDiscount d) => d.Card == card2);
			if (cardDiscount == null || cardDiscount.Discount <= 0)
			{
				return false;
			}
			modifiedCost = Math.Max(0m, originalCost - (decimal)cardDiscount.Discount);
			return true;
		}
	}
	public sealed class InkBottle : RelicModel
	{
		private const int CardsPerDraw = 10;

		private int _cardsPlayed;

		public override RelicRarity Rarity => (RelicRarity)6;

		public override string PackedIconPath => "res://wuwancients/images/relics/ink_bottle.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/ink_bottle.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/ink_bottle.png";

		public override bool ShowCounter => CombatManager.Instance.IsInProgress;

		public override int DisplayAmount => _cardsPlayed % 10;

		public override Task BeforeCombatStart()
		{
			_cardsPlayed = 0;
			((RelicModel)this).InvokeDisplayAmountChanged();
			return Task.CompletedTask;
		}

		public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null)
			{
				return;
			}
			if (((cardPlay != null) ? cardPlay.Card : null) != null && cardPlay.Card.Owner == ((RelicModel)this).Owner && CombatManager.Instance.IsInProgress)
			{
				_cardsPlayed++;
				((RelicModel)this).InvokeDisplayAmountChanged();
				if (_cardsPlayed % 10 == 0)
				{
					((RelicModel)this).Flash();
					await CardPileCmd.Draw(choiceContext, 1m, ((RelicModel)this).Owner, false);
				}
			}
		}
	}
	public sealed class InsectSpecimen : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)6;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("HpLossPercent", 25m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/insect_specimen.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/insect_specimen.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/insect_specimen.png";

		private bool IsEliteCombat
		{
			get
			{
				//IL_0028: Unknown result type (might be due to invalid IL or missing references)
				//IL_002e: Invalid comparison between Unknown and I4
				Player owner = ((RelicModel)this).Owner;
				object obj;
				if (owner == null)
				{
					obj = null;
				}
				else
				{
					IRunState runState = owner.RunState;
					obj = ((runState != null) ? runState.CurrentRoom : null);
				}
				CombatRoom val = (CombatRoom)((obj is CombatRoom) ? obj : null);
				return val != null && (int)((AbstractRoom)val).RoomType == 2;
			}
		}

		public override async Task BeforeCombatStart()
		{
			Player owner = ((RelicModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				IRunState runState = owner.RunState;
				obj = ((runState != null) ? runState.CurrentRoom : null);
			}
			AbstractRoom val = (AbstractRoom)obj;
			CombatRoom combatRoom = (CombatRoom)(object)((val is CombatRoom) ? val : null);
			if (combatRoom == null || (int)((AbstractRoom)combatRoom).RoomType != 2)
			{
				return;
			}
			((RelicModel)this).Flash();
			foreach (Creature enemy in combatRoom.Enemies)
			{
				await ApplyHpLoss(enemy);
			}
		}

		public override async Task AfterCreatureAddedToCombat(Creature creature)
		{
			if (IsEliteCombat && creature.Side != ((RelicModel)this).Owner.Creature.Side)
			{
				((RelicModel)this).Flash();
				await ApplyHpLoss(creature);
			}
		}

		private async Task ApplyHpLoss(Creature enemy)
		{
			decimal loss = Math.Ceiling((decimal)enemy.CurrentHp * (((RelicModel)this).DynamicVars["HpLossPercent"].BaseValue / 100m));
			if (!(loss <= 0m))
			{
				await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), enemy, loss, (ValueProp)6, (Creature)null);
			}
		}
	}
	public sealed class JianxinGreeting : RelicModel
	{
		[HarmonyPatch(typeof(LiftRestSiteOption), "OnSelect")]
		public static class GiryaExercisePatch
		{
			public static void Postfix(LiftRestSiteOption __instance)
			{
				Player value = Traverse.Create((object)__instance).Property("Owner", (object[])null).GetValue<Player>();
				if (value != null && value.Relics.Any((RelicModel r) => r is JianxinGreeting))
				{
					TaskHelper.RunSafely(TransformBasicPairToUltimate(value));
				}
			}
		}

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override string PackedIconPath => "res://wuwancients/images/relics/jianxin_greeting.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/jianxin_greeting.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/jianxin_greeting.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			get
			{
				List<IHoverTip> list = new List<IHoverTip>();
				list.AddRange(HoverTipFactory.FromCardWithCardHoverTips<UltimateStrike>(false));
				list.AddRange(HoverTipFactory.FromCardWithCardHoverTips<UltimateDefend>(false));
				return list;
			}
		}

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				((RelicModel)this).Flash();
				await TransformBasicPairToUltimate(((RelicModel)this).Owner);
			}
		}

		private static async Task TransformBasicPairToUltimate(Player player)
		{
			object obj;
			if (player == null)
			{
				obj = null;
			}
			else
			{
				CardPile deck = player.Deck;
				obj = ((deck != null) ? deck.Cards : null);
			}
			if (obj != null)
			{
				CardModel strike = ((IEnumerable<CardModel>)player.Deck.Cards).FirstOrDefault((Func<CardModel, bool>)((CardModel c) => (int)c.Rarity == 1 && c.Tags.Contains((CardTag)1) && c.IsRemovable));
				CardModel defend = ((IEnumerable<CardModel>)player.Deck.Cards).FirstOrDefault((Func<CardModel, bool>)((CardModel c) => (int)c.Rarity == 1 && c.Tags.Contains((CardTag)2) && c.IsRemovable));
				List<CardTransformation> transformations = new List<CardTransformation>();
				if (strike != null)
				{
					transformations.Add(new CardTransformation(strike, (CardModel)(object)((ICardScope)player.RunState).CreateCard<UltimateStrike>(player)));
				}
				if (defend != null)
				{
					transformations.Add(new CardTransformation(defend, (CardModel)(object)((ICardScope)player.RunState).CreateCard<UltimateDefend>(player)));
				}
				if (transformations.Count != 0)
				{
					await CardCmd.Transform((IEnumerable<CardTransformation>)transformations, player.PlayerRng.Transformations, (CardPreviewStyle)1);
				}
			}
		}
	}
	public sealed class Knot : RelicModel
	{
		private decimal _gainedThisTurn;

		private decimal _gainedLastTurn;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/knot.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/knot.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/knot.png";

		public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
		{
			if (creature.IsPlayer && delta < 0m)
			{
				decimal gained = -delta * 2m;
				creature.GainBlockInternal(gained);
				_gainedThisTurn += gained;
			}
		}

		public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Invalid comparison between Unknown and I4
			if ((int)side == 1)
			{
				_gainedLastTurn = _gainedThisTurn;
				_gainedThisTurn = default(decimal);
			}
			return Task.CompletedTask;
		}

		public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
		{
			Player owner = ((RelicModel)this).Owner;
			if (creature == ((owner != null) ? owner.Creature : null))
			{
				_gainedThisTurn += amount;
			}
			return Task.CompletedTask;
		}

		public override bool ShouldClearBlock(Creature creature)
		{
			Player owner = ((RelicModel)this).Owner;
			if (creature == ((owner != null) ? owner.Creature : null))
			{
				return false;
			}
			return true;
		}

		public override Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
		{
			if (this != preventer)
			{
				return Task.CompletedTask;
			}
			Player owner = ((RelicModel)this).Owner;
			if (creature != ((owner != null) ? owner.Creature : null))
			{
				return Task.CompletedTask;
			}
			decimal num = Math.Min(_gainedLastTurn, creature.Block);
			decimal num2 = (decimal)creature.Block - num;
			if (num2 > 0m)
			{
				creature.LoseBlockInternal(num2);
			}
			return Task.CompletedTask;
		}
	}
	public sealed class Latte : RelicModel
	{
		private const int MaxTurns = 6;

		private int _turnsActive;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/latte.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/latte.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/latte.png";

		public override Task BeforeCombatStart()
		{
			_turnsActive = 0;
			return Task.CompletedTask;
		}

		public override decimal ModifyHandDraw(Player player, decimal originalCardCount)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return originalCardCount;
			}
			return (_turnsActive < 6) ? (originalCardCount + 2m) : (originalCardCount - 1m);
		}

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner)
			{
				if (_turnsActive < 6)
				{
					await PlayerCmd.GainEnergy(1m, player);
				}
				_turnsActive++;
			}
		}
	}
	public sealed class LightlyTossedThoughts : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/lightly_tossed_thoughts.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/lightly_tossed_thoughts.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/lightly_tossed_thoughts.png";

		public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			if (cardPlay.Player != ((RelicModel)this).Owner || (int)cardPlay.Card.Type != 1)
			{
				return;
			}
			int bonusDamage = ((RelicModel)this).Owner.PlayerRng.Rewards.NextInt(1, 13);
			Creature target = cardPlay.Target;
			if (target != null && target.IsAlive)
			{
				await CreatureCmd.Damage(choiceContext, (IEnumerable<Creature>)new <>z__ReadOnlySingleElementList<Creature>(target), (decimal)bonusDamage, (ValueProp)12, ((RelicModel)this).Owner.Creature);
			}
			else if (target == null)
			{
				Creature creature = ((RelicModel)this).Owner.Creature;
				object obj;
				if (creature == null)
				{
					obj = null;
				}
				else
				{
					ICombatState combatState = creature.CombatState;
					obj = ((combatState != null) ? combatState.HittableEnemies : null);
				}
				IReadOnlyList<Creature> enemies = (IReadOnlyList<Creature>)obj;
				if (enemies != null && enemies.Count > 0)
				{
					await CreatureCmd.Damage(choiceContext, (IEnumerable<Creature>)enemies.ToList(), (decimal)bonusDamage, (ValueProp)12, ((RelicModel)this).Owner.Creature);
				}
			}
		}
	}
	public sealed class LinaGreeting : RelicModel
	{
		private const string _roomsKey = "Rooms";

		private const int BaseCharges = 2;

		private int _charges;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool IsUsedUp => false;

		public override bool ShowCounter => true;

		public override int DisplayAmount => Charges;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("Rooms", 2m)
		};

		[SavedProperty]
		public int Charges
		{
			get
			{
				return _charges;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_charges = ((value >= 0) ? value : 0);
				((RelicModel)this).DynamicVars["Rooms"].BaseValue = _charges;
				((RelicModel)this).InvokeDisplayAmountChanged();
			}
		}

		public override string PackedIconPath => "res://wuwancients/images/relics/lina_greeting.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/lina_greeting.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/lina_greeting.png";

		public override Task AfterObtained()
		{
			Charges = 2;
			return Task.CompletedTask;
		}

		public override bool ShouldAllowFreeTravel()
		{
			return Charges > 0;
		}

		public override Task AfterRoomEntered(AbstractRoom room)
		{
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			if (Charges <= 0)
			{
				return Task.CompletedTask;
			}
			if (((RelicModel)this).Owner.RunState.CurrentRoomCount > 1)
			{
				return Task.CompletedTask;
			}
			IRunState runState = ((RelicModel)this).Owner.RunState;
			RunState val = (RunState)(object)((runState is RunState) ? runState : null);
			if (val == null)
			{
				return Task.CompletedTask;
			}
			if (val.VisitedMapCoords.Count <= 1)
			{
				return Task.CompletedTask;
			}
			IReadOnlyList<MapCoord> visitedMapCoords = val.VisitedMapCoords;
			MapCoord val2 = visitedMapCoords[visitedMapCoords.Count - 2];
			MapPoint point = val.Map.GetPoint(val2);
			if (point == null)
			{
				return Task.CompletedTask;
			}
			MapPoint currentMapPoint = ((RelicModel)this).Owner.RunState.CurrentMapPoint;
			if (currentMapPoint == null)
			{
				return Task.CompletedTask;
			}
			if (point.Children.Contains(currentMapPoint))
			{
				return Task.CompletedTask;
			}
			Charges--;
			return Task.CompletedTask;
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Invalid comparison between Unknown and I4
			if (((room != null) ? room.Encounter : null) == null)
			{
				return Task.CompletedTask;
			}
			if ((int)room.Encounter.RoomType != 2)
			{
				return Task.CompletedTask;
			}
			Charges++;
			((RelicModel)this).Flash();
			return Task.CompletedTask;
		}
	}
	public sealed class LingYinGreeting : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override string PackedIconPath => "res://wuwancients/images/relics/ling_yin_greeting.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/ling_yin_greeting.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/ling_yin_greeting.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			get
			{
				//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
				//IL_00da: Expected O, but got Unknown
				List<IHoverTip> list = new List<IHoverTip>();
				if (((AbstractModel)this).IsMutable)
				{
					Player owner = ((RelicModel)this).Owner;
					if (((owner != null) ? owner.Character : null) != null)
					{
						Player owner2 = ((RelicModel)this).Owner;
						List<CardModel> list2 = (from c in owner2.Character.StartingDeck
							where !c.Tags.Contains((CardTag)1) && !c.Tags.Contains((CardTag)2)
							group c by ((object)c).GetType() into g
							select g.First()).ToList();
						foreach (CardModel item in list2)
						{
							CardModel val = (CardModel)((AbstractModel)item).MutableClone();
							CardCmd.Upgrade(val, (CardPreviewStyle)1);
							list.Add(HoverTipFactory.FromCard(val, false));
						}
						return list;
					}
				}
				return list;
			}
		}

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			Player player = ((RelicModel)this).Owner;
			HashSet<Type> starterTypes = player.Character.StartingDeck.Select((CardModel c) => ((object)c).GetType()).ToHashSet();
			CardPile deck = player.Deck;
			IEnumerable<CardModel> enumerable = ((deck != null) ? deck.Cards : null);
			List<CardModel> targets = (from c in enumerable ?? Enumerable.Empty<CardModel>()
				where starterTypes.Contains(((object)c).GetType())
				where !c.Tags.Contains((CardTag)1) && !c.Tags.Contains((CardTag)2)
				select c).ToList();
			foreach (CardModel card in targets)
			{
				CardCmd.Upgrade(card, (CardPreviewStyle)1);
			}
			await Task.CompletedTask;
		}
	}
	public sealed class LinnaDango : RelicModel
	{
		private const int ExtraPlays = 1;

		private int _cardsPlayedThisTurn;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/linna_dango.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/linna_dango.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/linna_dango.png";

		public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner)
			{
				_cardsPlayedThisTurn = 0;
			}
			return Task.CompletedTask;
		}

		public override CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocation cardLocation)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Invalid comparison between Unknown and I4
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			if (card.Owner != ((RelicModel)this).Owner)
			{
				return cardLocation;
			}
			if (_cardsPlayedThisTurn != 0)
			{
				return cardLocation;
			}
			if ((int)card.Type == 3 || card.IsDupe)
			{
				return cardLocation;
			}
			CardLocation result = cardLocation;
			result.pileType = (PileType)4;
			return result;
		}

		public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Invalid comparison between Unknown and I4
			if (card.Owner != ((RelicModel)this).Owner)
			{
				return playCount;
			}
			if (_cardsPlayedThisTurn != 1)
			{
				return playCount;
			}
			CardPile pile = card.Pile;
			if (pile == null || (int)pile.Type != 5)
			{
				return playCount;
			}
			return playCount + 1;
		}

		public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			if (cardPlay.Card.Owner != ((RelicModel)this).Owner || !cardPlay.IsLastInSeries)
			{
				return Task.CompletedTask;
			}
			_cardsPlayedThisTurn++;
			if (_cardsPlayedThisTurn <= 2)
			{
				((RelicModel)this).Flash();
			}
			return Task.CompletedTask;
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			_cardsPlayedThisTurn = 0;
			return Task.CompletedTask;
		}
	}
	public sealed class LongSnakeNecklace : RelicModel
	{
		private bool _hasUsedEffect;

		public override RelicRarity Rarity => (RelicRarity)1;

		public override string PackedIconPath => "res://wuwancients/images/relics/long_snake_necklace.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/long_snake_necklace.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/long_snake_necklace.png";

		public override Task BeforeCombatStart()
		{
			_hasUsedEffect = false;
			return Task.CompletedTask;
		}

		public override decimal ModifyHandDraw(Player player, decimal count)
		{
			if (player == ((RelicModel)this).Owner)
			{
				Creature creature = player.Creature;
				if (((creature != null) ? creature.CombatState : null) != null)
				{
					if (player.Creature.CombatState.RoundNumber > 1)
					{
						return count;
					}
					return count + 4m;
				}
			}
			return count;
		}

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner && !_hasUsedEffect)
			{
				_hasUsedEffect = true;
				List<CardModel> discardCards = (await CardSelectCmd.FromHandForDiscard(choiceContext, ((RelicModel)this).Owner, new CardSelectorPrefs(((RelicModel)this).SelectionScreenPrompt, 0, 999999999), (Func<CardModel, bool>)null, (AbstractModel)(object)this)).ToList();
				if (discardCards.Count > 0)
				{
					await CardCmd.DiscardAndDraw(choiceContext, (IEnumerable<CardModel>)discardCards, discardCards.Count);
				}
			}
		}
	}
	public sealed class LongSummerFlower : RelicModel
	{
		private const int TurnsThreshold = 6;

		private bool _pendingExtraTurn;

		private int _turnsSeen;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool ShowCounter => true;

		[SavedProperty]
		public int TurnsSeen
		{
			get
			{
				return _turnsSeen;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_turnsSeen = value;
				((RelicModel)this).InvokeDisplayAmountChanged();
			}
		}

		[SavedProperty]
		public bool PendingExtraTurn
		{
			get
			{
				return _pendingExtraTurn;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_pendingExtraTurn = value;
			}
		}

		public override int DisplayAmount => 6 - TurnsSeen % 6;

		public override string PackedIconPath => "res://wuwancients/images/relics/long_summer_flower.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/long_summer_flower.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/long_summer_flower.png";

		public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if (participants.Contains(((RelicModel)this).Owner.Creature))
			{
				TurnsSeen++;
				if (TurnsSeen % 6 == 0)
				{
					PendingExtraTurn = true;
					((RelicModel)this).Flash();
				}
			}
		}

		public override bool ShouldTakeExtraTurn(Player player)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return false;
			}
			if (PendingExtraTurn)
			{
				PendingExtraTurn = false;
				return true;
			}
			return false;
		}
	}
	public sealed class LostPhylactery : RelicModel
	{
		private const string _startOfCombatKey = "StartOfCombat";

		private const string _startOfTurnKey = "StartOfTurn";

		public override RelicRarity Rarity => (RelicRarity)1;

		public override bool SpawnsPets => true;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			(DynamicVar)new SummonVar("StartOfCombat", 1m),
			(DynamicVar)new SummonVar("StartOfTurn", 5m)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.Static((StaticHoverTip)13, Array.Empty<DynamicVar>()) };

		public override string PackedIconPath => "res://wuwancients/images/relics/lost_phylactery.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/lost_phylactery.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/lost_phylactery.png";

		public override async Task BeforeCombatStart()
		{
			await OstyCmd.Summon((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RelicModel)this).Owner, ((RelicModel)this).DynamicVars["StartOfCombat"].BaseValue, (AbstractModel)(object)this);
		}

		public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if ((int)side == 1)
			{
				await OstyCmd.Summon((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RelicModel)this).Owner, ((RelicModel)this).DynamicVars["StartOfTurn"].BaseValue, (AbstractModel)(object)this);
			}
		}
	}
	public sealed class MemoryStarAnchor : RelicModel
	{
		private const decimal HpLoss = 9m;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override bool ShowCounter => false;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("HpLoss", 9m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/memory_star_anchor.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/memory_star_anchor.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/memory_star_anchor.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				RelicModel rareRelic = RelicFactory.PullNextRelicFromFront(((RelicModel)this).Owner, (RelicRarity)4).ToMutable();
				await RelicCmd.Obtain(rareRelic, ((RelicModel)this).Owner, -1);
				if (((RelicModel)this).Owner.Creature != null)
				{
					await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RelicModel)this).Owner.Creature, 9m, (ValueProp)6, (CardModel)null, (CardPlay)null);
				}
			}
		}
	}
	public sealed class MiniTone : RelicModel
	{
		private int _attacksPlayedThisTurn;

		public override RelicRarity Rarity => (RelicRarity)6;

		public override string PackedIconPath => "res://wuwancients/images/relics/mini_tone.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/mini_tone.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/mini_tone.png";

		public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return Task.CompletedTask;
			}
			_attacksPlayedThisTurn = 0;
			return Task.CompletedTask;
		}

		public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Invalid comparison between Unknown and I4
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Invalid comparison between Unknown and I4
			if (card.Owner != ((RelicModel)this).Owner)
			{
				return playCount;
			}
			if ((int)card.Type != 1)
			{
				return playCount;
			}
			if (_attacksPlayedThisTurn > 0)
			{
				return playCount;
			}
			CardPile pile = card.Pile;
			if (pile == null || (int)pile.Type != 5)
			{
				return playCount;
			}
			return playCount + 1;
		}

		public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Invalid comparison between Unknown and I4
			if (cardPlay.Card == null || cardPlay.Card.Owner != ((RelicModel)this).Owner)
			{
				return Task.CompletedTask;
			}
			if ((int)cardPlay.Card.Type == 1 && cardPlay.IsLastInSeries)
			{
				_attacksPlayedThisTurn++;
			}
			return Task.CompletedTask;
		}

		public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
		{
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Invalid comparison between Unknown and I4
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null || dealer != ((RelicModel)this).Owner.Creature)
			{
				return 1m;
			}
			if (cardSource == null || cardSource.Owner != ((RelicModel)this).Owner)
			{
				return 1m;
			}
			if (_attacksPlayedThisTurn == 0 && (int)cardSource.Type == 1)
			{
				return 0.5m;
			}
			return 1m;
		}
	}
	public sealed class MissetFallacy : RelicModel
	{
		private const string _combatsKey = "Combats";

		private int _combatsSeen;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override int DisplayAmount => CombatsSeen % ((RelicModel)this).DynamicVars["Combats"].IntValue;

		public override bool ShowCounter => true;

		[SavedProperty]
		public int CombatsSeen
		{
			get
			{
				return _combatsSeen;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_combatsSeen = value;
				((RelicModel)this).InvokeDisplayAmountChanged();
			}
		}

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("Combats", 4m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/misset_fallacy.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/misset_fallacy.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/misset_fallacy.png";

		public override Task AfterCombatEnd(CombatRoom room)
		{
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			CombatsSeen++;
			if (CombatsSeen % ((RelicModel)this).DynamicVars["Combats"].IntValue == 0)
			{
				((RelicModel)this).Flash();
				List<CardModel> list = ((RelicModel)this).Owner.Deck.Cards.Where((CardModel c) => c.IsUpgradable).ToList();
				if (list.Count == 0)
				{
					return Task.CompletedTask;
				}
				CardRarity maxRarity = list.Max((CardModel c) => c.Rarity);
				List<CardModel> list2 = list.Where((CardModel c) => c.Rarity == maxRarity).ToList();
				CardModel val = ((RelicModel)this).Owner.RunState.Rng.Niche.NextItem<CardModel>((IEnumerable<CardModel>)list2);
				if (val != null)
				{
					CardCmd.Upgrade(val, (CardPreviewStyle)1);
				}
			}
			return Task.CompletedTask;
		}

		public override Task BeforeCombatStart()
		{
			return Task.CompletedTask;
		}
	}
	public sealed class MonasticHat : RelicModel
	{
		[CompilerGenerated]
		private sealed class <get_ExtraHoverTips>d__24 : IEnumerable<IHoverTip>, IEnumerable, IEnumerator<IHoverTip>, IEnumerator, IDisposable
		{
			private int <>1__state;

			private IHoverTip <>2__current;

			private int <>l__initialThreadId;

			public MonasticHat <>4__this;

			IHoverTip IEnumerator<IHoverTip>.Current
			{
				[DebuggerHidden]
				get
				{
					return <>2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return <>2__current;
				}
			}

			[DebuggerHidden]
			public <get_ExtraHoverTips>d__24(int <>1__state)
			{
				this.<>1__state = <>1__state;
				<>l__initialThreadId = Environment.CurrentManagedThreadId;
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
				<>1__state = -2;
			}

			private bool MoveNext()
			{
				switch (<>1__state)
				{
				default:
					return false;
				case 0:
					<>1__state = -1;
					<>2__current = HoverTipFactory.FromPower<NoHatPower>((int?)null);
					<>1__state = 1;
					return true;
				case 1:
					<>1__state = -1;
					<>2__current = HoverTipFactory.FromPower<HasHatPower>((int?)null);
					<>1__state = 2;
					return true;
				case 2:
					<>1__state = -1;
					return false;
				}
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
				throw new NotSupportedException();
			}

			[DebuggerHidden]
			IEnumerator<IHoverTip> IEnumerable<IHoverTip>.GetEnumerator()
			{
				<get_ExtraHoverTips>d__24 result;
				if (<>1__state == -2 && <>l__initialThreadId == Environment.CurrentManagedThreadId)
				{
					<>1__state = 0;
					result = this;
				}
				else
				{
					result = new <get_ExtraHoverTips>d__24(0)
					{
						<>4__this = <>4__this
					};
				}
				return result;
			}

			[DebuggerHidden]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<IHoverTip>)this).GetEnumerator();
			}
		}

		private const string PetScenePath = "res://wuwancients/scenes/monastic_pet.tscn";

		private const string PetAnimNodeName = "Sprite";

		public const string NoHatIdleAnim = "no_hat_idle";

		public const string HasHatIdleAnim = "has_hat_idle";

		public const string HasToNoHatAnim = "has_to_no_hat";

		public const string NoToHasHatAnim = "no_to_has_hat";

		private const string HasHatSfx = "res://wuwancients/audio/monastic_hat_has.wav";

		private const string NoHatSfx = "res://wuwancients/audio/monastic_hat_no.wav";

		private AnimatedSprite2D? _petAnim;

		private bool _isHasHatForm;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool AddsPet => true;

		public override string PackedIconPath => "res://wuwancients/images/relics/monastic_hat.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/monastic_hat.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/monastic_hat.png";

		[SavedProperty]
		public bool IsHasHatForm
		{
			get
			{
				return _isHasHatForm;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_isHasHatForm = value;
			}
		}

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			[IteratorStateMachine(typeof(<get_ExtraHoverTips>d__24))]
			get
			{
				//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
				<get_ExtraHoverTips>d__24 <get_ExtraHoverTips>d__ = new <get_ExtraHoverTips>d__24(-2);
				<get_ExtraHoverTips>d__.<>4__this = this;
				return <get_ExtraHoverTips>d__;
			}
		}

		public override bool IsAllowed(IRunState runState)
		{
			if (((IPlayerCollection)runState).Players.Any((Player p) => p.HasEventPet()))
			{
				return false;
			}
			return ((RelicModel)this).IsAllowed(runState);
		}

		public override async Task AfterObtained()
		{
			await <>n__0();
			if (CombatManager.Instance.IsInProgress)
			{
				await EnsurePetVisual();
				await SyncForm((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), playTransform: false);
			}
		}

		public override async Task BeforeCombatStart()
		{
			await EnsurePetVisual();
			await SyncForm((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), playTransform: false);
		}

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner && ((RelicModel)this).Owner.Creature != null)
			{
				await SyncForm(choiceContext, playTransform: true);
			}
		}

		public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null && target == ((RelicModel)this).Owner.Creature)
			{
				await SyncForm(choiceContext, playTransform: false);
			}
		}

		private async Task SyncForm(PlayerChoiceContext choiceContext, bool playTransform)
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null)
			{
				return;
			}
			bool shouldHasHat = ((RelicModel)this).Owner.Creature.CurrentHp <= ((RelicModel)this).Owner.Creature.MaxHp / 2;
			NoHatPower no = ((RelicModel)this).Owner.Creature.GetPower<NoHatPower>();
			HasHatPower has = ((RelicModel)this).Owner.Creature.GetPower<HasHatPower>();
			if (shouldHasHat)
			{
				if (no != null)
				{
					await PowerCmd.Remove((PowerModel)(object)no);
				}
				if (has == null)
				{
					await PowerCmd.Apply<HasHatPower>(choiceContext, ((RelicModel)this).Owner.Creature, 1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
			}
			else
			{
				if (has != null)
				{
					await PowerCmd.Remove((PowerModel)(object)has);
				}
				if (no == null)
				{
					await PowerCmd.Apply<NoHatPower>(choiceContext, ((RelicModel)this).Owner.Creature, 1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
			}
			if (IsHasHatForm != shouldHasHat)
			{
				IsHasHatForm = shouldHasHat;
				if (playTransform)
				{
					PlayTransform();
					return;
				}
				PlayIdleAnim();
				PlayFormAudio();
			}
			else if (!playTransform)
			{
				PlayIdleAnim();
				PlayFormAudio();
			}
		}

		private async Task EnsurePetVisual()
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null)
			{
				return;
			}
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((RelicModel)this).Owner.Creature) : null);
			if (creatureNode != null && !GodotObject.IsInstanceValid((GodotObject)(object)_petAnim))
			{
				PackedScene packed = GD.Load<PackedScene>("res://wuwancients/scenes/monastic_pet.tscn");
				if (packed == null)
				{
					GD.PushWarning("MonasticHat: could not load pet scene 'res://wuwancients/scenes/monastic_pet.tscn'");
					return;
				}
				Node2D node = packed.Instantiate<Node2D>((GenEditState)0);
				((Node)creatureNode).AddChild((Node)(object)node, false, (InternalMode)0);
				_petAnim = ((Node)node).GetNodeOrNull<AnimatedSprite2D>(NodePath.op_Implicit("Sprite"));
				await Task.CompletedTask;
			}
		}

		private void PlayTransform()
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)_petAnim))
			{
				string text = (IsHasHatForm ? "no_to_has_hat" : "has_to_no_hat");
				SpriteFrames spriteFrames = _petAnim.SpriteFrames;
				if (spriteFrames == null || !spriteFrames.HasAnimation(StringName.op_Implicit(text)))
				{
					PlayIdleAnim();
					PlayFormAudio();
				}
				else
				{
					_petAnim.Play(StringName.op_Implicit(text), 1f, false);
					PlayFormAudio();
					TaskHelper.RunSafely(ReturnToIdleAfterTransform(text));
				}
			}
		}

		private async Task ReturnToIdleAfterTransform(string anim)
		{
			AnimatedSprite2D sprite = _petAnim;
			if (GodotObject.IsInstanceValid((GodotObject)(object)sprite))
			{
				await ((GodotObject)sprite).ToSignal((GodotObject)(object)sprite, SignalName.AnimationFinished);
				if (GodotObject.IsInstanceValid((GodotObject)(object)_petAnim) && !(((object)_petAnim.Animation).ToString() != anim))
				{
					PlayIdleAnim();
				}
			}
		}

		private void PlayIdleAnim()
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)_petAnim))
			{
				string text = (IsHasHatForm ? "has_hat_idle" : "no_hat_idle");
				SpriteFrames spriteFrames = _petAnim.SpriteFrames;
				if (spriteFrames != null && spriteFrames.HasAnimation(StringName.op_Implicit(text)))
				{
					_petAnim.Play(StringName.op_Implicit(text), 1f, false);
				}
			}
		}

		private void PlayFormAudio()
		{
			string text = (IsHasHatForm ? "res://wuwancients/audio/monastic_hat_has.wav" : "res://wuwancients/audio/monastic_hat_no.wav");
			if (ResourceLoader.Exists(text, ""))
			{
				NAudioManager instance = NAudioManager.Instance;
				if (instance != null)
				{
					instance.PlayOneShot(text, 1f);
				}
			}
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			return Task.CompletedTask;
		}

		[CompilerGenerated]
		[DebuggerHidden]
		private Task <>n__0()
		{
			return ((RelicModel)this).AfterObtained();
		}
	}
	public sealed class MoonstoneBracelet : RelicModel
	{
		private int _subTurns;

		[SavedProperty]
		public int SubTurns
		{
			get
			{
				return _subTurns;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_subTurns = value;
				((RelicModel)this).InvokeDisplayAmountChanged();
			}
		}

		public override bool ShowCounter => true;

		public override int DisplayAmount => 3 - SubTurns;

		protected override IEnumerable<DynamicVar> CanonicalVars => Enumerable.Empty<DynamicVar>();

		protected override IEnumerable<IHoverTip> ExtraHoverTips => Enumerable.Empty<IHoverTip>();

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/moonstone_bracelet.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/moonstone_bracelet.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/moonstone_bracelet.png";

		public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if (side == ((RelicModel)this).Owner.Creature.Side)
			{
				_subTurns++;
				if (SubTurns >= 3)
				{
					SubTurns = 0;
					((RelicModel)this).Flash();
					await PowerCmd.Apply<BufferPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RelicModel)this).Owner.Creature, 1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
				((RelicModel)this).InvokeDisplayAmountChanged();
			}
		}

		public override Task AfterCombatEnd(CombatRoom _)
		{
			return Task.CompletedTask;
		}
	}
	public sealed class MoonviewFlower : RelicModel
	{
		private static readonly SavedSpireField<CardModel, bool> FreeThisCombat = new SavedSpireField<CardModel, bool>((Func<bool>)(() => false), "wuwancients_moonview_first_turn");

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/moonview_flower.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/moonview_flower.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/moonview_flower.png";

		private bool IsFirstTurn
		{
			get
			{
				Player owner = ((RelicModel)this).Owner;
				int? obj;
				if (owner == null)
				{
					obj = null;
				}
				else
				{
					PlayerCombatState playerCombatState = owner.PlayerCombatState;
					obj = ((playerCombatState != null) ? new int?(playerCombatState.TurnNumber) : null);
				}
				int? num = obj;
				return num.GetValueOrDefault() <= 1;
			}
		}

		public override Task BeforeCombatStart()
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Deck : null) != null)
			{
				foreach (CardModel card in ((RelicModel)this).Owner.Deck.Cards)
				{
					((SpireField<CardModel, bool>)(object)FreeThisCombat)[card] = false;
				}
			}
			return Task.CompletedTask;
		}

		public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
		{
			if (card.Owner != ((RelicModel)this).Owner)
			{
				return Task.CompletedTask;
			}
			if (!IsFirstTurn)
			{
				return Task.CompletedTask;
			}
			if (!WuwancientsConfig.见月花首回合抽牌全免费 && !fromHandDraw)
			{
				return Task.CompletedTask;
			}
			((SpireField<CardModel, bool>)(object)FreeThisCombat)[card] = true;
			((RelicModel)this).Flash();
			return Task.CompletedTask;
		}

		public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
		{
			if (card.Owner != ((RelicModel)this).Owner || originalCost <= 0m || !((SpireField<CardModel, bool>)(object)FreeThisCombat)[card])
			{
				modifiedCost = originalCost;
				return false;
			}
			modifiedCost = default(decimal);
			return true;
		}

		public override bool TryModifyStarCost(CardModel card, decimal cost, out decimal modifiedCost)
		{
			if (card.Owner == ((RelicModel)this).Owner && cost > 0m && ((SpireField<CardModel, bool>)(object)FreeThisCombat)[card])
			{
				modifiedCost = default(decimal);
				return true;
			}
			modifiedCost = cost;
			return false;
		}
	}
	public sealed class MottaliBankCard : RelicModel
	{
		private const decimal GoldOnPickup = 400m;

		private const decimal MaxHpLoss = 8m;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override bool ShowCounter => false;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("GoldAmount", 400m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/mottali_bank_card.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/mottali_bank_card.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/mottali_bank_card.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				await PlayerCmd.GainGold(400m, ((RelicModel)this).Owner, false);
				if (((RelicModel)this).Owner.Creature != null)
				{
					await CreatureCmd.LoseMaxHp((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RelicModel)this).Owner.Creature, 8m, false);
				}
			}
		}
	}
	public sealed class MottaliGift : RelicModel
	{
		private const int GoldThreshold = 100;

		private const int GoldToGive = 100;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool ShowCounter => false;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			new DynamicVar("GoldThreshold", 100m),
			new DynamicVar("GoldAmount", 100m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/mottali_gift.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/mottali_gift.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/mottali_gift.png";

		public override async Task BeforeCombatStart()
		{
			if (((RelicModel)this).Owner != null && ((RelicModel)this).Owner.Gold <= 100)
			{
				((RelicModel)this).Flash();
				await PlayerCmd.GainGold(100m, ((RelicModel)this).Owner, false);
			}
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			return Task.CompletedTask;
		}
	}
	public sealed class NamelessShadowHusk : RelicModel
	{
		private const int TurnsPerIntangible = 6;

		[SavedProperty]
		public int SubTurns { get; set; }

		public override bool ShowCounter => true;

		public override int DisplayAmount => 6 - SubTurns;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/nameless_shadow_husk.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/nameless_shadow_husk.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/nameless_shadow_husk.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<IntangiblePower>((int?)null) };

		public override decimal ModifyMaxEnergy(Player player, decimal amount)
		{
			if (player == ((RelicModel)this).Owner)
			{
				return amount + 1m;
			}
			return amount;
		}

		public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null || side != ((RelicModel)this).Owner.Creature.Side)
			{
				return;
			}
			SubTurns++;
			if (SubTurns >= 6)
			{
				SubTurns = 0;
				((RelicModel)this).Flash();
				List<Creature> enemies = (from c in combatState.GetCreaturesOnSide((CombatSide)2)
					where c.IsAlive
					select c).ToList();
				if (enemies.Count > 0)
				{
					await PowerCmd.Apply<IntangiblePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), (IEnumerable<Creature>)enemies, 1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
			}
			((RelicModel)this).InvokeDisplayAmountChanged();
		}
	}
	public sealed class NewWorldCarnival : RelicModel
	{
		private ModelId? _ancientCard;

		private IEnumerable<IHoverTip> _extraHoverTips = Array.Empty<IHoverTip>();

		public override RelicRarity Rarity => (RelicRarity)7;

		[SavedProperty]
		public ModelId? AncientCard
		{
			get
			{
				return _ancientCard;
			}
			set
			{
				//IL_0053: Unknown result type (might be due to invalid IL or missing references)
				((AbstractModel)this).AssertMutable();
				_ancientCard = value;
				if (value != (ModelId)null)
				{
					CardModel val = SaveUtil.CardOrDeprecated(value);
					_extraHoverTips = val.HoverTips.Concat((IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromCard(val, true) });
					((StringVar)((RelicModel)this).DynamicVars["AncientCard"]).StringValue = val.Title;
				}
			}
		}

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new StringVar[1]
		{
			new StringVar("AncientCard", "")
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			get
			{
				if (_ancientCard == (ModelId)null)
				{
					try
					{
						if (((RelicModel)this).Owner != null)
						{
							CardModel targetCardForPlayer = GetTargetCardForPlayer();
							if (targetCardForPlayer != null)
							{
								AncientCard = ((AbstractModel)targetCardForPlayer).Id;
							}
						}
					}
					catch
					{
					}
				}
				return _extraHoverTips;
			}
		}

		public override string PackedIconPath => "res://wuwancients/images/relics/new_world_carnival.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/new_world_carnival.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/new_world_carnival.png";

		private CardModel? GetTargetCardForPlayer()
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Character : null) == null)
			{
				return null;
			}
			Type type = ((object)owner.Character).GetType();
			if (type == typeof(Ironclad))
			{
				return (CardModel?)(object)ModelDb.Card<FuryUnleashed>();
			}
			if (type == typeof(Necrobinder))
			{
				return (CardModel?)(object)ModelDb.Card<PredestinedDeath>();
			}
			if (type == typeof(Regent))
			{
				return (CardModel?)(object)ModelDb.Card<Wormhole>();
			}
			if (type == typeof(Defect))
			{
				return (CardModel?)(object)ModelDb.Card<IntelligentCreation>();
			}
			if (type == typeof(Silent))
			{
				return (CardModel?)(object)ModelDb.Card<ForeseeFuture>();
			}
			if (((AbstractModel)owner.Character).Id.Entry == "WATCHER-WATCHER")
			{
				return ModelDb.GetById<CardModel>(ModelId.Deserialize("CARD.WATCHER-ANCIENT_CARD"));
			}
			return null;
		}

		public override async Task AfterObtained()
		{
			Player player = ((RelicModel)this).Owner;
			if (player != null)
			{
				CardModel targetCard = GetTargetCardForPlayer();
				if (targetCard != null)
				{
					AncientCard = ((AbstractModel)targetCard).Id;
					CardModel card = ((ICardScope)player.RunState).CreateCard(targetCard, player);
					CardCmd.Upgrade(card, (CardPreviewStyle)1);
					CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 2f, (CardPreviewStyle)1);
				}
			}
		}
	}
	public sealed class NuoNuoChaoFan : RelicModel
	{
		private bool _triggerPending;

		private int _remainUses = 2;

		public override RelicRarity Rarity => (RelicRarity)6;

		public override bool ShowCounter => true;

		public override int DisplayAmount => _remainUses;

		public override string PackedIconPath => "res://wuwancients/images/relics/nuo_nuo_chao_fan.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/nuo_nuo_chao_fan.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/nuo_nuo_chao_fan.png";

		public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
			Player owner = ((RelicModel)this).Owner;
			if (target != ((owner != null) ? owner.Creature : null))
			{
				return amount;
			}
			if (_remainUses <= 0)
			{
				return amount;
			}
			if (target.CurrentHp <= 0 || amount < (decimal)target.CurrentHp)
			{
				return amount;
			}
			_triggerPending = true;
			return (decimal)target.CurrentHp - 1m;
		}

		public override async Task AfterModifyingHpLostAfterOsty()
		{
			if (_triggerPending)
			{
				_triggerPending = false;
				_remainUses--;
				if (_remainUses <= 0)
				{
					((RelicModel)this).Status = (RelicStatus)2;
				}
				((RelicModel)this).Flash();
				((RelicModel)this).InvokeDisplayAmountChanged();
				((RelicModel)this).Owner.Creature.SetCurrentHpInternal(9m);
				await PowerCmd.Apply<VulnerablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RelicModel)this).Owner.Creature, 2m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
			}
		}
	}
	public sealed class OldHairband : RelicModel
	{
		private int _attacksThisTurn;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/old_hairband.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/old_hairband.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/old_hairband.png";

		public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return Task.CompletedTask;
			}
			_attacksThisTurn = 0;
			return Task.CompletedTask;
		}

		public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			Player owner = ((RelicModel)this).Owner;
			if (dealer != ((owner != null) ? owner.Creature : null))
			{
				return;
			}
			decimal? num = ((results != null) ? new int?(results.TotalDamage) : null);
			if (!((num.GetValueOrDefault() <= default(decimal)) & num.HasValue))
			{
				if (cardSource != null && (int)cardSource.Type == 1 && _attacksThisTurn < 2)
				{
					_attacksThisTurn++;
					decimal block = (decimal)results.TotalDamage / 2m;
					await CreatureCmd.GainBlock(dealer, block, (ValueProp)8, (CardPlay)null, false);
				}
			}
		}
	}
	public sealed class OldMemories : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)6;

		public override string PackedIconPath => "res://wuwancients/images/relics/old_memories.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/old_memories.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/old_memories.png";
	}
	public sealed class OldPhotoAlbum : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)6;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new BlockVar(3m, (ValueProp)4) };

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.Static((StaticHoverTip)5, Array.Empty<DynamicVar>()) };

		public override string PackedIconPath => "res://wuwancients/images/relics/old_photo_album.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/old_photo_album.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/old_photo_album.png";

		public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			if (((cardPlay != null) ? cardPlay.Card : null) != null && cardPlay.Card.Owner == ((RelicModel)this).Owner && (int)cardPlay.Card.Type == 3)
			{
				Player owner = ((RelicModel)this).Owner;
				if (((owner != null) ? owner.Creature : null) != null)
				{
					((RelicModel)this).Flash();
					await CreatureCmd.GainBlock(((RelicModel)this).Owner.Creature, ((RelicModel)this).DynamicVars.Block, cardPlay, false);
				}
			}
		}
	}
	public sealed class OldScissors : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new CardsVar(6) };

		public override string PackedIconPath => "res://wuwancients/images/relics/old_scissors.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/old_scissors.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/old_scissors.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			IEnumerable<CardModel> cardsToRemove = await CardSelectCmd.FromDeckForRemoval(((RelicModel)this).Owner, new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 0, 6), (Func<CardModel, bool>)null);
			int removedCount = 0;
			foreach (CardModel card in cardsToRemove)
			{
				await CardPileCmd.RemoveFromDeck(card, true);
				removedCount++;
			}
			List<CardModel> upgradedCards = ((RelicModel)this).Owner.Deck.Cards.Where((CardModel c) => c.IsUpgraded).ToList();
			int downgradeCount = Math.Min(2, upgradedCards.Count);
			for (int i = 0; i < downgradeCount; i++)
			{
				Player owner = ((RelicModel)this).Owner;
				int index = SyncedRng.Index(SyncedRng.Niche((owner != null) ? owner.RunState : null), upgradedCards.Count);
				CardModel cardToDowngrade = upgradedCards[index];
				upgradedCards.RemoveAt(index);
				CardCmd.Downgrade(cardToDowngrade);
				CardCmd.Preview(cardToDowngrade, 1.2f, (CardPreviewStyle)2);
			}
			((RelicModel)this).Flash();
		}
	}
	public sealed class OuterCalipers : RelicModel
	{
		private const decimal _lostBlock = 15m;

		public override RelicRarity Rarity => (RelicRarity)6;

		public override string PackedIconPath => "res://wuwancients/images/relics/outer_calipers.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/outer_calipers.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/outer_calipers.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new BlockVar(15m, (ValueProp)4) };

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			get
			{
				List<IHoverTip> list = new List<IHoverTip>();
				list.Add(HoverTipFactory.Static((StaticHoverTip)5, Array.Empty<DynamicVar>()));
				return list;
			}
		}

		public override bool ShouldClearBlock(Creature creature)
		{
			if (creature != ((RelicModel)this).Owner.Creature)
			{
				return true;
			}
			return false;
		}

		public override async Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
		{
			if (this == preventer && creature == ((RelicModel)this).Owner.Creature)
			{
				int block = creature.Block;
				if (block > 0)
				{
					await CreatureCmd.LoseBlock((PlayerChoiceContext)new BlockingPlayerChoiceContext(), creature, decimal.Min(block, 15m), (Creature)null);
				}
				((RelicModel)this).Flash();
			}
		}
	}
	public sealed class OxHorsePlushie : RelicModel
	{
		private const long SecondsPerPayout = 600L;

		private const int GoldPerPayout = 100;

		private int _goldPaid;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override string PackedIconPath => "res://wuwancients/images/relics/ox_horse_plushie.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/ox_horse_plushie.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/ox_horse_plushie.png";

		[SavedProperty]
		public int GoldPaid
		{
			get
			{
				return _goldPaid;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_goldPaid = value;
			}
		}

		public override Task AfterObtained()
		{
			return Settle();
		}

		public override Task AfterRoomEntered(AbstractRoom room)
		{
			return Settle();
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			return Settle();
		}

		public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			return (player == ((RelicModel)this).Owner) ? Settle() : Task.CompletedTask;
		}

		private async Task Settle()
		{
			if (((RelicModel)this).Owner != null)
			{
				RunManager instance = RunManager.Instance;
				long seconds = ((instance != null) ? instance.RunTime : 0);
				int earned = (int)(seconds / 600) * 100;
				int delta = earned - GoldPaid;
				if (delta > 0)
				{
					GoldPaid = earned;
					GD.Print($"[wuwancients] 牛马玩偶：局内计时 {seconds}s，发放 {delta} 金币");
					await PlayerCmd.GainGold((decimal)delta, ((RelicModel)this).Owner, false);
					((RelicModel)this).Flash();
				}
			}
		}
	}
	public sealed class PaintCan : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<SplashDye>(1);

		public override string PackedIconPath => "res://wuwancients/images/relics/paint_can.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/paint_can.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/paint_can.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			Player owner = ((RelicModel)this).Owner;
			SplashDye splashDye = ModelDb.Enchantment<SplashDye>();
			Func<CardModel, bool> obj = (CardModel? c) => c != null && ((int)c.Type == 1 || (int)c.Type == 2);
			CardSelectorPrefs val = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 0, 1);
			((CardSelectorPrefs)(ref val)).set_Cancelable(false);
			((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
			foreach (CardModel card in await CardSelectCmd.FromDeckForEnchantment(owner, (EnchantmentModel)(object)splashDye, 1, obj, val))
			{
				CardCmd.Enchant<SplashDye>(card, 1m);
			}
		}
	}
	public sealed class PaperRole : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/paper_role.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/paper_role.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/paper_role.png";

		public override bool IsAllowed(IRunState runState)
		{
			return ((IPlayerCollection)runState).Players.Count > 1;
		}

		public override async Task AfterRestSiteSmith(Player player)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return;
			}
			List<Player> allies = ((IPlayerCollection)((RelicModel)this).Owner.RunState).Players.Where((Player p) => p != ((RelicModel)this).Owner).ToList();
			if (allies.Count != 0)
			{
				((RelicModel)this).Owner.PlayerRng.Rewards.Shuffle<Player>((IList<Player>)allies);
				Player target = allies[0];
				List<CardModel> upgradable = target.Deck.Cards.Where((CardModel c) => c.IsUpgradable).ToList();
				if (upgradable.Count != 0)
				{
					target.PlayerRng.Rewards.Shuffle<CardModel>((IList<CardModel>)upgradable);
					((RelicModel)this).Flash();
					CardCmd.Upgrade(upgradable[0], (CardPreviewStyle)1);
				}
			}
		}
	}
	public sealed class PeaceAndProsperity : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)6;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromCard<SuisuiGold>(false) };

		public override string PackedIconPath => "res://wuwancients/images/relics/peace_and_prosperity.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/peace_and_prosperity.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/peace_and_prosperity.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				CardModel card = (CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<SuisuiGold>(((RelicModel)this).Owner);
				CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
			}
		}
	}
	public sealed class PeaceCharm : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/peace_charm.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/peace_charm.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/peace_charm.png";

		public override bool IsAllowed(IRunState runState)
		{
			return ((IPlayerCollection)runState).Players.Count > 1;
		}

		public override async Task AfterRestSiteHeal(Player player, bool isMimicked)
		{
			if (player == ((RelicModel)this).Owner)
			{
				List<Player> allies = ((IPlayerCollection)((RelicModel)this).Owner.RunState).Players.Where((Player p) => p != ((RelicModel)this).Owner).ToList();
				if (allies.Count != 0)
				{
					((RelicModel)this).Owner.PlayerRng.Rewards.Shuffle<Player>((IList<Player>)allies);
					Player target = allies[0];
					((RelicModel)this).Flash();
					await CreatureCmd.Heal(target.Creature, HealRestSiteOption.GetHealAmount(target), true);
				}
			}
		}
	}
	public sealed class PersonalMemo : RelicModel
	{
		private const int RareCount = 5;

		private const int UncommonCount = 10;

		private const int CommonCount = 5;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override string PackedIconPath => "res://wuwancients/images/relics/personal_memo.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/personal_memo.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/personal_memo.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			Rng rng = ((RelicModel)this).Owner.RunState.Rng.Niche;
			List<CardModel> pool = (from c in ((RelicModel)this).Owner.Character.CardPool.GetUnlockedCards(((RelicModel)this).Owner.UnlockState, ((RelicModel)this).Owner.RunState.CardMultiplayerConstraint)
				where c != null
				select c).ToList();
			List<CardModel> protos = new List<CardModel>();
			protos.AddRange(TakeDistinct(pool, (CardRarity)4, 5, rng));
			protos.AddRange(TakeDistinct(pool, (CardRarity)3, 10, rng));
			protos.AddRange(TakeDistinct(pool, (CardRarity)2, 5, rng));
			List<CardModel> cards = new List<CardModel>();
			foreach (CardModel proto in protos)
			{
				CardModel card2 = ((ICardScope)((RelicModel)this).Owner.RunState).CreateCard(proto, ((RelicModel)this).Owner);
				if (card2.IsUpgradable)
				{
					card2.UpgradeInternal();
					card2.FinalizeUpgradeInternal();
				}
				cards.Add(card2);
			}
			BlockingPlayerChoiceContext val = new BlockingPlayerChoiceContext();
			Player owner = ((RelicModel)this).Owner;
			CardSelectorPrefs val2 = new CardSelectorPrefs(((RelicModel)this).SelectionScreenPrompt, 0, cards.Count);
			((CardSelectorPrefs)(ref val2)).set_Cancelable(true);
			((CardSelectorPrefs)(ref val2)).set_RequireManualConfirmation(true);
			HashSet<CardModel> taken = new HashSet<CardModel>(await CardSelectCmd.FromSimpleGrid((PlayerChoiceContext)val, (IReadOnlyList<CardModel>)cards, owner, val2));
			foreach (CardModel card in cards)
			{
				if (taken.Contains(card))
				{
					await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
				}
				else
				{
					((ICardScope)((RelicModel)this).Owner.RunState).RemoveCard(card);
				}
			}
		}

		private static IEnumerable<CardModel> TakeDistinct(List<CardModel> pool, CardRarity rarity, int count, Rng rng)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			Rng rng2 = rng;
			List<CardModel> list = pool.Where((CardModel c) => c.Rarity == rarity).ToList();
			if (list.Count == 0)
			{
				return Enumerable.Empty<CardModel>();
			}
			return list.OrderBy((CardModel _) => rng2.NextDouble()).Take(count).ToList();
		}
	}
	public sealed class PhoebeChobi : RelicModel
	{
		private int _extraRemovalsLeft = 2;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/phoebe_chobi.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/phoebe_chobi.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/phoebe_chobi.png";

		public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice)
		{
			if (player == ((RelicModel)this).Owner && entry is MerchantCardRemovalEntry)
			{
				return 50m;
			}
			return originalPrice;
		}

		public override bool ShouldRefillMerchantEntry(MerchantEntry entry, Player player)
		{
			return player == ((RelicModel)this).Owner && entry is MerchantCardRemovalEntry;
		}

		public override async Task AfterItemPurchased(Player player, MerchantEntry entry, int goldSpent)
		{
			MerchantCardRemovalEntry removalEntry = default(MerchantCardRemovalEntry);
			int num;
			if (player == ((RelicModel)this).Owner)
			{
				removalEntry = (MerchantCardRemovalEntry)(object)((entry is MerchantCardRemovalEntry) ? entry : null);
				if (removalEntry != null)
				{
					num = ((_extraRemovalsLeft > 0) ? 1 : 0);
					goto IL_004f;
				}
			}
			num = 0;
			goto IL_004f;
			IL_004f:
			if (num != 0)
			{
				ResetUsed(removalEntry);
				NMerchantCardRemoval slot = FindMerchantCardRemovalSlot();
				if (slot != null)
				{
					RemoveUsedState(slot);
				}
				_extraRemovalsLeft--;
				GD.Print($"[PhoebeChobi] Extra removal granted, {_extraRemovalsLeft} left.");
			}
			await Task.CompletedTask;
		}

		private void ResetUsed(MerchantCardRemovalEntry entry)
		{
			PropertyInfo property = typeof(MerchantCardRemovalEntry).GetProperty("Used", BindingFlags.Instance | BindingFlags.Public);
			if (property != null && property.CanWrite)
			{
				property.SetValue(entry, false);
				GD.Print("[PhoebeChobi] Used reset via property.");
				return;
			}
			FieldInfo field = typeof(MerchantCardRemovalEntry).GetField("<Used>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field != null)
			{
				field.SetValue(entry, false);
				GD.Print("[PhoebeChobi] Used reset via backing field.");
			}
			else
			{
				GD.Print("[PhoebeChobi] Failed to reset Used field!");
			}
		}

		private NMerchantCardRemoval? FindMerchantCardRemovalSlot()
		{
			MainLoop mainLoop = Engine.GetMainLoop();
			SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
			if (val == null)
			{
				return null;
			}
			Window root = val.Root;
			return RecursiveFind<NMerchantCardRemoval>((Node)(object)root);
		}

		private T? RecursiveFind<T>(Node parent) where T : Node
		{
			T val = (T)(object)((parent is T) ? parent : null);
			if (val != null)
			{
				return val;
			}
			foreach (Node child in parent.GetChildren(false))
			{
				T val2 = RecursiveFind<T>(child);
				if (val2 != null)
				{
					return val2;
				}
			}
			return default(T);
		}

		private void RemoveUsedState(NMerchantCardRemoval removalSlot)
		{
			FieldInfo field = typeof(NMerchantCardRemoval).GetField("_isUnavailable", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field != null)
			{
				field.SetValue(removalSlot, false);
				GD.Print("[PhoebeChobi] Reset _isUnavailable.");
			}
			FieldInfo field2 = typeof(NMerchantSlot).GetField("_hitbox", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field2 != null)
			{
				object? value = field2.GetValue(removalSlot);
				Control val = (Control)((value is Control) ? value : null);
				if (val != null)
				{
					val.MouseFilter = (MouseFilterEnum)0;
					GD.Print("[PhoebeChobi] Reset _hitbox MouseFilter.");
				}
			}
			((Control)removalSlot).MouseFilter = (MouseFilterEnum)0;
			AnimationPlayer node = ((Node)removalSlot).GetNode<AnimationPlayer>(NodePath.op_Implicit("%Animation"));
			if (node != null)
			{
				node.Stop(false);
				if (((AnimationMixer)node).HasAnimation(StringName.op_Implicit("default")))
				{
					node.Play(StringName.op_Implicit("default"), -1.0, 1f, false);
				}
				else if (((AnimationMixer)node).HasAnimation(StringName.op_Implicit("idle")))
				{
					node.Play(StringName.op_Implicit("idle"), -1.0, 1f, false);
				}
				GD.Print("[PhoebeChobi] Reset animation.");
			}
			FieldInfo field3 = typeof(NMerchantSlot).GetField("_costLabel", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field3 != null)
			{
				object? value2 = field3.GetValue(removalSlot);
				Label val2 = (Label)((value2 is Label) ? value2 : null);
				if (val2 != null)
				{
					((CanvasItem)val2).Visible = true;
				}
			}
			FieldInfo field4 = typeof(NMerchantCardRemoval).GetField("_costContainer", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field4 != null)
			{
				object? value3 = field4.GetValue(removalSlot);
				Control val3 = (Control)((value3 is Control) ? value3 : null);
				if (val3 != null)
				{
					((CanvasItem)val3).Visible = true;
				}
			}
			typeof(NMerchantCardRemoval).GetMethod("UpdateVisual", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(removalSlot, null);
			GD.Print("[PhoebeChobi] Called UpdateVisual.");
		}

		public override Task BeforeCombatStart()
		{
			_extraRemovalsLeft = 2;
			return Task.CompletedTask;
		}
	}
	public sealed class PizzaSlice : RelicModel
	{
		private static bool _patched;

		internal static bool _isCardGain;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/pizza_slice.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/pizza_slice.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/pizza_slice.png";

		public override decimal ModifyMaxEnergy(Player player, decimal amount)
		{
			if (player == ((RelicModel)this).Owner)
			{
				return amount + 1m;
			}
			return amount;
		}

		public override decimal ModifyEnergyGain(Player player, decimal amount)
		{
			if (player == ((RelicModel)this).Owner && _isCardGain)
			{
				decimal num = amount - 1m;
				return (num < 0m) ? 0m : num;
			}
			return amount;
		}

		static PizzaSlice()
		{
			PatchEnergyGain();
		}

		private static void PatchEnergyGain()
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Expected O, but got Unknown
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Expected O, but got Unknown
			if (!_patched)
			{
				_patched = true;
				Harmony val = new Harmony("wuwancients.PizzaSlice");
				MethodInfo method = typeof(PlayerCmd).GetMethod("GainEnergy", BindingFlags.Static | BindingFlags.Public);
				MethodInfo method2 = typeof(PizzaSlice).GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
				val.Patch((MethodBase)method, new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
		}

		private static bool Prefix(decimal amount, Player player)
		{
			StackTrace stackTrace = new StackTrace();
			for (int i = 2; i < stackTrace.FrameCount; i++)
			{
				MethodBase methodBase = stackTrace.GetFrame(i)?.GetMethod();
				if ((object)methodBase != null && (methodBase.DeclaringType?.IsSubclassOf(typeof(CardModel))).GetValueOrDefault())
				{
					_isCardGain = true;
					return true;
				}
			}
			_isCardGain = false;
			return true;
		}
	}
	public sealed class Prayer : RelicModel
	{
		private static readonly Type[] ResidentGreetingTypes = new Type[4]
		{
			typeof(HellhoundGreeting),
			typeof(GoatBaaGreeting),
			typeof(JianxinGreeting),
			typeof(SproutGreeting)
		};

		private static readonly Type[] LimitedGreetingTypes = new Type[9]
		{
			typeof(BurySpiritGreeting),
			typeof(AutumnWaterGreeting),
			typeof(FireDevilGreeting),
			typeof(CamelliaGreeting),
			typeof(FeisaliesGift),
			typeof(MottaliGift),
			typeof(LinaGreeting),
			typeof(SuisuiGreeting),
			typeof(LingYinGreeting)
		};

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override string PackedIconPath => "res://wuwancients/images/relics/prayer.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/prayer.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/prayer.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			if (((RelicModel)this).Owner.PlayerRng.Rewards.NextBool())
			{
				int index = ((RelicModel)this).Owner.PlayerRng.Rewards.NextInt(ResidentGreetingTypes.Length);
				Type chosenType = ResidentGreetingTypes[index];
				RelicModel relic = CreateRelicOfType(chosenType);
				if (relic != null)
				{
					await RelicCmd.Obtain(relic, ((RelicModel)this).Owner, -1);
				}
			}
			else
			{
				int index2 = ((RelicModel)this).Owner.PlayerRng.Rewards.NextInt(LimitedGreetingTypes.Length);
				Type chosenType2 = LimitedGreetingTypes[index2];
				RelicModel relic2 = CreateRelicOfType(chosenType2);
				if (relic2 != null)
				{
					await RelicCmd.Obtain(relic2, ((RelicModel)this).Owner, -1);
				}
			}
		}

		private RelicModel? CreateRelicOfType(Type type)
		{
			MethodInfo methodInfo = typeof(ModelDb).GetMethod("Relic", Type.EmptyTypes)?.MakeGenericMethod(type);
			if (methodInfo != null)
			{
				object? obj = methodInfo.Invoke(null, null);
				RelicModel val = (RelicModel)((obj is RelicModel) ? obj : null);
				return (val != null) ? val.ToMutable() : null;
			}
			return null;
		}
	}
	[Pool(typeof(SharedRelicPool))]
	public sealed class PromiseBaton : RelicModel
	{
		private const int _targetCount = 2;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<DynamicVar> CanonicalVars => new <>z__ReadOnlySingleElementList<DynamicVar>((DynamicVar)new CardsVar(2));

		protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<Unison>(2);

		public override string PackedIconPath => "res://wuwancients/images/relics/promise_baton.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/promise_baton.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/promise_baton.png";

		public override async Task AfterObtained()
		{
			CardSelectorPrefs val = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 0, ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue);
			((CardSelectorPrefs)(ref val)).set_Cancelable(false);
			((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
			CardSelectorPrefs prefs = val;
			Unison canonical = ModelDb.Enchantment<Unison>();
			foreach (CardModel card in await CardSelectCmd.FromDeckForEnchantment(((RelicModel)this).Owner, (EnchantmentModel)(object)canonical, 2, (Func<CardModel, bool>)((CardModel? _) => true), prefs))
			{
				CardCmd.Enchant(((EnchantmentModel)canonical).ToMutable(), card, 1m);
				CardCmd.Preview(card, 1.2f, (CardPreviewStyle)1);
			}
		}
	}
	public sealed class PurifyingConch : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new CardsVar(1) };

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			get
			{
				List<IHoverTip> list = new List<IHoverTip>();
				list.Add(HoverTipFactory.ForEnergy((RelicModel)(object)this));
				list.AddRange(HoverTipFactory.FromCardWithCardHoverTips<Revelation1>(false));
				return list;
			}
		}

		public override string PackedIconPath => "res://wuwancients/images/relics/purifying_conch.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/purifying_conch.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/purifying_conch.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				CardModel revelation = (CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<Revelation1>(((RelicModel)this).Owner);
				await CardPileCmd.Add(revelation, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
			}
		}
	}
	public sealed class RadiantGlow : RelicModel
	{
		private const int RerollAttempts = 12;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/radiant_glow.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/radiant_glow.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/radiant_glow.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			List<CardModel> targets = PileTypeExtensions.GetPile((PileType)6, ((RelicModel)this).Owner).Cards.Where((CardModel card) => card != null && IsTarget(card)).ToList();
			List<CardPoolModel> otherPools = ModelDb.AllCharacterCardPools.Where((CardPoolModel pool) => pool != null && pool != ((RelicModel)this).Owner.Character.CardPool).ToList();
			if (targets.Count == 0 || otherPools.Count == 0)
			{
				return;
			}
			Rng rng = ((RelicModel)this).Owner.PlayerRng.Transformations;
			List<CardTransformation> transformations = new List<CardTransformation>();
			foreach (CardModel original in targets)
			{
				CardModel replacement = RollReplacement(otherPools, rng);
				if (replacement != null)
				{
					if (original.Keywords.Contains((CardKeyword)7))
					{
						original.RemoveKeyword((CardKeyword)7);
					}
					if (original.IsTransformable)
					{
						transformations.Add(new CardTransformation(original, replacement));
					}
				}
			}
			if (transformations.Count != 0)
			{
				((RelicModel)this).Flash();
				await CardCmd.Transform((IEnumerable<CardTransformation>)transformations, rng, (CardPreviewStyle)1);
			}
		}

		private static bool IsTarget(CardModel card)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Invalid comparison between Unknown and I4
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Invalid comparison between Unknown and I4
			if ((int)card.Type == 5)
			{
				return true;
			}
			if ((int)card.Rarity != 1)
			{
				return false;
			}
			return card.Tags.Contains((CardTag)1) || card.Tags.Contains((CardTag)2);
		}

		private CardModel? RollReplacement(List<CardPoolModel> pools, Rng rng)
		{
			List<CardModel> list = pools.SelectMany((CardPoolModel pool) => pool.GetUnlockedCards(((RelicModel)this).Owner.UnlockState, ((RelicModel)this).Owner.RunState.CardMultiplayerConstraint)).ToList();
			if (list.Count == 0)
			{
				return null;
			}
			CardModel val = null;
			for (int i = 0; i < 12; i++)
			{
				CardModel val2 = list[rng.NextInt(list.Count)];
				CardModel val3 = ((ICardScope)((RelicModel)this).Owner.RunState).CreateCard(val2, ((RelicModel)this).Owner);
				if (val3.IsUpgradable)
				{
					CardCmd.Upgrade(val3, (CardPreviewStyle)0);
				}
				bool flag = EnchantUtil.ApplyRandom(val3, rng);
				if (val == null)
				{
					val = val3;
				}
				if (val3.IsUpgraded && flag)
				{
					return val3;
				}
			}
			return val;
		}
	}
	public sealed class RedCurrantTart : RelicModel
	{
		private int _lostHp;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		[SavedProperty]
		public int LostHp
		{
			get
			{
				return _lostHp;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_lostHp = value;
				if (((RelicModel)this).DynamicVars.ContainsKey("LostHp"))
				{
					((RelicModel)this).DynamicVars["LostHp"].BaseValue = value;
				}
			}
		}

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new IntVar("LostHp", 0m) };

		protected override IEnumerable<IHoverTip> ExtraHoverTips => Enumerable.Empty<IHoverTip>();

		public override string PackedIconPath => "res://wuwancients/images/relics/red_currant_tart.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/red_currant_tart.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/red_currant_tart.png";

		public override decimal ModifyMaxEnergy(Player player, decimal amount)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return amount;
			}
			return amount + 1m;
		}

		public override async Task AfterObtained()
		{
			int currentMaxHp = ((RelicModel)this).Owner.Creature.MaxHp;
			int loss = (int)((decimal)currentMaxHp * 0.15m);
			if (loss < 1)
			{
				loss = 1;
			}
			LostHp = loss;
			await CreatureCmd.LoseMaxHp((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RelicModel)this).Owner.Creature, (decimal)loss, false);
		}
	}
	public sealed class RedHairband : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new CardsVar(5) };

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			get
			{
				List<IHoverTip> list = new List<IHoverTip>();
				list.Add(HoverTipFactory.ForEnergy((RelicModel)(object)this));
				list.AddRange(HoverTipFactory.FromCardWithCardHoverTips<JieXian>(false));
				return list;
			}
		}

		public override string PackedIconPath => "res://wuwancients/images/relics/red_hairband.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/red_hairband.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/red_hairband.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				CardSelectorPrefs val = new CardSelectorPrefs(((RelicModel)this).SelectionScreenPrompt, 0, 5);
				((CardSelectorPrefs)(ref val)).set_Cancelable(false);
				((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
				CardSelectorPrefs prefs = val;
				List<CardTransformation> transformations = (await CardSelectCmd.FromDeckForTransformation(((RelicModel)this).Owner, prefs, (Func<CardModel, CardTransformation>)((CardModel original) => new CardTransformation(original, CreateJieXian(original, forPreview: true))))).Select((Func<CardModel, CardTransformation>)((CardModel original) => new CardTransformation(original, CreateJieXian(original, forPreview: false)))).ToList();
				await CardCmd.Transform((IEnumerable<CardTransformation>)transformations, ((RelicModel)this).Owner.PlayerRng.Transformations, (CardPreviewStyle)1);
			}
		}

		private CardModel CreateJieXian(CardModel original, bool forPreview)
		{
			if (forPreview)
			{
				return ((CardModel)ModelDb.Card<JieXian>()).ToMutable();
			}
			return (CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<JieXian>(((RelicModel)this).Owner);
		}
	}
	public sealed class ResidualFrequencyOfBlackTide : RelicModel
	{
		private const int MarkCount = 7;

		private int _markedActIndex = -1;

		[SavedProperty]
		public int MarkedActIndex
		{
			get
			{
				return _markedActIndex;
			}
			private set
			{
				((AbstractModel)this).AssertMutable();
				_markedActIndex = value;
			}
		}

		[SavedProperty]
		private int[] MarkedCols { get; set; } = Array.Empty<int>();


		[SavedProperty]
		private int[] MarkedRows { get; set; } = Array.Empty<int>();


		[SavedProperty]
		private bool CoordsSet { get; set; }

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/residual_frequency_of_black_tide.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/residual_frequency_of_black_tide.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/residual_frequency_of_black_tide.png";

		public override Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return Task.CompletedTask;
			}
			MarkedActIndex = ((RelicModel)this).Owner.RunState.CurrentActIndex;
			AddMarkedRooms(((RelicModel)this).Owner.RunState.Map);
			return Task.CompletedTask;
		}

		public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
		{
			return AddMarkedRooms(map);
		}

		private ActMap AddMarkedRooms(ActMap map)
		{
			//IL_0082: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Expected O, but got Unknown
			//IL_019f: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
			ActMap map2 = map;
			if (((RelicModel)this).Owner == null || ((RelicModel)this).Owner.RunState.CurrentActIndex != MarkedActIndex)
			{
				return map2;
			}
			List<MapCoord> markedCoords = GetMarkedCoords();
			if (markedCoords == null || !markedCoords.TrueForAll((MapCoord c) => map2.HasPoint(c) && (int)map2.GetPoint(c).PointType == 5))
			{
				Rng val = new Rng(((RelicModel)this).Owner, ((AbstractModel)this).Id, 0uL);
				List<MapPoint> list = (from p in map2.GetAllMapPoints()
					where (int)p.PointType == 5 && !p.Quests.Any((AbstractModel q) => q is ResidualFrequencyOfBlackTide)
					select p).ToList();
				ListExtensions.UnstableShuffle<MapPoint>(list, val);
				int count = Math.Min(7, list.Count);
				List<MapPoint> list2 = list.Take(count).ToList();
				MarkedCols = list2.Select((MapPoint p) => p.coord.col).ToArray();
				MarkedRows = list2.Select((MapPoint p) => p.coord.row).ToArray();
				CoordsSet = true;
				foreach (MapPoint item in list2)
				{
					item.AddQuest((AbstractModel)(object)this);
				}
			}
			else
			{
				foreach (MapCoord item2 in markedCoords)
				{
					MapPoint point = map2.GetPoint(item2);
					if (point != null)
					{
						point.AddQuest((AbstractModel)(object)this);
					}
				}
			}
			return map2;
		}

		public List<MapCoord>? GetMarkedCoords()
		{
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			if (!CoordsSet)
			{
				return null;
			}
			List<MapCoord> list = new List<MapCoord>();
			for (int i = 0; i < MarkedCols.Length; i++)
			{
				list.Add(new MapCoord
				{
					col = MarkedCols[i],
					row = MarkedRows[i]
				});
			}
			return list;
		}

		public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
		{
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Expected O, but got Unknown
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Expected O, but got Unknown
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0107: Expected O, but got Unknown
			if (player != ((RelicModel)this).Owner)
			{
				return false;
			}
			if (!(room is CombatRoom))
			{
				return false;
			}
			Player owner = ((RelicModel)this).Owner;
			MapPoint val = ((owner != null) ? owner.RunState.CurrentMapPoint : null);
			List<MapCoord> markedCoords = GetMarkedCoords();
			if (val == null || markedCoords == null || !markedCoords.Contains(val.coord))
			{
				return false;
			}
			rewards.Add((Reward)new RelicReward(player));
			if (player.Relics.Any((RelicModel r) => r is BlackStar))
			{
				rewards.Add((Reward)new RelicReward(player));
			}
			if (player.Relics.Any((RelicModel r) => r is WhiteStar))
			{
				rewards.Add((Reward)new CardReward(CardCreationOptions.ForRoom(player, (RoomType)3), 3, player, (PlayerChoiceSynchronizer)null));
			}
			((RelicModel)this).Flash();
			return true;
		}
	}
	public sealed class ResonanceSuppressionCollar : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/resonance_suppression_collar.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/resonance_suppression_collar.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/resonance_suppression_collar.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new EnergyVar[1]
		{
			new EnergyVar(1)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.ForEnergy((RelicModel)(object)this) };

		public override decimal ModifyMaxEnergy(Player player, decimal amount)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return amount;
			}
			return amount + ((DynamicVar)((RelicModel)this).DynamicVars.Energy).BaseValue;
		}

		public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
		{
			if (card.Owner != ((RelicModel)this).Owner || !IsStrikeOrDefense(card))
			{
				modifiedCost = originalCost;
				return false;
			}
			modifiedCost = originalCost + 1m;
			return true;
		}

		private static bool IsStrikeOrDefense(CardModel card)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Invalid comparison between Unknown and I4
			return (int)card.Rarity == 1 && (card.Tags.Contains((CardTag)1) || card.Tags.Contains((CardTag)2));
		}
	}
	public sealed class ScatteredLycoris : RelicModel
	{
		private const int _applyCount = 3;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			(DynamicVar)new PowerVar<VulnerablePower>(1m),
			(DynamicVar)new PowerVar<WeakPower>(1m)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
		{
			HoverTipFactory.FromPower<VulnerablePower>((int?)null),
			HoverTipFactory.FromPower<WeakPower>((int?)null)
		};

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/scattered_lycoris.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/scattered_lycoris.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/scattered_lycoris.png";

		public override async Task BeforeCombatStart()
		{
			Player owner = ((RelicModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				Creature creature = owner.Creature;
				obj = ((creature != null) ? creature.CombatState : null);
			}
			if (obj == null)
			{
				return;
			}
			((RelicModel)this).Flash();
			IReadOnlyList<Creature> enemies = ((RelicModel)this).Owner.Creature.CombatState.HittableEnemies;
			if (enemies != null && enemies.Count != 0)
			{
				for (int i = 0; i < 3; i++)
				{
					await PowerCmd.Apply<VulnerablePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), (IEnumerable<Creature>)enemies, ((RelicModel)this).DynamicVars["VulnerablePower"].BaseValue, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
					await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), (IEnumerable<Creature>)enemies, ((RelicModel)this).DynamicVars["WeakPower"].BaseValue, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
			}
		}
	}
	public sealed class ShorekeeperDango : RelicModel
	{
		private const int GoldOnPickup = 100;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override bool ShowCounter => false;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("GoldAmount", 100m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/shorekeeper_dango.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/shorekeeper_dango.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/shorekeeper_dango.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				await PlayerCmd.GainGold(100m, ((RelicModel)this).Owner, false);
			}
		}
	}
	[HarmonyPatch(typeof(PlayerCmd), "LoseGold")]
	public static class GoldLossPatch
	{
		private const int GoldPerHeal = 20;

		private const int HealAmount = 2;

		public static void Postfix(decimal amount, Player player, GoldLossType goldLossType)
		{
			if (player != null && !(amount <= 0m) && player.Relics.Any((RelicModel r) => r is ShorekeeperDango))
			{
				int num = (int)(amount / 20m);
				if (num > 0)
				{
					TaskHelper.RunSafely(CreatureCmd.Heal(player.Creature, (decimal)(num * 2), false));
				}
			}
		}
	}
	public sealed class SisterLetter : RelicModel
	{
		private const int DrawCount = 2;

		private bool _usedThisTurn;

		public override RelicRarity Rarity => (RelicRarity)6;

		public override string PackedIconPath => "res://wuwancients/images/relics/sister_letter.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/sister_letter.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/sister_letter.png";

		public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner)
			{
				_usedThisTurn = false;
			}
			return Task.CompletedTask;
		}

		public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null && target == ((RelicModel)this).Owner.Creature && result.UnblockedDamage > 0 && !_usedThisTurn)
			{
				_usedThisTurn = true;
				((RelicModel)this).Flash();
				await CardPileCmd.Draw(choiceContext, 2m, ((RelicModel)this).Owner, false);
			}
		}
	}
	public sealed class SmallAcorn : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/small_acorn.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/small_acorn.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/small_acorn.png";

		public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
		{
			if (card.Owner != ((RelicModel)this).Owner)
			{
				return playCount;
			}
			if (card.EnergyCost.GetWithModifiers((CostModifiers)(-1)) == 0)
			{
				return playCount + 1;
			}
			return playCount;
		}
	}
	public sealed class SmeltedFragment : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("BonusPerUpgraded", 1m)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<SmeltedFragmentEnchantment>(1);

		public override string PackedIconPath => "res://wuwancients/images/relics/smelted_fragment.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/smelted_fragment.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/smelted_fragment.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			List<CardModel> attackCards = ((RelicModel)this).Owner.Deck.Cards.Where((CardModel c) => (int)c.Type == 1 && c.IsUpgradable).ToList();
			foreach (CardModel card2 in attackCards)
			{
				CardCmd.Upgrade(card2, (CardPreviewStyle)1);
			}
			List<CardModel> targetCards = ((RelicModel)this).Owner.Deck.Cards.Where((CardModel c) => (int)c.Type == 1).ToList();
			if (targetCards.Count == 0)
			{
				return;
			}
			Player owner = ((RelicModel)this).Owner;
			SmeltedFragmentEnchantment smeltedFragmentEnchantment = ModelDb.Enchantment<SmeltedFragmentEnchantment>();
			Func<CardModel, bool> obj = (CardModel? c) => c != null && (int)c.Type == 1;
			CardSelectorPrefs val = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 0, 1);
			((CardSelectorPrefs)(ref val)).set_Cancelable(false);
			((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
			foreach (CardModel card in await CardSelectCmd.FromDeckForEnchantment(owner, (EnchantmentModel)(object)smeltedFragmentEnchantment, 1, obj, val))
			{
				CardCmd.Enchant<SmeltedFragmentEnchantment>(card, 1m);
			}
		}
	}
	public sealed class SnowStew : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new BlockVar(5m, (ValueProp)4) };

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.Static((StaticHoverTip)5, Array.Empty<DynamicVar>()) };

		public override string PackedIconPath => "res://wuwancients/images/relics/snow_stew.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/snow_stew.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/snow_stew.png";

		public override async Task AfterObtained()
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				await CreatureCmd.GainMaxHp(((RelicModel)this).Owner.Creature, 7m);
			}
		}

		public override async Task BeforeCombatStart()
		{
			((RelicModel)this).Flash();
			await CreatureCmd.GainBlock(((RelicModel)this).Owner.Creature, ((RelicModel)this).DynamicVars.Block, (CardPlay)null, false);
		}
	}
	public sealed class SobUnderWail : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool ShowCounter => false;

		public override string PackedIconPath => "res://wuwancients/images/relics/sob_under_wail.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/sob_under_wail.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/sob_under_wail.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			get
			{
				//IL_001f: Unknown result type (might be due to invalid IL or missing references)
				//IL_002e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0039: Expected O, but got Unknown
				//IL_0039: Expected O, but got Unknown
				//IL_0034: Unknown result type (might be due to invalid IL or missing references)
				List<IHoverTip> list = new List<IHoverTip>();
				list.Add(HoverTipFactory.ForEnergy((RelicModel)(object)this));
				list.Add((IHoverTip)(object)new HoverTip(new LocString("relics", "SOB_UNDER_WAIL.bossTipTitle"), new LocString("relics", "SOB_UNDER_WAIL.bossTipDescription"), (Texture2D)null));
				return list;
			}
		}

		public override decimal ModifyMaxEnergy(Player player, decimal amount)
		{
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Invalid comparison between Unknown and I4
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Invalid comparison between Unknown and I4
			if (player != ((RelicModel)this).Owner)
			{
				return amount;
			}
			CombatState obj = CombatManager.Instance.DebugOnlyGetState();
			AbstractRoom obj2 = ((obj != null) ? obj.RunState.CurrentRoom : null);
			CombatRoom val = (CombatRoom)(object)((obj2 is CombatRoom) ? obj2 : null);
			RoomType? val2 = ((val != null) ? new RoomType?(val.Encounter.RoomType) : null);
			if ((int)val2.GetValueOrDefault() == 3 || (int)val2.GetValueOrDefault() == 2)
			{
				return amount + 1m;
			}
			return amount;
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			return Task.CompletedTask;
		}

		public override Task BeforeCombatStart()
		{
			return Task.CompletedTask;
		}
	}
	public sealed class SproutGreeting : RelicModel
	{
		private bool _subscribed;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new MaxHpVar(6m) };

		public override string PackedIconPath => "res://wuwancients/images/relics/sprout_greeting.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/sprout_greeting.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/sprout_greeting.png";

		public override Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return Task.CompletedTask;
			}
			EnsureSubscribed();
			TaskHelper.RunSafely(GainMaxHpAsync());
			return Task.CompletedTask;
		}

		public override Task AfterRoomEntered(AbstractRoom room)
		{
			EnsureSubscribed();
			return Task.CompletedTask;
		}

		public override Task AfterRemoved()
		{
			if (((RelicModel)this).Owner != null)
			{
				((RelicModel)this).Owner.RelicObtained -= OnRelicObtained;
			}
			_subscribed = false;
			return Task.CompletedTask;
		}

		private void EnsureSubscribed()
		{
			if (!_subscribed && ((RelicModel)this).Owner != null)
			{
				((RelicModel)this).Owner.RelicObtained += OnRelicObtained;
				_subscribed = true;
			}
		}

		private void OnRelicObtained(RelicModel relic)
		{
			if (relic != this)
			{
				TaskHelper.RunSafely(GainMaxHpAsync());
			}
		}

		private async Task GainMaxHpAsync()
		{
			if (((RelicModel)this).Owner != null)
			{
				((RelicModel)this).Flash();
				await CreatureCmd.GainMaxHp(((RelicModel)this).Owner.Creature, ((DynamicVar)((RelicModel)this).DynamicVars.MaxHp).BaseValue);
			}
		}
	}
	public sealed class StarChart : RelicModel
	{
		private const decimal BaseHeal = 1m;

		private const decimal DecodeHeal = 2m;

		public override RelicRarity Rarity => (RelicRarity)6;

		public override string PackedIconPath => "res://wuwancients/images/relics/star_chart.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/star_chart.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/star_chart.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new HealVar(1m) };

		private bool HasDecode
		{
			get
			{
				int result;
				if (((AbstractModel)this).IsMutable)
				{
					Player owner = ((RelicModel)this).Owner;
					result = ((owner != null && (owner.Relics?.Any((RelicModel r) => r is Decode)).GetValueOrDefault()) ? 1 : 0);
				}
				else
				{
					result = 0;
				}
				return (byte)result != 0;
			}
		}

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			get
			{
				RefreshHeal();
				return Array.Empty<IHoverTip>();
			}
		}

		private void RefreshHeal()
		{
			if (((AbstractModel)this).IsMutable)
			{
				((DynamicVar)((RelicModel)this).DynamicVars.Heal).BaseValue = (HasDecode ? 2m : 1m);
			}
		}

		public override Task AfterObtained()
		{
			RefreshHeal();
			return Task.CompletedTask;
		}

		public override Task BeforeCombatStart()
		{
			RefreshHeal();
			return Task.CompletedTask;
		}

		public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null && target == ((RelicModel)this).Owner.Creature && result.UnblockedDamage > 0)
			{
				RefreshHeal();
				await CreatureCmd.Heal(((RelicModel)this).Owner.Creature, ((DynamicVar)((RelicModel)this).DynamicVars.Heal).BaseValue, true);
			}
		}
	}
	public sealed class StarSequenceHarmony : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new CardsVar(3) };

		public override string PackedIconPath => "res://wuwancients/images/relics/star_sequence_harmony.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/star_sequence_harmony.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/star_sequence_harmony.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			List<CardModel> pool = (from c in ((CardPoolModel)ModelDb.CardPool<ColorlessCardPool>()).GetUnlockedCards(((RelicModel)this).Owner.UnlockState, ((RelicModel)this).Owner.RunState.CardMultiplayerConstraint)
				where c.CanBeGeneratedByModifiers
				select c).ToList();
			List<CardModel> selected = ListExtensions.StableShuffle<CardModel>(pool, ((RelicModel)this).Owner.RunState.Rng.Niche).Take(3).ToList();
			if (selected.Count == 0)
			{
				return;
			}
			List<CardModel> cardOptions = new List<CardModel>();
			foreach (CardModel card in selected)
			{
				CardModel mutableCard = ((ICardScope)((RelicModel)this).Owner.RunState).CreateCard(card, ((RelicModel)this).Owner);
				CardCmd.Upgrade(mutableCard, (CardPreviewStyle)1);
				cardOptions.Add(mutableCard);
			}
			CardModel chosenCard = await CardSelectCmd.FromChooseACardScreen((PlayerChoiceContext)new BlockingPlayerChoiceContext(), (IReadOnlyList<CardModel>)cardOptions, ((RelicModel)this).Owner, false);
			if (chosenCard != null)
			{
				CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(chosenCard, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
			}
			foreach (CardModel item in cardOptions)
			{
				if (item != chosenCard)
				{
					MapPointHistoryEntry currentMapPointHistoryEntry = ((RelicModel)this).Owner.RunState.CurrentMapPointHistoryEntry;
					if (currentMapPointHistoryEntry != null)
					{
						currentMapPointHistoryEntry.GetEntry(((RelicModel)this).Owner.NetId).CardChoices.Add(new CardChoiceHistoryEntry(item, false));
					}
				}
			}
		}
	}
	public sealed class StoneRose : RelicModel
	{
		public const string DevourAlternativeKey = "HEAL18";

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/stone_rose.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/stone_rose.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/stone_rose.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("HealAmount", 18m)
		};

		public override bool TryModifyCardRewardAlternatives(Player player, CardReward cardReward, List<CardRewardAlternative> alternatives)
		{
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Expected O, but got Unknown
			if (((RelicModel)this).Owner != player)
			{
				return false;
			}
			alternatives.Add(new CardRewardAlternative("HEAL18", (Func<Task>)OnDevourSelected, (PostAlternateCardRewardAction)2));
			return true;
		}

		private async Task OnDevourSelected()
		{
			((RelicModel)this).Flash();
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				await CreatureCmd.Heal(((RelicModel)this).Owner.Creature, ((RelicModel)this).DynamicVars["HealAmount"].BaseValue, true);
			}
		}
	}
	public sealed class StrangeSpoon : RelicModel
	{
		private const float DiscardChance = 0.5f;

		public override RelicRarity Rarity => (RelicRarity)6;

		public override string PackedIconPath => "res://wuwancients/images/relics/strange_spoon.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/strange_spoon.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/strange_spoon.png";

		public override CardLocation ModifyCardPlayResultLocation(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocation cardLocation)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Invalid comparison between Unknown and I4
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			if ((int)cardLocation.pileType != 4)
			{
				return cardLocation;
			}
			if (card.Owner == null || card.Owner != ((RelicModel)this).Owner)
			{
				return cardLocation;
			}
			if (!TryTurnExhaustIntoDiscard())
			{
				return cardLocation;
			}
			CardLocation result = cardLocation;
			result.pileType = (PileType)3;
			return result;
		}

		private bool TryTurnExhaustIntoDiscard()
		{
			Player owner = ((RelicModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				IRunState runState = owner.RunState;
				if (runState == null)
				{
					obj = null;
				}
				else
				{
					RunRngSet rng = runState.Rng;
					obj = ((rng != null) ? rng.CombatCardSelection : null);
				}
			}
			Rng val = (Rng)obj;
			if (val == null)
			{
				return false;
			}
			if (val.NextFloat(0f, 1f) >= 0.5f)
			{
				return false;
			}
			((RelicModel)this).Flash();
			return true;
		}
	}
	public sealed class SuisuiGreeting : RelicModel
	{
		private const int GoldThreshold = 99;

		private const int GoldOnPickup = 99;

		private const string _goldKey = "Gold";

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override string PackedIconPath => "res://wuwancients/images/relics/suisui_greeting.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/suisui_greeting.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/suisui_greeting.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("Gold", 99m)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<Glam>(1);

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				((RelicModel)this).Flash();
				await PlayerCmd.GainGold(99m, ((RelicModel)this).Owner, false);
			}
		}

		public override Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return Task.CompletedTask;
			}
			if (goldSpent <= 99)
			{
				return Task.CompletedTask;
			}
			if (!(itemPurchased is MerchantCardEntry))
			{
				return Task.CompletedTask;
			}
			Player owner = ((RelicModel)this).Owner;
			CardPile val = ((owner != null) ? owner.Deck : null);
			if (val == null || val.Cards.Count == 0)
			{
				return Task.CompletedTask;
			}
			CardModel val2 = val.Cards.Last();
			if (val2 == null)
			{
				return Task.CompletedTask;
			}
			((RelicModel)this).Flash();
			CardCmd.Enchant<Glam>(val2, 1m);
			return Task.CompletedTask;
		}
	}
	public sealed class SunAndGriffinSeal : RelicModel
	{
		private bool _hasBeenSaved;

		private bool _pendingSave;

		private bool _extraTurnActive;

		private bool _zeroCostThisTurn;

		private bool _killThisTurn;

		private bool _saveEffectsPending;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/sun_and_griffin_seal.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/sun_and_griffin_seal.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/sun_and_griffin_seal.png";

		public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
			Player owner = ((RelicModel)this).Owner;
			if (target != ((owner != null) ? owner.Creature : null))
			{
				return amount;
			}
			if (_pendingSave)
			{
				return 0m;
			}
			if (!_hasBeenSaved && amount >= (decimal)target.CurrentHp)
			{
				_pendingSave = true;
				_hasBeenSaved = true;
				_saveEffectsPending = true;
				target.RemoveAllPowersInternalExcept(target.Powers.Where((PowerModel p) => (int)p.Type != 2));
				return (decimal)target.CurrentHp - 1m;
			}
			return amount;
		}

		public override async Task AfterModifyingHpLostAfterOsty()
		{
			if (!_saveEffectsPending)
			{
				await Task.CompletedTask;
				return;
			}
			_saveEffectsPending = false;
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot($"res://wuwancients/audio/sun_and_griffin_seal_save_{Random.Shared.Next(1, 4)}.wav", 1f);
			}
			Player owner = ((RelicModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				Creature creature = owner.Creature;
				if (creature == null)
				{
					obj = null;
				}
				else
				{
					ICombatState combatState = creature.CombatState;
					obj = ((combatState != null) ? combatState.GetCreaturesOnSide((CombatSide)2) : null);
				}
			}
			if (obj == null)
			{
				obj = Array.Empty<Creature>();
			}
			IReadOnlyList<Creature> enemies = (IReadOnlyList<Creature>)obj;
			List<Creature> aliveTargets = enemies.Where((Creature e) => !e.IsDead && e.CurrentHp > 0).ToList();
			if (aliveTargets.Count > 0)
			{
				await CreatureCmd.Damage((PlayerChoiceContext)new BlockingPlayerChoiceContext(), (IEnumerable<Creature>)aliveTargets, 20m, (ValueProp)4, (Creature)null);
			}
			_extraTurnActive = true;
			_zeroCostThisTurn = true;
			((RelicModel)this).Flash();
		}

		public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
		{
			Player owner = ((RelicModel)this).Owner;
			if (creature != ((owner != null) ? owner.Creature : null))
			{
				await Task.CompletedTask;
			}
			else if (_pendingSave && (decimal)creature.CurrentHp <= 0m)
			{
				creature.SetCurrentHpInternal(1m);
			}
		}

		public override bool ShouldDie(Creature creature)
		{
			Player owner = ((RelicModel)this).Owner;
			if (creature != ((owner != null) ? owner.Creature : null))
			{
				return true;
			}
			return !_pendingSave;
		}

		public override bool ShouldTakeExtraTurn(Player player)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return false;
			}
			if (!_extraTurnActive)
			{
				return false;
			}
			_extraTurnActive = false;
			return true;
		}

		public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if (((RelicModel)this).Owner == null || side != ((RelicModel)this).Owner.Creature.Side)
			{
				await Task.CompletedTask;
			}
			else
			{
				_pendingSave = false;
			}
		}

		public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
		{
			if (!_zeroCostThisTurn || card.Owner != ((RelicModel)this).Owner)
			{
				modifiedCost = originalCost;
				return false;
			}
			modifiedCost = default(decimal);
			return true;
		}

		public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
		{
			if (!_hasBeenSaved)
			{
				await Task.CompletedTask;
			}
			else if (((RelicModel)this).Owner == null || creature.Side == ((RelicModel)this).Owner.Creature.Side)
			{
				await Task.CompletedTask;
			}
			else
			{
				_killThisTurn = true;
			}
		}

		public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			if (((RelicModel)this).Owner == null || side != ((RelicModel)this).Owner.Creature.Side)
			{
				await Task.CompletedTask;
				return;
			}
			if (_killThisTurn)
			{
				_killThisTurn = false;
				_extraTurnActive = true;
			}
			_zeroCostThisTurn = false;
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			_hasBeenSaved = false;
			_pendingSave = false;
			_extraTurnActive = false;
			_zeroCostThisTurn = false;
			_killThisTurn = false;
			_saveEffectsPending = false;
			return Task.CompletedTask;
		}
	}
	public sealed class SundayCrown : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<SolsticeCrusade>(false);

		public override string PackedIconPath => "res://wuwancients/images/relics/sunday_crown.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/sunday_crown.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/sunday_crown.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				CardModel card = (CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<SolsticeCrusade>(((RelicModel)this).Owner);
				await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
			}
		}
	}
	public sealed class Sundial : RelicModel
	{
		private const int ShufflesPerEnergy = 3;

		private const int EnergyGain = 2;

		private int _shuffles;

		[SavedProperty]
		public int Shuffles
		{
			get
			{
				return _shuffles;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_shuffles = value;
				((RelicModel)this).InvokeDisplayAmountChanged();
			}
		}

		public override RelicRarity Rarity => (RelicRarity)6;

		public override string PackedIconPath => "res://wuwancients/images/relics/sundial.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/sundial.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/sundial.png";

		public override bool ShowCounter => true;

		public override int DisplayAmount => Shuffles % 3;

		public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
		{
			if (((RelicModel)this).Owner != null && shuffler == ((RelicModel)this).Owner)
			{
				Shuffles++;
				if (Shuffles % 3 == 0)
				{
					((RelicModel)this).Flash();
					await PlayerCmd.GainEnergy(2m, ((RelicModel)this).Owner);
				}
			}
		}
	}
	public sealed class SurveyLog : RelicModel
	{
		private int _lastCardCost = -1;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/survey_log.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/survey_log.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/survey_log.png";

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner)
			{
				_lastCardCost = -1;
			}
		}

		public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			if (cardPlay.IsAutoPlay)
			{
				return;
			}
			CardModel card = cardPlay.Card;
			if (((card != null) ? card.Owner : null) == ((RelicModel)this).Owner)
			{
				ResourceInfo resources = cardPlay.Resources;
				int currentCost = ((ResourceInfo)(ref resources)).EnergySpent;
				if (_lastCardCost >= 0 && currentCost > _lastCardCost)
				{
					await CardPileCmd.Draw(choiceContext, 1m, ((RelicModel)this).Owner, false);
					await PlayerCmd.GainEnergy(1m, ((RelicModel)this).Owner);
				}
				_lastCardCost = currentCost;
			}
		}
	}
	public sealed class SweetDreams : RelicModel
	{
		private const decimal BlockGain = 8m;

		private const decimal DamageDealt = 8m;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/sweet_dreams.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/sweet_dreams.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/sweet_dreams.png";

		public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null || card == null || card.Owner != ((RelicModel)this).Owner || ((int)card.Type != 5 && (int)card.Type != 4))
			{
				return;
			}
			ICombatState state = ((RelicModel)this).Owner.Creature.CombatState;
			if (state != null)
			{
				((RelicModel)this).Flash();
				await CreatureCmd.GainBlock(((RelicModel)this).Owner.Creature, 8m, (ValueProp)4, (CardPlay)null, false);
				List<Creature> enemies = (from c in state.GetCreaturesOnSide((CombatSide)2)
					where c.IsAlive
					select c).ToList();
				if (enemies.Count != 0)
				{
					await CreatureCmd.Damage(choiceContext, (IEnumerable<Creature>)enemies, 8m, (ValueProp)4, ((RelicModel)this).Owner.Creature);
				}
			}
		}
	}
	public sealed class SweetLeafSplitBread : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/sweet_leaf_split_bread.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/sweet_leaf_split_bread.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/sweet_leaf_split_bread.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => new <>z__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromKeyword(WuwancientsKeywords.HeavyStrike));

		public override decimal ModifyMaxEnergy(Player player, decimal amount)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return amount;
			}
			return amount + 1m;
		}

		public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
		{
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Invalid comparison between Unknown and I4
			Player owner = ((RelicModel)this).Owner;
			if (dealer != ((owner != null) ? owner.Creature : null))
			{
				return 0m;
			}
			if (cardSource == null)
			{
				return 0m;
			}
			if ((int)cardSource.Type != 1)
			{
				return 0m;
			}
			int withModifiers = cardSource.EnergyCost.GetWithModifiers((CostModifiers)(-1));
			if (withModifiers < 2)
			{
				return 0m;
			}
			return -0.44m;
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			return Task.CompletedTask;
		}

		public override Task BeforeCombatStart()
		{
			return Task.CompletedTask;
		}
	}
	public sealed class SweetPickledOlive : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/sweet_pickled_olive.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/sweet_pickled_olive.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/sweet_pickled_olive.png";

		public override decimal ModifyMaxEnergy(Player player, decimal amount)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return amount;
			}
			return amount + 1m;
		}

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return;
			}
			Creature creature = ((RelicModel)this).Owner.Creature;
			ICombatState combatState = ((creature != null) ? creature.CombatState : null);
			if (((combatState != null) ? combatState.HittableEnemies : null) == null)
			{
				return;
			}
			foreach (Creature enemy in combatState.HittableEnemies)
			{
				await CreatureCmd.Heal(enemy, 6m, true);
			}
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			return Task.CompletedTask;
		}

		public override Task BeforeCombatStart()
		{
			return Task.CompletedTask;
		}
	}
	public sealed class SwordCalamus : RelicModel
	{
		private const decimal DamagePerTurn = 5m;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/sword_calamus.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/sword_calamus.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/sword_calamus.png";

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner)
			{
				await PlayerCmd.GainEnergy(1m, ((RelicModel)this).Owner);
			}
		}

		public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null && participants.Contains(((RelicModel)this).Owner.Creature))
			{
				await CreatureCmd.Damage(choiceContext, ((RelicModel)this).Owner.Creature, 5m, (ValueProp)4, (CardModel)null, (CardPlay)null);
			}
		}
	}
	public sealed class Taiji : RelicModel
	{
		public const string DexterityKey = "TaijiDexterity";

		public const string StrengthKey = "TaijiStrength";

		public override RelicRarity Rarity => (RelicRarity)6;

		public override string PackedIconPath => "res://wuwancients/images/relics/taiji.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/taiji.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/taiji.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			new DynamicVar("TaijiDexterity", 1m),
			new DynamicVar("TaijiStrength", 1m)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
		{
			HoverTipFactory.FromPower<StrengthPower>((int?)null),
			HoverTipFactory.FromPower<DexterityPower>((int?)null)
		};

		public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			Player owner = ((RelicModel)this).Owner;
			int num;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				num = ((((cardPlay != null) ? cardPlay.Card : null) == null) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
			if (num != 0 || cardPlay.Card.Owner != ((RelicModel)this).Owner)
			{
				return;
			}
			CardType type = cardPlay.Card.Type;
			CardType val = type;
			CardType val2 = val;
			if ((int)val2 != 1)
			{
				if ((int)val2 == 2)
				{
					await PowerCmd.Apply<TaijiStrengthPower>(choiceContext, ((RelicModel)this).Owner.Creature, ((RelicModel)this).DynamicVars["TaijiStrength"].BaseValue, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
			}
			else
			{
				await PowerCmd.Apply<TaijiDexterityPower>(choiceContext, ((RelicModel)this).Owner.Creature, ((RelicModel)this).DynamicVars["TaijiDexterity"].BaseValue, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
			}
		}
	}
	public sealed class TearStainedBandage : RelicModel
	{
		[SavedProperty]
		public int SubTurns { get; set; }

		public override bool ShowCounter => true;

		public override int DisplayAmount => 4 - SubTurns;

		protected override IEnumerable<DynamicVar> CanonicalVars => Enumerable.Empty<DynamicVar>();

		protected override IEnumerable<IHoverTip> ExtraHoverTips => Enumerable.Empty<IHoverTip>();

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/tear_stained_bandage.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/tear_stained_bandage.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/tear_stained_bandage.png";

		public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null && side == ((RelicModel)this).Owner.Creature.Side)
			{
				SubTurns++;
				if (SubTurns >= 4)
				{
					SubTurns = 0;
					((RelicModel)this).Flash();
					await PowerCmd.Apply<IntangiblePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RelicModel)this).Owner.Creature, 1m, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				}
				((RelicModel)this).InvokeDisplayAmountChanged();
			}
		}

		public override Task AfterCombatEnd(CombatRoom _)
		{
			return Task.CompletedTask;
		}
	}
	public sealed class TetisFlower : RelicModel
	{
		[SavedProperty]
		private int EliteCount { get; set; } = 0;


		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool ShowCounter => false;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("Relics", 1m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/tetis_flower.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/tetis_flower.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/tetis_flower.png";

		public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Invalid comparison between Unknown and I4
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Expected O, but got Unknown
			if (player != ((RelicModel)this).Owner)
			{
				return false;
			}
			if (room == null || (int)room.RoomType != 2)
			{
				return false;
			}
			if (EliteCount >= 2)
			{
				return false;
			}
			((RelicModel)this).Flash();
			EliteCount++;
			rewards.Add((Reward)new RelicReward(player));
			return true;
		}
	}
	public sealed class Texture1 : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new CardsVar(1) };

		public override string PackedIconPath => "res://wuwancients/images/relics/texture.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/texture.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/texture.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			List<CardPoolModel> otherCardPools = ((RelicModel)this).Owner.UnlockState.CharacterCardPools.Where((CardPoolModel p) => p != ((RelicModel)this).Owner.Character.CardPool).ToList();
			if (otherCardPools.Count == 0)
			{
				return;
			}
			List<CardPoolModel> selectedPools = ListExtensions.StableShuffle<CardPoolModel>(otherCardPools, ((RelicModel)this).Owner.RunState.Rng.Niche).Take(3).ToList();
			List<CardModel> rareCards = new List<CardModel>();
			foreach (CardPoolModel pool in selectedPools)
			{
				CardCreationOptions options = new CardCreationOptions((IEnumerable<CardPoolModel>)(object)new CardPoolModel[1] { pool }, (CardCreationSource)3, (CardRarityOddsType)5, (Func<CardModel, bool>)((CardModel c) => (int)c.Rarity == 4)).WithFlags((CardCreationFlags)2);
				IEnumerable<CardCreationResult> results = CardFactory.CreateForReward(((RelicModel)this).Owner, 1, options);
				if (results.Any())
				{
					rareCards.Add(results.First().Card);
				}
			}
			if (rareCards.Count != 0)
			{
				CardCreationOptions rerollOptions = CardCreationOptions.ForNonCombatWithDefaultOdds((IEnumerable<CardPoolModel>)Array.Empty<CardPoolModel>(), (Func<CardModel, bool>)null);
				CardReward reward = new CardReward((IEnumerable<CardModel>)rareCards, (CardCreationSource)3, ((RelicModel)this).Owner, rerollOptions, (PlayerChoiceSynchronizer)null);
				await RewardsCmd.OfferCustom(((RelicModel)this).Owner, new List<Reward> { (Reward)(object)reward });
			}
		}
	}
	public sealed class ThankYouGift : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override string PackedIconPath => "res://wuwancients/images/relics/thank_you_gift.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/thank_you_gift.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/thank_you_gift.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
		{
			HoverTipFactory.FromCard<Overtime>(false),
			HoverTipFactory.FromCard<TimeOff>(false)
		};

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				CardModel overtime = (CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<Overtime>(((RelicModel)this).Owner);
				CardModel timeOff = (CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<TimeOff>(((RelicModel)this).Owner);
				BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
				CardModel chosen = await CardSelectCmd.FromChooseACardScreen((PlayerChoiceContext)(object)ctx, (IReadOnlyList<CardModel>)(object)new CardModel[2] { overtime, timeOff }, ((RelicModel)this).Owner, false);
				if (chosen == null)
				{
					((ICardScope)((RelicModel)this).Owner.RunState).RemoveCard(overtime);
					((ICardScope)((RelicModel)this).Owner.RunState).RemoveCard(timeOff);
				}
				else
				{
					CardModel other = ((chosen == overtime) ? timeOff : overtime);
					((ICardScope)((RelicModel)this).Owner.RunState).RemoveCard(other);
					await CardPileCmd.Add(chosen, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
				}
			}
		}
	}
	public sealed class TimeManagementMaster : RelicModel
	{
		internal const int OverdraftLimit = 3;

		private const int DefaultTurnEnergy = 3;

		private int _debt;

		internal int PendingOverdraft;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/time_management_master.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/time_management_master.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/time_management_master.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new EnergyVar("Energy", 3) };

		[SavedProperty]
		public int Debt
		{
			get
			{
				return _debt;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_debt = Math.Clamp(value, 0, 3);
			}
		}

		public override Task AfterObtained()
		{
			RefreshTurnEnergyVar();
			return Task.CompletedTask;
		}

		public override Task AfterEnergyReset(Player player)
		{
			if (player != ((RelicModel)this).Owner)
			{
				return Task.CompletedTask;
			}
			PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
			if (playerCombatState == null)
			{
				return Task.CompletedTask;
			}
			RefreshTurnEnergyVar();
			int debt = Debt;
			if (debt > 0)
			{
				((RelicModel)this).Flash();
			}
			Debt = 0;
			if (debt > 0)
			{
				playerCombatState.Energy -= debt;
			}
			return Task.CompletedTask;
		}

		private void RefreshTurnEnergyVar()
		{
			if (((AbstractModel)this).IsMutable && ((RelicModel)this).Owner != null)
			{
				PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
				int num = ((playerCombatState != null) ? playerCombatState.MaxEnergy : ((RelicModel)this).Owner.MaxEnergy);
				((RelicModel)this).DynamicVars["Energy"].BaseValue = num;
			}
		}
	}
	[HarmonyPatch(typeof(PlayerCombatState), "HasEnoughResourcesFor")]
	public static class TimeMasterOverdraftAllowPatch
	{
		public static void Postfix(PlayerCombatState __instance, CardModel card, ref UnplayableReason reason)
		{
			if (((uint)reason & 0x10u) != 0 && TimeMasterOverdraft.TryGetRelic(__instance, out TimeManagementMaster _))
			{
				int num = Math.Max(0, card.EnergyCost.GetWithModifiers((CostModifiers)(-1)));
				if (num - __instance.Energy <= 3)
				{
					reason = (UnplayableReason)((uint)reason & 0xFFFFFFEFu);
				}
			}
		}
	}
	[HarmonyPatch(typeof(PlayerCombatState), "LoseEnergy")]
	public static class TimeMasterOverdraftDebtPatch
	{
		public static void Prefix(PlayerCombatState __instance, ref decimal amount)
		{
			if (!(amount <= 0m) && TimeMasterOverdraft.TryGetRelic(__instance, out TimeManagementMaster relic))
			{
				decimal num = __instance.Energy;
				if (amount <= num)
				{
					relic.PendingOverdraft = 0;
					return;
				}
				relic.PendingOverdraft = (int)Math.Ceiling(amount - num);
				amount = Math.Max(0m, num);
			}
		}

		public static void Postfix(PlayerCombatState __instance)
		{
			if (TimeMasterOverdraft.TryGetRelic(__instance, out TimeManagementMaster relic) && relic.PendingOverdraft > 0)
			{
				int num = Math.Min(relic.PendingOverdraft, 3);
				relic.PendingOverdraft = 0;
				__instance.Energy = -num;
				relic.Debt = num;
			}
		}
	}
	[HarmonyPatch(typeof(PlayerCombatState), "GainEnergy")]
	public static class TimeMasterGainEnergyPatch
	{
		public static void Prefix(PlayerCombatState __instance, out decimal __state)
		{
			__state = __instance.Energy;
		}

		public static void Postfix(PlayerCombatState __instance, decimal amount, decimal __state)
		{
			if (!(__state >= 0m) && TimeMasterOverdraft.TryGetRelic(__instance, out TimeManagementMaster relic))
			{
				__instance.Energy = (int)(__state + amount);
				relic.Debt = Math.Max(0, -__instance.Energy);
			}
		}
	}
	internal static class TimeMasterOverdraft
	{
		internal static bool TryGetRelic(PlayerCombatState state, out TimeManagementMaster relic)
		{
			relic = null;
			Player value = Traverse.Create((object)state).Field("_player").GetValue<Player>();
			if (value == null)
			{
				return false;
			}
			TimeManagementMaster timeManagementMaster = value.Relics.OfType<TimeManagementMaster>().FirstOrDefault();
			if (timeManagementMaster == null)
			{
				return false;
			}
			relic = timeManagementMaster;
			return true;
		}
	}
	public sealed class ToyOrnithopter : RelicModel
	{
		private const decimal HealAmount = 5m;

		public override RelicRarity Rarity => (RelicRarity)6;

		public override string PackedIconPath => "res://wuwancients/images/relics/toy_ornithopter.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/toy_ornithopter.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/toy_ornithopter.png";

		public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
		{
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				if (((potion != null) ? potion.Owner : null) == ((RelicModel)this).Owner)
				{
					((RelicModel)this).Flash();
					await CreatureCmd.Heal(((RelicModel)this).Owner.Creature, 5m, true);
				}
			}
		}
	}
	public sealed class UnfinishedSymphony : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<BiShi>(false).Concat(HoverTipFactory.FromCardWithCardHoverTips<BiAn>(false));

		public override string PackedIconPath => "res://wuwancients/images/relics/unfinished_symphony.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/unfinished_symphony.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/unfinished_symphony.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			List<CardModel> cardOptions = new List<CardModel>
			{
				(CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<BiShi>(((RelicModel)this).Owner),
				(CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<BiAn>(((RelicModel)this).Owner)
			};
			CardModel chosenCard = await CardSelectCmd.FromChooseACardScreen((PlayerChoiceContext)new BlockingPlayerChoiceContext(), (IReadOnlyList<CardModel>)cardOptions, ((RelicModel)this).Owner, false);
			if (chosenCard != null)
			{
				CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(chosenCard, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
			}
			foreach (CardModel item in cardOptions)
			{
				if (item != chosenCard)
				{
					MapPointHistoryEntry currentMapPointHistoryEntry = ((RelicModel)this).Owner.RunState.CurrentMapPointHistoryEntry;
					if (currentMapPointHistoryEntry != null)
					{
						currentMapPointHistoryEntry.GetEntry(((RelicModel)this).Owner.NetId).CardChoices.Add(new CardChoiceHistoryEntry(item, false));
					}
				}
			}
		}
	}
	public sealed class UnstableTunnel : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)6;

		public override bool ShowCounter => false;

		public override string PackedIconPath => "res://wuwancients/images/relics/unstable_tunnel.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/unstable_tunnel.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/unstable_tunnel.png";

		public override bool ShouldAllowFreeTravel()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Invalid comparison between Unknown and I4
			return (int)((RelicModel)this).Status == 0;
		}

		public override Task AfterRoomEntered(AbstractRoom room)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			if ((int)((RelicModel)this).Status == 0)
			{
				IRunState runState = ((RelicModel)this).Owner.RunState;
				RunState val = (RunState)(object)((runState is RunState) ? runState : null);
				if (val != null && val.VisitedMapCoords.Count > 1)
				{
					((RelicModel)this).Status = (RelicStatus)2;
					((RelicModel)this).InvokeDisplayAmountChanged();
				}
			}
			return Task.CompletedTask;
		}
	}
	public sealed class ViolationPartsSet : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/violation_parts_set.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/violation_parts_set.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/violation_parts_set.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<ExplosiveSprayPaint>(false);

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				CardModel card = (CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<ExplosiveSprayPaint>(((RelicModel)this).Owner);
				CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
			}
		}
	}
	public sealed class WaxAndWane : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<MoonPhaseFlow>(false);

		public override string PackedIconPath => "res://wuwancients/images/relics/wax_and_wane.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/wax_and_wane.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/wax_and_wane.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				CardModel card = (CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<MoonPhaseFlow>(((RelicModel)this).Owner);
				await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
			}
		}
	}
	internal static class WaxOriginTracker
	{
		private const char Separator = ';';

		internal static string KeyOf(RelicModel relic)
		{
			return ((AbstractModel)relic).Id.Category + "|" + ((AbstractModel)relic).Id.Entry;
		}

		internal static string Register(string list, RelicModel wax)
		{
			string text = KeyOf(wax);
			return string.IsNullOrEmpty(list) ? text : (list + ";" + text);
		}

		internal static string Unregister(string list, RelicModel wax)
		{
			if (string.IsNullOrEmpty(list))
			{
				return string.Empty;
			}
			string text = KeyOf(wax);
			List<string> list2 = new List<string>();
			bool flag = false;
			string[] array = list.Split(';');
			foreach (string text2 in array)
			{
				if (!flag && text2 == text)
				{
					flag = true;
				}
				else if (text2.Length > 0)
				{
					list2.Add(text2);
				}
			}
			return string.Join(';'.ToString(), list2);
		}

		internal static bool Owns(string list, RelicModel relic)
		{
			if (string.IsNullOrEmpty(list))
			{
				return false;
			}
			string text = KeyOf(relic);
			string[] array = list.Split(';');
			foreach (string text2 in array)
			{
				if (text2 == text)
				{
					return true;
				}
			}
			return false;
		}
	}
	public sealed class We : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool ShowCounter => false;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("SmithCount", 2m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/we.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/we.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/we.png";

		public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
		{
			if (((RelicModel)this).Owner == null || player != ((RelicModel)this).Owner)
			{
				return false;
			}
			IRunState runState = player.RunState;
			if (runState == null || runState.CurrentActIndex != 0)
			{
				return false;
			}
			SmithRestSiteOption val = options.OfType<SmithRestSiteOption>().FirstOrDefault();
			if ((RestSiteOption)(object)val != (RestSiteOption)null)
			{
				val.SmithCount = 2;
				return true;
			}
			return false;
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			return Task.CompletedTask;
		}

		public override Task BeforeCombatStart()
		{
			return Task.CompletedTask;
		}
	}
	[HarmonyPatch(typeof(RestSiteOption), "Generate")]
	public static class WeSmithCountGeneratePatch
	{
		[HarmonyPostfix]
		public static void Postfix(Player player, List<RestSiteOption> __result)
		{
			WeSmithCount.Apply(player, __result);
		}
	}
	[HarmonyPatch(typeof(SmithRestSiteOption), "OnSelect")]
	public static class WeSmithCountSelectPatch
	{
		[HarmonyPrefix]
		public static void Prefix(SmithRestSiteOption __instance)
		{
			Player value = Traverse.Create((object)__instance).Property("Owner", (object[])null).GetValue<Player>();
			if (value != null)
			{
				WeSmithCount.Apply(value, new List<RestSiteOption> { (RestSiteOption)(object)__instance });
			}
		}
	}
	internal static class WeSmithCount
	{
		internal static void Apply(Player? player, IEnumerable<RestSiteOption>? options)
		{
			if (player == null || options == null)
			{
				return;
			}
			IRunState runState = player.RunState;
			if (runState != null && runState.CurrentActIndex == 0 && player.Relics.Any((RelicModel r) => r is We && !r.IsMelted))
			{
				SmithRestSiteOption val = options.OfType<SmithRestSiteOption>().FirstOrDefault();
				if ((RestSiteOption)(object)val != (RestSiteOption)null)
				{
					val.SmithCount = 2;
				}
			}
		}
	}
	public sealed class WhiteRibbon : RelicModel
	{
		private static readonly Type[] ShorekeeperRelicTypes = new Type[29]
		{
			typeof(SnowStew),
			typeof(Texture1),
			typeof(StarSequenceHarmony),
			typeof(GoatBaaGreeting),
			typeof(ButterflyPrint),
			typeof(YokuuTopology),
			typeof(GiftOfQiqiu),
			typeof(BurySpiritGreeting),
			typeof(AutumnWaterGreeting),
			typeof(We),
			typeof(MissetFallacy),
			typeof(SobUnderWail),
			typeof(ShorekeeperDango),
			typeof(MottaliBankCard),
			typeof(EndlessLoop),
			typeof(TetisFlower),
			typeof(FireDevilGreeting),
			typeof(CamelliaGreeting),
			typeof(FeisaliesGift),
			typeof(Prayer),
			typeof(MottaliGift),
			typeof(ZannisSalaryCard),
			typeof(MemoryStarAnchor),
			typeof(JianxinGreeting),
			typeof(LinaGreeting),
			typeof(SuisuiGreeting),
			typeof(SproutGreeting),
			typeof(LingYinGreeting),
			typeof(HellhoundGreeting)
		};

		private static readonly Type[] ShorekeeperMultiplayerRelicTypes = new Type[3]
		{
			typeof(EchoAbsorptionDevice),
			typeof(PeaceCharm),
			typeof(PaperRole)
		};

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			new DynamicVar("Relics", 2m),
			new DynamicVar("Curses", 1m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/white_ribbon.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/white_ribbon.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/white_ribbon.png";

		public static bool IsShorekeeperProvided(RelicModel? relic)
		{
			if (relic == null)
			{
				return false;
			}
			if (relic is WhiteRibbon)
			{
				return true;
			}
			Type type = ((object)relic).GetType();
			return ShorekeeperRelicTypes.Contains(type) || ShorekeeperMultiplayerRelicTypes.Contains(type);
		}

		private List<RelicModel> GetValidRelics(Player owner)
		{
			RunState obj = RunManager.Instance.DebugOnlyGetState();
			bool flag = obj != null && obj.Players.Count > 1;
			List<Type> list = ShorekeeperRelicTypes.Concat(flag ? ((IEnumerable<Type>)ShorekeeperMultiplayerRelicTypes) : ((IEnumerable<Type>)Array.Empty<Type>())).ToList();
			List<RelicModel> list2 = new List<RelicModel>();
			foreach (Type item in list)
			{
				MethodInfo methodInfo = typeof(ModelDb).GetMethod("Relic", Type.EmptyTypes)?.MakeGenericMethod(item);
				if (methodInfo != null)
				{
					object? obj2 = methodInfo.Invoke(null, null);
					RelicModel val = (RelicModel)((obj2 is RelicModel) ? obj2 : null);
					if (val != null && val != this && val.IsAllowedAtNeow(owner))
					{
						list2.Add(val.ToMutable());
					}
				}
			}
			owner.PlayerRng.Rewards.Shuffle<RelicModel>((IList<RelicModel>)list2);
			return list2;
		}

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			List<RelicModel> validRelics = GetValidRelics(((RelicModel)this).Owner).Take(((RelicModel)this).DynamicVars["Relics"].IntValue).ToList();
			if (validRelics.Count > 0)
			{
				List<Reward> relicRewards = validRelics.Select((RelicModel r) => (Reward)new RelicReward(r, ((RelicModel)this).Owner)).ToList();
				RewardsSet rewardsSet = new RewardsSet(((RelicModel)this).Owner, (RewardsSetSynchronizer)null).WithCustomRewards(relicRewards).WithSkippingDisallowed();
				await rewardsSet.Offer();
			}
			CurseCardPool cursePool = ModelDb.CardPool<CurseCardPool>();
			List<CardModel> availableCurses = (from c in ((CardPoolModel)cursePool).GetUnlockedCards(((RelicModel)this).Owner.UnlockState, ((RelicModel)this).Owner.RunState.CardMultiplayerConstraint)
				where c.CanBeGeneratedByModifiers
				select c).ToList();
			if (availableCurses.Count > 0)
			{
				CardModel curseTemplate = ((RelicModel)this).Owner.RunState.Rng.Niche.NextItem<CardModel>((IEnumerable<CardModel>)availableCurses);
				if (curseTemplate != null)
				{
					CardModel mutableCurse = ((ICardScope)((RelicModel)this).Owner.RunState).CreateCard(curseTemplate, ((RelicModel)this).Owner);
					CardPileAddResult addResult = await CardPileCmd.Add(mutableCurse, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
					CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)new List<CardPileAddResult> { addResult }, 0.75f, (CardPreviewStyle)1);
					await Cmd.Wait(0.5f, false);
				}
			}
		}
	}
	public sealed class WinePot : RelicModel
	{
		private const int DrawCount = 2;

		public override RelicRarity Rarity => (RelicRarity)6;

		public override string PackedIconPath => "res://wuwancients/images/relics/wine_pot.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/wine_pot.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/wine_pot.png";

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (player == ((RelicModel)this).Owner)
			{
				await CardPileCmd.Draw(choiceContext, 2m, player, false);
				PlayerCombatState playerCombatState = player.PlayerCombatState;
				object obj;
				if (playerCombatState == null)
				{
					obj = null;
				}
				else
				{
					CardPile hand2 = playerCombatState.Hand;
					obj = ((hand2 != null) ? hand2.Cards : null);
				}
				IReadOnlyList<CardModel> hand = (IReadOnlyList<CardModel>)obj;
				if (hand != null && hand.Count != 0)
				{
					int index = ((RelicModel)this).Owner.PlayerRng.Rewards.NextInt(0, hand.Count);
					CardModel discard = hand.ElementAt(index);
					await CardCmd.Discard(choiceContext, (IEnumerable<CardModel>)(object)new CardModel[1] { discard });
				}
			}
		}
	}
	public sealed class YingBaiLadosOldRelic : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool ShowCounter => false;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new CardsVar[1]
		{
			new CardsVar(3)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<ArmorBreakEnchantment>(1);

		public override string PackedIconPath => "res://wuwancients/images/relics/ying_bai_lado_old_relic.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/ying_bai_lado_old_relic.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/ying_bai_lado_old_relic.png";

		public override async Task AfterObtained()
		{
			foreach (CardModel item in await CardSelectCmd.FromDeckForEnchantment(((RelicModel)this).Owner, (EnchantmentModel)(object)ModelDb.Enchantment<ArmorBreakEnchantment>(), 1, (Func<CardModel, bool>)((CardModel? c) => c != null && (int)c.Type == 1), new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue)))
			{
				CardCmd.Enchant<ArmorBreakEnchantment>(item, 1m);
				NCardEnchantVfx nCardEnchantVfx = NCardEnchantVfx.Create(item);
				if (nCardEnchantVfx != null)
				{
					NRun instance = NRun.Instance;
					if (instance != null)
					{
						GodotTreeExtensions.AddChildSafely((Node)(object)instance.GlobalUi.CardPreviewContainer, (Node)(object)nCardEnchantVfx);
					}
				}
			}
		}
	}
	public sealed class YokuuTopology : RelicModel
	{
		private int _timesUsed;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/yokuu_topology.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/yokuu_topology.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/yokuu_topology.png";

		public override bool IsUsedUp => TimesUsed >= ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue;

		public override bool ShowCounter => ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue > 0 && TimesUsed < ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue;

		public override int DisplayAmount => ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue - TimesUsed;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new CardsVar(4) };

		[SavedProperty]
		public int TimesUsed
		{
			get
			{
				return _timesUsed;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_timesUsed = value;
				((RelicModel)this).InvokeDisplayAmountChanged();
				CheckIfUsedUp();
			}
		}

		private void CheckIfUsedUp()
		{
			if (((RelicModel)this).IsUsedUp)
			{
				((RelicModel)this).Status = (RelicStatus)2;
			}
		}

		public override bool TryModifyCardRewardOptionsLate(Player player, List<CardCreationResult> cardRewards, CardCreationOptions options)
		{
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			if (player != ((RelicModel)this).Owner)
			{
				return false;
			}
			if (TimesUsed >= ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue)
			{
				return false;
			}
			if (!((Enum)options.Flags).HasFlag((Enum)(object)(CardCreationFlags)128))
			{
				return false;
			}
			foreach (CardCreationResult cardReward in cardRewards)
			{
				CardModel card = cardReward.Card;
				if (card.IsUpgradable)
				{
					CardModel val = ((ICardScope)player.RunState).CloneCard(card);
					CardCmd.Upgrade(val, (CardPreviewStyle)1);
					cardReward.ModifyCard(val, (RelicModel)(object)this);
				}
			}
			return true;
		}

		public override Task AfterModifyingCardRewardOptions()
		{
			if (TimesUsed >= ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue)
			{
				return Task.CompletedTask;
			}
			TimesUsed++;
			return Task.CompletedTask;
		}
	}
	public sealed class YounoDumpling : RelicModel
	{
		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<Anchored>(1);

		public override string PackedIconPath => "res://wuwancients/images/relics/youno_dumpling.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/youno_dumpling.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/youno_dumpling.png";

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner == null)
			{
				return;
			}
			List<CardModel> targetCards = ((RelicModel)this).Owner.Deck.Cards.ToList();
			if (targetCards.Count == 0)
			{
				return;
			}
			Player owner = ((RelicModel)this).Owner;
			Anchored anchored = ModelDb.Enchantment<Anchored>();
			CardSelectorPrefs val = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 0, 1);
			((CardSelectorPrefs)(ref val)).set_Cancelable(false);
			((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
			foreach (CardModel card in await CardSelectCmd.FromDeckForEnchantment(owner, (EnchantmentModel)(object)anchored, 1, (Func<CardModel, bool>)null, val))
			{
				CardCmd.Enchant<Anchored>(card, 1m);
			}
		}
	}
	public sealed class ZaniDango : RelicModel
	{
		private const decimal MaxHpPercent = 0.25m;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/zani_dango.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/zani_dango.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/zani_dango.png";

		public override async Task BeforeCombatStart()
		{
			Player owner = ((RelicModel)this).Owner;
			Creature me = ((owner != null) ? owner.Creature : null);
			if (((me != null) ? me.CombatState : null) == null)
			{
				return;
			}
			BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
			List<Creature> enemies = me.CombatState.HittableEnemies.ToList();
			foreach (Creature enemy in enemies)
			{
				if (enemy != null && enemy.IsAlive)
				{
					decimal amount = Math.Max(1m, Math.Floor((decimal)enemy.MaxHp * 0.25m));
					await CreatureCmd.Damage((PlayerChoiceContext)(object)ctx, enemy, amount, (ValueProp)6, (CardModel)null, (CardPlay)null);
				}
			}
			((RelicModel)this).Flash();
		}
	}
	public sealed class ZannisSalaryCard : RelicModel
	{
		private const decimal GoldOnPickup = 100m;

		private const int HpLossPerGold = 20;

		private const decimal GoldPerTick = 50m;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override bool HasUponPickupEffect => true;

		public override bool ShowCounter => false;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("GoldAmount", 100m)
		};

		public override string PackedIconPath => "res://wuwancients/images/relics/zannis_salary_card.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/zannis_salary_card.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/zannis_salary_card.png";

		[SavedProperty]
		public int AccumulatedHpLoss { get; set; }

		public override async Task AfterObtained()
		{
			if (((RelicModel)this).Owner != null)
			{
				await PlayerCmd.GainGold(100m, ((RelicModel)this).Owner, false);
			}
		}

		public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			if (!RunManager.Instance.IsSingleplayerOrFakeMultiplayer)
			{
				return;
			}
			Player owner = ((RelicModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null && target == ((RelicModel)this).Owner.Creature && !((decimal)result.UnblockedDamage <= 0m))
			{
				AccumulatedHpLoss += result.UnblockedDamage;
				while (AccumulatedHpLoss >= 20)
				{
					AccumulatedHpLoss -= 20;
					await PlayerCmd.GainGold(50m, ((RelicModel)this).Owner, false);
				}
			}
		}
	}
	public sealed class ZheyingWancai : RelicModel
	{
		private const int ChoiceCount = 5;

		public override RelicRarity Rarity => (RelicRarity)7;

		public override string PackedIconPath => "res://wuwancients/images/relics/zheying_wancai.png";

		protected override string PackedIconOutlinePath => "res://wuwancients/images/relics/zheying_wancai.png";

		protected override string BigIconPath => "res://wuwancients/images/relics/zheying_wancai.png";

		public override async Task AfterObtained()
		{
			Player player = ((RelicModel)this).Owner;
			if (player == null)
			{
				return;
			}
			List<CardModel> pool = GetAncientCardPool();
			if (pool.Count == 0)
			{
				return;
			}
			Rng rng = player.RunState.Rng.UpFront;
			List<CardModel> remaining = new List<CardModel>(pool);
			List<CardCreationResult> choices = new List<CardCreationResult>();
			for (int i = 0; i < 5; i++)
			{
				if (remaining.Count <= 0)
				{
					break;
				}
				CardModel picked = remaining[rng.NextInt(remaining.Count)];
				remaining.Remove(picked);
				choices.Add(new CardCreationResult(((ICardScope)player.RunState).CreateCard(picked, player)));
			}
			CardSelectorPrefs val = new CardSelectorPrefs(new LocString("relics", "ZHEYING_WANCAI_SELECT_PROMPT"), 1, 1);
			((CardSelectorPrefs)(ref val)).set_Cancelable(false);
			((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
			CardSelectorPrefs prefs = val;
			foreach (CardModel chosen in await CardSelectCmd.FromSimpleGridForRewards((PlayerChoiceContext)new BlockingPlayerChoiceContext(), choices, player, prefs))
			{
				CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(chosen, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
			}
		}

		private static List<CardModel> GetAncientCardPool()
		{
			return ModelDb.AllCards.Where((CardModel c) => (int)c.Rarity == 5).ToList();
		}
	}
}
namespace wuwancients.Powers
{
	public sealed class AnotherStrokePower : CustomPowerModel
	{
		private bool _ready;

		private bool _pendingArm;

		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)2;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/another_stroke.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/another_stroke.png";

		public override Task AfterEnergySpent(CardModel card, int amount)
		{
			Creature owner = ((PowerModel)this).Owner;
			if (((owner != null) ? owner.Player : null) == null)
			{
				return Task.CompletedTask;
			}
			if (card.Owner != ((PowerModel)this).Owner.Player)
			{
				return Task.CompletedTask;
			}
			if (amount <= 0)
			{
				return Task.CompletedTask;
			}
			if (_ready || _pendingArm)
			{
				return Task.CompletedTask;
			}
			PlayerCombatState playerCombatState = ((PowerModel)this).Owner.Player.PlayerCombatState;
			if (((playerCombatState != null && playerCombatState.Energy != 0) ? 1 : 0) <= (false ? 1 : 0))
			{
				_pendingArm = true;
			}
			return Task.CompletedTask;
		}

		public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
		{
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Invalid comparison between Unknown and I4
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Invalid comparison between Unknown and I4
			modifiedCost = originalCost;
			if (_ready)
			{
				Creature owner = ((PowerModel)this).Owner;
				if (((owner != null) ? owner.Player : null) != null)
				{
					if (card.Owner != ((PowerModel)this).Owner.Player)
					{
						return false;
					}
					CardPile pile = card.Pile;
					PileType? val = ((pile != null) ? new PileType?(pile.Type) : null);
					if ((int)val.GetValueOrDefault() != 2 && (int)val.GetValueOrDefault() != 5)
					{
						return false;
					}
					modifiedCost = default(decimal);
					return true;
				}
			}
			return false;
		}

		public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			if (((cardPlay != null) ? cardPlay.Card : null) != null)
			{
				Creature owner = ((PowerModel)this).Owner;
				if (((owner != null) ? owner.Player : null) != null)
				{
					if (cardPlay.Card.Owner != ((PowerModel)this).Owner.Player)
					{
						return Task.CompletedTask;
					}
					if (_pendingArm)
					{
						_pendingArm = false;
						_ready = true;
						((PowerModel)this).Flash();
						PlayerCombatState playerCombatState = ((PowerModel)this).Owner.Player.PlayerCombatState;
						if (playerCombatState != null)
						{
							playerCombatState.RecalculateCardValues();
						}
						return Task.CompletedTask;
					}
					if (!_ready)
					{
						return Task.CompletedTask;
					}
					_ready = false;
					PlayerCombatState playerCombatState2 = ((PowerModel)this).Owner.Player.PlayerCombatState;
					if (playerCombatState2 != null)
					{
						playerCombatState2.RecalculateCardValues();
					}
					return Task.CompletedTask;
				}
			}
			return Task.CompletedTask;
		}

		public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			if (((PowerModel)this).Owner != null && participants.Contains(((PowerModel)this).Owner))
			{
				_ready = false;
				_pendingArm = false;
			}
			return Task.CompletedTask;
		}
	}
	public sealed class AugustaDumplingAllyPower : CustomPowerModel
	{
		private int _turnsRemaining;

		private bool _initialized;

		private readonly List<Creature> _subscribedCreatures = new List<Creature>();

		private static readonly MethodInfo? _applyMethod = typeof(PowerCmd).GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault((MethodInfo m) => m.Name == "Apply" && m.IsGenericMethod && m.GetParameters().Length == 6 && m.GetParameters()[1].ParameterType == typeof(Creature));

		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/augusta_dumpling_ally.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/augusta_dumpling_ally.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override Task AfterApplied(Creature? applier, CardModel? cardSource)
		{
			_turnsRemaining = ((PowerModel)this).Amount;
			_initialized = true;
			Creature owner = ((PowerModel)this).Owner;
			if (((owner != null) ? owner.CombatState : null) != null)
			{
				IReadOnlyList<Creature> teammatesOf = ((PowerModel)this).Owner.CombatState.GetTeammatesOf(((PowerModel)this).Owner);
				if (teammatesOf != null)
				{
					foreach (Creature item in teammatesOf)
					{
						if (item != ((PowerModel)this).Owner && item.IsAlive)
						{
							item.PowerApplied += OnTeammatePowerApplied;
							_subscribedCreatures.Add(item);
						}
					}
				}
			}
			return Task.CompletedTask;
		}

		private async void OnTeammatePowerApplied(PowerModel power)
		{
			if (!_initialized || ((PowerModel)this).Owner == null || !((PowerModel)this).Owner.IsAlive || power == null || (int)power.Type != 1)
			{
				return;
			}
			try
			{
				Type powerType = ((object)power).GetType();
				if (_applyMethod != null)
				{
					MethodInfo genericApply = _applyMethod.MakeGenericMethod(powerType);
					object?[] parameters = new object[6]
					{
						(object)new ThrowingPlayerChoiceContext(),
						((PowerModel)this).Owner,
						power.Amount,
						((PowerModel)this).Owner,
						null,
						false
					};
					Task task = (Task)genericApply.Invoke(null, parameters);
					await task;
				}
			}
			catch
			{
			}
		}

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (!_initialized || ((PowerModel)this).Owner == null || !((PowerModel)this).Owner.IsAlive)
			{
				return;
			}
			if (((player != null) ? player.Creature : null) == ((PowerModel)this).Owner)
			{
				_turnsRemaining--;
				if (_turnsRemaining <= 0)
				{
					await PowerCmd.Remove((PowerModel)(object)this);
				}
			}
		}

		public override Task AfterRemoved(Creature oldOwner)
		{
			foreach (Creature subscribedCreature in _subscribedCreatures)
			{
				if (subscribedCreature != null)
				{
					subscribedCreature.PowerApplied -= OnTeammatePowerApplied;
				}
			}
			_subscribedCreatures.Clear();
			return Task.CompletedTask;
		}
	}
	public class BowlbugNectarPower : TemporaryStrengthPower, ICustomPower, ICustomModel
	{
		public override AbstractModel OriginModel => (AbstractModel)(object)ModelDb.Relic<ChimerasHeart>();

		public string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/strength_power.tres";

		public string? CustomBigIconPath => "res://images/powers/strength_power.png";
	}
	public sealed class DiffusePower : CustomPowerModel
	{
		private const int MaxTotalJellyfish = 21;

		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/diffuse_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/diffuse_power.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult results, ValueProp props, Creature target, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			int num;
			if (dealer == ((PowerModel)this).Owner && !((decimal)results.UnblockedDamage <= 0m))
			{
				Creature owner = ((PowerModel)this).Owner;
				num = ((((owner != null) ? owner.CombatState : null) == null) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
			if (num != 0)
			{
				return;
			}
			int totalJellyfish = 0;
			foreach (Player p in ((PowerModel)this).Owner.CombatState.Players)
			{
				if (((p != null) ? p.Creature : null) != null)
				{
					PowerModel jp = ((IEnumerable<PowerModel>)p.Creature.Powers).FirstOrDefault((Func<PowerModel, bool>)((PowerModel pw) => pw is DreamWeaverJellyfishPower));
					if (jp != null)
					{
						totalJellyfish += jp.Amount;
					}
				}
			}
			if (totalJellyfish < 21)
			{
				await PowerCmd.Apply<DreamWeaverJellyfishPower>(choiceContext, ((PowerModel)this).Owner, 1m, ((PowerModel)this).Owner, cardSource, false);
			}
		}
	}
	public sealed class DreamWeaverJellyfishPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/dream_weaver_jellyfish.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/dream_weaver_jellyfish.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		[SavedProperty]
		public int StacksRecent { get; set; }

		[SavedProperty]
		public int StacksExpireThis { get; set; }

		[SavedProperty]
		public int StacksExpireNext { get; set; }

		public override decimal ModifyDamageAdditive(Creature? target, decimal damage, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
		{
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Invalid comparison between Unknown and I4
			if (dealer != ((PowerModel)this).Owner || ((PowerModel)this).Amount <= 0)
			{
				return 0m;
			}
			if (cardSource == null || (int)cardSource.Type != 1)
			{
				return 0m;
			}
			return ((PowerModel)this).Amount;
		}

		public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
		{
			if (power == this && !(amount <= 0m))
			{
				StacksRecent += (int)amount;
				((PowerModel)this).SetAmount(StacksRecent + StacksExpireThis + StacksExpireNext, false);
				await Task.CompletedTask;
			}
		}

		public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			if (((PowerModel)this).Owner != null && ((PowerModel)this).Owner.IsAlive && (int)side == 1)
			{
				StacksExpireNext = StacksExpireThis;
				StacksExpireThis = StacksRecent;
				StacksRecent = 0;
				((PowerModel)this).SetAmount(StacksExpireThis + StacksExpireNext, false);
				await Task.CompletedTask;
			}
		}
	}
	public sealed class EndlessPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)2;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/endless_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/endless_power.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
		{
			Creature owner = ((PowerModel)this).Owner;
			if (((owner != null) ? owner.Player : null) != null && card != null && card.Owner == ((PowerModel)this).Owner.Player)
			{
				await CardPileCmd.Draw(choiceContext, 1m, ((PowerModel)this).Owner.Player, false);
			}
		}
	}
	public sealed class EntropyIllusionPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)0;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/entropy_illusion.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/entropy_illusion.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
		{
			if (dealer == ((PowerModel)this).Owner)
			{
				return 1.3m;
			}
			return 1m;
		}

		public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
		{
			if (((PowerModel)this).Owner != null)
			{
				EntropyStagePower stagePower = ((PowerModel)this).Owner.GetPower<EntropyStagePower>();
				if (stagePower != null)
				{
					await PowerCmd.Remove((PowerModel)(object)stagePower);
				}
			}
		}
	}
	public sealed class EntropyStagePower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/entropy_stage.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/entropy_stage.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (((PowerModel)this).Owner == null || !((PowerModel)this).Owner.IsAlive)
			{
				return;
			}
			if (((player != null) ? player.Creature : null) == ((PowerModel)this).Owner)
			{
				await PowerCmd.Apply<VirtualParticlePower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((PowerModel)this).Owner, 1m, ((PowerModel)this).Owner, (CardModel)null, false);
				await PowerCmd.Decrement((PowerModel)(object)this);
				if (((PowerModel)this).Amount <= 0)
				{
					await PowerCmd.Remove((PowerModel)(object)this);
				}
			}
		}

		public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
		{
			if (((PowerModel)this).Owner != null)
			{
				EntropyIllusionPower illusionPower = ((PowerModel)this).Owner.GetPower<EntropyIllusionPower>();
				if (illusionPower != null)
				{
					await PowerCmd.Remove((PowerModel)(object)illusionPower);
				}
			}
		}
	}
	public sealed class ExhaustedMindPower : CustomPowerModel
	{
		private sealed class StacksDynamicVar : DynamicVar
		{
			private readonly ExhaustedMindPower _power;

			public StacksDynamicVar(ExhaustedMindPower power)
				: base("Stacks", 0m)
			{
				_power = power;
			}

			protected override decimal GetBaseValueForIConvertible()
			{
				return ((PowerModel)_power).Amount;
			}
		}

		public override PowerType Type => (PowerType)2;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/exhausted_mind.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/exhausted_mind.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new StacksDynamicVar(this)
		};

		public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if (((PowerModel)this).Owner != null && side == ((PowerModel)this).Owner.Side)
			{
				Player player = ((PowerModel)this).Owner.Player;
				PlayerCombatState playerCombatState = ((player != null) ? player.PlayerCombatState : null);
				if (playerCombatState != null)
				{
					playerCombatState.LoseEnergy((decimal)playerCombatState.Energy);
					await PowerCmd.Decrement((PowerModel)(object)this);
				}
			}
		}
	}
	public sealed class FreezePower : CustomPowerModel
	{
		public const int StacksToFreeze = 5;

		private bool _converting;

		public override PowerType Type => (PowerType)2;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/freeze.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/freeze.png";

		public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
		{
			if (power != this || _converting)
			{
				return;
			}
			Creature me = ((PowerModel)this).Owner;
			if (me == null || !me.IsAlive || ((PowerModel)this).Amount < 5)
			{
				return;
			}
			_converting = true;
			try
			{
				for (int i = 0; i < 20; i++)
				{
					if (((PowerModel)this).Amount < 5)
					{
						break;
					}
					for (int j = 0; j < 5; j++)
					{
						await PowerCmd.Decrement((PowerModel)(object)this);
					}
					await CreatureCmd.Stun(me, (string)null);
				}
			}
			finally
			{
				_converting = false;
			}
		}
	}
	public class FullMoonFieldPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/full_moon_field_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/full_moon_field_power.png";

		public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if (((PowerModel)this).Owner == null || ((PowerModel)this).Owner.Side != side)
			{
				return;
			}
			if (combatState.RoundNumber % 2 == 1)
			{
				NAudioManager instance = NAudioManager.Instance;
				if (instance != null)
				{
					instance.PlayOneShot($"res://wuwancients/audio/full_moon_field_power_heal_{Random.Shared.Next(1, 4)}.wav", 1f);
				}
				await CreatureCmd.Heal(((PowerModel)this).Owner, 6m, true);
			}
			else
			{
				NAudioManager instance2 = NAudioManager.Instance;
				if (instance2 != null)
				{
					instance2.PlayOneShot($"res://wuwancients/audio/full_moon_field_power_block_{Random.Shared.Next(1, 4)}.wav", 1f);
				}
				await CreatureCmd.GainBlock(((PowerModel)this).Owner, 6m, (ValueProp)8, (CardPlay)null, false);
			}
		}

		public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			if (creature == ((PowerModel)this).Owner && !(amount <= 0m))
			{
				PaleDeathBlessingPower existing = creature.GetPower<PaleDeathBlessingPower>();
				if (existing == null || ((PowerModel)existing).Amount < 5)
				{
					await PowerCmd.Apply<PaleDeathBlessingPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((PowerModel)this).Owner, 1m, ((PowerModel)this).Owner, (CardModel)null, false);
				}
			}
		}
	}
	public sealed class FuluoluoTakeoverCountPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)2;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/fuluoluo_takeover_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/fuluoluo_takeover_power.png";

		protected override bool IsVisibleInternal => false;
	}
	public sealed class FuluoluoTakeoverPower : CustomPowerModel
	{
		private const string LoopAudio = "res://wuwancients/audio/conductor_loop.wav";

		private const int MaxAutoPlays = 60;

		private static AudioStreamPlayer? _longPlayer;

		private static readonly object BgmHolder = new object();

		public override PowerType Type => (PowerType)2;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/fuluoluo_takeover_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/fuluoluo_takeover_power.png";

		public override Task AfterApplied(Creature? applier, CardModel? cardSource)
		{
			PlayLongSfx();
			return Task.CompletedTask;
		}

		public override async Task AfterAutoPrePlayPhaseEnteredLate(PlayerChoiceContext choiceContext, Player player)
		{
			if (((PowerModel)this).Owner == null || ((PowerModel)this).Amount <= 0)
			{
				return;
			}
			if (((player != null) ? player.Creature : null) != ((PowerModel)this).Owner)
			{
				return;
			}
			ICombatState combatState = ((PowerModel)this).Owner.CombatState;
			if (combatState == null)
			{
				return;
			}
			for (int played = 0; played < 60; played++)
			{
				if (CombatManager.Instance == null)
				{
					break;
				}
				if (CombatManager.Instance.IsOverOrEnding)
				{
					break;
				}
				if (CombatManager.Instance.IsPlayerReadyToEndTurn(player))
				{
					break;
				}
				CardPile hand = PileTypeExtensions.GetPile((PileType)2, player);
				if (hand == null)
				{
					break;
				}
				List<CardModel> candidates = hand.Cards.Where((CardModel c) => c.CanPlay()).ToList();
				if (candidates.Count == 0)
				{
					break;
				}
				CardModel card = SyncedRng.Pick(SyncedRng.Cards(player.RunState), candidates);
				if (card == null)
				{
					break;
				}
				Creature target = GetTarget(card, combatState, player);
				if (target == null && (int)card.TargetType != 1 && (int)card.TargetType > 0)
				{
					break;
				}
				await card.SpendResources();
				await CardCmd.AutoPlay(choiceContext, card, target, (AutoPlayType)1, true, false);
			}
		}

		public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			if (((PowerModel)this).Owner != null && participants.Contains(((PowerModel)this).Owner) && ((PowerModel)this).Amount > 0)
			{
				await PowerCmd.Decrement((PowerModel)(object)this);
			}
		}

		public override Task AfterRemoved(Creature? owner)
		{
			StopLongSfx();
			return Task.CompletedTask;
		}

		public override Task AfterCombatEnd(CombatRoom room)
		{
			StopLongSfx();
			return Task.CompletedTask;
		}

		internal static void PlayLongSfx()
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Expected O, but got Unknown
			if (_longPlayer == null || !GodotObject.IsInstanceValid((GodotObject)(object)_longPlayer) || !((Node)_longPlayer).IsInsideTree())
			{
				_longPlayer = new AudioStreamPlayer();
				_longPlayer.Stream = GD.Load<AudioStream>("res://wuwancients/audio/conductor_loop.wav");
				_longPlayer.Finished += delegate
				{
					BgmDucker.Release(BgmHolder);
				};
				NGame instance = NGame.Instance;
				if (instance == null)
				{
					return;
				}
				((Node)instance).AddChild((Node)(object)_longPlayer, false, (InternalMode)0);
			}
			BgmDucker.Acquire(BgmHolder);
			if (_longPlayer.Playing)
			{
				_longPlayer.Stop();
			}
			_longPlayer.Play(0f);
		}

		internal static void StopLongSfx()
		{
			if (_longPlayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_longPlayer) && _longPlayer.Playing)
			{
				_longPlayer.Stop();
			}
			BgmDucker.Release(BgmHolder);
		}

		private Creature? GetTarget(CardModel card, ICombatState combatState, Player player)
		{
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Expected I4, but got Unknown
			IRunState runState = player.RunState;
			object obj;
			if (runState == null)
			{
				obj = null;
			}
			else
			{
				RunRngSet rng = runState.Rng;
				obj = ((rng != null) ? rng.CombatTargets : null);
			}
			Rng val = (Rng)obj;
			TargetType targetType = card.TargetType;
			if (1 == 0)
			{
			}
			Creature result = (Creature)((targetType - 2) switch
			{
				0 => combatState.HittableEnemies?.FirstOrDefault(), 
				4 => (val != null) ? val.NextItem<Creature>(combatState.Allies?.Where((Creature c) => c != null && c.IsAlive && c.IsPlayer && c != ((PowerModel)this).Owner) ?? Enumerable.Empty<Creature>()) : null, 
				3 => ((PowerModel)this).Owner, 
				_ => null, 
			});
			if (1 == 0)
			{
			}
			return result;
		}
	}
	public class FuryPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/fury_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/fury_power.png";

		public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
		{
			if (dealer == ((PowerModel)this).Owner && ((PowerModel)this).Amount > 0)
			{
				return 2m;
			}
			return 1m;
		}

		public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			if (side == ((PowerModel)this).Owner.Side)
			{
				await PowerCmd.Decrement((PowerModel)(object)this);
			}
		}
	}
	public class HasHatPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)0;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/has_hat_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/has_hat_power.png";

		public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
		{
			if (((PowerModel)this).Owner == null)
			{
				return 1m;
			}
			if (((PowerModel)this).Owner.CurrentHp > ((PowerModel)this).Owner.MaxHp / 2)
			{
				return 1m;
			}
			if (target == ((PowerModel)this).Owner && dealer != ((PowerModel)this).Owner)
			{
				return 0.70m;
			}
			return 1m;
		}
	}
	public sealed class HeroKingsBloodPower : CustomPowerModel
	{
		private int _turnsRemaining;

		private bool _initialized;

		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/hero_kings_blood.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/hero_kings_blood.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override Task AfterApplied(Creature? applier, CardModel? cardSource)
		{
			_turnsRemaining = ((PowerModel)this).Amount;
			_initialized = true;
			return Task.CompletedTask;
		}

		public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			if (!_initialized || ((PowerModel)this).Owner == null || !((PowerModel)this).Owner.IsAlive)
			{
				return;
			}
			if (((player != null) ? player.Creature : null) == ((PowerModel)this).Owner && _turnsRemaining > 0)
			{
				await CreatureCmd.Heal(((PowerModel)this).Owner, 20m, true);
				await CardPileCmd.Draw(choiceContext, 3m, player, false);
				await PlayerCmd.GainEnergy(2m, player);
				_turnsRemaining--;
				if (_turnsRemaining <= 0)
				{
					await PowerCmd.Remove((PowerModel)(object)this);
				}
			}
		}
	}
	public sealed class KarmaFirePower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)((((AbstractModel)this).IsMutable && ((PowerModel)this).Owner != null && (int)((PowerModel)this).Owner.Side == 1) ? 1 : 2);

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/karma_fire.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/karma_fire.png";

		public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if (((PowerModel)this).Owner != null && ((PowerModel)this).Owner.IsAlive && (int)((PowerModel)this).Owner.Side != 1 && (int)side == 2 && ((PowerModel)this).Amount > 0)
			{
				((PowerModel)this).Flash();
				await CreatureCmd.Damage((PlayerChoiceContext)new BlockingPlayerChoiceContext(), ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, (ValueProp)6, (CardModel)null, (CardPlay)null);
				if (!((PowerModel)this).Owner.IsAlive)
				{
					PlayKillVoice();
				}
			}
		}

		public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			if (((PowerModel)this).Owner != null && ((PowerModel)this).Owner.IsAlive && dealer == ((PowerModel)this).Owner && (int)((PowerModel)this).Owner.Side != 1 && target != null && (int)target.Side == 1 && ((PowerModel)this).Amount > 0 && result.TotalDamage > 0)
			{
				Player player = target.Player;
				if (((player != null) ? player.GetRelic<GalbrenasKarmaFire>() : null) != null)
				{
					int moved = ((PowerModel)this).Amount;
					await PowerCmd.Remove((PowerModel)(object)this);
					await PowerCmd.Apply<KarmaFirePower>(choiceContext, target, (decimal)moved, ((PowerModel)this).Owner, (CardModel)null, false);
					PlayTransferVoice();
				}
			}
		}

		public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			if (((PowerModel)this).Owner != null && ((PowerModel)this).Owner.IsAlive && target == ((PowerModel)this).Owner && (int)((PowerModel)this).Owner.Side == 1 && dealer != null && dealer.Side != ((PowerModel)this).Owner.Side && result.TotalDamage > 0)
			{
				KarmaFirePower power = ((PowerModel)this).Owner.GetPower<KarmaFirePower>();
				decimal stacks = ((decimal?)((power != null) ? new int?(((PowerModel)power).Amount) : null)) ?? 0m;
				if (!(stacks <= 0m))
				{
					((PowerModel)this).Flash();
					await CreatureCmd.Heal(((PowerModel)this).Owner, stacks, true);
				}
			}
		}

		private void PlayTransferVoice()
		{
			PlayRandomVoice("karma");
		}

		private void PlayKillVoice()
		{
			PlayRandomVoice("kill");
		}

		private void PlayRandomVoice(string set)
		{
			Creature owner = ((PowerModel)this).Owner;
			bool? obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				ICombatState combatState = owner.CombatState;
				obj = ((combatState == null) ? null : combatState.Players?.Any((Player p) => p.GetRelic<GalbrenasKarmaFire>() != null));
			}
			bool? flag = obj;
			if (flag.GetValueOrDefault())
			{
				NAudioManager instance = NAudioManager.Instance;
				if (instance != null)
				{
					instance.PlayOneShot($"res://wuwancients/audio/galbrena_{set}_{Random.Shared.Next(1, 4)}.wav", 1f);
				}
			}
		}
	}
	public class KingsDomain : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/kings_domain.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/kings_domain.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
		{
			if (((PowerModel)this).Owner != null && ((PowerModel)this).Owner.IsAlive && ((PowerModel)this).Owner.GetPower<FullMoonFieldPower>() != null)
			{
				PaleDeathBlessingPower existing = ((PowerModel)this).Owner.GetPower<PaleDeathBlessingPower>();
				if (existing != null)
				{
					await PowerCmd.Remove((PowerModel)(object)existing);
				}
				await PowerCmd.Apply<PaleDeathBlessingPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((PowerModel)this).Owner, 5m, ((PowerModel)this).Owner, (CardModel)null, false);
			}
		}

		public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			if (((PowerModel)this).Owner == null || !((PowerModel)this).Owner.IsAlive || cardPlay.Card == null)
			{
				return;
			}
			ICombatState combatState = ((PowerModel)this).Owner.CombatState;
			IReadOnlyList<Creature> allies = ((combatState != null) ? combatState.GetTeammatesOf(((PowerModel)this).Owner) : null);
			if (allies == null)
			{
				return;
			}
			foreach (Creature ally in allies)
			{
				if (ally != ((PowerModel)this).Owner && ally.IsAlive && ally.GetPower<ProstrationMoment>() != null)
				{
					await CreatureCmd.GainBlock(ally, 2m, (ValueProp)4, (CardPlay)null, false);
				}
			}
		}

		public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			if (((PowerModel)this).Owner != null && participants.Contains(((PowerModel)this).Owner))
			{
				await PowerCmd.Remove((PowerModel)(object)this);
			}
		}
	}
	public class Majesty : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/majesty.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/majesty.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();
	}
	public class MassEnergyEquivalencePower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)0;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/mass_energy_equivalence.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/mass_energy_equivalence.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
		{
			HoverTipFactory.FromPower<StrengthPower>((int?)null),
			HoverTipFactory.FromPower<DexterityPower>((int?)null)
		};

		public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
		{
			if (((PowerModel)this).Owner != null)
			{
				Creature owner = power.Owner;
				if (owner != null && (int)owner.Side == 2 && !(amount <= 0m) && (int)power.TypeForCurrentAmount == 1 && power != this)
				{
					await PowerCmd.Apply<StrengthPower>(choiceContext, ((PowerModel)this).Owner, 1m, ((PowerModel)this).Owner, (CardModel)null, false);
					await PowerCmd.Apply<DexterityPower>(choiceContext, ((PowerModel)this).Owner, 1m, ((PowerModel)this).Owner, (CardModel)null, false);
				}
			}
		}
	}
	public sealed class MassGravePower : CustomPowerModel
	{
		private int _selfDoom = 3;

		private int _enemyDoom = 10;

		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)2;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/mass_grave.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/mass_grave.png";

		[SavedProperty]
		public int SelfDoom
		{
			get
			{
				return _selfDoom;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_selfDoom = value;
			}
		}

		[SavedProperty]
		public int EnemyDoom
		{
			get
			{
				return _enemyDoom;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_enemyDoom = value;
			}
		}

		public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if (((PowerModel)this).Owner != null && ((PowerModel)this).Owner.Side == side)
			{
				ThrowingPlayerChoiceContext context = new ThrowingPlayerChoiceContext();
				await PowerCmd.Apply<DoomPower>((PlayerChoiceContext)(object)context, ((PowerModel)this).Owner, (decimal)SelfDoom, ((PowerModel)this).Owner, (CardModel)null, false);
				List<Creature> enemies = combatState.HittableEnemies.ToList();
				if (enemies.Count > 0)
				{
					await PowerCmd.Apply<DoomPower>((PlayerChoiceContext)(object)context, (IEnumerable<Creature>)enemies, (decimal)EnemyDoom, ((PowerModel)this).Owner, (CardModel)null, false);
				}
			}
		}
	}
	public sealed class NeuralNetworkPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)2;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/neural_network_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/neural_network_power.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override async Task AfterOrbChanneled(PlayerChoiceContext choiceContext, Player player, OrbModel orb)
		{
			int num;
			if (player != null)
			{
				Creature owner = ((PowerModel)this).Owner;
				num = ((player != ((owner != null) ? owner.Player : null)) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
			if (num == 0)
			{
				await CardPileCmd.Draw(choiceContext, 1m, player, false);
			}
		}

		public override async Task AfterOrbEvoked(PlayerChoiceContext choiceContext, OrbModel orb, IEnumerable<Creature> targets)
		{
			int num;
			if (((orb != null) ? orb.Owner : null) != null)
			{
				Player owner = orb.Owner;
				Creature owner2 = ((PowerModel)this).Owner;
				num = ((owner != ((owner2 != null) ? owner2.Player : null)) ? 1 : 0);
			}
			else
			{
				num = 1;
			}
			if (num == 0)
			{
				await CardPileCmd.Draw(choiceContext, 1m, orb.Owner, false);
			}
		}
	}
	public class NoBlockForeverPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)2;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/no_block_forever_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/no_block_forever_power.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.Static((StaticHoverTip)5, Array.Empty<DynamicVar>()) };

		public override decimal ModifyBlockMultiplicative(Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
		{
			if (target != ((PowerModel)this).Owner)
			{
				return 1m;
			}
			return 0m;
		}
	}
	public class NoHatPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)0;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/no_hat_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/no_hat_power.png";

		public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
		{
			if (((PowerModel)this).Owner == null)
			{
				return 1m;
			}
			if (((PowerModel)this).Owner.CurrentHp <= ((PowerModel)this).Owner.MaxHp / 2)
			{
				return 1m;
			}
			if (dealer == ((PowerModel)this).Owner && target != ((PowerModel)this).Owner)
			{
				return 1.30m;
			}
			return 1m;
		}
	}
	public sealed class OverclockPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/overclock.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/overclock.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override int ModifyAttackHitCount(AttackCommand attackCommand, int originalHitCount)
		{
			if (attackCommand.Attacker == ((PowerModel)this).Owner && originalHitCount > 1)
			{
				return originalHitCount + ((PowerModel)this).Amount;
			}
			return originalHitCount;
		}
	}
	public class PaleDeathBlessingPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/pale_death_blessing_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/pale_death_blessing_power.png";

		public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
		{
			if (dealer == ((PowerModel)this).Owner && ((PowerModel)this).Amount > 0)
			{
				return 1m + (decimal)((PowerModel)this).Amount * 0.1m;
			}
			return 1m;
		}

		public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			Creature owner = ((PowerModel)this).Owner;
			if ((CombatSide?)side == ((owner != null) ? new CombatSide?(owner.Side) : null) && ((PowerModel)this).Amount > 0)
			{
				await PowerCmd.Remove((PowerModel)(object)this);
			}
		}
	}
	public class PredestinedDeathPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/predestined_death_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/predestined_death_power.png";

		public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			Creature owner = ((PowerModel)this).Owner;
			if ((CombatSide?)side == ((owner != null) ? new CombatSide?(owner.Side) : null) && ((PowerModel)this).Owner.CombatState != null)
			{
				List<Creature> doomedEnemies = GetDoomedCreaturesNow(((PowerModel)this).Owner.CombatState);
				if (doomedEnemies.Any())
				{
					await DoomPower.DoomKill((IReadOnlyList<Creature>)doomedEnemies);
				}
			}
			await <>n__0(choiceContext, side, participants);
		}

		private List<Creature> GetDoomedCreaturesNow(ICombatState combatState)
		{
			IReadOnlyList<Creature> creaturesOnSide = combatState.GetCreaturesOnSide((CombatSide)2);
			return creaturesOnSide.Where(delegate(Creature e)
			{
				DoomPower power = e.GetPower<DoomPower>();
				return power != null && e.CurrentHp <= ((PowerModel)power).Amount;
			}).ToList();
		}

		[CompilerGenerated]
		[DebuggerHidden]
		private Task <>n__0(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			return ((AbstractModel)this).AfterSideTurnEnd(choiceContext, side, participants);
		}
	}
	public class ProstrationMoment : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/prostration_moment.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/prostration_moment.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
		{
			if (((PowerModel)this).Owner != null && ((PowerModel)this).Owner.IsAlive && ((PowerModel)this).Owner.GetPower<FullMoonFieldPower>() != null)
			{
				PaleDeathBlessingPower existing = ((PowerModel)this).Owner.GetPower<PaleDeathBlessingPower>();
				if (existing != null)
				{
					await PowerCmd.Remove((PowerModel)(object)existing);
				}
				await PowerCmd.Apply<PaleDeathBlessingPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((PowerModel)this).Owner, 5m, ((PowerModel)this).Owner, (CardModel)null, false);
			}
		}

		public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Invalid comparison between Unknown and I4
			if (((PowerModel)this).Owner != null)
			{
				Player owner = card.Owner;
				if (((owner != null) ? owner.Creature : null) == ((PowerModel)this).Owner)
				{
					if ((int)autoPlayType > 0)
					{
						return true;
					}
					Type type = ((object)card).GetType();
					if (type == typeof(BlazingSun) || type == typeof(ImmortalPurge))
					{
						return true;
					}
					return false;
				}
			}
			return true;
		}

		public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			if (((PowerModel)this).Owner == null || !((PowerModel)this).Owner.IsAlive || side != ((PowerModel)this).Owner.Side)
			{
				return;
			}
			ICombatState combatState = ((PowerModel)this).Owner.CombatState;
			if (combatState == null)
			{
				return;
			}
			Player player = ((PowerModel)this).Owner.Player;
			object obj;
			if (player == null)
			{
				obj = null;
			}
			else
			{
				PlayerCombatState playerCombatState = player.PlayerCombatState;
				obj = ((playerCombatState != null) ? playerCombatState.Hand : null);
			}
			CardPile hand = (CardPile)obj;
			if (hand != null)
			{
				CardModel purge = ((IEnumerable<CardModel>)hand.Cards).FirstOrDefault((Func<CardModel, bool>)((CardModel c) => c is ImmortalPurge));
				if (purge != null)
				{
					await CardCmd.AutoPlay(choiceContext, purge, (Creature)null, (AutoPlayType)1, false, false);
				}
			}
		}
	}
	public sealed class ResignationPower : CustomPowerModel
	{
		private readonly HashSet<CardModel> _processingCards = new HashSet<CardModel>();

		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)2;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/resignation_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/resignation_power.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
		{
			if (_processingCards.Contains(card))
			{
				_processingCards.Remove(card);
			}
			else if (((int)card.Type == 1 || (int)card.Type == 2 || (int)card.Type == 3) && !card.Keywords.Contains((CardKeyword)1))
			{
				_processingCards.Add(card);
				Creature target = GetAutoTarget(card);
				card.ExhaustOnNextPlay = true;
				await CardCmd.AutoPlay(choiceContext, card, target, (AutoPlayType)1, false, false);
			}
		}

		private Creature? GetAutoTarget(CardModel card)
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Expected I4, but got Unknown
			if (((PowerModel)this).Owner == null)
			{
				return null;
			}
			ICombatState combatState = ((PowerModel)this).Owner.CombatState;
			if (combatState == null)
			{
				return null;
			}
			TargetType targetType = card.TargetType;
			if (1 == 0)
			{
			}
			Creature result;
			switch (targetType - 2)
			{
			case 0:
				result = combatState.HittableEnemies?.FirstOrDefault();
				break;
			case 4:
			{
				Player player = ((PowerModel)this).Owner.Player;
				object obj;
				if (player == null)
				{
					obj = null;
				}
				else
				{
					IRunState runState = player.RunState;
					if (runState == null)
					{
						obj = null;
					}
					else
					{
						RunRngSet rng = runState.Rng;
						if (rng == null)
						{
							obj = null;
						}
						else
						{
							Rng combatTargets = rng.CombatTargets;
							if (combatTargets == null)
							{
								obj = null;
							}
							else
							{
								IEnumerable<Creature> allies = combatState.Allies;
								obj = combatTargets.NextItem<Creature>((allies ?? Enumerable.Empty<Creature>()).Where((Creature c) => c != null && c.IsAlive && c.IsPlayer && c != ((PowerModel)this).Owner));
							}
						}
					}
				}
				result = (Creature)obj;
				break;
			}
			case 3:
				result = ((PowerModel)this).Owner;
				break;
			default:
				result = null;
				break;
			}
			if (1 == 0)
			{
			}
			return result;
		}
	}
	public sealed class RoyalLovePower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)2;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/royal_love_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/royal_love_power.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new StarsVar(1) };

		public override async Task AfterEnergySpent(CardModel card, int amount)
		{
			if (amount > 0)
			{
				Creature owner = ((PowerModel)this).Owner;
				if (((owner != null) ? owner.Player : null) != null && card != null && card.Owner == ((PowerModel)this).Owner.Player)
				{
					((PowerModel)this).Flash();
					await PlayerCmd.GainStars((decimal)amount, ((PowerModel)this).Owner.Player);
				}
			}
		}
	}
	public sealed class Sandpit1Power : CustomPowerModel
	{
		public const int KillThreshold = 20;

		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://images/powers/sandpit_power.png";

		public override string? CustomBigIconPath => "res://images/powers/sandpit_power.png";

		public override async Task AfterPowerAmountChanged(PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
		{
			if (power != this)
			{
				return;
			}
			Creature me = ((PowerModel)this).Owner;
			if (me == null || ((PowerModel)this).Amount < 20)
			{
				return;
			}
			ICombatState combatState = me.CombatState;
			if (((combatState != null) ? combatState.HittableEnemies : null) != null)
			{
				foreach (Creature enemy in me.CombatState.HittableEnemies.ToList())
				{
					await CreatureCmd.Kill(enemy, false);
				}
			}
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}
	public sealed class SpeedPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override bool AllowNegative => true;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/speed.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/speed.png";
	}
	public sealed class SprayPaintPower : CustomPowerModel
	{
		private const int CardsPerDiscount = 4;

		[SavedProperty]
		public int CardsPlayedThisTurn { get; set; }

		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)2;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/spray_paint.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/spray_paint.png";

		public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
		{
			Creature owner = ((PowerModel)this).Owner;
			if (player != ((owner != null) ? owner.Player : null))
			{
				return Task.CompletedTask;
			}
			CardsPlayedThisTurn = 0;
			return Task.CompletedTask;
		}

		public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			Creature owner = ((PowerModel)this).Owner;
			if (((owner != null) ? owner.Player : null) == null)
			{
				return Task.CompletedTask;
			}
			if (cardPlay.Card.Owner != ((PowerModel)this).Owner.Player)
			{
				return Task.CompletedTask;
			}
			if (!cardPlay.IsLastInSeries)
			{
				return Task.CompletedTask;
			}
			CardsPlayedThisTurn++;
			return Task.CompletedTask;
		}

		public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
		{
			modifiedCost = originalCost;
			Creature owner = ((PowerModel)this).Owner;
			if (((owner != null) ? owner.Player : null) == null)
			{
				return false;
			}
			if (card.Owner != ((PowerModel)this).Owner.Player)
			{
				return false;
			}
			if (CardsPlayedThisTurn != 3)
			{
				return false;
			}
			modifiedCost = ((originalCost > 1m) ? (originalCost - 1m) : 0m);
			return true;
		}
	}
	public sealed class SuisuiGoldPower : CustomPowerModel
	{
		private const decimal AllyHealPercent = 0.5m;

		private const int CardsPerTrigger = 3;

		private bool _rescueUnavailable;

		private Creature? _allyBeforeHit;

		private decimal _allyHpBefore;

		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://images/powers/regen_power.png";

		public override string? CustomBigIconPath => "res://images/powers/regen_power.png";

		private bool RescueUnavailable => _rescueUnavailable || ((PowerModel)this).Owner == null || (decimal)((PowerModel)this).Owner.CurrentHp <= 1m;

		public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
		{
			Creature owner = ((PowerModel)this).Owner;
			if (((owner != null) ? owner.Player : null) == null)
			{
				return;
			}
			if (((card != null) ? card.Owner : null) == ((PowerModel)this).Owner.Player)
			{
				int exhausted = CombatManager.Instance.History.Entries.OfType<CardExhaustedEntry>().Count(delegate(CardExhaustedEntry e)
				{
					CardModel card2 = e.Card;
					return ((card2 != null) ? card2.Owner : null) == ((PowerModel)this).Owner.Player;
				});
				if (exhausted > 0 && exhausted % 3 == 0)
				{
					((PowerModel)this).Flash();
					await PowerCmd.Apply<RegenPower>(choiceContext, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, (CardModel)null, false);
				}
			}
		}

		public override Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature dealer, CardModel? cardSource)
		{
			if (IsRescuableAlly(target))
			{
				_allyBeforeHit = target;
				_allyHpBefore = target.CurrentHp;
			}
			return Task.CompletedTask;
		}

		public override bool ShouldDieLate(Creature creature)
		{
			if (!IsRescuableAlly(creature))
			{
				return true;
			}
			if (RescueUnavailable)
			{
				return true;
			}
			return false;
		}

		public override async Task AfterPreventingDeath(Creature creature)
		{
			if (((PowerModel)this).Owner != null && creature != null && IsRescuableAlly(creature))
			{
				decimal blocked = default(decimal);
				if (_allyBeforeHit == creature && _allyHpBefore > (decimal)creature.CurrentHp)
				{
					blocked = _allyHpBefore - (decimal)creature.CurrentHp;
				}
				_allyBeforeHit = null;
				_allyHpBefore = default(decimal);
				decimal pay2 = Math.Max(1m, Math.Floor((decimal)((PowerModel)this).Owner.CurrentHp * 0.5m));
				decimal affordable = Math.Max(0m, (decimal)((PowerModel)this).Owner.CurrentHp - 1m);
				pay2 = Math.Min(pay2, affordable);
				((PowerModel)this).Flash();
				decimal totalHeal = blocked + pay2;
				if (totalHeal > 0m)
				{
					await CreatureCmd.Heal(creature, totalHeal, true);
				}
				if (pay2 > 0m)
				{
					((PowerModel)this).Owner.LoseHpInternal(pay2, (ValueProp)6);
				}
				if ((decimal)((PowerModel)this).Owner.CurrentHp <= 1m)
				{
					_rescueUnavailable = true;
				}
			}
		}

		private bool IsRescuableAlly(Creature? creature)
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			if (((PowerModel)this).Owner == null || creature == null)
			{
				return false;
			}
			if (creature == ((PowerModel)this).Owner)
			{
				return false;
			}
			if (creature.Side != ((PowerModel)this).Owner.Side)
			{
				return false;
			}
			if (creature.Player == null)
			{
				return false;
			}
			return true;
		}
	}
	public sealed class SymbiosisPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/symbiosis_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/symbiosis_power.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override async Task AfterSummon(PlayerChoiceContext choiceContext, Player summoner, decimal amount)
		{
			if (((summoner != null) ? summoner.Creature : null) == ((PowerModel)this).Owner && !(amount <= 0m) && ((PowerModel)this).Amount > 0)
			{
				await CreatureCmd.GainBlock(((PowerModel)this).Owner, amount * (decimal)((PowerModel)this).Amount, (ValueProp)4, (CardPlay)null, false);
				await <>n__0(choiceContext, summoner, amount);
			}
		}

		[CompilerGenerated]
		[DebuggerHidden]
		private Task <>n__0(PlayerChoiceContext choiceContext, Player summoner, decimal amount)
		{
			return ((AbstractModel)this).AfterSummon(choiceContext, summoner, amount);
		}
	}
	public class TaijiDexterityPower : TemporaryDexterityPower, ICustomPower, ICustomModel
	{
		public override AbstractModel OriginModel => (AbstractModel)(object)ModelDb.Relic<Taiji>();

		public string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/dexterity_power.tres";

		public string? CustomBigIconPath => "res://images/powers/dexterity_power.png";
	}
	public class TaijiStrengthPower : TemporaryStrengthPower, ICustomPower, ICustomModel
	{
		public override AbstractModel OriginModel => (AbstractModel)(object)ModelDb.Relic<Taiji>();

		public string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/strength_power.tres";

		public string? CustomBigIconPath => "res://images/powers/strength_power.png";
	}
	public sealed class TimeOffExtraTurnPower : CustomPowerModel
	{
		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://images/powers/strength_power.png";

		public override string? CustomBigIconPath => "res://images/powers/strength_power.png";

		protected override bool IsVisibleInternal => false;

		public override bool ShouldTakeExtraTurn(Player player)
		{
			Creature owner = ((PowerModel)this).Owner;
			if (((owner != null) ? owner.Player : null) == null)
			{
				return false;
			}
			return ((PowerModel)this).Owner.Player == player;
		}

		public override async Task AfterTakingExtraTurn(Player player)
		{
			Creature owner = ((PowerModel)this).Owner;
			if (((owner != null) ? owner.Player : null) == player)
			{
				await PowerCmd.Remove((PowerModel)(object)this);
			}
		}
	}
	public sealed class VirtualParticlePower : CustomPowerModel
	{
		private bool _consumedThisCard;

		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/virtual_particle.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/virtual_particle.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override Task BeforeCardPlayed(CardPlay cardPlay)
		{
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Invalid comparison between Unknown and I4
			if (((PowerModel)this).Owner == null || !((PowerModel)this).Owner.IsAlive)
			{
				return Task.CompletedTask;
			}
			Player owner = cardPlay.Card.Owner;
			if (((owner != null) ? owner.Creature : null) != ((PowerModel)this).Owner)
			{
				return Task.CompletedTask;
			}
			if ((int)cardPlay.Card.Type != 1)
			{
				return Task.CompletedTask;
			}
			if (((PowerModel)this).Amount <= 0)
			{
				return Task.CompletedTask;
			}
			if (((PowerModel)this).Owner.GetPower<EntropyIllusionPower>() == null)
			{
				return Task.CompletedTask;
			}
			_consumedThisCard = true;
			((PowerModel)this).SetAmount(0, false);
			return Task.CompletedTask;
		}

		public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Invalid comparison between Unknown and I4
			if (dealer == ((PowerModel)this).Owner && _consumedThisCard && cardSource != null && (int)cardSource.Type == 1)
			{
				return 1.5m;
			}
			return 1m;
		}

		public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			if (_consumedThisCard)
			{
				_consumedThisCard = false;
				Creature owner = ((PowerModel)this).Owner;
				if (((owner != null) ? owner.Player : null) != null)
				{
					await PlayerCmd.GainEnergy(1m, ((PowerModel)this).Owner.Player);
				}
			}
		}
	}
	public sealed class WarpPower : CustomPowerModel
	{
		private const int CardsPerEnergy = 10;

		[SavedProperty]
		public int CardsPlayed { get; set; }

		public override PowerType Type => (PowerType)1;

		public override PowerStackType StackType => (PowerStackType)2;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/warp.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/warp.png";

		public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			Creature owner = ((PowerModel)this).Owner;
			if (((owner != null) ? owner.Player : null) != null && cardPlay.Card.Owner == ((PowerModel)this).Owner.Player && cardPlay.IsLastInSeries)
			{
				CardsPlayed++;
				if (CardsPlayed % 10 == 0)
				{
					((PowerModel)this).Flash();
					await PlayerCmd.GainEnergy(1m, ((PowerModel)this).Owner.Player);
				}
			}
		}
	}
	public class XieXianPower : CustomPowerModel
	{
		private bool _isProcessing;

		public override PowerType Type => (PowerType)2;

		public override PowerStackType StackType => (PowerStackType)1;

		public override string? CustomPackedIconPath => "res://wuwancients/images/powers/xiexian_power.png";

		public override string? CustomBigIconPath => "res://wuwancients/images/powers/xiexian_power.png";

		public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			if (((PowerModel)this).Owner == target && ((PowerModel)this).Amount > 0 && !_isProcessing && cardSource != null)
			{
				_isProcessing = true;
				try
				{
					await DamageCmd.Attack((decimal)((PowerModel)this).Amount).FromCard(cardSource, (CardPlay)null).Targeting(((PowerModel)this).Owner)
						.Execute(choiceContext);
					((PowerModel)this).Flash();
				}
				finally
				{
					_isProcessing = false;
				}
			}
		}
	}
}
namespace wuwancients.Potions
{
	[Pool(typeof(EventPotionPool))]
	public sealed class ChongZhouMalaiDouFu : CustomPotionModel
	{
		public override PotionRarity Rarity => (PotionRarity)4;

		public override PotionUsage Usage => (PotionUsage)2;

		public override TargetType TargetType => (TargetType)1;

		public override string? CustomPackedImagePath => "res://wuwancients/images/potions/chong_zhou_malai_dou_fu.png";

		public override string? CustomPackedOutlinePath => "res://wuwancients/images/potions/chong_zhou_malai_dou_fu.png";

		protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
		{
			if (((PotionModel)this).Owner != null)
			{
				await PlayerCmd.GainGold(250m, ((PotionModel)this).Owner, false);
			}
		}
	}
	[Pool(typeof(EventPotionPool))]
	public sealed class DazzlingSweetness : CustomPotionModel
	{
		public override PotionRarity Rarity => (PotionRarity)4;

		public override PotionUsage Usage => (PotionUsage)2;

		public override TargetType TargetType => (TargetType)1;

		public override string? CustomPackedImagePath => "res://wuwancients/images/potions/dazzling_sweetness.png";

		public override string? CustomPackedOutlinePath => "res://wuwancients/images/potions/dazzling_sweetness.png";

		protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
		{
			Player owner = ((PotionModel)this).Owner;
			if (((owner != null) ? owner.RunState : null) == null)
			{
				return;
			}
			foreach (Player player in ((IPlayerCollection)((PotionModel)this).Owner.RunState).Players)
			{
				if (((player != null) ? player.Creature : null) != null)
				{
					await CreatureCmd.Heal(player.Creature, 28m, true);
				}
			}
		}
	}
	[Pool(typeof(EventPotionPool))]
	public sealed class HeroKingsBlood : CustomPotionModel
	{
		public override PotionRarity Rarity => (PotionRarity)4;

		public override PotionUsage Usage => (PotionUsage)1;

		public override TargetType TargetType => (TargetType)1;

		public override string? CustomPackedImagePath => "res://wuwancients/images/potions/hero_kings_blood.png";

		public override string? CustomPackedOutlinePath => "res://wuwancients/images/potions/hero_kings_blood.png";

		protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
		{
			Player owner = ((PotionModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				await CreatureCmd.Heal(((PotionModel)this).Owner.Creature, 20m, true);
				await CardPileCmd.Draw(choiceContext, 3m, ((PotionModel)this).Owner, false);
				await PlayerCmd.GainEnergy(2m, ((PotionModel)this).Owner);
				await PowerCmd.Apply<HeroKingsBloodPower>(choiceContext, ((PotionModel)this).Owner.Creature, 2m, ((PotionModel)this).Owner.Creature, (CardModel)null, false);
			}
		}
	}
}
namespace wuwancients.Patches
{
	[HarmonyPatch(typeof(ActModel), "GenerateRooms")]
	public static class DisableVanillaAncients
	{
		[HarmonyPostfix]
		[HarmonyPriority(0)]
		public static void ForceCustom(ActModel __instance, RoomSet ____rooms)
		{
			ActModel __instance2 = __instance;
			if (!WuwancientsConfig.禁用原版先古 || ____rooms == null || !____rooms.HasAncient || ____rooms.Ancient is CustomAncientModel)
			{
				return;
			}
			List<CustomAncientModel> list = CustomContentDictionary.CustomAncients.Where((CustomAncientModel a) => a.IsValidForAct(__instance2)).ToList();
			if (list.Count == 0)
			{
				return;
			}
			AncientEventModel rngChosen = ____rooms.Ancient;
			CustomAncientModel val = ((IEnumerable<CustomAncientModel>)list).FirstOrDefault((Func<CustomAncientModel, bool>)((CustomAncientModel a) => a.ShouldForceSpawn(__instance2, rngChosen)));
			if (val == null)
			{
				RunState obj = RunManager.Instance.DebugOnlyGetState();
				object obj2;
				if (obj == null)
				{
					obj2 = null;
				}
				else
				{
					RunRngSet rng = obj.Rng;
					obj2 = ((rng != null) ? rng.UpFront : null);
				}
				Rng val2 = (Rng)obj2;
				val = ((val2 != null) ? list[val2.NextInt(list.Count)] : list[0]);
			}
			____rooms.Ancient = (AncientEventModel)(object)val;
		}
	}
	[HarmonyPatch(typeof(NAncientDialogueLine), "_Ready")]
	public static class GalbrenaChimeraIcon
	{
		private const string AltIconPath = "res://wuwancients/images/icons/galbrena_alt.png";

		private const string AltIconOutlinePath = "res://wuwancients/images/icons/galbrena_alt_outline.png";

		private static readonly string[] Markers = new string[3] { "[奇美拉]", "[Chimera]", "[키메라]" };

		[HarmonyPostfix]
		public static void SwapSpeakerIcon(NAncientDialogueLine __instance, AncientEventModel ____ancient, AncientDialogueLine ____line)
		{
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Invalid comparison between Unknown and I4
			if (!(____ancient is Galbrena) || ____line == null || ____line.LineText == null || (int)____line.Speaker != 1)
			{
				return;
			}
			string raw = ____line.LineText.GetRawText() ?? string.Empty;
			string text = Markers.FirstOrDefault((string m) => raw.StartsWith(m, StringComparison.Ordinal));
			if (text == null)
			{
				return;
			}
			Texture2D val = ResourceLoader.Load<Texture2D>("res://wuwancients/images/icons/galbrena_alt.png", (string)null, (CacheMode)1);
			if (val != null)
			{
				Control node = ((Node)__instance).GetNode<Control>(NodePath.op_Implicit("%AncientIcon"));
				((Node)node).GetNode<TextureRect>(NodePath.op_Implicit("Icon")).Texture = val;
				Texture2D val2 = ResourceLoader.Load<Texture2D>("res://wuwancients/images/icons/galbrena_alt_outline.png", (string)null, (CacheMode)1);
				if (val2 != null)
				{
					((Node)node).GetNode<TextureRect>(NodePath.op_Implicit("Icon/Outline")).Texture = val2;
				}
			}
			MegaRichTextLabel node2 = ((Node)__instance).GetNode<MegaRichTextLabel>(NodePath.op_Implicit("%Text"));
			string text2 = node2.Text ?? string.Empty;
			if (text2.StartsWith(text, StringComparison.Ordinal))
			{
				node2.Text = text2.Substring(text.Length).TrimStart();
			}
		}
	}
	[HarmonyPatch(typeof(ModelDb), "get_AllSharedEvents")]
	public static class RegisterCustomEvent
	{
		[HarmonyPostfix]
		public static void AddCustomEvents(ref IEnumerable<EventModel> __result)
		{
			List<EventModel> list = __result.ToList();
			AddIfMode<YiZhanZuYiEvent>(list, WuwancientsConfig.江湖梦出现方式);
			AddIfMode<MottaliAngerEvent>(list, WuwancientsConfig.莫塔里之忿出现方式);
			AddIfMode<CamelliaGift>(list, WuwancientsConfig.花女的礼物出现方式);
			AddIfMode<ItSmellsGoodHereEvent>(list, WuwancientsConfig.这里好香出现方式);
			AddIfMode<HappyBirthday>(list, WuwancientsConfig.生日快乐出现方式);
			AddIfMode<DataStreamEvent>(list, WuwancientsConfig.数据流出现方式);
			AddIfMode<GoodBestieEvent>(list, WuwancientsConfig.好闺蜜出现方式);
			AddIfMode<RhythmPoemEvent>(list, WuwancientsConfig.律绘之诗出现方式);
			AddIfMode<Drifter>(list, WuwancientsConfig.漂泊出现方式);
			AddIfMode<DaoguanMeng>(list, WuwancientsConfig.盗观梦出现方式);
			AddIfMode<Hunting>(list, WuwancientsConfig.狩猎出现方式);
			AddIfMode<WeiXun>(list, WuwancientsConfig.微醺出现方式);
			AddIfMode<FadingPhoto>(list, WuwancientsConfig.照片中的我们出现方式);
			AddIfMode<GalbrenaEvent>(list, WuwancientsConfig.别回头出现方式);
			AddIfMode<CarlottaEvent>(list, WuwancientsConfig.一刻闲暇出现方式);
			AddIfMode<StarChartEvent>(list, WuwancientsConfig.偏移的星图出现方式);
			__result = list;
		}

		private static void AddIfMode<T>(List<EventModel> list, EventAppearMode mode) where T : EventModel
		{
			if (mode != 0)
			{
				T ev = ModelDb.Event<T>();
				if (ev != null && !list.Any((EventModel e) => ((AbstractModel)e).Id == ((AbstractModel)(object)ev).Id))
				{
					list.Add((EventModel)(object)ev);
				}
			}
		}
	}
	[HarmonyPatch(typeof(NRelicCollectionCategory), "LoadRelics")]
	public static class StarterRelicCollectionPatch
	{
		private static readonly HashSet<string> CustomEntryNames = new HashSet<string> { "BRIGHT_BLOOD", "FROZEN_CORE", "HEAVENLY_MANDATE", "LONG_SNAKE_NECKLACE", "LOST_PHYLACTERY" };

		private static MethodInfo? _createForSubcategory;

		private static FieldInfo? _subCategoriesField;

		private static FieldInfo? _spacerField;

		private static FieldInfo? _headerLabelField;

		private static FieldInfo? _relicsContainerField;

		private static MethodInfo? _loadSubcategory;

		[HarmonyPostfix]
		public static void AddThirdRow(RelicRarity relicRarity, NRelicCollection collection, HashSet<RelicModel> seenRelics, HashSet<RelicModel> allUnlockedRelics, NRelicCollectionCategory __instance)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Invalid comparison between Unknown and I4
			if ((int)relicRarity != 1)
			{
				return;
			}
			List<RelicModel> list = ModelDb.AllRelics.Where((RelicModel r) => (int)r.Rarity == 1 && CustomEntryNames.Contains(((AbstractModel)r).Id.Entry)).ToList();
			if (list.Count == 0)
			{
				return;
			}
			if (_createForSubcategory == null)
			{
				Type typeFromHandle = typeof(NRelicCollectionCategory);
				_createForSubcategory = AccessTools.DeclaredMethod(typeFromHandle, "CreateForSubcategory", (Type[])null, (Type[])null);
				_subCategoriesField = AccessTools.DeclaredField(typeFromHandle, "_subCategories");
				_spacerField = AccessTools.DeclaredField(typeFromHandle, "_spacer");
				_headerLabelField = AccessTools.DeclaredField(typeFromHandle, "_headerLabel");
				_relicsContainerField = AccessTools.DeclaredField(typeFromHandle, "_relicsContainer");
				_loadSubcategory = AccessTools.DeclaredMethod(typeFromHandle, "LoadSubcategory", new Type[5]
				{
					typeof(NRelicCollection),
					typeof(LocString),
					typeof(IEnumerable<RelicModel>),
					typeof(HashSet<RelicModel>),
					typeof(HashSet<RelicModel>)
				}, (Type[])null);
			}
			object? value = _relicsContainerField.GetValue(__instance);
			GridContainer val = (GridContainer)((value is GridContainer) ? value : null);
			if (val != null)
			{
				foreach (Node child in ((Node)val).GetChildren(false))
				{
					if (child is NRelicCollectionEntry)
					{
						child.QueueFree();
					}
				}
			}
			List<NRelicCollectionCategory> list2 = ((IEnumerable)((Node)__instance).GetChildren(false)).OfType<NRelicCollectionCategory>().Skip(2).ToList();
			foreach (NRelicCollectionCategory item in list2)
			{
				((Node)item).QueueFree();
			}
			IList list3 = _subCategoriesField.GetValue(__instance) as IList;
			while (list3 != null && list3.Count > 2)
			{
				list3.RemoveAt(list3.Count - 1);
			}
			object? obj = _createForSubcategory.Invoke(__instance, null);
			NRelicCollectionCategory val2 = (NRelicCollectionCategory)((obj is NRelicCollectionCategory) ? obj : null);
			if (val2 != null)
			{
				list3?.Add(val2);
				object? value2 = _headerLabelField.GetValue(__instance);
				Node val3 = (Node)((value2 is Node) ? value2 : null);
				int num = ((val3 != null) ? val3.GetIndex(false) : 0) + 3;
				((Node)__instance).AddChild((Node)(object)val2, false, (InternalMode)0);
				((Node)__instance).MoveChild((Node)(object)val2, num);
				object? value3 = _spacerField.GetValue(val2);
				Control val4 = (Control)((value3 is Control) ? value3 : null);
				if (val4 != null)
				{
					((CanvasItem)val4).Visible = false;
				}
				object? value4 = _headerLabelField.GetValue(val2);
				Control val5 = (Control)((value4 is Control) ? value4 : null);
				if (val5 != null)
				{
					((CanvasItem)val5).Visible = false;
				}
				_loadSubcategory.Invoke(val2, new object[5] { collection, null, list, seenRelics, allUnlockedRelics });
			}
		}
	}
	[HarmonyPatch(typeof(Hook), "AfterRoomEntered")]
	public static class WuwuDeliveryPatch
	{
		[HarmonyPostfix]
		public static void Postfix(IRunState runState)
		{
			if (((runState != null) ? ((IPlayerCollection)runState).Players : null) == null)
			{
				return;
			}
			foreach (Player item in ((IPlayerCollection)runState).Players.ToList())
			{
				TaskHelper.RunSafely(WuwuDelivery.Tick(item));
			}
		}
	}
}
namespace wuwancients.Keywords
{
	public class WuwancientsKeywords
	{
		[CustomEnum("HEAVY_STRIKE")]
		[KeywordProperties(/*Could not decode attribute arguments.*/)]
		public static CardKeyword HeavyStrike;
	}
}
namespace wuwancients.Events
{
	public sealed class CamelliaGift : CustomEventModel
	{
		public override ActModel[] Acts => Array.Empty<ActModel>();

		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/camellia_gift_portrait.png";

		private static bool PlayerHasBloom(Player player)
		{
			bool? obj;
			if (player == null)
			{
				obj = null;
			}
			else
			{
				CardPile deck = player.Deck;
				obj = ((deck == null) ? null : deck.Cards?.Any((CardModel c) => c is Bloom));
			}
			bool? flag = obj;
			return flag.GetValueOrDefault();
		}

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.花女的礼物出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (((runState != null) ? ((IPlayerCollection)runState).Players : null) == null || ((IPlayerCollection)runState).Players.Count == 0)
			{
				return false;
			}
			return ((IPlayerCollection)runState).Players.All((Player p) => PlayerHasBloom(p));
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[3]
			{
				((CustomEventModel)this).Option((Func<Task>)Pluck, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Nurture, HoverTipFactory.FromCardWithCardHoverTips<OneDayFlower>(false), "INITIAL"),
				((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
			});
		}

		private async Task Pluck()
		{
			Player player = ((EventModel)this).Owner;
			if (((player != null) ? player.Creature : null) == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
				return;
			}
			try
			{
				await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), player.Creature, 14m, (ValueProp)4, (CardModel)null, (CardPlay)null);
				await CreatureCmd.GainMaxHp(player.Creature, 10m);
				List<CardModel> blooms = player.Deck.Cards.Where((CardModel c) => c is Bloom).ToList();
				foreach (CardModel bloom in blooms)
				{
					await CardPileCmd.RemoveFromDeck(bloom, true);
				}
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PLUCK_RESULT"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
			}
		}

		private async Task Nurture()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
				return;
			}
			try
			{
				List<CardModel> blooms = player.Deck.Cards.Where((CardModel c) => c is Bloom).ToList();
				foreach (CardModel bloom in blooms)
				{
					await CardPileCmd.RemoveFromDeck(bloom, true);
				}
				OneDayFlower oneDayFlower = ((ICardScope)player.RunState).CreateCard<OneDayFlower>(player);
				CardPileCmd.Add((CardModel)(object)oneDayFlower, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("NURTURE_RESULT"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
			}
		}

		private Task Leave()
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
			return Task.CompletedTask;
		}

		public CamelliaGift()
			: base(true)
		{
		}
	}
	public sealed class CarlottaEvent : CustomEventModel
	{
		private const int HealMin = 12;

		private const int HealMax = 20;

		private const int HealMinLink = 18;

		private const int HealMaxLink = 30;

		private const int GoldMin = 60;

		private const int GoldMax = 100;

		private const int GoldMinLink = 90;

		private const int GoldMaxLink = 150;

		private const int CostMin = 80;

		private const int CostMax = 110;

		private const int CostMinLink = 50;

		private const int CostMaxLink = 80;

		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/carlotta_event.png";

		private bool HasMottaliFavour
		{
			get
			{
				Player owner = ((EventModel)this).Owner;
				return owner != null && (owner.Relics?.Any((RelicModel r) => (r is MottaliBankCard || r is MottaliGift) ? true : false)).GetValueOrDefault();
			}
		}

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
		{
			(DynamicVar)new HealVar(0m),
			(DynamicVar)new GoldVar(0),
			(DynamicVar)new GoldVar("Cost", 0)
		};

		public override void CalculateVars()
		{
			bool hasMottaliFavour = HasMottaliFavour;
			((DynamicVar)((EventModel)this).DynamicVars.Heal).BaseValue = ((EventModel)this).Rng.NextInt(hasMottaliFavour ? 18 : 12, (hasMottaliFavour ? 30 : 20) + 1);
			((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue = ((EventModel)this).Rng.NextInt(hasMottaliFavour ? 90 : 60, (hasMottaliFavour ? 150 : 100) + 1);
			((EventModel)this).DynamicVars["Cost"].BaseValue = ((EventModel)this).Rng.NextInt(hasMottaliFavour ? 50 : 80, (hasMottaliFavour ? 80 : 110) + 1);
		}

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.一刻闲暇出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (((runState != null) ? ((IPlayerCollection)runState).Players : null) == null || ((IPlayerCollection)runState).Players.Count == 0)
			{
				return false;
			}
			return runState.CurrentActIndex == 2;
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[3]
			{
				((CustomEventModel)this).Option((Func<Task>)Tea, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Dessert, "INITIAL", Array.Empty<IHoverTip>()),
				BusinessOption()
			});
		}

		private EventOption BusinessOption()
		{
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Expected O, but got Unknown
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Expected O, but got Unknown
			//IL_007b: Expected O, but got Unknown
			Player owner = ((EventModel)this).Owner;
			if (((owner != null) ? owner.Gold : 0) < ((EventModel)this).DynamicVars["Cost"].IntValue)
			{
				string text = ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.BUSINESS";
				return ((CustomEventModel)this).Option((Func<Task>)null, new LocString("events", text + ".title"), new LocString("events", text + ".description"), Array.Empty<IHoverTip>());
			}
			return ((CustomEventModel)this).Option((Func<Task>)Business, "INITIAL", (IHoverTip[])(object)new IHoverTip[1] { (IHoverTip)(object)new HoverTip(new LocString("events", ((AbstractModel)this).Id.Entry + ".tips.RANDOM_RELIC"), (Texture2D)null) });
		}

		private async Task Tea()
		{
			Player player = ((EventModel)this).Owner;
			if (((player != null) ? player.Creature : null) != null)
			{
				try
				{
					await CreatureCmd.Heal(player.Creature, ((DynamicVar)((EventModel)this).DynamicVars.Heal).BaseValue, true);
				}
				catch
				{
				}
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("TEA_RESULT"));
		}

		private async Task Dessert()
		{
			Player player = ((EventModel)this).Owner;
			if (player != null)
			{
				try
				{
					await PlayerCmd.GainGold((decimal)((DynamicVar)((EventModel)this).DynamicVars.Gold).IntValue, player, false);
				}
				catch
				{
				}
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DESSERT_RESULT"));
		}

		private async Task Business()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BUSINESS_RESULT"));
				return;
			}
			int cost = ((EventModel)this).DynamicVars["Cost"].IntValue;
			if (player.Gold < cost)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BUSINESS_RESULT"));
				return;
			}
			try
			{
				await PlayerCmd.LoseGold((decimal)cost, player, (GoldLossType)1);
				await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(player).ToMutable(), player, -1);
			}
			catch
			{
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BUSINESS_RESULT"));
		}

		public CarlottaEvent()
			: base(true)
		{
		}
	}
	public sealed class CaveFragrance : CustomEventModel
	{
		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/cave_fragrance.png";

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.山洞中的香气出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			return true;
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Eat, HoverTipFactory.FromCardWithCardHoverTips<Fuxie>(false), "INITIAL"),
				((CustomEventModel)this).Option((Func<Task>)Pack, HoverTipFactory.FromRelic<NuoNuoChaoFan>(), "INITIAL")
			});
		}

		private async Task Eat()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PACK_DONE"));
				return;
			}
			Fuxie card = ((ICardScope)player.RunState).CreateCard<Fuxie>(player);
			CardPileCmd.Add((CardModel)(object)card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
			await PlayerCmd.GainGold(300m, player, false);
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("EAT_DONE"));
		}

		private async Task Pack()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("EAT_DONE"));
				return;
			}
			RelicModel relic = ((RelicModel)ModelDb.Relic<NuoNuoChaoFan>()).ToMutable();
			await RelicCmd.Obtain(relic, player, -1);
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PACK_DONE"));
		}

		public CaveFragrance()
			: base(true)
		{
		}
	}
	public sealed class DaoguanMeng : CustomEventModel
	{
		private const int MinGoldToAppear = 100;

		private const int PriceTaiji = 50;

		private const int PriceBurial = 80;

		private const int PriceBoth = 150;

		public override ActModel[] Acts => Array.Empty<ActModel>();

		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/daoguanmeng.png";

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.盗观梦出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (((runState != null) ? ((IPlayerCollection)runState).Players : null) == null || ((IPlayerCollection)runState).Players.Count == 0)
			{
				return false;
			}
			return ((IPlayerCollection)runState).Players.All((Player p) => p != null && p.Gold >= 100);
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			Player owner = ((EventModel)this).Owner;
			int gold = ((owner != null) ? owner.Gold : 0);
			return (IReadOnlyList<EventOption>)(object)new EventOption[4]
			{
				Buyable(gold, 50, "TAIJI", Taiji, HoverTipFactory.FromRelic<Taiji>()),
				Buyable(gold, 80, "BURIAL", Burial, HoverTipFactory.FromCardWithCardHoverTips<MassGrave>(false)),
				Buyable(gold, 150, "BOTH", Both, HoverTipFactory.FromRelic<Taiji>().Concat(HoverTipFactory.FromCardWithCardHoverTips<MassGrave>(false))),
				((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
			};
		}

		private EventOption Buyable(int gold, int price, string optionKey, Func<Task> action, IEnumerable<IHoverTip> tips)
		{
			return (EventOption)((gold >= price) ? ((object)((CustomEventModel)this).Option(action, tips, "INITIAL")) : ((object)Locked(optionKey)));
		}

		private EventOption Locked(string optionKey)
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Expected O, but got Unknown
			//IL_004e: Expected O, but got Unknown
			string text = ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options." + optionKey;
			return ((CustomEventModel)this).Option((Func<Task>)null, new LocString("events", text + ".title"), new LocString("events", text + ".description"), Array.Empty<IHoverTip>());
		}

		private async Task Taiji()
		{
			if (!TryPay(50))
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE_RESULT"));
				return;
			}
			try
			{
				await GrantRelic<Taiji>();
			}
			catch
			{
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("TAIJI_RESULT"));
		}

		private async Task Burial()
		{
			if (!TryPay(80))
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE_RESULT"));
				return;
			}
			try
			{
				await GrantCard<MassGrave>();
			}
			catch
			{
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BURIAL_RESULT"));
		}

		private async Task Both()
		{
			if (!TryPay(150))
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE_RESULT"));
				return;
			}
			try
			{
				await GrantCard<MassGrave>();
				await GrantRelic<Taiji>();
			}
			catch
			{
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BOTH_RESULT"));
		}

		private Task Leave()
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE_RESULT"));
			return Task.CompletedTask;
		}

		private bool TryPay(int price)
		{
			Player owner = ((EventModel)this).Owner;
			if (owner == null || owner.Gold < price)
			{
				return false;
			}
			PlayerCmd.LoseGold((decimal)price, owner, (GoldLossType)1);
			return true;
		}

		private async Task GrantRelic<T>() where T : RelicModel
		{
			Player player = ((EventModel)this).Owner;
			if (player != null)
			{
				await RelicCmd.Obtain(((RelicModel)ModelDb.Relic<T>()).ToMutable(), player, -1);
			}
		}

		private async Task GrantCard<T>() where T : CardModel
		{
			Player player = ((EventModel)this).Owner;
			if (player != null)
			{
				CardModel card = (CardModel)(object)((ICardScope)player.RunState).CreateCard<T>(player);
				await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
			}
		}

		public DaoguanMeng()
			: base(true)
		{
		}
	}
	public sealed class DataStreamEvent : CustomEventModel
	{
		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/data_stream.png";

		private bool HasCriticalProtocol
		{
			get
			{
				Player owner = ((EventModel)this).Owner;
				int result;
				if (owner == null)
				{
					result = 0;
				}
				else
				{
					CardPile deck = owner.Deck;
					result = (((deck == null) ? null : deck.Cards?.Any((CardModel c) => c is CriticalProtocol)).GetValueOrDefault() ? 1 : 0);
				}
				return (byte)result != 0;
			}
		}

		public override LocString InitialDescription
		{
			get
			{
				if (RunHasVisitedStarChart())
				{
					return ((CustomEventModel)this).PageDescription("AFTER_STAR_CHART");
				}
				return ((EventModel)this).InitialDescription;
			}
		}

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.数据流出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			int currentActIndex = runState.CurrentActIndex;
			if ((uint)currentActIndex > 1u)
			{
				return false;
			}
			return true;
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			IEnumerable<IHoverTip> enumerable = (HasCriticalProtocol ? HoverTipFactory.FromCardWithCardHoverTips<MassEnergyEquivalence>(false) : HoverTipFactory.FromCardWithCardHoverTips<CollectData>(false));
			return (IReadOnlyList<EventOption>)(object)new EventOption[3]
			{
				((CustomEventModel)this).Option((Func<Task>)Accelerate, enumerable, "INITIAL"),
				((CustomEventModel)this).Option((Func<Task>)Wake, HoverTipFactory.FromRelic<Decode>(), "INITIAL"),
				((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
			};
		}

		private bool RunHasVisitedStarChart()
		{
			Player owner = ((EventModel)this).Owner;
			IRunState val = ((owner != null) ? owner.RunState : null);
			if (val == null)
			{
				return false;
			}
			ModelId target = ((AbstractModel)ModelDb.Event<StarChartEvent>()).Id;
			return val.MapPointHistory.Any((IReadOnlyList<MapPointHistoryEntry> act) => act.Any((MapPointHistoryEntry point) => point.Rooms.Any(delegate(MapPointRoomHistoryEntry room)
			{
				ModelId modelId = room.ModelId;
				return modelId != null && modelId.Equals(target);
			})));
		}

		private async Task Accelerate()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
				return;
			}
			try
			{
				if (HasCriticalProtocol)
				{
					MassEnergyEquivalence linked = ((ICardScope)player.RunState).CreateCard<MassEnergyEquivalence>(player);
					CardPileCmd.Add((CardModel)(object)linked, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
				}
				else
				{
					CollectData card = ((ICardScope)player.RunState).CreateCard<CollectData>(player);
					CardPileCmd.Add((CardModel)(object)card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
				}
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ACCELERATE_DONE"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
			}
			await Task.CompletedTask;
		}

		private async Task Wake()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
				return;
			}
			try
			{
				RelicModel relic = ((RelicModel)ModelDb.Relic<Decode>()).ToMutable();
				await RelicCmd.Obtain(relic, player, -1);
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("WAKE_DONE"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
			}
		}

		private Task Leave()
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
			return Task.CompletedTask;
		}

		public DataStreamEvent()
			: base(true)
		{
		}
	}
	public sealed class Drifter : CustomEventModel
	{
		private const int ThresholdPercent = 15;

		private const int RelicCount = 2;

		private const string ShorekeeperAncientEntry = "WUWANCIENTS-SHOREKEEPER";

		public override ActModel[] Acts => Array.Empty<ActModel>();

		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/drifter.png";

		public override LocString InitialDescription
		{
			get
			{
				LocString initialDescription = ((EventModel)this).InitialDescription;
				initialDescription.Add("RelicClause", RelicClause());
				return initialDescription;
			}
		}

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.漂泊出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (((runState != null) ? ((IPlayerCollection)runState).Players : null) == null || ((IPlayerCollection)runState).Players.Count == 0)
			{
				return false;
			}
			if (!ActOneAncientIsShorekeeper(runState))
			{
				return false;
			}
			return ((IPlayerCollection)runState).Players.Any((Player p) => IsBadlyHurt((p != null) ? p.Creature : null));
		}

		private static bool ActOneAncientIsShorekeeper(IRunState runState)
		{
			if (runState.MapPointHistory == null || runState.MapPointHistory.Count == 0)
			{
				return false;
			}
			foreach (MapPointHistoryEntry item in runState.MapPointHistory[0])
			{
				foreach (MapPointRoomHistoryEntry room in item.Rooms)
				{
					ModelId modelId = room.ModelId;
					if (((modelId != null) ? modelId.Entry : null) == "WUWANCIENTS-SHOREKEEPER")
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool IsBadlyHurt(Creature? creature)
		{
			if (creature == null || (decimal)creature.MaxHp <= 0m)
			{
				return false;
			}
			return (decimal)creature.CurrentHp * 100m < (decimal)(creature.MaxHp * 15);
		}

		private string RelicClause()
		{
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			Player owner = ((EventModel)this).Owner;
			RelicModel val = ((owner == null) ? null : owner.Relics?.FirstOrDefault((Func<RelicModel, bool>)((RelicModel r) => WhiteRibbon.IsShorekeeperProvided(r))));
			if (val == null)
			{
				return new LocString("events", "WUWANCIENTS-DRIFTER.token").GetFormattedText();
			}
			string formattedText = new LocString("relics", ((AbstractModel)val).Id.Entry + ".title").GetFormattedText();
			return "[gold]" + formattedText + "[/gold]";
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)AcceptHelp, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)RefuseHelp, "INITIAL", Array.Empty<IHoverTip>())
			});
		}

		private async Task AcceptHelp()
		{
			Player player = ((EventModel)this).Owner;
			if (((player != null) ? player.Creature : null) == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ACCEPT_RESULT"));
				return;
			}
			try
			{
				decimal missing = player.Creature.MaxHp - player.Creature.CurrentHp;
				if (missing > 0m)
				{
					await CreatureCmd.Heal(player.Creature, missing, true);
				}
			}
			catch
			{
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ACCEPT_RESULT"));
		}

		private async Task RefuseHelp()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("REFUSE_RESULT"));
				return;
			}
			try
			{
				for (int i = 0; i < 2; i++)
				{
					await GrantRandomRelic(player);
				}
			}
			catch
			{
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("REFUSE_RESULT"));
		}

		private static async Task GrantRandomRelic(Player player)
		{
			Player player2 = player;
			List<RelicModel> pool = ModelDb.AllRelics.Where((RelicModel r) => (int)r.Rarity == 2 || (int)r.Rarity == 3 || (int)r.Rarity == 4 || (int)r.Rarity == 5).Where(delegate(RelicModel r)
			{
				RelicModel r2 = r;
				return !player2.Relics.Any((RelicModel owned) => ((AbstractModel)owned).Id.Equals(((AbstractModel)r2).Id));
			}).ToList();
			if (pool.Count != 0)
			{
				RelicModel? obj = SyncedRng.Pick(SyncedRng.Niche(player2.RunState), pool);
				RelicModel pick = ((obj != null) ? obj.ToMutable() : null);
				if (pick != null)
				{
					await RelicCmd.Obtain(pick, player2, -1);
				}
			}
		}

		public Drifter()
			: base(true)
		{
		}
	}
	public sealed class FadingPhoto : CustomEventModel
	{
		private const int GoldMin = 10;

		private const int GoldMax = 28;

		private const int HpLossMin = 3;

		private const int HpLossMax = 7;

		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/fading_photo.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			(DynamicVar)new GoldVar(0),
			(DynamicVar)new HpLossVar(0m)
		};

		public override void CalculateVars()
		{
			((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue = ((EventModel)this).Rng.NextInt(10, 29);
			RollNextHpLoss();
		}

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.照片中的我们出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (((runState != null) ? ((IPlayerCollection)runState).Players : null) == null || ((IPlayerCollection)runState).Players.Count == 0)
			{
				return false;
			}
			return ((IPlayerCollection)runState).Players.Any((Player p) => p != null && p.Relics.Any((RelicModel r) => r is OldMemories));
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[4]
			{
				RestoreOption(),
				((CustomEventModel)this).Option((Func<Task>)Observe, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Leave, HoverTipFactory.FromRelic<OldPhotoAlbum>(), "INITIAL"),
				((CustomEventModel)this).Option((Func<Task>)Ignore, "INITIAL", Array.Empty<IHoverTip>())
			});
		}

		private EventOption RestoreOption()
		{
			Player owner = ((EventModel)this).Owner;
			if (((owner != null) ? owner.Gold : 0) < 10)
			{
				return Locked("INITIAL", "RESTORE");
			}
			return ((CustomEventModel)this).Option((Func<Task>)Restore, "INITIAL", (IHoverTip[])(object)new IHoverTip[1] { HoverTipFactory.Static((StaticHoverTip)4, Array.Empty<DynamicVar>()) });
		}

		private EventOption Locked(string pageKey, string optionKey)
		{
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Expected O, but got Unknown
			//IL_0088: Expected O, but got Unknown
			string text = $"{((AbstractModel)this).Id.Entry}.pages.{pageKey}.options.{optionKey}";
			return ((CustomEventModel)this).Option((Func<Task>)null, new LocString("events", text + ".title"), new LocString("events", text + ".description"), Array.Empty<IHoverTip>());
		}

		private async Task Restore()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("IGNORE"));
				return;
			}
			if (player.Gold < 10)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("IGNORE"));
				return;
			}
			int gold = ((DynamicVar)((EventModel)this).DynamicVars.Gold).IntValue;
			await PlayerCmd.LoseGold((decimal)gold, player, (GoldLossType)1);
			CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1, 1);
			List<CardModel> selected = (await CardSelectCmd.FromDeckForTransformation(player, prefs, (Func<CardModel, CardTransformation>)null)).ToList();
			foreach (CardModel card in selected)
			{
				await CardCmd.TransformToRandom(card, player.RunState.Rng.Niche, (CardPreviewStyle)1);
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("RESTORE"));
		}

		private Task Observe()
		{
			ShowObservePage();
			return Task.CompletedTask;
		}

		private async Task Leave()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("IGNORE"));
				return;
			}
			try
			{
				RelicModel relic = ((IEnumerable<RelicModel>)player.Relics).FirstOrDefault((Func<RelicModel, bool>)((RelicModel r) => r is OldMemories));
				if (relic != null)
				{
					await RelicCmd.Remove(relic);
				}
				await RelicCmd.Obtain(((RelicModel)ModelDb.Relic<OldPhotoAlbum>()).ToMutable(), player, -1);
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("IGNORE"));
			}
		}

		private Task Ignore()
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("IGNORE"));
			return Task.CompletedTask;
		}

		private void ShowObservePage()
		{
			RollNextHpLoss();
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("OBSERVE"), (IEnumerable<EventOption>)GenerateObserveOptions());
		}

		private void RollNextHpLoss()
		{
			((DynamicVar)((EventModel)this).DynamicVars.HpLoss).BaseValue = ((EventModel)this).Rng.NextInt(3, 8);
		}

		private IReadOnlyList<EventOption> GenerateObserveOptions()
		{
			return new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[2]
			{
				(EventOption)(CanObserveAgain() ? ((object)((CustomEventModel)this).Option((Func<Task>)ObserveAgain, "OBSERVE", Array.Empty<IHoverTip>())) : ((object)Locked("OBSERVE", "OBSERVE_AGAIN"))),
				((CustomEventModel)this).Option((Func<Task>)StopObserving, "OBSERVE", Array.Empty<IHoverTip>())
			});
		}

		private bool CanObserveAgain()
		{
			Player owner = ((EventModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null)
			{
				return false;
			}
			if (owner.Creature.CurrentHp <= 7)
			{
				return false;
			}
			return owner.Deck.Cards.Any((CardModel c) => c != null && !c.IsUpgraded);
		}

		private async Task ObserveAgain()
		{
			Player player = ((EventModel)this).Owner;
			if (((player != null) ? player.Creature : null) == null)
			{
				ShowObservePage();
				return;
			}
			int hpLoss = ((DynamicVar)((EventModel)this).DynamicVars.HpLoss).IntValue;
			await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), player.Creature, (decimal)hpLoss, (ValueProp)6, (CardModel)null, (CardPlay)null);
			await WuwuDelivery.UpgradeRandomDeckCards(player, 1);
			ShowObservePage();
		}

		private Task StopObserving()
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("STOP_OBSERVING"));
			return Task.CompletedTask;
		}

		public FadingPhoto()
			: base(true)
		{
		}
	}
	public sealed class GalbrenaEvent : CustomEventModel
	{
		private const int HpCostMin = 8;

		private const int HpCostMax = 12;

		private const int GoldMin = 30;

		private const int GoldMax = 50;

		private const int CardRewardCount = 3;

		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/galbrena_event.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			(DynamicVar)new HpLossVar(0m),
			(DynamicVar)new GoldVar(0)
		};

		public override void CalculateVars()
		{
			((DynamicVar)((EventModel)this).DynamicVars.HpLoss).BaseValue = ((EventModel)this).Rng.NextInt(8, 13);
			((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue = ((EventModel)this).Rng.NextInt(30, 51);
		}

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.别回头出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (((runState != null) ? ((IPlayerCollection)runState).Players : null) == null || ((IPlayerCollection)runState).Players.Count == 0)
			{
				return false;
			}
			return runState.CurrentActIndex == 0;
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[3]
			{
				((CustomEventModel)this).Option((Func<Task>)Help, "INITIAL", (IHoverTip[])(object)new IHoverTip[1] { RandomRelicTip() }),
				((CustomEventModel)this).Option((Func<Task>)Ask, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
			});
		}

		private IHoverTip RandomRelicTip()
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Expected O, but got Unknown
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			return (IHoverTip)(object)new HoverTip(new LocString("events", ((AbstractModel)this).Id.Entry + ".tips.RANDOM_RELIC"), (Texture2D)null);
		}

		private async Task Help()
		{
			Player player = ((EventModel)this).Owner;
			if (((player != null) ? player.Creature : null) == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("HELP_RESULT"));
				return;
			}
			try
			{
				int hpCost = ((DynamicVar)((EventModel)this).DynamicVars.HpLoss).IntValue;
				await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), player.Creature, (decimal)hpCost, (ValueProp)6, (CardModel)null, (CardPlay)null);
				await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(player).ToMutable(), player, -1);
			}
			catch
			{
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("HELP_RESULT"));
		}

		private async Task Ask()
		{
			Player player = ((EventModel)this).Owner;
			if (((player != null) ? player.Creature : null) == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ASK_RESULT"));
				return;
			}
			try
			{
				await GrantUpgradedCardReward(player);
			}
			catch
			{
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ASK_RESULT"));
		}

		private async Task GrantUpgradedCardReward(Player player)
		{
			CardCreationOptions options = CardCreationOptions.ForNonCombatWithDefaultOdds((IEnumerable<CardPoolModel>)new <>z__ReadOnlySingleElementList<CardPoolModel>(player.Character.CardPool), (Func<CardModel, bool>)null);
			List<CardModel> candidates = new List<CardModel>();
			foreach (CardCreationResult result in CardFactory.CreateForReward(player, 3, options))
			{
				CardModel card2 = ((ICardScope)player.RunState).CloneCard(result.Card);
				if (card2.IsUpgradable)
				{
					CardCmd.Upgrade(card2, (CardPreviewStyle)1);
				}
				candidates.Add(card2);
			}
			if (candidates.Count == 0)
			{
				return;
			}
			CardSelectorPrefs val = new CardSelectorPrefs(new LocString("events", ((AbstractModel)this).Id.Entry + ".selectCardPrompt"), 0, 1);
			((CardSelectorPrefs)(ref val)).set_Cancelable(true);
			CardSelectorPrefs prefs = val;
			List<CardModel> chosen = (await CardSelectCmd.FromSimpleGrid((PlayerChoiceContext)new BlockingPlayerChoiceContext(), (IReadOnlyList<CardModel>)candidates, player, prefs)).ToList();
			foreach (CardModel card in chosen)
			{
				await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
			}
		}

		private async Task Leave()
		{
			Player player = ((EventModel)this).Owner;
			if (player != null)
			{
				int gold = ((DynamicVar)((EventModel)this).DynamicVars.Gold).IntValue;
				await PlayerCmd.GainGold((decimal)gold, player, false);
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE_RESULT"));
		}

		public GalbrenaEvent()
			: base(true)
		{
		}
	}
	public sealed class GoodBestieEvent : CustomEventModel
	{
		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/good_bestie.png";

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.好闺蜜出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (runState.CurrentActIndex != 0)
			{
				return false;
			}
			return true;
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Help, HoverTipFactory.FromRelic<OldMemories>(), "INITIAL"),
				((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
			};
		}

		private async Task Help()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
				return;
			}
			try
			{
				await PlayerCmd.GainGold(90m, player, false);
				RelicModel relic = ((RelicModel)ModelDb.Relic<OldMemories>()).ToMutable();
				await RelicCmd.Obtain(relic, player, -1);
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("HELP_DONE"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
			}
		}

		private Task Leave()
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
			return Task.CompletedTask;
		}

		public GoodBestieEvent()
			: base(true)
		{
		}
	}
	public sealed class HappyBirthday : CustomEventModel
	{
		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/happy_birthday.png";

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.生日快乐出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (runState.CurrentActIndex < 1)
			{
				return false;
			}
			return true;
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			List<EventOption> list = new List<EventOption>
			{
				((CustomEventModel)this).Option((Func<Task>)LoseHpForPotion, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)LeaveGift, "INITIAL", Array.Empty<IHoverTip>())
			};
			Player owner = ((EventModel)this).Owner;
			if (owner != null && (owner.Relics?.Any((RelicModel r) => r is OldMemories)).GetValueOrDefault())
			{
				list.Insert(0, ((CustomEventModel)this).Option((Func<Task>)GivePhotos, HoverTipFactory.FromCardWithCardHoverTips<RunePower>(false), "INITIAL"));
			}
			return list;
		}

		private async Task GivePhotos()
		{
			if (((EventModel)this).Owner == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
				return;
			}
			try
			{
				RelicModel relic = ((IEnumerable<RelicModel>)((EventModel)this).Owner.Relics).FirstOrDefault((Func<RelicModel, bool>)((RelicModel r) => r is OldMemories));
				if (relic != null)
				{
					await RelicCmd.Remove(relic);
				}
				RunePower card = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard<RunePower>(((EventModel)this).Owner);
				CardPileCmd.Add((CardModel)(object)card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PHOTOS_DONE"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
			}
		}

		private async Task LoseHpForPotion()
		{
			Player owner = ((EventModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null)
			{
				return;
			}
			try
			{
				await CreatureCmd.SetMaxHp(((EventModel)this).Owner.Creature, Math.Max(1m, (decimal)((EventModel)this).Owner.Creature.MaxHp - 9m));
				DazzlingSweetness dazzlingSweetness = ModelDb.Potion<DazzlingSweetness>();
				PotionModel potion = ((dazzlingSweetness != null) ? ((PotionModel)dazzlingSweetness).ToMutable() : null);
				if (potion != null)
				{
					await PotionCmd.TryToProcure(potion, ((EventModel)this).Owner, -1);
				}
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
			}
		}

		private async Task LeaveGift()
		{
			Player owner = ((EventModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null)
			{
				return;
			}
			try
			{
				IEnumerable<CardModel> selected = await CardSelectCmd.FromDeckForRemoval(((EventModel)this).Owner, new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1, 1), (Func<CardModel, bool>)null);
				if (!selected.Any())
				{
					((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("NO_CARD"));
					return;
				}
				CardModel chosenCard = selected.First();
				PlayerCmd.CompleteQuest(chosenCard);
				await CardPileCmd.RemoveFromDeck(chosenCard, true);
				CardRarity rarity = chosenCard.Rarity;
				CardRarity val = rarity;
				switch (val - 1)
				{
				case 8:
					await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, 6m, (ValueProp)6, (CardModel)null, (CardPlay)null);
					((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("CURSE_RESULT"));
					break;
				case 0:
				case 1:
				case 9:
					await CreatureCmd.Heal(((EventModel)this).Owner.Creature, 3m, true);
					((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("COMMON_RESULT"));
					break;
				case 2:
				case 5:
					await UpgradeCards(2);
					((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("UNCOMMON_RESULT"));
					break;
				case 3:
				{
					await CreatureCmd.Heal(((EventModel)this).Owner.Creature, 10m, true);
					CurtainsEnd curtainCard = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard<CurtainsEnd>(((EventModel)this).Owner);
					CardCmd.PreviewCardPileAdd(await CardPileCmd.Add((CardModel)(object)curtainCard, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
					((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("RARE_RESULT"));
					break;
				}
				case 4:
				{
					List<Reward> relicRewards = new List<Reward>
					{
						(Reward)new RelicReward((RelicRarity)2, ((EventModel)this).Owner),
						(Reward)new RelicReward((RelicRarity)3, ((EventModel)this).Owner),
						(Reward)new RelicReward((RelicRarity)4, ((EventModel)this).Owner)
					};
					await RewardsCmd.OfferCustom(((EventModel)this).Owner, relicRewards);
					((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ANCIENT_RESULT"));
					break;
				}
				default:
					((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
					break;
				}
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
			}
		}

		private async Task UpgradeCards(int count)
		{
			if (((EventModel)this).Owner == null)
			{
				return;
			}
			CardSelectorPrefs val = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1, count);
			((CardSelectorPrefs)(ref val)).set_Cancelable(true);
			((CardSelectorPrefs)(ref val)).set_RequireManualConfirmation(true);
			CardSelectorPrefs prefs = val;
			foreach (CardModel card in await CardSelectCmd.FromDeckForUpgrade(((EventModel)this).Owner, prefs))
			{
				CardCmd.Upgrade(card, (CardPreviewStyle)0);
			}
		}

		public HappyBirthday()
			: base(true)
		{
		}
	}
	public sealed class Hunting : CustomEventModel
	{
		private const int LearnSkillHpCost = 7;

		public override ActModel[] Acts => Array.Empty<ActModel>();

		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/hunting.png";

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.狩猎出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (((runState != null) ? ((IPlayerCollection)runState).Players : null) == null || ((IPlayerCollection)runState).Players.Count == 0)
			{
				return false;
			}
			return runState.CurrentActIndex == 0;
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Follow, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
			};
		}

		private Task Follow()
		{
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("FOLLOW"), (IEnumerable<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)ShareLoot, "FOLLOW", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)LearnSkill, HoverTipFactory.FromCardWithCardHoverTips<Hunt>(false), "FOLLOW")
			});
			return Task.CompletedTask;
		}

		private async Task ShareLoot()
		{
			Player player = ((EventModel)this).Owner;
			if (((player != null) ? player.Creature : null) == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LOOT_RESULT"));
				return;
			}
			try
			{
				await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(player).ToMutable(), player, -1);
			}
			catch
			{
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LOOT_RESULT"));
		}

		private async Task LearnSkill()
		{
			Player player = ((EventModel)this).Owner;
			if (((player != null) ? player.Creature : null) == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEARN_RESULT"));
				return;
			}
			try
			{
				await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), player.Creature, 7m, (ValueProp)6, (CardModel)null, (CardPlay)null);
				CardModel card = (CardModel)(object)((ICardScope)player.RunState).CreateCard<Hunt>(player);
				await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
			}
			catch
			{
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEARN_RESULT"));
		}

		private Task Leave()
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE_RESULT"));
			return Task.CompletedTask;
		}

		public Hunting()
			: base(true)
		{
		}
	}
	public sealed class ItSmellsGoodHereEvent : CustomEventModel
	{
		private int _cardsLostCount;

		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/it_smells_good_here.png";

		public override List<(string, string)>? Localization
		{
			get
			{
				int num = 21;
				List<(string, string)> list = new List<(string, string)>(num);
				CollectionsMarshal.SetCount(list, num);
				Span<(string, string)> span = CollectionsMarshal.AsSpan(list);
				span[0] = ("title", "这里好香");
				span[1] = ("pages.INITIAL.description", "一缕[aqua]若有若无的香气[/aqua]飘入鼻尖。\n你循着味道一路前行，不知何时，已经来到一处从未见过的庭院。\n\n一名女子正静静望着你。\n她似乎有些意外，随后露出一丝浅浅的笑意。\n\n[sine]\"……抱歉。\"[/sine]\n[sine]\"看来，你还是走到这里了。\"[/sine]\n\n[thinky_dots]你忽然有些想不起，自己为何会来到这里。[/thinky_dots]");
				span[2] = ("pages.INITIAL.options.SACRIFICE.title", "继续靠近");
				span[3] = ("pages.INITIAL.options.SACRIFICE.description", "随机失去[red]一张牌[/red]。");
				span[4] = ("pages.INITIAL.options.LEAVE_POTIONS.title", "转身离开");
				span[5] = ("pages.INITIAL.options.LEAVE_POTIONS.description", "什么也没有得到。");
				span[6] = ("pages.SACRIFICE_DONE.description", "香气变得愈发浓郁。\n\n[jitter]有什么东西。[/jitter]\n[jitter]正从你的记忆中悄然滑落。[/jitter]\n\n她静静注视着你，轻轻叹了口气。\n\n[sine]\"我提醒过你。\"[/sine]\n[sine]\"现在离开，还来得及。\"[/sine]");
				span[7] = ("pages.SACRIFICE_DONE.options.SACRIFICE.title", "继续靠近");
				span[8] = ("pages.SACRIFICE_DONE.options.SACRIFICE.description", "随机失去[red]一张牌[/red]。");
				span[9] = ("pages.SACRIFICE_DONE.options.LEAVE_POTIONS.title", "转身离开");
				span[10] = ("pages.SACRIFICE_DONE.options.LEAVE_POTIONS.description", "带着获得的药水离开。");
				span[11] = ("pages.AFTER_THREE.description", "当你终于来到她面前时，那股[aqua]香气[/aqua]反而渐渐平息。\n\n她望着你，像是在确认什么。\n\n[sine]\"真是固执。\"[/sine]\n\n停顿片刻，她轻轻笑了笑。\n\n[sine]\"既然如此......\"[/sine]\n[sine]\"总不能让你白白忘掉些什么。\"[/sine]\n\n[fade_in]她将一样东西放到了你的面前。[/fade_in]");
				span[12] = ("pages.AFTER_THREE.options.GET_RANDOM_RELIC.title", "收下馈赠");
				span[13] = ("pages.AFTER_THREE.options.GET_RANDOM_RELIC.description", "随机获得一个[gold]遗物[/gold]。");
				span[14] = ("pages.AFTER_THREE.options.ADD_UNDER_THE_SEA_CARD.title", "接受帮助");
				span[15] = ("pages.AFTER_THREE.options.ADD_UNDER_THE_SEA_CARD.description", "获得一份[gold]来自深海的启迪[/gold]。");
				span[16] = ("pages.AFTER_THREE.options.LEAVE_POTIONS.title", "转身离开");
				span[17] = ("pages.AFTER_THREE.options.LEAVE_POTIONS.description", "带着获得的药水离开。");
				span[18] = ("pages.RELIC_DONE.description", "你将那份馈赠收起。\n\n[sine]\"希望它能派上用场。\"[/sine]\n\n她依旧站在那里，脸上的笑意没有任何变化。\n\n当你回过神来时，[aqua]香气[/aqua]已经渐渐散去了。");
				span[19] = ("pages.CARD_DONE.description", "她将那卷泛黄的纸页放到你的手中。\n\n纸页无风自动，字迹一点一点淡去。\n\n[thinky_dots]你的脑海里，仿佛多出了一段从未经历过的记忆。[/thinky_dots]\n\n[sine]\"这样......应该就够了。\"[/sine]");
				span[20] = ("pages.DONE.description", "你离开了那座庭院。\n\n回头望去，那里只剩一片寂静，仿佛从未有人停留。\n\n只有鼻尖残留着一缕[aqua]淡淡的香气[/aqua]。\n\n[thinky_dots]而你总觉得，自己似乎忘记了什么。[/thinky_dots]");
				return list;
			}
		}

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.这里好香出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (!((IPlayerCollection)runState).Players.All((Player p) => p.Relics.Any((RelicModel r) => r is FeisaliesGift)))
			{
				return false;
			}
			return true;
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Sacrifice, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)LeavePotions, "INITIAL", Array.Empty<IHoverTip>())
			});
		}

		private async Task Sacrifice()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
				return;
			}
			List<CardModel> deck = player.Deck.Cards.ToList();
			if (deck.Count == 0)
			{
				await LeavePotions();
				return;
			}
			CardModel cardToRemove = deck[SyncedRng.Index(((EventModel)this).Rng, deck.Count)];
			_cardsLostCount++;
			if (_cardsLostCount <= 2)
			{
				((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("SACRIFICE_DONE"), (IEnumerable<EventOption>)new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[2]
				{
					((CustomEventModel)this).Option((Func<Task>)Sacrifice, "SACRIFICE_DONE", Array.Empty<IHoverTip>()),
					((CustomEventModel)this).Option((Func<Task>)LeavePotions, "SACRIFICE_DONE", Array.Empty<IHoverTip>())
				}));
			}
			else
			{
				((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("AFTER_THREE"), (IEnumerable<EventOption>)new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[3]
				{
					((CustomEventModel)this).Option((Func<Task>)GetRandomRelic, "AFTER_THREE", Array.Empty<IHoverTip>()),
					((CustomEventModel)this).Option((Func<Task>)AddUnderTheSeaCard, HoverTipFactory.FromCardWithCardHoverTips<UnderTheSea>(false), "AFTER_THREE"),
					((CustomEventModel)this).Option((Func<Task>)LeavePotions, "AFTER_THREE", Array.Empty<IHoverTip>())
				}));
			}
			await CardPileCmd.RemoveFromDeck(cardToRemove, true);
		}

		private async Task LeavePotions()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
				return;
			}
			try
			{
				List<Reward> rewards = new List<Reward>();
				for (int i = 0; i < _cardsLostCount; i++)
				{
					PotionModel potion = PotionFactory.CreateRandomPotionOutOfCombat(player, player.RunState.Rng.CombatPotionGeneration, (IEnumerable<PotionModel>)null).ToMutable();
					rewards.Add((Reward)new PotionReward(potion, player));
				}
				await RewardsCmd.OfferCustom(player, rewards);
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
			}
		}

		private async Task GetRandomRelic()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
				return;
			}
			try
			{
				RelicModel relic = RelicFactory.PullNextRelicFromFront(player).ToMutable();
				await RelicCmd.Obtain(relic, player, -1);
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("RELIC_DONE"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
			}
		}

		private async Task AddUnderTheSeaCard()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
				return;
			}
			try
			{
				UnderTheSea card = ((ICardScope)player.RunState).CreateCard<UnderTheSea>(player);
				CardPileCmd.Add((CardModel)(object)card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("CARD_DONE"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
			}
		}

		public ItSmellsGoodHereEvent()
			: base(true)
		{
		}
	}
	public sealed class MottaliAngerEvent : CustomEventModel
	{
		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/mottali_anger.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new GoldVar(300) };

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.莫塔里之忿出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (!((IPlayerCollection)runState).Players.All((Player p) => p.Relics.Any((RelicModel r) => r is MottaliGift || r is MottaliBankCard)))
			{
				return false;
			}
			return true;
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Apologize, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Mock, HoverTipFactory.FromCardWithCardHoverTips<NewWaveEra>(false), "INITIAL")
			});
		}

		private async Task Apologize()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
				return;
			}
			try
			{
				await PlayerCmd.GainGold(((DynamicVar)((EventModel)this).DynamicVars.Gold).BaseValue, player, false);
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("APOLOGIZE_DONE"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
			}
		}

		private async Task Mock()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
				return;
			}
			try
			{
				MottaliGift mottaliCanonical = ModelDb.Relic<MottaliGift>();
				RelicModel mottaliRelic = ((IEnumerable<RelicModel>)player.Relics).FirstOrDefault((Func<RelicModel, bool>)((RelicModel r) => ((AbstractModel)r).Id == ((AbstractModel)mottaliCanonical).Id));
				if (mottaliRelic != null)
				{
					await RelicCmd.Remove(mottaliRelic);
				}
				NewWaveEra card = ((ICardScope)player.RunState).CreateCard<NewWaveEra>(player);
				CardPileCmd.Add((CardModel)(object)card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("MOCK_DONE"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
			}
		}

		public MottaliAngerEvent()
			: base(true)
		{
		}
	}
	public sealed class RhythmPoemEvent : CustomEventModel
	{
		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/rhythm_poem.png";

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.律绘之诗出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (runState.CurrentActIndex != 0)
			{
				return false;
			}
			if (!(runState.Act is Overgrowth))
			{
				return false;
			}
			return true;
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[3]
			{
				((CustomEventModel)this).Option((Func<Task>)SingAlong, HoverTipFactory.FromRelic<MiniTone>(), "INITIAL"),
				((CustomEventModel)this).Option((Func<Task>)Listen, HoverTipFactory.FromCardWithCardHoverTips<TripleBrilliance>(false), "INITIAL"),
				((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
			};
		}

		private async Task SingAlong()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
				return;
			}
			try
			{
				RelicModel relic = ((RelicModel)ModelDb.Relic<MiniTone>()).ToMutable();
				await RelicCmd.Obtain(relic, player, -1);
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("SING_DONE"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
			}
		}

		private async Task Listen()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
				return;
			}
			try
			{
				TripleBrilliance card = ((ICardScope)player.RunState).CreateCard<TripleBrilliance>(player);
				CardPileCmd.Add((CardModel)(object)card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LISTEN_DONE"));
			}
			catch
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
			}
		}

		private Task Leave()
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
			return Task.CompletedTask;
		}

		public RhythmPoemEvent()
			: base(true)
		{
		}
	}
	public sealed class StarChartEvent : CustomEventModel
	{
		private const int MaxHpCostMin = 8;

		private const int MaxHpCostMax = 13;

		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/mornye_event.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new HpLossVar(0m) };

		public override LocString InitialDescription
		{
			get
			{
				if (RunHasVisitedDataStream())
				{
					return ((CustomEventModel)this).PageDescription("AFTER_DATA_STREAM");
				}
				return ((EventModel)this).InitialDescription;
			}
		}

		public override void CalculateVars()
		{
			((DynamicVar)((EventModel)this).DynamicVars.HpLoss).BaseValue = ((EventModel)this).Rng.NextInt(8, 14);
		}

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.偏移的星图出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (((runState != null) ? ((IPlayerCollection)runState).Players : null) == null || ((IPlayerCollection)runState).Players.Count == 0)
			{
				return false;
			}
			return true;
		}

		private bool RunHasVisitedDataStream()
		{
			Player owner = ((EventModel)this).Owner;
			IRunState val = ((owner != null) ? owner.RunState : null);
			if (val == null)
			{
				return false;
			}
			ModelId target = ((AbstractModel)ModelDb.Event<DataStreamEvent>()).Id;
			return val.MapPointHistory.Any((IReadOnlyList<MapPointHistoryEntry> act) => act.Any((MapPointHistoryEntry point) => point.Rooms.Any(delegate(MapPointRoomHistoryEntry room)
			{
				ModelId modelId = room.ModelId;
				return modelId != null && modelId.Equals(target);
			})));
		}

		private static bool HasQuestCard(Player player)
		{
			return player.Deck.Cards.Any((CardModel c) => (c is CollectData || c is MassEnergyEquivalence) ? true : false);
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[3]
			{
				AdjustOption(),
				ObserveOption(),
				((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
			});
		}

		private EventOption ObserveOption()
		{
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Expected O, but got Unknown
			//IL_0079: Expected O, but got Unknown
			string text = ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.OBSERVE";
			if (((EventModel)this).Owner != null && HasQuestCard(((EventModel)this).Owner))
			{
				return ((CustomEventModel)this).Option((Func<Task>)Observe, new LocString("events", text + ".title"), new LocString("events", text + ".descriptionLinked"), HoverTipFactory.FromCardWithCardHoverTips<CriticalProtocol>(true).ToArray());
			}
			return ((CustomEventModel)this).Option((Func<Task>)Observe, "INITIAL", HoverTipFactory.FromCardWithCardHoverTips<CriticalProtocol>(false).ToArray());
		}

		private EventOption AdjustOption()
		{
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Expected O, but got Unknown
			//IL_0089: Expected O, but got Unknown
			if (((EventModel)this).Owner == null || (decimal)((EventModel)this).Owner.Creature.MaxHp <= ((DynamicVar)((EventModel)this).DynamicVars.HpLoss).BaseValue)
			{
				string text = ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.ADJUST";
				return ((CustomEventModel)this).Option((Func<Task>)null, new LocString("events", text + ".title"), new LocString("events", text + ".description"), Array.Empty<IHoverTip>());
			}
			return ((CustomEventModel)this).Option((Func<Task>)Adjust, HoverTipFactory.FromRelic<StarChart>(), "INITIAL");
		}

		private async Task Adjust()
		{
			Player player = ((EventModel)this).Owner;
			if (((player != null) ? player.Creature : null) == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ADJUST_RESULT"));
				return;
			}
			try
			{
				await CreatureCmd.LoseMaxHp((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), player.Creature, ((DynamicVar)((EventModel)this).DynamicVars.HpLoss).BaseValue, false);
				await RelicCmd.Obtain(((RelicModel)ModelDb.Relic<StarChart>()).ToMutable(), player, -1);
			}
			catch
			{
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ADJUST_RESULT"));
		}

		private async Task Observe()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OBSERVE_RESULT"));
				return;
			}
			try
			{
				CardModel card = (CardModel)(object)((ICardScope)player.RunState).CreateCard<CriticalProtocol>(player);
				if (HasQuestCard(player))
				{
					CardCmd.Upgrade(card, (CardPreviewStyle)1);
					if (card is CriticalProtocol protocol)
					{
						protocol.PenaltyBattles = 0;
					}
				}
				CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
			}
			catch
			{
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OBSERVE_RESULT"));
		}

		private Task Leave()
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE_RESULT"));
			return Task.CompletedTask;
		}

		public StarChartEvent()
			: base(true)
		{
		}
	}
	public sealed class StoneGambling : CustomEventModel
	{
		private const int MinGoldToAppear = 100;

		private const int BetCost = 25;

		private const int HitPercent = 50;

		private const int RelicPercent = 50;

		private const int PotionPercent = 30;

		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/stone_gambling.png";

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (((runState != null) ? ((IPlayerCollection)runState).Players : null) == null || ((IPlayerCollection)runState).Players.Count == 0)
			{
				return false;
			}
			return ((IPlayerCollection)runState).Players.All((Player p) => p != null && p.Gold >= 100);
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Bet, "INITIAL", (IHoverTip[])(object)new IHoverTip[1] { GambleTip() }),
				((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
			});
		}

		private IHoverTip GambleTip()
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Expected O, but got Unknown
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			return (IHoverTip)(object)new HoverTip(new LocString("events", ((AbstractModel)this).Id.Entry + ".tips.GAMBLE"), (Texture2D)null);
		}

		private Task Bet()
		{
			return BetInternal();
		}

		private Task BetAgain()
		{
			return BetInternal();
		}

		private Task Leave()
		{
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("LEAVE"), (IEnumerable<EventOption>)Array.Empty<EventOption>());
			return Task.CompletedTask;
		}

		private async Task BetInternal()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("LEAVE"), (IEnumerable<EventOption>)Array.Empty<EventOption>());
				return;
			}
			if (player.Gold < 25)
			{
				((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("NO_GOLD"), (IEnumerable<EventOption>)new <>z__ReadOnlySingleElementList<EventOption>(((CustomEventModel)this).Option((Func<Task>)Leave, "NO_GOLD", Array.Empty<IHoverTip>())));
				return;
			}
			await PlayerCmd.LoseGold(25m, player, (GoldLossType)1);
			bool flag = ((EventModel)this).Rng.NextInt(100) < 50;
			bool flag2 = flag;
			if (flag2)
			{
				flag2 = await TryGrantSomething(player);
			}
			string page = (flag2 ? "REWARD" : "EMPTY");
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription(page), (IEnumerable<EventOption>)new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)BetAgain, page, Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Leave, page, Array.Empty<IHoverTip>())
			}));
		}

		private async Task<bool> TryGrantSomething(Player player)
		{
			int category = ((EventModel)this).Rng.NextInt(100);
			if (category < 50)
			{
				return await TryGrantRelic(player);
			}
			if (category < 80)
			{
				if (await TryGrantPotion(player))
				{
					return true;
				}
				bool flag = await TryGrantRelic(player);
				if (!flag)
				{
					flag = await TryGrantCard(player);
				}
				return flag;
			}
			return await TryGrantCard(player);
		}

		private async Task<bool> TryGrantRelic(Player player)
		{
			Player player2 = player;
			RelicRarity rarity = RollRelicRarity(((EventModel)this).Rng);
			List<RelicModel> pool = (from r in ModelDb.AllRelics
				where r != null && r.Rarity == rarity
				where player2.Relics.All((RelicModel owned) => ((AbstractModel)owned).Id.Entry != ((AbstractModel)r).Id.Entry)
				select r).ToList();
			if (pool.Count == 0)
			{
				return false;
			}
			RelicModel pick = pool[((EventModel)this).Rng.NextInt(pool.Count)];
			await RelicCmd.Obtain(pick.ToMutable(), player2, -1);
			return true;
		}

		private async Task<bool> TryGrantPotion(Player player)
		{
			if (!player.HasOpenPotionSlots)
			{
				return false;
			}
			PotionModel potion = PotionFactory.CreateRandomPotionOutOfCombat(player, ((EventModel)this).Rng, (IEnumerable<PotionModel>)null).ToMutable();
			return (await PotionCmd.TryToProcure(potion, player, -1)).success;
		}

		private async Task<bool> TryGrantCard(Player player)
		{
			CardRarity[] rarities = RollCardRarities(((EventModel)this).Rng);
			HashSet<CardPoolModel> allowedPools = (from p in ModelDb.AllCharacterCardPools.Concat(ModelDb.AllSharedCardPools)
				where p != null
				select p).ToHashSet();
			List<CardModel> pool = (from c in ModelDb.AllCards.Where((CardModel c) => c != null && rarities.Contains(c.Rarity)).Where(delegate(CardModel c)
				{
					//IL_0001: Unknown result type (might be due to invalid IL or missing references)
					//IL_0006: Unknown result type (might be due to invalid IL or missing references)
					//IL_0007: Unknown result type (might be due to invalid IL or missing references)
					//IL_0009: Unknown result type (might be due to invalid IL or missing references)
					//IL_000b: Invalid comparison between Unknown and I4
					CardType type = c.Type;
					return type - 1 <= 2;
				})
				where c.Pool != null && allowedPools.Contains(c.Pool)
				select c).ToList();
			if (pool.Count == 0)
			{
				return false;
			}
			CardModel card = ((ICardScope)player.RunState).CreateCard(pool[((EventModel)this).Rng.NextInt(pool.Count)], player);
			CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
			return true;
		}

		private static RelicRarity RollRelicRarity(Rng rng)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			int num = rng.NextInt(100);
			if (num < 2)
			{
				return (RelicRarity)7;
			}
			if (num < 20)
			{
				return (RelicRarity)4;
			}
			if (num < 70)
			{
				return (RelicRarity)3;
			}
			return (RelicRarity)2;
		}

		private static CardRarity[] RollCardRarities(Rng rng)
		{
			int num = rng.NextInt(100);
			if (num < 2)
			{
				return (CardRarity[])(object)new CardRarity[1] { (CardRarity)5 };
			}
			if (num < 20)
			{
				return (CardRarity[])(object)new CardRarity[1] { (CardRarity)4 };
			}
			if (num < 70)
			{
				return (CardRarity[])(object)new CardRarity[1] { (CardRarity)3 };
			}
			return (CardRarity[])(object)new CardRarity[2]
			{
				(CardRarity)2,
				(CardRarity)1
			};
		}

		public StoneGambling()
			: base(true)
		{
		}
	}
	public sealed class SuisuiMysteryShop : CustomEventModel
	{
		private const int MinGold = 100;

		private static readonly Type[] StockTypes = new Type[9]
		{
			typeof(SisterLetter),
			typeof(PeaceAndProsperity),
			typeof(DeadBranch),
			typeof(Sundial),
			typeof(InsectSpecimen),
			typeof(OuterCalipers),
			typeof(ToyOrnithopter),
			typeof(InkBottle),
			typeof(StrangeSpoon)
		};

		private const int StockSlots = 6;

		private MerchantInventory? _inventory;

		public MerchantInventory? Inventory => _inventory;

		public override EventLayoutType LayoutType => (EventLayoutType)3;

		public override bool IsShared => true;

		public override IEnumerable<LocString> GameInfoOptions => Array.Empty<LocString>();

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return Array.Empty<EventOption>();
		}

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (((runState != null) ? ((IPlayerCollection)runState).Players : null) == null || ((IPlayerCollection)runState).Players.Count == 0)
			{
				return false;
			}
			int currentActIndex = runState.CurrentActIndex;
			if ((uint)(currentActIndex - 1) > 1u)
			{
				return false;
			}
			return ((IPlayerCollection)runState).Players.All((Player p) => p != null && p.Gold > 100);
		}

		internal static bool IsShopRelic(Type relicType)
		{
			return StockTypes.Contains(relicType);
		}

		protected override Task BeforeEventStarted(bool isPreFinished)
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Expected O, but got Unknown
			//IL_015c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0166: Expected O, but got Unknown
			Player owner = ((EventModel)this).Owner;
			if (owner == null)
			{
				return Task.CompletedTask;
			}
			MerchantInventory val = new MerchantInventory(owner);
			List<Type> list = new List<Type>();
			Type[] stockTypes = StockTypes;
			foreach (Type type2 in stockTypes)
			{
				if (ModelDb.AllRelics.Any((RelicModel r) => r != null && ((object)r).GetType() == type2))
				{
					list.Add(type2);
				}
			}
			PlayerRngSet playerRng = owner.PlayerRng;
			Rng val2 = ((playerRng != null) ? playerRng.Shops : null);
			if (val2 != null)
			{
				for (int num = list.Count - 1; num > 0; num--)
				{
					int num2 = val2.NextInt(num + 1);
					List<Type> list2 = list;
					int index = num;
					int index2 = num2;
					Type value = list[num2];
					Type value2 = list[num];
					list2[index] = value;
					list[index2] = value2;
				}
			}
			foreach (Type type in list.Take(6))
			{
				RelicModel val3 = ModelDb.AllRelics.FirstOrDefault((Func<RelicModel, bool>)((RelicModel r) => r != null && ((object)r).GetType() == type));
				if (val3 != null)
				{
					val.AddRelicEntry(new MerchantRelicEntry(val3.ToMutable(), owner));
				}
			}
			_inventory = val;
			return Task.CompletedTask;
		}

		public void FinishShop()
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
		}

		public SuisuiMysteryShop()
			: base(true)
		{
		}
	}
	[HarmonyPatch(typeof(MerchantRelicEntry), "CalcCost")]
	public static class SuisuiShopPricePatch
	{
		private const int MinPrice = 50;

		private const int MaxPrice = 100;

		private static readonly HashSet<MerchantRelicEntry> Priced = new HashSet<MerchantRelicEntry>();

		public static void Postfix(MerchantRelicEntry __instance)
		{
			RelicModel model = __instance.Model;
			if (model != null && SuisuiMysteryShop.IsShopRelic(((object)model).GetType()) && Priced.Add(__instance))
			{
				Player value = Traverse.Create((object)__instance).Field("_player").GetValue<Player>();
				object obj;
				if (value == null)
				{
					obj = null;
				}
				else
				{
					PlayerRngSet playerRng = value.PlayerRng;
					obj = ((playerRng != null) ? playerRng.Shops : null);
				}
				Rng val = (Rng)obj;
				int num = ((val != null) ? (50 + val.NextInt(51)) : 75);
				Traverse.Create((object)__instance).Field("_cost").SetValue((object)num);
			}
		}
	}
	public sealed class WeiXun : CustomEventModel
	{
		public override ActModel[] Acts => Array.Empty<ActModel>();

		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/weixun.png";

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.微醺出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (((runState != null) ? ((IPlayerCollection)runState).Players : null) == null || ((IPlayerCollection)runState).Players.Count == 0)
			{
				return false;
			}
			return true;
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)PointTheWay, HoverTipFactory.FromRelic<EglaTributeWine>(), "INITIAL"),
				((CustomEventModel)this).Option((Func<Task>)TakeWine, HoverTipFactory.FromRelic<WinePot>(), "INITIAL")
			};
		}

		private async Task PointTheWay()
		{
			try
			{
				await GrantRelic<EglaTributeWine>();
			}
			catch
			{
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("POINT_RESULT"));
		}

		private async Task TakeWine()
		{
			try
			{
				await GrantRelic<WinePot>();
			}
			catch
			{
			}
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("WINE_RESULT"));
		}

		private async Task GrantRelic<T>() where T : RelicModel
		{
			Player player = ((EventModel)this).Owner;
			if (player != null)
			{
				await RelicCmd.Obtain(((RelicModel)ModelDb.Relic<T>()).ToMutable(), player, -1);
			}
		}

		public WeiXun()
			: base(true)
		{
		}
	}
	public sealed class WuwuLogistics : CustomEventModel
	{
		private const int PriceCards = 80;

		private const int PriceRelics = 160;

		private const int PriceAncient = 320;

		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/wuwu_logistics.png";

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.呜呜物流出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (runState.CurrentActIndex >= 2)
			{
				return false;
			}
			if (((IPlayerCollection)runState).Players == null || ((IPlayerCollection)runState).Players.Count == 0)
			{
				return false;
			}
			return ((IPlayerCollection)runState).Players.All((Player p) => p != null && p.Gold >= 100);
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Open, "INITIAL", Array.Empty<IHoverTip>()) };
		}

		private async Task Open()
		{
			Player player = ((EventModel)this).Owner;
			if (player == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
				return;
			}
			switch (SyncedRng.Index(((EventModel)this).Rng, 4))
			{
			case 0:
				await WuwuDelivery.GrantRandomPotions(player, 2);
				break;
			case 1:
				PlayerCmd.GainGold(99m, player, false);
				break;
			case 2:
				await OpenColorlessCard(player);
				break;
			default:
				await WuwuDelivery.UpgradeRandomDeckCards(player, 2);
				break;
			}
			((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("SHOP"), (IEnumerable<EventOption>)GenerateShopOptions());
		}

		private async Task OpenColorlessCard(Player player)
		{
			List<CardModel> cards = (from c in WuwuDelivery.CreateColorlessRewardOptions(player, 3)
				select c.Card).ToList();
			List<CardModel> chosen = (await CardSelectCmd.FromSimpleGrid((PlayerChoiceContext)new BlockingPlayerChoiceContext(), (IReadOnlyList<CardModel>)cards, player, new CardSelectorPrefs(new LocString("events", "WUWANCIENTS-WUWU_LOGISTICS.selectCardPrompt"), 1, 1))).ToList();
			foreach (CardModel card in chosen)
			{
				await CardPileCmd.Add(card, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
			}
		}

		private IReadOnlyList<EventOption> GenerateShopOptions()
		{
			Player owner = ((EventModel)this).Owner;
			int gold = ((owner != null) ? owner.Gold : 0);
			return (IReadOnlyList<EventOption>)(object)new EventOption[4]
			{
				Buyable(gold, 80, "CARDS"),
				Buyable(gold, 160, "RELICS"),
				Buyable(gold, 320, "ANCIENT"),
				((CustomEventModel)this).Option((Func<Task>)Leave, "SHOP", Array.Empty<IHoverTip>())
			};
		}

		private EventOption Buyable(int gold, int price, string optionKey)
		{
			return (EventOption)((gold >= price) ? ((object)((CustomEventModel)this).Option(For(price), "SHOP", Array.Empty<IHoverTip>())) : ((object)Locked(optionKey)));
		}

		private EventOption Locked(string optionKey)
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Expected O, but got Unknown
			//IL_004e: Expected O, but got Unknown
			string text = ((AbstractModel)this).Id.Entry + ".pages.SHOP.options." + optionKey;
			return ((CustomEventModel)this).Option((Func<Task>)null, new LocString("events", text + ".title"), new LocString("events", text + ".description"), Array.Empty<IHoverTip>());
		}

		private Func<Task> For(int price)
		{
			if (1 == 0)
			{
			}
			Func<Task> result = price switch
			{
				80 => Cards, 
				160 => Relics, 
				_ => Ancient, 
			};
			if (1 == 0)
			{
			}
			return result;
		}

		private Task Cards()
		{
			return Buy(80, WuwuDelivery.Kind.CardRewards);
		}

		private Task Relics()
		{
			return Buy(160, WuwuDelivery.Kind.TwoRelics);
		}

		private Task Ancient()
		{
			return Buy(320, WuwuDelivery.Kind.AncientRelic);
		}

		private Task Buy(int price, WuwuDelivery.Kind kind)
		{
			Player owner = ((EventModel)this).Owner;
			if (owner == null)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
				return Task.CompletedTask;
			}
			if (owner.Gold < price)
			{
				((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
				return Task.CompletedTask;
			}
			PlayerCmd.LoseGold((decimal)price, owner, (GoldLossType)1);
			WuwuDelivery.Schedule(owner, kind);
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BUY_DONE"));
			return Task.CompletedTask;
		}

		private Task Leave()
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
			return Task.CompletedTask;
		}

		public WuwuLogistics()
			: base(true)
		{
		}
	}
	public sealed class YiZhanZuYiEvent : CustomEventModel
	{
		public override string? CustomInitialPortraitPath => "res://wuwancients/images/events/yi_zhan_zu_yi.png";

		public override bool IsAllowed(IRunState runState)
		{
			if (!WuwancientsConfig.AllEventsEnabled)
			{
				return false;
			}
			if (WuwancientsConfig.江湖梦出现方式 == EventAppearMode.关闭)
			{
				return false;
			}
			if (runState.CurrentActIndex != 0)
			{
				return false;
			}
			if (!(runState.Act is Overgrowth))
			{
				return false;
			}
			return true;
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			return new <>z__ReadOnlyArray<EventOption>((EventOption[])(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)TakeCard, HoverTipFactory.FromCardWithCardHoverTips<YiZhanZuYi>(false), "INITIAL"),
				((CustomEventModel)this).Option((Func<Task>)TakePotion, "INITIAL", Array.Empty<IHoverTip>())
			});
		}

		private Task TakeCard()
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
			YiZhanZuYi yiZhanZuYi = ((ICardScope)((EventModel)this).Owner.RunState).CreateCard<YiZhanZuYi>(((EventModel)this).Owner);
			CardPileCmd.Add((CardModel)(object)yiZhanZuYi, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false);
			return Task.CompletedTask;
		}

		private async Task TakePotion()
		{
			PotionModel potion = ((PotionModel)ModelDb.Potion<ChongZhouMalaiDouFu>()).ToMutable();
			await PotionCmd.TryToProcure(potion, ((EventModel)this).Owner, -1);
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DONE"));
		}

		public YiZhanZuYiEvent()
			: base(true)
		{
		}
	}
}
namespace wuwancients.Enchantments
{
	public class Anchored : CustomEnchantmentModel
	{
		public override bool HasExtraCardText => true;

		public override bool ShowAmount => false;

		protected override string? CustomIconPath => "res://wuwancients/images/icons/youno_map_icon.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel drawnCard, bool fromHandDraw)
		{
			if (!(drawnCard.Enchantment is Anchored))
			{
				return;
			}
			Player player = drawnCard.Owner;
			if (((player != null) ? player.PlayerCombatState : null) != null)
			{
				int toDraw = CardPile.MaxCardsInHand - player.PlayerCombatState.Hand.Cards.Count;
				if (toDraw > 0)
				{
					await CardPileCmd.Draw(choiceContext, (decimal)toDraw, player, false);
				}
			}
		}

		protected override void OnEnchant()
		{
		}
	}
	public class ArmorBreakEnchantment : CustomEnchantmentModel
	{
		public override bool HasExtraCardText => true;

		public override bool ShowAmount => false;

		protected override string? CustomIconPath => "res://wuwancients/images/enchantments/ArmorBreakEnchantment.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
		{
			(DynamicVar)new DamageVar(2m, (ValueProp)8),
			(DynamicVar)new PowerVar<VulnerablePower>(1m),
			(DynamicVar)new PowerVar<WeakPower>(1m)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
		{
			HoverTipFactory.FromPower<VulnerablePower>((int?)null),
			HoverTipFactory.FromPower<WeakPower>((int?)null)
		};

		public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			if (!ValuePropExtensions.IsPoweredAttack(props))
			{
				return 0m;
			}
			return -((DynamicVar)((EnchantmentModel)this).DynamicVars.Damage).BaseValue;
		}

		public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
		{
			if (cardPlay == null)
			{
				return;
			}
			IReadOnlyList<Creature> targets;
			if ((int)((EnchantmentModel)this).Card.TargetType != 3 && cardPlay.Target != null)
			{
				targets = (IReadOnlyList<Creature>)(object)new Creature[1] { cardPlay.Target };
			}
			else
			{
				if (((EnchantmentModel)this).Card.CombatState == null)
				{
					return;
				}
				targets = ((EnchantmentModel)this).Card.CombatState.HittableEnemies;
			}
			await PowerCmd.Apply<VulnerablePower>(choiceContext, (IEnumerable<Creature>)targets, ((DynamicVar)((EnchantmentModel)this).DynamicVars.Vulnerable).BaseValue, ((EnchantmentModel)this).Card.Owner.Creature, ((EnchantmentModel)this).Card, false);
			await PowerCmd.Apply<WeakPower>(choiceContext, (IEnumerable<Creature>)targets, ((DynamicVar)((EnchantmentModel)this).DynamicVars.Weak).BaseValue, ((EnchantmentModel)this).Card.Owner.Creature, ((EnchantmentModel)this).Card, false);
		}

		protected override void OnEnchant()
		{
		}
	}
	public sealed class BloodOath : CustomEnchantmentModel
	{
		private bool _growing;

		private CardPlay? _lastCardPlay;

		private static readonly HashSet<string> DisplayOnlyVars = new HashSet<string> { "StrengthPerVulnerable" };

		[SavedProperty]
		public int Bonus { get; set; }

		[SavedProperty]
		public int Applied { get; set; }

		[SavedProperty]
		public string Baselines { get; set; } = "";


		[SavedProperty]
		public string VarBonuses { get; set; } = "";


		public override bool HasExtraCardText => true;

		public override bool ShowAmount => Bonus > 0;

		public override int DisplayAmount => Bonus;

		protected override string? CustomIconPath => "res://wuwancients/images/enchantments/blood_oath.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new StringVar("PowerPrefix", "") };

		public override bool CanEnchantCardType(CardType cardType)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Invalid comparison between Unknown and I4
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Invalid comparison between Unknown and I4
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Invalid comparison between Unknown and I4
			if ((int)cardType == 3)
			{
				return WuwancientsConfig.血誓可附魔能力牌;
			}
			return (int)cardType == 1 || (int)cardType == 2;
		}

		public override bool CanEnchant(CardModel card)
		{
			if (!((EnchantmentModel)this).CanEnchant(card))
			{
				return false;
			}
			return HasNumericVar(card);
		}

		protected override void OnEnchant()
		{
			if (((EnchantmentModel)this).Card != null)
			{
				if (!((EnchantmentModel)this).Card.Keywords.Contains((CardKeyword)1))
				{
					((EnchantmentModel)this).Card.AddKeyword((CardKeyword)1);
				}
				SyncPowerPrefix();
				CaptureBaselines(((EnchantmentModel)this).Card);
				ApplyAllBonuses(((EnchantmentModel)this).Card);
				Applied = Bonus;
				Log.Info(string.Format("[wuwancients] blood oath enchanted {0}: vars=[{1}] baselines=[{2}]", ((AbstractModel)((EnchantmentModel)this).Card).Id.Entry, string.Join(",", ((EnchantmentModel)this).Card.DynamicVars.Values.Select((DynamicVar v) => v.Name)), Baselines), 2);
			}
		}

		public override void RecalculateValues()
		{
			SyncPowerPrefix();
			if (((EnchantmentModel)this).Card != null && (Bonus > 0 || !string.IsNullOrEmpty(VarBonuses)) && NeedsBump(((EnchantmentModel)this).Card))
			{
				ApplyAllBonuses(((EnchantmentModel)this).Card);
			}
		}

		internal void GrowForPlay(CardPlay? cardPlay)
		{
			CardModel card = ((EnchantmentModel)this).Card;
			if (card == null)
			{
				Log.Info("[wuwancients] blood oath skip: enchantment has no card", 2);
				return;
			}
			if (_growing)
			{
				Log.Info($"[wuwancients] blood oath skip: re-entered while growing, card {((AbstractModel)card).Id.Entry}", 2);
				return;
			}
			if (cardPlay != null && cardPlay == _lastCardPlay)
			{
				Log.Info($"[wuwancients] blood oath skip: same CardPlay dispatched twice, card {((AbstractModel)card).Id.Entry}", 2);
				return;
			}
			_lastCardPlay = cardPlay;
			_growing = true;
			try
			{
				CardModel deckVersion = card.DeckVersion;
				BloodOath bloodOath2 = ((((deckVersion != null) ? deckVersion.Enchantment : null) is BloodOath bloodOath && bloodOath != this) ? bloodOath : this);
				CardModel val = ((bloodOath2 == this) ? card : deckVersion);
				bloodOath2.CaptureBaselines(val);
				List<string> list = bloodOath2.RollableNames(val);
				if (list.Count == 0)
				{
					Log.Info(string.Format("[wuwancients] blood oath skip: no rollable var on {0} (vars=[{1}] baselines=[{2}] bonuses=[{3}])", ((AbstractModel)val).Id.Entry, string.Join(",", val.DynamicVars.Values.Select((DynamicVar v) => v.Name)), bloodOath2.Baselines, bloodOath2.VarBonuses), 2);
					MirrorFrom(bloodOath2);
					Applied = Bonus;
					return;
				}
				Player owner = card.Owner;
				object obj;
				if (owner == null)
				{
					obj = null;
				}
				else
				{
					IRunState runState = owner.RunState;
					if (runState == null)
					{
						obj = null;
					}
					else
					{
						RunRngSet rng = runState.Rng;
						obj = ((rng != null) ? rng.Niche : null);
					}
				}
				Rng rng2 = (Rng)obj;
				int index = SyncedRng.Index(rng2, list.Count);
				string name = list[index];
				Dictionary<string, int> dictionary = ParseMap(bloodOath2.VarBonuses, int.Parse);
				dictionary.TryGetValue(name, out var value);
				dictionary[name] = value + 1;
				bloodOath2.VarBonuses = FormatMap(dictionary.Select((KeyValuePair<string, int> pair) => new KeyValuePair<string, decimal>(pair.Key, pair.Value)));
				bloodOath2.Bonus++;
				bloodOath2.Applied = bloodOath2.Bonus;
				bloodOath2.ApplyAllBonuses(val);
				if (bloodOath2 != this)
				{
					MirrorFrom(bloodOath2);
					CaptureBaselines(card);
					ApplyAllBonuses(card);
				}
				object[] obj2 = new object[6]
				{
					((AbstractModel)card).Id.Entry,
					((object)card).GetHashCode(),
					name,
					value + 1,
					bloodOath2.Bonus,
					null
				};
				DynamicVar? obj3 = card.DynamicVars.Values.FirstOrDefault((Func<DynamicVar, bool>)((DynamicVar v) => v.Name == name));
				obj2[5] = ((obj3 != null) ? obj3.BaseValue : 0m);
				Log.Info(string.Format("[wuwancients] blood oath on {0} (inst {1}): +1 {2} -> bonus {3}, total {4}, value now {5}", obj2), 2);
				SyncToDeckVersion();
				SyncPowerPrefix();
			}
			finally
			{
				_growing = false;
			}
		}

		private void MirrorFrom(BloodOath source)
		{
			if (source != this)
			{
				Bonus = source.Bonus;
				Applied = source.Bonus;
				Baselines = source.Baselines;
				VarBonuses = source.VarBonuses;
			}
		}

		public override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
		{
			SyncToDeckVersion();
			return Task.CompletedTask;
		}

		public static bool HasNumericVar(CardModel card)
		{
			return card.DynamicVars.Values.Any(IsBumpable);
		}

		private void SyncPowerPrefix()
		{
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			DynamicVar val = default(DynamicVar);
			if (((EnchantmentModel)this).DynamicVars.TryGetValue("PowerPrefix", ref val))
			{
				StringVar val2 = (StringVar)(object)((val is StringVar) ? val : null);
				if (val2 != null)
				{
					string text = (WuwancientsConfig.血誓可附魔能力牌 ? "WUWANCIENTS-BLOOD_OATH.prefixPower" : "WUWANCIENTS-BLOOD_OATH.prefixNormal");
					val2.StringValue = new LocString("enchantments", text).GetFormattedText();
				}
			}
		}

		private void SyncToDeckVersion()
		{
			CardModel card = ((EnchantmentModel)this).Card;
			CardModel val = ((card != null) ? card.DeckVersion : null);
			if (val == null)
			{
				Log.Info("[wuwancients] blood oath sync: combat copy has no deck version, this +1 only exists on the copy", 2);
				return;
			}
			if (!(val.Enchantment is BloodOath bloodOath))
			{
				Log.Info($"[wuwancients] blood oath sync: deck card {((AbstractModel)val).Id.Entry} carries no blood oath", 2);
				return;
			}
			if (bloodOath == this)
			{
				Log.Info($"[wuwancients] blood oath sync: deck card {((AbstractModel)val).Id.Entry} shares this enchantment instance", 2);
				return;
			}
			bloodOath.Bonus = Bonus;
			bloodOath.Applied = Bonus;
			bloodOath.Baselines = Baselines;
			bloodOath.VarBonuses = VarBonuses;
			bloodOath.ApplyAllBonuses(val);
			Log.Info($"[wuwancients] blood oath sync: wrote bonus {Bonus} back to deck card {((AbstractModel)val).Id.Entry}", 2);
		}

		private void CaptureBaselines(CardModel card)
		{
			Dictionary<string, decimal> dictionary = ParseMap(Baselines, ParseDecimal);
			Dictionary<string, int> dictionary2 = ParseMap(VarBonuses, int.Parse);
			bool flag = false;
			foreach (DynamicVar value2 in card.DynamicVars.Values)
			{
				if (IsBumpable(value2) && !dictionary.ContainsKey(value2.Name))
				{
					dictionary2.TryGetValue(value2.Name, out var value);
					dictionary[value2.Name] = Math.Max(0m, value2.BaseValue - (decimal)value);
					flag = true;
				}
			}
			if (flag)
			{
				Baselines = FormatMap(dictionary);
			}
		}

		private List<string> RollableNames(CardModel card)
		{
			Dictionary<string, decimal> dictionary = ParseMap(Baselines, ParseDecimal);
			return (from variable in card.DynamicVars.Values.Where(IsBumpable)
				select variable.Name).Where(dictionary.ContainsKey).ToList();
		}

		private void ApplyAllBonuses(CardModel card)
		{
			BumpVars(card);
			string text = SnapshotBumpable(card);
			card.DynamicVars.RecalculateForUpgradeOrEnchant();
			string text2 = SnapshotBumpable(card);
			if (text != text2)
			{
				Log.Info($"[wuwancients] blood oath recalc on {((AbstractModel)card).Id.Entry}: {text} -> {text2}", 2);
			}
			BumpVars(card);
		}

		private void BumpVars(CardModel card)
		{
			Dictionary<string, decimal> dictionary = ParseMap(Baselines, ParseDecimal);
			Dictionary<string, int> dictionary2 = ParseMap(VarBonuses, int.Parse);
			foreach (DynamicVar item in card.DynamicVars.Values.ToList())
			{
				if (IsBumpable(item) && dictionary.TryGetValue(item.Name, out var value))
				{
					dictionary2.TryGetValue(item.Name, out var value2);
					decimal num = value + (decimal)value2;
					if (item.BaseValue < num)
					{
						item.BaseValue = num;
						continue;
					}
					Log.Info($"[wuwancients] blood oath apply: {((AbstractModel)card).Id.Entry} {item.Name} stays {item.BaseValue}, target {num} not higher", 2);
				}
			}
		}

		private bool NeedsBump(CardModel card)
		{
			Dictionary<string, decimal> dictionary = ParseMap(Baselines, ParseDecimal);
			if (dictionary.Count == 0)
			{
				return false;
			}
			Dictionary<string, int> dictionary2 = ParseMap(VarBonuses, int.Parse);
			foreach (DynamicVar value3 in card.DynamicVars.Values)
			{
				if (IsBumpable(value3) && dictionary.TryGetValue(value3.Name, out var value))
				{
					dictionary2.TryGetValue(value3.Name, out var value2);
					if (value3.BaseValue < value + (decimal)value2)
					{
						return true;
					}
				}
			}
			return false;
		}

		private static string SnapshotBumpable(CardModel card)
		{
			return string.Join(",", from v in card.DynamicVars.Values.Where(IsBumpable)
				select v.Name + "=" + v.BaseValue);
		}

		private static decimal ParseDecimal(string text)
		{
			decimal result;
			return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out result) ? result : 0m;
		}

		private static Dictionary<string, T> ParseMap<T>(string raw, Func<string, T> parse)
		{
			Dictionary<string, T> dictionary = new Dictionary<string, T>();
			if (string.IsNullOrEmpty(raw))
			{
				return dictionary;
			}
			string[] array = raw.Split(';');
			foreach (string text in array)
			{
				int num = text.IndexOf('=');
				if (num > 0)
				{
					dictionary[text.Substring(0, num)] = parse(text.Substring(num + 1));
				}
			}
			return dictionary;
		}

		private static string FormatMap(IEnumerable<KeyValuePair<string, decimal>> map)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<string, decimal> item in map)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(';');
				}
				stringBuilder.Append(item.Key).Append('=').Append(item.Value.ToString(CultureInfo.InvariantCulture));
			}
			return stringBuilder.ToString();
		}

		private static bool IsBumpable(DynamicVar variable)
		{
			if ((variable is EnergyVar || variable is StringVar || variable is BoolVar || variable is IfUpgradedVar || variable is CalculatedVar) ? true : false)
			{
				return false;
			}
			return !DisplayOnlyVars.Contains(variable.Name);
		}
	}
	[HarmonyPatch(typeof(Hook), "BeforeCardPlayed")]
	internal static class BloodOathBeforeCardPlayedPatch
	{
		private static void Prefix(CardPlay cardPlay)
		{
			CardModel val = ((cardPlay != null) ? cardPlay.Card : null);
			if (val == null)
			{
				return;
			}
			if (val.Enchantment is BloodOath bloodOath)
			{
				bloodOath.GrowForPlay(cardPlay);
				return;
			}
			CardModel deckVersion = val.DeckVersion;
			if (((deckVersion != null) ? deckVersion.Enchantment : null) is BloodOath)
			{
				Log.Info($"[wuwancients] blood oath miss: played copy of {((AbstractModel)val).Id.Entry} has no enchantment, deck copy does", 2);
			}
		}
	}
	public sealed class FlameLight : CustomEnchantmentModel
	{
		internal int PendingX;

		private readonly List<(DynamicVar Var, decimal Base)> _scaled = new List<(DynamicVar, decimal)>();

		protected override string? CustomIconPath => "res://wuwancients/images/relics/flame_claw.png";

		public override bool HasExtraCardText => false;

		public override bool CanEnchantCardType(CardType cardType)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Invalid comparison between Unknown and I4
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Invalid comparison between Unknown and I4
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Invalid comparison between Unknown and I4
			return (int)cardType == 1 || (int)cardType == 2 || (int)cardType == 3;
		}

		public override bool CanEnchant(CardModel card)
		{
			return !card.EnergyCost.CostsX && !card.HasStarCostX;
		}

		internal int CurrentX()
		{
			return ResolvedX();
		}

		public override Task BeforeCardPlayed(CardPlay cardPlay)
		{
			if (!((EnchantmentModel)this).HasCard)
			{
				return Task.CompletedTask;
			}
			if (((cardPlay != null) ? cardPlay.Card : null) != ((EnchantmentModel)this).Card)
			{
				return Task.CompletedTask;
			}
			ScaleVars(ResolvedX());
			return Task.CompletedTask;
		}

		public override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
		{
			RestoreVars();
			return Task.CompletedTask;
		}

		private void ScaleVars(int x)
		{
			RestoreVars();
			if (!((EnchantmentModel)this).HasCard)
			{
				return;
			}
			foreach (DynamicVar value in ((EnchantmentModel)this).Card.DynamicVars.Values)
			{
				if (value != null)
				{
					decimal baseValue = value.BaseValue;
					_scaled.Add((value, baseValue));
					value.BaseValue = baseValue * (decimal)x;
				}
			}
		}

		private void RestoreVars()
		{
			foreach (var (val, baseValue) in _scaled)
			{
				if (val != null)
				{
					val.BaseValue = baseValue;
				}
			}
			_scaled.Clear();
		}

		private int ResolvedX()
		{
			int num = Math.Max(0, PendingX);
			if (!((EnchantmentModel)this).HasCard)
			{
				return num;
			}
			try
			{
				CardModel card = ((EnchantmentModel)this).Card;
				ICombatState combatState = card.CombatState;
				if (combatState == null)
				{
					return num;
				}
				return Math.Max(0, Hook.ModifyXValue(combatState, card, num));
			}
			catch (Exception)
			{
				return num;
			}
		}
	}
	[HarmonyPatch(typeof(CardModel), "SpendResources")]
	public static class FlameLightCaptureXPatch
	{
		public static void Prefix(CardModel __instance)
		{
			if (__instance.Enchantment is FlameLight flameLight)
			{
				Player owner = __instance.Owner;
				int? obj;
				if (owner == null)
				{
					obj = null;
				}
				else
				{
					PlayerCombatState playerCombatState = owner.PlayerCombatState;
					obj = ((playerCombatState != null) ? new int?(playerCombatState.Energy) : null);
				}
				int? num = obj;
				flameLight.PendingX = num.GetValueOrDefault();
			}
		}
	}
	[HarmonyPatch(typeof(CardEnergyCost), "GetAmountToSpend")]
	public static class FlameLightSpendAllEnergyPatch
	{
		public static void Postfix(CardEnergyCost __instance, ref int __result)
		{
			CardModel val = CardOf(__instance);
			if (((val != null) ? val.Enchantment : null) is FlameLight)
			{
				Player owner = val.Owner;
				int? obj;
				if (owner == null)
				{
					obj = null;
				}
				else
				{
					PlayerCombatState playerCombatState = owner.PlayerCombatState;
					obj = ((playerCombatState != null) ? new int?(playerCombatState.Energy) : null);
				}
				int? num = obj;
				__result = Math.Max(0, num.GetValueOrDefault());
			}
		}

		private static CardModel? CardOf(CardEnergyCost cost)
		{
			try
			{
				return Traverse.Create((object)cost).Field("_card").GetValue<CardModel>();
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
	[HarmonyPatch(typeof(PlayerCombatState), "HasEnoughResourcesFor")]
	public static class FlameLightAlwaysPlayablePatch
	{
		public static void Postfix(CardModel card, ref UnplayableReason reason)
		{
			if (((uint)reason & 0x10u) != 0 && card.Enchantment is FlameLight)
			{
				reason = (UnplayableReason)((uint)reason & 0xFFFFFFEFu);
			}
		}
	}
	[HarmonyPatch(typeof(ChemicalX), "BeforeCardPlayed")]
	public static class FlameLightChemicalXFlashPatch
	{
		public static void Prefix(CardPlay cardPlay, ChemicalX __instance)
		{
			object obj;
			if (cardPlay == null)
			{
				obj = null;
			}
			else
			{
				CardModel card = cardPlay.Card;
				obj = ((card != null) ? card.Enchantment : null);
			}
			if (obj is FlameLight && cardPlay.Card.Owner == ((RelicModel)__instance).Owner)
			{
				((RelicModel)__instance).Flash();
			}
		}
	}
	internal static class FlameLightCostLabelPatch
	{
		internal static void Apply(Harmony harmony)
		{
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Expected O, but got Unknown
			MethodInfo methodInfo = AccessTools.Method(typeof(NCard), "UpdateEnergyCostVisuals", new Type[1] { typeof(PileType) }, (Type[])null);
			if (!(methodInfo == null))
			{
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(FlameLightCostLabelPatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null);
			}
		}

		private static void Postfix(NCard __instance)
		{
			object obj;
			if (__instance == null)
			{
				obj = null;
			}
			else
			{
				CardModel model = __instance.Model;
				obj = ((model != null) ? model.Enchantment : null);
			}
			if (obj is FlameLight)
			{
				object value = Traverse.Create((object)__instance).Field("_energyLabel").GetValue<object>();
				value?.GetType().GetMethod("SetTextAutoSize", new Type[1] { typeof(string) })?.Invoke(value, new object[1] { "X" });
			}
		}
	}
	internal static class FlameLightDescriptionPatch
	{
		private static readonly string[] CostWords = new string[9] { "耗能", "费用", "能量", "cost", "Cost", "energy", "Energy", "비용", "에너지" };

		internal static void Apply(Harmony harmony)
		{
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Expected O, but got Unknown
			MethodInfo methodInfo = typeof(CardModel).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault((MethodInfo m) => m.Name == "GetDescriptionForPile" && m.GetParameters().Length == 3);
			if (!(methodInfo == null))
			{
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(FlameLightDescriptionPatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null);
			}
		}

		private static void Postfix(CardModel __instance, ref string __result)
		{
			if (((__instance != null) ? __instance.Enchantment : null) is FlameLight && !string.IsNullOrEmpty(__result))
			{
				__result = AddXToNumbers(__instance, __result);
			}
		}

		private static string AddXToNumbers(CardModel card, string text)
		{
			string text2 = text;
			HashSet<string> known = new HashSet<string>();
			foreach (DynamicVar value2 in card.DynamicVars.Values)
			{
				if (value2 != null)
				{
					known.Add(Format(value2.BaseValue));
					known.Add(Format(value2.EnchantedValue));
					known.Add(Format(value2.PreviewValue));
				}
			}
			return Regex.Replace(text2, "\\d+", delegate(Match match)
			{
				if (!known.Contains(match.Value))
				{
					return match.Value;
				}
				int num = Math.Max(0, match.Index - 8);
				string text3 = text2.Substring(num, match.Index - num);
				int num2 = Math.Min(4, text2.Length - match.Index - match.Length);
				string text4 = ((num2 > 0) ? text2.Substring(match.Index + match.Length, num2) : string.Empty);
				string[] costWords = CostWords;
				foreach (string value in costWords)
				{
					if (text3.Contains(value) || text4.Contains(value))
					{
						return match.Value;
					}
				}
				return text4.StartsWith("X") ? match.Value : (match.Value + "X");
			});
		}

		private static string Format(decimal value)
		{
			return (value == Math.Floor(value)) ? ((long)value).ToString() : value.ToString("0.##");
		}
	}
	public sealed class SmeltedFragmentEnchantment : CustomEnchantmentModel
	{
		public override bool HasExtraCardText => true;

		public override bool ShowAmount => false;

		protected override string? CustomIconPath => "res://wuwancients/images/relics/smelted_fragment.png";

		public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			if (!ValuePropExtensions.IsPoweredAttack(props))
			{
				return 0m;
			}
			Player owner = ((EnchantmentModel)this).Card.Owner;
			if (((owner != null) ? owner.Deck : null) == null)
			{
				return 0m;
			}
			int num = owner.Deck.Cards.Count((CardModel c) => (int)c.Type == 1 && c.IsUpgraded);
			return num;
		}

		protected override void OnEnchant()
		{
		}
	}
	public sealed class SplashDye : CustomEnchantmentModel
	{
		protected override string? CustomIconPath => "res://wuwancients/images/enchantments/splash_dye.png";

		public override bool HasExtraCardText => true;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new StringVar("SplashPrefix", "") };

		public override bool CanEnchantCardType(CardType cardType)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Invalid comparison between Unknown and I4
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Invalid comparison between Unknown and I4
			return (int)cardType == 1 || (int)cardType == 2;
		}

		protected override void OnEnchant()
		{
			SyncPrefix();
			if (((EnchantmentModel)this).Card != null && WuwancientsConfig.溅染转移牌面数字 && !((EnchantmentModel)this).Card.Keywords.Contains((CardKeyword)1))
			{
				((EnchantmentModel)this).Card.AddKeyword((CardKeyword)1);
			}
		}

		public override void RecalculateValues()
		{
			SyncPrefix();
		}

		public override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
		{
			//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
			CardModel self = ((EnchantmentModel)this).Card;
			CardModel obj = self;
			Player val = ((obj != null) ? obj.Owner : null);
			if (self == null || val == null)
			{
				return Task.CompletedTask;
			}
			EnchantmentModel val2 = (EnchantmentModel)(object)ModelDb.Enchantment<SplashDye>();
			List<CardModel> list = PileTypeExtensions.GetPile((PileType)2, val).Cards.Where((CardModel c) => c != self).ToList();
			foreach (CardModel item in list)
			{
				if (!(item.Enchantment is SplashDye) && ((item.Enchantment == null) ? val2.CanEnchant(item) : val2.CanEnchantCardType(item.Type)))
				{
					EnchantUtil.Apply(item, val2);
				}
			}
			List<CardModel> list2 = list.Where((CardModel c) => c.Enchantment is SplashDye).ToList();
			if (WuwancientsConfig.溅染转移牌面数字)
			{
				int num = EnchantUtil.NumericTotal(self);
				if (num != 0)
				{
					foreach (CardModel item2 in list2)
					{
						EnchantUtil.AddNumericVars(item2, num);
					}
				}
			}
			else
			{
				foreach (CardModel item3 in list2)
				{
					EnchantUtil.AddDamage(item3, 1);
				}
			}
			return Task.CompletedTask;
		}

		private void SyncPrefix()
		{
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			DynamicVar val = default(DynamicVar);
			if (((EnchantmentModel)this).DynamicVars.TryGetValue("SplashPrefix", ref val))
			{
				StringVar val2 = (StringVar)(object)((val is StringVar) ? val : null);
				if (val2 != null)
				{
					string text = (WuwancientsConfig.溅染转移牌面数字 ? "WUWANCIENTS-SPLASH_DYE.prefixFull" : "WUWANCIENTS-SPLASH_DYE.prefixNormal");
					val2.StringValue = new LocString("enchantments", text).GetFormattedText();
				}
			}
		}
	}
	public class Unison : CustomEnchantmentModel
	{
		public override bool HasExtraCardText => true;

		public override bool ShowAmount => false;

		protected override string? CustomIconPath => "res://wuwancients/images/enchantments/unison.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel drawnCard, bool fromHandDraw)
		{
			CardModel drawnCard2 = drawnCard;
			if (!(drawnCard2.Enchantment is Unison))
			{
				return;
			}
			Player player = drawnCard2.Owner;
			if (((player != null) ? player.PlayerCombatState : null) == null)
			{
				return;
			}
			IEnumerable<CardModel> allCards = player.PlayerCombatState.AllCards;
			List<CardModel> otherUnisonCards = allCards.Where((CardModel c) => c != drawnCard2 && c.Enchantment is Unison).ToList();
			foreach (CardModel card in otherUnisonCards)
			{
				CardPile pile = card.Pile;
				if (pile == null || (int)pile.Type != 2)
				{
					await CardPileCmd.Add(card, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
				}
			}
		}

		protected override void OnEnchant()
		{
		}
	}
}
namespace wuwancients.Cards
{
	public sealed class BiAn : CardModel
	{
		private static readonly string[] SfxPool = new string[3] { "res://wuwancients/audio/bi_an_1.wav", "res://wuwancients/audio/bi_an_2.wav", "res://wuwancients/audio/bi_an_3.wav" };

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/bi_an.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			(DynamicVar)new CardsVar(2),
			(DynamicVar)new EnergyVar(3)
		};

		public BiAn()
			: base(0, (CardType)2, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			SfxCmd.Play(SfxPool[Random.Shared.Next(SfxPool.Length)], 1f);
			Player owner = ((CardModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null)
			{
				return;
			}
			ICombatState combatState = ((CardModel)this).Owner.Creature.CombatState;
			if (combatState != null)
			{
				List<CardModel> curses = ModelDb.AllCards.Where((CardModel c) => (int)c.Type == 5).ToList();
				if (curses.Count > 0)
				{
					int index = ((CardModel)this).Owner.RunState.Rng.CombatCardGeneration.NextInt(curses.Count);
					CardModel curse = combatState.CreateCard(curses[index], ((CardModel)this).Owner);
					CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(curse, (PileType)3, ((CardModel)this).Owner, (CardPilePosition)1), 1.2f, (CardPreviewStyle)1);
				}
			}
			await CardPileCmd.Draw(choiceContext, (decimal)((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue, ((CardModel)this).Owner, false);
			await PlayerCmd.GainEnergy((decimal)((DynamicVar)((CardModel)this).DynamicVars.Energy).IntValue, ((CardModel)this).Owner);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).DynamicVars["Cards"].UpgradeValueBy(2m);
		}
	}
	public sealed class BiShi : CardModel
	{
		private const string TakeoverStepKey = "TakeoverStep";

		private static readonly string[] SfxPool = new string[3] { "res://wuwancients/audio/bi_shi_1.wav", "res://wuwancients/audio/bi_shi_2.wav", "res://wuwancients/audio/bi_shi_3.wav" };

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/bi_shi.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[4]
		{
			(DynamicVar)new EnergyVar(3),
			(DynamicVar)new PowerVar<StrengthPower>(3m),
			(DynamicVar)new CardsVar(3),
			(DynamicVar)new IntVar("TakeoverStep", 1m)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<FuluoluoTakeoverPower>((int?)null) };

		public BiShi()
			: base(0, (CardType)2, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			SfxCmd.Play(SfxPool[Random.Shared.Next(SfxPool.Length)], 1f);
			Player owner = ((CardModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				await PowerCmd.Apply<StrengthPower>(choiceContext, ((CardModel)this).Owner.Creature, ((CardModel)this).DynamicVars["StrengthPower"].BaseValue, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
				await PlayerCmd.GainEnergy((decimal)((DynamicVar)((CardModel)this).DynamicVars.Energy).IntValue, ((CardModel)this).Owner);
				await CardPileCmd.Draw(choiceContext, (decimal)((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue, ((CardModel)this).Owner, false);
				await PowerCmd.Apply<FuluoluoTakeoverCountPower>(choiceContext, ((CardModel)this).Owner.Creature, ((CardModel)this).DynamicVars["TakeoverStep"].BaseValue, ((CardModel)this).Owner.Creature, (CardModel)(object)this, true);
				FuluoluoTakeoverCountPower power = ((CardModel)this).Owner.Creature.GetPower<FuluoluoTakeoverCountPower>();
				int plays = (int)(((decimal?)((power != null) ? new int?(((PowerModel)power).Amount) : null)) ?? 1m);
				if (plays < 1)
				{
					plays = 1;
				}
				FuluoluoTakeoverPower takeover = ((CardModel)this).Owner.Creature.GetPower<FuluoluoTakeoverPower>();
				if (takeover == null)
				{
					await PowerCmd.Apply<FuluoluoTakeoverPower>(choiceContext, ((CardModel)this).Owner.Creature, (decimal)plays, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
					return;
				}
				await PowerCmd.ModifyAmount(choiceContext, (PowerModel)(object)takeover, (decimal)(plays - ((PowerModel)takeover).Amount), ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
				FuluoluoTakeoverPower.PlayLongSfx();
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)5);
		}
	}
	public sealed class BlazingSun : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/blazing_sun.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new DamageVar(9m, (ValueProp)8) };

		public override IEnumerable<CardKeyword> CanonicalKeywords => new <>z__ReadOnlyArray<CardKeyword>((CardKeyword[])(object)new CardKeyword[2]
		{
			(CardKeyword)1,
			(CardKeyword)2
		});

		public override int MaxUpgradeLevel => 0;

		public BlazingSun()
			: base(0, (CardType)1, (CardRarity)5, (TargetType)0, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			Player owner = ((CardModel)this).Owner;
			Creature creature = ((owner != null) ? owner.Creature : null);
			if (creature == null)
			{
				return;
			}
			ICombatState combatState = creature.CombatState;
			if (combatState != null)
			{
				IReadOnlyList<Creature> enemies = combatState.HittableEnemies;
				if (enemies.Count > 0)
				{
					await CreatureCmd.Damage(choiceContext, (IEnumerable<Creature>)enemies.ToList(), ((CardModel)this).DynamicVars["Damage"].BaseValue, (ValueProp)8, creature);
				}
				await PowerCmd.Apply<Majesty>(choiceContext, creature, 1m, creature, (CardModel)(object)this, false);
			}
		}
	}
	public sealed class Bloom : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/bloom.png";

		public override int MaxUpgradeLevel => 0;

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)4 };

		public override bool HasTurnEndInHandEffect => true;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new DamageVar(2m, (ValueProp)12) };

		public Bloom()
			: base(-1, (CardType)5, (CardRarity)9, (TargetType)0, true)
		{
		}

		protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
		{
			int num = Random.Shared.Next(3);
			if (1 == 0)
			{
			}
			string text = num switch
			{
				0 => "res://wuwancients/audio/bloom_1.wav", 
				1 => "res://wuwancients/audio/bloom_2.wav", 
				_ => "res://wuwancients/audio/bloom_3.wav", 
			};
			if (1 == 0)
			{
			}
			string audioPath = text;
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot(audioPath, 1f);
			}
			Player owner = ((CardModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				Creature creature = owner.Creature;
				obj = ((creature != null) ? creature.CombatState : null);
			}
			ICombatState combatState = (ICombatState)obj;
			if (combatState == null)
			{
				return;
			}
			DamageVar dmgVar = ((CardModel)this).DynamicVars.Damage;
			if (combatState.Players != null)
			{
				foreach (Player player in combatState.Players)
				{
					if (((player != null) ? player.Creature : null) != null)
					{
						await CreatureCmd.Damage(choiceContext, player.Creature, dmgVar, (CardModel)(object)this, (CardPlay)null);
					}
				}
			}
			if (combatState.HittableEnemies == null)
			{
				return;
			}
			foreach (Creature enemy in combatState.HittableEnemies)
			{
				await CreatureCmd.Damage(choiceContext, enemy, dmgVar, (CardModel)(object)this, (CardPlay)null);
			}
		}
	}
	public sealed class CanRen : CardModel
	{
		private const string PickableKey = "Pickable";

		private const string StepKey = "Step";

		private static readonly string[] SfxPool = new string[3] { "res://wuwancients/audio/cruelty_1.wav", "res://wuwancients/audio/cruelty_2.wav", "res://wuwancients/audio/cruelty_3.wav" };

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/cruelty.png";

		public override int MaxUpgradeLevel => 1;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			(DynamicVar)new IntVar("Pickable", 1m),
			(DynamicVar)new IntVar("Step", 1m)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromCard<DeathWarrant>(((CardModel)this).IsUpgraded) };

		public CanRen()
			: base(1, (CardType)2, (CardRarity)5, (TargetType)1, true)
		{
		}

		private int PlaysBefore(ICombatState combatState, CardPlay cardPlay)
		{
			ICombatState combatState2 = combatState;
			CardPlay cardPlay2 = cardPlay;
			CombatManager instance = CombatManager.Instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				CombatHistory history = instance.History;
				obj = ((history != null) ? history.CardPlaysFinished : null);
			}
			if (obj == null)
			{
				obj = Enumerable.Empty<CardPlayFinishedEntry>();
			}
			IEnumerable<CardPlayFinishedEntry> source = (IEnumerable<CardPlayFinishedEntry>)obj;
			return source.Count((CardPlayFinishedEntry e) => e.CardPlay.Player == ((CardModel)this).Owner && ((CombatHistoryEntry)e).HappenedThisTurn(combatState2) && e.CardPlay != cardPlay2);
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			SfxCmd.Play(SfxPool[Random.Shared.Next(SfxPool.Length)], 1f);
			Player owner = ((CardModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) == null)
			{
				return;
			}
			ICombatState combatState = ((CardModel)this).Owner.Creature.CombatState;
			if (combatState == null)
			{
				return;
			}
			int pickable = ((CardModel)this).DynamicVars["Pickable"].IntValue + ((CardModel)this).DynamicVars["Step"].IntValue * PlaysBefore(combatState, cardPlay);
			if (pickable <= 0)
			{
				return;
			}
			CardSelectorPrefs val = new CardSelectorPrefs(((CardModel)this).SelectionScreenPrompt, 0, pickable);
			List<CardModel> chosen = (await CardSelectCmd.FromHand(choiceContext, ((CardModel)this).Owner, val, (Func<CardModel, bool>)null, (AbstractModel)(object)this)).ToList();
			if (chosen.Count == 0)
			{
				return;
			}
			foreach (CardModel original in chosen)
			{
				CardModel warrant = combatState.CreateCard((CardModel)(object)ModelDb.Card<DeathWarrant>(), ((CardModel)this).Owner);
				if (((CardModel)this).IsUpgraded)
				{
					CardCmd.Upgrade(warrant, (CardPreviewStyle)1);
				}
				await CardPileCmd.RemoveFromCombat((IEnumerable<CardModel>)(object)new CardModel[1] { original }, false);
				await CardPileCmd.AddGeneratedCardToCombat(warrant, (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
			}
		}
	}
	public sealed class CollectData : CardModel
	{
		public const int RequiredRooms = 6;

		private int _roomsEntered;

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/collect_data.png";

		public override int MaxUpgradeLevel => 0;

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)4 };

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("Rooms", 6m)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromCard<MassEnergyEquivalence>(false) };

		[SavedProperty]
		public int RoomsEntered
		{
			get
			{
				return _roomsEntered;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_roomsEntered = value;
				((CardModel)this).DynamicVars["Rooms"].BaseValue = 6 - RoomsEntered;
			}
		}

		public CollectData()
			: base(-1, (CardType)6, (CardRarity)10, (TargetType)0, true)
		{
		}

		public override async Task AfterRoomEntered(AbstractRoom room)
		{
			if (((CardModel)this).Owner != null)
			{
				RoomsEntered++;
				if (RoomsEntered >= 6)
				{
					PlayerCmd.CompleteQuest((CardModel)(object)this);
					await CardCmd.TransformTo<MassEnergyEquivalence>((CardModel)(object)this, (CardPreviewStyle)1);
				}
			}
		}
	}
	public sealed class CriticalProtocol : CardModel
	{
		public const int PenaltyBattlesDefault = 3;

		private const int DazedPerCombat = 2;

		private int _penaltyBattles = 3;

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/critical_protocol.png";

		public override int MaxUpgradeLevel => 1;

		[SavedProperty]
		public int PenaltyBattles
		{
			get
			{
				return _penaltyBattles;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_penaltyBattles = value;
			}
		}

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			(DynamicVar)new DamageVar(16m, (ValueProp)8),
			(DynamicVar)new PowerVar<DexterityPower>(1m)
		};

		public CriticalProtocol()
			: base(1, (CardType)1, (CardRarity)5, (TargetType)3, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			Player owner = ((CardModel)this).Owner;
			Creature creature = ((owner != null) ? owner.Creature : null);
			ICombatState combatState = ((creature != null) ? creature.CombatState : null);
			if (creature != null && combatState != null)
			{
				await DamageCmd.Attack(((DynamicVar)((CardModel)this).DynamicVars.Damage).BaseValue).FromCard((CardModel)(object)this, cardPlay).TargetingAllOpponents(combatState)
					.Execute(choiceContext);
				await PowerCmd.Apply<DexterityPower>(choiceContext, creature, ((CardModel)this).DynamicVars["DexterityPower"].BaseValue, creature, (CardModel)(object)this, false);
			}
		}

		protected override void OnUpgrade()
		{
			((DynamicVar)((CardModel)this).DynamicVars.Damage).UpgradeValueBy(10m);
			((CardModel)this).DynamicVars["DexterityPower"].UpgradeValueBy(1m);
		}

		public override async Task BeforeCombatStart()
		{
			Player owner = ((CardModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null && PenaltyBattles > 0)
			{
				await CardPileCmd.AddToCombatAndPreview<Dazed>(((CardModel)this).Owner.Creature, (PileType)1, 2, ((CardModel)this).Owner, (CardPilePosition)3);
				PenaltyBattles--;
			}
		}
	}
	public sealed class CurtainsEnd : CardModel
	{
		private static readonly string[] SfxPool = new string[3] { "res://wuwancients/audio/curtains_end_1.wav", "res://wuwancients/audio/curtains_end_2.wav", "res://wuwancients/audio/curtains_end_3.wav" };

		private static readonly Random Rng = new Random();

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/curtains_end.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new DamageVar(20m, (ValueProp)8) };

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<EntropyIllusionPower>((int?)null) };

		public override int MaxUpgradeLevel => 1;

		public CurtainsEnd()
			: base(3, (CardType)1, (CardRarity)5, (TargetType)3, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			SfxCmd.Play(SfxPool[Rng.Next(SfxPool.Length)], 1f);
			await DamageCmd.Attack(((CardModel)this).DynamicVars["Damage"].BaseValue).FromCard((CardModel)(object)this, cardPlay).TargetingAllOpponents(((CardModel)this).CombatState)
				.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
				.Execute(choiceContext);
			Player owner = ((CardModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				await PowerCmd.Apply<EntropyIllusionPower>(choiceContext, ((CardModel)this).Owner.Creature, 1m, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
			}
			if (((CardModel)this).Owner != null)
			{
				CurtainsEndIllusion illusionCard = ((CardModel)this).CombatState.CreateCard<CurtainsEndIllusion>(((CardModel)this).Owner);
				CardCmd.PreviewCardPileAdd(await CardPileCmd.Add((CardModel)(object)illusionCard, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false), 0.5f, (CardPreviewStyle)1);
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).DynamicVars["Damage"].UpgradeValueBy(9m);
		}
	}
	public sealed class CurtainsEndIllusion : CardModel
	{
		private static readonly string[] SfxPool = new string[3] { "res://wuwancients/audio/curtains_end_illusion_1.wav", "res://wuwancients/audio/curtains_end_illusion_2.wav", "res://wuwancients/audio/curtains_end_illusion_3.wav" };

		private static readonly Random Rng = new Random();

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/curtains_end_illusion.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new DamageVar(10m, (ValueProp)8) };

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<EntropyStagePower>((int?)null) };

		public override int MaxUpgradeLevel => 1;

		public CurtainsEndIllusion()
			: base(2, (CardType)1, (CardRarity)5, (TargetType)2, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			SfxCmd.Play(SfxPool[Rng.Next(SfxPool.Length)], 1f);
			for (int i = 0; i < 4; i++)
			{
				await DamageCmd.Attack(((CardModel)this).DynamicVars["Damage"].BaseValue).FromCard((CardModel)(object)this, cardPlay).Targeting(cardPlay.Target)
					.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
					.Execute(choiceContext);
			}
			Player owner = ((CardModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				await PowerCmd.Apply<EntropyStagePower>(choiceContext, ((CardModel)this).Owner.Creature, 3m, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
			}
			if (((CardModel)this).Owner != null)
			{
				CurtainsEnd stageCard = ((CardModel)this).CombatState.CreateCard<CurtainsEnd>(((CardModel)this).Owner);
				CardCmd.PreviewCardPileAdd(await CardPileCmd.Add((CardModel)(object)stageCard, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false), 0.5f, (CardPreviewStyle)1);
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).DynamicVars["Damage"].UpgradeValueBy(4m);
		}
	}
	public sealed class DeathWarrant : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/death_warrant.png";

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new DamageVar(9m, (ValueProp)8) };

		public override int MaxUpgradeLevel => 1;

		public DeathWarrant()
			: base(0, (CardType)1, (CardRarity)7, (TargetType)2, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			await DamageCmd.Attack(((DynamicVar)((CardModel)this).DynamicVars.Damage).BaseValue).FromCard((CardModel)(object)this, cardPlay).Targeting(cardPlay.Target)
				.Execute(choiceContext);
		}

		protected override void OnUpgrade()
		{
			((DynamicVar)((CardModel)this).DynamicVars.Damage).UpgradeValueBy(4m);
		}
	}
	public sealed class Endless : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/silent/endless.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<EndlessPower>((int?)null) };

		public Endless()
			: base(1, (CardType)3, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
			await PowerCmd.Apply<EndlessPower>(choiceContext, ((CardModel)this).Owner.Creature, 1m, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).EnergyCost.UpgradeBy(-1);
			((CardModel)this).EnergyCost.FinalizeUpgrade();
		}
	}
	public sealed class ExplosiveSprayPaint : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/explosive_spray_paint.png";

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			(DynamicVar)new DamageVar(7m, (ValueProp)8),
			(DynamicVar)new RepeatVar(2)
		};

		public ExplosiveSprayPaint()
			: base(1, (CardType)1, (CardRarity)5, (TargetType)0, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot($"res://wuwancients/audio/explosive_spray_paint_{Random.Shared.Next(1, 4)}.wav", 1f);
			}
			Player owner = ((CardModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				Creature creature = owner.Creature;
				obj = ((creature != null) ? creature.CombatState : null);
			}
			ICombatState combatState = (ICombatState)obj;
			if (combatState != null && ((CardModel)this).Owner != null)
			{
				for (int i = 0; i < ((DynamicVar)((CardModel)this).DynamicVars.Repeat).IntValue; i++)
				{
					await DamageCmd.Attack(((CardModel)this).DynamicVars["Damage"].BaseValue).FromCard((CardModel)(object)this, cardPlay).TargetingAllOpponents(combatState)
						.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
						.Execute(choiceContext);
				}
				CardSelectorPrefs val = new CardSelectorPrefs(((CardModel)this).SelectionScreenPrompt, 1);
				CardModel selection = (await CardSelectCmd.FromHand(choiceContext, ((CardModel)this).Owner, val, (Func<CardModel, bool>)null, (AbstractModel)(object)this)).FirstOrDefault();
				if (selection != null)
				{
					CardModel copy = selection.CreateClone();
					copy.EnergyCost.SetCustomBaseCost(0);
					await CardPileCmd.AddGeneratedCardToCombat(copy, (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
				}
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).RemoveKeyword((CardKeyword)1);
		}
	}
	public sealed class ForeseeFuture : CardModel
	{
		protected override bool HasEnergyCostX => true;

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/silent/foresee_future.png";

		public ForeseeFuture()
			: base(0, (CardType)2, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			int x = ((CardModel)this).ResolveEnergyXValue();
			if (x > 0)
			{
				await PlayerCmd.GainEnergy((decimal)(2 * x), ((CardModel)this).Owner);
				await CardPileCmd.Draw(choiceContext, (decimal)(2 * x), ((CardModel)this).Owner, false);
				await PowerCmd.Apply<ExhaustedMindPower>(choiceContext, ((CardModel)this).Owner.Creature, (decimal)x, ((CardModel)this).Owner.Creature, (CardModel)null, false);
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)5);
		}
	}
	public sealed class FuryUnleashed : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/ironclad/fury_unleashed.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			(DynamicVar)new PowerVar<FuryPower>(3m),
			(DynamicVar)new PowerVar<NoBlockForeverPower>(1m)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
		{
			HoverTipFactory.FromPower<FuryPower>((int?)null),
			HoverTipFactory.FromPower<NoBlockForeverPower>((int?)null)
		};

		public FuryUnleashed()
			: base(1, (CardType)3, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
			await PowerCmd.Apply<FuryPower>(choiceContext, ((CardModel)this).Owner.Creature, ((CardModel)this).DynamicVars["FuryPower"].BaseValue, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
			await PowerCmd.Apply<NoBlockForeverPower>(choiceContext, ((CardModel)this).Owner.Creature, ((CardModel)this).DynamicVars["NoBlockForeverPower"].BaseValue, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).DynamicVars["FuryPower"].UpgradeValueBy(1m);
		}
	}
	public sealed class Fuxie : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/fuxie.png";

		public override int MaxUpgradeLevel => 0;

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)4 };

		public Fuxie()
			: base(-2, (CardType)5, (CardRarity)9, (TargetType)0, true)
		{
		}

		public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
		{
			if (card != this)
			{
				return;
			}
			Player owner = ((CardModel)this).Owner;
			if (((owner != null) ? owner.PlayerCombatState : null) != null)
			{
				List<CardModel> handCards = owner.PlayerCombatState.Hand.Cards.ToList();
				if (handCards.Count != 0)
				{
					await CardCmd.Discard(choiceContext, (IEnumerable<CardModel>)handCards);
				}
			}
		}
	}
	public sealed class Hunt : CardModel
	{
		private const string VoicePrefix = "res://wuwancients/audio/hunt_";

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/hunt.png";

		public override int MaxUpgradeLevel => 1;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
		{
			(DynamicVar)new DamageVar(18m, (ValueProp)8),
			(DynamicVar)new EnergyVar(2),
			(DynamicVar)new CardsVar(2)
		};

		public Hunt()
			: base(1, (CardType)1, (CardRarity)5, (TargetType)2, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			Player owner = ((CardModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				Creature creature = owner.Creature;
				obj = ((creature != null) ? creature.CombatState : null);
			}
			ICombatState combatState = (ICombatState)obj;
			if (combatState != null && cardPlay.Target != null)
			{
				bool killed = (await DamageCmd.Attack(((DynamicVar)((CardModel)this).DynamicVars.Damage).BaseValue).FromCard((CardModel)(object)this, cardPlay).Targeting(cardPlay.Target)
					.Execute(choiceContext)).Results.SelectMany((List<DamageResult> r) => r).Any((DamageResult r) => r.WasTargetKilled);
				string voice = ((!killed) ? ("res://wuwancients/audio/hunt_" + Random.Shared.Next(3, 6) + ".wav") : (combatState.HittableEnemies.Any((Creature c) => c.IsAlive) ? "res://wuwancients/audio/hunt_1.wav" : "res://wuwancients/audio/hunt_2.wav"));
				NAudioManager instance = NAudioManager.Instance;
				if (instance != null)
				{
					instance.PlayOneShot(voice, 1f);
				}
				if (killed)
				{
					await PlayerCmd.GainEnergy((decimal)((DynamicVar)((CardModel)this).DynamicVars.Energy).IntValue, ((CardModel)this).Owner);
					await CardPileCmd.Draw(choiceContext, (decimal)((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue, ((CardModel)this).Owner, false);
				}
			}
		}

		protected override void OnUpgrade()
		{
			((DynamicVar)((CardModel)this).DynamicVars.Damage).UpgradeValueBy(6m);
		}
	}
	public sealed class ImmortalPurge : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/immortal_purge.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new DamageVar(30m, (ValueProp)8) };

		public override IEnumerable<CardKeyword> CanonicalKeywords => new <>z__ReadOnlySingleElementList<CardKeyword>((CardKeyword)1);

		protected override bool IsPlayable
		{
			get
			{
				Player owner = ((CardModel)this).Owner;
				Creature val = ((owner != null) ? owner.Creature : null);
				if (val == null)
				{
					return false;
				}
				Majesty power = val.GetPower<Majesty>();
				return power != null && ((PowerModel)power).Amount >= 9;
			}
		}

		public override int MaxUpgradeLevel => 0;

		public ImmortalPurge()
			: base(0, (CardType)2, (CardRarity)5, (TargetType)0, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			Player owner = ((CardModel)this).Owner;
			Creature creature = ((owner != null) ? owner.Creature : null);
			if (creature == null)
			{
				return;
			}
			ICombatState combatState = creature.CombatState;
			if (combatState != null)
			{
				NAudioManager instance = NAudioManager.Instance;
				if (instance != null)
				{
					instance.PlayOneShot($"res://wuwancients/audio/immortal_purge_{Random.Shared.Next(1, 4)}.wav", 1f);
				}
				IReadOnlyList<Creature> enemies = combatState.HittableEnemies;
				if (enemies != null && enemies.Count > 0)
				{
					await CreatureCmd.Damage(choiceContext, (IEnumerable<Creature>)enemies.ToList(), ((CardModel)this).DynamicVars["Damage"].BaseValue, (ValueProp)8, creature);
				}
				Majesty majesty = creature.GetPower<Majesty>();
				if (majesty != null)
				{
					await PowerCmd.Remove((PowerModel)(object)majesty);
				}
				ProstrationMoment prostration = creature.GetPower<ProstrationMoment>();
				if (prostration != null)
				{
					await PowerCmd.Remove((PowerModel)(object)prostration);
				}
			}
		}
	}
	public class IntelligentCreation : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/defect/intelligent_creation.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new PowerVar<ArtifactPower>("Artifact", 1m) };

		public IntelligentCreation()
			: base(1, (CardType)3, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			await PowerCmd.Apply<ArtifactPower>(choiceContext, ((CardModel)this).Owner.Creature, ((CardModel)this).DynamicVars["Artifact"].BaseValue, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).DynamicVars["Artifact"].UpgradeValueBy(1m);
		}
	}
	public sealed class JieXian : CardModel
	{
		private sealed class PowerAmountVar : DynamicVar
		{
			public PowerAmountVar(decimal baseValue)
				: base("PowerAmount", baseValue)
			{
			}
		}

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/jiexian.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
		{
			(DynamicVar)new DamageVar(2m, (ValueProp)8),
			new PowerAmountVar(2m),
			(DynamicVar)new RepeatVar(2)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<XieXianPower>((int?)((CardModel)this).DynamicVars["PowerAmount"].IntValue) };

		public JieXian()
			: base(1, (CardType)1, (CardRarity)5, (TargetType)2, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			int num = Random.Shared.Next(3);
			if (1 == 0)
			{
			}
			string text = num switch
			{
				0 => "res://wuwancients/audio/jiexian_1.wav", 
				1 => "res://wuwancients/audio/jiexian_2.wav", 
				_ => "res://wuwancients/audio/jiexian_3.wav", 
			};
			if (1 == 0)
			{
			}
			string audioPath = text;
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot(audioPath, 1f);
			}
			for (int i = 0; i < ((DynamicVar)((CardModel)this).DynamicVars.Repeat).IntValue; i++)
			{
				await DamageCmd.Attack(((CardModel)this).DynamicVars["Damage"].BaseValue).FromCard((CardModel)(object)this, cardPlay).Targeting(cardPlay.Target)
					.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
					.Execute(choiceContext);
			}
			decimal amount = ((CardModel)this).DynamicVars["PowerAmount"].IntValue;
			await PowerCmd.Apply<XieXianPower>(choiceContext, cardPlay.Target, amount, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).DynamicVars["Damage"].UpgradeValueBy(1m);
			((CardModel)this).DynamicVars["PowerAmount"].UpgradeValueBy(1m);
		}
	}
	public sealed class Kuangsha : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/kuangsha.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<Sandpit1Power>((int?)1) };

		public Kuangsha()
			: base(1, (CardType)4, (CardRarity)8, (TargetType)0, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			Player owner2 = ((CardModel)this).Owner;
			Creature owner = ((owner2 != null) ? owner2.Creature : null);
			if (owner != null)
			{
				await PowerCmd.Apply<Sandpit1Power>(choiceContext, owner, 1m, owner, (CardModel)(object)this, false);
			}
		}
	}
	public sealed class MassEnergyEquivalence : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/mass_energy_equivalence.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
		{
			HoverTipFactory.FromPower<StrengthPower>((int?)null),
			HoverTipFactory.FromPower<DexterityPower>((int?)null)
		};

		public MassEnergyEquivalence()
			: base(1, (CardType)3, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				int num = Random.Shared.Next(3);
				if (1 == 0)
				{
				}
				string text = num switch
				{
					0 => "res://wuwancients/audio/mass_energy_equivalence_1.wav", 
					1 => "res://wuwancients/audio/mass_energy_equivalence_2.wav", 
					_ => "res://wuwancients/audio/mass_energy_equivalence_3.wav", 
				};
				if (1 == 0)
				{
				}
				instance.PlayOneShot(text, 1f);
			}
			await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
			await PowerCmd.Apply<MassEnergyEquivalencePower>(choiceContext, ((CardModel)this).Owner.Creature, 1m, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)3);
		}
	}
	public sealed class MassGrave : CardModel
	{
		public const string SelfDoomKey = "SelfDoom";

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/mass_grave.png";

		public override int MaxUpgradeLevel => 1;

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			new DynamicVar("SelfDoom", 3m),
			(DynamicVar)new PowerVar<DoomPower>(10m)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<DoomPower>((int?)null) };

		public MassGrave()
			: base(1, (CardType)3, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot($"res://wuwancients/audio/mass_grave_{Random.Shared.Next(1, 4)}.wav", 1f);
			}
			Player owner = ((CardModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				await PowerCmd.Apply<MassGravePower>(choiceContext, ((CardModel)this).Owner.Creature, 1m, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
				MassGravePower power = ((CardModel)this).Owner.Creature.GetPower<MassGravePower>();
				if (power != null)
				{
					power.SelfDoom = (int)((CardModel)this).DynamicVars["SelfDoom"].BaseValue;
					power.EnemyDoom = (int)((CardModel)this).DynamicVars["DoomPower"].BaseValue;
				}
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).DynamicVars["SelfDoom"].UpgradeValueBy(-2m);
		}
	}
	public sealed class MoonPhaseFlow : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/moon_phase_flow.png";

		public override int MaxUpgradeLevel => 1;

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
		{
			HoverTipFactory.FromPower<FullMoonFieldPower>((int?)1),
			HoverTipFactory.FromPower<PaleDeathBlessingPower>((int?)1)
		};

		public MoonPhaseFlow()
			: base(1, (CardType)3, (CardRarity)5, (TargetType)0, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot($"res://wuwancients/audio/moon_phase_flow_{Random.Shared.Next(1, 4)}.wav", 1f);
			}
			Player owner = ((CardModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				Creature creature = owner.Creature;
				if (creature == null)
				{
					obj = null;
				}
				else
				{
					ICombatState combatState = creature.CombatState;
					obj = ((combatState != null) ? combatState.Players : null);
				}
			}
			if (obj == null)
			{
				return;
			}
			foreach (Player player in ((CardModel)this).Owner.Creature.CombatState.Players)
			{
				if (((player != null) ? player.Creature : null) != null)
				{
					await PowerCmd.Apply<FullMoonFieldPower>(choiceContext, player.Creature, 1m, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
				}
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)3);
		}
	}
	public sealed class NeuralNetwork : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/defect/neural_network.png";

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<NeuralNetworkPower>((int?)null) };

		public NeuralNetwork()
			: base(1, (CardType)3, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
			await PowerCmd.Apply<NeuralNetworkPower>(choiceContext, ((CardModel)this).Owner.Creature, 1m, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)3);
		}
	}
	public sealed class NewWaveEra : CardModel
	{
		private static readonly string[] SfxPool = new string[6] { "res://wuwancients/audio/new_wave_era_1.wav", "res://wuwancients/audio/new_wave_era_2.wav", "res://wuwancients/audio/new_wave_era_3.wav", "res://wuwancients/audio/lethal_end_1.wav", "res://wuwancients/audio/lethal_end_2.wav", "res://wuwancients/audio/lethal_end_3.wav" };

		private const string HitFx = "vfx/vfx_attack_slash";

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/new_wave_era.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
		{
			(DynamicVar)new DamageVar("Damage", 6m, (ValueProp)8),
			(DynamicVar)new DamageVar("Damage2", 10m, (ValueProp)8),
			(DynamicVar)new IntVar("Repeat", 4m)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<FreezePower>((int?)null) };

		public override int MaxUpgradeLevel => 1;

		public NewWaveEra()
			: base(1, (CardType)1, (CardRarity)5, (TargetType)0, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
		{
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot(SfxPool[Random.Shared.Next(SfxPool.Length)], 1f);
			}
			Player owner = ((CardModel)this).Owner;
			Creature me = ((owner != null) ? owner.Creature : null);
			ICombatState cs = ((CardModel)this).CombatState;
			if (me == null || cs == null)
			{
				return;
			}
			int hits = ((CardModel)this).DynamicVars["Repeat"].IntValue;
			for (int i = 0; i < hits; i++)
			{
				Player owner2 = ((CardModel)this).Owner;
				Creature target = ((owner2 != null) ? owner2.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)cs.HittableEnemies) : null);
				if (target == null)
				{
					break;
				}
				await DamageCmd.Attack(((CardModel)this).DynamicVars["Damage"].BaseValue).FromCard((CardModel)(object)this, cardPlay).Targeting(target)
					.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
					.Execute(ctx);
				await PowerCmd.Apply<FreezePower>(ctx, target, 1m, me, (CardModel)(object)this, false);
			}
			if (cs.HittableEnemies.Count == 0)
			{
				return;
			}
			await DamageCmd.Attack(((CardModel)this).DynamicVars["Damage2"].BaseValue).FromCard((CardModel)(object)this, cardPlay).TargetingAllOpponents(cs)
				.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
				.Execute(ctx);
			foreach (Creature enemy in cs.HittableEnemies.ToList())
			{
				await PowerCmd.Apply<FreezePower>(ctx, enemy, 1m, me, (CardModel)(object)this, false);
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).DynamicVars["Damage"].UpgradeValueBy(2m);
			((CardModel)this).DynamicVars["Damage2"].UpgradeValueBy(4m);
		}
	}
	public sealed class OneDayFlower : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/one_day_flower.png";

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			(DynamicVar)new DamageVar(15m, (ValueProp)8),
			(DynamicVar)new DamageVar("SelfDamage", 1m, (ValueProp)4)
		};

		public OneDayFlower()
			: base(0, (CardType)1, (CardRarity)5, (TargetType)3, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			int num = Random.Shared.Next(3);
			if (1 == 0)
			{
			}
			string text = num switch
			{
				0 => "res://wuwancients/audio/one_day_flower_1.wav", 
				1 => "res://wuwancients/audio/one_day_flower_2.wav", 
				_ => "res://wuwancients/audio/one_day_flower_3.wav", 
			};
			if (1 == 0)
			{
			}
			string audioPath = text;
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot(audioPath, 1f);
			}
			await DamageCmd.Attack(((CardModel)this).DynamicVars["Damage"].BaseValue).FromCard((CardModel)(object)this, cardPlay).TargetingAllOpponents(((CardModel)this).CombatState)
				.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
				.Execute(choiceContext);
			DamageVar selfDamageVar = (DamageVar)((CardModel)this).DynamicVars["SelfDamage"];
			Player owner = ((CardModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				Creature creature = owner.Creature;
				if (creature == null)
				{
					obj = null;
				}
				else
				{
					ICombatState combatState = creature.CombatState;
					obj = ((combatState != null) ? combatState.Players : null);
				}
			}
			if (obj == null)
			{
				return;
			}
			foreach (Player player in ((CardModel)this).Owner.Creature.CombatState.Players)
			{
				if (((player != null) ? player.Creature : null) != null)
				{
					await CreatureCmd.Damage(choiceContext, player.Creature, selfDamageVar, (CardModel)(object)this, cardPlay);
				}
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).DynamicVars["Damage"].UpgradeValueBy(5m);
		}
	}
	public sealed class Overlimit : CardModel
	{
		private const decimal BaseDamage = 9m;

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/overlimit.png";

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new DamageVar(9m, (ValueProp)8) };

		public Overlimit()
			: base(1, (CardType)1, (CardRarity)5, (TargetType)2, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot($"res://wuwancients/audio/overlimit_{Random.Shared.Next(1, 5)}.wav", 1f);
			}
			Player owner2 = ((CardModel)this).Owner;
			Creature owner = ((owner2 != null) ? owner2.Creature : null);
			if (owner != null)
			{
				Creature target = cardPlay.Target;
				OverlimitKillCredit.Arm(((CardModel)this).Owner, target);
				await ApplyCardForcesOnPlay(choiceContext, cardPlay, owner);
				await DamageCmd.Attack(((CardModel)this).DynamicVars["Damage"].BaseValue).FromCard((CardModel)(object)this, cardPlay).Targeting(cardPlay.Target)
					.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
					.Execute(choiceContext);
				if (target != null && !target.IsAlive)
				{
					await ApplyCardForcesOnKill(choiceContext, owner);
				}
				OverlimitKillCredit.Clear();
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)5);
		}

		private async Task ApplyCardForcesOnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, Creature owner)
		{
			ChimerasHeart heart = ChimerasHeart.Of(((CardModel)this).Owner);
			if (heart == null)
			{
				return;
			}
			ICombatState combatState = owner.CombatState;
			IReadOnlyList<Creature> enemies = ((combatState != null) ? combatState.HittableEnemies : null) ?? Array.Empty<Creature>();
			Creature target = cardPlay.Target;
			if (heart.HasForce(DemonForceId.Chomper))
			{
				foreach (Creature enemy3 in enemies)
				{
					await PowerCmd.Apply<StrengthPower>(choiceContext, enemy3, -8m, owner, (CardModel)(object)this, false);
				}
			}
			if (heart.HasForce(DemonForceId.Tunneler))
			{
				await CreatureCmd.GainBlock(owner, 32m, (ValueProp)8, cardPlay, false);
			}
			if (heart.HasForce(DemonForceId.Myte) && target != null && target.IsAlive)
			{
				await PowerCmd.Apply<PoisonPower>(choiceContext, target, 10m, owner, (CardModel)(object)this, false);
			}
			if (heart.HasForce(DemonForceId.SlimedBerserker))
			{
				foreach (Creature enemy2 in enemies)
				{
					await PowerCmd.Apply<WeakPower>(choiceContext, enemy2, 3m, owner, (CardModel)(object)this, false);
				}
			}
			if (heart.HasForce(DemonForceId.TheLost) && target != null && target.IsAlive)
			{
				await PowerCmd.Apply<StrengthPower>(choiceContext, target, -2m, owner, (CardModel)(object)this, false);
			}
			if (heart.HasForce(DemonForceId.TheForgotten) && target != null && target.IsAlive)
			{
				await PowerCmd.Apply<DexterityPower>(choiceContext, target, -2m, owner, (CardModel)(object)this, false);
			}
			if (heart.HasForce(DemonForceId.BowlbugRock))
			{
				await CreatureCmd.GainBlock(owner, 16m, (ValueProp)8, cardPlay, false);
			}
			if (!heart.HasForce(DemonForceId.BowlbugSilk))
			{
				return;
			}
			foreach (Creature enemy in enemies)
			{
				await PowerCmd.Apply<WeakPower>(choiceContext, enemy, 2m, owner, (CardModel)(object)this, false);
			}
			if (target != null && target.IsAlive)
			{
				await CreatureCmd.Damage(choiceContext, target, 9m, (ValueProp)8, (CardModel)(object)this, cardPlay);
			}
		}

		private async Task ApplyCardForcesOnKill(PlayerChoiceContext choiceContext, Creature owner)
		{
			ChimerasHeart heart = ChimerasHeart.Of(((CardModel)this).Owner);
			if (heart == null)
			{
				return;
			}
			if (heart.HasForce(DemonForceId.ScrollOfBiting))
			{
				await CreatureCmd.GainMaxHp(owner, 2m);
			}
			if (heart.HasForce(DemonForceId.ThievingHopper) && ((CardModel)this).Owner != null)
			{
				AbstractRoom currentRoom = ((CardModel)this).Owner.RunState.CurrentRoom;
				CombatRoom room = (CombatRoom)(object)((currentRoom is CombatRoom) ? currentRoom : null);
				if (room != null)
				{
					room.AddExtraReward(((CardModel)this).Owner, (Reward)new CardReward(CardCreationOptions.ForRoom(((CardModel)this).Owner, ((AbstractRoom)room).RoomType), 3, ((CardModel)this).Owner, (PlayerChoiceSynchronizer)null));
				}
			}
		}
	}
	public sealed class Overtime : CardModel
	{
		private const int MaxAutoPlays = 100;

		private static readonly string[] SfxPool = new string[3] { "res://wuwancients/audio/overtime_1.wav", "res://wuwancients/audio/overtime_2.wav", "res://wuwancients/audio/overtime_3.wav" };

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/overtime.png";

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

		public Overtime()
			: base(3, (CardType)2, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot(SfxPool[Random.Shared.Next(SfxPool.Length)], 1f);
			}
			Player player = ((CardModel)this).Owner;
			if (((player != null) ? player.PlayerCombatState : null) == null)
			{
				return;
			}
			CardPile hand = PileTypeExtensions.GetPile((PileType)2, player);
			if (hand != null)
			{
				int missing = CardPile.MaxCardsInHand - hand.Cards.Count;
				if (missing > 0)
				{
					await CardPileCmd.Draw(choiceContext, (decimal)missing, player, false);
				}
			}
			for (int i = 0; i < 100; i++)
			{
				if (player.PlayerCombatState == null)
				{
					break;
				}
				CardPile currentHand = PileTypeExtensions.GetPile((PileType)2, player);
				CardModel next = ((currentHand != null) ? currentHand.Cards.FirstOrDefault() : null);
				if (next == null)
				{
					break;
				}
				await CardCmd.AutoPlay(choiceContext, next, (Creature)null, (AutoPlayType)1, true, false);
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)3);
		}
	}
	public class PredestinedDeath : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/necrobinder/predestined_death.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public PredestinedDeath()
			: base(1, (CardType)3, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
			await PowerCmd.Apply<PredestinedDeathPower>(choiceContext, ((CardModel)this).Owner.Creature, 1m, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)3);
		}
	}
	public sealed class Resignation : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/ironclad/resignation.png";

		public Resignation()
			: base(1, (CardType)3, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
			await PowerCmd.Apply<ResignationPower>(choiceContext, ((CardModel)this).Owner.Creature, 1m, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)3);
		}
	}
	public sealed class Revelation1 : CardModel
	{
		private const string _regenKey = "Regen";

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/revelation1.png";

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1]
		{
			new DynamicVar("Regen", 7m)
		};

		public Revelation1()
			: base(1, (CardType)2, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			int num = Random.Shared.Next(3);
			if (1 == 0)
			{
			}
			string text = num switch
			{
				0 => "res://wuwancients/audio/revelation1_1.wav", 
				1 => "res://wuwancients/audio/revelation1_2.wav", 
				_ => "res://wuwancients/audio/revelation1_3.wav", 
			};
			if (1 == 0)
			{
			}
			string audioPath = text;
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot(audioPath, 1f);
			}
			Player owner = ((CardModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				Creature creature = owner.Creature;
				obj = ((creature != null) ? creature.CombatState : null);
			}
			if (obj == null)
			{
				return;
			}
			ICombatState combatState = ((CardModel)this).Owner.Creature.CombatState;
			IReadOnlyList<Player> players = combatState.Players;
			if (players == null)
			{
				return;
			}
			await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
			foreach (Player player in players)
			{
				if (((player != null) ? player.Creature : null) != null)
				{
					await PowerCmd.Apply<RegenPower>(choiceContext, player.Creature, ((CardModel)this).DynamicVars["Regen"].BaseValue, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
				}
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).DynamicVars["Regen"].UpgradeValueBy(3m);
		}
	}
	public sealed class RoyalLove : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/regent/royal_love.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new StarsVar(1) };

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<RoyalLovePower>((int?)null) };

		public RoyalLove()
			: base(1, (CardType)3, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
			await PowerCmd.Apply<RoyalLovePower>(choiceContext, ((CardModel)this).Owner.Creature, 1m, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)3);
		}
	}
	public sealed class RunePower : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/rune_power.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[4]
		{
			new DynamicVar("DrawCount", 5m),
			(DynamicVar)new DamageVar("AttackDamage", 6m, (ValueProp)8),
			(DynamicVar)new BlockVar(6m, (ValueProp)8),
			new DynamicVar("Energy", 2m)
		};

		public RunePower()
			: base(2, (CardType)2, (CardRarity)5, (TargetType)0, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				int num = Random.Shared.Next(3);
				if (1 == 0)
				{
				}
				string text = num switch
				{
					0 => "res://wuwancients/audio/rune_power_1.wav", 
					1 => "res://wuwancients/audio/rune_power_2.wav", 
					_ => "res://wuwancients/audio/rune_power_3.wav", 
				};
				if (1 == 0)
				{
				}
				instance.PlayOneShot(text, 1f);
			}
			int drawCount = (int)((CardModel)this).DynamicVars["DrawCount"].BaseValue;
			foreach (CardModel card in await CardPileCmd.Draw(choiceContext, (decimal)drawCount, ((CardModel)this).Owner, false))
			{
				CardType type = card.Type;
				CardType val = type;
				switch (val - 1)
				{
				case 0:
				{
					Creature creature = ((CardModel)this).Owner.Creature;
					object obj;
					if (creature == null)
					{
						obj = null;
					}
					else
					{
						ICombatState combatState = creature.CombatState;
						obj = ((combatState != null) ? combatState.HittableEnemies : null);
					}
					IReadOnlyList<Creature> enemies = (IReadOnlyList<Creature>)obj;
					if (enemies != null && enemies.Count > 0)
					{
						await CreatureCmd.Damage(choiceContext, (IEnumerable<Creature>)enemies.ToList(), ((CardModel)this).DynamicVars["AttackDamage"].BaseValue, (ValueProp)8, ((CardModel)this).Owner.Creature);
					}
					break;
				}
				case 1:
					await CreatureCmd.GainBlock(((CardModel)this).Owner.Creature, ((CardModel)this).DynamicVars.Block, (CardPlay)null, false);
					break;
				case 2:
					await PlayerCmd.GainEnergy(((CardModel)this).DynamicVars["Energy"].BaseValue, ((CardModel)this).Owner);
					break;
				}
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)3);
		}
	}
	public sealed class SecondForm : CardModel
	{
		[CompilerGenerated]
		private sealed class <get_CanonicalVars>d__9 : IEnumerable<DynamicVar>, IEnumerable, IEnumerator<DynamicVar>, IEnumerator, IDisposable
		{
			private int <>1__state;

			private DynamicVar <>2__current;

			private int <>l__initialThreadId;

			public SecondForm <>4__this;

			DynamicVar IEnumerator<DynamicVar>.Current
			{
				[DebuggerHidden]
				get
				{
					return <>2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return <>2__current;
				}
			}

			[DebuggerHidden]
			public <get_CanonicalVars>d__9(int <>1__state)
			{
				this.<>1__state = <>1__state;
				<>l__initialThreadId = Environment.CurrentManagedThreadId;
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
				<>1__state = -2;
			}

			private bool MoveNext()
			{
				//IL_002b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0035: Expected O, but got Unknown
				switch (<>1__state)
				{
				default:
					return false;
				case 0:
					<>1__state = -1;
					<>2__current = new DynamicVar("OverclockAmount", 1m);
					<>1__state = 1;
					return true;
				case 1:
					<>1__state = -1;
					return false;
				}
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
				throw new NotSupportedException();
			}

			[DebuggerHidden]
			IEnumerator<DynamicVar> IEnumerable<DynamicVar>.GetEnumerator()
			{
				<get_CanonicalVars>d__9 result;
				if (<>1__state == -2 && <>l__initialThreadId == Environment.CurrentManagedThreadId)
				{
					<>1__state = 0;
					result = this;
				}
				else
				{
					result = new <get_CanonicalVars>d__9(0)
					{
						<>4__this = <>4__this
					};
				}
				return result;
			}

			[DebuggerHidden]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<DynamicVar>)this).GetEnumerator();
			}
		}

		[CompilerGenerated]
		private sealed class <get_ExtraHoverTips>d__7 : IEnumerable<IHoverTip>, IEnumerable, IEnumerator<IHoverTip>, IEnumerator, IDisposable
		{
			private int <>1__state;

			private IHoverTip <>2__current;

			private int <>l__initialThreadId;

			public SecondForm <>4__this;

			IHoverTip IEnumerator<IHoverTip>.Current
			{
				[DebuggerHidden]
				get
				{
					return <>2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return <>2__current;
				}
			}

			[DebuggerHidden]
			public <get_ExtraHoverTips>d__7(int <>1__state)
			{
				this.<>1__state = <>1__state;
				<>l__initialThreadId = Environment.CurrentManagedThreadId;
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
				<>1__state = -2;
			}

			private bool MoveNext()
			{
				switch (<>1__state)
				{
				default:
					return false;
				case 0:
					<>1__state = -1;
					<>2__current = HoverTipFactory.FromPower<OverclockPower>((int?)((CardModel)<>4__this).DynamicVars["OverclockAmount"].IntValue);
					<>1__state = 1;
					return true;
				case 1:
					<>1__state = -1;
					return false;
				}
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
				throw new NotSupportedException();
			}

			[DebuggerHidden]
			IEnumerator<IHoverTip> IEnumerable<IHoverTip>.GetEnumerator()
			{
				<get_ExtraHoverTips>d__7 result;
				if (<>1__state == -2 && <>l__initialThreadId == Environment.CurrentManagedThreadId)
				{
					<>1__state = 0;
					result = this;
				}
				else
				{
					result = new <get_ExtraHoverTips>d__7(0)
					{
						<>4__this = <>4__this
					};
				}
				return result;
			}

			[DebuggerHidden]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<IHoverTip>)this).GetEnumerator();
			}
		}

		private const string OverclockKey = "OverclockAmount";

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/second_form.png";

		public override int MaxUpgradeLevel => 1;

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			[IteratorStateMachine(typeof(<get_ExtraHoverTips>d__7))]
			get
			{
				//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
				<get_ExtraHoverTips>d__7 <get_ExtraHoverTips>d__ = new <get_ExtraHoverTips>d__7(-2);
				<get_ExtraHoverTips>d__.<>4__this = this;
				return <get_ExtraHoverTips>d__;
			}
		}

		protected override IEnumerable<DynamicVar> CanonicalVars
		{
			[IteratorStateMachine(typeof(<get_CanonicalVars>d__9))]
			get
			{
				//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
				<get_CanonicalVars>d__9 <get_CanonicalVars>d__ = new <get_CanonicalVars>d__9(-2);
				<get_CanonicalVars>d__.<>4__this = this;
				return <get_CanonicalVars>d__;
			}
		}

		public SecondForm()
			: base(1, (CardType)3, (CardRarity)5, (TargetType)0, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			int num = Random.Shared.Next(3);
			if (1 == 0)
			{
			}
			string text = num switch
			{
				0 => "res://wuwancients/audio/second_form_1.wav", 
				1 => "res://wuwancients/audio/second_form_2.wav", 
				_ => "res://wuwancients/audio/second_form_3.wav", 
			};
			if (1 == 0)
			{
			}
			string audioPath = text;
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot(audioPath, 1f);
			}
			Player owner = ((CardModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				int amount = ((CardModel)this).DynamicVars["OverclockAmount"].IntValue;
				await PowerCmd.Apply<OverclockPower>(choiceContext, ((CardModel)this).Owner.Creature, (decimal)amount, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).DynamicVars["OverclockAmount"].UpgradeValueBy(1m);
		}
	}
	public sealed class SolsticeCrusade : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/solstice_crusade.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		public SolsticeCrusade()
			: base(1, (CardType)3, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			Player player = ((CardModel)this).Owner;
			if (((player != null) ? player.Creature : null) == null)
			{
				return;
			}
			Creature creature = player.Creature;
			ICombatState combatState = creature.CombatState;
			if (combatState == null)
			{
				return;
			}
			PlayerCombatState playerCombatState = player.PlayerCombatState;
			object obj;
			if (playerCombatState == null)
			{
				obj = null;
			}
			else
			{
				CardPile hand = playerCombatState.Hand;
				obj = ((hand != null) ? hand.Cards.ToList() : null);
			}
			List<CardModel> handCards = (List<CardModel>)obj;
			if (handCards != null && handCards.Count > 0)
			{
				await CardCmd.Discard(choiceContext, (IEnumerable<CardModel>)handCards);
			}
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot($"res://wuwancients/audio/solstice_crusade_{Random.Shared.Next(1, 4)}.wav", 1f);
			}
			await CreatureCmd.TriggerAnim(creature, "Cast", player.Character.CastAnimDelay);
			await PowerCmd.Apply<IntangiblePower>(choiceContext, creature, 1m, creature, (CardModel)(object)this, false);
			await PowerCmd.Apply<BufferPower>(choiceContext, creature, 1m, creature, (CardModel)(object)this, false);
			IReadOnlyList<Creature> allies = combatState.GetTeammatesOf(creature);
			if (allies != null)
			{
				foreach (Creature ally in allies)
				{
					if (ally != creature && ally.IsAlive)
					{
						await PowerCmd.Apply<KingsDomain>(choiceContext, ally, 1m, creature, (CardModel)(object)this, false);
					}
				}
			}
			await PowerCmd.Apply<ProstrationMoment>(choiceContext, creature, 1m, creature, (CardModel)(object)this, false);
			for (int i = 0; i < 9; i++)
			{
				BlazingSun sun = combatState.CreateCard<BlazingSun>(player);
				await CardPileCmd.Add((CardModel)(object)sun, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
			}
			ImmortalPurge purge = combatState.CreateCard<ImmortalPurge>(player);
			await CardPileCmd.Add((CardModel)(object)purge, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)5);
		}
	}
	public sealed class StarDomain : CardModel
	{
		private const string _healKey = "Heal";

		private const string _damageKey = "Damage";

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/star_domain.png";

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
		{
			new DynamicVar("Heal", 6m),
			(DynamicVar)new DamageVar("Damage", 6m, (ValueProp)8)
		};

		public StarDomain()
			: base(1, (CardType)1, (CardRarity)5, (TargetType)0, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			int num = Random.Shared.Next(3);
			if (1 == 0)
			{
			}
			string text = num switch
			{
				0 => "res://wuwancients/audio/star_domain_1.wav", 
				1 => "res://wuwancients/audio/star_domain_2.wav", 
				_ => "res://wuwancients/audio/star_domain_3.wav", 
			};
			if (1 == 0)
			{
			}
			string audioPath = text;
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot(audioPath, 1f);
			}
			Player owner = ((CardModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				Creature creature = owner.Creature;
				obj = ((creature != null) ? creature.CombatState : null);
			}
			if (obj == null)
			{
				return;
			}
			ICombatState combatState = ((CardModel)this).Owner.Creature.CombatState;
			IReadOnlyList<Player> players = combatState.Players;
			if (players == null)
			{
				return;
			}
			int healAmt = (int)((CardModel)this).DynamicVars["Heal"].BaseValue;
			int dmgAmt = (int)((CardModel)this).DynamicVars["Damage"].BaseValue;
			foreach (Player player in players)
			{
				if (((player != null) ? player.Creature : null) != null)
				{
					await CreatureCmd.Heal(player.Creature, (decimal)healAmt, true);
				}
			}
			await DamageCmd.Attack((decimal)dmgAmt).FromCard((CardModel)(object)this, cardPlay).TargetingAllOpponents(combatState)
				.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
				.Execute(choiceContext);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).DynamicVars["Heal"].UpgradeValueBy(3m);
			((CardModel)this).DynamicVars["Damage"].UpgradeValueBy(3m);
		}
	}
	public sealed class SuisuiGold : CardModel
	{
		private const decimal BaseRegen = 1m;

		private const decimal UpgradeRegen = 2m;

		private const decimal CardsPerTrigger = 3m;

		private const decimal AllyHealPercent = 50m;

		private static readonly string[] SfxPool = new string[3] { "res://wuwancients/audio/suisui_gold_1.wav", "res://wuwancients/audio/suisui_gold_2.wav", "res://wuwancients/audio/suisui_gold_3.wav" };

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/suisui_gold.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
		{
			new DynamicVar("Regen", 1m),
			new DynamicVar("PerCards", 3m),
			new DynamicVar("AllyHeal", 50m)
		};

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
		{
			HoverTipFactory.FromPower<SuisuiGoldPower>((int?)null),
			HoverTipFactory.FromPower<RegenPower>((int?)null)
		};

		public SuisuiGold()
			: base(1, (CardType)3, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			SfxCmd.Play(SfxPool[Random.Shared.Next(SfxPool.Length)], 1f);
			Player owner = ((CardModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				await PowerCmd.Apply<SuisuiGoldPower>(choiceContext, ((CardModel)this).Owner.Creature, ((CardModel)this).DynamicVars["Regen"].BaseValue, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
			}
		}

		protected override void AddExtraArgsToDescription(LocString description)
		{
			Player owner = ((CardModel)this).Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				IRunState runState = owner.RunState;
				obj = ((runState != null) ? ((IPlayerCollection)runState).Players : null);
			}
			IReadOnlyList<Player> readOnlyList = (IReadOnlyList<Player>)obj;
			description.Add("IsMultiplayer", readOnlyList != null && readOnlyList.Count > 1);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).DynamicVars["Regen"].UpgradeValueBy(1m);
		}
	}
	public sealed class Symbiosis : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/necrobinder/symbiosis.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<SymbiosisPower>((int?)null) };

		public Symbiosis()
			: base(1, (CardType)3, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
			await PowerCmd.Apply<SymbiosisPower>(choiceContext, ((CardModel)this).Owner.Creature, 1m, ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)3);
		}
	}
	public sealed class TimeOff : CardModel
	{
		private static readonly string[] SfxPool = new string[3] { "res://wuwancients/audio/time_off_1.wav", "res://wuwancients/audio/time_off_2.wav", "res://wuwancients/audio/time_off_3.wav" };

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/time_off.png";

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

		protected override bool IsPlayable
		{
			get
			{
				Player owner = ((CardModel)this).Owner;
				object obj;
				if (owner == null)
				{
					obj = null;
				}
				else
				{
					PlayerCombatState playerCombatState = owner.PlayerCombatState;
					obj = ((playerCombatState != null) ? playerCombatState.Hand : null);
				}
				CardPile val = (CardPile)obj;
				if (val == null)
				{
					return false;
				}
				return !val.Cards.Any((CardModel c) => c != this && c.CanPlay());
			}
		}

		public TimeOff()
			: base(0, (CardType)2, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot(SfxPool[Random.Shared.Next(SfxPool.Length)], 1f);
			}
			Player player = ((CardModel)this).Owner;
			object obj;
			if (player == null)
			{
				obj = null;
			}
			else
			{
				Creature creature = player.Creature;
				obj = ((creature != null) ? creature.CombatState : null);
			}
			if (obj == null)
			{
				return;
			}
			List<PowerModel> debuffs = player.Creature.Powers.Where((PowerModel p) => p != null && (int)p.Type == 2).ToList();
			foreach (PowerModel power in debuffs)
			{
				await PowerCmd.Remove(power);
			}
			await PowerCmd.Apply<TimeOffExtraTurnPower>(choiceContext, player.Creature, 1m, player.Creature, (CardModel)(object)this, false);
			PlayerCmd.EndTurn(player, false, (Func<Task>)null);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)5);
		}
	}
	public sealed class TripleBrilliance : CardModel
	{
		private static readonly string[] SfxPool = new string[3] { "res://wuwancients/audio/triple_brilliance_1.wav", "res://wuwancients/audio/triple_brilliance_2.wav", "res://wuwancients/audio/triple_brilliance_3.wav" };

		private const string FullSfx = "res://wuwancients/audio/triple_brilliance_full.wav";

		private static AudioStreamPlayer? _longPlayer;

		private static readonly object BgmHolder = new object();

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/triple_brilliance.png";

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new IntVar("Replay", 1m) };

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.Static((StaticHoverTip)15, Array.Empty<DynamicVar>()) };

		public TripleBrilliance()
			: base(2, (CardType)2, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			PlayLongSfx();
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				instance.PlayOneShot(SfxPool[Random.Shared.Next(SfxPool.Length)], 1f);
			}
			Player player = ((CardModel)this).Owner;
			if (player == null)
			{
				return;
			}
			List<CardModel> candidates = (from c in PileTypeExtensions.GetPile((PileType)2, player).Cards
				where !c.Keywords.Contains((CardKeyword)4)
				where c.GetEnchantedReplayCount() < 1
				select c).ToList();
			List<CardModel> chosen = new List<CardModel>();
			while (chosen.Count < 3 && candidates.Count > 0)
			{
				CardModel pick = player.RunState.Rng.CombatCardSelection.NextItem<CardModel>((IEnumerable<CardModel>)candidates);
				chosen.Add(pick);
				candidates.Remove(pick);
			}
			foreach (CardModel card in chosen)
			{
				card.BaseReplayCount += ((CardModel)this).DynamicVars["Replay"].IntValue;
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).RemoveKeyword((CardKeyword)1);
		}

		public override Task AfterRoomEntered(AbstractRoom room)
		{
			StopLongSfx();
			return Task.CompletedTask;
		}

		private static void PlayLongSfx()
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Expected O, but got Unknown
			if (_longPlayer == null || !GodotObject.IsInstanceValid((GodotObject)(object)_longPlayer) || !((Node)_longPlayer).IsInsideTree())
			{
				_longPlayer = new AudioStreamPlayer();
				_longPlayer.Stream = GD.Load<AudioStream>("res://wuwancients/audio/triple_brilliance_full.wav");
				_longPlayer.Finished += delegate
				{
					BgmDucker.Release(BgmHolder);
				};
				NGame instance = NGame.Instance;
				if (instance == null)
				{
					return;
				}
				((Node)instance).AddChild((Node)(object)_longPlayer, false, (InternalMode)0);
			}
			BgmDucker.Acquire(BgmHolder);
			if (_longPlayer.Playing)
			{
				_longPlayer.Stop();
			}
			_longPlayer.Play(0f);
		}

		internal static void StopLongSfx()
		{
			if (_longPlayer != null && GodotObject.IsInstanceValid((GodotObject)(object)_longPlayer) && _longPlayer.Playing)
			{
				_longPlayer.Stop();
			}
			BgmDucker.Release(BgmHolder);
		}
	}
	[HarmonyPatch(typeof(NRun), "ShowGameOverScreen")]
	public static class StopLongSfxOnGameOverPatch
	{
		public static void Postfix()
		{
			TripleBrilliance.StopLongSfx();
			FuluoluoTakeoverPower.StopLongSfx();
			BgmDucker.ReleaseAll();
		}
	}
	[HarmonyPatch(typeof(NGame), "ReturnToMainMenu")]
	public static class StopLongSfxOnMainMenuPatch
	{
		public static void Postfix()
		{
			TripleBrilliance.StopLongSfx();
			FuluoluoTakeoverPower.StopLongSfx();
			BgmDucker.ReleaseAll();
		}
	}
	public sealed class UnderTheSea : CardModel
	{
		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/under_the_sea.png";

		public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)1 };

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new DamageVar(18m, (ValueProp)8) };

		protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
		{
			HoverTipFactory.FromPower<DiffusePower>((int?)1),
			HoverTipFactory.FromPower<DreamWeaverJellyfishPower>((int?)1)
		};

		public override int MaxUpgradeLevel => 1;

		public UnderTheSea()
			: base(3, (CardType)1, (CardRarity)5, (TargetType)3, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				int num = Random.Shared.Next(3);
				if (1 == 0)
				{
				}
				string text = num switch
				{
					0 => "res://wuwancients/audio/under_the_sea_1.wav", 
					1 => "res://wuwancients/audio/under_the_sea_2.wav", 
					_ => "res://wuwancients/audio/under_the_sea_3.wav", 
				};
				if (1 == 0)
				{
				}
				instance.PlayOneShot(text, 1f);
			}
			Player owner = ((CardModel)this).Owner;
			Creature creature = ((owner != null) ? owner.Creature : null);
			if (((creature != null) ? creature.CombatState : null) == null)
			{
				return;
			}
			IReadOnlyList<Creature> enemies = creature.CombatState.HittableEnemies;
			if (enemies.Count > 0)
			{
				await DamageCmd.Attack(((CardModel)this).DynamicVars["Damage"].BaseValue).FromCard((CardModel)(object)this, cardPlay).TargetingAllOpponents(((CardModel)this).CombatState)
					.Execute(choiceContext);
			}
			foreach (Player player in creature.CombatState.Players)
			{
				if (((player != null) ? player.Creature : null) != null && player.Creature.IsAlive)
				{
					await PowerCmd.Apply<DiffusePower>(choiceContext, player.Creature, 1m, creature, (CardModel)(object)this, false);
				}
			}
		}

		protected override void OnUpgrade()
		{
			((DynamicVar)((CardModel)this).DynamicVars.Damage).UpgradeValueBy(9m);
		}
	}
	public sealed class Wormhole : CardModel
	{
		[CompilerGenerated]
		private sealed class <get_ExtraHoverTips>d__8 : IEnumerable<IHoverTip>, IEnumerable, IEnumerator<IHoverTip>, IEnumerator, IDisposable
		{
			private int <>1__state;

			private IHoverTip <>2__current;

			private int <>l__initialThreadId;

			public Wormhole <>4__this;

			private UnstableTunnel <tunnel>5__1;

			private IEnumerator<IHoverTip> <>s__2;

			private IHoverTip <tip>5__3;

			IHoverTip IEnumerator<IHoverTip>.Current
			{
				[DebuggerHidden]
				get
				{
					return <>2__current;
				}
			}

			object IEnumerator.Current
			{
				[DebuggerHidden]
				get
				{
					return <>2__current;
				}
			}

			[DebuggerHidden]
			public <get_ExtraHoverTips>d__8(int <>1__state)
			{
				this.<>1__state = <>1__state;
				<>l__initialThreadId = Environment.CurrentManagedThreadId;
			}

			[DebuggerHidden]
			void IDisposable.Dispose()
			{
				int num = <>1__state;
				if (num == -3 || num == 1)
				{
					try
					{
					}
					finally
					{
						<>m__Finally1();
					}
				}
				<tunnel>5__1 = null;
				<>s__2 = null;
				<tip>5__3 = null;
				<>1__state = -2;
			}

			private bool MoveNext()
			{
				try
				{
					int num = <>1__state;
					if (num != 0)
					{
						if (num != 1)
						{
							return false;
						}
						<>1__state = -3;
						<tip>5__3 = null;
					}
					else
					{
						<>1__state = -1;
						<tunnel>5__1 = ModelDb.Relic<UnstableTunnel>();
						if (<tunnel>5__1 == null)
						{
							goto IL_00b4;
						}
						<>s__2 = HoverTipFactory.FromRelic((RelicModel)(object)<tunnel>5__1).GetEnumerator();
						<>1__state = -3;
					}
					if (<>s__2.MoveNext())
					{
						<tip>5__3 = <>s__2.Current;
						<>2__current = <tip>5__3;
						<>1__state = 1;
						return true;
					}
					<>m__Finally1();
					<>s__2 = null;
					goto IL_00b4;
					IL_00b4:
					return false;
				}
				catch
				{
					//try-fault
					((IDisposable)this).Dispose();
					throw;
				}
			}

			bool IEnumerator.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				return this.MoveNext();
			}

			private void <>m__Finally1()
			{
				<>1__state = -1;
				if (<>s__2 != null)
				{
					<>s__2.Dispose();
				}
			}

			[DebuggerHidden]
			void IEnumerator.Reset()
			{
				throw new NotSupportedException();
			}

			[DebuggerHidden]
			IEnumerator<IHoverTip> IEnumerable<IHoverTip>.GetEnumerator()
			{
				<get_ExtraHoverTips>d__8 result;
				if (<>1__state == -2 && <>l__initialThreadId == Environment.CurrentManagedThreadId)
				{
					<>1__state = 0;
					result = this;
				}
				else
				{
					result = new <get_ExtraHoverTips>d__8(0)
					{
						<>4__this = <>4__this
					};
				}
				return result;
			}

			[DebuggerHidden]
			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<IHoverTip>)this).GetEnumerator();
			}
		}

		public override int CanonicalStarCost => 3;

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/regent/wormhole.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

		protected override IEnumerable<IHoverTip> ExtraHoverTips
		{
			[IteratorStateMachine(typeof(<get_ExtraHoverTips>d__8))]
			get
			{
				//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
				<get_ExtraHoverTips>d__8 <get_ExtraHoverTips>d__ = new <get_ExtraHoverTips>d__8(-2);
				<get_ExtraHoverTips>d__.<>4__this = this;
				return <get_ExtraHoverTips>d__;
			}
		}

		public Wormhole()
			: base(3, (CardType)2, (CardRarity)5, (TargetType)1, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			UnstableTunnel tunnel = (UnstableTunnel)(object)((AbstractModel)ModelDb.Relic<UnstableTunnel>()).MutableClone();
			await RelicCmd.Obtain((RelicModel)(object)tunnel, ((CardModel)this).Owner, -1);
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)5);
		}
	}
	public sealed class YiZhanZuYi : CardModel
	{
		private const int DamageGainPerKill = 2;

		private int _currentDamage = 2;

		[SavedProperty]
		public int CurrentDamage
		{
			get
			{
				return _currentDamage;
			}
			set
			{
				((AbstractModel)this).AssertMutable();
				_currentDamage = value;
				((DynamicVar)((CardModel)this).DynamicVars.Damage).BaseValue = _currentDamage;
			}
		}

		public override string PortraitPath => "res://wuwancients/images/packed/card_portraits/yi_zhan_zu_yi.png";

		protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new DamageVar((decimal)CurrentDamage, (ValueProp)8) };

		public override int MaxUpgradeLevel => 1;

		public YiZhanZuYi()
			: base(1, (CardType)1, (CardRarity)5, (TargetType)0, true)
		{
		}

		protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
		{
			NAudioManager instance = NAudioManager.Instance;
			if (instance != null)
			{
				int num = Random.Shared.Next(3);
				if (1 == 0)
				{
				}
				string text = num switch
				{
					0 => "res://wuwancients/audio/yi_zhan_zu_yi_1.wav", 
					1 => "res://wuwancients/audio/yi_zhan_zu_yi_2.wav", 
					_ => "res://wuwancients/audio/yi_zhan_zu_yi_3.wav", 
				};
				if (1 == 0)
				{
				}
				instance.PlayOneShot(text, 1f);
			}
			Player owner = ((CardModel)this).Owner;
			Creature creature = ((owner != null) ? owner.Creature : null);
			if (((creature != null) ? creature.CombatState : null) == null)
			{
				return;
			}
			IReadOnlyList<Creature> enemies = creature.CombatState.HittableEnemies;
			foreach (Creature enemy in enemies)
			{
				bool shouldTriggerFatal = enemy.Powers.All((PowerModel p) => p.ShouldOwnerDeathTriggerFatal());
				AttackCommand attackCommand = await DamageCmd.Attack(((DynamicVar)((CardModel)this).DynamicVars.Damage).BaseValue).FromCard((CardModel)(object)this, cardPlay).Targeting(enemy)
					.Execute(choiceContext);
				if (shouldTriggerFatal && attackCommand.Results.SelectMany((List<DamageResult> r) => r).Any((DamageResult r) => r.WasTargetKilled))
				{
					OnKill();
				}
			}
		}

		private void OnKill()
		{
			CurrentDamage += 2;
			(((CardModel)this).DeckVersion as YiZhanZuYi)?.SyncFromCombat(CurrentDamage);
		}

		private void SyncFromCombat(int damage)
		{
			if (damage > CurrentDamage)
			{
				CurrentDamage = damage;
			}
		}

		protected override void OnUpgrade()
		{
			((CardModel)this).AddKeyword((CardKeyword)5);
		}
	}
}
namespace wuwancients.Ancients
{
	public static class AncientPool
	{
		private const ulong SaltAncientWinner = 2654446023uL;

		public static bool IsSelected(Type myType, ActModel act, Dictionary<Type, int> candidates)
		{
			int num = ActModelExtensions.ActNumber(act);
			RunState val = RunManager.Instance.DebugOnlyGetState();
			List<KeyValuePair<Type, int>> list = candidates.Where<KeyValuePair<Type, int>>((KeyValuePair<Type, int> kv) => kv.Value > 0).OrderBy<KeyValuePair<Type, int>, string>((KeyValuePair<Type, int> kv) => kv.Key.FullName, StringComparer.Ordinal).ToList();
			List<string> values = new List<string>();
			if (((val != null) ? val.Acts : null) != null)
			{
				HashSet<Type> appearedTypes = new HashSet<Type>();
				foreach (ActModel act2 in val.Acts)
				{
					if (act2 == act)
					{
						continue;
					}
					try
					{
						AncientEventModel ancient = act2.Ancient;
						CustomAncientModel val2 = (CustomAncientModel)(object)((ancient is CustomAncientModel) ? ancient : null);
						if (val2 != null)
						{
							appearedTypes.Add(((object)val2).GetType());
						}
					}
					catch (InvalidOperationException)
					{
					}
				}
				if (appearedTypes.Count > 0)
				{
					values = (from kv in list
						where appearedTypes.Contains(kv.Key)
						select kv.Key.Name).ToList();
					list.RemoveAll((KeyValuePair<Type, int> kv) => appearedTypes.Contains(kv.Key));
				}
			}
			if (WuwancientsConfig.禁用原版先古 && list.Count == 0 && candidates.Count > 0)
			{
				list = candidates.Select<KeyValuePair<Type, int>, KeyValuePair<Type, int>>((KeyValuePair<Type, int> kv) => new KeyValuePair<Type, int>(kv.Key, 1)).OrderBy<KeyValuePair<Type, int>, string>((KeyValuePair<Type, int> kv) => kv.Key.FullName, StringComparer.Ordinal).ToList();
			}
			Type type = null;
			int num2 = 0;
			int num3 = 0;
			int num4 = -1;
			if (list.Count > 0)
			{
				KeyValuePair<Type, int> keyValuePair = list.Where((KeyValuePair<Type, int> kv) => kv.Value >= 100).OrderBy<KeyValuePair<Type, int>, string>((KeyValuePair<Type, int> kv) => kv.Key.FullName, StringComparer.Ordinal).FirstOrDefault();
				if (keyValuePair.Key != null)
				{
					type = keyValuePair.Key;
				}
				else
				{
					num2 = ((!WuwancientsConfig.禁用原版先古 && ((val != null) ? val.UnlockState : null) != null) ? act.GetUnlockedAncients(val.UnlockState).Count() : 0);
					num3 = list.Count + num2;
					num4 = SyncedRng.DeterministicIndex((IRunState?)(object)val, (ulong)(2654446023u + num), num3);
					if (num4 < list.Count)
					{
						type = list[num4].Key;
					}
				}
			}
			Log.Info(string.Format("[wuwancients] ancient roll act={0} asker={1} candidates=[{2}] deduped=[{3}] original={4} total={5} roll={6} winner={7}", num, myType.Name, string.Join(",", list.Select((KeyValuePair<Type, int> kv) => kv.Key.Name)), string.Join(",", values), num2, num3, num4, type?.Name ?? "vanilla"), 2);
			return type == myType;
		}

		internal static List<T> ShuffleDeterministic<T>(IEnumerable<T> items, string saltKey)
		{
			List<T> list = items.ToList();
			if (list.Count <= 1)
			{
				return list;
			}
			RunState val = RunManager.Instance.DebugOnlyGetState();
			ulong seed = ((val == null) ? 0 : val.Rng.Seed);
			ulong salt = StringHelper.GetDeterministicHashCode(saltKey);
			return (from pair in list.Select((T item, int index) => new KeyValuePair<ulong, T>(Mix(seed, salt, (ulong)index), item))
				orderby pair.Key
				select pair.Value).ToList();
		}

		internal static bool CoinFlip(string saltKey)
		{
			RunState val = RunManager.Instance.DebugOnlyGetState();
			ulong seed = ((val == null) ? 0 : val.Rng.Seed);
			ulong deterministicHashCode = StringHelper.GetDeterministicHashCode(saltKey);
			return (Mix(seed, deterministicHashCode, 0uL) & 1) == 0;
		}

		private static ulong Mix(ulong seed, ulong salt, ulong index)
		{
			ulong num = seed + (ulong)((long)(salt ^ (ulong)((long)index * -7046029254386353131L)) * -4658895280553007687L);
			num = (num ^ (num >> 30)) * 13787848793156543929uL;
			num = (num ^ (num >> 27)) * 10723151780598845931uL;
			return num ^ (num >> 31);
		}
	}
	internal static class AncientScenePicker
	{
		public static string Pick(string ancientEntry, AncientSceneMode mode, params string[] scenes)
		{
			if (1 == 0)
			{
			}
			int num = mode switch
			{
				AncientSceneMode.场景1 => 0, 
				AncientSceneMode.场景2 => 1, 
				_ => -1, 
			};
			if (1 == 0)
			{
			}
			int forcedIndex = num;
			return PickCore(ancientEntry, forcedIndex, scenes);
		}

		public static string Pick(string ancientEntry, AncientSingleSceneMode mode, params string[] scenes)
		{
			int forcedIndex = ((mode != 0) ? (-1) : 0);
			return PickCore(ancientEntry, forcedIndex, scenes);
		}

		private static string PickCore(string ancientEntry, int forcedIndex, string[] scenes)
		{
			if (scenes.Length == 0)
			{
				return string.Empty;
			}
			if (forcedIndex >= 0 && forcedIndex < scenes.Length)
			{
				return scenes[forcedIndex];
			}
			if (scenes.Length == 1)
			{
				return scenes[0];
			}
			RunState val = RunManager.Instance.DebugOnlyGetState();
			ulong num = ((val == null) ? 0 : val.Rng.Seed);
			ulong deterministicHashCode = StringHelper.GetDeterministicHashCode(ancientEntry);
			ulong num2 = num + (ulong)((long)deterministicHashCode * -7046029254386353131L);
			num2 = (num2 ^ (num2 >> 30)) * 13787848793156543929uL;
			num2 = (num2 ^ (num2 >> 27)) * 10723151780598845931uL;
			num2 ^= num2 >> 31;
			return scenes[(uint)(num2 % (ulong)scenes.Length)];
		}
	}
	public class Augusta : CustomAncientModel
	{
		private static readonly HashSet<string> VanillaIds = new HashSet<string> { "IRONCLAD", "SILENT", "DEFECT", "REGENT", "NECROBINDER" };

		private static readonly HashSet<Type> BlockedRelicTypes = new HashSet<Type> { typeof(HiddenSeaRecord) };

		public override Color ButtonColor => new Color(0.85f, 0.62f, 0.08f, 0.7f);

		public override Color DialogueColor => new Color(0.35f, 0.05f, 0.05f, 1f);

		public override string? CustomMapIconPath => "res://wuwancients/images/icons/augusta_map.png";

		public override string? CustomMapIconOutlinePath => "res://wuwancients/images/icons/augusta_map_outline.png";

		public override string? CustomRunHistoryIconPath => "res://wuwancients/images/icons/augusta_map_icon.png";

		public override string? CustomRunHistoryIconOutlinePath => "res://wuwancients/images/icons/augusta_map_icon.png";

		public override string? CustomScenePath => AncientScenePicker.Pick(((AbstractModel)this).Id.Entry, WuwancientsConfig.奥古斯塔场景, "res://wuwancients/scenes/augusta.tscn");

		protected override OptionPools MakeOptionPools
		{
			get
			{
				//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
				//IL_00f1: Expected O, but got Unknown
				List<AncientOption> list = new List<AncientOption>();
				AddIfAllowed<SunAndGriffinSeal>(list);
				AddIfAllowed<SundayCrown>(list);
				AddIfAllowed<ResidualFrequencyOfBlackTide>(list);
				AddIfAllowed<AuthorityOfThunderAndCrown>(list);
				AddIfAllowed<SmeltedFragment>(list);
				AddIfAllowed<GlowingMarigold>(list);
				AddIfAllowed<SmallAcorn>(list);
				AddIfAllowed<SweetLeafSplitBread>(list);
				AddIfAllowed<OldHairband>(list);
				AddIfAllowed<AugustaDumpling>(list);
				List<AncientOption> source = AncientPool.ShuffleDeterministic(list, ((AbstractModel)this).Id.Entry);
				AncientOption[] array = source.Where((AncientOption o) => o != null).Take(3).Cast<AncientOption>()
					.ToArray();
				return new OptionPools(CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length != 0) ? array[0] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 1) ? array[1] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 2) ? array[2] : null }));
			}
		}

		public override IEnumerable<EventOption> AllPossibleOptions => (IEnumerable<EventOption>)(object)new EventOption[10]
		{
			((AncientEventModel)this).RelicOption<SunAndGriffinSeal>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<SundayCrown>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ResidualFrequencyOfBlackTide>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<AuthorityOfThunderAndCrown>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<SmeltedFragment>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<GlowingMarigold>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<SmallAcorn>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<SweetLeafSplitBread>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<OldHairband>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<AugustaDumpling>("INITIAL", (string)null)
		};

		private bool IsModPlayer()
		{
			RunState val = RunManager.Instance.DebugOnlyGetState();
			if (val == null)
			{
				return false;
			}
			Player val2 = val.Players?.FirstOrDefault();
			object obj;
			if (val2 == null)
			{
				obj = null;
			}
			else
			{
				CharacterModel character = val2.Character;
				if (character == null)
				{
					obj = null;
				}
				else
				{
					ModelId id = ((AbstractModel)character).Id;
					obj = ((id != null) ? id.Entry : null);
				}
			}
			if (obj == null)
			{
				return false;
			}
			return !VanillaIds.Contains(((AbstractModel)val2.Character).Id.Entry.ToUpperInvariant());
		}

		private bool ShouldAdd(Type relicType)
		{
			return !IsModPlayer() || !BlockedRelicTypes.Contains(relicType);
		}

		public override bool IsValidForAct(ActModel act)
		{
			return ActModelExtensions.ActNumber(act) == 3;
		}

		public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
		{
			if (ActModelExtensions.ActNumber(act) != 3)
			{
				return false;
			}
			Dictionary<Type, int> dictionary = new Dictionary<Type, int>();
			if (WuwancientsConfig.强制奥古斯塔出现)
			{
				dictionary[typeof(Augusta)] = 100;
			}
			else if (WuwancientsConfig.奥古斯塔是否出现)
			{
				dictionary[typeof(Augusta)] = 1;
			}
			if (WuwancientsConfig.强制千咲出现)
			{
				dictionary[typeof(Chisa)] = 100;
			}
			else if (WuwancientsConfig.千咲是否出现)
			{
				dictionary[typeof(Chisa)] = 1;
			}
			if (WuwancientsConfig.强制弗洛洛出现)
			{
				dictionary[typeof(Froro)] = 100;
			}
			else if (WuwancientsConfig.弗洛洛是否出现)
			{
				dictionary[typeof(Froro)] = 1;
			}
			if (WuwancientsConfig.强制琳奈出现)
			{
				dictionary[typeof(Linna)] = 100;
			}
			else if (WuwancientsConfig.琳奈是否出现)
			{
				dictionary[typeof(Linna)] = 1;
			}
			if (WuwancientsConfig.强制尤诺出现)
			{
				dictionary[typeof(Youno)] = 100;
			}
			else if (WuwancientsConfig.尤诺是否出现)
			{
				dictionary[typeof(Youno)] = 1;
			}
			return AncientPool.IsSelected(typeof(Augusta), act, dictionary);
		}

		private void AddIfAllowed<T>(List<AncientOption?> list) where T : RelicModel
		{
			if (ShouldAdd(typeof(T)))
			{
				list.Add(CustomAncientModel.AncientOption<T>(1, (Func<T, RelicModel>)null, (Func<T, IEnumerable<RelicModel>>)null));
			}
		}

		public Augusta()
			: base(true, false)
		{
		}
	}
	public class Chisa : CustomAncientModel
	{
		public override Color ButtonColor => new Color(0.55f, 0.05f, 0.05f, 0.6f);

		public override Color DialogueColor => new Color(0.1f, 0f, 0f, 1f);

		public override string? CustomMapIconPath => "res://wuwancients/images/icons/chisa_map.png";

		public override string? CustomMapIconOutlinePath => "res://wuwancients/images/icons/chisa_map_outline.png";

		public override string? CustomRunHistoryIconPath => "res://wuwancients/images/icons/chisa_map_icon.png";

		public override string? CustomRunHistoryIconOutlinePath => "res://wuwancients/images/icons/chisa_map_icon.png";

		public override string? CustomScenePath => AncientScenePicker.Pick(((AbstractModel)this).Id.Entry, WuwancientsConfig.千咲场景, "res://wuwancients/scenes/chisa.tscn");

		protected override OptionPools MakeOptionPools
		{
			get
			{
				//IL_0131: Unknown result type (might be due to invalid IL or missing references)
				//IL_0137: Expected O, but got Unknown
				List<AncientOption> items = new List<AncientOption>
				{
					CustomAncientModel.AncientOption<RedHairband>(1, (Func<RedHairband, RelicModel>)null, (Func<RedHairband, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<Knot>(1, (Func<Knot, RelicModel>)null, (Func<Knot, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<Latte>(1, (Func<Latte, RelicModel>)null, (Func<Latte, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<ResonanceSuppressionCollar>(1, (Func<ResonanceSuppressionCollar, RelicModel>)null, (Func<ResonanceSuppressionCollar, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<OldScissors>(1, (Func<OldScissors, RelicModel>)null, (Func<OldScissors, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<SurveyLog>(1, (Func<SurveyLog, RelicModel>)null, (Func<SurveyLog, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<LongSummerFlower>(1, (Func<LongSummerFlower, RelicModel>)null, (Func<LongSummerFlower, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<BoZaiDoll>(1, (Func<BoZaiDoll, RelicModel>)null, (Func<BoZaiDoll, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<ChisaDango>(1, (Func<ChisaDango, RelicModel>)null, (Func<ChisaDango, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<FragrantLemonShabuShabu>(1, (Func<FragrantLemonShabuShabu, RelicModel>)null, (Func<FragrantLemonShabuShabu, IEnumerable<RelicModel>>)null)
				};
				List<AncientOption> source = AncientPool.ShuffleDeterministic(items, ((AbstractModel)this).Id.Entry);
				AncientOption[] array = (from o in source.Take(3)
					where o != null
					select o).Cast<AncientOption>().ToArray();
				return new OptionPools(CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length != 0) ? array[0] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 1) ? array[1] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 2) ? array[2] : null }));
			}
		}

		public override IEnumerable<EventOption> AllPossibleOptions => (IEnumerable<EventOption>)(object)new EventOption[10]
		{
			((AncientEventModel)this).RelicOption<RedHairband>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<Knot>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<Latte>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ResonanceSuppressionCollar>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<OldScissors>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<SurveyLog>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<LongSummerFlower>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<BoZaiDoll>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ChisaDango>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<FragrantLemonShabuShabu>("INITIAL", (string)null)
		};

		public override bool IsValidForAct(ActModel act)
		{
			return ActModelExtensions.ActNumber(act) == 3;
		}

		public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
		{
			if (ActModelExtensions.ActNumber(act) != 3)
			{
				return false;
			}
			Dictionary<Type, int> dictionary = new Dictionary<Type, int>();
			if (WuwancientsConfig.强制奥古斯塔出现)
			{
				dictionary[typeof(Augusta)] = 100;
			}
			else if (WuwancientsConfig.奥古斯塔是否出现)
			{
				dictionary[typeof(Augusta)] = 1;
			}
			if (WuwancientsConfig.强制千咲出现)
			{
				dictionary[typeof(Chisa)] = 100;
			}
			else if (WuwancientsConfig.千咲是否出现)
			{
				dictionary[typeof(Chisa)] = 1;
			}
			if (WuwancientsConfig.强制弗洛洛出现)
			{
				dictionary[typeof(Froro)] = 100;
			}
			else if (WuwancientsConfig.弗洛洛是否出现)
			{
				dictionary[typeof(Froro)] = 1;
			}
			if (WuwancientsConfig.强制琳奈出现)
			{
				dictionary[typeof(Linna)] = 100;
			}
			else if (WuwancientsConfig.琳奈是否出现)
			{
				dictionary[typeof(Linna)] = 1;
			}
			if (WuwancientsConfig.强制尤诺出现)
			{
				dictionary[typeof(Youno)] = 100;
			}
			else if (WuwancientsConfig.尤诺是否出现)
			{
				dictionary[typeof(Youno)] = 1;
			}
			return AncientPool.IsSelected(typeof(Chisa), act, dictionary);
		}

		public Chisa()
			: base(true, false)
		{
		}
	}
	public class Febe : CustomAncientModel
	{
		public override Color ButtonColor => new Color(0.82f, 0.64f, 0.2f, 0.7f);

		public override Color DialogueColor => new Color(0.35f, 0.33f, 0.33f, 1f);

		public override string? CustomMapIconPath => "res://wuwancients/images/icons/febe_map.png";

		public override string? CustomMapIconOutlinePath => "res://wuwancients/images/icons/febe_map_outline.png";

		public override string? CustomRunHistoryIconPath => "res://wuwancients/images/icons/febe_map_icon.png";

		public override string? CustomRunHistoryIconOutlinePath => "res://wuwancients/images/icons/febe_map_icon.png";

		public override string? CustomScenePath => AncientScenePicker.Pick(((AbstractModel)this).Id.Entry, WuwancientsConfig.菲比场景, "res://wuwancients/scenes/febe.tscn");

		protected override OptionPools MakeOptionPools
		{
			get
			{
				//IL_0178: Unknown result type (might be due to invalid IL or missing references)
				//IL_017f: Expected O, but got Unknown
				List<AncientOption> list = new List<AncientOption>
				{
					CustomAncientModel.AncientOption<Gospel>(1, (Func<Gospel, RelicModel>)null, (Func<Gospel, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<YingBaiLadosOldRelic>(1, (Func<YingBaiLadosOldRelic, RelicModel>)null, (Func<YingBaiLadosOldRelic, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<HeGuangTongChang>(1, (Func<HeGuangTongChang, RelicModel>)null, (Func<HeGuangTongChang, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<GoldenGrace>(1, (Func<GoldenGrace, RelicModel>)null, (Func<GoldenGrace, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<PizzaSlice>(1, (Func<PizzaSlice, RelicModel>)null, (Func<PizzaSlice, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<PurifyingConch>(1, (Func<PurifyingConch, RelicModel>)null, (Func<PurifyingConch, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<PhoebeChobi>(1, (Func<PhoebeChobi, RelicModel>)null, (Func<PhoebeChobi, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<HiddenSeaRecord>(1, (Func<HiddenSeaRecord, RelicModel>)null, (Func<HiddenSeaRecord, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<FibiDumpling>(1, (Func<FibiDumpling, RelicModel>)null, (Func<FibiDumpling, IEnumerable<RelicModel>>)null)
				};
				RunState obj = RunManager.Instance.DebugOnlyGetState();
				if (obj == null || !obj.Players.Any((Player p) => p.HasEventPet()))
				{
					list.Add(CustomAncientModel.AncientOption<MonasticHat>(1, (Func<MonasticHat, RelicModel>)null, (Func<MonasticHat, IEnumerable<RelicModel>>)null));
				}
				List<AncientOption> source = AncientPool.ShuffleDeterministic(list, ((AbstractModel)this).Id.Entry);
				AncientOption[] array = (from o in source.Take(3)
					where o != null
					select o).Cast<AncientOption>().ToArray();
				return new OptionPools(CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length != 0) ? array[0] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 1) ? array[1] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 2) ? array[2] : null }));
			}
		}

		public override IEnumerable<EventOption> AllPossibleOptions => (IEnumerable<EventOption>)(object)new EventOption[10]
		{
			((AncientEventModel)this).RelicOption<Gospel>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<YingBaiLadosOldRelic>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<HeGuangTongChang>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<GoldenGrace>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<PizzaSlice>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<PurifyingConch>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<MonasticHat>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<PhoebeChobi>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<HiddenSeaRecord>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<FibiDumpling>("INITIAL", (string)null)
		};

		public override bool IsValidForAct(ActModel act)
		{
			return ActModelExtensions.ActNumber(act) == 2;
		}

		public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
		{
			if (ActModelExtensions.ActNumber(act) != 2)
			{
				return false;
			}
			Dictionary<Type, int> dictionary = new Dictionary<Type, int>();
			if (WuwancientsConfig.强制菲比出现)
			{
				dictionary[typeof(Febe)] = 100;
			}
			else if (WuwancientsConfig.菲比是否出现)
			{
				dictionary[typeof(Febe)] = 1;
			}
			if (WuwancientsConfig.强制尤诺出现)
			{
				dictionary[typeof(Youno)] = 100;
			}
			else if (WuwancientsConfig.尤诺是否出现)
			{
				dictionary[typeof(Youno)] = 1;
			}
			if (WuwancientsConfig.强制嘉贝莉娜出现)
			{
				dictionary[typeof(Galbrena)] = 100;
			}
			else if (WuwancientsConfig.嘉贝莉娜是否出现)
			{
				dictionary[typeof(Galbrena)] = 1;
			}
			if (WuwancientsConfig.强制赞妮出现)
			{
				dictionary[typeof(Zani)] = 100;
			}
			else if (WuwancientsConfig.赞妮是否出现)
			{
				dictionary[typeof(Zani)] = 1;
			}
			return AncientPool.IsSelected(typeof(Febe), act, dictionary);
		}

		public Febe()
			: base(true, false)
		{
		}
	}
	public class Froro : CustomAncientModel
	{
		public override Color ButtonColor => new Color(0.55f, 0.05f, 0.05f, 0.6f);

		public override Color DialogueColor => new Color(0.1f, 0f, 0f, 1f);

		public override string? CustomMapIconPath => "res://wuwancients/images/icons/froro_map.png";

		public override string? CustomMapIconOutlinePath => "res://wuwancients/images/icons/froro_map_outline.png";

		public override string? CustomRunHistoryIconPath => "res://wuwancients/images/icons/froro_map_icon.png";

		public override string? CustomRunHistoryIconOutlinePath => "res://wuwancients/images/icons/froro_map_icon.png";

		public override string? CustomScenePath => AncientScenePicker.Pick(((AbstractModel)this).Id.Entry, WuwancientsConfig.弗洛洛场景, "res://wuwancients/scenes/froro.tscn", "res://wuwancients/scenes/froro_b.tscn");

		protected override OptionPools MakeOptionPools
		{
			get
			{
				//IL_0131: Unknown result type (might be due to invalid IL or missing references)
				//IL_0137: Expected O, but got Unknown
				List<AncientOption> items = new List<AncientOption>
				{
					CustomAncientModel.AncientOption<DeathAndLifeMovement>(1, (Func<DeathAndLifeMovement, RelicModel>)null, (Func<DeathAndLifeMovement, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<TearStainedBandage>(1, (Func<TearStainedBandage, RelicModel>)null, (Func<TearStainedBandage, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<HecatesPhantom>(1, (Func<HecatesPhantom, RelicModel>)null, (Func<HecatesPhantom, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<PromiseBaton>(1, (Func<PromiseBaton, RelicModel>)null, (Func<PromiseBaton, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<FallingEcho>(1, (Func<FallingEcho, RelicModel>)null, (Func<FallingEcho, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<ScatteredLycoris>(1, (Func<ScatteredLycoris, RelicModel>)null, (Func<ScatteredLycoris, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<FulouluoDumpling>(1, (Func<FulouluoDumpling, RelicModel>)null, (Func<FulouluoDumpling, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<RedCurrantTart>(1, (Func<RedCurrantTart, RelicModel>)null, (Func<RedCurrantTart, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<UnfinishedSymphony>(1, (Func<UnfinishedSymphony, RelicModel>)null, (Func<UnfinishedSymphony, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<NewWorldCarnival>(1, (Func<NewWorldCarnival, RelicModel>)null, (Func<NewWorldCarnival, IEnumerable<RelicModel>>)null)
				};
				List<AncientOption> source = AncientPool.ShuffleDeterministic(items, ((AbstractModel)this).Id.Entry);
				AncientOption[] array = (from o in source.Take(3)
					where o != null
					select o).Cast<AncientOption>().ToArray();
				return new OptionPools(CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length != 0) ? array[0] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 1) ? array[1] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 2) ? array[2] : null }));
			}
		}

		public override IEnumerable<EventOption> AllPossibleOptions => (IEnumerable<EventOption>)(object)new EventOption[10]
		{
			((AncientEventModel)this).RelicOption<DeathAndLifeMovement>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<TearStainedBandage>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<HecatesPhantom>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<PromiseBaton>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<FallingEcho>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ScatteredLycoris>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<FulouluoDumpling>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<RedCurrantTart>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<UnfinishedSymphony>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<NewWorldCarnival>("INITIAL", (string)null)
		};

		public override bool IsValidForAct(ActModel act)
		{
			return ActModelExtensions.ActNumber(act) == 3;
		}

		public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
		{
			if (ActModelExtensions.ActNumber(act) != 3)
			{
				return false;
			}
			Dictionary<Type, int> dictionary = new Dictionary<Type, int>();
			if (WuwancientsConfig.强制奥古斯塔出现)
			{
				dictionary[typeof(Augusta)] = 100;
			}
			else if (WuwancientsConfig.奥古斯塔是否出现)
			{
				dictionary[typeof(Augusta)] = 1;
			}
			if (WuwancientsConfig.强制千咲出现)
			{
				dictionary[typeof(Chisa)] = 100;
			}
			else if (WuwancientsConfig.千咲是否出现)
			{
				dictionary[typeof(Chisa)] = 1;
			}
			if (WuwancientsConfig.强制弗洛洛出现)
			{
				dictionary[typeof(Froro)] = 100;
			}
			else if (WuwancientsConfig.弗洛洛是否出现)
			{
				dictionary[typeof(Froro)] = 1;
			}
			if (WuwancientsConfig.强制琳奈出现)
			{
				dictionary[typeof(Linna)] = 100;
			}
			else if (WuwancientsConfig.琳奈是否出现)
			{
				dictionary[typeof(Linna)] = 1;
			}
			if (WuwancientsConfig.强制尤诺出现)
			{
				dictionary[typeof(Youno)] = 100;
			}
			else if (WuwancientsConfig.尤诺是否出现)
			{
				dictionary[typeof(Youno)] = 1;
			}
			return AncientPool.IsSelected(typeof(Froro), act, dictionary);
		}

		public Froro()
			: base(true, false)
		{
		}
	}
	public class Galbrena : CustomAncientModel
	{
		public override Color ButtonColor => new Color(0.4f, 0.42f, 0.47f, 0.7f);

		public override Color DialogueColor => new Color(0.1f, 0.1f, 0.12f, 1f);

		public override string? CustomMapIconPath => "res://wuwancients/images/icons/galbrena_map.png";

		public override string? CustomMapIconOutlinePath => "res://wuwancients/images/icons/galbrena_map_outline.png";

		public override string? CustomRunHistoryIconPath => "res://wuwancients/images/icons/galbrena_map_icon.png";

		public override string? CustomRunHistoryIconOutlinePath => "res://wuwancients/images/icons/galbrena_map_icon.png";

		public override string? CustomScenePath => AncientScenePicker.Pick(((AbstractModel)this).Id.Entry, WuwancientsConfig.嘉贝莉娜场景, "res://wuwancients/scenes/galbrena.tscn", "res://wuwancients/scenes/galbrena_b.tscn");

		protected override OptionPools MakeOptionPools
		{
			get
			{
				//IL_0131: Unknown result type (might be due to invalid IL or missing references)
				//IL_0137: Expected O, but got Unknown
				List<AncientOption> items = new List<AncientOption>
				{
					CustomAncientModel.AncientOption<GalbrenasKarmaFire>(1, (Func<GalbrenasKarmaFire, RelicModel>)null, (Func<GalbrenasKarmaFire, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<ChimerasHeart>(1, (Func<ChimerasHeart, RelicModel>)null, (Func<ChimerasHeart, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<HarpyienSharpFeather>(1, (Func<HarpyienSharpFeather, RelicModel>)null, (Func<HarpyienSharpFeather, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<DullahansFlesh>(1, (Func<DullahansFlesh, RelicModel>)null, (Func<DullahansFlesh, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<BalorsEye>(1, (Func<BalorsEye, RelicModel>)null, (Func<BalorsEye, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<NamelessShadowHusk>(1, (Func<NamelessShadowHusk, RelicModel>)null, (Func<NamelessShadowHusk, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<SweetDreams>(1, (Func<SweetDreams, RelicModel>)null, (Func<SweetDreams, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<ExtraThickMilkshakeShavedIce>(1, (Func<ExtraThickMilkshakeShavedIce, RelicModel>)null, (Func<ExtraThickMilkshakeShavedIce, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<StoneRose>(1, (Func<StoneRose, RelicModel>)null, (Func<StoneRose, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<FirstBloodOath>(1, (Func<FirstBloodOath, RelicModel>)null, (Func<FirstBloodOath, IEnumerable<RelicModel>>)null)
				};
				List<AncientOption> source = AncientPool.ShuffleDeterministic(items, ((AbstractModel)this).Id.Entry);
				AncientOption[] array = source.Where((AncientOption o) => o != null).Take(3).Cast<AncientOption>()
					.ToArray();
				return new OptionPools(CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length != 0) ? array[0] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 1) ? array[1] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 2) ? array[2] : null }));
			}
		}

		public override IEnumerable<EventOption> AllPossibleOptions => (IEnumerable<EventOption>)(object)new EventOption[10]
		{
			((AncientEventModel)this).RelicOption<GalbrenasKarmaFire>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ChimerasHeart>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<HarpyienSharpFeather>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<DullahansFlesh>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<BalorsEye>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<NamelessShadowHusk>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<SweetDreams>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ExtraThickMilkshakeShavedIce>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<StoneRose>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<FirstBloodOath>("INITIAL", (string)null)
		};

		public override bool IsValidForAct(ActModel act)
		{
			return ActModelExtensions.ActNumber(act) == 2;
		}

		public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
		{
			if (ActModelExtensions.ActNumber(act) != 2)
			{
				return false;
			}
			Dictionary<Type, int> dictionary = new Dictionary<Type, int>();
			if (WuwancientsConfig.强制菲比出现)
			{
				dictionary[typeof(Febe)] = 100;
			}
			else if (WuwancientsConfig.菲比是否出现)
			{
				dictionary[typeof(Febe)] = 1;
			}
			if (WuwancientsConfig.强制尤诺出现)
			{
				dictionary[typeof(Youno)] = 100;
			}
			else if (WuwancientsConfig.尤诺是否出现)
			{
				dictionary[typeof(Youno)] = 1;
			}
			if (WuwancientsConfig.强制嘉贝莉娜出现)
			{
				dictionary[typeof(Galbrena)] = 100;
			}
			else if (WuwancientsConfig.嘉贝莉娜是否出现)
			{
				dictionary[typeof(Galbrena)] = 1;
			}
			if (WuwancientsConfig.强制赞妮出现)
			{
				dictionary[typeof(Zani)] = 100;
			}
			else if (WuwancientsConfig.赞妮是否出现)
			{
				dictionary[typeof(Zani)] = 1;
			}
			return AncientPool.IsSelected(typeof(Galbrena), act, dictionary);
		}

		protected override AncientDialogueSet DefineDialogues()
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Expected O, but got Unknown
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Expected O, but got Unknown
			AncientDialogueSet val = ((CustomAncientModel)this).DefineDialogues();
			int i;
			for (i = 0; FirstVisitLineExists(i); i++)
			{
			}
			if (i <= 1)
			{
				return val;
			}
			AncientDialogue firstVisitEverDialogue = new AncientDialogue(Enumerable.Repeat("", i).ToArray());
			AncientDialogueSet val2 = new AncientDialogueSet();
			val2.set_FirstVisitEverDialogue(firstVisitEverDialogue);
			val2.set_CharacterDialogues(val.CharacterDialogues);
			val2.set_AgnosticDialogues(val.AgnosticDialogues);
			return val2;
		}

		private bool FirstVisitLineExists(int index)
		{
			string text = ((AbstractModel)this).Id.Entry + ".talk.firstVisitEver.0-" + index;
			return LocString.Exists("ancients", text + ".ancient") || LocString.Exists("ancients", text + ".char") || LocString.Exists("ancients", text + "r.ancient") || LocString.Exists("ancients", text + "r.char");
		}

		public Galbrena()
			: base(true, false)
		{
		}
	}
	public class Linna : CustomAncientModel
	{
		public override Color ButtonColor => new Color(0.78f, 0.95f, 0.15f, 0.7f);

		public override Color DialogueColor => new Color(1f, 0.85f, 0.1f, 1f);

		public override string? CustomMapIconPath => "res://wuwancients/images/icons/lina_map.png";

		public override string? CustomMapIconOutlinePath => "res://wuwancients/images/icons/lina_map_outline.png";

		public override string? CustomRunHistoryIconPath => "res://wuwancients/images/icons/lina_map_icon.png";

		public override string? CustomRunHistoryIconOutlinePath => "res://wuwancients/images/icons/lina_map_icon.png";

		public override string? CustomScenePath => AncientScenePicker.Pick(((AbstractModel)this).Id.Entry, WuwancientsConfig.琳奈场景, "res://wuwancients/scenes/lina.tscn");

		protected override OptionPools MakeOptionPools
		{
			get
			{
				//IL_0131: Unknown result type (might be due to invalid IL or missing references)
				//IL_0137: Expected O, but got Unknown
				List<AncientOption> items = new List<AncientOption>
				{
					CustomAncientModel.AncientOption<CactusHugPillow>(1, (Func<CactusHugPillow, RelicModel>)null, (Func<CactusHugPillow, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<ColorfulCupNoodles>(1, (Func<ColorfulCupNoodles, RelicModel>)null, (Func<ColorfulCupNoodles, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<LinnaDango>(1, (Func<LinnaDango, RelicModel>)null, (Func<LinnaDango, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<FrostSignalFlower>(1, (Func<FrostSignalFlower, RelicModel>)null, (Func<FrostSignalFlower, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<RadiantGlow>(1, (Func<RadiantGlow, RelicModel>)null, (Func<RadiantGlow, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<ExpeditionKey>(1, (Func<ExpeditionKey, RelicModel>)null, (Func<ExpeditionKey, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<PaintCan>(1, (Func<PaintCan, RelicModel>)null, (Func<PaintCan, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<DingDongPendant>(1, (Func<DingDongPendant, RelicModel>)null, (Func<DingDongPendant, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<ViolationPartsSet>(1, (Func<ViolationPartsSet, RelicModel>)null, (Func<ViolationPartsSet, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<ZheyingWancai>(1, (Func<ZheyingWancai, RelicModel>)null, (Func<ZheyingWancai, IEnumerable<RelicModel>>)null)
				};
				List<AncientOption> source = AncientPool.ShuffleDeterministic(items, ((AbstractModel)this).Id.Entry);
				AncientOption[] array = source.Where((AncientOption o) => o != null).Take(3).Cast<AncientOption>()
					.ToArray();
				return new OptionPools(CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length != 0) ? array[0] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 1) ? array[1] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 2) ? array[2] : null }));
			}
		}

		public override IEnumerable<EventOption> AllPossibleOptions => (IEnumerable<EventOption>)(object)new EventOption[10]
		{
			((AncientEventModel)this).RelicOption<CactusHugPillow>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ColorfulCupNoodles>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<LinnaDango>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<FrostSignalFlower>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<RadiantGlow>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ExpeditionKey>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<PaintCan>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<DingDongPendant>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ViolationPartsSet>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ZheyingWancai>("INITIAL", (string)null)
		};

		public override bool IsValidForAct(ActModel act)
		{
			return ActModelExtensions.ActNumber(act) == 3;
		}

		public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
		{
			if (ActModelExtensions.ActNumber(act) != 3)
			{
				return false;
			}
			Dictionary<Type, int> dictionary = new Dictionary<Type, int>();
			if (WuwancientsConfig.强制琳奈出现)
			{
				dictionary[typeof(Linna)] = 100;
			}
			else if (WuwancientsConfig.琳奈是否出现)
			{
				dictionary[typeof(Linna)] = 1;
			}
			if (WuwancientsConfig.强制奥古斯塔出现)
			{
				dictionary[typeof(Augusta)] = 100;
			}
			else if (WuwancientsConfig.奥古斯塔是否出现)
			{
				dictionary[typeof(Augusta)] = 1;
			}
			if (WuwancientsConfig.强制千咲出现)
			{
				dictionary[typeof(Chisa)] = 100;
			}
			else if (WuwancientsConfig.千咲是否出现)
			{
				dictionary[typeof(Chisa)] = 1;
			}
			if (WuwancientsConfig.强制弗洛洛出现)
			{
				dictionary[typeof(Froro)] = 100;
			}
			else if (WuwancientsConfig.弗洛洛是否出现)
			{
				dictionary[typeof(Froro)] = 1;
			}
			if (WuwancientsConfig.强制尤诺出现)
			{
				dictionary[typeof(Youno)] = 100;
			}
			else if (WuwancientsConfig.尤诺是否出现)
			{
				dictionary[typeof(Youno)] = 1;
			}
			return AncientPool.IsSelected(typeof(Linna), act, dictionary);
		}

		protected override AncientDialogueSet DefineDialogues()
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Expected O, but got Unknown
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Expected O, but got Unknown
			AncientDialogueSet val = ((CustomAncientModel)this).DefineDialogues();
			int i;
			for (i = 0; FirstVisitLineExists(i); i++)
			{
			}
			if (i <= 1)
			{
				return val;
			}
			AncientDialogue firstVisitEverDialogue = new AncientDialogue(Enumerable.Repeat("", i).ToArray());
			AncientDialogueSet val2 = new AncientDialogueSet();
			val2.set_FirstVisitEverDialogue(firstVisitEverDialogue);
			val2.set_CharacterDialogues(val.CharacterDialogues);
			val2.set_AgnosticDialogues(val.AgnosticDialogues);
			return val2;
		}

		private bool FirstVisitLineExists(int index)
		{
			string text = ((AbstractModel)this).Id.Entry + ".talk.firstVisitEver.0-" + index;
			return LocString.Exists("ancients", text + ".ancient") || LocString.Exists("ancients", text + ".char") || LocString.Exists("ancients", text + "r.ancient") || LocString.Exists("ancients", text + "r.char");
		}

		public Linna()
			: base(true, false)
		{
		}
	}
	public class Shorekeeper : CustomAncientModel
	{
		private List<EventOption>? _modifierOptions;

		private List<EventOption> ModifierOptions
		{
			get
			{
				((AbstractModel)this).AssertMutable();
				if (_modifierOptions == null)
				{
					_modifierOptions = new List<EventOption>();
				}
				return _modifierOptions;
			}
		}

		public override Color ButtonColor => new Color(0.15f, 0.35f, 0.6f, 0.6f);

		public override Color DialogueColor => new Color(0.05f, 0.1f, 0.2f, 1f);

		public override string? CustomMapIconPath => "res://wuwancients/images/icons/shorekeeper_map.png";

		public override string? CustomMapIconOutlinePath => "res://wuwancients/images/icons/shorekeeper_map_outline.png";

		public override string? CustomRunHistoryIconPath => "res://wuwancients/images/icons/shorekeeper_map_icon.png";

		public override string? CustomRunHistoryIconOutlinePath => "res://wuwancients/images/icons/shorekeeper_map_icon.png";

		public override string? CustomScenePath => AncientScenePicker.Pick(((AbstractModel)this).Id.Entry, WuwancientsConfig.守岸人场景, "res://wuwancients/scenes/shorekeeper.tscn");

		protected override OptionPools MakeOptionPools
		{
			get
			{
				//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
				//IL_02b9: Expected O, but got Unknown
				List<AncientOption> list = new List<AncientOption>
				{
					CustomAncientModel.AncientOption<SnowStew>(1, (Func<SnowStew, RelicModel>)null, (Func<SnowStew, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<Texture1>(1, (Func<Texture1, RelicModel>)null, (Func<Texture1, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<StarSequenceHarmony>(1, (Func<StarSequenceHarmony, RelicModel>)null, (Func<StarSequenceHarmony, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<GoatBaaGreeting>(1, (Func<GoatBaaGreeting, RelicModel>)null, (Func<GoatBaaGreeting, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<ButterflyPrint>(1, (Func<ButterflyPrint, RelicModel>)null, (Func<ButterflyPrint, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<WhiteRibbon>(1, (Func<WhiteRibbon, RelicModel>)null, (Func<WhiteRibbon, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<YokuuTopology>(1, (Func<YokuuTopology, RelicModel>)null, (Func<YokuuTopology, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<GiftOfQiqiu>(1, (Func<GiftOfQiqiu, RelicModel>)null, (Func<GiftOfQiqiu, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<BurySpiritGreeting>(1, (Func<BurySpiritGreeting, RelicModel>)null, (Func<BurySpiritGreeting, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<AutumnWaterGreeting>(1, (Func<AutumnWaterGreeting, RelicModel>)null, (Func<AutumnWaterGreeting, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<We>(1, (Func<We, RelicModel>)null, (Func<We, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<MissetFallacy>(1, (Func<MissetFallacy, RelicModel>)null, (Func<MissetFallacy, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<SobUnderWail>(1, (Func<SobUnderWail, RelicModel>)null, (Func<SobUnderWail, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<ShorekeeperDango>(1, (Func<ShorekeeperDango, RelicModel>)null, (Func<ShorekeeperDango, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<MottaliBankCard>(1, (Func<MottaliBankCard, RelicModel>)null, (Func<MottaliBankCard, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<EndlessLoop>(1, (Func<EndlessLoop, RelicModel>)null, (Func<EndlessLoop, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<TetisFlower>(1, (Func<TetisFlower, RelicModel>)null, (Func<TetisFlower, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<FireDevilGreeting>(1, (Func<FireDevilGreeting, RelicModel>)null, (Func<FireDevilGreeting, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<CamelliaGreeting>(1, (Func<CamelliaGreeting, RelicModel>)null, (Func<CamelliaGreeting, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<FeisaliesGift>(1, (Func<FeisaliesGift, RelicModel>)null, (Func<FeisaliesGift, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<Prayer>(1, (Func<Prayer, RelicModel>)null, (Func<Prayer, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<MottaliGift>(1, (Func<MottaliGift, RelicModel>)null, (Func<MottaliGift, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<ZannisSalaryCard>(1, (Func<ZannisSalaryCard, RelicModel>)null, (Func<ZannisSalaryCard, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<MemoryStarAnchor>(1, (Func<MemoryStarAnchor, RelicModel>)null, (Func<MemoryStarAnchor, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<JianxinGreeting>(1, (Func<JianxinGreeting, RelicModel>)null, (Func<JianxinGreeting, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<LinaGreeting>(1, (Func<LinaGreeting, RelicModel>)null, (Func<LinaGreeting, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<SuisuiGreeting>(1, (Func<SuisuiGreeting, RelicModel>)null, (Func<SuisuiGreeting, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<SproutGreeting>(1, (Func<SproutGreeting, RelicModel>)null, (Func<SproutGreeting, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<LingYinGreeting>(1, (Func<LingYinGreeting, RelicModel>)null, (Func<LingYinGreeting, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<HellhoundGreeting>(1, (Func<HellhoundGreeting, RelicModel>)null, (Func<HellhoundGreeting, IEnumerable<RelicModel>>)null)
				};
				RunState obj = RunManager.Instance.DebugOnlyGetState();
				if (obj != null && obj.Players.Count > 1)
				{
					list.Add(CustomAncientModel.AncientOption<EchoAbsorptionDevice>(1, (Func<EchoAbsorptionDevice, RelicModel>)null, (Func<EchoAbsorptionDevice, IEnumerable<RelicModel>>)null));
					list.Add(CustomAncientModel.AncientOption<PeaceCharm>(1, (Func<PeaceCharm, RelicModel>)null, (Func<PeaceCharm, IEnumerable<RelicModel>>)null));
					list.Add(CustomAncientModel.AncientOption<PaperRole>(1, (Func<PaperRole, RelicModel>)null, (Func<PaperRole, IEnumerable<RelicModel>>)null));
				}
				List<AncientOption> source = AncientPool.ShuffleDeterministic(list, ((AbstractModel)this).Id.Entry);
				AncientOption[] array = (from o in source.Take(3)
					where o != null
					select o).Cast<AncientOption>().ToArray();
				return new OptionPools(CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length != 0) ? array[0] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 1) ? array[1] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 2) ? array[2] : null }));
			}
		}

		public override IEnumerable<EventOption> AllPossibleOptions => (IEnumerable<EventOption>)(object)new EventOption[33]
		{
			((AncientEventModel)this).RelicOption<SnowStew>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<Texture1>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<StarSequenceHarmony>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<GoatBaaGreeting>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ButterflyPrint>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<WhiteRibbon>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<YokuuTopology>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<GiftOfQiqiu>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<BurySpiritGreeting>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<AutumnWaterGreeting>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<We>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<MissetFallacy>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<SobUnderWail>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ShorekeeperDango>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<MottaliBankCard>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<EndlessLoop>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<TetisFlower>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<FireDevilGreeting>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<CamelliaGreeting>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<FeisaliesGift>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<Prayer>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<MottaliGift>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ZannisSalaryCard>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<MemoryStarAnchor>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<JianxinGreeting>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<LinaGreeting>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<SuisuiGreeting>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<SproutGreeting>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<LingYinGreeting>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<HellhoundGreeting>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<EchoAbsorptionDevice>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<PeaceCharm>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<PaperRole>("INITIAL", (string)null)
		};

		public override bool IsValidForAct(ActModel act)
		{
			return ActModelExtensions.ActNumber(act) == 1;
		}

		public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
		{
			if (ActModelExtensions.ActNumber(act) != 1)
			{
				return false;
			}
			if (WuwancientsConfig.强制守岸人出现)
			{
				return true;
			}
			if (!WuwancientsConfig.守岸人是否出现)
			{
				return false;
			}
			return AncientPool.CoinFlip(((AbstractModel)this).Id.Entry);
		}

		protected override async Task BeforeEventStarted(bool isPreFinished)
		{
			if (!isPreFinished)
			{
				Player owner = ((EventModel)this).Owner;
				if (((owner != null) ? owner.Creature : null) == null)
				{
					return;
				}
				((EventModel)this).Owner.Creature.SetCurrentHpInternal(0m);
			}
			await <>n__0(isPreFinished);
		}

		protected override IReadOnlyList<EventOption> GenerateInitialOptions()
		{
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Expected O, but got Unknown
			if (((EventModel)this).Owner.RunState.Modifiers.Count > 0)
			{
				foreach (ModifierModel modifier in ((EventModel)this).Owner.RunState.Modifiers)
				{
					Func<Task> neowOption = modifier.GenerateNeowOption((EventModel)(object)this);
					if (neowOption != null)
					{
						int index = ModifierOptions.Count;
						ModifierOptions.Add(new EventOption((EventModel)(object)this, (Func<Task>)(() => OnModifierOptionSelected(neowOption, index)), modifier.NeowOptionTitle, modifier.NeowOptionDescription, ((AbstractModel)modifier).Id.Entry, (IEnumerable<IHoverTip>)modifier.HoverTips.ToArray()));
					}
				}
				if (ModifierOptions.Count > 0)
				{
					return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ModifierOptions[0] };
				}
				return Array.Empty<EventOption>();
			}
			return ((CustomAncientModel)this).GenerateInitialOptions();
		}

		private async Task OnModifierOptionSelected(Func<Task> modifierFunc, int index)
		{
			await modifierFunc();
			if (index + 1 >= ModifierOptions.Count)
			{
				IReadOnlyList<EventOption> relicOptions = <>n__1();
				if (relicOptions.Count > 0)
				{
					((EventModel)this).SetEventState(((EventModel)this).InitialDescription, (IEnumerable<EventOption>)relicOptions);
				}
				else
				{
					((EventModel)this).SetEventFinished(((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.DONE.description"));
				}
			}
			else
			{
				((EventModel)this).SetEventState(((EventModel)this).InitialDescription, (IEnumerable<EventOption>)(object)new EventOption[1] { ModifierOptions[index + 1] });
			}
		}

		public Shorekeeper()
			: base(true, false)
		{
		}

		[CompilerGenerated]
		[DebuggerHidden]
		private Task <>n__0(bool isPreFinished)
		{
			return ((AncientEventModel)this).BeforeEventStarted(isPreFinished);
		}

		[CompilerGenerated]
		[DebuggerHidden]
		private IReadOnlyList<EventOption> <>n__1()
		{
			return ((CustomAncientModel)this).GenerateInitialOptions();
		}
	}
	public class Youno : CustomAncientModel
	{
		private static readonly HashSet<string> VanillaIds = new HashSet<string> { "IRONCLAD", "SILENT", "DEFECT", "REGENT", "NECROBINDER" };

		private static readonly HashSet<Type> BlockedRelicTypes = new HashSet<Type> { typeof(HiddenSeaRecord) };

		public override Color ButtonColor => new Color(0.05f, 0.1f, 0.45f, 0.7f);

		public override Color DialogueColor => new Color(0.02f, 0.05f, 0.2f, 1f);

		public override string? CustomMapIconPath => "res://wuwancients/images/icons/youno_map.png";

		public override string? CustomMapIconOutlinePath => "res://wuwancients/images/icons/youno_map_outline.png";

		public override string? CustomRunHistoryIconPath => "res://wuwancients/images/icons/youno_map_icon.png";

		public override string? CustomRunHistoryIconOutlinePath => "res://wuwancients/images/icons/youno_map_icon.png";

		public override string? CustomScenePath => AncientScenePicker.Pick(((AbstractModel)this).Id.Entry, WuwancientsConfig.尤诺场景, "res://wuwancients/scenes/youno.tscn", "res://wuwancients/scenes/youno_b.tscn");

		protected override OptionPools MakeOptionPools
		{
			get
			{
				//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
				//IL_00f1: Expected O, but got Unknown
				List<AncientOption> list = new List<AncientOption>();
				AddIfAllowed<MoonviewFlower>(list);
				AddIfAllowed<AnnotationsOfEternalPreservation>(list);
				AddIfAllowed<DirectionalAnchorFragment>(list);
				AddIfAllowed<WaxAndWane>(list);
				AddIfAllowed<IfMeasuredInAnInstant>(list);
				AddIfAllowed<LightlyTossedThoughts>(list);
				AddIfAllowed<YounoDumpling>(list);
				AddIfAllowed<SweetPickledOlive>(list);
				AddIfAllowed<BrokenRebirth>(list);
				AddIfAllowed<MoonstoneBracelet>(list);
				List<AncientOption> source = AncientPool.ShuffleDeterministic(list, ((AbstractModel)this).Id.Entry);
				AncientOption[] array = source.Where((AncientOption o) => o != null).Take(3).Cast<AncientOption>()
					.ToArray();
				return new OptionPools(CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length != 0) ? array[0] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 1) ? array[1] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 2) ? array[2] : null }));
			}
		}

		public override IEnumerable<EventOption> AllPossibleOptions => (IEnumerable<EventOption>)(object)new EventOption[10]
		{
			((AncientEventModel)this).RelicOption<MoonviewFlower>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<AnnotationsOfEternalPreservation>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<DirectionalAnchorFragment>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<WaxAndWane>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<IfMeasuredInAnInstant>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<LightlyTossedThoughts>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<YounoDumpling>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<SweetPickledOlive>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<BrokenRebirth>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<MoonstoneBracelet>("INITIAL", (string)null)
		};

		private bool IsModPlayer()
		{
			RunState val = RunManager.Instance.DebugOnlyGetState();
			if (val == null)
			{
				return false;
			}
			Player val2 = val.Players?.FirstOrDefault();
			object obj;
			if (val2 == null)
			{
				obj = null;
			}
			else
			{
				CharacterModel character = val2.Character;
				if (character == null)
				{
					obj = null;
				}
				else
				{
					ModelId id = ((AbstractModel)character).Id;
					obj = ((id != null) ? id.Entry : null);
				}
			}
			if (obj == null)
			{
				return false;
			}
			return !VanillaIds.Contains(((AbstractModel)val2.Character).Id.Entry.ToUpperInvariant());
		}

		private bool ShouldAdd(Type relicType)
		{
			return !IsModPlayer() || !BlockedRelicTypes.Contains(relicType);
		}

		public override bool IsValidForAct(ActModel act)
		{
			int num = ActModelExtensions.ActNumber(act);
			if ((uint)(num - 2) <= 1u)
			{
				return true;
			}
			return false;
		}

		public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
		{
			int num = ActModelExtensions.ActNumber(act);
			if ((uint)(num - 2) > 1u)
			{
				return false;
			}
			Dictionary<Type, int> dictionary = new Dictionary<Type, int>();
			if (num == 2)
			{
				if (WuwancientsConfig.强制菲比出现)
				{
					dictionary[typeof(Febe)] = 100;
				}
				else if (WuwancientsConfig.菲比是否出现)
				{
					dictionary[typeof(Febe)] = 1;
				}
				if (WuwancientsConfig.强制尤诺出现)
				{
					dictionary[typeof(Youno)] = 100;
				}
				else if (WuwancientsConfig.尤诺是否出现)
				{
					dictionary[typeof(Youno)] = 1;
				}
				if (WuwancientsConfig.强制嘉贝莉娜出现)
				{
					dictionary[typeof(Galbrena)] = 100;
				}
				else if (WuwancientsConfig.嘉贝莉娜是否出现)
				{
					dictionary[typeof(Galbrena)] = 1;
				}
				if (WuwancientsConfig.强制赞妮出现)
				{
					dictionary[typeof(Zani)] = 100;
				}
				else if (WuwancientsConfig.赞妮是否出现)
				{
					dictionary[typeof(Zani)] = 1;
				}
			}
			else
			{
				if (WuwancientsConfig.强制奥古斯塔出现)
				{
					dictionary[typeof(Augusta)] = 100;
				}
				else if (WuwancientsConfig.奥古斯塔是否出现)
				{
					dictionary[typeof(Augusta)] = 1;
				}
				if (WuwancientsConfig.强制千咲出现)
				{
					dictionary[typeof(Chisa)] = 100;
				}
				else if (WuwancientsConfig.千咲是否出现)
				{
					dictionary[typeof(Chisa)] = 1;
				}
				if (WuwancientsConfig.强制弗洛洛出现)
				{
					dictionary[typeof(Froro)] = 100;
				}
				else if (WuwancientsConfig.弗洛洛是否出现)
				{
					dictionary[typeof(Froro)] = 1;
				}
				if (WuwancientsConfig.强制琳奈出现)
				{
					dictionary[typeof(Linna)] = 100;
				}
				else if (WuwancientsConfig.琳奈是否出现)
				{
					dictionary[typeof(Linna)] = 1;
				}
				if (WuwancientsConfig.强制尤诺出现)
				{
					dictionary[typeof(Youno)] = 100;
				}
				else if (WuwancientsConfig.尤诺是否出现)
				{
					dictionary[typeof(Youno)] = 1;
				}
			}
			return AncientPool.IsSelected(typeof(Youno), act, dictionary);
		}

		private void AddIfAllowed<T>(List<AncientOption?> list) where T : RelicModel
		{
			if (ShouldAdd(typeof(T)))
			{
				list.Add(CustomAncientModel.AncientOption<T>(1, (Func<T, RelicModel>)null, (Func<T, IEnumerable<RelicModel>>)null));
			}
		}

		public Youno()
			: base(true, false)
		{
		}
	}
	public class Zani : CustomAncientModel
	{
		public override Color ButtonColor => new Color(0.45f, 0.08f, 0.1f, 0.6f);

		public override Color DialogueColor => new Color(0.12f, 0.05f, 0.05f, 1f);

		public override string? CustomMapIconPath => "res://wuwancients/images/icons/zani_map.png";

		public override string? CustomMapIconOutlinePath => "res://wuwancients/images/icons/zani_map_outline.png";

		public override string? CustomRunHistoryIconPath => "res://wuwancients/images/icons/zani_map_icon.png";

		public override string? CustomRunHistoryIconOutlinePath => "res://wuwancients/images/icons/zani_map_icon.png";

		public override string? CustomScenePath => AncientScenePicker.Pick(((AbstractModel)this).Id.Entry, WuwancientsConfig.赞妮场景, "res://wuwancients/scenes/zani.tscn", "res://wuwancients/scenes/zani_b.tscn");

		protected override OptionPools MakeOptionPools
		{
			get
			{
				//IL_0131: Unknown result type (might be due to invalid IL or missing references)
				//IL_0137: Expected O, but got Unknown
				List<AncientOption> items = new List<AncientOption>
				{
					CustomAncientModel.AncientOption<PersonalMemo>(1, (Func<PersonalMemo, RelicModel>)null, (Func<PersonalMemo, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<TimeManagementMaster>(1, (Func<TimeManagementMaster, RelicModel>)null, (Func<TimeManagementMaster, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<ThankYouGift>(1, (Func<ThankYouGift, RelicModel>)null, (Func<ThankYouGift, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<AssortedMeatSauceNoodles>(1, (Func<AssortedMeatSauceNoodles, RelicModel>)null, (Func<AssortedMeatSauceNoodles, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<SwordCalamus>(1, (Func<SwordCalamus, RelicModel>)null, (Func<SwordCalamus, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<FlameJudgment>(1, (Func<FlameJudgment, RelicModel>)null, (Func<FlameJudgment, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<DemonTax>(1, (Func<DemonTax, RelicModel>)null, (Func<DemonTax, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<FlameClaw>(1, (Func<FlameClaw, RelicModel>)null, (Func<FlameClaw, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<ZaniDango>(1, (Func<ZaniDango, RelicModel>)null, (Func<ZaniDango, IEnumerable<RelicModel>>)null),
					CustomAncientModel.AncientOption<OxHorsePlushie>(1, (Func<OxHorsePlushie, RelicModel>)null, (Func<OxHorsePlushie, IEnumerable<RelicModel>>)null)
				};
				List<AncientOption> source = AncientPool.ShuffleDeterministic(items, ((AbstractModel)this).Id.Entry);
				AncientOption[] array = (from o in source.Take(3)
					where o != null
					select o).Cast<AncientOption>().ToArray();
				return new OptionPools(CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length != 0) ? array[0] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 1) ? array[1] : null }), CustomAncientModel.MakePool((AncientOption[])(object)new AncientOption[1] { (array.Length > 2) ? array[2] : null }));
			}
		}

		public override IEnumerable<EventOption> AllPossibleOptions => (IEnumerable<EventOption>)(object)new EventOption[10]
		{
			((AncientEventModel)this).RelicOption<PersonalMemo>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<TimeManagementMaster>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ThankYouGift>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<AssortedMeatSauceNoodles>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<SwordCalamus>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<FlameJudgment>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<DemonTax>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<FlameClaw>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<ZaniDango>("INITIAL", (string)null),
			((AncientEventModel)this).RelicOption<OxHorsePlushie>("INITIAL", (string)null)
		};

		public override bool IsValidForAct(ActModel act)
		{
			return ActModelExtensions.ActNumber(act) == 2;
		}

		public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
		{
			if (ActModelExtensions.ActNumber(act) != 2)
			{
				return false;
			}
			Dictionary<Type, int> dictionary = new Dictionary<Type, int>();
			if (WuwancientsConfig.强制菲比出现)
			{
				dictionary[typeof(Febe)] = 100;
			}
			else if (WuwancientsConfig.菲比是否出现)
			{
				dictionary[typeof(Febe)] = 1;
			}
			if (WuwancientsConfig.强制尤诺出现)
			{
				dictionary[typeof(Youno)] = 100;
			}
			else if (WuwancientsConfig.尤诺是否出现)
			{
				dictionary[typeof(Youno)] = 1;
			}
			if (WuwancientsConfig.强制嘉贝莉娜出现)
			{
				dictionary[typeof(Galbrena)] = 100;
			}
			else if (WuwancientsConfig.嘉贝莉娜是否出现)
			{
				dictionary[typeof(Galbrena)] = 1;
			}
			if (WuwancientsConfig.强制赞妮出现)
			{
				dictionary[typeof(Zani)] = 100;
			}
			else if (WuwancientsConfig.赞妮是否出现)
			{
				dictionary[typeof(Zani)] = 1;
			}
			return AncientPool.IsSelected(typeof(Zani), act, dictionary);
		}

		public Zani()
			: base(true, false)
		{
		}
	}
}
