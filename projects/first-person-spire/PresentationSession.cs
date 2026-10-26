using Godot;
using FirstPersonSpire.Core;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Context;

namespace FirstPersonSpire;

// 原身体在原站位播放；展示坐标、原点击框和界面独立投影，不抢原版身体动画。
internal sealed class PresentationSession(NCombatRoom room, Node owner) : IDisposable
{
    private readonly Dictionary<CanvasItem, uint> _originalLayers = new();
    private readonly Dictionary<Control, UiProjection> _ui = new();
    private readonly List<EnemyView> _enemies = new();
    private readonly List<SubViewport> _captures = new();
    // 已归位的战斗特效 → 它被放进的层。避免每帧重复处理同一个节点。
    private readonly Dictionary<CanvasItem, uint> _vfxOwner = new();
    // UI 层原来的 ZIndex，关闭时还原。
    private int _originalUiZ;
    // 特效 → 它归属的生物。自己人的附加图标要跟着固定栏走，需要知道归属。
    private readonly Dictionary<CanvasItem, NCreature> _vfxCreature = new();
    // 属于自己人（非敌人）的特效集合。
    private readonly HashSet<CanvasItem> _selfVfx = new();
    // 自己人（自己/队友/宠物）→ 固定的状态栏行号。血条搬到屏幕右侧后不再跟随 3D 位置。
    private readonly Dictionary<NCreature, int> _selfStatusRows = new();
    private uint _reservedMask;
    private uint _hiddenLayer;
    private uint _originalMask;
    private uint[] _enemyLayers = [];
    private Viewport? _source;
    private SubViewport? _stage;
    private TextureRect? _screen;
    private Node2D? _overlay;
    private Camera3D? _camera;
    private bool _disposed;
    private WeaponPresentation? _weapon;
    private RoomPresentation? _backdrop;
    private uint _backgroundLayer;
    private uint _foregroundLayer;
    private readonly Dictionary<NCreature, Vector2> _anchors = new();
    private readonly Dictionary<NCreature, int> _companionSlots = new();
    private readonly Dictionary<CanvasItem, bool> _visibility = new();
    // 开局收敛期间临时隐藏的界面，独立于 _visibility，避免污染关闭时的还原状态。
    private readonly List<CanvasItem> _settleHidden = new();
    private readonly OpeningSettle _settle = new();
    private bool _settled;
    // 视角：按住右键拖动才转。松开停住，双击右键复位。
    private bool _looking;
    private bool _rightReleasedOnce;
    private ulong _rightReleaseTime;
    private Vector2 _lookStartMouse;
    private Vector2 _lookStartAngle;
    private Vector2 _lookAngle;
    internal static PresentationSession? Current { get; private set; }

    internal void Attack() => _weapon?.Attack();

    // 纸片怪物：Sprite3D + FixedY Billboard，永远面向玩家但不会上下翻倒。
    // 这是用户拍板的方案 2：保留 3D 房间，怪物不再做轮廓挤出立体网格。
    private sealed record EnemyView(NCreature Creature, uint Layer, SubViewport Capture, Sprite3D Sprite)
    {
        internal Vector2 Target;
        internal bool HasTarget;
        internal Rect2 SourceHitbox;
        internal Vector3[] HitCorners = [];
    }

