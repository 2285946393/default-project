using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.HoverTips;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.Common.HoverTips;

internal sealed class PredictionHoverTipRegistry<TInput>
{
	private readonly record struct Provider(string Name, Func<TInput, IEnumerable<IHoverTip>> GetHoverTips);

	private readonly List<Provider> _providers = new List<Provider>();

	public void Register(string name, Func<TInput, IEnumerable<IHoverTip>> provider)
	{
		string name2 = name;
		if (_providers.Any((Provider existingProvider) => existingProvider.Name == name2))
		{
			Entry.Logger.Warn($"Duplicate hover tip prediction provider registration ignored: {typeof(TInput)} '{name2}'", 1);
		}
		else
		{
			_providers.Add(new Provider(name2, provider));
		}
	}

	public IReadOnlyList<IHoverTip> GetHoverTips(TInput input)
	{
		List<IHoverTip> list = new List<IHoverTip>();
		foreach (Provider provider in _providers)
		{
			try
			{
				list.AddRange(provider.GetHoverTips(input));
			}
			catch (Exception ex)
			{
				object obj = input?.GetType();
				if (obj == null)
				{
					obj = typeof(TInput);
				}
				Type type = (Type)obj;
				Entry.Logger.Warn($"Hover tip prediction provider '{provider.Name}' failed for {type}: {ex}", 1);
				ModTelemetry.CaptureException(ex, "prediction_hover_tip_registry", "build_hover_tips", new
				{
					Provider = provider.Name,
					InputType = TelemetryContext.ForType(type)
				});
			}
		}
		return list;
	}
}
