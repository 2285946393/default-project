using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Modding;

namespace RandomForeseer.RandomForeseerCode.Common.Mirrors;

internal sealed class MethodMirrorRegistry<TBase, TContext> where TBase : class where TContext : IMethodMirrorContext<TBase>
{
	private readonly record struct LookupResult(MirrorDispatchKind Kind, Action<TBase, TContext>? Handler);

	[CompilerGenerated]
	private MirrorMethodSpec _003Cmethod_003EP;

	private readonly Dictionary<Type, LookupResult> _registrations;

	private readonly Dictionary<Type, LookupResult> _lookupCache;

	private MethodMirrorInferrer<TBase, TContext>? _inferrer;

	private bool _allowInference;

	public bool AllowInference
	{
		get
		{
			return _allowInference;
		}
		set
		{
			if (_allowInference != value)
			{
				_allowInference = value;
				InvalidateLookupCache();
			}
		}
	}

	public MethodMirrorRegistry(MirrorMethodSpec method)
	{
		_003Cmethod_003EP = method;
		_registrations = new Dictionary<Type, LookupResult>();
		_lookupCache = new Dictionary<Type, LookupResult>();
		_allowInference = true;
		base._002Ector();
	}

	public void InvalidateLookupCache()
	{
		_lookupCache.Clear();
	}

	public void Register<TModel>(Action<TModel, TContext> handler) where TModel : TBase
	{
		Action<TModel, TContext> handler2 = handler;
		Type typeFromHandle = typeof(TModel);
		ValidateOverride(typeFromHandle);
		_registrations.Add(typeFromHandle, new LookupResult(MirrorDispatchKind.Handled, delegate(TBase receiver, TContext context)
		{
			handler2((TModel)(object)receiver, context);
		}));
	}

	public void RegisterIgnored<TModel>() where TModel : TBase
	{
		Type typeFromHandle = typeof(TModel);
		ValidateOverride(typeFromHandle);
		_registrations.Add(typeFromHandle, new LookupResult(MirrorDispatchKind.Ignored, null));
	}

	public void RegisterInferrer(MethodMirrorInferrer<TBase, TContext> inferrer)
	{
		ArgumentNullException.ThrowIfNull(inferrer, "inferrer");
		if (_inferrer != null)
		{
			throw new InvalidOperationException("Mirror for " + _003Cmethod_003EP.Name + " already has a registered inferrer.");
		}
		_inferrer = inferrer;
	}

	public bool HasRegisteredHandler(TBase receiver)
	{
		if (_registrations.TryGetValue(receiver.GetType(), out var value))
		{
			return value.Kind == MirrorDispatchKind.Handled;
		}
		return false;
	}

	public bool TryInvokeRegistered(TBase receiver, TContext context, out MirrorDispatchResult dispatchResult)
	{
		if (!_registrations.TryGetValue(receiver.GetType(), out var value))
		{
			dispatchResult = default(MirrorDispatchResult);
			return false;
		}
		if (value.Kind == MirrorDispatchKind.Handled)
		{
			using (context.PushDispatchSource(receiver, _003Cmethod_003EP))
			{
				value.Handler(receiver, context);
			}
		}
		dispatchResult = new MirrorDispatchResult(value.Kind);
		return true;
	}

	public MirrorDispatchResult Invoke(TBase receiver, TContext context)
	{
		var (mirrorDispatchKind2, action2) = (LookupResult)(ref Lookup(receiver.GetType()));
		if ((mirrorDispatchKind2 == MirrorDispatchKind.NotOverridden || mirrorDispatchKind2 == MirrorDispatchKind.Ignored) ? true : false)
		{
			return new MirrorDispatchResult(mirrorDispatchKind2);
		}
		using (context.PushDispatchSource(receiver, _003Cmethod_003EP))
		{
			switch (mirrorDispatchKind2)
			{
			case MirrorDispatchKind.Handled:
				action2(receiver, context);
				break;
			case MirrorDispatchKind.Inferred:
				context.RecordMethodMirrorIncompleteRisk();
				action2(receiver, context);
				break;
			case MirrorDispatchKind.Unsupported:
				context.RecordMethodNotMirroredRisk();
				break;
			default:
				throw new InvalidOperationException($"Unexpected mirror dispatch kind {mirrorDispatchKind2}.");
			}
			return new MirrorDispatchResult(mirrorDispatchKind2);
		}
	}

	private LookupResult Lookup(Type type)
	{
		if (_registrations.TryGetValue(type, out var value) || _lookupCache.TryGetValue(type, out value))
		{
			return value;
		}
		Mod mod;
		if (!_003Cmethod_003EP.TryGetOverride(type, out MethodInfo overrideMethod))
		{
			value = new LookupResult(MirrorDispatchKind.NotOverridden, null);
		}
		else if (TryGetMod(overrideMethod, out mod) && !(mod.manifest?.affectsGameplay ?? true))
		{
			Entry.Logger.Info($"Mirror for {_003Cmethod_003EP.Name} ignored unsupported {type.FullName} from non-gameplay mod {mod.manifest?.id}.", 1);
			value = new LookupResult(MirrorDispatchKind.Ignored, null);
		}
		else
		{
			if (_allowInference)
			{
				Action<TBase, TContext> action = _inferrer?.Invoke(type, overrideMethod);
				if (action != null)
				{
					Entry.Logger.Info($"Mirror for {_003Cmethod_003EP.Name} will best-effort infer behavior for unregistered {type.FullName}.", 1);
					value = new LookupResult(MirrorDispatchKind.Inferred, action);
					goto IL_0210;
				}
			}
			Entry.Logger.Info($"No mirror is registered for {_003Cmethod_003EP.Name} on {type.FullName}; preview results may be incomplete.", 1);
			value = new LookupResult(MirrorDispatchKind.Unsupported, null);
		}
		goto IL_0210;
		IL_0210:
		_lookupCache.Add(type, value);
		return value;
	}

