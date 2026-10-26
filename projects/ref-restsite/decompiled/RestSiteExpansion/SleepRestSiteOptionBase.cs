using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rewards;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public abstract class SleepRestSiteOptionBase : ModRestSiteOptionTemplate
{
	protected const string LocTable = "relics";

	protected abstract decimal HealRatio { get; }

	public override IEnumerable<string> AssetPaths => ((ModRestSiteOptionTemplate)this).AssetPaths.Concat(NRestSmokeVfx.AssetPaths).Concat(NDesaturateTransitionVfx.AssetPaths);

	public override LocString Description
	{
		get
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Expected O, but got Unknown
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Expected O, but got Unknown
			if (TentBalanceGate.IsBlockedFor(((RestSiteOption)this).Owner, ((RestSiteOption)this).OptionId))
			{
				return new LocString("relics", TentBalanceGate.BlockedLocKeyFor(((RestSiteOption)this).OptionId));
			}
			LocString val = new LocString("relics", ((RestSiteOption)this).IsEnabled ? (LocPrefix + ".description") : (LocPrefix + ".descriptionDisabled"));
			val.Add("Heal", HealAmount(((RestSiteOption)this).Owner));
			return val;
		}
	}

	protected string LocPrefix => "REST_SITE_EXPANSION_RESTSITE_" + ((RestSiteOption)this).OptionId;

	protected SleepRestSiteOptionBase(Player owner)
		: base(owner)
	{
	}

	protected abstract void ApplyPendingEffect(Player player);

	public override async Task<bool> OnSelect()
	{
		Player owner = ((RestSiteOption)this).Owner;
		if (TentBalanceGate.IsBlockedFor(owner, ((RestSiteOption)this).OptionId))
		{
			MainFile.Logger.Info($"[TentBalance] {((RestSiteOption)this).OptionId} 被帐篷平衡拦下（组={TentBalanceGate.GroupNameFor(((RestSiteOption)this).OptionId)}）：player={owner.NetId} 已选={TentBalanceGate.DescribeChoices(owner)}", 1);
			return false;
		}
		await CreatureCmd.Heal(owner.Creature, HealAmount(owner), true);
		await Hook.AfterRestSiteHeal(owner.RunState, owner, false);
		List<Reward> rewards = new List<Reward>();
		Hook.ModifyRestSiteHealRewards(owner.RunState, owner, rewards, false);
		await RewardsCmd.OfferCustom(owner, rewards);
		ApplyPendingEffect(owner);
		MainFile.Logger.Info($"[Sleep] {((RestSiteOption)this).OptionId} 完成：回血 {HealAmount(owner):0}（{HealRatio:P0} 最大生命），待生效效果已置位", 1);
		return true;
	}

	public override async Task DoLocalPostSelectVfx(CancellationToken ct = default(CancellationToken))
	{
		HealRestSiteOption.PlayRestSiteHealSfx();
		NRestSiteRoom instance = NRestSiteRoom.Instance;
		if (instance != null)
		{
			GodotTreeExtensions.AddChildSafely((Node)(object)instance, (Node)(object)NRestSmokeVfx.Create());
		}
		NRestSiteRoom instance2 = NRestSiteRoom.Instance;
		if (instance2 != null)
		{
			GodotTreeExtensions.AddChildSafely((Node)(object)instance2, (Node)(object)NDesaturateTransitionVfx.Create());
		}
		await Cmd.CustomScaledWait(1.5f, 2.5f, false, ct);
	}

	public override Task DoRemotePostSelectVfx()
	{
		NDebugAudioManager instance = NDebugAudioManager.Instance;
		if (instance != null)
		{
			instance.Play("SOTE_SFX_SleepBlanket_v1.mp3", 0.5f, (PitchVariance)1);
		}
		return Task.CompletedTask;
	}

	protected decimal HealAmount(Player player)
	{
		decimal num = Math.Floor((decimal)player.Creature.MaxHp * HealRatio);
		return Math.Floor(Hook.ModifyRestSiteHealAmount(player.RunState, player.Creature, num));
	}
}
