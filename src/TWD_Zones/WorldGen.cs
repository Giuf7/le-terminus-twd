// Génération de carte "Géorgie" côté serveur dédié.
// Le générateur (WorldBuilder) a les mêmes réglages que l'écran du jeu (poids des biomes, disposition,
// quantité de villes / lacs / rivières / cratères), mais un serveur dédié garde les valeurs par défaut.
// Ce patch les remplace par celles de Mods/TWD_Zones/worldgen.xml, juste avant leur utilisation.
// Sans worldgen.xml : aucun effet. N'agit que pendant la génération d'une NOUVELLE carte (GameWorld = RWG).

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using HarmonyLib;
using UnityEngine;
using WB = global::WorldGenerationEngineFinal.WorldBuilder;

namespace TWDZones
{
    public static class WorldGenSettings
    {
        public static bool Loaded, Active;
        public static int Forest = -1, BurntForest = -1, Desert = -1, Snow = -1, Wasteland = -1;
        public static string Layout, Towns, Wilderness, Rivers, Lakes, Craters;
        public static bool Traders = true;   // false : aucun avant-poste de marchand "hors ville"
        public static float TownSpacing = 0;      // distance minimale (m) entre deux villes ; 0 = règle du jeu
        public static float PrisonDistance = 0;   // distance minimale (m) entre une prison et une ville ; 0 = placement du jeu

        // Plan de la carte (coordonnées du jeu, centre = 0,0 ; X vers l'est, Z vers le nord)
        public class Rect2 { public string Name; public float X1, Z1, X2, Z2;
            public bool Contains(float x, float z) { return x >= X1 && x <= X2 && z >= Z1 && z <= Z2; } }
        public static readonly Dictionary<string, Rect2> TownRegions = new Dictionary<string, Rect2>(StringComparer.OrdinalIgnoreCase);
        public static Rect2 PrisonRegion;
        public static string PrisonOnly;   // seule prison autorisée (ex. "prison_01")

        static Rect2 ReadRect(XmlElement e)
        {
            var r = new Rect2(); r.Name = e.GetAttribute("township");
            r.X1 = float.Parse(e.GetAttribute("x1")); r.Z1 = float.Parse(e.GetAttribute("z1"));
            r.X2 = float.Parse(e.GetAttribute("x2")); r.Z2 = float.Parse(e.GetAttribute("z2"));
            if (r.X1 > r.X2) { float t = r.X1; r.X1 = r.X2; r.X2 = t; }
            if (r.Z1 > r.Z2) { float t = r.Z1; r.Z1 = r.Z2; r.Z2 = t; }
            return r;
        }

        public static void Load()
        {
            Loaded = true;
            string path = Path.Combine(Path.GetDirectoryName(ZoneStore.FilePath), "worldgen.xml");
            if (!File.Exists(path)) return;
            var doc = new XmlDocument(); doc.Load(path);
            XmlElement r = doc.DocumentElement;
            Forest = Int(r, "forest"); BurntForest = Int(r, "burntforest"); Desert = Int(r, "desert");
            Snow = Int(r, "snow"); Wasteland = Int(r, "wasteland");
            Layout = Str(r, "layout"); Towns = Str(r, "towns"); Wilderness = Str(r, "wilderness");
            Rivers = Str(r, "rivers"); Lakes = Str(r, "lakes"); Craters = Str(r, "craters");
            Traders = Str(r, "traders") != "false";
            TownSpacing = Math.Max(0, Int(r, "townspacing"));
            PrisonDistance = Math.Max(0, Int(r, "prisondistance"));
            foreach (XmlElement e in r.SelectNodes("plan/town")) { Rect2 t = ReadRect(e); TownRegions[t.Name] = t; }
            XmlElement pe = r.SelectSingleNode("plan/prison") as XmlElement;
            if (pe != null) { PrisonRegion = ReadRect(pe); PrisonOnly = pe.GetAttribute("only"); }
            Active = true;
        }

        static int Int(XmlElement r, string n) { XmlElement e = r[n]; return e == null ? -1 : int.Parse(e.InnerText.Trim()); }
        static string Str(XmlElement r, string n) { XmlElement e = r[n]; return e == null ? null : e.InnerText.Trim(); }

        static T ParseEnum<T>(string s, T fallback)
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            try { return (T)Enum.Parse(typeof(T), s, true); } catch { return fallback; }
        }

