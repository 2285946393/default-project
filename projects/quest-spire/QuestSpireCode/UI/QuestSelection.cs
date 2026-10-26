using Godot;
using QuestSpire.Quests;

namespace QuestSpire.UI;

// 选任务浮层。开局和商店都**不自动弹**：进入时只点亮任务面板上的「接取任务」按钮，
// 玩家想接了再点，弹一次挑选。候选可以点「刷新候选」重抽（2026-09-25 玩家要求，学海克斯符文）。
// 候选池由 QuestRunner.Offers() 随机给出（排除进行中/已完成）。
//
// 多人局：**开局这次挑选由 QuestPickSync 接管**——本机挑完会广播给其他客户端，
// 所以每台机器的任务列表一致、主机存档也是全的。商店那次挑选只在单人局开放（见 OnShopEntered）。
public static class QuestSelection
{
    private enum PickCtx { None, RunStart, Shop }

    private const int RunStartOffers = 5;   // 单人局：开局给几个候选（玩家要求：5 个，别太多）
    private const int RunStartCap = 3;      // 单人局：开局最多接几个

    // 多人局：商店那次挑选是关掉的（会打乱游戏的选择编号），所以**开局这一次就是整局的量**，
    // 每人只给 3 条根本不够玩。2026-09-25 玩家定：联机开局候选 10 个、最多接 8 条。
    private const int RunStartOffersMp = 10;
    private const int RunStartCapMp = 8;

    /// <summary>本局开局给几个候选 / 最多接几个（联机和单人不一样）。</summary>
    private static int StartOffers => QuestRunner.PlayerCount > 1 ? RunStartOffersMp : RunStartOffers;
    private static int StartCap => QuestRunner.PlayerCount > 1 ? RunStartCapMp : RunStartCap;

    private static PickCtx _ctx;
    private static bool _open;
    private static Control? _layer;

    private static List<QuestDef> _pending = new();
    private static int _pendingCap;
    private static string _pendingTitle = "";
    private static Type? _pendingChar;
    private static int _rerollsLeft;   // 每批候选可刷新的次数（默认 1；设置页可开「无限刷新」）

    /// 每批候选的刷新预算（设置页「无限刷新候选」开了就是无限）。
    private static int RerollBudget => QuestSpireConfig.UnlimitedRerolls ? int.MaxValue : 1;

    /// 面板「接取任务」按钮是否可用（有待接批次即可用）。
    public static bool PickReady => _ctx != PickCtx.None;

    /// 强制复位并清理残留浮层：每局开始 / 局结束时调用。
    public static void CloseOpen()
    {
        _open = false;
        _ctx = PickCtx.None;
        _pending.Clear();
        if (_layer != null && GodotObject.IsInstanceValid(_layer)) _layer.QueueFree();
        _layer = null;
    }

    /// 开局：点亮按钮（不自动弹）。单人给 5 个候选最多接 3；**联机给 10 个候选最多接 8**
    /// （联机没有商店那次挑选了，开局这次就是整局的量）。
    /// 候选**优先从前置型任务里抽**——那些奖励是「拿到之后才生效」的遗物/随身物，
    /// 必须一开局就摆到玩家面前，不然到手时局都快结束了。
    public static void ArmRunStart(Type? character = null)
    {
        _pendingChar = character;
        _rerollsLeft = RerollBudget;
        _pendingCap = StartCap;
        _pendingTitle = "选择任务";
        RerollRunStartOffers(character);
    }

    /// 进商店：点亮按钮。给 3 个候选，上限 = 剩余空位。
    /// ⚠️ 多人局不开放：商店是"谁什么时候进都不一样"的时机，插进去会让各端的选择编号错位，
    /// 游戏自己后面那些「选哪张牌」就会卡住。多人局只在开局挑一次。
    public static void OnShopEntered(Type? character = null)
    {
        if (QuestRunner.PlayerCount > 1) return;
        _pendingChar = character;
        _pending = QuestRunner.Offers(3, character: character).ToList();
        _pendingCap = QuestRunner.SlotsLeft;
        _rerollsLeft = RerollBudget;
        _pendingTitle = "接取任务";
        _ctx = QuestRunner.IsFull || _pending.Count == 0 ? PickCtx.None : PickCtx.Shop;
    }

