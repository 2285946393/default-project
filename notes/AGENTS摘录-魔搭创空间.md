# 六点九、朋友的魔搭创空间（**最高优先级**，2026-10-10 用户指定）

> **本文件是从全局声明 `D:\software\dsh-home\AGENTS.md` 搬出来的章节**（2026-10-17 瘦身）。
> 内容是原文一字未改，只是挪了地方。

---

## 六点九、朋友的魔搭创空间（**最高优先级**，2026-10-10 用户指定）

> 用户原话：**「我们有用的设为最高优先级使用。在全局声明里面。」**

朋友 = 魔搭用户 **「三千」/ `lf3000y`**，主页 `https://modelscope.cn/profile/lf3000y`。  
他做了 **4 个创空间**，全部是 `programmatic`（带 API）、带 MCP 标记、状态「运行中」。  
**能用上的场景一律先走这里** —— 出图、抠图、超分、配音，排在第七节的老路线前面。

| 创空间                                             | 干什么                                                                                              | 优先用在哪                    |
| ----------------------------------------------- | ------------------------------------------------------------------------------------------------ | ------------------------ |
| **Image Studio**<br />`lf3000y/image-studio`    | 出图一条龙：RMBG-2.0 抠图、Real-CUGAN **4 倍**超分、Qwen-Image-2.1 文生图 / 图生图（最多 10 张参考图）、**拆图层导出 PSD**、GIF 逐帧 | **画图默认第一个用它**            |
| **AI Studio**<br />`lf3000y/ai-studio`          | 看图对话（Taichu 5.0-9B）+ **语音合成 VoxCPM2**（女声 / 方言口音 / 参考音频克隆）+ 斗地主 AI                                | 要**配音、念旁白、歌词 PV 人声**时先用它 |
| **RMBG Studio xGPU**<br />`lf3000y/rmbg-studio` | 纯抠图（RMBG-2.0 的 GPU 加速版），OpenAI 兼容                                                                | 只抠图、要快时用它                |
| **RMBG Studio**<br />`lf3000y/RMBG-2.0`         | 纯抠图（旧版，非 xGPU）。⚠️ 朋友说**计划下线**，2026-10-10 实测仍 `Running`                                              | 兜底，一般不用                  |

**调用地址**（魔搭创空间固定格式 `studio-<作者>-<名字>.api-inference.modelscope.net`）：

- `https://studio-lf3000y-image-studio.api-inference.modelscope.net`
- `https://studio-lf3000y-ai-studio.api-inference.modelscope.net`
- `https://studio-lf3000y-rmbg-studio.api-inference.modelscope.net`
- `https://studio-lf3000y-rmbg-2-0.api-inference.modelscope.net`（旧版抠图；**名字里的 `.` 要写成 `-`**）

**都是 OpenAI 兼容口**（官方 SDK 直接把 base_url 指过去就能用）：

- 出图 `POST /v1/images/generations`
- 改图 / 抠图 / 超分 `POST /v1/images/edits`  
  （`model=rmbg-2.0` 抠图、`model=real-cugan-x4` 超分、`operation=rmbg_then_upscale` 先超分再抠图）
- 对话 `POST /v1/chat/completions`、`POST /v1/responses`
- 配音 `POST /v1/audio/speech`（`model=voxcpm2`；`voice` 可用 `natural/warm/bright/calm/deep/story`）
- 看家底 `GET /v1/models`、`GET /api/status`

⚠️ **必须有魔搭访问令牌**，不带一律返回  
`401 Authentication failed, please make sure that a valid ModelScope token is supplied.`  
令牌去 `https://modelscope.cn/my/myaccesstoken` 取，调用时带 `Authorization: Bearer <token>`。  
**令牌已存（2026-10-10 用户提供）**：明文单行在 **`D:\software\dsh-home\.modelscope-token`**，  
读法 `T=$(tr -d '\r\n' < "D:/software/dsh-home/.modelscope-token")`，请求头带 `Authorization: Bearer $T`。

⛔ **绝对不要把令牌明文写进本文件**（或任何可能被分享的文件）—— 只写"去哪个文件读"。  
⛔ 也别让用户贴进群聊 / 截图（2026-10-10 已提醒过他）。

✅ **用户说自己「没有任何额度」，实测完全不影响**：创空间算力由**作者（lf3000y）**出，  
调用者只提供令牌鉴权。2026-10-10 实测三个创空间 `/v1/models` 全 200，文生图 26 秒出一张 1024×1024。

**实测记录（2026-10-10，可直接照抄）**：

- **image-studio 模型 id**：`rmbg-2.0`（抠图）、`real-cugan-x4`（4 倍超分）、`qwen-image-2.1` / `-4k`（文生图）、
  `qwen-image-2.1-rmbg` / `-4k-rmbg`（生成 + 抠图）、`qwen-vl-image-2.1` 系列（带参考图）、`taichu5-9b-bf16`、`qwen3.5-4b-q4_k_m`、`auto`
