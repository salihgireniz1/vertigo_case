using UnityEngine;

namespace Game.Data
{
    /// <summary>Client-side presentation of a reward. The id is what the rules and the server talk about.</summary>
    [CreateAssetMenu(menuName = "Game/Reward", fileName = "reward_new")]
    public sealed class RewardDefinition : ScriptableObject
    {
        [SerializeField] string displayName;
        [SerializeField] Sprite icon;

        /// <summary>Stable id shared with the server: the asset name, e.g. "reward_gold".</summary>
        public string Id => name;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Icon => icon;
    }
}
