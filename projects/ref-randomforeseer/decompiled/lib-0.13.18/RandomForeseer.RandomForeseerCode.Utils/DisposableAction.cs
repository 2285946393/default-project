using System;
using System.Threading;

namespace RandomForeseer.RandomForeseerCode.Utils;

public sealed class DisposableAction : IDisposable
{
	private Action? _action = action ?? throw new ArgumentNullException("action");

	public DisposableAction(Action action)
	{
	}

	public void Dispose()
	{
		Interlocked.Exchange(ref _action, null)?.Invoke();
	}
}
