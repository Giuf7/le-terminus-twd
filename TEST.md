# Plan de test du serveur TWD

Chaque test indique **quoi taper**, **ce qu'on doit observer**, et une case à cocher.
Les commandes se tapent dans la **console du jeu (touche F1)**, sauf mention « telnet ».
Toutes les commandes listées ont été vérifiées sur le serveur V 3.4.0.

---

## 0. Préparation (une seule fois)

1. Installer les mods **côté serveur** (déjà fait sur `E:\game`) : `TWD_Core`, `WalkerSim2`, `TWD_Zones`.
2. Lancer le serveur de test **avec EAC désactivé** :
   ```
   cd E:\game
   7DaysToDieServer.exe -quit -batchmode -nographics -configfile=serverconfig.test.xml -dedicated -logfile twd_test.log
   ```
   (Une fois le Sandbox Code obtenu, utiliser plutôt `serverconfig.twd.xml`.)
3. Dans le jeu : **Rejoindre une partie → Se connecter par IP** → `127.0.0.1`, port `26900`.
   - Le serveur tourne **sans EAC**. Si le jeu refuse la connexion, relancer le jeu en désactivant EasyAntiCheat dans son lanceur (à confirmer au premier essai).
4. Se donner les droits admin (**telnet** sur `127.0.0.1:8081`, ou console du serveur) :
   ```
   admin add <ton pseudo> 0
   ```
5. En jeu : `dm` (menu de debug : F3 / vol / invincibilité si besoin) et `cm` (menu créatif, pour récupérer des objets).

> Astuce : `listitems <mot>` (ou `li`) retrouve le nom exact d'un objet. `kill <id>` / `killall` nettoient la zone.
> Les `<id>` de joueurs/entités s'affichent avec `listplayers` (`lp`).

---

## 1. Rôdeurs

