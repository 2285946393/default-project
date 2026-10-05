# 本地 API Key 体检清单

日期：2026-09-21
测试方式：真实发一条 `1+1=几` 的请求，看**实际返回内容**，不看状态码。

---

## 一、结论：7 个 key 里有 2 个能用

| Key | 站点 | 结果 |
|---|---|---|
| `sk-eQ0AzDcf…`（公益站） | zc.gcmod.cn | ✅ **能用，而且最好用**（见下面第三节） |
| `sk-a49b32aaa…`（老鸡蛋） | sub.unsee.you | ✅ **能用**，只有 `gpt-5.6-luna` |
| `sk-99fc61ff…`（新鸡蛋） | sub.unsee.you | ❌ **额度已用完**（429 `API_KEY_QUOTA_EXHAUSTED`） |
| `sk-BQpb04bf…` | blameupstream.top | ❌ 令牌无效（401） |
| `sk-MuA3mBIU…` | blameupstream.top | ❌ 令牌无效（401） |
| `sk-gVgwxmSQ…` | newapi.96110do.fun | ❌ 站点已失效（404） |
| `m365_c0cba696…` | 本地 127.0.0.1:4141 | ❌ 网关没启动 |

**新鸡蛋确实烧完了**（你自己估的 3 小时很准）。但公益站这条更好用。

---

## 一·五、公益站 gcmod（2026-09-21 新增，目前最优）

- 地址：`https://zc.gcmod.cn/v1`
- 注册链接：<https://zc.gcmod.cn/sign-up?aff=IrR5>
- **实测 8 个模型，7 个能用**：

| 模型 | 速度 | 备注 |
|---|---|---|
| `deepseek-v4-flash` | 2.5 秒 | 最快 |
| `deepseek-v4.1-flash` | 3.6 秒 | **推荐主力** |
| `deepseek-v4-pro` | 4.6 秒 | 会思考（先出推理过程） |
| `glm-5.3-flash` | 5.7 秒 | |
| `agnes-2.5-flash` | 8.4 秒 | |
| `gpt-5.6-luna` | 8.6 秒 | |
| `agnes-3.0-flash` | 27 秒 | 太慢 |
| `agnes-image-2.1-flash` | ❌ | 图片模型，聊天接口报 400 |

- **流式正常**：先流 `reasoning_content`（思考），再流 `content`（正文）
- ⚠️ **限速：1 分钟最多 5 次请求**。
  正常聊天够用，但**别连续快速发问**，会报
  `您已达到请求数限制：1分钟内最多请求5次`

**已接进：**
- DSH 菜单 → `公益站 gcmod（可用）`
- opencode 菜单 → `公益站 gcmod（可用）`

---

## 一·六、AMD 开发者站（2026-09-21 新增）

- 地址：`https://developer.amd.com.cn/radeon/api/v1`
- 一共 6 个模型，**我只接了两个快的**：

| 模型 | 速度 | 说明 |
|---|---|---|
| `Qwen3.8-Flash-Next` | 6~8 秒 | 会思考，字段名是 `reasoning` |
| `Qwen3.8-27B` | 6 秒 | 会思考 |

**没接的（太慢/不稳定）：**
- `DeepSeek-V4.1-Flash` ❌ 跑了 5 分钟没出结果
- `DeepSeek-V4-Flash` 27 秒，还会报"并发上限 32"
- `GLM-5.3-Flash` 38 秒
- `MiniCPM5-2B` 0.85 秒，但模型太小

⚠️ 注意：这个站的"思考过程"字段叫 `reasoning`（别的站叫 `reasoning_content`）。

---

## 一·七、公益站 fiime（2026-09-21 新增）

- 地址：`https://opc.fiime.cn/api/model-service/v1`
- 一共 19 个模型，**我只接了三个好用的**：

| 模型 | 速度 |
|---|---|
| `deepseek-v4-flash-0731` | 2.8 秒（最快） |
| `kimi-k3` | 5.9 秒 |
| `glm-5-2` | 7.0 秒 |

