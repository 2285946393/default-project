using Godot;
using DouSpireRules;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using System.Collections.Generic;

namespace DouSpire.Table;

/// <summary>
/// 坐在牌桌边的**活角色** —— 不是我们自己画的图，是游戏本体那个带 Spine 骨骼的角色。
///
/// 用户原话（2026-10-07）：「为什么我们不把角色直接放在三对应的地方呢。打出对应的牌型会出现动作。
/// 然后我们只需要设置时机。什么都不用调，他有自己骨骼。然后再配上本来的音效？」
///
/// 所以这一层只干三件事：
/// <list type="number">
///   <item>**把角色摆上去** —— 用游戏自己的 <c>CharacterModel.CreateVisuals()</c>；</item>
///   <item>**在正确的时机喊一声"做这个动作"** —— 动画是游戏现成的，我们不碰骨骼；</item>
///   <item>**放游戏自带的角色音效** —— <c>CharacterModel.AttackSfx / CastSfx / DeathSfx</c>。</item>
/// </list>
///
/// ── API 都是从 sts2.dll 反编译确认过的（不是猜的）────────────────────
/// <list type="bullet">
///   <item><c>CharacterModel.CreateVisuals()</c> → <see cref="NCreatureVisuals"/>（**站着**那套，动作最全）</item>
///   <item><c>CharacterModel.RestSiteAnimPath</c> + <c>PreloadManager.Cache.GetScene(path)</c> → <see cref="NRestSiteCharacter"/>（**坐着**那套，兜底用）</item>
///   <item><see cref="NCreatureVisuals.SpineBody"/>（public getter，<c>_Ready</c> 里才赋值）→ <see cref="MegaSprite"/></item>
///   <item><c>SpineNodeExtensions.RunWhenSpineReady(node, sprite, onReady)</c> —— **必须等**，
///         骨骼是异步加载的，提前调 <c>GetAnimationState()</c> 会直接抛异常（dll 注释里写明了）。</item>
///   <item><see cref="MegaAnimationState.SetAnimation(string, bool, int)"/> +
///         <c>AddAnimation(string, float, bool, int)</c> —— 播完自动接回 <c>idle_loop</c>。</item>
/// </list>
///
/// ── 铁律 ──────────────────────────────────────────────────────
/// 任何一步出错都**只记日志，绝不抛出去**。角色加载失败就退回原来的静态立绘
/// （<c>DouSpireTable</c> 里靠 <see cref="Alive"/> 判断），牌桌该怎么玩还怎么玩。
/// </summary>
public sealed class DouSpireActor
{
    // ══════════════ 动画名（从 sts2.dll 的 UTF-16 字符串堆里扫出来的真名）══════════════

    public const string Idle = "idle_loop";

    /// <summary>出牌动作。按"这一手有多横"分档，越横动作越大。</summary>
    private const string AttackSmall = "attack";
    private const string AttackSingle = "attack_single";
    private const string AttackDouble = "attack_double";
    private const string AttackTriple = "attack_triple";
    private const string AttackMulti = "attack_multi";
    private const string AttackBig1 = "attack_big1";
    private const string AttackBig2 = "attack_big2";

    // ⚠️ 这三个名字**不是** CreatureAnimator 的触发器名（那是 "PowerUp" / "Hit" / "Dead"，
    // 只给状态机用）。骨骼里的真名是下面这几个 —— 从 sts2.dll 的字符串堆里核对过：
    //   "power_up" 和 "hit" 在游戏里压根不存在，照抄触发器名会静默退化成 attack / idle。
    // 映射关系见 CharacterModel.AnimationStates：(cast→Cast)、(attack→Attack)、(hurt→Hit)、(cast→PowerUp)。
    private const string PowerUp = "cast";     // 蓄力 / 称王
    /// <summary>蓄力动作的公开别名（牌桌那边直接点名播这个：明牌 / 抢地主 / 报单都拿它亮相）。</summary>
    public const string CastAnim = PowerUp;
    private const string Hit = "hurt";         // 挨一下（要不起）
    private const string Dead = "die";         // 倒下
    private const string Revive = "revive";    // 起势（赢了）

    // ══════════════ 实例 ══════════════

    private readonly CharacterModel _model;
    private Node2D? _node;
    private MegaSprite? _sprite;
    private MegaAnimationState? _state;

