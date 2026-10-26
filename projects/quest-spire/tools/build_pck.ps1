# 重新打包 QuestSpire.pck（图标 + 本地化）
#
# 什么时候要跑这个：
#   - 改了 QuestSpire/images/ 下的图
#   - 改了 QuestSpire/localization/ 下的文本
# 只改 C# 代码不需要跑（那部分在 dll 里）。
#
# 跑完之后再 `dotnet build`，csproj 的 CopyMod 目标会把 pck 一起复制到游戏的 mods 目录。
#
# 说明：Godot 读 mod 素材的地址是写死的 res://<ModId>/...，这个地址只有打了 pck 才存在。
# 没有 pck 时，游戏找不到遗物图标就会回退成 locked_model.png（锁头），连名字和说明也会变成「锁定」文案。

$ErrorActionPreference = 'Stop'

$godot = 'D:\software\godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe'
$proj = Split-Path -Parent $PSScriptRoot
$pck = Join-Path $proj 'QuestSpire.pck'

if (-not (Test-Path $godot)) { throw "找不到 Godot: $godot（改脚本里的路径，或者装一个）" }
if (-not (Test-Path (Join-Path $proj 'export_presets.cfg'))) { throw "找不到 export_presets.cfg" }

Write-Host '=== 1/2 导入素材（图会被 Godot 转成引擎格式） ===' -ForegroundColor Cyan
& $godot --headless --path $proj --import | Out-Null

Write-Host '=== 2/2 打包 ===' -ForegroundColor Cyan
& $godot --headless --path $proj --export-pack 'QuestSpirePck' $pck | Out-Null

if (Test-Path $pck) {
    $size = [math]::Round((Get-Item $pck).Length / 1KB, 1)
    Write-Host "完成：$pck（$size KB）" -ForegroundColor Green
    Write-Host '接着跑 dotnet build 把它部署进游戏。'
} else {
    throw '打包失败，pck 没生成'
}
