using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using RandomForeseer.RandomForeseerCode.Localization;
using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;

namespace RandomForeseer.RandomForeseerCode.Telemetry;

internal static class ModTelemetry
{
	internal const string ManualCaptureSourcePrefix = "random_foreseer/";

	private const int MaxRecentExceptionFingerprints = 128;

	private static readonly Lock ExceptionFingerprintLock = new Lock();

	private static readonly Queue<string> RecentExceptionFingerprintOrder = new Queue<string>();

	private static readonly HashSet<string> RecentExceptionFingerprints = new HashSet<string>(StringComparer.Ordinal);

	private static readonly JsonSerializerOptions ContextJsonOptions = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters = { (JsonConverter)new JsonStringEnumConverter() }
	};

	private static ITelemetryClient? _client;

	public static void Register()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
		RitsuLibFramework.RegisterTelemetryContributionProvider((ITelemetryContributionProvider)(object)new SettingsTelemetryContribution());
		TelemetryApplicant val = new TelemetryApplicant();
		val.set_ApplicantId("RandomForeseer");
		val.set_OwnerModId("RandomForeseer");
		val.set_DisplayName("Random Foreseer");
		val.set_DisplayNameText(T("telemetry.applicant.display_name"));
		val.set_Adapter((ITelemetryAdapter)(object)new ModTelemetryAdapter(TelemetryBuildConfiguration.CreateAdapter()));
		TelemetryRequest[] array = new TelemetryRequest[3];
		TelemetryRequest val2 = new TelemetryRequest();
		val2.set_RequestId("basic_usage");
		val2.set_Category((TelemetryDataCategory)1);
		val2.set_Description("telemetry.basic_usage.description");
		val2.set_DescriptionText(T("telemetry.basic_usage.description"));
		val2.set_ContributionSubscriptions((IReadOnlyList<string>)new _003C_003Ez__ReadOnlySingleElementList<string>("settings_snapshot"));
		array[0] = val2;
		array[1] = TelemetryRequest.ModInventory(T("telemetry.mod_inventory.description"));
		array[2] = TelemetryRequest.Diagnostics(T("telemetry.diagnostics.description"), (IReadOnlyList<string>)null).WithCaptureFilter((Func<TelemetryCaptureContext, bool>)ModTelemetryFilter.ShouldCapture);
		val.set_Requests((IReadOnlyList<TelemetryRequest>)new _003C_003Ez__ReadOnlyArray<TelemetryRequest>((TelemetryRequest[])(object)array));
		RitsuLibFramework.RegisterTelemetryApplicant(val);
		_client = RitsuLibFramework.GetTelemetryClient("RandomForeseer");
	}

	public static void CaptureException(Exception exception, string subsystem, string operation, object? context = null)
	{
		try
		{
			ITelemetryClient client = _client;
			if (client != null && client.IsEnabled("diagnostics") && TryMarkRecent(exception, subsystem, operation))
			{
				Dictionary<string, object> dictionary = new Dictionary<string, object>
				{
					["capture_mode"] = "manual",
					["capture_source"] = "random_foreseer/" + subsystem,
					["subsystem"] = subsystem,
					["operation"] = operation
				};
				if (context != null)
				{
					dictionary["exception_context"] = SerializeContext(context);
				}
				client.CaptureException(exception, (IReadOnlyDictionary<string, object>)dictionary);
			}
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Telemetry exception capture failed for {subsystem}/{operation}: {ex.Message}", 1);
		}
	}

	private static JsonNode? SerializeContext(object? context)
	{
		if (context == null)
		{
			return null;
		}
		Type type = context.GetType();
		try
		{
			return JsonSerializer.SerializeToNode(context, type, ContextJsonOptions);
		}
		catch (Exception ex)
		{
			Entry.Logger.Warn($"Telemetry exception context serialization failed for {type}: {ex.Message}", 1);
			return new JsonObject
			{
				["type"] = type.FullName ?? type.Name,
				["serialization_error"] = ex.Message
			};
		}
	}

	private static bool TryMarkRecent(Exception exception, string subsystem, string operation)
	{
		string text = exception.StackTrace?.Split('\n').FirstOrDefault()?.Trim() ?? string.Empty;
		_003C_003Ey__InlineArray5<string> buffer = default(_003C_003Ey__InlineArray5<string>);
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 0) = subsystem;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 1) = operation;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 2) = exception.GetType().FullName ?? exception.GetType().Name;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 3) = exception.Message;
		global::_003CPrivateImplementationDetails_003E.InlineArrayElementRef<_003C_003Ey__InlineArray5<string>, string>(ref buffer, 4) = text;
		string item = string.Join('\n', global::_003CPrivateImplementationDetails_003E.InlineArrayAsReadOnlySpan<_003C_003Ey__InlineArray5<string>, string>(in buffer, 5));
		using (ExceptionFingerprintLock.EnterScope())
		{
			if (!RecentExceptionFingerprints.Add(item))
			{
				return false;
			}
			RecentExceptionFingerprintOrder.Enqueue(item);
			while (RecentExceptionFingerprintOrder.Count > 128)
			{
				RecentExceptionFingerprints.Remove(RecentExceptionFingerprintOrder.Dequeue());
			}
			return true;
		}
	}

	private static ModSettingsText T(string key)
	{
		return ModSettingsText.I18N(ModLocalization.SettingsLocalization, key, key);
	}
}
