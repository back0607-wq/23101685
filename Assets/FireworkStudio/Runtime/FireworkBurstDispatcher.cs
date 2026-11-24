using UnityEngine;

namespace FireworkStudio
{
    public static class FireworkBurstDispatcher
    {
        public static void Spawn(Vector3 skyPosition, FireworkPattern pattern, FireworkPalette palette,
            bool useCustomColor, Color customColor, FireworkShowMode mode,
            CrackleOnlyOptions options, Quaternion orientation)
        {
            if (FireworkShowRules.UsesIndependentCrackle(mode))
            {
                CrackleOnlyBurst.Spawn(skyPosition, orientation, pattern, palette,
                    useCustomColor, customColor, options);
                return; // Never build the main burst or the old afterburst for this mode.
            }
            VideoBurstController.Spawn(skyPosition, pattern, palette, useCustomColor,
                customColor, FireworkShowRules.UsesLegacyAfterburst(mode));
        }
    }
}
