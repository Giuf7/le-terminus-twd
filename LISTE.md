# Serveur TWD (7 Days to Die V 3.4.0) : ce qui est fait / ce qu'il reste

_Mis à jour le 17/11/2026_

## Organisation du projet

| Emplacement | Contenu |
|---|---|
| `Mods/TWD_Core/` | Notre mod (XML), à copier dans `Mods/` du serveur |
| `Mods/WalkerSim2/` | WalkerSim 2.0.5 + notre config TWD (`WalkerSim.xml`, original dans `WalkerSim.default.xml`) |
| `Mods/TWD_Zones/` | Notre mod C# de zones PvP (`TWD_Zones.dll` + `zones.xml`) |
| `Mods/TWD_Metiers/` | Mod des métiers (XML **généré** : ne pas modifier à la main, relancer `tools/Generate-Metiers.ps1`) |
| `Mods/TWD_Vehicles/` | Notre mod C# : le bruit des moteurs attire les rôdeurs |
| `discord-bot/` | Bot Discord du Terminus (voir son `README.md`) |
| `src/TWD_Zones/`, `src/TWD_Vehicles/` | Code source C# (compiler avec `tools/Build-Mod.ps1 -Name <mod>`) |
| `serverconfig.twd.xml` | Configuration du serveur (Sandbox Code encore à remplir) |
| `SANDBOX.md` | Réglages Sandbox à faire dans le jeu pour générer le Sandbox Code |
| `tools/` | Scripts qui régénèrent certains fichiers du mod à partir du jeu (à relancer après une mise à jour du jeu) |
| `E:\game\` | Serveur dédié local de test (SteamCMD) |

---

## ✅ Fait (chargé et vérifié sur le serveur local, sans erreur)

### Rôdeurs
- [x] **Rôdeurs humains uniquement** : spéciaux (araignée, hurleuse, flic cracheur, démolisseur, mutants…), variantes (férales, irradiées, chargées, infernales) et animaux zombies remplacés par des rôdeurs normaux dans tous les groupes d'apparition (`tools/Generate-EntityGroups.ps1`) ; loup sanguinaire (yeux brillants) → loup ordinaire dans la faune, rôdeur dans les hordes ; Grace (sanglier boss) → rôdeur
- [x] **Difficulté fixe dès le jour 1** : un seul palier par type d'apparition, quels que soient le niveau et les jours survécus (`tools/Generate-Gamestages.ps1`, palier 100)
- [x] **WalkerSim 2** : ~14 800 rôdeurs simulés sur une carte 8192, population complète dès le jour 1, grands troupeaux sur les routes (groupes de 60), retour des rôdeurs par les bords de la carte, attirés par le bruit. **Testé : fonctionne sur la V 3.4** (côté serveur uniquement, EAC désactivé)

### Infection et morsure
- [x] **Morsure mortelle** : l'infection ne guérit jamais, mort en ~1,5 jour de jeu
- [x] Chaque coup de rôdeur : **15 % de chance de morsure** (en plus des blessures vanilla)
- [x] **Médicaments** (antibiotiques, miel) : ralentissent seulement (−60 %)
- [x] **Amputation** : kit fabricable (4 tissus, 1 bois, 3 ferraille), sur soi ou sur un autre joueur, sauve seulement au stade 1 (~5 h de jeu), −40 PV + hémorragie (stoppée par un bandage), malus « Amputé » jusqu'à la mort
- [x] **Signes visibles** : stade 1 caché ; stade 2 gémissements ; stade 3 gémissements fréquents + vomissements (qui attirent les rôdeurs)
- [x] Textes FR/EN des remèdes et de l'infection corrigés (« rien ne guérit une morsure »)

### Mort
- [x] **« Nous sommes tous infectés »** : un rôdeur se relève sur le corps de chaque joueur mort
- [x] **Mort définitive** préparée (option Sandbox) : la **base du groupe est conservée** (bloc de revendication gardé)
- [x] **Kit de survie J1** (couteau, batte, torche, 3 conserves, 3 eaux, 4 bandages, 1 bandage de premiers soins)
- [x] **Kit réduit à la réapparition** (couteau + bandage) ; le kit complet vanilla de la mort définitive est retiré

### Économie et compétences
- [x] **Pas de monnaie** : plus de Dukes dans le loot ni en démontant caisses et coffres ; la recette qui en demandait n'en demande plus. Le commerce se fait par troc
- [x] **Pas de super-pouvoirs** : les 5 Maîtrises (immunité à l'infection, esquive du premier coup, +100 PV…), Siphoning Strikes (vol de vie) et Turrets (robots) sont désactivées et non achetables
- [x] **Carte et boussole désactivées** (dans `SANDBOX.md`)

### Outils de test
- [x] `TEST.md` : plan de test complet avec les commandes console (vérifiées sur la V 3.4)
- [x] Buffs de test admin : `twdtest_stage2`, `twdtest_stage3`, `twdtest_dying` (avancer une infection en cours)

### Joueurs
- [x] **Règlement et guide du survivant** (document partageable) : https://claude.ai/artifact/EihD4S2MnJKoFWCo4iv7o4

### Armes
- [x] **Armes à feu plus bruyantes** : 23 armes, bruit ×1,5 et attraction de hordes ×3 ; silencieux inchangés (`tools/Generate-GunNoise.ps1`)
- [x] **Arbalète reine** : fabricable au niveau 5 de fabrication d'arcs (au lieu de 21), composite au 30 (au lieu de 56)

### Carte « Géorgie »
- [x] **Sans marchands** sur les cartes générées
- [x] Villes possibles en forêt ; 1 grande ville « Atlanta » garantie, 2-4 villes, beaucoup de villages, fermes, arrêts de bord de route
- [x] Pas de cratères, canyons ni ville western
- [x] **Lieux garantis** : prison, hôpital, base militaire ; fermes plus fréquentes

### Zones PvP (mod C# `TWD_Zones`, côté serveur uniquement)
- [x] **PvE partout, PvP seulement dans les zones** : les dégâts joueur → joueur sont bloqués par le serveur, sauf si l'attaquant ET la victime sont dans une zone PvP (mêlée, armes, explosions et pièges)
- [x] Zones **cercle** ou **rectangle** ; zones **sûres** prioritaires (ex. un marché neutre au milieu d'une ville PvP)
- [x] Messages à l'écran en entrant / sortant d'une zone PvP, et avertissement de l'attaquant en zone sûre
- [x] Commande console admin `twdzones` (alias `tz`), testée : `list`, `here`, `addcircle <nom> <rayon> [pvp|safe]`, `corner1` / `corner2` + `addrect <nom> [pvp|safe]`, `remove <nom>`, `default <pvp|safe>`, `reload`
- [x] Compilé avec le compilateur C# fourni par Windows (pas besoin d'installer de SDK) ; chargé sans erreur sur la V 3.4

### Configuration
- [x] `serverconfig.twd.xml` : RWG 8192, Europe/French, PvP global (3), 1 bloc de revendication, 100 zombies max, **EAC désactivé** (requis par WalkerSim), serveur caché (pour les tests)
- [x] `SANDBOX.md` : liste complète des réglages Sandbox

---

## ⏳ Reste à faire

### Bloqué en attendant le jeu client (en cours d'installation)
- [ ] **Générer le Sandbox Code** en suivant `SANDBOX.md`, puis le coller dans `serverconfig.twd.xml`
  - ⚠️ Le test WalkerSim montre que le serveur utilise encore la vitesse vanilla **« Sprint » la nuit** : le Sandbox Code est indispensable
- [ ] **Générer la carte Géorgie** : copier `Mods/TWD_Core` dans le dossier `Mods` du jeu, générer (8192, forêt ~80-90 %, un peu de forêt brûlée, désert/neige/wasteland ~0), vérifier la prévisualisation, copier la carte sur le serveur
- [ ] **Tests en jeu** (connexion `127.0.0.1:26900`) :
  - [ ] Headshots only (corps vs tête, mêlée et armes à feu)
  - [ ] Morsure → infection → stades → mort
  - [ ] Amputation **sur soi** et **sur un autre joueur** (le point le plus incertain)
  - [ ] Signes de l'infection **entendus par les autres joueurs** + fréquence des sons
  - [ ] Rôdeur qui se relève sur le cadavre
  - [ ] Kit J1, kit de réapparition, mort définitive (base conservée)
  - [ ] Arbalète au niveau 5
  - [ ] WalkerSim : hordes sur les routes, réaction aux coups de feu
  - [ ] Aucun spécial / féral / animal zombie ne réapparaît
  - [ ] **Zones PvP (2 joueurs nécessaires)** : PvP bloqué hors zone, autorisé dans la zone, bloqué si un seul des deux est dans la zone, zone sûre dans une zone PvP, messages d'entrée/sortie, explosifs

### Interface (mod `TWD_UI`, envoyé aux joueurs par le serveur)
- [x] Étape 1 « sobre » (à tester en jeu) : onglet **Métiers** en premier dans les compétences (attribut `attTWDMetiers`) ; HUD : icône du métier (`buffTWDMetier<M>`), icône **Zone PvP** (`buffTWDZonePvP`, posée par TWD_Zones via buffplayer/debuffplayer), stades d'infection renommés (Morsure / Propagation / Fièvre)
- [x] Nom de l'onglet (clé `atttwdmetiers` en minuscules, comme `attgeneralperks`)
- [x] Bouton **Quêtes** masqué. Essai « Quêtes → Métier » en XML seul (nouveau groupe avec XUiC_SkillWindowGroup) : ArgumentOutOfRangeException + fenêtres mal placées → annulé
- [ ] **DLL client** (dans le pack TWD_Textes, qui deviendra le pack client) : vrai onglet **Métier** dans la barre du haut + ouverture automatique à l'arrivée d'un nouveau survivant
- [x] Hordes « très éparpillées » : cohésion WalkerSim renforcée (FlockSameGroup 0.15 / 60 m, AlignSameGroup 0.2 / 60 m, marche aléatoire 0.08, évitement interne 0.05, vent ≤ 0.1 ; solitaires inchangés). ⚠️ Après toute modification de WalkerSim.xml : `walkersim restart` en console (sinon la simulation garde l'ancienne config sauvegardée et WalkerSim lève une erreur au démarrage) — à prévoir dans la procédure OVH
- [x] Journal : hémorragie « arrêtée par un soin » / « terminée d'elle-même » (90 s, 45 s pour le kit chirurgical)
- [x] Bruit des armes jugé trop faible en test : bruit **x2** (au lieu de x1,5 ; pistolet 52 → 104), chaleur x3 inchangée ; WalkerSim **SoundDistanceScale 3** (portée des sons dans la simulation) + journal des événements WalkerSim activé pour vérifier qu'il reçoit les tirs. En attente du redémarrage
- [x] **Zone Atlanta** : cercle 450 m → **rectangle (-3800, -2600) → (-2600, -1550)** = partie construite (tuiles de ville), prison sud (-3633, -2721) laissée hors zone. Appliqué à chaud (`twdzones reload`), #zones-pvp à jour
- [x] Messages à l'écran envoyés avec un son `null` non affichés (sortie de zone PvP, tous les messages des véhicules abandonnés) → son « ui_tab ». En attente du redémarrage
- [x] **Morsure 5 % : ne marchait pas en multijoueur.** Posée par l'arme du rôdeur, elle s'exécutait sur la copie SERVEUR du joueur (où `_InfectionRate`, défini par le jeu du joueur à sa réapparition, vaut 0 → infection annulée aussitôt, jamais reçue). Déplacée sur le joueur (`playerMale`, onOtherAttackedSelf, comme les critiques du jeu). Seuls les critiques infectaient jusqu'ici. À retester
- [x] Journal : les coups de rôdeur arrivent par `NetPackageDamageEntity` (calculés par le jeu du joueur), pas par EntityPlayer.DamageEntity → corrigé
- [x] **Journal d'événements TWD** (`TWD_Zones/Journal.cs` + véhicules) : coups de rôdeur, contamination (morsure / critique), stades, ralentissement, amputation (réussie / trop tard), hémorragie, morts (cause, position, survie, métier perdu), choix du métier, magazine refusé, point offert, véhicules. Sortie : journal serveur `[TWD_Journal]` + `<sauvegarde>/twd_journal.log`. À activer (redémarrage) et tester. Option : renvoi vers #logs Discord
- [x] Vérification « un coup à la tête » : les 23 rôdeurs pouvant apparaître ont 11 PV ; **armures retirées** (ouvrier 20 %, motard 20 %, soldat 50 %, démolisseur 60 %) — en attente du redémarrage
- [x] **Mort (1er test réel, 17/11)** : (1) le rôdeur ne se relevait pas (game event XML SpawnEntity « Position » sur un joueur mort : position invalide) → refait en C# (`TWD_Zones/Death.cs`, ModEvents.EntityKilled) ; (2) la **mort définitive plantait** (NullReferenceException dans `Progression.ResetProgression`, statistiques internes du jeu sur serveur dédié) → TOUTE la remise à zéro était annulée → remplacée par une version sûre (patch Harmony). À retester
- [x] Morsure : **5 %** par coup de rôdeur (au lieu de 15 %) ; les **essaims d'insectes n'infectent plus** (seuls les rôdeurs transmettent l'infection). En attente du prochain redémarrage
- [x] **Un coup à la tête tue** : santé de tous les rôdeurs fixée à 11 PV (`replace_passive_effect/property[health*]`), sans variation ±15 % ; la tête double les dégâts du joueur → toute arme ou outil tue d'un coup. **Poings : 0 dégât aux créatures** (ne tuent jamais, même à la tête). À tester. Conséquence : la compétence Bagarreur (Soldat, bonus aux poings) ne sert plus aux mains nues, seulement aux poings américains — à revoir
- [x] Rôdeurs obèses (Boe, gros Hawaïen, Big Mama) remplacés par des silhouettes normales (Joe, Moe, Marlene)
- [x] **Métier offert** au nouveau survivant (1re arrivée et après chaque mort) : +1 point + message (game event `twd_metier_intro`, marqueur `twdMetierIntro` remis à 0 à la mort). Testé : point et message OK
- [ ] Ouverture automatique de l'onglet Métier à l'arrivée : impossible en XML (ShowWindow rouvre le dernier onglet de la barre) → nécessite une petite DLL client (pack client), à faire avec la refonte
- [ ] Compétences hors métier : affichage « niveau 2 max » plus clair
- [ ] **Refonte visuelle façon TWD** (validée sur le principe) : sortir du gris terne du jeu de base ; palette sombre, rouille, kaki, papier jauni ; puis écran titre et écrans de chargement (pack client, images)

### Gameplay
- [ ] **Placer les vraies zones PvP** sur la carte définitive (en jeu avec `twdzones`) : no man's land entre factions, Atlanta, marché neutre…
- [ ] Zones PvP : afficher les zones sur la carte du jeu ? (non fait : le jeu ne le permet pas simplement côté serveur)
- [x] **Bandits** : absents du jeu de base V 3.4 → décision : **pas de bandits** (les autres joueurs en zone PvP jouent ce rôle)
- [x] **Système de métiers** (mod `TWD_Metiers`, généré par `tools/Generate-Metiers.ps1`, détail dans `METIERS.md`) : 6 métiers exclusifs, 43 compétences niveau 2 max hors métier, 23 magazines bloqués au premier palier hors métier (magazine rendu), kit chirurgical du Médecin, Élixir de l'oubli retiré, points de compétence réduits (Sandbox). Chargé sans erreur ; **à tester en jeu**
- [x] **Véhicules** : moteur qui attire les rôdeurs et les hordes (mod C# `TWD_Vehicles` + son `twd_vehicle_engine`), essence ~3x plus rare (loot + épaves), essence fabriquée par le Mécanicien seulement (via métiers), gyrocoptère retiré, consommation et dégâts de collision renforcés (Sandbox). Chargé sans erreur ; **à tester en jeu**
- [x] **Véhicules abandonnés en zone PvP** (`TWD_Vehicles`) : 1 par zone, abîmé (30 %) et presque à sec (8 %), réapparition 3 jours après sa sortie de zone ou sa destruction, limite de 3 véhicules à moteur par faction (fabriqués compris, vélos exclus), commande `twdvehicles`. **Testé en jeu le 17/11** : apparition (corrigée : le paquet NetPackageVehicleSpawn plantait sans expéditeur), prise par le premier qui monte, sortie de zone détectée
- [x] **Règle véhicules (17/11)** : **tout le monde conduit, seul le Mécanicien répare**. Montée contrôlée via NetPackageEntityAttach (le verrou ne couvre que le coffre) ; réparation par un non-Mécanicien annulée côté serveur (NetPackageVehicleDataSync : vie restaurée + kit rendu). **Réparation refusée encore à tester en jeu**
- [x] **Faim -40 %, soif -25 %** (`TWD_Core/entityclasses.xml`, consommation par point d'endurance) : nourriture rare sans marchands ni loot renouvelé. Discord à mettre à jour
- [ ] **Groupes = factions** : vérifié dans le code (V 3.4) qu'un joueur qui se déconnecte QUITTE son groupe (ConnectionManager.DisconnectClient → Party.ServerHandleDisconnectParty) et que le groupe n'est enregistré nulle part. Proposé : (1) remettre automatiquement un joueur dans son groupe à la reconnexion ; (2) délai pour changer de groupe (anti-contournement de la limite de véhicules) ; (3) nom de groupe facultatif affiché dans le chat. En attente de décision
- [x] **Sandbox Code généré automatiquement** (commande `twdsandbox` de `TWD_Zones`, réglages dans `tools/sandbox_twd.txt`, détail dans `SANDBOX.md`) : 43 réglages, vérifiés 43/43 après rechargement, inscrit dans `serverconfig.twd.xml`
- [x] **Carte Géorgie générée par le serveur** (patch `TWD_Zones/WorldGen.cs` + `Mods/TWD_Zones/worldgen.xml`) : « Old Yicesewa Valley » (graine TWD_Georgia, 8192), 100 % forêt, 3 104 lieux, Atlanta au sud-ouest, 2 prisons, 6 hôpitaux, base militaire, **0 marchand** (portes d'entrée des villes et marchands hors ville désactivés). Fichiers dans `%APPDATA%\7DaysToDie\GeneratedWorlds\Old Yicesewa Valley` (à copier sur OVH)
- [x] **Zones PvP** : Atlanta (X -3200, Z -2200, 450 m), Terminus (X -749, Z 340, 200 m), Woodbury (X -2560, Z 1980, 250 m) ; prisons hors PvP ; `#zones-pvp` et changelog 0.4 à jour
- [x] `tools/Start-LocalServer.ps1` : lance le serveur local en mode TWD complet, ou `-Vanilla` (sans mods, diagnostic)
- [x] **Pack client `TWD_Textes`** (`tools/Build-ClientPack.ps1` → `dist/TWD_Textes.zip`) : le serveur n'envoie pas les textes (Localization) au jeu ; sans ce pack, les joueurs voient des codes bruts. Installé dans le jeu de test. **À distribuer aux joueurs** (Discord) à l'ouverture
- [x] Métiers : coût 1 point (0 n'est pas géré par le jeu : « NA ») ; niveaux 1-2 des armures moyenne/lourde, Maître cuisinier et Vie à la campagne débloqués (prérequis neutre)
- [x] **Plantage du jeu client à l'apparition** : trouvé par élimination (vanilla OK → TWD_Core seul OK → +TWD_Metiers plante → progression.xml → les 7 compétences de métier). Cause : prérequis **circulaires** entre compétences de métier (A exige B=0, B exige A=0) → boucle infinie dans le jeu client. Corrigé : exclusivité par marqueurs `twdMetierBlock<M>` calculés par un buff permanent (plus aucune référence croisée). **À reconfirmer en jeu** avec le serveur complet
- [x] **Permissions Discord** (`discord-bot/apply_permissions.py`, relançable) : nouveau venu → ACCUEIL en lecture seule ; Survivant (règlement accepté) → LE TERMINUS et #mémorial en lecture, ROLEPLAY/FACTIONS/HORS-JEU/SUPPORT en écriture, VOCAL ; STAFF réservé à Staff + Modérateur. Vérifiable avec `inspect_permissions.py`
- [x] **Mécanicien et Ingénieur séparés** : 7 métiers (régénérés par `tools/Generate-Metiers.ps1`), outils de réparation passés au Mécanicien, rôle Discord « Ingénieur » créé, `#métiers`, guide, document et changelog 0.3 à jour
- [x] **Déconnexions en danger** (`TWD_Zones`) : déconnexion moins de 5 min après un coup reçu, avec des rôdeurs à moins de 15 m, ou près d'un joueur en zone PvP → signalée dans `#logs` (aucune sanction automatique, le staff tranche)
- [x] `#métiers` décrit chaque métier ; `#zones-pvp` explique les zones (message tenu à jour par `/zones publier:True` ou `post_zones.py`) ; zones d'exemple retirées de `zones.xml`
- [x] **Bot Discord « Balise-01 »** (`discord-bot/`, Python + discord.py, via telnet), **en ligne** : utilise les salons existants du Discord (statut → `#statut-serveur`, morts → `#mémorial`, chat ↔ `#radio`, métiers → `#métiers`, zones → `#zones-pvp`, logs → `#logs`), commandes admin. Rôles : Staff, Modérateur, Survivant + 6 métiers
- [x] Incident `/installer` (doublons) **réparé** : salons d'origine restaurés (catégories, ordre, permissions), 15 salons + 3 catégories du bot supprimés, `/installer` retiré du bot
- [ ] Bot Discord : tests avec de vrais joueurs (chat, morts, connexions, commandes admin) ; donner le rôle **Staff** aux admins
- [x] Rôle **Survivant** : donné par le bouton « J'accepte le règlement » dans `#règlement`
- [x] `#guide-du-survivant` rempli (8 messages, `discord-bot/guide.py` + `post_guide.py`)
- [x] **Discord tenu à jour** : `CHANGELOG.md` → `#changelog` (`discord-bot/post_changelog.py`, version 0.1 publiée). À chaque changement de gameplay : changelog + guide + document de règlement + cette liste
- [x] **Communication et financement** (document partageable) : https://claude.ai/artifact/5c8WKCqnXQRMfKgCGdHepX — dons uniquement, aucun avantage en jeu (EULA), places réservées aux donateurs, plan de lancement en 4 phases, textes prêts
- [ ] Financement : choisir la plateforme de dons, fixer l'objectif mensuel (dépend de l'offre OVH), créer le rôle « Soutien » et `#remerciements`
- [ ] Communication : fixer la date d'ouverture, puis dérouler le calendrier (annuaires, Reddit, communautés FR, vidéos, streamers)
- [ ] Règlement : fixer les durées de sanctions et le salon Discord de signalement
- [ ] Équilibrage après les tests (densité WalkerSim, chance de morsure, durée de l'infection, palier de difficulté)

### Hébergement OVH
- [ ] Choisir l'offre (CPU rapide en monocœur, 16 Go de RAM minimum)
- [ ] Installer le serveur (SteamCMD), copier `Mods/`, la carte et `serverconfig.twd.xml`
- [ ] Ouvrir les ports (26900 TCP/UDP, 26901-26903 UDP)
- [ ] Passer `ServerVisibility` à 2 (public), définir un mot de passe admin (`serveradmin.xml`)
- [ ] **Sécuriser telnet** : mettre un `TelnetPassword` (ou `TelnetEnabled` = false) ; sans mot de passe, telnet n'écoute qu'en local, mais un mot de passe est indispensable si on veut administrer à distance
- [ ] Après chaque mise à jour du jeu : recompiler `TWD_Zones` et `TWD_Vehicles` (`tools/Build-Mod.ps1 -Name ...`) et vérifier qu'il se charge
- [ ] Redémarrages automatiques, **sauvegardes** régulières de la partie, procédure de mise à jour
- [ ] Après chaque mise à jour du jeu : relancer les scripts de `tools/` et vérifier WalkerSim
