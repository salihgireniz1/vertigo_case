using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Data;
using Game.Runtime.Flow;
using Game.Runtime.State;
using NUnit.Framework;
using R3;
using UnityEngine;
using Wheel.Rules;

namespace Game.Tests
{
    /// <summary>The flow is driven entirely through fakes: no scene, no tweens, no real authority.</summary>
    public class GameFlowTests
    {
        FakeRunService service;
        RunStore store;
        FakeWheelView wheel;
        FakeHudView hud;
        FakeBombPopup bombPopup;
        FakeLeavePopup leavePopup;
        FakeErrorPopup errorPopup;
        GameFlow flow;
        RewardDefinition cash;

        [SetUp]
        public void SetUp()
        {
            service = new FakeRunService();
            store = new RunStore();
            wheel = new FakeWheelView();
            hud = new FakeHudView();
            bombPopup = new FakeBombPopup();
            leavePopup = new FakeLeavePopup();
            errorPopup = new FakeErrorPopup();

            cash = ScriptableObject.CreateInstance<RewardDefinition>();
            cash.name = "reward_cash";
            var catalog = CatalogWith(cash);

            flow = new GameFlow(service, store, catalog, wheel, hud, bombPopup, leavePopup, errorPopup, resultHoldSeconds: 0f, CancellationToken.None);
            flow.StartAsync().Forget();
        }

        [TearDown]
        public void TearDown()
        {
            flow.Dispose();
            store.Dispose();
            Object.DestroyImmediate(cash);
        }

        static RewardCatalog CatalogWith(RewardDefinition reward)
        {
            // Build a one-entry catalog through the same path production uses: valid wheels with that reward on every slice.
            var progression = ScriptableObject.CreateInstance<ZoneProgressionConfig>();
            var pso = new UnityEditor.SerializedObject(progression);
            pso.FindProperty("bronzeWheel").objectReferenceValue = WheelConfig(WheelTier.Bronze, reward, bombSlice: 2);
            pso.FindProperty("silverWheel").objectReferenceValue = WheelConfig(WheelTier.Silver, reward, bombSlice: -1);
            pso.FindProperty("goldenWheel").objectReferenceValue = WheelConfig(WheelTier.Golden, reward, bombSlice: -1);
            pso.ApplyModifiedPropertiesWithoutUndo();
            return RewardCatalog.From(progression);
        }

