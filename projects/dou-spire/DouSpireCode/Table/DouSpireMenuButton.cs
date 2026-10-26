using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace DouSpire.Table;

/// <summary>
/// 往游戏**主菜单**里加我们自己的按钮 —— 这就是"我们自己那个模式"的入口。
/// 目前两个：**「斗地主尖塔」**和**「德州扑克尖塔」**（玩法二，2026-10-09 加）。
///
/// 为什么不用自定义模式的修正器入口：自定义模式终究要开一局尖塔，
/// 那就有地图、有怪物、有房间 —— 而用户要的是
/// 「**不应该出现怪物，就一个桌子**」。从主菜单直接进，压根没有 run，最干净。
///
/// ── 挂载点 ──────────────────────────────────────────────
/// 反编译 RitsuLib（<c>STS2RitsuLib.Ui.MainMenu.NMainMenuScroller</c>）看来的：
/// 它把主菜单里那个 <c>MainMenuTextButtons</c> 节点整个换成自己的可滚动容器，
/// **但沿用了同一个节点名**（<c>nMainMenuScroller.Name = val.Name</c>），
/// 并且新容器依旧派生自 <see cref="Control"/>。
/// 所以不管换没换、换前换后，<c>menu.GetNodeOrNull&lt;Control&gt;("MainMenuTextButtons")</c>
/// 都拿得到，往里塞子节点即可 —— 不用写 Harmony。
/// （顺带：就算我们比它先塞，它换容器时会把原容器的子节点全搬过去，先来后到都不丢。）
///
/// ── 按钮怎么造 ──────────────────────────────────────────
/// 首选 **复制游戏自己的一个菜单按钮**（<see cref="NMainMenuTextButton"/>）：
/// 外观、字体（含中文的字体替换）、悬停动画、手柄/键盘焦点导航全都白送 ——
/// 因为那套逻辑就跑在它身上。自己 <c>new</c> 一个是不行的：
/// 它的 <c>_Ready()</c> 会 <c>GetChild&lt;MegaLabel&gt;(0)</c> 去取那个文字子节点，
/// 新对象没有子节点，直接抛。
///
/// 复制时**刻意不带 signal**（见 <see cref="DuplicateFlags"/> 的用法）：
/// 免得把模板按钮原本接的动作（比如"继续游戏"）一起抄过来，点一下莫名进了别人的流程。
/// </summary>
public static class DouSpireMenuButton
{
    private const string ContainerPath = "MainMenuTextButtons";

    /// <summary>「单人游戏」那个原生按钮的节点名 —— 我们两个按钮就摆在它**紧下面**。</summary>
    private const string SingleplayerNodeName = "SingleplayerButton";

    private const string DouButtonName = "DouSpireMainMenuButton";
    private const string DouLabel = "斗地主尖塔";

    private const string PokerButtonName = "DouSpirePokerMainMenuButton";
    private const string PokerLabel = "德州扑克尖塔";

    private const string BlackjackButtonName = "DouSpireBlackjackMainMenuButton";
    private const string BlackjackLabel = "21 点尖塔";

    private static bool _warnedMissingContainer;

    /// <summary>幂等安装「斗地主尖塔」。装不上只记日志，不影响 F9 那条路。</summary>
    public static void Install() =>
        InstallButton(DouButtonName, DouLabel, () => DouSpireEntry.Toggle(Entry.Logger),
                      afterAnchor: SingleplayerNodeName);

    /// <summary>幂等安装「德州扑克尖塔」（玩法二）。和上面完全同一套机制，只是名字和回调不同。</summary>
    public static void InstallPoker() =>
        InstallButton(PokerButtonName, PokerLabel, () => DouSpirePokerEntry.Toggle(Entry.Logger),
                      // ★ 锚在**斗地主那个按钮**后面 → 两个挨着，而且斗地主永远在前。
                      afterAnchor: DouButtonName);

    /// <summary>幂等安装「21 点尖塔」（玩法三）。锚在德州那个按钮后面 —— 三个按 斗地主 / 德州 / 21点 排下来。</summary>
    public static void InstallBlackjack() =>
        InstallButton(BlackjackButtonName, BlackjackLabel, () => DouSpireBlackjackEntry.Toggle(Entry.Logger),
                      afterAnchor: PokerButtonName);

    /// <summary>三个一起装 —— 入口在 <see cref="Entry.EnsureEntryPoints"/> 里调这一个就够。</summary>
    public static void InstallAll()
    {
        Install();
        InstallPoker();
        InstallBlackjack();
    }

