"""Etat des lieux du serveur Discord, en LECTURE SEULE (ne modifie rien).

Liste les rôles, catégories et salons, et lit le journal d'audit pour retrouver
ce que le bot a créé ou modifié. Résultat écrit dans inspect_report.txt.
Usage : .venv\\Scripts\\python.exe inspect_guild.py
"""

import asyncio
from pathlib import Path

import discord

import bot as B

OUT = Path(__file__).resolve().parent / "inspect_report.txt"


async def main():
    intents = discord.Intents.default()
    client = discord.Client(intents=intents)
    lines = []

    @client.event
    async def on_ready():
        try:
            g = client.get_guild(B.GUILD_ID)
            me = g.me
            lines.append(f"Serveur : {g.name}  (bot : {me}, id {me.id})")

            lines.append("\n=== ROLES (du plus haut au plus bas) ===")
            for r in sorted(g.roles, key=lambda r: r.position, reverse=True):
                lines.append(f"[{r.position:>2}] {r.name!r:40} id={r.id} créé={r.created_at:%Y-%m-%d %H:%M} membres={len(r.members)}")

            lines.append("\n=== CATEGORIES ET SALONS ===")
            for cat, chans in g.by_category():
                head = f"{cat.name!r} (id={cat.id}, créé={cat.created_at:%Y-%m-%d %H:%M})" if cat else "(sans catégorie)"
                lines.append(f"\n# {head}")
                for c in chans:
                    kind = type(c).__name__.replace("Channel", "")
                    lines.append(f"   - {c.name!r:30} {kind:6} id={c.id} créé={c.created_at:%Y-%m-%d %H:%M}")

            lines.append("\n=== JOURNAL D'AUDIT : actions du bot ===")
            async for e in g.audit_logs(limit=500, user=me):
                target = getattr(e.target, "name", None) or str(e.target)
                changes = ""
                if e.action.name in ("channel_update", "overwrite_create", "overwrite_update", "overwrite_delete"):
                    try:
                        before = {k: v for k, v in iter(e.before)}
                        after = {k: v for k, v in iter(e.after)}
                        changes = f"  avant={before}  après={after}"
                    except Exception:
                        pass
                lines.append(f"{e.created_at:%H:%M:%S} {e.action.name:18} {target!r}{changes}"[:600])
        except Exception as exc:
            lines.append(f"ERREUR : {exc!r}")
        finally:
            OUT.write_text("\n".join(lines), encoding="utf-8")
            await client.close()

    await client.start(B.TOKEN)


asyncio.run(main())