    /// <summary>骨骼还没加载好时先记着要播什么，加载好立刻补上。</summary>
    private string _pending = "";

    private float _scale = 1f;
    private bool _mirror;
    private bool _spineFailed;

    /// <summary>给 mod 角色的"换装补丁"用的真 <c>Creature</c>（见 <see cref="TryModVisualsHook"/>）。</summary>
    private Creature? _probe;

    private DouSpireActor(CharacterModel model, Node2D node)
    {
        _model = model;
        _node = node;
    }

    /// <summary>这个角色还活着吗（节点还在树上）。掉了就退回静态立绘。</summary>
    public bool Alive => _node is not null && GodotObject.IsInstanceValid(_node);

    /// <summary>能不能真的做动作（骨骼加载成功才算）。</summary>
    public bool CanAnimate => Alive && _state is not null;

    /// <summary>角色内部 id（<c>ironclad</c> / <c>silent</c> …），日志用。</summary>
    public string Entry
    {
        get
        {
            try { return _model.Id.Entry; } catch { return "?"; }
        }
    }

    // ══════════════ 生成 ══════════════

    /// <summary>
    /// 把一个活角色摆到 <paramref name="parent"/> 上。
    ///
    /// <para><b>本体角色</b>：先试**站着**那套（<c>CreateVisuals()</c>，战斗版，动作最全），
    /// 失败再试**坐着**那套（休息处场景）；两套都失败返回 null → 调用方退回静态立绘。</para>
    ///
    /// <para><b>第三方 mod 的角色</b>（<see cref="IsModCharacter"/>）：见 <see cref="TryModVisualsHook"/>
    /// 与 <c>DouSpireConfig.ModCharsPreferSitting</c> 的说明 ——
    /// mod 的换装补丁挂在我们走不到的 <c>NCreature</c> 上，所以默认额外补一刀，
    /// 补不上还可以拨开关改走"休息处那套"（那条路 mod 是真的补了的）。</para>
    ///
    /// <paramref name="foot"/> 是"脚底"的位置（角色原点在脚上，所以摆的是**下沿**不是中心）。
    /// <paramref name="probeCreature"/> 只在 mod 角色那一条路上用得到（见 <see cref="TryModVisualsHook"/>），
    /// 本体角色可以不传。
    /// </summary>
    public static DouSpireActor? Spawn(Node parent, CharacterModel model, Vector2 foot,
                                       float targetH, bool mirror, Creature? probeCreature = null)
    {
        if (model is null) return null;

        Node2D? node = null;
        string how = "";

        // mod 角色默认还是先试站着那套（外观最全），只在配置要求时改走坐着那套
        var restFirst = DouSpireConfig.ModCharsPreferSitting && IsModCharacter(model);

        if (restFirst)
        {
            node = TrySit(model, out how);
            if (node is null) node = TryStand(model, out how);
        }
        else
        {
            node = TryStand(model, out how);
            if (node is null) node = TrySit(model, out how);
        }

        if (node is null) return null;

        // ③ 挂上树
        try
        {
            parent.AddChild(node);
        }
        catch (System.Exception e)
        {
            DouSpire.Entry.Logger.Warn($"斗地主：{SafeEntry(model)} 角色挂不上树，用静态立绘: {e.Message}");
            try { node.Free(); } catch { /* 已经掉了就算了 */ }
            return null;
        }

        // 挂上树之后 `_Ready` 已经跑过一轮了，这时候看节点树才是最终样子 —— 自检写在这里
        LogVisualDiagnostics(model, node, how);

        var actor = new DouSpireActor(model, node)
        {
            _mirror = mirror,
            _probe = probeCreature,
        };
        actor.Position = foot;
        actor.FitHeight(targetH);
        actor.BindSpine();

        DouSpire.Entry.Logger.Info($"斗地主：活角色就位 —— {SafeEntry(model)}（{how}）"
                          + $" 目标高={targetH:0.#} 缩放={actor._scale:0.###}"
                          + $" 镜像={(mirror ? "是" : "否")}");
        return actor;
    }

    /// <summary>① 站着那套（战斗版）。失败返回 null，理由写进日志。</summary>
    private static Node2D? TryStand(CharacterModel model, out string how)
    {
        how = "战斗版（站着）";
        try
        {
            var n = model.CreateVisuals();
            if (n is null) how = "";
            return n;
        }
        catch (System.Exception e)
        {
            DouSpire.Entry.Logger.Warn($"斗地主：{SafeEntry(model)} 的战斗版角色没起来，改试坐着的: {e.Message}");
            how = "";
            return null;
        }
    }

