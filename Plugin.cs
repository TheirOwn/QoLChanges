using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace QolChanges
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, "1.4.0")]
    [BepInProcess("Rogue Tower.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        public static List<Tuple<TowerUI, VariablesTowerUI>> VariablesTowerUIDict = new List<Tuple<TowerUI, VariablesTowerUI>>();
        public static Dictionary<TowerType, Tower.Priority[]> defaultPriorityDict = new Dictionary<TowerType, Tower.Priority[]>();
        public static Tower.Priority[] copiedPriorityDict = new Tower.Priority[] { Tower.Priority.Progress, Tower.Priority.Progress, Tower.Priority.Progress};
        public static TowerUI ghostCircle;
        public static List<TowerType> SquareTowers = new List<TowerType>() {TowerType.Frost, TowerType.Encampment};

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
            harmony.PatchAll(typeof(TowerUIPatches));
            harmony.PatchAll(typeof(BuildingManagerPatches));
            harmony.PatchAll(typeof(CameraControllerPatches));
        }

        private void onDestroy()
        {
            Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.UnpatchSelf();
        }
    }


}
