// FACTIONS (décisions du 17/11/2026)
// Le groupe du jeu ("party", 8 joueurs max) sert de base ; la faction est enregistrée par le serveur et survit
// aux déconnexions et aux redémarrages (le jeu, lui, retire un joueur de son groupe à la déconnexion).
//  - Fondation : un joueur sans faction invite quelqu'un -> faction créée, l'inviteur en est le chef.
//  - Accepter une invitation = rejoindre la faction de l'inviteur (groupe de l'inviteur, ou un autre groupe de la
//    faction s'il est plein : une faction peut dépasser 8 membres).
//  - Quitter / changer de groupe du jeu ne fait PAS quitter la faction : commande /faction quitter.
//  - Reconnexion : le serveur remet le joueur dans un groupe de sa faction.
//  - Mort (mort définitive) : le survivant quitte la faction ; le nouveau personnage devra être réinvité.
//  - Délai : 24 h réelles après avoir quitté une faction ou en avoir été exclu, avant d'en rejoindre une.
//  - Entre membres d'une même faction : aucun dégât, même en zone PvP.
//  - Limite de véhicules (TWD_Vehicles) : comptée sur la faction.
// Fichier : <sauvegarde>/twd_factions.xml

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using HarmonyLib;
using UnityEngine;

namespace TWDZones
{
    public class Faction
    {
        public int Id;
        public string Name = "";
        public string Leader = "";                                              // identifiant de plateforme
        public Dictionary<string, string> Members = new Dictionary<string, string>();   // identifiant -> dernier nom connu
        public string Display { get { return Name != "" ? Name : "Faction n°" + Id; } }
    }

    public static class Factions
    {
        public const double CooldownHours = 24;
        public static readonly Dictionary<int, Faction> All = new Dictionary<int, Faction>();
        static readonly Dictionary<string, DateTime> leftAt = new Dictionary<string, DateTime>();   // identifiant -> date de départ (UTC)
        static int nextId = 1;
        static bool loaded;

        // ------------------------------------------------------------ identité
        public static string Key(EntityPlayer p)
        {
            if (p == null || GameManager.Instance.persistentPlayers == null) return null;
            PersistentPlayerData d = GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(p.entityId);
            return d == null ? null : d.PrimaryId.CombinedString;
        }

        public static Faction Of(string key)
        {
            EnsureLoaded();
            if (key == null) return null;
            foreach (Faction f in All.Values) if (f.Members.ContainsKey(key)) return f;
            return null;
        }

        public static Faction Of(EntityPlayer p) { return Of(Key(p)); }

        // Clés des membres de la faction du joueur (lui seul s'il n'en a pas) : utilisé par la limite de véhicules
        public static HashSet<string> MemberKeys(EntityPlayer p)
        {
            var set = new HashSet<string>();
            string k = Key(p);
            if (k != null) set.Add(k);
            Faction f = Of(k);
            if (f != null) foreach (string m in f.Members.Keys) set.Add(m);
            return set;
        }

        public static bool SameFaction(EntityPlayer a, EntityPlayer b)
        {
            Faction fa = Of(a);
            return fa != null && fa == Of(b);
        }

        public static double CooldownLeftHours(string key)
        {
            DateTime t;
            if (key == null || !leftAt.TryGetValue(key, out t)) return 0;
            double left = CooldownHours - (DateTime.UtcNow - t).TotalHours;
            return left > 0 ? left : 0;
        }

        // ------------------------------------------------------------ appartenance
        public static Faction Found(EntityPlayer leader)
        {
            var f = new Faction();
            f.Id = nextId++;
            f.Leader = Key(leader);
            f.Members[f.Leader] = leader.EntityName;
            All[f.Id] = f;
            Journal.Write("Faction", Journal.Name(leader) + " fonde la " + f.Display);
            return f;
        }

