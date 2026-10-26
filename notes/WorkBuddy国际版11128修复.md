# WorkBuddy 国际版 11128「请求被拦截」完整诊断与修复

> 2026-10-16 处理完毕。**三个问题叠加**，逐个修好，国际版 + 国内版均已恢复。
> 涉及文件在 `D:\software\dsh-home\profiles\web`（**工作区外**）。

---

## 一、症状

DSH 里用 WorkBuddy **国际版**模型，每一步都失败：

```
400: workbuddy upstream client (http 400)
{"code":11128,"msg":"conversation id missing",
 "displayMsg":{"zh":"请求被拦截，请重新发送"},
 "displayTips":{"zh":"这次调用没通过渠道校验，可能不是官方客户端发出的。请用官方版本重新发送。"}}
```

**国内版完全正常** —— 这个故障**只在国际版出现**。

---

## 二、真相：三个问题叠在一起

### 问题 1：环境变量指向了错误的 WorkBuddy 路径（先修这个）

**症状**：`node lib/bin.js status` 显示

```
CN (domestic): signed out
Global: signed out
```

**根因**：WorkBuddy 在 **10-14 自动更新**，改了目录结构：

| | 路径 |
|---|---|
| **旧** | `D:\software\WorkBuddy AI\WorkBuddy.exe` |
| **新（国内版）** | **`D:\software\WorkBuddy AI\WorkBuddyAI\WorkBuddyAI.exe`**（多一层子目录 + exe 改名） |
| 国际版 | `D:\software\WorkBuddyIntl\WorkBuddyAI.exe` |

**而系统里残留着指向国际版的用户环境变量**：

```
WORKBUDDY_APP_EXECUTABLE = D:\software\WorkBuddyIntl\WorkBuddyAI.exe
```

**`wb.mjs` 里那行是 `||=`**（环境变量没设时才用默认值）→ **被这个残留值挡住，正确的默认路径永远不生效。**

**修复**：
```cmd
setx WORKBUDDY_APP_EXECUTABLE "D:\software\WorkBuddy AI\WorkBuddyAI\WorkBuddyAI.exe"
```
**验证**：`node lib/bin.js status` → CN + Global 都 `signed in`。

### 问题 2：插件版本太旧

| | 版本 |
|---|---|
| 本地（`profiles/web/package.json` 里**写死**） | `2.0.15` |
| npm 最新 | **`4.6.2`**（10-15 20:21） |

**修复**：把 `package.json` 里 `"dsh-connect-workbuddy": "2.0.15"` 改成 `"4.6.2"`，
然后 `cd profiles/web && pnpm install`。
（备份：`package.json.bak-before-wb462-*`）

### ★ 问题 3：官方新增了「会话身份三件套」校验（**核心**）

**这是"官方加了新手段"的确切内容。**

**同一个错误码 11128，判定条件被换掉了：**

| | 11128 的含义 |
|---|---|
| **旧**（插件 3.1.0 适配过） | `first message is not system prompt` —— 对话必须以 system 消息开头 |
| **新**（当前） | **`conversation id missing`** —— 缺会话身份 |

**⇒ 官方把"要有 system 消息"升级成了"要有会话身份"，光有 system 消息不再够。**

#### 三件套是三个 HTTP 请求头

```
X-Conversation-ID
X-Conversation-Request-ID
X-Conversation-Message-ID
```

**关键实测结论（来自 Issue #66）**：
- **三个必须配套** —— 单独只加 `X-Conversation-ID` 仍然 400 / 11128
- **值不校验来源** —— **每次请求生成任意合法 UUID 即可通过**

#### 已被排除的假设（都是实测过的，别再往这些方向查）

| 假设 | 实测结论 |
|---|---|
| 缺 system message | 带 / 不带都是 11128，**不是当前根因**（但仍必需） |
| 请求体字段名不对 | `conversation_id` / `conversationId` / `session_id` / `prompt_id`（body 与 header 各试）**都还是 11128** |
| 账号被风控 | 同 token 打 billing 接口 200，官方桌面端同账号正常 → **账号没问题** |
| 区域 / base host 选错 | `www.workbuddy.ai` × `www.codebuddy.ai` **都是 11128** |
| `X-Domain` 不匹配 | 4 种域组合（含省略）**都是 11128** |
| UA 不是官方身份 | **不是** |
| `acp-connection-id` / `X-Agent-*` / `X-IDE-*` / `X-B3-*` / traceparent | **都不是必需**（加上也只有 200 是因为三件套已在） |

#### 修复：给插件打本地补丁

**文件**：`profiles/web/node_modules/dsh-connect-workbuddy/lib/host-heartbeat-De95rlXS.js`
**位置**：`function chatHeaders(credential)`（约 1139 行）

**在 `"Content-Type": "application/json",` 之后插入三行：**

```js
"X-Conversation-ID": crypto.randomUUID(),
"X-Conversation-Request-ID": crypto.randomUUID(),
"X-Conversation-Message-ID": crypto.randomUUID(),
```

（`crypto` 在 Node 18+ 是全局的，不用 import）

**备份**：`host-heartbeat-De95rlXS.js.bak-before-convid`

**验证结果**：
```
国际版：你好    ✅
国内版：你好    ✅
```

---

## 三、⚠️ 补丁的注意事项

1. **补丁在 `node_modules` 里，会被下次 `pnpm install` / 插件升级覆盖** ——
   升级后要重新打，或者等作者在正式版里修（Issue #66 已报告，**作者尚未修复**）
2. **恢复原状**：
   ```cmd
   copy /Y host-heartbeat-De95rlXS.js.bak-before-convid host-heartbeat-De95rlXS.js
   ```
3. **改完必须重启 DSH** —— Node 进程的环境变量和模块缓存在启动时固定

---

## 四、还差最后一步

**重启 DSH**（关掉再打开）：
- 环境变量的修改才生效
- 打补丁后的插件代码才被加载
- `package.json` 的升级结果才生效

---

## 五、参考

- **Issue #66**（本次问题的权威报告，2026-10-16）：
  https://github.com/dingminhua/dsh-connect-workbuddy/issues/66
  标题：**「国际版全部模型 400 / 11128：网关新增要求会话身份三件套，插件未发送
  （3.1.0 的 system 规则仍然必需，但已不再是充分条件）」**
- 插件仓库：https://github.com/dingminhua/dsh-connect-workbuddy
- npm：`dsh-connect-workbuddy`（latest = 4.6.2）

---

## 六、诊断用的命令（下次照抄）

```bash
# 1. 看账号登录状态（要带正确的 exe 路径）
cd D:/software/dsh-home/profiles/web/node_modules/dsh-connect-workbuddy
WORKBUDDY_APP_EXECUTABLE="D:\\software\\WorkBuddy AI\\WorkBuddyAI\\WorkBuddyAI.exe" node lib/bin.js status

# 2. 完整诊断
WORKBUDDY_APP_EXECUTABLE="..." node lib/bin.js doctor

# 3. 直接测国际版 / 国内版
cd "C:/Users/有花无实/Documents/Default Project"
node tools/wb.mjs "你好"              # 国际版
node tools/wb.mjs --region cn "你好"  # 国内版

# 4. 查 npm 最新版
curl -s https://registry.npmjs.org/dsh-connect-workbuddy -o x.json
python -c "import json;print(json.load(open('x.json',encoding='utf-8'))['dist-tags'])"
```