    /// 进入非商店房间：只收回“商店那次”的按钮；开局那批没点就一直留着。
    public static void OnOtherRoom()
    {
        if (_ctx == PickCtx.Shop)
        {
            _ctx = PickCtx.None;
            _pending.Clear();
        }
    }

    /// 玩家点「接取任务」按钮。
    public static void OpenPicker()
    {
        if (!PickReady || _open || QuestRunner.IsFull || _pendingCap <= 0) return;
        // ⚠️ 战斗中不给接（2026-10-08）：按钮从开局一直亮到点为止，玩家完全可能在战斗里点。
        //    接取会当场做「接取时改动」（诅咒交易塞 5 张诅咒 / 大改造换牌）——那是**改牌组**，
        //    战斗中途改牌组既容易和战斗结算打架，也会让各端牌组对不上（校验和 → 掉线）。
        try
        {
            if (MegaCrit.Sts2.Core.Combat.CombatManager.Instance.IsInProgress)
            {
                Entry.Toast("战斗结束后再接任务", "任务");
                return;
            }
        }
        catch (System.Exception e) { Entry.Logger.Warn($"判断是否战斗中失败（按可以接处理）: {e.Message}"); }
        OpenOnce();
    }

    private static void OpenOnce()
    {
        if (_pending.Count == 0 || _pendingCap <= 0) return;
        Open(_pending, _pendingTitle, cap: _pendingCap, autoFill: 0,
            onPicked: () => { _ctx = PickCtx.None; _pending.Clear(); },
            // 每批候选只能刷一次：刷完这个按钮就没了（不能一直刷到满意为止）。
            onRefresh: (_rerollsLeft > 0 && _ctx != PickCtx.None)
                ? () => { _rerollsLeft--; Reroll(); OpenOnce(); }
                : null);
    }

    /// <summary>重抽「开局那批」候选，并把按钮点亮/熄灭。</summary>
    private static void RerollRunStartOffers(Type? character)
    {
        _pending = QuestRunner.Offers(StartOffers, character: character).ToList();
        _pendingCap = StartCap;
        _pendingTitle = "选择任务";
        _ctx = _pending.Count > 0 ? PickCtx.RunStart : PickCtx.None;
    }

    /// 「刷新候选」：重新抽一批（自动排除已经接了的）。
    /// 多人局也安全——刷新只发生在你自己这台，广播出去的只有你最后挑中的那几个任务。
    private static void Reroll()
    {
        if (_ctx == PickCtx.RunStart)
        {
            RerollRunStartOffers(_pendingChar);
        }
        else if (_ctx == PickCtx.Shop)
        {
            _pending = QuestRunner.Offers(3, character: _pendingChar).ToList();
            _pendingCap = QuestRunner.SlotsLeft;
            _pendingTitle = "接取任务";
        }
    }

