"""Met à jour le message des métiers dans #métiers (texte de structure.METIER_TEXT + boutons).
Modifie le message existant s'il existe, sinon le publie. Ne crée jamais de doublon.
"""

import asyncio

import discord

import bot as B


async def main():
    client = discord.Client(intents=discord.Intents.default())

    @client.event
    async def on_ready():
        try:
            text = B.S.METIER_TEXT
            assert len(text) <= 2000, f"texte trop long ({len(text)})"
            ch = client.get_channel(B.S.CHANNELS["metiers"])
            st = B.load_state()
            msg = None
            if st.get("metier_message_id"):
                try:
                    msg = await ch.fetch_message(st["metier_message_id"])
                    await msg.edit(content=text, view=B.MetierView(), allowed_mentions=discord.AllowedMentions.none())
                    print(f"Message des métiers mis à jour ({len(text)} caractères)")
                except discord.NotFound:
                    msg = None
            if msg is None:
                msg = await ch.send(text, view=B.MetierView(), allowed_mentions=discord.AllowedMentions.none())
                st["metier_message_id"] = msg.id
                B.save_state(st)
                print("Message des métiers publié")
        finally:
            await client.close()

    await client.start(B.TOKEN)


asyncio.run(main())