        public static void Join(Faction f, EntityPlayer p)
        {
            string k = Key(p);
            if (f == null || k == null) return;
            f.Members[k] = p.EntityName;
            leftAt.Remove(k);
            Save();
            Journal.Write("Faction", Journal.Name(p) + " rejoint la " + f.Display);
            TellFaction(f, "[40ff40]" + p.EntityName + " rejoint la faction.[-]", -1);
        }

        // Retire un membre (départ, exclusion ou mort). cooldown : délai avant de pouvoir rejoindre une faction.
        public static void Remove(Faction f, string key, string why, bool cooldown)
        {
            if (f == null || key == null || !f.Members.ContainsKey(key)) return;
            string name = f.Members[key];
            f.Members.Remove(key);
            if (cooldown) leftAt[key] = DateTime.UtcNow;
            if (f.Members.Count == 0)
            {
                All.Remove(f.Id);
                Journal.Write("Faction", "la " + f.Display + " est dissoute (" + name + " " + why + ")");
            }
            else
            {
                if (f.Leader == key)
                {
                    foreach (string m in f.Members.Keys) { f.Leader = m; break; }
                    TellFaction(f, "[ffd040]" + f.Members[f.Leader] + " devient chef de la faction.[-]", -1);
                }
                TellFaction(f, "[ffd040]" + name + " " + why + ".[-]", -1);
                Journal.Write("Faction", name + " " + why + " (" + f.Display + ")");
            }
            Save();
            // Le joueur, s'il est en ligne, quitte aussi son groupe du jeu
            EntityPlayer p = Online(key);
            if (p != null && p.Party != null)
            {
                try { Party.ServerHandleLeaveParty(p, p.entityId); }
                catch (Exception e) { Log.Warning("[TWD_Faction] sortie du groupe : " + e.Message); }
            }
        }

        public static EntityPlayer Online(string key)
        {
            World w = GameManager.Instance.World;
            if (w == null || key == null) return null;
            foreach (EntityPlayer p in w.Players.list) if (p != null && Key(p) == key) return p;
            return null;
        }

        // ------------------------------------------------------------ groupes du jeu
        // Met le joueur dans un groupe de sa faction : celui qui a de la place et le plus de membres connectés.
        public static void PlaceInFaction(EntityPlayer p)
        {
            Faction f = Of(p);
            if (f == null || p == null) return;
            if (p.Party != null && PartyIsFaction(p.Party, f)) return;   // déjà dans un groupe de la faction
            Party best = null;
            foreach (string k in f.Members.Keys)
            {
                EntityPlayer m = Online(k);
                if (m == null || m == p || m.Party == null || m.Party.IsFull()) continue;
                if (best == null || m.Party.MemberList.Count > best.MemberList.Count) best = m.Party;
            }
            try
            {
                if (p.Party != null) Party.ServerHandleLeaveParty(p, p.entityId);
                if (best == null)
                {
                    // Personne de la faction avec un groupe : on en ouvre un, avec les membres connectés sans groupe
                    best = PartyManager.Current.CreateParty();
                    best.AddPlayer(p);
                    SendParty(best, p.entityId);
                    foreach (string k in f.Members.Keys)
                    {
                        EntityPlayer m = Online(k);
                        if (m == null || m == p || m.Party != null || best.IsFull()) continue;
                        best.AddPlayer(m);
                        SendParty(best, m.entityId);
                    }
                }
                else
                {
                    best.AddPlayer(p);
                    SendParty(best, p.entityId);
                }
                Tell(p.entityId, "[40ff40]Vous retrouvez votre faction : " + f.Display + ".[-]");
            }
            catch (Exception e) { Log.Warning("[TWD_Faction] placement dans un groupe : " + e.Message); }
        }

        static bool PartyIsFaction(Party party, Faction f)
        {
            foreach (EntityPlayer m in party.MemberList) if (m != null && Of(m) == f) return true;
            return false;
        }

        // Même annonce que le groupe automatique du jeu (Party.ServerHandleAutoJoinParty)
        static void SendParty(Party party, int changedId)
        {
            ConnectionManager.Instance.SendPackage(NetPackageManager.GetPackage<NetPackagePartyData>()
                .Setup(party, changedId, NetPackagePartyData.PartyActions.AutoJoin, false), false, -1, -1, -1, null, 192, false);
        }

