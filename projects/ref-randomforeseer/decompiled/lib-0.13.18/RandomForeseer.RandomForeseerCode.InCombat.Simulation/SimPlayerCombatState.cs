using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal sealed class SimPlayerCombatState
{
	[CompilerGenerated]
	private PlayerCombatState _003CliveState_003EP;

	[CompilerGenerated]
	private SimOrbQueue _003COrbQueue_003Ek__BackingField;

	[CompilerGenerated]
	private SimCardPile _003CHand_003Ek__BackingField;

	[CompilerGenerated]
	private SimCardPile _003CDrawPile_003Ek__BackingField;

	[CompilerGenerated]
	private SimCardPile _003CDiscardPile_003Ek__BackingField;

	[CompilerGenerated]
	private SimCardPile _003CExhaustPile_003Ek__BackingField;

	[CompilerGenerated]
	private SimCardPile _003CPlayPile_003Ek__BackingField;

	public SimOrbQueue OrbQueue => _003COrbQueue_003Ek__BackingField ?? (_003COrbQueue_003Ek__BackingField = new SimOrbQueue(_003CliveState_003EP.OrbQueue));

	public SimCardPile Hand => _003CHand_003Ek__BackingField ?? (_003CHand_003Ek__BackingField = new SimCardPile(_003CliveState_003EP.Hand));

	public SimCardPile DrawPile => _003CDrawPile_003Ek__BackingField ?? (_003CDrawPile_003Ek__BackingField = new SimCardPile(_003CliveState_003EP.DrawPile));

	public SimCardPile DiscardPile => _003CDiscardPile_003Ek__BackingField ?? (_003CDiscardPile_003Ek__BackingField = new SimCardPile(_003CliveState_003EP.DiscardPile));

	public SimCardPile ExhaustPile => _003CExhaustPile_003Ek__BackingField ?? (_003CExhaustPile_003Ek__BackingField = new SimCardPile(_003CliveState_003EP.ExhaustPile));

	public SimCardPile PlayPile => _003CPlayPile_003Ek__BackingField ?? (_003CPlayPile_003Ek__BackingField = new SimCardPile(_003CliveState_003EP.PlayPile));

	public IReadOnlyList<SimCardPile> AllPiles => new _003C_003Ez__ReadOnlyArray<SimCardPile>(new SimCardPile[5] { Hand, DrawPile, DiscardPile, ExhaustPile, PlayPile });

	public IEnumerable<PredictedCard> AllCards => AllPiles.SelectMany((SimCardPile pile) => pile.Cards);

	public int Energy { get; private set; }

	public int Stars { get; private set; }

	public SimPlayerCombatState(PlayerCombatState liveState)
	{
		_003CliveState_003EP = liveState;
		Energy = _003CliveState_003EP.Energy;
		Stars = _003CliveState_003EP.Stars;
		base._002Ector();
	}

	public PredictedCard? FindCard(CardModel card)
	{
		CardModel card2 = card;
		return AllCards.FirstOrDefault((PredictedCard predicted) => predicted.References(card2));
	}

	public SimCardPile? GetCardPile(PileType type)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected I4, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		return (int)type switch
		{
			0 => null, 
			1 => DrawPile, 
			2 => Hand, 
			3 => DiscardPile, 
			4 => ExhaustPile, 
			5 => PlayPile, 
			6 => throw new ArgumentOutOfRangeException("type", type, "Deck is not a combat pile."), 
			_ => throw new ArgumentOutOfRangeException("type", type, $"Unknown pile type: {type}."), 
		};
	}

	public void GainEnergy(decimal amount)
	{
		Energy = (int)Math.Clamp((decimal)Energy + amount, 0m, 999999999m);
	}

	public void LoseEnergy(decimal amount)
	{
		Energy = (int)Math.Clamp((decimal)Energy - amount, 0m, 999999999m);
	}

	public void GainStars(decimal amount)
	{
		Stars = (int)Math.Clamp((decimal)Stars + amount, 0m, 999999999m);
	}

	public void LoseStars(decimal amount)
	{
		Stars = (int)Math.Clamp((decimal)Stars - amount, 0m, 999999999m);
	}

	public void EndOfTurnCleanup()
	{
		foreach (PredictedCard allCard in AllCards)
		{
			allCard.MutablePreview.EndOfTurnCleanup();
		}
	}
}
