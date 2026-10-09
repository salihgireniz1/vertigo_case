using System;
using System.Linq;
using Game.Core;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace Game.Tests
{
    public class GameSessionTests
    {
        static readonly GameRules Rules = new(
            progression: new ProgressionRules(safeZoneInterval: 5, superZoneInterval: 30, growthPerZone: 0.25f),
            startingGold: 100,
            reviveCost: 25);

        // Rolls that land on each slice of a three-slice equal-weight wheel.
        const float RollCash = 0.1f;
        const float RollBomb = 0.5f;
        const float RollGold = 0.9f;

        RewardDefinition cash;
        RewardDefinition gold;
        WheelSliceData[] bronzeWheel;

        [SetUp]
        public void SetUp()
        {
            cash = ScriptableObject.CreateInstance<RewardDefinition>();
            cash.name = "cash";
            gold = ScriptableObject.CreateInstance<RewardDefinition>();
            gold.name = "gold";
            bronzeWheel = new[]
            {
                new WheelSliceData(cash, baseAmount: 100, weight: 1, isBomb: false),
                new WheelSliceData(null, baseAmount: 0, weight: 1, isBomb: true),
                new WheelSliceData(gold, baseAmount: 10, weight: 1, isBomb: false),
            };
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(cash);
            UnityEngine.Object.DestroyImmediate(gold);
        }

        static GameSession NewSession(params float[] rolls) =>
            new(Rules, new FakeRandom(rolls.Length == 0 ? new[] { RollCash } : rolls));

        void SpinOnce(GameSession session) => session.ApplySpinResult(session.StartSpin(bronzeWheel));

        [Test]
        public void NewSession_StartsAtZoneOneIdleWithStartingGold()
        {
            var session = NewSession();
            Assert.AreEqual(1, session.Zone.CurrentValue);
            Assert.AreEqual(GameState.Idle, session.State.CurrentValue);
            Assert.AreEqual(100, session.Gold.CurrentValue);
            Assert.IsEmpty(session.Rewards.Stacks);
        }

        [Test]
        public void StartSpin_PicksSliceAndEntersSpinning()
        {
            var session = NewSession(RollGold);
            var result = session.StartSpin(bronzeWheel);
            Assert.AreEqual(2, result.SliceIndex);
            Assert.AreSame(gold, result.Slice.Reward);
            Assert.IsFalse(result.IsBomb);
            Assert.AreEqual(GameState.Spinning, session.State.CurrentValue);
        }

        [Test]
        public void StartSpin_AmountScalesWithZone()
        {
            var session = NewSession(RollCash);
            SpinOnce(session);                              // zone 1 -> 2
            var result = session.StartSpin(bronzeWheel);    // zone 2: 100 * 1.25
            Assert.AreEqual(125, result.Amount);
        }

        [Test]
        public void StartSpin_WhenNotIdle_Throws()
        {
            var session = NewSession(RollCash);
            session.StartSpin(bronzeWheel);
            Assert.Throws<InvalidOperationException>(() => session.StartSpin(bronzeWheel));
        }

        [Test]
        public void ApplySpinResult_Reward_AddsToInventoryAndAdvancesZone()
        {
            var session = NewSession(RollCash);
            SpinOnce(session);
            Assert.AreEqual(2, session.Zone.CurrentValue);
            Assert.AreEqual(GameState.Idle, session.State.CurrentValue);
            var stack = session.Rewards.Stacks.Single();
            Assert.AreSame(cash, stack.Reward);
            Assert.AreEqual(100, stack.Amount);
        }

        [Test]
        public void ApplySpinResult_SameRewardTwice_StacksAmount()
        {
            var session = NewSession(RollCash);
            SpinOnce(session); // 100
            SpinOnce(session); // 125
            Assert.AreEqual(1, session.Rewards.Stacks.Count);
            Assert.AreEqual(225, session.Rewards.Stacks[0].Amount);
        }

        [Test]
        public void ApplySpinResult_Bomb_ClearsRewardsAndEntersBombed()
        {
            var session = NewSession(RollCash, RollBomb);
            SpinOnce(session);
            SpinOnce(session);
            Assert.AreEqual(GameState.Bombed, session.State.CurrentValue);
            Assert.IsEmpty(session.Rewards.Stacks);
            Assert.AreEqual(2, session.Zone.CurrentValue, "bomb does not advance the zone");
        }

        [Test]
        public void ApplySpinResult_WhenNotSpinning_Throws()
        {
            var session = NewSession(RollCash);
            var result = session.StartSpin(bronzeWheel);
            session.ApplySpinResult(result);
            Assert.Throws<InvalidOperationException>(() => session.ApplySpinResult(result));
        }

        [Test]
        public void TryLeave_InNormalZone_IsRefused()
        {
            var session = NewSession();
            Assert.IsFalse(session.CanLeave);
            Assert.IsFalse(session.TryLeave());
            Assert.AreEqual(GameState.Idle, session.State.CurrentValue);
        }

        [Test]
        public void TryLeave_InSafeZoneWhileIdle_EntersLeft()
        {
            var session = NewSession(RollCash);
            for (var i = 0; i < 4; i++) SpinOnce(session); // zone 5
            Assert.IsTrue(session.CanLeave);
            Assert.IsTrue(session.TryLeave());
            Assert.AreEqual(GameState.Left, session.State.CurrentValue);
        }

        [Test]
        public void TryLeave_WhileSpinning_IsRefused()
        {
            var session = NewSession(RollCash);
            for (var i = 0; i < 4; i++) SpinOnce(session); // zone 5
            session.StartSpin(bronzeWheel);
            Assert.IsFalse(session.TryLeave());
        }

        [Test]
        public void TryRevive_WhenBombedWithGold_DeductsAndContinues()
        {
            var session = NewSession(RollCash, RollBomb);
            SpinOnce(session);
            SpinOnce(session); // bombed at zone 2
            Assert.IsTrue(session.CanRevive);
            Assert.IsTrue(session.TryRevive());
            Assert.AreEqual(75, session.Gold.CurrentValue);
            Assert.AreEqual(3, session.Zone.CurrentValue);
            Assert.AreEqual(GameState.Idle, session.State.CurrentValue);
        }

        [Test]
        public void TryRevive_WithoutEnoughGold_IsRefused()
        {
            var poorRules = new GameRules(Rules.Progression, startingGold: 10, reviveCost: 25);
            var session = new GameSession(poorRules, new FakeRandom(RollBomb));
            SpinOnce(session);
            Assert.IsFalse(session.CanRevive);
            Assert.IsFalse(session.TryRevive());
            Assert.AreEqual(GameState.Bombed, session.State.CurrentValue);
        }

        [Test]
        public void TryRevive_WhenNotBombed_IsRefused()
        {
            Assert.IsFalse(NewSession().TryRevive());
        }

        [Test]
        public void TryRestart_AfterBomb_ResetsZoneRewardsAndState()
        {
            var session = NewSession(RollCash, RollBomb);
            SpinOnce(session);
            SpinOnce(session);
            Assert.IsTrue(session.TryRestart());
            Assert.AreEqual(1, session.Zone.CurrentValue);
            Assert.AreEqual(GameState.Idle, session.State.CurrentValue);
            Assert.IsEmpty(session.Rewards.Stacks);
        }

        [Test]
        public void TryRestart_AfterLeave_IsAllowed()
        {
            var session = NewSession(RollCash);
            for (var i = 0; i < 4; i++) SpinOnce(session); // zone 5
            session.TryLeave();
            Assert.IsTrue(session.TryRestart());
            Assert.AreEqual(GameState.Idle, session.State.CurrentValue);
        }

        [Test]
        public void TryRestart_WhileRunIsAlive_IsRefused()
        {
            var session = NewSession(RollCash, RollBomb);
            SpinOnce(session);
            Assert.IsFalse(session.TryRestart(), "idle run must not be wiped");
            SpinOnce(session);          // bombed
            session.TryRevive();        // back to idle at zone 3
            Assert.IsFalse(session.TryRestart(), "a revived run must not be wiped by a late Give Up");
            Assert.AreEqual(3, session.Zone.CurrentValue);
        }

        [Test]
        public void CurrentTier_TracksZone()
        {
            var session = NewSession(RollCash);
            Assert.AreEqual(WheelTier.Bronze, session.CurrentTier);
            for (var i = 0; i < 4; i++) SpinOnce(session);
            Assert.AreEqual(WheelTier.Silver, session.CurrentTier);
        }

        [Test]
        public void Rewards_Changed_EmitsUpdatedStack()
        {
            var session = NewSession(RollCash);
            RewardStack observed = null;
            using var subscription = session.Rewards.Changed.Subscribe(stack => observed = stack);
            SpinOnce(session);
            Assert.IsNotNull(observed);
            Assert.AreSame(cash, observed.Reward);
            Assert.AreEqual(100, observed.Amount);
        }
    }
}
