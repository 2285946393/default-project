# 立体尖塔 · 战斗场景视差（ParallaxSpire）

版本 0.1.1（2026-10-13）。方向已由用户拍板：**放弃第一人称，改做战斗场景视差**。

## 反编译确认的事实（不要猜）

- `NCombatBackground` 的层级是固定算法生成的：
  `Layer_00 / Layer_01 / ... / Foreground`。
- `_bg_` 图层按文件名序号**从远到近**叠放；`Foreground` 是唯一前景层。
- 每个 `Layer_XX` 只是**占位节点**，真正的 `NCombatBackgroundLayer` 里面有
  `Visual` 和 `PhobiaModeVisual`，由本体按恐惧症模式切换可见性。
  **所以只移动占位节点，不碰里面的子节点**，否则可能破坏恐惧症模式。
- 场景可能随机选同一序号的不同变体（`_bg_01_a/b/...`），数量可能变化；
  0.1.1 起，背景子节点数量变化时会自动重新收集并恢复旧位置。

## 这版能做什么

- 进入普通战斗自动生效：背景各图层按远近不同幅度随鼠标偏移，前景反向加强，产生深度感。
- **不移动怪物、不碰手牌/血条/特效容器**，不改战斗规则。
- F9 开关视差，只在本次运行内记住，不写配置、不碰存档。
- 离开战斗/切换场景时恢复图层原位置。

## 结构

| 文件 | 作用 |
|---|---|
| `MainFile.cs` | 入口，真实战斗挂控制器 |
| `ParallaxController.cs` | 每帧计算并应用视差偏移 |
| `Core/ParallaxMath.cs` | 纯数学：系数、归一化、平滑，离线可测 |
| `tests/Program.cs` | 17 项逻辑检查 |

## 构建/测试

```powershell
cd "C:\Users\有花无实\Documents\Default Project\projects\parallax-spire"
dotnet restore ParallaxSpire.csproj --source 'C:/Users/有花无实/.nuget/packages' -p:NuGetAudit=false
dotnet build ParallaxSpire.csproj --no-restore
dotnet restore tests/ParallaxChecks.csproj --source 'C:/Users/有花无实/.nuget/packages' -p:NuGetAudit=false
dotnet build tests/ParallaxChecks.csproj --no-restore
& '.\tests\.godot\mono\temp\bin\Debug\ParallaxChecks.exe'
```

## 当前验证

- 主工程：0 警告 0 错误。
- 逻辑检查：17 项通过。
- 已部署到游戏 mods 目录，dll/json 哈希与构建产物一致。
- **未实机验收**。检查无法证明画面效果、图层名适配、与其他背景类 mod 的兼容性。

## 下一步候选

1. 实机确认 Layer_N 命名是否覆盖所有场景；不行就按子节点序号分组。
2. 加 ModConfig：强度滑条、开关前景、默认关闭。
3. 加雾/光罩层，让深度更明显。
4. 若效果太弱，可把偏移从 ±14px 提到 ±24px（需实测眩晕感）。

