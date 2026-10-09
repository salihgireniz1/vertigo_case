namespace Wheel.Rules
{
    public enum GameState
    {
        /// <summary>The player may spin or, in a safe zone, leave.</summary>
        Idle = 0,
        /// <summary>The bomb hit; rewards are gone. The player may revive or give up.</summary>
        Bombed = 1,
        /// <summary>The player walked away with the rewards.</summary>
        Left = 2,
    }
}
