namespace Game.Core
{
    /// <summary>Zone cadence and reward growth. Plain data so the model stays free of asset types.</summary>
    public readonly struct ProgressionRules
    {
        public readonly int SafeZoneInterval;
        public readonly int SuperZoneInterval;
        public readonly float GrowthPerZone;

        public ProgressionRules(int safeZoneInterval, int superZoneInterval, float growthPerZone)
        {
            SafeZoneInterval = safeZoneInterval;
            SuperZoneInterval = superZoneInterval;
            GrowthPerZone = growthPerZone;
        }
    }

    public readonly struct GameRules
    {
        public readonly ProgressionRules Progression;
        public readonly int StartingGold;
        public readonly int ReviveCost;

        public GameRules(ProgressionRules progression, int startingGold, int reviveCost)
        {
            Progression = progression;
            StartingGold = startingGold;
            ReviveCost = reviveCost;
        }
    }
}
