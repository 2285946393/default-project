using Godot;

namespace DouSpire.Table;

/// <summary>
/// 编外角色的"说话声"。
///
/// 为什么另起一套：牌桌上的**游戏音效**走 FMOD（<see cref="DouSpireSfx"/>），
/// 音频文件在**加密的 pck 里**，只能按事件名播、塞不进自己的音频。
/// 而用户给的这批 wav（茑町千岁的配音）是**外部文件**，只能我们自己用
/// Godot 的 <see cref="AudioStreamPlayer"/> 放。
///
/// 两条铁律：
/// <list type="bullet">
///   <item>**任何一步出错都吞掉**，没声音只是安静，绝不能让语音把牌桌搞崩。</item>
///   <item>**每一席一个播放器** —— 三家同时开口也不会互相掐掉（早先共用一个，
///         新的一句会把上一句打断）。自己那席满音量，对手按设置压低。</item>
/// </list>
/// </summary>
/// <summary>
/// 一句话有多"要紧" —— 决定它**被概率闸门放行的机会有多大**。
/// 见 <see cref="DouSpireVoice.RollAdmit"/>（碎嘴 35% / 普通 60% / 关键 100%，都是设置页上的滑条）。
/// </summary>
public enum VoicePriority
{
    /// <summary>碎嘴：出一张单牌/对子、喊「要不起」、压别人一手。牌桌上最常发生，默认只 35% 说出来。</summary>
    Chatter = 0,

    /// <summary>有信息量的场面话：三带、顺子、连对、飞机、轮到我、点提示。默认 60%。</summary>
    Normal = 1,

    /// <summary>一定要说：炸弹/王炸、报单、胜负、抢地主/不抢、当地主、发牌。默认 100%（照旧必说）。</summary>
    Important = 2,
}

public static class DouSpireVoice
{
    private static readonly System.Collections.Generic.Dictionary<string, AudioStream?> Cache = new();

    /// <summary>
    /// 每一席一个播放器。数组下标就是座位号：0 = 我，1 = 下家，2 = 上家，3 = 第四家
    /// （斗地主最多四人，留足位置）。
    /// </summary>
    private static readonly AudioStreamPlayer?[] Players = new AudioStreamPlayer?[4];

    private static int _fails;
    private static bool _dead;

    // ── 播放状态（每席一份，下标 = 座位号）──────────────────────────
    /// <summary>每一席上一次**成功开口**的时刻（毫秒）。频率阀门已废弃，只作记录。</summary>
    private static readonly ulong[] LastSpokeAt = new ulong[4];

    /// <summary>每一席**上一次说的是哪一句**（已经解析过的实际路径）。轮播选文件时用来避免连着说同一句。</summary>
    private static readonly string[] LastResolved = { "", "", "", "" };

    /// <summary>每一席**正在说的那句有多要紧**。（频率阀门已废弃，暂无消费方。）</summary>
    private static readonly VoicePriority[] NowTalking = new VoicePriority[4];

    /// <summary>扫出来的"同一句的不同版本"缓存：相对路径 → 磁盘上所有候选（绝对路径）。</summary>
    private static readonly System.Collections.Generic.Dictionary<string, string[]> VariantCache = new();

    /// <summary>已经警告过"文件不存在"的路径（每个只警告一次，免得刷屏）。</summary>
    private static readonly System.Collections.Generic.HashSet<string> _warnedMissing = new();

    /// <summary>
    /// 播一句。名字对应 <c>assets/voice/&lt;名字&gt;</c>（也可以是**绝对路径**，见 <see cref="SayAbs"/>）。
    /// <paramref name="seat"/> = 哪一席说的 —— **自己那席满音量，对手按设置压低**
    /// （但都能正常放，不互相打断）。用户 2026-10-07："放大你选的，你的对手的声音低一些，但是都能正常放"。
    ///
    /// <paramref name="priority"/> 决定这句话**有多容易被概率闸门放行** ——
    /// 见 <see cref="RollAdmit"/>（2026-10-11 起：碎嘴档默认只 35% 说出来）。
    /// </summary>
    public static void Say(string file, int seat = 0, float volume = 1f,
        VoicePriority priority = VoicePriority.Normal)
    {
        if (_dead || string.IsNullOrEmpty(file)) return;
        if (!DouSpireConfig.VoiceEnabled) return;

        try
        {
            // ① 概率闸门：按这句话的"要紧程度"掷一次骰子（2026-10-11 加，见 RollAdmit）
            if (!RollAdmit(priority)) return;

            // ② 先把"这一句"解析成磁盘上真实存在的某一个文件
            //    （同前缀多个文件时随机挑；只有一个时原样返回）
            var target = Pick(file, seat);

            // ③ 该用哪个播放器（每席一个）。**本席正在说话时，只有更要紧的那句才许打断它** ——
            //    否则同一秒里连着触发的两句会互相掐掉，听起来就是"没声音"（见 Admit 的说明）。
            var p = Admit(seat, priority);
            if (p is null) return;

            // ④ 真正放
            var stream = LoadAny(target);
            if (stream is null)
            {
                // ★ 2026-10-08 加：文件不存在以前是**纯静默**返回的（这次 13_win 哑掉就是它，
                //   日志里连一行痕迹都没有，查了半天）。改成每个文件只警告一次，不刷屏。
                if (_warnedMissing.Add(target))
                    Entry.Logger.Warn($"斗地主：语音文件不存在，这句没播 {target}");
                return;
            }

            var scale = seat == 0 ? 1f : DouSpireConfig.OpponentVoiceScale;
            var final = Mathf.Clamp(volume * scale * DouSpireConfig.SfxVolume, 0.001f, 1f);
            p.VolumeDb = Mathf.LinearToDb(final);
            p.Stream = stream;
            p.Play();

            LastResolved[seat] = target;
            NowTalking[seat] = priority;
            LastSpokeAt[seat] = Time.GetTicksMsec();

            // 诊断：谁说了哪句、多要紧、音量多少。
            // 同时写进**我们自己的日志文件**（游戏自带的日志不知道被写到哪去了，找不着）。
            Entry.Logger.Info($"斗地主：语音 座位{seat} [{priority}] 音量{final:0.00} 文件={target}");
            DouSpireAssets.Diag($"语音 座位{seat} [{priority}] 音量{final:0.00} 文件={target}");
        }
        catch (System.Exception e)
        {
            if (_fails++ < 3) Entry.Logger.Warn($"斗地主：语音播不出来（不影响玩）: {e.Message}");
            if (_fails >= 3) _dead = true;
        }
    }

