namespace Wheel.Rules
{
    public enum GameState
    {
        /// <summary>The player may spin or, in a safe zone, leave.</summary>
        Idle = 0,
        /// <summary>The bomb hit. The rewards stay on the table: revive keeps them, give up wipes them.</summary>
        Bombed = 1,
        /// <summary>The player walked away with the rewards.</summary>
        Left = 2,
    }
}
