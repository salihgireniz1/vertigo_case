using System;
using UnityEngine;
using Wheel.Rules;

namespace Game.Data
{
    /// <summary>Inspector row for one slice. Converts to the rules' SliceDefinition.</summary>
    [Serializable]
    public sealed class WheelSliceData
    {
        [SerializeField] RewardDefinition reward;
        [SerializeField, Min(0)] int baseAmount = 1;
        [SerializeField, Min(0f)] float weight = 1f;
        [SerializeField] bool isBomb;

        public RewardDefinition Reward => reward;
        public int BaseAmount => baseAmount;
        public float Weight => weight;
        public bool IsBomb => isBomb;

        public SliceDefinition ToDefinition() => new(reward != null ? reward.Id : null, baseAmount, weight, isBomb);
    }
}
