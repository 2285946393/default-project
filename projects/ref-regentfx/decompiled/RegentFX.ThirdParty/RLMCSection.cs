using System.Collections.Generic;

namespace RegentFX.ThirdParty;

public struct RLMCSection
{
	public string id { get; set; }

	public string title { get; set; }

	public List<object> entries { get; set; }

	public RLMCSection()
	{
		id = null;
		title = null;
		entries = new List<object>();
	}
}
