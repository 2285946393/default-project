using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Audio;
using System.Linq;
using System.Reflection;

namespace DouSpire.Table;

/// <summary>
/// 斗地主的音效 —— **直接用杀戮尖塔 2 自己的音效**（用户："把杀戮尖塔的各种音效加进去"）。
///
/// 怎么找到的：游戏的音频走 FMOD，音频文件在**加密的 pck 里拿不出来**，
/// 但音效的**事件名**是硬编码在 <c>sts2.dll</c> 里的字符串（UTF-16 存的），
/// 扫一遍就能拿到 363 条真实路径，例如 <c>event:/sfx/ui/cards/card_impact_into_single</c>。
/// 播放走游戏自己的 <see cref="SfxCmd.Play(string, float)"/>，
/// 所以它跟游戏里的音量设置（设置 → 音频 → 音效）是同一条通道，不用另开播放器。
///
/// 两条铁律（和界面那套一样）：
/// <list type="bullet">
///   <item>**任何一步出错都吞掉**，绝不让音效把牌桌搞崩 —— 没声音只是安静。</item>
///   <item>连着出错 3 次就**永久闭嘴**，不然每帧刷一条日志更烦。</item>
/// </list>
/// </summary>
public static class DouSpireSfx
{
    // ══════════════ 从 sts2.dll 里扫出来的真实 FMOD 事件路径 ══════════════

    // 牌
    private const string CardSingle = "event:/sfx/ui/cards/card_impact_into_single";
    private const string CardMulti = "event:/sfx/ui/cards/card_impact_into_multi";
    private const string CardToDraw = "event:/sfx/ui/cards/card_movement_B_into_draw";
    private const string CardToDiscard = "event:/sfx/ui/cards/card_movement_B_play_into_discard";

    // 点击
    private const string UiClick = "event:/sfx/ui/clicks/ui_click";
    private const string UiHover = "event:/sfx/ui/clicks/ui_hover";
    private const string UiBack = "event:/sfx/ui/clicks/ui_back";

    // 反馈
    private const string Gold1 = "event:/sfx/ui/gold/gold_1";
    private const string Gold2 = "event:/sfx/ui/gold/gold_2";
    private const string Gold3 = "event:/sfx/ui/gold/gold_3";
    private const string MapSelect = "event:/sfx/ui/map/map_select";
    private const string MapOpen = "event:/sfx/ui/map/map_open";
    private const string MapClose = "event:/sfx/ui/map/map_close";
    private const string BlockBreak = "event:/sfx/block_break";
    private const string Buff = "event:/sfx/buff";
    private const string Debuff = "event:/sfx/debuff";
    private const string GameOver = "event:/temp/sfx/game_over";

    // ── 原版角色的招牌音效（用户 2026-10-07："像什么超能光束什么天降金币这种"）──
    //    这些名字都是从 sts2.dll 里扫出来的真事件（完整清单：notes/杀戮尖塔2-音效事件全表.md）
    private const string HyperBeam = "event:/sfx/characters/defect/defect_hyperbeam";            // 超能光束
    private const string LightningEvoke = "event:/sfx/characters/defect/defect_lightning_evoke";
    private const string SovereignBlade = "event:/sfx/characters/regent/regent_sovereign_blade"; // 主权之刃
    private const string GainEnergy = "event:/sfx/ui/gain_energy";
    private const string Treasure3 = "event:/sfx/ui/treasure/treasure_act3";
    private const string Wipe = "event:/sfx/ui/wipe_";

    private static int _fails;

    // ══════════════ 底层 ══════════════

    /// <summary>
    /// 播一个官方音效。
    /// 音量 = <paramref name="volume"/> × 设置里的总音量；关掉设置里的开关就一声不响。
    ///
    /// 2026-10-07：这条修过一次 —— 原来 SfxCmd.Play 失败 3 次就**永久闭嘴**，
    /// 而牌桌是从主菜单开的（那边战斗管理器是空的），于是整局一声不响。
    /// 现在：SfxCmd 连续失败 3 次就**只换通道、不全局关音**，改走底层的 NAudioManager。
    /// </summary>
    public static void Play(string ev, float volume = 1f)
    {
        if (string.IsNullOrEmpty(ev)) return;
        if (!DouSpireConfig.SfxEnabled) return;

        var vol = Mathf.Clamp(volume * DouSpireConfig.SfxVolume, 0f, 1f);

        // ① 直接走底层音频管理器 —— 实测最靠得住的一条。
        //    为什么不是首选游戏自己的 SfxCmd.Play：牌桌是从主菜单开的，属于"非战斗"场景，
        //    那个接口会把声音**静默丢掉**（不报错、也不出声），所以日志里查不到任何异常。
        try
        {
            var am = NAudioManager.Instance;
            if (am is not null) { am.PlayOneShot(ev, vol); return; }
        }
        catch (System.Exception e)
        {
            if (_fails++ < 5) Entry.Logger.Warn($"斗地主：底层音频管理器播不出来: {e.Message}");
        }

        // ② 退回游戏自己的命令通道（战斗里这条更对路）
        try { SfxCmd.Play(ev, vol); return; }
        catch (System.Exception e)
        {
            if (_fails++ < 5) Entry.Logger.Warn($"斗地主：官方通道也播不出来: {e.Message}");
        }

        // ③ 最后再用反射兜底
        PlayRaw(ev, vol);
    }

