# 斗地主尖塔 / DouSpire

在《杀戮尖塔 2》里开一桌斗地主。**一个独立窗口** —— 不跑尖塔、没有地图、没有怪物、没有房间。
从主菜单点「斗地主尖塔」进来（或按 F9），看到的就一张桌子，坐着你选的角色和两个电脑角色。

> 用户定的方向原话（推翻了我第一版的做法）：
> 「**你这个是吧斗地主嵌套进了游戏啊，感觉跟游戏没关系啊。正确来说是不应该出现怪物，
> 就一个桌子然后你选的角色，和ai选的角色出现，说到底只是在游戏里面开了个斗地主的窗口**」
>
> ⚠️ 第一版做成了「自定义模式的修正器 + 浮在尖塔战斗上的浮层」。
> 那是错的：自定义模式终究要开一局尖塔，就有地图/怪物/房间，等于"斗地主嵌在尖塔里"。
> 现在改成主菜单独立入口，**一个 run 都不会开**。

---

## 这东西现在是什么状态

| | 状态 |
|---|---|
| 规则内核（牌型判定 + 大小比较） | ✅ 与开源权威实现 rlcard **双向对车**过：970 万子集穷举 0 差异 |
| 一局流程（发牌/定地主/轮次/要不起/结算） | ✅ 30613 条断言 + 200 局自战 0 卡死 |
| 电脑对手 | ✅ rlcard 同款规则 AI，改良两处（不抢队友牌、允许王炸压炸弹） |
| mod 工程 / 编译 / 部署 | ✅ 一行命令编译并自动部署进游戏 `mods\DouSpire\` |
| 模式入口（主菜单按钮 + F9） | ✅ 复制游戏原生菜单按钮，外观/焦点导航和旁边一致 |
| 桌面角色立绘 | ✅ 取游戏本体 `CharacterModel.IconTexture` + 角色专属配色 |
| 选角色（三家各挑一个） | ✅ 进桌先选人，左右箭头拨；三家不撞脸 |
| 叫地主 / 抢地主（交互） | ⬜ 现在按手牌评分自动定地主 |
| 整屏牌桌场景（背景/圆桌/座位/剩余牌条/底牌/手牌/按钮） | ✅ 全部搭好，位置用相对坐标，换分辨率不跑位 |
| 素材分层（自己的图 → 游戏自带 → 纯色兜底） | ✅ 借了游戏 **46 张** UI 素材（路径全部校验过）；要画的清单见 `assets/素材清单.md` |
| 牌面换皮（尖塔风格卡图 / 花牌） | ⬜ 槽位已备好，往 `assets/` 丢图即生效（用户：UI 美化交给别人做） |

---

## 怎么玩到它

**方式一：主菜单按钮（正式入口）**

主菜单上多出一个「**斗地主尖塔**」，点它就开桌。它跟旁边那几个按钮同类同款，用键盘/手柄上下选也能选到。

**方式二：F9**

任何界面按 **F9** 开/收。`Node._Input` 是最早拿到输入的地方，桌面铺满屏幕时也收得到。

> ⚠️ 如果 mod 没生效：游戏有个「确认加载 mod」的弹窗，
> **没确认过的话游戏会跳过所有本地 mod**，日志里每个 mod 都会写
> `Skipping loading mod DouSpire, user has not yet seen the mods warning`。
> 在游戏里点一次确认就行（那个旗子就是 `settings.save` 里的 `mods_enabled`）。

---

## 编译 / 部署

```bash
# 从 dou-spire-rules 工程里跑（那里有沙箱编译脚本）
cd "projects/dou-spire-rules"

