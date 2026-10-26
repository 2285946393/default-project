using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace FirstPersonSpire;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public static void Initialize()
    {
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
        new Harmony("FirstPersonSpire").PatchAll(Assembly.GetExecutingAssembly());
        GD.Print("[FirstPersonSpire] 0.1.6 初始化完成；进入普通战斗后可点击第一人称按钮或按 F8。");
    }
}

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
internal static class CombatRoomPatch
{
    private static void Postfix(NCombatRoom __instance)
        => Attach(__instance);

    internal static void Attach(NCombatRoom room)
    {
        // 事件用假战斗和战后载入场景不挂控制器。
        GD.Print($"[FirstPersonSpire] 检查战斗房间，模式={room.Mode}。");
        if (room.Mode != CombatRoomMode.ActiveCombat) return;
        if (room.GetChildren().OfType<FirstPersonController>().Any()) return;
        try
        {
            room.AddChild(new FirstPersonController { Name = "FirstPersonController" });
            GD.Print("[FirstPersonSpire] 已挂载战斗控制器。");
        }
        catch (Exception error)
        {
            GD.PushError($"FirstPersonSpire 初始化失败，保留原画面：{error}");
        }
    }
}

// 有些事件先创建展示用房间，再转入真实战斗；此时 _Ready 不会再次执行。
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.TransitionToActiveCombat))]
internal static class ActiveCombatPatch
{
    private static void Postfix(NCombatRoom __instance) => CombatRoomPatch.Attach(__instance);
}

[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Commands.CreatureCmd),
    nameof(MegaCrit.Sts2.Core.Commands.CreatureCmd.TriggerAnim))]
internal static class FirstPersonAttackPatch
{
    private static void Postfix(MegaCrit.Sts2.Core.Entities.Creatures.Creature creature, string triggerName)
    {
        if (triggerName == "Attack" && MegaCrit.Sts2.Core.Context.LocalContext.IsMe(creature))
            PresentationSession.Current?.Attack();
    }
}
