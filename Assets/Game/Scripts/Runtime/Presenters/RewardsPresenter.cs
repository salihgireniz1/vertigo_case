using System;
using Game.Core;
using Game.Runtime.Views;
using R3;

namespace Game.Runtime.Presenters
{
    /// <summary>Mirrors the reward inventory into the side panel list.</summary>
    public sealed class RewardsPresenter : IDisposable
    {
        DisposableBag subscriptions;

        public RewardsPresenter(RewardInventory inventory, RewardListView view)
        {
            inventory.Changed.Subscribe(stack =>
            {
                var item = view.GetOrCreate(stack.Reward);
                item.Bind(stack.Reward.Icon, stack.Amount);
                item.Punch();
            }).AddTo(ref subscriptions);

            inventory.Cleared.Subscribe(_ => view.Clear()).AddTo(ref subscriptions);
        }

        public void Dispose() => subscriptions.Dispose();
    }
}
