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

        void OnValidate()
        {
            if (slices.Length != SliceCount)
                Debug.LogWarning($"{name}: wheel must have exactly {SliceCount} slices, has {slices.Length}", this);

            var bombs = 0;
            foreach (var slice in slices)
                if (slice != null && slice.IsBomb) bombs++;

            if (tier == WheelTier.Bronze && bombs != 1)
                Debug.LogWarning($"{name}: bronze wheel must have exactly one bomb, has {bombs}", this);
            if (tier != WheelTier.Bronze && bombs > 0)
                Debug.LogWarning($"{name}: {tier} wheel is risk-free and must not contain a bomb", this);
        }
    }
}
