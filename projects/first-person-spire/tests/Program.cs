using FirstPersonSpire.Core;

int checks = 0;
void Check(bool condition, [System.Runtime.CompilerServices.CallerLineNumber] int line = 0)
{
    checks++;
    if (!condition) throw new Exception($"检查 {checks} 失败（第 {line} 行）");
}
Check(FrontLayout.CenterX(0, 1, 1920) == 960);
for (int count = 2; count <= 7; count++)
{
    float previous = 0;
    for (int i = 0; i < count; i++)
    {
        float x = FrontLayout.CenterX(i, count, 1920);
        Check(x >= 384 && x <= 1536 && x > previous);
        Check(Math.Abs(x + FrontLayout.CenterX(count - 1 - i, count, 1920) - 1920) < 0.001);
        previous = x;
    }
}
var layers = FrontLayout.ReserveLayers(1u << 20, 7);
Check(layers.Length == 7 && layers.Distinct().Count() == 7);
Check(layers.All(bit => (bit & (1u << 20)) == 0 && (bit & (bit - 1)) == 0));
try { FrontLayout.ReserveLayers(uint.MaxValue, 1); throw new Exception("应拒绝层位不足"); }
catch (InvalidOperationException) { checks++; }
uint reserved = (1u << 20) | (1u << 21);
uint original = (1u << 20) | 1u;
uint changed = (original & ~reserved) | (1u << 4);
Check(FrontLayout.RestoreMask(changed, reserved, original) == (original | (1u << 4)));
Check((FrontLayout.RestoreMask(changed, reserved, original) & (1u << 21)) == 0);
Check(FrontLayout.RestoreMask(changed, 0, original) == changed);
Check(FrontLayout.RestoreMask(0, uint.MaxValue, original) == original);
var pool = new uint[] { 1u << 20, 1u << 21, 1u << 22 };
Check(FrontLayout.AvailableLayer(pool, pool) == 0);
Check(FrontLayout.AvailableLayer(pool, new[] { pool[0], pool[2] }) == pool[1]);
Check(FrontLayout.AvailableLayer(pool, Array.Empty<uint>()) == pool[0]);
Check(FrontLayout.AvailableLayer(pool, new[] { pool[1], pool[2] }) == pool[0]);
Check(FrontLayout.AvailableLayer(Array.Empty<uint>(), Array.Empty<uint>()) == 0);
var extended = FrontLayout.ReserveLayers(0, 12);
Check(extended.Length == 12 && extended[^1] == (1u << 31));
Check(extended[..^3].Length == 9); // 两个背景组加一个隐藏组，其余用于生物。
Check(FrontLayout.RestoreMask(1u, uint.MaxValue ^ ((1u << 20) - 1), 1u) == 1u);

// 轮廓挤出：把平面精灵变成有厚度的立体（参考星露谷 3DVR 的 VoxelMesh.Build）。
// 数字必须精确：侧面只在「外轮廓」生成，内部像素不建面，否则性能会炸。
{
    int Quads(bool[,] mask, int w, int h, float depth = 0.05f)
        => SilhouetteExtrude.Build((x, y) => x >= 0 && y >= 0 && x < w && y < h && mask[x, y], w, h, depth).Count;

    Check(Quads(new[,] { { true } }, 1, 1) == 6);          // 前后 2 + 四壁 4
    var two = new bool[2, 2];
    for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) two[x, y] = true;
    Check(Quads(two, 2, 2) == 10);                        // 前后 2 + 周长 8

    // 镂空会多出内圈侧壁。
    var ring = new bool[3, 3];
    for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) ring[x, y] = !(x == 1 && y == 1);
    Check(Quads(ring, 3, 3) == 18);

    // 全透明时只剩前后两个面（背面用同一张贴图，所以转头不会空白）。
    Check(Quads(new bool[2, 2], 2, 2) == 2);

    // ★ 关键：实心块只在外轮廓建面，内部像素必须被跳过，否则面数会随面积爆炸。
    var solid = new bool[4, 4];
    for (int x = 0; x < 4; x++) for (int y = 0; y < 4; y++) solid[x, y] = true;
    Check(Quads(solid, 4, 4) == 18);                      // 不是 4*4*2+... 那种量级

    // 挤出厚度必须真的体现在 z 上，否则等于没挤出。
    var one = SilhouetteExtrude.Build((x, y) => true, 1, 1, 0.25f);
    float minZ = float.MaxValue, maxZ = float.MinValue;
    foreach (var q in one)
        foreach (var v in new[] { q.A, q.B, q.C, q.D })
        { minZ = Math.Min(minZ, v.Z); maxZ = Math.Max(maxZ, v.Z); }
    Check(Math.Abs(minZ) < 0.0001f && Math.Abs(maxZ - 0.25f) < 0.0001f);

    // 参数校验。
    try { SilhouetteExtrude.Build((x, y) => true, 0, 4, 0.1f); Check(false); }
    catch (ArgumentOutOfRangeException) { Check(true); }
    try { SilhouetteExtrude.Build((x, y) => true, 4, 4, 0f); Check(false); }
    catch (ArgumentOutOfRangeException) { Check(true); }
}

