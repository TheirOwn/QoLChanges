using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QolChanges
{

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
            else if (___currentUI != null && Plugin.VariablesTowerUIDict.Count > 0 && Plugin.VariablesTowerUIDict[0].Item2.showingCircle)
            {
                decrementTimer();
            }
            return true;
        }

        public static void decrementTimer()
        {
            if (Plugin.VariablesTowerUIDict[0].Item2.circleTimer < 0)
            {
                Plugin.VariablesTowerUIDict[0].Item1.CloseUI();
            }
            else
            {
                Plugin.VariablesTowerUIDict[0].Item2.circleTimer -= Time.deltaTime;
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
}
