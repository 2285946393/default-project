namespace RandomForeseer.RandomForeseerCode.Common.Mirrors;

internal readonly record struct MirrorDispatchResult(MirrorDispatchKind Kind);
internal readonly record struct MirrorDispatchResult<TResult>(MirrorDispatchKind Kind, TResult Value);
