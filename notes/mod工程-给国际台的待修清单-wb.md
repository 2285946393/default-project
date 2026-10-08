# mod 工程 · 给国际台的待修清单（发件：国内台 wb，2026-10-08）

> **收件人**：国际版 WorkBuddy（工作目录 `C:\Users\有花无实\WorkBuddy AI\2026-10-07-23-28-27`）。
> 你正在修 mod 源码线（csproj / manifest / 部署）。
> 用户原话：「**mod 我让国际版在修。你把问题告诉他就行了。**」
> ⇒ 我（国内台）**只出结论 + 证据，一个字节都不动你的文件**。
>
> 你之前已落的 `1d`（`SyncManifestDependencies`，4 个 csproj + 上游 `RitsuLibModTemplate`），
> 我逐条复核过，**那部分是对的**。这份只讲**还没做的**，外加**两条必须先纠正的事实**。

---

## 〇、三分钟版

| # | 要做什么 | 摸哪些文件 | 优先级 |
|---|---|---|---|
| ⚠️ **0** | **先纠正**：RitsuLib 两份安装**都是 0.6.6**，不是 0.6.7 | — | 先看，不然下面会做反 |
| ⚠️ **0b** | **先纠正顺序**：`1c` **不能单独做** —— 你已经把 0.6.7 同步进仓库了，一改键名就会把两个 mod 拦死 | 见 §二 | ★★★ |
| **1e** | `Version="*"` → 钉死 `"0.6.6"` | 5 个 csproj（各 1 行） | ★★★ |
| **1c** | 依赖键 `version` → `min_version` | 4 个 manifest + 5 个 csproj（各 2 行，**别动第 3 行**） | ★★★ |
| **1a′** | `min_game_version` 重议 | 3 个 manifest | ★★★（要先取证） |

**一句话**：`1e` 和 `1c` 必须**同一批**做，而且 manifest 里的依赖值要一起写成 **`0.6.6`**。

---

## 一、⚠️ 先纠正 A：你写的「mods/ 那份是 0.6.7」不成立

你在 `notes/斗地主尖塔-任务尖塔-框架优化建议.md` §二.A1 的「追加发现：RitsuLib 有两份安装」里写：

| 位置 | 你写的版本 |
|---|---|
| 工坊 `3747602295/` | 0.6.6 |
| 游戏 `Slay the Spire 2/mods/STS2-RitsuLib/` | **0.6.7** ← **这条不成立** |

**我实测（Read 工具原文 + Python 直读，**没用** cat/grep）——两份的 `"version"` 字段都是 `0.6.6`：**

`D:\software\steam\steamapps\common\Slay the Spire 2\mods\STS2-RitsuLib\mod_manifest.json`
```json
  "version": "0.6.6",
  "min_game_version": "0.111.0"
```

`D:\software\steam\steamapps\workshop\content\2868840\3747602295\mod_manifest.json`
```json
  "version": "0.6.6",
  "min_game_version": "0.107.1"
```

**时间戳也对不上你的记录：**
- 游戏那份 mtime = `1791292180` = **2026-10-06 21:09:40**（你记的是 10-08 10:50）
- 工坊那份 mtime = `1791292813` = **2026-10-06 21:20:13**
- ⇒ 游戏那份**从 10-06 起就没被改过**，不可能在 10-08 10:50 变成 0.6.7。

**这大概率就是你自己总结的那条坑**（Git Bash 的 `cat`/`grep` 会读到陈旧内容）——你把规矩写下来了，还是踩了一次。
（另一种可能：你读的是 `compat/0.111.0/` 里运行时 dll 的版本号，那是另一回事。）

**为什么必须先纠正**：如果按 0.6.7 去钉 `min_version`，
会把所有「RitsuLib = 0.6.6」的玩家**全部拦在门外**（`DEPENDENCY_VERSION_UNSUPPORTED`）——
正好是我们最不想要的失败模式。**游戏实际加载的是 0.6.6，只能钉 0.6.6。**

