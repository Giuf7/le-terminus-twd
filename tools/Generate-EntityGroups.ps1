# Génère Mods/TWD_Core/Config/entitygroups.xml à partir des fichiers vanilla.
# Chaque zombie spécial, muté (Feral/Radiated/Charged/Infernal) ou animal zombie
# est remplacé par un rôdeur humain "de base" dans TOUS les groupes d'apparition.
# A relancer après chaque mise à jour du jeu :
#   pwsh tools/Generate-EntityGroups.ps1 -GameDir E:\game

param(
    [string]$GameDir = "E:\game",
    [string]$OutFile = "$PSScriptRoot\..\Mods\TWD_Core\Config\entitygroups.xml"
)

$ErrorActionPreference = "Stop"
[xml]$vanilla = Get-Content (Join-Path $GameDir "Data\Config\entitygroups.xml") -Raw
[xml]$classes = Get-Content (Join-Path $GameDir "Data\Config\entityclasses.xml") -Raw
$known = @{}
$classes.entity_classes.entity_class | ForEach-Object { $known[$_.name] = $true }

# Zombies spéciaux -> rôdeur humain le plus proche visuellement
$specials = [ordered]@{
    "zombieRancher"       = "zombieTomClark"
    "zombiePlagueSpitter" = "zombieTomClark"
    "zombieChuck"         = "zombieLumberjack"
    "zombieFrostclaw"     = "zombieLumberjack"
    "zombieSpider"        = "zombieSteve"
    "zombieScreamer"      = "zombieMarlene"
    "zombieMutated"       = "zombieMarlene"
    "zombieDemolition"    = "zombieSoldier"
    "zombieFatCop"        = "zombieMoe"
    "zombieWight"         = "zombieBiker"
    "animalZombieDog"     = "zombieJoe"
    "animalZombieBear"    = "zombieLumberjack"
    "animalZombieBoar"    = "zombieMoe"
    "animalZombieVulture" = "zombieYo"
    "animalBossGrace"     = "zombieMoe"        # sanglier zombie géant (boss)
    "animalDireWolf"      = "zombieJoe"        # loup sanguinaire aux yeux brillants (mutant) ; loup normal dans les groupes d'animaux, voir plus bas
    # Rôdeurs ordinaires mais obèses / boursouflés : pas dans l'esprit TWD (retour joueur 17/11/2026)
    "zombieBoe"           = "zombieJoe"
    "zombieFatHawaiian"   = "zombieMoe"
    "zombieFemaleFat"     = "zombieMarlene"
}

function Get-Walker([string]$name) {
    $base = $name -replace '(Feral|Radiated|Charged|Infernal)$', ''
    if ($specials.Contains($base)) { return $specials[$base] }
    if ($base -ne $name -and $known.ContainsKey($base)) { return $base }
    return $name
}

$names = $vanilla.SelectNodes("//e/@n") | ForEach-Object { $_.Value } | Sort-Object -Unique
# Faune sauvage : dans les groupes d'animaux, le loup sanguinaire devient un loup ordinaire (avant le remplacement général).
# (retour joueur 17/11/2026 : tué par un loup aux yeux brillants)
$lines = @("`t<set xpath=`"//entitygroup[starts-with(@name,'EnemyAnimals') or @name='WolfPack' or @name='animalDireWolf']/e[@n='animalDireWolf']/@n`">animalWolf</set>")
$lines += foreach ($n in $names) {
    $target = Get-Walker $n
    if ($target -ne $n) {
        if (-not $known.ContainsKey($target)) { throw "Cible inconnue : $target (pour $n)" }
        "`t<set xpath=`"//entitygroup/e[@n='$n']/@n`">$target</set>"
    }
}

$header = @"
<?xml version="1.0" encoding="UTF-8"?>
<!-- FICHIER GENERE par tools/Generate-EntityGroups.ps1 - ne pas modifier a la main. -->
<!-- Remplace zombies speciaux, mutes et animaux zombies par des rodeurs humains. -->
<configs>
"@
$content = $header + "`n" + ($lines -join "`n") + "`n</configs>`n"
New-Item -ItemType Directory -Force (Split-Path $OutFile) | Out-Null
Set-Content -Path $OutFile -Value $content -Encoding utf8NoBOM
Write-Host "$($lines.Count) remplacements écrits dans $OutFile"
