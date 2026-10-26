using System;
using MegaCrit.Sts2.Core.Logging;

namespace JmcModLib.Dispatch.Bootstrap;

internal static class BootstrapLog
{
	private const string Prefix = "[JmcModLib.Dispatch] ";

	public static void Info(string message)
	{
		Write((LogLevel)3, message);
	}

	public static void Warn(string message)
	{
		Write((LogLevel)4, message);
	}

	public static void Error(string message)
	{
		Write((LogLevel)5, message);
	}

	private static void Write(LogLevel level, string message)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			Log.LogMessage(level, (LogType)0, "[JmcModLib.Dispatch] " + message, 1);
		}
		catch
		{
			try
			{
				Console.WriteLine("[JmcModLib.Dispatch] " + message);
			}
			catch
			{
			}
		}
	}
}