    /// <summary>
    /// 播一句**绝对路径**的音频（用于借用别的 mod 的文件，那些不在我们 assets/ 里）。
    /// 现在 <see cref="Say"/> 自己就认绝对路径了，所以这里只是留个**叫得明白的名字**。
    /// </summary>
    public static void SayAbs(string absPath, int seat = 0, float volume = 1f,
        VoicePriority priority = VoicePriority.Normal)
    {
        Say(absPath, seat, volume, priority);
    }

    // ══════════════ 闸门一：这句话这次要不要说（概率触发）══════════════

    /// <summary>
    /// **概率闸门**：这句话这一次要不要说出口。
    ///
    /// ── 为什么加（2026-10-11）─────────────────────────────────────
    /// 2026-10-08 晚按用户要求把"按秒节流"那套频率阀门**整个撤掉**了
    /// （用户原话："我没让你们加频率阀门啊"），之后是"每次事件都开口"、句句都响。
    /// 响了一个多月，用户 2026-10-11 的反馈是：
    /// > 「**说句心里话，中文语音太尬了。所有语音改成概率触发。**」
    ///
    /// ⇒ 所以**不是**回到当年那套"按秒数节流"（那个玩家体感是"出牌没声音"、被骂过），
    ///   而是**按台词要紧程度掷骰子**：
    /// <list type="bullet">
    ///   <item><see cref="VoicePriority.Chatter"/>（出小牌 / 要不起 / 压一手）—— 默认只 35%；</item>
    ///   <item><see cref="VoicePriority.Normal"/>（三带 / 顺子 / 轮到我）—— 默认 60%；</item>
    ///   <item><see cref="VoicePriority.Important"/>（炸弹 / 报单 / 胜负 / 抢地主）—— 默认 100%，照旧必说。</item>
    /// </list>
    /// 三条概率都是设置页上的滑条（<c>DOUSPIRE-VOICE_CHANCE_*</c>），想全开都拉 100%，
    /// 想彻底清净都拉 0%。
    ///
    /// ⚠️ **掷骰子只影响"说不说"，不影响"说哪句"** —— 放行之后的选文件（多版本轮播）
    ///   还是走 <see cref="Pick"/>，跟以前一模一样。
    /// </summary>
    private static bool RollAdmit(VoicePriority prio)
    {
        var chance = prio switch
        {
            VoicePriority.Chatter => DouSpireConfig.VoiceChanceChatter,
            VoicePriority.Important => DouSpireConfig.VoiceChanceImportant,
            _ => DouSpireConfig.VoiceChanceNormal,
        };

        if (chance >= 100f) return true;      // 满概率：不掷骰子，省一次随机
        if (chance <= 0f) return false;       // 关掉这一档：一次都不说
        return System.Random.Shared.NextDouble() * 100.0 < chance;
    }

    // ══════════════ 闸门二：这一句用哪个播放器 / 要不要让路 ══════════════

    /// <summary>
    /// 这一句用哪个播放器放。**每席一个**，三家同时开口不会互相掐掉。
    ///
    /// ── 规则（2026-10-11 定稿）──────────────────────────────────────
    /// <list type="number">
    ///   <item>本席**没在说话** → 直接放。</item>
    ///   <item>本席**正在说话**，新的一句**比它更要紧** → 打断，放新的（炸弹 / 胜负照样能插进来）。</item>
    ///   <item>本席**正在说话**，新的一句**同级或更次要** → **这次就不说了**，让位给正在说的那句。</item>
    /// </list>
    ///
    /// ── 为什么要有第 3 条（用户反馈 + 诊断日志实锤）──────────────────
    /// 每席只有一个播放器，`p.Stream = x; p.Play();` 会**从 0 重新开始** ——
    /// 于是同一秒里连着触发的两句，前一句刚开口就被后一句掐掉，
    /// 听起来就是"这一席没声音"。诊断日志里的原样：
    /// <code>
    /// 19:21:48  座位0 [Normal]  4_hint_1.ogg
    /// 19:21:48  座位0 [Chatter] 10_pass_1.ogg      ← 同一秒，4_hint 被吃掉
    /// </code>
    /// 现在「点提示」正在说的时候，紧接着的「要不起」不会把它顶掉；反过来
    /// 「炸弹」永远能打断正在说的碎嘴。**不是**按秒数节流（那套 2026-10-08 已撤），
    /// 只在"真的正在说话"这一瞬间让路 —— 一局里丢掉的是零星几句，不会变成"出牌没声音"。
    /// </summary>
    private static AudioStreamPlayer? Admit(int seat, VoicePriority prio)
    {
        var p = PlayerFor(seat);
        if (p is null) return null;

        if (p.Playing && prio <= NowTalking[seat]) return null;   // 让位，不打断

        return p;
    }

