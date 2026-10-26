using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public abstract class SmithRestSiteOptionBase : ModRestSiteOptionTemplate
{
	private IReadOnlyList<CardModel> _upgradedCards = Array.Empty<CardModel>();

	public override IEnumerable<string> AssetPaths => ((ModRestSiteOptionTemplate)this).AssetPaths.Concat(NCardSmithVfx.AssetPaths);

	protected SmithRestSiteOptionBase(Player owner)
		: base(owner)
	{
	}

	protected void MarkUpgraded(IEnumerable<CardModel> cards)
	{
		_upgradedCards = cards.ToList();
	}

	public override async Task DoLocalPostSelectVfx(CancellationToken ct = default(CancellationToken))
	{
		if (_upgradedCards.Count != 0)
		{
			PlayLocalSmithVfx();
			await Cmd.CustomScaledWait(1f, 2f, false, ct);
		}
	}

	protected void PlayLocalSmithVfx()
	{
		if (_upgradedCards.Count != 0)
		{
			NRun instance = NRun.Instance;
			if (instance != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance.GlobalUi.CardPreviewContainer, (Node)(object)NCardSmithVfx.Create((IEnumerable<CardModel>)_upgradedCards.ToArray(), true));
			}
		}
	}

	public override Task DoRemotePostSelectVfx()
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		NRestSiteRoom instance = NRestSiteRoom.Instance;
		NRestSiteCharacter val = ((instance != null) ? ((IEnumerable<NRestSiteCharacter>)instance.Characters).FirstOrDefault((Func<NRestSiteCharacter, bool>)((NRestSiteCharacter c) => c.Player == ((RestSiteOption)this).Owner)) : null);
		NCardSmithVfx val2 = NCardSmithVfx.Create();
		if (val2 == null)
		{
			return Task.CompletedTask;
		}
		if (val != null)
		{
			GodotTreeExtensions.AddChildSafely((Node)(object)val, (Node)(object)val2);
		}
		((Node2D)val2).Position = Vector2.Zero;
		return Task.CompletedTask;
	}
}
