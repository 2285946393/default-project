# Repack MemeSpire.pck (localization text only). ASCII-only output for PS 5.1.
$ErrorActionPreference = 'Stop'

$godot = 'D:\software\godot\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe'
$proj = Split-Path -Parent $PSScriptRoot
$pck = Join-Path $proj 'MemeSpire.pck'

if (-not (Test-Path $godot)) { throw "Godot not found: $godot" }

Write-Host '=== 1/2 import ===' -ForegroundColor Cyan
& $godot --headless --path $proj --import | Out-Null

Write-Host '=== 2/2 export-pack ===' -ForegroundColor Cyan
& $godot --headless --path $proj --export-pack 'MemeSpirePck' $pck | Out-Null

if (Test-Path $pck) {
    $size = [math]::Round((Get-Item $pck).Length / 1KB, 1)
    Write-Host "OK: $pck ($size KB)" -ForegroundColor Green
} else {
    throw 'Pack failed: no pck generated'
}
