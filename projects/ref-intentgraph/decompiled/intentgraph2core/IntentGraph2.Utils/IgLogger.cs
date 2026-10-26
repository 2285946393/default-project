using MegaCrit.Sts2.Core.Logging;

namespace IntentGraph2.Utils;

internal class IgLogger
{
	public static void Info(string message)
	{
		Log.Info("[IntentGraph] " + message, 2);
	}

	public static void Warn(string message)
	{
		Log.Warn("[IntentGraph] " + message, 2);
	}

	public static void Error(string message)
	{
		Log.Error("[IntentGraph] " + message, 2);
	}

	public static void Debug(string message)
	{
		Log.Debug("[IntentGraph] " + message, 2);
	}

	public static void Verbose(string message)
	{
		Log.VeryDebug("[IntentGraph] " + message, 2);
	}
}
