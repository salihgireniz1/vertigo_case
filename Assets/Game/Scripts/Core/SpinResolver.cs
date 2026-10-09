using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Weighted random slice selection.</summary>
    public static class SpinResolver
    {
        public static int Pick(IReadOnlyList<WheelSliceData> slices, IRandom random)
        {
            var total = 0f;
            for (var i = 0; i < slices.Count; i++) total += Math.Max(0f, slices[i].Weight);
            if (total <= 0f) throw new ArgumentException("Wheel has no slice with a positive weight", nameof(slices));

            var roll = random.NextFloat() * total;
            var cumulative = 0f;
            var lastPositive = -1;
            for (var i = 0; i < slices.Count; i++)
            {
                var weight = Math.Max(0f, slices[i].Weight);
                if (weight <= 0f) continue;
                lastPositive = i;
                cumulative += weight;
                if (roll < cumulative) return i;
            }
            return lastPositive; // float drift guard: roll == total
        }
    }
}
