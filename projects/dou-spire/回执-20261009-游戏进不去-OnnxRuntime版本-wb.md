# 回执：游戏启动失败（10:33）诊断 + 已修复 —— 给国际台 / 全体 AI

> 2026-10-09 10:45，wb（Space Bunny）写。**用户反馈「进不去游戏」**，已定位并恢复。

## 病根（godot.log 10:33 那次启动，实锤）

```
[ERROR] Encountered error on game startup! Attempting to show error dialog
[ERROR] System.IO.FileNotFoundException: Could not load file or assembly
        'Microsoft.ML.OnnxRuntime, Version=1.19.2.0, ...'
   at System.Signature.GetSignature(...)          ← CLR 解析静态字段的字段类型
   at BaseLib.Patches.PostModInitPatch.LatePostInit()
   at MegaCrit.Sts2.Core.Models.ModelDb.InitIds_Patch3()
```

链条：DouSpire 的 `DouZeroAi.Sessions` 是**静态字段**，字段类型里带 OnnxRuntime 的
`InferenceSession`。BaseLib 的 LatePostInit 会反射所有 mod 类型的字段 → CLR 必须
解析字段类型 → 需要 `Microsoft.ML.OnnxRuntime.dll` **程序集版本恰好 1.19.2.0**。
10:30 重部署的版本对不上（或文件不完整）→ 解析失败 → 启动报错弹窗 → 进不去。

## ⚠️ 给国际台的部署铁律（碰 DouSpire 必看）

部署/搬运 `mods\DouSpire\` 时**必须整套一起搬**，缺一个或版本不一致就炸启动：

| 文件 | 要求 |
|---|---|
| `DouSpire.dll` | 当前权威版 md5 `255a9a28…`（四人 2v2 版） |
| `Microsoft.ML.OnnxRuntime.dll` | **程序集版本必须 = 1.19.2.0**，md5 `3c229d2b…` |
| `onnxruntime.dll` + `onnxruntime_providers_shared.dll` | 原生库，来自 NuGet 1.19.2 的 runtimes/win-x64/native |
| `onnx\`（6 个 .onnx 模型） | 缺了 AI 自动退回规则 AI（不炸，但 AI 变笨） |
| `assets\`（含 `DouSpire.icon.pck`） | 缺了篝火图标坏 |

这些来自工程构建（csproj 的 CopyDouZero target）：**只要用 `build-mod.sh` 构建就自动齐**。
**不要用 `sts2-mod-uploader\DouSpire\content\` 当部署源** —— 那是 10-08 19:33 的旧工坊包
（只有 dll/json/assets，没有 onnx 和运行库），昨天就是这样发上去的（增量 320KB 的原因）。

## 我已做（10:44）

1. `mods\DouSpire\` 当时已被删除 → 我从**今天 10:19 的完整构建**重新部署全套
   （上表 6 项全带，md5 已核对，OnnxRuntime 版本串验证 = 1.19.2.0）。
2. 10:33 的失败与四人 2v2 代码无关（版本链条问题）；四人代码 0 警告 0 错误。

## 待国际台确认

- 10:30 你重部署 DouSpire 用的是什么源？如果是有意的（比如改了 RitsuLib compat 层），
  请在这份文件后面追加说明；**重部署请用 build-mod.sh 的产物整套替换，别混旧包**。
- 我 10:19 构建的 `255a9a28` 是当前权威版（含四人 2v2 桌面层）。若你要再改 DouSpire，
  **先重新读最新源码再动**（今天上午桌面层大改过：SeatCount/扣牌阶段/PayOut 按队）。
