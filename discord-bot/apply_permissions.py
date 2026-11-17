"""Applique le plan de permissions validé le 17/11/2026 (aucun salon créé, renommé ni supprimé).

- Nouveau venu (@everyone) : ne voit que 📌 ACCUEIL, en lecture seule.
- Survivant (a cliqué « J'accepte le règlement ») : lit 📖 LE TERMINUS et #mémorial ; écrit dans ROLEPLAY,
  FACTIONS, HORS-JEU, SUPPORT ; parle dans VOCAL ; ne voit pas 🔒 STAFF.
- Staff / Modérateur : voient et écrivent partout.
- Lecture seule = réactions autorisées. Le bot est administrateur (non concerné).
Relançable sans risque : réécrit les mêmes permissions.
"""

import asyncio

import discord

import bot as B

CAT = {
    "accueil": 1572161499089080410, "terminus": 1572161714026450954, "roleplay": 1572162000933355603,
    "factions": 1572162233545269248, "horsjeu": 1572162414823215214, "support": 1572162577285251122,
    "vocal": 1572162685527785582, "staff": 1572162777458413578,
}
READ_ONLY_CHANNELS = [B.S.CHANNELS["memorial"]]   # dans une catégorie où les survivants écrivent


def P(**kw):
    return discord.PermissionOverwrite(**kw)


async def main():
    client = discord.Client(intents=discord.Intents.default())

    @client.event
    async def on_ready():
        try:
            g = client.get_guild(B.GUILD_ID)
            everyone = g.default_role
            surv = discord.utils.get(g.roles, name=B.S.SURVIVANT)
            staff = discord.utils.get(g.roles, name=B.S.STAFF)
            modo = discord.utils.get(g.roles, name=B.S.MODO)
            team = {staff: P(view_channel=True, send_messages=True, connect=True, speak=True),
                    modo: P(view_channel=True, send_messages=True, connect=True, speak=True)}
            read_only = P(view_channel=True, send_messages=False, add_reactions=True, read_message_history=True)
            write = P(view_channel=True, send_messages=True, add_reactions=True, read_message_history=True)
            hidden = P(view_channel=False)

            plan = {
                "accueil": {everyone: read_only, surv: read_only, **team},
                "terminus": {everyone: hidden, surv: read_only, **team},
                "roleplay": {everyone: hidden, surv: write, **team},
                "factions": {everyone: hidden, surv: write, **team},
                "horsjeu": {everyone: hidden, surv: write, **team},
                "support": {everyone: hidden, surv: write, **team},
                "vocal": {everyone: hidden, surv: P(view_channel=True, connect=True, speak=True), **team},
                "staff": {everyone: hidden, **team},
            }
            for key, ows in plan.items():
                cat = g.get_channel(CAT[key])
                await cat.edit(overwrites=ows, reason="Permissions Le Terminus")
                for ch in cat.channels:
                    if ch.id in READ_ONLY_CHANNELS:
                        await ch.edit(overwrites={everyone: hidden, surv: read_only, **team}, reason="Permissions Le Terminus")
                        print(f"   #{ch.name} : lecture seule pour les survivants")
                    else:
                        await ch.edit(sync_permissions=True, reason="Permissions Le Terminus")
                print(f"OK {cat.name} ({len(cat.channels)} salon(s))")
        finally:
            await client.close()

    await client.start(B.TOKEN)


asyncio.run(main())
