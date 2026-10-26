using System.Reflection;
using BaseLib.Config;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Runs;
using FrostSpire.Portal;
using FrostSpire.Relics;
using FrostSpire.UI;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Ui.Toast;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace FrostSpire;

[ModInitializer(nameof(Initialize))]
public partial class Entry
{
    public const string ModId = "FrostSpire";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        RunManager.Instance.RunStarted += OnRunStarted;
        // 兜底：读档进局不一定触发 RunStarted，进任何房间都确认面板挂着（学 quest-spire EnsureUi）。
        RunManager.Instance.RoomEntered += EnsurePanel;
        RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(_ =>
        {
            FrostPanel.Bind(null);   // = 隐藏 + 清引用
        });

        // 凛冬裂隙（传送门方案）出口兜底：RoomEntered 是**事件**，只能订阅，不能打补丁。
        FrostRiftExitPatch.Install();

        // 凛冬裂隙入口 = 原版地图上开局即可达的那个点（用户核心诉求）。
        //   注入 + 拦截「点它 → 换图」全在这个类里。
        FrostRiftMapNode.Install();

        // 素材热更新热键（F8）：清素材缓存 + 重建面板。美术改完图按一下即见效。
        // Initialize 跑得很早，场景树可能还没好，所以在几个生命周期事件上也补装（幂等）。
        FrostHotkey.Install();
        RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(_ => FrostHotkey.Install());
        RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(_ => FrostHotkey.Install());

        // 注册设置页（挂到游戏的「设置 → 模组配置」里，见 FrostSpireConfig）。
        try
        {
            ModConfigRegistry.Register(ModId, new FrostSpireConfig());
            Logger.Info("FrostSpire config registered.");
        }
        catch (System.Exception e)
        {
            Logger.Warn($"注册设置页失败（不影响玩法）: {e.Message}");
        }

