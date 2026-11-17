// TWD_Vehicles - véhicules abandonnés dans les zones PvP + limite de véhicules par faction.
// CÔTÉ SERVEUR UNIQUEMENT. Nécessite TWD_Zones (zones PvP) et TWD_Metiers (perkTWDMetierMecanicien).
//
// - Chaque zone PvP reçoit 1 véhicule abandonné (minibike / moto / 4x4), abîmé et presque à sec.
// - Tant qu'un Mécanicien ne l'a pas remis en route, personne ne peut l'utiliser (conduire, coffre, ramasser).
//   Le Mécanicien qui le remet en route en devient propriétaire.
// - Quand il quitte sa zone (pris) ou disparaît (détruit, ramassé) : nouveau véhicule 3 jours de jeu plus tard.
// - Une faction (le joueur + ses alliés + son groupe) ne peut pas posséder plus de 3 véhicules à moteur,
//   abandonnés remis en route ET fabriqués compris (les vélos ne comptent pas).
// Compilé en C# 5 : pas de ?., pas de $"", pas de nameof.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using HarmonyLib;
using TWDZones;
using UnityEngine;

namespace TWDVehicles
{
    public class VehicleRecord
    {
        public int Id;
        public string Owner = "";     // CombinedString du propriétaire ("" = personne)
        public bool Abandoned;        // véhicule abandonné encore rattaché à sa zone
        public bool Broken;           // abandonné et pas encore pris : le premier qui monte dessus le prend pour sa faction
        public string Zone = "";
        public bool Motor = true;     // false = vélo (ne compte pas dans la limite)
    }

    public static class Fleet
    {
        public const int FactionLimit = 3;
        public const int RespawnDays = 3;
        public const string MechanicPerk = "perkTWDMetierMecanicien";

        public static readonly Dictionary<int, VehicleRecord> Records = new Dictionary<int, VehicleRecord>();
        public static readonly Dictionary<string, int> NextSpawnDay = new Dictionary<string, int>();
        static readonly System.Random rng = new System.Random();
        static float nextTick;
        static bool loaded;
        static bool errorLogged;

        // ------------------------------------------------------------ persistance (dans la sauvegarde de la partie)
        static string FilePath() { return Path.Combine(GameIO.GetSaveGameDir(), "twd_vehicles.xml"); }

        public static void Load()
        {
            Records.Clear(); NextSpawnDay.Clear(); loaded = true;
            string path = FilePath();
            if (!File.Exists(path)) return;
            var doc = new XmlDocument(); doc.Load(path);
            foreach (XmlElement e in doc.DocumentElement.SelectNodes("vehicle"))
            {
                var r = new VehicleRecord();
                r.Id = int.Parse(e.GetAttribute("id"));
                r.Owner = e.GetAttribute("owner");
                r.Abandoned = e.GetAttribute("abandoned") == "true";
                r.Broken = e.GetAttribute("broken") == "true";
                r.Zone = e.GetAttribute("zone");
                r.Motor = e.GetAttribute("motor") != "false";
                Records[r.Id] = r;
            }
            foreach (XmlElement e in doc.DocumentElement.SelectNodes("cooldown"))
                NextSpawnDay[e.GetAttribute("zone")] = int.Parse(e.GetAttribute("day"));
            Debug.Log("[TWD_Vehicles] " + Records.Count + " véhicule(s) suivi(s) chargé(s).");
        }

        public static void Save()
        {
            var doc = new XmlDocument();
            XmlElement root = doc.CreateElement("twdvehicles"); doc.AppendChild(root);
            foreach (VehicleRecord r in Records.Values)
            {
                XmlElement e = doc.CreateElement("vehicle");
                e.SetAttribute("id", r.Id.ToString(CultureInfo.InvariantCulture));
                e.SetAttribute("owner", r.Owner);
                e.SetAttribute("abandoned", r.Abandoned ? "true" : "false");
                e.SetAttribute("broken", r.Broken ? "true" : "false");
                e.SetAttribute("zone", r.Zone);
                e.SetAttribute("motor", r.Motor ? "true" : "false");
                root.AppendChild(e);
            }
            foreach (KeyValuePair<string, int> kv in NextSpawnDay)
            {
                XmlElement e = doc.CreateElement("cooldown");
                e.SetAttribute("zone", kv.Key);
                e.SetAttribute("day", kv.Value.ToString(CultureInfo.InvariantCulture));
                root.AppendChild(e);
            }
            doc.Save(FilePath());
        }

