using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace RandomForeseer.RandomForeseerCode.Common;

internal static class PredictionExtensions
{
	public static Rng Clone(this Rng rng)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		return new Rng(rng.ToSerializable());
	}

	public static void Advance(this Rng rng, int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count, "count");
		for (int i = 0; i < count; i++)
		{
			rng.NextUnsignedLong();
		}
	}

	public static RelicGrabBag Clone(this RelicGrabBag grabBag)
	{
		return RelicGrabBag.FromSerializable(grabBag.ToSerializable());
	}

	public static IEnumerable<CardModel> GetUnlockedCards(this Player player, CardPoolModel cardPool)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		return cardPool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);
	}

	public static IEnumerable<CardModel> GetUnlockedCharacterCards(this Player player)
	{
		return player.GetUnlockedCards(player.Character.CardPool);
	}

	public static IEnumerable<CardModel> GetUnlockedColorlessCards(this Player player)
	{
		return player.GetUnlockedCards((CardPoolModel)(object)ModelDb.CardPool<ColorlessCardPool>());
	}

	public static IEnumerable<CardModel> GetUnlockedCurseCards(this Player player)
	{
		return player.GetUnlockedCards((CardPoolModel)(object)ModelDb.CardPool<CurseCardPool>());
	}

	public static string GetTitle(this AbstractModel model)
	{
		try
		{
			CardModel val = (CardModel)(object)((model is CardModel) ? model : null);
			string result;
			if (val == null)
			{
				RelicModel val2 = (RelicModel)(object)((model is RelicModel) ? model : null);
				if (val2 == null)
				{
					PowerModel val3 = (PowerModel)(object)((model is PowerModel) ? model : null);
					if (val3 == null)
					{
						PotionModel val4 = (PotionModel)(object)((model is PotionModel) ? model : null);
						if (val4 == null)
						{
							ModifierModel val5 = (ModifierModel)(object)((model is ModifierModel) ? model : null);
							if (val5 == null)
							{
								AfflictionModel val6 = (AfflictionModel)(object)((model is AfflictionModel) ? model : null);
								if (val6 == null)
								{
									EnchantmentModel val7 = (EnchantmentModel)(object)((model is EnchantmentModel) ? model : null);
									if (val7 == null)
									{
										OrbModel val8 = (OrbModel)(object)((model is OrbModel) ? model : null);
										if (val8 == null)
										{
											MonsterModel val9 = (MonsterModel)(object)((model is MonsterModel) ? model : null);
											result = ((val9 == null) ? model.Id.Entry : val9.Title.GetFormattedText());
										}
										else
										{
											result = val8.Title.GetFormattedText();
										}
									}
									else
									{
										result = val7.Title.GetFormattedText();
									}
								}
								else
								{
									result = val6.Title.GetFormattedText();
								}
							}
							else
							{
								result = val5.Title.GetFormattedText();
							}
						}
						else
						{
							result = val4.Title.GetFormattedText();
						}
					}
					else
					{
						result = val3.Title.GetFormattedText();
					}
				}
				else
				{
					result = val2.Title.GetFormattedText();
				}
			}
			else
			{
				result = val.Title;
			}
			return result;
		}
		catch
		{
			return model.Id.Entry;
		}
	}
}