**没接的：**
- `deepseek-v4-pro`（9 秒，会思考）、`deepseek-v4-flash`（25 秒）、`opc-txt-v1`（30 秒）——能跑但慢
- `opc-image-v1` / `opc-video-v1` 等是图片视频模型，聊天接口用不了
- `sensenova-*` / `mixmodel` / `qwen3-8-27b` / `dots-3` / `glm-5-2-l` / `north-mini-code` / `laguna-xs-2-1`
  —— 全是 `503 并发繁忙`，**不是死的，是排队**

⚠️ **这个站高峰会排队**。报 `模型服务当前并发繁忙，请稍后重试` 时，
**等十几秒重发就好**，不是坏了。

---

## 二、唯一能用的那条：unsee 老 key

- 地址：`https://sub.unsee.you/v1`
- 模型：`gpt-5.6-luna`（只有这一个）
- 实测：流式正常，**首字 3.3 秒**，短回复约 16 秒
- 已接入 DSH，菜单里叫 **`unsee Luna（老key·唯一能用）`**

⚠️ 这个 key 你自己标过"**禁止蒸馏，蒸馏直接拉闸**"，所以：
- 正常聊天用没问题
- **不要拿它大批量跑数据、不要拿去训练**，会被封

---

## 三、没 key 的免费站在这次全军覆没

之前 opencode 里配了一堆"不用 key 就能用"的站，实测**一个都不能用**：

| 站点 | 结果 |
|---|---|
| api.qnaigc.com（七牛） | 有 81 个模型，但要 key（401） |
| api.blazeapi.org | 有 551 个模型，但要 key（401） |
| kiraai.vn | 有 51 个模型，但要 key（401） |
| api.sld.lol | 返回的是**网页**，不是 API |
| api.maoniang.org / opc.fiime.cn / gy.guili.xyz | 要 key（401） |
| ruiflux.sbs | 502 服务器挂了 |
| speed.toter.me / speed32.toter.me | 要 key（401） |
| zhai.edu.pl | 被 Cloudflare 挡（403） |
| agentrouter.org / newapi.mailmail.cc.cd | 连不上 |
| www.agnes-ai.com | 404 |

**教训**：`api.sld.lol` 返回 200 但内容是它自己的网页 HTML，
一开始只看状态码误判成"能用"，后来看实际内容才发现是假的。
**以后测 API 一定要看返回内容，不能只看状态码。**

---

## 四、还能挖的方向

1. **qnaigc / blazeapi / kira 这三个站模型很多（81 / 551 / 51 个）**，
   只是缺 key。要是有这两个站的 key，能接进来一大堆模型。
2. **M365 本地网关**没启动。启动文件：
   `D:\work\m365-copilot2api-windows-amd64.exe`
   双击就能跑，主要用来出图（成功率很低，约 1%）。

---

## 五、当前 DSH 模型菜单里有什么

| 菜单名 | 模型数 | 能不能用 |
|---|---|---|
| `公益站 gcmod（可用）` | 5 | ✅ 能用，**最推荐** |
| `AMD 开发者站` | 2 | ✅ 能用（Qwen 系列，快） |
| `公益站 fiime（高峰会排队）` | 3 | ✅ 能用，高峰要重发 |
| `unsee Luna（老key·唯一能用）` | 1 | ✅ 能用，但慢（约 1 分钟） |
| `unsee 鸡蛋中转（新key 额度已耗尽）` | 3 | ❌ 别选，会报错 |
| `deepseek-official`（官方） | — | ✅ 一直在，最稳 |

**建议**：日常用 `公益站 gcmod` 里的 `DeepSeek V4.1 Flash（推荐）`；
它限速时切 `AMD 开发者站` 或官方那条。

> 菜单里那个"额度已耗尽"的条目我没删——万一额度恢复还能用。
> 想清掉说一声。