        // ------------------------------------------------------------ outils
        public static bool IsServer()
        {
            ConnectionManager cm = ConnectionManager.Instance;
            return cm != null && cm.IsServer && GameManager.Instance != null && GameManager.Instance.World != null;
        }

        public static int Day() { return GameUtils.WorldTimeToDays(GameManager.Instance.World.worldTime); }

        public static bool IsMechanic(EntityPlayer p)
        {
            if (p == null || p.Progression == null) return false;
            ProgressionValue pv = p.Progression.GetProgressionValue(MechanicPerk);
            return pv != null && pv.Level >= 1;
        }

        public static PersistentPlayerData Data(int entityId)
        {
            return GameManager.Instance.persistentPlayers == null ? null
                : GameManager.Instance.persistentPlayers.GetPlayerDataFromEntityID(entityId);
        }

        public static void Notify(int entityId, string msg)
        {
            ConnectionManager cm = ConnectionManager.Instance;
            if (cm == null || cm.Clients == null) return;
            ClientInfo ci = cm.Clients.ForEntityId(entityId);
            if (ci != null) ci.SendPackage(NetPackageManager.GetPackage<NetPackageShowToolbeltMessage>().Setup(msg, "ui_tab"));   // un son null n'est pas transmis
        }

        // Ids de tous les véhicules existants (chargés ou non)
        public static HashSet<int> ExistingIds()
        {
            var ids = new HashSet<int>();
            VehicleManager vm = VehicleManager.Instance;
            if (vm == null) return ids;
            foreach (EntityVehicle v in vm.vehiclesActive) if (v != null) ids.Add(v.entityId);
            foreach (EntityCreationData d in vm.vehiclesUnloaded) if (d != null) ids.Add(d.id);
            return ids;
        }

        // Nombre de véhicules à moteur de la faction du joueur (tous ses membres, connectés ou non ; lui seul s'il n'en a pas).
        // Avant le 17/11/2026 : groupe du moment + alliés (contournable en quittant le groupe).
        public static int FactionCount(EntityPlayer p, int excludeId)
        {
            HashSet<string> members = TWDZones.Factions.MemberKeys(p);
            if (members.Count == 0) return 0;
            HashSet<int> existing = ExistingIds();
            int n = 0;
            foreach (VehicleRecord r in Records.Values)
            {
                if (r.Id == excludeId || !r.Motor || r.Owner == "" || !existing.Contains(r.Id)) continue;
                if (members.Contains(r.Owner)) n++;
            }
            return n;
        }

        // ------------------------------------------------------------ boucle (toutes les 30 s)
        public static void OnGameUpdate(ref ModEvents.SGameUpdateData _data)
        {
            if (Time.time < nextTick) return;
            nextTick = Time.time + 30f;
            try
            {
                if (!IsServer()) return;
                if (!loaded) Load();
                Tick();
            }
            catch (Exception ex)
            {
                if (!errorLogged) { Debug.LogError("[TWD_Vehicles] " + ex); errorLogged = true; }
            }
        }

