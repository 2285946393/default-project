using System;
using System.Reflection;
using HarmonyLib;

namespace MeleeAttack;

internal static class RuntimeCompat
{
	private const string TargetTypeName = "LibraryOfRuina.compat.IncompatibleModGuard";

	private const string TargetMethodName = "DetectBlockingMods";

	private const string TargetPropName = "IsBlocked";

	private static readonly string[] CandidateMethodNames = new string[5] { "DetectBlockingMods", "CheckBlockedMods", "CheckIncompatible", "ValidateMods", "ScanBlockedMods" };

	private static Harmony _h;

	private static bool _done;

	public static void Init()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		_h = new Harmony("sts2.runtime.compat");
		AppDomain.CurrentDomain.AssemblyLoad += delegate
		{
			Try();
		};
		Try();
	}

	private static void Try()
	{
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Expected O, but got Unknown
		//IL_014b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Expected O, but got Unknown
		if (_done)
		{
			return;
		}
		Type type = null;
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			try
			{
				type = assembly.GetType("LibraryOfRuina.compat.IncompatibleModGuard", throwOnError: false);
				if (type != null)
				{
					break;
				}
			}
			catch
			{
			}
		}
		if (type == null)
		{
			return;
		}
		string[] candidateMethodNames = CandidateMethodNames;
		foreach (string name in candidateMethodNames)
		{
			MethodInfo method = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (!(method == null) && !(method.ReturnType != typeof(bool)))
			{
				try
				{
					_h.Patch((MethodBase)method, new HarmonyMethod(typeof(RuntimeCompat).GetMethod("Pre", BindingFlags.Static | BindingFlags.Public)), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				}
				catch
				{
				}
			}
		}
		try
		{
			MethodInfo methodInfo = type.GetProperty("IsBlocked", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetGetMethod(nonPublic: true);
			if (methodInfo != null)
			{
				_h.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(RuntimeCompat).GetMethod("Pre2", BindingFlags.Static | BindingFlags.Public)), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
		}
		catch
		{
		}
		_done = true;
	}

	public static bool Pre(ref bool __result)
	{
		__result = false;
		return false;
	}

	public static bool Pre2(ref bool __result)
	{
		__result = false;
		return false;
	}
}
