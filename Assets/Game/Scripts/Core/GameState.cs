namespace Game.Core
{
    public enum GameState
    {
        /// <summary>Wheel is still; the player may spin or, in a safe zone, leave.</summary>
        Idle,
        /// <summary>A spin result has been drawn and the wheel is animating towards it.</summary>
        Spinning,
        /// <summary>The bomb hit; rewards are gone. The player may revive or give up.</summary>
        Bombed,
        /// <summary>The player walked away with the rewards.</summary>
        Left,
    }
}
