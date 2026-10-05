# Codex 插件清单（2026-09-23 装的）

> 这份不用用户记，是留给"新任务里回来的我"看的：一读就知道这台机器上装了什么、拆了什么、为什么。
> 用户不会敲任何命令，只会打字说要干什么——装/拆/换插件都由 agent 直接动手。

## 两个插件目录（Codex 里叫 marketplace）

| 名字 | 来源 | 规模 |
|---|---|---|
| `awesome-codex-plugins` | github: hashgraph-online/awesome-codex-plugins | 235 个插件 |
| `claude-code-workflows` | github: wshobson/agents | 92 个插件 |

注意：Codex 客户端里那个「插件」页面走的是要登录 ChatGPT 的在线市场，本机是 API Key 登录，所以那页是空的——不影响，本地/Git 市场照装。

## 装着的

**写页面**：`uizze`（治 AI 味界面）、`ui-design`、`frontend-mobile-development`、`javascript-typescript`、`documentation-generation`、`accessibility-compliance`（无障碍，前端面试加分）

**干活流程**：`quiver`（21 技能 + 20 审查角色）、`dev-skills`（规格→计划→TDD→调试→验证→收尾）、`developer-essentials`、`agent-workflow-system`（全中文新手引导）

**看懂项目 / 复盘**：`codebase-recon`（先读 git 历史）、`devrecap`（从历史对话重建工作记录）

**学 Codex**：`codex-howto`

**文字**：`avoid-ai-writing`

**HTML 预览批注**：`reviewable-html-workbench`

**游戏**：`hera-godot`（要在 Godot 项目设置里启用 Hera 插件才生效，目前没启用）

**官方**：`chrome`（控制本机 Chrome）、`latex`

## 拆掉的（装上才发现是残包，留着会报错）

`mcp-md-reader`、`codex-usage-tracker`、`memoire`、`deepseek-minimal-anchor`、
`application-performance`、`seo-technical-optimization`

两类毛病：缺服务端文件/配套命令行；或 plugin.json 里声明了 `skills/` 但包里根本没有这个目录。

## 体检脚本

```
python D:\software\dsh\mk-validate.py          # 查已装插件是不是残包
python D:\software\dsh\mk-validate.py 插件名    # 装之前先查候选
python D:\software\dsh\mk-skills.py            # 列出全部技能 + 查重名
```

**装插件前先跑体检**，这个市场的包质量参差，别直接装。

## 使用注意

- 新装的插件要**新开一个任务**才生效（技能是开任务时加载的）。
- 技能总数已到 106 个，前缀变长但被 KV 缓存吃掉，钱上几乎无感；用户嫌挑不准时再精简。
