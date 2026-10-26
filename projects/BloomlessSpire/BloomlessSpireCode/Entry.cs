using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using BloomlessSpire.Packs;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace BloomlessSpire;

[ModInitializer(nameof(Initialize))]
public partial class Entry
{
    public const string ModId = "BloomlessSpire";
    public const string ResPath = $"res://{ModId}";

    public static Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        var assembly = Assembly.GetExecutingAssembly();

        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        // 新开一局时：初始化卡包，并弹出卡包选择浮层
        RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(evt =>
        {
            PackSelectionManager.Initialize(CardPackDatabase.Packs);
            Logger.Info($"Run started. Packs: {string.Join(", ", PackSelectionManager.AllPacks.Select(p => p.Id))}");
            ShowPackSelection();
        });

        Logger.Info("BloomlessSpire initialized.");
    }

    // 直接拿场景树根节点展示浮层，不依赖 GameReady 事件的时序
    private static void ShowPackSelection()
    {
        if (PackSelectionManager.ActivePack != null) return;

        var tree = Engine.GetMainLoop() as SceneTree;
        var root = tree?.Root;
        if (root == null)
        {
            Logger.Warn("ShowPackSelection: scene tree root unavailable.");
            return;
        }

        var panel = PackSelectionOverlay.CreatePanel();
        root.AddChild(panel);
        Logger.Info("Pack selection overlay added to scene tree.");
    }
}
