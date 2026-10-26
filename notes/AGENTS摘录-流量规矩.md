# 三点六、流量规矩（用户 2026-09-28 定）

> **本文件是从全局声明 `D:\software\dsh-home\AGENTS.md` 搬出来的章节**（2026-10-17 瘦身）。
> 内容是原文一字未改，只是挪了地方。

---

## 三点六、流量规矩（用户 2026-09-28 定）

**任何一次操作，只要预计会消耗 2 GB 以上网络流量，先停下来问用户，得到同意再执行。**

- 覆盖范围不限于下载：拉大模型 / 大文件、`git clone` 大仓库、`npm` / `pip` / `docker` 拉大依赖、  
  批量下视频素材、云盘或镜像站拖包、写脚本批量抓接口数据，都算。
- 判断口径是**单次操作**（一次下载 / 一次安装 / 一次批量抓取），不是整个会话的累计。
- **拿不准就先问**：预计接近 2 GB、或源没标体积、或要重试多轮才可能凑够流量，一律按"要问"处理。  
  不要抱着"先跑起来看看"的心态动手。
- 问的时候一句话说清楚三件事：**下什么、大概多大、从哪下**，然后等他说可以。
- 他同意后就正常做；中途发现会明显超出他点头的量，再停下来说明，别默默超。

### 省流量 & 代理（用户 2026-10-03 追加：「尽量少花流量，我的流量并不多」「这是我没开代理，你要告诉我」）

- **能不联网就不联网**：先在工作区、本机已装的东西、本地缓存里找；本地没有才去抓网。
- **同一个东西别反复抓**：一次把关键词跑完、结果存成本地文件复用，别一个词一个词地反复请求。
- **抓取有节制**：别翻十几页、别下整个仓库或整个视频；图片 / 视频尤其省。
- **抓网前先探代理**（`127.0.0.1:7892`）：  
  `Test-NetConnection 127.0.0.1 -Port 7892 -InformationLevel Quiet`（True = 开着）。
  - 通 → 走代理；不通 → 可以先试直连；**都不通就停下来告诉他「要开代理」**，别原地反复重试。
  - **代理没开时主动说一声**，别默默直连、也别默默失败；报进展带一句网络情况。

### 提新点子前先查工坊（用户 2026-10-04 要求：「你写个全局啥的吧。每次搜一下」）

- **开口之前先查**，不许凭印象说"应该没人做"。血泪史：沙盒 / 种子分享 / 回放 / 卡牌交易 / PVP /  
  自走棋 / 放置 / 卡牌编辑器 / 战绩海报 / 互坑卡包 / 复活队友 —— 这些他都提过，一查全有人做过。
- 搜索页：`https://steamcommunity.com/workshop/browse/?appid=<appid>&browsesort=textsearch&section=readytouseitems&searchtext=<词>`  
  （杀戮尖塔2 = `2868840`，一代 = `646570`；英文词 + 中文词各搜一轮。）  
  页面里 `filedetails/?id=` 后面的锚文本就是标题。
- 代理经常没开：先探 `127.0.0.1:7892`；不通先试直连；**都不通就停下来让他开代理**，别硬搜。
- 查完给结论：没人做过就说清搜了哪些词；有人做过就直接说，并说清差在哪。

### 免费额度清单（用户 2026-10-04 要求写进全局，省得每次重测）

- **主力 = DSH 免费车道**（免密不花钱）：`node D:\software\dsh-home\lib\free-model.mjs "问题"`。  
  2026-10-04 实测可用：`space-bunny-free` / `ling-3.1-flash-free` / `nemotron-3-ultra-free` /  
  `mimo-v2.6-flash-free` / `mimo-v2.5-free` / `fledge-alpha-free` / `longcat-2.5-preview-free` /  
  `jev-1.13-free`；不可用：`deepseek-v4-flash-free`、`ling-3.0-flash-fin-free`、`muse-spark-*`（地区封禁）；  
  `nemotron-3.5-lightning-free` 一次要 138 秒，别用。
- **多路调度器**：`tools/ask.py`（作品仓库根目录）—— 串起所有 key，一家挂了自动换下一家。  
  `--list` 看配置 / `--test` 挨个试活 / `--only <家> "问题"` 指定一家。
- **2026-10-04 实测**：✅ AMD `Qwen3.8-Flash-Next`（**直连** 1.8 秒，能出内容）；  
  ✅ DeepSeek 官方 key（能用但**要花钱**，只当兜底）；  
  ❌ gcmod 公益站（504 Gateway Time-out）；❌ fiime 公益站（503）；❌ unsee 老 key（401 失效）。
- **key 位置**：`D:\software\dsh-home\.credentials.yaml` 的 `refs:` 段  
  （GCMOD / FIIME / AMD / DEEPSEEK / UNSEE / UNSEE_LUNA）。**绝不把 key 的值打印到对话里**。
- ⚠️ **梯子会拦国内站**：gcmod / fiime / AMD 这几个**必须直连**，走代理反而连不上。
- ⚠️ **`127.0.0.1:3003` 那个"反代网关"其实没装**（`D:\dockerbuild` 不存在、docker 也没跑）——  
  会话里出现的那段只是**别人 repo 的 README**，别再去找它。
- ⚠️ `unsee` 的 key 主人标过「禁止蒸馏」：**别拿它大批量跑数据**。

### WorkBuddy 免费模型（2026-10-04 打通）

- 本机 DSH 插件 `dsh-connect-workbuddy` 把**已登录的 WorkBuddy 账号**接成模型源，**国内版 + 国际版都能用**。
- **直调工具 `tools/wb.mjs`**（作品仓库根目录）：  
  `node tools/wb.mjs "问题"` / `--region cn` / `--list` / `--model gpt-6-astra`。
- ⚠️ **插件默认找不到桌面端 → 国际版显示"未登录"**：必须给它  
  `WORKBUDDY_APP_EXECUTABLE=D:\software\WorkBuddy AI\WorkBuddy.exe`（`wb.mjs` 内部已自动设；
  ⚠️ **2026-10-12 实测：本机文件名是 `WorkBuddy.exe`，不是 `WorkBuddyAI.exe`**——
  写错的话 `bin.js status` 会显示 `signed-out / hostBundle: stale`，让人误以为没登录。
  DSH 界面里要用得自己设环境变量 + 重启 DSH）。
- ⚠️ 国际版**第一条消息必须是 system**，否则报 `code 11128`（`wb.mjs` 已处理）。
- 账号：国内「有花无实」／国际 `zjj357951@gmail.com`。国际版模型：  
  `deepseek-v4.1-flash`(据用户说免费) / `gpt-6-astra` / `grok-4.7` / `gpt-5.6-*` …；  
  国内版：`deepseek-v4.1-flash` / `glm-5.3` / `kimi-k3-1` / `minimax-m3` …
- 诊断：`node <插件目录>/lib/bin.js status|doctor`。⚠️ 蹭额度有账号风险，别高频猛刷。

---
