using System.Collections.Generic;

namespace RegentFX.ThirdParty;

public struct RLMCPage
{
	public string pageId { get; set; }

	public string title { get; set; }

	public string description { get; set; }

	public int sortOrder { get; set; }

	public List<RLMCSection> sections { get; set; }

	public RLMCPage()
	{
		pageId = null;
		title = null;
		description = null;
		sortOrder = 50;
		sections = new List<RLMCSection>();
	}
}