> 你对变体机制的理解是对的，我复核过：工坊版 `compat/` = `0.107.1 / 0.109.0 / 0.110.0 / 0.111.0`
> （`ritsulib-variants.manifest` **1956 B**）；安装副本 `compat/` 只剩 `0.111.0`
> （`variants.manifest` **629 B**），`min_game_version` 被改写成 `0.111.0`。
> 但**依赖声明校验的是 `mod_manifest.json` 的 `version` 字段，不是 compat 分支** ⇒ 结论仍是 0.6.6。

---

## 二、⚠️⚠️ 先纠正 B：`1c` 不能单独做 —— 你已经把 0.6.7 写进仓库了

**这是本次最要紧的一条。** 你把 `1d` 修好之后（版本来源改成"游戏目录优先"），
它开始**可靠地把 NuGet 解析到的版本写进 manifest**。实测后果：

### 现在四个 manifest 的依赖值（仓库源码）

| 文件 | RitsuLib 依赖值 | 游戏那边实际装的是 |
|---|---|---|
| `projects/dou-spire/DouSpire.json` | `"0.6.6"` | 0.6.6 ✅ |
| `projects/quest-spire/QuestSpire.json` | **`"0.6.7"`** | 0.6.6 ❌ |
| `projects/frost-spire/FrostSpire.json` | **`"0.6.7"`** | 0.6.6 ❌ |
| `projects/BloomlessSpire/BloomlessSpire.json` | `"0.6.2"` | 0.6.6 ⚠️（能满足，但陈旧） |

问的是 **`project.assets.json` 解析出的**：dou-spire → `0.6.6`；**quest-spire → `0.6.7`；frost-spire → `0.6.7`**
（NuGet 缓存 `~/.nuget/packages/sts2.ritsulib/` 里有 `0.6.2 / 0.6.5 / 0.6.6 / 0.6.7` 四个版本）。

### 游戏目录里已部署的那份

| mod | 部署副本的 RitsuLib 依赖 |
|---|---|
| `mods/DouSpire/DouSpire.json` | `"0.6.6"` ✅ |
| `mods/QuestSpire/QuestSpire.json` | `"0.6.6"`（源码已是 0.6.7 → **下次构建就会变**） |
| **`mods/FrostSpire/FrostSpire.json`** | **`"0.6.7"`** ← **已经躺在游戏目录里了** |

### ⇒ 后果（这就是"交叉结论"落地的样子）

**现在这么做没事**，因为键名是错的（`version`），游戏**读都不读**，约束恒为零。
**但只要 `1c` 一落地**：

- 键名变成 `min_version` → 游戏开始真的校验；
- `mods/FrostSpire` 里的 `0.6.7` > 游戏装的 `0.6.6` → **`DEPENDENCY_VERSION_UNSUPPORTED` → FrostSpire 直接不给加载**；
- QuestSpire 会在**下一次构建**后跟上，一样死。

**⇒ 这是我上一轮预测的「改好之后它会更危险」的具体发生**：
目标修好了 ⇒ 稳定写值 ⇒ 值比玩家装的新 ⇒ 一改键名就集体翻车。
**所以：`1e`（钉死 0.6.6）+ 把四个 manifest 的依赖值统一写成 `0.6.6`，要和 `1c` 同批做完。**

---

## 三、1c ★★★ 键名 `version` → `min_version`

### 3.1 为什么要做

游戏 v0.111.0 的 `ModDependency`（反编译源码，
`projects/sts2-moddev-workspace/02-游戏反编译资料/版本-0.111.0/反编译源码/MegaCrit.Sts2.Core.Modding/ModDependency.cs`）
**只有两个字段**：

```csharp
public class ModDependency {
    [JsonPropertyName("id")]          public string  id;
    [JsonPropertyName("min_version")] public string? minVersion;   // ← 唯一的版本字段
}
```

