# ref-analyzers —— `Alchyr/StS2ModAnalyzers`（Roslyn 分析器）

**不是 mod。** 塔2 modding 的 Roslyn 分析器/修复器，NuGet 包 `Alchyr.Sts2.ModAnalyzers` v0.2.1。

- 仓库：https://github.com/Alchyr/StS2ModAnalyzers · 分支 `master` · 最后推送 2026-09-29
- ⚠️ **只有 0★ / 1 fork**，**协议未声明**（nuspec 里没有 license 字段）
  —— 我们盘点笔记里原来把它标成 ★★，那是按"用处"评的，**已更正为 ★**
- 拆解笔记：`notes/杀戮尖塔2-拆解-ModTemplate工程模板.md` §六

## 4 条规则（`keyfiles/AnalyzerRules/README.md` 原文）

| 规则 | 类别 | 级别 | 意思 |
|---|---|---|---|
| `STS001` | Localization | **Error** | 符号必须有本地化（带 FixProvider，能自动补条目） |
| `STS002` | Localization | Warning | 本地化文件必须挂成 `AdditionalFiles`，否则分析器看不见 |
| `STS003` | Localization | Warning | 模型应该继承某个 `CustomModel`（BaseLib 的） |
| `STS004` | Usage | Warning | 模型必须用 `PoolAttribute` 加进某个池 |

**它不管 manifest、不管构建脚本** —— 抓不到我们今天踩的那几个坑。
对"加一堆卡/遗物"的内容型 mod 才有明显价值。

## 目录

| 路径 | 内容 |
|---|---|
| `keyfiles/` | 分析器 csproj + 规则 README（**入库**） |
| `upstream/` | 整仓快照（**已 gitignore**） |

## 怎么重新下载

```bash
P="$https_proxy"
curl -sSL --proxy "$P" -o /tmp/an.tar.gz \
  https://codeload.github.com/Alchyr/StS2ModAnalyzers/tar.gz/refs/heads/master
tar xzf /tmp/an.tar.gz -C upstream/
```
