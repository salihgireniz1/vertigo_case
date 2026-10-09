using System;
using System.Collections.Generic;

namespace Wheel.Rules
{
    /// <summary>One slice of a wheel: a reward with a base amount, or the bomb (its reward id only names the icon).</summary>
    public sealed class SliceDefinition
    {
        public string RewardId { get; }
        public int BaseAmount { get; }
        public float Weight { get; }
        public bool IsBomb { get; }

        public SliceDefinition(string rewardId, int baseAmount, float weight, bool isBomb)
        {
            RewardId = rewardId;
            BaseAmount = baseAmount;
            Weight = weight;
            IsBomb = isBomb;
        }
    }

    /// <summary>The content of one wheel tier.</summary>
    public sealed class WheelDefinition
    {
        public const int SliceCount = 8;

        public WheelTier Tier { get; }
        public IReadOnlyList<SliceDefinition> Slices { get; }

        public WheelDefinition(WheelTier tier, IReadOnlyList<SliceDefinition> slices)
        {
            Tier = tier;
            Slices = slices ?? throw new ArgumentNullException(nameof(slices));
        }

        /// <summary>Returns null when the wheel is playable, otherwise the first problem found.</summary>
        public string Validate()
        {
            if (Slices.Count != SliceCount) return $"must have exactly {SliceCount} slices, has {Slices.Count}";

            var bombs = 0;
            for (var i = 0; i < Slices.Count; i++)
            {
                var slice = Slices[i];
                if (slice == null) return $"slice {i} is empty";
                if (string.IsNullOrEmpty(slice.RewardId)) return $"slice {i} has no reward id";
                if (slice.IsBomb) bombs++;
                else if (slice.Weight <= 0f) return $"slice {i} can never be hit (weight {slice.Weight})";
            }

            if (Tier == WheelTier.Bronze && bombs != 1) return $"bronze wheel must have exactly one bomb, has {bombs}";
            if (Tier != WheelTier.Bronze && bombs > 0) return $"{Tier} wheel is risk-free and must not contain a bomb";
            return null;
        }
    }
}