        // ------------------------------------------------------------ messages
        public static void Tell(int entityId, string msg)
        {
            try
            {
                GameManager.Instance.ChatMessageServer(null, EChatType.Whisper, -1, msg, new List<int> { entityId },
                    EMessageSender.Server, GeneratedTextManager.BbCodeSupportMode.Supported);
            }
            catch (Exception e) { Log.Warning("[TWD_Faction] message : " + e.Message); }
        }

        public static void TellFaction(Faction f, string msg, int exceptId)
        {
            foreach (string k in f.Members.Keys)
            {
                EntityPlayer m = Online(k);
                if (m != null && m.entityId != exceptId) Tell(m.entityId, msg);
            }
        }

        // ------------------------------------------------------------ fichier
        static string FilePath() { return Path.Combine(GameIO.GetSaveGameDir(), "twd_factions.xml"); }

        public static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                string path = FilePath();
                if (!File.Exists(path)) return;
                var doc = new XmlDocument(); doc.Load(path);
                foreach (XmlElement e in doc.DocumentElement.SelectNodes("faction"))
                {
                    var f = new Faction();
                    f.Id = int.Parse(e.GetAttribute("id"));
                    f.Name = e.GetAttribute("name");
                    f.Leader = e.GetAttribute("leader");
                    foreach (XmlElement m in e.SelectNodes("member")) f.Members[m.GetAttribute("id")] = m.GetAttribute("name");
                    All[f.Id] = f;
                    if (f.Id >= nextId) nextId = f.Id + 1;
                }
                foreach (XmlElement e in doc.DocumentElement.SelectNodes("left"))
                    leftAt[e.GetAttribute("id")] = DateTime.Parse(e.GetAttribute("utc"), null, System.Globalization.DateTimeStyles.RoundtripKind);
                Log.Out("[TWD_Faction] " + All.Count + " faction(s) chargée(s)");
            }
            catch (Exception ex) { Log.Warning("[TWD_Faction] lecture de twd_factions.xml : " + ex.Message); }
        }

        public static void Save()
        {
            try
            {
                var doc = new XmlDocument();
                XmlElement root = doc.CreateElement("factions"); doc.AppendChild(root);
                foreach (Faction f in All.Values)
                {
                    XmlElement e = doc.CreateElement("faction");
                    e.SetAttribute("id", f.Id.ToString()); e.SetAttribute("name", f.Name); e.SetAttribute("leader", f.Leader);
                    foreach (var kv in f.Members)
                    {
                        XmlElement m = doc.CreateElement("member");
                        m.SetAttribute("id", kv.Key); m.SetAttribute("name", kv.Value);
                        e.AppendChild(m);
                    }
                    root.AppendChild(e);
                }
                foreach (var kv in leftAt)
                {
                    if ((DateTime.UtcNow - kv.Value).TotalHours > CooldownHours) continue;
                    XmlElement e = doc.CreateElement("left");
                    e.SetAttribute("id", kv.Key); e.SetAttribute("utc", kv.Value.ToString("o"));
                    root.AppendChild(e);
                }
                doc.Save(FilePath());
            }
            catch (Exception ex) { Log.Warning("[TWD_Faction] écriture de twd_factions.xml : " + ex.Message); }
        }

        // ------------------------------------------------------------ événements
        static readonly List<KeyValuePair<float, int>> pending = new List<KeyValuePair<float, int>>();

        public static void OnPlayerSpawned(ref ModEvents.SPlayerSpawnedInWorldData d)
        {
            if (!PvpRules.IsServer()) return;
            if (d.RespawnType != RespawnType.EnterMultiplayer && d.RespawnType != RespawnType.JoinMultiplayer) return;
            pending.Add(new KeyValuePair<float, int>(Time.time + 4f, d.EntityId));   // laisser le jeu du joueur finir de charger
        }

        public static void OnGameUpdate(ref ModEvents.SGameUpdateData _data)
        {
            if (pending.Count == 0) return;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (Time.time < pending[i].Key) continue;
                int id = pending[i].Value;
                pending.RemoveAt(i);
                World w = GameManager.Instance.World;
                EntityPlayer p = w == null ? null : w.GetEntity(id) as EntityPlayer;
                if (p == null) continue;
                Faction f = Of(p);
                if (f == null) continue;
                f.Members[Key(p)] = p.EntityName;   // nom à jour
                PlaceInFaction(p);
            }
        }

        // Mort définitive : le survivant quitte sa faction (sans délai : il pourra être réinvité)
        public static void OnPlayerDied(EntityPlayer p)
        {
            Faction f = Of(p);
            if (f != null) Remove(f, Key(p), "est mort et ne fait plus partie de la faction", false);
        }
    }

    // ------------------------------------------------------------ invitation acceptée
    [HarmonyPatch(typeof(Party), "ServerHandleAcceptInvite")]
    public static class Patch_FactionAcceptInvite
    {
        public static bool Prefix(EntityPlayer invitedBy, EntityPlayer invitedEntity)
        {
            try
            {
                if (!PvpRules.IsServer() || invitedBy == null || invitedEntity == null) return true;
                Faction fb = Factions.Of(invitedBy), fi = Factions.Of(invitedEntity);
                if (fi != null && fi == fb) return true;   // simple changement de groupe au sein de la faction
                if (fi != null)
                {
                    Factions.Tell(invitedEntity.entityId, "[ffd040]Vous êtes membre de la " + fi.Display + ". Pour rejoindre une autre faction : /faction quitter (puis 24 h d'attente).[-]");
                    Factions.Tell(invitedBy.entityId, "[ffd040]" + invitedEntity.EntityName + " appartient déjà à une autre faction.[-]");
                    return false;
                }
                double left = Factions.CooldownLeftHours(Factions.Key(invitedEntity));
                if (left > 0)
                {
                    string h = left >= 1 ? ((int)Math.Ceiling(left)) + " h" : ((int)Math.Ceiling(left * 60)) + " min";
                    Factions.Tell(invitedEntity.entityId, "[ffd040]Vous avez quitté une faction récemment : encore " + h + " avant de pouvoir en rejoindre une.[-]");
                    Factions.Tell(invitedBy.entityId, "[ffd040]" + invitedEntity.EntityName + " ne peut pas encore rejoindre de faction (" + h + ").[-]");
                    return false;
                }
                if (fb == null) { fb = Factions.Found(invitedBy); Factions.Save(); }
                Factions.Join(fb, invitedEntity);
                if (invitedBy.Party != null && invitedBy.Party.IsFull())
                {
                    Factions.PlaceInFaction(invitedEntity);   // groupe de l'inviteur plein : un autre groupe de la faction
                    return false;
                }
                return true;
            }
            catch (Exception e) { Log.Warning("[TWD_Faction] invitation : " + e.Message); return true; }
        }
    }

    // ------------------------------------------------------------ commandes du chat
    public static class FactionChat
    {
        public static ModEvents.EModEventResult OnChat(ref ModEvents.SChatMessageData d)
        {
            if (d.Message == null) return ModEvents.EModEventResult.Continue;
            string msg = d.Message.Trim();
            if (!msg.StartsWith("/faction", StringComparison.OrdinalIgnoreCase)) return ModEvents.EModEventResult.Continue;
            try
            {
                World w = GameManager.Instance.World;
                EntityPlayer p = w == null ? null : w.GetEntity(d.SenderEntityId) as EntityPlayer;
                if (p != null) Run(p, msg.Substring(8).Trim());
            }
            catch (Exception e) { Log.Warning("[TWD_Faction] commande : " + e.Message); }
            return ModEvents.EModEventResult.StopHandlersAndVanilla;   // la commande n'apparaît pas dans le chat public
        }

        static void Run(EntityPlayer p, string args)
        {
            string key = Factions.Key(p);
            Faction f = Factions.Of(key);
            string cmd = args, rest = "";
            int sp = args.IndexOf(' ');
            if (sp > 0) { cmd = args.Substring(0, sp); rest = args.Substring(sp + 1).Trim(); }
            cmd = cmd.ToLowerInvariant();
            bool chief = f != null && f.Leader == key;

            if (cmd == "" || cmd == "info")
            {
                if (f == null)
                {
                    double left = Factions.CooldownLeftHours(key);
                    Factions.Tell(p.entityId, "Vous n'avez pas de faction. Invitez un survivant dans votre groupe pour en fonder une."
                        + (left > 0 ? " (Délai avant d'en rejoindre une : " + Math.Ceiling(left) + " h.)" : ""));
                    return;
                }
                var names = new List<string>();
                foreach (var kv in f.Members) names.Add((kv.Key == f.Leader ? "[chef] " : "") + kv.Value + (Factions.Online(kv.Key) != null ? " (en ligne)" : ""));
                Factions.Tell(p.entityId, "[40ff40]" + f.Display + "[-] - " + f.Members.Count + " membre(s) : " + string.Join(", ", names.ToArray()));
                Factions.Tell(p.entityId, "Commandes : /faction | /faction quitter" + (chief ? " | /faction nom <nom> | /faction exclure <joueur> | /faction chef <joueur>" : ""));
                return;
            }
            if (f == null) { Factions.Tell(p.entityId, "Vous n'avez pas de faction."); return; }

            if (cmd == "quitter")
            {
                Factions.Remove(f, key, "a quitté la faction", true);
                Factions.Tell(p.entityId, "[ffd040]Vous avez quitté la " + f.Display + ". Vous pourrez rejoindre une faction dans " + Factions.CooldownHours + " h.[-]");
                return;
            }
            if (!chief) { Factions.Tell(p.entityId, "[ffd040]Seul le chef de la faction peut faire ça.[-]"); return; }

            if (cmd == "nom")
            {
                if (rest.Length < 2 || rest.Length > 24) { Factions.Tell(p.entityId, "Nom entre 2 et 24 caractères : /faction nom <nom>"); return; }
                string old = f.Display;
                f.Name = rest.Replace("[", "(").Replace("]", ")");
                Factions.Save();
                Factions.TellFaction(f, "[40ff40]La faction s'appelle désormais : " + f.Name + "[-]", -1);
                Journal.Write("Faction", old + " s'appelle désormais « " + f.Name + " »");
                return;
            }
            if (cmd == "exclure" || cmd == "chef")
            {
                string target = null;
                foreach (var kv in f.Members) if (string.Equals(kv.Value, rest, StringComparison.OrdinalIgnoreCase)) target = kv.Key;
                if (target == null) { Factions.Tell(p.entityId, "Membre introuvable : « " + rest + " ». Vérifiez avec /faction."); return; }
                if (target == key) { Factions.Tell(p.entityId, "C'est vous."); return; }
                if (cmd == "chef")
                {
                    f.Leader = target; Factions.Save();
                    Factions.TellFaction(f, "[40ff40]" + f.Members[target] + " devient chef de la faction.[-]", -1);
                    Journal.Write("Faction", f.Members[target] + " devient chef de la " + f.Display);
                    return;
                }
                string name = f.Members[target];
                Factions.Remove(f, target, "a été exclu de la faction", true);
                EntityPlayer t = Factions.Online(target);
                if (t != null) Factions.Tell(t.entityId, "[ff4040]Vous avez été exclu de la " + f.Display + ". Vous pourrez rejoindre une faction dans " + Factions.CooldownHours + " h.[-]");
                Factions.Tell(p.entityId, name + " a été exclu.");
                return;
            }
            Factions.Tell(p.entityId, "Commande inconnue. Tapez /faction pour l'aide.");
        }
    }
}
