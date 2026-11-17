"""Contenu du guide du survivant pour #guide-du-survivant (un message par section, < 2000 caractères).
Source : le document « Serveur TWD – Règlement et guide du survivant ». Publier / mettre à jour : post_guide.py
"""

GUIDE = [
"""# 📖 Guide du survivant
Le Terminus est une survie fidèle à **The Walking Dead** : pas de super-zombies, pas de magie. Juste des rôdeurs, la faim, et les autres survivants. La carte s'inspire de la Géorgie : forêts, campagne, petites villes, une prison, et une grande ville en ruine.

**Trois règles à retenir**
• **Visez la tête.** C'est la seule façon de tuer un rôdeur.
• **Une morsure tue.** Seule une amputation rapide peut vous sauver.
• **Un mort est un mort.** Votre personnage repart de zéro… et se relève en rôdeur.

Le règlement est dans <#{reglement}>.""",

"""## 🧟 Les rôdeurs
• **Lents** : ils marchent, de jour comme de nuit. Ils ne courent jamais.
• **Humains uniquement** : civils, soldats, infirmières, ouvriers, rampants. Ni mutants, ni obèses boursouflés, ni animaux zombies. La faune reste naturelle : loups, ours, pumas…
• **Headshots only** : seule la tête compte, et **un bon coup à la tête suffit**, quelle que soit l'arme. Les poings, eux, ne tuent jamais un rôdeur.
• **Hordes** : des milliers de rôdeurs existent réellement sur la carte. Ils se déplacent en troupeaux, suivent les routes et convergent vers les villes.
• **Bruit** : un coup de feu ou un moteur attire les rôdeurs de loin, et ils continuent d'arriver après votre départ.
• **Difficulté fixe** : le monde est aussi dangereux le premier jour que le centième. Pas de Blood Moon : le danger est permanent.""",

"""## 🩸 Morsure et infection
Une morsure tue en environ **un jour et demi de jeu**, et rien ne la guérit. Chaque coup de rôdeur a environ **5 %** de chances d'être une morsure : plus vous encaissez, plus le risque grimpe. Seuls les rôdeurs infectent.
• **Stade 1 – Morsure** (~5 h de jeu) : personne ne le remarque. Vous pouvez la cacher… ou l'avouer.
• **Stade 2 – Propagation** : endurance réduite, gémissements de temps en temps.
• **Stade 3 – Fièvre** : attributs en baisse, gémissements et vomissements (qui attirent les rôdeurs).
• **Fin** : la mort, puis vous vous relevez en rôdeur.
Antibiotiques et miel **ralentissent** l'infection, ils ne la soignent pas.

### ✂️ L'amputation, votre seule chance
Elle ne sauve **qu'au stade 1**. Après, vous perdez le membre et l'infection continue.
1. Fabriquez un **kit d'amputation** dans votre sac : 4 tissus, 1 bois, 3 ferraille.
2. Utilisez-le sur vous (clic gauche) ou sur un compagnon (clic droit en le visant).
3. **-40 PV** et une **hémorragie** : arrêtez-la vite avec un bandage.
Un **Médecin** fait mieux avec son **kit chirurgical** : -15 PV seulement.
Le survivant amputé garde jusqu'à sa mort le malus **Amputé** (plus lent, moins d'endurance, moins de port, moins précis).""",

"""## 🕯️ La mort
• **Nous sommes tous infectés** : quelle que soit la cause de la mort, un rôdeur se relève sur votre corps.
• **Vous perdez tout** : votre équipement reste sur votre cadavre, gardé par ce rôdeur.
• **Mort définitive** : retour au niveau 1, sans compétences, livres ni métier. Vous incarnez un nouveau survivant, qui choisit à nouveau son métier.
• **La base reste** : le bloc de revendication de votre groupe est conservé.
• **Équipement** : à la première arrivée, couteau, batte, torche, conserves, eau et bandages. Après une mort : un couteau et un bandage.
Les disparus sont honorés dans <#{memorial}>.""",

"""## 🧰 Les métiers
Chaque survivant choisit **un seul métier** (Compétences → onglet **Métiers**). Le premier est **offert** : un point vous attend à l'arrivée et après chaque mort. Expert dans son domaine, débutant dans les autres : **pour durer, il faut un groupe**.
🩺 **Médecin** – soins, antibiotiques, plâtres, trousses de soins, kit chirurgical
🏹 **Chasseur / Éclaireur** – arcs, arbalètes, lames, lances, discrétion, pistage
🎖️ **Soldat** – armes à feu, explosifs de combat, armures lourdes
🔧 **Mécanicien** – véhicules, essence, récupération, réparation, véhicules abandonnés
⚡ **Ingénieur** – électricité, pièges, explosifs, robotique
🌾 **Fermier / Cuisinier** – cultures, semences, cuisine
🧱 **Bâtisseur** – construction, établis, outils de récolte, minage, gourdins, masses
• Hors métier : compétences limitées au **niveau 2**, et les magazines n'apprennent que les bases. Un magazine trop avancé vous est rendu : échangez-le.
• On ne change de métier qu'**en mourant**.
Affichez le vôtre dans <#{metiers}>.""",

"""## 🎒 Ressources, armes et véhicules
• **Pas de marchands, pas de monnaie** : tout se trouve, se fabrique ou s'échange (<#{troc}>).
• **Le loot ne réapparaît pas** : une ville pillée reste pillée. Il faudra aller plus loin, ou cultiver.
• **Armes à feu rares et bruyantes** : chaque tir attire les rôdeurs et les hordes. Les versions silencieuses sont précieuses.
• **L'arbalète** : silencieuse, carreaux fabricables. L'arme du survivant.
• **Véhicules** : un moteur attire les rôdeurs, l'essence est rare, et foncer dans une horde abîme le véhicule. Seul le Mécanicien fabrique l'essence et les pièces de véhicules à moteur. Pas d'hélicoptère. Une faction ne peut pas posséder plus de **3 véhicules à moteur**.
• **Ni carte, ni boussole, ni marqueurs** : on s'oriente aux routes et aux repères. Notez vos itinéraires.
• **Pas de super-pouvoirs** : les compétences surhumaines du jeu de base sont désactivées.""",

"""## ⚔️ PvP et zones
• **Hors zone** (presque toute la carte) : PvE, aucun joueur ne peut en blesser un autre.
• **Zone PvP** : les dégâts passent si l'attaquant **et** la victime sont tous les deux dans la zone.
• **Zone sûre** (ex. un marché) : aucun dégât entre joueurs, même au milieu d'une zone PvP.
• **Véhicules abandonnés** : on n'en trouve que dans les zones PvP. Ils sont rares et en panne : seul un **Mécanicien** peut les remettre en route.
Un message s'affiche à l'écran en entrant et en sortant d'une zone PvP. La liste est dans <#{zones}>.
Les autres survivants sont le vrai danger : entrez-y en groupe.""",

"""## 🔥 Conseils pour vos premiers jours
1. Restez accroupi près des rôdeurs : vous faites moins de bruit.
2. Frappez à la tête, puis reculez. Ne vous laissez jamais encercler.
3. Gardez vos munitions pour les urgences : un tir peut attirer un troupeau entier.
4. Fabriquez tôt un kit d'amputation et gardez-le sur vous.
5. Portez toujours des bandages : une hémorragie tue vite.
6. Évitez les routes la nuit : les grands troupeaux les suivent.
7. Trouvez un groupe. Seul, une seule morsure suffit à vous perdre.
8. Cultivez : les conserves finiront par manquer, les récoltes non.""",
]
