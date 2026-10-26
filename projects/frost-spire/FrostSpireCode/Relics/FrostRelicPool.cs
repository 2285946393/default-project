using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace FrostSpire.Relics;

// 凛冬尖塔自己的遗物池。
//
// ⚠️ 必须挂 [RegisterSharedRelicPool]，否则游戏不认这个池子存在
//（quest-spire 实测过：不挂的话遗物详情页是一把锁）。
[RegisterSharedRelicPool]
public sealed class FrostRelicPool : TypeListRelicPoolModel
{
    public override string EnergyColorName => "White";
    public override Color LabOutlineColor => new(0.55f, 0.78f, 0.95f);
}
