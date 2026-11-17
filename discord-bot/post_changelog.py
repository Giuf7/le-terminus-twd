"""Publie dans #changelog les versions de CHANGELOG.md pas encore publiées (de la plus ancienne à la plus récente).

Les versions déjà publiées sont mémorisées dans state.json ("changelog_posted") : relancer le script ne crée
jamais de doublon. Pour corriger une version déjà publiée, modifier le message à la main sur Discord.
Usage : .venv\\Scripts\\python.exe post_changelog.py [--dry-run]
"""

import asyncio
import re
import sys
from pathlib import Path

import discord

import bot as B

CHANGELOG = Path(__file__).resolve().parent.parent / "CHANGELOG.md"
HEADER = re.compile(r"^## (?P<version>\S+) – (?P<title>.+)$", re.M)


def parse() -> list[tuple[str, str]]:
    text = re.sub(r"<!--.*?-->", "", CHANGELOG.read_text(encoding="utf-8"), flags=re.S)
    parts = HEADER.split(text)
    entries = []
    for i in range(1, len(parts), 3):
        version, title, body = parts[i], parts[i + 1].strip(), parts[i + 2].strip()
        entries.append((version, f"# 📋 Version {version} – {title}\n{body}"))
    return list(reversed(entries))   # de la plus ancienne à la plus récente


async def main(dry_run: bool):
    entries = parse()
    state = B.load_state()
    done = set(state.get("changelog_posted", []))
    todo = [(v, t) for v, t in entries if v not in done]
    for v, t in todo:
        if len(t) > 2000:
            raise SystemExit(f"Version {v} trop longue ({len(t)} caractères, max 2000)")
    if dry_run or not todo:
        for v, t in todo:
            print(f"--- à publier : {v}\n{t}\n")
        print(f"{len(todo)} version(s) à publier" + (" (simulation)" if dry_run else ""))
        return

    client = discord.Client(intents=discord.Intents.default())

    @client.event
    async def on_ready():
        try:
            ch = client.get_channel(B.S.CHANNELS["changelog"])
            for v, t in todo:
                await ch.send(t, allowed_mentions=discord.AllowedMentions.none(), suppress_embeds=True)
                state.setdefault("changelog_posted", []).append(v)
                B.save_state(state)
                print(f"Publié dans #{ch.name} : version {v}")
        finally:
            await client.close()

    await client.start(B.TOKEN)


asyncio.run(main("--dry-run" in sys.argv))
