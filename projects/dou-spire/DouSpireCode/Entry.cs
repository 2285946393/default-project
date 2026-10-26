using System.Reflection;
using BaseLib.Config;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using DouSpire.Table;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Ui.Toast;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace DouSpire;

/// <summary>
/// mod 入口。
///
/// 它是个**独立窗口**，不是尖塔里的一个 run —— 所以这里没有房间/地图/战斗的钩子，
/// 只有"把入口装上"这件事：主菜单按钮 + 全局热键 F9。
///
/// 用户对入口的要求：「**说到底只是在游戏里面开了个斗地主的窗口**」。
/// </summary>
[ModInitializer(nameof(Initialize))]
public partial class Entry
{
#if DOUSPIRE_LITE
    /// <summary>简洁版的 mod id —— 必须和完整版不同，否则两个工坊物品会装进同一个
    /// mods/DouSpire 文件夹互相覆盖。对应 DouSpireLite.json。</summary>
    public const string ModId = "DouSpireLite";
#elif DOUSPIRE_DEV
    /// <summary>**本地开发版**的 mod id（2026-10-09 加）。
    /// 必须和工坊版不同：游戏不允许两条同 id 的 mod，而用户要「订工坊版和别人联机」，
    /// 本地开发版得能用另一个身份同时待在 mods\ 里。对应 DouSpireDev.json。
    /// ★ 由 csproj 的 `ModName` 属性决定（ModName=DouSpireDev 时才定义 DOUSPIRE_DEV）——
    ///   打**工坊包**要 `dotnet build -p:ModName=DouSpire`，那时这里就是 "DouSpire"。</summary>
    public const string ModId = "DouSpireDev";
#else
    public const string ModId = "DouSpire";
#endif

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        // ★ 启动期就把 ONNX 运行时装上。平时 EnsureRuntime 等出牌才跑，但 BaseLib 在
        //   启动阶段（ModelDb.InitIds 的 LatePostInit）会反射我们所有静态字段读类型，
        //   那时 ONNX 还没加载就可能把游戏启动炸掉（2026-10-09 启动崩溃，方案一）。
        //   加载失败不影响启动 —— DouZero 会退回规则 AI（兜底铁律）。
        try { DouZeroAi.EnsureRuntime(); }
        catch (System.Exception e) { Logger.Warn($"DouZero 运行时预加载失败（AI 退回规则 AI）: {e.Message}"); }

        // 热键 + 主菜单按钮的"补装点"。Initialize() 跑得太早（场景树还没建），
        // 所以这几个事件到的时候都要再装一次（Install 是幂等的）。
        RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(_ => EnsureEntryPoints());
        RitsuLibFramework.SubscribeLifecycle<MainMenuReadyEvent>(_ => EnsureEntryPoints());
        RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(_ => EnsureEntryPoints());
        // 进了 run 就把桌面收掉：桌面是"窗口"，窗口开着的时候不该有 run 在跑。
        RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(_ => DouSpireTable.Close(Logger));

        // 本地化：桌面上的文字都是直接写死的中文，走不到游戏的 LocString 上。
        // 但**两条是必须注入的**（否则界面会露出英文属性名 / 空标题）：
        //   ① settings_ui  —— 设置页每一行的标题（DouSpireConfig.InjectLabels）
        //   ② rest_site_ui —— 篝火那个「斗地主」选项的标题和说明（DouSpireRestSiteSingleton.EnsureText）
        // ⚠️ 这两张表要等本地化加载完才取得到，所以初始化时调一次（可能太早、会 Warn 一次），
        //    下面 EnsureEntryPoints() 里再调一次兜底。见 DouSpireConfig.InstallLabels 的注释。
        DouSpireConfig.InstallLabels();

        try
        {
            ModConfigRegistry.Register(ModId, new DouSpireConfig());
            Logger.Info("DouSpire 设置页已注册。");
        }
        catch (System.Exception e)
        {
            Logger.Warn($"注册设置页失败（不影响玩法）: {e.Message}");
        }

        RegisterContentPack();
        RegisterNetworking();

        EnsureEntryPoints();
        Logger.Info("DouSpire initialized.");
    }

    private static bool _netDone;

    /// <summary>
    /// 把两张牌桌的**联机频道**挂到 RitsuLib 的 Sidecar 网络总线上。
    ///
    /// 两套频道**各自独立**（前缀不同，互不干扰）：
    ///   ① 斗地主：<c>table.in/open/act</c>（<see cref="DouSpireNet"/>，已上架，Seats 写死 3）
    ///   ② 德州扑克：<c>poker.in/open/act</c>（<see cref="DouSpirePokerNet"/>，玩法二新加）
    /// 每条频道都是"客机→主机（我的动作）、主机→所有人（开桌 / 牌局动作）"。
    /// 挂在 mod 初始化这里就够了 —— 注册只是往一个字典里塞处理器，收发都在 run 里发生。
    /// </summary>
    private static void RegisterNetworking()
    {
        if (_netDone) return;
        try
        {
            DouSpireNet.Register();
            DouSpirePokerNet.Register();
            _netDone = true;
        }
        catch (System.Exception e)
        {
            Logger.Warn($"注册联机频道失败（只能单人玩）: {e.Message}");
        }
    }

    private static bool _contentPackDone;

    /// <summary>
    /// 把**游戏内**的东西注册进 RitsuLib 的内容包 —— 目前只有篝火选项的单例模型。
    ///
    /// 这是让两个玩法**长进游戏本体**的关键一步：注册了 <see cref="DouSpireRestSiteSingleton"/>，
    /// 它才会在每次生成休息处选项时被调用，往篝火里塞「斗地主」+「德州扑克」两个入口
    /// （见 DouSpireRestSite.cs）。
    /// </summary>
    private static void RegisterContentPack()
    {
        if (_contentPackDone) return;
        try
        {
            RitsuLibFramework.CreateContentPack(ModId)
                .Singleton<DouSpireRestSiteSingleton>()
                .Apply();
            _contentPackDone = true;
            Logger.Info("DouSpire 篝火选项已注册（休息处会多「斗地主」和「德州扑克」两个）。");
        }
        catch (System.Exception e)
        {
            Logger.Warn($"注册篝火选项失败（桌面照常能用）: {e.Message}");
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

    /// <summary>
    /// 把两个入口（F9 热键 + 主菜单按钮）都装上。两个都幂等。
    ///
    /// ★ 这里还负责**兜底重刷设置页文案**：这个方法挂在 GameReady / MainMenuReady / RunLoaded
    /// 三个生命周期上，那时候本地化一定已经加载完了 —— 初始化时那次注入往往太早（取不到表）。
    /// 用户 2026-10-08 报的"设置全是英文"就是这条链断在这里（详见 DouSpireConfig 里的注释）。
    /// </summary>
    public static void EnsureEntryPoints()
    {
        DouSpireConfig.InstallLabels();

        try { DouSpireHotkey.Install(); }
        catch (System.Exception e) { Logger.Warn($"热键没装上: {e.Message}"); }

        // 两个入口按钮：「斗地主尖塔」+「德州扑克尖塔」（玩法二）。各自幂等。
        DouSpireMenuButton.InstallAll();
    }
}
