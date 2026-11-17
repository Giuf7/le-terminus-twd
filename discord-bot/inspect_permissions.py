"""LECTURE SEULE : permissions de chaque catégorie et salon (qui peut voir / écrire), et permissions des rôles.
Résultat : inspect_permissions.txt"""

import asyncio
from pathlib import Path

import discord

import bot as B

OUT = Path(__file__).resolve().parent / "inspect_permissions.txt"
KEYS = ["view_channel", "send_messages", "read_message_history", "add_reactions", "connect", "speak", "manage_messages"]


def fmt(ow: discord.PermissionOverwrite) -> str:
    allow, deny = ow.pair()
    a = [k for k in KEYS if getattr(allow, k)]
    d = [k for k in KEYS if getattr(deny, k)]
    return ("+" + ",".join(a) if a else "") + (" -" + ",".join(d) if d else "")


async def main():
    client = discord.Client(intents=discord.Intents.default())
    lines = []

    @client.event
    async def on_ready():
        try:
            g = client.get_guild(B.GUILD_ID)
            lines.append("=== RÔLES (permissions serveur notables) ===")
            for r in sorted(g.roles, key=lambda r: r.position, reverse=True):
                p = r.permissions
                flags = [k for k in ("administrator", "manage_guild", "manage_channels", "manage_roles", "kick_members",
                                     "ban_members", "manage_messages", "moderate_members", "view_channel", "send_messages")
                         if getattr(p, k)]
                lines.append(f"{r.name!r:28} {', '.join(flags)}")
            lines.append("\n=== CATÉGORIES ET SALONS (overwrites ; 'sync' = aligné sur la catégorie) ===")
            for cat, chans in g.by_category():
                if cat:
                    ows = "; ".join(f"{t.name}: {fmt(o)}" for t, o in cat.overwrites.items()) or "aucune"
                    lines.append(f"\n# {cat.name}  [{ows}]")
                for c in chans:
                    sync = " (sync)" if cat and c.permissions_synced else ""
                    ows = "; ".join(f"{t.name}: {fmt(o)}" for t, o in c.overwrites.items()) or "aucune"
                    lines.append(f"   - {c.name}{sync}  [{ows}]")
        finally:
            OUT.write_text("\n".join(lines), encoding="utf-8")
            await client.close()

    await client.start(B.TOKEN)


asyncio.run(main())