    public void Start()
    {
        var creatures = room.CreatureNodes.Where(Valid).ToArray();
        var enemies = creatures.Where(c => c.Entity.IsEnemy).ToArray();
        if (enemies.Length == 0) throw new InvalidOperationException("当前没有敌人。");
        _source = room.GetViewport();
        if (enemies.Any(c => c.GetCanvas() != _source.FindWorld2D().Canvas))
            throw new InvalidOperationException("原型暂不支持独立 CanvasLayer 中的敌人。");
        _originalMask = _source.CanvasCullMask;
        uint occupied = 0;
        foreach (var item in Descendants(_source).OfType<CanvasItem>())
            occupied |= item.VisibilityLayer;
        var freeCount = Enumerable.Range(20, 12).Count(bit => (occupied & (1u << bit)) == 0);
        if (freeCount < 4) throw new InvalidOperationException("没有足够的空闲展示层。");
        var layers = FrontLayout.ReserveLayers(occupied, freeCount);
        if (layers.Length < creatures.Length - 1 + 3)
            throw new InvalidOperationException("没有足够的空闲展示层。");
        _enemyLayers = layers[..^3];
        _reservedMask = layers.Aggregate(0u, (mask, bit) => mask | bit);
        _hiddenLayer = layers[^1];
        _backgroundLayer = layers[^2];
        _foregroundLayer = layers[^3];

        // ★★ 把游戏的 UI 层（手牌、顶栏、选牌弹窗）明确提到最上层。
        //   游戏源码里只给 SceneContainer(-10) 和 CombatVfxContainer(-9) 设了 ZIndex，
        //   UI 用的是默认值 0 —— 理论上我们的展示层(-8)在它下面，
        //   但手牌多次「数据正常却看不见」，所以不再依赖理论值：
        //   直接给 UI 层一个远高于展示层的 ZIndex，从机制上保证它画在最上面。
        //   这是纯 ZIndex 调整：不挪动节点、不改可见层、不动可见性，风险最低。
        if (Valid(room.Ui))
        {
            _originalUiZ = room.Ui.ZIndex;
            room.Ui.ZIndex = 200;
        }
        _stage = new SubViewport
        {
            Name = "FirstPersonStage", OwnWorld3D = true, World2D = new World2D(),
            TransparentBg = true, GuiDisableInput = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        owner.AddChild(_stage);
        _camera = new Camera3D { Position = new Vector3(0, 1.3f, 0), Fov = 60, Current = true };
        _stage.AddChild(_camera);
        var character = creatures.FirstOrDefault(c => LocalContext.IsMe(c.Entity))
            ?.Entity.Player?.Character.GetType().Name ?? "Ironclad";
        // 展示层是一张覆盖全屏的贴图，层级必须【夹在中间】：
        //   原版场景/背景 -10  <  展示层 -8  <  原版特效 -9?  <  手牌与 UI 0
        // - 太小（比如 -12）会跑到原版背景下面 → 背景把怪物整个盖住（用户实测「怪被挡住了」）。
        // - 太大（比如 0）会跟手牌同级、后画盖住手牌（用户实测「手牌看不见」）。
        // 取 -8：在原版场景之上、手牌之下。原版特效 -9 仍在展示层之下，特效不受影响。
        _screen = new TextureRect
        {
            Name = "FirstPersonScreen", Texture = _stage.GetTexture(),
            MouseFilter = Control.MouseFilterEnum.Ignore, ZIndex = -8,
            StretchMode = TextureRect.StretchModeEnum.Scale
        };
        _overlay = new Node2D { Name = "FirstPersonOverlay", ZIndex = -8 };
        room.AddChild(_overlay);
        _overlay.AddChild(_screen);
        _weapon = new WeaponPresentation(_overlay, character);
        if (Valid(room.Background))
        {
            if (room.Background.GetCanvas() != _source.FindWorld2D().Canvas)
                throw new InvalidOperationException("背景使用独立画布，暂不支持房间展示。");
            SplitBackground();
            var act = creatures.FirstOrDefault(c => LocalContext.IsMe(c.Entity))
                ?.Entity.Player?.RunState.Act.GetType().Name ?? "Overgrowth";
            _backdrop = new RoomPresentation(owner, _stage, _source, _backgroundLayer, _foregroundLayer, act);
        }
        Resize();
        for (int index = 0; index < enemies.Length; index++)
        {
            var creature = enemies[index];
            RememberAnchor(creature);
            var capture = new SubViewport
            {
                Name = $"EnemyCapture{index}", World2D = _source.FindWorld2D(),
                Disable3D = true, TransparentBg = true, GuiDisableInput = true,
                CanvasCullMask = layers[index],
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always
            };
            _captures.Add(capture);
            owner.AddChild(capture);
            // 纸片怪物：捕获贴图直接贴到 Sprite3D 上，只绕 Y 轴面向玩家。
            // 方案 2 已拍板：不再做逐像素轮廓挤出，稳定优先。
            var sprite = new Sprite3D
            {
                Name = $"EnemySprite{index}",
                Texture = capture.GetTexture(),
                Billboard = BaseMaterial3D.BillboardModeEnum.FixedY,
                Shaded = false,
                Transparent = true,
                AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear
            };
            _stage!.AddChild(sprite);
            _enemies.Add(new EnemyView(creature, layers[index], capture, sprite));
        }
        // 玩家隐藏身体与血量显示，格挡、能力和原逻辑保留。
        foreach (var ally in creatures.Where(c => !c.Entity.IsEnemy))
        {
            if (LocalContext.IsMe(ally.Entity))
            {
                IsolateLayerOnly(ally.Visuals, _hiddenLayer);
                foreach (var bar in Descendants(ally).OfType<NHealthBar>())
                {
                    _visibility.TryAdd(bar.HpBarContainer, bar.HpBarContainer.Visible);
                    bar.HpBarContainer.Visible = false;
                    HideHpLabel(bar);
                }
            }
        }
        _source.CanvasCullMask &= ~_reservedMask;
        Update();
        Current = this;
    }

    public void Update(double delta = 0)
    {
        // 游戏在 0.6.0 出现闪退。C# 异常若从 ProcessFrame 里冒出去，会让整个进程挂掉。
        // 这里兜住并把堆栈写到用户主目录，方便定位；同时把这次展示停掉，避免每帧反复抛。
        try { UpdateCore(delta); }
        catch (Exception error)
        {
            ReportCrash(error);
            Dispose();
        }
    }

    private bool _crashReported;

    private void ReportCrash(Exception error)
    {
        if (_crashReported) return;
        _crashReported = true;
        try
        {
            var path = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "FirstPersonSpire-崩溃.txt");
            var text = DateTime.Now.ToString("O")
                + "\n" + error
                + "\n\n--- 堆栈 ---\n" + error.StackTrace + "\n";
            System.IO.File.WriteAllText(path, text);
            GD.Print($"[FirstPersonSpire] 崩溃已记录：{path}");
        }
        catch { }
        GD.PushError($"FirstPersonSpire 更新异常：{error}");
    }

