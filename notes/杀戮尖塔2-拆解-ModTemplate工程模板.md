# 拆解 · ModTemplate-StS2 —— 塔2 mod 的"工程模板"（附 StS2ModAnalyzers）

> **仓库**：https://github.com/Alchyr/ModTemplate-StS2 · 作者 **Alchyr**（就是 BaseLib 的作者）
> **星标** **299★ / 50 fork** · 默认分支 **`master`** · 最后推送 **2026-08-22**
> **协议**：仓库根目录**没有 LICENSE 文件**；但模板包在 `Alchyr.Sts2.Templates.csproj` 里写了
> `<PackageLicenseExpression>MIT</PackageLicenseExpression>`（**所以按 MIT 看待**）。
> **形态**：**不是 mod，是一个 `dotnet new` 模板包**（`Alchyr.Sts2.Templates` v2.5.2）。
> 源码快照：`projects/ref-modtemplate/upstream/ModTemplate-StS2-master/`（**已 gitignore**）
>
> **附带**：`Alchyr/StS2ModAnalyzers` —— Roslyn 分析器/修复器，NuGet 包 `Alchyr.Sts2.ModAnalyzers` v0.2.1。
> ⚠️ **这个仓库只有 0★ / 1 fork**（我们之前的盘点笔记把它标成 ★★，那是按"用处"评的，**不是按热度**，
> 这里更正一下口径）。协议**未声明**。快照在 `projects/ref-analyzers/upstream/`（已 gitignore）。
>
> **一句话**：这份东西**不教你怎么写玩法，它定义"一个正规的塔2 mod 工程长什么样"** ——
> 路径怎么找、依赖版本怎么不漂移、怎么部署、怎么导出 pck、manifest 哪些字段是真的。
> **我们今天踩的坑，全部落在这个范围里。**

> ⚠️ **本笔记修订过两次，看的时候留意**：
> 1. **10-08 下午**：初稿把"我们那个同步目标为什么不行"归因成"守卫条件为空 → 静默跳过"。
>    **这条被实验证伪了**（另一位协作者做的实验，我复核过 —— `PkgSTS2_RitsuLib` 有真实路径，
>    而且故意写坏能被改回来）。真因见 **§3.5**。**我保留被证伪的那段并标注了，别只看结论。**
> 2. 同时补上一条更隐蔽的交叉结论：**同步目标修好之后反而更危险**（它会可靠地往 `version`
>    这个游戏不读的键里写）→ **`§八-1` 的键名修正必须先做**。

---

## 术语小抄（先说人话）

| 词 | 人话 |
|---|---|
| `dotnet new` 模板 | 一条命令生成一个工程骨架的"标准模板"，就像新建 Word 文档时的模板。这个仓库就是三份模板 |
| `csproj` | C# 工程文件，告诉编译器"编译哪些文件、引用哪些东西、编完干什么" |
| `.props` | 一段被 csproj `Import` 进来的配置片段，用来放"换个机器就不一样"的东西 |
| `MSBuild Target` | csproj 里的一个"构建时执行的步骤"，可以挂在构建前/后 |
| `project.assets.json` | NuGet 在 `obj/` 下生成的**依赖解析结果**文件：**实际**用了哪个版本，白纸黑字写在这 |
| manifest | 每个 mod 根目录里的那个 `.json`，游戏的"身份证"，见 §四 |
| pck | Godot 的资源包（贴图/场景打包成一个文件），游戏自己加载 |

---

## 一、为什么先拆它（而不是再拆一个玩法 mod）

盘点笔记 §六 里它排第一，理由现在有了实测支撑：

**我们今天（10-08）花了一整天修的三个坑，全是工程结构问题，不是玩法问题：**

| 今天踩的坑 | 性质 |
|---|---|
| `min_game_version` 写在 manifest 里，没人管它，跟依赖对不上 | **工程元数据** |
| `SyncManifestDependencies` 这个 MSBuild 目标**装死**（条件不成立 → 静默跳过 → 不报错） | **构建脚本** |
| 交付产物"编译过了"但部署副本是旧的 | **部署环节** |

**这份模板正好逐条回答"正规工程怎么做这三件事。** 拆它的性价比比再拆一个弹幕 mod 高得多。

---

## 二、它长什么样

一个 `dotnet new` 模板包，`content/` 下三份模板：

| 模板 | 路径 | 给你什么 |
|---|---|---|
| **Empty**（裸） | `content/ModTemplate/` | 一个空 mod：`MainFile.cs` + manifest + 图片占位 + Godot 工程 |
| **Content**（内容） | `content/ContentModTemplate/` | 上面 + **卡牌/能力/遗物**基类 + **6 个本地化 json** 骨架 |
| **Character**（角色） | `content/CharacterModTemplate/` | 上面 + **卡池/遗物池/药水池/先古** + 一整套 `charui/` 图（选人立绘、能量图标、地图标记…） |

三份 csproj **几乎完全一样**（`diff` 只差模板名占位符和几个空 `Folder` 声明）——
**说明"工程主干"是稳定的，只有内容骨架不同。** 这本身就是一个结论。

---

## 三、工程主干逐段拆（★ 对我们最值钱）

### 3.1 游戏安装路径：**自动发现**，不用手写

`Sts2PathDiscovery.props`（**入库**，不含任何本机信息）：

