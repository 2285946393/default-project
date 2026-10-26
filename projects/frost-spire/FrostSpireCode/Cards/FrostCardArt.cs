using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace FrostSpire.Cards;

/// <summary>
/// 凛冬卡牌的**卡面资源覆盖**（M3.6，接 pck）。
///
/// ★ 机制（反编译 STS2-RitsuLib 0.6.5 取证）：
///   游戏取卡图走 `CardModel.Portrait` = `ResourceLoader.Load(PortraitPath)`，
///   而 `PortraitPath` 默认拼的是 **本体的 `res://images/atlases/card_atlas.sprites/…`** ——
///   本 mod 的图不在那儿，所以必须**换路径**。
///
///   RitsuLib 为此提供了官方口子：卡牌实现
///   <see cref="IModCardAssetOverrides"/>，它在 `PortraitPath` 的 getter 上打了 Harmony 补丁，
///   只要实例实现了这个接口、且 <c>CustomPortraitPath</c> 非空，就**用我们的路径**。
///
/// ★ 为什么用「接口默认实现」而不是基类：
///   凛冬的卡分三族（`FrostDeployCardBase` / `FrostBuildingCard` / `SummonMilitiaCard` 直接挂
///   `CardModel`），三族是平行的、没有共同基类。用接口默认实现，**三族各加一个
///   <c>: IFrostCardArt</c> 就全都有卡面**，不用为「加图」这件事重构继承树。
///
/// ★ 安全兜底：RitsuLib 的 `TryUseStringOverride` 会先做
///   `AssetPathDiagnostics.Exists(path)` 检查 —— **路径不存在就静默回退**到游戏默认渲染。
///   所以：**图还没画的时候，代码这么写也完全不会崩**，只是继续显示默认卡面。
/// </summary>
public interface IFrostCardArt : IModCardAssetOverrides
{
    /// <summary>
    /// 卡面文件名（不带扩展名）。默认由**类名**推导（`IceBulwarkCard` → `ice_bulwark`）。
    /// 想给某张卡换个名字，覆盖这个属性即可。
    /// </summary>
    string FrostArtEntry => FrostCardArt.EntryOf(((object)this).GetType());

    CardAssetProfile IModCardAssetOverrides.AssetProfile
        => new(PortraitPath: FrostCardArt.PathOf(FrostArtEntry));

    string? IModCardAssetOverrides.CustomPortraitPath => FrostCardArt.PathOf(FrostArtEntry);
    string? IModCardAssetOverrides.CustomBetaPortraitPath => null;
    string? IModCardAssetOverrides.CustomFramePath => null;
    string? IModCardAssetOverrides.CustomPortraitBorderPath => null;
    string? IModCardAssetOverrides.CustomEnergyIconPath => null;
    string? IModCardAssetOverrides.CustomFrameMaterialPath => null;
    string? IModCardAssetOverrides.CustomOverlayScenePath => null;
    string? IModCardAssetOverrides.CustomBannerTexturePath => null;
    string? IModCardAssetOverrides.CustomBannerMaterialPath => null;
}

/// <summary>
/// 卡面路径的推导规则（`IFrostCardArt` 的实现细节，抽出来方便单测/复用）。
/// </summary>
public static class FrostCardArt
{
    /// <summary>mod 的 pck 挂载前缀（`res://` 根 + ModId 目录，与 quest-spire 同款约定）。</summary>
    private const string Root = "res://" + Entry.ModId + "/images/card_art/";

    /// <summary>`IceBulwarkCard` → `ice_bulwark`；去掉尾部 `Card`，再 PascalCase → snake_case。</summary>
    public static string EntryOf(System.Type type)
    {
        var name = type.Name;
        if (name.EndsWith("Card", System.StringComparison.Ordinal))
            name = name[..^"Card".Length];
        return ToSnake(name);
    }

    /// <summary>文件名 → 完整的 `res://` 路径。</summary>
    public static string PathOf(string entry) => Root + entry + ".png";

    private static string ToSnake(string name)
    {
        var sb = new System.Text.StringBuilder(name.Length + 4);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c) && i > 0 && name[i - 1] != '_') sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