bash tools/build-mod.sh ../dou-spire                            # 本地开发版 DouSpireDev
bash tools/build-mod.sh ../dou-spire -p:ModName=DouSpire        # 工坊正式版 DouSpire
```

> ⚠️ **`ModName` 默认是 `DouSpireDev`（csproj 里写死的）** —— 不加 `-p:ModName=DouSpire`
> 编出来的就是本地开发版，dll/目录/mod id 全是 `DouSpireDev`，**不能拿去上架**。

沙箱里编译带 NuGet 依赖的 mod 需要补 Windows 系统变量 + 指定 `NUGET_PACKAGES`，
细节见技能 `dotnet-build-in-sandbox`。

编译产物会自动拷到 `<游戏目录>\mods\DouSpire\`（`CopyMod` 目标），
`assets/` 下的图也会一起拷过去（`CopyAssets` 目标，`.md` 除外 —— 那只是给人看的清单）。
**游戏开着的时候 dll 会被锁住导致 `MSB3231 Access denied` —— 先关游戏再编。**

---

## 工程结构

```
dou-spire/
├─ DouSpire.csproj            # Godot.NET.Sdk/4.5.1 · net9.0 · 链规则源码（见下）
├─ DouSpire.json              # mod manifest（id=DouSpire，依赖 RitsuLib + BaseLib）
├─ local.props                # 本机游戏路径（gitignore 掉的）
├─ assets/                    # ★ 美术放图的地方（编译时自动拷进 mods\DouSpire\assets\）
│  ├─ 素材清单.md              # ★ 要画哪些图 / 多大 / 现在不画长什么样（给美术的）
│  ├─ 生成提示词.md            # ★ 每张图的绘图 AI 提示词 + 比例 + 负面词
│  └─ layout.txt              # ★ 三家坐哪（改完 F9 就生效，不用重编）
└─ DouSpireCode/
   ├─ Entry.cs                # [ModInitializer]：注册程序集 / 生命周期钩子 / 每处都补装入口
   ├─ DouSpireConfig.cs       # 设置页（BaseLib ModConfigRegistry）
   └─ Table/
      ├─ DouSpireTable.cs       # ★ 那张桌（独立窗口 · 纯 C# 搭的 Godot 控制树）
      ├─ DouSpireLayout.cs      # ★ 三家摆位表（可改文件 · 立绘和 HUD 同一行说清）
      ├─ DouSpireAssets.cs      # ★ 素材加载层：每个槽位一条候选链（自己的图 → 游戏自带 → 纯色）
      ├─ DouSpireMenuButton.cs  # ★ 主菜单按钮（复制游戏原生菜单按钮）
      ├─ DouSpireCharacters.cs  # 角色花名册：立绘 / 名字 / 专属配色
      ├─ DouSpireHotkey.cs      # F9 开关桌 + 启动期补装主菜单按钮
      └─ DouSpireUi.cs          # 屏幕缩放 / 字体借用 / 取图取字体的统一入口 / 配色
```

### ⚠️ 规则内核是**源码链接**，不是复制

`DouSpire.csproj` 里这一段：

```xml
<Compile Include="..\dou-spire-rules\Cards.cs"      Link="Rules\Cards.cs"/>
<Compile Include="..\dou-spire-rules\Combo.cs"      Link="Rules\Combo.cs"/>
<Compile Include="..\dou-spire-rules\ComboJudge.cs" Link="Rules\ComboJudge.cs"/>
<Compile Include="..\dou-spire-rules\Game\GameState.cs"     Link="Rules\GameState.cs"/>
<Compile Include="..\dou-spire-rules\Game\MoveGenerator.cs" Link="Rules\MoveGenerator.cs"/>
<Compile Include="..\dou-spire-rules\Game\RuleAi.cs"        Link="Rules\RuleAi.cs"/>
```

**改一处，原型工程和 mod 同时生效** —— 永远不会出现"原型修好了、mod 里还是旧的"。
这些文件是纯 C#（`namespace DouSpireRules`，零依赖），原样编进 mod 的 dll。

两个必须遵守的约定：

1. **本工程必须开 `ImplicitUsings`** —— 那几个文件里没写 `using System.Linq;` 之类。
2. **不要链 `Program.cs` / `TextTable.cs` / `Simulation.cs` / `RlcardCrossCheck.cs`** ——
   那些是控制台专用，带 `Main` 和 `Console`，进 mod 会打架。

---

## 技术要点（踩过的坑都在这儿）

### 1. 入口是**主菜单按钮**，不是修正器

「往主菜单里加一个按钮」不用写 Harmony。RitsuLib 把主菜单里那个
`NMainMenu/MainMenuTextButtons` 节点整个换成自己的可滚动容器
（`STS2RitsuLib.Ui.MainMenu.NMainMenuScroller`，**internal**），但：

- **沿用了同一个节点名**：`nMainMenuScroller.Name = val.Name`
- 新容器依旧派生自 `Control`
- 换容器时会**把原容器的子节点全部搬过去**

所以不管换没换、我们比它先塞还是后塞，
`menu.GetNodeOrNull<Control>("MainMenuTextButtons")` 都拿得到，往里 `AddChild` 即可。
容器的 `ChildOrderChanged += RequestLayout`，加一个子节点就会触发重新排版，
`SynchronizeItems()` 收的是 `GetChildren().OfType<Control>()` —— 我们的按钮自动被排上。

### 2. 按钮要**复制**游戏自己的，不能 `new`

`MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NMainMenuTextButton`（public）：

```csharp
public override void _Ready()   => ConnectSignals();
protected override void ConnectSignals()
{
    base.ConnectSignals();
    label = GetChild<MegaLabel>(0);     // ← 关键
    ...
}
```

`new` 出来的对象没有子节点，`GetChild<MegaLabel>(0)` 直接抛。所以：

```csharp
var clone = (NMainMenuTextButton)template.Duplicate(
    (int)(Node.DuplicateFlags.UseInstantiation | Node.DuplicateFlags.Scripts));
