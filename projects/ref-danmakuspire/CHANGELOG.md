# 更新日志 / Changelog

## v1.2.1

### 功能与改动

- 【共创弹幕】投稿功能：现在你可以在游戏里自己投稿弹幕了！在配置页选择「投稿弹幕」向弹幕库投稿自己创作的内容，既可以向已有规则投稿，也可自定义触发时机。每条投稿经作者人工审核后收录，随模组更新送到每位玩家————也许下一局你遇到的，就是另一位玩家几天前留下的弹幕。
- 【共创弹幕】捕获灵感：可以在游戏内点击“捕获场景”获取当前游戏状态并保存，存放于投稿页面“我的灵感”，可以用于快速复现场景用于投稿，便于保留投稿灵感。————看到弹幕的玩家和你有相似的遭遇。
- 【共创弹幕】弹幕颜色：投稿弹幕可选择特定颜色或彩色，彩色弹幕播放时呈现横跨整条弹幕、持续流动的彩虹渐变。
- 【共创弹幕】连续弹幕：一次投稿可包含最多 5 条独立正文，相邻弹幕停顿可选「快/ 中/ 慢」，按节奏连续播放。————“鸡煲是区”“前面的，鸡煲不是区”。
- 【共创弹幕】作者展示：使用鼠标指向弹幕，或使用暂停弹幕功能后点击弹幕，可以查看弹幕作者————也有机会看到自己创作的弹幕！
- 【共创弹幕】点赞弹幕：玩家可以给喜欢的弹幕点赞，点赞数据会收集到服务端。后续可能还会有其他功能奥！————比如······排行榜？
- 【聊天弹幕】多人游戏时可以通过对话框给队友发弹幕，可以添加emoji，游戏内使用回车键或点击聊天按钮使用。
- 【更多兼容】选择角色，卡牌，遗物，敌怪，事件投稿时，会自动扫描其他模组内容，可以对其他模组内容投稿，在玩家也安装了相同模组时有机会触发。
- 【新增规则】判定规则从 56 条扩展到 58 条，新增「卡组过小」判定与「领主阳伞」判定。
- 【意见反馈】设置页新增「意见反馈」入口，可向作者提交最多 1000 字的建议与反馈。
- 「重复抓牌」判定在已经获得卡牌后不再触发。
- 「跳过抓牌」判定使用动态概率，大幅降低出现频率。
- 模组配置页调整到设置-游戏设置中，不再占据主菜单位置。
- 可以在模组配置里选择关闭某些不希望出现的判定规则。

### 修复问题

- 修复女王死亡时火炬头随之死亡，仍误触发「误弑灯」判定的问题。
- 修复「建筑师」判定不触发弹幕的问题。
- 修复「完美格挡」判定没有考虑玩家「缓冲」状态的问题。
- 修复「全面神化」判定在错误时间触发问题。
- 修复多人游戏有玩家未添加模组无法进入游戏的问题。
- 修复模组在部分版本加载闪退，黑屏问题。
- 修复模组与部分角色模组一同使用闪退，黑屏问题。
- 修复部分情况下角色弹幕没有头像的问题。

---

### Features and changes

- Co-created danmaku submission: players can submit danmaku in-game from the settings page, targeting existing rules or custom triggers; each submission is reviewed by the author before release.
- Scene capture ("My Inspirations"): capture the current game state in-game and save it for quickly recreating a submission scene later.
- Danmaku colors: choose a specific color or a flowing rainbow gradient.
- Sequence danmaku: up to 5 independent texts with fast / medium / slow pause options.
- Author display: hover over a danmaku, or pause and click it, to see its author.
- Like danmaku: players can like danmaku and the data is collected on the server; more features may come later.
- Chat danmaku in multiplayer: send danmaku to teammates through the chat box with emoji support.
- More compatibility: when submitting for characters, cards, relics, monsters, or events, other mods' content is scanned automatically and can be targeted.
- New rules: the rule count is expanded from 56 to 58, adding "Small Deck" and "Lord's Umbrella".
- Feedback: a new "Feedback" entry in settings allows submitting suggestions up to 1000 characters.
- The "Duplicate Card" rule no longer triggers after the card is already owned.
- "Skip Card Reward" now uses dynamic probability, greatly reducing its frequency.
- The mod configuration page has been moved to Settings -> Game Settings instead of the main menu.
- Individual rules can now be disabled in the mod configuration.

### Fixes

- Fixed the "Ignoring the Torch" danmaku still triggering when the Torch Head dies alongside the Queen.
- Fixed the "Architect" rule not triggering danmaku.
- Fixed "Perfect Block" not taking the player's Buffer into account.
- Fixed "Full Apotheosis" triggering at the wrong time.
- Fixed multiplayer games being unable to start when a player does not have the mod installed.
- Fixed crashes/black screens on some game versions.
- Fixed crashes/black screens when used together with some character mods.
- Fixed character avatars missing from danmaku in some cases.

