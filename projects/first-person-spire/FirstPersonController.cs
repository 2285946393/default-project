using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace FirstPersonSpire;

public partial class FirstPersonController : Node
{
    private NCombatRoom _room = null!;
    private PresentationSession? _session;
    private CanvasLayer _controls = null!;
    private PanelContainer _panel = null!;
    private Button _toggle = null!;
    private Label _status = null!;
    private static bool _preferFirstPerson;
    private bool _reportedUpdateError;
    private bool _autoStartPending;

    public override void _Ready()
    {
        _room = (NCombatRoom)GetParent();
        _controls = new CanvasLayer { Name = "FirstPersonControls", Layer = 10 };
        AddChild(_controls);
        _panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Pass };
        _controls.AddChild(_panel);
        var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Pass };
        _panel.AddChild(column);
        _toggle = new Button
        {
            Text = "开启第一人称 [F8]",
            CustomMinimumSize = new Vector2(250, 42),
            FocusMode = Control.FocusModeEnum.None
        };
        column.AddChild(_toggle);
        _status = new Label
        {
            Text = "实验版 · 鼠标上方转头，手牌区回中",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(250, 40)
        };
        column.AddChild(_status);
        _toggle.Pressed += () => Toggle("按钮");
        UpdateControls();
        GD.Print("[FirstPersonSpire] 控制器就绪；按钮与 F8 输入已启用。");
        _autoStartPending = _preferFirstPerson;
    }

    public override void _Input(InputEvent input)
    {
        if (input is not InputEventKey { Pressed: true, Echo: false } key ||
            (key.Keycode != Key.F8 && key.PhysicalKeycode != Key.F8))
            return;
        Toggle("F8");
    }

    private void Toggle(string source)
    {
        _autoStartPending = false;
        GD.Print($"[FirstPersonSpire] 收到开关请求：{source}。");
        // 被地图、奖励或设置界面遮住时不切换，避免抢其他界面的操作。
        if (!CanToggle())
        {
            GD.Print($"[FirstPersonSpire] 未切换：战斗可见={_room.IsVisibleInTree()}，" +
                $"当前界面={ActiveScreenContext.Instance.GetCurrentScreen()?.GetType().Name ?? "无"}；请回到战斗。");
            return;
        }
        GetViewport().SetInputAsHandled();
        if (_session != null)
        {
            _preferFirstPerson = false;
            Stop();
            _status.Text = "已恢复原视角";
            GD.Print("[FirstPersonSpire] 已关闭并恢复原视角。");
            return;
        }
        try
        {
            _session = new PresentationSession(_room, this);
            _session.Start();
            _preferFirstPerson = true;
            _reportedUpdateError = false;
            _toggle.Text = "恢复原视角 [F8]";
            _status.Text = "已开启 · 鼠标上方转头，手牌区回中";
            GD.Print("[FirstPersonSpire] 展示已开启。");
        }
        catch (Exception error)
        {
            Stop();
            _status.Text = $"开启失败：{error.Message}";
            GD.PushError($"FirstPersonSpire 开启失败，已尝试恢复：{error}");
        }
    }

    public override void _Process(double delta)
    {
        UpdateControls();
        try
        {
            if (_autoStartPending && CanToggle() &&
                _room.CreatureNodes.Any(c => GodotObject.IsInstanceValid(c) && c.Entity?.IsEnemy == true))
                Toggle("保持上次视角");
            if (_session == null) return;
            _session.Update(delta);
            if (_reportedUpdateError)
            {
                _reportedUpdateError = false;
                _status.Text = "已开启 · 鼠标上方转头，手牌区回中";
            }
        }
        catch (Exception error)
        {
            // 更新失败不代替玩家关闭视角；保留当前展示，下一帧重试。
            _status.Text = "展示更新异常，可按 F8 手动切换";
            if (!_reportedUpdateError)
                GD.PushWarning($"FirstPersonSpire 展示更新异常（保留视角）：{error}");
            _reportedUpdateError = true;
        }
    }

    private bool CanToggle() =>
        _room.IsVisibleInTree() && ActiveScreenContext.Instance.IsCurrent(_room);

    private void UpdateControls()
    {
        _controls.Visible = CanToggle();
        var size = GetViewport().GetVisibleRect().Size;
        _panel.Position = new Vector2(Math.Max(0, size.X - 274), 100);
        // 限制长报错的宽度，不遮住整排敌人。
        _panel.Size = new Vector2(250, _panel.Size.Y);
    }

    private void Stop()
    {
        var session = _session;
        _session = null;
        session?.Dispose();
        if (GodotObject.IsInstanceValid(_toggle))
            _toggle.Text = "开启第一人称 [F8]";
    }

    public override void _ExitTree() => Stop();
}
