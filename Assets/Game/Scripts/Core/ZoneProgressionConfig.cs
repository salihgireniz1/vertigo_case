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

        public WheelTierConfig GetWheel(WheelTier tier)
        {
            var wheel = tier switch
            {
                WheelTier.Bronze => bronzeWheel,
                WheelTier.Silver => silverWheel,
                WheelTier.Golden => goldenWheel,
                _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
            };
            if (wheel == null) throw new InvalidOperationException($"{name}: no wheel assigned for the {tier} tier");
            return wheel;
        }

        /// <summary>Returns null when every tier has a valid wheel of the matching tier, otherwise the first problem.</summary>
        public string Validate()
        {
            foreach (var tier in (WheelTier[])Enum.GetValues(typeof(WheelTier)))
            {
                WheelTierConfig wheel;
                try { wheel = GetWheel(tier); }
                catch (InvalidOperationException e) { return e.Message; }
                if (wheel.Tier != tier) return $"{wheel.name} is a {wheel.Tier} wheel but sits in the {tier} slot";
                var problem = wheel.Validate();
                if (problem != null) return $"{wheel.name}: {problem}";
            }
            return null;
        }

        void OnValidate()
        {
            var problem = Validate();
            if (problem != null) Debug.LogError($"{name}: {problem}", this);
        }
    }
}
