using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public sealed class WhackFlyconidRestSiteOption : ModRestSiteOptionTemplate
{
	private const string LocTable = "relics";

	private const string LocPrefix = "REST_SITE_EXPANSION_RESTSITE_WHACK_FLYCONID";

	private const int HitsRequired = 60;

	private const int HealValue = 5;

	private const int MaxHpLoss = 1;

	private const string HitSfxPath = "event:/sfx/enemy/enemy_impact_enemy_size/enemy_impact_plant";

	private int _hits;

	public override string OptionId => "WHACK_FLYCONID";

	public override LocString? CustomTitle => new LocString("relics", "REST_SITE_EXPANSION_RESTSITE_WHACK_FLYCONID.title");

	public override IEnumerable<string> AssetPaths => ((ModRestSiteOptionTemplate)this).AssetPaths.Concat(PlayerFullscreenHealVfx.AssetPaths);

	public override LocString Description
	{
		get
		{
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Expected O, but got Unknown
			string text = ((!((RestSiteOption)this).IsEnabled) ? "REST_SITE_EXPANSION_RESTSITE_WHACK_FLYCONID.descriptionDisabled" : ((((RestSiteOption)this).Owner.Creature.MaxHp <= 1) ? "REST_SITE_EXPANSION_RESTSITE_WHACK_FLYCONID.descriptionLowMaxHp" : "REST_SITE_EXPANSION_RESTSITE_WHACK_FLYCONID.description"));
			LocString val = new LocString("relics", text);
			val.Add("Count", (decimal)_hits);
			val.Add("MaxHp", (decimal)((RestSiteOption)this).Owner.Creature.MaxHp);
			return val;
		}
	}

	public override RestSiteOptionAssetProfile AssetProfile => new RestSiteOptionAssetProfile("res://art/images/rest_site/whack_flyconid.png");

	public override bool IsEnabled => RestSiteExpansionRuntimeConfig.ResolveFun(((RestSiteOption)this).Owner).WhackFlyconidEnabled;

	public WhackFlyconidRestSiteOption(Player owner)
		: base(owner)
	{
		MainFile.Logger.Info($"[WhackFlyconid] 选项创建：player={owner.NetId} IsEnabled={((RestSiteOption)this).IsEnabled}", 1);
	}

	public override async Task<bool> OnSelect()
	{
		if (!((RestSiteOption)this).IsEnabled)
		{
			MainFile.Logger.Info($"[WhackFlyconid] 已关闭，点击无效果：player={((RestSiteOption)this).Owner.NetId}", 1);
			return false;
		}
		if (LocalContext.IsMe(((RestSiteOption)this).Owner))
		{
			SfxCmd.Play("event:/sfx/enemy/enemy_impact_enemy_size/enemy_impact_plant", 1f);
		}
		_hits++;
		if (((RestSiteOption)this).Owner.Creature.MaxHp <= 1)
		{
			MainFile.Logger.Info($"[WhackFlyconid] 已打 {_hits} 次但最大生命不足（{((RestSiteOption)this).Owner.Creature.MaxHp}），不触发：player={((RestSiteOption)this).Owner.NetId}", 1);
			return false;
		}
		if (_hits < 60)
		{
			MainFile.Logger.Info($"[WhackFlyconid] 拍打 {_hits}/{60}：player={((RestSiteOption)this).Owner.NetId}", 1);
			return false;
		}
		await CreatureCmd.LoseMaxHp((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((RestSiteOption)this).Owner.Creature, 1m, false);
		await CreatureCmd.Heal(((RestSiteOption)this).Owner.Creature, 5m, true);
		MainFile.Logger.Info($"[WhackFlyconid] 打满 {60} 次触发：player={((RestSiteOption)this).Owner.NetId} 最大生命 -{1}（现 {((RestSiteOption)this).Owner.Creature.MaxHp}）、回复 {5}（现 {((RestSiteOption)this).Owner.Creature.CurrentHp}）", 1);
		return true;
	}

	public override Task DoLocalPostSelectVfx(CancellationToken ct = default(CancellationToken))
	{
		PlayerFullscreenHealVfx.Play(((RestSiteOption)this).Owner, 5m, (Control)(object)NRestSiteRoom.Instance);
		return Task.CompletedTask;
	}

	public override Task DoRemotePostSelectVfx()
	{
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		NRestSiteRoom instance = NRestSiteRoom.Instance;
		NRestSiteCharacter val = ((instance != null) ? ((IEnumerable<NRestSiteCharacter>)instance.Characters).FirstOrDefault((Func<NRestSiteCharacter, bool>)((NRestSiteCharacter c) => c.Player == ((RestSiteOption)this).Owner)) : null);
		if (val == null)
		{
			return Task.CompletedTask;
		}
		Node2D val2 = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("vfx/vfx_cross_heal")).Instantiate<Node2D>((GenEditState)0);
		GodotTreeExtensions.AddChildSafely((Node)(object)val, (Node)(object)val2);
		val2.Position = Vector2.Zero;
		return Task.CompletedTask;
	}
}
