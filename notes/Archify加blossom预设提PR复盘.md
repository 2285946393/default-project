# Archify 加第五套预设 blossom + 提 PR 复盘

日期：2026-09-18
PR：https://github.com/tt-a1i/archify/pull/473（已提交，CI 10/10 全过）

---

## 这件事是什么

Archify 是个给 AI 编程助手用的「画图技能」（66k★，MIT）。你让 agent 讲一个系统架构，
它输出结构化 JSON，Archify 确定性地编译成单文件、离线可交互的 HTML 图。

它原本有 4 套视觉预设，每套对应一种场合：

| 预设 | 场合 |
|---|---|
| classic | 稳定默认 |
| signal-flow | 动态演示 |
| blueprint | 工程评审 |
| editorial | 发布说明 |

缺的是「个人作品展示」——把架构图放到个人站或作品集里，用 blueprint 那套会像内部工具漏出来。
我加的 `blossom` 补这个位置：语义配色和几何完全不变，只换表现层（暖玫瑰底、1.75rem 圆角、
柔和光晕、圆形角标）。

---

## 项目结构（改之前必须搞清楚的）

```
viewer/template.source.html        ← 唯一的模板源码（345KB / 6238 行）
  ↓ node ../scripts/generate-viewer.mjs
archify/assets/template.html       ← 生成产物（757KB），禁止手改

archify/schemas/*.schema.json      ← schema 源码
  ↓ node scripts/generate-validators.mjs
archify/renderers/shared/generated-validators.mjs   ← 生成产物，禁止手改
```

两个生成器都支持 `--check`，用来验证产物是不是最新的。CI 里会跑。

**依赖必须用 `npm ci` 装**（ajv / parse5 / saxes / simple-icons），否则
`generate-validators.mjs` 报 `ERR_MODULE_NOT_FOUND: Cannot find package 'ajv'`。

---

## 一套预设要改的地方（5 处，漏一处就白干）

1. `viewer/template.source.html`
   - 深/浅两套 CSS 变量块（约 30 个变量）
   - body 背景、header 角标、`.diagram-container` 圆角、`.card`、control mark、swatch
   - 菜单按钮 HTML + `data-preset-badge-blossom` 属性
   - `var PRESETS = [...]` 数组、`LABELS` 映射
2. `archify/renderers/shared/i18n.mjs`：`viewer.preset.blossom` / `.short` / `.hint` / `.badge`
3. `archify/schemas/common.schema.json`：`$defs.visualPreset` 枚举
4. `archify/schemas/workflow.schema.json`：**内联枚举**（这一个不是 `$ref`！）
5. `archify/test/preset-tryon.test.mjs`：4 处硬编码的预设列表

---

## 踩到的坑（按踩坑顺序）

### 1. `visual_preset` 写在 `meta` 里，不是根级
根级会被 `additionalProperties: false` 拒掉。

### 2. workflow schema 是内联枚举
其余 4 个 schema 都是 `$ref: common.schema.json#/$defs/visualPreset`，
只有 `workflow.schema.json` 把枚举抄了一份。两处都要加，否则只有 workflow 模式报错。

### 3. 模板一改，所有生成产物全变
改一行 CSS → 每个渲染出来的 HTML 都会变（因为模板是内联进去的）。要重建：

- `examples/*.html`（5 个）+ `archify/examples/*.html`（5 个）
- `examples/web-app.html`（它必须携带当前模板，golden 测试会查）
- `examples/checkout-platform-delta.html` + `.receipt.json`
- `docs/gallery.html` + `docs/gallery/manifest.json` + `docs/gallery/artifacts/*.html`（11 个）
- `docs/assets/archify-live-proof.gif` + `.json`
- `archify.zip`

`docs/guide.html` 和 `docs/start.html` 重建后和 main 字节一致，所以不进 diff。

### 4. `archify.zip` 的两个硬门槛
- `build-zip.sh` 硬校验 **Node 22**（本机是 24，得下便携版）
- `stage-clean-skill.mjs` 要 `git ls-files` 拿 index modes，所以**必须有真 .git**
  （我当初把子 .git 删了，导致 `required package input is not tracked by Git: archify/LICENSE`）

