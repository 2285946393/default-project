using System;
using System.IO;
using Godot;

namespace LoadOrderManager;

internal static class DebugLog
{
	private static readonly object Sync = new object();

	private static string? _logPath;

	public static string LogPath
	{
		get
		{
			if (!string.IsNullOrWhiteSpace(_logPath))
			{
				return _logPath;
			}
			try
			{
				string text = Path.Combine(OS.GetUserDataDir(), "LoadOrderManager");
				Directory.CreateDirectory(text);
				_logPath = Path.Combine(text, "load_order_manager.log");
			}
			catch
			{
				_logPath = "load_order_manager.log";
			}
			return _logPath;
		}
	}

	public static void Info(string message)
	{
		Write("INFO", message, null);
	}

	public static void Warn(string message)
	{
		Write("WARN", message, null);
	}

	public static void Error(string message, Exception? ex = null)
	{
		Write("ERROR", message, ex);
	}

	private static void Write(string level, string message, Exception? ex)
	{
		string value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
		string text = $"[LoadOrderManager] [{value}] [{level}] {message}";
		string text2 = ((ex == null) ? text : $"{text}{Environment.NewLine}{ex}");
		try
		{
			if (level == "ERROR")
			{
				GD.PrintErr(text);
			}
			else
			{
				GD.Print(text);
			}
		}
		catch
		{
		}
		try
		{
			lock (Sync)
			{
				File.AppendAllText(LogPath, text2 + Environment.NewLine);
			}
		}
		catch
		{
		}
	}
}
