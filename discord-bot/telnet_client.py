"""Connexion telnet à la console du serveur 7 Days to Die.

Le serveur envoie en continu tout son journal sur telnet. On s'en sert pour :
- lire les événements (chat, connexions, morts) ligne par ligne ;
- envoyer des commandes console (say, kick, lp, twdzones...) et récupérer leur réponse.
Les lignes du journal commencent par un horodatage ISO ; la réponse d'une commande, non.
"""

import asyncio
import logging
import re

log = logging.getLogger("terminus.telnet")

LOG_LINE = re.compile(r"^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\d ")


class TelnetConsole:
    def __init__(self, host: str, port: int, password: str, on_line):
        self.host = host
        self.port = port
        self.password = password
        self.on_line = on_line              # coroutine appelée pour chaque ligne du journal
        self._writer: asyncio.StreamWriter | None = None
        self._capture: list[str] | None = None
        self._lock = asyncio.Lock()
        self.connected = asyncio.Event()

    async def run_forever(self):
        """Se connecte et se reconnecte indéfiniment (le serveur redémarre, plante...)."""
        while True:
            try:
                await self._session()
            except (OSError, asyncio.IncompleteReadError, ConnectionError) as exc:
                log.warning("Telnet indisponible (%s), nouvelle tentative dans 15 s", exc)
            except Exception:
                log.exception("Erreur telnet inattendue")
            self.connected.clear()
            self._writer = None
            await asyncio.sleep(15)

    async def _session(self):
        reader, writer = await asyncio.open_connection(self.host, self.port)
        self._writer = writer
        log.info("Connecté à la console %s:%s", self.host, self.port)
        buf = b""
        authed = not self.password
        if authed:
            self.connected.set()
        while True:
            chunk = await reader.read(4096)
            if not chunk:
                raise ConnectionError("connexion fermée par le serveur")
            buf += chunk
            if not authed and b"password" in buf.lower():
                writer.write((self.password + "\r\n").encode())
                await writer.drain()
                authed = True
                buf = b""
                self.connected.set()
                continue
            while b"\n" in buf:
                raw, buf = buf.split(b"\n", 1)
                line = raw.decode("utf-8", errors="replace").rstrip("\r")
                if not line:
                    continue
                if self._capture is not None and not LOG_LINE.match(line):
                    self._capture.append(line)
                try:
                    await self.on_line(line)
                except Exception:
                    log.exception("Erreur en traitant la ligne : %s", line)

    async def send(self, command: str):
        await asyncio.wait_for(self.connected.wait(), timeout=10)
        self._writer.write((command + "\r\n").encode("utf-8"))
        await self._writer.drain()

    async def execute(self, command: str, wait: float = 2.0) -> list[str]:
        """Envoie une commande et renvoie les lignes de réponse (hors journal)."""
        async with self._lock:
            self._capture = []
            try:
                await self.send(command)
                await asyncio.sleep(wait)
                return [l for l in self._capture if l.strip()]
            finally:
                self._capture = None


def quote(text: str) -> str:
    """Prépare un texte pour une commande console entre guillemets."""
    return text.replace("\\", "").replace('"', "'").replace("\n", " ").strip()
