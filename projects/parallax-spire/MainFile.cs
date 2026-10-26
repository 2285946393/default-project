using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace ParallaxSpire;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public static void Initialize()
    {
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
        new Harmony("ParallaxSpire").PatchAll(Assembly.GetExecutingAssembly());
        GD.Print("[ParallaxSpire] 0.1.0 初始化完成；进入普通战斗后自动生效，F9 可开关。");
    }
}

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
internal static class CombatRoomPatch
{
    private static void Postfix(NCombatRoom __instance) => Attach(__instance);

    internal static void Attach(NCombatRoom room)
    {
        if (room.Mode != CombatRoomMode.ActiveCombat) return;
        if (room.GetChildren().OfType<ParallaxController>().Any()) return;
        try
        {
            room.AddChild(new ParallaxController { Name = "ParallaxController" });
            GD.Print("[ParallaxSpire] 已挂载战斗视差控制器。");
        }
        catch (Exception error)
        {
            GD.PushError($"ParallaxSpire 初始化失败，保留原画面：{error}");
        }
    }
}

// 事件房间转入真实战斗时补挂。
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.TransitionToActiveCombat))]
internal static class ActiveCombatPatch
{
    private static void Postfix(NCombatRoom __instance) => CombatRoomPatch.Attach(__instance);
}

