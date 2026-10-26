# 斗地主尖塔 —— 设置页中文、语音音量/频率、篝火图标（2026-10-08 收据）

> 本轮四件事全部落地，构建 **0 警告 0 错误**。
> 部署核对：`DouSpire.dll` md5 `93cdc787a7858cce7c0709480021950b`（313,344 B，16:07:43）、
> `assets/DouSpire.icon.pck` md5 `3f7f355c8826afd4aedf364547edce4e`（163,472 B）—— 本地 == 游戏目录。

---

## ① 设置页全是英文 → 真凶找到了，已修

### 现象
截图里那一页：标题「模组配置」、副标题「本页面镜像展示 BaseLib 自动生成的模组配置项。」，
下面一行行全是 `UiScale` / `AiDelay` / `ShowLog` / `SfxEnabled`…（英文属性名）。

### 真凶（不是猜的，三条证据链闭环）
1. 那页是 **RitsuLib 的 BaseLib 镜像页**：`baselib.mirroredPage.title/description` 就是这两句中文（zhs.json:61-62）。
   左树那个「设置分组」= RitsuLib 的 `section.default`（zhs.json:749）。
2. RitsuLib 源码里 `SetupConfigUI` 出现 **0 次** —— 镜像页根本不调它。
   它自己反射读 BaseLib 的 `ConfigProperties`，每行文案走
   `BaseLibMirrorHost.ResolveLabel(属性名)` → BaseLib `ModConfig.GetLabelText`
   → `LocString.GetIfExists("settings_ui", "DOUSPIRE-<SLUG>.title")`，查不到就**回退成属性名**。
3. 我们的 `InjectLabels()` **只**在 `SetupConfigUI()` 里调 ⇒ 没人调 ⇒ 键从来没进表 ⇒ 整页属性名。
   （日志里既没有"已注入"也没有"注入失败"，因为函数压根没被执行过。）

顺带把键格式也核对了一遍：RitsuLib 的 `ModSettingsMirrorSlugPolicy.Normalize`（=`NormalizePublicStem`）
和游戏 `StringHelper.Slugify` 对我们的属性名产出**完全相同**（`UiScale`→`UI_SCALE`），键格式本来就没错。

### 修法
- `DouSpireConfig.InjectLabels()` 改成 `public`，且**不再有静默早退**：
  `LocManager.Instance` 为 null、`GetTable` 抛 `LocException`、合并失败，三种都各自落到日志
  （`GetTable` 查不到表是**抛异常**不是返回 null，这点也一并修正）。
- 新增 `DouSpireConfig.InstallLabels()`（幂等）：注入 + 挂 `LocString.SubscribeToLocaleChange`，换语言自动重刷。
- 调用点：`Entry.Initialize()`（尽早）+ `Entry.EnsureEntryPoints()`（GameReady/MainMenuReady/RunLoaded 时兜底，
  那时本地化**一定**已加载完）+ `SetupConfigUI()`（BaseLib 自己那页的老路，保留）。
- 顺手补了 `DOUSPIRE.mod_title`，设置页里 mod 名显示成「斗地主尖塔」。

### 你怎么验证
进游戏 → 设置 → 模组配置 → 斗地主尖塔：应该看到「桌面界面缩放」「电脑出牌停顿」「显示出牌历史」…
日志里应有一条 `斗地主：设置项文案已注入 settings_ui（语言=zhs）`。

---

## ② 敌方角色声音太小 → 已调大

- 默认值 `OpponentVoiceScale` **0.75 → 1.0**（对手和"我"一样响），滑条上限 **1.5 → 2.0**。
- ⚠️ 这条设置**是存盘的**，改默认值不会覆盖已存的旧值。所以我把你本机那份
  `%APPDATA%\Roaming\SlayTheSpire2\mod_configs\DouSpire.cfg` 里的 `0.55` 直接改成了 `1`。
  （实测效果：对手那两席的音量从 0.47 → 0.85，约 +5 dB。）
- 还嫌小/嫌大 → 设置页「对手那两席的音量」，最高能拉到 2.0。

---

## ③ 有的语音狂响、有的几乎听不到 → 加了"频率闸门"

### 为什么会这样（结构性问题）
- 牌桌上**最高频**的两个事件是「出了一手牌」(`5_small`，单张/对子) 和「压了别人一手」(`11_beat`)，
  这两句**各只有一个音频文件**，于是一局响几十遍 —— 「触发太频繁」。
- 新的一句会**直接打断**正在说的那句（`AudioStreamPlayer.Stream` 被换掉），
  所以三带/顺子/飞机这些低频好句子**说不完就没了** —— 这就是「几乎不会触发」的真实体感。

### 修法（`DouSpireVoice.cs`，所有语音包共用一个闸门）
新增 `VoicePriority` 三档，`Admit()` 统一裁决：
| 档 | 覆盖事件 | 规则 |
|---|---|---|
| **Chatter** 碎嘴 | 单张/对子、要不起、压人一手 | 同一席至少隔 **3.5 秒**，且过了间隔再掷骰子（只说 **45%**） |
| **Normal** 场面话 | 三带/顺子/连对/飞机/四带二、轮到我、点提示 | 同一席至少隔 **1.2 秒**，**不打断**正在说的 |
| **Important** 要紧 | 炸弹/王炸、报单、胜负、抢地主/不抢、当地主、发牌 | **永远说**，且可以打断别人 |

