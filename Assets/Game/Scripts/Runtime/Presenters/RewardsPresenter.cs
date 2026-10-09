using System;
using System.Collections.Generic;
using Game.Data;
using Game.Runtime.State;
using Game.Runtime.Views;
using R3;
using Wheel.Rules;

namespace Game.Runtime.Presenters
{
    /// <summary>Mirrors the snapshot's reward stacks into the side panel list, punching stacks that grew.</summary>
    public sealed class RewardsPresenter : IDisposable
    {
        readonly RewardCatalog catalog;
        readonly RewardListView view;
        readonly Dictionary<string, int> shown = new();
        DisposableBag subscriptions;

        public RewardsPresenter(RunStore store, RewardCatalog catalog, RewardListView view)
        {
            this.catalog = catalog;
            this.view = view;
            store.Snapshot.Subscribe(Render).AddTo(ref subscriptions);
        }

        void Render(RunSnapshot snapshot)
        {
            if (snapshot.Rewards.Length == 0)
            {
                if (shown.Count > 0) view.Clear();
                shown.Clear();
                return;
            }

            foreach (var stack in snapshot.Rewards)
            {
                var changed = !shown.TryGetValue(stack.RewardId, out var amount) || amount != stack.Amount;
                if (!changed) continue;
                var reward = catalog.Get(stack.RewardId);
                var item = view.GetOrCreate(reward);
                item.Bind(reward.Icon, stack.Amount);
                item.Punch();
                shown[stack.RewardId] = stack.Amount;
            }
        }

        public void Dispose() => subscriptions.Dispose();
    }
}
