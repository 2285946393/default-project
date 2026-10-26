using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace DouSpire.Table;

/// <summary>
/// 斗地主桌上那三位是谁 —— 默认取**游戏本体**的角色（铁甲战士 / 沉默猎手 / ……），
/// 用它们自己的立绘、名字和专属配色。
///
/// 这是"跟游戏有关系"的关键：牌还是数字牌，但坐在桌边的是你在尖塔里选的那个人。
/// 用户原话：「**就一个桌子然后你选的角色，和ai选的角色出现**」。
///
/// 立绘走 <see cref="CharacterModel.IconTexture"/>，也就是游戏顶栏那个角色小头像
/// （<c>ui/top_panel/character_icon_&lt;entry&gt;.png</c>）。
/// 更"坐着"的姿势在 <c>rest_site/characters/&lt;entry&gt;_rest_site</c> 场景里，
/// 但那是整套动画场景，第一版先用静态立绘，稳。
///
/// 2026-10-07 起多了一类**编外角色**：DS鲸鱼娘。她不是游戏本体角色（没有卡牌、没有技能），
/// 但在斗地主里她只需要「一张立绘 + 一个名字」，所以用 <see cref="SeatPick"/> 把
/// 「游戏角色」和「编外角色」统一表示，桌边三席走同一套代码。
/// </summary>
public static class DouSpireCharacters
{
    private static List<CharacterModel>? _roster;
    private static List<SeatPick>? _pool;

    // ══════════════════════════════════════════════════════════════
    //  编外角色 —— 不是游戏本体的人，只有立绘 / 待机动画 / 表情三样。
    //  素材都是拿各自的 Live2D 模型渲染出来的，统一放在 assets/<Id>/ 下：
    //     <Id>/base.png          常态立绘（兜底）
    //     <Id>/idle/fNN.png      待机循环帧（12fps）
    //     <Id>/face/<名字>.png   表情
    //  ⚠️ 2026-10-11 起 **不再有** assets/seats/char_<Id>.png 和 seats/icon_<Id>.png ——
    //     那 9 张座位立绘 + 4 张小像已按用户要求全部删除（理由见 SeatArt 的注释）。
    //     现在座位走「本体角色=活模型 / 客人=idle 待机动画」，小像走 idle 第 0 帧或 base.png。
    // ══════════════════════════════════════════════════════════════

    /// <summary>一个编外角色。</summary>
    public sealed record Guest(string Id, string Name, string Rgb, int IdleFrames)
    {
        public Color Color => Color.FromHtml(Rgb);
    }

    /// <summary>全部编外角色。**要加新角色就往这儿加一条**（素材放 assets/&lt;Id&gt;/）。</summary>
#if DOUSPIRE_LITE
    /// <summary>简洁版（编译开关 <c>DOUSPIRE_LITE</c>）不带客串角色：
    /// 池子里只剩本体角色，随机分配永远不会落到鲸鱼娘那四位头上，
    /// 也就不会去读没打包的 Live2D/立绘。包体从 145MB 降到 ~40MB。</summary>
    public static readonly Guest[] Guests = System.Array.Empty<Guest>();
#else
    public static readonly Guest[] Guests =
    {
        new("whale", "DS鲸鱼娘", "#5B7FBF", 96),
        new("chun", "椿", "#C0554E", 96),
        new("yuki", "八千代辉夜姬", "#4FBFB0", 96),
        new("sherry", "雪莉", "#4A7BC8", 96),
    };
#endif

    public static Guest? GuestOf(string? id)
    {
        if (id is null) return null;
        foreach (var g in Guests) if (g.Id == id) return g;
        return null;
    }

    /// <summary>
    /// 一席是谁：要么是游戏本体角色（<see cref="Model"/> 非空），
    /// 要么是编外角色（<see cref="GuestId"/> 非空）。两者皆空 = 这一席还没定。
    /// </summary>
    public readonly record struct SeatPick(CharacterModel? Model, string? GuestId)
    {
        public bool IsGuest => GuestId is not null;
        public bool IsEmpty => Model is null && GuestId is null;
    }

    /// <summary>某个编外角色占的那一席。</summary>
    public static SeatPick SeatFor(string guestId) => new(null, guestId);

