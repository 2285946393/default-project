# ref-randomforeseer · 随机数预测 RandomForeseer

> 工坊 `3747531952` · 作者 **hotwords123** · v0.13.18 · **MIT 开源**
> 上游：https://github.com/hotwords123/StS2.RandomForeseer
> 拆解笔记：`notes/杀戮尖塔2-拆解-随机数预测-20261009.md`
> 上游原文 README 见 `README-upstream.md` / `README.en.md`。

**这个 mod 是生态里唯一"把整场战斗克隆到旁边重跑一遍"的样本。**
做法 = **克隆 RNG + 影子状态 + 逐个镜像卡牌行为**，全程不推进真实随机数。
→ 斗地主 AI / 战斗求解器的直接参照（CombatSolver 的模拟核心来自它）。

---

## 目录说明

| 路径 | 是什么 |
|---|---|
| `src/RandomForeseerCode/` | **真源码 5,954 行**（MIT）。核心是 `InCombat/Simulation/`（2,438 行模拟器）+ `Common/Mirrors/` + `InCombat/Mirrors/` + `OutOfCombat/` |
| `docs/` | **作者写的设计文档 30 份**。`hooks/` 23 份（每个钩子家族一份覆盖说明）+ `mirrors/` 7 份 + `version-adaptations/` 4 份（v0.108~111）+ `workshop-variant-distribution.md` |
| `tests/` | 测试项目（`GameTestBase` / `TestCombat` / `CombatStateProxy` / 各功能测试） |
| `workshop/` | **多变体分发包**：`loader/Bootstrap.cs`（分发器，抄自 RitsuLib）+ `active-game-versions.txt` + 文案 |
| `manifest.json` | 工坊实际清单（`has_pck:false`，依赖 RitsuLib 0.5.14） |
| `manifest-upstream.json` | 上游仓库里的清单（0.13.18 / min_game 0.111.0） |
| `mod-variants.manifest` | ★ 变体清单（注意扩展名是 `.manifest`，内容是 JSON） |
| `build-info-0.13.18.txt` | 构建信息（git-commit / git-ref / built-at） |
| `THIRD_PARTY_NOTICES.md` | 声明加载器设计抄自 STS2-RitsuLib（MIT） |
| `decompiled/` | 反编译产物（备查；源码已有就不必看） |
| `pck/` | 从变体 pck 里抽出来的资源（如有） |
| `README-upstream.md` / `README.en.md` / `CHANGELOG.md` / `LICENSE` | 作者原文 |

---

## 关键文件速查

| 想知道什么 | 看哪 |
|---|---|
| **游戏 RNG 机制** | 游戏本体 `MegaCrit.Sts2.Core.Random/Rng.cs` + `Runs/RunRngSet.cs` + `Random/PlayerRngSet.cs`；或笔记 §2 |
| 怎么克隆 RNG | `src/.../Common/PredictionExtensions.cs`（`Clone` / `Advance` 两行） |
| 克隆 RNG 集合 | `src/.../InCombat/Simulation/CombatPredictionRngSet.cs`（7 条流） |
| 镜像注册表核心 | `src/.../Common/Mirrors/MethodMirrorRegistry.cs`（5 种分发状态 + 缓存） |
| 镜像方法规格 | `src/.../Common/Mirrors/MirrorMethodSpec.cs`（`TryGetOverride`） |
| 风险模型 | `src/.../Common/PredictionRisk.cs`（5 种风险原因） |
| 影子状态 | `src/.../InCombat/Simulation/CombatPredictionState.cs` + `Common/SimCardPile.cs` |
| 影子卡 | `src/.../Common/PredictedCard.cs`（`Original` / `Preview` / `MutablePreview`） |
| 对象克隆 | `src/.../Common/PredictionCloner.cs` |
| 模拟器主体 | `src/.../InCombat/Simulation/CombatPredictionSimulator*.cs`（13 个 partial） |
| IL 推断 | `src/.../InCombat/Mirrors/Cards/OnPlay/CardOnPlayInferrer.cs`（用 RitsuLib 的 `HarmonyIl`） |
| 多变体分发器 | `workshop/loader/Bootstrap.cs` |
| 版本适配方法论 | `docs/version-adaptations/v0.111.0.md` 等 4 份 |
| 钩子覆盖原则 | `docs/hooks/overview.md` |
| 卡牌 OnPlay 覆盖（最详） | `docs/mirrors/card-on-play.md`（15.7KB） |
| 入口 | `src/.../Entry.cs` |

---

## 一句话结论（给国内 WB 用）

**要抄的三样**：
1. **克隆 RNG**：`new Rng(rng.ToSerializable())`；`SerializableRng = counter + 4×ulong`（Xoshiro256），**能联网传输**。
2. **影子状态**：改动全落在自己的 `Sim*` 结构上，真 `CombatState` 只读；"影子死亡"用 `HashSet` 记。
3. **镜像 + 风险记账**：按**精确运行时类型**分发；推断出来的结果**强制**打"不完整"标记；不确定就在 UI 上明说。

**不用抄的**：`CardOnPlayInferrer` 那套 IL 推断 —— 那是因为它要兼容**别人写的卡牌**。
斗地主是我们自己的玩法，不需要猜。