    // ══════════════ 备用通道：绕开 SfxCmd，直接找游戏的音频管理器 ══════════════

    private const BindingFlags AnyStatic =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags AnyInst =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private static object? _audio;
    private static MethodInfo? _audioPlay;
    private static bool _probed;

    private static bool PlayRaw(string ev, float vol)
    {
        if (!_probed) Probe();
        if (_audio is null || _audioPlay is null) return false;

        try
        {
            var args = _audioPlay.GetParameters().Length >= 2
                ? new object[] { ev, vol }
                : new object[] { ev };
            _audioPlay.Invoke(_audio, args);
            return true;
        }
        catch (System.Exception e)
        {
            if (_fails++ < 5)
                Entry.Logger.Warn($"斗地主：备用音效通道也失败: {e.InnerException?.Message ?? e.Message}");
            return false;
        }
    }

    private static void Probe()
    {
        _probed = true;
        try
        {
            var asm = typeof(SfxCmd).Assembly;
            Type?[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types; }

            var t = types.FirstOrDefault(x => x is not null && x.Name == "NAudioManager");
            if (t != null)
            {
                _audio = ReadStatic(t, "Instance") ?? ReadStatic(t, "AudioManager");
                if (_audio is null)
                {
                    foreach (var p in t.GetProperties(AnyStatic))
                    {
                        if (p.PropertyType != t) continue;
                        _audio = Safe(() => p.GetValue(null));
                        if (_audio != null) break;
                    }
                }
            }

            if (_audio is null)
            {
                foreach (var p in typeof(SfxCmd).GetProperties(AnyStatic))
                {
                    if (p.Name != "AudioManager" && p.Name != "Instance") continue;
                    _audio = Safe(() => p.GetValue(null));
                    if (_audio != null) break;
                }
            }

            var at = _audio?.GetType();
            _audioPlay = at?.GetMethods(AnyInst)
                             .FirstOrDefault(m => m.Name == "PlayOneShot" && m.GetParameters().Length == 2)
                         ?? at?.GetMethods(AnyInst)
                             .FirstOrDefault(m => m.Name == "PlayOneShot" && m.GetParameters().Length == 1);

            Entry.Logger.Info(_audioPlay != null
                ? $"斗地主：音效备用通道已就绪（{at!.Name}.{_audioPlay.Name}）"
                : "斗地主：没找到游戏的音频管理器 —— 音效只能靠官方通道。");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：探测音效通道时出错: {e.Message}");
        }
    }

    private static object? ReadStatic(Type t, string name)
        => Safe(() => t.GetProperty(name, AnyStatic)?.GetValue(null))
           ?? Safe(() => t.GetField(name, AnyStatic)?.GetValue(null));

    private static object? Safe(System.Func<object?> f)
    {
        try { return f(); } catch { return null; }
    }

    // ══════════════ 语义接口（界面只调这些，不碰事件名）══════════════

    /// <summary>开桌。</summary>
    public static void Open()
    {
        Play(MapOpen);
        MusicCalm();
    }

    /// <summary>收桌。</summary>
    public static void Close()
    {
        Play(MapClose);
        MusicStop();
    }

    /// <summary>发新的一局（洗牌发牌声）。</summary>
    public static void Deal() => Play(CardToDraw);

    /// <summary>定地主、地主拿走底牌（金币声）。</summary>
    public static void Landlord() => Play(Gold3);

    /// <summary>
    /// 出了一手牌。<paramref name="count"/> = 几张；<paramref name="bomb"/> = 是不是炸弹/王炸（额外加一声破甲，更有劲）。
    /// </summary>
    public static void PlayCards(int count, bool bomb = false)
    {
        if (bomb)
        {
            Play(HyperBeam, 0.9f);      // 超能光束 —— 炸弹的牌面（用户点名要的）
            Play(BlockBreak, 0.55f);    // 破甲垫一下，别盖住光束
        }
        Play(count <= 1 ? CardSingle : CardMulti, bomb ? 1f : 0.85f);
    }

