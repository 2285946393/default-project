using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Runs;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.OutOfCombat;

internal sealed class RunPredictionContext
{
	[CompilerGenerated]
	private Player _003Cplayer_003EP;

	[CompilerGenerated]
	private RunPredictionSharedRngSet _003CsharedRng_003EP;

	[CompilerGenerated]
	private Dictionary<Player, RunPredictionPlayerContext> _003CplayerContexts_003EP;

	public Player Player => _003Cplayer_003EP;

	public IRunState RunState => Player.RunState;

	public RunPredictionSharedRngSet SharedRng => _003CsharedRng_003EP;

	public RunPredictionPlayerRngSet Rng => GetPlayer(_003Cplayer_003EP).Rng;

	public RelicGrabBag RelicGrabBag => GetPlayer(_003Cplayer_003EP).RelicGrabBag;

	public SimCardPile Deck => GetPlayer(_003Cplayer_003EP).Deck;

	public CardRarityOdds CardRarityOdds => GetPlayer(_003Cplayer_003EP).CardRarityOdds;

	public PotionRewardOdds PotionRewardOdds => GetPlayer(_003Cplayer_003EP).PotionRewardOdds;

	public RunPredictionContext(Player player, RunPredictionSharedRngSet sharedRng, Dictionary<Player, RunPredictionPlayerContext> playerContexts)
	{
		_003Cplayer_003EP = player;
		_003CsharedRng_003EP = sharedRng;
		_003CplayerContexts_003EP = playerContexts;
		base._002Ector();
	}

	public RunPredictionContext(Player player)
		: this(player, RunPredictionSharedRngSet.From(player.RunState.Rng), new Dictionary<Player, RunPredictionPlayerContext>())
	{
	}

	public RunPredictionPlayerContext GetPlayer(Player otherPlayer)
	{
		if (!_003CplayerContexts_003EP.TryGetValue(otherPlayer, out RunPredictionPlayerContext value))
		{
			value = new RunPredictionPlayerContext(otherPlayer);
			_003CplayerContexts_003EP[otherPlayer] = value;
		}
		return value;
	}

	public RunPredictionContext Clone()
	{
		return new RunPredictionContext(_003Cplayer_003EP, _003CsharedRng_003EP.Clone(), _003CplayerContexts_003EP.ToDictionary<KeyValuePair<Player, RunPredictionPlayerContext>, Player, RunPredictionPlayerContext>((KeyValuePair<Player, RunPredictionPlayerContext> kvp) => kvp.Key, (KeyValuePair<Player, RunPredictionPlayerContext> kvp) => kvp.Value.Clone()));
	}

	public RunPredictionContext ForPlayer(Player otherPlayer)
	{
		if (_003Cplayer_003EP != otherPlayer)
		{
			return new RunPredictionContext(otherPlayer, _003CsharedRng_003EP, _003CplayerContexts_003EP);
		}
		return this;
	}
}