clone.Released += _ => DouSpireTable.Toggle(Entry.Logger);   // 点击
container.AddChild(clone);                                   // _Ready 在这儿跑
clone.label.Text = "斗地主尖塔";                              // label 这时才非空
```

三个细节：

- **刻意不带 `Signals` flag**：默认的 `Duplicate()` 会连信号连接一起复制，
  可能把模板按钮原本接的动作（"继续游戏"…）也抄过来。`Groups` 同理。
- **直接改 `label.Text`，不调 `SetLocalization`**：那个要游戏本地化表里的 key
  （`new LocString("main_menu_ui", key)`），我们没有也不该去加。
  复制体的 `_locString` 是 null（C# 字段不受 `Duplicate()` 影响），
  所以不会有 `RefreshLabel()` 来覆盖我们。
- **字体白送**：`Duplicate()` 会复制 theme override，而模板那个 label 早就做过
  中文的 `ApplyLocaleFontSubstitution` —— 复制体天然带正确的中文字体。

顺带：因为复制体就是 `NMainMenuTextButton`，它会被排进 scroller 的 `_buttons`，
**键盘/手柄焦点导航和两侧光标动画全都免费拿到**。

### 3. 界面全部用 C# 搭，所以不需要 pck

照 quest-spire 的套路：`sealed partial class DouSpireTable : Control`，
`SceneTree.Root.AddChild(...)`，控制树全部 `new` 出来。
没有 `.tscn`、没有贴图 → `has_pck: false`，manifest 也简单。

**必须借游戏的字体**：Godot 默认字体不含中文，直接 `new Label { Text = "斗地主" }`
会显示成豆腐块。`DouSpireUi.GameFont()` 从场景里已有的 `Label` 上"借"它正在用的字体；
借不到就退回 `SystemFont`（微软雅黑/黑体）。

### 4. 角色立绘取自游戏本体

`DouSpireCharacters` 走 `ModelDb.AllCharacters.Where(c => c.IsPlayable)` 拿花名册，
每个角色要三样东西：

| 要什么 | 从哪拿 |
|---|---|
| 名字 | `c.Title.GetFormattedText()`（游戏自己的本地化） |
| 配色 | `c.NameColor`（每个角色专属） |
| 立绘 | `c.IconTexture` = `ui/top_panel/character_icon_<entry>.png` |

`Icon()` 全程 try/catch 并缓存：**拿不到图只是不好看，不该把牌桌搞崩**。

> 更"坐着"的姿势在 `rest_site/characters/<entry>_rest_site` 场景里，
> 但那是整套动画场景，第一版先用静态头像，稳。

### 5. 热键用 `_Input` 而不是 `_GuiInput`

`Node._Input` 是**最先**拿到输入的地方（早于 GUI 的 `_GuiInput`），
所以哪怕桌面铺满屏幕把游戏 UI 全挡住，F9 也一定收得到。

`Open()` 早期可能拿不到场景树（`Initialize` 跑得很早），所以在
`GameReadyEvent` / `MainMenuReadyEvent` / `RunLoadedEvent` 上都补装一次
（`DouSpireHotkey.Install()` / `DouSpireMenuButton.Install()` 都是幂等的）。
`DouSpireHotkey._Process` 里还留了个兜底：前 30 秒每 2 秒重试装一次菜单按钮 ——
这几个生命周期事件的到达时机没法在无头模式实测，多试几次很便宜。

### 6. 「绝不让 mod 把游戏搞崩」

桌面搭建、渲染、AI 出牌各自 try/catch，出错只记日志 + 设 `_broken` 停摆
（不每帧重试，否则日志刷爆、帧数掉）。异常绝不抛进 Godot 的信号派发里。

### 7. 素材分三层：自己的图 → 游戏自带 → 纯色兜底

`DouSpireAssets` 里每个槽位是**一条候选链**（`string[]`），靠前的优先：

```
"table/crown.png"                          ← ① 我们 assets/ 里的图（随时换，不用重编）
"res://images/ui/combat/energy_star.png"   ← ② 游戏本体的贴图（pck 里现成的）
（链走完都没有）                             ← ③ 返回 null，界面退回纯色块 / Unicode 字符
```

两条加载路径，**不能混**：

| 路径 | 用什么读 | 为什么 |
|---|---|---|
| `res://…`（游戏自带） | `ResourceLoader.Load`（先 `Exists` 再 `Load`，免得刷错误日志） | 游戏自己的 pck，直接可用 |
| 其它（我们的图） | `Image.LoadFromFile(绝对路径)` → `ImageTexture.CreateFromImage` | 本 mod `has_pck: false`，**读不到 `res://<ModId>/`**，只能按文件系统路径读 |

