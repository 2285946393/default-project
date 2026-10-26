# 盘点 · 杀戮尖塔 2 创意工坊 mod 库存

> **数据来源**：本机 Steam 创意工坊目录
> `D:/software/steam/steamapps/workshop/content/2868840/`
> **扫描时间**：2026-10-09 10:50 · **共 124 个 mod**（上次 10-08 是 121 个）
>
> **怎么更新这份**：见文末「§七 复现方法」。
> **一键盘点脚本**：`python tools/mod-inventory.py`（本次新写，支持工坊 + 本地 `mods/` + `_disabled_mods/` 合并输出，`--json` 出机器可读格式）。

---

## 一、总览（2026-10-09 更新）

| 指标 | 数量 |
|---|---|
| 工坊 mod 总数 | **124** |
| **已拆**（有反编译产物 / 真源码 + 笔记） | **18** |
| 半拆（只看了开源仓库，未反编译） | 0 |
| **未拆** | 约 107 |
| 带 dll | 105+ |
| 带 pck | 87+ |
| 零依赖 | 72 |

### 10-09 新增的已拆

| 工坊 id | 名字 | 产出 | 核心看点 |
|---|---|---|---|
| `3813193750` | **尖塔决斗 / SpirePvp** 0.2.5 | `projects/ref-spirepvp/`（3,933 行） | ★ 把游戏改造成 2 人对战；`PlayerChoiceSynchronizer` 当投票/回合栅栏；每机自算布局（「自己在左」） |
| `3747607944` | **尖塔：琐事 / STS2_Things** 1.25.10 | `projects/ref-things/`（43,487 行） | ★★★ **加了一整幕「深处」**；壳内嵌双版本实现；双框架兼容；付费岔路走正规 `GameAction` |
| `3747597254` 等 | **Spine 动画 API 变更**（不是单个 mod） | `notes/杀戮尖塔2-拆解-Spine动画API变更-20261009.md` | ★★ 游戏把 `MegaAnimationState.SetAnimation` 的返回值从 `MegaTrackEntry` 改成 `void`，生态里 **4 套兼容层**；附 `MemberRefScan` 工具 |
| `3747528152` | **Intent Graph / 意图图** 1.6.0 | `projects/ref-intentgraph/`（88 文件 / 16,143 行） | ★★★ **"读游戏运行时数据"做得最深的样本**。三层硬抠怪物 AI：反射状态机 → **扫 IL 字节码**（读 `MoveState._onPerform` 的 IL 找 `PowerCmd.Apply<T>`）→ 手写覆盖数据。附**免依赖双框架（BaseLib+RitsuLib）适配完整配方** + **跨 mod 内容包约定**（丢个 JSON 就生效）。另产出 60+ 条游戏 API 清单（`ScriptManagerBridge.LookupScriptsInAssembly` / `FontControlUtils` / `PreloadManager.Cache.GetScene` / `ReleaseInfoManager` …） |
| `3747605109` | **LoadOrderManager / 模组加载顺序管理** 0.4.0 | `projects/ref-loadorder/`（18 文件 / 5,530 行） | ★★★ **加载顺序的真相**：手动顺序只是拓扑排序的平局决胜，**游戏启动后会用它排好的顺序覆盖 `settings.save` 的 `mod_list`**（只有 `IsEnabled` 被保住）。另附**正规反射读写 `settings.save` 的完整路径 + 账户隔离守卫**（防覆盖别的账号）、**写外部系统后留快照做子序列校验**、壳+版本化实现的**最干净模板**、纯代码搭 UI + 两帧延迟布局自检 |
| `3772226486` | **CombatFlight / 中国人能飞** 1.3.1 | `projects/ref-combatflight/`（11 文件 / 1,294 行）+ `projects/ref-modconfig/`（3,173 行） | ★★★ **本体只是个玩具，但它摊开了游戏整套联机机制**。`INetMessage`（**实现即自动注册**，`ReflectionHelper.GetSubtypesInMods`）+ `INetGameService` 完整接口 + `NetMessageBus` 收发路径。★★★ **`ContentSorter` 按 `affects_gameplay` 排序联机 id**（`false` 排末尾 → 保护没装该 mod 的玩家）→ **接上了我们之前记的「`AssociateAssemblyWithMod` 不调 → 联机失稳」，现在知道具体坏在哪（消息 id + 动作 id + 模型 id 三套一起错位）**。另：位置同步配方（相对偏移/序号去重/环形比较/限流/有限性校验）+ **dll 内嵌 mp3 播放**（不带 pck 的做法，DouSpire BGM 可抄） |
| `3747531952` | **RandomForeseer / 随机数预测** 0.13.18 | `projects/ref-randomforeseer/`（**真源码 5,954 行** + 30 份设计文档 + 测试项目） | ★★★ **生态里唯一"把整场战斗克隆到旁边重跑一遍"的样本**。**克隆 RNG（`new Rng(rng.ToSerializable())`，Xoshiro256）+ 影子状态 + 逐个镜像卡牌行为**，全程不推进真随机数。顺带把**游戏本体 RNG 机制**完整挖出来（`RunRngSet` 12 条流 + `PlayerRngSet` 3 条流，每条种子 = run seed + 流名哈希；`SerializableRng` 能联网传输）。★ 镜像注册表（按精确运行时类型分发 + 5 种状态 + **风险记账**）+ IL 推断（用 RitsuLib 的 `HarmonyIl`）+ **逐版本适配方法论**（v0.108~111 四份）。**斗地主 AI 的直接参照** |
| `3774789540` | **Mod Preset Sync / 模组预设同步** 0.5.4 | `projects/ref-modpreset/`（外壳 + 0.107.1 + 0.111.0 三份，共 6,569 行）+ `pck/zhs_main_menu_ui.json` | ★★★ **本体是个「配置搬运工」，但等于一份免费的《游戏 mod 系统说明书》**。① **加载顺序机制彻底读通**（Kahn 拓扑 + 手动顺序只是平局决胜 + 启动后回写 `settings.save`）；② **`affects_gameplay` 官方口径** = 联机必要 = `true` 的 mod **+ 传递依赖闭包**（`FindMultiplayerDependencyClosure`）；③ **挖出游戏一个真 bug** —— `ModManager.cs:293` 回写 `mod_list` 时**只按 `Id` 匹配、丢了 `Source`**，导致「同 id 双来源（本地+工坊）」时用户按来源分别开关会被吞（作者写了 `DuplicateSourceSettingsGuard` 绕：抄快照 → 等 `Initialized` → 写回）；④ **第三代、最规范的「壳+版本化」框架 `JmcModLib.Dispatch`**（完整依赖解析器：当前 ALC + 双重 Resolving + 重入保护 + 同名冲突可操作报错）；⑤ **Steamworks 批量订阅/下载**完整模板；⑥ **mod 自带本地化** `res://<Mod>/localization/<lang>/main_menu_ui.json`；⑦ ★★★ **逐版本适配成本实证**：0.107.1→0.111.0 **只差 2 处 API + 1 处警告** → `notes/杀戮尖塔2-拆解-模组预设同步-20261009.md` |

