using Godot;
using MegaCrit.Sts2.Core.Nodes.Audio;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace DouSpire.Table;

/// <summary>
/// 牌桌上的音乐"场景槽位"。
///
/// 为什么是"槽位"而不是"一首歌"：真斗地主的音乐是**跟着局势换的** ——
/// 开局一首舒缓的，有人快走完了换紧张的，炸弹一响切高潮，赢了有胜利曲。
/// 所以这里按**时机**分槽，每个槽位对应一个文件夹，往里丢几首就轮着放。
/// </summary>
public enum DouSpireMusicCue
{
    /// <summary>平时（打牌中）。</summary>
    Table = 0,

    /// <summary>局势紧了（有人只剩 ≤2 张）。</summary>
    Tense = 1,

    /// <summary>高潮（炸弹 / 王炸 / 加倍 / 抢地主）—— 放几秒自己退回局势曲。</summary>
    Climax = 2,

    /// <summary>这局我赢了。</summary>
    Win = 3,

    /// <summary>这局我输了。</summary>
    Lose = 4,
}

/// <summary>
/// 斗地主的**背景音乐**。
///
/// 和 <see cref="DouSpireSfx"/> 的分工：音效走游戏的 FMOD（事件名，塞不进自己的音频），
/// **音乐走 Godot 自己的 <see cref="AudioStreamPlayer"/>，直接从磁盘读文件** ——
/// 这样才能放玩家/作者自己准备的歌（和 <see cref="DouSpireVoice"/> 一个路子）。
///
/// ── 怎么加歌（不用改代码、不用重编 dll）──────────────────────────────
///   把音频丢进 <c>mods\DouSpire\assets\music\&lt;槽位&gt;\</c>，收桌再开桌就生效：
///     music\table\   平时        music\tense\   紧张
///     music\climax\  高潮        music\win\     胜利      music\lose\  失败
///   支持 <c>.ogg</c> / <c>.wav</c> / <c>.mp3</c>；一个槽位放多首会**随机轮播且不连着重复**。
///   也支持只放一首：<c>music\table.ogg</c>（直接一个文件，不建文件夹）。
///
/// ── 槽位空着怎么办 ──────────────────────────────────────────────
///   平时/紧张 会自动**回退到游戏本体的曲子**（见 <see cref="VanillaEvent"/>）。
///   **高潮 / 胜利 / 失败没有回退曲**：
///     · 高潮空着 = 保持当前曲子（boss 曲在主菜单场景的音色库没加载，放不出来，见下）
///     · 胜负空着 = 静音（让胜利音效自己响，反而更干净）
///   所以：想听高潮音乐，就得往 <c>music\climax\</c> 里放歌。
///
/// ── 两条铁律（跟音效那套一样）────────────────────────────────────
///   <item>**任何一步出错都吞掉** —— 没音乐只是安静，绝不让音乐把牌桌搞崩。</item>
///   <item>连续出错就**永久闭嘴**，不然每帧刷一条日志更烦。</item>
/// </summary>
public static class DouSpireMusic
{
    // ══════════════ 槽位 → 文件夹名 ══════════════
    //  故意用纯英文小写：玩家要照着这个建文件夹，中文名在有些解压工具下会乱码。

    private static string FolderOf(DouSpireMusicCue cue) => cue switch
    {
        DouSpireMusicCue.Tense => "tense",
        DouSpireMusicCue.Climax => "climax",
        DouSpireMusicCue.Win => "win",
        DouSpireMusicCue.Lose => "lose",
        _ => "table",
    };

    /// <summary>槽位空着时用游戏本体的哪首（null = 不放，交给音效）。</summary>
    private static string? VanillaEvent(DouSpireMusicCue cue) => cue switch
    {
        DouSpireMusicCue.Table => "event:/music/act1_a1_v1",        // 第一层：安稳
        DouSpireMusicCue.Tense => "event:/music/act3_a2_v1",        // 第三层：紧张

        // ⚠️ 高潮**故意不给回退曲**（2026-10-08 实测踩的坑）：
        //   boss 曲 `event:/music/act1_boss_the_kin` 在本体里确实存在，但它所在的
        //   **音色库在"主菜单"场景没有加载**（日志：loadedBankCount=7 / banksStillLoading=False），
        //   而牌桌多半是从主菜单开的 → 放它只会刷一条
        //   `FMOD event was not found ... cannot find music path`，一点声音都没有。
        //   所以：**要高潮音乐就自己放歌**（music\climax\）；没放就保持原曲，最稳。
        //
        // 胜负：也没有回退曲 —— 静音，让"天降金币"那几声音效自己响，反而更干净。
        _ => null,
    };

