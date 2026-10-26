using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal sealed record DamagePredictionTarget(Creature Target, IReadOnlyList<DamagePredictionLine> DamageLines)
{
	public decimal TotalDamage => DamageLines.Sum((DamagePredictionLine line) => line.Damage);

	public decimal TotalUnblockedDamage => DamageLines.Sum((DamagePredictionLine line) => line.UnblockedDamage);

	public bool WasTargetKilled => DamageLines.Any((DamagePredictionLine line) => line.WasTargetKilled);
}
