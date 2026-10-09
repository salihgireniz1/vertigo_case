using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Runtime.State;
using Game.Runtime.Views;
using R3;
using Wheel.Rules;

namespace Game.Runtime.Presenters
{
    public sealed class ZoneStripPresenter : IDisposable
    {
        DisposableBag subscriptions;

        public ZoneStripPresenter(RunStore store, ProgressionRules rules, ZoneStripView view, CancellationToken cancellationToken)
        {
            Func<int, WheelTier> tierOf = zone => ZoneRules.GetTier(zone, rules);
            store.Snapshot
                .Select(snapshot => snapshot.Zone)
                .DistinctUntilChanged()
                .Subscribe(zone => view.ShowZoneAsync(zone, tierOf, cancellationToken).Forget())
                .AddTo(ref subscriptions);
        }

        public void Dispose() => subscriptions.Dispose();
    }
}
