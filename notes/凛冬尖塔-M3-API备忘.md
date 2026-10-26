# 凛冬尖塔 · M3 工事牌 API 备忘（ZCode 侦察，2026-10-06 深夜）

> 给下一轮实现 8 张工事牌（建筑 4 + 单位 4）用的情报账。全部来自本机 dll 反编译/XML 文档，
> 来源标注在每条后面。**动手前照例先核签名，别凭这份备忘直接写。**

## 一、单位卡（民兵/猎手/冰法师/重装兵）→ 我方召唤物：官方叫「爪牙 Minion」

本体有一整套爪牙体系，不用发明：

| 部件 | 类型 | 说明 |
|---|---|---|
| `Creature.PetOwner`（`Player?`）/ `IsPet` / `Pets` | Entities.Creatures.Creature（反编译 181-205 行） | **我方单位 = PetOwner 指向玩家的 Creature**；`Player.PlayerCombatState.Pets` 持有列表 |
| `MinionPower` | Models.Powers | 爪牙身份的能力层（召唤物挂着它） |
| `VitruvianMinion`（遗物） | Models.Relics | **会召唤爪牙的现成遗物 → 第一反编译样板** |
| `MinionStrike` / `MinionDiveBomb` / `MinionSacrifice` / `Rally` | Models.Cards | 爪牙指挥牌 → 「让单位动起来」的现成写法 |
| `MockAttackAndSummonMinionMonster` + `MockAttackAndSummonEncounter` | Models.Monsters.Mocks / Models.Encounters.Mocks | **官方 Mock：会召唤 minions 的怪 → 第二反编译样板** |
| `SummonIntent` / `SummonNextTurnPower` / `SummonResult` / `SummonedEntry` | MonsterMoves.Intents / Models.Powers / Entities.Creatures / Combat.History | 召唤的意图/结果/历史条目全家桶 |
| `CombatState.CreateCreature(model, side, slot)` | Combat（反编译 491 行） | **side 传 `CombatSide.Ally` 就能造我方生物**（M1.5 已用它造过敌方） |

M3 实现路线：单位卡打出 → `CreateCreature(自定 Model, Ally, slot)` + 挂 `MinionPower` +
设 `PetOwner` → 每回合让爪牙普攻（照 `MinionStrike` 或 Mock 爪牙的行动写法）。
**第一个验证项就是拆 `VitruvianMinion`**（量小、链路全）。

## 二、建筑卡（箭塔/冰墙/篝火/了望台）→ 持久能力：FlameBarrier 模式

- 挂在玩家身上的 `PowerModel`，每回合触发（`BeforeSideTurnStart` / 能力自身的回合钩子）。
- 样板：`FlameBarrierPower`（quest-spire 交接 §10.2 已核过行为：能力自己反伤、`ValueProp.Unpowered`、无 cardSource）。
- 箭塔=每回合对随机敌人打 Unpowered 伤害（伤害命令照 `CreatureCmd` 家族找，比如 AttackCmd/DealDamage——**未核，写前反编译**）；
  冰墙=每回合 GainBlock（`CreatureCmd.GainBlock` 已验证）；
  篝火=每回合回血（**回血命令未核**）；了望台=每回合多抽 1（`ModifyHandDraw` 旋钮已验证可用）。
- 自定 Power 的注册：`RegisterPowerAttribute`（wb 情报）+ RitsuLib 的
  `ModTemporaryPowerTemplate` / `ModTemporaryAppliedPowerTemplate`（XML 确认存在）——**永久建筑的正确基类还没定位，写前查**。

## 三、自定义卡牌本体：没有独立模板类，继承游戏 CardModel

- RitsuLib XML 里**没有** `ModCardTemplate`（grep 过）→ 自定卡 = 直接继承游戏的 `CardModel`
  子类（本体每张卡就是一个 CardModel 子类，quest-spire 交接里 30 行的 Claw 反编译可当样子）。
