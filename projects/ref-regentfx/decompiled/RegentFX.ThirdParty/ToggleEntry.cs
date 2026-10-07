namespace RegentFX.ThirdParty;

public struct ToggleEntry
{
	public string id { get; set; }

	public string type { get; }

	public string key { get; set; }

	public string label { get; set; }

	public string description { get; set; }

	public RLMCScope scope { get; set; }

	public ToggleEntry()
	{
		id = null;
		key = null;
		label = null;
		description = null;
		type = "toggle";
		scope = RLMCScope.global;
	}
}