    /// <summary>高潮曲放多久后自动退回局势曲（秒）。</summary>
    private const double ClimaxSeconds = 11.0;

    private const double FadeIn = 0.9;
    private const double FadeOut = 1.2;

    // ══════════════ 状态 ══════════════

    /// <summary>两个播放器轮着用 —— 换曲时一个淡出一个淡入，不会"啪"地断掉。</summary>
    private static readonly AudioStreamPlayer?[] Players = new AudioStreamPlayer?[2];
    private static readonly Tween?[] Tweens = new Tween?[2];
    private static int _active = -1;

    private static AudioStream? _nowStream;          // 当前在放的自定义曲（同一首不重播）
    private static string _nowVanilla = "";          // 当前在放的本体曲
    private static DouSpireMusicCue _ambient = DouSpireMusicCue.Table;  // 高潮退场后回到哪个槽
    private static ulong _climaxUntil;               // 高潮曲的截止时刻（ms），0 = 不在高潮

    /// <summary>
    /// **当前正在服务的槽位**（null = 什么都没放）。
    ///
    /// ⚠️ 这个字段是 2026-10-08 补的，作用是**同一槽位重复 <see cref="Play"/> 时立刻返回**。
    /// 起因：<c>UpdateMusic()</c> 挂在每次界面刷新上（很频繁），而 <see cref="Pick"/> 是**随机挑**的
    /// —— 只要槽位里放了 **≥2 首**，每个刷新周期都会挑到"另一首"（`Pick` 刻意避开上一首，
    /// **两首时就是必然交替**）⇒ 两个播放器交叉淡入淡出互相打断，**音乐一直在抖**。
    /// 以前所有槽位基本都是空的，所以这个坑一直没露出来；一放歌就会现形。
    /// </summary>
    private static DouSpireMusicCue? _nowCue;

    private static readonly Dictionary<DouSpireMusicCue, string[]> FileCache = new();
    private static readonly Dictionary<DouSpireMusicCue, int> LastPick = new();
    private static readonly Dictionary<string, AudioStream?> StreamCache = new();

    private static int _fails;
    private static bool _dead;

    // ══════════════ 对外接口 ══════════════

    /// <summary>高潮曲还在放吗？（放的时候别再切局势曲，免得两首打架）</summary>
    public static bool ClimaxActive => _climaxUntil != 0;

