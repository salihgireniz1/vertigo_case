using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Runtime.Services;
using Game.Runtime.Views;
using R3;
using Wheel.Rules;

namespace Game.Tests
{
    /// <summary>Scripted authority: hands out the queued outcomes and snapshots, counts calls.</summary>
    public sealed class FakeRunService : IRunService
    {
        public readonly Queue<SpinOutcome> Outcomes = new();
        public RunSnapshot NextSnapshot = Snapshots.Idle(1);
        public int SpinCalls, LeaveCalls, ReviveCalls, RestartCalls;

        public UniTask<RunSnapshot> StartAsync(CancellationToken cancellationToken) => UniTask.FromResult(NextSnapshot);
        public UniTask<SpinOutcome> SpinAsync(CancellationToken cancellationToken) { SpinCalls++; return UniTask.FromResult(Outcomes.Dequeue()); }
        public UniTask<RunSnapshot> LeaveAsync(CancellationToken cancellationToken) { LeaveCalls++; return UniTask.FromResult(NextSnapshot); }
        public UniTask<RunSnapshot> ReviveAsync(CancellationToken cancellationToken) { ReviveCalls++; return UniTask.FromResult(NextSnapshot); }
        public UniTask<RunSnapshot> RestartAsync(CancellationToken cancellationToken) { RestartCalls++; return UniTask.FromResult(NextSnapshot); }
    }

    /// <summary>Wheel whose spin animation completes only when the test says so.</summary>
    public sealed class FakeWheelView : IWheelView
    {
        public readonly Subject<Unit> Clicks = new();
        public readonly List<int> SpunTo = new();
        public int Wins, Bombs;
        UniTaskCompletionSource pendingSpin;

        public Observable<Unit> SpinClicked => Clicks;

        public UniTask SpinToAsync(int sliceIndex, CancellationToken cancellationToken)
        {
            SpunTo.Add(sliceIndex);
            pendingSpin = new UniTaskCompletionSource();
            return pendingSpin.Task;
        }

        public void FinishSpin() => pendingSpin.TrySetResult();
        public void PlayWin(int sliceIndex) => Wins++;
        public void PlayBomb() => Bombs++;
    }

    public sealed class FakeHudView : IHudView
    {
        public readonly Subject<Unit> Clicks = new();
        public Observable<Unit> ExitClicked => Clicks;
    }

    public sealed class FakeBombPopup : IBombPopupView
    {
        public readonly Subject<Unit> Revive = new(), GiveUp = new();
        public bool Visible;
        public int ShownCost;
        public bool ShownCanRevive;

        public Observable<Unit> ReviveClicked => Revive;
        public Observable<Unit> GiveUpClicked => GiveUp;

        public UniTask ShowAsync(int reviveCost, bool canRevive, CancellationToken cancellationToken)
        {
            Visible = true;
            ShownCost = reviveCost;
            ShownCanRevive = canRevive;
            return UniTask.CompletedTask;
        }

        public UniTask HideAsync(CancellationToken cancellationToken) { Visible = false; return UniTask.CompletedTask; }
    }

    public sealed class FakeLeavePopup : ILeavePopupView
    {
        public readonly Subject<Unit> PlayAgain = new();
        public bool Visible;
        public int ShownZone;
        public IReadOnlyList<RewardEntry> ShownRewards;

        public Observable<Unit> PlayAgainClicked => PlayAgain;

        public UniTask ShowAsync(int zone, IReadOnlyList<RewardEntry> rewards, CancellationToken cancellationToken)
        {
            Visible = true;
            ShownZone = zone;
            ShownRewards = rewards;
            return UniTask.CompletedTask;
        }

        public UniTask HideAsync(CancellationToken cancellationToken) { Visible = false; return UniTask.CompletedTask; }
    }

    public static class Snapshots
    {
        public static RunSnapshot Idle(int zone, int gold = 100, bool canLeave = false) => new()
        {
            Zone = zone, State = GameState.Idle, Tier = WheelTier.Bronze, Gold = gold, ReviveCost = 25,
            CanSpin = true, CanLeave = canLeave, CanRevive = false, Rewards = new RewardStackDto[0],
        };

        public static RunSnapshot Bombed(int zone, bool canRevive = true) => new()
        {
            Zone = zone, State = GameState.Bombed, Tier = WheelTier.Bronze, Gold = 100, ReviveCost = 25,
            CanSpin = false, CanLeave = false, CanRevive = canRevive, Rewards = new RewardStackDto[0],
        };

        public static RunSnapshot Left(int zone, params (string id, int amount)[] rewards)
        {
            var stacks = new RewardStackDto[rewards.Length];
            for (var i = 0; i < rewards.Length; i++) stacks[i] = new RewardStackDto { RewardId = rewards[i].id, Amount = rewards[i].amount };
            return new RunSnapshot { Zone = zone, State = GameState.Left, Tier = WheelTier.Silver, Gold = 100, ReviveCost = 25, Rewards = stacks };
        }
    }
}