---

## 二、已拆（18 个）

| 工坊 id | 名字 | 作者 | 产出 | 核心看点 |
|---|---|---|---|---|
| `3747497501` | 万象辉星 [RegentFX] | — | `projects/ref-regentfx/`（79 `.cs`） | 美术层：`StartPos` 约定、`FitVfx`、pck 三个坑 |
| `3774274248` | 动作与特效 | — | `projects/ref-meleeattack/`（76 `.cs`） | 演出层：打击感六件套、位移状态机 |
| `3798368044` | More Ironclad Animations（id `ShieldOnly`） | page | `projects/ref-shieldonly/`（3 dll，26,917 行） | 工程层：免 pck 读贴图、`RuntimeAsset`、内联着色器 |
| `3790899961` | 战斗路线求解器（CombatSolver） | Torch | `projects/ref-combatsolver/`（213,836 行） | ★ Anytime 束搜索、三态药水策略、影子世界推演、内存工程 |
| `3747583646` | 鸣潮先古（wuwancients） | supermanpower | `projects/ref-wuwancients/`（22,957 行） | ★ 自定义先古 + 确定性随机（不动游戏随机流） |
| `3804485262` | 假如塔2是类幸存者（`local.action_game`） | 普通网友Cano | `projects/ref-actiongame/`（14,690 行） | ★ 定时器把回合制"切片"成实时 + 89 个解阻塞补丁 |
| **`3747602295`** | **RitsuLib**（`STS2-RitsuLib`） | OLC（GitHub `BAKAOLC`） | `projects/ref-ritsulib/`（**不反编译**：MIT 开源 + 29 篇官方文档） | ★★★ **框架层地基**：声明式注册 / 74 个生命周期事件 / IL 断言工具链 / W3C 设计令牌主题系统 / 免引用跨 mod 调用 |
| **`3737335127`** | **BaseLib** | Alchyr（StS1「Downfall」作者） | `projects/ref-baselib/`（**不反编译**：MIT 开源 + 28 篇官方文档） | ★★★ **内容层地基**：43 个 `Custom*Model` 基类 / `SpireField` 给原版类挂字段 / `[CustomEnum]` 运行期扩枚举 / **`Notes.txt` 作者的逆向笔记**（出牌流水线 + `CardPileCmd.Add` IL 控制流） |

