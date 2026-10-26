using Godot;

namespace FirstPersonSpire;

// 原背景远近层分别捕获；免费生成的材质补房间外壳，颜色随原画变化。
internal sealed class RoomPresentation : IDisposable
{
    private readonly SubViewport _farCapture;
    private readonly SubViewport _nearCapture;
    private readonly Viewport _source;
    private readonly Node3D _room;
    private readonly MeshInstance3D _far;
    private readonly MeshInstance3D _near;
    private readonly MeshInstance3D _left;
    private readonly MeshInstance3D _right;
    private readonly MeshInstance3D _floor;
    private readonly MeshInstance3D _ceiling;
    private readonly MeshInstance3D _backWall;
    private readonly Texture2D _wallTexture;
    private readonly Texture2D _floorTexture;
    private readonly Texture2D _ceilingTexture;
    private Vector2I _size;

    internal RoomPresentation(Node owner, SubViewport stage, Viewport source, uint farLayer, uint nearLayer,
        string act = "Overgrowth")
    {
        // v2 材质：第一幕两个场景各一套。Overgrowth 丛林用墙/地/顶，
        // Underdocks 暗潭码头用它自己的湿木与水面那套（v1 曾错误回退到石墙）。
        string prefix = act switch
        {
            "Hive" => "hive-",
            "Glory" => "glory-",
            "Underdocks" => "docks-",
            _ => ""
        };
        _wallTexture = LoadTexture($"{prefix}wall-v2");
        _floorTexture = LoadTexture($"{prefix}floor-v2");
        // 第三幕顶部沿用去透视的平铺版，避免出现消失点。
        _ceilingTexture = LoadTexture(act == "Glory" ? "glory-ceiling-flat-v2" : $"{prefix}ceiling-v2");
        GD.Print($"[FirstPersonSpire] 房间材质：{act}。");
        _source = source;
        _farCapture = Capture(owner, farLayer, false, "FirstPersonFarBackground");
        _nearCapture = Capture(owner, nearLayer, true, "FirstPersonNearBackground");
        _room = new Node3D { Name = "FirstPersonRoom" };
        stage.AddChild(_room);
        _far = AddSurface("FarScenery", new StandardMaterial3D
        {
            AlbedoTexture = _farCapture.GetTexture(),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear
        });
        _near = AddSurface("NearScenery", new StandardMaterial3D
        {
            AlbedoTexture = _nearCapture.GetTexture(),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear
        });
        _left = AddSurface("LeftWall", ToneMaterial(0.8f, 0));
        _right = AddSurface("RightWall", ToneMaterial(0.8f, 1));
        _floor = AddSurface("Floor", ToneMaterial(0.9f, 2));
        _ceiling = AddSurface("Ceiling", ToneMaterial(0.65f, 3));
        // 后墙用和侧墙同一套材质，转头看背后时不会是空白。
        _backWall = AddSurface("BackWall", ToneMaterial(0.75f, 0));
        Update();
    }

