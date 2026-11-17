"""LECTURE SEULE : liste les derniers messages de chaque salon texte (auteur + début du texte)."""

import asyncio
from pathlib import Path

import discord

import bot as B

OUT = Path(__file__).resolve().parent / "inspect_messages.txt"


async def main():
    client = discord.Client(intents=discord.Intents.default())
    lines = []

    @client.event
    async def on_ready():
        try:
            g = client.get_guild(B.GUILD_ID)
            for ch in g.text_channels:
                msgs = [m async for m in ch.history(limit=15, oldest_first=True)]
                lines.append(f"\n# {ch.name} ({len(msgs)} message(s))")
                for m in msgs:
                    who = "BOT" if m.author.id == g.me.id else m.author.display_name
                    txt = (m.content or ("[embed] " + (m.embeds[0].title or "") if m.embeds else "")).replace("\n", " / ")
                    lines.append(f"   {m.id} {who:12} {txt[:140]}")
        except Exception as exc:
            lines.append(f"ERREUR : {exc!r}")
        finally:
            OUT.write_text("\n".join(lines), encoding="utf-8")
            await client.close()

    await client.start(B.TOKEN)


asyncio.run(main())