对应笔记：`notes/杀戮尖塔2-特效实战*.md`（前 3 个）· `notes/杀戮尖塔2-拆解-*.md`（后 5 个）

> **另拆了两个"不在工坊"的工程层目标**（2026-10-08 下午）：
> `Alchyr/ModTemplate-StS2`（299★ 工程模板）+ `Alchyr/StS2ModAnalyzers`（Roslyn 分析器），
> 笔记：`notes/杀戮尖塔2-拆解-ModTemplate工程模板.md`。
> **它比再拆一个玩法 mod 值钱** —— 我们踩的三个坑（manifest 假话 / 构建目标装死 / 部署过期）
> 全落在它讲的范围里。**拆它顺带找到一条真 bug：依赖条目该写 `min_version` 而不是 `version`**
> （全生态 48 : 10，用 `version` 的 10 条全是我们自己的）。

> **两个框架都拆完了** —— RitsuLib（框架层，26 个 mod 依赖）+ BaseLib（内容层，25 个依赖），
> **合计覆盖 42 个工坊 mod（约 44%）**。这两个都是 **MIT 开源，不用反编译**。
> ⚠️ **BaseLib 的清单在嵌套目录里**：`3737335127/BaseLib/BaseLib.json`（不在包根），
> 所以按"包根找 json"的扫描会漏掉它 —— 这是第一轮统计出错的原因。

## 三、半拆（0 个）

> 原来唯一的半拆是 `3747531952` 随机数预测（只看了开源仓库、没反编译）。
> **2026-10-09 已升为「已拆」** —— 直接 `git clone` 拿了真源码（MIT），产出见上表。

---

## 四、今天（10-08）动过的 11 个

目录时间戳分两批 —— **02:00 前后那批像是刚下的**：

| 时间 | 工坊 id | 名字 | 版本 | 依赖 |
|---|---|---|---|---|
| 01:55 | `3799476240` | CouchCoop | 0.4.0 | 无 |
| 01:56 | `3771349866` | Crash Landing: As Depicted | 0.1.2 | 无 |
| 01:57 | `3748986432` | SignatureLib | v1.0.0 | 无 |
| 01:59 | `3747729740` | 无尽模式 | 0.4.3 | 无 |
| 01:59 | `3747588119` | Kafka | v0.12.10 | 无 |
| 02:00 | `3746969593` | Acts from the Past | 1.0.5 | BaseLib |
| 02:00 | `3777276296` | BetterAnimation2 | 0.6.1 | STS2-RitsuLib |
| 02:00 | `3810696310` | More Defect Animations | v0.15.7 | 无 |
| 00:20 | `3813511203` | STS2 Agent ModDev Workspace | 1.0.0 | 无（★ 是**教程资料包**，不是 mod） |
| 00:22 | `3747636338` | Campfire Trading | v1.0.1rs | BaseLib |
| 00:41 | `3747497501` | 万象辉星 [RegentFX] | 0.5.1 | 无（已拆，属更新） |

⚠️ **注意**：Steam 更新 mod 时也会刷新目录时间戳，所以"时间新"≠"刚下载"。
真正新下载的是 **01:55–02:00 那 8 个**（前面没有它们的笔记/产物）。

---

## 五、依赖分布（121 个里）

| 依赖 | 用到它的 mod 数 | 说明 |
|---|---|---|
| **（零依赖）** | **72** | 占 6 成 —— 说明这个生态"裸 Harmony 也能玩" |
| **`STS2-RitsuLib`** | **26** | ① **框架层**：patcher / 生命周期 / 模型能力 / 声明式注册 / 主题系统（CombatSolver 的底座） |
| `BaseLib` | 25 | ② **内容层**：提供 `CustomAncientModel` 等**内容注册口子**（鸣潮先古用的），作者 **Alchyr**（StS1「Downfall」作者） |
| `JmcModLib` | 3 | 另一套小框架 |
| `aemeath-ww` | 2 | |
| `MinionLib` | 2 | 随从系统（FrostSpire 依赖） |
| `tune_strain` / `ModConfig` / `MomoLib` / `voicemod` | 各 1 | |

