using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QolChanges
{
    /// <summary>
    /// Patches the Tower to Load Priorities from the plugin dictionary
    /// </summary>
    [HarmonyPatch(typeof(Tower))]
    public class TowerPatches
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Start_(Tower __instance)
        {
            for (int i = 0; i < __instance.priorities.Length; i++)
            {
                // sets the priorities on update.
                __instance.priorities[i] = Plugin.defaultPriorityDict[__instance.towerType][i];
            }
            UpdateRendering(__instance);
        }

        /// <summary>
        /// When the UI is opened, change the defaults
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(nameof(Tower.SpawnUI))]
        [HarmonyPostfix]
        static void SpawnUI_(Tower __instance)
        {
            for (int i = 0; i < __instance.priorities.Length; i++)
            {
                Plugin.defaultPriorityDict[__instance.towerType][i] = __instance.priorities[i];
            }
        }

        /// <summary>
        /// When Priorities are changed, change the default
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="index"></param>
        [HarmonyPatch(nameof(Tower.TogglePriority))]
        [HarmonyPostfix]
        public static void TogglePriority_(Tower __instance, int index)
        {
            Plugin.defaultPriorityDict[__instance.towerType][index] = __instance.priorities[index];
            UpdateRendering(__instance);
        }

        internal static void UpdateRendering(Tower tower)
        {
            var renderer = tower.GetComponentInChildren<MeshRenderer>(); // the first renderer should be the tower base
            if (renderer != null)
            {
                // priorities check ignores any repeated entries; each priority type (Progress, NearDeath, etc)
                // gets factored in only once, but with an exponential scale based on the index of its first appearance.
                // so if all three priorities are different, the selection weight for a given enemy will be
                //   p[0]^3 * p[1]^2 * p[2]^1
                // but if the first and second priorities are the same, the selection weight will be
                //   p[0]^3 * p[2]^1
                Color color = tower.priorities[0].GetColor();
                if (tower.priorities[1] != tower.priorities[0])
                    color = Color.Lerp(color, tower.priorities[1].GetColor(), 0.25f);
                if (tower.priorities[2] != tower.priorities[0] && tower.priorities[2] != tower.priorities[1])
                    color = Color.Lerp(color, tower.priorities[2].GetColor(), 0.1f);
                renderer.material.color = color.ScaleForTowerDisplay();
            }
        }
    }
}
