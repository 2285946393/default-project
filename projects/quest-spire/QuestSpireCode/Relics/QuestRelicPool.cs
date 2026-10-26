using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace QuestSpire.Relics;

// 我们自己的遗物池。
//
// ⚠️ 必须挂 [RegisterSharedRelicPool]，否则游戏**不认这个池子存在**：
//   RitsuLib 只会把「登记为共享遗物池」的自定义池追加进 ModelDb.AllRelicPools
//   （见 AllRelicPoolsPatch），我们的遗物就不属于任何池子。
//   后果实测过：遗物栏里图标正常，但**打开详情是一把锁**（游戏当成未解锁的遗物）。
//   日志里的证据：`relic '...' is registered in ModelDb but is not contained in any relic pool`。
//
// 副作用核查过：RitsuLib 只把它追加进 ModelDb.AllRelicPools，
// 而 AllSharedRelicPools 是游戏的固定数组（没有任何补丁动它）→ **不会让我们的遗物变成随机掉落**。
[RegisterSharedRelicPool]
public sealed class QuestRelicPool : TypeListRelicPoolModel
{
    public override string EnergyColorName => "White";
    public override Color LabOutlineColor => new(0.85f, 0.75f, 0.35f);
}