> **结论**：想做大改造，两条主流路径是 **`BaseLib`（塞内容）** 和 **`RitsuLib`（改框架）**。
> **两个框架合起来覆盖 42 个 mod（约 44%）**。
>
> ⚠️ **修订记录**：早先一版统计写的是「RitsuLib 23 / BaseLib 25」，那是因为扫描时
> 只读了工坊目录，且 `BaseLib` 装在**嵌套布局**里（`3737335127/BaseLib/BaseLib.json`）被漏掉。
> 现在扫的是「工坊 + 本地 `mods/` 合并、按 manifest id 归并」，**以本节为准**。

### 你自己在用的（本地 `Slay the Spire 2/mods/`）

| mod | 版本 | 依赖 |
|---|---|---|
| `DouSpire` | v0.2.0 | RitsuLib **0.6.6** + BaseLib v3.4.7 |
| `QuestSpire` | v0.2.28 | RitsuLib **0.6.2** + BaseLib v3.4.7 |
| `FrostSpire` | v0.1.0 | RitsuLib **0.6.5** + BaseLib v3.4.7 + MinionLib 0.6.3 |
| `BloomlessSpire` | v0.0.0 | RitsuLib **0.6.2** |

> ⚠️ **四个自研 mod 全部依赖 RitsuLib，但版本不一致（0.6.2/0.6.5/0.6.6）**。
> 框架仍在活跃更新（2026-10-06 还在推），建议统一到最新版。

---

## 六、值得拆的候选（我按"能学到多少"排的）

| 优先 | 工坊 id | 名字 | 为什么值得 |
|---|---|---|---|
| ✅ **已拆** | `3747602295` | **RitsuLib** | 被 **26 个 mod** 依赖、CombatSolver 的底座，**本机 4 个自研 mod 全依赖它**。MIT 开源 + 29 篇官方文档 |
| ✅ **已拆** | `3737335127` | **BaseLib** | 被 **25 个 mod** 依赖，**内容层地基**（43 个 `Custom*Model`）。MIT 开源 + 28 篇 Wiki + `Notes.txt` 逆向笔记。**清单在嵌套目录里** |
| ✅ **已拆** | — | **`Alchyr/ModTemplate-StS2`**（**不在工坊**） | **299★ / 50 fork**，**塔2 mod 工程模板**（`dotnet new` 模板包 `Alchyr.Sts2.Templates` v2.5.2），作者是 BaseLib 的 Alchyr。定义了"一个 mod 工程该长什么样"（路径自动发现 / 依赖版本同步 / 部署 / pck 导出 / manifest）。**对我们自己的工程最有参考价值** → `notes/杀戮尖塔2-拆解-ModTemplate工程模板.md` |
| ✅ **已拆** | — | **`Alchyr/StS2ModAnalyzers`**（不在工坊） | Roslyn 分析器 4 条规则（`Alchyr.Sts2.ModAnalyzers` v0.2.1）。⚠️ **更正**：本行原来标 ★★，但该仓库实际只有 **0★ / 1 fork**，协议也未声明 —— 降到 **★**（价值在"规则清单本身是份检查表"，不是热度）。同上一篇笔记 |
| ✅ **已拆** | `3779807977` | **弹幕尖塔 DanmakuSpire** | ⚠️ **纠正**：不是弹幕射击，是 **B 站式滚动弹幕评论**，`affects_gameplay:false`，**不改玩法**。价值在「兼容性工程」：逐补丁 try 降级 + 字符串类型名探测 + 内容全外置 + 多人只传信号。→ `notes/杀戮尖塔2-拆解-弹幕尖塔-20261009.md` |
| ★★ | `3772226486` | 中国人能飞 CombatFlight | ✅ **已拆**（2026-10-09）。本体只是玩具（1,294 行，让角色飞起来），但**摊开了游戏整套联机机制** → `notes/杀戮尖塔2-拆解-中国人能飞-20261009.md` |
| ✅ **已拆** | `3747531952` | 随机数预测 RandomForeseer | ✅ 2026-10-09 拆完。**MIT 有真源码**，直接 `git clone`（不用反编译）。★★★ 生态里唯一「把整场战斗克隆到旁边重跑」的样本 → 顺带挖出**游戏本体 RNG 机制**（12+3 条流 / Xoshiro256 / 可联网传输）。**斗地主 AI 的直接参照** → `notes/杀戮尖塔2-拆解-随机数预测-20261009.md` |
| ★ | `3747819202` | Hades Ancients | 和"鸣潮先古"同题材（先古线），**现在知道底是 `CustomAncientModel` 了**，可横向对比覆盖了哪些虚方法 |
| ★ | `3747588119` | Kafka（今天新下） | 鸣潮/星穹角色系 |
| ★ | `3777276296` | BetterAnimation2（今天新下） | 动画增强，和我们的特效线同域 |
| ★ | `3810696310` | More Defect Animations（今天新下） | 角色动画，同上 |
| ★ | `3747729740` | 无尽模式（今天新下） | 玩法模式扩展 |
| — | `3747575739` | More Enchantments | 原版机制扩展（附魔） |
| — | `3797367057` | 一代遗物 | 跨代内容移植 |

