using System;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Utils;

namespace RandomForeseer.RandomForeseerCode.Common;

internal sealed class PredictionTrace
{
	public PredictionTraceFrame? Current { get; private set; }

	public IDisposable Push(AbstractModel source, PredictionInvocation invocation)
	{
		PredictionTraceFrame frame = new PredictionTraceFrame
		{
			Parent = Current,
			Source = source,
			Invocation = invocation
		};
		Current = frame;
		return new DisposableAction(delegate
		{
			Pop(frame);
		});
	}

	private void Pop(PredictionTraceFrame frame)
	{
		if (Current != frame)
		{
			throw new InvalidOperationException("Prediction trace scopes are unbalanced.");
		}
		Current = frame.Parent;
	}
}
