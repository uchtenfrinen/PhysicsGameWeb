#requires -Version 5.1
<#
.SYNOPSIS
  Writes Groq API key to groq_api_key.txt in the repo (and optionally bin + user env).

.DESCRIPTION
  groq_api_key.txt is gitignored. Do not commit the key.

.EXAMPLE
  .\scripts\set-groq-api-key.ps1

.EXAMPLE
  .\scripts\set-groq-api-key.ps1 -ApiKey "gsk_..." -AlsoCopyToBin

.EXAMPLE
  .\scripts\set-groq-api-key.ps1 -SetUserEnvironment
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $ApiKey,

    [Parameter(Mandatory = $false)]
    [string] $RepoRoot,

    [switch] $AlsoCopyToBin,

    [switch] $SetUserEnvironment
)

function Normalize-ApiKey {
    param([string] $Raw)
    if ([string]::IsNullOrWhiteSpace($Raw)) { return $null }
    $k = $Raw.Trim().Trim([char]0xFEFF, [char]0x200B)
    if ($k.StartsWith('Bearer ', [System.StringComparison]::OrdinalIgnoreCase)) {
        $k = $k.Substring(7).Trim()
    }
    if (($k.Length -ge 2) -and (
            ($k.StartsWith('"') -and $k.EndsWith('"')) -or
            ($k.StartsWith("'") -and $k.EndsWith("'"))
        )) {
        $k = $k.Substring(1, $k.Length - 2).Trim()
    }
    if ([string]::IsNullOrWhiteSpace($k)) { return $null }
    return $k
}

if (-not $RepoRoot) {
    if ($PSScriptRoot) {
        $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    }
    else {
        $RepoRoot = (Get-Location).Path
    }
}
$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    $secure = Read-Host -Prompt 'Paste Groq API key' -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try {
        $ApiKey = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

$key = Normalize-ApiKey -Raw $ApiKey
if (-not $key) {
    Write-Error 'Key is empty or invalid.'
    exit 1
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$content = $key + "`n"

$targets = [System.Collections.Generic.List[string]]::new()
$targets.Add((Join-Path $RepoRoot 'groq_api_key.txt'))
$targets.Add((Join-Path $RepoRoot 'WPFPhisics1.1\WPFPhisics1.1\groq_api_key.txt'))
$ftPath = Join-Path $RepoRoot 'ForTeacher\ForTeacher\groq_api_key.txt'
if (Test-Path -LiteralPath (Split-Path $ftPath -Parent)) {
    $targets.Add($ftPath)
}

$written = @()
foreach ($p in $targets) {
    $dir = Split-Path -Parent $p
    if (-not (Test-Path -LiteralPath $dir)) {
        Write-Warning "Skip (folder missing): $p"
        continue
    }
    [System.IO.File]::WriteAllText($p, $content, $utf8NoBom)
    $written += $p
}

if ($AlsoCopyToBin) {
    foreach ($cfg in @('Debug', 'Release')) {
        foreach ($tfm in @('net9.0-windows', 'net8.0-windows')) {
            foreach ($leaf in @(
                    "PhisicsGame\bin\$cfg\$tfm\groq_api_key.txt",
                    "WPFPhisics1.1\WPFPhisics1.1\bin\$cfg\$tfm\groq_api_key.txt",
                    "ForTeacher\ForTeacher\bin\$cfg\$tfm\groq_api_key.txt"
                )) {
                $binFile = Join-Path $RepoRoot $leaf
                $binDir = Split-Path -Parent $binFile
                if (Test-Path -LiteralPath $binDir) {
                    [System.IO.File]::WriteAllText($binFile, $content, $utf8NoBom)
                    $written += $binFile
                }
            }
        }
    }
}

if ($SetUserEnvironment) {
    [System.Environment]::SetEnvironmentVariable('GROQ_API_KEY', $key, 'User')
    Write-Host 'User env GROQ_API_KEY set. Restart terminal / IDE to pick it up.'
}

Write-Host 'Wrote groq_api_key.txt to:'
$written | Sort-Object -Unique | ForEach-Object { Write-Host "  $_" }