写 `version` → `System.Text.Json` **静默忽略** → `minVersion = null`
→ `ModManager` 里 `else if (declaredDependency.minVersion != null)` **整段版本校验被跳过**。
⇒ 依赖的**存在性**还查（`id` 读得到），**版本**完全不查。

> 全生态实测（121 个工坊 manifest + 我们自己的，共 65 个依赖条目）：
> `min_version` **48** / `version` **10** / `min` 1 —— **那 10 个 `version` 全是我们自己的。**

### 3.2 四个源 manifest（`dependencies[]` 里每条都要改）

**共 8 条**（不是 7 —— `DouSpire 2 + QuestSpire 2 + FrostSpire 3 + BloomlessSpire 1 = 8`；
之前记的"7 处"是算错了一处）：

```jsonc
// projects/dou-spire/DouSpire.json        ← 2 条
{ "id": "STS2-RitsuLib", "version": "0.6.6" }   → "min_version": "0.6.6"
{ "id": "BaseLib",       "version": "v3.4.7" }  → "min_version": "v3.4.7"

// projects/quest-spire/QuestSpire.json     ← 2 条（值要顺手改成 0.6.6，见 §二）

// projects/frost-spire/FrostSpire.json     ← 3 条（MinionLib 0.6.3 那条也是 version）
{ "id": "MinionLib",     "version": "0.6.3" }   → "min_version": "0.6.3"

// projects/BloomlessSpire/BloomlessSpire.json ← 1 条
```

「`v3.4.7` 这种带 v 的能过吗？」—— **能**。`SemanticVersion.cs:66` 显式跳过开头的 `v`。
`0.6.6` / `v3.4.7` / `0.6.3` 三个值都**满足**现有要求，所以这是**收紧**，不是回退。

> `projects/meme-spire/MemeSpire.json`（梗化尖塔）**没有 `dependencies`** → 不受影响。我全仓扫过了。

### 3.3 五个 csproj —— ⚠️ 只有 2 行改，第 3 行**千万别动**

每个 csproj 里有 **3 行**含 `"version"`，但语义完全不同：

| 行 | 变量 | 它读/写谁 | 动作 |
|---|---|---|---|
| 1 | `_RitsuVerPattern` | 读 **RitsuLib 自己的** `mod_manifest.json`（那份**合法地**写 `version`） | **保持 `version`** ❌改了就取不到版本号 |
| 2 | `_Pat` | 匹配**我们**manifest 里的依赖条目 | → **`min_version`** |
| 3 | `_Rep` | 写回**我们**manifest 的依赖条目 | → **`min_version`** |

精确位置（行号）：

| 文件 | `_RitsuVerPattern` | `_Pat` | `_Rep` |
|---|---|---|---|
| `projects/dou-spire/DouSpire.csproj` | 142 | **158** | **169** |
| `projects/quest-spire/QuestSpire.csproj` | 105 | **121** | **130** |
| `projects/frost-spire/FrostSpire.csproj` | 123 | **139** | **148** |
| `projects/BloomlessSpire/BloomlessSpire.csproj` | 124 | **140** | **149** |
| `projects/sts2-mod/RitsuLibModTemplate/RitsuLibModTemplate.csproj` | 125 | **141** | **150** |

（最后那个是**上游模板本身**——你已经修过它一次，这次也一起，免得再被克隆出去。）

### 3.4 反证：上游模板本来就是对的

`projects/ref-modtemplate/keyfiles/ModTemplate.csproj:79`（Alchyr 官方模板）用的是：

```xml
::Replace($(OldContent), '("id":\s*"BaseLib",\s*"min_version":\s*")[^"]*', '${1}$(ActiveBaseLibVersion)')
```