    /// <summary>
    /// ② 坐着那套（休息处场景）。
    ///
    /// ⚠️ 这一条对**第三方 mod 的角色**特别重要：mod 换掉自己外观的补丁通常挂在
    /// <c>NRestSiteCharacter._Ready</c> 上（Kafka 那个 mod 就是，见 <see cref="TryModVisualsHook"/>），
    /// 而 `_Ready` 只有在**这个节点真的被挂上树**时才会跑 —— 所以这条路是能被触发的。
    /// </summary>
    private static Node2D? TrySit(CharacterModel model, out string how)
    {
        how = "休息处版（坐着）";
        try
        {
            var scene = PreloadManager.Cache.GetScene(model.RestSiteAnimPath);
            var n = scene.Instantiate<NRestSiteCharacter>(PackedScene.GenEditState.Disabled);
            if (n is null) how = "";
            return n;
        }
        catch (System.Exception e)
        {
            DouSpire.Entry.Logger.Warn($"斗地主：{SafeEntry(model)} 两套角色都拿不到，这一席用静态立绘: {e.Message}");
            how = "";
            return null;
        }
    }

    /// <summary>
    /// 这个角色是不是**第三方 mod 加的**（本体角色全都在 <c>MegaCrit.*</c> 命名空间下）。
    ///
    /// 为什么要判这个：本体角色的外观我们直接拿就对了；mod 角色可能把"换装"做在
    /// 我们走不到的钩子上（见 <see cref="TryModVisualsHook"/>），只有它们才需要额外照顾。
    /// 判错了也不会炸 —— 最坏只是多/少跑一次补丁，都是 try/catch 包着的。
    /// </summary>
    public static bool IsModCharacter(CharacterModel model)
    {
        try
        {
            var ns = model.GetType().Namespace ?? "";
            return !ns.StartsWith("MegaCrit.", System.StringComparison.Ordinal);
        }
        catch { return false; }
    }

    /// <summary>
    /// **角色自检**（只写日志，不改任何东西）。
    ///
    /// 记的是"这具身体到底是谁的"：模型类 → 节点类 → 场景资源路径 → 骨骼子节点（带显示/隐藏）。
    /// 用户报「角色没有正确引用。我这边还战士。没有变成卡芙卡」时就是靠这几行定位的 ——
    /// 光看"活角色就位 —— KAFKA-KAFKA_CHARACTER"是看不出来的，那只说明**模型**对，
    /// 说明不了**外观**是从哪个场景来的。
    /// </summary>
    private static void LogVisualDiagnostics(CharacterModel model, Node2D node, string how)
    {
        try
        {
            var spines = new List<string>();
            CollectSpineInfo(node, spines, 0);

            DouSpire.Entry.Logger.Info(
                $"斗地主：角色自检 —— 模型={SafeType(model)} 节点={SafeType(node)}"
                + $" 场景={(string.IsNullOrEmpty(node.SceneFilePath) ? "（代码建的）" : node.SceneFilePath)}"
                + $" 来路={how}"
                + $" 骨骼子节点=[{(spines.Count == 0 ? "一个都没找到" : string.Join(" | ", spines))}]");
        }
        catch (System.Exception e)
        {
            DouSpire.Entry.Logger.Warn($"斗地主：角色自检失败（不影响玩）: {e.Message}");
        }
    }

    /// <summary>
    /// 把节点树里的 SpineSprite 列出来；**只报直接子节点**（那几个才是"谁盖着谁"的关键）。
    ///
    /// ⚠️ 以前是"深度 ≤1 的全列出来" —— 结果 `Visuals` 底下挂着 ≈200 个 `SpineMesh2D`，
    /// 整行日志被刷到几万字符，**真正想看的 `SpineSprite` 反而被截断看不到了**（2026-10-08 实测）。
    /// 现在深度 0 报"名字(类型,子N)"，更深只收骨骼节点，并**在循环里**卡上限。
    /// </summary>
    private static void CollectSpineInfo(Node root, List<string> sink, int depth)
    {
        if (depth > 6 || sink.Count >= 14) return;

        for (var i = 0; i < root.GetChildCount(); i++)
        {
            var c = root.GetChild(i);
            var cls = c.GetClass();

            if (cls == MegaSprite.spineClassName)
            {
                var vis = c is CanvasItem ci && ci.Visible ? "显示" : "隐藏";
                sink.Add($"{vis}:{c.Name}");
            }
            else if (depth == 0)
            {
                var cnt = c.GetChildCount();
                sink.Add(cnt > 0 ? $"{c.Name}({cls},子{cnt})" : $"{c.Name}({cls})");
            }

            if (sink.Count >= 14) return;
            CollectSpineInfo(c, sink, depth + 1);
        }
    }