- **ai-studio 模型 id**：`TaichuAI/ZDTaichu5.0-9B`（对话）、`voxcpm2`（语音合成，`capabilities:["speech"]`）、`NeoHorse-Jev-4B`、`auto`
- **rmbg-studio**：只有 `rmbg-2.0`
- **出图返回**：`{"created":..,"model":..,"data":[{"mime_type":"image/png","meta":{...},"url":"..."}],"cache_hit":..,"server_elapsed_ms":..}`
  - ⚠️ `url` 是**带签名的临时地址，1 小时过期**（`X-Amz-Expires=3600`）→ **拿到立刻下载保存**，别存 URL 以后再用。
  - 有 `cache_hit`：同 prompt + 同参数会命中缓存，第二次秒回。
  - `meta.prompt_enhanced` = 服务端会**自动增强 prompt**；`meta.seed` 可用于复现。
- **`GET /api/status` 看队列与显存**：`queue.waiting/active/capacity`、`gpu.devices[].memory_free_mib`。
  实测 GPU = **NVIDIA L20（46 GB）**，出图前 free ≈ 19 GB。
- 耗时参考：**1024×1024 一张 ≈ 26 秒**。
- ⚠️ **`size` 会被显存上限压小**：请求 `1536x864`，实测回来还是 **1024×576**
  （`meta.request_size` 才是你要的、`meta.width/height` 是实际出的）。**别指望一次出大图。**
- ✅ **要高清就靠超分**：`POST /v1/images/edits` 传 `model=real-cugan-x4` + multipart 字段 `image=@文件`
  → **4 倍**（1024×576 → **4096×2304**，实测 **9~11 秒**，`meta.operation="upscale"`）。
  **比重新出图快，而且不吃 prompt。** 出图 → 超分 → 缩到目标尺寸，是稳的高清路线。
- ⚠️ 出图**不要挂"已经画歪的同类图"当参考图** —— 2026-10-10 斗地主桌图第一批就是这么废的：
  想画斗地主桌，却把德州桌当"只参考轮廓"的参考图传上去，AI 把刻线一起抄了，
  出来四张"德州桌换个配色"。**参考图只会把它的布局一起带过来。**

### 📌 动态 & 怎么核实（2026-10-10）

**2026-10-10 用户转来一张群聊截图**，朋友（「三千喵喵」）自己说：

1. **AI Studio 有 OpenAI 兼容 API**（原话「完提供了 openai 兼容的 api」→「woc」；一串「完」是打字手滑，他自己也吐槽了）—— 与上面已记录的一致。
2. **「后面在计划把 RMBG 那个下线掉」** → 指旧版 `lf3000y/RMBG-2.0`（非 xGPU 那个）。**实测 2026-10-10 仍是 `Running`**，还没下线 → 趁还在，能用就用。
3. **「启动一个 ai 生成视频的创空间」「就叫 video-studio」** → **实测尚未创建**：profile API 返回 `TotalCount = 4`，名字只有 `ai-studio` / `image-studio` / `rmbg-studio` / `RMBG-2.0`，**没有 video-studio**。等他建好，`video-studio` 自动纳入最高优先级（AI 生成视频）。

**怎么核实创空间清单（别靠记忆，也别瞎猜）**：

- ✅ **查 profile API**：`GET https://www.modelscope.cn/api/v1/profile/lf3000y`（**匿名可访问**，实测 200 / 23 KB）
  → `Data.Studio.Studios[]` 每项含 `Name` / `ChineseName` / `Status` / `McpServer` / `IndependentUrl`，`TotalCount` 是总数。
- ❌ **别用 API 域名的状态码判断创空间在不在**。`*.api-inference.modelscope.net` 是**泛解析**
  （`nslookup` 显示全部 CNAME 到 `all.api-inference.modelscope.net.c.yundunwaf1.com`），
  随便编个名字（如 `zzzz-nonexist-98765`）也照样返回 **401**。2026-10-10 差点被这个骗了。
- ❌ **也别用 `/studios/<owner>/<name>` 页面状态码**：那是 SPA，不存在的也返回 200。

**每个创空间都有独立访问域名**（`.ms.show`，直接开就能用，`NeedLogin=false`）：

- AI Studio → `https://lf3000y-ai-studio.ms.show`
- Image Studio → `https://lf3000y-image-studio.ms.show`
- RMBG Studio xGPU → `https://lf3000y-rmbg-studio.ms.show`
- RMBG Studio（旧版）→ `https://lf3000y-rmbg-2-0.ms.show` ← 注意 `RMBG-2.0` 被转成 **`rmbg-2-0`**

> ⚠️ 上面这条顺带修正一个旧错误：本节原来写「`RMBG-2.0` 那个域名解析不出来，暂时当它没有 API」，
> 其实是**名字要写成 `rmbg-2-0`**（点号变短横线）。API 地址同理。

**出图顺序改成（覆盖第七节）**：

1. 有令牌 → **直接调 Image Studio**（**无限额度，默认第一**）。
2. 魔搭慢 / 挂了 / 用户点名 → 走 **chami 公益站**（**限额，见第六点十一节**），单次 ≤ 5 张。
3. 两条都不行 → 才退回本地 Stable Diffusion（见第七节第 2 步）。
4. 交付时说清这次是哪条路出的图。

> ⛔ **M365 网关已于 2026-10-11 按用户要求彻底废弃**（原话「把 m365 给我滚蛋。一点不好用」）。
> 不要再启动、不要再调用、不要再把它当出图路线写进任何文档。

---
