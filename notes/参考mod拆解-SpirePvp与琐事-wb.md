# 参考 mod 拆解 —— SpirePvp（尖塔决斗）与 STS2_Things（尖塔：琐事）

> 2026-10-09。国际台拉回来两个新参考 mod，反编译源码在
> `projects/ref-spirepvp/`（129K）和 `projects/ref-things/`（5.6M，含 3 个 embedded dll）。
> 本文是通读后的拆解结论。**参考代码无 LICENSE，只读对答案，不抄代码。**

---

## 一、SpirePvp（尖塔决斗）v0.2.5 —— 对我们**最有价值**

**它是什么**：双人构筑 PvP（10/15/20 张三选一或现开套牌），走游戏自带联机。
反编译 3933 行、55 个 Harmony 补丁。要求 BaseLib ≥ 3.4.7、游戏 v0.111.0。

### 对斗地主四人联机（Phase C）能直接抄思路的三件事

1. **自己造大厅入口**：`HostMenuPatch` 给 `NMultiplayerHostSubmenu` 加一个
   「PvP · 构筑决斗」按钮（照着 `CustomRunButton` 的位置摆）。不用注册什么"模式"，
   一个按钮 + 一个启动函数就完事。
2. **完全复用游戏自己的网络栈**（这是最大的发现）：
   ```csharp
   var service = new NetHostGameService(PeerVersionInfo.LocalDefault());
   await service.StartSteamHost(2);            // Steam P2P 开房，2 = 最大人数
   // 或 service.StartENetHost(33771, 2);      // 无 Steam 时的 ENet 兜底
   NCustomRunScreen submenuType = stack.GetSubmenuType<NCustomRunScreen>();
   submenuType.InitializeMultiplayerAsHost(service, 2);
   submenuType.Lobby.SetModifiers(...);        // 把 mod 设置塞进大厅
   stack.Push(submenuType);                    // 推进大厅子菜单
   ```
   `NetHostGameService` / `PeerVersionInfo` / `NetErrorInfo` / `NErrorPopup` /
   `NCustomRunScreen.InitializeMultiplayerAsHost` 全是**游戏本体的类**。
   我们 DouSpireNet 现在自己管传输；四人联机如果要用 Steam 好友邀请/大厅，
   这条路是现成的（`StartSteamHost(4)` 就能开 4 人房）。
3. **设置进大厅**：`Lobby.SetModifiers(PvpLobbyModifiers.Create(...))` ——
   房主的设置随大厅同步，客机不用单独协商。我们 2v2 的"人数开关"将来进联机，
   就该走这种"大厅带配置"的路子，而不是各端各读各的 cfg。

### 其他可借鉴
- **PvpDuel : ModifierModel + [SavedProperty]** —— 又一个用 `ModifierModel` 做模式配置的实锤
  （RitsuLib 那套我们已经在用，这里确认是官方推荐姿势：设置随存档/大厅走）。
- `DuelMap : ActMap` —— 自定义地图节点直接继承游戏基类；决斗入口就是地图上一个节点。
- **防手抖/防卡死做得很足**：`InputGatePatch`、`IsAboutToBeginGame` postfix 锁按钮、
  各种 CleanupPatch 兜底 —— 和我们"绝不让 mod 卡死牌局"是同一个思路。
- 自绘对战 UI（双方镜像站位 `DuelPlacement(X, Y, Mirror)`）跟我们牌桌布局一个套路。

### 风险
- 补丁面很广（lobby/map/combat 55 处），跟别的 mod 同装时冲突概率不低；
  好在都有模式开关，平时不进 PvP 就不动。

## 二、STS2_Things（尖塔：琐事）v1.25.10 —— 最值得学的是**架构**

**它是什么**：Adnermo 的大型内容拓展（新 Boss：洞窟之神/穴居者/碗虫始祖…、
新卡、新能力、新试炼）。反编译 v111 实现 43486 行、303 个类。

### 架构绝招：一个工坊包同时兼容多个游戏版本（直接解决我们的老痛点）

三个 dll 的分工：
- `STS2_Things.dll` = **引导壳**（Bootstrap，只有 416 行）：
  1. 反射探测游戏版本：数 `AbstractModel.ModifyDamageMultiplicative` 的参数个数
     （5 个 = v107.1；6 个 + 存在 `HandshakeManager` 类型 = v111）；
  2. 从**程序集内嵌资源**里 `AssemblyLoadContext.LoadFromStream` 加载对应版本的实现 dll
     （`embedded-impl-*.dll` 就是内嵌实现被抠出来的样子）；
  3. v111 用游戏官方的 `ModManager.AssociateAssemblyWithMod(modId, asm)` 登记实现程序集；
     v107.1（旧版没有这个 API）就 Harmony 补 `ReflectionHelper.ModTypes` 把实现的类型**追加**进注册表。
- `STS2_Things.BaseLibBridge.dll` = BaseLib 桥（走 `godotsharp_game_main_init` 入口挂钩）。

**对我们的意义**：每次游戏更新我们都要重编重传（min_game_version / 0.6.6 钉版本的坑）。
"引导壳 + 按版本内嵌多份实现"是已被验证的解法——一次上传，新旧版本都能玩。
代价是包体翻倍、要维护两套实现。**建议等下次游戏大版本更新真把我们弄坏时再上这套**，
现在记住路子就行（细节都在 `decompiled/STS2_Things.decompiled.cs` 255-370 行）。

### 其他
- 303 个类里一大半是新 Boss/卡/Power（内容向），跟我们的桌子没有直接交集；
- 但它也有一组 `Config*Session*Patch`（联机大厅里的配置会话），说明内容 mod 也得处理联机下的配置同步。

## 三、跟我们的 mod 会不会打架

- **SpirePvp**：补丁集中在 lobby/地图/战斗，且只在进 PvP 时生效 —— 跟斗地主桌（篝火场景）
  理论上不撞；同装测试时重点看 BaseLib 版本是否 ≥3.4.7（我们依赖 BaseLib，版本得够）。
- **琐事**：内容向 + 它自己的配置会话补丁，没有看到动篝火/休息处的补丁，撞车风险低。
- 两个都装 + 斗地主：没实测，属于"应该没事，但以实测为准"。

## 四、待办钩子（留给下一步）

- [ ] 斗地主四人联机（Phase C）参考 `HostMenuPatch` + `NetHostGameService.StartSteamHost(4)`。
- [ ] 游戏下次大版本更新把斗地主弄坏时，参考琐事的"引导壳 + 内嵌双实现"。
- [ ] （可选）把我们 2v2 的 TableSize 设置挂进联机大厅（`Lobby.SetModifiers` 模式）。