    private Vector2 Position
    {
        set
        {
            if (_node != null) _node.Position = value;
        }
    }

    /// <summary>
    /// 缩放到想要的高度。<paramref name="targetH"/> 是屏幕上想让它占多高（像素）。
    /// 原始高度取场景里的 <c>%Bounds</c>；量不出来就当 300。
    /// </summary>
    private void FitHeight(float targetH)
    {
        var natural = 300f;
        try
        {
            if (_node is NCreatureVisuals v && v.Bounds != null && v.Bounds.Size.Y > 1f)
                natural = v.Bounds.Size.Y;
        }
        catch (System.Exception) { /* 量不到就用 300 */ }

        if (natural < 40f || natural > 4000f) natural = 300f;

        _scale = Mathf.Clamp(targetH / natural * DouSpireConfig.ActorScale, 0.08f, 8f);
        ApplyTransform();
    }

    private void ApplyTransform()
    {
        if (_node is null) return;
        try
        {
            var s = _mirror ? new Vector2(-_scale, _scale) : new Vector2(_scale, _scale);

            // 战斗版有官方的缩放方法（顺带把色相也处理了），优先走它
            if (_node is NCreatureVisuals v) v.SetScaleAndHue(_scale, 0f);
            else _node.Scale = Vector2.One * _scale;

            // 镜像单独设：SetScaleAndHue 只会给正的
            if (_mirror) _node.Scale = s;
        }
        catch (System.Exception e)
        {
            DouSpire.Entry.Logger.Warn($"斗地主：角色缩放没设上（不影响玩）: {e.Message}");
        }
    }

    /// <summary>左右翻面（让左/右两家"脸朝桌子中间"）。</summary>
    public void SetMirror(bool on)
    {
        if (_mirror == on) return;
        _mirror = on;
        ApplyTransform();
    }

    /// <summary>上下挪一点（脚底位置对不准时用）。</summary>
    public void Nudge(float dy)
    {
        if (_node is null) return;
        _node.Position += new Vector2(0f, dy);
    }

    // ══════════════ 骨骼 ══════════════

    /// <summary>
    /// 找到 Spine 骨骼并**等它加载好**。
    ///
    /// ⚠️ 这里是踩过的坑：Godot 的 <c>_Ready</c> 是自底向上跑的，而 SpineSprite 的骨架是
    /// **异步**加载 —— 所以刚 AddChild 完就去 <c>GetAnimationState()</c> 会抛
    /// "was called before the SpineSprite's skeleton finished initializing"。
    /// 官方给的答案是 <c>RunWhenSpineReady</c>，照用。
    /// </summary>
    private void BindSpine()
    {
        if (_node is null) return;

        try
        {
            // 战斗版有现成的；其它场景自己找 SpineSprite 子节点
            _sprite = _node is NCreatureVisuals v ? v.SpineBody : FindSpine(_node);

            if (_sprite is null)
            {
                _spineFailed = true;
                DouSpire.Entry.Logger.Warn($"斗地主：{Entry} 这具身体里没找到 Spine 骨骼，只当个站着的摆件。");
                return;
            }

            var host = _node;
            var sprite = _sprite;
            host.RunWhenSpineReady(sprite, state =>
            {
                if (!GodotObject.IsInstanceValid(host)) return;
                _state = state;
                DouSpire.Entry.Logger.Info($"斗地主：{Entry} 骨骼就绪，可以动起来了。");
                LogSkeletonDiagnostics();

                // ★ 第三方 mod 角色的换装时机 —— 必须夹在"骨骼就绪"和"播第一个动作"之间
                TryModVisualsHook();

                // 先站好，再把之前攒着的动作补上
                if (!string.IsNullOrEmpty(_pending))
                {
                    var p = _pending;
                    _pending = "";
                    Play(p, false);
                }
                else Play(Idle, true);
            });
        }
        catch (System.Exception e)
        {
            _spineFailed = true;
            DouSpire.Entry.Logger.Warn($"斗地主：{Entry} 骨骼绑定失败（角色还在，只是不动）: {e.Message}");
        }
    }

