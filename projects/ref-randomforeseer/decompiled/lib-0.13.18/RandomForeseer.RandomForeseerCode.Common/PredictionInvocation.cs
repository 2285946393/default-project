using System.Reflection;

namespace RandomForeseer.RandomForeseerCode.Common;

internal readonly record struct PredictionInvocation(MethodInfo? Method, PredictionActionKind? Action)
{
	public static PredictionInvocation ForMethod(MethodInfo method)
	{
		return new PredictionInvocation(method, null);
	}

	public static PredictionInvocation ForAction(PredictionActionKind action)
	{
		return new PredictionInvocation(null, action);
	}
}
