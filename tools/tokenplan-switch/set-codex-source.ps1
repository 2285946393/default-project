param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('tokenplan', 'deepseek')]
    [string]$Target
)

$ErrorActionPreference = 'Stop'

$cfg = Join-Path $env:USERPROFILE '.codex\config.toml'
if (-not (Test-Path -LiteralPath $cfg)) {
    Write-Host "找不到 Codex 配置文件：$cfg" -ForegroundColor Red
    exit 1
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
Copy-Item -LiteralPath $cfg -Destination "$cfg.bak-switch-$stamp" -Force

if ($Target -eq 'tokenplan') {
    $model    = 'deepseek-v4-flash-0731'
    $provider = 'tokenplan'
    $catalog  = (Join-Path $env:USERPROFILE '.codex\models-tokenplan.json') -replace '\\', '/'
    $label    = '书生 TokenPlan（免费额度）'
} else {
    $model    = 'deepseek-flash'
    $provider = 'deepseek'
    $catalog  = (Join-Path $env:USERPROFILE '.codex\models.json') -replace '\\', '/'
    $label    = 'DeepSeek 官方'
}

$text = [IO.File]::ReadAllText($cfg, [Text.Encoding]::UTF8)
$text = [regex]::Replace($text, '(?m)^model = "[^"]*"\r?$', 'model = "' + $model + '"')
$text = [regex]::Replace($text, '(?m)^model_provider = "[^"]*"\r?$', 'model_provider = "' + $provider + '"')
$text = [regex]::Replace($text, '(?m)^model_catalog_json = "[^"]*"\r?$', 'model_catalog_json = "' + $catalog + '"')
[IO.File]::WriteAllText($cfg, $text, (New-Object Text.UTF8Encoding($false)))

Write-Host ""
Write-Host "已经把 Codex 切到：$label" -ForegroundColor Green
Write-Host "  模型   $model"
Write-Host "  来源   $provider"
Write-Host ""
Write-Host "请重启 Codex 桌面端（完全关掉再打开）才会生效。" -ForegroundColor Yellow
Write-Host "原来的配置已备份为：$cfg.bak-switch-$stamp"
Write-Host ""
