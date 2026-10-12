using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Data;
using R3;

namespace Game.Runtime.Views
{
    /// <summary>What the flow needs from the wheel. Scene views implement these; tests use fakes.</summary>
    public interface IWheelView
    {
        Observable<Unit> SpinClicked { get; }
        UniTask SpinToAsync(int sliceIndex, CancellationToken cancellationToken);
        void PlayWin(int sliceIndex);
        void PlayBomb();
    }

    public interface IHudView
    {
        Observable<Unit> ExitClicked { get; }
    }

    public interface IPopupView
    {
        UniTask HideAsync(CancellationToken cancellationToken);
    }

    public interface IBombPopupView : IPopupView
    {
        Observable<Unit> ReviveClicked { get; }
        Observable<Unit> GiveUpClicked { get; }
        UniTask ShowAsync(int reviveCost, bool canRevive, CancellationToken cancellationToken);
    }

    public interface ILeavePopupView : IPopupView
    {
        Observable<Unit> PlayAgainClicked { get; }
        UniTask ShowAsync(int zone, IReadOnlyList<RewardEntry> rewards, CancellationToken cancellationToken);
    }

    /// <summary>"Connection lost": shows the reason and completes only when the player asks to retry.</summary>
    public interface IErrorPopupView : IPopupView
    {
        UniTask ShowUntilRetryAsync(string reason, CancellationToken cancellationToken);
    }

    /// <summary>A reward stack resolved to its client asset.</summary>
    public readonly struct RewardEntry
    {
        public readonly RewardDefinition Reward;
        public readonly int Amount;

        public RewardEntry(RewardDefinition reward, int amount)
        {
            Reward = reward;
            Amount = amount;
        }
    }
}