    /// <summary>换到某个槽位。**同一槽位重复调用什么都不做**（不重播、也不重新随机挑歌）。</summary>
    public static void Play(DouSpireMusicCue cue)
    {
        if (_dead) return;
        if (!DouSpireConfig.MusicEnabled) { _climaxUntil = 0; Stop(); return; }

        try
        {
            // ── 高潮：临时插播。**只有真的放了高潮曲才插** ──
            //   槽位空着就什么都不做：保持当前局势曲，而且**不占"高潮期"** ——
            //   否则为了一个空槽位会把局势曲的切换白白冻住 11 秒。
            if (cue == DouSpireMusicCue.Climax)
            {
                if (!DouSpireConfig.MusicClimaxEnabled) return;

                // 已经在放高潮了（同一手炸弹被两处各叫了一次）—— 只续时长，别重新挑歌
                if (ClimaxActive)
                {
                    _climaxUntil = Time.GetTicksMsec() + (ulong)(ClimaxSeconds * 1000.0);
                    return;
                }

                var boom = Pick(cue);
                if (boom is null) return;      // 空槽位：保持原曲，而且不占高潮期

                _climaxUntil = Time.GetTicksMsec() + (ulong)(ClimaxSeconds * 1000.0);
                _nowCue = cue;
                if (ReferenceEquals(boom, _nowStream)) return;   // 正好是同一首，别重播
                Crossfade(boom);
                return;
            }

            _climaxUntil = 0;
            _ambient = cue;

            // ★★ 同一槽位重复调用**必须立刻返回**（见 _nowCue 的注释）：
            //    否则槽位里放了 ≥2 首时，每次刷新都会随机挑到另一首 → 音乐一直抖。
            if (_nowCue == cue) return;

            var stream = Pick(cue);

            if (stream is not null)
            {
                _nowCue = cue;
                if (ReferenceEquals(stream, _nowStream)) return;   // 已经在放这首了
                Crossfade(stream);
                return;
            }

            // 槽位空着 → 回退本体曲（胜负槽没有回退曲，会静音）
            Vanilla(cue);
            // ⚠️ 必须放在最后：Vanilla 里可能调 Stop()，那会把 _nowCue 清掉
            _nowCue = cue;
        }
        catch (System.Exception e)
        {
            if (_fails++ < 3) Entry.Logger.Warn($"斗地主：换音乐出错（不影响玩）: {e.Message}");
            if (_fails >= 3) _dead = true;
        }
    }

    /// <summary>
    /// 每帧叫一下（牌桌开着时挂在牌桌的 `_Process` 上）：高潮曲到点自己退场。
    ///
    /// ⚠️ **收桌之后的音乐压制不在这里做** —— 这个 `Tick` 跟着牌桌一起消失，
    ///   收桌后它根本不会被调用。压制交给常驻的 <see cref="DouSpireHotkey"/>（见 <see cref="SuppressTick"/>）。
    /// </summary>
    public static void Tick()
    {
        if (_climaxUntil == 0) return;
        if (Time.GetTicksMsec() < _climaxUntil) return;
        _climaxUntil = 0;
        Play(_ambient);
    }

    /// <summary>停掉全部音乐（自定义 + 本体）。</summary>
    public static void Stop()
    {
        _climaxUntil = 0;
        _nowStream = null;
        _nowVanilla = "";
        _nowCue = null;      // ⚠️ 必须清：留着它下次 Play(同一个槽位) 会直接返回，音乐就再也起不来了

        for (var i = 0; i < Players.Length; i++) KillTween(i);

        foreach (var p in Players)
        {
            if (p is null || !GodotObject.IsInstanceValid(p)) continue;
            try { p.Stop(); } catch (System.Exception) { }
        }
        _active = -1;

        try { NAudioManager.Instance?.StopMusic(); }
        catch (System.Exception e) { Entry.Logger.Warn($"斗地主：本体音乐停不下来: {e.Message}"); }

        // ★★ 2026-10-13：**必须连 NRunMusicController 一起压住**，否则收桌之后音乐自己会回来。
        //   真因（用户：「关闭后音乐一直放」）：
        //     · 游戏里跑局的背景音乐由 `NRunMusicController` 管（不是 NAudioManager）——
        //       它每帧 `_Process` 跑 `UpdateMusic()`，手里攥着 `_currentTrack`，
        //       只要它认为"该有音乐"，就会重新 `PlayMusic` 把曲子放回来。
        //     · 我们原来只调了 `NAudioManager.StopMusic()`，等于**只关了喇叭、没关指挥**，
        //       下一帧它就把音乐重新推上来了 —— 听感就是"关了还在放、停不掉"。
        //   所以这里把它也停一道；再登记一个**压制期**，让它在收桌后的这一小段时间里
        //   即使想放也被我们按回去（见 SuppressTick）。
        try { _runMusic?.Invoke(); }
        catch (System.Exception e) { Entry.Logger.Warn($"斗地主：跑局音乐停不下来: {e.Message}"); }

        _suppressUntil = Time.GetTicksMsec() + (ulong)(SuppressSeconds * 1000.0);
    }