### 10-09 新增候选（盘点后补的）

| 优先 | 工坊 id | 名字 | 为什么值得 |
|---|---|---|---|
| ✅ **已拆** | `3747605109` | **LoadOrderManager**（模组加载顺序管理） | ✅ 2026-10-09 拆完。给本体模组界面补"排序编辑器"。★★★ 三条结论：**① 手动顺序只是拓扑排序的平局决胜，游戏启动后会用它排好的顺序覆盖 `settings.save` 的 `mod_list`（只有 `IsEnabled` 被保住）；② 怎么正规反射读写 `settings.save` + 账户隔离守卫；③ 写外部系统后留快照做子序列校验**。另：壳+版本化实现的最干净模板 → `notes/杀戮尖塔2-拆解-加载顺序管理-20261009.md` |
| ✅ **已拆** | `3774789540` | **Mod Preset Sync / `04_ModPresetManager`** | ✅ 2026-10-09 拆完（6,569 行）。和 LoadOrderManager 配套的「模组预设导出/导入/预览/应用」。**顺手挖出 4 样远超它本体的东西**：加载顺序机制彻底读通 + `affects_gameplay` 官方口径（= `true` 的 mod + 依赖闭包）+ **游戏一个真 bug**（`:293` 回写 `mod_list` 只按 `Id`、丢 `Source`）+ **最规范的壳+版本化框架 `JmcModLib.Dispatch`**；另附 Steamworks 批量下载模板、mod 自带本地化约定、**逐版本适配成本实证（0.107.1→0.111.0 只差 3 处）** → `notes/杀戮尖塔2-拆解-模组预设同步-20261009.md` |
| ★★ | `3747528152` | ~~**Intent Graph**~~ | ✅ **已拆**（2026-10-09）。**"读游戏运行时数据"最深样本**：反射怪物状态机 + **扫 IL 字节码** + 手写覆盖数据；附免依赖双框架适配 + 跨 mod 内容包约定 → `notes/杀戮尖塔2-拆解-意图图-20261009.md` |
| ✅ **已拆** | `3747531952` | 随机数预测 RandomForeseer | ✅ 2026-10-09 拆完（真源码 5,954 行 + 30 份文档）。**克隆 RNG + 影子状态 + 镜像**，不推进真随机数；**斗地主 AI 的直接参照** → `notes/杀戮尖塔2-拆解-随机数预测-20261009.md` |
| ✅ **已拆** | `3779807977` | **弹幕尖塔 DanmakuSpire** | 已拆完。**不是弹幕射击**，是 B 站式滚动弹幕评论；`deps` 为空、全自研。样本价值：兼容性工程（见笔记） |
| ★ | `3747597254` `3748235030` `3748603697` `3749627968` | Aemeath / AncientWaifus / Mesugaki / Necrobinder Card Portraits | **已确诊**被 Spine API 变更打断的 4 个（见 `杀戮尖塔2-拆解-Spine动画API变更`）。想学「角色立绘/动画」的话，**看已适配的**更有价值：`Chaos_XC_defect01`（有 `XCskinSpineCompat` 兼容层）、`chizuruCharacter`（39，大量 Spine 播放器） |
| ★ | `3747729740` | 无尽模式 | 玩法模式扩展 |
| ✅ **已拆** | `3802537170` | **休息处扩展 RestSiteExpansion** | 和 DouSpire 的篝火选项**同域**，2 选项扩到 14 个。★ 挖到官方口子 `AbstractModel.TryModifyRestSiteOptions` + **联机按下标同步选项**这个隐形炸弹 → `notes/杀戮尖塔2-拆解-休息处扩展-20261009.md` |
| — | `3747606660` | Show Player Hand Cards | UI 层小件 |

