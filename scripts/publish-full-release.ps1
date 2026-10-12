#requires -Version 5.1
<#
.SYNOPSIS
  Publishes PhisicsGame with PhysicsTasksTerminal, ForTeacher, WPFPhisics1.1 in one folder.

.DESCRIPTION
  Framework-dependent (FDD) by default: requires .NET 9 Windows Desktop Runtime on the PC.
  Use -SelfContained for a larger folder that runs without a separate runtime install.

  Groq: run scripts\set-groq-api-key.ps1 or put groq_api_key.txt next to PhisicsGame.exe (not committed to git).

.EXAMPLE
  .\scripts\publish-full-release.ps1

.EXAMPLE
  .\scripts\publish-full-release.ps1 -OutputPath artifacts\MyBuild -SelfContained
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string] $OutputPath = 'artifacts\PhisicsGame-Release',

    [switch] $SelfContained
)

$ErrorActionPreference = 'Stop'

$repoRoot = if ($PSScriptRoot) {
    (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}
else {
    (Get-Location).Path
}

$project = Join-Path $repoRoot 'PhisicsGame\PhisicsGame.csproj'
$out = Join-Path $repoRoot $OutputPath
$rid = 'win-x64'

Push-Location $repoRoot
try {
    if ($SelfContained) {
        dotnet publish $project -c Release -o $out -r $rid --self-contained true
    }
    else {
        dotnet publish $project -c Release -o $out
    }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Output: $out"
Write-Host "Main executable: PhisicsGame.exe (also: PhysicsTasksTerminal.exe, ForTeacher.exe, WPFPhisics1.1.exe)."
if (-not $SelfContained) {
    Write-Host "Requires .NET 9 Desktop Runtime: https://dotnet.microsoft.com/download/dotnet/9.0"
}
Write-Host "Groq API: set GROQ_API_KEY or place groq_api_key.txt next to the exe (see scripts\set-groq-api-key.ps1)."