    /// <summary>
    /// 收桌后再把音乐按回去多久（秒）。
    ///
    /// 为什么要"按一会儿"而不是停一次就完：`NRunMusicController` 是在**场景/房间变化**时
    /// 重新推导该放哪首的，收桌那一瞬间往往正赶上它重建。停一次可能被它的下一帧覆盖掉，
    /// 所以留一个短暂的压制窗口，把它的自动回放挡在门外。
    /// </summary>
    private const double SuppressSeconds = 2.5;

    private static ulong _suppressUntil;

    /// <summary>
    /// 压制窗口内，把游戏本体的跑局音乐再按回去一次。
    ///
    /// ⚠️ **调用方是常驻的 <see cref="DouSpireHotkey"/>，不是牌桌** —— 收桌之后牌桌就销毁了，
    ///   只有那个常驻节点还在跑。所以这里改成 public 由它每帧叫
    ///   （见 `DouSpireHotkey._Process` 里那段注释：这是"音乐停了又自己回来"的病根之一）。
    /// </summary>
    public static void SuppressTick()
    {
        if (_suppressUntil == 0) return;
        if (Time.GetTicksMsec() >= _suppressUntil) { _suppressUntil = 0; return; }

        // 牌桌开着的时候不该压（那是我们自己在放曲），只有收桌后才压。
        if (DouSpireTable.IsOpen || DouSpirePokerTable.IsOpen || DouSpireBlackjackTable.IsOpen) return;

        try { NAudioManager.Instance?.StopMusic(); } catch (System.Exception) { }
        try { _runMusic?.Invoke(); } catch (System.Exception) { }
    }

    /// <summary>
    /// 游戏本体的**跑局音乐控制器** `NRunMusicController.StopMusic()`（反射拿到，只找一次）。
    ///
    /// 为什么不直接引用类型：这个类在 `sts2.dll` 里，直接写类型名当然也能编过，
    /// 但**万一某个游戏版本把它改掉 / 去掉，整个 mod 会在加载时炸**。
    /// 反射找不到就退化成"只停 NAudioManager"，最多就是音乐没压住，**绝不会连累开桌**。
    /// </summary>
    private static System.Action? _runMusic;
    private static bool _runMusicProbed;