### 框架里的三个"还没做"（作者自己列的 TODO，说明这些是空白区）

BaseLib 的 `Notes.txt` 结尾写了它的 TODO，其中对我们是**机会信号**：

| TODO | 含义 |
|---|---|
| `base damage/block modifier support for card model` | **加"基础伤害/格挡"修正还没有官方支持** → 想做得自己动手 |
| `Adjust transpiler patch implementations to make them more general` | transpiler 还不够通用 |
| `WhatMod` | "这张卡/遗物来自哪个 mod"的查询工具（`Utils/WhatMod.cs` 已存在，但作者标为待完善） |

### 官方文档里**还没细读**但和特效/机制线相关的（模板已备好，随时可看）

| 来源 | 文档 | 为什么以后要看 |
|---|---|---|
| RitsuLib | `creature-visuals-and-animation.md`(6.1KB) · `card-dynamic-var-toolkit.md`(15KB) · `secondary-resources.md`(**27KB**) | 生物视觉 / 卡牌动态变量 / 第二资源子系统（88 类型） |
| BaseLib | `models/custom-orbs.md`(7.7KB) · `scenes/creature-visuals.md`(4.8KB) · `utilities/config-advanced.md`(9.2KB) | 充能球 / 生物视觉 / 高级配置 |

> 两个框架的文档都已**落盘到仓库**（`projects/ref-ritsulib/upstream/` 与
> `projects/ref-baselib/wiki/docs/`，后者是入库的），不用重新下载。

---

## 六·补、★★ 免费的「权威加载顺序表」（2026-10-09 发现）

**CombatSolver（战斗路线求解器）会快照整份 mod 列表**：

```
Slay the Spire 2/.combatsolver-precombat/process-<pid>/startup-mods/
    00_MemeSpire/  01_OmniscientCrystalSphere/  ...  83_voicemod/
```

它开战前把当前 mod 集合**按加载顺序**复制一份（带 `.combatsolver-precombat-owner` 标记），用来复现对局。
**副作用是一份免费的权威加载顺序** —— 以后排查「谁先加载」「内容排序为什么不同」不用猜。

本机 84 个的顺序（节选关键位）：

| 序号 | mod |
|---|---|
| `04` | ModPresetManager |
| `22` | intentgraph2 |
| `27` | MinionLib |
| **`33`** | **LoadOrderManager**（管加载顺序的那个） |
| **`34`** | **STS2-RitsuLib**（框架层） |
| `42` | CombatSolver |
| **`51`** | **BaseLib**（内容层，比 RitsuLib 晚 17 位） |
| `52` | ModConfig |
| **`68`** | **QuestSpire**（自研） |
| **`69`** | **FrostSpire**（自研） |
| **`70`** | **DouSpire**（自研） |
| `71` | RestSiteFondle + OrchisNecrobinderSkin + NecrobinderOstyAnim + NecrobinderCardPortraits（打包在一个目录里） |
| `83` | voicemod（君宝娘化） |

> ⚠️ 目录会随每次开战增长（本次 `process-10300` 是空的、`process-30500` 有效）。
> ✅ **`LoadOrderManager`（33）已拆**（2026-10-09）。它排在框架前面是因为**它只改 `settings.save`、
> 不需要任何框架**；而且它改的顺序会直接影响后面所有 mod 的加载次序
> → `notes/杀戮尖塔2-拆解-加载顺序管理-20261009.md`。

---

## 七、复现方法（以后一键更新）

思路：遍历工坊目录 → 每个子目录里找 `*.json`（深度 ≤2）当 manifest → 读 `name`/`version`/`dependencies`。

```python
import os, json, glob, datetime
base = r"D:/software/steam/steamapps/workshop/content/2868840"
for d in sorted(os.listdir(base)):
    p = os.path.join(base, d)
    if not os.path.isdir(p): continue
    for f in glob.glob(os.path.join(p, "*.json")) + glob.glob(os.path.join(p, "*", "*.json")):
        try: j = json.load(open(f, encoding="utf-8-sig"))
        except: continue
        if isinstance(j, dict) and ("name" in j or "id" in j):
            print(d, j.get("displayName") or j.get("name"),
                  j.get("version"), [x["id"] for x in j.get("dependencies", [])])
            break
```

