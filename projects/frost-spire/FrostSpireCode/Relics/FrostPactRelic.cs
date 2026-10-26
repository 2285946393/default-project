using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using FrostSpire.UI;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace FrostSpire.Relics;

/// <summary>
/// 凛冬契约：凛冬模式的载体遗物。开局由 Entry 发给所有玩家。
///
/// M2 v1 —— 单房间守塔循环（前身版，notes/凛冬尖塔-单房间模式设计案.md）：
///   **整局 = 一场被锁 2 扣住的超长战斗**。玩家进的那个房间里的本体遭遇算「第 0 波」，
///   之后每打空一次场地 → 锁 2 扣住战斗 → 下一回合开始 = 间隙回合：发一张补给牌 + 刷波次表的下一波
///   → 直到 14 波全部放完，最后一只怪倒下时战斗才正常结算。
///
///   这样全部踩在已验证的 API 上：锁 2（M1.5 实测）、CreatureCmd.Add 带槽位刷怪（M1.5 实测）、
///   CardFactory+AddGeneratedCardsToCombat 战斗内发牌（quest-spire 枯枝同款）。
///   「同房间重启一场战斗」这条路没人走过，v1 不碰。
///
///   wb 设计案的「开下一波时机」问题 v1 的答案：间隙回合就是玩家自己的回合——
///   想拖就拖、打完牌点结束回合 = 叫下一波。选择权在回合制里天然就有了。
/// </summary>
[RegisterRelic(typeof(FrostRelicPool))]
public sealed partial class FrostPactRelic : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Common;

    // 图标：借本体「灯笼」顶位。有真图后走 pck（三张图铁律：小图 85×85 / 轮廓 / 大图 256×256）。
    public override RelicAssetProfile AssetProfile => ContentAssetProfiles.Relic("lantern");

    /// <summary>历史累计刷过的波数（进存档，日志/统计用，不参与判定）。</summary>
    [SavedProperty]
    public int WavesSpawned { get; set; }

    /// <summary>
    /// 本场战斗计划放出的波数 = 波次表长度。
    ///
    /// ★ 计算属性，**刻意不进存档**（wb M2 审查·问题 A 的修复）：
    ///   波次表长度是编译期常量，没有理由落盘；而落盘反而会坏——
    ///   `BeforeCombatStart` **只在 CombatManager.StartCombatInternal() 里被调用**（全游戏唯一调用点，sts2.dll:600），
    ///   **读档恢复战斗不走这条路** ⇒ 写在里面的赋值不重跑。
    ///   于是「进房间前存档 / 遗物刚拿到」时 Planned 保持 int 默认值 0
    ///   → 锁1/锁2 判定 `0 < 0 = false` → **双双失效，14 波永远不来**。
    ///   改成计算属性后，任何时机读到的都是真值，读档天然正确，无需兜底。
    /// </summary>
    public int SubWavesPlanned => WaveTable.All.Length;

    /// <summary>本场战斗已经放出的波数（进存档；判定唯一依赖的运行时状态）。</summary>
    [SavedProperty]
    public int SubWavesSpawned { get; set; }

    /// <summary>
    /// 锁1：波次表没走完时不许出房间；打完最后一波（Spawned==Planned）自动放行，
    /// 战后奖励照常领，之后地图恢复正常（凛冬「前身局」到此结束）。
    /// </summary>
    public override bool ShouldProceedToNextMapPoint()
        => FrostSpireConfig.LockRoom && SubWavesSpawned < SubWavesPlanned;

    /// <summary>
    /// 锁2：敌人全灭但波次表还没放完 → 官方口子「new enemies are about to spawn」，扣住战斗不结算。
    /// </summary>
    public override bool ShouldStopCombatFromEnding() => SubWavesSpawned < SubWavesPlanned;

    /// <summary>
    /// ★ M3.10：把**本局种子**喂给波次表（懒绑定，重复调用无副作用）。
    ///
    /// 为什么要这个：`WaveTable.For(ulong seed)` 的种子参数写好了，但全项目调用点都用
    /// `WaveTable.All`，而 `All` 原来写死 `For(0)` —— **每一局的 16 波完全一样**，
    /// 「每局波次不同」从来没生效过（和 M3.7 修的难度旋钮是同一类死旋钮）。
    ///
    /// ★ 为什么放在这里懒绑定、而不是只放在 `BeforeCombatStart`：
    ///   `BeforeCombatStart` **只在 `CombatManager.StartCombatInternal()` 里被调用**，
    ///   **读档恢复战斗不走这条路**（M2 就踩过这个坑）⇒ 只在那里绑，读档局会退回种子 0。
    ///   ⇒ 改成"谁要用谁先确保一次"，任何时机进来都正确。
    /// </summary>
    private void EnsureRunSeed()
    {
        if (WaveTable.IsSeedBound) return;
        try
        {
            ulong seed = Owner?.RunState?.Rng?.Seed ?? 0uL;
            WaveTable.BindRunSeed(seed);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"绑定局种子失败（退回默认波次，不影响战斗）: {e.Message}");
            WaveTable.BindRunSeed(0uL);
        }
    }

    /// <summary>开战：波次账目立起来。玩家房间的本体遭遇 = 第 0 波，不算在表里。</summary>
    public override System.Threading.Tasks.Task BeforeCombatStart()
    {
        EnsureRunSeed();   // ★ M3.10：开战即绑定本局种子（之后 16 波按它生成）

        // SubWavesPlanned 是计算属性，不需要（也不能）在这里赋值——见它上面的注释。
        SubWavesSpawned = 0;
        Entry.Logger.Info($"凛冬尖塔：战斗开始，本场波次账目 {SubWavesSpawned}/{SubWavesPlanned}（本体遭遇 = 第 0 波）。");
        Entry.Toast($"凛冬敌潮共 {SubWavesPlanned} 波，这里就是你驻守的房间。", "凛冬尖塔");
        FrostPanel.Refresh();
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public override async System.Threading.Tasks.Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (Owner?.Creature == null || !participants.Contains(Owner.Creature)) return;
        if (Owner.PlayerCombatState == null) return;

        // 场地清空 + 波次表没走完 = 间隙回合：补给/事件 + 下一波。
        if (combatState.HittableEnemies.Count > 0 || SubWavesSpawned >= SubWavesPlanned) return;

        EnsureRunSeed();   // ★ M3.10 兜底：读档恢复的战斗不走 BeforeCombatStart，这里再保一次
        var wave = WaveTable.All[SubWavesSpawned];

        if (wave.Kind == WaveTable.WaveKind.Event)
        {
            // ★ 事件波 = 喘息波（M3.7）：**按 2 倍档位回血**。
            //   它不打架，存在的意义就是让玩家喘口气 —— 老版本它只弹个对话框，一点用没有。
            await HealInGapAsync(FrostSpireConfig.EventHealPercent);

            SubWavesSpawned++;
            WavesSpawned++;
            FrostEvents.ShowEvent(wave.Title, this);
            Entry.Toast($"{wave.Title}（{SubWavesSpawned}/{SubWavesPlanned}）", "凛冬尖塔");
            FrostPanel.Refresh();
            Entry.Logger.Info($"凛冬尖塔：事件波「{wave.Title}」（本场 {SubWavesSpawned}/{SubWavesPlanned}）");
            return;
        }

        // ★ 间隙回血（M3.7 新增，用户「第三波就死了」的主因之一）。
        //   16 波是**一场连续战斗**，中间没有火堆、没有商店、没有回血 —— 只有每波发一张牌。
        //   纯消耗战的话，玩家的血量池撑不到后期。这个恢复就是"间隙"这两个字的实际内容。
        await HealInGapAsync(FrostSpireConfig.GapHealPercent);

        // —— 间隙补给：官方选牌界面三选一（context 来自本钩子——quest-spire 当年缺的就是它）——
        await GrantSupplyCardAsync(choiceContext);

        // —— 下一波（精英/Boss 波每只怪额外挂力量，quest-spire 血之契约同款接口）——
        //
        // ★★ 不再给怪传「槽位名」—— 这是实测「结束回合就卡死」的根因，务必别改回去：
        //    `CreatureCmd.Add<T>(combatState, slotName)` 最终走到 `NCombatRoom.AddCreature`，那里有：
        //        if (creature.SlotName != null) {
        //            if (EncounterSlots == null)
        //                throw new InvalidOperationException("... has slot name '...' but NCombatRoom.EncounterSlots is null.");
        //            nCreature.GlobalPosition = EncounterSlots.GetNode<Marker2D>(slotName).GlobalPosition;
        //        }
        //    而 `EncounterSlots` **只在遭遇自带场景（`Encounter.HasScene`）时才会被赋值**，
        //    本体遭遇绝大多数没有 → 它恒为 null → **只要传了槽位名就必抛异常**。
        //
        //    ★ 要命的是抛异常的时机：此时怪**已经进了战斗**（CreatureCmd.Add 前半段已跑完），
        //      只是位置没设、后续初始化被中断 → 场上留下一只「没有 move 的僵尸怪」。
        //      下一个敌方回合 `MonsterModel.PerformMove()` 直接抛
        //      `No move has been set for the monster` → **回合循环死亡，整场战斗卡死**。
        //      （日志原文：`Combat #1 turn loop died while its combat is in progress;
        //                  the combat is stuck until the room is restarted`）
        //
        //    ⇒ 正确做法：**不传槽位**（`slotName` 本来就是可选参数，默认 null），
        //      刷完一波后**自己排布**（`RelayoutEnemies`，算法复刻自 `NCombatRoom.PositionEnemies`）。
        Flash();
        var spawned = new List<Creature>();
        for (int i = 0; i < wave.Monsters.Length; i++)
        {
            var creature = await SpawnMonster(combatState, wave.Monsters[i]);
            if (creature == null) continue;
            spawned.Add(creature);

            // ★ 难度倍率：直接缩放血量。
            //   `CreatureCmd.SetMaxAndCurrentHp` 反编译确认 = `SetMaxHp` + `SetCurrentHp`（同时改上限与当前血）。
            //   ⚠️ 不能用 `SetMaxHp` 单独改 —— 它**不会**同步当前血量，怪会变成 CurrentHp > MaxHp 的怪状态。
            if (System.Math.Abs(wave.HpScale - 1.0) > 0.0001)
            {
                try
                {
                    decimal target = System.Math.Max(1m, creature.MaxHp * (decimal)wave.HpScale);
                    await CreatureCmd.SetMaxAndCurrentHp(creature, target);
                }
                catch (System.Exception e)
                {
                    Entry.Logger.Warn($"波次血量缩放失败（用原值继续）: {e.Message}");
                }
            }

            if (wave.Strength > 0)
            {
                try
                {
                    await PowerCmd.Apply<StrengthPower>(choiceContext, creature, wave.Strength, Owner.Creature, null);
                }
                catch (System.Exception e)
                {
                    Entry.Logger.Warn($"精英强化失败（不影响战斗）: {e.Message}");
                }
            }
        }

        // 自己把这一波摆开（游戏不会给"战斗中后加的怪"自动排位）。
        RelayoutEnemies(combatState);

        SubWavesSpawned++;
        WavesSpawned++;
        string tail = SubWavesSpawned >= SubWavesPlanned ? "最后一波！" : "";
        Entry.Toast($"{wave.Title}（{SubWavesSpawned}/{SubWavesPlanned}）{tail}", "凛冬尖塔");
        FrostPanel.Refresh();
        Entry.Logger.Info($"凛冬尖塔：放出「{wave.Title}」（本场 {SubWavesSpawned}/{SubWavesPlanned}，累计 {WavesSpawned}）");
    }

    /// <summary>
    /// 间隙回血（M3.7）。按「最大生命的百分之几」恢复，`percent &lt;= 0` 或满血时不做任何事。
    ///
    /// ★ 为什么是"间隙"的固定内容而不是给张牌让玩家选：
    ///   16 波连打本来就够长了，再塞一个选择只会更拖。恢复做成**自动的、可预期的**，
    ///   玩家要操心的是"这一波怎么打"，不是"我要不要现在回血"。
    ///   数值由 `FrostSpireConfig.GapHeal` 控制（默认 15%，事件波 30%），随时可调、不用改代码。
    /// </summary>
    private async System.Threading.Tasks.Task HealInGapAsync(int percent)
    {
        try
        {
            if (percent <= 0) return;

            var me = Owner?.Creature;
            if (me == null || !me.IsAlive) return;
            if (me.CurrentHp >= me.MaxHp) return;

            decimal amount = System.Math.Max(1m, me.MaxHp * percent / 100m);
            await CreatureCmd.Heal(me, amount, false);
            Entry.Toast($"间隙：回复 {amount:0} 点生命（{percent}% 上限）。", "凛冬尖塔");
            Entry.Logger.Info($"凛冬尖塔：间隙回血 {amount:0}（{percent}% 上限），当前 {me.CurrentHp}/{me.MaxHp}。");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"间隙回血失败（不影响战斗）: {e.Message}");
        }
    }

    /// <summary>
    /// 间隙补给：官方选牌界面**三选一**（可跳过）。
    /// `CardSelectCmd.FromChooseACardScreen(context, cards, player, canSkip)` —— quest-spire 当年用不了
    /// 是因为拿不到 PlayerChoiceContext；本钩子（BeforeSideTurnStart）手里正好有一个。
    /// 选牌界面万一炸了 → 兜底自动拿第一张，补给绝不蒸发。
    /// </summary>
    private async System.Threading.Tasks.Task GrantSupplyCardAsync(PlayerChoiceContext choiceContext)
    {
        List<CardModel> cards;
        try
        {
            if (Owner.Character?.CardPool == null) return;
            var constraint = Owner.RunState.CardMultiplayerConstraint;
            var charPool = Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, constraint);

            // ★ M3.8：开了「凛冬起始卡组」→ 补给**以凛冬牌为主**（2 凛冬 + 1 角色牌），
            //   让牌库真的往凛冬方向长；没开 → 维持原比例（1 凛冬 + 2 角色牌）。
            int frostCount = FrostSpireConfig.FrostDeck ? 2 : 1;
            int charCount = 3 - frostCount;

            cards = CardFactory
                .GetDistinctForCombat(Owner, charPool, charCount, Owner.RunState.Rng.CombatCardGeneration)
                .ToList();

            // 每次补给固定带凛冬工事牌（从我们自己的共享卡池抽）——布防玩法的主入口。
            var frostPool = ModelDb.All.OfType<CardPoolModel>().FirstOrDefault(p => p is FrostSpire.Cards.FrostCardPool);
            if (frostPool != null)
            {
                var frostCandidates = frostPool.GetUnlockedCards(Owner.UnlockState, constraint);
                var extra = CardFactory
                    .GetDistinctForCombat(Owner, frostCandidates, frostCount, Owner.RunState.Rng.CombatCardGeneration)
                    .ToList();
                cards.AddRange(extra);
            }

            if (cards.Count == 0) return;
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"间隙补给候选生成失败（不影响战斗）: {e.Message}");
            return;
        }

        CardModel? picked;
        try
        {
            picked = await CardSelectCmd.FromChooseACardScreen(choiceContext, cards, Owner, canSkip: true);
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"选牌界面打开失败，自动拿第一张兜底: {e.Message}");
            picked = cards[0];
        }

        if (picked == null)
        {
            Entry.Logger.Info("凛冬尖塔：玩家跳过了间隙补给。");
            return;
        }

        try
        {
            // 手没满进手牌（立即可用）；满了进抽牌堆（不丢）。
            var hand = CardPile.Get(PileType.Hand, Owner);
            var target = (hand != null && hand.Cards.Count < CardPile.MaxCardsInHand)
                ? PileType.Hand : PileType.Draw;
            await CardPileCmd.AddGeneratedCardsToCombat(new List<CardModel> { picked }, target, Owner, CardPilePosition.Bottom);
            Entry.Logger.Info($"凛冬尖塔：间隙补给已入 {target}。");
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"间隙补给发牌失败（不影响战斗）: {e.Message}");
        }
    }

    /// <summary>
    /// 按波次表的编号刷怪（编号 → 具体类型的 switch，全部编译期可查），返回刷出来的怪以便强化。
    /// ★ 不传槽位名（传了会抛异常，见调用处注释）；位置由 `RelayoutEnemies` 统一排。
    /// </summary>
    private static async System.Threading.Tasks.Task<Creature?> SpawnMonster(ICombatState combatState, int monsterId)
    {
        Creature? creature;
        switch (monsterId)
        {
            case WaveTable.LeafSlime: creature = await CreatureCmd.Add<LeafSlimeM>(combatState); break;
            case WaveTable.Inklet: creature = await CreatureCmd.Add<Inklet>(combatState); break;
            case WaveTable.CorpseSlug: creature = await CreatureCmd.Add<CorpseSlug>(combatState); break;
            case WaveTable.GlobeHead: creature = await CreatureCmd.Add<GlobeHead>(combatState); break;
            case WaveTable.FuzzyWurmCrawler: creature = await CreatureCmd.Add<FuzzyWurmCrawler>(combatState); break;
            case WaveTable.Flyconid: creature = await CreatureCmd.Add<Flyconid>(combatState); break;
            case WaveTable.BowlbugEgg: creature = await CreatureCmd.Add<BowlbugEgg>(combatState); break;
            case WaveTable.BowlbugNectar: creature = await CreatureCmd.Add<BowlbugNectar>(combatState); break;
            case WaveTable.BowlbugSilk: creature = await CreatureCmd.Add<BowlbugSilk>(combatState); break;
            default: creature = await CreatureCmd.Add<BowlbugRock>(combatState); break;
        }
        Entry.Logger.Info($"凛冬尖塔：刷 {monsterId} 号怪（位置随后统一排布）");
        return creature;
    }

    /// <summary>
    /// 把场上存活的敌人重新摆开。
    ///
    /// ★ 为什么要自己做：`NCombatRoom.PositionEnemies` 只在**房间初始化**时被调一次
    ///   （`CreateEnemyNodes` 里），战斗中**后加的怪不会被自动排位** —— 它们全落在节点默认点，
    ///   看起来叠成一坨。而官方那条「用槽位名摆位」的路走不通（见调用处注释：会抛异常）。
    ///
    /// 算法逐行复刻 `NCombatRoom.PositionEnemies`（scaling 取 1，本体遭遇基本都是 1）：
    ///   按怪物可视宽度横向铺开，放不下就压缩间距（最窄 5px）；奇数位略微上抬，避免排得太死板。
    /// 排的是 `combatState.Enemies`（活着的敌人），所以每波刷完都能得到干净的一排。
    /// </summary>
    private static void RelayoutEnemies(ICombatState combatState)
    {
        try
        {
            var room = NCombatRoom.Instance;
            if (room == null) return;

            var nodes = new List<NCreature>();
            foreach (var enemy in combatState.Enemies)
            {
                var node = room.GetCreatureNode(enemy);
                if (node != null) nodes.Add(node);
            }
            if (nodes.Count == 0) return;

            const float span = 960f;   // 敌方区域宽度（scaling = 1）
            float gap = 70f;
            float widths = nodes.Sum(n => n.Visuals.Bounds.Size.X);
            float need = widths + (nodes.Count - 1) * gap;
            float x = Math.Max((span - need) * 0.5f, 150f);
            float lift = 0f;

            if (x + need > span && nodes.Count > 1)
            {
                gap = Math.Max((span - 150f - widths) / (nodes.Count - 1), 5f);
                need = widths + (nodes.Count - 1) * gap;
                x = (span - need) * 0.5f;
                if (gap < 30f) lift = float.Lerp(60f, 40f, (gap - 5f) / 25f);
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                node.Position = new Vector2(x + node.Visuals.Bounds.Size.X * 0.5f, 200f - ((i % 2 != 0) ? lift : 0f));
                x += node.Visuals.Bounds.Size.X + gap;
            }
        }
        catch (System.Exception e)
        {
            Entry.Logger.Warn($"敌人重新排布失败（不影响战斗）: {e.Message}");
        }
    }
}