```xml
<!-- Windows：先查注册表，再查 Steam 默认路径 -->
<RegistrySts2Path>$([MSBuild]::GetRegistryValueFromView(
  'HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 2868840',
  'InstallLocation', '', RegistryView.Registry64, RegistryView.Registry32))</RegistrySts2Path>
<AutoSteamPath>$(registry:HKEY_CURRENT_USER\Software\Valve\Steam@SteamPath)\steamapps</AutoSteamPath>
...
<Sts2Path Condition="'$(Sts2Path)' == '' and Exists('$(SteamLibraryPath)/common/Slay the Spire 2')">...</Sts2Path>
<ModsPath>$(Sts2Path)/mods/</ModsPath>
<Sts2DataDir>$(Sts2Path)/data_sts2_windows_x86_64</Sts2DataDir>
```

**三个要点**：
1. **`Steam App 2868840`** 是塔2的 Steam AppID —— 注册表里认这个号就够，不用猜盘符。
2. **`data_sts2_windows_x86_64`**：游戏 dll（`sts2.dll` / `0Harmony.dll`）就在这个子目录里。
3. 三平台都写了（Win / Linux `data_sts2_linuxbsd_x86_64` / macOS 在 `.app/Contents/Resources/...`）；
   本机路径被覆盖的出口是 **`Directory.Build.props`**，而它**被 `.gitignore` 忽略**（见 3.7）。

> **对比我们**：我们每个 mod 一份手写 `local.props`（也让 .gitignore 忽略了，思路一致），
> 但**路径是硬编码的**，换机器/换盘符要手改；而且 csproj 里还额外写了一条
> `$(MSBuildProgramFiles32)\Steam\...` 的默认值，**在本机是错的**（我们装在 D 盘），
> 只是靠 `local.props` 兜住了。**能抄的就这一段。**

### 3.2 开不了工就**报错**，不装作没事

```xml
<Project Sdk="Godot.NET.Sdk/4.5.1" InitialTargets="CheckDependencyPaths">
...
<Target Name="CheckDependencyPaths">
  <Error Text=" Slay the Spire 2 data not found at path '$(Sts2DataDir)'"
         Condition="'$(Sts2DataDir)' == '' or !Exists('$(Sts2DataDir)')" />
</Target>
```

`InitialTargets` = **一进来就跑，在任何构建动作之前**。找不到游戏 → **`<Error>` 硬失败**。

> **这就是我们那个装死 target 的反面教材。**
> 我们的 `SyncManifestDependencies` 把一个必须做的事挂在 `Condition="'$(PkgSTS2_RitsuLib)' != ''"` 上，
> 条件不成立时 **MSBuild 静默跳过、连警告都没有** —— 于是"安全网"变成了"假装有安全网"。
> **规矩：构建脚本里"必须成立"的前提，要么 `<Error>`，要么至少 `<Warning>`，绝不静默跳过。**
> （我们确实也有一个 `ValidateSts2GameInstall` 做对了这件事 —— 见 §五。）

### 3.3 引用游戏 dll 的正确姿势

```xml
<ItemGroup Condition="Exists('$(Sts2DataDir)')" >
  <Reference Include="0Harmony"><HintPath>$(Sts2DataDir)/0Harmony.dll</HintPath><Private>false</Private></Reference>
  <Reference Include="sts2"><HintPath>$(Sts2DataDir)/sts2.dll</HintPath><Private>false</Private></Reference>
</ItemGroup>

<!-- 架构不匹配的警告：关掉 -->
<NoWarn>$(NoWarn);MSB3270</NoWarn>
```

`Private=false` = **不把游戏自己的 dll 复制进我们的产物**（游戏从它自己的目录加载，复制一份进去只会打架）。
`MSB3270` 关掉的理由，模板里写了注释（这句很值）：

> *"mods are ideally built for all platforms (MSIL) while your local StS2 version will be for your specific platform."*
> —— **mod 应该编成"跨平台的中间码"，而本机游戏是特定平台的**，所以架构不匹配是**预期之内**，不该报警。

**可选开关** `Publicize`（默认 **关**）：
```xml
<PackageReference Include="Krafs.Publicizer" Version="2.3.0" PrivateAssets="All"/>
<Publicize Include="sts2" IncludeVirtualMembers="false" IncludeCompilerGeneratedMembers="false" />
```
把游戏 dll 里 `private/protected` 的成员**变公开**，编译期就能直接调用 ——
比反编译贴心，但模板注释也老实说了 *"may (unlikely to) cause errors"*。
我们**恒定开着** Publicizer（`Krafs.Publicizer 2.2.1`），模板是**可选**。这条不算错，但要知道自己是"激进档"。

### 3.4 依赖怎么引：**走 NuGet，不走硬编码路径**

```xml
<PackageReference Include="Alchyr.Sts2.BaseLib" Version="*" PrivateAssets="All"/>
<PackageReference Include="Alchyr.Sts2.ModAnalyzers" Version="*" />
<AdditionalFiles Include="ModTemplate/localization/**/*.json"/> <!-- Used by analyzer -->
```

**BaseLib 是有 NuGet 包的**（`Alchyr.Sts2.BaseLib`），`Version="*"` = 永远取最新。

> **对比我们**：`DouSpire.csproj` 里 BaseLib 是这么来的 ——
> ```xml
> <BaseLibPath>$(Sts2Dir)\..\..\workshop\content\2868840\3737335127\BaseLib\BaseLib.dll</BaseLibPath>
> <Reference Include="BaseLib" HintPath="$(BaseLibPath)" Private="False" Condition="Exists('$(BaseLibPath)')"/>
> ```
> **硬编码到"工坊内容目录 + 工坊 id + 嵌套子目录"**。本机能用（路径确实存在，我查过），但：
> ① 换台机器/换盘符就断；② 取消订阅那个工坊条目就断；③ 它带 `Exists(...)` 条件 →
> **断了也只是"不引用"，不会报错**（又是静默失败那一类）。
> **改用 `PackageReference Include="Alchyr.Sts2.BaseLib"` 是明确更优解**，而且是官方发布渠道。
>
> 顺带：**RitsuLib 我们已经是走 NuGet 的**（`STS2.RitsuLib`），这条我们做对了。