**坑**：
- manifest 文件名不固定（`<id>.json` / `mod_manifest.json` / 别的），所以**按内容找**而不是按名字。
- 编码要 `utf-8-sig`（有 BOM）。
- 有的包第一层不是 manifest（是 `README.md` / `assets/` / `lib/`），所以要把深度放宽到 2。
- **别用 `find -iname "*中文*"` 找 mod** —— 目录名是工坊数字 id，mod 名在 json 里。
- ⚠️ **框架包可能是嵌套目录**：`BaseLib` 装在 `3737335127/BaseLib/BaseLib.json`，
  **包根没有 json**。所以扫描必须放宽到深度 2（或更深），否则会漏掉它 —— 这是第一轮统计出错的原因。

---

## 八、和 `AGENTS.md` 的关系

本机 `Slay the Spire 2/mods/` 里（游戏实际加载的）：

| 类别 | 目录 |
|---|---|
| **自己写的** | `BloomlessSpire` / `DouSpire` / `FrostSpire` / `MemeSpire` / `QuestSpire` / `君宝娘化` |
| **框架（被依赖的大件）** | `STS2-RitsuLib` |

（`BaseLib` 在本地 mods 里**没有独立目录**，它是随工坊 `3737335127` 安装的。）

工坊那 121 个是**订阅下来的参考样本**，两者不要混 —— 但**框架是共用的**。

### 自研 mod 的依赖矩阵（发现：版本不统一）

| mod | RitsuLib | BaseLib | 声明的 `min_game_version` |
|---|---|---|---|
| `DouSpire` | 0.6.6 | v3.4.7 | **0.106.0** |
| `QuestSpire` | **0.6.2** | v3.4.7 | **0.106.0** |
| `FrostSpire` | **0.6.5** | v3.4.7 | **0.106.0** |
| `BloomlessSpire` | **0.6.2** | —（只依赖 RitsuLib） | 0.106.0（清单还是模板样稿） |
| `MemeSpire` | 无（纯文案包，`has_dll: false`） | — | 0.106.0 |

> ⚠️ **RitsuLib 版本三个不一样**（0.6.2 / 0.6.5 / 0.6.6）。两个框架都在活跃更新
> （RitsuLib 2026-10-06 推过，BaseLib 2026-10-07 推过），**建议统一到最新**。
>
> ⚠️ 四个 csproj 里都有个 `SyncManifestDependencies` 目标，本意是"构建时自动把依赖版本改对"，
> 但 **2026-10-08 实测它没生效**：部署出来的 `QuestSpire.json` 仍写 0.6.2、`FrostSpire.json` 仍写 0.6.5
> （实际装的是 0.6.6）。证据：部署副本与源码副本逐字节相同，而该目标会回写源码文件 ——
> 源码 json 的 mtime（10-04 / 10-06）早于最后一次构建（10-08 / 10-07），说明它被静默跳过了。
> 详见 `斗地主尖塔-任务尖塔-框架优化建议.md` §二.A1-2。

> ⚠️⚠️ **`min_game_version` 是个真 bug（2026-10-08 发现）**：
> 本机已装游戏是 **v0.111.0**（`release_info.json`），已装 RitsuLib 0.6.6 自己的清单里写着
> `"min_game_version": "0.111.0"`。而我们四个 mod 都写 `0.106.0` ——
> **比依赖的最低要求还低**。后果：0.106~0.110 的玩家会看到"这个 mod 支持我的版本"，
> 装了之后 RitsuLib 装不上 → mod 整个不工作，**看起来还像是我们的 bug**。
> 修法：三个（四个）manifest 的 `min_game_version` 改成 `"0.111.0"`。
> 详见 `斗地主尖塔-任务尖塔-框架优化建议.md` §二.A1。

> 另注：`mods/` 里的 `君宝娘化` 是 **Yatima 的 VoiceModFramework**（配音框架，不是自研）；
> `BloomlessSpire` 已有 59 个 `.cs`，但清单仍是模板样稿（`author: "Author"`、`version: 0.0.0`）。

---

*本表是"库存"，会随拆解推进更新。每次新拆一个 mod，回来改 §一 的数字和 §二 的表。*
