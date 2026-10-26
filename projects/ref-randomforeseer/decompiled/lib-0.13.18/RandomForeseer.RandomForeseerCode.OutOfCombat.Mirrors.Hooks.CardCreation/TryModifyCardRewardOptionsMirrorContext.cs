using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Runs;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat.Mirrors.Hooks.CardCreation;

internal sealed class TryModifyCardRewardOptionsMirrorContext : CardCreationMirrorContext
{
	public required CardCreationOptions Options { get; init; }

	public RunPredictionPlayerRngSet Rng => base.RunContext.Rng;

	public RunPredictionSharedRngSet SharedRng => base.RunContext.SharedRng;

	public CardRarityOdds RarityOdds => base.RunContext.CardRarityOdds;
}
