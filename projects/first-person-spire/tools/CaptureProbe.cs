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
        foreach (var item in new Godot.Collections.Array<Node> { root, drawn })
            if (item is CanvasItem ci) ci.VisibilityLayer = Layer20;

        // 捕获视口：只认第 20 层
        var capture = new SubViewport
        {
            Size = new Vector2I(200, 200), World2D = source.FindWorld2D(),
            Disable3D = true, TransparentBg = true, GuiDisableInput = true,
            CanvasCullMask = Layer20, RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        AddChild(capture);

        // 主视口把第 20 层剔掉（模拟主画面不再显示它）
        source.CanvasCullMask &= ~Layer20;

        // 等两帧再取样，确保渲染完成
        CallDeferred(nameof(Sample), capture);
    }

    private void Sample(SubViewport capture)
    {
        var image = capture.GetTexture().GetImage();
        int opaque = 0;
        for (int y = 0; y < image.GetHeight(); y++)
            for (int x = 0; x < image.GetWidth(); x++)
                if (image.GetPixel(x, y).A > 0.1f) opaque++;
        GD.Print($"捕获探针：尺寸={image.GetSize()} 不透明={opaque}（期望 >0）");
        GetTree().Quit(opaque > 0 ? 0 : 1);
    }
}
