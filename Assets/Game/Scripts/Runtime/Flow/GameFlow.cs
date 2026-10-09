using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Data;
using Game.Runtime.Services;
using Game.Runtime.State;
using Game.Runtime.Views;
using R3;
using Wheel.Rules;

namespace Game.Runtime.Flow
{
    /// <summary>
    /// Turns button presses into service intents and sequences the animations around the answers.
    /// Every guard reads the store's last snapshot, so the client only asks for what the authority already allowed.
    /// </summary>
    public sealed class GameFlow : IDisposable
    {
        readonly IRunService service;
        readonly RunStore store;
        readonly RewardCatalog catalog;
        readonly IWheelView wheel;
        readonly IBombPopupView bombPopup;
        readonly ILeavePopupView leavePopup;
        readonly TimeSpan resultHold;
        readonly CancellationTokenSource lifetime;
        readonly CancellationToken cancellationToken;
        DisposableBag subscriptions;

        public GameFlow(
            IRunService service,
            RunStore store,
            RewardCatalog catalog,
            IWheelView wheel,
            IHudView hud,
            IBombPopupView bombPopup,
            ILeavePopupView leavePopup,
            float resultHoldSeconds,
            CancellationToken parentCancellation)
        {
            this.service = service;
            this.store = store;
            this.catalog = catalog;
            this.wheel = wheel;
            this.bombPopup = bombPopup;
            this.leavePopup = leavePopup;
            resultHold = TimeSpan.FromSeconds(resultHoldSeconds);
            lifetime = CancellationTokenSource.CreateLinkedTokenSource(parentCancellation);
            cancellationToken = lifetime.Token;

            wheel.SpinClicked.Subscribe(_ => SpinAsync().Forget()).AddTo(ref subscriptions);
            hud.ExitClicked.Subscribe(_ => LeaveAsync().Forget()).AddTo(ref subscriptions);
            bombPopup.ReviveClicked.Subscribe(_ => ReviveAsync().Forget()).AddTo(ref subscriptions);
            bombPopup.GiveUpClicked.Subscribe(_ => RestartAsync(bombPopup).Forget()).AddTo(ref subscriptions);
            leavePopup.PlayAgainClicked.Subscribe(_ => RestartAsync(leavePopup).Forget()).AddTo(ref subscriptions);
        }

        public async UniTask StartAsync()
        {
            store.Apply(await service.StartAsync(cancellationToken));
        }

        async UniTaskVoid SpinAsync()
        {
            if (!TryBegin(store.Current.CanSpin)) return;
            try
            {
                var outcome = await service.SpinAsync(cancellationToken);
                await wheel.SpinToAsync(outcome.SliceIndex, cancellationToken);
                if (outcome.IsBomb) wheel.PlayBomb();
                else wheel.PlayWin(outcome.SliceIndex);
                await HoldAsync();
                store.Apply(outcome.Snapshot);
                if (outcome.IsBomb)
                    await bombPopup.ShowAsync(outcome.Snapshot.ReviveCost, outcome.Snapshot.CanRevive, cancellationToken);
            }
            finally { store.SetBusy(false); }
        }

        async UniTaskVoid LeaveAsync()
        {
            if (!TryBegin(store.Current.CanLeave)) return;
            try
            {
                var snapshot = await service.LeaveAsync(cancellationToken);
                store.Apply(snapshot);
                await leavePopup.ShowAsync(snapshot.Zone, Resolve(snapshot.Rewards), cancellationToken);
            }
            finally { store.SetBusy(false); }
        }

        async UniTaskVoid ReviveAsync()
        {
            if (!TryBegin(store.Current.CanRevive)) return;
            try
            {
                store.Apply(await service.ReviveAsync(cancellationToken));
                await bombPopup.HideAsync(cancellationToken);
            }
            finally { store.SetBusy(false); }
        }

        async UniTaskVoid RestartAsync(IPopupView popup)
        {
            if (!TryBegin(store.Current.State != GameState.Idle)) return;
            try
            {
                store.Apply(await service.RestartAsync(cancellationToken));
                await popup.HideAsync(cancellationToken);
            }
            finally { store.SetBusy(false); }
        }

        /// <summary>Single entry gate: one intent in flight at a time, and only when the last snapshot allows it.</summary>
        bool TryBegin(bool allowed)
        {
            if (store.Busy.CurrentValue || !allowed) return false;
            store.SetBusy(true);
            return true;
        }

        UniTask HoldAsync() => resultHold > TimeSpan.Zero ? UniTask.Delay(resultHold, cancellationToken: cancellationToken) : UniTask.CompletedTask;

        IReadOnlyList<RewardEntry> Resolve(RewardStackDto[] stacks)
        {
            var entries = new RewardEntry[stacks.Length];
            for (var i = 0; i < stacks.Length; i++) entries[i] = new RewardEntry(catalog.Get(stacks[i].RewardId), stacks[i].Amount);
            return entries;
        }

        public void Dispose()
        {
            subscriptions.Dispose();
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
