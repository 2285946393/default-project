# DSH（DeepSeek Harness）安装与使用笔记

日期：2026-09-19
状态：已安装可用，还没填 API key

---

## 这是什么

DeepSeek 官方出的 **agent 命令行工具**（开发者预览版），定位和 Claude Code / Codex CLI 一样：
给它一个任务，它自己读写文件、跑命令、调用工具，把事情做完。

核心技术概念是 **profile（配置档）**——一个 profile 就是一堆插件层按顺序叠起来的东西。
`web` / `headless` / `tui` / `sdk` / `acp` 都是内置 profile，第一次用会自动从模板初始化。

npm 包名 `@deepseek-ai/dsh`，MIT 协议。
仓库：https://github.com/deepseek-ai/dsh （如果搜不到就用 npm 上的信息）

---

## 装在哪

| 项 | 路径 |
|---|---|
| 程序本体 | `D:\software\dsh`（213 MB，518 个包） |
| 数据目录 | `D:\software\dsh-home`（环境变量 `DSH_HOME`） |
| 版本 | `0.1.5-rc.2` |

**数据目录里放什么**（`D:\software\dsh-home`）：

```
profiles/web/          当前 web 配置档（含插件依赖、cordis.patch.yml）
profiles/node_modules/ 插件装在这里（pnpm 管理）
sessions/              会话记录
storages/              持久化存储
attachments/           附件（图片等）
llm-deepseek/          模型侧文件
.credentials.yaml      凭证（API key 存这）
settings.yaml          界面设置（语言、主题、字号、权限模式）
```

`settings.yaml` 当前内容（DSH 自动生成的）：

```yaml
locale:
  preference: zh
ui-theme:
  preference: light
  fontSize: 17
permission:
  defaultPreset: danger-full-access   # 注意：这是最高权限，不询问直接执行
```

> ⚠️ `danger-full-access` 意味着 agent 可以不问你就执行任何命令、改任何文件。
> 想要它每次问你，就在 `settings.yaml` 里把这行改掉。

---

## 怎么启动

**前提：必须开一个全新的终端**（PATH 是后加的，老窗口读不到）。

```
dsh web
```

会自动在浏览器打开 `http://127.0.0.1:3080/?token=<一串令牌>`。那个 token 是浏览器信任凭据，**带 token 的完整 URL 才能打开**，直接输 `127.0.0.1:3080` 会被拦。

常用变体：

```
dsh web --no-open          # 只起服务，不自动开浏览器
dsh web --port 8080        # 换端口
dsh web --help             # 看 web 界面的参数
```

**不想用网页界面**，直接命令行跑一次性任务：

```
dsh --profile headless "把这个目录里的图片按日期重命名"
```

跑完打印结果就退出。

---

## 常用命令

| 命令 | 作用 |
|---|---|
| `dsh --version` | 看版本 |
| `dsh web` | 启动网页界面 |
| `dsh --profile headless "任务"` | 跑一次性任务，打印结果就退出 |
| `dsh --profile web --port 8080` | 换端口启网页 |
| `dsh plugin --profile web add <包名>` | 往 web 档装插件 |
| `dsh plugin --profile web remove <包名>` | 卸载插件 |
| `dsh --dump-config --profile web` | 打印当前叠加后的完整配置（不启动） |
| `dsh --dump-default-config --profile web` | 打印默认配置（不含你自己的改动） |

注意：**启动目录 = 默认工作区**。想让 agent 干活，先 `cd` 到那个目录再 `dsh web`。

---

## 装 Archify 插件（我提过 PR 的那个画图技能）

Archify 官方就带 DSH 插件，装完就能让 DSH 直接画架构图：

```
dsh plugin --profile web add @tt-a1i/archify-dsh@0.1.0
```

**必须钉死版本号**（`@0.1.0`）。官方文档明确说不加版本会装到不兼容的版本。

装完在网页里跟它说：

```
Use the archify skill to map this repository's runtime architecture.
```

卸载：

```
dsh plugin --profile web remove @tt-a1i/archify-dsh
```

---

## 踩过的坑

### 1. npm 11 默认拦截 install 脚本（重要）

第一次装 DSH 时，npm 报了：

```
npm warn install-scripts 5 packages have install scripts not yet covered by allowScripts:
  @deepseek-ai/dsh-subprocess-local (postinstall: ensure-spawn-helper.mjs)
  koffi       (install: cnoke --prebuild)
  node-pty    (install / postinstall)
  ...
```

这 5 个里 **`node-pty`（终端）和 `dsh-subprocess-local`（子进程）是 agent 的命脉**，
不跑它们的安装脚本，DSH 就是个跑不动的空壳。必须放行重装：

```powershell
npm install -g --prefix "D:\software\dsh" --allow-scripts=@deepseek-ai/dsh-subprocess-local,koffi,node-pty,@google/genai,protobufjs @deepseek-ai/dsh
```

以后装任何带原生模块的 npm 包，见到 `install-scripts not yet covered by allowScripts` 都要这么处理。

### 2. PATH 加了但当前终端不生效

环境变量写进注册表后，**已经开着的终端读不到**。必须重开窗口。

### 3. 数据目录默认在 C 盘

不设 `DSH_HOME` 的话，配置和插件会堆在 `C:\Users\有花无实\.dsh`。
C 盘只剩 80GB，所以指到了 D 盘。

---

## 卸载

三步，都是删文件夹 + 删环境变量，没有注册表垃圾：

```powershell
npm uninstall -g --prefix "D:\software\dsh" @deepseek-ai/dsh
Remove-Item "D:\software\dsh" -Recurse -Force
Remove-Item "D:\software\dsh-home" -Recurse -Force
# 再把 D:\software\dsh 从用户 PATH 里删掉，DSH_HOME 变量删掉
```

---

## 环境依赖（本机现状）

| 依赖 | 版本 | 状态 |
|---|---|---|
| Node.js | v24.20.0 | ✅ DSH 要求 `^22.19.0 \|\| >=24.0.0` |
| npm | 11.19.0 | ✅ |
| pnpm | 12.4.2 | ✅ `dsh plugin` 靠它装插件 |

---

## 待办

- [ ] 第一次打开网页界面时，**填 DeepSeek 的 API key**（在设置/引导里填）
- [ ] 确认 `permission.defaultPreset` 要不要从 `danger-full-access` 改保守一点
- [ ] 试一次 `dsh --profile headless "..."` 看命令行模式顺不顺手
- [ ] 装 Archify 插件试画图

---

## 一句话备忘

```
cd 到要干活的目录 → dsh web → 浏览器里下指令
```
