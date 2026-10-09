using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Zone cadence, reward growth and which wheel each tier uses.</summary>
    [CreateAssetMenu(menuName = "Game/Zone Progression", fileName = "zone_progression")]
    public sealed class ZoneProgressionConfig : ScriptableObject
    {
        [SerializeField, Min(1)] int safeZoneInterval = 5;
        [SerializeField, Min(1)] int superZoneInterval = 30;
        [SerializeField, Min(0f), Tooltip("Reward amount = base * (1 + growth * (zone - 1))")]
        float growthPerZone = 0.25f;
        [SerializeField] WheelTierConfig bronzeWheel;
        [SerializeField] WheelTierConfig silverWheel;
        [SerializeField] WheelTierConfig goldenWheel;

        public ProgressionRules Rules => new(safeZoneInterval, superZoneInterval, growthPerZone);

        public WheelTierConfig GetWheel(WheelTier tier) => tier switch
        {
            WheelTier.Bronze => bronzeWheel,
            WheelTier.Silver => silverWheel,
            WheelTier.Golden => goldenWheel,
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
        };
    }
}
