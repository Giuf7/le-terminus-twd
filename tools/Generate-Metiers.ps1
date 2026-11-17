# Génère le mod Mods/TWD_Metiers (système de métiers) à partir des fichiers du jeu.
# Règles (voir METIERS.md) :
#  - 7 métiers exclusifs (compétence "Métier : ..." gratuite, dans les compétences générales)
#  - compétences du métier jusqu'au niveau 5, niveau 2 maximum hors métier
#  - magazines : hors métier, seulement le premier palier d'objets ; au-delà le magazine est rendu
#  - Médecin : kit chirurgical exclusif
#  - Elixir de l'oubli retiré du loot (on ne change de métier qu'en mourant)
# A relancer après une mise à jour du jeu :  pwsh tools/Generate-Metiers.ps1 -GameDir E:\game

param(
    [string]$GameDir = "E:\game",
    [string]$OutDir = "$PSScriptRoot\..\Mods\TWD_Metiers"
)
$ErrorActionPreference = "Stop"
$cfg = Join-Path $GameDir "Data\Config"
[xml]$prog = Get-Content (Join-Path $cfg "progression.xml") -Raw
[xml]$items = Get-Content (Join-Path $cfg "items.xml") -Raw

# ---------------------------------------------------------------- données
$metiers = [ordered]@{
    Medecin    = @{ fr = "Médecin";               en = "Doctor";             icon = "ui_game_symbol_medical"
                    descFr = "Soigner, opérer, fabriquer les médicaments. Seul le Médecin fabrique antibiotiques, plâtres, trousses de soins et le kit chirurgical."
                    descEn = "Heal, operate, craft medicine. Only the Doctor crafts antibiotics, casts, first aid kits and the surgical kit."
                    perks = @("perkPhysician","perkHealingFactor","perkCharismaticNature")
                    crafting = @("craftingMedical") }
    Chasseur   = @{ fr = "Chasseur / Éclaireur";  en = "Hunter / Scout";     icon = "ui_game_symbol_archery"
                    descFr = "Arcs, arbalètes, lames et lances. Discrétion, pistage, parkour et chasse."
                    descEn = "Bows, crossbows, blades and spears. Stealth, tracking, parkour and hunting."
                    perks = @("perkArchery","perkDeepCuts","perkFlurryOfAgility","perkHiddenStrike","perkFromTheShadows","perkParkour","perkRuleOneCardio","perkJavelinMaster","perkFlurryOfPerception","perkAnimalTracker","perkPenetrator","perkTheHuntsman")
                    crafting = @("craftingBows","craftingBlades","craftingSpears") }
    Soldat     = @{ fr = "Soldat";                en = "Soldier";            icon = "ui_game_symbol_rifle"
                    descFr = "Toutes les armes à feu, les explosifs de combat, le corps à corps et les armures lourdes."
                    descEn = "All firearms, combat explosives, brawling and heavy armor."
                    perks = @("perkDeadEye","perkDemolitionsExpert","perkBoomstick","perkBrawler","perkMachineGunner","perkFlurryOfFortitude","perkPainTolerance","perkGunslinger","perkRunAndGun","perkHardTarget","perkMediumArmor","perkHeavyArmor")
                    crafting = @("craftingHandguns","craftingShotguns","craftingRifles","craftingMachineGuns","craftingKnuckles","craftingArmor") }
    Mecanicien = @{ fr = "Mécanicien";            en = "Mechanic";           icon = "ui_game_symbol_service"
                    descFr = "Véhicules, essence, récupération de pièces et réparation. Seul à remettre en route les véhicules abandonnés."
                    descEn = "Vehicles, fuel, salvaging and repairs. The only one who can restart abandoned vehicles."
                    perks = @("perkGreaseMonkey","perkSalvageOperations")
                    crafting = @("craftingVehicles","craftingSalvageTools","craftingRepairTools") }
    Ingenieur  = @{ fr = "Ingénieur";             en = "Engineer";           icon = "ui_game_symbol_electric_generator"
                    descFr = "Électricité, pièges, explosifs et robotique. Celui qui éclaire et défend la base."
                    descEn = "Electricity, traps, explosives and robotics. The one who powers and defends the base."
                    perks = @("perkAdvancedEngineering","perkElectrocutioner","perkFlurryOfIntellect","perkInfiltrator")
                    crafting = @("craftingElectrician","craftingTraps","craftingExplosives","craftingRobotics") }
    Fermier    = @{ fr = "Fermier / Cuisinier";   en = "Farmer / Cook";      icon = "ui_game_symbol_tree"
                    descFr = "Cultures, semences, cuisine et conservation. Sans lui, le groupe finit par mourir de faim."
                    descEn = "Crops, seeds, cooking and preserving. Without them, the group eventually starves."
                    perks = @("perkMasterChef","perkLivingOffTheLand","perkSlowMetabolism")
                    crafting = @("craftingFood","craftingSeeds") }
    Batisseur  = @{ fr = "Bâtisseur";             en = "Builder";            icon = "ui_game_symbol_mining"
                    descFr = "Construction, établis, outils, minage, gourdins et masses. Celui qui fortifie la communauté."
                    descEn = "Building, workstations, tools, mining, clubs and sledgehammers. The one who fortifies the community."
                    perks = @("perkPummelPete","perkSkullCrusher","perkGrandSlam","perkFlurryOfStrength","perkMiner69r","perkMotherLode","perkJunkMiner")
                    crafting = @("craftingWorkstations","craftingHarvestingTools","craftingClubs","craftingSledgehammers") }
}
$maxOutside = 2   # niveau max hors métier

