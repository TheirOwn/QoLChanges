using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QolChanges
{
    [HarmonyPatch(typeof(BuildingManager))]
    public class BuildingManagerPatches
    {
        [HarmonyPatch("DisplayGhost")]
        [HarmonyPostfix]
        // Display the building ghost. Called per frame. Calculates the range of the tower at `pos` and draws a circle.
        public static void DisplayGhost(BuildingManager __instance, Vector3 pos, string text, GameObject ___currentGhost, GameObject ___thingToBuild)
        {
            if (___currentGhost.activeInHierarchy)
            {
                Tower tower = ___thingToBuild.GetComponent<Tower>();
                if (tower == null)
                    return;
                Plugin.ghostCircle.towerlessCircle(pos, tower);
            }

        }

        [HarmonyPatch("EnterBuildMode")]
        [HarmonyPostfix]
        /// <summary>
        /// Hide the ghostCircle UI if it is active.
        /// </summary>

        public static void EnterBuildMode(BuildingManager __instance, GameObject objectToBuild, TowerType type)
        {
            if (Plugin.ghostCircle == null)
            {
                Tower tower = objectToBuild.GetComponent<Tower>();
                GameObject prefabUI = (GameObject)tower.GetType().GetField(
                    "towerUI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tower);
                Plugin.ghostCircle = Object.Instantiate<GameObject>(prefabUI, objectToBuild.transform.position, Quaternion.identity).GetComponent<TowerUI>();
                Plugin.ghostCircle.gameObject.SetActive(false);
                foreach (Transform child in Plugin.ghostCircle.gameObject.transform)
                {
                    if (child.GetComponent<LineRenderer>() == null)
                        child.transform.localScale = new Vector3(0, 0, 0);
                }
            }
            Plugin.ghostCircle.gameObject.SetActive(false);
        }

        [HarmonyPatch("HideGhost")]
        [HarmonyPostfix]
        public static void HideGhost()
        {
            if (Plugin.ghostCircle != null)
            {
                Plugin.ghostCircle.gameObject.SetActive(false);
            }
        }
    }
}
