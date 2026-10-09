using UnityEngine;

namespace Game.Core
{
    /// <summary>Root tuning asset handed to the composition root.</summary>
    [CreateAssetMenu(menuName = "Game/Game Config", fileName = "game_config")]
    public sealed class GameConfig : ScriptableObject
    {
        [SerializeField] ZoneProgressionConfig progression;
        [SerializeField, Min(0)] int startingGold = 100;
        [SerializeField, Min(0)] int reviveCost = 25;

        public ZoneProgressionConfig Progression => progression;
        public GameRules Rules => new(progression.Rules, startingGold, reviveCost);
    }
}
