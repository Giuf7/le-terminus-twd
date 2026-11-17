# Génère Mods/TWD_Core/Config/gamestages.xml : difficulté FIXE dès le jour 1.
# Pour chaque "spawner" à plusieurs paliers (dormeurs, hordes de bruit, POI spéciaux...),
# on ne garde que le palier le plus proche de -TargetStage (sans le dépasser) et on le
# rend valable pour tous les gamestages. Le nombre de rôdeurs ne dépend donc plus
# ni du niveau des joueurs ni du nombre de jours survécus.
# A relancer après chaque mise à jour du jeu ou pour changer la difficulté :
#   pwsh tools/Generate-Gamestages.ps1 -TargetStage 100

param(
    [string]$GameDir = "E:\game",
    [int]$TargetStage = 100,
    [string]$OutFile = "$PSScriptRoot\..\Mods\TWD_Core\Config\gamestages.xml"
)

$ErrorActionPreference = "Stop"
[xml]$vanilla = Get-Content (Join-Path $GameDir "Data\Config\gamestages.xml") -Raw

$lines = @()
$report = @()
foreach ($spawner in $vanilla.gamestages.spawner) {
    $stages = @($spawner.gamestage)
    if ($stages.Count -le 1) { continue }

    $eligible = $stages | Where-Object { [int]$_.stage -le $TargetStage }
    $chosen = if ($eligible) { $eligible | Sort-Object { [int]$_.stage } | Select-Object -Last 1 }
              else { $stages | Sort-Object { [int]$_.stage } | Select-Object -First 1 }
    $name = $spawner.name
    $stage = $chosen.stage

    $lines += "`t<!-- $name : palier $stage conservé sur $($stages.Count) -->"
    $lines += "`t<remove xpath=`"/gamestages/spawner[@name='$name']/gamestage[@stage!='$stage']`"/>"
    $lines += "`t<set xpath=`"/gamestages/spawner[@name='$name']/gamestage/@stage`">0</set>"
    $report += "{0,-28} palier {1}" -f $name, $stage
}

$header = @"
<?xml version="1.0" encoding="UTF-8"?>
<!-- FICHIER GENERE par tools/Generate-Gamestages.ps1 (TargetStage=$TargetStage) - ne pas modifier a la main. -->
<!-- Difficulte fixe des le jour 1 : un seul palier par spawner, valable pour tous les gamestages. -->
<configs>
"@
$content = $header + "`n" + ($lines -join "`n") + "`n</configs>`n"
New-Item -ItemType Directory -Force (Split-Path $OutFile) | Out-Null
Set-Content -Path $OutFile -Value $content -Encoding utf8NoBOM
$report
Write-Host "$($report.Count) spawners figés au palier <= $TargetStage dans $OutFile"
