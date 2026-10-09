using System.Collections.Generic;
using R3;

namespace Game.Core
{
    public sealed class RewardStack
    {
        public RewardDefinition Reward { get; }
        public int Amount { get; internal set; }

        internal RewardStack(RewardDefinition reward) => Reward = reward;
    }

    /// <summary>Rewards collected in the current run, stacked per reward type in pickup order.</summary>
    public sealed class RewardInventory
    {
        readonly List<RewardStack> stacks = new();
        readonly Subject<RewardStack> changed = new();
        readonly Subject<Unit> cleared = new();

        public IReadOnlyList<RewardStack> Stacks => stacks;
        /// <summary>Emits the stack that was created or grew.</summary>
        public Observable<RewardStack> Changed => changed;
        public Observable<Unit> Cleared => cleared;

        public void Add(RewardDefinition reward, int amount)
        {
            var stack = Find(reward);
            if (stack == null)
            {
                stack = new RewardStack(reward);
                stacks.Add(stack);
            }
            stack.Amount += amount;
            changed.OnNext(stack);
        }

        public void Clear()
        {
            stacks.Clear();
            cleared.OnNext(Unit.Default);
        }

        RewardStack Find(RewardDefinition reward)
        {
            foreach (var stack in stacks)
                if (stack.Reward == reward) return stack;
            return null;
        }
    }
}
