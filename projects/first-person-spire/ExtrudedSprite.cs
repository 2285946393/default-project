using Godot;
using FirstPersonSpire.Core;

namespace FirstPersonSpire;

// 把「捕获到的平面动画贴图」建成有厚度的立体网格。
//
// 参考星露谷 3DVR（GingasVR）的 VoxelMesh.Build：逐像素扫轮廓、在边缘竖起侧壁。
// 好处是任何角度看都是立体的，不像单片平面转到侧面就变成一条线。
//
// ★ 性能铁律（踩过坑，游戏因此闪退）：
//   Godot 的 Image.GetPixel 是逐次跨引擎边界的调用，极慢。
//   早先版本在同一帧里把整张图扫了五遍（判空、计数、比内容、挤出、诊断统计），
//   96×96 的图 × 3 只怪 × 5 遍 ≈ 每帧十几万次调用，直接把游戏拖崩。
//   现在**只读一次**像素数组，之后所有判断都在数组上做。
internal sealed class ExtrudedSprite
{
    // 扫描用的分辨率上限：轮廓精度与开销的折中。
    private const int MaxScanPixels = 96;
    // 挤出厚度（相对精灵宽度的比例）。太小等于没挤，太大像纸板。
    private const float DepthRatio = 0.18f;
    // 每帧最多重建几个网格。
    private const int RebuildBudget = 4;

    private readonly MeshInstance3D _mesh;
    private readonly StandardMaterial3D _material;
    private byte[]? _lastAlpha;
    private int _lastWidth;
    private int _lastHeight;
    private int _scale;

    internal Node3D Node => _mesh;

    // 是否已经成功建过一次网格。没建过时不能被重建预算卡住，否则永远是空的。
    internal bool HasBuilt => _lastAlpha != null;

    internal ExtrudedSprite(Node parent, Texture2D texture)
    {
        _material = new StandardMaterial3D
        {
            AlbedoTexture = texture,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            // ★ 必须用 Back（正常的背面剔除），不能用 Disabled。
            //   挤出网格的前面和背面都画了完整贴图；若两面都渲染，
            //   从正面看会同时看到「前面的怪」和「稍远的背面的怪」，
            //   因为透视投影让它们略微错开 → 看起来就是两只重叠（用户实测「为什么会出现第二个」）。
            //   绕序已用叉积验证：前面法线朝 +z（朝相机）、背面朝 -z、侧壁朝外，全部正确。
            //   用 Back 之后：正对相机的那一面才画，背面自动隐藏；转到背后看时同理。
            CullMode = BaseMaterial3D.CullModeEnum.Back,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
            // ★ 必须用 AlphaScissor（alpha 测试），不能用 Alpha（半透明混合）。
            //   挤出网格有几百个三角形；半透明物体不写深度缓冲，
            //   三角形渲染顺序随机 → 互相穿透，看起来是一堆白色碎块（用户实测）。
            //   AlphaScissor 让不透明像素正常写深度、透明像素直接丢弃，排序问题消失。
            //   代价是边缘略硬，对这种「纸片怪物」完全可以接受。
            Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor,
            AlphaScissorThreshold = 0.35f
        };
        _mesh = new MeshInstance3D { Name = "Extruded", MaterialOverride = _material };
        parent.AddChild(_mesh);
    }

    // 挤出网格按 0..1 归一化建模，所以 Scale 直接等于「该范围在游戏世界里的实际尺寸」。
    // ⚠️ 必须宽高分开给：捕获贴图带着自己的宽高比，用同一个值会把怪物压扁。
    internal void SetSize(float worldWidth, float worldHeight)
    {
        if (!float.IsFinite(worldWidth) || !float.IsFinite(worldHeight)) return;
        if (worldWidth <= 0 || worldHeight <= 0) return;
        _mesh.Scale = new Vector3(worldWidth, worldHeight, Math.Min(worldWidth, worldHeight));
    }

    internal void SetPosition(Vector3 position) => _mesh.Position = position;

