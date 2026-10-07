namespace RegentFX.ThirdParty;

public struct SliderEntry
{
	public string id { get; set; }

	public string type { get; }

	public string key { get; set; }

	public string label { get; set; }

	public string description { get; set; }

	public double min { get; set; }

	public double max { get; set; }

	public double step { get; set; }

	public RLMCScope scope { get; set; }

	public SliderEntry()
	{
		id = null;
		key = null;
		label = null;
		description = null;
		min = 0.0;
		max = 0.0;
		step = 0.0;
		type = "slider";
		scope = RLMCScope.global;
	}
}