---

## v1.2.0（封闭测试中，未发布）

### 功能与改动

- 共创弹幕：玩家在游戏内投稿（昵称/文本/触发时机/前置条件），经作者人工审核后，以版本化快照下发到所有玩家本地离线播放。
- 内容与功能解耦：规则弹幕词条、角色/别名目录、可增删集合与共创词条从程序集抽离为可下载、可替换、可校验的 JSON 内容包；硬编码仅作回退。
- 弹幕密度三档（稀疏/标准/密集），统一缩放规则弹幕与共创弹幕。
- 对象型弹幕多人互通：共创弹幕按词条稳定 id 转发信号（不传文本），接收端本地词库渲染。
- 设置面板新增「社区共创」组（共创开关、投稿弹幕、检查共创更新）。

### Features and changes

- Co-created danmaku: players submit (nickname / text / trigger / preconditions) in-game; after author review, a versioned snapshot is distributed for offline playback on every client.
- Content/function decoupling: rule danmaku, character/alias catalogs, collectible sets and co-created entries are extracted into downloadable, replaceable, verifiable JSON packs; hardcoded data remains only as a fallback.
- Three-tier danmaku density (sparse / standard / dense) scaling both rule and co-created danmaku.
- Object danmaku multiplayer sharing: entries are forwarded by stable id (no raw text), rendered locally from each receiver's trusted library.
- New "Community Co-creation" settings group (toggle, submission panel, check for updates).

---

## v1.1.0

### 功能与改动

- 不再使用 BaseLib 作为前置，模组配置页面转移到游戏主界面。

### 修复问题

- 修复部分游戏版本出现闪退等问题。
- 修复部分游戏版本中弹幕会堆在左上角的问题。
- 修复手机端字体调大后会超出背景范围的问题。
- 修复亡灵契约师被奥斯提替代受伤时仍触发“完美防御”弹幕的问题。
- 修复火炬头死亡时或使用 AOE 攻击女王时仍触发“无视灯”弹幕的问题。
- 修复被仪式兽晕眩或受女王魂缚影响而无法打出防御牌时，仍触发“有防不出”弹幕的问题。

---

### Features and changes

- Removed BaseLib as a prerequisite and moved the mod configuration page to the game's main menu.

### Fixes

- Fixed crashes and related compatibility issues affecting some game versions.
- Fixed danmaku piling up in the top-left corner in some game versions.
- Fixed danmaku text extending beyond its background after increasing the font size on mobile.
- Fixed the “Perfect Defense” danmaku triggering when Osty took damage in place of the Necrobinder.
- Fixed the “Ignore the Torch” danmaku triggering after the Torch Head died or when an AOE attack hit the Queen.
- Fixed the “Block Available but Unused” danmaku triggering when defensive cards could not be played because of the Ceremonial Beast's stun or the Queen's binding effect.

## v1.0.0

首个公开版本。

- 不添加角色或卡牌，以纯事件监听方式覆盖战斗、奖励、商店、事件、火堆与 Run 生命周期。
- 内置 56 条行为规则和 332 条可追溯来源的弹幕词条。
- 使用游戏原版烟雾、字体和角色头像显示横向滚动或轨道内顶部弹幕。
- 提供弹幕开关、字号、透明度、背景、速度、显示区域、词条密度和顶部弹幕等配置。
- 支持多人行为弹幕：网络只传可信信号和引用，文本由接收端本地词条库渲染。
- 支持关闭、仅单人、仅多人、全部四档显示，以及独立的多人转发和接收开关。
- 多人握手协议版本实时跟随模组程序集版本，发布时无需维护独立版本常量。
- 支持保存退出、继续 Run、新 Run、胜负结束等生命周期状态清理与恢复。
- 首发版本的配置页依赖第三方配置框架；v1.1.0 已改为完全自有实现并移除该前置。

---

Initial public release.

- Pure event-listening design with no added characters or cards.
- 56 behavior rules and 332 traceable community danmaku entries.
- Scrolling and lane-pinned danmaku using vanilla smoke panels, fonts and character avatars.
- Configurable visibility mode, font size, opacity, background, speed, display area, density and top-pinned behavior.
- Multiplayer behavior danmaku sends trusted signals and references only; text is rendered from the receiver's local entry library.
- Four visibility modes plus independent multiplayer forwarding and receiving controls.
- The multiplayer handshake version is derived directly from the mod assembly version.
- Run lifecycle persistence and cleanup across save, resume, new run, victory and defeat flows.
- The initial release used a third-party configuration framework; v1.1.0 replaced it with a fully self-contained implementation.
