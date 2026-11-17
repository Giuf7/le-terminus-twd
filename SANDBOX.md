# Réglages Sandbox du serveur TWD

Le **SandboxCode** est **généré automatiquement** (plus besoin du menu du jeu) :

1. Les réglages sont dans **`tools/sandbox_twd.txt`** (`Option=index`, commentés).
2. Sur un serveur où `TWD_Zones` est installé : copier ce fichier dans `Mods/TWD_Zones/`, puis en console :
   `twdsandbox apply sandbox_twd.txt` → le code s'affiche.
3. Le coller dans `serverconfig.twd.xml` (propriété `SandboxCode`).

Autres commandes : `twdsandbox list` (toutes les options et leurs valeurs possibles, écrites dans
`Mods/TWD_Zones/twd_sandbox_options.txt`), `twdsandbox code` (code des options chargées).
La commande utilise la fonction du jeu lui-même ; méthode validée : elle redonne le code d'origine
`AAAJABJACJADJARFBNC`, et le code TWD rechargé par le serveur donne bien les 43 réglages voulus.

## Code actuel (17/11/2026)

`AACJADJATDEMBBADBGDBHDAQBBJABKABLABNCBPABOACDABWACAACBACQACRACSACTACXADEDDIDDJADLAEYAEZAEOAFKAEPAEQAEUAEVAFLKFOK`

## Ce qu'il règle (détail dans `tools/sandbox_twd.txt`)

| Thème | Réglages |
|---|---|
| Rôdeurs | Headshot Only ; marche de jour, de nuit, férals et Blood Moon ; pas de rage ; ne creusent pas ; dégâts aux blocs 50 % |
| Dégâts | 100 % (le preset « Aventurier » d'origine donnait +50 % aux joueurs et -25 % de dégâts reçus) |
| Événements | Pas de Blood Moon, pas de largages, pas de dangers de biome, pas de marchands ni distributeurs, pas de quêtes ni de cartes au trésor |
| Mort | Mort définitive ; tout est lâché sur le cadavre |
| Loot | Ne réapparaît jamais ; munitions et armes à feu 50 % ; aucun Duke |
| Compétences | Un point tous les 2 niveaux ; points de départ donnés au début |
| Véhicules | Consommation x2 ; dégâts de collision x2 |
| Immersion | Pas de barres de vie, de chiffres de dégâts ni d'XP affichée ; ni carte, ni boussole, ni marqueurs, ni nom des lieux |
| Temps | Journées de 60 min (la durée de l'infection en dépend) |
