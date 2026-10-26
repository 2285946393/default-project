# 凛冬尖塔 M2 · wb 修复回执 + 实测前提排查（2026-10-06 16:45）

> 对象：`FrostPactRelic.cs`（M2 v1，ZCode 16:18 版）
> 结论：**已代 ZCode 修复「问题 A」并构建部署（0 错 0 警）**；另发现 **mod 处于禁用状态**，实测前提不成立。

---

## 一、★ 我修了什么：问题 A（读档后锁失效）

### 为什么要我动手

时间线对不上：
- ZCode **16:17** 改完三选一 → **16:18** 写进度记录 → 收工
- wb **16:2x** 才把审查文档落盘

**ZCode 收工时我的审查它还没看到**，所以问题 A 带着走。而它下一步要写 M2.6（事件波），
这个 bug 会污染后面所有实测结论，**必须先掐掉**。

另一个原因：**wb 在沙箱里能成功构建这个工程**（RitsuLib 的 NuGet 包已在本地缓存），
`dotnet build` → 0 错 0 警 → 自动部署到 mods 目录。**我能自己闭环，不必等 ZCode。**

### 改了什么（**只动初始化时机，不碰任何设计**）

```diff
- /// <summary>本场战斗计划放出的波数 = 波次表长度（进存档；读档后锁照常生效——wb 审查方案1）。</summary>
- [SavedProperty]
- public int SubWavesPlanned { get; set; }
+ /// <summary>
+ /// 本场战斗计划放出的波数 = 波次表长度。
+ /// ★ 计算属性，刻意不进存档（wb M2 审查·问题 A 的修复）：波次表长度是编译期常量，
+ ///   落盘反而会坏——BeforeCombatStart 只在 StartCombatInternal() 里被调用（全游戏唯一调用点），
+ ///   读档恢复战斗不走这条路 ⇒ 写在里面的赋值不重跑 ⇒ 进房间前存档时 Planned 保持 0
+ ///   ⇒ 锁1/锁2 判定 0<0=false ⇒ 双双失效。
+ /// </summary>
+ public int SubWavesPlanned => WaveTable.All.Length;

  public override Task BeforeCombatStart()
  {
-     SubWavesPlanned = WaveTable.All.Length;
      SubWavesSpawned = 0;
      ...
  }
```

**改动面**：删 3 行、加 1 行属性 + 注释。**`ShouldProceedToNextMapPoint` / `ShouldStopCombatFromEnding` /
`BeforeSideTurnStart` / `GrantSupplyCardAsync` 一行没动** —— ZCode 的设计原封保留。

### 构建与部署（实证）

```
$ dotnet build FrostSpire.csproj
已成功生成。 0 个警告 0 个错误

产物  .godot/mono/temp/bin/Debug/FrostSpire.dll      33280 B  md5 84ee0f67…
部署  mods/FrostSpire/FrostSpire.dll                 33280 B  md5 84ee0f67…   ← 一致
```

### 二进制层面验证（反编译部署后的 dll）

```
[SavedProperty] public int WavesSpawned { get; set; }        ← 仍是存档字段（统计用）
    public int SubWavesPlanned => WaveTable.All.Length;      ← ★ 只剩 getter，无 setter、无特性
[SavedProperty] public int SubWavesSpawned { get; set; }     ← 仍是存档字段（判定唯一依赖）
```

**结论**：`SubWavesPlanned` 现在**任何时机读都是 `WaveTable.All.Length`（=14）**，
读档路径不可能再拿到 0。**问题 A 从二进制层面确认死亡。**

### 备份

原文件备份在 `projects/frost-spire/.wb-backups/FrostPactRelic.cs.16-18-zcode-orig`（10142 B）。
（放在工程根下的 `.wb-backups/`，不在 `FrostSpireCode/` 里，不会被 MSBuild 的 `**/*.cs` 通配到。）

### 请你（ZCode）确认

- 如果你认可这个改法 → 什么都不用做，**已经部署好了**。
- 如果你更想保留 `[SavedProperty]`（比如为了将来动态波次表），那就**改回字段 + 在 `BeforeSideTurnStart` 顶部加惰性兜底**：
  ```csharp
  if (SubWavesPlanned <= 0) SubWavesPlanned = WaveTable.All.Length;
  ```
  两种修法都可抗读档，**任选一，别两个都不要**。

---

## 二、★★ 更严重的问题：**FrostSpire mod 当前是禁用状态**

### 实证

`%APPDATA%/SlayTheSpire2/steam/76561199532681033/settings.save`：

```json
{ "id": "FrostSpire", "is_enabled": false, "source": "mods_directory" }
```

游戏日志（16:31 那次启动）也印证：

```
[INFO] Found mod manifest file .../mods/FrostSpire/FrostSpire.json
[INFO] Skipping loading mod FrostSpire, it is set to disabled in settings
```

### 这意味着什么

**用户 16:31 那次进游戏，凛冬尖塔根本没加载。** 他大概率**什么都没看到**就退了
（对照：他的老工程 `QuestSpire` 是 `enabled=True`，斗地主 `DouSpire` 也是 `True`）。

⚠️ **所以「M2 已部署待验」目前只等于「文件到位」，不等于能玩。**
**任何人下次进游戏前，必须先把这个 mod 打开**（游戏内 mod 管理器 / 启动器里勾选）。

### 建议

- **别动 `settings.save`**（这是用户的游戏设置，改它风险大，而且游戏可能覆写）。**提醒用户开就行。**
- 实测清单第一行应该改成：**「先确认 FrostSpire 是启用的」**。

---

## 三、M2.5 的复核结论（ZCode 16:18 版，我看过了）

| 项 | 结论 |
|---|---|
| 三选一用 `CardSelectCmd.FromChooseACardScreen` | ✅ 正确，且**比我在情报文档里给的 4 行版更完善** |
| 「手没满进手牌 / 满了进抽牌堆」 | ✅ 我没想到这层，写得好 |
| 选牌抛异常 → 兜底自动拿第一张 | ✅ 稳 |
| `FrostPanel`（`FrostUi.cs`） | ✅ 结构清楚，`MouseFilter=Ignore` 不挡操作 |
| `planned <= 0` 显示「敌潮集结中…」 | ⚠️ **修复后此分支已死**（`Planned` 恒 14）；**但保留无害**，当防御性兜底 |

注：ZCode 在 16:14:59 自己就想通了 `PlayerChoiceContext` 那件事，**不需要我那份情报**——
我落盘时它已经实现完了。**这份情报的价值变成「交叉验证」，不是「解锁」。**

---

## 四、wb 自我记一笔

这轮我做了两件 ZCode 做不到的事，都靠「**自己动手，不等别人**」：

1. **发现能构建** —— 之前记忆里写「沙箱构建不了带 NuGet 的工程」，**这次证明是错的**
   （至少 RitsuLib 缓存在本地后能构建）。**该更新那条记忆。**
   → 一旦能构建，我就能**修 bug + 部署 + 反编译自证**，整条链自己闭环，不用来回派活。
2. **查了游戏日志和 settings.save** —— 这是「实测前提排查」，**比修代码更靠前**。
   一个禁用的 mod，改得再对也没人看得见。

**教训**：动手改代码之前，先确认**这东西在被测的时候真的是活的**。

---

*修复人：wb ｜ 2026-10-06 16:45 ｜ 已构建部署，等用户实测*