### 3.5 ★★★ 依赖版本自动同步：正解是**读 `project.assets.json`**

这段是整份模板对今天的我们**最有直接价值**的十几行：

```xml
<Target Name="UpdateDependencyVersions" BeforeTargets="BeforeBuild"
        Condition="Exists('.godot/mono/temp/obj/project.assets.json')">
  <PropertyGroup>
    <ManifestPath>$(MSBuildProjectName).json</ManifestPath>
    <!-- ① 读 NuGet 的解析结果，正则抠出 BaseLib 的**真实**版本号 -->
    <NuGetData>$([System.IO.File]::ReadAllText(".godot/mono/temp/obj/project.assets.json"))</NuGetData>
    <ActiveBaseLibVersion>$([System.Text.RegularExpressions.Regex]::Match(
        $(NuGetData), '"Alchyr.Sts2.BaseLib\/([^"]*)').Result('$1'))</ActiveBaseLibVersion>
    <!-- ② 直接改写 manifest 里的 min_version -->
    <OldContent>$([System.IO.File]::ReadAllText($(ManifestPath)))</OldContent>
    <NewContent>$([System.Text.RegularExpressions.Regex]::Replace($(OldContent),
        '("id":\s*"BaseLib",\s*"min_version":\s*")[^"]*', '${1}$(ActiveBaseLibVersion)'))</NewContent>
  </PropertyGroup>
  <!-- ③ 只在真的变了的时候写回，并且**大声说一句** -->
  <WriteLinesToFile Condition="'$(OldContent)' != '$(NewContent)'" File="$(ManifestPath)"
                    Lines="$(NewContent)" Overwrite="true" Encoding="UTF-8" />
  <Message Condition="'$(OldContent)' != '$(NewContent)'"
           Text="Updated BaseLib dependency version in mod manifest json to version $(BaseLibVersion)"
           Importance="high" />
</Target>
```

### 3.5 ★★★ 依赖版本自动同步：我们那个目标**到底为什么不行**

这段是整份模板对今天的我们**最有直接价值**的十几行（模板的 `UpdateDependencyVersions` 见上）。

#### ⚠️ 先更正：我最初对"我们那个目标为什么不行"的归因**是错的**

我原先写的是"守卫条件 `'$(PkgSTS2_RitsuLib)' != ''` 为假 → 整个目标被静默跳过"。
**这条被实验证伪了**（另一位协作者做的，我复核过）：

```
# 证据 1：属性根本不是空的
projects/dou-spire/.godot/mono/temp/obj/DouSpire.csproj.nuget.g.props:104
    <PkgSTS2_RitsuLib Condition=" '$(PkgSTS2_RitsuLib)' == '' ">
        C:\Users\有花无实\.nuget\packages\sts2.ritsulib\0.6.6

# 证据 2：故意把 DouSpire.json 里的版本写成 0.6.1 → 构建 → 源码 json 变回 0.6.7
#         → 目标**跑了**，而且真的改写了文件
```

**结论：目标一直在跑。"部署副本是旧的"不是因为目标没跑，是因为别的原因。**
（我当初那条"源码 mtime 没被刷新"的观察是真的，但**归因错了** —— 见下面 ①②③。）

#### 真正的原因（三个独立问题叠在一起）

| # | 真因 | 说明 | 修法 |
|---|---|---|---|
| **①** | **MSBuild 声明顺序抢跑** | 原写法 `AfterTargets="Build" BeforeTargets="CopyMod"`。**两个 target 都挂 `AfterTargets="Build"` 时，MSBuild 按声明顺序跑**，而这个 target 声明在 `CopyMod` **之后** → 实际顺序是 `Build → CopyMod（拷旧 json）→ 才改源码 json`，改完没人再拷一遍 | 让 `CopyMod` 显式 `DependsOnTargets="SyncManifestDependencies"`，顺序 100% 确定（**已落地**） |
| **②** | **版本号来源取错** | csproj 写 `Version="*"`（永远取最新）→ NuGet 缓存里躺着 0.6.2/0.6.5/0.6.6/0.6.7…，`$(PkgSTS2_RitsuLib)` 指向**最新那个（0.6.7）**，**不是游戏里装的那个（0.6.6）** → 声明和现实又对不上 | 优先读**游戏目录** `mods/STS2-RitsuLib/mod_manifest.json` 的真实版本，读不到才退回 NuGet（**已落地**） |
| **③** | **改的是游戏不认的键** | 正则写的是 `"version"`；**游戏只认 `min_version`**（见 §四） | **⬜ 还没改** —— 见下面"⚠️ 和 §四 的交叉" |
| ④ | `$1` 反向引用 + `(?s)` 跨行 | MSBuild 会先把 `${1}`/`$1` 当自己的属性语法展开成空串，再交给正则；配合 `(?s)` 让 `.*?` 跨行 → **把整个 manifest 吞进 version 字段写坏文件**（10-08 真的写坏过一次） | 用唯一占位符 `@@RITSULIB_DEP@@` 替换 → 数出现次数 → **必须恰好 1 个**才落盘（**已落地**） |
| ⑤ | `WriteLinesToFile` + `Regex::Split` | 拆成行数组再写会毁掉 JSON 字符串值里的 `\n` 转义 → `Invalid control character at: line 15 column 20` | 整个 JSON **作为单行一次写出**（**已落地**） |
| ⑥ | 静默失败 | 没输出 → "跳过"和"跑完了"日志长得一样 | 加 `<Message Importance="high">`（成功时报"同步为 X，来源 Y"）+ `<Warning>`（拿不到版本 / 匹配数 ≠ 1）（**已落地**） |

