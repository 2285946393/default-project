# ref-combatflight · 中国人能飞 CombatFlight

> 工坊 `3772226486` · 作者 **肥猫被打了** · v1.3.1 · `affects_gameplay: **true**` · 依赖 ModConfig 0.2.3
> 拆解笔记：`notes/杀戮尖塔2-拆解-中国人能飞-20261009.md`

**这个 mod 本身只是个玩具（1,294 行，让战斗角色飞起来），但它无意中摊开了游戏的整套联机机制。**
→ **斗地主 netcode 的核心参照**。

包结构：**只有一个 2MB 的 dll，没有 pck** —— 2MB 几乎全是**内嵌的 mp3**。

---

## 目录说明

| 路径 | 是什么 |
|---|---|
| `decompiled/CombatFlight/FlightController.cs` | **核心 889 行**：输入 / 每帧推进 / 位置同步 / 内嵌音频 / 提示条 |
| `decompiled/CombatFlight/FlightSettings.cs` | **ModConfig 用法样本**（222 行，13 个配置项，含按键绑定） |
| `decompiled/CombatFlight/Sts2CombatFlightPositionMessage.cs` | ★★ **自定义网络消息样本**（`INetMessage` + `IPacketSerializable`） |
| `decompiled/CombatFlight/NGameInputPatch.cs` 等 5 个 | 补丁挂载点（`_Input` / `_Ready` / `_Process` / `_Notification` / `SetBgmVol`） |
| `decompiled/CombatFlight/FlightText.cs` | 极简本地化（判 `LocManager.Instance.Language`） |
| `manifest.json` | 工坊清单 |

---

## 关键结论速查（给国内 WB 用）

**1. 自定义网络消息 = 实现 `INetMessage` 即可，启动自动注册。**
```csharp
public struct MyMessage : INetMessage, IPacketSerializable {
    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => (NetTransferMode)1;   // 1=Unreliable, 2=Reliable
    public LogLevel LogLevel => (LogLevel)0;             // VeryDebug
    public bool ShouldBuffer => true;
    public void Serialize(PacketWriter w) { … }
    public void Deserialize(PacketReader r) { … }
}
```
发现机制：`MessageTypes.Initialize()` 里 `list.AddRange(ReflectionHelper.GetSubtypesInMods<INetMessage>())`。

**2. ★★★ `affects_gameplay` 是联机 id 稳定性的开关，不是描述文字。**
`NetTypeCache` 的 id 由 `ContentSorter.Sort` 决定，**第一优先级就是 `affectsGameplay`**
（`false` 的 mod 内容排到末尾 → 保护"没装该 mod 的玩家"的 id 对齐）。
游戏源码注释原话："**This is important so that players without those mods will still have a stable and
deterministic sorting of IDs that will be sent over the network.**"

**3. ★★★ `ModManager.AssociateAssemblyWithMod` 不调 = 三套 id 一起错位。**
`ContentSorter` 被 `ModelIdSerializationCache` / `MessageTypes` / `ActionTypes` **共用** ——
所以那条 `not associated with any mod ... errors in multiplayer` 警告坏的不只是"内容排序"。

**4. 收发接口**（`RunManager.Instance.NetService`，类型 `INetGameService`）：
`SendMessage<T>(msg)` / `SendMessage<T>(msg, playerId)` / `RegisterMessageHandler<T>(handler)` /
`UnregisterMessageHandler<T>` / `SetBufferMessages(bool)` / `IsConnected` / `NetId` / `Type` / `LocalVersion`。

**5. 位置同步配方**（可直接抄）：相对偏移 + 序号去重 + 环形比较 + 按需限流 + `IsFinite` 校验 +
`ClampNetworkOffset(±10000)` + `Location` 一致性校验（不同战斗整包丢弃）。

**6. dll 内嵌 mp3 并播放**（不带 pck 的做法）：
`Assembly.GetManifestResourceStream("Xxx.Song.mp3")` → `AudioStreamMP3{Data, Loop=true}` →
`AudioStreamPlayer` → `AddChildSafely(NGame.Instance, player)`；
压制原 BGM 用 `NAudioManager.Instance.SetBgmVol(0f)`，恢复读 `SettingsSave.VolumeBgm`。

---

## 依赖框架

`../ref-modconfig/` —— **ModConfig（"皮皮配置"）**，工坊 `3747557003`，`affects_gameplay: false`。
生态里**第三个**配置框架（前两个：RitsuLib 设置页、BaseLib `SimpleModConfig`）。
`ModConfigApi` 只有 37 行：`Register(modId, name, displayNames, ConfigEntry[])` / `GetValue<T>` / `SetValue`。
`ConfigType` 9 种：`Toggle / Slider / Dropdown / KeyBind / TextInput / Header / Separator / Button / ColorPicker`。