        static void Tick()
        {
            World world = GameManager.Instance.World;
            int day = Day();
            bool dirty = false;
            HashSet<int> existing = ExistingIds();

            // 1. Véhicules disparus (détruits, ramassés)
            var gone = new List<int>();
            foreach (VehicleRecord r in Records.Values) if (!existing.Contains(r.Id)) gone.Add(r.Id);
            foreach (int id in gone)
            {
                VehicleRecord r = Records[id];
                if (r.Abandoned && r.Zone != "") NextSpawnDay[r.Zone] = day + RespawnDays;
                Records.Remove(id); dirty = true;
            }

            // 2. Véhicules abandonnés remis en route qui ont quitté leur zone : pris
            foreach (VehicleRecord r in Records.Values)
            {
                if (!r.Abandoned || r.Broken) continue;
                EntityVehicle v = world.GetEntity(r.Id) as EntityVehicle;
                Zone z = FindZone(r.Zone);
                if (v == null || (z != null && z.Contains(v.position))) continue;
                r.Abandoned = false;
                NextSpawnDay[r.Zone] = day + RespawnDays;
                dirty = true;
                Debug.Log("[TWD_Vehicles] Véhicule " + r.Id + " sorti de la zone " + r.Zone + " : nouveau véhicule dans " + RespawnDays + " jours.");
            }

            // 3. Un véhicule par zone PvP
            foreach (Zone z in ZoneStore.Zones)
            {
                if (!z.Pvp || HasAbandoned(z.Name)) continue;
                int next;
                if (NextSpawnDay.TryGetValue(z.Name, out next) && day < next) continue;
                if (Spawn(z)) dirty = true;
            }
            if (dirty) Save();
        }

        static Zone FindZone(string name)
        {
            foreach (Zone z in ZoneStore.Zones) if (z.Name == name) return z;
            return null;
        }

        static bool HasAbandoned(string zone)
        {
            foreach (VehicleRecord r in Records.Values) if (r.Abandoned && r.Zone == zone) return true;
            return false;
        }

        static bool NearPlayer(World world, Vector3 pos, float minDistance)
        {
            foreach (EntityPlayer p in world.Players.list)
                if (p != null && (p.position - pos).magnitude < minDistance) return true;
            return false;
        }

        // ------------------------------------------------------------ apparition
        public static bool Spawn(Zone z)
        {
            World world = GameManager.Instance.World;
            int roll = rng.Next(100);
            string entity = roll < 50 ? "vehicleMinibike" : roll < 85 ? "vehicleMotorcycle" : "vehicleTruck4x4";
            string item = roll < 50 ? "vehicleMinibikePlaceable" : roll < 85 ? "vehicleMotorcyclePlaceable" : "vehicleTruck4x4Placeable";

            // Point dégagé dans la zone (rien au-dessus du sol : ni bâtiment ni arbre). Chunk non chargé -> hauteur 0 -> on réessaie plus tard.
            Vector3 pos = Vector3.zero; bool found = false;
            for (int i = 0; i < 40 && !found; i++)
            {
                float x, zc;
                if (z.IsCircle)
                {
                    double a = rng.NextDouble() * 2 * Math.PI, d = Math.Sqrt(rng.NextDouble()) * z.Radius * 0.9;
                    x = z.X + (float)(Math.Cos(a) * d); zc = z.Z + (float)(Math.Sin(a) * d);
                }
                else
                {
                    x = z.MinX + (float)rng.NextDouble() * (z.MaxX - z.MinX);
                    zc = z.MinZ + (float)rng.NextDouble() * (z.MaxZ - z.MinZ);
                }
                int ix = Mathf.FloorToInt(x), iz = Mathf.FloorToInt(zc);
                int h = world.GetHeight(ix, iz), th = world.GetTerrainHeight(ix, iz);
                if (h <= 1 || h != th) continue;
                Vector3 candidate = new Vector3(ix + 0.5f, h + 1.5f, iz + 0.5f);
                if (NearPlayer(world, candidate, 80f)) continue;   // jamais sous les yeux d'un joueur
                pos = candidate; found = true;
            }
            if (!found) return false;

            ItemValue iv = new ItemValue(ItemClass.GetItem(item, false).type, 1, 3, false, null, 1f);
            // Création directe, comme NetPackageVehicleSpawn.ProcessPackage : appeler ce paquet sans joueur expéditeur
            // plante en V 3.4 (ValidEntityIdForSender -> NullReferenceException, vu le 17/11/2026 : aucun véhicule n'apparaissait).
            EntityVehicle v = EntityFactory.CreateEntity(EntityClass.FromString(entity), pos, new Vector3(0f, (float)rng.Next(360), 0f)) as EntityVehicle;
            if (v != null)
            {
                v.SetSpawnerSource(EnumSpawnerSource.StaticSpawner);
                v.GetVehicle().SetItemValue(iv.Clone());
                world.SpawnEntityInWorld(v);
            }
            if (v == null) { Debug.LogWarning("[TWD_Vehicles] Apparition échouée dans la zone " + z.Name); return false; }

            Vehicle veh = v.GetVehicle();
            v.Health = Math.Max(1, (int)(veh.GetMaxHealth() * 0.3f));
            veh.SetFuelLevel(veh.GetMaxFuelLevel() * 0.08f);
            var r = new VehicleRecord();
            r.Id = v.entityId; r.Abandoned = true; r.Broken = true; r.Zone = z.Name;
            Records[r.Id] = r;
            Debug.Log("[TWD_Vehicles] Véhicule abandonné (" + entity + ") apparu dans la zone " + z.Name + " en " + pos);
            return true;
        }
    }

