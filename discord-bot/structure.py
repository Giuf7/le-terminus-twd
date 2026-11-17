"""Salons et rôles du serveur Discord « Le Terminus » utilisés par le bot.

Le bot NE CRÉE AUCUN salon : il utilise ceux du serveur, par identifiant (un renommage ne casse rien).
Pour changer un salon : clic droit sur le salon > Copier l'identifiant (mode développeur activé).
"""

CHANNELS = {
    "reglement": 1572161575022760016,  # #règlement : règles + bouton d'acceptation (rôle Survivant)
    "guide": 1572161791528542208,      # #guide-du-survivant : cité dans le règlement
    "statut": 1572161646846025739,     # #statut-serveur : statut en direct
    "annonces": 1572161608619401257,   # #annonces : redémarrages
    "changelog": 1572161950031421460,  # #changelog : nouvelles versions (post_changelog.py)
    "zones": 1572161827062808598,      # #zones-pvp : liste des zones (/zones publier)
    "metiers": 1572161862072672286,    # #métiers : boutons de choix du métier
    "memorial": 1572162074182819911,   # #mémorial : morts des survivants
    "radio": 1572162127999934567,      # #radio : pont de chat jeu <-> Discord
    "logs": 1572163066391695390,       # #logs (staff) : arrivées/départs + actions admin
}

STAFF = "Staff"
MODO = "Modérateur"
SURVIVANT = "Survivant"

# Rôles de métier, choisis par le joueur dans #métiers (simple étiquette pour se trouver entre joueurs)
METIERS = [
    {"key": "medecin", "name": "Médecin", "emoji": "🩺"},
    {"key": "chasseur", "name": "Chasseur / Éclaireur", "emoji": "🏹"},
    {"key": "soldat", "name": "Soldat", "emoji": "🎖️"},
    {"key": "mecanicien", "name": "Mécanicien", "emoji": "🔧"},
    {"key": "ingenieur", "name": "Ingénieur", "emoji": "⚡"},
    {"key": "fermier", "name": "Fermier / Cuisinier", "emoji": "🌾"},
    {"key": "batisseur", "name": "Bâtisseur", "emoji": "🧱"},
]

ZONES_TEXT = (
    "# ⚔️ Zones PvP\n"
    "Le Terminus est **PvE partout**, sauf dans les zones ci-dessous.\n"
    "• **Zone PvP** : les dégâts entre joueurs passent si l'attaquant **et** la victime sont tous les deux dans la zone.\n"
    "• **Zone sûre** : aucun dégât entre joueurs, même au milieu d'une zone PvP.\n"
    "• Un message s'affiche à l'écran quand vous entrez dans une zone PvP et quand vous en sortez.\n\n"
    "🚗 **Véhicules abandonnés** : les zones PvP sont le seul endroit où l'on trouve des véhicules abandonnés. "
    "Ils sont rares et en panne : seul un **Mécanicien** peut les remettre en route. "
    "Une faction ne peut pas posséder plus de **3 véhicules**.\n\n"
    "## 🗺️ Zones actuelles\n"
    "{zones}\n\n"
    "*Liste mise à jour par le staff. Coordonnées : X = est/ouest, Z = nord/sud (sans carte ni boussole, fiez-vous aux repères).*"
)

NO_ZONES = "Aucune zone PvP n'est ouverte pour l'instant : elles seront annoncées avec la carte définitive."

RULES_TEXT = (
    "# ☣️ Règlement du Terminus\n"
    "Survie fidèle à **The Walking Dead** : rôdeurs lents, **headshots only**, **morsure mortelle**, "
    "**mort définitive**, hordes errantes, **métiers**, PvE partout et zones PvP.\n\n"
    "1. Respect de tous, en jeu comme ici. Pas d'insultes, de harcèlement ni de propos haineux.\n"
    "2. Pas de triche : programmes tiers, failles, duplication.\n"
    "3. Pas de déconnexion pour fuir un combat ou une horde (les déconnexions en danger sont signalées au staff).\n"
    "4. Pas de suicide pour obtenir un kit ou se téléporter.\n"
    "5. Hors zones PvP : pas de pillage, de destruction ni de horde attirée sur la base d'un autre groupe.\n"
    "6. Ce que vous apprenez hors jeu (Discord, vocal, stream) ne sert pas en jeu.\n\n"
    "📖 Le guide complet : <#{guide}>\n\n"
    "En cliquant ci-dessous, vous acceptez le règlement."
)

METIER_TEXT = (
    "# 🧰 Les métiers\n"
    "Chaque survivant choisit **un seul métier** en jeu (Compétences → onglet **Métiers**). "
    "Le premier est **offert** : un point vous attend à l'arrivée et après chaque mort. "
    "Expert dans son domaine, débutant dans les autres : **pour durer, il faut un groupe.**\n\n"
    "🩺 **Médecin** — Soigne et opère. Seul à fabriquer antibiotiques, plâtres, trousses de soins "
    "et le **kit chirurgical** (amputation à -15 PV au lieu de -40).\n\n"
    "🏹 **Chasseur / Éclaireur** — Arcs, arbalètes, lames et lances. Discrétion, pistage, parkour, chasse. "
    "Seul à fabriquer l'arbalète et les arcs avancés.\n\n"
    "🎖️ **Soldat** — Armes à feu, explosifs de combat, corps à corps, armures moyennes et lourdes. "
    "Seul à fabriquer les armes à feu avancées et les armures.\n\n"
    "🔧 **Mécanicien** — Véhicules, essence, récupération de pièces, outils de réparation. "
    "Seul à fabriquer les pièces de véhicules à moteur et l'essence, et seul à remettre en route les véhicules abandonnés.\n\n"
    "⚡ **Ingénieur** — Électricité, pièges, explosifs, robotique. "
    "Seul à fabriquer les installations électriques, pièges et explosifs avancés. Celui qui éclaire et défend la base.\n\n"
    "🌾 **Fermier / Cuisinier** — Cultures, semences, cuisine, conservation. "
    "Seul à fabriquer les semences et les plats avancés. Sans lui, le groupe finit par avoir faim.\n\n"
    "🧱 **Bâtisseur** — Construction, établis, outils de récolte, minage, gourdins et masses. "
    "Seul à fabriquer les établis et outils avancés. Celui qui fortifie la communauté.\n\n"
    "**Règles** : hors métier, compétences limitées au niveau 2 et magazines limités aux bases "
    "(un magazine trop avancé vous est rendu : échangez-le). On ne change de métier qu'en mourant.\n\n"
    "👇 **Affichez votre métier** pour que les autres survivants vous trouvent (cliquez à nouveau pour le retirer)."
)
