using Godot;

namespace DouSpire.Table;

/// <summary>
/// 常驻的小节点，干两件事：
///
/// 1. **全局热键 F9** 开/收斗地主桌。
///    `Node._Input` 是**最先**拿到输入的地方（早于 GUI 的 `_GuiInput`），
///    所以哪怕桌面铺满屏幕把主菜单全挡住，F9 也一定收得到。
///
/// 2. **给主菜单按钮兜底重试**。
///    为什么需要：`Initialize()` 和几个生命周期事件到这个节点的时候，主菜单可能还没建好，
///    那一次 `Install()` 就白跑了。主菜单就绪事件理论上会再来，但这条链我没法在游戏里实测，
///    所以用一个"每 2 秒试一次、最多试 30 秒"的兜底 —— 反正很便宜。
/// </summary>
public sealed partial class DouSpireHotkey : Node
{
    private static DouSpireHotkey? _instance;

    public const Key ToggleKey = Key.F9;

    private double _retryTimer;
    private int _retries;

    /// <summary>幂等安装：已经挂上就什么都不做。</summary>
    public static void Install()
    {
        if (_instance != null && GodotObject.IsInstanceValid(_instance)) return;
        var root = DouSpireUi.Root;
        if (root == null) return;
        _instance = new DouSpireHotkey { Name = "DouSpireHotkey" };
        root.AddChild(_instance);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey key) return;
        if (!key.Pressed || key.Echo) return;

        // ★★ 2026-10-13：**ESC 是"退出当前牌桌"的硬兜底**（用户：「一定要保证能退出房间」）。
        //   为什么必须有这一条：三张桌子原来都只能靠**屏幕上的收桌钮**退出 ——
        //   万一那颗钮被别的界面挡住 / 层级出岔 / 鼠标点不到，人就**彻底出不来**了，
        //   只能关游戏。键盘这条路走的是 `_Input`（早于 GUI 的 `_GuiInput`），
        //   **钮点不到也照样收得到** —— 这是最后一道保险，任何情况下都不该被拿掉。
        if (key.Keycode == Key.Escape)
        {
            if (DouSpireBlackjackTable.CloseByHotkey() | DouSpirePokerTable.CloseByHotkey()
                | DouSpireTable.CloseByHotkey())
            {
                GetViewport()?.SetInputAsHandled();
                return;
            }
            return;   // 没有桌子开着 → 让 ESC 走原本的用途（游戏自己的返回/菜单）
        }

        if (key.Keycode != ToggleKey) return;

        // ★★ 2026-10-13：**F9 有牌桌开着时必须"收桌"，不能再去开斗地主桌**。
        //   原来 F9 无条件 `DouSpireTable.Toggle()`：21 点 / 德州开着的时候按 F9，
        //   会在 21 点上面**又叠一张斗地主桌** —— 从此收桌钮被盖住、越叠越乱，
        //   用户那边的"退不出去"十有八九就是这条踩出来的。
        //   现在的口径：**先收当前开着的桌，都收干净了再按原来的开关逻辑走。**
        if (DouSpireBlackjackTable.CloseByHotkey() | DouSpirePokerTable.CloseByHotkey())
        {
            GetViewport()?.SetInputAsHandled();
            return;
        }

        DouSpireTable.Toggle(Entry.Logger);
        GetViewport()?.SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        // ★★ 2026-10-13：**音乐压制必须挂在这里**（用户：「关闭后音乐一直放」）。
        //   病因链条：游戏跑局音乐由 `NRunMusicController` 每帧自动推导、会自动把曲子放回来；
        //   我们收桌时把它停了，但**压制它需要每帧都有人盯着**。
        //   而原来 `DouSpireMusic.Tick()` 只挂在**牌桌自己的 `_Process`** 上 ——
        //   桌子一收，那个 `_Process` 就没了，**从此没有任何东西在管音乐**，
        //   它想放什么就放什么。这个常驻节点是三张桌子都收掉之后**唯一还在跑**的地方，
        //   所以压制的巡检钉在这儿。
        //   ⚠️ 这一句**必须排在下面那个 `_retries` 提前 return 之前** ——
        //      排在后面的话，开局 30 秒之后它就不再跑了（那个 return 是给按钮重试用的）。
        DouSpireMusic.SuppressTick();

        // 只在前 30 秒里重试，别一整个会话都在扫场景树
        if (_retries >= 15) return;
        _retryTimer -= delta;
        if (_retryTimer > 0) return;
        _retryTimer = 2.0;
        _retries++;
        DouSpireMenuButton.Install();
    }
}
