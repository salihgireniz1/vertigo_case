namespace Wheel.Rules
{
    /// <summary>Pure zone arithmetic: which wheel a zone uses and how rewards scale.</summary>
    public static class ZoneRules
    {
        public static WheelTier GetTier(int zone, in ProgressionRules rules)
        {
            if (zone % rules.SuperZoneInterval == 0) return WheelTier.Golden;
            if (zone % rules.SafeZoneInterval == 0) return WheelTier.Silver;
            return WheelTier.Bronze;
        }

        public static bool IsSafe(int zone, in ProgressionRules rules) => GetTier(zone, rules) != WheelTier.Bronze;

        /// <summary>amount = base * (100 + growth * (zone - 1)) / 100, rounded half up. Integer math only.</summary>
        public static int ScaleAmount(int baseAmount, int zone, in ProgressionRules rules)
        {
            var percent = 100L + (long)rules.GrowthPercentPerZone * (zone - 1);
            return (int)((baseAmount * percent + 50) / 100);
        }

        /// <summary>First safe (silver) zone at or after <paramref name="zone"/>.</summary>
        public static int NextSafeZone(int zone, in ProgressionRules rules) => NextMultiple(zone, rules.SafeZoneInterval);

        /// <summary>First super (golden) zone at or after <paramref name="zone"/>.</summary>
        public static int NextSuperZone(int zone, in ProgressionRules rules) => NextMultiple(zone, rules.SuperZoneInterval);

        static int NextMultiple(int value, int interval) => (value + interval - 1) / interval * interval;
    }
}