    private void UpdateCore(double delta)
    {
        if (_disposed || _source == null || _stage == null || _camera == null) return;
        Resize();
        _backdrop?.Update();
        // 先还原上一帧借用的界面坐标，捕获始终以原动画空间为准。
        foreach (var projection in _ui.Values) projection.Restore();
        SplitBackground();
        var protectedTargets = new List<Rect2>();
        foreach (var ally in room.CreatureNodes.Where(Valid).Where(c => !c.Entity.IsEnemy))
        {
            if (LocalContext.IsMe(ally.Entity))
            {
                IsolateLayerOnly(ally.Visuals, _hiddenLayer);
                RememberAnchor(ally);
                // 玩家身体不展示，但仍保留原目标牌与格挡/状态界面；
                // 血条不在这里摆，统一交给 DockSelfStatusBars（自己/队友/宠物一起）。
                continue;
            }
        }
        DockSelfStatusBars();
        // 视角交给「按住右键拖动」。用户实测：鼠标直接映射朝向会让打牌时画面一直晃。
        // 松开右键就停在当前朝向；想回正就双击右键复位（也可以拖回去）。
        var mouse = _source.GetMousePosition();
        bool rightHeld = Input.IsMouseButtonPressed(MouseButton.Right);
        if (rightHeld)
        {
            if (!_looking)
            {
                _looking = true;
                _lookStartMouse = mouse;
                _lookStartAngle = _lookAngle;
            }
            var drag = mouse - _lookStartMouse;
            // 拖过半个屏幕宽度约转 180°；上下限幅小一些更自然。
            _lookAngle = new Vector2(
                _lookStartAngle.X + drag.X / Math.Max(1f, _stage.Size.X) * Mathf.Pi * 2f,
                Mathf.Clamp(_lookStartAngle.Y + drag.Y / Math.Max(1f, _stage.Size.Y) * 1.2f, -1.1f, 1.1f));
        }
        else
        {
            if (_looking && _rightReleasedOnce)
            {
                // 双击右键复位朝向，方便打牌时快速回正。
                if (Time.GetTicksMsec() - _rightReleaseTime < 320) _lookAngle = Vector2.Zero;
                _rightReleasedOnce = false;
            }
            else if (_looking)
            {
                _rightReleasedOnce = true;
                _rightReleaseTime = Time.GetTicksMsec();
            }
            _looking = false;
        }
        var look = _lookAngle;
        _camera.Rotation = Vector3.Zero;
        // 本体先从 CreatureNodes 移走，再等待 DeathAnimationTask；退场队列必须一起展示。
        var live = room.CreatureNodes.Concat(room.RemovingCreatureNodes).Distinct().Where(Valid)
            .Where(c => !LocalContext.IsMe(c.Entity)).ToArray();
        SyncEnemies(live);
        var enemyCount = _enemies.Count(e => e.Creature.Entity.IsEnemy);
        int enemyIndex = 0;
        for (int i = 0; i < _enemies.Count; i++)
        {
            var entry = _enemies[i];
            var creature = entry.Creature;
            // ★ 用「只设层 + 祖先补层」，不挪动节点。
            //   实测对比：补层方案捕获到 6.1% 不透明像素（正常），
            //   挪进捕获容器只有 0.87%（位置偏移 + 打断动画）。
            IsolateLayerOnly(creature.Visuals, entry.Layer);
            var target = creature.Entity.IsEnemy ? new Vector2(
                FrontLayout.CenterX(enemyIndex++, enemyCount, _stage.Size.X), _stage.Size.Y * 0.50f) :
                CompanionTarget(creature);
            if (room.RemovingCreatureNodes.Contains(creature) && entry.HasTarget)
                target = entry.Target;
            entry.Target = target;
            entry.HasTarget = true;
            // 原身体可能在移动、缩放；将原点击框相对固定身体参考点的偏移投影出来。
            var shift = target - CanvasAnchor(creature);
            entry.SourceHitbox = CanvasRect(creature.Hitbox);
            // ★ 捕获范围只按点击框算会切掉换皮肤后的大帽子/装饰。
            // 取「点击框 ∪ %Bounds 实际显示范围」再留一圈余量（算法在 Core/FrontLayout.CaptureRect）。
            var displayBounds = DisplayBounds(creature);
            var box = FrontLayout.CaptureRect(
                (entry.SourceHitbox.Position.X, entry.SourceHitbox.Position.Y,
                    entry.SourceHitbox.Size.X, entry.SourceHitbox.Size.Y),
                displayBounds);
            var bounds = new Rect2(box.X, box.Y, box.W, box.H);
            float factor = Math.Min(1f, 1024f / Math.Max(bounds.Size.X, bounds.Size.Y));
            entry.Capture.Size = new Vector2I(
                Math.Max(2, (int)Math.Ceiling(bounds.Size.X * factor)),
                Math.Max(2, (int)Math.Ceiling(bounds.Size.Y * factor)));
            // 使用同一世界的原动画；截取区以主画面坐标确定。
            entry.Capture.CanvasTransform = new Transform2D(0, -bounds.Position) *
                _source.CanvasTransform;
            entry.Capture.GlobalCanvasTransform = new Transform2D(0, Vector2.Zero).Scaled(Vector2.One * factor);
            float depth = creature.Entity.IsEnemy ? 5f : 3.8f;
            var center = _camera.ProjectPosition(bounds.GetCenter() + shift, depth);
            entry.Sprite.SetPosition(center);
            // 3D 投影严格匹配原点击框尺寸，避免“点到旁边的怪”。
            float unitPerPixel = _camera.ProjectPosition(bounds.GetCenter() + shift + Vector2.Right, depth)
                .DistanceTo(center);
            // Sprite3D 的 PixelSize 是「贴图 1 像素 = 多少世界单位」，直接用 unitPerPixel。
            // 之前写成 1/unitPerPixel，把 590px 的怪放大成十万世界单位，直接糊满/闪屏。
            entry.Sprite.PixelSize = Math.Max(0.0001f, unitPerPixel);
            var hit = new Rect2(entry.SourceHitbox.Position + shift, entry.SourceHitbox.Size);
            entry.HitCorners = Corners(hit).Select(p => _camera.ProjectPosition(p, depth)).ToArray();
        }
        _camera.Rotation = new Vector3(-look.Y, -look.X, 0);
        // 转头后原点击区域同步投影位置，怪物和召唤物使用相同坐标路径。
        foreach (var entry in _enemies)
        {
            var points = entry.HitCorners.Select(p => _camera.UnprojectPosition(p)).ToArray();
            var min = points.Aggregate(Vector2.Inf, (a, b) => a.Min(b));
            var max = points.Aggregate(-Vector2.Inf, (a, b) => a.Max(b));
            var projectedHit = new Rect2(min, max - min);
            ShiftUi(entry.Creature, projectedHit.GetCenter() - entry.SourceHitbox.GetCenter());
            Projection(entry.Creature.Hitbox).Fit(projectedHit);
            var reticle = entry.Creature.GetNodeOrNull<Control>("%SelectionReticle");
            if (Valid(reticle)) Projection(reticle!).Fit(projectedHit);
            protectedTargets.Add(projectedHit.Grow(16));
        }
        _weapon?.Update(new Vector2(_stage.Size.X, _stage.Size.Y), delta, protectedTargets, mouse);
        AdoptCombatVfx(); // 收编本帧新出现的伤害数字/受击/格挡/治疗/死亡特效。
        _source.CanvasCullMask &= ~_reservedMask;
        SettleOpeningFrame();
        DiagnoseOnce();
    }

