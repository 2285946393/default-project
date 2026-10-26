using MegaCrit.Sts2.Core.Localization;

namespace CombatFlight;

internal static class FlightText
{
	public static string Choose(string simplifiedChinese, string english)
	{
		try
		{
			LocManager instance = LocManager.Instance;
			string text = ((instance != null) ? instance.Language : null);
			bool flag = ((text == "zhs" || text == "zht") ? true : false);
			return flag ? simplifiedChinese : english;
		}
		catch
		{
			return english;
		}
	}
}