    /// <summary>
    /// 我们**到底绑到了谁的骨骼**。认人看这张动画表就够：
    /// 本体角色是 <c>idle_loop / attack_single / attack_double / die / hurt / cast</c> 那一套，
    /// mod 角色是它自己的。连包围盒一起记 —— 换过骨骼的话尺寸也会跟着变。
    /// </summary>
    private void LogSkeletonDiagnostics()
    {
        if (_sprite is null) return;
        try
        {
            var skel = _sprite.GetSkeleton();
            Rect2? bounds = skel?.GetBounds();
            var names = skel?.GetData()?.GetAnimationNames();

            var list = "（读不到）";
            if (names is not null)
            {
                var sb = new System.Text.StringBuilder();
                var max = names.Count < 14 ? names.Count : 14;
                for (var i = 0; i < max; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(names[i]);
                }
                if (names.Count > max) sb.Append($" …共{names.Count}个");
                list = sb.ToString();
            }

            DouSpire.Entry.Logger.Info($"斗地主：{Entry} 骨骼自检 —— 包围盒={bounds} 动画表={list}");
        }
        catch (System.Exception e)
        {
            DouSpire.Entry.Logger.Warn($"斗地主：{Entry} 骨骼自检失败（不影响玩）: {e.Message}");
        }
    }

    /// <summary>
    /// **让第三方 mod 的角色有机会把自己的外观装上去。**
    ///
    /// 用户 2026-10-08 报：「角色没有正确引用。我这边还战士。没有变成卡芙卡」。
    /// 查下来根因不在我们这半边：
    /// <list type="bullet">
    ///   <item>Kafka 那个 mod（工坊 3747588119）**自己不打包角色场景** ——
    ///         它的 pck 里 <c>creature_visuals</c> / <c>rest_site</c> / <c>ironclad</c>
    ///         出现次数**全是 0**，只带了一堆贴图和自己的骨骼数据
    ///         （<c>res://Kafka/images/character_anim/spine_anim/kafka_anim.*</c>）；</item>
    ///   <item>所以它的角色是**借本体的壳**（我们量到的 Bounds 和 IRONCLAD 一模一样、
    ///         缩放都是 1.079，而别的角色各不相同 —— 这就是证据）；</item>
    ///   <item>"换成卡芙卡"这一步它做在 Harmony 补丁
    ///         <c>NCreature._Ready</c> / <c>NRestSiteCharacter._Ready</c> 里
    ///         （补丁名 <c>CreatureReadyPatch</c> → <c>ReplaceSpineWithKafka</c> →
    ///          <c>HideSpineRecursive</c> + <c>CreateKafkaSpineContainer</c>）。</item>
    /// </list>
    ///
    /// 而牌桌上我们**不建 <c>NCreature</c>**（也不需要），
    /// 于是那个补丁永远不跑 → 屏幕上留着**没换过的原壳**，看起来就是"还是战士"。
    ///
    /// 这里照 <c>NCreature._Ready</c> 的做法补一刀：调一次
    /// <c>CharacterModel.GenerateAnimator(骨骼, creature)</c>（那个 mod 也补了这个方法）。
    /// **只借它的副作用**，返回的 animator 我们不用（骨骼仍然由我们自己驱动），
    /// 而且只对 mod 角色调 —— 本体角色走原来的路，一点不受影响。
    ///
    /// 补不上也没关系：把 <c>DouSpireConfig.ModCharsPreferSitting</c> 打开，
    /// 改走"休息处那套"，那条路是真的能被 mod 补丁触发的。
    /// </summary>
    private void TryModVisualsHook()
    {
        if (_model is null || _sprite is null) return;
        if (!IsModCharacter(_model)) return;

        if (_probe is null)
        {
            DouSpire.Entry.Logger.Warn($"斗地主：{Entry} 是 mod 角色，但这次没拿到 Creature，"
                                       + "换装补丁可能没跑（外观若不对，把 ModCharsPreferSitting 打开试试）。");
            return;
        }

        try
        {
            var animator = _model.GenerateAnimator(_sprite, _probe);
            DouSpire.Entry.Logger.Info($"斗地主：{Entry} 已按 NCreature 的做法调过 GenerateAnimator"
                                       + $"（返回 {SafeType(animator)}）—— 第三方 mod 的换装补丁就挂这一步。");
        }
        catch (System.Exception e)
        {
            DouSpire.Entry.Logger.Warn($"斗地主：{Entry} 的 GenerateAnimator 没跑成（不影响玩）: {e.Message}");
        }
    }

