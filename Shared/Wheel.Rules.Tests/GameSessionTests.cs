using System;
using System.Linq;
using NUnit.Framework;
using Wheel.Rules;

namespace Wheel.Rules.Tests
{
    public class GameSessionTests
    {
        static readonly GameRules Rules = new(
            progression: new ProgressionRules(safeZoneInterval: 5, superZoneInterval: 30, growthPercentPerZone: 25),
            startingGold: 100,
            reviveCost: 25,
            walletRewardId: "gold");

        // Eight equal slices: roll (i + 0.5) / 8 lands on slice i.
        const int CashSlice = 0;
        const int BombSlice = 2;
        const int GoldSlice = 5;

        static float Roll(int slice) => (slice + 0.5f) / 8f;

        static WheelDefinition Wheel(WheelTier tier, bool withBomb) => new(tier, new[]
        {
            new SliceDefinition("cash", 100, 1f, false),
            new SliceDefinition("chest", 1, 1f, false),
            new SliceDefinition("bomb", 0, 1f, withBomb),
            new SliceDefinition("grenade", 2, 1f, false),
            new SliceDefinition("points", 10, 1f, false),
            new SliceDefinition("gold", 10, 1f, false),
            new SliceDefinition("molotov", 1, 1f, false),
            new SliceDefinition("knife", 10, 1f, false),
        });

        static readonly WheelDefinition Bronze = Wheel(WheelTier.Bronze, withBomb: true);
        static readonly WheelDefinition Silver = Wheel(WheelTier.Silver, withBomb: false);

        static GameSession NewSession(params int[] slices) =>
            new(Rules, new FakeRandom((slices.Length == 0 ? new[] { CashSlice } : slices).Select(Roll).ToArray()));

        static WheelDefinition WheelFor(GameSession session) => session.CurrentTier == WheelTier.Bronze ? Bronze : Silver;

        [Test]
        public void NewSession_StartsAtZoneOneIdleWithStartingGold()
        {
            var session = NewSession();
            Assert.That(session.Zone, Is.EqualTo(1));
            Assert.That(session.State, Is.EqualTo(GameState.Idle));
            Assert.That(session.Gold, Is.EqualTo(100));
            Assert.That(session.Rewards.Stacks, Is.Empty);
            Assert.That(session.CanSpin, Is.True);
            Assert.That(session.CanLeave, Is.False);
        }

        [Test]
        public void Spin_Reward_AddsToInventoryAndAdvancesZone()
        {
            var session = NewSession(GoldSlice);
            var outcome = session.Spin(Bronze);
            Assert.That(outcome.SliceIndex, Is.EqualTo(GoldSlice));
            Assert.That(outcome.RewardId, Is.EqualTo("gold"));
            Assert.That(outcome.Amount, Is.EqualTo(10));
            Assert.That(outcome.IsBomb, Is.False);
            Assert.That(session.Zone, Is.EqualTo(2));
            Assert.That(session.State, Is.EqualTo(GameState.Idle));
            Assert.That(session.Rewards.Stacks.Single().Amount, Is.EqualTo(10));
        }

        [Test]
        public void Spin_AmountScalesWithZone()
        {
            var session = NewSession(CashSlice);
            session.Spin(Bronze);                 // zone 1 -> 2
            var outcome = session.Spin(Bronze);   // zone 2: 100 * 1.25
            Assert.That(outcome.Amount, Is.EqualTo(125));
            Assert.That(session.Rewards.Stacks.Single().Amount, Is.EqualTo(225), "same reward stacks");
        }

        [Test]
        public void Spin_Bomb_ClearsRewardsAndEntersBombed()
        {
            var session = NewSession(CashSlice, BombSlice);
            session.Spin(Bronze);
            var outcome = session.Spin(Bronze);
            Assert.That(outcome.IsBomb, Is.True);
            Assert.That(outcome.Amount, Is.EqualTo(0));
            Assert.That(session.State, Is.EqualTo(GameState.Bombed));
            Assert.That(session.Rewards.Stacks, Is.Empty);
            Assert.That(session.Zone, Is.EqualTo(2), "bomb does not advance the zone");
            Assert.That(session.CanSpin, Is.False);
        }

        [Test]
        public void Spin_WhenNotIdle_Throws()
        {
            var session = NewSession(BombSlice);
            session.Spin(Bronze);
            Assert.Throws<InvalidOperationException>(() => session.Spin(Bronze));
        }

        [Test]
        public void Spin_WithWrongTierWheel_Throws()
        {
            var session = NewSession();
            Assert.Throws<ArgumentException>(() => session.Spin(Silver));
        }

        [Test]
        public void Spin_SnapshotMirrorsSession()
        {
            var session = NewSession(CashSlice);
            var snapshot = session.Spin(Bronze).Snapshot;
            Assert.That(snapshot.Zone, Is.EqualTo(2));
            Assert.That(snapshot.State, Is.EqualTo(GameState.Idle));
            Assert.That(snapshot.Tier, Is.EqualTo(WheelTier.Bronze));
            Assert.That(snapshot.Gold, Is.EqualTo(100));
            Assert.That(snapshot.ReviveCost, Is.EqualTo(25));
            Assert.That(snapshot.CanSpin, Is.True);
            Assert.That(snapshot.CanLeave, Is.False);
            Assert.That(snapshot.Rewards.Single().RewardId, Is.EqualTo("cash"));
            Assert.That(snapshot.Rewards.Single().Amount, Is.EqualTo(100));
        }

        [Test]
        public void TryLeave_InNormalZone_IsRefused()
        {
            var session = NewSession();
            Assert.That(session.TryLeave(), Is.False);
            Assert.That(session.State, Is.EqualTo(GameState.Idle));
        }