    // 从捕获视口取当前帧，必要时重建网格。
    internal void Refresh(SubViewport capture, bool allowRebuild)
    {
        RefreshCalls++;
        var texture = capture.GetTexture();
        if (texture == null) { LastSampleInfo = "GetTexture 返回 null"; return; }
        var image = texture.GetImage();
        if (image == null) { LastSampleInfo = "GetImage 返回 null"; return; }

        // 缩小到扫描分辨率，然后**一次性**把 alpha 读进数组（全流程只读这一遍）。
        int longest = Math.Max(image.GetWidth(), image.GetHeight());
        int scale = Math.Max(1, longest / MaxScanPixels);
        if (scale > 1)
            image.Resize(Math.Max(1, image.GetWidth() / scale),
                Math.Max(1, image.GetHeight() / scale));
        int w = image.GetWidth(), h = image.GetHeight();
        var alpha = ReadAlpha(image, w, h);

        int opaque = 0;
        for (int i = 0; i < alpha.Length; i++) if (alpha[i] >= 26) opaque++;
        LastSampleInfo = $"尺寸={w}x{h} 不透明={opaque} scale={scale} allow={allowRebuild}";

        // ★ 空图绝不能当有效帧缓存：开局头几帧捕获是空的，
        //   若那时就建网格并记进缓存，之后每帧都会因「内容没变」直接返回，网格永远是空的。
        if (opaque == 0) { SkippedBlank++; return; }

        if (_lastAlpha != null && _scale == scale && _lastWidth == w && _lastHeight == h &&
            SameAlpha(alpha, _lastAlpha))
        { SkippedSame++; return; }

        if (!allowRebuild && _lastAlpha == null) { SkippedBudget++; return; }

        _scale = scale;
        _lastWidth = w;
        _lastHeight = h;
        _lastAlpha = alpha;
        Rebuild(alpha, w, h, opaque);
    }

    // 一次性把 alpha 读进字节数组。这是全流程唯一一次逐像素跨引擎访问。
    private static byte[] ReadAlpha(Image image, int w, int h)
    {
        var alpha = new byte[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                alpha[y * w + x] = (byte)Math.Clamp((int)(image.GetPixel(x, y).A * 255f), 0, 255);
        return alpha;
    }

    // 在数组上比较，抽样即可。
    private static bool SameAlpha(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        int step = Math.Max(1, a.Length / 256);
        for (int i = 0; i < a.Length; i += step)
            if (Math.Abs(a[i] - b[i]) > 16) return false;
        return true;
    }

    // 诊断用。
    internal static string LastBuildInfo = "尚未重建";
    internal static string LastSampleInfo = "尚未采样";
    internal static int RefreshCalls;
    internal static int SkippedBudget;
    internal static int SkippedSame;
    internal static int SkippedBlank;
    internal static int Budget => RebuildBudget;

    private void Rebuild(byte[] alpha, int w, int h, int opaque)
    {
        var quads = SilhouetteExtrude.Build(
            (x, y) => x >= 0 && y >= 0 && x < w && y < h
                && alpha[y * w + x] >= SilhouetteExtrude.AlphaThreshold,
            w, h, DepthRatio);
        LastBuildInfo = $"扫描 {w}x{h} 不透明像素 {opaque}/{w * h} 四边形 {quads.Count}";
        if (quads.Count == 0) return;

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        var vertices = new Vector3[quads.Count * 4];
        var normals = new Vector3[quads.Count * 4];
        var uvs = new Vector2[quads.Count * 4];
        var indices = new int[quads.Count * 6];
        int vi = 0, ii = 0;
        foreach (var quad in quads)
        {
            var pts = new[] { quad.A, quad.B, quad.C, quad.D };
            for (int k = 0; k < 4; k++)
            {
                var p = pts[k];
                // ★ 坐标与 UV 的翻转规则（照参考实现 Stardew3D 的 VoxelMesh 对齐）：
                //   位置 y 【翻转】—— 图像顶部(j=0) 对应模型上方；
                //   UV  【不翻转】—— 图像顶部 对应 v≈0。
                //   之前写成「位置 y 不翻转 + UV 翻一次」，两次一错就成了上下颠倒
                //   （用户实测「怪物倒立」）。
                vertices[vi] = new Vector3(p.X - 0.5f, p.Y - 0.5f, -p.Z);
                normals[vi] = Vector3.Back;
                uvs[vi] = new Vector2(p.U, p.V);
                vi++;
            }
            int b = vi - 4;
            indices[ii++] = b; indices[ii++] = b + 1; indices[ii++] = b + 2;
            indices[ii++] = b; indices[ii++] = b + 2; indices[ii++] = b + 3;
        }
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.TexUV] = uvs;
        arrays[(int)Mesh.ArrayType.Index] = indices;
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        _mesh.Mesh = mesh;
    }

    internal void Dispose()
    {
        if (GodotObject.IsInstanceValid(_mesh)) _mesh.QueueFree();
    }
}
