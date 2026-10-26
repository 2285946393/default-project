using Godot;

namespace FrostSpire.UI;

/// <summary>
/// 常驻小节点：**素材热更新热键**（照斗地主 DouSpireHotkey 的写法）。
///
/// 按 <see cref="ReloadKey"/> 时：
/// <list type="number">
///   <item>清掉 <see cref="FrostAssets"/> 的贴图/字体缓存</item>
///   <item>拆掉旧波次面板、按新素材重建</item>
/// </list>
/// 于是美术把图丢进 <c>mods\FrostSpire\assets\</c> 后**按一下键就能看到效果**，
/// 不用重编译 dll、不用重启游戏。
///
/// 用 <see cref="Node._Input"/> 而不是 <c>_GuiInput</c>：前者是**最先**拿到输入的地方，
/// 哪怕全屏面板盖住了界面也一定收得到（斗地主那条也是这么干的）。
/// </summary>
public sealed partial class FrostHotkey : Node
{
    private static FrostHotkey? _instance;

    /// <summary>素材热更新热键。避开斗地主的 F9，也避开游戏自己的快捷键。</summary>
    public const Key ReloadKey = Key.F8;

    /// <summary>幂等安装：已经挂上就什么都不做。</summary>
    public static void Install()
    {
        if (_instance != null && GodotObject.IsInstanceValid(_instance)) return;
        var root = FrostUi.Root;
        if (root == null) return;
        _instance = new FrostHotkey { Name = "FrostSpireHotkey" };
        root.AddChild(_instance);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key) return;
        if (!key.Pressed || key.Echo) return;
        if (key.Keycode != ReloadKey) return;

        try
        {
            FrostPanel.Reload();
            Entry.Logger.Info("凛冬素材已重载（F8）—— 缓存已清、面板已按新素材重建。");
            Entry.Toast("素材已重载", "凛冬尖塔");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"素材重载失败（不影响玩）: {e.Message}");
        }

        GetViewport()?.SetInputAsHandled();
    }
}
