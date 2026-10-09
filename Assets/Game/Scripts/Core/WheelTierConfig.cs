using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Editor-authored content of one wheel: sprites, copy and its eight slices.</summary>
    [CreateAssetMenu(menuName = "Game/Wheel Tier", fileName = "wheel_new")]
    public sealed class WheelTierConfig : ScriptableObject
    {
        public const int SliceCount = 8;

        [SerializeField] WheelTier tier;
        [SerializeField] string title = "BRONZE SPIN";
        [SerializeField] string subtitle = "Watch out for the bomb";
        [SerializeField] Sprite baseSprite;
        [SerializeField] Sprite indicatorSprite;
        [SerializeField] WheelSliceData[] slices = new WheelSliceData[SliceCount];

        public WheelTier Tier => tier;
        public string Title => title;
        public string Subtitle => subtitle;
        public Sprite BaseSprite => baseSprite;
        public Sprite IndicatorSprite => indicatorSprite;
        public IReadOnlyList<WheelSliceData> Slices => slices;

        /// <summary>Returns null when the wheel is playable, otherwise the first problem found.</summary>
        public string Validate()
        {
            if (slices.Length != SliceCount) return $"must have exactly {SliceCount} slices, has {slices.Length}";

            var bombs = 0;
            for (var i = 0; i < slices.Length; i++)
            {
                var slice = slices[i];
                if (slice == null) return $"slice {i} is empty";
                if (slice.IsBomb) bombs++;
                if (slice.Reward == null) return $"slice {i} has no reward (bomb slices need one for the icon)";
                if (!slice.IsBomb && slice.Weight <= 0f) return $"slice {i} can never be hit (weight 0)";
            }

            if (tier == WheelTier.Bronze && bombs != 1) return $"bronze wheel must have exactly one bomb, has {bombs}";
            if (tier != WheelTier.Bronze && bombs > 0) return $"{tier} wheel is risk-free and must not contain a bomb";
            return null;
        }

        void OnValidate()
        {
            var problem = Validate();
            if (problem != null) Debug.LogError($"{name}: {problem}", this);
        }
    }
}