	private static bool TryGetMod(MethodInfo overrideMethod, [NotNullWhen(true)] out Mod? mod)
	{
		Type declaringType = overrideMethod.DeclaringType;
		if ((object)declaringType == null)
		{
			mod = null;
			return false;
		}
		bool flag = default(bool);
		mod = AssemblyInfo.ModForType(declaringType, ref flag);
		if (!flag)
		{
			return mod != null;
		}
		return false;
	}

	private void ValidateOverride(Type type)
	{
		if (!_003Cmethod_003EP.TryGetOverride(type, out MethodInfo _))
		{
			throw new InvalidOperationException($"{type.FullName} does not override {_003Cmethod_003EP.BaseMethod.DeclaringType?.FullName}.{_003Cmethod_003EP.Name}.");
		}
	}
}
internal sealed class MethodMirrorRegistry<TBase, TContext, TResult> where TBase : class where TContext : IMethodMirrorContext<TBase>
{
	private readonly record struct LookupResult(MirrorDispatchKind Kind, Func<TBase, TContext, TResult>? Handler);

	[CompilerGenerated]
	private MirrorMethodSpec _003Cmethod_003EP;

	private readonly Dictionary<Type, LookupResult> _registrations;

	private readonly Dictionary<Type, LookupResult> _lookupCache;

	public MethodMirrorRegistry(MirrorMethodSpec method)
	{
		_003Cmethod_003EP = method;
		_registrations = new Dictionary<Type, LookupResult>();
		_lookupCache = new Dictionary<Type, LookupResult>();
		base._002Ector();
	}

	public void Register<TModel>(Func<TModel, TContext, TResult> handler) where TModel : TBase
	{
		Func<TModel, TContext, TResult> handler2 = handler;
		Type typeFromHandle = typeof(TModel);
		ValidateOverride(typeFromHandle);
		_registrations.Add(typeFromHandle, new LookupResult(MirrorDispatchKind.Handled, (TBase receiver, TContext context) => handler2((TModel)(object)receiver, context)));
	}

	public bool TryInvokeRegistered(TBase receiver, TContext context, out MirrorDispatchResult<TResult> dispatchResult)
	{
		if (!_registrations.TryGetValue(receiver.GetType(), out var value))
		{
			dispatchResult = default(MirrorDispatchResult<TResult>);
			return false;
		}
		using (context.PushDispatchSource(receiver, _003Cmethod_003EP))
		{
			dispatchResult = new MirrorDispatchResult<TResult>(value.Kind, value.Handler(receiver, context));
		}
		return true;
	}

	public MirrorDispatchResult<TResult> Invoke(TBase receiver, TContext context, TResult defaultResult)
	{
		var (mirrorDispatchKind2, func2) = (LookupResult)(ref Lookup(receiver.GetType()));
		if ((mirrorDispatchKind2 == MirrorDispatchKind.NotOverridden || mirrorDispatchKind2 == MirrorDispatchKind.Ignored) ? true : false)
		{
			return new MirrorDispatchResult<TResult>(mirrorDispatchKind2, defaultResult);
		}
		using (context.PushDispatchSource(receiver, _003Cmethod_003EP))
		{
			if (mirrorDispatchKind2 == MirrorDispatchKind.Handled)
			{
				return new MirrorDispatchResult<TResult>(mirrorDispatchKind2, func2(receiver, context));
			}
			context.RecordMethodNotMirroredRisk();
			return new MirrorDispatchResult<TResult>(mirrorDispatchKind2, defaultResult);
		}
	}

	private LookupResult Lookup(Type type)
	{
		if (_registrations.TryGetValue(type, out var value) || _lookupCache.TryGetValue(type, out value))
		{
			return value;
		}
		if (!_003Cmethod_003EP.TryGetOverride(type, out MethodInfo _))
		{
			value = new LookupResult(MirrorDispatchKind.NotOverridden, null);
		}
		else
		{
			Entry.Logger.Info($"No mirror is registered for {_003Cmethod_003EP.Name} on {type.FullName}; preview results may be incomplete.", 1);
			value = new LookupResult(MirrorDispatchKind.Unsupported, null);
		}
		_lookupCache.Add(type, value);
		return value;
	}

	private void ValidateOverride(Type type)
	{
		if (!_003Cmethod_003EP.TryGetOverride(type, out MethodInfo _))
		{
			throw new InvalidOperationException($"{type.FullName} does not override {_003Cmethod_003EP.BaseMethod.DeclaringType?.FullName}.{_003Cmethod_003EP.Name}.");
		}
	}
}
