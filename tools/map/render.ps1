# Dessine la carte staff d'un monde généré : routes, bâtiments, lieux clés (Atlanta, prisons, base militaire, hôpitaux).
#   pwsh tools/map/render.ps1 -World "North Kezunahu County" -Zones -Out carte_10k_staff.png
param([string]$World = "North Kezunahu County", [string]$Out = "$PSScriptRoot\carte.png", [int]$Size = 2048, [switch]$Zones)
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition (Get-Content "$PSScriptRoot\Render.cs" -Raw) -ReferencedAssemblies System.Drawing, System.Drawing.Primitives, System.Drawing.Common -ErrorAction Stop
$w = "$env:APPDATA\7DaysToDie\GeneratedWorlds\$World"
[xml]$mi = Get-Content "$w\map_info.xml" -Raw
$N = [int](($mi.SelectSingleNode("//property[@name='HeightMapSize']").value -split ',')[0]); $half = $N / 2
$bmp = [MapRender]::Roads("$w\splat3.png", $Size)
$g = [Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'AntiAliasGridFit'
$s = $Size / [double]$N
function To-Px([double]$x, [double]$z) { return [Drawing.PointF]::new(($x + $half) * $s, ($half - $z) * $s) }   # haut = nord

[xml]$p = Get-Content "$w\prefabs.xml" -Raw
$decos = foreach ($d in $p.prefabs.decoration) { $c = $d.position -split ','; [pscustomobject]@{ n = $d.name; x = [double]$c[0]; z = [double]$c[2] } }
$poi = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(150, 200, 195, 180))
foreach ($d in $decos) {
    if ($d.n -match '^(part_|deco_|rock_|tree_|bush_|utility_|street_light|sign_|road_|bridge_)') { continue }
    $pt = To-Px $d.x $d.z; $g.FillRectangle($poi, $pt.X, $pt.Y - 3, 3, 3)
}

if ($Zones) {
    [xml]$zx = Get-Content "C:\Users\piino\Desktop\7DTD TWD\Mods\TWD_Zones\zones.xml" -Raw
    $pen = New-Object Drawing.Pen ([Drawing.Color]::FromArgb(230, 220, 50, 40)), 4
    $fill = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(45, 220, 50, 40))
    foreach ($z in $zx.SelectNodes("//zone")) {
        if ($z.radius) { $c = To-Px ([double]$z.x) ([double]$z.z); $r = [double]$z.radius * $s; $g.FillEllipse($fill, $c.X - $r, $c.Y - $r, 2 * $r, 2 * $r); $g.DrawEllipse($pen, $c.X - $r, $c.Y - $r, 2 * $r, 2 * $r) }
        elseif ($z.x1) { $a = To-Px ([double]$z.x1) ([double]$z.z2); $b = To-Px ([double]$z.x2) ([double]$z.z1); $g.FillRectangle($fill, $a.X, $a.Y, $b.X - $a.X, $b.Y - $a.Y); $g.DrawRectangle($pen, $a.X, $a.Y, $b.X - $a.X, $b.Y - $a.Y) }
    }
}

# Lieux clés trouvés dans la liste des bâtiments
$font = New-Object Drawing.Font("Segoe UI", 15, [Drawing.FontStyle]::Bold)
$white = [Drawing.Brushes]::White; $shadow = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(200, 0, 0, 0))
$dot = New-Object Drawing.SolidBrush ([Drawing.Color]::FromArgb(255, 255, 210, 80))
function Label($text, $x, $z) {
    $pt = To-Px $x $z
    $g.FillEllipse($dot, $pt.X - 6, $pt.Y - 6, 12, 12)
    $g.DrawString($text, $font, $shadow, $pt.X + 11, $pt.Y - 11); $g.DrawString($text, $font, $white, $pt.X + 9, $pt.Y - 13)
}
$sky = @($decos | Where-Object { $_.n -match '^(remnant_)?skyscraper_' })
if ($sky.Count) { $best = $sky | Sort-Object { $o = $_; @($sky | Where-Object { [math]::Abs($_.x - $o.x) -lt 800 -and [math]::Abs($_.z - $o.z) -lt 800 }).Count } -Descending | Select-Object -First 1; $core = @($sky | Where-Object { [math]::Abs($_.x - $best.x) -lt 800 -and [math]::Abs($_.z - $best.z) -lt 800 }); $ax = ($core | Measure-Object x -Average).Average; $az = ($core | Measure-Object z -Average).Average; Label "ATLANTA (PvP)" $ax $az; "Atlanta ~ $([int]$ax), $([int]$az) ($($sky.Count) gratte-ciel)" }
$i = 0; foreach ($d in ($decos | Where-Object { $_.n -match '^prison_0\d$' })) { $i++; Label "Prison $i" $d.x $d.z; "Prison $i : $($d.n) $([int]$d.x), $([int]$d.z)" }
foreach ($d in ($decos | Where-Object { $_.n -match '^base_military' })) { Label "Base militaire" $d.x $d.z; "Base militaire $([int]$d.x), $([int]$d.z)" }
foreach ($d in ($decos | Where-Object { $_.n -match '^hospital_' })) { Label "Hôpital" $d.x $d.z; "Hôpital $([int]$d.x), $([int]$d.z)" }

if ($Zones) { foreach ($z in $zx.SelectNodes("//zone[@radius]")) { Label ($z.name.ToUpper() + " (PvP)") ([double]$z.x) ([double]$z.z) } }
$big = New-Object Drawing.Font("Segoe UI", 26, [Drawing.FontStyle]::Bold)
$g.DrawString("N ↑", $big, $white, $Size - 90, 15)
$km = 1000 * $s; $g.FillRectangle($white, 30, $Size - 40, $km, 6); $g.DrawString("1 km", $font, $white, 30, $Size - 75)
$g.Dispose()
$bmp.Save($Out, [Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
"Carte : $Out ($N m)"