        [Test]
        public void TryLeave_InSafeZoneWhileIdle_EntersLeft()
        {
            var session = NewSession(CashSlice);
            for (var i = 0; i < 4; i++) session.Spin(WheelFor(session)); // zone 5
            Assert.That(session.CurrentTier, Is.EqualTo(WheelTier.Silver));
            Assert.That(session.CanLeave, Is.True);
            Assert.That(session.TryLeave(), Is.True);
            Assert.That(session.State, Is.EqualTo(GameState.Left));
            Assert.That(session.CanSpin, Is.False);
        }

        [Test]
        public void TryLeave_CreditsCollectedGoldToWallet()
        {
            var session = NewSession(GoldSlice);
            for (var i = 0; i < 4; i++) session.Spin(WheelFor(session)); // 10 + 13 + 15 + 18 gold, zone 5
            Assert.That(session.Rewards.AmountOf("gold"), Is.EqualTo(56));
            Assert.That(session.TryLeave(), Is.True);
            Assert.That(session.Gold, Is.EqualTo(156));
        }

        [Test]
        public void TryLeave_WithoutWalletReward_LeavesGoldUntouched()
        {
            var noWallet = new GameRules(Rules.Progression, startingGold: 100, reviveCost: 25);
            var session = new GameSession(noWallet, new FakeRandom(Roll(GoldSlice)));
            for (var i = 0; i < 4; i++) session.Spin(WheelFor(session));
            session.TryLeave();
            Assert.That(session.Gold, Is.EqualTo(100));
        }

        [Test]
        public void TryRevive_ReplaysSameZoneWithoutBomb()
        {
            var session = NewSession(CashSlice, BombSlice, BombSlice, GoldSlice);
            session.Spin(Bronze);           // zone 2
            session.Spin(Bronze);           // bomb at zone 2
            Assert.That(session.CanRevive, Is.True);
            Assert.That(session.TryRevive(), Is.True);
            Assert.That(session.Gold, Is.EqualTo(75));
            Assert.That(session.Zone, Is.EqualTo(2), "revive replays the bombed zone");
            Assert.That(session.State, Is.EqualTo(GameState.Idle));
            Assert.That(session.BombDisabled, Is.True);

            // The roll that would hit the bomb now lands elsewhere; the wheel still has eight slices.
            var outcome = session.Spin(Bronze);
            Assert.That(outcome.IsBomb, Is.False);
            Assert.That(session.Zone, Is.EqualTo(3));
            Assert.That(session.BombDisabled, Is.False, "bomb is back on the next zone");
        }

        [Test]
        public void TryRevive_WithoutEnoughGold_IsRefused()
        {
            var poor = new GameSession(new GameRules(Rules.Progression, startingGold: 10, reviveCost: 25), new FakeRandom(Roll(BombSlice)));
            poor.Spin(Bronze);
            Assert.That(poor.CanRevive, Is.False);
            Assert.That(poor.TryRevive(), Is.False);
            Assert.That(poor.State, Is.EqualTo(GameState.Bombed));
        }

        [Test]
        public void TryRevive_WhenNotBombed_IsRefused()
        {
            Assert.That(NewSession().TryRevive(), Is.False);
        }

        [Test]
        public void TryRestart_AfterBomb_ResetsEverything()
        {
            var session = NewSession(CashSlice, BombSlice);
            session.Spin(Bronze);
            session.Spin(Bronze);
            Assert.That(session.TryRestart(), Is.True);
            Assert.That(session.Zone, Is.EqualTo(1));
            Assert.That(session.State, Is.EqualTo(GameState.Idle));
            Assert.That(session.Rewards.Stacks, Is.Empty);
            Assert.That(session.BombDisabled, Is.False);
        }

        [Test]
        public void TryRestart_AfterLeave_IsAllowed()
        {
            var session = NewSession(CashSlice);
            for (var i = 0; i < 4; i++) session.Spin(WheelFor(session));
            session.TryLeave();
            Assert.That(session.TryRestart(), Is.True);
        }

        [Test]
        public void TryRestart_WhileRunIsAlive_IsRefused()
        {
            var session = NewSession(CashSlice, BombSlice);
            session.Spin(Bronze);
            Assert.That(session.TryRestart(), Is.False, "idle run must not be wiped");
            session.Spin(Bronze);     // bombed at zone 2
            session.TryRevive();      // back to idle, same zone
            Assert.That(session.TryRestart(), Is.False, "a revived run must not be wiped by a late Give Up");
            Assert.That(session.Zone, Is.EqualTo(2));
        }

        [Test]
        public void Changed_FiresOnEveryTransition()
        {
            var session = NewSession(CashSlice, BombSlice);
            var count = 0;
            session.Changed += () => count++;
            session.Spin(Bronze);
            session.Spin(Bronze);
            session.TryRevive();
            Assert.That(count, Is.EqualTo(3));
        }

        [Test]
        public void WheelDefinition_Validate_CatchesBrokenContent()
        {
            Assert.That(Bronze.Validate(), Is.Null);
            Assert.That(Silver.Validate(), Is.Null);
            Assert.That(Wheel(WheelTier.Silver, withBomb: true).Validate(), Does.Contain("risk-free"));
            Assert.That(Wheel(WheelTier.Bronze, withBomb: false).Validate(), Does.Contain("exactly one bomb"));
            Assert.That(new WheelDefinition(WheelTier.Bronze, new SliceDefinition[3]).Validate(), Does.Contain("exactly 8"));
        }
    }
}
