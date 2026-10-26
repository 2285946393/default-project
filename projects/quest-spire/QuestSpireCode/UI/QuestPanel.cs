using BaseLib.Config;
using Godot;
using QuestSpire.Quests;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace QuestSpire.UI;

// 可折叠 + 可拖动的任务侧栏。标题栏按钮收起/展开；面板尺寸随内容自动变化；
// 鼠标在面板空白处按住可拖动，拖动后不再自动贴右上角。
public sealed partial class QuestPanel : PanelContainer
{
    private static QuestPanel? _instance;
    // 面板宽度跟设置页的「界面缩放」走（原来固定 330，玩家反馈太大）。
    // ⚠️ 用 `UiKit.Scale`（= 玩家设置 × 全局基准），不是直接用 `QuestSpireConfig.UiScale` ——
    //    基准那 0.8 是为了让**已经有 cfg 的老玩家**也一起变小，见 `UiKit.BaseShrink`。
    // ⚠️ 保底 150px：滑块下限放到 0.1 之后，330 × 0.08 只有 26px —— 面板会缩成一条细缝，
    //    看着像渲染 bug。字号可以继续变小，宽度到此为止。
    private static float Width => Mathf.Max(150f, 330f * UiKit.Scale);

    /// <summary>左右各 14px 内边距换算成缩放后的值（行宽 = 面板宽 - 两边内边距）。</summary>
    private static float InnerWidth => Width - UiKit.Px(14) * 2;

    private readonly Dictionary<QuestState, (VBoxContainer row, Label title, Label reward, ProgressBar bar)> _rows = new();

    /// <summary>联机时：这条任务是哪个队友的（自己的不记）。</summary>
    private readonly Dictionary<QuestState, string> _ownerTag = new();

    // —— 联机「按人显示 / 按人收起」——
    // 多人局 4 个人各 20 条就是 80 行，全铺开会糊满屏幕。所以给每个人一个可以点的名字：
    // 点一下就把他那一批收起来/放出来；自己那条默认也是展开的。
    // 行本身还是那条带 `[角色名]` 前缀的行（不做真正的分组容器，改动最小、也不动老逻辑）。
    private sealed class OwnerFilter
    {
        public string Name = "";
        public bool IsMe;
        public bool Collapsed;
        public Label? Chip;
    }
    private HBoxContainer? _ownerBar;
    private readonly List<OwnerFilter> _filters = new();
    private readonly Dictionary<QuestState, int> _ownerIndex = new();
    private string _filterSignature = "";

    private VBoxContainer? _body;
    private Label? _title;
    private Button? _toggle;
    private Button? _pickBtn;
    private Label? _boonsTitle;
    private Label? _boatLabel;
    private Label? _branchLabel;
    private Label? _toriiLabel;
    private Label? _dripperLabel;
    private Label? _ornithopterLabel;
    private Label? _dualWieldLabel;
    private Label? _scholarLabel;
    private Label? _hagglerLabel;
    private Label? _zenLabel;
    private Label? _steadyLabel;
    private Label? _surgeLabel;
    private Label? _bulwarkLabel;
    private Label? _comboLabel;
    private Label? _indifferenceLabel;
    private Label? _scriptureLabel;
    private Label? _anthonyLabel;
    private Label? _bowlbugLabel;
    private Button? _forceBtn;
    private bool _collapsed;

    /// <summary>任务行里要不要显示「完成后的奖励」（玩家 Carson 要的开关；初值来自设置页）。</summary>
    private bool _showRewards = QuestSpireConfig.ShowRewardLines;
    private Button? _rewardToggle;

    /// <summary>建这个面板时用的界面缩放（设置页改了就按新尺度重建）。</summary>
    private float _scaleBuilt = QuestSpireConfig.UiScale;

    private bool _dragging;
    private bool _userMoved;
    private Vector2 _dragOffset;

