using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Random;

namespace RestSiteExpansion;

internal static class GoldEnchantPool
{
	internal readonly record struct Entry(EnchantmentModel Canonical, int Amount)
	{
		public string Id => ((AbstractModel)Canonical).Id.Entry;
	}

	private static Entry[]? _pool;

	private static Entry[] Pool
	{
		get
		{
			object obj = _pool;
			if (obj == null)
			{
				obj = new Entry[12]
				{
					new Entry((EnchantmentModel)(object)ModelDb.Enchantment<Sharp>(), 3),
					new Entry((EnchantmentModel)(object)ModelDb.Enchantment<Momentum>(), 2),
					new Entry((EnchantmentModel)(object)ModelDb.Enchantment<Vigorous>(), 3),
					new Entry((EnchantmentModel)(object)ModelDb.Enchantment<Adroit>(), 3),
					new Entry((EnchantmentModel)(object)ModelDb.Enchantment<Nimble>(), 3),
					new Entry((EnchantmentModel)(object)ModelDb.Enchantment<Inky>(), 1),
					new Entry((EnchantmentModel)(object)ModelDb.Enchantment<Swift>(), 2),
					new Entry((EnchantmentModel)(object)ModelDb.Enchantment<Glam>(), 0),
					new Entry((EnchantmentModel)(object)ModelDb.Enchantment<Spiral>(), 0),
					new Entry((EnchantmentModel)(object)ModelDb.Enchantment<Steady>(), 0),
					new Entry((EnchantmentModel)(object)ModelDb.Enchantment<RoyallyApproved>(), 0),
					new Entry((EnchantmentModel)(object)ModelDb.Enchantment<Imbued>(), 0)
				};
				_pool = (Entry[]?)obj;
			}
			return (Entry[])obj;
		}
	}

	public static int Count => Pool.Length;

	public static Entry? DrawFor(CardModel card, Rng rng)
	{
		CardModel card2 = card;
		Entry[] array = Pool.Where((Entry e) => e.Canonical.CanEnchant(card2)).ToArray();
		return (array.Length == 0) ? null : new Entry?(array[rng.NextInt(array.Length)]);
	}

	public static bool CanAnyEnchant(CardModel card)
	{
		CardModel card2 = card;
		return Pool.Any((Entry e) => e.Canonical.CanEnchant(card2));
	}

	public static bool HasAnyEnchantable(Player player)
	{
		return player.Deck.Cards.Any(CanAnyEnchant);
	}
}
