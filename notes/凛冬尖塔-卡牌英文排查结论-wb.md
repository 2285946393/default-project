# 凛冬尖塔 · 「卡牌还英文」排查结论 —— 真凶是 mod 被禁用（wb）

> 2026-10-06 18:30
> 现象：用户截图，卡面显示 `cards.FROST_SPIRE_CARD_OPEN_FROST_RIFT_CARD.description`，报「全变英文」

---

## 一、结论（一句话）

**不是翻译丢了，是 FrostSpire 整个 mod 没被游戏加载。**

---

## 二、证据链

### ① 决定性证据：日志明写"跳过"

最后一次游戏启动 = **17:55**
日志 `logs/godot2026-10-06T17.55.05.log`：

```
第 17 行：[INFO] Found mod manifest file ...\mods\FrostSpire\FrostSpire.json
第 276 行：[INFO] Skipping loading mod FrostSpire, it is set to disabled in settings
```

→ **mod 被跳过**，游戏用的是本体卡牌（英文）。卡面 key 显示成
`cards.FROST_SPIRE_CARD_XXX.description` 也是同一原因 —— 卡牌模型注册了，
但 `FrostLoc` 没跑（`Entry.Initialize` 没执行），覆盖表还是 17:55 之前那版（只有 3 条）。

### ② 本地化机制本身完全正常

同一份日志：

```
1110: Loading locale path=res://localization/eng
1143: Found flat localization override directory: ...\localization_override\zhs
1144: Loading locale path=res://localization/zhs
1209: Localization overrides are active for language 'zhs'
```

→ `zhs` 表正常加载，覆盖机制**已激活**。**不存在"回退英文"**。

### ③ 时间戳对不上

| 项 | 时间 |
|---|---|
| 最后一次游戏启动 | 17:55 |
| 当时部署的 DLL | 17:5x（旧版） |
| 现在的 DLL | 18:26 |

→ 用户看到的画面属于**旧 DLL + mod 被禁用**的那次运行。

### ④ `settings.save` 现已是启用

`steam/76561199532681033/settings.save`：
```json
"mod_settings": { "mod_list": [ ..., {"id": "FrostSpire", "is_enabled": true, "source": "mods_directory"} ] }
```
→ 启用状态已修好，**但还没重启过游戏**（重启才会读）。

---

## 三、排查过程中的两个弯路（记下来别再走）

### 弯路 1：以为"表名/目录名写错了"
- 查了 `LocManager.LoadTablesFromPath`，确认覆盖文件只在**本体有同名表**时才读
- 从 `SlayTheSpire2.pck` grep 出本体表清单：
  `cards/powers/monsters/relics` **全都有** → 表名没错
- 目录名 `zhs` / `eng` 也对（日志确认）

### 弯路 2：以为"覆盖文件格式非法导致整表失效"
- 查了 `TryLoadOverrideFile`：逐条 `LocValidator.ValidateFormatString`，
  **非法只丢单条**，不会整文件/整语言失效
- 所以格式问题**不可能**造成"全变英文"

→ 唯一能造成"全变英文"的入口是 `LoadTablesFromPath` 开头：
```csharp
if (!DirAccess.DirExistsAbsolute(text))   // text = "res://localization/zhs"
{
    Log.Warn($"... falling back to eng");
    text = LocalizationAssetDir + "/eng";   // ★ 整个语言换英文
}
```
但日志证明这个分支**没走到**（1144 行正常加载 zhs）。

**⇒ 排除法收窄到：不是本地化的问题 ⇒ 那就是 mod 没加载。** 回去翻日志，276 行等着。

---

## 四、★ 下次排查"卡牌英文"的固定三步（省时间）

```
1. 看最后一次游戏日志有没有 "Skipping loading mod FrostSpire"
   → 有 ⇒ mod 被禁用，去 settings.save 开，然后**完全重启游戏**
2. 看日志有没有 "FrostLoc: 注入 N 条 [cards] 文本到 ..."
   → 没有 ⇒ Entry.Initialize 没跑到 / FrostLoc 抛异常
3. 看日志有没有 "FrostLoc 自检通过" / "自检发现 N 个条目名对不上"
   → 对不上 ⇒ key 名和运行时真名不匹配（本次新增的自检，见下）
```

日志路径：`%AppData%/SlayTheSpire2/logs/godot<最新时间戳>.log`

---

## 五、本轮附带修复（真 bug，迟早会炸）

### ★ `RestoreOriginalMap` 的主路径是死的

```csharp
var actSave = state.Act.ToSave();
var savedMap = actSave?.SavedMap;   // ← 恒为 null
```

反编译 `ActModel.ToSave()`：
```csharp
public SerializableActModel ToSave() => new() {
    Id = base.Id,
    SerializableRooms = _rooms.ToSave()
};   // ★ 从不给 SavedMap 赋值
```

`SerializableActModel.SavedMap` 字段**存在**，但只有 `RunState.ToSave()`（真正存档）那条路才填。
⇒ 运行时恒为 null ⇒ 每次出裂隙都静默落到 `TryRebuildActMap` 去**重新生成**地图
⇒ **重新生成的图不含我们注入的裂隙点** → 玩家出裂隙后入口点凭空消失。

**修法**（`FrostRiftSession.cs`）：
1. **主路径**：进裂隙前把原图**对象引用**存进 `ConditionalWeakTable<RunState, ActMap>`
   （弱引用表，这局结束自动回收，不用手动清）。同一条命内 100% 准确。
2. **兜底**：内存里没有（读档后接着玩）→ 仍走重建，但重建后**用存档里的
   `RiftNodeCoord` 把裂隙点补回去**（新增 `RestoreRiftNode`）。

### ★ `FrostLoc` 新增自检

覆盖表 key 猜错时游戏**不报错**，卡面只显示原始 key —— 上次就卡在这。
现在 `Inject` 末尾调 `SelfCheck`，把"候选 key vs 运行时真名"打进日志：

- 全对上 → `FrostLoc 自检通过：所有条目名都能对上运行时真名。`
- 有对不上的 → `FrostLoc 自检发现 N 个条目名对不上（卡面会显示原始 key）：...`
- 运行时真名暂不可得（注册未完成）→ 记 info 跳过，**不算错误**

---

## 六、用户需要做的（唯一一步）

**完全退出游戏进程再启动。** 不是重启场景、不是退出到主菜单。
原因：
1. `settings.save` 的 `is_enabled` 只在**启动时**读
2. 新 DLL（18:26）只在**启动时**加载

启动后看日志应出现（大约在文件前中段）：
```
Found mod manifest file ...\mods\FrostSpire\FrostSpire.json
（不应该再有 Skipping loading mod FrostSpire）
FrostSpire initialized.
FrostLoc: 注入 N 条 [cards] 文本到 ...\localization_override\zhs\cards.json
FrostLoc 自检通过：所有条目名都能对上运行时真名。
[裂隙点] 地图入口点注入器已就绪。
```
