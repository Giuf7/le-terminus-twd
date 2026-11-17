# Construit le pack client "TWD_Textes" : uniquement les textes (Localization.csv) de nos mods.
# Le serveur envoie ses XML au jeu à la connexion, mais PAS les textes : sans ce pack, les joueurs
# voient des codes bruts (ex. perkTWDMetierMedecinName) au lieu des noms.
# Résultat : dist/TWD_Textes (dossier à mettre dans <jeu>/Mods) et dist/TWD_Textes.zip (à distribuer).
#   pwsh tools/Build-ClientPack.ps1 [-InstallTo "F:\SteamLibrary\steamapps\common\7 Days To Die"]
param([string]$InstallTo = "")

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root "dist\TWD_Textes"
Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force (Join-Path $out "Config") | Out-Null

# Fusion des Localization.txt (même en-tête), sans doublon de clé
$header = $null; $seen = @{}; $lines = New-Object System.Collections.Generic.List[string]
foreach ($mod in @("TWD_Core", "TWD_Metiers", "TWD_UI")) {
    $file = Join-Path $root "Mods\$mod\Config\Localization.csv"
    $content = [IO.File]::ReadAllLines($file, [Text.Encoding]::UTF8)
    if (-not $header) { $header = $content[0] }
    foreach ($l in $content | Select-Object -Skip 1) {
        if (-not $l.Trim()) { continue }
        $key = $l.Split(',')[0]
        if (-not $seen.ContainsKey($key)) { $seen[$key] = $true; $lines.Add($l) }
    }
}
$enc = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllLines((Join-Path $out "Config\Localization.csv"), @($header) + $lines, $enc)
[IO.File]::WriteAllText((Join-Path $out "ModInfo.xml"), @'
<?xml version="1.0" encoding="UTF-8" ?>
<xml>
	<Name value="TWD_Textes" />
	<DisplayName value="Le Terminus - textes" />
	<Description value="Textes (français / anglais) du serveur Le Terminus. À installer dans le dossier Mods du jeu pour jouer sur le serveur." />
	<Author value="Le Terminus" />
	<Version value="1.0.0" />
	<Website value="" />
</xml>
'@, $enc)

$zip = Join-Path $root "dist\TWD_Textes.zip"
Remove-Item $zip -ErrorAction SilentlyContinue
Compress-Archive -Path $out -DestinationPath $zip
Write-Host "$($lines.Count) textes -> $out (+ $zip)"

if ($InstallTo) {
    $dest = Join-Path $InstallTo "Mods\TWD_Textes"
    Remove-Item $dest -Recurse -Force -ErrorAction SilentlyContinue
    Copy-Item $out (Join-Path $InstallTo "Mods\") -Recurse -Force
    Write-Host "Installé dans $dest"
}
