using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal sealed class EventPredictionRegistry
{
	private readonly Dictionary<Type, Func<EventModel, EventOption, IReadOnlyList<IHoverTip>>> _predictors = new Dictionary<Type, Func<EventModel, EventOption, IReadOnlyList<IHoverTip>>>();

	public void Register<TEvent>(Func<TEvent, EventOption, IReadOnlyList<IHoverTip>> predictor) where TEvent : EventModel
	{
		Func<TEvent, EventOption, IReadOnlyList<IHoverTip>> predictor2 = predictor;
		Type typeFromHandle = typeof(TEvent);
		if (!_predictors.TryAdd(typeFromHandle, (EventModel eventModel, EventOption option) => predictor2((TEvent)(object)eventModel, option)))
		{
			Entry.Logger.Warn($"Duplicate event option prediction registration ignored: {typeFromHandle}", 1);
		}
	}

	public bool TryPredict(EventModel eventModel, EventOption option, out IReadOnlyList<IHoverTip> hoverTips)
	{
		if (_predictors.TryGetValue(((object)eventModel).GetType(), out Func<EventModel, EventOption, IReadOnlyList<IHoverTip>> value))
		{
			try
			{
				hoverTips = value(eventModel, option);
				return true;
			}
			catch (Exception ex)
			{
				Entry.Logger.Warn($"Event option prediction failed for {((AbstractModel)eventModel).Id} {option.TextKey}: {ex}", 1);
				ModTelemetry.CaptureException(ex, "event_option_prediction", "build_hover_tips", new
				{
					Event = TelemetryContext.ForModel((AbstractModel)(object)eventModel),
					Option = option.TextKey
				});
			}
		}
		hoverTips = Array.Empty<IHoverTip>();
		return false;
	}
}
