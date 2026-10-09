using System;
using System.Collections.Generic;
using Wheel.Rules;

namespace Wheel.Rules.Tests
{
    /// <summary>Returns a scripted sequence of rolls, repeating the last one forever.</summary>
    public sealed class FakeRandom : IRandom
    {
        readonly Queue<float> values;
        readonly float last;

        public FakeRandom(params float[] sequence)
        {
            if (sequence.Length == 0) throw new ArgumentException("Need at least one value", nameof(sequence));
            values = new Queue<float>(sequence);
            last = sequence[^1];
        }

        public float NextFloat() => values.Count > 0 ? values.Dequeue() : last;
    }
}
