using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QolChanges
{
    [HarmonyPatch(typeof(TowerUI))]
    public class TowerUIPatches
    {
        [HarmonyPatch("SetStats")]
        [HarmonyPrefix]
        /// <summary>
        /// Stops the SetStats call if the _myTower is null (if its a "towerlessCircle" function call).
        /// </summary>
        public static bool SetStatsPatch(TowerUI __instance, Tower _myTower)
        {
            return _myTower != null;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Update(TowerUI __instance, Tower ___myTower)
        {
            if (___myTower == null)
                return;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.C))
                {
                    // copy
                    for (int i = 0; i < ___myTower.priorities.Length; i++)
                    {
                        // sets the priorities on update.
                        Plugin.copiedPriorityDict[i] = ___myTower.priorities[i];
                    }
                    DamageNumber component = ObjectPool.instance.SpawnObject(ObjectPool.ObjectType.DamageNumber, ___myTower.transform.position + Vector3.up, Quaternion.identity).GetComponent<DamageNumber>();
                    component.SetText("Copied", "Grey", 2f);
                    component.SetHoldTime(0.5f);
                }
                else if (Input.GetKeyDown(KeyCode.V))
                {
                    //paste
                    for (int i = 0; i < ___myTower.priorities.Length; i++)
                    {
                        // sets the priorities on update.
                        ___myTower.priorities[i] = Plugin.copiedPriorityDict[i];
                    }
                    DamageNumber component = ObjectPool.instance.SpawnObject(ObjectPool.ObjectType.DamageNumber, ___myTower.transform.position + Vector3.up, Quaternion.identity).GetComponent<DamageNumber>();
                    component.SetText("Pasted", "Grey", 2f);
                    component.SetHoldTime(0.25f);
                    TowerPatches.UpdateRendering(___myTower);
                }
            }
        }
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

        /// <summary>
        /// Draws the range of a tower without needing the Tower object to be instantiated.
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="pos"> Where the circle is to be drawn</param>
        /// <param name="tower"> A generic tower object. </param>
        public static void towerlessCircle(this TowerUI __instance, Vector3 pos, Tower tower) //float range, bool squareUI)
        {
            int heightBonus = (int)Mathf.Round(pos.y * 3f - 1f);
            float baseRange = (float)tower.GetType().GetField("baseRange", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tower);
            float range = baseRange + (float)heightBonus / 2f + TowerManager.instance.GetBonusRange(tower.towerType);
            LineRenderer linePrefab = (LineRenderer)__instance.GetType().GetField("line", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            LineRenderer line = Object.Instantiate<LineRenderer>(linePrefab, pos, Quaternion.identity).GetComponent<LineRenderer>();

            if (Plugin.SquareTowers.Contains(tower.towerType))
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
            }
            else
            {
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
            }
            line.gameObject.SetActive(true);
            // hides all except the line renderer
            foreach (Transform child in __instance.gameObject.transform)
            {
                if (child.GetComponent<LineRenderer>() == null)
                    child.gameObject.SetActive(false);
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
}
