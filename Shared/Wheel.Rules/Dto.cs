using System;

namespace Wheel.Rules
{
    // Wire types. Public fields so Unity's JsonUtility and System.Text.Json both serialize them without attributes.

    [Serializable]
    public sealed class RewardStackDto
    {
        public string RewardId;
        public int Amount;
    }

    /// <summary>Everything the client needs to render the run. The server decides what is allowed; the client only shows it.</summary>
    [Serializable]
    public sealed class RunSnapshot
    {
        public int Zone;
        public GameState State;
        public WheelTier Tier;
        public int Gold;
        public int ReviveCost;
        /// <summary>True while the bomb slice is disabled after a revive on this zone.</summary>
        public bool BombDisabled;
        public bool CanSpin;
        public bool CanLeave;
        public bool CanRevive;
        public RewardStackDto[] Rewards = Array.Empty<RewardStackDto>();
    }

    /// <summary>Result of one spin: the slice the wheel must land on and the run state after it.</summary>
    [Serializable]
    public sealed class SpinOutcome
    {
        public int SliceIndex;
        public string RewardId;
        public int Amount;
        public bool IsBomb;
        public RunSnapshot Snapshot;
    }
}
