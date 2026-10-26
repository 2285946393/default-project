using Godot;

namespace FirstPersonSpire;

// 只借用点击框和界面的位置，绝不移动 NCreature 或 Visuals。
// 原版动画/变身若写入了新位置或大小，以新值为准，关闭时不恢复过时快照。
internal sealed class UiProjection(Control control)
{
    private Vector2 _rawPosition;
    private Vector2 _rawSize;
    private Vector2 _appliedPosition;
    private Vector2 _appliedSize;
    private bool _applied;

    internal void Restore()
    {
        if (!_applied || !GodotObject.IsInstanceValid(control)) return;
        if (control.Position.IsEqualApprox(_appliedPosition)) control.Position = _rawPosition;
        if (control.Size.IsEqualApprox(_appliedSize)) control.Size = _rawSize;
        _applied = false;
    }

    private void Begin()
    {
        _rawPosition = control.Position;
        _rawSize = control.Size;
    }

    private void End()
    {
        _appliedPosition = control.Position;
        _appliedSize = control.Size;
        _applied = true;
    }

    internal void Shift(Vector2 screenDelta)
    {
        Begin();
        control.GlobalPosition += control.GetCanvasTransform().AffineInverse().BasisXform(screenDelta);
        End();
    }

    // 把控件整体挪到屏幕上的指定左上角，保持自身尺寸不变。
    // 血条这类控件的 Size 由原版按当前血量计算，改了会破坏原版布局，所以不能走 Fit。
    internal void MoveTo(Vector2 screenTopLeft)
    {
        Begin();
        var parentTransform = control.GetGlobalTransformWithCanvas() *
            control.GetTransform().AffineInverse();
        control.Position = parentTransform.AffineInverse() * screenTopLeft;
        End();
    }

    internal void Fit(Rect2 screenRect)
    {
        Begin();
        // 点击框自身的位置不能参与父坐标反变换。
        var parentTransform = control.GetGlobalTransformWithCanvas() *
            control.GetTransform().AffineInverse();
        var inverse = parentTransform.AffineInverse();
        var points = new[] { screenRect.Position, screenRect.End,
            screenRect.Position + new Vector2(screenRect.Size.X, 0),
            screenRect.Position + new Vector2(0, screenRect.Size.Y) }.Select(p => inverse * p).ToArray();
        var min = points.Aggregate(Vector2.Inf, (a, b) => a.Min(b));
        var max = points.Aggregate(-Vector2.Inf, (a, b) => a.Max(b));
        control.Position = min;
        control.Size = max - min;
        End();
    }
}