#### ⚠️ ⚠️ 和 §四 的交叉：现在**更隐蔽**了

① ② ④ ⑤ ⑥ 修好之后，那个目标会**每次构建都可靠地把 `version` 写对** ——
而 **`version` 是游戏不读的键**。
⇒ 表现变成"清单看起来总是同步的、每次构建还打印一行成功日志"，**但游戏读到的仍然是 `minVersion = null`**。
**比修之前更容易让人以为没问题。** ⇒ **必须先做 §八-1（键名改成 `min_version`）**，否则这条修好等于白修。

#### ⚠️ 顺带：模板的 `project.assets.json` 方案**不能照抄**

模板用 `project.assets.json` 拿版本号 —— 那里面是 **NuGet 解析出来的版本**。
模板自己写 `Version="*"`，所以 `project.assets.json` 里永远是**最新版**。
对 BaseLib 还行（玩家从工坊拿到的也是最新）；**对我们不行**：

```
NuGet 上 STS2.RitsuLib 有 0.6.6 / 0.6.7（Version="*" → 解析成 0.6.7）
工坊/游戏里装的是 0.6.6
⇒ 照抄模板会把 min_version 写成 0.6.7 → 所有 0.6.6 的玩家被 GAME_VERSION 拦下
```

**根子上更干净的做法是把 `Version="*"` 换成钉死的版本号**（`Version="0.6.6"`），
这样"编译用的版本 = 声明的版本 = 游戏里装的版本"三者一致，两个方案就都安全了。

### 3.6 部署与 pck 导出

```xml
<Target Name="CopyToModsFolderOnBuild" AfterTargets="PostBuildEvent">
  <Copy SourceFiles="$(TargetPath)"                     DestinationFolder="$(ModsPath)$(MSBuildProjectName)/" />
  <Copy SourceFiles="$(AssemblyName).json"              DestinationFolder="$(ModsPath)$(MSBuildProjectName)/" />
  <Copy SourceFiles="$(TargetDir)$(TargetName).pdb"     DestinationFolder="$(ModsPath)$(MSBuildProjectName)/" />
</Target>

<Target Name="NeedGodotForPublish" BeforeTargets="Publish">
  <Error Text=" Godot path must be set up before publishing; ..."
         Condition="'$(GodotPath)' == '' or !Exists('$(GodotPath)')"/>
</Target>

<Target Name="GodotPublish" AfterTargets="Publish" Condition="... and '$(IsInnerGodotExport)' != 'true'">
  <Exec Command="&quot;$(GodotPath)&quot; --headless --export-pack &quot;BasicExport&quot;
                 &quot;$(ModsPath)$(MSBuildProjectName)/$(MSBuildProjectName).pck&quot;"
        EnvironmentVariables="IsInnerGodotExport=true;MSBUILDDISABLENODEREUSE=1"
        ContinueOnError="WarnAndContinue"/>
</Target>
```

- 部署 = **dll + json + pdb**（pdb 是调试符号，带上能让报错有行号）。
- pck 只在 **`Publish`** 时导出（不是每次 Build），用 `Godot --headless --export-pack "BasicExport"`。
- `IsInnerGodotExport` 这个环境变量是**防递归**（Godot 导出时会触发一次 build，别再套娃）。
- ⚠️ **导出用的 Godot 必须是游戏那一版**，`Directory.Build.props` 里的原话：
  > *"Megadot current version is 4.5.1, and the game won't load your .pck if the Godot version used is newer."*
  我们就装在 `D:\software\godot\Godot_v4.5.1-stable_mono_win64\` —— **版本刚好对上**。

`export_presets.cfg` 里两个关键项：
```
name="BasicExport"                 ← 必须和上面 --export-pack 的名字一致
exclude_filter="ModTemplate.json"  ← ★ manifest 不打进 pck（否则包里的 json 会被当成"又一个 mod"，见 §4.2）
binary_format/architecture="msil"  ← 跨平台中间码
```

### 3.7 目录约定与仓库卫生

```
ModTemplate/
├─ ModTemplate.json          ← manifest（包根，和工程名同名）
├─ ModTemplate/              ← 资源目录（= 打进 pck 的东西；图片放这）
│   └─ images/{card_portraits,powers,relics,charui,potions}/...
├─ ModTemplateCode/          ← C# 代码
│   └─ MainFile.cs
├─ Sts2PathDiscovery.props   ← 自动探路径（入库）
├─ Directory.Build.props     ← 本机 Godot 路径（**被 .gitignore 忽略**）
├─ project.godot / export_presets.cfg
└─ .gitignore / .gitattributes
```

`.gitignore`（模板的）：
```
.godot/  bin/  /android/  .git/  .idea/  .vs/  publish/  Properties/  packages/
.editorconfig  *.user  Directory.Build.props
```

> **收口原则和我们一致**：机器相关的东西（Godot 路径）单独一个文件 + 忽略；
> 代码和配置入库。**我们三个 mod 的代码资源划分和它一样**（`<Mod>Code/` + `<Mod>/`）——
> 这说明我们当初的骨架是对的，差的是**主干脚本**。

---

## 四、manifest 就是游戏 API（权威定义在这里）

**取证来源（不用反编译 dll，教程资料包里已经有反编译源码）**：
```
projects/sts2-moddev-workspace/02-游戏反编译资料/版本-0.111.0/反编译源码/
  MegaCrit.Sts2.Core.Modding/ModManifest.cs      ← manifest 的字段定义
  MegaCrit.Sts2.Core.Modding/ModDependency.cs    ← 依赖条目的字段定义
  MegaCrit.Sts2.Core.Modding/ModManager.cs       ← 校验逻辑（:552 / :780 / :860-930）
  MegaCrit.Sts2.Core.Debug/SemanticVersion.cs    ← 版本号怎么解析（:66 显式跳过开头 v）
