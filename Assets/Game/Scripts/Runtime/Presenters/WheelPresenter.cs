using System;
using Game.Core;
using Game.Runtime.UI;
using Game.Runtime.Views;
using R3;

namespace Game.Runtime.Presenters
{
    /// <summary>Renders the wheel for the current zone's tier and gates the spin button on state.</summary>
    public sealed class WheelPresenter : IDisposable
    {
        readonly ZoneProgressionConfig progression;
        readonly WheelView view;
        DisposableBag subscriptions;

        public WheelPresenter(GameSession session, ZoneProgressionConfig progression, WheelView view)
        {
            this.progression = progression;
            this.view = view;
            session.Zone.Subscribe(Render).AddTo(ref subscriptions);
            session.State.Subscribe(state => view.SetSpinInteractable(state == GameState.Idle)).AddTo(ref subscriptions);
        }

        void Render(int zone)
        {
            var rules = progression.Rules;
            var wheel = progression.GetWheel(ZoneRules.GetTier(zone, rules));
            view.SetTier(wheel.BaseSprite, wheel.IndicatorSprite, wheel.Title, wheel.Subtitle);

            // Counts are verified once in GameBootstrap; a mismatch here would mean a config edited at runtime.
            for (var i = 0; i < view.SliceCount; i++)
            {
                var slice = wheel.Slices[i];
                var icon = slice.Reward != null ? slice.Reward.Icon : null;
                var label = slice.IsBomb ? string.Empty : Formatting.Amount(ZoneRules.ScaleAmount(slice.BaseAmount, zone, rules));
                view.GetSlice(i).Bind(icon, label);
            }
        }

        public void Dispose() => subscriptions.Dispose();
    }
}