    // 手牌连续两轮没修好，不再靠猜。把手牌节点的真实状态打一次日志：
    // 可见性、所属层、画布遮罩、全局变换 —— 看它到底是被谁挡了还是被搬走了。
    private bool _diagnosed;

    private int _diagnoseFrame;

    private void DiagnoseOnce()
    {
        if (_diagnosed) return;
        // ⚠️ 不能第一帧就抓：那时 SubViewport 还没渲染出内容，拿到的全是空图，
        // 会让人误判成「网格是空的」。等约 90 帧后再取样。
        if (++_diagnoseFrame < 90) return;
        _diagnosed = true;
        var lines = new List<string>
        {
            $"生物 {_enemies.Count} 个，相机 {_camera!.Position}，舞台 {_stage!.Size}",
            $"主画布遮罩={_source!.CanvasCullMask} 预留={_reservedMask} 原始={_originalMask}",
            $"开局收敛：Settled={_settle.Settled} 临时隐藏UI数={_settleHidden.Count}"
        };
        // 手牌挂在 room.Ui 下面，不在 room 直系，之前找错地方了。
        var hand = room.Ui?.GetNodeOrNull<Control>("%Hand");
        lines.Add(hand == null ? "手牌：找不到 %Hand"
            : $"手牌：Visible={hand.Visible} VisibleInTree={hand.IsVisibleInTree()} " +
              $"Layer={hand.VisibilityLayer} 位置={hand.GlobalPosition} 尺寸={hand.Size} " +
              $"子节点={hand.GetChildCount()}");
        foreach (var entry in _enemies)
        {
            // ★ 把每个捕获视口拍到的画面直接存成 PNG。
            //   这比描述「看到几只怪」精确得多 —— 能直接看出捕获里到底混进了什么。
            try
            {
                var shot = entry.Capture.GetTexture()?.GetImage();
                if (shot != null)
                {
                    var dir = System.IO.Path.Combine(
                        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                        "FirstPersonSpire-捕获");
                    System.IO.Directory.CreateDirectory(dir);
                    string safe = string.Join("_", entry.Creature.Name.ToString()
                        .Split(System.IO.Path.GetInvalidFileNameChars()));
                    shot.SavePng(System.IO.Path.Combine(dir, $"捕获_{safe}.png"));
                    lines.Add($"捕获图已存：捕获_{safe}.png");
                }
            }
            catch (Exception error) { lines.Add($"捕获图保存失败：{error.Message}"); }
            // 挪动是否改变了显示对象与点击框的相对关系？这是「拍到空白」的直接判据。
            var visuals = entry.Creature.Visuals;
            // ★ 关键判据：Visuals 的【实际层】是否等于捕获层。
            //   若不等于，原版身体就不会被主画面剔除 → 与展示层的怪叠在一起（用户实测「两只重叠」）。
            //   同时看祖先链每一层的值，定位补层在哪一级断掉。
            var chain = new List<string>();
            for (Node? n = visuals; n != null && n is not Viewport && chain.Count < 6; n = n.GetParent())
                if (n is CanvasItem ci) chain.Add($"{ci.Name}={ci.VisibilityLayer}");
            // ★ 手牌被谁盖住？打印手牌与展示层的绘制顺序（ZIndex 链）与画布归属。
            {
                var handNode = room.Ui?.GetNodeOrNull<Control>("%Hand");
                if (handNode != null)
                {
                    var zchain = new List<string>();
                    for (Node? n = handNode; n != null && n is not Viewport && zchain.Count < 7; n = n.GetParent())
                        if (n is CanvasItem ci)
                            zchain.Add($"{ci.Name}(z={ci.ZIndex})");
                    lines.Add($"手牌Z链：{string.Join(" → ", zchain)}");
                }
                lines.Add($"展示层：overlay z={_overlay?.ZIndex} " +
                    $"screen z={_screen?.ZIndex} 尺寸={_screen?.Size} 位置={_screen?.GlobalPosition}");
                lines.Add($"UI层 ZIndex={room.Ui?.ZIndex}（应远高于展示层的 {_overlay?.ZIndex}）");
            }
            lines.Add($"位置：Visuals父={visuals.GetParent()?.Name} " +
                $"Visuals层={visuals.VisibilityLayer}（应为 {entry.Layer}） " +
                $"主遮罩={(visuals.VisibilityLayer & _source!.CanvasCullMask)}（应为 0 才不显示原版）");
            lines.Add($"展示链：{string.Join(" → ", chain)}");
            lines.Add($"纸片 {entry.Creature.Name}：纹理={entry.Sprite.Texture?.GetSize()} " +
                $"PixelSize={entry.Sprite.PixelSize} 位置={entry.Sprite.Position} " +
                $"Billboard={entry.Sprite.Billboard} 可见={entry.Sprite.Visible} 层={entry.Layer}");
        }
        try
        {
            var path = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "FirstPersonSpire-诊断.txt");
            System.IO.File.WriteAllLines(path, lines);
            GD.Print($"[FirstPersonSpire] 诊断已写入 {path}");
        }
        catch (Exception error) { GD.PushWarning($"诊断写入失败：{error}"); }
        foreach (var line in lines) GD.Print($"[FirstPersonSpire] 诊断 {line}");
    }

    // 读该生物的「实际显示范围」。
    // 本体把 %Bounds 当作显示容器（NCreature.UpdateBounds 从它反推 Hitbox），
    // 换皮肤后更大的帽子、装饰、武器都算在里面；点击框则只覆盖可点击的部分。
    // 拿不到就返回 null，退化成只用点击框 + 余量。
    private (float X, float Y, float W, float H)? DisplayBounds(NCreature creature)
    {
        var visuals = creature.Visuals;
        if (!Valid(visuals)) return null;
        var node = visuals.GetNodeOrNull<Control>("%Bounds");
        if (!Valid(node)) return null;
        var rect = CanvasRect(node!);
        if (rect.Size.X <= 0 || rect.Size.Y <= 0) return null;
        return (rect.Position.X, rect.Position.Y, rect.Size.X, rect.Size.Y);
    }

    // 隔离的边界：房间下层的「内容容器」。结构（本体 NCombatRoom._Ready）：
    //   room
    //   ├─ %AllyContainer      ← 我方（玩家、宠物）
    //   ├─ %EnemyContainer     ← 敌方
    //   ├─ %CombatUi           ← 手牌、顶栏、按钮（绝不能碰）
    //   ├─ %CombatVfxContainer ← 战斗特效
    //   └─ %BgContainer        ← 背景
    // 给祖先补可见层时补到这几个容器为止；再往上就是整场共用的 room。
    // 碰到 room 会把手牌/顶栏一起剔出主画面
    // —— 用户实测「人物和宠物全看不见」「牌卡在左上角」就是这么来的。
    private bool IsIsolationBoundary(Node node) =>
        node == room.GetNodeOrNull<Control>("%AllyContainer") ||
        node == room.GetNodeOrNull<Control>("%EnemyContainer") ||
        node == room.GetNodeOrNull<Control>("%BgContainer") ||
        node == room.GetNodeOrNull<Control>("%CombatVfxContainer") ||
        node == room.GetNodeOrNull<Control>("%BackCombatVfxContainer");

    // ★ 把展示对象的显示节点「挪进干净容器」再拍。
    //
    // 为什么不再改祖先层：Godot 的可见层是「自己 与 所有祖先 按位与」，
    // 所以祖先必须一路 |= 才能被拍到；但祖先（一直到 room）也在这一层之后，
    // 它底下别的分支只要碰巧带这层就会被一起抓进去 —— 用户实测「手牌看不见 / 抓的东西太多」。
    //
    // 改成把 Visuals 挂到我们自己的容器下：容器祖先链很短且全是本模组的节点，
    // 给它设捕获层即可，**背景和 UI 根本进不来**，也不用去动 room 的层。
    // 只设层、**不挪动节点**：给背景这类「挪了位置就会偏」的东西用。
    // 祖先链必须一路 |= 本层，否则按位与后为 0 谁也拍不到（探针实测）。
    // 之所以安全：给 room 带上本层后，room 底下别的分支（手牌等）自身不含本层，
    // 按位与仍是 0，因此不会被误拍进去。
    private void IsolateLayerOnly(CanvasItem? root, uint layer)
    {
        if (!Valid(root)) return;
        foreach (var item in Descendants(root!).OfType<CanvasItem>())
        {
            Save(item);
            item.VisibilityLayer = layer;
        }
        for (Node? parent = root!.GetParent();
             parent != null && parent is not Viewport;
             parent = parent.GetParent())
        {
            if (parent is not CanvasItem canvas) continue;
            Save(canvas);
            canvas.VisibilityLayer |= layer;
        }
    }

    private void Save(CanvasItem item) => _originalLayers.TryAdd(item, item.VisibilityLayer);

    private static Rect2 CanvasRect(Control control)
    {
        var transform = control.GetGlobalTransformWithCanvas();
        var corners = new[] { Vector2.Zero, new Vector2(control.Size.X, 0), control.Size,
            new Vector2(0, control.Size.Y) }.Select(p => transform * p).ToArray();
        var min = corners.Aggregate(Vector2.Inf, (a, b) => a.Min(b));
        var max = corners.Aggregate(-Vector2.Inf, (a, b) => a.Max(b));
        return new Rect2(min, max - min);
    }

    private static IEnumerable<Node> Descendants(Node root)
    {
        yield return root;
        foreach (var child in root.GetChildren())
            foreach (var nested in Descendants(child)) yield return nested;
    }

    private static bool Valid(GodotObject? node) => node != null && GodotObject.IsInstanceValid(node);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (Current == this) Current = null;
        Restore(() => _weapon?.Dispose());
        Restore(() => _backdrop?.Dispose());
        foreach (var projection in _ui.Values) Restore(projection.Restore);
        foreach (var pair in _visibility)
            if (Valid(pair.Key)) Restore(() => pair.Key.Visible = pair.Value);
        foreach (var pair in _originalLayers)
            if (Valid(pair.Key)) Restore(() => pair.Key.VisibilityLayer = pair.Value);
        if (Valid(room.Ui)) Restore(() => room.Ui.ZIndex = _originalUiZ);
        // 即使某个节点已经销毁，也继续恢复其余节点，不让一次清理失败中断整场恢复。
        if (Valid(_source)) Restore(() =>
            _source!.CanvasCullMask = FrontLayout.RestoreMask(_source.CanvasCullMask, _reservedMask, _originalMask));
        if (Valid(_screen)) Restore(() => { _screen!.Visible = false; _screen.QueueFree(); });
        if (Valid(_overlay)) Restore(() => _overlay!.QueueFree());
        foreach (var capture in _captures)
            if (Valid(capture)) Restore(() =>
            { capture.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled; capture.QueueFree(); });
        if (Valid(_stage)) Restore(() =>
        { _stage!.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled; _stage.QueueFree(); });
    }


    private void SyncEnemies(NCreature[] live)
    {
        foreach (var entry in _enemies.Where(e => !live.Contains(e.Creature)).ToArray())
        {
            if (Valid(entry.Creature)) IsolateLayerOnly(entry.Creature.Visuals, _hiddenLayer);
            entry.Sprite.QueueFree();
            entry.Capture.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
            entry.Capture.QueueFree();
            _captures.Remove(entry.Capture);
            _enemies.Remove(entry);
            _companionSlots.Remove(entry.Creature);
        }
        foreach (var creature in live.Where(c => !_enemies.Any(e => e.Creature == c)))
        {
            var layer = FrontLayout.AvailableLayer(_enemyLayers, _enemies.Select(e => e.Layer));
            // 极端情况下层位不足，额外生物保留本体显示，不关闭第一人称。
            if (layer == 0) continue;
            if (creature.GetCanvas() != _source!.FindWorld2D().Canvas) continue;
            RememberAnchor(creature);
            var capture = new SubViewport
            {
                World2D = _source!.FindWorld2D(), Disable3D = true,
                TransparentBg = true, GuiDisableInput = true, CanvasCullMask = layer,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always
            };
            _captures.Add(capture);
            owner.AddChild(capture);
            var sprite = new Sprite3D
            {
                Name = $"EnemySprite{_enemies.Count}",
                Texture = capture.GetTexture(),
                Billboard = BaseMaterial3D.BillboardModeEnum.FixedY,
                Shaded = false,
                Transparent = true,
                AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
                TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear
            };
            _stage!.AddChild(sprite);
            _enemies.Add(new EnemyView(creature, layer, capture, sprite));
        }
    }

    private void RememberAnchor(NCreature creature)
    {
        // 固定身体参考点，不把动画过程中变化的点击框中心当作角色的站位。
        _anchors.TryAdd(creature, creature.GetGlobalTransform().AffineInverse() *
            (creature.Hitbox.GetGlobalTransform() * (creature.Hitbox.Size * 0.5f)));
    }

    private Vector2 CanvasAnchor(NCreature creature) =>
        creature.GetGlobalTransformWithCanvas() * _anchors[creature];

    private static Vector2[] Corners(Rect2 rect) => [rect.Position,
        rect.Position + new Vector2(rect.Size.X, 0), rect.End,
        rect.Position + new Vector2(0, rect.Size.Y)];

    private UiProjection Projection(Control control)
    {
        if (!_ui.TryGetValue(control, out var projection))
            _ui.Add(control, projection = new UiProjection(control));
        return projection;
    }

    private void ShiftUi(NCreature creature, Vector2 shift)
    {
        // 自己人（含宠物）的血条已经固定在状态栏，不能再跟着 3D 位置平移，
        // 否则会一边停在栏里、一边又被挪走。
        bool docked = !creature.Entity.IsEnemy && _selfStatusRows.ContainsKey(creature);
        foreach (var control in new[] { creature.IntentContainer,
            docked ? null : creature.GetNodeOrNull<Control>("%HealthBar"), creature.OrbManager })
            if (Valid(control)) Projection(control!).Shift(shift);
        if (LocalContext.IsMe(creature.Entity))
        {
            Projection(creature.Hitbox).Shift(shift);
            var reticle = creature.GetNodeOrNull<Control>("%SelectionReticle");
            if (Valid(reticle)) Projection(reticle!).Shift(shift);
        }
    }

    private void SplitBackground()
    {
        if (!Valid(room.Background)) return;
        IsolateLayerOnly(room.Background, _backgroundLayer);
        var layers = room.Background.GetChildren()
            .Where(n => n.Name.ToString().StartsWith("Layer_", StringComparison.Ordinal)).ToArray();
        foreach (var node in layers.Skip(Math.Max(1, (layers.Length + 1) / 2))
            .Concat(room.Background.GetChildren().Where(n => n.Name == "Foreground")))
            if (node is CanvasItem item) IsolateLayerOnly(item, _foregroundLayer);
    }

    private Vector2 CompanionTarget(NCreature creature)
    {
        if (!_companionSlots.TryGetValue(creature, out var slot))
        {
            slot = Enumerable.Range(0, _enemyLayers.Length)
                .First(i => !_companionSlots.ContainsValue(i));
            _companionSlots.Add(creature, slot);
        }
        var (x, y) = FrontLayout.CompanionSlot(slot, _stage!.Size.X, _stage.Size.Y);
        return new Vector2(x, y);
    }

    private void DockSelfVfx(NCreature ally, Vector2 barTopLeft)
    {
        foreach (var item in _selfVfx.ToArray())
        {
            if (!GodotObject.IsInstanceValid(item)) { _selfVfx.Remove(item); continue; }
            if (!_vfxCreature.TryGetValue(item, out var ownerCreature) || ownerCreature != ally) continue;
            var target = new Vector2(barTopLeft.X - 6f, barTopLeft.Y - 34f);
            if (item is Control control)
            {
                var parent = control.GetGlobalTransformWithCanvas() *
                    control.GetTransform().AffineInverse();
                control.Position = parent.AffineInverse() * target;
            }
            else if (item is Node2D node2d) node2d.GlobalPosition = target;
        }
    }

    private void DockSelfStatusBars()
    {
        foreach (var ally in room.CreatureNodes.Where(Valid).Where(c => !c.Entity.IsEnemy))
        {
            if (!_selfStatusRows.TryGetValue(ally, out int row))
            {
                row = _selfStatusRows.Count;
                _selfStatusRows.Add(ally, row);
            }
            var (x, y) = FrontLayout.SelfStatusSlot(row, _stage!.Size.X, _stage.Size.Y);
            DockSelfVfx(ally, new Vector2(x, y));
            foreach (var bar in Descendants(ally).OfType<NHealthBar>())
            {
                if (!Valid(bar)) continue;
                _visibility.TryAdd(bar.HpBarContainer, bar.HpBarContainer.Visible);
                bar.HpBarContainer.Visible = true;
                var label = bar.GetNodeOrNull<CanvasItem>("%HpLabel");
                if (Valid(label))
                {
                    _visibility.TryAdd(label!, label!.Visible);
                    label!.Visible = true;
                }
                Projection(bar.HpBarContainer).MoveTo(new Vector2(x, y));
            }
        }
    }

    private void HideHpLabel(NHealthBar bar)
    {
        var label = bar.GetNodeOrNull<CanvasItem>("%HpLabel");
        if (!Valid(label)) return;
        _visibility.TryAdd(label!, label!.Visible);
        label.Visible = false;
    }

    private void Resize()
    {
        // 展示层尺寸沿用游戏逻辑分辨率对应的可见矩形。
        // ⚠️ 曾试过改成窗口像素尺寸（2560x1600），但那会同时改变 3D 渲染分辨率与投影比例，
        //    牵连 unitPerPixel、背景面片比例等一串计算，风险大。
        //    先只修「挪动节点导致位置偏移」这一件，尺寸问题单独验证后再动。
        var size = _source!.GetVisibleRect().Size;
        _stage!.Size = new Vector2I(Math.Max(2, (int)size.X), Math.Max(2, (int)size.Y));
        _screen!.Size = size;
        _overlay!.GlobalTransform = _overlay.GetCanvasTransform().AffineInverse();
    }

    private void AdoptCombatVfx()
    {
        foreach (var container in new[] { room.CombatVfxContainer, room.BackCombatVfxContainer })
        {
            if (!Valid(container)) continue;
            foreach (var child in container!.GetChildren())
            {
                if (child is not CanvasItem item) continue;
                if (_vfxOwner.ContainsKey(item)) continue;
                var ownerView = NearestOwner(item);
                if (ownerView == null) continue;
                // 特效同样【不能挪动】：换父节点会触发 _ExitTree/_EnterTree，
                // 打断它自己的动画与销毁计时器 —— 用户实测「卡牌特效打出后停留在原地」。
                // 改用「只设层 + 祖先补层」，既能被捕获到，又不动它的生命周期。
                IsolateLayerOnly(item, ownerView.Layer);
                _vfxOwner[item] = ownerView.Layer;
                _vfxCreature[item] = ownerView.Creature;
                if (!ownerView.Creature.Entity.IsEnemy) _selfVfx.Add(item);
            }
        }
    }

    private EnemyView? NearestOwner(CanvasItem item)
    {
        if (_enemies.Count == 0) return null;
        var origin = item.GetGlobalTransformWithCanvas().Origin;
        EnemyView? best = null;
        float bestDistance = float.MaxValue;
        foreach (var entry in _enemies)
        {
            if (!Valid(entry.Creature)) continue;
            float distance = DistanceToCreature(entry.Creature, origin);
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = entry;
        }
        return best;
    }

    private float DistanceToCreature(NCreature creature, Vector2 point)
    {
        var display = DisplayBounds(creature);
        if (display is { } d)
        {
            float dx = Math.Max(0f, Math.Max(d.X - point.X, point.X - (d.X + d.W)));
            float dy = Math.Max(0f, Math.Max(d.Y - point.Y, point.Y - (d.Y + d.H)));
            return MathF.Sqrt(dx * dx + dy * dy);
        }
        var body = creature.Visuals.GetCurrentBody();
        return Valid(body) ? point.DistanceTo(body.GlobalPosition) : float.MaxValue;
    }

    // 开局收敛：原版怪物入场会摆位，血条忠实跟随就会被玩家看见「一边滑一边追」。
    // 判定逻辑在 Core/OpeningSettle（可离线检查），这里只负责隐藏与恢复。
    private void SettleOpeningFrame()
    {
        if (_settled) { EnsureUiVisible(); return; }
        var anchors = new List<(float X, float Y)>();
        foreach (var entry in _enemies)
            if (Valid(entry.Creature))
            {
                var anchor = CanvasAnchor(entry.Creature);
                anchors.Add((anchor.X, anchor.Y));
            }
        if (!_settle.Step(anchors)) { HideUiForSettle(); return; }
        _settled = true;
        RestoreUiVisibility();
    }

    // 收敛期间若有新怪物加入，它会带着自己的血条出现；保证它们不被留在隐藏状态。
    private void EnsureUiVisible()
    {
        if (_settleHidden.Count > 0) RestoreUiVisibility();
    }

    private void HideUiForSettle()
    {
        foreach (var entry in _enemies)
        {
            foreach (var control in new[] { entry.Creature.IntentContainer,
                entry.Creature.GetNodeOrNull<Control>("%HealthBar"),
                entry.Creature.GetNodeOrNull<Control>("%SelectionReticle") })
            {
                if (!Valid(control)) continue;
                // 单独记一份，不能写进 _visibility —— 那是关闭时用来还原的。
                _settleHidden.Add(control!);
                control!.Visible = false;
            }
        }
    }

    private void RestoreUiVisibility()
    {
        foreach (var control in _settleHidden)
            if (GodotObject.IsInstanceValid(control)) control.Visible = true;
        _settleHidden.Clear();
    }

    private static void Restore(Action action)
    {
        try { action(); }
        catch (Exception error) { GD.PushWarning($"FirstPersonSpire 单项恢复失败：{error}"); }
    }
}
