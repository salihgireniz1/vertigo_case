using System;
using System.Collections.Generic;
using Wheel.Rules;

namespace Game.Data
{
    /// <summary>Maps the reward ids the rules and server use back to the client's presentation assets.</summary>
    public sealed class RewardCatalog
    {
        readonly Dictionary<string, RewardDefinition> byId = new();

        public static RewardCatalog From(ZoneProgressionConfig progression)
        {
            var catalog = new RewardCatalog();
            foreach (var tier in (WheelTier[])Enum.GetValues(typeof(WheelTier)))
                foreach (var slice in progression.GetWheel(tier).Slices)
                    if (slice?.Reward != null) catalog.byId[slice.Reward.Id] = slice.Reward;
            return catalog;
        }

        public RewardDefinition Get(string id) =>
            byId.TryGetValue(id, out var reward) ? reward : throw new KeyNotFoundException($"No reward asset for id '{id}'");
    }
}
