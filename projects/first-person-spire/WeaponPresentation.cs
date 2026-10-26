using Godot;

namespace FirstPersonSpire;

// 用户的图已经包含双手和第一人称构图；作为视角固定图层，不再重复生成第二把刀。
internal sealed class WeaponPresentation : IDisposable
{
    private readonly TextureRect _sprite;
    private readonly TextureRect? _energy;
    private readonly string _character;
    private float _swing;
    private float _energyTime = -1;
    private Vector2 _castStart;
    private Vector2 _castEnd;
    private Vector2 _viewportSize;
    private Vector2 _aim = new(0.5f, 0.45f);
    private readonly Vector2 _energyCenter;
    private readonly Shader _avoidShader = new()
    {
        Code = """
            shader_type canvas_item;
            uniform vec4 protected_rects[12];
            uniform int protected_count = 0;
            uniform vec2 origin;
            uniform vec2 pivot;
            uniform float angle = 0.0;
            varying vec2 local_point;
            void vertex() { local_point = VERTEX; }
            void fragment() {
                vec2 p = local_point-pivot;
                p = mat2(vec2(cos(angle),sin(angle)),vec2(-sin(angle),cos(angle))) * p + pivot + origin;
                float cover = 0.0;
                for (int i=0; i<12; i++) {
                    if (i >= protected_count) break;
                    vec4 r = protected_rects[i];
                    vec2 d = min(p-r.xy, r.xy+r.zw-p);
                    cover = max(cover, smoothstep(0.0, 12.0, min(d.x,d.y)));
                }
                // fragment 的 COLOR 已包含纹理与节点颜色，只修改遮挡透明度。
                COLOR.a *= 1.0-cover;
            }
            """
    };

    internal WeaponPresentation(Node parent, string character)
    {
        _character = character;
        var asset = character switch
        {
            "Silent" => "silent",
            "Regent" => "regent",
            "Defect" => "defect",
            "Necrobinder" => "necrobinder",
            _ => "ironclad"
        };
        bool casting = character is "Regent" or "Defect";
        _sprite = CreateSprite(parent, casting ? $"{asset}_hand" : asset, "FirstPersonWeapon");
        if (casting)
        {
            _energy = CreateSprite(parent, $"{asset}_energy", "FirstPersonEnergy");
            _energyCenter = character == "Defect" ? new Vector2(0.736f, 0.586f) : new Vector2(0.786f, 0.557f);
        }
        GD.Print($"[FirstPersonSpire] 用户武器已加载：{character}/{asset}。");
    }

    private TextureRect CreateSprite(Node parent, string asset, string name)
    {
        using var stream = typeof(WeaponPresentation).Assembly
            .GetManifestResourceStream($"FirstPersonSpire.Assets.{asset}.png")
            ?? throw new InvalidOperationException($"角色武器图片未打包：{asset}");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        using var image = new Image();
        if (image.LoadPngFromBuffer(bytes.ToArray()) != Error.Ok)
            throw new InvalidOperationException($"角色武器图片读取失败：{asset}");
        var sprite = new TextureRect
        {
            Name = name,
            Texture = ImageTexture.CreateFromImage(image),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
            ZIndex = 1,
            Material = new ShaderMaterial { Shader = _avoidShader }
        };
        parent.AddChild(sprite);
        return sprite;
    }

    internal void Attack()
    {
        _swing = 1f;
        if (_energy == null || _viewportSize == Vector2.Zero) return;
        _energyTime = 0;
        _castStart = (_energy.Position + _energy.Size * _energyCenter) / _viewportSize;
        _castEnd = _aim;
    }

    internal void Update(Vector2 viewportSize, double delta, IReadOnlyList<Rect2> protectedTargets, Vector2 mouse)
    {
        _viewportSize = viewportSize;
        _aim = (mouse / viewportSize).Clamp(new Vector2(0.2f, 0.25f), new Vector2(0.8f, 0.6f));
        _swing = Math.Max(0, _swing - (float)delta * 3f);
        float pulse = MathF.Sin(_swing * MathF.PI);
        // 长剑、镰刀缩小更多，靠屏幕右下；双刀保持左右对称。
        float heightRatio = _character is "Ironclad" or "Necrobinder" ? 0.52f : 0.68f;
        float scale = Math.Min(viewportSize.X / _sprite.Texture.GetWidth(),
            viewportSize.Y * heightRatio / _sprite.Texture.GetHeight());
        _sprite.Size = _sprite.Texture.GetSize() * scale;
        _sprite.PivotOffset = _sprite.Size *
            (_character == "Silent" ? new Vector2(0.5f, 0.92f) : new Vector2(0.9f, 0.9f));
        _sprite.Position = new Vector2(
            _character == "Silent" ? (viewportSize.X - _sprite.Size.X) * 0.5f :
                viewportSize.X - _sprite.Size.X,
            viewportSize.Y - _sprite.Size.Y + viewportSize.Y * 0.04f);
        bool casting = _character is "Regent" or "Defect";
        _sprite.Rotation = casting ? 0 : pulse * (_character == "Necrobinder" ? -0.32f : -0.24f);
        _sprite.Position += new Vector2(casting ? 0 : -pulse * viewportSize.X * 0.02f,
            -pulse * viewportSize.Y * (casting ? 0.035f : 0.02f));
        Avoid(_sprite, protectedTargets);
        if (_energy != null)
        {
            _energy.Size = _sprite.Size;
            _energy.Position = _sprite.Position;
            _energy.Rotation = 0;
            _energy.Modulate = Colors.White;
            if (_energyTime >= 0)
            {
                _energyTime += (float)delta;
                float t = Math.Clamp(_energyTime / 0.42f, 0, 1);
                _energy.Size = _sprite.Size * Mathf.Lerp(1, 0.3f, t);
                var center = _castStart.Lerp(_castEnd, t) * viewportSize;
                _energy.Position = center - _energy.Size * _energyCenter;
                _energy.Modulate = new Color(1, 1, 1, 1 - t);
                if (t >= 1) _energyTime = -1;
            }
            Avoid(_energy, protectedTargets);
        }
    }

    private static void Avoid(TextureRect sprite, IReadOnlyList<Rect2> protectedTargets)
    {
        var rects = new Vector4[12];
        for (int i = 0; i < Math.Min(rects.Length, protectedTargets.Count); i++)
        {
            var rect = protectedTargets[i];
            rects[i] = new Vector4(rect.Position.X, rect.Position.Y, rect.Size.X, rect.Size.Y);
        }
        var material = (ShaderMaterial)sprite.Material;
        material.SetShaderParameter("protected_rects", rects);
        material.SetShaderParameter("protected_count", Math.Min(rects.Length, protectedTargets.Count));
        material.SetShaderParameter("origin", sprite.Position);
        material.SetShaderParameter("pivot", sprite.PivotOffset);
        material.SetShaderParameter("angle", sprite.Rotation);
    }

    public void Dispose()
    {
        if (GodotObject.IsInstanceValid(_sprite))
        {
            _sprite.Visible = false;
            _sprite.QueueFree();
        }
        if (GodotObject.IsInstanceValid(_energy))
        {
            _energy!.Visible = false;
            _energy.QueueFree();
        }
    }
}