# Plafond de fabrication hors métier = juste avant le 2e palier d'objets du domaine
$capOverride = @{ craftingBows = 4 }   # TWD_Core débloque l'arbalète au niveau 5
function Get-Cap($skillName) {
    if ($capOverride.ContainsKey($skillName)) { return $capOverride[$skillName] }
    $cs = $prog.SelectSingleNode("//crafting_skill[@name='$skillName']")
    $starts = @($cs.SelectNodes("display_entry") | ForEach-Object { [int](($_.GetAttribute("unlock_level") -split ',')[0]) } | Sort-Object)
    if ($starts.Count -lt 2) { return 1 }
    return $starts[1] - 1
}

function PerkId($m) { "perkTWDMetier$m" }
function ReqMetier($m) { "<requirement name=`"ProgressionLevel`" progression_name=`"$(PerkId $m)`" operation=`"GTE`" value=`"1`" desc_key=`"twdReqMetier$m`"/>" }

# ---------------------------------------------------------------- progression.xml
$p = New-Object System.Text.StringBuilder
[void]$p.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
[void]$p.AppendLine('<!-- FICHIER GENERE par tools/Generate-Metiers.ps1 - ne pas modifier a la main. -->')
[void]$p.AppendLine('<configs>')
# Onglet « Métiers » dans la fenêtre des compétences : les onglets sont les attributs, dans l'ordre du fichier.
# Même structure que « Avantages généraux » (attribut caché, sans niveau). Placé en PREMIER.
[void]$p.AppendLine("`t<!-- Onglet Métiers (premier onglet des compétences) -->")
[void]$p.AppendLine("`t<insertBefore xpath=`"/progression/attributes/attribute[@name='attPerception']`">")
[void]$p.AppendLine("`t`t<attribute name=`"attTWDMetiers`" name_key=`"attTWDMetiersName`" desc_key=`"attTWDMetiersDesc`" icon=`"ui_game_symbol_tool`" min_level=`"0`" max_level=`"0`" base_skill_point_cost=`"1`" hidden=`"true`"><level_requirements level=`"1`"><requirement name=`"PlayerLevel`" operation=`"GTE`" value=`"1`" desc_key=`"reqGenericPoints01`"/></level_requirements></attribute>")
[void]$p.AppendLine("`t</insertBefore>")
[void]$p.AppendLine("`t<insertBefore xpath=`"/progression/skills/skill[@name='skillPerceptionCombat']`">")
[void]$p.AppendLine("`t`t<skill name=`"skillTWDMetiers`" parent=`"attTWDMetiers`" name_key=`"attTWDMetiersName`" desc_key=`"attTWDMetiersDesc`" long_desc_key=`"attTWDMetiersDesc`" icon=`"ui_game_symbol_tool`" hidden=`"true`"><effect_group/></skill>")
[void]$p.AppendLine("`t</insertBefore>")
[void]$p.AppendLine("`t<!-- Les 7 compétences de métier (exclusives, 1 point) -->")
[void]$p.AppendLine("`t<append xpath=`"/progression/perks`">")
foreach ($m in $metiers.Keys) {
    $d = $metiers[$m]
    [void]$p.AppendLine("`t`t<perk name=`"$(PerkId $m)`" max_level=`"1`" parent=`"skillTWDMetiers`" name_key=`"$(PerkId $m)Name`" desc_key=`"$(PerkId $m)Desc`" icon=`"$($d.icon)`" override_cost=`"1`">")
    # Exclusivité SANS référence entre compétences de métier : des prérequis croisés (A exige B=0, B exige A=0)
    # font tourner le jeu client en boucle jusqu'au plantage (ntdll, constaté le 17/11/2026).
    # On passe par un marqueur calculé par un buff permanent (voir $exclusivity plus bas).
    [void]$p.AppendLine("`t`t`t<level_requirements level=`"1`">")
    [void]$p.AppendLine("`t`t`t`t<requirement name=`"CVarCompare`" cvar=`"twdMetierBlock$m`" operation=`"Equals`" value=`"0`" desc_key=`"twdReqPasDeMetier`"/>")
    [void]$p.AppendLine("`t`t`t</level_requirements>")
    # Description du niveau 1 (affichée dans le panneau de droite ; sans elle, l'interface du jeu plante)
    [void]$p.AppendLine("`t`t`t<effect_group>")
    [void]$p.AppendLine("`t`t`t`t<effect_description level=`"1`" desc_key=`"$(PerkId $m)Rank1Desc`" long_desc_key=`"$(PerkId $m)Rank1LongDesc`"/>")
    if ($m -eq "Medecin") {
        [void]$p.AppendLine("`t`t`t`t<passive_effect name=`"RecipeTagUnlocked`" operation=`"base_set`" level=`"1`" value=`"1`" tags=`"medicalTWDSurgicalKit`"/>")
    }
    [void]$p.AppendLine("`t`t`t</effect_group>")
    [void]$p.AppendLine("`t`t</perk>")
}
[void]$p.AppendLine("`t</append>")

[void]$p.AppendLine("")
[void]$p.AppendLine("`t<!-- Compétences : niveaux 3+ réservés au métier -->")
$perkCount = 0
foreach ($m in $metiers.Keys) {
    foreach ($perkName in $metiers[$m].perks) {
        $perk = $prog.SelectSingleNode("//perks/perk[@name='$perkName']")
        if (-not $perk) { throw "Compétence introuvable : $perkName" }
        $max = if ($perk.GetAttribute("max_level")) { [int]$perk.GetAttribute("max_level") } else { 5 }
        $perkCount++
        # Compétence sans aucun prérequis en vanilla (armures, cuisine...) : si on n'ajoute des prérequis qu'aux
        # niveaux 3+, le jeu bloque aussi les niveaux 1-2. On leur donne un prérequis neutre (niveau de joueur >= 1).
        if (-not $perk.SelectSingleNode("level_requirements")) {
            for ($lvl = 1; $lvl -le [Math]::Min($maxOutside, $max); $lvl++) {
                [void]$p.AppendLine("`t<append xpath=`"/progression/perks/perk[@name='$perkName']`"><level_requirements level=`"$lvl`"><requirement name=`"PlayerLevel`" operation=`"GTE`" value=`"1`"/></level_requirements></append>")
            }
        }
        for ($lvl = $maxOutside + 1; $lvl -le $max; $lvl++) {
            if ($perk.SelectSingleNode("level_requirements[@level='$lvl']")) {
                [void]$p.AppendLine("`t<append xpath=`"/progression/perks/perk[@name='$perkName']/level_requirements[@level='$lvl']`">$(ReqMetier $m)</append>")
            } else {
                [void]$p.AppendLine("`t<append xpath=`"/progression/perks/perk[@name='$perkName']`"><level_requirements level=`"$lvl`">$(ReqMetier $m)</level_requirements></append>")
            }
        }
    }
}
[void]$p.AppendLine('</configs>')

# ---------------------------------------------------------------- items.xml + gameevents.xml (magazines)
$it = New-Object System.Text.StringBuilder
$ge = New-Object System.Text.StringBuilder
[void]$it.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
[void]$it.AppendLine('<!-- FICHIER GENERE par tools/Generate-Metiers.ps1 - ne pas modifier a la main. -->')
[void]$it.AppendLine('<configs>')
[void]$ge.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
[void]$ge.AppendLine('<!-- FICHIER GENERE par tools/Generate-Metiers.ps1 - ne pas modifier a la main. -->')
[void]$ge.AppendLine('<configs>')
[void]$ge.AppendLine("`t<append xpath=`"/gameevents`">")

$craftToMetier = @{}
foreach ($m in $metiers.Keys) { foreach ($c in $metiers[$m].crafting) { $craftToMetier[$c] = $m } }
$report = @()
foreach ($mag in $items.SelectNodes("//item[effect_group/triggered_effect[@action='AddProgressionLevel' and starts-with(@progression_name,'crafting')]]")) {
    $name = $mag.GetAttribute("name")
    if ($name -like "admin*") { continue }
    $skills = @($mag.SelectNodes("effect_group/triggered_effect[@action='AddProgressionLevel']") | ForEach-Object { $_.GetAttribute("progression_name") } | Select-Object -Unique)
    if ($skills.Count -ne 1) { continue }
    $skill = $skills[0]
    if (-not $craftToMetier.ContainsKey($skill)) { $report += "{0,-32} {1,-24} universel" -f $name, $skill; continue }
    $m = $craftToMetier[$skill]
    $cap = Get-Cap $skill
    $report += "{0,-32} {1,-24} {2,-11} hors métier : niveau {3} max" -f $name, $skill, $m, $cap
    $grp = "/items/item[@name='$name']/effect_group[triggered_effect[@action='AddProgressionLevel']]"
    # 1) refus (évalué AVANT l'apprentissage) : pas le métier et déjà au plafond -> message + magazine rendu
    [void]$it.AppendLine("`t<insertBefore xpath=`"$grp`">")
    [void]$it.AppendLine("`t`t<effect_group tiered=`"false`">")
    [void]$it.AppendLine("`t`t`t<requirement name=`"ProgressionLevel`" progression_name=`"$(PerkId $m)`" operation=`"Equals`" value=`"0`"/>")
    [void]$it.AppendLine("`t`t`t<requirement name=`"ProgressionLevel`" progression_name=`"$skill`" operation=`"GTE`" value=`"$cap`"/>")
    [void]$it.AppendLine("`t`t`t<triggered_effect trigger=`"onSelfPrimaryActionEnd`" action=`"ShowToolbeltMessage`" message_key=`"twdMagRefus$m`"/>")
    [void]$it.AppendLine("`t`t`t<triggered_effect trigger=`"onSelfPrimaryActionEnd`" action=`"CallGameEvent`" event=`"twd_refund_$name`"/>")
    [void]$it.AppendLine("`t`t</effect_group>")
    [void]$it.AppendLine("`t</insertBefore>")
    # 2) apprentissage : métier OU encore sous le plafond
    [void]$it.AppendLine("`t<insertBefore xpath=`"$grp/triggered_effect[1]`">")
    [void]$it.AppendLine("`t`t<requirement_group op=`"or`">")
    [void]$it.AppendLine("`t`t`t<requirement name=`"ProgressionLevel`" progression_name=`"$(PerkId $m)`" operation=`"GTE`" value=`"1`"/>")
    [void]$it.AppendLine("`t`t`t<requirement name=`"ProgressionLevel`" progression_name=`"$skill`" operation=`"LT`" value=`"$cap`"/>")
    [void]$it.AppendLine("`t`t</requirement_group>")
    [void]$it.AppendLine("`t</insertBefore>")
    # Remboursement
    [void]$ge.AppendLine("`t`t<action_sequence name=`"twd_refund_$name`"><property name=`"allow_user_trigger`" value=`"false`"/><property name=`"action_type`" value=`"Game`"/><action class=`"AddItems`"><property name=`"added_items`" value=`"$name`"/><property name=`"added_item_counts`" value=`"1`"/></action></action_sequence>")
}

# Kit chirurgical (Médecin)
[void]$it.AppendLine(@'

	<!-- KIT CHIRURGICAL : réservé au Médecin. Amputation à -15 PV, hémorragie courte. -->
	<append xpath="/items">
		<item name="medicalTWDSurgicalKit">
			<property name="Tags" value="medical,medicalSkill"/>
			<property name="DisplayType" value="medical"/>
			<property name="HoldType" value="64"/>
			<property name="Meshfile" value="@:Other/Items/Health/bandage.FBX"/>
			<property name="DropMeshfile" value="@:Other/Items/Misc/sack_droppedPrefab.prefab"/>
			<property name="Material" value="Mcloth"/>
			<property name="Stacknumber" value="3"/>
			<property name="Weight" value="20"/>
			<property name="EconomicValue" value="150"/>
			<property name="Group" value="Science,Medical,TCMedical"/>
			<property name="DescriptionKey" value="medicalTWDSurgicalKitDesc"/>
			<property name="UnlockedBy" value="perkTWDMetierMedecin"/>
			<property name="SoundPickup" value="bandage_grab"/>
			<property name="SoundPlace" value="bandage_place"/>
			<property name="CustomIcon" value="medicalFirstAidKit"/>
			<property name="CustomIconTint" value="200,60,60"/>
			<property class="Action0">
				<property name="Class" value="Eat"/>
				<property name="Delay" value="5"/>
				<property name="Sound_start" value="player_bandage"/>
			</property>
			<property class="Action1">
				<property name="Class" value="UseOther"/>
				<property name="Delay" value="5"/>
				<property name="Sound_start" value="player_bandage"/>
			</property>
			<effect_group tiered="false">
				<triggered_effect trigger="onSelfPrimaryActionEnd" action="AddBuff" buff="buffTWDSurgeryAttempt"/>
				<triggered_effect trigger="onSelfSecondaryActionEnd" action="AddBuff" target="other" buff="buffTWDSurgeryAttempt"/>
			</effect_group>
		</item>
	</append>
</configs>
'@)
# Choix du métier offert : 1 point de compétence (le message indique l'onglet Métier de la barre du haut)
[void]$ge.AppendLine("`t`t<action_sequence name=`"twd_metier_intro`">")
[void]$ge.AppendLine("`t`t`t<property name=`"allow_user_trigger`" value=`"false`"/>")
[void]$ge.AppendLine("`t`t`t<property name=`"action_type`" value=`"Game`"/>")
[void]$ge.AppendLine("`t`t`t<action class=`"AddSkillPoints`"><property name=`"skill_points`" value=`"1`"/></action>")
# (pas d'ouverture automatique : l'action ShowWindow ouvre mal les fenêtres de la barre du haut, vérifié le 17/11/2026)
[void]$ge.AppendLine("`t`t</action_sequence>")
[void]$ge.AppendLine("`t</append>")
[void]$ge.AppendLine('</configs>')

# Nouveau survivant (1re arrivée ou après une mort définitive) : le marqueur twdMetierIntro repasse à 0 à la mort
$ec = @'
<?xml version="1.0" encoding="UTF-8"?>
<!-- FICHIER GENERE par tools/Generate-Metiers.ps1 - ne pas modifier a la main. -->
<configs>
	<append xpath="/entity_classes/entity_class[@name='playerMale']/effect_group[triggered_effect[@trigger='onSelfRespawn']]">
		<triggered_effect trigger="onSelfDied" action="ModifyCVar" cvar="twdMetierIntro" operation="set" value="0"/>
	</append>
</configs>
'@

# ---------------------------------------------------------------- fichiers statiques
$buffs = @'
<?xml version="1.0" encoding="UTF-8"?>
<!-- FICHIER GENERE par tools/Generate-Metiers.ps1 - ne pas modifier a la main. -->
<configs>
	<!-- Amputation au kit chirurgical (Médecin) : même logique que buffTWDAmputationAttempt (TWD_Core),
	     mais -15 PV et une hémorragie courte. -->
	<append xpath="/buffs">
		<buff name="buffTWDSurgeryAttempt" name_key="buffTWDAmputationAttemptName" hidden="true">
			<stack_type value="ignore"/>
			<duration value=".1"/>
			<effect_group>
				<requirement name="!HasBuff" buff="buffInfectionMain"/>
				<triggered_effect trigger="onSelfBuffStart" action="ShowToolbeltMessage" message_key="twdAmputationNotInfected"/>
			</effect_group>
			<effect_group>
				<requirement name="HasBuff" buff="buffInfectionMain"/>
				<triggered_effect trigger="onSelfBuffStart" action="ModifyStats" stat="Health" operation="subtract" value="15"/>
				<triggered_effect trigger="onSelfBuffStart" action="AddBuff" buff="buffTWDHemorrhageLight"/>
				<triggered_effect trigger="onSelfBuffStart" action="AddBuff" buff="buffTWDAmputated"/>
			</effect_group>
			<effect_group>
				<requirement name="HasBuff" buff="buffInfectionMain"/>
				<requirement name="CVarCompare" cvar="infectionCounter" operation="LT" value="@$infectionStage2Duration"/>
				<triggered_effect trigger="onSelfBuffStart" action="ShowToolbeltMessage" message_key="twdSurgerySuccess"/>
				<triggered_effect trigger="onSelfBuffStart" action="RemoveBuff" buff="buffInfectionMain"/>
			</effect_group>
			<effect_group>
				<requirement name="HasBuff" buff="buffInfectionMain"/>
				<requirement name="CVarCompare" cvar="infectionCounter" operation="GTE" value="@$infectionStage2Duration"/>
				<triggered_effect trigger="onSelfBuffStart" action="ShowToolbeltMessage" message_key="twdAmputationTooLate"/>
			</effect_group>
		</buff>

		<buff name="buffTWDHemorrhageLight" name_key="buffTWDHemorrhageName" description_key="buffTWDHemorrhageDesc" icon="ui_game_symbol_critical" icon_color="255,128,0">
			<damage_type value="bloodloss"/>
			<damage_source value="Internal"/>
			<stack_type value="replace"/>
			<duration value="45"/>
			<display_value value="duration"/>
			<display_value_format value="time"/>
			<effect_group>
				<passive_effect name="HealthChangeOT" operation="base_subtract" value="0.2"/>
				<triggered_effect trigger="onSelfDied" action="RemoveBuff" buff="buffTWDHemorrhageLight"/>
			</effect_group>
		</buff>
	</append>
</configs>
'@

# Exclusivité des métiers : sur le buff permanent du joueur (buffStatusCheck01), calcule pour chaque métier M
# le marqueur twdMetierBlockM = 1 si un AUTRE métier est déjà pris, sinon 0.
$ex = New-Object System.Text.StringBuilder
[void]$ex.AppendLine("`t<!-- Exclusivité des métiers : twdMetierBlock<M> = 1 si un autre métier est déjà pris -->")
[void]$ex.AppendLine("`t<append xpath=`"/buffs/buff[@name='buffStatusCheck01']`">")
foreach ($m in $metiers.Keys) {
    [void]$ex.AppendLine("`t`t<effect_group><triggered_effect trigger=`"onSelfBuffUpdate`" action=`"ModifyCVar`" cvar=`"twdMetierBlock$m`" operation=`"set`" value=`"0`"/></effect_group>")
    [void]$ex.AppendLine("`t`t<effect_group>")
    [void]$ex.AppendLine("`t`t`t<requirement_group op=`"or`">")
    foreach ($o in $metiers.Keys) {
        if ($o -ne $m) { [void]$ex.AppendLine("`t`t`t`t<requirement name=`"ProgressionLevel`" progression_name=`"$(PerkId $o)`" operation=`"GTE`" value=`"1`"/>") }
    }
    [void]$ex.AppendLine("`t`t`t</requirement_group>")
    [void]$ex.AppendLine("`t`t`t<triggered_effect trigger=`"onSelfBuffUpdate`" action=`"ModifyCVar`" cvar=`"twdMetierBlock$m`" operation=`"set`" value=`"1`"/>")
    [void]$ex.AppendLine("`t`t</effect_group>")
    # Icône du métier dans le HUD (colonne d'état) : ajoutée dès que le métier est pris
    [void]$ex.AppendLine("`t`t<effect_group>")
    [void]$ex.AppendLine("`t`t`t<requirement name=`"ProgressionLevel`" progression_name=`"$(PerkId $m)`" operation=`"GTE`" value=`"1`"/>")
    [void]$ex.AppendLine("`t`t`t<requirement name=`"!HasBuff`" buff=`"buffTWDMetier$m`"/>")
    [void]$ex.AppendLine("`t`t`t<triggered_effect trigger=`"onSelfBuffUpdate`" action=`"AddBuff`" buff=`"buffTWDMetier$m`"/>")
    [void]$ex.AppendLine("`t`t</effect_group>")
}
# Métier offert au nouveau survivant : une seule fois par vie (twdMetierIntro), seulement s'il n'a encore aucun métier
[void]$ex.AppendLine("`t`t<effect_group>")
[void]$ex.AppendLine("`t`t`t<requirement name=`"CVarCompare`" cvar=`"twdMetierIntro`" operation=`"Equals`" value=`"0`"/>")
foreach ($m in $metiers.Keys) {
    [void]$ex.AppendLine("`t`t`t<requirement name=`"ProgressionLevel`" progression_name=`"$(PerkId $m)`" operation=`"Equals`" value=`"0`"/>")
}
[void]$ex.AppendLine("`t`t`t<triggered_effect trigger=`"onSelfBuffUpdate`" action=`"ModifyCVar`" cvar=`"twdMetierIntro`" operation=`"set`" value=`"1`"/>")
[void]$ex.AppendLine("`t`t`t<triggered_effect trigger=`"onSelfBuffUpdate`" action=`"CallGameEvent`" event=`"twd_metier_intro`"/>")
[void]$ex.AppendLine("`t`t`t<triggered_effect trigger=`"onSelfBuffUpdate`" action=`"ShowToolbeltMessage`" message_key=`"twdMetierIntroMsg`"/>")
[void]$ex.AppendLine("`t`t</effect_group>")
[void]$ex.AppendLine("`t</append>")
# Buffs d'affichage du métier (aucun effet) : se retirent d'eux-mêmes si le métier est perdu (mort définitive)
[void]$ex.AppendLine("`t<append xpath=`"/buffs`">")
foreach ($m in $metiers.Keys) {
    [void]$ex.AppendLine("`t`t<buff name=`"buffTWDMetier$m`" name_key=`"$(PerkId $m)Name`" description_key=`"$(PerkId $m)Rank1LongDesc`" icon=`"$($metiers[$m].icon)`" icon_color=`"210,190,140`">")
    [void]$ex.AppendLine("`t`t`t<stack_type value=`"ignore`"/><duration value=`"0`"/>")
    [void]$ex.AppendLine("`t`t`t<effect_group>")
    [void]$ex.AppendLine("`t`t`t`t<requirement name=`"ProgressionLevel`" progression_name=`"$(PerkId $m)`" operation=`"Equals`" value=`"0`"/>")
    [void]$ex.AppendLine("`t`t`t`t<triggered_effect trigger=`"onSelfBuffUpdate`" action=`"RemoveBuff`" buff=`"buffTWDMetier$m`"/>")
    [void]$ex.AppendLine("`t`t`t</effect_group>")
    [void]$ex.AppendLine("`t`t</buff>")
}
[void]$ex.AppendLine("`t</append>")
$buffs = $buffs.Replace("</configs>", $ex.ToString() + "</configs>")

$recipes = @'
<?xml version="1.0" encoding="UTF-8"?>
<!-- FICHIER GENERE par tools/Generate-Metiers.ps1 - ne pas modifier a la main. -->
<configs>
	<append xpath="/recipes">
		<!-- Kit chirurgical : Médecin uniquement (débloqué par la compétence de métier) -->
		<recipe name="medicalTWDSurgicalKit" count="1" craft_time="20" tags="learnable">
			<ingredient name="medicalBandage" count="2"/>
			<ingredient name="resourceCloth" count="2"/>
			<ingredient name="resourceForgedIron" count="2"/>
			<ingredient name="resourceWood" count="1"/>
		</recipe>
	</append>
</configs>
'@

$loot = @'
<?xml version="1.0" encoding="UTF-8"?>
<!-- FICHIER GENERE par tools/Generate-Metiers.ps1 - ne pas modifier a la main. -->
<configs>
	<!-- On ne change de métier qu'en mourant : plus d'Elixir de l'oubli (remise à zéro des compétences) -->
	<remove xpath="/lootcontainers//item[@name='drinkJarGrandpasForgettingElixir']"/>
</configs>
'@

# Localisation
$loc = New-Object System.Text.StringBuilder
[void]$loc.AppendLine("Key,File,Type,UsedInMainMenu,NoTranslate,english,french")
function Csv([string]$s) { '"' + $s.Replace('"', '""') + '"' }
foreach ($m in $metiers.Keys) {
    $d = $metiers[$m]
    [void]$loc.AppendLine("$(PerkId $m)Name,progression,Perk,,,$(Csv ('Trade: ' + $d.en)),$(Csv ('Métier : ' + $d.fr))")
    [void]$loc.AppendLine("$(PerkId $m)Desc,progression,Perk,,,$(Csv ($d.descEn + '\nOne trade per survivor: choosing it locks the others until death.')),$(Csv ($d.descFr + '\nUn seul métier par survivant (1 point) : le choisir bloque les autres jusqu''à la mort.'))")
    [void]$loc.AppendLine("twdReqMetier$m,progression,Perk,,,$(Csv ('Requires trade: ' + $d.en)),$(Csv ('Requiert le métier : ' + $d.fr))")
    [void]$loc.AppendLine("$(PerkId $m)Rank1Desc,progression,Perk,,,$(Csv $d.en),$(Csv $d.fr)")
    [void]$loc.AppendLine("$(PerkId $m)Rank1LongDesc,progression,Perk,,,$(Csv $d.descEn),$(Csv $d.descFr)")
    [void]$loc.AppendLine("twdMagRefus$m,items,Item,,,$(Csv ('Only a ' + $d.en + ' can learn more from this. The magazine is returned.')),$(Csv ('Seul un ' + $d.fr + ' peut en apprendre davantage. Le magazine vous est rendu.'))")
}
[void]$loc.AppendLine("attTWDMetiersName,progression,Attribute,,,Trades,Métiers")
[void]$loc.AppendLine("attTWDMetiersDesc,progression,Attribute,,,$(Csv 'Choose ONE trade (1 point). Expert in your trade, beginner elsewhere: other skills are capped at level 2. You only change trade by dying.'),$(Csv 'Choisissez UN métier (1 point). Expert dans votre métier, débutant ailleurs : les autres compétences sont limitées au niveau 2. On ne change de métier qu''en mourant.')")
[void]$loc.AppendLine("atttwdmetiers,progression,Attribute,,,Trades,Métiers")
[void]$loc.AppendLine("twdMetierIntroMsg,buffs,Buff,,,$(Csv 'Choose your trade: a free skill point has been given to you (Skills menu > Trades tab).'),$(Csv 'Choisissez votre métier : un point de compétence vous est offert (Compétences > onglet Métiers).')")
[void]$loc.AppendLine("twdReqPasDeMetier,progression,Perk,,,You already have a trade,Vous avez déjà un métier")
[void]$loc.AppendLine("medicalTWDSurgicalKit,items,Item,,,Surgical Kit,Kit chirurgical")
[void]$loc.AppendLine("medicalTWDSurgicalKitDesc,items,Item,,,$(Csv 'A clean amputation by a trained hand: only 15 HP lost and a short bleed. Stage 1 infection only.\nPrimary action: yourself. Secondary action: another player.'),$(Csv 'Une amputation propre, par une main expérimentée : seulement 15 PV perdus et une courte hémorragie. Infection au stade 1 uniquement.\nAction principale : sur soi. Action secondaire : sur un autre joueur.')")
[void]$loc.AppendLine("twdSurgerySuccess,buffs,Buff,,,Clean cut. The infection has stopped.,Coupe nette. L'infection est stoppée.")

$modInfo = @'
<?xml version="1.0" encoding="UTF-8" ?>
<xml>
	<Name value="TWD_Metiers" />
	<DisplayName value="TWD Métiers" />
	<Description value="Système de métiers : 6 métiers exclusifs, compétences et magazines réservés. Généré par tools/Generate-Metiers.ps1." />
	<Author value="TWD Server" />
	<Version value="0.1.0" />
	<Website value="" />
</xml>
'@

# ---------------------------------------------------------------- écriture
$enc = New-Object System.Text.UTF8Encoding($false)
$conf = Join-Path $OutDir "Config"
New-Item -ItemType Directory -Force $conf | Out-Null
[IO.File]::WriteAllText((Join-Path $OutDir "ModInfo.xml"), $modInfo, $enc)
[IO.File]::WriteAllText((Join-Path $conf "progression.xml"), $p.ToString(), $enc)
[IO.File]::WriteAllText((Join-Path $conf "items.xml"), $it.ToString(), $enc)
[IO.File]::WriteAllText((Join-Path $conf "gameevents.xml"), $ge.ToString(), $enc)
[IO.File]::WriteAllText((Join-Path $conf "buffs.xml"), $buffs, $enc)
[IO.File]::WriteAllText((Join-Path $conf "recipes.xml"), $recipes, $enc)
[IO.File]::WriteAllText((Join-Path $conf "loot.xml"), $loot, $enc)
[IO.File]::WriteAllText((Join-Path $conf "entityclasses.xml"), $ec, $enc)
[IO.File]::WriteAllText((Join-Path $conf "Localization.csv"), $loc.ToString(), $enc)

$report
Write-Host "$($metiers.Count) métiers, $perkCount compétences réservées, $(@($report | ? { $_ -notmatch 'universel' }).Count) magazines réservés -> $OutDir"
