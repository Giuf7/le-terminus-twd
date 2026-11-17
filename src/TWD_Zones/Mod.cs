using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace TWDZones
{
    public class TWDZonesMod : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            ZoneStore.FilePath = Path.Combine(_modInstance.Path, "zones.xml");
            try { Debug.Log("[TWD_Zones] " + ZoneStore.Load()); }
            catch (Exception ex) { Debug.LogError("[TWD_Zones] Erreur de lecture de zones.xml : " + ex.Message); }

            new Harmony("twd.zones").PatchAll(typeof(TWDZonesMod).Assembly);
            ModEvents.GameUpdate.RegisterHandler(Notifier.OnGameUpdate);
            ModEvents.PlayerDisconnected.RegisterHandler(LogoutWatch.OnDisconnect);
            ModEvents.GameUpdate.RegisterHandler(JournalWatch.OnGameUpdate);
            Debug.Log("[TWD_Zones] Initialisé : PvP joueur contre joueur limité aux zones PvP.");
        }
    }

    public static class PvpRules
    {
        // true = les dégâts sont autorisés. Ne bloque QUE joueur -> autre joueur.
        public static bool Allowed(World world, int attackerId, int targetId)
        {
            if (world == null || attackerId == targetId || attackerId < 0) return true;
            Entity source = world.GetEntity(attackerId);
            EntityPlayer target = world.GetEntity(targetId) as EntityPlayer;
            EntityPlayer attacker = source as EntityPlayer;
            if (attacker == null || target == null)
            {
                LogoutWatch.RecordDanger(target, source);   // coup de rôdeur : danger pour la détection des déconnexions
                return true;
            }
            if (ZoneStore.IsPvp(attacker.position) && ZoneStore.IsPvp(target.position))
            {
                LogoutWatch.RecordDanger(target, attacker);
                return true;
            }
            Notifier.WarnAttacker(attackerId);
            return false;
        }

        public static bool IsServer()
        {
            ConnectionManager cm = ConnectionManager.Instance;
            return cm != null && cm.IsServer;
        }
    }

    // Coups calculés par le client de l'attaquant (mêlée, armes) et envoyés au serveur
    [HarmonyPatch(typeof(NetPackageDamageEntity), "ProcessPackage")]
    public static class Patch_DamageEntityPackage
    {
        public static bool Prefix(NetPackageDamageEntity __instance, World _world)
        {
            if (!PvpRules.IsServer()) return true;
            return PvpRules.Allowed(_world, __instance.attackerEntityId, __instance.entityId);
        }
    }

    [HarmonyPatch(typeof(NetPackageRangeCheckDamageEntity), "ProcessPackage")]
    public static class Patch_RangeCheckDamagePackage
    {
        public static bool Prefix(NetPackageRangeCheckDamageEntity __instance, World _world)
        {
            if (!PvpRules.IsServer()) return true;
            return PvpRules.Allowed(_world, __instance.attackerEntityId, __instance.entityId);
        }
    }

    // Dégâts calculés directement par le serveur (explosions, pièges posés par un joueur...)
    [HarmonyPatch(typeof(EntityPlayer), "DamageEntity")]
    public static class Patch_PlayerDamageEntity
    {
        public static bool Prefix(EntityPlayer __instance, DamageSource _damageSource, ref int __result)
        {
            if (!PvpRules.IsServer() || _damageSource == null) return true;
            int src = _damageSource.getEntityId();
            if (PvpRules.Allowed(__instance.world, src, __instance.entityId)) return true;
            __result = 0;
            return false;
        }
    }

    // Messages d'entrée / sortie de zone et avertissement des attaquants
    public static class Notifier
    {
        static float nextCheck;
        static readonly Dictionary<int, string> lastZone = new Dictionary<int, string>();
        static readonly Dictionary<int, float> lastWarn = new Dictionary<int, float>();

        public static void OnGameUpdate(ref ModEvents.SGameUpdateData _data)
        {
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + 1f;
            if (!PvpRules.IsServer() || GameManager.Instance == null) return;
            World world = GameManager.Instance.World;
            if (world == null) return;

            var seen = new HashSet<int>();
            List<EntityPlayer> players = world.Players.list;
            for (int i = 0; i < players.Count; i++)
            {
                EntityPlayer p = players[i];
                if (p == null) continue;
                seen.Add(p.entityId);
                Zone z = ZoneStore.ZoneAt(p.position);
                bool pvp = z == null ? ZoneStore.DefaultPvp : z.Pvp;
                string key = (z == null ? "" : z.Name) + "|" + pvp;
                string prev;
                bool known = lastZone.TryGetValue(p.entityId, out prev);
                lastZone[p.entityId] = key;
                bool wasPvp = known && prev.EndsWith("|True");
                // Indicateur HUD (buffTWDZonePvP, mod TWD_UI) : aussi à la connexion, pour corriger un buff resté d'une session précédente
                if (!known || pvp != wasPvp) SetPvpIndicator(p.entityId, pvp);
                if (!known || prev == key) continue;   // pas de message à la connexion

                if (pvp && !wasPvp)
                    Send(p.entityId, "[ff4040]ZONE PvP" + (z != null ? " : " + z.Name : "") + "[-] - les autres survivants peuvent vous tuer.", "ui_denied");
                else if (!pvp && wasPvp)
                    Send(p.entityId, "[40ff40]Vous quittez la zone PvP[-]" + (z != null ? " (" + z.Name + ")" : "") + ".", "ui_tab");
            }
            // Nettoyage des joueurs déconnectés
            var gone = new List<int>();
            foreach (int id in lastZone.Keys) if (!seen.Contains(id)) gone.Add(id);
            foreach (int id in gone) { lastZone.Remove(id); lastWarn.Remove(id); }
        }

        public static void WarnAttacker(int attackerId)
        {
            float last;
            if (lastWarn.TryGetValue(attackerId, out last) && Time.time - last < 5f) return;
            lastWarn[attackerId] = Time.time;
            Send(attackerId, "[ffd040]Zone sûre : vous ne pouvez pas blesser un autre survivant ici.[-]", "ui_denied");
        }

        // Passe par les commandes officielles du serveur, qui transmettent le buff au jeu du joueur
        static void SetPvpIndicator(int entityId, bool on)
        {
            try
            {
                SdtdConsole.Instance.ExecuteSync((on ? "buffplayer " : "debuffplayer ") + entityId + " buffTWDZonePvP", null);
            }
            catch (Exception e) { Log.Warning("[TWD_Zones] indicateur PvP : " + e.Message); }
        }

        static void Send(int entityId, string msg, string sound)
        {
            ConnectionManager cm = ConnectionManager.Instance;
            if (cm == null || cm.Clients == null) return;
            ClientInfo ci = cm.Clients.ForEntityId(entityId);
            if (ci == null) return;
            // Un son null n'est pas transmis : le message ne s'affichait pas (sortie de zone, 17/11/2026)
            ci.SendPackage(NetPackageManager.GetPackage<NetPackageShowToolbeltMessage>().Setup(msg, string.IsNullOrEmpty(sound) ? "ui_tab" : sound));
        }
    }
}