    // RÈGLE (décision du 17/11/2026) : tout le monde peut conduire un véhicule ; seul le Mécanicien peut le réparer.
    // Véhicule abandonné : le premier qui monte dessus le prend pour sa faction (dans la limite de la faction).
    // La montée ne passe PAS par le verrou du véhicule (réservé au coffre) : le jeu du joueur s'installe puis demande
    // au serveur de l'attacher. Refus : le serveur n'attache pas et renvoie au joueur l'ordre de descendre.
    [HarmonyPatch(typeof(NetPackageEntityAttach), "ProcessPackage")]
    public static class Patch_VehicleMount
    {
        // true = montée autorisée
        static bool TakeAbandoned(EntityVehicle v, int playerId)
        {
            VehicleRecord r;
            if (v == null || !Fleet.Records.TryGetValue(v.entityId, out r) || !r.Broken) return true;   // Broken = pas encore pris
            EntityPlayer p = GameManager.Instance.World.GetEntity(playerId) as EntityPlayer;
            if (p == null) return true;
            if (Fleet.FactionCount(p, v.entityId) >= Fleet.FactionLimit)
            {
                Fleet.Notify(playerId, "[ffd040]Votre faction possède déjà " + Fleet.FactionLimit + " véhicules.[-]");
                Journal.Write("Véhicule", Journal.Name(p) + " : limite de faction atteinte, véhicule abandonné " + r.Id + " refusé");
                return false;
            }
            PersistentPlayerData d = Fleet.Data(playerId);
            r.Broken = false;
            if (d != null) { r.Owner = d.PrimaryId.CombinedString; v.SetOwner(d.PrimaryId); }
            Fleet.Save();
            Fleet.Notify(playerId, "[40ff40]Vous prenez ce véhicule abandonné : il appartient à votre faction. Il est abîmé, seul un Mécanicien peut le réparer.[-]");
            Journal.Write("Véhicule", Journal.Name(p) + " prend le véhicule abandonné " + r.Id + " " + Journal.Pos(v));
            return true;
        }

        public static bool Prefix(NetPackageEntityAttach __instance, World _world)
        {
            try
            {
                if (!Fleet.IsServer() || _world == null || __instance.attachType != NetPackageEntityAttach.AttachType.AttachServer) return true;
                EntityVehicle v = _world.GetEntity(__instance.vehicleId) as EntityVehicle;
                if (TakeAbandoned(v, __instance.riderId)) return true;
                ConnectionManager cm = ConnectionManager.Instance;
                ClientInfo ci = cm == null || cm.Clients == null ? null : cm.Clients.ForEntityId(__instance.riderId);
                if (ci != null)
                    ci.SendPackage(NetPackageManager.GetPackage<NetPackageEntityAttach>()
                        .Setup(NetPackageEntityAttach.AttachType.DetachClient, __instance.riderId, -1, -1));
                return false;
            }
            catch (Exception e) { Debug.LogWarning("[TWD_Vehicles] montée : " + e.Message); return true; }
        }
    }