    private static string SafeType(object? o)
    {
        try { return o is null ? "null" : (o.GetType().FullName ?? o.GetType().Name); }
        catch { return "?"; }
    }

    /// <summary>在一棵节点树里找第一个 SpineSprite。</summary>
    private static MegaSprite? FindSpine(Node root)
    {
        for (var i = 0; i < root.GetChildCount(); i++)
        {
            var child = root.GetChild(i);
            if (child.GetClass() == MegaSprite.spineClassName)
            {
                try { return new MegaSprite(child); }
                catch (System.Exception) { return null; }
            }
            var deeper = FindSpine(child);
            if (deeper != null) return deeper;
        }
        return null;
    }

    // ══════════════ 做动作 ══════════════

    /// <summary>
    /// 播一个动作。播完（非循环动作）自动接回 <c>idle_loop</c> —— **我们只管喊，剩下的它自己来**。
    /// 骨骼没好就先记着，好了立刻补。
    /// </summary>
    public void Play(string anim, bool loop = false)
    {
        if (_spineFailed || _node is null || !GodotObject.IsInstanceValid(_node)) return;

        if (_state is null)
        {
            _pending = anim;
            return;
        }

        try
        {
            var name = Resolve(anim);
            if (name is null) return;

            _state.SetAnimation(name, loop, 0);
            // 非循环动作播完接回待机 —— 不然会卡在最后一帧
            if (!loop && _sprite != null && _sprite.HasAnimation(Idle))
                _state.AddAnimation(Idle, 0f, true, 0);
        }
        catch (System.Exception e)
        {
            DouSpire.Entry.Logger.Warn($"斗地主：{Entry} 动作「{anim}」没播成（不影响玩）: {e.Message}");
        }
    }

    /// <summary>回到待机。一轮打完、或者动作乱了的时候喊一声。</summary>
    public void GoIdle() => Play(Idle, true);

    /// <summary>
    /// 动作名兜底：这一具骨骼不一定有喊的那个动作（角色之间动画表不一样），
    /// 没有就一级一级往下退，实在没有返回 null。
    /// </summary>
    private string? Resolve(string anim)
    {
        if (_sprite is null) return anim;

        string[] chain = anim switch
        {
            AttackSingle or AttackDouble or AttackTriple or AttackMulti
                => new[] { anim, AttackSmall, Idle },
            AttackBig1 or AttackBig2
                => new[] { anim, AttackMulti, AttackSmall, Idle },
            PowerUp => new[] { PowerUp, AttackSmall, Idle },
            Dead => new[] { Dead, Hit, Idle },
            Revive => new[] { Revive, PowerUp, Idle },
            _ => new[] { anim, AttackSmall, Idle },
        };

        foreach (var n in chain)
            if (_sprite.HasAnimation(n)) return n;

        return null;
    }

    // ══════════════ 牌型 → 动作 ══════════════

    /// <summary>
    /// 出了某一手牌，该做什么动作。**这就是"我们只需要设置时机"里那个映射表。**
    /// 越大的牌型动作越夸张 —— 炸弹直接上大招。
    /// </summary>
    public static string AnimForCombo(ComboType type)
    {
        return type switch
        {
            ComboType.Single => AttackSingle,
            ComboType.Pair => AttackDouble,
            ComboType.Triple => AttackTriple,

            ComboType.TripleWithSingle or ComboType.TripleWithPair
                or ComboType.FourWithTwoSingles or ComboType.FourWithTwoPairs
                => AttackSmall,

            ComboType.Straight or ComboType.ConsecutivePairs
                or ComboType.Plane or ComboType.PlaneWithSingles or ComboType.PlaneWithPairs
                => AttackMulti,

            ComboType.Bomb => AttackBig1,
            ComboType.Rocket => AttackBig2,

            _ => AttackSmall,
        };
    }

