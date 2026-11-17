using System.Collections.Generic;
using TWDZones;

namespace TWDVehicles
{
    // Commande console admin :
    //   twdvehicles list                  véhicules suivis + délais de réapparition par zone
    //   twdvehicles spawn <zone>          force l'apparition d'un véhicule abandonné dans une zone PvP
    //   twdvehicles cooldown <zone|all>   annule le délai de réapparition
    public class VehiclesCommand : ConsoleCmdAbstract
    {
        public override string[] getCommands() { return new string[] { "twdvehicles", "tv" }; }
        public override string getDescription() { return "TWD : véhicules abandonnés et limite par faction"; }
        public override string getHelp() { return "twdvehicles list | spawn <zone> | cooldown <zone|all>"; }

        static void Out(string s) { SdtdConsole.Instance.Output(s); }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (!Fleet.IsServer()) { Out("Serveur non prêt."); return; }
            string sub = _params.Count > 0 ? _params[0].ToLowerInvariant() : "list";
            HashSet<int> existing = Fleet.ExistingIds();
            if (sub == "list")
            {
                Out("Jour actuel : " + Fleet.Day() + " | limite par faction : " + Fleet.FactionLimit);
                if (Fleet.Records.Count == 0) Out("(aucun véhicule suivi)");
                foreach (VehicleRecord r in Fleet.Records.Values)
                    Out(" - #" + r.Id + (existing.Contains(r.Id) ? "" : " (absent)") + (r.Abandoned ? " abandonné, zone " + r.Zone : "")
                        + (r.Broken ? ", libre (pas encore pris)" : "") + (r.Motor ? "" : ", vélo") + ", propriétaire : " + (r.Owner == "" ? "personne" : r.Owner));
                foreach (KeyValuePair<string, int> kv in Fleet.NextSpawnDay)
                    Out(" - zone " + kv.Key + " : prochain véhicule au jour " + kv.Value);
            }
            else if (sub == "spawn" && _params.Count >= 2)
            {
                foreach (Zone z in ZoneStore.Zones)
                    if (z.Name == _params[1])
                    {
                        Out(Fleet.Spawn(z) ? "Véhicule abandonné apparu dans " + z.Name + "." : "Échec : aucun point dégagé trouvé (zone trop bâtie, ou personne à proximité pour charger le terrain).");
                        Fleet.Save();
                        return;
                    }
                Out("Zone inconnue : " + _params[1]);
            }
            else if (sub == "cooldown" && _params.Count >= 2)
            {
                if (_params[1] == "all") Fleet.NextSpawnDay.Clear(); else Fleet.NextSpawnDay.Remove(_params[1]);
                Fleet.Save();
                Out("Délai de réapparition annulé.");
            }
            else Out(getHelp());
        }
    }
}
