using System.Linq;
using RandomForeseer.RandomForeseerCode.Common;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;
using RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Potions.OnUse;

namespace RandomForeseer.RandomForeseerCode.InCombat.Extensions;

internal static class CombatPredictionTraceFrameExtensions
{
	public static PredictionTraceFrame? FindOriginatingEffect(this PredictionTraceFrame trace)
	{
		return trace.Ancestors().FirstOrDefault(delegate(PredictionTraceFrame frame)
		{
			bool flag = CardOnPlayMirrors.IsOnPlayInvocation(frame.Invocation) || PotionOnUseMirrors.IsOnUseInvocation(frame.Invocation);
			bool flag2;
			if (!flag)
			{
				PredictionActionKind? action = frame.Invocation.Action;
				if (action.HasValue)
				{
					PredictionActionKind valueOrDefault = action.GetValueOrDefault();
					if ((uint)valueOrDefault <= 1u)
					{
						flag2 = true;
						goto IL_004a;
					}
				}
				flag2 = false;
				goto IL_004a;
			}
			goto IL_004c;
			IL_004a:
			flag = flag2;
			goto IL_004c;
			IL_004c:
			return flag;
		});
	}

	public static PredictionTraceFrame? FindOriginatingAction(this PredictionTraceFrame trace)
	{
		return trace.Ancestors().FirstOrDefault(delegate(PredictionTraceFrame frame)
		{
			PredictionActionKind? action = frame.Invocation.Action;
			if (action.HasValue)
			{
				PredictionActionKind valueOrDefault = action.GetValueOrDefault();
				if ((uint)valueOrDefault <= 1u)
				{
					return true;
				}
			}
			return false;
		});
	}
}