    private static void ProbeRunMusic()
    {
        if (_runMusicProbed) return;
        _runMusicProbed = true;
        try
        {
            var t = typeof(NAudioManager).Assembly
                .GetType("MegaCrit.Sts2.Core.Nodes.Audio.NRunMusicController");
            if (t is null) return;

            var m = t.GetMethod("StopMusic",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (m is null) return;

            // 它是个 Node，得拿到**活着的那个实例**（挂在场景树上，不是单例字段）。
            _runMusic = () =>
            {
                var node = FindInTree(DouSpireUi.Root, t);
                if (node is null) return;
                m.Invoke(node, null);
            };
            Entry.Logger.Info("斗地主：已接上跑局音乐控制器（收桌时能一并停住）。");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：找跑局音乐控制器失败（收桌只停 NAudioManager）: {e.Message}");
        }
    }

    /// <summary>在场景树里按类型找一个活着的节点（最多翻几层，找不到就算了）。</summary>
    private static Node? FindInTree(Node? root, System.Type type)
    {
        if (root is null || !GodotObject.IsInstanceValid(root)) return null;
        try
        {
            if (type.IsInstanceOfType(root)) return root;
            foreach (var child in root.GetChildren())
            {
                var hit = FindInTree(child, type);
                if (hit is not null) return hit;
            }
        }
        catch (System.Exception) { /* 场景树正在变的时候翻不动，下一轮再来 */ }
        return null;
    }

    /// <summary>收桌时清缓存 —— 新丢进来的歌，重新开桌就生效（不用重启游戏）。</summary>
    public static void ClearCache()
    {
        FileCache.Clear();
        LastPick.Clear();
        StreamCache.Clear();
        _nowCue = null;      // ⚠️ 不清的话，重开桌时 Play(同一槽位) 会直接返回 → 开桌没有音乐
        _fails = 0;
        _dead = false;
        _ambient = DouSpireMusicCue.Table;
        _suppressUntil = 0;  // ★ 重开桌要立刻解除压制，不然开桌头 2 秒没音乐
        ProbeRunMusic();
    }

    // ══════════════ 挑歌 ══════════════

    /// <summary>挑一首：槽位里的歌随机选，尽量不跟上一首重复。</summary>
    private static AudioStream? Pick(DouSpireMusicCue cue)
    {
        var files = Files(cue);
        if (files.Length == 0) return null;

        var idx = 0;
        if (files.Length > 1)
        {
            var last = LastPick.TryGetValue(cue, out var l) ? l : -1;
            // 最多试 6 次，别在"只有两首但随机老撞"上死循环
            for (var t = 0; t < 6; t++)
            {
                idx = System.Random.Shared.Next(files.Length);
                if (idx != last) break;
            }
        }
        LastPick[cue] = idx;

        _lastPickPath = files[idx];
        return LoadStream(files[idx]);
    }

    /// <summary>最近一次挑中的文件（只给日志用 —— <c>LoadFromFile</c> 的流没有 ResourcePath，打出来是 <c>?</c>）。</summary>
    private static string _lastPickPath = "";

    /// <summary>某个槽位下的全部音频文件（绝对路径，缓存）。</summary>
    private static string[] Files(DouSpireMusicCue cue)
    {
        if (FileCache.TryGetValue(cue, out var hit)) return hit;

        var list = new List<string>();
        try
        {
            var root = DouSpireAssets.RootDir;
            if (!string.IsNullOrEmpty(root))
            {
                var name = FolderOf(cue);
                var dir = root + "/music/" + name;

                // ① 文件夹形式：music/table/01.ogg、music/table/02.ogg …
                if (Directory.Exists(dir))
                {
                    foreach (var f in Directory.GetFiles(dir))
                        if (IsAudio(f)) list.Add(f);
                    list.Sort(System.StringComparer.OrdinalIgnoreCase);
                }

                // ② 单文件形式：music/table.ogg
                foreach (var ext in new[] { ".ogg", ".mp3", ".wav" })
                {
                    var one = root + "/music/" + name + ext;
                    if (File.Exists(one)) { list.Insert(0, one); break; }
                }
            }
        }
        catch (System.Exception e)
        {
            if (_fails++ < 3) Entry.Logger.Warn($"斗地主：列音乐目录出错: {e.Message}");
        }

        var arr = list.ToArray();
        FileCache[cue] = arr;
        return arr;
    }

    private static bool IsAudio(string path)
    {
        var e = Path.GetExtension(path).ToLowerInvariant();
        return e is ".ogg" or ".wav" or ".mp3";
    }

    /// <summary>读一个音频文件（缓存）。**设成循环** —— 背景音乐不能放完就没了。</summary>
    private static AudioStream? LoadStream(string full)
    {
        if (StreamCache.TryGetValue(full, out var hit)) return hit;

        AudioStream? s = null;
        try
        {
            switch (Path.GetExtension(full).ToLowerInvariant())
            {
                case ".ogg":
                {
                    var o = AudioStreamOggVorbis.LoadFromFile(full);
                    if (o != null) { o.Loop = true; s = o; }
                    break;
                }
                case ".mp3":
                {
                    var m = AudioStreamMP3.LoadFromFile(full);
                    if (m != null) { m.Loop = true; s = m; }
                    break;
                }
                case ".wav":
                {
                    var w = AudioStreamWav.LoadFromFile(full);
                    if (w != null) { w.LoopMode = AudioStreamWav.LoopModeEnum.Forward; s = w; }
                    break;
                }
            }
        }
        catch (System.Exception e)
        {
            if (_fails++ < 3) Entry.Logger.Warn($"斗地主：音乐读不了 {Path.GetFileName(full)}: {e.Message}");
        }

        StreamCache[full] = s;
        return s;
    }

    // ══════════════ 播放 / 淡入淡出 ══════════════

    private static void Crossfade(AudioStream track)
    {
        var next = _active == 0 ? 1 : 0;
        var p = PlayerAt(next);
        if (p is null) return;

        p.Stream = track;
        p.VolumeDb = -60f;                       // 从听不见开始
        p.Play();
        FadeTo(next, TargetDb(), FadeIn, null);

        // 把上一首淡出，再停掉
        if (_active >= 0 && _active != next)
        {
            var old = _active;
            FadeTo(old, -60f, FadeOut, () =>
            {
                var q = Players[old];
                if (q is not null && GodotObject.IsInstanceValid(q)) { try { q.Stop(); } catch (System.Exception) { } }
            });
        }

        _active = next;
        _nowStream = track;
        _nowVanilla = "";

        // 切回自定义曲时，把本体曲停掉（两边同时响会很难听）
        try { NAudioManager.Instance?.StopMusic(); } catch (System.Exception) { }

        Log($"换曲 {(_lastPickPath.Length > 0 ? Path.GetFileName(_lastPickPath) : (track.ResourcePath ?? "?"))}");
    }

    private static void Vanilla(DouSpireMusicCue cue)
    {
        var ev = VanillaEvent(cue);
        if (ev is null) { Stop(); return; }
        if (_nowVanilla == ev) return;

        // 自定义那两个播放器先静下来
        for (var i = 0; i < Players.Length; i++)
        {
            KillTween(i);
            var p = Players[i];
            if (p is null || !GodotObject.IsInstanceValid(p)) continue;
            try { p.Stop(); } catch (System.Exception) { }
        }
        _active = -1;
        _nowStream = null;

        try
        {
            NAudioManager.Instance?.PlayMusic(ev);
            _nowVanilla = ev;
            Log($"本体曲 {ev}");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：本体音乐放不出来: {e.Message}");
        }
    }

    private static AudioStreamPlayer? PlayerAt(int i)
    {
        var p = Players[i];
        if (p is not null && GodotObject.IsInstanceValid(p)) return p;

        var root = DouSpireUi.Root;
        if (root is null) return null;

        p = new AudioStreamPlayer { Bus = "Master" };
        root.AddChild(p);
        Players[i] = p;
        return p;
    }

    private static float TargetDb()
        => Mathf.LinearToDb(Mathf.Clamp(DouSpireConfig.MusicVolume, 0.001f, 1f));

    private static void FadeTo(int i, float db, double dur, System.Action? then)
    {
        KillTween(i);
        var p = Players[i];
        if (p is null || !GodotObject.IsInstanceValid(p)) return;

        try
        {
            var t = p.CreateTween();
            t.TweenProperty(p, "volume_db", db, dur);
            if (then is not null) t.TweenCallback(Callable.From(then));
            Tweens[i] = t;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：音乐淡入淡出做不了（直接切）: {e.Message}");
            try { p.VolumeDb = db; } catch (System.Exception) { }
        }
    }

    private static void KillTween(int i)
    {
        var t = Tweens[i];
        if (t is null) return;
        try { if (t.IsValid()) t.Kill(); } catch (System.Exception) { }
        Tweens[i] = null;
    }

    // ══════════════ 诊断 ══════════════

    private static void Log(string what)
    {
        Entry.Logger.Info($"斗地主：{what}");
        DouSpireAssets.Diag($"[音乐] {what}");
    }

    private static bool _reported;

    /// <summary>
    /// 开桌时清点一遍：每个槽位找到了几首歌。
    /// 想知道"我丢的歌有没有被认出来"，看这条日志就够了。
    /// </summary>
    public static void ReportOnce()
    {
        if (_reported) return;
        _reported = true;
        try
        {
            var parts = new List<string>();
            var empty = 0;
            foreach (var cue in new[]
                     {
                         DouSpireMusicCue.Table, DouSpireMusicCue.Tense,
                         DouSpireMusicCue.Climax, DouSpireMusicCue.Win, DouSpireMusicCue.Lose,
                     })
            {
                var n = Files(cue).Length;
                if (n == 0) empty++;
                parts.Add($"{FolderOf(cue)}={n} 首");
            }
            var line = $"斗地主音乐：{string.Join("，", parts)}"
                       + (empty == 5 ? "（一个都没放 → 全部走游戏本体曲子）" : "");
            Entry.Logger.Info(line);
            DouSpireAssets.Diag($"[音乐] {line}");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：音乐清点失败: {e.Message}");
        }
    }
}
