using System.Collections.Generic;

namespace RegentFX.ThirdParty;

public struct RitsuLibModConfigEntity
{
	public string modId { get; set; }

	public string modDisplayName { get; set; }

	public int modSidebarOrder { get; set; }

	public List<RLMCPage> pages { get; set; }

	public RitsuLibModConfigEntity()
	{
		modDisplayName = null;
		modId = "RegentFX";
		modSidebarOrder = 50;
		pages = new List<RLMCPage>();
	}
}
