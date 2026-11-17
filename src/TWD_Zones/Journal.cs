using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace TWDZones
{
    // Journal d'événements TWD, côté serveur : coups de rôdeur, infection, amputation, morts, métiers, magazines, véhicules.
    // Chaque ligne va dans le journal du serveur ("[TWD_Journal] ...") ET dans <sauvegarde>/twd_journal.log.
    public static class Journal
    {
        static readonly object fileLock = new object();

        public static void Write(string category, string msg)
        {
            try
            {
                string when = "";
                World w = GameManager.Instance == null ? null : GameManager.Instance.World;
                if (w != null)
                    when = "Jour " + GameUtils.WorldTimeToDays(w.worldTime) + " " + GameUtils.WorldTimeToHours(w.worldTime).ToString("00") + ":" + GameUtils.WorldTimeToMinutes(w.worldTime).ToString("00") + " | ";
                string line = "[" + category + "] " + msg;
                Log.Out("[TWD_Journal] " + line);
                lock (fileLock)
                    File.AppendAllText(Path.Combine(GameIO.GetSaveGameDir(), "twd_journal.log"),
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + when + line + Environment.NewLine);
            }
            catch (Exception e) { Log.Warning("[TWD_Journal] écriture impossible : " + e.Message); }
        }

        public static string Name(Entity e)
        {
            if (e == null) return "?";
            EntityPlayer p = e as EntityPlayer;
            if (p != null) return "'" + p.EntityName + "'";
            return EntityClass.GetEntityClassName(e.entityClass);
        }

        public static string Pos(Entity e) { return "(" + (int)e.position.x + ", " + (int)e.position.z + ")"; }
        public static string Pos(Vector3 p) { return "(" + (int)p.x + ", " + (int)p.z + ")"; }
    }

    // Dernier coup reçu par chaque joueur (sert à expliquer une infection ou une mort)
    public class LastHit
    {
        public float Time;
        public int SourceId;
        public string SourceName;
        public bool Zombie;
        public bool Critical;
        public EnumDamageTypes Type;
    }

    // ------------------------------------------------------------------ coups reçus par un joueur
    [HarmonyPatch(typeof(EntityPlayer), "DamageEntity")]
    public static class Patch_JournalDamage
    {
        public static readonly Dictionary<int, LastHit> Last = new Dictionary<int, LastHit>();

        // Dégâts calculés par le serveur (rare pour un joueur : pièges, explosions...)
        static void Postfix(EntityPlayer __instance, DamageSource _damageSource, int _strength, bool _criticalHit, int __result)
        {
            try
            {
                if (!PvpRules.IsServer() || _damageSource == null) return;
                Record(__instance, GameManager.Instance.World.GetEntity(_damageSource.getEntityId()), _strength, _criticalHit, _damageSource.GetDamageType());
            }
            catch (Exception e) { Log.Warning("[TWD_Journal] coup : " + e.Message); }
        }

        static readonly Dictionary<int, float> lastLogged = new Dictionary<int, float>();

        public static void Record(EntityPlayer p, Entity src, int strength, bool critical, EnumDamageTypes type)
        {
            var h = new LastHit { Time = UnityEngine.Time.time, SourceId = src == null ? -1 : src.entityId, SourceName = Journal.Name(src),
                                  Zombie = src is EntityZombie, Critical = critical, Type = type };
            Last[p.entityId] = h;
            if (!h.Zombie || strength <= 0) return;
            float t; // un même coup peut arriver par deux chemins : une seule ligne
            if (lastLogged.TryGetValue(p.entityId, out t) && UnityEngine.Time.time - t < 0.3f) return;
            lastLogged[p.entityId] = UnityEngine.Time.time;
            Journal.Write("Coup", Journal.Name(p) + " frappé par " + h.SourceName + " : " + strength + " dégâts" + (critical ? " (critique)" : "") + ", PV ≈ " + Math.Max(0, p.Health) + " " + Journal.Pos(p));
        }
    }

    // Sur serveur dédié, c'est le jeu du JOUEUR qui calcule le coup d'un rôdeur et l'envoie au serveur dans ce paquet
    [HarmonyPatch(typeof(NetPackageDamageEntity), "ProcessPackage")]
    public static class Patch_JournalDamagePackage
    {
        static void Postfix(NetPackageDamageEntity __instance, World _world)
        {
            try
            {
                if (!PvpRules.IsServer() || _world == null) return;
                EntityPlayer p = _world.GetEntity(__instance.entityId) as EntityPlayer;
                if (p == null) return;
                bool critical = (__instance.flags & (uint)NetPackageDamageEntity.cFlagsCritical) != 0;
                Patch_JournalDamage.Record(p, _world.GetEntity(__instance.attackerEntityId), __instance.strength, critical, __instance.damageTyp);
            }
            catch (Exception e) { Log.Warning("[TWD_Journal] coup (paquet) : " + e.Message); }
        }
    }

    // ------------------------------------------------------------------ état des joueurs (lu toutes les secondes, via leurs buffs)
    public class PlayerState
    {
        public int Stage;          // 0 = sain, 1-3 = stades, 4 = phase finale
        public bool Slowed;        // infection ralentie (antibiotiques, miel)
        public bool Hemorrhage;
        public float HemorrhageStart;   // Time.time du début de l'hémorragie
        public bool HemorrhageLight;    // hémorragie légère du kit chirurgical (45 s au lieu de 90)
        public bool Amputated;
        public string Metier = "";
    }

    public static class JournalWatch
    {
        static readonly string[] Metiers = { "Medecin", "Chasseur", "Soldat", "Mecanicien", "Ingenieur", "Fermier", "Batisseur" };
        static readonly Dictionary<int, PlayerState> states = new Dictionary<int, PlayerState>();
        static float next;

        public static PlayerState Current(EntityPlayer p)
        {
            EntityBuffs b = p.Buffs;
            var s = new PlayerState();
            if (b == null) return s;
            if (b.HasBuff("buffInfection04")) s.Stage = 4;
            else if (b.HasBuff("buffInfection03Untreated") || b.HasBuff("buffInfection03GetBetter")) s.Stage = 3;
            else if (b.HasBuff("buffInfection02Untreated") || b.HasBuff("buffInfection02GetBetter")) s.Stage = 2;
            else if (b.HasBuff("buffInfection01Untreated") || b.HasBuff("buffInfection01GetBetter") || b.HasBuff("buffInfectionMain")) s.Stage = 1;
            s.Slowed = b.HasBuff("buffInfection01GetBetter") || b.HasBuff("buffInfection02GetBetter") || b.HasBuff("buffInfection03GetBetter");
            s.HemorrhageLight = b.HasBuff("buffTWDHemorrhageLight");
            s.Hemorrhage = b.HasBuff("buffTWDHemorrhage") || s.HemorrhageLight;
            s.Amputated = b.HasBuff("buffTWDAmputated");
            foreach (string m in Metiers) if (b.HasBuff("buffTWDMetier" + m)) { s.Metier = m; break; }
            return s;
        }

        public static PlayerState Previous(int entityId)
        {
            PlayerState s; return states.TryGetValue(entityId, out s) ? s : null;
        }

        public static void Forget(int entityId) { states.Remove(entityId); }

        public static void OnGameUpdate(ref ModEvents.SGameUpdateData _data)
        {
            if (Time.time < next) return;
            next = Time.time + 1f;
            try
            {
                if (!PvpRules.IsServer() || GameManager.Instance == null || GameManager.Instance.World == null) return;
                List<EntityPlayer> players = GameManager.Instance.World.Players.list;
                for (int i = 0; i < players.Count; i++)
                {
                    EntityPlayer p = players[i];
                    if (p == null || p.IsDead()) continue;
                    PlayerState now = Current(p);
                    PlayerState was;
                    if (!states.TryGetValue(p.entityId, out was)) { states[p.entityId] = now; continue; }   // connexion : pas d'événement
                    Compare(p, was, now);
                    states[p.entityId] = now;
                }
            }
            catch (Exception e) { Log.Warning("[TWD_Journal] suivi : " + e.Message); }
        }

        static readonly string[] StageNames = { "sain", "stade 1 (morsure)", "stade 2 (propagation)", "stade 3 (fièvre)", "phase finale" };

        static void Compare(EntityPlayer p, PlayerState was, PlayerState now)
        {
            string who = Journal.Name(p);
            // Infection
            if (was.Stage == 0 && now.Stage > 0)
                Journal.Write("Infection", who + " est contaminé : " + Cause(p) + " " + Journal.Pos(p));
            else if (now.Stage > was.Stage && was.Stage > 0)
                Journal.Write("Infection", who + " passe au " + StageNames[now.Stage]);
            if (now.Stage > 0 && now.Slowed && !was.Slowed)
                Journal.Write("Infection", who + " ralentit son infection (antibiotiques / miel), " + StageNames[now.Stage]);
            // Amputation (le stade AVANT l'amputation dit si elle a réussi)
            if (now.Amputated && !was.Amputated)
            {
                if (was.Stage == 1) Journal.Write("Amputation", who + " : amputation RÉUSSIE au stade 1, infection stoppée. Malus Amputé.");
                else if (was.Stage >= 2) Journal.Write("Amputation", who + " : amputation TROP TARD (" + StageNames[was.Stage] + "), membre perdu, l'infection continue. Malus Amputé.");
                else Journal.Write("Amputation", who + " : amputation sans infection. Malus Amputé.");
            }
            else if (was.Stage > 0 && now.Stage == 0)
                Journal.Write("Infection", who + " n'a plus d'infection");
            now.HemorrhageStart = was.HemorrhageStart;
            if (now.Hemorrhage && !was.Hemorrhage) { now.HemorrhageStart = Time.time; Journal.Write("Amputation", who + " : hémorragie"); }
            if (!now.Hemorrhage && was.Hemorrhage)
            {
                // L'hémorragie d'amputation dure 90 s (45 s pour la légère du kit chirurgical) : proche de cette durée, elle s'est terminée seule
                float lasted = Time.time - was.HemorrhageStart;
                float natural = was.HemorrhageLight ? 40f : 85f;
                if (was.HemorrhageStart > 0f && lasted < natural) Journal.Write("Amputation", who + " : hémorragie arrêtée par un soin après " + (int)lasted + " s");
                else Journal.Write("Amputation", who + " : hémorragie terminée d'elle-même (" + (int)lasted + " s, aucun soin)");
            }
            // Métier
            if (now.Metier != was.Metier && now.Metier != "")
                Journal.Write("Métier", who + " choisit le métier " + now.Metier);
        }

        static string Cause(EntityPlayer p)
        {
            LastHit h;
            if (Patch_JournalDamage.Last.TryGetValue(p.entityId, out h) && Time.time - h.Time < 5f)
            {
                if (h.Zombie) return (h.Critical ? "coup critique de " : "morsure de ") + h.SourceName;
                return "après un coup de " + h.SourceName;
            }
            return "cause inconnue (aucun coup reçu juste avant : objet admin ?)";
        }
    }

    // ------------------------------------------------------------------ morts
    public static class JournalDeath
    {
        public static void OnPlayerDied(EntityPlayer p, Entity k)
        {
            try
            {
                if (p == null || !PvpRules.IsServer()) return;
                PlayerState s = JournalWatch.Previous(p.entityId) ?? JournalWatch.Current(p);
                string cause;
                LastHit h;
                bool recent = Patch_JournalDamage.Last.TryGetValue(p.entityId, out h) && Time.time - h.Time < 10f;
                if (k is EntityPlayer) cause = "tué par le joueur " + Journal.Name(k);
                else if (k is EntityZombie) cause = "tué par un rôdeur (" + Journal.Name(k) + ")";
                else if (k != null && k != p) cause = "tué par " + Journal.Name(k);
                else if (s.Stage >= 3) cause = "emporté par l'infection";
                else if (recent && h.Type == EnumDamageTypes.Falling) cause = "chute";
                // Le message de mort ne donne pas toujours le tueur : on se rabat sur le dernier coup reçu
                else if (recent && h.Zombie) cause = "tué par un rôdeur (" + h.SourceName + ")";
                else if (recent && h.SourceName != null && h.SourceName.StartsWith("animal")) cause = "tué par un animal (" + h.SourceName + ")";
                else if (recent) cause = h.Type + (h.SourceId >= 0 ? " (" + h.SourceName + ")" : "");
                else cause = "cause inconnue";
                int minutes = (int)p.currentLife;
                Journal.Write("Mort", Journal.Name(p) + " est mort : " + cause + " " + Journal.Pos(p)
                    + " | a survécu " + (minutes / 60) + " h " + (minutes % 60).ToString("00") + " (temps réel)"
                    + " | métier perdu : " + (s.Metier == "" ? "aucun" : s.Metier)
                    + (s.Stage > 0 ? " | était infecté (" + s.Stage + ")" : ""));
                JournalWatch.Forget(p.entityId);
                Patch_JournalDamage.Last.Remove(p.entityId);
            }
            catch (Exception e) { Log.Warning("[TWD_Journal] mort : " + e.Message); }
        }
    }

    // ------------------------------------------------------------------ game events TWD (magazine refusé, point de métier offert)
    [HarmonyPatch(typeof(GameEventManager), "HandleAction", new Type[] { typeof(string), typeof(EntityPlayer), typeof(Entity), typeof(bool), typeof(string), typeof(string), typeof(bool), typeof(bool), typeof(string), typeof(GameEventActionSequence) })]
    public static class Patch_JournalGameEvent
    {
        static void Postfix(string name, EntityPlayer requester, Entity entity)
        {
            try
            {
                if (!PvpRules.IsServer() || name == null) return;
                Entity who = (Entity)requester ?? entity;
                if (name.StartsWith("twd_refund_"))
                    Journal.Write("Métier", Journal.Name(who) + " : magazine refusé (hors métier) et rendu : " + name.Substring("twd_refund_".Length));
                else if (name == "twd_metier_intro")
                    Journal.Write("Métier", Journal.Name(who) + " : nouveau survivant, point de métier offert");
            }
            catch (Exception e) { Log.Warning("[TWD_Journal] game event : " + e.Message); }
        }
    }
}