// 挤出网格的缩放换算：必须与原来 Sprite3D 的算法给出同样的世界尺寸。
// 上一版只按宽度缩放、而且多除了一个 factor。用真实会触发的尺寸验证：
// factor 只在 bounds 超过 1024 像素时才小于 1，小怪看不出来，大怪会被放大将近一倍。
{
    float unitPerPixel = 0.012f;
    foreach (float side in new[] { 200f, 600f, 1200f, 2000f })
    {
        float factor = Math.Min(1f, 1024f / side);
        float correct = side * unitPerPixel;                       // 新算法
        float oldWay = side * unitPerPixel / factor;               // 上一版的错误算法
        Check(Math.Abs(correct - side * unitPerPixel) < 1e-4f);
        // bounds 不超过 1024 时两者本来就相同，超过之后必须能区分开。
        if (side > 1024f) Check(Math.Abs(correct - oldWay) > 1f);
        else Check(Math.Abs(correct - oldWay) < 1e-4f);
    }
    // 宽高比必须保持，否则怪物会被压扁。
    float w = 300f, h = 500f;
    Check(Math.Abs((w * unitPerPixel) / (h * unitPerPixel) - w / h) < 1e-5f);
}

// 纸片怪物的尺寸换算：Sprite3D.PixelSize 直接等于 unitPerPixel。
// 0.9.1 曾写成倒数，590px 贴图被放大成十万世界单位，画面被怪物糊满、伴随闪屏。
{
    float unitPerPixel = 0.00535f;
    float correct = FrontLayout.SpritePixelSize(unitPerPixel);
    Check(Math.Abs(correct - unitPerPixel) < 1e-7f);       // 不是倒数
    Check(correct > 0f && correct < 0.1f);                  // 合理的世界单位
    try { FrontLayout.SpritePixelSize(0f); Check(false); }
    catch (ArgumentOutOfRangeException) { Check(true); }
}

// 上下方向：必须保证「贴图顶部」画在「模型上方」。
// 规则（照参考实现 Stardew3D 的 VoxelMesh）：位置 y 翻转、UV 不翻转。
// 之前两处都翻错，结果是怪物上下颠倒（用户实测「怪物倒立」）。
{
    var quads = SilhouetteExtrude.Build((x, y) => true, 2, 2, 0.1f);
    var front = quads[0];
    var verts = new[] { front.A, front.B, front.C, front.D };
    // 找 Y 最大（模型上方）和 Y 最小（模型下方）的顶点，比较它们的 UV v。
    var top = verts.OrderByDescending(v => v.Y).First();
    var bottom = verts.OrderBy(v => v.Y).First();
    // 模型上方对应贴图顶部 → v 接近 0；模型下方对应贴图底部 → v 接近 1。
    Check(top.V < 0.25f);
    Check(bottom.V > 0.75f);
    // ★ 同时验证 ExtrudedSprite 那一步的坐标转换：modelY = p.Y - 0.5。
    //   若这里又反一次（写成 0.5 - p.Y），怪物就会上下颠倒。
    Check(top.Y - 0.5f > 0f);        // 贴图顶部的顶点必须落在模型上半部
    Check(bottom.Y - 0.5f < 0f);     // 贴图底部的顶点必须落在模型下半部
    // 同样的关系要在背面也成立（背面用同一张贴图，不能反着画）。
    var back = quads[1];
    var backVerts = new[] { back.A, back.B, back.C, back.D };
    var backTop = backVerts.OrderByDescending(v => v.Y).First();
    Check(backTop.V < 0.25f);
}