    /// <summary>王炸 —— 比炸弹再猛一档：光束 + 雷击。</summary>
    public static void Rocket()
    {
        Play(HyperBeam, 1f);
        Play(LightningEvoke, 0.75f);
        Play(BlockBreak, 0.5f);
        Play(CardMulti, 0.9f);
    }

    /// <summary>当上地主 —— 摄政王的主权之刃，加冕的味儿。</summary>
    public static void LordCrown() => Play(SovereignBlade, 0.85f);

    /// <summary>报单（我剩 1 张）—— 攒够能量的紧张感。</summary>
    public static void LastOne() => Play(GainEnergy, 0.7f);

    /// <summary>要不起 / 过牌。</summary>
    public static void Pass() => Play(UiBack, 0.8f);

    /// <summary>牌进了弃牌堆（每手结束）。</summary>
    public static void ToDiscard() => Play(CardToDiscard, 0.6f);

    /// <summary>点了一张手牌（选中 / 取消）。</summary>
    public static void Select() => Play(UiClick, 0.7f);

    /// <summary>鼠标划过一张手牌。声音很轻。</summary>
    public static void Hover() => Play(UiHover, 0.35f);

    /// <summary>轮到我出牌了。</summary>
    public static void MyTurn() => Play(MapSelect, 0.7f);

    /// <summary>这局我赢了（金币 + 增益）。</summary>
    public static void Win()
    {
        Play(Gold1);
        Play(Gold2, 0.75f);
        Play(Gold3, 0.6f);          // 三档金币 —— "天降金币"
        Play(Treasure3, 0.55f);     // 开宝箱，收个尾
    }

    /// <summary>这局我输了 / 结束了。</summary>
    public static void Lose()
    {
        Play(Wipe, 0.7f);           // 清场、冲刷的声音
        Play(Debuff, 0.65f);
    }

    // ══════════════ 背景音乐（2026-10-08 交给 DouSpireMusic）══════════════
    //
    // 原来这里只有"舒缓 / 紧张"两首，而且只能用游戏本体的曲子。
    // 现在整套搬到 <see cref="DouSpireMusic"/>：按**时机**分槽位（平时/紧张/高潮/胜负），
    // 可以放自己准备的歌（丢进 mods\DouSpire\assets\music\ 即可），槽位空着才回退本体曲。
    //
    // 这三个方法保留成**转发**，是为了不动历史调用点 —— 新代码直接用 DouSpireMusic。

    /// <summary>平时那首（舒缓）。</summary>
    public static void MusicCalm() => DouSpireMusic.Play(DouSpireMusicCue.Table);

    /// <summary>有人快走完了 —— 切紧张那首。</summary>
    public static void MusicTense() => DouSpireMusic.Play(DouSpireMusicCue.Tense);

    /// <summary>收桌时把音乐停掉。</summary>
    public static void MusicStop() => DouSpireMusic.Stop();

    // ── 语音：先蹭 NPC 台词 ──
    /// <summary>
    /// 人声台词。单独一条通道 —— 设置里能**单独关掉人声**，音效照旧
    ///（用户："音乐配置有点乱了，主要是角色还有单独的配音"）。
    /// </summary>
    public static void Voice(string ev, float volume = 0.95f)
    {
        if (!DouSpireConfig.VoiceEnabled) return;
        Play(ev, volume);
    }

    /// <summary>
    /// 角色自己那套配音（攻击 / 施法 / 倒地）—— 也是单独开关。
    /// </summary>
    public static void ActorVoice(string ev, float volume = 0.8f)
    {
        if (!DouSpireConfig.ActorVoiceEnabled) return;
        Play(ev, volume);
    }

    /// <summary>"抢地主" —— 兴奋地喊一声。</summary>
    public static void VoiceBid() => Voice("event:/sfx/npcs/darv/darv_excited");

    /// <summary>"不抢 / 要不起" —— 一声惊。</summary>
    public static void VoicePass() => Voice("event:/sfx/npcs/nonupeipe/nonupeipe_eeked");

    /// <summary>"赢了" —— 大笑。</summary>
    public static void VoiceWin() => Voice("event:/sfx/npcs/tanx/tanx_laugh");

    /// <summary>开桌问好。</summary>
    public static void VoiceHello() => Voice("event:/sfx/npcs/neow/neow_welcome", 0.9f);
}
