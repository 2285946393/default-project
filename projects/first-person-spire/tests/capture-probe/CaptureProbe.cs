// 独立探针：验证「给节点设 VisibilityLayer 后，SubViewport 能不能拍到它」。
// 这是连续多轮「前方全黑」的可疑环节，必须单独确证。
using Godot;

public partial class CaptureProbe : Node
{
    public override void _Ready()
    {
        var source = new SubViewport { Size = new Vector2I(200, 200) };
        AddChild(source);
        source.World2D = new World2D();

        // 一个红色方块，放在第 20 层（1<<20）
        var root = new Node2D();
        source.AddChild(root);
        var drawn = new ColorRect
        {
            Color = new Color(1, 0, 0, 1), Position = new Vector2(50, 50), Size = new Vector2(100, 100)
        };
        root.AddChild(drawn);
        const uint Layer20 = 1u << 20;
        // 试验：只给叶子节点设层，祖先保持默认（对照之前「祖先也设层」的做法）
        // ★ 结论：子孙设层之外，祖先必须「或上」同一层，否则按位与后为 0，谁都看不见。
        drawn.VisibilityLayer = Layer20;
        root.VisibilityLayer |= Layer20;
        GD.Print($"探针：root.Layer={root.VisibilityLayer} drawn.Layer={drawn.VisibilityLayer}");

        // 捕获视口：只认第 20 层
        var capture = new SubViewport
        {
            Size = new Vector2I(200, 200), World2D = source.FindWorld2D(),
            TransparentBg = true, GuiDisableInput = true,
            CanvasCullMask = Layer20, RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        AddChild(capture);
        // 主视口剔除第 20 层（真实场景里主画面不再显示被捕获的东西）
        GD.Print($"探针：source.CanvasCullMask={source.CanvasCullMask} capture.CullMask={capture.CanvasCullMask}");

        // 主视口把第 20 层剔掉（模拟主画面不再显示它）

        _capture = capture;
        // 用 _Process 每帧取样，给渲染留足时间
        SetProcess(true);
    }

    private int _waited;
    private SubViewport? _capture;

    public override void _Process(double delta)
    {
        if (_capture == null) return;
        _waited++;
        var tex = _capture.GetTexture();
        var img = tex?.GetImage();
        int op = 0;
        if (img != null)
            for (int y = 0; y < img.GetHeight(); y += 4)
                for (int x = 0; x < img.GetWidth(); x += 4)
                    if (img.GetPixel(x, y).A > 0.1f) op++;
        if (_waited % 10 == 0) GD.Print($"探针 第{_waited}帧 不透明={op}");
        if (op > 0 || _waited > 60)
        {
            GD.Print($"捕获探针：第{_waited}帧 不透明={op}（期望 >0）");
            GetTree().Quit(op > 0 ? 0 : 1);
        }
    }

    private void Sample(SubViewport capture)
    {
        // headless 下没有渲染结果，等几帧再取；仍取不到就如实报告。
        var texture = capture.GetTexture();
        if (texture == null)
        {
            if (++_waited < 5) { CallDeferred(nameof(Sample), capture); return; }
            GD.Print("捕获探针：GetTexture 返回 null（headless 无渲染结果）");
            GetTree().Quit(2);
            return;
        }
        var image = texture.GetImage();
        if (image == null)
        {
            if (++_waited < 5) { CallDeferred(nameof(Sample), capture); return; }
            GD.Print("捕获探针：GetImage 返回 null");
            GetTree().Quit(2);
            return;
        }
        int opaque = 0;
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
                if (image.GetPixel(x, y).A > 0.1f) opaque++;
        GD.Print($"捕获探针：尺寸={image.GetSize()} 不透明={opaque}（期望 >0）");
        GetTree().Quit(opaque > 0 ? 0 : 1);
    }
}