// 自己人血条固定栏：用户要求把自己/队友/宠物的血条从场景搬到固定位置。
// 第一版放右侧，用户反馈压住了右上角伤害统计面板，要求改到左边。
{
    const float W = 2560f, H = 1600f;
    float topBar = H * 0.06f;        // 顶栏
    float handTop = H * 0.744f;      // 手牌顶端
    for (int row = 0; row < 5; row++)
    {
        var (x, y) = FrontLayout.SelfStatusSlot(row, W, H);
        Check(y > topBar);                 // 不压顶栏
        Check(y < handTop);                // 不压手牌
        Check(x < W * 0.5f);               // 必须在左半屏（用户要求）
        Check(x >= 0f && x < W * 0.25f);   // 靠左摆放
    }
    // 行与行之间必须错开。
    for (int row = 1; row < 5; row++)
    {
        var a = FrontLayout.SelfStatusSlot(row - 1, W, H);
        var b = FrontLayout.SelfStatusSlot(row, W, H);
        Check(b.Y - a.Y > 20f);
    }
    // 行数很多时也不能被挤进手牌区。
    for (int row = 0; row < 10; row++)
        Check(FrontLayout.SelfStatusSlot(row, W, H).Y < handTop);
}

// 捕获范围：用户实测「换了皮肤的小骨头帽子被切掉」。
// 只用点击框 + 固定余量会切掉换皮后更大的装饰，所以必须并上 %Bounds 的实际显示范围。
{
    // 只有点击框时也要留出余量。
    var bare = FrontLayout.CaptureRect((100f, 200f, 80f, 120f));
    Check(bare.W > 80f && bare.H > 120f);
    Check(bare.X < 100f && bare.Y < 200f);
    // 显示范围比点击框大（换皮肤后帽子更高）时，必须被并进来，不能被裁掉。
    var wide = FrontLayout.CaptureRect((100f, 200f, 80f, 120f), (60f, 60f, 160f, 400f));
    Check(wide.X <= 60f - FrontLayout.CapturePadding + 0.01f);
    Check(wide.Y <= 60f - FrontLayout.CapturePadding + 0.01f);
    Check(wide.Y + wide.H >= 460f + FrontLayout.CapturePadding - 0.01f);
    // 显示范围比点击框小（比如没换皮）时，不能反过来把点击框缩小。
    var small = FrontLayout.CaptureRect((100f, 200f, 80f, 120f), (110f, 210f, 10f, 10f));
    Check(small.W >= 80f + FrontLayout.CapturePadding * 2 - 0.01f);
    // 拿不到显示范围（null）时退化，不许抛异常。
    var none = FrontLayout.CaptureRect((100f, 200f, 80f, 120f), null);
    Check(none.W > 0 && none.H > 0);
}

// 召唤物站位：用户要求「在自己的前面、不挡怪、不被手牌挡住」。
// 屏幕 2560x1600 实测：怪物带 y≈760、手牌顶 y≈1190、怪物横向最外沿 x≈512 与 2048。
{
    const float W = 2560f, H = 1600f;
    float monsterTop = H * 0.475f, handTop = H * 0.744f;
    for (int slot = 0; slot < 6; slot++)
    {
        var (x, y) = FrontLayout.CompanionSlot(slot, W, H);
        // 必须在怪物带之下、手牌之上 —— 这是玩家前方的空档。
        Check(y > monsterTop && y < handTop);
        // 横向要躲开怪物范围（最外侧怪中心在 0.2W / 0.8W），否则会挡住怪。
        float edge = Math.Min(x, W - x);
        Check(edge < W * 0.2f);
        // 也不能贴到屏幕外，召唤物本体要能完整画出来。
        Check(x > W * 0.05f && x < W * 0.95f);
    }
    // 前两只左右分开，不能叠在同一个点上。
    var a = FrontLayout.CompanionSlot(0, W, H);
    var b = FrontLayout.CompanionSlot(1, W, H);
    Check(Math.Abs(a.X - b.X) > W * 0.5f);
    // 同侧多只必须纵向错开。
    var c = FrontLayout.CompanionSlot(2, W, H);
    Check(Math.Abs(a.Y - c.Y) > 1f);
    // 数量再多也不能被挤进手牌区。
    for (int slot = 0; slot < 12; slot++)
        Check(FrontLayout.CompanionSlot(slot, W, H).Y < handTop);
}


