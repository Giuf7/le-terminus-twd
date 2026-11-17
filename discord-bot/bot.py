"""Bot Discord du serveur « Le Terminus » (7 Days to Die, survie TWD).

Le bot ne crée et ne modifie AUCUN salon : il utilise ceux du serveur (structure.CHANNELS).
Fonctions :
- statut du serveur en direct (#statut-serveur)
- morts des survivants (#mémorial) ; arrivées / départs (#logs)
- pont de chat jeu <-> Discord (#radio)
- choix du métier par boutons (#métiers)
- commandes admin (n'importe quel salon, réponses privées) : /kick /ban /annonce /redemarrer /zones /console,
  tracées dans #logs
Configuration : fichier .env (voir .env.example). Lancement : start-bot.ps1
"""

import asyncio
import json
import logging
import os
import re
from datetime import datetime, timezone
from pathlib import Path

import discord
from discord import app_commands

import structure as S
from telnet_client import TelnetConsole, quote

BASE = Path(__file__).resolve().parent
logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s : %(message)s")
log = logging.getLogger("terminus")


# ------------------------------------------------------------------ configuration
def load_env(path: Path) -> dict:
    env = {}
    if path.exists():
        for line in path.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if line and not line.startswith("#") and "=" in line:
                k, v = line.split("=", 1)
                env[k.strip()] = v.strip().strip('"')
    env.update({k: v for k, v in os.environ.items() if k.startswith("TERMINUS_") or k == "DISCORD_TOKEN"})
    return env


ENV = load_env(BASE / ".env")
TOKEN = ENV.get("DISCORD_TOKEN", "")
GUILD_ID = int(ENV.get("GUILD_ID", "1572160114310385695"))
TELNET_HOST = ENV.get("TELNET_HOST", "127.0.0.1")
TELNET_PORT = int(ENV.get("TELNET_PORT", "8081"))
TELNET_PASSWORD = ENV.get("TELNET_PASSWORD", "")
GUIDE_URL = ENV.get("GUIDE_URL", "(lien du guide à définir)")
SERVER_NAME = ENV.get("SERVER_NAME", "Le Terminus")
SERVER_ADDRESS = ENV.get("SERVER_ADDRESS", "")   # ex. "51.38.xx.xx:26900", affiché dans le statut

STATE_FILE = BASE / "state.json"


def load_state() -> dict:
    try:
        return json.loads(STATE_FILE.read_text(encoding="utf-8"))
    except (OSError, ValueError):
        return {}


def save_state(state: dict):
    STATE_FILE.write_text(json.dumps(state, indent=2), encoding="utf-8")


# ------------------------------------------------------------------ événements du jeu (journal telnet)
RE_CHAT = re.compile(r"Chat \(from '(?P<src>[^']*)', entity id '(?P<eid>[^']*)', to '(?P<to>[^']*)'\): '(?P<name>[^']*)': (?P<msg>.*)$")
RE_JOIN = re.compile(r"GMSG: Player '(?P<name>.+)' joined the game")
RE_LEFT = re.compile(r"GMSG: Player '(?P<name>.+)' left the game")
RE_KILLED = re.compile(r"GMSG: Player '(?P<name>.+)' killed by '(?P<killer>.+)'")
RE_DIED = re.compile(r"GMSG: Player '(?P<name>.+)' died")
RE_LOGOUT = re.compile(r"\[TWD_Logout\] DECO_SAUVAGE '(?P<name>.+)' pos=\((?P<x>-?\d+|\?), (?P<z>-?\d+|\?)\) : (?P<why>.+)$")
RE_LP_TOTAL = re.compile(r"Total of (\d+) in the game")
RE_LP_PLAYER = re.compile(r"^\d+\. id=\d+, (?P<name>.+?), pos=")
# Journal TWD (mod TWD_Zones) : "[TWD_Journal] [Catégorie] message" -> #logs (sauf les coups de rôdeur, trop nombreux)
RE_JOURNAL = re.compile(r"\[TWD_Journal\] \[(?P<cat>[^\]]+)\] (?P<msg>.+)$")
JOURNAL_EMOJI = {"Infection": "🦠", "Amputation": "✂️", "Mort": "💀", "Métier": "🧰", "Véhicule": "🚗"}
JOURNAL_SKIP = {"Coup"}
RE_TIME = re.compile(r"Day (\d+), (\d\d):(\d\d)")
RE_ZONE_CIRCLE = re.compile(r"- (?P<name>.+?) \[(?P<kind>PvP|SURE)\] cercle centre \((?P<x>-?\d+), (?P<z>-?\d+)\) rayon (?P<r>\d+)")
RE_ZONE_RECT = re.compile(r"- (?P<name>.+?) \[(?P<kind>PvP|SURE)\] rectangle \((?P<x1>-?\d+), (?P<z1>-?\d+)\) -> \((?P<x2>-?\d+), (?P<z2>-?\d+)\)")