做法：克隆一份带 .git 的上游，把改动文件同步进去，用 Node 22 构建。
构建完再重跑到另一个路径 `cmp` 一次，确认字节稳定。

### 5. README 动图的 Windows 坑（最坑的一个）
`build-readme-showcase.mjs` 失败时只报 `EPERM ... rmSync`，真实错误被 `finally` 掩盖了。

写了个 wrapper 把 `fs.rmSync` 包一层吞掉异常，才看到真相：

- `commandPath()` 用 `sh -c 'command -v ffmpeg'`，Windows 下返回 MSYS 路径 `/c/Users/...`，
  spawnSync 执行不了 → `ffmpeg failed:` 且 stderr 为空

临时给脚本打补丁（把 MSYS 路径转成 `C:\...`）跑通，跑完**还原脚本**——这个修复和 PR 主题无关，
不该夹带进去。

### 6. 重录动图后 receipt 的路径分隔符
Windows 下 `path.relative()` 生成 `docs\assets\...`，而仓库约定是正斜杠。
要手动把 `output` 字段改回 `docs/assets/archify-live-proof.gif`，否则测试报字符串不等。

### 7. `labelAt` 在连线标签上似乎不生效
（这是后来画博客架构图时发现的，和 PR 无关）
标签重叠时改用「从不同边出发」（`fromSide`/`toSide`）来分开，比调 `labelAt` 有效。

---

## 怎么证明「零回归」

不要只看「跑一遍没报错」。正确做法是**基线对照**：

```bash
# 在同一个克隆里
git stash push          # 收起我的改动
npm test > pristine.log
git stash pop           # 恢复
npm test > mine.log
# 逐条比对失败列表
```

结果：两边**逐条一致**（1262 测试 / 1238 通过 / 17 失败 / 7 跳过，17 个 `not ok` 一字不差）。

### Windows 上 17 个固有失败（和改动无关）

| 数量 | 原因 |
|---|---|
| 11 | `git init --template=` 触发 Windows `\\.\nul` invalid argument |
| 4 | 没有 `.git` 时 `git ls-files` 返回空 |
| 1 | 预览子进程报 `SIGTERM` 而不是干净退出 |
| 1 | `ERR_UNSUPPORTED_ESM_URL_SCHEME`（把裸 Windows 路径喂给 ESM loader） |

另外有 3 个测试文件（`generate-validators` / `generate-viewer` / `release-package-gates`）
在**全量并行**时以 `0xC0000409` 崩溃，但**单独跑能过**——纯净上游也一样，属于资源问题。

---

## CI 结果

10/10 全过，包括：

- `test (18 / 20 / 22 / 24)` — 四个 Node 版本
- `zip-freshness` — 我在 Windows 上构建的 zip 和 CI 的 canonical 构建**字节一致**
- `package smoke (ubuntu / macos / windows)`
- `webm-artifact`

`zip-freshness` 是最关键的：它会在 Ubuntu + Node 22 上重建 zip 然后逐字节 `cmp`。
本地先在 Node 22 下重跑到另一个路径比对过一次，才敢推。

---

## 下次提 PR 的通用流程（可复用）

1. 克隆上游（**保留 .git**），建分支
2. 改源码 → 跑生成器 → 重建所有受影响的产物
3. `npm ci && npm test`
4. 做**基线对照**，确认失败列表没变
5. 用正确的工具链版本重建二进制产物（如 zip 要 Node 22）
6. 按 `CONTRIBUTING.md` + `.github/PULL_REQUEST_TEMPLATE.md` 写 PR 描述
7. 视觉改动要附证据：同输入、同视口、同主题的对比图
8. fork → push → `gh pr create --body-file`
9. 等 CI，有失败就查（先判断是不是环境固有）

---

## 一句话总结

真正花时间的不是「加一套配色」，而是**重建一整条生成链路上的产物**，
以及**用基线对照证明自己没弄坏任何东西**。配色本身反而是最简单的部分。
