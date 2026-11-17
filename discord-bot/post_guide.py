"""Publie le guide dans #guide-du-survivant, ou met à jour les messages déjà publiés par le bot.
Après une modification de guide.py, relancer ce script : les messages existants sont modifiés, pas dupliqués.
"""

import asyncio

import discord

import bot as B
from guide import GUIDE


async def main():
    client = discord.Client(intents=discord.Intents.default())

    @client.event
    async def on_ready():
        try:
            ch = client.get_channel(B.S.CHANNELS["guide"])
            C = B.S.CHANNELS
            ids = {"reglement": C["reglement"], "memorial": C["memorial"], "metiers": C["metiers"],
                   "troc": 1572162155602645002, "zones": C["zones"]}
            texts = [t.format(**ids) for t in GUIDE]
            for i, t in enumerate(texts):
                assert len(t) <= 2000, f"section {i} trop longue ({len(t)})"
            st = B.load_state()
            old = st.get("guide_message_ids", [])
            new_ids = []
            for i, text in enumerate(texts):
                msg = None
                if i < len(old):
                    try:
                        msg = await ch.fetch_message(old[i])
                        await msg.edit(content=text, allowed_mentions=discord.AllowedMentions.none())
                    except discord.NotFound:
                        msg = None
                if msg is None:
                    msg = await ch.send(text, allowed_mentions=discord.AllowedMentions.none(), suppress_embeds=True)
                new_ids.append(msg.id)
            for extra in old[len(texts):]:
                try:
                    await (await ch.fetch_message(extra)).delete()
                except discord.NotFound:
                    pass
            st["guide_message_ids"] = new_ids
            B.save_state(st)
            print(f"Guide : {len(new_ids)} message(s) dans #{ch.name}")
        finally:
            await client.close()

    await client.start(B.TOKEN)


asyncio.run(main())
