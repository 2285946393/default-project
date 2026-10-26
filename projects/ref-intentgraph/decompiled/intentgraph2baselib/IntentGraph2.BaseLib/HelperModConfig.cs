using BaseLib.Config;
using Godot;
using IntentGraph2.Utils;

namespace IntentGraph2.BaseLib;

public class HelperModConfig : SimpleModConfig
{
	internal static IntentGraphModConfig BaseConfig { get; } = new IntentGraphModConfig();


	[ConfigSection("Display")]
	public static bool ShowMonsterMoveNames
	{
		get
		{
			return BaseConfig.ShowMonsterMoveNames;
		}
		set
		{
			BaseConfig.ShowMonsterMoveNames = value;
			BaseConfig.NotifyUpdated("ShowMonsterMoveNames");
		}
	}

	public static bool ShowMoveDetail
	{
		get
		{
			return BaseConfig.ShowMoveDetail;
		}
		set
		{
			BaseConfig.ShowMoveDetail = value;
			BaseConfig.NotifyUpdated("ShowMoveDetail");
		}
	}

	public static bool UseAnimatedIntentIcon
	{
		get
		{
			return BaseConfig.UseAnimatedIntentIcon;
		}
		set
		{
			BaseConfig.UseAnimatedIntentIcon = value;
			BaseConfig.NotifyUpdated("UseAnimatedIntentIcon");
		}
	}

	public static bool ShowCurrentMove
	{
		get
		{
			return BaseConfig.ShowCurrentMove;
		}
		set
		{
			BaseConfig.ShowCurrentMove = value;
			BaseConfig.NotifyUpdated("ShowCurrentMove");
		}
	}

	public static IntentGraphPosition IntentGraphPosition
	{
		get
		{
			return BaseConfig.IntentGraphPosition;
		}
		set
		{
			BaseConfig.IntentGraphPosition = value;
			BaseConfig.NotifyUpdated("IntentGraphPosition");
		}
	}

	[ConfigSlider(50.0, 150.0, 10.0, Format = "{0:0.##}%")]
	public static float IntentGraphScale
	{
		get
		{
			return BaseConfig.IntentGraphScale * 100f;
		}
		set
		{
			BaseConfig.IntentGraphScale = value / 100f;
			BaseConfig.NotifyUpdated("IntentGraphScale");
		}
	}

	[ConfigHoverTip(true)]
	public static string AttackDamageFormat
	{
		get
		{
			return BaseConfig.AttackDamageFormat;
		}
		set
		{
			BaseConfig.AttackDamageFormat = value;
			BaseConfig.NotifyUpdated("AttackDamageFormat");
		}
	}

	[ConfigSection("Control")]
	public static bool PinableIntentGraph
	{
		get
		{
			return BaseConfig.PinableIntentGraph;
		}
		set
		{
			BaseConfig.PinableIntentGraph = value;
			BaseConfig.NotifyUpdated("PinableIntentGraph");
		}
	}

	[ConfigSection("Hotkey")]
	public static Key ToggleIntentGraphKey
	{
		get
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			return BaseConfig.ToggleIntentGraphKey;
		}
		set
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			BaseConfig.ToggleIntentGraphKey = value;
			BaseConfig.NotifyUpdated("ToggleIntentGraphKey");
		}
	}
}