`ResolveRoot()` 从 **dll 自己的位置**推出 `assets/` 目录（最稳，不管 exe 在哪），
兜底再试 `<游戏目录>/mods/DouSpire/assets`。

几个要点：

- **开桌时重清缓存**（`ClearCache()`）：美术改完图，F9 收桌再开就能看到效果，不用重启游戏。
- **开桌时打一条素材清单日志**：写明每个槽位最终用了哪张图、哪些在走纯色兜底 ——
  美术靠这条确认自己的图有没有被吃进去。
- **游戏里没有的东西**（皇冠 / 花色 / 大小王 / 圆桌）已翻遍 pck 确认，只能画。
- `res://` 路径**全部从 1.99GB 的 `SlayTheSpire2.pck` 里扫出来并逐条校验过**，
  写错一条就会静默退到下一候选，所以别凭记忆改路径。
- NinePatch：`StyleBoxTexture` 的边距按 `Scaled(12)` 算，四角别小于 40px（2 倍图）。

具体要画什么、多大、放哪，全在 **`assets/素材清单.md`**。

---

## 已知的没做 / 待办

- **叫地主**：现在按手牌评分（rlcard 同款：炸弹 +6、王炸 +8、A/2/王加分）自动定，没做交互。
- **牌面换皮**：数字版。尖塔风格卡图的方案在 `notes/斗地主尖塔-牌面与界面方案.md`。
- **胜负与尖塔流程挂钩**：因为现在是独立窗口、不跑 run，这条**大概率取消**。
- **联机**：明确不做（用户拍板放弃）。
- **AI 强度**：规则型，不是 DouZero。想要更聪明得另接。

---

## 无头模式不能用来验证 mod

`SlayTheSpire2.exe --headless --log-file <path>` 能跑起来、能写日志，
但**所有 mod 都会被跳过**（实测 85/85 全 skip）：

```
[INFO] Skipping loading mod DouSpire, user has not yet seen the mods warning
```

这条日志来自 `MegaCrit.Sts2.Core.Modding.ModManager`：

```csharp
public static bool PlayerAgreedToModLoading => _settings?.PlayerAgreedToModLoading ?? false;
...
else if (!PlayerAgreedToModLoading) {
    Log.Info($"Skipping loading mod {modId}, user has not yet seen the mods warning");
    mod.state = ModLoadState.Disabled;
}
```

而那个字段的 JSON 名字**极具误导性**（`ModSettings.cs`）：

```csharp
[JsonPropertyName("mods_enabled")]
public bool PlayerAgreedToModLoading { get; set; }
```

也就是说：**`settings.save` 里的 `mods_enabled` 就是"用户已确认加载 mod"这个旗子**，
不是"mod 功能总开关"。

**为什么无头会 skip**：无头跑的时候游戏落在 `default/1` 这个 profile 上
（日志后面才 `Profile-scoped data path initialized: user://steam/<id>/profile3`），
而 `default/1/settings.save` 里 `mod_settings` 是 **null** → 旗子读出来是 false → 全 skip。

**真实 profile 是正常的**：`user://steam/76561199532681033/settings.save` 里
`mods_enabled: true` + 87 条 `mod_list`。而且能确定它被真正写过——
`ModManager` 只在旗子为 true 时才重建 `ModList`，旗子为 false 时是
`_settings.ModList = new List<...>()`（空表不落盘）。文件里有 87 条 → 旗子当时就是 true。

**所以**：`--headless` 只能验证「游戏起不起得来」，**验证不了「我们的 mod 加载了没」**。
要验证只能有界面的会话里进主菜单看按钮在不在。

