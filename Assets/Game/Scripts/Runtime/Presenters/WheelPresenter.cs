using System;
using Game.Data;
using Game.Runtime.State;
using Game.Runtime.UI;
using Game.Runtime.Views;
using R3;
using Wheel.Rules;

namespace Game.Runtime.Presenters
{
    /// <summary>Renders the wheel for the snapshot's tier and gates the spin button.</summary>
    public sealed class WheelPresenter : IDisposable
    {
        readonly ZoneProgressionConfig progression;
        readonly WheelView view;
        DisposableBag subscriptions;

        public WheelPresenter(RunStore store, ZoneProgressionConfig progression, WheelView view)
        {
            this.progression = progression;
            this.view = view;
            store.Snapshot.Subscribe(Render).AddTo(ref subscriptions);
            store.CanSpin.Subscribe(view.SetSpinInteractable).AddTo(ref subscriptions);
        }

        void Render(RunSnapshot snapshot)
        {
            var rules = progression.Rules;
            var wheel = progression.GetWheel(snapshot.Tier);
            var subtitle = snapshot.BombDisabled ? "Revived: the bomb chamber is disabled for this spin" : wheel.Subtitle;
            view.SetTier(wheel.BaseSprite, wheel.IndicatorSprite, wheel.Title, subtitle);

            // Amounts shown here are the client's prediction from the shared rules; the server's outcome is what gets banked.
            for (var i = 0; i < view.SliceCount; i++)
            {
                var slice = wheel.Slices[i];
                var icon = slice.Reward != null ? slice.Reward.Icon : null;
                if (slice.IsBomb && snapshot.BombDisabled) view.GetSlice(i).BindDisabled(icon);
                else if (slice.IsBomb) view.GetSlice(i).Bind(icon, string.Empty);
                else view.GetSlice(i).Bind(icon, Formatting.Amount(ZoneRules.ScaleAmount(slice.BaseAmount, snapshot.Zone, rules)));
            }
        }

        public void Dispose() => subscriptions.Dispose();
    }
}
