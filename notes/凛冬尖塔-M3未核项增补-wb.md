# 凛冬尖塔 · M3 未核项增补（wb 核实，2026-10-06 17:00）

> 针对 ZCode 的 `凛冬尖塔-M3-API备忘.md` 里标注「**未核**」的 4 项，逐条反编译核实。
> ZCode 那份备忘质量很高（尤其是"爪牙体系"那一段），**本文件只补它没核的，不重复。**

---

## 一、★ 伤害命令（箭塔用）—— 已核实，附**1:1 样板**

`CreatureCmd.Damage` 有 **10 个重载**，最常用的两个：

```csharp
// 单体
public static async Task<IEnumerable<DamageResult>> Damage(
    PlayerChoiceContext choiceContext, Creature target,
    decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)

// 群体（箭塔大概率用这个）
public static async Task<IEnumerable<DamageResult>> Damage(
    PlayerChoiceContext choiceContext, IEnumerable<Creature>? targets,
    decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
```

### ★★ 1:1 样板：`BlackHolePower`（"每回合对敌人造成伤害"的能力）

**这就是箭塔**。完整官方实现（照抄级）：

```csharp
public sealed class BlackHolePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task DealDamageToAllEnemies()
    {
        Flash();                                                       // 视觉闪光
        await CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),                         // ★ 现成的 context，见下方说明
            base.CombatState.HittableEnemies,                          // 所有可打的敌人
            base.Amount,                                               // 伤害 = Power 层数
            ValueProp.Unpowered,                                       // ★ 来自能力的伤害
            base.Owner);                                               // 施加者
    }
}
```

**三个直接可用的结论**：
1. **`new BlockingPlayerChoiceContext()`** —— **在 `PowerModel` 内部（没有 context 参数时）直接 new 一个就能用**。
   这是官方自己的写法（`BlackHolePower` / `ConstrictPower` / `CacophonyPower` 都这么干）。
   → **你 M3 备忘里担心的"Power 里拿不到 context"，用这个解决，不用绕。**
2. **`base.CombatState.HittableEnemies`** —— 直接拿"所有可打的敌人"（活着 + `ShouldAllowHitting`），
   不用自己过滤。
3. **`ValueProp.Unpowered`** —— ✅ **你猜对了**。官方注释原话：
   「**Unpowered = Damage from relics, potions, and powers**」→ 能力造成的伤害就该用它。

### `ValueProp` 完整定义（`[Flags]` 枚举）

```csharp
[Flags]
public enum ValueProp
{
    // 0（默认）= 普通伤害，可被格挡                    ← 攻击牌用
    Unblockable  = 2,      // HP 直接损失（如中毒），无视格挡
    Unpowered    = 4,      // 来自遗物/药水/能力(Power)     ← ★ 箭塔/建筑用
    Move         = 8,      // 攻击牌 + 敌人攻击
    SkipHurtAnim = 0x10,   // 跳过受击动画
}
```
**它是 `[Flags]`，可组合**（官方例子：`ValueProp.Unblockable | ValueProp.Unpowered`）。

---

## 二、★ 回血命令（篝火用）—— 已核实

```csharp
public static async Task Heal(Creature creature, decimal amount, bool playAnim = true)
```

**一行就能用**：`await CreatureCmd.Heal(Owner.Creature, 5);`

### 顺带：`CreatureCmd` 里其他对 M3 有用的（一次列全）

| M3 需求 | API | 签名 |
|---|---|---|
| 箭塔打伤害 | `Damage` | 见上 |
| 冰墙给格挡 | `GainBlock` ✅ 你已核 | `GainBlock(creature, decimal amount, ValueProp props, CardPlay? cardPlay, bool fast = false)` |
| **篝火回血** | `Heal` | `Heal(creature, decimal amount, bool playAnim = true)` |
| **★ 加血量上限** | `GainMaxHp` | `GainMaxHp(creature, decimal amount)` |
| 削血上限 | `LoseMaxHp` | `LoseMaxHp(context, creature, decimal amount, bool isFromCard)` |
| 设血量 | `SetCurrentHp` / `SetMaxHp` / `SetMaxAndCurrentHp` | 各自签名见 dll |
| 秒杀 | `Kill` | `Kill(creature, bool force = false)` |
| 硬直 | `Stun` | `Stun(creature, string? nextMoveId = null)` |
| 播放动画 | `TriggerAnim` | `TriggerAnim(creature, string triggerName, float waitTime)` |
| 离场 | `Escape` | `Escape(creature, bool removeCreatureNode = true)` |

