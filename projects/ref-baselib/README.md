# ref-baselib —— BaseLib 参考

**上游**：https://github.com/Alchyr/BaseLib-StS2 · **协议 MIT** · **451★ / 91 fork**
**版本**：v3.4.7（工坊 `3737335127`，`min_game_version 0.107.1`）· 作者 **Alchyr**（StS1「Downfall」作者）
**官方 Wiki**：https://alchyr.github.io/BaseLib-Wiki/（源仓库 `Alchyr/BaseLib-Wiki`）
**清单路径很特殊**：`workshop/.../3737335127/BaseLib/BaseLib.json` —— **嵌套目录**，不在包根
**mod 起始模板**：https://github.com/Alchyr/ModTemplate-StS2 （299★）
**NuGet**：`<PackageReference Include="Alchyr.Sts2.BaseLib" Version="*" />`

## 为什么有这个目录

`BaseLib` 是工坊**第二被依赖**的框架（**25 个 mod**），定位是**内容层**：
给每种内容类型一个 `Custom*Model` 基类，继承它就能加内容。
配合 `STS2-RitsuLib`（26 个，框架层）**合计覆盖 42 个工坊 mod（约 44%）**。

- **本机 4 个自研 mod 全部依赖它**：DouSpire / QuestSpire / FrostSpire（各 v3.4.7）
- 已拆的 `wuwancients`（鸣潮先古）用的 `MakeOptionPools` / `IsValidForAct` /
  `ShouldForceSpawn` **就是 BaseLib 的 `CustomAncientModel` API**

拆解笔记：`notes/杀戮尖塔2-拆解-BaseLib.md`（16 节，含 15 条"可偷"清单 + 与 RitsuLib 的横向对照）

## 目录内容

```
ref-baselib/
├── README.md                    ← 本文件
├── BaseLib.json                 工坊清单（id / version / min_game_version / deps）
├── LICENSE.txt                  MIT
├── Notes.txt                    ★★ 作者自己的逆向笔记（11.5KB）—— 最值钱的东西
│                                  含：异步状态机 3.5 段结构 / 出牌流水线全链路 /
│                                  CardPileCmd.Add 的 IL 级控制流（带 IL_0611、flag2..flag8）/
│                                  DamageVar 三层值(BaseValue/EnchantedValue/PreviewValue) /
│                                  卡牌牌堆遍历的性能警告 / 作者自己的 TODO
├── Sts2PathDiscovery.props      ★ 自动探测游戏路径（注册表 HKLM ...Steam App 2868840
│                                  → InstallLocation，回退 HKCU\...\Valve\Steam@SteamPath）
├── wiki/
│   ├── index.md
│   └── docs/                    28 篇官方文档，186KB 纯文本
│       ├── Features.md, mechanics.md
│       ├── models/              custom-act / ancient / card / character / encounter /
│       │                        event / orbs / relic / singleton / temporary-power
│       ├── localization/        ancient-dialogue / code-loc / display-var / simplified-loc / var-loc
│       ├── scenes/              add-nodes / creature-visuals / energy-counter / merchant-character
│       └── utilities/           config / config-advanced / custom-calc-vars / enums /
│                                mod-audio / mod-interop / pooling / spirefield
└── upstream/                    ⚠️ 不进仓库（2.9MB，可一键重下）
    ├── BaseLib-StS2-master/     268 个 .cs / 40,112 行
    └── BaseLib-Wiki-main/
```

## 复现

```bash
B="D:/software/steam/steamapps/workshop/content/2868840/3737335127/BaseLib"
ls "$B"; cat "$B/BaseLib.json"     # 注意：清单在嵌套目录里

# 源码 + 文档（MIT，各自 ~400KB / ~2.5MB 压缩包）
cd upstream && \
  curl -sSL https://codeload.github.com/Alchyr/BaseLib-StS2/tar.gz/refs/heads/master | tar xz && \
  curl -sSL https://codeload.github.com/Alchyr/BaseLib-Wiki/tar.gz/refs/heads/main | tar xz

find BaseLib-StS2-master -name '*.cs' | xargs wc -l | tail -1    # → 40112
```

## 最短上手路径

1. **`Notes.txt`** —— 先读它，等于别人替你把原版逆向了一遍
2. **`wiki/docs/Features.md`** —— 2 分钟看完全部功能
3. **`wiki/docs/spirefield.md`** —— 招牌功能，4KB
4. **`wiki/docs/models/custom-ancient.md`** —— 如果你在做先古线（wuwancients 的底）
5. **`wiki/docs/utilities/enums.md`** —— `[CustomEnum]`，运行时扩枚举

## 相关

| 目录 | 内容 |
|---|---|
| `projects/ref-ritsulib/` | RitsuLib（框架层，26 个 mod 依赖）—— **和本目录互补** |
| `projects/ref-wuwancients/` | 鸣潮先古 —— 用的就是本库的 `CustomAncientModel` |
| `projects/ref-combatsolver/` | 战斗路线求解器（用 RitsuLib） |
| `projects/ref-actiongame/` | 类幸存者（**零依赖**，手写 89 个补丁 = 把本库的活手工干了一遍） |
| `notes/杀戮尖塔2-工坊mod盘点.md` | 全部 121 个 mod 的已拆/未拆清单 |

## 待办：值得单独拆的姊妹仓库

| 仓库 | star | 为什么 |
|---|---|---|
| **`Alchyr/ModTemplate-StS2`** | **299★** | **一个塔2 mod 工程应该长什么样**（csproj / 打包 / 本地化 / 部署）。我们自己的工程可对照对齐 |
| `Alchyr/StS2ModAnalyzers` | — | Roslyn 分析器/修复器，能自动提示塔2 modding 常见错误 |
