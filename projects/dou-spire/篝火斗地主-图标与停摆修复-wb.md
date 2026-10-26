# 篝火斗地主：图标变样 / 点进去卡死 —— 两个真 bug + 语音调查（wb）

> 2026-10-08 晚。**产物 md5 `c9cf0c98423e5a065c77935b45067b26`，310,272 B，15:27，本地 == 部署。**
> 编译 **0 警告 0 错误**。**需要重启游戏**才生效（旧 dll 还在内存里）。
>
> 用户原话：「然后我设置随便动了几下，斗地主就变成别的图例和名字了，进去也是卡死。」

---

## 一、结论先说

用户报的是**两个各自独立的 bug**，都出在我们自己身上，都在这一轮修掉了：

| 症状 | 真因 | 状态 |
|---|---|---|
| 篝火里「斗地主」变成别的**图例和名字** | 我们声明的图标 `res://` 路径**在文件系统里根本不存在** | ✅ 已修 |
| 点进去**卡死** | ①同一个假路径让按钮的 `_Ready` 半途抛异常 ②**另有一个**：贴图被引擎回收后直接赋值 | ✅ 都已修 |

一句话：**用户没有把设置改坏。是我们有一颗定时炸弹，恰好被他装的 `RestSiteExpansion` 点着了。**

---

## 二、Bug 1：图标假路径 → 图标错、名字错、按钮坏

### 证据链（`godot.log` 第 4255–4320 行，一次完整复现）

用户进休息处，`RestSiteExpansion` 重建选项列表：

```
4256  [DouSpire] 斗地主：篝火里挂上了「斗地主」选项（玩家 1，身上金币 99）。
4257  [RestSiteExpansion] 休息处选项构建：… 构建前选项数=3
4277  [RestSiteExpansion] 已添加选项（构建后选项数=22）：HEAL/SMITH/DOU_SPIRE/…
4282  Preloading 'RestSite Room' assets... count=21 vfx=2        ← ★ 游戏开始预加载每个选项的图
4283  [WARN] Threaded load status Failed for res://images/ui/rest_site/option_dou_spire.png,
             falling back to sync load
4284  ERROR: Error loading resource: 'res://images/ui/rest_site/option_dou_spire.png'.
4294  [ERROR] Failed to load resource synchronously: res://images/ui/rest_site/option_dou_spire.png
4304  [WARN] [RitsuLib] [Assets] Missing resource path for DouSpireRestSiteOption.CustomIconPath:
             '…/option_dou_spire.png'. Falling back to the base asset.     ← 退回本体图标/标题
4305  ERROR: MegaCrit.Sts2.Core.Assets.AssetLoadException:
             Asset previously failed to load: res://images/ui/rest_site/option_dou_spire.png.
             The game installation may be corrupted.
4306    at AssetCache.LoadAsset_Patch1(AssetCache this, String path)
4308    at RestSiteOption.get_Icon_Patch1(RestSiteOption this)
4309    at NRestSiteButton.Reload()
4310    at NRestSiteButton._Ready()
```

**一条假路径，连环三个症状：**

1. 游戏异步加载这张图 → **文件不存在** → `Error loading resource`；
2. `AssetCache` 把这条路径**永久标记成"加载失败"** → 之后无论谁问都直接抛
   `AssetLoadException: Asset previously failed to load … may be corrupted`；
3. `RestSiteOption.get_Icon` 抛异常 → `NRestSiteButton._Ready()` **半途中断**
   ⇒ 那个按钮停在半初始化状态（**点它没反应 = 用户说的"卡死"**）；
4. RitsuLib 兜底 → 标题和图标全换成本体的 **「挖掘 / Dig」**（= **"变成别的图例和名字"**）。

### 为什么会这样（我们的锅）

本 mod 是 **`has_pck: false`** —— 没有 pck，就**没有任何东西被挂进 `res://`**。
但我们把 `IconResPath = "res://images/ui/rest_site/option_dou_spire.png"` 当成了真路径，
靠 `PreloadManager.Cache.SetAsset(path, 贴图)` 往运行时缓存里"塞"一张图顶替。

