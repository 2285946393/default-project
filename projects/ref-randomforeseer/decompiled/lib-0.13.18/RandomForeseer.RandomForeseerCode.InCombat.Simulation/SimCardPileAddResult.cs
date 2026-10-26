using System.Runtime.CompilerServices;
using System.Text;
using MegaCrit.Sts2.Core.Entities.Cards;
using RandomForeseer.RandomForeseerCode.Common;

namespace RandomForeseer.RandomForeseerCode.InCombat.Simulation;

internal readonly record struct SimCardPileAddResult(bool Success, PredictedCard CardAdded, PileType OldPileType, PileType TargetPileType)
{
	public SimCardPileAddResult(bool Success, PredictedCard CardAdded, PileType OldPileType, PileType TargetPileType)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		this.Success = Success;
		this.CardAdded = CardAdded;
		this.OldPileType = OldPileType;
		this.TargetPileType = TargetPileType;
	}

	public SimCardPileAddResult(bool success, PredictedCard cardAdded)
		: this(success, cardAdded, (PileType)0, (PileType)0)
	{
	}

	[CompilerGenerated]
	private bool PrintMembers(StringBuilder builder)
	{
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		builder.Append("Success = ");
		builder.Append(Success.ToString());
		builder.Append(", CardAdded = ");
		builder.Append(CardAdded);
		builder.Append(", OldPileType = ");
		PileType val = OldPileType;
		builder.Append(((object)(PileType)(ref val)).ToString());
		builder.Append(", TargetPileType = ");
		val = TargetPileType;
		builder.Append(((object)(PileType)(ref val)).ToString());
		return true;
	}

	[CompilerGenerated]
	public void Deconstruct(out bool Success, out PredictedCard CardAdded, out PileType OldPileType, out PileType TargetPileType)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected I4, but got Unknown
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected I4, but got Unknown
		Success = this.Success;
		CardAdded = this.CardAdded;
		OldPileType = (PileType)(int)this.OldPileType;
		TargetPileType = (PileType)(int)this.TargetPileType;
	}
}
