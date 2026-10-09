using System.Collections.Generic;
using UnityEngine;
using Wheel.Rules;

namespace Game.Data
{
    /// <summary>Editor-authored content of one wheel: sprites, copy and its eight slices.</summary>
    [CreateAssetMenu(menuName = "Game/Wheel Tier", fileName = "wheel_new")]
    public sealed class WheelTierConfig : ScriptableObject
    {
        [SerializeField] WheelTier tier;
        [SerializeField] string title = "BRONZE SPIN";
        [SerializeField] string subtitle = "One of the chambers is a bomb";
        [SerializeField] Sprite baseSprite;
        [SerializeField] Sprite indicatorSprite;
        [SerializeField] WheelSliceData[] slices = new WheelSliceData[WheelDefinition.SliceCount];

        public WheelTier Tier => tier;
        public string Title => title;
        public string Subtitle => subtitle;
        public Sprite BaseSprite => baseSprite;
        public Sprite IndicatorSprite => indicatorSprite;
        public IReadOnlyList<WheelSliceData> Slices => slices;

        /// <summary>The rules-side view of this wheel; what the server would hold in its config.</summary>
        public WheelDefinition ToDefinition()
        {
            var definitions = new SliceDefinition[slices.Length];
            for (var i = 0; i < slices.Length; i++) definitions[i] = slices[i]?.ToDefinition();
            return new WheelDefinition(tier, definitions);
        }

        void OnValidate()
        {
            var problem = ToDefinition().Validate();
            if (problem != null) Debug.LogError($"{name}: {problem}", this);
        }
    }
}
