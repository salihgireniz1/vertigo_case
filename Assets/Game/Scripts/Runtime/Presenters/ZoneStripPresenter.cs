using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Runtime.Views;
using R3;

namespace Game.Runtime.Presenters
{
    public sealed class ZoneStripPresenter : IDisposable
    {
        DisposableBag subscriptions;

        public ZoneStripPresenter(GameSession session, ProgressionRules rules, ZoneStripView view, CancellationToken cancellationToken)
        {
            Func<int, WheelTier> tierOf = zone => ZoneRules.GetTier(zone, rules);
            session.Zone
                .Subscribe(zone => view.ShowZoneAsync(zone, tierOf, cancellationToken).Forget())
                .AddTo(ref subscriptions);
        }

        public void Dispose() => subscriptions.Dispose();
    }
}
