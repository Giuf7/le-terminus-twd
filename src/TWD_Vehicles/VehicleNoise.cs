// TWD_Vehicles - le moteur des véhicules attire les rôdeurs (7 Days to Die V 3.4)
// Mod CÔTÉ SERVEUR UNIQUEMENT. En vanilla, les sons de moteur n'émettent aucun "bruit" pour l'IA :
// un véhicule est silencieux pour les zombies. Toutes les 2 secondes, ce mod signale au directeur
// d'IA un bruit "twd_vehicle_engine" (défini dans TWD_Core/Config/sounds.xml) à la position de
// chaque véhicule à moteur qui a un conducteur. WalkerSim écoute aussi ces bruits : les hordes
// simulées convergent vers les véhicules.
// Compilé en C# 5 (tools/Build-Mod.ps1 -Name TWD_Vehicles).

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TWDVehicles
{
    public class TWDVehiclesMod : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            ModEvents.GameUpdate.RegisterHandler(EngineNoise.OnGameUpdate);
            ModEvents.GameUpdate.RegisterHandler(Fleet.OnGameUpdate);
            new HarmonyLib.Harmony("twd.vehicles").PatchAll(typeof(TWDVehiclesMod).Assembly);
            Debug.Log("[TWD_Vehicles] Initialisé : bruit des moteurs, véhicules abandonnés en zone PvP, limite de " + Fleet.FactionLimit + " véhicules par faction.");
        }
    }

    public static class EngineNoise
    {
        const float Interval = 2f;            // secondes entre deux bruits
        const string NoiseName = "twd_vehicle_engine";
        static float nextTick;
        static readonly Dictionary<int, Vector3> lastPos = new Dictionary<int, Vector3>();
        static bool errorLogged;

        public static void OnGameUpdate(ref ModEvents.SGameUpdateData _data)
        {
            if (Time.time < nextTick) return;
            nextTick = Time.time + Interval;
            try { Tick(); }
            catch (Exception ex)
            {
                if (!errorLogged) { Debug.LogError("[TWD_Vehicles] " + ex); errorLogged = true; }
            }
        }

        static void Tick()
        {
            ConnectionManager cm = ConnectionManager.Instance;
            if (cm == null || !cm.IsServer || GameManager.Instance == null) return;
            World world = GameManager.Instance.World;
            if (world == null || world.aiDirector == null) return;

            var seen = new HashSet<int>();
            List<Entity> entities = world.Entities.list;
            for (int i = 0; i < entities.Count; i++)
            {
                EntityVehicle v = entities[i] as EntityVehicle;
                if (v == null) continue;
                seen.Add(v.entityId);

                Vector3 pos = v.position;
                Vector3 prev;
                float speed = lastPos.TryGetValue(v.entityId, out prev) ? (pos - prev).magnitude / Interval : 0f;
                lastPos[v.entityId] = pos;

                Entity driver = v.AttachedMainEntity;
                if (driver == null) continue;                                   // personne au volant
                if (v.vehicle == null || !v.vehicle.HasEnginePart()) continue;   // vélo : pas de moteur
                if (!v.IsEngineRunning && speed < 0.5f) continue;                // moteur coupé, à l'arrêt

                // Plus on roule vite, plus on fait de bruit (0.6 au ralenti -> 1.5 à ~14 m/s, 50 km/h)
                float volume = Mathf.Clamp(0.6f + speed / 15f, 0.6f, 1.5f);
                world.aiDirector.NotifyNoise(driver, pos, NoiseName, volume);
            }

            // Nettoyage des véhicules disparus (détruits, ramassés)
            var gone = new List<int>();
            foreach (int id in lastPos.Keys) if (!seen.Contains(id)) gone.Add(id);
            foreach (int id in gone) lastPos.Remove(id);
        }
    }
}