- 注册用 `[RegisterCard]`（wb 情报；**签名/必填成员未核，写前反编译 RegisterCardAttribute**）。
- 卡面美术要进 pck（`ContentAssetProfiles.Encounter` 是遭遇的，卡牌的 profile 类型**未定位**）。
  ⚠️ 工程美术规矩：不自己造图，等用户给图或借本体——8 张工事牌发布前必须有图，先用占位（借本体同类型牌面）。

## 四、已验证可复用的全家福（本 M3 直接抄）

`CreatureCmd.Add<T>(combatState, slot)`（M1.5 实测）/ `PowerCmd.Apply<T>(context, target, amount, applier, source)`
（M2.6 实测）/ `CreatureCmd.GainBlock` / `CardFactory.GetDistinctForCombat` + `CardPileCmd.AddGeneratedCardsToCombat`
/ `CardSelectCmd.FromChooseACardScreen(context, cards, player, canSkip)`（M2.5）/ 锁 1 锁 2 两钩子 / `[SavedProperty]`。

## 五、M3 建议顺序

1. 拆 `VitruvianMinion` + `MinionStrike` + `MockAttackAndSummonMinionMonster`（一次反编译三连）→ 摸清爪牙全链路。
2. 先做**1 张最简单的单位卡**（民兵：站场、每回合普攻）验证链路 → 再铺其余 3 张单位。
3. 建筑卡从**冰墙**起步（GainBlock 已验证，无伤害命令风险）→ 再箭塔（要核伤害命令）。
4. 每张牌过真人实测再铺下一张（quest-spire 铁律：批量的未实测代码=一批假阴性）。

---

## 六、重大更新：MinionLib（2026-10-06 深夜，用户指路星见雅 mod 后挖出）

用户提示看星见雅 mod（Miyabi The Spire，工坊 3747607263，作者月下樱）——它的卡牌会召唤大量邦布。
拆解链：Miyabists2 → 前置库 **MinionLib**（工坊 3747595039，json 依赖 `min_version 0.5.2`）。

### MinionLib 是「专门做爪牙」的框架，比 OstyCmd 那条路完整一档

| 部件 | 说明 |
|---|---|
| `MinionModel : MonsterModel`（MinionLib 抽象基类） | 自定单位的正确基类；带 `virtual Task OnSummon(ctx, owner, MinionSummonOptions)` |
| `MinionSummonOptions(MaxHp, Primary/Secondary/TertiaryStatAmount, Source, **MinionPosition Front**)` | 召唤参数 record——**官方有站位概念** |
| `MinionGuardianPower` + 三个补丁（GuardianBlockToHp / GuardianOverkill / OwnerDamageSuppress） | **挡刀机制全套**：格挡换血、过量伤害、主人免伤——比裸用 DieForYouPower 周全 |
| `MinionTargetTypes` / `AllMinionsTargetType` / `AnyMinionOrOwnerTargetType` | 爪牙目标类型系统 |
| `PetsOrderAccessor` | 宠物排序 |
| Miyabi 侧模板 | `ModMinionTemplate : MinionModel`（RitsuLib 系模板，带素材覆盖接口）|

### 结论（影响 M3.2）

1. **自定单位/建筑的"身体"应继承 `MinionModel`（经 MinionLib）**，而不是裸 `AddPet<借来的怪>`。
   wb 已把 MinionLib 引入 frost-spire 工程（csproj+manifest，构建通过）并在核 Guardian 细节。
2. OstyCmd 路线（AddPet<借用的怪>）降级为「占位/快糙猛」方案——M3.1 民兵试点用它在跑，正式版换 MinionModel。
3. 邦布卡的具体实现类（BangbooSummonOne 等）定义在星见雅的另一层前置里，**没定位到，也不再需要**——
   MinionLib 框架本身已够自研。
4. 冲突记录：ZCode 与 wb 在 M3.2 同批文件撞车一次（FrostDeployment.cs 重复造轮子），
   ZCode 当场撤件让位，构建恢复 0 错 0 警。**建筑重构归 wb**，本备忘的 §二（Power 版建筑）标记为被 §六 取代。