    private SubViewport Capture(Node owner, uint layer, bool transparent, string name)
    {
        var capture = new SubViewport
        {
            Name = name, World2D = _source.FindWorld2D(), Disable3D = true,
            TransparentBg = transparent, GuiDisableInput = true, CanvasCullMask = layer,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        owner.AddChild(capture);
        return capture;
    }

    private ShaderMaterial ToneMaterial(float brightness, int region)
    {
        var material = new ShaderMaterial
        {
            Shader = new Shader { Code = """
                shader_type spatial;
                render_mode unshaded, cull_disabled;
                uniform sampler2D palette : source_color, filter_linear;
                uniform sampler2D surface_texture : source_color, filter_linear_mipmap;
                uniform float brightness = 0.8;
                uniform int region = 0;
                // 镜像往返采样，不出现重复贴图的硬接缝。
                float mirror_uv(float value) {
                    return 1.0 - abs(mod(value, 2.0) - 1.0);
                }
                void fragment() {
                    vec2 repeats = region == 2 ? vec2(3.0, 3.0) : vec2(2.0, 1.0);
                    vec2 sample_uv = vec2(mirror_uv(UV.x * repeats.x),
                        mirror_uv(UV.y * repeats.y));
                    if (region == 1) sample_uv.x = 1.0 - sample_uv.x;
                    vec3 detail = texture(surface_texture, sample_uv).rgb;
                    vec3 tone = (texture(palette, vec2(0.25,0.6)).rgb
                        + texture(palette, vec2(0.5,0.6)).rgb
                        + texture(palette, vec2(0.75,0.6)).rgb) / 3.0;
                    // 原画很暗或采到空白时，材质仍保持可见，不变成黑墙。
                    float luminance = dot(tone, vec3(0.2126,0.7152,0.0722));
                    vec3 tint = luminance < 0.025 ? vec3(1.0)
                        : clamp(tone / max(luminance, 0.025), vec3(0.65), vec3(1.4));
                    ALBEDO = detail * mix(vec3(1.0), tint, 0.65) * brightness
                        * mix(0.75, 1.0, UV.y);
                }
                """ }
        };
        material.SetShaderParameter("palette", _farCapture.GetTexture());
        material.SetShaderParameter("surface_texture",
            region == 2 ? _floorTexture : region == 3 ? _ceilingTexture : _wallTexture);
        material.SetShaderParameter("brightness", brightness);
        material.SetShaderParameter("region", region);
        return material;
    }

    private static Texture2D LoadTexture(string asset)
    {
        using var stream = typeof(RoomPresentation).Assembly
            .GetManifestResourceStream($"FirstPersonSpire.Room.{asset}.png")
            ?? throw new InvalidOperationException($"房间纹理未打包：{asset}");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        using var image = new Image();
        if (image.LoadPngFromBuffer(bytes.ToArray()) != Error.Ok)
            throw new InvalidOperationException($"房间纹理读取失败：{asset}");
        image.GenerateMipmaps();
        return ImageTexture.CreateFromImage(image);
    }

    private MeshInstance3D AddSurface(string name, Material material)
    {
        var mesh = new MeshInstance3D
        {
            Name = name, Mesh = new QuadMesh(), MaterialOverride = material
        };
        _room.AddChild(mesh);
        return mesh;
    }

    private static void Place(MeshInstance3D mesh, Vector2 size, Vector3 position, Vector3 rotation)
    {
        ((QuadMesh)mesh.Mesh).Size = size;
        mesh.Position = position;
        mesh.Rotation = rotation;
    }

    private static void Shell(MeshInstance3D mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        var vertices = new[] { a, b, c, a, c, d };
        var uvs = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1),
            new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 1) };
        for (int i = 0; i < vertices.Length; i++)
        {
            surface.SetUV(uvs[i]);
            surface.AddVertex(vertices[i]);
        }
        surface.GenerateNormals();
        mesh.Mesh = surface.Commit();
        surface.Dispose();
    }

    internal void Update()
    {
        var size = _source.GetVisibleRect().Size;
        var pixels = new Vector2I(Math.Max(2, (int)size.X), Math.Max(2, (int)size.Y));
        foreach (var capture in new[] { _farCapture, _nearCapture })
        {
            capture.Size = pixels;
            capture.CanvasTransform = _source.CanvasTransform;
            // 与人物捕获使用相同坐标空间，最终由宿主统一应用 GlobalCanvasTransform。
            capture.GlobalCanvasTransform = Transform2D.Identity;
        }
        if (_size == pixels) return;
        _size = pixels;
        float height = 2f * 12f * MathF.Tan(Mathf.DegToRad(60) / 2f) * 1.16f;
        float width = height * pixels.X / pixels.Y;
        // 留出转头余量；远近完整画布同构图，但转头产生不同位移。
        Place(_far, new Vector2(width, height), new Vector3(0, 1.3f, -12), Vector3.Zero);
        Place(_near, new Vector2(width, height) * (8f / 12f), new Vector3(0, 1.3f, -8), Vector3.Zero);

        // ★ 用户要求「转头能看到背后」，参考饥荒 Pickle's First Person Mod 的做法：
        //   它把背景元素摆在以玩家为圆心的大圈上（太阳 z+40 / 月亮 z-40 / 云 ±40），
        //   这样任何朝向都有东西可看，而不是面前一块大图。
        //
        // 之前的错误：外壳做得太小（front=4.5 米），相机转 180° 时后墙距离只剩 2 米，
        // 会直接糊满整个屏幕。改成「以相机为中心的大空间」，各面到相机的距离与前方景物相当。
        float radius = width * 0.5f;                    // 用视野宽度的一半当作半径
        float side = Math.Max(radius, 12f);             // 侧墙离相机的距离
        float backZ = 14f;                              // 后墙：与前方景物 z=-12 对称
        float top = 1.3f + height * 0.4f;               // 顶棚高度
        float roof = top + height * 0.2f;
        float floorY = -1f;
        // 侧墙：从后墙一直延伸到相机前方，全程贴着视野边缘。
        Shell(_left, new(-side, floorY, backZ), new(-side, floorY, -12),
            new(-side, top, -12), new(-side, roof, backZ));
        Shell(_right, new(side, floorY, -12), new(side, floorY, backZ),
            new(side, roof, backZ), new(side, top, -12));
        Shell(_floor, new(-side, floorY, backZ), new(side, floorY, backZ),
            new(side, floorY, -12), new(-side, floorY, -12));
        Shell(_ceiling, new(-side, roof, -12), new(side, roof, -12),
            new(side, roof, backZ), new(-side, roof, backZ));
        // 后墙：相机背后。转头 180° 时看到的就是这一面。
        Shell(_backWall, new(side, floorY, backZ), new(-side, floorY, backZ),
            new(-side, roof, backZ), new(side, roof, backZ));
    }

    public void Dispose()
    {
        foreach (var capture in new[] { _farCapture, _nearCapture })
        {
            if (!GodotObject.IsInstanceValid(capture)) continue;
            capture.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
            capture.QueueFree();
        }
        if (GodotObject.IsInstanceValid(_room)) _room.QueueFree();
    }
}
