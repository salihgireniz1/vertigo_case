using System;

namespace Wheel.Rules
{
    /// <summary>
    /// One player's run. Owns zone, state, gold and the inventory, and guards every transition.
    /// Idle -> Idle (reward) | Bombed (bomb). Idle -> Left (safe zone only). Bombed -> Idle (revive, same zone, bomb disabled).
    /// Bombed | Left -> Idle (restart). Runs unchanged on the server and in the client's local mode.
    /// </summary>
    public sealed class GameSession
    {
        readonly GameRules rules;
        readonly IRandom random;
        int zone = 1;
        GameState state = GameState.Idle;
        int gold;
        int bombDisabledZone = -1;

        public GameSession(GameRules rules, IRandom random)
        {
            this.rules = rules;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            gold = rules.StartingGold;
        }

        public event Action Changed;

        public GameRules Rules => rules;
        public int Zone => zone;
        public GameState State => state;
        public int Gold => gold;
        public RewardInventory Rewards { get; } = new RewardInventory();

        public WheelTier CurrentTier => ZoneRules.GetTier(zone, rules.Progression);
        /// <summary>After a revive the same zone is replayed without the bomb.</summary>
        public bool BombDisabled => bombDisabledZone == zone;
        public bool CanSpin => state == GameState.Idle;
        public bool CanLeave => state == GameState.Idle && ZoneRules.IsSafe(zone, rules.Progression);
        public bool CanRevive => state == GameState.Bombed && gold >= rules.ReviveCost;
        public bool CanRestart => state == GameState.Bombed || state == GameState.Left;

        /// <summary>Resolves one spin on the given wheel and applies it. Atomic, as the server does it.</summary>
        public SpinOutcome Spin(WheelDefinition wheel)
        {
            if (!CanSpin) throw new InvalidOperationException($"Cannot spin while {state}");
            if (wheel.Tier != CurrentTier) throw new ArgumentException($"Zone {zone} needs a {CurrentTier} wheel, got {wheel.Tier}", nameof(wheel));

            var index = SpinResolver.Pick(wheel.Slices, random, excludeBombs: BombDisabled);
            var slice = wheel.Slices[index];
            var amount = 0;
            if (slice.IsBomb)
            {
                Rewards.Clear();
                state = GameState.Bombed;
            }
            else
            {
                amount = ZoneRules.ScaleAmount(slice.BaseAmount, zone, rules.Progression);
                Rewards.Add(slice.RewardId, amount);
                zone++;
            }
            Notify();
            return new SpinOutcome { SliceIndex = index, RewardId = slice.RewardId, Amount = amount, IsBomb = slice.IsBomb, Snapshot = Snapshot() };
        }

        /// <summary>Banks the run: the wallet reward collected on the wheel is credited to the wallet.</summary>
        public bool TryLeave()
        {
            if (!CanLeave) return false;
            if (rules.WalletRewardId != null) gold += Rewards.AmountOf(rules.WalletRewardId);
            state = GameState.Left;
            Notify();
            return true;
        }

        /// <summary>Pays the revive cost and replays the current zone with the bomb slice disabled.</summary>
        public bool TryRevive()
        {
            if (!CanRevive) return false;
            gold -= rules.ReviveCost;
            bombDisabledZone = zone;
            state = GameState.Idle;
            Notify();
            return true;
        }

        public bool TryRestart()
        {
            if (!CanRestart) return false;
            Rewards.Clear();
            zone = 1;
            bombDisabledZone = -1;
            state = GameState.Idle;
            Notify();
            return true;
        }

        public RunSnapshot Snapshot() => new RunSnapshot
        {
            Zone = zone,
            State = state,
            Tier = CurrentTier,
            Gold = gold,
            ReviveCost = rules.ReviveCost,
            BombDisabled = BombDisabled,
            CanSpin = CanSpin,
            CanLeave = CanLeave,
            CanRevive = CanRevive,
            Rewards = Rewards.ToDto(),
        };

        void Notify() => Changed?.Invoke();
    }
}
