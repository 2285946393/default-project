using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common.Mirrors;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.TurnEnd;

internal static class AfterFlushMirrors
{
	private static readonly MirrorMethodSpec AfterFlush = MirrorMethodSpec.Hook("AfterFlush", new Type[4]
	{
		typeof(PlayerChoiceContext),
		typeof(Player),
		typeof(IReadOnlyCollection<CardModel>),
		typeof(IReadOnlyCollection<CardModel>)
	});

	private static readonly MethodMirrorRegistry<AbstractModel, AfterFlushMirrorContext> Registry = new MethodMirrorRegistry<AbstractModel, AfterFlushMirrorContext>(AfterFlush);

	public static void Invoke(AbstractModel listener, AfterFlushMirrorContext context)
	{
		Registry.Invoke(listener, context);
	}
}
