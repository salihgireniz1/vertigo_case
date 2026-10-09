using System;

namespace Game.Core
{
    /// <summary>Source of rolls in [0, 1). Abstracted so tests can script outcomes.</summary>
    public interface IRandom
    {
        float NextFloat();
    }

    public sealed class SystemRandom : IRandom
    {
        readonly Random random = new();

        public float NextFloat() => (float)random.NextDouble();
    }
}
