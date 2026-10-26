using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Enchantments.OnPlay;

internal sealed class EnchantmentOnPlayMirrorContext : CombatCardMirrorContext<EnchantmentModel>
{
	public required CardPlay CardPlay { get; init; }

	protected override AbstractModel GetDispatchSource(EnchantmentModel receiver)
	{
		EnchantmentModel enchantment = base.OriginalCard.Enchantment;
		if (enchantment == null || !(((object)enchantment).GetType() == ((object)receiver).GetType()))
		{
			return (AbstractModel)(object)receiver;
		}
		return (AbstractModel)(object)enchantment;
	}
}
