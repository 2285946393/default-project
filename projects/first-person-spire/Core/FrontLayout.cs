namespace FirstPersonSpire.Core;

// 无游戏依赖，方便离线检查布局和层位分配。
public static class FrontLayout
{
    public static uint AvailableLayer(IEnumerable<uint> reserved, IEnumerable<uint> used) =>
        reserved.FirstOrDefault(bit => !used.Contains(bit));

    // 只恢复本模组借用的层，保留其他模组对无关层的改动。
    public static uint RestoreMask(uint current, uint reserved, uint original) =>
        (current & ~reserved) | (original & reserved);

    public static float CenterX(int index, int count, float width)
    {
        if (count < 1 || index < 0 || index >= count || width <= 0)
            throw new ArgumentOutOfRangeException();
        return count == 1 ? width * 0.5f : width * (0.2f + 0.6f * index / (count - 1));
    }

    // 召唤物站位：用户要求「在自己的前面、不挡怪、不被手牌挡住」。
    // 屏幕纵向分成三段：怪物带（约 0.475）、玩家前方空档（0.55~0.74）、手牌区（0.744 起）。
    // 召唤物是「为主而死」的，所以放在玩家前方偏下、左右贴近两侧，避开怪物横向范围与手牌。
    private const float CompanionSideX = 0.155f;   // 躲开怪物最外沿的 0.2
    private const float CompanionTop = 0.55f;      // 怪物带之下
    private const float CompanionRowGap = 0.075f;  // 每多一只往下让一点
    private const int CompanionColumns = 2;        // 左右各一列

    public static (float X, float Y) CompanionSlot(int slot, float width, float height)
    {
        if (slot < 0) throw new ArgumentOutOfRangeException(nameof(slot));
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        int column = slot % CompanionColumns;
        int row = slot / CompanionColumns;
        float x = column == 0 ? width * CompanionSideX : width * (1f - CompanionSideX);
        float y = height * Math.Min(CompanionTop + row * CompanionRowGap, 0.70f);
        return (x, y);
    }

    // 自己人血条固定区：用户要求把自己/队友/宠物的血条从场景里搬到固定位置。
    // 迭代记录：右侧会压右上角伤害面板 → 改左侧；左上又压到遗物图标 → 继续下移。
    // 遗物图标在顶栏下方约 0.055~0.135 这一带，所以血条起点必须让开它。
    private const float SelfBarLeft = 0.035f;    // 左边距
    private const float SelfBarTop = 0.155f;     // 让开顶栏和遗物图标
    private const float SelfBarRowStep = 0.055f; // 每行间距
    private const float SelfBarMaxY = 0.58f;     // 不能压到手牌区

    public static (float X, float Y) SelfStatusSlot(int index, float width, float height)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        float y = height * Math.Min(SelfBarTop + index * SelfBarRowStep, SelfBarMaxY);
        return (width * SelfBarLeft, y);
    }

    // 捕获范围：不能只看点击框。
    // 本体 NCreature.UpdateBounds 是从 %Bounds 算出 Hitbox 的，而 %Bounds 会把该生物的
    // 显示容器（含换皮肤后更大的帽子、装饰）都包进去 —— 所以「实际显示范围」要取两者的并集。
    // 再留一圈固定余量，覆盖挂在容器外沿的特效（能力图标、格挡火花等）。
    // 之前只用 hitbox.Grow(96)，换皮后帽子就被切掉了。
    public const float CapturePadding = 160f;

    public static (float X, float Y, float W, float H) CaptureRect(
        (float X, float Y, float W, float H) hitbox,
        (float X, float Y, float W, float H)? displayBounds = null)
    {
        if (hitbox.W <= 0 || hitbox.H <= 0) throw new ArgumentOutOfRangeException(nameof(hitbox));
        float minX = hitbox.X, minY = hitbox.Y;
        float maxX = hitbox.X + hitbox.W, maxY = hitbox.Y + hitbox.H;
        if (displayBounds is { } d && d.W > 0 && d.H > 0)
        {
            minX = Math.Min(minX, d.X);
            minY = Math.Min(minY, d.Y);
            maxX = Math.Max(maxX, d.X + d.W);
            maxY = Math.Max(maxY, d.Y + d.H);
        }
        minX -= CapturePadding;
        minY -= CapturePadding;
        maxX += CapturePadding;
        maxY += CapturePadding;
        return (minX, minY, maxX - minX, maxY - minY);
    }

    // 纸片怪物的尺寸换算：PixelSize = 画布 1 像素对应的世界单位。
    // 这个纯函数用于离线检查，防止再把「倒数」当成「倍率」。
    public static float SpritePixelSize(float unitPerPixel)
    {
        if (unitPerPixel <= 0f || !float.IsFinite(unitPerPixel))
            throw new ArgumentOutOfRangeException(nameof(unitPerPixel));
        return unitPerPixel;
    }

    public static uint[] ReserveLayers(uint occupied, int count)
    {
        if (count < 1 || count > 12) throw new ArgumentOutOfRangeException(nameof(count));
        var free = Enumerable.Range(20, 12).Select(bit => 1u << bit)
            .Where(bit => (occupied & bit) == 0).Take(count).ToArray();
        if (free.Length != count)
            throw new InvalidOperationException("没有足够的空闲展示层；保持原视角。");
        return free;
    }
}
