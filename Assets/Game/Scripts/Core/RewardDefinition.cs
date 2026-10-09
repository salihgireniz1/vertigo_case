using UnityEngine;

namespace Game.Core
{
    /// <summary>A collectible reward type. Identity is the asset itself.</summary>
    [CreateAssetMenu(menuName = "Game/Reward", fileName = "reward_new")]
    public sealed class RewardDefinition : ScriptableObject
    {
        [SerializeField] string displayName;
        [SerializeField] Sprite icon;

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public Sprite Icon => icon;
    }
}
