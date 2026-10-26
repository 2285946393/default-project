using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Runs;

namespace IntentGraph2.Patches;

public class HasAscensionLevelPatches
{
	[HarmonyPatch(typeof(RunManager), "HasAscension")]
	public static class RunManagerPatch
	{
		public static bool Prefix(object __instance, AscensionLevel level, ref bool __result)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			if (OverwriteAsensionLevel.HasValue)
			{
				__result = OverwriteAsensionLevel >= level;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(AscensionManager), "HasLevel")]
	public static class AscensionManagerPatch
	{
		public static bool Prefix(object __instance, AscensionLevel level, ref bool __result)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			if (OverwriteAsensionLevel.HasValue)
			{
				__result = OverwriteAsensionLevel >= level;
				return false;
			}
			return true;
		}
	}

	public static AscensionLevel? OverwriteAsensionLevel;
}