def is_staff(member: discord.abc.User) -> bool:
    if not isinstance(member, discord.Member):
        return False
    if member.guild_permissions.administrator:
        return True
    return any(r.name in (S.STAFF, S.MODO) for r in member.roles)


# ------------------------------------------------------------------ boutons persistants
class RulesView(discord.ui.View):
    def __init__(self):
        super().__init__(timeout=None)

    @discord.ui.button(label="J'accepte le règlement", emoji="✅", style=discord.ButtonStyle.success, custom_id="terminus:rules")
    async def accept(self, interaction: discord.Interaction, _button: discord.ui.Button):
        role = discord.utils.get(interaction.guild.roles, name=S.SURVIVANT)
        if role is None:
            return await interaction.response.send_message("Le rôle Survivant n'existe pas sur le serveur.", ephemeral=True)
        if role in interaction.user.roles:
            return await interaction.response.send_message("Vous avez déjà accepté le règlement. Restez en vie. 🧟", ephemeral=True)
        await interaction.user.add_roles(role, reason="Règlement accepté")
        await interaction.response.send_message("Bienvenue au Terminus, survivant. Restez en vie. 🧟", ephemeral=True)


class MetierButton(discord.ui.Button):
    def __init__(self, metier: dict):
        super().__init__(label=metier["name"], emoji=metier["emoji"], style=discord.ButtonStyle.secondary,
                         custom_id=f"terminus:metier:{metier['key']}")
        self.metier = metier

    async def callback(self, interaction: discord.Interaction):
        member = interaction.user
        names = {m["name"] for m in S.METIERS}
        target = discord.utils.get(interaction.guild.roles, name=self.metier["name"])
        if target is None:
            return await interaction.response.send_message("Ce rôle de métier n'existe pas sur le serveur.", ephemeral=True)
        if target in member.roles:
            await member.remove_roles(target, reason="Métier retiré")
            return await interaction.response.send_message(f"Métier retiré : {self.metier['name']}.", ephemeral=True)
        old = [r for r in member.roles if r.name in names]
        if old:
            await member.remove_roles(*old, reason="Changement de métier")
        await member.add_roles(target, reason="Choix du métier")
        await interaction.response.send_message(f"Métier affiché : {self.metier['emoji']} {self.metier['name']}.", ephemeral=True)


class MetierView(discord.ui.View):
    def __init__(self):
        super().__init__(timeout=None)
        for m in S.METIERS:
            self.add_item(MetierButton(m))


