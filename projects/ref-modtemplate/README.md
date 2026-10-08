# ref-modtemplate —— `Alchyr/ModTemplate-StS2`（塔2 mod 工程模板）

**不是 mod。** 是 `dotnet new` 模板包 `Alchyr.Sts2.Templates` v2.5.2，
定义"一个正规的塔2 mod 工程长什么样"。

- 仓库：https://github.com/Alchyr/ModTemplate-StS2 · **299★ / 50 fork** · 分支 `master` · 最后推送 2026-08-22
- 协议：仓库根目录无 LICENSE 文件；模板包在 csproj 里声明了 `PackageLicenseExpression=MIT`
- 拆解笔记：`notes/杀戮尖塔2-拆解-ModTemplate工程模板.md`

## 目录

| 路径 | 内容 |
|---|---|
| `keyfiles/` | **挑出来的关键文件（入库）** —— 见下表 |
| `upstream/` | 整仓快照（**已 gitignore**，可一键重下） |

### keyfiles 是什么

| 文件 | 为什么留它 |
|---|---|
| `ModTemplate.csproj` | ★ **工程主干**。`CheckDependencyPaths` 硬报错 / `UpdateDependencyVersions` 读 `project.assets.json` 同步依赖版本 / 部署 / `GodotPublish` 导 pck |
| `Sts2PathDiscovery.props` | ★ 游戏路径自动发现（注册表 `Steam App 2868840` + Steam 默认路径，三平台） |
| `Directory.Build.props` | 本机 Godot 路径（上游就把它 gitignore 了） |
| `ModTemplate.json` | manifest 样例 —— 注意依赖版本字段是 **`min_version`** 不是 `version` |
| `export_presets.cfg` | pck 导出预设 `BasicExport`，并且 **`exclude_filter` 把 manifest 排除在 pck 外** |
| `dot-gitignore.txt` | 上游那份 `.gitignore`（改过名，免得被 git 当成真的忽略规则） |
| `Alchyr.Sts2.Templates.csproj` | 模板包自己的打包配置（MIT 声明在这） |

## 怎么重新下载

```bash
# 本机直连 GitHub 不通，用环境里的代理变量
P="$https_proxy"
curl -sSL --proxy "$P" -o /tmp/mt.tar.gz \
  https://codeload.github.com/Alchyr/ModTemplate-StS2/tar.gz/refs/heads/master
tar xzf /tmp/mt.tar.gz -C upstream/
```

> 注意默认分支是 **`master`**（不是 `main`，`main` 会返回 404）。
