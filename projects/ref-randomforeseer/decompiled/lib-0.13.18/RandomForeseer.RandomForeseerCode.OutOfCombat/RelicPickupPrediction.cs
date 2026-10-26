using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.Common.HoverTips;
using RandomForeseer.RandomForeseerCode.Data;
using RandomForeseer.RandomForeseerCode.OutOfCombat.Mirrors;
using RandomForeseer.RandomForeseerCode.Telemetry;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal static class RelicPickupPrediction
{
	private static readonly HashSet<Type> WarnedRelicTypes = new HashSet<Type>();

	public static IReadOnlyList<IHoverTip> GetHoverTips(Player player, RelicModel relic)
	{
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		Player player2 = player;
		if (!IsPredictionEnabled(relic))
		{
			return Array.Empty<IHoverTip>();
		}
		try
		{
			RunPredictionContext context = new RunPredictionContext(player2);
			IReadOnlyList<IHoverTip> result;
			if (!(relic is ArcaneScroll))
			{
				if (!(relic is HeftyTablet))
				{
					if (!(relic is Kaleidoscope))
					{
						if (!(relic is LargeCapsule))
						{
							if (!(relic is LeadPaperweight))
							{
								if (!(relic is LeafyPoultice))
								{
									if (!(relic is LostCoffer))
									{
										if (!(relic is MassiveScroll))
										{
											if (!(relic is NeowsBones))
											{
												if (!(relic is NewLeaf))
												{
													if (!(relic is PhialHolster))
													{
														if (!(relic is ScrollBoxes))
														{
															SilkenTress val = (SilkenTress)(object)((relic is SilkenTress) ? relic : null);
															if (val == null)
															{
																if (!(relic is SmallCapsule))
																{
																	if (!(relic is Astrolabe))
																	{
																		if (!(relic is CallingBell))
																		{
																			if (!(relic is PandorasBox))
																			{
																				BeautifulBracelet val2 = (BeautifulBracelet)(object)((relic is BeautifulBracelet) ? relic : null);
																				if (val2 == null)
																				{
																					if (!(relic is AlchemicalCoffer))
																					{
																						if (!(relic is GlassEye))
																						{
																							if (!(relic is SandCastle))
																							{
																								SeaGlass val3 = (SeaGlass)(object)((relic is SeaGlass) ? relic : null);
																								if (val3 == null)
																								{
																									if (!(relic is ToyBox))
																									{
																										if (!(relic is DistinguishedCape))
																										{
																											if (!(relic is Cauldron))
																											{
																												if (!(relic is FragrantMushroom))
																												{
																													if (!(relic is Orrery))
																													{
																														if (!(relic is WarPaint))
																														{
																															if (!(relic is Whetstone))
																															{
																																goto IL_07c2;
																															}
																															List<IHoverTip> list = new List<IHoverTip>();
																															list.AddRange(OutOfCombatPredictionUtils.PredictUpgradedDeckCards(player2, ((DynamicVar)relic.DynamicVars.Cards).IntValue, (CardModel card) => (int)card.Type == 1 && card.IsUpgradable).ToPredictionHoverTips());
																															result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
																														}
																														else
																														{
																															List<IHoverTip> list2 = new List<IHoverTip>();
																															list2.AddRange(OutOfCombatPredictionUtils.PredictUpgradedDeckCards(player2, ((DynamicVar)relic.DynamicVars.Cards).IntValue, (CardModel card) => (int)card.Type == 2 && card.IsUpgradable).ToPredictionHoverTips());
																															result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list2);
																														}
																													}
																													else
																													{
																														List<IHoverTip> list3 = new List<IHoverTip>();
																														list3.AddRange(OutOfCombatPredictionUtils.PredictCardRewardBundles(player2, ((DynamicVar)relic.DynamicVars.Cards).IntValue, 3, () => OutOfCombatPredictionUtils.CreateCharacterCardRewardOptions(player2)).ToPredictionCardBundleHoverTips());
																														result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list3);
																													}
																												}
																												else
																												{
																													List<IHoverTip> list4 = new List<IHoverTip>();
																													list4.AddRange(OutOfCombatPredictionUtils.PredictUpgradedDeckCards(player2, ((DynamicVar)relic.DynamicVars.Cards).IntValue, (CardModel card) => card.IsUpgradable).ToPredictionHoverTips());
																													result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list4);
																												}
																											}
																											else
																											{
																												List<IHoverTip> list5 = new List<IHoverTip>();
																												list5.AddRange(PredictionUtils.PredictPotionRewards(player2, relic.DynamicVars["Potions"].IntValue, player2.PlayerRng.Rewards.Clone()).ToPredictionHoverTips());
																												result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list5);
																											}
																										}
																										else
																										{
																											if (!IsSingleplayerUnfairPredictionAllowed())
																											{
																												goto IL_07c2;
																											}
																											List<IHoverTip> list6 = new List<IHoverTip>();
																											list6.AddRange(PredictCurses(context, relic.DynamicVars["Curses"].IntValue).ToPredictionHoverTips());
																											result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list6);
																										}
																									}
																									else
																									{
																										List<IHoverTip> list7 = new List<IHoverTip>();
																										list7.AddRange(PredictToyBoxRelics(context, relic.DynamicVars["Relics"].IntValue).ToPredictionHoverTips());
																										result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list7);
																									}
																								}
																								else
																								{
																									List<IHoverTip> list8 = new List<IHoverTip>();
																									list8.AddRange(PredictSeaGlassBundles(context, val3).ToPredictionCardBundleHoverTips());
																									result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list8);
																								}
																							}
																							else
																							{
																								if (!IsSingleplayerUnfairPredictionAllowed())
																								{
																									goto IL_07c2;
																								}
																								List<IHoverTip> list9 = new List<IHoverTip>();
																								list9.AddRange(PredictSandCastle(player2, ((DynamicVar)relic.DynamicVars.Cards).IntValue).ToPredictionHoverTips());
																								result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list9);
																							}
																						}
																						else
																						{
																							List<IHoverTip> list10 = new List<IHoverTip>();
																							list10.AddRange(PredictGlassEyeBundles(context).ToPredictionCardBundleHoverTips());
																							result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list10);
																						}
																					}
																					else
																					{
																						if (!IsSingleplayerUnfairPredictionAllowed())
																						{
																							goto IL_07c2;
																						}
																						List<IHoverTip> list11 = new List<IHoverTip>();
																						list11.AddRange(PotionFactory.CreateRandomPotionsOutOfCombat(player2, relic.DynamicVars["PotionSlots"].IntValue, player2.RunState.Rng.CombatPotionGeneration.Clone(), (IEnumerable<PotionModel>)null).ToPredictionHoverTips());
																						result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list11);
																					}
																				}
																				else
																				{
																					if (!IsSingleplayerUnfairPredictionAllowed())
																					{
																						goto IL_07c2;
																					}
																					List<IHoverTip> list12 = new List<IHoverTip>();
																					list12.AddRange(PredictBeautifulBracelet(context, val2).ToPredictionHoverTips());
																					result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list12);
																				}
																			}
																			else
																			{
																				if (!IsSingleplayerUnfairPredictionAllowed())
																				{
																					goto IL_07c2;
																				}
																				List<IHoverTip> list13 = new List<IHoverTip>();
																				list13.AddRange(PredictPandorasBox(player2).ToPredictionHoverTips());
																				result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list13);
																			}
																		}
																		else
																		{
																			List<IHoverTip> list14 = new List<IHoverTip>();
																			Player player3 = player2;
																			RelicRarity[] array = new RelicRarity[3];
																			RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
																			list14.AddRange(OutOfCombatPredictionUtils.PredictRelicRewards(player3, new _003C_003Ez__ReadOnlyArray<RelicRarity>((RelicRarity[])(object)array)).ToPredictionHoverTips());
																			result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list14);
																		}
																	}
																	else
																	{
																		if (!IsSingleplayerUnfairPredictionAllowed())
																		{
																			goto IL_07c2;
																		}
																		List<IHoverTip> list15 = new List<IHoverTip>();
																		list15.AddRange(PredictAstrolabeBundles(player2).ToPredictionCardBundleHoverTips(PredictionCardBundleKind.Transform));
																		result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list15);
																	}
																}
																else
																{
																	List<IHoverTip> list16 = new List<IHoverTip>();
																	list16.AddRange(OutOfCombatPredictionUtils.PredictRelicRewards(context, 1).ToPredictionHoverTips());
																	result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list16);
																}
															}
															else
															{
																if (!IsAllModesUnfairPredictionAllowed())
																{
																	goto IL_07c2;
																}
																IReadOnlyList<IHoverTip> readOnlyList;
																if (!RewardPagePredictionContext.HasOtherPendingReward((RelicModel)(object)val))
																{
																	readOnlyList = PredictSilkenTressRewardTips(context, val);
																}
																else
																{
																	IReadOnlyList<IHoverTip> readOnlyList2 = new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>((IHoverTip)(object)PredictionHoverTipFactory.Text("silken_tress_reward_offset"));
																	readOnlyList = readOnlyList2;
																}
																result = readOnlyList;
															}
														}
														else
														{
															List<IHoverTip> list17 = new List<IHoverTip>();
															list17.AddRange(PredictScrollBoxes(context).ToPredictionCardBundleHoverTips(PredictionCardBundleKind.ScrollBoxes));
															result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list17);
														}
													}
													else
													{
														if (!IsSingleplayerUnfairPredictionAllowed(relic))
														{
															goto IL_07c2;
														}
														List<IHoverTip> list18 = new List<IHoverTip>();
														list18.AddRange(PotionFactory.CreateRandomPotionsOutOfCombat(player2, relic.DynamicVars["Potions"].IntValue, player2.RunState.Rng.CombatPotionGeneration.Clone(), (IEnumerable<PotionModel>)null).ToPredictionHoverTips());
														result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list18);
													}
												}
												else
												{
													if (!IsSingleplayerUnfairPredictionAllowed(relic))
													{
														goto IL_07c2;
													}
													List<IHoverTip> list19 = new List<IHoverTip>();
													list19.AddRange(PredictNewLeaf(player2).ToPredictionHoverTips());
													result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list19);
												}
											}
											else
											{
												result = PredictNeowsBonesTips(context, relic);
											}
										}
										else
										{
											List<IHoverTip> list20 = new List<IHoverTip>();
											list20.AddRange(PredictMultiplayerCards(context, 3).ToPredictionHoverTips());
											result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list20);
										}
									}
									else
									{
										result = PredictLostCofferTips(context);
									}
								}
								else
								{
									if (!IsSingleplayerUnfairPredictionAllowed(relic))
									{
										goto IL_07c2;
									}
									List<IHoverTip> list21 = new List<IHoverTip>();
									list21.AddRange(PredictLeafyPoultice(player2).ToPredictionHoverTips());
									result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list21);
								}
							}
							else
							{
								List<IHoverTip> list22 = new List<IHoverTip>();
								list22.AddRange(PredictColorlessCards(context, 2).ToPredictionHoverTips());
								result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list22);
							}
						}
						else
						{
							if (!IsSingleplayerUnfairPredictionAllowed(relic))
							{
								goto IL_07c2;
							}
							List<IHoverTip> list23 = new List<IHoverTip>();
							list23.AddRange(OutOfCombatPredictionUtils.PredictRelicRewards(context, relic.DynamicVars["Relics"].IntValue).ToPredictionHoverTips());
							result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list23);
						}
					}
					else
					{
						List<IHoverTip> list24 = new List<IHoverTip>();
						list24.AddRange(PredictKaleidoscopeBundles(context).ToPredictionCardBundleHoverTips());
						result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list24);
					}
				}
				else
				{
					List<IHoverTip> list25 = new List<IHoverTip>();
					list25.AddRange(PredictRareCharacterCards(context, 3).ToPredictionHoverTips());
					result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list25);
				}
			}
			else
			{
				if (!IsSingleplayerUnfairPredictionAllowed(relic))
				{
					goto IL_07c2;
				}
				List<IHoverTip> list26 = new List<IHoverTip>();
				list26.AddRange(PredictRareCharacterCards(context, 1).ToPredictionHoverTips());
				result = new _003C_003Ez__ReadOnlyList<IHoverTip>(list26);
			}
			goto IL_07c9;
			IL_07c2:
			result = Array.Empty<IHoverTip>();
			goto IL_07c9;
			IL_07c9:
			return result;
		}
		catch (Exception ex)
		{
			WarnOnce(((object)relic).GetType(), $"Could not predict relic pickup effect for {((AbstractModel)relic).Id}: {ex}");
			ModTelemetry.CaptureException(ex, "relic_pickup_prediction", "build_hover_tips", TelemetryContext.ForModel((AbstractModel)(object)relic));
			return Array.Empty<IHoverTip>();
		}
	}

	private static bool IsPredictionEnabled(RelicModel relic)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Invalid comparison between Unknown and I4
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		ModSettings settings = ModData.Settings;
		if (!ModSettingsExtensions.get_IsPredictionEnabled(settings))
		{
			return false;
		}
		RelicRarity rarity = relic.Rarity;
		if ((int)rarity != 6)
		{
			if ((int)rarity == 7)
			{
				return settings.AncientRelicPickupPredictionEnabled;
			}
			return settings.RelicPickupPredictionEnabled;
		}
		return settings.EventOptionPredictionEnabled;
	}

	private static IReadOnlyList<CardModel> PredictRareCharacterCards(RunPredictionContext context, int count)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		CardCreationOptions options = new CardCreationOptions((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(context.Player.Character.CardPool), (CardCreationSource)3, (CardRarityOddsType)5, (Func<CardModel, bool>)((CardModel card) => (int)card.Rarity == 4)).WithFlags((CardCreationFlags)2);
		return CardRewardPrediction.PredictCards(context, count, options);
	}

	private static IReadOnlyList<CardModel> PredictColorlessCards(RunPredictionContext context, int count)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		CardCreationOptions options = new CardCreationOptions((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>((CardPoolModel)(object)ModelDb.CardPool<ColorlessCardPool>()), (CardCreationSource)3, (CardRarityOddsType)1, (Func<CardModel, bool>)null);
		return CardRewardPrediction.PredictCards(context, count, options);
	}

	private static IReadOnlyList<CardModel> PredictMultiplayerCards(RunPredictionContext context, int count)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		CardCreationOptions options = new CardCreationOptions((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlyArray<CardPoolModel>((CardPoolModel[])(object)new CardPoolModel[2]
		{
			context.Player.Character.CardPool,
			(CardPoolModel)ModelDb.CardPool<ColorlessCardPool>()
		}), (CardCreationSource)3, (CardRarityOddsType)1, (Func<CardModel, bool>)((CardModel card) => (int)card.MultiplayerConstraint == 1));
		return CardRewardPrediction.PredictCards(context, count, options);
	}

	private static IReadOnlyList<IReadOnlyList<CardModel>> PredictKaleidoscopeBundles(RunPredictionContext context)
	{
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		RunPredictionContext context2 = context;
		List<IReadOnlyList<CardModel>> list = new List<IReadOnlyList<CardModel>>();
		for (int i = 0; i < 2; i++)
		{
			List<CardModel> list2 = new List<CardModel>();
			foreach (CardPoolModel item in ListExtensions.StableShuffle<CardPoolModel>(context2.Player.UnlockState.CharacterCardPools.Where((CardPoolModel pool) => pool != context2.Player.Character.CardPool).ToList(), context2.SharedRng.Niche).Take(3))
			{
				CardCreationOptions options = new CardCreationOptions((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(item), (CardCreationSource)3, (CardRarityOddsType)1, (Func<CardModel, bool>)null).WithFlags((CardCreationFlags)16);
				list2.AddRange(CardRewardPrediction.PredictCards(context2, 1, options));
			}
			CardCreationOptions options2 = new CardCreationOptions((IEnumerable<CardPoolModel>)Array.Empty<CardPoolModel>(), (CardCreationSource)3, (CardRarityOddsType)5, (Func<CardModel, bool>)null).WithFlags((CardCreationFlags)176);
			list.Add(CardRewardPrediction.ApplyRewardModifiersToExistingCards(context2, list2, options2));
		}
		return list;
	}

	private static IReadOnlyList<IReadOnlyList<CardModel>> PredictAstrolabeBundles(Player player)
	{
		IReadOnlyList<IReadOnlyList<CardModel>> source = OutOfCombatPredictionUtils.PredictDistinctDeckTransformResultBundles(player, player.RunState.Rng.Niche, 3);
		List<IReadOnlyList<CardModel>> list = new List<IReadOnlyList<CardModel>>();
		list.AddRange(source.Select((IReadOnlyList<CardModel> cards) => cards.Select(PredictionUtils.CreateUpgradedCard).ToArray()));
		return new _003C_003Ez__ReadOnlyList<IReadOnlyList<CardModel>>(list);
	}

	private static IReadOnlyList<IReadOnlyList<CardModel>> PredictGlassEyeBundles(RunPredictionContext context)
	{
		RunPredictionContext context2 = context;
		CardRarity[] array = new CardRarity[5];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		return ((IEnumerable<CardRarity>)(object)array).Select(delegate(CardRarity rarity)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			CardCreationOptions options = CardCreationOptions.ForNonCombatWithUniformOdds((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(context2.Player.Character.CardPool), (Func<CardModel, bool>)((CardModel card) => card.Rarity == rarity)).WithFlags((CardCreationFlags)129);
			return CardRewardPrediction.PredictCards(context2, 3, options);
		}).ToList();
	}

	private static IReadOnlyList<IReadOnlyList<CardModel>> PredictSeaGlassBundles(RunPredictionContext context, SeaGlass seaGlass)
	{
		RunPredictionContext context2 = context;
		if (seaGlass.CharacterId == (ModelId)null)
		{
			return Array.Empty<IReadOnlyList<CardModel>>();
		}
		CharacterModel character = ModelDb.GetById<CharacterModel>(seaGlass.CharacterId);
		int cardCount = ((DynamicVar)((RelicModel)seaGlass).DynamicVars.Cards).IntValue / 3;
		CardRarity[] array = new CardRarity[3];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		return ((IEnumerable<CardRarity>)(object)array).Select(delegate(CardRarity rarity)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			CardCreationOptions options = CardCreationOptions.ForNonCombatWithUniformOdds((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(character.CardPool), (Func<CardModel, bool>)((CardModel card) => card.Rarity == rarity)).WithFlags((CardCreationFlags)17);
			return CardRewardPrediction.PredictCards(context2, cardCount, options);
		}).ToList();
	}

	private static IReadOnlyList<IHoverTip> PredictLostCofferTips(RunPredictionContext context)
	{
		CardCreationOptions options = OutOfCombatPredictionUtils.CreateCharacterCardRewardOptions(context.Player);
		List<IHoverTip> list = CardRewardPrediction.PredictCards(context, 3, options).ToPredictionHoverTips().ToList();
		PotionModel potion = PotionFactory.CreateRandomPotionOutOfCombat(context.Player, context.Rng.Rewards, (IEnumerable<PotionModel>)null);
		list.Add(PredictionHoverTipFactory.Potion(potion));
		return list;
	}

	private static IReadOnlyList<IHoverTip> PredictNeowsBonesTips(RunPredictionContext context, RelicModel relic)
	{
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		RunPredictionContext context2 = context;
		RelicModel relic2 = relic;
		List<RelicModel> list = (from relic in ((AncientEventModel)ModelDb.Event<Neow>()).AllPossibleOptions.Select((EventOption option) => option.Relic).OfType<RelicModel>()
			where relic.IsAllowedAtNeow(context2.Player) && !(relic is NeowsBones)
			select relic).ToList();
		context2.Rng.Rewards.Shuffle<RelicModel>((IList<RelicModel>)list);
		List<RelicModel> list2 = list.Take(relic2.DynamicVars["Relics"].IntValue).ToList();
		List<IHoverTip> list3 = list2.ToPredictionHoverTips().ToList();
		if (IsSingleplayerUnfairPredictionAllowed() && list2 != null && list2.Count == 2)
		{
			RelicModel firstRelic = list2[0];
			RelicModel secondRelic = list2[1];
			int curseCount = relic2.DynamicVars["Curses"].IntValue;
			RunPredictionContext context3 = context2.Clone();
			FastForwardRelicPickup(context2, firstRelic);
			FastForwardRelicPickup(context2, secondRelic);
			IReadOnlyList<CardModel> firstCurses = PredictCurses(context2, curseCount);
			FastForwardRelicPickup(context3, secondRelic);
			FastForwardRelicPickup(context3, firstRelic);
			IReadOnlyList<CardModel> secondCurses = PredictCurses(context3, curseCount);
			if (firstCurses.SequenceEqual(secondCurses))
			{
				list3.AddRange(firstCurses.ToPredictionHoverTips());
			}
			else
			{
				list3.Add((IHoverTip)(object)PredictionHoverTipFactory.Text("neows_bones_pickup_order", delegate(LocString description)
				{
					description.Add("FirstRelic", firstRelic.Title.GetFormattedText());
					description.Add("SecondRelic", secondRelic.Title.GetFormattedText());
					description.Add("NeowsBones", relic2.Title.GetFormattedText());
					description.Add("Curses", (decimal)curseCount);
					description.Add("FirstCurses", (IList<string>)firstCurses.Select((CardModel card) => card.Title).ToList());
					description.Add("SecondCurses", (IList<string>)secondCurses.Select((CardModel card) => card.Title).ToList());
				}));
			}
		}
		return list3;
	}

	private static IReadOnlyList<CardModel> PredictCurses(RunPredictionContext context, int count)
	{
		List<CardModel> list = (from card in context.Player.GetUnlockedCurseCards()
			where card.CanBeGeneratedByModifiers
			orderby ((AbstractModel)card).Id
			select card).ToList();
		List<CardModel> list2 = new List<CardModel>(count);
		for (int i = 0; i < count; i++)
		{
			CardModel val = context.SharedRng.Niche.NextItem<CardModel>((IEnumerable<CardModel>)list);
			if (val == null)
			{
				break;
			}
			list.Remove(val);
			list2.Add(val);
		}
		return list2;
	}

	internal static void FastForwardRelicPickup(RunPredictionContext context, RelicModel relic)
	{
		if (!(relic is ArcaneScroll))
		{
			if (!(relic is HeftyTablet))
			{
				if (!(relic is Kaleidoscope))
				{
					if (!(relic is LeadPaperweight))
					{
						if (!(relic is LostCoffer))
						{
							if (!(relic is MassiveScroll))
							{
								if (!(relic is NewLeaf))
								{
									if (!(relic is ScrollBoxes))
									{
										if (!(relic is LargeCapsule))
										{
											if (!(relic is SmallCapsule))
											{
												if (!(relic is WarPaint))
												{
													if (!(relic is Whetstone))
													{
														if (!(relic is Cauldron))
														{
															if (relic is Orrery)
															{
																for (int i = 0; i < ((DynamicVar)relic.DynamicVars.Cards).IntValue; i++)
																{
																	CardCreationOptions options = OutOfCombatPredictionUtils.CreateCharacterCardRewardOptions(context.Player);
																	CardRewardPrediction.PredictCards(context, 3, options);
																}
															}
														}
														else
														{
															PredictionUtils.PredictPotionRewards(context.Player, relic.DynamicVars["Potions"].IntValue, context.Rng.Rewards);
														}
													}
													else
													{
														OutOfCombatPredictionUtils.PredictUpgradedDeckCards(context.Player, ((DynamicVar)relic.DynamicVars.Cards).IntValue, (CardModel card) => (int)card.Type == 1 && card.IsUpgradable, context.SharedRng.Niche);
													}
												}
												else
												{
													OutOfCombatPredictionUtils.PredictUpgradedDeckCards(context.Player, ((DynamicVar)relic.DynamicVars.Cards).IntValue, (CardModel card) => (int)card.Type == 2 && card.IsUpgradable, context.SharedRng.Niche);
												}
											}
											else
											{
												FastForwardRelicRewardsPickup(context, 1);
											}
										}
										else
										{
											FastForwardRelicRewardsPickup(context, relic.DynamicVars["Relics"].IntValue);
										}
									}
									else
									{
										PredictScrollBoxes(context);
									}
								}
								else
								{
									OutOfCombatPredictionUtils.FastForwardDeckTransforms(context, ((DynamicVar)relic.DynamicVars.Cards).IntValue);
								}
							}
							else
							{
								PredictMultiplayerCards(context, 3);
							}
						}
						else
						{
							PredictLostCofferTips(context);
						}
					}
					else
					{
						PredictColorlessCards(context, 2);
					}
				}
				else
				{
					PredictKaleidoscopeBundles(context);
				}
			}
			else
			{
				PredictRareCharacterCards(context, ((DynamicVar)relic.DynamicVars.Cards).IntValue);
			}
		}
		else
		{
			PredictRareCharacterCards(context, ((DynamicVar)relic.DynamicVars.Cards).IntValue);
		}
	}

	private static void FastForwardRelicRewardsPickup(RunPredictionContext context, int count)
	{
		foreach (RelicModel item in OutOfCombatPredictionUtils.PredictRelicRewards(context, count))
		{
			FastForwardRelicPickup(context, item);
		}
	}

	private static IReadOnlyList<IHoverTip> PredictSilkenTressRewardTips(RunPredictionContext context, SilkenTress relic)
	{
		RelicModel item = PredictionUtils.CreateRelic(((RelicModel)relic).CanonicalInstance, context.Player);
		CardCreationOptions options = OutOfCombatPredictionUtils.CreateMonsterCombatCardRewardOptions(context.Player);
		OutOfCombatPredictionUtils.FastForwardBeforeMonsterCardReward(context);
		IReadOnlyList<CardModel> cards = CardRewardPrediction.PredictCards(context, 3, options, null, new _003C_003Ez__ReadOnlySingleElementList<AbstractModel>((AbstractModel)(object)item));
		List<IHoverTip> list = new List<IHoverTip>();
		list.AddRange(cards.ToPredictionHoverTips());
		return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
	}

	private static IReadOnlyList<CardModel> PredictNewLeaf(Player player)
	{
		return OutOfCombatPredictionUtils.PredictDistinctDeckTransformResults(player, player.RunState.Rng.Niche);
	}

	private static IReadOnlyList<CardModel> PredictPandorasBox(Player player)
	{
		Rng rng = player.RunState.Rng.Niche.Clone();
		return (from card in PileTypeExtensions.GetPile((PileType)6, player).Cards
			where card.IsBasicStrikeOrDefend && card.IsRemovable
			select PredictionUtils.PredictTransformResult(card, rng, isInCombat: false)).ToList();
	}

	private static IReadOnlyList<CardModel> PredictSandCastle(Player player, int count)
	{
		return OutOfCombatPredictionUtils.PredictUpgradedDeckCards(player, count, (CardModel card) => card.IsUpgradable);
	}

	private static IReadOnlyList<CardModel> PredictLeafyPoultice(Player player)
	{
		Rng rng = player.PlayerRng.Transformations.Clone();
		List<CardModel> source = PileTypeExtensions.GetPile((PileType)6, player).Cards.Where((CardModel card) => (int)card.Rarity == 1).ToList();
		List<CardModel> list = new List<CardModel>();
		CardModel val = ((IEnumerable<CardModel>)source).FirstOrDefault((Func<CardModel, bool>)((CardModel card) => card.Tags.Contains((CardTag)1)));
		if (val != null)
		{
			list.Add(PredictionUtils.PredictTransformResult(val, rng, isInCombat: false));
		}
		CardModel val2 = ((IEnumerable<CardModel>)source).FirstOrDefault((Func<CardModel, bool>)((CardModel card) => card.Tags.Contains((CardTag)2)));
		if (val2 != null)
		{
			list.Add(PredictionUtils.PredictTransformResult(val2, rng, isInCombat: false));
		}
		return list;
	}

	private static IReadOnlyList<IReadOnlyList<CardModel>> PredictScrollBoxes(RunPredictionContext context)
	{
		Player player = context.Player;
		bool flag = player.Character is Defect;
		CardPoolModel cardPool = player.Character.CardPool;
		CardCreationOptions val = HookMirrors.ModifyCardRewardCreationOptions(options: CardCreationOptions.ForNonCombatWithUniformOdds((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(cardPool), (Func<CardModel, bool>)((CardModel card) => (int)card.Rarity == 2)).WithFlags((CardCreationFlags)1), runState: player.RunState, player: player);
		CardCreationOptions options2 = CardCreationOptions.ForNonCombatWithUniformOdds((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(cardPool), (Func<CardModel, bool>)((CardModel card) => (int)card.Rarity == 3)).WithFlags((CardCreationFlags)1);
		options2 = HookMirrors.ModifyCardRewardCreationOptions(player.RunState, player, options2);
		List<CardModel> source = val.GetPossibleCards(player).ToList();
		List<CardModel> source2 = options2.GetPossibleCards(player).ToList();
		List<IReadOnlyList<CardModel>> list = new List<IReadOnlyList<CardModel>>();
		HashSet<ModelId> usedCardIds = new HashSet<ModelId>();
		for (int i = 0; i < 2; i++)
		{
			if (flag && context.Rng.Rewards.NextInt(100) < 1)
			{
				Claw val2 = ModelDb.Card<Claw>();
				list.Add(new _003C_003Ez__ReadOnlyArray<CardModel>((CardModel[])(object)new CardModel[3]
				{
					(CardModel)val2,
					(CardModel)val2,
					(CardModel)val2
				}));
				continue;
			}
			List<CardModel> list2 = new List<CardModel>();
			List<CardModel> list3 = source.Where((CardModel card) => !usedCardIds.Contains(((AbstractModel)card).Id)).ToList();
			for (int j = 0; j < 2; j++)
			{
				CardModel val3 = context.Rng.Rewards.NextItem<CardModel>((IEnumerable<CardModel>)list3);
				if (val3 == null)
				{
					break;
				}
				list2.Add(val3);
				usedCardIds.Add(((AbstractModel)val3).Id);
				list3.Remove(val3);
			}
			List<CardModel> list4 = source2.Where((CardModel card) => !usedCardIds.Contains(((AbstractModel)card).Id)).ToList();
			CardModel val4 = context.Rng.Rewards.NextItem<CardModel>((IEnumerable<CardModel>)list4);
			if (val4 != null)
			{
				list2.Add(val4);
				usedCardIds.Add(((AbstractModel)val4).Id);
			}
			list.Add(list2);
		}
		return list;
	}

	private static IReadOnlyList<RelicModel> PredictToyBoxRelics(RunPredictionContext context, int count)
	{
		List<RelicModel> list = (from relic in OutOfCombatPredictionUtils.PredictRelicRewards(context, count)
			select relic.ToMutable()).ToList();
		foreach (RelicModel item in list)
		{
			item.IsWax = true;
		}
		return list;
	}

	private static IReadOnlyList<CardModel> PredictBeautifulBracelet(RunPredictionContext context, BeautifulBracelet relic)
	{
		BeautifulBracelet relic2 = relic;
		Swift swift = ModelDb.Enchantment<Swift>();
		List<CardModel> list = new List<CardModel>();
		list.AddRange((from card in IEnumerableExtensions.TakeRandom<PredictedCard>(context.Deck.Cards.Where((PredictedCard card) => ((EnchantmentModel)swift).CanEnchant(card.Preview)), ((DynamicVar)((RelicModel)relic2).DynamicVars.Cards).IntValue, context.SharedRng.Niche)
			select card.Enchant(((EnchantmentModel)swift).ToMutable(), ((RelicModel)relic2).DynamicVars["Swift"].IntValue)).SelectPreviews());
		return new _003C_003Ez__ReadOnlyList<CardModel>(list);
	}

	private static void WarnOnce(Type relicType, string message)
	{
		if (WarnedRelicTypes.Add(relicType))
		{
			Entry.Logger.Warn(message, 1);
		}
	}

	private static bool IsSingleplayerUnfairPredictionAllowed(RelicModel? relicSource = null)
	{
		if (!ModData.Settings.Allows(PredictionFairness.UnfairInSingleplayer))
		{
			if (relicSource != null)
			{
				return RewardPagePredictionContext.HasOtherPendingReward(relicSource);
			}
			return false;
		}
		return true;
	}

	private static bool IsAllModesUnfairPredictionAllowed()
	{
		return ModData.Settings.Allows(PredictionFairness.UnfairInAllModes);
	}
}
