using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.Common;

internal sealed class PredictionStateStore
{
	private readonly Dictionary<(AbstractModel Model, Type StateType), object> _states = new Dictionary<(AbstractModel, Type), object>();

	public TState Get<TState>(AbstractModel model) where TState : new()
	{
		return Get(model, () => new TState());
	}

	public TState Get<TState>(AbstractModel model, Func<TState> create)
	{
		(AbstractModel, Type) key = (model, typeof(TState));
		if (!_states.TryGetValue(key, out object value))
		{
			TState val = create();
			if (val == null)
			{
				throw new InvalidOperationException("Prediction state factory returned null.");
			}
			value = val;
			_states[key] = value;
		}
		return (TState)value;
	}
}