    public static void ShowPanel(Logger logger)
    {
        var root = UiKit.Root;
        if (root == null) { logger.Warn("QuestPanel: no scene root."); return; }
        if (_instance != null && GodotObject.IsInstanceValid(_instance)) return;
        _instance = new QuestPanel();
        root.AddChild(_instance);
        logger.Info("QuestPanel mounted.");
    }

    public static void HidePanel(Logger logger)
    {
        if (_instance != null && GodotObject.IsInstanceValid(_instance)) _instance.QueueFree();
        _instance = null;
        logger.Info("QuestPanel unmounted.");
    }

    public override void _Ready()
    {
        SetMeta(UiKit.MetaTag, true);
        ProcessMode = ProcessModeEnum.Always;
        ZIndex = 100;
        MouseFilter = MouseFilterEnum.Stop; // 接收拖动输入

        AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.08f, 0.10f, 0.82f),
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            // 内边距也跟缩放走 —— 否则字号小了、边距还那么厚，面板缩不下去。
            ContentMarginLeft = UiKit.Px(14),
            ContentMarginRight = UiKit.Px(14),
            ContentMarginTop = UiKit.Px(10),
            ContentMarginBottom = UiKit.Px(12),
        });

        var root = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        root.AddThemeConstantOverride("separation", UiKit.Px(8));
        AddChild(root);

        var header = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        header.AddThemeConstantOverride("separation", UiKit.Px(8));
        root.AddChild(header);

        _title = new Label
        {
            Text = "尖塔任务",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _title.AddThemeColorOverride("font_color", new Color(0.98f, 0.86f, 0.52f));
        header.AddChild(_title);

        _pickBtn = new Button { Text = "接取任务" };
        _pickBtn.FocusMode = Control.FocusModeEnum.None;
        _pickBtn.Disabled = true;
        _pickBtn.Pressed += () => QuestSelection.OpenPicker();
        header.AddChild(_pickBtn);

        // 「奖励」开关（2026-09-28 玩家 Carson 提的）：
        // 「可以在任务栏那里显示任务完成后的奖励吗，有时候容易忘记。如果担心占屏幕的话，
        //   或许可以考虑加个按钮设置是否显示」
        // 默认**开着**（他本来就想要看到），嫌占地方的点一下就能收掉。
        _showRewards = true;
        _rewardToggle = new Button { Text = "奖励 ◉" };
        _rewardToggle.FocusMode = Control.FocusModeEnum.None;
        _rewardToggle.Pressed += OnToggleRewards;
        header.AddChild(_rewardToggle);

        _toggle = new Button { Text = "收起 ▾" };
        _toggle.FocusMode = Control.FocusModeEnum.None;
        _toggle.Pressed += OnToggle;
        header.AddChild(_toggle);

        _body = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _body.AddThemeConstantOverride("separation", UiKit.Px(9));
        _body.CustomMinimumSize = new Vector2(Width, 0);
        root.AddChild(_body);

        // 联机时才显示的「按人开关」一行（单人局整行隐藏）。放在任务行上面。
        _ownerBar = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        _ownerBar.AddThemeConstantOverride("separation", UiKit.Px(6));
        _body.AddChild(_ownerBar);

        // —— 随身物：任务给的、本体没有对应遗物的东西（船 / 枯枝）——
        _boonsTitle = new Label
        {
            Text = "随身物",
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _boonsTitle.AddThemeColorOverride("font_color", new Color(0.98f, 0.86f, 0.52f));
        root.AddChild(_boonsTitle);

        _boatLabel = MakeBoonLabel("帆船：战斗开始 +15 格挡；本场第一次格挡被清空时再 +20 格挡");
        root.AddChild(_boatLabel);

        _branchLabel = MakeBoonLabel("枯木树枝：每消耗一张牌，随机一张牌加入手牌");
        root.AddChild(_branchLabel);

        _toriiLabel = MakeBoonLabel("鸟居：每场战斗开始时获得 8 点格挡");
        root.AddChild(_toriiLabel);

        _dripperLabel = MakeBoonLabel("咖啡滤杯：每场战斗的第一个回合 +1 能量");
        root.AddChild(_dripperLabel);

        _ornithopterLabel = MakeBoonLabel("玩具扑翼飞机：每次使用药水回复 5 点生命");
        root.AddChild(_ornithopterLabel);

        _dualWieldLabel = MakeBoonLabel("莽夫之道：攻击牌伤害 +50%，但技能牌格挡减半（遗物/药水不受影响）");
        root.AddChild(_dualWieldLabel);

        _scholarLabel = MakeBoonLabel("手不释卷：每回合多抽 1 张牌");
        root.AddChild(_scholarLabel);

        _hagglerLabel = MakeBoonLabel("砍价高手：商店价格 -30%");
        root.AddChild(_hagglerLabel);

        _zenLabel = MakeBoonLabel("养生：篝火休息的回复量 +50%");
        root.AddChild(_zenLabel);

        _steadyLabel = MakeBoonLabel("稳如老狗：能量上限 +1");
        root.AddChild(_steadyLabel);

        _surgeLabel = MakeBoonLabel("厚积薄发：你施加的增益与减益都 +1 层");
        root.AddChild(_surgeLabel);

        _bulwarkLabel = MakeBoonLabel("铁壁：你单次受到的伤害不超过 15 点");
        root.AddChild(_bulwarkLabel);

        _comboLabel = MakeBoonLabel("连环拳：你的攻击牌多打一段");
        root.AddChild(_comboLabel);

        _indifferenceLabel = MakeBoonLabel("冷漠：每当你获得格挡，额外获得 3 点格挡");
        root.AddChild(_indifferenceLabel);

        _scriptureLabel = MakeBoonLabel("蛇花圣经：你给敌人施加的减益 ×1.5");
        root.AddChild(_scriptureLabel);

        _anthonyLabel = MakeBoonLabel("东尼意思：卡牌奖励更容易出现升级版");
        root.AddChild(_anthonyLabel);
        _bowlbugLabel = MakeBoonLabel("盛碗虫常数：每当你获得格挡，额外 +1（正好把 15 补成 16）");
        root.AddChild(_bowlbugLabel);

        // —— 测试按钮：把所有进行中的任务直接判完成并发奖，方便试奖励 ——
        _forceBtn = new Button { Text = "【测试】立即完成全部任务" };
        _forceBtn.FocusMode = Control.FocusModeEnum.None;
        _forceBtn.Pressed += () => _ = ForceComplete();
        root.AddChild(_forceBtn);

        UiKit.StyleFont(this, this);
        ApplyCollapsed();
    }

    // 测试按钮的实现：走 QuestRunner.ForceCompleteAll()，它会调遗物注册进来的发奖出口。
    private static async Task ForceComplete()
    {
        try
        {
            int n = await QuestRunner.ForceCompleteAll();
            Entry.Toast(n == 0 ? "当前没有进行中的任务" : $"已强制完成 {n} 个任务，奖励已发放", "测试按钮");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Error($"ForceComplete failed: {e}");
            Entry.Toast($"强制完成出错：{e.Message}", "测试按钮");
        }
    }

    private static Label MakeBoonLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(InnerWidth, 0),
        };
        label.AddThemeColorOverride("font_color", new Color(0.72f, 0.90f, 0.74f));
        return label;
    }

    private void OnToggle()
    {
        _collapsed = !_collapsed;
        ApplyCollapsed();
    }

    /// <summary>切换任务行里的「奖励」显示（默认开）。同步进设置页并落盘。</summary>
    private void OnToggleRewards()
    {
        _showRewards = !_showRewards;
        if (_rewardToggle != null) _rewardToggle.Text = _showRewards ? "奖励 ◉" : "奖励 ○";
        foreach (var ui in _rows.Values) ui.reward.Visible = _showRewards;

        // 面板上的开关和设置页里的选项是同一个值：这里改完写回配置并存盘，下次进游戏还记得。
        QuestSpireConfig.ShowRewardLines = _showRewards;
        try { ModConfigRegistry.Get<QuestSpireConfig>()?.SaveDebounced(); }
        catch (System.Exception e) { Entry.Logger.Warn($"保存设置失败: {e.Message}"); }
    }

    private void ApplyCollapsed()
    {
        if (_body == null || _toggle == null) return;
        _body.Visible = !_collapsed;
        _toggle.Text = _collapsed ? "展开 ▸" : "收起 ▾";
    }

    // —— 拖动 ——
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                _dragging = true;
                _userMoved = true;
                _dragOffset = GetGlobalMousePosition() - Position;
                AcceptEvent();
            }
            else _dragging = false;
        }
        else if (@event is InputEventMouseMotion && _dragging)
        {
            Position = GetGlobalMousePosition() - _dragOffset;
        }
    }

    public override void _Process(double delta)
    {
        if (_body == null || _title == null) return;

        // 设置页里可以整个关掉侧栏（改完立刻生效，不用重进房间）。
        if (Visible != QuestSpireConfig.ShowQuestPanel) Visible = QuestSpireConfig.ShowQuestPanel;

        // 设置页改了「界面缩放」→ 整个面板按新尺度重建（字号/宽度是建的时候定死的）。
        if (!Mathf.IsEqualApprox(_scaleBuilt, QuestSpireConfig.UiScale))
        {
            _scaleBuilt = QuestSpireConfig.UiScale;
            Entry.Logger.Info($"任务面板按新缩放 {_scaleBuilt:0.00}x 重建。");
            HidePanel(Entry.Logger);
            ShowPanel(Entry.Logger);
            return;
        }

        // 标题只报**我这本**（联机时队友那几本各自有自己的计数，混在一起会看不明白）。
        _title.Text = $"尖塔任务  我 {QuestRunner.Active.Count(q => q.Completed)}/{QuestRunner.Active.Count}"
                      + $"（{QuestRunner.Active.Count}/{QuestRunner.MaxActive}）";
        if (_pickBtn != null) _pickBtn.Disabled = !QuestSelection.PickReady || QuestRunner.IsFull;

        // 随身物一栏：有东西才显示（收起时也一起藏起来）
        bool showBoons = !_collapsed && QuestBoons.Any;
        if (_boonsTitle != null) _boonsTitle.Visible = showBoons;
        if (_boatLabel != null) _boatLabel.Visible = showBoons && QuestBoons.HasBoat;
        if (_branchLabel != null) _branchLabel.Visible = showBoons && QuestBoons.HasDeadBranch;
        if (_toriiLabel != null) _toriiLabel.Visible = showBoons && QuestBoons.HasTorii;
        if (_dripperLabel != null) _dripperLabel.Visible = showBoons && QuestBoons.HasDripper;
        if (_ornithopterLabel != null) _ornithopterLabel.Visible = showBoons && QuestBoons.HasOrnithopter;
        if (_dualWieldLabel != null) _dualWieldLabel.Visible = showBoons && QuestBoons.HasReckless;
        if (_scholarLabel != null) _scholarLabel.Visible = showBoons && QuestBoons.HasScholar;
        if (_hagglerLabel != null) _hagglerLabel.Visible = showBoons && QuestBoons.HasHaggler;
        if (_zenLabel != null) _zenLabel.Visible = showBoons && QuestBoons.HasZen;
        if (_steadyLabel != null) _steadyLabel.Visible = showBoons && QuestBoons.HasSteady;
        if (_surgeLabel != null) _surgeLabel.Visible = showBoons && QuestBoons.HasSurge;
        if (_bulwarkLabel != null) _bulwarkLabel.Visible = showBoons && QuestBoons.HasBulwark;
        if (_comboLabel != null) _comboLabel.Visible = showBoons && QuestBoons.HasCombo;
        if (_indifferenceLabel != null) _indifferenceLabel.Visible = showBoons && QuestBoons.HasIndifference;
        if (_scriptureLabel != null) _scriptureLabel.Visible = showBoons && QuestBoons.HasScripture;
        if (_anthonyLabel != null) _anthonyLabel.Visible = showBoons && QuestBoons.HasAnthony;
        if (_bowlbugLabel != null) _bowlbugLabel.Visible = showBoons && QuestBoons.HasBowlbugConstant;
        if (_forceBtn != null) _forceBtn.Visible = !_collapsed;

        // 显示清单 = 自己那本 + （联机时）每个队友那本 —— 按人分开，互不混算
        var owners = new List<(string Name, bool IsMe, IReadOnlyList<QuestState> Quests)>
        {
            ("我", true, QuestRunner.Active),
        };
        if (QuestRunner.PlayerCount > 1)
            foreach (var (who, quests) in QuestRunner.OtherBooks())
                owners.Add((who, false, quests));

        RebuildOwnerBar(owners);

        var show = new List<QuestState>();
        for (int oi = 0; oi < owners.Count; oi++)
        {
            var (who, isMe, quests) = owners[oi];
            bool collapsed = oi < _filters.Count && _filters[oi].Collapsed;
            foreach (var q in quests)
            {
                // 收起某个人的时候，他那批连行都不进清单 → 下面会被 QueueFree 掉，不占高度。
                if (collapsed) continue;
                show.Add(q);
                _ownerIndex[q] = oi;
                if (!isMe) _ownerTag[q] = who;
            }
        }
        foreach (var q in show)
            if (!_rows.ContainsKey(q)) AddRow(q);
        foreach (var stale in _rows.Keys.Where(k => !show.Contains(k)).ToList())
        {
            _rows[stale].row.QueueFree();
            _rows.Remove(stale);
            _ownerTag.Remove(stale);
            _ownerIndex.Remove(stale);
        }
        foreach (var (q, ui) in _rows)
        {
            string mark = q.Completed ? "✔ " : "□ ";
            // ⚠️ 用 q.Target（实际目标）而不是 q.Def.Target：
            // 多人局里带 ScaleWithPlayers 的任务目标会按队伍人数放大，显示也得跟着变。
            int target = System.Math.Max(1, q.Target);
            string status = q.Completed ? QuestText.Completed : $"{System.Math.Min(q.Current, target)}/{target}";
            // 队友的任务加个名字前缀，一眼能看出是谁的
            string prefix = _ownerTag.TryGetValue(q, out var owner) ? $"[{owner}] " : "";
            string nameText = $"{mark}{prefix}{QuestText.Title(q.Def)}  {status}";
            if (ui.title.Text != nameText) ui.title.Text = nameText;
            ui.title.Modulate = q.Completed ? new Color(0.6f, 0.9f, 0.62f) : Colors.White;
            float pct = (float)q.Current / target;
            if (!Mathf.IsEqualApprox(ui.bar.Value, pct)) ui.bar.Value = pct;
        }

        Size = GetCombinedMinimumSize();
        var vp = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080);
        float edge = UiKit.Px(14);
        if (!_userMoved)
        {
            Position = new Vector2(Mathf.Max(0, vp.X - Size.X - edge), edge);
        }
        else
        {
            Position = new Vector2(
                Mathf.Clamp(Position.X, 0, Mathf.Max(0, vp.X - Size.X)),
                Mathf.Clamp(Position.Y, 0, Mathf.Max(0, vp.Y - Size.Y)));
        }
    }

    /// <summary>
    /// 重建「按人开关」那一行：`我 3/20`、`[铁甲战士] 2/20` …… 点谁收谁。
    /// 只在名字/人数变了的时候重建（每帧重建按钮会抖、也会吃掉点击）。
    /// </summary>
    private void RebuildOwnerBar(List<(string Name, bool IsMe, IReadOnlyList<QuestState> Quests)> owners)
    {
        if (_ownerBar == null) return;
        bool multi = owners.Count > 1;
        _ownerBar.Visible = multi;
        if (!multi)
        {
            // 单人局：把联机时的「收起」状态一起清掉，免得切回单人后任务莫名其妙是隐藏的。
            if (_filters.Count > 0)
            {
                foreach (var f in _filters) f.Chip?.QueueFree();
                _filters.Clear();
            }
            _filterSignature = "";
            return;
        }

        var sig = string.Join("|", owners.Select(o => o.Name));
        if (sig != _filterSignature)
        {
            _filterSignature = sig;
            foreach (var f in _filters) f.Chip?.QueueFree();
            _filters.Clear();
            foreach (var (name, isMe, _) in owners)
            {
                var chip = new Label
                {
                    Text = name,
                    MouseFilter = MouseFilterEnum.Stop,   // 要接点击
                };
                chip.AddThemeColorOverride("font_color", new Color(0.78f, 0.82f, 0.90f));
                _ownerBar.AddChild(chip);

                var filter = new OwnerFilter { Name = name, IsMe = isMe, Chip = chip };
                chip.GuiInput += e =>
                {
                    if (e is not InputEventMouseButton mb) return;
                    if (mb.ButtonIndex != MouseButton.Left || !mb.Pressed) return;
                    filter.Collapsed = !filter.Collapsed;
                };
                _filters.Add(filter);
            }
        }

        // 每帧只更新计数和收起/展开的样式
        for (int i = 0; i < _filters.Count && i < owners.Count; i++)
        {
            var f = _filters[i];
            int done = owners[i].Quests.Count(q => q.Completed);
            int total = owners[i].Quests.Count;
            string text = f.Collapsed ? $"{f.Name} {done}/{total} ▸" : $"{f.Name} {done}/{total}";
            if (f.Chip != null)
            {
                if (f.Chip.Text != text) f.Chip.Text = text;
                f.Chip.AddThemeColorOverride("font_color",
                    f.Collapsed ? new Color(0.55f, 0.58f, 0.66f) : new Color(0.78f, 0.82f, 0.90f));
            }
        }
    }

    private void AddRow(QuestState q)
    {
        var box = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", UiKit.Px(2));

        var title = new Label
        {
            Text = QuestText.Title(q.Def),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        title.AddThemeColorOverride("font_color", Colors.White);
        box.AddChild(title);

        var desc = new Label
        {
            Text = QuestText.Description(q.Def),
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        desc.AddThemeColorOverride("font_color", new Color(0.72f, 0.74f, 0.80f));
        box.AddChild(desc);

        // 完成后的奖励（玩家 Carson 要的：容易忘，所以直接写在任务下面）。用金色跟标题区分开。
        var reward = new Label
        {
            Text = QuestText.RewardPrefix + (string.IsNullOrWhiteSpace(QuestText.Reward(q.Def)) ? "—" : QuestText.Reward(q.Def)),
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(InnerWidth, 0),
            Visible = _showRewards,
        };
        reward.AddThemeColorOverride("font_color", new Color(0.98f, 0.84f, 0.50f));
        box.AddChild(reward);

        var bar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Step = 0.001,
            Value = (float)q.Current / System.Math.Max(1, q.Target),
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(InnerWidth, UiKit.Px(12)),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        box.AddChild(bar);

        var font = UiKit.GameFont();
        if (font != null)
        {
            title.AddThemeFontOverride("font", font);
            title.AddThemeFontSizeOverride("font_size", UiKit.Scaled(17));
            desc.AddThemeFontOverride("font", font);
            desc.AddThemeFontSizeOverride("font_size", UiKit.Scaled(14));
            reward.AddThemeFontOverride("font", font);
            reward.AddThemeFontSizeOverride("font_size", UiKit.Scaled(13));
        }

        _body!.AddChild(box);
        // ⚠️ 必须显式排到最后：任务行是「按需新建」的（收起某人时他那批会被删掉、展开时又新建），
        //    不移动到末尾的话，新建的行会插到之前那批行前面，顺序看着是乱的。
        //    `_ownerBar` 永远留在第 0 位（它在 _Ready 里最先加进去）。
        _body.MoveChild(box, _body.GetChildCount() - 1);
        _rows[q] = (box, title, reward, bar);
    }
}
