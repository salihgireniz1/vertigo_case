using System;
using Game.Core;
using Game.Runtime.Views;
using R3;

namespace Game.Runtime.Presenters
{
    /// <summary>Binds zone badges, gold and exit availability to the session.</summary>
    public sealed class HudPresenter : IDisposable
    {
        DisposableBag subscriptions;

        public HudPresenter(GameSession session, ProgressionRules rules, HudView view)
        {
            session.Zone.Subscribe(zone =>
            {
                view.SetNextSafeZone(ZoneRules.NextSafeZone(zone, rules));
                view.SetNextSuperZone(ZoneRules.NextSuperZone(zone, rules));
            }).AddTo(ref subscriptions);

            session.Gold.Subscribe(view.SetGold).AddTo(ref subscriptions);

            session.State.CombineLatest(session.Zone, (state, zone) => state == GameState.Idle && ZoneRules.IsSafe(zone, rules))
                .Subscribe(view.SetExitInteractable)
                .AddTo(ref subscriptions);
        }

        public void Dispose() => subscriptions.Dispose();
    }
}
