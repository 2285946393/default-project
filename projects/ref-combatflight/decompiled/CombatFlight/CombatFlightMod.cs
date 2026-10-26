using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace CombatFlight;

[ModInitializer("Initialize")]
public static class CombatFlightMod
{
	private const string HarmonyId = "ChenWentao.CombatFlight";

	private static void Initialize()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		FlightSettings.Register();
		new Harmony("ChenWentao.CombatFlight").PatchAll(Assembly.GetExecutingAssembly());
		Log.Info("CombatFlight v1.3.1 initialized.", 2);
	}
}