    // Réparation réservée au Mécanicien. Elle se fait dans le jeu du joueur (kit de réparation consommé, vie augmentée)
    // puis arrive au serveur dans la synchronisation du véhicule. Si un non-Mécanicien a fait remonter la vie :
    // le serveur remet l'ancienne valeur, la renvoie à tous, et rend le kit au joueur.
    [HarmonyPatch(typeof(NetPackageVehicleDataSync), "ProcessPackage")]
    public static class Patch_VehicleRepair
    {
        public static void Prefix(NetPackageVehicleDataSync __instance, World _world, out int __state)
        {
            __state = -1;
            try
            {
                if (!Fleet.IsServer() || _world == null) return;
                EntityVehicle v = _world.GetEntity(__instance.vehicleId) as EntityVehicle;
                if (v != null && v.GetVehicle() != null) __state = v.GetVehicle().GetHealth();
            }
            catch { }
        }

        public static void Postfix(NetPackageVehicleDataSync __instance, World _world, int __state)
        {
            try
            {
                if (__state < 0 || _world == null) return;
                EntityVehicle v = _world.GetEntity(__instance.vehicleId) as EntityVehicle;
                if (v == null || v.GetVehicle() == null || v.GetVehicle().GetHealth() <= __state) return;
                EntityPlayer p = _world.GetEntity(__instance.senderId) as EntityPlayer;
                if (p == null || Fleet.IsMechanic(p)) return;
                v.Health = __state;
                v.SendSyncData(EntityVehicle.cSyncAllNonRates);
                GameManager.Instance.ItemDropServer(new ItemStack(ItemClass.GetItem("resourceRepairKit", false), 1), p.position, Vector3.zero, -1, 600f, false);
                Fleet.Notify(p.entityId, "[ffd040]Seul un Mécanicien sait réparer un véhicule. Votre kit de réparation est posé à vos pieds.[-]");
                Journal.Write("Véhicule", Journal.Name(p) + " n'est pas Mécanicien : réparation du véhicule " + v.entityId + " annulée " + Journal.Pos(v));
            }
            catch (Exception e) { Debug.LogWarning("[TWD_Vehicles] réparation : " + e.Message); }
        }
    }

    // Pose d'un véhicule fabriqué : limite de la faction
    [HarmonyPatch(typeof(NetPackageVehicleSpawn), "ProcessPackage")]
    public static class Patch_VehicleSpawn
    {
        public static bool Prefix(NetPackageVehicleSpawn __instance, World _world, ref HashSet<int> __state)
        {
            __state = null;
            if (!Fleet.IsServer() || __instance.entityThatPlaced < 0) return true;   // < 0 : apparition par ce mod
            EntityPlayer p = _world.GetEntity(__instance.entityThatPlaced) as EntityPlayer;
            bool bicycle = __instance.entityType == EntityClass.FromString("vehicleBicycle");
            if (p != null && !bicycle && Fleet.FactionCount(p, -1) >= Fleet.FactionLimit)
            {
                GameManager.Instance.ItemDropServer(new ItemStack(__instance.itemValue, 1), p.position, Vector3.zero, -1, 600f, false);
                Fleet.Notify(p.entityId, "[ffd040]Votre faction possède déjà " + Fleet.FactionLimit + " véhicules : le véhicule est posé à vos pieds, dans son emballage.[-]");
                return false;
            }
            __state = Fleet.ExistingIds();
            return true;
        }

        public static void Postfix(NetPackageVehicleSpawn __instance, HashSet<int> __state)
        {
            if (__state == null) return;
            PersistentPlayerData d = Fleet.Data(__instance.entityThatPlaced);
            if (d == null) return;
            bool bicycle = __instance.entityType == EntityClass.FromString("vehicleBicycle");
            foreach (EntityVehicle v in VehicleManager.Instance.vehiclesActive)
            {
                if (v == null || __state.Contains(v.entityId)) continue;
                var r = new VehicleRecord();
                r.Id = v.entityId; r.Owner = d.PrimaryId.CombinedString; r.Motor = !bicycle;
                Fleet.Records[r.Id] = r;
            }
            Fleet.Save();
        }
    }
}