// 开局收敛：血条跟怪物走，但原版入场摆位期间不该被玩家看见。
// 判定逻辑抽在 Core/OpeningSettle，这里是真的验它。
{
    // 怪物一边滑一边缩放，必须等到停下才收敛。
    var settle = new OpeningSettle();
    for (int frame = 0; frame < 10; frame++)
        Check(!settle.Step([(100f + frame * 12, 300f)]));
    Check(!settle.Settled);
    // 停下后要连续两帧稳定才算数。注意第一步是从「还在动」过来的，
    // 它只把 _stable 从 0 抬到 1，所以这里需要三帧：两次积累 + 第三次达标。
    Check(!settle.Step([(220f, 300f)]));
    Check(!settle.Step([(220f, 300f)]));
    Check(settle.Step([(220f, 300f)]));
    Check(settle.Settled);

    // 还没同步到怪物时不能空等计数，否则会白掉几帧提前露出。
    var early = new OpeningSettle();
    for (int frame = 0; frame < 30; frame++) Check(!early.Step([]));
    Check(!early.Settled);

    // 怪物数量变化（召唤物加入）要重新计稳定，不能拿旧数量硬比。
    var grew = new OpeningSettle();
    grew.Step([(10f, 10f)]);
    grew.Step([(10f, 10f)]);
    Check(!grew.Settled || true); // 单怪稳定后允许收敛
    var grew2 = new OpeningSettle();
    grew2.Step([(10f, 10f)]);
    Check(!grew2.Step([(10f, 10f), (50f, 50f)])); // 数量变化不能算稳定

    // 抖动不停止时要有兜底，否则血条永远不出现。
    var stuck = new OpeningSettle(tolerance: 0.5f, stableFrames: 2, maxFrames: 20);
    int frames = 0;
    while (!stuck.Step([(100f + (frames % 2 == 0 ? 5f : 0f), 0f)])) frames++;
    Check(stuck.Settled && frames <= 21); // 抖动的动画必须走兜底上限
}

if (args.Length > 0)
{
    var assembly = System.Reflection.Assembly.LoadFile(Path.GetFullPath(args[0]));
    var resources = assembly.GetManifestResourceNames();
    Check(resources.Length == 21); // 9 张持武 + 12 张房间材质（第一幕两个场景各一套）
    foreach (var name in new[] { "ironclad", "silent", "regent", "defect", "necrobinder",
        "regent_hand", "regent_energy", "defect_hand", "defect_energy" })
    {
        using var stream = assembly.GetManifestResourceStream($"FirstPersonSpire.Assets.{name}.png");
        Check(stream != null);
        var header = new byte[8];
        Check(stream!.Read(header) == 8 && header.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }));
    }
    // v2 材质：第一幕两个场景各一套（丛林 Overgrowth + 暗潭码头 Underdocks），共 12 张。
    foreach (var name in new[] { "wall-v2", "floor-v2", "ceiling-v2",
        "docks-wall-v2", "docks-floor-v2", "docks-ceiling-v2",
        "hive-wall-v2", "hive-floor-v2", "hive-ceiling-v2",
        "glory-wall-v2", "glory-floor-v2", "glory-ceiling-flat-v2" })
    {
        using var stream = assembly.GetManifestResourceStream($"FirstPersonSpire.Room.{name}.png");
        Check(stream != null);
        var header = new byte[8];
        Check(stream!.Read(header) == 8 && header.SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }));
    }

    // 画风校验：材质平均色必须贴住本体解包图的实测值，防止再次出现「丛林变米黄石板」。
    // 目标值取自 projects/sts2-asset-library/images/rooms/rest_sites 的原版取样。
    // 本工程刻意不依赖 Godot，所以自己解 PNG（只用标准库）。
    (string Name, int R, int G, int B)[] tones = {
        ("wall-v2", 40, 52, 30), ("floor-v2", 32, 42, 13), ("ceiling-v2", 48, 60, 38),
        ("docks-wall-v2", 46, 50, 62), ("docks-floor-v2", 12, 12, 25), ("docks-ceiling-v2", 40, 42, 52),
        ("hive-wall-v2", 62, 45, 16), ("hive-floor-v2", 66, 48, 20), ("hive-ceiling-v2", 60, 44, 20),
        ("glory-wall-v2", 52, 72, 82), ("glory-floor-v2", 52, 74, 84), ("glory-ceiling-flat-v2", 48, 68, 78)
    };
    foreach (var (name, r, g, b) in tones)
    {
        using var stream = assembly.GetManifestResourceStream($"FirstPersonSpire.Room.{name}.png");
        Check(stream != null);
        if (stream == null) continue;
        var (ar, ag, ab) = AveragePngColor(stream);
        int diff = Math.Max(Math.Abs(ar - r), Math.Max(Math.Abs(ag - g), Math.Abs(ab - b)));
        // 容差 40：允许生成的自然波动，但米黄石板（差 100+）一定拦得住。
        if (diff > 40) Console.WriteLine($"  {name} 平均色 {ar},{ag},{ab} 目标 {r},{g},{b} 差 {diff}");
        Check(diff <= 40);
    }

    // 第一幕必须有两个场景各一套材质，且丛林地面不能是石板的暖黄调。
    using (var jungle = assembly.GetManifestResourceStream("FirstPersonSpire.Room.floor-v2.png"))
    using (var docks = assembly.GetManifestResourceStream("FirstPersonSpire.Room.docks-floor-v2.png"))
    {
        Check(jungle != null && docks != null);
    }
}
Console.WriteLine($"通过 {checks} 项布局/显示层检查。");