| ✔ | Test | Commande | Résultat attendu |
|---|---|---|---|
| [ ] | Vitesse de jour | `settime 1 12 00` puis attirer un rôdeur | Il **marche** |
| [ ] | Vitesse de nuit | `settime 1 23 00` | Il **marche** encore (si sprint → le Sandbox Code n'est pas appliqué) |
| [ ] | Headshot only (mêlée) | `se <ton id> zombieJoe`, frapper le **corps** au couteau 10 fois | Il ne meurt **pas** |
| [ ] | Headshot only (tête) | Frapper la **tête** | Il meurt |
| [ ] | Headshot only (arme à feu) | `giveself gunHandgunT1Pistol`, `giveself ammo9mmBulletBall 1 50 true` ; tirer au corps puis à la tête | Corps : pas de mort ; tête : mort |
| [ ] | Pas de spéciaux | `spawnscouts` (horde de « hurleuses ») | Aucune hurleuse : des rôdeuses normales |
| [ ] | Pas de spéciaux (bis) | `spawnwandering` plusieurs fois | Aucun chien, vautour, araignée, flic cracheur, démolisseur, féral (yeux brillants), irradié (vert) |
| [ ] | Difficulté fixe | Comparer un même bâtiment au jour 1 et `settime 30 12 00` (jour 30) | Même nombre de dormeurs |

## 2. Morsure et infection

| ✔ | Test | Commande | Résultat attendu |
|---|---|---|---|
| [ ] | Morsure par un rôdeur | Se laisser frapper ~10 fois par un `zombieJoe` | Icône d'infection orange (au moins une fois sur 10 coups en moyenne) |
| [ ] | S'infecter directement | `buff buffInfectionCatch` | Icône d'infection orange, stade 1 |
| [ ] | Pas de guérison naturelle | Attendre 2-3 min réelles | Le % monte, ne descend jamais |
| [ ] | Antibiotiques | `giveself drugAntibiotics`, en prendre | Icône « ralentie », le % **monte toujours** (moins vite) |
| [ ] | Textes | Survoler l'icône et l'antibiotique | Textes FR « rien ne guérit une morsure » |
| [ ] | Stade 2 | `buff twdtest_stage2` | Icône rouge ; endurance -25 % |
| [ ] | Stade 3 | `buff twdtest_stage3` | Icône rouge clignotante ; attributs -1 |
| [ ] | Mort par infection | `buff twdtest_dying` | Mort en ~30 s |

## 3. Signes de l'infection (2 joueurs)

| ✔ | Test | Comment | Résultat attendu |
|---|---|---|---|
| [ ] | Stade 1 caché | Joueur A : `buff buffInfectionCatch` ; B reste à côté 2 min | B n'entend **rien** |
| [ ] | Stade 2 audible | A : `buff twdtest_stage2` | B entend un gémissement environ toutes les minutes |
| [ ] | Stade 3 audible | A : `buff twdtest_stage3` | B entend gémissements fréquents + vomissements |
| [ ] | Le vomi attire | A au stade 3 près d'un rôdeur qui ne l'a pas vu | Le rôdeur se tourne vers A |
| [ ] | Fréquence | Chronométrer | Si trop/pas assez : régler les `value` des `RandomRoll` dans `TWD_Core/Config/buffs.xml` (« twd signes stade ») |

## 4. Amputation

| ✔ | Test | Commande | Résultat attendu |
|---|---|---|---|
| [ ] | Fabrication | `giveself resourceCloth 1 4 true`, `giveself resourceWood`, `giveself resourceScrapIron 1 3 true` ; fabriquer dans le sac | « Kit d'amputation » fabricable |
| [ ] | Pas infecté | Utiliser le kit sans infection | Message « Il n'y a aucune infection à stopper », kit consommé, rien d'autre |
| [ ] | Stade 1 → succès | `buff buffInfectionCatch`, utiliser le kit | Message « L'infection est stoppée », -40 PV, hémorragie, icône **Amputé**, plus d'infection |
| [ ] | Hémorragie | Attendre sans soin | Perte de PV ~90 s |
| [ ] | Bandage | Utiliser un bandage pendant l'hémorragie | Hémorragie stoppée |
| [ ] | Malus | Courir, porter, viser | Plus lent, moins d'endurance, inventaire réduit, visée moins précise |
| [ ] | Stade 2 → trop tard | `buff buffInfectionCatch`, `buff twdtest_stage2`, kit | Message « Trop tard… », amputé **et** toujours infecté |
| [ ] | Sur un autre joueur (**2 joueurs**) | A infecté (`buff buffInfectionCatch`) ; B vise A et fait **clic droit** avec le kit | A : infection stoppée, -40 PV, Amputé. **Point le plus incertain** |
| [ ] | Amputé + mort | Mourir amputé | Au retour, plus de malus Amputé |

## 5. Mort, kits, mort définitive

| ✔ | Test | Commande | Résultat attendu |
|---|---|---|---|
| [ ] | Kit J1 | Se connecter avec un **nouveau** personnage | Couteau, batte, torche, 3 conserves, 3 eaux, 4 bandages, 1 bandage de premiers soins (pas de note de Duke) |
| [ ] | Rôdeur sur le cadavre | `kill <ton id>` (ou chute) | Un rôdeur apparaît à l'endroit de la mort |
| [ ] | Perte du sac | Après la mort | Tout l'inventaire est sur le cadavre |
| [ ] | Kit réduit | À la réapparition | **Seulement** couteau + 1 bandage (pas le kit J1 complet) |
| [ ] | Mort définitive (Sandbox) | `giveselfxp 50000`, mourir | Retour niveau 1, compétences remises à zéro |
| [ ] | Base conservée | Poser un bloc de revendication, mourir | Le bloc est toujours là et protège |

## 6. Armes, ressources, compétences

| ✔ | Test | Commande | Résultat attendu |
|---|---|---|---|
| [ ] | Arbalète tôt | Menu compétences → Fabrication → Arcs | Arbalète au **niveau 5** ; composite au **30** |
| [ ] | Tirs bruyants | Tirer au fusil (`giveself gunRifleT1HuntingRifle`) près de rôdeurs éloignés | Ils arrivent de loin ; avec silencieux, beaucoup moins |
| [ ] | Pas de Dukes | Démonter une caisse enregistreuse, fouiller des coffres | Aucun Duke |
| [ ] | Compétences retirées | Menu compétences : Maîtrises (5), Siphoning Strikes, Turrets | « Désactivée sur ce serveur », impossible à acheter |
| [ ] | Pas de marchands | Explorer la carte générée | Aucun avant-poste de marchand |
| [ ] | Carte / boussole (Sandbox) | Touche M, haut de l'écran | Ni carte, ni boussole, ni marqueurs |

## 6 bis. Métiers

Rappel : un nouveau personnage par test de métier (ou mourir, la mort définitive remet tout à zéro).

| ✔ | Test | Commande | Résultat attendu |
|---|---|---|---|
| [ ] | Les 6 métiers | Menu compétences → compétences générales | 6 « Métier : … », gratuits |
| [ ] | Exclusivité | Prendre « Métier : Chasseur », puis essayer « Métier : Soldat » | Soldat refusé (« Vous avez déjà un métier ») |
| [ ] | Niveau 5 dans le métier | `giveselfxp 200000`, monter Tir à l'arc | Jusqu'au niveau 5 |
| [ ] | Niveau 2 hors métier | Monter Œil de lynx (Soldat) | Bloqué au niveau 2 (« Requiert le métier : Soldat ») |
| [ ] | Compétence universelle | Monter Mule de bât | Jusqu'au niveau 5 |
| [ ] | Magazine du métier | Chasseur : `giveself bowsSkillMagazine 1 30 true`, en lire plusieurs | Fabrication d'arcs monte sans limite, arbalète au niveau 5 |
| [ ] | Magazine hors métier | Chasseur : `giveself riflesSkillMagazine 1 15 true`, en lire 12 | Fusils monte jusqu'au niveau 10, puis message « Seul un Soldat… » et **le magazine est rendu** |
| [ ] | Médicaments | Non-médecin : `giveself medicalSkillMagazine 1 25 true`, tout lire | Bloqué au niveau 19 : pas d'antibiotiques ni de trousse de soins |
| [ ] | Kit chirurgical | Médecin : chercher « Kit chirurgical » dans le sac | Fabricable (2 bandages, 2 tissus, 2 fer forgé, 1 bois) ; invisible pour les autres métiers |
| [ ] | Amputation chirurgicale | Médecin : `buff buffInfectionCatch`, utiliser le kit | « Coupe nette… », -15 PV seulement, hémorragie courte, Amputé |
| [ ] | Élixir de l'oubli | Fouiller les coffres renforcés | Plus jamais d'Élixir de l'oubli |

## 6 ter. Véhicules

| ✔ | Test | Commande | Résultat attendu |
|---|---|---|---|
| [ ] | Moteur bruyant | `giveself vehicleMinibikePlaceable` (ou `cm`), rouler près de rôdeurs qui ne vous ont pas vu | Ils se tournent et suivent le bruit |
| [ ] | Vélo silencieux | `giveself vehicleBicyclePlaceable`, même test | Ils ne réagissent pas au vélo |
| [ ] | Hordes attirées | Rouler plusieurs minutes, s'arrêter, attendre | Des rôdeurs convergent (WalkerSim) |
| [ ] | Essence rare | Fouiller des voitures, siphonner une épave | Peu d'essence (siphonnage 15-60) |
| [ ] | Consommation (Sandbox) | Rouler avec un plein | Le réservoir se vide nettement plus vite qu'en vanilla |
| [ ] | Essence réservée | Non-mécanicien : chercher la recette d'essence au labo de chimie | Non disponible |
| [ ] | Pas de gyrocoptère | Compétences → Fabrication → Véhicules | Le gyrocoptère n'apparaît plus |
| [ ] | Dégâts de collision (Sandbox) | Foncer dans un groupe de rôdeurs | Le véhicule perd beaucoup de points de vie |

## 6 quater. Véhicules abandonnés (zones PvP, 2 joueurs dont un Mécanicien)

Préparation : `twdzones addcircle Test 150` (zone PvP autour de vous), puis s'éloigner à plus de 80 m du point où doit apparaître le véhicule, ou `twdvehicles spawn Test`.

| ✔ | Test | Commande | Résultat attendu |
|---|---|---|---|
| [ ] | Apparition | Attendre 30 s (ou `twdvehicles spawn Test`), puis `twdvehicles list` | Un véhicule « abandonné, EN PANNE » dans la zone, jamais à moins de 80 m d'un joueur |
| [ ] | État | Aller le voir | Abîmé (~30 % de santé), presque à sec |
| [ ] | En panne | Non-mécanicien : essayer de conduire / ouvrir le coffre / ramasser | Refusé : « seul un Mécanicien peut le remettre en route » |
| [ ] | Remise en route | Mécanicien : interagir avec le véhicule | « Vous remettez le véhicule en route » ; ensuite conduisible par tous |
| [ ] | Réapparition | Sortir le véhicule de la zone, `twdvehicles list` | « prochain véhicule au jour J+3 » |
| [ ] | Limite de faction | Une faction avec 3 véhicules à moteur pose un 4e véhicule fabriqué | Refusé, le véhicule est posé à ses pieds dans son emballage |
| [ ] | Vélos | Même faction : poser un vélo | Autorisé |
| [ ] | Persistance | Redémarrer le serveur, `twdvehicles list` | Les véhicules suivis et les délais sont conservés |

## 7. WalkerSim (hordes)

| ✔ | Test | Comment | Résultat attendu |
|---|---|---|---|
| [ ] | Chargement | Journal du serveur | `[WalkerSim] Started simulation.` et `Agents: ~14800` sur la carte 8192 |
| [ ] | Troupeaux | Longer une route principale de jour (`dm` → vol pour aller vite) | Groupes de rôdeurs en marche le long des routes |
| [ ] | Bruit | Tirer plusieurs coups, s'éloigner, revenir 5 min plus tard | Des rôdeurs se sont rassemblés à l'endroit du bruit |

## 8. Zones PvP (2 joueurs, admin)

Préparation : `twdzones addcircle Test 30` (zone PvP de 30 m autour de toi), puis `twdzones list`.

| ✔ | Test | Comment | Résultat attendu |
|---|---|---|---|
| [ ] | Hors zone | A et B hors de la zone, A frappe B | B ne perd **aucun PV** ; A voit « Zone sûre… » |
| [ ] | Dans la zone | A et B dans la zone | B perd des PV |
| [ ] | Un seul dedans | A dedans, B dehors (et l'inverse) | Pas de dégâts |
| [ ] | Zone sûre | `twdzones addcircle Marche 8 safe` au centre de Test | Pas de dégâts dans le marché, dégâts autour |
| [ ] | Messages | Entrer / sortir de la zone | « ZONE PvP : Test » / « Vous quittez la zone PvP » |
| [ ] | Armes à feu et explosifs | Pistolet, grenade (`giveself thrownGrenade`) hors zone | Pas de dégâts à B |
| [ ] | Effet visuel | A frappe B hors zone | Noter si A voit du sang sur B (normal, sans perte de PV) |
| [ ] | Nettoyage | `twdzones remove Test`, `twdzones remove Marche` | Zones supprimées |

---

## Si un test échoue

1. Noter le test, ce qui s'est passé, et l'heure.
2. Récupérer le journal : `E:\game\twd_test.log` (chercher `ERR`, `Exception`, `TWD_`, `WalkerSim`).
3. Vérifier les fichiers réellement chargés : `%APPDATA%\7DaysToDie\Saves\<monde>\<partie>\ConfigsDump\`.
