# 重新打包 FrostSpire.pck（工事牌卡面 + 以后的 UI 素材）
#
# 什么时候要跑这个：
#   - 改了 FrostSpire/images/ 下的图（卡面、遗物图标…）
#   - 改了 FrostSpire/localization/ 下的文本（如果以后走 pck 本地化）
# 只改 C# 代码不需要跑（那部分在 dll 里）。
#
# 跑完之后再 `dotnet build`，csproj 的 CopyMod 目标会把 pck 一起复制到游戏的 mods 目录。
#
# 说明（照 quest-spire 的 build_pck.ps1）：
#   mod 的 pck 挂载在 `res://` 根上，所以项目里的 `FrostSpire/` 目录
#   在游戏里就是 `res://FrostSpire/`。
#   卡面代码写死的路径是 `res://FrostSpire/images/card_art/<entry>.png`
#   （见 FrostSpireCode/Cards/FrostCardArt.cs）—— 两边必须对得上。

$ErrorActionPreference = 'Stop'

$godot = 'D:\software\godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe'
$proj = Split-Path -Parent $PSScriptRoot
$pck = Join-Path $proj 'FrostSpire.pck'

if (-not (Test-Path $godot)) { throw "找不到 Godot: $godot（改脚本里的路径，或者装一个）" }
if (-not (Test-Path (Join-Path $proj 'export_presets.cfg'))) { throw "找不到 export_presets.cfg" }

Write-Host '=== 1/2 导入素材（图会被 Godot 转成引擎格式） ===' -ForegroundColor Cyan
& $godot --headless --path $proj --import | Out-Null

Write-Host '=== 2/2 打包 ===' -ForegroundColor Cyan
& $godot --headless --path $proj --export-pack 'FrostSpirePck' $pck | Out-Null

if (Test-Path $pck) {
    $size = [math]::Round((Get-Item $pck).Length / 1KB, 1)
    Write-Host "完成：$pck（$size KB）" -ForegroundColor Green
    Write-Host '接着跑 dotnet build 把它部署进游戏。'
} else {
    throw '打包失败，pck 没生成'
}
