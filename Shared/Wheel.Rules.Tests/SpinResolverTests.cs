using System;
using NUnit.Framework;
using Wheel.Rules;

namespace Wheel.Rules.Tests
{
    public class SpinResolverTests
    {
        static SliceDefinition Slice(float weight, bool isBomb = false) => new("r", 10, weight, isBomb);

        [Test]
        public void Pick_EqualWeights_LowRollGivesFirstSlice()
        {
            var slices = new[] { Slice(1), Slice(1), Slice(1), Slice(1) };
            Assert.That(SpinResolver.Pick(slices, new FakeRandom(0f)), Is.EqualTo(0));
        }

        [Test]
        public void Pick_EqualWeights_HighRollGivesLastSlice()
        {
            var slices = new[] { Slice(1), Slice(1), Slice(1), Slice(1) };
            Assert.That(SpinResolver.Pick(slices, new FakeRandom(0.999f)), Is.EqualTo(3));
        }

        [Test]
        public void Pick_EqualWeights_MiddleRollGivesMiddleSlice()
        {
            var slices = new[] { Slice(1), Slice(1), Slice(1), Slice(1) };
            Assert.That(SpinResolver.Pick(slices, new FakeRandom(0.6f)), Is.EqualTo(2));
        }

        [Test]
        public void Pick_ZeroWeightSliceIsNeverChosen()
        {
            var slices = new[] { Slice(0), Slice(5), Slice(0) };
            Assert.That(SpinResolver.Pick(slices, new FakeRandom(0f)), Is.EqualTo(1));
            Assert.That(SpinResolver.Pick(slices, new FakeRandom(0.999f)), Is.EqualTo(1));
        }

        [Test]
        public void Pick_HeavierSliceWinsProportionally()
        {
            var slices = new[] { Slice(1), Slice(3) };
            Assert.That(SpinResolver.Pick(slices, new FakeRandom(0.2f)), Is.EqualTo(0));
            Assert.That(SpinResolver.Pick(slices, new FakeRandom(0.3f)), Is.EqualTo(1));
        }

        [Test]
        public void Pick_ExcludeBombs_SkipsBombSliceEntirely()
        {
            var slices = new[] { Slice(1), Slice(1, isBomb: true), Slice(1) };
            Assert.That(SpinResolver.Pick(slices, new FakeRandom(0.49f), excludeBombs: true), Is.EqualTo(0));
            Assert.That(SpinResolver.Pick(slices, new FakeRandom(0.51f), excludeBombs: true), Is.EqualTo(2));
        }

        [Test]
        public void Pick_NoPositiveWeight_Throws()
        {
            var slices = new[] { Slice(0), Slice(0) };
            Assert.Throws<ArgumentException>(() => SpinResolver.Pick(slices, new FakeRandom(0.5f)));
        }
    }
}
