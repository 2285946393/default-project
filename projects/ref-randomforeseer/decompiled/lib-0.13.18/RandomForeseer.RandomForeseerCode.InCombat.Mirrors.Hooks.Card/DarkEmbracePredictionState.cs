using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Hooks.Card;

internal sealed class DarkEmbracePredictionState
{
	public int EtherealCount { get; set; } = ((PowerModel)power).GetInternalData<Data>().etherealCount;


	public DarkEmbracePredictionState(DarkEmbracePower power)
	{
	}
}
