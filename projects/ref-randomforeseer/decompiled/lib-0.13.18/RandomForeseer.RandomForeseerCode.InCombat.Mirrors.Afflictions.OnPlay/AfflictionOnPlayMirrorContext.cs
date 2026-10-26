using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Afflictions.OnPlay;

internal sealed class AfflictionOnPlayMirrorContext : CombatCardMirrorContext<AfflictionModel>
{
	public required Creature? Target { get; init; }

	protected override AbstractModel GetDispatchSource(AfflictionModel receiver)
	{
		AfflictionModel affliction = base.OriginalCard.Affliction;
		if (affliction == null || !(((object)affliction).GetType() == ((object)receiver).GetType()))
		{
			return (AbstractModel)(object)receiver;
		}
		return (AbstractModel)(object)affliction;
	}
}
