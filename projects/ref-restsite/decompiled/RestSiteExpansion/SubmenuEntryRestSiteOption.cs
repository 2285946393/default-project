using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;

namespace RestSiteExpansion;

public sealed class SubmenuEntryRestSiteOption : ModRestSiteOptionTemplate
{
	private const string LocTable = "relics";

	private readonly RestSubmenu _menu;

	private readonly string _iconPath;

	public override string OptionId { get; }

	public override LocString? CustomTitle => new LocString("relics", LocPrefix + ".title");

	public override LocString Description => new LocString("relics", LocPrefix + ".description");

	public override RestSiteOptionAssetProfile AssetProfile => new RestSiteOptionAssetProfile(_iconPath);

	public override bool IsEnabled => (_menu == RestSubmenu.None) ? RestMenuState.HasAnySubmenu(((RestSiteOption)this).Owner) : RestMenuState.IsActive(((RestSiteOption)this).Owner, _menu);

	private string LocPrefix => "REST_SITE_EXPANSION_RESTSITE_" + ((RestSiteOption)this).OptionId;

	public SubmenuEntryRestSiteOption(Player owner, RestSubmenu menu, string optionId, string iconPath)
		: base(owner)
	{
		_menu = menu;
		_iconPath = iconPath;
		OptionId = optionId;
		MainFile.Logger.Info($"[RestMenu] {RestMenuState.Describe(menu)}入口选项创建：player={owner.NetId} id={optionId} IsEnabled={((RestSiteOption)this).IsEnabled}", 1);
	}

	public override Task<bool> OnSelect()
	{
		if (_menu == RestSubmenu.None)
		{
			RestMenuState.CloseSubmenu(((RestSiteOption)this).Owner);
		}
		else
		{
			RestMenuState.OpenSubmenu(((RestSiteOption)this).Owner, _menu);
		}
		return Task.FromResult(result: false);
	}
}