    /// <summary>定地主：摆个"蓄力"的架势（比攻击更"称王"一点）。</summary>
    public void PlayLandlord() { Play(PowerUp, false); SfxCast(); }

    /// <summary>出牌：动作 + 根据牌型选择音效（大牌更有气势）。</summary>
    public void PlayCombo(ComboType type)
    {
        Play(AnimForCombo(type), false);
        SfxForCombo(type);
    }

    /// <summary>要不起 / 被压了一头：挨一下 + 减益音效。</summary>
    public void PlayHit()

    {
        Play(Hit, false);
        DouSpireSfx.Play("event:/sfx/debuff", 0.6f);
    }

    /// <summary>普通出招（猜拳亮拳这类小亮相），不带音效 —— 音效由场景自己管。</summary>
    public void PlayAttack() => Play(AttackSmall, false);

    /// <summary>赢了：起个势 + 增益音（胜利感）。</summary>
    public void PlayWin() 
    { 
        Play(Revive, false); 
        SfxCast();
        DouSpireSfx.Play("event:/sfx/buff", 0.7f);
    }

    /// <summary>输了：倒下。</summary>
    public void PlayLose() { Play(Dead, false); SfxDeath(); }

    // ══════════════ 角色自己的音效（游戏自带，不用我们找素材）══════════════

    /// <summary>这个角色的攻击音效（<c>event:/sfx/characters/&lt;entry&gt;/&lt;entry&gt;_attack</c>）。</summary>
    public void SfxAttack() => DouSpireSfx.ActorVoice(Safe(_model.AttackSfx), 0.8f);

    /// <summary>施法/蓄力音效。</summary>
    public void SfxCast() => DouSpireSfx.ActorVoice(Safe(_model.CastSfx), 0.7f);

    /// <summary>倒地音效。</summary>
    public void SfxDeath() => DouSpireSfx.ActorVoice(Safe(_model.DeathSfx), 0.8f);

    /// <summary>
    /// 根据牌型选择合适的音效。越大的牌型音效越有冲击力。
    /// <list type="bullet">
    ///   <item>小牌（单对三）→ 攻击音（轻快）</item>
    ///   <item>顺子/连对/飞机 → 施法音（有气势）</item>
    ///   <item>炸弹 → 施法音 + 攻击音（双重冲击）</item>
    ///   <item>王炸 → 施法音 + 攻击音（最强）</item>
    /// </list>
    /// </summary>
    private void SfxForCombo(ComboType type)
    {
        switch (type)
        {
            // 顺子、连对、飞机 → 施法音（更有气势）
            case ComboType.Straight:
            case ComboType.ConsecutivePairs:
            case ComboType.Plane:
            case ComboType.PlaneWithSingles:
            case ComboType.PlaneWithPairs:
                SfxCast();
                break;

            // 炸弹 → 施法 + 攻击 + 破甲（三重冲击）
            case ComboType.Bomb:
                SfxCast();
                SfxAttack();
                DouSpireSfx.Play("event:/sfx/block_break", 0.85f);
                break;

            // 王炸 → 施法 + 攻击 + 破甲（最强组合，声音更响）
            case ComboType.Rocket:
                SfxCast();
                SfxAttack();
                DouSpireSfx.Play("event:/sfx/block_break", 1.0f);
                break;

            // 其余小牌（单/对/三/带牌）→ 攻击音
            default:
                SfxAttack();
                break;
        }
    }

    private static string Safe(string? s) => string.IsNullOrEmpty(s) ? "" : s!;

    private static string SafeEntry(CharacterModel m)
    {
        try { return m.Id.Entry; } catch { return "?"; }
    }

    // ══════════════ 收摊 ══════════════

    /// <summary>把这个角色从树上摘掉。关桌子、换人时都要调。</summary>
    public void Despawn()
    {
        if (_node is null) return;
        try
        {
            if (GodotObject.IsInstanceValid(_node)) _node.QueueFree();
        }
        catch (System.Exception e)
        {
            DouSpire.Entry.Logger.Warn($"斗地主：角色 {Entry} 没能干净退场: {e.Message}");
        }
        _node = null;
        _sprite = null;
        _state = null;
    }
}