    /// <summary>
    /// 把按钮挪到某个锚点按钮的**紧后面**（用户 2026-10-09：「要加在单人游戏的下面，两个按钮，
    /// 而不是退出的下面」）。
    ///
    /// 为什么能靠"顺序"定位：容器（含 RitsuLib 换过的那个 scroller）是**按子节点顺序竖排**的，
    /// 所以我们 `AddChild` 是"加在最末 = 退出下面"，`MoveChild` 就能把它挪到任意位置。
    ///
    /// ★ 锚点找不到时**保持原地**（不报错、不动）—— 宁可位置不对，也不能把按钮弄丢。
    /// </summary>
    private static void PlaceAfter(Control container, Node node, string anchorName)
    {
        var kids = container.GetChildren();
        var idx = -1;
        for (var i = 0; i < kids.Count; i++)
        {
            if (kids[i] == node) continue;                      // 锚点不能是自己
            if (kids[i].Name.ToString() == anchorName) { idx = i; break; }
        }
        if (idx < 0)
        {
            Entry.Logger.Info($"斗地主：主菜单里找不到锚点「{anchorName}」，按钮保持原位（末尾）。");
            return;
        }
        container.MoveChild(node, idx + 1);
    }

    /// <summary>装一个菜单按钮。幂等（同名已存在就只重新摆一次位置）。</summary>
    private static void InstallButton(string nodeName, string label, System.Action onPressed,
                                      string afterAnchor)
    {
        try
        {
            var root = DouSpireUi.Root;
            if (root == null) return;

            var menu = FindDescendant<NMainMenu>(root);
            if (menu == null) return;                       // 还没进主菜单，下次再来

            var container = menu.GetNodeOrNull<Control>(ContainerPath);
            if (container == null)
            {
                if (!_warnedMissingContainer)
                {
                    _warnedMissingContainer = true;
                    Entry.Logger.Warn($"斗地主：主菜单里找不到 {ContainerPath}，按钮挂不上（F9 照样能用）。");
                }
                return;
            }
            if (container.HasNode(nodeName))
            {
                // 已经挂过了 —— 但 RitsuLib 换容器 / 别的东西搬动过，位置可能跑了。
                // 每次进来都重新摆一次（幂等、且很便宜），保证始终贴在锚点下面。
                PlaceAfter(container, container.GetNode(nodeName), afterAnchor);
                return;
            }

            // —— 首选：复制游戏自己的菜单按钮 ——
            //
            // ★ 模板必须挑**当前看得见**的那个。
            // `Duplicate` 会把模板的"隐藏 / 禁用 / 透明度"一并复制过去，而主菜单里
            // 「继续游戏」「放弃本局」在没有存档时是 `Visible=false` 的，且往往排在容器最前面 ——
            // 一旦 FirstOrDefault() 挑中它们，我们的按钮**建出来了却看不见**
            // （日志明明写着"已挂上"，屏幕上就是没有，2026-10-06 就是这个症状）。
            var natives = container.GetChildren().OfType<NMainMenuTextButton>().ToList();
            // 优先级：点名「单人模式」那个（一定可见、一定可点）→ 任何可见的 → 任何原生按钮
            var template = natives.FirstOrDefault(b => GodotObject.IsInstanceValid(b) && b.Name.ToString() == SingleplayerNodeName)
                           ?? natives.FirstOrDefault(b => GodotObject.IsInstanceValid(b) && b.Visible)
                           ?? natives.FirstOrDefault(b => GodotObject.IsInstanceValid(b));

            Entry.Logger.Info($"斗地主：主菜单按钮「{label}」的复制模板 = "
                              + $"{(template == null ? "<没有原生按钮，走兜底>" : template.Name.ToString())}"
                              + $"（容器 {ContainerPath} 实际类型 = {container.GetClass()}，里面原生按钮 {natives.Count} 个，"
                              + $"可见的 {natives.Count(b => GodotObject.IsInstanceValid(b) && b.Visible)} 个）。");

            if (template != null && TryCloneNative(template, container, nodeName, label, onPressed, afterAnchor)) return;

            // —— 兜底：Godot 原生 Button（长得不像一家，但一定能点） ——
            BuildPlainButton(container, nodeName, label, onPressed, afterAnchor);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：主菜单按钮「{label}」安装失败（F9 照样能用）: {e}");
        }
    }

