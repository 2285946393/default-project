using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace ModConfig;

internal static class SettingsTabInjector
{
	private sealed class LiveBinding
	{
		public Func<object, bool> Apply { get; }

		public LiveBinding(Func<object, bool> apply)
		{
			Apply = apply;
		}
	}

	private sealed class UiUpdateGuard
	{
		public bool Suppress { get; set; }
	}

	[CompilerGenerated]
	private static class _003C_003EO
	{
		public static NodeAddedEventHandler _003C0_003E__OnNodeAdded;

		public static Action _003C1_003E__OnLanguageChanged;
	}

	private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

	private static readonly List<WeakReference<VBoxContainer>> _allContainers = new List<WeakReference<VBoxContainer>>();

	private static readonly List<WeakReference<NSettingsTab>> _allTabs = new List<WeakReference<NSettingsTab>>();

	private static bool _i18nSubscribed;

	private static Button? _activeKeyBindButton;

	private static string _activeKeyBindModId = "";

	private static ConfigEntry? _activeKeyBindEntry;

	private static readonly Dictionary<(string ModId, string Key), List<LiveBinding>> _liveBindings = new Dictionary<(string, string), List<LiveBinding>>();

	private static Font? _gameFont;

	private static readonly Color CreamGold = new Color("D4C88E");

	private static readonly Color DimText = new Color("8A7E5C");

	private static readonly Color TextColor = new Color(0.9f, 0.85f, 0.75f, 1f);

	private static readonly Color ResetColor = new Color(0.8f, 0.5f, 0.4f, 1f);

	private static readonly Color KeyBindListening = new Color(1f, 0.85f, 0.3f, 1f);

	private static readonly Color ModHeaderBg = new Color("2C434F");

	private static readonly Color RowSeparatorColor = new Color("2C434F", 0.5f);

	private static KeyCaptureNode? _captureNode;

