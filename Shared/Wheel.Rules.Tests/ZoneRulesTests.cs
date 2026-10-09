using NUnit.Framework;
using Wheel.Rules;

namespace Wheel.Rules.Tests
{
    public class ZoneRulesTests
    {
        static readonly ProgressionRules Rules = new(safeZoneInterval: 5, superZoneInterval: 30, growthPercentPerZone: 25);

        [TestCase(1, WheelTier.Bronze)]
        [TestCase(4, WheelTier.Bronze)]
        [TestCase(5, WheelTier.Silver)]
        [TestCase(10, WheelTier.Silver)]
        [TestCase(25, WheelTier.Silver)]
        [TestCase(30, WheelTier.Golden)]
        [TestCase(60, WheelTier.Golden)]
        [TestCase(31, WheelTier.Bronze)]
        public void GetTier_FollowsSafeAndSuperIntervals(int zone, WheelTier expected)
        {
            Assert.That(ZoneRules.GetTier(zone, Rules), Is.EqualTo(expected));
        }

        [TestCase(1, false)]
        [TestCase(5, true)]
        [TestCase(30, true)]
        [TestCase(7, false)]
        public void IsSafe_TrueOnlyForSilverAndGolden(int zone, bool expected)
        {
            Assert.That(ZoneRules.IsSafe(zone, Rules), Is.EqualTo(expected));
        }

        [TestCase(100, 1, 100)]
        [TestCase(100, 2, 125)]
        [TestCase(100, 5, 200)]
        [TestCase(7, 3, 11)]   // 7 * 1.5 = 10.5 rounds half up
        [TestCase(1, 2, 1)]    // 1.25 rounds down
        [TestCase(2, 2, 3)]    // 2.5 rounds half up
        public void ScaleAmount_GrowsLinearlyWithIntegerMath(int baseAmount, int zone, int expected)
        {
            Assert.That(ZoneRules.ScaleAmount(baseAmount, zone, Rules), Is.EqualTo(expected));
        }

        [TestCase(1, 5)]
        [TestCase(5, 5)]
        [TestCase(6, 10)]
        [TestCase(11, 15)]
        public void NextSafeZone_IsFirstSilverZoneAtOrAfter(int zone, int expected)
        {
            Assert.That(ZoneRules.NextSafeZone(zone, Rules), Is.EqualTo(expected));
        }

        [TestCase(1, 30)]
        [TestCase(30, 30)]
        [TestCase(31, 60)]
        public void NextSuperZone_IsFirstGoldenZoneAtOrAfter(int zone, int expected)
        {
            Assert.That(ZoneRules.NextSuperZone(zone, Rules), Is.EqualTo(expected));
        }
    }
}
