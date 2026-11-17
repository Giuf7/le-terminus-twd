using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace TWDZones
{
    // "Nous sommes tous infectés" : un rôdeur se relève là où un joueur meurt, quelle que soit la cause.
    // Fait côté serveur (l'ancien game event XML SpawnEntity "Position" ne fonctionnait pas sur un joueur mort, vérifié le 17/11/2026).
    public static class PlayerTurns
    {
        static readonly string[] Walkers = {
            "zombieJoe", "zombieSteve", "zombieTomClark", "zombieBusinessMan", "zombieMoe", "zombieYo",
            "zombieArlene", "zombieDarlene", "zombieMarlene", "zombieNurse", "zombieJanitor",
            "zombieUtilityWorker", "zombieBiker", "zombieLumberjack", "zombieSoldier", "zombieInmate"
        };
        static readonly System.Random rng = new System.Random();

        public static void OnPlayerDied(EntityPlayer p, Entity killer)
        {
            try
            {
                if (p == null || !PvpRules.IsServer()) return;
                World world = GameManager.Instance.World;
                Vector3 pos = p.position + new Vector3(0f, 0.5f, 0f);
                string name = Walkers[rng.Next(Walkers.Length)];
                Entity z = EntityFactory.CreateEntity(EntityClass.FromString(name), pos, new Vector3(0f, p.rotation.y, 0f));
                world.SpawnEntityInWorld(z);
                Log.Out("[TWD_Mort] " + p.EntityName + " se relève en rôdeur (" + name + ") à " + (int)pos.x + ", " + (int)pos.z);
            }
            catch (Exception e) { Log.Warning("[TWD_Mort] rôdeur non créé : " + e.Message); }
        }
    }

    // Mort d'un joueur, vue par le serveur : ModEvents.EntityKilled NE se déclenche PAS pour les joueurs (vérifié le 17/11/2026).
    // Le serveur reçoit en revanche le message de jeu EntityWasKilled (celui qui affiche « Player ... died »).
    [HarmonyPatch(typeof(GameManager), "GameMessageServer")]
    public static class Patch_PlayerDeathMessage
    {
        static readonly Dictionary<int, float> lastDeath = new Dictionary<int, float>();

        static void Postfix(EnumGameMessages _type, int _mainEntityId, int _secondaryEntityId)
        {
            Handle(_type, _mainEntityId, _secondaryEntityId);
        }

        public static void Handle(EnumGameMessages _type, int _mainEntityId, int _secondaryEntityId)
        {
            try
            {
                if (_type != EnumGameMessages.EntityWasKilled || !PvpRules.IsServer()) return;
                World w = GameManager.Instance.World;
                EntityPlayer p = w.GetEntity(_mainEntityId) as EntityPlayer;
                if (p == null) return;
                float last;
                if (lastDeath.TryGetValue(p.entityId, out last) && Time.time - last < 5f) return;   // doublon
                lastDeath[p.entityId] = Time.time;
                Entity killer = _secondaryEntityId >= 0 ? w.GetEntity(_secondaryEntityId) : null;
                JournalDeath.OnPlayerDied(p, killer);   // d'abord le journal : il lit l'état du joueur avant tout changement
                PlayerTurns.OnPlayerDied(p, killer);
            }
            catch (Exception e) { Log.Warning("[TWD_Mort] message de mort : " + e.Message); }
        }
    }

    [HarmonyPatch(typeof(GameManager), "GameMessage")]
    public static class Patch_PlayerDeathMessageLocal
    {
        static void Postfix(EnumGameMessages _type, EntityAlive _mainEntity, EntityAlive _otherEntity)
        {
            if (_mainEntity == null) return;
            Patch_PlayerDeathMessage.Handle(_type, _mainEntity.entityId, _otherEntity == null ? -1 : _otherEntity.entityId);
        }
    }

    // Mort définitive : sur serveur dédié, Progression.ResetProgression du jeu plante (NullReferenceException dans son
    // code de statistiques internes, V 3.4 b10), ce qui annule TOUTE la remise à zéro (niveau, compétences, livres...).
    // Version sûre, même résultat sans les statistiques : chaque compétence revient à son niveau minimum.
    // (Les points de compétence sont ensuite recalculés par ResetPlayerData, qui remet le niveau à 1.)
    [HarmonyPatch(typeof(Progression), "ResetProgression")]
    public static class Patch_SafeResetProgression
    {
        static bool Prefix(Progression __instance, bool _resetSkills, bool _resetBooks, bool _resetCrafting)
        {
            try
            {
                List<ProgressionValue> list = __instance.ProgressionValueQuickList;
                int count = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    ProgressionValue pv = list[i];
                    if (pv == null) continue;
                    ProgressionClass pc = pv.ProgressionClass;
                    if (pc == null) continue;
                    bool reset = (pc.IsBook && _resetBooks) || (pc.IsCrafting && _resetCrafting) || ((pc.IsAttribute || pc.IsPerk) && _resetSkills);
                    if (reset && pv.Level != pc.MinLevel) { pv.Level = pc.MinLevel; count++; }
                }
                __instance.bProgressionStatsChanged = true;
                Log.Out("[TWD_Mort] remise à zéro de la progression : " + count + " valeur(s)");
                return false;   // on ne lance pas la version du jeu (qui planterait)
            }
            catch (Exception e)
            {
                Log.Warning("[TWD_Mort] remise à zéro sûre impossible, version du jeu utilisée : " + e.Message);
                return true;
            }
        }
    }
}
