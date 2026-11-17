// TWD_Zones - zones PvP pour le serveur TWD (7 Days to Die V 3.4)
// Mod CÔTÉ SERVEUR UNIQUEMENT. Le serveur tourne en PlayerKillingMode = 3 (tout le monde
// peut blesser tout le monde) et ce mod BLOQUE les dégâts joueur -> joueur, sauf si
// l'attaquant ET la victime sont tous les deux dans une zone PvP.
// Compilé en C# 5 (csc du .NET Framework) : pas de $"", pas de ?., pas de nameof.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using UnityEngine;

namespace TWDZones
{
    public class Zone
    {
        public string Name;
        public bool Pvp;
        public bool IsCircle;
        public float X, Z, Radius;               // cercle
        public float MinX, MinZ, MaxX, MaxZ;     // rectangle

        public bool Contains(Vector3 p)
        {
            if (IsCircle)
            {
                float dx = p.x - X, dz = p.z - Z;
                return dx * dx + dz * dz <= Radius * Radius;
            }
            return p.x >= MinX && p.x <= MaxX && p.z >= MinZ && p.z <= MaxZ;
        }

        public string Describe()
        {
            string kind = Pvp ? "PvP" : "SURE";
            if (IsCircle)
                return string.Format(CultureInfo.InvariantCulture, "{0} [{1}] cercle centre ({2:0}, {3:0}) rayon {4:0}", Name, kind, X, Z, Radius);
            return string.Format(CultureInfo.InvariantCulture, "{0} [{1}] rectangle ({2:0}, {3:0}) -> ({4:0}, {5:0})", Name, kind, MinX, MinZ, MaxX, MaxZ);
        }
    }

    public static class ZoneStore
    {
        public static readonly List<Zone> Zones = new List<Zone>();
        public static bool DefaultPvp = false;
        public static string FilePath;
        static readonly object Lock = new object();

        static float F(XmlElement e, string attr)
        {
            return float.Parse(e.GetAttribute(attr), CultureInfo.InvariantCulture);
        }

        public static string Load()
        {
            lock (Lock)
            {
                Zones.Clear();
                if (!File.Exists(FilePath)) return "Fichier introuvable : " + FilePath;
                var doc = new XmlDocument();
                doc.Load(FilePath);
                XmlElement root = doc.DocumentElement;
                DefaultPvp = root.GetAttribute("defaultPvp") == "true";
                foreach (XmlNode n in root.ChildNodes)
                {
                    XmlElement e = n as XmlElement;
                    if (e == null || e.Name != "zone") continue;
                    var z = new Zone();
                    z.Name = e.GetAttribute("name");
                    z.Pvp = e.GetAttribute("pvp") != "false";
                    if (e.HasAttribute("radius"))
                    {
                        z.IsCircle = true; z.X = F(e, "x"); z.Z = F(e, "z"); z.Radius = F(e, "radius");
                    }
                    else
                    {
                        float x1 = F(e, "x1"), z1 = F(e, "z1"), x2 = F(e, "x2"), z2 = F(e, "z2");
                        z.MinX = Math.Min(x1, x2); z.MaxX = Math.Max(x1, x2);
                        z.MinZ = Math.Min(z1, z2); z.MaxZ = Math.Max(z1, z2);
                    }
                    Zones.Add(z);
                }
                return Zones.Count + " zone(s) chargée(s), PvP par défaut : " + (DefaultPvp ? "oui" : "non");
            }
        }

        public static void Save()
        {
            lock (Lock)
            {
                var doc = new XmlDocument();
                doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
                doc.AppendChild(doc.CreateComment(" Zones du serveur TWD. pvp=\"true\" : zone PvP. pvp=\"false\" : zone sure (prioritaire). Coordonnees : x = Est/Ouest, z = Nord/Sud. "));
                XmlElement root = doc.CreateElement("zones");
                root.SetAttribute("defaultPvp", DefaultPvp ? "true" : "false");
                doc.AppendChild(root);
                foreach (Zone z in Zones)
                {
                    XmlElement e = doc.CreateElement("zone");
                    e.SetAttribute("name", z.Name);
                    e.SetAttribute("pvp", z.Pvp ? "true" : "false");
                    if (z.IsCircle)
                    {
                        e.SetAttribute("x", z.X.ToString("0", CultureInfo.InvariantCulture));
                        e.SetAttribute("z", z.Z.ToString("0", CultureInfo.InvariantCulture));
                        e.SetAttribute("radius", z.Radius.ToString("0", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        e.SetAttribute("x1", z.MinX.ToString("0", CultureInfo.InvariantCulture));
                        e.SetAttribute("z1", z.MinZ.ToString("0", CultureInfo.InvariantCulture));
                        e.SetAttribute("x2", z.MaxX.ToString("0", CultureInfo.InvariantCulture));
                        e.SetAttribute("z2", z.MaxZ.ToString("0", CultureInfo.InvariantCulture));
                    }
                    root.AppendChild(e);
                }
                var settings = new XmlWriterSettings();
                settings.Indent = true; settings.IndentChars = "\t";
                using (var w = XmlWriter.Create(FilePath, settings)) doc.Save(w);
            }
        }

        // Zone "effective" à une position : une zone sûre l'emporte sur une zone PvP.
        // null = hors de toute zone (règle par défaut).
        public static Zone ZoneAt(Vector3 p)
        {
            Zone pvpZone = null;
            for (int i = 0; i < Zones.Count; i++)
            {
                Zone z = Zones[i];
                if (!z.Contains(p)) continue;
                if (!z.Pvp) return z;
                if (pvpZone == null) pvpZone = z;
            }
            return pvpZone;
        }

        public static bool IsPvp(Vector3 p)
        {
            Zone z = ZoneAt(p);
            return z == null ? DefaultPvp : z.Pvp;
        }
    }
}
