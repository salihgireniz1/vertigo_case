using System;

namespace Wheel.Rules
{
    /// <summary>Source of rolls in [0, 1). The server owns the real one; tests script it.</summary>
    public interface IRandom
    {
        float NextFloat();
    }

    public sealed class SystemRandom : IRandom
    {
        readonly Random random;

        public SystemRandom() => random = new Random();
        public SystemRandom(int seed) => random = new Random(seed);

        public float NextFloat() => (float)random.NextDouble();
    }
}