```

### 4.1 `ModManifest` 的字段表（v0.111.0，权威）

| JSON 键 | C# 字段 | 类型 | 说明 |
|---|---|---|---|
| `id` | `id` | string? | **唯一标识**，没有它 → 报错（见 4.2） |
| `name` / `author` / `description` | 同名 | string? | 展示用 |
| `version` | `version` | string? | 自己这一版的版本号，**要能被 SemanticVersion 解析** |
| `has_pck` | `hasPck` | bool | 声明"我要加载 pck" |
| `has_dll` | `hasDll` | bool | 声明"我要加载 dll" |
| `affects_gameplay` | `affectsGameplay` | bool（默认 `true`） | 会不会影响玩法（联机/存档提示用） |
| `dependencies` | `dependencies` | `ModDependency[]?` | 依赖表 |
| `min_game_version` | `minGameVersion` | string? | **低于它就不加载**（见 4.3） |

### 4.2 ★ 依赖条目只有两个字段：`id` 和 `min_version`

```csharp
public class ModDependency {
    [JsonPropertyName("id")]          public string  id;
    [JsonPropertyName("min_version")] public string? minVersion;   // ← 就这一个版本字段
}
```

**`version` 不是合法键，`System.Text.Json` 会直接忽略它。**
结果：`minVersion = null` → 游戏**跳过整个版本校验**。

**这不是猜的，是统计出来的**（本机 121 个工坊 manifest + 本地 4 份，共 65 个依赖条目）：

| 依赖条目里用的键 | 出现次数 |
|---|---|
| `id` | 65 |
| **`min_version`** | **48** |
| `version` | **10** ← **这 10 条全是我们自己的 mod** |
| `min` | 1 |

**全生态只有我们在用 `version`。** 这是一个"只有我们家犯"的错。

`ModManifest.ReadFromStream` 里还留了一条**老格式迁移逻辑**（有意思，顺手记下）：
如果 `dependencies` 是一个**字符串数组**（`["BaseLib"]`），游戏会
`Log.Error("Detected old-style dependencies without min version specified! ... this will be removed in a future release")`
并挂一条 `MOD_ERROR.MIGRATION_REQUIRED` 提示 —— **将来会强制要求写 `min_version`**。

### 4.3 manifest 文件是怎么被发现的：**递归扫描所有 `.json`**

`ModManager.ReadModsInDirRecursive`（`:548`）：

```csharp
foreach (string text in array) {
    if (text.EndsWith(".json")) {          // ← 只看后缀
        Mod mod = ReadModManifest(...);    //    整份 json 当 manifest 解析
        if (mod != null) _mods.Add(mod);
    }
}
// 然后**递归进所有子目录**再来一遍
```

推论（都挺要紧）：
1. **文件名随便**：RitsuLib 叫 `mod_manifest.json`，我们叫 `DouSpire.json`，**都行**。
2. **递归**：所以 BaseLib 能待在 `3737335127/BaseLib/BaseLib.json` 这种嵌套布局里（这就是我们第一轮扫描漏掉它的原因）。
3. ⚠️ **mod 目录（含所有子目录）里任何 `.json` 都会被当 manifest 试读一次**：
   - 缺 `id` 且 `name/author/description/version` **全缺** → 只打一条 `Log.Info("does not look like a mod manifest; skipping it.")`（无害）
   - 缺 `id` 但**有** `name/author/description/version` 任一 → **`Log.Error("...looks like a mod manifest but is missing the 'id' field!")`**
   **→ 别往 mod 目录里丢随手写的 `.json`**（配置、调试表、本地化分片都算）。放进 pck 里最安全。
4. `pck_name` 字段**已经死了**：v0.111.0 的 `ModManifest` 里没有它，全反编译源码里也搜不到。
   pck 的路径是**写死的**：`Path.Combine(mod.path, modId + ".pck")`，
   而且要 `has_pck == true` 才会去加载（`:970`）。**我们四个 manifest 都带着 `pck_name`，是无效字段。**

### 4.4 七种"不给你加载"的原因（`ModManager.TryLoadMod`，按代码顺序）

| 代码里的 `MOD_ERROR.` 键 | 触发条件 | 后果 |
|---|---|---|
| `DUPLICATE_ID` | 两个 mod 的 `id` 撞了 | Failed |
| `CIRCULAR_DEPENDENCY` | 依赖成环 | Failed |
| `GAME_VERSION_INVALID` | `min_game_version` **解析不了** | Failed |
| `GAME_VERSION_UNSUPPORTED` | **游戏版本 < `min_game_version`** | Failed |
| `MISSING_DEPENDENCY` | 依赖的 mod 没装 / 没加载成功 | Failed |
| `DEPENDENCY_MIN_VERSION_INVALID` | 依赖的 `min_version` 解析不了 | Failed |
| `DEPENDENCY_VERSION_MISSING` / `_INVALID` / `_UNSUPPORTED` | 被依赖方没写 `version`、写错、或**比要求低** | Failed |

`min_game_version` 缺失时是 `Log.Warn("...does not declare min game version. Assuming that it is supported.")` ——
**只是警告，照样加载**。
`v` 前缀**能解析**（`SemanticVersion.cs:66` 开头就 `if (i == 0 && version[i] == 'v') continue;`）→
所以 `v3.4.7` 这种写法是合法的，我们不用改。

---

## 五、与自家工程逐项对比

拿 `dou-spire` / `quest-spire` / `frost-spire` 跟模板对照（BloomlessSpire 还是模板样稿，另算）：

| 维度 | ModTemplate-StS2 | 我们 | 结论 |
|---|---|---|---|
| 游戏路径发现 | `Sts2PathDiscovery.props`：注册表 `Steam App 2868840` + Steam 默认路径，**入库**、三平台 | 手写 `local.props`（入库忽略 ✅）+ csproj 里一条**本机是错的**默认值 | **可抄** |
| 找不到游戏 | `CheckDependencyPaths`（`InitialTargets`，硬 `<Error>`） | `ValidateSts2GameInstall`（`BeforeTargets=ResolveAssemblyReferences`，硬 `<Error>`） | ✅ **等价，做对了** |
| 依赖版本同步 | 读 `project.assets.json` + 改 `min_version` | 读 `$(PkgSTS2_RitsuLib)` + 改 `version`（**三重失效**） | ❌ **必改** |
| manifest 依赖键 | `min_version` | `version`（**游戏不认**） | ❌ **必改** |
| 借游戏 dll | `Private=false` | `0Harmony`=False ✅；**`sts2` / `Steamworks.NET` = True** | ⚠️ 见下 |
| 构图依赖（BaseLib） | `PackageReference Alchyr.Sts2.BaseLib` | **硬编码工坊路径**（+ `Exists()` 静默降级） | ❌ **建议改** |
| 静态分析 | `Alchyr.Sts2.ModAnalyzers` + `AdditionalFiles` 挂本地化 | 无 | 可选 |
| Publicizer | 可选开关（默认关） | 恒开 | 知道就行 |
| pck 导出 | `Publish` 时 `Godot --headless --export-pack "BasicExport"`；manifest 排除在 pck 外 | `RunPckExport=false` + 手写脚本 | 我们有自己的路子，暂不动 |
| 部署 | dll + json + pdb | dll + json + pck + assets（我们的 `CopyAssets` 是独有需求） | 我们更全 |

**关于 `Private=True`（`sts2` / `Steamworks.NET`）—— 我实测过，目前无害**：
`bin/Debug/` 里确实躺着 `sts2.dll`（9,757,184 B）和 `Steamworks.NET.dll`，
但我们的 `CopyMod` 目标**只挑 `$(TargetPath)` + json + pck** 拷，
所以**部署目录里没有它们**（`mods/DouSpire/` 只有 dll + json + assets），游戏不会读到第二份 `sts2.dll`。
→ **这是一个"看起来危险但其实没炸"的写法**，改成 `Private=False` 更干净、也更省 IO，但不是急事。

---

## 六、StS2ModAnalyzers：4 条规则，价值在"规则清单本身"

`Alchyr.Sts2.ModAnalyzers` v0.2.1（`netstandard2.0`，Roslyn 分析器）。
用它的方式就是在 csproj 里加一条 `PackageReference`，再把本地化 json 挂成 `AdditionalFiles`。

| 规则 | 类别 | 级别 | 意思 |
|---|---|---|---|
| **STS001** | Localization | **Error** | **符号必须有本地化**（有配套的 FixProvider，能自动补条目的那种） |
| **STS002** | Localization | Warning | 本地化文件必须挂成 `AdditionalFiles`，否则分析器看不见 |
| **STS003** | Localization | Warning | 模型应该继承某个 `CustomModel`（BaseLib 的） |
| **STS004** | Usage | Warning | 模型必须用 `PoolAttribute` 加进某个池 |

**对我们有多大用？** 说实在的：
- STS001/STS002 是**内容型 mod**（卡牌/遗物/能力）的痛点 —— 少写一条本地化 → 游戏里显示成 key。
  我们的**斗地主/任务/凛冬**是"加一套玩法"，不是"加一堆卡"，**踩这个坑的概率低**。
- STS003/STS004 同理，指向 BaseLib 的 `Custom*Model` 体系。
- 它体积很小（`73KB` 仓库），**试错成本几乎为零**，但**别指望它抓到我们今天那些坑**
  （它不管 manifest、不管构建脚本）。

**结论：★★ 降为 ★，可以试，不值得专门花时间。**（0★ 也说明社区没怎么用起来。）

---

## 七、连带修正 —— 把之前的结论改口

拆这份模板时，**把昨天的一个结论推翻了**，必须记下来（错的留在笔记里比没有更糟）。

### 7.1 ❌ 错：「RitsuLib 0.6.6 自己要求 `min_game_version 0.111.0`」

**真实情况：RitsuLib 0.6.6 支持的游戏版本是 `0.107.1 / 0.109.0 / 0.110.0 / 0.111.0` 四个。**

证据：
```
工坊 3747602295/mod_manifest.json     →  "version": "0.6.6", "min_game_version": "0.107.1"
工坊 3747602295/ritsulib-variants.manifest（1956 B）
  variants: compatTarget = 0.107.1 / 0.109.0 / 0.110.0 / 0.111.0   ← 四套变体
