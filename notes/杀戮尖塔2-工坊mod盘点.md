# 盘点 · 杀戮尖塔 2 创意工坊 mod 库存

> **数据来源**：本机 Steam 创意工坊目录
> `D:/software/steam/steamapps/workshop/content/2868840/`
> **扫描时间**：2026-10-08 02:10 · **共 121 个 mod**
>
> **怎么更新这份**：见文末「§七 复现方法」。

---

## 一、总览

| 指标 | 数量 |
|---|---|
| 工坊 mod 总数 | **121** |
| **已拆**（有反编译产物 + 笔记） | **6** |
| 半拆（只看了开源仓库，未反编译） | 1 |
| **未拆** | **114** |
| 今天（10-08）目录被动过 | 11 |
| 带 dll | 105 |
| 带 pck | 87 |
| 零依赖 | 72 |

---

## 二、已拆（6 个）

| 工坊 id | 名字 | 作者 | 产出 | 核心看点 |
|---|---|---|---|---|
| `3747497501` | 万象辉星 [RegentFX] | — | `projects/ref-regentfx/`（79 `.cs`） | 美术层：`StartPos` 约定、`FitVfx`、pck 三个坑 |
| `3774274248` | 动作与特效 | — | `projects/ref-meleeattack/`（76 `.cs`） | 演出层：打击感六件套、位移状态机 |
| `3798368044` | More Ironclad Animations（id `ShieldOnly`） | page | `projects/ref-shieldonly/`（3 dll，26,917 行） | 工程层：免 pck 读贴图、`RuntimeAsset`、内联着色器 |
| `3790899961` | 战斗路线求解器（CombatSolver） | Torch | `projects/ref-combatsolver/`（213,836 行） | ★ Anytime 束搜索、三态药水策略、影子世界推演、内存工程 |
| `3747583646` | 鸣潮先古（wuwancients） | supermanpower | `projects/ref-wuwancients/`（22,957 行） | ★ 自定义先古 + 确定性随机（不动游戏随机流） |
| `3804485262` | 假如塔2是类幸存者（`local.action_game`） | 普通网友Cano | `projects/ref-actiongame/`（14,690 行） | ★ 定时器把回合制"切片"成实时 + 89 个解阻塞补丁 |

对应笔记：`notes/杀戮尖塔2-特效实战*.md`（前 3 个）· `notes/杀戮尖塔2-拆解-*.md`（后 3 个）

## 三、半拆（1 个）

| 工坊 id | 名字 | 情况 |
|---|---|---|
| `3747531952` | 随机数预测（RandomForeseer，作者 hotwords123） | **MIT 开源**，GitHub `hotwords123/StS2.RandomForeseer` 有**真源码**。看了 README 与目录结构，**未反编译**。它是 CombatSolver 的模拟核心来源。 |

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
| `BaseLib` | 25 | 提供 `CustomAncientModel` 等**内容注册口子**（鸣潮先古用的） |
| `STS2-RitsuLib` | 23 | 提供 **patcher / 生命周期 / 模型能力**框架（CombatSolver 用的） |
| `JmcModLib` | 3 | 另一套小框架 |
| `aemeath-ww` | 2 | |
| `MinionLib` / `tune_strain` / `ModConfig` / `MomoLib` | 各 1 | |

> **结论**：想做大改造，两条主流路径是 **BaseLib（塞内容）** 和 **RitsuLib（改框架）**。
> 两个都**还没拆**，是目前最该补的洞。

---

## 六、值得拆的候选（我按"能学到多少"排的）

| 优先 | 工坊 id | 名字 | 为什么值得 |
|---|---|---|---|
| ★★★ | `3747602295` | **RitsuLib** | 被 **23 个 mod** 依赖、CombatSolver 的底座。拆它 = 一次搞懂半个生态的框架层。**本机 `mods/` 里也装了** |
| ★★★ | `3737335127` | **BaseLib** | 被 **25 个 mod** 依赖，先古/内容注册的口子都在这（v3.4.7） |
| ★★ | `3779807977` | 弹幕尖塔 DanmakuSpire | 之前列为待拆；玩法改造，和"类幸存者"可对照 |
| ★★ | `3772226486` | 中国人能飞 CombatFlight | 之前列为待拆；依赖 ModConfig，是**给原版加机制**的样本 |
| ★★ | `3747531952` | 随机数预测 RandomForeseer | **MIT 有真源码**，比反编译省事得多；是 CombatSolver 的上游 |
| ★ | `3747819202` | Hades Ancients | 和"鸣潮先古"同题材（先古线），可横向对比 |
| ★ | `3747588119` | Kafka（今天新下） | 鸣潮/星穹角色系 |
| ★ | `3777276296` | BetterAnimation2（今天新下） | 动画增强，和我们的特效线同域 |
| ★ | `3810696310` | More Defect Animations（今天新下） | 角色动画，同上 |
| ★ | `3747729740` | 无尽模式（今天新下） | 玩法模式扩展 |
| — | `3747575739` | More Enchantments | 原版机制扩展（附魔） |
| — | `3797367057` | 一代遗物 | 跨代内容移植 |

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

---

## 八、和 `AGENTS.md` 的关系

本机 `Slay the Spire 2/mods/` 里（游戏实际加载的）是**自己写的 6 个 + RitsuLib**：
`BloomlessSpire` / `DouSpire` / `FrostSpire` / `MemeSpire` / `QuestSpire` / `STS2-RitsuLib` / `君宝娘化`。
工坊那 121 个是**订阅下来的参考样本**，两者不要混。

---

*本表是"库存"，会随拆解推进更新。每次新拆一个 mod，回来改 §一 的数字和 §二 的表。*
