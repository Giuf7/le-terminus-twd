using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace TWDZones
{
    // Commande console (F1 en jeu pour un admin, ou telnet / panneau web) :
    //   twdzones list                          liste des zones
    //   twdzones here                          zone à votre position
    //   twdzones addcircle <nom> <rayon> [pvp|safe]   cercle centré sur vous
    //   twdzones corner1  /  twdzones corner2  puis  twdzones addrect <nom> [pvp|safe]
    //   twdzones remove <nom>
    //   twdzones default <pvp|safe>            règle hors zones (safe recommandé)
    //   twdzones reload                        relit zones.xml
    public class ZonesCommand : ConsoleCmdAbstract
    {
        static readonly Dictionary<string, Vector3> corner1 = new Dictionary<string, Vector3>();
        static readonly Dictionary<string, Vector3> corner2 = new Dictionary<string, Vector3>();

        public override string[] getCommands() { return new string[] { "twdzones", "tz" }; }
        public override string getDescription() { return "TWD : gestion des zones PvP"; }
        public override string getHelp()
        {
            return "twdzones list | here | addcircle <nom> <rayon> [pvp|safe] | corner1 | corner2 | addrect <nom> [pvp|safe] | remove <nom> | default <pvp|safe> | reload";
        }

        static void Out(string s) { SdtdConsole.Instance.Output(s); }

        static bool TryGetPos(CommandSenderInfo sender, out Vector3 pos, out string key)
        {
            pos = Vector3.zero; key = "console";
            if (sender.RemoteClientInfo == null) { Out("Cette sous-commande doit être lancée EN JEU (F1) : elle utilise votre position."); return false; }
            key = sender.RemoteClientInfo.playerName;
            EntityPlayer p = GameManager.Instance.World.GetEntity(sender.RemoteClientInfo.entityId) as EntityPlayer;
            if (p == null) { Out("Joueur introuvable."); return false; }
            pos = p.position;
            return true;
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (_params.Count == 0) { Out(getHelp()); return; }
            string sub = _params[0].ToLowerInvariant();
            Vector3 pos; string key;

            if (sub == "list")
            {
                Out("PvP hors zones : " + (ZoneStore.DefaultPvp ? "OUI" : "non"));
                if (ZoneStore.Zones.Count == 0) Out("(aucune zone)");
                foreach (Zone z in ZoneStore.Zones) Out(" - " + z.Describe());
            }
            else if (sub == "here")
            {
                if (!TryGetPos(_senderInfo, out pos, out key)) return;
                Zone z = ZoneStore.ZoneAt(pos);
                Out(string.Format(CultureInfo.InvariantCulture, "Position : x={0:0} z={1:0}", pos.x, pos.z));
                Out(z == null ? "Hors zone (règle par défaut : " + (ZoneStore.DefaultPvp ? "PvP" : "sûre") + ")" : "Dans : " + z.Describe());
            }
            else if (sub == "addcircle" && _params.Count >= 3)
            {
                if (!TryGetPos(_senderInfo, out pos, out key)) return;
                var z = new Zone();
                z.Name = _params[1]; z.IsCircle = true; z.X = pos.x; z.Z = pos.z;
                z.Radius = float.Parse(_params[2], CultureInfo.InvariantCulture);
                z.Pvp = !(_params.Count >= 4 && _params[3].ToLowerInvariant() == "safe");
                ZoneStore.Zones.RemoveAll(x => x.Name == z.Name);
                ZoneStore.Zones.Add(z); ZoneStore.Save();
                Out("Zone ajoutée : " + z.Describe());
            }
            else if (sub == "corner1" || sub == "corner2")
            {
                if (!TryGetPos(_senderInfo, out pos, out key)) return;
                (sub == "corner1" ? corner1 : corner2)[key] = pos;
                Out(string.Format(CultureInfo.InvariantCulture, "{0} enregistré : x={1:0} z={2:0}", sub, pos.x, pos.z));
            }
            else if (sub == "addrect" && _params.Count >= 2)
            {
                if (!TryGetPos(_senderInfo, out pos, out key)) return;
                Vector3 a, b;
                if (!corner1.TryGetValue(key, out a) || !corner2.TryGetValue(key, out b)) { Out("Utilisez d'abord 'twdzones corner1' puis 'twdzones corner2' aux deux coins opposés."); return; }
                var z = new Zone();
                z.Name = _params[1]; z.IsCircle = false;
                z.MinX = Math.Min(a.x, b.x); z.MaxX = Math.Max(a.x, b.x);
                z.MinZ = Math.Min(a.z, b.z); z.MaxZ = Math.Max(a.z, b.z);
                z.Pvp = !(_params.Count >= 3 && _params[2].ToLowerInvariant() == "safe");
                ZoneStore.Zones.RemoveAll(x => x.Name == z.Name);
                ZoneStore.Zones.Add(z); ZoneStore.Save();
                Out("Zone ajoutée : " + z.Describe());
            }
            else if (sub == "remove" && _params.Count >= 2)
            {
                int n = ZoneStore.Zones.RemoveAll(x => x.Name == _params[1]);
                ZoneStore.Save();
                Out(n > 0 ? "Zone supprimée : " + _params[1] : "Aucune zone nommée " + _params[1]);
            }
            else if (sub == "default" && _params.Count >= 2)
            {
                ZoneStore.DefaultPvp = _params[1].ToLowerInvariant() == "pvp";
                ZoneStore.Save();
                Out("Règle hors zones : " + (ZoneStore.DefaultPvp ? "PvP" : "sûre"));
            }
            else if (sub == "reload")
            {
                Out(ZoneStore.Load());
            }
            else Out(getHelp());
        }
    }
}
