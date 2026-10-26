using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal static class FrozenEyeDrawPileView
{
	public static bool TryRefresh(NCardPileScreen screen)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Invalid comparison between Unknown and I4
		ModSettings settings = ModData.Settings;
		if (ModSettingsExtensions.get_IsPredictionEnabled(settings) && settings.FrozenEyeEnabled && screen != null)
		{
			CardPile pile = screen.Pile;
			if (pile != null && (int)pile.Type == 1)
			{
				NCardGrid grid = screen._grid;
				if (grid != null)
				{
					IReadOnlyList<CardModel> readOnlyList = screen.Pile.Cards;
					IReadOnlyList<CardModel> shufflePrediction = GetShufflePrediction(screen);
					int num;
					if (shufflePrediction != null && shufflePrediction.Count > 0)
					{
						FrozenEyeDrawPileViewState.SetPredictedShuffleCards(grid, shufflePrediction);
						IReadOnlyList<CardModel> readOnlyList2 = readOnlyList;
						IReadOnlyList<CardModel> readOnlyList3 = shufflePrediction;
						num = 0;
						CardModel[] array = (CardModel[])(object)new CardModel[readOnlyList2.Count + readOnlyList3.Count];
						foreach (CardModel item in readOnlyList2)
						{
							array[num] = item;
							num++;
						}
						foreach (CardModel item2 in readOnlyList3)
						{
							array[num] = item2;
							num++;
						}
						readOnlyList = new _003C_003Ez__ReadOnlyArray<CardModel>(array);
					}
					else
					{
						FrozenEyeDrawPileViewState.SetPredictedShuffleCards(grid, Array.Empty<CardModel>());
					}
					IReadOnlyList<CardModel> readOnlyList4 = readOnlyList;
					num = 1;
					List<SortingOrders> list = new List<SortingOrders>(num);
					CollectionsMarshal.SetCount(list, num);
					CollectionsMarshal.AsSpan(list)[0] = (SortingOrders)8;
					grid.SetCards(readOnlyList4, (PileType)1, list, (Task)null);
					return true;
				}
			}
		}
		return false;
	}

	private static IReadOnlyList<CardModel> GetShufflePrediction(NCardPileScreen screen)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		if (ModData.Settings.ShufflePredictionEnabled && CardPileUtils.TryGetDrawPileOwner(screen.Pile, out Player player))
		{
			ICombatState combatState = player.Creature.CombatState;
			if (combatState != null && combatState.CurrentSide == player.Creature.Side)
			{
				try
				{
					using CombatPredictionSession combatPredictionSession = new CombatPredictionSession(combatState);
					CombatPredictionSimulator simulator = combatPredictionSession.Simulator;
					SimPlayerCombatState playerCombatState = simulator.State.GetPlayerCombatState(player);
					if (playerCombatState.DiscardPile.IsEmpty)
					{
						return Array.Empty<CardModel>();
					}
					playerCombatState.DrawPile.Clear();
					simulator.Shuffle(player);
					List<CardModel> list = new List<CardModel>();
					list.AddRange(playerCombatState.DrawPile.Cards.SelectPreviews());
					return new _003C_003Ez__ReadOnlyList<CardModel>(list);
				}
				catch (Exception ex)
				{
					Entry.Logger.Warn($"Failed to predict draw pile shuffle for {player.Creature.Name}: {ex}", 1);
					ModTelemetry.CaptureException(ex, "frozen_eye_prediction", "predict_draw_pile_shuffle");
					return Array.Empty<CardModel>();
				}
			}
		}
		return Array.Empty<CardModel>();
	}
}