**作者写的就是 `min_version`。**
错的只有我们自己那份从 RitsuLib 模板抄来的。第三方参考 `projects/ref-combatsolver/CombatSolver.json`
与 `projects/ref-wuwancients/wuwancients.json` 也都是 `min_version`。

---

## 四、1e ★★★ `Version="*"` 钉死成 `"0.6.6"`

5 处：

| 文件 | 行 |
|---|---|
| `projects/dou-spire/DouSpire.csproj` | 31 |
| `projects/quest-spire/QuestSpire.csproj` | 30 |
| `projects/frost-spire/FrostSpire.csproj` | 34 |
| `projects/BloomlessSpire/BloomlessSpire.csproj` | 32 |
| `projects/sts2-mod/RitsuLibModTemplate/RitsuLibModTemplate.csproj` | 32 |

`<PackageReference Include="STS2.RitsuLib" Version="*" GeneratePathProperty="true"/>` → `Version="0.6.6"`

### 为什么这条比"版本漂"更严重

quest-spire / frost-spire 现在是**拿 0.6.7 编、在 0.6.6 上跑**。
哪天代码用到一个 0.6.7 才有的 API：**编译期一切正常，运行期 `MissingMethodException`，
而且编译日志里一句话都没有。** 这类崩溃玩家只会说"装了你的 mod 就崩"。

（模板作者的 `Alchyr.Sts2.BaseLib` 也是 `Version="*"` —— 别照抄这个习惯。）

---

## 五、1a′ ★★★ `min_game_version` 重议（这条要你拍板，我不替你定）

**现状**：DouSpire / QuestSpire / FrostSpire 三个都写 `"0.111.0"`。
**依据已被推翻**：那个值当时是照"RitsuLib 0.6.6 要求 0.111.0"推出来的 —— 假的。
RitsuLib 0.6.6 实际支持 **0.107.1 / 0.109.0 / 0.110.0 / 0.111.0** 四个游戏版本
（工坊版 `min_game_version` 写的是 **`0.107.1`**，取最低）。

**但不能直接改回 0.107.1** —— 我们的 dll 是拿 0.111.0 的 `sts2.dll` 编的，
"代码只用 ≤0.107 的 API"**没有任何证据**。要动就得先取证。

**取证方法**（`sts2.xml` 那条，我们在别的项目上验证过有效）：
```bash
XML=projects/dou-spire/.godot/mono/temp/bin/Debug/sts2.xml   # 游戏官方 XML 文档，构建后就有
grep -oE 'M:MegaCrit\.Sts2\.Core\.[A-Za-z0-9_.]+' "$XML" | sort -u > /tmp/api.txt
# 再把我们在 DouSpireCode/ 里调用的 MegaCrit.* 符号抽出来，逐条比对是否都在
```

**我的建议**：**先别动**。等 `1c`/`1e` 落地、跑一局稳定之后，单独做一次 API 下限核查再定。
（这条我已经写进 `notes/斗地主尖塔-任务尖塔-框架优化建议.md` §六 表里的 `1a′`。）

---

## 六、低优先（顺手可做，不做也不影响）

| # | 事 | 说明 |
|---|---|---|
| 7 | BaseLib 从硬编码工坊路径改成 `PackageReference Include="Alchyr.Sts2.BaseLib"` | 上游模板就是这么做的。现在是 `$(Sts2Dir)\..\..\workshop\content\2868840\3737335127\BaseLib\BaseLib.dll` 硬编码 |
| 8 | 删掉 manifest 里死字段 `pck_name` | 游戏 v0.111.0 的 `ModManifest` 没这个字段（会被忽略），pck 路径写死 `<modId>.pck`。⚠️ 但 **RitsuLib 自己的 manifest 也写了 `pck_name`**，属"无害冗余"，不急 |
| 11 | `sts2` / `Steamworks.NET` 的 `<Reference>` 都加 `Private="False"` | 模板规矩（编跨平台 MSIL）。BaseLib 那行已经是 `Private="False"` 了 |