    /// <summary>
    /// 把 <c>"yuki/5_small.ogg"</c> 展开成"这个目录里所有同前缀的音频"，随机挑一个返回
    /// （**绝对路径**）。只有一个时原样返回传进来的相对路径。
    ///
    /// 为什么这么做 —— 用户："之后我们再多生成几条"。以后往
    /// <c>assets/voice/yuki/</c> 里丢 <c>5_small_2.ogg</c>、<c>5_small_3.ogg</c>…
    /// **不用改一行代码**，这里自己就扫得到、随机轮着说。现在每句只有一个文件，行为跟以前完全一样。
    /// ⚠️ 同名不同后缀（.ogg/.wav）也算两个版本，会随机挑。
    /// </summary>
    private static string Pick(string file, int seat)
    {
        var all = Variants(file);
        if (all.Length == 0) return file;

        // ★ 2026-10-08 修：只有一个版本时**必须用扫出来的绝对路径**，不能返回传进来的相对名 ——
        //   我们现在的命名是 <事件>_1.ogg（没有不带 _1 的本体文件），把 13_win.ogg 这种"词干名"
        //   原样交给 LoadAny 会找不到文件 → 静默不播 → 胜利语音整个哑掉（实测日志里 13_win 零次）。
        if (all.Length == 1) return all[0];

        // 尽量别连着说同一句（最多试 6 次，全撞上就随便给一个）
        for (var i = 0; i < 6; i++)
        {
            var cand = all[System.Random.Shared.Next(all.Length)];
            if (cand != LastResolved[seat]) return cand;
        }
        return all[System.Random.Shared.Next(all.Length)];
    }

    /// <summary>扫出"同一句的所有版本"（带缓存；出错了就当成只有一个）。</summary>
    private static string[] Variants(string file)
    {
        if (VariantCache.TryGetValue(file, out var hit)) return hit;

        string[] result;
        try
        {
            result = ScanVariants(file);
        }
        catch (System.Exception)
        {
            result = new[] { file };   // 扫不动就当只有一个，绝不因为找素材把语音搞没
        }

        VariantCache[file] = result;
        return result;
    }

    private static string[] ScanVariants(string file)
    {
        // 绝对路径（借别人的语音包）**不展开**：那边一个类别里混着各种台词，
        // 按前缀扫反而可能把别的事件扫进来。储君那套自己已经随机过了。
        if (System.IO.Path.IsPathRooted(file)) return new[] { file };

        var stem = Stem(file);
        var slash = stem.LastIndexOf('/');
        if (slash <= 0) return new[] { file };

        var root = DouSpireAssets.RootDir;
        if (string.IsNullOrEmpty(root)) return new[] { file };

        var dir = (root + "/voice/" + stem[..slash]).Replace('\\', '/');
        if (!Directory.Exists(dir)) return new[] { file };

        var leaf = stem[(slash + 1)..];
        var found = new System.Collections.Generic.List<string>();
        foreach (var f in Directory.GetFiles(dir, leaf + "*"))
        {
            var ext = System.IO.Path.GetExtension(f);
            if (ext.Equals(".ogg", System.StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".wav", System.StringComparison.OrdinalIgnoreCase))
                found.Add(f.Replace('\\', '/'));
        }

        if (found.Count == 0) return new[] { file };
        found.Sort(System.StringComparer.OrdinalIgnoreCase);
        return found.ToArray();
    }

    /// <summary><c>"yuki/5_small.ogg"</c> → <c>"yuki/5_small"</c>；顺手把绝对路径的前缀也去掉比较。</summary>
    private static string Stem(string f)
    {
        var dot = f.LastIndexOf('.');
        var slash = f.LastIndexOf('/');
        return dot > slash ? f[..dot] : f;
    }

    /// <summary>
    /// 读一个音频流（带缓存）。**相对路径**按 <c>&lt;mod根&gt;/voice/&lt;相对&gt;</c> 找，
    /// **绝对路径**直接用 —— 两条路统一到这一个函数，省得再分两套。
    /// </summary>
    private static AudioStream? LoadAny(string f)
    {
        if (Cache.TryGetValue(f, out var hit)) return hit;

        AudioStream? s = null;
        try
        {
            string? full = null;
            if (System.IO.Path.IsPathRooted(f))
            {
                full = f.Replace('\\', '/');
            }
            else
            {
                var root = DouSpireAssets.RootDir;
                if (!string.IsNullOrEmpty(root)) full = root + "/voice/" + f;
            }

            if (full is not null && File.Exists(full))
            {
                s = full.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase)
                    ? AudioStreamOggVorbis.LoadFromFile(full)
                    : AudioStreamWav.LoadFromFile(full);
            }
        }
        catch (System.Exception e)
        {
            if (_fails++ < 3) Entry.Logger.Warn($"斗地主：语音文件读不了 {f}: {e.Message}");
        }

