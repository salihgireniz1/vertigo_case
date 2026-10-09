using System;
using System.Collections.Generic;
using R3;

namespace Game.Core
{
    /// <summary>
    /// The run state machine. Owns zone, state, gold and the reward inventory, and guards every transition.
    /// Idle -> Spinning -> Idle (reward) | Bombed (bomb). Idle -> Left (safe zone only). Bombed -> Idle (revive).
    /// </summary>
    public sealed class GameSession
    {
        readonly GameRules rules;
        readonly IRandom random;
        readonly ReactiveProperty<int> zone = new(1);
        readonly ReactiveProperty<GameState> state = new(GameState.Idle);
        readonly ReactiveProperty<int> gold;

        public GameSession(GameRules rules, IRandom random)
        {
            this.rules = rules;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            gold = new ReactiveProperty<int>(rules.StartingGold);
        }

        public GameRules Rules => rules;
        public ReadOnlyReactiveProperty<int> Zone => zone;
        public ReadOnlyReactiveProperty<GameState> State => state;
        public ReadOnlyReactiveProperty<int> Gold => gold;
        public RewardInventory Rewards { get; } = new();

        public WheelTier CurrentTier => ZoneRules.GetTier(zone.Value, rules.Progression);
        public bool CanLeave => state.Value == GameState.Idle && ZoneRules.IsSafe(zone.Value, rules.Progression);
        public bool CanRevive => state.Value == GameState.Bombed && gold.Value >= rules.ReviveCost;

        /// <summary>Draws the outcome for the current zone and enters Spinning.</summary>
        public SpinResult StartSpin(IReadOnlyList<WheelSliceData> slices)
        {
            Require(GameState.Idle);
            var index = SpinResolver.Pick(slices, random);
            var slice = slices[index];
            var amount = slice.IsBomb ? 0 : ZoneRules.ScaleAmount(slice.BaseAmount, zone.Value, rules.Progression);
            state.Value = GameState.Spinning;
            return new SpinResult(index, slice, amount);
        }

        /// <summary>Banks the reward and advances, or wipes everything on a bomb.</summary>
        public void ApplySpinResult(SpinResult result)
        {
            Require(GameState.Spinning);
            if (result.IsBomb)
            {
                Rewards.Clear();
                state.Value = GameState.Bombed;
                return;
            }
            Rewards.Add(result.Slice.Reward, result.Amount);
            zone.Value++;
            state.Value = GameState.Idle;
        }

        public bool TryLeave()
        {
            if (!CanLeave) return false;
            state.Value = GameState.Left;
            return true;
        }

        public bool TryRevive()
        {
            if (!CanRevive) return false;
            gold.Value -= rules.ReviveCost;
            zone.Value++;
            state.Value = GameState.Idle;
            return true;
        }

        public void Restart()
        {
            Rewards.Clear();
            zone.Value = 1;
            state.Value = GameState.Idle;
        }

        void Require(GameState expected)
        {
            if (state.Value != expected)
                throw new InvalidOperationException($"Expected state {expected} but session is {state.Value}");
        }
    }
}
