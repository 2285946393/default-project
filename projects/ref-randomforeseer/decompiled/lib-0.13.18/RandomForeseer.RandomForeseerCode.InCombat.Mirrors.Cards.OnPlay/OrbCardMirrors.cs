using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using RandomForeseer.RandomForeseerCode.InCombat.Simulation;

namespace RandomForeseer.RandomForeseerCode.InCombat.Mirrors.Cards.OnPlay;

internal static class OrbCardMirrors
{
	public static void BallLightningOnPlay(BallLightning card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle();
		context.Simulator.OrbChannel<LightningOrb>(((CardModel)card).Owner, 1);
	}

	public static void ChaosOnPlay(Chaos card, CardOnPlayMirrorContext context)
	{
		for (int i = 0; i < ((DynamicVar)((CardModel)card).DynamicVars.Repeat).IntValue; i++)
		{
			OrbModel orb = OrbModel.GetRandomOrb(context.Rng.CombatOrbGeneration).ToMutable(0);
			context.Simulator.OrbChannel(((CardModel)card).Owner, orb);
		}
	}

	public static void ChillOnPlay(Chill card, CardOnPlayMirrorContext context)
	{
		context.Simulator.OrbChannel<FrostOrb>(((CardModel)card).Owner, context.State.HittableEnemies.Count);
	}

	public static void ColdSnapOnPlay(ColdSnap card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle();
		context.Simulator.OrbChannel<FrostOrb>(((CardModel)card).Owner, 1);
	}

