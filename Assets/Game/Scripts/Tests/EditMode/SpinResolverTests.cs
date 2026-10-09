using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests
{
    public class SpinResolverTests
    {
        static WheelSliceData Slice(float weight, bool isBomb = false) =>
            new(reward: null, baseAmount: 10, weight: weight, isBomb: isBomb);

        [Test]
        public void Pick_EqualWeights_LowRollGivesFirstSlice()
        {
            var slices = new[] { Slice(1), Slice(1), Slice(1), Slice(1) };
            Assert.AreEqual(0, SpinResolver.Pick(slices, new FakeRandom(0f)));
        }

        [Test]
        public void Pick_EqualWeights_HighRollGivesLastSlice()
        {
            var slices = new[] { Slice(1), Slice(1), Slice(1), Slice(1) };
            Assert.AreEqual(3, SpinResolver.Pick(slices, new FakeRandom(0.999f)));
        }

        [Test]
        public void Pick_EqualWeights_MiddleRollGivesMiddleSlice()
        {
            var slices = new[] { Slice(1), Slice(1), Slice(1), Slice(1) };
            Assert.AreEqual(2, SpinResolver.Pick(slices, new FakeRandom(0.6f)));
        }

        [Test]
        public void Pick_ZeroWeightSliceIsNeverChosen()
        {
            var slices = new[] { Slice(0), Slice(5), Slice(0) };
            Assert.AreEqual(1, SpinResolver.Pick(slices, new FakeRandom(0f)));
            Assert.AreEqual(1, SpinResolver.Pick(slices, new FakeRandom(0.999f)));
        }

        [Test]
        public void Pick_HeavierSliceWinsProportionally()
        {
            var slices = new[] { Slice(1), Slice(3) };
            Assert.AreEqual(0, SpinResolver.Pick(slices, new FakeRandom(0.2f)));
            Assert.AreEqual(1, SpinResolver.Pick(slices, new FakeRandom(0.3f)));
        }

        [Test]
        public void Pick_NoPositiveWeight_Throws()
        {
            var slices = new[] { Slice(0), Slice(0) };
            Assert.Throws<ArgumentException>(() => SpinResolver.Pick(slices, new FakeRandom(0.5f)));
        }
    }
}
