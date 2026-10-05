param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('tokenplan', 'deepseek')]
    [string]$Target
)

$ErrorActionPreference = 'Stop'

$dir    = Join-Path $env:USERPROFILE '.claude'
$active = Join-Path $dir 'settings.json'
$source = Join-Path $dir "settings.$Target.json"

if (-not (Test-Path -LiteralPath $source)) {
    Write-Host "找不到模板文件：$source" -ForegroundColor Red
    exit 1
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
if (Test-Path -LiteralPath $active) {
    Copy-Item -LiteralPath $active -Destination "$active.bak-switch-$stamp" -Force
}
Copy-Item -LiteralPath $source -Destination $active -Force

if ($Target -eq 'tokenplan') {
    $label = '书生 TokenPlan（免费额度）'
} else {
    $label = 'DeepSeek 官方'
}

Write-Host ""
Write-Host "已经把 Claude Code 切到：$label" -ForegroundColor Green
Write-Host ""
Write-Host "新开一个终端窗口，再运行 claude 才会生效。" -ForegroundColor Yellow
Write-Host "原来的配置已备份为：$active.bak-switch-$stamp"
Write-Host ""