另外：
- **同一句正在播 → 不重头再来**（防结巴）；刚说过同一句 2.5 秒内不重复。
- 收桌清缓存时把闸门状态一起清（不然新开一局的前几句会被莫名节流）。
- ★ **为「之后多生成几条」做好了准备**：`Say()` 现在会按前缀扫目录。
  往 `assets/voice/<角色>/` 里丢 `5_small_2.ogg`、`5_small_3.ogg`… **不用改一行代码**，
  自动扫到、随机轮着说、尽量不连着重样。

---

## ④ 篝火图标是「克隆」→ 打了一个自己的 pck，但**由我们自己挂载**

### 为什么必须 pck
`RestSiteOption.Icon` 只认 `res://`，本 mod 平时 `has_pck:false`，assets 里的散图进不了 res://。

### 关键安全设计（这是本次最重要的一条）
反编译确认（`ModManager.cs:970-979`）：manifest 写 `has_pck:true` 时，`LoadResourcePack` 返回 false
会 `throw InvalidOperationException` → 外层 catch 把**整个 mod 标成 Failed**（主菜单还弹错误条）。
即：pck 读不了，损失的是整个 mod，不是一张图标。

⇒ **不改 manifest（保持 `has_pck:false`），在代码里自己挂**：
`DouSpireRestSiteOption.TryMountIconPack()` → `Godot.ProjectSettings.LoadResourcePack(pck, false)`，
包在 try/catch 里，失败只是"图标还是本体的"，mod 照常。
挂上之后 `res://images/ui/rest_site/option_dou_spire.png` **真的存在**，
`ResolveIconPath()` 的 `ResourceLoader.Exists` 验证就能过 —— 也不再需要往缓存里塞贴图那个定时炸弹
（`PreloadManager.Cache.SetAsset` 那段已整个删掉）。

### 产物
- `pck-project/` —— 独立小 Godot 工程，**只含那一个 png**（故意不并进主工程：
  主工程 assets 里有 800+ 贴图和音乐，`all_resources` 会全打进去，包体爆炸 + 音乐侵权）。
  导出命令（Godot 4.5.1，和游戏同版本）：
  ```
  Godot_v4.5.1-stable_mono_win64.exe --headless --import --path pck-project
  Godot_v4.5.1-stable_mono_win64.exe --headless --export-pack "BasicExport" DouSpire.icon.pck --path pck-project
  ```
- `assets/DouSpire.icon.pck`（163,472 B）。放 `assets/` 是故意的：
  csproj 的 `CopyMod` 会把 `assets/**` 拷进游戏目录，工坊打包脚本也只带 `assets/` ⇒ 部署和上架都不用额外操心。
- pck 头部已验：`GDPC / pack_version=3 / godot 4.5.1`，与游戏加载的 `ModConfig.pck` 同代。

### 你怎么验证
进一局 → 走到篝火：选项应该显示**我们那张"牌桌+皇冠"**图标，不再是「克隆」。
日志应有 `斗地主：图标包挂载成功：…/mods/DouSpire/assets/DouSpire.icon.pck`。
万一图标没换，也只是退回本体图，不影响玩 —— 看日志那行是"成功"还是"失败"即可定位。

---

## 改动文件清单
| 文件 | 改了什么 |
|---|---|
| `DouSpireCode/DouSpireConfig.cs` | 注入不再静默；新增 `InstallLabels()`；挂语言切换；`OpponentVoiceScale` 默认 1.0 / 上限 2.0；补 `DOUSPIRE.mod_title` |
| `DouSpireCode/Entry.cs` | 初始化与 `EnsureEntryPoints()` 各调一次 `InstallLabels()` |
| `DouSpireCode/Table/DouSpireVoice.cs` | `VoicePriority` + `Admit()` 频率闸门；多文件随机；`Load`/`LoadAbs` 合一 |
| `DouSpireCode/Table/DouSpireRestSite.cs` | `TryMountIconPack()` 自己挂 pck；删掉 `SetAsset` 塞贴图的老路 |
| `assets/DouSpire.icon.pck` | 新增（163 KB，只含那一张图） |
| `pck-project/` | 新增（图标包源工程 + 导出预设） |
| `AppData/Roaming/SlayTheSpire2/mod_configs/DouSpire.cfg` | `OpponentVoiceScale` 0.55 → 1（你本机这份） |

## 待办 / 备注
- 「之后多生成几条语音」：直接往 `assets/voice/<角色>/` 丢 `5_small_2.ogg` 这类同名前缀文件即可，代码已支持。
- 工坊上架前老规矩：`python tools\music-switch.py public` 撤掉别人的歌。
- `DouSpire.icon.pck` 会被 `prepare_workshop.py` 一起打包（在 assets/ 里）✓，上架不用改脚本。
