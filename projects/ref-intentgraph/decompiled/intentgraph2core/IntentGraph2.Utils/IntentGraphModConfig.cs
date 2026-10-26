using System;
using Godot;

namespace IntentGraph2.Utils;

public class IntentGraphModConfig
{
	public Key ToggleIntentGraphKey { get; set; } = (Key)4194332;


	public bool ShowMonsterMoveNames { get; set; } = true;


	public bool ShowMoveDetail { get; set; }

	public bool UseAnimatedIntentIcon { get; set; } = true;


	public bool ShowCurrentMove { get; set; } = true;


	public bool PinableIntentGraph { get; set; }

	public IntentGraphPosition IntentGraphPosition { get; set; }

	public float IntentGraphScale { get; set; } = 1f;


	public string AttackDamageFormat { get; set; } = "{base}";


	public event EventHandler<string>? OnUpdated;

	public void NotifyUpdated(string propertyName)
	{
		this.OnUpdated?.Invoke(this, propertyName);
	}

	public void SetFrom(IntentGraphModConfig config)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		ToggleIntentGraphKey = config.ToggleIntentGraphKey;
		ShowMonsterMoveNames = config.ShowMonsterMoveNames;
		ShowMoveDetail = config.ShowMoveDetail;
		UseAnimatedIntentIcon = config.UseAnimatedIntentIcon;
		ShowCurrentMove = config.ShowCurrentMove;
		PinableIntentGraph = config.PinableIntentGraph;
		IntentGraphPosition = config.IntentGraphPosition;
		IntentGraphScale = config.IntentGraphScale;
		AttackDamageFormat = config.AttackDamageFormat;
	}
}