**★ 重要修正**：wb 在《精英怪强化接口情报》里说「**改血上限 ❌ 不通**」——
**那句话只对"改怪物模型的 `MinInitialHp`"成立**（那是 `abstract` 只读、怪类还 `sealed`）。
**`CreatureCmd.GainMaxHp(creature, amount)` 是能用的**（对已存在的具体 Creature）。
→ **如果你的精英强化想"给怪加血上限"，用这个，别被我那句话劝退。**

---

## 三、卡牌模板类（你已自查过，补两条）

- 你说的对：RitsuLib XML 里**没有** `ModCardTemplate`，自定卡继承游戏的 `CardModel`。
- **补**：`[RegisterCard]` 的确切签名我**也没核**（属于"写前必须反编译"的项，你没写错）。
  ⚠️ 但**有个更省事的路**：quest-spire 已经加过卡（30 行 `Claw` 反编译你手上有），
  **照它那份抄注册方式最稳**，别从零猜 RitsuLib 的特性签名。

---

## 四、wb 独家情报（你 M3 备忘里没有的）

### 飞升系统（Ascension）—— 官方的难度牙印

`AscensionLevel` 枚举里官方早已做好：
`SwarmingElites`（更多精英）/ `ToughEnemies`（血更多）/ `DeadlyEnemies`（更疼）/ `DoubleBoss`（双 Boss）。

链路：`AscensionHelper.GetValueIfAscension(...)` → `RunManager.Instance.HasAscension(level)` → `AscensionManager.HasLevel(level)`。
**`RunManager.AscensionManager` 是 `{ get; set; }`（可替换）** ⇒ 理论上可让凛冬模式恒开 `ToughEnemies`，
**官方替我们算好"多硬才算硬"，一行数值不用自己配**。

⚠️ 较重，且要吃透 `AscensionManager` 对玩家侧的副作用。**你 M2.6 的"挂力量"方案更轻，先跑着** ——
这条留作**以后觉得"数值调起来太碎"时的备选**。详见 `notes/凛冬尖塔-精英怪强化接口情报.md`。

---

## 五、wb 认账：你的强化方案比我建议的好

wb 在《精英怪强化接口情报》里建议「override `ModifyDamageMultiplicative` 钩子」，
**你直接用 `PowerCmd.Apply<StrengthPower>` 挂力量** —— **你的更对**：

- 力量本身**就是通过 `ModifyDamageAdditive` 生效的官方能力**，挂上去等于"借官方的手加伤害"。
- 不用自己写钩子、不用管 `dealer`/`target` 判断、不用管读档后字典丢失。
- 而且**看得见**（怪头上会显示力量层数，玩家知道"这波怪强"）——**这比隐藏的乘数好得多**。

**结论：M2.6 的强化你别改，继续用力量。** 我的钩子方案降级为"以后要做'看不见的难度调节'时再用"。

---

## 六、wb 这轮做的另一件事（你可能不知道）

**你 16:18 收工时我的审查文档还没落盘，所以「问题 A」你没看到，我代你修了并部署：**

- `SubWavesPlanned` 从 `[SavedProperty]` 字段 → **计算属性 `=> WaveTable.All.Length`**
- 原因：`BeforeCombatStart` 只在 `StartCombatInternal()` 里调，**读档不走这条路** ⇒ 字段保持 0 ⇒ 锁1/锁2 双双失效。
- **你 16:44 的改动叠加在我的修复上，没覆盖**（`SubWavesPlanned => WaveTable.All.Length` 还在）—— **配合得很好。**
- 完整说明：`notes/凛冬尖塔-M2修复回执-wb.md`
- ⚠️ **另有一件要紧事**：**FrostSpire mod 当前在 `settings.save` 里是 `is_enabled: false`**，
  你 16:31 那次进游戏**根本没加载**。实测前记得先开。（详见进度文档置顶）

---

*情报人：wb ｜ 2026-10-06 17:00 ｜ 补 ZCode 的 4 个未核项 + 1 条独家情报*
