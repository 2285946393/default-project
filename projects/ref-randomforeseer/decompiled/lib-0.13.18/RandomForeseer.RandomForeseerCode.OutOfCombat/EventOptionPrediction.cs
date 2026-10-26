using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.OutOfCombat.Events;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class EventOptionPrediction
{
	private static readonly EventPredictionRegistry Registry = CreateRegistry();

	private static EventPredictionRegistry CreateRegistry()
	{
		EventPredictionRegistry eventPredictionRegistry = new EventPredictionRegistry();
		eventPredictionRegistry.Register<AromaOfChaos>((Func<AromaOfChaos, EventOption, IReadOnlyList<IHoverTip>>)AromaOfChaosPrediction.GetHoverTips);
		eventPredictionRegistry.Register<BattlewornDummy>((Func<BattlewornDummy, EventOption, IReadOnlyList<IHoverTip>>)BattlewornDummyPrediction.GetHoverTips);
		eventPredictionRegistry.Register<BrainLeech>((Func<BrainLeech, EventOption, IReadOnlyList<IHoverTip>>)BrainLeechPrediction.GetHoverTips);
		eventPredictionRegistry.Register<ColorfulPhilosophers>((Func<ColorfulPhilosophers, EventOption, IReadOnlyList<IHoverTip>>)ColorfulPhilosophersPrediction.GetHoverTips);
		eventPredictionRegistry.Register<DenseVegetation>((Func<DenseVegetation, EventOption, IReadOnlyList<IHoverTip>>)DenseVegetationPrediction.GetHoverTips);
		eventPredictionRegistry.Register<DollRoom>((Func<DollRoom, EventOption, IReadOnlyList<IHoverTip>>)DollRoomPrediction.GetHoverTips);
		eventPredictionRegistry.Register<DoorsOfLightAndDark>((Func<DoorsOfLightAndDark, EventOption, IReadOnlyList<IHoverTip>>)DoorsOfLightAndDarkPrediction.GetHoverTips);
		eventPredictionRegistry.Register<EndlessConveyor>((Func<EndlessConveyor, EventOption, IReadOnlyList<IHoverTip>>)EndlessConveyorPrediction.GetHoverTips);
		eventPredictionRegistry.Register<InfestedAutomaton>((Func<InfestedAutomaton, EventOption, IReadOnlyList<IHoverTip>>)InfestedAutomatonPrediction.GetHoverTips);
		eventPredictionRegistry.Register<LuminousChoir>((Func<LuminousChoir, EventOption, IReadOnlyList<IHoverTip>>)LuminousChoirPrediction.GetHoverTips);
		eventPredictionRegistry.Register<MorphicGrove>((Func<MorphicGrove, EventOption, IReadOnlyList<IHoverTip>>)MorphicGrovePrediction.GetHoverTips);
		eventPredictionRegistry.Register<PotionCourier>((Func<PotionCourier, EventOption, IReadOnlyList<IHoverTip>>)PotionCourierPrediction.GetHoverTips);
		eventPredictionRegistry.Register<PunchOff>((Func<PunchOff, EventOption, IReadOnlyList<IHoverTip>>)PunchOffPrediction.GetHoverTips);
		eventPredictionRegistry.Register<RanwidTheElder>((Func<RanwidTheElder, EventOption, IReadOnlyList<IHoverTip>>)RanwidTheElderPrediction.GetHoverTips);
		eventPredictionRegistry.Register<Reflections>((Func<Reflections, EventOption, IReadOnlyList<IHoverTip>>)ReflectionsPrediction.GetHoverTips);
		eventPredictionRegistry.Register<RoomFullOfCheese>((Func<RoomFullOfCheese, EventOption, IReadOnlyList<IHoverTip>>)RoomFullOfCheesePrediction.GetHoverTips);
		eventPredictionRegistry.Register<RoundTeaParty>((Func<RoundTeaParty, EventOption, IReadOnlyList<IHoverTip>>)RoundTeaPartyPrediction.GetHoverTips);
		eventPredictionRegistry.Register<SlipperyBridge>((Func<SlipperyBridge, EventOption, IReadOnlyList<IHoverTip>>)SlipperyBridgePrediction.GetHoverTips);
		eventPredictionRegistry.Register<Symbiote>((Func<Symbiote, EventOption, IReadOnlyList<IHoverTip>>)SymbiotePrediction.GetHoverTips);
		eventPredictionRegistry.Register<TabletOfTruth>((Func<TabletOfTruth, EventOption, IReadOnlyList<IHoverTip>>)TabletOfTruthPrediction.GetHoverTips);
		eventPredictionRegistry.Register<TheFutureOfPotions>((Func<TheFutureOfPotions, EventOption, IReadOnlyList<IHoverTip>>)TheFutureOfPotionsPrediction.GetHoverTips);
		eventPredictionRegistry.Register<TheLegendsWereTrue>((Func<TheLegendsWereTrue, EventOption, IReadOnlyList<IHoverTip>>)TheLegendsWereTruePrediction.GetHoverTips);
		eventPredictionRegistry.Register<ThisOrThat>((Func<ThisOrThat, EventOption, IReadOnlyList<IHoverTip>>)ThisOrThatPrediction.GetHoverTips);
		eventPredictionRegistry.Register<TinkerTime>((Func<TinkerTime, EventOption, IReadOnlyList<IHoverTip>>)TinkerTimePrediction.GetHoverTips);
		eventPredictionRegistry.Register<TrashHeap>((Func<TrashHeap, EventOption, IReadOnlyList<IHoverTip>>)TrashHeapPrediction.GetHoverTips);
		eventPredictionRegistry.Register<Trial>((Func<Trial, EventOption, IReadOnlyList<IHoverTip>>)TrialPrediction.GetHoverTips);
		eventPredictionRegistry.Register<UnrestSite>((Func<UnrestSite, EventOption, IReadOnlyList<IHoverTip>>)UnrestSitePrediction.GetHoverTips);
		eventPredictionRegistry.Register<WarHistorianRepy>((Func<WarHistorianRepy, EventOption, IReadOnlyList<IHoverTip>>)WarHistorianRepyPrediction.GetHoverTips);
		eventPredictionRegistry.Register<WelcomeToWongos>((Func<WelcomeToWongos, EventOption, IReadOnlyList<IHoverTip>>)WelcomeToWongosPrediction.GetHoverTips);
		eventPredictionRegistry.Register<Wellspring>((Func<Wellspring, EventOption, IReadOnlyList<IHoverTip>>)WellspringPrediction.GetHoverTips);
		eventPredictionRegistry.Register<WhisperingHollow>((Func<WhisperingHollow, EventOption, IReadOnlyList<IHoverTip>>)WhisperingHollowPrediction.GetHoverTips);
		return eventPredictionRegistry;
	}

	public static IReadOnlyList<IHoverTip> GetHoverTips(EventOption option)
	{
		if (EventOptionEventModelMap.TryGetEvent(option, out EventModel eventModel))
		{
			Player owner = eventModel.Owner;
			if (owner != null && !option.IsLocked)
			{
				List<IHoverTip> list = new List<IHoverTip>();
				RelicModel relicForOption = GetRelicForOption(eventModel, option);
				if (relicForOption != null)
				{
					list.AddRange(RelicPickupPrediction.GetHoverTips(owner, relicForOption));
				}
				ModSettings settings = ModData.Settings;
				if (ModSettingsExtensions.get_IsPredictionEnabled(settings) && settings.EventOptionPredictionEnabled && Registry.TryPredict(eventModel, option, out IReadOnlyList<IHoverTip> hoverTips))
				{
					list.AddRange(hoverTips);
				}
				return list;
			}
		}
		Entry.Logger.Warn("Event option prediction skipped for " + option.TextKey + ": no event model or owner found", 1);
		return Array.Empty<IHoverTip>();
	}

	private static RelicModel? GetRelicForOption(EventModel eventModel, EventOption option)
	{
		if (option.Relic != null)
		{
			return option.Relic;
		}
		RelicTrader val = (RelicTrader)(object)((eventModel is RelicTrader) ? eventModel : null);
		if (val != null)
		{
			return RelicTraderPrediction.GetReceivedRelic(val, option);
		}
		return null;
	}
}
