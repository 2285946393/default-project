# 凛冬尖塔 / FrostSpire —— 创意工坊说明文（草稿）

> 给 M4 上传工坊用。板块结构照 quest-spire 调研出的热门模组写法（§9.4）。
> ⚠️ 硬限制：说明文 ≤4000 字 / ≤8000 字节；封面必须 16:9 横图 <1MB。
> ⚠️ 上传纪律：**等用户发话才传**。

---

[h1] 凛冬尖塔 / FrostSpire

把《凛冬前线》的守塔玩法搬进杀戮尖塔2：**布防 → 敌潮一波波来 → 撑过全部波次**。卡牌还是卡牌。

[h2] 玩法

[list][*] 开局获得遗物「凛冬契约」：本局战斗变成敌潮战
[*] 打空场地不会结算——下一回合开始时先发间隙补给（官方三选一），然后下一波敌潮上岸
[*] 波次间隙打出「工事牌」：征召民兵（自动协战的单位）、冰垒（替你挨打的前排）、箭塔（每回合自动射击）……
[*] 左上角面板实时显示波次进度和下一波阵容
[*] 16 波：普通 / 精英 / 事件 / BOSS 混排，打完自动结算放行[/list]

[h2] 部署与容量

场上容量 **5 点**（致敬怪物火车2），每张工事牌占 1~3 点，超容量的牌打不出去——取舍感来自容量贵，不是位置少。

[h2] 前置

[list][*] STS2-RitsuLib[*] BaseLib[*] MinionLib（建筑单位系统）[/list]

[h2] 兼容性

[list][*] 单机优先；联机未适配（波次与部署在多人局不生效）[*] 与「尖塔任务」等逻辑 mod 无冲突[/list]

[h2] 已知问题

[list][*] 卡面为占位渲染（本家风格美术在画了）[*] 事件波为双选对话框 v1[/list]

[h2] English

Bring FrostBound's siege gameplay into Slay the Spire 2: deploy units & buildings, survive 16 waves of enemies, cards stay cards. Requires RitsuLib / BaseLib / MinionLib. Single-player focused.

[h2] 更新日志

[list][*] 0.1.0 —— 首个可玩版：16 波守塔 / 工事牌 / 传送门（凛冬裂隙）[/list]

[h2] 致谢

[list][*] STS2-RitsuLib / BaseLib / MinionLib 的作者们[*]《凛冬前线》——玩法灵感来源（本 mod 未使用其任何素材）[/list]
