# Génère Mods/TWD_Core/Config/sounds.xml : armes à feu plus bruyantes.
# Pour chaque son de tir NON silencieux (heat_map_strength présent, hors arcs/arbalètes) :
#   - "noise" (rôdeurs proches qui entendent) x NoiseFactor
#   - "heat_map_strength" (attire des hordes) x HeatFactor
# Les versions silencieuses (*_s_fire) gardent leurs valeurs : un silencieux devient précieux.
#   pwsh tools/Generate-GunNoise.ps1 -NoiseFactor 1.5 -HeatFactor 3

param(
    [string]$GameDir = "E:\game",
    [double]$NoiseFactor = 2,
    [double]$HeatFactor = 3,
    [string]$OutFile = "$PSScriptRoot\..\Mods\TWD_Core\Config\sounds.xml"
)

$ErrorActionPreference = "Stop"
$inv = [Globalization.CultureInfo]::InvariantCulture
[xml]$vanilla = Get-Content (Join-Path $GameDir "Data\Config\sounds.xml") -Raw

$lines = @()
$report = @()
foreach ($node in $vanilla.SelectNodes("/Sounds/SoundDataNode[Noise[@heat_map_strength]]")) {
    $name = $node.name
    if ($name -notmatch '_fire$') { continue }              # uniquement les tirs
    if ($name -match '(^|_)s_fire$') { continue }           # silencieux : inchangés
    if ($name -match 'bow|crossbow') { continue }           # arcs / arbalètes : inchangés
    if ($name -match 'darttrap|junksledge') { continue }    # pièges et outils, pas des armes à feu
    $noise = $node.SelectSingleNode("Noise")
    $n = [double]::Parse($noise.noise, $inv)
    $h = [double]::Parse($noise.heat_map_strength, $inv)
    $newN = [math]::Round($n * $NoiseFactor).ToString($inv)
    $newH = [math]::Round($h * $HeatFactor, 2).ToString($inv)
    $lines += "`t<set xpath=`"/Sounds/SoundDataNode[@name='$name']/Noise/@noise`">$newN</set>"
    $lines += "`t<set xpath=`"/Sounds/SoundDataNode[@name='$name']/Noise/@heat_map_strength`">$newH</set>"
    $report += "{0,-28} bruit {1,4} -> {2,4}   chaleur {3,5} -> {4,5}" -f $name, $n, $newN, $h, $newH
}

$header = @"
<?xml version="1.0" encoding="UTF-8"?>
<!-- FICHIER GENERE par tools/Generate-GunNoise.ps1 (bruit x$NoiseFactor, chaleur x$HeatFactor) - ne pas modifier a la main. -->
<configs>
"@
$engine = @'

	<!-- BRUIT DES MOTEURS : utilisé par le mod C# TWD_Vehicles (AIDirector.NotifyNoise toutes les 2 s
	     pour chaque véhicule à moteur avec un conducteur). Jamais joué comme son : seul le bruit compte.
	     noise = attire les rôdeurs proches ; heat_map = attire les hordes. -->
	<append xpath="/Sounds">
		<SoundDataNode name="twd_vehicle_engine"> <AudioSource name="@:Sounds/Prefabs/AudioSource_Vehicle.prefab"/>
			<Noise ID="4" noise="45" time="3" heat_map_strength="0.15" heat_map_time="120"/>
			<AudioClip ClipName="@:Sounds/Vehicles/Minibike/minibike_idle_lp_1.wav"/>
			<LocalCrouchVolumeScale value="1.0"/> <CrouchNoiseScale value="1"/> <NoiseScale value="1"/> <MaxVoices value="1"/> <MaxRepeatRate value="0.5"/>
		</SoundDataNode>
	</append>
'@
$content = $header + "`n" + ($lines -join "`n") + "`n" + $engine + "</configs>`n"
Set-Content -Path $OutFile -Value $content -Encoding utf8NoBOM
$report
Write-Host "$($report.Count) sons de tir modifiés dans $OutFile"
