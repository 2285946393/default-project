# Codex 用免费 Key（不登录 ChatGPT）

日期：2026-09-21
状态：**✅ 已配好并实测通过**

---

## 一句话结论

Codex 现在**默认走 unsee Luna**（你的免费 key），
**完全不需要 ChatGPT 登录**，打开就能用。

---

## 为什么要这么做

原来那套是：用 Chrome 插件把 ChatGPT 登录态"抄"出来存成 `auth.json`。

**问题**：那是快照，不能自动续期。你浏览器那边一刷新，它立刻失效，
Codex 就报：

```
"code": "token_expired"
Your access token could not be refreshed.
```

而正式的登录（`codex login --device-auth`）**要手机号验证**，走不通。

所以改成**绕开登录**：直接让 Codex 用第三方中转站。

---

## 关键的一行配置

```toml
web_search = "disabled"
```

Codex 默认会带一个"联网搜索"工具，**第三方中转站基本都不支持**，
会直接报 400 拒绝整个请求。必须关掉。

（这个键在 cc-switch 的源码里找到的，官方文档没写清楚。）

---

## 三个站实测结果

| 站 | 结果 | 原因 |
|---|---|---|
| **unsee Luna** | ✅ **能用** | 就是慢，一次回复 20~60 秒 |
| gcmod | ❌ | 它的 `/responses` **流式是坏的**（500），非流式才正常。Codex 只用流式 |
| AMD | ❌ | 协议完全正确，但对 Codex 的请求**返回空答案**（0 token） |

### 排查 AMD 的过程（记录一下，以后可能用得上）

1. 一开始报 `Model does not support native web search` → 关掉 `web_search` 后不报了
2. 但还是不出字，`tokens used 0`
3. 开调试日志（`RUST_LOG=debug`）看到：
   - `POST /responses status=200 OK` —— 请求成功
   - 但 `output_token_count=0` —— 服务端返回空
4. 手写请求打它的 `/responses`，**流的格式完全标准**（`response.created` →
   `output_text.delta` → `response.completed`），内容也对
5. 结论：AMD 对**简单请求**正常，对 **Codex 那种带工具的复杂请求**返回空

---

## 另一个坑：梯子会拦截国内站

中途开过一个开关 `respect_system_proxy = true`（让 Codex 走系统代理），
结果日志里出现：

```
proxy(http://127.0.0.1:7892/) intercepts 'https://developer.amd.com.cn/'
```

**梯子把国内站也拦了**，AMD 直接挂掉。

现在已经**关掉**这个开关（`respect_system_proxy = false`），
因为当前用的都是国内中转站，不需要绕道。

---

## 配置文件长什么样

位置：`C:\Users\有花无实\.codex\config.toml`

关键部分：

```toml
model = "gpt-5.6-luna"
model_provider = "unsee-luna"
web_search = "disabled"

[model_providers.unsee-luna]
name = "unsee Luna"
base_url = "https://sub.unsee.you/v1"
wire_api = "responses"
env_key = "UNSEE_LUNA_API_KEY"

[features]
respect_system_proxy = false
```

**key 不存在配置文件里**，存在用户环境变量里：

| 变量名 | 对应站 |
|---|---|
| `UNSEE_LUNA_API_KEY` | unsee Luna（当前默认） |
| `GCMOD_API_KEY` | gcmod |
| `AMD_API_KEY` | AMD |

---

## 怎么验证它工作正常

```powershell
# 命令行版路径
$cli = 'C:\Users\有花无实\AppData\Local\OpenAI\Codex\bin\247581e40ee272fb\codex.exe'

# 检查配置
& $cli doctor

# 发一句话
& $cli exec -s read-only --skip-git-repo-check "只回复两个字：成功"
```

看到 `codex` 下面出了字、`tokens used` 不是 0，就是通的。

---

## 怎么切回官方 ChatGPT

1. 把 `config.toml` 里这两行删掉或注释掉：
   ```toml
   model = "gpt-5.6-luna"
   model_provider = "unsee-luna"
   ```
2. 把 `[features]` 里的 `respect_system_proxy` 改回 `true`
   （否则连不上 chatgpt.com）
3. 重新搞一份 `auth.json`

---

## 备份文件

| 文件 | 说明 |
|---|---|
| `C:\Users\有花无实\.codex\config.toml.bak-before-keys` | 改之前的原始配置 |
| `C:\Users\有花无实\.codex\auth.json.bak-synthetic` | 插件导出的那份登录凭证 |

---

## 遗留问题

1. **慢**：unsee Luna 一次回复 20~60 秒，干重活会很磨人
2. **gcmod 可惜了**：它最快（2~3 秒），但 `/responses` 流式是坏的。
   哪天它修好了，把 `model_provider` 改成 `gcmod`、`model` 改成
   `deepseek-v4.1-flash` 就行
3. **AMD 也没完全死**：如果哪天 Codex 支持"不带工具"的模式，或者有人
   搞出了 model catalog 来压制 `apply_patch` 工具，它就能用

---

## 2026-10-04 补丁：Codex 0.160 不再认 `wire_api = "chat"`

**现象**：Codex 整个打不开 / 一用就报

```
Error loading config.toml: `wire_api = "chat"` is no longer supported.
How to fix: set `wire_api = "responses"` in your provider config.
in `model_providers.deepseek.wire_api`
```

**原因**：新版 Codex（0.160.0）**只支持 responses 协议**，旧写法 `chat` 一律拒绝。
注意这是**整份配置加载失败**——哪怕当前用的不是这一家，只要有哪一家写着 `chat`，
Codex 就整个起不来。所以早期"DeepSeek 官方走 chat"的写法必须改掉。

**修法**：把 `[model_providers.deepseek]` 的 `wire_api` 改成 `"responses"`。

实测（2026-10-04）：DeepSeek 官方的 `https://api.deepseek.com/v1/responses`
**是通的**，返回标准 responses 格式；所以官方线和免费线现在能随便切。

**当前默认**（按"能白嫖就白嫖"）：

```toml
model = "deepseek-v4-flash-0731"
model_provider = "tokenplan"
model_catalog_json = "C:/Users/有花无实/.codex/models-tokenplan.json"
```

想切回官方：双击 `tools\tokenplan-switch\Codex-切回DeepSeek.cmd`（已确认不会再弄坏）。

**验证命令**（跑通了会打印 `model:` / `provider:` 并出字）：

```powershell
$cli='C:\Users\有花无实\AppData\Local\OpenAI\Codex\bin\8aaf1547b825b104\codex.exe'
& $cli exec -s read-only --skip-git-repo-check "只回复两个字：成功"
```

另外 `preferred_auth_method` 这个键现在会被忽略（每次都刷警告），已删。
