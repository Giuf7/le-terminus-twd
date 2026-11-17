"""Publie le règlement (avec bouton d'acceptation) dans #règlement, ou met à jour le message existant du bot."""

import asyncio

import discord

import bot as B


async def main():
    client = discord.Client(intents=discord.Intents.default())

    @client.event
    async def on_ready():
        try:
            ch = client.get_channel(B.S.CHANNELS["reglement"])
            text = B.S.RULES_TEXT.format(guide=B.S.CHANNELS["guide"])
            st = B.load_state()
            if st.get("rules_message_id"):
                try:
                    msg = await ch.fetch_message(st["rules_message_id"])
                    await msg.edit(content=text, view=B.RulesView())
                    print("Règlement mis à jour :", msg.jump_url)
                    return
                except discord.NotFound:
                    pass
            msg = await ch.send(text, view=B.RulesView())
            st["rules_message_id"] = msg.id
            B.save_state(st)
            print("Règlement publié :", msg.jump_url)
        finally:
            await client.close()

    await client.start(B.TOKEN)


asyncio.run(main())