    /// <summary>
    /// **我当前 run 里正在用的角色**（篝火开桌时用得上）。
    ///
    /// 走 <c>RunManager.State</c> → <c>LocalContext.GetMe</c> → <c>Player.Character</c>。
    /// 拿不到（主菜单 F9 开桌、联机客机、还没开 run）返回 null，调用方退回抽签。
    /// ⚠️ **mod 角色也算** —— 酒狐是 mod 加的，同样在 <c>ModelDb.AllCharacters</c> 里，
    ///    只要 <c>IsPlayable</c> 就能进池子、能上桌。
    /// </summary>
    public static CharacterModel? MyRunCharacter()
    {
        try
        {
            var state = RunManager.Instance?.DebugOnlyGetState();
            if (state is null) return null;
            var me = LocalContext.GetMe(state);
            var c = me?.Character;
            if (c is not null) return c;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"斗地主：读当前 run 的角色失败（改用抽签）: {e.Message}");
        }
        return null;
    }

    // ══════════════════════════════════════════════════════════════

    /// <summary>可选角色（游戏里能玩的那几位）。取不到就返回空表，界面上退化成纯文字。</summary>
    public static IReadOnlyList<CharacterModel> Roster
    {
        get
        {
            if (_roster != null) return _roster;
            try
            {
                _roster = ModelDb.AllCharacters
                    .Where(c => c.IsPlayable)
                    .ToList();
                Entry.Logger.Info($"斗地主：取到 {_roster.Count} 个可选角色"
                                  + $"（{string.Join("、", _roster.Select(Name))}）。");
            }
            catch (System.Exception e)
            {
                Entry.Logger.Warn($"取角色列表失败，桌面上将不显示立绘: {e.Message}");
                _roster = new List<CharacterModel>();
            }
            return _roster;
        }
    }

    public static bool HasRoster => Roster.Count > 0;

    /// <summary>
    /// 真正用来发牌的池子 = 游戏里可玩的角色 + 编外角色（鲸鱼娘）。
    /// 「抽到哪几个角色就放哪几个 UI」走的就是这个池子。
    /// </summary>
    public static IReadOnlyList<SeatPick> Pool
    {
        get
        {
            if (_pool != null) return _pool;
            var list = Roster.Select(c => new SeatPick(c, null)).ToList();
            foreach (var g in Guests) list.Add(SeatFor(g.Id));
            _pool = list;
            Entry.Logger.Info($"斗地主：发牌池 {list.Count} 席"
                              + $"（{string.Join("、", list.Select(Name))}）。");
            return _pool;
        }
    }

    /// <summary>角色中文名（走游戏自带的 characters 本地化表）。</summary>
    public static string Name(CharacterModel? c)
    {
        if (c == null) return "？";
        try
        {
            var t = c.Title.GetFormattedText();
            if (!string.IsNullOrWhiteSpace(t)) return t;
        }
        catch (System.Exception) { /* 本地化没加载就退回内部名 */ }
        try { return c.Id.Entry; } catch (System.Exception) { return "？"; }
    }

    /// <summary>这一席的名字。</summary>
    public static string Name(SeatPick p)
        => p.IsGuest ? (GuestOf(p.GuestId)?.Name ?? "？") : Name(p.Model);

    /// <summary>角色专属配色（游戏自己给每个角色定的）。</summary>
    public static Color ColorOf(CharacterModel? c)
    {
        try { return c?.NameColor ?? Colors.White; }
        catch (System.Exception) { return Colors.White; }
    }

    /// <summary>这一席的名字配色。</summary>
    public static Color ColorOf(SeatPick p)
        => p.IsGuest ? (GuestOf(p.GuestId)?.Color ?? Colors.White) : ColorOf(p.Model);

    /// <summary>角色的内部 id（`ironclad` / `silent` …）。以前用来拼立绘文件名 `seats/char_&lt;entry&gt;.png`；
    /// 那批图 2026-10-11 已删，现在只用于语音包目录和活模型。</summary>
    public static string EntryOf(CharacterModel? c)
    {
        try { return c?.Id.Entry ?? ""; }
        catch (System.Exception) { return ""; }
    }

    private static Dictionary<string, string>? _cardIndex;
    private static Dictionary<string, CardModel>? _cardModels;
    private static Dictionary<string, CardModel>? _cardModelsByClass;

    /// <summary>把名字归一化：只留字母数字、全小写。<c>StrikeIronclad</c> 和 <c>STRIKE_IRONCLAD</c> 都变 <c>strikeironclad</c>。</summary>
    private static string Norm(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var ch in s)
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
        return sb.ToString();
    }

    /// <summary>
    /// 按**游戏内部类名**取卡模型（<c>Models.Cards.StrikeIronclad</c> → "StrikeIronclad"）。
    /// 这是尖塔牌面**唯一可靠**的找法：中文名会被 mod 撞、被本地化改，类型名不会。
    /// </summary>
    public static CardModel? CardModelByClass(string className)
    {
        if (string.IsNullOrEmpty(className)) return null;

        if (_cardModelsByClass is null)
        {
            var map = new Dictionary<string, CardModel>();
            var baseAsm = typeof(ModelDb).Assembly;      // 游戏本体那个程序集
            var mods = 0;
            try
            {
                foreach (var card in ModelDb.AllCards)
                {
                    var n = Norm(card.GetType().Name);
                    if (n.Length == 0) continue;

                    var isBase = card.GetType().Assembly == baseAsm;
                    if (!isBase) mods++;

                    if (map.TryGetValue(n, out var old))
                    {
                        // **同名冲突时永远让游戏本体那张赢** ——
                        // mod 能改卡名改卡面，也能加一张跟本体重名的卡（用户 2026-10-07 提的）。
                        if (!isBase && old.GetType().Assembly == baseAsm) continue;
                    }
                    map[n] = card;
                }
                Entry.Logger.Info($"斗地主：卡表索引（按类名）{map.Count} 条，其中 {mods} 张来自 mod。");
            }
            catch (System.Exception e)
            {
                Entry.Logger.Warn($"斗地主：读游戏卡表失败（按类名）: {e.Message}");
            }
            _cardModelsByClass = map;
        }

        return _cardModelsByClass.TryGetValue(Norm(className), out var hit) ? hit : null;
    }

    /// <summary>
    /// 中文卡名 → 游戏里那张卡的 <see cref="CardModel"/>。
    /// 尖塔牌面要**把游戏自己的卡节点（<c>scenes/cards/card.tscn</c>）喂给它**渲染，
    /// 这样出来的就是百科全书/卡库里那张一模一样的卡，不用我们自己拼。
    /// </summary>
    public static CardModel? CardModelOf(string title)
    {
        if (string.IsNullOrEmpty(title)) return null;

        if (_cardModels is null)
        {
            var map = new Dictionary<string, CardModel>();
            try
            {
                foreach (var card in ModelDb.AllCards)
                {
                    string t;
                    try { t = card.Title; } catch (System.Exception) { continue; }
                    if (string.IsNullOrWhiteSpace(t) || map.ContainsKey(t)) continue;
                    map[t] = card;
                }
            }
            catch (System.Exception e)
            {
                Entry.Logger.Warn($"斗地主：读游戏卡表失败（尖塔牌面拿不到卡模型）: {e.Message}");
            }
            _cardModels = map;
        }

        return _cardModels.TryGetValue(title, out var hit) ? hit : null;
    }

    /// <summary>
    /// 中文卡名 → 卡框类型（attack / skill / power / quest）。
    /// **运行时把游戏卡表扫一遍建索引**，不靠我手写猜 ——
    /// 尖塔牌面要按卡的真实类型挑卡框（攻击红 / 技能绿 / 能力蓝）。
    /// 查不到返回 null，调用方退回内置兜底表。
    /// </summary>
    public static string? CardKindOf(string title)
    {
        if (string.IsNullOrEmpty(title)) return null;

        if (_cardIndex is null)
        {
            var map = new Dictionary<string, string>();
            try
            {
                foreach (var card in ModelDb.AllCards)
                {
                    string t;
                    try { t = card.Title; } catch (System.Exception) { continue; }
                    if (string.IsNullOrWhiteSpace(t) || map.ContainsKey(t)) continue;

                    var raw = card.Type.ToString().ToLowerInvariant();
                    map[t] = raw.Contains("attack") ? "attack"
                           : raw.Contains("power") ? "power"
                           : raw.Contains("quest") ? "quest"
                           : "skill";
                }
                Entry.Logger.Info($"斗地主：卡表索引建好了，{map.Count} 条（尖塔牌面用）。");
            }
            catch (System.Exception e)
            {
                Entry.Logger.Warn($"斗地主：读游戏卡表失败，尖塔牌面退回内置类型表: {e.Message}");
            }
            _cardIndex = map;
        }

        return _cardIndex.TryGetValue(title, out var hit) ? hit : null;
    }

    /// <summary>
    /// 这一席猜拳时用**哪一套手势图**。
    /// 游戏只为那 5 个可玩角色画了手，编外角色没有 —— 那就**一家借一个**，
    /// 别全退回摄政王（用户 2026-10-07 抓的："为什么全是储君的手"）。
    /// </summary>
    public static string HandEntry(SeatPick p)
    {
        if (!p.IsGuest) return EntryOf(p);
        return p.GuestId switch
        {
            "whale" => "ironclad",
            "chun" => "silent",
            "yuki" => "defect",
            "sherry" => "necrobinder",
            _ => "regent",
        };
    }

    /// <summary>
    /// 按内部 id 取那个角色的**官方配色**（尖塔牌面模式的牌框要用）。
    /// 取不到（角色表没加载 / id 写错）就返回兜底色，绝不抛。
    /// </summary>
    public static Color EntryColor(string entry, Color fallback)
    {
        try
        {
            foreach (var c in Roster)
                if (string.Equals(EntryOf(c), entry, System.StringComparison.OrdinalIgnoreCase))
                    return ColorOf(c);
        }
        catch (System.Exception) { }
        return fallback;
    }

    /// <summary>这一席的 id。</summary>
    public static string EntryOf(SeatPick p)
        => p.IsGuest ? (p.GuestId ?? "") : EntryOf(p.Model);

    // ════════════ 本体角色的语音包目录 ════════════
    // 本体四个角色一人一套**斗地主梗**台词（assets/voice/<目录>/），用户自己用 FishAudio 生成。
    // 目录名直接用游戏内部 id，好认：ironclad=铁甲战士、silent=静默猎手（猎宝）、
    // defect=故障机器人（鸡煲）、necrobinder=亡灵缚师（骨妹）。
    // 对不上的（别的 mod 角色）返回 null = 安静，绝不为找不到台词多报一个错。

    private static readonly (string Key, string Dir)[] BaseVoiceDirs =
    {
        ("ironclad", "ironclad"),
        ("silent", "silent"),
        ("defect", "defect"),
        ("necro", "necrobinder"),
    };

    /// <summary>本体角色对应的语音包目录（没有就 null = 这位不说话）。</summary>
    public static string? BaseVoiceDir(SeatPick p)
    {
        if (p.IsGuest) return null;
        var id = EntryOf(p);
        if (string.IsNullOrEmpty(id)) return null;
        id = id.ToLowerInvariant();
        foreach (var (key, dir) in BaseVoiceDirs)
            if (id.Contains(key, System.StringComparison.Ordinal)) return dir;
        return null;
    }

    /// <summary>
    /// 本体角色"坐在桌边"的立绘。
    ///
    /// ★ 2026-10-11 起**正常情况下返回 null** —— `seats/char_&lt;entry&gt;.png` 已删，
    ///   本体角色一律走游戏自己的**活模型**（<see cref="DouSpireActor"/>）。
    ///   这里留着候选链只是为了兼容"外部素材包自己补了图"的情况。
    ///   返回 null 时界面**不是**退回"HUD 头像框"那套 —— 而是活模型直接顶上来；
    ///   活模型也请不到才会看到空位（日志里有 Warn）。
    /// </summary>
    public static Texture2D? SeatArt(CharacterModel? c)
    {
        var entry = EntryOf(c);
        if (string.IsNullOrEmpty(entry)) return null;
        return DouSpireAssets.First(DouSpireAssets.SeatOf(entry));
    }

    /// <summary>
    /// 这一席的座位立绘。
    ///
    /// <para>★★ 2026-10-11：<c>assets/seats/char_*.png</c> 那 9 张静态立绘**已经全部删掉**
    /// ★★（用户："把这些静态图给我删了。我用的是本家和那四个二次元人物。"）——
    /// 因为池子里这 9 位**每一位都有比静态图更好的东西**：</para>
    /// <list type="bullet">
    ///   <item>**本体 5 位**（铁甲战士 / 沉默猎手 / 故障机器人 / 亡灵缚师 / 帝王）→ 游戏自己的
    ///     **活模型**（<see cref="DouSpireActor"/>）。静态图那几张是早先 AI 仿的，跟游戏里真正的
    ///     角色不像，用户一眼就看出来了。</item>
    ///   <item>**四位客人**（DS鲸鱼娘 / 椿 / 八千代辉夜姬 / 雪莉）→ 她们自己的
    ///     **待机动画**（<c>&lt;id&gt;/idle/fNN.png</c>，见 <see cref="IdleClock"/>）。</item>
    /// </list>
    ///
    /// <para>所以这个函数现在**只作兜底**：本体角色返回 null（活模型顶着），
    /// 客人返回待机动画的第 0 帧（活模型/动画都取不到时至少还有个人形）。</para>
    /// </summary>
    public static Texture2D? SeatArt(SeatPick p)
    {
        if (p.IsGuest)
        {
            var id = p.GuestId ?? "";
            return DouSpireAssets.First(DouSpireAssets.SeatOf(id))   // 外部素材包若补了图，仍然优先
                   ?? IdleFrame(id, 0)
                   ?? DouSpireAssets.First($"{id}/base.png");
        }
        return SeatArt(p.Model);
    }

    private static readonly Dictionary<string, Texture2D?> IconCache = new();

    /// <summary>
    /// **这张贴图还活着吗。**
    ///
    /// 本体角色的立绘来自 <c>CharacterModel.IconTexture</c> → <c>PreloadManager.Cache.GetTexture2D(...)</c>，
    /// 也就是**游戏自己的 AssetCache**。换房间/换场景时游戏会打印
    /// <c>Unloading N missed cache assets</c> 并把一批贴图**回收**掉 ——
    /// 那一刻我们静态缓存里攥着的 `CompressedTexture2D` 就变成**已释放对象**了。
    ///
    /// 2026-10-08 实测（`godot.log` 第 3685 行，用户报"进去卡死"）：
    /// <code>
    /// ObjectDisposedException: Cannot access a disposed object.
    /// Object name: 'Godot.CompressedTexture2D'.
    ///   at Godot.TextureRect.SetTexture(Texture2D) → DouSpireTable.FillHud(Int32 seat)
    ///   → RefreshCore → Refresh → BeginShowCards → Deal → RestoreOrPick
    ///   → "DouSpire 桌面渲染出错，已停摆"
    /// </code>
    /// ⇒ 贴图被回收 + 直接 <c>SetTexture</c> = **整张桌子停摆**。
    /// 阴在**第七次开桌才复现**（前面六次都好好的），所以查了很久。
    ///
    /// 所以凡是要用这张图之前都先过一遍这里；判不了（连判都会抛）就按"没有图"处理 ——
    /// **宁可没有头像，也绝不能让桌子停摆。**
    /// </summary>
    public static bool IsAliveTexture(GodotObject? t)
    {
        if (t is null) return false;
        try { return Godot.GodotObject.IsInstanceValid(t); }
        catch (System.Exception) { return false; }
    }

    /// <summary>
    /// 角色立绘。取不到返回 null（界面会退化成只有名字），**绝不抛异常** ——
    /// 拿不到图只是不好看，不该把牌桌搞崩。
    ///
    /// **只缓存"确实还活着"的图**：缓存里那张若已被引擎回收就丢掉重取；
    /// 重取回来还是死的就**这帧先不缓存**（下次调用再试）—— 死图会自己恢复。
    /// </summary>
    public static Texture2D? Icon(CharacterModel? c)
    {
        if (c == null) return null;
        string key;
        try { key = c.Id.Entry; } catch (System.Exception) { return null; }

        if (IconCache.TryGetValue(key, out var cached))
        {
            if (IsAliveTexture(cached)) return cached;
            IconCache.Remove(key);   // 引擎把这张收了 —— 别再用
        }

        Texture2D? tex = null;
        try
        {
            tex = c.IconTexture;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"角色 {key} 的立绘取不到（只影响好看程度）: {e.Message}");
            return null;
        }

        if (!IsAliveTexture(tex)) return null;   // 死的这帧就不给；**刻意不缓存**，下次再试
        IconCache[key] = tex;
        return tex;
    }

    /// <summary>
    /// 选人界面的小像。编外角色没有游戏头像，就用她自己的立绘当小像。
    ///
    /// ★ 2026-10-11：`seats/icon_*.png` 那 4 张也删了（它们本来就是
    ///   `seats/char_*.png` 的**复制品** —— md5 一模一样，等于同一张图存了两份）。
    ///   现在直接落到待机动画第 0 帧 / `base.png`，都是她自己的正经素材。
    /// </summary>
    public static Texture2D? Icon(SeatPick p)
    {
        if (!p.IsGuest) return Icon(p.Model);
        var id = p.GuestId ?? "";
        return DouSpireAssets.First($"seats/icon_{id}.png",
                                    $"seats/char_{id}.png")
               ?? IdleFrame(id, 0)
               ?? DouSpireAssets.First($"{id}/base.png");
    }

    // ══════════════════════════════════════════════════════════════
    //  出牌心情 —— 游戏本体角色自带动作音效，编外角色（鲸鱼娘）没有，
    //  所以按牌型/场面从她自己的表情库里挑一张贴上去。
    //  表情图在 assets/whale/<名字>.png（拿她那套 Live2D 渲染出来的）。
    // ══════════════════════════════════════════════════════════════

    /// <summary>她会为什么事做表情。**14 种牌型逐个分开**，另外把所有场面也都列上。</summary>
    public enum Mood
    {
        /// <summary>常态，无表情。</summary>
        Idle,

        // ── 开局 / 场面 ──
        /// <summary>发牌。</summary>
        Deal,
        /// <summary>我当上地主（加冕）。</summary>
        Landlord,
        /// <summary>轮到我出牌。</summary>
        MyTurn,
        /// <summary>我在挑牌（还没出）。</summary>
        Picking,
        /// <summary>点了「提示」。</summary>
        Hint,

        // ── 出牌：14 种牌型逐个分开 ──
        /// <summary>单张。</summary>
        Single,
        /// <summary>对子。</summary>
        Pair,
        /// <summary>三张。</summary>
        Triple,
        /// <summary>三带一。</summary>
        TripleWithSingle,
        /// <summary>三带二。</summary>
        TripleWithPair,
        /// <summary>顺子。</summary>
        Straight,
        /// <summary>连对。</summary>
        ConsecutivePairs,
        /// <summary>飞机（不带）。</summary>
        Plane,
        /// <summary>飞机带单。</summary>
        PlaneWithSingles,
        /// <summary>飞机带对。</summary>
        PlaneWithPairs,
        /// <summary>四带二（两张单）。</summary>
        FourWithTwoSingles,
        /// <summary>四带两对。</summary>
        FourWithTwoPairs,
        /// <summary>炸弹。</summary>
        Bomb,
        /// <summary>王炸。</summary>
        Rocket,

        // ── 其他场面 ──
        /// <summary>要不起（被对手压住）。</summary>
        Pass,
        /// <summary>压住了别人（对手要不起）。</summary>
        Beat,
        /// <summary>我剩最后一张（报单）。</summary>
        LastOne,
        /// <summary>对家剩最后一张（要防）。</summary>
        RivalLastOne,
        /// <summary>队友剩最后一张（2v2 好消息：他那队要跑了）—— 2026-10-09 加，独立槽位 12_matelast。</summary>
        MateLastOne,
        /// <summary>这一局赢了。</summary>
        Win,
        /// <summary>这一局输了。</summary>
        Lose,
    }

    /// <summary>
    /// 各角色的「心情 → 表情池」。**每次从池里随机挑一张**，所以同一件事不会每次都一个样。
    /// 名字对应 <c>assets/&lt;角色&gt;/face/&lt;名字&gt;.png</c>。
    /// 角色表情不够多时，只挑最能对上的几件事回应，其余返回空表（= 不回应，继续播待机）。
    /// </summary>

    /// <summary>DS鲸鱼娘：44 张，事件铺得最满。</summary>
    private static string[] WhaleFaces(Mood m) => m switch
    {
        // ── 开局 / 场面 ──
        Mood.Deal => new[] { "呆呆眼", "兔兔贴纸", "蝴蝶结贴纸" },
        Mood.Landlord => new[] { "头箍", "墨镜", "双手比耶" },
        Mood.MyTurn => new[] { "感叹号", "脸红" },
        Mood.Picking => new[] { "画笔", "橡皮", "撤回", "手机换色", "单边马尾", "点菜按下", "深色桌布" },
        Mood.Hint => new[] { "圆眼镜", "方眼镜", "椭圆眼镜" },

        // ── 出牌：14 种牌型 ──
        Mood.Single => new[] { "调皮", "吐舌", "脸红" },
        Mood.Pair => new[] { "调皮", "爱心眼", "挤" },
        Mood.Triple => new[] { "开心兴奋", "星星眼", "双手比耶" },
        Mood.TripleWithSingle => new[] { "双手比耶", "情绪花花" },
        Mood.TripleWithPair => new[] { "情绪花花", "喵喵手~喵~动画" },
        Mood.Straight => new[] { "星星眼", "开心兴奋", "喵喵手~喵~动画" },
        Mood.ConsecutivePairs => new[] { "喵喵手~喵~动画", "星星眼" },
        Mood.Plane => new[] { "双手比耶", "情绪花花", "感叹号" },
        Mood.PlaneWithSingles => new[] { "情绪花花", "呆呆眼" },
        Mood.PlaneWithPairs => new[] { "双手比耶", "开心兴奋" },
        Mood.FourWithTwoSingles => new[] { "生气", "心跳" },
        Mood.FourWithTwoPairs => new[] { "生气", "魔爪" },
        Mood.Bomb => new[] { "魔爪", "魔爪换色", "生气", "心跳" },
        Mood.Rocket => new[] { "鲸鱼", "鲸鱼放桌上", "爱心眼", "星星眼" },

        // ── 其他场面 ──
        Mood.Pass => new[] { "流汗", "晕晕", "闭眼口水", "吐魂", "阴暗" },
        Mood.Beat => new[] { "墨镜", "调皮", "感叹号", "猫猫贴纸" },
        Mood.LastOne => new[] { "心跳", "love", "脸红" },
        Mood.RivalLastOne => new[] { "问号", "流汗", "呆呆眼" },
        Mood.Win => new[] { "爱心眼", "蛋包饭", "巴菲", "开心兴奋", "双手比耶" },
        Mood.Lose => new[] { "哭", "悲伤", "吐魂", "阴暗" },

        _ => System.Array.Empty<string>(),
    };

    /// <summary>椿：模型只带 5 个表情（哭 / 爱心眼 / 眼高光消失 / 脸红 / 黑脸），粗着用。</summary>
    private static string[] ChunFaces(Mood m) => m switch
    {
        Mood.Win or Mood.Landlord or Mood.Rocket or Mood.Bomb
            or Mood.FourWithTwoPairs or Mood.FourWithTwoSingles => new[] { "爱心眼", "脸红" },
        Mood.Lose or Mood.Pass or Mood.RivalLastOne or Mood.LastOne => new[] { "眼高光消失", "哭" },
        Mood.Beat => new[] { "爱心眼" },
        _ => new[] { "脸红" },
    };

    /// <summary>八千代辉夜姬：模型只带 4 个表情（泪珠 / 眯眯眼 / 眼泪 / 笑咪咪）。</summary>
    private static string[] YukiFaces(Mood m) => m switch
    {
        Mood.Lose or Mood.Pass or Mood.RivalLastOne => new[] { "眼泪", "泪珠" },
        Mood.Bomb or Mood.Rocket or Mood.FourWithTwoPairs or Mood.FourWithTwoSingles
            => new[] { "眯眯眼" },
        Mood.Win or Mood.Landlord or Mood.Beat or Mood.Rocket => new[] { "笑咪咪" },
        Mood.Hint or Mood.Picking => new[] { "眯眯眼" },
        _ => System.Array.Empty<string>(),
    };

    /// <summary>
    /// 雪莉：模型只带 4 个表情，渲出来看过实物再配的 ——
    /// <c>jushou</c> 举麦克风（表演）、<c>monv</c> **魔女形态**（衣服变黑、背后长出手，反差最大）、
    /// <c>pingguo</c> 举苹果（可爱日常）、<c>taishou</c> 举着东西笑。
    /// </summary>
    private static string[] SherryFaces(Mood m) => m switch
    {
        // 最狠的牌 → 魔女形态
        Mood.Bomb or Mood.Rocket or Mood.FourWithTwoPairs or Mood.FourWithTwoSingles
            => new[] { "monv" },
        // 得意 / 表演 → 举麦克风
        Mood.Win or Mood.Landlord or Mood.Beat or Mood.Plane or Mood.PlaneWithSingles
            or Mood.PlaneWithPairs => new[] { "jushou" },
        // 日常小动作 → 举苹果
        Mood.Single or Mood.Pair or Mood.Hint or Mood.Picking or Mood.MyTurn
            => new[] { "pingguo" },
        // 挨打 / 无奈 → 举着东西笑（就这一个还算合适）
        Mood.Lose or Mood.Pass or Mood.RivalLastOne or Mood.LastOne
            or Mood.Triple or Mood.TripleWithSingle or Mood.TripleWithPair
            or Mood.Straight or Mood.ConsecutivePairs => new[] { "taishou" },
        _ => new[] { "pingguo" },
    };

    /// <summary>取某个编外角色对某件事的表情池；空表 = 这个角色不回应这件事。</summary>
    public static string[] FacesOf(string? guestId, Mood m) => guestId switch
    {
        "chun" => ChunFaces(m),
        "yuki" => YukiFaces(m),
        "sherry" => SherryFaces(m),
        _ => WhaleFaces(m),
    };

    /// <summary>一张牌型对应什么心情 —— 14 种牌型逐个分开，不合并档位。</summary>
    public static Mood MoodOf(DouSpireRules.Combo? combo)
    {
        if (combo is null) return Mood.Idle;
        return combo.Type switch
        {
            DouSpireRules.ComboType.Single => Mood.Single,
            DouSpireRules.ComboType.Pair => Mood.Pair,
            DouSpireRules.ComboType.Triple => Mood.Triple,
            DouSpireRules.ComboType.TripleWithSingle => Mood.TripleWithSingle,
            DouSpireRules.ComboType.TripleWithPair => Mood.TripleWithPair,
            DouSpireRules.ComboType.Straight => Mood.Straight,
            DouSpireRules.ComboType.ConsecutivePairs => Mood.ConsecutivePairs,
            DouSpireRules.ComboType.Plane => Mood.Plane,
            DouSpireRules.ComboType.PlaneWithSingles => Mood.PlaneWithSingles,
            DouSpireRules.ComboType.PlaneWithPairs => Mood.PlaneWithPairs,
            DouSpireRules.ComboType.FourWithTwoSingles => Mood.FourWithTwoSingles,
            DouSpireRules.ComboType.FourWithTwoPairs => Mood.FourWithTwoPairs,
            DouSpireRules.ComboType.Bomb => Mood.Bomb,
            DouSpireRules.ComboType.Rocket => Mood.Rocket,
            _ => Mood.Single,
        };
    }

    /// <summary>某个编外角色某张表情图的贴图。</summary>
    public static Texture2D? Face(string? guestId, string label)
        => string.IsNullOrEmpty(guestId) || string.IsNullOrEmpty(label)
            ? null
            : DouSpireAssets.First($"{guestId}/face/{label}.png");

    /// <summary>
    /// 「会动的表情」的第 <paramref name="f"/> 帧 —— 表情是固定的，但呼吸/摆头/眼珠还在动，
    /// 所以切表情时不再是硬邦邦一张死图。素材在 <c>assets/&lt;id&gt;/faceanim/&lt;表情&gt;/fNN.png</c>。
    /// **只有常用的那批表情做了动画版**，没做的返回 null → 调用方退回静态表情图。
    /// </summary>
    public static Texture2D? FaceAnim(string? guestId, string label, int f)
    {
        if (string.IsNullOrEmpty(guestId) || string.IsNullOrEmpty(label)) return null;
        // 12 帧一轮
        f = ((f % FaceAnimFrames) + FaceAnimFrames) % FaceAnimFrames;
        return DouSpireAssets.First($"{guestId}/faceanim/{label}/f{f:00}.png");
    }

    /// <summary>「会动的表情」有几帧。</summary>
    public const int FaceAnimFrames = 12;

    /// <summary>这个角色的这个表情有没有动画版（没有就退回静态图）。</summary>
    public static bool HasFaceAnim(string? guestId, string label)
        => FaceAnim(guestId, label, 0) is not null;

    /// <summary>待机动画每秒播多少帧。</summary>
    public const double IdleFps = 12.0;

    /// <summary>
    /// 某个编外角色待机动画的第 <paramref name="f"/> 帧（`assets/&lt;id&gt;/idle/fNN.png`）。
    /// 游戏本体角色自带骨骼动画，编外角色没有，所以就靠这个"一直动"。
    /// 帧数按角色的 <see cref="Guest.IdleFrames"/> 取模，不同角色可以不一样。
    /// </summary>
    public static Texture2D? IdleFrame(string? guestId, int f)
    {
        if (string.IsNullOrEmpty(guestId)) return null;
        var n = GuestOf(guestId)?.IdleFrames ?? 48;
        f = ((f % n) + n) % n;
        return DouSpireAssets.First($"{guestId}/idle/f{f:00}.png");
    }

    /// <summary>
    /// **编外角色待机动画的帧时钟** —— 三张桌子（斗地主 / 德州 / 21 点）共用同一套算法。
    ///
    /// <para>各自持有一个实例，在 <c>_Process</c> 里 <see cref="Tick"/> 一下就能拿到当前帧号；
    /// 帧号丢给 <see cref="IdleFrame"/> 就是那张要贴的图。</para>
    ///
    /// <para>★ 2026-10-11 抽出来：以前只有斗地主会播待机动画（`TickIdleAnimation`），
    /// 德州和 21 点直接贴一张静态立绘 —— 玩家在德州/21 点看到的四位客人**是死的**，
    /// 在斗地主里却在动。用户："为什么预览是这些静态图。把这些静态图给我删了。
    /// 我用的是本家和那四个二次元人物。" 现在三张桌子统一走这个时钟。</para>
    ///
    /// <para>★ **掉帧按实际时间跳格**（不是每帧 +1）—— 否则帧率一低动画就变慢。</para>
    /// </summary>
    public sealed class IdleClock
    {
        private double _acc;
        private int _frame;

        /// <summary>当前帧号（单调递增，取图时自己取模）。</summary>
        public int Frame => _frame;

        /// <summary>推进 <paramref name="delta"/> 秒，返回推进后的帧号。</summary>
        public int Tick(double delta)
        {
            var step = 1.0 / IdleFps;
            _acc += delta;
            if (_acc < step) return _frame;
            var advance = (int)(_acc / step);
            _acc -= advance * step;
            _frame += advance;
            return _frame;
        }
    }

    /// <summary>
    /// 按种子给各席分配角色：<paramref name="mine"/> 是我选的那位，
    /// 其余各席从剩下的里挑，尽量不重复。<paramref name="seatCount"/> = 3（经典）或 4（2v2）。
    /// </summary>
    public static SeatPick[] Assign(SeatPick mine, int seed, int seatCount = 3)
    {
        // ⚠️ 永远返回 **4 格**（多出的席保持 IsEmpty）：调用方（DouSpireTable._chars）把它整块
        //    存进一个声明为 [4] 的字段 —— 这里若按 seatCount 返回 3 格，切四人桌时 _chars[3]
        //    直接越界（2026-10-09 用户实测：选人界面点「四人桌」整个界面卡死）。
        var result = new SeatPick[4];
        var pool = Pool.ToList();
        if (pool.Count == 0) return result;

        var rng = new System.Random(seed);
        var mineIdx = mine.IsEmpty ? -1 : IndexIn(pool, mine);
        if (mineIdx < 0) mineIdx = rng.Next(pool.Count);

        result[0] = pool[mineIdx];
        var rest = pool.Where((_, i) => i != mineIdx).OrderBy(_ => rng.Next()).ToList();

        // 座位 1 起依次分（4 人时：下家、对家、上家）
        for (var s = 1; s < seatCount; s++)
            result[s] = rest.Count >= s ? rest[s - 1] : pool[mineIdx];
        return result;
    }

    /// <summary>
    /// 某个**游戏角色**在发牌池里的下标 —— 联机同步用（各端池子一样，所以下标可以互认）。
    /// 找不到（编外角色 / 不在池里）返回 -1。
    /// </summary>
    public static int PoolIndex(CharacterModel? c)
    {
        if (c is null) return -1;
        var pool = Pool;
        for (var i = 0; i < pool.Count; i++)
            if (ReferenceEquals(pool[i].Model, c)) return i;
        return -1;
    }

    /// <summary>按下标取池子里的一席（越界返回"还没定"）。联机同步用。</summary>
    public static SeatPick PickAt(int index)
    {
        var pool = Pool;
        return index >= 0 && index < pool.Count ? pool[index] : default;
    }

    /// <summary>把角色往前/往后拨一位（选人界面用）。</summary>
    public static SeatPick Cycle(SeatPick current, int delta)
    {
        var pool = Pool;
        if (pool.Count == 0) return current;
        var idx = current.IsEmpty ? -1 : IndexIn(pool, current);
        if (idx < 0) idx = 0;
        var next = ((idx + delta) % pool.Count + pool.Count) % pool.Count;
        return pool[next];
    }

    /// <summary>
    /// 按"引用相等"找位置 —— 用 <c>List.IndexOf</c> 会走 record 生成的
    /// <c>Equals</c>，万一游戏角色重写了相等比较就会串位，所以这里显式比。
    /// </summary>
    /// <summary>
    /// 这个座位用的角色在**池子里的下标** —— 联机选角广播用的就是它。
    ///
    /// 为什么发下标不发角色名：两端池子内容一样（同一个 build），下标互认，
    /// 而且比字符串省字节、也不会因为中/日文名编码出岔子。找不到返回 -1。
    /// </summary>
    public static int PoolIndexOf(SeatPick pick) => pick.IsEmpty ? -1 : IndexIn(Pool, pick);

    private static int IndexIn(IReadOnlyList<SeatPick> pool, SeatPick pick)
    {
        for (var i = 0; i < pool.Count; i++)
            if (pool[i].GuestId == pick.GuestId && ReferenceEquals(pool[i].Model, pick.Model))
                return i;
        return -1;
    }
}