# ------------------------------------------------------------------ le bot
class TerminusBot(discord.Client):
    def __init__(self):
        intents = discord.Intents.default()
        intents.message_content = True      # à activer dans le portail développeur (pont de chat)
        super().__init__(intents=intents)
        self.tree = app_commands.CommandTree(self)
        self.state = load_state()
        self.console = TelnetConsole(TELNET_HOST, TELNET_PORT, TELNET_PASSWORD, self.on_game_line)
        self.players: list[str] = []
        self.game_time = ""
        self.restart_task: asyncio.Task | None = None

    # --- salons (par identifiant, voir structure.CHANNELS)
    def channel(self, key: str) -> discord.TextChannel | None:
        return self.get_channel(S.CHANNELS[key])

    async def post(self, key: str, content: str = None, embed: discord.Embed = None):
        ch = self.channel(key)
        if ch is None:
            return log.warning("Salon '%s' introuvable (identifiant %s)", key, S.CHANNELS[key])
        try:
            await ch.send(content=content, embed=embed, allowed_mentions=discord.AllowedMentions.none())
        except discord.HTTPException as exc:
            log.warning("Envoi impossible dans #%s : %s", ch.name, exc)

    async def admin_log(self, who: discord.abc.User, action: str):
        await self.post("logs", f"🛠️ **{who.display_name}** : {action}")

    # --- démarrage
    async def setup_hook(self):
        self.add_view(RulesView())
        self.add_view(MetierView())
        register_commands(self)
        guild = discord.Object(id=GUILD_ID)
        self.tree.copy_global_to(guild=guild)
        await self.tree.sync(guild=guild)
        self._tasks = [asyncio.create_task(self.console.run_forever(), name="telnet"),
                       asyncio.create_task(self.status_loop(), name="statut")]
        for t in self._tasks:
            t.add_done_callback(self._task_died)

    def _task_died(self, task: asyncio.Task):
        if not task.cancelled() and task.exception():
            log.error("La tâche '%s' s'est arrêtée sur une erreur", task.get_name(), exc_info=task.exception())

    async def on_ready(self):
        log.info("Connecté à Discord en tant que %s", self.user)

    # --- jeu -> Discord
    async def on_game_line(self, line: str):
        m = RE_CHAT.search(line)
        if m:
            if m["to"] == "Global" and m["src"] != "-non-player-" and m["eid"] != "-1":
                await self.post("radio", f"📻 **{discord.utils.escape_markdown(m['name'])}** : {discord.utils.escape_mentions(m['msg'])}")
            return
        m = RE_JOURNAL.search(line)
        if m:
            if m["cat"] in JOURNAL_SKIP:
                return
            emoji = JOURNAL_EMOJI.get(m["cat"], "📓")
            return await self.post("logs", f"{emoji} **{m['cat']}** — {discord.utils.escape_mentions(m['msg'])}")
        m = RE_LOGOUT.search(line)
        if m:
            return await self.post("logs", f"⚠️ **Déconnexion en danger** : **{m['name']}** (X {m['x']}, Z {m['z']}) — {m['why']}")
        m = RE_JOIN.search(line)
        if m:
            return await self.post("logs", f"🚶 **{m['name']}** a rejoint le serveur.")
        m = RE_LEFT.search(line)
        if m:
            return await self.post("logs", f"👋 **{m['name']}** a quitté le serveur.")
        m = RE_KILLED.search(line)
        if m:
            return await self.post("memorial", f"🕯️ **{m['name']}** a été tué par **{m['killer']}**… et s'est relevé en rôdeur.")
        m = RE_DIED.search(line)
        if m:
            return await self.post("memorial", f"🕯️ **{m['name']}** est mort… et s'est relevé en rôdeur.")

    # --- Discord -> jeu
    async def on_message(self, message: discord.Message):
        if message.author.bot or message.guild is None or message.guild.id != GUILD_ID:
            return
        if message.channel.id != S.CHANNELS["radio"] or not message.content:
            return
        text = quote(message.clean_content)[:200]
        if not text:
            return
        try:
            await self.console.send(f'say "[Discord] {quote(message.author.display_name)} : {text}"')
        except asyncio.TimeoutError:
            await message.add_reaction("⚠️")

    # --- statut
    async def refresh_players(self):
        lines = await self.console.execute("lp", wait=1.5)
        names = [m["name"] for m in (RE_LP_PLAYER.search(l) for l in lines) if m]
        self.players = names
        tl = await self.console.execute("gettime", wait=1.0)
        for l in tl:
            m = RE_TIME.search(l)
            if m:
                self.game_time = f"Jour {m[1]}, {m[2]}:{m[3]}"

    def status_embed(self, online: bool) -> discord.Embed:
        if online:
            e = discord.Embed(title=f"🟢 {SERVER_NAME} est en ligne", color=0x2ECC71)
            e.add_field(name="Survivants connectés", value=str(len(self.players)))
            e.add_field(name="Temps de jeu", value=self.game_time or "?")
            if self.players:
                e.add_field(name="En jeu", value=", ".join(self.players)[:1000], inline=False)
        else:
            e = discord.Embed(title=f"🔴 {SERVER_NAME} est hors ligne", color=0xE74C3C,
                              description="Le serveur redémarre ou est en maintenance.")
        if SERVER_ADDRESS:
            e.add_field(name="Adresse", value=f"`{SERVER_ADDRESS}`", inline=False)
        now = int(datetime.now(timezone.utc).timestamp())
        e.add_field(name="Dernière mise à jour", value=f"<t:{now}:R> (toutes les minutes)", inline=False)
        return e

    async def status_loop(self):
        await self.wait_until_ready()
        while not self.is_closed():
            online = self.console.connected.is_set()
            if online:
                try:
                    await self.refresh_players()
                except Exception:
                    log.exception("Lecture du statut impossible")
            try:
                await self.update_status_message(online)
                log.info("Statut mis à jour (%s, %d joueur(s), %s)", "en ligne" if online else "hors ligne", len(self.players), self.game_time)
            except Exception:
                log.exception("Mise à jour du statut impossible")
            await asyncio.sleep(60)

    async def update_zones_message(self, console_lines: list[str] | None) -> str:
        return await write_zones_message(self, console_lines)

    async def update_status_message(self, online: bool):
        ch = self.channel("statut")
        if ch is None:
            return
        embed = self.status_embed(online)
        self.state = load_state()   # relu à chaque fois : les scripts (guide, changelog...) écrivent aussi ce fichier
        msg_id = self.state.get("status_message_id")
        try:
            if msg_id:
                msg = await ch.fetch_message(msg_id)
                await msg.edit(embed=embed)
                return
        except discord.NotFound:
            pass
        except discord.HTTPException as exc:
            log.warning("Statut : %s", exc)
            return
        msg = await ch.send(embed=embed)
        self.state["status_message_id"] = msg.id
        save_state(self.state)


