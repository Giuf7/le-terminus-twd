# Bot Discord « Le Terminus » (Balise-01)

Bot Python (discord.py) relié à la console telnet du serveur 7 Days to Die.
**Il ne crée et ne modifie aucun salon** : il utilise ceux du serveur, par identifiant (`structure.py` → `CHANNELS`).

| Fonction | Salon |
|---|---|
| Règlement + bouton « J'accepte » (donne le rôle Survivant) — publié par `post_rules.py` | `#règlement` |
| Guide du survivant (8 messages, texte dans `guide.py`) — publié / mis à jour par `post_guide.py` | `#guide-du-survivant` |
| Nouvelles versions de `../CHANGELOG.md` — publiées une seule fois par `post_changelog.py` (`--dry-run` pour simuler) | `#changelog` |
| Statut en direct (en ligne, joueurs, jour de jeu), mis à jour chaque minute | `#statut-serveur` |
| Morts (« … et s'est relevé en rôdeur ») | `#mémorial` |
| Pont de chat jeu ↔ Discord | `#radio` |
| Choix du métier (boutons → rôle de métier) | `#métiers` |
| Liste des zones (`/zones publier:True`) | `#zones-pvp` |
| Redémarrages | `#annonces` |
| Arrivées / départs + journal des actions admin | `#logs` (staff) |

Commandes : `/joueurs` (tous) ; `/kick`, `/ban`, `/annonce`, `/redemarrer`, `/annuler-redemarrage`, `/zones`, `/console`
(rôles **Staff** / **Modérateur** ou administrateurs), utilisables dans n'importe quel salon, réponses visibles seulement par l'auteur.

## Lancement

1. Lancer le serveur de jeu (telnet activé, port 8081).
2. Lancer `start-bot.ps1` (le bot se relance tout seul s'il s'arrête).

## Configuration (`.env`, à ne jamais partager)

| Clé | Rôle |
|---|---|
| `DISCORD_TOKEN` | Jeton du bot (secret) |
| `GUILD_ID` | Serveur Discord (`1572160114310385695`) |
| `TELNET_HOST` / `TELNET_PORT` / `TELNET_PASSWORD` | Console du serveur de jeu (en production : mettre un mot de passe telnet) |
| `SERVER_NAME`, `SERVER_ADDRESS` | Affichage dans le statut (`SERVER_ADDRESS` = IP:port publics une fois sur OVH) |

Changer un salon utilisé par le bot : Discord → Paramètres → Avancés → Mode développeur, puis clic droit sur le salon → Copier l'identifiant, et le remplacer dans `structure.py`.

## Outils

- `inspect_guild.py` : état des lieux du Discord en **lecture seule** (rôles, salons, actions du bot dans le journal d'audit) → `inspect_report.txt`.
- `inspect_messages.py` : derniers messages de chaque salon, en lecture seule → `inspect_messages.txt`.

## Notes

- `/redemarrer` sauvegarde puis **arrête** le serveur : il faut un lanceur qui le relance automatiquement (prévu pour OVH).
- Les formats des lignes de chat / connexion / mort ont été vérifiés sur des exemples ; à confirmer avec de vrais joueurs (`TEST.md`).
