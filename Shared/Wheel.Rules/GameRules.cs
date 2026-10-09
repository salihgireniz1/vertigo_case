namespace Wheel.Rules
{
    /// <summary>Zone cadence and reward growth.</summary>
    public readonly struct ProgressionRules
    {
        public readonly int SafeZoneInterval;
        public readonly int SuperZoneInterval;
        /// <summary>Percent growth per zone. Integer so client and server scale identically.</summary>
        public readonly int GrowthPercentPerZone;

        public ProgressionRules(int safeZoneInterval, int superZoneInterval, int growthPercentPerZone)
        {
            SafeZoneInterval = safeZoneInterval;
            SuperZoneInterval = superZoneInterval;
            GrowthPercentPerZone = growthPercentPerZone;
        }
    }

    public readonly struct GameRules
    {
        public readonly ProgressionRules Progression;
        public readonly int StartingGold;
        public readonly int ReviveCost;
        /// <summary>Reward id that is credited to the wallet when the player leaves with it (null: none).</summary>
        public readonly string WalletRewardId;

        public GameRules(ProgressionRules progression, int startingGold, int reviveCost, string walletRewardId = null)
        {
            Progression = progression;
            StartingGold = startingGold;
            ReviveCost = reviveCost;
            WalletRewardId = walletRewardId;
        }
    }
}
