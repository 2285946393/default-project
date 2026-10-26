using System;
using System.Collections.Generic;

namespace ModConfig;

public class ConfigEntry
{
	public string Key { get; set; } = "";


	public string Label { get; set; } = "";


	public string? LabelKey { get; set; }

	public Dictionary<string, string>? Labels { get; set; }

	public string Description { get; set; } = "";


	public string? DescriptionKey { get; set; }

	public Dictionary<string, string>? Descriptions { get; set; }

	public ConfigType Type { get; set; }

	public object DefaultValue { get; set; } = false;


	public float Min { get; set; }

	public float Max { get; set; } = 100f;


	public float Step { get; set; } = 1f;


	public string Format { get; set; } = "F0";


	public string[]? Options { get; set; }

	public string[]? OptionsKeys { get; set; }

	public int MaxLength { get; set; } = 64;


	public string Placeholder { get; set; } = "";


	public string ButtonText { get; set; } = "";


	public Dictionary<string, string>? ButtonTexts { get; set; }

	public Func<object, bool>? Validator { get; set; }

	public Action<object>? OnChanged { get; set; }
}