---

## 七、改完怎么自证（照着跑）

1. **manifest 层**：四个 json 里 `dependencies[].*` 的键名应只有 `min_version`、没有 `version`，
   且 RitsuLib 那条的值是 **`0.6.6`**：
```bash
python -c "
import io,glob,json
for f in glob.glob('projects/*/*.json'):
    try: j=json.load(io.open(f,encoding='utf-8-sig'))
    except Exception: continue
    if not isinstance(j,dict) or not j.get('dependencies'): continue
    print(j['id'], json.dumps(j['dependencies'],ensure_ascii=False))
"
```
2. **csproj 层**：`grep -c 'min_version' <csproj>` 应为 **2**（`_Pat`/`_Rep`）；
   第 1 行 `_RitsuVerPattern` 里仍应是 `&quot;version&quot;`。
3. **端到端（真验收）**：构建一次，然后 **Read 游戏目录里的部署副本**
   `D:\software\steam\steamapps\common\Slay the Spire 2\mods\<Mod>\<Mod>.json`，
   确认是 `"min_version": "0.6.6"`。
   **这一步才是真验收** —— 因为"部署副本是旧的"正是 `1d` 那个陷阱会骗过你的地方。

---

## 八、我的边界（避免我们再互相覆盖）

- **我只读、没写**（除本文件）。上面每条结论都有命令输出 / Read 原文支撑，可复现。
- 我**不碰** `projects/{dou-spire,quest-spire,frost-spire,BloomlessSpire}/` 下的
  任何 `.cs` / `.csproj` / `.json`。
- **我不动 `1d` 的既有成果** —— 你写的那版是对的，别为了 `1c` 一起重写（**只改 2 行**）。
- 你把 `1c`/`1e` 做完后，建议在 `框架优化建议.md` §六 表里把 `1c`/`1e` 两行改成 ✅（那张表一直是你在维护）。

---

## 九、证据索引

| 结论 | 证据（本轮实测） |
|---|---|
| RitsuLib 两份都是 0.6.6 | Read `...\mods\STS2-RitsuLib\mod_manifest.json` + `...\workshop\content\2868840\3747602295\mod_manifest.json` |
| 部署副本 mtime 10-06，不可能 10-08 变 0.6.7 | `1791292180` → 2026-10-06 21:09:40 |
| 4 个源 manifest 键名错 / 值不一致 | Python 直读 `projects/*/*.json` |
| 5 个 csproj 仍写 `version` | `grep -n '&quot;version&quot;' --include=*.csproj` |
| 上游模板本身用 `min_version` | `projects/ref-modtemplate/keyfiles/ModTemplate.csproj:79` |
| NuGet 漂移 | `~/.nuget/packages/sts2.ritsulib/` = 0.6.2/0.6.5/0.6.6/0.6.7；`project.assets.json` → dou 0.6.6 / quest 0.6.7 / frost 0.6.7 |
| 工坊 vs 安装副本 = 裁剪关系 | `compat/` 4 个分支 vs 1 个；`variants.manifest` 1956 B vs 629 B |

---

## 十、延伸阅读（都是现成的，不用重拆）

- `notes/杀戮尖塔2-拆解-ModTemplate工程模板.md` —— 工程主干权威口径（§3.5 依赖同步 / §7 连带修正 / §八 可抄清单）
- `notes/斗地主尖塔-任务尖塔-框架优化建议.md` —— §二.A1（三条 bug）+ §六 任务表
- `projects/ref-modtemplate/keyfiles/` —— 官方模板关键文件速查
- 反编译权威源：`projects/sts2-moddev-workspace/02-游戏反编译资料/版本-0.111.0/反编译源码/MegaCrit.Sts2.Core.Modding/`

---
*发件：国内台 wb · `C:\Users\有花无实\WorkBuddy\2026-10-08-00-26-18` · 会话 2026-10-08*
