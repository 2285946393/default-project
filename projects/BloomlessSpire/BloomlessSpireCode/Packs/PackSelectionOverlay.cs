using Godot;
using BloomlessSpire.Packs;
using static BloomlessSpire.Entry;

namespace BloomlessSpire.Packs;

// 卡包选择浮层：创建一个 Control 节点，显示 3 个卡包选项。
// 不依赖 .tscn 文件，纯 C# 动态创建。
public static class PackSelectionOverlay
{
    private static Control? _overlay;

    public static Control CreatePanel()
    {
        var candidates = PackSelectionManager.GetCandidatePacks(3);
        if (candidates.Length == 0)
            return new Control();

        // 全屏半透明遮罩
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var bg = new ColorRect();
        bg.Color = new Color(0, 0, 0, 0.75f);
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        panel.AddChild(bg);

        // 垂直布局
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(Control.LayoutPreset.Center);
        vbox.OffsetLeft = -250;
        vbox.OffsetRight = 250;
        vbox.OffsetTop = -200;
        vbox.OffsetBottom = 200;
        vbox.AddThemeConstantOverride("separation", 16);
        panel.AddChild(vbox);

        // 标题
        var title = new Label();
        title.Text = "  选择一个卡包  ";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 26);
        vbox.AddChild(title);

        // 卡包按钮
        foreach (var pack in candidates)
        {
            var btn = new Button();
            btn.Text = GetPackDisplayName(pack.Id);
            btn.CustomMinimumSize = new Vector2(350, 55);
            btn.AddThemeFontSizeOverride("font_size", 18);

            var packId = pack.Id;
            btn.Pressed += () =>
            {
                var selected = PackSelectionManager.AllPacks.First(p => p.Id == packId);
                PackSelectionManager.SelectPack(selected);
                Entry.Logger.Info($"Pack selected: {packId} ({GetPackDisplayName(packId)})");
                Hide();
            };

            vbox.AddChild(btn);
        }

        _overlay = panel;
        Entry.Logger.Info($"Pack selection overlay created with {candidates.Length} options");
        return panel;
    }

    public static void Hide()
    {
        if (_overlay == null) return;
        _overlay.GetParent()?.RemoveChild(_overlay);
        _overlay.QueueFree();
        _overlay = null;
    }

    private static string GetPackDisplayName(string packId) => packId switch
    {
        "aggression" => "  进攻包  ",
        "poison" => "  毒药包  ",
        "claw" => "  爪击包  ",
        _ => packId
    };
}
