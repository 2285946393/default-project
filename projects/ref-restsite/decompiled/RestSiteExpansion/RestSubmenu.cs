using System;

namespace RestSiteExpansion;

[Flags]
public enum RestSubmenu
{
	None = 0,
	Rest = 1,
	Forge = 2,
	Gold = 4,
	Fun = 8
}