工坊 3747602295/compat/ = {0.107.1, 0.109.0, 0.110.0, 0.111.0}     ← 四个目录

而本地 mods/STS2-RitsuLib/ 是**按当前游戏版本裁剪过的安装副本**：
  compat/ 只剩 {0.111.0}，ritsulib-variants.manifest 只有 629 B（只有 0.111.0 一项），
  mod_manifest.json 里的 min_game_version 被改写成 "0.111.0"
```

RitsuLib 的构建规则也印证了：`build/RitsuLib.ModManifest.targets:84`
```
<RitsuLibManifestMinGameVersion Condition="'$(RitsuLibManifestMinGameVersion)' == ''">$(Sts2ApiCompat)</...>
```
即 `min_game_version` = **本次构建的 compatTarget**；发行版取**最低**那个
（`scripts/release_lib/nuget.py:332: min_game_version=lowest_compat_target(compat_targets)`）。

**我昨天看的是那份"被裁剪成 0.111.0"的本地副本，于是把"它当前选中的变体"当成了"它的最低要求"。**

**后果（要紧）**：我据此把三个 mod 的 `min_game_version` 从 `0.106.0` 抬到了 **`0.111.0`**。
按游戏的判定（§4.4 `GAME_VERSION_UNSUPPORTED`），**游戏版本 0.107.1~0.110 的玩家现在会被直接拦住** ——
而他们的 RitsuLib 其实是能跑的。
⚠️ **但这不等于"应该改回 0.107.1"**：我们的 dll 是拿 0.111.0 的 `sts2.dll` 编的，
**"我们的代码在 0.107 上能不能跑"没有任何证据**。把 `min_game_version` 调低的正确前提是
**先验证代码只用 ≤0.107 的 API**，那是另一件事（见 §九）。

### 7.2 ⚠️ 半对：deployed 清单是旧的 —— 观察成立，**归因错了**

观察（部署那份 = 源码原文、值对不上）全部成立，但**原因不是"目标没跑"**，
而是 §3.5 列的三个真因（**声明顺序抢跑 / 版本号来源取错 / 键名错**）。
我原先那条"守卫条件为空 → 静默跳过"**已被实验证伪**，详见 §3.5。

**而且比"没生效"更麻烦**：那个目标现在被修好了，会**每次构建可靠地写 `version`** ——
一个游戏不读的键。⇒ 看起来一切同步正常，实际约束仍是零。**先做 §八-1。**

### 7.3 `Version="*"` 本身是个隐患（有实测数据）

我们 csproj 里 `STS2.RitsuLib` 写的是 `Version="*"`（永远取最新）。实测后果：

```
~/.nuget/packages/sts2.ritsulib/  里有四个版本：0.6.2  0.6.5  0.6.6  0.6.7
projects/dou-spire/.godot/mono/temp/obj/project.assets.json    → STS2.RitsuLib/0.6.6
projects/quest-spire/.godot/.../project.assets.json            → STS2.RitsuLib/0.6.7   ← 不一致
projects/frost-spire/.godot/.../project.assets.json            → STS2.RitsuLib/0.6.7   ← 不一致
游戏/工坊里实际装的                                            → 0.6.6
```

**三个工程解析出了两个不同版本** —— 这就是 `Version="*"` 的非确定性。
配合"读 NuGet 解析结果"同步方案，`min_version` 会被写成 **0.6.7**，
而玩家装的是 **0.6.6** → **把本来能跑的玩家挡在外面**（`DEPENDENCY_VERSION_UNSUPPORTED`）。

**改法：钉死版本号**（`Version="0.6.6"`）→"编译版本 = 声明版本 = 玩家版本"三者一致，
上面那个"游戏目录优先还是 NuGet 优先"的两难也随之消失。

### 7.4 ⚠️ 修正口径：「没有任何自动机制在管它」

准确说法是：**我们四个工程里没有任何机制在管它**。
生态里其实**有** —— RitsuLib 自己的 `GenerateRitsuLibModManifest`（每次 Build 前重写 manifest）
和这份模板的 `UpdateDependencyVersions`（每次 Build 前同步依赖版本）都是。
**我们缺的是"接入"，不是"世上没有"。**

---

## 八、可抄清单（按性价比排序，都还没动）

| # | 抄什么 | 从哪抄 | 收益 |
|---|---|---|---|
| **1** | **manifest 依赖键 `version` → `min_version`**（3 个 mod 共 6 条；BloomlessSpire 1 条） | §4.2 | ★★★ 修一个真 bug。**必须先做这条** —— 同步目标现在会可靠地往错键里写（§3.5） |
| **2** | 同步目标的**时机**：`CopyMod` 显式 `DependsOnTargets="SyncManifestDependencies"`（别靠两个 `AfterTargets="Build"` 的声明顺序） | §3.5-① | ✅ **已落地**（三个 csproj） |
| **3** | 同步目标的**版本号来源**：优先读游戏目录里那份依赖 manifest，读不到才退回 NuGet | §3.5-② | ✅ **已落地** |
| **4** | 同步目标的**正则安全**：唯一占位符 + 数出现次数（必须 =1）+ JSON 单行写出 + `Message`/`Warning` | §3.5-④⑤⑥ | ✅ **已落地** |
| **5** | **把 `Version="*"` 钉死成具体版本号** | §7.4 | ★★ 让"编译/声明/玩家"三者一致 |
| **6** | **BaseLib 改用 `PackageReference Alchyr.Sts2.BaseLib`**，删掉硬编码工坊路径 | `ModTemplate.csproj:38` | ★★ 换机器/退订不再断 |
| **7** | **`Sts2PathDiscovery.props`** 替代手写路径默认值（`local.props` 保留当覆盖口） | `Sts2PathDiscovery.props` | ★★ 少一处换机踩坑 |
| **8** | 构建脚本里**"必须成立"的条件一律 `<Error>`**，不许静默跳过 | `CheckDependencyPaths` | ★★ 规矩（今天两次栽在这） |
| **9** | manifest 里的 `pck_name` 删掉（无效字段）；并注意**别往 mod 目录丢 `.json`** | §4.2 / §4.3 | ★ 卫生 |
| 10 | 试挂 `Alchyr.Sts2.ModAnalyzers` + `AdditionalFiles` 本地化 | `ModTemplate.csproj:39,42` | ★ 可选 |
| 11 | `sts2` / `Steamworks.NET` 改 `Private=False` | `ModTemplate.csproj:26-29` | ★ 卫生（实测目前无害） |
| 12 | `Directory.Build.props` 单独放本机 Godot 路径并忽略 | `.gitignore` | ★ 和我们的 `local.props` 等价，不必动 |

> ⚠️ **不要照抄模板的 `UpdateDependencyVersions` 版本号来源**（`project.assets.json`）——
> 理由见 §3.5 末尾。**时机/正则安全/日志那几条可以抄，版本号来源要换成"游戏目录优先"。**

---

## 九、没做完 / 待验证

1. **`min_game_version` 到底该写多少？**（§7.1）
   要下结论必须先证明"我们的代码只用了 ≤某版本 的 API"。可行路子：
   - 找/装对应版本的游戏 dll 做一次编译（最硬）；
   - 看 RitsuLib 的 `compat/*` 里到底"compat"了什么（哪些 API 会被 shim 掉）→ 反推哪些 API 是新的。
   **在那之前，0.111.0 是"保守但过度"，0.107.1 是"没证据"** —— 两个都不能算完成。
2. **三个模板里 Content/Character 的内容骨架**（卡牌基类、本地化 6 个 json 的字段结构、
   `charui/` 每张图的尺寸与用途）**只看了目录和 `ContentModCard.cs`，没细读**。
   等要做"加卡牌/加角色"的线再回来啃。
3. `Sts2ModAnalyzers` 的 `LocalizationFixProvider` 具体怎么自动补本地化条目 —— 没看实现。
4. **BloomlessSpire 还是模板样稿**（`author: "Author"`、`version: 0.0.0`、
   `min_game_version: 0.106.0`、RitsuLib `0.6.2`、依赖键 `version`），一直挂着没动。
5. **一处还没解释干净的观察**：`QuestSpire.json` 源码 mtime = 10-04 14:54，
   而最后一次构建是 10-08 01:03 —— 如果按 §3.5-① 的顺序（先拷后改），源码**应该**每次构建都被刷 mtime。
   现在有两条实证（属性非空 + 故意改坏能变回来），说明目标确实在跑；
   但"那两次构建为什么没写"没有对上。**可能是当时 `obj/` 没还原好导致目标根本没被调度**，
   也可能是别的 —— **在下一次复现之前不要当成结论**。
6. **§八-5（钉死 `Version="*"`）没动**：现在 NuGet 上的 RitsuLib 是 0.6.7、工坊/游戏里是 0.6.6，
   两边已经分叉了。**只要还用 `Version="*"`，同步出来的版本号就有"越来越新"的趋势。**

---

## 十、复核索引

**本机快照（都在 `upstream/` 下，已 gitignore；仓库是公开的，随时可重下）**
```
projects/ref-modtemplate/upstream/ModTemplate-StS2-master/
  Alchyr.Sts2.Templates.csproj                      模板包的打包配置（MIT 声明在这）
  README.md / content/*/.template.config/template.json   模板参数（ModAuthor/PublicizeSts/NullableChecks）
  content/ModTemplate/ModTemplate.csproj            ★ 工程主干（139 行，全篇重点）
  content/ModTemplate/Sts2PathDiscovery.props       ★ 路径自动发现
  content/ModTemplate/Directory.Build.props         本机 Godot 路径（被 .gitignore 忽略）
  content/ModTemplate/ModTemplate.json              manifest 样例（注意是 min_version）
  content/ModTemplate/export_presets.cfg            BasicExport + 排除 manifest
  content/ModTemplate/project.godot
  content/ContentModTemplate/ContentModCode/Cards/ContentModCard.cs   卡牌基类用法
