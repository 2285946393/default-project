using MegaCrit.Sts2.Core.Models;

namespace RandomForeseer.RandomForeseerCode.InCombat;

internal sealed record DamagePredictionLine(decimal Damage, decimal UnblockedDamage, bool WasTargetKilled, AbstractModel Source);
