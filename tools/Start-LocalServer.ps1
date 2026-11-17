# Lance le serveur de test local (E:\game).
#   pwsh tools/Start-LocalServer.ps1            -> mode TWD complet : nos mods + WalkerSim, carte Géorgie, SandboxCode TWD
#   pwsh tools/Start-LocalServer.ps1 -Vanilla   -> mode sans mods (diagnostic) : config de test, Navezgane
#   pwsh tools/Start-LocalServer.ps1 -Exclude WalkerSim2,TWD_Vehicles   -> mode TWD sans certains mods (diagnostic)
# Arrête d'abord tout serveur déjà lancé. Connexion en jeu : 127.0.0.1, port 26900.
param([switch]$Vanilla, [string[]]$Exclude = @(), [string]$GameDir = "E:\game")

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$ours = @("TWD_Core", "TWD_Metiers", "TWD_UI", "TWD_Vehicles", "TWD_Zones", "WalkerSim2")

Get-Process 7DaysToDieServer -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep 3
foreach ($m in $ours) { Remove-Item (Join-Path $GameDir "Mods\$m") -Recurse -Force -ErrorAction SilentlyContinue }

if ($Vanilla) {
    $config = "serverconfig.test.xml"
} else {
    foreach ($m in $ours) { if ($Exclude -notcontains $m) { Copy-Item (Join-Path $root "Mods\$m") (Join-Path $GameDir "Mods\") -Recurse -Force } }
    Copy-Item (Join-Path $root "serverconfig.twd.xml") (Join-Path $GameDir "serverconfig.twd.xml") -Force
    $config = "serverconfig.twd.xml"
}

$log = Join-Path $GameDir "twd_test.log"
Remove-Item $log -ErrorAction SilentlyContinue
$p = Start-Process -FilePath (Join-Path $GameDir "7DaysToDieServer.exe") -WorkingDirectory $GameDir -PassThru `
    -ArgumentList "-quit", "-batchmode", "-nographics", "-configfile=$config", "-dedicated", "-logfile `"$log`""
for ($i = 0; $i -lt 600; $i++) {
    if ((Test-Path $log) -and (Select-String -Path $log -Pattern "StartGame done" -Quiet)) { break }
    if ($p.HasExited) { throw "Le serveur s'est arrêté au démarrage, voir $log" }
    Start-Sleep 1
}
Write-Host ("Serveur prêt ({0}), PID {1}. Mods : {2}" -f $(if ($Vanilla) { "SANS MODS" } else { "TWD complet" }), $p.Id, ((Get-ChildItem (Join-Path $GameDir "Mods") -Name) -join ", "))