    /// <summary>复制一个原生菜单按钮。任何一步不对就返回 false，交给兜底路径。</summary>
    private static bool TryCloneNative(NMainMenuTextButton template, Control container,
                                       string nodeName, string label, System.Action onPressed,
                                       string afterAnchor)
    {
        // 只要"实体化 + 脚本"，**不要 signals / groups**：
        // signals 会把模板按钮原本接的动作一起复制过来（点一下可能触发别人的功能），
        // groups 则可能让它被别的系统当成原按钮处理。子节点（那个文字）照样会一起复制过来。
        const Node.DuplicateFlags flags = Node.DuplicateFlags.UseInstantiation | Node.DuplicateFlags.Scripts;

        var clone = template.Duplicate((int)flags) as NMainMenuTextButton;
        if (clone == null) return false;

        clone.Name = nodeName;
        clone.Released += _ => onPressed();
        container.AddChild(clone);                          // _Ready 在这儿跑，label 字段这时才填上
        PlaceAfter(container, clone, afterAnchor);          // ★ 挪到锚点下面（不然会落在"退出"下面）

        // ★ 把从模板那儿继承来的"隐藏 / 禁用 / 半透明"逐个掰回来 —— 否则按钮在树上但看不见也点不动。
        clone.Visible = true;
        clone.Modulate = Colors.White;
        if (!clone.IsEnabled) clone.Enable();
        // 有些容器会在自己的入场动画里重置子节点状态，所以等容器这帧布完局再确认一次
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(clone)) return;
            clone.Visible = true;
            clone.Modulate = Colors.White;
            if (!clone.IsEnabled) clone.Enable();
            // 自检：把"最终到底可不可见、摆在哪个 rect、贴在谁下面"写进日志。
            // 下次再出现"按钮看不到"，看这一行就能定位（再也不用靠猜）。
            Entry.Logger.Info($"斗地主：按钮自检「{label}」 可见={clone.Visible} 树上可见={clone.IsVisibleInTree()} "
                              + $"rect={clone.GetGlobalRect()} 父={(clone.GetParent()?.GetClass() ?? "null")} "
                              + $"第 {clone.GetIndex()} 个（共 {container.GetChildCount()} 个）");
        }).CallDeferred();

        // label 是 NMainMenuTextButton 的公开字段，_Ready → ConnectSignals 里赋值。
        // 复制体的第 0 个子节点就是原按钮那个文字控件，所以这里一定拿得到。
        // 直接改它的文字（不调 SetLocalization —— 那需要游戏本地化表里的 key，
        // 我们没有也不该去加；它的 _locString 在复制体上是 null，不会有东西来覆盖我们）。
        var labelControl = clone.label;
        if (labelControl == null)
        {
            container.RemoveChild(clone);
            clone.QueueFree();
            return false;
        }

        labelControl.Text = label;
        labelControl.AddThemeFontSizeOverride("font_size", DouSpireUi.Scaled(28));
        // 模板若是"禁用态"，label 被调成 1/4 透明（NMainMenuTextButton 里那条 quarterTransparentWhite）——
        // 一并掰回白色，否则字看着发灰。
        labelControl.Modulate = Colors.White;

        Entry.Logger.Info($"斗地主：主菜单按钮「{label}」已挂上（复制了游戏原生菜单按钮，外观/焦点导航都和旁边一致）。");
        DouSpireTable.DebugAutoOpen();   // 只在设了 DOUSPIRE_DEBUG_OPEN=1 时才会真的开桌
        return true;
    }

    private static void BuildPlainButton(Control container, string nodeName, string label,
                                         System.Action onPressed, string afterAnchor)
    {
        var btn = new Button
        {
            Name = nodeName,
            Text = label,
            FocusMode = Control.FocusModeEnum.None,
        };
        // ★★ 2026-10-10 用户：「按钮没有声音，加个本体点按钮的声音」—— 本 mod 自造的按钮统一补上。
        btn.Pressed += () => { DouSpireSfx.Select(); onPressed(); };
        btn.MouseEntered += DouSpireSfx.Hover;

        // 先抄一个现成菜单按钮的外观，再挂上去 —— 不抄就是 Godot 默认皮肤，跟旁边格格不入。
        var look = container.GetChildren()
            .OfType<Control>()
            .FirstOrDefault(c => c is Button && c.Name != nodeName);
        container.AddChild(btn);
        PlaceAfter(container, btn, afterAnchor);            // ★ 同样挪到锚点下面
        if (look != null) DouSpireUi.BorrowLook(btn, look);
        else DouSpireUi.StyleControl(btn);
        // 兜底路径同样别继承到"不可见 / 半透明"
        btn.Visible = true;
        btn.Modulate = Colors.White;

        Entry.Logger.Warn($"斗地主：主菜单按钮「{label}」用了兜底造法（普通 Button）—— 能点，但外观可能和旁边不一致。");
        DouSpireTable.DebugAutoOpen();
    }

    private static T? FindDescendant<T>(Node node) where T : Node
    {
        foreach (var child in node.GetChildren())
        {
            if (child is T hit) return hit;
            var deeper = FindDescendant<T>(child);
            if (deeper != null) return deeper;
        }
        return null;
    }
}
