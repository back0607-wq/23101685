namespace FireworkStudio
{
    // Do not reorder: value 0 preserves all pre-existing serialized rocket props.
    public enum FireworkShowMode
    {
        Combined = 0,
        TrailsOnly = 1,
        CrackleOnly = 2
    }

    public static class FireworkShowRules
    {
        public static bool UsesMain(FireworkShowMode mode)
        { return mode != FireworkShowMode.CrackleOnly; }
        public static bool UsesLegacyAfterburst(FireworkShowMode mode)
        { return mode == FireworkShowMode.Combined; }
        public static bool UsesIndependentCrackle(FireworkShowMode mode)
        { return mode == FireworkShowMode.CrackleOnly; }
    }
}
