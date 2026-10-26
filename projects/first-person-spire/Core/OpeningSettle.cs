namespace FirstPersonSpire.Core;

// 开局收敛判定：血条必须跟怪物走，但原版怪物入场会摆位，
// 血条忠实跟随就会被玩家看见「一边滑一边追」。
// 这里只做纯判定，方便离线检查，不依赖 Godot。
public sealed class OpeningSettle
{
    private readonly float _tolerance;
    private readonly int _stableFrames;
    private readonly int _maxFrames;
    private readonly List<(float X, float Y)> _last = new();
    private int _stable;
    private int _frames;

    // tolerance：相邻帧位移小于它就算「停住了」，单位是原画布像素。
    // stableFrames：要连续几帧停住才算摆好。
    // maxFrames：兜底上限，防止某些动画一直抖导致血条永不出现。
    public OpeningSettle(float tolerance = 1.5f, int stableFrames = 2, int maxFrames = 60)
    {
        if (tolerance <= 0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        if (stableFrames < 1) throw new ArgumentOutOfRangeException(nameof(stableFrames));
        if (maxFrames < stableFrames) throw new ArgumentOutOfRangeException(nameof(maxFrames));
        _tolerance = tolerance;
        _stableFrames = stableFrames;
        _maxFrames = maxFrames;
    }

    public bool Settled { get; private set; }

    // 每帧喂入所有怪物的锚点。数量为 0 时不计帧（怪物还没同步进来）。
    public bool Step(IReadOnlyList<(float X, float Y)> anchors)
    {
        if (Settled) return true;
        if (anchors.Count == 0) return false;

        _frames++;
        var stable = true;
        if (_last.Count != anchors.Count)
        {
            stable = false;
        }
        else
        {
            for (int i = 0; i < anchors.Count; i++)
            {
                float dx = anchors[i].X - _last[i].X;
                float dy = anchors[i].Y - _last[i].Y;
                if (MathF.Sqrt(dx * dx + dy * dy) > _tolerance) stable = false;
            }
        }
        _last.Clear();
        _last.AddRange(anchors);

        _stable = stable ? _stable + 1 : 0;
        if (_stable >= _stableFrames || _frames > _maxFrames) Settled = true;
        return Settled;
    }
}
