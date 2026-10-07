using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace MeleeAttack;

[ModInitializer("Initialize")]
public static class MeleeAttackBootstrap
{
	public static void Initialize()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		try
		{
			Harmony val = new Harmony("sts2.runtime.compat");
			val.PatchAll(Assembly.GetExecutingAssembly());
		}
		catch
		{
		}
		RuntimeCompat.Init();
	}
}
