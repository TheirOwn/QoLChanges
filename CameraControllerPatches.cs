using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QolChanges
{
    [HarmonyPatch(typeof(CameraController))]
    public class CameraControllerPatches
    {
        [HarmonyPatch("UpdateMovement")]
        [HarmonyPrefix]
        public static bool UpdateMovement_MyPatch(CameraController __instance)
        {
            return !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl));
        }



    }
}
