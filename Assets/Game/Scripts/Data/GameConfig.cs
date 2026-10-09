using UnityEngine;
using Wheel.Rules;

namespace Game.Data
{
    /// <summary>Root tuning asset handed to the composition root.</summary>
    [CreateAssetMenu(menuName = "Game/Game Config", fileName = "game_config")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField] ZoneProgressionConfig progression;
        [SerializeField, Min(0)] int startingGold = 100;
        [SerializeField, Min(0)] int reviveCost = 25;
        [SerializeField, Tooltip("Reward credited to the wallet when the player leaves with it")]
        RewardDefinition walletReward;

        public ZoneProgressionConfig Progression => progression;
        public RewardDefinition WalletReward => walletReward;
        public GameRules Rules => new(progression.Rules, startingGold, reviveCost, walletReward != null ? walletReward.Id : null);
    }
}
