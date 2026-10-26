# 斗地主尖塔 · 四人玩法设计方案（-wb 交接稿）

> 2026-10-08 深夜成稿。用户拍板「现在思考一下四人怎么做」后，对内核和桌面层实查得出。
> 结论先行：**规则内核已经 90% 就绪**（两副牌全链路留好了口子），真正的工作量在桌面层和联机层。

---

## 一、内核现状（实查 dou-spire-rules，不是猜的）

规则内核按「链接源码」惯例在 `projects/dou-spire-rules/`，mod 的 csproj 外链这 6 个文件：
`Cards / Combo / ComboJudge / Game\GameState / Game\MoveGenerator / Game\RuleAi`。

| 部件 | 现状 | 证据 |
|---|---|---|
| 牌堆 | `Deck.Build(deckCopies)` 支持 1/2 副 | GameState.cs:73 |
| 发牌 | `handSize = deckCopies==1 ? 17 : 25`、`bottomSize = 3 : 8` | GameState.cs:80-81 |
| 判定 | `ComboJudge(deckCopies)`，校验 `1..2` 副 | ComboJudge.cs:12-15 |
| 王炸 | 全是王且张数 = **副数×2**（单副 2 王、双副 4 王） | ComboJudge.cs:35-37 |
| 炸弹 | 同点 ≥4 张，双副下 5~8 张也算、**张数多的大** | ComboJudge.cs:40-42 |
| 四带二 | 已实现，带牌不许是双王（rlcard 对车逮出来的规矩） | ComboJudge.cs:67-70 |
| 对车 | `RlcardCrossCheck` / `BidirectionalCrossCheck`（970 万子集 0 差异那套） | 根目录 |

**唯一的内核堵点**：`GameState.NewGame()` 写死 3 席 ——
`for (var s = 0; s < 3; s++)`、`names3` 默认、`deck.Skip(3 * handSize)`、定地主循环 `s < 3`
（GameState.cs:83-108）。mod 侧唯一调用点 `DouSpireTable.cs:553`。

> 双副牌的发牌数字 25+8 正好是 **4 人制**的分法（4×25+8=108），说明当初写内核时就是冲着四人留的。

---

## 二、要用户拍板的三件事

1. **1v3 还是 2v2？** 建议 **1v3**（地主独自打三家，结算 ±3 倍）。
   2v2 是掼蛋/跑得快的玩法，等于另做一个游戏，不建议塞进斗地主。
2. **两副牌（108 张）还是一副牌（54 张）？** 建议**两副牌**。
   一副 4 人每人只有 13 张，顺子飞机都凑不齐，没法玩；两副 25 张才是主流四人斗地主。
   且内核对两副牌的支持已经写好。
3. **王炸规则**：内核现状 = 两副牌王炸要凑齐 **4 张王（2 大 2 小）**，一大一小只是对子。
   这跟欢乐斗地主四人版的通行规矩一致，建议照用，别自创。

结算公式（1v3、两副牌）：地主赢 → 地主 +3×底注×倍数，3 个农民各 −1×；
地主输 → 地主 −3×，3 个农民各 +1×。炸弹每个再 ×2，与现有一致。

---

## 三、改动清单（按文件，全部实查过）

### A. 内核（dou-spire-rules，半天）
- `GameState.NewGame` 加 `playerCount` 参数（默认 3 兼容原型）：
  循环上限、names 默认（"我/电脑A/电脑B/电脑C"）、`Skip(playerCount * handSize)`、定地主循环。
- `ComboJudge / MoveGenerator / RuleAi` **不用动**：判定已按 deckCopies 泛化，AI 基于 GameState.Players。
- 对车补充：rlcard 没有"4 人两副"的权威实现，照抄对车不可行。
  改用**不变量自测**：每局发牌 108 张守恒、底牌归属唯一、随机局模拟到终局不卡死、
  王炸/炸弹压盖关系穷举（复用 BidirectionalCrossCheck 的框架）。

### B. 桌面层（单机四人可玩，约 2~3 天）
- `DouSpireLayout.cs`：`Seats` 3→4，加**左席**（我=下、下家=右、对家=上、上家=左）；
  覆盖文件校验 `seat > 2` → `> 3`（:101）；`Apply` 循环 `i < 3` → 4（:138）。
