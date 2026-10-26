# 斗地主尖塔 - 本体角色语音包配装（2026-10-08 17:20）

## 干了什么
用户下载了两套真人语音素材，转码 ogg（EBU R128 响度统一 -16 LUFS、单声道 44.1k、q4）
按 `<编号>_<事件>[_n].ogg` 命名，放进 `assets/voice/<角色>/`。**AI 生成的卢本伟台词按要求弃用。**

## 角色 ↔ 素材
| 角色 | 目录 | 素材来源 | 条数 |
|---|---|---|---|
| 铁甲战士 | `voice/ironclad/` | Downloads 卢本伟真人包 P1–P57 | 26 |
| 故障机器人(鸡煲) | `voice/defect/` | D:\BaiduNetdiskDownload 臭企鹅/MyGO系 wav | 17 |
| 静默猎手 | `voice/silent/` | **空着**（用户没想好） | 0 |
| 亡灵缚师(骨妹) | `voice/necrobinder/` | **空着**（用户没想好） | 0 |

## 战士（卢本伟）槽位映射
- 1_deal: P39 white进入游戏 / P14 123
- 2_landlord: P11 7分06秒卢本伟接管比赛
- 3_myturn: P4 哎说句话好吧 / P46 听得见我说话吗老铁
- 4_hint: P8 你在想什么我都知道
- 5_small: P47 单走一个六 / P5 1毛8
- 6_triple: P30 nice-3
- 7_straight: P16 5678910
- 8_big: P10 5秒大别急我5秒大
- 9_bomb: P9 全体起立
- 10_pass: P1 得得得得得 / P36 okok / P35 nonono / P37 okokcanisay（闲聊档，3.5s 闸门+45%骰子）
- 11_beat: P55 那这波是谁的问题 / P18 fucking crazy
- 12_lastone: P42 阿姨你K都不要吗
- 13_win: P7 陈刀仔 / P2 你们可能不知道只用 / P31 nice125万封顶
- 14_lose: P20 GG游戏结束啦 / P40 wo de fuck
- 15_bid: P6 3 2 1
- 16_nobid: P3 拜拜

## 鸡煲（臭企鹅系）槽位映射
1_deal 凑企鹅糖笑 / 2_landlord 康康企鹅 / 3_myturn 偶内该（素世）/ 4_hint 哈？（爱音）/
5_small cnmd-千早 / 6_triple 受击 / 7_straight 大司马版1 / 8_big cnmd / 9_bomb 囔囔囔 /
10_pass hey / 11_beat 受击2 / 12_lastone 为什么要演奏春日影 / 13_win 爱音糖笑+私人笑声 /
14_lose ~~凑企鹅糖哭~~（**2026-10-11 用户要求删除，已删**）/ 15_bid 大司马版2 / 16_nobid 咕咕嘎嘎合集

## 没用的素材
- AI 卢本伟 9 条 mp3（D:\BaiduNetdiskDownload，用户明确说 AI 不好听不用）
- 灵感菇（8.2s，留作补槽备用）
- 营销号小曲（31.7s，是曲子不是语音；想加背景音乐可入 music-private 歌单）

## 空目录安全性（已核对代码）
`DouSpireVoice.LoadAny`：先 `File.Exists`，缺文件静默返回 null；异常日志封顶 3 条。
猎人/骨妹空目录 **不炸、不刷屏**。`Pick()` 变体轮播对空目录也安全。

## 构建与部署
- `dotnet build -c Release`：**0 警告 0 错误**。
- ⚠️ 本次部署 dll 为 **Release**（291,840 B，md5 `865f2a46116d3c497c116d246a3527f7`，17:18）；
  之前部署的是 Debug 构建（316,928 B）。功能已用字节搜索验证：ReactBase / BaseVoiceDir /
  VoicePriority / InstallLabels 识别符 + ironclad/necrobinder/defect/silent UTF-16 字面量全在。
- 语音已核对部署到位：`mods/DouSpire/assets/voice/ironclad/` 26 个、`defect/` 17 个。