- 平时看着是好的（我们从没触发过那条异步预加载）；
- **只要有一次异步预加载走到这个路径，就炸** —— 而 `RestSiteExpansion` 恰好会重建选项，
  于是走了 `Preloading 'RestSite Room' assets`。
- ⇒ **不是用户改设置改坏的，是"改设置"这个动作让他重新进了休息处**，把炸弹点着了。

### 改法（`DouSpireCode/Table/DouSpireRestSite.cs`）

```csharp
public const string OwnIconResPath      = "res://images/ui/rest_site/option_dou_spire.png";
private const string FallbackIconResPath = "res://images/ui/rest_site/option_clone.png"; // 本体真有的图

/// 先问 ResourceLoader.Exists：我们那张图真的存在（以后打了 pck）才用；
/// 验不过就退回本体图标 —— 宁可图标朴素，也绝不让选项坏掉。
public static string ResolveIconPath()
{
    if (_resolvedIconPath is not null) return _resolvedIconPath;
    try { if (Godot.ResourceLoader.Exists(OwnIconResPath)) { _resolvedIconPath = OwnIconResPath; return _resolvedIconPath; } }
    catch (Exception e) { Entry.Logger.Warn(…); }
    Entry.Logger.Warn("…自己在 res:// 里的那张篝火图标不存在（本 mod 没有 pck）—— 改用本体图标顶一下。");
    _resolvedIconPath = FallbackIconResPath;
    return _resolvedIconPath;
}
public static bool IsOwnIcon(string path) => path == OwnIconResPath;
public override RestSiteOptionAssetProfile AssetProfile => new(ResolveIconPath());
public override string? CustomIconPath => ResolveIconPath();
```

`EnsureIcon()` 同步收紧：**只在"真的是我们自己那张图"时才 `SetAsset`**，
退回本体图标时**什么都不做** —— 绝不能拿我们的贴图去顶一个本体路径
（那会把本体、甚至别的 mod 用的同一张图偷偷换掉）。

> ⚠️ 坑中坑：文件里 `using MegaCrit.Sts2.Core.Assets;` 会让 Godot 的 `ResourceLoader` 被遮蔽，
> 必须写全 `Godot.ResourceLoader.Exists(...)`（否则 CS0103）。

### 代价（要如实告诉用户）

**篝火里那个图标现在是本体的「复制 / Clone」那张图，不是我们自己的斗地主图。**
想用自己的图，正解是**给 DouSpire 打一个 pck**（`project.godot` / `workshop/` 都在，
但还没有 export presets）。这是独立的一步，没在本轮做。

---

## 三、Bug 2：贴图被引擎回收 → 整张桌子"已停摆"

### 证据链（`godot.log` 第 3685 行）

```
3684  [DouSpire] 斗地主：座位立绘 = KAFKA-KAFKA_CHARACTER|SILENT|sherry
3685  [ERROR] [DouSpire] DouSpire 桌面渲染出错，已停摆:
        System.ObjectDisposedException: Cannot access a disposed object.
        Object name: 'Godot.CompressedTexture2D'.
          at Godot.GodotObject.GetPtr(GodotObject instance)
          at Godot.TextureRect.SetTexture(Texture2D texture)
          at DouSpire.Table.DouSpireTable.FillHud(Int32 seat)
          at DouSpire.Table.DouSpireTable.RefreshCore()
          at DouSpire.Table.DouSpireTable.Refresh()
          at DouSpire.Table.DouSpireTable.BeginShowCards()
          at DouSpire.Table.DouSpireTable.Deal(Nullable`1 seed)
          at DouSpire.Table.DouSpireTable.<RestoreOrPick>b__197_0()