# ------------------------------------------------------------------ message de #zones-pvp
async def write_zones_message(client: discord.Client, console_lines: list[str] | None) -> str:
    """Met à jour (ou publie) le message unique de #zones-pvp. console_lines = réponse de 'twdzones list'."""
    entries = []
    for l in console_lines or []:
        m = RE_ZONE_CIRCLE.search(l)
        if m:
            kind = "⚔️ PvP" if m["kind"] == "PvP" else "🛡️ zone sûre"
            entries.append(f"• **{m['name']}** — {kind} — cercle de {m['r']} m autour de X {m['x']}, Z {m['z']}")
            continue
        m = RE_ZONE_RECT.search(l)
        if m:
            kind = "⚔️ PvP" if m["kind"] == "PvP" else "🛡️ zone sûre"
            entries.append(f"• **{m['name']}** — {kind} — rectangle de X {m['x1']}, Z {m['z1']} à X {m['x2']}, Z {m['z2']}")
    text = S.ZONES_TEXT.format(zones="\n".join(entries) if entries else S.NO_ZONES)[:2000]
    ch = client.get_channel(S.CHANNELS["zones"])
    st = load_state()
    try:
        if st.get("zones_message_id"):
            msg = await ch.fetch_message(st["zones_message_id"])
            await msg.edit(content=text, allowed_mentions=discord.AllowedMentions.none())
            return text
    except discord.NotFound:
        pass
    msg = await ch.send(text, allowed_mentions=discord.AllowedMentions.none())
    st["zones_message_id"] = msg.id
    save_state(st)
    return text