## 怎么验证（用户）
1. **重启游戏**（旧 dll 在内存里）。
2. 篝火开一局斗地主，用铁甲战士 / 故障机器人各试一局，听发牌/出牌/过牌/炸弹/输赢。
3. 觉得哪条太长/太吵（比如「得得得」8.3 秒、「囔囔囔」7.5 秒），说一声我换短的那条。

## 备注
- 上架工坊前必须处理：`python tools\music-switch.py public`。
> ~~换掉卢本伟 / 臭企鹅两套真人语音~~ —— **2026-10-11 用户明确撤销**：
> 「不要再给我提换卢本伟臭企鹅的声音了。你自己去创意工坊看看有多少各种音乐的引入，遍地都是。」
> 创意工坊上真人语音/音乐引入是常态，**这条不再作为待办，也不再提**。

- 猎人/骨妹素材到位后丢 Downloads 或网盘说一声即可，槽位表见 `notes/斗地主尖塔-本体四人斗地主梗台词.md`。

## ── 2026-10-09 15:50 调整 ──
- 素材源：卢本伟 P1–P57 原包已删；新素材 = GitHub Surager/Rainbow-Fart-Wuwukai（gitee 镜像直连下载，
  `/_work/lbw-new/`，30 条 wav/mp3；raw.githubusercontent 走 UU 代理 502，**gitee 能直连**）。
- **两组对调**（改动前 25 条全备份在 `_work/lbw-new/backup-ironclad-20261009/`）：
  - 10_pass（要不起）↔ 12_lastone（报单）内容互换：得得得/okok/nonono ×4 → 12_lastone_1..4；
    阿姨你K都不要吗 → 10_pass_1。
  - 11_beat（被压一头）↔ 8_big（出大牌）互换：5秒大 → 11_beat_1；那这波是谁的问题 / fucking crazy → 8_big_1..2。
- **新增 5 条**（loudnorm -16 LUFS / 单声道 44.1k / q4）：
  - 4_hint_2 这把我理性分析一下(2s) / 4_hint_3 应该是能选的出来的吧(1s) / 4_hint_4 不是很难选吧(1s)
  - 10_pass_2 漂亮4 / 10_pass_3 漂亮5（认怂反讽）
- ⚠️ 部署目录删了 2 个**过期残留**（10_pass_4、11_beat_2 —— 对调后工程里已不存在，不删会照播旧台词）。
  现工程/部署各 **30 个**，diff 一致。
- 备选没用上的（在 _work/lbw-new/pack/）：伞兵一号(2s)、赌怪(3s)、把k换成狗叫绝杀(3s)、卢本伟广场(2s)、
  我卢本伟没有开挂(2s)、单走一个六 傻逼 走K(2s)、5000人民币哥们(1s)、我现在对天发誓(1s)、
  我这把要赢你很轻松(1s)、今天卢本伟回归(1s)、试一下好吧(1s)、哎呀很烦什么都捡不到(1s)、
  完了选错技能(1s)、卢本伟牛逼x10(7s)、番茄连招(10s)、十七张牌你能秒我(11s)、
  给阿姨倒杯茶卡布奇诺(3s, pack内)、+ 女孩子系列4条。

## ── 2026-10-09 16:00 第二轮 ──
- **代码加了"队友报单"独立槽**：`Mood.MateLastOne` → 铁甲走 `12_matelast`（其他角色先兜底普通报单）。
  桌面⑤按 2v2 队伍判断：自己=LastOne / 队友=MateLastOne / 对手=RivalLastOne。PriorityOf=Important。编译 0/0。
- **卢本伟（31 条）**：11_beat_2 不祥的预感(2s)；12_matelast_1 给阿姨倒杯卡布奇诺(4s)；
  14_lose_3 玩游戏一定要笑(9s→**裁到 5.2s**，尾部拖音)；删 12_lastone_4(okokcanisay)、8_big_2(fucking crazy)。