        Logger.Info("FrostSpire initialized.");
        // 卡牌中文名注入（AppData 覆盖表，quest-spire LocInjector 同款机制）。
        FrostLoc.Inject(Logger);
        // 素材盘点：把「每个槽位最终用了哪张图」写进日志（美术靠这条确认图有没有被吃进去）。
        FrostAssets.ReportOnce();
    }

    /// <summary>波次面板没挂就挂上；已挂着则什么都不做。顺带兜底"凛冬卡组"换装。</summary>
    public static void EnsurePanel()
    {
        FrostPanel.EnsurePanel();
        TryInstallFrostDeck();
    }

    // ========================================================================
    //  ★ 凛冬起始卡组换装（M3.8）
    //
    //  为什么要"兜底重试"：`RunStarted` 触发时**牌库不一定已经铺好**
    //  （顺序由本体的开局流程决定，我们没有把握）。所以：
    //    · RunStarted 里先试一次；牌库是空的就留着
    //    · 之后每次进房间（RoomEntered → EnsurePanel）再试，直到装上
    //  `_deckInstallState == null` 表示"已装完 / 无需装"，不会重复执行。
    // ========================================================================
    private static RunState? _deckInstallState;
    private static bool _deckInstalling;

    /// <summary>把本地玩家的起始卡组换成凛冬卡组（牌库还没铺好就留着，下次进房间再试）。</summary>
    private static void TryInstallFrostDeck()
    {
        var state = _deckInstallState;
        if (state == null || _deckInstalling) return;
        _ = InstallFrostDeckAsync(state);
    }

    private static async System.Threading.Tasks.Task InstallFrostDeckAsync(RunState state)
    {
        var local = LocalPlayer(state);
        if (local == null) { _deckInstallState = null; return; }

        // 牌库还没铺好 → 留着，下次进房间再试（新开局牌库必不为空，所以"空"只可能是"还没铺"）。
        if (local.Deck.Cards.Count == 0) return;

        _deckInstalling = true;
        try
        {
            if (await FrostDeck.InstallAsync(state, local))
                _deckInstallState = null;   // 成功 → 不再重试
        }
        finally
        {
            _deckInstalling = false;
        }
    }

    /// <summary>屏幕角落的轻提示。失败只记日志，绝不让它把游戏搞崩。</summary>
    public static void Toast(string body, string? title = null)
    {
        try
        {
            RitsuToastService.ShowInfo(body, title);
        }
        catch (System.Exception e)
        {
            Logger.Warn($"Toast failed: {e.Message}");
        }
    }

    private static void OnRunStarted(RunState state)
    {
        // 读档继续：本地玩家已有契约 → 只把面板绑回来，不重发。
        if (LocalPlayer(state)?.GetRelic<FrostPactRelic>() is { } existing)
        {
            FrostPanel.Bind(existing);
            Logger.Info("Run loaded: 凛冬契约已从存档恢复，波次面板已挂。");
            return;
        }

        if (!FrostSpireConfig.WinterMode)
        {
            Logger.Info("凛冬模式未开启，本局照常。");
            return;
        }

        // 多人局确定性：给**所有玩家**都发一遍（每个客户端执行同一套操作，天然一致）。
        // 凛冬模式 v1 只按单机调数值，联机未适配（见设计案）。
        _ = GrantPactToEveryoneAsync(state);
    }

    private static async System.Threading.Tasks.Task GrantPactToEveryoneAsync(RunState state)
    {
        var local = LocalPlayer(state);
        foreach (var p in state.Players)
        {
            try
            {
                if (p.GetRelic<FrostPactRelic>() != null) continue; // 读档继续：不重发
                var pact = await MegaCrit.Sts2.Core.Commands.RelicCmd.Obtain<FrostPactRelic>(p);
                if (p == local)
                {
                    FrostPanel.Bind(pact);
                    FrostPanel.ShowPanel();
                    Toast("本局是守塔战：清空场地会引来下一波，共 16 波。", "凛冬尖塔");
                    // ★ 单位卡不在这里发了 —— 用户明确要求：
                    //   「你选了进传送点。才会给你那几张需要战旗的牌」
                    //   改在 EnterRiftAsync 里发（见 FrostRiftGrant.StarterCards）。
                }
                Logger.Info($"凛冬契约已发给玩家 {p.NetId}。");
            }
            catch (System.Exception e)
            {
                Logger.Error($"给玩家 {p.NetId} 发凛冬契约失败: {e}");
            }
        }

        // ★ 凛冬起始卡组（M3.8，用户要求「直接把自己卡组全部删掉换成我们凛冬前线的卡组」）。
        //   只对**新开局**生效（上面 `GetRelic != null` 的读档局会 continue 掉）。
        //   这里先试一次，牌库没铺好就交给 EnsurePanel 兜底重试。
        if (FrostSpireConfig.FrostDeck)
        {
            _deckInstallState = state;
            TryInstallFrostDeck();
        }
        else
        {
            Logger.Info("凛冬起始卡组：已关闭，保留角色原卡组。");
        }
    }

    /// <summary>
    /// 发凛冬单位卡组（M3 最小可玩集）：征召民兵 + 冰垒(前排) + 冰垒(后排) + 箭塔。
    /// 写法照 quest-spire §4.3：CreateCard 造有主的牌 → CardPileCmd.Add 进整局卡组 → 预览动画。
    ///
    /// ★ 触发时机（用户要求）：**进传送点之后才发**，不是开局就发。
    ///   —— 这样不点裂隙点的玩家，牌库里干干净净，完全是原来的尖塔。
    ///   由 `FrostRiftSession.EnterRiftAsync` 调用；`StarterCardsGranted` 保证只发一次。
    ///
    /// ★ M3.6 扩充（用户原话：「凛冬前线的玩法应该是各种有意思卡。我们就这么几个基础塔
    ///   有什么用呢。连人家邦布的效果都比我们多」）：
    ///   一次把 8 张新卡全发给玩家，**目的是让他一次性能把每种新玩法都摸到**
    ///   （篝火/瞭望台/符文壁/法师/霜爆塔/王座 + 暴风雪/冰封术）。
    ///   合计 12 张部署/技能牌。等数值定了再拆成分批解锁。
    /// </summary>
    internal static async System.Threading.Tasks.Task GrantStarterUnitCard(RunState state, MegaCrit.Sts2.Core.Entities.Players.Player p)
    {
        // ---- M3.4 基础四件套：1 张单位（民兵）+ 前排/后排冰垒 + 箭塔 ----
        // ★ 用泛型版 CreateCard<T>（内部走 ModelDb 取 canonical 原型），
        //   不要用 new CardX() —— CreateCard 会调 ToMutable()，要求传 canonical。
        await GrantOneCard(state, p, state.CreateCard<Cards.SummonMilitiaCard>(p), "征召民兵");
        await GrantOneCard(state, p, state.CreateCard<Cards.IceBulwarkCard>(p), "冰垒（前排）");
        await GrantOneCard(state, p, state.CreateCard<Cards.IceBulwarkBackCard>(p), "冰垒（后排）");
        await GrantOneCard(state, p, state.CreateCard<Cards.ArrowTowerCard>(p), "箭塔");

        // ---- M3.6 扩充八件套（建筑 6 + 技能 2）----
        await GrantOneCard(state, p, state.CreateCard<Cards.CampfireCard>(p), "篝火");
        await GrantOneCard(state, p, state.CreateCard<Cards.WatchtowerCard>(p), "瞭望台");
        await GrantOneCard(state, p, state.CreateCard<Cards.RuneWallCard>(p), "符文壁");
        await GrantOneCard(state, p, state.CreateCard<Cards.FrostMageCard>(p), "寒霜法师");
        await GrantOneCard(state, p, state.CreateCard<Cards.FrostBurstTowerCard>(p), "霜爆塔");
        await GrantOneCard(state, p, state.CreateCard<Cards.FrostThroneCard>(p), "寒冰王座");
        await GrantOneCard(state, p, state.CreateCard<Cards.BlizzardCard>(p), "暴风雪（技能）");
        await GrantOneCard(state, p, state.CreateCard<Cards.FreezeCard>(p), "冰封术（技能）");

        // ---- M3.9 凛冬「回响」十一件套（触发式效果牌）----
        // ★ 全部都发：这批牌的价值在**组合**（遗霜⇄霜葬、雪崩⇄霜燃、回响⇄冰棱），
        //   只发一半玩家根本看不到它们之间的关系，等于没发。
        await GrantOneCard(state, p, state.CreateCard<Cards.LastFrostCard>(p), "遗霜");
        await GrantOneCard(state, p, state.CreateCard<Cards.FrostLegacyCard>(p), "霜之遗赠");
        await GrantOneCard(state, p, state.CreateCard<Cards.FrostBurialCard>(p), "霜葬");
        await GrantOneCard(state, p, state.CreateCard<Cards.AvalancheCard>(p), "雪崩");
        await GrantOneCard(state, p, state.CreateCard<Cards.WinterEmberCard>(p), "余烬");
        await GrantOneCard(state, p, state.CreateCard<Cards.FrostBrandCard>(p), "霜燃（消耗源）");
        await GrantOneCard(state, p, state.CreateCard<Cards.IceShardCard>(p), "冰棱（消耗源）");
        await GrantOneCard(state, p, state.CreateCard<Cards.WinterWrathCard>(p), "凛冬之怒");
        await GrantOneCard(state, p, state.CreateCard<Cards.IceShatterCard>(p), "碎冰");
        await GrantOneCard(state, p, state.CreateCard<Cards.ColdWindCard>(p), "寒风");
        await GrantOneCard(state, p, state.CreateCard<Cards.FrostEchoCard>(p), "霜之回响");

        // ---- M3.10 凛冬「裂霜」五件套（易伤来源 + 力量来源）----
        await GrantOneCard(state, p, state.CreateCard<Cards.FrostCrackCard>(p), "裂霜（易伤来源）");
        await GrantOneCard(state, p, state.CreateCard<Cards.FrostSpreadCard>(p), "霜蚀蔓延");
        await GrantOneCard(state, p, state.CreateCard<Cards.FrostForgeCard>(p), "霜锻（力量来源）");
        await GrantOneCard(state, p, state.CreateCard<Cards.DeepFreezeCoreCard>(p), "极寒核心");
        await GrantOneCard(state, p, state.CreateCard<Cards.IceShellCard>(p), "冰壳");

        // ---- M3.12 凛冬「精锐」四件套（连击 / 不朽 / 固守 + 晋升引擎）----
        await GrantOneCard(state, p, state.CreateCard<Cards.FrostBladeCard>(p), "霜刃卫士（连击）");
        await GrantOneCard(state, p, state.CreateCard<Cards.UndyingFrostCard>(p), "不冻者（不朽）");
        await GrantOneCard(state, p, state.CreateCard<Cards.FrostWardenCard>(p), "凛冬守望者（固守）");
        await GrantOneCard(state, p, state.CreateCard<Cards.FrostDrillCard>(p), "凛冬操演（晋升引擎）");
    }

    private static async System.Threading.Tasks.Task GrantOneCard(
        RunState state, MegaCrit.Sts2.Core.Entities.Players.Player p,
        MegaCrit.Sts2.Core.Models.CardModel ownedCard, string label)
    {
        try
        {
            var result = await MegaCrit.Sts2.Core.Commands.CardPileCmd.Add(
                ownedCard, MegaCrit.Sts2.Core.Entities.Cards.PileType.Deck);
            MegaCrit.Sts2.Core.Commands.CardCmd.PreviewCardPileAdd(result, 1.6f);
            Logger.Info($"凛冬尖塔：{label} 已加入卡组。");
        }
        catch (System.Exception e)
        {
            Logger.Warn($"发「{label}」卡失败（不影响本局）: {e.Message}");
        }
    }

    /// <summary>
    /// 找出**本地玩家**：NetId 跟本机网络服务一致的那一位。
    /// 找不到（单人 / 测试环境）就退回第一位玩家。（照抄 quest-spire 的写法。）
    /// </summary>
    public static MegaCrit.Sts2.Core.Entities.Players.Player? LocalPlayer(RunState? state)
    {
        if (state == null || state.Players.Count == 0) return null;
        try
        {
            var localId = RunManager.Instance.NetService?.NetId;
            if (localId.HasValue)
            {
                foreach (var p in state.Players)
                    if (p.NetId == localId.Value)
                        return p;
            }
        }
        catch (System.Exception e)
        {
            Logger.Warn($"找不到本地玩家，退回第一位玩家: {e.Message}");
        }
        return state.Players[0];
    }
}
