// BUTIN DE LA PRISON (décision du 17/11/2026)
// L'armure de maton (mod TWD_ArmureMaton, 4 pièces) ne se trouve QUE dans la prison, en butin rare.
// Les coffres de la prison sont des coffres ordinaires (mêmes listes de butin que partout ailleurs) : on ne peut donc
// pas passer par loot.xml. À la PREMIÈRE ouverture d'un coffre situé dans la zone "Prison" (zones.xml), le serveur
// tire au sort et y ajoute éventuellement une pièce. Le butin ne réapparaît pas sur ce serveur : le total est limité.

using System;
using HarmonyLib;
using UnityEngine;

namespace TWDZones
{
    [HarmonyPatch(typeof(LootManager), "LootContainerOpened")]
    public static class Patch_PrisonLoot
    {
        public const string ZoneName = "Prison";
        public const float Chance = 0.05f;   // 5 % par coffre, à sa première ouverture
        static readonly string[] Pieces = { "armorMatonHelmet", "armorMatonChest", "armorMatonArms", "armorMatonBoots" };
        static readonly System.Random rng = new System.Random();

        // Avant : le coffre a-t-il déjà été ouvert ?
        public static void Prefix(TEFeatureStorage _tileEntity, out bool __state)
        {
            __state = false;
            try { __state = _tileEntity != null && _tileEntity.ItemGrid != null && !_tileEntity.ItemGrid.Touched; }
            catch { }
        }

        public static void Postfix(TEFeatureStorage _tileEntity, int _entityIdThatOpenedIt, bool __state)
        {
            try
            {
                if (!__state || !PvpRules.IsServer() || _tileEntity == null) return;
                Zone z = ZoneStore.ZoneAt(_tileEntity.ToWorldCenterPos());
                if (z == null || !string.Equals(z.Name, ZoneName, StringComparison.OrdinalIgnoreCase)) return;
                if (rng.NextDouble() >= Chance) return;
                string name = Pieces[rng.Next(Pieces.Length)];
                ItemValue iv = ItemClass.GetItem(name, false);
                if (iv == null || iv.type == 0 || iv.ItemClass == null) return;   // mod TWD_ArmureMaton absent
                int quality = 1 + rng.Next(3);   // qualité 1 à 3
                var stack = new ItemStack(new ItemValue(iv.type, quality, quality, false, null, 1f), 1);
                if (_tileEntity.AddItem(stack))
                {
                    _tileEntity.SetModified();
                    World w = GameManager.Instance.World;
                    Journal.Write("Butin", "pièce d'armure de maton (" + name + ", qualité " + quality + ") trouvée dans la prison par "
                        + Journal.Name(w == null ? null : w.GetEntity(_entityIdThatOpenedIt)) + " " + Journal.Pos(_tileEntity.ToWorldCenterPos()));
                }
            }
            catch (Exception e) { Log.Warning("[TWD_Prison] butin : " + e.Message); }
        }
    }
}