- **企鹅（15 条，大重排）**：hey→4_hint；哈(爱音)→11_beat；偶内盖→5_small；cnmd→14_lose_2(与糖哭轮播)；
  春日影→10_pass；爱音糖笑→12_lastone；13_win=私人笑声(16s)+**初华喊小祥阴暗爬行(5s，Downloads 裁的)**；
  删两条大司马(7_straight/15_bid 素材)。**空槽：3_myturn、7_straight、15_bid**（等用户生成 MyGO 新梗补）。
- 被顶下来的旧素材全在 `_work/lbw-new/回收-20261009/`（受击2/哈/偶内盖/cnmd/春日影没删——春日影只是换槽）。
- 部署核对：ironclad 31/31、defect 15/15 清单一致，dll 354816B 16:00。

## ── 2026-10-09 16:12 MyGO 批量入库（丰川祥子声线 ×10）──
- 3_myturn_1 過去の弱かった私は、もう死んだ / 7_straight_1 迷子でもいい迷子でも進め / 15_bid_1 一生バンドやめない（三个空槽填满）
- 4_hint_2 最初から全部わかってた / 10_pass_2 みんな戻ってくるよね + 10_pass_3 かわいそ～ / 11_beat_2 うわきも + 11_beat_3 あたし嫌い / 14_lose_3 自分のことばっかり / 1_deal_2 うゆゆんチャンネル
- なんで春日影やったの！留作备用（源在 D:/BaiduNetdiskDownload，未转码）
- 企鹅包 25/25 部署一致。素材源 mp3 在 D:/BaiduNetdiskDownload（16:07-16:10 批次）。
- 9_bomb_2 なんで春日影やったの！(1s) 入炸弹槽（16:13），与囔囔囔轮播。企鹅包 26/26 一致。

## ── 2026-10-11 20:0x 删掉鸡煲失败音 ──
用户：「**另外鸡煲失败的唐笑发癫音效太难听了。删掉**」

- **删的是 `assets/voice/defect/14_lose_1.ogg`**（72,844 B / **8.20s**，原映射 =「凑企鹅糖哭」）。
- 鸡煲（故障机器人 `defect`）的失败槽原有三条轮播，删后剩两条：
  | 文件 | 时长 | 原映射 | 处置 |
  |---|---|---|---|
  | `14_lose_1.ogg` | 8.20s | 凑企鹅糖哭 | **删**（移入 `_work/defect-lose/回收-20261011/`） |
  | `14_lose_2.ogg` | 0.79s | cnmd | 留 |
  | `14_lose_3.ogg` | 2.72s | 自分のことばっかり | 留 |
- **判据**：① 三条里只有它是「笑/哭」类人声（另两条是短骂人和一句日语台词），
  8.2 秒的拖长哭腔正是「发癫」的听感；② 它是三条里**过零率 1526 / 谱重心 1940**（全场最高，最「刺」）
  的那条 —— 与「太难听」吻合；③ 文档第 37 行原映射白纸黑字写着它 =「凑企鹅糖哭」。
- **做法**：没有硬删 —— 先 `mv` 进 `_work/defect-lose/回收-20261011/`，可随时撤回。
- 顺带清了部署目录里的同一份（`mods/DouSpireDev/assets/voice/defect/14_lose_1.ogg`）——
  构建脚本只做拷贝、不会删残留，不手动清会**照播旧音**（2026-10-09 踩过同样的坑）。
- `dotnet build` **0 警告 0 错误**，`DouSpireDev.dll` **603,648 B @ 20:05** 已部署。
- 空槽安全性再核对：`DouSpireVoice.LoadAny` 先 `File.Exists`、缺文件静默返回 null；
  槽里还剩 2 条，`Pick()` 轮播正常。
- 留了个离线试听页给用户确认删对了没：`_work/defect-lose/试听-鸡煲失败音.html`
  （ogg 以 base64 内嵌，双击即听，不依赖网络）。
- ⚠️ **待用户确认**：若用户说的「唐笑」其实指 `1_deal_1.ogg`（原映射「凑企鹅糖笑」，发牌时播）
  那条，再删它。试听页里编了号，用户说个数字即可。
