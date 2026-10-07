// ============================================================================
//  LooseAsset —— 免 pck 资源加载
//  来源：反编译自「More Ironclad Animations」(ShieldOnly) v1.9.57
//
//  这个 mod 没有 pck！贴图/JSON/曲线全是散装文件放在 dll 旁边，运行时从磁盘读。
//  好处：
//    · 不用装 Godot 编辑器、不用导出 pck、不用等构建
//    · 改一张 PNG 直接生效；改一行 GLSL 直接生效（着色器是 C# 字符串）
//    · 调一个粒子参数只改 JSON，不用重编译
//  代价：自己写加载、自己缓存、自己保证"找不到文件时优雅失败"。
//
//  配套：着色器也可以不落文件，直接 new Shader { Code = "..." }（见 InlineShader）
// ============================================================================

using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using Godot;

namespace YourMod.Scripts.Vfx;

public static class LooseAsset
{
    // ------------------------------------------------------------------
    //  1. 我在哪 —— mod 自己的目录
    //
    //  dll 就躺在 mod 目录里，所以拿自己的 Assembly.Location 就能定位到
    //  同目录的贴图 / JSON / 子文件夹。
    // ------------------------------------------------------------------
    public static string ModDirectory { get; } =
        Path.GetDirectoryName(typeof(LooseAsset).Assembly.Location) ?? ".";

    public static string PathOf(params string[] parts)
        => Path.Combine(ModDirectory, Path.Combine(parts));

    // ------------------------------------------------------------------
    //  2. 读贴图（★ 核心：绕过 Godot 的 res:// 导入系统）
    // ------------------------------------------------------------------

    /// <summary>
    /// 从磁盘读一张 PNG/JPG 变成 Texture2D。找不到 / 读坏了 → 返回 null，不抛异常。
    /// ⚠️ Image 是 IDisposable，必须 Dispose；ImageTexture 不用（它把数据拷进 GPU 了）。
    /// </summary>
    public static Texture2D? TryLoadTexture(string relativePath, bool smooth = true)
    {
        var full = Path.IsPathRooted(relativePath) ? relativePath : PathOf(relativePath);
        Image? img = null;
        try
        {
            if (!File.Exists(full))
            {
                ModEntry.Log($"[LooseAsset] 贴图不存在：{full}");
                return null;
            }

            img = Image.LoadFromFile(full);
            if (img == null || img.IsEmpty())
            {
                ModEntry.Log($"[LooseAsset] 贴图是空的：{full}");
                return null;
            }

            var tex = ImageTexture.CreateFromImage(img);
            // 想让像素风不糊，改的是 CanvasItem 的 texture_filter，不是 Image：
            //   sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
            return tex;
        }
        catch (Exception ex)
        {
            ModEntry.Log($"[LooseAsset] 读贴图失败 {full}：{ex.Message}");
            return null;
        }
        finally
        {
            img?.Dispose();          // ★ 一定要 Dispose
        }
    }

    // ------------------------------------------------------------------
    //  3. 带缓存的版本（特效反复播，别每次都读盘）
    // ------------------------------------------------------------------
    private static readonly ConcurrentDictionary<string, Texture2D> _texCache = new();

    /// <summary>
    /// ⚠️ 只在读成功时才进缓存 —— 读失败就每次都重试，
    /// 这样开发期补上文件之后不用重启游戏。
    /// </summary>
    public static Texture2D? GetTexture(string relativePath)
    {
        if (_texCache.TryGetValue(relativePath, out var hit)) return hit;

        var tex = TryLoadTexture(relativePath);
        if (tex != null) _texCache[relativePath] = tex;
        return tex;
    }

    /// <summary>换场景 / 换 mod 版本时清一下</summary>
    public static void ClearCache() => _texCache.Clear();

    // ------------------------------------------------------------------
    //  4. ★ RuntimeAsset：4 字节 magic 决定要不要 Brotli 解压
    //
    //  同一个路径两种形态，代码一行都不用改：
    //    开发期 → 明文 JSON（可读、可 diff、可手改）
    //    发布期 → 4 字节 magic + Brotli 压缩（体积小、用户改不动）
    //
    //  JSON 用 "JMZ1"，二进制曲线用 "CAZ1"（各自约定，别混用）
    // ------------------------------------------------------------------
    private static readonly byte[] JsonMagic = "JMZ1"u8.ToArray();

    /// <summary>打开一个资源流。头 4 字节 == magic 就当 Brotli 解压，否则当明文。</summary>
    public static Stream Open(string path, byte[] marker, out bool compact)
    {
        var fs = File.OpenRead(path);
        try
        {
            Span<byte> head = stackalloc byte[4];
            fs.ReadExactly(head);
            compact = head.SequenceEqual(marker);
            if (compact) return new BrotliStream(fs, CompressionMode.Decompress);
            fs.Position = 0L;
            return fs;
        }
        catch
        {
            fs.Dispose();
            throw;
        }
    }