# ------------------------------------------------------------------ commandes slash
def register_commands(bot: TerminusBot):
    tree = bot.tree

    @tree.error
    async def on_app_command_error(interaction: discord.Interaction, error: app_commands.AppCommandError):
        if isinstance(error, app_commands.CheckFailure):
            return  # message déjà envoyé par la vérification staff
        log.exception("Erreur de commande /%s", interaction.command.name if interaction.command else "?", exc_info=error)
        msg = "❌ Erreur pendant la commande."
        if interaction.response.is_done():
            await interaction.followup.send(msg, ephemeral=True)
        else:
            await interaction.response.send_message(msg, ephemeral=True)

    def staff_only():
        async def predicate(interaction: discord.Interaction) -> bool:
            if not is_staff(interaction.user):
                await interaction.response.send_message("Commande réservée au staff.", ephemeral=True)
                return False
            return True
        return app_commands.check(predicate)

    async def run(interaction: discord.Interaction, command: str, action_label: str, wait: float = 2.0):
        await interaction.response.defer(ephemeral=True, thinking=True)
        try:
            out = await bot.console.execute(command, wait=wait)
        except asyncio.TimeoutError:
            return await interaction.followup.send("❌ Serveur de jeu injoignable.", ephemeral=True)
        await bot.admin_log(interaction.user, action_label)
        text = "\n".join(out)[-1800:] or "(pas de réponse)"
        await interaction.followup.send(f"✅ {action_label}\n```\n{text}\n```", ephemeral=True)

    @tree.command(name="joueurs", description="Survivants connectés au Terminus")
    async def joueurs(interaction: discord.Interaction):
        if not bot.console.connected.is_set():
            return await interaction.response.send_message("🔴 Le serveur est hors ligne.", ephemeral=True)
        await interaction.response.defer(ephemeral=True)
        await bot.refresh_players()
        liste = ", ".join(bot.players) if bot.players else "personne"
        await interaction.followup.send(f"🧍 {len(bot.players)} survivant(s) en jeu : {liste}\n🕒 {bot.game_time}", ephemeral=True)

    @tree.command(name="kick", description="Expulser un joueur du serveur de jeu")
    @staff_only()
    async def kick(interaction: discord.Interaction, joueur: str, raison: str = "Expulsé par le staff"):
        await run(interaction, f'kick "{quote(joueur)}" "{quote(raison)}"', f"kick {joueur} ({raison})")

    @tree.command(name="ban", description="Bannir un joueur du serveur de jeu")
    @staff_only()
    @app_commands.choices(unite=[app_commands.Choice(name=n, value=v) for n, v in
                                 [("minutes", "minutes"), ("heures", "hours"), ("jours", "days"), ("semaines", "weeks"), ("mois", "months"), ("années", "years")]])
    async def ban(interaction: discord.Interaction, joueur: str, duree: app_commands.Range[int, 1, 1000],
                  unite: app_commands.Choice[str], raison: str = "Banni par le staff"):
        await run(interaction, f'ban add "{quote(joueur)}" {duree} {unite.value} "{quote(raison)}"',
                  f"ban {joueur} {duree} {unite.name} ({raison})")

    @tree.command(name="annonce", description="Envoyer une annonce dans le chat du jeu")
    @staff_only()
    async def annonce(interaction: discord.Interaction, message: str):
        await run(interaction, f'say "[Annonce] {quote(message)[:200]}"', f"annonce : {message}", wait=1.0)

    @tree.command(name="redemarrer", description="Redémarrer le serveur de jeu après un compte à rebours")
    @staff_only()
    async def redemarrer(interaction: discord.Interaction, minutes: app_commands.Range[int, 1, 60] = 5):
        if bot.restart_task and not bot.restart_task.done():
            return await interaction.response.send_message("Un redémarrage est déjà programmé.", ephemeral=True)

        async def countdown():
            remaining = minutes * 60
            marks = sorted({m for m in (minutes * 60, 300, 60, 30, 10) if m <= remaining}, reverse=True)
            for mark in marks:
                await asyncio.sleep(remaining - mark)
                remaining = mark
                label = f"{mark // 60} min" if mark >= 60 else f"{mark} s"
                await bot.console.send(f'say "[Serveur] Redémarrage dans {label}. Mettez-vous à l\'abri."')
            await asyncio.sleep(remaining)
            await bot.post("annonces", "🔄 Le serveur redémarre. Retour dans quelques minutes.")
            await bot.console.send("saveworld")
            await asyncio.sleep(5)
            await bot.console.send("shutdown")

        bot.restart_task = asyncio.create_task(countdown())
        await bot.admin_log(interaction.user, f"redémarrage programmé dans {minutes} min")
        await interaction.response.send_message(f"🔄 Redémarrage programmé dans {minutes} min (annonces en jeu).", ephemeral=True)

    @tree.command(name="annuler-redemarrage", description="Annuler le redémarrage programmé")
    @staff_only()
    async def annuler(interaction: discord.Interaction):
        if bot.restart_task and not bot.restart_task.done():
            bot.restart_task.cancel()
            await bot.console.send('say "[Serveur] Redémarrage annulé."')
            await bot.admin_log(interaction.user, "redémarrage annulé")
            return await interaction.response.send_message("Redémarrage annulé.", ephemeral=True)
        await interaction.response.send_message("Aucun redémarrage programmé.", ephemeral=True)

    @tree.command(name="zones", description="Lister les zones PvP (publier:True met à jour le message de #zones-pvp)")
    @staff_only()
    async def zones(interaction: discord.Interaction, publier: bool = False):
        await interaction.response.defer(ephemeral=True)
        out = await bot.console.execute("twdzones list", wait=1.5)
        text = "\n".join(out) or "(aucune réponse)"
        if publier:
            await bot.update_zones_message(out)
            await bot.admin_log(interaction.user, "liste des zones mise à jour dans #zones-pvp")
        await interaction.followup.send(f"```\n{text}\n```" + ("\n✅ #zones-pvp mis à jour." if publier else ""), ephemeral=True)

    @tree.command(name="console", description="Exécuter une commande console du serveur de jeu (staff)")
    @staff_only()
    async def console(interaction: discord.Interaction, commande: str):
        await run(interaction, commande, f"console : {commande}", wait=2.5)



if __name__ == "__main__":
    if not TOKEN:
        raise SystemExit("DISCORD_TOKEN manquant : copiez .env.example en .env et collez-y le jeton du bot.")
    TerminusBot().run(TOKEN, log_handler=None)