```

**这是"卡死"的第二个、也是更硬的那个原因**：`DouSpireTable.Refresh()` 顶层有
`catch → _broken = true → "已停摆"`（刻意不让异常抛进 Godot 信号派发，否则会崩游戏）。
代价是**一旦出错，整张桌子就永久不再刷新** —— 表现就是"点进去卡死"。

### 真因

`DouSpireCharacters.Icon(c)` 把 `c.IconTexture` 塞进一个 **static 字典缓存**，永不过期。
而本体角色的立绘来自（反编译源码 `MegaCrit.Sts2.Core.Models/CharacterModel.cs:121`）：

```csharp
public Texture2D IconTexture => PreloadManager.Cache.GetTexture2D(IconTexturePath);
```

—— 那是**游戏自己的 AssetCache**。而**换房间/换场景时游戏会回收**它：

```
4278  [INFO] Unloading 3 missed cache assets
4279  [WARN] Asset not cached: res://images/powers/curl_up_power.png
4280  [WARN] Asset not cached: Kafka\images\powers\big\shock_power.png
```

回收之后，我们静态缓存里攥着的那张 `CompressedTexture2D` 就变成**已释放对象**了。
下一次开桌 `FillHud` 往 `TextureRect.Texture` 一赋 → `ObjectDisposedException` → 停摆。

**阴在这**：同一场里前**六**次开桌（`godot.log` 2831 / 2883 / 2919 / 2953 / 3000 / 3670）
全都正常，第七次才炸。所以这条和"改设置"没有因果关系，是**攒够了一次房间切换**才现形。

### 改法

**根因（`DouSpireCode/Table/DouSpireCharacters.cs`）** —— 只缓存"确实还活着"的图：

```csharp
/// 本体立绘来自游戏 AssetCache，换房间会被回收成"已释放对象"；判不了就按"没有图"处理。
public static bool IsAliveTexture(GodotObject? t)
{
    if (t is null) return false;
    try { return Godot.GodotObject.IsInstanceValid(t); }
    catch (System.Exception) { return false; }
}

public static Texture2D? Icon(CharacterModel? c)
{
    …
    if (IconCache.TryGetValue(key, out var cached))
    {
        if (IsAliveTexture(cached)) return cached;
        IconCache.Remove(key);          // 引擎把这张收了 —— 别再用
    }
    Texture2D? tex;
    try { tex = c.IconTexture; }
    catch (System.Exception e) { Entry.Logger.Warn(…); return null; }
    if (!IsAliveTexture(tex)) return null;   // 死的这帧不给；**刻意不缓存** ⇒ 下次自己恢复
    IconCache[key] = tex;
    return tex;
}
```

**兜底（`DouSpireCode/Table/DouSpireTable.cs`）** —— 两个赋值点都加了守卫，
**失效就退回无图，绝不放异常出去掀桌子**：

- `FillHud(int seat)`（第 3331 行附近，HUD 头像 `_hudAvatar[seat]`）
- `RefreshPicker()`（第 2867 行附近，选人界面 `_pickPortrait[seat]`）

### 影响面核对（确认只有这一处是游戏贴图）

| 贴图来源 | 方式 | 会被引擎回收吗 |
|---|---|---|
| `CharacterModel.IconTexture` | `PreloadManager.Cache.GetTexture2D` | **会** ← 本次修的就是它 |
| 座位立绘 / 牌面 / 猜拳手势 | `Image.LoadFromFile` + `ImageTexture.CreateFromImage`（`DouSpireAssets.cs:753/755`） | 不会（我们自己持有） |

⇒ 修一处即可，不会漏。

---

## 四、顺带修掉：日志自检把 `SpineSprite` 刷没了

`DouSpireActor.CollectSpineInfo` 原写法是"深度 ≤1 的全部列出来"，而 `Visuals` 底下挂着
**≈200 个 `SpineMesh2D`** —— 整行日志被刷到几万字符，**真正想看的那行 `SpineSprite` 反而被截断**
（`godot.log:3675/3680` 实测）。现在改成：深度 0 报 `名字(类型,子N)`，更深只收骨骼节点，
并**在循环里**卡上限（旧代码只在函数入口卡，一个 200 次的 for 直接冲过去）。

---

## 五、用户问的三件事，逐条回答

### 1️⃣ 「设置里面很多英文我看不懂」

**根因是我们的**：`DouSpireConfig.SetupConfigUI()` 里有一套"往 `settings_ui` 本地化表注入标题"的机制
（`InjectLabels()`，必须**在 `base.SetupConfigUI()` 之前**调），
但**上一轮我新加的三个设置忘了登记**，于是设置页直接显示属性名。

已补上（`DouSpireCode/DouSpireConfig.cs`）：

| 设置（中文名） | 键 | 默认值 |
|---|---|---|
| 一份底注 = 金币 ÷ N（越大越温和） | `DOUSPIRE-BET_UNIT_DIVISOR.title` | 20 |
| 一局最多输赢（占身上金币的%） | `DOUSPIRE-BET_MAX_SWING_PERCENT.title` | 25 |
| 其他 mod 的角色改用坐姿（备选，一般不用动） | `DOUSPIRE-MOD_CHARS_PREFER_SITTING.title` | False |

> 📌 规矩记在这儿：**以后加设置项，必须同时加 `InjectLabels()` 里那条键**，否则用户看到的是天书。

### 2️⃣ 「主界面有星见雅和玄仪的语音，到底有多少条」

- 用户听到的**不是我们牌桌的声音** —— 那些来自**选人界面**（`NCharacterSelectScreen`），
  是 mod `Miyabists2`（工坊 `3747607263`，作者 月下樱，v0.4.4）自己放的。
- **实测数字：`Miyabists2.pck` 里 88 个音频文件 / 44 条不同语音**（重复是因为多场景复用）。
  分组：`yixuan_*` 19 条、星见雅侧 `guang_*` 6 / `dan_*` 5 / `card_*` 3 / `select_*` 2 / `begin_*` 2 等、
  通用 `hurted_*` 2 + `pp_*` + `nuomu_*`。
  全部在 `res://scenes/audio/*.mp3`。
