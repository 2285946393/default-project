using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using QuestSpire.Quests;
using QuestSpire.UI;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace QuestSpire.Relics;

// 任务册：整局存活的任务追踪遗物。把战斗/全局事件转成任务进度，达标发现金；
// 进入商店时弹出“接取任务”面板。
[RegisterRelic(typeof(QuestRelicPool))]
public sealed class QuestBookRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Common;

    // 图标：纯 dll 的 mod 往游戏图集里加不了自己的贴图（那要 pck），
    // 而**没有图标**的话游戏会回退成 locked_model.png（那个锁头），
    // 连标题和说明都会变成「锁定」文案 —— 我们写好的效果说明就被盖掉了（实测确认）。
    // 所以先借一件主题接近的本体遗物图标顶上，等有原画再换。
    //   DUSTY_TOME 尘封古籍 → 任务册
    public override RelicAssetProfile AssetProfile => ContentAssetProfiles.Relic("dusty_tome");

    /// <summary>
    /// **这一本任务册自己的进度**（不再走静态单例）。
    /// 本机会模拟所有玩家的遗物 → 每个客户端都会推进所有玩家的任务，算出来一样、存下去也一样。
    /// </summary>
    private readonly QuestRunState _state = new();

    /// <summary>给 Entry / UI 用：这本任务册的进度。</summary>
    public QuestRunState State => _state;

    // —— 存档桥 ——
    // 游戏自带机制：打了 [SavedProperty] 的属性会被 RelicModel.ToSerializable() 写进本局存档，
    // 读档时由 RelicModel.FromSerializable() -> SavedProperties.Fill() 灌回来（走 setter）。
    // 所以这一条属性就是任务进度的存档入口，读写都转发给 QuestRunner。
    [SavedProperty]
    public int[] QuestData
    {
        get => _state.Export();
        // 每本任务册读自己的存档 —— 所有玩家的都要读（主机那份存档里就有别人的数据）。
        set => _state.Import(value);
    }

    /// <summary>
    /// **还没发到玩家手上的任务奖励**（存的是任务种类）。
    ///
    /// ⚠️⚠️ 为什么要存这个（2026-09-26 玩家反馈「快速 SL 奖励就会消失」）：
    /// 奖励队列（`_batch` / `_pendingRewards` / `_pendingRelics`）都是**纯内存**的普通 List，
    /// 存档里没有它们。而任务进度（`QuestData`）是存了的。
    /// 于是：战斗中完成任务 → 奖励排队 → 玩家这时候快速存档/读档
    ///      → 队列没了，但任务还是「已完成」 → **奖励就永久消失了**。
    /// 修法：发奖励**之前**先把任务种类记到这个数组里（它进存档），
    /// 真正发出去之后才把它移掉；读档时如果发现里面有东西，就重新排队、进下一个房间补发。
    ///
    /// 存 int[] 而不是 List&lt;int&gt;：`[SavedProperty]` 走的是游戏的序列化器，数组最稳。
    /// </summary>
    [SavedProperty]
    public int[] PendingRewardKinds
    {
        get => _dueKinds.Select(k => (int)k).ToArray();
        set
        {
            _dueKinds.Clear();
            if (value == null) return;
            foreach (var raw in value) _dueKinds.Add((QuestKind)raw);
            Entry.Logger.Info($"读档：发现 {_dueKinds.Count} 项任务奖励还没发出去，等进下一个房间补发。");
        }
    }

    /// <summary>「奖励还没发出去」的任务种类（内存里的那份，存档那份见 PendingRewardKinds）。</summary>
    private readonly List<QuestKind> _dueKinds = new();

    /// <summary>
    /// 本册**第几次抽奖励内容**（「展望未来」的 X 费牌三选一、各类随机卡牌奖励）。
    /// ⚠️ 联机：奖励列表必须各端一致，所以种子不能用 `Random.Shared`，要靠这个序号推。
    ///    奖励流程现在各端都会跑（见 `Reward`），所以这个计数在各端是同步递增的。
    /// </summary>
    private int _giftSeq;

    /// <summary>
    /// **已经发过奖励**的任务种类（进存档）。
    /// 和 `_dueKinds` 一起构成"对账"的两端：`任务已完成` − `已发过` = `还欠着`。
    /// 有了它，进房间时就能自动发现"完成了却没发"的任务并补发（见 `ReconcileOwedRewards`），
    /// 不用再依赖某个触发点一定会被执行到。
    /// </summary>
    [SavedProperty]
    public int[] PaidRewardKinds
    {
        get => _paidKinds.Select(k => (int)k).ToArray();
        set
        {
            _paidKinds.Clear();
            if (value == null) return;
            foreach (var raw in value) _paidKinds.Add((QuestKind)raw);
        }
    }

    private readonly HashSet<QuestKind> _paidKinds = new();

    /// <summary>发奖励之前先记账：这个任务的奖励还欠着（存档会一起存）。</summary>
    private void MarkRewardDue(QuestKind kind)
    {
        if (_dueKinds.Contains(kind)) return;
        _dueKinds.Add(kind);
    }

    /// <summary>奖励真的交到玩家手上了：销账。销掉的是哪一项已经无从对应（队列里可能混着几条），
    /// 所以调用方在「这一批全部交出去」之后调，把已经排进去的那部分移掉。</summary>
    private void ClearDue(QuestKind kind) => _dueKinds.Remove(kind);

    /// <summary>
    /// 把「欠着的奖励」（`_dueKinds`，已进存档）重新排进待发队列。进房间 / 读档后调。
    /// 只要账上还欠着就会重排，所以失败重试、中途读档都能兜住。
    /// </summary>
    private void RescheduleRestoredRewards()
    {
        if (_dueKinds.Count == 0) return;
        try
        {
            if (CombatManager.Instance.IsInProgress) return;   // 战斗中先不动，下个房间再说

            // ⚠️⚠️ 关键：**重建之前先把旧的待发队列丢掉**。
            // 2026-09-27 玩家实测「奖励发重叠了」：一个任务走了两条路 ——
            //   ① 奖励早就进了 `_batch`（`_dueKinds` 也记着账）；
            //   ② 进房间时对账把这笔账重建成一个新的奖励对象，应用了一次；
            //   ③ 紧接着 `FlushPendingRewards` 又把 `_batch` 里**原来那份**发了一遍 → 重复。
            // 一份奖励只能走一条路：既然按账重建了，旧的队列就作废。
            if (_batch.Count > 0)
            {
                Entry.Logger.Warn($"补发：丢掉待发队列里重复的 {_batch.Count} 项（改由对账重建，避免发两次）。");
                _batch.Clear();
            }

            int n = 0;
            foreach (var kind in _dueKinds.ToList())
            {
                var def = QuestCatalog.Find(kind);
                if (def == null) { ClearDue(kind); continue; }
                var state = new QuestState(def);
                state.ForceComplete();
                _pendingRewards.Enqueue(state);
                n++;
            }
            if (n > 0) Entry.Logger.Info($"补发：{n} 项欠着的任务奖励进了待发队列。");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Error($"补发奖励失败: {e}");
        }
    }

    // —— 多人局守卫 ——
    // 本机模拟着所有玩家的遗物（包括队友的那本任务册），但 QuestRunner 是**静态单例**、只服务本地玩家。
    // 所以只有「本地玩家的那本」才允许改状态；不然 2~4 本任务册会往同一份进度里叠。
    // 判定：本机网络服务的 NetId == 这本任务册主人的 NetId（找不到网络信息就当单人）。
    private Player? _isLocalOwnerFor;      // 上次算的时候 Owner 是谁（Owner 换了就重算）
    private bool _isLocalOwnerCached;

    private bool IsLocalOwner
    {
        get
        {
            var owner = Owner;
            if (owner == null) return false; // 还没挂上玩家，先别乱写状态
            if (ReferenceEquals(_isLocalOwnerFor, owner)) return _isLocalOwnerCached;

            bool local;
            try
            {
                // ⚠️ 用 `LocalContext.NetId`（游戏自己的口径 —— `RunManager.Launch()` 里就是写的它），
                //    不要用 `RunManager.Instance.NetService?.NetId`：后者在**没连上/断线时会抛异常**，
                //    而老代码的 catch 把它吞成「是本人」→ 每台机器都会以为别人的册子也是自己的
                //    → 同一份奖励被各端各发一遍。现在拿不到就**当非本人**（宁可不发，也不要重复发）。
                var localId = MegaCrit.Sts2.Core.Context.LocalContext.NetId;
                local = !localId.HasValue || owner.NetId == localId.Value;
            }
            catch
            {
                local = false;
            }

            // 缓存要跟着 Owner 走：遗物实例可能被跨局复用，光缓存一个 bool 会读到上一局的结论。
            _isLocalOwnerFor = owner;
            _isLocalOwnerCached = local;
            return local;
        }
    }

    // 每回合 / 每战斗的临时追踪。
    private int _turnCards;
    private bool _sawFirstTurn;
    private bool _dmgThisCombat;
    private readonly HashSet<RoomType> _roomTypes = new();

    // 2026-09-22 新增任务用的临时状态。
    private int _combatTurnIndex;                        // 本场战斗已经开始了几个玩家回合
    private bool _isEliteCombat;                         // 本场是不是精英战
    private bool _nonCommonPlayedThisCombat;             // 本场是否打过罕见/稀有牌
    private readonly HashSet<int> _costsThisTurn = new();   // 本回合打出过的能量费
    private readonly HashSet<Type> _orbsThisTurn = new();   // 本回合激发过的充能球种类
    private bool _inShop;                                // 上一个房间是不是商店
    private bool _shopSpent;                             // 这次商店里花过钱没
    private bool _boatBlockUsedThisCombat;                // 船的「格挡被清空」本场是否已触发

    // 2026-09-23 批（梗任务）用的临时状态。
    private bool _skillPlayedThisCombat;                  // 本场有没有打过技能牌（我说别带）
    private bool _bloodiedThisCombat;                     // 本场有没有先被打到半血以下（战个未来）
    private int _selfDamageThisCombat;                    // 本场自己一共挨了多少（战士鸽）
    private int _goldAtShopEntry;                         // 进商店那一刻身上有多少钱（基米精神）

    // 2026-09-23 批之六（乐子）用的临时状态。
    private int _cardsThisCombat;                         // 本场一共打了几张牌（咕咕嘎嘎：要 0 张）
    private int _exhaustThisCombat;                       // 本场消耗了几张牌（枯枝还魂：单场 10 张以上）
    private bool _oneCostOkThisCombat = true;             // 本场是否只打 1 费牌（文明6：精打细算）
    private int _raresThisCombat;                          // 本场打出的稀有牌（暗黑破坏神）
    private int _commonsThisCombat;                        // 本场打出的普通牌（原神）
    private int _uncommonsThisCombat;                      // 本场打出的罕见牌（原神）
    private int _powersThisCombat;                         // 本场打出的能力牌（明日方舟）
    private int _killsThisTurn;                           // 本回合杀了几个（三杀）
    private int _blockBrokenThisCombat;                   // 本场格挡被打穿几次（破防）
    private int _orbsThisCombat;                          // 本场激发几个充能球（雷霆大机煲）
    private int _attacksThisTurnRaw;                      // 本回合打出几张攻击牌（杂耍：每 3 张触发一次）
    private int _flawlessStreak;                          // 连续无伤场数（农种）
    private int _blockThisCombat;                         // 本场战斗的格挡总量（龟壳）
    private readonly HashSet<string> _cardEntriesThisCombat = new(); // 本场打出过的**不同**牌（你知道是谁：只用 2 种）
    private readonly Dictionary<string, int> _cardPlayCountThisTurn = new(); // 本回合每张牌打了几次（复读机）

    // 2026-10-01 批之二用的临时状态。
    private int _blockThisTurn;          // 本回合获得的格挡（我说叠甲有没有懂的）
    private int _skillsThisTurn;         // 本回合打出的技能牌数（花拳绣腿）

    // 2026-10-02 批之九（梗任务用）。
    private int _skillsThisRun;             // 本局打出的技能牌总数（电子木鱼）
    private bool _attackPlayedThisCombat;   // 本场打过攻击牌吗（退退退：打了这场就作废）
    private bool _powerPlayedThisCombat;    // 本场打过能力牌吗（裸装）
    private bool _playedEarlyThisCombat;    // 本场前 2 回合出过牌吗（摸鱼）

    // 2026-10-03 批之十（速度 / 牌组极致用）。
    private int _discardsThisCombat;        // 本场战斗弃掉几张牌（羊关通关：单场 25 张）

    // 2026-09-22 深夜二批：一代遗物任务用的计数。
    // 这三个必须进存档：读档回来要是归零，「今晚不睡」会被误判成满足条件（_everRested 变回 false）。
    [SavedProperty]
    public bool EverRested { get; set; }

    [SavedProperty]
    public int Smiths { get; set; }

    /// 三和大神用的：本局在篝火休息了几次。必须进存档，否则读档后重数会把「打没打铁」判错。
    [SavedProperty]
    public int RestsForSanhe { get; set; }

    [SavedProperty]
    public int Treasures { get; set; }

    // 鸡煲三部曲用的临时状态。
    private int _earlyTurnCards;                            // 本场前 3 个回合一共打了几张牌
    private readonly HashSet<string> _cardEntriesThisTurn = new(); // 本回合打出过的牌的 Id（判「回响形态」）

    // 本局开局时的生命上限（判「原装出厂」用）。跟 QuestData 一样随遗物进存档。
    [SavedProperty]
    public int BaselineMaxHp { get; set; }

    // 「随身物」两个开关也随存档走（读写都转发给 QuestBoons，见那个文件里的说明）。
    [SavedProperty]
    public bool HasBoat { get; set; }

    [SavedProperty]
    public bool HasDeadBranch { get; set; }

    [SavedProperty]
    public bool HasTorii { get; set; }

    [SavedProperty]
    public bool HasDripper { get; set; }

    [SavedProperty]
    public bool HasOrnithopter { get; set; }

    // ==========================================================================
    // F11 修复（2026-10-08）：上面那四个「兜底随身物」**不再被写入**，改成**派生**。
    //
    // 原来：遗物发放抛异常时执行 `fallback()` → `HasXxx = true`。
    //   问题在于「有没有抛异常」是**只有本人那台知道**的事实：
    //     · 本人那台抛了 → `HasXxx = true`；
    //     · 别人那台 `OfferRelic` 根本不真发（直接 `return true`）→ 永不抛 → 一直 false。
    //   而 `HasXxx` 是 `[SavedProperty]`，会经 `SerializableRelic.Props` 进**校验和** →
    //   各端数值分叉（枯枝会凭空往手牌塞牌、鸟居会多给格挡）→ 判定数据不同步 → 客机被踢回主菜单。
    //
    // 现在：catch 里**一个存档属性都不写**，读取时现算 ——
    //   「这条任务的奖励已经发过了」 且 「本人手上没有对应的真遗物」 → 点亮兜底效果。
    //   · 战斗时各端的遗物早已同步完毕（`SyncLocalObtainedRelic`），所以这个条件**各端算出来一致**；
    //   · 真遗物发成功时它天然是 false → **不会和真遗物的效果叠加**（这是照搬原条件会踩的坑）；
    //   · 旧存档里已经写进去的 `HasXxx` 不再参与判定 —— 同一条任务在旧存档里也是「奖励已发过」，
    //     派生条件照样成立，行为不倒退。
    //
    // 之所以不用「新加一条网络消息广播发放结果」：那要新建 `INetMessage` 层，
    // 而这里「本人有没有这件遗物」本来就是个**各端都看得到的共享事实**，派生一下就够了。
    // ==========================================================================

    /// <summary>本人手上有没有这件（我们自研的）真遗物。拿不到就当没有 —— 宁可点亮兜底，也不要静默失效。</summary>
    private bool HasOwnRelic<T>() where T : RelicModel
    {
        try { return Owner != null && Owner.GetRelic<T>() != null; }
        catch { return false; }
    }

    /// <summary>
    /// 兜底随身物的**统一条件**：配置开着 + 这条任务的奖励确实发过 + 本人没有对应的真遗物。
    /// 三个条件在各端都成立/都不成立（都是共享事实），所以不会造成数值分叉。
    /// </summary>
    private bool BoonFallbackActive(QuestKind kind, bool hasOwnRelic)
        => QuestSpireConfig.BoonsActive && !hasOwnRelic && _rewardedKinds.Contains(kind);

    /// <summary>仿制枯木树枝（消耗一张牌 → 随机一张牌进手牌）。</summary>
    private bool DeadBranchBoon
        => BoonFallbackActive(QuestKind.CardsExhaustedBig, HasOwnRelic<QuestDeadBranchRelic>());

    /// <summary>仿制鸟居（战斗开始 +8 格挡）。</summary>
    private bool ToriiBoon
        => BoonFallbackActive(QuestKind.BlockGainedBig, HasOwnRelic<QuestToriiRelic>());

    /// <summary>仿制咖啡滤杯（每场第一个回合 +1 能量）。</summary>
    private bool DripperBoon
        => BoonFallbackActive(QuestKind.SmithNoRest, HasOwnRelic<QuestDripperRelic>());

    /// <summary>仿制玩具扑翼飞机（每次用药水回 5 血）。</summary>
    private bool OrnithopterBoon
        => BoonFallbackActive(QuestKind.PotionsUsedBig, HasOwnRelic<QuestOrnithopterRelic>());

    [SavedProperty]
    public bool HasReckless { get; set; }

    [SavedProperty]
    public bool HasScholar { get; set; }

    [SavedProperty]
    public bool HasHaggler { get; set; }

    [SavedProperty]
    public bool HasZen { get; set; }

    [SavedProperty]
    public bool HasSteady { get; set; }

    [SavedProperty]
    public bool HasScripture { get; set; }

    [SavedProperty]
    public bool HasAnthony { get; set; }

    // ⚠️ 下面这四个以前**漏了 [SavedProperty]**（等于没进存档，读档就丢）——
    // 这次改成实例字段时补上的，顺手修了那个旧 bug。
    [SavedProperty]
    public bool HasSurge { get; set; }

    [SavedProperty]
    public bool HasBulwark { get; set; }

    [SavedProperty]
    public bool HasCombo { get; set; }

    [SavedProperty]
    public bool HasIndifference { get; set; }

    // 盛碗虫常数（2026-10-01）：获得格挡时额外 +1 —— 社区梗「15 挡不住 16」。
    [SavedProperty]
    public bool HasBowlbugConstant { get; set; }

    // 「一个都不放过」改版后的两个半条件：第一幕的宝箱房、第一幕的精英战。
    // 必须进存档：读档回来若归零，而宝箱房早就开过了，这任务就永远做不完。
    [SavedProperty]
    public bool ActOneChestOpened { get; set; }

    [SavedProperty]
    public bool ActOneEliteWon { get; set; }

    // —— 造成伤害 / 获得格挡 ——
    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer == null || dealer != Owner.Creature) return;
        await Grant(QuestKind.DamageDealt, result.TotalDamage);
        // 数值轮椅（梗）：单回合伤害，每回合开始清零（见 AfterPlayerTurnStart）
        await Grant(QuestKind.DamageInOneTurn, result.TotalDamage);
        // 观者？！（梗）：观者级爆发。单回合 300 点，每回合清零。
        await Grant(QuestKind.WatcherBurst, result.TotalDamage);
        // 一拳超人（乐子）：只看**单次**打出的最大伤害，所以用快照（进度条会显示你的最高一击）
        await Set(QuestKind.OnePunch, result.TotalDamage);

        // 火焰屏障输出特别高（梗）：这玩意的伤害**不是牌打的**。
        // 反编译确认：牌本身只给格挡 + 挂一层 FlameBarrierPower，真正弹回去的伤害来自那层能力，
        // 标记是 ValueProp.Unpowered、且没有 cardSource —— 只按牌名去抓是永远抓不到的。
        // 判定：自己造成 + 没有牌来源 + Unpowered 类伤害 + 身上正挂着火焰屏障。
        if (cardSource == null
            && (props & ValueProp.Unpowered) != 0
            && (props & ValueProp.Move) == 0
            && Owner.Creature.HasPower<FlameBarrierPower>())
            await Grant(QuestKind.FlameBarrierDamage, result.TotalDamage);
    }

    public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != Owner.Creature) return Task.CompletedTask;
        _ = Grant(QuestKind.BlockGained, (int)amount);
        _ = Grant(QuestKind.BlockGainedBig, (int)amount); // 「小伤不理」→ 鸟居
        _ = Set(QuestKind.IronShirt, (int)amount);        // 「铁布衫」（乐子）：单次最大格挡
        // 龟壳（流派）：单场战斗里的格挡总量（进战斗房间时清零）
        _blockThisCombat += (int)amount;
        _ = Grant(QuestKind.TurtleBlock, (int)amount);
        _ = Set(QuestKind.RetreatBlock, _blockThisCombat); // 退退退：单场格挡总量
        // 我说叠甲有没有懂的：本回合格挡（进回合时清零）
        _blockThisTurn += (int)amount;
        _ = Set(QuestKind.BlockInOneTurn, _blockThisTurn);
        return Task.CompletedTask;
    }

    // 破防（乐子）：本场格挡被打穿的次数。
    public override Task AfterBlockBroken(PlayerChoiceContext choiceContext, Creature target, Creature? breaker)
    {
        if (target != Owner.Creature || !IsLocalOwner) return Task.CompletedTask;
        _blockBrokenThisCombat++;
        return Grant(QuestKind.GuardBroken, 1);
    }

    // 三杀（乐子）：本回合击杀数。只数敌人（自己人死了不算）。
    public override Task AfterDeath(
        PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature == null) return Task.CompletedTask;
        // 打赢假商人：我们把它直接召进了普通战斗（本体事件很难遇到），所以按"打死它"来判定。
        if (creature.Monster is FakeMerchantMonster) _ = Grant(QuestKind.FakeMerchantWin, 1);
        if (!IsLocalOwner) return Task.CompletedTask;
        if (!creature.IsPrimaryEnemy && !creature.IsSecondaryEnemy) return Task.CompletedTask;
        _killsThisTurn++;
        return Grant(QuestKind.TripleKill, 1);
    }

    // 骨头人（亡灵绑定者）：用「灾厄」击杀敌人。
    // 灾厄 = DoomPower，是**直接 `CreatureCmd.Kill`**（反编译确认）、不走伤害结算 ——
    // 所以本体里「自己零伤害也能赢」靠的就是它（联机里靠队友也算）。专门用 AfterDiedToDoom 这个钩子做。
    public override Task AfterDiedToDoom(PlayerChoiceContext choiceContext, IReadOnlyList<Creature> creatures)
        => creatures != null && creatures.Count > 0
            ? Grant(QuestKind.DoomKills, creatures.Count)
            : Task.CompletedTask;

    // 噶人焖（乐子）：摄政王的辉星。梗来自主播「菜农来辣」（农神）的口头禅。
    public override Task AfterStarsGained(int amount, Player gainer)
        => gainer == Owner ? Grant(QuestKind.GaRenMen, amount) : Task.CompletedTask;

    // 我说弃牌有没有懂的（梗）：弃牌计数（静默猎手的看家本领）。
    // 羊关通关（批之十）：顺手记本场弃了多少张（进战斗房间时清零）。
    public override Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (card == null || card.Owner != Owner) return Task.CompletedTask;
        _discardsThisCombat++;
        return Grant(QuestKind.CardsDiscarded, 1);
    }

    // 骨小妹（梗）：召唤计数。Osty 是亡灵绑定者那只小骷髅（反编译确认 `Monsters.Osty`）。
    public override Task AfterSummon(PlayerChoiceContext choiceContext, Player summoner, decimal amount)
        => summoner == Owner ? Grant(QuestKind.OstySummons, 1) : Task.CompletedTask;

    // ==========================================================================
    // 2026-10-01 批之五：用**没碰过的钩子**做的几条（玩家：「自己找点乐子」）。
    // ==========================================================================

    // 轮回：弃牌堆洗回抽牌堆（`AfterFlush`）。抽牌堆空了才会洗，一局能洗好几次。
    public override Task AfterFlush(PlayerChoiceContext choiceContext, Player player,
        IReadOnlyCollection<CardModel> flushedCards, IReadOnlyCollection<CardModel> retainedCards)
        => player == Owner ? Grant(QuestKind.FlushCount, 1) : Task.CompletedTask;

    // 我说丢药有没有懂的：丢弃药水（`AfterPotionDiscarded`，没 player 参数 → 药水只有自己的）。
    public override Task AfterPotionDiscarded(PotionModel potion)
        => Grant(QuestKind.PotionDiscarded, 1);

    // 骨小妹·不死：Osty 复活（`AfterOstyRevived`）。Osty 是亡灵绑定者那只小骷髅。
    public override Task AfterOstyRevived(Creature osty)
        => Grant(QuestKind.OstyRevive, 1);

    // —— 批之六：接着挖没用过的钩子 ——

    // 牌堆搬运工：牌在牌堆之间移动（`AfterCardChangedPiles`）。抽牌 / 弃牌 / 消耗 / 洗牌都会触发。
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
        => card != null && card.Owner == Owner ? Grant(QuestKind.PileMoved, 1) : Task.CompletedTask;

    // 我说给药有没有懂的：获得药水（`AfterPotionProcured`）。之前只有"用了几瓶""丢了几瓶"。
    public override Task AfterPotionProcured(PotionModel potion)
        => Grant(QuestKind.PotionProcured, 1);

    // 我说重放有没有懂的：牌的**打出次数被改写**（`AfterModifyingCardPlayCount`，重放/回响那类）。
    public override Task AfterModifyingCardPlayCount(CardModel card)
        => card != null && card.Owner == Owner ? Grant(QuestKind.CardReplay, 1) : Task.CompletedTask;

    // —— 批之七 ——

    // 一步之遥：走到第三幕（`AfterActEntered` 没参数 → 看 RunState.CurrentActIndex；一幕=0）。
    public override Task AfterActEntered()
    {
        if ((Owner.RunState?.CurrentActIndex ?? 0) >= 2) _ = Grant(QuestKind.ActThree, 1);
        return Task.CompletedTask;
    }

    // 永固：阻止格挡被清空（`AfterPreventingBlockClear`，比如壁垒 / 卡尺 / 模糊）。
    public override Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
        => creature == Owner.Creature ? Grant(QuestKind.BlockClearPrevented, 1) : Task.CompletedTask;

    // 皮糙肉厚：累计**挨打的次数**（`BeforeDamageReceived`）。之前只算"挨了多少血"。
    public override Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
        => target == Owner.Creature ? Grant(QuestKind.DamageTakenCount, 1) : Task.CompletedTask;

    // 我说别塞有没有懂的：阻止卡牌进入牌组（`AfterAddToDeckPrevented`，比如御守挡诅咒）。
    public override Task AfterAddToDeckPrevented(CardModel card)
        => Grant(QuestKind.DeckAddPrevented, 1);

    /// <summary>
    /// **Harmony 补丁调进来的**（见 `Patches/CardAddedToDeckPatch`）：牌被加进了**整局卡组**。
    /// 本体没有这个正向钩子，所以只能补丁；按牌的类型分别计数（一代 #7「加 3 攻 / 3 技 / 3 能」那批）。
    /// </summary>
    public void NotifyCardAddedToDeck(CardModel card)
    {
        if (card == null || card.Owner != Owner) return;
        switch (card.Type)
        {
            case CardType.Attack: _ = Grant(QuestKind.CardsAddedAttack, 1); break;
            case CardType.Skill: _ = Grant(QuestKind.CardsAddedSkill, 1); break;
            case CardType.Power: _ = Grant(QuestKind.CardsAddedPower, 1); break;
        }
    }

    /// <summary>
    /// **Harmony 补丁调进来的**（见 `Patches/CardRemovedFromDeckPatch`）：一张牌被**从整局卡组里删掉**。
    /// 「清仓大甩卖」（批之十）用它数"本局一共删了几张牌"——
    /// 商店花钱删、事件删、卡牌效果删，全都算，不挑来源。
    /// </summary>
    public void NotifyCardRemovedFromDeck(CardModel card)
    {
        if (card == null || card.Owner != Owner) return;
        _ = Grant(QuestKind.PurgeMaster, 1);
    }

    // —— 出牌：总览 + 分类 + 单回合攻击数 ——
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player != Owner) return Task.CompletedTask;
        _ = Grant(QuestKind.CardsPlayed, 1);
        _ = Grant(QuestKind.CardsPlayedBig, 1); // 「熟能生巧」→ 升级牌
        _ = Grant(QuestKind.SilentCardsInTurn, 1); // 「劣人手速」（单回合，进回合时清零）
        _ = Grant(QuestKind.CardsInOneTurnBig, 1); // 「无限」（单回合，进回合时清零）
        _turnCards++;
        _cardsThisCombat++;                        // 「咕咕嘎嘎」要看本场是不是 0 张
        _cardEntriesThisCombat.Add(cardPlay.Card.Id.Entry); // 「你知道是谁」看本场用了几种牌

        var card = cardPlay.Card;

        // 复读机（乐子）：本回合同一张牌打到第 3 次
        _cardPlayCountThisTurn.TryGetValue(card.Id.Entry, out int playedTimes);
        _cardPlayCountThisTurn[card.Id.Entry] = playedTimes + 1;
        if (playedTimes + 1 == 3) _ = Grant(QuestKind.Repeater, 1);
        _ = Set(QuestKind.SameCardManyTimes, playedTimes + 1); // 堆叠宝石：本回合同一张牌的最多打出次数
        if (card.Type == CardType.Attack)
        {
            _ = Grant(QuestKind.AttacksPlayed, 1);
            _ = Grant(QuestKind.AttacksPlayedBig, 1); // 「连环拳」
            _ = Grant(QuestKind.AttacksInOneTurn, 1);
            _attackPlayedThisCombat = true;           // 「退退退」：本场打过攻击牌就作废

            // 杂耍（梗）：本回合第 3 张攻击牌会被 JugglingPower 复制一张到手牌。
            // 只有身上真挂着那层能力才算数（反编译确认它每回合第 3 张触发）。
            _attacksThisTurnRaw++;
            if (_attacksThisTurnRaw % 3 == 0
                && Owner.Creature != null
                && Owner.Creature.HasPower<JugglingPower>())
                _ = Grant(QuestKind.JugglingTricks, 1);
        }
        else if (card.Type == CardType.Power)
        {
            _ = Grant(QuestKind.PowersPlayed, 1);
            _ = Grant(QuestKind.PowersPlayedBig, 1); // 「厚积薄发」
            _powerPlayedThisCombat = true;            // 「裸装」：本场打过能力牌就作废
        }
        else if (card.Type == CardType.Skill)
        {
            // 我说别带：本场一旦打出技能牌，这场战斗就不算数了（目标是「一张都别带」）
            _skillPlayedThisCombat = true;
            // 花拳绣腿：本回合技能牌数（进回合时清零）
            _skillsThisTurn++;
            _ = Set(QuestKind.SkillsInOneTurn, _skillsThisTurn);
            // 电子木鱼：本局技能牌总数（不清零）
            _skillsThisRun++;
            _ = Set(QuestKind.MokugyoSkills, _skillsThisRun);
        }

        if (card.Rarity == CardRarity.Rare) _ = Grant(QuestKind.RareCardsPlayed, 1);

        int cost = card.EnergyCost.Canonical;
        if (cost <= 0) _ = Grant(QuestKind.FreeCardsPlayed, 1);
        else if (cost >= 2) _ = Grant(QuestKind.HeavyCardsPlayed, 1);

        // 三费齐鸣：本回合 1 / 2 / 3 费各打出一张就算一次。
        if (cost >= 1 && cost <= 3)
        {
            _costsThisTurn.Add(cost);
            if (_costsThisTurn.Contains(1) && _costsThisTurn.Contains(2) && _costsThisTurn.Contains(3))
            {
                _costsThisTurn.Clear();
                _ = Grant(QuestKind.ComboCosts, 1);
            }
        }

        // 朴实无华：本场只要打过罕见/稀有（含远古）牌，这场就不算了。
        if (card.Rarity is CardRarity.Uncommon or CardRarity.Rare or CardRarity.Ancient)
            _nonCommonPlayedThisCombat = true;

        // 爪爪爪
        if (card is Claw) _ = Grant(QuestKind.ClawPlays, 1);

        // 宇！宙！冷！漠！（摄政王的牌，社区当歌唱）
        // 宇！宙！冷！漠！（2026-09-25 改）：不再数"打出几张"（打 3 张太容易），
        // 改成**牌组里有 3 张以上**「宇宙冷漠」——见 CheckDeckQuests()。
        // 按稀有度 / 类型计数（暗黑破坏神、原神、明日方舟）
        if (card.Owner == Owner)
        {
            switch (card.Rarity)
            {
                case CardRarity.Common: _commonsThisCombat++; break;
                case CardRarity.Uncommon: _uncommonsThisCombat++; break;
                case CardRarity.Rare: _raresThisCombat++; break;
            }
            if (card.Type == CardType.Power) _powersThisCombat++;
        }

        // 文明6：精打细算 —— 一场战斗里只要打出过非 1 费牌就作废
        if (card.Owner == Owner && card.EnergyCost.Canonical != 1) _oneCostOkThisCombat = false;
        // 宝可梦：进化 —— 打出「打击 / 防御」这类**起始牌**累计计数（用牌的内部 id 判断，最准）
        if (card.Owner == Owner)
        {
            var cid = card.Id.ToString() ?? "";
            if (cid.Contains("STRIKE_") || cid.Contains("DEFEND_"))
            {
                _ = Grant(QuestKind.PokemonEvolve, 1);
                _ = Grant(QuestKind.WorkerCards, 1); // 打工人：起始牌累计
            }
        }


        // 蛇咬（梗）：社区吵「它是不是全游戏最烂的卡」。反编译确认它是真牌：
        // 2 费技能 / 普通 / 给 7 层中毒 / 带「保留」。
        if (card is Snakebite) _ = Grant(QuestKind.SnakebitePlays, 1);

        // 肘击（梗）：铁甲战士的起手牌「痛击」（Bash）。反编译确认有 `Cards.Bash`。
        if (card is Bash) _ = Grant(QuestKind.BashPlays, 1);


        // —— 鸡煲梗 ——
        // 还在启动：统计前 3 个回合打了几张牌（_combatTurnIndex 从 1 开始）
        if (_combatTurnIndex >= 1 && _combatTurnIndex <= 3) _earlyTurnCards++;
        // 摸鱼：本场前 2 回合出过牌就作废
        if (_combatTurnIndex >= 1 && _combatTurnIndex <= 2) _playedEarlyThisCombat = true;
        // 回响形态：本回合同一张牌打第二次（_combatTurnIndex 还是上一回合的值也算在战斗内）
        if (_combatTurnIndex >= 1 && !_cardEntriesThisTurn.Add(card.Id.Entry))
            _ = Grant(QuestKind.EchoFormTurn, 1);

        return Task.CompletedTask;
    }

    // —— 充能球：单回合激发 4 种不同的球 ——
    public override Task AfterOrbEvoked(PlayerChoiceContext choiceContext, OrbModel orb, IEnumerable<Creature> targets)
    {
        if (orb == null) return Task.CompletedTask;
        // 只算自己激发的球（联机里队友激发球也会通知到本机，不判就会"统一计算"）
        if (orb.Owner != Owner) return Task.CompletedTask;
        _orbsThisTurn.Add(orb.GetType());
        if (_orbsThisCombat < 99) _orbsThisCombat++;
        _ = Set(QuestKind.ThunderDefect, _orbsThisCombat); // 雷霆大机煲（乐子）：单场激发数
        if (_orbsThisTurn.Count >= 4)
        {
            _orbsThisTurn.Clear();
            return Grant(QuestKind.OrbsInOneTurn, 1);
        }
        return Task.CompletedTask;
    }

    // 通电（梗）：生成充能球就计数 —— 「别人在过牌输出叠甲，鸡煲在通电」。
    public override Task AfterOrbChanneled(PlayerChoiceContext choiceContext, Player player, OrbModel orb)
        => player == Owner ? Grant(QuestKind.OrbsChanneled, 1) : Task.CompletedTask;

    // 复制防御（梗）：战斗内生成牌就计数。
    // 反编译确认：「复制一张牌」那类效果的写法是 selection.CreateClone() +
    // CardPileCmd.AddGeneratedCardToCombat(...)（见 DualWield），所以这条路能覆盖到复制出来的牌。
    // ⚠️ 这是**单场战斗**计数（进战斗房间时清零），得在同一场里攒够 5 张。
    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
        => creator == Owner ? Grant(QuestKind.CardsGeneratedInCombat, 1) : Task.CompletedTask;

    // 我说有费有没有懂的（梗）：单回合花掉多少能量。
    // ⚠️ 这个钩子**没有 Player 参数**，只能从花的哪张牌反推是谁花的（多人局要靠 card.Owner 挡一下）。
    public override async Task AfterEnergySpent(CardModel card, int amount)
    {
        if (card == null || card.Owner != Owner) return;
        await Grant(QuestKind.EnergySpentInOneTurn, amount);  // 单回合花掉多少
        await Grant(QuestKind.EnergyTotal, amount);           // 本局累计花掉多少（能量黑洞）
    }

    // —— 蛇花圣经 / 掉集中：buff 层数变化 ——
    // ⚠️ 这里必须用 Before 版：AfterPowerAmountChanged 的签名里**没有 target**，
    // 而「给敌人叠减益」正是要靠 target 才能分清是叠给谁的。
    public override Task BeforePowerAmountChanged(
        PowerModel power, decimal amount, Creature target, Creature? applier, CardModel? cardSource)
    {
        if (power == null) return Task.CompletedTask;

        // 蛇花圣经（梗）：自己给**敌人**叠的减益，按层数累计（PowerType 是反编译确认存在的字段）
        if (applier == Owner.Creature
            && target != Owner.Creature
            && amount > 0
            && power.Type == PowerType.Debuff)
            _ = Grant(QuestKind.DebuffLayersGiven, (int)amount);

        // 掉集中（梗）：偏差认知每回合掉集中，社区玩「玩家说话越来越短」
        if (target == Owner.Creature && amount < 0 && power is FocusPower)
            _ = Grant(QuestKind.FocusLost, (int)(-amount));

        // —— 流派线（批之十）——
        // 只看「加」不看「减」，层数按实际变化的量累计。buff 类型都反编译核实过。
        if (amount > 0)
        {
            if (target == Owner.Creature)
            {
                if (power is StrengthPower) _ = Grant(QuestKind.StrengthGained, (int)amount);
                if (power is DexterityPower) _ = Grant(QuestKind.DexterityGained, (int)amount);
            }
            else
            {
                // 注意：中毒和易伤都算「给敌人的减益」，但这里分别记账，任务才分得开
                if (power is PoisonPower) _ = Grant(QuestKind.PoisonGiven, (int)amount);
                if (power is VulnerablePower) _ = Grant(QuestKind.VulnerableGiven, (int)amount);
            }
        }

        return Task.CompletedTask;
    }

    // 「蛇花圣经」随身物：你给敌人施加的减益 ×1.5。
    // 这是**乘法**旋钮，和「厚积薄发」的加法旋钮是两个独立的东西（先加后乘），同时拿到不会打架。
    public override decimal ModifyPowerAmountGivenMultiplicative(
        PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
    {
        if (!QuestSpireConfig.BoonsActive || !HasScripture) return 1m;
        if (power == null || giver != Owner.Creature) return 1m;
        if (power.Type != PowerType.Debuff) return 1m;
        return 1.5m;
    }

    // 手不释卷：抽牌计数（card.Owner 就是抽到牌的人）
    // 顺手喂「我说过牌有没有懂的」——那条是**单回合**计数，每回合开始清零（见 AfterPlayerTurnStart）。
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner != Owner) return;
        await Grant(QuestKind.CardsDrawn, 1);
        await Grant(QuestKind.CardsDrawnInOneTurn, 1);

        // 满手牌（乐子）：手牌上限是 10（CardPile.MaxCardsInHand），抽满就算
        var hand = CardPile.Get(PileType.Hand, Owner);
        if (hand != null) await Set(QuestKind.FullHand, hand.Cards.Count);
    }

    public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Owner != Owner) return Task.CompletedTask;
        _ = Grant(QuestKind.CardsExhausted, 1);
        // 枯枝还魂（2026-09-25 改）：从"本局消耗 15 张"改成"**单场战斗**消耗 10 张以上"，
        // 所以这里只累加本场计数，然后把它当快照写进去（回合开始/战斗开始时清零）。
        _exhaustThisCombat++;
        _state.ResetProgress(QuestKind.CardsExhaustedBig);   // 单场计数：先清零再写成本场张数（别显示成历史最大值）
        _ = Set(QuestKind.CardsExhaustedBig, _exhaustThisCombat);
        _ = Set(QuestKind.WoWEnchant, _exhaustThisCombat);   // 魔兽世界：附魔
        // 枯枝（自研复刻）：每消耗一张牌，随机一张牌加入手牌
        // ⚠️ 条件走 `DeadBranchBoon`（派生）而不是读存档属性 `HasDeadBranch` —— 见属性区那段说明。
        if (DeadBranchBoon) _ = GiveBranchCard();
        return Task.CompletedTask;
    }

    // 铁壁：受伤计数（「硬抗到底」是 100 的自伤，这个是更大的目标）
    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner.Creature) return Task.CompletedTask;
        // ⚠️ 玩家反馈：「被格挡的伤害不算」导致 600 点自伤根本做不到（满血才 75）。
        // 改成按**这一次挨打的总伤害**算（含被格挡掉的），这样坦克流也能推进。
        _ = Grant(QuestKind.SelfDamage, result.TotalDamage);
        return Grant(QuestKind.SelfDamageBig, result.TotalDamage);
    }

    // —— 修好的帆船 + 几件仿制遗物：「随身物」，不是真遗物（原因见 QuestBoons.cs）——
    public override async Task BeforeCombatStart()
    {
        // 帆船：每场战斗开始 +15 格挡
        if (QuestSpireConfig.BoonsActive && HasBoat)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner.Creature, 15m, ValueProp.Unpowered, null);
        }
        // 仿制鸟居：战斗开始 +8 格挡
        if (ToriiBoon)
        {
            Flash();
            await CreatureCmd.GainBlock(Owner.Creature, 8m, ValueProp.Unpowered, null);
        }
    }

    // ==========================================================================
    // 「血之契约」：接下之后，**每场战斗敌人开场随机带一个增益**（变难）。
    // 写法照抄本体《弹珠袋》(BagOfMarbles)：在 `BeforeSideTurnStart` 里给 `HittableEnemies` 挂能力。
    // 这个钩子**带 PlayerChoiceContext**，是 `PowerCmd.Apply` 唯一好用的时机
    // （`BeforeCombatStart` 没有 context —— 就是 §4 里「齿轮工艺品」那个时序坑）。
    //
    // ⚠️ 多人局**不生效**（靠 BoonsActive 闸）：给敌人挂能力会改战斗数值，各端必须一致，
    //    否则又是「数据不同步」。单人局才开。
    // ⚠️ 随机用 **ActFloor 当种子**（不是 Random.Shared）：同一次战斗各端算出来一样。
    // ==========================================================================
    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!QuestSpireConfig.BoonsActive) return;
        if (Owner?.Creature == null || !participants.Contains(Owner.Creature)) return;
        if (Owner.PlayerCombatState == null || Owner.PlayerCombatState.TurnNumber > 1) return;
        int seed = unchecked((int)(Owner.NetId ^ (uint)(Owner.RunState?.ActFloor ?? 0)) ^ 0x5F3759DF);

        // —— 盛碗虫泛滥：额外召一只盛碗虫（社区梗「盛碗虫常数」）——
        // `CreatureCmd.Add<T>(combatState)` 就是往战斗里加怪的口子（反编译确认）。
        if (_state.Active.Any(a => a.Def.Kind == QuestKind.BowlbugSwarm && !a.Completed))
        {
            try
            {
                int which = new System.Random(seed ^ 0x27D4EB2F).Next(4);
                Flash();
                switch (which)
                {
                    case 0: await CreatureCmd.Add<BowlbugEgg>(combatState); break;
                    case 1: await CreatureCmd.Add<BowlbugNectar>(combatState); break;
                    case 2: await CreatureCmd.Add<BowlbugSilk>(combatState); break;
                    default: await CreatureCmd.Add<BowlbugRock>(combatState); break;
                }
                Entry.Logger.Info($"盛碗虫泛滥：本场额外召了一只盛碗虫（index={which}）");
            }
            catch (System.Exception e) { Entry.Logger.Warn($"盛碗虫召不出来（不影响战斗）: {e.Message}"); }
        }

        // —— 怪潮：额外多 2 只普通怪（一代 #55 Reality Twist 的轻量版）——
        if (_state.Active.Any(a => a.Def.Kind == QuestKind.MonsterRush && !a.Completed))
        {
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    int mw = new System.Random(seed + 101 + i * 17).Next(6);
                    switch (mw)
                    {
                        case 0: await CreatureCmd.Add<LeafSlimeM>(combatState); break;
                        case 1: await CreatureCmd.Add<Flyconid>(combatState); break;
                        case 2: await CreatureCmd.Add<Inklet>(combatState); break;
                        case 3: await CreatureCmd.Add<GlobeHead>(combatState); break;
                        case 4: await CreatureCmd.Add<FuzzyWurmCrawler>(combatState); break;
                        default: await CreatureCmd.Add<CorpseSlug>(combatState); break;
                    }
                }
                Flash();
                Entry.Logger.Info("怪潮：本场额外召了 2 只怪");
            }
            catch (System.Exception e) { Entry.Logger.Warn($"怪潮召怪失败（不影响战斗）: {e.Message}"); }
        }

        // —— 精英加餐 / 大改造：**每一场战斗都额外加一只精英（每一幕都加）**——
        // 玩家明确要求：大改造会把整副牌换成「飞溅 / 发现」，够强、扛得住（所以**不做幕数限制**）。
        // 精英**按当前幕选**（`ActModel.AllEliteEncounters`）：一幕就出一幕的精英，跨幕不会过强。
        if (_state.Active.Any(a => (a.Def.Kind == QuestKind.EliteAmbush || a.Def.Kind == QuestKind.DeckOverhaul) && !a.Completed))
        {
            try
            {
                var elite = PickActElite(seed);
                if (elite != null)
                {
                    await CreatureCmd.Add(elite.ToMutable(), combatState);
                    Entry.Logger.Info($"精英加餐 / 大改造：本场额外召了一只本幕精英（{elite.Id.Entry}）");
                }
                else
                {
                    // 取不到本幕精英池就退回固定池。
                    // ⛔ 这里只放**验证过安全**的精英（见 IsUnsafeElite 的说明）：
                    //    BygoneEffigy / InfestedPrism 会把战斗卡死，**绝不能**加回来。
                    int ew = new System.Random(seed + 777).Next(2);
                    switch (ew)
                    {
                        case 0: await CreatureCmd.Add<Byrdonis>(combatState); break;
                        default: await CreatureCmd.Add<Entomancer>(combatState); break;
                    }
                    Entry.Logger.Info("精英加餐 / 大改造：本场额外召了一只精英（固定池兜底）");
                }
                Flash();
            }
            catch (System.Exception e) { Entry.Logger.Warn($"精英召不出来（不影响战斗）: {e.Message}"); }
        }

        // —— 打赢假商人：接下来**必定**遇到（本体里它是二幕事件、很难碰到，还得靠药水）——
        if (_state.Active.Any(a => a.Def.Kind == QuestKind.FakeMerchantWin && !a.Completed))
        {
            try
            {
                Flash();
                await CreatureCmd.Add<FakeMerchantMonster>(combatState);
                Entry.Logger.Info("假商人：本场必定出现。");
            }
            catch (System.Exception e) { Entry.Logger.Warn($"假商人召不出来（不影响战斗）: {e.Message}"); }
        }

        // —— 血之契约：全队敌人随机增益 ——
        if (!_state.Active.Any(a => a.Def.Kind == QuestKind.ContractChallenge && !a.Completed)) return;

        var enemies = combatState.HittableEnemies;
        if (enemies == null || enemies.Count == 0) return;

        int pick = new System.Random(seed).Next(6);
        Flash();
        try
        {
            switch (pick)
            {
                case 0: await PowerCmd.Apply<StrengthPower>(choiceContext, enemies, 2m, Owner.Creature, null); break;
                case 1: await PowerCmd.Apply<ThornsPower>(choiceContext, enemies, 2m, Owner.Creature, null); break;
                case 2: await PowerCmd.Apply<DexterityPower>(choiceContext, enemies, 2m, Owner.Creature, null); break;
                case 3: await PowerCmd.Apply<ArtifactPower>(choiceContext, enemies, 1m, Owner.Creature, null); break;
                case 4: await PowerCmd.Apply<BufferPower>(choiceContext, enemies, 1m, Owner.Creature, null); break;
                default: await PowerCmd.Apply<RitualPower>(choiceContext, enemies, 1m, Owner.Creature, null); break;
            }
            Entry.Logger.Info($"血之契约：本场敌人开场随机增益 index={pick}");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"血之契约挂增益失败（不影响战斗）: {e.Message}");
        }
    }

    // 船的第二个效果：本场战斗中第一次格挡被清空时，再 +20 格挡。
    public override async Task AfterBlockCleared(Creature creature)
    {
        if (!QuestSpireConfig.BoonsActive || !HasBoat || _boatBlockUsedThisCombat) return;
        if (creature != Owner.Creature) return;
        _boatBlockUsedThisCombat = true;
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, 20m, ValueProp.Unpowered, null);
    }

    // ==========================================================================
    // 「随身物」改写的游戏旋钮。
    // 这套写法是拆《海克斯符文》学来的：效果不用一个个硬写，
    // 先去 AbstractModel 上找对应的旋钮 —— 25 个 Should*（否决）+ 29 个 Modify*（改写）。
    // 下面每个随身物对着**不同的**旋钮，所以它们的手感天然不一样。
    // ==========================================================================

    // 莽夫之道（代价换强度）：收益 = 你打出的攻击牌伤害 +50%
    // 收紧到「有牌来源 + 明确是打出去的那张牌 + 自己造成 + 是攻击」，避免误伤别的东西。
    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!QuestSpireConfig.BoonsActive || !HasReckless) return 1m;
        if (cardSource == null || cardPlay == null) return 1m;
        if (!props.IsPoweredAttack()) return 1m;
        if (dealer != Owner.Creature) return 1m;
        if (cardSource.Owner != Owner) return 1m;
        return 1.5m;
    }

    // 莽夫之道：代价 = **你的技能牌不再提供格挡**。
    //
    // ⚠️ 这里别写成「你获得的一切格挡都归零」。第一版就是那么写的，结果把
    // 锚 / 鸟居 / 帆船这些遗物给的格挡、以及药水给的格挡也一起清零了 ——
    // 玩家会觉得「这遗物怎么管这么宽」。加了 cardSource 判断之后就只影响技能牌。
    public override decimal ModifyBlockMultiplicative(
        Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!QuestSpireConfig.BoonsActive || !HasReckless) return 1m;
        if (target != Owner.Creature) return 1m;
        if (cardSource == null) return 1m;                       // 遗物 / 药水 / 能力给的格挡不受影响
        if (cardSource.Type != CardType.Skill) return 1m;        // 只针对技能牌
        if (cardSource.Owner != Owner) return 1m;
        // 玩家反馈：原来直接归零太狠了（+50% 伤害换不到防守）→ 改成减半
        return 0.5m;
    }

    // 手不释卷：每回合多抽 1 张牌
    public override decimal ModifyHandDraw(Player player, decimal count)
        => QuestSpireConfig.BoonsActive && HasScholar && player == Owner ? count + 1m : count;

    // 砍价高手：商店价格 -30%
    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)
        => QuestSpireConfig.BoonsActive && HasHaggler && player == Owner ? cost * 0.7m : cost;

    // 养生：篝火休息的回复量 +50%
    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
        => QuestSpireConfig.BoonsActive && HasZen && creature == Owner.Creature ? amount * 1.5m : amount;

    // 稳如老狗：能量上限 +1
    public override decimal ModifyMaxEnergy(Player player, decimal amount)
        => QuestSpireConfig.BoonsActive && HasSteady && player == Owner ? amount + 1m : amount;

    // 厚积薄发：你施加的增益与减益都 +1 层（只加自己给出去的，不加别人给你的）
    //
    // ⚠️⚠️ 这里返回的是「**要加上去的量**」，不是「加完后的总量」！基类返回 0m。
    // 游戏里的用法是：`num += item.ModifyPowerAmountGivenAdditive(power, giver, num, ...)`。
    // 早期版本错写成 `amount + 1m`，于是变成 num += num + 1 = **2×num + 1** —— 玩家实测就是
    // 「怪物和自己的 buff/debuff 翻倍」（2026-09-23 玩家报的 bug，已修）。
    public override decimal ModifyPowerAmountGivenAdditive(
        PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
        => QuestSpireConfig.BoonsActive && HasSurge && giver == Owner.Creature ? 1m : 0m;

    // 铁壁：你单次受到的伤害不超过 15 点
    public override decimal ModifyDamageCap(
        Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
        => QuestSpireConfig.BoonsActive && HasBulwark && target == Owner.Creature ? 15m : decimal.MaxValue;

    // 连环拳：你的攻击牌多打一段
    public override int ModifyAttackHitCount(AttackCommand attack, int hitCount)
        => QuestSpireConfig.BoonsActive && HasCombo ? hitCount + 1 : hitCount;

    // 冷漠：每当你获得格挡，额外获得 3 点格挡
    // （钩子返回的是「加多少」，基类返回 0m —— 不用它的场合必须原样返回 0）
    public override decimal ModifyBlockAdditive(
        Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!QuestSpireConfig.BoonsActive || target != Owner.Creature) return 0m;
        decimal add = 0m;
        if (HasIndifference) add += 3m;      // 冷漠：获得格挡 +3
        if (HasBowlbugConstant) add += 1m;   // 盛碗虫常数：再 +1（把 15 补成 16）
        return add;
    }

    // 东尼意思（梗）：卡牌奖励更容易出现升级版。
    // ⚠️ 这个钩子返回的是**新的概率值**（基类原样返回 odds），不是倍率对象，所以自己乘完要夹在 1 以内。
    public override decimal ModifyCardRewardUpgradeOdds(Player player, CardModel card, decimal odds)
        => QuestSpireConfig.BoonsActive && HasAnthony && player == Owner ? Math.Min(odds * 3m, 1m) : odds;

    // —— 回合开始：结算“整回合未出牌”，并清零单回合计数 ——
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        // （每本任务册各自清零，不再需要「只让本地那本动」这条判断）

        // 仿制咖啡滤杯：每场战斗的第一个回合开始 +1 能量（_combatTurnIndex 在下面才自增）
        if (_combatTurnIndex == 0 && DripperBoon)
            await PlayerCmd.GainEnergy(1m, Owner);

        // 注意：这时候 _turnCards 还是「上一回合」的计数。
        if (_sawFirstTurn && _turnCards == 0)
        {
            _ = Grant(QuestKind.IdleTurns, 1);
            // 礼让一手：精英战的第一回合一张牌都没出
            if (_isEliteCombat && _combatTurnIndex == 1) _ = Grant(QuestKind.EliteFirstTurnIdle, 1);
        }

        _turnCards = 0;
        _sawFirstTurn = true;
        _combatTurnIndex++;
        _costsThisTurn.Clear();
        _orbsThisTurn.Clear();
        _cardEntriesThisTurn.Clear(); // 回响形态是「单回合内」
        _killsThisTurn = 0;           // 三杀：本回合重新数
        _attacksThisTurnRaw = 0;      // 杂耍：本回合的攻击牌重新数
        _cardPlayCountThisTurn.Clear(); // 复读机：本回合重新数
        _blockThisTurn = 0;             // 我说叠甲有没有懂的：本回合重新数
        _skillsThisTurn = 0;            // 花拳绣腿：本回合重新数
        QuestRunner.ResetProgress(QuestKind.AttacksInOneTurn);
        _state.ResetProgress(QuestKind.SameCardManyTimes); // 堆叠宝石：单回合条件，进回合清零
        QuestRunner.ResetProgress(QuestKind.CardsDrawnInOneTurn); // 我说过牌有没有懂的：也是单回合计数
        QuestRunner.ResetProgress(QuestKind.DamageInOneTurn);     // 数值轮椅
        QuestRunner.ResetProgress(QuestKind.SilentCardsInTurn);   // 劣人手速
        QuestRunner.ResetProgress(QuestKind.CardsInOneTurnBig);   // 无限
        QuestRunner.ResetProgress(QuestKind.EnergySpentInOneTurn); // 我说有费有没有懂的
        QuestRunner.ResetProgress(QuestKind.WatcherBurst);         // 观者？！（单回合伤害）
        await Grant(QuestKind.TurnsSurvived, 1);
    }

    public override Task AfterHandEmptied(PlayerChoiceContext choiceContext, Player player)
        => player == Owner ? Grant(QuestKind.EmptyHands, 1) : Task.CompletedTask;

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        _ = Grant(QuestKind.PotionsUsed, 1);
        _ = Grant(QuestKind.PotionsUsedBig, 1); // 「药罐子」→ 玩具扑翼飞机

        // 仿制玩具扑翼飞机：每次使用药水回 5 血（对齐一代原版）
        if (OrnithopterBoon)
        {
            Flash();
            await CreatureCmd.Heal(Owner.Creature, 5m);
        }
    }

    // —— 自身掉血：累计受伤 + 标记“本场掉过血” ——
    // 只有战斗中的掉血才算破坏“毫发无伤”，否则事件房掉的血会串到下一场战斗上。
    public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner.Creature && delta < 0)
        {
            if (CombatManager.Instance.IsInProgress)
            {
                _dmgThisCombat = true;
                _selfDamageThisCombat += (int)(-delta); // 战士鸽：本场挨了多少
                // 战个未来（梗）：本场被打到半血以下，先亏；后面还得赢才算（见 AfterCombatVictory）。
                var self = Owner.Creature;
                if (self != null && self.MaxHp > 0 && self.CurrentHp * 2 <= self.MaxHp)
                    _bloodiedThisCombat = true;
            }
            return Grant(QuestKind.SelfDamage, (int)(-delta));
        }
        return Task.CompletedTask;
    }

    // —— 商店 / 篝火 ——
    public override Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
    {
        if (player != Owner) return Task.CompletedTask;
        _shopSpent = true; // 只看不买任务用
        CheckBoatRelics(); // 船的三件零件可能是在商店买的
        _ = Grant(QuestKind.GoldSpent, goldSpent); // 砍价高手：累计花掉多少
        // 152 金币（梗）：一口气花掉一大笔。AfterItemPurchased 直接给了「这次花了多少」，不用自己算。
        if (goldSpent >= 150) _ = Grant(QuestKind.BigPurchase, 1);
        // 删内切（梗）：把「花钱删牌」单独记一笔。
        // 反编译发现删牌是独立的 MerchantCardRemovalEntry，而且**它也会触发 AfterItemPurchased**
        // （别的商店商品走基类那一份，删牌走它自己重写的那份）——所以这条能精确认出来。
        if (itemPurchased is MerchantCardRemovalEntry) _ = Grant(QuestKind.CardRemovals, 1);
        return Grant(QuestKind.ShopPurchases, 1);
    }

    public override Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        if (player != Owner) return Task.CompletedTask;
        EverRested = true; // 「今晚不睡」要的是「一次都没休息过」
        _ = Grant(QuestKind.RestSites, 1);
        // 三和大神（叙事）：休息满 3 次，而且一次铁都没打过
        RestsForSanhe++;
        if (RestsForSanhe >= 3 && Smiths == 0) _ = Grant(QuestKind.SanheGod, 1);
        return Grant(QuestKind.RestSitesBig, 1); // 「养生」
    }

    public override Task AfterRestSiteSmith(Player player)
    {
        if (player != Owner) return Task.CompletedTask;
        Smiths++;
        // 「今晚不睡」：升级够 5 张，而且这局一次都没在篝火休息过
        if (Smiths >= 4 && !EverRested) _ = Set(QuestKind.SmithNoRest, 1);
        return Grant(QuestKind.CampfireSmiths, 1);
    }

    // —— 金币快照 ——
    public override Task AfterGoldGained(Player player)
    {
        if (player != Owner) return Task.CompletedTask;
        _ = Set(QuestKind.GoldHoard, Owner.Gold);
        return Set(QuestKind.GoldHoardBig, Owner.Gold); // 「见钱眼开」→ 金偶像
    }

    // —— 战斗结束 / 胜利 ——
    public override Task AfterCombatVictory(CombatRoom room)
    {
        // ⚠️⚠️ 2026-09-28 结构改动（照《海克斯符文》）：**把任务奖励直接塞进游戏的战后「搜刮」单子**。
        //
        // 前面十个版本（0.2.12~0.2.21）我们一直在自己弹一个框（`RewardsCmd.OfferCustom`），
        // 于是"两个框抢位置"派生出无穷症状：盖掉 / 重复 / 拿不到 / 得上一个房间才发。
        // 海克斯符文用的是 `CombatRoom.AddExtraReward`，奖励**直接显示在搜刮界面里**——
        // 只有一个框，这整类毛病在构造上就不存在。
        // 它在 `AfterCombatVictory` 里做（`HextechMayhemModifier` → `AddRandomForgeReward` → `AddExtraReward`），
        // 我们照抄同一个时机。**别再改回自己弹框（`OfferCustom`）那条路。**
        //
        // 兜底：万一这个时机塞不进去（`_batch` 没被清空），后面的 `BeforeCombatRewardOffered`
        // 和离开房间时的 `FlushBatchAsync` 还会接住，**不会丢**。日志会写清楚是哪一种。
        //
        // ⚠️⚠️ 2026-10-01：**塞进搜刮单子这一步挪到本方法的最末尾**（原来在开头）。
        // 原因：「胜利那一刻才判定」的任务（只狼·弹反 / 文明6 / 泰拉瑞亚 / 饥荒 / 暗黑 / 原神 /
        // 明日方舟，见 §32 的残留项）是在本方法**下面**才 Grant 的。原来先塞（此时批次还是空的、
        // 或只有战斗中攒下的那点），判定完产生的那批就赶不上本场的搜刮界面，只能晚一个房间发。
        // 挪到末尾 = 判定全部跑完再塞，这一整批就能进**同一张**搜刮单子（和金币/卡牌并排）。
        // （这些 Grant 在本方法里都是同步完成的，所以挪到末尾时 `_batch` 已经装齐了。）

        _ = Grant(QuestKind.CombatsWon, 1);
        _ = Grant(QuestKind.ContractChallenge, 1);   // 血之契约：打赢一场就 +1
        _ = Grant(QuestKind.BowlbugSwarm, 1);        // 盛碗虫泛滥：打赢一场就 +1
        _ = Grant(QuestKind.MonsterRush, 1);         // 怪潮
        _ = Grant(QuestKind.EliteAmbush, 1);         // 精英加餐
        _ = Grant(QuestKind.CursedBargain, 1);       // 诅咒交易
        _ = Grant(QuestKind.DeckOverhaul, 1);        // 大改造
        // 批之九「挑战型」：本场**没违规**才算一场（违规的那场只是不计，不算失败）。
        if (!_attackPlayedThisCombat) _ = Grant(QuestKind.RetreatBlock, 1); // 退退退：全程没打攻击牌
        if (!_playedEarlyThisCombat)  _ = Grant(QuestKind.SlackOff, 1);     // 摸鱼：前 2 回合没出牌
        if (!_powerPlayedThisCombat)  _ = Grant(QuestKind.NakedRun, 1);     // 裸装：没打能力牌
        if (Owner.Gold == 0) _ = Grant(QuestKind.GoldZeroWin, 1);            // 两袖清风

        // —— 批之十：速度型 / 牌组极致型 ——
        // 一发入魂：第一个回合就把这场打完（_combatTurnIndex 在「回合开始」时自增，所以第 1 回合就是 1）
        if (_combatTurnIndex == 1) _ = Grant(QuestKind.FirstTurnKill, 1);
        // 限时特惠：3 回合内打赢才算一场（和「退退退」同一套：违规只是不计，不算失败）
        if (_combatTurnIndex > 0 && _combatTurnIndex <= 3) _ = Grant(QuestKind.SpeedContract, 1);
        // 落地成盒：3 回合内打赢一场**精英战**
        if (room.RoomType == RoomType.Elite && _combatTurnIndex > 0 && _combatTurnIndex <= 3)
            _ = Grant(QuestKind.EliteSpeedKill, 1);
        // 极简主义：牌组很小的时候还赢下一场
        if (Owner.Deck.Cards.Count <= 10) _ = Grant(QuestKind.MinimalDeck, 1);
        // 羊关通关：本场弃牌数（快照）
        _ = Set(QuestKind.DiscardInCombat, _discardsThisCombat);
        if (room.RoomType == RoomType.Elite) _ = Grant(QuestKind.EliteWins, 1);
        if (room.RoomType == RoomType.Boss) _ = Grant(QuestKind.BossWins, 1);
        // 只狼：弹反 —— 全程不掉血 + 格挡一次都没被打穿（完美防守）
        if (!_dmgThisCombat && _blockBrokenThisCombat == 0) _ = Set(QuestKind.ParryFlawless, 1);
        // 文明6：精打细算 —— 本场只打过 1 费牌（且至少打过一张）
        if (_oneCostOkThisCombat && _cardsThisCombat > 0) _ = Set(QuestKind.OnlyOneCostWin, 1);
        // 跨界 第四批
        if (_blockThisCombat >= 100 && !_dmgThisCombat) _ = Set(QuestKind.TerrariaShieldWin, 1);      // 泰拉瑞亚
        if (_selfDamageThisCombat >= 40) _ = Set(QuestKind.DontStarveEndure, 1);                     // 饥荒
        if (_raresThisCombat >= 3) _ = Set(QuestKind.DiabloFarming, 1);                              // 暗黑破坏神
        if (_commonsThisCombat >= 4 && _uncommonsThisCombat >= 2 && _raresThisCombat >= 1)
            _ = Set(QuestKind.GenshinWish, 1);                                                       // 原神
        if (_powersThisCombat >= 4) _ = Set(QuestKind.ArknightsDeploy, 1);                           // 明日方舟
        if (!_dmgThisCombat)
        {
            _ = Grant(QuestKind.FlawlessCombats, 1);
            _ = Grant(QuestKind.FlawlessCombatsBig, 1); // 「稳如老狗」
            // 农种（梗）：连续无伤。开刷的爽种子，前期就该一路平推。
            _flawlessStreak++;
            if (_flawlessStreak >= 3) _ = Grant(QuestKind.JackpotStreak, 1);
        }
        else
        {
            _flawlessStreak = 0; // 掉过血就断了
        }

        var c = Owner.Creature;
        if (c != null && c.MaxHp > 0 && c.CurrentHp * 100 <= c.MaxHp * 30)
        {
            _ = Grant(QuestKind.CloseCalls, 1);
            _ = Grant(QuestKind.CloseCallsBig, 1); // 「玩命」→ 二刀流
        }
        // 绝不认输（梗）：以低于 10% 生命赢下来（白夕Seal 直播间那首禁歌的梗，死磕不 SL）
        if (c != null && c.MaxHp > 0 && c.CurrentHp * 10 <= c.MaxHp)
            _ = Grant(QuestKind.NeverSurrender, 1);
        // 你知道是谁（梗）：整场只用 2 种牌就赢了（「一层两牌无限」那种极限 feat）
        if (_cardsThisCombat > 0 && _cardEntriesThisCombat.Count <= 2)
            _ = Grant(QuestKind.TwoCardWin, 1);

        // 朴实无华：精英或首领战，整场没打过罕见/稀有牌
        if ((room.RoomType == RoomType.Elite || room.RoomType == RoomType.Boss) && !_nonCommonPlayedThisCombat)
            _ = Grant(QuestKind.CommonsOnlyWins, 1);

        // 我说别带（梗）：整场一张技能牌都没打，还打赢了（和「朴实无华」不是一个维度：
        // 那个管稀有度，这个管牌的类型）
        if (!_skillPlayedThisCombat) _ = Grant(QuestKind.NoSkillCombatWin, 1);

        // 战个未来（梗）：先被打到半血以下、最后还赢 = 先亏后赚。
        // 结束时血本来就少于一半的，也一定「先亏」过，所以一并算数。
        var selfNow = Owner.Creature;
        if (_bloodiedThisCombat
            || (selfNow != null && selfNow.MaxHp > 0 && selfNow.CurrentHp * 2 <= selfNow.MaxHp))
            _ = Grant(QuestKind.BloodiedWin, 1);

        // 打赢假商人（梗）：它那一战的房间 Id 里带 FAKE_MERCHANT。
        // 反编译确认 CombatRoom.ModelId 就是 Encounter.Id（类型 FakeMerchantEventEncounter）；
        // ⚠️ 而且它的 RoomType 是 Monster 不是 Boss —— 所以不能靠 BossWins 认，必须看 Id。
        if (room.ModelId.Entry.Contains("FAKE_MERCHANT", StringComparison.OrdinalIgnoreCase))
            _ = Grant(QuestKind.FakeMerchantWin, 1);

        // 你玩的鸡煲啊（梗）：故障机器人本局赢下 2 场精英战
        if (room.RoomType == RoomType.Elite && Owner.Character is Defect)
            _ = Grant(QuestKind.DefectEliteWins, 1);

        // 战士鸽（梗）：本场自己挨了 20 点以上，还打赢了（铁甲战士就是挨打变强的）
        if (_selfDamageThisCombat >= 20) _ = Grant(QuestKind.TankedCombatWin, 1);

        // —— 乐子批 ——
        // 我从地狱蠕动出来了（B 站那条 6.9 万播放）：以 1 点生命赢下这场
        if (selfNow != null && selfNow.CurrentHp == 1) _ = Grant(QuestKind.HellCrawlWin, 1);
        // 谁最适合结婚（社区连载梗）：满血赢下首领战
        if (room.RoomType == RoomType.Boss && selfNow != null && selfNow.CurrentHp >= selfNow.MaxHp)
            _ = Grant(QuestKind.MarriageBoss, 1);
        // 咕咕嘎嘎：整场一张牌都没打，还赢了
        if (_cardsThisCombat == 0) _ = Grant(QuestKind.GuguGaga, 1);
        // 小蓝人（梗）：故障机器人赢下首领战（社区管机器人叫小蓝人）
        if (room.RoomType == RoomType.Boss && Owner.Character is Defect)
            _ = Grant(QuestKind.DefectBossWins, 1);
        // 太牢了（梗）：一场战斗磨到第 10 个回合还赢下来
        if (_combatTurnIndex >= 10) _ = Grant(QuestKind.LongCombatWin, 1);

        // 礼让一手：只打了一回合就赢，且那一回合没出牌
        if (_isEliteCombat && _combatTurnIndex == 1 && _turnCards == 0)
            _ = Grant(QuestKind.EliteFirstTurnIdle, 1);

        // 闪电战：4 回合内打完
        if (_combatTurnIndex > 0 && _combatTurnIndex <= 4) _ = Grant(QuestKind.FastCombats, 1);

        // 一身清白：牌组里**最多 1 张**诅咒，赢下 5 场 → 御守
        // ⚠️ 原来要求「零诅咒」，玩家指出 A5 起会白送一张「进阶之灾」——
        //    反编译 AscendersBane 确认它带 Eternal 关键词（无法移除），高进阶下这任务永远做不完。
        if (DeckCurseCount() <= 1) _ = Grant(QuestKind.CurseFreeCombats, 1);

        // —— 鸡煲三部曲 ——
        // 还在启动：前 3 个回合没出过牌，而且撑到了第 4 个回合还赢了
        if (_combatTurnIndex >= 4 && _earlyTurnCards == 0) _ = Grant(QuestKind.SlowStartWin, 1);
        // 严父在上：第一幕（CurrentActIndex 0）的精英战
        if (room.RoomType == RoomType.Elite && Owner.RunState.CurrentActIndex == 0)
            _ = Grant(QuestKind.ActOneEliteWin, 1);

        // 一个都不放过（改版）：第一幕的精英战这一半（另一半是宝箱房，见 CheckTreasureQuest）
        if (room.RoomType == RoomType.Elite && Owner.RunState.CurrentActIndex == 0)
        {
            ActOneEliteWon = true;
            CheckTreasureQuest();
        }

        // 原装出厂：二幕首领（CurrentActIndex: 一幕=0、二幕=1），全程没动过生命上限
        if (room.RoomType == RoomType.Boss
            && Owner.RunState.CurrentActIndex == 1
            && BaselineMaxHp > 0
            && c != null && c.MaxHp == BaselineMaxHp)
            _ = Grant(QuestKind.UnchangedMaxHpBossWin, 1);

        _ = Set(QuestKind.GoldHoard, Owner.Gold);
        _ = Set(QuestKind.GoldHoardBig, Owner.Gold);
        CheckDeckQuests();
        CheckBoatRelics();
        CheckFakeRelics();
        CheckCollectionQuests();

        // 胜利那一刻判定的任务全跑完了 → 现在把这一整批塞进本场「搜刮」单子
        // （为什么放在最后、而不是方法开头：见本方法开头的说明）。
        //
        // ⚠️ 联机（2026-10-08 修）：**这里不能再判 `IsLocalOwner`**。
        //    房间的「额外奖励」是按玩家存的（`CombatRoom.ExtraRewards[player]`），战斗结束时
        //    `RewardsSet.WithRewardsFromRoom` 会把它并进**那位玩家**的奖励集。
        //    只有本人那台塞 → 各端那位玩家的奖励集**内容不一样**（编号却一样）→
        //    本人那台点了「第 3 项」，别人那台去取自己的第 3 项 → 发错东西，甚至下标越界抛异常。
        //    所以各端都要塞同一批（`AfterCombatVictory` 这个钩子各端都会跑）。
        PushBatchIntoRoom(room, "AfterCombatVictory");

        // 联机自检：战斗刚结束、奖励还没发出去 —— 这是「各端计数器应当一致」的第二个观察点。
        LogNetCounters("战斗胜利");

        // 战斗中攒下的奖励**不在这里补发**：这时候游戏的战斗奖励结算界面正在搭，
        // 我们再 OfferCustom 一个奖励会打架（玩家报过"凭空弹界面 / 奖励丢失"）。
        // 改成一进下一个房间就补（见房间钩子里的 FlushPendingRewards）。
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        // 这里**故意不清零**“本场”标记。AfterCombatEnd 与 AfterCombatVictory 谁先谁后没实测过，
        // 万一 End 先跑，Victory 里的判断（毫发无伤 / 朴实无华 / 闪电战 / 礼让一手）就会读到被清空的值。
        // 这些标记统一在「进战斗房间」时清零（见 AfterRoomEntered），这样谁先谁后都不影响。
        return Task.CompletedTask;
    }

    // —— 房间：进入过的房间类型 + 进商店弹选任务 ——
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room == null) return;
        EnsureBaselineMaxHp();

        // 进战斗房间时把“本场”标记清干净，避免上一处（事件房 / 上一场战斗）的状态串场。
        if (room is CombatRoom)
        {
            _dmgThisCombat = false;
            _turnCards = 0;
            _sawFirstTurn = false;
            _combatTurnIndex = 0;
            _nonCommonPlayedThisCombat = false;
            _costsThisTurn.Clear();
            _orbsThisTurn.Clear();
            _boatBlockUsedThisCombat = false;
            _earlyTurnCards = 0;
            _cardEntriesThisTurn.Clear();
            _skillPlayedThisCombat = false; // 「我说别带」本场重新开始算
            _bloodiedThisCombat = false;    // 「战个未来」本场重新开始算
            _selfDamageThisCombat = 0;      // 「战士鸽」本场重新开始算
            _cardsThisCombat = 0;           // 「咕咕嘎嘎」
            _exhaustThisCombat = 0;         // 「枯枝还魂」是单场计数，每场重新开始算
            _oneCostOkThisCombat = true;    // 「文明6」每场重新开始算
            _raresThisCombat = 0; _commonsThisCombat = 0; _uncommonsThisCombat = 0; _powersThisCombat = 0;
            _blockBrokenThisCombat = 0;     // 「破防」
            _orbsThisCombat = 0;            // 「雷霆大机煲」
            _cardEntriesThisCombat.Clear(); // 「你知道是谁」
            _blockThisCombat = 0;           // 「龟壳」
            _blockThisTurn = 0;             // 「我说叠甲有没有懂的」
            _skillsThisTurn = 0;            // 「花拳绣腿」
            _attackPlayedThisCombat = false; // 「退退退」本场重新开始算
            _powerPlayedThisCombat = false;  // 「裸装」本场重新开始算
            _playedEarlyThisCombat = false;  // 「摸鱼」本场重新开始算
            _discardsThisCombat = 0;         // 「羊关通关」本场重新开始算
            QuestRunner.ResetProgress(QuestKind.CardsGeneratedInCombat); // 「复制防御」是单场计数
            _isEliteCombat = room.RoomType == RoomType.Elite;
        }
        // 上一场战斗（或误判成"战斗中"）排队没发出去的奖励，一进新房间就补发。
        // ⚠️ 「展望未来」这种非战斗触发的任务也会被 `IsInProgress` 判成战斗中而排队，
        //    玩家放弃这局，排队的奖励就彻底丢了（2026-09-25 玩家实测：日志里只有"等这场打完再发"）。
        // 攒着的任务奖励：不在战斗里就交出去（战斗里完成的那批在完成当时就已经并进战后奖励了）。
        // ⚠️ 不能再写 "有待发队列才触发" —— 那个队列已经废弃，条件永远不成立（2026-09-24 踩过）。
        if (!CombatManager.Instance.IsInProgress)
        {
            _lootPending = false;   // 已经进新房间了，搜刮界面肯定走完了
            // ⚠️ 别再单独调 `RescheduleRestoredRewards()`：`FlushPendingRewards` 里已经调了，
            //    调两次会把同一批欠账**重建两遍**、`_batch` 里每样变两份（2026-10-01 日志实测）。
            _ = FlushPendingRewards();
        }

        _roomTypes.Add(room.RoomType);
        _ = Set(QuestKind.RoomTypesSeen, _roomTypes.Count);
        _ = Set(QuestKind.GoldHoard, Owner.Gold);
        _ = Set(QuestKind.GoldHoardBig, Owner.Gold);

        // 一个都不放过（改版）：第一幕的宝箱房 + 第一幕的精英战，两个都做到才算 → 套娃
        // 原来只要求「开第一个宝箱房」= 白送（每幕都有宝箱房），玩家反馈太容易。
        if (room.RoomType == RoomType.Treasure)
        {
            Treasures++;
            _ = Set(QuestKind.MinecraftDig, Treasures);   // 我的世界：挖矿
            if (Owner.RunState.CurrentActIndex == 0) ActOneChestOpened = true;
            CheckTreasureQuest();
        }

        // 问号猎手：地图上的 ? 房就是 RoomType.Event（已反编译确认）
        if (room.RoomType == RoomType.Event) _ = Grant(QuestKind.EventRooms, 1);

        // 只看不买：进商店清零标记，离开商店（进下一个非商店房间）时结算
        if (room.RoomType == RoomType.Shop)
        {
            _inShop = true;
            _shopSpent = false;
            _goldAtShopEntry = Owner.Gold; // 记下进店时的家底（基米精神要看这个）

            // 展望未来（改版）：接了这条任务、身上又有 250 金币 → 进店当场成交，自动扣钱。
            // ⚠️ 必须确认「这条任务正在进行」，否则没接任务也会白扣钱。
            bool wantXDeal = IsLocalOwner
                             && QuestRunner.Active.Any(q => q.Def.Kind == QuestKind.ShopXDeal && !q.Completed);
            if (wantXDeal)
            {
                // 留证据：玩家反馈「展望未来好像根本没发奖励」，日志里得能看出是哪一步没满足。
                Entry.Logger.Info($"展望未来：进了商店，金币 {Owner.Gold}/250。");
                if (Owner.Gold >= 250)
                {
                    try
                    {
                        await PlayerCmd.LoseGold(250, Owner, GoldLossType.Spent);
                        if (QuestRunner.PlayerCount > 1)
                            RunManager.Instance.RewardSynchronizer?.SyncLocalGoldLost(250);
                        Entry.Toast("付了 250 金币，收下这份未来", "展望未来");
                        Entry.Logger.Info("展望未来：已扣 250 金币，准备发奖。");
                        _ = Set(QuestKind.ShopXDeal, 1);
                    }
                    catch (System.Exception e)
                    {
                        Entry.Logger.Error($"展望未来扣 250 金币失败: {e}");
                    }
                }
            }
        }
        else if (_inShop)
        {
            if (!_shopSpent) _ = Grant(QuestKind.WindowShopping, 1);
            // 基米精神（梗）：揣着一大笔钱进商店、一分不花就走 = 不打过渡、死赌未来
            if (!_shopSpent && _goldAtShopEntry >= 200) _ = Grant(QuestKind.GreedyShopVisit, 1);
            _inShop = false;
        }

        // 满血见王：进首领房时还是满血
        if (room.RoomType == RoomType.Boss)
        {
            var boss = Owner.Creature;
            if (boss != null && boss.MaxHp > 0 && boss.CurrentHp >= boss.MaxHp)
                _ = Grant(QuestKind.FullHpAtBoss, 1);
        }

        CheckDeckQuests();
        CheckBoatRelics();
        CheckFakeRelics();
        CheckCollectionQuests();
        await EnsureQuestSetups();   // 接取时改牌组的任务（诅咒交易 / 大改造）

        // 商店：点亮按钮；非商店：只收回商店那次（开局那批留给玩家自己点）。
        if (room.RoomType == RoomType.Shop) QuestSelection.OnShopEntered(Owner.Character?.GetType());
        else QuestSelection.OnOtherRoom();

        // 联机自检：进房间是「补发 + 各类判定」的收口，也是检查各端计数器有没有跑偏的最好时机。
        // 两台机器的日志里这两行的 choiceIds / rewardIds / due / paid 必须逐字相同（册主 NetId 不同）。
        LogNetCounters("进房间:" + room.RoomType);
        return;
    }

    // —— 卡组构成类：条件成立就算完成，进度只写 1、不回退 ——
    // ⚠️ 依赖「Player.Deck 就是整局卡组」这一条（反编译 PileType 注释确认过，但还没进游戏实测）。
    // 万一实测发现它其实是战斗抽牌堆，这三个任务会永远停在 0，不会崩。
    private void CheckDeckQuests()
    {
        var deck = Owner.Deck.Cards;
        if (deck.Count == 0) return;

        int attacks = 0, skills = 0, powers = 0;
        int commons = 0, uncommons = 0, rares = 0;
        int evenCost = 0, oddCost = 0;
        int upgraded = 0;   // 东尼意思：卡组里升级过的牌数
        int cosmic = 0;     // 宇！宙！冷！漠！：卡组里的「宇宙冷漠」张数
        int strikes = 0;    // 地狱狂徒：卡组里带「打击」标签的牌数
        int zeroCost = 0;   // 斗地主附体（批之十）：卡组里的 0 费牌数
        int otherClass = 0; // 国际纵队（批之十）：卡组里别的职业的牌数

        foreach (var card in deck)
        {
            switch (card.Type)
            {
                case CardType.Attack: attacks++; break;
                case CardType.Skill: skills++; break;
                case CardType.Power: powers++; break;
            }
            switch (card.Rarity)
            {
                case CardRarity.Common: commons++; break;
                case CardRarity.Uncommon: uncommons++; break;
                case CardRarity.Rare: rares++; break;
            }
            // 偶数为王：按一代原意排除起始牌（起始牌是 1 费的 Basic，不排除就永远不可能）
            if (card.Rarity != CardRarity.Basic)
            {
                int cost = card.EnergyCost.Canonical;
                if (cost >= 0)
                {
                    if (cost % 2 == 0) evenCost++;
                    else oddCost++;
                }
            }

            if (card.IsUpgraded) upgraded++;
            if (card is CosmicIndifference) cosmic++;
            if (card.Tags.Contains(CardTag.Strike)) strikes++;
            if (card.EnergyCost.Canonical == 0) zeroCost++;
            if (!IsOwnerClassCard(card)) otherClass++;
        }

        // 宇！宙！冷！漠！/ 地狱狂徒：**实时**报当前张数（以前是"到了目标才写"，导致面板一直显示 0）。
        _ = Set(QuestKind.CosmicIndifferencePlayed, cosmic);
        _ = Set(QuestKind.StrikeDeckHellraiser, strikes);
        // 批之十：卡组里的「0 费牌数量」和「别的职业的牌数量」（都是实时快照）
        _ = Set(QuestKind.ZeroCostDeck, zeroCost);
        _ = Set(QuestKind.RainbowDeck, otherClass);
        // 我的世界：纯净卡组 —— 15 张以上且一张都不重样（维度：牌组唯一性）
        // 宝可梦：全队进化 —— 升级过的牌 12 张以上（升级维度的加强版）
        if (upgraded >= 12) _ = Set(QuestKind.PokemonAllEvolve, 1);
        int starters = deck.Count(c =>
        {
            var sid = c.Id.ToString() ?? "";
            return sid.Contains("STRIKE_") || sid.Contains("DEFEND_");
        });
        if (deck.Count >= 15 && starters <= 2) _ = Set(QuestKind.DeckNoDuplicates, 1);

        if (attacks == 10 && skills == 10 && powers == 5) _ = Set(QuestKind.DeckOrganized, 1);
        // 全家桶（叙事）：攻击 / 技能 / 能力 各攒够 6 张
        if (attacks >= 6 && skills >= 6 && powers >= 6) _ = Set(QuestKind.FamilyBucket, 1);
        if (commons - uncommons - rares >= 7) _ = Set(QuestKind.DeckPeasant, 1);
        if (evenCost >= 7 && oddCost == 0) _ = Set(QuestKind.DeckEvenOnly, 1);
        // 东尼意思（梗）：打不过就把别的都加强 —— 卡组里攒够 8 张升级过的牌
        if (upgraded >= 8) _ = Set(QuestKind.UpgradedDeck, 1);
        // 自暴自弃（乐子）：卡组里攒到 5 张以上诅咒（和「一身清白」正好相反）
        int curses = DeckCurseCount();
        if (curses >= 5) _ = Set(QuestKind.SelfDestruct, curses);
    }

    // —— 2026-10-01 批：跨游戏「收集 / 成长」类 ——
    // 读当前**持有量**，用 Set（绝对值、只增不减）记快照。
    // 数据来源都反编译核对过：Player.Relics(遗物列表) / Player.Potions(药水) /
    // Player.Deck.Cards(整局卡组) / Creature.MaxHp（配上开局记的 BaselineMaxHp）。
    private void CheckCollectionQuests()
    {
        try
        {
            _ = Set(QuestKind.RelicsHeld, Owner.Relics.Count);
            _ = Set(QuestKind.PotionsHeld, Owner.Potions.Count());
            _ = Set(QuestKind.DeckSizeBig, Owner.Deck.Cards.Count);

            var c = Owner.Creature;
            if (c != null && BaselineMaxHp > 0)
                _ = Set(QuestKind.MaxHpGained, c.MaxHp - BaselineMaxHp);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"收集类任务统计失败: {e.Message}");
        }
    }

    // 牌组里有几张诅咒（「一身清白」用）。
    // ⚠️ 阈值是 1 不是 0：A5 起白送的「进阶之灾」带 Eternal，移除不掉（见 AscendersBane 反编译）。
    private int DeckCurseCount()
    {
        int n = 0;
        foreach (var card in Owner.Deck.Cards)
            if (card.Type == CardType.Curse) n++;
        return n;
    }

    // 一个都不放过：第一幕的宝箱房 + 第一幕的精英战，两个都做到才算。
    // 两个标记都进存档（读档回来不会丢，否则读档后宝箱房已经开过、任务就再也做不完了）。
    private void CheckTreasureQuest()
    {
        if (ActOneChestOpened && ActOneEliteWon) _ = Set(QuestKind.TreasureRooms, 1);
    }

    /// <summary>
    /// 「升级一张牌」型奖励：从牌组里挑 n 张还能升级的牌，直接升掉。
    /// 借自《海克斯符文》——奖励不一定是「给你新东西」，也可以是「让你已有的东西变强」。
    ///
    /// ⚠️⚠️ 联机（2026-10-08 修）：**这里原来在多人局直接折现 150 金币**，
    ///   理由是「升级牌没有对应的同步接口」。**那条结论是错的**，反编译实证：
    ///
    ///   `CardCmd.Upgrade(IEnumerable&lt;CardModel&gt;, CardPreviewStyle)` 的实现是——
    ///   对每张牌依次执行 `UpgradeInternal()` + `FinalizeUpgradeInternal()`（改的是**共享的 run 状态**），
    ///   只有末尾那段**升级特效**被 `if (!LocalContext.IsMine(card)) continue;` 闸住。
    ///   也就是说：**它本来就是设计成「各端都跑」的**，和 `CardPileCmd.Add` 一个套路。
    ///
    ///   佐证：本体篝火「铁匠」`SmithRestSiteOption.OnSelect` 就是
    ///   `CardSelectCmd.FromDeckForUpgrade(...)` + `CardCmd.Upgrade(item, CardPreviewStyle.None)`，
    ///   **没有任何额外的 `SyncLocal*` 调用** —— 它靠的正是「`OnSelect` 在各端都会跑」
    ///   （`RestSiteSynchronizer` 把选项广播给所有端）。我们现在 `Reward()` 也是各端都跑，形状完全一致。
    ///
    ///   所以正确的做法不是折现，而是：**各端都算出同一批牌、各端都执行 Upgrade**。
    ///   挑牌必须确定性（`SeedFor` + `ShuffleDeterministic`），否则各端升的不是同一批 → 数据分叉。
    ///
    ///   为什么不用 `CardSelectCmd.FromDeckForUpgrade`（那个更"官方"）：
    ///   它会给本人弹 `NDeckUpgradeSelectScreen` 选牌界面，而 `Reward()` 可能在**战斗中**被触发，
    ///   战斗中途弹选牌界面会打架。所以这里走「静默 + 确定性」这条路。
    /// </summary>
    private async Task<string> UpgradeRandomCards(int n)
    {
        // 稳定候选集：各端必须挑出**同一批**牌。
        // 先按 Id 排序（LINQ `OrderBy` 是稳定排序，重复的牌保持原有相对顺序），
        // 再用固定种子洗 —— 这样结果**不依赖各端牌组的内部排列顺序**。
        // （重复牌：挑中哪一张副本结果都一样，升级后序列化出来是同一个东西，不影响一致性。）
        var candidates = Owner.Deck.Cards
            .Where(c => c.IsUpgradable)
            .OrderBy(c => c.Id.ToString(), StringComparer.Ordinal)
            .ToList();

        if (candidates.Count == 0)
        {
            await GainGoldSynced(150);
            return "牌组里没有能升级的牌了，折现 150 金币";
        }

        var picked = CardGift
            .ShuffleDeterministic(candidates, CardGift.SeedFor(Owner, ++_giftSeq))
            .Take(n)
            .ToList();

        // ⚠️ 各端都跑（**不能**加 IsLocalOwner 闸）：见上面那段说明。
        CardCmd.Upgrade(picked, MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle.None);

        if (QuestRunner.PlayerCount > 1)
            Entry.Logger.Info($"升级型奖励：各端同时升级 {picked.Count} 张牌（不再折现）");
        return $"升级了牌组里的 {picked.Count} 张牌";
    }

    // 记下本局开局时的生命上限，只记一次。
    private void EnsureBaselineMaxHp()
    {
        if (BaselineMaxHp > 0) return;
        var c = Owner.Creature;
        if (c != null && c.MaxHp > 0) BaselineMaxHp = c.MaxHp;
    }

    // —— 船长的执念：数一数玩家手上有几件船用零件 ——
    private void CheckBoatRelics()
    {
        int parts = 0;
        if (Owner.GetRelic<Anchor>() != null) parts++;
        if (Owner.GetRelic<HornCleat>() != null) parts++;
        if (Owner.GetRelic<CaptainsWheel>() != null) parts++;
        _ = Set(QuestKind.CaptainsLegacy, parts);
    }

    // —— 假货（假商人卖的 Fake* 遗物）——
    // 假商人一共 9 件假货：假锚「锚???」/ 假血瓶 / 假快乐花 / 假华夫饼 / 假芒果 / 假奥利哈刚 /
    // 假蛇眼 / 假打击木桩 / 假古茶具。
    // 它们**是真的独立遗物**（dll 里的类型名就是 FakeAnchor 这种），所以不用去猜事件流程，
    // 直接扫玩家遗物列表、看 Id 里带不带 FAKE 就行。
    // 顺带做一条「真的假的」：真锚 + 假锚「锚???」同时在手。
    private void CheckFakeRelics()
    {
        int fakes = 0;
        bool fakeAnchor = false;
        foreach (var relic in Owner.Relics)
        {
            if (!relic.Id.Entry.Contains("FAKE", StringComparison.OrdinalIgnoreCase)) continue;
            fakes++;
            if (relic.Id.Entry.Contains("ANCHOR", StringComparison.OrdinalIgnoreCase)) fakeAnchor = true;
        }

        _ = Set(QuestKind.FakeRelicsOwned, fakes);
        if (fakeAnchor && Owner.GetRelic<Anchor>() != null)
            _ = Set(QuestKind.RealAndFakeAnchor, 1);
    }

    // —— 可随机生成的牌池 / 生成牌 ——
    // 都挪到 CardGift 里了（我们自己的遗物和「随身物」版仿制枯枝共用同一份实现）。

    /// <summary>
    /// 发**我们自己造的**遗物（不依赖任何别的 mod）。已经有了就折现。
    /// 发放异常时执行 <paramref name="fallback"/>（改成挂在任务册上的「随身物」效果），绝不静默失败。
    /// </summary>
    private async Task<string> GrantOwnRelic<T>(string cnName, Action fallback) where T : RelicModel
    {
        if (Owner.GetRelic<T>() != null)
        {
            await GainGoldSynced(150);
            return $"你已经有一件「{cnName}」了，折现 150 金币";
        }

        try
        {
            if (await OfferRelic(ModelDb.Relic<T>().ToMutable())) return $"获得遗物「{cnName}」";
            var obtained = await RelicCmd.Obtain<T>(Owner);
            SyncRelic(obtained); // 兜底：直接给也要通知其他客户端
            return $"获得遗物「{cnName}」";
        }
        catch (System.Exception e)
        {
            Entry.Logger.Error($"发放遗物「{cnName}」失败，改用随身物效果: {e}");
            fallback();
            return $"获得「{cnName}」的效果（遗物发放失败，改为随身物）";
        }
    }

    // 「随身物」版的枯枝（只在真的遗物发不出去时才会用到）
    private async Task GiveBranchCard()
    {
        if (await CardGift.GiveRandomCardToHand(Owner)) Flash();
    }

    // 展望未来：发一张 X 费牌的卡牌奖励（三选一、不限职业，玩家自己挑）
    private string GrantRandomXCard()
    {
        var reward = CardGift.BuildXCardReward(Owner, CardGift.SeedFor(Owner, ++_giftSeq));
        if (reward == null)
        {
            _batch.Add(new GoldReward(150, Owner));
            return "找不到 X 费牌，折现 150 金币";
        }
        _batch.Add(reward);
        return "一张 X 费牌（三选一，不限职业）";
    }

    // —— 一代遗物奖励 ——
    //
    // 思路：不硬依赖任何 mod。运行期在游戏的内容库（ModelDb）里按 ID 找那件一代遗物——
    // 只要玩家装了「更多遗物」(moreRelics) 这类移植 mod，就找到真货直接发；
    // 找不到（没装 mod / 那件在 mod 配置里被关了）就退回我们自己的仿制效果（QuestBoons）。
    //
    // 这些移植 mod 的遗物 ID 就是它写的那串英文，例如 moreRelics 的 [CustomID("DeadBranch")]
    // → 本地化 key 是 "DeadBranch.title"，中文名是「枯木树枝」。
    private static RelicModel? FindLegacyRelic(string entryKeyword)
    {
        foreach (var model in ModelDb.All)
        {
            if (model is RelicModel r
                && r.Id.Entry.Contains(entryKeyword, StringComparison.OrdinalIgnoreCase))
                return r;
        }
        return null;
    }

    /// <summary>
    /// 发一件一代遗物。找到真货就发真的，找不到就执行 <paramref name="fallback"/>。
    /// </summary>
    private async Task<string> GrantLegacyRelic(string entryKeyword, string cnName, Func<Task<string>> fallback)
    {
        RelicModel? found = null;
        try
        {
            found = FindLegacyRelic(entryKeyword);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"FindLegacyRelic({entryKeyword}) failed: {e.Message}");
        }

        if (found == null)
        {
            string fallbackMsg = await fallback();
            return $"{fallbackMsg}（没装带「{cnName}」的遗物 mod，给的是仿制品）";
        }

        if (Owner.GetRelicById(found.Id) != null)
        {
            await GainGoldSynced(120);
            return $"你已经有一件「{cnName}」了，折现 120 金币";
        }

        try
        {
            var mutable = found.ToMutable();
            if (await OfferRelic(mutable)) return $"获得遗物「{cnName}」";
            await RelicCmd.Obtain(mutable, Owner);
            SyncRelic(mutable); // 兜底
            return $"获得遗物「{cnName}」";
        }
        catch (System.Exception e)
        {
            Entry.Logger.Error($"Failed to grant legacy relic {entryKeyword}: {e}");
            string fallbackMsg2 = await fallback();
            return $"{fallbackMsg2}（发放「{cnName}」失败，给的是仿制品）";
        }
    }

    /// <summary>
    /// 同 <see cref="GrantOwnRelic{T}"/>，但兜底是折现金币（用于那些「没有随身物版本」的遗物）。
    /// </summary>
    private async Task<string> GrantOwnRelicOrGold<T>(string cnName, int fallbackGold) where T : RelicModel
    {
        if (Owner.GetRelic<T>() != null)
        {
            await GainGoldSynced(fallbackGold);
            return $"你已经有一件「{cnName}」了，折现 {fallbackGold} 金币";
        }

        try
        {
            if (await OfferRelic(ModelDb.Relic<T>().ToMutable())) return $"获得遗物「{cnName}」";
            var obtained = await RelicCmd.Obtain<T>(Owner);
            SyncRelic(obtained); // 兜底
            return $"获得遗物「{cnName}」";
        }
        catch (System.Exception e)
        {
            Entry.Logger.Error($"发放遗物「{cnName}」失败，折现 {fallbackGold} 金币: {e}");
            await GainGoldSynced(fallbackGold);
            return $"获得 {fallbackGold} 金币（遗物发放失败）";
        }
    }

    // ==========================================================================
    // 多人局：本模组会**直接改动游戏状态**（发遗物 / 加金币 / 加牌），而游戏自己的奖励流程
    // 是靠 `RewardSynchronizer` 通知其他客户端的（见 MerchantRelicEntry / CrystalSphereCurse）。
    // 我们不通知的话，各客户端的金币、遗物、卡组就会不一致 → 「多人数据不同步」。
    // 所以每次改状态都补一个同步调用。
    // ==========================================================================
    /// <summary>
    /// 发金币：**走游戏自己的奖励流程**（`RewardsCmd.OfferCustom` + `GoldReward`）。
    /// 那套流程自带多人同步（`RewardsSetSynchronizer`），而直接 `player.Gold += x` 只有本机知道
    /// → 其他客户端对不上 → 多人数据不同步（2026-09-24 玩家反馈的那条）。
    /// 本方法只在**非战斗状态**被调用（战斗中的奖励先排队，见 Reward）。
    /// </summary>
    private async Task GainGoldSynced(int amount)
    {
        // **战斗外直接到手**（商人 / 水晶球那套写法 + 官方同步接口）。
        // ⚠️ 以前战斗外走"弹奖励界面"，那个时机游戏不接受 → 玩家报"战斗外完成任务没奖励"。
        if (!CombatManager.Instance.IsInProgress)
        {
            // ⚠️ 联机：**只有本人那台真正加钱**（加完用 SyncLocalObtainedGold 通知别人，
            //    别人那边由 `RewardSynchronizer.HandleRewardObtainedMessage` 给「发送者」补上）。
            //    非本人那台直接返回 —— 但注意**不能连 `_batch` 那条路一起跳过**（下面那句要各端都走到，
            //    否则各端建的奖励集内容不一样 → 校验和不匹配 → 掉线）。
            if (!IsLocalOwner) return;
            await PlayerCmd.GainGold(amount, Owner);
            if (QuestRunner.PlayerCount > 1)
            {
                try { RunManager.Instance.RewardSynchronizer?.SyncLocalObtainedGold(amount); }
                catch (System.Exception ex) { Entry.Logger.Warn($"SyncLocalObtainedGold 失败: {ex.Message}"); }
            }
            return;
        }
        // 战斗里：攒进"这批"，打赢时并进官方战后奖励（和金币卡牌同一个界面）。
        _batch.Add(new GoldReward(amount, Owner));
    }

    // —— 一批任务奖励：全部塞进**同一个**奖励界面 ——
    // 玩家反馈（2026-09-25）：奖励一个个弹、还可能互相顶掉。游戏自己的战斗奖励就是
    // 金币+卡牌+遗物列在一个界面里，我们照这个来。
    private readonly List<Reward> _batch = new();

    /// <summary>
    /// 发遗物：**直接进遗物栏**——和商人 / 水晶球一样的写法（`RelicCmd.Obtain` + `SyncLocalObtainedRelic`）。
    ///
    /// ⚠️ 以前走 `RewardsCmd.OfferCustom(RelicReward)`：那会**凭空弹出一个奖励界面**，
    /// 而且时机不对时会被房间切换顶掉 → 玩家报「还没点就弹了界面、遗物还没拿到」（2026-09-25）。
    /// 直接发既不弹界面，也不会丢。
    /// </summary>
    private async Task<bool> OfferRelic(RelicModel relic)
    {
        // 保险：**已经有的遗物绝不放进奖励界面**。
        // 游戏会给它建一个按钮，但点下去加不进第二件 → 按钮没反应 → 玩家**卡在奖励界面出不来**
        // （2026-09-25 玩家实测卡死：界面里两件"一样的枯枝"）。
        if (Owner.GetRelicById(relic.Id) != null)
        {
            Entry.Logger.Warn($"已经有遗物「{relic.Id}」了，不进奖励界面（调用方会折现）。");
            return false;
        }
        // **一律直接进遗物栏**（商人 / 水晶球那套：RelicCmd.Obtain + 官方同步接口）。
        // ⚠️ 绝对不要再放进奖励界面：日志确认 `RelicReward.OnSelect` 会对我们的遗物抛异常，
        //    结果是"界面里点了没反应、遗物拿不到"（2026-09-24 玩家实测）。
        // ⚠️ 战斗中绝不能发：游戏禁止在战斗中同步遗物事件，硬发会被判数据不同步 → 踢回主菜单。
        if (CombatManager.Instance.IsInProgress)
        {
            _pendingRelics.Add(relic);
            Entry.Logger.Info("遗物先排队，打完这场再发（战斗中不能同步遗物）。");
            return true;
        }
        // ⚠️ 联机：非本人那台**不真发**（发了会变成两份 —— 本人那台发完还会 SyncLocalObtainedRelic）。
        //    但要**返回和本人那台一样的值**，否则调用方（GrantOwnRelic 之类）会在不同端走不同分支。
        if (!IsLocalOwner) return true;
        try
        {
            await RelicCmd.Obtain(relic, Owner);
            SyncRelic(relic);
            return true;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"直接发遗物失败: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 把这一批任务奖励**一次性**交给游戏自己的奖励流程（`RewardsCmd.OfferCustom`）。
    ///
    /// 战斗中不开界面（战斗结算正在用），等战斗结束那一刻（`BeforeCombatRewardOffered`）
    /// 或进下一个房间（`FlushPendingRewards`）再交。
    /// ⚠️ 万一两次都没成，`_dueKinds`（已进存档）还记着账，读档/进房间还会再补——**不会静默丢**。
    /// </summary>
    private void ScheduleFlush()
    {
        if (_batch.Count == 0) return;
        if (CombatManager.Instance.IsInProgress) return;   // 战斗中先攒着
        _ = FlushBatchAsync();
    }

    /// <summary>
    /// **读档 / 快速 SL 后**丢掉内存里的奖励对象（`_batch` 等）。
    ///
    /// ⚠️⚠️ 为什么必须清：奖励界面点"领取"时会走游戏自己的
    /// `RewardsSetSynchronizer.SelectLocalReward(reward)`，里面是
    /// **`if (reward.Player != LocalPlayer)` 的实例引用比较**（不是比 NetId）。
    /// SL 之后运行局被重建、本地玩家是**新实例**，而这些奖励对象还攥着**旧的 Player 实例**
    /// → 判定"非本地玩家" → **点了没反应、只能跳过**（2026-10-01 玩家实测 + 日志实锤）。
    /// 清掉之后，对账会用**当前玩家**重建一份新的，实例就对得上了。
    /// （`_dueKinds` / `_paidKinds` 是存档账本，**不清** → 欠的还会补。）
    /// </summary>
    public void ClearInMemoryRewardObjects()
    {
        _batch.Clear();
        _pendingRewards.Clear();
        _pendingRelics.Clear();
    }

    /// <summary>新局：把上一局没发出去的奖励队列一起丢掉（册子实例会跨局复用，不清会跟着进新局）。</summary>
    public void ClearPendingRewards()
    {
        _batch.Clear();
        _pendingRewards.Clear();
        _pendingRelics.Clear();
        _dueKinds.Clear();          // 上一局欠的账不作数
        _rewardedKinds.Clear();     // 防重复的闸也要清，否则新局同一条任务会被当成"已发过"
    }

    private async Task FlushBatchAsync()
    {
        if (_batch.Count == 0) return;

        // ⚠️⚠️ 硬闸：**官方的战后「搜刮」界面还没关掉之前，绝不弹我们自己的框。**
        //
        // 2026-09-28 玩家 Carson 实测（泰拉瑞亚·以盾取胜）：
        //   「打完后弹出选择一张卡牌保留，但是随后马上被打完后的搜刮物（药水金币卡牌）覆盖了，
        //     且拿完搜刮物后被覆盖的任务奖励没有了」
        // 原因：战斗刚结束时 `CombatManager.IsInProgress` 已经变 false，`ScheduleFlush()` 就立刻
        // 弹了我们的框；而官方的搜刮界面是**紧接着**才搭起来的，于是把我们的盖掉了。
        // 所以只要 `_lootPending`（战斗结束、搜刮还没走完）就一律押着，
        // 等玩家真的离开这个房间（`BeforeRoomEntered`）再发。
        if (_lootPending)
        {
            Entry.Logger.Info($"搜刮界面还没走完：{_batch.Count} 项任务奖励押后到离开房间时发（避免被盖掉）。");
            return;   // `_batch` 原样留着，等 BeforeRoomEntered 再发
        }

        // ⚠️⚠️ 串行闸（2026-09-27 玩家问「为什么不是同一个框而是多个框／会被挤掉吗」）：
        // `FlushBatchAsync` 会被三个地方各自触发（`BeforeRoomEntered` / `AfterRoomEntered` / 战斗外完成）。
        // 每次调用都会**弹一个新框**（`RewardsCmd.OfferCustom`），而两次调用挨得近时 ——
        // 后一个框会把前一个**顶掉**（画面被切换），前一个框里的奖励玩家就看不到了。
        // 表现就是「多个框」+「被挤掉」，日志里会出现两条 `一次发了 N 项`。
        // 所以：**同一时间只准有一个发放过程**，后来的请求等着，攒到同一次里一起发。
        if (_offering)
        {
            Entry.Logger.Info($"发放中：{_batch.Count} 项先攒着，等这一轮发完一起给你（避免弹多个框互相顶掉）。");
            _needsAnotherFlush = true;
            return;
        }
        _offering = true;
        try
        {
            do
            {
                _needsAnotherFlush = false;
                if (_batch.Count == 0) break;

                var rewards = _batch.ToList();
                _batch.Clear();

                // 用**我们自己的**奖励流程把这一批一次性摆出来（`RewardsCmd.OfferCustom`）。
                //
                // ⚠️ 2026-09-26 深夜的重大教训：**别再试图往游戏的战后「搜刮」界面里塞东西了。**
                //    前后试了三种塞法，全被同一个规律挡回来——游戏总是在我们动手之前就把清单读走了。
                //    详见 §32~§34。
                // ⚠️ 只在**安全时机**调用（进房间 / 战斗外）。战斗结算那一刻调会打架
                //    —— 玩家报过「凭空弹界面 / 奖励丢失」。
                if (CombatManager.Instance.IsInProgress)
                {
                    Entry.Logger.Info($"任务奖励：此刻还在战斗流程里，{rewards.Count} 项留着下次发（避免和结算界面打架）。");
                    _batch.InsertRange(0, rewards);
                    break;
                }

                try
                {
                    await RewardsCmd.OfferCustom(Owner, rewards);
                    // 真交出去了才销账：记进「已发过」的账（存档），再从「欠着」的账里划掉。
                    foreach (var kind in _dueKinds.ToList()) _paidKinds.Add(kind);
                    _dueKinds.Clear();
                    Entry.Logger.Info($"任务奖励：一次发了 {rewards.Count} 项（同一个界面）。");
                }
                catch (System.Exception e)
                {
                    // 弹不出来就把奖励留着，下次进房间再试——绝不能让奖励凭空消失。
                    Entry.Logger.Error($"发任务奖励失败，留着下次再发: {e}");
                    _batch.InsertRange(0, rewards);
                    break;
                }
            } while (_needsAnotherFlush);
        }
        finally
        {
            _offering = false;
        }
    }

    /// <summary>发放过程是否正在进行（同一时间只准有一个，避免弹多个框互相顶掉）。</summary>
    private bool _offering;
    private bool _needsAnotherFlush;

    /// <summary>
    /// 战斗结束、官方奖励界面**即将摆出来**的时刻。
    ///
    /// ⚠️⚠️ 这里**什么都不做**（2026-09-26 深夜又踩了一次坑，别再往里加东西）：
    ///   - 往里塞任务奖励：试了三种写法全失败（详见 `FlushBatchAsync` 上面的注释）；
    ///   - 在这里调 `OfferCustom` 自己弹框：**更糟** —— 这个时刻战斗结算 + 官方「搜刮」界面
    ///     正在搭建，我们再弹一个盒子会打架。玩家早就报过这个症状：
    ///     「凭空弹界面 / 奖励丢失 / 还没点就弹了界面、遗物还没拿到」（见 `OfferRelic` 的注释）。
    ///
    /// 正确做法（玩家定的）：**战斗中拿到的奖励先攒着，等进了下一个房间再发**——
    /// 见 `AfterRoomEntered` → `FlushPendingRewards`。那才是干净的时机。
    /// </summary>
    public override Task BeforeCombatRewardOffered(RewardsSet rewards, CombatRoom room)
    {
        // 战斗结束、官方「搜刮」界面即将搭起来 —— 从这一刻起，我们**不许再弹自己的框**，
        // 直到玩家离开这个房间（见 `_lootPending` 的说明和 `BeforeRoomEntered`）。
        _lootPending = true;

        // 如果 `AfterCombatVictory` 那一步没把奖励塞进搜刮单子，这里再试一次。
        // ⚠️ 日志会写清楚是哪一步成功的 —— 下一个版本就只留成功的那条路。
        // ⚠️ 联机：**各端都要塞**（理由同 AfterCombatVictory 里那段：奖励集内容必须各端一致）。
        if (_batch.Count > 0)
        {
            Entry.Logger.Warn($"任务奖励：AfterCombatVictory 没塞进去，还剩 {_batch.Count} 项，这里再试一次。");
            PushBatchIntoRoom(room, "BeforeCombatRewardOffered");
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 战斗结束、官方「搜刮」界面正在走 —— 这期间**不准弹我们自己的奖励框**。
    /// 2026-09-28 玩家 Carson 实测的「任务奖励被搜刮界面盖掉、拿完搜刮就没了」就是这个。
    /// 在 `BeforeCombatRewardOffered` 置位，在离开房间 / 进新房间时清除。
    /// </summary>
    private bool _lootPending;

    /// <summary>
    /// 离开当前房间、正要进下一个房间的那一刻。
    ///
    /// 为什么加这个出口（2026-09-26 深夜，玩家问「必须进下一个房间才行？如果不进就 SL 呢」）：
    /// 战斗里攒下的奖励原来只在 `AfterRoomEntered` 发，意思是**必须真的走进下一个房间**才拿得到。
    /// 玩家如果在这一步之前读档/退出，奖励就还欠着（有 `_dueKinds` 兜底不会丢，但体验很差）。
    /// 这里抢在房间切换的路上就把账结了 —— 那时「搜刮」界面已经关掉、新房间还没搭起来，不会打架。
    /// </summary>
    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        // 玩家要离开这个房间了 → 官方「搜刮」已经走完，现在才是我们弹框的安全时机。
        _lootPending = false;
        try
        {
            if (_batch.Count > 0 && !CombatManager.Instance.IsInProgress)
            {
                Entry.Logger.Info($"离开房间：把 {_batch.Count} 项任务奖励发掉。");
                _ = FlushBatchAsync();
            }
        }
        catch (System.Exception e) { Entry.Logger.Error($"BeforeRoomEntered 发奖励失败: {e}"); }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 把攒着的任务奖励**塞进这一场战斗的「搜刮」单子**（照《海克斯符文》的做法）。
    /// 塞成功 = 玩家在官方的「搜刮」界面里直接看到它们，和金币/卡牌同一张单子，只有一个框。
    ///
    /// 返回是否**全部**塞成功。没塞进去的留在 `_batch` 里，由后面的时机兜底。
    /// </summary>
    private bool PushBatchIntoRoom(CombatRoom room, string fromWhere)
    {
        if (_batch.Count == 0) return false;
        var rewards = _batch.ToList();
        var failed = new List<Reward>();
        int n = 0;
        foreach (var r in rewards)
        {
            try { room.AddExtraReward(Owner!, r); n++; }
            catch (System.Exception e)
            {
                Entry.Logger.Error($"[{fromWhere}] AddExtraReward 失败（{r.GetType().Name}）: {e.Message}");
                failed.Add(r);
            }
        }
        _batch.Clear();
        _batch.AddRange(failed);

        if (n > 0)
            Entry.Logger.Info($"[{fromWhere}] 任务奖励：{n} 项已塞进本场「搜刮」单子"
                              + $"[{string.Join("、", rewards.Take(n).Select(r => r.GetType().Name))}]");

        if (failed.Count == 0 && n > 0)
        {
            // 全部交出去了 → 销账（对账那边就不会再把它当欠款）。
            foreach (var k in _dueKinds.ToList()) _paidKinds.Add(k);
            _dueKinds.Clear();
            return true;
        }
        return false;
    }

    private static int SafeCount(RewardsSet? set)
    {
        try { return set?.Rewards?.Count ?? 0; }
        catch (System.Exception) { return -1; }
    }

    private static void SyncRelic(RelicModel? relic)
    {
        if (relic == null) return;
        try { RunManager.Instance.RewardSynchronizer?.SyncLocalObtainedRelic(relic); }
        catch (System.Exception e) { Entry.Logger.Warn($"SyncLocalObtainedRelic 失败: {e.Message}"); }
    }

    private static void SyncCard(CardModel? card)
    {
        if (card == null) return;
        try { RunManager.Instance.RewardSynchronizer?.SyncLocalObtainedCard(card); }
        catch (System.Exception e) { Entry.Logger.Warn($"SyncLocalObtainedCard 失败: {e.Message}"); }
    }

    /// <summary>
    /// 把**本地玩家这本**的随身物镜像到静态的 <see cref="QuestBoons"/> 里 —— 只给任务面板显示用。
    /// 真正生效的是本类的实例字段（每个客户端都会为每位玩家各算一遍，所以联机也不会不同步）。
    /// </summary>
    public void MirrorBoons()
    {
        if (!IsLocalOwner) return;
        QuestBoons.HasBoat = HasBoat;
        // ⚠️ 这四个读**派生属性**（不是存档属性）—— 见属性区那段 F11 说明。
        QuestBoons.HasDeadBranch = DeadBranchBoon;
        QuestBoons.HasTorii = ToriiBoon;
        QuestBoons.HasDripper = DripperBoon;
        QuestBoons.HasOrnithopter = OrnithopterBoon;
        QuestBoons.HasReckless = HasReckless;
        QuestBoons.HasScholar = HasScholar;
        QuestBoons.HasHaggler = HasHaggler;
        QuestBoons.HasZen = HasZen;
        QuestBoons.HasSteady = HasSteady;
        QuestBoons.HasSurge = HasSurge;
        QuestBoons.HasBulwark = HasBulwark;
        QuestBoons.HasCombo = HasCombo;
        QuestBoons.HasIndifference = HasIndifference;
        QuestBoons.HasScripture = HasScripture;
        QuestBoons.HasAnthony = HasAnthony;
        QuestBoons.HasBowlbugConstant = HasBowlbugConstant;
    }

    /// <summary>
    /// **联机自检日志**（2026-10-08 加）：把「校验和里会算到的那几项」打出来。
    ///
    /// 为什么要有这个：`ChecksumTracker` 只会在**对不上之后**报一句
    /// 「State divergence detected!」（然后把客机踢回主菜单），
    /// 它**不会告诉你**是哪一项先分叉的。而这个自检行把两边都打一遍 ——
    /// 拿同一个 `Owner?.NetId` 在两台的日志里各搜一次，逐位比一下就知道是谁先跑偏：
    ///
    ///   · `choiceIds` / `rewardIds` —— 两个**计数器**，两台必须逐位相同
    ///     （来源：`PlayerChoiceSynchronizer.ChoiceIds` / `RewardsSetSynchronizer.GetNextRewardIds()`）；
    ///   · `due` / `paid` / `rewarded` / `setups` —— 这本册子的 `[SavedProperty]` 账本，
    ///     会通过 `SerializableRelic.Props` 进校验和；
    ///   · `giftSeq` —— 抽奖励内容的序号（种子靠它推，跑偏了奖励列表就不一样）。
    ///
    /// 只在多人局打（单人局直接返回），一次进房间一本册子一行，开销可以忽略。
    /// </summary>
    private void LogNetCounters(string where)
    {
        if (QuestRunner.PlayerCount <= 1) return;
        try
        {
            string choice, reward;
            try
            {
                var cs = RunManager.Instance.PlayerChoiceSynchronizer;
                choice = cs == null ? "null" : string.Join(",", cs.ChoiceIds);
            }
            catch (System.Exception e) { choice = "err:" + e.Message; }

            try
            {
                var rs = RunManager.Instance.RewardsSetSynchronizer;
                reward = rs == null ? "null" : string.Join(",", rs.GetNextRewardIds());
            }
            catch (System.Exception e) { reward = "err:" + e.Message; }

            Entry.Logger.Info($"[联机自检/{where}] 册主={Owner?.NetId} 我是本人={IsLocalOwner}"
                              + $" choiceIds=[{choice}] rewardIds=[{reward}]"
                              + $" due=[{string.Join(",", _dueKinds)}]"
                              + $" paid=[{string.Join(",", _paidKinds)}]"
                              + $" rewarded=[{string.Join(",", _rewardedKinds)}]"
                              + $" setups=[{string.Join(",", _setupDone)}]"
                              + $" giftSeq={_giftSeq}");
        }
        catch (System.Exception e) { Entry.Logger.Warn($"联机自检日志失败（不影响玩法）: {e.Message}"); }
    }

    // —— 结算辅助 ——
    // 所有进度都从这里进 QuestRunner，所以多人局守卫放在这一层就够了（见 IsLocalOwner）。
    private async Task Grant(QuestKind kind, int amount)
    {
        // ⚠️ 这里**不拦**「非本地」：每本任务册都推进自己的进度（每个客户端对所有玩家都算一遍），
        // 这样各客户端对「谁有多少进度」的看法一致。奖励在 Reward 里才拦（只由本人那台发）。
        foreach (var q in _state.AddProgress(kind, amount))
            await Reward(q);
    }

    private async Task Set(QuestKind kind, int value)
    {
        foreach (var q in _state.SetProgress(kind, value))
            await Reward(q);
    }

    // 战斗中途拿到的任务奖励先排队 —— 奖励界面在战斗里弹不出来，而且**战斗中直接改状态会不同步**。
    private readonly Queue<QuestState> _pendingRewards = new();

    // ⚠️ 战斗中拿到的遗物**不能立刻发**：游戏禁止在战斗中同步遗物事件
    //    （日志原话：Tried to sync relic event ... during combat! This is not allowed），
    //    硬发会让两边状态不一致 → 判定数据不同步 → 直接被踢回主菜单。所以先排队，出了战斗再发。
    private readonly List<RelicModel> _pendingRelics = new();

    /// <summary>
    /// **防重复**：这一局里已经**发过奖励**的任务种类（放奖入口的幂等闸）。
    ///
    /// 2026-09-27 玩家实测「奖励发重叠了」（一次战斗里刷出两个一模一样的奖励按钮）。
    /// 根因是同一个任务的奖励被发放了两次（`_batch` 里进了两份）。
    /// 不再去追"到底是谁触发了两次"——直接在这里按任务种类卡死：**一条任务只准发一次**。
    ///
    /// 和 `_paidKinds`（已确认送到玩家手上）的区别：
    ///   `_rewardedKinds` = 已经**开始发**过了（发放路径的入口闸）；
    ///   `_paidKinds`     = 已经**确认送到**了（对账用）。
    /// 进存档，读档后也不会重发。
    /// </summary>
    [SavedProperty]
    public int[] RewardedKinds
    {
        get => _rewardedKinds.Select(k => (int)k).ToArray();
        set
        {
            _rewardedKinds.Clear();
            if (value == null) return;
            foreach (var raw in value) _rewardedKinds.Add((QuestKind)raw);
        }
    }

    private readonly HashSet<QuestKind> _rewardedKinds = new();

    /// <summary>
    /// 已经做过「接取时改动」的任务种类（`CursedBargain` / `DeckOverhaul`）。
    /// **必须进存档**：不然读档会再塞一次诅咒 / 再改一次牌组。
    /// </summary>
    [SavedProperty]
    public int[] QuestSetupDone
    {
        get => _setupDone.Select(k => (int)k).ToArray();
        set
        {
            _setupDone.Clear();
            if (value == null) return;
            foreach (var raw in value) _setupDone.Add((QuestKind)raw);
        }
    }

    private readonly HashSet<QuestKind> _setupDone = new();

    /// <summary>
    /// 接取时改牌组的两个任务在这里做**一次性设置**。
    /// ⚠️ 各端都会跑：牌组是各端复制的一份，同样的改动各端算出来一样 → 不会不同步。
    /// ⚠️ 随机（选哪 5 张诅咒）用**确定性种子**（NetId + ActFloor），各端选出来一样。
    /// </summary>
    public async Task EnsureQuestSetups()   // public：接取那一刻由 QuestRunner 直接调（别等进房间）
    {
        foreach (var q in _state.Active)
        {
            var kind = q.Def.Kind;
            if (kind is not (QuestKind.CursedBargain or QuestKind.DeckOverhaul or QuestKind.FakeMerchantWin)) continue;
            if (!_setupDone.Add(kind)) continue;   // 只做一次
            try
            {
                if (kind == QuestKind.CursedBargain) await AddRandomCurses(5);
                else if (kind == QuestKind.DeckOverhaul) await OverhaulDeck();
                else await GiveFoulPotion();       // 假商人：接取时给一瓶污浊药水
                Entry.Logger.Info($"任务「{q.Def.Title}」的接取改动已生效。");
            }
            catch (System.Exception e) { Entry.Logger.Warn($"接取改动失败（{kind}）: {e.Message}"); }
        }
    }

    /// <summary>「打赢假商人」接取时给一瓶「污浊药水」（本体里扔给商人可换金币）。</summary>
    private async Task GiveFoulPotion()
    {
        try
        {
            await PotionCmd.TryToProcure<FoulPotion>(Owner);
            Entry.Logger.Info("假商人：已发一瓶污浊药水。");
        }
        catch (System.Exception e) { Entry.Logger.Warn($"发污浊药水失败: {e.Message}"); }
    }

    /// <summary>随机塞 n 张诅咒进卡组（不含 Eternal —— 那类移除不掉）。</summary>
    private async Task AddRandomCurses(int n)
    {
        int seed = unchecked((int)(Owner.NetId ^ (uint)(Owner.RunState?.ActFloor ?? 0)) ^ 0x1D2C3B4A);
        var rng = new System.Random(seed);
        var pool = ModelDb.All.OfType<CardModel>()
            .Where(c => c.Type == CardType.Curse && !c.Keywords.Contains(CardKeyword.Eternal))
            .OrderBy(_ => rng.Next())
            .Take(n)
            .ToList();
        var owner = Owner;
        if (owner == null || owner.RunState == null) return;
        var runState = owner.RunState;
        var cards = new List<CardModel>();
        foreach (var m in pool) cards.Add(runState.CreateCard(m, owner));
        if (cards.Count > 0) await CardPileCmd.Add(cards, PileType.Deck);
    }

    /// <summary>大改造：牌组里的「打击」→ 升级版「飞溅」，「防御」→ 升级版「发现」。</summary>
    private async Task OverhaulDeck()
    {
        var deck = Owner.Deck.Cards;
        var strikes = deck.Where(c => c.Tags.Contains(CardTag.Strike)).ToList();
        var defends = deck.Where(c => c.Tags.Contains(CardTag.Defend)).ToList();
        if (strikes.Count == 0 && defends.Count == 0) return;

        await CardPileCmd.RemoveFromDeck(strikes.Concat(defends).ToList(), showPreview: false);

        var splashModel = ModelDb.Card<Splash>();
        var discoveryModel = ModelDb.Card<Discovery>();
        var add = new List<CardModel>();
        foreach (var _ in strikes)
        {
            var c = Owner.RunState.CreateCard(splashModel, Owner);
            CardCmd.Upgrade(c);
            add.Add(c);
        }
        foreach (var _ in defends)
        {
            var c = Owner.RunState.CreateCard(discoveryModel, Owner);
            CardCmd.Upgrade(c);
            add.Add(c);
        }
        if (add.Count > 0) await CardPileCmd.Add(add, PileType.Deck);
    }

    /// <summary>
    /// <summary>
    /// **所有客户端都要做**的奖励副作用（它们改写战斗数值 / 牌组 → 各端必须一致，否则联机不同步）。
    /// ⚠️ 只放「确定性的」那些；**兜底型**随身物（遗物发不出去才启用）不能放这里
    ///    —— 那取决于本地发放结果，各端不一定一样。
    /// </summary>
    private async Task ApplySharedEffect(QuestKind kind)
    {
        switch (kind)
        {
            case QuestKind.CaptainsLegacy: HasBoat = true; break;
            case QuestKind.CloseCallsBig: HasReckless = true; break;
            case QuestKind.CardsDrawn: HasScholar = true; break;
            case QuestKind.GoldSpent: HasHaggler = true; break;
            case QuestKind.RestSitesBig: HasZen = true; break;
            case QuestKind.FlawlessCombatsBig: HasSteady = true; break;
            case QuestKind.PowersPlayedBig: HasSurge = true; break;
            case QuestKind.SelfDamageBig: HasBulwark = true; break;
            case QuestKind.AttacksPlayedBig: HasCombo = true; break;
            case QuestKind.CosmicIndifferencePlayed: HasIndifference = true; break;
            case QuestKind.DebuffLayersGiven: HasScripture = true; break;
            case QuestKind.UpgradedDeck: HasAnthony = true; break;
            case QuestKind.BowlbugSwarm: HasBowlbugConstant = true; break;
            case QuestKind.CursedBargain: await RemoveAllCurses(); break;   // 奖励里"清除所有诅咒"也要各端一致
        }
    }

    /// <summary>清除牌组里所有可移除的诅咒（Eternal 那类游戏不允许移除，跳过）。</summary>
    private async Task RemoveAllCurses()
    {
        var curses = Owner.Deck.Cards
            .Where(c => c.Type == CardType.Curse && !c.Keywords.Contains(CardKeyword.Eternal))
            .ToList();
        if (curses.Count > 0) await CardPileCmd.RemoveFromDeck(curses, showPreview: false);
    }

    /// 发奖。public 是为了让 Entry 能把它注册成 QuestRunner.RewardSink（测试按钮要用）。
    /// </summary>
    public async Task Reward(QuestState q)
    {
        // ⚠️⚠️ 随身物是**确定性**的：每台客户端都会为每位玩家算进度、判定也一致 →
        //    **所有客户端都点亮同一批随身物**，各端战斗数值才不会分叉（联机里同样生效）。
        //    （2026-10-01 玩家要求「每个效果多人局也生效」。以前只在本人那台开 → 各端数值对不上。）
        await ApplySharedEffect(q.Def.Kind);

        // ⚠️⚠️⚠️ 联机（2026-10-08 修）：**整条奖励流程各端都要跑**，不能只跑本人那台。
        //
        // 为什么（反编译实证）：
        //   游戏的校验和 `ChecksumTracker.GenerateChecksum` = XxHash32(序列化后的 `NetFullCombatState`)，
        //   而 `NetFullCombatState.FromRun` 把下面这些**全都算进去**：
        //     ① `RewardsSetSynchronizer.GetNextRewardIds()` —— 每位玩家的「奖励集编号」计数器
        //     ② `PlayerChoiceSynchronizer.ChoiceIds`     —— 每位玩家的「选择编号」计数器
        //     ③ 每位玩家遗物的 `[SavedProperty]`（`SerializableRelic.Props`）——
        //        也就是这本册子的 QuestData / PendingRewardKinds / PaidRewardKinds / …
        //     ④ 每位玩家的 `PlayerRng`（`RelicReward` 之类会消耗它）
        //   主客机只要有一项对不上 → `CompareChecksums` 判「State divergence detected!」→
        //   `DisconnectClient(senderId, NetError.StateDivergence)` 把客机**踢回主菜单**。
        //
        //   而游戏的奖励是**按「奖励集编号 + 下标」在各端复现**的：
        //   `RewardSelectedMessage` 只带 setId 和 rewardIndex，接收端拿 `set.Rewards[index]` 去应用。
        //   本体就是这么用的 —— 见 `HealRestSiteOption.ExecuteRestSiteHeal`：它调 `RewardsCmd.OfferCustom`，
        //   而 `RestSiteSynchronizer` 把「选了哪个篝火选项」广播给所有端、各端都执行一遍
        //   （所以 `BeginRewardsSet` 会在各端为同一位玩家依次发生，编号才对得上）。
        //
        //   反过来，「只有本人那台建奖励集」会同时踩三颗雷：
        //     · 本人那台的 nextRewardIds 比别人多 1 → 校验和不匹配 → 掉线；
        //     · 别人那台收到 `RewardSelectedMessage` 时 `nextId <= setId` → 当成「奖励集还没建」**永久缓冲**，
        //       奖励在本机根本不生效（金币/卡牌/遗物不同步）；
        //     · `RelicReward.Populate()` 消耗的是**共享**的 PlayerRng，只有一台消耗 → Rng 也分叉。
        //
        // 正确形状（本函数现在这样）：**各端都算奖励、各端都建奖励集、各端都记账**；
        //   只有「真正改玩家状态」的那几行才限定本人那台（见 GainGoldSynced / OfferRelic /
        //   下面两个 `CardPileCmd.Add` 分支），改完用 `SyncLocalObtained*` 通知别人。
        //   奖励界面本身只有本人那台会弹（`RewardsSet.Offer()` 里判 `LocalContext.IsMe`），
        //   别人那台只是 `await` 同一个奖励集走完，不弹任何东西。
        //
        // ⚠️ 幂等由 `GrantRewardNow` 把关（补发路径也走那里，闸必须放在同一处）。
        // ⚠️ 先记「这笔奖励还欠着」（会进存档）。
        //    **只有当场就交到玩家手上的那条**才会在这里销账；排队等会儿再发的那批，
        //    要等真正发出去（`FlushBatchAsync`）才销账 —— 否则中途退出/读档就白记了。
        MarkRewardDue(q.Def.Kind);
        try
        {
            // 奖励不再"攒在自己这儿等"，直接交给**官方那套战后奖励**（见 FlushBatchAsync）：
            // 战斗中完成 → 并进本场战斗的 ExtraRewards → 打完架在官方「搜刮」界面里和金币/卡牌一起出现；
            // 不在战斗里（商店 / 事件 / 篝火）→ 走游戏奖励流程，一次性弹一个界面。
            bool queued = await GrantRewardNow(q);
            if (queued) ScheduleFlush();
            else { ClearDue(q.Def.Kind); _paidKinds.Add(q.Def.Kind); }   // 当场给了：销账 + 记「已送到」
        }
        catch (System.Exception e)
        {
            // 没发成功就把「欠着」的账留着（存进存档），读档/下次进房间会补发。
            Entry.Logger.Error($"发奖励失败（{q.Def.Title}），已记为待补发: {e}");
        }

        // 联机自检：发完一笔奖励 —— 这里最该看的是 rewardIds（建了几个奖励集）和 paid/rewarded。
        LogNetCounters("发奖励:" + q.Def.Kind);
    }

    /// <summary>
    /// **对账**：把这个册子里「已经判定完成、但账上还欠着奖励」的任务全部揪出来。
    ///
    /// 2026-09-27 玩家要的"检测"就是这个：与其依赖某一个触发点（战斗结束 / 进房间）去发奖励，
    /// 不如每次进房间都**对一遍账** —— 完成了却没发过的，一律补发。
    /// 这样就算中间任何一环被打断（切房间顶掉、读档、异常、战斗结算打架），
    /// 奖励都会在**下一次进房间**自己冒出来，不会再出现"任务完成但什么都没有"。
    /// </summary>
    private void ReconcileOwedRewards()
    {
        try
        {
            var completed = _state.CompletedKinds;
            if (completed.Count == 0) return;

            var missing = new List<QuestKind>();
            foreach (var kind in completed)
                if (!_paidKinds.Contains(kind) && !_dueKinds.Contains(kind))
                    missing.Add(kind);

            if (missing.Count == 0) return;   // 账对得上，什么都不用做

            foreach (var kind in missing) _dueKinds.Add(kind);
            Entry.Logger.Warn($"对账：发现 {missing.Count} 个任务已完成但奖励没发过，已记入待发："
                              + string.Join("、", missing));
        }
        catch (System.Exception e)
        {
            Entry.Logger.Error($"对账失败: {e}");
        }
    }

    /// <summary>把战斗中攒下的奖励补发掉。</summary>
    private async Task FlushPendingRewards()
    {
        // ① 先对账：完成了但没发过奖励的，全部记进"欠着"的账。
        ReconcileOwedRewards();
        // ② 把欠着的账排进待发队列。
        RescheduleRestoredRewards();

        // 先把战斗中排队没发的遗物发掉（此刻已经出了战斗，同步是允许的）
        if (_pendingRelics.Count > 0)
        {
            var relics = _pendingRelics.ToList();
            _pendingRelics.Clear();
            // ⚠️ 联机：队列各端都会填、也各端都清（保证 `_pendingRelics` 不会一边有一边没有），
            //    但**真正发遗物只有本人那台**（发完 SyncRelic 通知别人，别人那边才会加上这件遗物）。
            if (!IsLocalOwner) relics.Clear();
            foreach (var r in relics)
            {
                try
                {
                    await RelicCmd.Obtain(r, Owner);
                    SyncRelic(r);
                    Entry.Logger.Info("补发了一件排队中的遗物（战斗已结束）。");
                }
                catch (System.Exception e) { Entry.Logger.Error($"补发遗物失败: {e}"); _pendingRelics.Add(r); }
            }
        }

        while (_pendingRewards.Count > 0)
        {
            var q = _pendingRewards.Dequeue();
            try
            {
                bool queued = await GrantRewardNow(q);
                // 欠账补发：**当场到手**的记「已送到」并销账；只排进待发队列的等下面 FlushBatchAsync 结算。
                if (!queued) { ClearDue(q.Def.Kind); _paidKinds.Add(q.Def.Kind); }
            }
            catch (System.Exception e) { Entry.Logger.Error($"补发奖励失败（{q.Def.Title}）: {e}"); }
        }
        // ⚠️ 这里必须**直接**把队列交出去（`FlushBatchAsync`），不能再走 `ScheduleFlush()`
        //   —— `ScheduleFlush` 在"战斗流程还没完全结束"时会直接返回，补发的奖励就会被无限期押着。
        //   进房间已经是不在战斗里的安全时机了。
        if (_batch.Count > 0) await FlushBatchAsync();
    }

    /// <summary>
    /// 真正发这一条任务的奖励。
    /// 返回 **true = 只是排队了（晚点才出去，账先别销）**，false = 当场就交到玩家手上（可以销账）。
    ///
    /// ⚠️ **幂等闸在这里**（不在 `Reward()` 里）。原因：补发路径
    /// （`RescheduleRestoredRewards` → `_pendingRewards` → 这里）**不经过 `Reward()`**，
    /// 闸放在 `Reward()` 里拦不住它 —— 2026-09-27 日志实测「补发又把三条任务重发了一遍」。
    /// 所以闸必须放在**真正产生奖励副作用**的这一个入口。
    /// </summary>
    private async Task<bool> GrantRewardNow(QuestState q)
    {
        // ⚠️ 幂等闸：同一条任务的奖励**只准发一次**（不管从哪条路进来的）。
        // ⚠️⚠️ 但**账上还欠着**（`_dueKinds`）的必须放行：「开始发过」≠「送到了」——
        //    奖励可能还在内存待发队列里（`_batch` 不进存档），玩家一 SL 就没了；
        //    这时对账会把它重新排进来，闸再拦 = **永久吞掉**
        //    （2026-10-01 玩家实测：问号房 SL 后奖励消失，日志 `补发…` 紧跟着 `已经发过了，跳过`）。
        bool owed = _dueKinds.Contains(q.Def.Kind);
        if (!owed && !_rewardedKinds.Add(q.Def.Kind))
        {
            Entry.Logger.Warn($"任务「{q.Def.Title}」的奖励已经发过了，跳过（防重复）。");
            return false;
        }
        _rewardedKinds.Add(q.Def.Kind);

        int batchBefore = _batch.Count;
        switch (q.Def.Kind)
        {
            // —— 船长的执念：三件零件拼成一艘船 ——
            case QuestKind.CaptainsLegacy:
                HasBoat = true;
                Announce(q, "锚、角夹、船长之轮拼成了一艘帆船："
                            + "战斗开始 +15 格挡，本场第一次格挡被清空时再 +20 格挡");
                break;

            // —— 枯枝还魂：发**我们自己造的**枯木树枝，不依赖别的 mod ——
            case QuestKind.CardsExhaustedBig:
                // ⚠️ 兜底是**空操作**：这里绝不能写 `HasDeadBranch = true`（那会进校验和 → 各端分叉）。
                //    真正的兜底效果由 `DeadBranchBoon`（派生）在读取时判定，见属性区那段说明。
                Announce(q, await GrantOwnRelic<QuestDeadBranchRelic>(
                    "枯木树枝", () => { }));
                break;

            // —— 一代遗物：优先发我们自研的那几件（谁都能拿到），没有自研版的才去别的 mod 里找 ——
            case QuestKind.BlockGainedBig:
                // 兜底空操作，理由同上面的枯木树枝（效果改由 `ToriiBoon` 派生判定）。
                Announce(q, await GrantOwnRelic<QuestToriiRelic>(
                    "鸟居", () => { }));
                break;

            case QuestKind.SmithNoRest:
                // 兜底空操作，理由同上面的枯木树枝（效果改由 `DripperBoon` 派生判定）。
                Announce(q, await GrantOwnRelic<QuestDripperRelic>(
                    "咖啡滤杯", () => { }));
                break;

            case QuestKind.PotionsUsedBig:
                // 兜底空操作，理由同上面的枯木树枝（效果改由 `OrnithopterBoon` 派生判定）。
                Announce(q, await GrantOwnRelic<QuestOrnithopterRelic>(
                    "玩具扑翼飞机", () => { }));
                break;

            case QuestKind.GoldHoardBig:
                Announce(q, await GrantOwnRelicOrGold<QuestGoldenIdolRelic>("金偶像", 200));
                break;

            case QuestKind.TreasureRooms:
                Announce(q, await GrantLegacyRelic("Matryoshka", "套娃", async () =>
                {
                    await GainGoldSynced(150);
                    return "获得 150 金币";
                }));
                break;

            case QuestKind.CurseFreeCombats:
                Announce(q, await GrantOwnRelicOrGold<QuestOmamoriRelic>("御守", 150));
                break;

            // —— 鸡煲三部曲 ——
            case QuestKind.SlowStartWin:
                Announce(q, await GrantLegacyRelic("Inserter", "机械臂", async () =>
                {
                    await GainGoldSynced(200);
                    return "获得 200 金币";
                }));
                break;

            case QuestKind.EchoFormTurn:
                Announce(q, await GrantOwnRelicOrGold<QuestClockworkRelic>("齿轮工艺品", 180));
                break;

            case QuestKind.ActOneEliteWin:
                Announce(q, await GrantLegacyRelic("MutagenicStrength", "突变酵素", async () =>
                {
                    await GainGoldSynced(180);
                    return "获得 180 金币";
                }));
                break;

            // —— 借《海克斯符文》的两个手法 ——
            // ① 升级型奖励：不发东西，直接强化你已有的牌
            case QuestKind.CardsPlayedBig:
                Announce(q, await UpgradeRandomCards(2));
                break;

            // ② 代价换强度：强力，但有明确的代价
            case QuestKind.CloseCallsBig:
                HasReckless = true;
                Announce(q, "获得「莽夫之道」：打出的攻击牌伤害 +50%，但你的技能牌格挡减半");
                break;

            // —— 每个奖励对着一个不同的游戏旋钮 ——
            case QuestKind.CardsDrawn:
                HasScholar = true;
                Announce(q, "获得「手不释卷」：每回合多抽 1 张牌");
                break;

            case QuestKind.GoldSpent:
                HasHaggler = true;
                Announce(q, "获得「砍价高手」：商店价格 -30%");
                break;

            case QuestKind.RestSitesBig:
                HasZen = true;
                Announce(q, "获得「养生」：篝火休息的回复量 +50%");
                break;

            case QuestKind.FlawlessCombatsBig:
                HasSteady = true;
                Announce(q, "获得「稳如老狗」：能量上限 +1");
                break;

            case QuestKind.PowersPlayedBig:
                HasSurge = true;
                Announce(q, "获得「厚积薄发」：你施加的增益与减益都 +1 层");
                break;

            case QuestKind.SelfDamageBig:
                HasBulwark = true;
                Announce(q, "获得「铁壁」：你单次受到的伤害不超过 15 点");
                break;

            case QuestKind.AttacksPlayedBig:
                HasCombo = true;
                Announce(q, "获得「连环拳」：你的攻击牌多打一段");
                break;

            // —— 魔兽世界：附魔 —— 给牌组里选一张牌挂「保留」词条（游戏自带的词条系统）
            case QuestKind.WoWEnchant:
                _batch.Add(new KeywordEnchantReward(Owner, CardKeyword.Retain, "保留"));
                Announce(q, "附魔奖励已加入奖励界面：选一张牌挂上「保留」");
                break;

            // —— 我的世界：挖矿 —— 随机一件我们自己的遗物
            case QuestKind.MinecraftDig:
                AddRandomRelicReward(q);
                break;

            // —— 宝可梦：进化 —— 选一张牌升级（BaseLib 的零件）
            case QuestKind.PokemonEvolve:
                // 进化 = 把一张牌**变成**另一张（BaseLib 的变牌奖励）
                _batch.Add(new BaseLib.Common.Rewards.CardTransformReward(Owner) { MaxCards = 1, Upgrade = false });
                Announce(q, "把一张牌变成另一张");
                break;

            // —— 跨界 第四批 ——
            case QuestKind.TerrariaShieldWin:
                _batch.Add(new KeywordEnchantReward(Owner, CardKeyword.Retain, "保留"));
                Announce(q, "附魔奖励已加入奖励界面：选一张牌挂上「保留」");
                break;
            case QuestKind.DontStarveEndure:
                await GainGoldSynced(200);
                Announce(q, "金币 +200");
                break;
            case QuestKind.DiabloFarming:
                AddRandomRelicReward(q);
                break;
            case QuestKind.GenshinWish:
                await GainGoldSynced(150);
                Announce(q, "金币 +150");
                break;
            case QuestKind.ArknightsDeploy:
                _batch.Add(new KeywordEnchantReward(Owner, CardKeyword.Innate, "固有"));
                Announce(q, "附魔奖励已加入奖励界面：选一张牌挂上「固有」");
                break;
            case QuestKind.PokemonAllEvolve:
                // 顺着"升级路线"给：再送两次升级（变牌会把升级抹掉，不能这么配）
                _batch.Add(new BaseLib.Common.Rewards.CardUpgradeReward(Owner) { Amount = 2 });
                Announce(q, "再选 2 张牌升级");
                break;

            // —— 我的世界：纯净卡组 —— 随机一件遗物
            case QuestKind.DeckNoDuplicates:
                // 已经把起始牌清理干净了 → 奖励"变牌"，把剩下那张也换掉（承接条件，不抹掉成果）
                _batch.Add(new BaseLib.Common.Rewards.CardTransformReward(Owner) { MaxCards = 1, Upgrade = true });
                Announce(q, "把一张牌变成另一张（会保留升级）");
                break;

            // —— 文明6：精打细算 —— 金币 +250
            case QuestKind.OnlyOneCostWin:
                await GainGoldSynced(250);
                Announce(q, "金币 +250");
                break;

            // —— 只狼：弹反 —— 条件是把防守做到极致，奖励就给你"强化一张牌"（BaseLib 的选牌升级奖励）
            case QuestKind.ParryFlawless:
            {
                _batch.Add(new BaseLib.Common.Rewards.CardUpgradeReward(Owner) { Amount = 1 });
                Announce(q, "把一张牌升级");
                break;
            }

            // —— 地狱狂徒（战士专属）：发一张真卡「地狱狂徒」——
            // 它本身是「抽到带『打击』标签的牌时自动打出」的能力牌，配厚打击牌组刚好。
            case QuestKind.StrikeDeckHellraiser:
            {
                // ⚠️ 联机：改卡组只有本人那台做（加完 SyncLocalObtainedCard 通知别人）。
                if (IsLocalOwner)
                {
                    var hellModel = ModelDb.Card<Hellraiser>();
                    var hellCard = Owner.RunState.CreateCard(hellModel, Owner);
                    var added = await CardPileCmd.Add(hellCard, PileType.Deck);
                    CardCmd.PreviewCardPileAdd(added, 1.6f);
                    try { RunManager.Instance.RewardSynchronizer?.SyncLocalObtainedCard(hellCard); }
                    catch (System.Exception ex) { Entry.Logger.Warn($"SyncLocalObtainedCard 失败: {ex.Message}"); }
                }
                Announce(q, "获得一张「地狱狂徒」（抽到带「打击」的牌会自动打出）");
                break;
            }

            // —— 堆叠宝石：单回合把同一张牌打 8 次的回报，发一张「未掘宝石」(HIDDEN_GEM) ——
            // 它是本体里唯一带「宝石」的卡（稀有）：给一张牌叠「重放」，让同一张牌多打几遍。
            case QuestKind.SameCardManyTimes:
            {
                // ⚠️ 联机：改卡组只有本人那台做（加完 SyncLocalObtainedCard 通知别人）。
                if (IsLocalOwner)
                {
                    var gemModel = ModelDb.Card<HiddenGem>();
                    var gemCard = Owner.RunState.CreateCard(gemModel, Owner);
                    var gemAdded = await CardPileCmd.Add(gemCard, PileType.Deck);
                    CardCmd.PreviewCardPileAdd(gemAdded, 1.6f);
                    try { RunManager.Instance.RewardSynchronizer?.SyncLocalObtainedCard(gemCard); }
                    catch (System.Exception ex) { Entry.Logger.Warn($"SyncLocalObtainedCard 失败: {ex.Message}"); }
                }
                Announce(q, "获得一张「未掘宝石」（给一张牌叠上「重放」）");
                break;
            }

            case QuestKind.CosmicIndifferencePlayed:
                HasIndifference = true;
                Announce(q, "获得「冷漠」：每当你获得格挡，额外获得 3 点格挡");
                break;

            // —— 蛇花圣经（梗）：把「给敌人叠减益」变成一件长期生效的事 ——
            case QuestKind.DebuffLayersGiven:
                HasScripture = true;
                Announce(q, "获得「蛇花圣经」：你给敌人施加的减益 ×1.5");
                break;

            // —— 打赢假商人（梗）——
            // ⚠️ 原来给「从它那儿抢一件假货」，但**打赢假商人本身就是给所有假货的**（本体行为）
            //    → 这个奖励等于没有。改成发一件随机遗物。
            case QuestKind.FakeMerchantWin:
                AddRandomRelicReward(q);
                break;

            // —— 东尼意思（梗）：打不过就加强 —— 卡牌奖励更容易是升级版 ——
            case QuestKind.UpgradedDeck:
                HasAnthony = true;
                Announce(q, "获得「东尼意思」：卡牌奖励更容易出现升级版（约 3 倍）");
                break;

            // —— 展望未来：本体真的有的「化学X」+ 一张 X 费牌 ——
            case QuestKind.ShopXDeal:
                string relicPart;
                if (Owner.GetRelic<ChemicalX>() == null && await OfferRelic(ModelDb.Relic<ChemicalX>().ToMutable()))
                {
                    // 走游戏奖励流程（自带多人同步），玩家在奖励界面里领
                    relicPart = "获得遗物「化学X」（X 费牌的 X 值 +2）";
                }
                else if (Owner.GetRelic<ChemicalX>() == null)
                {
                    // 兜底：奖励流程没走通就直接给（⚠️ 联机：只有本人那台真发 + 通知别人）
                    if (IsLocalOwner)
                    {
                        var chemX = await RelicCmd.Obtain<ChemicalX>(Owner);
                        SyncRelic(chemX);
                    }
                    relicPart = "获得遗物「化学X」（X 费牌的 X 值 +2）";
                }
                else
                {
                    await GainGoldSynced(120);
                    relicPart = "你已经有一个化学X了，折现 120 金币";
                }
                Announce(q, $"{relicPart}；{GrantRandomXCard()}");
                break;

            // —— 盛碗虫泛滥：随身物「盛碗虫常数」（社区梗：一堆牌恰好 15 格挡，挡不住 16 的头槌）——
            case QuestKind.BowlbugSwarm:
                HasBowlbugConstant = true;
                Announce(q, "获得「盛碗虫常数」：每当你获得格挡，额外 +1（正好把 15 补成 16）");
                break;

            // —— 怪潮 / 精英加餐：奖励是**遗物**（不是金币）——
            case QuestKind.MonsterRush:
                AddRandomRelicReward(q);
                break;

            case QuestKind.EliteAmbush:
                Announce(q, "奖励：一件随机遗物 + 150 金币");
                _batch.Add(new RelicReward(Owner));
                await GainGoldSynced(150);
                break;

            // —— 诅咒交易 / 大改造：奖励（诅咒的清除走 ApplySharedEffect，各端一致）——
            case QuestKind.CursedBargain:
                await GainGoldSynced(500);
                Announce(q, "金币 +500（牌组里的诅咒已清除）");
                break;

            case QuestKind.DeckOverhaul:
                AddRandomRelicReward(q);
                break;

            // —— 血之契约：一件强力随机遗物 + 150 金币（撑过敌人强化后的回报）——
            case QuestKind.ContractChallenge:
                AddRandomRelicReward(q);
                await GainGoldSynced(150);
                break;

            default:
                await GrantByRewardKind(q);
                break;
        }

        // 「欠着」的账还在，说明这一条是**排队**等会儿发的（战斗中的金币、进了 `_batch` 的那些）；
        // 账已销掉就是当场给了玩家。调用方靠这个决定要不要继续押着。
        // 返回「这一笔是不是只排进了待发队列（`_batch`）」。
        // ⚠️ 别再用 `_dueKinds.Contains`：补发时那个永远为真，会把「当场到手」的奖励也当成排队、账销不掉。
        return _batch.Count > batchBefore;
    }

    /// <summary>按 <see cref="QuestDef.Reward"/> 发奖励（金币以外的多样化奖励）。</summary>
    private async Task GrantByRewardKind(QuestState q)
    {
        switch (q.Def.Reward)
        {
            case RewardKind.Relic:
                AddRandomRelicReward(q);
                break;

            case RewardKind.CardSkill:
                AddCardReward(q, c => c.Type == CardType.Skill, "一张技能牌（三选一）");
                break;
            case RewardKind.CardPower:
                AddCardReward(q, c => c.Type == CardType.Power, "一张能力牌（三选一）");
                break;
            case RewardKind.CardRare:
                AddCardReward(q, c => c.Rarity == CardRarity.Rare, "一张稀有牌（三选一）");
                break;
            case RewardKind.CardOtherClass:
                AddCardReward(q, c => !IsOwnerClassCard(c), "一张其他职业的牌（三选一）");
                break;
            case RewardKind.CardAny:
                AddCardReward(q, _ => true, "一张牌（三选一）");
                break;

            case RewardKind.Upgrade:
                Announce(q, await UpgradeRandomCards(2));
                break;
            case RewardKind.Transform:
                _batch.Add(new BaseLib.Common.Rewards.CardTransformReward(Owner) { MaxCards = 1, Upgrade = false });
                Announce(q, "把一张牌变成另一张");
                break;

            case RewardKind.CardNamed:
                await GrantNamedCard(q);
                break;

            case RewardKind.Enchant:
                AddEnchantReward(q);
                break;

            // 批之十新加的奖励类型：**从牌组里删掉一张牌**（本体自带的删牌奖励，和商店花钱删牌同一个界面）。
            case RewardKind.RemoveCard:
                // ⚠️ 联机不发：删牌走的是本体的 `DoUnsyncedCardRemoval`（没有同步手段），
                //    各端删的牌不一样就会判数据不同步 —— 和「升级牌组」类奖励同一条政策，改发金币。
                if (QuestRunner.PlayerCount > 1)
                {
                    await GainGoldSynced(150);
                    Announce(q, "联机模式下不删牌，折现 150 金币");
                }
                else
                {
                    _batch.Add(new CardRemovalReward(Owner!));
                    Announce(q, "奖励已加入奖励界面：从牌组里删掉一张牌");
                }
                break;

            default:   // Gold
                await GainGoldSynced(q.Def.RewardGold);
                Announce(q, $"金币 +{q.Def.RewardGold}");
                break;
        }
    }

    /// <summary>把一张"筛过的卡牌奖励（三选一）"塞进这批任务奖励，和金币/遗物同一个界面弹。</summary>
    private void AddCardReward(QuestState q, Func<CardModel, bool> filter, string what)
    {
        var reward = CardGift.BuildCardReward(Owner, filter, seed: CardGift.SeedFor(Owner, ++_giftSeq));
        if (reward == null)
        {
            _ = GainGoldSynced(150);
            Announce(q, "没找到合适的牌，折现 150 金币");
            return;
        }
        _batch.Add(reward);
        Announce(q, $"奖励已加入：{what}");
    }

    /// <summary>
    /// 发一张**指定的**核心牌（`QuestDef.RewardCard` 存的是模型 Id，例如 "DEMON_FORM"）。
    /// 直接进卡组（和「地狱狂徒」同一条路：CreateCard + CardPileCmd.Add + SyncLocalObtainedCard）。
    /// </summary>
    private async Task GrantNamedCard(QuestState q)
    {
        var entry = q.Def.RewardCard;
        CardModel? model = null;
        if (!string.IsNullOrEmpty(entry))
            foreach (var m in ModelDb.All)
                if (m is CardModel c && string.Equals(c.Id.Entry, entry, System.StringComparison.OrdinalIgnoreCase))
                {
                    model = c;
                    break;
                }

        if (model == null)
        {
            Announce(q, $"找不到牌「{entry}」，折现 150 金币");
            await GainGoldSynced(150);
            return;
        }

        var card = Owner.RunState.CreateCard(model, Owner);
        var added = await CardPileCmd.Add(card, PileType.Deck);
        CardCmd.PreviewCardPileAdd(added, 1.6f);
        try { RunManager.Instance.RewardSynchronizer?.SyncLocalObtainedCard(card); }
        catch (System.Exception ex) { Entry.Logger.Warn($"SyncLocalObtainedCard 失败: {ex.Message}"); }
        Announce(q, $"获得一张核心牌「{model.Title ?? entry}」");
    }

    /// <summary>「给一张牌附魔」奖励：随机挑一个**本体自带的**附魔（排除废弃/测试用的），做成奖励。</summary>
    private void AddEnchantReward(QuestState q)
    {
        var enchant = PickRandomEnchant();
        if (enchant == null)
        {
            _ = GainGoldSynced(150);
            Announce(q, "找不到附魔，折现 150 金币");
            return;
        }
        _batch.Add(new EnchantReward(Owner, enchant));
        Announce(q, $"奖励已加入：给一张牌附魔（{enchant.Title.GetFormattedText()}）");
    }

    private static EnchantmentModel? PickRandomEnchant()
    {
        var list = ModelDb.All.OfType<EnchantmentModel>()
            .Where(e => e.GetType().Name is not ("DeprecatedEnchantment" or "MockFreeEnchantment"))
            .ToList();
        return list.Count == 0 ? null : list[Random.Shared.Next(list.Count)];
    }

    /// <summary>
    /// 按**当前幕**挑一只精英怪（只挑**单体精英** —— 多段怪比如蜈蚣段单独召会出问题）。
    /// 用 `seed`（ActFloor 推出）保证各端选到同一只。取不到就返回 null（调用方兜底）。
    /// </summary>
    private MonsterModel? PickActElite(int seed)
    {
        try
        {
            var act = Owner.RunState?.Act;
            if (act == null) return null;
            var pool = act.AllEliteEncounters
                .Where(e => e.AllPossibleMonsters.Count() == 1)
                .SelectMany(e => e.AllPossibleMonsters)
                .Distinct()
                .Where(m => !IsUnsafeElite(m))
                .ToList();
            return pool.Count == 0 ? null : pool[new System.Random(seed + 777).Next(pool.Count)];
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"取本幕精英池失败: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 不能往**普通战斗**里塞的精英 —— 它们会把战斗**卡死**（2026-10-03 玩家实测 + 日志实锤）。
    ///
    /// 原因：游戏的 `NRunMusicController.TriggerEliteSecondPhase()` 第一行就抛
    /// "You can only trigger the elite transition in an elite room" —— 它**要求当前是精英房**，
    /// 而我们正是把精英塞进普通战斗的，所以一走到那一步就抛异常。异常发生在**敌人回合**里，
    /// 于是 `Combat #1 turn loop died ... the combat is stuck until the room is restarted` —— 战斗卡死。
    ///
    /// 反编译核实：**全游戏只有 2 处**会调到 TriggerEliteSecondPhase，就是下面这两只：
    ///   - BygoneEffigy：它的 WakeMove（开局"苏醒"那一下）。
    ///   - InfestedPrism：它的 InfestedPower 在**死亡时**才触发（更阴，前面几回合看不出来）。
    ///
    /// 以后游戏加了新精英，先搜一遍 `TriggerEliteSecondPhase` 的调用点，有新的就加到这里。
    /// </summary>
    private static bool IsUnsafeElite(MonsterModel m)
    {
        string id = m?.Id.Entry ?? "";
        return id.Contains("BYGONE_EFFIGY", StringComparison.OrdinalIgnoreCase)
            || id.Contains("INFESTED_PRISM", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>这张牌是不是「自己职业」的（用于「其他职业的牌」奖励的筛选）。</summary>
    private bool IsOwnerClassCard(CardModel c)
    {
        try { return c.VisualCardPool?.Id.Entry == Owner.Character?.CardPool?.Id.Entry; }
        catch (System.Exception) { return false; }
    }

    /// <summary>
    /// 发一件**原版随机遗物**：走游戏的 `RelicReward`（`RelicFactory.PullNextRelicFromFront`）。
    /// 它自己防重复，真发完了会给「头环」（`Circlet` / `FallbackRelicPool`）——
    /// 不再用我们那 7 件的小池子（"池子空了折现金币"很出戏，玩家指出）。
    /// </summary>
    private void AddRandomRelicReward(QuestState q)
    {
        _batch.Add(new RelicReward(Owner));
        Announce(q, "奖励已加入：一件随机遗物");
    }

    private void Announce(QuestState q, string detail)
    {
        MirrorBoons(); // 拿到随身物之后刷新面板那一栏
        // ⚠️ 不再弹「任务完成」的提示框（玩家反馈：太烦）。信息只写进日志，奖励照常发。
        Entry.Logger.Info($"Quest completed: {q.Def.Title} -> {detail}");
    }
}
