using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace JmcModLib.Dispatch.Bootstrap;

internal sealed class DispatchDescriptor
{
	public string InitializerType { get; set; } = string.Empty;


	public string InitializerMethod { get; set; } = "Initialize";


	public List<DispatchEntry> Entries { get; set; } = new List<DispatchEntry>();


	public string? FallbackRuntimeAssembly { get; set; }

	public List<string> FallbackProbeDirectories { get; set; } = new List<string>();


	public List<string> FallbackDependencies { get; set; } = new List<string>();


	public bool ProbeAllDlls { get; set; }

	public static DispatchDescriptor Load(string descriptorPath, string modName)
	{
		if (!File.Exists(descriptorPath))
		{
			throw new FileNotFoundException("找不到分派配置文件：" + descriptorPath);
		}
		try
		{
			string json = File.ReadAllText(descriptorPath);
			DispatchDescriptor dispatchDescriptor = JsonSerializer.Deserialize<DispatchDescriptor>(json, new JsonSerializerOptions
			{
				AllowTrailingCommas = true,
				PropertyNameCaseInsensitive = true,
				ReadCommentHandling = JsonCommentHandling.Skip
			}) ?? new DispatchDescriptor();
			dispatchDescriptor.Normalize(modName);
			BootstrapLog.Info("已读取分派配置：" + descriptorPath);
			return dispatchDescriptor;
		}
		catch (JsonException innerException)
		{
			throw new InvalidOperationException("分派配置 JSON 格式无效：" + descriptorPath, innerException);
		}
	}

	public DispatchEntry SelectEntry(GameVersionInfo gameVersion)
	{
		BootstrapLog.Info("当前 STS2 版本：" + (gameVersion.RawVersion ?? "<未知>"));
		foreach (DispatchEntry entry in Entries)
		{
			if (entry.IsMatch(gameVersion))
			{
				BootstrapLog.Info("命中分派项：" + entry.Id);
				return entry;
			}
		}
		throw new InvalidOperationException("没有找到匹配当前 STS2 版本 " + (gameVersion.RawVersion ?? "<未知>") + " 的 Runtime 分派项。");
	}

	public static List<string> NormalizeList(IEnumerable<string>? values)
	{
		if (values == null)
		{
			return new List<string>();
		}
		List<string> list = new List<string>();
		foreach (string value in values)
		{
			string[] array = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			foreach (string text in array)
			{
				if (!string.IsNullOrWhiteSpace(text) && !list.Contains<string>(text, StringComparer.OrdinalIgnoreCase))
				{
					list.Add(text);
				}
			}
		}
		return list;
	}

	private void Normalize(string modName)
	{
		InitializerType = NormalizeText(InitializerType, modName + ".MainFile");
		InitializerMethod = NormalizeText(InitializerMethod, "Initialize");
		FallbackProbeDirectories = NormalizeList(FallbackProbeDirectories);
		FallbackDependencies = NormalizeList(FallbackDependencies);
		foreach (DispatchEntry entry in Entries)
		{
			entry.Normalize();
		}
		if (!string.IsNullOrWhiteSpace(FallbackRuntimeAssembly))
		{
			DispatchEntry dispatchEntry = new DispatchEntry
			{
				Id = "fallback",
				RuntimeAssembly = FallbackRuntimeAssembly,
				ProbeDirectories = FallbackProbeDirectories,
				Dependencies = FallbackDependencies,
				ProbeAllDlls = ProbeAllDlls
			};
			dispatchEntry.Normalize();
			Entries.Add(dispatchEntry);
		}
		if (Entries.Count == 0)
		{
			throw new InvalidOperationException("分派配置至少需要一个 entries 项或 fallbackRuntimeAssembly。");
		}
	}

	private static string NormalizeText(string? text, string fallback)
	{
		return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
	}
}
