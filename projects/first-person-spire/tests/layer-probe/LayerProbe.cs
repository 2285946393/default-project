// 独立探针：复现「手牌数据正常却看不见」的关键差异。
// 上一版只测了普通父节点，结论是 UI z=200 一定可见；但游戏里仍看不见。
// 这里补上更接近事实的三个条件：
//   1) 手牌/CombatUi 挂在 CanvasLayer 下（游戏 UI 很常见）；
//   2) 展示层是 TextureRect.StretchMode=Scale 的实际实现；
//   3) 展示层自带一个 CanvasItemMaterial 材质。
// 如果这组会把手牌盖住，就说明问题不在 ZIndex，而在 TextureRect 的绘制范围/材质状态。
using Godot;

public partial class LayerProbe : Node
{
    private int _frame;
    private int _case;
    private readonly (bool UseTexture, bool UiOnCanvasLayer, int UiZ, int OverlayZ, bool ExpectVisible)[] _cases =
    [
        // 基线：普通父节点 + 纯色屏（上一版已验证）
        (false, false, 0, -8, true),
        // 反证：展示层压过 UI（机制有效性）
        (false, false, 0, 8, false),
        // 新增：UI 在 CanvasLayer 下，用真实 TextureRect+Scale
        (true, true, 200, -8, true),
    ];

    public override void _Ready() => Build();

    private void Build()
    {
        foreach (var child in GetChildren()) child.QueueFree();
        var (useTexture, uiOnCanvasLayer, uiZ, overlayZ, _) = _cases[_case];

        var room = new Node2D { Name = "Room" };
        AddChild(room);

        Node uiParent = room;
        if (uiOnCanvasLayer)
        {
            var layer = new CanvasLayer { Name = "UiCanvasLayer", Layer = 1 };
            AddChild(layer);
            uiParent = layer;
        }

        var ui = new Control { Name = "CombatUi", Size = new Vector2(1920, 1080), ZIndex = uiZ };
        uiParent.AddChild(ui);
        var hand = new ColorRect
        {
            Name = "Hand", Color = new Color(1, 0, 0, 1),
            Position = new Vector2(700, 900), Size = new Vector2(500, 120)
        };
        ui.AddChild(hand);

        var overlay = new Node2D { Name = "FirstPersonOverlay", ZIndex = overlayZ };
        room.AddChild(overlay);

        // 和生产代码一致：TextureRect + StretchMode.Scale + Ignore 鼠标。
        // 无渲染环境（headless dummy renderer）拿不到 ViewportTexture，所以只在非 headless 用；
        // headless 时退回 ColorRect，至少还能验证节点链。
        CanvasItem screen;
        if (useTexture && RenderingServer.GetRenderingDevice() != null)
        {
            var stage = new SubViewport
            {
                Name = "Stage", Size = new Vector2I(1920, 1080), TransparentBg = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always
            };
            AddChild(stage);
            var stageBackground = new ColorRect
            {
                Color = new Color(0, 1, 0, 1), Size = new Vector2(1920, 1080)
            };
            stage.AddChild(stageBackground);
            screen = new TextureRect
            {
                Name = "FirstPersonScreen", Texture = stage.GetTexture(),
                Size = new Vector2(1920, 1080),
                StretchMode = TextureRect.StretchModeEnum.Scale,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                ZIndex = 0,
                Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Mix }
            };
        }
        else
        {
            screen = new ColorRect
            {
                Name = "FirstPersonScreen", Color = new Color(0, 1, 0, 1),
                Size = new Vector2(1920, 1080)
            };
        }
        overlay.AddChild(screen);

        GD.Print($"探针 第{_case + 1}组：UI在CanvasLayer={uiOnCanvasLayer} " +
            $"UI.ZIndex={uiZ} 展示层.ZIndex={overlayZ} TextureRect={useTexture}");
        _frame = 0;
    }

    public override void _Process(double delta)
    {
        if (++_frame < 10) return;
        var image = GetViewport().GetTexture().GetImage();
        if (image == null)
        {
            GD.PushWarning("探针：headless 模式拿不到截图，无法判断颜色；改用节点链检查。");
            var (_, uiOnLayer, uiZ, overlayZ, expect) = _cases[_case];
            var hand = FindChild("Hand", true, false);
            var screenNode = FindChild("FirstPersonScreen", true, false);
            bool chainOk = hand != null && screenNode != null &&
                hand.GetViewport() == screenNode.GetViewport() &&
                uiOnLayer && uiZ > overlayZ;
            GD.Print($"探针 第{_case + 1}组 链路={(chainOk ? "符合预期" : "★不符预期")} " +
                $"期望={(expect ? "可见" : "被盖住")}");
            if (++_case >= _cases.Length) { GetTree().Quit(0); return; }
            Build();
            return;
        }
        var pixel = image.GetPixel(950, 960);
        bool visible = pixel.R > 0.5f && pixel.G < 0.5f;
        var (_, _, _, _, expect2) = _cases[_case];
        string verdict = visible == expect2 ? "符合预期" : "★不符预期";
        GD.Print($"探针 第{_case + 1}组 像素={pixel} 手牌={(visible ? "可见" : "被盖住")} " +
            $"期望={(expect2 ? "可见" : "被盖住")} → {verdict}");

        if (++_case >= _cases.Length) { GD.Print("探针：三组测试完成"); GetTree().Quit(0); return; }
        Build();
        _frame = 0;
    }
}
