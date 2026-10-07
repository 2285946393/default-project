# ref-ritsulib —— RitsuLib 框架参考

**上游**：https://github.com/BAKAOLC/STS2-RitsuLib · **协议 MIT** · 200★/20 fork
**版本**：0.6.6（工坊 `3747602295`，`min_game_version 0.111.0`）
**官网**：https://sts2-ritsulib.ritsukage.com/

## 为什么有这个目录

`STS2-RitsuLib` 是全部 119 个工坊 mod 里**被依赖最多的（26 个）**，
且**本机 4 个自研 mod 全部依赖它**（DouSpire 0.6.6 / FrostSpire 0.6.5 /
QuestSpire 0.6.2 / BloomlessSpire 0.6.2）。它是地基，不是可选参考。

拆解笔记：`notes/杀戮尖塔2-拆解-RitsuLib框架.md`（16 节，含 14 条"可偷"清单）

## 目录内容

```
ref-ritsulib/
├── README.md                    ← 本文件
├── tools/
│   ├── parse_xml_api.py         解析 XML 文档 → 类型/成员统计 + 命名空间分布
│   └── dump_ns.py               按命名空间导出成员清单（带中文文档摘要）
├── dumps/                       API 表面导出（12 个命名空间 + 全类型清单）
│   ├── _alltypes.txt            1772 个 Runtime 类型
│   ├── patching.txt             Patching 命名空间
│   ├── interop.txt              ★ 76 个 [Register…] 特性全表
│   ├── content.txt              ★ 40+ 个"塞进原版列表"的补丁
│   ├── timeline.txt             Epoch / Story / 解锁
│   ├── lifecycle.txt            30 个生命周期补丁
│   ├── localization.txt         本地化 + SmartFormat 桥
│   ├── capabilities.txt         ★ 能力系统 90 类型
│   ├── saves.txt                存档
│   ├── persistence.txt          Shared 的持久化（Profile 作用域）
│   ├── scaffolding-ancients.txt 先古之民选项注册
│   ├── scaffolding-characters.txt 角色脚手架
│   ├── ui-shell.txt             ★ Shell 主题 + token 体系
│   ├── framework-entry.txt      RitsuLibFramework 入口
│   ├── theme-tokens.txt         （空，令牌在 ui-shell.txt 里）
│   └── theme-root.txt
├── assets/                      从 assets.zip 解出的内置资源
│   ├── themes/                  18 个 .theme.json（9 主题 × 普通/圆角）
│   ├── localization/            内置本地化（settings 表 138KB）
│   └── images/
└── upstream/                    ⚠️ 不进仓库（见 .gitignore）
    └── STS2-RitsuLib-main/      上游源码快照，19MB，含 docs/ 29 篇 + src/ 1344 文件
```

## 复现

```bash
M="D:/software/steam/steamapps/common/Slay the Spire 2/mods/STS2-RitsuLib"

# API 表面（比反编译干净，中英双语说明）
python tools/parse_xml_api.py "$M/shared/STS2-RitsuLib.Shared.xml" \
  "$M/shared/STS2-RitsuLib.Ui.xml" "$M/shared/STS2-RitsuLib.Settings.xml" \
  "$M/compat/0.111.0/STS2-RitsuLib.Runtime.xml"

# 按命名空间导出
python tools/dump_ns.py "$M/compat/0.111.0/STS2-RitsuLib.Runtime.xml" "STS2RitsuLib.Patching"
python tools/dump_ns.py "$M/compat/0.111.0/STS2-RitsuLib.Runtime.xml" --list   # 全类型清单

# 内置主题 / 本地化
python -c "import zipfile; zipfile.ZipFile('$M/assets.zip').extractall('assets')"

# 上游源码（MIT）
mkdir -p upstream && cd upstream && \
  curl -sSL -o r.tar.gz https://codeload.github.com/BAKAOLC/STS2-RitsuLib/tar.gz/refs/heads/main && \
  tar -xzf r.tar.gz && rm r.tar.gz
```

## 两条最该先看的官方文档

- `upstream/STS2-RitsuLib-main/docs/pages/guide/shell-theme.md`（8.5KB）—— UI 线
- `upstream/STS2-RitsuLib-main/docs/pages/guide/lifecycle-events.md`（4.9KB）—— 74 个事件表

## 相关

| 目录 | 内容 |
|---|---|
| `projects/ref-combatsolver/` | CombatSolver（依赖 RitsuLib ≥0.6.0）反编译 213,836 行 |
| `projects/ref-wuwancients/` | 鸣潮先古（用 BaseLib）22,957 行 |
| `projects/ref-actiongame/` | 类幸存者（**零依赖**，89 个裸 Harmony 补丁）14,690 行 |
| `notes/杀戮尖塔2-工坊mod盘点.md` | 全部 119 个 mod 的已拆/未拆清单 |
