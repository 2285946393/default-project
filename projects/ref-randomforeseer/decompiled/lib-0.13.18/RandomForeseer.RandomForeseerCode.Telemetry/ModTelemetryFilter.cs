using System;
using System.Text.Json.Nodes;
using STS2RitsuLib.Telemetry;

namespace RandomForeseer.RandomForeseerCode.Telemetry;

internal static class ModTelemetryFilter
{
	private const string StackTraceMarker = "RandomForeseer.";

	internal static bool ShouldCapture(TelemetryCaptureContext context)
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		string eventName = ((TelemetryCaptureContext)(ref context)).EventName;
		if (!(eventName == "exception"))
		{
			if (eventName == "godot_engine_error")
			{
				return ShouldCaptureGodotEngineError(context);
			}
			return true;
		}
		return ShouldCaptureException(context);
	}

	private static bool ShouldCaptureGodotEngineError(TelemetryCaptureContext context)
	{
		if (((TelemetryCaptureContext)(ref context)).SourceData is JsonObject jsonObject)
		{
			if (!ContainsModStackTrace(jsonObject["script_backtrace"]))
			{
				return ContainsModStackTrace(jsonObject["code"]);
			}
			return true;
		}
		return false;
	}

	private static bool ShouldCaptureException(TelemetryCaptureContext context)
	{
		string source = ((TelemetryCaptureContext)(ref context)).Source;
		if (source != null && source.StartsWith("random_foreseer/", StringComparison.Ordinal))
		{
			return true;
		}
		for (Exception ex = ((TelemetryCaptureContext)(ref context)).Exception; ex != null; ex = ex.InnerException)
		{
			string? stackTrace = ex.StackTrace;
			if (stackTrace != null && stackTrace.Contains("RandomForeseer.", StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static bool ContainsModStackTrace(JsonNode? node)
	{
		if (node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out string value))
		{
			return value.Contains("RandomForeseer.", StringComparison.Ordinal);
		}
		return false;
	}
}
