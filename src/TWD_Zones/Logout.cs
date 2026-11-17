// Détection des déconnexions "sauvages" (déconnexion pour fuir un danger).
// AUCUNE conséquence automatique : une ligne est écrite dans le journal du serveur,
// que le bot Discord relaie dans #logs pour que le staff tranche.
// Danger = coup reçu d'un rôdeur ou d'un joueur dans les 5 dernières minutes,
//          rôdeurs à moins de 15 m, ou autre joueur à moins de 50 m dans une zone PvP.

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TWDZones
{
    public static class LogoutWatch
    {
        const float DangerWindow = 300f;   // 5 minutes
        static readonly Dictionary<int, float> lastDanger = new Dictionary<int, float>();
        static readonly Dictionary<int, string> lastDangerWhat = new Dictionary<int, string>();

        public static void RecordDanger(EntityPlayer victim, Entity source)
        {
            if (victim == null) return;
            string what = source is EntityPlayer ? "attaqué par " + ((EntityPlayer)source).EntityName
                        : source is EntityZombie ? "attaqué par un rôdeur"
                        : null;
            if (what == null) return;
            lastDanger[victim.entityId] = Time.time;
            lastDangerWhat[victim.entityId] = what;
        }

        public static void OnDisconnect(ref ModEvents.SPlayerDisconnectedData _data)
        {
            try
            {
                if (_data.GameShuttingDown || _data.ClientInfo == null || GameManager.Instance == null) return;
                World world = GameManager.Instance.World;
                if (world == null) return;
                int id = _data.ClientInfo.entityId;
                EntityPlayer p = world.GetEntity(id) as EntityPlayer;
                var reasons = new List<string>();

                float t;
                if (lastDanger.TryGetValue(id, out t) && Time.time - t < DangerWindow)
                    reasons.Add(lastDangerWhat[id] + " il y a " + Mathf.CeilToInt((Time.time - t) / 60f) + " min");

                if (p != null)
                {
                    var near = new List<Entity>();
                    world.GetEntitiesInBounds(typeof(EntityZombie), new Bounds(p.position, new Vector3(30f, 12f, 30f)), near);
                    int alive = 0;
                    foreach (Entity e in near) if (e != null && !e.IsDead()) alive++;
                    if (alive > 0) reasons.Add(alive + " rôdeur(s) à moins de 15 m");

                    if (ZoneStore.IsPvp(p.position))
                        foreach (EntityPlayer o in world.Players.list)
                            if (o != null && o.entityId != id && (o.position - p.position).magnitude < 50f && ZoneStore.IsPvp(o.position))
                            {
                                reasons.Add("en zone PvP, " + o.EntityName + " à moins de 50 m");
                                break;
                            }
                }

                lastDanger.Remove(id); lastDangerWhat.Remove(id);
                if (reasons.Count == 0) return;
                string name = p != null ? p.EntityName : _data.ClientInfo.playerName;
                string pos = p != null ? string.Format(CultureInfo.InvariantCulture, "{0:0}, {1:0}", p.position.x, p.position.z) : "?, ?";
                // Ligne lue par le bot Discord (format à ne pas modifier sans adapter bot.py)
                Log.Out("[TWD_Logout] DECO_SAUVAGE '" + name + "' pos=(" + pos + ") : " + string.Join(" ; ", reasons.ToArray()));
            }
            catch (Exception ex)
            {
                Debug.LogError("[TWD_Logout] " + ex);
            }
        }
    }
}
