using System;
using System.Collections.Generic;

namespace Wheel.Rules
{
    /// <summary>Weighted random slice selection.</summary>
    public static class SpinResolver
    {
        public static int Pick(IReadOnlyList<SliceDefinition> slices, IRandom random, bool excludeBombs = false)
        {
            var total = 0f;
            for (var i = 0; i < slices.Count; i++) total += EffectiveWeight(slices[i], excludeBombs);
            if (total <= 0f) throw new ArgumentException("Wheel has no slice with a positive weight", nameof(slices));

            var roll = random.NextFloat() * total;
            var cumulative = 0f;
            var lastPositive = -1;
            for (var i = 0; i < slices.Count; i++)
            {
                var weight = EffectiveWeight(slices[i], excludeBombs);
                if (weight <= 0f) continue;
                lastPositive = i;
                cumulative += weight;
                if (roll < cumulative) return i;
            }
            return lastPositive; // float drift guard: roll == total
        }

        static float EffectiveWeight(SliceDefinition slice, bool excludeBombs) =>
            excludeBombs && slice.IsBomb ? 0f : Math.Max(0f, slice.Weight);
    }
}
