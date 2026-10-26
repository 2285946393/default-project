using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Modding;

namespace JmcModLib.Dispatch.Bootstrap;

[ModInitializer("Initialize")]
internal static class DispatchBootstrap
{
	private const string DescriptorSuffix = ".dispatch.json";

	public static void Initialize()
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		string text = executingAssembly.GetName().Name ?? "UnknownMod";
		string text2 = ResolveModDirectory(executingAssembly);
		string descriptorPath = Path.Combine(text2, text + ".dispatch.json");
		GameVersionInfo currentGameVersion = GetCurrentGameVersion();
		DispatchDescriptor dispatchDescriptor = DispatchDescriptor.Load(descriptorPath, text);
		DispatchEntry dispatchEntry = dispatchDescriptor.SelectEntry(currentGameVersion);
		BootstrapDependencyResolver.Install(text2, dispatchDescriptor, dispatchEntry);
		Assembly runtimeAssembly = BootstrapDependencyResolver.LoadRuntimeAssembly(dispatchEntry.RuntimeAssembly);
		InvokeRuntimeInitializer(runtimeAssembly, dispatchDescriptor);
		BootstrapLog.Info("版本分派完成：" + dispatchEntry.Id + " -> " + dispatchEntry.RuntimeAssembly);
	}

	private static GameVersionInfo GetCurrentGameVersion()
	{
		try
		{
			ReleaseInfoManager instance = ReleaseInfoManager.Instance;
			ReleaseInfo releaseInfo = instance.ReleaseInfo;
			return new GameVersionInfo((releaseInfo != null) ? releaseInfo.Version : null, instance.SemVer);
		}
		catch (Exception ex)
		{
			BootstrapLog.Warn("读取 STS2 版本失败，将只匹配无版本范围的分派项：" + ex.Message);
			return new GameVersionInfo(null, null);
		}
	}

	private static void InvokeRuntimeInitializer(Assembly runtimeAssembly, DispatchDescriptor descriptor)
	{
		Type type = runtimeAssembly.GetType(descriptor.InitializerType, throwOnError: true) ?? throw new TypeLoadException("找不到 Runtime 初始化类型：" + descriptor.InitializerType);
		MethodInfo methodInfo = type.GetMethod(descriptor.InitializerMethod, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new MissingMethodException(descriptor.InitializerType, descriptor.InitializerMethod);
		try
		{
			methodInfo.Invoke(null, null);
		}
		catch (TargetInvocationException ex) when (ex.InnerException != null)
		{
			ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
		}
	}

	private static string ResolveModDirectory(Assembly assembly)
	{
		string location = assembly.Location;
		if (!string.IsNullOrWhiteSpace(location))
		{
			string directoryName = Path.GetDirectoryName(location);
			if (!string.IsNullOrWhiteSpace(directoryName))
			{
				return directoryName;
			}
		}
		return AppContext.BaseDirectory;
	}
}
