using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;
using MegaCrit.Sts2.addons.mega_text;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]
[assembly: AssemblyCompany("ShieldOnly.BaseLib")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.9.48.0")]
[assembly: AssemblyInformationalVersion("1.9.48-tempo.4")]
[assembly: AssemblyProduct("ShieldOnly.BaseLib")]
[assembly: AssemblyTitle("ShieldOnly.BaseLib")]
[assembly: AssemblyVersion("1.9.48.0")]
[module: RefSafetyRules(11)]
namespace ShieldOnly;

public static class BaseLibSettingsMenu
{
	private sealed class Menu : SimpleModConfig
	{
		public override bool VisibleInModList()
		{
			SettingsLocalization.Install();
			return true;
		}

		public override void SetupConfigUI(Control container)
		{
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Expected O, but got Unknown
			for (Node val = (Node)(object)container; val != null; val = val.GetParent())
			{
				if (val is NModConfigSubmenu)
				{
					MegaRichTextLabel nodeOrNull = val.GetNodeOrNull<MegaRichTextLabel>(NodePath.op_Implicit("ModListTitle"));
					if (nodeOrNull != null)
					{
						nodeOrNull.Text = "[center]" + SettingsLocalization.ModListTitle + "[/center]";
					}
				}
			}
			(string, string, object)[] options = SettingsState.Options;
			for (int i = 0; i < options.Length; i++)
			{
				(string, string, object) tuple = options[i];
				PropertyInfo property = typeof(Values).GetProperty(tuple.Item1);
				Control val2 = (Control)((property.PropertyType == typeof(bool)) ? ((object)((ModConfig)this).CreateRawTickboxControl(property)) : ((object)((ModConfig)this).CreateRawSliderControl(property)));
				NConfigOptionRow val3 = new NConfigOptionRow(((ModConfig)this).ModPrefix, tuple.Item1, (Control)(object)ModConfig.CreateRawLabelControl(SettingsLocalization.Title(tuple.Item1), 28), val2);
				val3.AddCustomHoverTip(SettingsLocalization.TitleKey(tuple.Item1), SettingsLocalization.DescriptionKey(tuple.Item1));
				((Node)container).AddChild((Node)(object)val3, false, (InternalMode)0);
			}
			((SimpleModConfig)this).AddRestoreDefaultsButton(container);
			SimpleModConfig.SetupFocusNeighbors(container);
		}

		protected override void RestoreDefaultsNoConfirm()
		{
			(string, string, object)[] options = SettingsState.Options;
			for (int i = 0; i < options.Length; i++)
			{
				(string, string, object) tuple = options[i];
				SettingsState.Save(tuple.Item1, tuple.Item3);
			}
			((ModConfig)this).ConfigReloaded();
		}
	}

	public static class Values
	{
		public static bool BloodFlowEnabled
		{
			get
			{
				return AnimationSettings.Current.BloodFlowEnabled;
			}
			set
			{
				SettingsState.Save("BloodFlowEnabled", value);
			}
		}

		public static bool ShieldEnabled
		{
			get
			{
				return AnimationSettings.Current.ShieldEnabled;
			}
			set
			{
				SettingsState.Save("ShieldEnabled", value);
			}
		}

		[ConfigSlider(0.0, 40.0, 1.0)]
		public static int ShieldBlockThreshold
		{
			get
			{
				return AnimationSettings.Current.ShieldBlockThreshold;
			}
			set
			{
				SettingsState.Save("ShieldBlockThreshold", value);
			}
		}

		public static bool ImperviousEnabled
		{
			get
			{
				return AnimationSettings.Current.ImperviousEnabled;
			}
			set
			{
				SettingsState.Save("ImperviousEnabled", value);
			}
		}

		public static bool SecondWindEnabled
		{
			get
			{
				return AnimationSettings.Current.SecondWindEnabled;
			}
			set
			{
				SettingsState.Save("SecondWindEnabled", value);
			}
		}

		public static bool BrandEnabled
		{
			get
			{
				return AnimationSettings.Current.BrandEnabled;
			}
			set
			{
				SettingsState.Save("BrandEnabled", value);
			}
		}

		public static bool BattleTranceEnabled
		{
			get
			{
				return AnimationSettings.Current.BattleTranceEnabled;
			}
			set
			{
				SettingsState.Save("BattleTranceEnabled", value);
			}
		}

		public static bool FeedEnabled
		{
			get
			{
				return AnimationSettings.Current.FeedEnabled;
			}
			set
			{
				SettingsState.Save("FeedEnabled", value);
			}
		}

		public static bool DemonFormEnabled
		{
			get
			{
				return AnimationSettings.Current.DemonFormEnabled;
			}
			set
			{
				SettingsState.Save("DemonFormEnabled", value);
			}
		}

		public static bool DarkEmbraceEnabled
		{
			get
			{
				return AnimationSettings.Current.DarkEmbraceEnabled;
			}
			set
			{
				SettingsState.Save("DarkEmbraceEnabled", value);
			}
		}

		public static bool ShrugItOffEnabled
		{
			get
			{
				return AnimationSettings.Current.ShrugItOffEnabled;
			}
			set
			{
				SettingsState.Save("ShrugItOffEnabled", value);
			}
		}

		public static bool BodySlamEnabled
		{
			get
			{
				return AnimationSettings.Current.BodySlamEnabled;
			}
			set
			{
				SettingsState.Save("BodySlamEnabled", value);
			}
		}

		[ConfigSlider(0.0, 50.0, 1.0)]
		public static int BodySlamBlockThreshold
		{
			get
			{
				return AnimationSettings.Current.BodySlamBlockThreshold;
			}
			set
			{
				SettingsState.Save("BodySlamBlockThreshold", value);
			}
		}

		public static bool BarricadeEnabled
		{
			get
			{
				return AnimationSettings.Current.BarricadeEnabled;
			}
			set
			{
				SettingsState.Save("BarricadeEnabled", value);
			}
		}

		public static bool CardWeaponsEnabled
		{
			get
			{
				return AnimationSettings.Current.CardWeaponsEnabled;
			}
			set
			{
				SettingsState.Save("CardWeaponsEnabled", value);
			}
		}
	}

	public static void Install()
	{
		if (ModConfigRegistry.Get("ShieldOnly") == null)
		{
			Menu menu = new Menu();
			SettingsState.Activate();
			ModConfigRegistry.Register("ShieldOnly", (ModConfig)(object)menu);
			SettingsState.Changed += ((ModConfig)menu).ConfigReloaded;
		}
	}
}