- `DouSpireTable.cs`（seat 相关引用 ~146 处，多数是数组/循环，机械扫一遍）：
  - `_chars/_watchHand/_whaleFace` 等 `[3]` 数组 → `[4]`，`s < 3` 循环 → 4；
  - **发牌/开局**：`NewGame(...)` 传 `playerCount=4, deckCopies=2`；地主手牌 = 25+8 = 33 张；
  - **底牌展示** 3 张 → 8 张；
  - **手牌行**：17 张的间距公式压到 ~0.65 倍；地主 33 张建议**自动分组着色 + 悬浮放大**，
    摆不下就双行（先做单行压缩，实测再定）；
  - **猜拳定地主**：4 人沿用猜拳，平局重摇（现逻辑天然兼容，只改人头数）；
  - **明牌/加倍**：AI 自动表态循环扩到 4 家；`HostDecideAi*` 同理；
  - **结算**：`PayOut/SettleAllGold` 改 1v3 分账（地主 ±3），结算文本 4 行，记分板 4 家；
  - **重新选人/篝火选人**：选人池扩到 1 个我 + 3 个 AI 席。
- `DouSpireVoice.cs`：`Players` 数组本来就是 `[4]`（"留足位置"），**不用动**；
  座位文案"座位0/1/2"不用改，第 4 席自动有声音。
- `DouSpireCharacters.cs`：发牌池、表情 `FacesOf` 按席位数组化（跟着 Table 的 [3]→[4] 一起扫）。
- `DouSpireConfig.cs`：加「牌局人数」设置项（3 人 / 4 人），菜单和篝火各一个入口，**不拆第二个 mod**。

### C. 联机层（约 1 天，最后做）
- `DouSpireNet.cs`：房间人数协商 3→4；协议里 seat 字段本来就是 index，广播大多通用；
  主机替远程玩家表态的兜底循环扩 1 家；「都点了才发牌」计数改 4。
- 联机 4 人测试：用户已有好友代码 1572415305 的路子，找 2 个朋友就能开。

### D. 打磨（看反馈再排）
- AI 农民配合：现在 RuleAi 各打各的，1v3 下三家互压的观感问题；
  v2 再加"农民不压队友的牌"的简单约束。
- 地主 33 张手牌的显示打磨、结算演出（金币雨 4 家坐标）、四人表情布局。

---

## 四、风险与边界

1. **手牌拥挤是最大 UI 风险**：25/33 张 vs 现在 17 张。方案已列（压缩/分组/悬浮），必须真人实测。
2. **4 人两副牌没有权威对车源**：内核正确性靠不变量测试 + 现有 rlcard 对车回归（3 人单副那条不能退化）。
3. **联机 4 人没测过是既知短板**：这次一并把 4 人联机做上，正好补「没朋友测试」的缺口。
4. 编译部署铁律照旧：0 警告 0 错误 + md5 核对 + 用户重启游戏实测。

---

## 五、建议的开工顺序

A（内核，半天）→ B（单机四人，2~3 天）→ 用户实测 → C（联机四人，1 天）→ D。
第一步今天就能动：`GameState.NewGame` 的 playerCount 参数化 + 不变量测试，改动小、收益立现。

---

## 六、AI 代打调研：接 DouZero（2026-10-09 凌晨查证）

用户拍板「找个比较聪明的 AI 代打，从 GitHub」。实查结论：

**选型：DouZero（快手，github.com/kwai/DouZero）**
- Apache-2.0 许可证（查过 api.github.com，工坊无法律风险），4.6k★，ICML 2021
- Botzone 344 个斗地主 AI 排第一；论文实证它**学会了农民配合**（出小牌喂队友）
- 预训练三件套：landlord / landlord_up / landlord_down（各坐一个位置用各的模型），
  有 ADP（分差目标，比赛级）和 WP（胜率目标）两版
- 没有现成 ONNX，需要自己转：PyTorch ckpt → torch.onnx.export（装 CPU 版 torch 一次性转）

**运行路线（推荐 A）**
- A. ONNX：转成 onnx 后用 Microsoft.ML.OnnxRuntime（NuGet，CPU 版）在 mod 里直接推理。
  原生 dll ~30MB + 三个模型 ~15MB，mod 从 145MB 涨到 ~190MB。
- B. 纯 C# 手写 forward（零依赖但工作量大）—— 备选。
- C. 本地 HTTP 服务（DouZeroApi 那种）—— 排除，工坊玩家不可能装 Python。

**集成工作清单**
1. 模型获取：官方百度网盘（提取码 4624），douzero_ADP 三个位置 ckpt
2. 转换脚本：ckpt → ONNX（torch CPU，装进 managed venv，一次性）
3. C# 端 `DouZeroAi.cs`：
   - 移植 DouZero 的状态编码（features.py 的 GameEnvEncoder：手牌/出牌历史 LSTM 序列/位置特征）
   - 我们内核 MoveGenerator 出合法动作 → DouZero 动作 one-hot 编码（两边都是 rlcard 口径，对得上）
   - 每步把**全部候选动作批量**过网络取最高分（官方演示就是这么算每种打法胜率的），CPU 毫秒级
