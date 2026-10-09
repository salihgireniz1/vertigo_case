using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core;
using Game.Runtime.Views;
using R3;

namespace Game.Runtime.Flow
{
    /// <summary>Turns button presses into session transitions and sequences the animations between them.</summary>
    public sealed class GameFlow : IDisposable
    {
        readonly GameSession session;
        readonly ZoneProgressionConfig progression;
        readonly WheelView wheel;
        readonly BombPopupView bombPopup;
        readonly LeavePopupView leavePopup;
        readonly TimeSpan resultHold;
        readonly CancellationTokenSource lifetime;
        readonly CancellationToken cancellationToken;
        DisposableBag subscriptions;

        public GameFlow(
            GameSession session,
            ZoneProgressionConfig progression,
            WheelView wheel,
            HudView hud,
            BombPopupView bombPopup,
            LeavePopupView leavePopup,
            float resultHoldSeconds,
            CancellationToken parentCancellation)
        {
            this.session = session;
            this.progression = progression;
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

        async UniTaskVoid SpinAsync()
        {
            if (session.State.CurrentValue != GameState.Idle) return;

            var slices = progression.GetWheel(session.CurrentTier).Slices;
            var result = session.StartSpin(slices);
            await wheel.SpinToAsync(result.SliceIndex, cancellationToken);

            if (result.IsBomb)
            {
                wheel.PlayBomb();
                await UniTask.Delay(resultHold, cancellationToken: cancellationToken);
                session.ApplySpinResult(result);
                await bombPopup.ShowAsync(session.Rules.ReviveCost, session.CanRevive, cancellationToken);
                return;
            }

            wheel.PlayWin(result.SliceIndex);
            await UniTask.Delay(resultHold, cancellationToken: cancellationToken);
            session.ApplySpinResult(result);
        }

        async UniTaskVoid LeaveAsync()
        {
            var zone = session.Zone.CurrentValue;
            if (!session.TryLeave()) return;
            await leavePopup.ShowAsync(zone, session.Rewards.Stacks, cancellationToken);
        }

        async UniTaskVoid ReviveAsync()
        {
            if (!session.TryRevive()) return;
            await bombPopup.HideAsync(cancellationToken);
        }

        // The session transition happens first so a second tap during the hide tween cannot act on a stale state.
        async UniTaskVoid RestartAsync(PopupView popup)
        {
            if (!session.TryRestart()) return;
            await popup.HideAsync(cancellationToken);
        }

        public void Dispose()
        {
            subscriptions.Dispose();
            lifetime.Cancel();
            lifetime.Dispose();
        }
    }
}
