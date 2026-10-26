using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal sealed class RunPredictionPlayerContext
{
	[CompilerGenerated]
	private Player _003Cplayer_003EP;

	[CompilerGenerated]
	private RunPredictionPlayerRngSet _003Crng_003EP;

	[CompilerGenerated]
	private RelicGrabBag? _003CrelicGrabBag_003EP;

	[CompilerGenerated]
	private SimCardPile? _003Cdeck_003EP;

	[CompilerGenerated]
	private CardRarityOdds _003CcardRarityOdds_003EP;

	[CompilerGenerated]
	private PotionRewardOdds _003CpotionRewardOdds_003EP;

	public Player Player => _003Cplayer_003EP;

	public RunPredictionPlayerRngSet Rng => _003Crng_003EP;

	public RelicGrabBag RelicGrabBag => _003CrelicGrabBag_003EP ?? (_003CrelicGrabBag_003EP = _003Cplayer_003EP.RelicGrabBag.Clone());

	public SimCardPile Deck => _003Cdeck_003EP ?? (_003Cdeck_003EP = new SimCardPile(_003Cplayer_003EP.Deck));

	public CardRarityOdds CardRarityOdds => _003CcardRarityOdds_003EP;

	public PotionRewardOdds PotionRewardOdds => _003CpotionRewardOdds_003EP;

	public RunPredictionPlayerContext(Player player, RunPredictionPlayerRngSet rng, RelicGrabBag? relicGrabBag, SimCardPile? deck, CardRarityOdds cardRarityOdds, PotionRewardOdds potionRewardOdds)
	{
		_003Cplayer_003EP = player;
		_003Crng_003EP = rng;
		_003CrelicGrabBag_003EP = relicGrabBag;
		_003Cdeck_003EP = deck;
		_003CcardRarityOdds_003EP = cardRarityOdds;
		_003CpotionRewardOdds_003EP = potionRewardOdds;
		base._002Ector();
	}

	public RunPredictionPlayerContext(Player player)
		: this(player, RunPredictionPlayerRngSet.From(player.PlayerRng))
	{
	}

	public RunPredictionPlayerContext(Player player, RunPredictionPlayerRngSet rng)
		: this(player, rng, null, null, new CardRarityOdds(((AbstractOdds)player.PlayerOdds.CardRarity).CurrentValue, rng.Rewards), new PotionRewardOdds(((AbstractOdds)player.PlayerOdds.PotionReward).CurrentValue, rng.Rewards))
	{
	}//IL_001b: Unknown result type (might be due to invalid IL or missing references)
	//IL_0036: Unknown result type (might be due to invalid IL or missing references)
	//IL_0040: Expected O, but got Unknown
	//IL_0040: Expected O, but got Unknown


	public RunPredictionPlayerContext Clone()
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		//IL_0068: Expected O, but got Unknown
		RunPredictionPlayerRngSet runPredictionPlayerRngSet = _003Crng_003EP.Clone();
		return new RunPredictionPlayerContext(_003Cplayer_003EP, runPredictionPlayerRngSet, _003CrelicGrabBag_003EP?.Clone(), _003Cdeck_003EP?.Clone(), new CardRarityOdds(((AbstractOdds)_003CcardRarityOdds_003EP).CurrentValue, runPredictionPlayerRngSet.Rewards), new PotionRewardOdds(((AbstractOdds)_003CpotionRewardOdds_003EP).CurrentValue, runPredictionPlayerRngSet.Rewards));
	}
}
