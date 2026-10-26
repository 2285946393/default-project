using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ParallaxSpire;

// 战斗场景视差：只移动原版背景的图层，不碰怪物、手牌、血条和特效容器。
// 图层越远（名字里的序号越小）偏移越小；前景反向加强，制造深度。
public partial class ParallaxController : Node
{
    private NCombatRoom _room = null!;
    private readonly List<(CanvasItem Item, Vector2 Original, float Factor)> _layers = new();
    private Vector2 _mouse;
    private bool _enabled = true;
    private int _layerCount = -1;

    private static Vector2 GetPosition(CanvasItem item) => item switch
    {
        Node2D node => node.Position,
        Control control => control.Position,
        _ => Vector2.Zero
    };

    private static void SetPosition(CanvasItem item, Vector2 value)
    {
        switch (item)
        {
            case Node2D node: node.Position = value; break;
            case Control control: control.Position = value; break;
        }
    }

    public override void _Ready()
    {
        _room = (NCombatRoom)GetParent();
        _enabled = Settings.Enabled;
        CollectLayers();
        SetProcess(_enabled);
    }

    public override void _Input(InputEvent input)
    {
        if (input is not InputEventKey { Pressed: true, Echo: false } key ||
            (key.Keycode != Key.F9 && key.PhysicalKeycode != Key.F9))
            return;
        _enabled = !_enabled;
        Settings.Enabled = _enabled;
        if (!_enabled) Restore();
        SetProcess(_enabled);
        GetViewport().SetInputAsHandled();
        GD.Print($"[ParallaxSpire] {(_enabled ? "开启" : "关闭")}视差。");
    }

    public override void _Process(double delta)
    {
        if (!_enabled || !IsValid(_room) || !_room.IsVisibleInTree()) return;
        var viewport = GetViewport();
        if (viewport == null) return;

        // 场景切换或随机背景重载后重新收集；原引用失效时自动恢复。
        int layerCount = _room.Background?.GetChildCount() ?? -1;
        if (layerCount != _layerCount)
        {
            Restore();
            CollectLayers();
            _layerCount = layerCount;
        }

        var size = viewport.GetVisibleRect().Size;
        if (size.X <= 1f || size.Y <= 1f) return;

        var mouse = viewport.GetMousePosition();
        _mouse = ParallaxMath.Smooth(_mouse, mouse, (float)delta);
        var normalized = ParallaxMath.Normalize(_mouse, size);

        foreach (var (item, original, factor) in _layers)
            if (GodotObject.IsInstanceValid(item))
                SetPosition(item, original + normalized * factor);
    }

    public override void _ExitTree() => Restore();

    private void CollectLayers()
    {
        _layers.Clear();
        if (!IsValid(_room.Background)) return;
        // 反编译证据：NCombatBackground.SetBackgroundLayers 按 00..N 顺序生成 Layer_00..；
        // 每个占位节点里才是真正的 NCombatBackgroundLayer 实例。我们要移动的是「占位节点」本身，
        // 不碰里面的 Visual / PhobiaModeVisual，避免破坏恐惧症模式的可见性切换。
        var named = _room.Background.GetChildren()
            .Where(n => n.Name.ToString().StartsWith("Layer_", StringComparison.Ordinal))
            .OfType<CanvasItem>()
            .OrderBy(n => n.Name.ToString(), StringComparer.Ordinal)
            .ToArray();
        for (int i = 0; i < named.Length; i++)
            _layers.Add((named[i], GetPosition(named[i]), ParallaxMath.LayerFactor(i, named.Length)));

        // 反编译证据：前景占位节点固定叫 Foreground；没有前景的场景直接跳过。
        foreach (var child in _room.Background.GetChildren())
            if (child.Name == "Foreground" && child is CanvasItem front)
                _layers.Add((front, GetPosition(front), ParallaxMath.ForegroundFactor));
    }

    private void Restore()
    {
        foreach (var (item, original, _) in _layers)
            if (GodotObject.IsInstanceValid(item)) SetPosition(item, original);
    }

    private static bool IsValid(GodotObject? node) =>
        node != null && GodotObject.IsInstanceValid(node);
}

// 只在本次游戏运行内记住开关状态，不写配置文件、不碰存档。
internal static class Settings
{
    internal static bool Enabled = true;
}