4. `DouSpireConfig` 加「AI 难度」：低=现有 RuleAi / 中=DouZero-WP / 高=DouZero-ADP
5. 兜底铁律：模型缺失/推理失败 → 自动退回 RuleAi，mod 永不因 AI 崩；致谢加 kwai/DouZero

**边界**
- DouZero 只有**三人**斗地主的模型 —— 四人两副牌没数据，四人模式 AI 仍走 RuleAi + 农民配合启发式
- 对车策略不变：DouZero 只在「合法动作里挑一个」，合法性判定仍归我们内核，判牌正确性不受影响

### 六·补：模型已到手（2026-10-09 00:25）
- 计划外收获：HuggingFace 镜像 `palemoky/douzero-baselines`（apache-2.0 标记）**连转好的 ONNX 都带了**，
  torch 转换步骤整个取消。
- 已下载 6 个模型到 `projects/dou-spire/_douzero/onnx/`（ADP×3 + WP×3，各 5.8~6MB，共 ~35MB），
  全部用 onnxruntime 实测加载通过。
- 实测 IO（决定 C# 端写法）：
  - 输入 `z` = [batch, seq_len, 162]（出牌历史编码，整段历史每次重喂，LSTM 隐状态不用跨回合维护）
  - 输入 `x` = [batch, 373(地主) / 484(农民)] —— **含动作编码**（x_no_action + 54 维动作 one-hot 拼接）
  - 输出 `values` = [batch, 1]（每个候选动作的胜率估计）
  - ⇒ C# 推理 = 每个合法动作拼一行 x + 共享 z，一次 batch 前向，argmax
- 代理备忘：本机活代理口是 `127.0.0.1:7892`（env 里的 54556 是死的）；gdown 对 Google Drive 文件夹
  会 404，HF 的 `resolve/main/` 直链 + curl 最稳。

## 七、Phase A 落地 + 参考实现对答案（2026-10-09）

### A. 内核已落地并全绿
- `GameState.NewGame` 加 `playerCount`（0=按 deckCopies 推导 1副→3 / 2副→4），names 默认补"电脑C"，
  发牌不均直接 throw（`(54*copies-底)/人数` 必须整除）。
- `Simulation.RunBatch` 加 deckCopies/playerCount 透传，护栏 2 副牌放宽到 6000 步。
- `Program.cs` 新增 `--sim4` 入口 + 自测九·B 段（发牌守恒/每张恰好 2 份/底牌⊂地主手牌/自战不卡死/3 人回归）。
- 结果：**30623 条自测全过**；`--sim4 500`：地主胜率 53.6%，平均 71.5 回合，**0 卡死**；
  3 人老路 100 局照常。mod 编译 0 警告 0 错误，dll md5 两处一致（11c5a4a5）。

### B. 参考实现对答案（用户提议"看 github 有没有现成的"）
- **HullQin/poker + poker_fe**（三人1副3底/四人2副8底，和我们的配置一模一样）：
  但它**不做牌型校验**（drop_card 只查"牌在不在你手上"，规则靠玩家自觉，还带悔棋）。
  只能印证外围流程：4人=108张 25×4+8、地主拿底牌先出、循环发牌 ✅ 一致。对内核没帮助。
- **WangAnXin/poker**（Java，88★，四人两副牌 108 张，有真判定）：
  - `OutCardStyle.judgeCardStyle` + `CrushPreCard`：炸弹比大小=**先比张数再比点数** ✅ 与我们一致；
  - 四大天王=**4 张王**才算顶级炸弹，两副牌下两张王只是对子 ✅ 与我们"王炸=王总数=副数×2"一致；
  - 顺子/连对/飞机上限 A（2 不能进顺子）✅ 一致；
  - 差异（他们的简化，不用跟）：只有三带二没有三带一、飞机只带两对、没有四带二。我们是超集。
- 参考代码放在 `projects/ref-ddz4p/`（HullQin 无 LICENSE 只读不抄；WangAnXin 同理，仅对答案）。

### C. 桌面层进行中
- `DouSpireLayout.cs` 已改完：Seats 固定 4 槽；默认摆位 3 人（我/右/左）原样保留，
  4 人 = 我(下)→下家(右)→对家(上，HUD 0.5/0.115)→上家(左)；
  layout.txt 行号改按**位置**固定（0=我 1=右 2=左 3=上），4 人模式座位→行映射 {0,1,3,2}，
  3 人共用一份文件不打架。