        static WheelTierConfig WheelConfig(WheelTier tier, RewardDefinition reward, int bombSlice)
        {
            var wheelConfig = ScriptableObject.CreateInstance<WheelTierConfig>();
            var so = new UnityEditor.SerializedObject(wheelConfig);
            so.FindProperty("tier").enumValueIndex = (int)tier;
            var slices = so.FindProperty("slices");
            slices.arraySize = WheelDefinition.SliceCount;
            for (var i = 0; i < WheelDefinition.SliceCount; i++)
            {
                var slice = slices.GetArrayElementAtIndex(i);
                slice.FindPropertyRelative("reward").objectReferenceValue = reward;
                slice.FindPropertyRelative("isBomb").boolValue = i == bombSlice;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return wheelConfig;
        }

        static SpinOutcome Outcome(int slice, RunSnapshot after, bool bomb = false) =>
            new() { SliceIndex = slice, RewardId = "reward_cash", Amount = bomb ? 0 : 100, IsBomb = bomb, Snapshot = after };

        [Test]
        public void Start_AppliesInitialSnapshot()
        {
            Assert.AreEqual(1, store.Current.Zone);
            Assert.IsTrue(store.Current.CanSpin);
        }

        [Test]
        public void Spin_AnimatesToOutcomeThenAppliesSnapshot()
        {
            service.Outcomes.Enqueue(Outcome(5, Snapshots.Idle(2)));
            wheel.Clicks.OnNext(Unit.Default);

            Assert.AreEqual(1, service.SpinCalls);
            Assert.AreEqual(5, wheel.SpunTo[0]);
            Assert.AreEqual(1, store.Current.Zone, "snapshot waits for the animation");
            Assert.IsTrue(store.Busy.CurrentValue);

            wheel.FinishSpin();

            Assert.AreEqual(2, store.Current.Zone);
            Assert.AreEqual(1, wheel.Wins);
            Assert.IsFalse(store.Busy.CurrentValue);
        }

        [Test]
        public void Spin_WhileSpinning_IsIgnored()
        {
            service.Outcomes.Enqueue(Outcome(5, Snapshots.Idle(2)));
            service.Outcomes.Enqueue(Outcome(6, Snapshots.Idle(3)));
            wheel.Clicks.OnNext(Unit.Default);
            wheel.Clicks.OnNext(Unit.Default);
            Assert.AreEqual(1, service.SpinCalls, "second tap must not reach the authority");
        }

        [Test]
        public void Spin_Bomb_ShowsPopupWithReviveOffer()
        {
            service.Outcomes.Enqueue(Outcome(2, Snapshots.Bombed(3), bomb: true));
            wheel.Clicks.OnNext(Unit.Default);
            wheel.FinishSpin();

            Assert.AreEqual(1, wheel.Bombs);
            Assert.IsTrue(bombPopup.Visible);
            Assert.AreEqual(25, bombPopup.ShownCost);
            Assert.IsTrue(bombPopup.ShownCanRevive);
            Assert.AreEqual(GameState.Bombed, store.Current.State);
        }

        [Test]
        public void Revive_AppliesSnapshotAndHidesPopup()
        {
            service.Outcomes.Enqueue(Outcome(2, Snapshots.Bombed(3), bomb: true));
            wheel.Clicks.OnNext(Unit.Default);
            wheel.FinishSpin();

            service.NextSnapshot = Snapshots.Idle(3, gold: 75);
            bombPopup.Revive.OnNext(Unit.Default);

            Assert.AreEqual(1, service.ReviveCalls);
            Assert.IsFalse(bombPopup.Visible);
            Assert.AreEqual(75, store.Current.Gold);
            Assert.AreEqual(GameState.Idle, store.Current.State);
        }

        [Test]
        public void Revive_WhenSnapshotForbidsIt_NeverReachesAuthority()
        {
            service.Outcomes.Enqueue(Outcome(2, Snapshots.Bombed(3, canRevive: false), bomb: true));
            wheel.Clicks.OnNext(Unit.Default);
            wheel.FinishSpin();

            bombPopup.Revive.OnNext(Unit.Default);
            Assert.AreEqual(0, service.ReviveCalls);
            Assert.IsTrue(bombPopup.Visible);
        }

        [Test]
        public void GiveUp_RestartsOnce_EvenWhenTappedTwice()
        {
            service.Outcomes.Enqueue(Outcome(2, Snapshots.Bombed(3), bomb: true));
            wheel.Clicks.OnNext(Unit.Default);
            wheel.FinishSpin();

            service.NextSnapshot = Snapshots.Idle(1);
            bombPopup.GiveUp.OnNext(Unit.Default);
            bombPopup.GiveUp.OnNext(Unit.Default);

            Assert.AreEqual(1, service.RestartCalls);
            Assert.AreEqual(1, store.Current.Zone);
            Assert.IsFalse(bombPopup.Visible);
        }

        [Test]
        public void Exit_InNormalZone_IsIgnored()
        {
            hud.Clicks.OnNext(Unit.Default);
            Assert.AreEqual(0, service.LeaveCalls);
            Assert.IsFalse(leavePopup.Visible);
        }

        [Test]
        public void Spin_WhenAuthorityUnreachable_ShowsErrorAndResendsOnRetry()
        {
            service.FailSpins = 1;
            service.Outcomes.Enqueue(Outcome(3, Snapshots.Idle(2), bomb: false));

            wheel.Clicks.OnNext(Unit.Default);
            Assert.IsTrue(errorPopup.Visible, "the error popup is up");
            Assert.AreEqual("offline", errorPopup.ShownReason);
            Assert.AreEqual(1, service.SpinCalls);
            Assert.IsTrue(store.Busy.CurrentValue, "the gate stays closed while the error is up");
            Assert.AreEqual(0, wheel.SpunTo.Count, "nothing animates without an answer");

            errorPopup.Retry();
            Assert.IsFalse(errorPopup.Visible);
            Assert.AreEqual(2, service.SpinCalls, "the same intent is sent again");
            wheel.FinishSpin();
            Assert.AreEqual(2, store.Current.Zone, "the retried answer is applied");
            Assert.IsFalse(store.Busy.CurrentValue);
        }

        [Test]
        public void Exit_InSafeZone_ShowsRewardsThenPlayAgainRestarts()
        {
            store.Apply(Snapshots.Idle(5, canLeave: true));
            service.NextSnapshot = Snapshots.Left(5, ("reward_cash", 375));
            hud.Clicks.OnNext(Unit.Default);

            Assert.AreEqual(1, service.LeaveCalls);
            Assert.IsTrue(leavePopup.Visible);
            Assert.AreEqual(5, leavePopup.ShownZone);
            Assert.AreSame(cash, leavePopup.ShownRewards[0].Reward);
            Assert.AreEqual(375, leavePopup.ShownRewards[0].Amount);

            service.NextSnapshot = Snapshots.Idle(1);
            leavePopup.PlayAgain.OnNext(Unit.Default);
            Assert.AreEqual(1, service.RestartCalls);
            Assert.IsFalse(leavePopup.Visible);
            Assert.AreEqual(1, store.Current.Zone);
        }
    }
}
