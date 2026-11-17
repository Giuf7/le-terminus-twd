"""Publie / met à jour le message de #zones-pvp à partir des zones du serveur de jeu (via telnet).
Recharge d'abord zones.xml côté serveur (twdzones reload). Serveur injoignable -> liste vide."""

import asyncio

import discord

import bot as B
from telnet_client import TelnetConsole


async def ignore(_line):
    pass


async def main():
    lines = []
    console = TelnetConsole(B.TELNET_HOST, B.TELNET_PORT, B.TELNET_PASSWORD, ignore)
    task = asyncio.create_task(console.run_forever())
    try:
        await asyncio.wait_for(console.connected.wait(), 10)
        print("reload :", await console.execute("twdzones reload", wait=1.0))
        lines = await console.execute("twdzones list", wait=1.5)
    except asyncio.TimeoutError:
        print("Serveur de jeu injoignable : liste vide.")
    task.cancel()

    client = discord.Client(intents=discord.Intents.default())

    @client.event
    async def on_ready():
        try:
            text = await B.write_zones_message(client, lines)
            print(f"#zones-pvp à jour ({len(text)} caractères), zones lues : {lines}")
        finally:
            await client.close()

    await client.start(B.TOKEN)


asyncio.run(main())
