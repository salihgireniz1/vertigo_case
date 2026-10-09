using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>One slice of a wheel: either a reward with a base amount, or the bomb.</summary>
    [Serializable]
    public sealed class WheelSliceData
    {
        [SerializeField] RewardDefinition reward;
        [SerializeField, Min(0)] int baseAmount = 1;
        [SerializeField, Min(0f)] float weight = 1f;
        [SerializeField] bool isBomb;

        public WheelSliceData() { }

        public WheelSliceData(RewardDefinition reward, int baseAmount, float weight, bool isBomb)
        {
            this.reward = reward;
            this.baseAmount = baseAmount;
            this.weight = weight;
            this.isBomb = isBomb;
        }

        public RewardDefinition Reward => reward;
        public int BaseAmount => baseAmount;
        public float Weight => weight;
        public bool IsBomb => isBomb;
    }
}
