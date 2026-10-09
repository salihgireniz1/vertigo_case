using System.Collections.Generic;

namespace Wheel.Rules
{
    public sealed class RewardStack
    {
        public string RewardId { get; }
        public int Amount { get; internal set; }

        internal RewardStack(string rewardId) => RewardId = rewardId;
    }

    /// <summary>Rewards collected in the current run, stacked per reward id in pickup order.</summary>
    public sealed class RewardInventory
    {
        readonly List<RewardStack> stacks = new List<RewardStack>();

        public IReadOnlyList<RewardStack> Stacks => stacks;

        public void Add(string rewardId, int amount)
        {
            var stack = Find(rewardId);
            if (stack == null)
            {
                stack = new RewardStack(rewardId);
                stacks.Add(stack);
            }
            stack.Amount += amount;
        }

        public void Clear() => stacks.Clear();

        public int AmountOf(string rewardId) => Find(rewardId)?.Amount ?? 0;

        public RewardStackDto[] ToDto()
        {
            var result = new RewardStackDto[stacks.Count];
            for (var i = 0; i < stacks.Count; i++)
                result[i] = new RewardStackDto { RewardId = stacks[i].RewardId, Amount = stacks[i].Amount };
            return result;
        }

        RewardStack Find(string rewardId)
        {
            foreach (var stack in stacks)
                if (stack.RewardId == rewardId) return stack;
            return null;
        }
    }
}