    private static void Open(IReadOnlyList<QuestDef> offers, string title, int cap, int autoFill,
        System.Action? onPicked, System.Action? onRefresh)
    {
        var root = UiKit.Root;
        if (root == null || _open || offers.Count == 0 || cap <= 0) return;

        _open = true;
        var picks = new List<QuestDef>();

        // ⚠️ 2026-10-11：这个浮层原来**完全不响应**设置页的「界面缩放」——
        //    面板固定 680px、描述列固定 560px，玩家把滑块拉到底它也纹丝不动，
        //    所以「UI 还是太大」的反馈多半就是指它。现在整层按 `UiKit.Scale` 走。
        //
        // ⚠️ 同样有最小宽度兜底：滑块下限放到 0.1 之后，680 × 0.08 只有 54px，
        //    浮层会挤成一团。字号继续变小，宽度到此为止。
        float s = UiKit.Scale;
        float panelW = Mathf.Max(380f, 680f * s);
        float descW = Mathf.Max(260f, 560f * s);

        var layer = new Control { ProcessMode = Node.ProcessModeEnum.Always };
        layer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        layer.ZIndex = 200;
        layer.SetMeta(UiKit.MetaTag, true);
        root.AddChild(layer);
        _layer = layer;

        var dim = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.6f),
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        layer.AddChild(dim);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(panelW, 0) };
        panel.AddThemeStyleboxOverride("panel", CardStyle(new Color(0.10f, 0.11f, 0.14f, 0.97f)));
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Both;
        layer.AddChild(panel);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", UiKit.Px(10));
        panel.AddChild(box);

        var header = new Label { Text = title, HorizontalAlignment = HorizontalAlignment.Center };
        // 这里显式设字号，`UiKit.StyleFont` 会跳过它（见 `UiKit.ApplyFont`）——
        // 原来是 26 被 StyleFont 无条件压成 18，标题根本没体现出层级。
        header.AddThemeFontSizeOverride("font_size", UiKit.Scaled(26));
        box.AddChild(header);

        foreach (var def in offers)
        {
            var captured = def;
            var row = new PanelContainer();
            row.AddThemeStyleboxOverride("panel", CardStyle(new Color(0.17f, 0.19f, 0.24f, 1f)));
            box.AddChild(row);

            var hb = new HBoxContainer();
            hb.AddThemeConstantOverride("separation", UiKit.Px(14));
            row.AddChild(hb);

            var cb = new CheckBox { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
            hb.AddChild(cb);

            var info = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
            hb.AddChild(info);
            var name = new Label { Text = $"【{QuestText.Title(captured)}】 {QuestText.RewardPrefix}{QuestText.Reward(captured)}" };
            name.AddThemeColorOverride("font_color", new Color(0.98f, 0.87f, 0.55f));
            info.AddChild(name);
            var desc = new Label { Text = QuestText.Description(captured), AutowrapMode = TextServer.AutowrapMode.WordSmart };
            desc.CustomMinimumSize = new Vector2(descW, 0);
            info.AddChild(desc);

            cb.Toggled += on =>
            {
                if (on && !picks.Contains(captured))
                {
                    if (picks.Count >= cap) { cb.SetPressedNoSignal(false); return; }
                    picks.Add(captured);
                }
                else if (!on) picks.Remove(captured);
            };
        }

        var btns = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        box.AddChild(btns);
        var cancel = new Button { Text = "关闭" };
        cancel.Pressed += () => Close(layer);
        btns.AddChild(cancel);

        if (onRefresh != null)
        {
            var refresh = new Button { Text = QuestSpireConfig.UnlimitedRerolls ? "刷新候选（无限）" : "刷新候选（仅 1 次）" };
            refresh.Pressed += () =>
            {
                Close(layer);
                onRefresh();
            };
            btns.AddChild(refresh);
        }

        var ok = new Button { Text = "确认接取" };
        ok.Pressed += () =>
        {
            var chosen = picks.ToList();
            if (chosen.Count == 0 && autoFill > 0)
                chosen = offers.Take(autoFill).ToList();
            foreach (var d in chosen) QuestRunner.TryAdd(d);
            _ = QuestRunner.ApplyAcceptSetups();   // 接取当场就改牌组（诅咒交易/大改造），别等进房间
            Entry.Logger.Info($"QuestSelection confirmed: picked {chosen.Count} quests.");
            // 多人局：把自己挑的广播出去，别人才看得到、主机存档里才有。
            QuestPickSync.NotifyLocalPick(chosen);
            Close(layer);
            onPicked?.Invoke();
        };
        btns.AddChild(ok);

        UiKit.StyleFont(layer, layer);
    }

    private static void Close(Control layer)
    {
        _open = false;
        _layer = null;
        if (GodotObject.IsInstanceValid(layer)) layer.QueueFree();
    }

    private static StyleBoxFlat CardStyle(Color bg) => new()
    {
        BgColor = bg,
        CornerRadiusTopLeft = 10,
        CornerRadiusTopRight = 10,
        CornerRadiusBottomLeft = 10,
        CornerRadiusBottomRight = 10,
        // 内边距跟缩放走，否则字号小了、留白还那么大，浮层缩不下去。
        ContentMarginLeft = UiKit.Px(16),
        ContentMarginRight = UiKit.Px(16),
        ContentMarginTop = UiKit.Px(12),
        ContentMarginBottom = UiKit.Px(12),
    };
}