	public static void ConsumingShadowOnPlay(ConsumingShadow card, CardOnPlayMirrorContext context)
	{
		context.Simulator.OrbChannel<DarkOrb>(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Repeat).IntValue);
	}

	public static void CoolheadedOnPlay(Coolheaded card, CardOnPlayMirrorContext context)
	{
		context.Simulator.OrbChannel<FrostOrb>(((CardModel)card).Owner, 1);
		context.Simulator.Draw(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Cards).BaseValue);
	}

	public static void DarknessOnPlay(Darkness card, CardOnPlayMirrorContext context)
	{
		context.Simulator.OrbChannel<DarkOrb>(((CardModel)card).Owner, 1);
		int num = ((!((CardModel)card).IsUpgraded) ? 1 : 2);
		DarkOrb[] array = context.OwnerState.OrbQueue.Orbs.OfType<DarkOrb>().ToArray();
		foreach (DarkOrb orb in array)
		{
			for (int j = 0; j < num; j++)
			{
				context.Simulator.OrbPassive((OrbModel)(object)orb);
			}
		}
	}

	public static void DualcastOnPlay(Dualcast card, CardOnPlayMirrorContext context)
	{
		context.Simulator.OrbEvokeNext(((CardModel)card).Owner, 2);
	}

	public static void FusionOnPlay(Fusion card, CardOnPlayMirrorContext context)
	{
		context.Simulator.OrbChannel<PlasmaOrb>(((CardModel)card).Owner, 1);
	}

	public static void GlacierOnPlay(Glacier card, CardOnPlayMirrorContext context)
	{
		context.GainBlock(((CardModel)card).Owner.Creature);
		context.Simulator.OrbChannel<FrostOrb>(((CardModel)card).Owner, 2);
	}

	public static void GlassworkOnPlay(Glasswork card, CardOnPlayMirrorContext context)
	{
		context.GainBlock(((CardModel)card).Owner.Creature);
		context.Simulator.OrbChannel<GlassOrb>(((CardModel)card).Owner, 1);
	}

	public static void IceLanceOnPlay(IceLance card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle();
		context.Simulator.OrbChannel<FrostOrb>(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Repeat).IntValue);
	}

	public static void IgnitionOnPlay(Ignition card, CardOnPlayMirrorContext context)
	{
		context.Simulator.OrbChannel<PlasmaOrb>(context.TargetPlayer, 1);
	}

	public static void MeteorStrikeOnPlay(MeteorStrike card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle();
		context.Simulator.OrbChannel<PlasmaOrb>(((CardModel)card).Owner, 3);
	}

	public static void MultiCastOnPlay(MultiCast card, CardOnPlayMirrorContext context)
	{
		int repeat = context.ResolveEnergyXValue() + (((CardModel)card).IsUpgraded ? 1 : 0);
		context.Simulator.OrbEvokeNext(((CardModel)card).Owner, repeat);
	}

	public static void NullOnPlay(Null card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle();
		context.Simulator.OrbChannel<DarkOrb>(((CardModel)card).Owner, 1);
	}

	public static void QuadcastOnPlay(Quadcast card, CardOnPlayMirrorContext context)
	{
		context.Simulator.OrbEvokeNext(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Repeat).IntValue);
	}

	public static void RainbowOnPlay(Rainbow card, CardOnPlayMirrorContext context)
	{
		context.Simulator.OrbChannel<LightningOrb>(((CardModel)card).Owner, 1);
		context.Simulator.OrbChannel<FrostOrb>(((CardModel)card).Owner, 1);
		context.Simulator.OrbChannel<DarkOrb>(((CardModel)card).Owner, 1);
	}

	public static void RefractOnPlay(Refract card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle(2);
		context.Simulator.OrbChannel<GlassOrb>(((CardModel)card).Owner, ((DynamicVar)((CardModel)card).DynamicVars.Repeat).IntValue);
	}

	public static void ShadowShieldOnPlay(ShadowShield card, CardOnPlayMirrorContext context)
	{
		context.GainBlock(((CardModel)card).Owner.Creature);
		context.Simulator.OrbChannel<DarkOrb>(((CardModel)card).Owner, 1);
	}

	public static void ShatterOnPlay(Shatter card, CardOnPlayMirrorContext context)
	{
		context.AttackAllOpponents();
		int count = context.OwnerState.OrbQueue.Orbs.Count;
		for (int i = 0; i < count; i++)
		{
			context.Simulator.OrbEvokeNext(((CardModel)card).Owner, 2);
		}
	}

	public static void SpinnerOnPlay(Spinner card, CardOnPlayMirrorContext context)
	{
		if (((CardModel)card).IsUpgraded)
		{
			context.Simulator.OrbChannel<GlassOrb>(((CardModel)card).Owner, 1);
		}
	}

	public static void TempestOnPlay(Tempest card, CardOnPlayMirrorContext context)
	{
		int count = context.ResolveEnergyXValue() + (((CardModel)card).IsUpgraded ? 1 : 0);
		context.Simulator.OrbChannel<LightningOrb>(((CardModel)card).Owner, count);
	}

	public static void TeslaCoilOnPlay(TeslaCoil card, CardOnPlayMirrorContext context)
	{
		context.AttackSingle();
		int num = ((!((CardModel)card).IsUpgraded) ? 1 : 2);
		LightningOrb[] array = context.OwnerState.OrbQueue.Orbs.OfType<LightningOrb>().ToArray();
		foreach (LightningOrb orb in array)
		{
			for (int j = 0; j < num; j++)
			{
				context.Simulator.OrbPassive((OrbModel)(object)orb, context.Target);
			}
		}
	}

	public static void VoltaicOnPlay(Voltaic card, CardOnPlayMirrorContext context)
	{
		Voltaic card2 = card;
		int num = CombatManager.Instance.History.Entries.OfType<OrbChanneledEntry>().Count((OrbChanneledEntry entry) => ((CombatHistoryEntry)entry).Actor.Player == ((CardModel)card2).Owner && entry.Orb is LightningOrb);
		num += context.Simulator.History.OfType<CombatPredictionOrbChanneledEntry>().Count((CombatPredictionOrbChanneledEntry entry) => entry.Orb.Owner == ((CardModel)card2).Owner && entry.Orb is LightningOrb);
		context.Simulator.OrbChannel<LightningOrb>(((CardModel)card2).Owner, num);
	}

	public static void ZapOnPlay(Zap card, CardOnPlayMirrorContext context)
	{
		context.Simulator.OrbChannel<LightningOrb>(((CardModel)card).Owner, 1);
	}
}
