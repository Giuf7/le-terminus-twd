# Compile un mod C# du projet (C# 5, compilateur du .NET Framework fourni avec Windows).
#   pwsh tools/Build-Mod.ps1 -Name TWD_Zones
#   pwsh tools/Build-Mod.ps1 -Name TWD_Vehicles
# Source : src/<Name>/*.cs   ->   Mods/<Name>/<Name>.dll
param(
    [Parameter(Mandatory = $true)][string]$Name,
    [string]$GameDir = "E:\game"
)
# Dépendances entre nos mods (DLL déjà compilées dans Mods/)
$modDeps = @{ "TWD_Vehicles" = @("TWD_Zones") }

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$managed = Join-Path $GameDir "7DaysToDieServer_Data\Managed"
$harmony = Join-Path $GameDir "Mods\0_TFP_Harmony\0Harmony.dll"
$out = Join-Path $root "Mods\$Name\$Name.dll"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$src = @(Get-ChildItem (Join-Path $root "src\$Name") -Filter *.cs | ForEach-Object { $_.FullName })
if (-not $src) { throw "Aucun fichier source dans src\$Name" }

$refs = @("mscorlib.dll","System.dll","System.Core.dll","System.Xml.dll","netstandard.dll","UnityEngine.dll","UnityEngine.CoreModule.dll","Assembly-CSharp.dll","Assembly-CSharp-firstpass.dll","LogLibrary.dll") |
    ForEach-Object { "/r:" + (Join-Path $managed $_) }

if ($modDeps.ContainsKey($Name)) {
    foreach ($dep in $modDeps[$Name]) { $refs += "/r:" + (Join-Path $root "Mods\$dep\$dep.dll") }
}
New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null
$output = & $csc /nologo /noconfig /nostdlib+ /target:library /optimize+ /warn:1 "/out:$out" @refs "/r:$harmony" @src 2>&1
if ($LASTEXITCODE -ne 0) { $output | ForEach-Object { Write-Host $_ }; throw "Echec de compilation de $Name" }
Write-Host "OK : $out"