        public static void Apply(WB wb, string where)
        {
            if (!Loaded) Load();
            if (!Active) return;
            if (Forest >= 0) wb.ForestBiomeWeight = Forest;
            if (BurntForest >= 0) wb.BurntForestBiomeWeight = BurntForest;
            if (Desert >= 0) wb.DesertBiomeWeight = Desert;
            if (Snow >= 0) wb.SnowBiomeWeight = Snow;
            if (Wasteland >= 0) wb.WastelandBiomeWeight = Wasteland;
            wb.biomeLayout = ParseEnum(Layout, wb.biomeLayout);
            wb.Towns = ParseEnum(Towns, wb.Towns);
            wb.Wilderness = ParseEnum(Wilderness, wb.Wilderness);
            wb.Rivers = ParseEnum(Rivers, wb.Rivers);
            wb.Lakes = ParseEnum(Lakes, wb.Lakes);
            wb.Craters = ParseEnum(Craters, wb.Craters);
            Debug.Log("[TWD_WorldGen] (" + where + ") biomes forêt " + wb.ForestBiomeWeight + ", brûlée " + wb.BurntForestBiomeWeight
                + ", désert " + wb.DesertBiomeWeight + ", neige " + wb.SnowBiomeWeight + ", wasteland " + wb.WastelandBiomeWeight
                + " | disposition " + wb.biomeLayout + " | villes " + wb.Towns + ", nature " + wb.Wilderness
                + ", rivières " + wb.Rivers + ", lacs " + wb.Lakes + ", cratères " + wb.Craters);
        }
    }

    [HarmonyPatch(typeof(WB), "GenerateTerrain")]
    public static class Patch_WorldGenTerrain
    {
        public static void Prefix(WB __instance) { WorldGenSettings.Apply(__instance, "terrain"); }
    }

    // Avant-postes de marchands placés le long des chemins "hors ville" (en plus de ceux des villes,
    // déjà désactivés par TWD_Core/rwgmixer.xml) : ignorés si <traders>false</traders>.
    [HarmonyPatch(typeof(global::WorldGenerationEngineFinal.WildernessPathPlanner), "createTraderSpawnIfAble")]
    public static class Patch_WorldGenNoTraders
    {
        public static bool Prefix()
        {
            if (!WorldGenSettings.Loaded) WorldGenSettings.Load();
            return !(WorldGenSettings.Active && !WorldGenSettings.Traders);
        }
    }

    [HarmonyPatch(typeof(WB), "GenerateBiomeTiles")]
    public static class Patch_WorldGenBiomes
    {
        public static void Prefix(WB __instance) { WorldGenSettings.Apply(__instance, "biomes"); }
    }

    // ------------------------------------------------------------------ villes écartées
    // Le jeu (TownPlanner.IsTooClose) ne laisse qu'une marge de quelques cases entre deux villes.
    // On ajoute une distance minimale en mètres (<townspacing>), mesurée entre les rues des deux villes.
    // Les arrêts de bord de route (roadside) gardent la règle du jeu.
    [HarmonyPatch(typeof(global::WorldGenerationEngineFinal.TownPlanner), "IsTooClose")]
    public static class Patch_WorldGenTownSpacing
    {
        public static bool Prefix(global::WorldGenerationEngineFinal.TownPlanner __instance, Vector2i position, bool isRoadsideTownship, ref bool __result)
        {
            if (!WorldGenSettings.Loaded) WorldGenSettings.Load();
            if (!WorldGenSettings.Active) return true;
            WB wb = __instance.worldBuilder;
            float tile = WorldGenHelpers.TileMeters(wb);
            if (tile <= 0) return true;

            // Plan de la carte : chaque ville nommée dans sa région ; les autres (hameaux, bords de route) hors de ces régions
            if (WorldGenSettings.TownRegions.Count > 0 && Patch_WorldGenCurrentTownship.Current != null)
            {
                float half = wb.WorldSize / 2f;
                float gx = position.x * tile + tile / 2f - half, gz = position.y * tile + tile / 2f - half;
                WorldGenSettings.Rect2 own;
                if (WorldGenSettings.TownRegions.TryGetValue(Patch_WorldGenCurrentTownship.Current, out own))
                {
                    if (!own.Contains(gx, gz)) { __result = true; return false; }
                }
                else
                {
                    foreach (var reg in WorldGenSettings.TownRegions.Values)
                        if (gx > reg.X1 - 600 && gx < reg.X2 + 600 && gz > reg.Z1 - 600 && gz < reg.Z2 + 600) { __result = true; return false; }
                    // ... et loin de la région de la prison, qui doit rester isolée
                    var pr = WorldGenSettings.PrisonRegion;
                    float m = WorldGenSettings.PrisonDistance + 600;
                    if (pr != null && gx > pr.X1 - m && gx < pr.X2 + m && gz > pr.Z1 - m && gz < pr.Z2 + m) { __result = true; return false; }
                }
            }
            if (WorldGenSettings.TownSpacing <= 0 || isRoadsideTownship) return true;
            float minTiles = WorldGenSettings.TownSpacing / tile;
            float min2 = minTiles * minTiles;
            foreach (var t in wb.Townships)
            {
                if (t.IsRoadside()) continue;
                foreach (Vector2i k in t.Streets.Keys)
                {
                    float dx = k.x - position.x, dy = k.y - position.y;
                    if (dx * dx + dy * dy < min2) { __result = true; return false; }
                }
            }
            return true;   // assez loin de toutes les villes : la règle du jeu s'applique ensuite
        }
    }

    // ------------------------------------------------------------------ prisons isolées
    // Les prisons portent l'étiquette "rural" : le jeu les place dans le quartier rural en bordure des villes.
    // On leur retire "rural" et on leur donne l'étiquette des lieux isolés : elles ne sont plus placées qu'en pleine nature,
    // et jamais à moins de <prisondistance> mètres d'une ville.
    public static class WorldGenHelpers
    {
        static WB tagged;

        public static float TileMeters(WB wb)
        {
            foreach (var t in wb.Townships)
                foreach (var st in t.Streets.Values)
                    if (st.GridPosition.x != 0) return (float)st.WorldPosition.x / st.GridPosition.x;
            return 150f;   // taille d'une case de rue du générateur
        }

        public static bool IsPrison(string name) { return name != null && name.StartsWith("prison_", StringComparison.OrdinalIgnoreCase) && !name.Contains("sign"); }

        public static void RetagPrisons(WB wb)
        {
            if (wb == null || tagged == wb || !WorldGenSettings.Active || WorldGenSettings.PrisonDistance <= 0) return;
            tagged = wb;
            var data = wb.PrefabManager.prefabManagerData;
            var rural = FastTags<TagGroup.Poi>.Parse("rural");
            int n = 0;
            string only = WorldGenSettings.PrisonOnly;   // ex. "prison_01" : les autres prisons ne sont jamais placées
            foreach (PrefabData p in data.prefabDataList)
            {
                if (p == null || !IsPrison(p.Name)) continue;
                if (!string.IsNullOrEmpty(only) && !p.Name.StartsWith(only, StringComparison.OrdinalIgnoreCase))
                    p.Tags = p.Tags.Remove(rural).Remove(data.WildernessTags);   // ni en ville, ni en pleine nature
                else { p.Tags = p.Tags.Remove(rural) | data.WildernessTags; n++; }
            }
            Debug.Log("[TWD_WorldGen] " + n + " prison(s) déplacée(s) vers les lieux isolés (min " + WorldGenSettings.PrisonDistance + " m des villes)");
        }

        // Distance (m) entre un point du monde et la rue de ville la plus proche
        public static float DistanceToTowns(WB wb, Vector2i world)
        {
            float best = float.MaxValue;
            foreach (var t in wb.Townships)
            {
                if (t.IsRoadside()) continue;
                foreach (var st in t.Streets.Values)
                {
                    float dx = st.WorldPositionCenter.x - world.x, dy = st.WorldPositionCenter.y - world.y;
                    float d = dx * dx + dy * dy;
                    if (d < best) best = d;
                }
            }
            return best == float.MaxValue ? best : Mathf.Sqrt(best);
        }
    }

    [HarmonyPatch(typeof(global::WorldGenerationEngineFinal.TownPlanner), "Plan")]
    public static class Patch_WorldGenRetagPrisons
    {
        public static void Prefix(global::WorldGenerationEngineFinal.TownPlanner __instance)
        {
            if (!WorldGenSettings.Loaded) WorldGenSettings.Load();
            WorldGenHelpers.RetagPrisons(__instance.worldBuilder);
        }

        // Bilan : où chaque ville a été placée (vérification du plan)
        public static void Postfix(global::WorldGenerationEngineFinal.TownPlanner __instance)
        {
            if (!WorldGenSettings.Active) return;
            WB wb = __instance.worldBuilder; float half = wb.WorldSize / 2f;
            var seen = new HashSet<string>();
            foreach (var t in wb.Townships)
            {
                if (t.IsRoadside()) continue;
                string n = t.townshipData.Name;
                float sx = 0, sz = 0; int c = 0;
                foreach (var st in t.Streets.Values) { sx += st.WorldPositionCenter.x; sz += st.WorldPositionCenter.y; c++; }
                if (c == 0) continue;
                seen.Add(n);
                Debug.Log("[TWD_WorldGen] ville " + n + " : " + c + " cases, centre (" + (int)(sx / c - half) + ", " + (int)(sz / c - half) + ")");
            }
            foreach (string n in WorldGenSettings.TownRegions.Keys)
                if (!seen.Contains(n)) Debug.LogWarning("[TWD_WorldGen] ville " + n + " NON PLACÉE (région trop petite ou occupée ?)");
        }
    }

    // Type de ville en cours de placement (le générateur appelle cette méthode pour chaque ville, avant de tester des emplacements)
    [HarmonyPatch(typeof(global::WorldGenerationEngineFinal.TownPlanner), "getBiomeWithMostAvailableSpace")]
    public static class Patch_WorldGenCurrentTownship
    {
        public static string Current;
        public static void Prefix(global::WorldGenerationEngineFinal.TownshipData _townshipData)
        {
            Current = _townshipData == null ? null : _townshipData.Name;
        }
    }

    // Emplacement trop proche d'une ville : les prisons sont masquées le temps du choix, le jeu prend un autre bâtiment.
    // (Renvoyer "aucun bâtiment" fait planter la génération : StreetTile.spawnWildernessPrefab ne le gère pas, vu le 17/11/2026.)
    [HarmonyPatch(typeof(global::WorldGenerationEngineFinal.PrefabManager), "GetWildernessPrefab")]
    public static class Patch_WorldGenPrisonDistance
    {
        public static void Prefix(global::WorldGenerationEngineFinal.PrefabManager __instance, Vector2i center, out List<KeyValuePair<PrefabData, FastTags<TagGroup.Poi>>> __state)
        {
            __state = null;
            if (!WorldGenSettings.Active || (WorldGenSettings.PrisonDistance <= 0 && WorldGenSettings.PrisonRegion == null)) return;
            float half = __instance.worldBuilder.WorldSize / 2f;
            bool inRegion = WorldGenSettings.PrisonRegion == null || WorldGenSettings.PrisonRegion.Contains(center.x - half, center.y - half);
            bool farEnough = WorldGenSettings.PrisonDistance <= 0 || WorldGenHelpers.DistanceToTowns(__instance.worldBuilder, center) >= WorldGenSettings.PrisonDistance;
            if (inRegion && farEnough) return;   // emplacement valable : les prisons restent proposées
            var data = __instance.prefabManagerData;
            foreach (PrefabData p in data.prefabDataList)
            {
                if (p == null || !WorldGenHelpers.IsPrison(p.Name)) continue;
                if (__state == null) __state = new List<KeyValuePair<PrefabData, FastTags<TagGroup.Poi>>>();
                __state.Add(new KeyValuePair<PrefabData, FastTags<TagGroup.Poi>>(p, p.Tags));
                p.Tags = p.Tags.Remove(data.WildernessTags);
            }
        }

        public static void Postfix(global::WorldGenerationEngineFinal.PrefabManager __instance, Vector2i center, PrefabData __result, List<KeyValuePair<PrefabData, FastTags<TagGroup.Poi>>> __state)
        {
            if (__state != null) foreach (var kv in __state) kv.Key.Tags = kv.Value;   // étiquettes rétablies
            if (__result != null && WorldGenHelpers.IsPrison(__result.Name))
                Debug.Log("[TWD_WorldGen] " + __result.Name + " retenue en " + center + ", à " + (int)WorldGenHelpers.DistanceToTowns(__instance.worldBuilder, center) + " m de la ville la plus proche");
        }
    }
}
