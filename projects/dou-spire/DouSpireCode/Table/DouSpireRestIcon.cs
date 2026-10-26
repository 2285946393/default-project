using System;
using System.IO;

namespace DouSpire.Table;

/// <summary>
/// 篝火选项图标的**唯一挂载点** —— <c>assets/DouSpire.icon.pck</c>。
///
/// ── 为什么需要这么个东西 ────────────────────────────────────────────
/// 篝火选项的图标（<c>RestSiteOption.Icon</c>）**只认 <c>res://</c>**，而本 mod 是
/// <c>has_pck: false</c>，散在 <c>assets/</c> 里的图进不了 <c>res://</c>。
/// 所以以前篝火里那个「斗地主」只能显示成本体的「克隆」图标
/// （用户 2026-10-08 原话："现在显示的是克隆图标，你没改对"）。
///
/// 解法：自己额外挂一个**只装这两张图的小 pck**，于是
/// <c>res://images/ui/rest_site/option_dou_spire*.png</c> 就**真的存在**了。
///
/// ── 为什么**不**走 manifest 的 <c>has_pck</c> ────────────────────────
/// 反编译确认（<c>ModManager.cs:970-979</c>）：manifest 写 <c>has_pck: true</c> 时，
/// <c>ProjectSettings.LoadResourcePack(...)</c> 返回 false 会直接
/// <c>throw new InvalidOperationException</c>，外层 catch 把**整个 mod 标成 Failed**
/// （主菜单还弹一个错误条）。也就是说：pck 一旦读不了，损失的是整个 mod，不是一张图标。
/// 自己挂的话，失败只是"图标没换成"，mod 照常 —— 风险被关在我们自己的 try/catch 里。
///
/// ── 为什么要抽成共用 ────────────────────────────────────────────────
/// 2026-10-09 晚加了第二个篝火选项（德州扑克），它要用同一个包里的另一张图。
/// 挂两次同一个 pck 没必要，所以挂载这件事收口到这里，两个选项共用。
/// </summary>
internal static class DouSpireRestIcon
{
    /// <summary>
    /// 图标包的相对路径（相对 mod 的 <c>assets/</c>）。
    ///
    /// ⚠️ **为什么是这个位置**：csproj 的 <c>CopyMod</c> 会把 <c>assets\**</c> 原样拷进
    /// <c>mods\DouSpire\assets\</c>，工坊打包脚本也只带 <c>assets/</c> —— 放这儿，
    /// 本机部署和上架两件事**都不用额外操心**。
    /// 源头工程是 <c>pck-project/</c>（只含那两张 png，用 Godot 4.5.1 的
    /// <c>--export-pack "BasicExport"</c> 导出；**导出前必须先 <c>--import</c>**）。
    /// </summary>
    public const string PackAssetPath = "DouSpire.icon.pck";

    private static bool _tried;
    private static bool _mounted;

    /// <summary>
    /// 把图标包挂进游戏的虚拟文件系统（**由我们自己挂，不走 manifest 的 has_pck**）。
    /// 幂等；挂过就记住结果。返回"现在包是不是在虚拟文件系统里"。
    /// </summary>
    public static bool EnsureMounted()
    {
        if (_mounted) return true;
        if (_tried) return false;
        _tried = true;

        try
        {
            var root = DouSpireAssets.RootDir;
            if (string.IsNullOrEmpty(root))
            {
                Entry.Logger.Info("斗地主：还不知道 mod 目录在哪，图标包先不挂（下次开桌再试）。");
                _tried = false;               // 目录还没就绪 ≠ 没有，下次再来
                return false;
            }

            var pck = (root + "/" + PackAssetPath).Replace('\\', '/');
            if (!File.Exists(pck))
            {
                // 正常情况之一：工坊上还没带这个包 / 被人删了。安静退回本体图标。
                Entry.Logger.Info($"斗地主：没有图标包（{pck}），篝火图标用本体那张顶一下。");
                return false;
            }

            // replaceFiles=false：游戏自己的文件永远优先，我们这个包只**新增**路径。
            _mounted = Godot.ProjectSettings.LoadResourcePack(pck, false);
            Entry.Logger.Info($"斗地主：图标包挂载{(_mounted ? "成功" : "失败")}：{pck}");
            return _mounted;
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"斗地主：挂载图标包失败（篝火图标用本体那张顶一下）: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 某个 <c>res://</c> 路径**现在**能不能真的加载（会先确保图标包已挂）。
    ///
    /// ⚠️ 这个"先验再给"是必须的，不是洁癖 —— 2026-10-08 踩过一个连环雷：
    /// 给了一个 <c>res://</c> 里并不存在的路径 → 游戏某次异步预加载真去读文件 →
    /// 失败 → <c>AssetCache</c> 把这条路径**永久标记成"加载失败"** →
    /// 选项的图标、标题全被退回成本体的「挖掘 / Dig」，而且按钮半途中断、**点进去卡死**。
    /// 一个假路径连环引发三个症状。详见 <see cref="DouSpireRestSiteOption.ResolveIconPath"/>。
    /// </summary>
    public static bool Exists(string resPath)
    {
        try
        {
            EnsureMounted();
            return Godot.ResourceLoader.Exists(resPath);
        }
        catch (Exception e)
        {
            Entry.Logger.Warn($"斗地主：检查 res:// 资源是否存在时出错（按不存在处理）: {e.Message}");
            return false;
        }
    }
}