    /// <summary>读 JSON（自动处理 Brotli 形态）。失败返回 null，不抛。</summary>
    public static JsonDocument? TryReadJson(string relativePath)
    {
        var full = Path.IsPathRooted(relativePath) ? relativePath : PathOf(relativePath);
        try
        {
            if (!File.Exists(full)) { ModEntry.Log($"[LooseAsset] JSON 不存在：{full}"); return null; }
            using var s = Open(full, JsonMagic, out _);
            return JsonDocument.Parse(s);            // 交给调用方 Dispose
        }
        catch (Exception ex)
        {
            ModEntry.Log($"[LooseAsset] 读 JSON 失败 {full}：{ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 把明文文件压成 Brotli 形态（开发期跑一次，发布用）。
    /// 用法：LooseAsset.Compact("blood/plans.blood", LooseAsset.JsonMagic);
    /// </summary>
    public static void Compact(string relativePath, byte[] magic)
    {
        var full = Path.IsPathRooted(relativePath) ? relativePath : PathOf(relativePath);
        var raw = File.ReadAllBytes(full);

        // 已经是压缩形态就别压第二次
        if (raw.Length >= 4 && raw.AsSpan(0, 4).SequenceEqual(magic))
        {
            ModEntry.Log($"[LooseAsset] 已是压缩形态，跳过：{relativePath}");
            return;
        }

        using var outFs = File.Create(full + ".compact");
        outFs.Write(magic);
        using (var br = new BrotliStream(outFs, CompressionLevel.Optimal, leaveOpen: true))
            br.Write(raw);

        ModEntry.Log($"[LooseAsset] 压缩完成 → {relativePath}.compact " +
                     $"({raw.Length} → {new FileInfo(full + ".compact").Length} 字节)");
    }

    // ------------------------------------------------------------------
    //  5. 着色器写成 C# 字符串（不用 .gdshader 文件，热改）
    // ------------------------------------------------------------------
    public static Shader InlineShader(string glsl) => new() { Code = glsl };

    /// <summary>最常用的一个：只取原图 alpha、颜色刷白 —— 辉光/剪影/受击闪白都用它</summary>
    public static readonly string ShaderWhiteFromAlpha = """
        shader_type canvas_item;
        render_mode unshaded;
        varying vec4 vertex_tint;
        void vertex() { vertex_tint = COLOR; }
        void fragment() { COLOR = vec4(vec3(1.0), texture(TEXTURE, UV).a * vertex_tint.a); }
        """;

    /// <summary>纯黑剪影（挖洞 / 遮挡层）</summary>
    public static readonly string ShaderSolidBlack = """
        shader_type canvas_item;
        void fragment() { COLOR = vec4(0.0); }
        """;

    /// <summary>屏幕空间扭曲 —— 不需要 WorldEnvironment（读已渲染的画面）</summary>
    public static readonly string ShaderScreenDistort = """
        shader_type canvas_item;
        render_mode unshaded;
        uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
        uniform float strength = 0.01;
        uniform float speed = 2.0;
        void fragment() {
            vec2 uv = SCREEN_UV;
            uv.x += sin(uv.y * 40.0 + TIME * speed) * strength;
            COLOR = texture(screen_texture, uv);
        }
        """;

    // ------------------------------------------------------------------
    //  6. 可选依赖：运行时加载同目录的另一个 dll
    //
    //  用来做"装了设置库就有设置界面，没装也能跑"这种可选依赖。
    //  三要素：版本守卫 + try/catch + 失败降级。
    // ------------------------------------------------------------------
    public static bool TryLoadOptional(
        string dllFileName, string typeName, string methodName,
        Version minVersion, Version actualVersion)
    {
        try
        {
            if (actualVersion < minVersion)
            {
                ModEntry.Log($"[LooseAsset] 可选依赖需要 {minVersion}+，当前 {actualVersion}，跳过");
                return false;
            }

            var me = typeof(LooseAsset).Assembly;
            var path = Path.Combine(Path.GetDirectoryName(me.Location)!, dllFileName);

            var asm = System.Runtime.Loader.AssemblyLoadContext
                          .GetLoadContext(me)!
                          .LoadFromAssemblyPath(path);

            asm.GetType(typeName, throwOnError: true)!
               .GetMethod(methodName, BindingFlags.Static | BindingFlags.Public)!
               .Invoke(null, null);

            ModEntry.Log($"[LooseAsset] 可选依赖已挂载：{dllFileName}");
            return true;
        }
        catch (Exception ex)
        {
            // 不崩：用不了就用不了，退回默认行为
            ModEntry.Log($"[LooseAsset] 可选依赖不可用（{dllFileName}）：{ex.GetBaseException().Message}");
            return false;
        }
    }
}