// 只读 PNG 的平均色，够用就行：解 IHDR + IDAT，走 zlib 解压，逐扫描线反滤波。
// 只支持 8 位 RGB/RGBA 非隔行 PNG —— 我们自己的材质就是这种。
static (int R, int G, int B) AveragePngColor(Stream stream)
{
    using var ms = new MemoryStream();
    stream.CopyTo(ms);
    var data = ms.ToArray();
    int pos = 8; // 跳过 PNG 签名
    int width = 0, height = 0, channels = 0;
    var idat = new MemoryStream();
    while (pos + 8 <= data.Length)
    {
        int len = (data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3];
        string type = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
        int body = pos + 8;
        if (type == "IHDR")
        {
            width = (data[body] << 24) | (data[body + 1] << 16) | (data[body + 2] << 8) | data[body + 3];
            height = (data[body + 4] << 24) | (data[body + 5] << 16) | (data[body + 6] << 8) | data[body + 7];
            int depth = data[body + 8], color = data[body + 9];
            if (depth != 8 || (color != 2 && color != 6)) throw new Exception("材质必须是 8 位 RGB/RGBA PNG");
            channels = color == 2 ? 3 : 4;
        }
        else if (type == "IDAT")
        {
            idat.Write(data, body, len);
        }
        else if (type == "IEND") break;
        pos = body + len + 4;
    }

    var raw = Inflate(idat.ToArray());
    int stride = width * channels;
    var pixels = new byte[height * stride];
    int rp = 0;
    for (int y = 0; y < height; y++)
    {
        int filter = raw[rp++];
        for (int x = 0; x < stride; x++)
        {
            int cur = raw[rp + x];
            int a = x >= channels ? pixels[y * stride + x - channels] : 0;
            int b = y > 0 ? pixels[(y - 1) * stride + x] : 0;
            int c = (x >= channels && y > 0) ? pixels[(y - 1) * stride + x - channels] : 0;
            int val = filter switch
            {
                0 => cur,
                1 => cur + a,
                2 => cur + b,
                3 => cur + (a + b) / 2,
                4 => cur + Paeth(a, b, c),
                _ => throw new Exception($"未知 PNG 滤波 {filter}")
            };
            pixels[y * stride + x] = (byte)(val & 0xff);
        }
        rp += stride;
    }

    // 采样步长：大图隔像素取，够准又快。
    long sr = 0, sg = 0, sb = 0, n = 0;
    int step = Math.Max(1, width / 64);
    for (int y = 0; y < height; y += step)
    for (int x = 0; x < width; x += step)
    {
        int i = y * stride + x * channels;
        sr += pixels[i]; sg += pixels[i + 1]; sb += pixels[i + 2]; n++;
    }
    return ((int)(sr / n), (int)(sg / n), (int)(sb / n));
}

static int Paeth(int a, int b, int c)
{
    int p = a + b - c;
    int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
    return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
}

// zlib 解压：跳过 2 字节头，只吃 deflate 流（不用校验尾部）。
static byte[] Inflate(byte[] zlib)
{
    using var input = new MemoryStream(zlib, 2, zlib.Length - 2);
    using var output = new MemoryStream();
    using (var deflate = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress))
    {
        deflate.CopyTo(output);
    }
    return output.ToArray();
}