projects/ref-analyzers/upstream/StS2ModAnalyzers-master/
  ModAnalyzers/ModAnalyzers/README.md               ★ 4 条规则的清单
  ModAnalyzers/ModAnalyzers/LocalizationAnalyzer.cs / ModelRequiresPool.cs
```

**游戏侧权威定义（教程资料包里的 v0.111.0 反编译源码，不用自己反编译）**
```
projects/sts2-moddev-workspace/02-游戏反编译资料/版本-0.111.0/反编译源码/
  MegaCrit.Sts2.Core.Modding/ModManifest.cs         字段表 + 老格式迁移逻辑
  MegaCrit.Sts2.Core.Modding/ModDependency.cs       ★ 只有 id / min_version
  MegaCrit.Sts2.Core.Modding/ModManager.cs          :548 递归扫 json / :780 版本解析 / :860-930 七种失败
  MegaCrit.Sts2.Core.Debug/SemanticVersion.cs       :66 跳过开头的 'v'
```

**取数命令（顺手记，以后一键复现）**
```bash
# 统计全生态依赖键名的用法
python -c "..."   # 见 §4.2：遍历工坊 2868840 + 本地 mods，按 manifest id 归并，数 dependencies[].* 的键

# 一个 mod 到底要求哪个游戏版本：看它的 compat/ 目录和 variants.manifest
cat "D:/software/steam/steamapps/workshop/content/2868840/3747602295/ritsulib-variants.manifest"
```

**相关笔记**：`notes/杀戮尖塔2-拆解-RitsuLib框架.md` · `notes/杀戮尖塔2-拆解-BaseLib.md` ·
`notes/杀戮尖塔2-工坊mod盘点.md`（库存 + 依赖矩阵）· `notes/斗地主尖塔-任务尖塔-框架优化建议.md`（§二.A1 要按 §7.1 改口）

---

*写于 2026-10-08。结论基于本机游戏 v0.111.0、RitsuLib 0.6.6、BaseLib v3.4.7、
仓库 master 快照（模板最后推送 2026-08-22）。游戏或框架更新后，§四 的字段表要重新对一遍。*