- ⚠️ 但**没有任何 FMOD 事件**（`grep 'event:/'` 在 pck 和 dll 里都是 0 命中）——
  它们的语音是**自己的 DLL / 场景用 `AudioStreamPlayer` 内部播的**，没有对外接口。

### 3️⃣ 「诸如此类的 mod 角色能不能直接用他们的语音」

**能，但不是走他们的接口 —— 是直接读他们 pck 里的 mp3。** 理由：

- 他们 `has_pck: true` ⇒ 那些 mp3 **在运行时确实存在于 `res://`**
  ⇒ 我们可以自己 `ResourceLoader.Load<AudioStream>("res://scenes/audio/xxx.mp3")` 拿进来播。
- 我们**已经有**一套现成的每角色语音系统：`mods/DouSpire/assets/voice/<角色>/<NN>_<事件>_n.ogg`，
  现在有 **4 个角色包（chun / sherry / whale / yuki），每个 16 条**，
  事件槽正好 16 个：`1_deal 2_landlord 3_myturn 4_hint 5_small 6_triple 7_straight 8_big 9_bomb
  10_pass 11_beat 12_lastone 13_win 14_lose 15_bid 16_nobid`。
- 所以正解是给 `DouSpireVoice` 加一个**"从 `res://` 取"的分支** + 一张
  「角色 → 事件 → 他们那个 mp3 文件名」的对照表。
- ★ 而且**星见雅和仪玄已经在我们牌桌上出现了**（实测 `座位立绘 = KAFKA|sherry|MIYABISTS2_CHARACTER_YIXUAN`、
  `KAFKA|whale|MIYABISTS2_CHARACTER_MIYABI`）—— 就是**没声音**。补齐对照表就能让她们开口。

---

## 六、卡芙卡（战士立绘）

**这轮没动代码，但证据更硬了。** `godot.log:3674-3679`：

```
3674  [INFO] [Kafka] Visuals are NCreatureVisuals, returning directly
3675  角色自检 —— 模型=Kafka.KafkaCode.Character.KafkaCharacter
                  场景=res://scenes/creature_visuals/ironclad.tscn     ← ★ 借的是战士的场景
3677  骨骼自检 —— 包围盒=(-532.0001, -1113), (878.31116, 1183.3269)
                  动画表=attack, attack_heavy, cast, die, hurt, idle_loop, low_health_loop, relaxed_loop
3678  已按 NCreature 的做法调过 GenerateAnimator（返回 CreatureAnimator）—— 第三方 mod 的换装补丁就挂这一步
```

