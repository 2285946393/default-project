using System;
using System.Reflection;

namespace RandomForeseer.RandomForeseerCode.Common.Mirrors;

internal delegate Action<TBase, TContext>? MethodMirrorInferrer<in TBase, in TContext>(Type runtimeType, MethodInfo overrideMethod) where TBase : class where TContext : IMethodMirrorContext<TBase>;
