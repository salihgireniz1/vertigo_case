using System;
using Game.Runtime.State;
using Game.Runtime.Views;
using R3;
using Wheel.Rules;

namespace Game.Runtime.Presenters
{
    /// <summary>Binds zone badges, gold and exit availability to the run store.</summary>
    public sealed class HudPresenter : IDisposable
    {
        DisposableBag subscriptions;

        public HudPresenter(RunStore store, ProgressionRules rules, HudView view)
        {
            store.Snapshot.Subscribe(snapshot =>
            {
                view.SetNextSafeZone(ZoneRules.NextSafeZone(snapshot.Zone, rules));
                view.SetNextSuperZone(ZoneRules.NextSuperZone(snapshot.Zone, rules));
                view.SetGold(snapshot.Gold);
            }).AddTo(ref subscriptions);

            store.CanLeave.Subscribe(view.SetExitInteractable).AddTo(ref subscriptions);
        }

        public void Dispose() => subscriptions.Dispose();
    }
}