- 卡芙卡**借用** `creature_visuals/ironclad.tscn`（她的 pck 里 `creature_visuals` 出现 **0** 次）；
- 我们的补偿调用**确实跑了**（返回了 `CreatureAnimator`），但外观仍是战士
  ⇒ 那个 mod 的换装补丁挂在 `NCreature._Ready` / `NRestSiteCharacter._Ready` 里，
  **我们这条"自己搭牌桌"的路根本走不到那儿**。
- 对照：本体角色全对（`Regent → res://scenes/creature_visuals/regent.tscn`），
  印证用户的观察「其他的角色倒是正确引用了」。
- **唯一还没试的杠杆**：`ModCharsPreferSitting` = 让她改走"休息处坐着"那套
  （该 mod 确实补了 `NRestSiteCharacter._Ready`，所以这条路**有可能**被命中）。
  → 按用户自己说的「如果是因为这个角色很特殊那就算了吧」，
  **建议就试这一次，不行就收手。**

---

## 七、怎么验证（交给用户）

1. **完全退出游戏再重开**（旧 dll 还在内存里，不重开看不到效果）。
2. 进休息处（篝火）→ 看「斗地主」选项：
   - ✅ 名字还是**「斗地主」**；
   - ✅ 图标是**本体那张**（不是我们画的，本轮代价，见 §二）；
   - ✅ **能点开**，不再卡死。
3. 反复开桌 **7～8 次**（Bug 2 是攒够房间切换才现形）→ 桌面应当**始终正常刷新**。
4. 想试卡芙卡坐姿：设置里把「其他 mod 的角色改用坐姿（备选，一般不用动）」打开 → 重开一局看她。
5. 日志确认（可选）：
   ```bash
   grep -a "自己的那张篝火图标不存在\|HUD 头像贴图已失效\|选人头像贴图已失效" \
        "$AppData/SlayTheSpire2/logs/godot.log"
   ```
   出现第一条是**预期的**（说明退回本体图标生效了，不再有假路径）；
   出现后两条说明守卫**接住了**一次贴图回收（桌子没停摆）—— 也是好事。

---

## 八、没改的 / 边界（如实交代）

- ⚠️ **篝火图标用的是本体图，不是我们的斗地主图。** 想要自己的图必须给 DouSpire 打 pck。
- ⚠️ **Bug 2 的守卫是"止损"**：贴图被回收后那一帧会**没有头像**，下一次刷新会自己恢复。
  更好的做法是每次用之前重取（本轮已让 `Icon()` 不缓存死图，等于已做到）。
- ⚠️ 本轮**没有**验证过多人（联机）场景下的这两条路径。
- ⚠️ `projects/dou-spire/` 仍是 **未跟踪（`??`）** ⇒ 这些改动**没有 git commit**。
- 非我们的报错（只是记一笔，不用管）：`OrchisNecrobinderSkinMod.NRelicReadyPatch`
  每局抛 4 次 `InvalidOperationException: Model was accessed before it was set.`。
- `FrostSpire` 有**同款隐患**（`res://FrostSpire/images/card_art/*.png` 也不存在，
  日志里几百条 `Missing resource path for …CustomPortraitPath`）——
  但 RitsuLib 对卡图是**先查存在再退回**的，只刷警告不炸；**没在 `res://` 里真加载**，
  所以不会像篝火图标那样把缓存毒掉。**优先级低，但值得排期统一修。**

---

## 九、改动文件清单

| 文件 | 改了什么 |
|---|---|
| `DouSpireCode/Table/DouSpireRestSite.cs` | `ResolveIconPath()` 先验后给 + `IsOwnIcon()` + `EnsureIcon()` 收紧 |
| `DouSpireCode/Table/DouSpireCharacters.cs` | 新增 `IsAliveTexture()`；`Icon()` 只缓存活图、死图丢掉重取 |
| `DouSpireCode/Table/DouSpireTable.cs` | `FillHud()` / `RefreshPicker()` 两处贴图赋值加守卫 |
| `DouSpireCode/Table/DouSpireActor.cs` | `CollectSpineInfo()` 只报直接子节点 + 循环内卡上限 |
| `DouSpireCode/DouSpireConfig.cs` | 补三个新设置的中文/英文标题键 |
