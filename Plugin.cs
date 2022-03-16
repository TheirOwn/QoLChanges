using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using Object = UnityEngine.Object;
using System.Collections.Generic;

namespace QolChanges
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Rogue Tower.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        public static List<Tuple<TowerUI, VariablesTowerUI>> VariablesTowerUIDict = new List<Tuple<TowerUI, VariablesTowerUI>>();
        public static Dictionary<TowerType, Tower.Priority[]> defaultPriorityDict = new Dictionary<TowerType, Tower.Priority[]>();
        public static TowerUI ghostCircle;

        private void Awake()
        {
            Plugin.Log = base.Logger;
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            foreach (TowerType type in (TowerType[]) Enum.GetValues(typeof(TowerType))) 
            {
                defaultPriorityDict.Add(type, new Tower.Priority[] { Tower.Priority.Progress, Tower.Priority.Progress, Tower.Priority.Progress });
            }

            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll(typeof(UIManagerPatches));
            harmony.PatchAll(typeof(TowerPatches));
            harmony.PatchAll(typeof(BuildingManagerPatches))
        }

        private void onDestroy()
        {
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.UnpatchSelf();
        }
    }



    [HarmonyPatch(typeof(BuildingManager))]
    public class BuildingManagerPatches
    {
        [HarmonyPatch("DisplayGhost")]
        [HarmonyPostfix]
        public static void DisplayGhost(BuildingManager __instance, Vector3 pos, string text, GameObject ___currentGhost)
        {
            if (Plugin.ghostCircle != null)
            {
                //Plugin.ghostCircle.gameObject.transform.position = ___currentGhost.transform.position;
                //Plugin.ghostCircle.gameObject.SetActive(true);
                Plugin.ghostCircle.towerlessCircle(pos, 20f, false);
            }
        }

        [HarmonyPatch("EnterBuildMode")]
        [HarmonyPostfix]
        public static void EnterBuildMode(BuildingManager __instance, GameObject objectToBuild, TowerType type)
        {
            Tower tower = objectToBuild.GetComponent<Tower>();
            if (Plugin.ghostCircle == null)
            {
                Plugin.Log.LogInfo("ghostCircle made");
                GameObject prefabUI = (GameObject)tower.GetType().GetField(
                        "towerUI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tower);
                Plugin.ghostCircle = Object.Instantiate<GameObject>(prefabUI, tower.transform.position, Quaternion.identity).GetComponent<TowerUI>();
            }
            Plugin.Log.LogInfo("Set ghost circle tower");
            Plugin.ghostCircle.GetType().GetField("myTower", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(Plugin.ghostCircle, tower);
            Plugin.ghostCircle.gameObject.SetActive(false);
        }

        [HarmonyPatch("HideGhost")]
        [HarmonyPostfix]
        public static void HideGhost()
        {
            if (Plugin.ghostCircle != null)
                Plugin.ghostCircle.gameObject.SetActive(false);
        }


    }

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
                __instance.priorities[i] = Plugin.defaultPriorityDict[__instance.towerType][i];
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
        }
    }

    /// <summary>
    /// Updates the UI Manager to check for hovering over buildings in order to display range circle
    /// 
    /// </summary>
    [HarmonyPatch(typeof(UIManager))]
    public class UIManagerPatches
    {

        /// <summary>
        /// On Update, check if a range circle should be shown or removed
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="___currentUI"></param>
        /// <param name="___clickableMask"></param>
        /// <returns> true to run follow up and false to not run the update function</returns>
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static bool Update_MyPatch(UIManager __instance, GameObject ___currentUI, LayerMask ___clickableMask)
        {
            RaycastHit raycastHit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!BuildingManager.instance.buildMode &&
                Physics.Raycast(ray.origin, ray.direction, out raycastHit, 2000f, ___clickableMask, QueryTriggerInteraction.Collide))
            {
                if (!Input.GetMouseButtonDown(0) && ___currentUI == null && Plugin.VariablesTowerUIDict.Count == 0)
                {
                    Tower tower = raycastHit.collider.GetComponent<Tower>();
                    if (tower != null)
                    {
                        var towerUI2 = tower.GetType().GetField("towerUI", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        TowerUI towerUI;
                        if (towerUI2 != null)
                        {
                            // specified cast was not valid for turning into TowerUI. Need to investigate more
                            GameObject towerUI_GO = (GameObject)towerUI2.GetValue(tower);
                            towerUI = Object.Instantiate<GameObject>(towerUI_GO, tower.transform.position, Quaternion.identity).GetComponent<TowerUI>();
                            towerUI.GetType().GetField("myTower", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(towerUI, tower);
                            towerUI.showCircle();
                        }
                        else
                        {
                            //Plugin.Log.LogInfo("TowerUI not retrieved.");
                            return true;
                        }
                    }
                    return false;
                }
                else if (!Input.GetMouseButtonDown(0) && Plugin.VariablesTowerUIDict.Count > 0 && Plugin.VariablesTowerUIDict[0].Item2.showingCircle)
                {
                    TowerUI towerUI = Plugin.VariablesTowerUIDict[0].Item1;
                    if (___currentUI != null)
                    {
                        Tower myTower = (Tower)towerUI.GetType().GetField("myTower", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(towerUI);
                        Tower tower = raycastHit.collider.GetComponent<Tower>();
                        if (tower != myTower)
                        {
                            decrementTimer();
                        }
                        else
                        {
                            resetTimer();
                        }
                    }
                    return true;
                }
            }
            else if (___currentUI != null && Plugin.VariablesTowerUIDict.Count > 0 &&Plugin.VariablesTowerUIDict[0].Item2.showingCircle)
            {
                decrementTimer();
            }
            return true;
        }

        public static void decrementTimer()
        {
            Plugin.VariablesTowerUIDict[0].Item2.circleTimer -= Time.deltaTime;
            if (Plugin.VariablesTowerUIDict[0].Item2.circleTimer < 0)
            {
                Plugin.VariablesTowerUIDict[0].Item1.CloseUI();
            }
        }

        public static void resetTimer()
        {
            Plugin.VariablesTowerUIDict[0].Item2.circleTimer = 0.2f;
        }

        /// <summary>
        /// When a new ui is made, track it.
        /// </summary>
        /// <param name="newUI"></param>
        [HarmonyPatch(nameof(UIManager.SetNewUI))]
        [HarmonyPostfix]
        public static void SetNewUI_Patch(GameObject newUI)
        {
            if (Plugin.VariablesTowerUIDict.Count > 0)
            {
                if (Plugin.VariablesTowerUIDict[0].Item2.justMade)
                {
                    Plugin.VariablesTowerUIDict[0].Item2.justMade = false;
                    return;
                }
                Plugin.VariablesTowerUIDict.RemoveAt(0);
            }
            TowerUI result = newUI.GetComponent<TowerUI>();
            if (result != null)
            {
                Plugin.VariablesTowerUIDict.Add(new Tuple<TowerUI, VariablesTowerUI>(result, new VariablesTowerUI()));
            }
        }

        /// <summary>
        /// Tracks when ui is closed.
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(nameof(UIManager.CloseUI))]
        [HarmonyPostfix]
        public static void CloseUI_Patch(TowerUI __instance)
        {
            if (Plugin.VariablesTowerUIDict.Count > 0)
            {
                Plugin.VariablesTowerUIDict.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Extra information for tower uis to discern between normal ui and tower range circle.
    /// </summary>
    public class VariablesTowerUI
    {
        public bool showingCircle = false;
        public float circleTimer = 0.2f;
        public bool justMade = false;
    }

    /// <summary>
    /// Adds a new method to the TowerUI to show just the circle.
    /// </summary>
    public static class ExtendsTowerUI
    {
        public static void showCircle(this TowerUI __instance)
        {
            // hides all except the line renderer
            foreach (Transform child in __instance.gameObject.transform)
            {
                if (child.GetComponent<LineRenderer>() == null) 
                    child.gameObject.SetActive(false);
            }
            if (Plugin.VariablesTowerUIDict.Count > 0)
            {
                Plugin.VariablesTowerUIDict.RemoveAt(0);
            }
            Plugin.VariablesTowerUIDict.Add(new Tuple<TowerUI, VariablesTowerUI>(__instance, new VariablesTowerUI()));
            VariablesTowerUI vUI = Plugin.VariablesTowerUIDict[0].Item2;
            
            vUI.showingCircle = true;
            vUI.circleTimer = 0.5f;
            vUI.justMade = true;
            var drawCircle = __instance.GetType().GetMethod("DrawCircle", BindingFlags.NonPublic | BindingFlags.Instance);
            drawCircle.Invoke(__instance, new object[0]);
        }

        public static void towerlessCircle(this TowerUI __instance, Vector3 pos, float range, bool squareUI)
        {
            LineRenderer linePrefab = (LineRenderer) __instance.GetType().GetField("line", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            LineRenderer line = Object.Instantiate<LineRenderer>(linePrefab, pos, Quaternion.identity).GetComponent<LineRenderer>();
            if (squareUI)
            {
                range += 0.5f;
                line.SetVertexCount(5);
                line.useWorldSpace = true;
                Vector3 position = pos;
                position.y = 0.4f;
                line.SetPosition(0, new Vector3(range, 0f, range) + position);
                line.SetPosition(1, new Vector3(range, 0f, -range) + position);
                line.SetPosition(2, new Vector3(-range, 0f, -range) + position);
                line.SetPosition(3, new Vector3(-range, 0f, range) + position);
                line.SetPosition(4, new Vector3(range, 0f, range) + position);
                return;
            } 
            line.SetVertexCount(61);
            line.useWorldSpace = true;
            Vector3 a = new Vector3(0f, 0f, 0f);
            Vector3 position2 = pos;
            position2.y = 0.4f;
            float num2 = 0f;
            for (int i = 0; i < 61; i++)
            {
                a.x = Mathf.Cos(0.017453292f * num2) * range;
                a.z = Mathf.Sin(0.017453292f * num2) * range;
                line.SetPosition(i, a + position2);
                num2 += 6f;
            }
            line.gameObject.SetActive(true);
        }
    }
}