        Cache[f] = s;
        return s;
    }

    /// <summary>取（或建）某一席的播放器。</summary>
    private static AudioStreamPlayer? PlayerFor(int seat)
    {
        seat = Mathf.Clamp(seat, 0, Players.Length - 1);
        var p = Players[seat];
        if (p is not null && GodotObject.IsInstanceValid(p)) return p;

        var root = DouSpireUi.Root;
        if (root is null) return null;

        p = new AudioStreamPlayer { Bus = "Master" };
        root.AddChild(p);
        Players[seat] = p;
        return p;
    }

    /// <summary>
    /// 收桌时清缓存（美术换了 wav / 加了新 ogg，重新开桌就能听到）。
    /// ⚠️ 频率闸门那套状态**必须一起清** —— 留着上一局的"刚说过"记录，
    /// 新开一局的前几句会被莫名节流掉。
    /// </summary>
    public static void ClearCache()
    {
        Cache.Clear();
        VariantCache.Clear();
        RegentCache.Clear();
        for (var i = 0; i < LastSpokeAt.Length; i++)
        {
            LastSpokeAt[i] = 0;
            LastResolved[i] = "";
            NowTalking[i] = VoicePriority.Normal;
        }
        _fails = 0;
        _dead = false;
    }

    // ══════════════ 事件 → 说哪一句（鲸鱼娘）══════════════
    //  素材是 DSH 网页版「鲸鱼挂件」那 40 条助手内心 OS 的原句，
    //  用户用**可莉的音色**在 FishAudio 生成的，放在 assets/voice/whale/。
    //  文件名形如 <编号>_<事件>_<第几句>.ogg。

    /// <summary>鲸鱼娘对某件事的"台词"。返回 null = 这件事她不出声。</summary>
    private static string? FileFor(DouSpireCharacters.Mood m) => m switch
    {
        // 开局 / 场面
        DouSpireCharacters.Mood.Deal => "whale/1_deal.ogg",
        DouSpireCharacters.Mood.Landlord => "whale/2_landlord.ogg",
        DouSpireCharacters.Mood.MyTurn => "whale/3_myturn.ogg",
        DouSpireCharacters.Mood.Hint => "whale/4_hint.ogg",
        DouSpireCharacters.Mood.Picking => null,                 // 选牌太频繁，别吵

        // 出牌
        DouSpireCharacters.Mood.Single => "whale/5_small.ogg",
        DouSpireCharacters.Mood.Pair => "whale/5_small.ogg",
        DouSpireCharacters.Mood.Triple => "whale/6_triple.ogg",
        DouSpireCharacters.Mood.TripleWithSingle => "whale/6_triple.ogg",
        DouSpireCharacters.Mood.TripleWithPair => "whale/6_triple.ogg",
        DouSpireCharacters.Mood.Straight => "whale/7_straight.ogg",
        DouSpireCharacters.Mood.ConsecutivePairs => "whale/7_straight.ogg",
        DouSpireCharacters.Mood.Plane => "whale/8_big.ogg",
        DouSpireCharacters.Mood.PlaneWithSingles => "whale/8_big.ogg",
        DouSpireCharacters.Mood.PlaneWithPairs => "whale/8_big.ogg",
        DouSpireCharacters.Mood.FourWithTwoSingles => "whale/8_big.ogg",
        DouSpireCharacters.Mood.FourWithTwoPairs => "whale/8_big.ogg",
        DouSpireCharacters.Mood.Bomb => "whale/9_bomb.ogg",
        DouSpireCharacters.Mood.Rocket => "whale/9_bomb.ogg",

        // 其他场面
        // Pass：2026-10-08 补 —— 用户生成了要不起的语音（原来整个槽是空的，鲸鱼娘接不上时从来是哑的）
        DouSpireCharacters.Mood.Pass => "whale/10_pass.ogg",
        DouSpireCharacters.Mood.Beat => "whale/11_beat.ogg",
        DouSpireCharacters.Mood.LastOne => "whale/12_lastone.ogg",
        DouSpireCharacters.Mood.RivalLastOne => "whale/12_lastone.ogg",
        DouSpireCharacters.Mood.MateLastOne => "whale/12_lastone.ogg",   // 队友报单：先用普通报单兜底
        DouSpireCharacters.Mood.Win => "whale/13_win.ogg",
        DouSpireCharacters.Mood.Lose => "whale/14_lose.ogg",
        _ => null,
    };

    // ══════════════ 语音包⑥：本体四角色（战士 / 猎人 / 鸡煲 / 骨妹）══════════════
    //
    //  台词全是**斗地主梗**（陈刀仔、得得得得、地主家没有余粮…），用户自己用 FishAudio 生成，
    //  丢进 assets/voice/<ironclad|silent|defect|necrobinder>/。
    //  文件名跟编外那几套完全一致：<编号>_<事件>[_n].ogg —— 同前缀多文件**自动随机轮播**（见 Pick），
    //  所以以后想给某句加第二个版本，直接丢个 5_small_2.ogg 进去就行，代码不用动。
    //  目录由 <see cref="DouSpireCharacters.BaseVoiceDir"/> 按角色内部 id 认。

    /// <summary>牌局事件 → 语音文件名（不含目录和后缀）。本体四角色共用这一套事件表。</summary>
    private static string? EventOf(DouSpireCharacters.Mood m) => m switch
    {
        DouSpireCharacters.Mood.Deal => "1_deal",
        DouSpireCharacters.Mood.Landlord => "2_landlord",
        DouSpireCharacters.Mood.MyTurn => "3_myturn",
        DouSpireCharacters.Mood.Hint => "4_hint",

        DouSpireCharacters.Mood.Single => "5_small",
        DouSpireCharacters.Mood.Pair => "5_small",
        DouSpireCharacters.Mood.Triple => "6_triple",
        DouSpireCharacters.Mood.TripleWithSingle => "6_triple",
        DouSpireCharacters.Mood.TripleWithPair => "6_triple",
        DouSpireCharacters.Mood.Straight => "7_straight",
        DouSpireCharacters.Mood.ConsecutivePairs => "7_straight",

        DouSpireCharacters.Mood.Plane => "8_big",
        DouSpireCharacters.Mood.PlaneWithSingles => "8_big",
        DouSpireCharacters.Mood.PlaneWithPairs => "8_big",
        DouSpireCharacters.Mood.FourWithTwoSingles => "8_big",
        DouSpireCharacters.Mood.FourWithTwoPairs => "8_big",
        DouSpireCharacters.Mood.Bomb => "9_bomb",
        DouSpireCharacters.Mood.Rocket => "9_bomb",

        DouSpireCharacters.Mood.Pass => "10_pass",
        DouSpireCharacters.Mood.Beat => "11_beat",
        DouSpireCharacters.Mood.LastOne => "12_lastone",
        DouSpireCharacters.Mood.RivalLastOne => "12_lastone",
        DouSpireCharacters.Mood.MateLastOne => "12_matelast",   // 2v2 队友报单独立槽（2026-10-09）：没有素材就安静
        DouSpireCharacters.Mood.Win => "13_win",
        DouSpireCharacters.Mood.Lose => "14_lose",
        _ => null,   // Idle / Picking 不说话
    };

    /// <summary>
    /// 让**本体角色**用他自己那套梗台词回应一件事。目录里没对应素材就安静（<c>Load</c> 返回 null）。
    /// 这就是「桌边坐的是游戏角色也能开口说斗地主梗」的那条路。
    /// </summary>
    public static void ReactBase(string dir, DouSpireCharacters.Mood m, int seat)
    {
        var ev = EventOf(m);
        if (ev is null) return;
        Say(dir + "/" + ev + ".ogg", seat, 0.85f, PriorityOf(m));
    }

    /// <summary>
    /// 这件事"有多要紧"。**牌桌上最高频的两个事件（出小牌 / 压人一手）故意划成 Chatter**，
    /// 让它们被节流；其余照常。改这张表就等于改语音的频率手感，
    /// 见 <see cref="Admit"/> 的说明。
    /// </summary>
    private static VoicePriority PriorityOf(DouSpireCharacters.Mood m) => m switch
    {
        // 一定要说（不节流、可打断）
        DouSpireCharacters.Mood.Deal
            or DouSpireCharacters.Mood.Landlord
            or DouSpireCharacters.Mood.Bomb
            or DouSpireCharacters.Mood.Rocket
            or DouSpireCharacters.Mood.LastOne
            or DouSpireCharacters.Mood.RivalLastOne
            or DouSpireCharacters.Mood.MateLastOne
            or DouSpireCharacters.Mood.Win
            or DouSpireCharacters.Mood.Lose
            => VoicePriority.Important,

        // 碎嘴：一局里会重复几十次的那几句
        DouSpireCharacters.Mood.Single
            or DouSpireCharacters.Mood.Pair
            or DouSpireCharacters.Mood.Pass
            or DouSpireCharacters.Mood.Beat
            => VoicePriority.Chatter,

        // 其余（三带/顺子/连对/飞机/四带二/轮到我/点提示）照常
        _ => VoicePriority.Normal,
    };

    /// <summary>编外角色对某件事说一句（没有对应台词就不出声）。</summary>
    public static void ReactTo(DouSpireCharacters.Mood m, int seat)
    {
        var f = FileFor(m);
        if (f is not null) Say(f, seat, 0.85f, PriorityOf(m));
    }

    /// <summary>
    /// **按某一席坐的是谁**，用他自己那套语音包回应一件事 —— 桌子无关的公共分发。
    ///
    /// 斗地主（<c>DouSpireTable.WhaleReact</c>）和德州（<c>DouSpirePokerTable.Speak</c>）
    /// **共用这一份**。以前这段 switch 只写在斗地主那边，德州接语音时若再抄一份，
    /// 以后加一个语音包就得改两处、早晚漂掉 —— 所以抽到这里来。
    ///
    /// 六条路：
    /// <list type="bullet">
    ///   <item>编外四套（whale / yuki / sherry / chun）走各自的槽位表；</item>
    ///   <item>本体四角色走 <see cref="ReactBase"/>（目录里没素材就安静）；</item>
    ///   <item>储君走 <see cref="ReactRegent"/>（借玩家装的第三方语音包，没装就安静）。</item>
    /// </list>
    /// 认不出来的角色 / 空座 = 什么都不做（这是正常降级，不刷日志）。
    /// </summary>
    public static void ReactAs(DouSpireCharacters.SeatPick c, DouSpireCharacters.Mood mood, int seat)
    {
        switch (c.GuestId)
        {
            case "whale": ReactTo(mood, seat); break;
            case "yuki": ReactYuki(mood, seat); break;
            case "sherry": ReactSherry(mood, seat); break;
            case "chun": ReactChun(mood, seat); break;
            default:
                if (IsRegent(c)) ReactRegent(mood, seat);
                else if (DouSpireCharacters.BaseVoiceDir(c) is { } dir)
                    ReactBase(dir, mood, seat);   // 本体四角色自己的梗台词
                break;
        }
    }

    /// <summary>储君（摄政王）那一席 —— 只有他走"君宝娘化"那套语音。</summary>
    private static bool IsRegent(DouSpireCharacters.SeatPick c)
        => !c.IsGuest
           && DouSpireCharacters.EntryOf(c).Contains("regent", System.StringComparison.OrdinalIgnoreCase);

    // ══════════════ 语音包②：储君 —— **运行时借用玩家装的第三方语音包** ══════════════
    //
    //  ── 为什么改成"借用"而不是自带 ──────────────────────────────────────
    //  原来这里扫的是 `assets/voice/regent/<类别>/<台词>.ogg`，注释还写着"斗地主自带一份"。
    //  但那份素材**已经被删掉了**，于是储君这一席永远是哑的（代码在找一个不存在的目录）。
    //
    //  用户 2026-10-08 定的规矩：
    //  > "我下载了储君语音mod。所以应该生效储君的语音，然后没下载储君语音的不生效。"
    //  > "万一别人用mod角色玩引用mod角色的动作和声音。我们斗地主一样能玩能发出声音"
    //
    //  也就是**加性叠加**（跟「尖塔牌面」同一套思路：不自己造一份，而是接上别人已有的）：
    //    ① 玩家装了储君语音 mod  → 用它的
    //    ② 玩家没装              → 静默，不报错不崩
    //    ③ **绝不把别人的音频打包进我们的包**（版权 + 包体都受不了）
    //
    //  ── 找什么 ────────────────────────────────────────────────────────
    //  本机实测：`mods/君宝娘化/`（VoiceModFramework，N 网 880）里是
    //    voices_regent/<英文类别>/<日文台词>.ogg   —— 33 个类别、约 160 个文件。
    //  目录类别名是**统一约定**（combat_start / card / victory / defeat …），
    //  台词文件名是各语音包自己的（这里全是日文），所以只需按目录名找、文件名随便挑一个。
    //
    //  ── 怎么找 ────────────────────────────────────────────────────────
    //  走游戏的 `ModManager.Mods`（每个 mod 带 `path` 字段）—— 这是官方给的入口，
    //  比自己去猜 `mods/` 在哪稳得多。找不到就返回空，外面自然会安静。

    /// <summary>某个事件用储君的哪一类语音（返回 null = 这件事不说话）。</summary>
    private static string? RegentCategory(DouSpireCharacters.Mood m) => m switch
    {
        DouSpireCharacters.Mood.Deal => "combat_start",        // "让朕瞧瞧你的本事"
        DouSpireCharacters.Mood.Landlord => "boss_start",      // "朕的全力……"
        DouSpireCharacters.Mood.MyTurn => "char_select",
        DouSpireCharacters.Mood.Hint => "buff",

        DouSpireCharacters.Mood.Single => "card",
        DouSpireCharacters.Mood.Pair => "card",
        DouSpireCharacters.Mood.Triple => "card",
        DouSpireCharacters.Mood.TripleWithSingle => "card",
        DouSpireCharacters.Mood.TripleWithPair => "card",
        DouSpireCharacters.Mood.Straight => "buff",
        DouSpireCharacters.Mood.ConsecutivePairs => "buff",
        DouSpireCharacters.Mood.Plane => "boss_victory",
        DouSpireCharacters.Mood.PlaneWithSingles => "boss_victory",
        DouSpireCharacters.Mood.PlaneWithPairs => "boss_victory",
        DouSpireCharacters.Mood.FourWithTwoSingles => "card_big_bang",
        DouSpireCharacters.Mood.FourWithTwoPairs => "card_big_bang",
        DouSpireCharacters.Mood.Bomb => "card_big_bang",       // 星辰大招，最华丽
        DouSpireCharacters.Mood.Rocket => "card_big_bang",

        DouSpireCharacters.Mood.Pass => "damage_partial",      // "才不痛呢~"
        DouSpireCharacters.Mood.Beat => "debuff_apply",        // "弱哦~，弱，弱~"
        DouSpireCharacters.Mood.LastOne => "low_hp",           // "朕……要输了吗"
        DouSpireCharacters.Mood.RivalLastOne => "damage",
        DouSpireCharacters.Mood.MateLastOne => "low_hp",       // 队友报单：先用"要输了吗"兜底
        DouSpireCharacters.Mood.Win => "victory",              // "将死~！是朕赢了"
        DouSpireCharacters.Mood.Lose => "defeat",
        _ => null,
    };

    private static readonly System.Collections.Generic.Dictionary<string, string[]> RegentCache = new();

    /// <summary>
    /// 第三方语音包可能的根目录名（按优先级）。装到哪个名字都能认出来。
    /// <c>voices_regent</c> 是 VoiceModFramework 那套（君宝娘化 / N 网 880）的约定；
    /// 后面几个是自己兜的别名，防别的作者用别的名字。
    /// </summary>
    private static readonly string[] RegentDirNames =
    {
        "voices_regent", "voice_regent", "regent_voice", "voices/regent",
    };

    /// <summary>
    /// 找出**玩家装的那个储君语音包**的根目录（绝对路径，正斜杠）。
    /// 找不到返回 null —— 那就静默，一句话都不说。
    ///
    /// 走 <c>ModManager.Mods</c>：每个已加载的 mod 都带 <c>path</c>（mod 所在文件夹），
    /// 我们挨个进去看有没有 <c>voices_regent/</c> 之类的目录。
    /// **跳过自己**（DouSpire），免得自引用。
    /// </summary>
    private static string? FindThirdPartyRegentRoot()
    {
        // 冷路径，只跑一次，结果缓存在 _regentRootResolved 里。
        if (_regentRootResolved) return _regentRoot;

        _regentRootResolved = true;
        try
        {
            foreach (var mod in MegaCrit.Sts2.Core.Modding.ModManager.Mods)
            {
                try
                {
                    var modPath = mod?.path;
                    if (string.IsNullOrEmpty(modPath)) continue;

                    // 跳过自己
                    var id = mod!.manifest?.id;
                    if (!string.IsNullOrEmpty(id)
                        && id.Equals(Entry.ModId, System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    foreach (var name in RegentDirNames)
                    {
                        var cand = modPath!.Replace('\\', '/').TrimEnd('/') + "/" + name;
                        if (Directory.Exists(cand))
                        {
                            Entry.Logger.Info($"斗地主：储君语音——发现第三方语音包 {modPath}");
                            return _regentRoot = cand;
                        }
                    }
                }
                catch (System.Exception) { /* 单个 mod 读不了就跳过 */ }
            }
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：扫描第三方语音包失败（不影响玩）: {e.Message}");
        }

        // 兜底：ModManager 拿不到时，从自己所在目录往上一级找（就是 mods/ 那层）。
        try
        {
            var root = DouSpireAssets.RootDir;                 // .../mods/DouSpire/assets
            if (!string.IsNullOrEmpty(root))
            {
                var modsDir = System.IO.Path.GetDirectoryName(
                    System.IO.Path.GetDirectoryName(root.TrimEnd('/')));  // → .../mods
                if (!string.IsNullOrEmpty(modsDir) && Directory.Exists(modsDir))
                {
                    foreach (var sub in Directory.GetDirectories(modsDir))
                    {
                        if (System.IO.Path.GetFileName(sub)
                            .Equals(Entry.ModId, System.StringComparison.OrdinalIgnoreCase))
                            continue;
                        foreach (var name in RegentDirNames)
                        {
                            var cand = sub.Replace('\\', '/').TrimEnd('/') + "/" + name;
                            if (Directory.Exists(cand))
                            {
                                Entry.Logger.Info($"斗地主：储君语音——兜底扫描发现 {sub}");
                                return _regentRoot = cand;
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception) { /* 找不到就安静 */ }

        return _regentRoot;
    }

    private static string? _regentRoot;
    private static bool _regentRootResolved;
    private static bool _regentReportedEmpty;

    /// <summary>
    /// 某个类别下的全部 ogg（**绝对路径**，缓存；找不到就返回空数组）。
    ///
    /// 跟以前不同：以前返回的是自己的相对路径（"regent/cat/x.ogg"），
    /// 现在返回**别人的绝对路径** —— 因为音频根本不在我们的包体里。
    /// </summary>
    private static string[] RegentFiles(string cat)
    {
        if (RegentCache.TryGetValue(cat, out var hit)) return hit;

        var list = new System.Collections.Generic.List<string>();
        try
        {
            var root = FindThirdPartyRegentRoot();
            if (!string.IsNullOrEmpty(root))
            {
                var dir = root + "/" + cat;
                if (Directory.Exists(dir))
                    foreach (var f in Directory.GetFiles(dir, "*.ogg"))
                        list.Add(f.Replace('\\', '/'));
            }
        }
        catch (System.Exception) { /* 列不出来就当没有 */ }

        var arr = list.ToArray();
        RegentCache[cat] = arr;
        return arr;
    }

    /// <summary>让某个编外角色用储君的语音回应某件事。</summary>
    public static void ReactRegent(DouSpireCharacters.Mood m, int seat)
    {
        var cat = RegentCategory(m);
        if (cat is null) return;
        var files = RegentFiles(cat);
        if (files.Length == 0)
        {
            // 只报一次：没装储君语音包是**正常情况**，不该刷屏。
            if (!_regentReportedEmpty)
            {
                _regentReportedEmpty = true;
                Entry.Logger.Info("斗地主：储君这一席没有语音包（玩家没装），保持安静。");
            }
            return;
        }
        SayAbs(files[System.Random.Shared.Next(files.Length)], seat, 0.85f, PriorityOf(m));
    }

    /// <summary>收桌时把储君的目录缓存也清掉（重新开桌会重新找一遍）。</summary>
    public static void ClearRegentCache()
    {
        RegentCache.Clear();
        _regentRoot = null;
        _regentRootResolved = false;
        _regentReportedEmpty = false;
    }

    // ══════════════ 语音包③：八千代辉夜姬（FishAudio 自己生成的，版权干净）══════════════
    //  素材在 assets/voice/yuki/<编号>.ogg —— 日文，软糯妹系。
    //  **这套是用户自己花钱/花额度生成的，不受任何第三方 mod 的版权约束。**

    private static string? YukiFile(DouSpireCharacters.Mood m) => m switch
    {
        DouSpireCharacters.Mood.Deal => "1_deal",
        DouSpireCharacters.Mood.Landlord => "2_landlord",
        DouSpireCharacters.Mood.MyTurn => "3_myturn",
        DouSpireCharacters.Mood.Hint => "4_hint",

        DouSpireCharacters.Mood.Single => "5_small",
        DouSpireCharacters.Mood.Pair => "5_small",
        DouSpireCharacters.Mood.Triple => "6_triple",
        DouSpireCharacters.Mood.TripleWithSingle => "6_triple",
        DouSpireCharacters.Mood.TripleWithPair => "6_triple",
        DouSpireCharacters.Mood.Straight => "7_straight",
        DouSpireCharacters.Mood.ConsecutivePairs => "7_straight",

        DouSpireCharacters.Mood.Plane => "8_big",
        DouSpireCharacters.Mood.PlaneWithSingles => "8_big",
        DouSpireCharacters.Mood.PlaneWithPairs => "8_big",
        DouSpireCharacters.Mood.FourWithTwoSingles => "8_big",
        DouSpireCharacters.Mood.FourWithTwoPairs => "8_big",
        DouSpireCharacters.Mood.Bomb => "9_bomb",
        DouSpireCharacters.Mood.Rocket => "9_bomb",

        DouSpireCharacters.Mood.Pass => "10_pass",
        DouSpireCharacters.Mood.Beat => "11_beat",
        DouSpireCharacters.Mood.LastOne => "12_lastone",
        DouSpireCharacters.Mood.RivalLastOne => "12_lastone",
        DouSpireCharacters.Mood.MateLastOne => "12_lastone",   // 队友报单：先用普通报单兜底
        DouSpireCharacters.Mood.Win => "13_win",
        DouSpireCharacters.Mood.Lose => "14_lose",
        _ => null,
    };

    /// <summary>让八千代用她自己那套（FishAudio 生成的日文）回应某件事。</summary>
    public static void ReactYuki(DouSpireCharacters.Mood m, int seat)
    {
        var f = YukiFile(m);
        if (f is not null) Say("yuki/" + f + ".ogg", seat, 0.85f, PriorityOf(m));
    }

    /// <summary>
    /// 「抢地主 / 不抢」——这两句不在 <see cref="DouSpireCharacters.Mood"/> 里
    /// （抢地主是开局前的事，不是牌局事件），所以单独一条路。
    /// 编外角色有自己的就说自己的；没有就什么都不播（外面会退回游戏 NPC 的台词）。
    /// </summary>
    public static bool ReactBid(string? guestId, bool want, int seat = 0)
    {
        // 有自己配音包的角色。鲸鱼娘那批文件名带 `_1` 序号，另两个没有。
        string? f = guestId switch
        {
            "yuki" => want ? "yuki/15_bid.ogg" : "yuki/16_nobid.ogg",
            "sherry" => want ? "sherry/15_bid.ogg" : "sherry/16_nobid.ogg",
            "whale" => want ? "whale/15_bid_1.ogg" : "whale/16_nobid.ogg",
            "chun" => want ? "chun/15_bid_1.ogg" : "chun/16_nobid.ogg",
            _ => null,
        };
        if (f is null) return false;
        // 抢地主 / 不抢是**开局的一次性事件**，而且玩家会觉得"她答了我一句"，
        // 所以划成 Important：永远说得出来，不会被节流掉。
        Say(f, seat, 0.9f, VoicePriority.Important);
        return true;
    }

    // ══════════════ 语音包④：橘雪莉（同样是自己生成的，版权干净）══════════════
    //  《魔法少女的魔女审判》里那个自称名侦探的少女 —— 奶声奶气但张口就是"犯人确定了"。

    private static string? SherryFile(DouSpireCharacters.Mood m) => m switch
    {
        DouSpireCharacters.Mood.Deal => "1_deal",
        DouSpireCharacters.Mood.Landlord => "2_landlord",
        DouSpireCharacters.Mood.MyTurn => "3_myturn",
        DouSpireCharacters.Mood.Hint => "4_hint",

        DouSpireCharacters.Mood.Single => "5_small",
        DouSpireCharacters.Mood.Pair => "5_small",
        DouSpireCharacters.Mood.Triple => "6_triple",
        DouSpireCharacters.Mood.TripleWithSingle => "6_triple",
        DouSpireCharacters.Mood.TripleWithPair => "6_triple",
        DouSpireCharacters.Mood.Straight => "7_straight",
        DouSpireCharacters.Mood.ConsecutivePairs => "7_straight",

        DouSpireCharacters.Mood.Plane => "8_big",
        DouSpireCharacters.Mood.PlaneWithSingles => "8_big",
        DouSpireCharacters.Mood.PlaneWithPairs => "8_big",
        DouSpireCharacters.Mood.FourWithTwoSingles => "8_big",
        DouSpireCharacters.Mood.FourWithTwoPairs => "8_big",
        DouSpireCharacters.Mood.Bomb => "9_bomb",
        DouSpireCharacters.Mood.Rocket => "9_bomb",

        DouSpireCharacters.Mood.Pass => "10_pass",
        DouSpireCharacters.Mood.Beat => "11_beat",
        DouSpireCharacters.Mood.LastOne => "12_lastone",
        DouSpireCharacters.Mood.RivalLastOne => "12_lastone",
        DouSpireCharacters.Mood.MateLastOne => "12_lastone",   // 队友报单：先用普通报单兜底
        DouSpireCharacters.Mood.Win => "13_win",
        DouSpireCharacters.Mood.Lose => "14_lose",
        _ => null,
    };

    /// <summary>让橘雪莉用她自己那套（名侦探腔的日文）回应某件事。</summary>
    public static void ReactSherry(DouSpireCharacters.Mood m, int seat)
    {
        var f = SherryFile(m);
        if (f is not null) Say("sherry/" + f + ".ogg", seat, 0.85f, PriorityOf(m));
    }

    // ══════════════ 语音包⑤：椿（鸣潮，中文·危险御姐）══════════════
    //  素材在 assets/voice/chun/ —— 同样是自己用 FishAudio 生成的。

    private static string? ChunFile(DouSpireCharacters.Mood m) => m switch
    {
        DouSpireCharacters.Mood.Deal => "chun/1_deal.ogg",
        DouSpireCharacters.Mood.Landlord => "chun/2_landlord.ogg",
        DouSpireCharacters.Mood.MyTurn => "chun/3_myturn.ogg",
        DouSpireCharacters.Mood.Hint => "chun/4_hint.ogg",

        DouSpireCharacters.Mood.Single => "chun/5_small.ogg",
        DouSpireCharacters.Mood.Pair => "chun/5_small.ogg",
        DouSpireCharacters.Mood.Triple => "chun/6_triple.ogg",
        DouSpireCharacters.Mood.TripleWithSingle => "chun/6_triple.ogg",
        DouSpireCharacters.Mood.TripleWithPair => "chun/6_triple.ogg",
        DouSpireCharacters.Mood.Straight => "chun/7_straight.ogg",
        DouSpireCharacters.Mood.ConsecutivePairs => "chun/7_straight.ogg",

        DouSpireCharacters.Mood.Plane => "chun/8_big.ogg",
        DouSpireCharacters.Mood.PlaneWithSingles => "chun/8_big.ogg",
        DouSpireCharacters.Mood.PlaneWithPairs => "chun/8_big.ogg",
        DouSpireCharacters.Mood.FourWithTwoSingles => "chun/8_big.ogg",
        DouSpireCharacters.Mood.FourWithTwoPairs => "chun/8_big.ogg",
        DouSpireCharacters.Mood.Bomb => "chun/9_bomb.ogg",
        DouSpireCharacters.Mood.Rocket => "chun/9_bomb.ogg",

        DouSpireCharacters.Mood.Pass => "chun/10_pass.ogg",
        DouSpireCharacters.Mood.Beat => "chun/11_beat.ogg",
        DouSpireCharacters.Mood.LastOne => "chun/12_lastone.ogg",
        DouSpireCharacters.Mood.RivalLastOne => "chun/12_lastone.ogg",
        DouSpireCharacters.Mood.MateLastOne => "chun/12_lastone.ogg",   // 队友报单：先用普通报单兜底
        DouSpireCharacters.Mood.Win => "chun/13_win.ogg",
        DouSpireCharacters.Mood.Lose => "chun/14_lose.ogg",
        _ => null,
    };

    /// <summary>让椿用她自己那套（鸣潮的中文御姐音）回应某件事。</summary>
    public static void ReactChun(DouSpireCharacters.Mood m, int seat)
    {
        var f = ChunFile(m);
        if (f is not null) Say(f, seat, 0.85f, PriorityOf(m));
    }
}
