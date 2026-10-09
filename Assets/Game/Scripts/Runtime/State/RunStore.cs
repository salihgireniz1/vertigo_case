using System;
using R3;
using Wheel.Rules;

namespace Game.Runtime.State
{
    /// <summary>The client's copy of the run. Only snapshots from the service are applied; views bind here.</summary>
    public sealed class RunStore : IDisposable
    {
        readonly ReactiveProperty<RunSnapshot> snapshot = new(new RunSnapshot());
        readonly ReactiveProperty<bool> busy = new(false);

        public ReadOnlyReactiveProperty<RunSnapshot> Snapshot => snapshot;
        /// <summary>True while a request or its animation is in flight; input is ignored meanwhile.</summary>
        public ReadOnlyReactiveProperty<bool> Busy => busy;
        public RunSnapshot Current => snapshot.Value;

        public Observable<bool> CanSpin => snapshot.CombineLatest(busy, (s, b) => s.CanSpin && !b);
        public Observable<bool> CanLeave => snapshot.CombineLatest(busy, (s, b) => s.CanLeave && !b);

        public void Apply(RunSnapshot next) => snapshot.Value = next ?? throw new ArgumentNullException(nameof(next));
        public void SetBusy(bool value) => busy.Value = value;

        public void Dispose()
        {
            snapshot.Dispose();
            busy.Dispose();
        }
    }
}
