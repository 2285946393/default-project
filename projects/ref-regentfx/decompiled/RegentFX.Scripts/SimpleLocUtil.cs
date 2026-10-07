using MegaCrit.Sts2.Core.Localization;

namespace RegentFX.Scripts;

public static class SimpleLocUtil
{
	public static string Simple(string chs, string eng)
	{
		LocManager instance = LocManager.Instance;
		string text = ((instance != null) ? instance.Language : null);
		if ((!(text == "zhs") && !(text == "zht")) || 1 == 0)
		{
			return eng;
		}
		return chs;
	}
}
