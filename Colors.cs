using System.Collections.Generic;
using UnityEngine;

namespace QolChanges
{
    public static class Colors
    {
        // Colors taken directly from UnityExplorer properties panel
        public static Color HealthColor = new(0.533f, 0.8f, 0.533f);
        public static Color ArmorColor = new(1, 1, 0.667f);
        public static Color ShieldColor = new(0.4706f, 0.5294f, 0.6706f);
        public static Color BleedColor = new(1, 0.3451f, 0.3451f);
        public static Color BurnColor = new(1, 0.6431f, 0.3529f);
        public static Color PoisonColor = new(0.6902f, 0.3647f, 1);
        public static Color DamageColor = new(0.8314f, 0.4157f, 0.4157f);
        public static Color HasteColor = new(0.8314f, 0.6941f, 0.4157f);
        // the slow debuff is the same color as haste, just different placement

        // Color estimated by setting sunlight to pure white (with UnityExplorer) and screengrabbing
        internal static Color towerBaseColor = new(0.596f, 0.588f, 0.537f);

        public static Dictionary<Tower.Priority, Color> PriorityColors = new()
        {
            // Progress: use the natural tower base
            { Tower.Priority.Progress, towerBaseColor },
            // Near death: use the color of damage on a monster health bar
            { Tower.Priority.NearDeath, DamageColor },
            // Most health/armor/shield: use the bar color
            { Tower.Priority.MostHealth, HealthColor },
            { Tower.Priority.MostArmor, ArmorColor },
            { Tower.Priority.MostShield, ShieldColor },
            // Least health/armor/shield: darkened versions of the same
            { Tower.Priority.LeastHealth, Color.Lerp(HealthColor, Color.black, 0.5f) },
            { Tower.Priority.LeastArmor, Color.Lerp(ArmorColor, Color.black, 0.5f) },
            { Tower.Priority.LeastShield, Color.Lerp(ShieldColor, Color.black, 0.5f) },
            // Fastest: use the haste buff color
            { Tower.Priority.Fastest, HasteColor },
            // Slowest: use a darkened haste buff color
            { Tower.Priority.Slowest, Color.Lerp(HasteColor, Color.black, 0.5f) },
            // Marked: use a darkened pure red
            { Tower.Priority.Marked, Color.Lerp(Color.red, Color.black, 0.25f) },
        };

        public static Color GetColor(this Tower.Priority priority)
        {
            if (!PriorityColors.TryGetValue(priority, out Color color))
            {
                color = Color.Lerp(towerBaseColor, Color.white, 0.5f);
            }
            return color;
        }

        // Scale the color tinting to "remove" the inherent warm gray of the tower base. This may
        // result in a Color with values over 1; this is fine.
        internal static Color ScaleForTowerDisplay(this Color color)
        {
            return new(color.r / towerBaseColor.r,
                       color.g / towerBaseColor.g,
                       color.b / towerBaseColor.b);
        }
    }
}