	internal static void Initialize()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		SceneTree val = (SceneTree)Engine.GetMainLoop();
		object obj = _003C_003EO._003C0_003E__OnNodeAdded;
		if (obj == null)
		{
			NodeAddedEventHandler val2 = OnNodeAdded;
			_003C_003EO._003C0_003E__OnNodeAdded = val2;
			obj = (object)val2;
		}
		val.NodeAdded += (NodeAddedEventHandler)obj;
	}

	private static void OnNodeAdded(Node node)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		Node node2 = node;
		if (node2 is NSettingsTabManager && node2.GetNodeOrNull(NodePath.op_Implicit("Mods")) == null)
		{
			((GodotObject)node2).Connect(StringName.op_Implicit("ready"), Callable.From((Action)delegate
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_0010: Expected O, but got Unknown
				InjectModsTab((NSettingsTabManager)node2);
			}), 4u);
		}
	}

	private static void InjectModsTab(NSettingsTabManager tabManager)
	{
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Expected O, but got Unknown
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Expected O, but got Unknown
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Expected O, but got Unknown
		//IL_0330: Unknown result type (might be due to invalid IL or missing references)
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_0381: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_039d: Unknown result type (might be due to invalid IL or missing references)
		NSettingsTabManager tabManager2 = tabManager;
		try
		{
			FieldInfo field = typeof(NSettingsTabManager).GetField("_tabs", PrivateInstance);
			if (field == null)
			{
				MainFile.Log.Error("Cannot find _tabs field on NSettingsTabManager", 1);
			}
			else if (field.GetValue(tabManager2) is IDictionary { Count: not 0 } dictionary)
			{
				NSettingsTab val = null;
				NSettingsPanel val2 = null;
				{
					IDictionaryEnumerator dictionaryEnumerator = dictionary.GetEnumerator();
					try
					{
						if (dictionaryEnumerator.MoveNext())
						{
							DictionaryEntry dictionaryEntry = (DictionaryEntry)dictionaryEnumerator.Current;
							object key = dictionaryEntry.Key;
							val = (NSettingsTab)((key is NSettingsTab) ? key : null);
							object? value = dictionaryEntry.Value;
							val2 = (NSettingsPanel)((value is NSettingsPanel) ? value : null);
						}
					}
					finally
					{
						IDisposable disposable = dictionaryEnumerator as IDisposable;
						if (disposable != null)
						{
							disposable.Dispose();
						}
					}
				}
				if (val == null || val2 == null)
				{
					MainFile.Log.Error("Could not find existing tab/panel to clone", 1);
					return;
				}
				NSettingsTab modsTab = (NSettingsTab)((Node)val).Duplicate(15);
				((Node)modsTab).Name = StringName.op_Implicit("Mods");
				TextureRect nodeOrNull = ((Node)modsTab).GetNodeOrNull<TextureRect>(NodePath.op_Implicit("TabImage"));
				Material obj = ((nodeOrNull != null) ? ((CanvasItem)nodeOrNull).Material : null);
				ShaderMaterial val3 = (ShaderMaterial)(object)((obj is ShaderMaterial) ? obj : null);
				if (val3 != null)
				{
					((CanvasItem)nodeOrNull).Material = (Material)(ShaderMaterial)((Resource)val3).Duplicate(false);
				}
				((Node)tabManager2).AddChild((Node)(object)modsTab, false, (InternalMode)0);
				modsTab.SetLabel(I18n.TabMods);
				modsTab.Deselect();
				PositionNewTab(dictionary, modsTab);
				NSettingsPanel val4 = (NSettingsPanel)((Node)val2).Duplicate(15);
				((Node)val4).Name = StringName.op_Implicit("ModsSettings");
				((CanvasItem)val4).Visible = false;
				Control val5 = CreatePreReadyFocusSentinel(val2);
				VBoxContainer content = val2.Content;
				StringName val6 = ((content != null) ? ((Node)content).Name : null);
				VBoxContainer val7 = null;
				Node[] array = ((IEnumerable<Node>)((Node)val4).GetChildren(false)).ToArray();
				foreach (Node val8 in array)
				{
					if (val8 is VBoxContainer && ((val6 != (StringName)null && val8.Name == val6) || (val6 == (StringName)null && val7 == null)))
					{
						VBoxContainer val9 = (VBoxContainer)(object)((val8 is VBoxContainer) ? val8 : null);
						if (val9 != null)
						{
							val7 = val9;
							Node[] array2 = ((IEnumerable<Node>)((Node)val9).GetChildren(false)).ToArray();
							foreach (Node val10 in array2)
							{
								((Node)val9).RemoveChild(val10);
								((GodotObject)val10).Free();
							}
							continue;
						}
					}
					((Node)val4).RemoveChild(val8);
					((GodotObject)val8).Free();
				}
				if (val7 != null && val5 != null)
				{
					((Node)val5).Name = StringName.op_Implicit("__PreReadyFocusSentinel");
					((CanvasItem)val5).Visible = false;
					val5.MouseFilter = (MouseFilterEnum)2;
					((Node)val7).AddChild((Node)(object)val5, false, (InternalMode)0);
				}
				((Node)val2).GetParent().AddChild((Node)(object)val4, false, (InternalMode)0);
				if (val7 == null)
				{
					val7 = val4.Content;
				}
				_allContainers.Add(new WeakReference<VBoxContainer>(val7));
				dictionary.Add(modsTab, val4);
				((GodotObject)modsTab).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)delegate
				{
					//IL_001e: Unknown result type (might be due to invalid IL or missing references)
					//IL_0023: Unknown result type (might be due to invalid IL or missing references)
					//IL_0028: Unknown result type (might be due to invalid IL or missing references)
					try
					{
						((GodotObject)tabManager2).Call(StringName.op_Implicit("SwitchTabTo"), (Variant[])(object)new Variant[1] { Variant.op_Implicit((GodotObject)(object)modsTab) });
					}
					catch (Exception value4)
					{
						MainFile.Log.Error($"Tab switch failed: {value4}", 1);
					}
				}), 0u);
				_allTabs.Add(new WeakReference<NSettingsTab>(modsTab));
				if (!_i18nSubscribed)
				{
					I18n.Changed += OnLanguageChanged;
					_i18nSubscribed = true;
				}
				try
				{
					float num = ((Control)val2).Size.Y;
					if (num < 100f)
					{
						num = ((Node)val4).GetParent<Control>().Size.Y * 0.85f;
					}
					((Control)val4).Size = new Vector2(((Control)val4).Size.X, num);
					Viewport viewport = ((Node)val4).GetViewport();
					if (viewport != null)
					{
						OverrideRefreshSize(viewport, val4, num);
					}
				}
				catch (Exception value2)
				{
					MainFile.Log.Error($"Failed to cap panel height: {value2}", 1);
				}
				CacheGameFont(val2);
				PopulateInto(val7);
				RebuildFocusTargets(val4, val7, val5);
				MainFile.Log.Info("Mods tab injected into settings screen!", 1);
			}
			else
			{
				MainFile.Log.Error("_tabs dictionary is empty or null", 1);
			}
		}
		catch (Exception value3)
		{
			MainFile.Log.Error($"Failed to inject Mods tab: {value3}", 1);
		}
	}

	private static void PositionNewTab(IDictionary tabs, NSettingsTab modsTab)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0199: Unknown result type (might be due to invalid IL or missing references)
		List<NSettingsTab> list = new List<NSettingsTab>();
		foreach (DictionaryEntry tab in tabs)
		{
			list.Add((NSettingsTab)tab.Key);
		}
		if (list.Count < 2)
		{
			return;
		}
		float num = ((Control)list[1]).Position.X - ((Control)list[0]).Position.X;
		NSettingsTab val = list.Last();
		((Control)modsTab).Position = new Vector2(((Control)val).Position.X + num, ((Control)val).Position.Y);
		((Control)modsTab).Size = ((Control)list[0]).Size;
		Control parent = ((Node)modsTab).GetParent<Control>();
		if (((Control)modsTab).Position.X + ((Control)modsTab).Size.X > parent.Size.X && parent.Size.X > 0f)
		{
			int num2 = list.Count + 1;
			float x = ((Control)list[0]).Size.X;
			float num3 = parent.Size.X / (float)num2;
			float num4 = (num3 - x) / 2f;
			for (int i = 0; i < list.Count; i++)
			{
				((Control)list[i]).Position = new Vector2(num4 + num3 * (float)i, ((Control)list[i]).Position.Y);
			}
			((Control)modsTab).Position = new Vector2(num4 + num3 * (float)list.Count, ((Control)list[0]).Position.Y);
		}
	}

	private static void OnLanguageChanged()
	{
		for (int num = _allTabs.Count - 1; num >= 0; num--)
		{
			if (!_allTabs[num].TryGetTarget(out NSettingsTab target) || !GodotObject.IsInstanceValid((GodotObject)(object)target))
			{
				_allTabs.RemoveAt(num);
			}
			else
			{
				target.SetLabel(I18n.TabMods);
			}
		}
		RefreshUI();
	}

	internal static void RefreshUI()
	{
		_liveBindings.Clear();
		for (int num = _allContainers.Count - 1; num >= 0; num--)
		{
			if (!_allContainers[num].TryGetTarget(out VBoxContainer target) || !GodotObject.IsInstanceValid((GodotObject)(object)target))
			{
				_allContainers.RemoveAt(num);
			}
			else
			{
				Node[] array = ((IEnumerable<Node>)((Node)target).GetChildren(false)).ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					array[i].QueueFree();
				}
				PopulateInto(target);
				NSettingsPanel val = FindOwningSettingsPanel((Control)(object)target);
				if (val != null)
				{
					RebuildFocusTargets(val, target, null);
				}
			}
		}
	}

	private static Control? CreatePreReadyFocusSentinel(NSettingsPanel firstPanel)
	{
		try
		{
			Control defaultFocusedControl = firstPanel.DefaultFocusedControl;
			if (defaultFocusedControl != null && GodotObject.IsInstanceValid((GodotObject)(object)defaultFocusedControl))
			{
				Node obj = ((Node)defaultFocusedControl).Duplicate(15);
				Control val = (Control)(object)((obj is Control) ? obj : null);
				if (val != null)
				{
					val.FocusMode = (FocusModeEnum)2;
					((CanvasItem)val).Visible = false;
					return val;
				}
			}
		}
		catch
		{
		}
		try
		{
			Control content = (Control)(object)firstPanel.Content;
			if (content != null)
			{
				Control val2 = FindFirstGameSettingsControl(content);
				if (val2 != null)
				{
					Node obj3 = ((Node)val2).Duplicate(15);
					Control val3 = (Control)(object)((obj3 is Control) ? obj3 : null);
					if (val3 != null)
					{
						val3.FocusMode = (FocusModeEnum)2;
						((CanvasItem)val3).Visible = false;
						return val3;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static Control? FindFirstGameSettingsControl(Control parent)
	{
		foreach (Control item in ((IEnumerable)((Node)parent).GetChildren(false)).OfType<Control>())
		{
			bool flag;
			switch (((object)item).GetType().Name)
			{
			case "NTickbox":
			case "NSettingsSlider":
			case "NDropdownPositioner":
			case "NPaginator":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				return item;
			}
			Control val = FindFirstGameSettingsControl(item);
			if (val != null)
			{
				return val;
			}
		}
		return null;
	}

	private static void OverrideRefreshSize(Viewport viewport, NSettingsPanel modsPanel, float maxHeight)
	{
		WeakReference<NSettingsPanel> panelRef = new WeakReference<NSettingsPanel>(modsPanel);
		viewport.SizeChanged += delegate
		{
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			if (panelRef.TryGetTarget(out NSettingsPanel target) && GodotObject.IsInstanceValid((GodotObject)(object)target))
			{
				((GodotObject)target).CallDeferred(StringName.op_Implicit("set_size"), (Variant[])(object)new Variant[1] { Variant.op_Implicit(new Vector2(((Control)target).Size.X, maxHeight)) });
			}
		};
	}

	private static void CacheGameFont(NSettingsPanel panel)
	{
		if (_gameFont != null)
		{
			return;
		}
		try
		{
			if (panel.Content == null)
			{
				return;
			}
			foreach (Node child in ((Node)panel.Content).GetChildren(false))
			{
				Label val = (Label)(object)((child is Label) ? child : null);
				if (val != null)
				{
					_gameFont = ((Control)val).GetThemeFont(StringName.op_Implicit("font"), (StringName)null);
					if (_gameFont != null)
					{
						break;
					}
				}
				Control val2 = (Control)(object)((child is Control) ? child : null);
				if (val2 == null)
				{
					continue;
				}
				foreach (Node child2 in ((Node)val2).GetChildren(false))
				{
					Label val3 = (Label)(object)((child2 is Label) ? child2 : null);
					if (val3 != null)
					{
						_gameFont = ((Control)val3).GetThemeFont(StringName.op_Implicit("font"), (StringName)null);
						if (_gameFont != null)
						{
							return;
						}
					}
				}
			}
		}
		catch
		{
		}
	}

	private static void ApplyGameFont(Label label)
	{
		if (_gameFont != null)
		{
			((Control)label).AddThemeFontOverride(StringName.op_Implicit("font"), _gameFont);
		}
	}

	private static NSettingsPanel? FindOwningSettingsPanel(Control control)
	{
		for (Node val = (Node)(object)control; val != null; val = val.GetParent())
		{
			NSettingsPanel val2 = (NSettingsPanel)(object)((val is NSettingsPanel) ? val : null);
			if (val2 != null)
			{
				return val2;
			}
		}
		return null;
	}

	private static void RebuildFocusTargets(NSettingsPanel panel, VBoxContainer contentContainer, Control? preReadyFocusSentinel)
	{
		try
		{
			List<Control> list = new List<Control>();
			CollectFocusableControls((Control)(object)contentContainer, list);
			if (preReadyFocusSentinel != null && GodotObject.IsInstanceValid((GodotObject)(object)preReadyFocusSentinel) && ((Node)preReadyFocusSentinel).GetParent() == contentContainer && list.Count > 0)
			{
				((Node)contentContainer).RemoveChild((Node)(object)preReadyFocusSentinel);
				((Node)preReadyFocusSentinel).QueueFree();
				preReadyFocusSentinel = null;
			}
			if (list.Count == 0 && preReadyFocusSentinel != null && GodotObject.IsInstanceValid((GodotObject)(object)preReadyFocusSentinel))
			{
				list.Add(preReadyFocusSentinel);
			}
			for (int i = 0; i < list.Count; i++)
			{
				Control val = list[i];
				val.FocusNeighborLeft = ((Node)val).GetPath();
				val.FocusNeighborRight = ((Node)val).GetPath();
				val.FocusNeighborTop = ((Node)((i > 0) ? list[i - 1] : val)).GetPath();
				val.FocusNeighborBottom = ((Node)((i < list.Count - 1) ? list[i + 1] : val)).GetPath();
			}
			FieldInfo field = typeof(NSettingsPanel).GetField("_firstControl", PrivateInstance);
			if (field != null)
			{
				field.SetValue(panel, list.FirstOrDefault());
			}
		}
		catch (Exception value)
		{
			MainFile.Log.Error($"Failed to rebuild settings panel focus targets: {value}", 1);
		}
	}

	private static void CollectFocusableControls(Control parent, List<Control> focusables)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Invalid comparison between Unknown and I8
		foreach (Control item in ((IEnumerable)((Node)parent).GetChildren(false)).OfType<Control>())
		{
			if (((CanvasItem)item).Visible)
			{
				if ((long)item.FocusMode == 2)
				{
					focusables.Add(item);
				}
				CollectFocusableControls(item, focusables);
			}
		}
	}

	internal static void NotifyValueChanged(string modId, string key, object value)
	{
		(string, string) key2 = (modId, key);
		if (!_liveBindings.TryGetValue(key2, out List<LiveBinding> value2))
		{
			return;
		}
		for (int num = value2.Count - 1; num >= 0; num--)
		{
			try
			{
				if (!value2[num].Apply(value))
				{
					value2.RemoveAt(num);
				}
			}
			catch (Exception value3)
			{
				MainFile.Log.Error($"Live config UI update failed [{modId}.{key}]: {value3}", 1);
				value2.RemoveAt(num);
			}
		}
		if (value2.Count == 0)
		{
			_liveBindings.Remove(key2);
		}
	}

	private static void RegisterLiveBinding(string modId, string key, Func<object, bool> apply)
	{
		(string, string) key2 = (modId, key);
		if (!_liveBindings.TryGetValue(key2, out List<LiveBinding> value))
		{
			value = new List<LiveBinding>();
			_liveBindings[key2] = value;
		}
		value.Add(new LiveBinding(apply));
	}

	private static void PopulateInto(VBoxContainer contentContainer)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Expected O, but got Unknown
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Expected O, but got Unknown
		if (contentContainer == null)
		{
			return;
		}
		IReadOnlyDictionary<string, ModRegistration> registrations = ModConfigManager.Registrations;
		if (registrations.Count == 0)
		{
			AddCenteredLabel(contentContainer, I18n.NoConfigs);
			return;
		}
		ScrollContainer val = new ScrollContainer();
		val.HorizontalScrollMode = (ScrollMode)0;
		val.VerticalScrollMode = (ScrollMode)1;
		((Control)val).SizeFlagsVertical = (SizeFlags)3;
		((Control)val).SizeFlagsHorizontal = (SizeFlags)3;
		val.FollowFocus = true;
		((Node)contentContainer).AddChild((Node)(object)val, false, (InternalMode)0);
		VBoxContainer val2 = new VBoxContainer();
		((Control)val2).SizeFlagsHorizontal = (SizeFlags)3;
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		bool flag = true;
		foreach (KeyValuePair<string, ModRegistration> item3 in registrations)
		{
			var (modId, modRegistration2) = (KeyValuePair<string, ModRegistration>)(ref item3);
			if (!flag)
			{
				AddModSeparator(val2);
			}
			flag = false;
			bool flag2 = ModConfigManager.CollapsedMods.Contains(modId);
			string localizedName = modRegistration2.GetLocalizedName();
			(PanelContainer, Label) tuple = AddModHeaderWithReset(val2, modId, localizedName, flag2);
			PanelContainer item = tuple.Item1;
			Label item2 = tuple.Item2;
			VBoxContainer val3 = new VBoxContainer
			{
				Name = StringName.op_Implicit("Entries_" + modId)
			};
			((Node)val2).AddChild((Node)(object)val3, false, (InternalMode)0);
			VBoxContainer capturedEntriesBox = val3;
			Label capturedLabel = item2;
			_ = modId;
			ModRegistration capturedReg = modRegistration2;
			((Control)item).GuiInput += (GuiInputEventHandler)delegate(InputEvent @event)
			{
				//IL_0019: Unknown result type (might be due to invalid IL or missing references)
				//IL_0020: Invalid comparison between Unknown and I8
				InputEventMouseButton val4 = (InputEventMouseButton)(object)((@event is InputEventMouseButton) ? @event : null);
				if (val4 != null && val4.Pressed && (long)val4.ButtonIndex == 1)
				{
					if (ModConfigManager.CollapsedMods.Contains(modId))
					{
						ModConfigManager.CollapsedMods.Remove(modId);
					}
					else
					{
						ModConfigManager.CollapsedMods.Add(modId);
					}
					bool flag3 = ModConfigManager.CollapsedMods.Contains(modId);
					((CanvasItem)capturedEntriesBox).Visible = !flag3;
					capturedLabel.Text = (flag3 ? "▶" : "▼") + "  " + capturedReg.GetLocalizedName();
					ModConfigManager.SaveUiState();
				}
			};
			for (int i = 0; i < modRegistration2.Entries.Length; i++)
			{
				ConfigEntry configEntry = modRegistration2.Entries[i];
				switch (configEntry.Type)
				{
				case ConfigType.Header:
					AddSectionHeader(val3, ResolveLabel(configEntry));
					break;
				case ConfigType.Separator:
					AddSeparator(val3);
					break;
				case ConfigType.Toggle:
					AddToggle(val3, modId, configEntry);
					break;
				case ConfigType.Slider:
					AddSlider(val3, modId, configEntry);
					break;
				case ConfigType.Dropdown:
					AddDropdown(val3, modId, configEntry);
					break;
				case ConfigType.KeyBind:
					AddKeyBind(val3, modId, configEntry);
					break;
				case ConfigType.TextInput:
					AddTextInput(val3, modId, configEntry);
					break;
				case ConfigType.Button:
					AddButton(val3, modId, configEntry);
					break;
				case ConfigType.ColorPicker:
					AddColorPicker(val3, modId, configEntry);
					break;
				}
				ConfigType type = configEntry.Type;
				if ((uint)(type - 5) > 1u)
				{
					AddRowSeparator(val3);
				}
			}
			if (flag2)
			{
				((CanvasItem)val3).Visible = false;
			}
		}
	}

	private static string ResolveLabel(ConfigEntry entry)
	{
		Dictionary<string, string> labels = entry.Labels;
		if (labels != null && labels.Count > 0)
		{
			string text = ResolveLangDict(entry.Labels);
			if (text != null)
			{
				return text;
			}
		}
		if (!string.IsNullOrEmpty(entry.LabelKey))
		{
			return I18n.Get(entry.LabelKey, entry.Label);
		}
		return entry.Label;
	}

	private static string ResolveDescription(ConfigEntry entry)
	{
		Dictionary<string, string> descriptions = entry.Descriptions;
		if (descriptions != null && descriptions.Count > 0)
		{
			string text = ResolveLangDict(entry.Descriptions);
			if (text != null)
			{
				return text;
			}
		}
		if (!string.IsNullOrEmpty(entry.DescriptionKey))
		{
			return I18n.Get(entry.DescriptionKey, entry.Description);
		}
		return entry.Description;
	}

	private static string? ResolveLangDict(Dictionary<string, string> dict)
	{
		string currentLang = I18n.CurrentLang;
		if (dict.TryGetValue(currentLang, out string value))
		{
			return value;
		}
		foreach (var (text3, result) in dict)
		{
			if (currentLang.StartsWith(text3) || text3.StartsWith(currentLang))
			{
				return result;
			}
		}
		return null;
	}

	private static string ResolveDropdownOption(ConfigEntry entry, int index)
	{
		if (entry.OptionsKeys != null && index < entry.OptionsKeys.Length && entry.Options != null && index < entry.Options.Length)
		{
			return I18n.Get(entry.OptionsKeys[index], entry.Options[index]);
		}
		if (entry.Options != null && index < entry.Options.Length)
		{
			return entry.Options[index];
		}
		return "";
	}

	private static string ResolveButtonText(ConfigEntry entry)
	{
		Dictionary<string, string> buttonTexts = entry.ButtonTexts;
		if (buttonTexts != null && buttonTexts.Count > 0)
		{
			string text = ResolveLangDict(entry.ButtonTexts);
			if (text != null)
			{
				return text;
			}
		}
		if (string.IsNullOrEmpty(entry.ButtonText))
		{
			return ResolveLabel(entry);
		}
		return entry.ButtonText;
	}

	private static void AddDescriptionIfPresent(VBoxContainer parent, ConfigEntry entry)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		string text = ResolveDescription(entry);
		if (!string.IsNullOrEmpty(text))
		{
			Label val = new Label
			{
				Text = "      " + text,
				CustomMinimumSize = new Vector2(0f, 20f),
				AutowrapMode = (AutowrapMode)3
			};
			((Control)val).AddThemeColorOverride(StringName.op_Implicit("font_color"), DimText);
			((Control)val).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 15);
			ApplyGameFont(val);
			((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		}
	}

	private static void ApplyTooltip(Control control, ConfigEntry entry)
	{
		string text = ResolveDescription(entry);
		if (!string.IsNullOrEmpty(text))
		{
			control.TooltipText = text;
		}
	}

	private static void AddModSeparator(VBoxContainer parent)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Expected O, but got Unknown
		((Node)parent).AddChild((Node)new Control
		{
			CustomMinimumSize = new Vector2(0f, 8f)
		}, false, (InternalMode)0);
		HSeparator val = new HSeparator
		{
			CustomMinimumSize = new Vector2(0f, 4f)
		};
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 4);
		StyleBoxFlat val2 = new StyleBoxFlat
		{
			BgColor = new Color(ModHeaderBg, 0.7f),
			ContentMarginTop = 2f,
			ContentMarginBottom = 2f
		};
		((Control)val).AddThemeStyleboxOverride(StringName.op_Implicit("separator"), (StyleBox)(object)val2);
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		((Node)parent).AddChild((Node)new Control
		{
			CustomMinimumSize = new Vector2(0f, 8f)
		}, false, (InternalMode)0);
	}

	private static void AddRowSeparator(VBoxContainer parent)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		HSeparator val = new HSeparator
		{
			CustomMinimumSize = new Vector2(0f, 1f)
		};
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 1);
		StyleBoxFlat val2 = new StyleBoxFlat
		{
			BgColor = RowSeparatorColor,
			ContentMarginTop = 0f,
			ContentMarginBottom = 0f
		};
		((Control)val).AddThemeStyleboxOverride(StringName.op_Implicit("separator"), (StyleBox)(object)val2);
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
	}

	private static (PanelContainer, Label) AddModHeaderWithReset(VBoxContainer parent, string modId, string text, bool isCollapsed)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected O, but got Unknown
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Expected O, but got Unknown
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Expected O, but got Unknown
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		string modId2 = modId;
		PanelContainer val = new PanelContainer
		{
			MouseFilter = (MouseFilterEnum)0
		};
		((Control)val).MouseDefaultCursorShape = (CursorShape)2;
		StyleBoxFlat val2 = new StyleBoxFlat
		{
			BgColor = ModHeaderBg,
			CornerRadiusTopLeft = 4,
			CornerRadiusTopRight = 4,
			CornerRadiusBottomLeft = 4,
			CornerRadiusBottomRight = 4,
			ContentMarginLeft = 8f,
			ContentMarginRight = 8f,
			ContentMarginTop = 4f,
			ContentMarginBottom = 4f
		};
		((Control)val).AddThemeStyleboxOverride(StringName.op_Implicit("panel"), (StyleBox)(object)val2);
		HBoxContainer val3 = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(0f, 40f),
			MouseFilter = (MouseFilterEnum)2
		};
		string text2 = (isCollapsed ? "▶" : "▼");
		Label val4 = new Label
		{
			Text = text2 + "  " + text,
			SizeFlagsHorizontal = (SizeFlags)3,
			MouseFilter = (MouseFilterEnum)2
		};
		((Control)val4).AddThemeColorOverride(StringName.op_Implicit("font_color"), CreamGold);
		((Control)val4).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 20);
		ApplyGameFont(val4);
		Button val5 = new Button
		{
			Text = I18n.ResetDefaults,
			CustomMinimumSize = new Vector2(0f, 28f),
			MouseFilter = (MouseFilterEnum)0
		};
		((Control)val5).AddThemeColorOverride(StringName.op_Implicit("font_color"), ResetColor);
		((Control)val5).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 14);
		((BaseButton)val5).Pressed += delegate
		{
			if (ModConfigManager.ResetToDefaults(modId2))
			{
				RefreshUI();
			}
		};
		((Node)val3).AddChild((Node)(object)val4, false, (InternalMode)0);
		((Node)val3).AddChild((Node)(object)val5, false, (InternalMode)0);
		((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		return (val, val4);
	}

	private static void AddSectionHeader(VBoxContainer parent, string text)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		Label val = new Label
		{
			Text = text,
			HorizontalAlignment = (HorizontalAlignment)0,
			CustomMinimumSize = new Vector2(0f, 32f)
		};
		((Control)val).AddThemeColorOverride(StringName.op_Implicit("font_color"), DimText);
		((Control)val).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 20);
		ApplyGameFont(val);
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
	}

	private static void AddCenteredLabel(VBoxContainer parent, string text)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		Label val = new Label
		{
			Text = text,
			HorizontalAlignment = (HorizontalAlignment)1,
			CustomMinimumSize = new Vector2(0f, 50f)
		};
		((Control)val).AddThemeColorOverride(StringName.op_Implicit("font_color"), DimText);
		((Control)val).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 16);
		ApplyGameFont(val);
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
	}

	private static void AddSeparator(VBoxContainer parent)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		HSeparator val = new HSeparator
		{
			CustomMinimumSize = new Vector2(0f, 8f)
		};
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 6);
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
	}

	private static void AddToggle(VBoxContainer parent, string modId, ConfigEntry entry)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected O, but got Unknown
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Expected O, but got Unknown
		string modId2 = modId;
		ConfigEntry entry2 = entry;
		HBoxContainer val = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(0f, 45f)
		};
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 20);
		Label val2 = new Label
		{
			Text = "  " + ResolveLabel(entry2),
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((Control)val2).AddThemeColorOverride(StringName.op_Implicit("font_color"), TextColor);
		((Control)val2).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 20);
		ApplyGameFont(val2);
		CheckButton val3 = new CheckButton
		{
			ButtonPressed = ModConfigManager.GetValue<bool>(modId2, entry2.Key)
		};
		UiUpdateGuard guard = new UiUpdateGuard();
		WeakReference<CheckButton> toggleRef = new WeakReference<CheckButton>(val3);
		RegisterLiveBinding(modId2, entry2.Key, delegate(object value)
		{
			if (!TryGetLiveTarget<CheckButton>(toggleRef, out CheckButton target))
			{
				return false;
			}
			if (!TryConvertValue<bool>(value, out var converted))
			{
				return true;
			}
			if (((BaseButton)target).ButtonPressed == converted)
			{
				return true;
			}
			guard.Suppress = true;
			((BaseButton)target).ButtonPressed = converted;
			guard.Suppress = false;
			return true;
		});
		((BaseButton)val3).Toggled += (ToggledEventHandler)delegate(bool pressed)
		{
			if (!guard.Suppress)
			{
				ModConfigManager.SetValue(modId2, entry2.Key, pressed);
			}
		};
		ApplyTooltip((Control)(object)val, entry2);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		AddDescriptionIfPresent(parent, entry2);
	}

	private static void AddSlider(VBoxContainer parent, string modId, ConfigEntry entry)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Expected O, but got Unknown
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Expected O, but got Unknown
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f7: Expected O, but got Unknown
		string modId2 = modId;
		ConfigEntry entry2 = entry;
		HBoxContainer val = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(0f, 45f)
		};
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 15);
		Label val2 = new Label
		{
			Text = "  " + ResolveLabel(entry2),
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((Control)val2).AddThemeColorOverride(StringName.op_Implicit("font_color"), TextColor);
		((Control)val2).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 20);
		ApplyGameFont(val2);
		HSlider val3 = new HSlider
		{
			MinValue = entry2.Min,
			MaxValue = entry2.Max,
			Step = entry2.Step,
			CustomMinimumSize = new Vector2(200f, 0f),
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((Range)val3).Value = ModConfigManager.GetValue<float>(modId2, entry2.Key);
		string fmt = entry2.Format ?? "F0";
		Label valueLabel = new Label
		{
			CustomMinimumSize = new Vector2(70f, 0f),
			HorizontalAlignment = (HorizontalAlignment)2,
			Text = ((Range)val3).Value.ToString(fmt)
		};
		((Control)valueLabel).AddThemeColorOverride(StringName.op_Implicit("font_color"), CreamGold);
		((Control)valueLabel).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 18);
		UiUpdateGuard guard = new UiUpdateGuard();
		WeakReference<HSlider> sliderRef = new WeakReference<HSlider>(val3);
		WeakReference<Label> valueLabelRef = new WeakReference<Label>(valueLabel);
		RegisterLiveBinding(modId2, entry2.Key, delegate(object value)
		{
			if (!TryGetLiveTarget<HSlider>(sliderRef, out HSlider target))
			{
				return false;
			}
			if (!TryConvertValue<float>(value, out var converted))
			{
				return true;
			}
			if (Math.Abs(((Range)target).Value - (double)converted) > 0.0001)
			{
				guard.Suppress = true;
				((Range)target).Value = converted;
				guard.Suppress = false;
			}
			if (TryGetLiveTarget<Label>(valueLabelRef, out Label target2))
			{
				target2.Text = converted.ToString(fmt);
			}
			return true;
		});
		((Range)val3).ValueChanged += (ValueChangedEventHandler)delegate(double value)
		{
			valueLabel.Text = ((float)value).ToString(fmt);
			if (!guard.Suppress)
			{
				ModConfigManager.SetValue(modId2, entry2.Key, (float)value);
			}
		};
		ApplyTooltip((Control)(object)val, entry2);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
		((Node)val).AddChild((Node)(object)valueLabel, false, (InternalMode)0);
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		AddDescriptionIfPresent(parent, entry2);
	}

	private static void AddDropdown(VBoxContainer parent, string modId, ConfigEntry entry)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Expected O, but got Unknown
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Expected O, but got Unknown
		ConfigEntry entry2 = entry;
		string modId2 = modId;
		HBoxContainer val = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(0f, 45f)
		};
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 20);
		Label val2 = new Label
		{
			Text = "  " + ResolveLabel(entry2),
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((Control)val2).AddThemeColorOverride(StringName.op_Implicit("font_color"), TextColor);
		((Control)val2).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 20);
		ApplyGameFont(val2);
		OptionButton val3 = new OptionButton
		{
			CustomMinimumSize = new Vector2(180f, 0f)
		};
		string value2 = ModConfigManager.GetValue<string>(modId2, entry2.Key);
		if (entry2.Options != null)
		{
			for (int i = 0; i < entry2.Options.Length; i++)
			{
				val3.AddItem(ResolveDropdownOption(entry2, i), i);
				if (entry2.Options[i] == value2)
				{
					val3.Selected = i;
				}
			}
		}
		UiUpdateGuard guard = new UiUpdateGuard();
		WeakReference<OptionButton> dropdownRef = new WeakReference<OptionButton>(val3);
		RegisterLiveBinding(modId2, entry2.Key, delegate(object value)
		{
			if (!TryGetLiveTarget<OptionButton>(dropdownRef, out OptionButton target))
			{
				return false;
			}
			if (!TryConvertValue<string>(value, out var converted) || entry2.Options == null)
			{
				return true;
			}
			int num = Array.IndexOf(entry2.Options, converted);
			if (num < 0 || target.Selected == num)
			{
				return true;
			}
			guard.Suppress = true;
			target.Selected = num;
			guard.Suppress = false;
			return true;
		});
		val3.ItemSelected += (ItemSelectedEventHandler)delegate(long index)
		{
			if (!guard.Suppress && entry2.Options != null && index < entry2.Options.Length)
			{
				ModConfigManager.SetValue(modId2, entry2.Key, entry2.Options[index]);
			}
		};
		ApplyTooltip((Control)(object)val, entry2);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		AddDescriptionIfPresent(parent, entry2);
	}

	private static void AddKeyBind(VBoxContainer parent, string modId, ConfigEntry entry)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
		string modId2 = modId;
		ConfigEntry entry2 = entry;
		HBoxContainer val = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(0f, 45f)
		};
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 20);
		Label val2 = new Label
		{
			Text = "  " + ResolveLabel(entry2),
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((Control)val2).AddThemeColorOverride(StringName.op_Implicit("font_color"), TextColor);
		((Control)val2).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 20);
		ApplyGameFont(val2);
		long value2 = ModConfigManager.GetValue<long>(modId2, entry2.Key);
		Button btn = new Button
		{
			CustomMinimumSize = new Vector2(140f, 32f),
			FocusMode = (FocusModeEnum)2,
			TooltipText = I18n.KeyBindTooltip
		};
		((Control)btn).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 17);
		ApplyKeyBindButtonState(btn, value2);
		WeakReference<Button> buttonRef = new WeakReference<Button>(btn);
		RegisterLiveBinding(modId2, entry2.Key, delegate(object value)
		{
			if (!TryGetLiveTarget<Button>(buttonRef, out Button target))
			{
				return false;
			}
			if (!TryConvertValue<long>(value, out var converted))
			{
				return true;
			}
			if (_activeKeyBindButton != null && GodotObject.IsInstanceValid((GodotObject)(object)_activeKeyBindButton) && _activeKeyBindButton == target && _activeKeyBindModId == modId2 && _activeKeyBindEntry?.Key == entry2.Key)
			{
				return true;
			}
			ApplyKeyBindButtonState(target, converted);
			return true;
		});
		((BaseButton)btn).Pressed += delegate
		{
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			if (_activeKeyBindButton != null && GodotObject.IsInstanceValid((GodotObject)(object)_activeKeyBindButton))
			{
				long value3 = ModConfigManager.GetValue<long>(_activeKeyBindModId, _activeKeyBindEntry.Key);
				ApplyKeyBindButtonState(_activeKeyBindButton, value3);
				CancelKeyCapture();
			}
			_activeKeyBindButton = btn;
			_activeKeyBindModId = modId2;
			_activeKeyBindEntry = entry2;
			btn.Text = I18n.PressAnyKey;
			((Control)btn).AddThemeColorOverride(StringName.op_Implicit("font_color"), KeyBindListening);
			StartKeyCapture(modId2, entry2, btn);
		};
		ApplyTooltip((Control)(object)val, entry2);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		((Node)val).AddChild((Node)(object)btn, false, (InternalMode)0);
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		AddDescriptionIfPresent(parent, entry2);
	}

	private static string KeyToDisplayString(long keyCode)
	{
		if (keyCode == 0L)
		{
			return I18n.KeyUnbound;
		}
		return OS.GetKeycodeString((Key)keyCode);
	}

	private static void ApplyKeyBindButtonState(Button button, long keyCode)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		button.Text = KeyToDisplayString(keyCode);
		((Control)button).TooltipText = I18n.KeyBindTooltip;
		if (keyCode == 0L)
		{
			((Control)button).AddThemeColorOverride(StringName.op_Implicit("font_color"), DimText);
		}
		else
		{
			((Control)button).RemoveThemeColorOverride(StringName.op_Implicit("font_color"));
		}
	}

	private static void StartKeyCapture(string modId, ConfigEntry entry, Button btn)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		string modId2 = modId;
		ConfigEntry entry2 = entry;
		Button btn2 = btn;
		CancelKeyCapture();
		_captureNode = new KeyCaptureNode();
		_captureNode.OnKeyCaptured = delegate(long keyCode)
		{
			ModConfigManager.SetValue(modId2, entry2.Key, keyCode);
			ApplyKeyBindButtonState(btn2, keyCode);
			_activeKeyBindButton = null;
			_activeKeyBindModId = "";
			_activeKeyBindEntry = null;
			CancelKeyCapture();
		};
		((Node)((SceneTree)Engine.GetMainLoop()).Root).AddChild((Node)(object)_captureNode, false, (InternalMode)0);
	}

	private static void CancelKeyCapture()
	{
		if (_captureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)_captureNode))
		{
			((Node)_captureNode).QueueFree();
			_captureNode = null;
		}
	}

	private static void AddTextInput(VBoxContainer parent, string modId, ConfigEntry entry)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Expected O, but got Unknown
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_027e: Expected O, but got Unknown
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Expected O, but got Unknown
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01be: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01da: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e6: Expected O, but got Unknown
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0201: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0231: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_023f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0246: Unknown result type (might be due to invalid IL or missing references)
		//IL_024d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0267: Expected O, but got Unknown
		ConfigEntry entry2 = entry;
		string modId2 = modId;
		HBoxContainer val = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(0f, 45f)
		};
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 20);
		Label val2 = new Label
		{
			Text = "  " + ResolveLabel(entry2),
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((Control)val2).AddThemeColorOverride(StringName.op_Implicit("font_color"), TextColor);
		((Control)val2).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 20);
		ApplyGameFont(val2);
		LineEdit lineEdit = new LineEdit
		{
			Text = ModConfigManager.GetValue<string>(modId2, entry2.Key),
			MaxLength = entry2.MaxLength,
			PlaceholderText = entry2.Placeholder,
			CustomMinimumSize = new Vector2(200f, 32f),
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((Control)lineEdit).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 17);
		WeakReference<LineEdit> lineEditRef = new WeakReference<LineEdit>(lineEdit);
		RegisterLiveBinding(modId2, entry2.Key, delegate(object value)
		{
			if (!TryGetLiveTarget<LineEdit>(lineEditRef, out LineEdit target))
			{
				return false;
			}
			if (((Control)target).HasFocus())
			{
				return true;
			}
			if (!TryConvertValue<string>(value, out var converted))
			{
				return true;
			}
			if (target.Text == converted)
			{
				return true;
			}
			target.Text = converted;
			return true;
		});
		StyleBoxFlat validStyle = null;
		StyleBoxFlat invalidStyle = null;
		if (entry2.Validator != null)
		{
			validStyle = new StyleBoxFlat
			{
				BgColor = new Color(0.15f, 0.15f, 0.15f, 1f),
				BorderColor = new Color(0.3f, 0.3f, 0.3f, 1f),
				BorderWidthBottom = 1,
				BorderWidthTop = 1,
				BorderWidthLeft = 1,
				BorderWidthRight = 1,
				CornerRadiusTopLeft = 3,
				CornerRadiusTopRight = 3,
				CornerRadiusBottomLeft = 3,
				CornerRadiusBottomRight = 3
			};
			invalidStyle = new StyleBoxFlat
			{
				BgColor = new Color(0.2f, 0.1f, 0.1f, 1f),
				BorderColor = new Color(0.8f, 0.3f, 0.3f, 1f),
				BorderWidthBottom = 2,
				BorderWidthTop = 2,
				BorderWidthLeft = 2,
				BorderWidthRight = 2,
				CornerRadiusTopLeft = 3,
				CornerRadiusTopRight = 3,
				CornerRadiusBottomLeft = 3,
				CornerRadiusBottomRight = 3
			};
		}
		lineEdit.TextSubmitted += (TextSubmittedEventHandler)delegate(string text)
		{
			ApplyValidation(lineEdit, text);
			ModConfigManager.SetValue(modId2, entry2.Key, text);
		};
		((Control)lineEdit).FocusExited += delegate
		{
			ApplyValidation(lineEdit, lineEdit.Text);
			ModConfigManager.SetValue(modId2, entry2.Key, lineEdit.Text);
		};
		lineEdit.TextChanged += (TextChangedEventHandler)delegate(string text)
		{
			ApplyValidation(lineEdit, text);
		};
		ApplyValidation(lineEdit, lineEdit.Text);
		ApplyTooltip((Control)(object)val, entry2);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		((Node)val).AddChild((Node)(object)lineEdit, false, (InternalMode)0);
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		AddDescriptionIfPresent(parent, entry2);
		void ApplyValidation(LineEdit le, string text)
		{
			if (entry2.Validator == null)
			{
				return;
			}
			try
			{
				bool flag = entry2.Validator(text);
				((Control)le).AddThemeStyleboxOverride(StringName.op_Implicit("normal"), (StyleBox)(object)(flag ? validStyle : invalidStyle));
			}
			catch (Exception value2)
			{
				MainFile.Log.Error($"Validator error [{modId2}.{entry2.Key}]: {value2}", 1);
			}
		}
	}

	private static void AddButton(VBoxContainer parent, string modId, ConfigEntry entry)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Expected O, but got Unknown
		ConfigEntry entry2 = entry;
		string modId2 = modId;
		HBoxContainer val = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(0f, 45f)
		};
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 20);
		Label val2 = new Label
		{
			Text = "  " + ResolveLabel(entry2),
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((Control)val2).AddThemeColorOverride(StringName.op_Implicit("font_color"), TextColor);
		((Control)val2).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 20);
		ApplyGameFont(val2);
		Button val3 = new Button
		{
			Text = ResolveButtonText(entry2),
			CustomMinimumSize = new Vector2(140f, 32f),
			FocusMode = (FocusModeEnum)2
		};
		((Control)val3).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 17);
		((BaseButton)val3).Pressed += delegate
		{
			try
			{
				entry2.OnChanged?.Invoke(true);
			}
			catch (Exception value)
			{
				MainFile.Log.Error($"Button callback error [{modId2}.{entry2.Key}]: {value}", 1);
			}
		};
		ApplyTooltip((Control)(object)val, entry2);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		AddDescriptionIfPresent(parent, entry2);
	}

	private static void AddColorPicker(VBoxContainer parent, string modId, ConfigEntry entry)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Expected O, but got Unknown
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Expected O, but got Unknown
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Expected O, but got Unknown
		string modId2 = modId;
		ConfigEntry entry2 = entry;
		HBoxContainer val = new HBoxContainer
		{
			CustomMinimumSize = new Vector2(0f, 45f)
		};
		((Control)val).AddThemeConstantOverride(StringName.op_Implicit("separation"), 20);
		Label val2 = new Label
		{
			Text = "  " + ResolveLabel(entry2),
			SizeFlagsHorizontal = (SizeFlags)3
		};
		((Control)val2).AddThemeColorOverride(StringName.op_Implicit("font_color"), TextColor);
		((Control)val2).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 20);
		ApplyGameFont(val2);
		Color color2 = ParseHexColor(ModConfigManager.GetValue<string>(modId2, entry2.Key));
		ColorPickerButton val3 = new ColorPickerButton
		{
			Color = color2,
			CustomMinimumSize = new Vector2(60f, 32f),
			FocusMode = (FocusModeEnum)2,
			EditAlpha = false,
			TooltipText = I18n.ColorPickerTooltip
		};
		Label hexLabel = new Label
		{
			Text = ((Color)(ref color2)).ToHtml(false).ToUpperInvariant(),
			CustomMinimumSize = new Vector2(80f, 0f),
			HorizontalAlignment = (HorizontalAlignment)2
		};
		((Control)hexLabel).AddThemeColorOverride(StringName.op_Implicit("font_color"), CreamGold);
		((Control)hexLabel).AddThemeFontSizeOverride(StringName.op_Implicit("font_size"), 16);
		ApplyGameFont(hexLabel);
		UiUpdateGuard guard = new UiUpdateGuard();
		WeakReference<ColorPickerButton> pickerRef = new WeakReference<ColorPickerButton>(val3);
		WeakReference<Label> hexLabelRef = new WeakReference<Label>(hexLabel);
		RegisterLiveBinding(modId2, entry2.Key, delegate(object value)
		{
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			if (!TryGetLiveTarget<ColorPickerButton>(pickerRef, out ColorPickerButton target))
			{
				return false;
			}
			if (!TryConvertValue<string>(value, out var converted))
			{
				return true;
			}
			Color color3 = ParseHexColor(converted);
			guard.Suppress = true;
			target.Color = color3;
			guard.Suppress = false;
			if (TryGetLiveTarget<Label>(hexLabelRef, out Label target2))
			{
				target2.Text = ((Color)(ref color3)).ToHtml(false).ToUpperInvariant();
			}
			return true;
		});
		val3.ColorChanged += (ColorChangedEventHandler)delegate(Color color)
		{
			hexLabel.Text = ((Color)(ref color)).ToHtml(false).ToUpperInvariant();
			if (!guard.Suppress)
			{
				ModConfigManager.SetValue(modId2, entry2.Key, "#" + ((Color)(ref color)).ToHtml(false).ToUpperInvariant());
			}
		};
		ApplyTooltip((Control)(object)val, entry2);
		((Node)val).AddChild((Node)(object)val2, false, (InternalMode)0);
		((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
		((Node)val).AddChild((Node)(object)hexLabel, false, (InternalMode)0);
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		AddDescriptionIfPresent(parent, entry2);
	}

	private static Color ParseHexColor(string? hex)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if (string.IsNullOrEmpty(hex))
		{
			return Colors.White;
		}
		try
		{
			hex = hex.TrimStart('#');
			return Color.FromHtml((ReadOnlySpan<char>)hex);
		}
		catch
		{
			return Colors.White;
		}
	}

	private static bool TryGetLiveTarget<T>(WeakReference<T> reference, out T target) where T : GodotObject
	{
		if (reference.TryGetTarget(out T target2) && GodotObject.IsInstanceValid((GodotObject)(object)target2))
		{
			target = target2;
			return true;
		}
		target = default(T);
		return false;
	}

	private static bool TryConvertValue<T>(object value, out T converted)
	{
		try
		{
			if (value is T val)
			{
				converted = val;
				return true;
			}
			if (Convert.ChangeType(value, typeof(T)) is T val2)
			{
				converted = val2;
				return true;
			}
		}
		catch
		{
		}
		converted = default(T);
		return false;
	}
}
