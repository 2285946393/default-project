using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;

namespace JmcModLib.Dispatch.Bootstrap;

internal static class BootstrapDependencyResolver
{
	[CompilerGenerated]
	private sealed class _003CEnumerateDependencyCandidates_003Ed__13 : IEnumerable<string>, IEnumerable, IEnumerator<string>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private string _003C_003E2__current;

		private int _003C_003El__initialThreadId;

		private string modDirectory;

		public string _003C_003E3__modDirectory;

		private string dependency;

		public string _003C_003E3__dependency;

		private string _003CnormalizedDependency_003E5__1;

		private List<string>.Enumerator _003C_003Es__2;

		private string _003CprobeDirectory_003E5__3;

		string IEnumerator<string>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CEnumerateDependencyCandidates_003Ed__13(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
			_003C_003El__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			int num = _003C_003E1__state;
			if (num == -3 || num == 3)
			{
				try
				{
				}
				finally
				{
					_003C_003Em__Finally1();
				}
			}
			_003CnormalizedDependency_003E5__1 = null;
			_003C_003Es__2 = default(List<string>.Enumerator);
			_003CprobeDirectory_003E5__3 = null;
			_003C_003E1__state = -2;
		}

		private bool MoveNext()
		{
			try
			{
				switch (_003C_003E1__state)
				{
				default:
					return false;
				case 0:
					_003C_003E1__state = -1;
					_003CnormalizedDependency_003E5__1 = (dependency.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? dependency : (dependency + ".dll"));
					if (Path.IsPathRooted(_003CnormalizedDependency_003E5__1))
					{
						_003C_003E2__current = _003CnormalizedDependency_003E5__1;
						_003C_003E1__state = 1;
						return true;
					}
					_003C_003E2__current = Path.GetFullPath(Path.Combine(modDirectory, _003CnormalizedDependency_003E5__1));
					_003C_003E1__state = 2;
					return true;
				case 1:
					_003C_003E1__state = -1;
					return false;
				case 2:
					_003C_003E1__state = -1;
					_003C_003Es__2 = ProbeDirectories.GetEnumerator();
					_003C_003E1__state = -3;
					break;
				case 3:
					_003C_003E1__state = -3;
					_003CprobeDirectory_003E5__3 = null;
					break;
				}
				if (_003C_003Es__2.MoveNext())
				{
					_003CprobeDirectory_003E5__3 = _003C_003Es__2.Current;
					_003C_003E2__current = Path.GetFullPath(Path.Combine(_003CprobeDirectory_003E5__3, _003CnormalizedDependency_003E5__1));
					_003C_003E1__state = 3;
					return true;
				}
				_003C_003Em__Finally1();
				_003C_003Es__2 = default(List<string>.Enumerator);
				return false;
			}
			catch
			{
				//try-fault
				((IDisposable)this).Dispose();
				throw;
			}
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		private void _003C_003Em__Finally1()
		{
			_003C_003E1__state = -1;
			((IDisposable)_003C_003Es__2).Dispose();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			_003CEnumerateDependencyCandidates_003Ed__13 _003CEnumerateDependencyCandidates_003Ed__;
			if (_003C_003E1__state == -2 && _003C_003El__initialThreadId == Environment.CurrentManagedThreadId)
			{
				_003C_003E1__state = 0;
				_003CEnumerateDependencyCandidates_003Ed__ = this;
			}
			else
			{
				_003CEnumerateDependencyCandidates_003Ed__ = new _003CEnumerateDependencyCandidates_003Ed__13(0);
			}
			_003CEnumerateDependencyCandidates_003Ed__.modDirectory = _003C_003E3__modDirectory;
			_003CEnumerateDependencyCandidates_003Ed__.dependency = _003C_003E3__dependency;
			return _003CEnumerateDependencyCandidates_003Ed__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<string>)this).GetEnumerator();
		}
	}

	private static readonly ConcurrentDictionary<string, string> AssemblyPaths = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static readonly List<string> ProbeDirectories = new List<string>();

	private static readonly object ConfigureLock = new object();

	[ThreadStatic]
	private static HashSet<string>? resolvingNames;

	private static AssemblyLoadContext? loadContext;

	private static int installed;

	public static void Install(string modDirectory, DispatchDescriptor descriptor, DispatchEntry entry)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(modDirectory, "modDirectory");
		ArgumentNullException.ThrowIfNull(descriptor, "descriptor");
		ArgumentNullException.ThrowIfNull(entry, "entry");
		lock (ConfigureLock)
		{
			if (loadContext == null)
			{
				loadContext = AssemblyLoadContext.GetLoadContext(typeof(BootstrapDependencyResolver).Assembly) ?? AssemblyLoadContext.Default;
			}
			AddProbeDirectories(modDirectory, entry.ProbeDirectories);
			AddDependencyPath(modDirectory, entry.RuntimeAssembly);
			foreach (string dependency in entry.Dependencies)
			{
				AddDependencyPath(modDirectory, dependency);
			}
			if (descriptor.ProbeAllDlls || entry.EffectiveProbeAllDlls)
			{
				AddAllDllsFromProbeDirectories();
			}
			if (Interlocked.Exchange(ref installed, 1) == 0)
			{
				loadContext.Resolving += ResolveFromLoadContext;
				AppDomain.CurrentDomain.AssemblyResolve += ResolveFromAppDomain;
				BootstrapLog.Info("分派依赖解析器已安装。");
			}
		}
	}

	public static Assembly LoadRuntimeAssembly(string runtimeAssembly)
	{
		string text = FindDependencyPath(runtimeAssembly);
		if (text == null)
		{
			throw new FileNotFoundException("找不到 Runtime DLL：" + runtimeAssembly);
		}
		AssemblyName assemblyName = AssemblyName.GetAssemblyName(text);
		Assembly assembly = FindLoadedAssembly(assemblyName);
		if (assembly != null)
		{
			string location = assembly.Location;
			if (!PathEquals(location, text))
			{
				throw new InvalidOperationException($"Runtime 程序集标识 {assemblyName.Name} 已被 {location} 占用。" + "请将 Runtime 项目的 AssemblyName 设为不同于入口 DLL 的名称，例如 MyMod.Runtime。");
			}
			BootstrapLog.Info("Runtime 已加载，复用程序集：" + assembly.FullName);
			return assembly;
		}
		Assembly result = (loadContext ?? AssemblyLoadContext.Default).LoadFromAssemblyPath(text);
		BootstrapLog.Info("已加载 Runtime：" + text);
		return result;
	}

	private static Assembly? ResolveFromLoadContext(AssemblyLoadContext context, AssemblyName requestedAssembly)
	{
		return ResolveAssembly(context, requestedAssembly);
	}

	private static Assembly? ResolveFromAppDomain(object? sender, ResolveEventArgs args)
	{
		return ResolveAssembly(loadContext ?? AssemblyLoadContext.Default, new AssemblyName(args.Name));
	}

	private static Assembly? ResolveAssembly(AssemblyLoadContext context, AssemblyName requestedAssembly)
	{
		string text = requestedAssembly.Name ?? string.Empty;
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (resolvingNames == null)
		{
			resolvingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		if (!resolvingNames.Add(text))
		{
			return null;
		}
		try
		{
			Assembly assembly = FindLoadedAssembly(requestedAssembly);
			if (assembly != null)
			{
				return assembly;
			}
			string text2 = FindDependencyPath(text);
			if (text2 == null)
			{
				return null;
			}
			AssemblyName assemblyName = AssemblyName.GetAssemblyName(text2);
			if (!IsCompatible(assemblyName, requestedAssembly))
			{
				BootstrapLog.Warn($"跳过不兼容依赖：请求 {requestedAssembly.FullName}，找到 {assemblyName.FullName}，路径 {text2}");
				return null;
			}
			Assembly result = context.LoadFromAssemblyPath(text2);
			BootstrapLog.Info("已解析依赖：" + requestedAssembly.Name + " -> " + text2);
			return result;
		}
		catch (Exception ex) when (((ex is IOException || ex is BadImageFormatException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			BootstrapLog.Error($"解析依赖失败：{requestedAssembly.FullName}\n{ex}");
			return null;
		}
		finally
		{
			resolvingNames.Remove(text);
		}
	}

	private static void AddProbeDirectories(string modDirectory, IEnumerable<string> probeDirectories)
	{
		foreach (string probeDirectory in probeDirectories)
		{
			string text = ResolvePath(modDirectory, probeDirectory);
			if (Directory.Exists(text) && !ProbeDirectories.Contains<string>(text, StringComparer.OrdinalIgnoreCase))
			{
				ProbeDirectories.Add(text);
				BootstrapLog.Info("添加依赖探测目录：" + text);
			}
		}
	}

	private static void AddDependencyPath(string modDirectory, string dependency)
	{
		foreach (string item in EnumerateDependencyCandidates(modDirectory, dependency))
		{
			if (File.Exists(item))
			{
				AddAssemblyPath(item);
				return;
			}
		}
		BootstrapLog.Warn("依赖文件不存在：" + dependency);
	}

	[IteratorStateMachine(typeof(_003CEnumerateDependencyCandidates_003Ed__13))]
	private static IEnumerable<string> EnumerateDependencyCandidates(string modDirectory, string dependency)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CEnumerateDependencyCandidates_003Ed__13(-2)
		{
			_003C_003E3__modDirectory = modDirectory,
			_003C_003E3__dependency = dependency
		};
	}

	private static void AddAllDllsFromProbeDirectories()
	{
		foreach (string probeDirectory in ProbeDirectories)
		{
			foreach (string item in Directory.EnumerateFiles(probeDirectory, "*.dll", SearchOption.TopDirectoryOnly))
			{
				AddAssemblyPath(item);
			}
		}
	}

	private static void AddAssemblyPath(string assemblyPath)
	{
		try
		{
			AssemblyName assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
			if (!string.IsNullOrWhiteSpace(assemblyName.Name))
			{
				AssemblyPaths[assemblyName.Name] = assemblyPath;
			}
		}
		catch (Exception ex) when (((ex is IOException || ex is BadImageFormatException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			BootstrapLog.Warn("跳过无效依赖：" + assemblyPath + "，原因：" + ex.Message);
		}
	}

	private static string? FindDependencyPath(string assemblyNameOrPath)
	{
		string text = NormalizeAssemblyName(assemblyNameOrPath);
		if (!string.IsNullOrWhiteSpace(text) && AssemblyPaths.TryGetValue(text, out string value))
		{
			return value;
		}
		string path = (assemblyNameOrPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? Path.GetFileName(assemblyNameOrPath) : (text + ".dll"));
		foreach (string probeDirectory in ProbeDirectories)
		{
			string text2 = Path.Combine(probeDirectory, path);
			if (File.Exists(text2))
			{
				AddAssemblyPath(text2);
				return text2;
			}
		}
		return null;
	}

	private static Assembly? FindLoadedAssembly(AssemblyName requestedAssembly)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			AssemblyName name = assembly.GetName();
			if (IsCompatible(name, requestedAssembly))
			{
				return assembly;
			}
		}
		return null;
	}

	private static bool IsCompatible(AssemblyName candidate, AssemblyName requested)
	{
		if (!string.Equals(candidate.Name, requested.Name, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		byte[] publicKeyToken = requested.GetPublicKeyToken();
		byte[] publicKeyToken2 = candidate.GetPublicKeyToken();
		if (publicKeyToken != null && publicKeyToken.Length > 0 && (publicKeyToken2 == null || publicKeyToken2.Length <= 0 || !((ReadOnlySpan<byte>)publicKeyToken).SequenceEqual((ReadOnlySpan<byte>)publicKeyToken2)))
		{
			return false;
		}
		if (requested.Version == null || candidate.Version == null)
		{
			return true;
		}
		return candidate.Version.CompareTo(requested.Version) >= 0;
	}

	private static string ResolvePath(string baseDirectory, string path)
	{
		return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(baseDirectory, path));
	}

	private static string NormalizeAssemblyName(string assemblyName)
	{
		if (string.IsNullOrWhiteSpace(assemblyName))
		{
			return string.Empty;
		}
		string text = assemblyName.Trim();
		if (text.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
		{
			return Path.GetFileNameWithoutExtension(text);
		}
		return text.Contains(',') ? (new AssemblyName(text).Name ?? string.Empty) : text;
	}

	private static bool PathEquals(string left, string right)
	{
		if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
		{
			return false;
		}
		return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
	}
}
